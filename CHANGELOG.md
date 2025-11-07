# Changelog

All notable changes to **SimpleSoundboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [v0.4.0] - Major UX Overhaul & Audio Enhancements

Significant improvements to audio playback behavior, UI redesign with table layout, and extended audio capabilities.

### Added

* **Stop-and-Play Audio Behavior:** When a new sound is triggered, any currently playing sound is automatically stopped.
    * Same sound can be retriggered by pressing its hotkey again.
    * Prevents audio overlap for cleaner sound playback.
* **Stop All Sounds Button & Hotkey:** Orange "Stop All Sounds" button to immediately stop all playing audio.
    * Microphone continues playing (only sounds are stopped).
    * Programmable hotkey support for stopping sounds.
* **Audio Normalization:** All sounds are automatically normalized to 0 dB (peak normalization).
    * Ensures consistent volume levels across different audio files.
    * Prevents sounds from being too quiet or too loud.
* **Global Sound Volume Slider:** Master volume control (0-200%, default 100%) for all sound effects.
    * Acts as multiplier on individual sound volumes.
    * Allows quick adjustment of all sounds without changing individual settings.
* **Table-Style Sound Items UI:** Completely redesigned sound library interface.
    * Compact table layout with columns: Play button, Name, Duration, Hotkey, Volume, Play Count, Remove.
    * Shows sound duration in seconds.
    * Green play button (▶) for manual playback.
    * Inline name editing.
    * Click-to-adjust volume display (shows percentage).
    * More sounds visible at once.
* **Percentage Labels on Sliders:** Microphone Gain and Global Sound Volume sliders now show percentage values.
* **Extended Audio Duration:** Maximum sound length increased from 10 seconds to **30 seconds**.

### Changed

* **Audio Engine:** Tracks currently playing sounds for stop-and-play behavior.
* **Sound Volume Display:** Changed from slider to percentage text in table (click to adjust).
* **UI Layout:** Reorganized controls - Global Sound Volume added, VB-Cable status moved for better space utilization.

### Fixed

* **Graceful Shutdown**: Application now properly stops audio and saves play counts before exiting.
    * Audio is stopped automatically when closing the window
    * Play counts are always saved, even if user chooses not to save configuration changes
    * All background threads are terminated to prevent lingering processes
* **Hotkey Registration Race Condition**: Fixed bug where only the first sound's hotkey would work after restarting with saved configuration.
    * Hotkeys are now properly registered after async configuration loading completes
* **Remove Confirmation**: Added confirmation dialog when removing sounds to prevent accidental deletion.
* **Volume Popup Display**: Fixed volume percentage display in table (now shows 0%-100% instead of 0.0-1.0).

### Technical Details

* AudioEngine: Added `StopAllSounds()`, `IsSoundPlaying()`, `GlobalSoundVolume` property.
* AudioEngine: Implemented sound tracking with `_playingSounds` dictionary and `_currentlyPlayingSoundId`.
* AudioEngine: Added peak normalization in `CachedSound` constructor.
* AudioEngine: `CachedSoundSampleProvider` now has `Stop()` method and `_stopped` flag.
* MainViewModel: Added `StopAllSoundsCommand`, `PlaySoundCommand`, `GlobalSoundVolume` property.
* MainViewModel: Added `PlaySoundInternal()` helper for unified sound playback.
* MainViewModel: Added `SavePlayCountsAsync()` to persist play statistics on exit.
* MainViewModel: Added `StopAudio()` public method for clean shutdown.
* MainViewModel: Added `HasUnsavedChanges` tracking with exclusion of volume and play count changes.
* MainViewModel: Added `PromptSaveChangesAsync()` for exit confirmation dialog.
* HotkeyManager: Added `IsInitialized` property to check initialization state.
* HotkeyManager: Added `RegisterAllHotkeys()` helper method.
* SoundItem: Added `DurationSeconds` property.
* MainWindow: Complete table-style UI with fixed column widths for consistent alignment.
* MainWindow: Added `OnClosing` handler to stop audio and prompt for unsaved changes.
* App.xaml: Set `ShutdownMode="OnMainWindowClose"` for proper application termination.

---

## [v0.3.0] - UI/UX Improvements & Bug Fixes

Significant improvements to user experience, visual feedback, and several critical bug fixes.

### Added

* **Visual Audio State Indicator:**
    * Start/Stop button now shows **green (▶ Start Audio)** when OFF and **red (⬛ Stop Audio)** when ON.
    * Larger, bolder button with clear visual feedback.
    * Sound items list grayed out (40% opacity) when audio is OFF.
    * Clear indication that audio system must be running for sounds to play.
* **App Icon in UI:** Application icon now displayed in top-right corner of header (256x256 pixels).
* **Status Message Auto-Clear:** "Playing: sound_name" message now automatically clears after 10 seconds.
* **Buffer Size Persistence:** Buffer size (latency) setting now persists in configuration file.

### Changed

* **Audio Level Monitoring:**
    * Changed from microphone-only to **full mixer output monitoring** (microphone + sounds).
    * Uses `MeteringSampleProvider` for accurate level display.
    * Bar is completely empty (0%) when audio is stopped.
* **Audio Reinitialization:** Now properly stops and restarts audio engine when changing settings.
    * Buffer size changes work without manual restart.
    * Output device changes work without manual restart.
    * Monitoring toggle works without manual restart.
* **Hotkey Behavior:** Hotkeys are now **disabled when audio is OFF**.
    * Prevents confusion when audio system isn't running.
    * Play counts only increment when audio is actually running.

### Fixed

* **Critical: Saved Configuration Loading:** Sounds from saved configuration now load correctly on startup.
    * Fixed null `_mixerFormat` issue by using default format (48kHz stereo) before initialization.
    * Sounds now play immediately after loading configuration and starting audio.
* **Critical: Null Reference Exception:** Fixed crash when adding sounds due to null WaveFormat access.
    * Added proper null checks in resampling logic.
* **Audio Reinitialization:** Fixed issue where changing settings required manual audio restart.
    * Now automatically stops, reinitializes, and restarts audio.
* **Audio Level Display:** Bar no longer shows residual 2% when audio is off.
    * Properly resets to 0 when stopping audio or on error.

### Technical Details

* AudioEngine: Added `MeteringSampleProvider` wrapper for mixer output monitoring.
* AudioEngine: `LoadSoundAsync` now uses default format fallback: `_mixerFormat ?? WaveFormat.CreateIeeeFloatWaveFormat(48000, 2)`.
* MainViewModel: `ReinitializeAudio()` now calls Stop() → Initialize() → Start() sequence.
* MainViewModel: Added `AudioLevel = 0f` when stopping audio.
* MainViewModel: Added `OnHotkeyPressed` early return when `!IsAudioRunning`.
* MainViewModel: Added `DispatcherTimer` for status message auto-clear.
* AppConfiguration: `BufferSize` property now saved and loaded from config.
* MainWindow: Start/Stop button styled with DataTriggers for green/red states.
* MainWindow: Sound items list opacity controlled by `IsAudioRunning` binding.

---

## [v0.2.0] - Dual Audio Output (Monitoring)

Added monitoring support allowing users to hear sounds on their speakers/headphones while simultaneously sending audio to VB-Cable.

### Added

* **Dual Audio Output System:**
    * Sounds now play on both **VB-Cable AND user's main speakers/headphones**.
    * "Enable Monitoring" checkbox in UI (enabled by default).
    * Automatic detection when output device matches Windows default (skips duplicate output).
    * **Only sounds are monitored** (not microphone input to prevent echo).
    * Zero additional latency for monitoring.
    * Independent mixer for monitor output with graceful error handling.
* **Unit Tests:** Added comprehensive test suite with 73 tests covering AudioEngine monitoring functionality.

### Changed

* AudioEngine now manages two output streams: main output (VB-Cable) and monitor output (default device).
* Monitor output initialization failures are non-critical (app continues without monitoring).

### Technical Details

* AudioEngine: Added `_monitorOutput`, `_monitorMixer`, `MonitoringEnabled` property.
* MainViewModel: Added `MonitoringEnabled` property with audio reinitialization.
* MainWindow: Added "Enable Monitoring" checkbox with tooltip.

---

## [v0.1.0] - Initial Release

This release establishes the core functionality, including the low-latency audio engine, sound management, global hotkey support, and integration with the VB-Cable virtual device.

### Added

* **Core Audio Engine:** Implemented **WASAPI-based audio engine (NAudio)** for low-latency capture, playback, and **real-time mixing** of microphone input and sound files.
    * Supports multiple audio formats (MP3, WAV, OGG, FLAC, M4A, WMA).
* **Sound Management:** Functionality to add, remove, and configure sounds.
    * **Per-sound volume control** (0-100%) and enable/disable toggle.
    * 10-second sound duration limit with error handling.
* **Hotkey System:** Full support for **Global Hotkeys** using the Win32 API.
    * Custom control for setting hotkeys with conflict detection.
    * Support for Ctrl, Alt, Shift, Win modifiers.
* **VB-Cable Integration System:** Automatic **VB-Cable detection** on startup and auto-selection of "CABLE Input."
    * Visual status indicator and installation guide button.
* **Latency Control:** Added support for **configurable buffer sizes** (3ms, 5ms, 10ms, 20ms).
    * **Real-time latency monitoring** display in the UI.
* **Device Management:** Audio device enumeration, selection dropdowns, and refresh functionality.
    * Prioritization of VB-Cable for output selection when available.
* **Configuration System:** **JSON-based persistence** for saving device selections, sound library, and hotkey bindings to `%LOCALAPPDATA%`.
* **Documentation:** Comprehensive setup guides for **VB-Cable, Discord, and OBS**.

### Changed

* The audio engine was configured for **Shared Mode** to improve compatibility.
* The **default buffer size** was set to **5ms** (240 samples) for optimized latency.
* UI layout reorganized for better visual hierarchy and audio configuration access.

### Fixed

* **Audio Compatibility:** Resolved **WaveFormat mismatch errors** by implementing automatic resampling to the standard 48kHz sample rate.
* Fixed issues causing **sped-up audio playback** by ensuring correct resampling and channel conversion (mono-to-stereo).
* **Hotkey Reliability:** Ensured global hotkeys are properly registered and their changes immediately reflected in the UI.
* **Latency Accuracy:** Corrected the calculation and application of selected buffer sizes within the audio engine.