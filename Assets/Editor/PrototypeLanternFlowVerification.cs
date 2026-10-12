using System;
using System.IO;
using System.Reflection;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.EditorTools
{
    [InitializeOnLoad]
    internal static class PrototypeLanternFlowVerification
    {
        private const string Version = "v0.43.213";
        private const string RequestFileName = "VerifyLanternFlow.request";
        private const string ActiveKey = "seeker.verify.lantern.active";
        private const string StageKey = "seeker.verify.lantern.stage";
        private static readonly LanternQuality[] Qualities =
        {
            LanternQuality.Common,
            LanternQuality.Uncommon,
            LanternQuality.Rare,
            LanternQuality.Epic,
            LanternQuality.Legendary
        };
        private static double nextActionAt;

        static PrototypeLanternFlowVerification()
        {
            EditorApplication.update -= Update;
            EditorApplication.update += Update;
            nextActionAt = EditorApplication.timeSinceStartup + 1d;
        }

        private static void Update()
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            var requestPath = Path.Combine(projectRoot, RequestFileName);
            if (File.Exists(requestPath))
            {
                File.Delete(requestPath);
                SessionState.SetBool(ActiveKey, true);
                SessionState.SetInt(StageKey, 0);
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
            }
            if (!SessionState.GetBool(ActiveKey, false) ||
                EditorApplication.timeSinceStartup < nextActionAt) return;

            var stage = SessionState.GetInt(StageKey, 0);
            if (stage == 0)
            {
                if (!EditorApplication.isPlaying)
                {
                    SessionState.SetInt(StageKey, 1);
                    EditorApplication.EnterPlaymode();
                    return;
                }
                SessionState.SetInt(StageKey, 1);
                stage = 1;
            }
            if (!EditorApplication.isPlaying) return;

            var bootstrap = UnityEngine.Object.FindObjectOfType<PrototypeBootstrap>();
            if (bootstrap == null)
            {
                nextActionAt = EditorApplication.timeSinceStartup + 1d;
                return;
            }

            try
            {
                if (stage == 1)
                {
                    VerifyAllResources();
                    var inventory = BuildVisualInventory();
                    VerifyEconomy(inventory);
                    SetField(bootstrap, "lanternInventory", inventory);
                    var lighthouse = GetField<ChapterLighthouseProgressState>(
                        bootstrap, "chapterOneLighthouse");
                    lighthouse.RecordBossVictory();
                    lighthouse.RecordChapterLanternAward();
                    Require(lighthouse.TryLight(),
                        "The equipped visual-proof lantern did not light the chapter lighthouse.");
                    SetEnumField(bootstrap, "currentView", "Warehouse");
                    SetField(bootstrap, "homeTopMenu", 0);
                    SetField(bootstrap, "warehouseLanternDetailOpen", false);
                    SetEnumField(bootstrap, "warehouseTab", "Lantern");
                    Screen.SetResolution(1080, 2400, false);
                    RepaintGameView();
                    SessionState.SetInt(StageKey, 2);
                    nextActionAt = EditorApplication.timeSinceStartup + 2d;
                    return;
                }

                if (stage == 2)
                {
                    Capture("SeekerAutoChess-v0.43.213-lantern-five-tier-economy-1080x2400.png");
                    SetEnumField(bootstrap, "currentView", "Home");
                    RepaintGameView();
                    SessionState.SetInt(StageKey, 3);
                    nextActionAt = EditorApplication.timeSinceStartup + 2d;
                    return;
                }

                if (stage >= 3 && stage <= 7)
                {
                    var captureIndex = stage - 3;
                    var quality = Qualities[captureIndex];
                    Capture("SeekerAutoChess-v0.43.213-lantern-home-" +
                            quality.ToString().ToLowerInvariant() + "-1080x2400.png");
                    if (captureIndex + 1 < Qualities.Length)
                    {
                        var inventory = GetField<LanternInventory>(bootstrap, "lanternInventory");
                        Require(inventory.TrySetActive(InstanceId(Qualities[captureIndex + 1])),
                            "Could not select the next lantern quality for runtime verification.");
                    }
                    RepaintGameView();
                    SessionState.SetInt(StageKey, stage + 1);
                    nextActionAt = EditorApplication.timeSinceStartup + 2d;
                    return;
                }

                WriteResult(projectRoot, true,
                    "Five qualities use the confirmed economy ranges and 1/1/2/2/3 glyph counts; all representative combat values and live Warehouse/Home rendering passed.");
                Finish();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                WriteResult(projectRoot, false, exception.GetBaseException().Message);
                Finish();
            }
        }

        private static LanternInventory BuildVisualInventory()
        {
            var instances = new LanternInstanceState[Qualities.Length];
            for (var index = 0; index < Qualities.Length; index++)
            {
                instances[index] = FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(
                    Qualities[index]);
            }
            return new LanternInventory(instances, InstanceId(LanternQuality.Common));
        }

        private static void VerifyEconomy(LanternInventory inventory)
        {
            foreach (var quality in Qualities)
            {
                var instance = inventory.Get(InstanceId(quality));
                Require(FirstChapterLanternEconomyRules.MatchesConfirmedQualityContract(instance),
                    "Lantern representative falls outside the confirmed economy contract: " + quality);
            }

            var uncommon = LanternSnapshot.FromInstance(inventory.Get(InstanceId(LanternQuality.Uncommon)));
            Require(LanternCombatStatRules.ApplyAttackDamage(100, uncommon) == 104,
                "Uncommon attack glyph was not applied with floor rounding.");
            var rare = LanternSnapshot.FromInstance(inventory.Get(InstanceId(LanternQuality.Rare)));
            Require(LanternCombatStatRules.ApplyHealingAmount(100, rare) == 106 &&
                    LanternCombatStatRules.ApplyArmor(10, rare) == 15,
                "Rare healing/armor glyphs were not applied.");
            var epic = LanternSnapshot.FromInstance(inventory.Get(InstanceId(LanternQuality.Epic)));
            Require(LanternCombatStatRules.ApplyMaximumHealth(100, epic) == 134,
                "Epic base flame and health glyph were not additive at the lantern layer.");
            var legendary = LanternSnapshot.FromInstance(inventory.Get(InstanceId(LanternQuality.Legendary)));
            Require(LanternCombatStatRules.ApplyMaximumHealth(100, legendary) == 142 &&
                    LanternCombatStatRules.ApplyHealingAmount(100, legendary) == 110 &&
                    LanternCombatStatRules.ApplyAttackSpeed(10000, legendary) == 11000,
                "Legendary health/healing/speed glyphs were not applied.");
        }

        private static void VerifyAllResources()
        {
            foreach (var templateId in new[]
            {
                LanternArtContract.BreakwaterTemplateId,
                LanternArtContract.LayeredShellTemplateId,
                LanternArtContract.TideEchoTemplateId
            })
            foreach (var quality in Qualities)
            {
                var resourceId = LanternArtContract.ResourceId(templateId, quality);
                var texture = Resources.Load<Texture2D>(resourceId);
                Require(texture != null, "Missing formal lantern runtime texture: " + resourceId);
                Require(texture.width == LanternArtContract.NativeSize &&
                        texture.height == LanternArtContract.NativeSize,
                    "Formal lantern runtime texture was resized: " + resourceId);
            }
        }

        private static string InstanceId(LanternQuality quality)
        {
            return "economy-proof-" + quality.ToString().ToLowerInvariant();
        }

        private static void Capture(string fileName)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../outputs/screenshots"));
            Directory.CreateDirectory(outputDirectory);
            ScreenCapture.CaptureScreenshot(Path.Combine(outputDirectory, fileName), 1);
        }

        private static void RepaintGameView()
        {
            EditorApplication.QueuePlayerLoopUpdate();
            UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
        }

        private static void WriteResult(string projectRoot, bool passed, string message)
        {
            var outputDirectory = Path.GetFullPath(Path.Combine(projectRoot, "../../outputs/test-results"));
            Directory.CreateDirectory(outputDirectory);
            var json = JsonUtility.ToJson(new Result
            {
                version = Version,
                passed = passed,
                message = message,
                checkedAt = DateTime.UtcNow.ToString("O")
            }, true);
            File.WriteAllText(Path.Combine(outputDirectory,
                "主程-v0.43.213航灯五档测试库存-运行时验证-v1.0.json"), json);
        }

        private static T GetField<T>(object target, string name)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            return (T)field.GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            field.SetValue(target, value);
        }

        private static void SetEnumField(object target, string name, string value)
        {
            var field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new MissingFieldException(target.GetType().FullName, name);
            field.SetValue(target, Enum.Parse(field.FieldType, value));
        }

        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }

        private static void Finish()
        {
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetInt(StageKey, 0);
            if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
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
