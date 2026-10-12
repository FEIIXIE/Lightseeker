using System;
using System.Collections;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using SeekerAutoChess.Prototype.Presentation;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    internal static class PrototypeArenaFormalVerification
    {
        private const string RequestFileName = "VerifyFormalArena.request";
        private const string ActiveKey = "seeker.verify.formalArena.active";
        private const string StageKey = "seeker.verify.formalArena.stage";
        private const string WaitKey = "seeker.verify.formalArena.wait";
        private const string RefreshedKey = "seeker.verify.formalArena.refreshed";
        private const string CommandLineKey = "seeker.verify.formalArena.commandLine";
        private const string Version = "v0.43.413";
        private const string NormalScreenshot =
            "SeekerAutoChess-v0.43.404-arena-e-normal-1080x2400.png";
        private const string DetailScreenshot =
            "SeekerAutoChess-v0.43.404-arena-e-opponent-picker-1080x2400.png";

        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;

        static PrototypeArenaFormalVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Seeker/Verify Formal Arena E v1.0.2")]
        private static void StartFromMenu()
        {
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(WaitKey, 0);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        public static void StartFromCommandLine()
        {
            SessionState.SetBool(CommandLineKey, true);
            StartFromMenu();
        }

        private static void Update()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (File.Exists(requestPath))
            {
                if (!SessionState.GetBool(RefreshedKey, false))
                {
                    SessionState.SetBool(RefreshedKey, true);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                    return;
                }
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                File.Delete(requestPath);
                SessionState.SetBool(RefreshedKey, false);
                StartFromMenu();
            }

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
                        EditorApplication.EnterPlaymode();
                    }
                    return;
                }

                var bootstrap = UnityEngine.Object.FindObjectOfType<PrototypeBootstrap>();
                if (bootstrap == null && stage == 0)
                {
                    var host = new GameObject("FormalArenaVerificationBootstrap");
                    bootstrap = host.AddComponent<PrototypeBootstrap>();
                    nextActionAt = EditorApplication.timeSinceStartup + 2d;
                    return;
                }
                if (bootstrap == null)
                {
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if (stage == 0)
                {
                    DeletePriorEvidence();
                    Screen.SetResolution(1080, 2400, false);
                    ConfigureArena(bootstrap, false);
                    SessionState.SetInt(StageKey, 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 3d;
                    return;
                }

                if (stage == 1)
                {
                    BeginCapture(bootstrap, NormalScreenshot, 2);
                    return;
                }

                if (stage == 2)
                {
                    if (!WaitForCapture("normal Arena page")) return;
                    ConfigureArena(bootstrap, true);
                    SessionState.SetInt(StageKey, 3);
                    SessionState.SetInt(WaitKey, 0);
                    nextActionAt = EditorApplication.timeSinceStartup + 2d;
                    return;
                }

                if (stage == 3)
                {
                    BeginCapture(bootstrap, DetailScreenshot, 4);
                    return;
                }

                if (stage == 4)
                {
                    if (!WaitForCapture("Arena opponent detail")) return;
                    BuildComparisonEvidence();
                    WriteResult(projectRoot, true,
                        "Captured Arena E v1.0.2 main/opponent-picker states at 1080x2400 and generated overlay plus difference evidence.");
                    Finish();
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(projectRoot, false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static void BeginCapture(PrototypeBootstrap bootstrap, string fileName, int nextStage)
        {
            captureCompleted = false;
            captureFailure = null;
            bootstrap.StartCoroutine(CaptureFrame(fileName));
            SessionState.SetInt(StageKey, nextStage);
            SessionState.SetInt(WaitKey, 0);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        private static bool WaitForCapture(string stateName)
        {
            if (captureCompleted)
            {
                if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);
                return true;
            }

            var wait = SessionState.GetInt(WaitKey, 0) + 1;
            SessionState.SetInt(WaitKey, wait);
            if (wait > 12) throw new TimeoutException("Timed out waiting for " + stateName + " screenshot");
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
            return false;
        }

        private static void ConfigureArena(PrototypeBootstrap bootstrap, bool detail)
        {
            var viewField = FindField("currentView");
            viewField.SetValue(bootstrap, Enum.Parse(viewField.FieldType, "Arena"));
            FindField("arenaRating").SetValue(bootstrap, 1090);
            FindField("arenaChallengesRemaining").SetValue(bootstrap, 4);
            FindField("arenaRankingScroll").SetValue(bootstrap, Vector2.zero);
            var opponents = ArenaPrototypeRules.CreateDeterministicBots(1090);
            var playerRank = ArenaPrototypeRules.CalculatePlayerDisplayRank(opponents, 1090);
            var candidates = ArenaPrototypeRules.SelectNearby(opponents, playerRank, 6);
            FindField("selectedArenaOpponentIndex").SetValue(bootstrap,
                detail && candidates.Length > 0 ? Array.IndexOf(opponents, candidates[0]) : -1);
            FindField("arenaOpponents").SetValue(bootstrap, opponents);
            var stateField = FindField("arenaPageState");
            stateField.SetValue(bootstrap, Enum.Parse(stateField.FieldType,
                detail ? "OpponentDetail" : "Ranking"));
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

        private static IEnumerator CaptureFrame(string fileName)
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) throw new InvalidOperationException("Frame-end screenshot texture is null");
                if (texture.width != 1080 || texture.height != 2400)
                    throw new InvalidOperationException("Unexpected Arena screenshot size: " +
                                                        texture.width + "x" + texture.height);
                Directory.CreateDirectory(OutputDirectory());
                File.WriteAllBytes(Path.Combine(OutputDirectory(), fileName), texture.EncodeToPNG());
            }
            catch (Exception exception)
            {
                captureFailure = exception.GetBaseException().Message;
            }
            finally
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                captureCompleted = true;
            }
        }

        private static void BuildComparisonEvidence()
        {
            BuildComparison(NormalScreenshot,
                "主美-竞技页面E统一风格-正式组合QA-主页面-v1.0.2.png", "normal");
            BuildComparison(DetailScreenshot,
                "主美-竞技页面E统一风格-正式组合QA-寻找对手浮窗-v1.0.2.png", "opponent-picker");
        }

        private static void BuildComparison(string screenshotName, string baselineName, string stateName)
        {
            var screenshot = LoadTexture(Path.Combine(OutputDirectory(), screenshotName));
            var baseline = LoadTexture(Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/meishu/_accepted/ui/arena-page-e-unified-v1.0.2/preview/" +
                baselineName)));
            try
            {
                Require(screenshot.width == 1080 && screenshot.height == 2400,
                    "Arena screenshot is not 1080x2400: " + stateName);
                Require(baseline.width == screenshot.width && baseline.height == screenshot.height,
                    "Arena baseline size differs from screenshot: " + stateName);
                WriteComparison(baseline, screenshot, stateName, false);
                WriteComparison(baseline, screenshot, stateName, true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screenshot);
                UnityEngine.Object.DestroyImmediate(baseline);
            }
        }

        private static Texture2D LoadTexture(string path)
        {
            Require(File.Exists(path), "Missing image: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Require(texture.LoadImage(File.ReadAllBytes(path), false), "Cannot decode image: " + path);
            return texture;
        }

        private static void WriteComparison(Texture2D baseline, Texture2D screenshot,
            string stateName, bool difference)
        {
            var baselinePixels = baseline.GetPixels32();
            var screenshotPixels = screenshot.GetPixels32();
            var result = new Color32[baselinePixels.Length];
            for (var index = 0; index < result.Length; index++)
            {
                if (difference)
                {
                    result[index] = new Color32(
                        (byte)Math.Abs(baselinePixels[index].r - screenshotPixels[index].r),
                        (byte)Math.Abs(baselinePixels[index].g - screenshotPixels[index].g),
                        (byte)Math.Abs(baselinePixels[index].b - screenshotPixels[index].b), 255);
                }
                else
                {
                    result[index] = new Color32(
                        (byte)((baselinePixels[index].r + screenshotPixels[index].r) / 2),
                        (byte)((baselinePixels[index].g + screenshotPixels[index].g) / 2),
                        (byte)((baselinePixels[index].b + screenshotPixels[index].b) / 2), 255);
                }
            }

            var comparison = new Texture2D(baseline.width, baseline.height, TextureFormat.RGBA32, false);
            try
            {
                comparison.SetPixels32(result);
                comparison.Apply(false, false);
                var suffix = difference ? "difference" : "overlay-50pct";
                File.WriteAllBytes(Path.Combine(OutputDirectory(),
                    "SeekerAutoChess-v0.43.404-arena-e-" + stateName + "-" + suffix +
                    "-1080x2400.png"), comparison.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(comparison); }
        }

        private static string OutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
        }

        private static void DeletePriorEvidence()
        {
            var directory = OutputDirectory();
            if (!Directory.Exists(directory)) return;
            foreach (var fileName in new[]
            {
                NormalScreenshot,
                DetailScreenshot,
                "SeekerAutoChess-v0.43.404-arena-e-normal-overlay-50pct-1080x2400.png",
                "SeekerAutoChess-v0.43.404-arena-e-normal-difference-1080x2400.png",
                "SeekerAutoChess-v0.43.404-arena-e-opponent-picker-overlay-50pct-1080x2400.png",
                "SeekerAutoChess-v0.43.404-arena-e-opponent-picker-difference-1080x2400.png"
            })
            {
                var path = Path.Combine(directory, fileName);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void WriteResult(string projectRoot, bool passed, string message)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, "../../outputs/test-results"));
            Directory.CreateDirectory(outputDirectory);
            var result = JsonUtility.ToJson(new Result
            {
                version = Version,
                packageVersion = ArenaFormalArtContract.PackageVersion,
                passed = passed,
                message = message,
                checkedAt = DateTime.UtcNow.ToString("O")
            }, true);
            File.WriteAllText(Path.Combine(outputDirectory,
                "主程-v0.43.404远征账户合同修复-运行时验证-v1.0.json"), result);
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            var exitEditor = SessionState.GetBool(CommandLineKey, false);
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(WaitKey, 0);
            SessionState.SetBool(CommandLineKey, false);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            if (exitEditor) EditorApplication.delayCall += () => EditorApplication.Exit(0);
        }

        [Serializable]
        private sealed class Result
        {
            public string version;
            public string packageVersion;
            public bool passed;
            public string message;
            public string checkedAt;
        }
    }
}
