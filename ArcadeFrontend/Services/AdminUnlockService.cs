using System.Windows.Input;

namespace ArcadeFrontend.Services
{
    public class AdminUnlockService
    {
        private readonly List<Key> _unlockSequence;
        private readonly List<Key> _recentKeyHistory = new();

        public bool IsUnlocked { get; private set; }

        public AdminUnlockService(IEnumerable<Key> unlockSequence)
        {
            _unlockSequence = unlockSequence.ToList();
        }

        public bool TrackKey(Key key)
        {
            _recentKeyHistory.Add(key);

            if (_recentKeyHistory.Count > _unlockSequence.Count)
            {
                _recentKeyHistory.RemoveAt(0);
            }

            if (_recentKeyHistory.Count == _unlockSequence.Count &&
                _recentKeyHistory.SequenceEqual(_unlockSequence))
            {
                IsUnlocked = true;
                _recentKeyHistory.Clear();
                return true;
            }

            return false;
        }
    }
}