using System;

namespace SeekerAutoChess.Core
{
    public static class N5ActiveSkillIntegration
    {
        public const string DoubleStitchAbilityId = "S_N5_DOUBLE_STITCH";
        public const string DoubleStitchProjectileProfileId = "PP_CLOTH_HEAL";
        public const int TriggerActionCount = 3;
        public const int MaximumTargets = 2;
        public const int HealBasisPoints = 9000;

        public static CardBattlePresentationRule CreateDoubleStitchRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = DoubleStitchAbilityId,
                ActionClass = CardBattleActionClass.RhythmSkill,
                TriggerRule = "N5_EVERY_THIRD_ACTION",
                TargetRule = "LOWEST_HEALTH_INJURED_ALLIES",
                MaxTargetCount = MaximumTargets,
                DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = DoubleStitchProjectileProfileId,
                TravelMs = 260,
                ImpactEmissionMode = CardBattleImpactEmissionMode.MultiEventPhased,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 1,
                Tags = { "support", "stable-child-index", "no-target-is-no-valid-target" }
            };
        }

        public static bool IsDoubleStitchAbility(string abilityId)
        {
            return string.Equals(abilityId, DoubleStitchAbilityId, StringComparison.Ordinal);
        }

        public static bool IsN5(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                   string.Equals(unit.DefinitionId, "enemy.n5.support", StringComparison.Ordinal);
        }
    }
}
