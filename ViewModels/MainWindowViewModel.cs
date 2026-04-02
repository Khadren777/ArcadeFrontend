using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.Services.Launching;
using ArcadeFrontend.Services.Library;
using ArcadeFrontend.Services.Navigation;
using ArcadeFrontend.Services.Sessions;
using ArcadeFrontend.Services.State;

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
    private readonly SettingsService _settingsService;
    private readonly LoggingService _loggingService;
    private readonly AdminDiagnosticsService _adminDiagnosticsService;
    private readonly VisualStateService _visualStateService;

    private bool _shouldExit;
    private int _selectedIndex;
    private string _titleText = "ARCADE FRONTEND";
    private string _statusText = "Ready";
    private string _subtitleText = string.Empty;
    private string _backgroundImagePath = string.Empty;
    private string _attractVideoPath = string.Empty;
    private bool _showAttractVideo;
    private bool _showDiagnosticsPanel;
    private bool _dimBackgroundUnderVideo = true;
    private AppSettings _settings = new();

    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();
    public ObservableCollection<string> DiagnosticLines { get; } = new();

    public string TitleText
    {
        get => _titleText;
        private set { if (_titleText == value) return; _titleText = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        private set { if (_statusText == value) return; _statusText = value; OnPropertyChanged(); }
    }

    public string SubtitleText
    {
        get => _subtitleText;
        private set { if (_subtitleText == value) return; _subtitleText = value; OnPropertyChanged(); }
    }

    public string BackgroundImagePath
    {
        get => _backgroundImagePath;
        private set { if (_backgroundImagePath == value) return; _backgroundImagePath = value; OnPropertyChanged(); }
    }

    public string AttractVideoPath
    {
        get => _attractVideoPath;
        private set { if (_attractVideoPath == value) return; _attractVideoPath = value; OnPropertyChanged(); }
    }

    public bool ShowAttractVideo
    {
        get => _showAttractVideo;
        private set { if (_showAttractVideo == value) return; _showAttractVideo = value; OnPropertyChanged(); }
    }

    public bool ShowDiagnosticsPanel
    {
        get => _showDiagnosticsPanel;
        private set { if (_showDiagnosticsPanel == value) return; _showDiagnosticsPanel = value; OnPropertyChanged(); }
    }

    public bool DimBackgroundUnderVideo
    {
        get => _dimBackgroundUnderVideo;
        private set { if (_dimBackgroundUnderVideo == value) return; _dimBackgroundUnderVideo = value; OnPropertyChanged(); }
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
        private set { if (_shouldExit == value) return; _shouldExit = value; OnPropertyChanged(); }
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
        PathService pathService,
        SettingsService settingsService,
        LoggingService loggingService,
        AdminDiagnosticsService adminDiagnosticsService,
        VisualStateService visualStateService)
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
        _settingsService = settingsService;
        _loggingService = loggingService;
        _adminDiagnosticsService = adminDiagnosticsService;
        _visualStateService = visualStateService;

        _idleStateService.AttractModeRequested += (_, _) =>
        {
            _loggingService.Info("Idle", "Attract mode requested");
            _navigationStateService.EnterAttractMode();
            SelectedIndex = 0;
            RenderCurrentScreen();
            RefreshSelectionState();
        };
    }

    public void Initialize()
    {
        ReloadSettings();
        _libraryService.Load();
        _recentSessionService.Load();
        ValidateEnvironment();
        _idleStateService.Start();
        _loggingService.Info("App", "Frontend initialized");
        RenderCurrentScreen();
        RefreshDiagnostics();
    }

    public void ReloadSettings()
    {
        _settings = _settingsService.LoadSettings();
        RefreshVisualState();
        RefreshDiagnostics();
    }

    public void NotifyUserInteraction()
    {
        bool exitedAttractMode = _idleStateService.NotifyUserInteraction();

        if (exitedAttractMode)
        {
            _loggingService.Info("Idle", "Exited attract mode from user interaction");
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
            _loggingService.Info("Admin", "Admin unlocked");
            NavigateTo(ScreenType.AdminMenu);
            StatusText = "Admin unlocked";
            RefreshDiagnostics();
            return true;
        }

        return false;
    }

    public void OpenServiceMode()
    {
        if (_idleStateService.IsInAttractMode) { TryExitAttractMode(); return; }
        _idleStateService.NotifyUserInteraction();
        _loggingService.Info("Admin", "Service mode opened");
        NavigateTo(ScreenType.AdminMenu);
        RefreshDiagnostics();
    }

    public void TryExitAttractMode()
    {
        if (_idleStateService.ExitAttractMode())
        {
            _loggingService.Info("Idle", "Attract mode exited");
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

    public void ClearExitRequest()
    {
        ShouldExit = false;
    }

    private void EnsureValidSelection()
    {
        if (MenuItems.Count == 0) { SelectedIndex = 0; return; }
        if (SelectedIndex < 0 || SelectedIndex >= MenuItems.Count) SelectedIndex = 0;
    }

    private void ValidateEnvironment()
    {
        List<string> issues = new();

        foreach (EmulatorProfile emulator in _libraryService.EmulatorProfiles)
        {
            string resolvedPath = _pathService.Resolve(emulator.ExecutablePath);
            if (!File.Exists(resolvedPath))
            {
                issues.Add($"Missing emulator: {emulator.DisplayName}");
            }
        }

        if (issues.Count > 0)
        {
            foreach (string issue in issues)
            {
                _loggingService.Warning("Environment", issue);
            }
        }

        StatusText = issues.Count > 0
            ? $"Missing dependencies: {string.Join(", ", issues)}"
            : "Ready";
    }

    private void RefreshDiagnostics()
    {
        DiagnosticLines.Clear();
        DiagnosticLines.Add(_adminDiagnosticsService.BuildSummary());

        if (!_settings.EnableDiagnosticLogging)
        {
            return;
        }

        foreach (AppLogEntry entry in _adminDiagnosticsService.GetRecentLogs().TakeLast(12))
        {
            DiagnosticLines.Add($"[{entry.Level}] {entry.TimestampUtc:HH:mm:ss} {entry.Category} - {entry.Message}");
        }
    }

    private void RefreshVisualState()
    {
        VisualStateSnapshot state = _visualStateService.Build(
            _navigationStateService.CurrentScreen,
            _navigationStateService.SelectedSystem,
            _settings,
            _adminStateService.IsUnlocked);

        BackgroundImagePath = state.BackgroundImagePath;
        AttractVideoPath = state.AttractVideoPath;
        ShowAttractVideo = state.ShowAttractVideo;
        ShowDiagnosticsPanel = state.ShowDiagnosticsPanel;
        DimBackgroundUnderVideo = state.DimBackgroundUnderVideo;
        SubtitleText = state.SubtitleText;
    }

    private void UpdateLaunchAvailability()
    {
        foreach (MenuItemViewModel item in MenuItems)
        {
            item.IsLaunchAvailable = true;
            item.LaunchIssue = string.Empty;

            if (item.Game == null) continue;
            Game game = item.Game;

            if (game.LaunchType == LaunchType.Emulator)
            {
                EmulatorProfile? emulator = _libraryService.EmulatorProfiles.FirstOrDefault(e => e.Key == game.EmulatorKey);

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
        if (MenuItems.Count == 0 || SelectedIndex < 0 || SelectedIndex >= MenuItems.Count) return;

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
                if (_adminStateService.IsUnlocked && _settings.ShowHiddenGamesInAdmin) NavigateTo(ScreenType.HiddenGamesMenu);
                else StatusText = "Hidden Games locked";
                break;
            case MenuAction.OpenSettings:
            case MenuAction.OpenAdminMenu:
                if (_adminStateService.IsUnlocked)
                {
                    NavigateTo(ScreenType.AdminMenu);
                    RefreshDiagnostics();
                }
                else StatusText = "Admin locked";
                break;
            case MenuAction.OpenSystemGames:
                _loggingService.Info("Navigation", $"Opening system: {item.Value}");
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
                    LaunchResult result = _launchFlowService.Launch(item.Game);
                    StatusText = result.Message;

                    if (_settings.EnableLaunchLogging)
                    {
                        if (result.Success) _loggingService.Info("Launch", result.Message);
                        else _loggingService.Error("Launch", result.Message);
                    }

                    if (!result.Success) errorMessage = result.Message;
                    RefreshDiagnostics();
                }
                break;
            case MenuAction.ToggleFavorite:
                ToggleFavorite();
                break;
            case MenuAction.ExitApp:
                ShouldExit = true;
                _loggingService.Info("App", "Exit requested");
                RefreshDiagnostics();
                break;
            case MenuAction.RescanLibrary:
                _libraryService.Reload();
                StatusText = "Library rescanned";
                _loggingService.Info("Library", "Library rescanned");
                RenderCurrentScreen();
                RefreshDiagnostics();
                break;
            case MenuAction.InputTest:
                StatusText = "Input Test selected";
                _loggingService.Info("Admin", "Input test selected");
                RefreshDiagnostics();
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

    private void ToggleFavorite()
    {
        Game? selectedGame = GetSelectedGame();
        if (selectedGame is null)
        {
            StatusText = "No game selected";
            return;
        }

        _favoritesService.ToggleFavorite(selectedGame, _libraryService.Games);
        StatusText = selectedGame.IsFavorite
            ? $"Added to favorites: {selectedGame.Title}"
            : $"Removed from favorites: {selectedGame.Title}";

        _loggingService.Info("Favorites", StatusText);
        RefreshDiagnostics();
        RenderCurrentScreen();
    }

    private Game? GetSelectedGame()
    {
        if (SelectedIndex < 0 || SelectedIndex >= MenuItems.Count) return null;
        return MenuItems[SelectedIndex].Game;
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

    private void NavigateTo(ScreenType targetScreen)
    {
        _loggingService.Info("Navigation", $"NavigateTo: {targetScreen}");
        _navigationStateService.NavigateTo(targetScreen);
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
        UpdateLaunchAvailability();
        RefreshVisualState();
        UpdateStatusForCurrentScreen();
    }

    private MenuScreen BuildScreenForCurrentState()
    {
        return _navigationStateService.CurrentScreen switch
        {
            ScreenType.MainMenu => _menuDefinitionService.BuildMainMenu(),
            ScreenType.SystemsMenu => _menuDefinitionService.BuildSystemsMenu(),
            ScreenType.GamesMenu => _menuDefinitionService.BuildGamesMenu(_navigationStateService.SelectedSystem, _libraryService.Games.ToList()),
            ScreenType.HiddenGamesMenu => _menuDefinitionService.BuildHiddenGamesMenu(_libraryService.Games.ToList()),
            ScreenType.AdminMenu => _menuDefinitionService.BuildAdminMenu(),
            ScreenType.RecentGamesMenu => _menuDefinitionService.BuildRecentGamesMenu(_recentSessionService.RecentGames.ToList()),
            ScreenType.FavoritesMenu => _menuDefinitionService.BuildFavoritesMenu(_libraryService.Games.ToList()),
            ScreenType.AttractMode => _menuDefinitionService.BuildAttractModeScreen(_recentSessionService.RecentGames.ToList()),
            _ => new MenuScreen()
        };
    }

    private void RefreshSelectionState()
    {
        if (MenuItems.Count == 0) return;
        EnsureValidSelection();

        for (int i = 0; i < MenuItems.Count; i++)
        {
            MenuItems[i].IsSelected = i == SelectedIndex;
        }
    }

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
