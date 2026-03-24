namespace ArcadeFrontend.Models
{
    public class MenuScreen
    {
        public string Title { get; set; } = string.Empty;
        public List<MenuItemModel> Items { get; set; } = new();
    }
}