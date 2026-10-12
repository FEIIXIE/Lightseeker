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
    internal static class PrototypeCharacterDetailCRevisionVerification
    {
        private const string RequestFileName = "VerifyCharacterDetailBRevision.request";
        private const string ActiveKey = "seeker.verify.characterDetailCRevision.active";
        private const string StageKey = "seeker.verify.characterDetailCRevision.stage";
        private const string WaitKey = "seeker.verify.characterDetailCRevision.wait";
        private const string RefreshedKey = "seeker.verify.characterDetailCRevision.refreshed";
        private const string CommandLineKey = "seeker.verify.characterDetailCRevision.commandLine";
        private const string Version = "v0.43.369";
        private const string ScreenshotName =
            "SeekerAutoChess-v0.43.369-character-detail-b-1080x2400.png";
        private const string OverlayName =
            "SeekerAutoChess-v0.43.369-character-detail-b-overlay-50pct-1080x2400.png";
        private const string DifferenceName =
            "SeekerAutoChess-v0.43.369-character-detail-b-difference-1080x2400.png";

        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;

        static PrototypeCharacterDetailCRevisionVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Seeker/Verify Character Detail B v1.0.4")]
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
            var unityProjectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(unityProjectRoot, RequestFileName);
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
                if (bootstrap == null)
                {
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }

                if (stage == 0)
                {
                    DeletePriorEvidence();
                    Screen.SetResolution(1080, 2400, false);
                    ConfigureCharacterDetail(bootstrap);
                    SessionState.SetInt(StageKey, 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 3d;
                    return;
                }
                if (stage == 1)
                {
                    captureCompleted = false;
                    captureFailure = null;
                    bootstrap.StartCoroutine(CaptureFrame());
                    SessionState.SetInt(StageKey, 2);
                    nextActionAt = EditorApplication.timeSinceStartup + 1d;
                    return;
                }
                if (!WaitForCapture()) return;

                var difference = BuildComparisonEvidence();
                WriteResult(unityProjectRoot, true,
                    "Captured Character Detail B v1.0.4 at 1080x2400 and generated QA overlay/difference evidence.",
                    difference);
                Finish();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(unityProjectRoot, false, exception.GetBaseException().Message, -1f);
                Finish();
            }
        }

        private static void ConfigureCharacterDetail(PrototypeBootstrap bootstrap)
        {
            var viewField = FindField("currentView");
            viewField.SetValue(bootstrap, Enum.Parse(viewField.FieldType, "Deck"));
            var collection = (PlayerCollection)FindField("playerCollection").GetValue(bootstrap);
            if (collection == null || collection.Entries.Count == 0)
                throw new InvalidOperationException("Character collection is unavailable for verification.");

            var modeField = FindField("deckCharacterDetailMode");
            var mode = Enum.Parse(modeField.FieldType, "ActiveDeckLoadout");
            var openMethod = typeof(PrototypeBootstrap).GetMethod("OpenCharacterDetail",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (openMethod == null)
                throw new MissingMethodException(typeof(PrototypeBootstrap).FullName, "OpenCharacterDetail");
            openMethod.Invoke(bootstrap,
                new object[] { collection.Entries[0].CharacterInstanceId, mode, true });
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

        private static IEnumerator CaptureFrame()
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) throw new InvalidOperationException("Character Detail B screenshot is null.");
                Require(texture.width == 1080 && texture.height == 2400,
                    "Unexpected Character Detail B screenshot size: " + texture.width + "x" + texture.height);
                Directory.CreateDirectory(OutputDirectory());
                File.WriteAllBytes(Path.Combine(OutputDirectory(), ScreenshotName), texture.EncodeToPNG());
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

        private static bool WaitForCapture()
        {
            if (captureCompleted)
            {
                if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);
                return true;
            }
            var wait = SessionState.GetInt(WaitKey, 0) + 1;
            SessionState.SetInt(WaitKey, wait);
            if (wait > 12) throw new TimeoutException("Timed out waiting for Character Detail B screenshot.");
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
            return false;
        }

        private static float BuildComparisonEvidence()
        {
            var screenshot = LoadTexture(Path.Combine(OutputDirectory(), ScreenshotName));
            var baseline = LoadTexture(Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/meishu/_accepted/ui/character-detail-deck-edit-b-v1.0.4/preview/" +
                "主美-卡组编辑角色详情B单一全高框-正式组合QA-v1.0.4.png")));
            try
            {
                Require(baseline.width == 1080 && baseline.height == 2400,
                    "Character Detail B QA baseline must be 1080x2400.");
                Require(baseline.width == screenshot.width && baseline.height == screenshot.height,
                    "Character Detail B QA baseline size differs from screenshot.");
                var baselinePixels = baseline.GetPixels32();
                var screenshotPixels = screenshot.GetPixels32();
                var overlayPixels = new Color32[baselinePixels.Length];
                var differencePixels = new Color32[baselinePixels.Length];
                double differenceTotal = 0d;
                for (var index = 0; index < baselinePixels.Length; index++)
                {
                    var red = Math.Abs(baselinePixels[index].r - screenshotPixels[index].r);
                    var green = Math.Abs(baselinePixels[index].g - screenshotPixels[index].g);
                    var blue = Math.Abs(baselinePixels[index].b - screenshotPixels[index].b);
                    differenceTotal += red + green + blue;
                    overlayPixels[index] = new Color32(
                        (byte)((baselinePixels[index].r + screenshotPixels[index].r) / 2),
                        (byte)((baselinePixels[index].g + screenshotPixels[index].g) / 2),
                        (byte)((baselinePixels[index].b + screenshotPixels[index].b) / 2), 255);
                    differencePixels[index] = new Color32((byte)red, (byte)green, (byte)blue, 255);
                }
                WriteTexture(overlayPixels, screenshot.width, screenshot.height, OverlayName);
                WriteTexture(differencePixels, screenshot.width, screenshot.height, DifferenceName);
                return (float)(differenceTotal / (baselinePixels.Length * 3d * 255d));
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

        private static void WriteTexture(Color32[] pixels, int width, int height, string fileName)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(Path.Combine(OutputDirectory(), fileName), texture.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        private static string OutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
        }

        private static void DeletePriorEvidence()
        {
            foreach (var fileName in new[] { ScreenshotName, OverlayName, DifferenceName })
            {
                var path = Path.Combine(OutputDirectory(), fileName);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void WriteResult(string unityProjectRoot, bool passed, string message, float difference)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(unityProjectRoot, "../../outputs/test-results"));
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory,
                "主程-v0.43.369卡组编辑角色详情B正式美术接入-运行时验证-v1.0.json"),
                JsonUtility.ToJson(new Result
                {
                    version = Version,
                    packageVersion = CharacterDetailBFormalArtContract.PackageVersion,
                    passed = passed,
                    meanNormalizedRgbDifference = difference,
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
            public float meanNormalizedRgbDifference;
            public string message;
            public string checkedAt;
        }
    }
}
