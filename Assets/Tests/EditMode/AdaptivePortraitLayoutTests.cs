using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class AdaptivePortraitLayoutTests
    {
        [TestCase(1080f, 1920f, 1920f)]
        [TestCase(1080f, 2400f, 2400f)]
        [TestCase(1200f, 2670f, 2403f)]
        public void CanvasHeightFollowsWindowAspect(float width, float height, float expectedHeight)
        {
            var metrics = AdaptivePortraitLayoutMath.Calculate(
                width, height, 0f, 0f, width, height, 1080f);

            Assert.That(metrics.CanvasHeight, Is.EqualTo(expectedHeight).Within(0.01f));
            Assert.That(metrics.SafeContentHeight, Is.EqualTo(expectedHeight).Within(0.01f));
        }

        [Test]
        public void SafeInsetsAreConvertedIntoDesignCoordinates()
        {
            var metrics = AdaptivePortraitLayoutMath.Calculate(
                1200f, 2670f, 0f, 90f, 1200f, 2500f, 1080f);

            Assert.That(metrics.SafeTop, Is.EqualTo(72f).Within(0.01f));
            Assert.That(metrics.SafeBottom, Is.EqualTo(81f).Within(0.01f));
            Assert.That(metrics.SafeContentHeight, Is.EqualTo(2250f).Within(0.01f));
        }

        [Test]
        public void BottomAnchorAndMiddleOffsetRemainStableAtReferenceHeight()
        {
            Assert.That(AdaptivePortraitLayoutMath.BottomAnchoredY(1920f, 224f, 0f), Is.EqualTo(1696f));
            Assert.That(AdaptivePortraitLayoutMath.ExpandedPageOffset(1920f, 0.5f), Is.EqualTo(0f));
            Assert.That(AdaptivePortraitLayoutMath.ExpandedPageOffset(2400f, 0.5f), Is.EqualTo(240f));
            Assert.That(AdaptivePortraitLayoutMath.ExpandedPageOffset(2400f, 1f), Is.EqualTo(480f));
        }

        [Test]
        public void HomeMiddleUsesFormalFortyByEightyNineCoordinates()
        {
            var middle = AdaptivePortraitLayoutMath.CalculateHomeMiddle(2403f);

            Assert.That(middle.Scale, Is.EqualTo(1f));
            Assert.That(middle.Top, Is.EqualTo(216f));
        }

        [Test]
        public void HomeMiddleCompressesWithoutStretchingOnNineBySixteen()
        {
            var middle = AdaptivePortraitLayoutMath.CalculateHomeMiddle(1920f);
            var islandWidth = 700f * middle.Scale;
            var islandHeight = 980f * middle.Scale;
            var actionYMax = middle.Top + (1852f - 216f) * middle.Scale;

            Assert.That(middle.Scale, Is.LessThan(1f));
            Assert.That(islandWidth / islandHeight, Is.EqualTo(700f / 980f).Within(0.0001f));
            Assert.That(actionYMax, Is.LessThanOrEqualTo(1626f));
        }

        [Test]
        public void FullCanvasPageFit_DoesNotInheritSafeAreaTranslation()
        {
            var fit = AdaptivePortraitLayoutMath.CalculateFullCanvasFit(1080f, 2403f, 841f, 1870f);

            Assert.That(fit.Scale, Is.EqualTo(1080f / 841f).Within(0.0001f));
            Assert.That(fit.OffsetX, Is.EqualTo(0f).Within(0.01f));
            Assert.That(fit.OffsetY, Is.LessThan(2f));
        }
    }
}
