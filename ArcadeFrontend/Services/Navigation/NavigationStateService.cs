using ArcadeFrontend.Models;

/// <summary>
/// Owns navigation state for the frontend.
/// </summary>
namespace ArcadeFrontend.Services.Navigation;

public sealed class NavigationStateService
{
    private ScreenType _currentScreen = ScreenType.MainMenu;
    private ScreenType _screenBeforeAttractMode = ScreenType.MainMenu;
    private string _selectedSystem = string.Empty;

    /// <summary>
    /// Gets the currently active screen.
    /// </summary>
    public ScreenType CurrentScreen => _currentScreen;

    /// <summary>
    /// Gets the screen that was active before attract mode started.
    /// </summary>
    public ScreenType ScreenBeforeAttractMode => _screenBeforeAttractMode;

    /// <summary>
    /// Gets the currently selected system key.
    /// </summary>
    public string SelectedSystem => _selectedSystem;

    /// <summary>
    /// Sets the active screen.
    /// </summary>
    public void NavigateTo(ScreenType targetScreen)
    {
        _currentScreen = targetScreen;
    }

    /// <summary>
    /// Stores the current screen and enters attract mode.
    /// </summary>
    public void EnterAttractMode()
    {
        _screenBeforeAttractMode = _currentScreen;
        _currentScreen = ScreenType.AttractMode;
    }

    /// <summary>
    /// Exits attract mode and restores the previous screen.
    /// </summary>
    public void ExitAttractMode()
    {
        _currentScreen = _screenBeforeAttractMode;
    }

    /// <summary>
    /// Sets the selected system and enters the games menu.
    /// </summary>
    public void OpenSystem(string systemKey)
    {
        _selectedSystem = systemKey;
        _currentScreen = ScreenType.GamesMenu;
    }

    /// <summary>
    /// Resolves backward navigation from the current screen.
    /// </summary>
    public ScreenType ResolveBackTarget()
    {
        return _currentScreen switch
        {
            ScreenType.GamesMenu => ScreenType.SystemsMenu,
            ScreenType.HiddenGamesMenu => ScreenType.AdminMenu,
            ScreenType.AdminMenu => ScreenType.MainMenu,
            ScreenType.RecentGamesMenu => ScreenType.MainMenu,
            ScreenType.SystemsMenu => ScreenType.MainMenu,
            ScreenType.FavoritesMenu => ScreenType.MainMenu,
            _ => ScreenType.MainMenu
        };
    }
}
