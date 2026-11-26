using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

using Muzzle.Gallery.Models;
using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class DataMediaPage : UserControl {
    private static readonly string[] SampleNames = [
        "Ada Lovelace", "Grace Hopper", "Alan Turing", "Katherine Johnson", "Linus", "Margaret Hamilton",
        "Mochi", "Biscuit Butterworth", "Pepper", "Noodle", "Clementine Rose", "Tofu"
    ];

    private static readonly string[] NewCatNames = ["Waffles", "Pudding", "Marble", "Juniper", "Toast", "Pixel", "Sushi", "Olive"];

    private readonly ObservableCollection<CatRecord> cats = CatRecord.CreateSample();
    private int newCatCount;

    public DataMediaPage() {
        InitializeComponent();
        CatGrid.ItemsSource = cats;
        NameGrid.ItemsSource = SampleNames;
        SlideShow.ItemsSource = Slide.CreateSample();
        HouseTree.ItemsSource = HouseNode.CreateSample();
        BrokenAvatar.Source = CreateBrokenImage();
    }

    public ObservableCollection<CatRecord> Cats => cats;

    // A picture whose download is refused straight away (nothing listens on the discard port), so the avatar falls back to initials.
    private static BitmapImage CreateBrokenImage() {
        BitmapImage image = new();
        image.BeginInit();
        image.UriSource = new Uri("http://127.0.0.1:9/cat.png");
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.EndInit();
        return image;
    }

    private void OnAddCat(object sender, RoutedEventArgs e) {
        string name = NewCatNames[newCatCount % NewCatNames.Length];
        newCatCount++;
        CatRecord cat = new(name, CatRecord.Breeds[newCatCount % CatRecord.Breeds.Count], newCatCount % 6, true, CatRecord.Moods[newCatCount % CatRecord.Moods.Count], 3, AvatarStatus.Online);
        cats.Add(cat);
        CatGrid.SelectedItem = cat;
        CatGrid.ScrollIntoView(cat);
    }

    private void OnRemoveCat(object sender, RoutedEventArgs e) {
        if (CatGrid.SelectedItem is CatRecord cat) {
            cats.Remove(cat);
        }
    }

    private void OnCycleStatus(object sender, RoutedEventArgs e) {
        StatusAvatar.Status = StatusAvatar.Status switch {
            AvatarStatus.Online => AvatarStatus.Away,
            AvatarStatus.Away => AvatarStatus.Busy,
            AvatarStatus.Busy => AvatarStatus.Offline,
            AvatarStatus.Offline => AvatarStatus.None,
            _ => AvatarStatus.Online
        };
        StatusText.Text = StatusAvatar.Status.ToString();
    }
}