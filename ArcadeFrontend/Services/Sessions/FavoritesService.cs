using ArcadeFrontend.Models;

/// <summary>
/// Owns favorite-toggle behavior and save-through persistence.
/// </summary>
namespace ArcadeFrontend.Services.Sessions;

public sealed class FavoritesService
{
    private readonly GameDataService _gameDataService;

    /// <summary>
    /// Initializes the favorites service.
    /// </summary>
    public FavoritesService(GameDataService gameDataService)
    {
        _gameDataService = gameDataService;
    }

    /// <summary>
    /// Toggles favorite state for a game and persists the library.
    /// </summary>
    public void ToggleFavorite(Game game, IReadOnlyList<Game> allGames)
    {
        game.IsFavorite = !game.IsFavorite;
        _gameDataService.SaveGames(allGames.ToList());
    }
}
