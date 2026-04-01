/// <summary>
/// Owns idle tracking and attract mode entry / exit state.
/// </summary>
namespace ArcadeFrontend.Services.State;

public sealed class IdleStateService
{
    private readonly AttractModeService _attractModeService;

    /// <summary>
    /// Initializes the idle state service.
    /// </summary>
    public IdleStateService(AttractModeService attractModeService)
    {
        _attractModeService = attractModeService;
    }

    /// <summary>
    /// Gets whether the application is currently in attract mode.
    /// </summary>
    public bool IsInAttractMode { get; private set; }

    /// <summary>
    /// Event raised when idle timeout enters attract mode.
    /// </summary>
    public event EventHandler? AttractModeRequested;

    /// <summary>
    /// Starts idle tracking.
    /// </summary>
    public void Start()
    {
        _attractModeService.IdleTimeoutReached += HandleIdleTimeoutReached;
        _attractModeService.Start();
    }

    /// <summary>
    /// Stops idle tracking.
    /// </summary>
    public void Stop()
    {
        _attractModeService.IdleTimeoutReached -= HandleIdleTimeoutReached;
    }

    /// <summary>
    /// Notifies the service that user interaction occurred.
    /// </summary>
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

    /// <summary>
    /// Marks the application as having entered attract mode.
    /// </summary>
    public void EnterAttractMode()
    {
        IsInAttractMode = true;
    }

    /// <summary>
    /// Exits attract mode if active.
    /// </summary>
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
