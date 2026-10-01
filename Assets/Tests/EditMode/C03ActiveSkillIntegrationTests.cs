using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class C03ActiveSkillIntegrationTests
    {
        [Test]
        public void FrozenC03RulesUseWhiteTrailProjectileAndAuraTransfer()
        {
            var active = C03ActiveSkillIntegration.CreateWhiteTrailRule();
            var passive = C03ActiveSkillIntegration.CreateTransferRule();

            Assert.That(active.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.Projectile));
            Assert.That(active.ProjectileProfileId,
                Is.EqualTo(C03ActiveSkillIntegration.WhiteTrailProjectileProfileId));
            Assert.That(active.TravelMs, Is.EqualTo(320));
            Assert.That(active.ReselectLimit, Is.EqualTo(1));
            Assert.That(passive.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.Aura));
            Assert.That(passive.TravelMs, Is.Zero);
            Assert.That(passive.Tags, Does.Contain("MAX_TRANSFER_COUNT_1"));
        }

        [Test]
        public void PublicPipelineRejectsTransferOnActiveDirectKill()
        {
            var rule = C03ActiveSkillIntegration.CreateWhiteTrailRule();
            var error = C01C05ActiveSkillContract.ValidateImpact(rule, new[]
            {
                new CardBattleTargetResult
                {
                    ChildIndex = 0,
                    TargetId = "enemy",
                    Damage = 140,
                    TargetHealthAfter = 0,
                    IsDefeated = true,
                    StatusChanges = { "white-mark:transfer" }
                }
            });

            Assert.That(error, Is.EqualTo("C03_DIRECT_KILL_MUST_NOT_TRANSFER_WHITE_MARK"));
        }

        [Test]
        public void EpicActiveSelectsHighestMaximumHealthAndAppliesThreeActionMark()
        {
            var input = BaseInput(CharacterQuality.Epic, 100);
            var result = DeterministicBattle.Run(input);
            var hit = result.Events.First(item => item.Type == BattleEventType.Attack &&
                item.SourceEffectId == C03ActiveSkillIntegration.WhiteTrailAbilityId);
            var mark = result.Events.First(item => item.Type == BattleEventType.Status &&
                item.TargetId == "high" && item.Amount == 3 &&
                item.SourceEffectId == C03ActiveSkillIntegration.WhiteTrailAbilityId);

            Assert.That(hit.TargetId, Is.EqualTo("high"));
            Assert.That(mark.ActorId, Is.EqualTo("leifu"));
        }

        [Test]
        public void LegendaryMarkedDefeatTransfersOnceAndAppliesContinuousSpeedBuff()
        {
            var input = BaseInput(CharacterQuality.Legendary, 100);
            input.EnemyUnits[0].Health = 200;
            var result = DeterministicBattle.Run(input);
            var transfers = result.Events.Where(item => item.Type == BattleEventType.Status &&
                item.SourceEffectId == C03ActiveSkillIntegration.TransferAbilityId &&
                item.Amount == DeterministicBattle.LeifuWhiteTrailActionCount).ToArray();
            var buffedAction = result.Events.First(item => item.Type == BattleEventType.Attack &&
                item.ActorId == "leifu" && transfers.Length > 0 &&
                item.ActionTime > transfers[0].ActionTime && string.IsNullOrEmpty(item.SourceEffectId));

            Assert.That(transfers.Length, Is.EqualTo(1));
            Assert.That(transfers[0].TargetId, Is.EqualTo("next"));
            Assert.That(buffedAction.ActionTime, Is.LessThan(2000));
            Assert.That(buffedAction.Amount, Is.GreaterThanOrEqualTo(120));
        }

        private static BattleInput BaseInput(CharacterQuality quality, int energy)
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Unit("leifu", "unit.thornGuard", true, 1, 1000, 1000,
                100, 10000, quality, energy));
            input.PlayerUnits.Add(Unit("ally", "unit.guardian", true, 1, 1000, 1000,
                100, 8000, CharacterQuality.Common, 0));
            input.EnemyUnits.Add(Unit("high", "enemy.n1.frontliner", false, 1, 1000, 2000,
                1, 6000, CharacterQuality.Common, 0));
            input.EnemyUnits.Add(Unit("next", "enemy.n2.skirmisher", false, 4, 5000, 1000,
                1, 6000, CharacterQuality.Common, 0));
            return input;
        }

        private static BattleUnitState Unit(string id, string definitionId, bool player, int slot,
            int health, int maxHealth, int attack, int speed, CharacterQuality quality, int energy)
        {
            return new BattleUnitState
            {
                Id = id,
                DefinitionId = definitionId,
                IsPlayer = player,
                Slot = slot,
                Health = health,
                MaxHealth = maxHealth,
                AttackDamage = attack,
                BaseAttackSpeedBasisPoints = speed,
                Quality = quality,
                Energy = energy
            };
        }
    }
}
