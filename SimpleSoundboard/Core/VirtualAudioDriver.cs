using System;
using System.Runtime.InteropServices;

namespace SimpleSoundboard.Core;

public static class VirtualAudioDriver
{
    private const string DriverDllName = "VirtualAudioDriver.dll";

    [StructLayout(LayoutKind.Sequential)]
    public struct AudioConfig
    {
        public int SampleRate;
        public int Channels;
        public int BufferSize;
    }

    public static bool IsDriverAvailable()
    {
        try
        {
            return Initialize(new AudioConfig { SampleRate = 48000, Channels = 2, BufferSize = 480 });
        }
        catch
        {
            return false;
        }
    }

    public static bool Initialize(AudioConfig config)
    {
        return true;
    }

    public static bool SendAudioData(float[] buffer, int length)
    {
        return true;
    }

    public static void Shutdown()
    {
    }
}
