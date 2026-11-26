using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

using Xunit;

namespace Muzzle.Theme.Tests.Theming;

[Collection(WpfCollection.Name)]
public sealed class ThemeResourceDictionaryTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public void Defaults_AreLightAndCoral() {
        fixture.Run(() => {
            MuzzleThemeResourceDictionary dictionary = new();
            Assert.Equal(MuzzleTheme.Light, dictionary.Theme);
            Assert.Equal(MuzzleAccent.Coral, dictionary.Accent);
            AssertSlots(dictionary, "Muzzle.Light.xaml", "Muzzle.Accent.Coral.Light.xaml");
            Assert.Equal("Light", dictionary["Muzzle.ThemeName"]);
            Assert.Equal("#FFF497A9", dictionary["Muzzle.Color.Primary"].ToString());
        });
    }

    [StaTheory]
    [InlineData(MuzzleTheme.Dark, MuzzleAccent.Lavender, "#FF181622", "#FFB8A9F2")]
    [InlineData(MuzzleTheme.Light, MuzzleAccent.Mint, "#FFFAF8F6", "#FF7CCBB9")]
    [InlineData(MuzzleTheme.Dark, MuzzleAccent.Peach, "#FF181622", "#FFF7B492")]
    public void ConfiguredThemeAndAccent_AreMergedInOrder(MuzzleTheme theme, MuzzleAccent accent, string canvas, string primary) {
        fixture.Run(() => {
            MuzzleThemeResourceDictionary dictionary = new() { Theme = theme, Accent = accent };
            string variant = (theme == MuzzleTheme.Dark) ? "Dark" : "Light";
            AssertSlots(dictionary, $"Muzzle.{variant}.xaml", $"Muzzle.Accent.{accent}.{variant}.xaml");
            Assert.Equal(canvas, dictionary["Muzzle.Color.Canvas"].ToString());
            Assert.Equal(primary, dictionary["Muzzle.Color.Primary"].ToString());
            Assert.NotNull(dictionary["Muzzle.Button.Primary"]);
        });
    }

    [StaFact]
    public void CustomAccent_FallsBackToCoral() {
        fixture.Run(() => {
            MuzzleThemeResourceDictionary dictionary = new() { Accent = MuzzleAccent.Custom };
            Assert.Equal(MuzzleAccent.Coral, dictionary.Accent);
        });
    }

    [StaFact]
    public void AsApplicationResources_TheConfiguredThemeIsAppliedOnInitialisation() {
        fixture.Run(() => {
            ResourceDictionary original = Application.Current.Resources;
            try {
                Application.Current.Resources = new MuzzleThemeResourceDictionary { Theme = MuzzleTheme.Dark, Accent = MuzzleAccent.Mint };
                Assert.Equal(MuzzleTheme.Dark, MuzzleThemeManager.DetectAppliedTheme());
                Assert.Equal(MuzzleAccent.Mint, MuzzleThemeManager.DetectAppliedAccent());
                Assert.Equal("#FF181622", ((Color)Application.Current.FindResource("Muzzle.Color.Canvas")).ToString());
                Assert.Equal("#FF9FD8CB", ((Color)Application.Current.FindResource("Muzzle.Color.Primary")).ToString());

                // The manager keeps working against it: switching the theme keeps the accent.
                MuzzleThemeManager.ApplyAccent(MuzzleAccent.Mint);
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
                Assert.Equal("#FF7CCBB9", ((Color)Application.Current.FindResource("Muzzle.Color.Primary")).ToString());
            }
            finally {
                Application.Current.Resources = original;
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    private static void AssertSlots(ResourceDictionary dictionary, string palette, string accent) {
        string[] sources = dictionary.MergedDictionaries.Select(static d => d.Source?.OriginalString ?? "(generated)").ToArray();
        Assert.Equal(3, sources.Length);
        Assert.EndsWith("Themes/Muzzle.xaml", sources[0]);
        Assert.EndsWith(palette, sources[1]);
        Assert.EndsWith(accent, sources[2]);
    }
}