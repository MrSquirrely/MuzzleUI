namespace Muzzle.Theme.Controls;

/// <summary>How a <see cref="PropertyGrid"/> orders its properties and categories.</summary>
public enum PropertyGridSortMode {
    /// <summary>Properties by display name and categories by name, with "Misc" last.</summary>
    Alphabetical,

    /// <summary>
    /// Properties in the order the type declares them, or by <c>[Display(Order = n)]</c> where present;
    /// each category sits where its first property does.
    /// </summary>
    Declared
}