using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Muzzle.Theme;

/// <summary>
/// Keeps keyboard focus inside a container (a dialog, drawer or command palette): Tab and Shift+Tab cycle through
/// its focusable children instead of escaping. With <c>FocusFirstOnShow</c>, the first focusable child gets focus
/// whenever the container becomes visible.
/// </summary>
public static class FocusTrap {
    /// <summary>Identifies the <c>IsEnabled</c> attached property.</summary>
    public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(FocusTrap), new PropertyMetadata(false, OnIsEnabledChanged));

    /// <summary>Identifies the <c>FocusFirstOnShow</c> attached property.</summary>
    public static readonly DependencyProperty FocusFirstOnShowProperty = DependencyProperty.RegisterAttached(
        "FocusFirstOnShow", typeof(bool), typeof(FocusTrap), new PropertyMetadata(false));

    /// <summary>Gets the value of the <c>IsEnabled</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>IsEnabled</c> value.</returns>
    public static bool GetIsEnabled(DependencyObject element) {
        return (bool)element.GetValue(IsEnabledProperty);
    }

    /// <summary>Sets the value of the <c>IsEnabled</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetIsEnabled(DependencyObject element, bool value) {
        element.SetValue(IsEnabledProperty, value);
    }

    /// <summary>Gets the value of the <c>FocusFirstOnShow</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>FocusFirstOnShow</c> value.</returns>
    public static bool GetFocusFirstOnShow(DependencyObject element) {
        return (bool)element.GetValue(FocusFirstOnShowProperty);
    }

    /// <summary>Sets the value of the <c>FocusFirstOnShow</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetFocusFirstOnShow(DependencyObject element, bool value) {
        element.SetValue(FocusFirstOnShowProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not UIElement element) {
            return;
        }
        element.PreviewLostKeyboardFocus -= OnPreviewLostKeyboardFocus;
        element.IsVisibleChanged -= OnIsVisibleChanged;
        if (e.NewValue is true) {
            KeyboardNavigation.SetTabNavigation(element, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetControlTabNavigation(element, KeyboardNavigationMode.Cycle);
            KeyboardNavigation.SetDirectionalNavigation(element, KeyboardNavigationMode.Contained);
            element.PreviewLostKeyboardFocus += OnPreviewLostKeyboardFocus;
            element.IsVisibleChanged += OnIsVisibleChanged;
        }
        else {
            element.ClearValue(KeyboardNavigation.TabNavigationProperty);
            element.ClearValue(KeyboardNavigation.ControlTabNavigationProperty);
            element.ClearValue(KeyboardNavigation.DirectionalNavigationProperty);
        }
    }

    // Belt and braces: a Tab that would still land outside (e.g. via a nested scope) is cancelled.
    private static void OnPreviewLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) {
        if ((sender is not UIElement container) || !container.IsVisible || !Keyboard.IsKeyDown(Key.Tab)) {
            return;
        }
        if ((e.NewFocus is DependencyObject target) && !IsInside(target, container)) {
            e.Handled = true;
        }
    }

    private static void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
        if ((e.NewValue is true) && (sender is UIElement container) && GetFocusFirstOnShow(container) && !container.IsKeyboardFocusWithin) {
            container.Dispatcher.BeginInvoke(new System.Action(() => container.MoveFocus(new TraversalRequest(FocusNavigationDirection.First))),
                System.Windows.Threading.DispatcherPriority.Input);
        }
    }

    private static bool IsInside(DependencyObject target, DependencyObject container) {
        DependencyObject? current = target;
        while (current is not null) {
            if (ReferenceEquals(current, container)) {
                return true;
            }
            current = (current is Visual or System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current);
        }
        return false;
    }
}

/// <summary>
/// Binds the Esc key (pressed while focus is inside the element) to a command and/or to an <c>IsOpen</c> flag that
/// is set back to false, e.g. <c>mz:EscapeKeyBehavior.IsOpen="{Binding IsPanelOpen}"</c>.
/// </summary>
public static class EscapeKeyBehavior {
    /// <summary>Identifies the <c>Command</c> attached property.</summary>
    public static readonly DependencyProperty CommandProperty = DependencyProperty.RegisterAttached(
        "Command", typeof(ICommand), typeof(EscapeKeyBehavior), new PropertyMetadata(null, OnHookChanged));

    /// <summary>Identifies the <c>CommandParameter</c> attached property.</summary>
    public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.RegisterAttached(
        "CommandParameter", typeof(object), typeof(EscapeKeyBehavior), new PropertyMetadata(null));

    /// <summary>Identifies the <c>IsOpen</c> attached property.</summary>
    public static readonly DependencyProperty IsOpenProperty = DependencyProperty.RegisterAttached(
        "IsOpen", typeof(bool), typeof(EscapeKeyBehavior),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnHookChanged));

    /// <summary>Gets the value of the <c>Command</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Command</c> value.</returns>
    public static ICommand? GetCommand(DependencyObject element) {
        return (ICommand?)element.GetValue(CommandProperty);
    }

    /// <summary>Sets the value of the <c>Command</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetCommand(DependencyObject element, ICommand? value) {
        element.SetValue(CommandProperty, value);
    }

    /// <summary>Gets the value of the <c>CommandParameter</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>CommandParameter</c> value.</returns>
    public static object? GetCommandParameter(DependencyObject element) {
        return element.GetValue(CommandParameterProperty);
    }

    /// <summary>Sets the value of the <c>CommandParameter</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetCommandParameter(DependencyObject element, object? value) {
        element.SetValue(CommandParameterProperty, value);
    }

    /// <summary>Gets the value of the <c>IsOpen</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>IsOpen</c> value.</returns>
    public static bool GetIsOpen(DependencyObject element) {
        return (bool)element.GetValue(IsOpenProperty);
    }

    /// <summary>Sets the value of the <c>IsOpen</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetIsOpen(DependencyObject element, bool value) {
        element.SetValue(IsOpenProperty, value);
    }

    private static void OnHookChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is UIElement element) {
            element.PreviewKeyDown -= OnPreviewKeyDown;
            element.PreviewKeyDown += OnPreviewKeyDown;
        }
    }

    private static void OnPreviewKeyDown(object sender, KeyEventArgs e) {
        if ((e.Key != Key.Escape) || e.Handled || (sender is not DependencyObject element)) {
            return;
        }
        bool acted = false;
        if (GetCommand(element) is ICommand command) {
            object? parameter = GetCommandParameter(element);
            if (command.CanExecute(parameter)) {
                command.Execute(parameter);
                acted = true;
            }
        }
        if (GetIsOpen(element)) {
            element.SetCurrentValue(IsOpenProperty, false);
            acted = true;
        }
        e.Handled = acted;
    }
}