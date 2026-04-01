using System.Windows.Input;

/// <summary>
/// Owns admin unlock state and unlock sequence tracking.
/// </summary>
namespace ArcadeFrontend.Services.State;

public sealed class AdminStateService
{
    private readonly AdminUnlockService _adminUnlockService;

    public AdminStateService(AdminUnlockService adminUnlockService)
    {
        _adminUnlockService = adminUnlockService;
    }

    public bool IsUnlocked => _adminUnlockService.IsUnlocked;

    public bool RegisterKey(Key key)
    {
        return _adminUnlockService.TrackKey(key);
    }
}
