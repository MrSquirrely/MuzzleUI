using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Draws an icon from Muzzle's built-in pack: <c>&lt;mz:MuzzleIcon Kind="Search" IconSize="18" /&gt;</c>. Icons are
/// rounded strokes in <see cref="Control.Foreground"/> (inherited, so they follow the text colour around them) and
/// scale crisply to <see cref="IconSize"/>. <see cref="Data"/> follows the "Muzzle.Icon.&lt;Kind&gt;" resource, so an
/// app can restyle any icon by redefining that key.
/// </summary>
public class MuzzleIcon : Control {
    /// <summary>Identifies the <see cref="Kind"/> dependency property.</summary>
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(MuzzleIconKind), typeof(MuzzleIcon), new PropertyMetadata(MuzzleIconKind.None, OnKindChanged));

    /// <summary>Identifies the <see cref="IconSize"/> dependency property.</summary>
    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize), typeof(double), typeof(MuzzleIcon), new FrameworkPropertyMetadata(16.0, FrameworkPropertyMetadataOptions.AffectsMeasure));

    /// <summary>Stroke width on the 24-unit grid (default 1.8); it scales with the icon.</summary>
    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness), typeof(double), typeof(MuzzleIcon), new PropertyMetadata(1.8));

    /// <summary>Identifies the <see cref="Data"/> dependency property.</summary>
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data), typeof(Geometry), typeof(MuzzleIcon), new PropertyMetadata(null));

    static MuzzleIcon() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(MuzzleIcon), new FrameworkPropertyMetadata(typeof(MuzzleIcon)));
        FocusableProperty.OverrideMetadata(typeof(MuzzleIcon), new FrameworkPropertyMetadata(false));
        IsTabStopProperty.OverrideMetadata(typeof(MuzzleIcon), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Gets or sets the kind. The default is <c>MuzzleIconKind.None</c>. This is a dependency property.</summary>
    public MuzzleIconKind Kind {
        get => (MuzzleIconKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Gets or sets the icon size. The default is <c>16.0</c>. This is a dependency property.</summary>
    public double IconSize {
        get => (double)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    /// <summary>Gets or sets the stroke thickness. The default is <c>1.8</c>. This is a dependency property.</summary>
    public double StrokeThickness {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>The icon geometry (24 x 24 grid). Normally set from <see cref="Kind"/>.</summary>
    public Geometry? Data {
        get => (Geometry?)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    /// <summary>The resource key of an icon: "Muzzle.Icon.&lt;Kind&gt;".</summary>
    public static string GetResourceKey(MuzzleIconKind kind) {
        return "Muzzle.Icon." + kind;
    }

    /// <summary>Looks an icon's geometry up in the application resources (null for <see cref="MuzzleIconKind.None"/>).</summary>
    public static Geometry? GetGeometry(MuzzleIconKind kind) {
        return (kind == MuzzleIconKind.None) ? null : Application.Current?.TryFindResource(GetResourceKey(kind)) as Geometry;
    }

    private static void OnKindChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        MuzzleIcon icon = (MuzzleIcon)d;
        MuzzleIconKind kind = (MuzzleIconKind)e.NewValue;
        if (kind == MuzzleIconKind.None) {
            icon.ClearValue(DataProperty);
        }
        else {
            icon.SetResourceReference(DataProperty, GetResourceKey(kind));
        }
    }
}