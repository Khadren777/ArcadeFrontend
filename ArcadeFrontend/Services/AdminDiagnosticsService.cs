using ArcadeFrontend.Models;
using System.Linq;
using ArcadeFrontend.Models;
using ArcadeFrontend.Services;

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
        return _loggingService.GetRecentEntries()
    .Select(x => new AppLogEntry
    {
        TimestampUtc = x.TimestampUtc,
        Level = x.Level.ToString(),
        Source = x.Source,
        Message = x.Message,
        Details = x.Details,
        ExceptionType = x.ExceptionType,
        StackTrace = x.StackTrace
    })
    .ToList()
    .AsReadOnly();
    }

    public string BuildSummary()
    {
        AppSettings settings = _settingsService.LoadSettings();
        IReadOnlyList<AppLogEntry> logs = _loggingService.GetRecentEntries()
    .Select(x => new AppLogEntry
    {
        TimestampUtc = x.TimestampUtc,
        Level = x.Level.ToString(),
        Source = x.Source,
        Message = x.Message,
        Details = x.Details,
        ExceptionType = x.ExceptionType,
        StackTrace = x.StackTrace
    })
    .ToList()
    .AsReadOnly();

        return $"Settings loaded | Attract: {settings.AttractModeTimeoutSeconds}s | Max recents: {settings.MaxRecentGames} | Logs: {logs.Count}";
    }
}
