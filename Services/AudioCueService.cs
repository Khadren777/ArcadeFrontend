using ArcadeFrontend.Models;

/// <summary>
/// Provides a small translation layer from app actions/events to sound effects.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class AudioCueService
{
    public SoundEffectType ForNavigationMove()
    {
        return SoundEffectType.MenuMove;
    }

    public SoundEffectType ForSelect()
    {
        return SoundEffectType.MenuSelect;
    }

    public SoundEffectType ForBack()
    {
        return SoundEffectType.MenuBack;
    }

    public SoundEffectType ForLaunch()
    {
        return SoundEffectType.Launch;
    }
}
