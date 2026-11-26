using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A vertical stack of <see cref="GrowlCard"/>s, top-right by default (see Growl.xaml). Registers itself while
/// loaded so <see cref="Growl"/> can find it, optionally under a <c>mz:Growl.Token</c>.
/// </summary>
public class GrowlContainer : StackPanel {
    /// <summary>Identifies the <see cref="MaxCount"/> dependency property.</summary>
    public static readonly DependencyProperty MaxCountProperty = DependencyProperty.Register(
        nameof(MaxCount),
        typeof(int),
        typeof(GrowlContainer),
        new PropertyMetadata(6));

    /// <summary>Initializes a new instance of the <see cref="GrowlContainer"/> class.</summary>
    public GrowlContainer() {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Oldest toasts are dismissed when more than this many are showing.</summary>
    public int MaxCount {
        get => (int)GetValue(MaxCountProperty);
        set => SetValue(MaxCountProperty, value);
    }

    /// <summary>The desktop window's container is resolved directly and never registered.</summary>
    internal bool IsDesktop { get; init; }

    internal void Add(GrowlCard card) {
        Children.Add(card);
        List<GrowlCard> open = [];
        foreach (UIElement child in Children) {
            if (child is GrowlCard { IsClosing: false } existing) {
                open.Add(existing);
            }
        }
        for (int i = 0; i < open.Count - MaxCount; i++) {
            open[i].Close();
        }
    }

    internal void CloseAll() {
        List<GrowlCard> cards = [];
        foreach (UIElement child in Children) {
            if (child is GrowlCard card) {
                cards.Add(card);
            }
        }
        foreach (GrowlCard card in cards) {
            card.Close();
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        if (!IsDesktop) {
            Growl.Containers.Add(this);
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        Growl.Containers.Remove(this);
    }
}