using System.Windows;
using System.Windows.Controls;

using Muzzle.Gallery.Models;

namespace Muzzle.Gallery.Pages;

public partial class PropertyGridPage : UserControl {
    private readonly PlaygroundModel model = new();

    public PropertyGridPage() {
        InitializeComponent();
        DataContext = model;
        Inspector.SelectedObject = model;
    }

    private void OnTargetChanged(object sender, RoutedEventArgs e) {
        if (Inspector is null) {
            return;
        }
        Inspector.SearchText = string.Empty;
        Inspector.SelectedObject = InspectButton.IsChecked == true ? PreviewButton : model;
    }
}