using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Muzzle.Theme.Converters;

/// <summary>Converts a <see cref="ShadowDepth"/> into the matching themed shadow effect.</summary>
public sealed class ShadowDepthToEffectConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static ShadowDepthToEffectConverter Instance { get; } = new();

    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is ShadowDepth depth ? MuzzleShadows.Resolve(depth) : null;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>Grows (or shrinks) a CornerRadius by the amount in ConverterParameter; used for focus rings that sit outside a control.</summary>
public sealed class CornerRadiusOffsetConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static CornerRadiusOffsetConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not CornerRadius radius) {
            return new CornerRadius();
        }
        double offset = parameter switch {
            double number => number,
            string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
            _ => 0
        };
        return new CornerRadius(
            Math.Max(0, radius.TopLeft + offset),
            Math.Max(0, radius.TopRight + offset),
            Math.Max(0, radius.BottomRight + offset),
            Math.Max(0, radius.BottomLeft + offset));
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>Adds the number in ConverterParameter to a double (e.g. a drawer's size plus its bounce gutter).</summary>
public sealed class DoubleOffsetConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static DoubleOffsetConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        double offset = parameter switch {
            double number => number,
            string text when double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) => parsed,
            _ => 0
        };
        return value is double baseValue ? baseValue + offset : DependencyProperty.UnsetValue;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>Converts a uniform double into a CornerRadius.</summary>
public sealed class DoubleToCornerRadiusConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static DoubleToCornerRadiusConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is double radius ? new CornerRadius(Math.Max(0, radius)) : new CornerRadius();
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is CornerRadius radius ? radius.TopLeft : 0.0;
    }
}

/// <summary>
/// (ratio, trackWidth, trackHeight) → indicator width. Keeps a visible pill end-cap for any non-zero progress.
/// </summary>
public sealed class ProgressWidthConverter : IMultiValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static ProgressWidthConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object[] values, Type targetType, object? parameter, CultureInfo culture) {
        if ((values.Length < 3) || (values[0] is not double ratio) || (values[1] is not double width) || (values[2] is not double height)) {
            return 0.0;
        }
        if ((ratio <= 0) || (width <= 0)) {
            return 0.0;
        }
        return Math.Min(width, Math.Max(height, ratio * width));
    }

    /// <inheritdoc />
    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) {
        throw new NotSupportedException();
    }
}

/// <summary>Negates a boolean in both directions; anything that isn't <see langword="true"/> counts as <see langword="false"/>.</summary>
public sealed class InverseBooleanConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static InverseBooleanConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is not true;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is not true;
    }
}

/// <summary>Color ⇄ frozen SolidColorBrush.</summary>
public sealed class ColorToBrushConverter : IValueConverter {
    /// <summary>Gets a shared instance of the converter (use it with <c>{x:Static}</c>).</summary>
    public static ColorToBrushConverter Instance { get; } = new();

    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not Color color) {
            return Brushes.Transparent;
        }
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        return value is SolidColorBrush brush ? brush.Color : Colors.Transparent;
    }
}