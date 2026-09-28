using System;

namespace SeekerAutoChess.Core
{
    public struct AdaptivePortraitMetricsData
    {
        public readonly float Scale;
        public readonly float CanvasHeight;
        public readonly float SafeTop;
        public readonly float SafeBottom;
        public readonly float SafeContentHeight;

        public AdaptivePortraitMetricsData(
            float scale, float canvasHeight, float safeTop, float safeBottom, float safeContentHeight)
        {
            Scale = scale;
            CanvasHeight = canvasHeight;
            SafeTop = safeTop;
            SafeBottom = safeBottom;
            SafeContentHeight = safeContentHeight;
        }
    }

    public struct AdaptiveHomeMiddleData
    {
        public readonly float Scale;
        public readonly float Top;

        public AdaptiveHomeMiddleData(float scale, float top)
        {
            Scale = scale;
            Top = top;
        }
    }

    public struct AdaptivePageFitData
    {
        public readonly float Scale;
        public readonly float OffsetX;
        public readonly float OffsetY;

        public AdaptivePageFitData(float scale, float offsetX, float offsetY)
        {
            Scale = scale;
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }

    public static class AdaptivePortraitLayoutMath
    {
        public const float LegacyPageHeight = 1920f;

        public static AdaptivePortraitMetricsData Calculate(
            float screenWidth, float screenHeight, float safeX, float safeY,
            float safeWidth, float safeHeight, float designWidth)
        {
            var width = Math.Max(1f, screenWidth);
            var height = Math.Max(1f, screenHeight);
            var scale = width / Math.Max(1f, designWidth);
            var clampedSafeX = Clamp(safeX, 0f, width);
            var clampedSafeY = Clamp(safeY, 0f, height);
            var clampedSafeWidth = Clamp(safeWidth, 0f, width - clampedSafeX);
            var clampedSafeHeight = Clamp(safeHeight, 0f, height - clampedSafeY);
            if (clampedSafeWidth <= 0f || clampedSafeHeight <= 0f)
            {
                clampedSafeY = 0f;
                clampedSafeHeight = height;
            }

            var canvasHeight = height / scale;
            var safeTop = Math.Max(0f, height - (clampedSafeY + clampedSafeHeight)) / scale;
            var safeBottom = Math.Max(0f, clampedSafeY) / scale;
            return new AdaptivePortraitMetricsData(
                scale, canvasHeight, safeTop, safeBottom,
                Math.Max(1f, canvasHeight - safeTop - safeBottom));
        }

        public static AdaptiveHomeMiddleData CalculateHomeMiddle(float safeContentHeight)
        {
            const float referenceHeight = 2403f;
            const float referenceTop = 216f;
            const float referenceEnd = 1852f;
            const float dockHeight = 224f;
            const float shortScreenBottomGap = 70f;
            if (safeContentHeight >= referenceHeight)
            {
                return new AdaptiveHomeMiddleData(1f, referenceTop);
            }

            var top = safeContentHeight < 2200f ? 190f : referenceTop;
            var availableEnd = safeContentHeight - dockHeight - shortScreenBottomGap;
            var scale = Clamp((availableEnd - top) / (referenceEnd - referenceTop), 0.72f, 1f);
            return new AdaptiveHomeMiddleData(scale, top);
        }

        public static float BottomAnchoredY(float safeContentHeight, float elementHeight, float margin)
        {
            return Math.Max(0f, safeContentHeight - elementHeight - Math.Max(0f, margin));
        }

        public static float ExpandedPageOffset(float safeContentHeight, float share)
        {
            return Math.Max(0f, safeContentHeight - LegacyPageHeight) * Clamp(share, 0f, 1f);
        }

        public static AdaptivePageFitData CalculateFullCanvasFit(
            float canvasWidth, float canvasHeight, float referenceWidth, float referenceHeight)
        {
            var safeCanvasWidth = Math.Max(1f, canvasWidth);
            var safeCanvasHeight = Math.Max(1f, canvasHeight);
            var safeReferenceWidth = Math.Max(1f, referenceWidth);
            var safeReferenceHeight = Math.Max(1f, referenceHeight);
            var scale = Math.Min(safeCanvasWidth / safeReferenceWidth,
                safeCanvasHeight / safeReferenceHeight);
            return new AdaptivePageFitData(scale,
                (safeCanvasWidth - safeReferenceWidth * scale) * 0.5f,
                (safeCanvasHeight - safeReferenceHeight * scale) * 0.5f);
        }

        private static float Clamp(float value, float minimum, float maximum)
        {
            return Math.Max(minimum, Math.Min(maximum, value));
        }
    }
}
