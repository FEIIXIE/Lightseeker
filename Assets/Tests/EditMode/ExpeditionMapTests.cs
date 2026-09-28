using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ExpeditionMapTests
    {
        [Test]
        public void ChapterOne_IsAConnectedFifteenLayerGraph()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(12345u);

            Assert.That(map.IsValidChapterOne(), Is.True);
            Assert.That(ExpeditionMapGraph.ChapterLayerCount, Is.EqualTo(15));
            Assert.That(map.GetLayer(1)[0].Type, Is.EqualTo(ExpeditionNodeType.NormalBattle));
            Assert.That(map.GetLayer(ExpeditionMapGraph.ChapterLayerCount)[0].Type,
                Is.EqualTo(ExpeditionNodeType.Boss));
            Assert.That(map.GetNode("M19").Type, Is.EqualTo(ExpeditionNodeType.EliteBattle));
            Assert.That(map.GetNode("M28").Type, Is.EqualTo(ExpeditionNodeType.NormalBattle));
            Assert.That(map.GetNode("M29").Type, Is.EqualTo(ExpeditionNodeType.Boss));
        }

        [Test]
        public void SameSeed_RecreatesNodesAndConnections()
        {
            var first = ExpeditionMapGraph.CreateChapterOne(987654u);
            var second = ExpeditionMapGraph.CreateChapterOne(987654u);

            Assert.That(second.Nodes.Count, Is.EqualTo(first.Nodes.Count));
            for (var index = 0; index < first.Nodes.Count; index++)
            {
                Assert.That(second.Nodes[index].Id, Is.EqualTo(first.Nodes[index].Id));
                Assert.That(second.Nodes[index].Type, Is.EqualTo(first.Nodes[index].Type));
                Assert.That(second.Nodes[index].NextNodeIds,
                    Is.EqualTo(first.Nodes[index].NextNodeIds));
            }
        }

        [Test]
        public void AvailableChoices_AlwaysMoveStrictlyRight()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(42u);
            var current = map.GetLayer(4)[0];

            var choices = map.GetAvailableNext(current.Id);

            Assert.That(choices, Is.Not.Empty);
            foreach (var choice in choices)
            {
                Assert.That(choice.JourneyProgress, Is.GreaterThan(current.JourneyProgress));
            }
        }

        [Test]
        public void GeneratedSeeds_RespectE21TopologyConstraints()
        {
            var expectedRankWidths = new[] { 1, 2, 3, 2, 3, 2, 2, 4, 1, 2, 2, 2, 2, 1, 1 };
            for (uint seed = 1; seed <= 256; seed++)
            {
                var map = ExpeditionMapGraph.CreateChapterOne(seed);
                Assert.That(map.ValidateChapterOne(), Is.Empty,
                    "Seed " + seed + " produced an invalid graph.");

                for (var layer = 1; layer <= ExpeditionMapGraph.ChapterLayerCount; layer++)
                {
                    var nodes = map.GetLayer(layer);
                    Assert.That(nodes.Count, Is.EqualTo(expectedRankWidths[layer - 1]));
                    foreach (var node in nodes)
                    {
                        if (layer == ExpeditionMapGraph.ChapterLayerCount)
                            Assert.That(node.NextNodeIds, Is.Empty);
                        else
                            Assert.That(node.NextNodeIds.Count,
                                Is.InRange(1, ExpeditionMapGraph.MaximumOrdinaryOutDegree));
                        foreach (var nextId in node.NextNodeIds)
                            Assert.That(map.GetNode(nextId).JourneyProgress,
                                Is.GreaterThan(node.JourneyProgress));
                    }
                }
                Assert.That(map.GetNode("M29").NextNodeIds, Is.Empty);
                Assert.That(map.GetNode("M28").NextNodeIds, Is.EqualTo(new[] { "M29" }));
            }
        }

        [Test]
        public void Generator_SelectsReviewedSkeletonsInGoldenOrder()
        {
            Assert.That(ExpeditionMapGraph.CreateChapterOne(1u).SkeletonId, Is.EqualTo("A"));
            Assert.That(ExpeditionMapGraph.CreateChapterOne(2u).SkeletonId, Is.EqualTo("B"));
            Assert.That(ExpeditionMapGraph.CreateChapterOne(3u).SkeletonId, Is.EqualTo("C"));
            Assert.That(ExpeditionMapGraph.CreateChapterOne(4u).SkeletonId, Is.EqualTo("D"));
            Assert.That(ExpeditionMapGraph.CreateChapterOne(5u).SkeletonId, Is.EqualTo("A"));
        }
    }
}
