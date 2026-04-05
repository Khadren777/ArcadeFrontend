using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ArcadeFrontend.Infrastructure;
using ArcadeFrontend.Models;

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
        private Process? _trackedProcess;

        public GameLauncherService(ILoggingService loggingService) { _loggingService = loggingService; }

        public LaunchResult LaunchGame(GameLaunchRequest request, IReadOnlyCollection<EmulatorProfile> emulatorProfiles)
        {
            if (request == null) return LaunchResult.Fail("The launch request was missing.", FailureCategory.Validation);
            if (string.IsNullOrWhiteSpace(request.GameTitle)) return LaunchResult.Fail("The selected game is missing a title.", FailureCategory.Validation);
            if (string.IsNullOrWhiteSpace(request.LaunchTarget)) return LaunchResult.Fail("The selected game is missing a launch target.", FailureCategory.Validation, gameTitle: request.GameTitle);

            try
            {
                string? exe = request.ExecutablePathOverride;
                string? workDir = request.WorkingDirectoryOverride;
                string? args = request.Arguments;

                if (string.IsNullOrWhiteSpace(exe))
                {
                    var profile = emulatorProfiles?.FirstOrDefault(p => string.Equals(p.Key, request.EmulatorProfileKey, StringComparison.OrdinalIgnoreCase));
                    if (profile == null) return LaunchResult.Fail("The emulator profile could not be found.", FailureCategory.Configuration, gameTitle: request.GameTitle, launchTarget: request.LaunchTarget);
                    exe = profile.ExecutablePath;
                    workDir ??= profile.ResolveWorkingDirectory();
                    args ??= profile.DefaultArgumentsTemplate;
                }

                if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
                    return LaunchResult.Fail($"The launcher executable for {request.GameTitle} could not be found.", FailureCategory.MissingFile, gameTitle: request.GameTitle, launchTarget: request.LaunchTarget, executablePath: exe);

                workDir ??= Path.GetDirectoryName(exe) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(workDir) || !Directory.Exists(workDir))
                    return LaunchResult.Fail($"The working directory for {request.GameTitle} could not be found.", FailureCategory.MissingDirectory, gameTitle: request.GameTitle, launchTarget: request.LaunchTarget, executablePath: exe, workingDirectory: workDir);

                var startInfo = new ProcessStartInfo { FileName = exe, Arguments = args ?? string.Empty, WorkingDirectory = workDir, UseShellExecute = true };
                var process = Process.Start(startInfo);
                if (process == null)
                    return LaunchResult.Fail($"{request.GameTitle} could not be launched.", FailureCategory.LaunchFailure, "Process.Start returned null.", gameTitle: request.GameTitle, launchTarget: request.LaunchTarget, executablePath: exe, arguments: args, workingDirectory: workDir);

                if (request.TrackProcess) _trackedProcess = process;
                return LaunchResult.Success(request.GameTitle, request.LaunchTarget, exe, args, workDir, process, $"{request.GameTitle} launched successfully.");
            }
            catch (Exception ex)
            {
                return LaunchResult.Fail($"{request.GameTitle} failed to launch.", FailureCategory.Unexpected, ex.Message, ex, request.GameTitle, request.LaunchTarget, request.ExecutablePathOverride, request.Arguments, request.WorkingDirectoryOverride);
            }
        }

        public OperationResult TerminateTrackedProcess()
        {
            if (_trackedProcess == null) return OperationResult.Fail("There is no tracked game process to terminate.", FailureCategory.ProcessFailure);
            try
            {
                if (_trackedProcess.HasExited) { _trackedProcess = null; return OperationResult.Success("Tracked game process had already exited."); }
                _trackedProcess.Kill(true);
                _trackedProcess.WaitForExit(5000);
                _trackedProcess = null;
                return OperationResult.Success("Tracked game process terminated successfully.");
            }
            catch (Exception ex)
            {
                return OperationResult.Fail("The tracked game process could not be terminated.", FailureCategory.ProcessFailure, ex.Message, ex);
            }
        }

        public Process? GetTrackedProcess() => _trackedProcess;
    }
}
