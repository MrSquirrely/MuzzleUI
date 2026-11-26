using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Muzzle.Theme;

/// <summary>
/// Mouse-wheel helpers for a <see cref="ScrollViewer"/> (or any element containing one):
/// <c>EnableSmoothScroll</c> glides to the wheel target instead of jumping, and <c>EnableShiftHorizontalScroll</c>
/// scrolls sideways while Shift is held. At either end the wheel passes through to an outer scroller.
/// </summary>
public static class ScrollViewerAssist {
    /// <summary>Identifies the <c>EnableSmoothScroll</c> attached property.</summary>
    public static readonly DependencyProperty EnableSmoothScrollProperty = DependencyProperty.RegisterAttached(
        "EnableSmoothScroll", typeof(bool), typeof(ScrollViewerAssist), new PropertyMetadata(false, OnBehaviorChanged));

    /// <summary>Identifies the <c>EnableShiftHorizontalScroll</c> attached property.</summary>
    public static readonly DependencyProperty EnableShiftHorizontalScrollProperty = DependencyProperty.RegisterAttached(
        "EnableShiftHorizontalScroll", typeof(bool), typeof(ScrollViewerAssist), new PropertyMetadata(false, OnBehaviorChanged));

    /// <summary>DIPs scrolled per wheel notch (default 96).</summary>
    public static readonly DependencyProperty WheelStepProperty = DependencyProperty.RegisterAttached(
        "WheelStep", typeof(double), typeof(ScrollViewerAssist), new PropertyMetadata(96.0));

    private static readonly DependencyProperty StateProperty = DependencyProperty.RegisterAttached(
        "State", typeof(SmoothState), typeof(ScrollViewerAssist), new PropertyMetadata(null));

    /// <summary>Gets the value of the <c>EnableSmoothScroll</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>EnableSmoothScroll</c> value.</returns>
    public static bool GetEnableSmoothScroll(DependencyObject element) {
        return (bool)element.GetValue(EnableSmoothScrollProperty);
    }

    /// <summary>Sets the value of the <c>EnableSmoothScroll</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetEnableSmoothScroll(DependencyObject element, bool value) {
        element.SetValue(EnableSmoothScrollProperty, value);
    }

    /// <summary>Gets the value of the <c>EnableShiftHorizontalScroll</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>EnableShiftHorizontalScroll</c> value.</returns>
    public static bool GetEnableShiftHorizontalScroll(DependencyObject element) {
        return (bool)element.GetValue(EnableShiftHorizontalScrollProperty);
    }

    /// <summary>Sets the value of the <c>EnableShiftHorizontalScroll</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetEnableShiftHorizontalScroll(DependencyObject element, bool value) {
        element.SetValue(EnableShiftHorizontalScrollProperty, value);
    }

    /// <summary>Gets the value of the <c>WheelStep</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>WheelStep</c> value.</returns>
    public static double GetWheelStep(DependencyObject element) {
        return (double)element.GetValue(WheelStepProperty);
    }

    /// <summary>Sets the value of the <c>WheelStep</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetWheelStep(DependencyObject element, double value) {
        element.SetValue(WheelStepProperty, value);
    }

    private static void OnBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        if (d is not UIElement element) {
            return;
        }
        element.PreviewMouseWheel -= OnPreviewMouseWheel;
        if (GetEnableSmoothScroll(element) || GetEnableShiftHorizontalScroll(element)) {
            element.PreviewMouseWheel += OnPreviewMouseWheel;
        }
    }

    private static void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
        if (e.Handled || (sender is not DependencyObject owner) || (FindScrollViewer(owner) is not ScrollViewer viewer)) {
            return;
        }
        // Let an inner scroller that can still move have the wheel first.
        if (FindInnerScrollable(e.OriginalSource as DependencyObject, viewer, e.Delta, Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))) {
            return;
        }
        bool horizontal = GetEnableShiftHorizontalScroll(owner) && Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) && (viewer.ScrollableWidth > 0);
        if (!horizontal && !GetEnableSmoothScroll(owner)) {
            return;
        }
        double notches = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        bool itemScrolling = viewer.CanContentScroll;
        double step = itemScrolling ? 3 : GetWheelStep(owner);
        SmoothState state = GetState(viewer);
        double current = horizontal ? (state.IsAnimatingX ? state.TargetX : viewer.HorizontalOffset) : (state.IsAnimatingY ? state.TargetY : viewer.VerticalOffset);
        double limit = horizontal ? viewer.ScrollableWidth : viewer.ScrollableHeight;
        double target = Math.Clamp(current - (notches * step), 0, limit);
        if (Math.Abs(target - current) < 0.01) {
            return; // At the end: let the wheel bubble to an outer scroller.
        }
        e.Handled = true;
        bool smooth = GetEnableSmoothScroll(owner) && !itemScrolling && SystemParameters.ClientAreaAnimation;
        if (!smooth) {
            if (horizontal) {
                viewer.ScrollToHorizontalOffset(target);
            }
            else {
                viewer.ScrollToVerticalOffset(target);
            }
            return;
        }
        if (horizontal) {
            state.TargetX = target;
            state.IsAnimatingX = true;
        }
        else {
            state.TargetY = target;
            state.IsAnimatingY = true;
        }
        state.Start();
    }

    private static SmoothState GetState(ScrollViewer viewer) {
        if (viewer.GetValue(StateProperty) is not SmoothState state) {
            state = new SmoothState(viewer);
            viewer.SetValue(StateProperty, state);
        }
        return state;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root) {
        if (root is ScrollViewer viewer) {
            return viewer;
        }
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            if (FindScrollViewer(VisualTreeHelper.GetChild(root, i)) is ScrollViewer found) {
                return found;
            }
        }
        return null;
    }

    private static bool FindInnerScrollable(DependencyObject? source, ScrollViewer outer, int delta, bool shift) {
        while ((source is not null) && !ReferenceEquals(source, outer)) {
            if (source is ScrollViewer inner) {
                bool horizontal = shift && (inner.ScrollableWidth > 0);
                double offset = horizontal ? inner.HorizontalOffset : inner.VerticalOffset;
                double limit = horizontal ? inner.ScrollableWidth : inner.ScrollableHeight;
                if (((delta > 0) && (offset > 0)) || ((delta < 0) && (offset < limit))) {
                    return true;
                }
            }
            source = (source is Visual or System.Windows.Media.Media3D.Visual3D) ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }

    /// <summary>Per-viewer glide: each frame moves a fraction of the way to the target.</summary>
    private sealed class SmoothState(ScrollViewer viewer) {
        private bool running;

        public double TargetX { get; set; }

        public double TargetY { get; set; }

        public bool IsAnimatingX { get; set; }

        public bool IsAnimatingY { get; set; }

        public void Start() {
            if (running) {
                return;
            }
            running = true;
            CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e) {
            if (IsAnimatingY) {
                double next = Step(viewer.VerticalOffset, TargetY);
                viewer.ScrollToVerticalOffset(next);
                IsAnimatingY = Math.Abs(next - TargetY) > 0.5;
                if (!IsAnimatingY) {
                    viewer.ScrollToVerticalOffset(TargetY);
                }
            }
            if (IsAnimatingX) {
                double next = Step(viewer.HorizontalOffset, TargetX);
                viewer.ScrollToHorizontalOffset(next);
                IsAnimatingX = Math.Abs(next - TargetX) > 0.5;
                if (!IsAnimatingX) {
                    viewer.ScrollToHorizontalOffset(TargetX);
                }
            }
            if (!IsAnimatingX && !IsAnimatingY) {
                running = false;
                CompositionTarget.Rendering -= OnRendering;
            }
        }

        private static double Step(double current, double target) {
            return current + ((target - current) * 0.2);
        }
    }
}