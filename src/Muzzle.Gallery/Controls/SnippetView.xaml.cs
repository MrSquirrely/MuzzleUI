using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Muzzle.Gallery.Controls;

/// <summary>A collapsible, copyable XAML snippet.</summary>
public partial class SnippetView : UserControl {
    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code),
        typeof(string),
        typeof(SnippetView),
        new PropertyMetadata(string.Empty));

    private readonly DispatcherTimer resetTimer;

    public SnippetView() {
        InitializeComponent();
        resetTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.6) };
        resetTimer.Tick += OnResetTick;
    }

    public string Code {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    private void OnCopyClick(object sender, RoutedEventArgs e) {
        try {
            Clipboard.SetText(Code);
            CopyButton.Content = "Copied!";
        }
        catch (COMException) {
            CopyButton.Content = "Clipboard busy";
        }
        resetTimer.Stop();
        resetTimer.Start();
    }

    private void OnResetTick(object? sender, EventArgs e) {
        resetTimer.Stop();
        CopyButton.Content = "Copy";
    }
}