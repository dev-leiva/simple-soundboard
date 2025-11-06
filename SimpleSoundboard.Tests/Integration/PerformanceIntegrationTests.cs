using FluentAssertions;
using SimpleSoundboard.Core.Models;
using SimpleSoundboard.Core.Services;
using System.Diagnostics;
using Xunit;

namespace SimpleSoundboard.Tests.Integration;

[Collection("ConfigFileAccess")]
public class PerformanceIntegrationTests
{
    [Fact]
    public void LatencyCalculation_WithStandardBuffer_ShouldBeWithinThreshold()
    {
        var bufferSize = 480;
        var sampleRate = 48000;
        var latencyMs = (bufferSize / (double)sampleRate) * 1000;

        latencyMs.Should().BeInRange(5, 20);
    }

    [Fact]
    public void AudioMixing_Performance_ShouldCompleteQuickly()
    {
        var buffer1 = new float[96000];
        var buffer2 = new float[96000];
        var output = new float[96000];

        Array.Fill(buffer1, 0.5f);
        Array.Fill(buffer2, 0.3f);

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = Math.Clamp(buffer1[i] + buffer2[i], -1.0f, 1.0f);
        }

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
    }

    [Fact]
    public void VolumeApplication_Performance_ShouldCompleteQuickly()
    {
        var buffer = new float[96000];
        Array.Fill(buffer, 0.8f);
        var volume = 0.5f;

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] *= volume;
        }

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20);
    }

    [Fact]
    public async Task ConfigurationSerialization_Performance_ShouldBeReasonable()
    {
        var service = new ConfigurationService();
        var config = new AppConfiguration
        {
            SoundItems = Enumerable.Range(0, 100).Select(i => new SoundItem
            {
                Id = Guid.NewGuid(),
                Name = $"Sound {i}",
                FilePath = $"C:\\sounds\\sound{i}.wav",
                Volume = 0.8f
            }).ToList()
        };

        try
        {
            var stopwatch = Stopwatch.StartNew();
            await service.SaveConfigurationAsync(config);
            await service.LoadConfigurationAsync();
            stopwatch.Stop();

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
        }
        finally
        {
            var configPath = service.GetConfigFilePath();
            if (File.Exists(configPath))
            {
                File.Delete(configPath);
            }
        }
    }

    [Fact]
    public void HotkeyHashCode_Performance_ShouldBeInstant()
    {
        var hotkeys = Enumerable.Range(0, 1000).Select(i => new HotkeyBinding
        {
            Modifiers = (ModifierKeys)(i % 4),
            VirtualKeyCode = (uint)(0x41 + (i % 26))
        }).ToList();

        var stopwatch = Stopwatch.StartNew();

        var hashCodes = hotkeys.Select(h => h.GetHashCode()).ToList();

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(10);
        hashCodes.Should().HaveCount(1000);
    }

    [Fact]
    public void SoundValidation_Performance_WithManyItems_ShouldBeQuick()
    {
        var sounds = Enumerable.Range(0, 1000).Select(i => new SoundItem
        {
            Name = $"Sound {i}",
            FilePath = "C:\\test.wav",
            Volume = 0.8f
        }).ToList();

        var stopwatch = Stopwatch.StartNew();

        var validCounts = sounds.Count(s => s.IsValid());

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void LargeAudioBuffer_Allocation_ShouldSucceed()
    {
        var bufferSize = 48000 * 10;

        var stopwatch = Stopwatch.StartNew();
        var buffer = new float[bufferSize];
        Array.Fill(buffer, 0.5f);
        stopwatch.Stop();

        buffer.Should().HaveCount(bufferSize);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(100);
    }

    [Fact]
    public void HotkeyDisplayString_Generation_ShouldBeEfficient()
    {
        var hotkeys = Enumerable.Range(0, 100).Select(i => new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
            VirtualKeyCode = (uint)(0x41 + (i % 26))
        }).ToList();

        var stopwatch = Stopwatch.StartNew();

        var displayStrings = hotkeys.Select(h => h.GetDisplayString()).ToList();

        stopwatch.Stop();

        stopwatch.ElapsedMilliseconds.Should().BeLessThan(50);
        displayStrings.Should().HaveCount(100);
    }

    [Fact]
    public void ConcurrentSoundLoading_ShouldHandleMultipleRequests()
    {
        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await Task.Delay(10);
            return new SoundItem
            {
                Id = Guid.NewGuid(),
                Name = $"Sound {i}",
                FilePath = $"C:\\sounds\\sound{i}.wav"
            };
        });

        var stopwatch = Stopwatch.StartNew();
        var results = Task.WhenAll(tasks).Result;
        stopwatch.Stop();

        results.Should().HaveCount(10);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    [Fact(Skip = "Memory intensive test")]
    public void MemoryUsage_ExtendedOperation_ShouldBeStable()
    {
        var initialMemory = GC.GetTotalMemory(true);

        for (int i = 0; i < 1000; i++)
        {
            var sound = new SoundItem
            {
                Id = Guid.NewGuid(),
                Name = $"Sound {i}",
                FilePath = "C:\\test.wav",
                Volume = 0.8f
            };

            _ = sound.IsValid();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryGrowth = finalMemory - initialMemory;

        memoryGrowth.Should().BeLessThan(10 * 1024 * 1024);
    }
}
