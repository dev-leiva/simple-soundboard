using Microsoft.UI.Xaml;
using SimpleSoundboard.Views;
using System;

namespace SimpleSoundboard;

public partial class App : Application
{
    private Window? m_window;

    public App()
    {
        InitializeComponent();
        
        // Register global unhandled exception handler
        UnhandledException += OnUnhandledException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            m_window = new MainWindow();
            m_window.Activate();
        }
        catch (Exception ex)
        {
            // Last resort: show error in a simple window
            ShowErrorWindow(ex);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        e.Handled = true;
        System.Diagnostics.Debug.WriteLine($"[UNHANDLED EXCEPTION] {e.Exception}");
        
        // Try to show error to user
        try
        {
            if (m_window == null)
            {
                ShowErrorWindow(e.Exception);
            }
        }
        catch
        {
            // Can't show UI, just log
        }
    }

    private void ShowErrorWindow(Exception ex)
    {
        try
        {
            var errorWindow = new Window
            {
                Title = "Simple Soundboard - Error"
            };
            
            var errorText = new Microsoft.UI.Xaml.Controls.TextBlock
            {
                Text = $"Fatal Error:\n\n{ex.Message}\n\nType: {ex.GetType().Name}\n\nStack Trace:\n{ex.StackTrace}",
                TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap,
                Margin = new Microsoft.UI.Xaml.Thickness(20)
            };
            
            var scrollViewer = new Microsoft.UI.Xaml.Controls.ScrollViewer
            {
                Content = errorText
            };
            
            errorWindow.Content = scrollViewer;
            errorWindow.Activate();
            m_window = errorWindow;
        }
        catch
        {
            // Failed to show error window - nothing we can do
        }
    }
}
