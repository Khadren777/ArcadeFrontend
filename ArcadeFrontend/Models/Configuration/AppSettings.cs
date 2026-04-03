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
}
