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
        private readonly ArcadeFrontend.ViewModels.MainViewModel _mainViewModel;
        private readonly IInputComboService _inputComboService;
        private readonly IAppSettingsService _appSettingsService;
        private readonly RevealMediaService _revealMediaService;

        public MainWindow(
            ILoggingService loggingService,
            IPathService pathService,
            IAppStartupCoordinator appStartupCoordinator,
            IInputAbstractionService inputService,
            IGameLauncherService gameLauncherService,
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
            PreviewKeyDown += OnKeyDown;
            Closing += OnClosing;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                _loggingService.Info(nameof(MainWindow), "Main window loaded. Startup initialization beginning.");
                ConfigureInputBindings();
                MainWindowConfigurator.Configure(_inputService, _idleService, _inputComboService, _attractModeCoordinator, _appSettingsService, _revealMediaService, _loggingService);
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
                _mainViewModel.Initialize(
                startupData.GameData,
                startupData.EmulatorProfiles,
                "MainMenu",
                _diagnosticsSummaryBuilder.BuildStartupSummary(startupData));

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


        private void OnSelectClicked(object sender, RoutedEventArgs e)
        {
            _inputService.RegisterExternalInput(InputAction.Select, "UI");
        }

        private void OnBackClicked(object sender, RoutedEventArgs e)
        {
            _inputService.RegisterExternalInput(InputAction.Back, "UI");
        }

        private void OnDiagnosticsClicked(object sender, RoutedEventArgs e)
        {
            _inputService.RegisterExternalInput(InputAction.Admin, "UI");
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            _inputService.RegisterExternalInput(InputAction.Exit, "UI");
        }

        private void ConfigureInputBindings()
        {
            _inputService.RegisterKeyBinding(Key.Up, InputAction.Up);
            _inputService.RegisterKeyBinding(Key.Down, InputAction.Down);
            _inputService.RegisterKeyBinding(Key.Left, InputAction.Left);
            _inputService.RegisterKeyBinding(Key.Right, InputAction.Right);
            _inputService.RegisterKeyBinding(Key.Escape, InputAction.Back);
            _inputService.RegisterKeyBinding(Key.Space, InputAction.Start);
            _inputService.RegisterKeyBinding(Key.Enter, InputAction.Select);
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _inputService.HandleKeyDown(e.Key);
        }

        private void OnClosing(object sender, CancelEventArgs e)
        {
            _loggingService.Info(nameof(MainWindow), "Main window closing.");
            _idleService.Stop();
            _attractModeCoordinator.Shutdown();
        }
    }
}