using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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
}