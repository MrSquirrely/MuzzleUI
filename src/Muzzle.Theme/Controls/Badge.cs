using System;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>Pastel colour of a <see cref="Badge"/>.</summary>
public enum BadgeKind {
    /// <summary>The accent colour (the default).</summary>
    Primary,
    /// <summary>The secondary supporting colour.</summary>
    Secondary,
    /// <summary>Informational blue.</summary>
    Info,
    /// <summary>Success mint.</summary>
    Success,
    /// <summary>Warning butter-yellow.</summary>
    Warning,
    /// <summary>Error coral-red.</summary>
    Error
}

/// <summary>
/// Decorates its content with a count pill (<see cref="Value"/>, capped at <see cref="MaxValue"/> as "99+"),
/// a custom <see cref="Text"/>, or a status dot (<see cref="IsDot"/>). The pill pops in with an elastic bounce
/// (0.5× → 1.15× → 1×) whenever it appears or its value changes. For any element, see <c>MuzzleAssist.Badge</c>.
/// </summary>
[TemplatePart(Name = PillPartName, Type = typeof(FrameworkElement))]
public class Badge : ContentControl {
    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(int), typeof(Badge), new PropertyMetadata(0, OnDisplayChanged));

    /// <summary>Identifies the <see cref="MaxValue"/> dependency property.</summary>
    public static readonly DependencyProperty MaxValueProperty = DependencyProperty.Register(
        nameof(MaxValue), typeof(int), typeof(Badge), new PropertyMetadata(99, OnDisplayChanged));

    /// <summary>Shown instead of the count when set (e.g. "New").</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(Badge), new PropertyMetadata(null, OnDisplayChanged));

    /// <summary>Identifies the <see cref="IsDot"/> dependency property.</summary>
    public static readonly DependencyProperty IsDotProperty = DependencyProperty.Register(
        nameof(IsDot), typeof(bool), typeof(Badge), new PropertyMetadata(false, OnDisplayChanged));

    /// <summary>Show "0" instead of hiding the pill when <see cref="Value"/> is 0.</summary>
    public static readonly DependencyProperty ShowZeroProperty = DependencyProperty.Register(
        nameof(ShowZero), typeof(bool), typeof(Badge), new PropertyMetadata(false, OnDisplayChanged));

    /// <summary>Identifies the <see cref="Kind"/> dependency property.</summary>
    public static readonly DependencyProperty KindProperty = DependencyProperty.Register(
        nameof(Kind), typeof(BadgeKind), typeof(Badge), new PropertyMetadata(BadgeKind.Primary));

    /// <summary>Where the pill sits relative to the content's top-right corner.</summary>
    public static readonly DependencyProperty BadgeMarginProperty = DependencyProperty.Register(
        nameof(BadgeMargin), typeof(Thickness), typeof(Badge), new PropertyMetadata(new Thickness(0, -8, -8, 0)));

    /// <summary>Text colour of the pill label. Kept separate from Foreground so the wrapped content isn't recoloured.</summary>
    public static readonly DependencyProperty BadgeForegroundProperty = DependencyProperty.Register(
        nameof(BadgeForeground), typeof(Brush), typeof(Badge), new PropertyMetadata(null));

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(Badge), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="DisplayText"/> read-only dependency property.</summary>
    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsBadgeVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsBadgeVisible), typeof(bool), typeof(Badge), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsBadgeVisible"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsBadgeVisibleProperty = IsBadgeVisiblePropertyKey.DependencyProperty;

    private const string PillPartName = "PART_Pill";

    private FrameworkElement? pill;

    static Badge() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(typeof(Badge)));
        FocusableProperty.OverrideMetadata(typeof(Badge), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="Badge"/> class.</summary>
    public Badge() {
        UpdateDisplay(false);
    }

    /// <summary>Gets or sets the value. The default is <c>0</c>. This is a dependency property.</summary>
    public int Value {
        get => (int)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Gets or sets the max value. The default is <c>99</c>. This is a dependency property.</summary>
    public int MaxValue {
        get => (int)GetValue(MaxValueProperty);
        set => SetValue(MaxValueProperty, value);
    }

    /// <summary>Gets or sets the text. The default is <see langword="null"/>. This is a dependency property.</summary>
    public string? Text {
        get => (string?)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>Gets or sets whether the badge is a small dot instead of a pill. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsDot {
        get => (bool)GetValue(IsDotProperty);
        set => SetValue(IsDotProperty, value);
    }

    /// <summary>Gets or sets whether to show the zero. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool ShowZero {
        get => (bool)GetValue(ShowZeroProperty);
        set => SetValue(ShowZeroProperty, value);
    }

    /// <summary>Gets or sets the kind. The default is <c>BadgeKind.Primary</c>. This is a dependency property.</summary>
    public BadgeKind Kind {
        get => (BadgeKind)GetValue(KindProperty);
        set => SetValue(KindProperty, value);
    }

    /// <summary>Gets or sets the badge margin. The default is <c>Thickness(0, -8, -8, 0)</c>. This is a dependency property.</summary>
    public Thickness BadgeMargin {
        get => (Thickness)GetValue(BadgeMarginProperty);
        set => SetValue(BadgeMarginProperty, value);
    }

    /// <summary>Gets or sets the badge foreground. The default is <see langword="null"/>. This is a dependency property.</summary>
    public Brush? BadgeForeground {
        get => (Brush?)GetValue(BadgeForegroundProperty);
        set => SetValue(BadgeForegroundProperty, value);
    }

    /// <summary>Gets the display text. This is a read-only dependency property.</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    /// <summary>Gets whether the badge is visible. This is a read-only dependency property.</summary>
    public bool IsBadgeVisible => (bool)GetValue(IsBadgeVisibleProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        pill = GetTemplateChild(PillPartName) as FrameworkElement;
    }

    /// <summary>Plays the pop animation (also runs automatically when the badge appears or changes).</summary>
    public void Pop() {
        if ((pill is null) || !IsBadgeVisible || !IsLoaded || !Jelly.IsMotionEnabled(this)) {
            return;
        }
        ScaleTransform scale = Jelly.EnsureScale(pill);
        DoubleAnimationUsingKeyFrames pop = new();
        pop.KeyFrames.Add(new EasingDoubleKeyFrame(0.5, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        pop.KeyFrames.Add(new EasingDoubleKeyFrame(1.15, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(170)), MuzzleEasings.Get(this, MuzzleEasings.SquishKey)));
        pop.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(760)), MuzzleEasings.Get(this, MuzzleEasings.JellyKey)));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    private static void OnDisplayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Badge)d).UpdateDisplay(true);
    }

    private void UpdateDisplay(bool animate) {
        string text = IsDot
            ? string.Empty
            : !string.IsNullOrEmpty(Text)
                ? Text
                : Value > MaxValue ? MaxValue.ToString(CultureInfo.CurrentCulture) + "+" : Value.ToString(CultureInfo.CurrentCulture);
        bool visible = IsDot || !string.IsNullOrEmpty(Text) || (Value != 0) || ShowZero;
        bool changed = (text != DisplayText) || (visible != IsBadgeVisible);
        SetValue(DisplayTextPropertyKey, text);
        SetValue(IsBadgeVisiblePropertyKey, visible);
        AutomationProperties.SetItemStatus(this, visible ? (IsDot ? "status" : text) : string.Empty);
        if (animate && changed && visible) {
            Dispatcher.BeginInvoke(new Action(Pop));
        }
    }
}