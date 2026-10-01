using System.Collections.Generic;
using System.Linq;
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

            Assert.That(CountActionsBeforeFrenzy(result, "fast"),
                Is.GreaterThan(CountActionsBeforeFrenzy(result, "standard")));
            Assert.That(CountActionsBeforeFrenzy(result, "standard"),
                Is.GreaterThan(CountActionsBeforeFrenzy(result, "slow")));
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

        [Test]
        public void EpicOlinCastsOpenDawnOnFourthOwnActionWithoutIncomingHits()
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Olin(CharacterQuality.Epic, 0, 1000, CombatSpeedTier.Fast));
            input.EnemyUnits.Add(PassiveHealer("enemy-healer-0", 0));
            input.EnemyUnits.Add(PassiveHealer("enemy-healer-1", 1));

            var result = DeterministicBattle.Run(input);
            var activeTime = FirstOpenDawnTime(result);
            var normalActionsBeforeSkill = result.Events.Count(battleEvent =>
                battleEvent.Type == BattleEventType.Attack && battleEvent.ActorId == "olin" &&
                !battleEvent.IsSecondary && battleEvent.ActionTime < activeTime);

            Assert.That(normalActionsBeforeSkill, Is.EqualTo(3));
        }

        [Test]
        public void IncomingDirectHitsAdvanceEpicOlinFirstCast()
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Olin(CharacterQuality.Epic, 0, 1000, CombatSpeedTier.Slow));
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "enemy-attacker", IsPlayer = false, Slot = 0,
                Health = 100000, MaxHealth = 100000, AttackDamage = 1,
                SpeedTier = CombatSpeedTier.Fast
            });

            var result = DeterministicBattle.Run(input);
            var activeTime = FirstOpenDawnTime(result);
            var normalActionsBeforeSkill = result.Events.Count(battleEvent =>
                battleEvent.Type == BattleEventType.Attack && battleEvent.ActorId == "olin" &&
                !battleEvent.IsSecondary && battleEvent.ActionTime < activeTime);

            Assert.That(normalActionsBeforeSkill, Is.EqualTo(2));
        }

        [Test]
        public void OpenDawnWithZeroShieldOnlyCreatesFiftyPercentShield()
        {
            var input = ActiveOlinInput(CharacterQuality.Epic, 0);
            var result = DeterministicBattle.Run(input);
            var activeTime = FirstOpenDawnTime(result);
            var activeEvents = result.Events.Where(battleEvent =>
                battleEvent.ActionTime == activeTime &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.OpenDawnAbilityId).ToList();

            Assert.That(activeEvents.Any(battleEvent => battleEvent.Type == BattleEventType.Attack), Is.False);
            Assert.That(activeEvents.Single(battleEvent => battleEvent.Type == BattleEventType.Shield).Amount,
                Is.EqualTo(500));
            Assert.That(activeEvents.Single(battleEvent => battleEvent.Type == BattleEventType.Shield)
                .TargetShieldAfter, Is.EqualTo(500));
        }

        [Test]
        public void OpenDawnConsumesShieldDealsSixtyPercentAndUsesStableSlotOrder()
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Olin(CharacterQuality.Epic, 100, 400, CombatSpeedTier.Fast));
            input.EnemyUnits.Add(TrainingEnemy("enemy-slot-3", 3));
            input.EnemyUnits.Add(TrainingEnemy("enemy-slot-1", 1));

            var result = DeterministicBattle.Run(input);
            var activeTime = FirstOpenDawnTime(result);
            var activeHits = result.Events.Where(battleEvent =>
                battleEvent.ActionTime == activeTime && battleEvent.Type == BattleEventType.Attack &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.OpenDawnAbilityId).ToList();
            var shieldReset = result.Events.Single(battleEvent =>
                battleEvent.ActionTime == activeTime && battleEvent.Type == BattleEventType.Status &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.OpenDawnAbilityId);
            Assert.That(activeHits.Select(item => item.TargetId).ToArray(),
                Is.EqualTo(new[] { "enemy-slot-1", "enemy-slot-3" }));
            Assert.That(activeHits.All(item => item.Amount == 240), Is.True);
            Assert.That(shieldReset.TargetShieldAfter, Is.Zero);
            Assert.That(result.Events.Any(battleEvent =>
                battleEvent.ActionTime == activeTime && battleEvent.Type == BattleEventType.Shield &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.OpenDawnAbilityId), Is.False);
        }

        [Test]
        public void OpenDawnShieldDamageHasTenPercentMaximumHealthFloor()
        {
            var input = ActiveOlinInput(CharacterQuality.Epic, 10);
            var result = DeterministicBattle.Run(input);
            var activeTime = FirstOpenDawnTime(result);
            var hit = result.Events.Single(battleEvent =>
                battleEvent.ActionTime == activeTime && battleEvent.Type == BattleEventType.Attack &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.OpenDawnAbilityId);

            Assert.That(hit.Amount, Is.EqualTo(100));
        }

        [Test]
        public void LegendaryOlinTideBuffsEachLivingAllysNextThreeActions()
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Olin(CharacterQuality.Legendary, 100, 200, CombatSpeedTier.Fast));
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "ally", IsPlayer = true, Slot = 1,
                Health = 1000, MaxHealth = 1000, AttackDamage = 100,
                SpeedTier = CombatSpeedTier.Standard
            });
            input.EnemyUnits.Add(TrainingEnemy("enemy", 0));

            var result = DeterministicBattle.Run(input);
            var activeTime = FirstOpenDawnTime(result);
            var tideTargets = result.Events.Where(battleEvent =>
                battleEvent.ActionTime == activeTime && battleEvent.Type == BattleEventType.Status &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.TideAbilityId)
                .Select(battleEvent => battleEvent.TargetId).ToArray();
            var allyAttacks = result.Events.Where(battleEvent =>
                battleEvent.Type == BattleEventType.Attack && battleEvent.ActorId == "ally" &&
                !battleEvent.IsSecondary).Take(4).ToArray();

            Assert.That(tideTargets, Is.EqualTo(new[] { "olin", "ally" }));
            Assert.That(allyAttacks.Length, Is.EqualTo(4));
            Assert.That(allyAttacks.Take(3).All(item => item.Amount == 125), Is.True);
            Assert.That(allyAttacks[3].Amount, Is.EqualTo(100));
            Assert.That(allyAttacks[0].ActionTime, Is.EqualTo(973));
            Assert.That(allyAttacks[1].ActionTime - allyAttacks[0].ActionTime, Is.EqualTo(833));
            Assert.That(allyAttacks[2].ActionTime - allyAttacks[1].ActionTime, Is.EqualTo(833));
            Assert.That(allyAttacks[3].ActionTime - allyAttacks[2].ActionTime, Is.EqualTo(1000));
            var expiry = result.Events.First(battleEvent =>
                battleEvent.Type == BattleEventType.Status && battleEvent.TargetId == "ally" &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.TideAbilityId &&
                battleEvent.Amount < 0);
            Assert.That(expiry.ActionTime, Is.EqualTo(allyAttacks[2].ActionTime));
        }

        [Test]
        public void OlinTideHasNoPerBattleTriggerLimitAndConsumesContinuousSpeedBonus()
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Olin(CharacterQuality.Legendary, 100, 200, CombatSpeedTier.Standard));
            input.PlayerUnits[0].Health = input.PlayerUnits[0].MaxHealth = 100000;
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "shield-breaker", IsPlayer = false, Slot = 0,
                Health = 1000000, MaxHealth = 1000000, AttackDamage = 101,
                SpeedTier = CombatSpeedTier.Fast
            });

            var result = DeterministicBattle.Run(input);
            var tideEvents = result.Events.Where(battleEvent =>
                battleEvent.Type == BattleEventType.Status && battleEvent.TargetId == "olin" &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.TideAbilityId &&
                battleEvent.Amount > 0).ToList();
            var finalOlin = result.PlayerUnits.Single(unit => unit.Id == "olin");

            Assert.That(tideEvents.Count, Is.GreaterThan(3));
            Assert.That(tideEvents.Select(item => item.ActionTime).Distinct().Count(),
                Is.EqualTo(tideEvents.Count));
            Assert.That(finalOlin.OlinTideTriggerCount, Is.EqualTo(tideEvents.Count));
            Assert.That(finalOlin.OlinTideActionsRemaining,
                Is.InRange(0, DeterministicBattle.OlinTideBuffActionCount));
        }

        [Test]
        public void LegendaryBrunCreatesSharedBarrierCountersAtHalfAndBreaksNormally()
        {
            var input = new BattleInput { Seed = 20260928u };
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "brun", DefinitionId = "unit.starblade", IsPlayer = true, Slot = 0,
                Health = 1000, MaxHealth = 1000, AttackDamage = 100, Energy = 100,
                Quality = CharacterQuality.Legendary, SpeedTier = CombatSpeedTier.Slow
            });
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "attacker", IsPlayer = false, Slot = 0,
                Health = 100000, MaxHealth = 100000, AttackDamage = 200,
                SpeedTier = CombatSpeedTier.Fast
            });

            var result = DeterministicBattle.Run(input);
            var barrierIndex = result.Events.FindIndex(item =>
                item.SourceEffectId == C05ActiveSkillIntegration.RebuildBridgeAbilityId);
            var counterIndex = result.Events.FindIndex(item =>
                item.SourceEffectId == C05ActiveSkillIntegration.HoldBridgeAbilityId &&
                item.Type == BattleEventType.Attack && item.IsSecondary);
            var breakIndex = result.Events.FindIndex(item =>
                item.SourceEffectId == C05ActiveSkillIntegration.BarrierBrokenStatusId);

            Assert.That(barrierIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.Events[barrierIndex].Amount, Is.EqualTo(250));
            Assert.That(counterIndex, Is.GreaterThan(barrierIndex));
            Assert.That(breakIndex, Is.GreaterThan(counterIndex));
        }

        [Test]
        public void EpicOlinNeverTriggersLegendaryTide()
        {
            var result = DeterministicBattle.Run(ActiveOlinInput(CharacterQuality.Epic, 200));

            Assert.That(result.Events.Any(battleEvent =>
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.TideAbilityId), Is.False);
        }

        [Test]
        public void C01RuntimeEventsRemainDeterministicForTheSameInput()
        {
            var first = DeterministicBattle.Run(ActiveOlinInput(CharacterQuality.Legendary, 200));
            var second = DeterministicBattle.Run(ActiveOlinInput(CharacterQuality.Legendary, 200));

            Assert.That(second.Events.Count, Is.EqualTo(first.Events.Count));
            for (var index = 0; index < first.Events.Count; index++)
            {
                Assert.That(second.Events[index].Type, Is.EqualTo(first.Events[index].Type));
                Assert.That(second.Events[index].ActorId, Is.EqualTo(first.Events[index].ActorId));
                Assert.That(second.Events[index].TargetId, Is.EqualTo(first.Events[index].TargetId));
                Assert.That(second.Events[index].Amount, Is.EqualTo(first.Events[index].Amount));
                Assert.That(second.Events[index].TargetHealthAfter,
                    Is.EqualTo(first.Events[index].TargetHealthAfter));
                Assert.That(second.Events[index].TargetShieldAfter,
                    Is.EqualTo(first.Events[index].TargetShieldAfter));
                Assert.That(second.Events[index].ActionTime, Is.EqualTo(first.Events[index].ActionTime));
                Assert.That(second.Events[index].SourceEffectId,
                    Is.EqualTo(first.Events[index].SourceEffectId));
            }
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

        private static BattleInput ActiveOlinInput(CharacterQuality quality, int shield)
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Olin(quality, 100, shield, CombatSpeedTier.Fast));
            input.EnemyUnits.Add(TrainingEnemy("enemy", 0));
            return input;
        }

        private static BattleUnitState Olin(
            CharacterQuality quality, int energy, int shield, CombatSpeedTier speedTier)
        {
            return new BattleUnitState
            {
                Id = "olin", DefinitionId = "unit.guardian", Quality = quality,
                IsPlayer = true, Slot = 0, Health = 1000, MaxHealth = 1000,
                AttackDamage = 1, Shield = shield, Energy = energy, SpeedTier = speedTier
            };
        }

        private static BattleUnitState TrainingEnemy(string id, int slot)
        {
            return new BattleUnitState
            {
                Id = id, IsPlayer = false, Slot = slot,
                Health = 100000, MaxHealth = 100000, AttackDamage = 1,
                SpeedTier = CombatSpeedTier.Slow
            };
        }

        private static BattleUnitState PassiveHealer(string id, int slot)
        {
            return new BattleUnitState
            {
                Id = id, IsPlayer = false, Slot = slot,
                Health = 50000, MaxHealth = 100000, AttackDamage = 1, HealAmount = 1,
                IsHealer = true, SpeedTier = CombatSpeedTier.Slow
            };
        }

        private static int FirstOpenDawnTime(BattleResult result)
        {
            return result.Events.First(battleEvent =>
                battleEvent.Type == BattleEventType.Status &&
                battleEvent.SourceEffectId == C01ActiveSkillIntegration.OpenDawnAbilityId).ActionTime;
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

        private static int CountActionsBeforeFrenzy(BattleResult result, string actorId)
        {
            return result.Events.Count(battleEvent =>
                battleEvent.Type == BattleEventType.Attack && battleEvent.ActorId == actorId &&
                battleEvent.ActionTime < DeterministicBattle.FrenzyStartMilliseconds);
        }
    }
}
