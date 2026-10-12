using System;

namespace SeekerAutoChess.Core
{
    public enum GameplayTutorialOpenReason
    {
        None = 0,
        FirstVisit = 1,
        Manual = 2
    }

    public enum GameplayTutorialSwipeResult
    {
        None = 0,
        PreviousPage = 1,
        NextPage = 2
    }

    public enum GameplayTutorialCloseResult
    {
        None = 0,
        PersistSeen = 1
    }

    /// <summary>
    /// Pure state machine for the first-visit and replayable gameplay tutorial.
    /// Keeping gesture and persistence decisions outside Unity makes boundary
    /// behaviour deterministic and easy to verify before the formal art arrives.
    /// </summary>
    public sealed class GameplayTutorialFlow
    {
        public const int PageCount = 6;
        public const float DefaultSwipeThreshold = 120f;

        public bool IsOpen { get; private set; }
        public int CurrentPageIndex { get; private set; }
        public GameplayTutorialOpenReason OpenReason { get; private set; }

        public bool IsFirstPage => CurrentPageIndex == 0;
        public bool IsLastPage => CurrentPageIndex == PageCount - 1;

        public bool TryOpenFirstVisit(bool hasSeenTutorial)
        {
            if (hasSeenTutorial || IsOpen) return false;
            Open(GameplayTutorialOpenReason.FirstVisit);
            return true;
        }

        public void OpenManual()
        {
            Open(GameplayTutorialOpenReason.Manual);
        }

        public bool TryNextPage()
        {
            if (!IsOpen || IsLastPage) return false;
            CurrentPageIndex++;
            return true;
        }

        public bool TryPreviousPage()
        {
            if (!IsOpen || IsFirstPage) return false;
            CurrentPageIndex--;
            return true;
        }

        public GameplayTutorialSwipeResult HandleHorizontalSwipe(
            float horizontalDelta,
            float threshold = DefaultSwipeThreshold)
        {
            if (!IsOpen) return GameplayTutorialSwipeResult.None;
            if (threshold <= 0f) throw new ArgumentOutOfRangeException(nameof(threshold));

            if (horizontalDelta <= -threshold && TryNextPage())
                return GameplayTutorialSwipeResult.NextPage;
            if (horizontalDelta >= threshold && TryPreviousPage())
                return GameplayTutorialSwipeResult.PreviousPage;
            return GameplayTutorialSwipeResult.None;
        }

        public GameplayTutorialCloseResult Close()
        {
            if (!IsOpen) return GameplayTutorialCloseResult.None;
            var shouldPersistSeen = OpenReason == GameplayTutorialOpenReason.FirstVisit;
            IsOpen = false;
            CurrentPageIndex = 0;
            OpenReason = GameplayTutorialOpenReason.None;
            return shouldPersistSeen
                ? GameplayTutorialCloseResult.PersistSeen
                : GameplayTutorialCloseResult.None;
        }

        private void Open(GameplayTutorialOpenReason reason)
        {
            IsOpen = true;
            CurrentPageIndex = 0;
            OpenReason = reason;
        }
    }
}
