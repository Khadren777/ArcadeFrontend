using System;

/// <summary>
/// Prevents duplicate or rapid-fire launch attempts.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class LaunchGuardService
{
    private DateTime _lastLaunchTime = DateTime.MinValue;
    private bool _isLaunching;

    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(2);

    public bool CanLaunch()
    {
        if (_isLaunching)
            return false;

        if (DateTime.UtcNow - _lastLaunchTime < _cooldown)
            return false;

        return true;
    }

    public void MarkLaunching()
    {
        _isLaunching = true;
        _lastLaunchTime = DateTime.UtcNow;
    }

    public void MarkComplete()
    {
        _isLaunching = false;
    }
}
