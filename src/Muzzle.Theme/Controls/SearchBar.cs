using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>Data for <see cref="SearchBar.SearchRequested"/>.</summary>
public sealed class SearchRequestedEventArgs(RoutedEvent routedEvent, object source, string query, bool isSubmitted) : RoutedEventArgs(routedEvent, source) {
    /// <summary>Gets the search text.</summary>
    public string Query { get; } = query;

    /// <summary>True when the user pressed Enter or picked a suggestion; false for a debounced as-you-type search.</summary>
    public bool IsSubmitted { get; } = isSubmitted;
}

/// <summary>Handles <see cref="SearchBar.SearchRequested"/>.</summary>
/// <param name="sender">The search bar.</param>
/// <param name="e">The query and whether it was submitted with Enter.</param>
public delegate void SearchRequestedEventHandler(object sender, SearchRequestedEventArgs e);

/// <summary>
/// A TextBox with a magnifier, a clear (×) button that pops in when there is text, debounced searching
/// (<see cref="SearchRequested"/> / <see cref="SearchCommand"/>) and an optional live suggestion dropdown
/// filtered from <see cref="SuggestionsSource"/>. Arrow keys move through suggestions; Enter picks; Esc closes, then clears.
/// </summary>
[TemplatePart(Name = SuggestionListPartName, Type = typeof(ListBox))]
public class SearchBar : TextBox {
    /// <summary>Gets the command that clears the text (the × button).</summary>
    public static readonly RoutedCommand ClearCommand = new(nameof(ClearCommand), typeof(SearchBar));

    /// <summary>Identifies the <see cref="SearchRequested"/> routed event.</summary>
    public static readonly RoutedEvent SearchRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(SearchRequested), RoutingStrategy.Bubble, typeof(SearchRequestedEventHandler), typeof(SearchBar));

    /// <summary>Identifies the <see cref="SuggestionChosen"/> routed event.</summary>
    public static readonly RoutedEvent SuggestionChosenEvent = EventManager.RegisterRoutedEvent(
        nameof(SuggestionChosen), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<object?>), typeof(SearchBar));

    /// <summary>Executed with the query text after the debounce delay and on Enter.</summary>
    public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(
        nameof(SearchCommand), typeof(ICommand), typeof(SearchBar), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="DebounceDelay"/> dependency property.</summary>
    public static readonly DependencyProperty DebounceDelayProperty = DependencyProperty.Register(
        nameof(DebounceDelay), typeof(TimeSpan), typeof(SearchBar), new PropertyMetadata(TimeSpan.FromMilliseconds(300)));

    /// <summary>All suggestions; the dropdown shows those whose text contains the query.</summary>
    public static readonly DependencyProperty SuggestionsSourceProperty = DependencyProperty.Register(
        nameof(SuggestionsSource), typeof(IEnumerable), typeof(SearchBar), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="MaxSuggestions"/> dependency property.</summary>
    public static readonly DependencyProperty MaxSuggestionsProperty = DependencyProperty.Register(
        nameof(MaxSuggestions), typeof(int), typeof(SearchBar), new PropertyMetadata(8));

    /// <summary>Identifies the <see cref="IsSuggestionListOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsSuggestionListOpenProperty = DependencyProperty.Register(
        nameof(IsSuggestionListOpen), typeof(bool), typeof(SearchBar), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    private const string SuggestionListPartName = "PART_SuggestionList";

    private readonly ObservableCollection<object> suggestions = [];
    private readonly DispatcherTimer debounce;
    private ListBox? suggestionList;
    private bool suppressSuggestions;

    static SearchBar() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SearchBar), new FrameworkPropertyMetadata(typeof(SearchBar)));
    }

    /// <summary>Initializes a new instance of the <see cref="SearchBar"/> class.</summary>
    public SearchBar() {
        Suggestions = new ReadOnlyObservableCollection<object>(suggestions);
        debounce = new DispatcherTimer(DispatcherPriority.Input, Dispatcher);
        debounce.Tick += OnDebounceTick;
        CommandBindings.Add(new CommandBinding(ClearCommand, OnClear));
    }

    /// <summary>Occurs after <see cref="DebounceDelay"/> while typing, and immediately when Enter is pressed.</summary>
    public event SearchRequestedEventHandler SearchRequested {
        add => AddHandler(SearchRequestedEvent, value);
        remove => RemoveHandler(SearchRequestedEvent, value);
    }

    /// <summary>Occurs when a suggestion is picked from the list.</summary>
    public event RoutedPropertyChangedEventHandler<object?> SuggestionChosen {
        add => AddHandler(SuggestionChosenEvent, value);
        remove => RemoveHandler(SuggestionChosenEvent, value);
    }

    /// <summary>Gets or sets the search command. The default is <see langword="null"/>. This is a dependency property.</summary>
    public ICommand? SearchCommand {
        get => (ICommand?)GetValue(SearchCommandProperty);
        set => SetValue(SearchCommandProperty, value);
    }

    /// <summary>Gets or sets the debounce delay. The default is <c>TimeSpan.FromMilliseconds(300)</c>. This is a dependency property.</summary>
    public TimeSpan DebounceDelay {
        get => (TimeSpan)GetValue(DebounceDelayProperty);
        set => SetValue(DebounceDelayProperty, value);
    }

    /// <summary>Gets or sets the suggestions source. The default is <see langword="null"/>. This is a dependency property.</summary>
    public IEnumerable? SuggestionsSource {
        get => (IEnumerable?)GetValue(SuggestionsSourceProperty);
        set => SetValue(SuggestionsSourceProperty, value);
    }

    /// <summary>Gets or sets the max suggestions. The default is <c>8</c>. This is a dependency property.</summary>
    public int MaxSuggestions {
        get => (int)GetValue(MaxSuggestionsProperty);
        set => SetValue(MaxSuggestionsProperty, value);
    }

    /// <summary>Gets or sets whether the suggestion list is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsSuggestionListOpen {
        get => (bool)GetValue(IsSuggestionListOpenProperty);
        set => SetValue(IsSuggestionListOpenProperty, value);
    }

    /// <summary>The filtered suggestions currently shown.</summary>
    public ReadOnlyObservableCollection<object> Suggestions { get; }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (suggestionList is not null) {
            suggestionList.PreviewMouseLeftButtonUp -= OnSuggestionClicked;
        }
        base.OnApplyTemplate();
        suggestionList = GetTemplateChild(SuggestionListPartName) as ListBox;
        if (suggestionList is not null) {
            suggestionList.ItemsSource = Suggestions;
            suggestionList.PreviewMouseLeftButtonUp += OnSuggestionClicked;
        }
    }

    /// <inheritdoc />
    protected override void OnTextChanged(TextChangedEventArgs e) {
        base.OnTextChanged(e);
        debounce.Stop();
        if (DebounceDelay > TimeSpan.Zero) {
            debounce.Interval = DebounceDelay;
            debounce.Start();
        }
        else {
            RaiseSearch(false);
        }
        if (!suppressSuggestions) {
            UpdateSuggestions();
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e) {
        switch (e.Key) {
            case Key.Down when IsSuggestionListOpen && (suggestionList is not null):
                MoveSelection(1);
                e.Handled = true;
                return;
            case Key.Up when IsSuggestionListOpen && (suggestionList is not null):
                MoveSelection(-1);
                e.Handled = true;
                return;
            case Key.Enter:
                if (IsSuggestionListOpen && (suggestionList?.SelectedItem is object chosen)) {
                    Choose(chosen);
                }
                else {
                    debounce.Stop();
                    SetCurrentValue(IsSuggestionListOpenProperty, false);
                    RaiseSearch(true);
                }
                e.Handled = true;
                return;
            case Key.Escape:
                if (IsSuggestionListOpen) {
                    SetCurrentValue(IsSuggestionListOpenProperty, false);
                }
                else if (Text.Length > 0) {
                    Clear();
                }
                e.Handled = true;
                return;
        }
        base.OnPreviewKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {
        base.OnLostKeyboardFocus(e);
        SetCurrentValue(IsSuggestionListOpenProperty, false);
    }

    private void UpdateSuggestions() {
        suggestions.Clear();
        string query = Text.Trim();
        IEnumerable? source = SuggestionsSource;
        if ((source is not null) && (query.Length > 0)) {
            foreach (object? item in source) {
                if (item is null) {
                    continue;
                }
                string label = item.ToString() ?? string.Empty;
                if (label.Contains(query, StringComparison.CurrentCultureIgnoreCase) && !string.Equals(label, query, StringComparison.CurrentCultureIgnoreCase)) {
                    suggestions.Add(item);
                    if (suggestions.Count >= MaxSuggestions) {
                        break;
                    }
                }
            }
        }
        SetCurrentValue(IsSuggestionListOpenProperty, (suggestions.Count > 0) && IsKeyboardFocusWithin);
        if (suggestionList is not null) {
            suggestionList.SelectedIndex = -1;
        }
    }

    private void MoveSelection(int delta) {
        if ((suggestionList is null) || (suggestions.Count == 0)) {
            return;
        }
        int next = suggestionList.SelectedIndex + delta;
        if (next < 0) {
            next = suggestions.Count - 1;
        }
        else if (next >= suggestions.Count) {
            next = 0;
        }
        suggestionList.SelectedIndex = next;
        suggestionList.ScrollIntoView(suggestionList.SelectedItem);
    }

    private void Choose(object item) {
        suppressSuggestions = true;
        Text = item.ToString() ?? string.Empty;
        CaretIndex = Text.Length;
        suppressSuggestions = false;
        SetCurrentValue(IsSuggestionListOpenProperty, false);
        debounce.Stop();
        RaiseEvent(new RoutedPropertyChangedEventArgs<object?>(null, item, SuggestionChosenEvent));
        RaiseSearch(true);
    }

    private void OnSuggestionClicked(object sender, MouseButtonEventArgs e) {
        DependencyObject? current = e.OriginalSource as DependencyObject;
        while ((current is not null) && (current is not ListBoxItem)) {
            current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }
        if (current is ListBoxItem { DataContext: object item }) {
            Choose(item);
            Focus();
            e.Handled = true;
        }
    }

    private void OnDebounceTick(object? sender, EventArgs e) {
        debounce.Stop();
        RaiseSearch(false);
    }

    private void RaiseSearch(bool submitted) {
        string query = Text;
        RaiseEvent(new SearchRequestedEventArgs(SearchRequestedEvent, this, query, submitted));
        ICommand? command = SearchCommand;
        if ((command is not null) && command.CanExecute(query)) {
            command.Execute(query);
        }
    }

    private void OnClear(object sender, ExecutedRoutedEventArgs e) {
        Clear();
        Focus();
    }
}