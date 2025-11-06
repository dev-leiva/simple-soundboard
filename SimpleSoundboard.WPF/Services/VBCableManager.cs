using SimpleSoundboard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleSoundboard.WPF.Services;

public class VBCableManager
{
    private readonly AudioEngine _audioEngine;
    
    public event EventHandler<VBCableStatus>? StatusChanged;
    
    public VBCableManager(AudioEngine audioEngine)
    {
        _audioEngine = audioEngine;
    }
    
    /// <summary>
    /// Detects if VB-Cable is installed by checking for "CABLE Input" device
    /// </summary>
    public VBCableStatus DetectVBCable()
    {
        try
        {
            var outputDevices = _audioEngine.GetAudioDevices(AudioDeviceType.Output);
            
            // Look for VB-Cable devices (common names)
            var cableDevice = outputDevices.FirstOrDefault(d => 
                d.FriendlyName.Contains("CABLE Input", StringComparison.OrdinalIgnoreCase) ||
                d.FriendlyName.Contains("VB-Audio Virtual Cable", StringComparison.OrdinalIgnoreCase));
            
            if (cableDevice != null)
            {
                return new VBCableStatus
                {
                    IsInstalled = true,
                    IsAvailable = true,
                    DeviceInfo = cableDevice,
                    Message = "VB-Cable detected and ready"
                };
            }
            
            return new VBCableStatus
            {
                IsInstalled = false,
                IsAvailable = false,
                Message = "VB-Cable not detected. Install for virtual audio routing."
            };
        }
        catch (Exception ex)
        {
            return new VBCableStatus
            {
                IsInstalled = false,
                IsAvailable = false,
                Message = $"Error detecting VB-Cable: {ex.Message}"
            };
        }
    }
    
    /// <summary>
    /// Attempts to auto-select VB-Cable as output device
    /// </summary>
    public AudioDeviceInfo? TryAutoSelectVBCable()
    {
        var status = DetectVBCable();
        
        if (status.IsAvailable && status.DeviceInfo != null)
        {
            StatusChanged?.Invoke(this, status);
            return status.DeviceInfo;
        }
        
        return null;
    }
    
    /// <summary>
    /// Gets setup instructions for the user
    /// </summary>
    public string GetSetupInstructions()
    {
        return @"VB-Cable Setup Instructions:

1. Download VB-AUDIO Virtual Cable from: https://vb-audio.com/Cable/
2. Extract the ZIP file
3. Run 'VBCABLE_Setup_x64.exe' as Administrator
4. Click 'Install Driver'
5. Restart SimpleSoundboard

After installation, VB-Cable will appear as 'CABLE Input' in output devices.

Discord Setup:
- Voice & Video → Input Device → Select 'CABLE Output (VB-Audio Virtual Cable)'

OBS Setup:
- Add Audio Input Capture source
- Device → Select 'CABLE Output (VB-Audio Virtual Cable)'

Note: You can still hear your audio by setting 'CABLE Input' as your default playback device,
or use VoiceMeeter for more advanced routing.";
    }
}

public class VBCableStatus
{
    public bool IsInstalled { get; set; }
    public bool IsAvailable { get; set; }
    public AudioDeviceInfo? DeviceInfo { get; set; }
    public string Message { get; set; } = string.Empty;
}
