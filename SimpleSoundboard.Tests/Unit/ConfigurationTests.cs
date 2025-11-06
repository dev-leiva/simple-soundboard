using FluentAssertions;
using SimpleSoundboard.Core.Models;
using SimpleSoundboard.Core.Services;
using System.Text.Json;
using Xunit;

namespace SimpleSoundboard.Tests.Unit;

[Collection("ConfigFileAccess")]
public class ConfigurationTests
{
    [Fact]
    public async Task ConfigurationSerialization_ComplexObject_ShouldPreserveData()
    {
        var config = new AppConfiguration
        {
            SelectedInputDeviceId = "input-device-123",
            SelectedOutputDeviceId = "output-device-456",
            BufferSize = 480,
            SampleRate = 48000,
            MasterVolume = 0.85f,
            MicrophoneVolume = 0.75f,
            UseExclusiveMode = true,
            SoundItems = new List<SoundItem>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "Test Sound",
                    FilePath = "C:\\test.wav",
                    Volume = 0.9f,
                    Hotkey = new HotkeyBinding
                    {
                        Modifiers = ModifierKeys.Control,
                        VirtualKeyCode = 0x41
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserialized = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        deserialized.Should().NotBeNull();
        deserialized!.SelectedInputDeviceId.Should().Be(config.SelectedInputDeviceId);
        deserialized.BufferSize.Should().Be(config.BufferSize);
        deserialized.SoundItems.Should().HaveCount(1);
    }

    [Fact]
    public void DefaultConfiguration_ShouldHaveValidValues()
    {
        var config = AppConfiguration.GetDefault();

        config.Should().NotBeNull();
        config.BufferSize.Should().BeGreaterThan(0);
        config.SampleRate.Should().BeGreaterThan(0);
        config.MasterVolume.Should().BeInRange(0f, 1f);
        config.MicrophoneVolume.Should().BeInRange(0f, 1f);
        config.SoundItems.Should().NotBeNull();
    }

    [Fact(Skip = "Requires test isolation - shared config file")]
    public async Task ConfigurationService_SaveAndLoad_ShouldRoundtrip()
    {
        var service = new ConfigurationService();
        var config = new AppConfiguration
        {
            SelectedInputDeviceId = "test-input",
            SelectedOutputDeviceId = "test-output",
            BufferSize = 960,
            SampleRate = 44100
        };

        var saved = await service.SaveConfigurationAsync(config);
        saved.Should().BeTrue();

        var loaded = await service.LoadConfigurationAsync();
        loaded.Should().NotBeNull();
        loaded.SelectedInputDeviceId.Should().Be(config.SelectedInputDeviceId);
        loaded.BufferSize.Should().Be(config.BufferSize);
    }

    [Fact]
    public async Task ConfigurationService_LoadNonExistent_ShouldReturnDefault()
    {
        var service = new ConfigurationService();
        var configPath = service.GetConfigFilePath();

        if (File.Exists(configPath))
        {
            File.Delete(configPath);
        }

        var loaded = await service.LoadConfigurationAsync();

        loaded.Should().NotBeNull();
        loaded.Should().BeEquivalentTo(AppConfiguration.GetDefault());
    }

    [Fact]
    public void ConfigurationValidation_InvalidBufferSize_ShouldBeDetectable()
    {
        var config = new AppConfiguration
        {
            BufferSize = -100
        };

        config.BufferSize.Should().BeLessThan(0);
    }

    [Fact]
    public void ConfigurationValidation_InvalidVolume_ShouldBeDetectable()
    {
        var config = new AppConfiguration
        {
            MasterVolume = 1.5f
        };

        config.MasterVolume.Should().BeGreaterThan(1.0f);
    }

    [Fact]
    public void Configuration_EmptyDeviceIds_ShouldBeAllowed()
    {
        var config = new AppConfiguration
        {
            SelectedInputDeviceId = string.Empty,
            SelectedOutputDeviceId = string.Empty
        };

        config.SelectedInputDeviceId.Should().BeEmpty();
        config.SelectedOutputDeviceId.Should().BeEmpty();
    }

    [Fact]
    public void Configuration_EmptySoundItems_ShouldBeValid()
    {
        var config = new AppConfiguration
        {
            SoundItems = new List<SoundItem>()
        };

        config.SoundItems.Should().BeEmpty();
        config.SoundItems.Should().NotBeNull();
    }

    [Fact]
    public async Task ConfigurationSerialization_NullValues_ShouldHandle()
    {
        var config = new AppConfiguration
        {
            SelectedInputDeviceId = string.Empty,
            SelectedOutputDeviceId = string.Empty,
            SoundItems = new List<SoundItem>
            {
                new()
                {
                    Name = "Test",
                    FilePath = "C:\\test.wav",
                    Hotkey = null
                }
            }
        };

        var json = JsonSerializer.Serialize(config);
        var deserialized = JsonSerializer.Deserialize<AppConfiguration>(json);

        deserialized.Should().NotBeNull();
        deserialized!.SoundItems[0].Hotkey.Should().BeNull();
    }

    [Fact]
    public void Configuration_AllProperties_ShouldBeSettable()
    {
        var config = new AppConfiguration
        {
            SelectedInputDeviceId = "input",
            SelectedOutputDeviceId = "output",
            BufferSize = 512,
            SampleRate = 96000,
            MasterVolume = 0.5f,
            MicrophoneVolume = 0.6f,
            UseExclusiveMode = false,
            EnableVirtualAudioDriver = true
        };

        config.SelectedInputDeviceId.Should().Be("input");
        config.SelectedOutputDeviceId.Should().Be("output");
        config.BufferSize.Should().Be(512);
        config.SampleRate.Should().Be(96000);
        config.MasterVolume.Should().Be(0.5f);
        config.MicrophoneVolume.Should().Be(0.6f);
        config.UseExclusiveMode.Should().BeFalse();
        config.EnableVirtualAudioDriver.Should().BeTrue();
    }

    [Fact(Skip = "Requires test isolation - shared config file")]
    public async Task ConfigurationService_MultipleWrites_ShouldOverwrite()
    {
        var service = new ConfigurationService();
        
        var config1 = new AppConfiguration { BufferSize = 480 };
        await service.SaveConfigurationAsync(config1);

        var config2 = new AppConfiguration { BufferSize = 960 };
        await service.SaveConfigurationAsync(config2);

        var loaded = await service.LoadConfigurationAsync();
        loaded.BufferSize.Should().Be(960);
    }
}
