using System;
using System.Collections.Generic;
using System.Linq;

namespace SeekerAutoChess.Core
{
    public static class C03ActiveSkillIntegration
    {
        public const string WhiteTrailAbilityId = "S_LEIF_WHITE_TRAIL";
        public const string TransferAbilityId = "P_LEIF_TRANSFER";
        public const string WhiteTrailProjectileProfileId = "PP_ARROW_WHITE_TRAIL";
        public const string WhiteTrailStatusKey = "TargetedCritical";
        public const string HuntTeamSurgeStatusKey = "PendingActionBuff";

        public static CardBattlePresentationRule CreateWhiteTrailRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = WhiteTrailAbilityId,
                ActionClass = CardBattleActionClass.HeroActive,
                TriggerRule = "C03_ACTIVE_TRIGGER",
                TargetRule = "HIGHEST_MAX_HEALTH_LIVING_ENEMY",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = WhiteTrailProjectileProfileId,
                TravelMs = 320,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.ReselectOnceBeforeRelease,
                ReselectLimit = 1
            };
        }

        public static CardBattlePresentationRule CreateTransferRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = TransferAbilityId,
                ActionClass = CardBattleActionClass.TriggeredEffect,
                TriggerRule = "C03_MARKED_TARGET_DEFEATED",
                TargetRule = "HIGHEST_MAX_HEALTH_REMAINING_ENEMY",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.Aura,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.TriggerCommitted,
                ReselectLimit = 0,
                Tags = { "MAX_TRANSFER_COUNT_1" }
            };
        }

        public static List<string> StableHighestMaximumHealthOrder(
            IEnumerable<BattleUnitState> units)
        {
            return (units ?? new List<BattleUnitState>())
                .Where(unit => unit != null && unit.Health > 0)
                .OrderByDescending(unit => unit.MaxHealth)
                .ThenBy(unit => unit.Slot)
                .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                .Select(unit => unit.Id)
                .ToList();
        }

        public static bool IsC03Ability(string abilityId)
        {
            return string.Equals(abilityId, WhiteTrailAbilityId, StringComparison.Ordinal) ||
                   string.Equals(abilityId, TransferAbilityId, StringComparison.Ordinal);
        }
    }
}
