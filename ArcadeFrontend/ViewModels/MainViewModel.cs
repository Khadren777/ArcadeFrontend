using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;

namespace ArcadeFrontend.ViewModels
{
    public sealed partial class MainViewModel : ViewModelBase, IDisposable
    {
        private readonly IInputAbstractionService _inputService;
        private readonly IGameLauncherService _gameLauncherService;
        private readonly IGameDataService _gameDataService;
        private readonly INavigationStateService _navigationStateService;
        private readonly IAttractModeCoordinator _attractModeCoordinator;
        private readonly ILoggingService _loggingService;
        private readonly IDiagnosticsSummaryBuilder _diagnosticsSummaryBuilder;
        private readonly ObservableCollection<GameDefinition> _games = new();
        private readonly ObservableCollection<string> _mainMenuItems = new();

        [ObservableProperty]
        private int selectedIndex;

        [ObservableProperty]
        private int selectedMenuIndex;

        [ObservableProperty]
        private string statusMessage = "Ready";

        [ObservableProperty]
        private string currentScreen = "MainMenu";

        [ObservableProperty]
        private string diagnosticsText = string.Empty;

        [ObservableProperty]
        private string emptyStateMessage = string.Empty;

        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private bool isAttractModeActive;

        [ObservableProperty]
        private IReadOnlyList<EmulatorProfile> emulatorProfiles = Array.Empty<EmulatorProfile>();

        public ReadOnlyObservableCollection<GameDefinition> Games { get; }
        public ReadOnlyObservableCollection<string> MainMenuItems { get; }

        public GameDefinition? SelectedGame =>
            SelectedIndex >= 0 && SelectedIndex < _games.Count
                ? _games[SelectedIndex]
                : null;

        public string? SelectedMenuItem =>
            SelectedMenuIndex >= 0 && SelectedMenuIndex < _mainMenuItems.Count
                ? _mainMenuItems[SelectedMenuIndex]
                : null;

        public string MainMenuVisibility => CurrentScreen == "MainMenu" ? "Visible" : "Collapsed";
        public string GamesVisibility => CurrentScreen == "GamesMenu" ? "Visible" : "Collapsed";
        public string LeftPanelTitle => CurrentScreen == "MainMenu" ? "Main Menu" : "Games";
        public string RightPanelTitle => CurrentScreen == "AdminDiagnostics" ? "Diagnostics Focus" : "Selection";

        public string RightPanelHeadline
        {
            get
            {
                if (CurrentScreen == "GamesMenu")
                {
                    return SelectedGame?.Title ?? "No Game Selected";
                }

                return SelectedMenuItem ?? "Main Menu";
            }
        }

        public string RightPanelSubheadline
        {
            get
            {
                if (CurrentScreen == "GamesMenu")
                {
                    return SelectedGame?.Platform ?? "No platform";
                }

                return CurrentScreen;
            }
        }

        public string RightPanelBody
        {
            get
            {
                if (CurrentScreen == "GamesMenu")
                {
                    if (SelectedGame == null)
                    {
                        return _games.Count == 0
                            ? "No games are currently loaded."
                            : "Choose a game from the list.";
                    }

                    return string.IsNullOrWhiteSpace(SelectedGame.Description)
                        ? "No description available."
                        : SelectedGame.Description;
                }

                return SelectedMenuItem switch
                {
                    "Systems" => "Reserved for the future systems browser. This is the next logical expansion point after content hookup is proven.",
                    "Games" => _games.Count > 0
                        ? "Open the full games list and launch from the current library."
                        : "No games are loaded yet. Once games.json is wired correctly, this option becomes the main library browser.",
                    "Diagnostics" => "Open the diagnostics view and inspect startup, navigation, and launch state.",
                    "Exit" => "Close the frontend cleanly.",
                    _ => "Choose an option from the main menu."
                };
            }
        }

        [RelayCommand(CanExecute = nameof(CanHandlePrimaryAction))]
        private void HandlePrimaryAction()
        {
            if (CurrentScreen == "MainMenu")
            {
                ExecuteMainMenuSelection();
                return;
            }

            if (CurrentScreen == "GamesMenu")
            {
                LaunchSelectedGame();
                return;
            }

            if (CurrentScreen == "AdminDiagnostics")
            {
                RefreshDiagnostics();
            }
        }

        [RelayCommand]
        private void HandleBackAction()
        {
            if (CurrentScreen == "GameRunning")
            {
                var terminateResult = _gameLauncherService.TerminateTrackedProcess();
                DiagnosticsText = _diagnosticsSummaryBuilder.BuildOperationFailureSummary("Terminate Game Process", terminateResult);
                ReturnToMainMenu();
                StatusMessage = terminateResult.UserMessage;
                return;
            }

            if (CurrentScreen == "GamesMenu" || CurrentScreen == "AdminDiagnostics")
            {
                ReturnToMainMenu();
                return;
            }

            ExitApplication();
        }

        [RelayCommand]
        private void OpenDiagnostics()
        {
            CurrentScreen = "AdminDiagnostics";
            _navigationStateService.SetCurrentScreen(CurrentScreen, "Diagnostics opened");
            RefreshDiagnostics();
            StatusMessage = "Diagnostics opened.";
            EmptyStateMessage = "Review startup and navigation details in the diagnostics panel.";
        }

        [RelayCommand]
        private void ExitApplication()
        {
            _loggingService.Info(nameof(MainViewModel), "Exit requested from main view model.");
            Application.Current?.Shutdown();
        }

        [RelayCommand]
        private void RefreshDiagnosticsCommand()
        {
            RefreshDiagnostics();
        }

        public MainViewModel(
            IInputAbstractionService inputService,
            IGameLauncherService gameLauncherService,
            IGameDataService gameDataService,
            INavigationStateService navigationStateService,
            IAttractModeCoordinator attractModeCoordinator,
            ILoggingService loggingService,
            IDiagnosticsSummaryBuilder diagnosticsSummaryBuilder)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _gameLauncherService = gameLauncherService ?? throw new ArgumentNullException(nameof(gameLauncherService));
            _gameDataService = gameDataService ?? throw new ArgumentNullException(nameof(gameDataService));
            _navigationStateService = navigationStateService ?? throw new ArgumentNullException(nameof(navigationStateService));
            _attractModeCoordinator = attractModeCoordinator ?? throw new ArgumentNullException(nameof(attractModeCoordinator));
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _diagnosticsSummaryBuilder = diagnosticsSummaryBuilder ?? throw new ArgumentNullException(nameof(diagnosticsSummaryBuilder));

            Games = new ReadOnlyObservableCollection<GameDefinition>(_games);
            MainMenuItems = new ReadOnlyObservableCollection<string>(_mainMenuItems);

            _inputService.InputReceived += HandleInputReceived;
        }

        partial void OnSelectedIndexChanged(int oldValue, int newValue)
        {
            if (newValue < 0 || newValue >= _games.Count)
            {
                SelectedIndex = oldValue;
                return;
            }

            UpdateSelectedGameState("Selection changed");
            RefreshRightPanel();
        }

        partial void OnSelectedMenuIndexChanged(int oldValue, int newValue)
        {
            if (newValue < 0 || newValue >= _mainMenuItems.Count)
            {
                SelectedMenuIndex = oldValue;
                return;
            }

            StatusMessage = $"Selected: {SelectedMenuItem}";
            RefreshRightPanel();
        }

        partial void OnCurrentScreenChanged(string oldValue, string newValue)
        {
            OnPropertyChanged(nameof(MainMenuVisibility));
            OnPropertyChanged(nameof(GamesVisibility));
            OnPropertyChanged(nameof(LeftPanelTitle));
            OnPropertyChanged(nameof(RightPanelTitle));
            RefreshRightPanel();
        }

        public void Initialize(
            GameDataLoadResult? gameData,
            IReadOnlyList<EmulatorProfile>? emulatorProfiles,
            string initialScreen = "MainMenu")
        {
            _games.Clear();
            _mainMenuItems.Clear();

            _mainMenuItems.Add("Systems");
            _mainMenuItems.Add("Games");
            _mainMenuItems.Add("Diagnostics");
            _mainMenuItems.Add("Exit");

            if (gameData?.Games != null)
            {
                foreach (var game in gameData.Games.Where(g => g.IsEnabled && !g.IsHidden))
                {
                    _games.Add(game);
                }
            }

            EmulatorProfiles = emulatorProfiles ?? Array.Empty<EmulatorProfile>();
            CurrentScreen = initialScreen;
            _navigationStateService.SetCurrentScreen(initialScreen, "Main view model initialized");

            SelectedMenuIndex = 0;

            if (_games.Count > 0)
            {
                SelectedIndex = 0;
                UpdateSelectedGameState("Initial game selection");
                StatusMessage = $"Selected: {SelectedMenuItem}";
                EmptyStateMessage = "Choose an option from the main menu.";
            }
            else
            {
                StatusMessage = $"Selected: {SelectedMenuItem}";
                EmptyStateMessage = "No games were loaded - Diagnostics and Exit are still available - When ready, add Config/games.json to the app output folder.";
            }

            _loggingService.Info(nameof(MainViewModel), "Main view model initialized.", $"Games: {_games.Count} | Screen: {initialScreen}");
            RefreshDiagnostics();
            RefreshRightPanel();
        }

        private void HandleInputReceived(object? sender, InputEvent e)
        {
            _attractModeCoordinator.NotifyUserActivity($"Input received: {e.Action}");

            if (IsBusy)
            {
                return;
            }

            if (CurrentScreen == "MainMenu")
            {
                switch (e.Action)
                {
                    case InputAction.Up:
                    case InputAction.Left:
                        MoveMenuSelection(-1);
                        break;
                    case InputAction.Down:
                    case InputAction.Right:
                        MoveMenuSelection(1);
                        break;
                    case InputAction.Select:
                    case InputAction.Start:
                        ExecuteMainMenuSelection();
                        break;
                    case InputAction.Back:
                    case InputAction.Exit:
                        ExitApplication();
                        break;
                    case InputAction.Admin:
                        OpenDiagnostics();
                        break;
                }

                return;
            }

            if (CurrentScreen == "GamesMenu")
            {
                switch (e.Action)
                {
                    case InputAction.Up:
                    case InputAction.Left:
                        MoveGameSelection(-1);
                        break;
                    case InputAction.Down:
                    case InputAction.Right:
                        MoveGameSelection(1);
                        break;
                    case InputAction.Select:
                    case InputAction.Start:
                        LaunchSelectedGame();
                        break;
                    case InputAction.Back:
                    case InputAction.Exit:
                        ReturnToMainMenu();
                        break;
                    case InputAction.Admin:
                        OpenDiagnostics();
                        break;
                }

                return;
            }

            if (CurrentScreen == "AdminDiagnostics")
            {
                switch (e.Action)
                {
                    case InputAction.Back:
                    case InputAction.Exit:
                        ReturnToMainMenu();
                        break;
                    case InputAction.Admin:
                        RefreshDiagnostics();
                        break;
                }
            }
        }

        private void MoveMenuSelection(int delta)
        {
            if (_mainMenuItems.Count == 0)
            {
                return;
            }

            var newIndex = SelectedMenuIndex + delta;
            if (newIndex < 0)
            {
                newIndex = _mainMenuItems.Count - 1;
            }
            else if (newIndex >= _mainMenuItems.Count)
            {
                newIndex = 0;
            }

            SelectedMenuIndex = newIndex;
        }

        private void MoveGameSelection(int delta)
        {
            if (_games.Count == 0)
            {
                StatusMessage = "No games available.";
                return;
            }

            var newIndex = SelectedIndex + delta;
            if (newIndex < 0)
            {
                newIndex = _games.Count - 1;
            }
            else if (newIndex >= _games.Count)
            {
                newIndex = 0;
            }

            SelectedIndex = newIndex;
            StatusMessage = $"Selected: {SelectedGame?.Title}";
        }

        private bool CanHandlePrimaryAction()
        {
            return !IsBusy;
        }

        private void ExecuteMainMenuSelection()
        {
            switch (SelectedMenuItem)
            {
                case "Systems":
                    StatusMessage = "Systems menu is not implemented yet.";
                    EmptyStateMessage = "Systems is the next expansion point. For now, use Games or Diagnostics.";
                    RefreshRightPanel();
                    break;

                case "Games":
                    CurrentScreen = "GamesMenu";
                    _navigationStateService.SetCurrentScreen(CurrentScreen, "Opened games menu");
                    StatusMessage = _games.Count > 0
                        ? $"Selected: {SelectedGame?.Title}"
                        : "No games available.";
                    EmptyStateMessage = _games.Count > 0
                        ? "Choose a game to launch."
                        : "No games were loaded. Add Config/games.json to the app output folder.";
                    break;

                case "Diagnostics":
                    OpenDiagnostics();
                    break;

                case "Exit":
                    ExitApplication();
                    break;
            }
        }

        private void ReturnToMainMenu()
        {
            CurrentScreen = "MainMenu";
            _navigationStateService.SetCurrentScreen(CurrentScreen, "Returned to main menu");
            StatusMessage = $"Selected: {SelectedMenuItem}";
            EmptyStateMessage = _games.Count > 0
                ? "Choose an option from the main menu."
                : "No games were loaded. Diagnostics and Exit are still available.";
        }

        private void LaunchSelectedGame()
        {
            var game = SelectedGame;
            if (game == null)
            {
                StatusMessage = "No game selected.";
                return;
            }

            IsBusy = true;
            StatusMessage = $"Launching {game.Title}...";
            _navigationStateService.PushReturnPoint("Launching selected game");

            var request = new GameLaunchRequest
            {
                GameTitle = game.Title,
                LaunchTarget = game.LaunchTarget,
                EmulatorProfileKey = game.EmulatorProfileKey,
                ExecutablePathOverride = game.ExecutablePathOverride,
                Arguments = game.Arguments,
                WorkingDirectoryOverride = game.WorkingDirectoryOverride,
                TrackProcess = true
            };

            var result = _gameLauncherService.LaunchGame(request, EmulatorProfiles);
            DiagnosticsText = _diagnosticsSummaryBuilder.BuildLaunchSummary(result);

            if (result.IsSuccess)
            {
                CurrentScreen = "GameRunning";
                _navigationStateService.SetCurrentScreen(CurrentScreen, "Game launched successfully");
                StatusMessage = result.UserMessage;
            }
            else
            {
                _navigationStateService.PopReturnPoint("Launch failed; restoring previous state");
                StatusMessage = result.UserMessage;
            }

            _loggingService.Info(nameof(MainViewModel), "Launch attempt completed.", result.ToDiagnosticSummary());
            IsBusy = false;
        }

        private void RefreshDiagnostics()
        {
            var snapshot = _navigationStateService.GetSnapshot();
            var result = OperationResult<NavigationStateSnapshot>.Success(snapshot, "Navigation snapshot captured.");
            DiagnosticsText = _diagnosticsSummaryBuilder.BuildOperationFailureSummary("Navigation Snapshot", result);
            IsAttractModeActive = _attractModeCoordinator.IsAttractModeActive;
        }

        private void UpdateSelectedGameState(string reason)
        {
            var selectedGame = SelectedGame;
            _navigationStateService.SetSelectedGame(selectedGame?.Id, SelectedIndex, selectedGame?.Platform, reason);
        }

        private void RefreshRightPanel()
        {
            OnPropertyChanged(nameof(RightPanelHeadline));
            OnPropertyChanged(nameof(RightPanelSubheadline));
            OnPropertyChanged(nameof(RightPanelBody));
            OnPropertyChanged(nameof(SelectedGame));
            OnPropertyChanged(nameof(SelectedMenuItem));
        }

        public void Dispose()
        {
            _inputService.InputReceived -= HandleInputReceived;
        }
    }
}
