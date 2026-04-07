using System;
using System.Collections.Generic;
using System.Windows.Input;
using SharpDX.DirectInput;
using WpfKey = System.Windows.Input.Key;

namespace ArcadeFrontend.Services
{
    public enum InputAction { None, Up, Down, Left, Right, Select, Back, Start, Pause, Exit, Admin, Coin }

    public sealed class InputEvent
    {
        public InputAction Action { get; init; }
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public string Source { get; init; } = "Unknown";
    }

    public interface IInputAbstractionService
    {
        void RegisterKeyBinding(WpfKey key, InputAction action);
        void HandleKeyDown(WpfKey key);
        void HandleKeyUp(WpfKey key);
        void RegisterExternalInput(InputAction action, string source = "External");
        event EventHandler<InputEvent>? InputReceived;
        IReadOnlyDictionary<WpfKey, InputAction> GetBindings();
    }

    public sealed class InputAbstractionService : IInputAbstractionService
    {
        private readonly ILoggingService _loggingService;
        private readonly Dictionary<WpfKey, InputAction> _keyBindings = new();
        private readonly Dictionary<GamepadButtonEnum, InputAction> _gamepadBindings = new();
        
        private DirectInput? _directInput;
        private Joystick? _joystick;
        private bool _gamepadConnected;

        public event EventHandler<InputEvent>? InputReceived;

        public InputAbstractionService(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            InitializeGamepadSupport();
        }

        private void InitializeGamepadSupport()
        {
            try
            {
                _directInput = new DirectInput();
                var devices = _directInput.GetDevices(DeviceType.Gamepad, DeviceEnumerationFlags.AllDevices);

                if (devices.Count > 0)
                {
                    var gamepadGuid = devices[0].InstanceGuid;
                    _joystick = new Joystick(_directInput, gamepadGuid);
                    _joystick.Acquire();
                    _gamepadConnected = true;
                    _loggingService.Info(nameof(InputAbstractionService), "Gamepad detected and initialized.");

                    // Set up default gamepad-to-action mappings
                    SetupDefaultGamepadBindings();
                }
                else
                {
                    _loggingService.Info(nameof(InputAbstractionService), "No gamepad detected. Keyboard input only.");
                }
            }
            catch (Exception ex)
            {
                _loggingService.Warning(nameof(InputAbstractionService), "Failed to initialize gamepad support.", ex.Message);
                _gamepadConnected = false;
            }
        }

        private void SetupDefaultGamepadBindings()
        {
            _gamepadBindings[GamepadButtonEnum.DPadUp] = InputAction.Up;
            _gamepadBindings[GamepadButtonEnum.DPadDown] = InputAction.Down;
            _gamepadBindings[GamepadButtonEnum.DPadLeft] = InputAction.Left;
            _gamepadBindings[GamepadButtonEnum.DPadRight] = InputAction.Right;
            _gamepadBindings[GamepadButtonEnum.A] = InputAction.Select;
            _gamepadBindings[GamepadButtonEnum.B] = InputAction.Back;
            _gamepadBindings[GamepadButtonEnum.X] = InputAction.Start;
            _gamepadBindings[GamepadButtonEnum.Y] = InputAction.Pause;
            _gamepadBindings[GamepadButtonEnum.Back] = InputAction.Back;
            _gamepadBindings[GamepadButtonEnum.Start] = InputAction.Start;
        }

        public void RegisterKeyBinding(WpfKey key, InputAction action)
        {
            _keyBindings[key] = action;
            _loggingService.Info(nameof(InputAbstractionService), "Key binding registered.", $"Key: {key} -> Action: {action}");
        }

        public void HandleKeyDown(WpfKey key)
        {
            if (_keyBindings.TryGetValue(key, out var action) && action != InputAction.None)
                RaiseInput(action, "Keyboard");
        }

        public void HandleKeyUp(WpfKey key) { }

        public void RegisterExternalInput(InputAction action, string source = "External")
        {
            if (action != InputAction.None)
                RaiseInput(action, source);
        }

        public IReadOnlyDictionary<WpfKey, InputAction> GetBindings() => _keyBindings;

        public void PollGamepadInput()
        {
            if (!_gamepadConnected || _joystick == null)
                return;

            try
            {
                _joystick.Poll();
                var state = _joystick.GetCurrentState();

                // Poll directional input (D-Pad and Analog sticks)
                if (state.PointOfViewControllers != null && state.PointOfViewControllers.Length > 0)
                {
                    int pov = state.PointOfViewControllers[0];
                    if (pov >= 0)
                    {
                        // D-Pad is reported as angle in degrees (0 = up, 9000 = right, 18000 = down, 27000 = left)
                        if (pov < 4500 || pov > 31500)
                            RaiseInput(InputAction.Up, "Gamepad");
                        else if (pov >= 4500 && pov < 13500)
                            RaiseInput(InputAction.Right, "Gamepad");
                        else if (pov >= 13500 && pov < 22500)
                            RaiseInput(InputAction.Down, "Gamepad");
                        else if (pov >= 22500 && pov <= 31500)
                            RaiseInput(InputAction.Left, "Gamepad");
                    }
                }

                // Poll button input
                var buttons = state.Buttons;
                if (buttons != null)
                {
                    for (int i = 0; i < Math.Min(buttons.Length, 16); i++)
                    {
                        if (buttons[i])
                        {
                            var buttonAction = MapGamepadButton(i);
                            if (buttonAction != InputAction.None)
                                RaiseInput(buttonAction, "Gamepad");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService.Warning(nameof(InputAbstractionService), "Error polling gamepad input.", ex.Message);
            }
        }

        private InputAction MapGamepadButton(int buttonIndex) => buttonIndex switch
        {
            0 => InputAction.Select,  // A button
            1 => InputAction.Back,    // B button
            2 => InputAction.Start,   // X button
            3 => InputAction.Pause,   // Y button
            4 => InputAction.Back,    // LB button
            5 => InputAction.Start,   // RB button
            6 => InputAction.Back,    // Back button
            7 => InputAction.Start,   // Start button
            _ => InputAction.None
        };

        private void RaiseInput(InputAction action, string source)
        {
            InputReceived?.Invoke(this, new InputEvent { Action = action, Source = source, TimestampUtc = DateTime.UtcNow });
        }

        public void Dispose()
        {
            _joystick?.Dispose();
            _directInput?.Dispose();
        }
    }

    /// <summary>
    /// Enum for gamepad buttons for explicit binding mapping
    /// </summary>
    public enum GamepadButtonEnum
    {
        A,
        B,
        X,
        Y,
        Back,
        Start,
        LeftThumb,
        RightThumb,
        LB,
        RB,
        DPadUp,
        DPadDown,
        DPadLeft,
        DPadRight
    }
}
