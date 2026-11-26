using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>One reflected, editable property of the object a <see cref="PropertyGrid"/> inspects.</summary>
public sealed class PropertyItem : INotifyPropertyChanged {
    private static readonly IReadOnlyList<Color> PastelPresets = [
        Color.FromRgb(0xF4, 0x97, 0xA9),
        Color.FromRgb(0xF7, 0xAB, 0xB9),
        Color.FromRgb(0xF9, 0xC3, 0xA5),
        Color.FromRgb(0xF8, 0xDE, 0x9B),
        Color.FromRgb(0x9F, 0xD8, 0xCB),
        Color.FromRgb(0xA9, 0xD3, 0xF0),
        Color.FromRgb(0xB8, 0xA9, 0xE6),
        Color.FromRgb(0xDD, 0xD5, 0xF5),
        Color.FromRgb(0xFD, 0xE7, 0xEC),
        Color.FromRgb(0xE3, 0xF4, 0xF0),
        Color.FromRgb(0xFA, 0xF8, 0xF6),
        Color.FromRgb(0xFF, 0xFF, 0xFF),
        Color.FromRgb(0xEF, 0xEC, 0xE8),
        Color.FromRgb(0x78, 0x73, 0x85),
        Color.FromRgb(0x2E, 0x2B, 0x36),
        Color.FromRgb(0xF2, 0x8B, 0x82)
    ];

    private readonly object component;
    private readonly PropertyDescriptor descriptor;
    private readonly Type valueType;
    private readonly bool isNullable;
    private readonly bool hasRange;
    private readonly double? step;
    private readonly string? valueFormat;
    private string editText;
    private string? errorText;
    private bool hasError;
    private bool isSelected;
    private bool isVisible = true;
    private bool isCommitting;
    private bool isAttached;

    internal PropertyItem(object target, PropertyDescriptor propertyDescriptor) {
        component = target;
        descriptor = propertyDescriptor;
        PropertyType = propertyDescriptor.PropertyType;
        Type? underlying = Nullable.GetUnderlyingType(PropertyType);
        isNullable = underlying is not null;
        valueType = underlying ?? PropertyType;

        Name = propertyDescriptor.Name;
        DisplayName = string.IsNullOrWhiteSpace(propertyDescriptor.DisplayName) ? propertyDescriptor.Name : propertyDescriptor.DisplayName;
        Category = string.IsNullOrWhiteSpace(propertyDescriptor.Category) ? "Misc" : propertyDescriptor.Category;
        Description = string.IsNullOrWhiteSpace(propertyDescriptor.Description) ? string.Empty : propertyDescriptor.Description;
        Order = (propertyDescriptor.Attributes[typeof(DisplayAttribute)] as DisplayAttribute)?.GetOrder();
        TypeName = isNullable ? valueType.Name + "?" : valueType.Name;

        if ((propertyDescriptor.Attributes[typeof(RangeAttribute)] is RangeAttribute range) && IsNumericType(valueType)) {
            hasRange = true;
            Minimum = System.Convert.ToDouble(range.Minimum, CultureInfo.InvariantCulture);
            Maximum = System.Convert.ToDouble(range.Maximum, CultureInfo.InvariantCulture);
        }
        else {
            Minimum = double.MinValue;
            Maximum = double.MaxValue;
        }
        bool integral = IsIntegerType(valueType);
        IsIntegral = integral;
        if ((propertyDescriptor.Attributes[typeof(PropertyStepAttribute)] is PropertyStepAttribute stepAttribute) && IsNumericType(valueType)) {
            if (double.IsFinite(stepAttribute.Step) && (stepAttribute.Step > 0)) {
                step = integral ? Math.Max(1, Math.Round(stepAttribute.Step)) : stepAttribute.Step;
            }
            valueFormat = string.IsNullOrEmpty(stepAttribute.Format) ? null : stepAttribute.Format;
        }
        SmallChange = step ?? (integral ? 1 : (hasRange ? Math.Max(0.01, Math.Round((Maximum - Minimum) / 50, 2)) : 1));
        LargeChange = hasRange ? Math.Max(SmallChange, (Maximum - Minimum) / 10) : SmallChange * 10;

        Editor = CreateEditor(propertyDescriptor);
        EditorKind = Editor is null ? ResolveKind() : PropertyEditorKind.Custom;
        EnumValues = valueType.IsEnum ? Enum.GetValues(valueType).Cast<object>().ToArray() : [];

        IncrementCommand = new MuzzleCommand(_ => Step(1));
        DecrementCommand = new MuzzleCommand(_ => Step(-1));
        ApplyColorCommand = new MuzzleCommand(ApplyColor);
        ResetCommand = new MuzzleCommand(_ => Reset());

        editText = Format(Value);
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Gets the CLR property name.</summary>
    public string Name { get; }

    /// <summary>Gets the label shown (from <c>[DisplayName]</c>, or the property name).</summary>
    public string DisplayName { get; }

    /// <summary>Gets the category (from <c>[Category]</c>, or "Misc").</summary>
    public string Category { get; }

    /// <summary>Gets the description shown in the grid's footer (from <c>[Description]</c>), or an empty string when none is declared.</summary>
    public string Description { get; }

    /// <summary>Gets the descriptor of the edited property, for reading custom attributes.</summary>
    public PropertyDescriptor Descriptor => descriptor;

    /// <summary>Gets the object whose property is edited.</summary>
    public object Component => component;

    /// <summary>Gets the custom editor named by the property's <c>[Editor]</c> attribute, or <see langword="null"/>.</summary>
    public PropertyEditorBase? Editor { get; }

    /// <summary>Gets the position from <c>[Display(Order = n)]</c>, or <see langword="null"/> when none is declared.</summary>
    internal int? Order { get; }

    /// <summary>Gets the property's CLR type.</summary>
    public Type PropertyType { get; }

    /// <summary>Gets a friendly type name shown beside the editor.</summary>
    public string TypeName { get; }

    /// <summary>Gets which editor the grid shows for this property.</summary>
    public PropertyEditorKind EditorKind { get; }

    /// <summary>Gets the enum's values (empty for other types).</summary>
    public IReadOnlyList<object> EnumValues { get; }

    /// <summary>Gets the pastel preset swatches offered by colour editors.</summary>
    public IReadOnlyList<Color> PresetColors => PastelPresets;

    /// <summary>Gets the smallest allowed value for numeric editors (from <c>[Range]</c> or the type).</summary>
    public double Minimum { get; }

    /// <summary>Gets the largest allowed value for numeric editors (from <c>[Range]</c> or the type).</summary>
    public double Maximum { get; }

    /// <summary>Gets the step used by the numeric steppers and arrow keys.</summary>
    public double SmallChange { get; }

    /// <summary>Gets the larger step used by Page Up / Page Down.</summary>
    public double LargeChange { get; }

    /// <summary>Gets whether the value is a whole number.</summary>
    public bool IsIntegral { get; }

    /// <summary>Gets whether values snap to <see cref="SmallChange"/> (whole numbers, or a <see cref="PropertyStepAttribute"/>).</summary>
    public bool SnapsToStep => IsIntegral || (step is not null);

    /// <summary>Gets whether a <see cref="PropertyStepAttribute"/> gives the value a display format.</summary>
    public bool HasValueFormat => valueFormat is not null;

    /// <summary>Gets the value formatted with the <see cref="PropertyStepAttribute.Format"/>, or as plain text without one.</summary>
    public string FormattedValue {
        get {
            if ((valueFormat is null) || (Value is not IFormattable formattable)) {
                return Format(Value);
            }
            try {
                return formattable.ToString(valueFormat, CultureInfo.CurrentCulture);
            }
            catch (FormatException) {
                return Format(Value);
            }
        }
    }

    /// <summary>Gets whether the value differs from its default, so <see cref="ResetCommand"/> would change it.</summary>
    public bool CanReset {
        get {
            try {
                return descriptor.CanResetValue(component);
            }
            catch (Exception) {
                return false;
            }
        }
    }

    /// <summary>Gets the command that restores the property's default value (<c>[DefaultValue]</c> or a <c>ResetXxx</c> method).</summary>
    public ICommand ResetCommand { get; }

    /// <summary>Gets the command that raises a numeric value by <see cref="SmallChange"/>.</summary>
    public ICommand IncrementCommand { get; }

    /// <summary>Gets the command that lowers a numeric value by <see cref="SmallChange"/>.</summary>
    public ICommand DecrementCommand { get; }

    /// <summary>Gets the command that applies a preset colour (the parameter is the <see cref="System.Windows.Media.Color"/>).</summary>
    public ICommand ApplyColorCommand { get; }

    /// <summary>The live value on the inspected object.</summary>
    public object? Value {
        get {
            try {
                return descriptor.GetValue(component);
            }
            catch (Exception) {
                return null;
            }
        }
        set => Commit(value, true);
    }

    /// <summary>The value as a double, for sliders and spin buttons.</summary>
    public double NumericValue {
        get {
            try {
                return Value is null ? 0 : System.Convert.ToDouble(Value, CultureInfo.InvariantCulture);
            }
            catch (Exception) {
                return 0;
            }
        }
        set => Commit(ToNumeric(value), true);
    }

    /// <summary>The value as text; parsed through the property's TypeConverter as the user types.</summary>
    public string EditText {
        get => editText;
        set {
            string text = value ?? string.Empty;
            if (text == editText) {
                return;
            }
            editText = text;
            OnPropertyChanged();
            if (TryParse(text, out object? parsed, out string? error)) {
                Commit(parsed, false);
            }
            else {
                SetError(error);
            }
        }
    }

    /// <summary>Gets a brush preview of the current colour value.</summary>
    public Brush SwatchBrush {
        get {
            object? value = Value;
            if (value is Color color) {
                return CreateBrush(color);
            }
            return value as Brush ?? Brushes.Transparent;
        }
    }

    /// <summary>Gets whether the last edit was invalid.</summary>
    public bool HasError {
        get => hasError;
        private set => SetField(ref hasError, value);
    }

    /// <summary>Gets the message explaining the last invalid edit.</summary>
    public string? ErrorText {
        get => errorText;
        private set => SetField(ref errorText, value);
    }

    /// <summary>Gets or sets whether this row is selected (its description shows in the footer).</summary>
    public bool IsSelected {
        get => isSelected;
        internal set => SetField(ref isSelected, value);
    }

    /// <summary>Gets whether this property matches the current search.</summary>
    public bool IsVisible {
        get => isVisible;
        internal set => SetField(ref isVisible, value);
    }

    internal bool Matches(string query) {
        return (query.Length == 0)
            || DisplayName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || Name.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || Category.Contains(query, StringComparison.CurrentCultureIgnoreCase)
            || TypeName.Contains(query, StringComparison.CurrentCultureIgnoreCase);
    }

    internal void Attach() {
        if (isAttached || !descriptor.SupportsChangeEvents) {
            return;
        }
        descriptor.AddValueChanged(component, OnComponentValueChanged);
        isAttached = true;
    }

    internal void Detach() {
        if (!isAttached) {
            return;
        }
        descriptor.RemoveValueChanged(component, OnComponentValueChanged);
        isAttached = false;
    }

    private void OnComponentValueChanged(object? sender, EventArgs e) {
        Refresh(!isCommitting);
    }

    private void Refresh(bool includeText) {
        if (includeText) {
            string formatted = Format(Value);
            if (formatted != editText) {
                editText = formatted;
                OnPropertyChanged(nameof(EditText));
            }
            ClearError();
        }
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(NumericValue));
        OnPropertyChanged(nameof(SwatchBrush));
        OnPropertyChanged(nameof(FormattedValue));
        OnPropertyChanged(nameof(CanReset));
    }

    private void Reset() {
        if (!CanReset) {
            return;
        }
        try {
            descriptor.ResetValue(component);
            ClearError();
        }
        catch (Exception exception) {
            SetError(exception.InnerException?.Message ?? exception.Message);
        }
        Refresh(true);
    }

    private static PropertyEditorBase? CreateEditor(PropertyDescriptor propertyDescriptor) {
        foreach (Attribute attribute in propertyDescriptor.Attributes) {
            if ((attribute is not EditorAttribute editor) || (editor.EditorBaseTypeName is not string baseName)
                || (Type.GetType(baseName) != typeof(PropertyEditorBase))) {
                continue;
            }
            try {
                if ((Type.GetType(editor.EditorTypeName) is Type type) && (Activator.CreateInstance(type) is PropertyEditorBase instance)) {
                    return instance;
                }
            }
            catch (Exception) {
                // An editor that cannot be created falls back to the built-in one for the type.
            }
        }
        return null;
    }

    private void Commit(object? newValue, bool refreshText) {
        isCommitting = !refreshText;
        try {
            descriptor.SetValue(component, newValue);
            ClearError();
        }
        catch (Exception exception) {
            SetError(exception.InnerException?.Message ?? exception.Message);
        }
        finally {
            isCommitting = false;
        }
        Refresh(refreshText);
    }

    private void SetError(string? message) {
        ErrorText = message ?? "That value doesn't fit here.";
        HasError = true;
    }

    private void ClearError() {
        HasError = false;
        ErrorText = null;
    }

    private void Step(int direction) {
        Commit(ToNumeric(NumericValue + (direction * SmallChange)), true);
    }

    private void ApplyColor(object? parameter) {
        if (parameter is not Color color) {
            return;
        }
        Commit(EditorKind == PropertyEditorKind.Color ? color : CreateBrush(color), true);
    }

    private object? ToNumeric(double value) {
        double number = value;
        if (step is double size) {
            double origin = hasRange ? Minimum : 0;
            number = origin + (Math.Round((number - origin) / size) * size);
        }
        number = hasRange ? Math.Clamp(number, Minimum, Maximum) : number;
        number = IsIntegral ? Math.Round(number) : Math.Round(number, 6);
        try {
            return System.Convert.ChangeType(number, valueType, CultureInfo.InvariantCulture);
        }
        catch (OverflowException) {
            return Value;
        }
    }

    private bool TryParse(string text, out object? result, out string? error) {
        result = null;
        error = null;
        string trimmed = text.Trim();
        try {
            if ((valueType == typeof(string)) || (valueType == typeof(object))) {
                result = text;
                return true;
            }
            if (isNullable && (trimmed.Length == 0)) {
                return true;
            }
            if ((EditorKind == PropertyEditorKind.Color) || (EditorKind == PropertyEditorKind.Brush)) {
                if ((trimmed.Length > 0) && (trimmed[0] != '#') && trimmed.All(Uri.IsHexDigit)) {
                    trimmed = "#" + trimmed;
                }
                if (ColorConverter.ConvertFromString(trimmed) is not Color color) {
                    error = "Enter a hex colour such as #F497A9.";
                    return false;
                }
                result = EditorKind == PropertyEditorKind.Color ? color : CreateBrush(color);
                return true;
            }
            TypeConverter converter = TypeDescriptor.GetConverter(valueType);
            result = converter.ConvertFromString(null, CultureInfo.CurrentCulture, trimmed);
            if (hasRange && (result is not null)) {
                double number = System.Convert.ToDouble(result, CultureInfo.InvariantCulture);
                if ((number < Minimum) || (number > Maximum)) {
                    error = string.Create(CultureInfo.CurrentCulture, $"Keep it between {Minimum} and {Maximum}.");
                    return false;
                }
            }
            return true;
        }
        catch (Exception exception) {
            error = exception.InnerException?.Message ?? exception.Message;
            return false;
        }
    }

    private string Format(object? value) {
        try {
            return value switch {
                null => string.Empty,
                string text => text,
                Color color => ToHex(color),
                SolidColorBrush brush => ToHex(brush.Color),
                Brush brush => brush.GetType().Name,
                double number => number.ToString("0.###", CultureInfo.CurrentCulture),
                float number => number.ToString("0.###", CultureInfo.CurrentCulture),
                decimal number => number.ToString("0.###", CultureInfo.CurrentCulture),
                _ => TypeDescriptor.GetConverter(valueType).ConvertToString(null, CultureInfo.CurrentCulture, value) ?? value.ToString() ?? string.Empty
            };
        }
        catch (Exception) {
            return value?.ToString() ?? string.Empty;
        }
    }

    private PropertyEditorKind ResolveKind() {
        if ((valueType == typeof(string)) || (valueType == typeof(object))) {
            return PropertyEditorKind.Text;
        }
        if (valueType == typeof(bool)) {
            return isNullable ? PropertyEditorKind.NullableBoolean : PropertyEditorKind.Boolean;
        }
        if (valueType.IsEnum) {
            return valueType.IsDefined(typeof(FlagsAttribute), false) ? PropertyEditorKind.Text : PropertyEditorKind.Enum;
        }
        if (IsNumericType(valueType)) {
            return hasRange ? PropertyEditorKind.Range : PropertyEditorKind.Numeric;
        }
        if (valueType == typeof(Color)) {
            return PropertyEditorKind.Color;
        }
        if (typeof(Brush).IsAssignableFrom(valueType)) {
            return PropertyEditorKind.Brush;
        }
        return TypeDescriptor.GetConverter(valueType).CanConvertFrom(typeof(string)) ? PropertyEditorKind.Text : PropertyEditorKind.ReadOnly;
    }

    private static bool IsNumericType(Type type) {
        return IsIntegerType(type) || (type == typeof(double)) || (type == typeof(float)) || (type == typeof(decimal));
    }

    private static bool IsIntegerType(Type type) {
        return (type == typeof(int)) || (type == typeof(long)) || (type == typeof(short)) || (type == typeof(byte))
            || (type == typeof(uint)) || (type == typeof(ulong)) || (type == typeof(ushort)) || (type == typeof(sbyte));
    }

    private static SolidColorBrush CreateBrush(Color color) {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    private static string ToHex(Color color) {
        return color.A == 255
            ? $"#{color.R:X2}{color.G:X2}{color.B:X2}"
            : $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return;
        }
        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}