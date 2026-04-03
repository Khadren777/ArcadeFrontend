using System;
using System.Linq;
using System.Text;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public interface IDiagnosticsSummaryBuilder
    {
        string BuildStartupSummary(AppStartupResult startupResult);
        string BuildOperationFailureSummary(string title, OperationResult result);
        string BuildLaunchSummary(LaunchResult result);
    }

    public sealed class DiagnosticsSummaryBuilder : IDiagnosticsSummaryBuilder
    {
        public string BuildStartupSummary(AppStartupResult startupResult)
        {
            if (startupResult == null) return "Startup summary unavailable. No startup result was provided.";
            var builder = new StringBuilder();
            builder.AppendLine(startupResult.CanContinue ? "Startup status: READY" : "Startup status: BLOCKED");
            foreach (var message in startupResult.StatusMessages) builder.AppendLine($"- {message}");
            if (startupResult.ValidationReport != null) builder.AppendLine(startupResult.ValidationReport.Summary);
            if (startupResult.GameData != null) builder.AppendLine($"Game entries loaded: {startupResult.GameData.Games.Count}");
            return builder.ToString().TrimEnd();
        }

        public string BuildOperationFailureSummary(string title, OperationResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine(string.IsNullOrWhiteSpace(title) ? "Operation summary" : title);
            builder.AppendLine(result.IsSuccess ? "Status: SUCCESS" : "Status: FAILURE");
            builder.AppendLine($"Category: {result.FailureCategory}");
            builder.AppendLine($"Message: {result.UserMessage}");
            if (!string.IsNullOrWhiteSpace(result.TechnicalMessage)) builder.AppendLine($"Technical detail: {result.TechnicalMessage}");
            if (result.Exception != null) builder.AppendLine(result.Exception.ToString());
            return builder.ToString().TrimEnd();
        }

        public string BuildLaunchSummary(LaunchResult result)
        {
            var builder = new StringBuilder();
            builder.AppendLine(result.IsSuccess ? "Launch status: SUCCESS" : "Launch status: FAILURE");
            builder.AppendLine($"Game: {result.GameTitle}");
            builder.AppendLine($"Target: {result.LaunchTarget}");
            builder.AppendLine($"Message: {result.UserMessage}");
            if (!string.IsNullOrWhiteSpace(result.ExecutablePath)) builder.AppendLine($"Executable: {result.ExecutablePath}");
            if (!string.IsNullOrWhiteSpace(result.TechnicalMessage)) builder.AppendLine($"Technical detail: {result.TechnicalMessage}");
            return builder.ToString().TrimEnd();
        }
    }
}
