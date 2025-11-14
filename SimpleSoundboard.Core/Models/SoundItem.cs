using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleSoundboard.Core.Models;

public class SoundItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private Guid _id = Guid.NewGuid();
    private string _name = string.Empty;
    private string _filePath = string.Empty;
    private float _volume = 1.0f;
    private HotkeyBinding? _hotkey;
    private bool _isEnabled = true;
    private DateTime _lastPlayed;
    private int _playCount;
    private double _durationSeconds;
    private bool _isPinned = false;
    private int _sortOrder = 0;

    public Guid Id { get => _id; set { _id = value; OnPropertyChanged(); } }
    public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
    public string FilePath { get => _filePath; set { _filePath = value; OnPropertyChanged(); } }
    public float Volume { get => _volume; set { _volume = value; OnPropertyChanged(); } }
    public HotkeyBinding? Hotkey { get => _hotkey; set { _hotkey = value; OnPropertyChanged(); } }
    public bool IsEnabled { get => _isEnabled; set { _isEnabled = value; OnPropertyChanged(); } }
    public DateTime LastPlayed { get => _lastPlayed; set { _lastPlayed = value; OnPropertyChanged(); } }
    public int PlayCount { get => _playCount; set { _playCount = value; OnPropertyChanged(); } }
    public double DurationSeconds { get => _durationSeconds; set { _durationSeconds = value; OnPropertyChanged(); } }
    public bool IsPinned { get => _isPinned; set { _isPinned = value; OnPropertyChanged(); } }
    public int SortOrder { get => _sortOrder; set { _sortOrder = value; OnPropertyChanged(); } }

    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(Name) && 
               !string.IsNullOrWhiteSpace(FilePath) && 
               File.Exists(FilePath) &&
               Volume >= 0.0f && Volume <= 1.0f;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
