using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A round icon button that cycles the app theme Light → Dark → System through <see cref="MuzzleThemeManager"/>.
/// The sun, moon and monitor icons spin and spring into place, and the button itself gives a jelly bounce.
/// </summary>
public class ThemeToggleButton : Button {
    /// <summary>Identifies the <see cref="Theme"/> dependency property.</summary>
    public static readonly DependencyProperty ThemeProperty = DependencyProperty.Register(
        nameof(Theme), typeof(MuzzleTheme), typeof(ThemeToggleButton),
        new FrameworkPropertyMetadata(MuzzleTheme.Light, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnThemePropertyChanged));

    /// <summary>Identifies the <see cref="AnimateTransition"/> dependency property.</summary>
    public static readonly DependencyProperty AnimateTransitionProperty = DependencyProperty.Register(
        nameof(AnimateTransition), typeof(bool), typeof(ThemeToggleButton), new PropertyMetadata(true));

    private static readonly Geometry SunGeometry = Freeze(Geometry.Parse(
        "M 10,6.4 A 3.6,3.6 0 1 1 9.99,6.4 Z M 10,1.8 V 3.4 M 10,16.6 V 18.2 M 1.8,10 H 3.4 M 16.6,10 H 18.2 "
        + "M 4.2,4.2 L 5.35,5.35 M 14.65,14.65 L 15.8,15.8 M 4.2,15.8 L 5.35,14.65 M 14.65,5.35 L 15.8,4.2"));

    private static readonly Geometry MoonGeometry = Freeze(Geometry.Parse("M 16.5,12.3 A 7,7 0 1 1 7.7,3.5 A 5.6,5.6 0 0 0 16.5,12.3 Z"));

    private static readonly Geometry SystemGeometry = Freeze(Geometry.Parse(
        "M 3.5,4 H 16.5 A 1.8,1.8 0 0 1 18.3,5.8 V 12.2 A 1.8,1.8 0 0 1 16.5,14 H 3.5 A 1.8,1.8 0 0 1 1.7,12.2 V 5.8 A 1.8,1.8 0 0 1 3.5,4 Z "
        + "M 7,17.4 H 13 M 10,14 V 17.4 M 10,6.6 A 2.4,2.4 0 0 0 10,11.4 Z"));

    private readonly Path sun;
    private readonly Path moon;
    private readonly Path system;
    private bool syncing;

    static ThemeToggleButton() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemeToggleButton), new FrameworkPropertyMetadata(typeof(ThemeToggleButton)));
    }

    /// <summary>Initializes a new instance of the <see cref="ThemeToggleButton"/> class.</summary>
    public ThemeToggleButton() {
        sun = CreateIcon(SunGeometry, "Muzzle.Brush.WarningText", null);
        moon = CreateIcon(MoonGeometry, "Muzzle.Brush.SecondaryText", "Muzzle.Brush.SecondarySoft");
        system = CreateIcon(SystemGeometry, "Muzzle.Brush.TextSecondary", null);
        Grid icons = new() { Width = 20, Height = 20, IsHitTestVisible = false };
        icons.Children.Add(sun);
        icons.Children.Add(moon);
        icons.Children.Add(system);
        Content = icons;
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        ShowIcon(MuzzleTheme.Light, false);
    }

    /// <summary>The theme this button shows and applies. Setting it applies the theme.</summary>
    public MuzzleTheme Theme {
        get => (MuzzleTheme)GetValue(ThemeProperty);
        set => SetValue(ThemeProperty, value);
    }

    /// <summary>Crossfade the windows when switching (default true).</summary>
    public bool AnimateTransition {
        get => (bool)GetValue(AnimateTransitionProperty);
        set => SetValue(AnimateTransitionProperty, value);
    }

    /// <summary>The theme that follows <paramref name="theme"/> in the Light → Dark → System cycle.</summary>
    public static MuzzleTheme Next(MuzzleTheme theme) {
        return theme switch {
            MuzzleTheme.Light => MuzzleTheme.Dark,
            MuzzleTheme.Dark => MuzzleTheme.System,
            _ => MuzzleTheme.Light
        };
    }

    /// <inheritdoc />
    protected override void OnClick() {
        Jelly.Wobble(this);
        Theme = Next(Theme);
        base.OnClick();
    }

    private static void OnThemePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ThemeToggleButton button = (ThemeToggleButton)d;
        MuzzleTheme theme = (MuzzleTheme)e.NewValue;
        button.ShowIcon(theme, button.IsLoaded);
        if (!button.syncing && (theme != MuzzleThemeManager.CurrentTheme) && (Application.Current is not null)) {
            MuzzleThemeManager.ApplyTheme(theme, button.AnimateTransition);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        MuzzleThemeManager.ThemeChanged += OnManagerThemeChanged;
        Sync(MuzzleThemeManager.CurrentTheme);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        MuzzleThemeManager.ThemeChanged -= OnManagerThemeChanged;
    }

    private void OnManagerThemeChanged(object? sender, MuzzleThemeChangedEventArgs e) {
        Sync(e.Theme);
        UpdateDescription();
    }

    private void Sync(MuzzleTheme theme) {
        syncing = true;
        try {
            SetCurrentValue(ThemeProperty, theme);
        }
        finally {
            syncing = false;
        }
        UpdateDescription();
    }

    private void UpdateDescription() {
        string label = (Theme == MuzzleTheme.System) ? $"System ({MuzzleThemeManager.EffectiveTheme})" : Theme.ToString();
        string text = $"Theme: {label}. Click for {Next(Theme)}.";
        ToolTip = text;
        AutomationProperties.SetName(this, text);
    }

    private void ShowIcon(MuzzleTheme theme, bool animate) {
        Path active = theme switch {
            MuzzleTheme.Dark => moon,
            MuzzleTheme.System => system,
            _ => sun
        };
        bool motion = animate && Jelly.IsMotionEnabled(this);
        foreach (Path icon in (Path[])[sun, moon, system]) {
            bool show = ReferenceEquals(icon, active);
            TransformGroup group = (TransformGroup)icon.RenderTransform;
            ScaleTransform scale = (ScaleTransform)group.Children[0];
            RotateTransform rotate = (RotateTransform)group.Children[1];
            double opacity = show ? 1 : 0;
            double size = show ? 1 : 0.4;
            double angle = show ? 0 : 90;
            if (!motion) {
                icon.BeginAnimation(OpacityProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                rotate.BeginAnimation(RotateTransform.AngleProperty, null);
                icon.Opacity = opacity;
                scale.ScaleX = size;
                scale.ScaleY = size;
                rotate.Angle = angle;
                continue;
            }
            if (show) {
                IEasingFunction spring = MuzzleEasings.Get(this, MuzzleEasings.JellyKey);
                IEasingFunction back = MuzzleEasings.Get(this, MuzzleEasings.BackKey);
                Duration duration = new(TimeSpan.FromMilliseconds(720));
                TimeSpan delay = TimeSpan.FromMilliseconds(90);
                icon.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(200))) { BeginTime = delay });
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.4, 1, duration) { BeginTime = delay, EasingFunction = spring });
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.4, 1, duration) { BeginTime = delay, EasingFunction = spring });
                rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(-90, 0, new Duration(TimeSpan.FromMilliseconds(560))) { BeginTime = delay, EasingFunction = back });
            }
            else {
                Duration duration = new(TimeSpan.FromMilliseconds(180));
                QuadraticEase ease = new() { EasingMode = EasingMode.EaseIn };
                icon.BeginAnimation(OpacityProperty, new DoubleAnimation(0, duration) { EasingFunction = ease });
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.4, duration) { EasingFunction = ease });
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.4, duration) { EasingFunction = ease });
                rotate.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(90, duration) { EasingFunction = ease });
            }
        }
    }

    private static Path CreateIcon(Geometry data, string strokeKey, string? fillKey) {
        Path icon = new() {
            Data = data,
            Width = 20,
            Height = 20,
            Stretch = Stretch.None,
            StrokeThickness = 1.7,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round,
            StrokeLineJoin = PenLineJoin.Round,
            RenderTransformOrigin = new Point(0.5, 0.5),
            RenderTransform = new TransformGroup { Children = [new ScaleTransform(1, 1), new RotateTransform(0)] }
        };
        icon.SetResourceReference(Shape.StrokeProperty, strokeKey);
        if (fillKey is not null) {
            icon.SetResourceReference(Shape.FillProperty, fillKey);
        }
        return icon;
    }

    private static Geometry Freeze(Geometry geometry) {
        geometry.Freeze();
        return geometry;
    }
}