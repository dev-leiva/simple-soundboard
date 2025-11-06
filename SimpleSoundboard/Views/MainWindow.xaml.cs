using Microsoft.UI.Xaml;
using SimpleSoundboard.ViewModels;
using SimpleSoundboard.Core;
using System;
using System.Runtime.InteropServices;
using WinRT.Interop;

namespace SimpleSoundboard.Views;

public sealed partial class MainWindow : Window
{
    public MainViewModel? ViewModel { get; private set; }
    private IntPtr _hwnd;
    private WindowSubclass? _subclass;

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            ViewModel = new MainViewModel();
            
            _hwnd = WindowNative.GetWindowHandle(this);
            
            _subclass = new WindowSubclass(_hwnd, WndProc);
            
            ViewModel.InitializeHotkeys(_hwnd);
            
            Closed += OnClosed;
        }
        catch (Exception ex)
        {
            // Show error to user - create a fallback UI
            var errorText = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = $"Failed to initialize application:\n\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Margin = new Microsoft.UI.Xaml.Thickness(20)
            };
            
            var scrollViewer = new Microsoft.UI.Xaml.Controls.ScrollViewer
            {
                Content = errorText
            };
            
            Content = scrollViewer;
            
            // Log to debugger/console
            System.Diagnostics.Debug.WriteLine($"[CRITICAL ERROR] MainWindow initialization failed: {ex}");
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        _subclass?.Dispose();
        ViewModel?.Dispose();
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg == Win32Interop.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();
            ViewModel?.ProcessHotkey(hotkeyId);
        }

        return _subclass?.CallWindowProc(hWnd, msg, wParam, lParam) ?? IntPtr.Zero;
    }

    private class WindowSubclass : IDisposable
    {
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const int GWL_WNDPROC = -4;
        private readonly IntPtr _hwnd;
        private readonly IntPtr _oldWndProc;
        private readonly WndProcDelegate _newWndProc;
        private readonly Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> _callback;

        public WindowSubclass(IntPtr hwnd, Func<IntPtr, uint, IntPtr, IntPtr, IntPtr> callback)
        {
            _hwnd = hwnd;
            _callback = callback;
            _newWndProc = NewWndProc;
            
            var newWndProcPtr = Marshal.GetFunctionPointerForDelegate(_newWndProc);
            _oldWndProc = SetWindowLongPtr(hwnd, GWL_WNDPROC, newWndProcPtr);
        }

        private IntPtr NewWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return _callback(hWnd, msg, wParam, lParam);
        }

        public IntPtr CallWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            if (_oldWndProc != IntPtr.Zero)
            {
                SetWindowLongPtr(_hwnd, GWL_WNDPROC, _oldWndProc);
            }
        }
    }
}
