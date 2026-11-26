using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

using Muzzle.Theme.Controls;

namespace Muzzle.Theme.Converters;

internal static class ConverterParameters {
    /// <summary>True when the parameter asks for inversion: true, "Invert", "Inverse", "Not" or "true".</summary>
    public static bool IsInvert(object? parameter) {
        return parameter switch {
            bool flag => flag,
            string text => text.Equals("Invert", StringComparison.OrdinalIgnoreCase)
                || text.Equals("Inverse", StringComparison.OrdinalIgnoreCase)
                || text.Equals("Not", StringComparison.OrdinalIgnoreCase)
                || text.Equals("True", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}

/// <summary>
/// true → Visible, false → Collapsed (or Hidden with <see cref="UseHidden"/>). Inverts with <see cref="Invert"/> or a
/// ConverterParameter of "Invert". Converts back too.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static BoolToVisibilityConverter Instance { get; } = new();

    /// <summary>Gets a shared inverted instance: <see langword="true"/> → Collapsed, <see langword="false"/> → Visible.</summary>
    public static BoolToVisibilityConverter Inverted { get; } = new() { Invert = true };

    /// <summary>Gets or sets whether the mapping is inverted. Combined with an "Invert" ConverterParameter the two cancel out. The default is <see langword="false"/>.</summary>
    public bool Invert { get; set; }

    /// <summary>Gets or sets whether "off" maps to Hidden (keeping layout space) instead of Collapsed. The default is <see langword="false"/>.</summary>
    public bool UseHidden { get; set; }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        bool visible = (value is true) ^ (Invert ^ ConverterParameters.IsInvert(parameter));
        return visible ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return (value is Visibility.Visible) ^ (Invert ^ ConverterParameters.IsInvert(parameter));
    }
}

/// <summary>null (or an empty string) → Collapsed, anything else → Visible. Inverts like <see cref="BoolToVisibilityConverter"/>.</summary>
public sealed class NullToVisibilityConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static NullToVisibilityConverter Instance { get; } = new();

    /// <summary>Gets a shared inverted instance: absent values → Visible, present values → Collapsed.</summary>
    public static NullToVisibilityConverter Inverted { get; } = new() { Invert = true };

    /// <summary>Gets or sets whether the mapping is inverted. The default is <see langword="false"/>.</summary>
    public bool Invert { get; set; }

    /// <summary>Gets or sets whether absent values map to Hidden instead of Collapsed. The default is <see langword="false"/>.</summary>
    public bool UseHidden { get; set; }

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        bool present = (value is not (null or "")) && !ReferenceEquals(value, DependencyProperty.UnsetValue);
        bool visible = present ^ (Invert ^ ConverterParameters.IsInvert(parameter));
        return visible ? Visibility.Visible : (UseHidden ? Visibility.Hidden : Visibility.Collapsed);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>
/// "Sorrel Hartwick" → "SH", "Lee Loo Dallas" → "LD" (first + last word), "Muzzle" → "M"; blank → "".
/// Set <see cref="SingleWordLetters"/> to 2 for the <see cref="Avatar"/> rule ("Muzzle" → "MU").
/// </summary>
public sealed class StringToInitialsConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static StringToInitialsConverter Instance { get; } = new();

    /// <summary>Letters taken from a single-word name (default 1).</summary>
    public int SingleWordLetters { get; set; } = 1;

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if ((value is null) || ReferenceEquals(value, DependencyProperty.UnsetValue)) {
            return string.Empty;
        }
        return Avatar.CreateInitials(value.ToString(), SingleWordLetters);
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Turns a <see cref="Color"/> or <see cref="SolidColorBrush"/> into a soft, semi-transparent tint brush — a pastel
/// wash for chips, highlights and backgrounds. ConverterParameter (or <see cref="Opacity"/>) sets the strength, 0–1.
/// </summary>
public sealed class ColorToSoftTintConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static ColorToSoftTintConverter Instance { get; } = new();

    /// <summary>Default tint strength (0.18).</summary>
    public double Opacity { get; set; } = 0.18;

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        Color? color = value switch {
            Color c => c,
            SolidColorBrush brush => brush.Color,
            string text => TryParse(text),
            _ => null
        };
        if (color is not Color baseColor) {
            return Brushes.Transparent;
        }
        double strength = parameter switch {
            double number => number,
            string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
            _ => Opacity
        };
        byte alpha = (byte)Math.Round(Math.Clamp(strength, 0, 1) * (baseColor.A / 255.0) * 255);
        SolidColorBrush tint = new(Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B));
        tint.Freeze();
        if (targetType == typeof(Color)) {
            return tint.Color;
        }
        return tint;
    }

    private static Color? TryParse(string text) {
        try {
            return ColorConverter.ConvertFromString(text) as Color?;
        }
        catch (Exception exception) when (exception is FormatException or NotSupportedException or ArgumentException) {
            return null;
        }
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}