using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class DetailTideWindowLayoutTests
    {
        [Test]
        public void ReferenceCanvasMatchesFormalV1144Contract()
        {
            var modal = DetailTideWindowLayout.CalculateModal(1080f, 2400f, 0f, 0f);

            Assert.That(modal.X, Is.EqualTo(100f));
            Assert.That(modal.Y, Is.EqualTo(470f));
            Assert.That(modal.Width, Is.EqualTo(880f));
            Assert.That(modal.Height, Is.EqualTo(1360f));
        }

        [Test]
        public void OnlyForegroundModalRespondsToSafeArea()
        {
            var modal = DetailTideWindowLayout.CalculateModal(1080f, 2400f, 120f, 40f);

            Assert.That(modal.X, Is.EqualTo(100f));
            Assert.That(modal.Y, Is.GreaterThanOrEqualTo(184f));
            Assert.That(modal.Y + modal.Height, Is.LessThanOrEqualTo(2296f));
        }

        [TestCase(CombatSpeedTier.Slow, 8000, "0.80")]
        [TestCase(CombatSpeedTier.Standard, 10000, "1.00")]
        [TestCase(CombatSpeedTier.Fast, 12000, "1.20")]
        public void AttackSpeedUsesContinuousBasisPointPresentation(
            CombatSpeedTier tier, int expectedBasisPoints, string expectedText)
        {
            Assert.That(DetailTideWindowLayout.AttackSpeedBasisPoints(tier), Is.EqualTo(expectedBasisPoints));
            Assert.That(DetailTideWindowLayout.FormatAttackSpeed(tier), Is.EqualTo(expectedText));
        }

    }
}
