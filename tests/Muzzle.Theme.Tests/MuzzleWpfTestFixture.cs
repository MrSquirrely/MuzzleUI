using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

using Xunit;

namespace Muzzle.Theme.Tests;

/// <summary>Every test that touches WPF objects, application resources or the theme manager shares this collection.</summary>
[CollectionDefinition(Name)]
public sealed class WpfCollection : ICollectionFixture<MuzzleWpfTestFixture> {
    public const string Name = "WPF";
}

/// <summary>
/// Boots one <see cref="Application"/> for the whole run on a dedicated STA "UI thread" (or adopts an existing one),
/// merges <c>Themes/Muzzle.xaml</c> exactly as an app would, and runs test bodies on that thread through
/// <see cref="Run"/> / <see cref="RunAsync"/>. Keeping every WPF object on one dispatcher mirrors a real app:
/// <see cref="MuzzleThemeManager"/>, overlay hosts and shared resources all expect the application's thread.
/// It also records binding errors and unhandled dispatcher exceptions so tests can assert there were none.
/// </summary>
public sealed class MuzzleWpfTestFixture : IDisposable {
    public const string ThemeUri = "pack://application:,,,/Muzzle.Theme;component/Themes/Muzzle.xaml";

    private readonly Thread? ownedThread;
    private readonly BindingErrorListener bindingErrors = new();
    private readonly List<Exception> unhandled = [];
    private readonly Lock unhandledLock = new();

    public MuzzleWpfTestFixture() {
        if (Application.Current is Application existing) {
            Dispatcher = existing.Dispatcher;
            Run(() => Initialise(existing));
            return;
        }
        Dispatcher? created = null;
        Exception? failure = null;
        using ManualResetEventSlim ready = new();
        ownedThread = new Thread(() => {
            try {
                Application application = Application.Current ?? new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                created = application.Dispatcher;
                Initialise(application);
            }
            catch (Exception exception) {
                failure = exception;
            }
            finally {
                ready.Set();
            }
            if (failure is null) {
                Dispatcher.Run();
            }
        }) {
            IsBackground = true,
            Name = "Muzzle test UI thread"
        };
        ownedThread.SetApartmentState(ApartmentState.STA);
        ownedThread.Start();
        ready.Wait();
        if (failure is not null) {
            throw new InvalidOperationException("Could not start the WPF test application.", failure);
        }
        Dispatcher = created!;
    }

    /// <summary>The application's (UI thread's) dispatcher.</summary>
    public Dispatcher Dispatcher { get; }

    /// <summary>Runs <paramref name="body"/> on the UI thread; exceptions (including failed asserts) propagate.</summary>
    public void Run(Action body) {
        Dispatcher.Invoke(body);
    }

    public T Run<T>(Func<T> body) {
        return Dispatcher.Invoke(body);
    }

    /// <summary>Runs an async body on the UI thread; awaits inside it resume on the UI thread.</summary>
    public Task RunAsync(Func<Task> body) {
        return Dispatcher.InvokeAsync(body).Task.Unwrap();
    }

    /// <summary>Back to Light + Coral (call from the UI thread, usually in a finally block).</summary>
    public static void ResetTheme() {
        MuzzleThemeManager.ApplyTheme(MuzzleTheme.Light);
        MuzzleThemeManager.ApplyAccent(MuzzleAccent.Coral);
    }

    /// <summary>
    /// Shows <paramref name="content"/> in a small off-screen window that never takes focus. Close it in a finally
    /// block (or use <see cref="HostWindow"/>).
    /// </summary>
    public static Window ShowOffScreen(object content, double width = 900, double height = 700, Window? window = null) {
        window ??= new Window();
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -20000;
        window.Top = -20000;
        window.Width = width;
        window.Height = height;
        window.ShowActivated = false;
        window.ShowInTaskbar = false;
        window.Content = content;
        window.Show();
        window.UpdateLayout();
        return window;
    }

    /// <summary>Lets the dispatcher run (layout, Loaded, data binding, animations) for a while.</summary>
    public static Task WaitAsync(int milliseconds) {
        return Task.Delay(milliseconds);
    }

    /// <summary>Polls <paramref name="condition"/> on the UI thread until it holds or the timeout passes.</summary>
    public static async Task<bool> WaitForAsync(Func<bool> condition, int timeoutMilliseconds = 3000) {
        Stopwatch watch = Stopwatch.StartNew();
        while (watch.ElapsedMilliseconds < timeoutMilliseconds) {
            if (condition()) {
                return true;
            }
            await Task.Delay(20);
        }
        return condition();
    }

    /// <summary>Lets pending work at Loaded / DataBind priority (and below Input) finish.</summary>
    public static void Flush() {
        Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(static () => { }));
    }

    /// <summary>Clears recorded binding errors and unhandled exceptions (call at the start of a test).</summary>
    public void ResetDiagnostics() {
        bindingErrors.Clear();
        lock (unhandledLock) {
            unhandled.Clear();
        }
    }

    /// <summary>Binding errors recorded since the last <see cref="ResetDiagnostics"/>.</summary>
    public IReadOnlyList<string> BindingErrors => bindingErrors.Snapshot();

    /// <summary>Exceptions that escaped to the dispatcher since the last <see cref="ResetDiagnostics"/>.</summary>
    public IReadOnlyList<Exception> UnhandledExceptions {
        get {
            lock (unhandledLock) {
                return [.. unhandled];
            }
        }
    }

    /// <summary>Asserts no binding errors and no unhandled dispatcher exceptions were recorded.</summary>
    public void AssertClean() {
        Assert.True(BindingErrors.Count == 0, "Binding errors:\n" + string.Join("\n", BindingErrors));
        Assert.True(UnhandledExceptions.Count == 0, "Unhandled dispatcher exceptions:\n" + string.Join("\n", UnhandledExceptions));
    }

    public void Dispose() {
        if (ownedThread is null) {
            return;
        }
        try {
            Dispatcher.Invoke(() => {
                foreach (Window window in Application.Current.Windows) {
                    window.Close();
                }
                Application.Current.Shutdown();
            });
        }
        catch (TaskCanceledException) {
        }
        Dispatcher.InvokeShutdown();
        ownedThread.Join(TimeSpan.FromSeconds(5));
    }

    private void Initialise(Application application) {
        bool hasTheme = false;
        foreach (ResourceDictionary dictionary in application.Resources.MergedDictionaries) {
            hasTheme |= dictionary.Source?.OriginalString.EndsWith("Themes/Muzzle.xaml", StringComparison.OrdinalIgnoreCase) == true;
        }
        if (!hasTheme) {
            application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri(ThemeUri, UriKind.Absolute) });
        }
        application.DispatcherUnhandledException += (_, e) => {
            lock (unhandledLock) {
                unhandled.Add(e.Exception);
            }
            e.Handled = true;
        };
        PresentationTraceSources.Refresh();
        PresentationTraceSources.DataBindingSource.Listeners.Add(bindingErrors);
        PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
        // Theme state starts at the defaults regardless of what ran before.
        ResetTheme();
    }

    private sealed class BindingErrorListener : TraceListener {
        private readonly List<string> errors = [];
        private readonly Lock gate = new();

        public void Clear() {
            lock (gate) {
                errors.Clear();
            }
        }

        public IReadOnlyList<string> Snapshot() {
            lock (gate) {
                return [.. errors];
            }
        }

        public override void Write(string? message) {
        }

        public override void WriteLine(string? message) {
            if (string.IsNullOrEmpty(message)) {
                return;
            }
            lock (gate) {
                errors.Add(message);
            }
        }
    }
}