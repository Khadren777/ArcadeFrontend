using System.ComponentModel;
using System.Runtime.CompilerServices;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.ViewModels
{
    public class MenuItemViewModel : INotifyPropertyChanged
    {
        private bool _isSelected;

        public string Label { get; set; } = string.Empty;
        public MenuAction Action { get; set; } = MenuAction.None;
        public string Value { get; set; } = string.Empty;
        public Game? Game { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value)
                {
                    return;
                }

                _isSelected = value;
                OnPropertyChanged();
            }
        }
        private bool _isLaunchAvailable = true;
        private string _launchIssue = string.Empty;

        public bool IsLaunchAvailable
        {
            get => _isLaunchAvailable;
            set
            {
                if (_isLaunchAvailable == value)
                {
                    return;
                }

                _isLaunchAvailable = value;
                OnPropertyChanged();
            }
        }

        public string LaunchIssue
        {
            get => _launchIssue;
            set
            {
                if (_launchIssue == value)
                {
                    return;
                }

                _launchIssue = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static MenuItemViewModel FromModel(MenuItemModel model)
        {
            return new MenuItemViewModel
            {
                Label = model.Label,
                Action = model.Action,
                Value = model.Value,
                Game = model.Game
            };
        }
    }
}