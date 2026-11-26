using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

/// <summary>
/// Every Muzzle control (and every restyled standard control) is created in an off-screen window, measured, arranged
/// and templated. The test fails on a missing template, a declared [TemplatePart] the template lacks (or has with
/// the wrong type), a binding error, or an exception escaping to the dispatcher.
/// </summary>
[Collection(WpfCollection.Name)]
public sealed class ControlTemplateTests(MuzzleWpfTestFixture fixture) {
    private static readonly Dictionary<string, Func<FrameworkElement>> Factories = new() {
        ["PropertyGrid"] = static () => new PropertyGrid { Height = 520, SelectedObject = new SampleModel() },
        ["NavigationView"] = static () => {
            NavigationView view = new() { Height = 400 };
            view.Items.Add(new NavigationViewItem { Content = "Home", IconGeometry = Geometry.Parse("M 2,8 L 8,2 L 14,8") });
            view.Items.Add(new NavigationViewItem { Content = "Cats", Icon = new MuzzleIcon { Kind = MuzzleIconKind.Cat } });
            view.SelectedIndex = 0;
            return view;
        },
        ["BreadcrumbBar"] = static () => new BreadcrumbBar { ItemsSource = new[] { "Home", "Cats", "Mochi" } },
        ["Pagination"] = static () => new Pagination { PageCount = 24, CurrentPage = 3 },
        ["TransitioningContentControl"] = static () => new TransitioningContentControl { Content = "Hello" },
        ["StepBar"] = static () => {
            StepBar bar = new() { StepIndex = 1 };
            bar.Items.Add(new StepBarItem { Content = "Pick" });
            bar.Items.Add(new StepBarItem { Content = "Meet" });
            bar.Items.Add(new StepBarItem { Content = "Adopt" });
            return bar;
        },
        ["NumericUpDown"] = static () => new NumericUpDown { Minimum = 0, Maximum = 10, Value = 3 },
        ["TagInput"] = static () => new TagInput { Tags = new ObservableCollection<string> { "fluffy", "orange" } },
        ["SearchBar"] = static () => new SearchBar { SuggestionsSource = new[] { "Mochi", "Biscuit" } },
        ["ColorPicker"] = static () => new ColorPicker { SelectedColor = Colors.Pink },
        ["ColorPickerDropDown"] = static () => new ColorPickerDropDown { SelectedColor = Colors.Lavender },
        ["DatePicker"] = static () => new DatePicker { SelectedDate = new DateTime(2026, 11, 26) },
        ["Calendar"] = static () => new Calendar(),
        ["TimePicker"] = static () => new TimePicker { SelectedTime = new TimeSpan(9, 30, 0) },
        ["Rating"] = static () => new Rating { Value = 3.5, AllowHalf = true },
        ["Badge"] = static () => new Badge { Value = 3, Content = new Button { Content = "Inbox" } },
        ["Skeleton"] = static () => new Skeleton { IsLoading = true, Content = new TextBlock { Text = "Loaded" } },
        ["LoadingSpinner.ArcRing"] = static () => new LoadingSpinner { IsActive = true },
        ["LoadingSpinner.BouncingDots"] = static () => new LoadingSpinner { IsActive = true, SpinnerStyle = SpinnerStyle.BouncingDots },
        ["InfoBar"] = static () => new InfoBar { Title = "Heads up", Message = "Treats are low.", Severity = InfoBarSeverity.Warning },
        ["Popover"] = static () => new Popover { Content = new Button { Content = "Open" }, PopoverContent = new TextBlock { Text = "Hi" } },
        ["Avatar"] = static () => new Avatar { DisplayName = "Ada Lovelace", Status = AvatarStatus.Online },
        ["Carousel"] = static () => new Carousel { Height = 200, ItemsSource = new[] { "One", "Two", "Three" } },
        ["ImageViewer"] = static () => new ImageViewer { Height = 200 },
        ["CommandPalette"] = static () => new CommandPalette { ItemsSource = SamplePaletteItems() },
        ["SplitButton"] = static () => {
            SplitButton button = new() { Content = "Save" };
            button.MenuItems.Add(new MenuItem { Header = "Save as…" });
            return button;
        },
        ["DropDownButton"] = static () => new DropDownButton { Content = "Sort", DropDownContent = new TextBlock { Text = "By name" } },
        ["RangeSlider"] = static () => new RangeSlider { Width = 300 },
        ["PasswordStrengthMeter"] = static () => new PasswordStrengthMeter { Password = "Sunny-Window-7" },
        ["CodeBlock"] = static () => new CodeBlock { Code = "<Button Content=\"Hi\" />", Language = "xaml", ShowLineNumbers = true },
        ["CopyableText"] = static () => new CopyableText { Text = "MZ-0042" },
        ["MuzzleIcon"] = static () => new MuzzleIcon { Kind = MuzzleIconKind.Paw, IconSize = 24 },
        ["EmptyState"] = static () => new EmptyState { Title = "Nothing yet", Description = "Add a cat.", ActionContent = new Button { Content = "Add" } },
        ["Keycap"] = static () => new Keycap { KeyText = "Ctrl" },
        ["ShortcutBadge"] = static () => new ShortcutBadge { Shortcut = "Ctrl+Shift+P" },
        ["Card"] = static () => new Card { Content = new TextBlock { Text = "Card" } },
        ["ToggleSwitch"] = static () => new ToggleSwitch { Content = "Purr", IsChecked = true },
        ["ThemeToggleButton"] = static () => new ThemeToggleButton(),
        ["AccentSwatch"] = static () => new AccentSwatch { Accent = MuzzleAccent.Lavender },
        ["DataGrid"] = static () => new DataGrid {
            Height = 200,
            AutoGenerateColumns = true,
            ItemsSource = new[] { new SampleRow("Mochi", 3, true), new SampleRow("Pepper", 2, false) }
        },
        ["TreeView"] = static () => {
            TreeView tree = new() { Height = 200 };
            TreeViewItem room = new() { Header = "Living room", IsExpanded = true };
            room.Items.Add(new TreeViewItem { Header = "Sofa", IsSelected = true });
            tree.Items.Add(room);
            return tree;
        },
        ["Button"] = static () => new Button { Content = "Boop" },
        ["TextBox"] = static () => new TextBox { Text = "Mochi" },
        ["PasswordBox"] = static () => new PasswordBox(),
        ["ComboBox"] = static () => new ComboBox { ItemsSource = new[] { "A", "B" }, SelectedIndex = 0 },
        ["CheckBox"] = static () => new CheckBox { Content = "Indoor", IsChecked = true },
        ["RadioButton"] = static () => new RadioButton { Content = "Nap", IsChecked = true },
        ["Slider"] = static () => new Slider { Width = 200, Value = 4 },
        ["ProgressBar"] = static () => new ProgressBar { Width = 200, Value = 40 },
        ["TabControl"] = static () => new TabControl { Items = { new TabItem { Header = "One", Content = "1" }, new TabItem { Header = "Two", Content = "2" } } },
        ["Expander"] = static () => new Expander { Header = "More", Content = "Hidden", IsExpanded = true },
        ["ListBox"] = static () => new ListBox { ItemsSource = new[] { "A", "B" }, SelectedIndex = 1 },
        ["ScrollViewer"] = static () => new ScrollViewer { Height = 80, Content = new Border { Height = 400 } },
        ["GridSplitter"] = static () => new GridSplitter { Width = 10, Height = 100 },
        ["ToolTip"] = static () => new Button { Content = "Hover", ToolTip = new ToolTip { Content = "Tip" } },
        ["Menu"] = static () => {
            Menu menu = new();
            MenuItem file = new() { Header = "_File" };
            file.Items.Add(new MenuItem { Header = "_Open", InputGestureText = "Ctrl+O" });
            file.Items.Add(new Separator());
            file.Items.Add(new MenuItem { Header = "Autosave", IsCheckable = true, IsChecked = true });
            menu.Items.Add(file);
            menu.Items.Add(new MenuItem { Header = "_Help" });
            return menu;
        },
        ["Separator"] = static () => new Separator { Width = 120 }
    };

    public static TheoryData<string> ControlNames() {
        TheoryData<string> data = [];
        foreach (string name in Factories.Keys) {
            data.Add(name);
        }
        return data;
    }

    [StaTheory]
    [MemberData(nameof(ControlNames))]
    public Task Control_TemplatesCleanly(string name) {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            FrameworkElement element = Factories[name]();
            Window window = MuzzleWpfTestFixture.ShowOffScreen(new Grid { Children = { element } });
            try {
                await AssertTemplatedAsync(element);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task AllControls_TemplateCleanlyInDarkWithEveryAccent() {
        return fixture.RunAsync(async () => {
            try {
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.Dark);
                MuzzleThemeManager.ApplyAccent(MuzzleAccent.Lavender);
                fixture.ResetDiagnostics();
                StackPanel panel = new();
                List<FrameworkElement> elements = [];
                foreach (Func<FrameworkElement> factory in Factories.Values) {
                    FrameworkElement element = factory();
                    elements.Add(element);
                    panel.Children.Add(element);
                }
                Window window = MuzzleWpfTestFixture.ShowOffScreen(new ScrollViewer { Content = panel }, 1000, 900);
                try {
                    foreach (MuzzleAccent accent in (MuzzleAccent[])[MuzzleAccent.Mint, MuzzleAccent.Peach, MuzzleAccent.Coral]) {
                        MuzzleThemeManager.ApplyAccent(accent);
                        window.UpdateLayout();
                    }
                    MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
                    window.UpdateLayout();
                    await MuzzleWpfTestFixture.WaitAsync(200);
                    foreach (FrameworkElement element in elements.OfType<Control>()) {
                        Assert.NotNull(((Control)element).Template);
                    }
                }
                finally {
                    window.Close();
                }
                fixture.AssertClean();
            }
            finally {
                MuzzleWpfTestFixture.ResetTheme();
            }
        });
    }

    [StaFact]
    public Task MuzzleWindow_HostsInteractiveNonClientContent() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            Button searchButton = new() { Content = "Search" };
            MuzzleWindow window = new() { Title = "Test", NonClientAreaContent = new StackPanel { Children = { searchButton } } };
            MuzzleWpfTestFixture.ShowOffScreen(new TextBlock { Text = "Body" }, 800, 500, window);
            try {
                await MuzzleWpfTestFixture.WaitAsync(50);
                Assert.True(searchButton.IsLoaded);
                Assert.True(searchButton.IsDescendantOf(window));
                Assert.True(System.Windows.Shell.WindowChrome.GetIsHitTestVisibleInChrome(searchButton),
                    "Title-bar content must be hit-testable inside the WindowChrome caption.");
                System.Windows.Shell.WindowChrome chrome = Assert.IsType<System.Windows.Shell.WindowChrome>(System.Windows.Shell.WindowChrome.GetWindowChrome(window));
                Assert.Equal(window.NonClientAreaHeight, chrome.CaptionHeight);
                foreach (string part in (string[])["PART_MinimizeButton", "PART_MaximizeButton", "PART_CloseButton"]) {
                    Assert.NotNull(window.Template.FindName(part, window));
                }
                await AssertTemplatedAsync(window);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    /// <summary>Template applied, visuals realised, every declared template part present with the declared type.</summary>
    internal static async Task AssertTemplatedAsync(FrameworkElement element) {
        element.ApplyTemplate();
        element.Measure(new Size(900, 700));
        element.Arrange(new Rect(element.DesiredSize));
        element.UpdateLayout();
        await MuzzleWpfTestFixture.WaitAsync(60);
        if (element is not Control control) {
            return;
        }
        Assert.NotNull(control.Template);
        Assert.True(VisualTreeHelper.GetChildrenCount(control) > 0, $"{control.GetType().Name} produced no visuals.");
        foreach ((string partName, Type? partType) in RequiredParts(control)) {
            object? found = control.Template.FindName(partName, control);
            Assert.True(found is not null, $"{control.GetType().Name} template lacks {partName}.");
            if (partType is not null) {
                Assert.True(partType.IsInstanceOfType(found), $"{control.GetType().Name}.{partName} is {found!.GetType().Name}, expected {partType.Name}.");
            }
        }
    }

    // Parts a Muzzle control declares but may legitimately lack in one of its template variants.
    private static readonly Dictionary<Type, string[]> OptionalMuzzleParts = new() {
        [typeof(LoadingSpinner)] = ["PART_Arc"] // only the ArcRing template has an arc
    };

    // Stock WPF controls declare optional parts (Slider.PART_SelectionRange, ProgressBar.PART_Track…); these are the
    // ones the control actually needs to work.
    private static readonly Dictionary<Type, (string Name, Type? Type)[]> RequiredStockParts = new() {
        [typeof(TextBox)] = [("PART_ContentHost", null)],
        [typeof(PasswordBox)] = [("PART_ContentHost", null)],
        [typeof(ComboBox)] = [("PART_Popup", typeof(System.Windows.Controls.Primitives.Popup))],
        [typeof(Slider)] = [("PART_Track", typeof(System.Windows.Controls.Primitives.Track))],
        [typeof(ScrollViewer)] = [("PART_ScrollContentPresenter", typeof(ScrollContentPresenter))],
        [typeof(DatePicker)] = [("PART_TextBox", null), ("PART_Button", null), ("PART_Popup", typeof(System.Windows.Controls.Primitives.Popup))],
        [typeof(Calendar)] = [("PART_CalendarItem", null)],
        [typeof(TabControl)] = [("PART_SelectedContentHost", null)],
        [typeof(DataGrid)] = [("DG_ScrollViewer", typeof(ScrollViewer))]
    };

    private static IEnumerable<(string Name, Type? Type)> RequiredParts(Control control) {
        Type type = control.GetType();
        if (type.Namespace?.StartsWith("Muzzle.", StringComparison.Ordinal) == true) {
            string[] optional = OptionalMuzzleParts.TryGetValue(type, out string[]? names) ? names : [];
            return type.GetCustomAttributes<TemplatePartAttribute>(inherit: true)
                .Where(part => !optional.Contains(part.Name))
                .Select(static part => (part.Name, (Type?)part.Type));
        }
        return RequiredStockParts.TryGetValue(type, out (string Name, Type? Type)[]? parts) ? parts : [];
    }

    internal static ObservableCollection<CommandPaletteItem> SamplePaletteItems() {
        return [
            new CommandPaletteItem { Title = "Light theme", Category = "Theme", Icon = MuzzleIconKind.Sun },
            new CommandPaletteItem { Title = "Dark theme", Category = "Theme", Icon = MuzzleIconKind.Moon, Keywords = "night" },
            new CommandPaletteItem { Title = "Data & media", Category = "Page", Icon = MuzzleIconKind.Database, Shortcut = "Ctrl+D" },
            new CommandPaletteItem { Title = "Boop the page", Category = "Action", Icon = MuzzleIconKind.Paw }
        ];
    }
}

public enum SampleMood {
    Sleepy,
    Playful,
    Grumpy
}

public sealed class SampleModel {
    [System.ComponentModel.Category("Identity")]
    [System.ComponentModel.DisplayName("Cat name")]
    [System.ComponentModel.Description("What the cat answers to (sometimes).")]
    public string Name { get; set; } = "Mochi";

    [System.ComponentModel.Category("Numbers")]
    public int Treats { get; set; } = 3;

    [System.ComponentModel.Category("Habits")]
    public bool IsIndoor { get; set; } = true;

    [System.ComponentModel.Category("Habits")]
    public SampleMood Mood { get; set; } = SampleMood.Playful;

    [System.ComponentModel.Category("Looks")]
    public Color Collar { get; set; } = Colors.Pink;

    [System.ComponentModel.Browsable(false)]
    public string Secret { get; set; } = "hidden";
}

public sealed record SampleRow(string Name, int Age, bool IsIndoor);