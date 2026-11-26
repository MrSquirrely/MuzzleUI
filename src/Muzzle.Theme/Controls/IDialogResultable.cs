using System;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Implement on dialog content (a UserControl or its DataContext / ViewModel) to return a typed result.
/// When shown through <see cref="Dialog.ShowAsync{TResult}(object, string?)"/>, <see cref="CloseAction"/> is
/// wired to close the dialog with <see cref="Result"/>.
/// </summary>
public interface IDialogResultable<TResult> {
    /// <summary>Gets or sets the result handed back when the dialog closes.</summary>
    TResult Result { get; set; }

    /// <summary>Gets or sets the action that closes the dialog; Dialog assigns it when the content is shown.</summary>
    Action? CloseAction { get; set; }
}