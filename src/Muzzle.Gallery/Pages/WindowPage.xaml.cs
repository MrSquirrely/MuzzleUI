using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class WindowPage : UserControl {
    public WindowPage() {
        InitializeComponent();
    }

    private void OnOpenDemoClick(object sender, RoutedEventArgs e) {
        DemoWindow window = new() { Owner = Window.GetWindow(this) };
        window.Show();
    }

    private void OnOpenDialogClick(object sender, RoutedEventArgs e) {
        StackPanel body = new() { Margin = new Thickness(24, 8, 24, 24) };
        body.Children.Add(new TextBlock {
            Text = "Fixed-size dialogs hide the minimize and maximize buttons automatically (ResizeMode=\"NoResize\").",
            TextWrapping = TextWrapping.Wrap,
            Style = (Style)FindResource("Muzzle.Text.Secondary")
        });
        MuzzleWindow dialog = new() {
            Title = "A tiny dialog",
            Width = 380,
            Height = 200,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = Window.GetWindow(this),
            ShowInTaskbar = false,
            Content = body
        };
        Button close = new() {
            Content = "Got it",
            Margin = new Thickness(0, 18, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right,
            IsDefault = true,
            Style = (Style)FindResource("Muzzle.Button.Primary")
        };
        close.Click += (_, _) => dialog.Close();
        body.Children.Add(close);
        dialog.ShowDialog();
    }
}