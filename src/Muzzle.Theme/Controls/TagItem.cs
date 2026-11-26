using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>A rounded pastel chip with a remove (×) button, used by <see cref="TagInput"/>.</summary>
[TemplatePart(Name = RemoveButtonPartName, Type = typeof(ButtonBase))]
public class TagItem : ContentControl {
    /// <summary>Identifies the <see cref="RemoveRequested"/> routed event.</summary>
    public static readonly RoutedEvent RemoveRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(RemoveRequested), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TagItem));

    /// <summary>True while the chip is armed for removal by Backspace (drawn highlighted).</summary>
    public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
        nameof(IsHighlighted), typeof(bool), typeof(TagItem), new PropertyMetadata(false));

    private const string RemoveButtonPartName = "PART_RemoveButton";

    private readonly ScaleTransform scale = new(1, 1);
    private ButtonBase? removeButton;

    static TagItem() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(TagItem), new FrameworkPropertyMetadata(typeof(TagItem)));
        FocusableProperty.OverrideMetadata(typeof(TagItem), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="TagItem"/> class.</summary>
    public TagItem() {
        RenderTransform = scale;
        RenderTransformOrigin = new Point(0.5, 0.5);
    }

    /// <summary>Occurs when the chip's × button is clicked.</summary>
    public event RoutedEventHandler RemoveRequested {
        add => AddHandler(RemoveRequestedEvent, value);
        remove => RemoveHandler(RemoveRequestedEvent, value);
    }

    /// <summary>Gets or sets whether the control is highlighted. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool IsHighlighted {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    /// <summary>Gets whether the chip is playing its exit animation.</summary>
    public bool IsExiting { get; private set; }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (removeButton is not null) {
            removeButton.Click -= OnRemoveClick;
        }
        base.OnApplyTemplate();
        removeButton = GetTemplateChild(RemoveButtonPartName) as ButtonBase;
        if (removeButton is not null) {
            removeButton.Click += OnRemoveClick;
        }
    }

    /// <summary>Pops the chip in from 0.6× with an elastic overshoot.</summary>
    public void PlayEnter() {
        if (!Jelly.IsMotionEnabled(this)) {
            return;
        }
        DoubleAnimation pop = new(0.6, 1, TimeSpan.FromMilliseconds(620)) { EasingFunction = MuzzleEasings.Get(this, MuzzleEasings.JellyKey) };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
        BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
    }

    /// <summary>Squishes the chip out, then calls <paramref name="completed"/>.</summary>
    public void PlayExit(Action completed) {
        IsExiting = true;
        IsHitTestVisible = false;
        if (!Jelly.IsMotionEnabled(this) || !IsLoaded) {
            completed();
            return;
        }
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.BackInKey);
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.6, TimeSpan.FromMilliseconds(180)) { EasingFunction = squish });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.5, TimeSpan.FromMilliseconds(180)) { EasingFunction = squish });
        DoubleAnimation fade = new(0, TimeSpan.FromMilliseconds(180));
        fade.Completed += (_, _) => completed();
        BeginAnimation(OpacityProperty, fade);
    }

    private void OnRemoveClick(object sender, RoutedEventArgs e) {
        e.Handled = true;
        RaiseEvent(new RoutedEventArgs(RemoveRequestedEvent, this));
    }
}