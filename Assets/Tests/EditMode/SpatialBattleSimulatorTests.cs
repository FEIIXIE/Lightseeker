using System;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class SpatialBattleSimulatorTests
    {
        [Test]
        public void G1BaselineIsDeterministicAcrossOneThousandRuns()
        {
            var baseline = SpatialBattleSimulator.Run(SpatialBattleSimulator.CreateG1Baseline());
            for (var iteration = 0; iteration < 1000; iteration++)
            {
                var replay = SpatialBattleSimulator.Run(SpatialBattleSimulator.CreateG1Baseline());
                Assert.That(replay.InputHash, Is.EqualTo(baseline.InputHash));
                Assert.That(replay.EventHash, Is.EqualTo(baseline.EventHash));
                Assert.That(replay.FinalStateHash, Is.EqualTo(baseline.FinalStateHash));
                Assert.That(replay.Outcome, Is.EqualTo(baseline.Outcome));
            }
        }

        [Test]
        public void InsertionOrderDoesNotChangeTargetingOrResult()
        {
            var ordered = SpatialBattleSimulator.CreateG1Baseline();
            var reversed = SpatialBattleSimulator.CreateG1Baseline();
            reversed.PlayerUnits.Reverse();
            reversed.EnemyUnits.Reverse();

            var first = SpatialBattleSimulator.Run(ordered);
            var second = SpatialBattleSimulator.Run(reversed);

            Assert.That(second.InputHash, Is.EqualTo(first.InputHash));
            Assert.That(second.EventHash, Is.EqualTo(first.EventHash));
            Assert.That(second.FinalStateHash, Is.EqualTo(first.FinalStateHash));
        }

        [Test]
        public void MeleeCannotDamageBeforeMovingIntoContactRange()
        {
            var input = Duel(SpatialUnitRole.Melee, SpatialUnitRole.Melee, 1000, 15000);
            var result = SpatialBattleSimulator.Run(input);
            var firstDamage = result.Events.First(entry => entry.Type == SpatialBattleEventType.Damaged);

            Assert.That(result.Events.Any(entry => entry.Type == SpatialBattleEventType.Moved &&
                                                   entry.ActorId == firstDamage.ActorId &&
                                                   entry.Sequence < firstDamage.Sequence), Is.True);
            var attacker = result.FinalUnits.First(unit => unit.BattleUnitId == firstDamage.ActorId);
            var target = result.FinalUnits.First(unit => unit.BattleUnitId == firstDamage.TargetId);
            Assert.That(attacker.Role, Is.EqualTo(SpatialUnitRole.Melee));
            Assert.That(target, Is.Not.Null);
        }

        [Test]
        public void RangedMovesThenReleasesOnlyAfterEnteringRange()
        {
            var input = Duel(SpatialUnitRole.Ranged, SpatialUnitRole.Melee, 1000, 15000);
            var result = SpatialBattleSimulator.Run(input);
            var release = result.Events.First(entry => entry.Type == SpatialBattleEventType.Released &&
                                                       entry.ActorId == "P-A");
            Assert.That(result.Events.Any(entry => entry.Type == SpatialBattleEventType.Moved &&
                                                   entry.ActorId == "P-A" &&
                                                   entry.Sequence < release.Sequence), Is.True);
        }

        [Test]
        public void DeadTargetIsReplacedByStableIdOnNextAcquire()
        {
            var input = new SpatialBattleInput();
            input.PlayerUnits.Add(Unit("P-A", true, SpatialUnitRole.Ranged, 5000, 12000, 30, 50));
            input.EnemyUnits.Add(Unit("E-A", false, SpatialUnitRole.Melee, 4000, 7000, 5, 1));
            input.EnemyUnits.Add(Unit("E-B", false, SpatialUnitRole.Melee, 6000, 7000, 30, 1));

            var result = SpatialBattleSimulator.Run(input);
            var acquisitions = result.Events.Where(entry =>
                entry.Type == SpatialBattleEventType.TargetAcquired && entry.ActorId == "P-A").ToArray();

            Assert.That(acquisitions.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(acquisitions[0].TargetId, Is.EqualTo("E-A"));
            Assert.That(acquisitions.Any(entry => entry.TargetId == "E-B"), Is.True);
            var defeat = result.Events.First(entry => entry.Type == SpatialBattleEventType.Defeated &&
                                                      entry.ActorId == "E-A");
            var retarget = acquisitions.First(entry => entry.TargetId == "E-B");
            Assert.That(retarget.Tick, Is.GreaterThan(defeat.Tick));
        }

        [Test]
        public void PositionsAreClampedInsideFieldIncludingRadius()
        {
            var input = Duel(SpatialUnitRole.Melee, SpatialUnitRole.Ranged, -2000, 19000);
            var result = SpatialBattleSimulator.Run(input);
            foreach (var unit in result.FinalUnits)
            {
                Assert.That(unit.Position.X, Is.InRange(unit.Radius, input.Config.FieldWidth - unit.Radius));
                Assert.That(unit.Position.Y, Is.InRange(unit.Radius, input.Config.FieldHeight - unit.Radius));
            }
            foreach (var moved in result.Events.Where(entry => entry.Type == SpatialBattleEventType.Moved))
            {
                Assert.That(moved.Position.X, Is.InRange(350, input.Config.FieldWidth - 350));
                Assert.That(moved.Position.Y, Is.InRange(350, input.Config.FieldHeight - 350));
            }
        }

        [Test]
        public void UnknownRulesVersionIsRejectedInsteadOfFallingBackToV1()
        {
            var input = SpatialBattleSimulator.CreateG1Baseline();
            input.Config.RulesVersion = "battle-rules-v99";
            Assert.Throws<NotSupportedException>(() => SpatialBattleSimulator.Run(input));
        }

        private static SpatialBattleInput Duel(
            SpatialUnitRole playerRole,
            SpatialUnitRole enemyRole,
            int playerY,
            int enemyY)
        {
            var input = new SpatialBattleInput();
            input.PlayerUnits.Add(Unit("P-A", true, playerRole, 5000, playerY, 40, 4));
            input.EnemyUnits.Add(Unit("E-A", false, enemyRole, 5000, enemyY, 40, 4));
            return input;
        }

        private static SpatialBattleUnitInput Unit(
            string id,
            bool player,
            SpatialUnitRole role,
            int x,
            int y,
            int health,
            int damage)
        {
            return new SpatialBattleUnitInput
            {
                BattleUnitId = id,
                IsPlayer = player,
                Role = role,
                Position = new SpatialPoint(x, y),
                Radius = 350,
                MoveSpeed = role == SpatialUnitRole.Melee ? 1000 : 950,
                MaximumHealth = health,
                AttackDamage = damage,
                AttackPeriodMilliseconds = 1000
            };
        }
    }
}
