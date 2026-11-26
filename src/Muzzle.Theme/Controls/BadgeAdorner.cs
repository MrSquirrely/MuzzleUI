using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>Draws a content-less <see cref="Badge"/> over an element for <c>MuzzleAssist.Badge</c>.</summary>
internal sealed class BadgeAdorner : Adorner {
    public BadgeAdorner(UIElement adornedElement) : base(adornedElement) {
        Badge = new Badge {
            IsHitTestVisible = false,
            Focusable = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        AddVisualChild(Badge);
        IsHitTestVisible = false;
    }

    public Badge Badge { get; }

    /// <inheritdoc />
    protected override int VisualChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) {
        return index == 0 ? Badge : throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint) {
        Badge.Measure(AdornedElement.RenderSize);
        return AdornedElement.RenderSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize) {
        Badge.Arrange(new Rect(AdornedElement.RenderSize));
        return finalSize;
    }
}