using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ChapterOneRoguelikeV11RulesTests
    {
        private static ChapterOneRoguelikeEligibility FullContext()
        {
            return new ChapterOneRoguelikeEligibility
            {
                HasAnyAlly = true,
                HasSteelOrGuardian = true,
                HasGuard = true,
                HasOutput = true,
                HasSupport = true,
                HasActiveHeal = true,
                HasShieldSource = true,
                HasFireSource = true,
                HasFrontAndSameColumnBack = true,
                DirectAttackerCount = 3
            };
        }

        [Test]
        public void ContractContainsExactSixteenNormalEffectsAndNineKeystones()
        {
            Assert.That(ChapterOneRoguelikeV11Rules.ContractRulesVersion, Is.EqualTo(2));
            Assert.That(ChapterOneRoguelikeV11Rules.NormalDefinitions.Select(x => x.Id),
                Is.EqualTo(Enumerable.Range(1, 16).Select(x => "B" + x.ToString("00"))));
            Assert.That(ChapterOneRoguelikeV11Rules.KeystoneDefinitions.Select(x => x.Id),
                Is.EqualTo(Enumerable.Range(1, 9).Select(x => "K" + x.ToString("00"))));
            Assert.That(ChapterOneRoguelikeV11Rules.GetNormal("B09").Values,
                Is.EqualTo(new[] { 2500, 3500, 4500, 6000, 8000 }));
            Assert.That(ChapterOneRoguelikeV11Rules.GetNormal("B09").SecondaryValues,
                Is.EqualTo(new[] { 800, 1000, 1200, 1500, 2000 }));
            Assert.That(ChapterOneRoguelikeV11Rules.GetNormal("B16").Values,
                Is.EqualTo(new[] { 200, 300, 400, 600, 800 }));
        }

        [Test]
        public void RarityBandsMatchApprovedV11Contract()
        {
            Assert.That(ChapterOneRoguelikeV11Rules.RarityWeights(0),
                Is.EqualTo(new[] { 5500, 3000, 1200, 300, 0 }));
            Assert.That(ChapterOneRoguelikeV11Rules.RarityWeights(2500),
                Is.EqualTo(new[] { 4300, 3100, 1900, 600, 100 }));
            Assert.That(ChapterOneRoguelikeV11Rules.RarityWeights(5000),
                Is.EqualTo(new[] { 3200, 3000, 2500, 1000, 300 }));
            Assert.That(ChapterOneRoguelikeV11Rules.RarityWeights(7500),
                Is.EqualTo(new[] { 2000, 3000, 3000, 1500, 500 }));
            Assert.That(ChapterOneRoguelikeV11Rules.RarityWeights(9999).Sum(), Is.EqualTo(10000));
        }

        [Test]
        public void EliteGuaranteeUsesCurrentBandRareEpicLegendaryRelativeWeights()
        {
            var seen = new HashSet<RoguelikeRarity>();
            for (uint seed = 0; seed < 10000; seed++)
            {
                var rarity = ChapterOneRoguelikeV11Rules.RollEliteGuaranteedRarity(seed, 9999);
                Assert.That(rarity, Is.GreaterThanOrEqualTo(RoguelikeRarity.Rare));
                seen.Add(rarity);
            }
            Assert.That(seen, Is.EquivalentTo(new[]
            {
                RoguelikeRarity.Rare, RoguelikeRarity.Epic, RoguelikeRarity.Legendary
            }));
        }

        [Test]
        public void VictoryCadenceReplacesM19NormalChoiceAndSkipsBoss()
        {
            Assert.That(ChapterOneRoguelikeV11Rules.ChoiceKindForVictory("M18", false),
                Is.EqualTo(ChapterOneRoguelikeChoiceKind.Normal));
            Assert.That(ChapterOneRoguelikeV11Rules.ChoiceKindForVictory("M19", false),
                Is.EqualTo(ChapterOneRoguelikeChoiceKind.Keystone));
            Assert.That(ChapterOneRoguelikeV11Rules.ChoiceKindForVictory("M29", true),
                Is.EqualTo(ChapterOneRoguelikeChoiceKind.None));
        }

        [Test]
        public void PreparationBoundariesSkipKeystoneAndBlockM18BuildPrep()
        {
            Assert.That(ChapterOneRoguelikeV11Rules.PreparationAffectsChoice("P09",
                ChapterOneRoguelikeChoiceKind.Normal), Is.True);
            Assert.That(ChapterOneRoguelikeV11Rules.PreparationAffectsChoice("P09",
                ChapterOneRoguelikeChoiceKind.Keystone), Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsPreparationAllowedAtNode("P11", "M18", "M19"),
                Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsPreparationAllowedAtNode("E05", "M17", "M19"),
                Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsPreparationAllowedAtNode("P09", "M17", "M20"),
                Is.True);
        }

        [Test]
        public void EligibilityMatchesAllApprovedHardRequirements()
        {
            var empty = new ChapterOneRoguelikeEligibility { HasAnyAlly = true };
            Assert.That(ChapterOneRoguelikeV11Rules.IsNormalEligible("B01", empty), Is.True);
            Assert.That(ChapterOneRoguelikeV11Rules.IsNormalEligible("B03", empty), Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsNormalEligible("B05", empty), Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsNormalEligible("B09", empty), Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsNormalEligible("B16", empty), Is.False);
            Assert.That(ChapterOneRoguelikeV11Rules.IsKeystoneEligible("K04", empty), Is.True);
            Assert.That(ChapterOneRoguelikeV11Rules.IsKeystoneEligible("K09", empty), Is.False);

            var full = FullContext();
            foreach (var effect in ChapterOneRoguelikeV11Rules.NormalDefinitions)
                Assert.That(ChapterOneRoguelikeV11Rules.IsNormalEligible(effect.Id, full), Is.True,
                    effect.Id);
            foreach (var keystone in ChapterOneRoguelikeV11Rules.KeystoneDefinitions)
                Assert.That(ChapterOneRoguelikeV11Rules.IsKeystoneEligible(keystone.Id, full), Is.True,
                    keystone.Id);
        }

        [Test]
        public void NormalCandidatesAreDeterministicDistinctAndRespectCaps()
        {
            var acquired = new[]
            {
                new RoguelikeAcquiredEffect
                {
                    effectId = "B01", rarityId = (int)RoguelikeRarity.Legendary, stackCount = 3
                }
            };
            var first = ChapterOneRoguelikeV11Rules.GenerateNormalCandidates("run-v11", 7, 8200,
                true, FullContext(), acquired, ChapterOneLanternRoute.Break, "K08", true);
            var repeated = ChapterOneRoguelikeV11Rules.GenerateNormalCandidates("run-v11", 7, 8200,
                true, FullContext(), acquired, ChapterOneLanternRoute.Break, "K08", true);
            Assert.That(first.Select(x => x.Id), Is.EqualTo(repeated.Select(x => x.Id)));
            Assert.That(first.Select(x => x.RarityId), Is.EqualTo(repeated.Select(x => x.RarityId)));
            Assert.That(first.Select(x => x.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(first.Any(x => x.Id == "B01"), Is.False);
            Assert.That(first.Any(x => x.RarityId >= (int)RoguelikeRarity.Rare), Is.True);
        }

        [Test]
        public void KeystoneCandidatesAreDeterministicDistinctAndQualityFree()
        {
            var first = ChapterOneRoguelikeV11Rules.GenerateKeystoneCandidates("run-k", 3,
                FullContext());
            var repeated = ChapterOneRoguelikeV11Rules.GenerateKeystoneCandidates("run-k", 3,
                FullContext());
            Assert.That(first.Select(x => x.Id), Is.EqualTo(repeated.Select(x => x.Id)));
            Assert.That(first.Select(x => x.Id).Distinct().Count(), Is.EqualTo(3));
            Assert.That(first.All(x => x.RarityId == (int)RoguelikeRarity.Common), Is.True);
        }

        [Test]
        public void KeystoneConvergencePoolsMatchApprovedMappings()
        {
            Assert.That(ChapterOneRoguelikeV11Rules.KeystoneConvergencePool("K01"),
                Is.EqualTo(new[] { "B01", "B02", "B03", "B07", "B13" }));
            Assert.That(ChapterOneRoguelikeV11Rules.KeystoneConvergencePool("K06"),
                Is.EqualTo(new[] { "B01", "B04", "B05", "B10", "B13" }));
            Assert.That(ChapterOneRoguelikeV11Rules.KeystoneConvergencePool("K09"),
                Is.EqualTo(new[] { "B01", "B02", "B03", "B09", "B11", "B12", "B16" }));
        }

        [Test]
        public void M19StateSelectsExactlyOneKeystoneAndRestoresIt()
        {
            var state = new ChapterOneRoguelikeV11State();
            var set = state.EnsureChoiceSet("run-m19", "M19", false, 6500, true,
                FullContext(), ChapterOneLanternRoute.Break, true, true, true);
            Assert.That((ChapterOneRoguelikeChoiceKind)set.choiceKind,
                Is.EqualTo(ChapterOneRoguelikeChoiceKind.Keystone));
            Assert.That(set.requiredSelectionCount, Is.EqualTo(1));
            Assert.That(set.preparationP11DoubleStack, Is.False);
            var receipt = state.Select(set.choiceSetId,
                ChapterOneRoguelikeChoiceSetV11.OptionId(0), "m19-select", 0);
            Assert.That((ChapterOneRoguelikeSelectionCode)receipt.resultCode,
                Is.EqualTo(ChapterOneRoguelikeSelectionCode.Success));
            Assert.That(state.SelectedKeystoneId, Is.EqualTo(set.options[0].Id));
            Assert.That(state.CompleteSelection(), Is.True);

            var restored = new ChapterOneRoguelikeV11State();
            restored.Restore(ChapterOneRoguelikeV11State.RuntimeGeneration, state.StateVersion,
                state.ChoiceOrdinal, state.SelectedKeystoneId, null, state.CopyNormalEffects(),
                state.CopyReceipts());
            Assert.That(restored.SelectedKeystoneId, Is.EqualTo(state.SelectedKeystoneId));
            Assert.That(restored.ChoiceOrdinal, Is.EqualTo(1));
        }

        [Test]
        public void P09RequiresTwoDistinctSelectionsAndP11AddsSameRarityStack()
        {
            var state = new ChapterOneRoguelikeV11State();
            var set = state.EnsureChoiceSet("run-prep", "M12", false, 4200, false,
                FullContext(), ChapterOneLanternRoute.Wall, true, false, true);
            Assert.That(set.requiredSelectionCount, Is.EqualTo(2));
            var firstIndex = System.Array.FindIndex(set.options,
                x => ChapterOneRoguelikeV11Rules.GetNormal(x.Id).MaximumStacks >= 2);
            Assert.That(firstIndex, Is.GreaterThanOrEqualTo(0));
            var first = state.Select(set.choiceSetId,
                ChapterOneRoguelikeChoiceSetV11.OptionId(firstIndex), "select-1", 0);
            Assert.That(first.acquiredStackCount, Is.EqualTo(2));
            Assert.That(state.CompleteSelection(), Is.False);
            var replay = state.Select(set.choiceSetId,
                ChapterOneRoguelikeChoiceSetV11.OptionId(firstIndex), "select-1", 0);
            Assert.That(replay.isReplay, Is.True);
            Assert.That(replay.resultDigest, Is.EqualTo(first.resultDigest));
            var secondIndex = firstIndex == 0 ? 1 : 0;
            var second = state.Select(set.choiceSetId,
                ChapterOneRoguelikeChoiceSetV11.OptionId(secondIndex), "select-2", 1);
            Assert.That((ChapterOneRoguelikeSelectionCode)second.resultCode,
                Is.EqualTo(ChapterOneRoguelikeSelectionCode.Success));
            Assert.That(state.CompleteSelection(), Is.True);
            Assert.That(state.NormalEffects.Count, Is.EqualTo(2));
            Assert.That(second.acquiredStackCount, Is.EqualTo(System.Math.Min(2,
                ChapterOneRoguelikeV11Rules.GetNormal(set.options[secondIndex].Id).MaximumStacks)));
        }

        [Test]
        public void P11StopsAtRemainingStackCapacity()
        {
            var state = new ChapterOneRoguelikeV11State();
            state.Restore(ChapterOneRoguelikeV11State.RuntimeGeneration, 4, 2, string.Empty,
                new ChapterOneRoguelikeChoiceSetV11
                {
                    runId = "run-cap",
                    choiceSetId = "manual-cap",
                    choiceOrdinal = 2,
                    choiceKind = (int)ChapterOneRoguelikeChoiceKind.Normal,
                    options = new[]
                    {
                        new ChapterOneRoguelikeCandidate
                        {
                            Id = "B01", RarityId = (int)RoguelikeRarity.Legendary
                        }
                    },
                    selectedOptionIds = System.Array.Empty<string>(),
                    requiredSelectionCount = 1,
                    expectedRunStateVersion = 4,
                    contractRulesVersion = ChapterOneRoguelikeV11Rules.ContractRulesVersion,
                    contractId = ChapterOneRoguelikeV11Rules.ContractId,
                    preparationP11DoubleStack = true
                }, new[]
                {
                    new RoguelikeAcquiredEffect
                    {
                        effectId = "B01", rarityId = (int)RoguelikeRarity.Common,
                        stackCount = 2, acquiredAtRunStateVersion = 2
                    }
                }, null);
            var receipt = state.Select("manual-cap",
                ChapterOneRoguelikeChoiceSetV11.OptionId(0), "select-cap", 4);
            Assert.That(receipt.acquiredStackCount, Is.EqualTo(1));
            Assert.That(RoguelikeEnhancementRules.StackCount(state.NormalEffects, "B01"), Is.EqualTo(3));
        }

        [Test]
        public void RestoreRejectsWrongRuntimeGenerationAndPendingContract()
        {
            var state = new ChapterOneRoguelikeV11State();
            Assert.Throws<System.InvalidOperationException>(() => state.Restore(2, 0, 0,
                string.Empty, null, null, null));
            Assert.Throws<System.InvalidOperationException>(() => state.Restore(
                ChapterOneRoguelikeV11State.RuntimeGeneration, 0, 0, string.Empty,
                new ChapterOneRoguelikeChoiceSetV11
                {
                    contractRulesVersion = 1,
                    contractId = "legacy"
                }, null, null));
        }

        [Test]
        public void PendingChoiceCompatibilityIdentifiesCurrentNullAndLegacyContracts()
        {
            Assert.That(ChapterOneRoguelikeV11State.IsPendingChoiceCompatible(null), Is.True);
            Assert.That(ChapterOneRoguelikeV11State.IsPendingChoiceCompatible(
                new ChapterOneRoguelikeChoiceSetV11
                {
                    contractRulesVersion = ChapterOneRoguelikeV11Rules.ContractRulesVersion,
                    contractId = ChapterOneRoguelikeV11Rules.ContractId
                }), Is.True);
            Assert.That(ChapterOneRoguelikeV11State.IsPendingChoiceCompatible(
                new ChapterOneRoguelikeChoiceSetV11
                {
                    contractRulesVersion = ChapterOneRoguelikeV11Rules.ContractRulesVersion - 1,
                    contractId = ChapterOneRoguelikeV11Rules.ContractId
                }), Is.False);
        }

        [Test]
         public void V11BattleStartAppliesB16AndK09OnlyForThreeProfessionRoster()
        {
            var input = ThreeProfessionBattleInput();
            input.PlayerRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration;
            input.PlayerKeystoneId = "K09";
            input.PlayerRoguelikeEffects.Add(new RoguelikeAcquiredEffect
            {
                effectId = "B16", rarityId = (int)RoguelikeRarity.Rare, stackCount = 1
            });
            var result = DeterministicBattle.Run(input);
            var guard = result.PlayerUnits.Single(x => x.ProfessionId == "profession.guardian");
            var output = result.PlayerUnits.Single(x => x.ProfessionId == "profession.output");
            var support = result.PlayerUnits.Single(x => x.ProfessionId == "profession.support");
            Assert.That(guard.AttackDamage, Is.EqualTo(104));
            Assert.That(output.AttackDamage, Is.EqualTo(124));
            Assert.That(support.AttackDamage, Is.EqualTo(104));
            Assert.That(result.PlayerUnits.All(x => x.MaxHealth == 104), Is.True);
            Assert.That(guard.Armor, Is.EqualTo(20));

            var incomplete = ThreeProfessionBattleInput();
            incomplete.PlayerUnits.RemoveAt(2);
            incomplete.PlayerRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration;
            incomplete.PlayerKeystoneId = "K09";
            incomplete.PlayerRoguelikeEffects.Add(new RoguelikeAcquiredEffect
            {
                effectId = "B16", rarityId = (int)RoguelikeRarity.Legendary, stackCount = 1
            });
            var incompleteResult = DeterministicBattle.Run(incomplete);
            Assert.That(incompleteResult.PlayerUnits.All(x => x.AttackDamage == 100 &&
                x.MaxHealth == 100), Is.True);
             Assert.That(incompleteResult.PlayerUnits[0].Armor, Is.EqualTo(10));
         }

         [Test]
         public void V11DirectAttackAppliesB11ExecuteAndB12SameColumnProtection()
         {
             var execute = new BattleInput
             {
                 Seed = 11u,
                 PlayerRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration
             };
             execute.PlayerUnits.Add(new BattleUnitState
             {
                 Id = "execute-output", IsPlayer = true, Slot = 0, Health = 100,
                 MaxHealth = 100, AttackDamage = 100, Armor = 0,
                 BaseAttackSpeedBasisPoints = 20000, ProfessionId = "profession.output"
             });
             execute.EnemyUnits.Add(new BattleUnitState
             {
                 Id = "execute-target", IsPlayer = false, Slot = 0, Health = 50,
                 MaxHealth = 100, AttackDamage = 1, Armor = 0,
                 BaseAttackSpeedBasisPoints = 1000, ProfessionId = "profession.guardian"
             });
             execute.PlayerRoguelikeEffects.Add(new RoguelikeAcquiredEffect
             {
                 effectId = "B11", rarityId = (int)RoguelikeRarity.Common, stackCount = 1
             });
             var executeResult = DeterministicBattle.Run(execute);
             var executeHit = executeResult.Events.First(x =>
                 x.Type == BattleEventType.Attack && x.ActorId == "execute-output" &&
                 x.TargetId == "execute-target" && !x.IsSecondary);
             Assert.That(executeHit.Amount, Is.EqualTo(107));

             var escort = new BattleInput
             {
                 Seed = 12u,
                 PlayerRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration
             };
             escort.PlayerUnits.Add(new BattleUnitState
             {
                 Id = "escort-front", IsPlayer = true, Slot = 0, Health = 1000,
                 MaxHealth = 1000, AttackDamage = 1, Armor = 0,
                 BaseAttackSpeedBasisPoints = 1000, ProfessionId = "profession.guardian"
             });
             escort.PlayerUnits.Add(new BattleUnitState
             {
                 Id = "escort-rear", IsPlayer = true, Slot = 3, Health = 1000,
                 MaxHealth = 1000, AttackDamage = 1, Armor = 0,
                 BaseAttackSpeedBasisPoints = 1000, ProfessionId = "profession.support"
             });
             escort.EnemyUnits.Add(new BattleUnitState
             {
                 Id = "escort-enemy", IsPlayer = false, Slot = 3, Health = 1000,
                 MaxHealth = 1000, AttackDamage = 100, Armor = 0,
                 BaseAttackSpeedBasisPoints = 20000, ProfessionId = "profession.output"
             });
             escort.PlayerRoguelikeEffects.Add(new RoguelikeAcquiredEffect
             {
                 effectId = "B12", rarityId = (int)RoguelikeRarity.Common, stackCount = 1
             });
             var escortResult = DeterministicBattle.Run(escort);
             var protectedHit = escortResult.Events.First(x =>
                 x.Type == BattleEventType.Attack && x.ActorId == "escort-enemy" &&
                 x.TargetId == "escort-rear" && !x.IsSecondary);
             Assert.That(protectedHit.Amount, Is.EqualTo(96));
         }

         [Test]
         public void V11K09RaisesSupportActiveHealingAfterThreeProfessionLock()
         {
             var input = ThreeProfessionBattleInput();
             input.PlayerRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration;
             input.PlayerKeystoneId = "K09";
             var guard = input.PlayerUnits.Single(x => x.ProfessionId == "profession.guardian");
             guard.Health = 50;
             guard.MaxHealth = 200;
             var support = input.PlayerUnits.Single(x => x.ProfessionId == "profession.support");
             support.IsHealer = true;
             support.HealAmount = 100;
             support.BaseAttackSpeedBasisPoints = 20000;
             input.EnemyUnits[0].BaseAttackSpeedBasisPoints = 1000;

             var result = DeterministicBattle.Run(input);
             var heal = result.Events.First(x => x.Type == BattleEventType.Heal &&
                 x.ActorId == support.Id && x.TargetId == guard.Id);
             Assert.That(heal.Amount, Is.EqualTo(125));
         }

         [Test]
         public void V11BattleInputRejectsUnknownGenerationAndKeystone()
        {
            var wrongGeneration = ThreeProfessionBattleInput();
            wrongGeneration.PlayerRoguelikeRuntimeGeneration = 2;
            Assert.Throws<System.InvalidOperationException>(() =>
                DeterministicBattle.Run(wrongGeneration));

            var wrongKeystone = ThreeProfessionBattleInput();
            wrongKeystone.PlayerRoguelikeRuntimeGeneration =
                ChapterOneRoguelikeV11State.RuntimeGeneration;
            wrongKeystone.PlayerKeystoneId = "K99";
            Assert.Throws<System.Collections.Generic.KeyNotFoundException>(() =>
                DeterministicBattle.Run(wrongKeystone));
        }

        private static BattleInput ThreeProfessionBattleInput()
        {
            var input = new BattleInput { Seed = 20261008u };
            input.PlayerUnits.Add(Unit("guard", 0, "profession.guardian", 10));
            input.PlayerUnits.Add(Unit("output", 1, "profession.output", 5));
            input.PlayerUnits.Add(Unit("support", 2, "profession.support", 3));
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "enemy", IsPlayer = false, Slot = 0, Health = 100000,
                MaxHealth = 100000, AttackDamage = 1, Armor = 0,
                BaseAttackSpeedBasisPoints = 8000, ProfessionId = "profession.output"
            });
            return input;
        }

        private static BattleUnitState Unit(string id, int slot, string professionId, int armor)
        {
            return new BattleUnitState
            {
                Id = id, IsPlayer = true, Slot = slot, Health = 100, MaxHealth = 100,
                AttackDamage = 100, Armor = armor, BaseAttackSpeedBasisPoints = 8000,
                ProfessionId = professionId
            };
        }
    }
}
