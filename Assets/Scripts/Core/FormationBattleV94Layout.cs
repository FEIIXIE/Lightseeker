using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Runtime-safe source of truth for the v9.4 "Compass Front" formation and battle pages.
    /// All coordinates use the 1080x2400 design canvas and are shared by both phases.
    /// </summary>
    public static class FormationBattleV94Layout
    {
        public const float CanvasWidth = 1080f;
        public const float CanvasHeight = 2400f;
        public const float TransitionDurationSeconds = 0.24f;
        public const float TransitionEndScale = 0.84f;
        public const int MaximumVisibleSynergies = 5;
        public const int MaximumVisibleBuffs = 5;

        public static readonly float[] Columns = { 205f, 540f, 875f };
        public const float EnemyRearFootY = 690f;
        public const float EnemyFrontFootY = 1030f;
        public const float PlayerFrontFootY = 1330f;
        public const float PlayerRearFootY = 1690f;

        public static readonly UiRect SynergyLabel = new UiRect(56f, 308f, 98f, 56f);
        public static readonly UiRect SynergyPopup = new UiRect(150f, 420f, 560f, 310f);
        public const float SynergyCenterY = 344f;
        public const float SynergyTileSize = 84f;

        public const float BuffSingleRowY = 1916f;
        public const float BuffTileSize = SynergyTileSize;
        public const float StatusTilePitch = 130f;
        public static readonly UiRect BuffSingleRowLabel = new UiRect(56f, 1904f, 100f, 56f);
        public static readonly UiRect BuffPopup = new UiRect(260f, 1555f, 560f, 260f);

        public static readonly UiRect RecycleControl = new UiRect(36f, 2008f, 180f, 108f);
        public static readonly UiRect StartControl = new UiRect(250f, 1996f, 580f, 132f);
        public static readonly UiRect ShopControl = new UiRect(864f, 2008f, 180f, 108f);

        public static readonly UiRect BenchLabel = new UiRect(56f, 2161f, 300f, 48f);
        public static readonly float[] BenchCentersX = { 135f, 297f, 459f, 621f, 783f, 945f };
        public const float BenchCenterY = 2280f;
        public const float BenchSlotDiameter = 120f;

        public static readonly UiPoint[] StatusCenters =
        {
            new UiPoint(180f, 230f),
            new UiPoint(540f, 230f),
            new UiPoint(900f, 230f)
        };

        public static UiPoint UnitFootPosition(bool isPlayer, int slot)
        {
            if (slot < 0 || slot >= 6) throw new ArgumentOutOfRangeException(nameof(slot));
            var column = slot % 3;
            var row = slot / 3;
            var y = isPlayer
                ? (row == 0 ? PlayerFrontFootY : PlayerRearFootY)
                : (row == 0 ? EnemyFrontFootY : EnemyRearFootY);
            return new UiPoint(Columns[column], y);
        }

        public static int BossFormationSlotForEnemyIndex(int enemyIndex)
        {
            if (enemyIndex == 0) return 1;
            var minionSlots = new[] { 0, 2, 3, 5 };
            return enemyIndex > 0 && enemyIndex <= minionSlots.Length ? minionSlots[enemyIndex - 1] : -1;
        }

        public static bool IsBossReservedSlot(int slot)
        {
            return slot == 1 || slot == 4;
        }

        public static IReadOnlyList<UiPoint> CenteredStatusGroupCenters(int count, float centerY)
        {
            count = Math.Max(0, Math.Min(MaximumVisibleBuffs + 1, count));
            var points = new List<UiPoint>(count);
            if (count == 0) return points;
            var startX = CanvasWidth * 0.5f - (count - 1) * StatusTilePitch * 0.5f;
            for (var index = 0; index < count; index++)
                points.Add(new UiPoint(startX + index * StatusTilePitch, centerY));
            return points;
        }

        public static IReadOnlyList<UiPoint> BuffCenters(int count)
        {
            return CenteredStatusGroupCenters(Math.Min(MaximumVisibleBuffs, Math.Max(0, count)),
                BuffSingleRowY);
        }

        public static UiRect BenchSlot(int index)
        {
            if (index < 0 || index >= BenchCentersX.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return new UiRect(BenchCentersX[index] - BenchSlotDiameter * 0.5f,
                BenchCenterY - BenchSlotDiameter * 0.5f,
                BenchSlotDiameter, BenchSlotDiameter);
        }

        public static float FormationOverlayAlpha(float normalizedProgress)
        {
            return 1f - CubicIn(normalizedProgress);
        }

        public static float FormationOverlayScale(float normalizedProgress)
        {
            return 1f + (TransitionEndScale - 1f) * CubicIn(normalizedProgress);
        }

        private static float CubicIn(float value)
        {
            value = Math.Max(0f, Math.Min(1f, value));
            return value * value * value;
        }
    }

    public struct UiPoint
    {
        public readonly float X;
        public readonly float Y;

        public UiPoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    public struct UiRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public UiRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
