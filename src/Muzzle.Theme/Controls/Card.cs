using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>A soft, rounded surface with a diffused elevation shadow. Tune it with MuzzleAssist.CornerRadius / ShadowDepth.</summary>
public class Card : ContentControl {
    static Card() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Card), new FrameworkPropertyMetadata(typeof(Card)));
    }
}