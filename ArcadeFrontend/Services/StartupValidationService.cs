using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.Services
{
    public enum ValidationSeverity { Info, Warning, Error }

    public sealed class ValidationIssue
    {
        public string Code { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? TechnicalDetails { get; init; }
        public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
        public string? AffectedPath { get; init; }
        public override string ToString() => $"[{Severity}] {Code} - {Title}: {Message}";
    }

    public sealed class StartupValidationReport
    {
        public DateTime GeneratedUtc { get; init; } = DateTime.UtcNow;
        public IReadOnlyList<ValidationIssue> Issues { get; init; } = Array.Empty<ValidationIssue>();
        public bool HasErrors => Issues.Any(i => i.Severity == ValidationSeverity.Error);
        public bool IsValid => !HasErrors;
        public string Summary => $"Validation complete. Errors: {Issues.Count(i => i.Severity == ValidationSeverity.Error)}, Warnings: {Issues.Count(i => i.Severity == ValidationSeverity.Warning)}, Info: {Issues.Count(i => i.Severity == ValidationSeverity.Info)}.";
    }

    public sealed class StartupValidationOptions
    {
        public string? GamesConfigPath { get; init; }
        public IEnumerable<string>? RequiredDirectories { get; init; }
        public IEnumerable<string>? OptionalDirectories { get; init; }
        public IEnumerable<string>? RequiredFiles { get; init; }
        public IEnumerable<string>? OptionalFiles { get; init; }
        public IEnumerable<EmulatorProfile>? EmulatorProfiles { get; init; }
    }

    public interface IStartupValidationService { OperationResult<StartupValidationReport> Validate(StartupValidationOptions options); }

    public sealed class StartupValidationService : IStartupValidationService
    {
        private readonly ILoggingService _loggingService;
        public StartupValidationService(ILoggingService loggingService) { _loggingService = loggingService; }

        public OperationResult<StartupValidationReport> Validate(StartupValidationOptions options)
        {
            if (options == null) return OperationResult<StartupValidationReport>.Fail("Startup validation options were missing.", FailureCategory.Validation);
            var issues = new List<ValidationIssue>();
            if (!string.IsNullOrWhiteSpace(options.GamesConfigPath) && !File.Exists(options.GamesConfigPath))
                issues.Add(new ValidationIssue { Code = "CONFIG_GAMES_FILE_MISSING", Title = "Games config file missing", Message = "The configured game library file could not be found.", Severity = ValidationSeverity.Warning, AffectedPath = options.GamesConfigPath });
            if (options.RequiredDirectories != null)
                foreach (var d in options.RequiredDirectories.Where(x => !string.IsNullOrWhiteSpace(x) && !Directory.Exists(x)))
                    issues.Add(new ValidationIssue { Code = "DIR_REQUIRED_MISSING", Title = "Required directory missing", Message = "A required directory was not found.", Severity = ValidationSeverity.Error, AffectedPath = d });
            if (options.EmulatorProfiles != null)
                foreach (var p in options.EmulatorProfiles)
                    if (!string.IsNullOrWhiteSpace(p.ExecutablePath) && !File.Exists(p.ExecutablePath))
                        issues.Add(new ValidationIssue { Code = "EMU_PROFILE_EXE_NOT_FOUND", Title = "Emulator executable not found", Message = $"The emulator executable for profile '{p.Key}' could not be found.", Severity = ValidationSeverity.Error, AffectedPath = p.ExecutablePath });
            var report = new StartupValidationReport { Issues = issues, GeneratedUtc = DateTime.UtcNow };
            return OperationResult<StartupValidationReport>.Success(report, report.IsValid ? "Startup validation passed." : "Startup validation found issues that should be reviewed.");
        }
    }
}
