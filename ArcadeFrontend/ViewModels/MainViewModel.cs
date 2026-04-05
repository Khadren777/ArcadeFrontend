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

        private int _selectedIndex;
        private string _statusMessage = "Ready";
        private string _currentScreen = "MainMenu";
        private string _diagnosticsText = string.Empty;
        private string _emptyStateMessage = string.Empty;
        private bool _isBusy;
        private bool _isAttractModeActive;
        private IReadOnlyList<EmulatorProfile> _emulatorProfiles = Array.Empty<EmulatorProfile>();

        public event PropertyChangedEventHandler? PropertyChanged;

        public ReadOnlyObservableCollection<GameDefinition> Games { get; }
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

        public GameDefinition? SelectedGame =>
            _selectedIndex >= 0 && _selectedIndex < _games.Count
                ? _games[_selectedIndex]
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
            }
        }

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
            LaunchSelectedGameCommand = new RelayCommand(LaunchSelectedGame, CanLaunchSelectedGame);
            BackCommand = new RelayCommand(HandleBackAction);
            RefreshDiagnosticsCommand = new RelayCommand(RefreshDiagnostics);
            ExitCommand = new RelayCommand(ExitApplication);

            _inputService.InputReceived += HandleInputReceived;
        }

        public void Initialize(
            GameDataLoadResult? gameData,
            IReadOnlyList<EmulatorProfile>? emulatorProfiles,
            string initialScreen = "MainMenu")
        {
            _games.Clear();

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

            if (_games.Count > 0)
            {
                _selectedIndex = 0;
                UpdateSelectedGameState("Initial selection");
                StatusMessage = $"Selected: {SelectedGame?.Title}";
                EmptyStateMessage = string.Empty;
            }
            else
            {
                StatusMessage = "No games available.";
                EmptyStateMessage =
                    "No games were loaded.\n\n" +
                    "Check that config\\games.json exists in the app output folder and contains valid entries.\n\n" +
                    "You can still use Diagnostics or Exit from this screen.";
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

            switch (e.Action)
            {
                case InputAction.Up:
                    MoveSelection(-1);
                    break;
                case InputAction.Down:
                    MoveSelection(1);
                    break;
                case InputAction.Left:
                    MoveSelection(-1);
                    break;
                case InputAction.Right:
                    MoveSelection(1);
                    break;
                case InputAction.Select:
                case InputAction.Start:
                    LaunchSelectedGame();
                    break;
                case InputAction.Back:
                case InputAction.Exit:
                    HandleBackAction();
                    break;
                case InputAction.Admin:
                    RefreshDiagnostics();
                    break;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_games.Count == 0)
            {
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

        private bool CanLaunchSelectedGame()
        {
            return !IsBusy && SelectedGame != null;
        }

        private void LaunchSelectedGame()
        {
            var game = SelectedGame;
            if (game == null)
            {
                StatusMessage = _games.Count == 0 ? "No game selected because no games are loaded." : "No game selected.";
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

                var restoreResult = _navigationStateService.PopReturnPoint("Returning from game");
                if (restoreResult.IsSuccess && restoreResult.Data != null)
                {
                    ApplySnapshot(restoreResult.Data);
                }
                else
                {
                    CurrentScreen = "MainMenu";
                    _navigationStateService.SetCurrentScreen(CurrentScreen, "Fallback after game exit");
                }

                StatusMessage = terminateResult.UserMessage;
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

        private void ApplySnapshot(NavigationStateSnapshot snapshot)
        {
            CurrentScreen = snapshot.CurrentScreen ?? "MainMenu";
            _navigationStateService.SetCurrentScreen(CurrentScreen, "Snapshot applied to main view model");

            if (!string.IsNullOrWhiteSpace(snapshot.SelectedGameId))
            {
                var index = _games.ToList().FindIndex(g => string.Equals(g.Id, snapshot.SelectedGameId, StringComparison.OrdinalIgnoreCase));
                if (index >= 0)
                {
                    _selectedIndex = index;
                    OnPropertyChanged(nameof(SelectedIndex));
                    OnPropertyChanged(nameof(SelectedGame));
                }
            }
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
