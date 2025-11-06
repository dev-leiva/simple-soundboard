using SimpleSoundboard.WPF.Core;
using SimpleSoundboard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleSoundboard.WPF.Services;

public class HotkeyManager : IDisposable
{
    private readonly Dictionary<int, Guid> _hotkeyToSoundMap = new();
    private readonly Dictionary<Guid, int> _soundToHotkeyMap = new();
    private IntPtr _windowHandle;
    private int _nextHotkeyId = 1;

    public event EventHandler<Guid>? HotkeyPressed;
    public event EventHandler<string>? ErrorOccurred;

    public void Initialize(IntPtr windowHandle)
    {
        _windowHandle = windowHandle;
    }

    public bool RegisterHotkey(Guid soundId, HotkeyBinding hotkey)
    {
        try
        {
            if (_soundToHotkeyMap.ContainsKey(soundId))
            {
                UnregisterHotkey(soundId);
            }

            if (_hotkeyToSoundMap.Values.Contains(soundId))
            {
                ErrorOccurred?.Invoke(this, "Hotkey already registered for another sound");
                return false;
            }

            var hotkeyId = _nextHotkeyId++;
            var success = Win32Interop.RegisterHotKey(
                _windowHandle,
                hotkeyId,
                (uint)hotkey.Modifiers,
                hotkey.VirtualKeyCode
            );

            if (success)
            {
                _hotkeyToSoundMap[hotkeyId] = soundId;
                _soundToHotkeyMap[soundId] = hotkeyId;
                hotkey.Id = hotkeyId;
                hotkey.IsRegistered = true;
                return true;
            }
            else
            {
                var errorCode = Win32Interop.GetLastError();
                ErrorOccurred?.Invoke(this, $"Failed to register hotkey. Error code: {errorCode}. Hotkey might already be in use.");
                return false;
            }
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Exception registering hotkey: {ex.Message}");
            return false;
        }
    }

    public bool UnregisterHotkey(Guid soundId)
    {
        try
        {
            if (_soundToHotkeyMap.TryGetValue(soundId, out var hotkeyId))
            {
                var success = Win32Interop.UnregisterHotKey(_windowHandle, hotkeyId);
                
                if (success)
                {
                    _hotkeyToSoundMap.Remove(hotkeyId);
                    _soundToHotkeyMap.Remove(soundId);
                }

                return success;
            }

            return false;
        }
        catch (Exception ex)
        {
            ErrorOccurred?.Invoke(this, $"Exception unregistering hotkey: {ex.Message}");
            return false;
        }
    }

    public void ProcessHotkeyMessage(int hotkeyId)
    {
        if (_hotkeyToSoundMap.TryGetValue(hotkeyId, out var soundId))
        {
            HotkeyPressed?.Invoke(this, soundId);
        }
    }

    public void UnregisterAll()
    {
        var soundIds = _soundToHotkeyMap.Keys.ToList();
        foreach (var soundId in soundIds)
        {
            UnregisterHotkey(soundId);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
    }
}
