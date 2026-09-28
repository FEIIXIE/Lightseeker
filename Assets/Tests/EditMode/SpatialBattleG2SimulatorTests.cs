using System;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class SpatialBattleG2SimulatorTests
    {
        [Test]
        public void G1HashesRemainUnchanged()
        {
            var result = SpatialBattleSimulator.Run(SpatialBattleSimulator.CreateG1Baseline());
            Assert.That(result.InputHash, Is.EqualTo("359CF5CAE5605846"));
            Assert.That(result.EventHash, Is.EqualTo("97A1015FE3B9B6C0"));
            Assert.That(result.FinalStateHash, Is.EqualTo("532D8552E61AAD28"));
        }

        [Test]
        public void G2AIsDeterministicAndOrderIndependent()
        {
            var baseline = SpatialBattleG2Simulator.Run(SpatialBattleG2Simulator.CreateG2A());
            for (var iteration = 0; iteration < 1000; iteration++)
            {
                var replay = SpatialBattleG2Simulator.Run(SpatialBattleG2Simulator.CreateG2A());
                Assert.That(replay.EventHash, Is.EqualTo(baseline.EventHash));
                Assert.That(replay.FinalStateHash, Is.EqualTo(baseline.FinalStateHash));
            }
            var reversed = SpatialBattleG2Simulator.CreateG2A();
            reversed.PlayerUnits.Reverse();
            reversed.EnemyUnits.Reverse();
            var reverseResult = SpatialBattleG2Simulator.Run(reversed);
            Assert.That(reverseResult.InputHash, Is.EqualTo(baseline.InputHash));
            Assert.That(reverseResult.EventHash, Is.EqualTo(baseline.EventHash));
            Assert.That(reverseResult.FinalStateHash, Is.EqualTo(baseline.FinalStateHash));
        }

        [Test]
        public void SupportSelectsLowestRatioAndHeals()
        {
            var result = SpatialBattleG2Simulator.Run(SpatialBattleG2Simulator.CreateG2A());
            var first = result.Events.First(entry => entry.ActorId == "P-C02" &&
                                                     entry.Type == SpatialG2EventType.TargetAcquired &&
                                                     entry.TargetMode == SpatialG2TargetMode.Heal);
            Assert.That(first.TargetId, Is.EqualTo("P-C01"));
            Assert.That(result.Events.Any(entry => entry.ActorId == "P-C02" &&
                                                   entry.Type == SpatialG2EventType.Healed &&
                                                   entry.Amount > 0), Is.True);
            Assert.That(result.HealingDone, Is.GreaterThan(0));
        }

        [Test]
        public void SupportFollowsFormationInsteadOfChasingWhenNobodyIsInjured()
        {
            var input = SpatialBattleG2Simulator.CreateG2A(20261920u);
            foreach (var unit in input.PlayerUnits) unit.InitialHealth = unit.MaximumHealth;
            foreach (var unit in input.EnemyUnits) unit.InitialHealth = unit.MaximumHealth;
            var result = SpatialBattleG2Simulator.Run(input);
            var first = result.Events.First(entry => entry.ActorId == "P-C02" &&
                                                     entry.Type == SpatialG2EventType.TargetAcquired);
            Assert.That(first.TargetMode, Is.EqualTo(SpatialG2TargetMode.Follow));
            Assert.That(first.ReasonCode, Does.Contain("formation"));
        }

        [Test]
        public void N4UsesTwentyPercentagePointThresholdAndRetargetsInVectorB()
        {
            Assert.That(SpatialBattleG2Simulator.IsN4RetargetThresholdMet(61, 100, 80, 100), Is.False);
            Assert.That(SpatialBattleG2Simulator.IsN4RetargetThresholdMet(60, 100, 80, 100), Is.True);
            var result = SpatialBattleG2Simulator.Run(SpatialBattleG2Simulator.CreateG2B());
            var first = result.Events.First(entry => entry.ActorId == "E-N4" &&
                                                     entry.Type == SpatialG2EventType.TargetAcquired);
            Assert.That(first.TargetId, Is.EqualTo("P-C01"));
            Assert.That(result.Events.Any(entry => entry.ActorId == "E-N4" &&
                                                   entry.Type == SpatialG2EventType.Retargeted &&
                                                   entry.ReasonCode.Contains("n4_gap_2000")), Is.True);
        }

        [Test]
        public void CongestionVectorSeparatesSidestepsRetargetsAndTimesOutWithoutRewards()
        {
            var result = SpatialBattleG2Simulator.Run(SpatialBattleG2Simulator.CreateG2C());
            Assert.That(result.Outcome, Is.EqualTo(SpatialG2Outcome.DrawTimeout));
            Assert.That(result.RewardsGranted, Is.False);
            Assert.That(result.SeparationCount, Is.GreaterThan(0));
            Assert.That(result.StuckDetectionCount, Is.GreaterThan(0));
            Assert.That(result.SidestepCount, Is.GreaterThan(0));
            Assert.That(result.RetargetCount, Is.GreaterThan(0));
        }

        [Test]
        public void UnknownG2RulesVersionIsRejected()
        {
            var input = SpatialBattleG2Simulator.CreateG2A();
            input.Config.RulesVersion = "battle-rules-v2-g999";
            Assert.Throws<NotSupportedException>(() => SpatialBattleG2Simulator.Run(input));
        }

        [TestCase("P-C01", "unit.guardian")]
        [TestCase("P-C02", "unit.healer")]
        [TestCase("P-C03", "unit.thornGuard")]
        [TestCase("P-C05", "unit.starblade")]
        [TestCase("E-N2A", "enemy.n2.fighter")]
        [TestCase("E-N2B", "enemy.n2.fighter")]
        [TestCase("E-N3", "enemy.n3.ranged")]
        [TestCase("E-N4", "enemy.n4.skirmisher")]
        [TestCase("E-N5", "enemy.n5.support")]
        public void G2BattleIdsUseTheApprovedStaticArtDefinitions(string battleUnitId, string expected)
        {
            Assert.That(SpatialBattleG2PresentationMapping.DefinitionIdFor(battleUnitId),
                Is.EqualTo(expected));
        }
    }
}
