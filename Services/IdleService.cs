using System;
using System.Timers;

namespace ArcadeFrontend.Services
{
    public interface IIdleService : IDisposable
    {
        bool IsRunning { get; }
        bool IsInAttractMode { get; }
        DateTime LastActivityUtc { get; }
        TimeSpan IdleDuration { get; }
        void Start();
        void Stop();
        void ResetActivity(string reason = "Activity detected");
        void EnterAttractMode(string reason = "Idle threshold reached");
        void ExitAttractMode(string reason = "Activity detected");
        void Configure(IdleServiceOptions options);
        event EventHandler<IdleStateChangedEventArgs>? AttractModeEntered;
        event EventHandler<IdleStateChangedEventArgs>? AttractModeExited;
    }

    public sealed class IdleServiceOptions
    {
        public TimeSpan AttractModeDelay { get; init; } = TimeSpan.FromMinutes(3);
        public TimeSpan HeartbeatInterval { get; init; } = TimeSpan.FromSeconds(1);
        public bool StartInAttractMode { get; init; } = false;
        public bool AutoEnterAttractMode { get; init; } = true;
    }

    public sealed class IdleStateChangedEventArgs : EventArgs
    {
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
        public string Reason { get; init; } = string.Empty;
        public TimeSpan IdleDuration { get; init; }
    }

    public sealed class IdleService : IIdleService
    {
        private readonly ILoggingService _loggingService;
        private Timer? _timer;
        private IdleServiceOptions _options = new();
        public bool IsRunning { get; private set; }
        public bool IsInAttractMode { get; private set; }
        public DateTime LastActivityUtc { get; private set; } = DateTime.UtcNow;
        public TimeSpan IdleDuration => DateTime.UtcNow - LastActivityUtc;
        public event EventHandler<IdleStateChangedEventArgs>? AttractModeEntered;
        public event EventHandler<IdleStateChangedEventArgs>? AttractModeExited;

        public IdleService(ILoggingService loggingService) { _loggingService = loggingService; }

        public void Configure(IdleServiceOptions options) { _options = options; if (_timer != null) _timer.Interval = options.HeartbeatInterval.TotalMilliseconds; }

        public void Start()
        {
            if (IsRunning) return;
            LastActivityUtc = DateTime.UtcNow;
            IsInAttractMode = _options.StartInAttractMode;
            _timer = new Timer(_options.HeartbeatInterval.TotalMilliseconds) { AutoReset = true, Enabled = true };
            _timer.Elapsed += (_, __) =>
            {
                if (_options.AutoEnterAttractMode && !IsInAttractMode && IdleDuration >= _options.AttractModeDelay) EnterAttractMode("Configured idle delay elapsed");
            };
            _timer.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            if (_timer != null) { _timer.Stop(); _timer.Dispose(); _timer = null; }
            IsRunning = false;
        }

        public void ResetActivity(string reason = "Activity detected")
        {
            LastActivityUtc = DateTime.UtcNow;
            if (IsInAttractMode) ExitAttractMode(reason);
        }

        public void EnterAttractMode(string reason = "Idle threshold reached")
        {
            if (IsInAttractMode) return;
            IsInAttractMode = true;
            AttractModeEntered?.Invoke(this, new IdleStateChangedEventArgs { Reason = reason, IdleDuration = IdleDuration });
        }

        public void ExitAttractMode(string reason = "Activity detected")
        {
            if (!IsInAttractMode) return;
            IsInAttractMode = false;
            AttractModeExited?.Invoke(this, new IdleStateChangedEventArgs { Reason = reason, IdleDuration = IdleDuration });
        }

        public void Dispose() => Stop();
    }
}
