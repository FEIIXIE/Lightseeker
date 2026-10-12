using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ExpeditionMapDiagnosticsTests
    {
        [Test]
        public void FourThousandSeeds_KeepThirtyAnchorsConnectedToUniqueBoss()
        {
            for (uint seed = 1; seed <= 4096; seed++)
            {
                var map = ExpeditionMapGraph.CreateChapterOne(seed);
                Assert.That(map.Nodes.Count, Is.EqualTo(30), "seed=" + seed);
                Assert.That(map.ValidateChapterOne(), Is.Empty, "seed=" + seed);
                Assert.That(map.GetNode("M19").Type, Is.EqualTo(ExpeditionNodeType.EliteBattle));
                Assert.That(map.GetNode("M28").NextNodeIds, Is.EqualTo(new[] { "M29" }));
                Assert.That(map.GetNode("M29").NextNodeIds, Is.Empty);
            }
        }

        [Test]
        public void SameSeedAndVersion_RecreateIdenticalLockedMap()
        {
            foreach (var scenario in ExpeditionMapDiagnostics.FixedTestSeeds)
            {
                var first = ExpeditionMapGraph.CreateChapterOne(scenario.Seed);
                var repeated = ExpeditionMapGraph.CreateChapterOne(scenario.Seed);
                Assert.That(ExpeditionMapDiagnostics.DeterministicSignature(repeated),
                    Is.EqualTo(ExpeditionMapDiagnostics.DeterministicSignature(first)), scenario.Scenario);
            }
        }

        [Test]
        public void FixedSeeds_CoverAllSkeletonsAndDifferentContent()
        {
            var skeletons = new System.Collections.Generic.HashSet<string>();
            var signatures = new System.Collections.Generic.HashSet<string>();
            foreach (var scenario in ExpeditionMapDiagnostics.FixedTestSeeds)
            {
                var map = ExpeditionMapGraph.CreateChapterOne(scenario.Seed);
                skeletons.Add(map.SkeletonId);
                signatures.Add(ExpeditionMapDiagnostics.DeterministicSignature(map));
                var range = ExpeditionMapDiagnostics.AnalyzeRoutes(map);
                Assert.That(range.MinimumSelections, Is.InRange(13, 18), scenario.Scenario);
                Assert.That(range.MaximumSelections, Is.InRange(13, 18), scenario.Scenario);
            }
            Assert.That(skeletons, Is.EquivalentTo(new[] { "A", "B", "C", "D" }));
            Assert.That(signatures.Count, Is.EqualTo(ExpeditionMapDiagnostics.FixedTestSeeds.Count));
        }

        [Test]
        public void RevealWindow_ShowsOnlyThreeForwardRouteChoices()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(2u);
            var visible = ExpeditionMapDiagnostics.GetVisibleNodeIds(map, "M00");
            Assert.That(ExpeditionMapDiagnostics.MapVisibilityRulesVersion, Is.EqualTo(2));
            Assert.That(ExpeditionMapDiagnostics.VisibleRouteDepth, Is.EqualTo(3));
            Assert.That(visible.Contains("M00"), Is.True);
            foreach (var node in map.Nodes)
            {
                if (node.Layer > 4) Assert.That(visible.Contains(node.Id), Is.False, node.Id);
            }
        }

        [Test]
        public void NodeCompletion_RemainsIdempotentAcrossRestoreAndRetry()
        {
            var firstLedger = new ExpeditionRewardLedger();
            Assert.That(ExpeditionProgression.TryCompleteNode(
                firstLedger, "M01", 1), Is.True);
            Assert.That(ExpeditionProgression.TryCompleteNode(
                firstLedger, "M01", 1), Is.False);

            var restored = new ExpeditionRewardLedger();
            restored.Restore(1, new string[0], firstLedger.CompletedNodeIds, false);
            Assert.That(ExpeditionProgression.TryCompleteNode(
                restored, "M01", 1), Is.False);
            Assert.That(ExpeditionProgression.TryCompleteNode(
                restored, "M02", 1), Is.True);
        }

        [Test]
        public void TraversedEdges_FollowActualCompletionOrderAndCurrentLeg()
        {
            var completed = new[] { "M00", "M02", "M05" };
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                completed, "M08", "M00", "M02"), Is.True);
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                completed, "M08", "M02", "M05"), Is.True);
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                completed, "M08", "M05", "M08"), Is.True);
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                completed, "M08", "M02", "M08"), Is.False);
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                completed, "M08", "M05", "M09"), Is.False);
        }

        [Test]
        public void TraversedEdges_IncludesOriginLegAlthoughOriginIsNotACompletedEncounter()
        {
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                new string[0], "M01", "M00", "M01"), Is.True);
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                new[] { "M01", "M03" }, "M06", "M00", "M01"), Is.True);
            Assert.That(ExpeditionProgression.IsTraversedEdge(
                new[] { "M02", "M05" }, "M07", "M00", "M01"), Is.False);
        }

        [Test]
        public void DiagnosticSummary_ContainsRequiredReproductionFields()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(2u);
            var summary = ExpeditionMapDiagnostics.CreateSummary(map, "M00", 123u);
            StringAssert.Contains("mapSeed=2", summary);
            StringAssert.Contains("mapGenerationVersion=9", summary);
            StringAssert.Contains("routeSkeleton=B", summary);
            StringAssert.Contains("currentNodeId=M00", summary);
            StringAssert.Contains("journeyProgress=0.000", summary);
            StringAssert.DoesNotContain("fogTick=", summary);
            StringAssert.Contains("mapVisibilityRulesVersion=2", summary);
            StringAssert.Contains("visibleRouteDepth=3", summary);
            StringAssert.Contains("encounterSeed=123", summary);
            StringAssert.Contains("rewardVersion=2", summary);
        }

        [Test]
        public void EncounterEconomySummary_ContainsBGroupFieldsWithoutFogState()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(
                5u, ExpeditionMapGraph.LegacyFiveTypeMapGenerationVersion);
            var node = map.GetNode("M28");
            var encounter = ChapterOneEncounterRules.Generate(map.Seed, node, false);
            var economy = new ExpeditionEconomyState();
            var coins = 7;
            economy.AddRerollVoucher(ref coins);
            economy.TryAddCombatItem("I01");
            economy.TryResolveAction("M07:choice:coins");

            var summary = ExpeditionMapDiagnostics.CreateEncounterEconomySummary(
                "v0.43.51", map, node.Id, encounter, coins, economy, new[] { "M00", "M02" });

            StringAssert.Contains("clientVersion=v0.43.51", summary);
            StringAssert.Contains("currentNodeId=M28", summary);
            StringAssert.Contains("nodeType=NormalBattle", summary);
            StringAssert.Contains("encounterGenerationVersion=2", summary);
            StringAssert.Contains("encounterRewardVersion=2", summary);
            StringAssert.Contains("battleSeed=", summary);
            StringAssert.Contains("enemyUnits=", summary);
            StringAssert.Contains("pendingCoinReward=6", summary);
            StringAssert.Contains("currentCoins=7", summary);
            StringAssert.Contains("morale=2", summary);
            StringAssert.Contains("backpack=I01", summary);
            StringAssert.Contains("rerollState=firstUsed:0,vouchers:1", summary);
            StringAssert.Contains("resolvedActions=count:1", summary);
            StringAssert.Contains("completedNodes=count:2", summary);
            StringAssert.DoesNotContain("fogTick", summary);
            StringAssert.DoesNotContain("fogGap", summary);
        }
    }
}
