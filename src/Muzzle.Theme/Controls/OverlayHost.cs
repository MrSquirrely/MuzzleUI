using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Lazily attaches a full-size overlay (a <see cref="DrawerContainer"/> under a <see cref="DialogContainer"/>)
/// to a window's AdornerLayer, so Dialog and Drawer can be shown from code without any host in XAML.
/// In a <see cref="MuzzleWindow"/> the overlay covers the client area and leaves the title bar usable.
/// </summary>
internal static class OverlayHost {
    private static readonly DependencyProperty LayerProperty = DependencyProperty.RegisterAttached(
        "Layer",
        typeof(OverlayLayer),
        typeof(OverlayHost),
        new PropertyMetadata(null));

    public static Window? ActiveWindow() {
        Application? application = Application.Current;
        if (application is null) {
            return null;
        }
        return application.Windows.OfType<Window>().FirstOrDefault(static window => window.IsActive && window is not GrowlWindow)
            ?? application.MainWindow;
    }

    public static Window RequireActiveWindow() {
        return ActiveWindow() ?? throw new InvalidOperationException("Muzzle overlays need an open window to attach to.");
    }

    public static OverlayLayer Get(Window window) {
        if (window.GetValue(LayerProperty) is OverlayLayer existing) {
            return existing;
        }
        if (window.Content is not UIElement content) {
            throw new InvalidOperationException("The window has no UIElement content to host overlays over.");
        }
        UIElement adorned = VisualTreeHelper.GetParent(content) as UIElement ?? content;
        AdornerLayer layer = AdornerLayer.GetAdornerLayer(adorned)
            ?? throw new InvalidOperationException("The window has no AdornerLayer. Its template needs an AdornerDecorator.");
        OverlayLayer overlay = new();
        layer.Add(new OverlayAdorner(adorned, overlay));
        window.SetValue(LayerProperty, overlay);
        return overlay;
    }
}

/// <summary>The per-window overlay: drawers below dialogs. Transparent to input wherever nothing is shown.</summary>
internal sealed class OverlayLayer : Grid {
    /// <summary>Initializes a new instance of the <see cref="OverlayLayer"/> class.</summary>
    public OverlayLayer() {
        Drawers = new DrawerContainer { IsOverlay = true };
        Dialogs = new DialogContainer { IsOverlay = true };
        Children.Add(Drawers);
        Children.Add(Dialogs);
    }

    public DrawerContainer Drawers { get; }

    public DialogContainer Dialogs { get; }
}

/// <summary>An adorner that sizes a single child to the adorned element.</summary>
internal sealed class OverlayAdorner : Adorner {
    private readonly FrameworkElement child;

    public OverlayAdorner(UIElement adornedElement, FrameworkElement overlay) : base(adornedElement) {
        child = overlay;
        AddVisualChild(child);
        if (adornedElement is FrameworkElement element) {
            element.SizeChanged += OnAdornedSizeChanged;
        }
    }

    /// <inheritdoc />
    protected override int VisualChildrenCount => 1;

    /// <inheritdoc />
    protected override Visual GetVisualChild(int index) {
        return index == 0 ? child : throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint) {
        Size size = AdornedElement.RenderSize;
        child.Measure(size);
        return size;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize) {
        child.Arrange(new Rect(AdornedElement.RenderSize));
        return AdornedElement.RenderSize;
    }

    private void OnAdornedSizeChanged(object sender, SizeChangedEventArgs e) {
        InvalidateMeasure();
    }
}