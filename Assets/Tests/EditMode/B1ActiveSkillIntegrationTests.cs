using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class B1ActiveSkillIntegrationTests
    {
        [Test]
        public void BlockadeTargetsUseFrontlineThenBacklineWithStableChildOrder()
        {
            var frontTargets = B1ActiveSkillIntegration.StableBlockadeTargets(new[]
            {
                Unit("front-2", true, 2, 100), Unit("rear-0", true, 3, 100),
                Unit("front-0", true, 0, 100)
            });
            Assert.That(frontTargets.Select(item => item.Id).ToArray(),
                Is.EqualTo(new[] { "front-0", "front-2" }));

            var rearTargets = B1ActiveSkillIntegration.StableBlockadeTargets(new[]
            {
                Unit("rear-2", true, 5, 100), Unit("rear-0", true, 3, 100)
            });
            Assert.That(rearTargets.Select(item => item.Id).ToArray(),
                Is.EqualTo(new[] { "rear-0", "rear-2" }));
        }

        [Test]
        public void RuntimeAppliesClosedArmorAndThirdActionBlockadeQuakeOncePerTarget()
        {
            var input = new BattleInput { Seed = 20260929u };
            input.PlayerUnits.Add(Unit("front-2", true, 2, 100000));
            input.PlayerUnits.Add(Unit("front-0", true, 0, 100000));
            input.PlayerUnits.Add(Unit("rear-0", true, 3, 100000));
            input.EnemyUnits.Add(Boss(100000, 9));

            var result = DeterministicBattle.Run(input);
            var openingShield = result.Events.First(item => item.SourceEffectId ==
                B1ActiveSkillIntegration.ClosedArmorAbilityId && item.Type == BattleEventType.Shield);
            Assert.That(openingShield.Amount, Is.EqualTo(25000));

            var firstQuakeTime = result.Events.First(item => item.SourceEffectId ==
                B1ActiveSkillIntegration.BlockadeQuakeAbilityId).ActionTime;
            var quake = result.Events.Where(item => item.SourceEffectId ==
                B1ActiveSkillIntegration.BlockadeQuakeAbilityId &&
                item.ActionTime == firstQuakeTime).ToArray();
            Assert.That(quake.Select(item => item.TargetId).ToArray(),
                Is.EqualTo(new[] { "front-0", "front-2" }));
            Assert.That(quake.Select(item => item.ChildIndex).ToArray(),
                Is.EqualTo(new[] { 0, 1 }));
        }

        [Test]
        public void ClosedArmorReductionBreakWindowAndNoReturnThresholdAreApplied()
        {
            var input = new BattleInput { Seed = 77u };
            var attacker = Unit("attacker", true, 0, 100000);
            attacker.AttackDamage = 300;
            attacker.BaseAttackSpeedBasisPoints = 20000;
            input.PlayerUnits.Add(attacker);
            input.EnemyUnits.Add(Boss(1000, 1));

            var result = DeterministicBattle.Run(input);
            var firstAttack = result.Events.First(item => item.Type == BattleEventType.Attack &&
                item.ActorId == "attacker" && item.TargetId == "b1");
            Assert.That(firstAttack.Amount, Is.EqualTo(240));
            Assert.That(result.Events.Any(item => item.SourceEffectId ==
                B1ActiveSkillIntegration.ClosedArmorAbilityId &&
                item.Type == BattleEventType.Status && item.Amount == 2), Is.True);
            Assert.That(result.Events.Any(item => item.SourceEffectId ==
                B1ActiveSkillIntegration.NoReturnAbilityId &&
                item.Type == BattleEventType.Shield && item.Amount == 200), Is.True);
        }

        private static BattleUnitState Boss(int health, int attack)
        {
            var boss = Unit("b1", false, 0, health);
            boss.DefinitionId = B1ActiveSkillIntegration.DefinitionId;
            boss.AttackDamage = attack;
            boss.BaseAttackSpeedBasisPoints = 7000;
            boss.SpeedTier = CombatSpeedTier.Slow;
            return boss;
        }

        private static BattleUnitState Unit(string id, bool isPlayer, int slot, int health)
        {
            return new BattleUnitState
            {
                Id = id, DefinitionId = isPlayer ? "unit.test" : "enemy.test",
                IsPlayer = isPlayer, Slot = slot, Health = health, MaxHealth = health,
                AttackDamage = 1, Armor = 0, BaseAttackSpeedBasisPoints = 10000,
                SpeedTier = CombatSpeedTier.Standard
            };
        }
    }
}
