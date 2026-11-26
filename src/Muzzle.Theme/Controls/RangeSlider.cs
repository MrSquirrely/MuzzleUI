using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A dual-thumb range selector. Drag either squishy thumb (or focus one and use the arrow keys); click the track to
/// move the nearer thumb. <see cref="StepFrequency"/> snaps values; <see cref="MinimumRange"/> keeps them apart.
/// </summary>
[TemplatePart(Name = CanvasPartName, Type = typeof(Canvas))]
[TemplatePart(Name = RangePartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = LowerThumbPartName, Type = typeof(Thumb))]
[TemplatePart(Name = UpperThumbPartName, Type = typeof(Thumb))]
public class RangeSlider : Control {
    /// <summary>Name of the Canvas template part the thumbs and range bar are positioned on.</summary>
    public const string CanvasPartName = "PART_Canvas";
    /// <summary>Name of the template part that draws the active range between the thumbs.</summary>
    public const string RangePartName = "PART_Range";
    /// <summary>Name of the lower-value Thumb template part.</summary>
    public const string LowerThumbPartName = "PART_LowerThumb";
    /// <summary>Name of the upper-value Thumb template part.</summary>
    public const string UpperThumbPartName = "PART_UpperThumb";

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnLimitsChanged));

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RangeSlider), new PropertyMetadata(100.0, OnLimitsChanged));

    /// <summary>Identifies the <see cref="LowerValue"/> dependency property.</summary>
    public static readonly DependencyProperty LowerValueProperty = DependencyProperty.Register(
        nameof(LowerValue), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(20.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceLower));

    /// <summary>Identifies the <see cref="UpperValue"/> dependency property.</summary>
    public static readonly DependencyProperty UpperValueProperty = DependencyProperty.Register(
        nameof(UpperValue), typeof(double), typeof(RangeSlider),
        new FrameworkPropertyMetadata(80.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceUpper));

    /// <summary>Snap step (0 = continuous). Also the keyboard step when set.</summary>
    public static readonly DependencyProperty StepFrequencyProperty = DependencyProperty.Register(
        nameof(StepFrequency), typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0));

    /// <summary>The smallest allowed gap between the two values.</summary>
    public static readonly DependencyProperty MinimumRangeProperty = DependencyProperty.Register(
        nameof(MinimumRange), typeof(double), typeof(RangeSlider), new PropertyMetadata(0.0, OnLimitsChanged));

    /// <summary>Identifies the <see cref="RangeChanged"/> routed event.</summary>
    public static readonly RoutedEvent RangeChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(RangeChanged), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(RangeSlider));

    private Canvas? canvas;
    private FrameworkElement? range;
    private Thumb? lowerThumb;
    private Thumb? upperThumb;
    private double dragStartValue;
    private double dragAccumulated;

    static RangeSlider() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(typeof(RangeSlider)));
        FocusableProperty.OverrideMetadata(typeof(RangeSlider), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Occurs when <see cref="LowerValue"/> or <see cref="UpperValue"/> changes.</summary>
    public event RoutedEventHandler RangeChanged {
        add => AddHandler(RangeChangedEvent, value);
        remove => RemoveHandler(RangeChangedEvent, value);
    }

    /// <summary>Gets or sets the minimum. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double Minimum {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Gets or sets the maximum. The default is <c>100.0</c>. This is a dependency property.</summary>
    public double Maximum {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Gets or sets the lower value. The default is <c>20.0</c>. This is a dependency property.</summary>
    public double LowerValue {
        get => (double)GetValue(LowerValueProperty);
        set => SetValue(LowerValueProperty, value);
    }

    /// <summary>Gets or sets the upper value. The default is <c>80.0</c>. This is a dependency property.</summary>
    public double UpperValue {
        get => (double)GetValue(UpperValueProperty);
        set => SetValue(UpperValueProperty, value);
    }

    /// <summary>Gets or sets the step frequency. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double StepFrequency {
        get => (double)GetValue(StepFrequencyProperty);
        set => SetValue(StepFrequencyProperty, value);
    }

    /// <summary>Gets or sets the minimum range. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double MinimumRange {
        get => (double)GetValue(MinimumRangeProperty);
        set => SetValue(MinimumRangeProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        Detach();
        base.OnApplyTemplate();
        canvas = GetTemplateChild(CanvasPartName) as Canvas;
        range = GetTemplateChild(RangePartName) as FrameworkElement;
        lowerThumb = GetTemplateChild(LowerThumbPartName) as Thumb;
        upperThumb = GetTemplateChild(UpperThumbPartName) as Thumb;
        if (canvas is not null) {
            canvas.SizeChanged += OnCanvasSizeChanged;
            canvas.MouseLeftButtonDown += OnTrackMouseDown;
        }
        foreach (Thumb? thumb in (Thumb?[])[lowerThumb, upperThumb]) {
            if (thumb is not null) {
                thumb.DragStarted += OnDragStarted;
                thumb.DragDelta += OnDragDelta;
                thumb.KeyDown += OnThumbKeyDown;
            }
        }
        UpdateLayoutParts();
    }

    private void Detach() {
        if (canvas is not null) {
            canvas.SizeChanged -= OnCanvasSizeChanged;
            canvas.MouseLeftButtonDown -= OnTrackMouseDown;
        }
        foreach (Thumb? thumb in (Thumb?[])[lowerThumb, upperThumb]) {
            if (thumb is not null) {
                thumb.DragStarted -= OnDragStarted;
                thumb.DragDelta -= OnDragDelta;
                thumb.KeyDown -= OnThumbKeyDown;
            }
        }
    }

    private static object CoerceLower(DependencyObject d, object value) {
        RangeSlider slider = (RangeSlider)d;
        double upperLimit = Math.Min(slider.Maximum, slider.UpperValue) - slider.MinimumRange;
        return Math.Clamp((double)value, slider.Minimum, Math.Max(slider.Minimum, upperLimit));
    }

    private static object CoerceUpper(DependencyObject d, object value) {
        RangeSlider slider = (RangeSlider)d;
        double lowerLimit = Math.Max(slider.Minimum, slider.LowerValue) + slider.MinimumRange;
        return Math.Clamp((double)value, Math.Min(slider.Maximum, lowerLimit), slider.Maximum);
    }

    private static void OnLimitsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        RangeSlider slider = (RangeSlider)d;
        slider.CoerceValue(LowerValueProperty);
        slider.CoerceValue(UpperValueProperty);
        slider.UpdateLayoutParts();
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        RangeSlider slider = (RangeSlider)d;
        slider.CoerceValue((e.Property == LowerValueProperty) ? UpperValueProperty : LowerValueProperty);
        slider.UpdateLayoutParts();
        slider.RaiseEvent(new RoutedEventArgs(RangeChangedEvent, slider));
    }

    private void OnCanvasSizeChanged(object sender, SizeChangedEventArgs e) {
        UpdateLayoutParts();
    }

    private double ThumbSize => lowerThumb?.ActualWidth > 0 ? lowerThumb.ActualWidth : 20;

    private double Usable => Math.Max(0, (canvas?.ActualWidth ?? 0) - ThumbSize);

    private double ToPixels(double value) {
        double span = Maximum - Minimum;
        return (span <= 0) ? 0 : ((value - Minimum) / span) * Usable;
    }

    private double ToValue(double pixels) {
        return (Usable <= 0) ? Minimum : Minimum + ((pixels / Usable) * (Maximum - Minimum));
    }

    private double Snap(double value) {
        double step = StepFrequency;
        return (step > 0) ? Minimum + (Math.Round((value - Minimum) / step) * step) : value;
    }

    private void UpdateLayoutParts() {
        if ((canvas is null) || (lowerThumb is null) || (upperThumb is null)) {
            return;
        }
        double lower = ToPixels(LowerValue);
        double upper = ToPixels(UpperValue);
        double top = Math.Max(0, (canvas.ActualHeight - ThumbSize) / 2);
        Canvas.SetLeft(lowerThumb, lower);
        Canvas.SetTop(lowerThumb, top);
        Canvas.SetLeft(upperThumb, upper);
        Canvas.SetTop(upperThumb, top);
        if (range is not null) {
            Canvas.SetLeft(range, lower + (ThumbSize / 2));
            Canvas.SetTop(range, (canvas.ActualHeight - range.Height) / 2);
            range.Width = Math.Max(0, upper - lower);
        }
    }

    private void OnDragStarted(object sender, DragStartedEventArgs e) {
        dragStartValue = ReferenceEquals(sender, lowerThumb) ? LowerValue : UpperValue;
        dragAccumulated = 0;
    }

    private void OnDragDelta(object sender, DragDeltaEventArgs e) {
        dragAccumulated += e.HorizontalChange;
        double span = Maximum - Minimum;
        double value = Snap(dragStartValue + ((Usable <= 0) ? 0 : (dragAccumulated / Usable) * span));
        SetCurrentValue(ReferenceEquals(sender, lowerThumb) ? LowerValueProperty : UpperValueProperty, value);
    }

    private void OnTrackMouseDown(object sender, MouseButtonEventArgs e) {
        if ((canvas is null) || (e.OriginalSource is DependencyObject source && IsThumbPart(source))) {
            return;
        }
        double value = Snap(ToValue(e.GetPosition(canvas).X - (ThumbSize / 2)));
        bool moveLower = Math.Abs(value - LowerValue) <= Math.Abs(value - UpperValue);
        if (Math.Abs(LowerValue - UpperValue) < 1e-9) {
            moveLower = value < LowerValue;
        }
        SetCurrentValue(moveLower ? LowerValueProperty : UpperValueProperty, value);
        (moveLower ? lowerThumb : upperThumb)?.Focus();
        e.Handled = true;
    }

    private bool IsThumbPart(DependencyObject source) {
        DependencyObject? current = source;
        while (current is not null) {
            if (ReferenceEquals(current, lowerThumb) || ReferenceEquals(current, upperThumb)) {
                return true;
            }
            current = System.Windows.Media.VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void OnThumbKeyDown(object sender, KeyEventArgs e) {
        bool lower = ReferenceEquals(sender, lowerThumb);
        DependencyProperty property = lower ? LowerValueProperty : UpperValueProperty;
        double current = lower ? LowerValue : UpperValue;
        double step = (StepFrequency > 0) ? StepFrequency : ((Maximum - Minimum) / 100);
        double? next = e.Key switch {
            Key.Left or Key.Down => current - step,
            Key.Right or Key.Up => current + step,
            Key.PageDown => current - (step * 10),
            Key.PageUp => current + (step * 10),
            Key.Home => Minimum,
            Key.End => Maximum,
            _ => null
        };
        if (next is double value) {
            SetCurrentValue(property, Snap(value));
            e.Handled = true;
        }
    }
}