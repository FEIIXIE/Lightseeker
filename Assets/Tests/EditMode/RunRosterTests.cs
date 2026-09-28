using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class RunRosterTests
    {
        [Test]
        public void NineEqualUnitsOnlyMergeWhenDraggedIntoFourTriples()
        {
            var roster = new RunRoster(boardCapacity: 6, benchCapacity: 6);
            for (var i = 0; i < 9; i++)
            {
                Assert.That(roster.TryAdd(new RunUnitState("guardian", 4)).Added, Is.True);
            }
            Assert.That(roster.TotalCount, Is.EqualTo(9));
            Assert.That(roster.TryManualMerge(0, 1, out _), Is.True);
            Assert.That(roster.TryManualMerge(3, 4, out _), Is.True);
            Assert.That(roster.TryManualMerge(6, 7, out _), Is.True);
            Assert.That(roster.TryManualMerge(1, 4, out var finalLevel), Is.True);
            Assert.That(roster.TotalCount, Is.EqualTo(1));
            Assert.That(finalLevel, Is.EqualTo(3));
            Assert.That(roster.Get(4).RunLevel, Is.EqualTo(3));
        }

        [Test]
        public void OverflowUsesBenchAndCanSwapWithBoard()
        {
            var roster = new RunRoster(boardCapacity: 2, benchCapacity: 2);
            roster.TryAdd(new RunUnitState("guardian", 4));
            roster.TryAdd(new RunUnitState("archer", 5));
            roster.TryAdd(new RunUnitState("healer", 3));

            Assert.That(roster.BoardCount, Is.EqualTo(2));
            Assert.That(roster.Get(2).DefinitionId, Is.EqualTo("healer"));
            Assert.That(roster.TryMoveOrSwap(2, 0), Is.True);
            Assert.That(roster.Get(0).DefinitionId, Is.EqualTo("healer"));
            Assert.That(roster.Get(2).DefinitionId, Is.EqualTo("guardian"));
        }

        [Test]
        public void ManualMergeRequiresThreeCopiesAndSaleUsesCumulativeCost()
        {
            var roster = new RunRoster(boardCapacity: 2, benchCapacity: 2);
            roster.TryAdd(new RunUnitState("guardian", 4, acquisitionBasis: 3));
            roster.TryAdd(new RunUnitState("guardian", 4, acquisitionBasis: 5));
            Assert.That(roster.TryManualMerge(0, 1, out _), Is.False);

            roster.TryAdd(new RunUnitState("guardian", 4, acquisitionBasis: 3));
            Assert.That(roster.TryManualMerge(0, 1, out var finalLevel), Is.True);
            Assert.That(finalLevel, Is.EqualTo(2));
            Assert.That(roster.TotalCount, Is.EqualTo(1));
            Assert.That(roster.Get(1).AcquisitionBasis, Is.EqualTo(11));

            Assert.That(roster.TrySellAt(1, out var refund), Is.True);
            Assert.That(refund, Is.EqualTo(5));
            Assert.That(roster.TotalCount, Is.EqualTo(0));
        }

        [Test]
        public void FullRosterPurchaseOnlySucceedsWhenItImmediatelyMerges()
        {
            var roster = new RunRoster(boardCapacity: 2, benchCapacity: 2);
            roster.TryRestoreAt(0, new RunUnitState("guardian", 4, acquisitionBasis: 3, runUnitId: "u0"));
            roster.TryRestoreAt(1, new RunUnitState("archer", 5, acquisitionBasis: 3, runUnitId: "u1"));
            roster.TryRestoreAt(2, new RunUnitState("guardian", 4, acquisitionBasis: 5, runUnitId: "u2"));
            roster.TryRestoreAt(3, new RunUnitState("healer", 3, acquisitionBasis: 3, runUnitId: "u3"));

            Assert.That(roster.CanPurchase(new RunUnitState("healer", 3, acquisitionBasis: 3)), Is.False);
            Assert.That(roster.TryPurchase(new RunUnitState("healer", 3, acquisitionBasis: 3)).Added, Is.False);
            Assert.That(roster.TotalCount, Is.EqualTo(4));

            var result = roster.TryPurchase(new RunUnitState("guardian", 4, acquisitionBasis: 2,
                runUnitId: "new"));
            Assert.That(result.Added, Is.True);
            Assert.That(result.MergeCount, Is.EqualTo(1));
            Assert.That(result.FinalIndex, Is.EqualTo(0), "Lowest deployed slot must be retained.");
            Assert.That(roster.Get(0).RunLevel, Is.EqualTo(2));
            Assert.That(roster.Get(0).AcquisitionBasis, Is.EqualTo(10));
            Assert.That(roster.Get(0).RunUnitId, Is.EqualTo("u0"));
            Assert.That(roster.TotalCount, Is.EqualTo(3));
        }

        [Test]
        public void ManualMergeUsesStableThirdSlotAndKeepsDestinationIdentity()
        {
            var roster = new RunRoster(boardCapacity: 2, benchCapacity: 3);
            roster.TryRestoreAt(0, new RunUnitState("guardian", 4, acquisitionBasis: 3, runUnitId: "source"));
            roster.TryRestoreAt(1, new RunUnitState("guardian", 4, acquisitionBasis: 5, runUnitId: "target"));
            roster.TryRestoreAt(2, new RunUnitState("guardian", 4, acquisitionBasis: 7, runUnitId: "third-low"));
            roster.TryRestoreAt(4, new RunUnitState("guardian", 4, acquisitionBasis: 11, runUnitId: "third-high"));

            Assert.That(roster.FindManualMergeThirdIndex(0, 1), Is.EqualTo(2));
            Assert.That(roster.TryManualMerge(0, 1, out var level), Is.True);
            Assert.That(level, Is.EqualTo(2));
            Assert.That(roster.Get(1).RunUnitId, Is.EqualTo("target"));
            Assert.That(roster.Get(1).AcquisitionBasis, Is.EqualTo(15));
            Assert.That(roster.Get(2), Is.Null);
            Assert.That(roster.Get(4), Is.Not.Null);
        }

        [Test]
        public void EconomyLedgerReplaysResultsAndRejectsStaleVersions()
        {
            var ledger = new RunEconomyActionLedger();
            var mutationCount = 0;
            var first = ledger.Execute("buy-1", 0, () =>
            {
                mutationCount++;
                return RunEconomyResultCode.Success;
            });
            var replay = ledger.Execute("buy-1", 0, () =>
            {
                mutationCount++;
                return RunEconomyResultCode.Success;
            });
            var stale = ledger.Execute("sell-1", 0, () =>
            {
                mutationCount++;
                return RunEconomyResultCode.Success;
            });

            Assert.That(first.ResultCode, Is.EqualTo(RunEconomyResultCode.Success));
            Assert.That(first.ResultingVersion, Is.EqualTo(1));
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.ResultDigest, Is.EqualTo(first.ResultDigest));
            Assert.That(stale.ResultCode, Is.EqualTo(RunEconomyResultCode.StaleVersion));
            Assert.That(ledger.StateVersion, Is.EqualTo(1));
            Assert.That(mutationCount, Is.EqualTo(1));
        }

        [Test]
        public void EconomyLedgerRestoreKeepsReplayReceipts()
        {
            var ledger = new RunEconomyActionLedger();
            ledger.Execute("buy-1", 0, () => RunEconomyResultCode.Success);
            ledger.Execute("buy-failed", 1, () => RunEconomyResultCode.InsufficientCoins);

            var restored = new RunEconomyActionLedger();
            restored.Restore(ledger.StateVersion, ledger.CopyActionIds(), ledger.CopyResultCodes(),
                ledger.CopyResultingVersions());
            var replay = restored.Execute("buy-1", 0, () => RunEconomyResultCode.InvalidTarget);
            var failedReplay = restored.Execute("buy-failed", 1, () => RunEconomyResultCode.Success);

            Assert.That(restored.StateVersion, Is.EqualTo(1));
            Assert.That(replay.IsReplay, Is.True);
            Assert.That(replay.ResultCode, Is.EqualTo(RunEconomyResultCode.Success));
            Assert.That(failedReplay.IsReplay, Is.True);
            Assert.That(failedReplay.ResultCode, Is.EqualTo(RunEconomyResultCode.InsufficientCoins));
        }

        [Test]
        public void LockedDeckCopiesInputAndOnlyReturnsItsEntries()
        {
            var source = new[] { "guardian", "archer", "healer" };
            var deck = new RunDeckSnapshot(source);
            source[0] = "tampered";

            Assert.That(deck.Contains("guardian"), Is.True);
            Assert.That(deck.Contains("tampered"), Is.False);
            Assert.That(deck.GetPrototypeOffer(4), Is.EqualTo("archer"));
        }
    }
}
