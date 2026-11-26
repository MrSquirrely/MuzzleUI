using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A token box: typed entries become pastel <see cref="TagItem"/> chips when the <see cref="Delimiter"/> or Enter is
/// typed (or focus leaves). Chips pop in with an elastic overshoot and squish out when removed with their × or with
/// Backspace (first press highlights the last chip, second press removes it). <see cref="Tags"/> is two-way bindable.
/// </summary>
[TemplatePart(Name = PanelPartName, Type = typeof(Panel))]
[TemplatePart(Name = TextBoxPartName, Type = typeof(TextBox))]
public class TagInput : Control {
    /// <summary>Identifies the <see cref="Tags"/> dependency property.</summary>
    public static readonly DependencyProperty TagsProperty = DependencyProperty.Register(
        nameof(Tags),
        typeof(IList<string>),
        typeof(TagInput),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTagsChanged));

    /// <summary>Characters that complete a tag as you type (each character counts). Enter always completes.</summary>
    public static readonly DependencyProperty DelimiterProperty = DependencyProperty.Register(
        nameof(Delimiter), typeof(string), typeof(TagInput), new PropertyMetadata(","));

    /// <summary>Identifies the <see cref="AllowDuplicates"/> dependency property.</summary>
    public static readonly DependencyProperty AllowDuplicatesProperty = DependencyProperty.Register(
        nameof(AllowDuplicates), typeof(bool), typeof(TagInput), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="Placeholder"/> dependency property.</summary>
    public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
        nameof(Placeholder), typeof(string), typeof(TagInput), new PropertyMetadata("Add a tag…"));

    /// <summary>0 means unlimited.</summary>
    public static readonly DependencyProperty MaxTagsProperty = DependencyProperty.Register(
        nameof(MaxTags), typeof(int), typeof(TagInput), new PropertyMetadata(0));

    private static readonly DependencyPropertyKey HasTagsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasTags), typeof(bool), typeof(TagInput), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasTags"/> read-only dependency property.</summary>
    public static readonly DependencyProperty HasTagsProperty = HasTagsPropertyKey.DependencyProperty;

    private const string PanelPartName = "PART_Panel";
    private const string TextBoxPartName = "PART_TextBox";

    private readonly List<TagItem> chips = [];
    private Panel? panel;
    private TextBox? textBox;
    private INotifyCollectionChanged? observed;
    private bool suppressTextHandling;

    static TagInput() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TagInput), new FrameworkPropertyMetadata(typeof(TagInput)));
        FocusableProperty.OverrideMetadata(typeof(TagInput), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="TagInput"/> class.</summary>
    public TagInput() {
        SetCurrentValue(TagsProperty, new ObservableCollection<string>());
        AddHandler(TagItem.RemoveRequestedEvent, new RoutedEventHandler(OnChipRemoveRequested));
    }

    /// <summary>Gets or sets the tags. The default is <see langword="null"/>. This is a dependency property.</summary>
    public IList<string> Tags {
        get => (IList<string>)GetValue(TagsProperty);
        set => SetValue(TagsProperty, value);
    }

    /// <summary>Gets or sets the delimiter. The default is <c>&quot;</c>. This is a dependency property.</summary>
    public string Delimiter {
        get => (string)GetValue(DelimiterProperty);
        set => SetValue(DelimiterProperty, value);
    }

    /// <summary>Gets or sets whether to allow duplicates. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool AllowDuplicates {
        get => (bool)GetValue(AllowDuplicatesProperty);
        set => SetValue(AllowDuplicatesProperty, value);
    }

    /// <summary>Gets or sets the placeholder. The default is <c>&quot;Add a tag…&quot;</c>. This is a dependency property.</summary>
    public string? Placeholder {
        get => (string?)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Gets or sets the max tags. The default is <c>0</c>. This is a dependency property.</summary>
    public int MaxTags {
        get => (int)GetValue(MaxTagsProperty);
        set => SetValue(MaxTagsProperty, value);
    }

    /// <summary>Gets whether it has tags. This is a read-only dependency property.</summary>
    public bool HasTags => (bool)GetValue(HasTagsProperty);

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (textBox is not null) {
            textBox.TextChanged -= OnTextChanged;
            textBox.PreviewKeyDown -= OnTextBoxKeyDown;
            textBox.LostKeyboardFocus -= OnTextBoxLostFocus;
        }
        panel?.Children.Clear();
        base.OnApplyTemplate();
        panel = GetTemplateChild(PanelPartName) as Panel;
        textBox = GetTemplateChild(TextBoxPartName) as TextBox;
        if (textBox is not null) {
            textBox.TextChanged += OnTextChanged;
            textBox.PreviewKeyDown += OnTextBoxKeyDown;
            textBox.LostKeyboardFocus += OnTextBoxLostFocus;
        }
        RebuildChips();
    }

    /// <summary>Adds a tag (trimmed). Returns false when empty, a rejected duplicate, or over <see cref="MaxTags"/>.</summary>
    public bool AddTag(string text) {
        string tag = text.Trim();
        IList<string>? tags = Tags;
        if ((tag.Length == 0) || (tags is null) || tags.IsReadOnly) {
            return false;
        }
        if (!AllowDuplicates) {
            for (int i = 0; i < tags.Count; i++) {
                if (string.Equals(tags[i], tag, StringComparison.CurrentCultureIgnoreCase)) {
                    if (i < chips.Count) {
                        Jelly.Wobble(chips[i]);
                    }
                    return false;
                }
            }
        }
        if ((MaxTags > 0) && (tags.Count >= MaxTags)) {
            if (textBox is not null) {
                Jelly.Wobble(textBox);
            }
            return false;
        }
        tags.Add(tag);
        if (observed is null) {
            // Plain IList: no change notifications, so mirror the chip ourselves.
            InsertChip(tags.Count - 1, tag, true);
        }
        return true;
    }

    /// <summary>Removes the tag at <paramref name="index"/> with a squish-out animation.</summary>
    public void RemoveTagAt(int index) {
        IList<string>? tags = Tags;
        if ((tags is null) || tags.IsReadOnly || (index < 0) || (index >= tags.Count)) {
            return;
        }
        if ((index < chips.Count) && !chips[index].IsExiting) {
            TagItem chip = chips[index];
            chip.PlayExit(() => RemoveFromSource(chip));
            return;
        }
        tags.RemoveAt(index);
    }

    private void RemoveFromSource(TagItem chip) {
        IList<string>? tags = Tags;
        int index = chips.IndexOf(chip);
        if ((tags is null) || (index < 0) || (index >= tags.Count)) {
            return;
        }
        tags.RemoveAt(index);
        if (observed is null) {
            RemoveChip(index);
        }
    }

    private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        TagInput input = (TagInput)d;
        if (input.observed is not null) {
            input.observed.CollectionChanged -= input.OnTagsCollectionChanged;
        }
        input.observed = e.NewValue as INotifyCollectionChanged;
        if (input.observed is not null) {
            input.observed.CollectionChanged += input.OnTagsCollectionChanged;
        }
        input.RebuildChips();
    }

    private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) {
        switch (e.Action) {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null:
                for (int i = 0; i < e.NewItems.Count; i++) {
                    InsertChip(e.NewStartingIndex + i, e.NewItems[i] as string ?? string.Empty, true);
                }
                break;
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null:
                for (int i = 0; i < e.OldItems.Count; i++) {
                    RemoveChip(e.OldStartingIndex);
                }
                break;
            default:
                RebuildChips();
                break;
        }
    }

    private void RebuildChips() {
        if (panel is null) {
            return;
        }
        foreach (TagItem chip in chips) {
            panel.Children.Remove(chip);
        }
        chips.Clear();
        IList<string>? tags = Tags;
        if (tags is not null) {
            for (int i = 0; i < tags.Count; i++) {
                InsertChip(i, tags[i], false);
            }
        }
        UpdateHasTags();
    }

    private void InsertChip(int index, string tag, bool animate) {
        if (panel is null) {
            return;
        }
        TagItem chip = new() { Content = tag };
        AutomationProperties.SetName(chip, tag);
        index = Math.Clamp(index, 0, chips.Count);
        chips.Insert(index, chip);
        panel.Children.Insert(index, chip);
        if (animate) {
            chip.Loaded += OnChipLoaded;
        }
        ClearHighlight();
        UpdateHasTags();
    }

    private void RemoveChip(int index) {
        if ((panel is null) || (index < 0) || (index >= chips.Count)) {
            return;
        }
        TagItem chip = chips[index];
        chips.RemoveAt(index);
        if (chip.IsExiting) {
            panel.Children.Remove(chip);
        }
        else {
            chip.PlayExit(() => panel?.Children.Remove(chip));
        }
        UpdateHasTags();
    }

    private static void OnChipLoaded(object sender, RoutedEventArgs e) {
        TagItem chip = (TagItem)sender;
        chip.Loaded -= OnChipLoaded;
        chip.PlayEnter();
    }

    private void UpdateHasTags() {
        SetValue(HasTagsPropertyKey, (Tags?.Count ?? 0) > 0);
    }

    private void OnChipRemoveRequested(object sender, RoutedEventArgs e) {
        if (e.OriginalSource is TagItem chip) {
            RemoveTagAt(chips.IndexOf(chip));
            textBox?.Focus();
        }
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e) {
        if (suppressTextHandling || (textBox is null) || string.IsNullOrEmpty(Delimiter)) {
            return;
        }
        string text = textBox.Text;
        int last = text.LastIndexOfAny(Delimiter.ToCharArray());
        if (last < 0) {
            ClearHighlight();
            return;
        }
        foreach (string part in text[..last].Split(Delimiter.ToCharArray(), StringSplitOptions.RemoveEmptyEntries)) {
            AddTag(part);
        }
        suppressTextHandling = true;
        textBox.Text = text[(last + 1)..];
        textBox.CaretIndex = textBox.Text.Length;
        suppressTextHandling = false;
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e) {
        if (textBox is null) {
            return;
        }
        if (e.Key == Key.Enter) {
            if (AddTag(textBox.Text)) {
                textBox.Clear();
            }
            e.Handled = true;
        }
        else if ((e.Key == Key.Back) && (textBox.Text.Length == 0) && (textBox.CaretIndex == 0) && (chips.Count > 0)) {
            TagItem lastChip = chips[^1];
            if (lastChip.IsHighlighted) {
                RemoveTagAt(chips.Count - 1);
            }
            else {
                lastChip.IsHighlighted = true;
            }
            e.Handled = true;
        }
        else if (e.Key == Key.Escape) {
            ClearHighlight();
        }
    }

    private void OnTextBoxLostFocus(object sender, KeyboardFocusChangedEventArgs e) {
        if ((textBox is not null) && AddTag(textBox.Text)) {
            textBox.Clear();
        }
        ClearHighlight();
    }

    private void ClearHighlight() {
        foreach (TagItem chip in chips) {
            chip.IsHighlighted = false;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonDown(e);
        // Clicking the empty part of the field focuses the text entry.
        if (!e.Handled && (textBox is not null)) {
            textBox.Focus();
            e.Handled = true;
        }
    }

}