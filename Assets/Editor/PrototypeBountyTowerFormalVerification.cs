using System;
using System.Collections;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

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
            "SeekerAutoChess-v0.43.244-bounty-daily-1080x2400.png",
            "SeekerAutoChess-v0.43.244-bounty-pve-1080x2400.png",
            "SeekerAutoChess-v0.43.244-bounty-pvp-1080x2400.png"
        };

        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;

        static PrototypeBountyTowerFormalVerification()
        {
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
                if (!EditorApplication.isPlaying)
                {
                    if (stage == 0)
                    {
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
                    SetTab(bootstrap, 0);
                    SessionState.SetInt(StageKey, 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 3d;
                    return;
                }

                if (stage == 1 || stage == 3 || stage == 5)
                {
                    captureCompleted = false;
                    captureFailure = null;
                    bootstrap.StartCoroutine(CaptureFrame((stage - 1) / 2));
                    SessionState.SetInt(StageKey, stage + 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if (stage == 2 || stage == 4 || stage == 6)
                {
                    if (!captureCompleted)
                    {
                        nextActionAt = EditorApplication.timeSinceStartup + 1d;
                        return;
                    }
                    if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);
                    if (stage < 6)
                    {
                        SetTab(bootstrap, stage / 2);
                        SessionState.SetInt(StageKey, stage + 1);
                        nextActionAt = EditorApplication.timeSinceStartup + 2d;
                        return;
                    }
                    WriteResult(true, "Captured Daily, PVE and PVP formal Bounty Tower pages at 1080x2400.");
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
            var editorAssembly = typeof(Editor).Assembly;
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
                BuildRanking("12,000", "15:00", "前60%", "#50", "170 SKR", true),
                BuildRanking("10,000", "15:00", "前100名", "#50", "140 SKR", false),
                new DailyBountySnapshot
                {
                    Pool = "1,000",
                    Tickets = "3 / 5",
                    CumulativeReward = "120 SKR",
                    RefreshCountdown = "08:42:16",
                    DailyProgress = "3 / 5",
                    ClaimSummaryLocalizationKey = "leaderboard.formal.claim.dailyTime",
                    Tasks = new[]
                    {
                        new DailyBountyTaskSnapshot { NameLocalizationKey = "leaderboard.formal.task.expedition3", Progress = "0 / 3" },
                        new DailyBountyTaskSnapshot { NameLocalizationKey = "leaderboard.formal.task.pve10", Progress = "6 / 10" },
                        new DailyBountyTaskSnapshot { NameLocalizationKey = "leaderboard.formal.task.forge1", Progress = "1 / 1" }
                    }
                });
        }

        private static BountyRankingSnapshot BuildRanking(string pool, string update,
            string eligibility, string currentRank, string reward, bool pve)
        {
            var rows = new BountyRankRow[8];
            for (var i = 0; i < rows.Length; i++)
            {
                rows[i] = new BountyRankRow
                {
                    Rank = i < 3 ? (i + 1).ToString() : "#" + (47 + i),
                    PlayerName = i == 3 ? "我" : "潮汐旅人 " + (i + 1).ToString("00"),
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
                Rows = rows
            };
        }

        private static void SetView(PrototypeBootstrap bootstrap)
        {
            var field = FindField("currentView");
            field.SetValue(bootstrap, Enum.Parse(field.FieldType, "Leaderboard"));
        }

        private static void SetTab(PrototypeBootstrap bootstrap, int tab)
        {
            FindField("leaderboardTab").SetValue(bootstrap, tab);
            FindField("leaderboardScroll").SetValue(bootstrap, Vector2.zero);
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

        private static IEnumerator CaptureFrame(int index)
        {
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
                "../../../outputs/主程-v0.43.244赏金塔文字与测试数据修正-验收证据-v1.0"));
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
