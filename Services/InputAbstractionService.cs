using System;
using System.Collections.Generic;
using System.Windows.Input;

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
        void RegisterKeyBinding(Key key, InputAction action);
        void HandleKeyDown(Key key);
        void HandleKeyUp(Key key);
        void RegisterExternalInput(InputAction action, string source = "External");
        event EventHandler<InputEvent>? InputReceived;
        IReadOnlyDictionary<Key, InputAction> GetBindings();
    }

    public sealed class InputAbstractionService : IInputAbstractionService
    {
        private readonly ILoggingService _loggingService;
        private readonly Dictionary<Key, InputAction> _keyBindings = new();
        public event EventHandler<InputEvent>? InputReceived;

        public InputAbstractionService(ILoggingService loggingService) { _loggingService = loggingService; }

        public void RegisterKeyBinding(Key key, InputAction action)
        {
            _keyBindings[key] = action;
            _loggingService.Info(nameof(InputAbstractionService), "Key binding registered.", $"Key: {key} -> Action: {action}");
        }

        public void HandleKeyDown(Key key)
        {
            if (_keyBindings.TryGetValue(key, out var action) && action != InputAction.None) RaiseInput(action, "Keyboard");
        }

        public void HandleKeyUp(Key key) { }

        public void RegisterExternalInput(InputAction action, string source = "External")
        {
            if (action != InputAction.None) RaiseInput(action, source);
        }

        public IReadOnlyDictionary<Key, InputAction> GetBindings() => _keyBindings;

        private void RaiseInput(InputAction action, string source)
        {
            InputReceived?.Invoke(this, new InputEvent { Action = action, Source = source, TimestampUtc = DateTime.UtcNow });
        }
    }
}
