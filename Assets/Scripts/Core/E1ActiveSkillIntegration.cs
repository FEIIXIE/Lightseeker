using System;

namespace SeekerAutoChess.Core
{
    public static class E1ActiveSkillIntegration
    {
        public const string DefinitionId = "enemy.e1.fortress";
        public const string SteadyLineAbilityId = "S_E1_STEADY_LINE";
        public const string FeedbackProfileId = "FX_E1_STEADY_LINE_PULSE_R40";
        public const int TriggerActionCount = 3;
        public const int SelfShieldBasisPoints = 1500;
        public const int FrontlineShieldBasisPoints = 800;
        public const int ShieldLifetimeActions = 2;

        public static bool IsE1(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                string.Equals(unit.DefinitionId, DefinitionId, StringComparison.Ordinal);
        }
    }
}
