using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;

namespace Muzzle.Theme;

/// <summary>
/// Remembers a window's bounds and state between runs: <c>&lt;Window mz:WindowStateSaver.Key="MainWindow" /&gt;</c>.
/// On close it writes Left / Top / Width / Height (the restored bounds) and WindowState to a JSON file in the user's
/// local app data; on the next start it restores them before the window appears, after checking that the title
/// bar would land on a connected monitor (otherwise the size is kept and the window is centred).
/// </summary>
public static class WindowStateSaver {
    /// <summary>Identifies the <c>Key</c> attached property.</summary>
    public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        "Key", typeof(string), typeof(WindowStateSaver), new PropertyMetadata(null, OnKeyChanged));

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Global switch (e.g. turn it off for automated test runs). Default true.</summary>
    public static bool IsEnabled { get; set; } = true;

    /// <summary>Where the state file lives (default %LOCALAPPDATA%\&lt;entry assembly name&gt;).</summary>
    public static string StorageFolder { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Assembly.GetEntryAssembly()?.GetName().Name ?? "MuzzleApp");

    /// <summary>Gets or sets the state file's name. The default is <c>window-state.json</c>.</summary>
    public static string FileName { get; set; } = "window-state.json";

    /// <summary>Gets the full path of the state file (<see cref="StorageFolder"/> + <see cref="FileName"/>).</summary>
    public static string FilePath => Path.Combine(StorageFolder, FileName);

    /// <summary>Gets the value of the <c>Key</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Key</c> value.</returns>
    public static string? GetKey(DependencyObject element) {
        return (string?)element.GetValue(KeyProperty);
    }

    /// <summary>Sets the value of the <c>Key</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetKey(DependencyObject element, string? value) {
        element.SetValue(KeyProperty, value);
    }

    /// <summary>Writes the window's current placement under its key.</summary>
    public static void Save(Window window) {
        if (!IsEnabled || (GetKey(window) is not string key) || string.IsNullOrWhiteSpace(key)) {
            return;
        }
        Rect bounds = (window.WindowState == WindowState.Normal) ? new Rect(window.Left, window.Top, window.ActualWidth, window.ActualHeight) : window.RestoreBounds;
        if (bounds.IsEmpty || (bounds.Width <= 0) || (bounds.Height <= 0) || double.IsNaN(bounds.Left)) {
            return;
        }
        Dictionary<string, SavedPlacement> all = Load();
        all[key] = new SavedPlacement {
            Left = bounds.Left,
            Top = bounds.Top,
            Width = bounds.Width,
            Height = bounds.Height,
            State = (window.WindowState == WindowState.Maximized) ? nameof(WindowState.Maximized) : nameof(WindowState.Normal)
        };
        try {
            Directory.CreateDirectory(StorageFolder);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(all, JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            // Persisting placement is best-effort.
        }
    }

    /// <summary>Applies a saved placement to a window that hasn't been shown yet. Returns false when nothing was saved.</summary>
    public static bool Restore(Window window) {
        if (!IsEnabled || (GetKey(window) is not string key) || !Load().TryGetValue(key, out SavedPlacement? saved)) {
            return false;
        }
        if ((saved.Width < 50) || (saved.Height < 50) || double.IsNaN(saved.Left) || double.IsNaN(saved.Top)) {
            return false;
        }
        Rect workArea = SystemParameters.WorkArea;
        double width = Math.Max(window.MinWidth, Math.Min(saved.Width, Math.Max(workArea.Width, window.MinWidth)));
        double height = Math.Max(window.MinHeight, Math.Min(saved.Height, Math.Max(workArea.Height, window.MinHeight)));
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Width = width;
        window.Height = height;
        if (IsOnAnyMonitor(new Rect(saved.Left, saved.Top, width, height))) {
            window.Left = saved.Left;
            window.Top = saved.Top;
        }
        else {
            window.Left = workArea.Left + ((workArea.Width - width) / 2);
            window.Top = workArea.Top + ((workArea.Height - height) / 2);
        }
        if (saved.State == nameof(WindowState.Maximized)) {
            window.WindowState = WindowState.Maximized;
        }
        return true;
    }

    /// <summary>Drops the saved placement for a key.</summary>
    public static void Forget(string key) {
        Dictionary<string, SavedPlacement> all = Load();
        if (all.Remove(key)) {
            try {
                File.WriteAllText(FilePath, JsonSerializer.Serialize(all, JsonOptions));
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) {
            }
        }
    }

    private static void OnKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not Window window) {
            return;
        }
        window.SourceInitialized -= OnSourceInitialized;
        window.Closing -= OnClosing;
        if (e.NewValue is string key && !string.IsNullOrWhiteSpace(key)) {
            window.SourceInitialized += OnSourceInitialized;
            window.Closing += OnClosing;
        }
    }

    // Restoring here (rather than when the key is set) wins over Width / Height / WindowStartupLocation set in XAML.
    private static void OnSourceInitialized(object? sender, EventArgs e) {
        if (sender is Window window) {
            Restore(window);
        }
    }

    private static void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e) {
        if ((sender is Window window) && !e.Cancel) {
            Save(window);
        }
    }

    private static Dictionary<string, SavedPlacement> Load() {
        try {
            if (File.Exists(FilePath)) {
                return JsonSerializer.Deserialize<Dictionary<string, SavedPlacement>>(File.ReadAllText(FilePath)) ?? [];
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException) {
        }
        return [];
    }

    /// <summary>True when the window's title strip overlaps some monitor's work area enough to grab it.</summary>
    private static bool IsOnAnyMonitor(Rect dipBounds) {
        double scale = GetSystemDpi() / 96.0;
        NativeRect strip = new() {
            Left = (int)Math.Round(dipBounds.Left * scale),
            Top = (int)Math.Round(dipBounds.Top * scale),
            Right = (int)Math.Round(dipBounds.Right * scale),
            Bottom = (int)Math.Round((dipBounds.Top + 40) * scale)
        };
        bool found = false;
        MonitorEnumProc callback = (IntPtr monitor, IntPtr hdc, ref NativeRect rect, IntPtr data) => {
            MonitorInfo info = new() { Size = Marshal.SizeOf<MonitorInfo>() };
            if (GetMonitorInfo(monitor, ref info)) {
                int overlapWidth = Math.Min(strip.Right, info.Work.Right) - Math.Max(strip.Left, info.Work.Left);
                int overlapHeight = Math.Min(strip.Bottom, info.Work.Bottom) - Math.Max(strip.Top, info.Work.Top);
                if ((overlapWidth >= 80) && (overlapHeight >= 16)) {
                    found = true;
                }
            }
            return true;
        };
        try {
            _ = EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);
            GC.KeepAlive(callback);
        }
        catch (EntryPointNotFoundException) {
            Rect virtualScreen = new(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);
            return virtualScreen.IntersectsWith(dipBounds);
        }
        return found;
    }

    private static uint GetSystemDpi() {
        try {
            uint dpi = GetDpiForSystem();
            return (dpi == 0) ? 96 : dpi;
        }
        catch (EntryPointNotFoundException) {
            return 96;
        }
    }

    private sealed class SavedPlacement {
        public double Left { get; set; }

        public double Top { get; set; }

        public double Width { get; set; }

        public double Height { get; set; }

        public string State { get; set; } = nameof(WindowState.Normal);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, ref NativeRect rect, IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForSystem();
}