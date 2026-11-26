using System.Windows;
using System.Windows.Data;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Base class for a custom <see cref="PropertyGrid"/> editor. Put
/// <c>[Editor(typeof(MyEditor), typeof(PropertyEditorBase))]</c> on a property and the grid shows the element
/// <see cref="CreateElement"/> returns, with the <see cref="PropertyItem"/> as its <c>DataContext</c>.
/// The editor type needs a public parameterless constructor.
/// </summary>
public abstract class PropertyEditorBase {
    /// <summary>Creates the editing element for a property.</summary>
    /// <param name="item">The property being edited. Its descriptor and component give access to custom attributes.</param>
    /// <returns>The element to show in the property's row.</returns>
    public abstract FrameworkElement CreateElement(PropertyItem item);

    /// <summary>
    /// Gets the dependency property of the created element that the grid binds to the property value. The default
    /// is <see langword="null"/>: the element binds itself, through its <c>DataContext</c>.
    /// </summary>
    /// <returns>The dependency property to bind, or <see langword="null"/>.</returns>
    public virtual DependencyProperty? GetDependencyProperty() {
        return null;
    }

    /// <summary>Creates the binding applied to <see cref="GetDependencyProperty"/>. The default is a two-way binding to <see cref="PropertyItem.Value"/>.</summary>
    /// <param name="item">The property being edited.</param>
    /// <returns>The binding to apply.</returns>
    public virtual BindingBase CreateBinding(PropertyItem item) {
        return new Binding(nameof(PropertyItem.Value)) { Source = item, Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged };
    }

    internal FrameworkElement Build(PropertyItem item) {
        FrameworkElement element = CreateElement(item);
        element.DataContext = item;
        if (GetDependencyProperty() is DependencyProperty property) {
            BindingOperations.SetBinding(element, property, CreateBinding(item));
        }
        return element;
    }
}