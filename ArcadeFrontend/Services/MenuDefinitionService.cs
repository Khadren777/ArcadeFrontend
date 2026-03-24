using ArcadeFrontend.Models;
using System.Windows.Controls;

namespace ArcadeFrontend.Services
{
    public class MenuDefinitionService
    {
        public MenuScreen BuildMainMenu()
        {
            return new MenuScreen
            {
                Title = "ARCADE FRONTEND",
                Items = new List<MenuItemModel>
                {
                    new() { Label = "Play", Action = MenuAction.Play },
                    new() { Label = "Favorites", Action = MenuAction.OpenFavorites },
                    new() { Label = "Systems", Action = MenuAction.OpenSystems },
                    new() { Label = "Recent Games", Action = MenuAction.OpenRecentGames },
                    new() { Label = "Hidden Games", Action = MenuAction.OpenHiddenGames },
                    new() { Label = "Settings", Action = MenuAction.OpenSettings },
                    new() { Label = "Exit", Action = MenuAction.ExitApp }
                }
            };
        }

        public MenuScreen BuildSystemsMenu()
        {
            return new MenuScreen
            {
                Title = "SYSTEMS",
                Items = new List<MenuItemModel>
                {
                    new() { Label = "Arcade", Action = MenuAction.OpenSystemGames, Value = "Arcade" },
                    new() { Label = "SNES", Action = MenuAction.OpenSystemGames, Value = "SNES" },
                    new() { Label = "Genesis", Action = MenuAction.OpenSystemGames, Value = "Genesis" },
                    new() { Label = "PlayStation", Action = MenuAction.OpenSystemGames, Value = "PlayStation" },
                    new() { Label = "PC Games", Action = MenuAction.OpenSystemGames, Value = "PC Games" },
                    new() { Label = "Back", Action = MenuAction.BackToMain }
                }
            };
        }

        public MenuScreen BuildFavoritesMenu(IEnumerable<Game> games)
        {
            List<MenuItemModel> items = games
                .Where(g => g.IsFavorite)
                .Select(g => new MenuItemModel
                {
                    Label = $"{g.Title} [{g.System}]",
                    Action = MenuAction.LaunchGame,
                    Game = g
                })
                .ToList();

            if (items.Count == 0)
            {
                items.Add(new MenuItemModel
                {
                    Label = "No favorites yet",
                    Action = MenuAction.None
                });
            }

            items.Add(new MenuItemModel
            {
                Label = "Back",
                Action = MenuAction.BackToMain
            });

            return new MenuScreen
            {
                Title = "FAVORITES",
                Items = items
            };
        }

        public MenuScreen BuildAdminMenu()
        {
            return new MenuScreen
            {
                Title = "ADMIN",
                Items = new List<MenuItemModel>
                {
                    new() { Label = "Hidden Games", Action = MenuAction.OpenHiddenGames },
                    new() { Label = "Rescan Library", Action = MenuAction.RescanLibrary },
                    new() { Label = "Input Test", Action = MenuAction.InputTest },
                    new() { Label = "Settings", Action = MenuAction.OpenSettings },
                    new() { Label = "Back", Action = MenuAction.BackToMain }
                }
            };
        }

        public MenuScreen BuildGamesMenu(string system, IEnumerable<Game> games)
        {
            List<MenuItemModel> items = games
                .Where(g => g.System == system && !g.IsHidden)
                .Select(g => new MenuItemModel
                {
                    Label = g.Title,
                    Action = MenuAction.LaunchGame,
                    Game = g
                })
                .ToList();

            items.Add(new MenuItemModel
            {
                Label = "Back",
                Action = MenuAction.BackToSystems
            });

            return new MenuScreen
            {
                Title = $"{system.ToUpper()} GAMES",
                Items = items
            };
        }

        public MenuScreen BuildHiddenGamesMenu(IEnumerable<Game> games)
        {
            List<MenuItemModel> items = games
                .Where(g => g.IsHidden)
                .Select(g => new MenuItemModel
                {
                    Label = g.Title,
                    Action = MenuAction.LaunchHiddenGame,
                    Game = g
                })
                .ToList();

            items.Add(new MenuItemModel
            {
                Label = "Back",
                Action = MenuAction.BackToAdmin
            });

            return new MenuScreen
            {
                Title = "HIDDEN GAMES",
                Items = items
            };
        }

        public MenuScreen BuildRecentGamesMenu(IEnumerable<Game> recentGames)
        {
            List<MenuItemModel> items = recentGames
                .Select(g => new MenuItemModel
                {
                    Label = $"{g.Title} [{g.System}]",
                    Action = MenuAction.LaunchRecentGame,
                    Game = g
                })
                .ToList();

            if (items.Count == 0)
            {
                items.Add(new MenuItemModel
                {
                    Label = "No recent games",
                    Action = MenuAction.None
                });
            }

            items.Add(new MenuItemModel
            {
                Label = "Back",
                Action = MenuAction.BackToMain
            });

            return new MenuScreen
            {
                Title = "RECENT GAMES",
                Items = items
            };
        }

        public MenuScreen BuildAttractModeScreen(IEnumerable<Game> recentGames)
        {
            List<MenuItemModel> items = recentGames
                .Take(5)
                .Select(g => new MenuItemModel
                {
                    Label = $"{g.Title} [{g.System}]",
                    Action = MenuAction.None,
                    Game = g
                })
                .ToList();

            if (items.Count == 0)
            {
                items.Add(new MenuItemModel
                {
                    Label = "No recent games yet",
                    Action = MenuAction.None
                });
            }

            return new MenuScreen
            {
                Title = "ATTRACT MODE",
                Items = items
            };
        }
    }
}