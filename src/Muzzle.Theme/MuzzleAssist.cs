using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Media.Animation;

using Muzzle.Theme.Controls;

namespace Muzzle.Theme;

/// <summary>
/// Attached properties that let you customise Muzzle controls from XAML without rewriting their templates.
/// </summary>
public static class MuzzleAssist {
    /// <summary>Identifies the <c>CornerRadius</c> attached property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.RegisterAttached(
        "CornerRadius",
        typeof(CornerRadius),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(new CornerRadius(10), FrameworkPropertyMetadataOptions.AffectsRender, OnCornerRadiusChanged));

    /// <summary>Identifies the <c>ShadowDepth</c> attached property.</summary>
    public static readonly DependencyProperty ShadowDepthProperty = DependencyProperty.RegisterAttached(
        "ShadowDepth",
        typeof(ShadowDepth),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(ShadowDepth.None, FrameworkPropertyMetadataOptions.AffectsRender, OnShadowDepthChanged));

    /// <summary>Multiplier for every jelly animation (0 = off, 1 = default, 2 = extra wobbly). Inherits down the tree.</summary>
    public static readonly DependencyProperty JellyIntensityProperty = DependencyProperty.RegisterAttached(
        "JellyIntensity",
        typeof(double),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.Inherits),
        IsValidIntensity);

    /// <summary>Turns an element into a jelly surface that squishes on the chosen interactions of its templated parent (or itself).</summary>
    public static readonly DependencyProperty JellyProperty = DependencyProperty.RegisterAttached(
        "Jelly",
        typeof(JellyMode),
        typeof(MuzzleAssist),
        new PropertyMetadata(JellyMode.None, OnJellyChanged));

    /// <summary>Identifies the <c>Placeholder</c> attached property.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.RegisterAttached(
        "Placeholder",
        typeof(string),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(null));

    /// <summary>Identifies the <c>HoverBackground</c> attached property.</summary>
    public static readonly DependencyProperty HoverBackgroundProperty = DependencyProperty.RegisterAttached(
        "HoverBackground",
        typeof(Brush),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>Identifies the <c>PressedBackground</c> attached property.</summary>
    public static readonly DependencyProperty PressedBackgroundProperty = DependencyProperty.RegisterAttached(
        "PressedBackground",
        typeof(Brush),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>True when a PasswordBox with <see cref="TrackPasswordProperty"/> contains text (drives the placeholder).</summary>
    public static readonly DependencyProperty HasTextProperty = DependencyProperty.RegisterAttached(
        "HasText",
        typeof(bool),
        typeof(MuzzleAssist),
        new PropertyMetadata(false));

    /// <summary>Identifies the <c>TrackPassword</c> attached property.</summary>
    public static readonly DependencyProperty TrackPasswordProperty = DependencyProperty.RegisterAttached(
        "TrackPassword",
        typeof(bool),
        typeof(MuzzleAssist),
        new PropertyMetadata(false, OnTrackPasswordChanged));

    /// <summary>When true on a RangeBase, <see cref="ProgressRatioProperty"/> eases smoothly towards the current value.</summary>
    public static readonly DependencyProperty AnimateProgressProperty = DependencyProperty.RegisterAttached(
        "AnimateProgress",
        typeof(bool),
        typeof(MuzzleAssist),
        new PropertyMetadata(false, OnAnimateProgressChanged));

    /// <summary>Identifies the <c>ProgressRatio</c> attached property.</summary>
    public static readonly DependencyProperty ProgressRatioProperty = DependencyProperty.RegisterAttached(
        "ProgressRatio",
        typeof(double),
        typeof(MuzzleAssist),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>Marks the element inside a TabControl template that slides under the selected tab.</summary>
    public static readonly DependencyProperty TabIndicatorProperty = DependencyProperty.RegisterAttached(
        "TabIndicator",
        typeof(bool),
        typeof(MuzzleAssist),
        new PropertyMetadata(false, OnTabIndicatorChanged));

    /// <summary>
    /// Shows a badge on any element: an int (count), a string (text, or a count if numeric), or true (status dot).
    /// Null removes it. The badge pops in whenever the value changes.
    /// </summary>
    public static readonly DependencyProperty BadgeProperty = DependencyProperty.RegisterAttached(
        "Badge",
        typeof(object),
        typeof(MuzzleAssist),
        new PropertyMetadata(null, OnBadgeChanged));

    /// <summary>Identifies the <c>BadgeKind</c> attached property.</summary>
    public static readonly DependencyProperty BadgeKindProperty = DependencyProperty.RegisterAttached(
        "BadgeKind",
        typeof(BadgeKind),
        typeof(MuzzleAssist),
        new PropertyMetadata(BadgeKind.Primary, OnBadgeChanged));

    private static readonly DependencyProperty BadgeAdornerProperty = DependencyProperty.RegisterAttached(
        "BadgeAdorner",
        typeof(BadgeAdorner),
        typeof(MuzzleAssist),
        new PropertyMetadata(null));

    private static readonly DependencyProperty JellyControllerProperty = DependencyProperty.RegisterAttached(
        "JellyController",
        typeof(JellyController),
        typeof(MuzzleAssist),
        new PropertyMetadata(null));

    private static readonly DependencyProperty TabIndicatorControllerProperty = DependencyProperty.RegisterAttached(
        "TabIndicatorController",
        typeof(TabIndicatorController),
        typeof(MuzzleAssist),
        new PropertyMetadata(null));

    /// <summary>Gets the value of the <c>CornerRadius</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>CornerRadius</c> value.</returns>
    public static CornerRadius GetCornerRadius(DependencyObject element) {
        return (CornerRadius)element.GetValue(CornerRadiusProperty);
    }

    /// <summary>Sets the value of the <c>CornerRadius</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetCornerRadius(DependencyObject element, CornerRadius value) {
        element.SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Gets the value of the <c>ShadowDepth</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>ShadowDepth</c> value.</returns>
    public static ShadowDepth GetShadowDepth(DependencyObject element) {
        return (ShadowDepth)element.GetValue(ShadowDepthProperty);
    }

    /// <summary>Sets the value of the <c>ShadowDepth</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetShadowDepth(DependencyObject element, ShadowDepth value) {
        element.SetValue(ShadowDepthProperty, value);
    }

    /// <summary>Gets the value of the <c>JellyIntensity</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>JellyIntensity</c> value.</returns>
    public static double GetJellyIntensity(DependencyObject element) {
        return (double)element.GetValue(JellyIntensityProperty);
    }

    /// <summary>Sets the value of the <c>JellyIntensity</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetJellyIntensity(DependencyObject element, double value) {
        element.SetValue(JellyIntensityProperty, value);
    }

    /// <summary>Gets the value of the <c>Jelly</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Jelly</c> value.</returns>
    public static JellyMode GetJelly(DependencyObject element) {
        return (JellyMode)element.GetValue(JellyProperty);
    }

    /// <summary>Sets the value of the <c>Jelly</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetJelly(DependencyObject element, JellyMode value) {
        element.SetValue(JellyProperty, value);
    }

    /// <summary>Gets the value of the <c>Placeholder</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Placeholder</c> value.</returns>
    public static string? GetPlaceholder(DependencyObject element) {
        return (string?)element.GetValue(PlaceholderProperty);
    }

    /// <summary>Sets the value of the <c>Placeholder</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetPlaceholder(DependencyObject element, string? value) {
        element.SetValue(PlaceholderProperty, value);
    }

    /// <summary>Gets the value of the <c>HoverBackground</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>HoverBackground</c> value.</returns>
    public static Brush? GetHoverBackground(DependencyObject element) {
        return (Brush?)element.GetValue(HoverBackgroundProperty);
    }

    /// <summary>Sets the value of the <c>HoverBackground</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetHoverBackground(DependencyObject element, Brush? value) {
        element.SetValue(HoverBackgroundProperty, value);
    }

    /// <summary>Gets the value of the <c>PressedBackground</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>PressedBackground</c> value.</returns>
    public static Brush? GetPressedBackground(DependencyObject element) {
        return (Brush?)element.GetValue(PressedBackgroundProperty);
    }

    /// <summary>Sets the value of the <c>PressedBackground</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetPressedBackground(DependencyObject element, Brush? value) {
        element.SetValue(PressedBackgroundProperty, value);
    }

    /// <summary>Gets the value of the <c>HasText</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>HasText</c> value.</returns>
    public static bool GetHasText(DependencyObject element) {
        return (bool)element.GetValue(HasTextProperty);
    }

    /// <summary>Sets the value of the <c>HasText</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetHasText(DependencyObject element, bool value) {
        element.SetValue(HasTextProperty, value);
    }

    /// <summary>Gets the value of the <c>TrackPassword</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>TrackPassword</c> value.</returns>
    public static bool GetTrackPassword(DependencyObject element) {
        return (bool)element.GetValue(TrackPasswordProperty);
    }

    /// <summary>Sets the value of the <c>TrackPassword</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetTrackPassword(DependencyObject element, bool value) {
        element.SetValue(TrackPasswordProperty, value);
    }

    /// <summary>Gets the value of the <c>AnimateProgress</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>AnimateProgress</c> value.</returns>
    public static bool GetAnimateProgress(DependencyObject element) {
        return (bool)element.GetValue(AnimateProgressProperty);
    }

    /// <summary>Sets the value of the <c>AnimateProgress</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetAnimateProgress(DependencyObject element, bool value) {
        element.SetValue(AnimateProgressProperty, value);
    }

    /// <summary>Gets the value of the <c>ProgressRatio</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>ProgressRatio</c> value.</returns>
    public static double GetProgressRatio(DependencyObject element) {
        return (double)element.GetValue(ProgressRatioProperty);
    }

    /// <summary>Sets the value of the <c>ProgressRatio</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetProgressRatio(DependencyObject element, double value) {
        element.SetValue(ProgressRatioProperty, value);
    }

    /// <summary>Gets the value of the <c>TabIndicator</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>TabIndicator</c> value.</returns>
    public static bool GetTabIndicator(DependencyObject element) {
        return (bool)element.GetValue(TabIndicatorProperty);
    }

    /// <summary>Sets the value of the <c>TabIndicator</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetTabIndicator(DependencyObject element, bool value) {
        element.SetValue(TabIndicatorProperty, value);
    }

    /// <summary>Gets the value of the <c>Badge</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Badge</c> value.</returns>
    public static object? GetBadge(DependencyObject element) {
        return element.GetValue(BadgeProperty);
    }

    /// <summary>Sets the value of the <c>Badge</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetBadge(DependencyObject element, object? value) {
        element.SetValue(BadgeProperty, value);
    }

    /// <summary>Gets the value of the <c>BadgeKind</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>BadgeKind</c> value.</returns>
    public static BadgeKind GetBadgeKind(DependencyObject element) {
        return (BadgeKind)element.GetValue(BadgeKindProperty);
    }

    /// <summary>Sets the value of the <c>BadgeKind</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetBadgeKind(DependencyObject element, BadgeKind value) {
        element.SetValue(BadgeKindProperty, value);
    }

    private static bool IsValidIntensity(object value) {
        return value is double intensity && double.IsFinite(intensity) && (intensity >= 0);
    }

    private static void OnCornerRadiusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is Border border) {
            border.CornerRadius = (CornerRadius)e.NewValue;
        }
    }

    private static void OnShadowDepthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        // Controls render the shadow inside their template (so text is never rasterised by the effect);
        // plain elements such as Border get the effect applied directly.
        if ((d is Control) || (d is not FrameworkElement element)) {
            return;
        }
        string? key = MuzzleShadows.GetResourceKey((ShadowDepth)e.NewValue);
        if (key is null) {
            element.ClearValue(UIElement.EffectProperty);
        }
        else {
            element.SetResourceReference(UIElement.EffectProperty, key);
        }
    }

    private static void OnJellyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement element) {
            return;
        }
        JellyMode mode = (JellyMode)e.NewValue;
        JellyController? controller = (JellyController?)element.GetValue(JellyControllerProperty);
        if (mode == JellyMode.None) {
            controller?.Dispose();
            element.ClearValue(JellyControllerProperty);
            return;
        }
        if (controller is null) {
            controller = JellyController.Create(element);
            element.SetValue(JellyControllerProperty, controller);
        }
        controller.Mode = mode;
    }

    private static void OnTabIndicatorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement element) {
            return;
        }
        TabIndicatorController? controller = (TabIndicatorController?)element.GetValue(TabIndicatorControllerProperty);
        controller?.Dispose();
        element.ClearValue(TabIndicatorControllerProperty);
        if (e.NewValue is true) {
            element.SetValue(TabIndicatorControllerProperty, TabIndicatorController.Create(element));
        }
    }

    private static void OnTrackPasswordChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not PasswordBox box) {
            return;
        }
        box.PasswordChanged -= OnPasswordChanged;
        if (e.NewValue is true) {
            box.PasswordChanged += OnPasswordChanged;
            SetHasText(box, box.SecurePassword.Length > 0);
        }
    }

    private static void OnPasswordChanged(object sender, RoutedEventArgs e) {
        if (sender is PasswordBox box) {
            SetHasText(box, box.SecurePassword.Length > 0);
        }
    }

    private static void OnAnimateProgressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not RangeBase range) {
            return;
        }
        range.ValueChanged -= OnRangeValueChanged;
        range.Loaded -= OnRangeLoaded;
        if (e.NewValue is true) {
            range.ValueChanged += OnRangeValueChanged;
            range.Loaded += OnRangeLoaded;
            SnapProgress(range);
        }
    }

    private static void OnRangeLoaded(object sender, RoutedEventArgs e) {
        if (sender is RangeBase range) {
            SnapProgress(range);
        }
    }

    private static void OnRangeValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
        if ((sender is not RangeBase range) || !ReferenceEquals(e.OriginalSource, range)) {
            return;
        }
        if (!range.IsLoaded || !SystemParameters.ClientAreaAnimation) {
            SnapProgress(range);
            return;
        }
        DoubleAnimation animation = new(ComputeRatio(range), TimeSpan.FromMilliseconds(560)) {
            EasingFunction = MuzzleEasings.Get(range, MuzzleEasings.GlideKey)
        };
        range.BeginAnimation(ProgressRatioProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static void SnapProgress(RangeBase range) {
        range.BeginAnimation(ProgressRatioProperty, null);
        range.SetValue(ProgressRatioProperty, ComputeRatio(range));
    }

    private static double ComputeRatio(RangeBase range) {
        double span = range.Maximum - range.Minimum;
        if (span <= 0) {
            return 0;
        }
        return Math.Clamp((range.Value - range.Minimum) / span, 0, 1);
    }
    private static void OnBadgeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not FrameworkElement element) {
            return;
        }
        element.Loaded -= OnBadgeHostLoaded;
        element.Unloaded -= OnBadgeHostUnloaded;
        element.Loaded += OnBadgeHostLoaded;
        element.Unloaded += OnBadgeHostUnloaded;
        if (element.IsLoaded) {
            ApplyBadge(element);
        }
    }

    private static void OnBadgeHostLoaded(object sender, RoutedEventArgs e) {
        ApplyBadge((FrameworkElement)sender);
    }

    private static void OnBadgeHostUnloaded(object sender, RoutedEventArgs e) {
        RemoveBadgeAdorner((FrameworkElement)sender);
    }

    private static void ApplyBadge(FrameworkElement element) {
        object? value = GetBadge(element);
        if ((value is null) || (value is false)) {
            RemoveBadgeAdorner(element);
            return;
        }
        if (element.GetValue(BadgeAdornerProperty) is not BadgeAdorner adorner) {
            if (AdornerLayer.GetAdornerLayer(element) is not AdornerLayer layer) {
                return;
            }
            adorner = new BadgeAdorner(element);
            layer.Add(adorner);
            element.SetValue(BadgeAdornerProperty, adorner);
        }
        Badge badge = adorner.Badge;
        badge.Kind = GetBadgeKind(element);
        switch (value) {
            case true:
                badge.Text = null;
                badge.IsDot = true;
                break;
            case int count:
                badge.IsDot = false;
                badge.Text = null;
                badge.Value = count;
                break;
            case string text when int.TryParse(text, out int parsed):
                badge.IsDot = false;
                badge.Text = null;
                badge.Value = parsed;
                break;
            default:
                badge.IsDot = false;
                badge.Text = value.ToString();
                break;
        }
    }

    private static void RemoveBadgeAdorner(FrameworkElement element) {
        if (element.GetValue(BadgeAdornerProperty) is not BadgeAdorner adorner) {
            return;
        }
        AdornerLayer.GetAdornerLayer(element)?.Remove(adorner);
        element.ClearValue(BadgeAdornerProperty);
    }
}