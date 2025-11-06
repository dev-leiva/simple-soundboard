using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using SimpleSoundboard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace SimpleSoundboard.Services;

public class AudioEngine : IDisposable
{
    private WasapiCapture? _microphoneCapture;
    private WasapiOut? _audioOutput;
    private MixingSampleProvider? _mixer;
    private BufferedWaveProvider? _microphoneBuffer;
    private readonly object _lock = new();
    private bool _isRunning;
    private readonly Dictionary<Guid, CachedSound> _cachedSounds = new();

    public event EventHandler<float>? AudioLevelChanged;
    public event EventHandler<string>? ErrorOccurred;

    public bool IsRunning => _isRunning;

    public void Initialize(AudioDeviceInfo inputDevice, AudioDeviceInfo outputDevice, int sampleRate, int bufferSize)
    {
        try
        {
            Stop();

            var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);

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

            _mixer = new MixingSampleProvider(waveFormat)
            {
                ReadFully = true
            };

            if (_microphoneBuffer != null)
            {
                var micSampleProvider = _microphoneBuffer.ToSampleProvider();
                if (micSampleProvider.WaveFormat.SampleRate != sampleRate)
                {
                    var resampler = new WdlResamplingSampleProvider(micSampleProvider, sampleRate);
                    _mixer.AddMixerInput(resampler);
                }
                else
                {
                    _mixer.AddMixerInput(micSampleProvider);
                }
            }

            if (!string.IsNullOrEmpty(outputDevice.DeviceId))
            {
                var renderDevices = new MMDeviceEnumerator().EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
                var device = renderDevices.FirstOrDefault(d => d.ID == outputDevice.DeviceId);

                if (device != null)
                {
                    _audioOutput = new WasapiOut(device, AudioClientShareMode.Exclusive, true, bufferSize / sampleRate);
                    _audioOutput.Init(_mixer);
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
            await System.Threading.Tasks.Task.Run(() =>
            {
                var audioFile = new AudioFileReader(soundItem.FilePath);
                var cachedSound = new CachedSound(audioFile, soundItem.Volume);
                
                lock (_lock)
                {
                    _cachedSounds[soundItem.Id] = cachedSound;
                }
            });

            return true;
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
                var sampleProvider = new CachedSoundSampleProvider(cachedSound, volume);
                _mixer.AddMixerInput(sampleProvider);
            }
        }
    }

    private void OnMicrophoneDataAvailable(object? sender, WaveInEventArgs e)
    {
        _microphoneBuffer?.AddSamples(e.Buffer, 0, e.BytesRecorded);

        var maxSample = 0f;
        for (int i = 0; i < e.BytesRecorded; i += 2)
        {
            var sample = Math.Abs(BitConverter.ToInt16(e.Buffer, i) / 32768f);
            if (sample > maxSample) maxSample = sample;
        }

        AudioLevelChanged?.Invoke(this, maxSample);
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
        _mixer = null;
        
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

        public CachedSound(AudioFileReader reader, float volume)
        {
            WaveFormat = reader.WaveFormat;
            Volume = volume;

            var sampleProvider = reader.ToSampleProvider();
            var samples = new List<float>();
            var buffer = new float[reader.WaveFormat.SampleRate];
            int samplesRead;

            while ((samplesRead = sampleProvider.Read(buffer, 0, buffer.Length)) > 0)
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
