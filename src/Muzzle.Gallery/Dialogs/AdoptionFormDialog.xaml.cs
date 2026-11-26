using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Dialogs;

/// <summary>The result of the adoption form dialog.</summary>
public sealed record AdoptionRequest(string Name, string Kitten, bool IndoorOnly);

/// <summary>A form dialog. Cancel uses Dialog.CloseCommand (no result); Adopt closes with an <see cref="AdoptionRequest"/>.</summary>
public partial class AdoptionFormDialog : UserControl {
    public AdoptionFormDialog() {
        InitializeComponent();
    }

    private void OnAdoptClick(object sender, RoutedEventArgs e) {
        string name = NameBox.Text.Trim();
        if (name.Length == 0) {
            // Jelly "nope": wobble the empty field and keep the dialog open.
            Jelly.Wobble(NameBox);
            NameBox.Focus();
            return;
        }
        string kitten = (KittenBox.SelectedItem as ComboBoxItem)?.Content as string ?? "a kitten";
        AdoptionRequest request = new(name, kitten.Split(" · ")[0], IndoorSwitch.IsChecked == true);
        Dialog.GetDialog(this)?.CloseWithResult(request);
    }
}