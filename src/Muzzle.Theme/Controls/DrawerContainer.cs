using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A clipping Grid that hosts <see cref="Drawer"/>s over its other children. Put your content and drawers inside;
/// drawers slide out from its edges. Give it <c>mz:Drawer.Token</c> to target it from <see cref="Drawer.Show"/>,
/// and <c>mz:MuzzleAssist.CornerRadius</c> to clip drawers and masks to rounded corners.
/// </summary>
public class DrawerContainer : Grid {
    /// <summary>Initializes a new instance of the <see cref="DrawerContainer"/> class.</summary>
    public DrawerContainer() {
        ClipToBounds = true;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    internal static TokenRegistry<DrawerContainer> Registry { get; } = new();

    /// <summary>True for the per-window overlay created by the static API.</summary>
    internal bool IsOverlay { get; init; }

    /// <inheritdoc />
    protected override Geometry? GetLayoutClip(Size layoutSlotSize) {
        if (!ClipToBounds || (ReadLocalValue(MuzzleAssist.CornerRadiusProperty) is not CornerRadius radius) || (radius.TopLeft <= 0)) {
            return base.GetLayoutClip(layoutSlotSize);
        }
        double corner = Math.Min(radius.TopLeft, Math.Min(RenderSize.Width, RenderSize.Height) / 2);
        RectangleGeometry clip = new(new Rect(RenderSize), corner, corner);
        clip.Freeze();
        return clip;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if (!IsOverlay) {
            Registry.Add(this);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        Registry.Remove(this);
    }
}