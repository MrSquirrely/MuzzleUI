using System;
using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A Border whose corner radii are clamped to half of its rendered size, so very large radii
/// (e.g. 999) give true pill / capsule shapes instead of WPF's stretched ellipses.
/// </summary>
public class SoftBorder : Border {
    static SoftBorder() {
        CornerRadiusProperty.OverrideMetadata(typeof(SoftBorder), new FrameworkPropertyMetadata(new CornerRadius(), null, CoerceCornerRadius));
    }

    /// <inheritdoc />
    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo) {
        base.OnRenderSizeChanged(sizeInfo);
        CoerceValue(CornerRadiusProperty);
    }

    private static object CoerceCornerRadius(DependencyObject d, object baseValue) {
        if ((d is not SoftBorder border) || (baseValue is not CornerRadius radius)) {
            return baseValue;
        }
        Size size = border.RenderSize;
        if ((size.Width <= 0) || (size.Height <= 0)) {
            return radius;
        }
        double max = Math.Min(size.Width, size.Height) / 2;
        return new CornerRadius(
            Math.Min(radius.TopLeft, max),
            Math.Min(radius.TopRight, max),
            Math.Min(radius.BottomRight, max),
            Math.Min(radius.BottomLeft, max));
    }
}