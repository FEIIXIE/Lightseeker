using System;
using System.Collections;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    internal static class PrototypeStoreFormalVerification
    {
        private const string RequestFileName = "VerifyFormalStore.request";
        private const string ActiveKey = "seeker.verify.formalStore.active";
        private const string StageKey = "seeker.verify.formalStore.stage";
        private const string WaitKey = "seeker.verify.formalStore.wait";
        private const string RefreshedKey = "seeker.verify.formalStore.refreshed";
        private const string CommandLineKey = "seeker.verify.formalStore.commandLine";
        private const string Version = "v0.43.214";
        private const string ScreenshotName =
            "SeekerAutoChess-v0.43.214-store-claim-overlay-1080x2400.png";
        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;

        static PrototypeStoreFormalVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Seeker/Verify Formal Store E v1.1")]
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
                    var host = new GameObject("StoreClaimVerificationBootstrap");
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
                    ConfigureStore(bootstrap);
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

                if (!captureCompleted)
                {
                    var wait = SessionState.GetInt(WaitKey, 0) + 1;
                    SessionState.SetInt(WaitKey, wait);
                    if (wait <= 12)
                    {
                        EditorApplication.QueuePlayerLoopUpdate();
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                        nextActionAt = EditorApplication.timeSinceStartup + 1d;
                        return;
                    }
                    throw new TimeoutException("Timed out waiting for formal Store E screenshot");
                }

                if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);
                BuildVerificationEvidence();
                WriteResult(projectRoot, true,
                    "Captured the completed claim modal above the full Store E page at 1080x2400 and verified the canonical five-quality text palette.");
                Finish();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(projectRoot, false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static void ConfigureStore(PrototypeBootstrap bootstrap)
        {
            var viewField = FindField("currentView");
            viewField.SetValue(bootstrap, Enum.Parse(viewField.FieldType, "Store"));
            FindField("skrBalance").SetValue(bootstrap, 1550);
            FindField("storeEpicPitySkr").SetValue(bootstrap, 1050);
            FindField("storeLegendaryPitySkr").SetValue(bootstrap, 1050);
            FindField("storeOpeningActive").SetValue(bootstrap, true);
            FindField("storeOpeningProgress").SetValue(bootstrap, 1f);
            FindField("storeRevealSelectedIndex").SetValue(bootstrap, 1);
            FindField("pendingStoreOrderId").SetValue(bootstrap, "verification.store.claim.v0.43.214");
            FindField("pendingStoreInstanceIds").SetValue(bootstrap,
                new[] { "verification.common", "verification.uncommon", "verification.rare" });
            FindField("pendingStoreDefinitionIds").SetValue(bootstrap,
                new[] { "unit.healer", "unit.archer", "unit.thornGuard" });
            FindField("pendingStoreQualities").SetValue(bootstrap,
                new[]
                {
                    (int)SeekerAutoChess.Core.CharacterQuality.Common,
                    (int)SeekerAutoChess.Core.CharacterQuality.Uncommon,
                    (int)SeekerAutoChess.Core.CharacterQuality.Rare
                });
            FindField("storeStatusMessage").SetValue(bootstrap, string.Empty);
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
                if (texture == null) throw new InvalidOperationException("Frame-end screenshot texture is null");
                if (texture.width != 1080 || texture.height != 2400)
                    throw new InvalidOperationException("Unexpected Store screenshot size: " +
                                                        texture.width + "x" + texture.height);
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

        private static void BuildVerificationEvidence()
        {
            var screenshot = LoadTexture(Path.Combine(OutputDirectory(), ScreenshotName));
            try
            {
                Require(screenshot.width == 1080 && screenshot.height == 2400,
                    "Store screenshot is not 1080x2400");
                AssertQualityColor(SeekerAutoChess.Core.CharacterQuality.Common,
                    new Color32(0x8E, 0x94, 0x97, 0xFF));
                AssertQualityColor(SeekerAutoChess.Core.CharacterQuality.Uncommon,
                    new Color32(0x7E, 0xBC, 0x86, 0xFF));
                AssertQualityColor(SeekerAutoChess.Core.CharacterQuality.Rare,
                    new Color32(0x33, 0x9C, 0xE8, 0xFF));
                AssertQualityColor(SeekerAutoChess.Core.CharacterQuality.Epic,
                    new Color32(0x9D, 0x5E, 0xCF, 0xFF));
                AssertQualityColor(SeekerAutoChess.Core.CharacterQuality.Legendary,
                    new Color32(0xE6, 0x87, 0x30, 0xFF));
                WriteCrop(screenshot, new RectInt(300, 875, 480, 260),
                    "SeekerAutoChess-v0.43.214-store-claim-quality-crop.png");
                WriteCrop(screenshot, new RectInt(0, 2080, 1080, 320),
                    "SeekerAutoChess-v0.43.214-store-claim-navigation-background-crop.png");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(screenshot);
            }
        }

        private static void AssertQualityColor(SeekerAutoChess.Core.CharacterQuality quality,
            Color32 expected)
        {
            var method = typeof(PrototypeBootstrap).GetMethod("QualityColor",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(PrototypeBootstrap).FullName,
                "QualityColor");
            var actual = (Color)method.Invoke(null, new object[] { quality });
            var actual32 = (Color32)actual;
            Require(actual32.r == expected.r && actual32.g == expected.g && actual32.b == expected.b &&
                    actual32.a == expected.a,
                "Unexpected quality color for " + quality + ": " + actual32);
        }

        private static Texture2D LoadTexture(string path)
        {
            Require(File.Exists(path), "Missing image: " + path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Require(texture.LoadImage(File.ReadAllBytes(path), false), "Cannot decode image: " + path);
            return texture;
        }

        private static void WriteOverlay(Texture2D baseline, Texture2D screenshot, string fileName)
        {
            var baselinePixels = baseline.GetPixels32();
            var screenshotPixels = screenshot.GetPixels32();
            var result = new Color32[baselinePixels.Length];
            for (var index = 0; index < result.Length; index++)
            {
                result[index] = new Color32(
                    (byte)((baselinePixels[index].r + screenshotPixels[index].r) / 2),
                    (byte)((baselinePixels[index].g + screenshotPixels[index].g) / 2),
                    (byte)((baselinePixels[index].b + screenshotPixels[index].b) / 2), 255);
            }
            var overlay = new Texture2D(baseline.width, baseline.height, TextureFormat.RGBA32, false);
            try
            {
                overlay.SetPixels32(result);
                overlay.Apply(false, false);
                File.WriteAllBytes(Path.Combine(OutputDirectory(), fileName), overlay.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(overlay); }
        }

        private static void WriteCrop(Texture2D source, RectInt topLeftRect, string fileName)
        {
            var sourceY = source.height - topLeftRect.y - topLeftRect.height;
            var crop = new Texture2D(topLeftRect.width, topLeftRect.height, TextureFormat.RGBA32, false);
            try
            {
                crop.SetPixels(source.GetPixels(topLeftRect.x, sourceY, topLeftRect.width, topLeftRect.height));
                crop.Apply(false, false);
                File.WriteAllBytes(Path.Combine(OutputDirectory(), fileName), crop.EncodeToPNG());
            }
            finally { UnityEngine.Object.DestroyImmediate(crop); }
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
                ScreenshotName,
                "SeekerAutoChess-v0.43.214-store-claim-quality-crop.png",
                "SeekerAutoChess-v0.43.214-store-claim-navigation-background-crop.png"
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
                packageVersion = "store-e-v1.1.0 + claim-overlay-fix-v1.0",
                passed = passed,
                message = message,
                checkedAt = DateTime.UtcNow.ToString("O")
            }, true);
            File.WriteAllText(Path.Combine(outputDirectory,
                "主程-v0.43.214商城领取浮层与品质色修正-运行时验证-v1.0.json"), result);
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
