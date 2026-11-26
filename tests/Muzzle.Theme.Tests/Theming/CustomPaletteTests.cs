using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Xunit;

namespace Muzzle.Theme.Tests.Theming;

[Collection(WpfCollection.Name)]
public sealed class CustomPaletteTests(MuzzleWpfTestFixture fixture) {
    private static readonly Color SepiaCanvas = Color.FromRgb(0xF4, 0xEC, 0xD8);
    private static readonly Color SepiaText = Color.FromRgb(0x5B, 0x46, 0x36);
    private static readonly Color NightCanvas = Color.FromRgb(0x10, 0x14, 0x18);
    private static readonly Color NightText = Color.FromRgb(0xD8, 0xDE, 0xE4);

    [StaFact]
    public void ApplyCustomPalette_DerivesTheNeutralRampAndKeepsTheAccent() {
        fixture.Run(() => {
            try {
                Color stockPrimary = Resolve("Muzzle.Color.Primary");
                Color stockInfo = Resolve("Muzzle.Color.Info");
                MuzzleThemeManager.ApplyCustomPalette(SepiaCanvas, SepiaText);

                Assert.Equal(SepiaCanvas, Resolve("Muzzle.Color.Canvas"));
                Assert.Equal(SepiaText, Resolve("Muzzle.Color.TextPrimary"));
                Assert.Equal(SepiaCanvas, ((SolidColorBrush)Application.Current.FindResource("Muzzle.Brush.Canvas")).Color);
                Assert.Equal(MuzzleTheme.Light, MuzzleThemeManager.EffectiveTheme);
                Assert.Equal(SepiaCanvas, MuzzleThemeManager.CustomPaletteCanvas);
                Assert.Equal(SepiaText, MuzzleThemeManager.CustomPaletteText);

                // Everything else keeps its place between canvas and text: surfaces at or above the canvas,
                // borders a step towards the text, secondary text between the two.
                Assert.True(Luma(Resolve("Muzzle.Color.Surface")) >= Luma(SepiaCanvas));
                Assert.InRange(Luma(Resolve("Muzzle.Color.ControlBorder")), Luma(SepiaText), Luma(SepiaCanvas) - 1);
                Assert.InRange(Luma(Resolve("Muzzle.Color.TextSecondary")), Luma(SepiaText) + 1, Luma(SepiaCanvas) - 1);
                // The warm tint carries through: more red than blue, like the canvas.
                Color border = Resolve("Muzzle.Color.ControlBorder");
                Assert.True(border.R > border.B);
                // Translucent tokens keep their alpha and take the new text colour.
                Color hover = Resolve("Muzzle.Color.HoverTint");
                Assert.Equal(0x0F, hover.A);
                Assert.Equal(SepiaText, Color.FromRgb(hover.R, hover.G, hover.B));
                Assert.IsAssignableFrom<GradientBrush>(Application.Current.FindResource("Muzzle.Brush.SurfaceRim"));

                // Accent and status colours are not part of the base palette ramp.
                Assert.Equal(stockPrimary, Resolve("Muzzle.Color.Primary"));
                Assert.Equal(stockInfo, Resolve("Muzzle.Color.Info"));
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public void ApplyCustomPalette_DarkCanvas_SwitchesToTheDarkBase() {
        fixture.Run(() => {
            try {
                MuzzleThemeManager.ApplyCustomPalette(NightCanvas, NightText, Colors.SteelBlue);
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.EffectiveTheme);
                Assert.Equal("Dark", Application.Current.FindResource("Muzzle.ThemeName"));
                Assert.Equal(NightCanvas, Resolve("Muzzle.Color.Canvas"));
                Assert.Equal(NightText, Resolve("Muzzle.Color.TextPrimary"));
                Assert.True(Luma(Resolve("Muzzle.Color.Surface")) > Luma(NightCanvas));
                Assert.Equal(MuzzleAccent.Custom, MuzzleThemeManager.CurrentAccent);
                Assert.Equal(Colors.SteelBlue, Resolve("Muzzle.Color.Primary"));
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public Task CustomPalette_UpdatesLiveElements_SurvivesAccents_AndEndsWithATheme() {
        return fixture.RunAsync(async () => {
            try {
                fixture.ResetDiagnostics();
                Border probe = new();
                probe.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.Canvas");
                Window window = MuzzleWpfTestFixture.ShowOffScreen(probe, 300, 200);
                try {
                    int themeChanges = 0;
                    void OnThemeChanged(object? sender, MuzzleThemeChangedEventArgs e) {
                        themeChanges++;
                    }

                    MuzzleThemeManager.ThemeChanged += OnThemeChanged;
                    try {
                        MuzzleThemeManager.ApplyCustomPalette(SepiaCanvas, SepiaText);
                        await MuzzleWpfTestFixture.WaitAsync(30);
                        Assert.Equal(SepiaCanvas, ((SolidColorBrush)probe.Background).Color);
                        Assert.Equal(1, themeChanges);

                        // Repeated calls (a slider being dragged) replace the one override dictionary.
                        int merged = Application.Current.Resources.MergedDictionaries.Count;
                        for (byte step = 0; step < 20; step++) {
                            MuzzleThemeManager.ApplyCustomPalette(Color.FromRgb(0xF4, 0xEC, (byte)(0xC0 + step)), SepiaText);
                        }
                        Assert.Equal(merged, Application.Current.Resources.MergedDictionaries.Count);
                        Assert.Equal(Color.FromRgb(0xF4, 0xEC, 0xD3), ((SolidColorBrush)probe.Background).Color);

                        MuzzleThemeManager.ApplyAccent(MuzzleAccent.Mint);
                        Assert.Equal(Color.FromRgb(0xF4, 0xEC, 0xD3), Resolve("Muzzle.Color.Canvas"));

                        MuzzleThemeManager.ClearCustomPalette();
                        Assert.Equal("#FFFAF8F6", Resolve("Muzzle.Color.Canvas").ToString());
                        Assert.Null(MuzzleThemeManager.CustomPaletteCanvas);
                        Assert.Equal("#FFFAF8F6", ((SolidColorBrush)probe.Background).Color.ToString());

                        MuzzleThemeManager.ApplyCustomPalette(SepiaCanvas, SepiaText);
                        MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                        Assert.Equal("#FF181622", Resolve("Muzzle.Color.Canvas").ToString());
                        Assert.Null(MuzzleThemeManager.CustomPaletteText);
                    }
                    finally {
                        MuzzleThemeManager.ThemeChanged -= OnThemeChanged;
                    }
                }
                finally {
                    window.Close();
                }
                fixture.AssertClean();
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    private static Color Resolve(string key) {
        return (Color)Application.Current.FindResource(key);
    }

    private static double Luma(Color color) {
        return (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);
    }
}
