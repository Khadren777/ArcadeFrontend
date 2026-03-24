using System.Text.Json.Serialization;

namespace ArcadeFrontend.Models
{
    public enum LaunchType
    {
        Emulator,
        Native
    }

    public class Game
    {
        public string Title { get; set; } = string.Empty;
        public string System { get; set; } = string.Empty;
        public bool IsHidden { get; set; }
        public bool IsFavorite { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public LaunchType LaunchType { get; set; }

        public string EmulatorKey { get; set; } = string.Empty;
        public string RomPath { get; set; } = string.Empty;

        public string LaunchTarget { get; set; } = string.Empty;
        public string LaunchArguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    }
}
