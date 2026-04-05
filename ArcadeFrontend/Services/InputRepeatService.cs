using System;
using System.Windows.Input;
using System.Windows.Threading;

/// <summary>
/// Provides hardware-style input repeat behavior for directional navigation.
/// </summary>
namespace ArcadeFrontend.Services;

public sealed class InputRepeatService
{
    private readonly DispatcherTimer _timer;
    private readonly Func<Key, bool> _repeatableKeyFilter;
    private Key? _heldKey;
    private Action<Key>? _repeatAction;
    private int _initialDelayMs = 150;
    private int _repeatIntervalMs = 75;
    private bool _hasFiredInitialRepeat;

    public InputRepeatService(Func<Key, bool> repeatableKeyFilter)
    {
        _repeatableKeyFilter = repeatableKeyFilter;
        _timer = new DispatcherTimer();
        _timer.Tick += Timer_Tick;
    }

    public void Configure(int initialDelayMs, int repeatIntervalMs)
    {
        _initialDelayMs = Math.Max(50, initialDelayMs);
        _repeatIntervalMs = Math.Max(25, repeatIntervalMs);
    }

    public void HandleKeyDown(Key key, Action<Key> repeatAction)
    {
        if (!_repeatableKeyFilter(key))
        {
            return;
        }

        if (_heldKey == key && _timer.IsEnabled)
        {
            return;
        }

        _heldKey = key;
        _repeatAction = repeatAction;
        _hasFiredInitialRepeat = false;
        _timer.Interval = TimeSpan.FromMilliseconds(_initialDelayMs);
        _timer.Start();
    }

    public void HandleKeyUp(Key key)
    {
        if (_heldKey == key)
        {
            Stop();
        }
    }

    public void Stop()
    {
        _timer.Stop();
        _heldKey = null;
        _repeatAction = null;
        _hasFiredInitialRepeat = false;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (_heldKey is null || _repeatAction is null)
        {
            Stop();
            return;
        }

        _repeatAction(_heldKey.Value);

        if (!_hasFiredInitialRepeat)
        {
            _hasFiredInitialRepeat = true;
            _timer.Interval = TimeSpan.FromMilliseconds(_repeatIntervalMs);
        }
    }
}
