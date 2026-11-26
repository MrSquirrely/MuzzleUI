using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A tactile keyboard key: a small pill with a mechanical-keycap bottom lip that depresses when clicked or when
/// <see cref="Press"/> is called (e.g. when its shortcut fires).
/// </summary>
[TemplatePart(Name = CapPartName, Type = typeof(Border))]
public class Keycap : Control {
    /// <summary>Name of the Border template part that forms the keycap (its bottom lip dips when pressed).</summary>
    public const string CapPartName = "PART_Cap";

    /// <summary>Identifies the <see cref="KeyText"/> dependency property.</summary>
    public static readonly DependencyProperty KeyTextProperty = DependencyProperty.Register(
        nameof(KeyText), typeof(string), typeof(Keycap), new PropertyMetadata(string.Empty));

    private static readonly Thickness RestLip = new(1, 1, 1, 3);
    private static readonly Thickness PressedLip = new(1, 1, 1, 1.5);

    private Border? cap;
    private bool isDown;

    static Keycap() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Keycap), new FrameworkPropertyMetadata(typeof(Keycap)));
        FocusableProperty.OverrideMetadata(typeof(Keycap), new FrameworkPropertyMetadata(false));
    }

    /// <summary>The key's label, e.g. "Ctrl", "Shift", "K", "↵".</summary>
    public string KeyText {
        get => (string)GetValue(KeyTextProperty);
        set => SetValue(KeyTextProperty, value);
    }

    /// <summary>Plays a full press: the cap dips onto its lip and springs back.</summary>
    public void Press() {
        PressDown();
        System.Windows.Threading.DispatcherTimer timer = new(System.Windows.Threading.DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromMilliseconds(110) };
        timer.Tick += (_, _) => {
            timer.Stop();
            Release();
        };
        timer.Start();
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        cap = GetTemplateChild(CapPartName) as Border;
        if (cap is not null) {
            cap.BorderThickness = RestLip;
            cap.RenderTransform = new TranslateTransform();
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonDown(e);
        PressDown();
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonUp(e);
        Release();
    }

    /// <inheritdoc />
    protected override void OnMouseLeave(MouseEventArgs e) {
        base.OnMouseLeave(e);
        Release();
    }

    private void PressDown() {
        if ((cap?.RenderTransform is not TranslateTransform shift) || isDown) {
            return;
        }
        isDown = true;
        Duration quick = new(TimeSpan.FromMilliseconds(70));
        shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(1.5, quick));
        cap.BeginAnimation(Border.BorderThicknessProperty, new ThicknessAnimation(PressedLip, quick));
    }

    private void Release() {
        if ((cap?.RenderTransform is not TranslateTransform shift) || !isDown) {
            return;
        }
        isDown = false;
        bool motion = Jelly.IsMotionEnabled(this);
        Duration back = new(TimeSpan.FromMilliseconds(motion ? 420 : 1));
        IEasingFunction? spring = motion ? MuzzleEasings.Get(this, MuzzleEasings.JellyKey) : null;
        shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, back) { EasingFunction = spring });
        cap.BeginAnimation(Border.BorderThicknessProperty, new ThicknessAnimation(RestLip, new Duration(TimeSpan.FromMilliseconds(160))));
    }
}

/// <summary>
/// A keyboard shortcut as a row of <see cref="Keycap"/>s: <c>&lt;mz:ShortcutBadge Shortcut="Ctrl+Shift+P" /&gt;</c>.
/// Chords separate with commas ("Ctrl+K, Ctrl+S").
/// </summary>
public class ShortcutBadge : Control {
    /// <summary>Identifies the <see cref="Shortcut"/> dependency property.</summary>
    public static readonly DependencyProperty ShortcutProperty = DependencyProperty.Register(
        nameof(Shortcut), typeof(string), typeof(ShortcutBadge), new PropertyMetadata(string.Empty, OnShortcutChanged));

    private static readonly DependencyPropertyKey KeysPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Keys), typeof(IReadOnlyList<string>), typeof(ShortcutBadge), new PropertyMetadata(Array.Empty<string>()));

    /// <summary>Identifies the <see cref="Keys"/> read-only dependency property.</summary>
    public static readonly DependencyProperty KeysProperty = KeysPropertyKey.DependencyProperty;

    static ShortcutBadge() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ShortcutBadge), new FrameworkPropertyMetadata(typeof(ShortcutBadge)));
        FocusableProperty.OverrideMetadata(typeof(ShortcutBadge), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Gets or sets the shortcut. The default is an empty string. This is a dependency property.</summary>
    public string Shortcut {
        get => (string)GetValue(ShortcutProperty);
        set => SetValue(ShortcutProperty, value);
    }

    /// <summary>The individual key labels (chord separators appear as ",").</summary>
    public IReadOnlyList<string> Keys => (IReadOnlyList<string>)GetValue(KeysProperty);

    /// <summary>Splits "Ctrl+Shift+P" into friendly key labels ("Ctrl", "Shift", "P"); "Ctrl++" ends with "+".</summary>
    public static IReadOnlyList<string> ParseKeys(string? shortcut) {
        List<string> keys = [];
        if (string.IsNullOrWhiteSpace(shortcut)) {
            return keys;
        }
        string[] chords = shortcut.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        for (int c = 0; c < chords.Length; c++) {
            if (c > 0) {
                keys.Add(",");
            }
            string chord = chords[c];
            bool endsWithPlus = chord.EndsWith("++", StringComparison.Ordinal) || (chord == "+");
            foreach (string part in chord.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)) {
                keys.Add(Friendly(part));
            }
            if (endsWithPlus) {
                keys.Add("+");
            }
        }
        return keys;
    }

    /// <summary>Presses every keycap in the badge.</summary>
    public void Press() {
        foreach (Keycap cap in FindKeycaps(this)) {
            cap.Press();
        }
    }

    private static string Friendly(string key) {
        return key.ToLowerInvariant() switch {
            "control" or "ctl" => "Ctrl",
            "windows" or "win" or "lwin" or "rwin" => "Win",
            "return" => "Enter",
            "escape" => "Esc",
            "up" => "↑",
            "down" => "↓",
            "left" => "←",
            "right" => "→",
            "space" => "Space",
            "delete" => "Del",
            _ => (key.Length == 1) ? key.ToUpperInvariant() : char.ToUpperInvariant(key[0]) + key[1..]
        };
    }

    private static void OnShortcutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        d.SetValue(KeysPropertyKey, ParseKeys((string?)e.NewValue));
    }

    private static IEnumerable<Keycap> FindKeycaps(DependencyObject root) {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is Keycap keycap) {
                yield return keycap;
            }
            foreach (Keycap nested in FindKeycaps(child)) {
                yield return nested;
            }
        }
    }
}