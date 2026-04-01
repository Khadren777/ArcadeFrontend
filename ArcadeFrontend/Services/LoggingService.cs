using ArcadeFrontend.Models;

/// <summary>
/// Stores lightweight in-memory application logs for diagnostics.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class LoggingService
{
    private readonly List<AppLogEntry> _entries = new();
    private readonly object _sync = new();

    public IReadOnlyList<AppLogEntry> Entries
    {
        get
        {
            lock (_sync)
            {
                return _entries.ToList();
            }
        }
    }

    public void Info(string category, string message)
    {
        Add("Info", category, message);
    }

    public void Warning(string category, string message)
    {
        Add("Warning", category, message);
    }

    public void Error(string category, string message)
    {
        Add("Error", category, message);
    }

    public void Clear()
    {
        lock (_sync)
        {
            _entries.Clear();
        }
    }

    private void Add(string level, string category, string message)
    {
        lock (_sync)
        {
            _entries.Add(new AppLogEntry
            {
                Level = level,
                Category = category,
                Message = message,
                TimestampUtc = DateTime.UtcNow
            });

            if (_entries.Count > 500)
            {
                _entries.RemoveRange(0, _entries.Count - 500);
            }
        }
    }
}
