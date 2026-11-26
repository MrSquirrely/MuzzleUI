using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

using Xunit;

namespace Muzzle.Theme.Tests.Theming;

[Collection(WpfCollection.Name)]
public sealed class ResourceKeyParityTests(MuzzleWpfTestFixture fixture) {
    private const string Base = "pack://application:,,,/Muzzle.Theme;component/Themes/";
    private static readonly string[] Accents = ["Coral", "Lavender", "Mint", "Peach"];
    private static readonly string[] AccentOnlyKeys = ["Muzzle.AccentSlot", "Muzzle.AccentName"];

    [StaFact]
    public void LightAndDark_DefineExactlyTheSameKeys() {
        fixture.Run(() => {
            HashSet<string> light = Keys(Load("Muzzle.Light.xaml"));
            HashSet<string> dark = Keys(Load("Muzzle.Dark.xaml"));
            Assert.NotEmpty(light);
            Assert.True(light.SetEquals(dark),
                $"Only in Light: {string.Join(", ", light.Except(dark))}. Only in Dark: {string.Join(", ", dark.Except(light))}.");
        });
    }

    [StaFact]
    public void LightAndDark_HaveMatchingValueTypes() {
        fixture.Run(() => {
            ResourceDictionary light = Load("Muzzle.Light.xaml");
            ResourceDictionary dark = Load("Muzzle.Dark.xaml");
            foreach (string key in Keys(light)) {
                Assert.True(light[key].GetType() == dark[key].GetType(), $"{key}: {light[key].GetType().Name} vs {dark[key].GetType().Name}");
            }
        });
    }

    [StaFact]
    public void Palettes_CoverEverySemanticFamily() {
        fixture.Run(() => {
            ResourceDictionary light = Load("Muzzle.Light.xaml");
            HashSet<string> keys = Keys(light);
            string[] colours = keys.Where(static k => k.StartsWith("Muzzle.Color.", StringComparison.Ordinal)).ToArray();
            Assert.True(colours.Length >= 60, $"Expected 60+ colour tokens, found {colours.Length}.");
            foreach (string colour in colours) {
                string brushKey = "Muzzle.Brush." + colour["Muzzle.Color.".Length..];
                if (colour == "Muzzle.Color.SurfaceRimHighlight") {
                    continue; // used only inside the SurfaceRim gradient
                }
                Assert.True(keys.Contains(brushKey), $"{colour} has no matching {brushKey}.");
                Assert.IsType<Color>(light[colour]);
                Assert.IsAssignableFrom<SolidColorBrush>(light[brushKey]);
            }
            foreach (string shadow in (string[])["Subtle", "Soft", "Raised", "Floating"]) {
                Assert.IsType<DropShadowEffect>(light["Muzzle.Shadow." + shadow]);
            }
            Assert.IsType<LinearGradientBrush>(light["Muzzle.Brush.SurfaceRim"]);
            Assert.IsType<DrawingBrush>(light["Muzzle.Brush.Checker"]);
            Assert.Equal("Light", light["Muzzle.ThemeName"]);
            Assert.Equal("Dark", Load("Muzzle.Dark.xaml")["Muzzle.ThemeName"]);
        });
    }

    [StaFact]
    public void DarkPalette_IsActuallyDarker() {
        fixture.Run(() => {
            Color lightCanvas = (Color)Load("Muzzle.Light.xaml")["Muzzle.Color.Canvas"];
            Color darkCanvas = (Color)Load("Muzzle.Dark.xaml")["Muzzle.Color.Canvas"];
            Color darkText = (Color)Load("Muzzle.Dark.xaml")["Muzzle.Color.TextPrimary"];
            Assert.True(Luminance(darkCanvas) < Luminance(lightCanvas));
            Assert.True(Luminance(darkText) > Luminance(darkCanvas));
        });
    }

    [StaTheory]
    [InlineData("Coral")]
    [InlineData("Lavender")]
    [InlineData("Mint")]
    [InlineData("Peach")]
    public void AccentPairs_ShareKeysAndOnlyOverridePaletteKeys(string accent) {
        fixture.Run(() => {
            HashSet<string> palette = Keys(Load("Muzzle.Light.xaml"));
            HashSet<string> light = Keys(Load($"Accents/Muzzle.Accent.{accent}.Light.xaml"));
            HashSet<string> dark = Keys(Load($"Accents/Muzzle.Accent.{accent}.Dark.xaml"));
            Assert.True(light.SetEquals(dark), $"{accent}: Light/Dark accent keys differ.");
            foreach (string key in light.Except(AccentOnlyKeys)) {
                Assert.True(palette.Contains(key), $"{accent} defines {key}, which the base palette lacks.");
            }
            Assert.Contains("Muzzle.Brush.Primary", light);
            Assert.Contains("Muzzle.Brush.Secondary", light);
            Assert.Contains("Muzzle.Brush.Tertiary", light);
            Assert.Equal(accent, Load($"Accents/Muzzle.Accent.{accent}.Light.xaml")["Muzzle.AccentName"]);
        });
    }

    [StaFact]
    public void EveryAccent_DefinesTheSameKeys() {
        fixture.Run(() => {
            HashSet<string> reference = Keys(Load("Accents/Muzzle.Accent.Coral.Light.xaml"));
            foreach (string accent in Accents) {
                Assert.True(reference.SetEquals(Keys(Load($"Accents/Muzzle.Accent.{accent}.Dark.xaml"))), $"{accent} differs from Coral.");
            }
        });
    }

    [StaFact]
    public void CoralAccent_MatchesTheBasePaletteExactly() {
        fixture.Run(() => {
            foreach (string variant in (string[])["Light", "Dark"]) {
                ResourceDictionary palette = Load($"Muzzle.{variant}.xaml");
                ResourceDictionary coral = Load($"Accents/Muzzle.Accent.Coral.{variant}.xaml");
                foreach (string key in Keys(coral).Except(AccentOnlyKeys).Where(static k => k.StartsWith("Muzzle.Color.", StringComparison.Ordinal))) {
                    Assert.True(Equals(palette[key], coral[key]), $"{variant} {key}: palette {palette[key]} vs Coral {coral[key]}");
                }
            }
        });
    }

    private static ResourceDictionary Load(string relative) {
        return new ResourceDictionary { Source = new Uri(Base + relative, UriKind.Absolute) };
    }

    private static HashSet<string> Keys(ResourceDictionary dictionary) {
        return dictionary.Keys.Cast<object>().Select(static k => k.ToString()!).ToHashSet(StringComparer.Ordinal);
    }

    private static double Luminance(Color color) {
        return (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
    }
}