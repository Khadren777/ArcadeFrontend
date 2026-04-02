using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.Services.Input;
using ArcadeFrontend.Services.Launching;
using ArcadeFrontend.Services.Library;
using ArcadeFrontend.Services.Navigation;
using ArcadeFrontend.Services.Sessions;
using ArcadeFrontend.Services.State;
using ArcadeFrontend.ViewModels;

/// <summary>
/// Main application window.
/// </summary>
namespace ArcadeFrontend;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shellViewModel;
    private readonly KeyboardInputMapper _keyboardInputMapper;
    private readonly InputRouterService _inputRouterService;
    private readonly SettingsService _settingsService;

    public MainWindow()
    {
        InitializeComponent();

        _keyboardInputMapper = new KeyboardInputMapper();
        _inputRouterService = new InputRouterService();

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var pathService = new PathService();
        var gameDataService = new GameDataService(baseDirectory);
        var recentGamesService = new RecentGamesService(baseDirectory);
        var libraryService = new LibraryService(gameDataService);
        var recentSessionService = new RecentSessionService(recentGamesService);
        var favoritesService = new FavoritesService(gameDataService);
        var adminUnlockService = new AdminUnlockService(new[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Enter });
        var adminStateService = new AdminStateService(adminUnlockService);
        _settingsService = new SettingsService(baseDirectory);
        var loggingService = new LoggingService();
        var adminDiagnosticsService = new AdminDiagnosticsService(loggingService, _settingsService);
        var visualStateService = new VisualStateService(pathService);

        AppSettings settings = _settingsService.LoadSettings();
        var attractModeService = new AttractModeService(TimeSpan.FromSeconds(settings.AttractModeTimeoutSeconds));
        var idleStateService = new IdleStateService(attractModeService);
        var navigationStateService = new NavigationStateService();
        var gameLauncherService = new GameLauncherService(pathService);
        var launchFlowService = new LaunchFlowService(
            gameLauncherService,
            libraryService,
            recentSessionService,
            _settingsService);

        var mainWindowViewModel = new MainWindowViewModel(
            libraryService,
            launchFlowService,
            navigationStateService,
            recentSessionService,
            favoritesService,
            adminStateService,
            idleStateService,
            new MenuDefinitionService(),
            pathService,
            _settingsService,
            loggingService,
            adminDiagnosticsService,
            visualStateService);

        _shellViewModel = new ShellViewModel(mainWindowViewModel);
        DataContext = _shellViewModel.Main;

        _shellViewModel.Main.PropertyChanged += MainViewModel_PropertyChanged;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Focus();
        _shellViewModel.Main.Initialize();
        RefreshMediaPlayback();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        AppAction action = _keyboardInputMapper.Map(e.Key);
        bool handled = _inputRouterService.Route(action, _shellViewModel, out string? errorMessage);

        if (!handled)
        {
            handled = _shellViewModel.Main.HandleKey(e.Key, out errorMessage);
        }

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            MessageBox.Show(
                errorMessage,
                "Launch Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        if (_shellViewModel.Main.ShouldExit)
        {
            _shellViewModel.Main.ClearExitRequest();
            Close();
            return;
        }

        if (handled)
        {
            e.Handled = true;
            RefreshMediaPlayback();
        }
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        StopAttractVideo();
    }

    private void AttractVideoPlayer_MediaEnded(object sender, RoutedEventArgs e)
    {
        AppSettings settings = _settingsService.LoadSettings();

        if (settings.LoopAttractVideo)
        {
            AttractVideoPlayer.Position = TimeSpan.Zero;
            AttractVideoPlayer.Play();
        }
    }

    private void AttractVideoPlayer_MediaFailed(object sender, ExceptionRoutedEventArgs e)
    {
        StopAttractVideo();
    }

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.ShowAttractVideo) ||
            e.PropertyName == nameof(MainWindowViewModel.AttractVideoPath))
        {
            RefreshMediaPlayback();
        }
    }

    private void RefreshMediaPlayback()
    {
        if (_shellViewModel.Main.ShowAttractVideo &&
            !string.IsNullOrWhiteSpace(_shellViewModel.Main.AttractVideoPath))
        {
            try
            {
                AttractVideoPlayer.Source = new Uri(_shellViewModel.Main.AttractVideoPath, UriKind.Absolute);
                AttractVideoPlayer.Position = TimeSpan.Zero;
                AttractVideoPlayer.Play();
            }
            catch
            {
                StopAttractVideo();
            }
        }
        else
        {
            StopAttractVideo();
        }
    }

    private void StopAttractVideo()
    {
        try
        {
            AttractVideoPlayer.Stop();
            AttractVideoPlayer.Source = null;
        }
        catch
        {
        }
    }
}
