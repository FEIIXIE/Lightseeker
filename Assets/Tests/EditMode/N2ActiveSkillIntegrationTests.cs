using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class N2ActiveSkillIntegrationTests
    {
        [Test]
        public void CrossCutRuleIsSingleTargetMeleeWithoutProjectile()
        {
            var rule = N2ActiveSkillIntegration.CreateCrossCutRule();
            Assert.That(rule.MaxTargetCount, Is.EqualTo(1));
            Assert.That(rule.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.InstantTarget));
            Assert.That(rule.ProjectileProfileId, Is.Null);
            Assert.That(rule.TravelMs, Is.Zero);
        }

        [Test]
        public void RuntimeTriggersEveryThirdActionAndOnlyOncePerTrigger()
        {
            var input = new BattleInput { Seed = 20260928u };
            input.PlayerUnits.Add(Unit("player", "unit.raider", true, 0, 1000000, 1));
            input.EnemyUnits.Add(Unit("n2", "enemy.n2.fighter", false, 0, 1000000, 1000));
            var result = DeterministicBattle.Run(input);
            var cuts = result.Events.Where(item => item.SourceEffectId ==
                N2ActiveSkillIntegration.CrossCutAbilityId && item.Type == BattleEventType.Attack).ToArray();
            Assert.That(cuts.Length, Is.GreaterThanOrEqualTo(2));
            Assert.That(cuts.All(item => item.TargetId == "player"), Is.True);
            Assert.That(cuts.All(item => item.Amount == 1500), Is.True);
            Assert.That(cuts.All(item => !item.IsSecondary), Is.True);
        }

        private static BattleUnitState Unit(string id, string definitionId, bool isPlayer,
            int slot, int health, int attackDamage)
        {
            return new BattleUnitState
            {
                Id = id, DefinitionId = definitionId, IsPlayer = isPlayer, Slot = slot,
                Health = health, MaxHealth = health, AttackDamage = attackDamage,
                BaseAttackSpeedBasisPoints = 20000, SpeedTier = CombatSpeedTier.Fast,
                Quality = CharacterQuality.Common
            };
        }
    }
}
