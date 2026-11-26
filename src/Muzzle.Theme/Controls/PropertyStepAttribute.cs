using System;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Sets the step of a numeric property in a <see cref="PropertyGrid"/>, and optionally how its value is shown:
/// <c>[Range(1, 3), PropertyStep(0.25, Format = "0.00×")]</c>. Values snap to the step.
/// </summary>
/// <param name="step">The step used by the slider, the steppers and the arrow keys. It must be greater than zero.</param>
[AttributeUsage(AttributeTargets.Property)]
public sealed class PropertyStepAttribute(double step) : Attribute {
    /// <summary>Gets the step.</summary>
    public double Step { get; } = step;

    /// <summary>
    /// Gets or sets a numeric format string for the value, such as <c>"0 px"</c> or <c>"0.00×"</c>. When set, a
    /// range editor shows the formatted value as a label instead of an editable box. The default is <see langword="null"/>.
    /// </summary>
    public string? Format { get; set; }
}