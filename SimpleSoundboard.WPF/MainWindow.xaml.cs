using SimpleSoundboard.WPF.ViewModels;
using SimpleSoundboard.WPF.Core;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace SimpleSoundboard.WPF;

public partial class MainWindow : Window
{
    public MainViewModel? ViewModel { get; private set; }
    private IntPtr _hwnd;
    private HwndSource? _hwndSource;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            
            Loaded += OnLoaded;
            Closed += OnClosed;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] MainWindow initialization failed: {ex}");
            MessageBox.Show(
                $"Failed to initialize application:\n\n{ex.Message}",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Get window handle
            _hwnd = new WindowInteropHelper(this).Handle;
            
            // Hook into window messages
            _hwndSource = HwndSource.FromHwnd(_hwnd);
            if (_hwndSource != null)
            {
                _hwndSource.AddHook(WndProc);
            }
            
            // Initialize hotkeys
            ViewModel?.InitializeHotkeys(_hwnd);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ERROR] OnLoaded failed: {ex}");
            MessageBox.Show(
                $"Failed to initialize hotkeys:\n\n{ex.Message}",
                "Hotkey Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource?.Dispose();
        ViewModel?.Dispose();
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32Interop.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            ViewModel?.ProcessHotkey(hotkeyId);
            handled = true;
        }

        return IntPtr.Zero;
    }
}
