using System.Collections;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    internal static class StaticBattleArtEvidenceCapture
    {
        private const string CaptureRequested = "seeker.capture.static-battle-art.requested";
        private const string CaptureStage = "seeker.capture.static-battle-art.stage";
        private static double nextActionAt;

        private static readonly string[] PlayerDefinitions =
        {
            "unit.guardian", "unit.healer", "unit.thornGuard",
            "unit.archer", "unit.starblade", "unit.starpriest"
        };

        private static readonly string[] NormalDefinitions =
        {
            "enemy.n1.guard", "enemy.n2.fighter", "enemy.n3.ranged",
            "enemy.n4.skirmisher", "enemy.n5.support"
        };

        private static readonly string[] EliteDefinitions =
        {
            "enemy.e1.fortress", "enemy.e2.hunter", "enemy.e3.caster"
        };

        private static readonly string[] BossDefinitions =
        {
            "enemy.b1.heavy", "enemy.b2.hunter"
        };

        [InitializeOnLoadMethod]
        private static void ResumeCapture()
        {
            if (!SessionState.GetBool(CaptureRequested, false)) return;
            EditorApplication.update -= UpdateCapture;
            EditorApplication.update += UpdateCapture;
            nextActionAt = EditorApplication.timeSinceStartup + 4d;
        }

        [MenuItem("Seeker/Capture Static Battle Art Evidence")]
        public static void CaptureEvidence()
        {
            SessionState.SetBool(CaptureRequested, true);
            SessionState.SetInt(CaptureStage, 0);
            ResumeCapture();
            if (!EditorApplication.isPlaying) EditorApplication.EnterPlaymode();
        }

        private static void UpdateCapture()
        {
            if (!SessionState.GetBool(CaptureRequested, false))
            {
                EditorApplication.update -= UpdateCapture;
                return;
            }
            if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < nextActionAt) return;

            var bootstrap = Object.FindObjectOfType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
                return;
            }

            Screen.SetResolution(1080, 2400, false);
            var stage = SessionState.GetInt(CaptureStage, 0);
            switch (stage)
            {
                case 0:
                    ConfigureFormation(bootstrap, PlayerDefinitions, NormalDefinitions, false);
                    Capture("SeekerAutoChess-v0.43.61-static-art-01-six-character-formation-1080x2400.png");
                    break;
                case 1:
                    ConfigureFormation(bootstrap, FirstPlayers(3), NormalDefinitions, true);
                    Capture("SeekerAutoChess-v0.43.61-static-art-02-normal-battle-fixed-front-1080x2400.png");
                    break;
                case 2:
                    ConfigureFormation(bootstrap, FirstPlayers(3), EliteDefinitions, true);
                    Capture("SeekerAutoChess-v0.43.61-static-art-03-elite-battle-fixed-front-1080x2400.png");
                    break;
                case 3:
                    ConfigureFormation(bootstrap, FirstPlayers(3), BossDefinitions, true);
                    Capture("SeekerAutoChess-v0.43.61-static-art-04-boss-battle-fixed-front-1080x2400.png");
                    break;
                case 4:
                    ConfigureFormation(bootstrap, FirstPlayers(1), new[] { "enemy.b2.hunter" }, true);
                    Capture("SeekerAutoChess-v0.43.61-static-art-05-b2-front-view-bounds-1080x2400.png");
                    break;
                default:
                    SessionState.SetBool(CaptureRequested, false);
                    EditorApplication.update -= UpdateCapture;
                    EditorApplication.ExitPlaymode();
                    if (Application.isBatchMode) EditorApplication.Exit(0);
                    return;
            }

            SessionState.SetInt(CaptureStage, stage + 1);
            nextActionAt = EditorApplication.timeSinceStartup + 3d;
        }

        private static string[] FirstPlayers(int count)
        {
            var result = new string[count];
            for (var i = 0; i < count; i++) result[i] = PlayerDefinitions[i];
            return result;
        }

        private static void ConfigureFormation(
            PrototypeBootstrap bootstrap,
            string[] players,
            string[] enemies,
            bool liveBattle)
        {
            SetExpeditionView(bootstrap);
            GetField<ExpeditionFlow>(bootstrap, "expeditionFlow").Reset();
            SetField(bootstrap, "preparationShopOpen", false);
            SetField(bootstrap, "battlePresentationActive", false);
            SetField(bootstrap, "battlePaused", true);

            var roster = GetField<RunRoster>(bootstrap, "runRoster");
            roster.Clear();
            var catalog = CharacterCatalog.CreatePrototype();
            for (var i = 0; i < players.Length; i++)
            {
                var definition = catalog.Get(players[i]);
                roster.TryRestoreAt(i, new RunUnitState(players[i], definition.BasePower, acquisitionBasis: 3));
            }

            var enemyList = (IList)FindField("enemyUnits").GetValue(bootstrap);
            enemyList.Clear();
            var createUnit = typeof(PrototypeBootstrap).GetMethod("CreateUnit",
                BindingFlags.Instance | BindingFlags.NonPublic);
            foreach (var enemyId in enemies)
            {
                var definition = catalog.Get(enemyId);
                enemyList.Add(createUnit.Invoke(bootstrap,
                    new object[] { enemyId, definition.BasePower, Color.white, 1 }));
            }

            if (!liveBattle) return;
            SetField(bootstrap, "battlePresentationActive", true);
            typeof(PrototypeBootstrap).GetMethod("CreateCombatTeams",
                BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bootstrap, null);
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
            return typeof(PrototypeBootstrap).GetField(name,
                BindingFlags.Instance | BindingFlags.NonPublic);
        }

        private static void Capture(string fileName)
        {
            var outputDirectory = Path.GetFullPath(
                Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
            Directory.CreateDirectory(outputDirectory);
            var outputPath = Path.Combine(outputDirectory, fileName);
            ScreenCapture.CaptureScreenshot(outputPath, 1);
            Debug.Log("Static battle art evidence requested: " + outputPath);
        }
    }
}
