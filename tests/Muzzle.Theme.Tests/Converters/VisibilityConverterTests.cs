using System;
using System.Globalization;
using System.Windows;

using Muzzle.Theme.Converters;

using Xunit;

namespace Muzzle.Theme.Tests.Converters;

public sealed class BoolToVisibilityConverterTests {
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData(true, Visibility.Visible)]
    [InlineData(false, Visibility.Collapsed)]
    public void Convert_MapsBooleans(bool input, Visibility expected) {
        Assert.Equal(expected, BoolToVisibilityConverter.Instance.Convert(input, typeof(Visibility), null, Culture));
    }

    [Theory]
    [InlineData("Invert")]
    [InlineData("invert")]
    [InlineData("Inverse")]
    [InlineData("Not")]
    [InlineData("true")]
    [InlineData(true)]
    public void Convert_InvertParameter_FlipsResult(object parameter) {
        Assert.Equal(Visibility.Collapsed, BoolToVisibilityConverter.Instance.Convert(true, typeof(Visibility), parameter, Culture));
        Assert.Equal(Visibility.Visible, BoolToVisibilityConverter.Instance.Convert(false, typeof(Visibility), parameter, Culture));
    }

    [Theory]
    [InlineData("false")]
    [InlineData(false)]
    [InlineData("something else")]
    [InlineData(42)]
    public void Convert_NonInvertParameter_IsIgnored(object parameter) {
        Assert.Equal(Visibility.Visible, BoolToVisibilityConverter.Instance.Convert(true, typeof(Visibility), parameter, Culture));
    }

    [Fact]
    public void Convert_InvertedInstanceAndProperty_FlipResult() {
        Assert.Equal(Visibility.Collapsed, BoolToVisibilityConverter.Inverted.Convert(true, typeof(Visibility), null, Culture));
        BoolToVisibilityConverter converter = new() { Invert = true };
        Assert.Equal(Visibility.Visible, converter.Convert(false, typeof(Visibility), null, Culture));
    }

    [Fact]
    public void Convert_InvertPropertyAndInvertParameter_CancelOut() {
        BoolToVisibilityConverter converter = new() { Invert = true };
        Assert.Equal(Visibility.Visible, converter.Convert(true, typeof(Visibility), "Invert", Culture));
    }

    [Fact]
    public void Convert_UseHidden_ReturnsHiddenInsteadOfCollapsed() {
        BoolToVisibilityConverter converter = new() { UseHidden = true };
        Assert.Equal(Visibility.Hidden, converter.Convert(false, typeof(Visibility), null, Culture));
        Assert.Equal(Visibility.Visible, converter.Convert(true, typeof(Visibility), null, Culture));
    }

    [Theory]
    [MemberData(nameof(NonBooleanInputs))]
    public void Convert_NonBooleanInputs_TreatedAsFalse(object? input) {
        Assert.Equal(Visibility.Collapsed, BoolToVisibilityConverter.Instance.Convert(input, typeof(Visibility), null, Culture));
    }

    public static TheoryData<object?> NonBooleanInputs() {
        return new TheoryData<object?> { null, DependencyProperty.UnsetValue, "true", 1, new object(), Visibility.Visible };
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConvertBack_RoundTrips(bool value) {
        object visibility = BoolToVisibilityConverter.Instance.Convert(value, typeof(Visibility), null, Culture);
        Assert.Equal(value, BoolToVisibilityConverter.Instance.ConvertBack(visibility, typeof(bool), null, Culture));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConvertBack_RoundTripsWhenInverted(bool value) {
        object visibility = BoolToVisibilityConverter.Instance.Convert(value, typeof(Visibility), "Invert", Culture);
        Assert.Equal(value, BoolToVisibilityConverter.Instance.ConvertBack(visibility, typeof(bool), "Invert", Culture));
    }

    [Theory]
    [InlineData(Visibility.Visible, true)]
    [InlineData(Visibility.Collapsed, false)]
    [InlineData(Visibility.Hidden, false)]
    public void ConvertBack_MapsVisibility(Visibility input, bool expected) {
        Assert.Equal(expected, BoolToVisibilityConverter.Instance.ConvertBack(input, typeof(bool), null, Culture));
    }

    [Fact]
    public void ConvertBack_UnsetValue_IsFalse() {
        Assert.Equal(false, BoolToVisibilityConverter.Instance.ConvertBack(DependencyProperty.UnsetValue, typeof(bool), null, Culture));
    }
}

public sealed class NullToVisibilityConverterTests {
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [MemberData(nameof(AbsentValues))]
    public void Convert_AbsentValues_AreCollapsed(object? input) {
        Assert.Equal(Visibility.Collapsed, NullToVisibilityConverter.Instance.Convert(input, typeof(Visibility), null, Culture));
    }

    [Theory]
    [MemberData(nameof(PresentValues))]
    public void Convert_PresentValues_AreVisible(object input) {
        Assert.Equal(Visibility.Visible, NullToVisibilityConverter.Instance.Convert(input, typeof(Visibility), null, Culture));
    }

    public static TheoryData<object?> AbsentValues() {
        return new TheoryData<object?> { null, string.Empty, DependencyProperty.UnsetValue };
    }

    public static TheoryData<object> PresentValues() {
        return new TheoryData<object> { "Mochi", " ", 0, false, new object(), Visibility.Collapsed };
    }

    [Fact]
    public void Convert_InvertParameter_FlipsResult() {
        Assert.Equal(Visibility.Visible, NullToVisibilityConverter.Instance.Convert(null, typeof(Visibility), "Invert", Culture));
        Assert.Equal(Visibility.Collapsed, NullToVisibilityConverter.Instance.Convert("Mochi", typeof(Visibility), "Invert", Culture));
        Assert.Equal(Visibility.Visible, NullToVisibilityConverter.Instance.Convert(null, typeof(Visibility), true, Culture));
    }

    [Fact]
    public void Convert_InvertedInstance_FlipsResult() {
        Assert.Equal(Visibility.Visible, NullToVisibilityConverter.Inverted.Convert(null, typeof(Visibility), null, Culture));
        Assert.Equal(Visibility.Collapsed, NullToVisibilityConverter.Inverted.Convert(new object(), typeof(Visibility), null, Culture));
    }

    [Fact]
    public void Convert_UseHidden_ReturnsHidden() {
        NullToVisibilityConverter converter = new() { UseHidden = true };
        Assert.Equal(Visibility.Hidden, converter.Convert(null, typeof(Visibility), null, Culture));
    }

    [Fact]
    public void ConvertBack_IsNotSupported() {
        Assert.Throws<NotSupportedException>(() => NullToVisibilityConverter.Instance.ConvertBack(Visibility.Visible, typeof(object), null, Culture));
    }
}