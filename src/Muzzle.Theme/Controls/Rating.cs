using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>Icon used by <see cref="Rating"/>.</summary>
public enum RatingShape {
    /// <summary>Five-pointed stars (the default).</summary>
    Star,
    /// <summary>Hearts.</summary>
    Heart,
    /// <summary>Toe-bean paw prints.</summary>
    PawPrint
}

/// <summary>Frozen 24×24 vector icons for <see cref="Rating"/>.</summary>
public static class RatingGeometries {
    /// <summary>Gets the frozen 24 × 24 star geometry.</summary>
    public static Geometry Star { get; } = Freeze(Geometry.Parse("M 12,2.2 L 14.9,8.5 L 21.8,9.3 L 16.7,14 L 18.1,20.8 L 12,17.4 L 5.9,20.8 L 7.3,14 L 2.2,9.3 L 9.1,8.5 Z"));

    /// <summary>Gets the frozen 24 × 24 heart geometry.</summary>
    public static Geometry Heart { get; } = Freeze(Geometry.Parse("M 12,21 C 12,21 2.5,15.2 2.5,8.6 C 2.5,5.5 4.9,3.2 7.8,3.2 C 9.6,3.2 11.1,4.1 12,5.5 C 12.9,4.1 14.4,3.2 16.2,3.2 C 19.1,3.2 21.5,5.5 21.5,8.6 C 21.5,15.2 12,21 12,21 Z"));

    /// <summary>Gets the frozen 24 × 24 paw-print geometry.</summary>
    public static Geometry PawPrint { get; } = Freeze(CreatePaw());

    /// <summary>Gets the geometry for a rating shape.</summary>
    /// <param name="shape">The shape.</param>
    /// <returns>The frozen geometry.</returns>
    public static Geometry Get(RatingShape shape) {
        return shape switch {
            RatingShape.Heart => Heart,
            RatingShape.PawPrint => PawPrint,
            _ => Star
        };
    }

    private static Geometry CreatePaw() {
        GeometryGroup paw = new() { FillRule = FillRule.Nonzero };
        paw.Children.Add(new EllipseGeometry(new Point(12, 16.2), 6, 5.2));
        paw.Children.Add(new EllipseGeometry(new Point(4.6, 10.2), 2.3, 2.8));
        paw.Children.Add(new EllipseGeometry(new Point(9, 6), 2.4, 2.9));
        paw.Children.Add(new EllipseGeometry(new Point(15, 6), 2.4, 2.9));
        paw.Children.Add(new EllipseGeometry(new Point(19.4, 10.2), 2.3, 2.8));
        return paw;
    }

    private static Geometry Freeze(Geometry geometry) {
        geometry.Freeze();
        return geometry;
    }
}

/// <summary>
/// An interactive rating. Icons fill in blush (optionally by halves), scale up 1.18× and wiggle on hover, and bounce in
/// a little wave when a value is picked. Click the current value again to clear it. Arrow keys, Home and End work too.
/// </summary>
[TemplatePart(Name = PanelPartName, Type = typeof(Panel))]
public class Rating : Control {
    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(Rating));

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(Rating),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValueCallback));

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(int), typeof(Rating), new PropertyMetadata(5, OnLayoutChanged), static v => (int)v is >= 1 and <= 50);

    /// <summary>Identifies the <see cref="AllowHalf"/> dependency property.</summary>
    public static readonly DependencyProperty AllowHalfProperty = DependencyProperty.Register(
        nameof(AllowHalf), typeof(bool), typeof(Rating), new PropertyMetadata(false, OnLayoutChanged));

    /// <summary>Identifies the <see cref="IconShape"/> dependency property.</summary>
    public static readonly DependencyProperty IconShapeProperty = DependencyProperty.Register(
        nameof(IconShape), typeof(RatingShape), typeof(Rating), new PropertyMetadata(RatingShape.Star, OnLayoutChanged));

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(Rating), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="ItemSize"/> dependency property.</summary>
    public static readonly DependencyProperty ItemSizeProperty = DependencyProperty.Register(
        nameof(ItemSize), typeof(double), typeof(Rating), new PropertyMetadata(26.0, OnLayoutChanged));

    /// <summary>Identifies the <see cref="ItemSpacing"/> dependency property.</summary>
    public static readonly DependencyProperty ItemSpacingProperty = DependencyProperty.Register(
        nameof(ItemSpacing), typeof(double), typeof(Rating), new PropertyMetadata(4.0, OnLayoutChanged));

    /// <summary>Identifies the <see cref="FillBrush"/> dependency property.</summary>
    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(Rating), new PropertyMetadata(Brushes.HotPink));

    /// <summary>Identifies the <see cref="EmptyBrush"/> dependency property.</summary>
    public static readonly DependencyProperty EmptyBrushProperty = DependencyProperty.Register(
        nameof(EmptyBrush), typeof(Brush), typeof(Rating), new PropertyMetadata(Brushes.LightGray));

    private const string PanelPartName = "PART_Panel";

    private readonly List<RatingItem> items = [];
    private Panel? panel;
    private double? hoverValue;
    private int hoveredIndex = -1;

    static Rating() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Rating), new FrameworkPropertyMetadata(typeof(Rating)));
    }

    /// <summary>Occurs when <see cref="Value"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<double> ValueChanged {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>Gets or sets the value. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double Value {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Gets or sets the maximum. The default is <c>5</c>. This is a dependency property.</summary>
    public int Maximum {
        get => (int)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Gets or sets whether to allow half. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool AllowHalf {
        get => (bool)GetValue(AllowHalfProperty);
        set => SetValue(AllowHalfProperty, value);
    }

    /// <summary>Gets or sets the icon shape. The default is <c>RatingShape.Star</c>. This is a dependency property.</summary>
    public RatingShape IconShape {
        get => (RatingShape)GetValue(IconShapeProperty);
        set => SetValue(IconShapeProperty, value);
    }

    /// <summary>Gets or sets whether the value is read-only. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsReadOnly {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Gets or sets the item size. The default is <c>26.0</c>. This is a dependency property.</summary>
    public double ItemSize {
        get => (double)GetValue(ItemSizeProperty);
        set => SetValue(ItemSizeProperty, value);
    }

    /// <summary>Gets or sets the item spacing. The default is <c>4.0</c>. This is a dependency property.</summary>
    public double ItemSpacing {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    /// <summary>Gets or sets the fill brush. The default is <c>Brushes.HotPink</c>. This is a dependency property.</summary>
    public Brush FillBrush {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    /// <summary>Gets or sets the empty brush. The default is <c>Brushes.LightGray</c>. This is a dependency property.</summary>
    public Brush EmptyBrush {
        get => (Brush)GetValue(EmptyBrushProperty);
        set => SetValue(EmptyBrushProperty, value);
    }

    private double Step => AllowHalf ? 0.5 : 1;

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (panel is not null) {
            panel.MouseMove -= OnPanelMouseMove;
            panel.MouseLeave -= OnPanelMouseLeave;
            panel.MouseLeftButtonUp -= OnPanelMouseUp;
            panel.Children.Clear();
        }
        base.OnApplyTemplate();
        panel = GetTemplateChild(PanelPartName) as Panel;
        if (panel is not null) {
            panel.Background ??= Brushes.Transparent;
            panel.MouseMove += OnPanelMouseMove;
            panel.MouseLeave += OnPanelMouseLeave;
            panel.MouseLeftButtonUp += OnPanelMouseUp;
        }
        BuildItems();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (IsReadOnly || e.Handled) {
            return;
        }
        double next = e.Key switch {
            Key.Right or Key.Up => Value + Step,
            Key.Left or Key.Down => Value - Step,
            Key.Home => 0,
            Key.End => Maximum,
            _ => double.NaN
        };
        if (!double.IsNaN(next)) {
            Commit(next);
            e.Handled = true;
        }
    }

    private static object CoerceValueCallback(DependencyObject d, object value) {
        Rating rating = (Rating)d;
        double step = rating.Step;
        double snapped = Math.Round((double)value / step, MidpointRounding.AwayFromZero) * step;
        return Math.Clamp(double.IsNaN(snapped) ? 0 : snapped, 0, rating.Maximum);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Rating rating = (Rating)d;
        rating.UpdateFills();
        rating.UpdateAutomation();
        rating.RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, ValueChangedEvent));
    }

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Rating rating = (Rating)d;
        rating.CoerceValue(ValueProperty);
        rating.BuildItems();
    }

    private void BuildItems() {
        if (panel is null) {
            return;
        }
        panel.Children.Clear();
        items.Clear();
        Geometry shape = RatingGeometries.Get(IconShape);
        for (int i = 0; i < Maximum; i++) {
            RatingItem item = new() {
                Shape = shape,
                Width = ItemSize,
                Height = ItemSize,
                Margin = new Thickness(i == 0 ? 0 : ItemSpacing, 0, 0, 0)
            };
            item.SetBinding(RatingItem.FillBrushProperty, new Binding(nameof(FillBrush)) { Source = this });
            item.SetBinding(RatingItem.EmptyBrushProperty, new Binding(nameof(EmptyBrush)) { Source = this });
            items.Add(item);
            panel.Children.Add(item);
        }
        UpdateFills();
        UpdateAutomation();
    }

    private void UpdateFills() {
        double shown = hoverValue ?? Value;
        for (int i = 0; i < items.Count; i++) {
            items[i].FillRatio = Math.Clamp(shown - i, 0, 1);
        }
    }

    private void UpdateAutomation() {
        string text = string.Create(CultureInfo.CurrentCulture, $"{Value:0.#} of {Maximum}");
        AutomationProperties.SetItemStatus(this, text);
        ToolTipService.SetToolTip(this, IsReadOnly ? text : null);
    }

    private double ValueAt(Point point, out int index) {
        index = -1;
        for (int i = 0; i < items.Count; i++) {
            RatingItem item = items[i];
            Point local = panel!.TranslatePoint(point, item);
            double slotLeft = i == 0 ? 0 : -ItemSpacing;
            if ((local.X >= slotLeft) && (local.X <= item.ActualWidth)) {
                index = i;
                bool firstHalf = AllowHalf && (local.X < item.ActualWidth / 2);
                return i + (firstHalf ? 0.5 : 1);
            }
        }
        return Value;
    }

    private void OnPanelMouseMove(object sender, MouseEventArgs e) {
        if (IsReadOnly || (panel is null)) {
            return;
        }
        hoverValue = ValueAt(e.GetPosition(panel), out int index);
        if (index != hoveredIndex) {
            if ((hoveredIndex >= 0) && (hoveredIndex < items.Count)) {
                items[hoveredIndex].Relax();
            }
            hoveredIndex = index;
            if (index >= 0) {
                items[index].Wiggle(1.18);
            }
        }
        UpdateFills();
    }

    private void OnPanelMouseLeave(object sender, MouseEventArgs e) {
        if ((hoveredIndex >= 0) && (hoveredIndex < items.Count)) {
            items[hoveredIndex].Relax();
        }
        hoveredIndex = -1;
        hoverValue = null;
        UpdateFills();
    }

    private void OnPanelMouseUp(object sender, MouseButtonEventArgs e) {
        if (IsReadOnly || (panel is null)) {
            return;
        }
        double picked = ValueAt(e.GetPosition(panel), out int index);
        if (index < 0) {
            return;
        }
        Focus();
        Commit(Math.Abs(picked - Value) < 0.01 ? 0 : picked);
        e.Handled = true;
    }

    private void Commit(double value) {
        SetCurrentValue(ValueProperty, value);
        // A little wave across the filled icons.
        int filled = (int)Math.Ceiling(Value);
        for (int i = 0; i < filled && i < items.Count; i++) {
            items[i].Bounce(TimeSpan.FromMilliseconds(i * 45));
        }
    }
}

/// <summary>One icon of a <see cref="Rating"/>, drawn from a 24×24 geometry with a partial fill.</summary>
public class RatingItem : FrameworkElement {
    /// <summary>Identifies the <see cref="Shape"/> dependency property.</summary>
    public static readonly DependencyProperty ShapeProperty = DependencyProperty.Register(
        nameof(Shape), typeof(Geometry), typeof(RatingItem), new FrameworkPropertyMetadata(RatingGeometries.Star, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FillRatio"/> dependency property.</summary>
    public static readonly DependencyProperty FillRatioProperty = DependencyProperty.Register(
        nameof(FillRatio), typeof(double), typeof(RatingItem), new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="FillBrush"/> dependency property.</summary>
    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(RatingItem), new FrameworkPropertyMetadata(Brushes.HotPink, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <see cref="EmptyBrush"/> dependency property.</summary>
    public static readonly DependencyProperty EmptyBrushProperty = DependencyProperty.Register(
        nameof(EmptyBrush), typeof(Brush), typeof(RatingItem), new FrameworkPropertyMetadata(Brushes.LightGray, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly ScaleTransform scale = new(1, 1);
    private readonly RotateTransform rotate = new();

    /// <summary>Initializes a new instance of the <see cref="RatingItem"/> class.</summary>
    public RatingItem() {
        TransformGroup group = new();
        group.Children.Add(scale);
        group.Children.Add(rotate);
        RenderTransform = group;
        RenderTransformOrigin = new Point(0.5, 0.5);
    }

    /// <summary>Gets or sets the shape. The default is <c>RatingGeometries.Star</c>. This is a dependency property.</summary>
    public Geometry Shape {
        get => (Geometry)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    /// <summary>0 = empty, 1 = full, anything between fills from the left.</summary>
    public double FillRatio {
        get => (double)GetValue(FillRatioProperty);
        set => SetValue(FillRatioProperty, value);
    }

    /// <summary>Gets or sets the fill brush. The default is <c>Brushes.HotPink</c>. This is a dependency property.</summary>
    public Brush FillBrush {
        get => (Brush)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    /// <summary>Gets or sets the empty brush. The default is <c>Brushes.LightGray</c>. This is a dependency property.</summary>
    public Brush EmptyBrush {
        get => (Brush)GetValue(EmptyBrushProperty);
        set => SetValue(EmptyBrushProperty, value);
    }

    /// <summary>Grows to <paramref name="peak"/> and wiggles (hover).</summary>
    public void Wiggle(double peak) {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        DoubleAnimation grow = new(peak, TimeSpan.FromMilliseconds(380)) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.BackStrongKey) };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
        DoubleAnimationUsingKeyFrames wiggle = new();
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(-9, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(90))));
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(7, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(190))));
        wiggle.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(560)), MuzzleEasings.Get(this, MuzzleEasings.JellyKey)));
        rotate.BeginAnimation(RotateTransform.AngleProperty, wiggle);
    }

    /// <summary>Settles back to rest (hover ended).</summary>
    public void Relax() {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        DoubleAnimation settle = new(1, TimeSpan.FromMilliseconds(520)) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellySoftKey) };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, settle);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, settle);
    }

    /// <summary>A quick 1 → 1.18 → 1 jelly pop (selection), after <paramref name="delay"/>.</summary>
    public void Bounce(TimeSpan delay) {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.SquishKey);
        IEasingFunction jelly = MuzzleEasings.Get(this, MuzzleEasings.JellyKey);
        double rest = IsMouseOver ? 1.18 : 1;
        DoubleAnimationUsingKeyFrames pop = Jelly.CreatePulse(1.26, rest, 110, 700, squish, jelly);
        pop.BeginTime = delay;
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) {
        return new Size(double.IsNaN(Width) ? 24 : Width, double.IsNaN(Height) ? 24 : Height);
    }

    /// <inheritdoc />
    protected override void OnRender(DrawingContext drawingContext) {
        Size size = RenderSize;
        if ((size.Width <= 0) || (size.Height <= 0)) {
            return;
        }
        double factor = Math.Min(size.Width, size.Height) / 24;
        drawingContext.PushTransform(new ScaleTransform(factor, factor));
        drawingContext.DrawGeometry(EmptyBrush, null, Shape);
        double ratio = Math.Clamp(FillRatio, 0, 1);
        if (ratio > 0) {
            drawingContext.PushClip(new RectangleGeometry(new Rect(0, 0, 24 * ratio, 24)));
            drawingContext.DrawGeometry(FillBrush, null, Shape);
            drawingContext.Pop();
        }
        drawingContext.Pop();
    }
}