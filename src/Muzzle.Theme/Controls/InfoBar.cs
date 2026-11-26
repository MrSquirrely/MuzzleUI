using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>Severity of an <see cref="InfoBar"/>.</summary>
public enum InfoBarSeverity {
    /// <summary>Informational: blue tint and an "i" icon (the default).</summary>
    Info,
    /// <summary>Success: mint tint and a check icon.</summary>
    Success,
    /// <summary>Warning: butter tint and an exclamation icon.</summary>
    Warning,
    /// <summary>Error: coral tint and a cross icon.</summary>
    Error
}

/// <summary>
/// An inline, dismissible callout. Pastel tint and icon follow <see cref="Severity"/>; <see cref="ActionContent"/>
/// hosts a button or link; extra <c>Content</c> renders under the message. Closing squishes the banner and collapses
/// its height so the layout below glides up; setting <see cref="IsOpen"/> back to true springs it back in.
/// </summary>
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
public class InfoBar : ContentControl {
    /// <summary>Identifies the <see cref="Closed"/> routed event.</summary>
    public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent(
        nameof(Closed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InfoBar));

    /// <summary>Identifies the <see cref="Severity"/> dependency property.</summary>
    public static readonly DependencyProperty SeverityProperty = DependencyProperty.Register(
        nameof(Severity), typeof(InfoBarSeverity), typeof(InfoBar), new PropertyMetadata(InfoBarSeverity.Info, OnSeverityChanged));

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Message"/> dependency property.</summary>
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(InfoBar), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(InfoBar), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    /// <summary>Identifies the <see cref="IsClosable"/> dependency property.</summary>
    public static readonly DependencyProperty IsClosableProperty = DependencyProperty.Register(
        nameof(IsClosable), typeof(bool), typeof(InfoBar), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ActionContent"/> dependency property.</summary>
    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent), typeof(object), typeof(InfoBar), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsIconVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsIconVisibleProperty = DependencyProperty.Register(
        nameof(IsIconVisible), typeof(bool), typeof(InfoBar), new PropertyMetadata(true));

    private const string CloseButtonPartName = "PART_CloseButton";

    private readonly ScaleTransform scale = new(1, 1);
    private ButtonBase? closeButton;
    private Thickness restingMargin;
    private bool marginCaptured;

    static InfoBar() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(InfoBar), new FrameworkPropertyMetadata(typeof(InfoBar)));
        FocusableProperty.OverrideMetadata(typeof(InfoBar), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="InfoBar"/> class.</summary>
    public InfoBar() {
        RenderTransform = scale;
        RenderTransformOrigin = new Point(0.5, 0.5);
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
        Loaded += OnLoaded;
    }

    /// <summary>Occurs after the bar has finished its close animation (its height has folded away).</summary>
    public event RoutedEventHandler Closed {
        add => AddHandler(ClosedEvent, value);
        remove => RemoveHandler(ClosedEvent, value);
    }

    /// <summary>Gets or sets the severity. The default is <c>InfoBarSeverity.Info</c>. This is a dependency property.</summary>
    public InfoBarSeverity Severity {
        get => (InfoBarSeverity)GetValue(SeverityProperty);
        set => SetValue(SeverityProperty, value);
    }

    /// <summary>Gets or sets the title. The default is <see langword="null"/>. This is a dependency property.</summary>
    public string? Title {
        get => (string?)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the message. The default is <see langword="null"/>. This is a dependency property.</summary>
    public string? Message {
        get => (string?)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets whether the control is open. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsOpen {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets whether the control is closable. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsClosable {
        get => (bool)GetValue(IsClosableProperty);
        set => SetValue(IsClosableProperty, value);
    }

    /// <summary>Gets or sets the action content. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? ActionContent {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }

    /// <summary>Gets or sets whether the icon is visible. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsIconVisible {
        get => (bool)GetValue(IsIconVisibleProperty);
        set => SetValue(IsIconVisibleProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (closeButton is not null) {
            closeButton.Click -= OnCloseClick;
        }
        base.OnApplyTemplate();
        closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;
        if (closeButton is not null) {
            closeButton.Click += OnCloseClick;
        }
    }

    private static void OnSeverityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        InfoBarSeverity severity = (InfoBarSeverity)e.NewValue;
        AutomationProperties.SetLiveSetting(d, severity == InfoBarSeverity.Error ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        InfoBar bar = (InfoBar)d;
        if (e.NewValue is true) {
            bar.Open();
        }
        else {
            bar.Collapse();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        CaptureMargin();
        if (!IsOpen) {
            Visibility = Visibility.Collapsed;
        }
    }

    private void CaptureMargin() {
        if (!marginCaptured) {
            restingMargin = Margin;
            marginCaptured = true;
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) {
        SetCurrentValue(IsOpenProperty, false);
    }

    private void Open() {
        CaptureMargin();
        BeginAnimation(HeightProperty, null);
        BeginAnimation(MarginProperty, null);
        Margin = restingMargin;
        Visibility = Visibility.Visible;
        if (!IsLoaded || !Jelly.IsMotionEnabled(this)) {
            BeginAnimation(OpacityProperty, null);
            Opacity = 1;
            return;
        }
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        DoubleAnimation grow = new(0.94, 1, TimeSpan.FromMilliseconds(600)) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellyKey) };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
    }

    private void Collapse() {
        CaptureMargin();
        if (!IsLoaded || !Jelly.IsMotionEnabled(this)) {
            Visibility = Visibility.Collapsed;
            RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
            return;
        }
        // Squish (wider, flatter), fade, then fold the slot away so the content below slides up.
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.SquishKey);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(1.03, TimeSpan.FromMilliseconds(160)) { EasingFunction = squish });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.82, TimeSpan.FromMilliseconds(160)) { EasingFunction = squish });
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
        TimeSpan delay = TimeSpan.FromMilliseconds(140);
        Duration fold = TimeSpan.FromMilliseconds(260);
        IEasingFunction glide = MuzzleEasings.Get(this, MuzzleEasings.GlideKey);
        DoubleAnimation height = new(ActualHeight, 0, fold) { BeginTime = delay, EasingFunction = glide };
        height.Completed += OnCollapseCompleted;
        BeginAnimation(HeightProperty, height);
        BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(0), fold) { BeginTime = delay, EasingFunction = glide });
    }

    private void OnCollapseCompleted(object? sender, EventArgs e) {
        if (IsOpen) {
            return;
        }
        Visibility = Visibility.Collapsed;
        BeginAnimation(HeightProperty, null);
        BeginAnimation(MarginProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        RaiseEvent(new RoutedEventArgs(ClosedEvent, this));
    }
}