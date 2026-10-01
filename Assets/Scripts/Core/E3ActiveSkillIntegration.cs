using System;

namespace SeekerAutoChess.Core
{
    public static class E3ActiveSkillIntegration
    {
        public const string DefinitionId = "enemy.e3.caster";
        public const string BellReturnAbilityId = "S_E3_BELL_RETURN";
        public const string FeedbackProfileId = "FX_E3_BELL_RETURN_CHIME_R42";
        public const int TriggerActionCount = 3;
        public const int HealBasisPoints = 5500;
        public const int NextActionBonusBasisPoints = 1200;

        public static bool IsE3(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                string.Equals(unit.DefinitionId, DefinitionId, StringComparison.Ordinal);
        }
    }
}
