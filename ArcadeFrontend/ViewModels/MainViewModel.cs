using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.Services
{
    public sealed class MainViewModel : INotifyPropertyChanged, IDisposable
    {
        private readonly IInputAbstractionService _inputService;
        private readonly IGameLauncherService _gameLauncherService;
        private readonly INavigationStateService _navigationStateService;
        private readonly IAttractModeCoordinator _attractModeCoordinator;
        private readonly IDiagnosticsSummaryBuilder _diagnosticsSummaryBuilder;
        private readonly ObservableCollection<GameDefinition> _games = new();

        private int _selectedIndex;
        private string _statusMessage = "Ready";
        private string _currentScreen = "MainMenu";
        private string _diagnosticsText = string.Empty;
        private bool _isBusy;
        private bool _isAttractModeActive;
        private IReadOnlyList<EmulatorProfile> _emulatorProfiles = Array.Empty<EmulatorProfile>();

        public event PropertyChangedEventHandler? PropertyChanged;
        public ReadOnlyObservableCollection<GameDefinition> Games { get; }
        public ICommand LaunchSelectedGameCommand { get; }
        public ICommand BackCommand { get; }
        public ICommand RefreshDiagnosticsCommand { get; }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_selectedIndex == value || value < 0 || value >= _games.Count) return;
                _selectedIndex = value;
                UpdateSelectedGameState("Selection changed");
                OnPropertyChanged();
                OnPropertyChanged(nameof(SelectedGame));
            }
        }

        public GameDefinition? SelectedGame => _selectedIndex >= 0 && _selectedIndex < _games.Count ? _games[_selectedIndex] : null;
        public string StatusMessage { get => _statusMessage; private set { if (_statusMessage != value) { _statusMessage = value; OnPropertyChanged(); } } }
        public string CurrentScreen { get => _currentScreen; private set { if (_currentScreen != value) { _currentScreen = value; OnPropertyChanged(); } } }
        public string DiagnosticsText { get => _diagnosticsText; private set { if (_diagnosticsText != value) { _diagnosticsText = value; OnPropertyChanged(); } } }
        public bool IsBusy { get => _isBusy; private set { if (_isBusy != value) { _isBusy = value; OnPropertyChanged(); } } }
        public bool IsAttractModeActive { get => _isAttractModeActive; private set { if (_isAttractModeActive != value) { _isAttractModeActive = value; OnPropertyChanged(); } } }

        public MainViewModel(IInputAbstractionService inputService, IGameLauncherService gameLauncherService, IGameDataService gameDataService, INavigationStateService navigationStateService, IAttractModeCoordinator attractModeCoordinator, ILoggingService loggingService, IDiagnosticsSummaryBuilder diagnosticsSummaryBuilder)
        {
            _inputService = inputService;
            _gameLauncherService = gameLauncherService;
            _navigationStateService = navigationStateService;
            _attractModeCoordinator = attractModeCoordinator;
            _diagnosticsSummaryBuilder = diagnosticsSummaryBuilder;

            Games = new ReadOnlyObservableCollection<GameDefinition>(_games);
            LaunchSelectedGameCommand = new RelayCommand(LaunchSelectedGame, () => !IsBusy && SelectedGame != null);
            BackCommand = new RelayCommand(HandleBackAction);
            RefreshDiagnosticsCommand = new RelayCommand(RefreshDiagnostics);

            _inputService.InputReceived += HandleInputReceived;
        }

        public void Initialize(GameDataLoadResult? gameData, IReadOnlyList<EmulatorProfile>? emulatorProfiles, string initialScreen = "MainMenu")
        {
            _games.Clear();
            if (gameData?.Games != null)
                foreach (var g in gameData.Games.Where(g => g.IsEnabled && !g.IsHidden)) _games.Add(g);
            _emulatorProfiles = emulatorProfiles ?? Array.Empty<EmulatorProfile>();
            CurrentScreen = initialScreen;
            _navigationStateService.SetCurrentScreen(initialScreen, "Main view model initialized");
            if (_games.Count > 0) { _selectedIndex = 0; UpdateSelectedGameState("Initial selection"); StatusMessage = $"Selected: {SelectedGame?.Title}"; }
            else StatusMessage = "No games available.";
            RefreshDiagnostics();
            RaiseCommandStates();
        }

        private void HandleInputReceived(object? sender, InputEvent e)
        {
            _attractModeCoordinator.NotifyUserActivity($"Input received: {e.Action}");
            if (IsBusy) return;
            switch (e.Action)
            {
                case InputAction.Up:
                case InputAction.Left:
                    MoveSelection(-1); break;
                case InputAction.Down:
                case InputAction.Right:
                    MoveSelection(1); break;
                case InputAction.Select:
                case InputAction.Start:
                    LaunchSelectedGame(); break;
                case InputAction.Back:
                case InputAction.Exit:
                    HandleBackAction(); break;
                case InputAction.Admin:
                    OpenAdminDiagnostics(); break;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_games.Count == 0) return;
            var newIndex = _selectedIndex + delta;
            if (newIndex < 0) newIndex = _games.Count - 1;
            else if (newIndex >= _games.Count) newIndex = 0;
            SelectedIndex = newIndex;
            StatusMessage = $"Selected: {SelectedGame?.Title}";
        }

        private void LaunchSelectedGame()
        {
            var game = SelectedGame;
            if (game == null) { StatusMessage = "No game selected."; return; }
            IsBusy = true;
            StatusMessage = $"Launching {game.Title}...";
            _navigationStateService.PushReturnPoint("Launching selected game");
            var result = _gameLauncherService.LaunchGame(new GameLaunchRequest
            {
                GameTitle = game.Title,
                LaunchTarget = game.LaunchTarget,
                EmulatorProfileKey = game.EmulatorProfileKey,
                ExecutablePathOverride = game.ExecutablePathOverride,
                Arguments = game.Arguments,
                WorkingDirectoryOverride = game.WorkingDirectoryOverride,
                TrackProcess = true
            }, _emulatorProfiles);

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

            IsBusy = false;
            RaiseCommandStates();
        }

        private void HandleBackAction()
        {
            if (CurrentScreen == "AdminDiagnostics")
            {
                var restoreResult = _navigationStateService.PopReturnPoint("Leaving admin diagnostics");
                if (restoreResult.IsSuccess && restoreResult.Data != null) ApplySnapshot(restoreResult.Data);
                else { CurrentScreen = "MainMenu"; _navigationStateService.SetCurrentScreen(CurrentScreen, "Fallback after admin diagnostics back action"); }
                StatusMessage = "Returned from diagnostics.";
                return;
            }

            if (CurrentScreen == "GameRunning")
            {
                var terminate = _gameLauncherService.TerminateTrackedProcess();
                DiagnosticsText = _diagnosticsSummaryBuilder.BuildOperationFailureSummary("Terminate Game Process", terminate);
                var restoreResult = _navigationStateService.PopReturnPoint("Returning from game");
                if (restoreResult.IsSuccess && restoreResult.Data != null) ApplySnapshot(restoreResult.Data);
                else { CurrentScreen = "MainMenu"; _navigationStateService.SetCurrentScreen(CurrentScreen, "Fallback after game exit"); }
                StatusMessage = terminate.UserMessage;
                return;
            }

            StatusMessage = "Back action ignored on current screen.";
        }

        private void OpenAdminDiagnostics()
        {
            _navigationStateService.PushReturnPoint("Opening admin diagnostics");
            CurrentScreen = "AdminDiagnostics";
            _navigationStateService.SetCurrentScreen(CurrentScreen, "Admin input action received");
            RefreshDiagnostics();
            StatusMessage = "Admin diagnostics opened.";
        }

        private void RefreshDiagnostics()
        {
            var snapshot = _navigationStateService.GetSnapshot();
            DiagnosticsText = _diagnosticsSummaryBuilder.BuildOperationFailureSummary("Navigation Snapshot", OperationResult<NavigationStateSnapshot>.Success(snapshot, "Navigation snapshot captured."));
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
                if (index >= 0) { _selectedIndex = index; OnPropertyChanged(nameof(SelectedIndex)); OnPropertyChanged(nameof(SelectedGame)); }
            }
        }

        private void RaiseCommandStates()
        {
            if (LaunchSelectedGameCommand is RelayCommand launch) launch.RaiseCanExecuteChanged();
            if (BackCommand is RelayCommand back) back.RaiseCanExecuteChanged();
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public void Dispose() => _inputService.InputReceived -= HandleInputReceived;
    }
}
