using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

/// <summary>Menu, ContextMenu, MenuItem and Separator pick up the Muzzle styles without any markup in the app.</summary>
[Collection(WpfCollection.Name)]
public sealed class MenuTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public Task ContextMenu_BuiltInCode_IsStyledAndOpensItsSubmenu() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            MenuItem copy = new() { Header = "Copy", InputGestureText = "Ctrl+C" };
            MenuItem pin = new() { Header = "Pin", IsCheckable = true, IsChecked = true };
            MenuItem nested = new() { Header = "Nested" };
            MenuItem more = new() { Header = "More" };
            more.Items.Add(nested);
            Separator separator = new();
            ContextMenu menu = new();
            menu.Items.Add(copy);
            menu.Items.Add(separator);
            menu.Items.Add(pin);
            menu.Items.Add(more);
            menu.Items.Add(new MenuItem { Header = "Unavailable", IsEnabled = false });
            Button target = new() { Content = "Target", ContextMenu = menu };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(target);
            try {
                menu.PlacementTarget = target;
                menu.IsOpen = true;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => copy.IsLoaded), "The context menu never opened.");

                Assert.Same(Application.Current.FindResource(typeof(ContextMenu)), menu.Style);
                Assert.NotNull(menu.Template.FindName("Root", menu));
                Assert.Same(Application.Current.FindResource(typeof(MenuItem)), copy.Style);
                Assert.Same(Application.Current.FindResource(MenuItem.SeparatorStyleKey), separator.Style);

                Assert.Equal(MenuItemRole.SubmenuItem, copy.Role);
                Assert.Same(Application.Current.FindResource("Muzzle.MenuItem.SubmenuItemTemplate"), copy.Template);
                Assert.Equal(Visibility.Visible, Assert.IsAssignableFrom<UIElement>(pin.Template.FindName("CheckMark", pin)).Visibility);
                Assert.Equal(Visibility.Collapsed, Assert.IsAssignableFrom<UIElement>(copy.Template.FindName("CheckMark", copy)).Visibility);

                Assert.Equal(MenuItemRole.SubmenuHeader, more.Role);
                Assert.Same(Application.Current.FindResource("Muzzle.MenuItem.SubmenuHeaderTemplate"), more.Template);
                Assert.NotNull(more.Template.FindName("Chevron", more));
                Popup popup = Assert.IsType<Popup>(more.Template.FindName("PART_Popup", more));
                Assert.False(popup.IsOpen);
                more.IsSubmenuOpen = true;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => nested.IsLoaded), "The nested submenu never opened.");
                Assert.True(popup.IsOpen);
                Assert.Same(Application.Current.FindResource(typeof(MenuItem)), nested.Style);
            }
            finally {
                more.IsSubmenuOpen = false;
                menu.IsOpen = false;
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Menu_TopLevelRoles_UseTheirTemplatesAndDropTheirSubmenu() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            MenuItem open = new() { Header = "_Open", InputGestureText = "Ctrl+O" };
            MenuItem recent = new() { Header = "Recent" };
            recent.Items.Add(new MenuItem { Header = "mochi.txt" });
            MenuItem file = new() { Header = "_File" };
            file.Items.Add(open);
            file.Items.Add(new Separator());
            file.Items.Add(recent);
            MenuItem help = new() { Header = "_Help" };
            Menu menu = new();
            menu.Items.Add(file);
            menu.Items.Add(help);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(new DockPanel { LastChildFill = false, Children = { menu } });
            try {
                await ControlTemplateTests.AssertTemplatedAsync(menu);
                Assert.Same(Application.Current.FindResource(typeof(Menu)), menu.Style);
                Assert.Equal(MenuItemRole.TopLevelHeader, file.Role);
                Assert.Same(Application.Current.FindResource("Muzzle.MenuItem.TopLevelHeaderTemplate"), file.Template);
                Assert.Equal(MenuItemRole.TopLevelItem, help.Role);
                Assert.Same(Application.Current.FindResource("Muzzle.MenuItem.TopLevelItemTemplate"), help.Template);
                // Menu-bar entries hug their header instead of taking the 180 px submenu row width.
                Assert.True(help.ActualWidth < 120, $"Top-level item is {help.ActualWidth} px wide.");

                Popup popup = Assert.IsType<Popup>(file.Template.FindName("PART_Popup", file));
                file.IsSubmenuOpen = true;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => open.IsLoaded), "The File menu never opened.");
                Assert.True(popup.IsOpen);
                Assert.Equal(MenuItemRole.SubmenuItem, open.Role);
                Assert.Equal(MenuItemRole.SubmenuHeader, recent.Role);
            }
            finally {
                file.IsSubmenuOpen = false;
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task ItemsControls_HonourAnAppImplicitContainerStyle() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            const string Marker = "app-style";
            ListBox list = new() { ItemsSource = new[] { "A", "B" } };
            ComboBox combo = new() { ItemsSource = new[] { "A", "B" }, SelectedIndex = 0 };
            TreeView tree = new();
            TreeViewItem node = new() { Header = "Node" };
            tree.Items.Add(node);
            StackPanel panel = new() { Children = { list, combo, tree } };
            panel.Resources[typeof(ListBoxItem)] = AppStyle<ListBoxItem>(Marker);
            panel.Resources[typeof(ComboBoxItem)] = AppStyle<ComboBoxItem>(Marker);
            panel.Resources[typeof(TreeViewItem)] = AppStyle<TreeViewItem>(Marker);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(panel);
            try {
                await MuzzleWpfTestFixture.WaitAsync(50);
                ListBoxItem listItem = Assert.IsType<ListBoxItem>(list.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.Equal(Marker, listItem.Tag);
                // Still the Muzzle look: the app style is based on the theme's.
                Assert.NotNull(listItem.Template.FindName("Body", listItem));
                Assert.Equal(Marker, node.Tag);

                combo.IsDropDownOpen = true;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => combo.ItemContainerGenerator.ContainerFromIndex(0) is not null));
                ComboBoxItem comboItem = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.Equal(Marker, comboItem.Tag);
            }
            finally {
                combo.IsDropDownOpen = false;
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    private static Style AppStyle<T>(string marker) where T : FrameworkElement {
        Style style = new(typeof(T), (Style)Application.Current.FindResource(typeof(T)));
        style.Setters.Add(new Setter(FrameworkElement.TagProperty, marker));
        return style;
    }
}