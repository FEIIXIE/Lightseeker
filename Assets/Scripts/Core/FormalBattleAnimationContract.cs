using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public static class FormalBattleAnimationIds
    {
        public const string Idle = "Idle";
        public const string BasicGateShieldStrike = "Basic_GateShieldStrike";
        public const string VariantGateStand = "Variant_GateStand";
        public const string Hit = "Hit";
        public const string Defeat = "Defeat";
    }

    public sealed class FormalBattleAnimationFrame
    {
        public int TimeMs { get; }
        public string ResourcePath { get; }

        public FormalBattleAnimationFrame(int timeMs, string resourcePath)
        {
            if (timeMs < 0) throw new ArgumentOutOfRangeException(nameof(timeMs));
            if (string.IsNullOrWhiteSpace(resourcePath))
                throw new ArgumentException("Resource path is required.", nameof(resourcePath));
            TimeMs = timeMs;
            ResourcePath = resourcePath;
        }
    }

    public sealed class FormalBattleAnimationClip
    {
        private readonly FormalBattleAnimationFrame[] frames;

        public string Id { get; }
        public int DurationMs { get; }
        public bool Loop { get; }
        public int ReleaseAtMs { get; }
        public int ImpactAtMs { get; }
        public IReadOnlyList<FormalBattleAnimationFrame> Frames { get { return frames; } }

        public FormalBattleAnimationClip(
            string id,
            int durationMs,
            bool loop,
            int releaseAtMs,
            int impactAtMs,
            params FormalBattleAnimationFrame[] frames)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Clip id is required.", nameof(id));
            if (durationMs <= 0) throw new ArgumentOutOfRangeException(nameof(durationMs));
            if (frames == null || frames.Length == 0) throw new ArgumentException("Frames are required.", nameof(frames));
            for (var index = 0; index < frames.Length; index++)
            {
                if (frames[index] == null) throw new ArgumentException("Frames cannot contain null.", nameof(frames));
                if (index > 0 && frames[index].TimeMs < frames[index - 1].TimeMs)
                    throw new ArgumentException("Frame times must be monotonic.", nameof(frames));
                if (frames[index].TimeMs > durationMs)
                    throw new ArgumentException("Frame time exceeds clip duration.", nameof(frames));
            }
            Id = id;
            DurationMs = durationMs;
            Loop = loop;
            ReleaseAtMs = releaseAtMs;
            ImpactAtMs = impactAtMs;
            this.frames = frames;
        }

        public FormalBattleAnimationFrame FrameAt(int elapsedMs)
        {
            var sampleMs = Math.Max(0, elapsedMs);
            if (Loop) sampleMs %= DurationMs;
            else sampleMs = Math.Min(DurationMs, sampleMs);

            var selected = frames[0];
            for (var index = 1; index < frames.Length; index++)
            {
                if (frames[index].TimeMs > sampleMs) break;
                selected = frames[index];
            }
            return selected;
        }
    }

    /// <summary>
    /// Runtime contract for imported fixed-position action candidates. Entries remain
    /// candidates until art continuity and E49 authoritative-event validation both pass.
    /// The visual contract never creates damage, targets, projectiles, movement or turns.
    /// </summary>
    public static class FormalBattleAnimationContract
    {
        public const string CandidateStatus =
            "FORMAL_ANIMATION_CANDIDATE_PENDING_ART_AND_RUNTIME_ACCEPTANCE";
        public const string GateboardGuardDefinitionId = "enemy.n1.guard";
        public const bool RuntimeEnabled = false;
        public const string GateStandSourceEffectId = "N1.GateStand";
        public const string ResourcesRoot = "Art/BattleActions/N1GateboardGuard/v0_2/";
        public const int CanvasWidth = 1212;
        public const int CanvasHeight = 1297;
        public const int GroundX = 606;
        public const int GroundYTopOrigin = 1267;
        public const bool AllowsRootMotion = false;
        public const bool AllowsFacingFlip = false;
        public const int DefeatFinalHoldMs = 350;

        private static readonly Dictionary<string, FormalBattleAnimationClip> clips = BuildClips();

        public static bool SupportsDefinition(string definitionId)
        {
            return RuntimeEnabled &&
                   string.Equals(definitionId, GateboardGuardDefinitionId, StringComparison.Ordinal);
        }

        public static bool TryGetClip(string definitionId, string clipId, out FormalBattleAnimationClip clip)
        {
            clip = null;
            return SupportsDefinition(definitionId) && !string.IsNullOrEmpty(clipId) &&
                   clips.TryGetValue(clipId, out clip);
        }

        public static FormalBattleAnimationClip GetClip(string clipId)
        {
            FormalBattleAnimationClip clip;
            if (!clips.TryGetValue(clipId, out clip))
                throw new KeyNotFoundException("Unknown formal battle animation clip: " + clipId);
            return clip;
        }

        private static Dictionary<string, FormalBattleAnimationClip> BuildClips()
        {
            var result = new Dictionary<string, FormalBattleAnimationClip>(StringComparer.Ordinal);
            result.Add(FormalBattleAnimationIds.Idle, new FormalBattleAnimationClip(
                FormalBattleAnimationIds.Idle, 1000, true, -1, -1,
                Frame(0, "frames/idle-base"),
                Frame(500, "frames/idle-breathe-up"),
                Frame(750, "frames/idle-base")));
            result.Add(FormalBattleAnimationIds.BasicGateShieldStrike, new FormalBattleAnimationClip(
                FormalBattleAnimationIds.BasicGateShieldStrike, 1250, false, 200, 300,
                Frame(0, "frames/basic-gateshieldstrike/N1-Basic-frame-00-000ms"),
                Frame(50, "frames/basic-gateshieldstrike/N1-Basic-frame-01-050ms"),
                Frame(100, "frames/basic-gateshieldstrike/N1-Basic-frame-02-100ms"),
                Frame(150, "frames/basic-gateshieldstrike/N1-Basic-frame-03-150ms"),
                Frame(200, "frames/basic-gateshieldstrike/N1-Basic-frame-04-200ms-release"),
                Frame(233, "frames/basic-gateshieldstrike/N1-Basic-frame-05-233ms"),
                Frame(267, "frames/basic-gateshieldstrike/N1-Basic-frame-06-267ms"),
                Frame(350, "frames/basic-gateshieldstrike/N1-Basic-frame-07-350ms"),
                Frame(433, "frames/basic-gateshieldstrike/N1-Basic-frame-08-433ms"),
                Frame(517, "frames/basic-gateshieldstrike/N1-Basic-frame-09-517ms"),
                Frame(600, "frames/basic-gateshieldstrike/N1-Basic-frame-10-600ms"),
                Frame(750, "frames/basic-gateshieldstrike/N1-Basic-frame-11-750ms"),
                Frame(833, "frames/basic-gateshieldstrike/N1-Basic-frame-12-833ms"),
                Frame(1000, "frames/basic-gateshieldstrike/N1-Basic-frame-13-1000ms"),
                Frame(1250, "frames/basic-gateshieldstrike/N1-Basic-frame-14-1250ms")));
            result.Add(FormalBattleAnimationIds.VariantGateStand, new FormalBattleAnimationClip(
                FormalBattleAnimationIds.VariantGateStand, 1250, false, 200, 300,
                Frame(0, "frames/variant-gatestand/N1-GateStand-frame-00-000ms"),
                Frame(50, "frames/variant-gatestand/N1-GateStand-frame-01-050ms"),
                Frame(100, "frames/variant-gatestand/N1-GateStand-frame-02-100ms"),
                Frame(150, "frames/variant-gatestand/N1-GateStand-frame-03-150ms"),
                Frame(200, "frames/variant-gatestand/N1-GateStand-frame-04-200ms-release"),
                Frame(233, "frames/variant-gatestand/N1-GateStand-frame-05-233ms"),
                Frame(267, "frames/variant-gatestand/N1-GateStand-frame-06-267ms"),
                Frame(350, "frames/variant-gatestand/N1-GateStand-frame-07-350ms"),
                Frame(433, "frames/variant-gatestand/N1-GateStand-frame-08-433ms"),
                Frame(517, "frames/variant-gatestand/N1-GateStand-frame-09-517ms"),
                Frame(600, "frames/variant-gatestand/N1-GateStand-frame-10-600ms"),
                Frame(750, "frames/variant-gatestand/N1-GateStand-frame-11-750ms"),
                Frame(833, "frames/variant-gatestand/N1-GateStand-frame-12-833ms"),
                Frame(1000, "frames/variant-gatestand/N1-GateStand-frame-13-1000ms"),
                Frame(1250, "frames/variant-gatestand/N1-GateStand-frame-14-1250ms")));
            result.Add(FormalBattleAnimationIds.Hit, new FormalBattleAnimationClip(
                FormalBattleAnimationIds.Hit, 150, false, -1, -1,
                Frame(0, "frames/hit/N1-Hit-frame-00-000ms"),
                Frame(20, "frames/hit/N1-Hit-frame-01-020ms"),
                Frame(40, "frames/hit/N1-Hit-frame-02-040ms"),
                Frame(60, "frames/hit/N1-Hit-frame-03-060ms-peak"),
                Frame(80, "frames/hit/N1-Hit-frame-04-080ms"),
                Frame(100, "frames/hit/N1-Hit-frame-05-100ms"),
                Frame(120, "frames/hit/N1-Hit-frame-06-120ms"),
                Frame(150, "frames/hit/N1-Hit-frame-07-150ms")));
            result.Add(FormalBattleAnimationIds.Defeat, new FormalBattleAnimationClip(
                FormalBattleAnimationIds.Defeat, 650, false, -1, -1,
                Frame(0, "frames/defeat/N1-Defeat-frame-00-000ms"),
                Frame(100, "frames/defeat/N1-Defeat-frame-01-100ms"),
                Frame(250, "frames/defeat/N1-Defeat-frame-02-250ms"),
                Frame(400, "frames/defeat/N1-Defeat-frame-03-400ms"),
                Frame(550, "frames/defeat/N1-Defeat-frame-04-550ms"),
                Frame(650, "frames/defeat/N1-Defeat-frame-05-650ms-final")));
            return result;
        }

        private static FormalBattleAnimationFrame Frame(int timeMs, string relativeResourcePath)
        {
            return new FormalBattleAnimationFrame(timeMs, ResourcesRoot + relativeResourcePath);
        }
    }
}
