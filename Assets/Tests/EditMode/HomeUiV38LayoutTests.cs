using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class HomeUiV38LayoutTests
    {
        [Test]
        public void DynamicRectsMatchRuntimeLayerManifest()
        {
            AssertRect(HomeUiV38Layout.PlayerName, 176f, 65f, 130f, 46f);
            AssertRect(HomeUiV38Layout.PlayerPower, 217f, 118f, 152f, 40f);
            AssertRect(HomeUiV38Layout.EnergyValue, 704f, 62f, 40f, 46f);
            AssertRect(HomeUiV38Layout.SkrValue, 851f, 62f, 40f, 46f);
            AssertRect(HomeUiV38Layout.TideValue, 1000f, 62f, 44f, 46f);
            AssertRect(HomeUiV38Layout.Season, 406f, 470f, 270f, 73f);
            AssertRect(HomeUiV38Layout.Chapter, 317f, 546f, 446f, 68f);
            AssertRect(HomeUiV38Layout.IslandName, 457f, 652f, 167f, 51f);
            AssertRect(HomeUiV38Layout.StatusTitle, 375f, 1664f, 333f, 62f);
            AssertRect(HomeUiV38Layout.ExpeditionButton, 186f, 1880f, 708f, 123f);
            AssertRect(HomeUiV38Layout.BottomNavigation, 0f, 2187f, 1080f, 213f);
        }

        [Test]
        public void NavigationSpriteHasFiveContiguousHitSlots()
        {
            for (var index = 0; index < 5; index++)
            {
                var slot = HomeUiV38Layout.BottomNavigationSlot(index);
                Assert.That(slot.X, Is.EqualTo(index * 216f));
                Assert.That(slot.Width, Is.EqualTo(216f));
                Assert.That(slot.Y, Is.EqualTo(2187f));
                Assert.That(slot.Height, Is.EqualTo(213f));
            }
        }

        [TestCase(true, false, false, 5, HomeExpeditionButtonVisualState.Normal)]
        [TestCase(true, true, false, 4, HomeExpeditionButtonVisualState.Continue)]
        [TestCase(true, false, true, 0, HomeExpeditionButtonVisualState.FreeNoReward)]
        [TestCase(false, true, false, 4, HomeExpeditionButtonVisualState.Disabled)]
        public void ExpeditionButtonStateUsesDeliveredSpriteMapping(bool enabled, bool hasProgress,
            bool startsNewRun, int energy, HomeExpeditionButtonVisualState expected)
        {
            Assert.That(HomeUiV38Layout.ResolveExpeditionButtonState(
                enabled, hasProgress, startsNewRun, energy), Is.EqualTo(expected));
        }

        private static void AssertRect(ReferenceUiRect actual, float x, float y, float width, float height)
        {
            Assert.That(actual.X, Is.EqualTo(x));
            Assert.That(actual.Y, Is.EqualTo(y));
            Assert.That(actual.Width, Is.EqualTo(width));
            Assert.That(actual.Height, Is.EqualTo(height));
        }
    }
}
