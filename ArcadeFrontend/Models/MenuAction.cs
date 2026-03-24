namespace ArcadeFrontend.Models
{
    public enum MenuAction
    {
        None,

        Play,
        OpenSystems,
        OpenRecentGames,
        OpenHiddenGames,
        OpenSettings,
        OpenAdminMenu,
        OpenFavorites,
        ToggleFavorite,
        ExitApp,

        OpenSystemGames,

        LaunchGame,
        LaunchHiddenGame,
        LaunchRecentGame,

        RescanLibrary,
        InputTest,

        BackToMain,
        BackToSystems,
        BackToAdmin
    }
}