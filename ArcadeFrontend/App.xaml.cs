using System;
using System.IO;
using System.Windows;
using ArcadeFrontend.Services;
using ArcadeFrontend.ViewModels;

namespace ArcadeFrontend
{
    public partial class App : Application
    {
        private ILoggingService? _loggingService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                var appRootPath = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                _loggingService = new LoggingService(appRootPath);

                RegisterGlobalExceptionHandlers();

                _loggingService.Info(nameof(App), "Application startup beginning.", $"AppRoot: {appRootPath}");

                var mainWindow = BuildMainWindow(appRootPath);
                MainWindow = mainWindow;
                mainWindow.Show();

                _loggingService.Info(nameof(App), "Main window created and shown.");
            }
            catch (Exception ex)
            {
                _loggingService?.Critical(nameof(App), "Fatal exception during application startup.", ex, ex.Message);

                MessageBox.Show(
                    ex.ToString(),
                    "Fatal Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Shutdown(-1);
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                _loggingService?.Info(nameof(App), $"Application exiting with code {e.ApplicationExitCode}.");
            }
            catch
            {
            }

            base.OnExit(e);
        }

        private void RegisterGlobalExceptionHandlers()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        }

        private MainWindow BuildMainWindow(string appRootPath)
        {
            var pathService = new PathService(appRootPath, _loggingService);
            var gameDataService = new GameDataService(_loggingService!);
            var startupValidationService = new StartupValidationService(_loggingService!);
            var inputService = new InputAbstractionService(_loggingService!);
            var inputComboService = new InputComboService(_loggingService!);
            var gameLauncherService = new GameLauncherService(_loggingService!);
            var navigationStateService = new NavigationStateService(_loggingService!);
            var idleService = new IdleService(_loggingService!);
            var diagnosticsSummaryBuilder = new DiagnosticsSummaryBuilder();
            var appSettingsService = new AppSettingsService(pathService, _loggingService!);
            var revealMediaService = new RevealMediaService(pathService, _loggingService!);
            var attractModeCoordinator = new AttractModeCoordinator(idleService, navigationStateService, _loggingService!);
            var appStartupCoordinator = new AppStartupCoordinator(
                pathService,
                _loggingService!,
                startupValidationService,
                gameDataService);

            appSettingsService.Load();

            var mainViewModel = new MainViewModel(
                inputService,
                gameLauncherService,
                gameDataService,
                navigationStateService,
                attractModeCoordinator,
                _loggingService!,
                diagnosticsSummaryBuilder);

            return new MainWindow(
                loggingService: _loggingService!,
                pathService: pathService,
                gameDataService: gameDataService,
                startupValidationService: startupValidationService,
                appStartupCoordinator: appStartupCoordinator,
                inputService: inputService,
                gameLauncherService: gameLauncherService,
                navigationStateService: navigationStateService,
                idleService: idleService,
                attractModeCoordinator: attractModeCoordinator,
                diagnosticsSummaryBuilder: diagnosticsSummaryBuilder,
                mainViewModel: mainViewModel,
                inputComboService: inputComboService,
                appSettingsService: appSettingsService,
                revealMediaService: revealMediaService);
        }

        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                _loggingService?.Critical(nameof(App), "Unhandled dispatcher exception.", e.Exception, e.Exception.Message);
            }
            finally
            {
                MessageBox.Show(
                    e.Exception.ToString(),
                    "Unhandled UI Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                e.Handled = true;
            }
        }

        private void OnCurrentDomainUnhandledException(object? sender, UnhandledExceptionEventArgs e)
        {
            try
            {
                if (e.ExceptionObject is Exception ex)
                {
                    _loggingService?.Critical(nameof(App), "Unhandled AppDomain exception.", ex, ex.Message);
                }
                else
                {
                    _loggingService?.Critical(nameof(App), "Unhandled non-exception AppDomain error.", details: e.ExceptionObject?.ToString());
                }
            }
            catch
            {
            }
        }
    }
}
