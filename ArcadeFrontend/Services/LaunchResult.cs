using System;
using System.Diagnostics;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public sealed class LaunchResult : OperationResult
    {
        public string? GameTitle { get; private init; }
        public string? LaunchTarget { get; private init; }
        public string? ExecutablePath { get; private init; }
        public string? Arguments { get; private init; }
        public string? WorkingDirectory { get; private init; }
        public int? ProcessId { get; private init; }
        public DateTime TimestampUtc { get; private init; } = DateTime.UtcNow;

        public static LaunchResult Success(string gameTitle, string launchTarget, string executablePath, string? arguments, string? workingDirectory, Process? process = null, string userMessage = "Game launched successfully.")
        {
            return new LaunchResult { IsSuccess = true, UserMessage = userMessage, GameTitle = gameTitle, LaunchTarget = launchTarget, ExecutablePath = executablePath, Arguments = arguments, WorkingDirectory = workingDirectory, ProcessId = process?.Id, TimestampUtc = DateTime.UtcNow };
        }

        public static new LaunchResult Fail(string userMessage, FailureCategory failureCategory, string? technicalMessage = null, Exception? exception = null, string? gameTitle = null, string? launchTarget = null, string? executablePath = null, string? arguments = null, string? workingDirectory = null)
        {
            return new LaunchResult { IsSuccess = false, FailureCategory = failureCategory, UserMessage = userMessage, TechnicalMessage = technicalMessage, Exception = exception, GameTitle = gameTitle, LaunchTarget = launchTarget, ExecutablePath = executablePath, Arguments = arguments, WorkingDirectory = workingDirectory, TimestampUtc = DateTime.UtcNow };
        }

        public string ToDiagnosticSummary() => IsSuccess ? $"Launch success | Game: {GameTitle} | Target: {LaunchTarget} | Exe: {ExecutablePath}" : $"Launch failure ({FailureCategory}) | Game: {GameTitle} | Target: {LaunchTarget} | Exe: {ExecutablePath} | Technical: {TechnicalMessage}";
    }
}
