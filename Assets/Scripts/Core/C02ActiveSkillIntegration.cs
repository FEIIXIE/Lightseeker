using System;
using System.Collections.Generic;
using System.Linq;

namespace SeekerAutoChess.Core
{
    public static class C02ActiveSkillIntegration
    {
        public const string ReturnBeaconAbilityId = "S_YIWEN_RETURN_BEACON";
        public const string WaitingAbilityId = "P_YIWEN_WAITING";
        public const string RescueBeamProfileId = "PP_RESCUE_BEAM";
        public const string ReturnBeaconStatusKey = "LethalProtected";

        public static CardBattlePresentationRule CreateReturnBeaconRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = ReturnBeaconAbilityId,
                ActionClass = CardBattleActionClass.HeroActive,
                TriggerRule = "C02_ACTIVE_TRIGGER",
                TargetRule = "LOWEST_HEALTH_LIVING_ALLY",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.InstantTarget,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.ReselectOnceBeforeRelease,
                ReselectLimit = 1
            };
        }

        public static CardBattlePresentationRule CreateWaitingRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = WaitingAbilityId,
                ActionClass = CardBattleActionClass.TriggeredEffect,
                TriggerRule = "C02_LETHAL_ALLY_TRIGGER",
                TargetRule = "OTHER_LIVING_ALLY_BEFORE_LETHAL",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.Beam,
                ProjectileProfileId = RescueBeamProfileId,
                TravelMs = 120,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.TriggerCommitted,
                ReselectLimit = 0
            };
        }

        public static List<string> StableSourceOrder(IEnumerable<string> sourceIds)
        {
            return (sourceIds ?? new List<string>())
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct(StringComparer.Ordinal)
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
        }

        public static bool IsC02Ability(string abilityId)
        {
            return string.Equals(abilityId, ReturnBeaconAbilityId, StringComparison.Ordinal) ||
                   string.Equals(abilityId, WaitingAbilityId, StringComparison.Ordinal);
        }
    }
}
