# Changelog

All notable changes to **SimpleSoundboard** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

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