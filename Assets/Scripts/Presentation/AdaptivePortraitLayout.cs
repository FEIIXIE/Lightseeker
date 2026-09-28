using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Presentation
{
    public struct AdaptivePortraitMetrics
    {
        public readonly float Scale;
        public readonly float CanvasHeight;
        public readonly float SafeTop;
        public readonly float SafeBottom;
        public readonly float SafeContentHeight;

        public AdaptivePortraitMetrics(
            float scale, float canvasHeight, float safeTop, float safeBottom, float safeContentHeight)
        {
            Scale = scale;
            CanvasHeight = canvasHeight;
            SafeTop = safeTop;
            SafeBottom = safeBottom;
            SafeContentHeight = safeContentHeight;
        }
    }

    public struct AdaptiveHomeMiddleLayout
    {
        public readonly float Scale;
        public readonly float Top;

        public AdaptiveHomeMiddleLayout(float scale, float top)
        {
            Scale = scale;
            Top = top;
        }
    }

    /// <summary>
    /// Keeps the established 1080-wide design coordinate system while allowing the
    /// vertical canvas to follow the actual portrait window. Backgrounds can cover the
    /// complete window; controls use SafeTop/SafeBottom and remain inside Screen.safeArea.
    /// </summary>
    public static class AdaptivePortraitLayout
    {
        public static AdaptivePortraitMetrics Calculate(
            float screenWidth, float screenHeight, Rect safeArea, float designWidth)
        {
            var result = AdaptivePortraitLayoutMath.Calculate(
                screenWidth, screenHeight, safeArea.x, safeArea.y, safeArea.width, safeArea.height, designWidth);
            return new AdaptivePortraitMetrics(result.Scale, result.CanvasHeight, result.SafeTop,
                result.SafeBottom, result.SafeContentHeight);
        }

        public static float BottomAnchoredY(float safeContentHeight, float elementHeight, float margin)
        {
            return AdaptivePortraitLayoutMath.BottomAnchoredY(safeContentHeight, elementHeight, margin);
        }

        public static float FlexibleMiddleOffset(float safeContentHeight, float referenceHeight, float share)
        {
            return Mathf.Max(0f, safeContentHeight - referenceHeight) * Mathf.Clamp01(share);
        }

        public static float ExpandedPageOffset(float safeContentHeight, float share)
        {
            return AdaptivePortraitLayoutMath.ExpandedPageOffset(safeContentHeight, share);
        }

        public static AdaptiveHomeMiddleLayout CalculateHomeMiddle(float safeContentHeight)
        {
            var result = AdaptivePortraitLayoutMath.CalculateHomeMiddle(safeContentHeight);
            return new AdaptiveHomeMiddleLayout(result.Scale, result.Top);
        }

        public static Rect ScaleHomeMiddleRect(Rect referenceRect, AdaptiveHomeMiddleLayout layout)
        {
            const float referenceTop = 216f;
            const float centerX = 540f;
            return new Rect(
                centerX + (referenceRect.x - centerX) * layout.Scale,
                layout.Top + (referenceRect.y - referenceTop) * layout.Scale,
                referenceRect.width * layout.Scale,
                referenceRect.height * layout.Scale);
        }
    }
}
