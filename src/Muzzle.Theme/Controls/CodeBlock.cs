using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Threading;

namespace Muzzle.Theme.Controls;

/// <summary>Clipboard helpers that cope with the clipboard being briefly locked by another app.</summary>
public static class ClipboardHelper {
    /// <summary>Copies text, retrying a few times while another process holds the clipboard. Returns false if it never got it.</summary>
    public static bool TrySetText(string text) {
        for (int attempt = 0; attempt < 5; attempt++) {
            try {
                Clipboard.SetDataObject(text, true);
                return true;
            }
            catch (COMException) {
                Thread.Sleep(40);
            }
            catch (ExternalException) {
                Thread.Sleep(40);
            }
        }
        return false;
    }
}

/// <summary>Shared copy-button behaviour: copy, flip <c>IsCopied</c> for two seconds, raise <c>Copied</c>.</summary>
public abstract class CopyableControlBase : Control {
    /// <summary>Name of the copy button template part.</summary>
    public const string CopyButtonPartName = "PART_CopyButton";

    private static readonly DependencyPropertyKey IsCopiedPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsCopied), typeof(bool), typeof(CopyableControlBase), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="IsCopied"/> read-only dependency property.</summary>
    public static readonly DependencyProperty IsCopiedProperty = IsCopiedPropertyKey.DependencyProperty;

    /// <summary>Identifies the <see cref="Copied"/> routed event.</summary>
    public static readonly RoutedEvent CopiedEvent = EventManager.RegisterRoutedEvent(
        nameof(Copied), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(CopyableControlBase));

    private readonly DispatcherTimer resetTimer;
    private ButtonBase? copyButton;

    /// <summary>Initializes a new instance of the <see cref="CopyableControlBase"/> class.</summary>
    protected CopyableControlBase() {
        resetTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher) { Interval = TimeSpan.FromSeconds(2) };
        resetTimer.Tick += OnResetTick;
    }

    /// <summary>Occurs after the text has been copied to the clipboard.</summary>
    public event RoutedEventHandler Copied {
        add => AddHandler(CopiedEvent, value);
        remove => RemoveHandler(CopiedEvent, value);
    }

    /// <summary>True for two seconds after a successful copy (the button shows a mint "Copied!").</summary>
    public bool IsCopied => (bool)GetValue(IsCopiedProperty);

    /// <summary>Copies <see cref="GetCopyText"/> to the clipboard. Returns false when the clipboard was unavailable.</summary>
    public bool Copy() {
        if (!ClipboardHelper.TrySetText(GetCopyText())) {
            return false;
        }
        SetValue(IsCopiedPropertyKey, true);
        resetTimer.Stop();
        resetTimer.Start();
        RaiseEvent(new RoutedEventArgs(CopiedEvent, this));
        return true;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        if (copyButton is not null) {
            copyButton.Click -= OnCopyClick;
        }
        base.OnApplyTemplate();
        copyButton = GetTemplateChild(CopyButtonPartName) as ButtonBase;
        if (copyButton is not null) {
            copyButton.Click += OnCopyClick;
        }
    }

    /// <summary>When overridden, returns the text that <see cref="Copy"/> puts on the clipboard.</summary>
    /// <returns>The text to copy.</returns>
    protected abstract string GetCopyText();

    private void OnCopyClick(object sender, RoutedEventArgs e) {
        Copy();
    }

    private void OnResetTick(object? sender, EventArgs e) {
        resetTimer.Stop();
        SetValue(IsCopiedPropertyKey, false);
    }
}

/// <summary>
/// A rounded monospace code surface with optional line numbers, light syntax colouring for XAML / XML and C#, a
/// language badge and a floating one-click Copy button that morphs into a bouncing mint "Copied!" for two seconds.
/// </summary>
[TemplatePart(Name = CodePartName, Type = typeof(TextBlock))]
[TemplatePart(Name = LineNumbersPartName, Type = typeof(TextBlock))]
public class CodeBlock : CopyableControlBase {
    /// <summary>Name of the TextBlock template part that receives the (coloured) code runs.</summary>
    public const string CodePartName = "PART_Code";
    /// <summary>Name of the TextBlock template part that shows line numbers.</summary>
    public const string LineNumbersPartName = "PART_LineNumbers";

    /// <summary>Identifies the <see cref="Code"/> dependency property.</summary>
    public static readonly DependencyProperty CodeProperty = DependencyProperty.Register(
        nameof(Code), typeof(string), typeof(CodeBlock), new PropertyMetadata(string.Empty, OnContentChanged));

    /// <summary>"xaml", "xml", "csharp" / "cs" / "c#" get colouring; anything else is plain. Shown as a badge.</summary>
    public static new readonly DependencyProperty LanguageProperty = DependencyProperty.Register(
        nameof(Language), typeof(string), typeof(CodeBlock), new PropertyMetadata(string.Empty, OnContentChanged));

    /// <summary>Identifies the <see cref="ShowLineNumbers"/> dependency property.</summary>
    public static readonly DependencyProperty ShowLineNumbersProperty = DependencyProperty.Register(
        nameof(ShowLineNumbers), typeof(bool), typeof(CodeBlock), new PropertyMetadata(false));

    /// <summary>Identifies the <see cref="ShowCopyButton"/> dependency property.</summary>
    public static readonly DependencyProperty ShowCopyButtonProperty = DependencyProperty.Register(
        nameof(ShowCopyButton), typeof(bool), typeof(CodeBlock), new PropertyMetadata(true));

    private static readonly string[] CSharpKeywords = [
        "abstract", "as", "async", "await", "base", "bool", "break", "case", "catch", "char", "class", "const", "continue", "default",
        "delegate", "do", "double", "else", "enum", "event", "false", "finally", "for", "foreach", "get", "if", "in", "init", "int",
        "interface", "internal", "is", "long", "namespace", "new", "not", "null", "object", "or", "out", "override", "params",
        "partial", "private", "protected", "public", "readonly", "record", "ref", "required", "return", "sealed", "set", "static",
        "string", "struct", "switch", "this", "throw", "true", "try", "typeof", "using", "value", "void", "while", "with", "yield"
    ];

    private static readonly Regex CSharpToken = new(
        @"(?<comment>//[^\n]*|/\*.*?\*/)|(?<string>@?""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)')|(?<number>\b\d+(\.\d+)?[fdmFDM]?\b)|(?<word>\b[A-Za-z_][A-Za-z0-9_]*\b)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex XamlToken = new(
        @"(?<comment><!--.*?-->)|(?<tag></?[A-Za-z_][\w:.\-]*|/?>)|(?<attr>[A-Za-z_][\w:.\-]*(?=\s*=))|(?<string>""[^""]*""|'[^']*')",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private TextBlock? codeText;
    private TextBlock? lineNumbers;

    static CodeBlock() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CodeBlock), new FrameworkPropertyMetadata(typeof(CodeBlock)));
    }

    /// <summary>Gets or sets the code. The default is an empty string. This is a dependency property.</summary>
    public string Code {
        get => (string)GetValue(CodeProperty);
        set => SetValue(CodeProperty, value);
    }

    /// <summary>Gets or sets the language. The default is an empty string. This is a dependency property.</summary>
    public new string Language {
        get => (string)GetValue(LanguageProperty);
        set => SetValue(LanguageProperty, value);
    }

    /// <summary>Gets or sets whether to show the line numbers. The default is <see langword="false"/>. This is a dependency property.</summary>
    public bool ShowLineNumbers {
        get => (bool)GetValue(ShowLineNumbersProperty);
        set => SetValue(ShowLineNumbersProperty, value);
    }

    /// <summary>Gets or sets whether to show the copy button. The default is <see langword="true"/>. This is a dependency property.</summary>
    public bool ShowCopyButton {
        get => (bool)GetValue(ShowCopyButtonProperty);
        set => SetValue(ShowCopyButtonProperty, value);
    }

    /// <inheritdoc />
    public override void OnApplyTemplate() {
        base.OnApplyTemplate();
        codeText = GetTemplateChild(CodePartName) as TextBlock;
        lineNumbers = GetTemplateChild(LineNumbersPartName) as TextBlock;
        Render();
    }

    /// <inheritdoc />
    protected override string GetCopyText() {
        return Code ?? string.Empty;
    }

    private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
        ((CodeBlock)d).Render();
    }

    private void Render() {
        string code = (Code ?? string.Empty).Replace("\r\n", "\n").TrimEnd('\n');
        if (lineNumbers is not null) {
            int lines = code.Split('\n').Length;
            StringBuilder numbers = new();
            for (int i = 1; i <= lines; i++) {
                numbers.Append(i);
                if (i < lines) {
                    numbers.Append('\n');
                }
            }
            lineNumbers.Text = numbers.ToString();
        }
        if (codeText is null) {
            return;
        }
        codeText.Inlines.Clear();
        string language = (Language ?? string.Empty).Trim().ToLowerInvariant();
        Regex? tokens = language switch {
            "xaml" or "xml" or "html" => XamlToken,
            "csharp" or "cs" or "c#" => CSharpToken,
            _ => null
        };
        if (tokens is null) {
            codeText.Inlines.Add(new Run(code));
            return;
        }
        HashSet<string> keywords = new(CSharpKeywords, StringComparer.Ordinal);
        int position = 0;
        foreach (Match match in tokens.Matches(code)) {
            if (match.Index > position) {
                codeText.Inlines.Add(new Run(code[position..match.Index]));
            }
            string? key = Classify(match, keywords);
            Run run = new(match.Value);
            if (key is not null) {
                run.SetResourceReference(TextElement.ForegroundProperty, key);
            }
            codeText.Inlines.Add(run);
            position = match.Index + match.Length;
        }
        if (position < code.Length) {
            codeText.Inlines.Add(new Run(code[position..]));
        }
    }

    private static string? Classify(Match match, HashSet<string> keywords) {
        if (match.Groups["comment"].Success) {
            return "Muzzle.Brush.TextTertiary";
        }
        if (match.Groups["string"].Success) {
            return "Muzzle.Brush.TertiaryText";
        }
        if (match.Groups["tag"].Success) {
            return "Muzzle.Brush.PrimaryText";
        }
        if (match.Groups["attr"].Success) {
            return "Muzzle.Brush.SecondaryText";
        }
        if (match.Groups["number"].Success) {
            return "Muzzle.Brush.PrimaryText";
        }
        if (match.Groups["word"].Success) {
            if (keywords.Contains(match.Value)) {
                return "Muzzle.Brush.SecondaryText";
            }
            return char.IsUpper(match.Value[0]) ? "Muzzle.Brush.PrimaryText" : null;
        }
        return null;
    }
}

/// <summary>Inline text with a small copy button that flips to a mint checkmark after copying (e.g. an ID or a command).</summary>
public class CopyableText : CopyableControlBase {
    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(CopyableText), new PropertyMetadata(string.Empty));

    static CopyableText() {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CopyableText), new FrameworkPropertyMetadata(typeof(CopyableText)));
    }

    /// <summary>Gets or sets the text. The default is an empty string. This is a dependency property.</summary>
    public string Text {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <inheritdoc />
    protected override string GetCopyText() {
        return Text ?? string.Empty;
    }
}