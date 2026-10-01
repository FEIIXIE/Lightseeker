using System;

namespace SeekerAutoChess.Core
{
    public static class BattleStatusBarRules
    {
        public const int HealthPerSegment = 5;
        public const int MaximumSegments = 10;

        public static int SegmentCount(int maximumHealth)
        {
            var safeMaximumHealth = Math.Max(0, maximumHealth);
            return Math.Max(1, Math.Min(MaximumSegments,
                (safeMaximumHealth + HealthPerSegment - 1) / HealthPerSegment));
        }
    }
}
