using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>The state of one step in a <see cref="StepBar"/>.</summary>
public enum StepStatus {
    /// <summary>A step that hasn't been reached yet.</summary>
    Waiting,
    /// <summary>The step in progress.</summary>
    Current,
    /// <summary>A finished step (its circle fills and shows a check).</summary>
    Completed
}

/// <summary>
/// A multi-step progress tracker. Steps before <see cref="StepIndex"/> are completed (their circles pop in with a
/// checkmark), the step at <see cref="StepIndex"/> is current, and a pastel track fills smoothly between circles.
/// Horizontal or vertical via <see cref="Orientation"/>.
/// </summary>
[TemplatePart(Name = TrackPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ProgressPartName, Type = typeof(FrameworkElement))]
public class StepBar : ItemsControl {
    private const string TrackPartName = "PART_Track";
    private const string ProgressPartName = "PART_Progress";
    private const double TrackThickness = 4;

    /// <summary>0-based index of the current step. Equal to the step count when everything is complete.</summary>
    public static readonly DependencyProperty StepIndexProperty = DependencyProperty.Register(
        nameof(StepIndex),
        typeof(int),
        typeof(StepBar),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnStepIndexChanged, CoerceStepIndex));

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation), typeof(Orientation), typeof(StepBar), new PropertyMetadata(Orientation.Horizontal, OnOrientationChanged));

    private FrameworkElement? track;
    private FrameworkElement? progress;

    static StepBar() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StepBar), new FrameworkPropertyMetadata(typeof(StepBar)));
        FocusableProperty.OverrideMetadata(typeof(StepBar), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="StepBar"/> class.</summary>
    public StepBar() {
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
    }

    /// <summary>Gets or sets the step index. The default is <c>0</c>. This is a dependency property.</summary>
    public int StepIndex {
        get => (int)GetValue(StepIndexProperty);
        set => SetValue(StepIndexProperty, value);
    }

    /// <summary>Gets or sets the orientation. The default is <c>Orientation.Horizontal</c>. This is a dependency property.</summary>
    public Orientation Orientation {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>Moves to the next step (up to "all complete").</summary>
    public void Next() {
        SetCurrentValue(StepIndexProperty, StepIndex + 1);
    }

    /// <summary>Moves back one step.</summary>
    public void Previous() {
        SetCurrentValue(StepIndexProperty, StepIndex - 1);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        track = GetTemplateChild(TrackPartName) as FrameworkElement;
        progress = GetTemplateChild(ProgressPartName) as FrameworkElement;
        Schedule(false);
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) {
        return item is StepBarItem;
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() {
        return new StepBarItem();
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
        base.PrepareContainerForItemOverride(element, item);
        Schedule(false);
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
        base.OnItemsChanged(e);
        CoerceValue(StepIndexProperty);
        Schedule(false);
    }

    private static object CoerceStepIndex(DependencyObject d, object value) {
        StepBar bar = (StepBar)d;
        return Math.Clamp((int)value, 0, Math.Max(0, bar.Items.Count));
    }

    private static void OnStepIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((StepBar)d).Schedule(true);
    }

    private static void OnOrientationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((StepBar)d).Schedule(false);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        Schedule(false);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        Schedule(false);
    }

    private void Schedule(bool animate) {
        UpdateItems();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => UpdateTrack(animate)));
    }

    private void UpdateItems() {
        int count = Items.Count;
        int index = StepIndex;
        for (int i = 0; i < count; i++) {
            if (ItemContainerGenerator.ContainerFromIndex(i) is not StepBarItem item) {
                continue;
            }
            StepStatus status = i < index ? StepStatus.Completed : (i == index ? StepStatus.Current : StepStatus.Waiting);
            item.Update(i + 1, status, i == count - 1, Orientation);
        }
    }

    private void UpdateTrack(bool animate) {
        if ((track is null) || (progress is null) || (track.Parent is not Visual host)) {
            return;
        }
        int count = Items.Count;
        FrameworkElement? first = CircleAt(0);
        FrameworkElement? last = CircleAt(count - 1);
        if ((count < 2) || (first is null) || (last is null) || !first.IsDescendantOf(host) || !last.IsDescendantOf(host)) {
            track.Visibility = Visibility.Collapsed;
            progress.Visibility = Visibility.Collapsed;
            return;
        }
        track.Visibility = Visibility.Visible;
        progress.Visibility = Visibility.Visible;
        Point start = Center(first, host);
        Point end = Center(last, host);
        bool horizontal = Orientation == Orientation.Horizontal;
        double length = horizontal ? end.X - start.X : end.Y - start.Y;
        int reached = Math.Min(StepIndex, count - 1);
        FrameworkElement? reachedCircle = CircleAt(reached);
        double filled = reachedCircle is null ? 0 : (horizontal ? Center(reachedCircle, host).X - start.X : Center(reachedCircle, host).Y - start.Y);

        if (horizontal) {
            Place(track, start.X, start.Y - (TrackThickness / 2), length, TrackThickness);
            Place(progress, start.X, start.Y - (TrackThickness / 2), double.NaN, TrackThickness);
            Fill(progress, FrameworkElement.WidthProperty, Math.Max(0, filled), animate);
            progress.Height = TrackThickness;
            progress.BeginAnimation(FrameworkElement.HeightProperty, null);
        }
        else {
            Place(track, start.X - (TrackThickness / 2), start.Y, TrackThickness, length);
            Place(progress, start.X - (TrackThickness / 2), start.Y, TrackThickness, double.NaN);
            Fill(progress, FrameworkElement.HeightProperty, Math.Max(0, filled), animate);
            progress.Width = TrackThickness;
            progress.BeginAnimation(FrameworkElement.WidthProperty, null);
        }
    }

    private FrameworkElement? CircleAt(int index) {
        if ((index < 0) || (ItemContainerGenerator.ContainerFromIndex(index) is not StepBarItem item)) {
            return null;
        }
        item.ApplyTemplate();
        return item.Template?.FindName("PART_Circle", item) as FrameworkElement;
    }

    private static Point Center(FrameworkElement element, Visual host) {
        return element.TransformToAncestor(host).Transform(new Point(element.ActualWidth / 2, element.ActualHeight / 2));
    }

    private static void Place(FrameworkElement element, double x, double y, double width, double height) {
        element.HorizontalAlignment = HorizontalAlignment.Left;
        element.VerticalAlignment = VerticalAlignment.Top;
        element.Margin = new Thickness(x, y, 0, 0);
        if (!double.IsNaN(width)) {
            element.Width = Math.Max(0, width);
        }
        if (!double.IsNaN(height)) {
            element.Height = Math.Max(0, height);
        }
    }

    private void Fill(FrameworkElement element, DependencyProperty property, double value, bool animate) {
        if (animate && Jelly.IsMotionEnabled(this) && !double.IsNaN((double)element.GetValue(property))) {
            element.BeginAnimation(property, new DoubleAnimation(value, TimeSpan.FromMilliseconds(620)) {
                EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.BackSoftKey)
            });
            return;
        }
        element.BeginAnimation(property, null);
        element.SetValue(property, value);
    }
}

/// <summary>One step of a <see cref="StepBar"/>: a numbered circle and a label (the item content).</summary>
public class StepBarItem : ContentControl {
    private static readonly DependencyPropertyKey NumberPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Number), typeof(int), typeof(StepBarItem), new PropertyMetadata(1));

    /// <summary>Identifies the <see cref="Number"/> read-only dependency property.</summary>
    public static readonly DependencyProperty NumberProperty = NumberPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StatusPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Status), typeof(StepStatus), typeof(StepBarItem), new PropertyMetadata(StepStatus.Waiting));

    /// <summary>Identifies the <see cref="Status"/> read-only dependency property.</summary>
    public static readonly DependencyProperty StatusProperty = StatusPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsLastPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsLast), typeof(bool), typeof(StepBarItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsLast"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsLastProperty = IsLastPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey OrientationPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Orientation), typeof(Orientation), typeof(StepBarItem), new PropertyMetadata(Orientation.Horizontal));

    /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
    public static readonly DependencyProperty OrientationProperty = OrientationPropertyKey.DependencyProperty;

    static StepBarItem() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(StepBarItem), new FrameworkPropertyMetadata(typeof(StepBarItem)));
        FocusableProperty.OverrideMetadata(typeof(StepBarItem), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Gets the number. This is a read-only dependency property.</summary>
    public int Number => (int)GetValue(NumberProperty);

    /// <summary>Gets the status. This is a read-only dependency property.</summary>
    public StepStatus Status => (StepStatus)GetValue(StatusProperty);

    /// <summary>Gets whether this is the last item. This is a read-only dependency property.</summary>
    public bool IsLast => (bool)GetValue(IsLastProperty);

    /// <summary>Gets the orientation. This is a read-only dependency property.</summary>
    public Orientation Orientation => (Orientation)GetValue(OrientationProperty);

    internal void Update(int number, StepStatus status, bool isLast, Orientation orientation) {
        SetValue(NumberPropertyKey, number);
        SetValue(StatusPropertyKey, status);
        SetValue(IsLastPropertyKey, isLast);
        SetValue(OrientationPropertyKey, orientation);
    }
}