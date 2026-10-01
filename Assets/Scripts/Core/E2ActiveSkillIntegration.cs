using System;

namespace SeekerAutoChess.Core
{
    public static class E2ActiveSkillIntegration
    {
        public const string DefinitionId = "enemy.e2.hunter";
        public const string TrackJudgmentAbilityId = "S_E2_TRACK_JUDGMENT";
        public const string FeedbackProfileId = "FX_E2_TRACK_JUDGMENT_HOOK_R41";
        public const int TriggerActionCount = 3;
        public const int DirectDamageBasisPoints = 17000;
        public const int LowHealthBonusPercent = 25;

        public static bool IsE2(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                string.Equals(unit.DefinitionId, DefinitionId, StringComparison.Ordinal);
        }
    }
}
