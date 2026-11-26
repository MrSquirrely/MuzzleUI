using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Models;

/// <summary>A row in the Data &amp; media page's editable DataGrid.</summary>
public sealed class CatRecord(string name, string breed, int age, bool isIndoor, Mood mood, int treats, AvatarStatus status) : INotifyPropertyChanged {
    public static IReadOnlyList<string> Breeds { get; } = ["Calico", "Maine Coon", "Ragdoll", "Siamese", "Scottish Fold", "Tabby", "Tuxedo"];

    public static IReadOnlyList<Mood> Moods { get; } = [Mood.Sleepy, Mood.Playful, Mood.Zoomies, Mood.Grumpy];

    private string name = name;
    private string breed = breed;
    private int age = age;
    private bool isIndoor = isIndoor;
    private Mood mood = mood;
    private int treats = treats;
    private AvatarStatus status = status;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name {
        get => name;
        set => SetField(ref name, value);
    }

    public string Breed {
        get => breed;
        set => SetField(ref breed, value);
    }

    public int Age {
        get => age;
        set => SetField(ref age, value);
    }

    public bool IsIndoor {
        get => isIndoor;
        set => SetField(ref isIndoor, value);
    }

    public Mood Mood {
        get => mood;
        set => SetField(ref mood, value);
    }

    public int Treats {
        get => treats;
        set => SetField(ref treats, value);
    }

    public AvatarStatus Status {
        get => status;
        set => SetField(ref status, value);
    }

    public static ObservableCollection<CatRecord> CreateSample() {
        return [
            new CatRecord("Mochi", "Calico", 3, true, Mood.Playful, 4, AvatarStatus.Online),
            new CatRecord("Biscuit Butterworth", "Tabby", 7, true, Mood.Sleepy, 2, AvatarStatus.Away),
            new CatRecord("Pepper", "Tuxedo", 2, false, Mood.Zoomies, 6, AvatarStatus.Busy),
            new CatRecord("Sir Fluffington", "Maine Coon", 9, true, Mood.Grumpy, 1, AvatarStatus.Offline),
            new CatRecord("Noodle", "Siamese", 4, true, Mood.Playful, 3, AvatarStatus.Online),
            new CatRecord("Clementine Rose", "Ragdoll", 5, false, Mood.Sleepy, 5, AvatarStatus.None),
            new CatRecord("Tofu", "Scottish Fold", 1, true, Mood.Zoomies, 8, AvatarStatus.Online)
        ];
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null) {
        if (EqualityComparer<T>.Default.Equals(field, value)) {
            return;
        }
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>A node in the Data &amp; media page's TreeView.</summary>
public sealed class HouseNode(string name, string glyph, string? detail = null, params HouseNode[] children) {
    public string Name { get; } = name;

    /// <summary>Path data for a 16×16 line icon.</summary>
    public string Glyph { get; } = glyph;

    public string? Detail { get; } = detail;

    public IReadOnlyList<HouseNode> Children { get; } = children;

    public bool IsExpanded { get; set; }

    public const string RoomGlyph = "M 2,8 L 8,2.5 L 14,8 M 4,6.5 V 14 H 12 V 6.5";
    public const string BedGlyph = "M 2,12 V 6 M 2,10 H 14 V 12 M 4.5,10 V 8 H 9 V 10";
    public const string ToyGlyph = "M 8,2 A 6,6 0 1 1 7.99,2 Z M 3,6 C 6,8 10,8 13,6 M 3,10 C 6,8 10,8 13,10";
    public const string BowlGlyph = "M 2,8 H 14 C 14,11.5 11.5,13.5 8,13.5 C 4.5,13.5 2,11.5 2,8 Z";

    public static IReadOnlyList<HouseNode> CreateSample() {
        return [
            new HouseNode("Living room", RoomGlyph, null,
                new HouseNode("Sunny window sill", BedGlyph, "Mochi's favourite"),
                new HouseNode("Cardboard castle", ToyGlyph, "3 floors"),
                new HouseNode("Sofa", BedGlyph, null,
                    new HouseNode("Left cushion", BedGlyph, "claimed"),
                    new HouseNode("Under the sofa", ToyGlyph, "11 lost toys"))) { IsExpanded = true },
            new HouseNode("Kitchen", RoomGlyph, null,
                new HouseNode("Food bowl", BowlGlyph, "salmon bites"),
                new HouseNode("Water fountain", BowlGlyph, null)),
            new HouseNode("Bedroom", RoomGlyph, null,
                new HouseNode("The human's pillow", BedGlyph, "3 a.m. zoomies start here"),
                new HouseNode("Laundry basket", BedGlyph, "warm"))
        ];
    }
}

/// <summary>A slide in the Data &amp; media page's carousel.</summary>
public sealed record Slide(string Title, string Caption, Brush Background, Brush Accent) {
    public static IReadOnlyList<Slide> CreateSample() {
        return [
            Create("Sunbeam season", "Biscuit has found the warmest square metre in the house.", "#FDE7EC", "#F9C3A5", "#B4405C"),
            Create("Cardboard castle", "Three floors, two windows, zero building permits.", "#EEEAFA", "#C9E4F6", "#4B3F7A"),
            Create("Garden patrol", "Mochi inspects every leaf. Twice.", "#E3F4F0", "#F8F0C9", "#2F6458"),
            Create("Nap o'clock", "Do not disturb. Tofu is recharging.", "#E8F3FC", "#EEEAFA", "#2D5E87")
        ];
    }

    private static Slide Create(string title, string caption, string from, string to, string accent) {
        LinearGradientBrush background = new((Color)ColorConverter.ConvertFromString(from), (Color)ColorConverter.ConvertFromString(to), 35);
        background.Freeze();
        SolidColorBrush ink = new((Color)ColorConverter.ConvertFromString(accent));
        ink.Freeze();
        return new Slide(title, caption, background, ink);
    }
}