using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class RoguelikeEnhancementTests
    {
        private static RoguelikeEligibilityContext FullContext()
        {
            return new RoguelikeEligibilityContext
            {
                HasShieldSource = true, HasSteelOrGuardian = true, DirectAttackerCount = 3
            };
        }

        private static RoguelikeAcquiredEffect Effect(string id, RoguelikeRarity rarity, int stacks = 1)
        {
            return new RoguelikeAcquiredEffect { effectId = id, rarityId = (int)rarity, stackCount = stacks };
        }

        [Test]
        public void D01_ChoiceGenerationIsDeterministic()
        {
            var first = RoguelikeEnhancementRules.GenerateChoiceSet("run-a", 4, 2, 5000, false, FullContext(), null);
            var repeated = RoguelikeEnhancementRules.GenerateChoiceSet("run-a", 4, 2, 5000, false, FullContext(), null);
            Assert.That(repeated.choiceSetId, Is.EqualTo(first.choiceSetId));
            Assert.That(repeated.options.Select(x => x.effectId + ":" + x.rarityId),
                Is.EqualTo(first.options.Select(x => x.effectId + ":" + x.rarityId)));
        }

        [Test]
        public void D02_RarityBandsUseLockedIntegerWeights()
        {
            Assert.That(RoguelikeEnhancementRules.RarityWeights(0), Is.EqualTo(new[] { 6000, 3000, 900, 100, 0 }));
            Assert.That(RoguelikeEnhancementRules.RarityWeights(2500), Is.EqualTo(new[] { 4800, 3400, 1500, 280, 20 }));
            Assert.That(RoguelikeEnhancementRules.RarityWeights(5000), Is.EqualTo(new[] { 3800, 3400, 2100, 650, 50 }));
            Assert.That(RoguelikeEnhancementRules.RarityWeights(7500), Is.EqualTo(new[] { 2800, 3400, 2700, 950, 150 }));
            Assert.That(RoguelikeEnhancementRules.RarityWeights(9999).Sum(), Is.EqualTo(10000));
        }

        [Test]
        public void D03_EliteChoiceAlwaysContainsRareOrBetter()
        {
            for (var ordinal = 0; ordinal < 100; ordinal++)
            {
                var set = RoguelikeEnhancementRules.GenerateChoiceSet("elite", ordinal, 0, 2000, true, FullContext(), null);
                Assert.That(set.options.Any(x => x.rarityId >= (int)RoguelikeRarity.Rare), Is.True, set.choiceSetId);
            }
        }

        [Test]
        public void D04_EligibilityCapsAndDuplicatesAreEnforced()
        {
            var acquired = new[] { Effect("B01", RoguelikeRarity.Legendary, 3) };
            var context = new RoguelikeEligibilityContext { DirectAttackerCount = 1 };
            var set = RoguelikeEnhancementRules.GenerateChoiceSet("limited", 1, 0, 3000, false, context, acquired);
            var ids = set.options.Select(x => x.effectId).ToArray();
            Assert.That(ids.Distinct().Count(), Is.EqualTo(3));
            Assert.That(ids, Does.Not.Contain("B01"));
            Assert.That(ids, Does.Not.Contain("B08"));
            Assert.That(ids, Does.Not.Contain("B10"));
            Assert.That(ids, Does.Not.Contain("B12"));
        }

        [Test]
        public void D05_SelectionIsPersistentIdempotentAndVersionGated()
        {
            var state = new RoguelikeBuildState();
            var set = state.EnsureChoiceSet("run", 4000, false, false, true, FullContext());
            var first = state.Select(set.choiceSetId, set.options[0].optionId, "action-1", 0);
            var replay = state.Select(set.choiceSetId, set.options[0].optionId, "action-1", 0);
            var stale = state.Select(set.choiceSetId, set.options[1].optionId, "action-2", 0);
            Assert.That((RoguelikeChoiceResultCode)first.resultCode, Is.EqualTo(RoguelikeChoiceResultCode.Success));
            Assert.That(replay.isReplay, Is.True);
            Assert.That(state.AcquiredEffects.Count, Is.EqualTo(1));
            Assert.That((RoguelikeChoiceResultCode)stale.resultCode, Is.EqualTo(RoguelikeChoiceResultCode.StaleVersion));
        }

        [Test]
        public void D06_BaseAttackAndHealthApplyOnceWithFloorAndFullHealth()
        {
            var input = BasicBattle();
            input.PlayerUnits[0].AttackDamage = 25;
            input.PlayerUnits[0].Health = 1;
            input.PlayerUnits[0].MaxHealth = 101;
            input.PlayerRoguelikeEffects.Add(Effect("B01", RoguelikeRarity.Common));
            input.PlayerRoguelikeEffects.Add(Effect("B02", RoguelikeRarity.Common));
            var result = DeterministicBattle.Run(input);
            Assert.That(result.PlayerUnits[0].AttackDamage, Is.EqualTo(26));
            Assert.That(result.PlayerUnits[0].MaxHealth, Is.EqualTo(106));
            Assert.That(result.PlayerUnits[0].Health, Is.GreaterThan(1));
        }

        [Test]
        public void D07_ArmorIdentityCountsOnceAndShieldRepairTriggersOnce()
        {
            var input = BasicBattle();
            input.PlayerUnits[0].Armor = 79;
            input.PlayerUnits[0].FactionId = "faction.steel";
            input.PlayerUnits[0].ProfessionId = "profession.guardian";
            input.PlayerUnits[0].Shield = 2;
            input.EnemyUnits[0].AttackDamage = 30;
            input.EnemyUnits[0].SpeedTier = CombatSpeedTier.Fast;
            input.PlayerRoguelikeEffects.Add(Effect("B03", RoguelikeRarity.Legendary));
            input.PlayerRoguelikeEffects.Add(Effect("B08", RoguelikeRarity.Common));
            var result = DeterministicBattle.Run(input);
            Assert.That(result.PlayerUnits[0].Armor, Is.EqualTo(80));
            Assert.That(result.Events.Count(x => x.SourceEffectId == "B08"), Is.LessThanOrEqualTo(1));
        }

        [Test]
        public void D08_FireUsesArmorAndLeechDoesNotRecurse()
        {
            var input = BasicBattle();
            input.PlayerUnits[0].MaxHealth = 200;
            input.EnemyUnits[0].AttackDamage = 80;
            input.EnemyUnits[0].SpeedTier = CombatSpeedTier.Fast;
            input.EnemyUnits[0].Armor = 50;
            input.EnemyUnits[0].MaxHealth = input.EnemyUnits[0].Health = 500;
            input.PlayerRoguelikeEffects.Add(Effect("B04", RoguelikeRarity.Legendary));
            input.PlayerRoguelikeEffects.Add(Effect("B06", RoguelikeRarity.Legendary));
            var result = DeterministicBattle.Run(input);
            Assert.That(result.Events.Any(x => x.SourceEffectId == "B04" && x.IsSecondary), Is.True);
            Assert.That(result.Events.Any(x => x.SourceEffectId == "B06" && x.IsSecondary), Is.True);
            Assert.That(result.Events.All(x => x.SourceEffectId != "B04" || x.Type == BattleEventType.Attack), Is.True);
        }

        [Test]
        public void D09_FocusChainIsDeterministicAndConsumesArmedStrike()
        {
            var input = BasicBattle();
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "p2", IsPlayer = true, Slot = 1, Health = 100, MaxHealth = 100,
                AttackDamage = 100, SpeedTier = CombatSpeedTier.Standard
            });
            input.PlayerUnits[0].AttackDamage = 100;
            input.EnemyUnits[0].Health = input.EnemyUnits[0].MaxHealth = 1000;
            input.EnemyUnits[0].Armor = 80;
            input.EnemyUnits[0].AttackDamage = 1;
            input.PlayerRoguelikeEffects.Add(Effect("B10", RoguelikeRarity.Rare));
            var first = DeterministicBattle.Run(input);
            var repeated = DeterministicBattle.Run(input);
            Assert.That(first.Events.Select(EventKey), Is.EqualTo(repeated.Events.Select(EventKey)));
            Assert.That(first.Events.Where(x => x.Type == BattleEventType.Attack && !x.IsSecondary && x.ActorId.StartsWith("p"))
                .Select(x => x.Amount).Distinct().Count(), Is.GreaterThan(1));
        }

        [Test]
        public void D10_HealthThresholdAndLowHealthActionHaveExactBoundaries()
        {
            var input = BasicBattle();
            input.PlayerUnits[0].MaxHealth = input.PlayerUnits[0].Health = 100;
            input.PlayerUnits[0].AttackDamage = 20;
            input.EnemyUnits[0].AttackDamage = 75;
            input.EnemyUnits[0].SpeedTier = CombatSpeedTier.Fast;
            input.EnemyUnits[0].MaxHealth = input.EnemyUnits[0].Health = 300;
            input.PlayerRoguelikeEffects.Add(Effect("B14", RoguelikeRarity.Common));
            input.PlayerRoguelikeEffects.Add(Effect("B15", RoguelikeRarity.Common));
            var result = DeterministicBattle.Run(input);
            Assert.That(result.Events.Count(x => x.SourceEffectId == "B14"), Is.EqualTo(1));
            Assert.That(result.Events.Any(x => x.ActorId == "p1" && x.Type == BattleEventType.Attack && x.Amount >= 21), Is.True);
        }

        [Test]
        public void D11_BossAndNonBattleDoNotGenerateChoiceSets()
        {
            var boss = new RoguelikeBuildState();
            var node = new RoguelikeBuildState();
            Assert.That(boss.EnsureChoiceSet("run", 9000, false, true, true, FullContext()), Is.Null);
            Assert.That(node.EnsureChoiceSet("run", 3000, false, false, false, FullContext()), Is.Null);
        }

        [Test]
        public void D12_LegacyPlaceholderIdsAreNotRegisteredAsD1Effects()
        {
            Assert.That(RoguelikeEnhancementRules.Definitions.Any(x => x.EffectId == "upgrade.might"), Is.False);
            Assert.That(RoguelikeEnhancementRules.Get("B12").EnabledInD1, Is.False);
            Assert.That(RoguelikeEnhancementRules.RulesVersion, Is.EqualTo(1));
        }

        [Test]
        public void D13_PersistentStatPreviewMatchesBattleStartProjection()
        {
            var input = BasicBattle();
            input.PlayerUnits[0].Health = input.PlayerUnits[0].MaxHealth = 23;
            input.PlayerUnits[0].AttackDamage = 25;
            input.PlayerUnits[0].Armor = 72;
            input.PlayerUnits[0].FactionId = "faction.steel";
            input.PlayerUnits[0].ProfessionId = "profession.guardian";
            input.PlayerRoguelikeEffects.Add(Effect("B01", RoguelikeRarity.Common));
            input.PlayerRoguelikeEffects.Add(Effect("B02", RoguelikeRarity.Rare));
            input.PlayerRoguelikeEffects.Add(Effect("B02", RoguelikeRarity.Uncommon));
            input.PlayerRoguelikeEffects.Add(Effect("B03", RoguelikeRarity.Legendary));

            var expectedAttack = RoguelikeEnhancementRules.EffectiveAttackDamage(
                input.PlayerUnits[0].AttackDamage, input.PlayerRoguelikeEffects);
            var expectedHealth = RoguelikeEnhancementRules.EffectiveMaximumHealth(
                input.PlayerUnits[0].MaxHealth, input.PlayerRoguelikeEffects);
            var expectedArmor = RoguelikeEnhancementRules.EffectiveArmor(
                input.PlayerUnits[0].Armor, input.PlayerUnits[0].FactionId,
                input.PlayerUnits[0].ProfessionId, input.PlayerRoguelikeEffects);

            var result = DeterministicBattle.Run(input);
            Assert.That(expectedHealth, Is.EqualTo(26));
            Assert.That(result.PlayerUnits[0].AttackDamage, Is.EqualTo(expectedAttack));
            Assert.That(result.PlayerUnits[0].MaxHealth, Is.EqualTo(expectedHealth));
            Assert.That(result.PlayerUnits[0].Armor, Is.EqualTo(expectedArmor));
        }

        private static BattleInput BasicBattle()
        {
            var input = new BattleInput { Seed = 123u };
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "p1", IsPlayer = true, Slot = 0, Health = 100, MaxHealth = 100,
                AttackDamage = 20, SpeedTier = CombatSpeedTier.Standard,
                FactionId = "faction.wild", ProfessionId = "profession.output"
            });
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "e1", IsPlayer = false, Slot = 0, Health = 100, MaxHealth = 100,
                AttackDamage = 5, SpeedTier = CombatSpeedTier.Slow
            });
            return input;
        }

        private static string EventKey(BattleEvent value)
        {
            return value.Type + ":" + value.ActorId + ":" + value.TargetId + ":" + value.Amount + ":" +
                   value.TargetHealthAfter + ":" + value.IsSecondary + ":" + value.SourceEffectId;
        }
    }
}
