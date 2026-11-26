using System;
using System.Collections.Generic;
using System.Windows;

namespace Muzzle.Theme.Controls;

/// <summary>Weakly tracks loaded overlay hosts (Growl, Dialog, Drawer containers) so static APIs can find them by token.</summary>
internal sealed class TokenRegistry<T> where T : FrameworkElement {
    private readonly List<WeakReference<T>> items = [];

    public void Add(T item) {
        Prune();
        foreach (WeakReference<T> reference in items) {
            if (reference.TryGetTarget(out T? existing) && ReferenceEquals(existing, item)) {
                return;
            }
        }
        items.Add(new WeakReference<T>(item));
    }

    public void Remove(T item) {
        items.RemoveAll(reference => !reference.TryGetTarget(out T? existing) || ReferenceEquals(existing, item));
    }

    /// <summary>Live hosts, most recently registered first.</summary>
    public List<T> Alive() {
        Prune();
        List<T> alive = [];
        for (int i = items.Count - 1; i >= 0; i--) {
            if (items[i].TryGetTarget(out T? item)) {
                alive.Add(item);
            }
        }
        return alive;
    }

    /// <summary>Finds the host registered with <paramref name="token"/>, or (for no token) the untokened host in the active window.</summary>
    public T? Find(string? token, Func<T, string?> getToken) {
        List<T> alive = Alive();
        if (!string.IsNullOrEmpty(token)) {
            return alive.Find(item => string.Equals(getToken(item), token, StringComparison.Ordinal));
        }
        Window? active = OverlayHost.ActiveWindow();
        T? inActive = alive.Find(item => string.IsNullOrEmpty(getToken(item)) && item.IsVisible && ReferenceEquals(Window.GetWindow(item), active));
        return inActive ?? alive.Find(item => string.IsNullOrEmpty(getToken(item)) && item.IsVisible);
    }

    private void Prune() {
        items.RemoveAll(static reference => !reference.TryGetTarget(out _));
    }
}