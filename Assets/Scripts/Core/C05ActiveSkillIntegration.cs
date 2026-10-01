using System;

namespace SeekerAutoChess.Core
{
    public static class C05ActiveSkillIntegration
    {
        public const string RebuildBridgeAbilityId = "S_BRUN_REBUILD_BRIDGE";
        public const string HoldBridgeAbilityId = "P_BRUN_HOLD_BRIDGE";
        public const string BarrierStatusId = "C05_SHARED_COLUMN_BARRIER";
        public const string BarrierBrokenStatusId = "C05_BARRIER_BROKEN";
        public const string ColumnActionBuffStatusId = "C05_COLUMN_ACTION_BUFF";
        public const string BarrierResourceId = "FX_C05_BRIDGE_BARRIER";
        public const string BarrierBreakResourceId = "FX_C05_BRIDGE_BREAK_BUFF";
        public const string ColumnActionBuffResourceId = "FX_C05_COLUMN_ACTION_BUFF";
        public const int BarrierCapacityBasisPoints = 2500;
        public const int BarrierLifetimeActions = 3;
        public const int HalfCapacityCounterBasisPoints = 5000;
        public const int CounterAttackBasisPoints = 12000;
        public const int ColumnActionAttackBonusBasisPoints = 3500;
        public const int ColumnActionHealBonusBasisPoints = 3500;
        public const int ColumnActionCount = 1;
        public const int PersonalShieldBasisPoints = 1000;

        public static CardBattlePresentationRule CreateRebuildBridgeRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = RebuildBridgeAbilityId,
                ActionClass = CardBattleActionClass.HeroActive,
                TriggerRule = "C05_ACTIVE_TRIGGER",
                TargetRule = "TR_COLUMN_BOUND",
                MaxTargetCount = 1,
                DeliveryMode = CardBattleDeliveryMode.GroundPulse,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget,
                CancelPolicy = CardBattleCancelPolicy.SelfIfActorAlive,
                ReselectLimit = 0,
                Tags = { "no-collider", "barrier-column-bound" }
            };
        }

        public static bool IsC05Ability(string abilityId)
        {
            return string.Equals(abilityId, RebuildBridgeAbilityId, StringComparison.Ordinal) ||
                   string.Equals(abilityId, HoldBridgeAbilityId, StringComparison.Ordinal);
        }
    }
}
