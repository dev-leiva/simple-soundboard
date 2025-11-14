using SimpleSoundboard.WPF.ViewModels;
using SimpleSoundboard.WPF.Core;
using SimpleSoundboard.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace SimpleSoundboard.WPF;

public partial class MainWindow : Window
{
    public MainViewModel? ViewModel { get; private set; }
    private IntPtr _hwnd;
    private HwndSource? _hwndSource;
    private Point _dragStartPoint;
    private SoundItem? _draggedItem;
    private List<Popup> _openVolumePopups = new();

    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            ViewModel = new MainViewModel();
            DataContext = ViewModel;
            
            Loaded += OnLoaded;
            Closing += OnClosing;
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

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (ViewModel != null)
        {
            // Stop audio if running (ensures clean shutdown)
            if (ViewModel.IsAudioRunning)
            {
                ViewModel.StopAudio();
            }
            
            // Check for unsaved changes
            var canClose = await ViewModel.PromptSaveChangesAsync();
            if (!canClose)
            {
                e.Cancel = true; // Cancel the close operation
            }
        }
    }
    
    private void OnClosed(object? sender, EventArgs e)
    {
        try
        {
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource?.Dispose();
            ViewModel?.Dispose();
        }
        finally
        {
            // Force application shutdown
            Application.Current.Shutdown();
        }
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
    
    private void VolumeButton_Click(object sender, RoutedEventArgs e)
    {
        // Close all other popups first
        CloseAllVolumePopups();
        
        // Find the popup in the same parent Grid
        if (sender is Button button)
        {
            var grid = button.Parent as Grid;
            if (grid != null)
            {
                // Find the Popup sibling in the Grid
                foreach (var child in grid.Children)
                {
                    if (child is Popup popup)
                    {
                        popup.IsOpen = true;
                        if (!_openVolumePopups.Contains(popup))
                        {
                            _openVolumePopups.Add(popup);
                        }
                        break;
                    }
                }
            }
        }
    }
    
    private void Window_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Check if click is outside any open volume popup
        var clickedElement = e.OriginalSource as DependencyObject;
        
        foreach (var popup in _openVolumePopups.ToList())
        {
            if (popup.IsOpen)
            {
                // Check if the click was inside the popup
                bool isInsidePopup = IsElementInsidePopup(clickedElement, popup);
                
                if (!isInsidePopup)
                {
                    popup.IsOpen = false;
                    _openVolumePopups.Remove(popup);
                }
            }
        }
    }
    
    private bool IsElementInsidePopup(DependencyObject? element, Popup popup)
    {
        while (element != null)
        {
            if (element == popup || element == popup.Child)
                return true;
            
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }
    
    private void CloseAllVolumePopups()
    {
        foreach (var popup in _openVolumePopups.ToList())
        {
            popup.IsOpen = false;
        }
        _openVolumePopups.Clear();
    }
    
    private void SoundItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Don't start dragging if clicking on interactive controls (buttons, sliders, textboxes)
        var originalSource = e.OriginalSource as DependencyObject;
        if (IsInteractiveControl(originalSource))
        {
            _draggedItem = null;
            return;
        }
        
        _dragStartPoint = e.GetPosition(null);
        
        if (sender is FrameworkElement element && element.DataContext is SoundItem soundItem)
        {
            _draggedItem = soundItem;
        }
    }
    
    private void SoundItem_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && _draggedItem != null)
        {
            // Don't drag if over interactive controls
            var originalSource = e.OriginalSource as DependencyObject;
            if (IsInteractiveControl(originalSource))
            {
                return;
            }
            
            Point mousePos = e.GetPosition(null);
            Vector diff = _dragStartPoint - mousePos;

            if (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance)
            {
                var dragData = new DataObject("SoundItem", _draggedItem);
                DragDrop.DoDragDrop((DependencyObject)sender, dragData, DragDropEffects.Move);
            }
        }
    }
    
    private void SoundItem_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent("SoundItem"))
        {
            var sourceItem = e.Data.GetData("SoundItem") as SoundItem;
            if (sender is FrameworkElement element && element.DataContext is SoundItem targetItem)
            {
                if (sourceItem != null && sourceItem != targetItem && ViewModel != null)
                {
                    var targetIndex = ViewModel.SoundItems.IndexOf(targetItem);
                    ViewModel.MoveSoundItem(sourceItem, targetIndex);
                }
            }
        }
        _draggedItem = null;
    }
    
    private void SoundItem_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent("SoundItem"))
        {
            e.Effects = DragDropEffects.None;
        }
    }
    
    private void CloseVolumePopup_Click(object sender, RoutedEventArgs e)
    {
        // Find and close the popup
        if (sender is FrameworkElement button)
        {
            var popup = FindParent<Popup>(button);
            if (popup != null)
            {
                popup.IsOpen = false;
                _openVolumePopups.Remove(popup);
            }
        }
    }
    
    private void CloseVolumePopup_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Mark event as handled to prevent it from bubbling
        e.Handled = true;
        
        // Find and close the popup
        if (sender is FrameworkElement button)
        {
            var popup = FindParent<Popup>(button);
            if (popup != null)
            {
                popup.IsOpen = false;
                _openVolumePopups.Remove(popup);
            }
        }
    }
    
    private T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parentObject = VisualTreeHelper.GetParent(child);
        
        if (parentObject == null)
            return null;
        
        if (parentObject is T parent)
            return parent;
        
        return FindParent<T>(parentObject);
    }
    
    private bool IsInteractiveControl(DependencyObject? element)
    {
        // Check if the element or any of its parents is an interactive control
        while (element != null)
        {
            if (element is Button || 
                element is Slider || 
                element is TextBox || 
                element is Popup ||
                element is System.Windows.Controls.Primitives.Thumb) // Slider thumb
            {
                return true;
            }
            
            // Also check if we're inside a Popup
            var popup = FindParent<Popup>(element);
            if (popup != null)
            {
                return true;
            }
            
            element = VisualTreeHelper.GetParent(element);
        }
        return false;
    }
}
