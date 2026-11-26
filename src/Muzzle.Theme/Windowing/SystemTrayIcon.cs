using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using Muzzle.Theme.Controls;

namespace Muzzle.Theme;

/// <summary>
/// A notification-area (system tray) icon for WPF. Declare it anywhere in a window (it takes no space):
/// <code>&lt;mz:SystemTrayIcon IconSource="{StaticResource AppIcon}" ToolTipText="Muzzle" LeftClickCommand="{Binding ShowCommand}"&gt;
///     &lt;mz:SystemTrayIcon.MenuContent&gt;&lt;StackPanel&gt;&lt;MenuItem Header="Open" /&gt;…&lt;/StackPanel&gt;&lt;/mz:SystemTrayIcon.MenuContent&gt;
/// &lt;/mz:SystemTrayIcon&gt;</code>
/// Right-click opens <see cref="MenuContent"/> in a Muzzle card that pops in at the cursor (MenuItems inside get the
/// Muzzle menu look and close the card when clicked) instead of a legacy Win32 menu. The icon is removed when the
/// element unloads, when <see cref="IsIconVisible"/> turns false, or when the app exits.
/// </summary>
public class SystemTrayIcon : FrameworkElement, IDisposable {
    /// <summary>Identifies the <see cref="IconSource"/> dependency property.</summary>
    public static readonly DependencyProperty IconSourceProperty = DependencyProperty.Register(
        nameof(IconSource), typeof(ImageSource), typeof(SystemTrayIcon), new PropertyMetadata(null, OnIconChanged));

    /// <summary>Identifies the <see cref="ToolTipText"/> dependency property.</summary>
    public static readonly DependencyProperty ToolTipTextProperty = DependencyProperty.Register(
        nameof(ToolTipText), typeof(string), typeof(SystemTrayIcon), new PropertyMetadata(string.Empty, OnIconChanged));

    /// <summary>Identifies the <see cref="MenuContent"/> dependency property.</summary>
    public static readonly DependencyProperty MenuContentProperty = DependencyProperty.Register(
        nameof(MenuContent), typeof(object), typeof(SystemTrayIcon), new PropertyMetadata(null, OnMenuContentChanged));

    /// <summary>Identifies the <see cref="LeftClickCommand"/> dependency property.</summary>
    public static readonly DependencyProperty LeftClickCommandProperty = DependencyProperty.Register(
        nameof(LeftClickCommand), typeof(ICommand), typeof(SystemTrayIcon), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="LeftClickCommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty LeftClickCommandParameterProperty = DependencyProperty.Register(
        nameof(LeftClickCommandParameter), typeof(object), typeof(SystemTrayIcon), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsIconVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsIconVisibleProperty = DependencyProperty.Register(
        nameof(IsIconVisible), typeof(bool), typeof(SystemTrayIcon), new PropertyMetadata(true, OnIsIconVisibleChanged));

    private const int WmApp = 0x8000;
    private const int CallbackMessage = WmApp + 0x4D5A;
    private const int WmLButtonUp = 0x0202;
    private const int WmLButtonDblClk = 0x0203;
    private const int WmRButtonUp = 0x0205;
    private const int WmContextMenu = 0x007B;
    private const uint NimAdd = 0;
    private const uint NimModify = 1;
    private const uint NimDelete = 2;
    private const uint NifMessage = 0x1;
    private const uint NifIcon = 0x2;
    private const uint NifTip = 0x4;
    private const uint NifInfo = 0x10;
    private const uint NifShowTip = 0x80;

    private static readonly int TaskbarCreatedMessage = RegisterWindowMessage("TaskbarCreated");
    private static int nextId = 1;

    private readonly uint id = (uint)nextId++;
    private HwndSource? messageWindow;
    private IntPtr iconHandle;
    private bool added;
    private Popup? menuPopup;

    static SystemTrayIcon() {
        VisibilityProperty.OverrideMetadata(typeof(SystemTrayIcon), new FrameworkPropertyMetadata(Visibility.Collapsed));
    }

    /// <summary>Initializes a new instance of the <see cref="SystemTrayIcon"/> class.</summary>
    public SystemTrayIcon() {
        MenuStyling.RegisterImplicitStyles(Resources);
        Loaded += (_, _) => UpdateIcon();
        Unloaded += (_, _) => RemoveIcon();
        Dispatcher.ShutdownStarted += (_, _) => Dispose();
    }

    /// <summary>Occurs when the icon is left-clicked (before <see cref="LeftClickCommand"/> runs).</summary>
    public event EventHandler? LeftClick;

    /// <summary>Occurs when the icon is double-clicked.</summary>
    public event EventHandler? DoubleClick;

    /// <summary>Occurs when the icon is right-clicked, just before the menu opens.</summary>
    public event EventHandler? RightClick;

    /// <summary>Gets or sets the icon source. The default is <see langword="null"/>. This is a dependency property.</summary>
    public ImageSource? IconSource {
        get => (ImageSource?)GetValue(IconSourceProperty);
        set => SetValue(IconSourceProperty, value);
    }

    /// <summary>Gets or sets the tool tip text. The default is an empty string. This is a dependency property.</summary>
    public string ToolTipText {
        get => (string)GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    /// <summary>Content of the right-click card (e.g. a StackPanel of MenuItems).</summary>
    public object? MenuContent {
        get => GetValue(MenuContentProperty);
        set => SetValue(MenuContentProperty, value);
    }

    /// <summary>Gets or sets the left click command. The default is <see langword="null"/>. This is a dependency property.</summary>
    public ICommand? LeftClickCommand {
        get => (ICommand?)GetValue(LeftClickCommandProperty);
        set => SetValue(LeftClickCommandProperty, value);
    }

    /// <summary>Gets or sets the left click command parameter. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? LeftClickCommandParameter {
        get => GetValue(LeftClickCommandParameterProperty);
        set => SetValue(LeftClickCommandParameterProperty, value);
    }

    /// <summary>Gets or sets whether the icon is visible. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsIconVisible {
        get => (bool)GetValue(IsIconVisibleProperty);
        set => SetValue(IsIconVisibleProperty, value);
    }

    /// <summary>True while the icon is in the notification area.</summary>
    public bool IsCreated => added;

    /// <summary>Shows a Windows notification from the icon (a toast on Windows 10/11).</summary>
    public bool ShowNotification(string title, string message) {
        if (!added) {
            return false;
        }
        NotifyIconData data = CreateData(NifInfo);
        data.InfoTitle = Truncate(title, 63);
        data.Info = Truncate(message, 255);
        data.InfoFlags = 0x1; // NIIF_INFO
        return ShellNotifyIcon(NimModify, ref data);
    }

    /// <summary>Opens the menu card at the current cursor position.</summary>
    public void ShowMenu() {
        if (MenuContent is null) {
            return;
        }
        menuPopup ??= CreateMenuPopup();
        menuPopup.IsOpen = true;
    }

    /// <inheritdoc />
    public void Dispose() {
        RemoveIcon();
        messageWindow?.RemoveHook(WndProc);
        messageWindow?.Dispose();
        messageWindow = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>The menu content is a logical child, so it inherits DataContext and picks up the Muzzle menu style.</summary>
    protected override System.Collections.IEnumerator LogicalChildren =>
        (MenuContent is null) ? Array.Empty<object>().GetEnumerator() : new[] { MenuContent }.GetEnumerator();

    private static void OnMenuContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        SystemTrayIcon tray = (SystemTrayIcon)d;
        if (e.OldValue is not null) {
            tray.RemoveLogicalChild(e.OldValue);
        }
        if (e.NewValue is not null) {
            tray.AddLogicalChild(e.NewValue);
        }
        MenuStyling.ApplySoon(e.NewValue);
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((SystemTrayIcon)d).UpdateIcon();
    }

    private static void OnIsIconVisibleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        SystemTrayIcon tray = (SystemTrayIcon)d;
        if ((bool)e.NewValue) {
            tray.UpdateIcon();
        }
        else {
            tray.RemoveIcon();
        }
    }

    private void UpdateIcon() {
        if (!IsLoaded || !IsIconVisible) {
            return;
        }
        EnsureMessageWindow();
        IntPtr previous = iconHandle;
        iconHandle = CreateIconHandle(IconSource);
        NotifyIconData data = CreateData(NifMessage | NifIcon | NifTip | NifShowTip);
        added = ShellNotifyIcon(added ? NimModify : NimAdd, ref data) || added;
        if (previous != IntPtr.Zero) {
            _ = DestroyIcon(previous);
        }
    }

    private void RemoveIcon() {
        if (added) {
            NotifyIconData data = CreateData(0);
            _ = ShellNotifyIcon(NimDelete, ref data);
            added = false;
        }
        if (iconHandle != IntPtr.Zero) {
            _ = DestroyIcon(iconHandle);
            iconHandle = IntPtr.Zero;
        }
        if (menuPopup is not null) {
            menuPopup.IsOpen = false;
        }
    }

    private NotifyIconData CreateData(uint flags) {
        return new NotifyIconData {
            Size = Marshal.SizeOf<NotifyIconData>(),
            Window = messageWindow?.Handle ?? IntPtr.Zero,
            Id = id,
            Flags = flags,
            CallbackMessage = CallbackMessage,
            Icon = iconHandle,
            Tip = Truncate(ToolTipText, 127),
            Info = string.Empty,
            InfoTitle = string.Empty
        };
    }

    private void EnsureMessageWindow() {
        if (messageWindow is not null) {
            return;
        }
        HwndSourceParameters parameters = new("MuzzleSystemTrayIcon") {
            Width = 0,
            Height = 0,
            WindowStyle = 0,
            ParentWindow = new IntPtr(-3) // HWND_MESSAGE: a message-only window
        };
        messageWindow = new HwndSource(parameters);
        messageWindow.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        if (msg == TaskbarCreatedMessage) {
            // Explorer restarted: put the icon back.
            added = false;
            UpdateIcon();
            return IntPtr.Zero;
        }
        if (msg != CallbackMessage) {
            return IntPtr.Zero;
        }
        switch ((int)(lParam.ToInt64() & 0xFFFF)) {
            case WmLButtonUp:
                LeftClick?.Invoke(this, EventArgs.Empty);
                if ((LeftClickCommand is ICommand command) && command.CanExecute(LeftClickCommandParameter)) {
                    command.Execute(LeftClickCommandParameter);
                }
                break;
            case WmLButtonDblClk:
                DoubleClick?.Invoke(this, EventArgs.Empty);
                break;
            case WmRButtonUp:
            case WmContextMenu:
                RightClick?.Invoke(this, EventArgs.Empty);
                ShowMenu();
                break;
        }
        handled = true;
        return IntPtr.Zero;
    }

    private Popup CreateMenuPopup() {
        ContentPresenter presenter = new();
        presenter.SetBinding(ContentPresenter.ContentProperty, new System.Windows.Data.Binding(nameof(MenuContent)) { Source = this });
        SoftBorder card = new() {
            Padding = new Thickness(6),
            CornerRadius = new CornerRadius(12),
            BorderThickness = new Thickness(1),
            MinWidth = 200,
            Child = presenter
        };
        card.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.Surface");
        card.SetResourceReference(Border.BorderBrushProperty, "Muzzle.Brush.SurfaceRim");
        card.SetResourceReference(TextElement.ForegroundProperty, "Muzzle.Brush.TextPrimary");
        card.SetResourceReference(TextElement.FontFamilyProperty, "Muzzle.FontFamily");
        card.Effect = MuzzleShadows.Resolve(ShadowDepth.Floating);
        MenuStyling.RegisterImplicitStyles(card.Resources);
        Grid root = new() { Margin = new Thickness(14, 6, 14, 20), Children = { card } };
        Popup popup = new() {
            Child = root,
            Placement = PlacementMode.MousePoint,
            AllowsTransparency = true,
            PopupAnimation = PopupAnimation.None,
            StaysOpen = false
        };
        root.AddHandler(MenuItem.ClickEvent, new RoutedEventHandler((_, e) => {
            if (e.OriginalSource is MenuItem { HasItems: false }) {
                popup.IsOpen = false;
            }
        }));
        popup.Opened += (_, _) => {
            // The tray menu belongs to a background app: take the foreground so an outside click dismisses it.
            if (PresentationSource.FromVisual(root) is HwndSource source) {
                _ = SetForegroundWindow(source.Handle);
            }
            if (!Jelly.IsMotionEnabled(this)) {
                return;
            }
            card.RenderTransformOrigin = new Point(0.5, 1);
            ScaleTransform scale = Jelly.EnsureScale(card);
            DoubleAnimation pop = new(0.85, 1, new Duration(TimeSpan.FromMilliseconds(520))) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellyKey) };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
            card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(140))));
        };
        return popup;
    }

    /// <summary>Renders an ImageSource into a 32 x 32 HICON (null → the default application icon).</summary>
    private static IntPtr CreateIconHandle(ImageSource? source) {
        if (source is null) {
            return LoadIcon(IntPtr.Zero, new IntPtr(32512)); // IDI_APPLICATION
        }
        const int Size = 32;
        DrawingVisual visual = new();
        using (DrawingContext context = visual.RenderOpen()) {
            context.DrawImage(source, new Rect(0, 0, Size, Size));
        }
        RenderTargetBitmap rendered = new(Size, Size, 96, 96, PixelFormats.Pbgra32);
        rendered.Render(visual);
        FormatConvertedBitmap straight = new(rendered, PixelFormats.Bgra32, null, 0);
        byte[] pixels = new byte[Size * Size * 4];
        straight.CopyPixels(pixels, Size * 4, 0);
        IntPtr color = CreateBitmap(Size, Size, 1, 32, pixels);
        IntPtr mask = CreateBitmap(Size, Size, 1, 1, new byte[Size * Size / 8]);
        IconInfo info = new() { IsIcon = true, Mask = mask, Color = color };
        IntPtr icon = CreateIconIndirect(ref info);
        _ = DeleteObject(color);
        _ = DeleteObject(mask);
        return icon;
    }

    private static string Truncate(string? text, int max) {
        text ??= string.Empty;
        return (text.Length <= max) ? text : text[..max];
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData {
        public int Size;
        public IntPtr Window;
        public uint Id;
        public uint Flags;
        public int CallbackMessage;
        public IntPtr Icon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Tip;

        public uint State;
        public uint StateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string Info;

        public uint TimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string InfoTitle;

        public uint InfoFlags;
        public Guid Item;
        public IntPtr BalloonIcon;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct IconInfo {
        [MarshalAs(UnmanagedType.Bool)]
        public bool IsIcon;

        public int HotspotX;
        public int HotspotY;
        public IntPtr Mask;
        public IntPtr Color;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIcon(uint message, ref NotifyIconData data);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int RegisterWindowMessage(string name);

    [DllImport("user32.dll")]
    private static extern IntPtr CreateIconIndirect(ref IconInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr icon);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr instance, IntPtr name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateBitmap(int width, int height, uint planes, uint bitsPerPixel, byte[] bits);

    [DllImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(IntPtr handle);
}