using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A round, squishy colour swatch that applies its <see cref="Accent"/> through <see cref="MuzzleThemeManager"/> when
/// clicked. It shows the accent as it looks in the current base theme, and while it is the active accent a ring and a
/// checkmark spring in. Put several side by side for an accent picker.
/// </summary>
public class AccentSwatch : ButtonBase {
    /// <summary>Identifies the <see cref="Accent"/> dependency property.</summary>
    public static readonly DependencyProperty AccentProperty = DependencyProperty.Register(
        nameof(Accent), typeof(MuzzleAccent), typeof(AccentSwatch), new PropertyMetadata(MuzzleAccent.Coral, OnAccentPropertyChanged));

    /// <summary>Identifies the <see cref="AnimateTransition"/> dependency property.</summary>
    public static readonly DependencyProperty AnimateTransitionProperty = DependencyProperty.Register(
        nameof(AnimateTransition), typeof(bool), typeof(AccentSwatch), new PropertyMetadata(true));

    private static readonly DependencyPropertyKey IsActivePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsActive), typeof(bool), typeof(AccentSwatch), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsActive"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsActiveProperty = IsActivePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey SwatchBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(SwatchBrush), typeof(Brush), typeof(AccentSwatch), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="SwatchBrush"/> read-only dependency property.</summary>
    public static readonly DependencyProperty SwatchBrushProperty = SwatchBrushPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey CheckBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(CheckBrush), typeof(Brush), typeof(AccentSwatch), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CheckBrush"/> read-only dependency property.</summary>
    public static readonly DependencyProperty CheckBrushProperty = CheckBrushPropertyKey.DependencyProperty;

    static AccentSwatch() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(AccentSwatch), new FrameworkPropertyMetadata(typeof(AccentSwatch)));
    }

    /// <summary>Initializes a new instance of the <see cref="AccentSwatch"/> class.</summary>
    public AccentSwatch() {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        UpdateState();
    }

    /// <summary>The accent this swatch applies. <see cref="MuzzleAccent.Custom"/> shows the current custom colour.</summary>
    public MuzzleAccent Accent {
        get => (MuzzleAccent)GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    /// <summary>Crossfade the windows when switching (default true).</summary>
    public bool AnimateTransition {
        get => (bool)GetValue(AnimateTransitionProperty);
        set => SetValue(AnimateTransitionProperty, value);
    }

    /// <summary>True while <see cref="Accent"/> is the application's accent.</summary>
    public bool IsActive => (bool)GetValue(IsActiveProperty);

    /// <summary>The accent's base colour in the current base theme.</summary>
    public Brush? SwatchBrush => (Brush?)GetValue(SwatchBrushProperty);

    /// <summary>A colour that reads on <see cref="SwatchBrush"/> (used for the checkmark).</summary>
    public Brush? CheckBrush => (Brush?)GetValue(CheckBrushProperty);

    /// <summary>The base and on-accent colours of a preset accent, for the given variant.</summary>
    public static (Color Base, Color On) GetPresetColors(MuzzleAccent accent, bool dark) {
        return (accent, dark) switch {
            (MuzzleAccent.Lavender, false) => (Color.FromRgb(0xA8, 0x96, 0xE6), Colors.White),
            (MuzzleAccent.Lavender, true) => (Color.FromRgb(0xB8, 0xA9, 0xF2), Color.FromRgb(0x18, 0x16, 0x22)),
            (MuzzleAccent.Mint, false) => (Color.FromRgb(0x7C, 0xCB, 0xB9), Color.FromRgb(0x1E, 0x33, 0x2E)),
            (MuzzleAccent.Mint, true) => (Color.FromRgb(0x9F, 0xD8, 0xCB), Color.FromRgb(0x18, 0x16, 0x22)),
            (MuzzleAccent.Peach, false) => (Color.FromRgb(0xF5, 0xA6, 0x7E), Colors.White),
            (MuzzleAccent.Peach, true) => (Color.FromRgb(0xF7, 0xB4, 0x92), Color.FromRgb(0x18, 0x16, 0x22)),
            (_, true) => (Color.FromRgb(0xF6, 0xA2, 0xB3), Color.FromRgb(0x3A, 0x1A, 0x26)),
            _ => (Color.FromRgb(0xF4, 0x97, 0xA9), Color.FromRgb(0x4A, 0x23, 0x33))
        };
    }

    /// <inheritdoc />
    protected override void OnClick() {
        Jelly.Wobble(this);
        if (Accent != MuzzleAccent.Custom) {
            MuzzleThemeManager.ApplyAccent(Accent, AnimateTransition);
        }
        else if (MuzzleThemeManager.CustomAccentColor is Color custom) {
            MuzzleThemeManager.ApplyCustomAccent(custom, AnimateTransition);
        }
        base.OnClick();
    }

    private static void OnAccentPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((AccentSwatch)d).UpdateState();
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        MuzzleThemeManager.AccentChanged += OnAccentChanged;
        MuzzleThemeManager.ThemeChanged += OnThemeChanged;
        UpdateState();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        MuzzleThemeManager.AccentChanged -= OnAccentChanged;
        MuzzleThemeManager.ThemeChanged -= OnThemeChanged;
    }

    private void OnAccentChanged(object? sender, MuzzleAccentChangedEventArgs e) {
        UpdateState();
    }

    private void OnThemeChanged(object? sender, MuzzleThemeChangedEventArgs e) {
        UpdateState();
    }

    private void UpdateState() {
        bool dark = MuzzleThemeManager.IsDark;
        (Color swatch, Color check) = GetPresetColors(Accent, dark);
        if (Accent == MuzzleAccent.Custom) {
            swatch = MuzzleThemeManager.CustomAccentColor ?? Color.FromRgb(0xC4, 0xBA, 0xB1);
            check = Colors.White;
        }
        SetValue(SwatchBrushPropertyKey, Freeze(new SolidColorBrush(swatch)));
        SetValue(CheckBrushPropertyKey, Freeze(new SolidColorBrush(check)));
        bool active = MuzzleThemeManager.CurrentAccent == Accent;
        SetValue(IsActivePropertyKey, active);
        string label = $"{Accent} accent";
        ToolTip = active ? $"{label} (active)" : label;
        AutomationProperties.SetName(this, label);
        AutomationProperties.SetItemStatus(this, active ? "Selected" : string.Empty);
    }

    private static SolidColorBrush Freeze(SolidColorBrush brush) {
        brush.Freeze();
        return brush;
    }
}