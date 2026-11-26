using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>An entry in a <see cref="CommandPalette"/>.</summary>
public class CommandPaletteItem {
    /// <summary>Gets or sets the text shown in the list; it is matched first when searching.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional second line.</summary>
    public string? Description { get; set; }

    /// <summary>Shown as a soft badge (e.g. "Page", "Theme", "Action").</summary>
    public string? Category { get; set; }

    /// <summary>Shown as keycaps, e.g. "Ctrl+Shift+P".</summary>
    public string? Shortcut { get; set; }

    /// <summary>Gets or sets the icon shown beside the title. The default is <see cref="MuzzleIconKind.Zap"/>.</summary>
    public MuzzleIconKind Icon { get; set; } = MuzzleIconKind.Zap;

    /// <summary>Extra words that match the search but aren't shown.</summary>
    public string? Keywords { get; set; }

    /// <summary>Gets or sets the command run when the item is invoked (with <see cref="CommandParameter"/>).</summary>
    public ICommand? Command { get; set; }

    /// <summary>Gets or sets the parameter passed to <see cref="Command"/>.</summary>
    public object? CommandParameter { get; set; }

    /// <summary>Anything you want back in <see cref="CommandPalette.ItemInvoked"/>.</summary>
    public object? Tag { get; set; }

    /// <inheritdoc />
    public override string ToString() {
        return Title;
    }
}

/// <summary>Data for <see cref="CommandPalette.ItemInvoked"/>.</summary>
public sealed class CommandPaletteItemInvokedEventArgs(RoutedEvent routedEvent, object source, object item) : RoutedEventArgs(routedEvent, source) {
    /// <summary>The invoked item (a <see cref="CommandPaletteItem"/> or whatever ItemsSource holds).</summary>
    public object Item { get; } = item;
}

/// <summary>
/// A Ctrl+K "spotlight" launcher: a floating search card over a softly dimmed backdrop that pops in with a BackEase.
/// Typing fuzzy-filters <see cref="ItemsSource"/>; ↑ / ↓ move, Enter runs the item's Command (and raises
/// <see cref="ItemInvoked"/>), Esc or a click on the backdrop closes it. Place it over your window's content; its
/// <see cref="ShortcutGesture"/> works anywhere in that window.
/// </summary>
[TemplatePart(Name = SearchBoxPartName, Type = typeof(TextBox))]
[TemplatePart(Name = ResultsPartName, Type = typeof(ListBox))]
[TemplatePart(Name = BackdropPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = CardPartName, Type = typeof(FrameworkElement))]
public class CommandPalette : Control {
    /// <summary>Name of the search TextBox template part.</summary>
    public const string SearchBoxPartName = "PART_SearchBox";
    /// <summary>Name of the ListBox template part that shows the ranked results.</summary>
    public const string ResultsPartName = "PART_Results";
    /// <summary>Name of the dimmed backdrop template part (clicking it closes the palette).</summary>
    public const string BackdropPartName = "PART_Backdrop";
    /// <summary>Name of the floating search card template part (it pops in when the palette opens).</summary>
    public const string CardPartName = "PART_Card";

    /// <summary>Identifies the <see cref="ItemsSource"/> dependency property.</summary>
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(CommandPalette), new PropertyMetadata(null, OnItemsSourceChanged));

    /// <summary>Identifies the <see cref="SearchText"/> dependency property.</summary>
    public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
        nameof(SearchText), typeof(string), typeof(CommandPalette),
        new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSearchTextChanged));

    /// <summary>Identifies the <see cref="Placeholder"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(CommandPalette), new PropertyMetadata("Type a command or search…"));

    /// <summary>Identifies the <see cref="IsOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.Register(
        nameof(IsOpen), typeof(bool), typeof(CommandPalette),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsOpenChanged));

    /// <summary>The window-wide shortcut that toggles the palette (default Ctrl+K; null to disable).</summary>
    public static readonly DependencyProperty ShortcutGestureProperty = DependencyProperty.Register(
        nameof(ShortcutGesture), typeof(KeyGesture), typeof(CommandPalette), new PropertyMetadata(new KeyGesture(Key.K, ModifierKeys.Control)));

    /// <summary>Identifies the <see cref="MaxResults"/> dependency property.</summary>
    public static readonly DependencyProperty MaxResultsProperty = DependencyProperty.Register(
        nameof(MaxResults), typeof(int), typeof(CommandPalette), new PropertyMetadata(50, OnSearchTextChanged));

    private static readonly DependencyPropertyKey ResultsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Results), typeof(ObservableCollection<object>), typeof(CommandPalette), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Results"/> read-only dependency property.</summary>
    public static readonly DependencyProperty ResultsProperty = ResultsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey HasResultsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasResults), typeof(bool), typeof(CommandPalette), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasResults"/> read-only dependency property.</summary>
    public static readonly DependencyProperty HasResultsProperty = HasResultsPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="ItemInvoked"/> routed event.</summary>
    public static readonly RoutedEvent ItemInvokedEvent = EventManager.RegisterRoutedEvent(
        nameof(ItemInvoked), RoutingStrategy.Bubble, typeof(EventHandler<CommandPaletteItemInvokedEventArgs>), typeof(CommandPalette));

    private readonly ObservableCollection<object> results = [];
    private TextBox? searchBox;
    private ListBox? resultsList;
    private FrameworkElement? backdrop;
    private FrameworkElement? card;
    private Window? hostWindow;
    private IInputElement? restoreFocus;
    private INotifyCollectionChanged? observedSource;

    static CommandPalette() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CommandPalette), new FrameworkPropertyMetadata(typeof(CommandPalette)));
        VisibilityProperty.OverrideMetadata(typeof(CommandPalette), new FrameworkPropertyMetadata(Visibility.Collapsed));
        FocusableProperty.OverrideMetadata(typeof(CommandPalette), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="CommandPalette"/> class.</summary>
    public CommandPalette() {
        SetValue(ResultsPropertyKey, results);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Occurs after an item is run (Enter or a click): the palette has closed and the item's command has executed.</summary>
    public event EventHandler<CommandPaletteItemInvokedEventArgs> ItemInvoked {
        add => AddHandler(ItemInvokedEvent, value);
        remove => RemoveHandler(ItemInvokedEvent, value);
    }

    /// <summary>Gets or sets the items source. The default is <see langword="null"/>. This is a dependency property.</summary>
    public IEnumerable? ItemsSource {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>Gets or sets the search text. The default is an empty string. This is a dependency property.</summary>
    public string SearchText {
        get => (string)GetValue(SearchTextProperty);
        set => SetValue(SearchTextProperty, value);
    }

    /// <summary>Gets or sets the placeholder. The default is <c>&quot;Type a command or search…&quot;</c>. This is a dependency property.</summary>
    public string Placeholder {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Gets or sets whether the control is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsOpen {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>Gets or sets the shortcut gesture. The default is <c>KeyGesture(Key.K, ModifierKeys.Control)</c>. This is a dependency property.</summary>
    public KeyGesture? ShortcutGesture {
        get => (KeyGesture?)GetValue(ShortcutGestureProperty);
        set => SetValue(ShortcutGestureProperty, value);
    }

    /// <summary>Gets or sets the max results. The default is <c>50</c>. This is a dependency property.</summary>
    public int MaxResults {
        get => (int)GetValue(MaxResultsProperty);
        set => SetValue(MaxResultsProperty, value);
    }

    /// <summary>The filtered, ranked items currently shown.</summary>
    public ObservableCollection<object> Results => (ObservableCollection<object>)GetValue(ResultsProperty);

    /// <summary>Gets whether it has results. This is a read-only dependency property.</summary>
    public bool HasResults => (bool)GetValue(HasResultsProperty);

    /// <summary>
    /// Fuzzy score of <paramref name="query"/> against <paramref name="text"/>: null when the query's characters don't
    /// all appear in order; higher for prefix matches, word starts and consecutive runs.
    /// </summary>
    public static int? FuzzyScore(string query, string? text) {
        if (string.IsNullOrEmpty(query)) {
            return 0;
        }
        if (string.IsNullOrEmpty(text)) {
            return null;
        }
        int score = 0;
        int textIndex = 0;
        int run = 0;
        int previous = -2;
        foreach (char raw in query) {
            char c = char.ToLowerInvariant(raw);
            if (char.IsWhiteSpace(c)) {
                continue;
            }
            int found = -1;
            for (int i = textIndex; i < text.Length; i++) {
                if (char.ToLowerInvariant(text[i]) == c) {
                    found = i;
                    break;
                }
            }
            if (found < 0) {
                return null;
            }
            bool wordStart = (found == 0) || !char.IsLetterOrDigit(text[found - 1]) || (char.IsUpper(text[found]) && char.IsLower(text[found - 1]));
            run = (found == previous + 1) ? run + 1 : 0;
            score += 1 + (run * 5) + (wordStart ? 8 : 0) + ((found == 0) ? 10 : 0) - Math.Min(found - textIndex, 6);
            previous = found;
            textIndex = found + 1;
        }
        return score;
    }

    /// <summary>Opens the palette with an empty search.</summary>
    public void Open() {
        SetCurrentValue(SearchTextProperty, string.Empty);
        SetCurrentValue(IsOpenProperty, true);
    }

    /// <summary>Closes the palette (it fades out) and returns keyboard focus to where it was.</summary>
    public void Close() {
        SetCurrentValue(IsOpenProperty, false);
    }

    /// <summary>Runs an item: closes the palette, executes its command and raises <see cref="ItemInvoked"/>.</summary>
    public void Invoke(object item) {
        Close();
        if (item is CommandPaletteItem entry && (entry.Command is ICommand command) && command.CanExecute(entry.CommandParameter)) {
            command.Execute(entry.CommandParameter);
        }
        RaiseEvent(new CommandPaletteItemInvokedEventArgs(ItemInvokedEvent, this, item));
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (searchBox is not null) {
            searchBox.PreviewKeyDown -= OnSearchPreviewKeyDown;
        }
        if (resultsList is not null) {
            resultsList.MouseLeftButtonUp -= OnResultsMouseUp;
        }
        if (backdrop is not null) {
            backdrop.MouseLeftButtonDown -= OnBackdropMouseDown;
        }
        base.OnApplyTemplate();
        searchBox = GetTemplateChild(SearchBoxPartName) as TextBox;
        resultsList = GetTemplateChild(ResultsPartName) as ListBox;
        backdrop = GetTemplateChild(BackdropPartName) as FrameworkElement;
        card = GetTemplateChild(CardPartName) as FrameworkElement;
        if (searchBox is not null) {
            searchBox.PreviewKeyDown += OnSearchPreviewKeyDown;
        }
        if (resultsList is not null) {
            resultsList.ItemsSource = results;
            resultsList.MouseLeftButtonUp += OnResultsMouseUp;
        }
        if (backdrop is not null) {
            backdrop.MouseLeftButtonDown += OnBackdropMouseDown;
        }
        Refilter();
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e) {
        base.OnPreviewKeyDown(e);
        if ((e.Key == Key.Escape) && IsOpen) {
            Close();
            e.Handled = true;
        }
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        CommandPalette palette = (CommandPalette)d;
        if (palette.observedSource is not null) {
            palette.observedSource.CollectionChanged -= palette.OnSourceCollectionChanged;
        }
        palette.observedSource = e.NewValue as INotifyCollectionChanged;
        if (palette.observedSource is not null) {
            palette.observedSource.CollectionChanged += palette.OnSourceCollectionChanged;
        }
        palette.Refilter();
    }

    private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        Refilter();
    }

    private static void OnSearchTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((CommandPalette)d).Refilter();
    }

    private void Refilter() {
        string query = (SearchText ?? string.Empty).Trim();
        List<(object Item, int Score, int Order)> ranked = [];
        int order = 0;
        foreach (object item in ItemsSource ?? Array.Empty<object>()) {
            int? score = Score(query, item);
            if (score is int value) {
                ranked.Add((item, value, order));
            }
            order++;
        }
        IEnumerable<object> sorted = (query.Length == 0)
            ? ranked.Select(static r => r.Item)
            : ranked.OrderByDescending(static r => r.Score).ThenBy(static r => r.Order).Select(static r => r.Item);
        results.Clear();
        foreach (object item in sorted.Take(Math.Max(1, MaxResults))) {
            results.Add(item);
        }
        SetValue(HasResultsPropertyKey, results.Count > 0);
        if (resultsList is not null) {
            resultsList.SelectedIndex = (results.Count > 0) ? 0 : -1;
            if (results.Count > 0) {
                resultsList.ScrollIntoView(results[0]);
            }
        }
    }

    private static int? Score(string query, object item) {
        if (item is not CommandPaletteItem entry) {
            return FuzzyScore(query, item?.ToString());
        }
        int? title = FuzzyScore(query, entry.Title);
        int? extra = FuzzyScore(query, $"{entry.Category} {entry.Keywords} {entry.Description}");
        if (title is null && extra is null) {
            return null;
        }
        return Math.Max((title ?? int.MinValue) + 20, extra ?? int.MinValue);
    }

    private static void OnIsOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        CommandPalette palette = (CommandPalette)d;
        if ((bool)e.NewValue) {
            palette.Show();
        }
        else {
            palette.Hide();
        }
    }

    private void Show() {
        restoreFocus = Keyboard.FocusedElement;
        BeginAnimation(VisibilityProperty, null);
        Visibility = Visibility.Visible;
        Refilter();
        bool motion = Jelly.IsMotionEnabled(this);
        if (backdrop is not null) {
            backdrop.BeginAnimation(OpacityProperty, motion ? new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(180))) : null);
        }
        if (card is not null) {
            card.RenderTransformOrigin = new Point(0.5, 0);
            ScaleTransform scale = Jelly.EnsureScale(card);
            if (motion) {
                DoubleAnimation pop = new(0.9, 1, new Duration(TimeSpan.FromMilliseconds(480))) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.BackKey) };
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
                card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(160))));
            }
            else {
                scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
                card.BeginAnimation(OpacityProperty, null);
            }
        }
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
            searchBox?.Focus();
            searchBox?.SelectAll();
        }));
    }

    private void Hide() {
        bool motion = Jelly.IsMotionEnabled(this) && IsLoaded && (Visibility == Visibility.Visible);
        if (motion) {
            Duration fade = new(TimeSpan.FromMilliseconds(130));
            backdrop?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, fade));
            card?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, fade));
            ObjectAnimationUsingKeyFrames collapse = new();
            collapse.KeyFrames.Add(new DiscreteObjectKeyFrame(Visibility.Collapsed, KeyTime.FromTimeSpan(fade.TimeSpan)));
            BeginAnimation(VisibilityProperty, collapse);
        }
        else {
            Visibility = Visibility.Collapsed;
        }
        if ((restoreFocus is UIElement element) && element.IsVisible) {
            element.Focus();
        }
        restoreFocus = null;
    }

    private void OnSearchPreviewKeyDown(object sender, KeyEventArgs e) {
        if (resultsList is null) {
            return;
        }
        int count = results.Count;
        switch (e.Key) {
            case Key.Down when count > 0:
                Move(1);
                e.Handled = true;
                break;
            case Key.Up when count > 0:
                Move(-1);
                e.Handled = true;
                break;
            case Key.PageDown when count > 0:
                Move(5);
                e.Handled = true;
                break;
            case Key.PageUp when count > 0:
                Move(-5);
                e.Handled = true;
                break;
            case Key.Enter:
                if (resultsList.SelectedItem is object item) {
                    Invoke(item);
                }
                e.Handled = true;
                break;
        }
    }

    private void Move(int delta) {
        if (resultsList is null) {
            return;
        }
        int count = results.Count;
        int index = resultsList.SelectedIndex;
        int next = (Math.Abs(delta) == 1) ? (((index + delta) % count) + count) % count : Math.Clamp(index + delta, 0, count - 1);
        resultsList.SelectedIndex = next;
        resultsList.ScrollIntoView(results[next]);
    }

    private void OnResultsMouseUp(object sender, MouseButtonEventArgs e) {
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while ((source is not null) && (source is not ListBoxItem)) {
            source = VisualTreeHelper.GetParent(source);
        }
        if (source is ListBoxItem { DataContext: object item }) {
            Invoke(item);
        }
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e) {
        Close();
        e.Handled = true;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        hostWindow = Window.GetWindow(this);
        if (hostWindow is not null) {
            hostWindow.PreviewKeyDown += OnWindowPreviewKeyDown;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        if (hostWindow is not null) {
            hostWindow.PreviewKeyDown -= OnWindowPreviewKeyDown;
            hostWindow = null;
        }
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e) {
        if ((ShortcutGesture is KeyGesture gesture) && gesture.Matches(null, e)) {
            if (IsOpen) {
                Close();
            }
            else {
                Open();
            }
            e.Handled = true;
        }
    }
}