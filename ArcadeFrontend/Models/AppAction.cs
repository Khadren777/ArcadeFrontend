using System;
using System.Collections.Generic;
using System.Text;

/// <summary>
/// Defines high-level user actions used throughout the application.
/// 
/// This decouples input (keyboard, encoder, etc.) from application behavior.
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
