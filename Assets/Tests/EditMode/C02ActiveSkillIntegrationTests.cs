using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class C02ActiveSkillIntegrationTests
    {
        [Test]
        public void FrozenC02RulesSeparateActiveInstantTargetFromPassiveBeam()
        {
            var active = C02ActiveSkillIntegration.CreateReturnBeaconRule();
            var passive = C02ActiveSkillIntegration.CreateWaitingRule();

            Assert.That(active.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.InstantTarget));
            Assert.That(active.ProjectileProfileId, Is.Null);
            Assert.That(active.ReselectLimit, Is.EqualTo(1));
            Assert.That(passive.DeliveryMode, Is.EqualTo(CardBattleDeliveryMode.Beam));
            Assert.That(passive.ProjectileProfileId,
                Is.EqualTo(C02ActiveSkillIntegration.RescueBeamProfileId));
        }

        [Test]
        public void ActiveApplicationDoesNotEmitProjectileReleased()
        {
            var pipeline = Pipeline("c02-active");
            var action = pipeline.BeginAction("yiwen", "active",
                C02ActiveSkillIntegration.CreateReturnBeaconRule(), 10);
            pipeline.ResolveImpact(action, "impact", 10, new[]
            {
                new CardBattleTargetResult { ChildIndex = 0, TargetId = "ally", TargetHealthAfter = 50 }
            });
            pipeline.AddStatusFeedback(action, "ally", C02ActiveSkillIntegration.ReturnBeaconStatusKey,
                string.Empty, "2", 95);
            pipeline.CompleteAction(action, 10, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);

            Assert.That(pipeline.ProducedEvents.Any(item =>
                item.Kind == CardBattleEventKind.ProjectileReleased), Is.False);
        }

        [Test]
        public void RescueBeamDoesNotEmitProjectileReleasedAndRetainsOneBeforeHealing()
        {
            var pipeline = Pipeline("c02-beam");
            var action = pipeline.BeginAction("yiwen", "passive",
                C02ActiveSkillIntegration.CreateWaitingRule(), 20);
            pipeline.AddStatusFeedback(action, "ally", C02ActiveSkillIntegration.ReturnBeaconStatusKey,
                "1", string.Empty, 95);
            pipeline.ResolveImpact(action, "impact", 20, new[]
            {
                new CardBattleTargetResult
                {
                    ChildIndex = 0,
                    TargetId = "ally",
                    Healing = 120,
                    TargetHealthAfter = 121,
                    StatusChanges = { "retained-health:1" }
                }
            });
            pipeline.CompleteAction(action, 20, CardBattleActionOutcome.Completed,
                CardBattleCancelReason.None, CardBattleInterruptReason.None);

            Assert.That(pipeline.ValidateProducedEvents(), Is.Empty);
            Assert.That(pipeline.ProducedEvents.Any(item =>
                item.Kind == CardBattleEventKind.ProjectileReleased), Is.False);
        }

        [Test]
        public void RuntimeActiveRescueRetainsOneThenHealsFromYiwenStat()
        {
            var result = DeterministicBattle.Run(LethalInput(CharacterQuality.Legendary, 100));
            var rescue = result.Events.First(item => item.Type == BattleEventType.Heal &&
                item.SourceEffectId == C02ActiveSkillIntegration.ReturnBeaconAbilityId &&
                result.Events.Any(attack => attack.Type == BattleEventType.Attack &&
                    attack.TargetId == "ally" && attack.ActionTime == item.ActionTime &&
                    attack.TargetHealthAfter == 1));
            var lethal = result.Events.Last(item => item.Type == BattleEventType.Attack &&
                item.TargetId == "ally" && item.ActionTime == rescue.ActionTime);

            Assert.That(lethal.TargetHealthAfter, Is.EqualTo(1));
            Assert.That(rescue.Amount, Is.EqualTo(180));
            Assert.That(rescue.TargetHealthAfter, Is.EqualTo(181));
            Assert.That(result.Events.Any(item => item.Type == BattleEventType.Defeat &&
                item.TargetId == "ally" && item.ActionTime == rescue.ActionTime), Is.False);
        }

        [Test]
        public void LegendaryPassiveUsesStableSourceAndCannotProtectSelf()
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Yiwen("yiwen-z", CharacterQuality.Legendary, 0, 4));
            input.PlayerUnits.Add(Yiwen("yiwen-a", CharacterQuality.Legendary, 0, 5));
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "ally", IsPlayer = true, Slot = 0, Health = 50, MaxHealth = 1000,
                AttackDamage = 1, SpeedTier = CombatSpeedTier.Slow
            });
            input.EnemyUnits.Add(Enemy());
            var result = DeterministicBattle.Run(input);
            var passive = result.Events.First(item => item.Type == BattleEventType.Heal &&
                item.SourceEffectId == C02ActiveSkillIntegration.WaitingAbilityId);
            Assert.That(passive.ActorId, Is.EqualTo("yiwen-a"));

            var selfInput = new BattleInput { Seed = 20260926u };
            selfInput.PlayerUnits.Add(Yiwen("yiwen", CharacterQuality.Legendary, 0, 0, 50));
            selfInput.EnemyUnits.Add(Enemy());
            var selfResult = DeterministicBattle.Run(selfInput);
            Assert.That(selfResult.Events.Any(item =>
                item.SourceEffectId == C02ActiveSkillIntegration.WaitingAbilityId), Is.False);
        }

        private static C01C05ActiveSkillEventPipeline Pipeline(string battleId)
        {
            return new C01C05ActiveSkillEventPipeline(battleId, new[]
            {
                new CardBattleUnitSnapshot { Id = "yiwen", Health = 1000 },
                new CardBattleUnitSnapshot { Id = "ally", Health = 50 }
            });
        }

        private static BattleInput LethalInput(CharacterQuality quality, int energy)
        {
            var input = new BattleInput { Seed = 20260926u };
            input.PlayerUnits.Add(Yiwen("yiwen", quality, energy, 2));
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "ally", IsPlayer = true, Slot = 0, Health = 50, MaxHealth = 1000,
                AttackDamage = 1, SpeedTier = CombatSpeedTier.Slow
            });
            input.EnemyUnits.Add(Enemy(CombatSpeedTier.Standard));
            return input;
        }

        private static BattleUnitState Yiwen(string id, CharacterQuality quality, int energy,
            int slot, int health = 1000)
        {
            return new BattleUnitState
            {
                Id = id, DefinitionId = "unit.healer", Quality = quality,
                IsPlayer = true, Slot = slot, Health = health, MaxHealth = 1000,
                AttackDamage = 1, HealAmount = 100, IsHealer = true,
                Energy = energy, SpeedTier = CombatSpeedTier.Slow
            };
        }

        private static BattleUnitState Enemy(CombatSpeedTier speedTier = CombatSpeedTier.Fast)
        {
            return new BattleUnitState
            {
                Id = "enemy", IsPlayer = false, Slot = 0,
                Health = 100000, MaxHealth = 100000, AttackDamage = 100,
                SpeedTier = speedTier
            };
        }
    }
}
