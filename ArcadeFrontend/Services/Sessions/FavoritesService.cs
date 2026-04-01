using ArcadeFrontend.Models;

/// <summary>
/// Owns favorite-toggle behavior and save-through persistence.
/// </summary>
namespace ArcadeFrontend.Services.Sessions;

public sealed class FavoritesService
{
    private readonly GameDataService _gameDataService;

    public FavoritesService(GameDataService gameDataService)
    {
        _gameDataService = gameDataService;
    }

    public void ToggleFavorite(Game game, IReadOnlyList<Game> allGames)
    {
        game.IsFavorite = !game.IsFavorite;
        _gameDataService.SaveGames(allGames.ToList());
    }
}
