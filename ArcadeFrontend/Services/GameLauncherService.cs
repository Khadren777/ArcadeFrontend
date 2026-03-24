using System.Diagnostics;
using System.IO;
using ArcadeFrontend.Models;

namespace ArcadeFrontend.Services
{
    public class GameLauncherService
    {
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

            string arguments = BuildArguments(emulator.ArgumentTemplate, game);

            ProcessStartInfo startInfo = new()
            {
                FileName = emulator.ExecutablePath,
                Arguments = arguments,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(emulator.WorkingDirectory))
            {
                startInfo.WorkingDirectory = emulator.WorkingDirectory;
            }

            Process.Start(startInfo);
        }

        private void LaunchNativeGame(Game game)
        {
            if (string.IsNullOrWhiteSpace(game.LaunchTarget))
            {
                throw new InvalidOperationException($"LaunchTarget missing for game '{game.Title}'.");
            }

            ProcessStartInfo startInfo = new()
            {
                FileName = game.LaunchTarget,
                Arguments = game.LaunchArguments ?? string.Empty,
                UseShellExecute = true
            };

            if (!string.IsNullOrWhiteSpace(game.WorkingDirectory))
            {
                startInfo.WorkingDirectory = game.WorkingDirectory;
            }
            else if (Path.IsPathRooted(game.LaunchTarget))
            {
                string? directory = Path.GetDirectoryName(game.LaunchTarget);

                if (!string.IsNullOrWhiteSpace(directory))
                {
                    startInfo.WorkingDirectory = directory;
                }
            }

            Process.Start(startInfo);
        }

        private static string BuildArguments(string template, Game game)
        {
            return template
                .Replace("{rom}", game.RomPath ?? string.Empty)
                .Replace("{title}", game.Title ?? string.Empty);
        }
    }
}