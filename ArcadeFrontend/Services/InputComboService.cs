using System;
using System.Collections.Generic;
using System.Linq;

namespace ArcadeFrontend.Services
{
    public sealed class InputComboDefinition
    {
        public string Key { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public IReadOnlyList<InputAction> Sequence { get; init; } = Array.Empty<InputAction>();
        public TimeSpan MaxGapBetweenInputs { get; init; } = TimeSpan.FromSeconds(2);
        public bool IsEnabled { get; init; } = true;
    }

    public sealed class InputComboMatchEventArgs : EventArgs
    {
        public string ComboKey { get; init; } = string.Empty;
        public string DisplayName { get; init; } = string.Empty;
        public IReadOnlyList<InputAction> Sequence { get; init; } = Array.Empty<InputAction>();
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }

    public interface IInputComboService
    {
        event EventHandler<InputComboMatchEventArgs>? ComboMatched;
        void RegisterCombo(InputComboDefinition combo);
        void RegisterCombos(IEnumerable<InputComboDefinition> combos);
        void ClearCombos();
        void ResetBuffer(string reason = "Manual reset");
        void ProcessInput(InputEvent inputEvent);
    }

    public sealed class InputComboService : IInputComboService
    {
        private readonly ILoggingService _loggingService;
        private readonly List<InputComboDefinition> _combos = new();
        private readonly List<(InputAction Action, DateTime TimestampUtc)> _buffer = new();
        public event EventHandler<InputComboMatchEventArgs>? ComboMatched;

        public InputComboService(ILoggingService loggingService) { _loggingService = loggingService; }

        public void RegisterCombo(InputComboDefinition combo)
        {
            _combos.RemoveAll(c => string.Equals(c.Key, combo.Key, StringComparison.OrdinalIgnoreCase));
            _combos.Add(combo);
        }

        public void RegisterCombos(IEnumerable<InputComboDefinition> combos)
        {
            foreach (var combo in combos) RegisterCombo(combo);
        }

        public void ClearCombos() => _combos.Clear();
        public void ResetBuffer(string reason = "Manual reset") => _buffer.Clear();

        public void ProcessInput(InputEvent inputEvent)
        {
            if (inputEvent.Action == InputAction.None) return;
            var maxGap = _combos.Count == 0 ? TimeSpan.FromSeconds(2) : TimeSpan.FromTicks(_combos.Max(c => c.MaxGapBetweenInputs.Ticks));
            _buffer.RemoveAll(x => inputEvent.TimestampUtc - x.TimestampUtc > maxGap);
            _buffer.Add((inputEvent.Action, inputEvent.TimestampUtc));

            foreach (var combo in _combos.Where(c => c.IsEnabled))
            {
                if (combo.Sequence.Count > _buffer.Count) continue;
                var tail = _buffer.Skip(_buffer.Count - combo.Sequence.Count).ToList();
                var ok = true;
                for (var i = 0; i < combo.Sequence.Count; i++)
                {
                    if (tail[i].Action != combo.Sequence[i]) { ok = false; break; }
                    if (i > 0 && tail[i].TimestampUtc - tail[i - 1].TimestampUtc > combo.MaxGapBetweenInputs) { ok = false; break; }
                }

                if (!ok) continue;

                ComboMatched?.Invoke(this, new InputComboMatchEventArgs
                {
                    ComboKey = combo.Key,
                    DisplayName = string.IsNullOrWhiteSpace(combo.DisplayName) ? combo.Key : combo.DisplayName,
                    Sequence = combo.Sequence,
                    TimestampUtc = DateTime.UtcNow
                });
                _buffer.Clear();
                _loggingService.Info(nameof(InputComboService), "Input combo matched.", combo.Key);
                break;
            }
        }
    }
}
