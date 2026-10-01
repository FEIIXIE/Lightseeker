using System;

namespace SeekerAutoChess.Core
{
    public struct PreparationBattlefieldLayoutData
    {
        public readonly float CombatOffset;
        public readonly float Expansion;
        public readonly float PlayerLabelY;
        public readonly float EnemyFrontFootY;
        public readonly float EnemyBackFootY;
        public readonly float PlayerFrontFootY;
        public readonly float PlayerBackFootY;
        public readonly float StartButtonY;
        public readonly float ToolsY;
        public readonly float BuffY;
        public readonly float BenchLabelY;
        public readonly float BenchY;

        public PreparationBattlefieldLayoutData(
            float combatOffset, float expansion, float playerLabelY,
            float enemyFrontFootY, float enemyBackFootY,
            float playerFrontFootY, float playerBackFootY,
            float startButtonY, float toolsY, float buffY, float benchLabelY, float benchY)
        {
            CombatOffset = combatOffset;
            Expansion = expansion;
            PlayerLabelY = playerLabelY;
            EnemyFrontFootY = enemyFrontFootY;
            EnemyBackFootY = enemyBackFootY;
            PlayerFrontFootY = playerFrontFootY;
            PlayerBackFootY = playerBackFootY;
            StartButtonY = startButtonY;
            ToolsY = toolsY;
            BuffY = buffY;
            BenchLabelY = benchLabelY;
            BenchY = benchY;
        }
    }

    /// <summary>
    /// Compatibility adapter for the shared v9.4 formation/battle coordinates. Legacy
    /// 1080x1920 editor previews retain their original positions, while the 1080x2400
    /// production canvas resolves exactly to the v9.4 layout contract.
    /// </summary>
    public static class PreparationBattlefieldLayoutMath
    {
        public const float MaximumBattlefieldExpansion = 480f;
        public const float EnemyFormationVerticalShift = -20f;
        public const float PlayerFormationVerticalShift = 20f;

        public static PreparationBattlefieldLayoutData Calculate(float contentHeight)
        {
            var pageExtra = Math.Max(0f, contentHeight - AdaptivePortraitLayoutMath.LegacyPageHeight);
            var expansion = Math.Min(pageExtra, MaximumBattlefieldExpansion);
            var t = expansion / MaximumBattlefieldExpansion;
            var startButtonY = Lerp(1370f, FormationBattleV94Layout.StartControl.Y, t);
            var toolsY = startButtonY + (FormationBattleV94Layout.StartControl.Height -
                FormationBattleV94Layout.RecycleControl.Height) * 0.5f;
            var benchY = Lerp(1580f, FormationBattleV94Layout.BenchCenterY -
                FormationBattleV94Layout.BenchSlotDiameter * 0.5f, t);
            var playerBackFootY = Lerp(1310f, FormationBattleV94Layout.PlayerRearFootY, t) +
                PlayerFormationVerticalShift;
            var buffY = startButtonY - (FormationBattleV94Layout.StartControl.Y -
                FormationBattleV94Layout.BuffSingleRowY);
            var minimumBuffY = playerBackFootY + 76f + 24f +
                FormationBattleV94Layout.BuffTileSize * 0.5f;
            var maximumBuffY = startButtonY - 24f -
                FormationBattleV94Layout.BuffTileSize * 0.5f;
            if (minimumBuffY <= maximumBuffY)
                buffY = Math.Max(minimumBuffY, Math.Min(maximumBuffY, buffY));

            return new PreparationBattlefieldLayoutData(
                0f,
                expansion,
                Lerp(900f, 1310f, t),
                Lerp(660f, FormationBattleV94Layout.EnemyFrontFootY, t) +
                    EnemyFormationVerticalShift,
                Lerp(850f, FormationBattleV94Layout.EnemyRearFootY, t) +
                    EnemyFormationVerticalShift,
                Lerp(1120f, FormationBattleV94Layout.PlayerFrontFootY, t) +
                    PlayerFormationVerticalShift,
                playerBackFootY,
                startButtonY,
                toolsY,
                buffY,
                benchY - (FormationBattleV94Layout.BenchCenterY -
                    FormationBattleV94Layout.BenchSlotDiameter * 0.5f -
                    FormationBattleV94Layout.BenchLabel.Y),
                benchY);
        }

        private static float Lerp(float from, float to, float t)
        {
            return from + (to - from) * Math.Max(0f, Math.Min(1f, t));
        }
    }
}
