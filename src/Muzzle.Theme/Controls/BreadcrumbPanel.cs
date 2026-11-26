using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Lays breadcrumb segments out in a row. When they don't fit, segments after the first collapse (oldest first)
/// behind an "…" marker, and the final segment is given whatever width remains (its label trims with an ellipsis).
/// </summary>
public class BreadcrumbPanel : Panel {
    private readonly TextBlock overflow;
    private readonly List<bool> hidden = [];
    private bool showOverflow;
    private bool overflowAttached;

    /// <summary>Initializes a new instance of the <see cref="BreadcrumbPanel"/> class.</summary>
    public BreadcrumbPanel() {
        overflow = new TextBlock {
            Text = "…",
            Margin = new Thickness(8, 0, 2, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontWeight = FontWeights.SemiBold
        };
        overflow.SetResourceReference(TextBlock.ForegroundProperty, "Muzzle.Brush.TextTertiary");
        ClipToBounds = true;
    }

    /// <inheritdoc />
    protected override int VisualChildrenCount => base.VisualChildrenCount + (overflowAttached ? 1 : 0);

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) {
        return overflowAttached && (index == base.VisualChildrenCount) ? overflow : base.GetVisualChild(index);
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize) {
        // Attached lazily: an ItemsPresenter requires its panel to start with no visual children.
        if (!overflowAttached) {
            overflowAttached = true;
            AddVisualChild(overflow);
        }
        int count = InternalChildren.Count;
        hidden.Clear();
        double total = 0;
        double height = 0;
        foreach (UIElement child in InternalChildren) {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            total += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
            hidden.Add(false);
        }
        overflow.Measure(new Size(double.PositiveInfinity, availableSize.Height));
        showOverflow = false;
        double limit = availableSize.Width;
        if (!double.IsInfinity(limit) && (total > limit) && (count > 2)) {
            showOverflow = true;
            total += overflow.DesiredSize.Width;
            for (int i = 1; (i < count - 1) && (total > limit); i++) {
                hidden[i] = true;
                total -= InternalChildren[i].DesiredSize.Width;
            }
        }
        if (!double.IsInfinity(limit) && (total > limit) && (count > 0)) {
            // Give the last segment only what's left so its label trims.
            UIElement last = InternalChildren[count - 1];
            double others = total - last.DesiredSize.Width;
            last.Measure(new Size(Math.Max(0, limit - others), availableSize.Height));
            total = others + last.DesiredSize.Width;
        }
        return new Size(double.IsInfinity(limit) ? total : Math.Min(total, limit), Math.Max(height, overflow.DesiredSize.Height));
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize) {
        double x = 0;
        int count = InternalChildren.Count;
        bool overflowPlaced = false;
        for (int i = 0; i < count; i++) {
            UIElement child = InternalChildren[i];
            if ((i < hidden.Count) && hidden[i]) {
                // Parked far outside the clip: arranging at zero size would still paint the content.
                child.Arrange(new Rect(-100000, 0, child.DesiredSize.Width, finalSize.Height));
                if (!overflowPlaced) {
                    overflow.Arrange(new Rect(x, 0, overflow.DesiredSize.Width, finalSize.Height));
                    x += overflow.DesiredSize.Width;
                    overflowPlaced = true;
                }
                continue;
            }
            double width = (i == count - 1) ? Math.Max(0, Math.Min(child.DesiredSize.Width, finalSize.Width - x)) : child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }
        if (!showOverflow || !overflowPlaced) {
            overflow.Arrange(new Rect(-100000, 0, overflow.DesiredSize.Width, finalSize.Height));
        }
        return finalSize;
    }
}