using System.Collections.ObjectModel;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;

namespace ArcadeFrontend.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly GameDataService _gameDataService;
        private readonly RecentGamesService _recentGamesService;
        private readonly GameLauncherService _gameLauncherService;
        private readonly AdminUnlockService _adminUnlockService;
        private readonly AttractModeService _attractModeService;
        private readonly MenuDefinitionService _menuDefinitionService;

        private readonly List<Game> _recentGames = new();
        private List<Game> _games = new();
        private List<EmulatorProfile> _emulatorProfiles = new();

        private ScreenType _currentScreen = ScreenType.MainMenu;
        private ScreenType _screenBeforeAttractMode = ScreenType.MainMenu;
        private string _selectedSystem = string.Empty;
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

        public MainWindowViewModel(
            GameDataService gameDataService,
            RecentGamesService recentGamesService,
            GameLauncherService gameLauncherService,
            AdminUnlockService adminUnlockService,
            AttractModeService attractModeService,
            MenuDefinitionService menuDefinitionService)
        {
            _gameDataService = gameDataService;
            _recentGamesService = recentGamesService;
            _gameLauncherService = gameLauncherService;
            _adminUnlockService = adminUnlockService;
            _attractModeService = attractModeService;
            _menuDefinitionService = menuDefinitionService;

            _attractModeService.IdleTimeoutReached += (_, _) => EnterAttractMode();
        }

        public void Initialize()
        {
            _emulatorProfiles = _gameDataService.LoadEmulators();
            _games = _gameDataService.LoadGames();

            _recentGames.Clear();
            _recentGames.AddRange(_recentGamesService.LoadRecentGames());

            _attractModeService.Start();
            RenderCurrentScreen();
        }

        public bool HandleKey(Key key, out string? errorMessage)
        {
            errorMessage = null;

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

            switch (key)
            {
                case Key.Up:
                    MoveSelection(-1);
                    return true;

                case Key.Down:
                    MoveSelection(1);
                    return true;

                case Key.Right:
                    ToggleFavorite();
                    return true;

                case Key.Enter:
                    ActivateSelectedItem(out errorMessage);
                    return true;

                case Key.Escape:
                    HandleBackOrExit();
                    return true;

                default:
                    return false;
            }
        }

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

        public void ClearExitRequest()
        {
            ShouldExit = false;
        }

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
                        StatusText = "Settings selected";
                    }
                    break;

                case MenuAction.OpenAdminMenu:
                    NavigateTo(ScreenType.AdminMenu);
                    break;

                case MenuAction.ExitApp:
                    ShouldExit = true;
                    break;

                case MenuAction.OpenSystemGames:
                    _selectedSystem = item.Value;
                    NavigateTo(ScreenType.GamesMenu);
                    break;

                case MenuAction.LaunchGame:
                case MenuAction.LaunchHiddenGame:
                case MenuAction.LaunchRecentGame:
                    if (item.Game != null)
                    {
                        LaunchAndTrack(item.Game, out errorMessage);
                    }
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

        private void LaunchAndTrack(Game game, out string? errorMessage)
        {
            errorMessage = null;

            try
            {
                _gameLauncherService.LaunchGame(game, _emulatorProfiles);
                AddToRecentGames(game);
                StatusText = $"Launched: {game.Title}";
            }
            catch (Exception ex)
            {
                StatusText = $"Launch failed: {game.Title}";
                errorMessage = $"Could not launch '{game.Title}'.\n\nError: {ex.Message}";
            }
        }

        private void AddToRecentGames(Game game)
        {
            _recentGames.RemoveAll(g => g.Title == game.Title && g.System == game.System);
            _recentGames.Insert(0, game);

            if (_recentGames.Count > 10)
            {
                _recentGames.RemoveAt(_recentGames.Count - 1);
            }

            _recentGamesService.SaveRecentGames(_recentGames);
        }
        private void ToggleFavorite()
        {
            if (MenuItems.Count == 0 || SelectedIndex < 0 || SelectedIndex >= MenuItems.Count)
            {
                return;
            }

            var item = MenuItems[SelectedIndex];

            if (item.Game == null)
            {
                return;
            }

            item.Game.IsFavorite = !item.Game.IsFavorite;

            StatusText = item.Game.IsFavorite
                ? $"Added to favorites: {item.Game.Title}"
                : $"Removed from favorites: {item.Game.Title}";

            _gameDataService.SaveGames(_games);

        }


        private void EnterAttractMode()
        {
            if (_isInAttractMode)
            {
                return;
            }

            _screenBeforeAttractMode = _currentScreen;
            _isInAttractMode = true;
            NavigateTo(ScreenType.AttractMode);
        }

        private void ExitAttractMode()
        {
            if (!_isInAttractMode)
            {
                return;
            }

            _isInAttractMode = false;
            NavigateTo(_screenBeforeAttractMode);
            _attractModeService.Reset();
        }

        private void HandleBackOrExit()
        {
            switch (_currentScreen)
            {
                case ScreenType.GamesMenu:
                    NavigateTo(ScreenType.SystemsMenu);
                    break;

                case ScreenType.HiddenGamesMenu:
                    NavigateTo(ScreenType.AdminMenu);
                    break;

                case ScreenType.AdminMenu:
                case ScreenType.RecentGamesMenu:
                case ScreenType.SystemsMenu:
                    NavigateTo(ScreenType.MainMenu);
                    break;

                case ScreenType.MainMenu:
                    ShouldExit = true;
                    break;
            }
        }

        private void NavigateTo(ScreenType targetScreen)
        {
            _currentScreen = targetScreen;
            SelectedIndex = 0;
            RenderCurrentScreen();
            RefreshSelectionState();
        }

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

        private MenuScreen BuildScreenForCurrentState()
        {
            return _currentScreen switch
            {
                ScreenType.MainMenu => _menuDefinitionService.BuildMainMenu(),
                ScreenType.SystemsMenu => _menuDefinitionService.BuildSystemsMenu(),
                ScreenType.GamesMenu => _menuDefinitionService.BuildGamesMenu(_selectedSystem, _games),
                ScreenType.HiddenGamesMenu => _menuDefinitionService.BuildHiddenGamesMenu(_games),
                ScreenType.AdminMenu => _menuDefinitionService.BuildAdminMenu(),
                ScreenType.RecentGamesMenu => _menuDefinitionService.BuildRecentGamesMenu(_recentGames),
                ScreenType.FavoritesMenu => _menuDefinitionService.BuildFavoritesMenu(_games),
                ScreenType.AttractMode => _menuDefinitionService.BuildAttractModeScreen(_recentGames),
                _ => new MenuScreen()
            };
        }

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

        private void UpdateStatusForCurrentScreen()
        {
            if (_currentScreen == ScreenType.AttractMode)
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
}