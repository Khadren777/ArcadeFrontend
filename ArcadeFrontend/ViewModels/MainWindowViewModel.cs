using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.Services.Sessions;
using ArcadeFrontend.Services.Navigation;

/// <summary>
/// Primary application view model for the current transitional architecture.
///
/// This class still owns most navigation and orchestration behavior, but now
/// delegates recent-session tracking and favorite persistence to dedicated services.
/// </summary>
namespace ArcadeFrontend.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly GameDataService _gameDataService;
    private readonly RecentGamesService _recentGamesService;
    private readonly GameLauncherService _gameLauncherService;
    private readonly AdminUnlockService _adminUnlockService;
    private readonly AttractModeService _attractModeService;
    private readonly MenuDefinitionService _menuDefinitionService;
    private readonly RecentSessionService _recentSessionService;
    private readonly FavoritesService _favoritesService;
    private readonly NavigationStateService _navigationStateService;

    private List<Game> _games = new();
    private List<EmulatorProfile> _emulatorProfiles = new();

    private bool _isInAttractMode;
    private bool _shouldExit;
    private int _selectedIndex;
    private string _titleText = "ARCADE FRONTEND";
    private string _statusText = "Ready";

    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

    public string TitleText
    {
        get => _titleText;
        private set
        {
            if (_titleText == value)
            {
                return;
            }

            _titleText = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            int safeValue = value < 0 ? 0 : value;

            if (_selectedIndex == safeValue)
            {
                return;
            }

            _selectedIndex = safeValue;
            OnPropertyChanged();
        }
    }

    public bool ShouldExit
    {
        get => _shouldExit;
        private set
        {
            if (_shouldExit == value)
            {
                return;
            }

            _shouldExit = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Initializes the view model with application services.
    /// </summary>
    public MainWindowViewModel(
        GameDataService gameDataService,
        RecentGamesService recentGamesService,
        GameLauncherService gameLauncherService,
        AdminUnlockService adminUnlockService,
        AttractModeService attractModeService,
        MenuDefinitionService menuDefinitionService,
        RecentSessionService recentSessionService,
        FavoritesService favoritesService,
        NavigationStateService navigationStateService)
    {
        _gameDataService = gameDataService;
        _recentGamesService = recentGamesService;
        _gameLauncherService = gameLauncherService;
        _adminUnlockService = adminUnlockService;
        _attractModeService = attractModeService;
        _menuDefinitionService = menuDefinitionService;
        _recentSessionService = recentSessionService;
        _favoritesService = favoritesService;

        _attractModeService.IdleTimeoutReached += (_, _) => EnterAttractMode();
    }

    /// <summary>
    /// Loads application data and renders the initial screen.
    /// </summary>
    public void Initialize()
    {
        _emulatorProfiles = _gameDataService.LoadEmulators();
        _games = _gameDataService.LoadGames();

        _recentSessionService.Load();

        _attractModeService.Start();
        RenderCurrentScreen();
    }

    /// <summary>
    /// Resets idle tracking in response to user interaction.
    /// </summary>
    public void NotifyUserInteraction()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
    }

    /// <summary>
    /// Moves the current selection upward.
    /// </summary>
    public void MoveSelectionUp()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
        MoveSelection(-1);
    }

    /// <summary>
    /// Moves the current selection downward.
    /// </summary>
    public void MoveSelectionDown()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
        MoveSelection(1);
    }

    /// <summary>
    /// Handles leftward navigation input.
    /// </summary>
    public void MoveSelectionLeft()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
    }

    /// <summary>
    /// Handles rightward navigation input.
    /// </summary>
    public void MoveSelectionRight()
    {
        ToggleSelectedFavorite();
    }

    /// <summary>
    /// Activates the currently selected item.
    /// </summary>
    public bool SelectCurrent(out string? errorMessage)
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            errorMessage = null;
            return true;
        }

        _attractModeService.Reset();
        ActivateSelectedItem(out errorMessage);
        return true;
    }

    /// <summary>
    /// Navigates backward or exits when appropriate.
    /// </summary>
    public void NavigateBack()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
        HandleBackOrExit();
    }

    /// <summary>
    /// Toggles favorite state for the currently selected game.
    /// </summary>
    public void ToggleSelectedFavorite()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
        ToggleFavorite();
    }

    /// <summary>
    /// Registers one admin unlock input pulse.
    /// </summary>
    public bool RegisterAdminPulse(Key key)
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return true;
        }

        _attractModeService.Reset();

        if (_adminUnlockService.TrackKey(key))
        {
            NavigateTo(ScreenType.AdminMenu);
            StatusText = "Admin unlocked";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Opens service/admin mode.
    /// </summary>
    public void OpenServiceMode()
    {
        if (_isInAttractMode)
        {
            ExitAttractMode();
            return;
        }

        _attractModeService.Reset();
        NavigateTo(ScreenType.AdminMenu);
    }

    /// <summary>
    /// Exits attract mode when active.
    /// </summary>
    public void TryExitAttractMode()
    {
        ExitAttractMode();
    }

    /// <summary>
    /// Handles raw keyboard input for the current transitional input path.
    /// </summary>
    public bool HandleKey(Key key, out string? errorMessage)
    {
        errorMessage = null;

        if (_isInAttractMode)
        {
            ExitAttractMode();
            return true;
        }

        if (RegisterAdminPulse(key))
        {
            return true;
        }

        switch (key)
        {
            case Key.Up:
                MoveSelectionUp();
                return true;

            case Key.Down:
                MoveSelectionDown();
                return true;

            case Key.Right:
                MoveSelectionRight();
                return true;

            case Key.Enter:
                return SelectCurrent(out errorMessage);

            case Key.Escape:
                NavigateBack();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Clears a pending exit request.
    /// </summary>
    public void ClearExitRequest()
    {
        ShouldExit = false;
    }

    /// <summary>
    /// Ensures the current selection index is valid.
    /// </summary>
    private void EnsureValidSelection()
    {
        if (MenuItems.Count == 0)
        {
            SelectedIndex = 0;
            return;
        }

        if (SelectedIndex < 0 || SelectedIndex >= MenuItems.Count)
        {
            SelectedIndex = 0;
        }
    }

    /// <summary>
    /// Moves the selected item index and refreshes menu state.
    /// </summary>
    private void MoveSelection(int direction)
    {
        if (MenuItems.Count == 0)
        {
            return;
        }

        SelectedIndex += direction;

        if (SelectedIndex < 0)
        {
            SelectedIndex = MenuItems.Count - 1;
        }
        else if (SelectedIndex >= MenuItems.Count)
        {
            SelectedIndex = 0;
        }

        RefreshSelectionState();
        UpdateStatusForCurrentScreen();
    }

    /// <summary>
    /// Activates the selected menu item.
    /// </summary>
    private void ActivateSelectedItem(out string? errorMessage)
    {
        errorMessage = null;

        if (MenuItems.Count == 0 || SelectedIndex < 0 || SelectedIndex >= MenuItems.Count)
        {
            return;
        }

        MenuItemViewModel item = MenuItems[SelectedIndex];

        switch (item.Action)
        {
            case MenuAction.None:
                break;

            case MenuAction.Play:
                StatusText = "Play selected";
                break;

            case MenuAction.OpenSystems:
                NavigateTo(ScreenType.SystemsMenu);
                break;

            case MenuAction.OpenFavorites:
                NavigateTo(ScreenType.FavoritesMenu);
                break;

            case MenuAction.OpenRecentGames:
                NavigateTo(ScreenType.RecentGamesMenu);
                break;

            case MenuAction.OpenHiddenGames:
                if (_adminUnlockService.IsUnlocked)
                {
                    NavigateTo(ScreenType.HiddenGamesMenu);
                }
                else
                {
                    StatusText = "Hidden Games locked";
                }
                break;

            case MenuAction.OpenSettings:
                if (_adminUnlockService.IsUnlocked)
                {
                    NavigateTo(ScreenType.AdminMenu);
                }
                else
                {
                    StatusText = "Settings locked";
                }
                break;

            case MenuAction.OpenAdminMenu:
                if (_adminUnlockService.IsUnlocked)
                {
                    NavigateTo(ScreenType.AdminMenu);
                }
                else
                {
                    StatusText = "Admin locked";
                }
                break;

            case MenuAction.OpenSystemGames:
                _navigationStateService.OpenSystem(item.Value);
                SelectedIndex = 0;
                RenderCurrentScreen();
                RefreshSelectionState();
                break;

            case MenuAction.LaunchGame:
            case MenuAction.LaunchHiddenGame:
            case MenuAction.LaunchRecentGame:
                if (item.Game != null)
                {
                    LaunchAndTrack(item.Game, out errorMessage);
                }
                break;

            case MenuAction.ToggleFavorite:
                ToggleFavorite();
                break;

            case MenuAction.ExitApp:
                ShouldExit = true;
                break;

            case MenuAction.RescanLibrary:
                _games = _gameDataService.LoadGames();
                _emulatorProfiles = _gameDataService.LoadEmulators();
                StatusText = "Library rescanned";
                RenderCurrentScreen();
                break;

            case MenuAction.InputTest:
                StatusText = "Input Test selected";
                break;

            case MenuAction.BackToMain:
                NavigateTo(ScreenType.MainMenu);
                break;

            case MenuAction.BackToSystems:
                NavigateTo(ScreenType.SystemsMenu);
                break;

            case MenuAction.BackToAdmin:
                NavigateTo(ScreenType.AdminMenu);
                break;
        }
    }

    /// <summary>
    /// Launches a game and records a successful launch in recents.
    /// </summary>
    private void LaunchAndTrack(Game game, out string? errorMessage)
    {
        errorMessage = null;

        try
        {
            _gameLauncherService.LaunchGame(game, _emulatorProfiles);
            _recentSessionService.RecordLaunch(game);
            StatusText = $"Launched: {game.Title}";
        }
        catch (Exception ex)
        {
            StatusText = $"Launch failed: {game.Title}";
            errorMessage = $"Could not launch '{game.Title}'.\n\nError: {ex.Message}";
        }
    }

    /// <summary>
    /// Toggles favorite state for the selected game and persists the change.
    /// </summary>
    private void ToggleFavorite()
    {
        Game? selectedGame = GetSelectedGame();

        if (selectedGame is null)
        {
            StatusText = "No game selected";
            return;
        }

        _favoritesService.ToggleFavorite(selectedGame, _games);
        StatusText = selectedGame.IsFavorite
            ? $"Added to favorites: {selectedGame.Title}"
            : $"Removed from favorites: {selectedGame.Title}";

        RenderCurrentScreen();
    }

    /// <summary>
    /// Returns the currently selected game when one is available.
    /// </summary>
    private Game? GetSelectedGame()
    {
        if (SelectedIndex < 0 || SelectedIndex >= MenuItems.Count)
        {
            return null;
        }

        return MenuItems[SelectedIndex].Game;
    }

    /// <summary>
    /// Enters attract mode.
    /// </summary>
    private void EnterAttractMode()
    {
        if (_isInAttractMode)
        {
            return;
        }

        _isInAttractMode = true;
        _navigationStateService.EnterAttractMode();
        SelectedIndex = 0;
        RenderCurrentScreen();
        RefreshSelectionState();
    }

    /// <summary>
    /// Exits attract mode and restores the previous screen.
    /// </summary>
    private void ExitAttractMode()
    {
        if (!_isInAttractMode)
        {
            return;
        }

        _isInAttractMode = false;
        _navigationStateService.ExitAttractMode();
        SelectedIndex = 0;
        RenderCurrentScreen();
        RefreshSelectionState();
        _attractModeService.Reset();
    }

    /// <summary>
    /// Handles back navigation or application exit.
    /// </summary>
    private void HandleBackOrExit()
    {
        if (_navigationStateService.CurrentScreen == ScreenType.MainMenu)
        {
            ShouldExit = true;
            return;
        }

        NavigateTo(_navigationStateService.ResolveBackTarget());
    }

    /// <summary>
    /// Changes the active screen and refreshes menu state.
    /// </summary>
    private void NavigateTo(ScreenType targetScreen)
    {
        _navigationStateService.NavigateTo(targetScreen);
        SelectedIndex = 0;
        RenderCurrentScreen();
        RefreshSelectionState();
    }

    /// <summary>
    /// Rebuilds the visible menu for the current screen.
    /// </summary>
    private void RenderCurrentScreen()
    {
        MenuScreen screen = BuildScreenForCurrentState();

        TitleText = screen.Title;
        MenuItems.Clear();

        foreach (MenuItemModel item in screen.Items)
        {
            MenuItems.Add(MenuItemViewModel.FromModel(item));
        }

        EnsureValidSelection();
        RefreshSelectionState();
        UpdateStatusForCurrentScreen();
    }

    /// <summary>
    /// Builds the menu definition for the current screen.
    /// </summary>
    private MenuScreen BuildScreenForCurrentState()
    {
        return _navigationStateService.CurrentScreen switch
        {
            ScreenType.MainMenu => _menuDefinitionService.BuildMainMenu(),
            ScreenType.SystemsMenu => _menuDefinitionService.BuildSystemsMenu(),
            ScreenType.GamesMenu => _menuDefinitionService.BuildGamesMenu(_navigationStateService.SelectedSystem, _games),
            ScreenType.HiddenGamesMenu => _menuDefinitionService.BuildHiddenGamesMenu(_games),
            ScreenType.AdminMenu => _menuDefinitionService.BuildAdminMenu(),
            ScreenType.RecentGamesMenu => _menuDefinitionService.BuildRecentGamesMenu(_recentSessionService.RecentGames),
            ScreenType.FavoritesMenu => _menuDefinitionService.BuildFavoritesMenu(_games),
            ScreenType.AttractMode => _menuDefinitionService.BuildAttractModeScreen(_recentSessionService.RecentGames),
            _ => new MenuScreen()
        };
    }

    /// <summary>
    /// Updates which menu item is marked as selected.
    /// </summary>
    private void RefreshSelectionState()
    {
        if (MenuItems.Count == 0)
        {
            return;
        }

        EnsureValidSelection();

        for (int i = 0; i < MenuItems.Count; i++)
        {
            MenuItems[i].IsSelected = i == SelectedIndex;
        }
    }

    /// <summary>
    /// Updates the status text based on the current screen and selection.
    /// </summary>
    private void UpdateStatusForCurrentScreen()
    {
        if (_navigationStateService.CurrentScreen == ScreenType.AttractMode)
        {
            StatusText = "Press any key to return";
            return;
        }

        if (MenuItems.Count == 0)
        {
            StatusText = "No items available";
            return;
        }

        EnsureValidSelection();
        StatusText = $"{TitleText} - Selected: {MenuItems[SelectedIndex].Label}";
    }
}