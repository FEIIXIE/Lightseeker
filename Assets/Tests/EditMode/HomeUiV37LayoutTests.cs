using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class HomeUiV37LayoutTests
    {
        [Test]
        public void LockedCanvasMatchesSeekerMaster()
        {
            Assert.That(HomeUiV37Layout.Version, Is.EqualTo("3.7"));
            Assert.That(HomeUiV37Layout.ReferenceWidth, Is.EqualTo(1080f));
            Assert.That(HomeUiV37Layout.ReferenceHeight, Is.EqualTo(2400f));
        }

        [Test]
        public void InteractiveRegionsMatchLockedManifest()
        {
            AssertRect(HomeUiV37Layout.Profile, 0.0428f, 0.0214f, 0.3330f, 0.0674f);
            AssertRect(HomeUiV37Layout.Affairs, 0.7158f, 0.0615f, 0.1034f, 0.0578f);
            AssertRect(HomeUiV37Layout.Warehouse, 0.8300f, 0.0615f, 0.1118f, 0.0578f);
            AssertRect(HomeUiV37Layout.Season, 0.2889f, 0.1882f, 0.4210f, 0.0802f);
            AssertRect(HomeUiV37Layout.Island, 0.1998f, 0.3096f, 0.6005f, 0.3508f);
            AssertRect(HomeUiV37Layout.StatusCard, 0.1736f, 0.6658f, 0.6504f, 0.1086f);
            AssertRect(HomeUiV37Layout.Expedition, 0.1736f, 0.7834f, 0.6504f, 0.0529f);
            AssertRect(HomeUiV37Layout.BottomNavigation, 0f, 0.9107f, 1f, 0.0893f);
        }

        [Test]
        public void BottomNavigationSplitsIntoFiveContiguousTransparentHitAreas()
        {
            for (var i = 0; i < 5; i++)
            {
                var slot = HomeUiV37Layout.BottomNavigationSlot(i);
                Assert.That(slot.X, Is.EqualTo(i * 0.2f).Within(0.0001f));
                Assert.That(slot.Width, Is.EqualTo(0.2f).Within(0.0001f));
                Assert.That(slot.Y, Is.EqualTo(HomeUiV37Layout.BottomNavigation.Y));
                Assert.That(slot.Height, Is.EqualTo(HomeUiV37Layout.BottomNavigation.Height));
            }
        }

        private static void AssertRect(NormalizedUiRect actual, float x, float y, float width, float height)
        {
            Assert.That(actual.X, Is.EqualTo(x));
            Assert.That(actual.Y, Is.EqualTo(y));
            Assert.That(actual.Width, Is.EqualTo(width));
            Assert.That(actual.Height, Is.EqualTo(height));
        }
    }
}
