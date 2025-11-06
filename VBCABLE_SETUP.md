# VB-Cable Integration Guide

## Overview

SimpleSoundboard now features seamless VB-Cable integration for virtual audio routing, allowing you to route your microphone + soundboard mix to Discord, OBS, or any other application without additional hardware.

## Features

- **Automatic VB-Cable Detection**: Detects and auto-selects "CABLE Input" when available
- **Manual Device Selection**: Can still select any physical audio output if preferred
- **Low-Latency Optimization**: Configurable buffer sizes (3ms, 5ms, 10ms, 20ms)
- **Real-Time Latency Monitoring**: Live display of current audio latency
- **Setup Instructions**: Built-in help dialog for VB-Cable installation

## VB-Cable Installation

### Step 1: Download VB-Cable

1. Visit: https://vb-audio.com/Cable/
2. Download the VB-AUDIO Virtual Cable installer (free)
3. Extract the ZIP file to a temporary location

### Step 2: Install the Driver

1. Right-click on `VBCABLE_Setup_x64.exe` (or `VBCABLE_Setup.exe` for 32-bit)
2. Select **"Run as Administrator"**
3. Click **"Install Driver"**
4. Wait for installation to complete
5. Restart SimpleSoundboard

### Step 3: Verify Installation

- Open SimpleSoundboard
- Check the **"VB-Cable Status"** in the audio configuration section
- Should display: **"VB-Cable Detected"**
- The output device should auto-select to **"CABLE Input (VB-Audio Virtual Cable)"**

## Application Setup

### Discord Configuration

To route SimpleSoundboard audio to Discord:

1. Open Discord → Settings → Voice & Video
2. Under **"Input Device"**, select:
   - **"CABLE Output (VB-Audio Virtual Cable)"**
3. Adjust input sensitivity as needed
4. Start audio in SimpleSoundboard
5. Your microphone + soundboard will now be transmitted to Discord

### OBS Studio Configuration

To capture SimpleSoundboard audio in OBS:

1. In OBS, add a new source: **Audio Input Capture**
2. Under **"Device"**, select:
   - **"CABLE Output (VB-Audio Virtual Cable)"**
3. Adjust audio levels in OBS mixer
4. Start audio in SimpleSoundboard
5. Your microphone + soundboard will now be captured in OBS

### Other Applications

Any application that supports audio input can use:
- **Input Device**: "CABLE Output (VB-Audio Virtual Cable)"

## Buffer Size Configuration

SimpleSoundboard offers four latency presets:

| Buffer Size | Samples | Latency | Best For |
|-------------|---------|---------|----------|
| **3ms** | 144 | ~3ms | Ultra-low latency, requires powerful CPU |
| **5ms** ⭐ | 240 | ~5ms | **Recommended** - Best balance |
| **10ms** | 480 | ~10ms | Stable performance, moderate CPU |
| **20ms** | 960 | ~20ms | Maximum stability, low CPU usage |

### Changing Buffer Size

1. Locate **"Buffer Size (Latency)"** dropdown in the UI
2. Select desired preset
3. Audio will automatically reinitialize with new settings
4. Monitor **"Current Latency"** display for real-time feedback

### Recommendations

- **Gaming/Streaming**: Use **5ms** for responsive audio
- **Music Production**: Use **3ms** for precise timing
- **Stability Priority**: Use **10ms** or **20ms** if experiencing crackling
- **Low-End Hardware**: Start with **20ms**, decrease if stable

## Latency Monitoring

The **"Current Latency"** display shows real-time audio latency in milliseconds.

- Updates every 500ms to conserve CPU resources
- Formula: `(buffer_size / sample_rate) * 1000`
- Example: 240 samples ÷ 48000 Hz × 1000 = **5.00ms**

## Troubleshooting

### VB-Cable Not Detected

**Solution:**
1. Verify VB-Cable is installed (check Windows Sound Settings)
2. Click **"Refresh Devices"** in SimpleSoundboard
3. Manually select "CABLE Input" from output devices dropdown
4. Restart SimpleSoundboard if still not detected

### Audio Crackling/Stuttering

**Solution:**
1. Increase buffer size to **10ms** or **20ms**
2. Close unnecessary background applications
3. Disable CPU-intensive effects or plugins
4. Update audio drivers

### No Audio in Discord/OBS

**Solution:**
1. Verify SimpleSoundboard audio is **started** (not just open)
2. Check that Discord/OBS input device is set to **"CABLE Output"**
3. Verify VB-Cable device is **not muted** in Windows Sound Settings
4. Test microphone gain slider in SimpleSoundboard
5. Ensure sounds are loaded and enabled

### Hearing Audio Twice (Echo)

**Cause:** You're monitoring SimpleSoundboard output AND receiving it in Discord/OBS

**Solution:**
- Don't set "CABLE Input" as your default Windows playback device
- Use headphones plugged into your physical audio device
- Or use VoiceMeeter for advanced routing with monitoring

## Advanced Configuration

### Using Physical Outputs

SimpleSoundboard can still output to physical devices:

1. In **"Output Device"** dropdown, select your physical device
2. VB-Cable status will show "VB-Cable Not Found" (expected)
3. Audio will route to speakers/headphones directly
4. Use this for testing or personal monitoring

### Monitoring Your Mix

If you want to hear what you're sending:

**Option 1: VoiceMeeter** (Recommended for advanced users)
- Download from: https://vb-audio.com/Voicemeeter/
- Provides full routing matrix with monitoring
- More complex setup, better control

**Option 2: Windows "Listen to this device"**
1. Right-click speaker icon → Sounds
2. Recording tab → CABLE Output → Properties
3. Listen tab → Check "Listen to this device"
4. Select your playback device
5. Warning: Adds latency

### Multiple Virtual Cables

VB-Cable offers VB-Cable A+B for multiple virtual devices:
- Download from: https://vb-audio.com/Cable/
- Allows routing different audio to different destinations
- SimpleSoundboard will detect "CABLE Input" (the first cable)

## Performance Tips

1. **CPU Usage**: Lower buffer sizes increase CPU usage
2. **USB Devices**: May require larger buffers (10-20ms)
3. **Background Apps**: Close Spotify, browser tabs, etc.
4. **Power Settings**: Use "High Performance" power plan
5. **Sample Rate**: App uses 48kHz (standard for gaming/streaming)

## FAQ

**Q: Is VB-Cable free?**  
A: Yes, but donationware. Consider supporting the developer.

**Q: Do I need VB-Cable to use SimpleSoundboard?**  
A: No, you can use physical audio devices. VB-Cable enables virtual routing.

**Q: Can I use other virtual audio drivers?**  
A: Yes, SimpleSoundboard will auto-detect any device with "CABLE Input" in the name.

**Q: Why is latency important?**  
A: Lower latency means less delay between speaking and the audio being transmitted.

**Q: What's the target latency range?**  
A: 5-15ms is ideal for real-time voice communication.

**Q: Can I use ASIO drivers?**  
A: ASIO is not currently supported. SimpleSoundboard uses WASAPI for compatibility.

## Getting Help

If you encounter issues not covered here:

1. Click **"VB-Cable Setup"** button in SimpleSoundboard for quick reference
2. Check Windows Event Viewer for audio errors
3. Verify VB-Cable driver is properly installed
4. Test with physical audio devices first to isolate issues

## Technical Details

- **Audio API**: WASAPI (Windows Audio Session API)
- **Share Mode**: Shared (for compatibility)
- **Sample Rate**: 48000 Hz
- **Channels**: Stereo (2 channels)
- **Bit Depth**: 32-bit float
- **Buffer Range**: 144-960 samples
- **Latency Update**: 500ms interval (low CPU overhead)

---

**Version**: 1.0  
**Last Updated**: 2025  
**VB-Cable Version**: Compatible with all VB-Cable releases
