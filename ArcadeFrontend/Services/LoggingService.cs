using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.File;

namespace ArcadeFrontend.Services
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }

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

        public string ToDetailedBlock()
        {
            var builder = new StringBuilder();
            builder.AppendLine(ToSingleLine());

            if (!string.IsNullOrWhiteSpace(StackTrace))
            {
                builder.AppendLine("StackTrace:");
                builder.AppendLine(StackTrace);
            }

            return builder.ToString();
        }
    }

    public interface ILoggingService
    {
        string LogDirectoryPath { get; }
        string CurrentLogFilePath { get; }
        string GetLatestLogFilePath();
        string GetLogDirectoryPath();
        void Debug(string source, string message, string? details = null);
        void Info(string source, string message, string? details = null);
        void Warning(string source, string message, string? details = null);
        void Error(string source, string message, Exception? exception = null, string? details = null);
        void Critical(string source, string message, Exception? exception = null, string? details = null);
        IReadOnlyList<LogEntry> GetRecentEntries(int maxCount = 200);
        string ReadCurrentLog();
    }

    public sealed class LoggingService : ILoggingService
    {
        private readonly object _syncLock = new();
        private readonly List<LogEntry> _recentEntries = new();
        private readonly int _maxInMemoryEntries;
        private readonly ILogger _serilogLogger;

        public string LogDirectoryPath { get; }
        public string CurrentLogFilePath { get; }

        public LoggingService(string applicationRootPath, int maxInMemoryEntries = 500)
        {
            if (string.IsNullOrWhiteSpace(applicationRootPath))
            {
                throw new ArgumentException("Application root path cannot be null or whitespace.", nameof(applicationRootPath));
            }

            _maxInMemoryEntries = Math.Max(100, maxInMemoryEntries);
            LogDirectoryPath = Path.Combine(applicationRootPath, "Logs");
            Directory.CreateDirectory(LogDirectoryPath);

            CurrentLogFilePath = Path.Combine(LogDirectoryPath, $"arcade-{DateTime.UtcNow:yyyyMMdd}.log");

            // Configure Serilog with file sink and structured logging
            _serilogLogger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    CurrentLogFilePath,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30)
                .Enrich.WithProperty("Application", "ArcadeFrontend")
                .CreateLogger();

            Info(nameof(LoggingService), "Logging service initialized.", $"Log file: {CurrentLogFilePath}");
        }

        public string GetLatestLogFilePath() => CurrentLogFilePath;
        public string GetLogDirectoryPath() => LogDirectoryPath;

        public void Debug(string source, string message, string? details = null)
            => Write(LogLevel.Debug, source, message, details, null);

        public void Info(string source, string message, string? details = null)
            => Write(LogLevel.Info, source, message, details, null);

        public void Warning(string source, string message, string? details = null)
            => Write(LogLevel.Warning, source, message, details, null);

        public void Error(string source, string message, Exception? exception = null, string? details = null)
            => Write(LogLevel.Error, source, message, details, exception);

        public void Critical(string source, string message, Exception? exception = null, string? details = null)
            => Write(LogLevel.Critical, source, message, details, exception);

        public IReadOnlyList<LogEntry> GetRecentEntries(int maxCount = 200)
        {
            lock (_syncLock)
            {
                return _recentEntries
                    .TakeLast(Math.Max(1, maxCount))
                    .ToList()
                    .AsReadOnly();
            }
        }

        public string ReadCurrentLog()
        {
            lock (_syncLock)
            {
                if (!File.Exists(CurrentLogFilePath))
                {
                    return string.Empty;
                }

                return File.ReadAllText(CurrentLogFilePath);
            }
        }

        private void Write(LogLevel level, string source, string message, string? details, Exception? exception)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                source = "General";
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "(no message)";
            }

            var entry = new LogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Level = level,
                Source = source,
                Message = message,
                Details = details,
                ExceptionType = exception?.GetType().FullName,
                StackTrace = exception?.ToString()
            };

            lock (_syncLock)
            {
                _recentEntries.Add(entry);

                if (_recentEntries.Count > _maxInMemoryEntries)
                {
                    _recentEntries.RemoveRange(0, _recentEntries.Count - _maxInMemoryEntries);
                }

                // Log to Serilog
                var serilogLevel = MapToSerilogLevel(level);
                _serilogLogger
                    .ForContext("Source", source)
                    .Write(
                        serilogLevel,
                        exception,
                        "{Message} {Details}",
                        message,
                        details ?? "");
            }
        }

        private static LogEventLevel MapToSerilogLevel(LogLevel level) => level switch
        {
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Info => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }
}
