using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>
/// The window behind <see cref="MessageBox"/>: a small <see cref="MuzzleWindow"/> with an icon, a message and one
/// to three buttons. Enter presses the default button and Esc the cancelling one. Create it directly to show a
/// message without blocking (<c>Show()</c>, then read <see cref="Result"/> when it closes).
/// </summary>
public class MessageBoxWindow : MuzzleWindow {
    private const string StringPrefix = "Muzzle.String.MessageBox.";

    private readonly MessageBoxButton buttons;

    /// <summary>Initializes a new instance of the <see cref="MessageBoxWindow"/> class.</summary>
    /// <param name="text">The message.</param>
    /// <param name="caption">The title-bar text, or <see langword="null"/> for none.</param>
    /// <param name="button">Which buttons to show.</param>
    /// <param name="image">The icon beside the message.</param>
    /// <param name="defaultResult">The result whose button Enter presses; <see cref="MessageBoxResult.None"/> picks the first button.</param>
    public MessageBoxWindow(string text, string? caption = null, MessageBoxButton button = MessageBoxButton.OK,
        MessageBoxImage image = MessageBoxImage.None, MessageBoxResult defaultResult = MessageBoxResult.None) {
        buttons = button;
        Title = caption ?? string.Empty;
        SizeToContent = SizeToContent.WidthAndHeight;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowIcon = false;
        MinWidth = 320;
        MaxWidth = 560;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent(text ?? string.Empty, image, defaultResult);
    }

    /// <summary>
    /// Gets the button the user chose. While the window is open it is <see cref="MessageBoxResult.None"/>; closing
    /// it another way (Esc, the close button) gives <see cref="MessageBoxResult.Cancel"/> where there is a Cancel
    /// button and <see cref="MessageBoxResult.OK"/> for an OK-only message.
    /// </summary>
    public MessageBoxResult Result { get; private set; }

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e) {
        base.OnClosing(e);
        if (e.Cancel || (Result != MessageBoxResult.None)) {
            return;
        }
        switch (buttons) {
            case MessageBoxButton.OK:
                Result = MessageBoxResult.OK;
                break;
            case MessageBoxButton.OKCancel:
            case MessageBoxButton.YesNoCancel:
                Result = MessageBoxResult.Cancel;
                break;
            default:
                // Yes / No has no neutral answer, so the question stays until one of them is chosen.
                e.Cancel = true;
                break;
        }
    }

    private Grid BuildContent(string text, MessageBoxImage image, MessageBoxResult defaultResult) {
        Grid layout = new() { Margin = new Thickness(22, 8, 22, 18) };
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition());
        layout.RowDefinitions.Add(new RowDefinition());
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        if (CreateIcon(image) is MuzzleIcon icon) {
            layout.Children.Add(icon);
        }

        TextBlock message = new() { Text = text, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center, MinHeight = 22 };
        message.SetResourceReference(StyleProperty, "Muzzle.Text.Body");
        Grid.SetColumn(message, 1);
        layout.Children.Add(message);
        AutomationProperties.SetHelpText(this, text);

        StackPanel row = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
        Grid.SetRow(row, 1);
        Grid.SetColumnSpan(row, 2);
        MessageBoxResult[] results = buttons switch {
            MessageBoxButton.OKCancel => [MessageBoxResult.OK, MessageBoxResult.Cancel],
            MessageBoxButton.YesNo => [MessageBoxResult.Yes, MessageBoxResult.No],
            MessageBoxButton.YesNoCancel => [MessageBoxResult.Yes, MessageBoxResult.No, MessageBoxResult.Cancel],
            _ => [MessageBoxResult.OK]
        };
        MessageBoxResult accept = (Array.IndexOf(results, defaultResult) >= 0) ? defaultResult : results[0];
        MessageBoxResult cancel = buttons switch {
            MessageBoxButton.OK => MessageBoxResult.OK,
            MessageBoxButton.YesNo => MessageBoxResult.None,
            _ => MessageBoxResult.Cancel
        };
        foreach (MessageBoxResult result in results) {
            Button button = new() { MinWidth = 84, Margin = new Thickness(8, 0, 0, 0), IsDefault = result == accept, IsCancel = result == cancel };
            button.SetResourceReference(ContentProperty, StringPrefix + result);
            if (result == accept) {
                button.SetResourceReference(StyleProperty, "Muzzle.Button.Primary");
            }
            button.Click += (_, _) => {
                Result = result;
                Close();
            };
            row.Children.Add(button);
        }
        layout.Children.Add(row);
        Loaded += (_, _) => {
            foreach (object child in row.Children) {
                if (child is Button { IsDefault: true } button) {
                    button.Focus();
                }
            }
        };
        return layout;
    }

    private static MuzzleIcon? CreateIcon(MessageBoxImage image) {
        // MessageBoxImage has aliases: Hand / Stop = Error, Exclamation = Warning, Asterisk = Information.
        (MuzzleIconKind Kind, string Brush, string Name)? look = image switch {
            MessageBoxImage.Error => (MuzzleIconKind.Error, "Muzzle.Brush.ErrorText", "Error"),
            MessageBoxImage.Warning => (MuzzleIconKind.Warning, "Muzzle.Brush.WarningText", "Warning"),
            MessageBoxImage.Information => (MuzzleIconKind.Info, "Muzzle.Brush.InfoText", "Information"),
            MessageBoxImage.Question => (MuzzleIconKind.Help, "Muzzle.Brush.PrimaryText", "Question"),
            _ => null
        };
        if (look is not (MuzzleIconKind kind, string brush, string name)) {
            return null;
        }
        MuzzleIcon icon = new() { Kind = kind, IconSize = 30, Margin = new Thickness(0, 0, 16, 0), VerticalAlignment = VerticalAlignment.Top };
        icon.SetResourceReference(ForegroundProperty, brush);
        icon.SetResourceReference(AutomationProperties.NameProperty, StringPrefix + name);
        return icon;
    }
}