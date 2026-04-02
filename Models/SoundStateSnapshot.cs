/// <summary>
/// Represents the resolved sound state for the current frontend context.
/// </summary>
namespace ArcadeFrontend.Models;

public sealed class SoundStateSnapshot
{
    public string MenuMoveSoundPath { get; init; } = string.Empty;
    public string MenuSelectSoundPath { get; init; } = string.Empty;
    public string MenuBackSoundPath { get; init; } = string.Empty;
    public string LaunchSoundPath { get; init; } = string.Empty;
    public string AmbientMusicPath { get; init; } = string.Empty;

    public bool EnableMenuSounds { get; init; }
    public bool EnableAmbientMusic { get; init; }
    public double MasterVolume { get; init; } = 1.0;
    public double MenuSoundVolume { get; init; } = 1.0;
    public double AmbientMusicVolume { get; init; } = 1.0;
}
