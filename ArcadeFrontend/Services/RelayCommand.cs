using System;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace ArcadeFrontend.Services
{
    /// <summary>
    /// Backward-compatible wrapper for CommunityToolkit.Mvvm's RelayCommand.
    /// Use CommunityToolkit.Mvvm.Input.RelayCommand directly for new code.
    /// </summary>
    [Obsolete("Use CommunityToolkit.Mvvm.Input.RelayCommand directly", false)]
    public sealed class RelayCommand : ICommand
    {
        private readonly CommunityToolkit.Mvvm.Input.RelayCommand _innerCommand;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _innerCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(
                execute ?? throw new ArgumentNullException(nameof(execute)),
                canExecute ?? (() => true)
            );
        }

        public bool CanExecute(object? parameter) => _innerCommand.CanExecute(parameter);

        public void Execute(object? parameter) => _innerCommand.Execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => _innerCommand.CanExecuteChanged += value;
            remove => _innerCommand.CanExecuteChanged -= value;
        }

        public void RaiseCanExecuteChanged() => _innerCommand.NotifyCanExecuteChanged();
    }
}

