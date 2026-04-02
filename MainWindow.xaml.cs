using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.Services.Input;
using ArcadeFrontend.Services.Launching;
using ArcadeFrontend.Services.Library;
using ArcadeFrontend.Services.Navigation;
using ArcadeFrontend.Services.Sessions;
using ArcadeFrontend.Services.State;
using ArcadeFrontend.ViewModels;

namespace ArcadeFrontend;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shellViewModel;
    private readonly KeyboardInputMapper _keyboardInputMapper;
    private readonly InputRouterService _inputRouterService;
    private readonly InputRepeatService _inputRepeatService;
    private readonly SettingsService _settingsService;
    private readonly SoundStateService _soundStateService;
    private readonly AudioCueService _audioCueService;
    private readonly RevealMediaService _revealMediaService;
    private readonly MediaPlayer _ambientMusicPlayer = new();

    public MainWindow()
    {
        InitializeComponent();

        _keyboardInputMapper = new KeyboardInputMapper();
        _inputRouterService = new InputRouterService();
        _inputRepeatService = new InputRepeatService(IsRepeatableKey);

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
        var uiStateStoreService = new UiStateStoreService(baseDirectory);
        _soundStateService = new SoundStateService(pathService);
        _audioCueService = new AudioCueService();
        _revealMediaService = new RevealMediaService(pathService);
        var launchGuardService = new LaunchGuardService();
        var revealSequenceService = new SecretSequenceService(new[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Enter });

        AppSettings settings = _settingsService.LoadSettings();
        _inputRepeatService.Configure(settings.InputRepeatInitialDelayMs, settings.InputRepeatIntervalMs);

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
            visualStateService,
            uiStateStoreService,
            _soundStateService,
            _audioCueService,
            launchGuardService,
            revealSequenceService);

        _shellViewModel = new ShellViewModel(mainWindowViewModel);
        DataContext = _shellViewModel.Main;
        _shellViewModel.Main.PropertyChanged += MainViewModel_PropertyChanged;
        _ambientMusicPlayer.MediaEnded += AmbientMusicPlayer_MediaEnded;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Focus();
        _shellViewModel.Main.Initialize();
        RefreshMediaPlayback();
        RefreshAmbientMusic();
        TryPlayPendingSound();
    }

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        ProcessKey(e.Key);
        _inputRepeatService.HandleKeyDown(e.Key, key => Dispatcher.Invoke(() => ProcessKey(key)));
        e.Handled = true;
    }

    private void Window_KeyUp(object sender, KeyEventArgs e)
    {
        _inputRepeatService.HandleKeyUp(e.Key);
    }

    private void Window_Closing(object sender, CancelEventArgs e)
    {
        _inputRepeatService.Stop();
        StopAttractVideo();
        StopAmbientMusic();
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

    private void AmbientMusicPlayer_MediaEnded(object? sender, EventArgs e)
    {
        try
        {
            _ambientMusicPlayer.Position = TimeSpan.Zero;
            _ambientMusicPlayer.Play();
        }
        catch
        {
        }
    }

    private void MainViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainWindowViewModel.ShowAttractVideo) ||
            e.PropertyName == nameof(MainWindowViewModel.AttractVideoPath))
            RefreshMediaPlayback();

        if (e.PropertyName == nameof(MainWindowViewModel.PendingSoundEffect))
            TryPlayPendingSound();

        if (e.PropertyName == nameof(MainWindowViewModel.SoundState))
            RefreshAmbientMusic();

        if (e.PropertyName == nameof(MainWindowViewModel.RevealRequested))
            TryConsumeReveal();
    }

    private void ProcessKey(Key key)
    {
        AppAction action = _keyboardInputMapper.Map(key);
        bool handled = _inputRouterService.Route(action, _shellViewModel, out string? errorMessage);

        if (!handled)
            handled = _shellViewModel.Main.HandleKey(key, out errorMessage);

        if (!string.IsNullOrWhiteSpace(errorMessage))
            MessageBox.Show(errorMessage, "Launch Error", MessageBoxButton.OK, MessageBoxImage.Error);

        if (_shellViewModel.Main.ShouldExit)
        {
            _shellViewModel.Main.ClearExitRequest();
            Close();
            return;
        }

        if (handled)
        {
            RefreshMediaPlayback();
            RefreshAmbientMusic();
            TryPlayPendingSound();
            TryConsumeReveal();
        }
    }

    private void TryConsumeReveal()
    {
        if (!_shellViewModel.Main.ConsumeRevealRequested())
            return;

        AppSettings settings = _settingsService.LoadSettings();
        string revealVideoPath = _revealMediaService.ResolveRevealVideoPath(settings.RevealVideoPath);

        if (string.IsNullOrWhiteSpace(revealVideoPath))
        {
            MessageBox.Show(
                "Reveal sequence detected, but the reveal video file was not found.",
                "Hidden Reveal",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = revealVideoPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Reveal video launch failed. {ex.Message}",
                "Hidden Reveal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private static bool IsRepeatableKey(Key key)
    {
        return key == Key.Up || key == Key.Down || key == Key.Left || key == Key.Right;
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

    private void RefreshAmbientMusic()
    {
        SoundStateSnapshot state = _shellViewModel.Main.SoundState;
        if (!state.EnableAmbientMusic || string.IsNullOrWhiteSpace(state.AmbientMusicPath))
        {
            StopAmbientMusic();
            return;
        }

        try
        {
            Uri source = new Uri(state.AmbientMusicPath, UriKind.Absolute);
            if (_ambientMusicPlayer.Source == null || _ambientMusicPlayer.Source != source)
                _ambientMusicPlayer.Open(source);

            _ambientMusicPlayer.Volume = state.MasterVolume * state.AmbientMusicVolume;
            _ambientMusicPlayer.Play();
        }
        catch
        {
            StopAmbientMusic();
        }
    }

    private void TryPlayPendingSound()
    {
        SoundEffectType effect = _shellViewModel.Main.ConsumePendingSoundEffect();
        if (effect == SoundEffectType.None) return;

        SoundStateSnapshot state = _shellViewModel.Main.SoundState;
        if (!state.EnableMenuSounds) return;

        string path = effect switch
        {
            SoundEffectType.MenuMove => state.MenuMoveSoundPath,
            SoundEffectType.MenuSelect => state.MenuSelectSoundPath,
            SoundEffectType.MenuBack => state.MenuBackSoundPath,
            SoundEffectType.Launch => state.LaunchSoundPath,
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(path)) return;

        try
        {
            MediaPlayer player = new MediaPlayer();
            player.Open(new Uri(path, UriKind.Absolute));
            player.Volume = state.MasterVolume * state.MenuSoundVolume;
            player.Play();
        }
        catch
        {
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

    private void StopAmbientMusic()
    {
        try
        {
            _ambientMusicPlayer.Stop();
            _ambientMusicPlayer.Close();
        }
        catch
        {
        }
    }
}
