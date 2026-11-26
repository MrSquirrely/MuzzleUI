using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Shared drop-down plumbing for <see cref="DropDownButton"/> and <see cref="SplitButton"/>: a springy Muzzle popup
/// holding <see cref="DropDownContent"/> or a list of <see cref="MenuItems"/> (any objects; <c>MenuItem</c>s get the
/// Muzzle menu look). Clicking a menu item, pressing Esc or clicking outside closes it.
/// </summary>
[ContentProperty(nameof(Content))]
[TemplatePart(Name = PopupPartName, Type = typeof(Popup))]
[TemplatePart(Name = ToggleButtonPartName, Type = typeof(ButtonBase))]
public abstract class DropDownButtonBase : ContentControl {
    /// <summary>Name of the Popup template part that holds the drop-down.</summary>
    public const string PopupPartName = "PART_Popup";
    /// <summary>Name of the button template part that toggles the drop-down.</summary>
    public const string ToggleButtonPartName = "PART_ToggleButton";
    /// <summary>Name of the menu card template part inside the popup (it springs open from the button).</summary>
    public const string MenuCardPartName = "PART_MenuCard";

    /// <summary>Identifies the <see cref="DropDownContent"/> dependency property.</summary>
    public static readonly DependencyProperty DropDownContentProperty = DependencyProperty.Register(
        nameof(DropDownContent), typeof(object), typeof(DropDownButtonBase), new PropertyMetadata(null, OnDropDownContentChanged));

    /// <summary>Identifies the <see cref="IsDropDownOpen"/> dependency property.</summary>
    public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
        nameof(IsDropDownOpen), typeof(bool), typeof(DropDownButtonBase),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

    /// <summary>Identifies the <see cref="Placement"/> dependency property.</summary>
    public static readonly DependencyProperty PlacementProperty = DependencyProperty.Register(
        nameof(Placement), typeof(PlacementMode), typeof(DropDownButtonBase), new PropertyMetadata(PlacementMode.Bottom));

    /// <summary>The Button style used for the segments (default Muzzle.Button; try Muzzle.Button.Primary).</summary>
    public static readonly DependencyProperty SegmentStyleProperty = DependencyProperty.Register(
        nameof(SegmentStyle), typeof(Style), typeof(DropDownButtonBase), new PropertyMetadata(null));

    private const long LightDismissWindowMilliseconds = 500;

    private Popup? popup;
    private ButtonBase? toggleButton;
    private long lastLightDismissTick = long.MinValue / 2;
    private bool closingOurselves;

    /// <summary>Initializes a new instance of the <see cref="DropDownButtonBase"/> class.</summary>
    protected DropDownButtonBase() {
        MenuStyling.RegisterImplicitStyles(Resources);
        MenuItems = [];
        MenuItems.CollectionChanged += (_, e) => {
            foreach (object item in e.NewItems ?? Array.Empty<object>()) {
                MenuStyling.Apply(item);
            }
        };
        AddHandler(MenuItem.ClickEvent, new RoutedEventHandler(OnMenuItemClick));
    }

    /// <summary>Gets or sets the drop down content. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? DropDownContent {
        get => GetValue(DropDownContentProperty);
        set => SetValue(DropDownContentProperty, value);
    }

    /// <summary>Menu entries shown when <see cref="DropDownContent"/> is not set.</summary>
    public ObservableCollection<object> MenuItems { get; }

    /// <summary>Gets or sets whether the drop down is open. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsDropDownOpen {
        get => (bool)GetValue(IsDropDownOpenProperty);
        set => SetValue(IsDropDownOpenProperty, value);
    }

    /// <summary>Gets or sets the placement. The default is <c>PlacementMode.Bottom</c>. This is a dependency property.</summary>
    public PlacementMode Placement {
        get => (PlacementMode)GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
    }

    /// <summary>Gets or sets the segment style. The default is <see langword="null"/>. This is a dependency property.</summary>
    public Style? SegmentStyle {
        get => (Style?)GetValue(SegmentStyleProperty);
        set => SetValue(SegmentStyleProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (popup is not null) {
            popup.Opened -= OnPopupOpened;
            popup.Closed -= OnPopupClosed;
        }
        if (toggleButton is not null) {
            toggleButton.Click -= OnToggleClick;
        }
        base.OnApplyTemplate();
        popup = GetTemplateChild(PopupPartName) as Popup;
        toggleButton = GetTemplateChild(ToggleButtonPartName) as ButtonBase;
        if (popup is not null) {
            popup.Opened += OnPopupOpened;
            popup.Closed += OnPopupClosed;
        }
        if (toggleButton is not null) {
            toggleButton.Click += OnToggleClick;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e) {
        base.OnPreviewKeyDown(e);
        if ((e.Key == Key.Escape) && IsDropDownOpen) {
            Close();
            toggleButton?.Focus();
            e.Handled = true;
        }
        else if ((e.Key == Key.Down) && Keyboard.Modifiers.HasFlag(ModifierKeys.Alt) && !IsDropDownOpen) {
            Open();
            e.Handled = true;
        }
    }

    /// <summary>Content plus the drop-down content: both are logical children (DataContext and implicit styles flow in).</summary>
    protected override System.Collections.IEnumerator LogicalChildren {
        get {
            System.Collections.Generic.List<object> children = [];
            System.Collections.IEnumerator inherited = base.LogicalChildren;
            while ((inherited is not null) && inherited.MoveNext()) {
                children.Add(inherited.Current);
            }
            if (DropDownContent is not null) {
                children.Add(DropDownContent);
            }
            return children.GetEnumerator();
        }
    }

    private static void OnDropDownContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        DropDownButtonBase owner = (DropDownButtonBase)d;
        if (e.OldValue is not null) {
            owner.RemoveLogicalChild(e.OldValue);
        }
        if (e.NewValue is not null) {
            owner.AddLogicalChild(e.NewValue);
        }
        MenuStyling.ApplySoon(e.NewValue);
    }

    /// <summary>Opens the drop-down.</summary>
    public void Open() {
        SetCurrentValue(IsDropDownOpenProperty, true);
    }

    /// <summary>Closes the drop-down.</summary>
    public void Close() {
        if (IsDropDownOpen) {
            closingOurselves = true;
            SetCurrentValue(IsDropDownOpenProperty, false);
        }
    }

    private void OnToggleClick(object sender, RoutedEventArgs e) {
        // The click that light-dismissed the popup must not reopen it on release.
        if (!IsDropDownOpen && ((Environment.TickCount64 - lastLightDismissTick) < LightDismissWindowMilliseconds)) {
            return;
        }
        bool open = !IsDropDownOpen;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => {
            if (open) {
                Open();
            }
            else {
                Close();
            }
        }));
    }

    private void OnMenuItemClick(object sender, RoutedEventArgs e) {
        if (e.OriginalSource is MenuItem { HasItems: false }) {
            Close();
        }
    }

    private void OnPopupOpened(object? sender, EventArgs e) {
        if ((popup?.Child is not FrameworkElement root) || !Jelly.IsMotionEnabled(this)) {
            return;
        }
        FrameworkElement card = (GetTemplateChild(MenuCardPartName) as FrameworkElement) ?? root;
        card.RenderTransformOrigin = new Point(0.5, 0);
        ScaleTransform scale = Jelly.EnsureScale(card);
        DoubleAnimation pop = new(0.88, 1, new Duration(TimeSpan.FromMilliseconds(460))) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.BackKey) };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, new Duration(TimeSpan.FromMilliseconds(140))));
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => card.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))));
    }

    private void OnPopupClosed(object? sender, EventArgs e) {
        if (!closingOurselves) {
            lastLightDismissTick = Environment.TickCount64;
        }
        closingOurselves = false;
    }
}

/// <summary>
/// Gives MenuItems that live outside a Menu (drop-down lists, tray cards) the Muzzle menu style up front. Without it
/// the stock MenuItem theme binds to an ItemsControl ancestor that a detached item doesn't have yet.
/// </summary>
internal static class MenuStyling {
    /// <summary>
    /// Styles now and again on the next dispatcher turn: XAML assigns panels to properties before filling them
    /// (top-down initialisation), and the second pass still runs before bindings resolve.
    /// </summary>
    /// <summary>Makes MenuItems / Separators under an owner's logical tree pick up the Muzzle menu styles implicitly.</summary>
    public static void RegisterImplicitStyles(ResourceDictionary resources) {
        if (Application.Current?.TryFindResource("Muzzle.MenuItem") is Style itemStyle) {
            resources[typeof(MenuItem)] = itemStyle;
        }
        if (Application.Current?.TryFindResource("Muzzle.MenuSeparator") is Style separatorStyle) {
            resources[typeof(Separator)] = separatorStyle;
        }
    }

    public static void ApplySoon(object? content) {
        Apply(content);
        if (content is DispatcherObject owner) {
            owner.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() => Apply(content)));
        }
    }

    public static void Apply(object? content) {
        if (content is not DependencyObject root) {
            return;
        }
        if ((root is MenuItem item) && (item.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
            && (Application.Current?.TryFindResource("Muzzle.MenuItem") is Style style)) {
            item.Style = style;
        }
        else if ((root is Separator separator) && (separator.ReadLocalValue(FrameworkElement.StyleProperty) == DependencyProperty.UnsetValue)
            && (Application.Current?.TryFindResource("Muzzle.MenuSeparator") is Style separatorStyle)) {
            separator.Style = separatorStyle;
        }
        foreach (object child in LogicalTreeHelper.GetChildren(root)) {
            Apply(child);
        }
    }
}

/// <summary>A button whose whole face opens a drop-down menu; a chevron flips while it is open.</summary>
public class DropDownButton : DropDownButtonBase {
    static DropDownButton() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DropDownButton), new FrameworkPropertyMetadata(typeof(DropDownButton)));
    }
}

/// <summary>
/// A primary action button paired with a chevron pill that opens a drop-down menu. Each segment squishes on its own.
/// The primary segment raises <see cref="Click"/> and runs <see cref="Command"/>.
/// </summary>
[TemplatePart(Name = PrimaryButtonPartName, Type = typeof(ButtonBase))]
public class SplitButton : DropDownButtonBase {
    /// <summary>Name of the primary-action button template part.</summary>
    public const string PrimaryButtonPartName = "PART_PrimaryButton";

    /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
        nameof(Command), typeof(ICommand), typeof(SplitButton), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
        nameof(CommandParameter), typeof(object), typeof(SplitButton), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Click"/> routed event.</summary>
    public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
        nameof(Click), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SplitButton));

    private ButtonBase? primaryButton;

    static SplitButton() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));
    }

    /// <summary>Occurs when the primary segment is clicked (before <see cref="Command"/> runs).</summary>
    public event RoutedEventHandler Click {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    /// <summary>Gets or sets the command. The default is <see langword="null"/>. This is a dependency property.</summary>
    public ICommand? Command {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>Gets or sets the command parameter. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? CommandParameter {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (primaryButton is not null) {
            primaryButton.Click -= OnPrimaryClick;
        }
        base.OnApplyTemplate();
        primaryButton = GetTemplateChild(PrimaryButtonPartName) as ButtonBase;
        if (primaryButton is not null) {
            primaryButton.Click += OnPrimaryClick;
        }
    }

    private void OnPrimaryClick(object sender, RoutedEventArgs e) {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(ClickEvent, this));
        if ((Command is ICommand command) && command.CanExecute(CommandParameter)) {
            command.Execute(CommandParameter);
        }
    }
}