using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class ExpeditionFlowTests
    {
        [Test]
        public void VictoryRequiresUpgradeThenRouteBeforeNextBattle()
        {
            var flow = new ExpeditionFlow();

            Assert.That(flow.TryBeginBattle(), Is.True);
            Assert.That(flow.TryFinishBattle(playerWon: true, runEnded: false), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(ExpeditionPhase.Upgrade));
            Assert.That(flow.TryBeginBattle(), Is.False);

            Assert.That(flow.TryChooseUpgrade(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(ExpeditionPhase.Route));
            Assert.That(flow.TryBeginBattle(), Is.False);

            Assert.That(flow.TryChooseRoute(), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(ExpeditionPhase.Preparation));
            Assert.That(flow.TryBeginBattle(), Is.True);
        }

        [Test]
        public void DiagnosticRunCanAdvanceThroughMultipleVictoryCycles()
        {
            var flow = new ExpeditionFlow();

            for (var battle = 0; battle < 3; battle++)
            {
                Assert.That(flow.TryBeginBattle(), Is.True, "begin battle " + battle);
                Assert.That(flow.TryFinishBattle(playerWon: true, runEnded: false), Is.True,
                    "finish battle " + battle);
                Assert.That(flow.TryChooseUpgrade(), Is.True, "choose upgrade " + battle);
                Assert.That(flow.TryChooseRoute(), Is.True, "choose route " + battle);
            }

            Assert.That(flow.CanPrepare, Is.True);
        }

        [Test]
        public void LossReturnsToPreparationAndDoesNotOfferReward()
        {
            var flow = new ExpeditionFlow();

            flow.TryBeginBattle();
            Assert.That(flow.TryFinishBattle(playerWon: false, runEnded: false), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(ExpeditionPhase.Preparation));
            Assert.That(flow.TryChooseUpgrade(), Is.False);
            Assert.That(flow.TryChooseRoute(), Is.False);
        }

        [Test]
        public void NonBattleNodeReturnsToMapWithoutUpgrade()
        {
            var flow = new ExpeditionFlow();
            flow.TryBeginBattle();
            flow.TryFinishBattle(playerWon: true, runEnded: false);
            flow.TryChooseUpgrade();

            Assert.That(flow.TryEnterNode(), Is.True);
            Assert.That(flow.CanResolveNode, Is.True);
            Assert.That(flow.TryFinishNode(), Is.True);
            Assert.That(flow.CanChooseRoute, Is.True);
        }

        [Test]
        public void DuplicateAndOutOfOrderActionsAreRejected()
        {
            var flow = new ExpeditionFlow();

            Assert.That(flow.TryChooseUpgrade(), Is.False);
            Assert.That(flow.TryChooseRoute(), Is.False);
            Assert.That(flow.TryBeginBattle(), Is.True);
            Assert.That(flow.TryBeginBattle(), Is.False);
            Assert.That(flow.TryFinishBattle(playerWon: true, runEnded: false), Is.True);
            Assert.That(flow.TryFinishBattle(playerWon: true, runEnded: false), Is.False);
        }

        [Test]
        public void InvalidOrMidBattleSaveCannotBeRestored()
        {
            var flow = new ExpeditionFlow();

            Assert.That(flow.TryRestore((int)ExpeditionPhase.Battle), Is.False);
            Assert.That(flow.TryRestore(999), Is.False);
            Assert.That(flow.TryRestore((int)ExpeditionPhase.Route), Is.True);
            Assert.That(flow.Phase, Is.EqualTo(ExpeditionPhase.Route));
        }

        [Test]
        public void AbandonEndsRunAndWaitsForExplicitReset()
        {
            var flow = new ExpeditionFlow();

            Assert.That(flow.TryAbandon(), Is.True);
            Assert.That(flow.IsComplete, Is.True);
            Assert.That(flow.CanPrepare, Is.False);
            Assert.That(flow.TryAbandon(), Is.False);

            flow.Reset();
            Assert.That(flow.CanPrepare, Is.True);
        }

        [Test]
        public void EmptyFormationBelowMinimumRecruitCostIsUnrecoverable()
        {
            Assert.That(ExpeditionFlow.IsUnrecoverablePreparation(
                ExpeditionPhase.Preparation, coins: 1, occupiedUnits: 0, minimumRecruitCost: 3), Is.True);
            Assert.That(ExpeditionFlow.IsUnrecoverablePreparation(
                ExpeditionPhase.Preparation, coins: 3, occupiedUnits: 0, minimumRecruitCost: 3), Is.False);
            Assert.That(ExpeditionFlow.IsUnrecoverablePreparation(
                ExpeditionPhase.Preparation, coins: 1, occupiedUnits: 1, minimumRecruitCost: 3), Is.False);
            Assert.That(ExpeditionFlow.IsUnrecoverablePreparation(
                ExpeditionPhase.Upgrade, coins: 1, occupiedUnits: 0, minimumRecruitCost: 3), Is.False);
        }

        [Test]
        public void ThreeEqualRunUnitsMergeAndCanCascade()
        {
            var ids = new[] { "guardian", "guardian", "guardian", "guardian", "guardian" };
            var levels = new[] { 1, 1, 1, 2, 2 };

            var first = RunUnitRules.FindTriple(ids, levels, "guardian", 1, preferredIndex: 2);
            Assert.That(first, Is.EqualTo(new[] { 2, 0, 1 }));
            Assert.That(RunUnitRules.GetHealthMultiplierBasisPoints(1), Is.EqualTo(10000));
            Assert.That(RunUnitRules.GetHealthMultiplierBasisPoints(2), Is.EqualTo(16500));
            Assert.That(RunUnitRules.GetHealthMultiplierBasisPoints(3), Is.EqualTo(25500));
            Assert.That(RunUnitRules.GetActionMultiplierBasisPoints(1), Is.EqualTo(10000));
            Assert.That(RunUnitRules.GetActionMultiplierBasisPoints(2), Is.EqualTo(15500));
            Assert.That(RunUnitRules.GetActionMultiplierBasisPoints(3), Is.EqualTo(23500));
            Assert.That(RunUnitRules.ScaleHealth(100, 2), Is.EqualTo(165));
            Assert.That(RunUnitRules.ScaleHealth(100, 3), Is.EqualTo(255));
            Assert.That(RunUnitRules.ScaleAction(20, 2), Is.EqualTo(31));
            Assert.That(RunUnitRules.ScaleAction(20, 3), Is.EqualTo(47));
            Assert.That(RunUnitRules.ScaleAction(10, 2), Is.EqualTo(16));
            Assert.That(RunUnitRules.ScaleAction(10, 3), Is.EqualTo(24));
            Assert.That(RunUnitRules.ScaleHealth(101, 2), Is.EqualTo(167));
            Assert.That(RunUnitRules.ScaleHealth(101, 3), Is.EqualTo(258));
            Assert.That(RunUnitRules.ScaleAction(11, 2), Is.EqualTo(17));
            Assert.That(RunUnitRules.ScaleAction(11, 3), Is.EqualTo(26));
            Assert.That(RunUnitRules.ScaleAction(7, 2), Is.EqualTo(11));
            Assert.That(RunUnitRules.ScaleAction(7, 3), Is.EqualTo(16));

            levels[2] = 2;
            ids[0] = null;
            ids[1] = null;
            var second = RunUnitRules.FindTriple(ids, levels, "guardian", 2, preferredIndex: 2);
            Assert.That(second, Is.EqualTo(new[] { 2, 3, 4 }));
            Assert.That(RunUnitRules.FindTriple(ids, new[] { 0, 0, 3, 3, 3 }, "guardian", 3, 2), Is.Empty);
        }
    }
}
