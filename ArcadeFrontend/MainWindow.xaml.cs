using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.ViewModels;

namespace ArcadeFrontend
{
    public partial class MainWindow : Window
    {
        private readonly ILoggingService _loggingService;
        private readonly IPathService _pathService;
        private readonly IAppStartupCoordinator _appStartupCoordinator;
        private readonly IInputAbstractionService _inputService;
        private readonly IGameLauncherService _gameLauncherService;
        private readonly IIdleService _idleService;
        private readonly IAttractModeCoordinator _attractModeCoordinator;
        private readonly IDiagnosticsSummaryBuilder _diagnosticsSummaryBuilder;
        private readonly MainViewModel _mainViewModel;
        private readonly IInputComboService _inputComboService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly RevealMediaService _revealMediaService;

        public MainWindow(
            ILoggingService loggingService,
            IPathService pathService,
            IGameDataService gameDataService,
            IStartupValidationService startupValidationService,
            IAppStartupCoordinator appStartupCoordinator,
            IInputAbstractionService inputService,
            IGameLauncherService gameLauncherService,
            INavigationStateService navigationStateService,
            IIdleService idleService,
            IAttractModeCoordinator attractModeCoordinator,
            IDiagnosticsSummaryBuilder diagnosticsSummaryBuilder,
            MainViewModel mainViewModel,
            IInputComboService inputComboService,
            IAppSettingsService appSettingsService,
            RevealMediaService revealMediaService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            _pathService = pathService ?? throw new ArgumentNullException(nameof(pathService));
            _appStartupCoordinator = appStartupCoordinator ?? throw new ArgumentNullException(nameof(appStartupCoordinator));
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _gameLauncherService = gameLauncherService ?? throw new ArgumentNullException(nameof(gameLauncherService));
            _idleService = idleService ?? throw new ArgumentNullException(nameof(idleService));
            _attractModeCoordinator = attractModeCoordinator ?? throw new ArgumentNullException(nameof(attractModeCoordinator));
            _diagnosticsSummaryBuilder = diagnosticsSummaryBuilder ?? throw new ArgumentNullException(nameof(diagnosticsSummaryBuilder));
            _mainViewModel = mainViewModel ?? throw new ArgumentNullException(nameof(mainViewModel));
            _inputComboService = inputComboService ?? throw new ArgumentNullException(nameof(inputComboService));
            _appSettingsService = appSettingsService ?? throw new ArgumentNullException(nameof(appSettingsService));
            _revealMediaService = revealMediaService ?? throw new ArgumentNullException(nameof(revealMediaService));

            InitializeComponent();

            DataContext = _mainViewModel;

            Loaded += OnLoaded;
            KeyDown += OnKeyDown;
            Closing += OnClosing;

            ConfigureInputBindings();
            ConfigureIdleService();
            ConfigureInputCombos();
            WireComboPipeline();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.Info(nameof(MainWindow), "Main window loaded. Startup initialization beginning.");
                _attractModeCoordinator.Initialize();
                _idleService.Start();

                var startupResult = _appStartupCoordinator.Initialize();
                if (!startupResult.IsSuccess || startupResult.Data == null)
                {
                    var summary = _diagnosticsSummaryBuilder.BuildOperationFailureSummary("Startup Initialization", startupResult);
                    _loggingService.Error(nameof(MainWindow), "Startup initialization failed.", details: summary);
                    MessageBox.Show(summary, "Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    _mainViewModel.Initialize(null, Array.Empty<EmulatorProfile>());
                    return;
                }

                var startupData = startupResult.Data;
                _mainViewModel.Initialize(startupData.GameData, startupData.EmulatorProfiles);

                if (!startupData.CanContinue)
                {
                    MessageBox.Show(_diagnosticsSummaryBuilder.BuildStartupSummary(startupData), "Startup Diagnostics", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                _loggingService.Critical(nameof(MainWindow), "Unhandled exception during window load.", ex, ex.Message);
                MessageBox.Show(ex.ToString(), "Fatal Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ConfigureInputBindings()
        {
            _inputService.RegisterKeyBinding(Key.Up, InputAction.Up);
            _inputService.RegisterKeyBinding(Key.Down, InputAction.Down);
            _inputService.RegisterKeyBinding(Key.Left, InputAction.Left);
            _inputService.RegisterKeyBinding(Key.Right, InputAction.Right);
            _inputService.RegisterKeyBinding(Key.Enter, InputAction.Select);
            _inputService.RegisterKeyBinding(Key.Space, InputAction.Start);
            _inputService.RegisterKeyBinding(Key.Escape, InputAction.Back);
            _inputService.RegisterKeyBinding(Key.Back, InputAction.Exit);
            _inputService.RegisterKeyBinding(Key.F1, InputAction.Admin);
        }

        private void ConfigureIdleService()
        {
            _idleService.Configure(new IdleServiceOptions
            {
                AttractModeDelay = TimeSpan.FromMinutes(5),
                HeartbeatInterval = TimeSpan.FromSeconds(1),
                AutoEnterAttractMode = true,
                StartInAttractMode = false
            });
        }

        private void ConfigureInputCombos()
        {
            _inputComboService.RegisterCombos(new List<InputComboDefinition>
            {
                new InputComboDefinition
                {
                    Key = "admin-open",
                    DisplayName = "Admin Diagnostics",
                    Sequence = new[] { InputAction.Up, InputAction.Up, InputAction.Down, InputAction.Down, InputAction.Select },
                    MaxGapBetweenInputs = TimeSpan.FromSeconds(2),
                    IsEnabled = true
                },
                new InputComboDefinition
                {
                    Key = "reveal-video",
                    DisplayName = "Reveal Video Trigger",
                    Sequence = new[] { InputAction.Left, InputAction.Right, InputAction.Left, InputAction.Right, InputAction.Start },
                    MaxGapBetweenInputs = TimeSpan.FromSeconds(2),
                    IsEnabled = true
                },
                new InputComboDefinition
                {
                    Key = "toggle-attract",
                    DisplayName = "Toggle Attract Mode",
                    Sequence = new[] { InputAction.Back, InputAction.Back, InputAction.Start },
                    MaxGapBetweenInputs = TimeSpan.FromSeconds(2),
                    IsEnabled = true
                },
                new InputComboDefinition
                {
                    Key = "open-logs",
                    DisplayName = "Open Logs Folder",
                    Sequence = new[] { InputAction.Admin, InputAction.Admin, InputAction.Back },
                    MaxGapBetweenInputs = TimeSpan.FromSeconds(2),
                    IsEnabled = true
                }
            });
        }

        private void WireComboPipeline()
        {
            _inputService.InputReceived += HandleInputForCombos;
            _inputComboService.ComboMatched += HandleComboMatched;
        }

        private void HandleInputForCombos(object? sender, InputEvent e)
        {
            try
            {
                _inputComboService.ProcessInput(e);
            }
            catch (Exception ex)
            {
                _loggingService.Error(nameof(MainWindow), "Unhandled exception while processing combo input.", ex, $"Action: {e.Action}");
            }
        }

        private void HandleComboMatched(object? sender, InputComboMatchEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                _loggingService.Info(nameof(MainWindow), "Combo matched.", $"Key: {e.ComboKey}");

                switch (e.ComboKey)
                {
                    case "admin-open":
                        _inputService.RegisterExternalInput(InputAction.Admin, "ComboService");
                        break;

                    case "reveal-video":
                        var settings = _appSettingsService.Current;
                        var launched = _revealMediaService.TryLaunchRevealMedia(settings.RevealVideoPath);
                        if (!launched)
                        {
                            MessageBox.Show("Reveal combo detected, but no reveal video path was configured or found.", "Reveal Trigger", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        break;

                    case "toggle-attract":
                        if (_attractModeCoordinator.IsAttractModeActive)
                        {
                            _attractModeCoordinator.ForceExitAttractMode("Toggle attract combo");
                        }
                        else
                        {
                            _attractModeCoordinator.ForceEnterAttractMode("Toggle attract combo");
                        }
                        break;

                    case "open-logs":
                        OpenLogsFolder();
                        break;
                }
            });
        }

        private void OpenLogsFolder()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = _loggingService.GetLogDirectoryPath(),
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                _loggingService.Error(nameof(MainWindow), "Failed to open logs folder.", ex, _loggingService.GetLogDirectoryPath());
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                _inputService.HandleKeyDown(e.Key);
                e.Handled = true;
            }
            catch (Exception ex)
            {
                _loggingService.Error(nameof(MainWindow), "Unhandled exception while processing key input.", ex, $"Key: {e.Key}");
            }
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            try
            {
                _inputService.InputReceived -= HandleInputForCombos;
                _inputComboService.ComboMatched -= HandleComboMatched;
                _idleService.Stop();
                _attractModeCoordinator.Shutdown();
                _mainViewModel.Dispose();

                if (_gameLauncherService.GetTrackedProcess() != null)
                {
                    var terminateResult = _gameLauncherService.TerminateTrackedProcess();
                    _loggingService.Info(nameof(MainWindow), "Tracked process termination attempted during shutdown.", terminateResult.UserMessage);
                }

                _idleService.Dispose();
            }
            catch (Exception ex)
            {
                _loggingService.Error(nameof(MainWindow), "Unhandled exception during window closing.", ex, ex.Message);
            }
        }
    }
}
