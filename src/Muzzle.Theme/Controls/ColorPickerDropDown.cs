using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A compact colour field (swatch + hex) that opens a full <see cref="ColorPicker"/> in a springy dropdown card.
/// <see cref="SelectedColor"/> / <see cref="SelectedBrush"/> are two-way bindable and update live while picking.
/// </summary>
public class ColorPickerDropDown : Control {
    /// <summary>Identifies the <see cref="SelectedColor"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
        nameof(SelectedColor), typeof(Color), typeof(ColorPickerDropDown),
        new FrameworkPropertyMetadata(Color.FromRgb(0xF4, 0x97, 0xA9), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedColorChanged));

    /// <summary>Identifies the <see cref="SelectedBrush"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedBrushProperty = DependencyProperty.Register(
        nameof(SelectedBrush), typeof(SolidColorBrush), typeof(ColorPickerDropDown),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedBrushChanged));

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(ColorPickerDropDown), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="ShowAlpha"/> dependency property.</summary>
    public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
        nameof(ShowAlpha), typeof(bool), typeof(ColorPickerDropDown), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="Presets"/> dependency property.</summary>
    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(
        nameof(Presets), typeof(IList<Color>), typeof(ColorPickerDropDown), new PropertyMetadata(null, OnPresetsChanged));

    private static readonly DependencyPropertyKey HexTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HexText), typeof(string), typeof(ColorPickerDropDown), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="HexText"/> read-only dependency property.</summary>
    public static readonly DependencyProperty HexTextProperty = HexTextPropertyKey.DependencyProperty;

    private ColorPicker? picker;
    private bool syncing;

    static ColorPickerDropDown() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ColorPickerDropDown), new FrameworkPropertyMetadata(typeof(ColorPickerDropDown)));
    }

    /// <summary>Initializes a new instance of the <see cref="ColorPickerDropDown"/> class.</summary>
    public ColorPickerDropDown() {
        Sync(SelectedColor);
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

    /// <summary>Gets or sets whether the drop down is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsDropDownOpen {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Gets or sets whether to show the alpha. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowAlpha {
        get => (bool)GetValue(ShowAlphaProperty);
        set => SetValue(ShowAlphaProperty, value);
    }

    /// <summary>Preset swatches for the inner picker; null uses the ColorPicker's pastel defaults.</summary>
    public IList<Color>? Presets {
        get => (IList<Color>?)GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    /// <summary>Gets the hex text. This is a read-only dependency property.</summary>
    public string HexText => (string)GetValue(HexTextProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        picker = GetTemplateChild("Picker") as ColorPicker;
        ApplyPresets();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (e.Handled) {
            return;
        }
        if ((e.Key == Key.Down) && ((Keyboard.Modifiers & ModifierKeys.Alt) != 0)) {
            SetCurrentValue(IsDropDownOpenProperty, true);
            e.Handled = true;
        }
        else if ((e.Key == Key.Escape) && IsDropDownOpen) {
            SetCurrentValue(IsDropDownOpenProperty, false);
            e.Handled = true;
        }
    }

    private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((ColorPickerDropDown)d).Sync((Color)e.NewValue);
    }

    private static void OnSelectedBrushChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ColorPickerDropDown dropDown = (ColorPickerDropDown)d;
        if (!dropDown.syncing && (e.NewValue is SolidColorBrush brush)) {
            dropDown.SetCurrentValue(SelectedColorProperty, brush.Color);
        }
    }

    private static void OnPresetsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((ColorPickerDropDown)d).ApplyPresets();
    }

    private void ApplyPresets() {
        if (picker is null) {
            return;
        }
        if (Presets is null) {
            picker.ClearValue(ColorPicker.PresetsProperty);
        }
        else {
            picker.Presets = Presets;
        }
    }

    private void Sync(Color color) {
        syncing = true;
        SolidColorBrush brush = new(color);
        brush.Freeze();
        SetCurrentValue(SelectedBrushProperty, brush);
        SetValue(HexTextPropertyKey, color.A == 255 ? $"#{color.R:X2}{color.G:X2}{color.B:X2}" : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
        syncing = false;
    }
}