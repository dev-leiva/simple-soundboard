# Simple Soundboard

A low-latency Windows soundboard application that mixes microphone input with triggered sound files and outputs to a virtual audio device. Perfect for Discord, OBS, streaming, and content creation.

## Features

- **VB-Cable Integration**: Automatic detection and setup of VB-Audio Virtual Cable
- **Low-Latency Audio Engine**: WASAPI with configurable buffer sizes (3-20ms)
- **Real-Time Latency Monitoring**: Live display of audio latency with low CPU overhead
- **Real-time Audio Mixing**: Mix microphone input with sound effects
- **Global Hotkeys**: Trigger sounds from anywhere with customizable hotkey combinations
- **WPF Interface**: Modern design with real-time audio level monitoring
- **JSON Configuration**: Persistent storage of sound library and settings
- **Microphone Gain Control**: Adjustable gain from 0-200%
- **Duration Limiting**: Automatic 10-second sound limit with graceful handling
- **Hotkey Capture UI**: Click-to-set hotkey controls with visual feedback

## Technology Stack

- **Language**: C# with .NET 8+
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Audio Processing**: NAudio with WASAPI
- **Architecture**: MVVM pattern with proper separation of concerns
- **Virtual Audio**: VB-Cable integration for routing

## Prerequisites

### Development
- Visual Studio 2022 (Community or higher)
- .NET 8.0 SDK
- Windows 11 SDK (10.0.19041.0+)
- Windows App SDK 1.4+

### Runtime
- Windows 10 or 11
- .NET 8.0 Runtime
- Administrator privileges (for global hotkeys)
- **VB-Audio Virtual Cable** (recommended, free) - [Download Here](https://vb-audio.com/Cable/)
  - See [VBCABLE_SETUP.md](VBCABLE_SETUP.md) for detailed installation and configuration guide

## Testing

The project includes comprehensive unit and integration tests.

### Running Tests
```powershell
# Run all tests
dotnet test

# Run with detailed output
dotnet test --verbosity detailed

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run only unit tests
dotnet test --filter "FullyQualifiedName~Unit"

# Run only integration tests
dotnet test --filter "FullyQualifiedName~Integration"
```

### Test Coverage
- **60+ Unit Tests**: Sound management, audio processing, hotkeys, configuration
- **18+ Integration Tests**: Audio pipeline, performance benchmarks
- **Coverage**: Models (95%), Services (80%), Core (70%), Helpers (100%)

See [SimpleSoundboard.Tests/README.md](SimpleSoundboard.Tests/README.md) for detailed test documentation.

## Building the Project

### Using Visual Studio
1. Open `SimpleSoundboard.sln` in Visual Studio 2022
2. Restore NuGet packages (should happen automatically)
3. Set build configuration to `Debug|x64` or `Release|x64`
4. Build the solution (Ctrl+Shift+B)

### Using Command Line
```powershell
# Restore dependencies
dotnet restore

# Build
dotnet build -c Release

# Publish (creates self-contained executable)
dotnet publish -c Release -r win-x64 --self-contained
```

## Running the Application

### From Visual Studio
1. Right-click the SimpleSoundboard project
2. Select "Run as Administrator" (required for global hotkeys)
3. Press F5 or click Start

### From Command Line
```powershell
# Run the built executable as Administrator
Start-Process -FilePath ".\SimpleSoundboard\bin\x64\Release\net8.0-windows10.0.19041.0\SimpleSoundboard.exe" -Verb RunAs
```

## Usage

1. **VB-Cable Setup** (First Time Only)
   - Click "VB-Cable Setup" button for installation instructions
   - Install VB-Cable from https://vb-audio.com/Cable/
   - Restart SimpleSoundboard
   - "VB-Cable Status" should show "VB-Cable Detected"
   - See [VBCABLE_SETUP.md](VBCABLE_SETUP.md) for detailed guide

2. **Select Audio Devices**
   - Choose your microphone from the "Input Device" dropdown
   - Output device auto-selects to "CABLE Input" if VB-Cable is installed
   - Can manually select any physical or virtual output device
   - Click "Refresh Devices" if devices don't appear

3. **Configure Latency** (Optional)
   - Select buffer size from dropdown: 3ms, 5ms (default), 10ms, or 20ms
   - Lower = less latency, higher CPU usage
   - Higher = more stable, lower CPU usage
   - "Current Latency" displays real-time measurement

4. **Adjust Microphone Gain**
   - Use "Microphone Gain" slider (0-200%, default 100%)
   - Real-time audio level shown in progress bar

5. **Start Audio Engine**
   - Click "Start/Stop Audio" button
   - Progress bar shows microphone input level

6. **Add Sound Items**
   - Click "Add Sound" button
   - Browse for audio file (MP3, WAV, OGG, FLAC, M4A, WMA)
   - Sound name auto-populates from filename (editable)
   - Click hotkey field and press desired key combination
   - Adjust volume slider (0-100%)
   - Maximum sound duration: 10 seconds

7. **Save Configuration**
   - Click "Save Configuration" to persist your sound library
   - Configuration saved to: `%LOCALAPPDATA%\SimpleSoundboard\config.json`

8. **Trigger Sounds**
   - With audio engine running, press configured hotkeys
   - Sounds will mix with microphone input and output to selected device

9. **Use with Discord/OBS**
   - **Discord**: Settings → Voice & Video → Input Device → "CABLE Output"
   - **OBS**: Add "Audio Input Capture" → Device → "CABLE Output"
   - See [VBCABLE_SETUP.md](VBCABLE_SETUP.md) for detailed setup

## Configuration File

Configuration is stored in JSON format at:
```
%LOCALAPPDATA%\SimpleSoundboard\config.json
```

Example structure:
```json
{
  "selectedInputDeviceId": "{device-guid}",
  "selectedOutputDeviceId": "{device-guid}",
  "bufferSize": 480,
  "sampleRate": 48000,
  "masterVolume": 1.0,
  "microphoneVolume": 1.0,
  "useExclusiveMode": true,
  "soundItems": [
    {
      "id": "guid",
      "name": "Example Sound",
      "filePath": "C:\\Path\\To\\sound.wav",
      "volume": 0.8,
      "hotkey": {
        "id": 1,
        "modifiers": 2,
        "virtualKeyCode": 49
      },
      "isEnabled": true
    }
  ]
}
```

### Hotkey Configuration

Hotkeys use Windows virtual key codes and modifier flags:

**Modifiers** (combine with bitwise OR):
- `0x0001` - Alt
- `0x0002` - Control (Ctrl)
- `0x0004` - Shift
- `0x0008` - Win

**Common Virtual Key Codes**:
- `0x30-0x39` - Number keys 0-9
- `0x41-0x5A` - Letter keys A-Z
- `0x70-0x87` - Function keys F1-F24

Example: Ctrl+Shift+1 = `"modifiers": 6, "virtualKeyCode": 49`

## Project Structure

```
SimpleSoundboard/
├── Models/              # Data models
│   ├── SoundItem.cs
│   ├── HotkeyBinding.cs
│   ├── AudioDeviceInfo.cs
│   └── AppConfiguration.cs
├── ViewModels/          # MVVM ViewModels
│   └── MainViewModel.cs
├── Views/               # XAML UI
│   ├── MainWindow.xaml
│   └── MainWindow.xaml.cs
├── Services/            # Business logic
│   ├── AudioEngine.cs
│   ├── HotkeyManager.cs
│   └── ConfigurationService.cs
├── Core/                # Win32 interop
│   ├── Win32Interop.cs
│   └── VirtualAudioDriver.cs
├── Helpers/             # Utilities
│   ├── ObservableObject.cs
│   └── RelayCommand.cs
├── App.xaml[.cs]        # Application entry
└── Program.cs           # Main entry point
```

## Recent Updates

### v1.1 - VB-Cable Integration & Latency Optimization
- ✅ VB-Cable automatic detection and setup
- ✅ Configurable buffer sizes (3/5/10/20ms)
- ✅ Real-time latency monitoring
- ✅ Hotkey capture UI (click-to-set)
- ✅ File browser dialog with audio filters
- ✅ Microphone gain control (0-200%)
- ✅ 10-second sound duration limit
- ✅ Auto-resampling for format compatibility
- ✅ WaveFormat mismatch fixes
- ✅ Playback speed corrections

### v1.0 - WPF Migration
- ✅ Migrated from WinUI 3 to WPF for stability
- ✅ Fixed DLL loading issues on Windows 11
- ✅ WASAPI Shared mode for better compatibility
- ✅ Observable SoundItem for real-time UI updates

## Future Enhancements

- [ ] Exclusive mode WASAPI with fallback
- [ ] Drag-and-drop file support
- [ ] Sound preview playback
- [ ] Volume meters per sound
- [ ] Sound categories/folders
- [ ] Import/export sound packs
- [ ] Dark theme support
- [ ] System tray minimization
- [ ] Sound search/filter
- [ ] Customizable sound duration limits

## Troubleshooting

### VB-Cable Not Detected
- Verify VB-Cable is installed (Windows Sound Settings → Playback → look for "CABLE Input")
- Click "Refresh Devices" button
- Manually select "CABLE Input" from output dropdown
- Restart SimpleSoundboard
- See [VBCABLE_SETUP.md](VBCABLE_SETUP.md) for complete troubleshooting

### Audio Engine Won't Start
- Ensure devices are connected and recognized by Windows
- Try running as Administrator
- Check if another application is using the device
- Verify microphone is not muted in Windows

### Audio Crackling/Stuttering
- Increase buffer size to 10ms or 20ms
- Close background applications (browsers, Spotify, etc.)
- Use "High Performance" power plan
- Check CPU usage in Task Manager

### Hotkeys Not Working
- Verify application is running as Administrator
- Check if hotkey is already registered by another application
- Try different key combinations
- Unregister and re-register the hotkey

### No Audio Devices Found
- Click "Refresh Devices"
- Ensure audio drivers are installed
- Check Windows Sound settings
- Restart audio services: `net stop audiosrv && net start audiosrv`

### No Audio in Discord/OBS
- Verify SimpleSoundboard audio is **started** (not just open)
- Check Discord/OBS input device is set to "CABLE Output"
- Verify VB-Cable device is not muted in Windows
- Test microphone gain slider
- See [VBCABLE_SETUP.md](VBCABLE_SETUP.md) for application-specific setup

## License

MIT License - See LICENSE file for details

## Documentation

- **[VBCABLE_SETUP.md](VBCABLE_SETUP.md)** - Complete VB-Cable installation, configuration, and troubleshooting guide
- **[SimpleSoundboard.Tests/README.md](SimpleSoundboard.Tests/README.md)** - Unit and integration test documentation

## Credits

- **NAudio**: Audio library by Mark Heath
- **VB-Audio Software**: Virtual audio cable drivers
- **WPF**: Microsoft's Windows Presentation Foundation
