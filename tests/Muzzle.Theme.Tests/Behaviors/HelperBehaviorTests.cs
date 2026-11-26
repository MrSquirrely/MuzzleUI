using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using Xunit;

namespace Muzzle.Theme.Tests.Behaviors;

[Collection(WpfCollection.Name)]
public sealed class ClipHelperTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public void Grid_GetsARoundedClipMatchingItsBounds() {
        fixture.Run(() => {
            Grid grid = new() { Width = 200, Height = 100, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            ClipHelper.SetCornerRadius(grid, new CornerRadius(16));
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                RectangleGeometry clip = Assert.IsType<RectangleGeometry>(grid.Clip);
                Assert.Equal(new Rect(0, 0, 200, 100), clip.Rect);
                Assert.Equal(16, clip.RadiusX);
                Assert.Equal(16, clip.RadiusY);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Clip_FollowsSizeChanges() {
        fixture.Run(() => {
            Grid grid = new() { Width = 200, Height = 100, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            ClipHelper.SetCornerRadius(grid, new CornerRadius(10));
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                grid.Width = 320;
                grid.Height = 140;
                window.UpdateLayout();
                RectangleGeometry clip = Assert.IsType<RectangleGeometry>(grid.Clip);
                Assert.Equal(new Rect(0, 0, 320, 140), clip.Rect);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Border_ClipsItsChildInsideTheBorderAndPadding() {
        fixture.Run(() => {
            Grid child = new();
            Border border = new() {
                Width = 200,
                Height = 100,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(2),
                Padding = new Thickness(3),
                BorderBrush = Brushes.Pink,
                Child = child
            };
            ClipHelper.SetCornerRadius(border, new CornerRadius(12));
            Window window = MuzzleWpfTestFixture.ShowOffScreen(border);
            try {
                Assert.Null(border.Clip);
                RectangleGeometry clip = Assert.IsType<RectangleGeometry>(child.Clip);
                Assert.Equal(new Rect(0, 0, 190, 90), clip.Rect);
                Assert.Equal(7, clip.RadiusX);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void PerCornerRadii_ProduceAMatchingGeometry() {
        fixture.Run(() => {
            Grid grid = new() { Width = 120, Height = 80, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
            ClipHelper.SetCornerRadius(grid, new CornerRadius(16, 0, 16, 0));
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                Assert.NotNull(grid.Clip);
                Assert.IsNotType<RectangleGeometry>(grid.Clip);
                Assert.Equal(new Rect(0, 0, 120, 80), grid.Clip.Bounds);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void ZeroRadius_RemovesTheClip() {
        fixture.Run(() => {
            Grid grid = new() { Width = 100, Height = 60 };
            ClipHelper.SetCornerRadius(grid, new CornerRadius(8));
            Window window = MuzzleWpfTestFixture.ShowOffScreen(grid);
            try {
                Assert.NotNull(grid.Clip);
                ClipHelper.SetCornerRadius(grid, new CornerRadius(0));
                Assert.Null(grid.Clip);
            }
            finally {
                window.Close();
            }
        });
    }

    [Fact]
    public void CreateRoundedRectangle_ClampsOversizedRadii() {
        Geometry geometry = ClipHelper.CreateRoundedRectangle(new Rect(0, 0, 40, 20), new CornerRadius(999));
        RectangleGeometry rectangle = Assert.IsType<RectangleGeometry>(geometry);
        Assert.Equal(10, rectangle.RadiusX);
        Assert.True(geometry.IsFrozen);
    }
}

[Collection(WpfCollection.Name)]
public sealed class FocusAndEscapeTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public void Escape_ExecutesTheCommandWithItsParameter() {
        fixture.Run(() => {
            TextBox input = new();
            Border panel = new() { Child = input };
            RecordingCommand command = new();
            EscapeKeyBehavior.SetCommand(panel, command);
            EscapeKeyBehavior.SetCommandParameter(panel, "close-me");
            Window window = MuzzleWpfTestFixture.ShowOffScreen(panel);
            try {
                KeyEventArgs args = PressKey(input, Key.Escape);
                Assert.Equal(1, command.Executions);
                Assert.Equal("close-me", command.LastParameter);
                Assert.True(args.Handled);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Escape_SetsBoundIsOpenToFalse() {
        fixture.Run(() => {
            ToggleButton toggle = new() { IsChecked = true };
            TextBox input = new();
            Border panel = new() { Child = input };
            panel.SetBinding(EscapeKeyBehavior.IsOpenProperty, new Binding(nameof(ToggleButton.IsChecked)) { Source = toggle });
            Window window = MuzzleWpfTestFixture.ShowOffScreen(new StackPanel { Children = { toggle, panel } });
            try {
                Assert.True(EscapeKeyBehavior.GetIsOpen(panel));
                PressKey(input, Key.Escape);
                Assert.False(EscapeKeyBehavior.GetIsOpen(panel));
                Assert.False(toggle.IsChecked);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void OtherKeys_AndDisabledCommands_DoNothing() {
        fixture.Run(() => {
            TextBox input = new();
            Border panel = new() { Child = input };
            RecordingCommand command = new() { Enabled = false };
            EscapeKeyBehavior.SetCommand(panel, command);
            EscapeKeyBehavior.SetIsOpen(panel, true);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(panel);
            try {
                KeyEventArgs enter = PressKey(input, Key.Enter);
                Assert.False(enter.Handled);
                Assert.True(EscapeKeyBehavior.GetIsOpen(panel));
                KeyEventArgs escape = PressKey(input, Key.Escape);
                Assert.Equal(0, command.Executions);
                Assert.False(EscapeKeyBehavior.GetIsOpen(panel));
                Assert.True(escape.Handled);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void FocusTrap_CyclesTabNavigationInsideTheContainer() {
        fixture.Run(() => {
            Border panel = new();
            FocusTrap.SetIsEnabled(panel, true);
            Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation(panel));
            Assert.Equal(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetControlTabNavigation(panel));
            Assert.Equal(KeyboardNavigationMode.Contained, KeyboardNavigation.GetDirectionalNavigation(panel));
            FocusTrap.SetIsEnabled(panel, false);
            Assert.Equal(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(panel));
        });
    }

    private static KeyEventArgs PressKey(UIElement target, Key key) {
        PresentationSource source = PresentationSource.FromVisual(target) ?? throw new InvalidOperationException("Target is not on screen.");
        KeyEventArgs args = new(Keyboard.PrimaryDevice, source, Environment.TickCount, key) { RoutedEvent = Keyboard.PreviewKeyDownEvent };
        target.RaiseEvent(args);
        return args;
    }

    private sealed class RecordingCommand : ICommand {
        public event EventHandler? CanExecuteChanged {
            add { }
            remove { }
        }

        public bool Enabled { get; init; } = true;

        public int Executions { get; private set; }

        public object? LastParameter { get; private set; }

        public bool CanExecute(object? parameter) {
            return Enabled;
        }

        public void Execute(object? parameter) {
            Executions++;
            LastParameter = parameter;
        }
    }
}

[Collection(WpfCollection.Name)]
public sealed class ScrollViewerAssistTests(MuzzleWpfTestFixture fixture) {
    [StaFact]
    public Task SmoothScroll_GlidesToTheWheelTarget() {
        return fixture.RunAsync(async () => {
            ScrollViewer viewer = new() { Height = 100, Content = new Border { Height = 1000 } };
            ScrollViewerAssist.SetEnableSmoothScroll(viewer, true);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(viewer);
            try {
                MouseWheelEventArgs up = Wheel(viewer, 120);
                Assert.False(up.Handled, "At the top, scrolling up should pass through to an outer scroller.");
                MouseWheelEventArgs down = Wheel(viewer, -120);
                Assert.True(down.Handled);
                Assert.True(await MuzzleWpfTestFixture.WaitForAsync(() => Math.Abs(viewer.VerticalOffset - 96) < 0.5), $"Offset {viewer.VerticalOffset}");
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Disabled_LeavesTheWheelAlone() {
        fixture.Run(() => {
            ScrollViewer viewer = new() { Height = 100, Content = new Border { Height = 1000 } };
            Window window = MuzzleWpfTestFixture.ShowOffScreen(viewer);
            try {
                Assert.False(Wheel(viewer, -120).Handled);
            }
            finally {
                window.Close();
            }
        });
    }

    private static MouseWheelEventArgs Wheel(UIElement target, int delta) {
        MouseWheelEventArgs args = new(Mouse.PrimaryDevice, Environment.TickCount, delta) { RoutedEvent = UIElement.PreviewMouseWheelEvent, Source = target };
        target.RaiseEvent(args);
        return args;
    }
}