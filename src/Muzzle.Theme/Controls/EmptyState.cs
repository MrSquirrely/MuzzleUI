using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A centred placeholder for empty lists, zero-result searches and onboarding: a soft icon disc, a title, a short
/// description and optional action content (e.g. a button). The icon pops in when the state appears.
/// </summary>
public class EmptyState : Control {
    /// <summary>Identifies the <see cref="IconKind"/> dependency property.</summary>
    public static readonly DependencyProperty IconKindProperty = DependencyProperty.Register(
        nameof(IconKind), typeof(MuzzleIconKind), typeof(EmptyState), new PropertyMetadata(MuzzleIconKind.Inbox));

    /// <summary>Custom icon content; when set it replaces <see cref="IconKind"/>.</summary>
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
    public static readonly DependencyProperty DescriptionProperty = DependencyProperty.Register(
        nameof(Description), typeof(string), typeof(EmptyState), new PropertyMetadata(string.Empty));

    /// <summary>Identifies the <see cref="ActionContent"/> dependency property.</summary>
    public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register(
        nameof(ActionContent), typeof(object), typeof(EmptyState), new PropertyMetadata(null));

    static EmptyState() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(EmptyState), new FrameworkPropertyMetadata(typeof(EmptyState)));
        FocusableProperty.OverrideMetadata(typeof(EmptyState), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Gets or sets the icon kind. The default is <c>MuzzleIconKind.Inbox</c>. This is a dependency property.</summary>
    public MuzzleIconKind IconKind {
        get => (MuzzleIconKind)GetValue(IconKindProperty);
        set => SetValue(IconKindProperty, value);
    }

    /// <summary>Gets or sets the icon. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? Icon {
        get => GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    /// <summary>Gets or sets the title. The default is an empty string. This is a dependency property.</summary>
    public string Title {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>Gets or sets the description. The default is an empty string. This is a dependency property.</summary>
    public string Description {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    /// <summary>Gets or sets the action content. The default is <see langword="null"/>. This is a dependency property.</summary>
    public object? ActionContent {
        get => GetValue(ActionContentProperty);
        set => SetValue(ActionContentProperty, value);
    }
}