using System;
using System.Collections.Generic;
using ArcadeFrontend.Infrastructure;

namespace ArcadeFrontend.Services
{
    public interface INavigationStateService
    {
        string? CurrentScreen { get; }
        string? PreviousScreen { get; }
        string? SelectedGameId { get; }
        int SelectedIndex { get; }
        string? SelectedPlatform { get; }
        NavigationStateSnapshot GetSnapshot();
        void SetCurrentScreen(string screenName, string reason = "Navigation updated");
        void SetSelectedGame(string? gameId, int selectedIndex, string? platform = null, string reason = "Selection updated");
        void RestoreSnapshot(NavigationStateSnapshot snapshot, string reason = "State restored");
        void PushReturnPoint(string reason = "Return point saved");
        OperationResult<NavigationStateSnapshot> PopReturnPoint(string reason = "Return point restored");
    }

    public sealed class NavigationStateSnapshot
    {
        public string? CurrentScreen { get; init; }
        public string? PreviousScreen { get; init; }
        public string? SelectedGameId { get; init; }
        public int SelectedIndex { get; init; }
        public string? SelectedPlatform { get; init; }
        public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;
    }

    public sealed class NavigationStateService : INavigationStateService
    {
        private readonly ILoggingService _loggingService;
        private readonly Stack<NavigationStateSnapshot> _returnPoints = new();
        public string? CurrentScreen { get; private set; }
        public string? PreviousScreen { get; private set; }
        public string? SelectedGameId { get; private set; }
        public int SelectedIndex { get; private set; }
        public string? SelectedPlatform { get; private set; }

        public NavigationStateService(ILoggingService loggingService) { _loggingService = loggingService; }

        public NavigationStateSnapshot GetSnapshot() => new() { CurrentScreen = CurrentScreen, PreviousScreen = PreviousScreen, SelectedGameId = SelectedGameId, SelectedIndex = SelectedIndex, SelectedPlatform = SelectedPlatform };

        public void SetCurrentScreen(string screenName, string reason = "Navigation updated")
        {
            PreviousScreen = CurrentScreen;
            CurrentScreen = screenName;
        }

        public void SetSelectedGame(string? gameId, int selectedIndex, string? platform = null, string reason = "Selection updated")
        {
            SelectedGameId = gameId; SelectedIndex = selectedIndex; SelectedPlatform = platform;
        }

        public void RestoreSnapshot(NavigationStateSnapshot snapshot, string reason = "State restored")
        {
            CurrentScreen = snapshot.CurrentScreen;
            PreviousScreen = snapshot.PreviousScreen;
            SelectedGameId = snapshot.SelectedGameId;
            SelectedIndex = snapshot.SelectedIndex;
            SelectedPlatform = snapshot.SelectedPlatform;
        }

        public void PushReturnPoint(string reason = "Return point saved") => _returnPoints.Push(GetSnapshot());

        public OperationResult<NavigationStateSnapshot> PopReturnPoint(string reason = "Return point restored")
        {
            if (_returnPoints.Count == 0)
                return OperationResult<NavigationStateSnapshot>.Fail("There is no saved return point to restore.", FailureCategory.Validation, "The return point stack was empty.");
            var s = _returnPoints.Pop();
            RestoreSnapshot(s, reason);
            return OperationResult<NavigationStateSnapshot>.Success(s, "Return point restored successfully.");
        }
    }
}
