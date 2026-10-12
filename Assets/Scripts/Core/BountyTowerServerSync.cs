using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    public sealed class BountyServerRankEntry
    {
        public int Rank { get; set; }
        public string PlayerName { get; set; }
        public long Score { get; set; }
        public long RewardMicroSkr { get; set; }
        public bool IsCurrentPlayer { get; set; }
    }

    public sealed class BountyServerCurrentPlayer
    {
        public int Rank { get; set; }
        public long Score { get; set; }
        public long EstimatedRewardMicroSkr { get; set; }
        public bool IsEligible { get; set; }
    }

    public sealed class BountyServerRankingPage
    {
        public string SnapshotId { get; set; }
        public string Mode { get; set; }
        public string GeneratedAt { get; set; }
        public string NextRefreshAt { get; set; }
        public bool IsFinal { get; set; }
        public bool IsStale { get; set; }
        public bool IsSynthetic { get; set; }
        public long PoolMicroSkr { get; set; }
        public string Eligibility { get; set; }
        public int TotalRankedPlayers { get; set; }
        public int Offset { get; set; }
        public int Limit { get; set; }
        public BountyServerCurrentPlayer CurrentPlayer { get; set; }
        public BountyServerRankEntry[] Rows { get; set; }
        public string RequestId { get; set; }
    }

    public sealed class BountyServerTicket
    {
        public string TicketId { get; set; }
        public string SourceCode { get; set; }
        public string AcquiredAt { get; set; }
    }

    public sealed class BountyServerDailyState
    {
        public string SnapshotId { get; set; }
        public string GeneratedAt { get; set; }
        public string NextDrawAt { get; set; }
        public bool IsSynthetic { get; set; }
        public long PoolMicroSkr { get; set; }
        public decimal EstimatedSingleTicketWinRate { get; set; }
        public bool CanClaim { get; set; }
        public BountyServerTicket[] Tickets { get; set; }
        public string RequestId { get; set; }
    }

    public interface IBountyTowerDataTransport
    {
        Task<BountyServerRankingPage> GetRankingPageAsync(
            string accessToken,
            string mode,
            int offset,
            int limit,
            CancellationToken cancellationToken);

        Task<BountyServerDailyState> GetDailyAsync(
            string accessToken,
            CancellationToken cancellationToken);
    }

    public sealed class BountyTowerSyncResult
    {
        public BountyRankingSnapshot Pve { get; set; }
        public BountyRankingSnapshot Pvp { get; set; }
        public DailyBountySnapshot Daily { get; set; }
        public bool IsSynthetic { get; set; }
    }

    public sealed class BountyTowerSyncCoordinator
    {
        private const int PageSize = 50;
        private const int MaximumRankedPlayers = 10000;
        private const long MicroSkrPerSkr = 1000000L;
        private static readonly TimeSpan HongKongOffset = TimeSpan.FromHours(8);
        private readonly IBountyTowerDataTransport transport;

        public BountyTowerSyncCoordinator(IBountyTowerDataTransport transport)
        {
            if (transport == null) throw new ArgumentNullException("transport");
            this.transport = transport;
        }

        public async Task<BountyTowerSyncResult> LoadAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("An authenticated access token is required.", "accessToken");

            var pvePage = await LoadAllRankingPagesAsync(
                accessToken, "pve", cancellationToken);
            var pvpPage = await LoadAllRankingPagesAsync(
                accessToken, "pvp", cancellationToken);
            var daily = await transport.GetDailyAsync(accessToken, cancellationToken);
            ValidateDaily(daily);

            return new BountyTowerSyncResult
            {
                Pve = MapRanking(pvePage),
                Pvp = MapRanking(pvpPage),
                Daily = MapDaily(daily),
                IsSynthetic = pvePage.IsSynthetic && pvpPage.IsSynthetic && daily.IsSynthetic
            };
        }

        private async Task<BountyServerRankingPage> LoadAllRankingPagesAsync(
            string accessToken,
            string mode,
            CancellationToken cancellationToken)
        {
            BountyServerRankingPage first = null;
            var rows = new List<BountyServerRankEntry>();
            var offset = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var page = await transport.GetRankingPageAsync(
                    accessToken, mode, offset, PageSize, cancellationToken);
                ValidatePage(page, mode, offset);
                if (first == null)
                {
                    first = page;
                    if (page.TotalRankedPlayers > MaximumRankedPlayers)
                        throw new InvalidOperationException(
                            "The bounty ranking exceeds the client safety limit.");
                }
                else if (!string.Equals(first.SnapshotId, page.SnapshotId,
                             StringComparison.Ordinal) ||
                         first.TotalRankedPlayers != page.TotalRankedPlayers)
                {
                    throw new InvalidOperationException(
                        "The bounty ranking snapshot changed during pagination.");
                }

                var pageRows = page.Rows ?? new BountyServerRankEntry[0];
                for (var index = 0; index < pageRows.Length; index++)
                {
                    var expectedRank = rows.Count + 1;
                    if (pageRows[index] == null || pageRows[index].Rank != expectedRank)
                        throw new InvalidOperationException(
                            "The bounty ranking contains a missing or out-of-order row.");
                    rows.Add(pageRows[index]);
                }

                if (rows.Count > page.TotalRankedPlayers)
                    throw new InvalidOperationException(
                        "The bounty ranking contains more rows than declared.");

                if (rows.Count >= page.TotalRankedPlayers) break;
                if (pageRows.Length == 0)
                    throw new InvalidOperationException(
                        "The bounty ranking ended before its declared player count.");
                offset += pageRows.Length;
            }

            first.Rows = rows.ToArray();
            first.Offset = 0;
            first.Limit = rows.Count;
            return first;
        }

        private static void ValidatePage(BountyServerRankingPage page, string mode, int offset)
        {
            if (page == null || string.IsNullOrWhiteSpace(page.SnapshotId) ||
                page.CurrentPlayer == null)
                throw new InvalidOperationException("The server returned an incomplete bounty ranking page.");
            if (!string.Equals(page.Mode, mode, StringComparison.OrdinalIgnoreCase) ||
                page.Offset != offset)
                throw new InvalidOperationException("The server returned the wrong bounty ranking page.");
            if (page.TotalRankedPlayers < 0)
                throw new InvalidOperationException("The server returned an invalid bounty player count.");
        }

        private static void ValidateDaily(BountyServerDailyState daily)
        {
            if (daily == null || string.IsNullOrWhiteSpace(daily.SnapshotId))
                throw new InvalidOperationException("The server returned an incomplete daily bounty state.");
            if (daily.EstimatedSingleTicketWinRate < 0m ||
                daily.EstimatedSingleTicketWinRate > 1m)
                throw new InvalidOperationException("The daily bounty probability is invalid.");
        }

        private static BountyRankingSnapshot MapRanking(BountyServerRankingPage page)
        {
            var sourceRows = page.Rows ?? new BountyServerRankEntry[0];
            var rows = new BountyRankRow[sourceRows.Length];
            for (var index = 0; index < sourceRows.Length; index++)
            {
                var source = sourceRows[index];
                rows[index] = new BountyRankRow
                {
                    Rank = source.Rank.ToString(CultureInfo.InvariantCulture),
                    PlayerName = source.PlayerName ?? "--",
                    Score = source.Score.ToString("N0", CultureInfo.InvariantCulture),
                    Reward = FormatSkr(source.RewardMicroSkr, true),
                    IsCurrentPlayer = source.IsCurrentPlayer
                };
            }

            var nextRefresh = ParseServerTime(page.NextRefreshAt, "nextRefreshAt");
            return new BountyRankingSnapshot
            {
                Pool = FormatSkr(page.PoolMicroSkr, false),
                NextUpdateHongKong = nextRefresh.ToOffset(HongKongOffset)
                    .ToString("HH:mm", CultureInfo.InvariantCulture),
                Eligibility = page.Eligibility ?? "--",
                CurrentRank = page.CurrentPlayer.Rank.ToString(CultureInfo.InvariantCulture),
                CurrentScore = page.CurrentPlayer.Score.ToString("N0", CultureInfo.InvariantCulture),
                EstimatedReward = FormatSkr(page.CurrentPlayer.EstimatedRewardMicroSkr, true),
                SettlementCountdown = "--:--:--",
                EligibilityDividerAfterRow = ResolveEligibilityDivider(
                    page.Eligibility, page.TotalRankedPlayers),
                ClaimSummaryLocalizationKey = "leaderboard.formal.claim.pending",
                CanClaim = false,
                Rows = rows
            };
        }

        private static DailyBountySnapshot MapDaily(BountyServerDailyState daily)
        {
            var sourceTickets = daily.Tickets ?? new BountyServerTicket[0];
            var tickets = new BountyTicketSnapshot[sourceTickets.Length];
            for (var index = 0; index < sourceTickets.Length; index++)
            {
                var source = sourceTickets[index] ?? new BountyServerTicket();
                var acquiredAt = ParseServerTime(source.AcquiredAt, "acquiredAt");
                tickets[index] = new BountyTicketSnapshot
                {
                    TicketId = string.IsNullOrWhiteSpace(source.TicketId) ? "--" : source.TicketId,
                    SourceCode = DailyBountySourceCodes.Normalize(source.SourceCode),
                    AcquiredAtHongKong = acquiredAt.ToOffset(HongKongOffset)
                        .ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)
                };
            }

            var nextDraw = ParseServerTime(daily.NextDrawAt, "nextDrawAt");
            return new DailyBountySnapshot
            {
                Pool = FormatSkr(daily.PoolMicroSkr, false),
                OwnedTicketCount = sourceTickets.Length.ToString(CultureInfo.InvariantCulture),
                EstimatedSingleTicketWinRate =
                    daily.EstimatedSingleTicketWinRate.ToString("P2", CultureInfo.InvariantCulture),
                DrawCountdown = FormatCountdown(nextDraw - DateTimeOffset.UtcNow),
                ClaimSummaryLocalizationKey = "leaderboard.formal.claim.dailyTime",
                CanClaim = false,
                TicketRows = tickets
            };
        }

        private static DateTimeOffset ParseServerTime(string value, string fieldName)
        {
            DateTimeOffset result;
            if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out result))
                throw new InvalidOperationException(
                    "The server bounty " + fieldName + " value is invalid.");
            return result;
        }

        private static string FormatSkr(long microSkr, bool includeUnit)
        {
            var whole = microSkr / MicroSkrPerSkr;
            var remainder = Math.Abs(microSkr % MicroSkrPerSkr);
            var value = remainder == 0
                ? whole.ToString("N0", CultureInfo.InvariantCulture)
                : (microSkr / (decimal)MicroSkrPerSkr).ToString("N2", CultureInfo.InvariantCulture);
            return includeUnit ? value + " SKR" : value;
        }

        private static int ResolveEligibilityDivider(string eligibility, int totalRankedPlayers)
        {
            if (string.Equals(eligibility, "top_60_percent", StringComparison.Ordinal))
                return (int)Math.Ceiling(Math.Max(0, totalRankedPlayers) * 0.6d);
            if (string.Equals(eligibility, "top_100", StringComparison.Ordinal))
                return Math.Min(100, Math.Max(0, totalRankedPlayers));
            return -1;
        }

        private static string FormatCountdown(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            var hours = (int)Math.Min(99d, Math.Floor(remaining.TotalHours));
            return hours.ToString("00", CultureInfo.InvariantCulture) + ":" +
                   remaining.Minutes.ToString("00", CultureInfo.InvariantCulture) + ":" +
                   remaining.Seconds.ToString("00", CultureInfo.InvariantCulture);
        }
    }

    public static class BountyRankingViewport
    {
        public static int ClampStart(int requestedStart, int totalRows, int visibleRows)
        {
            if (visibleRows < 1) throw new ArgumentOutOfRangeException("visibleRows");
            var maximum = Math.Max(0, totalRows - visibleRows);
            return Math.Max(0, Math.Min(requestedStart, maximum));
        }

        public static BountyRankRow RowAt(
            BountyRankingSnapshot snapshot,
            int firstVisibleRow,
            int visibleIndex,
            int visibleRows)
        {
            if (visibleIndex < 0 || visibleIndex >= visibleRows)
                throw new ArgumentOutOfRangeException("visibleIndex");
            var rows = snapshot == null || snapshot.Rows == null
                ? new BountyRankRow[0]
                : snapshot.Rows;
            var start = ClampStart(firstVisibleRow, rows.Length, visibleRows);
            var index = start + visibleIndex;
            return index < rows.Length && rows[index] != null ? rows[index] : new BountyRankRow();
        }
    }
}
