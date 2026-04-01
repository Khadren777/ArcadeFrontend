using ArcadeFrontend.Models;

/// <summary>
/// Owns recent-session tracking behavior and persistence coordination.
/// </summary>
namespace ArcadeFrontend.Services.Sessions;

public sealed class RecentSessionService
{
    private readonly RecentGamesService _recentGamesService;
    private readonly List<Game> _recentGames = new();

    public RecentSessionService(RecentGamesService recentGamesService)
    {
        _recentGamesService = recentGamesService;
    }

    public IReadOnlyList<Game> RecentGames => _recentGames;

    public void Load()
    {
        _recentGames.Clear();
        _recentGames.AddRange(_recentGamesService.LoadRecentGames());
    }

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
