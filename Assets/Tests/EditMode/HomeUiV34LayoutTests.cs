using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class HomeUiV34LayoutTests
    {
        [Test]
        public void ApprovedAnchorsMatchE32MachineLayout()
        {
            Assert.That(HomeUiV34Layout.PlayerProfile.X, Is.EqualTo(0.045f));
            Assert.That(HomeUiV34Layout.SeasonEntry.Y, Is.EqualTo(0.194f));
            Assert.That(HomeUiV34Layout.IslandHero.Y, Is.EqualTo(0.315f));
            Assert.That(HomeUiV34Layout.RepairCard.Y, Is.EqualTo(0.67f));
            Assert.That(HomeUiV34Layout.ExpeditionButton.Y, Is.EqualTo(0.785f));
            Assert.That(HomeUiV34Layout.BottomNavigation.Y + HomeUiV34Layout.BottomNavigation.Height,
                Is.EqualTo(1f).Within(0.0001f));
        }
    }
}
