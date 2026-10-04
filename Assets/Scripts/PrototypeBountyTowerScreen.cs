using System;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
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
#if UNITY_EDITOR
        private bool bountySnapshotsSupplied;
        private bool bountyEditorFixturesApplied;
#endif

        public event Action<string> BountyTowerClaimRequested;

        public void ApplyBountyTowerSnapshots(BountyRankingSnapshot pve,
            BountyRankingSnapshot pvp, DailyBountySnapshot daily)
        {
#if UNITY_EDITOR
            bountySnapshotsSupplied = true;
#endif
            bountyPveSnapshot = pve ?? BountyRankingSnapshot.Empty();
            bountyPvpSnapshot = pvp ?? BountyRankingSnapshot.Empty();
            bountyDailySnapshot = daily ?? DailyBountySnapshot.Empty();
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
                ? new Rect(760f, 158f, 240f, 30f)
                : leaderboardTab == 1
                    ? new Rect(760f, 140f, 240f, 30f)
                    : new Rect(760f, 154f, 240f, 30f);
            var poolValueRect = leaderboardTab == 0
                ? new Rect(742f, 184f, 270f, 52f)
                : leaderboardTab == 1
                    ? new Rect(742f, 166f, 270f, 52f)
                    : new Rect(742f, 180f, 270f, 52f);
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
                    : new Rect(742f, 222f, 270f, 28f);
                DrawBountyTowerText(nextUpdateRect,
                    string.Format(T("leaderboard.formal.nextUpdate"), SafeBountyValue(update, "--:--")), 16,
                    BountyCream, TextAnchor.MiddleCenter, false);
            }
        }

        private void DrawFormalRankingBounty(bool pve)
        {
            var snapshot = pve ? bountyPveSnapshot : bountyPvpSnapshot;
            DrawBountyTowerText(new Rect(150f, 478f, 780f, 70f),
                T(pve ? "leaderboard.formal.pveTitle" : "leaderboard.formal.pvpTitle"), 43,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(120f, 548f, 840f, 44f),
                T(pve ? "leaderboard.formal.pveSubtitle" : "leaderboard.formal.pvpSubtitle"), 24,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawBountyTowerText(new Rect(80f, 650f, 170f, 64f), T("leaderboard.rank"), 24,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(250f, 650f, 290f, 64f), T("leaderboard.playerName"), 24,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(540f, 650f, 230f, 64f),
                T(pve ? "leaderboard.formal.floor" : "leaderboard.arenaRating"), 24,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(770f, 650f, 230f, 64f), T("leaderboard.skrReward"), 24,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(100f, 760f, 880f, 54f), T("leaderboard.formal.disclaimer"), 23,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawFormalRankingRows(snapshot);
            DrawBountyTowerModule(BountyTowerAFormalArtContract.EligibilityDivider,
                new Rect(80f, 1618f, 920f, 72f), 920, 72);
            DrawBountyTowerText(new Rect(600f, 1578f, 360f, 42f),
                string.Format(T("leaderboard.formal.eligibility"), SafeBountyValue(snapshot.Eligibility, "--")), 22,
                BountyGold, TextAnchor.MiddleCenter, true);

            DrawBountyTowerText(new Rect(150f, 1828f, 360f, 54f),
                string.Format(T("leaderboard.formal.currentRank"), SafeBountyValue(snapshot.CurrentRank, "--")), 25,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(530f, 1828f, 360f, 54f),
                string.Format(T("leaderboard.formal.estimatedReward"),
                    SafeBountyValue(snapshot.EstimatedReward, "--")), 25,
                BountyGold, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(330f, 1920f, 600f, 70f),
                T(ResolveRankingClaimSummaryKey(snapshot, pve)), 27,
                BountyInk, TextAnchor.MiddleCenter, true);
            var claimRect = pve ? new Rect(342f, 2088f, 398f, 92f) : new Rect(342f, 2029f, 398f, 92f);
            DrawBountyTowerClaim(claimRect, snapshot.CanClaim, pve ? "pve" : "pvp");
        }

        private void DrawFormalRankingRows(BountyRankingSnapshot snapshot)
        {
            var rows = snapshot.Rows ?? Array.Empty<BountyRankRow>();
            if (rows.Length == 0)
            {
                DrawBountyTowerText(new Rect(100f, 965f, 880f, 120f),
                    T("leaderboard.formal.noRankingData"), 25, BountyMuted,
                    TextAnchor.MiddleCenter, false);
                return;
            }

            var viewport = new Rect(80f, 958f, 920f, 640f);
            var contentHeight = Mathf.Max(viewport.height, rows.Length * 80f + 14f);
            leaderboardScroll = GUI.BeginScrollView(viewport, leaderboardScroll,
                new Rect(0f, 0f, 920f, contentHeight), false, false, GUIStyle.none, GUIStyle.none);
            for (var i = 0; i < rows.Length; i++)
            {
                var row = rows[i] ?? new BountyRankRow();
                var rowY = 7f + i * 80f;
                if (row.IsCurrentPlayer)
                {
                    DrawBountyTowerModule(BountyTowerAFormalArtContract.SelectedRankRow,
                        new Rect(0f, rowY + 7f, 920f, 64f), 920, 64);
                }
                var rankColor = row.IsCurrentPlayer ? BountyWhite : BountyInk;
                var nameColor = row.IsCurrentPlayer ? BountyWhite : BountyInk;
                DrawBountyTowerText(new Rect(10f, rowY, 130f, 78f), SafeBountyValue(row.Rank, "--"), 25,
                    rankColor, TextAnchor.MiddleCenter, i < 3);
                DrawBountyTowerText(new Rect(150f, rowY, 300f, 78f),
                    row.IsCurrentPlayer ? T("leaderboard.formal.me") : SafeBountyValue(row.PlayerName, "--"), 26,
                    nameColor, TextAnchor.MiddleCenter, row.IsCurrentPlayer);
                DrawBountyTowerText(new Rect(450f, rowY, 220f, 78f), SafeBountyValue(row.Score, "--"), 24,
                    row.IsCurrentPlayer ? BountyWhite : BountyMuted, TextAnchor.MiddleCenter, false);
                DrawBountyTowerText(new Rect(680f, rowY, 220f, 78f), SafeBountyValue(row.Reward, "--"), 24,
                    row.IsCurrentPlayer ? BountyWhite : BountyGold, TextAnchor.MiddleCenter, true);
            }
            GUI.EndScrollView();
        }

        private void DrawFormalDailyBounty()
        {
            var daily = bountyDailySnapshot;
            DrawBountyTowerText(new Rect(150f, 478f, 780f, 70f), T("leaderboard.daily"), 43,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(120f, 548f, 840f, 44f), T("leaderboard.formal.dailySubtitle"), 24,
                BountyMuted, TextAnchor.MiddleCenter, false);

            DrawDailyMetric(new Rect(218f, 642f, 150f, 42f), new Rect(218f, 682f, 150f, 62f),
                "leaderboard.formal.dailyTickets", daily.Tickets, 34, BountyInk);
            DrawDailyMetric(new Rect(520f, 642f, 150f, 42f), new Rect(510f, 682f, 170f, 62f),
                "leaderboard.formal.dailyCumulative", daily.CumulativeReward, 32, BountyGold);
            DrawDailyMetric(new Rect(820f, 642f, 150f, 42f), new Rect(804f, 682f, 182f, 62f),
                "leaderboard.formal.dailyCountdown", daily.RefreshCountdown, 30, BountyInk);

            var stepKeys = new[] { "leaderboard.formal.step.sail", "leaderboard.formal.step.win",
                "leaderboard.formal.step.ticket", "leaderboard.formal.step.claim" };
            var stepCenters = new[] { 120f, 365f, 610f, 855f };
            for (var i = 0; i < stepKeys.Length; i++)
                DrawBountyTowerText(new Rect(stepCenters[i] - 90f, 1134f, 180f, 46f), T(stepKeys[i]), 22,
                    BountyInk, TextAnchor.MiddleCenter, true);

            var tasks = daily.Tasks ?? Array.Empty<DailyBountyTaskSnapshot>();
            var y = new[] { 1280f, 1390f, 1500f };
            var badges = new[] { BountyTowerAFormalArtContract.DifficultyGold,
                BountyTowerAFormalArtContract.DifficultyTeal, BountyTowerAFormalArtContract.DifficultyBlue };
            var badgeLabels = new[] { "leaderboard.formal.difficulty.high",
                "leaderboard.formal.difficulty.standard", "leaderboard.formal.difficulty.base" };
            for (var i = 0; i < 3; i++)
            {
                var task = i < tasks.Length && tasks[i] != null ? tasks[i] : new DailyBountyTaskSnapshot();
                DrawBountyTowerText(new Rect(105f, y[i], 330f, 68f),
                    T(SafeBountyValue(task.NameLocalizationKey, "leaderboard.formal.empty")), 27,
                    BountyInk, TextAnchor.MiddleCenter, true);
                DrawBountyTowerText(new Rect(760f, y[i], 180f, 68f), SafeBountyValue(task.Progress, "-- / --"), 25,
                    BountyMuted, TextAnchor.MiddleCenter, true);
                DrawBountyTowerModule(badges[i], new Rect(480f, y[i] + 12f, 144f, 44f), 144, 44);
                DrawBountyTowerText(new Rect(480f, y[i] + 12f, 144f, 44f), T(badgeLabels[i]), 20,
                    BountyWhite, TextAnchor.MiddleCenter, true);
            }

            DrawBountyTowerText(new Rect(280f, 1828f, 520f, 54f),
                string.Format(T("leaderboard.formal.dailyProgress"), SafeBountyValue(daily.DailyProgress, "-- / --")), 26,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerText(new Rect(330f, 1920f, 600f, 70f),
                T(ResolveDailyClaimSummaryKey(daily)), 27,
                BountyInk, TextAnchor.MiddleCenter, true);
            DrawBountyTowerClaim(new Rect(342f, 1991f, 398f, 92f), daily.CanClaim, "daily");
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
            }
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
            bountyPveSnapshot = BuildBountyTowerEditorRanking(
                "12,480", "15:00", "前60%", "#50", "170 SKR", true);
            bountyPvpSnapshot = BuildBountyTowerEditorRanking(
                "9,680", "15:00", "前100名", "#50", "140 SKR", false);
            bountyDailySnapshot = new DailyBountySnapshot
            {
                Pool = "1,000",
                Tickets = "3 / 5",
                CumulativeReward = "120 SKR",
                RefreshCountdown = "08:42:16",
                DailyProgress = "3 / 5",
                ClaimSummaryLocalizationKey = "leaderboard.formal.claim.dailyTime",
                CanClaim = true,
                Tasks = new[]
                {
                    new DailyBountyTaskSnapshot
                    {
                        NameLocalizationKey = "leaderboard.formal.task.expedition3", Progress = "0 / 3"
                    },
                    new DailyBountyTaskSnapshot
                    {
                        NameLocalizationKey = "leaderboard.formal.task.pve10", Progress = "6 / 10"
                    },
                    new DailyBountyTaskSnapshot
                    {
                        NameLocalizationKey = "leaderboard.formal.task.forge1", Progress = "1 / 1"
                    }
                }
            };
#endif
        }

#if UNITY_EDITOR
        private static BountyRankingSnapshot BuildBountyTowerEditorRanking(string pool, string update,
            string eligibility, string currentRank, string reward, bool pve)
        {
            var rows = new BountyRankRow[8];
            for (var i = 0; i < rows.Length; i++)
            {
                rows[i] = new BountyRankRow
                {
                    Rank = i < 3 ? (i + 1).ToString() : "#" + (47 + i),
                    PlayerName = "潮汐旅人 " + (i + 1).ToString("00"),
                    Score = pve ? (88 - i).ToString() : (2208 - i * 12).ToString(),
                    Reward = (200 - i * 10) + " SKR",
                    IsCurrentPlayer = i == 3
                };
            }

            return new BountyRankingSnapshot
            {
                Pool = pool,
                NextUpdateHongKong = update,
                Eligibility = eligibility,
                CurrentRank = currentRank,
                EstimatedReward = reward,
                ClaimSummaryLocalizationKey = pve
                    ? "leaderboard.formal.claim.pveTime"
                    : "leaderboard.formal.claim.pvpTime",
                CanClaim = true,
                Rows = rows
            };
        }
#endif
    }
}
