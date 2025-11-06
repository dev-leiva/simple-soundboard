using SimpleSoundboard.WPF.Helpers;
using SimpleSoundboard.Core.Models;
using SimpleSoundboard.Core.Services;
using SimpleSoundboard.WPF.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;

namespace SimpleSoundboard.WPF.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
    private readonly AudioEngine _audioEngine;
    private readonly HotkeyManager _hotkeyManager;
    private readonly ConfigurationService _configService;
    private readonly VBCableManager _vbCableManager;
    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _statusClearTimer;

    private bool _isAudioRunning;
    private float _audioLevel;
    private float _microphoneGain = 1.0f;
    private float _currentLatency = 0f;
    private string _statusMessage = "Ready";
    private string _vbCableStatus = "Checking...";
    private AudioDeviceInfo? _selectedInputDevice;
    private AudioDeviceInfo? _selectedOutputDevice;
    private int _selectedBufferSize = 240;
    private bool _monitoringEnabled = true;

    public ObservableCollection<SoundItem> SoundItems { get; } = new();
    public ObservableCollection<AudioDeviceInfo> InputDevices { get; } = new();
    public ObservableCollection<AudioDeviceInfo> OutputDevices { get; } = new();

    public bool IsAudioRunning
    {
        get => _isAudioRunning;
        set => SetProperty(ref _isAudioRunning, value);
    }

    public float AudioLevel
    {
        get => _audioLevel;
        set => SetProperty(ref _audioLevel, value);
    }

    public float MicrophoneGain
    {
        get => _microphoneGain;
        set
        {
            if (SetProperty(ref _microphoneGain, value))
            {
                _audioEngine.SetMicrophoneGain(value);
            }
        }
    }

    public float CurrentLatency
    {
        get => _currentLatency;
        set => SetProperty(ref _currentLatency, value);
    }

    public string VBCableStatus
    {
        get => _vbCableStatus;
        set => SetProperty(ref _vbCableStatus, value);
    }

    public int SelectedBufferSize
    {
        get => _selectedBufferSize;
        set
        {
            if (SetProperty(ref _selectedBufferSize, value))
            {
                _audioEngine.SetBufferSize(value);
                ReinitializeAudio();
            }
        }
    }

    public bool MonitoringEnabled
    {
        get => _monitoringEnabled;
        set
        {
            if (SetProperty(ref _monitoringEnabled, value))
            {
                _audioEngine.MonitoringEnabled = value;
                ReinitializeAudio();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public AudioDeviceInfo? SelectedInputDevice
    {
        get => _selectedInputDevice;
        set
        {
            if (SetProperty(ref _selectedInputDevice, value))
            {
                ReinitializeAudio();
            }
        }
    }

    public AudioDeviceInfo? SelectedOutputDevice
    {
        get => _selectedOutputDevice;
        set
        {
            if (SetProperty(ref _selectedOutputDevice, value))
            {
                ReinitializeAudio();
            }
        }
    }

    public ICommand StartStopAudioCommand { get; }
    public ICommand AddSoundCommand { get; }
    public ICommand RemoveSoundCommand { get; }
    public ICommand RefreshDevicesCommand { get; }
    public ICommand SaveConfigurationCommand { get; }
    public ICommand ShowVBCableSetupCommand { get; }

    public MainViewModel()
    {
        try
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            _audioEngine = new AudioEngine();
            _hotkeyManager = new HotkeyManager();
            _configService = new ConfigurationService();
            _vbCableManager = new VBCableManager(_audioEngine);

            _audioEngine.AudioLevelChanged += OnAudioLevelChanged;
            _audioEngine.ErrorOccurred += OnAudioError;
            _audioEngine.LatencyChanged += OnLatencyChanged;
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _hotkeyManager.ErrorOccurred += OnHotkeyError;
            _vbCableManager.StatusChanged += OnVBCableStatusChanged;

            StartStopAudioCommand = new RelayCommand(ToggleAudio);
            AddSoundCommand = new RelayCommand(AddSound);
            RemoveSoundCommand = new RelayCommand<SoundItem>(RemoveSound);
            RefreshDevicesCommand = new RelayCommand(RefreshDevices);
            SaveConfigurationCommand = new RelayCommand(async () => await SaveConfigurationAsync());
            ShowVBCableSetupCommand = new RelayCommand(ShowVBCableSetup);

            // Safely attempt to initialize devices and config
            try
            {
                RefreshDevices();
                CheckVBCableStatus();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Warning: Could not enumerate audio devices. {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshDevices failed: {ex}");
            }

            try
            {
                _ = LoadConfigurationAsync();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Warning: Could not load configuration. {ex.Message}";
                System.Diagnostics.Debug.WriteLine($"[ERROR] LoadConfigurationAsync failed: {ex}");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Critical initialization error: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[CRITICAL] MainViewModel constructor failed: {ex}");
            throw; // Re-throw so MainWindow can handle it
        }
    }

    public void InitializeHotkeys(IntPtr windowHandle)
    {
        _hotkeyManager.Initialize(windowHandle);
        
        foreach (var sound in SoundItems.Where(s => s.Hotkey != null))
        {
            _hotkeyManager.RegisterHotkey(sound.Id, sound.Hotkey!);
        }
    }

    public void ProcessHotkey(int hotkeyId)
    {
        _hotkeyManager.ProcessHotkeyMessage(hotkeyId);
    }

    private async System.Threading.Tasks.Task LoadConfigurationAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();

            foreach (var sound in config.SoundItems)
            {
                SoundItems.Add(sound);
                
                // Subscribe to property changes for hotkey registration
                sound.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SoundItem.Hotkey) && s is SoundItem snd)
                    {
                        RegisterHotkeyForSound(snd);
                    }
                };
                
                if (sound.IsValid())
                {
                    await _audioEngine.LoadSoundAsync(sound);
                }
            }

            if (!string.IsNullOrEmpty(config.SelectedInputDeviceId))
            {
                SelectedInputDevice = InputDevices.FirstOrDefault(d => d.DeviceId == config.SelectedInputDeviceId);
            }

            if (!string.IsNullOrEmpty(config.SelectedOutputDeviceId))
            {
                SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.DeviceId == config.SelectedOutputDeviceId);
            }

            // Restore buffer size if valid
            if (config.BufferSize > 0)
            {
                _selectedBufferSize = config.BufferSize;
                _audioEngine.SetBufferSize(config.BufferSize);
                OnPropertyChanged(nameof(SelectedBufferSize));
            }

            StatusMessage = "Configuration loaded";
        }
        catch (Exception ex)
        {
        StatusMessage = $"Error loading configuration: {ex.Message}";
        }
    }

    private void RefreshDevices()
    {
        try
        {
            InputDevices.Clear();
            OutputDevices.Clear();

            var inputs = _audioEngine.GetAudioDevices(AudioDeviceType.Input);
            var outputs = _audioEngine.GetAudioDevices(AudioDeviceType.Output);

            foreach (var device in inputs)
                InputDevices.Add(device);

            foreach (var device in outputs)
                OutputDevices.Add(device);

            if (SelectedInputDevice == null && InputDevices.Count > 0)
                SelectedInputDevice = InputDevices.FirstOrDefault(d => d.IsDefault) ?? InputDevices[0];

            // Try to auto-select VB-Cable if available
            if (SelectedOutputDevice == null && OutputDevices.Count > 0)
            {
                var vbCableDevice = _vbCableManager.TryAutoSelectVBCable();
                SelectedOutputDevice = vbCableDevice ?? OutputDevices.FirstOrDefault(d => d.IsDefault) ?? OutputDevices[0];
            }

            StatusMessage = $"Found {InputDevices.Count} input(s) and {OutputDevices.Count} output(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing devices: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshDevices exception: {ex}");
            // Don't throw - allow the app to continue even if device enumeration fails
        }
    }

    private void CheckVBCableStatus()
    {
        var status = _vbCableManager.DetectVBCable();
        VBCableStatus = status.IsInstalled ? "VB-Cable Detected" : "VB-Cable Not Found";
    }

    private async System.Threading.Tasks.Task SaveConfigurationAsync()
    {
        try
        {
            var config = new AppConfiguration
            {
                SelectedInputDeviceId = SelectedInputDevice?.DeviceId ?? string.Empty,
                SelectedOutputDeviceId = SelectedOutputDevice?.DeviceId ?? string.Empty,
                BufferSize = _selectedBufferSize,
                SoundItems = SoundItems.ToList()
            };

            var success = await _configService.SaveConfigurationAsync(config);
            StatusMessage = success ? "Configuration saved" : "Failed to save configuration";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving configuration: {ex.Message}";
        }
    }


    private void ToggleAudio()
    {
        try
        {
            if (IsAudioRunning)
            {
                _audioEngine.Stop();
                IsAudioRunning = false;
                AudioLevel = 0f; // Reset audio level bar to empty
                StatusMessage = "Audio stopped";
            }
            else
            {
                if (SelectedInputDevice != null && SelectedOutputDevice != null)
                {
                    _audioEngine.Initialize(SelectedInputDevice, SelectedOutputDevice, 48000, _selectedBufferSize);
                    _audioEngine.Start();
                    IsAudioRunning = true;
                    StatusMessage = "Audio running";
                }
                else
                {
                    StatusMessage = "Please select input and output devices";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
            IsAudioRunning = false;
            AudioLevel = 0f; // Reset audio level bar to empty
        }
    }

    private void ReinitializeAudio()
    {
        if (IsAudioRunning && SelectedInputDevice != null && SelectedOutputDevice != null)
        {
            try
            {
                // Stop, reinitialize, and restart audio
                _audioEngine.Stop();
                _audioEngine.Initialize(SelectedInputDevice, SelectedOutputDevice, 48000, _selectedBufferSize);
                _audioEngine.Start();
                StatusMessage = "Audio reinitialized";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reinitializing: {ex.Message}";
                IsAudioRunning = false;
            }
        }
    }

    private void AddSound()
    {
        var openFileDialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Select Sound File",
            Filter = "Audio Files|*.mp3;*.wav;*.ogg;*.flac;*.m4a;*.wma|" +
                     "MP3 Files|*.mp3|" +
                     "WAV Files|*.wav|" +
                     "All Files|*.*",
            FilterIndex = 1
        };

        if (openFileDialog.ShowDialog() == true)
        {
            var filePath = openFileDialog.FileName;
            var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            var newSound = new SoundItem
            {
                Name = fileName,
                FilePath = filePath,
                Volume = 1.0f,
                IsEnabled = true
            };

            SoundItems.Add(newSound);
            
            // Try to load the sound
            _ = _audioEngine.LoadSoundAsync(newSound);
            
            // Subscribe to property changes to register hotkey when set
            if (newSound is System.ComponentModel.INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(SoundItem.Hotkey) && s is SoundItem sound)
                    {
                        RegisterHotkeyForSound(sound);
                    }
                };
            }
            
            StatusMessage = $"Added sound: {fileName}";
        }
    }

    private void RegisterHotkeyForSound(SoundItem sound)
    {
        if (sound.Hotkey != null)
        {
            // Unregister old hotkey if exists
            _hotkeyManager.UnregisterHotkey(sound.Id);
            
            // Register new hotkey
            if (_hotkeyManager.RegisterHotkey(sound.Id, sound.Hotkey))
            {
                StatusMessage = $"Hotkey registered for {sound.Name}: {sound.Hotkey.GetDisplayString()}";
            }
            else
            {
                StatusMessage = $"Failed to register hotkey for {sound.Name}. It might be in use.";
            }
        }
    }

    private void RemoveSound(SoundItem? sound)
    {
        if (sound != null)
        {
            if (sound.Hotkey != null)
            {
                _hotkeyManager.UnregisterHotkey(sound.Id);
            }

            SoundItems.Remove(sound);
            StatusMessage = $"Removed sound: {sound.Name}";
        }
    }

    private void OnHotkeyPressed(object? sender, Guid soundId)
    {
        _dispatcher.Invoke(() =>
        {
            // Only process hotkeys when audio is running
            if (!IsAudioRunning)
            {
                return;
            }
            
            var sound = SoundItems.FirstOrDefault(s => s.Id == soundId);
            if (sound != null && sound.IsEnabled)
            {
                _audioEngine.PlaySound(soundId, sound.Volume);
                sound.LastPlayed = DateTime.Now;
                sound.PlayCount++;
                StatusMessage = $"Playing: {sound.Name}";
                
                // Clear the "Playing" message after sound duration (max 10 seconds)
                _statusClearTimer?.Stop();
                _statusClearTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(10)
                };
                _statusClearTimer.Tick += (s, e) =>
                {
                    _statusClearTimer?.Stop();
                    if (StatusMessage.StartsWith("Playing:"))
                    {
                        StatusMessage = "Ready";
                    }
                };
                _statusClearTimer.Start();
            }
        });
    }

    private void OnAudioLevelChanged(object? sender, float level)
    {
        _dispatcher.Invoke(() =>
        {
            AudioLevel = level;
        });
    }

    private void OnAudioError(object? sender, string error)
    {
        _dispatcher.Invoke(() =>
        {
            StatusMessage = $"Audio Error: {error}";
        });
    }

    private void OnHotkeyError(object? sender, string error)
    {
        _dispatcher.Invoke(() =>
        {
            StatusMessage = $"Hotkey Error: {error}";
        });
    }

    private void OnLatencyChanged(object? sender, float latency)
    {
        _dispatcher.Invoke(() =>
        {
            CurrentLatency = latency;
        });
    }

    private void OnVBCableStatusChanged(object? sender, VBCableStatus status)
    {
        _dispatcher.Invoke(() =>
        {
            VBCableStatus = status.IsInstalled ? "VB-Cable Detected" : "VB-Cable Not Found";
        });
    }

    private void ShowVBCableSetup()
    {
        var instructions = _vbCableManager.GetSetupInstructions();
        System.Windows.MessageBox.Show(
            instructions,
            "VB-Cable Setup Instructions",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    public void Dispose()
    {
        _audioEngine?.Dispose();
        _hotkeyManager?.Dispose();
    }
}
