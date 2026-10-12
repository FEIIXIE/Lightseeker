using System;
using System.Collections;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    internal static class PrototypeBountyTowerFormalVerification
    {
        private const string RequestFileName = "VerifyFormalBountyTower.request";
        private const string ActiveKey = "seeker.verify.bountyTower.active";
        private const string StageKey = "seeker.verify.bountyTower.stage";
        private const string BootstrapWaitKey = "seeker.verify.bountyTower.bootstrapWait";
        private const string CommandLineKey = "seeker.verify.bountyTower.commandLine";
        private static readonly string[] ScreenshotNames =
        {
            "SeekerAutoChess-v0.43.265-bounty-daily-1080x2400.png",
            "SeekerAutoChess-v0.43.265-bounty-pve-rank-1-15-1080x2400.png",
            "SeekerAutoChess-v0.43.265-bounty-pve-rank-32-46-1080x2400.png",
            "SeekerAutoChess-v0.43.265-bounty-pvp-rank-90-105-1080x2400.png",
            "SeekerAutoChess-v0.43.265-bounty-pvp-tail-1080x2400.png"
        };

        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;
        private static int lastLoggedStage = -1;

        static PrototypeBountyTowerFormalVerification()
        {
            // A tester may deliberately stop Play Mode while a capture is active. Treat that
            // as cancellation after a domain reload instead of leaving a stale stage waiting
            // forever or unexpectedly starting another verification pass.
            if (SessionState.GetBool(ActiveKey, false) &&
                SessionState.GetInt(StageKey, 0) > 0 && !EditorApplication.isPlaying)
            {
                SessionState.SetBool(ActiveKey, false);
                SessionState.SetInt(StageKey, 0);
                SessionState.SetInt(BootstrapWaitKey, 0);
            }
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
            ConsumeRequestIfReady();
        }

        public static void StartFromCommandLine()
        {
            SessionState.SetBool(CommandLineKey, true);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(BootstrapWaitKey, 0);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        private static void Update()
        {
            ConsumeRequestIfReady();

            if (!SessionState.GetBool(ActiveKey, false) ||
                EditorApplication.timeSinceStartup < nextActionAt) return;

            try
            {
                var stage = SessionState.GetInt(StageKey, 0);
                if (stage != lastLoggedStage)
                {
                    lastLoggedStage = stage;
                    Debug.Log("Bounty Tower formal verification stage " + stage +
                              "; playing=" + EditorApplication.isPlaying);
                }
                if (!EditorApplication.isPlaying)
                {
                    if (stage == 0)
                    {
                        const string prototypeScene = "Assets/Scenes/SampleScene.unity";
                        if (SceneManager.GetActiveScene().path != prototypeScene)
                            EditorSceneManager.OpenScene(prototypeScene, OpenSceneMode.Single);
                        EditorApplication.ExecuteMenuItem("Window/General/Game");
                        SelectFixedGameViewSize(1080, 2400);
                        EditorApplication.EnterPlaymode();
                    }
                    return;
                }

                var bootstrap = UnityEngine.Object.FindObjectOfType<PrototypeBootstrap>();
                if (bootstrap == null)
                {
                    var wait = SessionState.GetInt(BootstrapWaitKey, 0) + 1;
                    SessionState.SetInt(BootstrapWaitKey, wait);
                    Debug.Log("Bounty Tower verification is waiting for PrototypeBootstrap; attempt " + wait + ".");
                    if (stage == 0 && wait == 1)
                    {
                        // A tester may have left another scene running. Return to edit mode,
                        // then let stage zero enter the configured prototype scene cleanly.
                        EditorApplication.ExitPlaymode();
                        nextActionAt = EditorApplication.timeSinceStartup + 2d;
                        return;
                    }
                    if (wait > 15)
                        throw new TimeoutException("Timed out waiting for PrototypeBootstrap");
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if (stage == 0)
                {
                    Screen.SetResolution(1080, 2400, false);
                    ConfigureSnapshots(bootstrap);
                    SetView(bootstrap);
                    SetCaptureState(bootstrap, 0);
                    SessionState.SetInt(StageKey, 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 3d;
                    return;
                }

                if ((stage & 1) == 1 && stage <= 9)
                {
                    captureCompleted = false;
                    captureFailure = null;
                    bootstrap.StartCoroutine(CaptureFrame(bootstrap, (stage - 1) / 2));
                    SessionState.SetInt(StageKey, stage + 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if ((stage & 1) == 0 && stage >= 2 && stage <= 10)
                {
                    if (!captureCompleted)
                    {
                        nextActionAt = EditorApplication.timeSinceStartup + 1d;
                        return;
                    }
                    if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);
                    if (stage < 10)
                    {
                        SetCaptureState(bootstrap, stage / 2);
                        SessionState.SetInt(StageKey, stage + 1);
                        nextActionAt = EditorApplication.timeSinceStartup + 2d;
                        return;
                    }
                    WriteResult(true,
                        "Captured Daily, PVE ranks 1-15 and 32-46, PVP ranks 90-105 and leaderboard tail at 1080x2400.");
                    Finish();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static void ConsumeRequestIfReady()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (!File.Exists(requestPath)) return;

            File.Delete(requestPath);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(BootstrapWaitKey, 0);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
            Debug.Log("Bounty Tower formal verification started.");
        }

        private static void SelectFixedGameViewSize(int width, int height)
        {
            var editorAssembly = typeof(UnityEditor.Editor).Assembly;
            var gameViewType = editorAssembly.GetType("UnityEditor.GameView");
            var sizesType = editorAssembly.GetType("UnityEditor.GameViewSizes");
            if (gameViewType == null || sizesType == null)
                throw new InvalidOperationException("Unity GameView reflection types are unavailable");

            var gameView = EditorWindow.GetWindow(gameViewType, false, "Game", true);
            var singletonType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instanceProperty = singletonType.GetProperty("instance",
                BindingFlags.Static | BindingFlags.Public);
            var sizes = instanceProperty == null ? null : instanceProperty.GetValue(null, null);
            if (sizes == null) throw new InvalidOperationException("Unity GameViewSizes instance is unavailable");

            var groupTypeProperty = sizesType.GetProperty("currentGroupType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var getGroupMethod = sizesType.GetMethod("GetGroup",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (groupTypeProperty == null || getGroupMethod == null)
                throw new InvalidOperationException("Unity GameViewSizes group API is unavailable");
            var groupType = groupTypeProperty.GetValue(sizes, null);
            var group = getGroupMethod.Invoke(sizes, new[] { groupType });
            if (group == null) throw new InvalidOperationException("Current Unity GameViewSizeGroup is unavailable");

            var displayTextsMethod = group.GetType().GetMethod("GetDisplayTexts",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var displayTexts = displayTextsMethod == null
                ? null
                : displayTextsMethod.Invoke(group, null) as string[];
            if (displayTexts == null) throw new InvalidOperationException("Unity GameView size labels are unavailable");

            var selectedIndex = -1;
            var compactNeedle = width + "x" + height;
            for (var index = 0; index < displayTexts.Length; index++)
            {
                var compact = (displayTexts[index] ?? string.Empty).Replace(" ", string.Empty);
                if (compact.IndexOf(compactNeedle, StringComparison.OrdinalIgnoreCase) < 0) continue;
                selectedIndex = index;
                break;
            }
            if (selectedIndex < 0)
                throw new InvalidOperationException("A fixed 1080x2400 Game view size is not configured");

            var selectedSizeProperty = gameViewType.GetProperty("selectedSizeIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (selectedSizeProperty == null)
                throw new InvalidOperationException("Unity GameView selected-size API is unavailable");
            selectedSizeProperty.SetValue(gameView, selectedIndex, null);
            gameView.Repaint();
        }

        private static void ConfigureSnapshots(PrototypeBootstrap bootstrap)
        {
            bootstrap.ApplyBountyTowerSnapshots(
                BuildRanking("12,480", "15:00", "前60%", "第 37 名", "531 SKR", true),
                BuildRanking("9,680", "15:00", "前100名", "第 90 名", "39 SKR", false),
                new DailyBountySnapshot
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
                });
        }

        private static BountyRankingSnapshot BuildRanking(string pool, string update,
            string eligibility, string currentRank, string reward, bool pve)
        {
            var rows = new BountyRankRow[500];
            for (var i = 0; i < rows.Length; i++)
            {
                var rank = i + 1;
                var currentPlayer = pve ? rank == 37 : rank == 90;
                var score = pve ? Math.Max(1000, 20000 - i * 23) : Math.Max(100, 5000 - i * 7);
                rows[i] = new BountyRankRow
                {
                    Rank = rank.ToString(),
                    PlayerName = currentPlayer ? "我" : (pve ? "潮汐旅人 " : "竞技旅人 ") + rank.ToString("000"),
                    Score = score.ToString("N0"),
                    Reward = rank <= 10 ? Math.Max(0, 650 - rank * 50) + " SKR" : "0 SKR",
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

        private static void SetView(PrototypeBootstrap bootstrap)
        {
            var field = FindField("currentView");
            field.SetValue(bootstrap, Enum.Parse(field.FieldType, "Leaderboard"));
        }

        private static void SetCaptureState(PrototypeBootstrap bootstrap, int captureIndex)
        {
            var tab = captureIndex == 0 ? 0 : captureIndex <= 2 ? 1 : 2;
            FindField("leaderboardTab").SetValue(bootstrap, tab);
            FindField("leaderboardScroll").SetValue(bootstrap, Vector2.zero);
            FindField("bountyPveFirstVisibleRow").SetValue(bootstrap, captureIndex == 2 ? 31 : 0);
            FindField("bountyPvpFirstVisibleRow").SetValue(bootstrap,
                captureIndex == 3 ? 89 : captureIndex == 4 ? 484 : 0);
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static FieldInfo FindField(string name)
        {
            var field = typeof(PrototypeBootstrap).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(PrototypeBootstrap).FullName, name);
            return field;
        }

        private static IEnumerator CaptureFrame(PrototypeBootstrap bootstrap, int index)
        {
            // Re-apply the requested state in the same frame as the capture. Background
            // editors may stop advancing ordinary `yield return null` frames, while
            // WaitForEndOfFrame remains reliable for Game-view screenshots.
            SetCaptureState(bootstrap, index);
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) throw new InvalidOperationException("Bounty screenshot is null");
                if (texture.width != 1080 || texture.height != 2400)
                    throw new InvalidOperationException("Unexpected Bounty screenshot size: " +
                                                        texture.width + "x" + texture.height);
                Directory.CreateDirectory(OutputDirectory());
                File.WriteAllBytes(Path.Combine(OutputDirectory(), ScreenshotNames[index]), texture.EncodeToPNG());
            }
            catch (Exception exception) { captureFailure = exception.GetBaseException().Message; }
            finally
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                captureCompleted = true;
            }
        }

        private static string OutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/主程-v0.43.265赏金塔统一数字排名-验收证据-v1.0"));
        }

        private static void WriteResult(bool success, string message)
        {
            Directory.CreateDirectory(OutputDirectory());
            File.WriteAllText(Path.Combine(OutputDirectory(), "verification-result.txt"),
                (success ? "PASS" : "FAIL") + Environment.NewLine + message + Environment.NewLine);
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(BootstrapWaitKey, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            if (SessionState.GetBool(CommandLineKey, false))
            {
                SessionState.SetBool(CommandLineKey, false);
                EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
        }
    }
}
