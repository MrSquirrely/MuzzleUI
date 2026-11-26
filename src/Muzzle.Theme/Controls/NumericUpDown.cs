using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A numeric field with integrated − / + buttons (they squish in and spring back), arrow-key and mouse-wheel stepping
/// (wheel only while focused, so scrolling a page never changes values by accident), and typed entry committed on
/// Enter or focus loss.
/// </summary>
[TemplatePart(Name = TextBoxPartName, Type = typeof(TextBox))]
public class NumericUpDown : Control {
    /// <summary>Gets the command that raises the value by <see cref="Increment"/> (the + button).</summary>
    public static readonly RoutedCommand IncreaseCommand = new(nameof(IncreaseCommand), typeof(NumericUpDown));
    /// <summary>Gets the command that lowers the value by <see cref="Increment"/> (the − button).</summary>
    public static readonly RoutedCommand DecreaseCommand = new(nameof(DecreaseCommand), typeof(NumericUpDown));

    /// <summary>Identifies the <see cref="ValueChanged"/> routed event.</summary>
    public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(ValueChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<double>), typeof(NumericUpDown));

    /// <summary>Identifies the <see cref="Value"/> dependency property.</summary>
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(NumericUpDown),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValueCallback));

    /// <summary>Identifies the <see cref="Minimum"/> dependency property.</summary>
    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MinValue, OnRangeChanged));

    /// <summary>Identifies the <see cref="Maximum"/> dependency property.</summary>
    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(NumericUpDown), new PropertyMetadata(double.MaxValue, OnRangeChanged));

    /// <summary>Identifies the <see cref="Increment"/> dependency property.</summary>
    public static readonly DependencyProperty IncrementProperty = DependencyProperty.Register(
        nameof(Increment), typeof(double), typeof(NumericUpDown), new PropertyMetadata(1.0));

    /// <summary>Values are rounded to this many decimals; also the default display precision.</summary>
    public static readonly DependencyProperty DecimalPlacesProperty = DependencyProperty.Register(
        nameof(DecimalPlaces), typeof(int), typeof(NumericUpDown), new PropertyMetadata(0, OnFormatChanged), static v => (int)v is >= 0 and <= 15);

    /// <summary>Optional .NET format string for display (e.g. "C2", "0.0 'kg'"). Defaults to F{DecimalPlaces}.</summary>
    public static readonly DependencyProperty ValueFormatProperty = DependencyProperty.Register(
        nameof(ValueFormat), typeof(string), typeof(NumericUpDown), new PropertyMetadata(null, OnFormatChanged));

    /// <summary>Identifies the <see cref="IsReadOnly"/> dependency property.</summary>
    public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
        nameof(IsReadOnly), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(false));

    /// <summary>Hides the − / + buttons (keys and the wheel still step) for tight layouts.</summary>
    public static readonly DependencyProperty ShowSpinButtonsProperty = DependencyProperty.Register(
        nameof(ShowSpinButtons), typeof(bool), typeof(NumericUpDown), new PropertyMetadata(true));

    private const string TextBoxPartName = "PART_TextBox";

    private TextBox? textBox;
    private bool editing;

    static NumericUpDown() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(typeof(NumericUpDown)));
        FocusableProperty.OverrideMetadata(typeof(NumericUpDown), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="NumericUpDown"/> class.</summary>
    public NumericUpDown() {
        CommandBindings.Add(new CommandBinding(IncreaseCommand, OnIncrease, CanIncrease));
        CommandBindings.Add(new CommandBinding(DecreaseCommand, OnDecrease, CanDecrease));
    }

    /// <summary>Occurs when <see cref="Value"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<double> ValueChanged {
        add => AddHandler(ValueChangedEvent, value);
        remove => RemoveHandler(ValueChangedEvent, value);
    }

    /// <summary>Gets or sets the value. The default is <c>0.0</c>. This is a dependency property.</summary>
    public double Value {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    /// <summary>Gets or sets the minimum. The default is <c>double.MinValue</c>. This is a dependency property.</summary>
    public double Minimum {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    /// <summary>Gets or sets the maximum. The default is <c>double.MaxValue</c>. This is a dependency property.</summary>
    public double Maximum {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    /// <summary>Gets or sets the increment. The default is <c>1.0</c>. This is a dependency property.</summary>
    public double Increment {
        get => (double)GetValue(IncrementProperty);
        set => SetValue(IncrementProperty, value);
    }

    /// <summary>Gets or sets the decimal places. The default is <c>0</c>. This is a dependency property.</summary>
    public int DecimalPlaces {
        get => (int)GetValue(DecimalPlacesProperty);
        set => SetValue(DecimalPlacesProperty, value);
    }

    /// <summary>Gets or sets the value format. The default is <see langword="null"/>. This is a dependency property.</summary>
    public string? ValueFormat {
        get => (string?)GetValue(ValueFormatProperty);
        set => SetValue(ValueFormatProperty, value);
    }

    /// <summary>Gets or sets whether the value is read-only. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsReadOnly {
        get => (bool)GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Gets or sets whether to show the spin buttons. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowSpinButtons {
        get => (bool)GetValue(ShowSpinButtonsProperty);
        set => SetValue(ShowSpinButtonsProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (textBox is not null) {
            textBox.LostKeyboardFocus -= OnTextBoxLostFocus;
            textBox.GotKeyboardFocus -= OnTextBoxGotFocus;
        }
        base.OnApplyTemplate();
        textBox = GetTemplateChild(TextBoxPartName) as TextBox;
        if (textBox is not null) {
            textBox.LostKeyboardFocus += OnTextBoxLostFocus;
            textBox.GotKeyboardFocus += OnTextBoxGotFocus;
        }
        UpdateText();
    }

    /// <summary>Steps the value by <paramref name="steps"/> × <see cref="Increment"/>.</summary>
    public void Step(int steps) {
        if (!IsReadOnly) {
            SetCurrentValue(ValueProperty, Value + (steps * Increment));
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e) {
        base.OnPreviewKeyDown(e);
        switch (e.Key) {
            case Key.Up:
                Commit();
                Step(1);
                e.Handled = true;
                break;
            case Key.Down:
                Commit();
                Step(-1);
                e.Handled = true;
                break;
            case Key.PageUp:
                Commit();
                Step(10);
                e.Handled = true;
                break;
            case Key.PageDown:
                Commit();
                Step(-10);
                e.Handled = true;
                break;
            case Key.Enter:
                Commit();
                textBox?.SelectAll();
                e.Handled = true;
                break;
            case Key.Escape:
                UpdateText();
                textBox?.SelectAll();
                break;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e) {
        base.OnPreviewMouseWheel(e);
        if (IsKeyboardFocusWithin && !IsReadOnly) {
            Commit();
            Step(e.Delta > 0 ? 1 : -1);
            e.Handled = true;
        }
    }

    private static object CoerceValueCallback(DependencyObject d, object value) {
        NumericUpDown control = (NumericUpDown)d;
        double number = (double)value;
        if (double.IsNaN(number)) {
            return control.Minimum > double.MinValue ? control.Minimum : 0.0;
        }
        double clamped = Math.Clamp(number, Math.Min(control.Minimum, control.Maximum), control.Maximum);
        return Math.Round(clamped, control.DecimalPlaces, MidpointRounding.AwayFromZero);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        NumericUpDown control = (NumericUpDown)d;
        control.UpdateText();
        control.RaiseEvent(new RoutedPropertyChangedEventArgs<double>((double)e.OldValue, (double)e.NewValue, ValueChangedEvent));
        CommandManager.InvalidateRequerySuggested();
    }

    private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        d.CoerceValue(ValueProperty);
        CommandManager.InvalidateRequerySuggested();
    }

    private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        d.CoerceValue(ValueProperty);
        ((NumericUpDown)d).UpdateText();
    }

    private void OnTextBoxGotFocus(object sender, KeyboardFocusChangedEventArgs e) {
        // Show the raw number while editing so formats like "0.0 kg" don't get in the way.
        editing = true;
        UpdateText();
        textBox?.SelectAll();
    }

    private void OnTextBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e) {
        Commit();
        editing = false;
        UpdateText();
    }

    private void Commit() {
        // Only typed input is parsed; the formatted display text (e.g. "$5.00") is never read back.
        if ((textBox is null) || IsReadOnly || !editing) {
            return;
        }
        string text = textBox.Text.Trim();
        if (text.Length == 0) {
            UpdateText();
            return;
        }
        if (double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out double parsed)) {
            SetCurrentValue(ValueProperty, parsed);
            UpdateText();
            return;
        }
        // Not a number: snap back and give a little "nope" wobble.
        Jelly.Wobble(textBox);
        UpdateText();
    }

    private void UpdateText() {
        if (textBox is null) {
            return;
        }
        string raw = "F" + DecimalPlaces.ToString(CultureInfo.InvariantCulture);
        // While editing, show the plain number so formats like "0.0 'kg'" don't get in the way of typing.
        string format = editing || string.IsNullOrWhiteSpace(ValueFormat) ? raw : ValueFormat;
        try {
            textBox.Text = Value.ToString(format, CultureInfo.CurrentCulture);
        }
        catch (FormatException) {
            textBox.Text = Value.ToString(CultureInfo.CurrentCulture);
        }
    }

    private void CanIncrease(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = !IsReadOnly && (Value < Maximum);
    }

    private void CanDecrease(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = !IsReadOnly && (Value > Minimum);
    }

    private void OnIncrease(object sender, ExecutedRoutedEventArgs e) {
        Commit();
        Step(1);
    }

    private void OnDecrease(object sender, ExecutedRoutedEventArgs e) {
        Commit();
        Step(-1);
    }
}