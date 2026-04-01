using System.Windows;
using System.Windows.Input;
using ArcadeFrontend.Services;
using ArcadeFrontend.ViewModels;

/// <summary>
/// Main application window.
/// 
/// Responsible only for:
/// - Composing services and view models
/// - Forwarding input
/// - Handling window lifecycle
/// 
/// All business logic remains outside this class.
/// </summary>
namespace ArcadeFrontend;

public partial class MainWindow : Window
{
    private readonly ShellViewModel _shellViewModel;

    /// <summary>
    /// Initializes the application shell and composes dependencies.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        var mainWindowViewModel = new MainWindowViewModel(
            new GameDataService(baseDirectory),
            new RecentGamesService(baseDirectory),
            new GameLauncherService(),
            new AdminUnlockService(new[] { Key.Up, Key.Up, Key.Down, Key.Down, Key.Enter }),
            new AttractModeService(TimeSpan.FromSeconds(15)),
            new MenuDefinitionService());

        _shellViewModel = new ShellViewModel(mainWindowViewModel);

        // Transitional: still binding directly to MainWindowViewModel
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
    /// Routes keyboard input into the application.
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        bool handled = _shellViewModel.Main.HandleKey(e.Key, out string? errorMessage);

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