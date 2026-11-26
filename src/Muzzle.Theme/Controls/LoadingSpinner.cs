using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Muzzle.Theme.Controls;

/// <summary>Visual style of a <see cref="LoadingSpinner"/>.</summary>
public enum SpinnerStyle {
    /// <summary>An arc that whips round a pastel track with an elastic rhythm (the default).</summary>
    ArcRing,
    /// <summary>Three dots that hop in turn with squash and stretch.</summary>
    BouncingDots
}

/// <summary>
/// An indeterminate progress indicator: an arc that whips around a pastel track with an elastic rhythm, or three
/// pastel dots that hop in turn with BackEase squash-and-stretch. Collapses (and stops animating) when not active.
/// </summary>
[TemplatePart(Name = ArcPartName, Type = typeof(Shape))]
public class LoadingSpinner : Control {
    /// <summary>Identifies the <see cref="IsActive"/> dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive), typeof(bool), typeof(LoadingSpinner), new PropertyMetadata(true, OnIsActiveChanged));

    /// <summary>Identifies the <see cref="SpinnerStyle"/> dependency property.</summary>
    public static readonly DependencyProperty SpinnerStyleProperty = DependencyProperty.Register(
        nameof(SpinnerStyle), typeof(SpinnerStyle), typeof(LoadingSpinner), new PropertyMetadata(SpinnerStyle.ArcRing));

    /// <summary>Arc colour, and the first dot's colour.</summary>
    public static readonly DependencyProperty AccentBrushProperty = DependencyProperty.Register(
        nameof(AccentBrush), typeof(Brush), typeof(LoadingSpinner), new PropertyMetadata(Brushes.HotPink));

    /// <summary>Identifies the <see cref="Thickness"/> dependency property.</summary>
    public static readonly DependencyProperty ThicknessProperty = DependencyProperty.Register(
        nameof(Thickness), typeof(double), typeof(LoadingSpinner), new PropertyMetadata(4.0, OnGeometryChanged));

    private const string ArcPartName = "PART_Arc";

    private Shape? arc;

    static LoadingSpinner() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(LoadingSpinner), new FrameworkPropertyMetadata(typeof(LoadingSpinner)));
        FocusableProperty.OverrideMetadata(typeof(LoadingSpinner), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="LoadingSpinner"/> class.</summary>
    public LoadingSpinner() {
        AutomationProperties.SetName(this, "Loading");
        AutomationProperties.SetLiveSetting(this, AutomationLiveSetting.Polite);
    }

    /// <summary>Gets or sets whether the control is active. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsActive {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    /// <summary>Gets or sets the spinner style. The default is <c>SpinnerStyle.ArcRing</c>. This is a dependency property.</summary>
    public SpinnerStyle SpinnerStyle {
        get => (SpinnerStyle)GetValue(SpinnerStyleProperty);
        set => SetValue(SpinnerStyleProperty, value);
    }

    /// <summary>Gets or sets the accent brush. The default is <c>Brushes.HotPink</c>. This is a dependency property.</summary>
    public Brush AccentBrush {
        get => (Brush)GetValue(AccentBrushProperty);
        set => SetValue(AccentBrushProperty, value);
    }

    /// <summary>Gets or sets the thickness. The default is <c>4.0</c>. This is a dependency property.</summary>
    public double Thickness {
        get => (double)GetValue(ThicknessProperty);
        set => SetValue(ThicknessProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (arc is not null) {
            arc.SizeChanged -= OnArcSizeChanged;
        }
        base.OnApplyTemplate();
        arc = GetTemplateChild(ArcPartName) as Shape;
        if (arc is not null) {
            arc.SizeChanged += OnArcSizeChanged;
        }
        UpdateDash();
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        AutomationProperties.SetItemStatus(d, e.NewValue is true ? "Loading" : string.Empty);
    }

    private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((LoadingSpinner)d).UpdateDash();
    }

    private void OnArcSizeChanged(object sender, SizeChangedEventArgs e) {
        UpdateDash();
    }

    /// <summary>Dash lengths are in stroke-thickness units, so size the visible arc to ~28% of the circumference.</summary>
    private void UpdateDash() {
        if ((arc is null) || (Thickness <= 0)) {
            return;
        }
        double diameter = Math.Max(0, Math.Min(arc.ActualWidth, arc.ActualHeight) - Thickness);
        double circumference = Math.PI * diameter / Thickness;
        if (circumference <= 0) {
            return;
        }
        arc.StrokeDashArray = [circumference * 0.28, circumference];
    }
}