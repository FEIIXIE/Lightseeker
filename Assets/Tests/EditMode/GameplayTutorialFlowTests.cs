using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class GameplayTutorialFlowTests
    {
        [Test]
        public void FirstVisitOpensOnlyWhenTutorialHasNotBeenSeen()
        {
            var flow = new GameplayTutorialFlow();

            Assert.That(flow.TryOpenFirstVisit(hasSeenTutorial: true), Is.False);
            Assert.That(flow.IsOpen, Is.False);
            Assert.That(flow.TryOpenFirstVisit(hasSeenTutorial: false), Is.True);
            Assert.That(flow.IsOpen, Is.True);
            Assert.That(flow.OpenReason, Is.EqualTo(GameplayTutorialOpenReason.FirstVisit));
        }

        [Test]
        public void ManualEntryAlwaysStartsFromFirstPage()
        {
            var flow = new GameplayTutorialFlow();
            flow.OpenManual();
            flow.TryNextPage();
            flow.Close();

            flow.OpenManual();

            Assert.That(flow.IsOpen, Is.True);
            Assert.That(flow.CurrentPageIndex, Is.Zero);
            Assert.That(flow.OpenReason, Is.EqualTo(GameplayTutorialOpenReason.Manual));
        }

        [Test]
        public void LeftSwipeAdvancesAndRightSwipeReturns()
        {
            var flow = new GameplayTutorialFlow();
            flow.OpenManual();

            Assert.That(flow.HandleHorizontalSwipe(-120f),
                Is.EqualTo(GameplayTutorialSwipeResult.NextPage));
            Assert.That(flow.CurrentPageIndex, Is.EqualTo(1));
            Assert.That(flow.HandleHorizontalSwipe(120f),
                Is.EqualTo(GameplayTutorialSwipeResult.PreviousPage));
            Assert.That(flow.CurrentPageIndex, Is.Zero);
        }

        [Test]
        public void ShortSwipeAndBoundarySwipeDoNotChangePage()
        {
            var flow = new GameplayTutorialFlow();
            flow.OpenManual();

            Assert.That(flow.HandleHorizontalSwipe(-80f), Is.EqualTo(GameplayTutorialSwipeResult.None));
            Assert.That(flow.HandleHorizontalSwipe(120f), Is.EqualTo(GameplayTutorialSwipeResult.None));
            for (var index = 1; index < GameplayTutorialFlow.PageCount; index++)
                Assert.That(flow.TryNextPage(), Is.True);
            Assert.That(flow.IsLastPage, Is.True);
            Assert.That(flow.HandleHorizontalSwipe(-120f), Is.EqualTo(GameplayTutorialSwipeResult.None));
        }

        [Test]
        public void ClosingFirstVisitRequestsSeenPersistenceButManualCloseDoesNot()
        {
            var firstVisit = new GameplayTutorialFlow();
            firstVisit.TryOpenFirstVisit(hasSeenTutorial: false);
            Assert.That(firstVisit.Close(), Is.EqualTo(GameplayTutorialCloseResult.PersistSeen));
            Assert.That(firstVisit.IsOpen, Is.False);
            Assert.That(firstVisit.CurrentPageIndex, Is.Zero);

            var manual = new GameplayTutorialFlow();
            manual.OpenManual();
            Assert.That(manual.Close(), Is.EqualTo(GameplayTutorialCloseResult.None));
        }

        [Test]
        public void CatalogMatchesFlowAndHasCompleteBilingualCopy()
        {
            Assert.That(GameplayTutorialCatalog.Pages.Count,
                Is.EqualTo(GameplayTutorialFlow.PageCount));
            foreach (var page in GameplayTutorialCatalog.Pages)
            {
                Assert.That(page.Id, Is.Not.Empty);
                Assert.That(page.ChineseTitle, Is.Not.Empty);
                Assert.That(page.EnglishTitle, Is.Not.Empty);
                Assert.That(page.ChineseBody, Is.Not.Empty);
                Assert.That(page.EnglishBody, Is.Not.Empty);
                Assert.That(page.ChineseAction, Is.Not.Empty);
                Assert.That(page.EnglishAction, Is.Not.Empty);
            }
        }
    }
}
