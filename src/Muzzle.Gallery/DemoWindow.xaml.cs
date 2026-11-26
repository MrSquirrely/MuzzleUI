using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery;

public partial class DemoWindow : MuzzleWindow {
    public DemoWindow() {
        InitializeComponent();
    }

    private void OnTabChecked(object sender, RoutedEventArgs e) {
        if ((Headline is null) || (sender is not RadioButton { Content: string title })) {
            return;
        }
        Headline.Text = title;
        Jelly.Wobble(Body);
    }

    private void OnWobbleClick(object sender, RoutedEventArgs e) {
        Jelly.Wobble(Body);
    }
}