using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SimpleSoundboard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SimpleSoundboard.WPF.Services;

public class AudioEngine : IDisposable
{
    private WasapiCapture? _microphoneCapture;
    private WasapiOut? _audioOutput;
    private WasapiOut? _monitorOutput;
    private MixingSampleProvider? _mixer;
    private MixingSampleProvider? _monitorMixer;
    private BufferedWaveProvider? _microphoneBuffer;
    private VolumeSampleProvider? _micVolumeProvider;
    private MeteringSampleProvider? _meteringSampleProvider;
    private readonly object _lock = new();
    private bool _isRunning;
    private readonly Dictionary<Guid, CachedSound> _cachedSounds = new();
    private WaveFormat? _mixerFormat;
    private float _microphoneGain = 1.0f;
    private int _currentBufferSize = 240;
    private DateTime _lastLatencyCheck = DateTime.Now;
    private float _currentLatencyMs = 0f;
    private bool _monitoringEnabled = true;

    public event EventHandler<float>? AudioLevelChanged;
    public event EventHandler<string>? ErrorOccurred;
    public event EventHandler<float>? LatencyChanged;

    public bool IsRunning => _isRunning;
    public float CurrentLatencyMs => _currentLatencyMs;
    public int CurrentBufferSize => _currentBufferSize;
    public bool MonitoringEnabled
    {
        get => _monitoringEnabled;
        set => _monitoringEnabled = value;
    }
    
    public void SetMicrophoneGain(float gain)
    {
        _microphoneGain = Math.Clamp(gain, 0f, 2.0f);
        if (_micVolumeProvider != null)
        {
            _micVolumeProvider.Volume = _microphoneGain;
        }
    }

    public void SetBufferSize(int bufferSize)
    {
        _currentBufferSize = bufferSize;
    }

    public void Initialize(AudioDeviceInfo inputDevice, AudioDeviceInfo outputDevice, int sampleRate, int bufferSize)
    {
        try
        {
            Stop();

            _currentBufferSize = bufferSize;
            _mixerFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

            if (!string.IsNullOrEmpty(inputDevice.DeviceId))
            {
                var captureDevices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
                var device = captureDevices.FirstOrDefault(d => d.ID == inputDevice.DeviceId);
                
                if (device != null)
                {
                    _microphoneCapture = new WasapiCapture(device, true, bufferSize / sampleRate);
                    _microphoneBuffer = new BufferedWaveProvider(_microphoneCapture.WaveFormat)
                    {
                        BufferLength = sampleRate * 10,
                        DiscardOnBufferOverflow = true
                    };

                    _microphoneCapture.DataAvailable += OnMicrophoneDataAvailable;
                }
            }

            _mixer = new MixingSampleProvider(_mixerFormat)
            {
                ReadFully = true
            };

            if (_microphoneBuffer != null)
            {
                var micSampleProvider = _microphoneBuffer.ToSampleProvider();
                ISampleProvider micInput = micSampleProvider;
                
                // Resample if needed
                if (micSampleProvider.WaveFormat.SampleRate != sampleRate || micSampleProvider.WaveFormat.Channels != 2)
                {
                    if (micSampleProvider.WaveFormat.Channels == 1)
                    {
                        micInput = new MonoToStereoSampleProvider(micSampleProvider);
                    }
                    if (micInput.WaveFormat.SampleRate != sampleRate)
                    {
                        micInput = new WdlResamplingSampleProvider(micInput, sampleRate);
                    }
                }
                
                // Add volume control for microphone gain
                _micVolumeProvider = new VolumeSampleProvider(micInput)
                {
                    Volume = _microphoneGain
                };
                _mixer.AddMixerInput(_micVolumeProvider);
            }

            if (!string.IsNullOrEmpty(outputDevice.DeviceId))
            {
                var renderDevices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var device = renderDevices.FirstOrDefault(d => d.ID == outputDevice.DeviceId);

                if (device != null)
                {
                    // Wrap mixer with metering to monitor output levels
                    _meteringSampleProvider = new MeteringSampleProvider(_mixer);
                    _meteringSampleProvider.StreamVolume += OnStreamVolume;
                    
                    // Use Shared mode for better compatibility (Exclusive mode often fails with 0x88890016)
                    _audioOutput = new WasapiOut(device, AudioClientShareMode.Shared, true, bufferSize);
                    _audioOutput.Init(_meteringSampleProvider);
                }
            }

            // Initialize monitoring output (sounds only, not microphone)
            if (_monitoringEnabled)
            {
                try
                {
                    var enumerator = new MMDeviceEnumerator();
                    var defaultDevice = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
                    
                    // Only create monitor output if it's different from the main output
                    if (defaultDevice.ID != outputDevice.DeviceId)
                    {
                        _monitorMixer = new MixingSampleProvider(_mixerFormat)
                        {
                            ReadFully = true
                        };
                        
                        _monitorOutput = new WasapiOut(defaultDevice, AudioClientShareMode.Shared, true, bufferSize);
                        _monitorOutput.Init(_monitorMixer);
                    }
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"Monitor output initialization error (non-critical): {ex.Message}");
                    // Continue without monitoring - not critical
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Initialization error: {ex.Message}");
            throw;
        }
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _microphoneCapture?.StartRecording();
            _audioOutput?.Play();
            _monitorOutput?.Play();
            _isRunning = true;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Start error: {ex.Message}");
            throw;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        try
        {
            _microphoneCapture?.StopRecording();
            _audioOutput?.Stop();
            _monitorOutput?.Stop();
            _isRunning = false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Stop error: {ex.Message}");
        }
    }

    public async System.Threading.Tasks.Task<bool> LoadSoundAsync(SoundItem soundItem)
    {
        try
        {
            var result = await System.Threading.Tasks.Task.Run(() =>
            {
                var audioFile = new AudioFileReader(soundItem.FilePath);
                
                // Check duration (10 second limit)
                if (audioFile.TotalTime.TotalSeconds > 10)
                {
                    audioFile.Dispose();
                    return (false, $"Sound '{soundItem.Name}' is too long ({audioFile.TotalTime.TotalSeconds:F1}s). Maximum is 10 seconds.");
                }
                
                // Use default format if mixer format is not initialized yet (will be reloaded when audio starts)
                var targetFormat = _mixerFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);
                var cachedSound = new CachedSound(audioFile, soundItem.Volume, targetFormat);
                
                lock (_lock)
                {
                    _cachedSounds[soundItem.Id] = cachedSound;
                }
                
                return (true, string.Empty);
            });

            if (!result.Item1)
            {
                ErrorOccurred?.Invoke(this, result.Item2);
            }
            return result.Item1;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Load sound error: {ex.Message}");
            return false;
        }
    }

    public void PlaySound(Guid soundId, float volume = 1.0f)
    {
        if (!_isRunning || _mixer == null) return;

        lock (_lock)
        {
            if (_cachedSounds.TryGetValue(soundId, out var cachedSound))
            {
                // Add sound to main output (VB-Cable)
                var sampleProvider = new CachedSoundSampleProvider(cachedSound, volume);
                _mixer.AddMixerInput(sampleProvider);
                
                // Also add sound to monitor output (user's speakers) if available
                if (_monitorMixer != null && _monitoringEnabled)
                {
                    var monitorSampleProvider = new CachedSoundSampleProvider(cachedSound, volume);
                    _monitorMixer.AddMixerInput(monitorSampleProvider);
                }
            }
        }
    }

    private void OnMicrophoneDataAvailable(object? sender, WaveInEventArgs e)
    {
        _microphoneBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);

        // Calculate latency every 500ms to reduce overhead
        if ((DateTime.Now - _lastLatencyCheck).TotalMilliseconds > 500)
        {
            _lastLatencyCheck = DateTime.Now;
            var sampleRate = _mixerFormat?.SampleRate ?? 48000;
            _currentLatencyMs = (_currentBufferSize / (float)sampleRate) * 1000f;
            LatencyChanged?.Invoke(this, _currentLatencyMs);
        }
    }

    private void OnStreamVolume(object? sender, StreamVolumeEventArgs e)
    {
        // Use the max value from the metering to show overall output level (microphone + sounds)
        AudioLevelChanged?.Invoke(this, e.MaxSampleValues[0]);
    }

    public List<AudioDeviceInfo> GetAudioDevices(AudioDeviceType deviceType)
    {
        var devices = new List<AudioDeviceInfo>();

        try
        {
            // Ensure COM is initialized for the current thread
            var comInitialized = false;
            try
            {
                var hr = CoInitializeEx(IntPtr.Zero, COINIT_MULTITHREADED);
                comInitialized = (hr == 0 || hr == 1); // S_OK or S_FALSE (already initialized)
            }
            catch
            {
                // COM may already be initialized - continue anyway
            }

            try
            {
                var enumerator = new MMDeviceEnumerator();
                var dataFlow = deviceType == AudioDeviceType.Input ? DataFlow.Capture : DataFlow.Render;
                var deviceCollection = enumerator.EnumerateAudioEndPoints(dataFlow, DeviceState.Active);

                foreach (var device in deviceCollection)
                {
                    try
                    {
                        devices.Add(new AudioDeviceInfo
                        {
                            DeviceId = device.ID,
                            FriendlyName = device.FriendlyName,
                            DeviceType = deviceType,
                            SampleRate = device.AudioClient.MixFormat.SampleRate,
                            Channels = device.AudioClient.MixFormat.Channels,
                            IsDefault = device.ID == enumerator.GetDefaultAudioEndpoint(dataFlow, Role.Multimedia).ID
                        });
                    }
                    catch (Exception deviceEx)
                    {
                        // Skip this device if we can't read its properties
                        ErrorOccurred?.Invoke(this, $"Error reading device {device.FriendlyName}: {deviceEx.Message}");
                    }
                }
            }
            finally
            {
                if (comInitialized)
                {
                    try { CoUninitialize(); } catch { }
                }
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Device enumeration error: {ex.Message}");
        }

        return devices;
    }

    public void Dispose()
    {
        Stop();
        _microphoneCapture?.Dispose();
        _audioOutput?.Dispose();
        _monitorOutput?.Dispose();
        _mixer = null;
        _monitorMixer = null;
        
        lock (_lock)
        {
            foreach (var sound in _cachedSounds.Values)
            {
                sound.Dispose();
            }
            _cachedSounds.Clear();
        }
    }

    // COM Interop for initialization
    private const int COINIT_MULTITHREADED = 0x0;

    [DllImport("ole32.dll")]
    private static extern int CoInitializeEx(IntPtr pvReserved, int dwCoInit);

    [DllImport("ole32.dll")]
    private static extern void CoUninitialize();

    private class CachedSound : IDisposable
    {
        public float[] AudioData { get; }
        public WaveFormat WaveFormat { get; }
        public float Volume { get; }

        public CachedSound(AudioFileReader reader, float volume, WaveFormat targetFormat)
        {
            WaveFormat = targetFormat;
            Volume = volume;

            var sampleProvider = reader.ToSampleProvider();
            ISampleProvider input = sampleProvider;
            
            // Convert mono to stereo if needed
            if (sampleProvider.WaveFormat.Channels == 1 && targetFormat.Channels == 2)
            {
                input = new MonoToStereoSampleProvider(sampleProvider);
            }
            
            // Resample if sample rate doesn't match
            var currentSampleRate = input.WaveFormat.SampleRate;
            if (currentSampleRate != targetFormat.SampleRate)
            {
                input = new WdlResamplingSampleProvider(input, targetFormat.SampleRate);
            }
            
            var samples = new List<float>();
            var buffer = new float[targetFormat.SampleRate];
            int samplesRead;

            while ((samplesRead = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                samples.AddRange(buffer.Take(samplesRead));
            }

            AudioData = samples.ToArray();
            reader.Dispose();
        }

        public void Dispose()
        {
        }
    }

    private class CachedSoundSampleProvider : ISampleProvider
    {
        private readonly CachedSound _cachedSound;
        private readonly float _volume;
        private long _position;

        public WaveFormat WaveFormat => _cachedSound.WaveFormat;

        public CachedSoundSampleProvider(CachedSound cachedSound, float volume)
        {
            _cachedSound = cachedSound;
            _volume = volume * cachedSound.Volume;
        }

        public int Read(float[] buffer, int offset, int count)
        {
            var availableSamples = _cachedSound.AudioData.Length - _position;
            var samplesToCopy = Math.Min(availableSamples, count);

            for (int i = 0; i < samplesToCopy; i++)
            {
                buffer[offset + i] = _cachedSound.AudioData[_position + i] * _volume;
                buffer[offset + i] = Math.Clamp(buffer[offset + i], -1.0f, 1.0f);
            }

            _position += samplesToCopy;
            return (int)samplesToCopy;
        }
    }
}
