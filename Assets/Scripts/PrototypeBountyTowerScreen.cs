using System;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
using SeekerAutoChess.Wallet;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private static readonly Color BountyCream = new Color32(244, 233, 197, 255);
        private static readonly Color BountyInk = new Color32(23, 57, 69, 255);
        private static readonly Color BountyMuted = new Color32(50, 66, 67, 255);
        private static readonly Color BountyGold = new Color32(159, 113, 44, 255);
        private static readonly Color BountyWhite = new Color32(255, 247, 221, 255);
        private static readonly Color BountyTeal = new Color32(18, 91, 97, 255);
        private static readonly Color BountyDisabled = new Color32(218, 205, 169, 255);

        private BountyRankingSnapshot bountyPveSnapshot = BountyRankingSnapshot.Empty();
        private BountyRankingSnapshot bountyPvpSnapshot = BountyRankingSnapshot.Empty();
        private DailyBountySnapshot bountyDailySnapshot = DailyBountySnapshot.Empty();
        private BountyTowerSyncCoordinator bountySyncCoordinator;
        private string bountyAccessToken;
        private CancellationTokenSource bountyRefreshCancellation;
        private Task bountyRefreshTask;
        private DateTimeOffset bountyLastRefreshAt;
        private string bountySyncStatusKey = "leaderboard.server.notConnected";
        private int bountyPveFirstVisibleRow;
        private int bountyPvpFirstVisibleRow;
        private bool bountyRankingPointerHeld;
        private float bountyRankingPointerLastY;
        private float bountyRankingPointerRemainder;
#if UNITY_EDITOR
        private bool bountySnapshotsSupplied;
        private bool bountyEditorFixturesApplied;
#endif

        public event Action<string> BountyTowerClaimRequested;

        public void ConfigureBountyTowerServer(string baseUrl,
            bool allowInsecureLocalDevelopment, WalletAuthenticationSession session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                throw new ArgumentException("An authenticated wallet session is required.", "session");
            CancelBountyTowerRefresh();
            bountySyncCoordinator = new BountyTowerSyncCoordinator(
                new UnityBountyTowerTransport(baseUrl, allowInsecureLocalDevelopment));
            bountyAccessToken = session.AccessToken;
            bountyLastRefreshAt = DateTimeOffset.MinValue;
#if UNITY_EDITOR
            bountySnapshotsSupplied = true;
#endif
            RequestBountyTowerRefreshIfNeeded(true);
        }

        public void UpdateBountyTowerSession(WalletAuthenticationSession session)
        {
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                throw new ArgumentException("An authenticated wallet session is required.", "session");
            CancelBountyTowerRefresh();
            bountyAccessToken = session.AccessToken;
            bountyLastRefreshAt = DateTimeOffset.MinValue;
            RequestBountyTowerRefreshIfNeeded(true);
        }

        public void ClearBountyTowerServerSession()
        {
            CancelBountyTowerRefresh();
            bountySyncCoordinator = null;
            bountyAccessToken = null;
            bountyLastRefreshAt = DateTimeOffset.MinValue;
            bountySyncStatusKey = "leaderboard.server.notConnected";
            ApplyBountyTowerSnapshots(BountyRankingSnapshot.Empty(),
                BountyRankingSnapshot.Empty(), DailyBountySnapshot.Empty());
        }

        public Task RefreshBountyTowerFromServerAsync(CancellationToken cancellationToken)
        {
            if (bountySyncCoordinator == null || string.IsNullOrWhiteSpace(bountyAccessToken))
                throw new InvalidOperationException("The bounty server session is not configured.");
            return RefreshBountyTowerFromServerCoreAsync(cancellationToken);
        }

        public void ApplyBountyTowerSnapshots(BountyRankingSnapshot pve,
            BountyRankingSnapshot pvp, DailyBountySnapshot daily)
        {
#if UNITY_EDITOR
            bountySnapshotsSupplied = true;
#endif
            bountyPveSnapshot = pve ?? BountyRankingSnapshot.Empty();
            bountyPvpSnapshot = pvp ?? BountyRankingSnapshot.Empty();
            bountyDailySnapshot = daily ?? DailyBountySnapshot.Empty();
            bountyPveFirstVisibleRow = BountyRankingViewport.ClampStart(
                bountyPveFirstVisibleRow, BountyRowCount(bountyPveSnapshot), 15);
            bountyPvpFirstVisibleRow = BountyRankingViewport.ClampStart(
                bountyPvpFirstVisibleRow, BountyRowCount(bountyPvpSnapshot), 16);
            leaderboardScroll = Vector2.zero;
        }

        private void DrawFormalBountyTowerScreen()
        {
            EnsureBountyTowerEditorTestData();
            var shell = leaderboardTab == 0
                ? BountyTowerAFormalArtContract.DailyShell
                : leaderboardTab == 1
                    ? BountyTowerAFormalArtContract.PveShell
                    : BountyTowerAFormalArtContract.PvpShell;
            DrawBountyTowerModule(shell, new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);

            DrawBountyTowerCommon();
            if (!string.IsNullOrWhiteSpace(bountySyncStatusKey))
                DrawBountyTowerText(new Rect(130f, 430f, 820f, 32f), T(bountySyncStatusKey), 17,
                    BountyMuted, TextAnchor.MiddleCenter, false);
            if (leaderboardTab == 0) DrawFormalDailyBounty();
            else DrawFormalRankingBounty(leaderboardTab == 1);
            DrawBountyTowerInteractions();
        }

        private void DrawBountyTowerCommon()
        {
            DrawBountyTowerText(new Rect(64f, 338f, 330f, 88f), T("leaderboard.daily"), 30,
                leaderboardTab == 0 ? BountyInk : BountyCream, TextAnchor.MiddleCenter, leaderboardTab == 0);
            DrawBountyTowerText(new Rect(394f, 338f, 300f, 88f), T("leaderboard.formal.pveTab"), 30,
                leaderboardTab == 1 ? BountyInk : BountyCream, TextAnchor.MiddleCenter, leaderboardTab == 1);
            DrawBountyTowerText(new Rect(694f, 338f, 322f, 88f), T("leaderboard.formal.pvpTab"), 30,
                leaderboardTab == 2 ? BountyInk : BountyCream, TextAnchor.MiddleCenter, leaderboardTab == 2);

            var pool = leaderboardTab == 0 ? bountyDailySnapshot.Pool :
                leaderboardTab == 1 ? bountyPveSnapshot.Pool : bountyPvpSnapshot.Pool;
            var poolLabelRect = leaderboardTab == 0
                ? new Rect(760f, 151f, 240f, 30f)
                : leaderboardTab == 1
                    ? new Rect(760f, 140f, 240f, 30f)
                    : new Rect(760f, 151f, 240f, 30f);
            var poolValueRect = leaderboardTab == 0
                ? new Rect(742f, 177f, 270f, 52f)
                : leaderboardTab == 1
                    ? new Rect(742f, 166f, 270f, 52f)
                    : new Rect(742f, 177f, 270f, 52f);
            DrawBountyTowerText(poolLabelRect, T("leaderboard.formal.pool"), 20,
                BountyCream, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(poolValueRect,
                string.Format(T("leaderboard.formal.poolValue"), SafeBountyValue(pool, "--")), 38,
                BountyWhite, TextAnchor.MiddleCenter, true);
            if (leaderboardTab != 0)
            {
                var update = leaderboardTab == 1
                    ? bountyPveSnapshot.NextUpdateHongKong
                    : bountyPvpSnapshot.NextUpdateHongKong;
                var nextUpdateRect = leaderboardTab == 1
                    ? new Rect(742f, 218f, 270f, 32f)
                    : new Rect(742f, 224f, 270f, 32f);
                DrawBountyTowerText(nextUpdateRect,
                    string.Format(T("leaderboard.formal.nextUpdate"), SafeBountyValue(update, "--:--")), 16,
                    BountyCream, TextAnchor.MiddleCenter, false);
            }
        }

        private void DrawFormalRankingBounty(bool pve)
        {
            var snapshot = pve ? bountyPveSnapshot : bountyPvpSnapshot;
            if (pve)
            {
                DrawFormalPveBounty(snapshot);
                return;
            }

            DrawFormalPvpBounty(snapshot);
        }

        private void DrawFormalPvpBounty(BountyRankingSnapshot snapshot)
        {
            DrawBountyTowerText(new Rect(150f, 470f, 780f, 68f),
                T("leaderboard.formal.pvpTitle"), 43,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(120f, 535f, 840f, 42f),
                T("leaderboard.formal.pvpSubtitle"), 23,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawRankingSummaryCell(80f, 220f, "leaderboard.formal.pveCurrentPlayer",
                FormatBountyCurrentRank(snapshot.CurrentRank), 30, BountyInk);
            DrawRankingSummaryCell(300f, 205f, "leaderboard.arenaRating",
                ResolveCurrentRankingScore(snapshot), 30, BountyInk);
            DrawRankingSummaryCell(505f, 225f, "leaderboard.formal.pveCurrentReward",
                SafeBountyValue(snapshot.EstimatedReward, "--"), 30, BountyTeal);
            DrawRankingSummaryCell(730f, 270f, "leaderboard.formal.pveCountdown",
                SafeBountyValue(snapshot.SettlementCountdown, "--:--:--"), 30, BountyInk);

            DrawBountyTowerText(new Rect(78f, 778f, 300f, 54f), T("leaderboard.pvpBoard"), 31,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(710f, 778f, 290f, 54f),
                string.Format(T("leaderboard.formal.pvpRewardRange"),
                    FormatBountyEligibility(snapshot.Eligibility)), 19,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawRankingHeaders(875f, 55f, "leaderboard.arenaRating");
            DrawFormalPvpRankingRows(snapshot);

            DrawRankingDividerOrRange(snapshot, false, 1464f, 1470f,
                BountyTowerAFormalArtContract.PvpEligibilityDivider, 11, 16);

            DrawBountyTowerText(new Rect(455f, 1843f, 430f, 56f), T("leaderboard.myReward"), 31,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(420f, 1901f, 560f, 42f),
                T("leaderboard.formal.claim.pvpOpen"), 21,
                BountyInk, TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(new Rect(420f, 1949f, 560f, 42f),
                T("leaderboard.formal.claim.window"), 21,
                BountyInk, TextAnchor.MiddleCenter, false);
            DrawBountyTowerClaim(new Rect(342f, 2029f, 398f, 92f), snapshot.CanClaim, "pvp");
        }

        private void DrawRankingSummaryCell(float x, float width, string labelKey, string value,
            int valueSize, Color valueColor)
        {
            DrawBountyTowerText(new Rect(x, 622f, width, 30f), T(labelKey), 20,
                BountyMuted, TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(new Rect(x, 659f, width, 58f), value, valueSize,
                valueColor, TextAnchor.MiddleCenter, true);
        }

        private void DrawRankingHeaders(float y, float height, string scoreHeaderKey)
        {
            DrawBountyTowerText(new Rect(80f, y, 170f, height), T("leaderboard.rank"), 22,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(250f, y, 290f, height), T("leaderboard.playerName"), 22,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(540f, y, 230f, height), T(scoreHeaderKey), 22,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(770f, y, 230f, height), T("leaderboard.skrReward"), 22,
                BountyInk, TextAnchor.MiddleCenter, true);
        }

        private void DrawFormalPveBounty(BountyRankingSnapshot snapshot)
        {
            DrawBountyTowerText(new Rect(150f, 470f, 780f, 68f),
                T("leaderboard.formal.pveTitle"), 43, BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(120f, 535f, 840f, 42f),
                T("leaderboard.formal.pveSubtitle"), 23, BountyMuted, TextAnchor.MiddleCenter, false);

            DrawPveMetric(new Rect(80f, 600f, 220f, 34f), new Rect(80f, 646f, 220f, 58f),
                "leaderboard.formal.pveCurrentPlayer", FormatBountyCurrentRank(snapshot.CurrentRank),
                30, BountyInk);
            DrawPveMetric(new Rect(300f, 600f, 205f, 34f), new Rect(300f, 646f, 205f, 58f),
                "leaderboard.formal.pveBalance", ResolveCurrentRankingScore(snapshot), 30, BountyInk);
            DrawPveMetric(new Rect(505f, 600f, 225f, 34f), new Rect(505f, 646f, 225f, 58f),
                "leaderboard.formal.pveCurrentReward", SafeBountyValue(snapshot.EstimatedReward, "--"), 30,
                BountyTeal);
            DrawPveMetric(new Rect(730f, 600f, 270f, 34f), new Rect(730f, 646f, 270f, 58f),
                "leaderboard.formal.pveCountdown", SafeBountyValue(snapshot.SettlementCountdown, "--:--:--"),
                30, BountyInk);

            DrawBountyTowerText(new Rect(78f, 778f, 300f, 54f), T("leaderboard.pveBoard"), 31,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(710f, 778f, 290f, 54f),
                string.Format(T("leaderboard.formal.pveRewardRange"),
                    FormatBountyEligibility(snapshot.Eligibility)), 19,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawRankingHeaders(875f, 55f, "leaderboard.tideBalance");

            DrawFormalPveRankingRows(snapshot);

            DrawBountyTowerText(new Rect(455f, 1902f, 430f, 56f), T("leaderboard.myReward"), 31,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(420f, 1960f, 560f, 42f),
                T("leaderboard.formal.claim.pveOpen"), 21,
                BountyInk, TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(new Rect(420f, 2008f, 560f, 42f),
                T("leaderboard.formal.claim.window"), 21,
                BountyMuted, TextAnchor.MiddleCenter, false);
            DrawBountyTowerClaim(new Rect(342f, 2088f, 398f, 92f), snapshot.CanClaim, "pve");
        }

        private void DrawPveMetric(Rect labelRect, Rect valueRect, string labelKey, string value,
            int valueSize, Color valueColor)
        {
            DrawBountyTowerText(labelRect, T(labelKey), 19, BountyMuted,
                TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(valueRect, value, valueSize, valueColor,
                TextAnchor.MiddleCenter, true);
        }

        private void DrawFormalPveRankingRows(BountyRankingSnapshot snapshot)
        {
            if (snapshot.Rows == null || snapshot.Rows.Length == 0)
            {
                DrawBountyTowerText(new Rect(100f, 950f, 880f, 120f),
                    T("leaderboard.formal.noRankingData"), 25, BountyMuted,
                    TextAnchor.MiddleCenter, false);
                return;
            }

            var boundaries = new[]
            {
                930f, 993f, 1056f, 1119f, 1176f, 1231f, 1287f, 1342f, 1397f, 1452f, 1508f,
                1567f, 1622f, 1674f, 1726f, 1779f, 1835f
            };
            for (var i = 0; i < 15; i++)
            {
                var intervalIndex = i < 10 ? i : i + 1;
                var rowY = boundaries[intervalIndex];
                var rowHeight = boundaries[intervalIndex + 1] - rowY;
                var row = BountyRankingViewport.RowAt(
                    snapshot, bountyPveFirstVisibleRow, i, 15);
                if (row.IsCurrentPlayer)
                {
                    DrawBountyTowerModule(BountyTowerAFormalArtContract.PveSelectedRankRow,
                        new Rect(80f, rowY, 920f, 64f), 920, 64);
                }

                var primaryColor = row.IsCurrentPlayer ? BountyWhite : BountyInk;
                var secondaryColor = row.IsCurrentPlayer ? BountyWhite : BountyMuted;
                DrawBountyTowerText(new Rect(80f, rowY, 170f, rowHeight),
                    SafeBountyValue(row.Rank, "--"), 22, primaryColor, TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(250f, rowY, 290f, rowHeight),
                    SafeBountyValue(row.PlayerName, "--"), 23, primaryColor,
                    TextAnchor.MiddleCenter, row.IsCurrentPlayer);
                DrawBountyTowerText(new Rect(540f, rowY, 230f, rowHeight),
                    SafeBountyValue(row.Score, "--"), 22, secondaryColor,
                    TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(770f, rowY, 230f, rowHeight),
                    SafeBountyValue(row.Reward, "--"), 22,
                    row.IsCurrentPlayer ? BountyWhite : BountyGold,
                    TextAnchor.MiddleCenter, row.IsCurrentPlayer);
            }

            DrawRankingDividerOrRange(snapshot, true, 1502f, 1508f,
                BountyTowerAFormalArtContract.PveEligibilityDivider, 10, 15);
        }

        private static string ResolveCurrentRankingScore(BountyRankingSnapshot snapshot)
        {
            if (snapshot == null) return "--";
            if (!string.IsNullOrWhiteSpace(snapshot.CurrentScore) && snapshot.CurrentScore != "--")
                return snapshot.CurrentScore;
            var rows = snapshot.Rows ?? Array.Empty<BountyRankRow>();
            for (var i = 0; i < rows.Length; i++)
                if (rows[i] != null && rows[i].IsCurrentPlayer)
                    return SafeBountyValue(rows[i].Score, "--");
            return "--";
        }

        private void DrawFormalPvpRankingRows(BountyRankingSnapshot snapshot)
        {
            var rowTops = new[]
            {
                930f, 978f, 1026f, 1073f, 1120f, 1167f, 1214f, 1261f,
                1308f, 1356f, 1403f, 1548f, 1597f, 1644f, 1692f, 1739f
            };
            var rowBottoms = new[]
            {
                978f, 1026f, 1073f, 1120f, 1167f, 1214f, 1261f, 1308f,
                1356f, 1403f, 1451f, 1597f, 1644f, 1692f, 1739f, 1787f
            };
            for (var i = 0; i < 16; i++)
            {
                var row = BountyRankingViewport.RowAt(
                    snapshot, bountyPvpFirstVisibleRow, i, 16);
                var rowY = rowTops[i];
                var rowHeight = rowBottoms[i] - rowY;
                if (row.IsCurrentPlayer)
                {
                    DrawBountyTowerModule(BountyTowerAFormalArtContract.PvpSelectedRankRow,
                        new Rect(80f, rowY, 920f, 48f), 920, 48);
                }
                var rankColor = row.IsCurrentPlayer ? BountyWhite : BountyInk;
                var nameColor = row.IsCurrentPlayer ? BountyWhite : BountyInk;
                DrawBountyTowerText(new Rect(80f, rowY, 170f, rowHeight), SafeBountyValue(row.Rank, "--"), 22,
                    rankColor, TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(250f, rowY, 290f, rowHeight),
                    SafeBountyValue(row.PlayerName, "--"), 23,
                    nameColor, TextAnchor.MiddleCenter, row.IsCurrentPlayer);
                DrawBountyTowerText(new Rect(540f, rowY, 230f, rowHeight), SafeBountyValue(row.Score, "--"), 22,
                    row.IsCurrentPlayer ? BountyWhite : BountyMuted, TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(770f, rowY, 230f, rowHeight), SafeBountyValue(row.Reward, "--"), 22,
                    row.IsCurrentPlayer ? BountyWhite : BountyGold, TextAnchor.MiddleCenter, false);
            }
        }

        private void DrawFormalDailyBounty()
        {
            var daily = bountyDailySnapshot;
            DrawBountyTowerText(new Rect(150f, 500f, 780f, 68f), T("leaderboard.daily"), 43,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(110f, 566f, 860f, 42f), T("leaderboard.formal.dailySubtitle"), 22,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawDailyMetric(new Rect(210f, 649f, 170f, 34f), new Rect(216f, 705f, 158f, 58f),
                "leaderboard.formal.dailyOwnedTickets", FormatBountyTicketCount(daily.OwnedTicketCount),
                33, BountyInk);
            DrawDailyMetric(new Rect(500f, 649f, 190f, 34f), new Rect(510f, 689f, 170f, 52f),
                "leaderboard.formal.dailyWinRate", daily.EstimatedSingleTicketWinRate, 32, BountyInk);
            DrawBountyTowerText(new Rect(478f, 749f, 234f, 24f),
                T("leaderboard.formal.dailyEstimateNote1"), 15, BountyMuted, TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(new Rect(478f, 776f, 234f, 24f),
                T("leaderboard.formal.dailyEstimateNote2"), 15, BountyMuted, TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(new Rect(478f, 803f, 234f, 24f),
                T("leaderboard.formal.dailyEstimateNote3"), 15, BountyMuted, TextAnchor.MiddleCenter, false);
            DrawDailyMetric(new Rect(800f, 649f, 180f, 34f), new Rect(798f, 700f, 184f, 52f),
                "leaderboard.formal.dailyDrawCountdown", daily.DrawCountdown, 31, BountyInk);
            DrawBountyTowerText(new Rect(798f, 757f, 184f, 30f),
                T("leaderboard.formal.hongKongTime"), 17, BountyInk, TextAnchor.MiddleCenter, false);

            DrawBountyTowerText(new Rect(74f, 865f, 168f, 52f),
                T("leaderboard.formal.flowTitle"), 31, BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(230f, 910f, 720f, 34f),
                T("leaderboard.formal.flowDescription"), 20, BountyMuted, TextAnchor.MiddleCenter, false);
            DrawDailyFlow();
            DrawDailyTicketTable(daily);

            DrawBountyTowerText(new Rect(390f, 1827f, 470f, 56f),
                T("leaderboard.myReward"), 31, BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(370f, 1897f, 610f, 42f),
                T("leaderboard.formal.claim.dailyOpen"), 22, BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(370f, 1941f, 610f, 40f),
                T("leaderboard.formal.claim.dailyWindow"), 19, BountyMuted, TextAnchor.MiddleCenter, false);
            DrawBountyTowerClaim(new Rect(342f, 1991f, 398f, 92f), daily.CanClaim, "daily");
        }

        private void DrawDailyFlow()
        {
            var numberRects = new[]
            {
                new Rect(136f, 1070f, 79f, 30f), new Rect(380f, 1070f, 79f, 30f),
                new Rect(624f, 1070f, 80f, 30f), new Rect(869f, 1070f, 79f, 30f)
            };
            var firstLineRects = new[]
            {
                new Rect(87f, 1107f, 177f, 36f), new Rect(330f, 1107f, 179f, 36f),
                new Rect(574f, 1107f, 180f, 30f), new Rect(819f, 1107f, 179f, 30f)
            };
            var firstLineKeys = new[]
            {
                "leaderboard.formal.flow.step1", "leaderboard.formal.flow.step2",
                "leaderboard.formal.flow.step3.line1", "leaderboard.formal.flow.step4.line1"
            };
            for (var i = 0; i < 4; i++)
            {
                DrawBountyTowerText(numberRects[i], (i + 1).ToString("00"), 19,
                    BountyGold, TextAnchor.MiddleCenter, true);
                DrawBountyTowerText(firstLineRects[i], T(firstLineKeys[i]), 20,
                    BountyInk, TextAnchor.MiddleCenter, true);
            }
            DrawBountyTowerText(new Rect(574f, 1138f, 180f, 30f),
                T("leaderboard.formal.flow.step3.line2"), 20, BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(819f, 1138f, 179f, 30f),
                T("leaderboard.formal.flow.step4.line2"), 20, BountyInk, TextAnchor.MiddleCenter, true);
        }

        private void DrawDailyTicketTable(DailyBountySnapshot daily)
        {
            DrawBountyTowerText(new Rect(74f, 1208f, 216f, 58f),
                T("leaderboard.formal.ticketTableTitle"), 31, BountyInk, TextAnchor.MiddleCenter, true);

            var headerRects = new[]
            {
                new Rect(80f, 1278f, 120f, 55f), new Rect(200f, 1278f, 340f, 55f),
                new Rect(540f, 1278f, 170f, 55f), new Rect(710f, 1278f, 290f, 55f)
            };
            var headerKeys = new[]
            {
                "leaderboard.formal.ticket.index", "leaderboard.formal.ticket.id",
                "leaderboard.formal.ticket.source", "leaderboard.formal.ticket.time"
            };
            for (var i = 0; i < headerRects.Length; i++)
                DrawBountyTowerText(headerRects[i], T(headerKeys[i]), 20,
                    BountyInk, TextAnchor.MiddleCenter, true);

            var boundaries = new[] { 1333f, 1394f, 1454f, 1514f, 1574f, 1634f, 1695f, 1756f };
            var tickets = daily.TicketRows ?? Array.Empty<BountyTicketSnapshot>();
            for (var i = 0; i < 7; i++)
            {
                var row = i < tickets.Length && tickets[i] != null ? tickets[i] : new BountyTicketSnapshot();
                var top = boundaries[i];
                var height = boundaries[i + 1] - top;
                DrawBountyTowerText(new Rect(80f, top, 120f, height), (i + 1).ToString(), 20,
                    BountyGold, TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(200f, top, 340f, height),
                    SafeBountyValue(row.TicketId, "--"), 19, BountyInk, TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(540f, top, 170f, height),
                    DailyBountySourceCodes.Normalize(row.SourceCode), 21,
                    BountyTeal, TextAnchor.MiddleCenter, true);
                DrawBountyTowerText(new Rect(710f, top, 290f, height),
                    SafeBountyValue(row.AcquiredAtHongKong, "--"), 18,
                    BountyMuted, TextAnchor.MiddleCenter, false);
            }
        }

        private void DrawDailyMetric(Rect labelRect, Rect valueRect, string labelKey, string value,
            int valueSize, Color valueColor)
        {
            DrawBountyTowerText(labelRect, T(labelKey), 21, BountyMuted, TextAnchor.MiddleCenter, false);
            DrawBountyTowerText(valueRect, SafeBountyValue(value, "--"), valueSize, valueColor,
                TextAnchor.MiddleCenter, true);
        }

        private void DrawBountyTowerInteractions()
        {
            HandleBountyRankingPointer();
            if (GUI.Button(new Rect(34f, 48f, 118f, 112f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "bounty-tower-back-harbor");
                currentView = ScreenView.Harbor;
                leaderboardScroll = Vector2.zero;
                return;
            }

            var tabRects = new[] { new Rect(64f, 338f, 330f, 88f), new Rect(394f, 338f, 300f, 88f),
                new Rect(694f, 338f, 322f, 88f) };
            for (var i = 0; i < tabRects.Length; i++)
            {
                if (i == leaderboardTab || !GUI.Button(tabRects[i], GUIContent.none, GUIStyle.none)) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "bounty-tower-tab-" + i);
                leaderboardTab = i;
                leaderboardScroll = Vector2.zero;
                RequestBountyTowerRefreshIfNeeded(false);
            }
        }

        private void DrawRankingDividerOrRange(BountyRankingSnapshot snapshot, bool pve,
            float moduleY, float textY, string resourceId, int rowsBeforeDivider, int visibleRows)
        {
            var first = pve ? bountyPveFirstVisibleRow : bountyPvpFirstVisibleRow;
            var dividerRank = snapshot == null ? -1 : snapshot.EligibilityDividerAfterRow;
            var dividerVisible = dividerRank > 0 && first + rowsBeforeDivider == dividerRank;
            if (dividerVisible)
            {
                DrawBountyTowerModule(resourceId, new Rect(80f, moduleY, 920f, 72f), 920, 72);
                DrawBountyTowerText(new Rect(pve ? 430f : 405f, textY, pve ? 264f : 310f, 60f),
                    string.Format(T("leaderboard.formal.eligibility"),
                        FormatBountyEligibility(snapshot.Eligibility)), pve ? 21 : 23,
                    BountyInk, TextAnchor.MiddleCenter, true);
                return;
            }

            var total = snapshot == null || snapshot.Rows == null ? 0 : snapshot.Rows.Length;
            var start = total == 0 ? 0 : first + 1;
            var end = Math.Min(total, first + visibleRows);
            DrawBountyTowerText(new Rect(350f, textY, 380f, 60f),
                string.Format(T("leaderboard.server.visibleRange"), start, end, total), 18,
                BountyMuted, TextAnchor.MiddleCenter, false);
        }

        private string FormatBountyCurrentRank(string value)
        {
            int numericValue;
            if (!int.TryParse(value, out numericValue)) return SafeBountyValue(value, "--");
            return string.Format(T("leaderboard.server.rankValue"), numericValue);
        }

        private string FormatBountyTicketCount(string value)
        {
            int numericValue;
            if (!int.TryParse(value, out numericValue)) return SafeBountyValue(value, "--");
            return string.Format(T("leaderboard.server.ticketCountValue"), numericValue);
        }

        private string FormatBountyEligibility(string value)
        {
            if (value == "top_60_percent") return T("leaderboard.server.eligibility.top60");
            if (value == "top_100") return T("leaderboard.server.eligibility.top100");
            return SafeBountyValue(value, "--");
        }

        private void HandleBountyRankingPointer()
        {
            if (leaderboardTab == 0 || Event.current == null) return;
            var interactionArea = new Rect(80f, 875f, 920f, 920f);
            var currentEvent = Event.current;
            if (currentEvent.type == EventType.ScrollWheel &&
                interactionArea.Contains(currentEvent.mousePosition))
            {
                AdjustBountyRankingStart(currentEvent.delta.y > 0f ? 3 : -3);
                currentEvent.Use();
                return;
            }
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 &&
                interactionArea.Contains(currentEvent.mousePosition))
            {
                bountyRankingPointerHeld = true;
                bountyRankingPointerLastY = currentEvent.mousePosition.y;
                bountyRankingPointerRemainder = 0f;
                return;
            }
            if (currentEvent.type == EventType.MouseDrag && bountyRankingPointerHeld)
            {
                bountyRankingPointerRemainder += bountyRankingPointerLastY - currentEvent.mousePosition.y;
                bountyRankingPointerLastY = currentEvent.mousePosition.y;
                var rowDelta = Mathf.FloorToInt(Mathf.Abs(bountyRankingPointerRemainder) / 48f);
                if (rowDelta > 0)
                {
                    AdjustBountyRankingStart(bountyRankingPointerRemainder > 0f ? rowDelta : -rowDelta);
                    bountyRankingPointerRemainder %= 48f;
                }
                currentEvent.Use();
                return;
            }
            if (currentEvent.type == EventType.MouseUp && bountyRankingPointerHeld)
            {
                bountyRankingPointerHeld = false;
                bountyRankingPointerRemainder = 0f;
            }
        }

        private void AdjustBountyRankingStart(int delta)
        {
            if (leaderboardTab == 1)
                bountyPveFirstVisibleRow = BountyRankingViewport.ClampStart(
                    bountyPveFirstVisibleRow + delta, BountyRowCount(bountyPveSnapshot), 15);
            else if (leaderboardTab == 2)
                bountyPvpFirstVisibleRow = BountyRankingViewport.ClampStart(
                    bountyPvpFirstVisibleRow + delta, BountyRowCount(bountyPvpSnapshot), 16);
        }

        private static int BountyRowCount(BountyRankingSnapshot snapshot)
        {
            return snapshot == null || snapshot.Rows == null ? 0 : snapshot.Rows.Length;
        }

        private void RequestBountyTowerRefreshIfNeeded(bool force)
        {
            if (bountySyncCoordinator == null || string.IsNullOrWhiteSpace(bountyAccessToken)) return;
            if (bountyRefreshTask != null && !bountyRefreshTask.IsCompleted) return;
            if (!force && bountyLastRefreshAt != DateTimeOffset.MinValue &&
                DateTimeOffset.UtcNow - bountyLastRefreshAt < TimeSpan.FromHours(1)) return;

            CancelBountyTowerRefresh();
            bountyRefreshCancellation = new CancellationTokenSource();
            bountyRefreshTask = RefreshBountyTowerFromServerCoreAsync(
                bountyRefreshCancellation.Token);
        }

        private async Task RefreshBountyTowerFromServerCoreAsync(CancellationToken cancellationToken)
        {
            bountySyncStatusKey = "leaderboard.server.loading";
            try
            {
                var result = await bountySyncCoordinator.LoadAsync(
                    bountyAccessToken, cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                ApplyBountyTowerSnapshots(result.Pve, result.Pvp, result.Daily);
                bountyLastRefreshAt = DateTimeOffset.UtcNow;
                bountySyncStatusKey = result.IsSynthetic
                    ? "leaderboard.server.synthetic"
                    : null;
            }
            catch (OperationCanceledException)
            {
                // Configuration and sign-out paths own the replacement status.
            }
            catch (Exception exception)
            {
                bountySyncStatusKey = "leaderboard.server.unavailable";
                Debug.LogWarning("Bounty Tower server refresh failed: " + exception.Message);
            }
        }

        private void CancelBountyTowerRefresh()
        {
            if (bountyRefreshCancellation == null) return;
            bountyRefreshCancellation.Cancel();
            bountyRefreshCancellation.Dispose();
            bountyRefreshCancellation = null;
            bountyRefreshTask = null;
        }

        private void DrawBountyTowerClaim(Rect area, bool canClaim, string page)
        {
            DrawBountyTowerText(area, T(canClaim ? "leaderboard.claimReward" :
                "leaderboard.formal.claimUnavailableButton"), 31,
                canClaim ? BountyCream : BountyDisabled, TextAnchor.MiddleCenter, true);
            if (!canClaim || !GUI.Button(area, GUIContent.none, GUIStyle.none)) return;
            PlayUiClick(UiAudioCueIds.Tap, "bounty-tower-claim-" + page);
            BountyTowerClaimRequested?.Invoke(page);
        }

        private void DrawBountyTowerModule(string resourceId, Rect area, int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetBountyTowerTexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight) return;
            var previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            GUI.color = previous;
        }

        private void DrawBountyTowerText(Rect area, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized)
        {
            var style = ArchiveStyle(fontSize, color, alignment, emphasized);
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            GUI.Label(area, new GUIContent(text ?? string.Empty), style);
        }

        private static string SafeBountyValue(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string ResolveRankingClaimSummaryKey(BountyRankingSnapshot snapshot, bool pve)
        {
            var key = snapshot == null ? null : snapshot.ClaimSummaryLocalizationKey;
            return string.IsNullOrWhiteSpace(key) || key == "leaderboard.formal.claim.pending"
                ? (pve ? "leaderboard.formal.claim.pveTime" : "leaderboard.formal.claim.pvpTime")
                : key;
        }

        private static string ResolveDailyClaimSummaryKey(DailyBountySnapshot snapshot)
        {
            var key = snapshot == null ? null : snapshot.ClaimSummaryLocalizationKey;
            return string.IsNullOrWhiteSpace(key) || key == "leaderboard.formal.claim.pending"
                ? "leaderboard.formal.claim.dailyTime"
                : key;
        }

        private void EnsureBountyTowerEditorTestData()
        {
#if UNITY_EDITOR
            if (bountySnapshotsSupplied || bountyEditorFixturesApplied) return;
            bountyEditorFixturesApplied = true;
            bountySyncStatusKey = null;
            bountyPveSnapshot = BuildBountyTowerEditorRanking(
                "12,480", "15:00", "前60%", "第 37 名", "531 SKR", true);
            bountyPvpSnapshot = BuildBountyTowerEditorRanking(
                "9,680", "15:00", "前100名", "第 90 名", "39 SKR", false);
            bountyDailySnapshot = new DailyBountySnapshot
            {
                Pool = "1,000",
                OwnedTicketCount = "7张",
                EstimatedSingleTicketWinRate = "5.00%",
                DrawCountdown = "16:00:00",
                ClaimSummaryLocalizationKey = "leaderboard.formal.claim.dailyTime",
                CanClaim = true,
                TicketRows = new[]
                {
                    Ticket("BNT-20250418-7F3K", "D3", "2025-04-18 20:14"),
                    Ticket("BNT-20250418-6D9M", "D2", "2025-04-18 19:36"),
                    Ticket("BNT-20250418-5K2A", "D1", "2025-04-18 18:22"),
                    Ticket("BNT-20250418-4P7D", "D0", "2025-04-18 16:11"),
                    Ticket("BNT-20250418-3H8L", "D2", "2025-04-18 14:27"),
                    Ticket("BNT-20250418-2N6V", "D1", "2025-04-18 13:03"),
                    Ticket("BNT-20250418-1Q9J", "D3", "2025-04-18 11:48")
                }
            };
#endif
        }

#if UNITY_EDITOR
        private static BountyRankingSnapshot BuildBountyTowerEditorRanking(string pool, string update,
            string eligibility, string currentRank, string reward, bool pve)
        {
            const int rowCount = 500;
            var rows = new BountyRankRow[rowCount];
            for (var i = 0; i < rows.Length; i++)
            {
                var rank = i + 1;
                var currentPlayer = pve ? rank == 37 : rank == 90;
                var score = pve ? Math.Max(1000, 20000 - i * 23) : Math.Max(100, 5000 - i * 7);
                var rowReward = pve
                    ? (rank <= 10 ? Math.Max(0, 650 - rank * 50) + " SKR" : "0 SKR")
                    : (rank <= 100 ? Math.Max(0, 129 - rank) + " SKR" : "0 SKR");
                rows[i] = new BountyRankRow
                {
                    Rank = rank.ToString(),
                    PlayerName = currentPlayer ? "我" : (pve ? "潮汐旅人 " : "竞技旅人 ") + rank.ToString("000"),
                    Score = score.ToString("N0"),
                    Reward = rowReward,
                    IsCurrentPlayer = currentPlayer
                };
            }

            return new BountyRankingSnapshot
            {
                Pool = pool,
                NextUpdateHongKong = update,
                Eligibility = eligibility,
                CurrentRank = currentRank,
                CurrentScore = pve ? rows[36].Score : rows[89].Score,
                EstimatedReward = reward,
                SettlementCountdown = pve ? "3天 12:36:18" : "2天 00:00:00",
                EligibilityDividerAfterRow = pve ? 300 : -1,
                ClaimSummaryLocalizationKey = pve
                    ? "leaderboard.formal.claim.pveTime"
                    : "leaderboard.formal.claim.pvpTime",
                CanClaim = true,
                Rows = rows
            };
        }

        private static BountyTicketSnapshot Ticket(string id, string sourceCode, string acquiredAt)
        {
            return new BountyTicketSnapshot
            {
                TicketId = id,
                SourceCode = sourceCode,
                AcquiredAtHongKong = acquiredAt
            };
        }
#endif
    }
}
