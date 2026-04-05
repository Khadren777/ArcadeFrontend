using System;

namespace ArcadeFrontend.Models
{
    public sealed class EmulatorProfile
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public string ExecutablePath { get; init; } = string.Empty;
        public string? WorkingDirectory { get; init; }
        public string? DefaultArgumentsTemplate { get; init; }
        public bool IsEnabled { get; init; } = true;

        public bool HasValidKey => !string.IsNullOrWhiteSpace(Key);
        public bool HasExecutablePath => !string.IsNullOrWhiteSpace(ExecutablePath);

        public string ResolveWorkingDirectory()
        {
            if (!string.IsNullOrWhiteSpace(WorkingDirectory))
            {
                return WorkingDirectory;
            }

            return HasExecutablePath
                ? System.IO.Path.GetDirectoryName(ExecutablePath) ?? string.Empty
                : string.Empty;
        }

        public override string ToString()
        {
            return string.IsNullOrWhiteSpace(DisplayName) ? Key : $"{DisplayName} ({Key})";
        }
    }
}
