using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Xunit;

namespace Muzzle.Theme.Tests.Behaviors;

[Collection(WpfCollection.Name)]
public sealed class JellyBehaviorTests(MuzzleWpfTestFixture fixture) {
    private const double Tolerance = 0.015;

    [StaFact]
    public void Attaching_InstallsAScaleTransform() {
        fixture.Run(() => {
            Border hover = new();
            JellyBehavior.SetEnableHoverBounce(hover, true);
            ScaleTransform scale = Assert.IsType<ScaleTransform>(hover.RenderTransform);
            Assert.Equal(1, scale.ScaleX);
            Assert.Equal(new Point(0.5, 0.5), hover.RenderTransformOrigin);

            Border press = new();
            JellyBehavior.SetEnablePressSquish(press, true);
            Assert.IsType<ScaleTransform>(press.RenderTransform);
        });
    }

    [StaFact]
    public void Attaching_KeepsAnExistingTransform() {
        fixture.Run(() => {
            RotateTransform rotate = new(15);
            Border element = new() { RenderTransform = rotate };
            JellyBehavior.SetEnableHoverBounce(element, true);
            JellyBehavior.SetEnablePressSquish(element, true);
            TransformGroup group = Assert.IsType<TransformGroup>(element.RenderTransform);
            Assert.Contains(rotate, group.Children);
            Assert.Single(group.Children.OfType<ScaleTransform>());
        });
    }

    [StaFact]
    public void Attaching_ReusesAnExistingScaleOrGroup() {
        fixture.Run(() => {
            ScaleTransform existing = new(1, 1);
            Border scaled = new() { RenderTransform = existing };
            JellyBehavior.SetEnableHoverBounce(scaled, true);
            Assert.Same(existing, scaled.RenderTransform);

            TranslateTransform shift = new(4, 0);
            TransformGroup group = new() { Children = [shift] };
            Border grouped = new() { RenderTransform = group };
            JellyBehavior.SetEnablePressSquish(grouped, true);
            Assert.Same(group, grouped.RenderTransform);
            Assert.Contains(shift, group.Children);
            Assert.Single(group.Children.OfType<ScaleTransform>());
        });
    }

    [StaFact]
    public Task HoverAndPress_AnimateTheScale() {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            Border element = new() { Width = 80, Height = 40, Background = Brushes.Pink };
            JellyBehavior.SetEnableHoverBounce(element, true);
            JellyBehavior.SetEnablePressSquish(element, true);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(element);
            try {
                ScaleTransform scale = Assert.IsType<ScaleTransform>(element.RenderTransform);
                bool motion = Jelly.IsMotionEnabled(element);

                element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                await MuzzleWpfTestFixture.WaitAsync(500);
                Assert.Equal(motion ? 1.03 : 1.0, scale.ScaleX, Tolerance);

                element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
                await MuzzleWpfTestFixture.WaitAsync(220);
                Assert.Equal(motion ? 0.94 : 1.0, scale.ScaleX, Tolerance);
                Assert.Equal(motion ? 0.92 : 1.0, scale.ScaleY, Tolerance);

                element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent });
                await MuzzleWpfTestFixture.WaitAsync(800);
                Assert.Equal(1.0, scale.ScaleX, Tolerance);

                element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseLeaveEvent });
                await MuzzleWpfTestFixture.WaitAsync(650);
                Assert.Equal(1.0, scale.ScaleX, Tolerance);
                Assert.Equal(1.0, scale.ScaleY, Tolerance);
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public Task CustomScales_AreHonoured() {
        return fixture.RunAsync(async () => {
            Border element = new() { Width = 80, Height = 40 };
            JellyBehavior.SetEnableHoverBounce(element, true);
            JellyBehavior.SetHoverScale(element, 1.2);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(element);
            try {
                ScaleTransform scale = (ScaleTransform)element.RenderTransform;
                element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                await MuzzleWpfTestFixture.WaitAsync(550);
                Assert.Equal(Jelly.IsMotionEnabled(element) ? 1.2 : 1.0, scale.ScaleX, Tolerance);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public Task JellyIntensityZero_KeepsTheElementStill() {
        return fixture.RunAsync(async () => {
            Border element = new() { Width = 80, Height = 40 };
            MuzzleAssist.SetJellyIntensity(element, 0);
            JellyBehavior.SetEnableHoverBounce(element, true);
            JellyBehavior.SetEnablePressSquish(element, true);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(element);
            try {
                ScaleTransform scale = (ScaleTransform)element.RenderTransform;
                element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
                element.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent });
                await MuzzleWpfTestFixture.WaitAsync(300);
                Assert.Equal(1.0, scale.ScaleX, Tolerance);
                Assert.Equal(1.0, scale.ScaleY, Tolerance);
            }
            finally {
                window.Close();
            }
        });
    }

    [StaFact]
    public void Disabling_DetachesTheHandlers() {
        fixture.Run(() => {
            Border element = new();
            JellyBehavior.SetEnableHoverBounce(element, true);
            JellyBehavior.SetEnableHoverBounce(element, false);
            ScaleTransform scale = (ScaleTransform)element.RenderTransform;
            element.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
            Assert.False(scale.HasAnimatedProperties);
        });
    }

    [StaTheory]
    [InlineData(JellyEntrance.PopIn)]
    [InlineData(JellyEntrance.SlideUp)]
    [InlineData(JellyEntrance.FadeIn)]
    public Task Entrance_PlaysOnLoadedAndSettles(JellyEntrance entrance) {
        return fixture.RunAsync(async () => {
            fixture.ResetDiagnostics();
            Border element = new() { Width = 60, Height = 60, Background = Brushes.Pink };
            JellyBehavior.SetEntranceAnimation(element, entrance);
            Window window = MuzzleWpfTestFixture.ShowOffScreen(element);
            try {
                bool motion = Jelly.IsMotionEnabled(element);
                if (motion) {
                    Assert.True(element.HasAnimatedProperties, "The entrance should be animating right after Loaded.");
                }
                if (entrance == JellyEntrance.SlideUp) {
                    TransformGroup group = Assert.IsType<TransformGroup>(element.RenderTransform);
                    Assert.Single(group.Children.OfType<TranslateTransform>());
                }
                await MuzzleWpfTestFixture.WaitAsync(900);
                // Settled: fully opaque, full size, back in place (animations use FillBehavior.Stop).
                Assert.Equal(1.0, element.Opacity, Tolerance);
                ScaleTransform? scale = (element.RenderTransform as ScaleTransform)
                    ?? (element.RenderTransform as TransformGroup)?.Children.OfType<ScaleTransform>().FirstOrDefault();
                if (scale is not null) {
                    Assert.Equal(1.0, scale.ScaleX, Tolerance);
                    Assert.Equal(1.0, scale.ScaleY, Tolerance);
                }
                TranslateTransform? shift = (element.RenderTransform as TransformGroup)?.Children.OfType<TranslateTransform>().FirstOrDefault();
                if (shift is not null) {
                    Assert.Equal(0.0, shift.Y, Tolerance);
                }
            }
            finally {
                window.Close();
            }
            fixture.AssertClean();
        });
    }

    [StaFact]
    public void PlayEntrance_WithNone_DoesNothing() {
        fixture.Run(() => {
            Border element = new();
            JellyBehavior.PlayEntrance(element);
            Assert.False(element.HasAnimatedProperties);
            Assert.Equal(1.0, element.Opacity);
        });
    }

    [StaFact]
    public void Defaults_AreTheDocumentedValues() {
        fixture.Run(() => {
            Border element = new();
            Assert.False(JellyBehavior.GetEnableHoverBounce(element));
            Assert.False(JellyBehavior.GetEnablePressSquish(element));
            Assert.Equal(1.03, JellyBehavior.GetHoverScale(element));
            Assert.Equal(0.94, JellyBehavior.GetPressSquishScale(element));
            Assert.Equal(JellyEntrance.None, JellyBehavior.GetEntranceAnimation(element));
            Assert.Equal(TimeSpan.Zero, JellyBehavior.GetEntranceDelay(element));
        });
    }
}