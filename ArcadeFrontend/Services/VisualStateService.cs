using System.IO;
using ArcadeFrontend.Models;

/// <summary>
/// Builds screen-appropriate visual state for the frontend.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class VisualStateService
{
    private readonly PathService _pathService;

    public VisualStateService(PathService pathService)
    {
        _pathService = pathService;
    }

    public VisualStateSnapshot Build(ScreenType currentScreen, string selectedSystem, AppSettings settings, bool adminUnlocked)
    {
        string backgroundImagePath = ResolveBackgroundImage(currentScreen, selectedSystem, settings);
        string subtitle = BuildSubtitle(currentScreen, selectedSystem);
        string attractVideoPath = ResolveOptionalPath(settings.AttractVideoPath);

        return new VisualStateSnapshot
        {
            BackgroundImagePath = backgroundImagePath,
            AttractVideoPath = attractVideoPath,
            ShowAttractVideo = currentScreen == ScreenType.AttractMode &&
                               settings.UseAttractModeVideo &&
                               !string.IsNullOrWhiteSpace(attractVideoPath),
            ShowDiagnosticsPanel = currentScreen == ScreenType.AdminMenu && adminUnlocked,
            DimBackgroundUnderVideo = settings.DimBackgroundUnderVideo,
            SubtitleText = subtitle
        };
    }

    private string ResolveBackgroundImage(ScreenType currentScreen, string selectedSystem, AppSettings settings)
    {
        if (!settings.EnableBackgroundImages)
        {
            return string.Empty;
        }

        return currentScreen switch
        {
            ScreenType.MainMenu => ResolveOptionalPath(settings.MainMenuBackgroundPath),
            ScreenType.SystemsMenu => ResolveOptionalPath(settings.SystemsBackgroundPath),
            ScreenType.AdminMenu => ResolveOptionalPath(settings.AdminBackgroundPath),
            ScreenType.FavoritesMenu => ResolveOptionalPath(settings.FavoritesBackgroundPath),
            ScreenType.RecentGamesMenu => ResolveOptionalPath(settings.RecentBackgroundPath),
            ScreenType.HiddenGamesMenu => ResolveOptionalPath(settings.HiddenGamesBackgroundPath),
            ScreenType.AttractMode => ResolveOptionalPath(settings.AttractModeBackgroundPath),
            ScreenType.GamesMenu => ResolveSystemBackground(selectedSystem, settings),
            _ => ResolveOptionalPath(settings.MainMenuBackgroundPath)
        };
    }

    private string ResolveSystemBackground(string selectedSystem, AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(selectedSystem))
        {
            return ResolveOptionalPath(settings.SystemsBackgroundPath);
        }

        string folder = settings.BackgroundImageFolder ?? string.Empty;
        string jpgCandidate = Path.Combine(folder, $"{selectedSystem}.jpg");
        string pngCandidate = Path.Combine(folder, $"{selectedSystem}.png");

        string resolvedJpg = ResolveOptionalPath(jpgCandidate);
        if (!string.IsNullOrWhiteSpace(resolvedJpg))
        {
            return resolvedJpg;
        }

        string resolvedPng = ResolveOptionalPath(pngCandidate);
        if (!string.IsNullOrWhiteSpace(resolvedPng))
        {
            return resolvedPng;
        }

        return ResolveOptionalPath(settings.SystemsBackgroundPath);
    }

    private string BuildSubtitle(ScreenType currentScreen, string selectedSystem)
    {
        return currentScreen switch
        {
            ScreenType.MainMenu => "Choose your path",
            ScreenType.SystemsMenu => "Select a system",
            ScreenType.GamesMenu => string.IsNullOrWhiteSpace(selectedSystem) ? "Browse games" : $"System: {selectedSystem}",
            ScreenType.RecentGamesMenu => "Jump back in",
            ScreenType.FavoritesMenu => "Your curated list",
            ScreenType.AdminMenu => "Service and diagnostics",
            ScreenType.HiddenGamesMenu => "Restricted library",
            ScreenType.AttractMode => "Press any key to return",
            _ => string.Empty
        };
    }

    private string ResolveOptionalPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        string resolved = _pathService.Resolve(configuredPath);
        return File.Exists(resolved) ? resolved : string.Empty;
    }
}
