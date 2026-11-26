using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Moves a highlight "pill" over a target element with a springy BackEase overshoot.
/// The pill must be a top/left-aligned sibling of the targets inside <c>container</c>.
/// </summary>
internal static class PillAnimator {
    private static readonly TimeSpan SlideDuration = TimeSpan.FromMilliseconds(460);
    private static readonly TimeSpan StretchDuration = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// Places <paramref name="pill"/> over <paramref name="target"/>. Only the requested axes are tracked
    /// (position and size); an untracked axis keeps its layout (e.g. a Stretch-aligned width).
    /// Returns false (and hides the pill) when there is no visible target.
    /// </summary>
    public static bool MoveTo(FrameworkElement pill, FrameworkElement? target, Visual container, bool animate, bool trackX, bool trackY) {
        if ((target is null) || !target.IsVisible || (target.ActualWidth <= 0) || !target.IsDescendantOf(container)) {
            pill.Opacity = 0;
            return false;
        }
        bool wasHidden = pill.Opacity < 1;
        pill.Opacity = 1;
        TranslateTransform shift = EnsureTranslate(pill);
        Point origin = target.TransformToAncestor(container).Transform(new Point(0, 0));
        bool motion = animate && !wasHidden && Jelly.IsMotionEnabled(pill);
        IEasingFunction slide = MuzzleEasings.Get(pill, MuzzleEasings.BackKey);
        IEasingFunction stretch = MuzzleEasings.Get(pill, MuzzleEasings.JellySoftKey);
        if (trackX) {
            Apply(shift, TranslateTransform.XProperty, origin.X, motion, SlideDuration, slide);
            Apply(pill, FrameworkElement.WidthProperty, target.ActualWidth, motion, StretchDuration, stretch);
        }
        if (trackY) {
            Apply(shift, TranslateTransform.YProperty, origin.Y, motion, SlideDuration, slide);
            Apply(pill, FrameworkElement.HeightProperty, target.ActualHeight, motion, StretchDuration, stretch);
        }
        return true;
    }

    private static void Apply(IAnimatable owner, DependencyProperty property, double value, bool motion, TimeSpan duration, IEasingFunction easing) {
        // An unset size (NaN) can't be an animation origin: snap the first placement.
        if (motion && !double.IsNaN((double)((DependencyObject)owner).GetValue(property))) {
            owner.BeginAnimation(property, new DoubleAnimation(value, duration) { EasingFunction = easing });
            return;
        }
        owner.BeginAnimation(property, null);
        ((DependencyObject)owner).SetValue(property, value);
    }

    private static TranslateTransform EnsureTranslate(FrameworkElement pill) {
        if (pill.RenderTransform is TranslateTransform { IsFrozen: false } existing) {
            return existing;
        }
        TranslateTransform shift = new();
        pill.RenderTransform = shift;
        return shift;
    }
}