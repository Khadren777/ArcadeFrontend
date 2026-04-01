/// <summary>
/// Defines high-level user actions used throughout the application.
/// </summary>
namespace ArcadeFrontend.Models;

public enum AppAction
{
    None = 0,
    Up,
    Down,
    Left,
    Right,
    Select,
    Back,
    ToggleFavorite,
    AdminUnlockPulse,
    ServiceMode,
    ExitAttractMode,
    ResetIdleTimer
}
