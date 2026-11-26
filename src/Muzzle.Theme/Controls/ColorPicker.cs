using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>
/// An inline colour picker: an HSV saturation/brightness canvas, hue and alpha sliders, pastel preset swatches,
/// and Hex / RGB inputs. Every view stays in sync; <see cref="SelectedColor"/> and <see cref="SelectedBrush"/> are
/// two-way bindable.
/// </summary>
[TemplatePart(Name = SpectrumPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SpectrumThumbPartName, Type = typeof(FrameworkElement))]
public class ColorPicker : Control {
    // Declared first: static fields initialise in order and PresetsProperty uses this as its default.
    private static readonly IList<Color> DefaultPresets = Array.AsReadOnly([
        Color.FromRgb(0xF4, 0x97, 0xA9),
        Color.FromRgb(0xF7, 0xAB, 0xB9),
        Color.FromRgb(0xF2, 0x94, 0x9C),
        Color.FromRgb(0xF9, 0xC3, 0xA5),
        Color.FromRgb(0xF7, 0xCF, 0x8B),
        Color.FromRgb(0xF8, 0xDE, 0x9B),
        Color.FromRgb(0x9F, 0xD8, 0xCB),
        Color.FromRgb(0x98, 0xC9, 0xF2),
        Color.FromRgb(0xA9, 0xD3, 0xF0),
        Color.FromRgb(0xB8, 0xA9, 0xE6),
        Color.FromRgb(0xDD, 0xD5, 0xF5),
        Color.FromRgb(0xFA, 0xF8, 0xF6),
        Color.FromRgb(0x78, 0x73, 0x85),
        Color.FromRgb(0x2E, 0x2B, 0x36)
    ]);

    /// <summary>Applies a preset swatch; the command parameter is the <see cref="Color"/>.</summary>
    public static readonly RoutedCommand ApplyPresetCommand = new(nameof(ApplyPresetCommand), typeof(ColorPicker));

    /// <summary>Identifies the <see cref="SelectedColorChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectedColorChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedColorChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ColorPicker));

    /// <summary>Identifies the <see cref="SelectedColor"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorPicker),
        new FrameworkPropertyMetadata(Color.FromRgb(0xF4, 0x97, 0xA9), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

    /// <summary>Identifies the <see cref="SelectedBrush"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
        nameof(SelectedBrush), typeof(SolidColorBrush), typeof(ColorPicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

    /// <summary>Identifies the <see cref="Hue"/> dependency property.</summary>
    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnHsvChanged));

    /// <summary>Identifies the <see cref="Saturation"/> dependency property.</summary>
    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnHsvChanged));

    /// <summary>Identifies the <see cref="Brightness"/> dependency property.</summary>
    public static readonly DependencyProperty BrightnessProperty = DependencyProperty.Register(
        nameof(Brightness), typeof(double), typeof(ColorPicker), new PropertyMetadata(1.0, OnHsvChanged));

    /// <summary>Identifies the <see cref="Alpha"/> dependency property.</summary>
    public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
        nameof(Alpha), typeof(double), typeof(ColorPicker), new PropertyMetadata(255.0, OnAlphaChanged));

    /// <summary>Identifies the <see cref="Red"/> dependency property.</summary>
    public static readonly DependencyProperty RedProperty = DependencyProperty.Register(
        nameof(Red), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnRgbChanged));

    /// <summary>Identifies the <see cref="Green"/> dependency property.</summary>
    public static readonly DependencyProperty GreenProperty = DependencyProperty.Register(
        nameof(Green), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnRgbChanged));

    /// <summary>Identifies the <see cref="Blue"/> dependency property.</summary>
    public static readonly DependencyProperty BlueProperty = DependencyProperty.Register(
        nameof(Blue), typeof(double), typeof(ColorPicker), new PropertyMetadata(0.0, OnRgbChanged));

    /// <summary>"#RRGGBB" (or "#AARRGGBB" when translucent). Accepts input without '#' and 3/4/6/8-digit forms.</summary>
    public static readonly DependencyProperty HexTextProperty = DependencyProperty.Register(
        nameof(HexText), typeof(string), typeof(ColorPicker), new PropertyMetadata(string.Empty, OnHexTextChanged));

    /// <summary>Identifies the <see cref="ShowAlpha"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
        nameof(ShowAlpha), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="Presets"/> dependency property.</summary>
    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(
        nameof(Presets), typeof(IList<Color>), typeof(ColorPicker), new PropertyMetadata(DefaultPresets));

    private static readonly DependencyPropertyKey HueBrushPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HueBrush), typeof(Brush), typeof(ColorPicker), new PropertyMetadata(Brushes.Red));

    /// <summary>The pure hue, painted behind the saturation/brightness canvas.</summary>
    public static readonly DependencyProperty HueBrushProperty = HueBrushPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey AlphaGradientPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(AlphaGradient), typeof(Brush), typeof(ColorPicker), new PropertyMetadata(Brushes.Transparent));

    /// <summary>Transparent → opaque gradient of the current colour, for the alpha slider track.</summary>
    public static readonly DependencyProperty AlphaGradientProperty = AlphaGradientPropertyKey.DependencyProperty;

    private const string SpectrumPartName = "PART_Spectrum";
    private const string SpectrumThumbPartName = "PART_SpectrumThumb";

    private FrameworkElement? spectrum;
    private FrameworkElement? spectrumThumb;
    private readonly ScaleTransform thumbScale = new(1, 1);
    private bool syncing;

    static ColorPicker() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(typeof(ColorPicker)));
        FocusableProperty.OverrideMetadata(typeof(ColorPicker), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="ColorPicker"/> class.</summary>
    public ColorPicker() {
        CommandBindings.Add(new CommandBinding(ApplyPresetCommand, OnApplyPreset));
        Sync(SelectedColor, ColorSource.External);
    }

    private enum ColorSource {
        External,
        Hsv,
        Alpha,
        Rgb,
        Hex
    }

    /// <summary>Occurs when <see cref="SelectedColor"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<Color> SelectedColorChanged {
        add => AddHandler(SelectedColorChangedEvent, value);
        remove => RemoveHandler(SelectedColorChangedEvent, value);
    }

    /// <summary>Gets or sets the selected color. The default is <c>Color.FromRgb(0xF4, 0x97, 0xA9)</c>. This is a dependency property.</summary>
    public Color SelectedColor {
        get => (Color)GetValue(SelectedColorProperty);
        set => SetValue(SelectedColorProperty, value);
    }

    /// <summary>Gets or sets the selected brush. The default is <see langword="null"/>. This is a dependency property.</summary>
    public SolidColorBrush? SelectedBrush {
        get => (SolidColorBrush?)GetValue(SelectedBrushProperty);
        set => SetValue(SelectedBrushProperty, value);
    }

    /// <summary>0–360.</summary>
    public double Hue {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>0–1.</summary>
    public double Saturation {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    /// <summary>0–1 (HSV value).</summary>
    public double Brightness {
        get => (double)GetValue(BrightnessProperty);
        set => SetValue(BrightnessProperty, value);
    }

    /// <summary>0–255.</summary>
    public double Alpha {
        get => (double)GetValue(AlphaProperty);
        set => SetValue(AlphaProperty, value);
    }

    /// <summary>Gets or sets the red. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double Red {
        get => (double)GetValue(RedProperty);
        set => SetValue(RedProperty, value);
    }

    /// <summary>Gets or sets the green. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double Green {
        get => (double)GetValue(GreenProperty);
        set => SetValue(GreenProperty, value);
    }

    /// <summary>Gets or sets the blue. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double Blue {
        get => (double)GetValue(BlueProperty);
        set => SetValue(BlueProperty, value);
    }

    /// <summary>Gets or sets the hex text. The default is an empty string. This is a dependency property.</summary>
    public string HexText {
        get => (string)GetValue(HexTextProperty);
        set => SetValue(HexTextProperty, value);
    }

    /// <summary>Gets or sets whether to show the alpha. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowAlpha {
        get => (bool)GetValue(ShowAlphaProperty);
        set => SetValue(ShowAlphaProperty, value);
    }

    /// <summary>Gets or sets the presets. The default is <c>DefaultPresets</c>. This is a dependency property.</summary>
    public IList<Color> Presets {
        get => (IList<Color>)GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    /// <summary>Gets the hue brush. This is a read-only dependency property.</summary>
    public Brush HueBrush => (Brush)GetValue(HueBrushProperty);

    /// <summary>Gets the alpha gradient. This is a read-only dependency property.</summary>
    public Brush AlphaGradient => (Brush)GetValue(AlphaGradientProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (spectrum is not null) {
            spectrum.MouseLeftButtonDown -= OnSpectrumMouseDown;
            spectrum.MouseMove -= OnSpectrumMouseMove;
            spectrum.MouseLeftButtonUp -= OnSpectrumMouseUp;
            spectrum.KeyDown -= OnSpectrumKeyDown;
            spectrum.SizeChanged -= OnSpectrumSizeChanged;
        }
        base.OnApplyTemplate();
        spectrum = GetTemplateChild(SpectrumPartName) as FrameworkElement;
        spectrumThumb = GetTemplateChild(SpectrumThumbPartName) as FrameworkElement;
        if (spectrumThumb is not null) {
            spectrumThumb.RenderTransform = thumbScale;
            spectrumThumb.RenderTransformOrigin = new Point(0.5, 0.5);
        }
        if (spectrum is not null) {
            spectrum.MouseLeftButtonDown += OnSpectrumMouseDown;
            spectrum.MouseMove += OnSpectrumMouseMove;
            spectrum.MouseLeftButtonUp += OnSpectrumMouseUp;
            spectrum.KeyDown += OnSpectrumKeyDown;
            spectrum.SizeChanged += OnSpectrumSizeChanged;
        }
        PlaceThumb();
    }

    /// <summary>Converts hue / saturation / brightness to a colour.</summary>
    /// <param name="hue">Hue in degrees, 0–360.</param>
    /// <param name="saturation">Saturation, 0–1.</param>
    /// <param name="brightness">Brightness (value), 0–1.</param>
    /// <param name="alpha">Alpha, 0–255.</param>
    /// <returns>The colour.</returns>
    public static Color FromHsv(double hue, double saturation, double brightness, byte alpha = 255) {
        double h = ((hue % 360) + 360) % 360 / 60;
        double c = brightness * saturation;
        double x = c * (1 - Math.Abs((h % 2) - 1));
        double m = brightness - c;
        (double r, double g, double b) = (int)Math.Floor(h) switch {
            0 => (c, x, 0.0),
            1 => (x, c, 0.0),
            2 => (0.0, c, x),
            3 => (0.0, x, c),
            4 => (x, 0.0, c),
            _ => (c, 0.0, x)
        };
        return Color.FromArgb(alpha, ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    /// <summary>Converts a colour to hue / saturation / brightness.</summary>
    /// <param name="color">The colour (alpha is ignored).</param>
    /// <returns>Hue in degrees (0–360), saturation and brightness (0–1).</returns>
    public static (double Hue, double Saturation, double Brightness) ToHsv(Color color) {
        double r = color.R / 255.0;
        double g = color.G / 255.0;
        double b = color.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b));
        double min = Math.Min(r, Math.Min(g, b));
        double delta = max - min;
        double hue = 0;
        if (delta > 0) {
            if (max == r) {
                hue = 60 * (((g - b) / delta) % 6);
            }
            else if (max == g) {
                hue = 60 * (((b - r) / delta) + 2);
            }
            else {
                hue = 60 * (((r - g) / delta) + 4);
            }
        }
        if (hue < 0) {
            hue += 360;
        }
        return (hue, max == 0 ? 0 : delta / max, max);
    }

    private static byte ToByte(double unit) {
        return (byte)Math.Clamp(Math.Round(unit * 255), 0, 255);
    }

    private static string ToHex(Color color) {
        return color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private static bool TryParseHex(string text, out Color color) {
        color = default;
        string hex = text.Trim().TrimStart('#');
        if ((hex.Length is not (3 or 4 or 6 or 8)) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _)) {
            return false;
        }
        if (hex.Length is 3 or 4) {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2], hex.Length == 4 ? string.Concat(hex[3], hex[3]) : string.Empty);
        }
        uint value = uint.Parse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        // WPF ordering: #ARGB / #AARRGGBB.
        color = hex.Length == 8
            ? Color.FromArgb((byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value)
            : Color.FromRgb((byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return true;
    }

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPicker picker = (ColorPicker)d;
        if (!picker.syncing) {
            picker.Sync((Color)e.NewValue, ColorSource.External);
        }
        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<Color>((Color)e.OldValue, (Color)e.NewValue, SelectedColorChangedEvent));
    }

    private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPicker picker = (ColorPicker)d;
        if (!picker.syncing && (e.NewValue is SolidColorBrush brush)) {
            picker.SetCurrentValue(SelectedColorProperty, brush.Color);
        }
    }

    private static void OnHsvChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPicker picker = (ColorPicker)d;
        if (!picker.syncing) {
            picker.Sync(FromHsv(picker.Hue, Math.Clamp(picker.Saturation, 0, 1), Math.Clamp(picker.Brightness, 0, 1), (byte)Math.Clamp(picker.Alpha, 0, 255)), ColorSource.Hsv);
        }
    }

    private static void OnAlphaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPicker picker = (ColorPicker)d;
        if (!picker.syncing) {
            Color current = picker.SelectedColor;
            picker.Sync(Color.FromArgb((byte)Math.Clamp(Math.Round((double)e.NewValue), 0, 255), current.R, current.G, current.B), ColorSource.Alpha);
        }
    }

    private static void OnRgbChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPicker picker = (ColorPicker)d;
        if (!picker.syncing) {
            byte r = (byte)Math.Clamp(Math.Round(picker.Red), 0, 255);
            byte g = (byte)Math.Clamp(Math.Round(picker.Green), 0, 255);
            byte b = (byte)Math.Clamp(Math.Round(picker.Blue), 0, 255);
            picker.Sync(Color.FromArgb(picker.SelectedColor.A, r, g, b), ColorSource.Rgb);
        }
    }

    private static void OnHexTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPicker picker = (ColorPicker)d;
        if (!picker.syncing && TryParseHex((string)e.NewValue, out Color color)) {
            picker.Sync(color, ColorSource.Hex);
        }
    }

    /// <summary>Pushes <paramref name="color"/> to every representation except the one it came from.</summary>
    private void Sync(Color color, ColorSource source) {
        syncing = true;
        try {
            SetCurrentValue(SelectedColorProperty, color);
            SolidColorBrush brush = new(color);
            brush.Freeze();
            SetCurrentValue(SelectedBrushProperty, brush);
            if (source != ColorSource.Hsv) {
                (double hue, double saturation, double brightness) = ToHsv(color);
                // Greys and black have no hue; keep the current one so the canvas doesn't jump to red.
                if ((saturation > 0) && (brightness > 0)) {
                    SetCurrentValue(HueProperty, hue);
                }
                if (brightness > 0) {
                    SetCurrentValue(SaturationProperty, saturation);
                }
                SetCurrentValue(BrightnessProperty, brightness);
            }
            if (source != ColorSource.Alpha) {
                SetCurrentValue(AlphaProperty, (double)color.A);
            }
            if (source != ColorSource.Rgb) {
                SetCurrentValue(RedProperty, (double)color.R);
                SetCurrentValue(GreenProperty, (double)color.G);
                SetCurrentValue(BlueProperty, (double)color.B);
            }
            if (source != ColorSource.Hex) {
                SetCurrentValue(HexTextProperty, ToHex(color));
            }
            SolidColorBrush hueBrush = new(FromHsv(Hue, 1, 1));
            hueBrush.Freeze();
            SetValue(HueBrushPropertyKey, hueBrush);
            LinearGradientBrush alpha = new(Color.FromArgb(0, color.R, color.G, color.B), Color.FromArgb(255, color.R, color.G, color.B), 0);
            alpha.Freeze();
            SetValue(AlphaGradientPropertyKey, alpha);
        }
        finally {
            syncing = false;
        }
        PlaceThumb();
    }

    private void OnApplyPreset(object sender, ExecutedRoutedEventArgs e) {
        if (e.Parameter is Color preset) {
            Sync(preset, ColorSource.External);
        }
    }

    private void OnSpectrumMouseDown(object sender, MouseButtonEventArgs e) {
        if (spectrum is null) {
            return;
        }
        spectrum.Focus();
        spectrum.CaptureMouse();
        PickFrom(e.GetPosition(spectrum));
        AnimateThumb(1.3, MuzzleEasings.BackKey, 260);
        e.Handled = true;
    }

    private void OnSpectrumMouseMove(object sender, MouseEventArgs e) {
        if ((spectrum is not null) && spectrum.IsMouseCaptured) {
            PickFrom(e.GetPosition(spectrum));
        }
    }

    private void OnSpectrumMouseUp(object sender, MouseButtonEventArgs e) {
        if ((spectrum is not null) && spectrum.IsMouseCaptured) {
            spectrum.ReleaseMouseCapture();
            AnimateThumb(1, MuzzleEasings.JellyKey, 640);
        }
    }

    private void OnSpectrumKeyDown(object sender, KeyEventArgs e) {
        const double step = 0.02;
        switch (e.Key) {
            case Key.Left:
                SetCurrentValue(SaturationProperty, Math.Clamp(Saturation - step, 0, 1));
                break;
            case Key.Right:
                SetCurrentValue(SaturationProperty, Math.Clamp(Saturation + step, 0, 1));
                break;
            case Key.Up:
                SetCurrentValue(BrightnessProperty, Math.Clamp(Brightness + step, 0, 1));
                break;
            case Key.Down:
                SetCurrentValue(BrightnessProperty, Math.Clamp(Brightness - step, 0, 1));
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private void OnSpectrumSizeChanged(object sender, SizeChangedEventArgs e) {
        PlaceThumb();
    }

    private void PickFrom(Point point) {
        if ((spectrum is null) || (spectrum.ActualWidth <= 0) || (spectrum.ActualHeight <= 0)) {
            return;
        }
        double saturation = Math.Clamp(point.X / spectrum.ActualWidth, 0, 1);
        double brightness = 1 - Math.Clamp(point.Y / spectrum.ActualHeight, 0, 1);
        syncing = true;
        SetCurrentValue(SaturationProperty, saturation);
        SetCurrentValue(BrightnessProperty, brightness);
        syncing = false;
        Sync(FromHsv(Hue, saturation, brightness, SelectedColor.A), ColorSource.Hsv);
    }

    private void PlaceThumb() {
        if ((spectrum is null) || (spectrumThumb is null)) {
            return;
        }
        double x = (Saturation * spectrum.ActualWidth) - (spectrumThumb.Width / 2);
        double y = ((1 - Brightness) * spectrum.ActualHeight) - (spectrumThumb.Height / 2);
        Canvas.SetLeft(spectrumThumb, double.IsFinite(x) ? x : 0);
        Canvas.SetTop(spectrumThumb, double.IsFinite(y) ? y : 0);
    }

    private void AnimateThumb(double to, string easingKey, double milliseconds) {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        DoubleAnimation animation = new(to, TimeSpan.FromMilliseconds(milliseconds)) { EasingFunction = MuzzleEasings.Get(this, easingKey) };
        thumbScale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
        thumbScale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
    }
}