using System;

namespace ArcadeFrontend.Services
{
    public interface IAttractModeCoordinator
    {
        bool IsInitialized { get; }
        bool IsAttractModeActive { get; }
        string AttractScreenName { get; }
        void Initialize();
        void Shutdown();
        void NotifyUserActivity(string reason = "User activity detected");
        void ForceEnterAttractMode(string reason = "Manual attract mode request");
        void ForceExitAttractMode(string reason = "Manual attract mode exit");
    }

    public sealed class AttractModeCoordinator : IAttractModeCoordinator
    {
        private readonly IIdleService _idleService;
        private readonly INavigationStateService _navigationStateService;
        public bool IsInitialized { get; private set; }
        public bool IsAttractModeActive => _idleService.IsInAttractMode;
        public string AttractScreenName { get; }

        public AttractModeCoordinator(IIdleService idleService, INavigationStateService navigationStateService, ILoggingService loggingService, string attractScreenName = "AttractMode")
        {
            _idleService = idleService;
            _navigationStateService = navigationStateService;
            AttractScreenName = attractScreenName;
        }

        public void Initialize()
        {
            if (IsInitialized) return;
            _idleService.AttractModeEntered += (_, __) =>
            {
                _navigationStateService.PushReturnPoint("Entering attract mode");
                _navigationStateService.SetCurrentScreen(AttractScreenName, "Idle service entered attract mode");
            };
            _idleService.AttractModeExited += (_, __) => _navigationStateService.PopReturnPoint("Exiting attract mode");
            IsInitialized = true;
        }

        public void Shutdown() => IsInitialized = false;
        public void NotifyUserActivity(string reason = "User activity detected") => _idleService.ResetActivity(reason);
        public void ForceEnterAttractMode(string reason = "Manual attract mode request") => _idleService.EnterAttractMode(reason);
        public void ForceExitAttractMode(string reason = "Manual attract mode exit") { _idleService.ExitAttractMode(reason); _idleService.ResetActivity(reason); }
    }
}
