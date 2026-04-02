using System.IO;
using ArcadeFrontend.Models;

/// <summary>
/// Resolves configured sound paths and playback settings into a runtime sound state.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class SoundStateService
{
    private readonly PathService _pathService;

    public SoundStateService(PathService pathService)
    {
        _pathService = pathService;
    }

    public SoundStateSnapshot Build(AppSettings settings)
    {
        return new SoundStateSnapshot
        {
            MenuMoveSoundPath = ResolveOptionalPath(settings.MenuMoveSoundPath),
            MenuSelectSoundPath = ResolveOptionalPath(settings.MenuSelectSoundPath),
            MenuBackSoundPath = ResolveOptionalPath(settings.MenuBackSoundPath),
            LaunchSoundPath = ResolveOptionalPath(settings.LaunchSoundPath),
            AmbientMusicPath = ResolveOptionalPath(settings.AmbientMusicPath),
            EnableMenuSounds = settings.EnableMenuSounds,
            EnableAmbientMusic = settings.EnableAmbientMusic,
            MasterVolume = ClampVolume(settings.MasterVolume),
            MenuSoundVolume = ClampVolume(settings.MenuSoundVolume),
            AmbientMusicVolume = ClampVolume(settings.AmbientMusicVolume)
        };
    }

    private string ResolveOptionalPath(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return string.Empty;
        }

        string resolved = _pathService.Resolve(configuredPath);
        return File.Exists(resolved) ? resolved : string.Empty;
    }

    private static double ClampVolume(double value)
    {
        if (value < 0) return 0;
        if (value > 1) return 1;
        return value;
    }
}
