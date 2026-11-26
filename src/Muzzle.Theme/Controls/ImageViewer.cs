using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Pan-and-zoom image surface. The mouse wheel zooms smoothly around the pointer, dragging pans (kept inside the
/// frame), and a double-click springs back to 1× (or, when already at 1×, zooms 2× into the clicked spot). Keyboard:
/// + / − zoom, 0 resets, arrows pan.
/// </summary>
[TemplatePart(Name = ViewportPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ImagePartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = ZoomInButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = ZoomOutButtonPartName, Type = typeof(ButtonBase))]
[TemplatePart(Name = ResetButtonPartName, Type = typeof(ButtonBase))]
public class ImageViewer : Control {
    /// <summary>Name of the clipping viewport template part that receives wheel and drag input.</summary>
    public const string ViewportPartName = "PART_Viewport";
    /// <summary>Name of the Image template part that is zoomed and panned.</summary>
    public const string ImagePartName = "PART_Image";
    /// <summary>Name of the zoom-in toolbar button template part.</summary>
    public const string ZoomInButtonPartName = "PART_ZoomInButton";
    /// <summary>Name of the zoom-out toolbar button template part.</summary>
    public const string ZoomOutButtonPartName = "PART_ZoomOutButton";
    /// <summary>Name of the reset-to-100 % toolbar button template part.</summary>
    public const string ResetButtonPartName = "PART_ResetButton";

    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(ImageViewer), new PropertyMetadata(null, OnSourceChanged));

    /// <summary>Identifies the <see cref="Zoom"/> dependency property.</summary>
    public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
        nameof(Zoom), typeof(double), typeof(ImageViewer),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnZoomPropertyChanged, CoerceZoom));

    /// <summary>Identifies the <see cref="MinZoom"/> dependency property.</summary>
    public static readonly DependencyProperty MinZoomProperty = DependencyProperty.Register(
        nameof(MinZoom), typeof(double), typeof(ImageViewer), new PropertyMetadata(0.5, OnZoomLimitChanged), static value => value is double zoom && (zoom > 0));

    /// <summary>Identifies the <see cref="MaxZoom"/> dependency property.</summary>
    public static readonly DependencyProperty MaxZoomProperty = DependencyProperty.Register(
        nameof(MaxZoom), typeof(double), typeof(ImageViewer), new PropertyMetadata(8.0, OnZoomLimitChanged), static value => value is double zoom && (zoom > 0));

    /// <summary>Multiplier applied per wheel notch and per zoom-button click (default 1.25).</summary>
    public static readonly DependencyProperty ZoomStepProperty = DependencyProperty.Register(
        nameof(ZoomStep), typeof(double), typeof(ImageViewer), new PropertyMetadata(1.25), static value => value is double step && (step > 1));

    /// <summary>Identifies the <see cref="Stretch"/> dependency property.</summary>
    public static readonly DependencyProperty StretchProperty = DependencyProperty.Register(
        nameof(Stretch), typeof(Stretch), typeof(ImageViewer), new PropertyMetadata(Stretch.Uniform));

    /// <summary>Identifies the <see cref="ShowToolbar"/> dependency property.</summary>
    public static readonly DependencyProperty ShowToolbarProperty = DependencyProperty.Register(
        nameof(ShowToolbar), typeof(bool), typeof(ImageViewer), new PropertyMetadata(true));

    private static readonly DependencyPropertyKey IsPanningPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsPanning), typeof(bool), typeof(ImageViewer), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsPanning"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsPanningProperty = IsPanningPropertyKey.DependencyProperty;

    private readonly ScaleTransform scale = new(1, 1);
    private readonly TranslateTransform shift = new();
    private FrameworkElement? viewport;
    private FrameworkElement? image;
    private ButtonBase? zoomInButton;
    private ButtonBase? zoomOutButton;
    private ButtonBase? resetButton;
    private Point panStart;
    private Vector panOrigin;
    private bool settingZoom;

    static ImageViewer() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(typeof(ImageViewer)));
        FocusableProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(true));
        ClipToBoundsProperty.OverrideMetadata(typeof(ImageViewer), new FrameworkPropertyMetadata(true));
    }

    /// <summary>Gets or sets the source. The default is <see langword="null"/>. This is a dependency property.</summary>
    public ImageSource? Source {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Current magnification (1 = fitted to the frame). Setting it zooms around the centre.</summary>
    public double Zoom {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>Gets or sets the min zoom. The default is <c>0.5</c>. This is a dependency property.</summary>
    public double MinZoom {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    /// <summary>Gets or sets the max zoom. The default is <c>8.0</c>. This is a dependency property.</summary>
    public double MaxZoom {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    /// <summary>Gets or sets the zoom step. The default is <c>1.25</c>. This is a dependency property.</summary>
    public double ZoomStep {
        get => (double)GetValue(ZoomStepProperty);
        set => SetValue(ZoomStepProperty, value);
    }

    /// <summary>Gets or sets the stretch. The default is <c>Stretch.Uniform</c>. This is a dependency property.</summary>
    public Stretch Stretch {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <summary>Show the zoom − / % / + / reset pill (default true).</summary>
    public bool ShowToolbar {
        get => (bool)GetValue(ShowToolbarProperty);
        set => SetValue(ShowToolbarProperty, value);
    }

    /// <summary>True while the image is being dragged.</summary>
    public bool IsPanning => (bool)GetValue(IsPanningProperty);

    /// <summary>The current pan offset in DIPs.</summary>
    public Vector Offset => new(shift.X, shift.Y);

    /// <summary>Zooms to <paramref name="zoom"/> keeping <paramref name="anchor"/> (viewport coordinates) still.</summary>
    public void ZoomTo(double zoom, Point anchor, bool animate = true) {
        double target = Math.Clamp(zoom, MinZoom, MaxZoom);
        double current = scale.ScaleX;
        if ((viewport is null) || (current <= 0)) {
            SetZoomValue(target);
            return;
        }
        // The anchor in the image's own (unzoomed) coordinates stays under the pointer.
        Vector layout = LayoutOffset();
        Point local = new((anchor.X - layout.X - shift.X) / current, (anchor.Y - layout.Y - shift.Y) / current);
        Vector offset = new(anchor.X - layout.X - (local.X * target), anchor.Y - layout.Y - (local.Y * target));
        offset = Constrain(offset, target);
        Apply(target, offset, animate ? MotionKind.Glide : MotionKind.None);
    }

    /// <summary>Zooms by one <see cref="ZoomStep"/> around the centre.</summary>
    public void ZoomIn() {
        ZoomTo(scale.ScaleX * ZoomStep, Centre());
    }

    /// <summary>Zooms out by one <see cref="ZoomStep"/> around the centre.</summary>
    public void ZoomOut() {
        ZoomTo(scale.ScaleX / ZoomStep, Centre());
    }

    /// <summary>Springs back to 1× and recentres.</summary>
    public void ResetView(bool animate = true) {
        Apply(1, Constrain(default, 1), animate ? MotionKind.Spring : MotionKind.None);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (viewport is not null) {
            viewport.SizeChanged -= OnViewportSizeChanged;
        }
        foreach ((ButtonBase? button, RoutedEventHandler handler) in (((ButtonBase?, RoutedEventHandler)[])[(zoomInButton, OnZoomInClick), (zoomOutButton, OnZoomOutClick), (resetButton, OnResetClick)])) {
            if (button is not null) {
                button.Click -= handler;
            }
        }
        base.OnApplyTemplate();
        viewport = GetTemplateChild(ViewportPartName) as FrameworkElement;
        image = GetTemplateChild(ImagePartName) as FrameworkElement;
        zoomInButton = GetTemplateChild(ZoomInButtonPartName) as ButtonBase;
        zoomOutButton = GetTemplateChild(ZoomOutButtonPartName) as ButtonBase;
        resetButton = GetTemplateChild(ResetButtonPartName) as ButtonBase;
        if (image is not null) {
            image.RenderTransformOrigin = new Point(0, 0);
            image.RenderTransform = new TransformGroup { Children = [scale, shift] };
        }
        if (viewport is not null) {
            viewport.SizeChanged += OnViewportSizeChanged;
        }
        if (zoomInButton is not null) {
            zoomInButton.Click += OnZoomInClick;
        }
        if (zoomOutButton is not null) {
            zoomOutButton.Click += OnZoomOutClick;
        }
        if (resetButton is not null) {
            resetButton.Click += OnResetClick;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseWheel(MouseWheelEventArgs e) {
        base.OnMouseWheel(e);
        if (e.Handled || (viewport is null) || (Source is null)) {
            return;
        }
        double notches = e.Delta / (double)Mouse.MouseWheelDeltaForOneLine;
        ZoomTo(scale.ScaleX * Math.Pow(ZoomStep, notches), e.GetPosition(viewport));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonDown(e);
        if (e.Handled || (viewport is null) || (Source is null)) {
            return;
        }
        Focus();
        if (e.ClickCount == 2) {
            if (Math.Abs(scale.ScaleX - 1) < 0.01) {
                ZoomTo(2, e.GetPosition(viewport));
            }
            else {
                ResetView();
            }
            e.Handled = true;
            return;
        }
        if (CaptureMouse()) {
            panStart = e.GetPosition(viewport);
            panOrigin = Offset;
            StopMotion();
            SetValue(IsPanningPropertyKey, true);
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnMouseMove(MouseEventArgs e) {
        base.OnMouseMove(e);
        if (!IsPanning || (viewport is null)) {
            return;
        }
        Vector delta = e.GetPosition(viewport) - panStart;
        Vector offset = Constrain(panOrigin + delta, scale.ScaleX);
        shift.X = offset.X;
        shift.Y = offset.Y;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e) {
        base.OnMouseLeftButtonUp(e);
        EndPan();
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e) {
        base.OnLostMouseCapture(e);
        EndPan();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (e.Handled || (Source is null)) {
            return;
        }
        const double PanStep = 40;
        switch (e.Key) {
            case Key.Add:
            case Key.OemPlus:
                ZoomIn();
                break;
            case Key.Subtract:
            case Key.OemMinus:
                ZoomOut();
                break;
            case Key.D0:
            case Key.NumPad0:
                ResetView();
                break;
            case Key.Left:
                PanBy(new Vector(PanStep, 0));
                break;
            case Key.Right:
                PanBy(new Vector(-PanStep, 0));
                break;
            case Key.Up:
                PanBy(new Vector(0, PanStep));
                break;
            case Key.Down:
                PanBy(new Vector(0, -PanStep));
                break;
            default:
                return;
        }
        e.Handled = true;
    }

    private static object CoerceZoom(DependencyObject d, object value) {
        ImageViewer viewer = (ImageViewer)d;
        return Math.Clamp((double)value, viewer.MinZoom, Math.Max(viewer.MinZoom, viewer.MaxZoom));
    }

    private static void OnZoomPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ImageViewer viewer = (ImageViewer)d;
        if (!viewer.settingZoom) {
            viewer.ZoomTo((double)e.NewValue, viewer.Centre());
        }
    }

    private static void OnZoomLimitChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        d.CoerceValue(ZoomProperty);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((ImageViewer)d).ResetView(false);
    }

    private void OnViewportSizeChanged(object sender, SizeChangedEventArgs e) {
        if (viewport is not null) {
            double radius = Math.Max(0, MuzzleAssist.GetCornerRadius(this).TopLeft - BorderThickness.Left);
            viewport.Clip = new RectangleGeometry(new Rect(e.NewSize), radius, radius);
        }
        Vector offset = Constrain(Offset, scale.ScaleX);
        Apply(scale.ScaleX, offset, MotionKind.None);
    }

    private void OnZoomInClick(object sender, RoutedEventArgs e) {
        ZoomIn();
    }

    private void OnZoomOutClick(object sender, RoutedEventArgs e) {
        ZoomOut();
    }

    private void OnResetClick(object sender, RoutedEventArgs e) {
        ResetView();
    }

    private void PanBy(Vector delta) {
        Apply(scale.ScaleX, Constrain(Offset + delta, scale.ScaleX), MotionKind.Glide);
    }

    private void EndPan() {
        if (!IsPanning) {
            return;
        }
        SetValue(IsPanningPropertyKey, false);
        if (IsMouseCaptured) {
            ReleaseMouseCapture();
        }
    }

    private Point Centre() {
        return (viewport is null) ? default : new Point(viewport.ActualWidth / 2, viewport.ActualHeight / 2);
    }

    /// <summary>
    /// Keeps the zoomed picture covering the frame along each axis where it is bigger than the frame, and centred
    /// along each axis where it is smaller.
    /// </summary>
    private Vector Constrain(Vector offset, double zoom) {
        if ((viewport is null) || (image is null)) {
            return default;
        }
        Vector layout = LayoutOffset();
        return new Vector(
            ConstrainAxis(offset.X, layout.X, image.RenderSize.Width, viewport.ActualWidth, zoom),
            ConstrainAxis(offset.Y, layout.Y, image.RenderSize.Height, viewport.ActualHeight, zoom));
    }

    // offset: translate; origin: the picture's layout position in the frame; size: its unzoomed size; frame: frame length.
    private static double ConstrainAxis(double offset, double origin, double size, double frame, double zoom) {
        double scaled = size * zoom;
        if (scaled <= frame) {
            return ((frame - scaled) / 2) - origin;
        }
        double left = Math.Clamp(origin + offset, frame - scaled, 0);
        return left - origin;
    }

    private Vector LayoutOffset() {
        return (image is null) ? default : VisualTreeHelper.GetOffset(image);
    }

    private void Apply(double zoom, Vector offset, MotionKind motion) {
        bool animate = (motion != MotionKind.None) && IsLoaded && Jelly.IsMotionEnabled(this);
        IEasingFunction? easing = motion switch {
            MotionKind.Spring => MuzzleEasings.Get(this, MuzzleEasings.JellySoftKey),
            MotionKind.Glide => MuzzleEasings.Get(this, MuzzleEasings.GlideKey),
            _ => null
        };
        TimeSpan duration = TimeSpan.FromMilliseconds((motion == MotionKind.Spring) ? 650 : 220);
        Animate(scale, ScaleTransform.ScaleXProperty, zoom, duration, easing, animate);
        Animate(scale, ScaleTransform.ScaleYProperty, zoom, duration, easing, animate);
        Animate(shift, TranslateTransform.XProperty, offset.X, duration, easing, animate);
        Animate(shift, TranslateTransform.YProperty, offset.Y, duration, easing, animate);
        SetZoomValue(zoom);
    }

    private static void Animate(Animatable target, DependencyProperty property, double to, TimeSpan duration, IEasingFunction? easing, bool animate) {
        double from = (double)target.GetValue(property);
        target.SetValue(property, to);
        if (!animate || (Math.Abs(from - to) < 0.0001)) {
            target.BeginAnimation(property, null);
            return;
        }
        target.BeginAnimation(property, new DoubleAnimation(from, to, new Duration(duration)) { EasingFunction = easing, FillBehavior = FillBehavior.Stop });
    }

    private void StopMotion() {
        foreach ((Animatable target, DependencyProperty property) in (((Animatable, DependencyProperty)[])[
                     (scale, ScaleTransform.ScaleXProperty), (scale, ScaleTransform.ScaleYProperty),
                     (shift, TranslateTransform.XProperty), (shift, TranslateTransform.YProperty)])) {
            double current = (double)target.GetValue(property);
            target.BeginAnimation(property, null);
            target.SetValue(property, current);
        }
    }

    private void SetZoomValue(double zoom) {
        settingZoom = true;
        try {
            SetCurrentValue(ZoomProperty, zoom);
        }
        finally {
            settingZoom = false;
        }
    }

    private enum MotionKind {
        None,
        Glide,
        Spring
    }
}