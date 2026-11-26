using System;
using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class DesktopControlsPage : UserControl {
    public DesktopControlsPage() {
        InitializeComponent();
        StatePath.Text = WindowStateSaver.FilePath;
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if ((Window.GetWindow(this) is Window window) && (BackdropHelper.GetBackdropType(window) is BackdropType current)) {
            foreach (RadioButton option in FindRadios(BackdropNone.Parent as Panel)) {
                option.IsChecked = (option.Tag as string) == current.ToString();
            }
        }
        BackdropStatus.Text = BackdropHelper.IsSupported
            ? "Windows 11 22H2+ detected: backdrops are available."
            : "Needs Windows 11 22H2 or later; on this system the setting is ignored gracefully.";
        TraySwitch.IsChecked = Tray.IsIconVisible;
    }

    private static System.Collections.Generic.IEnumerable<RadioButton> FindRadios(Panel? panel) {
        if (panel is null) {
            yield break;
        }
        foreach (UIElement child in panel.Children) {
            if (child is RadioButton radio) {
                yield return radio;
            }
        }
    }

    private void Log(string text) {
        ActionLog.Text = "Last action: " + text;
    }

    private void OnOpenPalette(object sender, RoutedEventArgs e) {
        PaletteShortcut.Press();
        if (Window.GetWindow(this) is MainWindow main) {
            main.Palette.Open();
        }
    }

    private void OnSaveClick(object sender, RoutedEventArgs e) {
        Log($"{((SplitButton)sender).Content} (primary segment)");
    }

    private void OnMenuClick(object sender, RoutedEventArgs e) {
        if (sender is MenuItem item) {
            Log($"{item.Header}{(item.IsCheckable ? (item.IsChecked ? " on" : " off") : string.Empty)}");
        }
    }

    private void OnSortChecked(object sender, RoutedEventArgs e) {
        if (IsLoaded && (sender is RadioButton radio)) {
            Log($"sort by {radio.Content}");
            SortDropDown.Close();
        }
    }

    private void OnFindTreats(object sender, RoutedEventArgs e) {
        Growl.Info("Try the treat drawer in the kitchen.", "Treat hunt");
    }

    private void OnPressChord(object sender, RoutedEventArgs e) {
        ChordBadge.Press();
    }

    private void OnTryPassword(object sender, RoutedEventArgs e) {
        if (sender is Button { Tag: string password }) {
            SecretBox.Password = password;
        }
    }

    private void OnBackdropChecked(object sender, RoutedEventArgs e) {
        if (!IsLoaded || (sender is not RadioButton { Tag: string tag }) || (Window.GetWindow(this) is not Window window)) {
            return;
        }
        BackdropType type = Enum.Parse<BackdropType>(tag);
        BackdropHelper.SetBackdropType(window, type);
        bool active = BackdropHelper.GetIsBackdropActive(window);
        BackdropStatus.Text = (type == BackdropType.None)
            ? "Backdrop off."
            : active ? $"{tag} is on: the window background is now translucent." : "Not supported here, so nothing changed.";
    }

    private void OnForgetPlacement(object sender, RoutedEventArgs e) {
        WindowStateSaver.Forget("MainWindow");
        Growl.Success("The next start will use the default size and position.", "Placement forgotten");
    }

    private void OnTrayChanged(object sender, RoutedEventArgs e) {
        Tray.IsIconVisible = TraySwitch.IsChecked == true;
    }

    private void OnTrayLeftClick(object? sender, EventArgs e) {
        BringGalleryForward();
    }

    private void OnTrayShow(object sender, RoutedEventArgs e) {
        BringGalleryForward();
    }

    private void OnTrayTheme(object sender, RoutedEventArgs e) {
        MuzzleThemeManager.ApplyTheme(MuzzleThemeManager.IsDark ? MuzzleTheme.Light : MuzzleTheme.Dark, true);
    }

    private void OnTrayHello(object sender, RoutedEventArgs e) {
        Tray.ShowNotification("Muzzle Gallery", "Boop! Hello from the tray.");
    }

    private void OnTrayHide(object sender, RoutedEventArgs e) {
        TraySwitch.IsChecked = false;
    }

    private void BringGalleryForward() {
        if (Window.GetWindow(this) is Window window) {
            if (window.WindowState == WindowState.Minimized) {
                window.WindowState = WindowState.Normal;
            }
            window.Activate();
        }
    }
}