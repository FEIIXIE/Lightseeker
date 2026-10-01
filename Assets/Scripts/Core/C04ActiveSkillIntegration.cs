using System;

namespace SeekerAutoChess.Core
{
    public static class C04ActiveSkillIntegration
    {
        public const string HiddenSpringAbilityId = "S_MILA_HIDDEN_SPRING";
        public const string AfterSpringAbilityId = "P_MILA_AFTER_SPRING";
        public const string HiddenSpringStoredStatusId = "C04_HIDDEN_SPRING_STORED";
        public const string HiddenSpringProjectileProfileId = "PP_HIDDEN_SPRING_SEED";
        public const int HiddenSpringTargetCount = 2;
        public const int HiddenSpringActionCount = 2;
        public const int HiddenSpringAttackBonusBasisPoints = 3500;
        public const int HiddenSpringHealBonusBasisPoints = 3500;
        public const int HiddenSpringAttackSpeedBonusBasisPoints = 2500;
        public const int AfterSpringBasisPointsPerTenPercent = 800;
        public const int AfterSpringMaximumBasisPoints = 4000;
        public const int AfterSpringActionCount = 3;

        public static CardBattlePresentationRule CreateHiddenSpringRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = HiddenSpringAbilityId,
                ActionClass = CardBattleActionClass.HeroActive,
                TriggerRule = "C04_ACTIVE_TRIGGER",
                TargetRule = "LOWEST_HEALTH_RATIO_LIVING_ALLIES",
                MaxTargetCount = HiddenSpringTargetCount,
                DeliveryMode = CardBattleDeliveryMode.Projectile,
                ProjectileProfileId = HiddenSpringProjectileProfileId,
                TravelMs = 300,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventMultiTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 0
            };
        }

        public static CardBattlePresentationRule CreateAfterSpringRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = AfterSpringAbilityId,
                ActionClass = CardBattleActionClass.TriggeredEffect,
                TriggerRule = "C04_POOL_RELEASE",
                TargetRule = "SAME_POOL_OWNER",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.InstantTarget,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.TriggerCommitted,
                ReselectLimit = 0
            };
        }

        public static bool IsC04Ability(string abilityId)
        {
            return string.Equals(abilityId, HiddenSpringAbilityId, StringComparison.Ordinal) ||
                   string.Equals(abilityId, AfterSpringAbilityId, StringComparison.Ordinal);
        }
    }
}
