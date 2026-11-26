using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;

namespace Muzzle.Theme.Controls;

/// <summary>
/// Borderless, transparent, click-through overlay pinned to the top-right of the desktop work area.
/// Used for global toasts and whenever no in-window <see cref="GrowlContainer"/> is registered.
/// It hides itself when the last toast leaves and closes with the application's main window.
/// </summary>
public sealed class GrowlWindow : Window {
    private const double ColumnWidth = 400;

    private static GrowlWindow? shared;

    private GrowlWindow() {
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Focusable = false;
        Title = "Notifications";
        SetResourceReference(FontFamilyProperty, "Muzzle.FontFamily");
        SetResourceReference(ForegroundProperty, "Muzzle.Brush.TextPrimary");

        Container = new GrowlContainer { IsDesktop = true, Margin = new Thickness(16), Width = ColumnWidth - 32 };
        Content = Container;
        PlaceInCorner();
        SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
        if (Application.Current?.MainWindow is Window main) {
            main.Closed += OnMainWindowClosed;
        }
    }

    internal static bool HasShared => shared is not null;

    internal static GrowlWindow Shared => shared ??= new GrowlWindow();

    /// <summary>Gets the toast container hosted in the desktop-corner window.</summary>
    public GrowlContainer Container { get; }

    internal void Add(GrowlCard card) {
        card.Closed += OnCardClosed;
        Container.Add(card);
        if (!IsVisible) {
            PlaceInCorner();
            Show();
        }
    }

    /// <inheritdoc />
    protected override void OnClosed(EventArgs e) {
        SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
        if (ReferenceEquals(shared, this)) {
            shared = null;
        }
        base.OnClosed(e);
    }

    private void PlaceInCorner() {
        Rect area = SystemParameters.WorkArea;
        Width = ColumnWidth;
        Height = area.Height;
        Left = area.Right - ColumnWidth;
        Top = area.Top;
    }

    private void OnSystemParametersChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(SystemParameters.WorkArea)) {
            PlaceInCorner();
        }
    }

    private void OnCardClosed(object? sender, EventArgs e) {
        if (sender is GrowlCard card) {
            card.Closed -= OnCardClosed;
        }
        if (Container.Children.Count == 0) {
            Hide();
        }
    }

    private void OnMainWindowClosed(object? sender, EventArgs e) {
        Close();
    }
}