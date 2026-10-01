using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class BattleAudioCue
    {
        public string CueId;
        public string BattleId;
        public string ActionId;
        public string SourceId;
        public string AbilityId;
        public string TargetId;
        public long EventSequence;
        public int ChildIndex;
        public string Outcome;
        public long AuthorityTick;
        public string StateHashAfter;
        public bool IsFallback;
        public bool IsTerminal;
        public float PlaybackRate;
    }

    public sealed class BattleAudioEventRouter
    {
        private readonly HashSet<string> consumedKeys = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> activeCueCounts =
            new Dictionary<string, int>(StringComparer.Ordinal);

        public bool Enabled { get; private set; }
        public int MaxConcurrent { get; private set; }
        public float PlaybackRate { get; private set; }
        public int SuppressedCount { get; private set; }
        public int DuplicateCount { get; private set; }
        public int ConsumedCount { get; private set; }

        public BattleAudioEventRouter(int maxConcurrent = 12)
        {
            Enabled = true;
            MaxConcurrent = Math.Max(1, maxConcurrent);
            PlaybackRate = 1f;
        }

        public IReadOnlyList<BattleAudioCue> Consume(CardBattleEvent battleEvent,
            Func<string, bool> targetIsValid = null)
        {
            var cues = new List<BattleAudioCue>();
            if (battleEvent == null || string.IsNullOrEmpty(battleEvent.ActionId) ||
                battleEvent.EventSequence <= 0) return cues;

            if (battleEvent.Kind == CardBattleEventKind.ImpactResolved)
            {
                if (battleEvent.Results == null || battleEvent.Results.Count == 0) return cues;
                for (var index = 0; index < battleEvent.Results.Count; index++)
                {
                    var result = battleEvent.Results[index];
                    if (result == null || string.IsNullOrEmpty(result.TargetId) ||
                        result.ChildIndex != index) continue;
                    if (targetIsValid != null && !targetIsValid(result.TargetId) && !result.IsDefeated)
                        continue;
                    var key = Key(battleEvent, result.ChildIndex);
                    if (!consumedKeys.Add(key))
                    {
                        DuplicateCount++;
                        continue;
                    }
                    var impactCue = ImpactCueId(result);
                    if (result.Damage > 0 && string.Equals(battleEvent.AbilityId,
                            "A_BASIC_RANGED", StringComparison.Ordinal))
                        impactCue = "RANGED_HIT";
                    else if (result.Damage > 0 && string.Equals(battleEvent.AbilityId,
                            "A_BASIC_MELEE", StringComparison.Ordinal))
                        impactCue = "MELEE_SWING";
                    var cueId = AbilityFamily(battleEvent.AbilityId) + "_" + impactCue;
                    if (string.IsNullOrEmpty(cueId)) continue;
                    var cue = CreateCue(battleEvent, result.TargetId, result.ChildIndex, cueId,
                        battleEvent.Outcome.ToString(), false);
                    if (cue != null) { cues.Add(cue); ConsumedCount++; }
                }
                return cues;
            }

            var topLevelKey = Key(battleEvent, -1);
            if (!consumedKeys.Add(topLevelKey))
            {
                DuplicateCount++;
                return cues;
            }

            var cueIdForEvent = CueIdForEvent(battleEvent);
            if (string.IsNullOrEmpty(cueIdForEvent)) return cues;
            var topLevelCue = CreateCue(battleEvent, battleEvent.TargetId, -1, cueIdForEvent,
                battleEvent.Outcome.ToString(), battleEvent.Kind == CardBattleEventKind.ActionCompleted);
            if (topLevelCue != null) { cues.Add(topLevelCue); ConsumedCount++; }
            return cues;
        }

        public void CompleteCue(BattleAudioCue cue)
        {
            if (cue == null || string.IsNullOrEmpty(cue.ActionId)) return;
            int count;
            if (!activeCueCounts.TryGetValue(cue.ActionId, out count)) return;
            if (count <= 1) activeCueCounts.Remove(cue.ActionId);
            else activeCueCounts[cue.ActionId] = count - 1;
        }

        public void SetEnabled(bool enabled)
        {
            Enabled = enabled;
            if (!enabled) activeCueCounts.Clear();
        }

        public void SetPlaybackRate(float playbackRate)
        {
            PlaybackRate = Math.Max(0.25f, Math.Min(3f, playbackRate));
        }

        public void SetMaxConcurrent(int maxConcurrent)
        {
            MaxConcurrent = Math.Max(1, maxConcurrent);
        }

        public void SkipAndFlush(string actionId)
        {
            if (!string.IsNullOrEmpty(actionId)) activeCueCounts.Remove(actionId);
        }

        public void ReconcileAfterReconnect(long lastConfirmedEventSequence,
            IEnumerable<CardBattleEvent> confirmedEvents)
        {
            if (confirmedEvents == null) return;
            foreach (var battleEvent in confirmedEvents)
            {
                if (battleEvent == null || battleEvent.EventSequence > lastConfirmedEventSequence)
                    continue;
                if (battleEvent.Kind == CardBattleEventKind.ImpactResolved && battleEvent.Results != null)
                {
                    for (var index = 0; index < battleEvent.Results.Count; index++)
                        consumedKeys.Add(Key(battleEvent, index));
                }
                else consumedKeys.Add(Key(battleEvent, -1));
            }
        }

        private BattleAudioCue CreateCue(CardBattleEvent battleEvent, string targetId,
            int childIndex, string cueId, string outcome, bool terminal)
        {
            if (!Enabled) return null;
            var activeCount = 0;
            foreach (var count in activeCueCounts.Values) activeCount += count;
            if (activeCount >= MaxConcurrent)
            {
                SuppressedCount++;
                return null;
            }
            int actionCount;
            activeCueCounts.TryGetValue(battleEvent.ActionId, out actionCount);
            activeCueCounts[battleEvent.ActionId] = actionCount + 1;
            return new BattleAudioCue
            {
                CueId = cueId,
                BattleId = battleEvent.BattleId,
                ActionId = battleEvent.ActionId,
                SourceId = battleEvent.SourceId,
                AbilityId = battleEvent.AbilityId,
                TargetId = targetId,
                EventSequence = battleEvent.EventSequence,
                ChildIndex = childIndex,
                Outcome = outcome,
                AuthorityTick = battleEvent.AuthorityTick,
                StateHashAfter = battleEvent.StateHashAfter,
                IsFallback = true,
                IsTerminal = terminal,
                PlaybackRate = PlaybackRate
            };
        }

        private static string Key(CardBattleEvent battleEvent, int childIndex)
        {
            return (battleEvent.ActionId ?? string.Empty) + ":" + battleEvent.EventSequence + ":" + childIndex;
        }

        private static string CueIdForEvent(CardBattleEvent battleEvent)
        {
            var family = AbilityFamily(battleEvent.AbilityId);
            switch (battleEvent.Kind)
            {
                case CardBattleEventKind.ActionStarted: return family + "_ACTION_STARTED";
                case CardBattleEventKind.ProjectileReleased: return family + "_PROJECTILE_RELEASED";
                case CardBattleEventKind.StatusFeedback: return StatusCueId(battleEvent.StatusKey);
                case CardBattleEventKind.ActionCompleted:
                    return family + "_ACTION_COMPLETED_" + battleEvent.Outcome.ToString().ToUpperInvariant();
                default: return string.Empty;
            }
        }

        private static string AbilityFamily(string abilityId)
        {
            if (string.Equals(abilityId, "S_OLIN_OPEN_DAWN", StringComparison.Ordinal) ||
                string.Equals(abilityId, "P_OLIN_TIDE", StringComparison.Ordinal)) return "BTL_C01";
            if (string.Equals(abilityId, "S_YIWEN_RETURN_BEACON", StringComparison.Ordinal) ||
                string.Equals(abilityId, "P_YIWEN_WAITING", StringComparison.Ordinal)) return "BTL_C02";
            if (string.Equals(abilityId, "S_LEIF_WHITE_TRAIL", StringComparison.Ordinal) ||
                string.Equals(abilityId, "P_LEIF_TRANSFER", StringComparison.Ordinal)) return "BTL_C03";
            if (string.Equals(abilityId, "S_MILA_HIDDEN_SPRING", StringComparison.Ordinal) ||
                string.Equals(abilityId, "P_MILA_AFTER_SPRING", StringComparison.Ordinal)) return "BTL_C04";
            if (!string.IsNullOrEmpty(abilityId) && abilityId.IndexOf("C05", StringComparison.OrdinalIgnoreCase) >= 0)
                return "BTL_C05";
            return "BTL";
        }

        private static string StatusCueId(string statusKey)
        {
            var normalized = Normalize(statusKey);
            if (normalized.Contains("R31")) return "BTL_C05_R31";
            if (normalized.Contains("R32")) return "BTL_C05_R32";
            if (normalized.Contains("COLUMN")) return "BTL_C05_COLUMN_BIND";
            if (normalized.Contains("BARRIER")) return "BTL_C05_BARRIER_BREAK";
            if (normalized.Contains("SHIELD") && (normalized.Contains("BREAK") || normalized.Contains("BURST")))
                return "BTL_C01_SHIELD_BREAK";
            if (normalized.Contains("SHIELD")) return "BTL_C01_SHIELD_GAIN";
            if (normalized.Contains("BEAM")) return "BTL_C02_BEAM";
            if (normalized.Contains("HEAL")) return "BTL_C02_HEAL";
            if (normalized.Contains("WHITE") || normalized.Contains("MARK"))
                return normalized.Contains("TRANSFER") ? "BTL_C03_MARK_TRANSFER" :
                    normalized.Contains("REMOVE") ? "BTL_C03_MARK_REMOVE" : "BTL_C03_MARK_ATTACH";
            if (normalized.Contains("SEED") || normalized.Contains("POOL"))
                return normalized.Contains("RELEASE") ? "BTL_C04_SEED_RELEASE" :
                    normalized.Contains("DROP") ? "BTL_C04_POOL_DROP" : "BTL_C04_SEED_STORE";
            return "BTL_STATUS_" + normalized;
        }

        private static string ImpactCueId(CardBattleTargetResult result)
        {
            if (result.Damage > 0) return result.IsDefeated ? "BTL_IMPACT_DEFEAT" : "BTL_IMPACT_HIT";
            if (result.Healing > 0) return "BTL_IMPACT_HEAL";
            if (result.ShieldDelta != 0) return result.ShieldDelta > 0
                ? "BTL_IMPACT_SHIELD_GAIN" : "BTL_IMPACT_SHIELD_BREAK";
            return result.StatusChanges != null && result.StatusChanges.Count > 0
                ? "BTL_IMPACT_STATUS" : string.Empty;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrEmpty(value)) return "GENERIC";
            var chars = value.ToUpperInvariant().ToCharArray();
            for (var index = 0; index < chars.Length; index++)
                if (!char.IsLetterOrDigit(chars[index])) chars[index] = '_';
            return new string(chars);
        }
    }

    public static class BattleAudioResourcePolicy
    {
        public const string GenericImpactResourcePath =
            "Audio/Battle/SFX_BTL_GENERIC_IMPACT_HIT_A_V02";
        public const string MeleeResourcePath =
            "Audio/Battle/SFX_BTL_MELEE_DAGGER_WOOSH_MIXKIT_1487_V01";
        public const string RangedResourcePath =
            "Audio/Battle/SFX_BTL_RANGED_ARROW_METAL_HIT_MIXKIT_2770_V01";
        public const string GenericShieldGainResourcePath =
            "Audio/Battle/SFX_BTL_GENERIC_SHIELD_GAIN_A_V02";
        public const string GenericShieldBreakResourcePath =
            "Audio/Battle/SFX_BTL_GENERIC_SHIELD_BREAK_A_V02";
        public const string BasicHealResourcePath =
            "Audio/Battle/SFX_BTL_BASIC_HEAL_ACTION_MIXKIT_2793_V02";
        public const string SkillReleaseResourcePath =
            "Audio/Battle/SFX_BTL_GENERIC_SKILL_RELEASE_SELECTED_B_V02";

        public static string ResolveResourcePath(string cueId)
        {
            var normalized = cueId ?? string.Empty;
            if (normalized.IndexOf("ACTION_STARTED", StringComparison.Ordinal) >= 0)
                return SkillReleaseResourcePath;
            if (normalized.IndexOf("IMPACT_HEAL", StringComparison.Ordinal) >= 0 ||
                normalized.IndexOf("_HEAL", StringComparison.Ordinal) >= 0)
                return BasicHealResourcePath;
            if (normalized.IndexOf("SHIELD_GAIN", StringComparison.Ordinal) >= 0)
                return GenericShieldGainResourcePath;
            if (normalized.IndexOf("SHIELD_BREAK", StringComparison.Ordinal) >= 0 ||
                normalized.IndexOf("BARRIER_BREAK", StringComparison.Ordinal) >= 0)
                return GenericShieldBreakResourcePath;
            if (normalized.IndexOf("IMPACT_HIT", StringComparison.Ordinal) >= 0 ||
                normalized.IndexOf("IMPACT_DEFEAT", StringComparison.Ordinal) >= 0)
                return GenericImpactResourcePath;
            if (normalized.IndexOf("MELEE_SWING", StringComparison.Ordinal) >= 0)
                return MeleeResourcePath;
            if (normalized.IndexOf("RANGED_HIT", StringComparison.Ordinal) >= 0)
                return RangedResourcePath;
            if (normalized.IndexOf("PROJECTILE_RELEASED", StringComparison.Ordinal) >= 0)
                return null;
            return null;
        }

        public static float ResolveVolumeScale(string resourcePath)
        {
            if (string.Equals(resourcePath, BasicHealResourcePath, StringComparison.Ordinal)) return 0.65f;
            if (string.Equals(resourcePath, SkillReleaseResourcePath, StringComparison.Ordinal)) return 0.75f;
            return 0.8f;
        }
    }
}
