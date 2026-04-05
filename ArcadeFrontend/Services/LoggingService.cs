using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ArcadeFrontend.Services
{
    public enum LogLevel { Debug, Info, Warning, Error, Critical }

    public sealed class LogEntry
    {
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public LogLevel Level { get; init; } = LogLevel.Info;
        public string Source { get; init; } = "General";
        public string Message { get; init; } = string.Empty;
        public string? Details { get; init; }
        public string? ExceptionType { get; init; }
        public string? StackTrace { get; init; }

        public string ToSingleLine()
        {
            var details = string.IsNullOrWhiteSpace(Details) ? string.Empty : $" | Details: {Details}";
            var exception = string.IsNullOrWhiteSpace(ExceptionType) ? string.Empty : $" | Exception: {ExceptionType}";
            return $"[{TimestampUtc:yyyy-MM-dd HH:mm:ss.fff} UTC] [{Level}] [{Source}] {Message}{details}{exception}";
        }
    }

    public interface ILoggingService
    {
        string LogDirectoryPath { get; }
        string CurrentLogFilePath { get; }
        string GetLogDirectoryPath();
        IReadOnlyList<LogEntry> Entries { get; }
        void Debug(string source, string message, string? details = null);
        void Info(string source, string message, string? details = null);
        void Warning(string source, string message, string? details = null);
        void Error(string source, string message, Exception? exception = null, string? details = null);
        void Critical(string source, string message, Exception? exception = null, string? details = null);
        IReadOnlyList<LogEntry> GetRecentEntries(int maxCount = 200);
    }

    public sealed class LoggingService : ILoggingService
    {
        private readonly object _syncLock = new();
        private readonly List<LogEntry> _recentEntries = new();
        public string LogDirectoryPath { get; }
        public string CurrentLogFilePath { get; }

        public LoggingService(string applicationDataPath, string applicationName = "ArcadeFrontend")
        {
            LogDirectoryPath = Path.Combine(applicationDataPath, applicationName, "Logs");
            Directory.CreateDirectory(LogDirectoryPath);
            CurrentLogFilePath = Path.Combine(LogDirectoryPath, $"arcade-{DateTime.UtcNow:yyyyMMdd}.log");
            Info(nameof(LoggingService), "Logging service initialized.", $"Log file: {CurrentLogFilePath}");
        }

        public void Debug(string source, string message, string? details = null) => Write(LogLevel.Debug, source, message, details, null);
        public void Info(string source, string message, string? details = null) => Write(LogLevel.Info, source, message, details, null);
        public void Warning(string source, string message, string? details = null) => Write(LogLevel.Warning, source, message, details, null);
        public void Error(string source, string message, Exception? exception = null, string? details = null) => Write(LogLevel.Error, source, message, details, exception);
        public void Critical(string source, string message, Exception? exception = null, string? details = null) => Write(LogLevel.Critical, source, message, details, exception);
        
        public string GetLogDirectoryPath() => LogDirectoryPath;

        public IReadOnlyList<LogEntry> Entries
        {
            get
            {
                lock (_syncLock)
                {
                    return _recentEntries.ToList().AsReadOnly();
                }
            }
        }
        public IReadOnlyList<LogEntry> GetRecentEntries(int maxCount = 200)
        {
            lock (_syncLock) return _recentEntries.TakeLast(Math.Max(1, maxCount)).ToList().AsReadOnly();
        }

        private void Write(LogLevel level, string source, string message, string? details, Exception? exception)
        {
            var entry = new LogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Source = string.IsNullOrWhiteSpace(source) ? "General" : source,
                Message = string.IsNullOrWhiteSpace(message) ? "(no message)" : message,
                Details = details,
                ExceptionType = exception?.GetType().FullName,
                StackTrace = exception?.ToString()
            };

            lock (_syncLock)
            {
                _recentEntries.Add(entry);
                try { File.AppendAllText(CurrentLogFilePath, entry.ToSingleLine() + Environment.NewLine); } catch { }
            }
        }
    }
}
