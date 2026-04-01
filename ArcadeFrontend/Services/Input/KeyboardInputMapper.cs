using System.Windows.Input;
using ArcadeFrontend.Models;

/// <summary>
/// Maps raw keyboard input to high-level application actions.
/// </summary>
namespace ArcadeFrontend.Services.Input;

public sealed class KeyboardInputMapper
{
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
