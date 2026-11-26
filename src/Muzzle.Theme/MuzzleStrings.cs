using System.Windows;

namespace Muzzle.Theme;

/// <summary>
/// Reads the theme's built-in text. Every string a Muzzle template or control shows on its own is a
/// <c>Muzzle.String.*</c> resource (see <c>Themes/Tokens/Strings.xaml</c>), read with <c>DynamicResource</c>.
/// To translate or reword one, declare the same key in your application's resources, after the Muzzle dictionary:
/// <c>&lt;sys:String x:Key="Muzzle.String.Window.Close"&gt;Schließen&lt;/sys:String&gt;</c>.
/// </summary>
public static class MuzzleStrings {
    /// <summary>The prefix every string resource key starts with.</summary>
    public const string KeyPrefix = "Muzzle.String.";

    /// <summary>Gets a built-in string as the application currently resolves it, overrides included.</summary>
    /// <param name="name">The key without its prefix, such as <c>"Window.Close"</c>.</param>
    /// <param name="fallback">The text to return when there is no application or the key is not defined.</param>
    /// <returns>The resolved string, or <paramref name="fallback"/> (an empty string when that is <see langword="null"/>).</returns>
    public static string Get(string name, string? fallback = null) {
        return (Application.Current?.TryFindResource(KeyPrefix + name) as string) ?? fallback ?? string.Empty;
    }
}