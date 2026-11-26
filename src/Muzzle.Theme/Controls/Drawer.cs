using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A panel that slides out from an edge of its host, overshoots its resting edge slightly and settles with a
/// jelly bounce, over a synchronised backdrop fade.
/// <para>Declare it inside a <see cref="DrawerContainer"/> (or any Grid) and bind <see cref="IsOpen"/>, or open one
/// from code with <see cref="Show(object, Dock, object?, string?)"/>.</para>
/// </summary>
[TemplatePart(Name = PanelPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = MaskPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
public class Drawer : ContentControl {
    /// <summary>Extra panel length hidden beyond the docked edge so the overshoot never reveals a gap.</summary>
    public const double BounceGutter = 24;

    private const string PanelPartName = "PART_Panel";
    private const string MaskPartName = "PART_Mask";
    private const string CloseButtonPartName = "PART_CloseButton";

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen),
        typeof(bool),
        typeof(Drawer),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    /// <summary>Identifies the <see cref="Dock"/> dependency property.</summary>
    public static readonly DependencyProperty DockProperty = DependencyProperty.Register(
        nameof(Dock), typeof(Dock), typeof(Drawer), new PropertyMetadata(Dock.Right));

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
        nameof(Header), typeof(object), typeof(Drawer), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
        nameof(HeaderTemplate), typeof(DataTemplate), typeof(Drawer), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="ShowMask"/> dependency property.</summary>
    public static readonly DependencyProperty ShowMaskProperty = DependencyProperty.Register(
        nameof(ShowMask), typeof(bool), typeof(Drawer), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="MaskCanClose"/> dependency property.</summary>
    public static readonly DependencyProperty MaskCanCloseProperty = DependencyProperty.Register(
        nameof(MaskCanClose), typeof(bool), typeof(Drawer), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="CloseButtonVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty CloseButtonVisibilityProperty = DependencyProperty.Register(
        nameof(CloseButtonVisibility), typeof(Visibility), typeof(Drawer), new PropertyMetadata(Visibility.Visible));

    /// <summary>Width (Left/Right) or height (Top/Bottom) of the visible panel.</summary>
    public static readonly DependencyProperty DrawerSizeProperty = DependencyProperty.Register(
        nameof(DrawerSize),
        typeof(double),
        typeof(Drawer),
        new PropertyMetadata(360.0),
        static value => value is double size && double.IsFinite(size) && (size >= 0));

    /// <summary>Names a <see cref="DrawerContainer"/> so <see cref="Show(object, Dock, object?, string?)"/> can target it.</summary>
    public static readonly DependencyProperty TokenProperty = DependencyProperty.RegisterAttached(
        "Token",
        typeof(string),
        typeof(Drawer),
        new PropertyMetadata(null));

    private FrameworkElement? panel;
    private FrameworkElement? mask;
    private ButtonBase? closeButton;
    private TranslateTransform? slide;
    private IInputElement? restoreFocus;

    static Drawer() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Drawer), new FrameworkPropertyMetadata(typeof(Drawer)));
        FocusableProperty.OverrideMetadata(typeof(Drawer), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="Drawer"/> class.</summary>
    public Drawer() {
        Visibility = Visibility.Collapsed;
        Loaded += OnLoaded;
    }

    /// <summary>Occurs after the drawer has finished opening.</summary>
    public event EventHandler? Opened;

    /// <summary>Occurs after the drawer has finished closing.</summary>
    public event EventHandler? Closed;

    /// <summary>Gets or sets whether the control is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsOpen {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the dock. The default is <c>Dock.Right</c>. This is a dependency property.</summary>
    public Dock Dock {
        get => (Dock)GetValue(DockProperty);
        set => SetValue(DockProperty, value);
    }

    /// <summary>Gets or sets the header. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? Header {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>Gets or sets the header template. The default is <see langword="null"/>. This is a dependency property.</summary>
    public DataTemplate? HeaderTemplate {
        get => (DataTemplate?)GetValue(HeaderTemplateProperty);
        set => SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Gets or sets whether to show the mask. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowMask {
        get => (bool)GetValue(ShowMaskProperty);
        set => SetValue(ShowMaskProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether mask can close. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool MaskCanClose {
        get => (bool)GetValue(MaskCanCloseProperty);
        set => SetValue(MaskCanCloseProperty, value);
    }

    /// <summary>Gets or sets the close button visibility. The default is <c>Visibility.Visible</c>. This is a dependency property.</summary>
    public Visibility CloseButtonVisibility {
        get => (Visibility)GetValue(CloseButtonVisibilityProperty);
        set => SetValue(CloseButtonVisibilityProperty, value);
    }

    /// <summary>Gets or sets the drawer size. The default is <c>360.0</c>. This is a dependency property.</summary>
    public double DrawerSize {
        get => (double)GetValue(DrawerSizeProperty);
        set => SetValue(DrawerSizeProperty, value);
    }

    /// <summary>Drawers created by <see cref="Show(object, Dock, object?, string?)"/> remove themselves after closing.</summary>
    internal bool IsTransient { get; init; }

    /// <summary>Gets the value of the <c>Token</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Token</c> value.</returns>
    public static string? GetToken(DependencyObject element) {
        return (string?)element.GetValue(TokenProperty);
    }

    /// <summary>Sets the value of the <c>Token</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetToken(DependencyObject element, string? value) {
        element.SetValue(TokenProperty, value);
    }

    /// <summary>
    /// Opens <paramref name="content"/> in a new drawer over the active window (or in the <see cref="DrawerContainer"/>
    /// with the matching token). Close it with <c>drawer.IsOpen = false</c>; it removes itself afterwards.
    /// </summary>
    public static Drawer Show(object content, Dock dock = Dock.Right, object? header = null, string? token = null) {
        DrawerContainer host = string.IsNullOrEmpty(token)
            ? OverlayHost.Get(OverlayHost.RequireActiveWindow()).Drawers
            : DrawerContainer.Registry.Find(token, GetToken) ?? throw new InvalidOperationException($"No loaded DrawerContainer has mz:Drawer.Token=\"{token}\".");
        Drawer drawer = new() { Content = content, Dock = dock, Header = header, IsTransient = true };
        host.Children.Add(drawer);
        drawer.IsOpen = true;
        return drawer;
    }

    /// <summary>Finds the drawer that contains <paramref name="element"/>.</summary>
    public static Drawer? GetDrawer(DependencyObject element) {
        DependencyObject? current = element;
        while (current is not null) {
            if (current is Drawer drawer) {
                return drawer;
            }
            DependencyObject? parent = current is Visual ? VisualTreeHelper.GetParent(current) : null;
            current = parent ?? LogicalTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (mask is not null) {
            mask.MouseLeftButtonDown -= OnMaskMouseDown;
        }
        if (closeButton is not null) {
            closeButton.Click -= OnCloseClick;
        }
        base.OnApplyTemplate();
        panel = GetTemplateChild(PanelPartName) as FrameworkElement;
        mask = GetTemplateChild(MaskPartName) as FrameworkElement;
        closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;
        if (panel is not null) {
            slide = new TranslateTransform();
            panel.RenderTransform = slide;
        }
        if (mask is not null) {
            mask.MouseLeftButtonDown += OnMaskMouseDown;
        }
        if (closeButton is not null) {
            closeButton.Click += OnCloseClick;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (!e.Handled && (e.Key == Key.Escape) && IsOpen) {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
        }
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Drawer drawer = (Drawer)d;
        if (!drawer.IsLoaded) {
            return;
        }
        if (e.NewValue is true) {
            drawer.Open();
        }
        else {
            drawer.Close();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if (IsOpen && (Visibility != Visibility.Visible)) {
            Open();
        }
    }

    private void OnMaskMouseDown(object sender, MouseButtonEventArgs e) {
        e.Handled = true;
        if (MaskCanClose) {
            SetCurrentValue(IsOpenProperty, false);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) {
        SetCurrentValue(IsOpenProperty, false);
    }

    private void Open() {
        restoreFocus = Keyboard.FocusedElement;
        Visibility = Visibility.Visible;
        ApplyTemplate();
        UpdateLayout();
        if ((slide is null) || (panel is null)) {
            Opened?.Invoke(this, EventArgs.Empty);
            return;
        }
        Point hidden = HiddenOffset();
        if (Jelly.IsMotionEnabled(this)) {
            IEasingFunction spring = MuzzleEasings.Get(this, MuzzleEasings.BackSoftKey);
            Duration duration = TimeSpan.FromMilliseconds(620);
            slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(hidden.X, 0, duration) { EasingFunction = spring });
            slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(hidden.Y, 0, duration) { EasingFunction = spring });
            mask?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(320)));
        }
        else {
            StopAnimations();
            slide.X = 0;
            slide.Y = 0;
            if (mask is not null) {
                mask.Opacity = 1;
            }
        }
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusFirst));
        Opened?.Invoke(this, EventArgs.Empty);
    }

    private void Close() {
        if ((slide is null) || (panel is null) || !Jelly.IsMotionEnabled(this)) {
            Finish();
            return;
        }
        Point hidden = HiddenOffset();
        IEasingFunction tuck = MuzzleEasings.Get(this, MuzzleEasings.BackInKey);
        Duration duration = TimeSpan.FromMilliseconds(320);
        DoubleAnimation slideX = new(hidden.X, duration) { EasingFunction = tuck };
        slideX.Completed += OnCloseAnimationCompleted;
        slide.BeginAnimation(TranslateTransform.XProperty, slideX);
        slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(hidden.Y, duration) { EasingFunction = tuck });
        mask?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(280)));
    }

    private void OnCloseAnimationCompleted(object? sender, EventArgs e) {
        if (!IsOpen) {
            Finish();
        }
    }

    private void Finish() {
        bool hadFocus = IsKeyboardFocusWithin;
        Visibility = Visibility.Collapsed;
        StopAnimations();
        if (hadFocus && (restoreFocus is UIElement { IsVisible: true } previous)) {
            previous.Focus();
        }
        Closed?.Invoke(this, EventArgs.Empty);
        if (IsTransient && (Parent is Panel host)) {
            host.Children.Remove(this);
        }
    }

    private void StopAnimations() {
        slide?.BeginAnimation(TranslateTransform.XProperty, null);
        slide?.BeginAnimation(TranslateTransform.YProperty, null);
        mask?.BeginAnimation(OpacityProperty, null);
    }

    private void FocusFirst() {
        if ((panel is not null) && IsOpen && !panel.IsKeyboardFocusWithin) {
            panel.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private Point HiddenOffset() {
        double width = panel?.ActualWidth ?? 0;
        double height = panel?.ActualHeight ?? 0;
        return Dock switch {
            Dock.Left => new Point(-width, 0),
            Dock.Top => new Point(0, -height),
            Dock.Bottom => new Point(0, height),
            _ => new Point(width, 0)
        };
    }
}