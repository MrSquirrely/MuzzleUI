using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Hosts <see cref="Dialog"/>s above its own content. Wrap a region of your UI in one (optionally with
/// <c>mz:Dialog.Token</c>) to scope dialogs to it; without one, dialogs use an overlay on the active window.
/// While a modal dialog is open, the content underneath is taken out of hit testing and tab order.
/// </summary>
[TemplatePart(Name = DialogHostPartName, Type = typeof(Panel))]
public class DialogContainer : ContentControl {
    private const string DialogHostPartName = "PART_DialogHost";

    private static readonly DependencyPropertyKey HasModalDialogPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(HasModalDialog),
        typeof(bool),
        typeof(DialogContainer),
        new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="HasModalDialog"/> read-only dependency property.</summary>
    public static readonly DependencyProperty HasModalDialogProperty = HasModalDialogPropertyKey.DependencyProperty;

    private readonly List<Dialog> pending = [];
    private Panel? host;

    static DialogContainer() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(DialogContainer), new FrameworkPropertyMetadata(typeof(DialogContainer)));
        FocusableProperty.OverrideMetadata(typeof(DialogContainer), new FrameworkPropertyMetadata(false));
    }

    /// <summary>Initializes a new instance of the <see cref="DialogContainer"/> class.</summary>
    public DialogContainer() {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    /// <summary>Gets whether it has modal dialog. This is a read-only dependency property.</summary>
    public bool HasModalDialog => (bool)GetValue(HasModalDialogProperty);

    internal static TokenRegistry<DialogContainer> Registry { get; } = new();

    /// <summary>True for the per-window overlay created by the static API.</summary>
    internal bool IsOverlay { get; init; }

    internal Dialog? TopDialog {
        get {
            if (host is null) {
                return null;
            }
            for (int i = host.Children.Count - 1; i >= 0; i--) {
                if (host.Children[i] is Dialog { IsClosing: false } dialog) {
                    return dialog;
                }
            }
            return null;
        }
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        host = GetTemplateChild(DialogHostPartName) as Panel;
        if (host is null) {
            return;
        }
        foreach (Dialog dialog in pending) {
            host.Children.Add(dialog);
        }
        pending.Clear();
        UpdateModalState();
    }

    internal void Add(Dialog dialog) {
        dialog.Host = this;
        if (host is null) {
            ApplyTemplate();
        }
        if (host is null) {
            pending.Add(dialog);
        }
        else {
            host.Children.Add(dialog);
        }
        UpdateModalState();
    }

    internal void Remove(Dialog dialog) {
        pending.Remove(dialog);
        host?.Children.Remove(dialog);
        UpdateModalState();
    }

    internal void OnDialogClosing() {
        UpdateModalState();
    }

    private void UpdateModalState() {
        bool modal = false;
        IEnumerable<UIElement> dialogs = host is null ? pending : host.Children.OfType<UIElement>();
        foreach (UIElement element in dialogs) {
            if (element is Dialog { IsModal: true, IsClosing: false }) {
                modal = true;
                break;
            }
        }
        SetValue(HasModalDialogPropertyKey, modal);
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        Registry.Add(this);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) {
        Registry.Remove(this);
    }
}