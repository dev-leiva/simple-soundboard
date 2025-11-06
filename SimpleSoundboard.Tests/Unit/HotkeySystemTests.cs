using FluentAssertions;
using SimpleSoundboard.Core.Models;
using Xunit;

namespace SimpleSoundboard.Tests.Unit;

public class HotkeySystemTests
{
    [Fact]
    public void HotkeyString_WithMultipleModifiers_ShouldFormatCorrectly()
    {
        var hotkey = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control | ModifierKeys.Shift,
            VirtualKeyCode = 0x41
        };

        var displayString = hotkey.GetDisplayString();

        displayString.Should().Contain("Ctrl");
        displayString.Should().Contain("Shift");
        displayString.Should().Contain("A");
    }

    [Fact]
    public void HotkeyString_WithSingleModifier_ShouldFormatCorrectly()
    {
        var hotkey = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Alt,
            VirtualKeyCode = 0x70
        };

        var displayString = hotkey.GetDisplayString();

        displayString.Should().Contain("Alt");
        displayString.Should().Contain("F1");
    }

    [Fact]
    public void HotkeyString_FunctionKeys_ShouldFormatCorrectly()
    {
        var hotkeyF1 = new HotkeyBinding { VirtualKeyCode = 0x70 };
        var hotkeyF12 = new HotkeyBinding { VirtualKeyCode = 0x7B };

        hotkeyF1.GetDisplayString().Should().Contain("F1");
        hotkeyF12.GetDisplayString().Should().Contain("F12");
    }

    [Fact]
    public void HotkeyString_NumberKeys_ShouldFormatCorrectly()
    {
        var hotkey = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x31
        };

        var displayString = hotkey.GetDisplayString();

        displayString.Should().Contain("1");
    }

    [Fact]
    public void HotkeyIdentifier_SameConfiguration_ShouldBeConsistent()
    {
        var hotkey1 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
            VirtualKeyCode = 0x42
        };

        var hotkey2 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control | ModifierKeys.Alt,
            VirtualKeyCode = 0x42
        };

        hotkey1.GetHashCode().Should().Be(hotkey2.GetHashCode());
        hotkey1.Equals(hotkey2).Should().BeTrue();
    }

    [Fact]
    public void HotkeyIdentifier_DifferentModifiers_ShouldBeDifferent()
    {
        var hotkey1 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x41
        };

        var hotkey2 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Alt,
            VirtualKeyCode = 0x41
        };

        hotkey1.Equals(hotkey2).Should().BeFalse();
    }

    [Fact]
    public void HotkeyIdentifier_DifferentKeys_ShouldBeDifferent()
    {
        var hotkey1 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x41
        };

        var hotkey2 = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control,
            VirtualKeyCode = 0x42
        };

        hotkey1.Equals(hotkey2).Should().BeFalse();
    }

    [Fact]
    public void ModifierKeys_BitwiseOperations_ShouldCombineCorrectly()
    {
        var modifiers = ModifierKeys.Control | ModifierKeys.Shift | ModifierKeys.Alt;

        modifiers.HasFlag(ModifierKeys.Control).Should().BeTrue();
        modifiers.HasFlag(ModifierKeys.Shift).Should().BeTrue();
        modifiers.HasFlag(ModifierKeys.Alt).Should().BeTrue();
        modifiers.HasFlag(ModifierKeys.Win).Should().BeFalse();
    }

    [Fact]
    public void ModifierKeys_None_ShouldHaveNoFlags()
    {
        var modifiers = ModifierKeys.None;

        modifiers.HasFlag(ModifierKeys.Control).Should().BeFalse();
        modifiers.HasFlag(ModifierKeys.Shift).Should().BeFalse();
        modifiers.HasFlag(ModifierKeys.Alt).Should().BeFalse();
        modifiers.HasFlag(ModifierKeys.Win).Should().BeFalse();
    }

    [Fact(Skip = "Requires Windows-specific HotkeyManager")]
    public void HotkeyManager_ProcessMessage_ShouldTriggerCorrectSound()
    {
        Assert.True(true);
    }

    [Fact]
    public void HotkeyBinding_AllModifiers_ShouldDisplayCorrectly()
    {
        var hotkey = new HotkeyBinding
        {
            Modifiers = ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift | ModifierKeys.Win,
            VirtualKeyCode = 0x41
        };

        var displayString = hotkey.GetDisplayString();

        displayString.Should().Contain("Ctrl");
        displayString.Should().Contain("Alt");
        displayString.Should().Contain("Shift");
        displayString.Should().Contain("Win");
        displayString.Should().Contain("A");
    }

    [Fact]
    public void HotkeyBinding_SpecialKeys_ShouldHandleCorrectly()
    {
        var spaceKey = new HotkeyBinding { VirtualKeyCode = 0x20 };
        var enterKey = new HotkeyBinding { VirtualKeyCode = 0x0D };
        var escKey = new HotkeyBinding { VirtualKeyCode = 0x1B };

        spaceKey.GetDisplayString().Should().Contain("Space");
        enterKey.GetDisplayString().Should().Contain("Enter");
        escKey.GetDisplayString().Should().Contain("Esc");
    }

    [Fact(Skip = "Requires Windows-specific HotkeyManager")]
    public void HotkeyManager_Dispose_ShouldUnregisterAll()
    {
        Assert.True(true);
    }
}
