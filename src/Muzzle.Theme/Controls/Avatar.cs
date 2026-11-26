using System;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace Muzzle.Theme.Controls;

/// <summary>Outline of an <see cref="Avatar"/>.</summary>
public enum AvatarShape {
    /// <summary>A full circle (the default).</summary>
    Circle,

    /// <summary>A rounded square (corner radius 30 % of the size).</summary>
    Squircle
}

/// <summary>Presence shown by an <see cref="Avatar"/>'s status dot.</summary>
public enum AvatarStatus {
    /// <summary>No status dot.</summary>
    None,
    /// <summary>Available: a mint dot.</summary>
    Online,
    /// <summary>Away: a butter-yellow dot.</summary>
    Away,
    /// <summary>Busy / do not disturb: a coral dot with a bar.</summary>
    Busy,
    /// <summary>Offline: a grey dot.</summary>
    Offline
}

/// <summary>
/// A profile picture. Shows <see cref="Source"/> clipped to a circle or squircle; when there is no image (or it fails to
/// load) it shows two-letter initials from <see cref="DisplayName"/> on a soft pastel chosen deterministically from the
/// name. An optional <see cref="Status"/> dot pops in with a springy bounce.
/// </summary>
[TemplatePart(Name = StatusDotPartName, Type = typeof(FrameworkElement))]
public class Avatar : Control {
    /// <summary>Name of the template part that draws the status dot (it pops in when the status changes).</summary>
    public const string StatusDotPartName = "PART_StatusDot";

    /// <summary>Number of pastel tints <see cref="ColorIndex"/> chooses from.</summary>
    public const int PaletteSize = 6;

    /// <summary>Identifies the <see cref="Source"/> dependency property.</summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(Avatar), new PropertyMetadata(null, OnSourceChanged));

    /// <summary>Identifies the <see cref="DisplayName"/> dependency property.</summary>
    public static readonly DependencyProperty DisplayNameProperty = DependencyProperty.Register(
        nameof(DisplayName), typeof(string), typeof(Avatar), new PropertyMetadata(string.Empty, OnDisplayNameChanged));

    /// <summary>Identifies the <see cref="Size"/> dependency property.</summary>
    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size), typeof(double), typeof(Avatar),
        new FrameworkPropertyMetadata(40.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnGeometryChanged), static value => value is double size && (size > 0) && !double.IsInfinity(size));

    /// <summary>Identifies the <see cref="Shape"/> dependency property.</summary>
    public static readonly DependencyProperty ShapeProperty = DependencyProperty.Register(
        nameof(Shape), typeof(AvatarShape), typeof(Avatar), new PropertyMetadata(AvatarShape.Circle, OnGeometryChanged));

    /// <summary>Identifies the <see cref="Status"/> dependency property.</summary>
    public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
        nameof(Status), typeof(AvatarStatus), typeof(Avatar), new PropertyMetadata(AvatarStatus.None, OnStatusChanged));

    private static readonly DependencyPropertyKey InitialsPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(Initials), typeof(string), typeof(Avatar), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Initials"/> read-only dependency property.</summary>
    public static readonly DependencyProperty InitialsProperty = InitialsPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ColorIndexPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ColorIndex), typeof(int), typeof(Avatar), new PropertyMetadata(0));

    /// <summary>Identifies the <see cref="ColorIndex"/> read-only dependency property.</summary>
    public static readonly DependencyProperty ColorIndexProperty = ColorIndexPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ImageFillPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ImageFill), typeof(Brush), typeof(Avatar), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="ImageFill"/> read-only dependency property.</summary>
    public static readonly DependencyProperty ImageFillProperty = ImageFillPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsImageVisiblePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsImageVisible), typeof(bool), typeof(Avatar), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsImageVisible"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsImageVisibleProperty = IsImageVisiblePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey ShapeCornerRadiusPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ShapeCornerRadius), typeof(CornerRadius), typeof(Avatar), new PropertyMetadata(new CornerRadius(999)));

    /// <summary>Identifies the <see cref="ShapeCornerRadius"/> read-only dependency property.</summary>
    public static readonly DependencyProperty ShapeCornerRadiusProperty = ShapeCornerRadiusPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey InitialsFontSizePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(InitialsFontSize), typeof(double), typeof(Avatar), new PropertyMetadata(15.6));

    /// <summary>Identifies the <see cref="InitialsFontSize"/> read-only dependency property.</summary>
    public static readonly DependencyProperty InitialsFontSizeProperty = InitialsFontSizePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StatusDotSizePropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StatusDotSize), typeof(double), typeof(Avatar), new PropertyMetadata(11.2));

    /// <summary>Identifies the <see cref="StatusDotSize"/> read-only dependency property.</summary>
    public static readonly DependencyProperty StatusDotSizeProperty = StatusDotSizePropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey StatusDotMarginPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(StatusDotMargin), typeof(Thickness), typeof(Avatar), new PropertyMetadata(default(Thickness)));

    /// <summary>Identifies the <see cref="StatusDotMargin"/> read-only dependency property.</summary>
    public static readonly DependencyProperty StatusDotMarginProperty = StatusDotMarginPropertyKey.DependencyProperty;

    private FrameworkElement? statusDot;
    private BitmapSource? watchedBitmap;

    static Avatar() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Avatar), new FrameworkPropertyMetadata(typeof(Avatar)));
    }

    /// <summary>Initializes a new instance of the <see cref="Avatar"/> class.</summary>
    public Avatar() {
        UpdateGeometry();
        Loaded += OnLoaded;
    }

    /// <summary>The picture. When null, or when it fails to load, initials are shown instead.</summary>
    public ImageSource? Source {
        get => (ImageSource?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>Gets or sets the display name. The default is an empty string. This is a dependency property.</summary>
    public string DisplayName {
        get => (string)GetValue(DisplayNameProperty);
        set => SetValue(DisplayNameProperty, value);
    }

    /// <summary>Width and height in DIPs (default 40).</summary>
    public double Size {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    /// <summary>Gets or sets the shape. The default is <c>AvatarShape.Circle</c>. This is a dependency property.</summary>
    public AvatarShape Shape {
        get => (AvatarShape)GetValue(ShapeProperty);
        set => SetValue(ShapeProperty, value);
    }

    /// <summary>Gets or sets the status. The default is <c>AvatarStatus.None</c>. This is a dependency property.</summary>
    public AvatarStatus Status {
        get => (AvatarStatus)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    /// <summary>Up to two letters taken from <see cref="DisplayName"/>.</summary>
    public string Initials => (string)GetValue(InitialsProperty);

    /// <summary>0 – <see cref="PaletteSize"/>-1: which pastel the initials sit on. Stable for a given name.</summary>
    public int ColorIndex => (int)GetValue(ColorIndexProperty);

    /// <summary>The picture as a brush (UniformToFill), or null.</summary>
    public Brush? ImageFill => (Brush?)GetValue(ImageFillProperty);

    /// <summary>True while a picture is set and has not failed.</summary>
    public bool IsImageVisible => (bool)GetValue(IsImageVisibleProperty);

    /// <summary>Gets the shape corner radius. This is a read-only dependency property.</summary>
    public CornerRadius ShapeCornerRadius => (CornerRadius)GetValue(ShapeCornerRadiusProperty);

    /// <summary>Gets the initials font size. This is a read-only dependency property.</summary>
    public double InitialsFontSize => (double)GetValue(InitialsFontSizeProperty);

    /// <summary>Gets the status dot size. This is a read-only dependency property.</summary>
    public double StatusDotSize => (double)GetValue(StatusDotSizeProperty);

    /// <summary>Gets the status dot margin. This is a read-only dependency property.</summary>
    public Thickness StatusDotMargin => (Thickness)GetValue(StatusDotMarginProperty);

    /// <summary>
    /// Two-letter initials: the first letters of the first and last words ("Ada Lovelace" → "AL"), or the first two
    /// letters of a single word ("mochi" → "MO"). Empty for a blank name.
    /// </summary>
    public static string GetInitials(string? name) {
        return CreateInitials(name, 2);
    }

    /// <summary>
    /// Initials with a choice of how many letters a single-word name gives (<paramref name="singleWordLetters"/>):
    /// 2 is the Avatar rule ("mochi" → "MO"), 1 gives "M". Several words always give first + last initial.
    /// </summary>
    public static string CreateInitials(string? name, int singleWordLetters) {
        if (string.IsNullOrWhiteSpace(name)) {
            return string.Empty;
        }
        string[] words = name.Split((char[])[' ', '\t', '-', '_', '.', ',', '(', ')', '"', '\''], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0) {
            return string.Empty;
        }
        StringBuilder initials = new();
        if (words.Length >= 2) {
            initials.Append(StringInfo.GetNextTextElement(words[0]));
            initials.Append(StringInfo.GetNextTextElement(words[^1]));
        }
        else {
            TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(words[0]);
            int taken = 0;
            while ((taken < Math.Max(1, singleWordLetters)) && elements.MoveNext()) {
                initials.Append(elements.GetTextElement());
                taken++;
            }
        }
        return initials.ToString().ToUpper(CultureInfo.CurrentCulture);
    }

    /// <summary>A stable pastel index for a name (FNV-1a over the trimmed, lower-cased name).</summary>
    public static int GetColorIndex(string? name) {
        string key = (name ?? string.Empty).Trim().ToLowerInvariant();
        uint hash = 2166136261;
        foreach (char c in key) {
            hash ^= c;
            hash *= 16777619;
        }
        return (int)(hash % PaletteSize);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        statusDot = GetTemplateChild(StatusDotPartName) as FrameworkElement;
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Avatar)d).UpdateImage();
    }

    private static void OnDisplayNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Avatar avatar = (Avatar)d;
        string name = (string?)e.NewValue ?? string.Empty;
        avatar.SetValue(InitialsPropertyKey, GetInitials(name));
        avatar.SetValue(ColorIndexPropertyKey, GetColorIndex(name));
        avatar.UpdateAutomationName();
    }

    private static void OnGeometryChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((Avatar)d).UpdateGeometry();
    }

    private static void OnStatusChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        Avatar avatar = (Avatar)d;
        avatar.UpdateAutomationName();
        if ((AvatarStatus)e.NewValue != AvatarStatus.None) {
            avatar.PopStatusDot();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if (Status != AvatarStatus.None) {
            PopStatusDot();
        }
    }

    private void UpdateImage() {
        if (watchedBitmap is not null) {
            watchedBitmap.DownloadFailed -= OnBitmapFailed;
            watchedBitmap.DecodeFailed -= OnBitmapFailed;
            watchedBitmap = null;
        }
        if (Source is not ImageSource source) {
            SetValue(ImageFillPropertyKey, null);
            SetValue(IsImageVisiblePropertyKey, false);
            return;
        }
        // Bitmaps that are still downloading report failure later; anything else that failed would have thrown
        // when it was created.
        if (source is BitmapSource { IsFrozen: false, IsDownloading: true } bitmap) {
            watchedBitmap = bitmap;
            bitmap.DownloadFailed += OnBitmapFailed;
            bitmap.DecodeFailed += OnBitmapFailed;
        }
        SetValue(ImageFillPropertyKey, new ImageBrush(source) { Stretch = Stretch.UniformToFill });
        SetValue(IsImageVisiblePropertyKey, true);
    }

    private void OnBitmapFailed(object? sender, ExceptionEventArgs e) {
        if (!ReferenceEquals(sender, watchedBitmap)) {
            return;
        }
        SetValue(ImageFillPropertyKey, null);
        SetValue(IsImageVisiblePropertyKey, false);
    }

    private void UpdateGeometry() {
        double size = Size;
        SetValue(ShapeCornerRadiusPropertyKey, (Shape == AvatarShape.Squircle) ? new CornerRadius(Math.Round(size * 0.3, 1)) : new CornerRadius(999));
        SetValue(InitialsFontSizePropertyKey, Math.Max(8, Math.Round(size * 0.38, 1)));
        double dot = Math.Max(8, Math.Round(size * 0.28));
        SetValue(StatusDotSizePropertyKey, dot);
        // Centre the dot on the outline: 45° on a circle, nearer the corner on a squircle.
        double centre = (Shape == AvatarShape.Squircle) ? (size * 0.9) : (size * 0.854);
        double inset = size - centre - (dot / 2);
        SetValue(StatusDotMarginPropertyKey, new Thickness(0, 0, inset, inset));
    }

    private void UpdateAutomationName() {
        string name = string.IsNullOrWhiteSpace(DisplayName) ? "Avatar" : DisplayName;
        AutomationProperties.SetName(this, (Status == AvatarStatus.None) ? name : $"{name}, {Status}");
    }

    private void PopStatusDot() {
        if (!IsLoaded || (statusDot is null)) {
            return;
        }
        ScaleTransform scale = Jelly.EnsureScale(statusDot);
        if (!Jelly.IsMotionEnabled(this)) {
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            return;
        }
        IEasingFunction spring = MuzzleEasings.Get(this, MuzzleEasings.JellyKey);
        Duration duration = new(TimeSpan.FromMilliseconds(650));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, new DoubleAnimation(0.2, 1, duration) { EasingFunction = spring });
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, new DoubleAnimation(0.2, 1, duration) { EasingFunction = spring });
    }
}