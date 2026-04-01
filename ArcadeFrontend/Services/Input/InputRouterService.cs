using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.ViewModels;

/// <summary>
/// Routes application actions into the current shell / view model layer.
///
/// This keeps the window free of behavior-specific branching and creates
/// the bridge between input mapping and application orchestration.
/// </summary>
namespace ArcadeFrontend.Services.Input;

public sealed class InputRouterService
{
    /// <summary>
    /// Routes an application action into the active shell.
    /// </summary>
    public bool Route(AppAction action, ShellViewModel shellViewModel, out string? errorMessage)
    {
        errorMessage = null;

        if (action == AppAction.None)
        {
            return false;
        }

        shellViewModel.NotifyUserInteraction();

        switch (action)
        {
            case AppAction.Up:
                shellViewModel.MoveSelectionUp();
                return true;

            case AppAction.Down:
                shellViewModel.MoveSelectionDown();
                return true;

            case AppAction.Left:
                shellViewModel.MoveSelectionLeft();
                return true;

            case AppAction.Right:
                shellViewModel.MoveSelectionRight();
                return true;

            case AppAction.Select:
                return shellViewModel.SelectCurrent(out errorMessage);

            case AppAction.Back:
                shellViewModel.NavigateBack();
                return true;

            case AppAction.ToggleFavorite:
                shellViewModel.ToggleSelectedFavorite();
                return true;

            case AppAction.AdminUnlockPulse:
                return shellViewModel.RegisterAdminPulse(Key.A);

            case AppAction.ServiceMode:
                shellViewModel.OpenServiceMode();
                return true;

            case AppAction.ExitAttractMode:
                shellViewModel.TryExitAttractMode();
                return true;

            case AppAction.ResetIdleTimer:
                shellViewModel.NotifyUserInteraction();
                return true;

            default:
                return false;
        }
    }
}
