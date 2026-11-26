using System;

namespace Muzzle.Theme;

/// <summary>Which interactions make an element wobble. Combine flags in XAML, e.g. <c>"Hover, Press"</c>.</summary>
[Flags]
public enum JellyMode {
    /// <summary>No jelly physics.</summary>
    None = 0,
    /// <summary>Grow slightly (about 3 %) on hover and settle back on leave.</summary>
    Hover = 1,
    /// <summary>Squish on press (about 0.94 × 0.92) and spring back with an elastic overshoot on release.</summary>
    Press = 2,
    /// <summary>A small pulse when keyboard focus arrives.</summary>
    Focus = 4,
    /// <summary>A bounce when a toggle control is checked or unchecked.</summary>
    Toggle = 8,
    /// <summary>Hover and Press together.</summary>
    HoverPress = Hover | Press,
    /// <summary>Every effect: Hover, Press, Focus and Toggle.</summary>
    All = Hover | Press | Focus | Toggle
}