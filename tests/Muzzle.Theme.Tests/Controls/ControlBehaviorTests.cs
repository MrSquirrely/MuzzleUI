using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

[Collection(WpfCollection.Name)]
public sealed class ControlBehaviorTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public Task PropertyGrid_ReflectsCategorisedEditors() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            PropertyGrid grid = new() { Height = 600, SelectedObject = new SampleModel() };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                await MuzzleWpfTestFixture.WaitAsync(100);
                Dictionary<string, PropertyItem> items = grid.Properties.ToDictionary(static p => p.Name);
                Assert.Equal(PropertyEditorKind.Text, items[nameof(SampleModel.Name)].EditorKind);
                Assert.Equal(PropertyEditorKind.Numeric, items[nameof(SampleModel.Treats)].EditorKind);
                Assert.Equal(PropertyEditorKind.Boolean, items[nameof(SampleModel.IsIndoor)].EditorKind);
                Assert.Equal(PropertyEditorKind.Enum, items[nameof(SampleModel.Mood)].EditorKind);
                Assert.Equal(PropertyEditorKind.Color, items[nameof(SampleModel.Collar)].EditorKind);
                Assert.DoesNotContain(nameof(SampleModel.Secret), items.Keys);
                Assert.Equal("Cat name", items[nameof(SampleModel.Name)].DisplayName);
                Assert.Equal("Habits", items[nameof(SampleModel.Mood)].Category);
                Assert.Equal(Enum.GetValues<SampleMood>().Cast<object>(), items[nameof(SampleModel.Mood)].EnumValues);
                Assert.Equal(["Habits", "Identity", "Looks", "Numbers"], grid.Categories.Select(static c => c.Name).OrderBy(static n => n));

                // Editors are realised in the visual tree.
                Assert.NotEmpty(Find<TextBox>(grid));
                Assert.NotEmpty(Find<ComboBox>(grid));

                // Edits write back to the object.
                items[nameof(SampleModel.Treats)].Value = 7;
                Assert.Equal(7, ((SampleModel)grid.SelectedObject!).Treats);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public void NumericUpDown_StepsAndClampsToTheRange() {
        fixture.Run(() => {
            NumericUpDown numeric = new() { Minimum = 0, Maximum = 5, Increment = 2, Value = 4 };
            int changes = 0;
            numeric.ValueChanged += (_, _) => changes++;
            numeric.Step(1);
            Assert.Equal(5, numeric.Value);
            numeric.Step(-10);
            Assert.Equal(0, numeric.Value);
            numeric.Value = 99;
            Assert.Equal(5, numeric.Value);
            numeric.Value = -3;
            Assert.Equal(0, numeric.Value);
            numeric.Maximum = 3;
            numeric.Value = 3;
            numeric.Step(1);
            Assert.Equal(3, numeric.Value);
            Assert.True(changes >= 4);
        });
    }

    [StaFact]
    public void TagInput_AddsAndRemovesTags() {
        fixture.Run(() => {
            ObservableCollection<string> tags = ["fluffy"];
            TagInput input = new() { Tags = tags, MaxTags = 3 };
            Assert.True(input.AddTag("orange"));
            Assert.False(input.AddTag("orange"), "Duplicates are refused by default.");
            Assert.False(input.AddTag("   "), "Blank tags are refused.");
            Assert.True(input.AddTag("sleepy"));
            Assert.False(input.AddTag("chonky"), "MaxTags is 3.");
            Assert.Equal(["fluffy", "orange", "sleepy"], tags);
            input.RemoveTagAt(0);
            Assert.Equal(["orange", "sleepy"], tags);
            input.AllowDuplicates = true;
            Assert.True(input.AddTag("orange"));
        });
    }

    [StaFact]
    public void RangeSlider_KeepsLowerBelowUpper() {
        fixture.Run(() => {
            RangeSlider slider = new() { Minimum = 0, Maximum = 100, LowerValue = 20, UpperValue = 70, MinimumRange = 10 };
            slider.LowerValue = 95;
            Assert.Equal(60, slider.LowerValue);
            Assert.True(slider.LowerValue <= slider.UpperValue);
            slider.UpperValue = 5;
            Assert.Equal(70, slider.UpperValue);
            slider.LowerValue = -50;
            Assert.Equal(0, slider.LowerValue);
            slider.UpperValue = 500;
            Assert.Equal(100, slider.UpperValue);
            slider.Maximum = 50;
            Assert.Equal(50, slider.UpperValue);
            Assert.True(slider.LowerValue <= slider.UpperValue - slider.MinimumRange);
        });
    }

    [StaTheory]
    [InlineData("", 0)]
    [InlineData("abc", 0)]
    [InlineData("password", 0)]
    [InlineData("aaaaaaaaaa", 0)]
    [InlineData("mochi2024", 2)]
    [InlineData("Sunny-Window-7", 4)]
    [InlineData("purr!Biscuit#Nap42", 4)]
    public void PasswordStrengthMeter_EstimatesStrength(string password, int expected) {
        fixture.Run(() => {
            Assert.Equal(expected, PasswordStrengthMeter.Estimate(password));
            PasswordStrengthMeter meter = new() { Password = password };
            Assert.Equal(expected, meter.StrengthScore);
        });
    }

    [StaFact]
    public void PasswordStrengthMeter_FollowsAPasswordBoxAndLabels() {
        fixture.Run(() => {
            PasswordBox box = new();
            PasswordStrengthMeter meter = new() { PasswordBox = box };
            Assert.Equal("Enter a password", meter.StrengthLabel);
            box.Password = "purr!Biscuit#Nap42";
            Assert.Equal(4, meter.StrengthScore);
            Assert.Equal("Strong", meter.StrengthLabel);
            box.Password = "password";
            Assert.Equal("Too weak", meter.StrengthLabel);
            meter.StrengthScore = 9;
            Assert.Equal(4, meter.StrengthScore);
        });
    }

    [StaFact]
    public void CommandPalette_FiltersWithFuzzySearch() {
        fixture.Run(() => {
            CommandPalette palette = new() { ItemsSource = ControlTemplateTests.SamplePaletteItems() };
            Assert.Equal(4, palette.Results.Count);
            Assert.True(palette.HasResults);
            palette.SearchText = "drk thm";
            Assert.Equal("Dark theme", palette.Results[0].ToString());
            palette.SearchText = "night";
            Assert.Equal("Dark theme", Assert.Single(palette.Results).ToString());
            palette.SearchText = "zzzqqq";
            Assert.Empty(palette.Results);
            Assert.False(palette.HasResults);
            palette.SearchText = string.Empty;
            Assert.Equal(["Light theme", "Dark theme", "Data & media", "Boop the page"], palette.Results.Select(static r => r.ToString()));
            palette.MaxResults = 2;
            Assert.Equal(2, palette.Results.Count);
        });
    }

    [Theory]
    [InlineData("dm", "Data & media", true)]
    [InlineData("data", "Data & media", true)]
    [InlineData("xyz", "Data & media", false)]
    [InlineData("", "anything", true)]
    [InlineData("a", null, false)]
    public void FuzzyScore_MatchesSubsequences(string query, string? text, bool matches) {
        Assert.Equal(matches, CommandPalette.FuzzyScore(query, text) is not null);
    }

    [Fact]
    public void FuzzyScore_PrefersPrefixesAndWordStarts() {
        int prefix = CommandPalette.FuzzyScore("dar", "Dark theme")!.Value;
        int middle = CommandPalette.FuzzyScore("dar", "Sidebar")!.Value;
        Assert.True(prefix > middle);
    }

    [StaFact]
    public void CommandPalette_InvokeRunsTheCommandAndCloses() {
        fixture.Run(() => {
            int runs = 0;
            object? invoked = null;
            CommandPaletteItem item = new() { Title = "Boop", Command = new DelegateCommand(() => runs++) };
            CommandPalette palette = new() { ItemsSource = new[] { item } };
            palette.ItemInvoked += (_, e) => invoked = e.Item;
            palette.Open();
            Assert.True(palette.IsOpen);
            palette.Invoke(item);
            Assert.False(palette.IsOpen);
            Assert.Equal(1, runs);
            Assert.Same(item, invoked);
        });
    }

    [StaFact]
    public Task SplitButton_PrimarySegmentClicksAndMenuOpens() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            int commandRuns = 0;
            int clicks = 0;
            SplitButton split = new() { Content = "Save", Command = new DelegateCommand(() => commandRuns++) };
            MenuItem saveAs = new() { Header = "Save as…" };
            split.MenuItems.Add(saveAs);
            split.Click += (_, _) => clicks++;
            Window window = MuzzleWpfTestFixture.ShowOffScreen(split);
            try {
                Button primary = Assert.IsType<Button>(split.Template.FindName(SplitButton.PrimaryButtonPartName, split));
                primary.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, primary));
                Assert.Equal(1, clicks);
                Assert.Equal(1, commandRuns);
                Assert.Equal(Application.Current.FindResource("Muzzle.MenuItem"), saveAs.Style);
                split.Open();
                await MuzzleWpfTestFixture.WaitAsync(100);
                Assert.True(split.IsDropDownOpen);
                saveAs.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, saveAs));
                Assert.False(split.IsDropDownOpen, "A menu-item click closes the menu.");
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public void CodeBlock_ColoursCodeAndNumbersLines() {
        fixture.Run(() => {
            CodeBlock block = new() { Code = "<Button\n    Content=\"Hi\" />\n<!-- done -->", Language = "xaml", ShowLineNumbers = true };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(block);
            try {
                TextBlock code = Assert.IsType<TextBlock>(block.Template.FindName(CodeBlock.CodePartName, block));
                TextBlock numbers = Assert.IsType<TextBlock>(block.Template.FindName(CodeBlock.LineNumbersPartName, block));
                Assert.Equal("1\n2\n3", numbers.Text);
                Assert.Equal(Visibility.Visible, numbers.Visibility);
                Assert.True(code.Inlines.Count > 3);
                Assert.Equal(block.Code.Replace("\r\n", "\n"), new TextRange(code.ContentStart, code.ContentEnd).Text.Replace("\r\n", "\n"));
                block.ShowLineNumbers = false;
                Assert.Equal(Visibility.Collapsed, numbers.Visibility);
                block.Language = "plain";
                Assert.Single(code.Inlines);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Avatar_FallsBackToStableInitials() {
        fixture.Run(() => {
            Avatar avatar = new() { DisplayName = "Ada Lovelace" };
            Assert.Equal("AL", avatar.Initials);
            Assert.False(avatar.IsImageVisible);
            Assert.InRange(avatar.ColorIndex, 0, Avatar.PaletteSize - 1);
            Assert.Equal(Avatar.GetColorIndex(" ada lovelace "), avatar.ColorIndex);
            avatar.Source = new DrawingImage(new GeometryDrawing(Brushes.Pink, null, new RectangleGeometry(new Rect(0, 0, 10, 10))));
            Assert.True(avatar.IsImageVisible);
            avatar.Source = null;
            Assert.False(avatar.IsImageVisible);
        });
    }

    [StaFact]
    public void Carousel_NavigatesAndLoops() {
        fixture.Run(() => {
            Carousel carousel = new() { Height = 120, ItemsSource = new[] { "One", "Two", "Three" } };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(carousel);
            try {
                Assert.Equal(0, carousel.SelectedIndex);
                carousel.Next();
                Assert.Equal(1, carousel.SelectedIndex);
                Assert.Equal("Two", carousel.SelectedItem);
                carousel.SelectedIndex = 0;
                carousel.Previous();
                Assert.Equal(2, carousel.SelectedIndex);
                carousel.IsLooping = false;
                carousel.Next();
                Assert.Equal(2, carousel.SelectedIndex);
                carousel.SelectedIndex = 99;
                Assert.Equal(2, carousel.SelectedIndex);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void ImageViewer_ClampsZoomAndResets() {
        fixture.Run(() => {
            DrawingImage picture = new(new GeometryDrawing(Brushes.Pink, null, new RectangleGeometry(new Rect(0, 0, 400, 300))));
            ImageViewer viewer = new() { Width = 400, Height = 300, Source = picture, MaxZoom = 4 };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(viewer);
            try {
                viewer.ZoomTo(2.5, new Point(200, 150), animate: false);
                Assert.Equal(2.5, viewer.Zoom, 3);
                viewer.ZoomTo(100, new Point(0, 0), animate: false);
                Assert.Equal(4, viewer.Zoom, 3);
                viewer.ZoomTo(0.01, new Point(0, 0), animate: false);
                Assert.Equal(viewer.MinZoom, viewer.Zoom, 3);
                viewer.ResetView(animate: false);
                Assert.Equal(1, viewer.Zoom, 3);
                Assert.Equal(new Vector(0, 0), viewer.Offset);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Badge_CapsAndHidesZero() {
        fixture.Run(() => {
            Badge badge = new() { Value = 150, MaxValue = 99 };
            Assert.Equal("99+", badge.DisplayText);
            Assert.True(badge.IsBadgeVisible);
            badge.Value = 0;
            Assert.False(badge.IsBadgeVisible);
            badge.ShowZero = true;
            Assert.True(badge.IsBadgeVisible);
        });
    }

    [StaFact]
    public void Pagination_And_StepBar_ClampTheirPositions() {
        fixture.Run(() => {
            Pagination pagination = new() { PageCount = 10, CurrentPage = 3 };
            pagination.CurrentPage = 50;
            Assert.Equal(10, pagination.CurrentPage);
            Assert.Contains(pagination.Slots, static s => s.IsCurrent && (s.Page == 10));

            StepBar steps = new();
            steps.Items.Add(new StepBarItem { Content = "A" });
            steps.Items.Add(new StepBarItem { Content = "B" });
            steps.Next();
            steps.Next();
            steps.Next();
            Assert.Equal(2, steps.StepIndex);
            steps.Previous();
            steps.Previous();
            steps.Previous();
            Assert.Equal(0, steps.StepIndex);
        });
    }

    [StaFact]
    public void Rating_And_ColorPicker_KeepTheirValuesConsistent() {
        fixture.Run(() => {
            Rating rating = new() { Maximum = 5, Value = 9 };
            Assert.Equal(5, rating.Value);

            ColorPicker picker = new() { SelectedColor = Color.FromRgb(0x9F, 0xD8, 0xCB) };
            Assert.Equal(0x9F, picker.Red);
            Assert.Equal(0xD8, picker.Green);
            Assert.Equal(0xCB, picker.Blue);
            Assert.Equal(Color.FromRgb(0x9F, 0xD8, 0xCB), picker.SelectedBrush!.Color);
            picker.HexText = "#F497A9";
            Assert.Equal(Color.FromRgb(0xF4, 0x97, 0xA9), picker.SelectedColor);
        });
    }

    [StaFact]
    public void ShortcutBadge_ParsesKeysAndChords() {
        fixture.Run(() => {
            Assert.Equal(["Ctrl", "Shift", "P"], ShortcutBadge.ParseKeys("Ctrl+Shift+P"));
            Assert.Equal(["Ctrl", "K", ",", "Ctrl", "S"], ShortcutBadge.ParseKeys("Ctrl+K, Ctrl+S"));
            Assert.Equal(["Ctrl", "+"], ShortcutBadge.ParseKeys("Ctrl++"));
            Assert.Equal(["Esc", "↑"], ShortcutBadge.ParseKeys("escape+up"));
            Assert.Empty(ShortcutBadge.ParseKeys("  "));
        });
    }

    [StaFact]
    public void MuzzleIcon_EveryKindResolvesAGeometryOnTheGrid() {
        fixture.Run(() => {
            MuzzleIconKind[] kinds = Enum.GetValues<MuzzleIconKind>().Where(static k => k != MuzzleIconKind.None).ToArray();
            Assert.True(kinds.Length >= 120, $"Only {kinds.Length} icons.");
            foreach (MuzzleIconKind kind in kinds) {
                Geometry? geometry = MuzzleIcon.GetGeometry(kind);
                Assert.True(geometry is not null, $"{kind} has no Muzzle.Icon.{kind} geometry.");
                Rect bounds = geometry!.Bounds;
                Assert.False(bounds.IsEmpty, $"{kind} is empty.");
                Assert.True((bounds.Left >= -0.5) && (bounds.Top >= -0.5) && (bounds.Right <= 24.5) && (bounds.Bottom <= 24.5), $"{kind} spills off the 24 x 24 grid: {bounds}.");
            }
            Assert.Null(MuzzleIcon.GetGeometry(MuzzleIconKind.None));
        });
    }

    [StaFact]
    public void MuzzleIcon_ControlFollowsItsKind() {
        fixture.Run(() => {
            MuzzleIcon icon = new() { Kind = MuzzleIconKind.Heart };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(icon);
            try {
                Assert.Same(MuzzleIcon.GetGeometry(MuzzleIconKind.Heart), icon.Data);
                icon.Kind = MuzzleIconKind.Paw;
                Assert.Same(MuzzleIcon.GetGeometry(MuzzleIconKind.Paw), icon.Data);
                icon.Kind = MuzzleIconKind.None;
                Assert.Null(icon.Data);
            }
            finally {
                window.Close();
            }
        });
    }

    private static List<T> Find<T>(DependencyObject root) where T : DependencyObject {
        List<T> found = [];
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) {
                found.Add(match);
            }
            found.AddRange(Find<T>(child));
        }
        return found;
    }
}

internal sealed class DelegateCommand(Action execute) : System.Windows.Input.ICommand {
    public event EventHandler? CanExecuteChanged {
        add { }
        remove { }
    }

    public bool CanExecute(object? parameter) {
        return true;
    }

    public void Execute(object? parameter) {
        execute();
    }
}