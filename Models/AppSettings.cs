using System;

/// <summary>
/// Stores configurable frontend settings.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class AppSettings
{
    public int AttractModeTimeoutSeconds { get; set; } = 15;
    public int MaxRecentGames { get; set; } = 10;
    public bool ShowHiddenGamesInAdmin { get; set; } = true;
    public bool EnableLaunchLogging { get; set; } = true;
    public bool EnableDiagnosticLogging { get; set; } = true;

    public bool EnableBackgroundImages { get; set; } = true;
    public bool UseAttractModeVideo { get; set; } = false;

    public string BackgroundImageFolder { get; set; } = @"Assets\Backgrounds";
    public string MainMenuBackgroundPath { get; set; } = @"Assets\Backgrounds\main.jpg";
    public string SystemsBackgroundPath { get; set; } = @"Assets\Backgrounds\systems.jpg";
    public string AdminBackgroundPath { get; set; } = @"Assets\Backgrounds\admin.jpg";
    public string FavoritesBackgroundPath { get; set; } = @"Assets\Backgrounds\favorites.jpg";
    public string RecentBackgroundPath { get; set; } = @"Assets\Backgrounds\recent.jpg";
    public string AttractVideoPath { get; set; } = @"Assets\Video\attract.mp4";
}
