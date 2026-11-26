using System;

namespace Muzzle.Theme.Controls;

/// <summary>The semantic flavour of a growl toast.</summary>
public enum GrowlType {
    /// <summary>An informational toast that dismisses itself after its wait time.</summary>
    Info,
    /// <summary>A success toast that dismisses itself after its wait time.</summary>
    Success,
    /// <summary>A warning toast that dismisses itself after its wait time.</summary>
    Warning,
    /// <summary>An error toast that dismisses itself after its wait time.</summary>
    Error,
    /// <summary>An error that stays until the user dismisses it.</summary>
    Fatal,
    /// <summary>A question with Confirm / Cancel pills; the answer goes to <see cref="GrowlInfo.Callback"/>.</summary>
    Ask
}

/// <summary>Everything a growl toast can be configured with. Pass it to <see cref="Growl.Show(GrowlInfo)"/>.</summary>
public sealed class GrowlInfo {
    /// <summary>Gets or sets the toast text.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Optional heading; defaults to the type name ("Info", "Success"…).</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the kind of toast: its colour, icon and whether it dismisses itself.</summary>
    public GrowlType Type { get; set; } = GrowlType.Info;

    /// <summary>Targets the container registered with this <c>mz:Growl.Token</c>. Null uses the active window's container.</summary>
    public string? Token { get; set; }

    /// <summary>How long the toast stays before dismissing itself. Null picks a default for the type.</summary>
    public TimeSpan? WaitTime { get; set; }

    /// <summary>Keeps the toast until the user closes it. Ask and Fatal toasts always stay open.</summary>
    public bool StaysOpen { get; set; }

    /// <summary>Gets or sets whether the × button is shown. The default is <see langword="true"/>.</summary>
    public bool ShowCloseButton { get; set; } = true;

    /// <summary>Shows the toast in the desktop-corner <see cref="GrowlWindow"/> even if an in-window container exists.</summary>
    public bool IsGlobal { get; set; }

    /// <summary>Gets or sets the confirm pill's text for <see cref="GrowlType.Ask"/> toasts.</summary>
    public string ConfirmText { get; set; } = MuzzleStrings.Get("Growl.Yes", "Yes");

    /// <summary>Gets or sets the cancel pill's text for <see cref="GrowlType.Ask"/> toasts.</summary>
    public string CancelText { get; set; } = MuzzleStrings.Get("Growl.No", "No");

    /// <summary>For <see cref="GrowlType.Ask"/>: called with true (confirm) or false (cancel / dismissed).</summary>
    public Action<bool>? Callback { get; set; }
}