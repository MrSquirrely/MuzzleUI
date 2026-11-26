using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>How <see cref="TransitioningContentControl"/> swaps content.</summary>
public enum ContentTransition {
    /// <summary>Swap instantly, without animation.</summary>
    None,
    /// <summary>Fade the old content out while the new content fades in.</summary>
    CrossFade,
    /// <summary>The new content slides in from the right, moving left.</summary>
    SlideLeft,
    /// <summary>The new content slides in from the left, moving right.</summary>
    SlideRight,
    /// <summary>The new content rises in from below (the gallery's page transition).</summary>
    SlideUp,
    /// <summary>The new content drops in from above.</summary>
    SlideDown,
    /// <summary>The new content pops in from slightly smaller with an elastic overshoot.</summary>
    ScalePop
}

/// <summary>
/// A ContentControl that animates every content change: the old content fades out while the new content fades in
/// with a springy slide or scale overshoot. Works with UIElements and with data + DataTemplates.
/// </summary>
[TemplatePart(Name = CurrentPartName, Type = typeof(ContentPresenter))]
[TemplatePart(Name = PreviousPartName, Type = typeof(ContentPresenter))]
public class TransitioningContentControl : ContentControl {
    private const string CurrentPartName = "PART_Current";
    private const string PreviousPartName = "PART_Previous";

    /// <summary>Identifies the <see cref="Transition"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionProperty = DependencyProperty.Register(
        nameof(Transition), typeof(ContentTransition), typeof(TransitioningContentControl), new PropertyMetadata(ContentTransition.SlideUp));

    /// <summary>Distance in DIPs that sliding transitions travel.</summary>
    public static readonly DependencyProperty SlideDistanceProperty = DependencyProperty.Register(
        nameof(SlideDistance), typeof(double), typeof(TransitioningContentControl), new PropertyMetadata(36.0));

    /// <summary>Identifies the <see cref="TransitionDuration"/> dependency property.</summary>
    public static readonly DependencyProperty TransitionDurationProperty = DependencyProperty.Register(
        nameof(TransitionDuration), typeof(Duration), typeof(TransitioningContentControl), new PropertyMetadata(new Duration(TimeSpan.FromMilliseconds(560))));

    private ContentPresenter? current;
    private ContentPresenter? previous;
    private int generation;

    static TransitioningContentControl() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TransitioningContentControl), new FrameworkPropertyMetadata(typeof(TransitioningContentControl)));
        FocusableProperty.OverrideMetadata(typeof(TransitioningContentControl), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Raised when the incoming content has finished animating in.</summary>
    public event EventHandler? TransitionCompleted;

    /// <summary>Gets or sets the transition. The default is <c>ContentTransition.SlideUp</c>. This is a dependency property.</summary>
    public ContentTransition Transition {
        get => (ContentTransition)GetValue(TransitionProperty);
        set => SetValue(TransitionProperty, value);
    }

    /// <summary>Gets or sets the slide distance. The default is <c>36.0</c>. This is a dependency property.</summary>
    public double SlideDistance {
        get => (double)GetValue(SlideDistanceProperty);
        set => SetValue(SlideDistanceProperty, value);
    }

    /// <summary>Gets or sets the transition duration. The default is <c>Duration(TimeSpan.FromMilliseconds(560))</c>. This is a dependency property.</summary>
    public Duration TransitionDuration {
        get => (Duration)GetValue(TransitionDurationProperty);
        set => SetValue(TransitionDurationProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        current = GetTemplateChild(CurrentPartName) as ContentPresenter;
        previous = GetTemplateChild(PreviousPartName) as ContentPresenter;
        if (current is not null) {
            current.Content = Content;
        }
        if (previous is not null) {
            previous.Content = null;
            previous.IsHitTestVisible = false;
        }
    }

    /// <inheritdoc />
    protected override void OnContentChanged(object oldContent, object newContent) {
        base.OnContentChanged(oldContent, newContent);
        if (current is null) {
            return;
        }
        bool animate = IsLoaded && (previous is not null) && (Transition != ContentTransition.None) && Jelly.IsMotionEnabled(this);
        // Release the old content from the current presenter before handing it to the outgoing one.
        current.Content = null;
        if (!animate || (previous is null)) {
            if (previous is not null) {
                previous.Content = null;
            }
            current.Content = newContent;
            ResetTransforms(current);
            return;
        }
        previous.Content = oldContent;
        current.Content = newContent;
        Play(++generation);
    }

    private void Play(int run) {
        if ((current is null) || (previous is null)) {
            return;
        }
        Duration duration = TransitionDuration;
        Duration fadeIn = TimeSpan.FromMilliseconds(Math.Min(240, duration.HasTimeSpan ? duration.TimeSpan.TotalMilliseconds : 240));
        Duration fadeOut = TimeSpan.FromMilliseconds(180);
        IEasingFunction spring = MuzzleEasings.Get(this, MuzzleEasings.BackKey);
        IEasingFunction jelly = MuzzleEasings.Get(this, MuzzleEasings.JellySoftKey);
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.SquishKey);
        (ScaleTransform inScale, TranslateTransform inShift) = CreateTransforms(current);
        (ScaleTransform outScale, TranslateTransform outShift) = CreateTransforms(previous);
        double distance = SlideDistance;

        DoubleAnimation fadeInAnimation = new(0, 1, fadeIn);
        DoubleAnimation fadeOutAnimation = new(1, 0, fadeOut);
        fadeOutAnimation.Completed += (_, _) => {
            if ((run == generation) && (previous is not null)) {
                previous.Content = null;
                ResetTransforms(previous);
            }
        };

        switch (Transition) {
            case ContentTransition.SlideLeft:
                Slide(inShift, outShift, TranslateTransform.XProperty, distance, duration, fadeOut, spring, squish);
                break;
            case ContentTransition.SlideRight:
                Slide(inShift, outShift, TranslateTransform.XProperty, -distance, duration, fadeOut, spring, squish);
                break;
            case ContentTransition.SlideUp:
                Slide(inShift, outShift, TranslateTransform.YProperty, distance, duration, fadeOut, spring, squish);
                break;
            case ContentTransition.SlideDown:
                Slide(inShift, outShift, TranslateTransform.YProperty, -distance, duration, fadeOut, spring, squish);
                break;
            case ContentTransition.ScalePop:
                DoubleAnimation grow = new(0.92, 1, duration) { EasingFunction = jelly };
                inScale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
                inScale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
                DoubleAnimation swell = new(1, 1.04, fadeOut) { EasingFunction = squish };
                outScale.BeginAnimation(ScaleTransform.ScaleXProperty, swell);
                outScale.BeginAnimation(ScaleTransform.ScaleYProperty, swell);
                break;
        }

        fadeInAnimation.Completed += (_, _) => {
            if (run == generation) {
                TransitionCompleted?.Invoke(this, EventArgs.Empty);
            }
        };
        current.BeginAnimation(OpacityProperty, fadeInAnimation);
        previous.BeginAnimation(OpacityProperty, fadeOutAnimation);
    }

    private static void Slide(TranslateTransform incoming, TranslateTransform outgoing, DependencyProperty axis, double distance,
                              Duration duration, Duration outDuration, IEasingFunction spring, IEasingFunction squish) {
        incoming.BeginAnimation(axis, new DoubleAnimation(distance, 0, duration) { EasingFunction = spring });
        outgoing.BeginAnimation(axis, new DoubleAnimation(0, -distance * 0.6, outDuration) { EasingFunction = squish });
    }

    private static (ScaleTransform Scale, TranslateTransform Shift) CreateTransforms(UIElement element) {
        ScaleTransform scale = new(1, 1);
        TranslateTransform shift = new();
        TransformGroup group = new();
        group.Children.Add(scale);
        group.Children.Add(shift);
        element.RenderTransform = group;
        element.RenderTransformOrigin = new Point(0.5, 0.5);
        return (scale, shift);
    }

    private static void ResetTransforms(UIElement element) {
        element.BeginAnimation(OpacityProperty, null);
        element.Opacity = 1;
        element.RenderTransform = Transform.Identity;
    }
}