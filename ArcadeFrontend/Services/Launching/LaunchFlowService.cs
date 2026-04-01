using ArcadeFrontend.Models;
using ArcadeFrontend.Services.Library;
using ArcadeFrontend.Services.Sessions;

/// <summary>
/// Coordinates launch attempts and post-launch bookkeeping.
/// </summary>
namespace ArcadeFrontend.Services.Launching;

public sealed class LaunchFlowService
{
    private readonly GameLauncherService _gameLauncherService;
    private readonly LibraryService _libraryService;
    private readonly RecentSessionService _recentSessionService;

    /// <summary>
    /// Initializes the launch flow service.
    /// </summary>
    public LaunchFlowService(
        GameLauncherService gameLauncherService,
        LibraryService libraryService,
        RecentSessionService recentSessionService)
    {
        _gameLauncherService = gameLauncherService;
        _libraryService = libraryService;
        _recentSessionService = recentSessionService;
    }

    /// <summary>
    /// Attempts to launch a game and records a successful launch.
    /// </summary>
    public LaunchResult Launch(Game game)
    {
        try
        {
            _gameLauncherService.LaunchGame(game, _libraryService.EmulatorProfiles.ToList());
            _recentSessionService.RecordLaunch(game);
            return LaunchResult.Succeeded($"Launched: {game.Title}");
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed($"Could not launch '{game.Title}'. {ex.Message}");
        }
    }
}
