using System;
using System.Collections.Generic;
using System.Linq;

namespace SeekerAutoChess.Core
{
    public static class C01ActiveSkillIntegration
    {
        public const string OpenDawnAbilityId = "S_OLIN_OPEN_DAWN";
        public const string TideAbilityId = "P_OLIN_TIDE";

        public static CardBattlePresentationRule CreateOpenDawnRule(bool projectileConfigured,
            int maxTargetCount)
        {
            return new CardBattlePresentationRule {
                AbilityId = OpenDawnAbilityId, ActionClass = CardBattleActionClass.HeroActive,
                TriggerRule = "C01_ACTIVE_TRIGGER", TargetRule = "C01_RANGE_TARGETS",
                MaxTargetCount = maxTargetCount,
                DeliveryMode = projectileConfigured ? CardBattleDeliveryMode.Projectile : CardBattleDeliveryMode.GroundPulse,
                ProjectileProfileId = projectileConfigured ? "PP_C01_OPEN_DAWN" : null,
                ImpactEmissionMode = maxTargetCount == 1
                    ? CardBattleImpactEmissionMode.SingleEventSingleTarget
                    : CardBattleImpactEmissionMode.SingleEventMultiTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 0
            };
        }

        public static CardBattlePresentationRule CreateTideRule()
        {
            return new CardBattlePresentationRule {
                AbilityId = TideAbilityId, ActionClass = CardBattleActionClass.TriggeredEffect,
                TriggerRule = "C01_PASSIVE_TRIGGER", TargetRule = "C01_SELF_OR_ALLY",
                MaxTargetCount = 1, DeliveryMode = CardBattleDeliveryMode.Aura,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.SelfIfActorAlive
            };
        }

        public static List<string> StableTargetOrder(IEnumerable<string> targetIds)
        {
            return (targetIds ?? new List<string>()).Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.Ordinal).OrderBy(id => id, StringComparer.Ordinal).ToList();
        }

        public static List<CardBattleTargetResult> NormalizeTargets(
            IEnumerable<CardBattleTargetResult> candidates,
            IEnumerable<CardBattleUnitSnapshot> availableUnits)
        {
            var available = new Dictionary<string, CardBattleUnitSnapshot>(StringComparer.Ordinal);
            foreach (var unit in availableUnits ?? new List<CardBattleUnitSnapshot>())
                if (unit != null && !string.IsNullOrEmpty(unit.Id)) available[unit.Id] = unit;
            var results = new List<CardBattleTargetResult>();
            foreach (var candidate in (candidates ?? new List<CardBattleTargetResult>())
                .Where(item => item != null && !string.IsNullOrEmpty(item.TargetId))
                .OrderBy(item => item.TargetId, StringComparer.Ordinal))
            {
                CardBattleUnitSnapshot unit;
                if (!available.TryGetValue(candidate.TargetId, out unit) || unit.IsDefeated) continue;
                var copy = candidate.Copy();
                copy.ChildIndex = results.Count;
                results.Add(copy);
            }
            return results;
        }

        public static bool IsC01Ability(string abilityId)
        {
            return string.Equals(abilityId, OpenDawnAbilityId, StringComparison.Ordinal) ||
                   string.Equals(abilityId, TideAbilityId, StringComparison.Ordinal);
        }
    }
}
