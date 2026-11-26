using System;
using System.Windows;
using System.Windows.Threading;

namespace Muzzle.Theme;

/// <summary>
/// The whole Muzzle theme, its base theme and its accent in one declarative line:
/// <code>
/// &lt;Application.Resources&gt;
///     &lt;mz:MuzzleThemeResourceDictionary Theme="System" Accent="Lavender" /&gt;
/// &lt;/Application.Resources&gt;
/// </code>
/// It merges Muzzle.xaml followed by the chosen palette and accent, so the first frame already has the right colours.
/// Your own resources can go inside it as usual. <see cref="MuzzleThemeManager"/> takes over at runtime (with
/// <see cref="MuzzleTheme.System"/> it keeps following Windows).
/// </summary>
public class MuzzleThemeResourceDictionary : ResourceDictionary {
    private readonly ResourceDictionary muzzle = Create("Muzzle.xaml");
    private MuzzleTheme theme = MuzzleTheme.Light;
    private MuzzleAccent accent = MuzzleAccent.Coral;
    private bool syncQueued;

    /// <summary>Initializes a new instance of the <see cref="MuzzleThemeResourceDictionary"/> class.</summary>
    public MuzzleThemeResourceDictionary() {
        Rebuild();
    }

    /// <summary>Light, Dark or System (default Light).</summary>
    public MuzzleTheme Theme {
        get => theme;
        set {
            theme = value;
            Rebuild();
        }
    }

    /// <summary>Coral (default), Lavender, Mint or Peach. Custom accents are applied in code with ApplyCustomAccent.</summary>
    public MuzzleAccent Accent {
        get => accent;
        set {
            accent = (value == MuzzleAccent.Custom) ? MuzzleAccent.Coral : value;
            Rebuild();
        }
    }

    private void Rebuild() {
        bool dark = (theme == MuzzleTheme.Dark) || ((theme == MuzzleTheme.System) && MuzzleThemeManager.IsSystemDark());
        MergedDictionaries.Clear();
        MergedDictionaries.Add(muzzle);
        MergedDictionaries.Add(Create(dark ? "Muzzle.Dark.xaml" : "Muzzle.Light.xaml"));
        MergedDictionaries.Add(MuzzleThemeManager.CreateAccentDictionary(accent, dark));
        QueueManagerSync();
    }

    // Once the application is running, let the manager adopt the choice (and start watching Windows for System).
    private void QueueManagerSync() {
        if (syncQueued || (Application.Current is not Application application)) {
            return;
        }
        syncQueued = true;
        application.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => {
            syncQueued = false;
            if (theme == MuzzleTheme.System) {
                MuzzleThemeManager.ApplyTheme(MuzzleTheme.System);
            }
        }));
    }

    private static ResourceDictionary Create(string file) {
        return new ResourceDictionary { Source = new Uri($"pack://application:,,,/Muzzle.Theme;component/Themes/{file}", UriKind.Absolute) };
    }
}