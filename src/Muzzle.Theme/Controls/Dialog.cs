using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>
/// A modal overlay: a softly dimmed backdrop with a surface that pops in from 0.82× with a springy
/// overshoot (1.04× → 1.0×) and shrinks away to 0.9× when dismissed.
/// <para>Show from code with <see cref="Show(object, string?)"/>, <see cref="Show{T}(string?)"/> or
/// <see cref="ShowAsync{TResult}(object, string?)"/>. Dialogs appear over the active window's client area, or inside
/// the <see cref="DialogContainer"/> whose <c>mz:Dialog.Token</c> matches.</para>
/// </summary>
[TemplatePart(Name = BackdropPartName, Type = typeof(FrameworkElement))]
[TemplatePart(Name = SurfacePartName, Type = typeof(FrameworkElement))]
public class Dialog : ContentControl {
    private const string BackdropPartName = "PART_Backdrop";
    private const string SurfacePartName = "PART_Surface";

    /// <summary>Closes the containing dialog; the CommandParameter becomes its result.</summary>
    public static readonly RoutedCommand CloseCommand = new(nameof(CloseCommand), typeof(Dialog));

    /// <summary>Names a <see cref="DialogContainer"/> so dialogs can target it.</summary>
    public static readonly DependencyProperty TokenProperty = DependencyProperty.RegisterAttached(
        "Token",
        typeof(string),
        typeof(Dialog),
        new PropertyMetadata(null));

    /// <summary>Identifies the <see cref="CloseOnBackdropClick"/> dependency property.</summary>
    public static readonly DependencyProperty CloseOnBackdropClickProperty = DependencyProperty.Register(
        nameof(CloseOnBackdropClick), typeof(bool), typeof(Dialog), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="CloseOnEscape"/> dependency property.</summary>
    public static readonly DependencyProperty CloseOnEscapeProperty = DependencyProperty.Register(
        nameof(CloseOnEscape), typeof(bool), typeof(Dialog), new PropertyMetadata(true));

    /// <summary>When true (default) the backdrop blocks the content behind; false shows a non-blocking dialog.</summary>
    public static readonly DependencyProperty IsModalProperty = DependencyProperty.Register(
        nameof(IsModal), typeof(bool), typeof(Dialog), new PropertyMetadata(true));

    private readonly TaskCompletionSource<object?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private FrameworkElement? backdrop;
    private FrameworkElement? surface;
    private IInputElement? restoreFocus;

    static Dialog() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(Dialog), new FrameworkPropertyMetadata(typeof(Dialog)));
    }

    /// <summary>Initializes a new instance of the <see cref="Dialog"/> class.</summary>
    public Dialog() {
        CommandBindings.Add(new CommandBinding(CloseCommand, OnCloseCommand));
        Loaded += OnLoaded;
    }

    /// <summary>Raised after the dialog has animated out and left its host.</summary>
    public event EventHandler? Closed;

    /// <summary>Gets or sets a value indicating whether close on backdrop click. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool CloseOnBackdropClick {
        get => (bool)GetValue(CloseOnBackdropClickProperty);
        set => SetValue(CloseOnBackdropClickProperty, value);
    }

    /// <summary>Gets or sets a value indicating whether close on escape. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool CloseOnEscape {
        get => (bool)GetValue(CloseOnEscapeProperty);
        set => SetValue(CloseOnEscapeProperty, value);
    }

    /// <summary>Gets or sets whether the dialog blocks the content behind it. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool IsModal {
        get => (bool)GetValue(IsModalProperty);
        set => SetValue(IsModalProperty, value);
    }

    /// <summary>Gets whether the dialog is playing its close animation.</summary>
    public bool IsClosing { get; private set; }

    /// <summary>Gets whether the dialog has finished closing and been removed.</summary>
    public bool IsClosed => completion.Task.IsCompleted;

    /// <summary>The value passed to <see cref="CloseWithResult(object?)"/>.</summary>
    public object? Result { get; private set; }

    /// <summary>Completes with the result once the dialog is closed.</summary>
    public Task<object?> Completion => completion.Task;

    internal DialogContainer? Host { get; set; }

    /// <summary>Gets the value of the <c>Token</c> attached property.</summary>
    /// <param name="element">The element to read from.</param>
    /// <returns>The current <c>Token</c> value.</returns>
    public static string? GetToken(DependencyObject element) {
        return (string?)element.GetValue(TokenProperty);
    }

    /// <summary>Sets the value of the <c>Token</c> attached property.</summary>
    /// <param name="element">The element to set it on.</param>
    /// <param name="value">The new value.</param>
    public static void SetToken(DependencyObject element, string? value) {
        element.SetValue(TokenProperty, value);
    }

    /// <summary>Shows <paramref name="content"/> (a UserControl, or a ViewModel rendered through a DataTemplate) in a dialog.</summary>
    public static Dialog Show(object content, string? token = null) {
        DialogContainer host = ResolveHost(token);
        Dialog dialog = content as Dialog ?? new Dialog { Content = content };
        host.Add(dialog);
        return dialog;
    }

    /// <summary>Creates a <typeparamref name="T"/> (typically a UserControl) and shows it in a dialog.</summary>
    public static Dialog Show<T>(string? token = null) where T : new() {
        return Show(new T()!, token);
    }

    /// <summary>Shows <paramref name="content"/> and completes with its result when it closes.</summary>
    public static Task<TResult?> ShowAsync<TResult>(object content, string? token = null) {
        Dialog dialog = Show(content, token);
        dialog.WireResultable<TResult>(content);
        if (content is FrameworkElement { DataContext: not null } element) {
            dialog.WireResultable<TResult>(element.DataContext);
        }
        return dialog.GetResultAsync<TResult>();
    }

    /// <summary>Creates a <typeparamref name="T"/>, shows it and completes with its <typeparamref name="TResult"/>.</summary>
    public static Task<TResult?> ShowAsync<T, TResult>(string? token = null) where T : new() {
        return ShowAsync<TResult>(new T()!, token);
    }

    /// <summary>Closes the top-most dialog in the host for <paramref name="token"/> (or the active window).</summary>
    public static void Close(string? token = null) {
        DialogContainer? host = string.IsNullOrEmpty(token) ? TryResolveDefaultHost() : DialogContainer.Registry.Find(token, GetToken);
        host?.TopDialog?.Close();
    }

    /// <summary>Finds the dialog that contains <paramref name="element"/>, for closing it from inside its content.</summary>
    public static Dialog? GetDialog(DependencyObject element) {
        DependencyObject? current = element;
        while (current is not null) {
            if (current is Dialog dialog) {
                return dialog;
            }
            DependencyObject? parent = (current is Visual) || (current is Visual3D) ? VisualTreeHelper.GetParent(current) : null;
            current = parent ?? LogicalTreeHelper.GetParent(current);
        }
        return null;
    }

    /// <summary>Closes the dialog with no result.</summary>
    public void Close() {
        CloseWithResult(null);
    }

    /// <summary>Closes the dialog; <paramref name="result"/> completes <see cref="Completion"/> / <see cref="GetResultAsync{TResult}"/>.</summary>
    public void CloseWithResult(object? result) {
        if (IsClosing || IsClosed) {
            return;
        }
        IsClosing = true;
        Result = result;
        Host?.OnDialogClosing();
        PlayClose();
    }

    /// <summary>Returns a task that completes with the dialog's result when it closes.</summary>
    /// <typeparam name="TResult">The expected result type.</typeparam>
    /// <returns>The result, or the default when the dialog closed without a result of that type.</returns>
    public async Task<TResult?> GetResultAsync<TResult>() {
        object? result = await completion.Task;
        return result is TResult typed ? typed : default;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (backdrop is not null) {
            backdrop.MouseLeftButtonDown -= OnBackdropMouseDown;
        }
        base.OnApplyTemplate();
        backdrop = GetTemplateChild(BackdropPartName) as FrameworkElement;
        surface = GetTemplateChild(SurfacePartName) as FrameworkElement;
        if (backdrop is not null) {
            backdrop.MouseLeftButtonDown += OnBackdropMouseDown;
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e) {
        base.OnKeyDown(e);
        if (!e.Handled && (e.Key == Key.Escape) && CloseOnEscape) {
            Close();
            e.Handled = true;
        }
    }

    private static DialogContainer ResolveHost(string? token) {
        if (!string.IsNullOrEmpty(token)) {
            return DialogContainer.Registry.Find(token, GetToken)
                ?? throw new InvalidOperationException($"No loaded DialogContainer has mz:Dialog.Token=\"{token}\".");
        }
        return TryResolveDefaultHost() ?? throw new InvalidOperationException("Dialog needs an open window to attach to.");
    }

    private static DialogContainer? TryResolveDefaultHost() {
        Window? window = OverlayHost.ActiveWindow();
        if (window is null) {
            return null;
        }
        DialogContainer? declared = DialogContainer.Registry.Alive().Find(container =>
            !container.IsOverlay && string.IsNullOrEmpty(GetToken(container)) && ReferenceEquals(Window.GetWindow(container), window));
        return declared ?? OverlayHost.Get(window).Dialogs;
    }

    private void WireResultable<TResult>(object? candidate) {
        if (candidate is IDialogResultable<TResult> resultable) {
            resultable.CloseAction = () => CloseWithResult(resultable.Result);
        }
    }

    private void OnCloseCommand(object sender, ExecutedRoutedEventArgs e) {
        CloseWithResult(e.Parameter);
        e.Handled = true;
    }

    private void OnBackdropMouseDown(object sender, MouseButtonEventArgs e) {
        e.Handled = true;
        if (CloseOnBackdropClick) {
            Close();
        }
        else if (surface is not null) {
            // A gentle "not like that" wobble instead of closing.
            Jelly.Wobble(surface);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e) {
        Loaded -= OnLoaded;
        restoreFocus = Keyboard.FocusedElement;
        PlayOpen();
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(FocusFirst));
    }

    private void FocusFirst() {
        if ((surface is not null) && !surface.IsKeyboardFocusWithin) {
            surface.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
        }
    }

    private void PlayOpen() {
        if ((surface is null) || !Jelly.IsMotionEnabled(this)) {
            return;
        }
        backdrop?.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(240)));
        surface.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
        ScaleTransform scale = Jelly.EnsureScale(surface);
        DoubleAnimationUsingKeyFrames pop = new();
        pop.KeyFrames.Add(new EasingDoubleKeyFrame(0.82, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        pop.KeyFrames.Add(new EasingDoubleKeyFrame(1.04, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240)), MuzzleEasings.Get(this, MuzzleEasings.BackKey)));
        pop.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(560)), MuzzleEasings.Get(this, MuzzleEasings.JellySoftKey)));
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pop);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pop);
    }

    private void PlayClose() {
        if ((surface is null) || !Jelly.IsMotionEnabled(this) || !IsLoaded) {
            Finish();
            return;
        }
        Duration duration = TimeSpan.FromMilliseconds(190);
        IEasingFunction squish = MuzzleEasings.Get(this, MuzzleEasings.SquishKey);
        ScaleTransform scale = Jelly.EnsureScale(surface);
        DoubleAnimation shrink = new(0.9, duration) { EasingFunction = squish };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, shrink);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, shrink);
        surface.BeginAnimation(OpacityProperty, new DoubleAnimation(0, duration));
        DoubleAnimation fade = new(0, TimeSpan.FromMilliseconds(230));
        fade.Completed += OnCloseAnimationCompleted;
        if (backdrop is not null) {
            backdrop.BeginAnimation(OpacityProperty, fade);
        }
        else {
            BeginAnimation(OpacityProperty, fade);
        }
    }

    private void OnCloseAnimationCompleted(object? sender, EventArgs e) {
        Finish();
    }

    private void Finish() {
        bool hadFocus = IsKeyboardFocusWithin;
        Host?.Remove(this);
        if (hadFocus && (restoreFocus is UIElement { IsVisible: true } previous)) {
            previous.Focus();
        }
        completion.TrySetResult(Result);
        Closed?.Invoke(this, EventArgs.Empty);
    }
}