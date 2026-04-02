using System;
using System.Linq;
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
    private readonly SettingsService _settingsService;

    public LaunchFlowService(
        GameLauncherService gameLauncherService,
        LibraryService libraryService,
        RecentSessionService recentSessionService,
        SettingsService settingsService)
    {
        _gameLauncherService = gameLauncherService;
        _libraryService = libraryService;
        _recentSessionService = recentSessionService;
        _settingsService = settingsService;
    }

    public LaunchResult Launch(Game game)
    {
        try
        {
            AppSettings settings = _settingsService.LoadSettings();
            _gameLauncherService.LaunchGame(game, _libraryService.EmulatorProfiles.ToList());
            _recentSessionService.RecordLaunch(game, settings.MaxRecentGames);
            return LaunchResult.Succeeded($"Launched: {game.Title}");
        }
        catch (Exception ex)
        {
            return LaunchResult.Failed($"Could not launch '{game.Title}'. {ex.Message}");
        }
    }
}
