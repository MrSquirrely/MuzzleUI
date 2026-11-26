using System.Linq;
using System.Windows;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A themed, synchronous replacement for <see cref="System.Windows.MessageBox"/>. Because it is a real window it
/// also works over hosted HWND content (WebView2, WindowsFormsHost) and before the app has a main window.
/// Button captions and icon names come from the <c>Muzzle.String.MessageBox.*</c> resources.
/// </summary>
/// <example>
/// <code>
/// MessageBoxResult result = MessageBox.Show(this, "Save your changes?", "Unsaved changes", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
/// </code>
/// </example>
public static class MessageBox {
    /// <summary>Shows a message, modal to the active window (or centred on the screen when there is none), and waits for the answer.</summary>
    /// <param name="text">The message.</param>
    /// <param name="caption">The title-bar text, or <see langword="null"/> for none.</param>
    /// <param name="button">Which buttons to show.</param>
    /// <param name="image">The icon beside the message.</param>
    /// <param name="defaultResult">The result whose button Enter presses; <see cref="MessageBoxResult.None"/> picks the first button.</param>
    /// <returns>The button the user chose.</returns>
    public static MessageBoxResult Show(string text, string? caption = null, MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None) {
        return ShowCore(null, text, caption, button, image, defaultResult);
    }

    /// <summary>Shows a message centred on, and modal to, <paramref name="owner"/> and waits for the answer.</summary>
    /// <param name="owner">The window the message belongs to, or <see langword="null"/> for the active window.</param>
    /// <param name="text">The message.</param>
    /// <param name="caption">The title-bar text, or <see langword="null"/> for none.</param>
    /// <param name="button">Which buttons to show.</param>
    /// <param name="image">The icon beside the message.</param>
    /// <param name="defaultResult">The result whose button Enter presses; <see cref="MessageBoxResult.None"/> picks the first button.</param>
    /// <returns>The button the user chose.</returns>
    public static MessageBoxResult Show(Window? owner, string text, string? caption = null, MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None) {
        return ShowCore(owner, text, caption, button, image, defaultResult);
    }

    private static MessageBoxResult ShowCore(Window? owner, string text, string? caption, MessageBoxButton button, MessageBoxImage image, MessageBoxResult defaultResult) {
        if ((Application.Current is Application application) && !application.Dispatcher.CheckAccess()) {
            return application.Dispatcher.Invoke(() => ShowCore(owner, text, caption, button, image, defaultResult));
        }
        MessageBoxWindow window = new(text, caption, button, image, defaultResult);
        Window? parent = owner ?? Application.Current?.Windows.OfType<Window>().FirstOrDefault(static candidate => candidate.IsActive);
        // Only a window that has been shown can own another one.
        if ((parent is not null) && parent.IsLoaded && !ReferenceEquals(parent, window)) {
            window.Owner = parent;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        _ = window.ShowDialog();
        return window.Result;
    }
}