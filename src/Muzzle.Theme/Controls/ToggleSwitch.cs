using System.Windows;
using System.Windows.Controls.Primitives;

namespace Muzzle.Theme.Controls;

/// <summary>An on/off switch with a spring-loaded thumb. Behaves exactly like a two-state ToggleButton.</summary>
public class ToggleSwitch : ToggleButton {
    static ToggleSwitch() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(typeof(ToggleSwitch)));
        IsThreeStateProperty.OverrideMetadata(typeof(ToggleSwitch), new FrameworkPropertyMetadata(false, null, static (_, _) => false));
    }
}