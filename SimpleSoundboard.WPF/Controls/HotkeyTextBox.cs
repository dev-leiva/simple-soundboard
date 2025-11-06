using SimpleSoundboard.Core.Models;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace SimpleSoundboard.WPF.Controls;

public class HotkeyTextBox : TextBox
{
    public static readonly DependencyProperty HotkeyProperty =
        DependencyProperty.Register(
            nameof(Hotkey),
            typeof(HotkeyBinding),
            typeof(HotkeyTextBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHotkeyChanged));

    public HotkeyBinding? Hotkey
    {
        get => (HotkeyBinding?)GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    private bool _isCapturing;

    public HotkeyTextBox()
    {
        IsReadOnly = true;
        Cursor = Cursors.Hand;
        UpdateDisplayText();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        e.Handled = true;

        if (!_isCapturing)
        {
            _isCapturing = true;
            Text = "Press combination of keys...";
            return;
        }

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        // Ignore modifier keys by themselves
        if (key == Key.LeftCtrl || key == Key.RightCtrl ||
            key == Key.LeftAlt || key == Key.RightAlt ||
            key == Key.LeftShift || key == Key.RightShift ||
            key == Key.LWin || key == Key.RWin)
        {
            return;
        }

        // Capture the hotkey
        var modifiers = SimpleSoundboard.Core.Models.ModifierKeys.None;
        if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            modifiers |= SimpleSoundboard.Core.Models.ModifierKeys.Control;
        if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
            modifiers |= SimpleSoundboard.Core.Models.ModifierKeys.Alt;
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
            modifiers |= SimpleSoundboard.Core.Models.ModifierKeys.Shift;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
            modifiers |= SimpleSoundboard.Core.Models.ModifierKeys.Win;

        // Convert WPF Key to Virtual Key Code
        var vk = (uint)KeyInterop.VirtualKeyFromKey(key);

        Hotkey = new HotkeyBinding
        {
            Modifiers = modifiers,
            VirtualKeyCode = vk
        };

        _isCapturing = false;
        UpdateDisplayText();
    }

    protected override void OnGotFocus(RoutedEventArgs e)
    {
        base.OnGotFocus(e);
        _isCapturing = false;
        Text = "Press combination of keys...";
    }

    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        _isCapturing = false;
        UpdateDisplayText();
    }

    private static void OnHotkeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HotkeyTextBox textBox)
        {
            textBox.UpdateDisplayText();
        }
    }

    private void UpdateDisplayText()
    {
        if (Hotkey != null)
        {
            Text = Hotkey.GetDisplayString();
        }
        else
        {
            Text = "Click to set hotkey";
        }
    }
}
