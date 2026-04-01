/// <summary>
/// Owns idle tracking and attract mode entry / exit state.
/// </summary>
namespace ArcadeFrontend.Services.State;

public sealed class IdleStateService
{
    private readonly AttractModeService _attractModeService;

    public IdleStateService(AttractModeService attractModeService)
    {
        _attractModeService = attractModeService;
    }

    public bool IsInAttractMode { get; private set; }

    public event EventHandler? AttractModeRequested;

    public void Start()
    {
        _attractModeService.IdleTimeoutReached += HandleIdleTimeoutReached;
        _attractModeService.Start();
    }

    public void Stop()
    {
        _attractModeService.IdleTimeoutReached -= HandleIdleTimeoutReached;
    }

    public bool NotifyUserInteraction()
    {
        if (IsInAttractMode)
        {
            IsInAttractMode = false;
            _attractModeService.Reset();
            return true;
        }

        _attractModeService.Reset();
        return false;
    }

    public void EnterAttractMode()
    {
        IsInAttractMode = true;
    }

    public bool ExitAttractMode()
    {
        if (!IsInAttractMode)
        {
            return false;
        }

        IsInAttractMode = false;
        _attractModeService.Reset();
        return true;
    }

    private void HandleIdleTimeoutReached(object? sender, EventArgs e)
    {
        IsInAttractMode = true;
        AttractModeRequested?.Invoke(this, EventArgs.Empty);
    }
}
