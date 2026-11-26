using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>An icon + label entry in a <see cref="NavigationView"/>.</summary>
public class NavigationViewItem : ListBoxItem {
    /// <summary>Any icon content (Path, Image, glyph TextBlock…).</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(object), typeof(NavigationViewItem), new PropertyMetadata(null));

    /// <summary>A line-art geometry drawn with the item's foreground (switches to blush when selected).</summary>
    public static readonly DependencyProperty IconGeometryProperty = DependencyProperty.Register(
        nameof(IconGeometry), typeof(Geometry), typeof(NavigationViewItem), new PropertyMetadata(null));

    static NavigationViewItem() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationViewItem), new FrameworkPropertyMetadata(typeof(NavigationViewItem)));
    }

    /// <summary>Gets or sets the icon. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? Icon {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the icon geometry. The default is <see langword="null"/>. This is a dependency property.</summary>
    public Geometry? IconGeometry {
        get => (Geometry?)GetValue(IconGeometryProperty);
        set => SetValue(IconGeometryProperty, value);
    }
}