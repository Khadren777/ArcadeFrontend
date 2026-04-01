using ArcadeFrontend.Models;

/// <summary>
/// Owns screen and navigation state for the frontend.
/// </summary>
namespace ArcadeFrontend.Services.Navigation;

public sealed class NavigationStateService
{
    private ScreenType _currentScreen = ScreenType.MainMenu;
    private ScreenType _screenBeforeAttractMode = ScreenType.MainMenu;
    private string _selectedSystem = string.Empty;

    public ScreenType CurrentScreen => _currentScreen;
    public string SelectedSystem => _selectedSystem;

    public void NavigateTo(ScreenType targetScreen)
    {
        _currentScreen = targetScreen;
    }

    public void EnterAttractMode()
    {
        _screenBeforeAttractMode = _currentScreen;
        _currentScreen = ScreenType.AttractMode;
    }

    public void ExitAttractMode()
    {
        _currentScreen = _screenBeforeAttractMode;
    }

    public void OpenSystem(string systemKey)
    {
        _selectedSystem = systemKey;
        _currentScreen = ScreenType.GamesMenu;
    }

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
