namespace SimpleSoundboard.Core.Models;

public class AppConfiguration
{
    public string SelectedInputDeviceId { get; set; } = string.Empty;
    public string SelectedOutputDeviceId { get; set; } = string.Empty;
    public int BufferSize { get; set; } = 480;
    public int SampleRate { get; set; } = 48000;
    public float MasterVolume { get; set; } = 1.0f;
    public float MicrophoneVolume { get; set; } = 1.0f;
    public bool UseExclusiveMode { get; set; } = true;
    public bool EnableVirtualAudioDriver { get; set; } = false;
    public bool IsDarkMode { get; set; } = false;
    public List<SoundItem> SoundItems { get; set; } = new();

    public static AppConfiguration GetDefault()
    {
        return new AppConfiguration();
    }
}
