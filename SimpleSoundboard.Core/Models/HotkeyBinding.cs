namespace SimpleSoundboard.Core.Models;

[Flags]
public enum ModifierKeys : uint
{
    None = 0x0000,
    Alt = 0x0001,
    Control = 0x0002,
    Shift = 0x0004,
    Win = 0x0008
}

public class HotkeyBinding
{
    public int Id { get; set; }
    public ModifierKeys Modifiers { get; set; }
    public uint VirtualKeyCode { get; set; }
    public bool IsRegistered { get; set; }

    public string GetDisplayString()
    {
        var parts = new List<string>();

        if (Modifiers.HasFlag(ModifierKeys.Control))
            parts.Add("Ctrl");
        if (Modifiers.HasFlag(ModifierKeys.Alt))
            parts.Add("Alt");
        if (Modifiers.HasFlag(ModifierKeys.Shift))
            parts.Add("Shift");
        if (Modifiers.HasFlag(ModifierKeys.Win))
            parts.Add("Win");

        parts.Add(GetKeyName(VirtualKeyCode));

        return string.Join(" + ", parts);
    }

    private static string GetKeyName(uint vkCode)
    {
        if (vkCode >= 0x30 && vkCode <= 0x39)
            return ((char)vkCode).ToString();
        if (vkCode >= 0x41 && vkCode <= 0x5A)
            return ((char)vkCode).ToString();
        if (vkCode >= 0x70 && vkCode <= 0x87)
            return $"F{vkCode - 0x70 + 1}";

        return vkCode switch
        {
            0x20 => "Space",
            0x0D => "Enter",
            0x1B => "Esc",
            0x09 => "Tab",
            0x08 => "Backspace",
            _ => $"Key{vkCode}"
        };
    }

    public override bool Equals(object? obj)
    {
        return obj is HotkeyBinding other &&
               Modifiers == other.Modifiers &&
               VirtualKeyCode == other.VirtualKeyCode;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Modifiers, VirtualKeyCode);
    }
}
