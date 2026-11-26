using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>Side of the anchor a <see cref="Popover"/> opens on.</summary>
public enum PopoverPlacement {
    /// <summary>Above the anchor.</summary>
    Top,
    /// <summary>Below the anchor (the default).</summary>
    Bottom,
    /// <summary>To the left of the anchor.</summary>
    Left,
    /// <summary>To the right of the anchor.</summary>
    Right
}

/// <summary>What opens a <see cref="Popover"/>.</summary>
public enum PopoverTriggerMode {
    /// <summary>Opens and closes on click (the default); a click outside closes it unless StaysOpen is set.</summary>
    Click,
    /// <summary>Opens after HoverDelay while hovered and stays open while the pointer is over the card.</summary>
    Hover,
    /// <summary>Only <see cref="Popover.IsOpen"/> opens and closes it.</summary>
    Manual
}

/// <summary>
/// Wraps an anchor (its <c>Content</c>) and pops <see cref="PopoverContent"/> open beside it in a rounded card with a
/// directional arrow tail, springing out from the anchor side. Opens on click, on hover, or only via <see cref="IsOpen"/>.
/// </summary>
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
[TemplatePart(Name = BubblePartName, Type = typeof(FrameworkElement))]
public class Popover : ContentControl {
    /// <summary>Identifies the <see cref="Opened"/> routed event.</summary>
    public static readonly RoutedEvent OpenedEvent = EventManager.RegisterRoutedEvent(
        nameof(Opened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Popover));

    /// <summary>Identifies the <see cref="Closed"/> routed event.</summary>
    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Popover));

    /// <summary>Identifies the <see cref="PopoverContent"/> dependency property.</summary>
    public static readonly DependencyProperty PopoverContentProperty = DependencyProperty.Register(
        nameof(PopoverContent), typeof(object), typeof(Popover), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="PopoverContentTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty PopoverContentTemplateProperty = DependencyProperty.Register(
        nameof(PopoverContentTemplate), typeof(DataTemplate), typeof(Popover), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(Popover), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="Placement"/> dependency property.</summary>
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(
        nameof(Placement), typeof(PopoverPlacement), typeof(Popover), new PropertyMetadata(PopoverPlacement.Bottom));

    /// <summary>Identifies the <see cref="TriggerMode"/> dependency property.</summary>
    public static readonly DependencyProperty TriggerModeProperty = DependencyProperty.Register(
        nameof(TriggerMode), typeof(PopoverTriggerMode), typeof(Popover), new PropertyMetadata(PopoverTriggerMode.Click, OnTriggerModeChanged));

    /// <summary>Identifies the <see cref="ShowArrow"/> dependency property.</summary>
    public static readonly DependencyProperty ShowArrowProperty = DependencyProperty.Register(
        nameof(ShowArrow), typeof(bool), typeof(Popover), new PropertyMetadata(true));

    /// <summary>When false (default) a click outside closes it. Hover popovers always manage themselves.</summary>
    public static readonly DependencyProperty StaysOpenProperty = DependencyProperty.Register(
        nameof(StaysOpen), typeof(bool), typeof(Popover), new PropertyMetadata(false, OnTriggerModeChanged));

    /// <summary>Identifies the <see cref="HoverDelay"/> dependency property.</summary>
    public static readonly DependencyProperty HoverDelayProperty = DependencyProperty.Register(
        nameof(HoverDelay), typeof(TimeSpan), typeof(Popover), new PropertyMetadata(TimeSpan.FromMilliseconds(160)));

    private const string PopupPartName = "PART_Popup";
    private const string BubblePartName = "PART_Bubble";

    /// <summary>Distance between the arrow tip and the anchor.</summary>
    private const double Gap = 4;

    /// <summary>The bubble's margin inside the popup (room for the shadow and arrow).</summary>
    private const double BubbleInset = 16;

    /// <summary>How far the arrow sticks out of the card.</summary>
    private const double ArrowDepth = 8;

    /// <summary>How long after a light-dismiss close a release on the anchor is treated as part of that same click.</summary>
    private const long LightDismissWindowMilliseconds = 500;

    private readonly DispatcherTimer openTimer;
    private bool pressedOnAnchor;
    private bool closingOurselves;
    private long lastLightDismissTick = long.MinValue / 2;
    private readonly DispatcherTimer closeTimer;
    private Popup? popup;
    private FrameworkElement? bubble;

    static Popover() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Popover), new FrameworkPropertyMetadata(typeof(Popover)));
        FocusableProperty.OverrideMetadata(typeof(Popover), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="Popover"/> class.</summary>
    public Popover() {
        openTimer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher);
        openTimer.Tick += OnOpenTimerTick;
        closeTimer = new DispatcherTimer(DispatcherPriority.Input, Dispatcher) { Interval = TimeSpan.FromMilliseconds(260) };
        closeTimer.Tick += OnCloseTimerTick;
    }

    /// <summary>Occurs when the popover opens.</summary>
    public event RoutedEventHandler Opened {
        add => AddHandler(OpenedEvent, value);
        remove => RemoveHandler(OpenedEvent, value);
    }

    /// <summary>Occurs when the popover closes.</summary>
    public event RoutedEventHandler Closed {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    /// <summary>Gets or sets the popover content. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? PopoverContent {
        get => GetValue(PopoverContentProperty);
        set => SetValue(PopoverContentProperty, value);
    }

    /// <summary>Gets or sets the popover content template. The default is <see langword="null"/>. This is a dependency property.</summary>
    public DataTemplate? PopoverContentTemplate {
        get => (DataTemplate?)GetValue(PopoverContentTemplateProperty);
        set => SetValue(PopoverContentTemplateProperty, value);
    }

    /// <summary>Gets or sets whether the control is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsOpen {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the placement. The default is <c>PopoverPlacement.Bottom</c>. This is a dependency property.</summary>
    public PopoverPlacement Placement {
        get => (PopoverPlacement)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <summary>Gets or sets the trigger mode. The default is <c>PopoverTriggerMode.Click</c>. This is a dependency property.</summary>
    public PopoverTriggerMode TriggerMode {
        get => (PopoverTriggerMode)GetValue(TriggerModeProperty);
        set => SetValue(TriggerModeProperty, value);
    }

    /// <summary>Gets or sets whether to show the arrow. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowArrow {
        get => (bool)GetValue(ShowArrowProperty);
        set => SetValue(ShowArrowProperty, value);
    }

    /// <summary>Gets or sets whether it stays open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool StaysOpen {
        get => (bool)GetValue(StaysOpenProperty);
        set => SetValue(StaysOpenProperty, value);
    }

    /// <summary>Gets or sets the hover delay. The default is <c>TimeSpan.FromMilliseconds(160)</c>. This is a dependency property.</summary>
    public TimeSpan HoverDelay {
        get => (TimeSpan)GetValue(HoverDelayProperty);
        set => SetValue(HoverDelayProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (popup is not null) {
            popup.Opened -= OnPopupOpened;
            popup.Closed -= OnPopupClosed;
        }
        if (bubble is not null) {
            bubble.MouseEnter -= OnBubbleMouseEnter;
            bubble.MouseLeave -= OnBubbleMouseLeave;
        }
        base.OnApplyTemplate();
        popup = GetTemplateChild(PopupPartName) as Popup;
        bubble = GetTemplateChild(BubblePartName) as FrameworkElement;
        if (popup is not null) {
            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = PlacePopup;
            popup.Opened += OnPopupOpened;
            popup.Closed += OnPopupClosed;
        }
        if (bubble is not null) {
            bubble.MouseEnter += OnBubbleMouseEnter;
            bubble.MouseLeave += OnBubbleMouseLeave;
        }
        UpdateStaysOpen();
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnPreviewMouseLeftButtonDown(e);
        // Only clicks on the anchor count; clicks inside the popup bubble also route through here.
        pressedOnAnchor = (TriggerMode == PopoverTriggerMode.Click) && !IsInsideBubble(e.OriginalSource as DependencyObject);
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e) {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!pressedOnAnchor) {
            return;
        }
        pressedOnAnchor = false;
        if ((TriggerMode != PopoverTriggerMode.Click) || IsInsideBubble(e.OriginalSource as DependencyObject)) {
            return;
        }
        // A light-dismiss popup closes itself when this same press lands outside it; don't reopen it on the release.
        if (!IsOpen && ((Environment.TickCount64 - lastLightDismissTick) < LightDismissWindowMilliseconds)) {
            return;
        }
        // Toggle on release, after the anchor has finished its click: opening during the press let the popup take
        // mouse capture and close again as soon as the button was released.
        bool open = !IsOpen;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => SetOpen(open)));
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e) {
        base.OnLostMouseCapture(e);
        if (Mouse.LeftButton == MouseButtonState.Released) {
            pressedOnAnchor = false;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if ((e.Key == Key.Escape) && IsOpen) {
            SetCurrentValue(IsOpenProperty, false);
            e.Handled = true;
        }
        else if ((TriggerMode == PopoverTriggerMode.Click) && (e.Key is Key.Enter or Key.Space) && !IsInsideBubble(e.OriginalSource as DependencyObject)) {
            SetOpen(!IsOpen);
        }
    }

    private void SetOpen(bool open) {
        if (open == IsOpen) {
            return;
        }
        closingOurselves = !open;
        SetCurrentValue(IsOpenProperty, open);
    }

    /// <inheritdoc />
    protected override void OnMouseEnter(MouseEventArgs e) {
        base.OnMouseEnter(e);
        if (TriggerMode != PopoverTriggerMode.Hover) {
            return;
        }
        closeTimer.Stop();
        if (!IsOpen) {
            openTimer.Interval = HoverDelay;
            openTimer.Start();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e) {
        base.OnMouseLeave(e);
        if (TriggerMode == PopoverTriggerMode.Hover) {
            openTimer.Stop();
            closeTimer.Start();
        }
    }

    private static void OnTriggerModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Popover)d).UpdateStaysOpen();
    }

    private void UpdateStaysOpen() {
        if (popup is not null) {
            popup.StaysOpen = StaysOpen || (TriggerMode != PopoverTriggerMode.Click);
        }
    }

    private bool IsInsideBubble(DependencyObject? source) {
        return (bubble is not null) && (source is Visual visual) && visual.IsDescendantOf(bubble);
    }

    private void OnBubbleMouseEnter(object sender, MouseEventArgs e) {
        closeTimer.Stop();
    }

    private void OnBubbleMouseLeave(object sender, MouseEventArgs e) {
        if (TriggerMode == PopoverTriggerMode.Hover) {
            closeTimer.Start();
        }
    }

    private void OnOpenTimerTick(object? sender, EventArgs e) {
        openTimer.Stop();
        SetCurrentValue(IsOpenProperty, true);
    }

    private void OnCloseTimerTick(object? sender, EventArgs e) {
        closeTimer.Stop();
        if ((bubble is null) || !bubble.IsMouseOver) {
            SetCurrentValue(IsOpenProperty, false);
        }
    }

    private CustomPopupPlacement[] PlacePopup(Size popupSize, Size targetSize, Point offset) {
        // Sizes arrive in device units; scale the layout constants the same way.
        double unit = (bubble is not null) && (bubble.ActualWidth > 0) ? popupSize.Width / (bubble.ActualWidth + (2 * BubbleInset)) : 1;
        double edge = (BubbleInset - ArrowDepth - Gap) * unit;
        Point point = Placement switch {
            PopoverPlacement.Top => new Point((targetSize.Width - popupSize.Width) / 2, -popupSize.Height + edge),
            PopoverPlacement.Left => new Point(-popupSize.Width + edge, (targetSize.Height - popupSize.Height) / 2),
            PopoverPlacement.Right => new Point(targetSize.Width - edge, (targetSize.Height - popupSize.Height) / 2),
            _ => new Point((targetSize.Width - popupSize.Width) / 2, targetSize.Height - edge)
        };
        PopupPrimaryAxis axis = Placement is PopoverPlacement.Left or PopoverPlacement.Right ? PopupPrimaryAxis.Horizontal : PopupPrimaryAxis.Vertical;
        return [new CustomPopupPlacement(point, axis)];
    }

    private void OnPopupOpened(object? sender, EventArgs e) {
        RaiseEvent(new RoutedEventArgs(OpenedEvent, this));
        if ((bubble is null) || !Jelly.IsMotionEnabled(this)) {
            return;
        }
        bubble.RenderTransformOrigin = Placement switch {
            PopoverPlacement.Top => new Point(0.5, 1),
            PopoverPlacement.Left => new Point(1, 0.5),
            PopoverPlacement.Right => new Point(0, 0.5),
            _ => new Point(0.5, 0)
        };
        if (bubble.RenderTransform is not ScaleTransform { IsFrozen: false } scale) {
            scale = new ScaleTransform();
            bubble.RenderTransform = scale;
        }
        DoubleAnimation pop = new(0.8, 1, TimeSpan.FromMilliseconds(620)) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellyKey) };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        bubble.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(150)));
    }

    private void OnPopupClosed(object? sender, EventArgs e) {
        if (!closingOurselves) {
            lastLightDismissTick = Environment.TickCount64;
        }
        closingOurselves = false;
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }
}