using System.IO;
using System.Reflection;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    internal static class PrototypeEvidenceCapture
    {
        private const string ArchiveCaptureRequested = "seeker.capture.archive.requested";
        private const string ArchiveCaptureStage = "seeker.capture.archive.stage";
        private static double archiveNextActionAt;

        [InitializeOnLoadMethod]
        private static void ResumeArchiveCapture()
        {
            if (!SessionState.GetBool(ArchiveCaptureRequested, false)) return;
            EditorApplication.update -= UpdateArchiveCapture;
            EditorApplication.update += UpdateArchiveCapture;
            archiveNextActionAt = EditorApplication.timeSinceStartup + 4d;
        }

        [MenuItem("Seeker/Capture UI Trial Screenshot _F8")]
        private static void CaptureUiTrialScreenshot()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("Enter Play Mode before capturing UI trial evidence.");
                return;
            }

            var outputDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory,
                "SeekerAutoChess-v0.43.0-chapter1-economy-1080x2400.png");
            ScreenCapture.CaptureScreenshot(outputPath, 1);
            Debug.Log("UI trial screenshot requested: " + outputPath);
        }

        [MenuItem("Seeker/Capture Serif UI Preview")]
        public static void CaptureArchiveEvidence()
        {
            SessionState.SetBool(ArchiveCaptureRequested, true);
            SessionState.SetInt(ArchiveCaptureStage, 0);
            ResumeArchiveCapture();
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
        }

        private static void UpdateArchiveCapture()
        {
            if (!SessionState.GetBool(ArchiveCaptureRequested, false))
            {
                EditorApplication.update -= UpdateArchiveCapture;
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < archiveNextActionAt) return;

            var bootstrap = Object.FindObjectOfType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                archiveNextActionAt = EditorApplication.timeSinceStartup + 1d;
                return;
            }

            Screen.SetResolution(1080, 2400, false);
            var stage = SessionState.GetInt(ArchiveCaptureStage, 0);
            if (stage == 0)
            {
                ConfigurePreparationShop(bootstrap);
                Capture("SeekerAutoChess-E48-preparation-serif-1080x2400.png");
                SessionState.SetInt(ArchiveCaptureStage, 1);
                archiveNextActionAt = EditorApplication.timeSinceStartup + 3d;
                return;
            }
            if (stage == 1)
            {
                ConfigureUpgradeArchive(bootstrap);
                Capture("SeekerAutoChess-E48-upgrade-serif-1080x2400.png");
                SessionState.SetInt(ArchiveCaptureStage, 2);
                archiveNextActionAt = EditorApplication.timeSinceStartup + 3d;
                return;
            }

            SessionState.SetBool(ArchiveCaptureRequested, false);
            EditorApplication.update -= UpdateArchiveCapture;
            EditorApplication.ExitPlaymode();
            EditorApplication.Exit(0);
        }

        private static void ConfigurePreparationShop(PrototypeBootstrap bootstrap)
        {
            SetExpeditionView(bootstrap);
            var flow = GetField<ExpeditionFlow>(bootstrap, "expeditionFlow");
            flow.Reset();
            SetField(bootstrap, "preparationShopOpen", true);

            var roster = GetField<RunRoster>(bootstrap, "runRoster");
            roster.Clear();
            roster.TryRestoreAt(0, new RunUnitState("unit.guardian", 4, acquisitionBasis: 3));
            roster.TryRestoreAt(1, new RunUnitState("unit.healer", 3, acquisitionBasis: 3));
            roster.TryRestoreAt(2, new RunUnitState("unit.archer", 5, acquisitionBasis: 3));
            roster.TryRestoreAt(roster.BoardCapacity, new RunUnitState("unit.thornGuard", 4, acquisitionBasis: 3));
            roster.TryRestoreAt(roster.BoardCapacity + 1, new RunUnitState("unit.guardian", 4, acquisitionBasis: 3));
        }

        private static void ConfigureUpgradeArchive(PrototypeBootstrap bootstrap)
        {
            SetExpeditionView(bootstrap);
            GetField<ExpeditionFlow>(bootstrap, "expeditionFlow").TryRestore((int)ExpeditionPhase.Upgrade);
            SetField(bootstrap, "preparationShopOpen", false);
        }

        private static void SetExpeditionView(PrototypeBootstrap bootstrap)
        {
            var field = FindField("currentView");
            field.SetValue(bootstrap, System.Enum.Parse(field.FieldType, "Expedition"));
        }

        private static T GetField<T>(PrototypeBootstrap bootstrap, string name)
        {
            return (T)FindField(name).GetValue(bootstrap);
        }

        private static void SetField(PrototypeBootstrap bootstrap, string name, object value)
        {
            FindField(name).SetValue(bootstrap, value);
        }

        private static FieldInfo FindField(string name)
        {
            return typeof(PrototypeBootstrap).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void Capture(string fileName)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, fileName);
            ScreenCapture.CaptureScreenshot(outputPath, 1);
            Debug.Log("Archive UI evidence requested: " + outputPath);
        }
    }
}
