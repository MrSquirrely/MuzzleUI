using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Gallery.Pages;

public partial class RangePage : UserControl {
    public RangePage() {
        InitializeComponent();
    }

    private void OnSetProgress(object sender, RoutedEventArgs e) {
        if ((sender is FrameworkElement { Tag: string tag }) && double.TryParse(tag, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) {
            DemoProgress.Value = value;
        }
    }

    private void OnRandomProgress(object sender, RoutedEventArgs e) {
        DemoProgress.Value = Random.Shared.Next(5, 100);
    }
}