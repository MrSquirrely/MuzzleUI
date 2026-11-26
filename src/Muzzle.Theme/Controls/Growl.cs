using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// HandyControl-style toast notifications.
/// <para>Host them in a window by setting <c>mz:Growl.GrowlParent="True"</c> on a Grid (a <see cref="GrowlContainer"/> is
/// added to its top-right corner), or place a <see cref="GrowlContainer"/> yourself. Without any registered container,
/// toasts appear in a transparent desktop-corner <see cref="GrowlWindow"/>.</para>
/// </summary>
public static class Growl {
    /// <summary>Set to true on a Panel (ideally a Grid) to add a growl container to it.</summary>
    public static readonly DependencyProperty GrowlParentProperty = DependencyProperty.RegisterAttached(
        "GrowlParent",
        typeof(bool),
        typeof(Growl),
        new PropertyMetadata(false, OnGrowlParentChanged));

    /// <summary>Names a container so toasts can target it with a token.</summary>
    public static readonly DependencyProperty TokenProperty = DependencyProperty.RegisterAttached(
        "Token",
        typeof(string),
        typeof(Growl),
        new PropertyMetadata(null, OnTokenChanged));

    private static readonly DependencyProperty AttachedContainerProperty = DependencyProperty.RegisterAttached(
        "AttachedContainer",
        typeof(GrowlContainer),
        typeof(Growl),
        new PropertyMetadata(null));

    internal static TokenRegistry<GrowlContainer> Containers { get; } = new();

    /// <summary>Gets the value of the <c>GrowlParent</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>GrowlParent</c> value.</returns>
    public static bool GetGrowlParent(DependencyObject element) {
        return (bool)element.GetValue(GrowlParentProperty);
    }

    /// <summary>Sets the value of the <c>GrowlParent</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetGrowlParent(DependencyObject element, bool value) {
        element.SetValue(GrowlParentProperty, value);
    }

    /// <summary>Gets the value of the <c>Token</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Token</c> value.</returns>
    public static string? GetToken(DependencyObject element) {
        return (string?)element.GetValue(TokenProperty);
    }

    /// <summary>Sets the value of the <c>Token</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetToken(DependencyObject element, string? value) {
        element.SetValue(TokenProperty, value);
    }

    /// <summary>Shows an informational toast.</summary>
    /// <param name="message">The toast text.</param>
    /// <param name="title">An optional bold title.</param>
    /// <param name="token">Targets the container whose <c>Growl.Token</c> matches; <see langword="null"/> uses the active window's container, or the desktop corner when there is none.</param>
    /// <param name="waitTime">How long before it dismisses itself; <see langword="null"/> uses the default.</param>
    /// <returns>The toast card, or <see langword="null"/> when called from a background thread (it is then shown asynchronously).</returns>
    public static GrowlCard? Info(string message, string? title = null, string? token = null, TimeSpan? waitTime = null) {
        return Show(new GrowlInfo { Message = message, Title = title, Token = token, WaitTime = waitTime, Type = GrowlType.Info });
    }

    /// <summary>Shows a success toast.</summary>
    /// <param name="message">The toast text.</param>
    /// <param name="title">An optional bold title.</param>
    /// <param name="token">Targets the container whose <c>Growl.Token</c> matches; <see langword="null"/> uses the active window's container, or the desktop corner when there is none.</param>
    /// <param name="waitTime">How long before it dismisses itself; <see langword="null"/> uses the default.</param>
    /// <returns>The toast card, or <see langword="null"/> when called from a background thread (it is then shown asynchronously).</returns>
    public static GrowlCard? Success(string message, string? title = null, string? token = null, TimeSpan? waitTime = null) {
        return Show(new GrowlInfo { Message = message, Title = title, Token = token, WaitTime = waitTime, Type = GrowlType.Success });
    }

    /// <summary>Shows a warning toast.</summary>
    /// <param name="message">The toast text.</param>
    /// <param name="title">An optional bold title.</param>
    /// <param name="token">Targets the container whose <c>Growl.Token</c> matches; <see langword="null"/> uses the active window's container, or the desktop corner when there is none.</param>
    /// <param name="waitTime">How long before it dismisses itself; <see langword="null"/> uses the default.</param>
    /// <returns>The toast card, or <see langword="null"/> when called from a background thread (it is then shown asynchronously).</returns>
    public static GrowlCard? Warning(string message, string? title = null, string? token = null, TimeSpan? waitTime = null) {
        return Show(new GrowlInfo { Message = message, Title = title, Token = token, WaitTime = waitTime, Type = GrowlType.Warning });
    }

    /// <summary>Shows an error toast.</summary>
    /// <param name="message">The toast text.</param>
    /// <param name="title">An optional bold title.</param>
    /// <param name="token">Targets the container whose <c>Growl.Token</c> matches; <see langword="null"/> uses the active window's container, or the desktop corner when there is none.</param>
    /// <param name="waitTime">How long before it dismisses itself; <see langword="null"/> uses the default.</param>
    /// <returns>The toast card, or <see langword="null"/> when called from a background thread (it is then shown asynchronously).</returns>
    public static GrowlCard? Error(string message, string? title = null, string? token = null, TimeSpan? waitTime = null) {
        return Show(new GrowlInfo { Message = message, Title = title, Token = token, WaitTime = waitTime, Type = GrowlType.Error });
    }

    /// <summary>An error that stays until dismissed.</summary>
    public static GrowlCard? Fatal(string message, string? title = null, string? token = null) {
        return Show(new GrowlInfo { Message = message, Title = title, Token = token, Type = GrowlType.Fatal });
    }

    /// <summary>A question with Confirm / Cancel pills. <paramref name="callback"/> receives true when confirmed.</summary>
    public static GrowlCard? Ask(string message, Action<bool> callback, string? title = null, string? token = null) {
        return Show(new GrowlInfo { Message = message, Title = title, Token = token, Type = GrowlType.Ask, Callback = callback });
    }

    /// <summary>Shows a toast built from <paramref name="info"/>. Safe to call from any thread (returns null when marshalled).</summary>
    public static GrowlCard? Show(GrowlInfo info) {
        ArgumentNullException.ThrowIfNull(info);
        Dispatcher dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        if (!dispatcher.CheckAccess()) {
            dispatcher.BeginInvoke(new Action(() => Show(info)));
            return null;
        }
        GrowlCard card = new(info);
        GrowlContainer? container = info.IsGlobal ? null : Containers.Find(info.Token, GetToken);
        if (container is null) {
            GrowlWindow.Shared.Add(card);
        }
        else {
            container.Add(card);
        }
        return card;
    }

    /// <summary>Dismisses every toast in the container for <paramref name="token"/> (or the active window's container).</summary>
    public static void Clear(string? token = null) {
        Containers.Find(token, GetToken)?.CloseAll();
    }

    /// <summary>Dismisses every toast in the desktop-corner window.</summary>
    public static void ClearGlobal() {
        if (GrowlWindow.HasShared) {
            GrowlWindow.Shared.Container.CloseAll();
        }
    }

    /// <summary>Dismisses every toast everywhere.</summary>
    public static void ClearAll() {
        List<GrowlContainer> containers = Containers.Alive();
        foreach (GrowlContainer container in containers) {
            container.CloseAll();
        }
        ClearGlobal();
    }

    private static void OnGrowlParentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if ((d is not Panel panel) || (panel is GrowlContainer)) {
            return;
        }
        if (panel.GetValue(AttachedContainerProperty) is GrowlContainer existing) {
            panel.Children.Remove(existing);
            panel.ClearValue(AttachedContainerProperty);
        }
        if (e.NewValue is not true) {
            return;
        }
        GrowlContainer container = new();
        SetToken(container, GetToken(panel));
        Grid.SetRowSpan(container, 100);
        Grid.SetColumnSpan(container, 100);
        Panel.SetZIndex(container, 1000);
        panel.Children.Add(container);
        panel.SetValue(AttachedContainerProperty, container);
    }

    private static void OnTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d.GetValue(AttachedContainerProperty) is GrowlContainer container) {
            SetToken(container, (string?)e.NewValue);
        }
    }
}