using System;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class BountyRankRow
    {
        public string Rank = "--";
        public string PlayerName = "--";
        public string Score = "--";
        public string Reward = "--";
        public bool IsCurrentPlayer;
    }

    [Serializable]
    public sealed class BountyRankingSnapshot
    {
        public string Pool = "--";
        public string NextUpdateHongKong = "--:--";
        public string Eligibility = "--";
        public string CurrentRank = "--";
        public string EstimatedReward = "--";
        public string ClaimSummaryLocalizationKey = "leaderboard.formal.claim.pending";
        public bool CanClaim;
        public BountyRankRow[] Rows = Array.Empty<BountyRankRow>();

        public static BountyRankingSnapshot Empty()
        {
            return new BountyRankingSnapshot();
        }
    }

    [Serializable]
    public sealed class DailyBountyTaskSnapshot
    {
        public string NameLocalizationKey = "leaderboard.formal.empty";
        public string Progress = "-- / --";
    }

    [Serializable]
    public sealed class DailyBountySnapshot
    {
        public string Pool = "--";
        public string Tickets = "-- / --";
        public string CumulativeReward = "-- SKR";
        public string RefreshCountdown = "--:--:--";
        public string DailyProgress = "-- / --";
        public string ClaimSummaryLocalizationKey = "leaderboard.formal.claim.pending";
        public bool CanClaim;
        public DailyBountyTaskSnapshot[] Tasks =
        {
            new DailyBountyTaskSnapshot(),
            new DailyBountyTaskSnapshot(),
            new DailyBountyTaskSnapshot()
        };

        public static DailyBountySnapshot Empty()
        {
            return new DailyBountySnapshot();
        }
    }
}
