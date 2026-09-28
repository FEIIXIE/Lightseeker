using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class DeterministicBattleTests
    {
        [Test]
        public void SameInputAndSeedProduceSameOutcomeAndEvents()
        {
            var first = DeterministicBattle.Run(CreateInput());
            var second = DeterministicBattle.Run(CreateInput());

            Assert.That(second.PlayerWon, Is.EqualTo(first.PlayerWon));
            Assert.That(second.Events.Count, Is.EqualTo(first.Events.Count));
            for (var i = 0; i < first.Events.Count; i++)
            {
                Assert.That(second.Events[i].Type, Is.EqualTo(first.Events[i].Type));
                Assert.That(second.Events[i].ActorId, Is.EqualTo(first.Events[i].ActorId));
                Assert.That(second.Events[i].TargetId, Is.EqualTo(first.Events[i].TargetId));
                Assert.That(second.Events[i].Amount, Is.EqualTo(first.Events[i].Amount));
                Assert.That(second.Events[i].TargetHealthAfter, Is.EqualTo(first.Events[i].TargetHealthAfter));
                Assert.That(second.Events[i].TargetShieldAfter, Is.EqualTo(first.Events[i].TargetShieldAfter));
                Assert.That(second.Events[i].ShieldAbsorbed, Is.EqualTo(first.Events[i].ShieldAbsorbed));
                Assert.That(second.Events[i].ActionTime, Is.EqualTo(first.Events[i].ActionTime));
            }
        }

        [Test]
        public void FirstActionBonusIncreasesTheOpeningAttack()
        {
            var baseline = CreateInput();
            var boosted = CreateInput();
            boosted.PlayerUnits[0].FirstActionPercent = 25;

            var baselineResult = DeterministicBattle.Run(baseline);
            var boostedResult = DeterministicBattle.Run(boosted);

            Assert.That(FindFirstAttack(boostedResult, "player-0").Amount,
                Is.GreaterThan(FindFirstAttack(baselineResult, "player-0").Amount));
        }

        [TestCase(0, 100)]
        [TestCase(20, 83)]
        [TestCase(40, 71)]
        [TestCase(80, 55)]
        [TestCase(200, 55)]
        [TestCase(-20, 100)]
        public void ArmorUsesApprovedReductionAndClamp(int armor, int expectedDamage)
        {
            Assert.That(DeterministicBattle.CalculateDamageAfterArmor(100, armor),
                Is.EqualTo(expectedDamage));
        }

        [Test]
        public void SpeedTiersCreateDistinctActionFrequencies()
        {
            var input = new BattleInput { Seed = 20260911u };
            input.PlayerUnits.Add(LongBattleUnit("fast", true, 0, CombatSpeedTier.Fast));
            input.PlayerUnits.Add(LongBattleUnit("slow", true, 1, CombatSpeedTier.Slow));
            input.EnemyUnits.Add(LongBattleUnit("standard", false, 0, CombatSpeedTier.Standard));

            var result = DeterministicBattle.Run(input);

            Assert.That(CountActions(result, "fast"), Is.GreaterThan(CountActions(result, "standard")));
            Assert.That(CountActions(result, "standard"), Is.GreaterThan(CountActions(result, "slow")));
        }

        [Test]
        public void ZeroEnhancementBattleEmitsOnePrimaryAttackPerActorAndActionTime()
        {
            var input = new BattleInput { Seed = 20260921u };
            input.PlayerUnits.Add(LongBattleUnit("player-0", true, 0, CombatSpeedTier.Standard));
            input.PlayerUnits.Add(LongBattleUnit("player-1", true, 1, CombatSpeedTier.Standard));
            input.EnemyUnits.Add(LongBattleUnit("enemy-0", false, 0, CombatSpeedTier.Standard));
            input.EnemyUnits.Add(LongBattleUnit("enemy-1", false, 1, CombatSpeedTier.Standard));

            var result = DeterministicBattle.Run(input);
            var actions = new HashSet<string>();
            foreach (var battleEvent in result.Events)
            {
                if (battleEvent.Type != BattleEventType.Attack) continue;
                Assert.That(battleEvent.IsSecondary, Is.False,
                    "A zero-enhancement battle must not create secondary attacks.");
                Assert.That(actions.Add(battleEvent.ActorId + "@" + battleEvent.ActionTime), Is.True,
                    "An actor emitted more than one primary attack for the same action time.");
            }

            Assert.That(actions.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ShieldAbsorbsArmorReducedDamageBeforeHealth()
        {
            var input = new BattleInput { Seed = 77u };
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "attacker", IsPlayer = true, Health = 100, MaxHealth = 100,
                AttackDamage = 100, SpeedTier = CombatSpeedTier.Fast
            });
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "target", IsPlayer = false, Health = 100, MaxHealth = 100,
                Armor = 20, Shield = 50, SpeedTier = CombatSpeedTier.Slow
            });

            var attack = FindFirstAttack(DeterministicBattle.Run(input), "attacker");

            Assert.That(attack.Amount, Is.EqualTo(83));
            Assert.That(attack.ShieldAbsorbed, Is.EqualTo(50));
            Assert.That(attack.TargetShieldAfter, Is.Zero);
            Assert.That(attack.TargetHealthAfter, Is.EqualTo(67));
        }

        private static BattleInput CreateInput()
        {
            var input = new BattleInput { Seed = 123456u };
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "player-0", IsPlayer = true, Slot = 0,
                Health = 20, MaxHealth = 20, AttackDamage = 4
            });
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "player-1", IsPlayer = true, Slot = 1,
                Health = 12, MaxHealth = 12, HealAmount = 3, IsHealer = true
            });
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "enemy-0", IsPlayer = false, Slot = 0,
                Health = 18, MaxHealth = 18, AttackDamage = 3
            });
            return input;
        }

        private static BattleEvent FindFirstAttack(BattleResult result, string actorId)
        {
            foreach (var battleEvent in result.Events)
            {
                if (battleEvent.Type == BattleEventType.Attack && battleEvent.ActorId == actorId)
                    return battleEvent;
            }
            return null;
        }

        private static BattleUnitState LongBattleUnit(
            string id, bool isPlayer, int slot, CombatSpeedTier speedTier)
        {
            return new BattleUnitState
            {
                Id = id, IsPlayer = isPlayer, Slot = slot,
                Health = 100000, MaxHealth = 100000, AttackDamage = 1,
                Armor = 80, SpeedTier = speedTier
            };
        }

        private static int CountActions(BattleResult result, string actorId)
        {
            var count = 0;
            foreach (var battleEvent in result.Events)
            {
                if ((battleEvent.Type == BattleEventType.Attack || battleEvent.Type == BattleEventType.Heal) &&
                    battleEvent.ActorId == actorId) count++;
            }
            return count;
        }
    }
}
