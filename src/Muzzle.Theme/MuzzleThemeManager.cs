using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

using Microsoft.Win32;

namespace Muzzle.Theme;

/// <summary>Which Muzzle palette to use.</summary>
public enum MuzzleTheme {
    /// <summary>The cozy light palette (warm cream canvas, white surfaces).</summary>
    Light,
    /// <summary>The "Pastel Twilight" dark palette (plum-charcoal canvas, slate-indigo surfaces).</summary>
    Dark,

    /// <summary>Follow the Windows "app mode" setting (and keep following it while the app runs).</summary>
    System
}

/// <summary>Muzzle's accent colour: the primary ramp plus the secondary / tertiary roles that harmonise with it.</summary>
public enum MuzzleAccent {
    /// <summary>Toe-bean blush, the default.</summary>
    Coral,

    /// <summary>Soft wisteria.</summary>
    Lavender,

    /// <summary>Pastel sage.</summary>
    Mint,

    /// <summary>Warm apricot.</summary>
    Peach,

    /// <summary>A ramp derived from any colour via <see cref="MuzzleThemeManager.ApplyCustomAccent"/>.</summary>
    Custom
}

/// <summary>Data for <see cref="MuzzleThemeManager.ThemeChanged"/>.</summary>
public sealed class MuzzleThemeChangedEventArgs(MuzzleTheme theme, MuzzleTheme effectiveTheme) : EventArgs {
    /// <summary>The requested theme (may be <see cref="MuzzleTheme.System"/>).</summary>
    public MuzzleTheme Theme { get; } = theme;

    /// <summary>The palette actually applied: <see cref="MuzzleTheme.Light"/> or <see cref="MuzzleTheme.Dark"/>.</summary>
    public MuzzleTheme EffectiveTheme { get; } = effectiveTheme;
}

/// <summary>Data for <see cref="MuzzleThemeManager.AccentChanged"/>.</summary>
public sealed class MuzzleAccentChangedEventArgs(MuzzleAccent accent, Color? customColor) : EventArgs {
    /// <summary>Gets the accent that was applied.</summary>
    public MuzzleAccent Accent { get; } = accent;

    /// <summary>The base colour when <see cref="Accent"/> is <see cref="MuzzleAccent.Custom"/>.</summary>
    public Color? CustomColor { get; } = customColor;
}

/// <summary>
/// Switches Muzzle's base theme (Light / Dark / System) and accent colour (Coral / Lavender / Mint / Peach / Custom)
/// at runtime, independently of each other. The base palette (Muzzle.Light.xaml / Muzzle.Dark.xaml) and the accent
/// overlay (Themes/Accents/Muzzle.Accent.&lt;Name&gt;.&lt;Light|Dark&gt;.xaml), which is kept merged right after the
/// palette, are swapped inside the application's merged dictionaries. Everything themed through DynamicResource updates
/// at once: open windows, popups, adorner overlays, the desktop Growl window and MuzzleWindow's DWM frame.
/// </summary>
public static class MuzzleThemeManager {
    /// <summary>Resource key every accent dictionary defines, used to find the accent slot.</summary>
    public const string AccentSlotKey = "Muzzle.AccentSlot";

    /// <summary>Resource key holding the active accent's name ("Coral", …, "Custom").</summary>
    public const string AccentNameKey = "Muzzle.AccentName";

    private const string LightFile = "Muzzle.Light.xaml";
    private const string DarkFile = "Muzzle.Dark.xaml";
    private const string AccentFolder = "/Accents/";
    private const string AccentPrefix = "Muzzle.Accent.";
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    private static readonly string[] AccentTokens = [
        "Primary", "PrimaryHover", "PrimaryPressed", "PrimarySoft", "PrimarySoftHover", "PrimaryText", "OnPrimary", "FocusRing", "PrimaryWash"
    ];

    private static bool watchingSystem;
    private static MuzzleTheme? currentTheme;
    private static MuzzleTheme? effectiveTheme;
    private static MuzzleAccent? currentAccent;

    /// <summary>Raised after a base theme has been applied.</summary>
    public static event EventHandler<MuzzleThemeChangedEventArgs>? ThemeChanged;

    /// <summary>Raised after an accent has been applied.</summary>
    public static event EventHandler<MuzzleAccentChangedEventArgs>? AccentChanged;

    /// <summary>The requested theme (until <see cref="ApplyTheme"/> is called: the palette merged in XAML).</summary>
    public static MuzzleTheme CurrentTheme {
        get => currentTheme ?? EffectiveTheme;
        private set => currentTheme = value;
    }

    /// <summary>The palette in use: Light or Dark (System resolved).</summary>
    public static MuzzleTheme EffectiveTheme {
        get => effectiveTheme ??= DetectAppliedTheme();
        private set => effectiveTheme = value;
    }

    /// <summary>Gets whether the Dark palette is in effect.</summary>
    public static bool IsDark => EffectiveTheme == MuzzleTheme.Dark;

    /// <summary>The accent in use (until one is applied: the accent merged in XAML, Coral by default).</summary>
    public static MuzzleAccent CurrentAccent {
        get => currentAccent ??= DetectAppliedAccent();
        private set => currentAccent = value;
    }

    /// <summary>The base colour of the custom accent, when <see cref="CurrentAccent"/> is Custom.</summary>
    public static Color? CustomAccentColor { get; private set; }

    /// <summary>Length of the optional crossfade.</summary>
    public static TimeSpan TransitionDuration { get; set; } = TimeSpan.FromMilliseconds(380);

    /// <summary>
    /// Applies <paramref name="theme"/>, keeping the current accent. With <paramref name="animate"/>, each open window
    /// shows a snapshot of its old look that fades away over the new one, so the switch feels gentle rather than instant.
    /// </summary>
    public static void ApplyTheme(MuzzleTheme theme, bool animate = false) {
        Application application = Application.Current ?? throw new InvalidOperationException("MuzzleThemeManager needs a running WPF Application.");
        if (!application.Dispatcher.CheckAccess()) {
            application.Dispatcher.BeginInvoke(new Action(() => ApplyTheme(theme, animate)));
            return;
        }
        CurrentTheme = theme;
        WatchSystemTheme(theme == MuzzleTheme.System);
        MuzzleTheme effective = (theme == MuzzleTheme.System) ? (IsSystemDark() ? MuzzleTheme.Dark : MuzzleTheme.Light) : theme;
        bool willChange = (effective != EffectiveTheme) || (FindEffectivePalette(application) is null);
        Apply(application, effective, CurrentAccent, CustomAccentColor, animate && willChange);
        MuzzleShadows.Refresh();
        ThemeChanged?.Invoke(null, new MuzzleThemeChangedEventArgs(theme, effective));
    }

    /// <summary>Applies one of the preset accents, keeping the current base theme.</summary>
    public static void ApplyAccent(MuzzleAccent accent, bool animate = false) {
        if (accent == MuzzleAccent.Custom) {
            if (CustomAccentColor is not Color custom) {
                throw new ArgumentException("Use ApplyCustomAccent(Color) to choose a custom accent colour.", nameof(accent));
            }
            ApplyCustomAccent(custom, animate);
            return;
        }
        ApplyAccentCore(accent, null, animate);
    }

    /// <summary>
    /// Derives a full accent ramp (hover, pressed, soft fills, readable text, on-accent foreground, focus ring) from
    /// <paramref name="baseColor"/>, with secondary / tertiary roles picked so they don't share its hue.
    /// </summary>
    public static void ApplyCustomAccent(Color baseColor, bool animate = false) {
        ApplyAccentCore(MuzzleAccent.Custom, Color.FromRgb(baseColor.R, baseColor.G, baseColor.B), animate);
    }

    /// <summary>Reads the theme a palette slot currently holds (Light when none is found).</summary>
    public static MuzzleTheme DetectAppliedTheme() {
        string? source = (Application.Current is Application application) ? FindEffectivePalette(application)?.Source?.OriginalString : null;
        return (source?.EndsWith(DarkFile, StringComparison.OrdinalIgnoreCase) ?? false) ? MuzzleTheme.Dark : MuzzleTheme.Light;
    }

    /// <summary>Reads the accent currently in effect (Coral when none is found).</summary>
    public static MuzzleAccent DetectAppliedAccent() {
        object? name = Application.Current?.TryFindResource(AccentNameKey);
        return ((name is string text) && Enum.TryParse(text, out MuzzleAccent accent)) ? accent : MuzzleAccent.Coral;
    }

    /// <summary>True when Windows apps are set to dark mode (HKCU …\Personalize\AppsUseLightTheme = 0).</summary>
    public static bool IsSystemDark() {
        try {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
            return key?.GetValue("AppsUseLightTheme") is int value && (value == 0);
        }
        catch (Exception exception) when (exception is System.Security.SecurityException or UnauthorizedAccessException or System.IO.IOException) {
            return false;
        }
    }

    /// <summary>
    /// Builds the accent dictionary for <paramref name="accent"/> in the given variant: the packaged file for presets,
    /// or a generated ramp for <see cref="MuzzleAccent.Custom"/>.
    /// </summary>
    public static ResourceDictionary CreateAccentDictionary(MuzzleAccent accent, bool dark, Color? customColor = null) {
        if (accent != MuzzleAccent.Custom) {
            return CreatePackDictionary($"Accents/{AccentPrefix}{accent}.{(dark ? "Dark" : "Light")}.xaml");
        }
        Color baseColor = customColor ?? throw new ArgumentNullException(nameof(customColor), "A custom accent needs a base colour.");
        // Secondary / tertiary roles come from the preset whose supporting hues sit furthest from this colour.
        ResourceDictionary dictionary = new();
        dictionary.MergedDictionaries.Add(CreateAccentDictionary(GetCompanionPreset(baseColor), dark));
        dictionary[AccentSlotKey] = true;
        dictionary[AccentNameKey] = nameof(MuzzleAccent.Custom);
        foreach ((string token, Color color) in DeriveRamp(baseColor, dark)) {
            dictionary["Muzzle.Color." + token] = color;
            SolidColorBrush brush = new(color);
            brush.Freeze();
            dictionary["Muzzle.Brush." + token] = brush;
        }
        return dictionary;
    }

    private static void ApplyAccentCore(MuzzleAccent accent, Color? customColor, bool animate) {
        Application application = Application.Current ?? throw new InvalidOperationException("MuzzleThemeManager needs a running WPF Application.");
        if (!application.Dispatcher.CheckAccess()) {
            application.Dispatcher.BeginInvoke(new Action(() => ApplyAccentCore(accent, customColor, animate)));
            return;
        }
        bool same = (accent == CurrentAccent) && (CustomAccentColor == customColor) && (FindAccentSlots(application).Count > 0);
        CurrentAccent = accent;
        CustomAccentColor = customColor;
        Apply(application, EffectiveTheme, accent, customColor, animate && !same);
        AccentChanged?.Invoke(null, new MuzzleAccentChangedEventArgs(accent, customColor));
    }

    /// <summary>Puts the wanted palette and accent into their slots and tells every open window about it.</summary>
    private static void Apply(Application application, MuzzleTheme effective, MuzzleAccent accent, Color? customColor, bool animate) {
        bool dark = effective == MuzzleTheme.Dark;
        List<Popup> overlays = (animate && SystemParameters.ClientAreaAnimation) ? CaptureWindows(application) : [];
        bool nested = SwapPalette(application, dark ? DarkFile : LightFile);
        nested |= SwapAccent(application, accent, dark, customColor);
        if (nested) {
            // Dictionaries merged below Application.Resources (e.g. inside Muzzle.xaml) don't tell the application
            // when they change, so existing DynamicResource references would keep the old colours. Briefly merging a
            // copy of the new palette and accent (in slot order) at the top level raises the application-wide change
            // notification for exactly their keys; removing it leaves the nested slots as the single source of truth.
            ResourceDictionary notifier = new();
            notifier.MergedDictionaries.Add(CreatePackDictionary(dark ? DarkFile : LightFile));
            notifier.MergedDictionaries.Add(CreateAccentDictionary(accent, dark, customColor));
            application.Resources.MergedDictionaries.Add(notifier);
            _ = application.Resources.MergedDictionaries.Remove(notifier);
        }
        EffectiveTheme = effective;
        foreach (Popup overlay in overlays) {
            FadeOut(overlay);
        }
    }

    /// <summary>Swaps every palette slot to <paramref name="file"/>. Returns true when a nested slot changed.</summary>
    private static bool SwapPalette(Application application, string file) {
        List<(ResourceDictionary Parent, int Index)> slots = FindPaletteSlots(application);
        if (slots.Count == 0) {
            application.Resources.MergedDictionaries.Add(CreatePackDictionary(file));
            return false;
        }
        bool nested = false;
        foreach ((ResourceDictionary parent, int index) in slots) {
            string current = parent.MergedDictionaries[index].Source?.OriginalString ?? string.Empty;
            if (!current.EndsWith(file, StringComparison.OrdinalIgnoreCase)) {
                parent.MergedDictionaries[index] = CreatePackDictionary(file);
                nested |= !ReferenceEquals(parent, application.Resources);
            }
        }
        return nested;
    }

    /// <summary>
    /// Makes sure an accent slot sits right after the palette in effect (so its keys override the palette's), then
    /// swaps every accent slot to the wanted accent and variant. Returns true when a nested slot changed.
    /// </summary>
    private static bool SwapAccent(Application application, MuzzleAccent accent, bool dark, Color? customColor) {
        List<(ResourceDictionary Parent, int Index)> palettes = FindPaletteSlots(application);
        if (palettes.Count > 0) {
            (ResourceDictionary parent, int index) = palettes[^1];
            bool followed = (index + 1 < parent.MergedDictionaries.Count) && IsAccentSlot(parent.MergedDictionaries[index + 1]);
            if (!followed) {
                parent.MergedDictionaries.Insert(index + 1, CreateAccentDictionary(accent, dark, customColor));
                if (!ReferenceEquals(parent, application.Resources)) {
                    return true;
                }
            }
        }
        else if (FindAccentSlots(application).Count == 0) {
            application.Resources.MergedDictionaries.Add(CreateAccentDictionary(accent, dark, customColor));
            return false;
        }
        string? wantedFile = (accent == MuzzleAccent.Custom) ? null : $"{AccentPrefix}{accent}.{(dark ? "Dark" : "Light")}.xaml";
        bool nested = false;
        foreach ((ResourceDictionary parent, int index) in FindAccentSlots(application)) {
            string current = parent.MergedDictionaries[index].Source?.OriginalString ?? string.Empty;
            bool upToDate = (wantedFile is not null) && current.EndsWith(wantedFile, StringComparison.OrdinalIgnoreCase);
            if (!upToDate) {
                parent.MergedDictionaries[index] = CreateAccentDictionary(accent, dark, customColor);
                nested |= !ReferenceEquals(parent, application.Resources);
            }
        }
        return nested;
    }

    private static List<(ResourceDictionary Parent, int Index)> FindPaletteSlots(Application application) {
        List<(ResourceDictionary Parent, int Index)> slots = [];
        Collect(application.Resources, slots, IsPaletteSlot);
        return slots;
    }

    private static List<(ResourceDictionary Parent, int Index)> FindAccentSlots(Application application) {
        List<(ResourceDictionary Parent, int Index)> slots = [];
        Collect(application.Resources, slots, IsAccentSlot);
        return slots;
    }

    private static void Collect(ResourceDictionary dictionary, List<(ResourceDictionary Parent, int Index)> slots, Func<ResourceDictionary, bool> isSlot) {
        for (int i = 0; i < dictionary.MergedDictionaries.Count; i++) {
            ResourceDictionary child = dictionary.MergedDictionaries[i];
            if (isSlot(child)) {
                slots.Add((dictionary, i));
            }
            else if (!IsPaletteSlot(child) && !IsAccentSlot(child)) {
                Collect(child, slots, isSlot);
            }
        }
    }

    private static bool IsPaletteSlot(ResourceDictionary dictionary) {
        string source = dictionary.Source?.OriginalString ?? string.Empty;
        return source.EndsWith(LightFile, StringComparison.OrdinalIgnoreCase) || source.EndsWith(DarkFile, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAccentSlot(ResourceDictionary dictionary) {
        if (dictionary.Source is Uri source) {
            return source.OriginalString.Contains(AccentFolder + AccentPrefix, StringComparison.OrdinalIgnoreCase);
        }
        // Generated (custom) accents have no Source; they carry the marker key themselves.
        foreach (object key in dictionary.Keys) {
            if (Equals(key, AccentSlotKey)) {
                return true;
            }
        }
        return false;
    }

    private static ResourceDictionary? FindEffectivePalette(Application application) {
        List<(ResourceDictionary Parent, int Index)> slots = FindPaletteSlots(application);
        // Merged dictionaries are searched last-first, so the last slot in document order is the one in effect.
        return (slots.Count == 0) ? null : slots[^1].Parent.MergedDictionaries[slots[^1].Index];
    }

    private static ResourceDictionary CreatePackDictionary(string relativePath) {
        return new ResourceDictionary { Source = new Uri($"pack://application:,,,/Muzzle.Theme;component/Themes/{relativePath}", UriKind.Absolute) };
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Custom accent ramp
    // ---------------------------------------------------------------------------------------------------------------

    private static MuzzleAccent GetCompanionPreset(Color color) {
        (double hue, double saturation, _) = ToHsl(color);
        if (saturation < 0.12) {
            return MuzzleAccent.Coral;
        }
        if ((hue >= 225) && (hue < 300)) {
            return MuzzleAccent.Lavender; // blush secondary, mint tertiary
        }
        if ((hue >= 100) && (hue < 200)) {
            return MuzzleAccent.Mint; // lavender secondary, peach tertiary
        }
        return MuzzleAccent.Coral; // lavender secondary, mint tertiary
    }

    private static IEnumerable<(string Token, Color Color)> DeriveRamp(Color baseColor, bool dark) {
        Color surface = dark ? Color.FromRgb(0x22, 0x1F, 0x2E) : Colors.White;
        (double hue, double saturation, _) = ToHsl(baseColor);
        bool lightBase = RelativeLuminance(baseColor) > 0.42;
        Color text = dark ? Mix(baseColor, Colors.White, 0.3) : FromHsl(hue, Math.Clamp(saturation, 0.35, 0.6), 0.36);
        Color onPrimary = lightBase
            ? (dark ? Color.FromRgb(0x18, 0x16, 0x22) : FromHsl(hue, Math.Min(saturation, 0.5) * 0.6, 0.18))
            : Colors.White;
        Color[] values = [
            baseColor,
            Mix(baseColor, Colors.White, 0.2),
            Mix(baseColor, Colors.Black, 0.08),
            Mix(surface, baseColor, dark ? 0.2 : 0.15),
            Mix(surface, baseColor, dark ? 0.3 : 0.25),
            text,
            onPrimary,
            Color.FromArgb((byte)(dark ? 0x80 : 0x73), baseColor.R, baseColor.G, baseColor.B),
            Mix(surface, baseColor, dark ? 0.05 : 0.06)
        ];
        for (int i = 0; i < AccentTokens.Length; i++) {
            yield return (AccentTokens[i], values[i]);
        }
    }

    private static Color Mix(Color from, Color to, double amount) {
        return Color.FromRgb(Lerp(from.R, to.R, amount), Lerp(from.G, to.G, amount), Lerp(from.B, to.B, amount));
    }

    private static byte Lerp(byte a, byte b, double t) {
        return (byte)Math.Round(a + ((b - a) * t));
    }

    private static double RelativeLuminance(Color color) {
        static double Channel(byte value) {
            double c = value / 255.0;
            return (c <= 0.03928) ? (c / 12.92) : Math.Pow((c + 0.055) / 1.055, 2.4);
        }

        return (0.2126 * Channel(color.R)) + (0.7152 * Channel(color.G)) + (0.0722 * Channel(color.B));
    }

    private static (double Hue, double Saturation, double Lightness) ToHsl(Color color) {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double lightness = (max + min) / 2;
        double delta = max - min;
        if (delta < 1e-6) {
            return (0, 0, lightness);
        }
        double saturation = delta / (1 - Math.Abs((2 * lightness) - 1));
        double hue = (max == r) ? (60 * (((g - b) / delta) % 6)) : (max == g) ? (60 * (((b - r) / delta) + 2)) : (60 * (((r - g) / delta) + 4));
        return ((hue + 360) % 360, saturation, lightness);
    }

    private static Color FromHsl(double hue, double saturation, double lightness) {
        double chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;
        double x = chroma * (1 - Math.Abs(((hue / 60) % 2) - 1));
        double m = lightness - (chroma / 2);
        (double r, double g, double b) = hue switch {
            < 60 => (chroma, x, 0.0),
            < 120 => (x, chroma, 0.0),
            < 180 => (0.0, chroma, x),
            < 240 => (0.0, x, chroma),
            < 300 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x)
        };
        return Color.FromRgb((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255));
    }

    // ---------------------------------------------------------------------------------------------------------------
    // Crossfade
    // ---------------------------------------------------------------------------------------------------------------

    // A snapshot of each visible window's old look sits in a borderless popup over it and fades away.
    private static List<Popup> CaptureWindows(Application application) {
        List<Popup> overlays = [];
        foreach (Window window in application.Windows) {
            if (!window.IsVisible || (window.WindowState == WindowState.Minimized) || (window.ActualWidth < 1) || (window.ActualHeight < 1)) {
                continue;
            }
            if (window.Content is not UIElement root) {
                continue;
            }
            DpiScale dpi = VisualTreeHelper.GetDpi(window);
            UIElement target = (VisualTreeHelper.GetChildrenCount(window) > 0) && (VisualTreeHelper.GetChild(window, 0) is UIElement chrome) ? chrome : root;
            double width = target.RenderSize.Width;
            double height = target.RenderSize.Height;
            if ((width < 1) || (height < 1)) {
                continue;
            }
            RenderTargetBitmap bitmap = new((int)Math.Ceiling(width * dpi.DpiScaleX), (int)Math.Ceiling(height * dpi.DpiScaleY), dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            bitmap.Render(target);
            bitmap.Freeze();
            Popup overlay = new() {
                PlacementTarget = target,
                Placement = PlacementMode.Relative,
                AllowsTransparency = true,
                PopupAnimation = PopupAnimation.None,
                StaysOpen = true,
                IsHitTestVisible = false,
                Focusable = false,
                Child = new Image {
                    Source = bitmap,
                    Width = width,
                    Height = height,
                    Stretch = Stretch.Fill,
                    IsHitTestVisible = false
                }
            };
            overlay.IsOpen = true;
            overlays.Add(overlay);
        }
        return overlays;
    }

    private static void FadeOut(Popup overlay) {
        if (overlay.Child is not UIElement image) {
            overlay.IsOpen = false;
            return;
        }
        DoubleAnimation fade = new(1, 0, TransitionDuration) {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
        };
        fade.Completed += (_, _) => {
            overlay.IsOpen = false;
            overlay.Child = null;
        };
        image.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    // ---------------------------------------------------------------------------------------------------------------
    // System theme
    // ---------------------------------------------------------------------------------------------------------------

    private static void WatchSystemTheme(bool watch) {
        if (watch == watchingSystem) {
            return;
        }
        watchingSystem = watch;
        if (watch) {
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }
        else {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        }
    }

    private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e) {
        if ((CurrentTheme != MuzzleTheme.System) || (e.Category is not (UserPreferenceCategory.General or UserPreferenceCategory.Color or UserPreferenceCategory.VisualStyle))) {
            return;
        }
        MuzzleTheme effective = IsSystemDark() ? MuzzleTheme.Dark : MuzzleTheme.Light;
        if (effective != EffectiveTheme) {
            Application.Current?.Dispatcher.BeginInvoke(new Action(() => ApplyTheme(MuzzleTheme.System, true)));
        }
    }
}