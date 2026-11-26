using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme;

/// <summary>Entrance played by <see cref="JellyBehavior.EntranceAnimationProperty"/> when an element loads.</summary>
public enum JellyEntrance {
    /// <summary>No entrance animation (the default).</summary>
    None,

    /// <summary>Springs up from 60 % with an elastic overshoot while fading in.</summary>
    PopIn,

    /// <summary>Rises 16 px with a back-ease while fading in.</summary>
    SlideUp,

    /// <summary>A plain fade.</summary>
    FadeIn
}

/// <summary>
/// Jelly physics for any FrameworkElement, without retemplating: a springy hover bounce, a press squish and an
/// entrance animation on Loaded. Honours <see cref="MuzzleAssist.JellyIntensityProperty"/> and the Windows animation
/// setting (no motion when either is off).
/// </summary>
/// <example><code>&lt;Border mz:JellyBehavior.EnableHoverBounce="True" mz:JellyBehavior.EnablePressSquish="True" mz:JellyBehavior.EntranceAnimation="PopIn" /&gt;</code></example>
public static class JellyBehavior {
    /// <summary>Identifies the <c>EnableHoverBounce</c> attached property.</summary>
    public static readonly DependencyProperty EnableHoverBounceProperty = DependencyProperty.RegisterAttached(
        "EnableHoverBounce", typeof(bool), typeof(JellyBehavior), new PropertyMetadata(false, OnInteractionChanged));

    /// <summary>Identifies the <c>HoverScale</c> attached property.</summary>
    public static readonly DependencyProperty HoverScaleProperty = DependencyProperty.RegisterAttached(
        "HoverScale", typeof(double), typeof(JellyBehavior), new PropertyMetadata(1.03));

    /// <summary>Identifies the <c>EnablePressSquish</c> attached property.</summary>
    public static readonly DependencyProperty EnablePressSquishProperty = DependencyProperty.RegisterAttached(
        "EnablePressSquish", typeof(bool), typeof(JellyBehavior), new PropertyMetadata(false, OnInteractionChanged));

    /// <summary>Identifies the <c>PressSquishScale</c> attached property.</summary>
    public static readonly DependencyProperty PressSquishScaleProperty = DependencyProperty.RegisterAttached(
        "PressSquishScale", typeof(double), typeof(JellyBehavior), new PropertyMetadata(0.94));

    /// <summary>Identifies the <c>EntranceAnimation</c> attached property.</summary>
    public static readonly DependencyProperty EntranceAnimationProperty = DependencyProperty.RegisterAttached(
        "EntranceAnimation", typeof(JellyEntrance), typeof(JellyBehavior), new PropertyMetadata(JellyEntrance.None, OnEntranceChanged));

    /// <summary>Delay before the entrance plays (handy for staggering a list).</summary>
    public static readonly DependencyProperty EntranceDelayProperty = DependencyProperty.RegisterAttached(
        "EntranceDelay", typeof(TimeSpan), typeof(JellyBehavior), new PropertyMetadata(TimeSpan.Zero));

    private static readonly DependencyProperty IsPressedProperty = DependencyProperty.RegisterAttached(
        "IsPressed", typeof(bool), typeof(JellyBehavior), new PropertyMetadata(false));

    /// <summary>Gets the value of the <c>EnableHoverBounce</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>EnableHoverBounce</c> value.</returns>
    public static bool GetEnableHoverBounce(DependencyObject element) {
        return (bool)element.GetValue(EnableHoverBounceProperty);
    }

    /// <summary>Sets the value of the <c>EnableHoverBounce</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetEnableHoverBounce(DependencyObject element, bool value) {
        element.SetValue(EnableHoverBounceProperty, value);
    }

    /// <summary>Gets the value of the <c>HoverScale</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>HoverScale</c> value.</returns>
    public static double GetHoverScale(DependencyObject element) {
        return (double)element.GetValue(HoverScaleProperty);
    }

    /// <summary>Sets the value of the <c>HoverScale</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetHoverScale(DependencyObject element, double value) {
        element.SetValue(HoverScaleProperty, value);
    }

    /// <summary>Gets the value of the <c>EnablePressSquish</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>EnablePressSquish</c> value.</returns>
    public static bool GetEnablePressSquish(DependencyObject element) {
        return (bool)element.GetValue(EnablePressSquishProperty);
    }

    /// <summary>Sets the value of the <c>EnablePressSquish</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetEnablePressSquish(DependencyObject element, bool value) {
        element.SetValue(EnablePressSquishProperty, value);
    }

    /// <summary>Gets the value of the <c>PressSquishScale</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>PressSquishScale</c> value.</returns>
    public static double GetPressSquishScale(DependencyObject element) {
        return (double)element.GetValue(PressSquishScaleProperty);
    }

    /// <summary>Sets the value of the <c>PressSquishScale</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetPressSquishScale(DependencyObject element, double value) {
        element.SetValue(PressSquishScaleProperty, value);
    }

    /// <summary>Gets the value of the <c>EntranceAnimation</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>EntranceAnimation</c> value.</returns>
    public static JellyEntrance GetEntranceAnimation(DependencyObject element) {
        return (JellyEntrance)element.GetValue(EntranceAnimationProperty);
    }

    /// <summary>Sets the value of the <c>EntranceAnimation</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetEntranceAnimation(DependencyObject element, JellyEntrance value) {
        element.SetValue(EntranceAnimationProperty, value);
    }

    /// <summary>Gets the value of the <c>EntranceDelay</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>EntranceDelay</c> value.</returns>
    public static TimeSpan GetEntranceDelay(DependencyObject element) {
        return (TimeSpan)element.GetValue(EntranceDelayProperty);
    }

    /// <summary>Sets the value of the <c>EntranceDelay</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetEntranceDelay(DependencyObject element, TimeSpan value) {
        element.SetValue(EntranceDelayProperty, value);
    }

    /// <summary>Plays the element's entrance again (e.g. when content is swapped without reloading).</summary>
    public static void PlayEntrance(FrameworkElement element) {
        JellyEntrance entrance = GetEntranceAnimation(element);
        if ((entrance == JellyEntrance.None) || !Jelly.IsMotionEnabled(element)) {
            element.BeginAnimation(UIElement.OpacityProperty, null);
            return;
        }
        TimeSpan delay = GetEntranceDelay(element);
        Duration fadeDuration = new(TimeSpan.FromMilliseconds((entrance == JellyEntrance.FadeIn) ? 320 : 200));
        element.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, fadeDuration) { BeginTime = delay, FillBehavior = FillBehavior.Stop });
        switch (entrance) {
            case JellyEntrance.PopIn: {
                ScaleTransform scale = Jelly.EnsureScale(element);
                IEasingFunction spring = MuzzleEasings.Get(element, MuzzleEasings.JellyKey);
                Duration duration = new(TimeSpan.FromMilliseconds(700));
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.6, 1, duration) { BeginTime = delay, EasingFunction = spring, FillBehavior = FillBehavior.Stop });
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.6, 1, duration) { BeginTime = delay, EasingFunction = spring, FillBehavior = FillBehavior.Stop });
                break;
            }
            case JellyEntrance.SlideUp: {
                TranslateTransform shift = EnsureTranslate(element);
                shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(16, 0, new Duration(TimeSpan.FromMilliseconds(520))) {
                    BeginTime = delay,
                    EasingFunction = MuzzleEasings.Get(element, MuzzleEasings.BackKey),
                    FillBehavior = FillBehavior.Stop
                });
                break;
            }
        }
    }

    private static void OnInteractionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement element) {
            return;
        }
        element.MouseEnter -= OnMouseEnter;
        element.MouseLeave -= OnMouseLeave;
        element.PreviewMouseLeftButtonDown -= OnPreviewMouseDown;
        element.PreviewMouseLeftButtonUp -= OnPreviewMouseUp;
        element.LostMouseCapture -= OnLostMouseCapture;
        if (GetEnableHoverBounce(element) || GetEnablePressSquish(element)) {
            // Install (or adopt) the scale transform up front; existing transforms are kept in a TransformGroup.
            _ = Jelly.EnsureScale(element);
            element.MouseEnter += OnMouseEnter;
            element.MouseLeave += OnMouseLeave;
            element.PreviewMouseLeftButtonDown += OnPreviewMouseDown;
            element.PreviewMouseLeftButtonUp += OnPreviewMouseUp;
            element.LostMouseCapture += OnLostMouseCapture;
        }
    }

    private static void OnEntranceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement element) {
            return;
        }
        element.Loaded -= OnLoadedForEntrance;
        if ((JellyEntrance)e.NewValue != JellyEntrance.None) {
            element.Loaded += OnLoadedForEntrance;
        }
    }

    private static void OnLoadedForEntrance(object sender, RoutedEventArgs e) {
        PlayEntrance((FrameworkElement)sender);
    }

    private static void OnMouseEnter(object sender, MouseEventArgs e) {
        FrameworkElement element = (FrameworkElement)sender;
        if (GetEnableHoverBounce(element) && !(bool)element.GetValue(IsPressedProperty)) {
            AnimateTo(element, Scaled(element, GetHoverScale(element)), Scaled(element, GetHoverScale(element)), MuzzleEasings.BackKey, 380);
        }
    }

    private static void OnMouseLeave(object sender, MouseEventArgs e) {
        FrameworkElement element = (FrameworkElement)sender;
        if (!(bool)element.GetValue(IsPressedProperty)) {
            AnimateTo(element, 1, 1, MuzzleEasings.JellySoftKey, 520);
        }
    }

    private static void OnPreviewMouseDown(object sender, MouseButtonEventArgs e) {
        FrameworkElement element = (FrameworkElement)sender;
        if (!GetEnablePressSquish(element)) {
            return;
        }
        element.SetValue(IsPressedProperty, true);
        double squish = Scaled(element, GetPressSquishScale(element));
        AnimateTo(element, squish, squish - (0.02 * MuzzleAssist.GetJellyIntensity(element)), MuzzleEasings.SquishKey, 120);
    }

    private static void OnPreviewMouseUp(object sender, MouseButtonEventArgs e) {
        Release((FrameworkElement)sender);
    }

    private static void OnLostMouseCapture(object sender, MouseEventArgs e) {
        if (Mouse.LeftButton == MouseButtonState.Released) {
            Release((FrameworkElement)sender);
        }
    }

    private static void Release(FrameworkElement element) {
        if (!(bool)element.GetValue(IsPressedProperty)) {
            return;
        }
        element.SetValue(IsPressedProperty, false);
        double rest = (GetEnableHoverBounce(element) && element.IsMouseOver) ? Scaled(element, GetHoverScale(element)) : 1;
        AnimateTo(element, rest, rest, MuzzleEasings.JellyKey, 650);
    }

    /// <summary>Scales the distance from 1 by the element's jelly intensity.</summary>
    private static double Scaled(FrameworkElement element, double target) {
        return 1 + ((target - 1) * MuzzleAssist.GetJellyIntensity(element));
    }

    private static void AnimateTo(FrameworkElement element, double x, double y, string easingKey, double milliseconds) {
        ScaleTransform scale = Jelly.EnsureScale(element);
        if (!Jelly.IsMotionEnabled(element)) {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            scale.ScaleX = 1;
            scale.ScaleY = 1;
            return;
        }
        IEasingFunction easing = MuzzleEasings.Get(element, easingKey);
        Duration duration = new(TimeSpan.FromMilliseconds(milliseconds));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(x, duration) { EasingFunction = easing });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(y, duration) { EasingFunction = easing });
    }

    private static TranslateTransform EnsureTranslate(FrameworkElement element) {
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        switch (element.RenderTransform) {
            case TranslateTransform { IsFrozen: false } translate:
                return translate;
            case TransformGroup { IsFrozen: false } group: {
                foreach (Transform child in group.Children) {
                    if (child is TranslateTransform { IsFrozen: false } existing) {
                        return existing;
                    }
                }
                TranslateTransform added = new();
                group.Children.Add(added);
                return added;
            }
            case ScaleTransform { IsFrozen: false } scale: {
                TranslateTransform added = new();
                element.RenderTransform = new TransformGroup { Children = [scale, added] };
                return added;
            }
            default: {
                TranslateTransform added = new();
                element.RenderTransform = new TransformGroup { Children = [new ScaleTransform(1, 1), added] };
                return added;
            }
        }
    }
}