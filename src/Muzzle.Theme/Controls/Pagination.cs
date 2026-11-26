using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>One slot in a <see cref="Pagination"/> strip: a page number or an ellipsis gap.</summary>
public sealed class PaginationSlot(int page, bool isCurrent) {
    /// <summary>The page number, or 0 for an ellipsis.</summary>
    public int Page { get; } = page;

    /// <summary>Gets whether this slot is a "…" gap rather than a page.</summary>
    public bool IsEllipsis => Page == 0;

    /// <summary>Gets whether this slot is the current page.</summary>
    public bool IsCurrent { get; } = isCurrent;

    /// <summary>Gets the text shown: the page number, or "…".</summary>
    public string Label => IsEllipsis ? "…" : Page.ToString(CultureInfo.CurrentCulture);
}

/// <summary>
/// Page navigation: first / previous / numbered pages / next / last, plus an optional jump-to-page box.
/// A blush pill bounces to the current page; every button squishes on click.
/// </summary>
[TemplatePart(Name = PillPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = PagesPartName, Type = typeof(ItemsControl))]
[TemplatePart(Name = JumpBoxPartName, Type = typeof(TextBox))]
public class Pagination : Control {
    private const string PillPartName = "PART_Pill";
    private const string PagesPartName = "PART_Pages";
    private const string JumpBoxPartName = "PART_JumpBox";

    /// <summary>Gets the command that jumps to page 1.</summary>
    public static readonly RoutedCommand FirstPageCommand = new(nameof(FirstPageCommand), typeof(Pagination));
    /// <summary>Gets the command that goes back one page.</summary>
    public static readonly RoutedCommand PreviousPageCommand = new(nameof(PreviousPageCommand), typeof(Pagination));
    /// <summary>Gets the command that goes forward one page.</summary>
    public static readonly RoutedCommand NextPageCommand = new(nameof(NextPageCommand), typeof(Pagination));
    /// <summary>Gets the command that jumps to the last page.</summary>
    public static readonly RoutedCommand LastPageCommand = new(nameof(LastPageCommand), typeof(Pagination));
    /// <summary>Gets the command that goes to the page given as its parameter.</summary>
    public static readonly RoutedCommand GoToPageCommand = new(nameof(GoToPageCommand), typeof(Pagination));
    /// <summary>Gets the command that goes to the page typed in the jump box (invalid input wobbles).</summary>
    public static readonly RoutedCommand JumpCommand = new(nameof(JumpCommand), typeof(Pagination));

    /// <summary>Identifies the <see cref="PageChanged"/> routed event.</summary>
    public static readonly RoutedEvent PageChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(PageChanged), RoutingStrategy.Bubble, typeof(RoutedPropertyChangedEventHandler<int>), typeof(Pagination));

    /// <summary>Identifies the <see cref="PageCount"/> dependency property.</summary>
    public static readonly DependencyProperty PageCountProperty = DependencyProperty.Register(
        nameof(PageCount),
        typeof(int),
        typeof(Pagination),
        new PropertyMetadata(10, OnPageCountChanged, CoercePageCount));

    /// <summary>Identifies the <see cref="CurrentPage"/> dependency property.</summary>
    public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register(
        nameof(CurrentPage),
        typeof(int),
        typeof(Pagination),
        new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCurrentPageChanged, CoerceCurrentPage));

    /// <summary>How many slots (numbers and ellipses) the strip shows; at least 5.</summary>
    public static readonly DependencyProperty MaxDisplayedPagesProperty = DependencyProperty.Register(
        nameof(MaxDisplayedPages), typeof(int), typeof(Pagination), new PropertyMetadata(7, OnLayoutChanged));

    /// <summary>Identifies the <see cref="ShowJumpBox"/> dependency property.</summary>
    public static readonly DependencyProperty ShowJumpBoxProperty = DependencyProperty.Register(
        nameof(ShowJumpBox), typeof(bool), typeof(Pagination), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="ShowFirstLastButtons"/> dependency property.</summary>
    public static readonly DependencyProperty ShowFirstLastButtonsProperty = DependencyProperty.Register(
        nameof(ShowFirstLastButtons), typeof(bool), typeof(Pagination), new PropertyMetadata(true));

    private readonly ObservableCollection<PaginationSlot> slots = [];
    private FrameworkElement? pill;
    private ItemsControl? pages;
    private TextBox? jumpBox;

    static Pagination() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Pagination), new FrameworkPropertyMetadata(typeof(Pagination)));
        FocusableProperty.OverrideMetadata(typeof(Pagination), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="Pagination"/> class.</summary>
    public Pagination() {
        CommandBindings.Add(new CommandBinding(FirstPageCommand, OnFirst, CanGoBack));
        CommandBindings.Add(new CommandBinding(PreviousPageCommand, OnPrevious, CanGoBack));
        CommandBindings.Add(new CommandBinding(NextPageCommand, OnNext, CanGoForward));
        CommandBindings.Add(new CommandBinding(LastPageCommand, OnLast, CanGoForward));
        CommandBindings.Add(new CommandBinding(GoToPageCommand, OnGoToPage));
        CommandBindings.Add(new CommandBinding(JumpCommand, OnJump));
        Slots = new ReadOnlyObservableCollection<PaginationSlot>(slots);
        RebuildSlots();
        Loaded += OnLoaded;
    }

    /// <summary>Occurs when <see cref="CurrentPage"/> changes.</summary>
    public event RoutedPropertyChangedEventHandler<int> PageChanged {
        add => AddHandler(PageChangedEvent, value);
        remove => RemoveHandler(PageChangedEvent, value);
    }

    /// <summary>Gets or sets the page count. The default is <c>10</c>. This is a dependency property.</summary>
    public int PageCount {
        get => (int)GetValue(PageCountProperty);
        set => SetValue(PageCountProperty, value);
    }

    /// <summary>1-based current page, kept within 1…<see cref="PageCount"/>.</summary>
    public int CurrentPage {
        get => (int)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    /// <summary>Gets or sets the max displayed pages. The default is <c>7</c>. This is a dependency property.</summary>
    public int MaxDisplayedPages {
        get => (int)GetValue(MaxDisplayedPagesProperty);
        set => SetValue(MaxDisplayedPagesProperty, value);
    }

    /// <summary>Gets or sets whether to show the jump box. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool ShowJumpBox {
        get => (bool)GetValue(ShowJumpBoxProperty);
        set => SetValue(ShowJumpBoxProperty, value);
    }

    /// <summary>Gets or sets whether to show the first last buttons. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowFirstLastButtons {
        get => (bool)GetValue(ShowFirstLastButtonsProperty);
        set => SetValue(ShowFirstLastButtonsProperty, value);
    }

    /// <summary>The slots currently shown (bound by the template).</summary>
    public ReadOnlyObservableCollection<PaginationSlot> Slots { get; }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (jumpBox is not null) {
            jumpBox.KeyDown -= OnJumpBoxKeyDown;
        }
        base.OnApplyTemplate();
        pill = GetTemplateChild(PillPartName) as FrameworkElement;
        pages = GetTemplateChild(PagesPartName) as ItemsControl;
        jumpBox = GetTemplateChild(JumpBoxPartName) as TextBox;
        if (pages is not null) {
            pages.ItemsSource = slots;
        }
        if (jumpBox is not null) {
            jumpBox.KeyDown += OnJumpBoxKeyDown;
        }
        SchedulePill(false);
    }

    private static object CoercePageCount(DependencyObject d, object value) {
        return Math.Max(1, (int)value);
    }

    private static object CoerceCurrentPage(DependencyObject d, object value) {
        return Math.Clamp((int)value, 1, ((Pagination)d).PageCount);
    }

    private static void OnPageCountChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Pagination pagination = (Pagination)d;
        pagination.CoerceValue(CurrentPageProperty);
        pagination.RebuildSlots();
        pagination.SchedulePill(false);
    }

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Pagination pagination = (Pagination)d;
        pagination.RebuildSlots();
        pagination.SchedulePill(false);
    }

    private static void OnCurrentPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Pagination pagination = (Pagination)d;
        pagination.RebuildSlots();
        pagination.SchedulePill(true);
        pagination.RaiseEvent(new RoutedPropertyChangedEventArgs<int>((int)e.OldValue, (int)e.NewValue, PageChangedEvent));
        CommandManager.InvalidateRequerySuggested();
    }

    private void RebuildSlots() {
        int count = PageCount;
        int current = CurrentPage;
        int max = Math.Max(5, MaxDisplayedPages);
        slots.Clear();
        if (count <= max) {
            for (int page = 1; page <= count; page++) {
                slots.Add(new PaginationSlot(page, page == current));
            }
            return;
        }
        // First and last always show; the window between them follows the current page, with ellipses for gaps.
        int window = max - 4;
        int start = Math.Clamp(current - (window / 2), 2, count - window);
        int end = Math.Min(count - 1, start + window - 1);
        if (start == 3) {
            start = 2;
        }
        if (end == count - 2) {
            end = count - 1;
        }
        if (start == 2) {
            end = Math.Min(count - 1, Math.Max(end, max - 2));
        }
        if (end == count - 1) {
            start = Math.Max(2, Math.Min(start, count - (max - 3)));
        }
        slots.Add(new PaginationSlot(1, current == 1));
        if (start > 2) {
            slots.Add(new PaginationSlot(0, false));
        }
        for (int page = start; page <= end; page++) {
            slots.Add(new PaginationSlot(page, page == current));
        }
        if (end < count - 1) {
            slots.Add(new PaginationSlot(0, false));
        }
        slots.Add(new PaginationSlot(count, current == count));
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        SchedulePill(false);
    }

    private void SchedulePill(bool animate) {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => UpdatePill(animate)));
    }

    private void UpdatePill(bool animate) {
        if ((pill is null) || (pages is null)) {
            return;
        }
        FrameworkElement? target = null;
        foreach (PaginationSlot slot in slots) {
            if (slot.IsCurrent) {
                target = pages.ItemContainerGenerator.ContainerFromItem(slot) as FrameworkElement;
                break;
            }
        }
        if (pill.Parent is Visual host) {
            PillAnimator.MoveTo(pill, target, host, animate, trackX: true, trackY: true);
        }
    }

    private void CanGoBack(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = CurrentPage > 1;
    }

    private void CanGoForward(object sender, CanExecuteRoutedEventArgs e) {
        e.CanExecute = CurrentPage < PageCount;
    }

    private void OnFirst(object sender, ExecutedRoutedEventArgs e) {
        SetCurrentValue(CurrentPageProperty, 1);
    }

    private void OnPrevious(object sender, ExecutedRoutedEventArgs e) {
        SetCurrentValue(CurrentPageProperty, CurrentPage - 1);
    }

    private void OnNext(object sender, ExecutedRoutedEventArgs e) {
        SetCurrentValue(CurrentPageProperty, CurrentPage + 1);
    }

    private void OnLast(object sender, ExecutedRoutedEventArgs e) {
        SetCurrentValue(CurrentPageProperty, PageCount);
    }

    private void OnGoToPage(object sender, ExecutedRoutedEventArgs e) {
        if (e.Parameter is int page && (page > 0)) {
            SetCurrentValue(CurrentPageProperty, page);
        }
    }

    private void OnJump(object sender, ExecutedRoutedEventArgs e) {
        Jump();
    }

    private void OnJumpBoxKeyDown(object sender, KeyEventArgs e) {
        if (e.Key == Key.Enter) {
            Jump();
            e.Handled = true;
        }
    }

    private void Jump() {
        if (jumpBox is null) {
            return;
        }
        if (int.TryParse(jumpBox.Text.Trim(), NumberStyles.Integer, CultureInfo.CurrentCulture, out int page) && (page >= 1) && (page <= PageCount)) {
            SetCurrentValue(CurrentPageProperty, page);
            jumpBox.Clear();
            return;
        }
        // Out of range or not a number: a "nope" wobble instead of a silent clamp.
        Jelly.Wobble(jumpBox);
        jumpBox.SelectAll();
    }
}