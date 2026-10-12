using System;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Prototype;
using SeekerAutoChess.Prototype.Presentation;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    public static class PrototypeHarborFormalVerification
    {
        private const string RequestFileName = "VerifyFormalHarbor.request";
        private const string ActiveKey = "seeker.verify.formalHarbor.active";
        private const string StageKey = "seeker.verify.formalHarbor.stage";
        private const string WaitKey = "seeker.verify.formalHarbor.wait";
        private const string RefreshedKey = "seeker.verify.formalHarbor.refreshed";
        private const string CommandLineKey = "seeker.verify.formalHarbor.commandLine";
        private const string ScreenshotName =
            "SeekerAutoChess-v0.43.238-harbor-construction-alternate-1080x2400.png";
        private const string PriorNavigationScreenshot =
            "SeekerAutoChess-v0.43.201-navigation-harbor-1080x2400.png";

        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;

        static PrototypeHarborFormalVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        public static void StartFromCommandLine()
        {
            SessionState.SetBool(CommandLineKey, true);
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(WaitKey, 0);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
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
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(StageKey, 0);
                SessionState.SetInt(WaitKey, 0);
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
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
                        SelectFixedGameViewSize(1080, 2400);
                        EditorApplication.EnterPlaymode();
                    }
                    return;
                }

                var bootstrap = UnityEngine.Object.FindObjectOfType<PrototypeBootstrap>();
                if (bootstrap == null)
                {
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if (stage == 0)
                {
                    Screen.SetResolution(1080, 2400, false);
                    SetField(bootstrap, "currentView", "Harbor");
                    SetField(bootstrap, "homeTopMenu", 0);
                    SessionState.SetInt(StageKey, 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 3d;
                    return;
                }

                if (stage == 1)
                {
                    captureCompleted = false;
                    captureFailure = null;
                    CaptureFrameImmediate();
                    SessionState.SetInt(StageKey, 2);
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if (!captureCompleted)
                {
                    var wait = SessionState.GetInt(WaitKey, 0) + 1;
                    SessionState.SetInt(WaitKey, wait);
                    if (wait > 12) throw new TimeoutException("Timed out waiting for Harbor screenshot");
                    EditorApplication.QueuePlayerLoopUpdate();
                    UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }
                if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);

                VerifyNavigationPixels();
                BuildArtComparison();
                WriteResult(true,
                    "Captured formal Harbor construction alternate at 1080x2400; shared bottom navigation pixels match v0.43.201 exactly.");
                Finish();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static void SetField(PrototypeBootstrap bootstrap, string fieldName, object value)
        {
            var field = typeof(PrototypeBootstrap).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(PrototypeBootstrap).FullName, fieldName);
            if (value is string) value = Enum.Parse(field.FieldType, (string)value);
            field.SetValue(bootstrap, value);
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
            var spacedNeedle = width + " x " + height;
            for (var index = 0; index < displayTexts.Length; index++)
            {
                var compact = (displayTexts[index] ?? string.Empty).Replace(" ", string.Empty);
                if (compact.IndexOf(compactNeedle, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (displayTexts[index] ?? string.Empty).IndexOf(spacedNeedle,
                        StringComparison.OrdinalIgnoreCase) < 0) continue;
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

        private static void CaptureFrameImmediate()
        {
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                Require(texture != null, "Frame-end Harbor screenshot is null");
                Require(texture.width == HarborFormalArtContract.CanvasWidth &&
                        texture.height == HarborFormalArtContract.CanvasHeight,
                    "Unexpected Harbor screenshot size: " + texture.width + "x" + texture.height);
                Directory.CreateDirectory(OutputDirectory());
                File.WriteAllBytes(Path.Combine(OutputDirectory(), ScreenshotName), texture.EncodeToPNG());
            }
            catch (Exception exception) { captureFailure = exception.GetBaseException().Message; }
            finally
            {
                if (texture != null) UnityEngine.Object.Destroy(texture);
                captureCompleted = true;
            }
        }

        private static void VerifyNavigationPixels()
        {
            var current = LoadTexture(Path.Combine(OutputDirectory(), ScreenshotName));
            var baseline = LoadTexture(Path.Combine(OutputDirectory(), PriorNavigationScreenshot));
            try
            {
                Require(current.width == baseline.width && current.height == baseline.height,
                    "Navigation baseline dimensions differ from Harbor screenshot");
                var currentPixels = current.GetPixels32();
                var baselinePixels = baseline.GetPixels32();
                // The navigation body is fully opaque below visual y=2200. The upper
                // curved transparent edge intentionally reveals each page background.
                var navPixelCount = 200 * HarborFormalArtContract.CanvasWidth;
                for (var index = 0; index < navPixelCount; index++)
                    Require(currentPixels[index].Equals(baselinePixels[index]),
                        "Shared navigation body pixel drift at bottom-canvas pixel " + index);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(current);
                UnityEngine.Object.DestroyImmediate(baseline);
            }
        }

        private static void BuildArtComparison()
        {
            var screenshot = LoadTexture(Path.Combine(OutputDirectory(), ScreenshotName));
            var preview = LoadTexture(Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/meishu/_accepted/ui/port-page-a-bounty-tower-construction-alt-v1.0.0/preview/" +
                "主美-港口页面A赏金塔-铁匠铺灯坊建设中备用版-v1.0.0.png")));
            try
            {
                Require(screenshot.width == preview.width && screenshot.height == preview.height,
                    "Harbor art preview dimensions differ from runtime screenshot");
                WriteComparison(preview, screenshot, false);
                WriteComparison(preview, screenshot, true);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screenshot);
                UnityEngine.Object.DestroyImmediate(preview);
            }
        }

        private static void WriteComparison(Texture2D baseline, Texture2D screenshot, bool difference)
        {
            var baselinePixels = baseline.GetPixels32();
            var screenshotPixels = screenshot.GetPixels32();
            var result = new Color32[baselinePixels.Length];
            for (var index = 0; index < result.Length; index++)
            {
                result[index] = difference
                    ? new Color32(
                        (byte)Math.Abs(baselinePixels[index].r - screenshotPixels[index].r),
                        (byte)Math.Abs(baselinePixels[index].g - screenshotPixels[index].g),
                        (byte)Math.Abs(baselinePixels[index].b - screenshotPixels[index].b), 255)
                    : new Color32(
                        (byte)((baselinePixels[index].r + screenshotPixels[index].r) / 2),
                        (byte)((baselinePixels[index].g + screenshotPixels[index].g) / 2),
                        (byte)((baselinePixels[index].b + screenshotPixels[index].b) / 2), 255);
            }
            var comparison = new Texture2D(baseline.width, baseline.height, TextureFormat.RGBA32, false);
            try
            {
                comparison.SetPixels32(result);
                comparison.Apply(false, false);
                var suffix = difference ? "difference" : "overlay-50pct";
                File.WriteAllBytes(Path.Combine(OutputDirectory(),
                    "SeekerAutoChess-v0.43.238-harbor-construction-alternate-" + suffix +
                    "-1080x2400.png"), comparison.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(comparison); }
        }

        private static Texture2D LoadTexture(string path)
        {
            Require(File.Exists(path), "Missing image: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Require(texture.LoadImage(File.ReadAllBytes(path), false), "Cannot decode image: " + path);
            return texture;
        }

        private static string OutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
        }

        private static void WriteResult(bool passed, string message)
        {
            var directory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/test-results"));
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory,
                "主程-v0.43.238港口建设中备用版-运行时验证-v1.0.json"), JsonUtility.ToJson(new Result
            {
                version = "v0.43.238",
                passed = passed,
                message = message,
                checkedAt = DateTime.UtcNow.ToString("O")
            }, true));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            var commandLine = SessionState.GetBool(CommandLineKey, false);
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetInt(WaitKey, 0);
            SessionState.SetBool(CommandLineKey, false);
            if (EditorApplication.isPlaying)
            {
                EditorApplication.ExitPlaymode();
                if (commandLine) EditorApplication.delayCall += () => EditorApplication.Exit(0);
            }
            else if (commandLine) EditorApplication.Exit(0);
        }

        [Serializable]
        private sealed class Result
        {
            public string version;
            public bool passed;
            public string message;
            public string checkedAt;
        }
    }
}
