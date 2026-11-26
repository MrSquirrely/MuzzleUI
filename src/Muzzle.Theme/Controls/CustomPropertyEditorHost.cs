using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>The root of the template for <see cref="PropertyEditorKind.Custom"/>: hosts the element a <see cref="PropertyEditorBase"/> creates.</summary>
internal sealed class CustomPropertyEditorHost : Decorator {
    public CustomPropertyEditorHost() {
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e) {
        Child = (e.NewValue is PropertyItem { Editor: PropertyEditorBase editor } item) ? editor.Build(item) : null;
    }
}