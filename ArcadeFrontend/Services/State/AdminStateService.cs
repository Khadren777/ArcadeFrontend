using System.Windows.Input;

/// <summary>
/// Owns admin unlock state and unlock sequence tracking.
/// </summary>
namespace ArcadeFrontend.Services.State;

public sealed class AdminStateService
{
    private readonly AdminUnlockService _adminUnlockService;

    /// <summary>
    /// Initializes the admin state service.
    /// </summary>
    public AdminStateService(AdminUnlockService adminUnlockService)
    {
        _adminUnlockService = adminUnlockService;
    }

    /// <summary>
    /// Gets whether admin features are currently unlocked.
    /// </summary>
    public bool IsUnlocked => _adminUnlockService.IsUnlocked;

    /// <summary>
    /// Registers a key against the admin unlock sequence.
    /// </summary>
    public bool RegisterKey(Key key)
    {
        return _adminUnlockService.TrackKey(key);
    }
}
