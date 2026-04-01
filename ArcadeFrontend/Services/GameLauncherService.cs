using ArcadeFrontend.Models;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace ArcadeFrontend.Services
{
    public class GameLauncherService
    {
        private readonly PathService _pathService;

        public GameLauncherService(PathService pathService)
        {
            _pathService = pathService;
        }
        public void LaunchGame(Game game, List<EmulatorProfile> emulatorProfiles)
        {
            switch (game.LaunchType)
            {
                case LaunchType.Emulator:
                    LaunchEmulatorGame(game, emulatorProfiles);
                    break;

                case LaunchType.Native:
                    LaunchNativeGame(game);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported launch type: {game.LaunchType}");
            }
        }

        private void LaunchEmulatorGame(Game game, List<EmulatorProfile> emulatorProfiles)
        {
            EmulatorProfile? emulator = emulatorProfiles.FirstOrDefault(e => e.Key == game.EmulatorKey);

            if (emulator == null)
            {
                throw new InvalidOperationException($"No emulator profile found for key '{game.EmulatorKey}'.");
            }

            if (string.IsNullOrWhiteSpace(emulator.ExecutablePath))
            {
                throw new InvalidOperationException($"ExecutablePath missing for emulator '{emulator.DisplayName}'.");
            }

            if (string.IsNullOrWhiteSpace(game.RomPath))
            {
                throw new InvalidOperationException($"RomPath missing for game '{game.Title}'.");
            }

            string resolvedExecutablePath = _pathService.Resolve(emulator.ExecutablePath);
            string resolvedRomPath = _pathService.Resolve(game.RomPath);
            string arguments = BuildArguments(emulator.ArgumentTemplate, game, resolvedRomPath);

            if (!File.Exists(resolvedExecutablePath))
            {
                throw new FileNotFoundException($"Emulator not found: {resolvedExecutablePath}");
            }

            if (!File.Exists(resolvedRomPath) && !Directory.Exists(resolvedRomPath))
            {
                throw new FileNotFoundException($"ROM not found: {resolvedRomPath}");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = resolvedExecutablePath,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            if (string.IsNullOrWhiteSpace(startInfo.WorkingDirectory))
            {
                string? directory = Path.GetDirectoryName(resolvedExecutablePath);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    startInfo.WorkingDirectory = directory;
                }
            }

            if (!string.IsNullOrWhiteSpace(emulator.WorkingDirectory))
            {
                string resolvedWorkingDirectory = _pathService.Resolve(emulator.WorkingDirectory);

                if (Directory.Exists(resolvedWorkingDirectory))
                {
                    startInfo.WorkingDirectory = resolvedWorkingDirectory;
                }
            }
            else
            {
                string? directory = Path.GetDirectoryName(resolvedExecutablePath);

                if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                {
                    startInfo.WorkingDirectory = directory;
                }
            }

            if (!File.Exists(resolvedExecutablePath))
            {
                MessageBox.Show($"Emulator not found: {resolvedExecutablePath}");
                return;
            }

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch '{game.Title}'\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void LaunchNativeGame(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.LaunchTarget))
            {
                throw new InvalidOperationException($"LaunchTarget missing for game '{game.Title}'.");
            }

            string resolvedLaunchTarget = _pathService.Resolve(game.LaunchTarget);

            ProcessStartInfo startInfo = new()
            {
                FileName = resolvedLaunchTarget,
                Arguments = game.LaunchArguments ?? string.Empty,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            if (!string.IsNullOrWhiteSpace(game.WorkingDirectory))
            {
                string resolvedWorkingDirectory = _pathService.Resolve(game.WorkingDirectory);

                if (Directory.Exists(resolvedWorkingDirectory))
                {
                    startInfo.WorkingDirectory = resolvedWorkingDirectory;
                }
            }
            else
            {
                bool hasDirectorySeparator =
                    resolvedLaunchTarget.Contains("\\") || resolvedLaunchTarget.Contains("/");

                if (hasDirectorySeparator && Path.IsPathRooted(resolvedLaunchTarget))
                {
                    string? directory = Path.GetDirectoryName(resolvedLaunchTarget);

                    if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
                    {
                        startInfo.WorkingDirectory = directory;
                    }
                }
            }

            if (!File.Exists(resolvedLaunchTarget))
            {
                MessageBox.Show($"Emulator not found: {resolvedLaunchTarget}");
                return;
            }

            try
            {
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to launch '{game.Title}'\n\n{ex.Message}",
                    "Launch Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private static string BuildArguments(string template, Game game, string resolvedRomPath)
        {
            string romName = Path.GetFileNameWithoutExtension(resolvedRomPath);

            return template
                .Replace("{rom}", resolvedRomPath ?? string.Empty)
                .Replace("{romname}", romName)
                .Replace("{title}", game.Title ?? string.Empty);
        }
    }
}