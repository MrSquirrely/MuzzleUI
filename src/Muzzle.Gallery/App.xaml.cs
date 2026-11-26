using System;
using System.Windows;

using Muzzle.Gallery.Diagnostics;

namespace Muzzle.Gallery;

public partial class App : Application {
    protected override void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);
        string? snapshotDirectory = ReadOption(e.Args, "--snapshot");
        if (snapshotDirectory is not null) {
            SnapshotRunner.Run(this, snapshotDirectory);
            return;
        }
        MainWindow window = new();
        window.Show();
    }

    private static string? ReadOption(string[] args, string name) {
        int index = Array.IndexOf(args, name);
        return (index >= 0) && (index + 1 < args.Length) ? args[index + 1] : null;
    }
}