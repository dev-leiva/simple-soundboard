using SimpleSoundboard.Helpers;
using SimpleSoundboard.Core.Models;
using SimpleSoundboard.Core.Services;
using SimpleSoundboard.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.UI.Dispatching;

namespace SimpleSoundboard.ViewModels;

public class MainViewModel : ObservableObject, IDisposable
{
    private readonly AudioEngine _audioEngine;
    private readonly HotkeyManager _hotkeyManager;
    private readonly ConfigurationService _configService;
    private readonly DispatcherQueue _dispatcherQueue;

    private bool _isAudioRunning;
    private float _audioLevel;
    private string _statusMessage = "Ready";
    private AudioDeviceInfo? _selectedInputDevice;
    private AudioDeviceInfo? _selectedOutputDevice;

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

    public MainViewModel()
    {
        try
        {
            _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
            _audioEngine = new AudioEngine();
            _hotkeyManager = new HotkeyManager();
            _configService = new ConfigurationService();

            _audioEngine.AudioLevelChanged += OnAudioLevelChanged;
            _audioEngine.ErrorOccurred += OnAudioError;
            _hotkeyManager.HotkeyPressed += OnHotkeyPressed;
            _hotkeyManager.ErrorOccurred += OnHotkeyError;

            StartStopAudioCommand = new RelayCommand(ToggleAudio);
            AddSoundCommand = new RelayCommand(AddSound);
            RemoveSoundCommand = new RelayCommand<SoundItem>(RemoveSound);
            RefreshDevicesCommand = new RelayCommand(RefreshDevices);
            SaveConfigurationCommand = new RelayCommand(async () => await SaveConfigurationAsync());

            // Safely attempt to initialize devices and config
            try
            {
                RefreshDevices();
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

            if (SelectedOutputDevice == null && OutputDevices.Count > 0)
                SelectedOutputDevice = OutputDevices.FirstOrDefault(d => d.IsDefault) ?? OutputDevices[0];

            StatusMessage = $"Found {InputDevices.Count} input(s) and {OutputDevices.Count} output(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error refreshing devices: {ex.Message}";
            System.Diagnostics.Debug.WriteLine($"[ERROR] RefreshDevices exception: {ex}");
            // Don't throw - allow the app to continue even if device enumeration fails
        }
    }

    private async System.Threading.Tasks.Task SaveConfigurationAsync()
    {
        try
        {
            var config = new AppConfiguration
            {
                SelectedInputDeviceId = SelectedInputDevice?.DeviceId ?? string.Empty,
                SelectedOutputDeviceId = SelectedOutputDevice?.DeviceId ?? string.Empty,
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
                StatusMessage = "Audio stopped";
            }
            else
            {
                if (SelectedInputDevice != null && SelectedOutputDevice != null)
                {
                    _audioEngine.Initialize(SelectedInputDevice, SelectedOutputDevice, 48000, 480);
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
        }
    }

    private void ReinitializeAudio()
    {
        if (IsAudioRunning && SelectedInputDevice != null && SelectedOutputDevice != null)
        {
            try
            {
                _audioEngine.Initialize(SelectedInputDevice, SelectedOutputDevice, 48000, 480);
                StatusMessage = "Audio reinitialized";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error reinitializing: {ex.Message}";
            }
        }
    }

    private void AddSound()
    {
        var newSound = new SoundItem
        {
            Name = $"Sound {SoundItems.Count + 1}",
            Volume = 1.0f
        };

        SoundItems.Add(newSound);
        StatusMessage = "Sound added. Configure file path and hotkey.";
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
        _dispatcherQueue.TryEnqueue(() =>
        {
            var sound = SoundItems.FirstOrDefault(s => s.Id == soundId);
            if (sound != null && sound.IsEnabled)
            {
                _audioEngine.PlaySound(soundId, sound.Volume);
                sound.LastPlayed = DateTime.Now;
                sound.PlayCount++;
                StatusMessage = $"Playing: {sound.Name}";
            }
        });
    }

    private void OnAudioLevelChanged(object? sender, float level)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            AudioLevel = level;
        });
    }

    private void OnAudioError(object? sender, string error)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StatusMessage = $"Audio Error: {error}";
        });
    }

    private void OnHotkeyError(object? sender, string error)
    {
        _dispatcherQueue.TryEnqueue(() =>
        {
            StatusMessage = $"Hotkey Error: {error}";
        });
    }

    public void Dispose()
    {
        _audioEngine?.Dispose();
        _hotkeyManager?.Dispose();
    }
}
