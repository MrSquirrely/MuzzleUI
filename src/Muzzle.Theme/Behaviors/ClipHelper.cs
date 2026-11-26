using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Theme;

/// <summary>
/// Rounded content clipping. Set <c>ClipHelper.CornerRadius</c> on any element and its content is clipped to a
/// rounded rectangle that follows its size (0 turns it off). On a <see cref="Border"/> the clip goes on the child,
/// inset by the border thickness and padding with matching inner radii, so images, media and scroll viewers never
/// bleed past the rounded corners while the border itself stays crisp.
/// </summary>
public static class ClipHelper {
    /// <summary>Identifies the <c>CornerRadius</c> attached property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
        "CornerRadius", typeof(CornerRadius), typeof(ClipHelper), new PropertyMetadata(default(CornerRadius), OnCornerRadiusChanged));

    // The element currently carrying our clip (the Border's child, or the element itself).
    private static readonly DependencyProperty ClippedProperty = DependencyProperty.RegisterAttached(
        "Clipped", typeof(UIElement), typeof(ClipHelper), new PropertyMetadata(null));

    /// <summary>Gets the value of the <c>CornerRadius</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>CornerRadius</c> value.</returns>
    public static CornerRadius GetCornerRadius(DependencyObject element) {
        return (CornerRadius)element.GetValue(CornerRadiusProperty);
    }

    /// <summary>Sets the value of the <c>CornerRadius</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetCornerRadius(DependencyObject element, CornerRadius value) {
        element.SetValue(CornerRadiusProperty, value);
    }

    /// <summary>A rounded rectangle with per-corner radii (each clamped to fit), suitable as a clip.</summary>
    public static Geometry CreateRoundedRectangle(Rect rect, CornerRadius radius) {
        double max = Math.Max(0, Math.Min(rect.Width, rect.Height) / 2);
        double tl = Math.Clamp(radius.TopLeft, 0, max);
        double tr = Math.Clamp(radius.TopRight, 0, max);
        double br = Math.Clamp(radius.BottomRight, 0, max);
        double bl = Math.Clamp(radius.BottomLeft, 0, max);
        if ((tl == tr) && (tr == br) && (br == bl)) {
            RectangleGeometry simple = new(rect, tl, tl);
            simple.Freeze();
            return simple;
        }
        StreamGeometry geometry = new();
        using (StreamGeometryContext context = geometry.Open()) {
            context.BeginFigure(new Point(rect.Left + tl, rect.Top), true, true);
            context.LineTo(new Point(rect.Right - tr, rect.Top), false, false);
            context.ArcTo(new Point(rect.Right, rect.Top + tr), new Size(tr, tr), 0, false, SweepDirection.Clockwise, false, false);
            context.LineTo(new Point(rect.Right, rect.Bottom - br), false, false);
            context.ArcTo(new Point(rect.Right - br, rect.Bottom), new Size(br, br), 0, false, SweepDirection.Clockwise, false, false);
            context.LineTo(new Point(rect.Left + bl, rect.Bottom), false, false);
            context.ArcTo(new Point(rect.Left, rect.Bottom - bl), new Size(bl, bl), 0, false, SweepDirection.Clockwise, false, false);
            context.LineTo(new Point(rect.Left, rect.Top + tl), false, false);
            context.ArcTo(new Point(rect.Left + tl, rect.Top), new Size(tl, tl), 0, false, SweepDirection.Clockwise, false, false);
        }
        geometry.Freeze();
        return geometry;
    }

    private static bool IsEmpty(CornerRadius radius) {
        return (radius.TopLeft <= 0) && (radius.TopRight <= 0) && (radius.BottomRight <= 0) && (radius.BottomLeft <= 0);
    }

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement element) {
            return;
        }
        element.SizeChanged -= OnSizeChanged;
        element.Loaded -= OnLoaded;
        if (IsEmpty((CornerRadius)e.NewValue)) {
            if (element.GetValue(ClippedProperty) is UIElement clipped) {
                clipped.ClearValue(UIElement.ClipProperty);
                if (clipped is FrameworkElement child) {
                    child.SizeChanged -= OnChildSizeChanged;
                }
            }
            element.ClearValue(ClippedProperty);
            return;
        }
        element.SizeChanged += OnSizeChanged;
        element.Loaded += OnLoaded;
        Update(element);
    }

    private static void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        Update((FrameworkElement)sender);
    }

    private static void OnLoaded(object sender, RoutedEventArgs e) {
        Update((FrameworkElement)sender);
    }

    private static void OnChildSizeChanged(object sender, SizeChangedEventArgs e) {
        if ((sender is FrameworkElement child) && (child.Parent is FrameworkElement owner)) {
            Update(owner);
        }
    }

    private static void Update(FrameworkElement element) {
        CornerRadius radius = GetCornerRadius(element);
        if (IsEmpty(radius)) {
            return;
        }
        if (element is Border border) {
            if ((element.GetValue(ClippedProperty) is FrameworkElement previous) && !ReferenceEquals(previous, border.Child)) {
                previous.SizeChanged -= OnChildSizeChanged;
                previous.ClearValue(UIElement.ClipProperty);
                element.ClearValue(ClippedProperty);
            }
            if (border.Child is not FrameworkElement child) {
                return;
            }
            if (!ReferenceEquals(element.GetValue(ClippedProperty), child)) {
                child.SizeChanged += OnChildSizeChanged;
                element.SetValue(ClippedProperty, child);
            }
            // The child sits inside the border and padding; shrink each corner radius by what surrounds it.
            Thickness t = border.BorderThickness;
            Thickness p = border.Padding;
            CornerRadius inner = new(
                Math.Max(0, radius.TopLeft - Math.Max(t.Left + p.Left, t.Top + p.Top)),
                Math.Max(0, radius.TopRight - Math.Max(t.Right + p.Right, t.Top + p.Top)),
                Math.Max(0, radius.BottomRight - Math.Max(t.Right + p.Right, t.Bottom + p.Bottom)),
                Math.Max(0, radius.BottomLeft - Math.Max(t.Left + p.Left, t.Bottom + p.Bottom)));
            child.Clip = CreateRoundedRectangle(new Rect(0, 0, child.ActualWidth, child.ActualHeight), inner);
            return;
        }
        element.SetValue(ClippedProperty, element);
        element.Clip = CreateRoundedRectangle(new Rect(0, 0, element.ActualWidth, element.ActualHeight), radius);
    }
}