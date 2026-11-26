using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Muzzle.Theme.Controls;

/// <summary>Data for <see cref="BreadcrumbBar.ItemClicked"/>.</summary>
public sealed class BreadcrumbItemClickedEventArgs(RoutedEvent routedEvent, object source, object? item, int index) : RoutedEventArgs(routedEvent, source) {
    /// <summary>The data item (or the <see cref="BreadcrumbItem"/> itself when declared directly).</summary>
    public object? Item { get; } = item;

    /// <summary>Gets the zero-based index of the clicked segment.</summary>
    public int Index { get; } = index;
}

/// <summary>Handles <see cref="BreadcrumbBar.ItemClicked"/>.</summary>
/// <param name="sender">The breadcrumb bar.</param>
/// <param name="e">The clicked item and its index.</param>
public delegate void BreadcrumbItemClickedEventHandler(object sender, BreadcrumbItemClickedEventArgs e);

/// <summary>
/// A path of clickable segments separated by animated chevrons. Use <c>ItemsSource</c> or declare
/// <see cref="BreadcrumbItem"/>s. When space runs out, middle segments collapse into an ellipsis and the last segment trims.
/// </summary>
public class BreadcrumbBar : ItemsControl {
    /// <summary>Identifies the <see cref="ItemClicked"/> routed event.</summary>
    public static readonly RoutedEvent ItemClickedEvent = EventManager.RegisterRoutedEvent(
        nameof(ItemClicked), RoutingStrategy.Bubble, typeof(BreadcrumbItemClickedEventHandler), typeof(BreadcrumbBar));

    /// <summary>Executed with the clicked data item as parameter.</summary>
    public static readonly DependencyProperty ItemCommandProperty = DependencyProperty.Register(
        nameof(ItemCommand), typeof(ICommand), typeof(BreadcrumbBar), new PropertyMetadata(null));

    static BreadcrumbBar() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BreadcrumbBar), new FrameworkPropertyMetadata(typeof(BreadcrumbBar)));
        FocusableProperty.OverrideMetadata(typeof(BreadcrumbBar), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="BreadcrumbBar"/> class.</summary>
    public BreadcrumbBar() {
        AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnItemClick));
    }

    /// <summary>Occurs when a breadcrumb segment is clicked; reports the item and its index.</summary>
    public event BreadcrumbItemClickedEventHandler ItemClicked {
        add => AddHandler(ItemClickedEvent, value);
        remove => RemoveHandler(ItemClickedEvent, value);
    }

    /// <summary>Gets or sets the item command. The default is <see langword="null"/>. This is a dependency property.</summary>
    public ICommand? ItemCommand {
        get => (ICommand?)GetValue(ItemCommandProperty);
        set => SetValue(ItemCommandProperty, value);
    }

    /// <inheritdoc />
    protected override bool IsItemItsOwnContainerOverride(object item) {
        return item is BreadcrumbItem;
    }

    /// <inheritdoc />
    protected override DependencyObject GetContainerForItemOverride() {
        return new BreadcrumbItem();
    }

    /// <inheritdoc />
    protected override void PrepareContainerForItemOverride(DependencyObject element, object item) {
        base.PrepareContainerForItemOverride(element, item);
        Dispatcher.BeginInvoke(new Action(UpdatePositions));
    }

    /// <inheritdoc />
    protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e) {
        base.OnItemsChanged(e);
        Dispatcher.BeginInvoke(new Action(UpdatePositions));
    }

    private void UpdatePositions() {
        int count = Items.Count;
        for (int i = 0; i < count; i++) {
            if (ItemContainerGenerator.ContainerFromIndex(i) is BreadcrumbItem container) {
                container.SetPosition(i == 0, i == count - 1);
            }
        }
    }

    private void OnItemClick(object sender, RoutedEventArgs e) {
        if (e.OriginalSource is not BreadcrumbItem container) {
            return;
        }
        int index = ItemContainerGenerator.IndexFromContainer(container);
        object? item = index >= 0 ? Items[index] : null;
        RaiseEvent(new BreadcrumbItemClickedEventArgs(ItemClickedEvent, this, item, index));
        ICommand? command = ItemCommand;
        if ((command is not null) && command.CanExecute(item)) {
            command.Execute(item);
        }
    }
}

/// <summary>One segment of a <see cref="BreadcrumbBar"/>. The first hides its leading chevron; the last is the current location.</summary>
public class BreadcrumbItem : ButtonBase {
    private static readonly DependencyPropertyKey IsFirstPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsFirst), typeof(bool), typeof(BreadcrumbItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsFirst"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsFirstProperty = IsFirstPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsLastPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsLast), typeof(bool), typeof(BreadcrumbItem), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsLast"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsLastProperty = IsLastPropertyKey.DependencyProperty;

    static BreadcrumbItem() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(BreadcrumbItem), new FrameworkPropertyMetadata(typeof(BreadcrumbItem)));
    }

    /// <summary>Gets whether this is the first item. This is a read-only dependency property.</summary>
    public bool IsFirst => (bool)GetValue(IsFirstProperty);

    /// <summary>Gets whether this is the last item. This is a read-only dependency property.</summary>
    public bool IsLast => (bool)GetValue(IsLastProperty);

    internal void SetPosition(bool first, bool last) {
        SetValue(IsFirstPropertyKey, first);
        SetValue(IsLastPropertyKey, last);
    }
}