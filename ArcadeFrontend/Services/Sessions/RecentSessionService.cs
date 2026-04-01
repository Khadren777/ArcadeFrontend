using ArcadeFrontend.Models;

/// <summary>
/// Owns recent-session tracking behavior and persistence coordination.
/// </summary>
namespace ArcadeFrontend.Services.Sessions;

public sealed class RecentSessionService
{
    private readonly RecentGamesService _recentGamesService;
    private readonly List<Game> _recentGames = new();

    /// <summary>
    /// Initializes the service with the existing recent-games persistence layer.
    /// </summary>
    public RecentSessionService(RecentGamesService recentGamesService)
    {
        _recentGamesService = recentGamesService;
    }

    /// <summary>
    /// Returns the current in-memory recent game list.
    /// </summary>
    public IReadOnlyList<Game> RecentGames => _recentGames;

    /// <summary>
    /// Loads recent games from persistence.
    /// </summary>
    public void Load()
    {
        _recentGames.Clear();
        _recentGames.AddRange(_recentGamesService.LoadRecentGames());
    }

    /// <summary>
    /// Records a launched game in the recent list and persists the result.
    /// </summary>
    public void RecordLaunch(Game game, int maxItems = 10)
    {
        _recentGames.RemoveAll(existing => string.Equals(existing.Title, game.Title, StringComparison.OrdinalIgnoreCase));
        _recentGames.Insert(0, game);

        if (_recentGames.Count > maxItems)
        {
            _recentGames.RemoveRange(maxItems, _recentGames.Count - maxItems);
        }

        _recentGamesService.SaveRecentGames(_recentGames);
    }
}
