using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Muzzle.Theme.Controls;

/// <summary>Placeholder shape drawn by <see cref="Skeleton"/> while loading.</summary>
public enum SkeletonShape {
    /// <summary>One rounded block filling the content's size (the default).</summary>
    Rectangle,
    /// <summary>A circle, e.g. for avatars.</summary>
    Circle,
    /// <summary><see cref="Skeleton.LineCount"/> text-like lines, the last one shorter.</summary>
    TextLines
}

/// <summary>
/// A loading placeholder: soft #EFECE8 blocks with a diagonal shimmer sweep while <see cref="IsLoading"/> is true,
/// then a smooth cross-fade into its real <c>Content</c>. While loading, the content is laid out invisibly, so the
/// placeholder takes the content's size; without content it uses its own Width / Height.
/// </summary>
[TemplatePart(Name = BlocksPartName, Type = typeof(Panel))]
public class Skeleton : ContentControl {
    /// <summary>Identifies the <see cref="IsLoading"/> dependency property.</summary>
    public static readonly DependencyProperty IsLoadingProperty = DependencyProperty.Register(
        nameof(IsLoading), typeof(bool), typeof(Skeleton), new PropertyMetadata(true));

    /// <summary>Identifies the <see cref="SkeletonShape"/> dependency property.</summary>
    public static readonly DependencyProperty SkeletonShapeProperty = DependencyProperty.Register(
        nameof(SkeletonShape), typeof(SkeletonShape), typeof(Skeleton), new PropertyMetadata(SkeletonShape.Rectangle, OnShapeChanged));

    /// <summary>Identifies the <see cref="CornerRadius"/> dependency property.</summary>
    public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
        nameof(CornerRadius), typeof(CornerRadius), typeof(Skeleton), new PropertyMetadata(new CornerRadius(12), OnShapeChanged));

    /// <summary>Number of lines for <see cref="SkeletonShape.TextLines"/>; the last one is shorter.</summary>
    public static readonly DependencyProperty LineCountProperty = DependencyProperty.Register(
        nameof(LineCount), typeof(int), typeof(Skeleton), new PropertyMetadata(3, OnShapeChanged), static v => (int)v is >= 1 and <= 50);

    /// <summary>Identifies the <see cref="LineHeight"/> dependency property.</summary>
    public static readonly DependencyProperty LineHeightProperty = DependencyProperty.Register(
        nameof(LineHeight), typeof(double), typeof(Skeleton), new PropertyMetadata(12.0, OnShapeChanged));

    private const string BlocksPartName = "PART_Blocks";

    private Panel? blocks;

    static Skeleton() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Skeleton), new FrameworkPropertyMetadata(typeof(Skeleton)));
        FocusableProperty.OverrideMetadata(typeof(Skeleton), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Gets or sets whether the control is loading. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsLoading {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>Gets or sets the skeleton shape. The default is <c>SkeletonShape.Rectangle</c>. This is a dependency property.</summary>
    public SkeletonShape SkeletonShape {
        get => (SkeletonShape)GetValue(SkeletonShapeProperty);
        set => SetValue(SkeletonShapeProperty, value);
    }

    /// <summary>Gets or sets the corner radius. The default is <c>CornerRadius(12)</c>. This is a dependency property.</summary>
    public CornerRadius CornerRadius {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        set => SetValue(CornerRadiusProperty, value);
    }

    /// <summary>Gets or sets the line count. The default is <c>3</c>. This is a dependency property.</summary>
    public int LineCount {
        get => (int)GetValue(LineCountProperty);
        set => SetValue(LineCountProperty, value);
    }

    /// <summary>Gets or sets the line height. The default is <c>12.0</c>. This is a dependency property.</summary>
    public double LineHeight {
        get => (double)GetValue(LineHeightProperty);
        set => SetValue(LineHeightProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        blocks = GetTemplateChild(BlocksPartName) as Panel;
        // The shimmer is masked to the blocks. Wiring the VisualBrush here (rather than an ElementName binding inside a
        // template Freezable) avoids a "cannot find governing element" binding error on every load.
        if ((GetTemplateChild("Shimmer") is UIElement shimmer) && (blocks is not null)) {
            shimmer.OpacityMask = new VisualBrush(blocks) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left, AlignmentY = AlignmentY.Top };
        }
        BuildBlocks();
    }

    private static void OnShapeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Skeleton)d).BuildBlocks();
    }

    private void BuildBlocks() {
        if (blocks is null) {
            return;
        }
        blocks.Children.Clear();
        switch (SkeletonShape) {
            case SkeletonShape.Circle:
                Ellipse circle = new() { Stretch = Stretch.Uniform };
                circle.SetResourceReference(Shape.FillProperty, "Muzzle.Brush.SurfaceBorder");
                blocks.Children.Add(circle);
                break;
            case SkeletonShape.TextLines:
                StackPanel lines = new() { VerticalAlignment = VerticalAlignment.Top };
                for (int i = 0; i < LineCount; i++) {
                    bool last = (i == LineCount - 1) && (LineCount > 1);
                    Border line = new() {
                        Height = LineHeight,
                        CornerRadius = new CornerRadius(LineHeight / 2),
                        Margin = new Thickness(0, i == 0 ? 0 : LineHeight * 0.75, 0, 0),
                        HorizontalAlignment = last ? HorizontalAlignment.Left : HorizontalAlignment.Stretch
                    };
                    if (last) {
                        // The last line is 60% wide, like the end of a paragraph.
                        line.SetBinding(WidthProperty, new Binding(nameof(ActualWidth)) {
                            Source = lines,
                            Converter = ScaleConverter.Instance,
                            ConverterParameter = 0.6
                        });
                    }
                    line.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.SurfaceBorder");
                    lines.Children.Add(line);
                }
                blocks.Children.Add(lines);
                break;
            default:
                Border block = new() { CornerRadius = CornerRadius };
                block.SetResourceReference(Border.BackgroundProperty, "Muzzle.Brush.SurfaceBorder");
                blocks.Children.Add(block);
                break;
        }
    }

    /// <summary>value × parameter.</summary>
    private sealed class ScaleConverter : IValueConverter {
        public static ScaleConverter Instance { get; } = new();

        /// <inheritdoc />
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
            return (value is double number) && (parameter is double factor) ? number * factor : DependencyProperty.UnsetValue;
        }

        /// <inheritdoc />
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }
}