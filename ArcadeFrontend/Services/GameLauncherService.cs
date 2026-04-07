using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;
using Polly;
using Polly.CircuitBreaker;

namespace ArcadeFrontend.Services
{
    public interface IGameLauncherService
    {
        LaunchResult LaunchGame(GameLaunchRequest request, IReadOnlyCollection<EmulatorProfile> emulatorProfiles);
        OperationResult TerminateTrackedProcess();
        Process? GetTrackedProcess();
    }

    public sealed class GameLaunchRequest
    {
        public string GameTitle { get; init; } = string.Empty;
        public string LaunchTarget { get; init; } = string.Empty;
        public string? ExecutablePathOverride { get; init; }
        public string? Arguments { get; init; }
        public string? WorkingDirectoryOverride { get; init; }
        public string? EmulatorProfileKey { get; init; }
        public bool TrackProcess { get; init; } = true;
    }

    public sealed class GameLauncherService : IGameLauncherService
    {
        private readonly ILoggingService _loggingService;
        private readonly IAsyncPolicy<LaunchResult> _launchRetryPolicy;
        private readonly IAsyncPolicy<OperationResult> _terminateRetryPolicy;
        private Process? _trackedProcess;

        public GameLauncherService(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            _launchRetryPolicy = Policy<LaunchResult>
                .Handle<IOException>()
                .Or<UnauthorizedAccessException>()
                .OrResult(r => r.IsSuccess == false && r.FailureCategory == FailureCategory.LaunchFailure)
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(Math.Pow(2, attempt) * 100),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _loggingService.Warning(
                            nameof(GameLauncherService),
                            $"Launch attempt {retryCount} failed, retrying in {timespan.TotalMilliseconds}ms",
                            $"Previous error: {outcome.Result?.UserMessage ?? outcome.Exception?.Message}");
                    });

            _terminateRetryPolicy = Policy<OperationResult>
                .Handle<IOException>()
                .Or<UnauthorizedAccessException>()
                .WaitAndRetryAsync(
                    retryCount: 2,
                    sleepDurationProvider: attempt => TimeSpan.FromMilliseconds(200),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _loggingService.Warning(
                            nameof(GameLauncherService),
                            $"Termination attempt {retryCount} failed, retrying",
                            outcome.Exception?.Message);
                    });
        }

        public LaunchResult LaunchGame(GameLaunchRequest request, IReadOnlyCollection<EmulatorProfile> emulatorProfiles)
        {
            if (request == null)
                return LaunchResult.Fail("The launch request was missing.", FailureCategory.Validation);

            if (string.IsNullOrWhiteSpace(request.GameTitle))
                return LaunchResult.Fail("The selected game is missing a title.", FailureCategory.Validation);

            if (string.IsNullOrWhiteSpace(request.LaunchTarget))
                return LaunchResult.Fail(
                    "The selected game is missing a launch target.",
                    FailureCategory.Validation,
                    gameTitle: request.GameTitle);

            try
            {
                EmulatorProfile? profile = null;
                string? exe = request.ExecutablePathOverride;
                string? workDir = request.WorkingDirectoryOverride;

                if (string.IsNullOrWhiteSpace(exe))
                {
                    profile = emulatorProfiles?.FirstOrDefault(p =>
                        string.Equals(p.Key, request.EmulatorProfileKey, StringComparison.OrdinalIgnoreCase));

                    if (profile == null)
                    {
                        return LaunchResult.Fail(
                            "The emulator profile could not be found.",
                            FailureCategory.Configuration,
                            gameTitle: request.GameTitle,
                            launchTarget: request.LaunchTarget);
                    }

                    exe = profile.ExecutablePath;
                    workDir ??= profile.ResolveWorkingDirectory();
                }

                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                {
                    return LaunchResult.Fail(
                        $"The launcher executable for {request.GameTitle} could not be found.",
                        FailureCategory.MissingFile,
                        gameTitle: request.GameTitle,
                        launchTarget: request.LaunchTarget,
                        executablePath: exe);
                }

                workDir ??= Path.GetDirectoryName(exe) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(workDir) || !Directory.Exists(workDir))
                {
                    return LaunchResult.Fail(
                        $"The working directory for {request.GameTitle} could not be found.",
                        FailureCategory.MissingDirectory,
                        gameTitle: request.GameTitle,
                        launchTarget: request.LaunchTarget,
                        executablePath: exe,
                        workingDirectory: workDir);
                }

                var args = ResolveArguments(request, profile, exe, workDir);

                var result = _launchRetryPolicy.ExecuteAsync(async () =>
                {
                    return await LaunchProcessWithRetryAsync(request, exe, args, workDir);
                }).GetAwaiter().GetResult();

                return result;
            }
            catch (Exception ex)
            {
                return LaunchResult.Fail(
                    $"{request.GameTitle} failed to launch.",
                    FailureCategory.Unexpected,
                    ex.Message,
                    ex,
                    request.GameTitle,
                    request.LaunchTarget,
                    request.ExecutablePathOverride,
                    request.Arguments,
                    request.WorkingDirectoryOverride);
            }
        }

        private static string ResolveArguments(GameLaunchRequest request, EmulatorProfile? profile, string exe, string workDir)
        {
            var template =
                !string.IsNullOrWhiteSpace(request.Arguments) ? request.Arguments :
                !string.IsNullOrWhiteSpace(profile?.DefaultArgumentsTemplate) ? profile!.DefaultArgumentsTemplate :
                request.LaunchTarget;

            if (string.IsNullOrWhiteSpace(template))
            {
                return string.Empty;
            }

            return ApplyArgumentTokens(template, request, profile, exe, workDir);
        }

        private static string ApplyArgumentTokens(
            string template,
            GameLaunchRequest request,
            EmulatorProfile? profile,
            string exe,
            string workDir)
        {
            var launchTarget = request.LaunchTarget ?? string.Empty;
            var gameTitle = request.GameTitle ?? string.Empty;
            var profileKey = profile?.Key ?? request.EmulatorProfileKey ?? string.Empty;

            var replacements = new Dictionary<string, string>
            {
                ["{launchTarget}"] = launchTarget,
                ["{launchTargetQuoted}"] = QuoteIfNeeded(launchTarget),
                ["{romPath}"] = launchTarget,
                ["{romPathQuoted}"] = QuoteIfNeeded(launchTarget),
                ["{contentPath}"] = launchTarget,
                ["{contentPathQuoted}"] = QuoteIfNeeded(launchTarget),
                ["{gameTitle}"] = gameTitle,
                ["{gameTitleQuoted}"] = QuoteIfNeeded(gameTitle),
                ["{profileKey}"] = profileKey,
                ["{profileKeyQuoted}"] = QuoteIfNeeded(profileKey),
                ["{emulatorExecutablePath}"] = exe,
                ["{emulatorExecutablePathQuoted}"] = QuoteIfNeeded(exe),
                ["{workingDirectory}"] = workDir,
                ["{workingDirectoryQuoted}"] = QuoteIfNeeded(workDir)
            };

            var resolved = template;
            foreach (var replacement in replacements)
            {
                resolved = resolved.Replace(replacement.Key, replacement.Value);
            }

            return resolved;
        }

        private static string QuoteIfNeeded(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            if (value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal))
            {
                return value;
            }

            return value.Contains(' ') ? $"\"{value}\"" : value;
        }

        private async System.Threading.Tasks.Task<LaunchResult> LaunchProcessWithRetryAsync(
            GameLaunchRequest request,
            string exe,
            string? args,
            string workDir)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args ?? string.Empty,
                WorkingDirectory = workDir,
                UseShellExecute = true
            };

            var process = Process.Start(startInfo);
            if (process == null)
            {
                return LaunchResult.Fail(
                    $"{request.GameTitle} could not be launched.",
                    FailureCategory.LaunchFailure,
                    "Process.Start returned null.",
                    gameTitle: request.GameTitle,
                    launchTarget: request.LaunchTarget,
                    executablePath: exe,
                    arguments: args,
                    workingDirectory: workDir);
            }

            if (request.TrackProcess)
            {
                _trackedProcess = process;
            }

            return LaunchResult.Success(
                request.GameTitle,
                request.LaunchTarget,
                exe,
                args,
                workDir,
                process,
                $"{request.GameTitle} launched successfully.");
        }

        public OperationResult TerminateTrackedProcess()
        {
            if (_trackedProcess == null)
            {
                return OperationResult.Fail(
                    "There is no tracked game process to terminate.",
                    FailureCategory.ProcessFailure);
            }

            try
            {
                var result = _terminateRetryPolicy.ExecuteAsync(async () =>
                {
                    return await TerminateProcessWithRetryAsync();
                }).GetAwaiter().GetResult();

                return result;
            }
            catch (Exception ex)
            {
                return OperationResult.Fail(
                    "The tracked game process could not be terminated.",
                    FailureCategory.ProcessFailure,
                    ex.Message,
                    ex);
            }
        }

        private async System.Threading.Tasks.Task<OperationResult> TerminateProcessWithRetryAsync()
        {
            if (_trackedProcess?.HasExited == true)
            {
                _trackedProcess = null;
                return OperationResult.Success("Tracked game process had already exited.");
            }

            _trackedProcess?.Kill(true);
            _trackedProcess?.WaitForExit(5000);
            _trackedProcess = null;
            return OperationResult.Success("Tracked game process terminated successfully.");
        }

        public Process? GetTrackedProcess() => _trackedProcess;
    }
}