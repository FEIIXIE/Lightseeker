using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    public static class UiMainClickPlayModeInspectionRunner
    {
        private const string RequestFileName = "RunUiMainClickPlayModeInspection.request";
        private const string ActiveKey = "seeker.uiClickPlayMode.active";
        private const string StageKey = "seeker.uiClickPlayMode.stage";
        private const string StartedPlayModeKey = "seeker.uiClickPlayMode.startedPlayMode";
        private const string FailureKey = "seeker.uiClickPlayMode.failure";
        private const string ErrorCountKey = "seeker.uiClickPlayMode.errorCount";
        private const string LogFileName = "ui-main-click-playmode-v0.43.124.log";
        private static readonly List<string> runtimeErrors = new List<string>();
        private static double nextActionAt;

        static UiMainClickPlayModeInspectionRunner()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            Application.logMessageReceived -= CaptureRuntimeLog;
            Application.logMessageReceived += CaptureRuntimeLog;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        [MenuItem("Seeker/Run UI Main Click PlayMode Inspection")]
        public static void RunFromMenu()
        {
            Begin();
        }

        private static void Update()
        {
            var requestPath = Path.Combine(ProjectRoot(), RequestFileName);
            if (File.Exists(requestPath) && !SessionState.GetBool(ActiveKey, false))
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
                File.Delete(requestPath);
                Begin();
                return;
            }

            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating ||
                EditorApplication.timeSinceStartup < nextActionAt) return;

            if (!EditorApplication.isPlaying)
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    SessionState.SetBool(StartedPlayModeKey, true);
                    EditorApplication.EnterPlaymode();
                }
                nextActionAt = EditorApplication.timeSinceStartup + 2d;
                return;
            }

            var bootstrap = UnityEngine.Object.FindObjectOfType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
                return;
            }

            try
            {
                RunStage(bootstrap, SessionState.GetInt(StageKey, 0));
            }
            catch (Exception exception)
            {
                Fail("runner-exception=" + exception.GetBaseException().Message);
                Append("stack=" + exception);
                Finish();
            }
        }

        private static void Begin()
        {
            Directory.CreateDirectory(ResultDirectory());
            Directory.CreateDirectory(EvidenceDirectory());
            ClearConsole();
            runtimeErrors.Clear();
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetInt(StageKey, 0);
            SessionState.SetBool(StartedPlayModeKey, false);
            SessionState.SetString(FailureKey, string.Empty);
            SessionState.SetInt(ErrorCountKey, 0);
            File.WriteAllText(LogPath(),
                "UI MAIN CLICK PLAYMODE INSPECTION\n" +
                "timestamp=" + DateTimeOffset.Now.ToString("O") + "\n" +
                "unityVersion=" + Application.unityVersion + "\n" +
                "projectPath=" + ProjectRoot() + "\n", Encoding.UTF8);
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        private static void RunStage(PrototypeBootstrap bootstrap, int stage)
        {
            Screen.SetResolution(1080, 2400, false);
            switch (stage)
            {
                case 0:
                    ConfigureHome(bootstrap);
                    CheckRuntimeShape(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.Confirm, "home-expedition-entry", "home-entry");
                    Capture("01-home-entry.png");
                    Advance(stage, 2d);
                    return;
                case 1:
                    ConfigureShop(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.Cancel, "page-back", "return");
                    CheckCue(bootstrap, UiAudioCueIds.TabSwitch, "warehouse-tab-All", "tab-switch");
                    CheckCue(bootstrap, UiAudioCueIds.Tap, "recruit-shop-refresh", "shop-refresh");
                    CheckCue(bootstrap, UiAudioCueIds.Purchase, "recruit-shop-buy", "shop-purchase");
                    CheckCue(bootstrap, UiAudioCueIds.PanelClose, "recruit-shop-close", "shop-close");
                    Capture("02-recruit-shop.png");
                    Advance(stage, 2d);
                    return;
                case 2:
                    ConfigurePreparation(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.Purchase,
                        "recruit-shop-buy-delayed-repeat", "shop-purchase-delayed-repeat");
                    CheckDragBoundaries(bootstrap);
                    Capture("03-preparation-drag.png");
                    Advance(stage, 2d);
                    return;
                case 3:
                    ConfigurePreparation(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.BattleStart, "preparation-start-battle", "auto-battle");
                    Capture("04-auto-battle-entry.png");
                    Advance(stage, 2d);
                    return;
                case 4:
                    ConfigureUpgrade(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.Upgrade, "upgrade-choice", "post-battle-upgrade");
                    Capture("05-post-battle-upgrade.png");
                    Advance(stage, 2d);
                    return;
                case 5:
                    ConfigureMap(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.Confirm, "map-node-playmode", "map-valid-node");
                    CheckRejected(bootstrap, UiAudioCueIds.Confirm, "map-node-unavailable", "map-invalid-node");
                    Capture("06-map-node.png");
                    Advance(stage, 2d);
                    return;
                case 6:
                    ConfigureDiagnostics(bootstrap);
                    CheckMuteVolumeAndRateLimit(bootstrap);
                    Capture("07-audio-diagnostics.png");
                    Advance(stage, 2d);
                    return;
                case 7:
                    ConfigureHome(bootstrap);
                    CheckCue(bootstrap, UiAudioCueIds.Confirm, "home-expedition-entry-repeat", "home-entry-repeat");
                    CheckRuntimeShape(bootstrap);
                    Capture("08-home-return.png");
                    Advance(stage, 2d);
                    return;
                default:
                    Finish();
                    return;
            }
        }

        private static void ConfigureHome(PrototypeBootstrap bootstrap)
        {
            SetEnumField(bootstrap, "currentView", "Home");
            SetField(bootstrap, "battlePresentationActive", false);
            SetField(bootstrap, "preparationShopOpen", false);
            SetField(bootstrap, "audioDiagnosticsOpen", false);
        }

        private static void ConfigureShop(PrototypeBootstrap bootstrap)
        {
            SetEnumField(bootstrap, "currentView", "Expedition");
            GetField<ExpeditionFlow>(bootstrap, "expeditionFlow").Reset();
            SetField(bootstrap, "battlePresentationActive", false);
            SetField(bootstrap, "preparationShopOpen", true);
            SetField(bootstrap, "preparationBackpackExpanded", false);
            SetField(bootstrap, "audioDiagnosticsOpen", false);
        }

        private static void ConfigurePreparation(PrototypeBootstrap bootstrap)
        {
            ConfigureShop(bootstrap);
            SetField(bootstrap, "preparationShopOpen", false);
        }

        private static void ConfigureUpgrade(PrototypeBootstrap bootstrap)
        {
            ConfigurePreparation(bootstrap);
            GetField<ExpeditionFlow>(bootstrap, "expeditionFlow").TryRestore((int)ExpeditionPhase.Upgrade);
        }

        private static void ConfigureMap(PrototypeBootstrap bootstrap)
        {
            ConfigurePreparation(bootstrap);
            GetField<ExpeditionFlow>(bootstrap, "expeditionFlow").TryRestore((int)ExpeditionPhase.Route);
        }

        private static void ConfigureDiagnostics(PrototypeBootstrap bootstrap)
        {
            ConfigureShop(bootstrap);
            SetField(bootstrap, "audioDiagnosticsOpen", true);
        }

        private static void CheckRuntimeShape(PrototypeBootstrap bootstrap)
        {
            var players = UnityEngine.Object.FindObjectsOfType<UiMainClickAudioPlayer>();
            Record("single-player-object", players.Length == 1, "count=" + players.Length);
            if (players.Length != 1) return;
            var sources = players[0].GetComponents<AudioSource>();
            var valid = sources.Length == 1 && !sources[0].playOnAwake && !sources[0].loop &&
                        Mathf.Approximately(sources[0].spatialBlend, 0f);
            Record("single-2d-audio-source", valid,
                "count=" + sources.Length + " playOnAwake=" + (sources.Length > 0 && sources[0].playOnAwake) +
                " loop=" + (sources.Length > 0 && sources[0].loop) +
                " spatialBlend=" + (sources.Length > 0 ? sources[0].spatialBlend.ToString("F2") : "missing"));
        }

        private static void CheckCue(PrototypeBootstrap bootstrap, string cueId, string interactionId, string label)
        {
            var router = GetField<UiAudioEventRouter>(bootstrap, "uiAudioRouter");
            var player = GetField<UiMainClickAudioPlayer>(bootstrap, "uiMainClickAudioPlayer");
            router.SetMuted(false);
            router.Mixer.SetGroup("master", 1f);
            router.Mixer.SetGroup("ui", 1f);
            router.ResetCooldowns();
            var before = player.SuccessfulPlayCount;
            var result = InvokePlayUiClick(bootstrap, cueId, interactionId);
            Record(label, result && player.SuccessfulPlayCount == before + 1,
                "cue=" + cueId + " before=" + before + " after=" + player.SuccessfulPlayCount +
                " last=" + player.LastCueId + " resource=" + player.LastResourcePath);
        }

        private static void CheckRejected(PrototypeBootstrap bootstrap, string cueId, string interactionId, string label)
        {
            var router = GetField<UiAudioEventRouter>(bootstrap, "uiAudioRouter");
            var player = GetField<UiMainClickAudioPlayer>(bootstrap, "uiMainClickAudioPlayer");
            router.ResetCooldowns();
            var before = player.SuccessfulPlayCount;
            var result = player.TryPlay(cueId, interactionId, 900000 + before, false);
            Record(label, !result && player.SuccessfulPlayCount == before,
                "controlEnabled=false before=" + before + " after=" + player.SuccessfulPlayCount);
        }

        private static void CheckDragBoundaries(PrototypeBootstrap bootstrap)
        {
            CheckCue(bootstrap, UiAudioCueIds.DragPick, "formation-drag-pick", "legal-drag-pick-cue-runtime");
            var player = GetField<UiMainClickAudioPlayer>(bootstrap, "uiMainClickAudioPlayer");
            Record("drag-pick-stone-resource",
                player.LastResourcePath == UiMainClickAudioPolicy.FormationDropResourcePath,
                "resource=" + player.LastResourcePath);
            CheckCue(bootstrap, UiAudioCueIds.DragDrop, "formation-valid-drop", "legal-drag-cue-runtime");
            Record("drag-drop-stone-resource",
                player.LastResourcePath == UiMainClickAudioPolicy.FormationDropResourcePath,
                "resource=" + player.LastResourcePath);
            CheckRejected(bootstrap, UiAudioCueIds.DragDrop, "formation-invalid-drop", "invalid-drag-silent");
            var before = player.SuccessfulPlayCount;
            Record("blank-hover-silent", player.SuccessfulPlayCount == before,
                "before=" + before + " after=" + player.SuccessfulPlayCount);

            var source = File.ReadAllText(Path.Combine(Application.dataPath, "Scripts/PrototypeBootstrap.cs"));
            var hasPickHook = source.Contains("PlayUiClick(UiAudioCueIds.DragPick");
            var hasDropHook = source.Contains("PlayUiClick(UiAudioCueIds.DragDrop");
            Record("legal-drag-gameplay-hook", hasPickHook || hasDropHook,
                "dragPick=" + hasPickHook + " dragDrop=" + hasDropHook);
        }

        private static void CheckMuteVolumeAndRateLimit(PrototypeBootstrap bootstrap)
        {
            var router = GetField<UiAudioEventRouter>(bootstrap, "uiAudioRouter");
            var player = GetField<UiMainClickAudioPlayer>(bootstrap, "uiMainClickAudioPlayer");
            router.ResetCooldowns();
            router.SetMuted(true);
            var beforeMute = player.SuccessfulPlayCount;
            var mutedResult = InvokePlayUiClick(bootstrap, UiAudioCueIds.Tap, "diagnostic-muted");
            Record("mute-silent", !mutedResult && player.SuccessfulPlayCount == beforeMute,
                "before=" + beforeMute + " after=" + player.SuccessfulPlayCount);

            router.SetMuted(false);
            router.ResetCooldowns();
            var recoveredResult = InvokePlayUiClick(bootstrap, UiAudioCueIds.Tap, "diagnostic-unmuted");
            Record("mute-recovery", recoveredResult && player.SuccessfulPlayCount == beforeMute + 1,
                "after=" + player.SuccessfulPlayCount);

            router.Mixer.SetGroup("ui", 0f);
            router.ResetCooldowns();
            var beforeZero = player.SuccessfulPlayCount;
            var zeroResult = InvokePlayUiClick(bootstrap, UiAudioCueIds.Tap, "diagnostic-ui-zero");
            Record("ui-volume-zero-silent", !zeroResult && player.SuccessfulPlayCount == beforeZero,
                "before=" + beforeZero + " after=" + player.SuccessfulPlayCount);
            router.Mixer.SetGroup("ui", 1f);
            router.ResetCooldowns();
            var volumeRecovered = InvokePlayUiClick(bootstrap, UiAudioCueIds.Tap, "diagnostic-ui-restored");
            Record("ui-volume-recovery", volumeRecovered && player.SuccessfulPlayCount == beforeZero + 1,
                "after=" + player.SuccessfulPlayCount);

            router.ResetCooldowns();
            var rateBefore = player.SuccessfulPlayCount;
            var first = player.TryPlay(UiAudioCueIds.Tap, "rate-first", 910000);
            var second = player.TryPlay(UiAudioCueIds.Tap, "rate-second", 910001);
            var third = player.TryPlay(UiAudioCueIds.Tap, "rate-third", 910002);
            Record("rate-limit", first && !second && third && player.SuccessfulPlayCount == rateBefore + 2,
                "results=" + first + "," + second + "," + third +
                " before=" + rateBefore + " after=" + player.SuccessfulPlayCount);
        }

        private static bool InvokePlayUiClick(PrototypeBootstrap bootstrap, string cueId, string interactionId)
        {
            var method = typeof(PrototypeBootstrap).GetMethod("PlayUiClick",
                BindingFlags.Instance | BindingFlags.NonPublic);
            return method != null && (bool)method.Invoke(bootstrap, new object[] { cueId, interactionId });
        }

        private static void Record(string name, bool passed, string detail)
        {
            Append("check=" + name + " result=" + (passed ? "PASS" : "FAIL") + " " + detail);
            if (!passed) Fail(name);
        }

        private static void Fail(string reason)
        {
            var current = SessionState.GetString(FailureKey, string.Empty);
            SessionState.SetString(FailureKey,
                string.IsNullOrEmpty(current) ? reason : current + "," + reason);
        }

        private static void Advance(int stage, double delay)
        {
            SessionState.SetInt(StageKey, stage + 1);
            nextActionAt = EditorApplication.timeSinceStartup + delay;
        }

        private static void Finish()
        {
            var failures = SessionState.GetString(FailureKey, string.Empty);
            var errorCount = SessionState.GetInt(ErrorCountKey, 0);
            Append("consoleErrorCount=" + errorCount);
            Append("runtimeErrorCount=" + runtimeErrors.Count);
            Append("conclusion=" + (string.IsNullOrEmpty(failures) && errorCount == 0
                ? "PLAYMODE_PASSED" : "PLAYMODE_BLOCKED"));
            Append("failures=" + (string.IsNullOrEmpty(failures) ? "none" : failures));
            Append("finishedAt=" + DateTimeOffset.Now.ToString("O"));
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(StageKey, 0);
            if (SessionState.GetBool(StartedPlayModeKey, false)) EditorApplication.ExitPlaymode();
        }

        private static void CaptureRuntimeLog(string condition, string stackTrace, LogType type)
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
            runtimeErrors.Add(type + ": " + condition);
            SessionState.SetInt(ErrorCountKey, SessionState.GetInt(ErrorCountKey, 0) + 1);
            Append("console=" + type + " message=" + condition.Replace(Environment.NewLine, " | "));
        }

        private static void Capture(string fileName)
        {
            var path = Path.Combine(EvidenceDirectory(), fileName);
            ScreenCapture.CaptureScreenshot(path, 1);
            Append("screenshot=" + path);
        }

        private static void ClearConsole()
        {
            var type = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.LogEntries");
            var method = type == null ? null : type.GetMethod("Clear", BindingFlags.Static | BindingFlags.Public);
            if (method != null) method.Invoke(null, null);
        }

        private static T GetField<T>(PrototypeBootstrap bootstrap, string name)
        {
            return (T)FindField(name).GetValue(bootstrap);
        }

        private static void SetField(PrototypeBootstrap bootstrap, string name, object value)
        {
            FindField(name).SetValue(bootstrap, value);
        }

        private static void SetEnumField(PrototypeBootstrap bootstrap, string name, string value)
        {
            var field = FindField(name);
            field.SetValue(bootstrap, Enum.Parse(field.FieldType, value));
        }

        private static FieldInfo FindField(string name)
        {
            var field = typeof(PrototypeBootstrap).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(typeof(PrototypeBootstrap).FullName, name);
            return field;
        }

        private static void Append(string line)
        {
            Directory.CreateDirectory(ResultDirectory());
            File.AppendAllText(LogPath(), line + Environment.NewLine, Encoding.UTF8);
        }

        private static string ProjectRoot()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        private static string ResultDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/test-results"));
        }

        private static string EvidenceDirectory()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath,
                "../../../outputs/evidence/ui-main-click-playmode-v0.43.124"));
        }

        private static string LogPath()
        {
            return Path.Combine(ResultDirectory(), LogFileName);
        }
    }
}
