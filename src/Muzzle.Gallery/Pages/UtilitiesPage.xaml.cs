using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Muzzle.Theme;
using Muzzle.Theme.Controls;
using Muzzle.Theme.Converters;

namespace Muzzle.Gallery.Pages;

public partial class UtilitiesPage : UserControl {
    private static readonly MuzzleIconKind[] AllIcons = Enum.GetValues<MuzzleIconKind>().Where(static k => k != MuzzleIconKind.None).ToArray();

    private static readonly string[] TileKeys = [
        "Muzzle.Brush.Primary", "Muzzle.Brush.Secondary", "Muzzle.Brush.Tertiary", "Muzzle.Brush.Butter",
        "Muzzle.Brush.Sky", "Muzzle.Brush.Peach", "Muzzle.Brush.Mint", "Muzzle.Brush.Danger"
    ];

    public UtilitiesPage() {
        InitializeComponent();
        IconGrid.ItemsSource = AllIcons;
        IconCount.Text = $"{AllIcons.Length} rounded, stroke-drawn vector icons on a 24 × 24 grid: navigation, actions, media, files, status, interface, people and a few cozy extras. No icon package needed.";
        PlainTiles.ItemsSource = CreateTiles();
        SmoothTiles.ItemsSource = CreateTiles();
        BuildTintSwatches();
    }

    /// <summary>The icon kinds currently listed (snapshot mode reads it).</summary>
    public int VisibleIconCount => (IconGrid.ItemsSource as IEnumerable<MuzzleIconKind>)?.Count() ?? 0;

    private static List<FrameworkElement> CreateTiles() {
        List<FrameworkElement> tiles = [];
        for (int i = 0; i < 48; i++) {
            Border tile = new() { Width = 74, Height = 56, Margin = new Thickness(4), CornerRadius = new CornerRadius(10), Opacity = 0.85 };
            tile.SetResourceReference(Border.BackgroundProperty, TileKeys[i % TileKeys.Length]);
            tile.Child = new TextBlock { Text = (i + 1).ToString(), HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, FontWeight = FontWeights.SemiBold };
            tiles.Add(tile);
        }
        return tiles;
    }

    private void BuildTintSwatches() {
        (string Name, Color Color)[] colours = [
            ("Coral", Color.FromRgb(0xF4, 0x97, 0xA9)), ("Lavender", Color.FromRgb(0xA8, 0x96, 0xE6)), ("Sage", Color.FromRgb(0x7C, 0xCB, 0xB9)),
            ("Apricot", Color.FromRgb(0xF5, 0xA6, 0x7E)), ("Sky", Color.FromRgb(0x5F, 0xA8, 0xD3)), ("Plum", Color.FromRgb(0x8E, 0x4A, 0x7E))
        ];
        foreach ((string name, Color color) in colours) {
            Border chip = new() {
                Margin = new Thickness(0, 0, 8, 8),
                Padding = new Thickness(12, 6, 12, 6),
                CornerRadius = new CornerRadius(999),
                Background = (Brush)ColorToSoftTintConverter.Instance.Convert(color, typeof(Brush), 0.22, System.Globalization.CultureInfo.InvariantCulture),
                BorderBrush = new SolidColorBrush(color),
                BorderThickness = new Thickness(1),
                Child = new TextBlock { Text = name, FontWeight = FontWeights.SemiBold, FontSize = 12.5 }
            };
            TintSwatches.Children.Add(chip);
        }
    }

    private void OnReplayEntrances(object sender, RoutedEventArgs e) {
        foreach (FrameworkElement tile in BehaviorTiles.Children.OfType<FrameworkElement>()) {
            JellyBehavior.PlayEntrance(tile);
        }
    }

    private void OnClosePanel(object sender, RoutedEventArgs e) {
        PanelSwitch.IsChecked = false;
    }

    private void OnIconSearchChanged(object sender, TextChangedEventArgs e) {
        string query = IconSearch.Text.Trim();
        IconGrid.ItemsSource = (query.Length == 0)
            ? AllIcons
            : AllIcons.Where(kind => CommandPalette.FuzzyScore(query, kind.ToString()) is not null).ToArray();
    }
}