using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class BountyTowerPresentationTests
    {
        [Test]
        public void DailyEmptyStateDoesNotInventTickets()
        {
            var snapshot = DailyBountySnapshot.Empty();

            Assert.That(snapshot.Pool, Is.EqualTo("--"));
            Assert.That(snapshot.OwnedTicketCount, Is.EqualTo("--"));
            Assert.That(snapshot.EstimatedSingleTicketWinRate, Is.EqualTo("--"));
            Assert.That(snapshot.DrawCountdown, Is.EqualTo("--:--:--"));
            Assert.That(snapshot.TicketRows, Is.Empty);
            Assert.That(snapshot.CanClaim, Is.False);
        }

        [TestCase("D0", "D0")]
        [TestCase("d1", "D1")]
        [TestCase(" D2 ", "D2")]
        [TestCase("D3", "D3")]
        [TestCase("legacy", "--")]
        [TestCase(null, "--")]
        public void DailySourcesStayInsideFormalContract(string value, string expected)
        {
            Assert.That(DailyBountySourceCodes.Normalize(value), Is.EqualTo(expected));
        }

        [TestCase("D0", "leaderboard.formal.source.d0")]
        [TestCase("D1", "leaderboard.formal.source.d1")]
        [TestCase("D2", "leaderboard.formal.source.d2")]
        [TestCase("D3", "leaderboard.formal.source.d3")]
        [TestCase("other", "leaderboard.formal.source.unknown")]
        public void DailySourcesMapToStableLocalizationKeys(string value, string expected)
        {
            Assert.That(DailyBountySourceCodes.LocalizationKey(value), Is.EqualTo(expected));
        }
    }
}
