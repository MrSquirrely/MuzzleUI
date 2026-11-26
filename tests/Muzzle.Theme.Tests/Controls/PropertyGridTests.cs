using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

[Collection(WpfCollection.Name)]
public sealed class PropertyGridTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public void SortMode_Declared_KeepsTheDeclaredOrderOfPropertiesAndCategories() {
        fixture.Run(() => {
            PropertyGrid grid = new() { SelectedObject = new SettingsModel() };
            Assert.Equal(["Font", "Scale", "Size", "Wrap", "Zoom"], grid.Properties.Select(static p => p.DisplayName));
            Assert.Equal(["Layout", "Text"], grid.Categories.Select(static c => c.Name));

            grid.SortMode = PropertyGridSortMode.Declared;
            // [Display(Order = 1)] lifts Font above Size; the rest keep the order the class declares them in.
            Assert.Equal(["Font", "Size", "Zoom", "Wrap", "Scale"], grid.Properties.Select(static p => p.DisplayName));
            Assert.Equal(["Text", "Layout"], grid.Categories.Select(static c => c.Name));
            Assert.Equal(["Font", "Size", "Scale"], grid.Categories[0].Items.Select(static p => p.DisplayName));
        });
    }

    [StaFact]
    public void PropertyItem_ExposesItsDescriptorAndAnEmptyDescription() {
        fixture.Run(() => {
            SettingsModel model = new();
            PropertyGrid grid = new() { SelectedObject = model };
            PropertyItem size = grid.Properties.Single(static p => p.Name == nameof(SettingsModel.Size));
            Assert.Same(model, size.Component);
            Assert.Equal(nameof(SettingsModel.Size), size.Descriptor.Name);
            Assert.NotNull(size.Descriptor.Attributes[typeof(RangeAttribute)]);
            Assert.Equal(string.Empty, size.Description);
        });
    }

    [StaFact]
    public void PropertyStep_SetsTheStepSnapsValuesAndFormatsThem() {
        fixture.Run(() => {
            SettingsModel model = new();
            PropertyGrid grid = new() { SelectedObject = model };
            PropertyItem scale = grid.Properties.Single(static p => p.Name == nameof(SettingsModel.Scale));
            Assert.Equal(PropertyEditorKind.Range, scale.EditorKind);
            Assert.Equal(0.25, scale.SmallChange);
            Assert.True(scale.SnapsToStep);
            Assert.True(scale.HasValueFormat);

            scale.NumericValue = 1.3;
            Assert.Equal(1.25, model.Scale);
            Assert.Equal(1.25.ToString("0.00×", System.Globalization.CultureInfo.CurrentCulture), scale.FormattedValue);
            scale.IncrementCommand.Execute(null);
            Assert.Equal(1.5, model.Scale);
            scale.NumericValue = 99;
            Assert.Equal(3, model.Scale);

            // Without the attribute nothing changes: a ranged double keeps its derived step and plain text.
            PropertyItem zoom = grid.Properties.Single(static p => p.Name == nameof(SettingsModel.Zoom));
            Assert.False(zoom.SnapsToStep);
            Assert.False(zoom.HasValueFormat);
        });
    }

    [StaFact]
    public void Reset_RestoresTheDefaultValueAndReportsWhetherItCan() {
        fixture.Run(() => {
            SettingsModel model = new() { Size = 19 };
            PropertyGrid grid = new() { SelectedObject = model };
            PropertyItem size = grid.Properties.Single(static p => p.Name == nameof(SettingsModel.Size));
            List<string?> changes = [];
            size.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
            Assert.True(size.CanReset);

            size.ResetCommand.Execute(null);
            Assert.Equal(14, model.Size);
            Assert.False(size.CanReset);
            Assert.Contains(nameof(PropertyItem.CanReset), changes);
            Assert.Equal("14", size.EditText);

            size.Value = 16;
            Assert.True(size.CanReset);
        });
    }

    [StaFact]
    public Task Template_HonoursHeaderTypeNameResetAndLabelWidthOptions() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            PropertyGrid grid = new() { Height = 600, SelectedObject = new SettingsModel() };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                await MuzzleWpfTestFixture.WaitAsync(100);
                FrameworkElement header = Assert.IsAssignableFrom<FrameworkElement>(grid.Template.FindName("Header", grid));
                FrameworkElement typeName = Assert.IsAssignableFrom<FrameworkElement>(grid.Template.FindName("TypeName", grid));
                Assert.Equal(Visibility.Visible, header.Visibility);
                Assert.DoesNotContain(Find<Button>(grid), static b => b.Command is not null && b.IsVisible && (b.Content is MuzzleIcon { Kind: MuzzleIconKind.Undo }));

                grid.ShowTypeNames = false;
                Assert.Equal(Visibility.Collapsed, typeName.Visibility);
                grid.ShowHeader = false;
                Assert.Equal(Visibility.Collapsed, header.Visibility);

                grid.ShowResetButtons = true;
                grid.LabelWidth = new GridLength(150);
                await MuzzleWpfTestFixture.WaitAsync(100);
                List<Button> resets = [.. Find<Button>(grid).Where(static b => b.IsVisible && (b.Content is MuzzleIcon { Kind: MuzzleIconKind.Undo }))];
                Assert.Equal(grid.Properties.Count, resets.Count);
                // Size has [DefaultValue(14)] and is 14: its reset button is disabled; nothing else declares a default.
                Button sizeReset = resets.Single(static b => ((PropertyItem)b.DataContext).Name == nameof(SettingsModel.Size));
                Assert.False(sizeReset.IsEnabled);
                ((PropertyItem)sizeReset.DataContext).Value = 20;
                Assert.True(sizeReset.IsEnabled);

                TextBlock label = Find<TextBlock>(grid).First(static t => t.IsVisible && (t.Text == "Wrap"));
                Grid row = Assert.IsType<Grid>(VisualTreeHelper.GetParent(label));
                Assert.Equal(150, row.ColumnDefinitions[0].ActualWidth, 0.5);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Templates_CanBeReplacedAndStringsOverridden() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            FrameworkElementFactory rowFactory = new(typeof(TextBlock));
            rowFactory.SetValue(FrameworkElement.TagProperty, "custom-row");
            rowFactory.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding(nameof(PropertyItem.DisplayName)));
            PropertyGrid grid = new() {
                Height = 600,
                SelectedObject = new SettingsModel(),
                RowTemplate = new DataTemplate { VisualTree = rowFactory }
            };
            grid.Resources["Muzzle.String.PropertyGrid.Header"] = "Einstellungen";
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                await MuzzleWpfTestFixture.WaitAsync(100);
                Assert.Equal(grid.Properties.Count, Find<TextBlock>(grid).Count(static t => (t.Tag as string) == "custom-row"));
                Assert.Contains(Find<TextBlock>(grid), static t => t.Text == "Einstellungen");
                Assert.DoesNotContain(Find<TextBlock>(grid), static t => t.Text == "Inspector");
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task CustomEditors_ComeFromTheEditorAttributeOrTheGridSelector() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            SettingsModel model = new();
            FrameworkElementFactory zoomFactory = new(typeof(Border));
            zoomFactory.SetValue(FrameworkElement.TagProperty, "selector-editor");
            PropertyGrid grid = new() {
                Height = 600,
                SelectedObject = model,
                EditorTemplateSelector = new NamedEditorSelector(nameof(SettingsModel.Zoom), new DataTemplate { VisualTree = zoomFactory })
            };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                await MuzzleWpfTestFixture.WaitAsync(100);
                PropertyItem font = grid.Properties.Single(static p => p.Name == nameof(SettingsModel.Font));
                Assert.Equal(PropertyEditorKind.Custom, font.EditorKind);
                Assert.IsType<FontEditor>(font.Editor);

                // [Editor]: the element the editor created is in the row, bound two-way through GetDependencyProperty.
                ComboBox combo = Assert.Single(Find<ComboBox>(grid), static c => (c.Tag as string) == "font-editor");
                Assert.Same(font, combo.DataContext);
                Assert.Equal("Segoe UI", combo.SelectedItem);
                combo.SelectedItem = "Consolas";
                Assert.Equal("Consolas", model.Font);

                // The grid's selector replaced Zoom's editor and left every other property to the built-in ones.
                Assert.Single(Find<Border>(grid), static b => (b.Tag as string) == "selector-editor");
                Assert.NotEmpty(Find<ToggleSwitch>(grid));
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    private static List<T> Find<T>(DependencyObject root) where T : DependencyObject {
        List<T> found = [];
        Collect(root, found);
        return found;
    }

    private static void Collect<T>(DependencyObject root, List<T> found) where T : DependencyObject {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) {
                found.Add(match);
            }
            Collect(child, found);
        }
    }

    private sealed class NamedEditorSelector(string propertyName, DataTemplate template) : DataTemplateSelector {
        public override DataTemplate? SelectTemplate(object item, DependencyObject container) {
            return (item is PropertyItem property) && (property.Name == propertyName) ? template : null;
        }
    }
}

public sealed class FontEditor : PropertyEditorBase {
    public override FrameworkElement CreateElement(PropertyItem item) {
        return new ComboBox { Tag = "font-editor", ItemsSource = new[] { "Segoe UI", "Consolas", "Georgia" } };
    }

    public override DependencyProperty? GetDependencyProperty() {
        return System.Windows.Controls.Primitives.Selector.SelectedItemProperty;
    }
}

public sealed class SettingsModel {
    [Category("Text")]
    [DefaultValue(14)]
    [Range(8, 40)]
    public int Size { get; set; } = 14;

    [Category("Layout")]
    [Range(0.5, 4.0)]
    public double Zoom { get; set; } = 1;

    [Category("Text")]
    [Display(Order = 1)]
    [Editor(typeof(FontEditor), typeof(PropertyEditorBase))]
    public string Font { get; set; } = "Segoe UI";

    [Category("Layout")]
    public bool Wrap { get; set; } = true;

    [Category("Text")]
    [Range(1.0, 3.0)]
    [PropertyStep(0.25, Format = "0.00×")]
    public double Scale { get; set; } = 1;
}