using System;

namespace SeekerAutoChess.Core
{
    public static class N3ActiveSkillIntegration
    {
        public const string HeavyBoltAbilityId = "S_N3_HEAVY_BOLT";
        public const string HeavyBoltProjectileProfileId = "PP_CROSSBOW_HEAVY";
        public const int TriggerActionCount = 3;
        public const int DirectDamageBasisPoints = 15000;

        public static CardBattlePresentationRule CreateHeavyBoltRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = HeavyBoltAbilityId,
                ActionClass = CardBattleActionClass.RhythmSkill,
                TriggerRule = "N3_EVERY_THIRD_ACTION",
                TargetRule = "HIGHEST_ATTACK_ENEMY",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = HeavyBoltProjectileProfileId,
                TravelMs = 300,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 1,
                Tags = { "ranged", "single-authoritative-impact", "no-status" }
            };
        }

        public static bool IsHeavyBoltAbility(string abilityId)
        {
            return string.Equals(abilityId, HeavyBoltAbilityId, StringComparison.Ordinal);
        }

        public static bool IsN3(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                   string.Equals(unit.DefinitionId, "enemy.n3.ranged", StringComparison.Ordinal);
        }
    }
}
