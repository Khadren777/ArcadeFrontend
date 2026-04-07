using CommunityToolkit.Mvvm.ComponentModel;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.ViewModels
{
    public partial class MenuItemViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isSelected;

        [ObservableProperty]
        private bool isLaunchAvailable = true;

        [ObservableProperty]
        private string launchIssue = string.Empty;

        public string Label { get; set; } = string.Empty;
        public MenuAction Action { get; set; } = MenuAction.None;
        public string Value { get; set; } = string.Empty;
        public Game? Game { get; set; }

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