using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

using Muzzle.Theme;

namespace Muzzle.Gallery.Models;

public enum Mood {
    Sleepy,
    Playful,
    Zoomies,
    Grumpy
}

/// <summary>A live sample object for the PropertyGrid playground. Every edit updates the preview card instantly.</summary>
public sealed class PlaygroundModel : INotifyPropertyChanged {
    private string caption = "Boop me";
    private string cardTitle = "Biscuit the cat";
    private bool isEnabled = true;
    private int treats = 3;
    private double cornerRadius = 16;
    private ShadowDepth shadow = ShadowDepth.Soft;
    private Thickness padding = new(20);
    private double jellyIntensity = 1;
    private int progress = 64;
    private Color accent = Color.FromRgb(0xF4, 0x97, 0xA9);
    private Brush cardBackground = CreateBrush((Application.Current?.TryFindResource("Muzzle.Color.Surface") is Color surface) ? surface : Colors.White);
    private Mood mood = Mood.Playful;
    private bool? likesBelly;
    private string secret = "hidden from the grid";

    public event PropertyChangedEventHandler? PropertyChanged;

    [Category("Content")]
    [DisplayName("Button caption")]
    [Description("Text shown on the preview button.")]
    public string Caption {
        get => caption;
        set => SetField(ref caption, value);
    }

    [Category("Content")]
    [DisplayName("Card title")]
    [Description("Headline of the preview card.")]
    public string CardTitle {
        get => cardTitle;
        set => SetField(ref cardTitle, value);
    }

    [Category("Content")]
    [DisplayName("Enabled")]
    [Description("Disabled controls fade to a soft pastel opacity instead of turning grey.")]
    public bool IsEnabled {
        get => isEnabled;
        set => SetField(ref isEnabled, value);
    }

    [Category("Content")]
    [DisplayName("Treats")]
    [Description("A plain integer: edit it with the spin buttons or type a number.")]
    [DefaultValue(3)]
    public int Treats {
        get => treats;
        set => SetField(ref treats, value);
    }

    [Category("Shape")]
    [DisplayName("Corner radius")]
    [Description("MuzzleAssist.CornerRadius of the card. [Range(0, 40)] turns this into a slider.")]
    [Range(0, 40)]
    public double CornerRadius {
        get => cornerRadius;
        set => SetField(ref cornerRadius, value);
    }

    [Category("Shape")]
    [DisplayName("Shadow depth")]
    [Description("MuzzleAssist.ShadowDepth: soft, diffused elevation from None to Floating.")]
    public ShadowDepth Shadow {
        get => shadow;
        set => SetField(ref shadow, value);
    }

    [Category("Shape")]
    [DisplayName("Padding")]
    [Description("Any type with a TypeConverter gets a text editor. Try \"28,16\".")]
    public Thickness Padding {
        get => padding;
        set => SetField(ref padding, value);
    }

    [Category("Jelly")]
    [DisplayName("Jelly intensity")]
    [Description("MuzzleAssist.JellyIntensity multiplies every squish and bounce. 0 turns motion off.")]
    [Range(0, 3)]
    [DefaultValue(1.0)]
    [Muzzle.Theme.Controls.PropertyStep(0.25, Format = "0.00×")]
    public double JellyIntensity {
        get => jellyIntensity;
        set => SetField(ref jellyIntensity, value);
    }

    [Category("Jelly")]
    [DisplayName("Progress")]
    [Description("Drives the preview ProgressBar, which glides to each new value.")]
    [Range(0, 100)]
    [DefaultValue(64)]
    public int Progress {
        get => progress;
        set => SetField(ref progress, value);
    }

    [Category("Colors")]
    [DisplayName("Accent")]
    [Description("A Color property: pick a pastel swatch or type a hex value.")]
    public Color Accent {
        get => accent;
        set => SetField(ref accent, value);
    }

    [Category("Colors")]
    [DisplayName("Card background")]
    [Description("A Brush property: solid colour brushes are edited like colours.")]
    public Brush CardBackground {
        get => cardBackground;
        set => SetField(ref cardBackground, value);
    }

    [Category("Personality")]
    [DisplayName("Mood")]
    [Description("Enums become a ComboBox.")]
    public Mood Mood {
        get => mood;
        set => SetField(ref mood, value);
    }

    [Category("Personality")]
    [DisplayName("Likes belly rubs")]
    [Description("A nullable bool becomes a three-state CheckBox. Nobody truly knows.")]
    public bool? LikesBelly {
        get => likesBelly;
        set => SetField(ref likesBelly, value);
    }

    [Browsable(false)]
    public string Secret {
        get => secret;
        set => SetField(ref secret, value);
    }

    private static SolidColorBrush CreateBrush(Color color) {
        SolidColorBrush brush = new(color);
        brush.Freeze();
        return brush;
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}