using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Muzzle.Theme;

/// <summary>Windows 11 system backdrop materials.</summary>
public enum BackdropType {
    /// <summary>No system backdrop: the normal opaque window background.</summary>
    None,

    /// <summary>Mica: the desktop wallpaper, heavily blurred and tinted, behind the whole window.</summary>
    Mica,

    /// <summary>Acrylic: a frosted, translucent blur of what's behind the window.</summary>
    Acrylic,

    /// <summary>Mica Alt ("tabbed"): a stronger wallpaper tint, meant for tabbed title bars.</summary>
    Tabbed
}

/// <summary>
/// Opt-in Windows 11 Mica / Acrylic backdrops: <c>&lt;mz:MuzzleWindow mz:BackdropHelper.BackdropType="Mica" /&gt;</c>.
/// Uses DWMWA_SYSTEMBACKDROP_TYPE (Windows 11 22H2, build 22621+); on older systems it does nothing and
/// <c>IsBackdropActive</c> stays false. While active, the window background switches to the translucent
/// <c>Muzzle.Brush.BackdropCanvas</c> so the material shows through, still following Light / Dark; set
/// <c>mz:BackdropHelper.TintBackground="False"</c> to keep your own <c>Background</c> instead (for example
/// <c>Transparent</c>, with panels you paint yourself). The system menu (Alt+Space, right-click on the title bar)
/// stays available while a backdrop is on.
/// </summary>
public static class BackdropHelper {
    /// <summary>Identifies the <see cref="BackdropType"/> dependency property.</summary>
    public static readonly DependencyProperty BackdropTypeProperty = DependencyProperty.RegisterAttached(
        "BackdropType", typeof(BackdropType), typeof(BackdropHelper), new PropertyMetadata(BackdropType.None, OnBackdropTypeChanged));

    /// <summary>Identifies the <c>TintBackground</c> attached property.</summary>
    public static readonly DependencyProperty TintBackgroundProperty = DependencyProperty.RegisterAttached(
        "TintBackground", typeof(bool), typeof(BackdropHelper), new PropertyMetadata(true, OnTintBackgroundChanged));

    private static readonly DependencyPropertyKey IsBackdropActivePropertyKey = DependencyProperty.RegisterAttachedReadOnly(
        "IsBackdropActive", typeof(bool), typeof(BackdropHelper), new PropertyMetadata(false));

    /// <summary>Identifies the <c>IsBackdropActive</c> attached property.</summary>
    public static readonly DependencyProperty IsBackdropActiveProperty = IsBackdropActivePropertyKey.DependencyProperty;

    private static readonly DependencyProperty SavedGlassProperty = DependencyProperty.RegisterAttached(
        "SavedGlass", typeof(Thickness?), typeof(BackdropHelper), new PropertyMetadata(null));

    private static readonly DependencyProperty RemovedSysMenuProperty = DependencyProperty.RegisterAttached(
        "RemovedSysMenu", typeof(bool), typeof(BackdropHelper), new PropertyMetadata(false));

    private static readonly DependencyProperty TintedProperty = DependencyProperty.RegisterAttached(
        "Tinted", typeof(bool), typeof(BackdropHelper), new PropertyMetadata(false));

    private static readonly DependencyProperty SystemMenuHookProperty = DependencyProperty.RegisterAttached(
        "SystemMenuHook", typeof(HwndSourceHook), typeof(BackdropHelper), new PropertyMetadata(null));

    private const int WmNcRButtonUp = 0x00A5;
    private const int WmSysCommand = 0x0112;
    private const int ScKeyMenu = 0xF100;
    private const int HtCaption = 2;
    private const uint TpmReturnCommand = 0x0100;
    private const uint TpmRightButton = 0x0002;

    private const int DwmaUseImmersiveDarkMode = 20;
    private const int DwmaSystemBackdropType = 38;
    private const int GwlStyle = -16;
    private const long WsSysMenu = 0x00080000;

    // SWP_NOSIZE | SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE | SWP_FRAMECHANGED
    private const uint SwpRefreshFrame = 0x0001 | 0x0002 | 0x0004 | 0x0010 | 0x0020;

    /// <summary>True on Windows 11 22H2 (build 22621) or later.</summary>
    public static bool IsSupported => OperatingSystem.IsWindowsVersionAtLeast(10, 0, 22621);

    /// <summary>Gets the value of the <c>BackdropType</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>BackdropType</c> value.</returns>
    public static BackdropType GetBackdropType(DependencyObject element) {
        return (BackdropType)element.GetValue(BackdropTypeProperty);
    }

    /// <summary>Sets the value of the <c>BackdropType</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetBackdropType(DependencyObject element, BackdropType value) {
        element.SetValue(BackdropTypeProperty, value);
    }

    /// <summary>Gets the value of the <c>IsBackdropActive</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>IsBackdropActive</c> value.</returns>
    public static bool GetIsBackdropActive(DependencyObject element) {
        return (bool)element.GetValue(IsBackdropActiveProperty);
    }

    /// <summary>Gets the value of the <c>TintBackground</c> attached property.</summary>
    /// <param name="element">The window to read from.</param>
    /// <returns>
    /// <see langword="true"/> (the default) when an active backdrop replaces the window background with
    /// <c>Muzzle.Brush.BackdropCanvas</c>; <see langword="false"/> when the window keeps its own background.
    /// </returns>
    public static bool GetTintBackground(DependencyObject element) {
        return (bool)element.GetValue(TintBackgroundProperty);
    }

    /// <summary>Sets the value of the <c>TintBackground</c> attached property.</summary>
    /// <param name="element">The window to set it on.</param>
    /// <param name="value">
    /// <see langword="false"/> to leave the window's <c>Background</c> alone while a backdrop is active.
    /// </param>
    public static void SetTintBackground(DependencyObject element, bool value) {
        element.SetValue(TintBackgroundProperty, value);
    }

    private static void OnTintBackgroundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if ((d is Window window) && GetIsBackdropActive(window)) {
            ApplyTint(window);
        }
    }

    /// <summary>Tints the window with the backdrop canvas, or gives the background back, per <c>TintBackground</c>.</summary>
    private static void ApplyTint(Window window) {
        bool tint = GetIsBackdropActive(window) && GetTintBackground(window);
        bool tinted = (bool)window.GetValue(TintedProperty);
        if (tint && !tinted) {
            window.SetResourceReference(Window.BackgroundProperty, "Muzzle.Brush.BackdropCanvas");
            window.SetValue(TintedProperty, true);
        }
        else if (!tint && tinted) {
            window.ClearValue(Window.BackgroundProperty);
            window.ClearValue(TintedProperty);
        }
    }

    /// <summary>
    /// Shows the system menu of a window whose system-menu style an active backdrop removed.
    /// Returns <see langword="false"/> (and does nothing) for any other window.
    /// </summary>
    internal static bool TryShowSystemMenu(Window window, int screenX, int screenY) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if ((handle == IntPtr.Zero) || (window.GetValue(RemovedSysMenuProperty) is not true)) {
            return false;
        }
        // GetSystemMenu needs the style; put it back just for the call, without a frame refresh, so DWM never
        // gets to draw its caption buttons.
        long style = GetWindowLongPtr(handle, GwlStyle).ToInt64();
        _ = SetWindowLongPtr(handle, GwlStyle, new IntPtr(style | WsSysMenu));
        IntPtr menu = GetSystemMenu(handle, false);
        _ = SetWindowLongPtr(handle, GwlStyle, new IntPtr(style & ~WsSysMenu));
        if (menu == IntPtr.Zero) {
            return true;
        }
        int command = TrackPopupMenuEx(menu, TpmReturnCommand | TpmRightButton, screenX, screenY, handle, IntPtr.Zero);
        if (command != 0) {
            _ = PostMessage(handle, WmSysCommand, new IntPtr(command), IntPtr.Zero);
        }
        return true;
    }

    private static HwndSourceHook SystemMenuHookFor(Window window) {
        if (window.GetValue(SystemMenuHookProperty) is not HwndSourceHook hook) {
            hook = (IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) => {
                if ((msg == WmNcRButtonUp) && (wParam.ToInt64() == HtCaption)) {
                    long point = lParam.ToInt64();
                    handled = TryShowSystemMenu(window, unchecked((short)(point & 0xFFFF)), unchecked((short)((point >> 16) & 0xFFFF)));
                }
                else if ((msg == WmSysCommand) && ((wParam.ToInt64() & 0xFFF0) == ScKeyMenu) && (lParam.ToInt64() == ' ')) {
                    // Alt+Space: drop the menu from the top-left corner of the content, as the stock frame does.
                    _ = GetWindowRect(hwnd, out NativeRect rect);
                    handled = TryShowSystemMenu(window, rect.Left + 8, rect.Top + 8);
                }
                return IntPtr.Zero;
            };
            window.SetValue(SystemMenuHookProperty, hook);
        }
        return hook;
    }

    private static void OnBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not Window window) {
            return;
        }
        window.SourceInitialized -= OnSourceInitialized;
        MuzzleThemeManager.ThemeChanged -= ThemeHandlerFor(window);
        if (new WindowInteropHelper(window).Handle == IntPtr.Zero) {
            window.SourceInitialized += OnSourceInitialized;
            return;
        }
        Apply(window);
    }

    private static void OnSourceInitialized(object? sender, EventArgs e) {
        if (sender is Window window) {
            window.SourceInitialized -= OnSourceInitialized;
            Apply(window);
        }
    }

    // One cached handler per window so it can be unsubscribed.
    private static readonly DependencyProperty ThemeHandlerProperty = DependencyProperty.RegisterAttached(
        "ThemeHandler", typeof(EventHandler<MuzzleThemeChangedEventArgs>), typeof(BackdropHelper), new PropertyMetadata(null));

    private static EventHandler<MuzzleThemeChangedEventArgs> ThemeHandlerFor(Window window) {
        if (window.GetValue(ThemeHandlerProperty) is not EventHandler<MuzzleThemeChangedEventArgs> handler) {
            handler = (_, _) => SetDarkFlag(window);
            window.SetValue(ThemeHandlerProperty, handler);
        }
        return handler;
    }

    private static void Apply(Window window) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        HwndSource? source = HwndSource.FromHwnd(handle);
        BackdropType type = GetBackdropType(window);
        bool activate = (type != BackdropType.None) && IsSupported && (source?.CompositionTarget is not null);
        WindowChrome? chrome = WindowChrome.GetWindowChrome(window);
        if (activate) {
            if ((chrome is not null) && (window.GetValue(SavedGlassProperty) is null)) {
                window.SetValue(SavedGlassProperty, chrome.GlassFrameThickness);
            }
            if (chrome is not null) {
                chrome.GlassFrameThickness = new Thickness(-1);
            }
            else {
                Margins margins = new() { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                _ = DwmExtendFrameIntoClientArea(handle, ref margins);
            }
            source!.CompositionTarget!.BackgroundColor = Colors.Transparent;
            int value = type switch {
                BackdropType.Acrylic => 3,
                BackdropType.Tabbed => 4,
                _ => 2
            };
            _ = DwmSetWindowAttribute(handle, DwmaSystemBackdropType, ref value, sizeof(int));
            // With the frame extended into the client area DWM would paint its own caption buttons behind ours;
            // dropping WS_SYSMENU while the backdrop is on hides them (restored when it turns off).
            long style = GetWindowLongPtr(handle, GwlStyle).ToInt64();
            if ((style & WsSysMenu) != 0) {
                window.SetValue(RemovedSysMenuProperty, true);
                _ = SetWindowLongPtr(handle, GwlStyle, new IntPtr(style & ~WsSysMenu));
                _ = SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpRefreshFrame);
            }
            source.RemoveHook(SystemMenuHookFor(window));
            source.AddHook(SystemMenuHookFor(window));
            SetDarkFlag(window);
            MuzzleThemeManager.ThemeChanged += ThemeHandlerFor(window);
            window.SetValue(IsBackdropActivePropertyKey, true);
            ApplyTint(window);
            return;
        }
        if (!GetIsBackdropActive(window)) {
            return;
        }
        if ((chrome is not null) && (window.GetValue(SavedGlassProperty) is Thickness glass)) {
            chrome.GlassFrameThickness = glass;
        }
        window.ClearValue(SavedGlassProperty);
        int none = 1;
        if (handle != IntPtr.Zero) {
            _ = DwmSetWindowAttribute(handle, DwmaSystemBackdropType, ref none, sizeof(int));
            if (window.GetValue(RemovedSysMenuProperty) is true) {
                long style = GetWindowLongPtr(handle, GwlStyle).ToInt64();
                _ = SetWindowLongPtr(handle, GwlStyle, new IntPtr(style | WsSysMenu));
                _ = SetWindowPos(handle, IntPtr.Zero, 0, 0, 0, 0, SwpRefreshFrame);
            }
        }
        window.ClearValue(RemovedSysMenuProperty);
        source?.RemoveHook(SystemMenuHookFor(window));
        if (source?.CompositionTarget is not null) {
            source.CompositionTarget.BackgroundColor = SystemColors.WindowColor;
        }
        window.SetValue(IsBackdropActivePropertyKey, false);
        ApplyTint(window);
    }

    private static void SetDarkFlag(Window window) {
        IntPtr handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) {
            return;
        }
        int dark = MuzzleThemeManager.IsDark ? 1 : 0;
        _ = DwmSetWindowAttribute(handle, DwmaUseImmersiveDarkMode, ref dark, sizeof(int));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetSystemMenu(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool revert);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr parameters);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int index, IntPtr value);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}