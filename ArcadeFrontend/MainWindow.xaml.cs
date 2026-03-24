using System.Windows;
using System.Windows.Input;
using ArcadeFrontend.Services;
using ArcadeFrontend.ViewModels;

namespace ArcadeFrontend
{
    public partial class MainWindow : Window
    {
        private readonly MainWindowViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

            _viewModel = new MainWindowViewModel(
                new GameDataService(baseDirectory),
                new RecentGamesService(baseDirectory),
                new GameLauncherService(),
                new AdminUnlockService(new[]
                {
                    Key.Up, Key.Up, Key.Down, Key.Down, Key.Enter
                }),
                new AttractModeService(TimeSpan.FromSeconds(15)),
                new MenuDefinitionService());

            DataContext = _viewModel;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Focus();
            _viewModel.Initialize();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            bool handled = _viewModel.HandleKey(e.Key, out string? errorMessage);

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                MessageBox.Show(
                    errorMessage,
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            if (_viewModel.ShouldExit)
            {
                _viewModel.ClearExitRequest();
                Close();
                return;
            }

            if (handled)
            {
                e.Handled = true;
            }
        }
    }
}