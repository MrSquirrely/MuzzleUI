using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Shows one item at a time and slides between them with a springy BackEase overshoot. Items come from
/// <c>ItemsSource</c> (shown through <c>ItemTemplate</c>) or from direct children. Optional auto-play (paused while
/// the pointer or keyboard focus is inside), bouncing dot indicators and pill previous / next buttons.
/// </summary>
[TemplatePart(Name = ViewportPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PresenterAPartName, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PresenterBPartName, Type = typeof(ContentPresenter))]
[TemplatePart(Name = IndicatorsPartName, Type = typeof(Panel))]
[TemplatePart(Name = PreviousButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = NextButtonPartName, Type = typeof(ButtonBase))]
public class Carousel : ItemsControl {
    /// <summary>Name of the clipping viewport template part that hosts the two sliding presenters.</summary>
    public const string ViewportPartName = "PART_Viewport";
    /// <summary>Name of the first content presenter template part (the two take turns as incoming and outgoing slide).</summary>
    public const string PresenterAPartName = "PART_PresenterA";
    /// <summary>Name of the second content presenter template part.</summary>
    public const string PresenterBPartName = "PART_PresenterB";
    /// <summary>Name of the panel template part that receives the dot indicators.</summary>
    public const string IndicatorsPartName = "PART_Indicators";
    /// <summary>Name of the previous-item button template part.</summary>
    public const string PreviousButtonPartName = "PART_PreviousButton";
    /// <summary>Name of the next-item button template part.</summary>
    public const string NextButtonPartName = "PART_NextButton";

    /// <summary>Identifies the <see cref="SelectedIndex"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedIndexProperty = DependencyProperty.Register(
        nameof(SelectedIndex), typeof(int), typeof(Carousel),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedIndexChanged, CoerceSelectedIndex));

    private static readonly DependencyPropertyKey SelectedItemPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SelectedItem), typeof(object), typeof(Carousel), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="SelectedItem"/> read-only dependency property.</summary>
    public static readonly DependencyProperty SelectedItemProperty = SelectedItemPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="AutoPlay"/> dependency property.</summary>
    public static readonly DependencyProperty AutoPlayProperty = DependencyProperty.Register(
        nameof(AutoPlay), typeof(bool), typeof(Carousel), new PropertyMetadata(false, OnTimerSettingChanged));

    /// <summary>Identifies the <see cref="Interval"/> dependency property.</summary>
    public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
        nameof(Interval), typeof(TimeSpan), typeof(Carousel), new PropertyMetadata(TimeSpan.FromSeconds(4), OnTimerSettingChanged),
        static value => value is TimeSpan interval && (interval > TimeSpan.Zero));

    /// <summary>Identifies the <see cref="ShowIndicators"/> dependency property.</summary>
    public static readonly DependencyProperty ShowIndicatorsProperty = DependencyProperty.Register(
        nameof(ShowIndicators), typeof(bool), typeof(Carousel), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="ShowNavigationButtons"/> dependency property.</summary>
    public static readonly DependencyProperty ShowNavigationButtonsProperty = DependencyProperty.Register(
        nameof(ShowNavigationButtons), typeof(bool), typeof(Carousel), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="IsLooping"/> dependency property.</summary>
    public static readonly DependencyProperty IsLoopingProperty = DependencyProperty.Register(
        nameof(IsLooping), typeof(bool), typeof(Carousel), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration), typeof(TimeSpan), typeof(Carousel), new PropertyMetadata(TimeSpan.FromMilliseconds(620)));

    /// <summary>Identifies the <see cref="SelectedIndexChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectedIndexChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedIndexChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<int>), typeof(Carousel));

    private readonly List<Border> indicators = [];
    private DispatcherTimer? timer;
    private FrameworkElement? viewport;
    private ContentPresenter? front;
    private ContentPresenter? back;
    private Panel? indicatorPanel;
    private ButtonBase? previousButton;
    private ButtonBase? nextButton;
    private int pendingDirection;

    static Carousel() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Carousel), new FrameworkPropertyMetadata(typeof(Carousel)));
        FocusableProperty.OverrideMetadata(typeof(Carousel), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Initializes a new instance of the <see cref="Carousel"/> class.</summary>
    public Carousel() {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Occurs after <see cref="SelectedIndex"/> changes (with the old and new index).</summary>
    public event RoutedPropertyChangedEventHandler<int> SelectedIndexChanged {
        add => AddHandler(SelectedIndexChangedEvent, value);
        remove => RemoveHandler(SelectedIndexChangedEvent, value);
    }

    /// <summary>Gets or sets the selected index. The default is <c>0</c>. This is a dependency property.</summary>
    public int SelectedIndex {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    /// <summary>Gets the item at <see cref="SelectedIndex"/>, or <see langword="null"/> when there are no items. This is a read-only dependency property.</summary>
    public object? SelectedItem => GetValue(SelectedItemProperty);

    /// <summary>Advance automatically every <see cref="Interval"/>.</summary>
    public bool AutoPlay {
        get => (bool)GetValue(AutoPlayProperty);
        set => SetValue(AutoPlayProperty, value);
    }

    /// <summary>Gets or sets the interval. The default is <c>TimeSpan.FromSeconds(4)</c>. This is a dependency property.</summary>
    public TimeSpan Interval {
        get => (TimeSpan)GetValue(IntervalProperty);
        set => SetValue(IntervalProperty, value);
    }

    /// <summary>Gets or sets whether to show the indicators. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowIndicators {
        get => (bool)GetValue(ShowIndicatorsProperty);
        set => SetValue(ShowIndicatorsProperty, value);
    }

    /// <summary>Gets or sets whether to show the navigation buttons. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowNavigationButtons {
        get => (bool)GetValue(ShowNavigationButtonsProperty);
        set => SetValue(ShowNavigationButtonsProperty, value);
    }

    /// <summary>Wrap from the last item to the first and back (default true).</summary>
    public bool IsLooping {
        get => (bool)GetValue(IsLoopingProperty);
        set => SetValue(IsLoopingProperty, value);
    }

    /// <summary>Gets or sets the transition duration. The default is <c>TimeSpan.FromMilliseconds(620)</c>. This is a dependency property.</summary>
    public TimeSpan TransitionDuration {
        get => (TimeSpan)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <summary>Slides to the next item (wrapping when <see cref="IsLooping"/>).</summary>
    public void Next() {
        Step(1);
    }

    /// <summary>Slides to the previous item (wrapping when <see cref="IsLooping"/>).</summary>
    public void Previous() {
        Step(-1);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (previousButton is not null) {
            previousButton.Click -= OnPreviousClick;
        }
        if (nextButton is not null) {
            nextButton.Click -= OnNextClick;
        }
        if (viewport is not null) {
            viewport.SizeChanged -= OnViewportSizeChanged;
        }
        base.OnApplyTemplate();
        viewport = GetTemplateChild(ViewportPartName) as FrameworkElement;
        front = GetTemplateChild(PresenterAPartName) as ContentPresenter;
        back = GetTemplateChild(PresenterBPartName) as ContentPresenter;
        indicatorPanel = GetTemplateChild(IndicatorsPartName) as Panel;
        previousButton = GetTemplateChild(PreviousButtonPartName) as ButtonBase;
        nextButton = GetTemplateChild(NextButtonPartName) as ButtonBase;
        if (previousButton is not null) {
            previousButton.Click += OnPreviousClick;
        }
        if (nextButton is not null) {
            nextButton.Click += OnNextClick;
        }
        if (viewport is not null) {
            viewport.SizeChanged += OnViewportSizeChanged;
        }
        foreach (ContentPresenter? presenter in (ContentPresenter?[])[front, back]) {
            if (presenter is not null) {
                presenter.RenderTransform = new TranslateTransform();
            }
        }
        if (back is not null) {
            back.Visibility = Visibility.Collapsed;
        }
        RebuildIndicators();
        ShowCurrent(0);
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
        base.OnItemsChanged(e);
        CoerceValue(SelectedIndexProperty);
        RebuildIndicators();
        ShowCurrent(0);
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (e.Handled) {
            return;
        }
        switch (e.Key) {
            case Key.Left:
                Previous();
                e.Handled = true;
                break;
            case Key.Right:
                Next();
                e.Handled = true;
                break;
            case Key.Home when Items.Count > 0:
                GoTo(0, -1);
                e.Handled = true;
                break;
            case Key.End when Items.Count > 0:
                GoTo(Items.Count - 1, 1);
                e.Handled = true;
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonDown(e);
        if (!IsKeyboardFocusWithin) {
            Focus();
        }
    }

    private static object CoerceSelectedIndex(DependencyObject d, object value) {
        Carousel carousel = (Carousel)d;
        int count = carousel.Items.Count;
        return (count == 0) ? -1 : Math.Clamp((int)value, 0, count - 1);
    }

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Carousel carousel = (Carousel)d;
        int oldIndex = (int)e.OldValue;
        int newIndex = (int)e.NewValue;
        int direction = (carousel.pendingDirection != 0) ? carousel.pendingDirection : Math.Sign(newIndex - oldIndex);
        carousel.pendingDirection = 0;
        carousel.ShowCurrent(direction);
        carousel.UpdateIndicators(true);
        carousel.RestartTimer();
        carousel.RaiseEvent(new RoutedPropertyChangedEventArgs<int>(oldIndex, newIndex, SelectedIndexChangedEvent));
    }

    private static void OnTimerSettingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Carousel)d).RestartTimer();
    }

    private void Step(int delta) {
        int count = Items.Count;
        if (count == 0) {
            return;
        }
        int target = SelectedIndex + delta;
        if ((target < 0) || (target >= count)) {
            if (!IsLooping) {
                return;
            }
            target = ((target % count) + count) % count;
        }
        GoTo(target, delta);
    }

    private void GoTo(int index, int direction) {
        if (index == SelectedIndex) {
            return;
        }
        pendingDirection = Math.Sign(direction);
        SetCurrentValue(SelectedIndexProperty, index);
        pendingDirection = 0;
    }

    private void ShowCurrent(int direction) {
        int index = SelectedIndex;
        object? item = ((index >= 0) && (index < Items.Count)) ? Items[index] : null;
        SetValue(SelectedItemPropertyKey, item);
        if ((front is null) || (back is null)) {
            return;
        }
        if (ReferenceEquals(front.Content, item) && (item is not null)) {
            return;
        }
        double width = (viewport?.ActualWidth > 0) ? viewport.ActualWidth : ActualWidth;
        bool animate = (direction != 0) && IsLoaded && (width > 0) && (front.Content is not null) && Jelly.IsMotionEnabled(this);
        if (!animate) {
            back.Content = null;
            back.Visibility = Visibility.Collapsed;
            front.Content = item;
            front.Visibility = Visibility.Visible;
            ResetOffset(front);
            return;
        }
        ContentPresenter incoming = back;
        ContentPresenter outgoing = front;
        incoming.Content = item;
        incoming.Visibility = Visibility.Visible;
        Panel.SetZIndex(incoming, 1);
        Panel.SetZIndex(outgoing, 0);
        front = incoming;
        back = outgoing;

        TimeSpan duration = TransitionDuration;
        Slide(incoming, direction * width, 0, duration, MuzzleEasings.Get(this, MuzzleEasings.BackKey), null);
        Slide(outgoing, 0, -direction * width, TimeSpan.FromMilliseconds(duration.TotalMilliseconds * 0.7), MuzzleEasings.Get(this, MuzzleEasings.GlideKey), () => {
            if (!ReferenceEquals(outgoing, front)) {
                outgoing.Visibility = Visibility.Collapsed;
                outgoing.Content = null;
            }
        });
    }

    private static void Slide(ContentPresenter presenter, double from, double to, TimeSpan duration, IEasingFunction easing, Action? completed) {
        if (presenter.RenderTransform is not TranslateTransform { IsFrozen: false } shift) {
            shift = new TranslateTransform();
            presenter.RenderTransform = shift;
        }
        shift.X = to;
        DoubleAnimation animation = new(from, to, new Duration(duration)) { EasingFunction = easing, FillBehavior = FillBehavior.Stop };
        if (completed is not null) {
            animation.Completed += (_, _) => completed();
        }
        shift.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private static void ResetOffset(ContentPresenter presenter) {
        if (presenter.RenderTransform is TranslateTransform { IsFrozen: false } shift) {
            shift.BeginAnimation(TranslateTransform.XProperty, null);
            shift.X = 0;
        }
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) {
        if (viewport is null) {
            return;
        }
        double radius = Math.Max(0, MuzzleAssist.GetCornerRadius(this).TopLeft - BorderThickness.Left);
        viewport.Clip = new RectangleGeometry(new Rect(e.NewSize), radius, radius);
    }

    private void RebuildIndicators() {
        if (indicatorPanel is null) {
            return;
        }
        indicatorPanel.Children.Clear();
        indicators.Clear();
        for (int i = 0; i < Items.Count; i++) {
            int index = i;
            Border dot = new() {
                Width = 8,
                Height = 8,
                Margin = new Thickness(3, 0, 3, 0),
                CornerRadius = new CornerRadius(4),
                Cursor = Cursors.Hand,
                ToolTip = $"Item {i + 1}",
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            dot.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.ControlBorderStrong");
            dot.MouseLeftButtonUp += (_, e) => {
                Bounce(dot);
                GoTo(index, index - SelectedIndex);
                e.Handled = true;
            };
            indicators.Add(dot);
            indicatorPanel.Children.Add(dot);
        }
        UpdateIndicators(false);
    }

    private void UpdateIndicators(bool animate) {
        bool motion = animate && IsLoaded && Jelly.IsMotionEnabled(this);
        for (int i = 0; i < indicators.Count; i++) {
            Border dot = indicators[i];
            bool selected = i == SelectedIndex;
            double width = selected ? 22 : 8;
            dot.SetResourceReference(Border.BackgroundProperty, selected ? "Muzzle.Brush.Primary" : "Muzzle.Brush.ControlBorderStrong");
            if (motion) {
                dot.BeginAnimation(WidthProperty, new DoubleAnimation(width, new Duration(TimeSpan.FromMilliseconds(520))) {
                    EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellySoftKey)
                });
                if (selected) {
                    Bounce(dot);
                }
            }
            else {
                dot.BeginAnimation(WidthProperty, null);
                dot.Width = width;
            }
        }
    }

    private void Bounce(Border dot) {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        ScaleTransform scale = Jelly.EnsureScale(dot);
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.SquishKey);
        IEasingFunction spring = MuzzleEasings.Get(this, MuzzleEasings.JellyKey);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, Jelly.CreatePulse(1.45, 1, 110, 620, squish, spring));
    }

    private void OnPreviousClick(object sender, RoutedEventArgs e) {
        Previous();
    }

    private void OnNextClick(object sender, RoutedEventArgs e) {
        Next();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        UpdateIndicators(false);
        RestartTimer();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        timer?.Stop();
    }

    private void RestartTimer() {
        timer?.Stop();
        if (!AutoPlay || !IsLoaded) {
            return;
        }
        if (timer is null) {
            timer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher);
            timer.Tick += OnTimerTick;
        }
        timer.Interval = Interval;
        timer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e) {
        if (IsMouseOver || IsKeyboardFocusWithin) {
            return;
        }
        Next();
    }
}