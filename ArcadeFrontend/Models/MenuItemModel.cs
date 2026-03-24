namespace ArcadeFrontend.Models
{
    public class MenuItemModel
    {
        public string Label { get; set; } = string.Empty;
        public MenuAction Action { get; set; } = MenuAction.None;
        public string Value { get; set; } = string.Empty;
        public Game? Game { get; set; }
    }
}