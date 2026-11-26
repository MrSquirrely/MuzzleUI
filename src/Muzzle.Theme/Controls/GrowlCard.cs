using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>
/// One toast. Slides in from the right while springing up from 0.85×, counts down (paused while hovered),
/// then squishes to 0.9×, fades, and collapses its height so the toasts below glide up into place.
/// </summary>
[TemplatePart(Name = CloseButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = ConfirmButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = CancelButtonPartName, Type = typeof(ButtonBase))]
public class GrowlCard : Control {
    private const string CloseButtonPartName = "PART_CloseButton";
    private const string ConfirmButtonPartName = "PART_ConfirmButton";
    private const string CancelButtonPartName = "PART_CancelButton";

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(GrowlCard), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Message"/> dependency property.</summary>
    public static readonly DependencyProperty MessageProperty = DependencyProperty.Register(
        nameof(Message), typeof(string), typeof(GrowlCard), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Type"/> dependency property.</summary>
    public static readonly DependencyProperty TypeProperty = DependencyProperty.Register(
        nameof(Type), typeof(GrowlType), typeof(GrowlCard), new PropertyMetadata(GrowlType.Info));

    /// <summary>Identifies the <see cref="ShowCloseButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCloseButtonProperty = DependencyProperty.Register(
        nameof(ShowCloseButton), typeof(bool), typeof(GrowlCard), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ConfirmText"/> dependency property.</summary>
    public static readonly DependencyProperty ConfirmTextProperty = DependencyProperty.Register(
        nameof(ConfirmText), typeof(string), typeof(GrowlCard), new PropertyMetadata("Yes"));

    /// <summary>Identifies the <see cref="CancelText"/> dependency property.</summary>
    public static readonly DependencyProperty CancelTextProperty = DependencyProperty.Register(
        nameof(CancelText), typeof(string), typeof(GrowlCard), new PropertyMetadata("No"));

    /// <summary>Remaining share of the auto-close time, 1 → 0; drives the countdown line.</summary>
    public static readonly DependencyProperty CountdownProgressProperty = DependencyProperty.Register(
        nameof(CountdownProgress), typeof(double), typeof(GrowlCard), new PropertyMetadata(1.0));

    private static readonly DependencyPropertyKey IsCountingDownPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsCountingDown), typeof(bool), typeof(GrowlCard), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsCountingDown"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsCountingDownProperty = IsCountingDownPropertyKey.DependencyProperty;

    private static readonly TimeSpan ShortWait = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan LongWait = TimeSpan.FromSeconds(7);

    private readonly GrowlInfo info;
    private readonly ScaleTransform scale = new(1, 1);
    private readonly TranslateTransform slide = new();
    private Storyboard? countdown;
    private ButtonBase? closeButton;
    private ButtonBase? confirmButton;
    private ButtonBase? cancelButton;

    static GrowlCard() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(GrowlCard), new FrameworkPropertyMetadata(typeof(GrowlCard)));
    }

    /// <summary>Initializes a new instance of the <see cref="GrowlCard"/> class.</summary>
    public GrowlCard() : this(new GrowlInfo()) {
    }

    /// <summary>Initializes a toast card from its description.</summary>
    /// <param name="growlInfo">What the toast shows and how it behaves.</param>
    public GrowlCard(GrowlInfo growlInfo) {
        info = growlInfo;
        Title = string.IsNullOrWhiteSpace(info.Title) ? info.Type.ToString() : info.Title;
        Message = info.Message;
        Type = info.Type;
        ShowCloseButton = info.ShowCloseButton;
        ConfirmText = info.ConfirmText;
        CancelText = info.CancelText;

        TransformGroup transforms = new();
        transforms.Children.Add(scale);
        transforms.Children.Add(slide);
        RenderTransform = transforms;
        RenderTransformOrigin = new Point(0.5, 0.5);

        AutomationProperties.SetName(this, Title + ": " + Message);
        AutomationProperties.SetLiveSetting(this, info.Type is GrowlType.Error or GrowlType.Fatal ? AutomationLiveSetting.Assertive : AutomationLiveSetting.Polite);

        Loaded += OnLoaded;
        MouseEnter += OnMouseEnter;
        MouseLeave += OnMouseLeave;
    }

    /// <summary>Raised after the exit animation, once the toast has left its container.</summary>
    public event EventHandler? Closed;

    /// <summary>Gets or sets the title. The default is an empty string. This is a dependency property.</summary>
    public string Title {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the message. The default is an empty string. This is a dependency property.</summary>
    public string Message {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    /// <summary>Gets or sets the type. The default is <c>GrowlType.Info</c>. This is a dependency property.</summary>
    public GrowlType Type {
        get => (GrowlType)GetValue(TypeProperty);
        set => SetValue(TypeProperty, value);
    }

    /// <summary>Gets or sets whether to show the close button. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowCloseButton {
        get => (bool)GetValue(ShowCloseButtonProperty);
        set => SetValue(ShowCloseButtonProperty, value);
    }

    /// <summary>Gets or sets the confirm text. The default is <c>&quot;Yes&quot;</c>. This is a dependency property.</summary>
    public string ConfirmText {
        get => (string)GetValue(ConfirmTextProperty);
        set => SetValue(ConfirmTextProperty, value);
    }

    /// <summary>Gets or sets the cancel text. The default is <c>&quot;No&quot;</c>. This is a dependency property.</summary>
    public string CancelText {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    /// <summary>Gets or sets the countdown progress. The default is <c>1.0</c>. This is a dependency property.</summary>
    public double CountdownProgress {
        get => (double)GetValue(CountdownProgressProperty);
        set => SetValue(CountdownProgressProperty, value);
    }

    /// <summary>Gets whether the counting is down. This is a read-only dependency property.</summary>
    public bool IsCountingDown => (bool)GetValue(IsCountingDownProperty);

    /// <summary>Gets whether the card is playing its exit animation.</summary>
    public bool IsClosing { get; private set; }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (closeButton is not null) {
            closeButton.Click -= OnCloseClick;
        }
        if (confirmButton is not null) {
            confirmButton.Click -= OnConfirmClick;
        }
        if (cancelButton is not null) {
            cancelButton.Click -= OnCancelClick;
        }
        base.OnApplyTemplate();
        closeButton = GetTemplateChild(CloseButtonPartName) as ButtonBase;
        confirmButton = GetTemplateChild(ConfirmButtonPartName) as ButtonBase;
        cancelButton = GetTemplateChild(CancelButtonPartName) as ButtonBase;
        if (closeButton is not null) {
            closeButton.Click += OnCloseClick;
        }
        if (confirmButton is not null) {
            confirmButton.Click += OnConfirmClick;
        }
        if (cancelButton is not null) {
            cancelButton.Click += OnCancelClick;
        }
    }

    /// <summary>Dismisses the toast (an Ask toast reports false).</summary>
    public void Close() {
        Close(false);
    }

    internal void Close(bool confirmed) {
        if (IsClosing) {
            return;
        }
        IsClosing = true;
        countdown?.Stop(this);
        if (info.Type == GrowlType.Ask) {
            info.Callback?.Invoke(confirmed);
        }
        PlayExit();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (!e.Handled && (e.Key == Key.Escape)) {
            Close(false);
            e.Handled = true;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        Loaded -= OnLoaded;
        PlayEnter();
        StartCountdown();
        AutomationPeer? peer = UIElementAutomationPeer.FromElement(this) ?? UIElementAutomationPeer.CreatePeerForElement(this);
        peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e) {
        countdown?.Pause(this);
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) {
        if (!IsClosing) {
            countdown?.Resume(this);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) {
        Close(false);
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e) {
        Close(true);
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) {
        Close(false);
    }

    private TimeSpan ResolveWaitTime() {
        if (info.StaysOpen || (info.Type is GrowlType.Ask or GrowlType.Fatal)) {
            return TimeSpan.Zero;
        }
        return info.WaitTime ?? (info.Type is GrowlType.Warning or GrowlType.Error ? LongWait : ShortWait);
    }

    private void StartCountdown() {
        TimeSpan wait = ResolveWaitTime();
        if (wait <= TimeSpan.Zero) {
            SetValue(IsCountingDownPropertyKey, false);
            return;
        }
        SetValue(IsCountingDownPropertyKey, true);
        DoubleAnimation progress = new(1, 0, wait);
        Storyboard.SetTarget(progress, this);
        Storyboard.SetTargetProperty(progress, new PropertyPath(CountdownProgressProperty));
        countdown = new Storyboard();
        countdown.Children.Add(progress);
        countdown.Completed += OnCountdownCompleted;
        countdown.Begin(this, true);
        if (IsMouseOver) {
            countdown.Pause(this);
        }
    }

    private void OnCountdownCompleted(object? sender, EventArgs e) {
        Close(false);
    }

    private void PlayEnter() {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        double distance = (ActualWidth > 0 ? ActualWidth : 360) + 32;
        slide.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(distance, 0, TimeSpan.FromMilliseconds(560)) {
            EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.BackKey)
        });
        DoubleAnimation grow = new(0.85, 1, TimeSpan.FromMilliseconds(720)) {
            EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellySoftKey)
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200)));
    }

    private void PlayExit() {
        if (!Jelly.IsMotionEnabled(this) || !IsLoaded) {
            Finish();
            return;
        }
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.SquishKey);
        DoubleAnimation shrink = new(0.9, TimeSpan.FromMilliseconds(170)) { EasingFunction = squish };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(190)));

        // Collapse the slot so neighbouring toasts reflow smoothly instead of jumping.
        TimeSpan delay = TimeSpan.FromMilliseconds(150);
        Duration collapse = TimeSpan.FromMilliseconds(280);
        IEasingFunction glide = MuzzleEasings.Get(this, MuzzleEasings.GlideKey);
        DoubleAnimation height = new(ActualHeight, 0, collapse) { BeginTime = delay, EasingFunction = glide };
        height.Completed += OnExitCompleted;
        BeginAnimation(HeightProperty, height);
        BeginAnimation(MarginProperty, new ThicknessAnimation(new Thickness(0), collapse) { BeginTime = delay, EasingFunction = glide });
    }

    private void OnExitCompleted(object? sender, EventArgs e) {
        Finish();
    }

    private void Finish() {
        if (Parent is Panel panel) {
            panel.Children.Remove(this);
        }
        Closed?.Invoke(this, EventArgs.Empty);
    }
}