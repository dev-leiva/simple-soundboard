using Xunit;
using SimpleSoundboard.WPF.Services;
using SimpleSoundboard.Core.Models;
using System;
using System.Threading;
using FluentAssertions;

namespace SimpleSoundboard.Tests.Services;

public class AudioEngineMonitoringTests : IDisposable
{
    private AudioEngine _audioEngine;

    public AudioEngineMonitoringTests()
    {
        _audioEngine = new AudioEngine();
    }

    public void Dispose()
    {
        _audioEngine?.Dispose();
    }

    [Fact]
    public void MonitoringEnabled_DefaultsToTrue()
    {
        // Arrange & Act
        var audioEngine = new AudioEngine();

        // Assert
        audioEngine.MonitoringEnabled.Should().BeTrue();
    }

    [Fact]
    public void MonitoringEnabled_CanBeSetToFalse()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act
        audioEngine.MonitoringEnabled = false;

        // Assert
        audioEngine.MonitoringEnabled.Should().BeFalse();
    }

    [Fact]
    public void MonitoringEnabled_CanBeToggled()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act & Assert
        audioEngine.MonitoringEnabled.Should().BeTrue();
        
        audioEngine.MonitoringEnabled = false;
        audioEngine.MonitoringEnabled.Should().BeFalse();
        
        audioEngine.MonitoringEnabled = true;
        audioEngine.MonitoringEnabled.Should().BeTrue();
    }

    [Fact]
    public void SetBufferSize_UpdatesCurrentBufferSize()
    {
        // Arrange
        var audioEngine = new AudioEngine();
        var expectedBufferSize = 480;

        // Act
        audioEngine.SetBufferSize(expectedBufferSize);

        // Assert
        audioEngine.CurrentBufferSize.Should().Be(expectedBufferSize);
    }

    [Fact]
    public void CurrentLatencyMs_InitiallyZero()
    {
        // Arrange & Act
        var audioEngine = new AudioEngine();

        // Assert
        audioEngine.CurrentLatencyMs.Should().Be(0f);
    }

    [Fact]
    public void IsRunning_InitiallyFalse()
    {
        // Arrange & Act
        var audioEngine = new AudioEngine();

        // Assert
        audioEngine.IsRunning.Should().BeFalse();
    }

    [Theory]
    [InlineData(144)]
    [InlineData(240)]
    [InlineData(480)]
    [InlineData(960)]
    public void SetBufferSize_AcceptsValidBufferSizes(int bufferSize)
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act
        audioEngine.SetBufferSize(bufferSize);

        // Assert
        audioEngine.CurrentBufferSize.Should().Be(bufferSize);
    }

    [Fact]
    public void GetAudioDevices_ReturnsOutputDevices()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act
        var outputDevices = audioEngine.GetAudioDevices(AudioDeviceType.Output);

        // Assert
        outputDevices.Should().NotBeNull();
        // Note: Actual device count depends on system configuration
        // In CI environments, there might be 0 devices
    }

    [Fact]
    public void GetAudioDevices_ReturnsInputDevices()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act
        var inputDevices = audioEngine.GetAudioDevices(AudioDeviceType.Input);

        // Assert
        inputDevices.Should().NotBeNull();
        // Note: Actual device count depends on system configuration
    }

    [Fact]
    public void GetAudioDevices_OutputDevices_HaveValidProperties()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act
        var outputDevices = audioEngine.GetAudioDevices(AudioDeviceType.Output);

        // Assert
        foreach (var device in outputDevices)
        {
            device.DeviceId.Should().NotBeNullOrWhiteSpace();
            device.FriendlyName.Should().NotBeNullOrWhiteSpace();
            device.DeviceType.Should().Be(AudioDeviceType.Output);
            device.SampleRate.Should().BeGreaterThan(0);
            device.Channels.Should().BeGreaterThan(0);
        }
    }

    [Fact]
    public void AudioEngine_CanBeDisposedMultipleTimes()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act & Assert - Should not throw
        audioEngine.Dispose();
        audioEngine.Dispose();
    }

    [Fact]
    public void Stop_WhenNotRunning_DoesNotThrow()
    {
        // Arrange
        var audioEngine = new AudioEngine();

        // Act & Assert - Should not throw
        var act = () => audioEngine.Stop();
        act.Should().NotThrow();
    }
}
