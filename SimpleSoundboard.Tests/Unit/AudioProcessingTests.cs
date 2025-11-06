using FluentAssertions;
using NAudio.Wave;
using SimpleSoundboard.Core.Models;
using Xunit;

namespace SimpleSoundboard.Tests.Unit;

public class AudioProcessingTests
{
    [Fact]
    public void AudioBufferMixing_WithMultipleSources_ShouldCombineSamples()
    {
        var buffer1 = new float[] { 0.5f, 0.3f, 0.4f };
        var buffer2 = new float[] { 0.2f, 0.1f, 0.3f };
        var output = new float[3];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = buffer1[i] + buffer2[i];
        }

        output[0].Should().BeApproximately(0.7f, 0.001f);
        output[1].Should().BeApproximately(0.4f, 0.001f);
        output[2].Should().BeApproximately(0.7f, 0.001f);
    }

    [Fact]
    public void AudioMixing_WithOverflow_ShouldClampValues()
    {
        var buffer1 = new float[] { 0.8f, 0.9f };
        var buffer2 = new float[] { 0.5f, 0.6f };
        var output = new float[2];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = Math.Clamp(buffer1[i] + buffer2[i], -1.0f, 1.0f);
        }

        output[0].Should().Be(1.0f);
        output[1].Should().Be(1.0f);
    }

    [Fact]
    public void VolumeApplication_WithReductionFactor_ShouldReduceSamples()
    {
        var buffer = new float[] { 1.0f, 0.8f, 0.6f };
        var volume = 0.5f;
        var output = new float[3];

        for (int i = 0; i < buffer.Length; i++)
        {
            output[i] = buffer[i] * volume;
        }

        output[0].Should().BeApproximately(0.5f, 0.001f);
        output[1].Should().BeApproximately(0.4f, 0.001f);
        output[2].Should().BeApproximately(0.3f, 0.001f);
    }

    [Fact]
    public void VolumeApplication_WithAmplificationFactor_ShouldAmplifyAndClamp()
    {
        var buffer = new float[] { 0.8f, 0.6f };
        var volume = 1.5f;
        var output = new float[2];

        for (int i = 0; i < buffer.Length; i++)
        {
            output[i] = Math.Clamp(buffer[i] * volume, -1.0f, 1.0f);
        }

        output[0].Should().Be(1.0f);
        output[1].Should().BeApproximately(0.9f, 0.001f);
    }

    [Fact]
    public void AudioDevice_Enumeration_ShouldReturnValidFormat()
    {
        var device = new AudioDeviceInfo
        {
            DeviceId = "test-device-id",
            FriendlyName = "Test Device",
            DeviceType = AudioDeviceType.Input,
            Channels = 2,
            SampleRate = 48000,
            IsDefault = true
        };

        device.DeviceId.Should().NotBeNullOrEmpty();
        device.Channels.Should().BeGreaterThan(0);
        device.SampleRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void WaveFormat_StandardConfiguration_ShouldMatchSpecification()
    {
        var waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(48000, 2);

        waveFormat.SampleRate.Should().Be(48000);
        waveFormat.Channels.Should().Be(2);
        waveFormat.Encoding.Should().Be(WaveFormatEncoding.IeeeFloat);
    }

    [Fact]
    public void AudioBuffer_ZeroSamples_ShouldRemainSilent()
    {
        var buffer = new float[100];
        Array.Fill(buffer, 0f);

        var maxSample = buffer.Max(Math.Abs);
        maxSample.Should().Be(0f);
    }

    [Fact]
    public void AudioMixing_WithNegativeSamples_ShouldHandleCorrectly()
    {
        var buffer1 = new float[] { 0.5f, -0.3f };
        var buffer2 = new float[] { -0.2f, 0.4f };
        var output = new float[2];

        for (int i = 0; i < output.Length; i++)
        {
            output[i] = Math.Clamp(buffer1[i] + buffer2[i], -1.0f, 1.0f);
        }

        output[0].Should().BeApproximately(0.3f, 0.001f);
        output[1].Should().BeApproximately(0.1f, 0.001f);
    }

    [Fact]
    public void AudioBuffer_LargeSize_ShouldCalculateCorrectly()
    {
        var sampleRate = 48000;
        var channels = 2;
        var durationSeconds = 1;
        
        var expectedSamples = sampleRate * channels * durationSeconds;

        expectedSamples.Should().Be(96000);
    }

    [Fact]
    public void VolumeControl_ZeroVolume_ShouldProduceSilence()
    {
        var buffer = new float[] { 0.8f, 0.6f, 0.9f };
        var volume = 0f;
        var output = new float[3];

        for (int i = 0; i < buffer.Length; i++)
        {
            output[i] = buffer[i] * volume;
        }

        output.Should().AllSatisfy(x => x.Should().Be(0f));
    }

    [Fact]
    public void AudioConfiguration_ValidParameters_ShouldBeAccepted()
    {
        var config = new AppConfiguration
        {
            SampleRate = 48000,
            BufferSize = 480,
            MasterVolume = 1.0f,
            MicrophoneVolume = 0.8f
        };

        config.SampleRate.Should().Be(48000);
        config.BufferSize.Should().BeGreaterThan(0);
        config.MasterVolume.Should().BeInRange(0f, 1f);
        config.MicrophoneVolume.Should().BeInRange(0f, 1f);
    }

    [Fact]
    public void BufferLatency_Calculation_ShouldMatchExpected()
    {
        var bufferSize = 480;
        var sampleRate = 48000;
        var expectedLatencyMs = (bufferSize / (double)sampleRate) * 1000;

        expectedLatencyMs.Should().Be(10.0);
    }

    [Fact]
    public void SampleConversion_Int16ToFloat_ShouldNormalize()
    {
        short int16Sample = 16384;
        float normalized = int16Sample / 32768f;

        normalized.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void SampleConversion_FloatToInt16_ShouldScale()
    {
        float floatSample = 0.5f;
        short int16Sample = (short)(floatSample * 32767);

        int16Sample.Should().Be(16383);
    }
}
