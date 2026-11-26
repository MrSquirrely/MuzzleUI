using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>Picks the Muzzle editor template for a <see cref="PropertyItem"/> based on its <see cref="PropertyEditorKind"/>.</summary>
public sealed class PropertyEditorTemplateSelector : DataTemplateSelector {
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
        return property.EditorKind switch {
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
}