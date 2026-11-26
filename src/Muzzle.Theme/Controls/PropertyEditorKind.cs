namespace Muzzle.Theme.Controls;

/// <summary>The editor a <see cref="PropertyGrid"/> generates for a property.</summary>
public enum PropertyEditorKind {
    /// <summary>A text box, for strings and other types that convert to and from text.</summary>
    Text,
    /// <summary>A numeric field with − / + steppers.</summary>
    Numeric,
    /// <summary>A slider, for numbers with a <c>[Range]</c> attribute.</summary>
    Range,
    /// <summary>A toggle switch.</summary>
    Boolean,
    /// <summary>A three-state check box for <c>bool?</c>.</summary>
    NullableBoolean,
    /// <summary>A combo box of the enum's values.</summary>
    Enum,
    /// <summary>A colour swatch with hex text and pastel presets.</summary>
    Color,
    /// <summary>A solid-colour brush editor (swatch, hex text and presets).</summary>
    Brush,
    /// <summary>A read-only display (no setter, or <c>[ReadOnly(true)]</c>).</summary>
    ReadOnly,

    /// <summary>An element created by the <see cref="PropertyEditorBase"/> named in the property's <c>[Editor]</c> attribute.</summary>
    Custom
}