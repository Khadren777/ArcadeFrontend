using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.Services.Launching;
using ArcadeFrontend.Services.Library;
using ArcadeFrontend.Services.Navigation;
using ArcadeFrontend.Services.Sessions;
using ArcadeFrontend.Services.State;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

/// <summary>
/// Main application view model for the transitional shell architecture.
/// </summary>
namespace ArcadeFrontend.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly LibraryService _libraryService;
    private readonly LaunchFlowService _launchFlowService;
    private readonly NavigationStateService _navigationStateService;
    private readonly RecentSessionService _recentSessionService;
    private readonly FavoritesService _favoritesService;
    private readonly AdminStateService _adminStateService;
    private readonly IdleStateService _idleStateService;
    private readonly MenuDefinitionService _menuDefinitionService;
    private readonly PathService _pathService;

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
            if (_titleText == value) return;
            _titleText = value;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value) return;
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
            if (_selectedIndex == safeValue) return;
            _selectedIndex = safeValue;
            OnPropertyChanged();
        }
    }

    public bool ShouldExit
    {
        get => _shouldExit;
        private set
        {
            if (_shouldExit == value) return;
            _shouldExit = value;
            OnPropertyChanged();
        }
    }

    public MainWindowViewModel(
    LibraryService libraryService,
    LaunchFlowService launchFlowService,
    NavigationStateService navigationStateService,
    RecentSessionService recentSessionService,
    FavoritesService favoritesService,
    AdminStateService adminStateService,
    IdleStateService idleStateService,
    MenuDefinitionService menuDefinitionService,
    PathService pathService)
    {
        _libraryService = libraryService;
        _launchFlowService = launchFlowService;
        _navigationStateService = navigationStateService;
        _recentSessionService = recentSessionService;
        _favoritesService = favoritesService;
        _adminStateService = adminStateService;
        _idleStateService = idleStateService;
        _menuDefinitionService = menuDefinitionService;
        _pathService = pathService;

        _idleStateService.AttractModeRequested += (_, _) =>
        {
            _navigationStateService.EnterAttractMode();
            SelectedIndex = 0;
            RenderCurrentScreen();
            RefreshSelectionState();
        };
    }

    public void Initialize()
    {
        _libraryService.Load();
        _recentSessionService.Load();
        ValidateEnvironment();
        _idleStateService.Start();
        RenderCurrentScreen();
    }

    public void NotifyUserInteraction()
    {
        if (_idleStateService.NotifyUserInteraction())
        {
            _navigationStateService.ExitAttractMode();
            SelectedIndex = 0;
            RenderCurrentScreen();
            RefreshSelectionState();
        }
    }

    public void MoveSelectionUp()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
        MoveSelection(-1);
    }

    public void MoveSelectionDown()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
        MoveSelection(1);
    }

    public void MoveSelectionLeft()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
    }

    public void MoveSelectionRight()
    {
        ToggleSelectedFavorite();
    }

    public bool SelectCurrent(out string? errorMessage)
    {
        if (_idleStateService.IsInAttractMode)
        {
            TryExitAttractMode();
            errorMessage = null;
            return true;
        }

        _idleStateService.NotifyUserInteraction();
        ActivateSelectedItem(out errorMessage);
        return true;
    }

    public void NavigateBack()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
        HandleBackOrExit();
    }

    public void ToggleSelectedFavorite()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
        ToggleFavorite();
    }

    public bool RegisterAdminPulse(Key key)
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return true; }

        _idleStateService.NotifyUserInteraction();

        if (_adminStateService.RegisterKey(key))
        {
            NavigateTo(ScreenType.AdminMenu);
            StatusText = "Admin unlocked";
            return true;
        }

        return false;
    }

    public void OpenServiceMode()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
        NavigateTo(ScreenType.AdminMenu);
    }

    public void TryExitAttractMode()
    {
        if (_idleStateService.ExitAttractMode())
        {
            _navigationStateService.ExitAttractMode();
            SelectedIndex = 0;
            RenderCurrentScreen();
            RefreshSelectionState();
        }
    }

    public bool HandleKey(Key key, out string? errorMessage)
    {
        errorMessage = null;

        if (_idleStateService.IsInAttractMode)
        {
            TryExitAttractMode();
            return true;
        }

        if (RegisterAdminPulse(key)) return true;

        switch (key)
        {
            case Key.Up: MoveSelectionUp(); return true;
            case Key.Down: MoveSelectionDown(); return true;
            case Key.Right: MoveSelectionRight(); return true;
            case Key.Enter: return SelectCurrent(out errorMessage);
            case Key.Escape: NavigateBack(); return true;
            default: return false;
        }
    }

    public void ClearExitRequest() => ShouldExit = false;

    private void MoveSelection(int direction)
    {
        if (MenuItems.Count == 0) return;

        SelectedIndex += direction;

        if (SelectedIndex < 0) SelectedIndex = MenuItems.Count - 1;
        else if (SelectedIndex >= MenuItems.Count) SelectedIndex = 0;

        RefreshSelectionState();
        UpdateStatusForCurrentScreen();
    }

    private void ActivateSelectedItem(out string? errorMessage)
    {
        errorMessage = null;
        if (MenuItems.Count == 0) return;

        var item = MenuItems[SelectedIndex];

        switch (item.Action)
        {
            case MenuAction.OpenSystems:
                NavigateTo(ScreenType.SystemsMenu);
                break;

            case MenuAction.OpenSystemGames:
                _navigationStateService.OpenSystem(item.Value);
                SelectedIndex = 0;
                RenderCurrentScreen();
                RefreshSelectionState();
                break;

            case MenuAction.LaunchGame:
            case MenuAction.LaunchRecentGame:
            case MenuAction.LaunchHiddenGame:
                if (item.Game != null)
                {
                    var result = _launchFlowService.Launch(item.Game);
                    StatusText = result.Message;
                    if (!result.Success) errorMessage = result.Message;
                }
                break;

            case MenuAction.ExitApp:
                ShouldExit = true;
                break;
        }
    }

    private void ToggleFavorite()
    {
        var game = GetSelectedGame();
        if (game == null) return;

        _favoritesService.ToggleFavorite(game, _libraryService.Games);
        RenderCurrentScreen();
    }

    private Game? GetSelectedGame()
    {
        if (SelectedIndex < 0 || SelectedIndex >= MenuItems.Count) return null;
        return MenuItems[SelectedIndex].Game;
    }

    private void NavigateTo(ScreenType screen)
    {
        _navigationStateService.NavigateTo(screen);
        SelectedIndex = 0;
        RenderCurrentScreen();
        RefreshSelectionState();
    }

    private void HandleBackOrExit()
    {
        if (_navigationStateService.CurrentScreen == ScreenType.MainMenu)
        {
            ShouldExit = true;
            return;
        }

        NavigateTo(_navigationStateService.ResolveBackTarget());
    }

    private void RenderCurrentScreen()
    {
        var screen = BuildScreenForCurrentState();

        TitleText = screen.Title;
        MenuItems.Clear();

        foreach (var item in screen.Items)
            MenuItems.Add(MenuItemViewModel.FromModel(item));

        RefreshSelectionState();
    }

    private MenuScreen BuildScreenForCurrentState()
    {
        return _navigationStateService.CurrentScreen switch
        {
            ScreenType.MainMenu => _menuDefinitionService.BuildMainMenu(),
            ScreenType.SystemsMenu => _menuDefinitionService.BuildSystemsMenu(),
            ScreenType.GamesMenu => _menuDefinitionService.BuildGamesMenu(_navigationStateService.SelectedSystem, _libraryService.Games.ToList()),
            ScreenType.RecentGamesMenu => _menuDefinitionService.BuildRecentGamesMenu(_recentSessionService.RecentGames.ToList()),
            _ => new MenuScreen()
        };
    }

    private void RefreshSelectionState()
    {
        for (int i = 0; i < MenuItems.Count; i++)
            MenuItems[i].IsSelected = i == SelectedIndex;
    }

    private void UpdateStatusForCurrentScreen()
    {
        if (MenuItems.Count == 0) return;
        StatusText = $"{TitleText} - {MenuItems[SelectedIndex].Label}";
    }

    private void ValidateEnvironment()
    {
        StatusText = "Ready";
    }
}