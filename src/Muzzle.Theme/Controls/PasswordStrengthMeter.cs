using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A four-segment pastel strength bar. Set <see cref="Password"/> (or point <see cref="PasswordBox"/> at a
/// PasswordBox) and it estimates a 0–4 <see cref="StrengthScore"/>; or set the score yourself. Segments spring to
/// their new fill and the colour glides from coral-red (Weak) through warm peach (Fair) and butter (Good) to
/// mint-sage (Strong).
/// </summary>
[TemplatePart(Name = SegmentsPartName, Type = typeof(Panel))]
public class PasswordStrengthMeter : Control {
    /// <summary>Name of the Grid template part that receives the four segments.</summary>
    public const string SegmentsPartName = "PART_Segments";

    /// <summary>Identifies the <see cref="Password"/> dependency property.</summary>
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
        nameof(Password), typeof(string), typeof(PasswordStrengthMeter), new PropertyMetadata(null, OnPasswordChanged));

    /// <summary>A PasswordBox to follow (its Password isn't bindable, so the meter listens to it directly).</summary>
    public static readonly DependencyProperty PasswordBoxProperty = DependencyProperty.Register(
        nameof(PasswordBox), typeof(PasswordBox), typeof(PasswordStrengthMeter), new PropertyMetadata(null, OnPasswordBoxChanged));

    /// <summary>Identifies the <see cref="StrengthScore"/> dependency property.</summary>
    public static readonly DependencyProperty StrengthScoreProperty = DependencyProperty.Register(
        nameof(StrengthScore), typeof(int), typeof(PasswordStrengthMeter),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnScoreChanged, CoerceScore));

    /// <summary>Identifies the <see cref="ShowLabel"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register(
        nameof(ShowLabel), typeof(bool), typeof(PasswordStrengthMeter), new PropertyMetadata(true));

    private static readonly DependencyPropertyKey StrengthLabelPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StrengthLabel), typeof(string), typeof(PasswordStrengthMeter), new PropertyMetadata("Enter a password"));

    /// <summary>Identifies the <see cref="StrengthLabel"/> read-only dependency property.</summary>
    public static readonly DependencyProperty StrengthLabelProperty = StrengthLabelPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey LabelBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(LabelBrush), typeof(Brush), typeof(PasswordStrengthMeter), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="LabelBrush"/> read-only dependency property.</summary>
    public static readonly DependencyProperty LabelBrushProperty = LabelBrushPropertyKey.DependencyProperty;

    private static readonly string[] Labels = ["Too weak", "Weak", "Fair", "Good", "Strong"];
    private static readonly string[] FillKeys = ["Muzzle.Color.Error", "Muzzle.Color.Error", "Muzzle.Color.Peach", "Muzzle.Color.Warning", "Muzzle.Color.Success"];
    private static readonly string[] TextKeys = ["Muzzle.Brush.ErrorText", "Muzzle.Brush.ErrorText", "Muzzle.Brush.WarningText", "Muzzle.Brush.WarningText", "Muzzle.Brush.SuccessText"];

    private static readonly HashSet<string> CommonPasswords = new(StringComparer.OrdinalIgnoreCase) {
        "password", "passw0rd", "123456", "12345678", "123456789", "qwerty", "abc123", "letmein", "welcome", "admin", "iloveyou",
        "monkey", "dragon", "football", "baseball", "sunshine", "princess", "trustno1", "111111", "000000", "password1", "qwerty123"
    };

    private static readonly string[] Sequences = ["abcdefghijklmnopqrstuvwxyz", "01234567890", "qwertyuiop", "asdfghjkl", "zxcvbnm"];

    private readonly SolidColorBrush fillBrush = new(Colors.Transparent);
    private readonly List<FrameworkElement> fills = [];
    private Panel? segments;

    static PasswordStrengthMeter() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PasswordStrengthMeter), new FrameworkPropertyMetadata(typeof(PasswordStrengthMeter)));
        FocusableProperty.OverrideMetadata(typeof(PasswordStrengthMeter), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="PasswordStrengthMeter"/> class.</summary>
    public PasswordStrengthMeter() {
        Loaded += (_, _) => {
            MuzzleThemeManager.ThemeChanged += OnThemeChanged;
            Update(false);
        };
        Unloaded += (_, _) => MuzzleThemeManager.ThemeChanged -= OnThemeChanged;
    }

    /// <summary>Gets or sets the password. The default is <see langword="null"/>. This is a dependency property.</summary>
    public string? Password {
        get => (string?)GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }

    /// <summary>Gets or sets the password box. The default is <see langword="null"/>. This is a dependency property.</summary>
    public PasswordBox? PasswordBox {
        get => (PasswordBox?)GetValue(PasswordBoxProperty);
        set => SetValue(PasswordBoxProperty, value);
    }

    /// <summary>0 (too weak / empty) to 4 (strong).</summary>
    public int StrengthScore {
        get => (int)GetValue(StrengthScoreProperty);
        set => SetValue(StrengthScoreProperty, value);
    }

    /// <summary>Gets or sets whether to show the label. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowLabel {
        get => (bool)GetValue(ShowLabelProperty);
        set => SetValue(ShowLabelProperty, value);
    }

    /// <summary>"Too weak", "Weak", "Fair", "Good" or "Strong" ("Enter a password" while empty).</summary>
    public string StrengthLabel => (string)GetValue(StrengthLabelProperty);

    /// <summary>Gets the label brush. This is a read-only dependency property.</summary>
    public Brush? LabelBrush => (Brush?)GetValue(LabelBrushProperty);

    /// <summary>
    /// A quick, offline strength estimate: entropy from length and character variety, penalised for common
    /// passwords, repeats and keyboard / alphabet runs. 0 for empty or trivially guessable, 4 for strong.
    /// </summary>
    public static int Estimate(string? password) {
        if (string.IsNullOrEmpty(password)) {
            return 0;
        }
        if ((password.Length < 4) || CommonPasswords.Contains(password) || (password.Distinct().Count() <= 2)) {
            return 0;
        }
        int pool = 0;
        if (password.Any(char.IsLower)) {
            pool += 26;
        }
        if (password.Any(char.IsUpper)) {
            pool += 26;
        }
        if (password.Any(char.IsDigit)) {
            pool += 10;
        }
        if (password.Any(static c => !char.IsLetterOrDigit(c))) {
            pool += 33;
        }
        double bits = password.Length * Math.Log2(Math.Max(pool, 10));
        string lower = password.ToLowerInvariant();
        int runs = 0;
        for (int i = 0; i + 3 <= lower.Length; i++) {
            string piece = lower.Substring(i, 3);
            if (Sequences.Any(s => s.Contains(piece, StringComparison.Ordinal)) || ((piece[0] == piece[1]) && (piece[1] == piece[2]))) {
                runs++;
            }
        }
        bits -= runs * 4;
        // "word + a few digits" (mochi2024, kitty7) is among the first patterns guessed.
        int digits = password.Length - password.TrimEnd("0123456789".ToCharArray()).Length;
        if ((digits is > 0 and <= 4) && password[..^digits].All(char.IsLetter)) {
            bits -= 14;
        }
        foreach (string common in CommonPasswords) {
            if ((common.Length >= 5) && lower.Contains(common, StringComparison.Ordinal)) {
                bits -= 14;
            }
        }
        return bits switch {
            < 28 => 1,
            < 42 => 2,
            < 64 => 3,
            _ => 4
        };
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        segments = GetTemplateChild(SegmentsPartName) as Panel;
        BuildSegments();
        Update(false);
    }

    private static object CoerceScore(DependencyObject d, object value) {
        return Math.Clamp((int)value, 0, 4);
    }

    private static void OnPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        PasswordStrengthMeter meter = (PasswordStrengthMeter)d;
        meter.SetCurrentValue(StrengthScoreProperty, Estimate((string?)e.NewValue));
        meter.Update(true);
    }

    private static void OnPasswordBoxChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        PasswordStrengthMeter meter = (PasswordStrengthMeter)d;
        if (e.OldValue is PasswordBox oldBox) {
            oldBox.PasswordChanged -= meter.OnBoxPasswordChanged;
        }
        if (e.NewValue is PasswordBox newBox) {
            newBox.PasswordChanged += meter.OnBoxPasswordChanged;
            meter.SetCurrentValue(PasswordProperty, newBox.Password);
        }
    }

    private void OnBoxPasswordChanged(object sender, RoutedEventArgs e) {
        SetCurrentValue(PasswordProperty, ((PasswordBox)sender).Password);
    }

    private static void OnScoreChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((PasswordStrengthMeter)d).Update(true);
    }

    private void OnThemeChanged(object? sender, MuzzleThemeChangedEventArgs e) {
        Update(false);
    }

    private void BuildSegments() {
        fills.Clear();
        if (segments is null) {
            return;
        }
        segments.Children.Clear();
        for (int i = 0; i < 4; i++) {
            Border fill = new() {
                CornerRadius = new CornerRadius(3),
                Background = fillBrush,
                RenderTransformOrigin = new Point(0, 0.5),
                RenderTransform = new ScaleTransform(0, 1)
            };
            Border track = new() {
                Height = 6,
                Margin = new Thickness((i == 0) ? 0 : 3, 0, (i == 3) ? 0 : 3, 0),
                CornerRadius = new CornerRadius(3),
                Child = fill
            };
            track.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.Track");
            Grid.SetColumn(track, i);
            fills.Add(fill);
            segments.Children.Add(track);
        }
    }

    private void Update(bool animate) {
        int score = StrengthScore;
        bool empty = string.IsNullOrEmpty(Password) && (score == 0);
        SetValue(StrengthLabelPropertyKey, empty ? "Enter a password" : Labels[score]);
        SetValue(LabelBrushPropertyKey, TryFindResource(empty ? "Muzzle.Brush.TextTertiary" : TextKeys[score]) as Brush);
        Color target = (TryFindResource(FillKeys[score]) is Color color) ? color : Colors.Gray;
        bool motion = animate && IsLoaded && Jelly.IsMotionEnabled(this);
        if (motion) {
            fillBrush.BeginAnimation(SolidColorBrush.ColorProperty, new ColorAnimation(target, new Duration(TimeSpan.FromMilliseconds(320))));
        }
        else {
            fillBrush.BeginAnimation(SolidColorBrush.ColorProperty, null);
            fillBrush.Color = target;
        }
        for (int i = 0; i < fills.Count; i++) {
            ScaleTransform scale = (ScaleTransform)fills[i].RenderTransform;
            // Score 0 with text shows a sliver of the first segment so "Too weak" still has colour.
            double fill = (i < score) ? 1 : ((i == 0) && !empty && (score == 0) ? 0.35 : 0);
            if (motion) {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(fill, new Duration(TimeSpan.FromMilliseconds(560))) {
                    BeginTime = TimeSpan.FromMilliseconds(Math.Abs(i - score) * 25),
                    EasingFunction = MuzzleEasings.Get(this, (fill > scale.ScaleX) ? MuzzleEasings.JellySoftKey : MuzzleEasings.SmoothKey)
                });
            }
            else {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.ScaleX = fill;
            }
        }
    }
}