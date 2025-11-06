namespace SimpleSoundboard.Core.Utilities;

public static class AudioUtilities
{
    public static void MixAudioBuffers(float[] buffer1, float[] buffer2, float[] output)
    {
        var length = Math.Min(Math.Min(buffer1.Length, buffer2.Length), output.Length);
        
        for (int i = 0; i < length; i++)
        {
            output[i] = Math.Clamp(buffer1[i] + buffer2[i], -1.0f, 1.0f);
        }
    }

    public static void ApplyVolume(float[] buffer, float volume)
    {
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Math.Clamp(buffer[i] * volume, -1.0f, 1.0f);
        }
    }

    public static double CalculateLatencyMs(int bufferSize, int sampleRate)
    {
        return (bufferSize / (double)sampleRate) * 1000.0;
    }

    public static float NormalizeSampleInt16ToFloat(short sample)
    {
        return sample / 32768f;
    }

    public static short NormalizeSampleFloatToInt16(float sample)
    {
        return (short)(Math.Clamp(sample, -1.0f, 1.0f) * 32767);
    }

    public static int CalculateBufferSizeInSamples(int sampleRate, int channels, double durationSeconds)
    {
        return (int)(sampleRate * channels * durationSeconds);
    }
}
