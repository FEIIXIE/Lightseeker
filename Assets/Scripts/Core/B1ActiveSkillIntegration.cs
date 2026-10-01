using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public static class B1ActiveSkillIntegration
    {
        public const string DefinitionId = "enemy.b1.heavy";
        public const string ClosedArmorAbilityId = "P_B1_CLOSED_ARMOR";
        public const string BlockadeQuakeAbilityId = "S_B1_BLOCKADE_QUAKE";
        public const string NoReturnAbilityId = "P_B1_NO_RETURN";
        public const string FeedbackProfileId = "FX_B1_BLOCKADE_QUAKE_R45";
        public const int TriggerActionCount = 3;
        public const int ClosedArmorShieldBasisPoints = 2500;
        public const int ShieldedDamageReductionPercent = 20;
        public const int BrokenArmorDamageTakenPercent = 15;
        public const int BrokenArmorActionCount = 2;
        public const int BlockadeQuakeDamageBasisPoints = 11000;
        public const int NoReturnHealthThresholdBasisPoints = 5000;
        public const int NoReturnShieldBasisPoints = 2000;
        public const int NoReturnDamageBonusBasisPoints = 2500;
        public const int NoReturnDamageBonusActionCount = 2;

        public static bool IsB1(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                string.Equals(unit.DefinitionId, DefinitionId, StringComparison.Ordinal);
        }

        public static List<BattleUnitState> StableBlockadeTargets(
            IEnumerable<BattleUnitState> opponents)
        {
            var frontline = new List<BattleUnitState>();
            var backline = new List<BattleUnitState>();
            foreach (var unit in opponents ?? new List<BattleUnitState>())
            {
                if (unit == null || unit.Health <= 0) continue;
                if (unit.Slot >= 0 && unit.Slot < 3) frontline.Add(unit);
                else backline.Add(unit);
            }
            var targets = frontline.Count > 0 ? frontline : backline;
            targets.Sort(CompareBySlotAndId);
            return targets;
        }

        public static BattleUnitState SelectBasicTarget(BattleUnitState source,
            IEnumerable<BattleUnitState> opponents)
        {
            if (source == null) return null;
            var candidates = new List<BattleUnitState>();
            foreach (var unit in opponents ?? new List<BattleUnitState>())
                if (unit != null && unit.Health > 0) candidates.Add(unit);
            var column = source.Slot < 0 ? 0 : source.Slot % 3;
            var preferredFrontSlot = column;
            var preferredRearSlot = column + 3;
            var preferred = FindSlot(candidates, preferredFrontSlot);
            if (preferred != null) return preferred;
            preferred = FindSlot(candidates, preferredRearSlot);
            if (preferred != null) return preferred;
            candidates.Sort(CompareBySlotAndId);
            return candidates.Count > 0 ? candidates[0] : null;
        }

        private static BattleUnitState FindSlot(IEnumerable<BattleUnitState> units, int slot)
        {
            BattleUnitState selected = null;
            foreach (var unit in units)
            {
                if (unit.Slot != slot) continue;
                if (selected == null || StringComparer.Ordinal.Compare(unit.Id, selected.Id) < 0)
                    selected = unit;
            }
            return selected;
        }

        private static int CompareBySlotAndId(BattleUnitState left, BattleUnitState right)
        {
            if (left.Slot != right.Slot) return left.Slot.CompareTo(right.Slot);
            return StringComparer.Ordinal.Compare(left.Id, right.Id);
        }
    }
}
