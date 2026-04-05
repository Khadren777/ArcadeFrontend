/// <summary>
/// Represents one application log entry.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class AppLogEntry
{
    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    public string Category { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Level { get; init; } = "Info";
    public string Source { get; internal set; }
    public string? Details { get; internal set; }
    public string? ExceptionType { get; internal set; }
    public string? StackTrace { get; internal set; }
}
