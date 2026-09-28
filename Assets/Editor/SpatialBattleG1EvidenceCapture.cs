using System;
using System.Collections.Generic;
using System.IO;
using SeekerAutoChess.Core;
using UnityEditor;
using UnityEngine;

namespace SeekerAutoChess.Prototype.EditorTools
{
    internal static class SpatialBattleG1EvidenceCapture
    {
        private const string EvidenceFileName = "G1-spatial-graybox-unity-v0.43.49.png";

        [InitializeOnLoadMethod]
        private static void QueueCaptureIfMissing()
        {
            var path = EvidencePath();
            if (File.Exists(path)) return;
            EditorApplication.delayCall += () =>
            {
                // The evidence scene is rendered entirely off-screen with HideAndDontSave
                // objects, so it is safe to capture while the prototype is in Play Mode.
                // Keeping this available in Play Mode is important for long-running QA
                // sessions where the editor should not be stopped just to collect evidence.
                if (File.Exists(path)) return;
                try
                {
                    Capture(path);
                    UnityEngine.Debug.Log("G1 Unity graybox evidence captured: " + path);
                }
                catch (Exception exception)
                {
                    UnityEngine.Debug.LogException(exception);
                }
            };
        }

        [MenuItem("Seeker Auto Chess/QA/Capture G1 Spatial Graybox Evidence")]
        private static void CaptureFromMenu()
        {
            Capture(EvidencePath());
            AssetDatabase.Refresh();
        }

        public static void CaptureFromCommandLine()
        {
            Capture(EvidencePath());
            UnityEngine.Debug.Log("G1 Unity graybox evidence captured: " + EvidencePath());
        }

        private static void Capture(string outputPath)
        {
            var input = SpatialBattleSimulator.CreateG1Baseline();
            var result = SpatialBattleSimulator.Run(input);
            var views = ReplayAtTick(input, result, 120);
            var created = new List<UnityEngine.Object>();
            var root = new GameObject("G1SpatialEvidenceRoot") { hideFlags = HideFlags.HideAndDontSave };
            created.Add(root);
            var cameraObject = new GameObject("G1SpatialEvidenceCamera") { hideFlags = HideFlags.HideAndDontSave };
            created.Add(cameraObject);
            var camera = cameraObject.AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 0f, -20f);
            camera.orthographic = true;
            camera.orthographicSize = 9.6f;
            camera.aspect = 1080f / 1920f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.09f, 0.12f);

            DrawBackground(root.transform, created);
            DrawGrid(root.transform, created);
            DrawTitle(root.transform, created, result);
            DrawTargetLines(root.transform, created, views);
            DrawUnits(root.transform, created, views);

            var target = new RenderTexture(1080, 1920, 24, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            created.Add(target);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var image = new Texture2D(1080, 1920, TextureFormat.RGB24, false)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            created.Add(image);
            image.ReadPixels(new Rect(0, 0, 1080, 1920), 0, 0);
            image.Apply();
            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            for (var index = created.Count - 1; index >= 0; index--)
                UnityEngine.Object.DestroyImmediate(created[index]);
        }

        private static Dictionary<string, EvidenceUnit> ReplayAtTick(
            SpatialBattleInput input, SpatialBattleResult result, int tick)
        {
            var views = new Dictionary<string, EvidenceUnit>(StringComparer.Ordinal);
            AddViews(views, input.PlayerUnits);
            AddViews(views, input.EnemyUnits);
            foreach (var battleEvent in result.Events)
            {
                if (battleEvent.Tick > tick) break;
                EvidenceUnit actor;
                EvidenceUnit target;
                views.TryGetValue(battleEvent.ActorId, out actor);
                views.TryGetValue(battleEvent.TargetId, out target);
                if (battleEvent.Type == SpatialBattleEventType.TargetAcquired && actor != null)
                    actor.TargetId = battleEvent.TargetId;
                else if (battleEvent.Type == SpatialBattleEventType.StateChanged && actor != null)
                    actor.State = battleEvent.State;
                else if (battleEvent.Type == SpatialBattleEventType.Moved && actor != null)
                    actor.Position = battleEvent.Position;
                else if (battleEvent.Type == SpatialBattleEventType.Damaged && target != null)
                    target.Health = battleEvent.TargetHealthAfter;
                else if (battleEvent.Type == SpatialBattleEventType.Defeated && actor != null)
                {
                    actor.Health = 0;
                    actor.State = SpatialActionState.Defeated;
                    actor.TargetId = string.Empty;
                }
            }
            return views;
        }

        private static void AddViews(
            IDictionary<string, EvidenceUnit> destination,
            IEnumerable<SpatialBattleUnitInput> source)
        {
            foreach (var unit in source)
                destination.Add(unit.BattleUnitId, new EvidenceUnit(unit));
        }

        private static void DrawBackground(Transform parent, ICollection<UnityEngine.Object> created)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Resources/Art/Battle/ART-BATTLE-CHAPTER-01-ABANDONED-SIGNAL-YARD-READABILITY-02.png");
            if (sprite == null) return;
            var renderer = NewSprite("ExistingBattlefield", parent, sprite, created);
            renderer.transform.position = new Vector3(0f, 0f, 8f);
            var bounds = sprite.bounds.size;
            renderer.transform.localScale = new Vector3(10.8f / bounds.x, 19.2f / bounds.y, 1f);
            renderer.color = new Color(0.62f, 0.72f, 0.75f, 0.62f);
        }

        private static void DrawGrid(Transform parent, ICollection<UnityEngine.Object> created)
        {
            for (var column = 0; column <= 5; column++)
                DrawLine("GridV", parent, created, new Vector2(-5f + column * 2f, -7.4f),
                    new Vector2(-5f + column * 2f, 7.4f), new Color(0.25f, 0.82f, 0.86f, 0.28f), 0.025f, 4f);
            for (var row = 0; row <= 8; row++)
                DrawLine("GridH", parent, created, new Vector2(-5f, -7.4f + row * 1.85f),
                    new Vector2(5f, -7.4f + row * 1.85f), new Color(0.25f, 0.82f, 0.86f, 0.28f), 0.025f, 4f);
        }

        private static void DrawTitle(
            Transform parent, ICollection<UnityEngine.Object> created, SpatialBattleResult result)
        {
            AddText("Title", parent, created, "G1  battle-rules-v2  |  2 MELEE + 1 RANGED / SIDE",
                new Vector3(0f, 8.75f, 0f), 42, Color.white, TextAnchor.MiddleCenter);
            AddText("Boundary", parent, created,
                "NO REWARDS | EXISTING STATIC BATTLEUNITS | NOT FULL ANIMATION | V1 UNCHANGED",
                new Vector3(0f, 8.22f, 0f), 25, new Color(0.95f, 0.78f, 0.34f), TextAnchor.MiddleCenter);
            AddText("Hash", parent, created,
                "tick 120 / " + result.CompletedTicks + "    event " + result.EventHash +
                "    final " + result.FinalStateHash,
                new Vector3(0f, 7.78f, 0f), 22, new Color(0.72f, 0.92f, 0.92f), TextAnchor.MiddleCenter);
        }

        private static void DrawTargetLines(
            Transform parent,
            ICollection<UnityEngine.Object> created,
            IDictionary<string, EvidenceUnit> views)
        {
            foreach (var pair in views)
            {
                var unit = pair.Value;
                EvidenceUnit target;
                if (unit.Health <= 0 || string.IsNullOrEmpty(unit.TargetId) ||
                    !views.TryGetValue(unit.TargetId, out target) || target.Health <= 0) continue;
                DrawLine("Target-" + unit.Id, parent, created, Canvas(unit.Position), Canvas(target.Position),
                    unit.IsPlayer ? new Color(0.20f, 0.90f, 1f, 0.46f) :
                        new Color(1f, 0.32f, 0.24f, 0.46f), 0.035f, 1f);
            }
        }

        private static void DrawUnits(
            Transform parent,
            ICollection<UnityEngine.Object> created,
            IDictionary<string, EvidenceUnit> views)
        {
            foreach (var pair in views)
            {
                var unit = pair.Value;
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath(unit.Id));
                if (sprite == null) continue;
                var renderer = NewSprite(unit.Id, parent, sprite, created);
                var point = Canvas(unit.Position);
                renderer.transform.position = new Vector3(point.x, point.y, -2f + unit.Position.Y / 20000f);
                var bounds = sprite.bounds.size;
                var targetHeight = unit.Role == SpatialUnitRole.Melee ? 1.65f : 1.55f;
                var scale = targetHeight / bounds.y;
                renderer.transform.localScale = new Vector3(unit.IsPlayer ? scale : -scale, scale, 1f);
                renderer.color = unit.Health > 0 ? Color.white : new Color(0.3f, 0.3f, 0.3f);
                AddText("Label-" + unit.Id, parent, created,
                    unit.Id + "  " + unit.Role + "\n" + unit.State + "  HP " + unit.Health + "/" + unit.MaximumHealth,
                    new Vector3(point.x, point.y - 1.13f, -4f), 22,
                    unit.IsPlayer ? new Color(0.46f, 0.93f, 1f) : new Color(1f, 0.62f, 0.52f),
                    TextAnchor.UpperCenter);
            }
        }

        private static string SpritePath(string id)
        {
            if (id.EndsWith("M1", StringComparison.Ordinal))
                return "Assets/Resources/Art/BattleUnits/ART-CHAR-C01-LANTERN-SHIELD-GUARD-02.png";
            if (id.EndsWith("M2", StringComparison.Ordinal))
                return "Assets/Resources/Art/BattleUnits/ART-CHAR-C05-ROCK-HAMMER-GUARD-02.png";
            return "Assets/Resources/Art/BattleUnits/ART-CHAR-C06-EMBER-THROWER-02.png";
        }

        private static Vector2 Canvas(SpatialPoint point)
        {
            return new Vector2(-5f + point.X / 10000f * 10f, 7.4f - point.Y / 16000f * 14.8f);
        }

        private static SpriteRenderer NewSprite(
            string name, Transform parent, Sprite sprite, ICollection<UnityEngine.Object> created)
        {
            var gameObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            created.Add(gameObject);
            gameObject.transform.SetParent(parent, false);
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            return renderer;
        }

        private static void AddText(
            string name,
            Transform parent,
            ICollection<UnityEngine.Object> created,
            string value,
            Vector3 position,
            int fontSize,
            Color color,
            TextAnchor anchor)
        {
            var gameObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            created.Add(gameObject);
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.position = position;
            var text = gameObject.AddComponent<TextMesh>();
            text.text = value;
            text.fontSize = fontSize;
            text.characterSize = 0.035f;
            text.color = color;
            text.anchor = anchor;
            text.alignment = TextAlignment.Center;
        }

        private static void DrawLine(
            string name,
            Transform parent,
            ICollection<UnityEngine.Object> created,
            Vector2 start,
            Vector2 end,
            Color color,
            float width,
            float z)
        {
            var gameObject = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
            created.Add(gameObject);
            gameObject.transform.SetParent(parent, false);
            var line = gameObject.AddComponent<LineRenderer>();
            var shader = Shader.Find("Sprites/Default");
            line.material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            created.Add(line.material);
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.positionCount = 2;
            line.useWorldSpace = true;
            line.SetPosition(0, new Vector3(start.x, start.y, z));
            line.SetPosition(1, new Vector3(end.x, end.y, z));
        }

        private static string EvidencePath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "..",
                "outputs", "evidence", EvidenceFileName));
        }

        private sealed class EvidenceUnit
        {
            public readonly string Id;
            public readonly bool IsPlayer;
            public readonly SpatialUnitRole Role;
            public readonly int MaximumHealth;
            public SpatialPoint Position;
            public int Health;
            public string TargetId = string.Empty;
            public SpatialActionState State = SpatialActionState.AcquireTarget;

            public EvidenceUnit(SpatialBattleUnitInput input)
            {
                Id = input.BattleUnitId;
                IsPlayer = input.IsPlayer;
                Role = input.Role;
                MaximumHealth = input.MaximumHealth;
                Health = input.MaximumHealth;
                Position = input.Position;
            }
        }
    }
}
