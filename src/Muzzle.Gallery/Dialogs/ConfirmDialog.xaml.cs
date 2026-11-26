using System;
using System.Windows;
using System.Windows.Controls;

using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Dialogs;

/// <summary>A yes/no dialog using the <see cref="IDialogResultable{TResult}"/> pattern (works the same on a ViewModel).</summary>
public partial class ConfirmDialog : UserControl, IDialogResultable<bool> {
    public ConfirmDialog(string title, string message) {
        InitializeComponent();
        TitleText.Text = title;
        MessageText.Text = message;
    }

    public bool Result { get; set; }

    public Action? CloseAction { get; set; }

    private void OnYesClick(object sender, RoutedEventArgs e) {
        Result = true;
        CloseAction?.Invoke();
    }

    private void OnNoClick(object sender, RoutedEventArgs e) {
        Result = false;
        CloseAction?.Invoke();
    }
}