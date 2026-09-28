using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class ExpeditionArchivePresentationTests
    {
        [Test]
        public void BuffRarityIsStableForTheSameRuntimeState()
        {
            var first = ExpeditionArchivePresentation.SelectBuffRarity(321u, "node.4.2", 4, 1);
            var second = ExpeditionArchivePresentation.SelectBuffRarity(321u, "node.4.2", 4, 1);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void BuffRarityCanReachAllFivePresentationTiers()
        {
            var seen = new bool[5];
            for (uint seed = 1; seed <= 2000; seed++)
                seen[(int)ExpeditionArchivePresentation.SelectBuffRarity(seed, "node", 3, 0)] = true;

            Assert.That(seen, Is.All.True);
        }

        [TestCase("faction.steel", "steel")]
        [TestCase("faction.wild", "wild")]
        [TestCase("faction.starlight", "starlight")]
        [TestCase("faction.unknown", "neutral")]
        public void FactionIdsSelectTheirOwnVisualLanguage(string factionId, string expected)
        {
            Assert.That(ExpeditionArchivePresentation.FactionPresentationKey(factionId), Is.EqualTo(expected));
        }
    }
}
