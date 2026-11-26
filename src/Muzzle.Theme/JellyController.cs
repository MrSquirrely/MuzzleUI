using System;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme;

/// <summary>
/// Drives the squish/stretch of a template element ("surface") from the interaction state of its owner
/// (the templated parent, or the element itself). Scales are multiplied by the inherited JellyIntensity.
/// </summary>
internal sealed class JellyController(FrameworkElement surface) {
    private const double HoverGrowth = 0.03;
    private const double PressSquishX = 0.06;
    private const double PressSquishY = 0.08;
    private const double FocusPulse = 0.02;
    private const double TogglePopX = 0.07;
    private const double TogglePopY = 0.06;

    private UIElement? owner;
    private bool isPressed;

    public JellyMode Mode { get; set; }

    private double Intensity => MuzzleAssist.GetJellyIntensity(surface);

    private bool CanAnimate => SystemParameters.ClientAreaAnimation && (Intensity > 0);

    private double RestScale => (Mode.HasFlag(JellyMode.Hover) && (owner?.IsMouseOver ?? false)) ? 1 + (HoverGrowth * Intensity) : 1;

    public static JellyController Create(FrameworkElement surface) {
        JellyController controller = new(surface);
        surface.Loaded += controller.OnSurfaceLoaded;
        surface.Unloaded += controller.OnSurfaceUnloaded;
        if (surface.IsLoaded) {
            controller.Start();
        }
        return controller;
    }

    /// <inheritdoc />
    public void Dispose() {
        surface.Loaded -= OnSurfaceLoaded;
        surface.Unloaded -= OnSurfaceUnloaded;
        Stop();
    }

    private void OnSurfaceLoaded(object sender, RoutedEventArgs e) {
        Start();
    }

    private void OnSurfaceUnloaded(object sender, RoutedEventArgs e) {
        Stop();
    }

    private void Start() {
        if (owner is not null) {
            return;
        }
        owner = surface.TemplatedParent as UIElement ?? surface;
        Jelly.EnsureScale(surface);
        owner.MouseEnter += OnMouseEnter;
        owner.MouseLeave += OnMouseLeave;
        owner.PreviewMouseLeftButtonDown += OnMouseDown;
        owner.PreviewMouseLeftButtonUp += OnMouseUp;
        owner.LostMouseCapture += OnLostMouseCapture;
        owner.PreviewKeyDown += OnKeyDown;
        owner.PreviewKeyUp += OnKeyUp;
        owner.IsKeyboardFocusWithinChanged += OnFocusWithinChanged;
        owner.IsEnabledChanged += OnEnabledChanged;
        if (owner is ToggleButton toggle) {
            toggle.Checked += OnToggled;
            toggle.Unchecked += OnToggled;
            toggle.Indeterminate += OnToggled;
        }
    }

    private void Stop() {
        if (owner is null) {
            return;
        }
        owner.MouseEnter -= OnMouseEnter;
        owner.MouseLeave -= OnMouseLeave;
        owner.PreviewMouseLeftButtonDown -= OnMouseDown;
        owner.PreviewMouseLeftButtonUp -= OnMouseUp;
        owner.LostMouseCapture -= OnLostMouseCapture;
        owner.PreviewKeyDown -= OnKeyDown;
        owner.PreviewKeyUp -= OnKeyUp;
        owner.IsKeyboardFocusWithinChanged -= OnFocusWithinChanged;
        owner.IsEnabledChanged -= OnEnabledChanged;
        if (owner is ToggleButton toggle) {
            toggle.Checked -= OnToggled;
            toggle.Unchecked -= OnToggled;
            toggle.Indeterminate -= OnToggled;
        }
        owner = null;
        isPressed = false;
        Snap(1, 1);
    }

    private void OnMouseEnter(object sender, MouseEventArgs e) {
        if (Mode.HasFlag(JellyMode.Hover)) {
            double hover = 1 + (HoverGrowth * Intensity);
            AnimateTo(hover, hover, 380, MuzzleEasings.BackKey);
        }
    }

    private void OnMouseLeave(object sender, MouseEventArgs e) {
        isPressed = false;
        if (Mode.HasFlag(JellyMode.Hover) || Mode.HasFlag(JellyMode.Press)) {
            AnimateTo(1, 1, 520, MuzzleEasings.JellySoftKey);
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e) {
        if (Mode.HasFlag(JellyMode.Press) && (owner?.IsEnabled ?? false)) {
            Press();
        }
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e) {
        Release();
    }

    private void OnLostMouseCapture(object sender, MouseEventArgs e) {
        Release();
    }

    private void OnKeyDown(object sender, KeyEventArgs e) {
        if (!Mode.HasFlag(JellyMode.Press) || e.IsRepeat || !ReferenceEquals(e.OriginalSource, owner)) {
            return;
        }
        if (e.Key == Key.Space) {
            Press();
        }
        else if (e.Key == Key.Enter) {
            Pop();
        }
    }

    private void OnKeyUp(object sender, KeyEventArgs e) {
        if (e.Key == Key.Space) {
            Release();
        }
    }

    private void OnFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e) {
        if (Mode.HasFlag(JellyMode.Focus) && (e.NewValue is true) && CanAnimate) {
            double peak = RestScale + (FocusPulse * Intensity);
            Pulse(peak, peak);
        }
    }

    private void OnEnabledChanged(object sender, DependencyPropertyChangedEventArgs e) {
        isPressed = false;
        Snap(1, 1);
    }

    private void OnToggled(object sender, RoutedEventArgs e) {
        if (Mode.HasFlag(JellyMode.Toggle) && ReferenceEquals(e.OriginalSource, owner) && surface.IsLoaded) {
            Pop();
        }
    }

    private void Press() {
        isPressed = true;
        double rest = RestScale;
        AnimateTo(rest * (1 - (PressSquishX * Intensity)), rest * (1 - (PressSquishY * Intensity)), 110, MuzzleEasings.SquishKey);
    }

    private void Release() {
        if (!isPressed) {
            return;
        }
        isPressed = false;
        double rest = RestScale;
        AnimateTo(rest, rest, 720, MuzzleEasings.JellyKey);
    }

    private void Pop() {
        if (!CanAnimate) {
            return;
        }
        double rest = RestScale;
        Pulse(rest * (1 + (TogglePopX * Intensity)), rest * (1 - (TogglePopY * Intensity)));
    }

    private void Pulse(double peakX, double peakY) {
        ScaleTransform scale = Jelly.EnsureScale(surface);
        double rest = RestScale;
        IEasingFunction squish = MuzzleEasings.Get(surface, MuzzleEasings.SquishKey);
        IEasingFunction jelly = MuzzleEasings.Get(surface, MuzzleEasings.JellyKey);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, Jelly.CreatePulse(peakX, rest, 120, 780, squish, jelly));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, Jelly.CreatePulse(peakY, rest, 120, 780, squish, jelly));
    }

    private void AnimateTo(double x, double y, double milliseconds, string easingKey) {
        if (!CanAnimate) {
            Snap(1, 1);
            return;
        }
        ScaleTransform scale = Jelly.EnsureScale(surface);
        IEasingFunction easing = MuzzleEasings.Get(surface, easingKey);
        Duration duration = TimeSpan.FromMilliseconds(milliseconds);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(x, duration) { EasingFunction = easing });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(y, duration) { EasingFunction = easing });
    }

    private void Snap(double x, double y) {
        if (surface.RenderTransform is not ScaleTransform { IsFrozen: false } scale) {
            return;
        }
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        scale.ScaleX = x;
        scale.ScaleY = y;
    }
}