using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Muzzle.Theme;

/// <summary>
/// Resolves <see cref="ShadowDepth"/> values to Muzzle's elevation shadows.
/// <para>Templates that bind <c>MuzzleAssist.ShadowDepth</c> through a converter receive one shared, live
/// <see cref="DropShadowEffect"/> per depth. <see cref="Refresh"/> (called by <see cref="MuzzleThemeManager"/>) copies
/// the active palette's <c>Muzzle.Shadow.*</c> values into those instances, so shadows follow theme switches too.</para>
/// </summary>
public static class MuzzleShadows {
    /// <summary>Resource key of the subtle (lowest) elevation shadow.</summary>
    public const string SubtleKey = "Muzzle.Shadow.Subtle";
    /// <summary>Resource key of the soft elevation shadow.</summary>
    public const string SoftKey = "Muzzle.Shadow.Soft";
    /// <summary>Resource key of the raised elevation shadow.</summary>
    public const string RaisedKey = "Muzzle.Shadow.Raised";
    /// <summary>Resource key of the floating (highest) elevation shadow.</summary>
    public const string FloatingKey = "Muzzle.Shadow.Floating";

    private static readonly Color DefaultShadowColor = Color.FromRgb(0x4B, 0x2E, 0x3B);
    private static readonly DropShadowEffect Subtle = Create(12, 2, 0.08);
    private static readonly DropShadowEffect Soft = Create(16, 4, 0.09);
    private static readonly DropShadowEffect Raised = Create(20, 6, 0.12);
    private static readonly DropShadowEffect Floating = Create(24, 10, 0.15);
    private static bool synced;

    /// <summary>Gets the resource key of an elevation's shadow.</summary>
    /// <param name="depth">The elevation.</param>
    /// <returns>The key, or <see langword="null"/> for <see cref="ShadowDepth.None"/>.</returns>
    public static string? GetResourceKey(ShadowDepth depth) {
        return depth switch {
            ShadowDepth.Subtle => SubtleKey,
            ShadowDepth.Soft => SoftKey,
            ShadowDepth.Raised => RaisedKey,
            ShadowDepth.Floating => FloatingKey,
            _ => null
        };
    }

    /// <summary>The live effect for a depth (null for <see cref="ShadowDepth.None"/>).</summary>
    public static Effect? Resolve(ShadowDepth depth) {
        if (depth == ShadowDepth.None) {
            return null;
        }
        if (!synced) {
            Refresh();
        }
        return depth switch {
            ShadowDepth.Subtle => Subtle,
            ShadowDepth.Soft => Soft,
            ShadowDepth.Raised => Raised,
            _ => Floating
        };
    }

    /// <summary>Copies the active theme's shadow resources into the live effects.</summary>
    public static void Refresh() {
        if (Application.Current is not Application application) {
            return;
        }
        synced = true;
        Copy(application, SubtleKey, Subtle);
        Copy(application, SoftKey, Soft);
        Copy(application, RaisedKey, Raised);
        Copy(application, FloatingKey, Floating);
    }

    private static void Copy(Application application, string key, DropShadowEffect target) {
        if (application.TryFindResource(key) is not DropShadowEffect source) {
            return;
        }
        target.BlurRadius = source.BlurRadius;
        target.ShadowDepth = source.ShadowDepth;
        target.Direction = source.Direction;
        target.Opacity = source.Opacity;
        target.Color = source.Color;
        target.RenderingBias = source.RenderingBias;
    }

    private static DropShadowEffect Create(double blur, double depth, double opacity) {
        return new DropShadowEffect {
            BlurRadius = blur,
            ShadowDepth = depth,
            Direction = 270,
            Opacity = opacity,
            Color = DefaultShadowColor,
            RenderingBias = RenderingBias.Performance
        };
    }
}