using System.Windows;
using System.Windows.Input;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;
using ArcadeFrontend.Services.Input;
using ArcadeFrontend.ViewModels;

/// <summary>
/// Main application window.
///
/// Responsible only for:
/// - composing services and view models
/// - forwarding input into the input pipeline
/// - handling window lifecycle concerns
/// </summary>
namespace ArcadeFrontend;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shellViewModel;
    private readonly KeyboardInputMapper _keyboardInputMapper;
    private readonly InputRouterService _inputRouterService;

    /// <summary>
    /// Initializes the application shell and composes dependencies.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        _keyboardInputMapper = new KeyboardInputMapper();
        _inputRouterService = new InputRouterService();

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // IMPORTANT:
        // Keep this constructor block aligned to your CURRENT local repo state.
        // If you already added PathService and GameLauncherService(pathService),
        // preserve that exact wiring here.
        var pathService = new PathService();

        var mainWindowViewModel = new MainWindowViewModel(
            new GameDataService(baseDirectory),
            new RecentGamesService(baseDirectory),
            new GameLauncherService(pathService),
            new AdminUnlockService(new[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Enter }),
            new AttractModeService(TimeSpan.FromSeconds(15)),
            new MenuDefinitionService(),
            pathService);

        _shellViewModel = new ShellViewModel(mainWindowViewModel);

        // Transitional: UI still binds to MainWindowViewModel while shell/input
        // responsibilities are being introduced.
        DataContext = _shellViewModel.Main;
    }

    /// <summary>
    /// Handles initial window load and triggers application initialization.
    /// </summary>
    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Focus();
        _shellViewModel.Main.Initialize();
    }

    /// <summary>
    /// Routes keyboard input through the action-based input pipeline.
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        AppAction action = _keyboardInputMapper.Map(e.Key);
        bool handled = _inputRouterService.Route(action, _shellViewModel, out string? errorMessage);

        if (!handled)
        {
            // Transitional fallback: keep legacy key routing available while
            // the new input model is being phased in.
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
        }
    }
}