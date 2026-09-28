using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ExpeditionEnergyRulesTests
    {
        [Test]
        public void RewardedExpeditionConsumesOneEnergy()
        {
            var result = ExpeditionEnergyRules.BeginExpedition(5);

            Assert.That(result.RemainingEnergy, Is.EqualTo(4));
            Assert.That(result.RewardsEnabled, Is.True);
        }

        [Test]
        public void ZeroEnergyStillStartsWithoutRewards()
        {
            var result = ExpeditionEnergyRules.BeginExpedition(0);

            Assert.That(result.RemainingEnergy, Is.Zero);
            Assert.That(result.RewardsEnabled, Is.False);
        }

        [Test]
        public void InvalidNegativeEnergyIsTreatedAsZero()
        {
            var result = ExpeditionEnergyRules.BeginExpedition(-3);

            Assert.That(result.RemainingEnergy, Is.Zero);
            Assert.That(result.RewardsEnabled, Is.False);
        }
    }
}
