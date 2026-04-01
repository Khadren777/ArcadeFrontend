/// <summary>
/// Transitional root view model for the application.
///
/// Wraps MainWindowViewModel so new shell-level orchestration can be added
/// without forcing a full rewrite of the existing app behavior in one pass.
/// </summary>
namespace ArcadeFrontend.ViewModels;

public sealed class ShellViewModel : ViewModelBase
{

    /// <summary>
    /// Initializes the shell with the existing main view model.
    /// </summary>
    public ShellViewModel(MainWindowViewModel main)
    {
        Main = main;
    }

    public MainWindowViewModel Main { get; }

    public void NotifyUserInteraction()
    {
        Main.NotifyUserInteraction();
    }

    /// <summary>
    /// Moves selection upward.
    /// </summary>
    public void MoveSelectionUp()
    {
        Main.MoveSelectionUp();
    }

    /// <summary>
    /// Moves selection downward.
    /// </summary>
    public void MoveSelectionDown()
    {
        Main.MoveSelectionDown();
    }

    /// <summary>
    /// Handles leftward navigation input.
    /// </summary>
    public void MoveSelectionLeft()
    {
        Main.MoveSelectionLeft();
    }

    /// <summary>
    /// Handles rightward navigation input.
    /// </summary>
    public void MoveSelectionRight()
    {
        Main.MoveSelectionRight();
    }

    /// <summary>
    /// Activates the currently selected item.
    /// </summary>
    public bool SelectCurrent(out string? errorMessage)
    {
        return Main.SelectCurrent(out errorMessage);
    }

    /// <summary>
    /// Navigates backward or exits when appropriate.
    /// </summary>
    public void NavigateBack()
    {
        Main.NavigateBack();
    }

    /// <summary>
    /// Toggles favorite state for the selected item.
    /// </summary>
    public void ToggleSelectedFavorite()
    {
        Main.ToggleSelectedFavorite();
    }

    /// <summary>
    /// Registers one admin unlock pulse.
    /// </summary>
    public bool RegisterAdminPulse(System.Windows.Input.Key key)
    {
        return Main.RegisterAdminPulse(key);
    }

    /// <summary>
    /// Opens service/admin mode.
    /// </summary>
    public void OpenServiceMode()
    {
        Main.OpenServiceMode();
    }

    /// <summary>
    /// Exits attract mode when active.
    /// </summary>
    public void TryExitAttractMode()
    {
        Main.TryExitAttractMode();
    }
}