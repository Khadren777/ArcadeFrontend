using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Input;
using ArcadeFrontend.Models;

/// <summary>
/// Maps raw keyboard input to high-level application actions.
///
/// This keeps the rest of the application independent from specific keys
/// and makes future encoder / cabinet input support much easier to add.
/// </summary>
namespace ArcadeFrontend.Services.Input;

public sealed class KeyboardInputMapper
{
    /// <summary>
    /// Converts a keyboard key into an application action.
    /// </summary>
    public AppAction Map(Key key)
    {
        return key switch
        {
            Key.Up => AppAction.Up,
            Key.Down => AppAction.Down,
            Key.Left => AppAction.Left,
            Key.Right => AppAction.Right,
            Key.Enter => AppAction.Select,
            Key.Space => AppAction.Select,
            Key.Escape => AppAction.Back,
            Key.Back => AppAction.Back,
            Key.F => AppAction.ToggleFavorite,
            Key.A => AppAction.AdminUnlockPulse,
            Key.S => AppAction.ServiceMode,
            _ => AppAction.None
        };
    }
}
