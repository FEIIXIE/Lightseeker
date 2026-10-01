using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class N1ActiveSkillIntegrationTests
    {
        [Test]
        public void HoldGateUsesInstantTargetAndNoProjectile()
        {
            var rule = N1ActiveSkillIntegration.CreateHoldGateRule();
            Assert.That(rule.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.InstantTarget));
            Assert.That(rule.ProjectileProfileId, Is.Null);
            Assert.That(rule.TravelMs, Is.Zero);
        }

        [Test]
        public void StableSameColumnRearTargetsAreDeterministic()
        {
            var source = Unit("n1", 0, 100);
            var rear = Unit("rear", 3, 100);
            var otherColumn = Unit("other", 4, 100);
            var targets = N1ActiveSkillIntegration.StableSameColumnRearTargets(source,
                new[] { otherColumn, rear });
            Assert.That(targets.Select(item => item.Id).ToArray(), Is.EqualTo(new[] { "rear" }));
        }

        [Test]
        public void RuntimeTriggersOnThirdActionAndExpiresAfterTwoOwnerActions()
        {
            var input = new BattleInput { Seed = 20260928u };
            input.PlayerUnits.Add(Unit("player", "unit.raider", true, 0, 100000));
            input.EnemyUnits.Add(Unit("n1", "enemy.n1.guard", false, 0, 100000));
            input.EnemyUnits.Add(Unit("rear", "enemy.n2.fighter", false, 3, 100000));
            var result = DeterministicBattle.Run(input);
            var applied = result.Events.Where(item => item.SourceEffectId ==
                N1ActiveSkillIntegration.HoldGateAbilityId && item.Amount > 0).ToArray();
            Assert.That(applied.Any(item => item.TargetId == "n1" && item.Amount == 12000), Is.True);
            Assert.That(applied.Any(item => item.TargetId == "rear" && item.Amount == 8000), Is.True);
            Assert.That(result.Events.Any(item => item.SourceEffectId ==
                N1ActiveSkillIntegration.HoldGateAbilityId && item.Amount < 0), Is.True);
        }

        private static BattleUnitState Unit(string id, int slot, int health)
        {
            return Unit(id, "enemy.n1.guard", false, slot, health);
        }

        private static BattleUnitState Unit(string id, string definitionId, bool isPlayer,
            int slot, int health)
        {
            return new BattleUnitState
            {
                Id = id, DefinitionId = definitionId, IsPlayer = isPlayer, Slot = slot,
                Health = health, MaxHealth = health, AttackDamage = 1,
                BaseAttackSpeedBasisPoints = 10000, SpeedTier = CombatSpeedTier.Standard
            };
        }
    }
}
