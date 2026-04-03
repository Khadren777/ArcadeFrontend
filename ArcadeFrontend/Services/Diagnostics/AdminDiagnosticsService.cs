using ArcadeFrontend.Models;

/// <summary>
/// Provides summarized admin diagnostics for the frontend.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class AdminDiagnosticsService
{
    private readonly LoggingService _loggingService;
    private readonly SettingsService _settingsService;

    public AdminDiagnosticsService(LoggingService loggingService, SettingsService settingsService)
    {
        _loggingService = loggingService;
        _settingsService = settingsService;
    }

    public IReadOnlyList<AppLogEntry> GetRecentLogs()
    {
        return _loggingService.Entries;
    }

    public string BuildSummary()
    {
        AppSettings settings = _settingsService.LoadSettings();
        IReadOnlyList<AppLogEntry> logs = _loggingService.Entries;

        return $"Settings loaded | Attract: {settings.AttractModeTimeoutSeconds}s | Max recents: {settings.MaxRecentGames} | Logs: {logs.Count}";
    }
}
