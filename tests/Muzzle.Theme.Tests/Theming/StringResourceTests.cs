using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Theming;

/// <summary>The theme's built-in text lives in Muzzle.String.* resources that an app can override.</summary>
[Collection(WpfCollection.Name)]
public sealed class StringResourceTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public void Strings_AreAllNonEmptyTextUnderTheSharedPrefix() {
        fixture.Run(() => {
            ResourceDictionary strings = new() { Source = new Uri("pack://application:,,,/Muzzle.Theme;component/Themes/Tokens/Strings.xaml", UriKind.Absolute) };
            Assert.True(strings.Count >= 90, $"Only {strings.Count} strings.");
            foreach (DictionaryEntry entry in strings) {
                string key = Assert.IsType<string>(entry.Key);
                Assert.StartsWith(MuzzleStrings.KeyPrefix, key, StringComparison.Ordinal);
                Assert.False(string.IsNullOrWhiteSpace(Assert.IsType<string>(entry.Value)), key + " is empty.");
                // Merged into the theme, so every one of them resolves application-wide.
                Assert.Equal(entry.Value, Application.Current.FindResource(key));
            }
        });
    }

    [StaFact]
    public void Get_ReturnsTheResourceItsOverrideOrTheFallback() {
        fixture.Run(() => {
            try {
                Assert.Equal("Close", MuzzleStrings.Get("Window.Close"));
                Assert.Equal("fallback", MuzzleStrings.Get("No.Such.Key", "fallback"));
                Assert.Equal(string.Empty, MuzzleStrings.Get("No.Such.Key"));

                Application.Current.Resources["Muzzle.String.Growl.Yes"] = "Ja";
                Application.Current.Resources["Muzzle.String.PasswordStrength.Empty"] = "Passwort eingeben";
                Assert.Equal("Ja", MuzzleStrings.Get("Growl.Yes"));
                Assert.Equal("Ja", new GrowlInfo().ConfirmText);
                Assert.Equal("No", new GrowlInfo().CancelText);
                Assert.Equal("Passwort eingeben", new PasswordStrengthMeter { Password = string.Empty }.StrengthLabel);
            }
            finally {
                Application.Current.Resources.Remove("Muzzle.String.Growl.Yes");
                Application.Current.Resources.Remove("Muzzle.String.PasswordStrength.Empty");
            }
        });
    }

    [StaFact]
    public Task Templates_ShowAnAppsTranslationAndFollowItLive() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            SearchBar search = new();
            Pagination pagination = new() { PageCount = 9, CurrentPage = 2, ShowJumpBox = true };
            MuzzleWindow window = new() { Title = "Test" };
            MuzzleWpfTestFixture.ShowOffScreen(new StackPanel { Children = { search, pagination } }, 700, 300, window);
            try {
                await MuzzleWpfTestFixture.WaitAsync(80);
                Button close = Assert.IsType<Button>(window.Template.FindName("PART_CloseButton", window));
                Button maximize = Assert.IsType<Button>(window.Template.FindName("PART_MaximizeButton", window));
                Assert.Equal("Close", AutomationProperties.GetName(close));
                Assert.Equal("Close", close.ToolTip);
                Assert.Equal("Maximize", AutomationProperties.GetName(maximize));
                Assert.Equal("Search…", MuzzleAssist.GetPlaceholder(search));
                Assert.Contains(Descendants<TextBlock>(pagination), static t => t.Text == "Go to");

                // An override in the app's resources reaches templates that are already on screen.
                Application.Current.Resources["Muzzle.String.Window.Close"] = "Schließen";
                Application.Current.Resources["Muzzle.String.SearchBar.Placeholder"] = "Suchen…";
                Application.Current.Resources["Muzzle.String.Pagination.GoTo"] = "Gehe zu";
                await MuzzleWpfTestFixture.WaitAsync(50);
                Assert.Equal("Schließen", AutomationProperties.GetName(close));
                Assert.Equal("Schließen", close.ToolTip);
                Assert.Equal("Suchen…", MuzzleAssist.GetPlaceholder(search));
                Assert.Contains(Descendants<TextBlock>(pagination), static t => t.Text == "Gehe zu");
            }
            finally {
                Application.Current.Resources.Remove("Muzzle.String.Window.Close");
                Application.Current.Resources.Remove("Muzzle.String.SearchBar.Placeholder");
                Application.Current.Resources.Remove("Muzzle.String.Pagination.GoTo");
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    private static System.Collections.Generic.IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(root, i);
            if (child is T match) {
                yield return match;
            }
            foreach (T nested in Descendants<T>(child)) {
                yield return nested;
            }
        }
    }
}