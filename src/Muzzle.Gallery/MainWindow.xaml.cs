using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

using Muzzle.Gallery.Models;
using Muzzle.Gallery.Pages;
using Muzzle.Theme;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery;

public partial class MainWindow : MuzzleWindow {
    private readonly List<NavigationItem> items;
    private readonly ICollectionView view;
    private double intensityBeforeCalm = 1;

    public MainWindow() {
        InitializeComponent();
        items = [
            new NavigationItem("Overview", "palette colors typography tokens setup", MuzzleIconKind.Home, static () => new OverviewPage()),
            new NavigationItem("Buttons", "primary secondary ghost pill icon", MuzzleIconKind.Toggle, static () => new ButtonsPage()),
            new NavigationItem("Text input", "textbox passwordbox combobox dropdown", MuzzleIconKind.Edit, static () => new InputsPage()),
            new NavigationItem("Selection", "checkbox radiobutton toggleswitch", MuzzleIconKind.CheckSquare, static () => new SelectionPage()),
            new NavigationItem("Range", "slider progressbar", MuzzleIconKind.Sliders, static () => new RangePage()),
            new NavigationItem("Containers", "card border expander tabcontrol scrollviewer tooltip", MuzzleIconKind.Layers, static () => new ContainersPage()),
            new NavigationItem("Property grid", "propertygrid inspector reflection editor", MuzzleIconKind.List, static () => new PropertyGridPage()),
            new NavigationItem("Jelly lab", "animation easing elastic back storyboard physics wobble", MuzzleIconKind.Droplet, static () => new JellyLabPage()),
            new NavigationItem("Overlays", "growl toast notification dialog modal drawer panel overlay", MuzzleIconKind.Bell, static () => new OverlaysPage()),
            new NavigationItem("Navigation", "navigationview sidemenu breadcrumb pagination stepbar stepper transition layout", MuzzleIconKind.Compass, static () => new NavigationPage()),
            new NavigationItem("Inputs & pickers", "numericupdown tag token search color colour date time calendar rating star", MuzzleIconKind.Palette, static () => new PickersPage()),
            new NavigationItem("Feedback & status", "badge skeleton shimmer loading spinner infobar alert banner popover tooltip", MuzzleIconKind.Info, static () => new FeedbackPage()),
            new NavigationItem("Data & media", "datagrid grid table avatar profile initials carousel slideshow imageviewer zoom pan image treeview tree hierarchy", MuzzleIconKind.Database, static () => new DataMediaPage()),
            new NavigationItem("Utilities & behaviors", "jelly behavior entrance clip rounded scroll smooth focus trap escape converter icon icons theme dictionary markup", MuzzleIconKind.Wrench, static () => new UtilitiesPage()),
            new NavigationItem("Desktop controls", "command palette spotlight split dropdown button gridsplitter splitter empty state keycap shortcut range slider password strength code copy tray backdrop mica acrylic window state", MuzzleIconKind.Command, static () => new DesktopControlsPage()),
            new NavigationItem("Window", "muzzlewindow chrome title bar nonclientarea snap", MuzzleIconKind.Monitor, static () => new WindowPage())
        ];
        view = CollectionViewSource.GetDefaultView(items);
        view.Filter = FilterItem;
        NavList.ItemsSource = view;
        NavList.SelectedIndex = 0;
        InputBindings.Add(new KeyBinding(new RelayCommand(_ => {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }), Key.F, ModifierKeys.Control));
        Palette.ItemsSource = CreatePaletteItems();
    }

    public int PageCount => items.Count;

    /// <summary>Selects a gallery page by index (used by snapshot mode too).</summary>
    public void NavigateTo(int index) {
        SearchBox.Text = string.Empty;
        NavList.SelectedItem = items[Math.Clamp(index, 0, items.Count - 1)];
    }

    /// <summary>Index of a page by title (−1 when missing).</summary>
    public int IndexOf(string title) {
        return items.FindIndex(item => item.Title == title);
    }

    private ObservableCollection<CommandPaletteItem> CreatePaletteItems() {
        ObservableCollection<CommandPaletteItem> entries = [];
        for (int i = 0; i < items.Count; i++) {
            int index = i;
            entries.Add(new CommandPaletteItem {
                Title = items[i].Title,
                Description = "Open the page",
                Category = "Page",
                Icon = items[i].Icon,
                Keywords = items[i].Keywords,
                Command = new RelayCommand(_ => NavigateTo(index))
            });
        }
        entries.Add(new CommandPaletteItem { Title = "Light theme", Category = "Theme", Icon = MuzzleIconKind.Sun, Keywords = "bright day", Command = new RelayCommand(_ => MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light, true)) });
        entries.Add(new CommandPaletteItem { Title = "Dark theme", Category = "Theme", Icon = MuzzleIconKind.Moon, Keywords = "night twilight", Command = new RelayCommand(_ => MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark, true)) });
        entries.Add(new CommandPaletteItem { Title = "Follow the system theme", Category = "Theme", Icon = MuzzleIconKind.Monitor, Keywords = "windows auto", Command = new RelayCommand(_ => MuzzleThemeManager.ApplyTheme(MuzzleTheme.System, true)) });
        foreach (MuzzleAccent accent in (MuzzleAccent[])[MuzzleAccent.Coral, MuzzleAccent.Lavender, MuzzleAccent.Mint, MuzzleAccent.Peach]) {
            entries.Add(new CommandPaletteItem {
                Title = $"{accent} accent",
                Category = "Accent",
                Icon = MuzzleIconKind.Palette,
                Keywords = "colour color",
                Command = new RelayCommand(_ => MuzzleThemeManager.ApplyAccent(accent, true))
            });
        }
        entries.Add(new CommandPaletteItem { Title = "Boop the page", Description = "Give everything a wobble", Category = "Action", Icon = MuzzleIconKind.Paw, Command = new RelayCommand(_ => Jelly.Wobble(PageHost)) });
        entries.Add(new CommandPaletteItem { Title = "Toggle calm mode", Description = "Turn jelly physics on or off", Category = "Action", Icon = MuzzleIconKind.Leaf, Keywords = "motion reduce", Command = new RelayCommand(_ => CalmSwitch.IsChecked = CalmSwitch.IsChecked != true) });
        entries.Add(new CommandPaletteItem { Title = "Say hello", Description = "Pop a toast in the corner", Category = "Action", Icon = MuzzleIconKind.Bell, Command = new RelayCommand(_ => Growl.Success("Hello from the command palette!", "Boop")) });
        entries.Add(new CommandPaletteItem { Title = "Search pages", Category = "Action", Icon = MuzzleIconKind.Search, Shortcut = "Ctrl+F", Command = new RelayCommand(_ => {
            SearchBox.Focus();
            SearchBox.SelectAll();
        }) });
        return entries;
    }

    private bool FilterItem(object item) {
        return item is NavigationItem navigation && navigation.Matches(SearchBox.Text.Trim());
    }

    private void OnSearchChanged(object sender, TextChangedEventArgs e) {
        view.Refresh();
        if ((NavList.SelectedItem is null) && !view.IsEmpty) {
            view.MoveCurrentToFirst();
            NavList.SelectedItem = view.CurrentItem;
        }
    }

    private void OnNavigationChanged(object sender, SelectionChangedEventArgs e) {
        if (NavList.SelectedItem is NavigationItem item) {
            PageHost.Content = item.CreatePage();
        }
    }

    private void OnCalmChanged(object sender, RoutedEventArgs e) {
        if (CalmSwitch.IsChecked == true) {
            intensityBeforeCalm = MuzzleAssist.GetJellyIntensity(this);
            MuzzleAssist.SetJellyIntensity(this, 0);
        }
        else {
            MuzzleAssist.SetJellyIntensity(this, intensityBeforeCalm > 0 ? intensityBeforeCalm : 1);
        }
    }

    private void OnBoopClick(object sender, RoutedEventArgs e) {
        Jelly.Wobble(PageHost);
    }

    private void OnPaletteButtonClick(object sender, RoutedEventArgs e) {
        Palette.Open();
    }

    private void OnPaletteItemInvoked(object? sender, CommandPaletteItemInvokedEventArgs e) {
        // Items carry their own commands; this is where an app could log or track usage.
    }
}

/// <summary>A minimal ICommand around a delegate.</summary>
public sealed class RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null) : ICommand {
    public event EventHandler? CanExecuteChanged {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) {
        return canExecute?.Invoke(parameter) ?? true;
    }

    public void Execute(object? parameter) {
        execute(parameter);
    }
}