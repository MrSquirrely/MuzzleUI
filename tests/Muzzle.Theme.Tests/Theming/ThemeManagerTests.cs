using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Xunit;

namespace Muzzle.Theme.Tests.Theming;

[Collection(WpfCollection.Name)]
public sealed class ThemeManagerTests(MuzzleWpfTestFixture fixture) {
    private static readonly Dictionary<MuzzleAccent, (string Light, string Dark)> Primaries = new() {
        [MuzzleAccent.Coral] = ("#FFF497A9", "#FFF6A2B3"),
        [MuzzleAccent.Lavender] = ("#FFA896E6", "#FFB8A9F2"),
        [MuzzleAccent.Mint] = ("#FF7CCBB9", "#FF9FD8CB"),
        [MuzzleAccent.Peach] = ("#FFF5A67E", "#FFF7B492")
    };

    [StaFact]
    public void ApplyTheme_LightAndDark_UpdateStateAndResolveDistinctColours() {
        fixture.Run(() => {
            try {
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
                Assert.Equal(MuzzleTheme.Light, MuzzleThemeManager.CurrentTheme);
                Assert.Equal(MuzzleTheme.Light, MuzzleThemeManager.EffectiveTheme);
                Assert.False(MuzzleThemeManager.IsDark);
                Color lightCanvas = Resolve("Muzzle.Color.Canvas");
                Color lightText = Resolve("Muzzle.Color.TextPrimary");

                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.CurrentTheme);
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.EffectiveTheme);
                Assert.True(MuzzleThemeManager.IsDark);
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.DetectAppliedTheme());
                Color darkCanvas = Resolve("Muzzle.Color.Canvas");
                Color darkText = Resolve("Muzzle.Color.TextPrimary");

                Assert.NotEqual(lightCanvas, darkCanvas);
                Assert.NotEqual(lightText, darkText);
                Assert.Equal("#FF181622", darkCanvas.ToString());
                Assert.Equal("#FFFAF8F6", lightCanvas.ToString());
                Assert.Equal("Dark", Application.Current.FindResource("Muzzle.ThemeName"));
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public void ApplyTheme_RaisesThemeChanged() {
        fixture.Run(() => {
            List<MuzzleThemeChangedEventArgs> raised = [];
            void Handler(object? sender, MuzzleThemeChangedEventArgs e) {
                raised.Add(e);
            }

            MuzzleThemeManager.ThemeChanged += Handler;
            try {
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
            }
            finally {
                MuzzleThemeManager.ThemeChanged -= Handler;
                MuzzleWpfTestFixture.ResetTheme();
            }
            Assert.Equal(2, raised.Count);
            Assert.Equal(MuzzleTheme.Dark, raised[0].Theme);
            Assert.Equal(MuzzleTheme.Dark, raised[0].EffectiveTheme);
            Assert.Equal(MuzzleTheme.Light, raised[1].EffectiveTheme);
        });
    }

    [StaFact]
    public void ApplyTheme_UpdatesElementsThatAreAlreadyOnScreen() {
        fixture.Run(() => {
            Border border = new() { Width = 40, Height = 40 };
            border.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.Canvas");
            TextBlock text = new() { Text = "Mochi" };
            text.SetResourceReference(TextBlock.ForegroundProperty, "Muzzle.Brush.Primary");
            Window window = MuzzleWpfTestFixture.ShowOffScreen(new StackPanel { Children = { border, text } });
            try {
                Assert.Equal("#FFFAF8F6", ((SolidColorBrush)border.Background).Color.ToString());
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                Assert.Equal("#FF181622", ((SolidColorBrush)border.Background).Color.ToString());
                MuzzleThemeManager.ApplyAccent(MuzzleAccent.Mint);
                Assert.Equal("#FF9FD8CB", ((SolidColorBrush)text.Foreground).Color.ToString());
            }
            finally {
                window.Close();
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaTheory]
    [InlineData(MuzzleAccent.Coral)]
    [InlineData(MuzzleAccent.Lavender)]
    [InlineData(MuzzleAccent.Mint)]
    [InlineData(MuzzleAccent.Peach)]
    public void ApplyAccent_OverridesPrimaryAndSurvivesThemeSwitches(MuzzleAccent accent) {
        fixture.Run(() => {
            List<MuzzleAccentChangedEventArgs> raised = [];
            void Handler(object? sender, MuzzleAccentChangedEventArgs e) {
                raised.Add(e);
            }

            MuzzleThemeManager.AccentChanged += Handler;
            try {
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
                MuzzleThemeManager.ApplyAccent(accent);
                Assert.Equal(accent, MuzzleThemeManager.CurrentAccent);
                Assert.Equal(accent, MuzzleThemeManager.DetectAppliedAccent());
                Assert.Single(raised);
                Assert.Equal(accent, raised[0].Accent);
                Assert.Null(raised[0].CustomColor);
                Assert.Equal(Primaries[accent].Light, Resolve("Muzzle.Color.Primary").ToString());
                Assert.Equal(Primaries[accent].Light, ((SolidColorBrush)Application.Current.FindResource("Muzzle.Brush.Primary")).Color.ToString());

                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                Assert.Equal(accent, MuzzleThemeManager.CurrentAccent);
                Assert.Equal(Primaries[accent].Dark, Resolve("Muzzle.Color.Primary").ToString());
                Assert.Equal("#FF181622", Resolve("Muzzle.Color.Canvas").ToString());

                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
                Assert.Equal(accent, MuzzleThemeManager.CurrentAccent);
                Assert.Equal(Primaries[accent].Light, Resolve("Muzzle.Color.Primary").ToString());
            }
            finally {
                MuzzleThemeManager.AccentChanged -= Handler;
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaTheory]
    [InlineData(MuzzleAccent.Coral)]
    [InlineData(MuzzleAccent.Lavender)]
    [InlineData(MuzzleAccent.Mint)]
    [InlineData(MuzzleAccent.Peach)]
    public void ApplyAccent_SupportingRolesNeverShareThePrimaryHue(MuzzleAccent accent) {
        fixture.Run(() => {
            try {
                foreach (MuzzleTheme theme in (MuzzleTheme[])[MuzzleTheme.Light, MuzzleTheme.Dark]) {
                    MuzzleThemeManager.ApplyTheme(theme);
                    MuzzleThemeManager.ApplyAccent(accent);
                    Color primary = Resolve("Muzzle.Color.Primary");
                    Assert.NotEqual(primary, Resolve("Muzzle.Color.Secondary"));
                    Assert.NotEqual(primary, Resolve("Muzzle.Color.Tertiary"));
                }
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public void ApplyTheme_KeepsAccent_AndApplyAccent_KeepsTheme() {
        fixture.Run(() => {
            try {
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                MuzzleThemeManager.ApplyAccent(MuzzleAccent.Peach);
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.EffectiveTheme);
                Assert.Equal("#FF181622", Resolve("Muzzle.Color.Canvas").ToString());
                MuzzleThemeManager.ApplyAccent(MuzzleAccent.Lavender);
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.EffectiveTheme);
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
                Assert.Equal(MuzzleAccent.Lavender, MuzzleThemeManager.CurrentAccent);
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public void ApplyCustomAccent_DerivesARampAndSurvivesThemeSwitches() {
        fixture.Run(() => {
            Color sky = Color.FromRgb(0x5F, 0xA8, 0xD3);
            List<MuzzleAccentChangedEventArgs> raised = [];
            void Handler(object? sender, MuzzleAccentChangedEventArgs e) {
                raised.Add(e);
            }

            MuzzleThemeManager.AccentChanged += Handler;
            try {
                MuzzleThemeManager.ApplyCustomAccent(sky);
                Assert.Equal(MuzzleAccent.Custom, MuzzleThemeManager.CurrentAccent);
                Assert.Equal(sky, MuzzleThemeManager.CustomAccentColor);
                Assert.Equal(MuzzleAccent.Custom, raised[^1].Accent);
                Assert.Equal(sky, raised[^1].CustomColor);
                Assert.Equal(sky, Resolve("Muzzle.Color.Primary"));
                Assert.Equal("Custom", Application.Current.FindResource("Muzzle.AccentName"));
                Color lightSoft = Resolve("Muzzle.Color.PrimarySoft");
                Assert.NotEqual(Resolve("Muzzle.Color.PrimaryHover"), sky);
                Assert.NotEqual(Resolve("Muzzle.Color.PrimaryPressed"), sky);

                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                Assert.Equal(MuzzleAccent.Custom, MuzzleThemeManager.CurrentAccent);
                Assert.Equal(sky, Resolve("Muzzle.Color.Primary"));
                Assert.NotEqual(lightSoft, Resolve("Muzzle.Color.PrimarySoft"));
            }
            finally {
                MuzzleThemeManager.AccentChanged -= Handler;
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public void ApplyCustomAccent_IgnoresAlpha() {
        fixture.Run(() => {
            try {
                MuzzleThemeManager.ApplyCustomAccent(Color.FromArgb(40, 0x7C, 0x4D, 0xFF));
                Assert.Equal(255, Resolve("Muzzle.Color.Primary").A);
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public void ApplyAccent_CustomWithoutAColour_Throws() {
        fixture.Run(() => {
            MuzzleWpfTestFixture.ResetTheme();
            Assert.Null(MuzzleThemeManager.CustomAccentColor);
            Assert.Throws<ArgumentException>(() => MuzzleThemeManager.ApplyAccent(MuzzleAccent.Custom));
            Assert.Equal(MuzzleAccent.Coral, MuzzleThemeManager.CurrentAccent);
        });
    }

    [StaFact]
    public void CreateAccentDictionary_ReturnsPresetsAndCustomRamps() {
        fixture.Run(() => {
            ResourceDictionary lavender = MuzzleThemeManager.CreateAccentDictionary(MuzzleAccent.Lavender, dark: true);
            Assert.EndsWith("Muzzle.Accent.Lavender.Dark.xaml", lavender.Source.OriginalString);
            ResourceDictionary custom = MuzzleThemeManager.CreateAccentDictionary(MuzzleAccent.Custom, dark: false, Colors.Teal);
            Assert.Equal(Colors.Teal, custom["Muzzle.Color.Primary"]);
            Assert.True((bool)custom[MuzzleThemeManager.AccentSlotKey]);
            Assert.Throws<ArgumentNullException>(() => MuzzleThemeManager.CreateAccentDictionary(MuzzleAccent.Custom, dark: false));
        });
    }

    private static Color Resolve(string key) {
        return (Color)Application.Current.FindResource(key);
    }
}