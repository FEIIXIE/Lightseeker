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
        public string CurrentScore = "--";
        public string EstimatedReward = "--";
        public string SettlementCountdown = "--:--:--";
        public int EligibilityDividerAfterRow = -1;
        public string ClaimSummaryLocalizationKey = "leaderboard.formal.claim.pending";
        public bool CanClaim;
        public BountyRankRow[] Rows = Array.Empty<BountyRankRow>();

        public static BountyRankingSnapshot Empty()
        {
            return new BountyRankingSnapshot();
        }
    }

    [Serializable]
    public sealed class BountyTicketSnapshot
    {
        public string TicketId = "--";
        public string SourceCode = "--";
        public string AcquiredAtHongKong = "--";
    }

    [Serializable]
    public sealed class DailyBountySnapshot
    {
        public string Pool = "--";
        public string OwnedTicketCount = "--";
        public string EstimatedSingleTicketWinRate = "--";
        public string DrawCountdown = "--:--:--";
        public string ClaimSummaryLocalizationKey = "leaderboard.formal.claim.pending";
        public bool CanClaim;
        public BountyTicketSnapshot[] TicketRows = Array.Empty<BountyTicketSnapshot>();

        public static DailyBountySnapshot Empty()
        {
            return new DailyBountySnapshot();
        }
    }

    public static class DailyBountySourceCodes
    {
        public static string Normalize(string sourceCode)
        {
            switch ((sourceCode ?? string.Empty).Trim().ToUpperInvariant())
            {
                case "D0": return "D0";
                case "D1": return "D1";
                case "D2": return "D2";
                case "D3": return "D3";
                default: return "--";
            }
        }

        public static string LocalizationKey(string sourceCode)
        {
            switch (Normalize(sourceCode))
            {
                case "D0": return "leaderboard.formal.source.d0";
                case "D1": return "leaderboard.formal.source.d1";
                case "D2": return "leaderboard.formal.source.d2";
                case "D3": return "leaderboard.formal.source.d3";
                default: return "leaderboard.formal.source.unknown";
            }
        }
    }
}
