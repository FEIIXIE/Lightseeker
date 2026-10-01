using System;

namespace SeekerAutoChess.Core
{
    public static class N2ActiveSkillIntegration
    {
        public const string CrossCutAbilityId = "S_N2_CROSS_CUT";
        public const string CrossCutSlashResourceId = "FX_N2_CROSS_CUT_SLASH";
        public const int TriggerActionCount = 3;
        public const int DirectDamageBasisPoints = 15000;

        public static CardBattlePresentationRule CreateCrossCutRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = CrossCutAbilityId,
                ActionClass = CardBattleActionClass.RhythmSkill,
                TriggerRule = "N2_EVERY_THIRD_ACTION",
                TargetRule = "CURRENT_TARGET_SINGLE",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.InstantTarget,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 1,
                Tags = { "melee", "no-projectile", "single-authoritative-impact", "double-visual-slash" }
            };
        }

        public static bool IsCrossCutAbility(string abilityId)
        {
            return string.Equals(abilityId, CrossCutAbilityId, StringComparison.Ordinal);
        }

        public static bool IsN2(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                   string.Equals(unit.DefinitionId, "enemy.n2.fighter", StringComparison.Ordinal);
        }
    }
}
