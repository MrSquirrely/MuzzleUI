using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

using Muzzle.Theme.Controls;

using Xunit;

namespace Muzzle.Theme.Tests.Controls;

[Collection(WpfCollection.Name)]
public sealed class MuzzleWindowTests(MuzzleWpfTestFixture fixture) {
    private const double ContentWidth = 480;
    private const double ContentHeight = 160;
    private const double ManualSize = 400;

    /// <summary>
    /// WindowChrome plus SizeToContent is known to size the window for the stock frame it then removes. The window
    /// must come out exactly as large as its content, title bar and border.
    /// </summary>
    [StaTheory]
    [InlineData(SizeToContent.Height, ResizeMode.NoResize)]
    [InlineData(SizeToContent.Height, ResizeMode.CanResize)]
    [InlineData(SizeToContent.Width, ResizeMode.NoResize)]
    [InlineData(SizeToContent.Width, ResizeMode.CanResize)]
    [InlineData(SizeToContent.WidthAndHeight, ResizeMode.NoResize)]
    [InlineData(SizeToContent.WidthAndHeight, ResizeMode.CanResize)]
    [InlineData(SizeToContent.WidthAndHeight, ResizeMode.CanMinimize)]
    public Task SizeToContent_FitsTheContentExactly(SizeToContent sizeToContent, ResizeMode resizeMode) {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            Border content = new() { Width = ContentWidth, Height = ContentHeight };
            MuzzleWindow window = new() {
                SizeToContent = sizeToContent,
                ResizeMode = resizeMode,
                WindowStartupLocation = WindowStartupLocation.Manual,
                Left = -20000,
                Top = -20000,
                ShowActivated = false,
                ShowInTaskbar = false,
                Content = content
            };
            if (sizeToContent == SizeToContent.Height) {
                window.Width = ManualSize + ContentWidth;
            }
            else if (sizeToContent == SizeToContent.Width) {
                window.Height = ManualSize + ContentHeight;
            }
            window.Show();
            try {
                await MuzzleWpfTestFixture.WaitAsync(150);
                double border = window.BorderThickness.Left + window.BorderThickness.Right;
                double expectedWidth = ContentWidth + border + ((sizeToContent == SizeToContent.Height) ? ManualSize - border : 0);
                double expectedHeight = ContentHeight + window.NonClientAreaHeight + border
                    + ((sizeToContent == SizeToContent.Width) ? ManualSize - window.NonClientAreaHeight - border : 0);
                Assert.True(Math.Abs(window.ActualWidth - expectedWidth) <= 1, $"Width {window.ActualWidth}, expected {expectedWidth}.");
                Assert.True(Math.Abs(window.ActualHeight - expectedHeight) <= 1, $"Height {window.ActualHeight}, expected {expectedHeight}.");
                // Nothing clipped: the content got the size it asked for.
                Assert.Equal(ContentWidth, content.ActualWidth, 0.5);
                Assert.Equal(ContentHeight, content.ActualHeight, 0.5);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task NonClientAreaForeground_ColoursTheTitleAndCaptionGlyphs() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            MuzzleWindow window = new() { Title = "Test" };
            MuzzleWpfTestFixture.ShowOffScreen(new TextBlock { Text = "Body" }, 600, 300, window);
            try {
                await MuzzleWpfTestFixture.WaitAsync(50);
                TextBlock title = Assert.IsType<TextBlock>(window.Template.FindName("TitleText", window));
                Button close = Assert.IsType<Button>(window.Template.FindName("PART_CloseButton", window));
                Button minimize = Assert.IsType<Button>(window.Template.FindName("PART_MinimizeButton", window));

                // Default: the theme tokens, exactly as before the property existed.
                Assert.Null(window.NonClientAreaForeground);
                Assert.Same(window.FindResource("Muzzle.Brush.TextSecondary"), minimize.Foreground);
                Assert.Same(window.FindResource("Muzzle.Brush.TextSecondary"), close.Foreground);

                window.NonClientAreaForeground = Brushes.White;
                await MuzzleWpfTestFixture.WaitAsync(50);
                Assert.Same(Brushes.White, title.Foreground);
                Assert.Same(Brushes.White, minimize.Foreground);
                Assert.Same(Brushes.White, close.Foreground);

                window.ClearValue(MuzzleWindow.NonClientAreaForegroundProperty);
                await MuzzleWpfTestFixture.WaitAsync(50);
                Assert.Same(window.FindResource("Muzzle.Brush.TextSecondary"), minimize.Foreground);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task IsFullScreen_CoversTheMonitorAndRestoresTheBounds() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            MuzzleWindow window = new() { Title = "Test" };
            MuzzleWpfTestFixture.ShowOffScreen(new TextBlock { Text = "Body" }, 600, 300, window);
            try {
                await MuzzleWpfTestFixture.WaitAsync(50);
                IntPtr handle = new WindowInteropHelper(window).Handle;
                FrameworkElement titleBar = Assert.IsAssignableFrom<FrameworkElement>(window.Template.FindName("PART_TitleBar", window));
                FrameworkElement root = Assert.IsAssignableFrom<FrameworkElement>(window.Template.FindName("PART_Root", window));
                Assert.True(GetWindowRect(handle, out NativeRect before));

                window.IsFullScreen = true;
                await MuzzleWpfTestFixture.WaitAsync(150);
                MonitorInfo info = new() { Size = Marshal.SizeOf<MonitorInfo>() };
                Assert.True(GetMonitorInfo(MonitorFromWindow(handle, 2), ref info));
                Assert.True(GetWindowRect(handle, out NativeRect full));
                Assert.Equal(info.Monitor, full);
                Assert.Equal(Visibility.Collapsed, titleBar.Visibility);
                Assert.Equal(new Thickness(0), root.Margin);
                Assert.Equal(WindowState.Normal, window.WindowState);

                window.IsFullScreen = false;
                await MuzzleWpfTestFixture.WaitAsync(150);
                Assert.True(GetWindowRect(handle, out NativeRect after));
                Assert.Equal(before, after);
                Assert.Equal(Visibility.Visible, titleBar.Visibility);
                Assert.Equal(window.NonClientAreaHeight, System.Windows.Shell.WindowChrome.GetWindowChrome(window).CaptionHeight);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task Backdrop_TintBackgroundFalse_LeavesTheWindowBackgroundAlone() {
        return fixture.RunAsync(async () => {
            if (!BackdropHelper.IsSupported) {
                return; // needs Windows 11 22H2
            }
            fixture.ResetDiagnostics();
            MuzzleWindow window = new() { Title = "Test", Background = Brushes.Transparent };
            BackdropHelper.SetTintBackground(window, false);
            BackdropHelper.SetBackdropType(window, BackdropType.Mica);
            MuzzleWpfTestFixture.ShowOffScreen(new TextBlock { Text = "Body" }, 600, 300, window);
            try {
                await MuzzleWpfTestFixture.WaitAsync(50);
                Assert.True(BackdropHelper.GetIsBackdropActive(window));
                Assert.Same(Brushes.Transparent, window.Background);

                // Opting back in tints the window; opting out again returns to the style's canvas.
                BackdropHelper.SetTintBackground(window, true);
                Assert.Same(window.FindResource("Muzzle.Brush.BackdropCanvas"), window.Background);
                BackdropHelper.SetTintBackground(window, false);
                Assert.Same(window.FindResource("Muzzle.Brush.Canvas"), window.Background);

                BackdropHelper.SetBackdropType(window, BackdropType.None);
                Assert.False(BackdropHelper.GetIsBackdropActive(window));
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [StructLayout(LayoutKind.Sequential)]
    private record struct NativeRect(int Left, int Top, int Right, int Bottom);

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo {
        public int Size;
        public NativeRect Monitor;
        public NativeRect Work;
        public uint Flags;
    }
}