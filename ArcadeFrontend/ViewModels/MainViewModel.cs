using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;

namespace ArcadeFrontend.ViewModels
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
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

        private int _selectedIndex;
        private int _selectedMenuIndex;
        private string _statusMessage = "Ready";
        private string _currentScreen = "MainMenu";
        private string _diagnosticsText = string.Empty;
        private string _emptyStateMessage = string.Empty;
        private bool _isBusy;
        private bool _isAttractModeActive;
        private IReadOnlyList<EmulatorProfile> _emulatorProfiles = Array.Empty<EmulatorProfile>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReadOnlyObservableCollection<GameDefinition> Games { get; }
        public ReadOnlyObservableCollection<string> MainMenuItems { get; }

        public ICommand LaunchSelectedGameCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand RefreshDiagnosticsCommand { get; }
        public ICommand ExitCommand { get; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value)
                {
                    return;
                }

                if (value < 0 || value >= _games.Count)
                {
                    return;
                }

                _selectedIndex = value;
                UpdateSelectedGameState("Selection changed");
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedGame));
            }
        }

        public int SelectedMenuIndex
        {
            get => _selectedMenuIndex;
            set
            {
                if (_selectedMenuIndex == value)
                {
                    return;
                }

                if (value < 0 || value >= _mainMenuItems.Count)
                {
                    return;
                }

                _selectedMenuIndex = value;
                StatusMessage = $"Selected: {SelectedMenuItem}";
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedMenuItem));
            }
        }

        public GameDefinition? SelectedGame =>
            _selectedIndex >= 0 && _selectedIndex < _games.Count
                ? _games[_selectedIndex]
                : null;

        public string? SelectedMenuItem =>
            _selectedMenuIndex >= 0 && _selectedMenuIndex < _mainMenuItems.Count
                ? _mainMenuItems[_selectedMenuIndex]
                : null;

        public string StatusMessage
        {
            get => _statusMessage;
            private set
            {
                if (_statusMessage == value)
                {
                    return;
                }

                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string CurrentScreen
        {
            get => _currentScreen;
            private set
            {
                if (_currentScreen == value)
                {
                    return;
                }

                _currentScreen = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(MainMenuVisibility));
                OnPropertyChanged(nameof(GamesVisibility));
                OnPropertyChanged(nameof(LeftPanelTitle));
                OnPropertyChanged(nameof(RightPanelTitle));
            }
        }

        public string MainMenuVisibility => CurrentScreen == "MainMenu" ? "Visible" : "Collapsed";
        public string GamesVisibility => CurrentScreen == "GamesMenu" ? "Visible" : "Collapsed";
        public string LeftPanelTitle => CurrentScreen == "MainMenu" ? "Main Menu" : "Games";
        public string RightPanelTitle => CurrentScreen == "MainMenu" ? "Selection" : "Selected Game";

        public string DiagnosticsText
        {
            get => _diagnosticsText;
            private set
            {
                if (_diagnosticsText == value)
                {
                    return;
                }

                _diagnosticsText = value;
                OnPropertyChanged();
            }
        }

        public string EmptyStateMessage
        {
            get => _emptyStateMessage;
            private set
            {
                if (_emptyStateMessage == value)
                {
                    return;
                }

                _emptyStateMessage = value;
                OnPropertyChanged();
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (_isBusy == value)
                {
                    return;
                }

                _isBusy = value;
                OnPropertyChanged();
            }
        }

        public bool IsAttractModeActive
        {
            get => _isAttractModeActive;
            private set
            {
                if (_isAttractModeActive == value)
                {
                    return;
                }

                _isAttractModeActive = value;
                OnPropertyChanged();
            }
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

            LaunchSelectedGameCommand = new RelayCommand(HandlePrimaryAction, CanHandlePrimaryAction);
            BackCommand = new RelayCommand(HandleBackAction);
            RefreshDiagnosticsCommand = new RelayCommand(OpenDiagnostics);
            ExitCommand = new RelayCommand(ExitApplication);

            _inputService.InputReceived += HandleInputReceived;
        }

        public void Initialize(
            GameDataLoadResult? gameData,
            IReadOnlyList<EmulatorProfile>? emulatorProfiles,
            string initialScreen = "MainMenu")
        {
            _games.Clear();
            _mainMenuItems.Clear();

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

            _emulatorProfiles = emulatorProfiles ?? Array.Empty<EmulatorProfile>();
            CurrentScreen = initialScreen;
            _navigationStateService.SetCurrentScreen(initialScreen, "Main view model initialized");

            _selectedMenuIndex = 0;
            OnPropertyChanged(nameof(SelectedMenuIndex));
            OnPropertyChanged(nameof(SelectedMenuItem));

            if (_games.Count > 0)
            {
                _selectedIndex = 0;
                UpdateSelectedGameState("Initial game selection");
                StatusMessage = $"Selected: {SelectedMenuItem}";
                EmptyStateMessage = "Choose an option from the main menu.";
            }
            else
            {
                StatusMessage = $"Selected: {SelectedMenuItem}";
                EmptyStateMessage =
                    "No games were loaded.\n\n" +
                    "You can still open Diagnostics or Exit from the main menu.\n\n" +
                    "When ready, add config\\games.json to the app output folder.";
            }

            _loggingService.Info(nameof(MainViewModel), "Main view model initialized.", $"Games: {_games.Count} | Screen: {initialScreen}");
            RefreshDiagnostics();
            RaiseCommandStates();
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

                return;
            }
        }

        private void MoveMenuSelection(int delta)
        {
            if (_mainMenuItems.Count == 0)
            {
                return;
            }

            var newIndex = _selectedMenuIndex + delta;
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

            var newIndex = _selectedIndex + delta;
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

        private void ExecuteMainMenuSelection()
        {
            switch (SelectedMenuItem)
            {
                case "Games":
                    CurrentScreen = "GamesMenu";
                    _navigationStateService.SetCurrentScreen(CurrentScreen, "Opened games menu");
                    StatusMessage = _games.Count > 0
                        ? $"Selected: {SelectedGame?.Title}"
                        : "No games available.";
                    EmptyStateMessage = _games.Count > 0
                        ? "Choose a game to launch."
                        : "No games were loaded. Add config\\games.json to the app output folder.";
                    break;

                case "Diagnostics":
                    OpenDiagnostics();
                    break;

                case "Exit":
                    ExitApplication();
                    break;
            }
        }

        private void OpenDiagnostics()
        {
            CurrentScreen = "AdminDiagnostics";
            _navigationStateService.SetCurrentScreen(CurrentScreen, "Diagnostics opened");
            RefreshDiagnostics();
            StatusMessage = "Diagnostics opened.";
            EmptyStateMessage = "Review startup and navigation details in the diagnostics panel.";
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

            var result = _gameLauncherService.LaunchGame(request, _emulatorProfiles);
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
            RaiseCommandStates();
        }

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

        private void ExitApplication()
        {
            _loggingService.Info(nameof(MainViewModel), "Exit requested from main view model.");
            Application.Current?.Shutdown();
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
            _navigationStateService.SetSelectedGame(selectedGame?.Id, _selectedIndex, selectedGame?.Platform, reason);
            RaiseCommandStates();
        }

        private void RaiseCommandStates()
        {
            if (LaunchSelectedGameCommand is RelayCommand launchCommand)
            {
                launchCommand.RaiseCanExecuteChanged();
            }

            if (BackCommand is RelayCommand backCommand)
            {
                backCommand.RaiseCanExecuteChanged();
            }

            if (ExitCommand is RelayCommand exitCommand)
            {
                exitCommand.RaiseCanExecuteChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _inputService.InputReceived -= HandleInputReceived;
        }
    }
}
