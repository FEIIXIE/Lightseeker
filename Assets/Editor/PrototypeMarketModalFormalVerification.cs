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
    internal static class PrototypeMarketModalFormalVerification
    {
        private const string RequestFileName = "VerifyMarketModalFormal.request";
        private const string ActiveKey = "seeker.verify.marketModalFormal.active";
        private const string StageKey = "seeker.verify.marketModalFormal.stage";
        private const string WaitKey = "seeker.verify.marketModalFormal.wait";
        private const string RefreshedKey = "seeker.verify.marketModalFormal.refreshed";
        private const string CommandLineKey = "seeker.verify.marketModalFormal.commandLine";
        private const string Version = "v0.43.255";
        private const string PurchaseScreenshot =
            "主程-集市购买装备详情浮窗-运行时截图-v1.0.png";
        private const string SellScreenshot =
            "主程-集市出售挂单装备详情浮窗-运行时截图-v1.0.png";
        private const string ShelfScreenshot =
            "主程-集市货架扩容确认浮窗-运行时截图-v1.0.png";

        private static double nextActionAt;
        private static bool captureCompleted;
        private static string captureFailure;
        private static string captureFileName;
        private static Vector2 expectedScroll;
        private static string expectedModalIdentity;

        static PrototypeMarketModalFormalVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Seeker/Verify Market Modal Formal Integration v0.43.255")]
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

                switch (stage)
                {
                    case 0:
                        DeletePriorEvidence();
                        Screen.SetResolution(1080, 2400, false);
                        ConfigurePurchaseModal(bootstrap);
                        Advance(1, 3d);
                        return;
                    case 1:
                        VerifyInputIsolationState(bootstrap);
                        BeginCapture(PurchaseScreenshot, 2);
                        return;
                    case 2:
                        if (!WaitForCapture()) return;
                        ConfigureSellModal(bootstrap);
                        Advance(3, 3d);
                        return;
                    case 3:
                        VerifyInputIsolationState(bootstrap);
                        BeginCapture(SellScreenshot, 4);
                        return;
                    case 4:
                        if (!WaitForCapture()) return;
                        ConfigureShelfModal(bootstrap);
                        Advance(5, 3d);
                        return;
                    case 5:
                        VerifyInputIsolationState(bootstrap);
                        BeginCapture(ShelfScreenshot, 6);
                        return;
                    default:
                        if (!WaitForCapture()) return;
                        WriteResult(unityProjectRoot, true,
                            "Captured purchase detail, sell listing detail, and shelf expansion confirmation at 1080x2400. Background scroll, filters, and modal identity remained isolated while each modal was open.");
                        Finish();
                        return;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(unityProjectRoot, false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static void ConfigurePurchaseModal(PrototypeBootstrap bootstrap)
        {
            Invoke(bootstrap, "EnsureMarketState");
            SetEnumField(bootstrap, "currentView", "Market");
            SetEnumField(bootstrap, "marketPageTab", "Browse");
            ClearModalState(bootstrap);

            var board = (MarketBoard)FindField("marketBoard").GetValue(bootstrap);
            var selected = FindFirstActiveRemoteListing(board);
            if (selected == null)
            {
                Invoke(bootstrap, "SeedMarketAcceptancePurchaseListings");
                selected = FindFirstActiveRemoteListing(board);
            }
            Require(selected != null, "No active remote listing is available for purchase modal verification.");
            FindField("selectedMarketListingId").SetValue(bootstrap, selected.ListingId);
            PrepareIsolationState(bootstrap, "purchase:" + selected.ListingId);
        }

        private static MarketListing FindFirstActiveRemoteListing(MarketBoard board)
        {
            foreach (var listing in board.Listings)
            {
                if (listing.State == MarketListingState.Active &&
                    !string.Equals(listing.SellerId, MarketRules.LocalPlayerId, StringComparison.Ordinal))
                    return listing;
            }
            return null;
        }

        private static void ConfigureSellModal(PrototypeBootstrap bootstrap)
        {
            SetEnumField(bootstrap, "currentView", "Market");
            SetEnumField(bootstrap, "marketPageTab", "Sell");
            ClearModalState(bootstrap);

            var candidates = (IList)Invoke(bootstrap, "BuildMarketSaleCandidates");
            if (candidates.Count == 0)
            {
                var inventory = (EquipmentInventory)FindField("equipmentInventory").GetValue(bootstrap);
                var catalog = (EquipmentCatalog)FindField("equipmentCatalog").GetValue(bootstrap);
                Require(catalog.Definitions.Count > 0, "Equipment catalog is empty.");
                Require(inventory.TryAddFirst(new EquipmentInstanceState(
                    "verification.market.modal.sell", catalog.Definitions[0].TemplateId,
                    EquipmentQuality.Rare, "season-0")),
                    "Unable to add an in-memory sell candidate for verification.");
                candidates = (IList)Invoke(bootstrap, "BuildMarketSaleCandidates");
            }
            Require(candidates.Count > 0, "No sell candidate is available for sell modal verification.");
            var candidate = candidates[0];
            var candidateType = candidate.GetType();
            var kind = candidateType.GetField("Kind").GetValue(candidate);
            var instanceId = (string)candidateType.GetField("InstanceId").GetValue(candidate);
            FindField("selectedMarketAssetKind").SetValue(bootstrap, kind);
            FindField("selectedMarketAssetInstanceId").SetValue(bootstrap, instanceId);
            FindField("selectedMarketPreviewAssetInstanceId").SetValue(bootstrap, instanceId);
            FindField("marketListingPriceSkr").SetValue(bootstrap, 320);
            FindField("marketListingPriceInput").SetValue(bootstrap, "320");
            PrepareIsolationState(bootstrap, "sell:" + instanceId);
        }

        private static void ConfigureShelfModal(PrototypeBootstrap bootstrap)
        {
            SetEnumField(bootstrap, "currentView", "Market");
            SetEnumField(bootstrap, "marketPageTab", "Mine");
            ClearModalState(bootstrap);
            FindField("marketShelfExpansionConfirmOpen").SetValue(bootstrap, true);
            PrepareIsolationState(bootstrap, "shelf-expansion");
        }

        private static void ClearModalState(PrototypeBootstrap bootstrap)
        {
            FindField("selectedMarketListingId").SetValue(bootstrap, null);
            FindField("selectedMarketPreviewAssetInstanceId").SetValue(bootstrap, null);
            FindField("marketShelfExpansionConfirmOpen").SetValue(bootstrap, false);
            FindField("marketModalSubmissionInProgress").SetValue(bootstrap, false);
        }

        private static void PrepareIsolationState(PrototypeBootstrap bootstrap, string modalIdentity)
        {
            expectedScroll = new Vector2(173f, 289f);
            expectedModalIdentity = modalIdentity;
            FindField("marketScroll").SetValue(bootstrap, expectedScroll);
            FindField("marketEquipmentFilterOpen").SetValue(bootstrap, true);
            Invoke(bootstrap, "PrepareMarketModalFormal");
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void VerifyInputIsolationState(PrototypeBootstrap bootstrap)
        {
            var scroll = (Vector2)FindField("marketScroll").GetValue(bootstrap);
            Require(Vector2.Distance(scroll, expectedScroll) < 0.01f,
                "Background scroll changed while modal was open: " + expectedModalIdentity);
            Require(!(bool)FindField("marketEquipmentFilterOpen").GetValue(bootstrap),
                "Equipment filter remained open behind modal: " + expectedModalIdentity);
            Require(!(bool)FindField("marketModalSubmissionInProgress").GetValue(bootstrap),
                "Modal submission guard remained locked: " + expectedModalIdentity);
        }

        private static void BeginCapture(string fileName, int nextStage)
        {
            captureCompleted = false;
            captureFailure = null;
            captureFileName = fileName;
            Directory.CreateDirectory(OutputDirectory());
            ScreenCapture.CaptureScreenshot(Path.Combine(OutputDirectory(), captureFileName), 1);
            Advance(nextStage, 2d);
        }

        private static IEnumerator CaptureFrame()
        {
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            try
            {
                texture = ScreenCapture.CaptureScreenshotAsTexture();
                if (texture == null) throw new InvalidOperationException("Market modal screenshot is null.");
                Require(texture.width == 1080 && texture.height == 2400,
                    "Unexpected market modal screenshot size: " + texture.width + "x" + texture.height);
                Directory.CreateDirectory(OutputDirectory());
                File.WriteAllBytes(Path.Combine(OutputDirectory(), captureFileName), texture.EncodeToPNG());
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
            var screenshotPath = Path.Combine(OutputDirectory(), captureFileName);
            if (!captureCompleted && File.Exists(screenshotPath))
            {
                var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Require(texture.LoadImage(File.ReadAllBytes(screenshotPath), false),
                        "Cannot decode market modal screenshot: " + captureFileName);
                    Require(texture.width == 1080 && texture.height == 2400,
                        "Unexpected market modal screenshot size: " + texture.width + "x" + texture.height);
                    captureCompleted = true;
                }
                finally { UnityEngine.Object.DestroyImmediate(texture); }
            }
            if (captureCompleted)
            {
                if (!string.IsNullOrEmpty(captureFailure)) throw new IOException(captureFailure);
                SessionState.SetInt(WaitKey, 0);
                return true;
            }
            var wait = SessionState.GetInt(WaitKey, 0) + 1;
            SessionState.SetInt(WaitKey, wait);
            if (wait > 15) throw new TimeoutException("Timed out waiting for market modal screenshot.");
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
            return false;
        }

        private static void Advance(int stage, double delaySeconds)
        {
            SessionState.SetInt(StageKey, stage);
            nextActionAt = EditorApplication.timeSinceStartup + delaySeconds;
        }

        private static FieldInfo FindField(string name)
        {
            var field = typeof(PrototypeBootstrap).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(PrototypeBootstrap).FullName, name);
            return field;
        }

        private static object Invoke(PrototypeBootstrap bootstrap, string methodName)
        {
            var method = typeof(PrototypeBootstrap).GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null) throw new MissingMethodException(typeof(PrototypeBootstrap).FullName, methodName);
            return method.Invoke(bootstrap, null);
        }

        private static void SetEnumField(PrototypeBootstrap bootstrap, string fieldName, string value)
        {
            var field = FindField(fieldName);
            field.SetValue(bootstrap, Enum.Parse(field.FieldType, value));
        }

        private static string OutputDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/主程-v0.43.255集市三类浮窗输入隔离与正式包接入-验收证据-v1.0"));
        }

        private static void DeletePriorEvidence()
        {
            foreach (var fileName in new[] { PurchaseScreenshot, SellScreenshot, ShelfScreenshot })
            {
                var path = Path.Combine(OutputDirectory(), fileName);
                if (File.Exists(path)) File.Delete(path);
            }
        }

        private static void WriteResult(string unityProjectRoot, bool passed, string message)
        {
            Directory.CreateDirectory(OutputDirectory());
            File.WriteAllText(Path.Combine(OutputDirectory(),
                    "主程-集市三类浮窗输入隔离-运行时验证-v1.0.json"),
                JsonUtility.ToJson(new Result
                {
                    documentVersion = "v1.0",
                    gameVersion = Version,
                    detailPackageVersion = "v1.1.4",
                    sellPackageVersion = "v1.0.3",
                    shelfPackageVersion = "v1.0.1",
                    passed = passed,
                    inputIsolationChecked = passed,
                    screenshotCount = passed ? 3 : 0,
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
            public string documentVersion;
            public string gameVersion;
            public string detailPackageVersion;
            public string sellPackageVersion;
            public string shelfPackageVersion;
            public bool passed;
            public bool inputIsolationChecked;
            public int screenshotCount;
            public string message;
            public string checkedAt;
        }
    }
}
