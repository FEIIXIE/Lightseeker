using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class B2ActiveSkillIntegrationTests
    {
        [Test]
        public void BasicAndDoubleLightUseStableTargetsAndBeamEvents()
        {
            var input = new BattleInput { Seed = 20260929u };
            input.PlayerUnits.Add(Unit("p0", true, 0, 5000));
            input.PlayerUnits.Add(Unit("p1", true, 1, 5000));
            var boss = Unit("b2", false, 0, 100000);
            boss.DefinitionId = B2ActiveSkillIntegration.DefinitionId;
            boss.AttackDamage = 1;
            boss.BaseAttackSpeedBasisPoints = 20000;
            input.EnemyUnits.Add(boss);

            var result = DeterministicBattle.Run(input);
            Assert.That(result.Events.Any(item => item.SourceEffectId ==
                B2ActiveSkillIntegration.BasicAbilityId), Is.True);
            var doubleLight = result.Events.Where(item => item.SourceEffectId ==
                B2ActiveSkillIntegration.DoubleLightAbilityId).ToArray();
            Assert.That(doubleLight.Length, Is.GreaterThanOrEqualTo(1));
            Assert.That(doubleLight.GroupBy(item => item.ActionTime).All(group =>
                group.Select(item => item.ChildIndex).Distinct().Count() == group.Count()), Is.True);
        }

        [Test]
        public void TargetSelectionPrioritizesMarkedThenLowestHealth()
        {
            var marked = Unit("marked", true, 2, 90);
            marked.B2DeviantLightActionsRemaining = 1;
            var low = Unit("low", true, 0, 10);
            Assert.That(B2ActiveSkillIntegration.SelectBasicTarget(new[] { low, marked }).Id,
                Is.EqualTo("marked"));
            Assert.That(B2ActiveSkillIntegration.SelectDoubleLightTargets(
                new[] { Unit("a", true, 0, 80), Unit("b", true, 1, 20), Unit("c", true, 2, 50) }, 2)
                .Select(item => item.Id).ToArray(), Is.EqualTo(new[] { "b", "c" }));
        }

        [Test]
        public void TargetSelectionExcludesTheCastingBoss()
        {
            var boss = Unit("boss", false, 0, 100);
            boss.DefinitionId = B2ActiveSkillIntegration.DefinitionId;
            var target = Unit("target", true, 1, 80);
            Assert.That(B2ActiveSkillIntegration.SelectBasicTarget(
                new[] { boss, target }, boss.Id).Id, Is.EqualTo(target.Id));
            Assert.That(B2ActiveSkillIntegration.SelectDoubleLightTargets(
                new[] { boss, target }, 2, boss.Id).Select(item => item.Id).ToArray(),
                Is.EqualTo(new[] { target.Id }));
        }

        [Test]
        public void NonB2AttacksDoNotCreateDeviantLightMarks()
        {
            var input = new BattleInput { Seed = 20260929u };
            var player = Unit("p0", true, 0, 40);
            player.MaxHealth = 100;
            input.PlayerUnits.Add(player);
            var enemy = Unit("n1", false, 0, 100);
            enemy.AttackDamage = 1;
            enemy.BaseAttackSpeedBasisPoints = 20000;
            input.EnemyUnits.Add(enemy);

            var result = DeterministicBattle.Run(input);
            Assert.That(result.Events.Any(item => item.SourceEffectId ==
                B2ActiveSkillIntegration.DeviantLightAbilityId), Is.False);
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
