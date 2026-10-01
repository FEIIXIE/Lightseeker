using System;

namespace SeekerAutoChess.Core
{
    public static class N4ActiveSkillIntegration
    {
        public const string MistHuntAbilityId = "S_N4_MIST_HUNT";
        public const string MistHuntFeedbackProfileId = "FX_N4_MIST_HUNT_CRESCENT";
        public const int TriggerActionCount = 3;
        public const int DirectDamageBasisPoints = 14000;
        public const int ExecuteDamageBonusPercent = 15;

        public static CardBattlePresentationRule CreateMistHuntRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = MistHuntAbilityId,
                ActionClass = CardBattleActionClass.RhythmSkill,
                TriggerRule = "N4_EVERY_THIRD_ACTION",
                TargetRule = "LOWEST_HEALTH_ENEMY",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.Melee,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 1,
                Tags = { "melee", "single-authoritative-impact", "no-projectile", "no-movement" }
            };
        }

        public static bool IsMistHuntAbility(string abilityId)
        {
            return string.Equals(abilityId, MistHuntAbilityId, StringComparison.Ordinal);
        }

        public static bool IsN4(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                   string.Equals(unit.DefinitionId, "enemy.n4.skirmisher", StringComparison.Ordinal);
        }
    }
}
