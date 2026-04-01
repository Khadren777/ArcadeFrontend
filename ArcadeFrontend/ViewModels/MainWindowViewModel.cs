using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;

/// <summary>
/// Primary application view model for the current transitional architecture.
///
/// This class still owns most navigation and orchestration behavior, but now
/// exposes public wrapper methods so input handling can be gradually moved
/// out of raw key-based logic and into action-based routing.
/// </summary>
namespace ArcadeFrontend.ViewModels { 

public class MainWindowViewModel : ViewModelBase
{
        private readonly GameDataService _gameDataService;
        private readonly RecentGamesService _recentGamesService;
        private readonly GameLauncherService _gameLauncherService;
        private readonly AdminUnlockService _adminUnlockService;
        private readonly AttractModeService _attractModeService;
        private readonly MenuDefinitionService _menuDefinitionService;
        private readonly PathService _pathService;

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
            MenuDefinitionService menuDefinitionService,
            PathService pathService)
        {
            _gameDataService = gameDataService;
            _recentGamesService = recentGamesService;
            _gameLauncherService = gameLauncherService;
            _adminUnlockService = adminUnlockService;
            _attractModeService = attractModeService;
            _menuDefinitionService = menuDefinitionService;
            _pathService = pathService;


            _attractModeService.IdleTimeoutReached += (_, _) => EnterAttractMode();
        }

        public void Initialize()
        {
            _emulatorProfiles = _gameDataService.LoadEmulators();
            _games = _gameDataService.LoadGames();

            _recentGames.Clear();
            _recentGames.AddRange(_recentGamesService.LoadRecentGames());

            ValidateEnvironment();

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
        /// Handles leftward input.
        ///
        /// This is currently a no-op placeholder so the input layer can standardize
        /// on directional actions before horizontal navigation is fully implemented.
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
        /// Handles rightward input.
        ///
        /// In the current transitional UX, right maps to favorite toggle.
        /// This preserves existing cabinet/keyboard behavior until a fuller grid
        /// or horizontal navigation model is introduced.
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
        /// Navigates backward or exits the application when appropriate.
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
        /// Opens service or admin mode when available.
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
        /// Exits attract mode if it is active.
        /// </summary>
        public void TryExitAttractMode()
        {
            ExitAttractMode();
        }

    private void ValidateEnvironment()
        {
            List<string> issues = new();

            foreach (var emulator in _emulatorProfiles)
            {
                string resolvedPath = _pathService.Resolve(emulator.ExecutablePath);

                if (!File.Exists(resolvedPath))
                {
                    issues.Add($"Missing emulator: {emulator.DisplayName}");
                }
            }

            if (issues.Count > 0)
            {
                StatusText = $"Missing dependencies: {string.Join(", ", issues)}";
                return;
            }

            StatusText = "Ready";
        }

        private void UpdateLaunchAvailability()
        {
            foreach (var item in MenuItems)
            {
                item.IsLaunchAvailable = true;
                item.LaunchIssue = string.Empty;

                if (item.Game == null)
                {
                    continue;
                }

                Game game = item.Game;

                if (game.LaunchType == LaunchType.Emulator)
                {
                    EmulatorProfile? emulator = _emulatorProfiles.FirstOrDefault(e => e.Key == game.EmulatorKey);

                    if (emulator == null)
                    {
                        item.IsLaunchAvailable = false;
                        item.LaunchIssue = "Missing emulator profile";
                        continue;
                    }

                    string resolvedExecutablePath = _pathService.Resolve(emulator.ExecutablePath);

                    if (!File.Exists(resolvedExecutablePath))
                    {
                        item.IsLaunchAvailable = false;
                        item.LaunchIssue = "Missing emulator";
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(game.RomPath))
                    {
                        item.IsLaunchAvailable = false;
                        item.LaunchIssue = "Missing ROM path";
                        continue;
                    }

                    string resolvedRomPath = _pathService.Resolve(game.RomPath);

                    if (!File.Exists(resolvedRomPath) && !Directory.Exists(resolvedRomPath))
                    {
                        item.IsLaunchAvailable = false;
                        item.LaunchIssue = "Missing ROM";
                    }
                }
                else if (game.LaunchType == LaunchType.Native)
                {
                    if (string.IsNullOrWhiteSpace(game.LaunchTarget))
                    {
                        item.IsLaunchAvailable = false;
                        item.LaunchIssue = "Missing launch target";
                        continue;
                    }

                    string resolvedLaunchTarget = _pathService.Resolve(game.LaunchTarget);

                    if (!File.Exists(resolvedLaunchTarget))
                    {
                        item.IsLaunchAvailable = false;
                        item.LaunchIssue = "Missing game executable";
                    }
                }
            }
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
            if (!IsGameLaunchable(game, out string issue))
            {
                StatusText = $"{game.Title} unavailable";
                errorMessage = issue;
                return;
            }

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

        private bool IsGameLaunchable(Game game, out string issue)
        {
            issue = string.Empty;

            if (game.LaunchType == LaunchType.Emulator)
            {
                EmulatorProfile? emulator = _emulatorProfiles.FirstOrDefault(e => e.Key == game.EmulatorKey);

                if (emulator == null)
                {
                    issue = "Missing emulator profile.";
                    return false;
                }

                string resolvedExecutablePath = _pathService.Resolve(emulator.ExecutablePath);

                if (!File.Exists(resolvedExecutablePath))
                {
                    issue = $"Missing emulator: {resolvedExecutablePath}";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(game.RomPath))
                {
                    issue = "Missing ROM path.";
                    return false;
                }

                string resolvedRomPath = _pathService.Resolve(game.RomPath);

                if (!File.Exists(resolvedRomPath) && !Directory.Exists(resolvedRomPath))
                {
                    issue = $"Missing ROM: {resolvedRomPath}";
                    return false;
                }

                return true;
            }

            if (game.LaunchType == LaunchType.Native)
            {
                if (string.IsNullOrWhiteSpace(game.LaunchTarget))
                {
                    issue = "Missing launch target.";
                    return false;
                }

                string resolvedLaunchTarget = _pathService.Resolve(game.LaunchTarget);

                if (!File.Exists(resolvedLaunchTarget))
                {
                    issue = $"Missing game executable: {resolvedLaunchTarget}";
                    return false;
                }

                return true;
            }

            issue = "Unsupported launch type.";
            return false;
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

            UpdateLaunchAvailability();
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

            var selectedItem = MenuItems[SelectedIndex];

            if (!selectedItem.IsLaunchAvailable && !string.IsNullOrWhiteSpace(selectedItem.LaunchIssue))
            {
                StatusText = $"{selectedItem.Label} - {selectedItem.LaunchIssue}";
            }
            else
            {
                StatusText = $"{TitleText} - Selected: {selectedItem.Label}";
            }
        }
    }
}