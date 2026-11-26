using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class FeedbackPage : UserControl {
    private int bellCount = 7;

    public FeedbackPage() {
        InitializeComponent();
    }

    private void OnAddMessage(object sender, RoutedEventArgs e) {
        InboxBadge.Value++;
    }

    private void OnAddMany(object sender, RoutedEventArgs e) {
        InboxBadge.Value += 50;
    }

    private void OnClearMessages(object sender, RoutedEventArgs e) {
        InboxBadge.Value = 0;
    }

    private void OnRingBell(object sender, RoutedEventArgs e) {
        bellCount++;
        MuzzleAssist.SetBadge(BellButton, bellCount);
    }

    private async void OnReload(object sender, RoutedEventArgs e) {
        LoadingSwitch.IsChecked = true;
        await Task.Delay(TimeSpan.FromSeconds(2));
        LoadingSwitch.IsChecked = false;
    }

    private async void OnSave(object sender, RoutedEventArgs e) {
        SaveButton.IsEnabled = false;
        SaveSpinner.IsActive = true;
        SaveLabel.Text = "Saving…";
        await Task.Delay(TimeSpan.FromSeconds(1.6));
        SaveSpinner.IsActive = false;
        SaveLabel.Text = "Save profile";
        SaveButton.IsEnabled = true;
        Growl.Success("Profile saved.", "Done");
    }

    private void OnResetBanners(object sender, RoutedEventArgs e) {
        foreach (UIElement child in Banners.Children) {
            if (child is InfoBar bar) {
                bar.IsOpen = true;
            }
        }
    }
}