using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public static class B2ActiveSkillIntegration
    {
        public const string DefinitionId = "enemy.b2.hunter";
        public const string BasicAbilityId = "A_B2_BASIC";
        public const string DoubleLightAbilityId = "S_B2_DOUBLE_LIGHT";
        public const string RecallAbilityId = "P_B2_RECALL_LOST";
        public const string DeviantLightAbilityId = "P_B2_DEVIANT_LIGHT";
        public const string BeamProfileId = "PP_BOSS_LAMP_BEAM";
        public const string RecallProfileId = "FX_B2_RECALL_LOST_SIGNAL";
        public const string DeviantLightProfileId = "FX_B2_DEVIANT_LIGHT_MARK";
        public const int TriggerActionCount = 3;
        public const int BasicDamageBasisPoints = 10000;
        public const int BasicLowHealthBonusPercent = 25;
        public const int MarkedTargetBonusPercent = 20;
        public const int DoubleLightDamageBasisPoints = 13000;
        public const int DoubleLightSpeedPenaltyBasisPoints = -1500;
        public const int MarkHealthThresholdBasisPoints = 4000;
        public const int MarkActionCount = 2;
        public const int Recall70ThresholdBasisPoints = 7000;
        public const int Recall35ThresholdBasisPoints = 3500;
        public const int FullRosterShieldBasisPoints = 1200;

        public static bool IsB2(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                string.Equals(unit.DefinitionId, DefinitionId, StringComparison.Ordinal);
        }

        public static BattleUnitState SelectBasicTarget(IEnumerable<BattleUnitState> opponents)
        {
            return SelectBasicTarget(opponents, null);
        }

        public static BattleUnitState SelectBasicTarget(IEnumerable<BattleUnitState> opponents,
            string excludedId)
        {
            var candidates = Alive(opponents);
            if (!string.IsNullOrEmpty(excludedId))
                candidates.RemoveAll(unit => string.Equals(unit.Id, excludedId, StringComparison.Ordinal));
            candidates.Sort((left, right) =>
            {
                var leftMarked = left.B2DeviantLightActionsRemaining > 0 ? 0 : 1;
                var rightMarked = right.B2DeviantLightActionsRemaining > 0 ? 0 : 1;
                if (leftMarked != rightMarked) return leftMarked.CompareTo(rightMarked);
                var leftHealth = left.Health;
                var rightHealth = right.Health;
                if (leftHealth != rightHealth) return leftHealth.CompareTo(rightHealth);
                if (left.Slot != right.Slot) return left.Slot.CompareTo(right.Slot);
                return StringComparer.Ordinal.Compare(left.Id, right.Id);
            });
            return candidates.Count == 0 ? null : candidates[0];
        }

        public static List<BattleUnitState> SelectDoubleLightTargets(
            IEnumerable<BattleUnitState> opponents, int maximumTargets)
        {
            return SelectDoubleLightTargets(opponents, maximumTargets, null);
        }

        public static List<BattleUnitState> SelectDoubleLightTargets(
            IEnumerable<BattleUnitState> opponents, int maximumTargets, string excludedId)
        {
            var candidates = Alive(opponents);
            if (!string.IsNullOrEmpty(excludedId))
                candidates.RemoveAll(unit => string.Equals(unit.Id, excludedId, StringComparison.Ordinal));
            candidates.Sort((left, right) =>
            {
                var leftHealth = left.Health;
                var rightHealth = right.Health;
                if (leftHealth != rightHealth) return leftHealth.CompareTo(rightHealth);
                if (left.Slot != right.Slot) return left.Slot.CompareTo(right.Slot);
                return StringComparer.Ordinal.Compare(left.Id, right.Id);
            });
            if (candidates.Count > maximumTargets) candidates.RemoveRange(maximumTargets,
                candidates.Count - maximumTargets);
            return candidates;
        }

        public static BattleUnitState CreateRecalledUnit(string id, int slot, int variant)
        {
            var unit = new BattleUnitState
            {
                Id = id,
                DefinitionId = "enemy.n" + Math.Max(1, Math.Min(5, variant)) + "." +
                    (variant == 1 ? "guard" : variant == 2 ? "fighter" :
                     variant == 3 ? "ranged" : variant == 4 ? "skirmisher" : "support"),
                IsPlayer = false,
                Slot = slot,
                Health = variant == 1 ? 20 : variant == 2 ? 16 : variant == 3 ? 12 :
                    variant == 4 ? 13 : 14,
                MaxHealth = variant == 1 ? 20 : variant == 2 ? 16 : variant == 3 ? 12 :
                    variant == 4 ? 13 : 14,
                AttackDamage = variant == 1 ? 3 : variant == 2 ? 5 : variant == 3 ? 5 :
                    variant == 4 ? 4 : 2,
                HealAmount = variant == 5 ? 5 : 0,
                Armor = variant == 1 ? 20 : variant == 2 ? 10 : variant == 3 ? 3 :
                    variant == 4 ? 5 : 7,
                SpeedTier = variant == 1 ? CombatSpeedTier.Slow :
                    variant == 3 || variant == 4 ? CombatSpeedTier.Fast : CombatSpeedTier.Standard,
                BaseAttackSpeedBasisPoints = variant == 1 ? 8000 :
                    variant == 3 || variant == 4 ? 12000 : 10000,
                IsHealer = variant == 5,
                FactionId = "faction.enemy",
                ProfessionId = variant == 5 ? "profession.support" : "profession.output",
                Quality = CharacterQuality.Common
            };
            return unit;
        }

        private static List<BattleUnitState> Alive(IEnumerable<BattleUnitState> units)
        {
            var result = new List<BattleUnitState>();
            foreach (var unit in units ?? new List<BattleUnitState>())
                if (unit != null && unit.Health > 0) result.Add(unit);
            return result;
        }
    }
}
