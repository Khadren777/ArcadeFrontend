using System.Windows.Threading;

namespace ArcadeFrontend.Services
{
    public class AttractModeService
    {
        private readonly DispatcherTimer _idleTimer;

        public event EventHandler? IdleTimeoutReached;

        public AttractModeService(TimeSpan timeout)
        {
            _idleTimer = new DispatcherTimer
            {
                Interval = timeout
            };

            _idleTimer.Tick += OnTimerTick;
        }

        public void Start()
        {
            _idleTimer.Start();
        }

        public void Reset()
        {
            _idleTimer.Stop();
            _idleTimer.Start();
        }

        public void Stop()
        {
            _idleTimer.Stop();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            _idleTimer.Stop();
            IdleTimeoutReached?.Invoke(this, EventArgs.Empty);
        }
    }
}