using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A time field whose dropdown has scrollable hour / minute (/ AM-PM) columns. Picking a slot bounces a blush pill
/// into place and updates <see cref="SelectedTime"/> immediately.
/// </summary>
[TemplatePart(Name = HourListPartName, Type = typeof(ListBox))]
[TemplatePart(Name = MinuteListPartName, Type = typeof(ListBox))]
[TemplatePart(Name = PeriodListPartName, Type = typeof(ListBox))]
public class TimePicker : Control {
    /// <summary>Identifies the <see cref="SelectedTimeChanged"/> routed event.</summary>
    public static readonly RoutedEvent SelectedTimeChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(SelectedTimeChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<TimeSpan?>), typeof(TimePicker));

    /// <summary>Identifies the <see cref="SelectedTime"/> dependency property.</summary>
    public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(
        nameof(SelectedTime), typeof(TimeSpan?), typeof(TimePicker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeChanged, CoerceSelectedTime));

    /// <summary>Identifies the <see cref="Is24HourClock"/> dependency property.</summary>
    public static readonly DependencyProperty Is24HourClockProperty = DependencyProperty.Register(
        nameof(Is24HourClock), typeof(bool), typeof(TimePicker), new PropertyMetadata(false, OnClockLayoutChanged));

    /// <summary>Identifies the <see cref="MinuteIncrement"/> dependency property.</summary>
    public static readonly DependencyProperty MinuteIncrementProperty = DependencyProperty.Register(
        nameof(MinuteIncrement), typeof(int), typeof(TimePicker), new PropertyMetadata(5, OnClockLayoutChanged), static v => (int)v is >= 1 and <= 30);

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(TimePicker),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

    /// <summary>Identifies the <see cref="Placeholder"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(TimePicker), new PropertyMetadata("Pick a time"));

    private static readonly DependencyPropertyKey DisplayTextPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(DisplayText), typeof(string), typeof(TimePicker), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="DisplayText"/> read-only dependency property.</summary>
    public static readonly DependencyProperty DisplayTextProperty = DisplayTextPropertyKey.DependencyProperty;

    private const string HourListPartName = "PART_HourList";
    private const string MinuteListPartName = "PART_MinuteList";
    private const string PeriodListPartName = "PART_PeriodList";
    private const string Am = "AM";
    private const string Pm = "PM";

    private ListBox? hourList;
    private ListBox? minuteList;
    private ListBox? periodList;
    private bool syncing;

    static TimePicker() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TimePicker), new FrameworkPropertyMetadata(typeof(TimePicker)));
    }

    /// <summary>Occurs when <see cref="SelectedTime"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<TimeSpan?> SelectedTimeChanged {
        add => AddHandler(SelectedTimeChangedEvent, value);
        remove => RemoveHandler(SelectedTimeChangedEvent, value);
    }

    /// <summary>Time of day (00:00–23:59), or null when nothing is picked.</summary>
    public TimeSpan? SelectedTime {
        get => (TimeSpan?)GetValue(SelectedTimeProperty);
        set => SetValue(SelectedTimeProperty, value);
    }

    /// <summary>Gets or sets whether times use the 24-hour clock. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool Is24HourClock {
        get => (bool)GetValue(Is24HourClockProperty);
        set => SetValue(Is24HourClockProperty, value);
    }

    /// <summary>Gets or sets the minute increment. The default is <c>5</c>. This is a dependency property.</summary>
    public int MinuteIncrement {
        get => (int)GetValue(MinuteIncrementProperty);
        set => SetValue(MinuteIncrementProperty, value);
    }

    /// <summary>Gets or sets whether the drop down is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsDropDownOpen {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Gets or sets the placeholder. The default is <c>&quot;Pick a time&quot;</c>. This is a dependency property.</summary>
    public string? Placeholder {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Gets the display text. This is a read-only dependency property.</summary>
    public string DisplayText => (string)GetValue(DisplayTextProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        foreach (ListBox? list in new[] { hourList, minuteList, periodList }) {
            if (list is not null) {
                list.SelectionChanged -= OnSlotSelected;
                list.PreviewKeyDown -= OnListKeyDown;
            }
        }
        base.OnApplyTemplate();
        hourList = GetTemplateChild(HourListPartName) as ListBox;
        minuteList = GetTemplateChild(MinuteListPartName) as ListBox;
        periodList = GetTemplateChild(PeriodListPartName) as ListBox;
        foreach (ListBox? list in new[] { hourList, minuteList, periodList }) {
            if (list is not null) {
                list.SelectionChanged += OnSlotSelected;
                list.PreviewKeyDown += OnListKeyDown;
            }
        }
        PopulateLists();
        SyncLists();
        UpdateDisplayText();
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

    private static object CoerceSelectedTime(DependencyObject d, object? value) {
        // Wrap into a single day so 25:00 becomes 01:00 and -1:00 becomes 23:00.
        if (value is TimeSpan time) {
            return new TimeSpan(((time.Ticks % TimeSpan.TicksPerDay) + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay);
        }
        return value!;
    }

    private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TimePicker picker = (TimePicker)d;
        picker.UpdateDisplayText();
        if (!picker.syncing) {
            picker.SyncLists();
        }
        picker.RaiseEvent(new RoutedPropertyChangedEventArgs<TimeSpan?>((TimeSpan?)e.OldValue, (TimeSpan?)e.NewValue, SelectedTimeChangedEvent));
    }

    private static void OnClockLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TimePicker picker = (TimePicker)d;
        picker.PopulateLists();
        picker.SyncLists();
        picker.UpdateDisplayText();
    }

    private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TimePicker picker = (TimePicker)d;
        if (e.NewValue is true) {
            picker.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(picker.RevealSelection));
        }
    }

    private void PopulateLists() {
        if ((hourList is null) || (minuteList is null) || (periodList is null)) {
            return;
        }
        syncing = true;
        List<string> hours = [];
        if (Is24HourClock) {
            for (int hour = 0; hour < 24; hour++) {
                hours.Add(hour.ToString("00", CultureInfo.InvariantCulture));
            }
        }
        else {
            for (int hour = 1; hour <= 12; hour++) {
                hours.Add(hour.ToString(CultureInfo.InvariantCulture));
            }
        }
        List<string> minutes = [];
        for (int minute = 0; minute < 60; minute += MinuteIncrement) {
            minutes.Add(minute.ToString("00", CultureInfo.InvariantCulture));
        }
        hourList.ItemsSource = hours;
        minuteList.ItemsSource = minutes;
        periodList.ItemsSource = new[] { Am, Pm };
        syncing = false;
    }

    private void SyncLists() {
        if ((hourList is null) || (minuteList is null) || (periodList is null)) {
            return;
        }
        syncing = true;
        TimeSpan? time = SelectedTime;
        if (time is TimeSpan value) {
            int hour = value.Hours;
            hourList.SelectedItem = Is24HourClock
                ? hour.ToString("00", CultureInfo.InvariantCulture)
                : ((hour % 12) == 0 ? 12 : hour % 12).ToString(CultureInfo.InvariantCulture);
            minuteList.SelectedItem = value.Minutes.ToString("00", CultureInfo.InvariantCulture);
            periodList.SelectedItem = hour < 12 ? Am : Pm;
        }
        else {
            hourList.SelectedIndex = -1;
            minuteList.SelectedIndex = -1;
            periodList.SelectedIndex = -1;
        }
        syncing = false;
    }

    private void OnSlotSelected(object sender, SelectionChangedEventArgs e) {
        if (syncing || (hourList?.SelectedItem is not string hourText)) {
            return;
        }
        int hour = int.Parse(hourText, CultureInfo.InvariantCulture);
        int minute = minuteList?.SelectedItem is string minuteText ? int.Parse(minuteText, CultureInfo.InvariantCulture) : SelectedTime?.Minutes ?? 0;
        if (!Is24HourClock) {
            bool pm = periodList?.SelectedItem is string period ? period == Pm : (SelectedTime?.Hours ?? 0) >= 12;
            hour = (hour % 12) + (pm ? 12 : 0);
        }
        syncing = true;
        SetCurrentValue(SelectedTimeProperty, new TimeSpan(hour, minute, 0));
        syncing = false;
        SyncLists();
    }

    private void OnListKeyDown(object sender, KeyEventArgs e) {
        if (e.Key is Key.Enter or Key.Escape) {
            SetCurrentValue(IsDropDownOpenProperty, false);
            Focus();
            e.Handled = true;
        }
    }

    private void RevealSelection() {
        foreach (ListBox? list in new[] { hourList, minuteList, periodList }) {
            if (list?.SelectedItem is object selected) {
                CenterInView(list, selected);
            }
        }
        if ((hourList?.SelectedItem is object item) && (hourList.ItemContainerGenerator.ContainerFromItem(item) is ListBoxItem container)) {
            container.Focus();
        }
        else {
            hourList?.Focus();
        }
    }

    private static void CenterInView(ListBox list, object item) {
        list.ScrollIntoView(item);
        list.UpdateLayout();
        if ((list.ItemContainerGenerator.ContainerFromItem(item) is not FrameworkElement container) || (FindScrollViewer(list) is not ScrollViewer viewer)) {
            return;
        }
        Point offset = container.TransformToAncestor(viewer).Transform(new Point(0, 0));
        double target = viewer.VerticalOffset + offset.Y - ((viewer.ViewportHeight - container.ActualHeight) / 2);
        viewer.ScrollToVerticalOffset(Math.Max(0, target));
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root) {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer viewer) {
                return viewer;
            }
            if (FindScrollViewer(child) is ScrollViewer nested) {
                return nested;
            }
        }
        return null;
    }

    private void UpdateDisplayText() {
        string text = string.Empty;
        if (SelectedTime is TimeSpan time) {
            DateTime moment = DateTime.Today.Add(time);
            text = Is24HourClock ? moment.ToString("HH:mm", CultureInfo.CurrentCulture) : moment.ToString("h:mm ", CultureInfo.CurrentCulture) + (time.Hours < 12 ? Am : Pm);
        }
        SetValue(DisplayTextPropertyKey, text);
    }
}