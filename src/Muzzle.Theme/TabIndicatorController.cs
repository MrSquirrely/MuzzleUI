using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Muzzle.Theme;

/// <summary>
/// Slides a "pill" element (inside a TabControl template) underneath the selected TabItem
/// with a springy overshoot, and gently fades the newly selected content in.
/// </summary>
internal sealed class TabIndicatorController(FrameworkElement pill) {
    private TabControl? tabs;
    private FrameworkElement? container;
    private TranslateTransform? translate;
    private bool hasPlacement;

    public static TabIndicatorController Create(FrameworkElement pill) {
        TabIndicatorController controller = new(pill);
        pill.Loaded += controller.OnPillLoaded;
        pill.Unloaded += controller.OnPillUnloaded;
        if (pill.IsLoaded) {
            controller.Start();
        }
        return controller;
    }

    /// <inheritdoc />
    public void Dispose() {
        pill.Loaded -= OnPillLoaded;
        pill.Unloaded -= OnPillUnloaded;
        Stop();
    }

    private void OnPillLoaded(object sender, RoutedEventArgs e) {
        Start();
    }

    private void OnPillUnloaded(object sender, RoutedEventArgs e) {
        Stop();
    }

    private void Start() {
        if (tabs is not null) {
            return;
        }
        tabs = pill.TemplatedParent as TabControl;
        container = VisualTreeHelper.GetParent(pill) as FrameworkElement;
        if ((tabs is null) || (container is null)) {
            tabs = null;
            container = null;
            return;
        }
        translate = new TranslateTransform();
        pill.RenderTransform = translate;
        pill.HorizontalAlignment = HorizontalAlignment.Left;
        pill.VerticalAlignment = VerticalAlignment.Top;
        hasPlacement = false;
        tabs.SelectionChanged += OnSelectionChanged;
        container.SizeChanged += OnContainerSizeChanged;
        Schedule(false, false);
    }

    private void Stop() {
        if (tabs is not null) {
            tabs.SelectionChanged -= OnSelectionChanged;
        }
        if (container is not null) {
            container.SizeChanged -= OnContainerSizeChanged;
        }
        tabs = null;
        container = null;
    }

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) {
        if (ReferenceEquals(e.OriginalSource, tabs)) {
            Schedule(true, true);
        }
    }

    private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e) {
        Schedule(false, false);
    }

    private void Schedule(bool animate, bool animateContent) {
        pill.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Update(animate, animateContent)));
    }

    private void Update(bool animate, bool animateContent) {
        if ((tabs is null) || (container is null) || (translate is null)) {
            return;
        }
        TabItem? item = FindSelectedItem(tabs);
        if ((item is null) || !item.IsVisible || !item.IsDescendantOf(container)) {
            pill.Opacity = 0;
            hasPlacement = false;
            return;
        }
        pill.Opacity = 1;
        Point origin = item.TransformToAncestor(container).Transform(new Point(0, 0));
        double width = item.ActualWidth;
        double height = item.ActualHeight;
        bool motion = animate && hasPlacement && Jelly.IsMotionEnabled(pill);
        if (motion) {
            IEasingFunction slide = MuzzleEasings.Get(pill, MuzzleEasings.BackKey);
            IEasingFunction stretch = MuzzleEasings.Get(pill, MuzzleEasings.JellySoftKey);
            Duration slideDuration = TimeSpan.FromMilliseconds(460);
            Duration stretchDuration = TimeSpan.FromMilliseconds(620);
            translate.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(origin.X, slideDuration) { EasingFunction = slide });
            translate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(origin.Y, slideDuration) { EasingFunction = slide });
            pill.BeginAnimation(FrameworkElement.WidthProperty, new DoubleAnimation(width, stretchDuration) { EasingFunction = stretch });
            pill.BeginAnimation(FrameworkElement.HeightProperty, new DoubleAnimation(height, stretchDuration) { EasingFunction = stretch });
        }
        else {
            translate.BeginAnimation(TranslateTransform.XProperty, null);
            translate.BeginAnimation(TranslateTransform.YProperty, null);
            pill.BeginAnimation(FrameworkElement.WidthProperty, null);
            pill.BeginAnimation(FrameworkElement.HeightProperty, null);
            translate.X = origin.X;
            translate.Y = origin.Y;
            pill.Width = width;
            pill.Height = height;
        }
        hasPlacement = width > 0;
        if (animateContent) {
            AnimateContent();
        }
    }

    private void AnimateContent() {
        if ((tabs?.Template?.FindName("PART_SelectedContentHost", tabs) is not FrameworkElement host) || !Jelly.IsMotionEnabled(host)) {
            return;
        }
        if (host.RenderTransform is not TranslateTransform { IsFrozen: false } shift) {
            shift = new TranslateTransform();
            host.RenderTransform = shift;
        }
        host.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));
        shift.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(10, 0, TimeSpan.FromMilliseconds(480)) {
            EasingFunction = MuzzleEasings.Get(host, MuzzleEasings.BackKey)
        });
    }

    private static TabItem? FindSelectedItem(TabControl tabControl) {
        object? selected = tabControl.SelectedItem;
        if (selected is null) {
            return null;
        }
        return selected as TabItem ?? tabControl.ItemContainerGenerator.ContainerFromItem(selected) as TabItem;
    }
}