using System.Windows.Input;

/// <summary>
/// Transitional root view model for the application.
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

    public void MoveSelectionUp()
    {
        Main.MoveSelectionUp();
    }

    public void MoveSelectionDown()
    {
        Main.MoveSelectionDown();
    }

    public void MoveSelectionLeft()
    {
        Main.MoveSelectionLeft();
    }

    public void MoveSelectionRight()
    {
        Main.MoveSelectionRight();
    }

    public bool SelectCurrent(out string? errorMessage)
    {
        return Main.SelectCurrent(out errorMessage);
    }

    public void NavigateBack()
    {
        Main.NavigateBack();
    }

    public void ToggleSelectedFavorite()
    {
        Main.ToggleSelectedFavorite();
    }

    public bool RegisterAdminPulse(Key key)
    {
        return Main.RegisterAdminPulse(key);
    }

    public void OpenServiceMode()
    {
        Main.OpenServiceMode();
    }

    public void TryExitAttractMode()
    {
        Main.TryExitAttractMode();
    }
}