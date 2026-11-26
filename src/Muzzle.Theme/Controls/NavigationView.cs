using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A collapsible side menu. It is a single-selection ListBox, so it supports <c>ItemsSource</c>, <c>SelectedItem</c>,
/// keyboard navigation and UI Automation; items become <see cref="NavigationViewItem"/>s (icon + label).
/// A pastel pill slides behind the selected item with a BackEase overshoot, and the pane springs between
/// <see cref="CompactPaneLength"/> and <see cref="OpenPaneLength"/> when <see cref="IsPaneOpen"/> changes.
/// </summary>
[TemplatePart(Name = PaneRootPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SelectionPillPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ItemsHostPartName, Type = typeof(FrameworkElement))]
public class NavigationView : ListBox {
    private const string PaneRootPartName = "PART_PaneRoot";
    private const string SelectionPillPartName = "PART_SelectionPill";
    private const string ItemsHostPartName = "PART_ItemsHost";

    /// <summary>Identifies the <see cref="IsPaneOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsPaneOpenProperty = DependencyProperty.Register(
        nameof(IsPaneOpen),
        typeof(bool),
        typeof(NavigationView),
        new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnPaneLengthChanged));

    /// <summary>Identifies the <see cref="CompactPaneLength"/> dependency property.</summary>
    public static readonly DependencyProperty CompactPaneLengthProperty = DependencyProperty.Register(
        nameof(CompactPaneLength), typeof(double), typeof(NavigationView), new PropertyMetadata(64.0, OnPaneLengthChanged));

    /// <summary>Identifies the <see cref="OpenPaneLength"/> dependency property.</summary>
    public static readonly DependencyProperty OpenPaneLengthProperty = DependencyProperty.Register(
        nameof(OpenPaneLength), typeof(double), typeof(NavigationView), new PropertyMetadata(240.0, OnPaneLengthChanged));

    /// <summary>Content above the items (logo, title, search…). Shown next to the pane toggle button.</summary>
    public static readonly DependencyProperty PaneHeaderProperty = DependencyProperty.Register(
        nameof(PaneHeader), typeof(object), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="PaneHeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty PaneHeaderTemplateProperty = DependencyProperty.Register(
        nameof(PaneHeaderTemplate), typeof(DataTemplate), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Content pinned below the items (settings, account, version…).</summary>
    public static readonly DependencyProperty PaneFooterProperty = DependencyProperty.Register(
        nameof(PaneFooter), typeof(object), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="PaneFooterTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty PaneFooterTemplateProperty = DependencyProperty.Register(
        nameof(PaneFooterTemplate), typeof(DataTemplate), typeof(NavigationView), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="IsPaneToggleButtonVisible"/> dependency property.</summary>
    public static readonly DependencyProperty IsPaneToggleButtonVisibleProperty = DependencyProperty.Register(
        nameof(IsPaneToggleButtonVisible), typeof(bool), typeof(NavigationView), new PropertyMetadata(true));

    private FrameworkElement? paneRoot;
    private FrameworkElement? pill;
    private FrameworkElement? itemsHost;

    static NavigationView() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(NavigationView), new FrameworkPropertyMetadata(typeof(NavigationView)));
        SelectionModeProperty.OverrideMetadata(typeof(NavigationView), new FrameworkPropertyMetadata(SelectionMode.Single));
    }

    /// <summary>Initializes a new instance of the <see cref="NavigationView"/> class.</summary>
    public NavigationView() {
        SizeChanged += OnSizeChanged;
        Loaded += OnLoaded;
    }

    /// <summary>Gets or sets whether the pane is open. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsPaneOpen {
        get => (bool)GetValue(IsPaneOpenProperty);
        set => SetValue(IsPaneOpenProperty, value);
    }

    /// <summary>Gets or sets the compact pane length. The default is <c>64.0</c>. This is a dependency property.</summary>
    public double CompactPaneLength {
        get => (double)GetValue(CompactPaneLengthProperty);
        set => SetValue(CompactPaneLengthProperty, value);
    }

    /// <summary>Gets or sets the open pane length. The default is <c>240.0</c>. This is a dependency property.</summary>
    public double OpenPaneLength {
        get => (double)GetValue(OpenPaneLengthProperty);
        set => SetValue(OpenPaneLengthProperty, value);
    }

    /// <summary>Gets or sets the pane header. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? PaneHeader {
        get => GetValue(PaneHeaderProperty);
        set => SetValue(PaneHeaderProperty, value);
    }

    /// <summary>Gets or sets the pane header template. The default is <see langword="null"/>. This is a dependency property.</summary>
    public DataTemplate? PaneHeaderTemplate {
        get => (DataTemplate?)GetValue(PaneHeaderTemplateProperty);
        set => SetValue(PaneHeaderTemplateProperty, value);
    }

    /// <summary>Gets or sets the pane footer. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? PaneFooter {
        get => GetValue(PaneFooterProperty);
        set => SetValue(PaneFooterProperty, value);
    }

    /// <summary>Gets or sets the pane footer template. The default is <see langword="null"/>. This is a dependency property.</summary>
    public DataTemplate? PaneFooterTemplate {
        get => (DataTemplate?)GetValue(PaneFooterTemplateProperty);
        set => SetValue(PaneFooterTemplateProperty, value);
    }

    /// <summary>Gets or sets whether the pane toggle button is visible. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsPaneToggleButtonVisible {
        get => (bool)GetValue(IsPaneToggleButtonVisibleProperty);
        set => SetValue(IsPaneToggleButtonVisibleProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        paneRoot = GetTemplateChild(PaneRootPartName) as FrameworkElement;
        pill = GetTemplateChild(SelectionPillPartName) as FrameworkElement;
        itemsHost = GetTemplateChild(ItemsHostPartName) as FrameworkElement;
        if (paneRoot is not null) {
            paneRoot.Width = IsPaneOpen ? OpenPaneLength : CompactPaneLength;
        }
        SchedulePill(false);
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) {
        return item is NavigationViewItem;
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() {
        return new NavigationViewItem();
    }

    /// <inheritdoc />
    protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
        base.OnSelectionChanged(e);
        SchedulePill(true);
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
        base.OnItemsChanged(e);
        SchedulePill(false);
    }

    private static void OnPaneLengthChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((NavigationView)d).AnimatePane();
    }

    private void AnimatePane() {
        if (paneRoot is null) {
            return;
        }
        double target = IsPaneOpen ? OpenPaneLength : CompactPaneLength;
        if (!IsLoaded || !Jelly.IsMotionEnabled(this)) {
            paneRoot.BeginAnimation(WidthProperty, null);
            paneRoot.Width = target;
            return;
        }
        DoubleAnimation resize = new(target, TimeSpan.FromMilliseconds(520)) {
            EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.BackSoftKey)
        };
        paneRoot.BeginAnimation(WidthProperty, resize);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        SchedulePill(false);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e) {
        if (e.HeightChanged) {
            SchedulePill(false);
        }
    }

    private void SchedulePill(bool animate) {
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => UpdatePill(animate)));
    }

    private void UpdatePill(bool animate) {
        if ((pill is null) || (itemsHost is null)) {
            return;
        }
        FrameworkElement? container = SelectedItem is null ? null : ItemContainerGenerator.ContainerFromItem(SelectedItem) as FrameworkElement;
        PillAnimator.MoveTo(pill, container, itemsHost, animate, trackX: false, trackY: true);
    }
}