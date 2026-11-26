using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Gallery.Pages;

public partial class OverviewPage : UserControl {
    private static readonly string[] SwatchKeys = [
        "Canvas", "CanvasAlt", "Surface", "SurfaceBorder",
        "Primary", "PrimaryHover", "PrimaryPressed", "PrimarySoft",
        "Secondary", "SecondarySoft", "Mint", "MintSoft",
        "Butter", "Sky", "Peach", "Danger",
        "TextPrimary", "TextSecondary", "TextTertiary", "Track"
    ];

    public OverviewPage() {
        InitializeComponent();
        BuildSwatches();
        ThemeAware.Attach(this, BuildSwatches);
    }

    private void BuildSwatches() {
        List<Swatch> swatches = [];
        foreach (string key in SwatchKeys) {
            if (Application.Current.TryFindResource("Muzzle.Brush." + key) is SolidColorBrush brush) {
                Color color = brush.Color;
                swatches.Add(new Swatch(key, $"#{color.R:X2}{color.G:X2}{color.B:X2}", brush));
            }
        }
        SwatchList.ItemsSource = swatches;
    }

    public sealed record Swatch(string Name, string Hex, Brush Brush);
}