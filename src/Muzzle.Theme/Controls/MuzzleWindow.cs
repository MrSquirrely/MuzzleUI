using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A custom-chrome window: rounded, pastel, with jelly caption buttons, Windows 11 snap layouts and
/// a <see cref="NonClientAreaContent"/> slot for your own title-bar UI (search boxes, buttons, tabs…).
/// </summary>
[TemplatePart(Name = RootPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = MaximizeButtonPartName, Type = typeof(Button))]
public class MuzzleWindow : Window {
    private const string RootPartName = "PART_Root";
    private const string MaximizeButtonPartName = "PART_MaximizeButton";

    private const int WmNcHitTest = 0x0084;
    private const int WmNcMouseMove = 0x00A0;
    private const int WmNcLButtonDown = 0x00A1;
    private const int WmNcLButtonUp = 0x00A2;
    private const int WmNcMouseLeave = 0x02A2;
    private const int HtMaxButton = 9;
    private const int SmCxFrame = 32;
    private const int SmCyFrame = 33;
    private const int SmCxPaddedBorder = 92;
    private const int DwmaUseImmersiveDarkMode = 20;
    private const int DwmaWindowCornerPreference = 33;
    private const int DwmaBorderColor = 34;
    private const int DwmcpRound = 2;

    /// <summary>Identifies the <see cref="NonClientAreaContent"/> dependency property.</summary>
    public static readonly DependencyProperty NonClientAreaContentProperty = DependencyProperty.Register(
        nameof(NonClientAreaContent),
        typeof(object),
        typeof(MuzzleWindow),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="NonClientAreaContentTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty NonClientAreaContentTemplateProperty = DependencyProperty.Register(
        nameof(NonClientAreaContentTemplate),
        typeof(DataTemplate),
        typeof(MuzzleWindow),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="NonClientAreaHeight"/> dependency property.</summary>
    public static readonly DependencyProperty NonClientAreaHeightProperty = DependencyProperty.Register(
        nameof(NonClientAreaHeight),
        typeof(double),
        typeof(MuzzleWindow),
        new FrameworkPropertyMetadata(44.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnNonClientAreaHeightChanged),
        static value => value is double height && double.IsFinite(height) && (height >= 0));

    /// <summary>Identifies the <see cref="NonClientAreaBackground"/> dependency property.</summary>
    public static readonly DependencyProperty NonClientAreaBackgroundProperty = DependencyProperty.Register(
        nameof(NonClientAreaBackground),
        typeof(Brush),
        typeof(MuzzleWindow),
        new FrameworkPropertyMetadata(Brushes.Transparent, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="ShowTitle"/> dependency property.</summary>
    public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
        nameof(ShowTitle),
        typeof(bool),
        typeof(MuzzleWindow),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowIcon"/> dependency property.</summary>
    public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
        nameof(ShowIcon),
        typeof(bool),
        typeof(MuzzleWindow),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowMinimizeButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowMinimizeButtonProperty = DependencyProperty.Register(
        nameof(ShowMinimizeButton),
        typeof(bool),
        typeof(MuzzleWindow),
        new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowMaximizeButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowMaximizeButtonProperty = DependencyProperty.Register(
        nameof(ShowMaximizeButton),
        typeof(bool),
        typeof(MuzzleWindow),
        new PropertyMetadata(true));

    /// <summary>Set by the window on its maximize button while the OS owns the pointer (snap-layout hover).</summary>
    public static readonly DependencyProperty IsCaptionHoveredProperty = DependencyProperty.RegisterAttached(
        "IsCaptionHovered",
        typeof(bool),
        typeof(MuzzleWindow),
        new PropertyMetadata(false));

    /// <summary>Identifies the <c>IsCaptionPressed</c> attached property.</summary>
    public static readonly DependencyProperty IsCaptionPressedProperty = DependencyProperty.RegisterAttached(
        "IsCaptionPressed",
        typeof(bool),
        typeof(MuzzleWindow),
        new PropertyMetadata(false));

    private readonly WindowChrome chrome;
    private FrameworkElement? root;
    private Button? maximizeButton;
    private HwndSource? source;

    static MuzzleWindow() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MuzzleWindow), new FrameworkPropertyMetadata(typeof(MuzzleWindow)));
    }

    /// <summary>Initializes a new instance of the <see cref="MuzzleWindow"/> class.</summary>
    public MuzzleWindow() {
        chrome = new WindowChrome {
            GlassFrameThickness = new Thickness(0),
            CornerRadius = new CornerRadius(16),
            CaptionHeight = NonClientAreaHeight,
            ResizeBorderThickness = SystemParameters.WindowResizeBorderThickness,
            UseAeroCaptionButtons = false,
            NonClientFrameEdges = NonClientFrameEdges.None
        };
        WindowChrome.SetWindowChrome(this, chrome);

        // Implicit styles only match the exact type, so subclasses (e.g. MainWindow) pick the theme style up explicitly.
        SetResourceReference(StyleProperty, typeof(MuzzleWindow));

        CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));
        CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, OnShowSystemMenu));
    }

    /// <summary>Content rendered in the title bar between the title and the caption buttons. Interactive elements receive input.</summary>
    public object? NonClientAreaContent {
        get => GetValue(NonClientAreaContentProperty);
        set => SetValue(NonClientAreaContentProperty, value);
    }

    /// <summary>Gets or sets the non client area content template. The default is <see langword="null"/>. This is a dependency property.</summary>
    public DataTemplate? NonClientAreaContentTemplate {
        get => (DataTemplate?)GetValue(NonClientAreaContentTemplateProperty);
        set => SetValue(NonClientAreaContentTemplateProperty, value);
    }

    /// <summary>Height of the title bar; also the draggable caption height.</summary>
    public double NonClientAreaHeight {
        get => (double)GetValue(NonClientAreaHeightProperty);
        set => SetValue(NonClientAreaHeightProperty, value);
    }

    /// <summary>Gets or sets the non client area background. The default is <c>Brushes.Transparent</c>. This is a dependency property.</summary>
    public Brush? NonClientAreaBackground {
        get => (Brush?)GetValue(NonClientAreaBackgroundProperty);
        set => SetValue(NonClientAreaBackgroundProperty, value);
    }

    /// <summary>Gets or sets whether to show the title. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowTitle {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    /// <summary>Gets or sets whether to show the icon. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowIcon {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    /// <summary>Gets or sets whether to show the minimize button. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowMinimizeButton {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    /// <summary>Gets or sets whether to show the maximize button. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowMaximizeButton {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    /// <summary>Gets the value of the <c>IsCaptionHovered</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>IsCaptionHovered</c> value.</returns>
    public static bool GetIsCaptionHovered(DependencyObject element) {
        return (bool)element.GetValue(IsCaptionHoveredProperty);
    }

    /// <summary>Sets the value of the <c>IsCaptionHovered</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetIsCaptionHovered(DependencyObject element, bool value) {
        element.SetValue(IsCaptionHoveredProperty, value);
    }

    /// <summary>Gets the value of the <c>IsCaptionPressed</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>IsCaptionPressed</c> value.</returns>
    public static bool GetIsCaptionPressed(DependencyObject element) {
        return (bool)element.GetValue(IsCaptionPressedProperty);
    }

    /// <summary>Sets the value of the <c>IsCaptionPressed</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetIsCaptionPressed(DependencyObject element, bool value) {
        element.SetValue(IsCaptionPressedProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        root = GetTemplateChild(RootPartName) as FrameworkElement;
        maximizeButton = GetTemplateChild(MaximizeButtonPartName) as Button;
        UpdateMaximizedFrame();
    }

    /// <inheritdoc />
    protected override void OnSourceInitialized(EventArgs e) {
        base.OnSourceInitialized(e);
        IntPtr handle = new WindowInteropHelper(this).Handle;
        source = HwndSource.FromHwnd(handle);
        source?.AddHook(WndProc);
        ApplyDwmAttributes(handle);
        MuzzleThemeManager.ThemeChanged += OnThemeChanged;
        if (SizeToContent != SizeToContent.Manual) {
            // The first size-to-content pass ran before WindowChrome removed the stock frame, so it added that
            // frame's size to the window. Measure again now that the client area covers the whole window.
            InvalidateMeasure();
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e) {
        MuzzleThemeManager.ThemeChanged -= OnThemeChanged;
        source?.RemoveHook(WndProc);
        source = null;
        base.OnClosed(e);
    }

    /// <inheritdoc />
    protected override void OnStateChanged(EventArgs e) {
        base.OnStateChanged(e);
        UpdateMaximizedFrame();
    }

    /// <inheritdoc />
    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi) {
        base.OnDpiChanged(oldDpi, newDpi);
        UpdateMaximizedFrame();
    }

    private static void OnNonClientAreaHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is MuzzleWindow window) {
            window.chrome.CaptionHeight = (double)e.NewValue;
        }
    }

    private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e) {
        SystemCommands.CloseWindow(this);
    }

    private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e) {
        SystemCommands.MinimizeWindow(this);
    }

    private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e) {
        ToggleMaximize();
    }

    private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e) {
        SystemCommands.RestoreWindow(this);
    }

    private void OnShowSystemMenu(object sender, ExecutedRoutedEventArgs e) {
        Point screen = PointToScreen(new Point(0, NonClientAreaHeight));
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        SystemCommands.ShowSystemMenu(this, new Point(screen.X / dpi.DpiScaleX, screen.Y / dpi.DpiScaleY));
    }

    private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = ResizeMode != ResizeMode.NoResize;
    }

    private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = (ResizeMode == ResizeMode.CanResize) || (ResizeMode == ResizeMode.CanResizeWithGrip);
    }

    private void ToggleMaximize() {
        if (WindowState == WindowState.Maximized) {
            SystemCommands.RestoreWindow(this);
        }
        else if ((ResizeMode == ResizeMode.CanResize) || (ResizeMode == ResizeMode.CanResizeWithGrip)) {
            SystemCommands.MaximizeWindow(this);
        }
    }

    /// <summary>When maximized, Windows pushes the frame off-screen by the resize border; pad the content back in.</summary>
    private void UpdateMaximizedFrame() {
        if (root is null) {
            return;
        }
        root.Margin = WindowState == WindowState.Maximized ? GetMaximizedFrameThickness() : new Thickness(0);
    }

    private Thickness GetMaximizedFrameThickness() {
        IntPtr handle = new WindowInteropHelper(this).Handle;
        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        if (handle != IntPtr.Zero) {
            try {
                uint windowDpi = GetDpiForWindow(handle);
                int horizontal = GetSystemMetricsForDpi(SmCxFrame, windowDpi) + GetSystemMetricsForDpi(SmCxPaddedBorder, windowDpi);
                int vertical = GetSystemMetricsForDpi(SmCyFrame, windowDpi) + GetSystemMetricsForDpi(SmCxPaddedBorder, windowDpi);
                if ((horizontal > 0) && (vertical > 0)) {
                    double x = horizontal / dpi.DpiScaleX;
                    double y = vertical / dpi.DpiScaleY;
                    return new Thickness(x, y, x, y);
                }
            }
            catch (EntryPointNotFoundException) {
                // Pre-Windows 10 1607: fall through to the WPF estimate.
            }
        }
        Thickness border = SystemParameters.WindowResizeBorderThickness;
        return new Thickness(border.Left + 4, border.Top + 4, border.Right + 4, border.Bottom + 4);
    }

    private void OnThemeChanged(object? sender, MuzzleThemeChangedEventArgs e) {
        ApplyDwmAttributes(new WindowInteropHelper(this).Handle);
    }

    private void ApplyDwmAttributes(IntPtr handle) {
        if (handle == IntPtr.Zero) {
            return;
        }
        try {
            int corner = DwmcpRound;
            _ = DwmSetWindowAttribute(handle, DwmaWindowCornerPreference, ref corner, sizeof(int));
            int dark = MuzzleThemeManager.IsDark ? 1 : 0;
            _ = DwmSetWindowAttribute(handle, DwmaUseImmersiveDarkMode, ref dark, sizeof(int));
            Color borderColor = (TryFindResource("Muzzle.Color.SurfaceBorder") is Color themed) ? themed : Color.FromRgb(0xEF, 0xEC, 0xE8);
            int border = borderColor.R | (borderColor.G << 8) | (borderColor.B << 16); // COLORREF 0x00BBGGRR
            _ = DwmSetWindowAttribute(handle, DwmaBorderColor, ref border, sizeof(int));
        }
        catch (DllNotFoundException) {
            // DWM unavailable; corners stay square.
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled) {
        switch (msg) {
            case WmNcHitTest:
                if (IsOverMaximizeButton(lParam)) {
                    SetCaptionHover(true);
                    handled = true;
                    return new IntPtr(HtMaxButton);
                }
                SetCaptionHover(false);
                break;
            case WmNcMouseMove:
                if (wParam.ToInt32() != HtMaxButton) {
                    SetCaptionHover(false);
                }
                break;
            case WmNcMouseLeave:
                SetCaptionHover(false);
                SetCaptionPressed(false);
                break;
            case WmNcLButtonDown:
                if (wParam.ToInt32() == HtMaxButton) {
                    SetCaptionPressed(true);
                    handled = true;
                    return IntPtr.Zero;
                }
                break;
            case WmNcLButtonUp:
                if (wParam.ToInt32() == HtMaxButton) {
                    bool wasPressed = (maximizeButton is not null) && GetIsCaptionPressed(maximizeButton);
                    SetCaptionPressed(false);
                    if (wasPressed) {
                        ToggleMaximize();
                    }
                    handled = true;
                    return IntPtr.Zero;
                }
                break;
        }
        return IntPtr.Zero;
    }

    private bool IsOverMaximizeButton(IntPtr lParam) {
        if ((maximizeButton is null) || !maximizeButton.IsVisible || !maximizeButton.IsEnabled || (PresentationSource.FromVisual(maximizeButton) is null)) {
            return false;
        }
        long value = lParam.ToInt64();
        int x = unchecked((short)(value & 0xFFFF));
        int y = unchecked((short)((value >> 16) & 0xFFFF));
        Point local = maximizeButton.PointFromScreen(new Point(x, y));
        return (local.X >= 0) && (local.Y >= 0) && (local.X < maximizeButton.ActualWidth) && (local.Y < maximizeButton.ActualHeight);
    }

    private void SetCaptionHover(bool value) {
        if ((maximizeButton is not null) && (GetIsCaptionHovered(maximizeButton) != value)) {
            SetIsCaptionHovered(maximizeButton, value);
            if (!value) {
                SetCaptionPressed(false);
            }
        }
    }

    private void SetCaptionPressed(bool value) {
        if ((maximizeButton is not null) && (GetIsCaptionPressed(maximizeButton) != value)) {
            SetIsCaptionPressed(maximizeButton, value);
        }
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetricsForDpi(int index, uint dpi);
}