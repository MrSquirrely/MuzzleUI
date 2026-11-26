using System;
using System.Windows;
using System.Windows.Controls;

using Muzzle.Gallery.Dialogs;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class OverlaysPage : UserControl {
    public OverlaysPage() {
        InitializeComponent();
    }

    private void OnGrowlInfo(object sender, RoutedEventArgs e) {
        Growl.Info("Biscuit is napping in the sunbeam. Please lower your voice.", "Nap in progress");
    }

    private void OnGrowlSuccess(object sender, RoutedEventArgs e) {
        Growl.Success("Dinner is served. Every bowl is licked clean.", "Yum");
    }

    private void OnGrowlWarning(object sender, RoutedEventArgs e) {
        Growl.Warning("The treat jar is getting low. Stock up soon.", "Heads up");
    }

    private void OnGrowlError(object sender, RoutedEventArgs e) {
        Growl.Error("Someone knocked a glass off the table. Nobody is admitting anything.", "Oops");
    }

    private void OnGrowlAsk(object sender, RoutedEventArgs e) {
        Growl.Ask("Open a new bag of treats?", static confirmed => {
            if (confirmed) {
                Growl.Success("Crinkle crinkle! Everyone came running.", "Treats opened");
            }
            else {
                Growl.Info("Maybe later. The cats are disappointed.", "Treats stay sealed");
            }
        }, "Treat time");
    }

    private void OnGrowlFatal(object sender, RoutedEventArgs e) {
        Growl.Fatal("The laser pointer battery died mid-chase. This toast stays until you close it.", "Catastrophe");
    }

    private void OnGrowlGlobal(object sender, RoutedEventArgs e) {
        Growl.Show(new GrowlInfo {
            Message = "This toast lives in the desktop-corner GrowlWindow, outside the app.",
            Title = "Desktop corner",
            Type = GrowlType.Success,
            IsGlobal = true
        });
    }

    private void OnGrowlClear(object sender, RoutedEventArgs e) {
        Growl.ClearAll();
    }

    private async void OnFormDialog(object sender, RoutedEventArgs e) {
        AdoptionRequest? request = await Dialog.ShowAsync<AdoptionRequest>(new AdoptionFormDialog());
        if (request is null) {
            Growl.Info("No kittens were adopted. They'll wait for you.", "Form cancelled");
            return;
        }
        string home = request.IndoorOnly ? "an indoor-only" : "an indoor/outdoor";
        Growl.Success($"{request.Name}, {request.Kitten} is heading to {home} home!", "Adoption approved");
    }

    private async void OnConfirmDialog(object sender, RoutedEventArgs e) {
        bool confirmed = await Dialog.ShowAsync<bool>(new ConfirmDialog("Trim everyone's claws?", "This will cause betrayal, dramatic sighs and at least one hiding cat."));
        if (confirmed) {
            Growl.Warning("Claws trimmed. You are no longer trusted.", "Done");
        }
        else {
            Growl.Info("Claws left as they are. Peace prevails.", "Cancelled");
        }
    }

    private void OnMessageBox(object sender, RoutedEventArgs e) {
        MessageBoxResult result = Theme.Controls.MessageBox.Show(
            Window.GetWindow(this), "Biscuit knocked a glass off the table. Save your changes before cleaning up?", "Unsaved changes",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
        Growl.Info($"MessageBox returned {result}.");
    }

    private void OnLightDialog(object sender, RoutedEventArgs e) {
        StackPanel body = new() { Width = 320 };
        body.Children.Add(new TextBlock { Text = "Just a little note", Style = (Style)FindResource("Muzzle.Text.Title") });
        body.Children.Add(new TextBlock {
            Text = "CloseOnBackdropClick is on, so clicking anywhere outside this card (or pressing Esc) closes it.",
            Margin = new Thickness(0, 8, 0, 0),
            Style = (Style)FindResource("Muzzle.Text.Secondary")
        });
        Dialog dialog = Dialog.Show(body);
        dialog.CloseOnBackdropClick = true;
    }

    private void OnOpenDrawer(object sender, RoutedEventArgs e) {
        if ((sender is not FrameworkElement { Tag: string tag }) || !Enum.TryParse(tag, out Dock dock)) {
            return;
        }
        Drawer drawer = Drawer.Show(BuildDrawerContent(dock), dock, $"{dock} drawer");
        if ((dock == Dock.Top) || (dock == Dock.Bottom)) {
            drawer.DrawerSize = 240;
        }
        drawer.Closed += static (_, _) => Growl.Info("Drawer closed and removed itself.", "Drawer");
    }

    private StackPanel BuildDrawerContent(Dock dock) {
        StackPanel content = new();
        content.Children.Add(new TextBlock {
            Text = $"Slid in from the {dock.ToString().ToLowerInvariant()} edge with a springy overshoot.",
            Style = (Style)FindResource("Muzzle.Text.Secondary")
        });
        content.Children.Add(new ToggleSwitch { Content = "Night patrol", IsChecked = true, Margin = new Thickness(0, 16, 0, 0) });
        content.Children.Add(new ToggleSwitch { Content = "Zoomies mode", Margin = new Thickness(0, 10, 0, 0) });
        content.Children.Add(new Slider { Minimum = 0, Maximum = 100, Value = 60, Margin = new Thickness(0, 14, 0, 0) });
        Button done = new() {
            Content = "Done",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(0, 16, 0, 0),
            Style = (Style)FindResource("Muzzle.Button.Primary")
        };
        done.Click += OnDrawerDoneClick;
        content.Children.Add(done);
        return content;
    }

    private void OnDrawerDoneClick(object sender, RoutedEventArgs e) {
        if ((sender is DependencyObject element) && (Drawer.GetDrawer(element) is Drawer drawer)) {
            drawer.IsOpen = false;
        }
    }
}