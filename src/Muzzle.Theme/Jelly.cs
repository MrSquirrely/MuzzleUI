using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme;

/// <summary>Imperative jelly helpers for code-behind: wobble, squish and settle any element.</summary>
public static class Jelly {
    /// <summary>Whether motion should play for the element (honours the OS animation setting and <see cref="MuzzleAssist.JellyIntensityProperty"/>).</summary>
    public static bool IsMotionEnabled(FrameworkElement element) {
        return SystemParameters.ClientAreaAnimation && (MuzzleAssist.GetJellyIntensity(element) > 0);
    }

    /// <summary>Plays a playful squash-and-stretch wobble that settles back to rest.</summary>
    public static void Wobble(FrameworkElement element) {
        double k = MuzzleAssist.GetJellyIntensity(element);
        if (!IsMotionEnabled(element)) {
            return;
        }
        ScaleTransform scale = EnsureScale(element);
        IEasingFunction squish = MuzzleEasings.Get(element, MuzzleEasings.SquishKey);
        IEasingFunction wobble = MuzzleEasings.Get(element, MuzzleEasings.WobbleKey);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, CreatePulse(1 + (0.12 * k), 1, 130, 900, squish, wobble));
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, CreatePulse(1 - (0.10 * k), 1, 130, 900, squish, wobble));
    }

    /// <summary>Returns the element's animatable <see cref="ScaleTransform"/>, installing one centred on the element if needed.</summary>
    public static ScaleTransform EnsureScale(FrameworkElement element) {
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        if (element.RenderTransform is ScaleTransform { IsFrozen: false } existing) {
            return existing;
        }
        if (element.RenderTransform is TransformGroup { IsFrozen: false } group) {
            foreach (Transform child in group.Children) {
                if (child is ScaleTransform { IsFrozen: false } grouped) {
                    return grouped;
                }
            }
            ScaleTransform added = new(1, 1);
            group.Children.Insert(0, added);
            return added;
        }
        if ((element.RenderTransform is Transform existingTransform) && !ReferenceEquals(existingTransform, Transform.Identity)) {
            // Keep whatever transform the element already had (rotate, skew, translate…) and add the scale beside it.
            ScaleTransform wrapped = new(1, 1);
            element.RenderTransform = new TransformGroup { Children = [wrapped, existingTransform] };
            return wrapped;
        }
        ScaleTransform scale = new(1, 1);
        element.RenderTransform = scale;
        return scale;
    }

    internal static DoubleAnimationUsingKeyFrames CreatePulse(double peak, double settle, double peakMilliseconds, double totalMilliseconds, IEasingFunction toPeak, IEasingFunction toSettle) {
        DoubleAnimationUsingKeyFrames animation = new();
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(peak, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(peakMilliseconds)), toPeak));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(settle, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(totalMilliseconds)), toSettle));
        return animation;
    }
}