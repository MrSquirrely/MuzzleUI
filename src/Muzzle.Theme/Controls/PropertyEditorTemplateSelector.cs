using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Picks the Muzzle editor template for a <see cref="PropertyItem"/> based on its <see cref="PropertyEditorKind"/>.
/// The owning grid's <see cref="PropertyGrid.EditorTemplateSelector"/> is asked first; this selector answers when
/// that one returns <see langword="null"/>.
/// </summary>
public sealed class PropertyEditorTemplateSelector : DataTemplateSelector {
    private static readonly DataTemplate CustomTemplate = CreateCustomTemplate();

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.Text"/> editors.</summary>
    public DataTemplate? TextTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.Numeric"/> editors.</summary>
    public DataTemplate? NumericTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.Range"/> editors.</summary>
    public DataTemplate? RangeTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.Boolean"/> editors.</summary>
    public DataTemplate? BooleanTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.NullableBoolean"/> editors.</summary>
    public DataTemplate? NullableBooleanTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.Enum"/> editors.</summary>
    public DataTemplate? EnumTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.Color"/> and <see cref="PropertyEditorKind.Brush"/> editors.</summary>
    public DataTemplate? ColorTemplate { get; set; }

    /// <summary>Gets or sets the template for <see cref="PropertyEditorKind.ReadOnly"/> editors.</summary>
    public DataTemplate? ReadOnlyTemplate { get; set; }

    /// <inheritdoc />
    public override DataTemplate? SelectTemplate(object item, DependencyObject container) {
        if (item is not PropertyItem property) {
            return base.SelectTemplate(item, container);
        }
        if ((FindGrid(container)?.EditorTemplateSelector is DataTemplateSelector custom) && !ReferenceEquals(custom, this)
            && (custom.SelectTemplate(item, container) is DataTemplate chosen)) {
            return chosen;
        }
        return property.EditorKind switch {
            PropertyEditorKind.Custom => CustomTemplate,
            PropertyEditorKind.Text => TextTemplate,
            PropertyEditorKind.Numeric => NumericTemplate,
            PropertyEditorKind.Range => RangeTemplate,
            PropertyEditorKind.Boolean => BooleanTemplate,
            PropertyEditorKind.NullableBoolean => NullableBooleanTemplate,
            PropertyEditorKind.Enum => EnumTemplate,
            PropertyEditorKind.Color or PropertyEditorKind.Brush => ColorTemplate,
            _ => ReadOnlyTemplate
        };
    }

    private static PropertyGrid? FindGrid(DependencyObject? element) {
        DependencyObject? current = element;
        while ((current is not null) && (current is not PropertyGrid)) {
            current = (current is Visual) ? (VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current)) : LogicalTreeHelper.GetParent(current);
        }
        return current as PropertyGrid;
    }

    private static DataTemplate CreateCustomTemplate() {
        DataTemplate template = new() { VisualTree = new FrameworkElementFactory(typeof(CustomPropertyEditorHost)) };
        template.Seal();
        return template;
    }
}