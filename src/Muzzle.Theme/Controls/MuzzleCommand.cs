using System;
using System.Windows.Input;

namespace Muzzle.Theme.Controls;

internal sealed class MuzzleCommand(Action<object?> execute) : ICommand {
    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    /// <inheritdoc />
    public bool CanExecute(object? parameter) {
        return true;
    }

    /// <inheritdoc />
    public void Execute(object? parameter) {
        execute(parameter);
    }
}