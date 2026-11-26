using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Muzzle.Theme.Controls;

namespace Muzzle.Gallery.Pages;

public partial class NavigationPage : UserControl {
    private static readonly string[] Folders = ["Naps", "Sunbeams", "Window sills", "Boxes", "Laundry", "Secret stash", "Under the sofa", "Treat archive"];

    private static readonly (string Title, string Body, string Brush)[] Cards = [
        ("Mochi", "Calico. Chatty. Owns the left half of every bed.", "Muzzle.Brush.PrimarySoft"),
        ("Biscuit", "Ginger. Professional napper with a strict schedule.", "Muzzle.Brush.SecondarySoft"),
        ("Tofu", "White. Remains deeply suspicious of cucumbers.", "Muzzle.Brush.MintSoft"),
        ("Pudding", "Tabby. Will trade affection for chin scratches.", "Muzzle.Brush.WarningSoft")
    ];

    private readonly ObservableCollection<string> path = [];
    private int cardIndex;
    private int nextFolder;

    public NavigationPage() {
        InitializeComponent();
        Crumbs.ItemsSource = path;
        ResetPath();
        TransitionPicker.ItemsSource = Enum.GetValues<ContentTransition>();
        TransitionPicker.SelectedItem = ContentTransition.ScalePop;
        ShowCard();
        UpdatePageStatus();
        UpdateDemoContent();
    }

    private void ResetPath() {
        path.Clear();
        foreach (string segment in new[] { "Home", "Cats", "Mochi", "Favourite spots" }) {
            path.Add(segment);
        }
        nextFolder = 0;
    }

    private void OnCrumbClicked(object sender, BreadcrumbItemClickedEventArgs e) {
        CrumbStatus.Text = $"Clicked: {e.Item} (index {e.Index})";
        while (path.Count > e.Index + 1) {
            path.RemoveAt(path.Count - 1);
        }
    }

    private void OnCrumbDeeper(object sender, RoutedEventArgs e) {
        path.Add(Folders[nextFolder % Folders.Length]);
        nextFolder++;
    }

    private void OnCrumbReset(object sender, RoutedEventArgs e) {
        ResetPath();
        CrumbStatus.Text = "Clicked: nothing yet";
    }

    private void OnPageChanged(object sender, RoutedPropertyChangedEventArgs<int> e) {
        UpdatePageStatus();
    }

    private void UpdatePageStatus() {
        const int pageSize = 12;
        int first = ((Pager.CurrentPage - 1) * pageSize) + 1;
        PageStatus.Text = $"Showing cats {first}–{first + pageSize - 1} of {Pager.PageCount * pageSize}";
    }

    private void OnTransitionPicked(object sender, SelectionChangedEventArgs e) {
        if (TransitionPicker.SelectedItem is ContentTransition transition) {
            CardHost.Transition = transition;
        }
    }

    private void OnNextCard(object sender, RoutedEventArgs e) {
        cardIndex = (cardIndex + 1) % Cards.Length;
        ShowCard();
    }

    private void ShowCard() {
        (string title, string body, string brush) = Cards[cardIndex];
        StackPanel content = new() { Margin = new Thickness(20, 16, 20, 16) };
        content.Children.Add(new TextBlock { Text = title, Style = (Style)FindResource("Muzzle.Text.Title") });
        content.Children.Add(new TextBlock { Text = body, Margin = new Thickness(0, 6, 0, 0), Style = (Style)FindResource("Muzzle.Text.Secondary") });
        Border card = new() {
            CornerRadius = new CornerRadius(16),
            Child = content
        };
        card.SetResourceReference(Border.BackgroundProperty, brush);
        CardHost.Content = card;
    }

    private void OnStepBack(object sender, RoutedEventArgs e) {
        Steps.Previous();
    }

    private void OnStepNext(object sender, RoutedEventArgs e) {
        Steps.Next();
    }

    private void OnDemoNavChanged(object sender, SelectionChangedEventArgs e) {
        UpdateDemoContent();
    }

    private void UpdateDemoContent() {
        if ((DemoNavContent is null) || (DemoNav.SelectedItem is not NavigationViewItem item)) {
            return;
        }
        StackPanel content = new() { Margin = new Thickness(8) };
        content.Children.Add(new TextBlock { Text = item.Content as string ?? string.Empty, Style = (Style)FindResource("Muzzle.Text.Title") });
        content.Children.Add(new TextBlock {
            Text = "Content swaps through a TransitioningContentControl (SlideLeft) as the pill slides to the new item.",
            Margin = new Thickness(0, 6, 0, 0),
            Style = (Style)FindResource("Muzzle.Text.Secondary")
        });
        DemoNavContent.Content = content;
    }
}