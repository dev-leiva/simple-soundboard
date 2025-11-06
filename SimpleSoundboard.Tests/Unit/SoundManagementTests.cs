using FluentAssertions;
using SimpleSoundboard.Core.Models;
using SimpleSoundboard.Core.Services;
using Xunit;

namespace SimpleSoundboard.Tests.Unit;

public class SoundManagementTests
{
    [Fact]
    public void AddNewSound_WithValidProperties_ShouldAddToCollection()
    {
        var soundItem = new SoundItem
        {
            Name = "Test Sound",
            FilePath = Path.Combine(Path.GetTempPath(), "test.wav"),
            Volume = 0.8f,
            IsEnabled = true
        };

        File.WriteAllText(soundItem.FilePath, "dummy content");

        try
        {
            soundItem.IsValid().Should().BeTrue();
            soundItem.Name.Should().Be("Test Sound");
            soundItem.Volume.Should().Be(0.8f);
        }
        finally
        {
            if (File.Exists(soundItem.FilePath))
                File.Delete(soundItem.FilePath);
        }
    }

    [Fact]
    public void AddSound_WithDuplicateHotkey_ShouldDetectConflict()
    {
        var hotkey1 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x41
        };

        var hotkey2 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x41
        };

        hotkey1.Should().Be(hotkey2);
        hotkey1.GetHashCode().Should().Be(hotkey2.GetHashCode());
    }

    [Fact]
    public void AddSound_WithInvalidFilePath_ShouldFailValidation()
    {
        var soundItem = new SoundItem
        {
            Name = "Invalid Sound",
            FilePath = "C:\\NonExistent\\Path\\file.wav",
            Volume = 0.8f
        };

        soundItem.IsValid().Should().BeFalse();
    }

    [Fact]
    public void RemoveSound_WithValidIdentifier_ShouldRemoveFromCollection()
    {
        var sounds = new List<SoundItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Sound 1" },
            new() { Id = Guid.NewGuid(), Name = "Sound 2" }
        };

        var targetId = sounds[0].Id;
        sounds.RemoveAll(s => s.Id == targetId);

        sounds.Should().HaveCount(1);
        sounds.Should().NotContain(s => s.Id == targetId);
    }

    [Fact]
    public void RemoveSound_WithInvalidIdentifier_ShouldNotChangeCollection()
    {
        var sounds = new List<SoundItem>
        {
            new() { Id = Guid.NewGuid(), Name = "Sound 1" },
            new() { Id = Guid.NewGuid(), Name = "Sound 2" }
        };

        var initialCount = sounds.Count;
        var nonExistentId = Guid.NewGuid();
        
        sounds.RemoveAll(s => s.Id == nonExistentId);

        sounds.Should().HaveCount(initialCount);
    }

    [Fact]
    public void UpdateSound_Properties_ShouldPersist()
    {
        var sound = new SoundItem
        {
            Name = "Original Name",
            Volume = 0.5f
        };

        sound.Name = "Updated Name";
        sound.Volume = 0.9f;

        sound.Name.Should().Be("Updated Name");
        sound.Volume.Should().Be(0.9f);
    }

    [Fact(Skip = "Requires test isolation - shared config file")]
    public async Task DataPersistence_SaveAndReload_ShouldRestoreIdentically()
    {
        var configService = new ConfigurationService();
        var config = new AppConfiguration
        {
            SoundItems = new List<SoundItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Sound",
                    FilePath = "C:\\test.wav",
                    Volume = 0.7f,
                    Hotkey = new HotkeyBinding
                    {
                        Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
                        VirtualKeyCode = 0x42
                    }
                }
            }
        };

        var saved = await configService.SaveConfigurationAsync(config);
        saved.Should().BeTrue();

        var loaded = await configService.LoadConfigurationAsync();
        loaded.SoundItems.Should().HaveCount(1);
        loaded.SoundItems[0].Name.Should().Be("Test Sound");
        loaded.SoundItems[0].Volume.Should().Be(0.7f);
    }

    [Fact]
    public void HotkeyConflictDetection_AfterReload_ShouldDetectProperly()
    {
        var existingHotkeys = new List<HotkeyBinding>
        {
            new()
            {
                Modifiers = ModifierKeys.Control,
                VirtualKeyCode = 0x31
            }
        };

        var newHotkey = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x31
        };

        existingHotkeys.Should().Contain(h => h.Equals(newHotkey));
    }

    [Fact]
    public void SoundItem_WithEmptyName_ShouldFailValidation()
    {
        var sound = new SoundItem
        {
            Name = "",
            FilePath = Path.Combine(Path.GetTempPath(), "test.wav"),
            Volume = 1.0f
        };

        sound.IsValid().Should().BeFalse();
    }

    [Fact]
    public void SoundItem_WithVolumeOutOfRange_ShouldFailValidation()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), "test.wav");
        File.WriteAllText(tempFile, "dummy");

        try
        {
            var sound = new SoundItem
            {
                Name = "Test",
                FilePath = tempFile,
                Volume = 1.5f
            };

            sound.IsValid().Should().BeFalse();
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}
