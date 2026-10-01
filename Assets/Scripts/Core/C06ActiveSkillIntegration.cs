using System;
using System.Collections.Generic;
using System.Linq;

namespace SeekerAutoChess.Core
{
    public static class C06ActiveSkillIntegration
    {
        public const string ShareFireAbilityId = "S_NOCK_SHARE_FIRE";
        public const string ReturnFireAbilityId = "P_NOCK_RETURN_FIRE";
        public const string FireChargeStatusId = "C06_FIRE_CHARGE";
        public const string ReturnFireBuffStatusId = "C06_RETURN_FIRE_BUFF";
        public const string FireShareProjectileProfileId = "PP_FIRE_SHARE";
        public const string FireCauldronProjectileProfileId = "PP_FIRE_CAULDRON";
        public const string FireChargeResourceId = "FX_C06_FIRE_CHARGE_PIP";
        public const string ReturnFireBuffResourceId = "FX_C06_RETURN_FIRE_BUFF";
        public const int NockSelfChargeCount = 3;
        public const int AllyChargeCount = 2;
        public const int AttachedFireDamageBasisPoints = 4500;
        public const int ReturnFireTriggerCount = 6;
        public const int ReturnFireDamageBasisPoints = 12000;
        public const int ReturnFireDamageBonusBasisPoints = 1500;
        public const int ReturnFireAttackSpeedBonusBasisPoints = 2000;
        public const int ReturnFireActionCount = 1;

        public static CardBattlePresentationRule CreateShareFireRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = ShareFireAbilityId,
                ActionClass = CardBattleActionClass.HeroActive,
                TriggerRule = "C06_ACTIVE_TRIGGER",
                TargetRule = "ALL_LIVING_ALLIES_AT_RELEASE",
                MaxTargetCount = 6,
                DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = FireShareProjectileProfileId,
                TravelMs = 220,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventMultiTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 0
            };
        }

        public static CardBattlePresentationRule CreateReturnFireRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = ReturnFireAbilityId,
                ActionClass = CardBattleActionClass.TriggeredEffect,
                TriggerRule = "C06_SIXTH_LEGAL_FIRE_DAMAGE",
                TargetRule = "LOWEST_HEALTH_RATIO_LIVING_ENEMY",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = FireCauldronProjectileProfileId,
                TravelMs = 360,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.TriggerCommitted,
                ReselectLimit = 0,
                 Tags = { "secondary", "no-recursion", "attack-speed-continuous" }
            };
        }

        public static List<string> StableLivingTargetOrder(IEnumerable<BattleUnitState> units)
        {
            return (units ?? new List<BattleUnitState>())
                .Where(unit => unit != null && unit.Health > 0)
                .OrderBy(unit => unit.Slot)
                .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                .Select(unit => unit.Id)
                .ToList();
        }

        public static bool IsC06Ability(string abilityId)
        {
            return string.Equals(abilityId, ShareFireAbilityId, StringComparison.Ordinal) ||
                   string.Equals(abilityId, ReturnFireAbilityId, StringComparison.Ordinal);
        }
    }
}
