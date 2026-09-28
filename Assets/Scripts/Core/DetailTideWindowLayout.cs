using System;

namespace SeekerAutoChess.Core
{
    public struct DetailTideWindowRect
    {
        public readonly float X;
        public readonly float Y;
        public readonly float Width;
        public readonly float Height;

        public DetailTideWindowRect(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public static class DetailTideWindowLayout
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 2400f;
        public const float ModalWidth = 880f;
        public const float ModalHeight = 1360f;
        public const float ModalReferenceOffsetY = -50f;
        public const float MinimumSideMargin = 64f;

        public static DetailTideWindowRect CalculateModal(
            float canvasWidth, float canvasHeight, float safeTop, float safeBottom)
        {
            var width = Math.Max(1f, canvasWidth);
            var height = Math.Max(1f, canvasHeight);
            var top = Math.Max(0f, safeTop);
            var bottom = Math.Max(0f, safeBottom);
            var safeHeight = Math.Max(1f, height - top - bottom);
            var x = (width - ModalWidth) * 0.5f;
            var y = top + (safeHeight - ModalHeight) * 0.5f + ModalReferenceOffsetY;

            if (safeHeight >= ModalHeight + MinimumSideMargin * 2f)
            {
                var minimumY = top + MinimumSideMargin;
                var maximumY = top + safeHeight - MinimumSideMargin - ModalHeight;
                y = Math.Max(minimumY, Math.Min(maximumY, y));
            }

            return new DetailTideWindowRect(x, y, ModalWidth, ModalHeight);
        }

        public static int AttackSpeedBasisPoints(CombatSpeedTier tier)
        {
            switch (tier)
            {
                case CombatSpeedTier.Slow: return 8000;
                case CombatSpeedTier.Fast: return 12000;
                default: return 10000;
            }
        }

        public static string FormatAttackSpeed(CombatSpeedTier tier)
        {
            return (AttackSpeedBasisPoints(tier) / 10000d).ToString("0.00",
                System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
