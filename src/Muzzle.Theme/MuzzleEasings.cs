using System.Windows;
using System.Windows.Media.Animation;

namespace Muzzle.Theme;

/// <summary>Keys of the shared easing resources in <c>JellyEasings.xaml</c>, with frozen fallbacks for code-driven animation.</summary>
public static class MuzzleEasings {
    /// <summary>Resource key of the signature spring: an elastic ease-out with two oscillations.</summary>
    public const string JellyKey = "Muzzle.Ease.Jelly";
    /// <summary>Resource key of a gentler elastic ease-out (one oscillation).</summary>
    public const string JellySoftKey = "Muzzle.Ease.JellySoft";
    /// <summary>Resource key of a bouncier elastic ease-out (three oscillations), used for wobbles.</summary>
    public const string WobbleKey = "Muzzle.Ease.Wobble";
    /// <summary>Resource key of a back ease-out: a small overshoot that settles.</summary>
    public const string BackKey = "Muzzle.Ease.Back";
    /// <summary>Resource key of a softer back ease-out.</summary>
    public const string BackSoftKey = "Muzzle.Ease.BackSoft";
    /// <summary>Resource key of a pronounced back ease-out (chevron flips, pills).</summary>
    public const string BackStrongKey = "Muzzle.Ease.BackStrong";
    /// <summary>Resource key of a back ease-in: a little wind-up before leaving.</summary>
    public const string BackInKey = "Muzzle.Ease.BackIn";
    /// <summary>Resource key of the quick ease used for the press squish.</summary>
    public const string SquishKey = "Muzzle.Ease.Squish";
    /// <summary>Resource key of a smooth ease-in-out.</summary>
    public const string SmoothKey = "Muzzle.Ease.Smooth";
    /// <summary>Resource key of a smooth deceleration for glides, slides and pans.</summary>
    public const string GlideKey = "Muzzle.Ease.Glide";

    private static readonly IEasingFunction Jelly = Freeze(new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 2, Springiness = 6 });
    private static readonly IEasingFunction JellySoft = Freeze(new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 5 });
    private static readonly IEasingFunction Wobble = Freeze(new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 3, Springiness = 4 });
    private static readonly IEasingFunction Back = Freeze(new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 });
    private static readonly IEasingFunction BackSoft = Freeze(new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.4 });
    private static readonly IEasingFunction BackStrong = Freeze(new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.6 });
    private static readonly IEasingFunction BackIn = Freeze(new BackEase { EasingMode = EasingMode.EaseIn, Amplitude = 0.4 });
    private static readonly IEasingFunction Squish = Freeze(new CubicEase { EasingMode = EasingMode.EaseOut });
    private static readonly IEasingFunction Smooth = Freeze(new CubicEase { EasingMode = EasingMode.EaseInOut });
    private static readonly IEasingFunction Glide = Freeze(new QuinticEase { EasingMode = EasingMode.EaseOut });

    /// <summary>Looks up an easing from the element's resources (so themes can retune it), or returns the built-in default.</summary>
    public static IEasingFunction Get(FrameworkElement? element, string key) {
        if (element?.TryFindResource(key) is IEasingFunction themed) {
            return themed;
        }
        return GetFallback(key);
    }

    /// <summary>Returns Muzzle's built-in easing for a key without looking at resources.</summary>
    /// <param name="key">One of the <c>*Key</c> constants.</param>
    /// <returns>The easing (an ease-out quadratic for unknown keys).</returns>
    public static IEasingFunction GetFallback(string key) {
        return key switch {
            JellyKey => Jelly,
            JellySoftKey => JellySoft,
            WobbleKey => Wobble,
            BackKey => Back,
            BackSoftKey => BackSoft,
            BackStrongKey => BackStrong,
            BackInKey => BackIn,
            SmoothKey => Smooth,
            GlideKey => Glide,
            _ => Squish
        };
    }

    private static IEasingFunction Freeze(EasingFunctionBase easing) {
        easing.Freeze();
        return easing;
    }
}