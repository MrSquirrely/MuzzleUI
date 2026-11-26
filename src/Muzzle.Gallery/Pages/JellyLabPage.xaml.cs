using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

using Muzzle.Theme;

namespace Muzzle.Gallery.Pages;

public partial class JellyLabPage : UserControl {
    public JellyLabPage() {
        InitializeComponent();
        BuildLanes();
        ThemeAware.Attach(this, BuildLanes);
    }

    private void BuildLanes() {
        EasingLanes.ItemsSource = new List<EasingLane> {
            new("Jelly", MuzzleEasings.JellyKey, Brush("Muzzle.Brush.Primary")),
            new("JellySoft", MuzzleEasings.JellySoftKey, Brush("Muzzle.Brush.PrimaryHover")),
            new("Wobble", MuzzleEasings.WobbleKey, Brush("Muzzle.Brush.Secondary")),
            new("Back", MuzzleEasings.BackKey, Brush("Muzzle.Brush.Mint")),
            new("BackStrong", MuzzleEasings.BackStrongKey, Brush("Muzzle.Brush.Sky")),
            new("Glide", MuzzleEasings.GlideKey, Brush("Muzzle.Brush.Butter")),
            new("Smooth", MuzzleEasings.SmoothKey, Brush("Muzzle.Brush.Peach"))
        };
    }

    private static Brush Brush(string key) {
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Pink;
    }

    private void OnRaceClick(object sender, RoutedEventArgs e) {
        foreach (Border bean in FindBeans(EasingLanes)) {
            if ((bean.Tag is not string key) || (bean.Parent is not Border lane)) {
                continue;
            }
            if (bean.RenderTransform is not TranslateTransform { IsFrozen: false } shift) {
                shift = new TranslateTransform();
                bean.RenderTransform = shift;
            }
            double distance = Math.Max(0, lane.ActualWidth - bean.ActualWidth - 8);
            double target = shift.X > (distance / 2) ? 0 : distance;
            shift.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(target, TimeSpan.FromMilliseconds(1100)) {
                EasingFunction = MuzzleEasings.Get(this, key)
            });
        }
    }

    private static IEnumerable<Border> FindBeans(DependencyObject root) {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is Border { Name: "Bean" } bean) {
                yield return bean;
            }
            foreach (Border nested in FindBeans(child)) {
                yield return nested;
            }
        }
    }

    public sealed record EasingLane(string Name, string Key, Brush Brush);
}