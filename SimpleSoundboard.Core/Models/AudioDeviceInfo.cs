namespace SimpleSoundboard.Core.Models;

public enum AudioDeviceType
{
    Input,
    Output
}

public class AudioDeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public string FriendlyName { get; set; } = string.Empty;
    public AudioDeviceType DeviceType { get; set; }
    public int Channels { get; set; }
    public int SampleRate { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;

    public override string ToString()
    {
        var defaultMarker = IsDefault ? " (Default)" : "";
        return $"{FriendlyName}{defaultMarker}";
    }
}
