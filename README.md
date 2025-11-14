# Simple Soundboard

A low-latency Windows soundboard application that mixes microphone input with triggered sound files and outputs to virtual or physical audio devices. Perfect for Discord, OBS, streaming, and content creation.

## Features

- **Dark Mode**: Full dark mode theme with toggle button (sun/moon icon) for comfortable viewing
- **Pin/Unpin Sounds**: Pin important sounds to keep them at the top of the list
- **Drag-and-Drop Reordering**: Rearrange sounds by dragging them to new positions
- **VB-Cable Integration**: Automatic detection and setup of VB-Audio Virtual Cable
- **Dual Audio Output**: Sounds play on both VB-Cable (for Discord/OBS) and your main speakers (for monitoring)
- **Stop-and-Play Behavior**: New sounds automatically stop currently playing audio for clean playback
- **Audio Normalization**: Smart -3 dB peak normalization prevents overly loud sounds while boosting quiet ones
- **Global Sound Volume**: Master volume control (0-200%) for all sound effects
- **Table-Style UI**: Compact sound library with Play, Name, Duration, Hotkey, Volume, Play Count columns
- **Visual Audio State**: Color-coded Start/Stop button (Green=OFF, Red=ON) with sound list graying when inactive
- **Stop All Sounds**: Orange button to immediately stop all playing audio (microphone continues)
- **Polished UI**: Application icon displayed in header for professional appearance
- **Low-Latency Audio Engine**: WASAPI with configurable buffer sizes (3-20ms)
- **Real-Time Latency Monitoring**: Live display of audio latency with low CPU overhead
- **Accurate Audio Level Display**: Shows full mixer output (microphone + sounds), not just microphone
- **Real-time Audio Mixing**: Mix microphone input with sound effects
- **Global Hotkeys**: Trigger sounds from anywhere with customizable hotkey combinations (disabled when audio is off)
- **WPF Interface**: Modern design with clear visual feedback and percentage labels on sliders
- **JSON Configuration**: Persistent storage of sound library, settings, and buffer size
- **Microphone Gain Control**: Adjustable gain from 0-200% with percentage display
- **Extended Duration**: Support for audio files up to 30 seconds
- **Hotkey Capture UI**: Click-to-set hotkey controls with visual feedback
- **Auto-clearing Status**: "Playing" messages automatically clear after 30 seconds

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

### Runtime
- Windows 10 or 11
- .NET 8.0 Runtime
- Administrator privileges (for global hotkeys)
- **VB-Audio Virtual Cable** (recommended, free) - [Download Here](https://vb-audio.com/Cable/)
  - See [VBCABLE_SETUP.md](VBCABLE_SETUP.md) for detailed installation and configuration guide

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

4. **Configure Monitoring** (Optional)
   - Check "Enable Monitoring" to hear sounds on your speakers/headphones
   - When enabled, sounds play on both VB-Cable AND your default Windows output
   - Disable if you only want sounds sent to VB-Cable (Discord/OBS)
   - Monitoring is enabled by default

5. **Adjust Volumes**
   - **Microphone Gain**: 0-200%, default 100% (with percentage display)
   - **Global Sound Volume**: 0-200%, default 100% (affects all sounds)
   - Real-time audio level shown in progress bar

6. **Start Audio Engine**
   - Click the **green "▶ Start Audio"** button
   - Button turns **red "⬛ Stop Audio"** when running
   - Progress bar shows full mixer output level (microphone + sounds)
   - Sound items list grays out when audio is OFF

7. **Add Sound Items**
   - Click "Add Sound" button
   - Browse for audio file (MP3, WAV, OGG, FLAC, M4A, WMA)
   - Sound appears in table with duration automatically displayed
   - Edit name directly in table
   - Click hotkey field and press desired key combination
   - Click volume percentage to adjust (0-100%)
   - Maximum sound duration: 30 seconds
   - All sounds automatically normalized to 0 dB

8. **Save Configuration**
   - Click "Save Configuration" to persist your sound library
   - Configuration saved to: `%LOCALAPPDATA%\SimpleSoundboard\config.json`

9. **Play & Stop Sounds**
   - **Audio must be running** (red Stop button visible)
   - **Trigger via hotkey** OR **click green play button (▶)** in table
   - New sound automatically stops any currently playing sound
   - Click **"Stop All Sounds"** (orange button) to stop playback instantly
   - Sounds mix with microphone input and output to VB-Cable
   - If monitoring enabled, sounds also play on your speakers/headphones
   - Status shows "Playing: sound_name" and auto-clears after 30 seconds
   - Hotkeys are ignored when audio is OFF

10. **Use with Discord/OBS**
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

## Recent Updates

### v0.5.0 - Dark Mode, Sound Reordering & Audio Improvements (Latest)
- ✅ **Dark Mode Theme**: Full dark mode support with sun/moon toggle button
- ✅ **Pin/Unpin Sounds**: Pin important sounds to top of list with pin icon
- ✅ **Drag-and-Drop Reordering**: Rearrange sounds by dragging to new positions
- ✅ **Improved Normalization**: Smart -3 dB threshold prevents overly loud sounds
- ✅ **Persistent Ordering**: Sound order and pin state saved in configuration

### v0.4.0 - Major UX Overhaul & Audio Enhancements
- ✅ **Stop-and-Play Behavior**: Sounds automatically stop when new sound triggered (no overlap)
- ✅ **Audio Normalization**: All sounds normalized to 0 dB for consistent volume
- ✅ **Global Sound Volume**: Master volume control (0-200%, default 100%)
- ✅ **Table-Style UI**: Compact sound library with column headers and inline editing
- ✅ **Volume Popup Slider**: Click volume percentage to adjust with popup slider
- ✅ **Stop All Sounds Button**: Orange button to stop all playing audio instantly
- ✅ **Percentage Labels**: Sliders now show percentage values for clarity
- ✅ **Extended Duration**: Audio files up to 30 seconds supported (was 10s)
- ✅ **Duration Display**: Sound duration shown in table (e.g., "5.2s")
- ✅ **Manual Play Button**: Green play button (▶) in table for ad-hoc playback
- ✅ **Unsaved Changes Detection**: Prompts to save configuration changes on exit
- ✅ **Auto-Save Play Counts**: Play statistics always saved, even if you don't save config
- ✅ **Graceful Shutdown**: Audio stops automatically on exit, no lingering processes
- ✅ **Remove Confirmation**: Confirmation dialog prevents accidental sound deletion
- ✅ **Fixed Hotkey Bug**: All hotkeys now work correctly after restart

### v0.3.0 - UI/UX Improvements & Bug Fixes
- ✅ **Visual Audio State Indicator**: Green/Red Start/Stop button, grayed-out sounds when OFF
- ✅ **App Icon in UI**: Application icon displayed in top-right corner for polished look
- ✅ **Improved Audio Level Display**: Now shows full mixer output (mic + sounds)
- ✅ **Auto-clearing Status Messages**: "Playing" messages clear after 30 seconds
- ✅ **Buffer Size Persistence**: Latency setting now saved in configuration
- ✅ **Fixed Audio Reinitialization**: Settings changes no longer require manual restart
- ✅ **Fixed Configuration Loading**: Saved sounds now play correctly on startup
- ✅ **Fixed Null Reference Crash**: Resolved crash when adding sounds
- ✅ **Hotkeys Disabled When OFF**: Prevents confusion and unwanted play count increments

### v0.2.0 - Dual Audio Output (Monitoring)
- ✅ **Dual Output System**: Sounds play on both VB-Cable AND main speakers
- ✅ **Enable Monitoring Checkbox**: Toggle monitoring on/off
- ✅ **Smart Duplicate Detection**: Skips duplicate output when VB-Cable is default device
- ✅ **Zero Additional Latency**: Parallel audio streams

### v0.1.0 - Initial Release
- ✅ **Core Audio Engine**: WASAPI-based low-latency capture, playback, and real-time mixing
- ✅ **Sound Management**: Add/remove sounds with per-sound volume control and 10-second duration limit
- ✅ **Global Hotkeys**: Win32 API support with custom control and conflict detection
- ✅ **VB-Cable Integration**: Automatic detection and auto-selection of "CABLE Input"
- ✅ **Latency Control**: Configurable buffer sizes (3/5/10/20ms) with real-time monitoring
- ✅ **Device Management**: Audio device enumeration with VB-Cable prioritization
- ✅ **Configuration System**: JSON-based persistence for settings and sound library
- ✅ **Audio Compatibility**: Automatic resampling to 48kHz and mono-to-stereo conversion

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

## Future Enhancements

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

## Credits

- **NAudio**: Audio library by Mark Heath
- **VB-Audio Software**: Virtual audio cable drivers
- **WPF**: Microsoft's Windows Presentation Foundation
