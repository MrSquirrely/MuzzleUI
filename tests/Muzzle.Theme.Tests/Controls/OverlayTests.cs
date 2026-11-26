using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

[Collection(WpfCollection.Name)]
public sealed class OverlayTests(MuzzleWpfTestFixture fixture) {
    private static readonly TimeSpan LongWait = TimeSpan.FromMinutes(5);

    [StaFact]
    public Task Growl_AddsEveryTypeAndClearRemovesThem() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            const string Token = "growl-tests";
            Grid host = new();
            Growl.SetToken(host, Token);
            Growl.SetGrowlParent(host, true);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(host);
            try {
                GrowlContainer container = Assert.Single(host.Children.OfType<GrowlContainer>());
                Assert.NotNull(Growl.Info("Napping", "Info", Token, LongWait));
                Assert.NotNull(Growl.Success("Fed", "Success", Token, LongWait));
                Assert.NotNull(Growl.Warning("Treats low", "Warning", Token, LongWait));
                Assert.NotNull(Growl.Error("Glass broken", "Error", Token, LongWait));
                await MuzzleWpfTestFixture.WaitAsync(50);
                Assert.Equal(4, container.Children.OfType<GrowlCard>().Count());

                Growl.Clear(Token);
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => container.Children.Count == 0), $"{container.Children.Count} toasts left after Clear.");
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Growl_AutoDismissesAfterItsWaitTime() {
        return fixture.RunAsync(async () => {
            const string Token = "growl-auto";
            Grid host = new();
            Growl.SetToken(host, Token);
            Growl.SetGrowlParent(host, true);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(host);
            try {
                GrowlContainer container = host.Children.OfType<GrowlContainer>().Single();
                Growl.Info("Short", null, Token, TimeSpan.FromMilliseconds(200));
                Assert.Single(container.Children);
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => container.Children.Count == 0, 4000));
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public Task Growl_ParentFlagAddsAndRemovesTheContainer() {
        return fixture.RunAsync(async () => {
            Grid host = new();
            Growl.SetGrowlParent(host, true);
            Assert.Single(host.Children.OfType<GrowlContainer>());
            Growl.SetGrowlParent(host, false);
            Assert.Empty(host.Children.OfType<GrowlContainer>());
            await Task.CompletedTask;
        });
    }

    [StaFact]
    public Task Dialog_ShowAndCloseMountAndUnmountContent() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            const string Token = "dialog-tests";
            DialogContainer container = new() { Content = new TextBlock { Text = "Page" } };
            Dialog.SetToken(container, Token);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(container);
            try {
                TextBlock body = new() { Text = "Adopt Mochi?" };
                Dialog dialog = Dialog.Show(body, Token);
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => dialog.IsLoaded && body.IsLoaded));
                Assert.True(dialog.IsDescendantOf(container));
                Assert.True(container.HasModalDialog);
                Assert.Same(dialog, Dialog.GetDialog(body));
                Assert.True(HasFocusTrap(dialog), "The dialog surface should trap focus.");

                Dialog.Close(Token);
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => !dialog.IsLoaded && !container.HasModalDialog));
                Assert.False(body.IsDescendantOf(window));
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Dialog_ShowAsyncCompletesWithTheResult() {
        return fixture.RunAsync(async () => {
            const string Token = "dialog-result";
            DialogContainer container = new();
            Dialog.SetToken(container, Token);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(container);
            try {
                Task<string?> pending = Dialog.ShowAsync<string>(new TextBlock { Text = "Name?" }, Token);
                Dialog dialog = Assert.IsType<Dialog>(await WaitForDialogAsync(container));
                dialog.CloseWithResult("Mochi");
                Task finished = await Task.WhenAny(pending, Task.Delay(3000));
                Assert.Same(pending, finished);
                Assert.Equal("Mochi", await pending);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Dialog_WithAnUnknownToken_Throws() {
        fixture.Run(() => {
            Assert.Throws<InvalidOperationException>(() => Dialog.Show(new TextBlock(), "no-such-token"));
        });
    }

    [StaTheory]
    [InlineData(Dock.Left)]
    [InlineData(Dock.Right)]
    [InlineData(Dock.Top)]
    [InlineData(Dock.Bottom)]
    public Task Drawer_OpensAndClosesFromEveryEdge(Dock dock) {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            string token = "drawer-" + dock;
            DrawerContainer container = new() { Children = { new TextBlock { Text = "Page" } } };
            Drawer.SetToken(container, token);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(container);
            try {
                TextBlock content = new() { Text = $"{dock} drawer" };
                Drawer drawer = Drawer.Show(content, dock, $"{dock} header", token);
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => drawer.IsLoaded && content.IsLoaded));
                Assert.True(drawer.IsOpen);
                Assert.Equal(dock, drawer.Dock);
                Assert.True(drawer.IsDescendantOf(container));
                Assert.True(HasFocusTrap(drawer), "The drawer panel should trap focus.");

                drawer.IsOpen = false;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => drawer.Parent is null), "A drawer shown from code detaches after closing.");
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Drawer_DeclaredInXaml_TogglesIsOpen() {
        return fixture.RunAsync(async () => {
            Drawer drawer = new() { Dock = Dock.Right, Content = new TextBlock { Text = "Declared" } };
            DrawerContainer container = new() { Children = { new TextBlock { Text = "Page" }, drawer } };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(container);
            try {
                Assert.False(drawer.IsOpen);
                Assert.Equal(Visibility.Collapsed, drawer.Visibility);
                drawer.IsOpen = true;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => drawer.Visibility == Visibility.Visible));
                drawer.IsOpen = false;
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => drawer.Visibility == Visibility.Collapsed));
                Assert.Same(container, drawer.Parent);
            }
            finally {
                window.Close();
            }
        });
    }

    private static async Task<Dialog?> WaitForDialogAsync(DependencyObject root) {
        Dialog? found = null;
        await MuzzleWpfTestFixture.WaitForAsync(() => (found = FindFirst<Dialog>(root)) is not null);
        return found;
    }

    private static bool HasFocusTrap(DependencyObject root) {
        if (FocusTrap.GetIsEnabled(root)) {
            return true;
        }
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            if (HasFocusTrap(VisualTreeHelper.GetChild(root, i))) {
                return true;
            }
        }
        return false;
    }

    private static T? FindFirst<T>(DependencyObject root) where T : DependencyObject {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++) {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) {
                return match;
            }
            if (FindFirst<T>(child) is T nested) {
                return nested;
            }
        }
        return null;
    }
}