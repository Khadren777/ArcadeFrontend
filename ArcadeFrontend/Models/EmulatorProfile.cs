namespace ArcadeFrontend.Models
{
    public class EmulatorProfile
    {
        public string Key { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string ArgumentTemplate { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
    }
}