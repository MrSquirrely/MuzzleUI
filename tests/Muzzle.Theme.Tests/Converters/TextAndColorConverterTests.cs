using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

using Muzzle.Theme.Controls;
using Muzzle.Theme.Converters;

using Xunit;

namespace Muzzle.Theme.Tests.Converters;

public sealed class StringToInitialsConverterTests {
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    [Theory]
    [InlineData("Muzzle", "M")]
    [InlineData("Sorrel Hartwick", "SH")]
    [InlineData("Lee Loo Dallas", "LD")]
    [InlineData("ada lovelace", "AL")]
    [InlineData("  Grace   Hopper  ", "GH")]
    [InlineData("Jean-Luc Picard", "JP")]
    [InlineData("mochi", "M")]
    [InlineData("Émile Zola", "ÉZ")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("\t\n", "")]
    public void Convert_ProducesInitials(string input, string expected) {
        Assert.Equal(expected, StringToInitialsConverter.Instance.Convert(input, typeof(string), null, Culture));
    }

    [Fact]
    public void Convert_Null_IsEmpty() {
        Assert.Equal(string.Empty, StringToInitialsConverter.Instance.Convert(null, typeof(string), null, Culture));
    }

    [Fact]
    public void Convert_UnsetValue_IsEmpty() {
        Assert.Equal(string.Empty, StringToInitialsConverter.Instance.Convert(DependencyProperty.UnsetValue, typeof(string), null, Culture));
    }

    [Fact]
    public void Convert_NonString_UsesToString() {
        Assert.Equal("4", StringToInitialsConverter.Instance.Convert(42, typeof(string), null, Culture));
    }

    [Fact]
    public void Convert_SingleWordLetters_MatchesAvatarRule() {
        StringToInitialsConverter converter = new() { SingleWordLetters = 2 };
        Assert.Equal("MU", converter.Convert("Muzzle", typeof(string), null, Culture));
        Assert.Equal(Avatar.GetInitials("Muzzle"), converter.Convert("Muzzle", typeof(string), null, Culture));
        Assert.Equal("SH", converter.Convert("Sorrel Hartwick", typeof(string), null, Culture));
    }

    [Fact]
    public void Convert_SurrogatePairs_AreKeptWhole() {
        string result = (string)StringToInitialsConverter.Instance.Convert("😺 Cat", typeof(string), null, Culture);
        Assert.Equal("😺C", result);
    }

    [Fact]
    public void ConvertBack_IsNotSupported() {
        Assert.Throws<NotSupportedException>(() => StringToInitialsConverter.Instance.ConvertBack("SH", typeof(string), null, Culture));
    }
}

public sealed class ColorToSoftTintConverterTests {
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
    private static readonly Color Coral = Color.FromRgb(0xF4, 0x97, 0xA9);

    [Fact]
    public void Convert_Color_KeepsChannelsAndUsesDefaultAlpha() {
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert(Coral, typeof(Brush), null, Culture));
        AssertChannels(Coral, brush.Color);
        Assert.Equal((byte)Math.Round(0.18 * 255), brush.Color.A);
        Assert.True(brush.IsFrozen);
    }

    [Fact]
    public void Convert_SolidColorBrush_MatchesColorInput() {
        SolidColorBrush input = new(Coral);
        SolidColorBrush fromBrush = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert(input, typeof(Brush), null, Culture));
        SolidColorBrush fromColor = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert(Coral, typeof(Brush), null, Culture));
        Assert.Equal(fromColor.Color, fromBrush.Color);
    }

    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.2, 51)]
    [InlineData(0.5, 128)]
    [InlineData(1.0, 255)]
    [InlineData(1.5, 255)]
    [InlineData(-0.3, 0)]
    public void Convert_ParameterSetsClampedStrength(double strength, byte expectedAlpha) {
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert(Coral, typeof(Brush), strength, Culture));
        Assert.Equal(expectedAlpha, brush.Color.A);
        AssertChannels(Coral, brush.Color);
    }

    [Fact]
    public void Convert_StringParameter_IsParsedInvariantly() {
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert(Coral, typeof(Brush), "0.25", Culture));
        Assert.Equal((byte)Math.Round(0.25 * 255), brush.Color.A);
    }

    [Fact]
    public void Convert_OpacityProperty_IsTheDefaultStrength() {
        ColorToSoftTintConverter converter = new() { Opacity = 0.4 };
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(converter.Convert(Coral, typeof(Brush), null, Culture));
        Assert.Equal((byte)Math.Round(0.4 * 255), brush.Color.A);
    }

    [Fact]
    public void Convert_TranslucentInput_MultipliesAlpha() {
        Color half = Color.FromArgb(128, 0x10, 0x20, 0x30);
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert(half, typeof(Brush), 0.5, Culture));
        Assert.Equal((byte)Math.Round(0.5 * (128 / 255.0) * 255), brush.Color.A);
        AssertChannels(half, brush.Color);
    }

    [Fact]
    public void Convert_HexString_IsAccepted() {
        SolidColorBrush brush = Assert.IsType<SolidColorBrush>(ColorToSoftTintConverter.Instance.Convert("#F497A9", typeof(Brush), null, Culture));
        AssertChannels(Coral, brush.Color);
    }

    [Fact]
    public void Convert_ColorTargetType_ReturnsColor() {
        Color color = Assert.IsType<Color>(ColorToSoftTintConverter.Instance.Convert(Coral, typeof(Color), 0.5, Culture));
        Assert.Equal(128, color.A);
    }

    [Theory]
    [MemberData(nameof(InvalidInputs))]
    public void Convert_InvalidInputs_ReturnTransparent(object? input) {
        object result = ColorToSoftTintConverter.Instance.Convert(input, typeof(Brush), null, Culture);
        Assert.Same(Brushes.Transparent, result);
    }

    public static TheoryData<object?> InvalidInputs() {
        return new TheoryData<object?> { null, DependencyProperty.UnsetValue, 42, "not a colour", string.Empty, new LinearGradientBrush(Colors.Red, Colors.Blue, 0) };
    }

    [Fact]
    public void ConvertBack_IsNotSupported() {
        Assert.Throws<NotSupportedException>(() => ColorToSoftTintConverter.Instance.ConvertBack(Brushes.Red, typeof(Color), null, Culture));
    }

    private static void AssertChannels(Color expected, Color actual) {
        Assert.Equal(expected.R, actual.R);
        Assert.Equal(expected.G, actual.G);
        Assert.Equal(expected.B, actual.B);
    }
}