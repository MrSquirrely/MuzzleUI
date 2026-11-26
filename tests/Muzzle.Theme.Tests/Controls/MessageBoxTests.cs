using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

[Collection(WpfCollection.Name)]
public sealed class MessageBoxTests(MuzzleWpfTestFixture fixture) {
    [StaTheory]
    [InlineData(MessageBoxButton.OK, "OK")]
    [InlineData(MessageBoxButton.OKCancel, "OK,Cancel")]
    [InlineData(MessageBoxButton.YesNo, "Yes,No")]
    [InlineData(MessageBoxButton.YesNoCancel, "Yes,No,Cancel")]
    public Task Window_ShowsTheButtonsForEachButtonSet(MessageBoxButton button, string expected) {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            MessageBoxWindow window = ShowOffScreen(new MessageBoxWindow("Feed the cat?", "Dinner", button, MessageBoxImage.Question));
            try {
                await MuzzleWpfTestFixture.WaitAsync(80);
                List<Button> buttons = Buttons(window);
                Assert.Equal(expected, string.Join(",", buttons.Select(static b => b.Content)));
                Assert.Same(buttons[0], Assert.Single(buttons, static b => b.IsDefault));
                Assert.Equal(button != MessageBoxButton.YesNo, buttons.Any(static b => b.IsCancel));
                MuzzleIcon icon = Assert.Single(Find<MuzzleIcon>(window), static i => i.Kind == MuzzleIconKind.Help);
                Assert.Equal("Question", AutomationProperties.GetName(icon));
                Assert.Equal(MessageBoxResult.None, window.Result);

                buttons[^1].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                Assert.False(window.IsVisible);
                Assert.Equal(expected.Split(',')[^1], window.Result.ToString());
            }
            finally {
                ForceClose(window);
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Window_ClosingWithoutAChoice_MapsToTheNeutralResult() {
        return fixture.RunAsync(async () => {
            MessageBoxWindow ok = ShowOffScreen(new MessageBoxWindow("Done."));
            MessageBoxWindow cancel = ShowOffScreen(new MessageBoxWindow("Save?", null, MessageBoxButton.YesNoCancel, MessageBoxImage.None, MessageBoxResult.No));
            MessageBoxWindow yesNo = ShowOffScreen(new MessageBoxWindow("Delete?", null, MessageBoxButton.YesNo));
            try {
                await MuzzleWpfTestFixture.WaitAsync(80);
                Assert.Equal("No", Assert.Single(Buttons(cancel), static b => b.IsDefault).Content);
                ok.Close();
                cancel.Close();
                yesNo.Close();
                Assert.Equal(MessageBoxResult.OK, ok.Result);
                Assert.Equal(MessageBoxResult.Cancel, cancel.Result);
                // A yes / no question has no neutral answer: it stays open until one is chosen.
                Assert.True(yesNo.IsVisible);
                Assert.Equal(MessageBoxResult.None, yesNo.Result);
            }
            finally {
                ForceClose(ok);
                ForceClose(cancel);
                ForceClose(yesNo);
            }
        });
    }

    [StaFact]
    public Task Show_BlocksUntilAnsweredAndHonoursTranslatedCaptions() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            Window owner = MuzzleWpfTestFixture.ShowOffScreen(new TextBlock { Text = "Owner" }, 600, 400);
            Application.Current.Resources["Muzzle.String.MessageBox.Yes"] = "Ja";
            try {
                await MuzzleWpfTestFixture.WaitAsync(50);
                string? yesCaption = null;
                bool owned = false;
                // ShowDialog pumps the dispatcher, so this runs while the message is up.
                _ = owner.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, () => {
                    MessageBoxWindow box = Application.Current.Windows.OfType<MessageBoxWindow>().Single();
                    owned = ReferenceEquals(box.Owner, owner) && (box.WindowStartupLocation == WindowStartupLocation.CenterOwner);
                    Button yes = Buttons(box)[0];
                    yesCaption = yes.Content as string;
                    yes.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                });
                MessageBoxResult result = Muzzle.Theme.Controls.MessageBox.Show(owner, "Adopt Mochi?", "Adoption", MessageBoxButton.YesNo, MessageBoxImage.Question);
                Assert.Equal(MessageBoxResult.Yes, result);
                Assert.Equal("Ja", yesCaption);
                Assert.True(owned, "The message was not owned by its owner window.");
            }
            finally {
                Application.Current.Resources.Remove("Muzzle.String.MessageBox.Yes");
                owner.Close();
            }
            fixture.AssertClean();
        });
    }

    private static MessageBoxWindow ShowOffScreen(MessageBoxWindow window) {
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000;
        window.Top = -20000;
        window.ShowActivated = false;
        window.Show();
        return window;
    }

    private static void ForceClose(MessageBoxWindow window) {
        if (!window.IsVisible) {
            return;
        }
        // A yes / no window refuses to close without an answer; give it one.
        Buttons(window)[0].RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
    }

    private static List<Button> Buttons(MessageBoxWindow window) {
        return [.. Find<Button>((DependencyObject)window.Content)];
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
}