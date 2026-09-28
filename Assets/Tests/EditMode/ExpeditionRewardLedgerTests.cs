using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ExpeditionRewardLedgerTests
    {
        [Test]
        public void BranchChoice_RecordsOnlyTheCompletedTargetOnce()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(19u);
            ExpeditionMapNode branchSource = null;
            foreach (var node in map.Nodes)
            {
                if (node.NextNodeIds.Count < 2) continue;
                branchSource = node;
                break;
            }
            Assert.That(branchSource, Is.Not.Null);

            var chosenNodeId = branchSource.NextNodeIds[0];
            var ledger = new ExpeditionRewardLedger();
            Assert.That(ledger.TryRecordCompletedNode(chosenNodeId, 3, 1, 0, 0), Is.True);
            Assert.That(ledger.TryRecordCompletedNode(chosenNodeId, 3, 1, 0, 0), Is.False);
            Assert.That(ledger.CompletedNodeIds, Is.EqualTo(new[] { chosenNodeId }));
            Assert.That(ledger.TideCrystals, Is.EqualTo(3));
            Assert.That(ledger.Stone, Is.EqualTo(1));

            foreach (var candidateId in branchSource.NextNodeIds)
            {
                if (candidateId == chosenNodeId) continue;
                Assert.That(ledger.CompletedNodeIds, Does.Not.Contain(candidateId));
            }
        }

        [Test]
        public void Restore_PreservesCompletedNodeAuditAndRejectsReplay()
        {
            var ledger = new ExpeditionRewardLedger();
            ledger.Restore(5, 2, 1, 0, new string[0],
                new[] { "chapter1-3-1" }, false);

            Assert.That(ledger.TryRecordCompletedNode(
                "chapter1-3-1", 5, 2, 1, 0), Is.False);
            Assert.That(ledger.TideCrystals, Is.EqualTo(5));
            Assert.That(ledger.CompletedNodeIds, Is.EqualTo(new[] { "chapter1-3-1" }));
        }
    }
}
