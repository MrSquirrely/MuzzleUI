using System;
using System.Windows;

using Muzzle.Theme;

namespace Muzzle.Gallery;

/// <summary>Runs a refresh action whenever the Muzzle theme changes, while the element is loaded.</summary>
public static class ThemeAware {
    public static void Attach(FrameworkElement element, Action refresh) {
        void OnChanged(object? sender, MuzzleThemeChangedEventArgs e) {
            refresh();
        }

        element.Loaded += (_, _) => MuzzleThemeManager.ThemeChanged += OnChanged;
        element.Unloaded += (_, _) => MuzzleThemeManager.ThemeChanged -= OnChanged;
    }
}