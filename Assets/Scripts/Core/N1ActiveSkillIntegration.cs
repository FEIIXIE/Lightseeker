using System;
using System.Collections.Generic;
using System.Linq;

namespace SeekerAutoChess.Core
{
    public static class N1ActiveSkillIntegration
    {
        public const string HoldGateAbilityId = "S_N1_HOLD_GATE";
        public const string HoldGateStampResourceId = "FX_N1_HOLD_GATE_STAMP";
        public const string ShieldStatusId = "N1_HOLD_GATE_SHIELD";
        public const int TriggerActionCount = 3;
        public const int ShieldLifetimeActions = 2;
        public const int SelfShieldBasisPoints = 1200;
        public const int RearAllyShieldBasisPoints = 800;

        public static CardBattlePresentationRule CreateHoldGateRule()
        {
            return new CardBattlePresentationRule
            {
                AbilityId = HoldGateAbilityId,
                ActionClass = CardBattleActionClass.TriggeredEffect,
                TriggerRule = "N1_EVERY_THIRD_ACTION",
                TargetRule = "SELF_AND_SAME_COLUMN_REAR_ALLIES",
                MaxTargetCount = 3,
                DeliveryMode = CardBattleDeliveryMode.InstantTarget,
                TravelMs = 0,
                ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventMultiTarget,
                CancelPolicy = CardBattleCancelPolicy.FilterChildrenBeforeRelease,
                ReselectLimit = 0,
                Tags = { "no-projectile", "no-collider", "shield-lifetime-two-owner-actions" }
            };
        }

        public static List<BattleUnitState> StableSameColumnRearTargets(
            BattleUnitState source, IEnumerable<BattleUnitState> allies)
        {
            if (source == null) return new List<BattleUnitState>();
            var column = ColumnForSlot(source.Slot);
            return (allies ?? new List<BattleUnitState>())
                .Where(unit => unit != null && unit.Health > 0 &&
                    ColumnForSlot(unit.Slot) == column && unit.Slot > source.Slot)
                .OrderBy(unit => unit.Slot)
                .ThenBy(unit => unit.Id, StringComparer.Ordinal)
                .ToList();
        }

        public static int ColumnForSlot(int slot)
        {
            return slot < 0 ? -1 : slot % 3;
        }

        public static bool IsN1Ability(string abilityId)
        {
            return string.Equals(abilityId, HoldGateAbilityId, StringComparison.Ordinal);
        }
    }
}
