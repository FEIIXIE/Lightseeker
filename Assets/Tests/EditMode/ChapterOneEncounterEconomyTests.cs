using System.Linq;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ChapterOneEncounterEconomyTests
    {
        [Test]
        public void EncounterGeneration_IsDeterministicForRunAndNode()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(81234u);
            var node = map.GetNode("M19");
            var first = ChapterOneEncounterRules.Generate(map.Seed, node, false);
            var second = ChapterOneEncounterRules.Generate(map.Seed, node, false);

            Assert.That(second.BattleSeed, Is.EqualTo(first.BattleSeed));
            Assert.That(second.CoinReward, Is.EqualTo(first.CoinReward));
            Assert.That(second.Units.Select(unit => unit.DefinitionId + ":" + unit.RunLevel),
                Is.EqualTo(first.Units.Select(unit => unit.DefinitionId + ":" + unit.RunLevel)));
        }

        [Test]
        public void FirstActualNormalBattle_IsTwoDifferentOneStarUnitsWithoutSupport()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(77u);
            var normal = map.Nodes.First(node => node.Type == ExpeditionNodeType.NormalBattle);
            var encounter = ChapterOneEncounterRules.Generate(map.Seed, normal, true);

            Assert.That(encounter.Units.Count, Is.EqualTo(2));
            Assert.That(encounter.Units.All(unit => unit.RunLevel == 1), Is.True);
            Assert.That(encounter.Units.Select(unit => unit.DefinitionId).Distinct().Count(), Is.EqualTo(2));
            Assert.That(encounter.Units.Any(unit => unit.DefinitionId == "enemy.n5.support"), Is.False);
        }

        [Test]
        public void FixedNodes_UseGuardianSuppliesAndBossContracts()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(991u);
            var guardian = ChapterOneEncounterRules.Generate(map.Seed, map.GetNode("M19"), false);
            var boss = ChapterOneEncounterRules.Generate(map.Seed, map.GetNode("M29"), false);

            Assert.That(guardian.IsElite, Is.True);
            Assert.That(guardian.CoinReward, Is.EqualTo(8));
            Assert.That(guardian.Units.Count, Is.EqualTo(4));
            Assert.That(guardian.Units.Count(unit => unit.RunLevel == 2), Is.EqualTo(2));
            Assert.That(map.GetNode("M18").Type, Is.EqualTo(ExpeditionNodeType.Supply));
            Assert.That(map.GetNode("M28").Type, Is.EqualTo(ExpeditionNodeType.Supply));
            Assert.That(boss.IsBoss, Is.True);
            Assert.That(boss.CoinReward, Is.Zero);
            Assert.That(boss.Units.Count, Is.EqualTo(4));
            Assert.That(boss.Units.Skip(1).All(unit => unit.RunLevel == 2), Is.True);
        }

        [Test]
        public void LegacyM28FixedCombat_RemainsRecoverableForVersion8Runs()
        {
            var map = ExpeditionMapGraph.CreateChapterOne(
                5u, ExpeditionMapGraph.LegacyFiveTypeMapGenerationVersion);
            var encounter = ChapterOneEncounterRules.Generate(map.Seed, map.GetNode("M28"), true);

            Assert.That(encounter.CoinReward, Is.EqualTo(6));
            Assert.That(encounter.Units.Any(unit => unit.RunLevel == 3), Is.True);
            Assert.That(encounter.Units.Count, Is.EqualTo(3));
        }

        [TestCase(1u, "enemy.b2.hunter")]
        [TestCase(2u, "enemy.b1.heavy")]
        [TestCase(3u, "enemy.b2.hunter")]
        [TestCase(4u, "enemy.b2.hunter")]
        [TestCase(6u, "enemy.b1.heavy")]
        [TestCase(42u, "enemy.b2.hunter")]
        public void FixedSeeds_LockReviewedBossFamily(uint seed, string expectedBossId)
        {
            var map = ExpeditionMapGraph.CreateChapterOne(seed);
            var boss = ChapterOneEncounterRules.Generate(seed, map.GetNode("M29"), false);

            Assert.That(boss.Units[0].DefinitionId, Is.EqualTo(expectedBossId));
            Assert.That(boss.Units.Skip(1).All(unit => unit.RunLevel == 2), Is.True);
            Assert.That(boss.CoinReward, Is.Zero);
        }

        [Test]
        public void EveryGeneratedBattle_UsesTheReviewedCoinBand()
        {
            for (uint seed = 1; seed <= 200; seed++)
            {
                var map = ExpeditionMapGraph.CreateChapterOne(seed);
                foreach (var node in map.Nodes.Where(node =>
                             node.Type == ExpeditionNodeType.NormalBattle ||
                             node.Type == ExpeditionNodeType.EliteBattle ||
                             node.Type == ExpeditionNodeType.Boss))
                {
                    var encounter = ChapterOneEncounterRules.Generate(seed, node, false);
                    var expected = ExpectedCoinReward(node);
                    Assert.That(encounter.CoinReward, Is.EqualTo(expected), seed + ":" + node.Id);
                }
            }
        }

        [Test]
        public void ThreeStarNormals_AreForbiddenBeforeEightyFivePercent()
        {
            for (uint seed = 1; seed <= 200; seed++)
            {
                var map = ExpeditionMapGraph.CreateChapterOne(seed);
                foreach (var node in map.Nodes.Where(node =>
                             node.Type == ExpeditionNodeType.NormalBattle && node.JourneyProgress < 85f))
                {
                    var encounter = ChapterOneEncounterRules.Generate(seed, node, false);
                    Assert.That(encounter.Units.All(unit => unit.RunLevel < 3), Is.True,
                        seed + ":" + node.Id);
                }
            }
        }

        [Test]
        public void EveryGeneratedEnemyId_ExistsInTheRuntimeCharacterCatalog()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            for (uint seed = 1; seed <= 200; seed++)
            {
                var map = ExpeditionMapGraph.CreateChapterOne(seed);
                foreach (var node in map.Nodes.Where(node =>
                             node.Type == ExpeditionNodeType.NormalBattle ||
                             node.Type == ExpeditionNodeType.EliteBattle ||
                             node.Type == ExpeditionNodeType.Boss))
                {
                    var encounter = ChapterOneEncounterRules.Generate(seed, node, false);
                    foreach (var unit in encounter.Units)
                        Assert.That(catalog.Contains(unit.DefinitionId), Is.True,
                            seed + ":" + node.Id + ":" + unit.DefinitionId);
                }
            }
        }

        [TestCase(0f, 3)]
        [TestCase(44.999f, 3)]
        [TestCase(45f, 4)]
        [TestCase(62.299f, 4)]
        [TestCase(62.3f, 5)]
        [TestCase(84.999f, 5)]
        [TestCase(85f, 6)]
        public void NonBattleCoinBands_UseLeftClosedRightOpenBoundaries(float progress, int supply)
        {
            Assert.That(ChapterOneExpeditionEconomy.SupplyCoinAmount(progress), Is.EqualTo(supply));
        }

        [Test]
        public void Merchant_HasOneDistinctOfferFromEachCategory()
        {
            var offers = ChapterOneExpeditionEconomy.GenerateShopItemIds(42u, "M14", 70f, 1);
            Assert.That(offers.Length, Is.EqualTo(3));
            Assert.That(offers.Distinct().Count(), Is.EqualTo(3));
            Assert.That(offers.All(id => id.StartsWith("I")), Is.True);
            Assert.That(offers.Count(id => id == "I01" || id == "I02" || id == "I03"), Is.EqualTo(1));
            Assert.That(offers.Count(id => id == "I04" || id == "I05" || id == "I06"), Is.EqualTo(1));
            Assert.That(offers.Count(id => id == "I07" || id == "I08" || id == "I09"), Is.EqualTo(1));
        }

        [Test]
        public void SupplyRecruitmentOption_GrantsCoinsByBandWithoutCreatingAUnitAmount()
        {
            Assert.That(ChapterOneExpeditionEconomy.SupplyRecruitCoinAmount(0f), Is.EqualTo(1));
            Assert.That(ChapterOneExpeditionEconomy.SupplyRecruitCoinAmount(45f), Is.EqualTo(2));
            Assert.That(ChapterOneExpeditionEconomy.SupplyRecruitCoinAmount(85f), Is.EqualTo(3));
        }

        [Test]
        public void TacticalCandidates_AreDeterministicDistinctAndBandLimited()
        {
            var early = ChapterOneExpeditionEconomy.GenerateCombatItemIds(9u, "M04", 30f, 2, "supply");
            var repeated = ChapterOneExpeditionEconomy.GenerateCombatItemIds(9u, "M04", 30f, 2, "supply");
            var late = ChapterOneExpeditionEconomy.GenerateCombatItemIds(9u, "M25", 90f, 3, "treasure");

            Assert.That(repeated, Is.EqualTo(early));
            Assert.That(early.Distinct().Count(), Is.EqualTo(2));
            Assert.That(early.All(id => id == "I01" || id == "I02"), Is.True);
            Assert.That(late.Distinct().Count(), Is.EqualTo(3));
            Assert.That(late.All(id => string.CompareOrdinal(id, "I05") <= 0), Is.True);
        }

        [Test]
        public void EventSelection_RespectsMoraleBackpackAndLateProgressConditions()
        {
            for (uint seed = 1; seed <= 200; seed++)
            {
                var fullMorale = ChapterOneExpeditionEconomy.GenerateEventId(seed, "M08", 50f, 2, false);
                Assert.That(fullMorale, Is.Not.EqualTo("V05"));
                Assert.That(fullMorale, Is.Not.EqualTo("V07"));

                var late = ChapterOneExpeditionEconomy.GenerateEventId(seed, "M24", 90f, 1, true);
                Assert.That(late, Is.Not.EqualTo("V02"));
                Assert.That(late, Is.Not.EqualTo("V04"));
                Assert.That(late, Is.Not.EqualTo("V08"));
            }
        }

        [Test]
        public void MerchantInventory_IsIndependentOfMoraleAndAlwaysComplete()
        {
            for (uint seed = 1; seed <= 200; seed++)
            {
                var lowMorale = ChapterOneExpeditionEconomy.GenerateShopItemIds(seed, "M06", 30f, 1);
                var fullMorale = ChapterOneExpeditionEconomy.GenerateShopItemIds(seed, "M06", 30f, 2);
                Assert.That(fullMorale, Is.EqualTo(lowMorale));
                Assert.That(fullMorale.Length, Is.EqualTo(3));
            }
        }

        [Test]
        public void GlobalFreeRerollAndVoucher_DoNotStackOrConsumeTogether()
        {
            var state = new ExpeditionEconomyState();
            var coins = 0;
            state.AddRerollVoucher(ref coins);

            Assert.That(state.ConsumeManualRerollCost(), Is.Zero);
            Assert.That(state.RecruitRerollVouchers, Is.EqualTo(1));
            Assert.That(state.ConsumeManualRerollCost(), Is.Zero);
            Assert.That(state.RecruitRerollVouchers, Is.Zero);
            Assert.That(state.ConsumeManualRerollCost(), Is.EqualTo(2));
        }

        [Test]
        public void RecruitmentDiscounts_UseOnlyTheLargerAndPreserveTheOther()
        {
            var state = new ExpeditionEconomyState();
            state.SetRecruitmentDiscount(3, false);
            state.SetRecruitmentDiscount(4, true);

            Assert.That(state.QuoteRecruitmentPrice(3), Is.Zero);
            Assert.That(state.ApplyRecruitmentDiscount(3), Is.Zero);
            Assert.That(state.RecruitmentDiscount, Is.EqualTo(3));
            Assert.That(state.ChestRecruitmentDiscount, Is.Zero);
            Assert.That(state.ApplyRecruitmentDiscount(3), Is.Zero);
            Assert.That(state.RecruitmentDiscount, Is.Zero);
        }

        [Test]
        public void CurrentRecruitPrices_IgnoreQualityAndCapturedV1RunsRemainCompatible()
        {
            var qualities = new[]
            {
                CharacterQuality.Common,
                CharacterQuality.Uncommon,
                CharacterQuality.Rare,
                CharacterQuality.Epic,
                CharacterQuality.Legendary
            };
            var ordinaryLevelOne = new[] { 3, 4, 5, 6, 7 };
            var coreLevelOne = new[] { 5, 6, 8, 9, 11 };
            for (var index = 0; index < qualities.Length; index++)
            {
                for (var level = 1; level <= 3; level++)
                {
                    var multiplier = level == 1 ? 1 : level == 2 ? 3 : 9;
                    Assert.That(QualityRecruitPriceRules.QuoteListedPrice(
                            3, qualities[index], level),
                        Is.EqualTo(3 * multiplier));
                    Assert.That(QualityRecruitPriceRules.QuoteListedPrice(
                            5, qualities[index], level),
                        Is.EqualTo(5 * multiplier));
                    Assert.That(QualityRecruitPriceRules.QuoteListedPrice(
                            3, qualities[index], level,
                            QualityRecruitPriceRules.QualityScaledRulesVersion),
                        Is.EqualTo(ordinaryLevelOne[index] * multiplier));
                    Assert.That(QualityRecruitPriceRules.QuoteListedPrice(
                            5, qualities[index], level,
                            QualityRecruitPriceRules.QualityScaledRulesVersion),
                        Is.EqualTo(coreLevelOne[index] * multiplier));
                }
            }
        }

        [Test]
        public void LegacyRecruitPrices_RemainQualityAgnosticForOldRuns()
        {
            foreach (CharacterQuality quality in System.Enum.GetValues(typeof(CharacterQuality)))
            {
                Assert.That(QualityRecruitPriceRules.QuoteListedPrice(
                        3, quality, 2, QualityRecruitPriceRules.LegacyRulesVersion),
                    Is.EqualTo(9));
                Assert.That(QualityRecruitPriceRules.QuoteListedPrice(
                        5, quality, 3, QualityRecruitPriceRules.LegacyRulesVersion),
                    Is.EqualTo(45));
            }
        }

        [Test]
        public void BackpackAndBoundBattleItem_RestoreWithoutOverflowOrDuplication()
        {
            var state = new ExpeditionEconomyState();
            Assert.That(state.TryAddCombatItem("I01"), Is.True);
            Assert.That(state.TryAddCombatItem("I02"), Is.True);
            Assert.That(state.TryAddCombatItem("I03"), Is.True);
            Assert.That(state.TryAddCombatItem("I04"), Is.False);
            Assert.That(state.TryActivateBattleItem(1, 4), Is.True);
            Assert.That(state.TryActivateBattleItem(0, 1), Is.False);

            var restored = new ExpeditionEconomyState();
            restored.Restore(
                true, 1, 3, 2, 1, "fortify8",
                state.Backpack, new[] { "M04:choice:coins" },
                state.ActiveBattleItemId, state.ActiveBattleTargetSlot);

            Assert.That(restored.Backpack, Is.EqualTo(new[] { "I01", "I03" }));
            Assert.That(restored.ActiveBattleItemId, Is.EqualTo("I02"));
            Assert.That(restored.ActiveBattleTargetSlot, Is.EqualTo(-1));
            Assert.That(restored.FirstManualRecruitRerollUsed, Is.True);
            Assert.That(restored.RecruitRerollVouchers, Is.EqualTo(1));
            Assert.That(restored.Morale, Is.EqualTo(1));
            Assert.That(restored.NextBattlePreparationId, Is.EqualTo("fortify8"));
            Assert.That(restored.HasResolvedAction("M04:choice:coins"), Is.True);
        }

        [Test]
        public void EventAndShopContent_LockOnceAndSurviveRestore()
        {
            var state = new ExpeditionEconomyState();
            string first;
            string repeated;
            Assert.That(state.GetOrLockNodeContent("event:M08", "V05", out first), Is.True);
            Assert.That(state.GetOrLockNodeContent("event:M08", "V01", out repeated), Is.False);
            Assert.That(first, Is.EqualTo("V05"));
            Assert.That(repeated, Is.EqualTo("V05"));
            string shop;
            Assert.That(state.GetOrLockNodeContent(
                "shop:M10", "I01,I06,I07,I08", out shop), Is.True);

            var restored = new ExpeditionEconomyState();
            restored.Restore(
                false, 0, 0, 0, 2, null, new string[0], new string[0],
                null, -1, state.CopyLockedNodeContentKeys(), state.CopyLockedNodeContentValues());
            string restoredEvent;
            string restoredShop;
            Assert.That(restored.GetOrLockNodeContent("event:M08", "V03", out restoredEvent), Is.False);
            Assert.That(restored.GetOrLockNodeContent("shop:M10", "I02,I03,I04,I05", out restoredShop), Is.False);
            Assert.That(restoredEvent, Is.EqualTo("V05"));
            Assert.That(restoredShop, Is.EqualTo("I01,I06,I07,I08"));
        }

        [Test]
        public void DuplicateActionAndNodeSettlement_AreRejected()
        {
            var state = new ExpeditionEconomyState();
            Assert.That(state.TryResolveAction("M07:choice:1"), Is.True);
            Assert.That(state.TryResolveAction("M07:choice:1"), Is.False);

            var ledger = new ExpeditionRewardLedger();
            Assert.That(ledger.TryRecordCompletedNode("M07", 0), Is.True);
            Assert.That(ledger.HasCompletedNode("M07"), Is.True);
            Assert.That(ledger.TryRecordCompletedNode("M07", 0), Is.False);
        }

        [Test]
        public void TacticalArmorOilAndHuntersMark_AffectDeterministicDamage()
        {
            var baseline = FirstAttackDamage(0, 0, 0);
            var penetrated = FirstAttackDamage(15, 0, 0);
            var marked = FirstAttackDamage(0, 12, 5);

            Assert.That(penetrated, Is.GreaterThan(baseline));
            Assert.That(marked, Is.GreaterThan(baseline));
        }

        private static int FirstAttackDamage(int penetration, int damageTakenPercent, int markedHits)
        {
            var input = new BattleInput { Seed = 123u };
            input.PlayerUnits.Add(new BattleUnitState
            {
                Id = "p", IsPlayer = true, Slot = 0, Health = 200, MaxHealth = 200,
                AttackDamage = 20, ArmorPenetration = penetration, SpeedTier = CombatSpeedTier.Fast
            });
            input.EnemyUnits.Add(new BattleUnitState
            {
                Id = "e", IsPlayer = false, Slot = 0, Health = 200, MaxHealth = 200,
                AttackDamage = 1, Armor = 50, DamageTakenPercent = damageTakenPercent,
                DamageTakenBoostHitsRemaining = markedHits, SpeedTier = CombatSpeedTier.Slow
            });
            var result = DeterministicBattle.Run(input);
            return result.Events.First(evt => evt.Type == BattleEventType.Attack && evt.ActorId == "p").Amount;
        }

        private static int ExpectedCoinReward(ExpeditionMapNode node)
        {
            if (node.Id == "M19") return 8;
            if (node.Id == "M28") return 6;
            if (node.Id == "M29" || node.Type == ExpeditionNodeType.Boss) return 0;
            if (node.Type == ExpeditionNodeType.EliteBattle)
            {
                if (node.JourneyProgress < 45f) return 6;
                if (node.JourneyProgress < 62.3f) return 7;
                if (node.JourneyProgress < 85f) return 8;
                return 9;
            }
            if (node.JourneyProgress < 20f) return 3;
            if (node.JourneyProgress < 45f) return 4;
            if (node.JourneyProgress < 85f) return 5;
            return 6;
        }
    }
}
