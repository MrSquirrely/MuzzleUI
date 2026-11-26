namespace Muzzle.Theme;

/// <summary>Elevation levels for Muzzle's soft, diffused drop shadows.</summary>
public enum ShadowDepth {
    /// <summary>No shadow.</summary>
    None,
    /// <summary>Barely-there elevation for resting surfaces.</summary>
    Subtle,
    /// <summary>Soft elevation for cards and buttons.</summary>
    Soft,
    /// <summary>Raised elevation for hovered or important surfaces.</summary>
    Raised,
    /// <summary>The highest elevation, for popups, menus, dialogs and toasts.</summary>
    Floating
}