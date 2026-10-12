using System;
using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class LanternContractsTests
    {
        [Test]
        public void CircularArtContractMapsThreeFormsAndFiveQualitiesWithoutResizing()
        {
            Assert.That(LanternArtContract.NativeSize, Is.EqualTo(142));
            Assert.That(LanternArtContract.ResourceId(
                    LanternArtContract.BreakwaterTemplateId, LanternQuality.Common),
                Does.EndWith("common/ART-LANTERN-CH01-GUARDTIDE-B-RING-A1-BREAKWATER-COMMON-142-v1.0.0"));
            Assert.That(LanternArtContract.ResourceId(
                    LanternArtContract.LayeredShellTemplateId, LanternQuality.Uncommon),
                Does.EndWith("uncommon/ART-LANTERN-CH01-GUARDTIDE-B-RING-A2-LAYERED-SHELL-UNCOMMON-142-v1.0.0"));
            Assert.That(LanternArtContract.ResourceId(
                    LanternArtContract.TideEchoTemplateId, LanternQuality.Legendary),
                Does.EndWith("legendary/ART-LANTERN-CH01-GUARDTIDE-B-RING-A3-TIDE-ECHO-LEGENDARY-142-v1.0.0"));

            foreach (LanternQuality quality in Enum.GetValues(typeof(LanternQuality)))
                Assert.That(LanternArtContract.ResourceId(
                    LanternArtContract.BreakwaterTemplateId, quality), Does.Contain("-142-v1.0.0"));
        }

        [Test]
        public void HomeModularArtContractCoversSixAppearancesAndFiveIndependentQualities()
        {
            Assert.That(HomeLanternModularArtContract.PackageVersion,
                Is.EqualTo("home-lantern-modular-combination-v1.1.0"));
            Assert.That(HomeLanternModularArtContract.QualityBackgroundRoot,
                Does.Contain("HomeLanternModular/V110/"));
            Assert.That(HomeLanternModularArtContract.PatternRoot,
                Does.Contain("HomeLanternModular/V110/"));
            var resourceIds = new HashSet<string>();
            foreach (LanternQuality quality in Enum.GetValues(typeof(LanternQuality)))
            {
                var qualityResourceId =
                    HomeLanternModularArtContract.QualityBackgroundResourceId(quality);
                Assert.That(qualityResourceId, Does.Contain("-" +
                    quality.ToString().ToUpperInvariant() + "-142-v1.1.0"));

                for (var appearanceIndex = 1; appearanceIndex <= 6; appearanceIndex++)
                {
                    var appearanceId = "L" + appearanceIndex.ToString("00");
                    Assert.That(HomeLanternModularArtContract.IsValidAppearanceId(appearanceId),
                        Is.True);
                    Assert.That(HomeLanternModularArtContract.PatternResourceId(appearanceId),
                        Does.EndWith(appearanceId + "-142-v1.1.0"));
                    resourceIds.Add(qualityResourceId + "+" +
                                    HomeLanternModularArtContract.PatternResourceId(appearanceId));
                }
            }

            Assert.That(resourceIds.Count, Is.EqualTo(30));
            Assert.That(HomeLanternModularArtContract.ResolveAppearanceId(
                LanternArtContract.BreakwaterTemplateId), Is.EqualTo("L01"));
            Assert.That(HomeLanternModularArtContract.ResolveAppearanceId(
                LanternArtContract.LayeredShellTemplateId), Is.EqualTo("L02"));
            Assert.That(HomeLanternModularArtContract.ResolveAppearanceId(
                LanternArtContract.TideEchoTemplateId), Is.EqualTo("L03"));

            for (var appearanceIndex = 1;
                 appearanceIndex <= HomeLanternModularArtContract.AppearanceCount;
                 appearanceIndex++)
            {
                var appearanceId = "L" + appearanceIndex.ToString("00");
                Assert.That(HomeLanternModularArtContract.ResolveAppearanceId(
                    HomeLanternModularArtContract.TemplateIdForAppearanceId(appearanceId)),
                    Is.EqualTo(appearanceId));
            }
        }

        [Test]
        public void GeneratedLanternsUseAllSixAppearancesAndPersistValidQualityRolls()
        {
            var appearances = new HashSet<string>();
            foreach (LanternQuality quality in Enum.GetValues(typeof(LanternQuality)))
            {
                for (var roll = 0; roll < 120; roll++)
                {
                    var seed = "settlement:" + quality + ":" + roll;
                    var first = FirstChapterLanternEconomyRules.CreateGeneratedInstance(
                        "generated." + quality + "." + roll, quality, seed,
                        "receipt." + quality + "." + roll);
                    var replay = FirstChapterLanternEconomyRules.CreateGeneratedInstance(
                        "generated." + quality + "." + roll, quality, seed,
                        "receipt." + quality + "." + roll);

                    Assert.That(FirstChapterLanternEconomyRules.MatchesConfirmedQualityContract(first),
                        Is.True, seed);
                    Assert.That(replay.TemplateId, Is.EqualTo(first.TemplateId), seed);
                    Assert.That(replay.BaseFlameValue, Is.EqualTo(first.BaseFlameValue), seed);
                    Assert.That(replay.ModifierRolls.Count, Is.EqualTo(first.ModifierRolls.Count), seed);
                    for (var modifierIndex = 0;
                         modifierIndex < first.ModifierRolls.Count;
                         modifierIndex++)
                    {
                        Assert.That(replay.ModifierRolls[modifierIndex].ModifierId,
                            Is.EqualTo(first.ModifierRolls[modifierIndex].ModifierId), seed);
                        Assert.That(replay.ModifierRolls[modifierIndex].Value,
                            Is.EqualTo(first.ModifierRolls[modifierIndex].Value), seed);
                    }
                    appearances.Add(HomeLanternModularArtContract.ResolveAppearanceId(
                        first.TemplateId));
                }
            }

            Assert.That(appearances.Count,
                Is.EqualTo(HomeLanternModularArtContract.AppearanceCount));
        }

        [Test]
        public void QualityContractHasExactlyFiveOrderedTiers()
        {
            Assert.That((int)LanternQuality.Common, Is.EqualTo(0));
            Assert.That((int)LanternQuality.Uncommon, Is.EqualTo(1));
            Assert.That((int)LanternQuality.Rare, Is.EqualTo(2));
            Assert.That((int)LanternQuality.Epic, Is.EqualTo(3));
            Assert.That((int)LanternQuality.Legendary, Is.EqualTo(4));
        }

        [Test]
        public void GlobalSelectionDoesNotMutateLockedSnapshot()
        {
            var first = CreateLantern("lantern.instance.1", LanternQuality.Common);
            var second = CreateLantern("lantern.instance.2", LanternQuality.Rare);
            var inventory = new LanternInventory(new[] { first, second }, first.LanternInstanceId);
            var locked = inventory.CreateActiveSnapshot("lantern.none.v1");

            Assert.That(inventory.TrySetActive(second.LanternInstanceId), Is.True);
            Assert.That(locked.LanternInstanceId, Is.EqualTo(first.LanternInstanceId));
            Assert.That(locked.Quality, Is.EqualTo(LanternQuality.Common));
            Assert.That(inventory.CreateActiveSnapshot("lantern.none.v1").LanternInstanceId,
                Is.EqualTo(second.LanternInstanceId));
        }

        [Test]
        public void NoneSnapshotIsExplicitAndTransferHonorsUsageLocks()
        {
            var lantern = CreateLantern("lantern.instance.1", LanternQuality.Uncommon);
            var inventory = new LanternInventory(new[] { lantern });
            Assert.That(inventory.CreateActiveSnapshot("lantern.none.v1").IsNone, Is.True);
            Assert.That(inventory.CanTransfer(lantern.LanternInstanceId), Is.True);
            Assert.That(inventory.TryAddUsageLock(lantern.LanternInstanceId, LanternUsageLock.Defense), Is.True);
            Assert.That(inventory.CanTransfer(lantern.LanternInstanceId), Is.False);
            Assert.That(inventory.TryRemoveUsageLock(lantern.LanternInstanceId, LanternUsageLock.Defense), Is.True);
            Assert.That(inventory.TrySetActive(lantern.LanternInstanceId), Is.True);
            Assert.That(inventory.CanTransfer(lantern.LanternInstanceId), Is.False);
        }

        [Test]
        public void MarketPurchaseCanBeInsertedAtTopWithoutCreatingADuplicate()
        {
            var existing = CreateLantern("lantern.instance.existing", LanternQuality.Common);
            var purchased = CreateLantern("market.buy.market-receipt-000001", LanternQuality.Rare);
            var inventory = new LanternInventory(new[] { existing });

            Assert.That(inventory.TryAddFirst(purchased), Is.True);
            Assert.That(inventory.Instances[0], Is.SameAs(purchased));
            Assert.That(inventory.Contains(purchased.LanternInstanceId), Is.True);
            Assert.That(inventory.TryAddFirst(purchased), Is.False);
        }

        [Test]
        public void MarketListingClearsAllEquippedUsageAndInvalidatesStaleSnapshots()
        {
            var lantern = CreateLantern("lantern.instance.market", LanternQuality.Rare);
            var inventory = new LanternInventory(new[] { lantern }, lantern.LanternInstanceId);
            inventory.TryAddUsageLock(lantern.LanternInstanceId,
                LanternUsageLock.Expedition | LanternUsageLock.Defense);
            var staleSnapshot = inventory.CreateActiveSnapshot("lantern.none.v1");

            bool clearedActiveSelection;
            Assert.That(inventory.TryBeginMarketListing(
                lantern.LanternInstanceId, out clearedActiveSelection), Is.True);

            Assert.That(clearedActiveSelection, Is.True);
            Assert.That(inventory.ActiveLanternInstanceId, Is.Null);
            Assert.That(lantern.UsageLocks, Is.EqualTo(LanternUsageLock.Market));
            Assert.That(inventory.ResolveOwnedAvailableSnapshot(
                staleSnapshot, "lantern.none.v1").IsNone, Is.True);
            Assert.That(inventory.TrySetActive(lantern.LanternInstanceId), Is.False);
        }

        [Test]
        public void MarketSaleRemovesOwnershipAndDefenseSnapshotCannotRetainBonus()
        {
            var lantern = CreateLantern("lantern.instance.sold", LanternQuality.Epic);
            var inventory = new LanternInventory(new[] { lantern }, lantern.LanternInstanceId);
            var staleDefenseSnapshot = inventory.CreateActiveSnapshot("lantern.none.v1");

            Assert.That(inventory.TryCompleteMarketSale(lantern.LanternInstanceId), Is.True);

            Assert.That(inventory.Contains(lantern.LanternInstanceId), Is.False);
            Assert.That(inventory.ActiveLanternInstanceId, Is.Null);
            Assert.That(inventory.ResolveOwnedAvailableSnapshot(
                staleDefenseSnapshot, "lantern.none.v1").IsNone, Is.True);
        }

        [Test]
        public void CancelledMarketListingDoesNotRestoreEquipmentSilently()
        {
            var lantern = CreateLantern("lantern.instance.cancelled", LanternQuality.Uncommon);
            var inventory = new LanternInventory(new[] { lantern }, lantern.LanternInstanceId);
            bool ignored;
            Assert.That(inventory.TryBeginMarketListing(lantern.LanternInstanceId, out ignored), Is.True);

            Assert.That(inventory.TryCancelMarketListing(lantern.LanternInstanceId), Is.True);

            Assert.That(inventory.ActiveLanternInstanceId, Is.Null);
            Assert.That(inventory.TrySetActive(lantern.LanternInstanceId), Is.True);
        }

        [Test]
        public void LighthouseLightingDoesNotConsumeOrUnequipLantern()
        {
            var lantern = CreateLantern("lantern.instance.1", LanternQuality.Common);
            var inventory = new LanternInventory(new[] { lantern }, lantern.LanternInstanceId);
            var progress = new ChapterLighthouseProgressState();
            progress.RecordBossVictory();
            progress.RecordChapterLanternAward();

            Assert.That(progress.TryLight(), Is.True);
            Assert.That(progress.IsLit, Is.True);
            Assert.That(inventory.ActiveLanternInstanceId, Is.EqualTo(lantern.LanternInstanceId));
            Assert.That(inventory.Instances.Count, Is.EqualTo(1));
        }

        [Test]
        public void TestBypassLightsFirstClearFixtureWithoutPersistingBossVictory()
        {
            var fixture = FirstChapterLanternFixtures.CreateFirstClearCommon();
            var progress = new ChapterLighthouseProgressState();

            Assert.That(progress.RecordEquippedChapterLantern(fixture, true), Is.True);
            Assert.That(progress.BossDefeated, Is.False);
            Assert.That(progress.ChapterLanternAwarded, Is.True);
            Assert.That(progress.IsLit, Is.True);
        }

        [Test]
        public void FirstClearFixtureCannotBypassBossRequirementInReleaseMode()
        {
            var fixture = FirstChapterLanternFixtures.CreateFirstClearCommon();
            var progress = new ChapterLighthouseProgressState();

            Assert.That(progress.RecordEquippedChapterLantern(fixture, false), Is.False);
            Assert.That(progress.BossDefeated, Is.False);
            Assert.That(progress.ChapterLanternAwarded, Is.True);
            Assert.That(progress.IsLit, Is.False);
        }

        [Test]
        public void UnequippingLanternExtinguishesLightButKeepsChapterEligibility()
        {
            var fixture = FirstChapterLanternFixtures.CreateFirstClearCommon();
            var progress = new ChapterLighthouseProgressState();
            progress.RecordBossVictory();
            Assert.That(progress.RecordEquippedChapterLantern(fixture, false), Is.True);

            progress.RecordLanternUnequipped();

            Assert.That(progress.IsLit, Is.False);
            Assert.That(progress.BossDefeated, Is.True);
            Assert.That(progress.ChapterLanternAwarded, Is.True);
            Assert.That(progress.RecordEquippedChapterLantern(fixture, false), Is.True);
        }

        [Test]
        public void RestoringTestOnlyLitStateDoesNotPromoteBossVictory()
        {
            var progress = new ChapterLighthouseProgressState();

            progress.Restore(false, true, true);

            Assert.That(progress.BossDefeated, Is.False);
            Assert.That(progress.IsLit, Is.False);
        }

        [Test]
        public void EquippingOrdinaryChapterLanternCannotBypassBossRequirement()
        {
            var lantern = CreateLantern("lantern.instance.traded", LanternQuality.Common);
            var progress = new ChapterLighthouseProgressState();

            Assert.That(progress.RecordEquippedChapterLantern(lantern), Is.False);
            Assert.That(progress.BossDefeated, Is.False);
            Assert.That(progress.ChapterLanternAwarded, Is.True);
            Assert.That(progress.IsLit, Is.False);
        }

        [Test]
        public void LegacyMigrationPreservesLitChapterAndRequiresDefenseResync()
        {
            var migrated = LanternLegacyMigrationResult.FromLegacySave(false, true, true);
            Assert.That(migrated.BossDefeated, Is.True);
            Assert.That(migrated.ChapterLighthouseLit, Is.True);
            Assert.That(migrated.ExpeditionLanternSnapshot.IsNone, Is.True);
            Assert.That(migrated.ArenaDefenseLanternSnapshot.IsNone, Is.True);
            Assert.That(migrated.ArenaDefenseNeedsResync, Is.True);
        }

        [Test]
        public void RunSnapshotKeepsThreeIndependentRewardTableVersions()
        {
            var rewards = new ExpeditionRunRewardSnapshot(
                "D2", "paid", "tier.2",
                new ExpeditionRewardTableReference("crystal.D2", "crystal.v1"),
                new ExpeditionRewardTableReference("equipment.D2", "equipment.v3"),
                new ExpeditionRewardTableReference("lantern.D2", "lantern.v0.3"),
                "run-reward.v1");
            var run = new ExpeditionRunSnapshot("run-1", "season-1", "chapter-1",
                "account-1:season-1:chapter-1:run-1",
                LanternSnapshot.None("lantern.none.v1"), rewards);

            Assert.That(run.Rewards.DifficultyId, Is.EqualTo("D2"));
            Assert.That(run.Rewards.CrystalRewards.Version, Is.EqualTo("crystal.v1"));
            Assert.That(run.Rewards.EquipmentRewards.Version, Is.EqualTo("equipment.v3"));
            Assert.That(run.Rewards.LanternRewards.Version, Is.EqualTo("lantern.v0.3"));
        }

        [Test]
        public void FirstClearFixtureMatchesConfirmedEconomySampleAndIsNotAutoEquipped()
        {
            var inventory = new LanternInventory();

            Assert.That(FirstChapterLanternFixtures.EnsureFirstClearCommon(inventory), Is.True);
            Assert.That(FirstChapterLanternFixtures.EnsureFirstClearCommon(inventory), Is.False);
            Assert.That(inventory.Instances.Count, Is.EqualTo(1));
            Assert.That(inventory.ActiveLanternInstanceId, Is.Null);

            var lantern = inventory.Get(FirstChapterLanternFixtures.FirstClearInstanceId);
            Assert.That(lantern.TemplateId, Is.EqualTo("lantern.guard_tide.ch1"));
            Assert.That(lantern.Quality, Is.EqualTo(LanternQuality.Common));
            Assert.That(lantern.BaseFlameStatId, Is.EqualTo("team.max_health_bps"));
            Assert.That(lantern.BaseFlameValue, Is.EqualTo(1300));
            Assert.That(lantern.ContractStage, Is.EqualTo(0));
            Assert.That(lantern.CoreEffectId, Is.EqualTo("lantern.core.tide_shell.initial"));
            Assert.That(lantern.ModifierRolls.Count, Is.EqualTo(1));
            Assert.That(lantern.ModifierRolls[0].ModifierId, Is.EqualTo("armor_flat"));
            Assert.That(lantern.ModifierRolls[0].Value, Is.EqualTo(3));
        }

        [Test]
        public void EquippingFirstClearFixtureCreatesACompleteLockedSnapshot()
        {
            var lantern = FirstChapterLanternFixtures.CreateFirstClearCommon();
            var inventory = new LanternInventory(new[] { lantern });

            Assert.That(inventory.TrySetActive(lantern.LanternInstanceId), Is.True);
            var snapshot = inventory.CreateActiveSnapshot("lantern.none.v1");

            Assert.That(snapshot.IsNone, Is.False);
            Assert.That(snapshot.LanternInstanceId, Is.EqualTo("LGT-C-000001"));
            Assert.That(snapshot.BaseFlameStatId, Is.EqualTo("team.max_health_bps"));
            Assert.That(snapshot.BaseFlameValue, Is.EqualTo(1300));
            Assert.That(snapshot.ContractStage, Is.EqualTo(0));
            Assert.That(snapshot.ModifierRolls[0].Value, Is.EqualTo(3));
        }

        [Test]
        public void FirstClearLanternAppliesFrozenCombatStatsUsingFloorRounding()
        {
            var snapshot = LanternSnapshot.FromInstance(
                FirstChapterLanternFixtures.CreateFirstClearCommon());

            var finalHealth = LanternCombatStatRules.ApplyMaximumHealth(145, snapshot);

            Assert.That(finalHealth, Is.EqualTo(163));
            Assert.That(LanternCombatStatRules.ApplyArmor(7, snapshot), Is.EqualTo(10));
            Assert.That(LanternCombatStatRules.TideShellOpeningShield(finalHealth, snapshot), Is.EqualTo(40));
        }

        [Test]
        public void FiveQualityRepresentativesMatchConfirmedEconomyAndApplyAllGlyphStats()
        {
            var expectedBaseFlames = new[] { 1300, 1750, 2200, 2700, 3300 };
            var expectedGlyphCounts = new[] { 1, 1, 2, 2, 3 };
            foreach (LanternQuality quality in Enum.GetValues(typeof(LanternQuality)))
            {
                var lantern = FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(quality);
                Assert.That(FirstChapterLanternEconomyRules.MatchesConfirmedQualityContract(lantern),
                    Is.True, quality.ToString());
                Assert.That(lantern.BaseFlameValue, Is.EqualTo(expectedBaseFlames[(int)quality]));
                Assert.That(lantern.ModifierRolls.Count, Is.EqualTo(expectedGlyphCounts[(int)quality]));
            }

            var uncommon = LanternSnapshot.FromInstance(
                FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(LanternQuality.Uncommon));
            Assert.That(LanternCombatStatRules.ApplyAttackDamage(100, uncommon), Is.EqualTo(104));

            var rare = LanternSnapshot.FromInstance(
                FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(LanternQuality.Rare));
            Assert.That(LanternCombatStatRules.ApplyHealingAmount(100, rare), Is.EqualTo(106));
            Assert.That(LanternCombatStatRules.ApplyArmor(10, rare), Is.EqualTo(15));

            var epic = LanternSnapshot.FromInstance(
                FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(LanternQuality.Epic));
            Assert.That(LanternCombatStatRules.ApplyMaximumHealth(100, epic), Is.EqualTo(134));

            var legendary = LanternSnapshot.FromInstance(
                FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(LanternQuality.Legendary));
            Assert.That(LanternCombatStatRules.ApplyMaximumHealth(100, legendary), Is.EqualTo(142));
            Assert.That(LanternCombatStatRules.ApplyHealingAmount(100, legendary), Is.EqualTo(110));
            Assert.That(LanternCombatStatRules.ApplyAttackSpeed(10000, legendary), Is.EqualTo(11000));
        }

        [Test]
        public void DevelopmentQualitySetAddsFourMissingTiersOnceAndDoesNotAutoEquip()
        {
            var inventory = new LanternInventory();
            Assert.That(FirstChapterLanternFixtures.EnsureFirstClearCommon(inventory), Is.True);

            Assert.That(FirstChapterLanternEconomyRules.EnsureDevelopmentQualitySet(inventory), Is.EqualTo(4));
            Assert.That(FirstChapterLanternEconomyRules.EnsureDevelopmentQualitySet(inventory), Is.EqualTo(0));
            Assert.That(inventory.Instances.Count, Is.EqualTo(5));
            Assert.That(inventory.ActiveLanternInstanceId, Is.Null);

            var qualities = new HashSet<LanternQuality>();
            foreach (var lantern in inventory.Instances)
            {
                qualities.Add(lantern.Quality);
                Assert.That(FirstChapterLanternEconomyRules.MatchesConfirmedQualityContract(lantern),
                    Is.True, lantern.Quality.ToString());
            }
            Assert.That(qualities.Count, Is.EqualTo(5));
        }

        [Test]
        public void ExplicitNoneLanternLeavesCombatStatsUnchanged()
        {
            var snapshot = LanternSnapshot.None("lantern.none.v1");

            Assert.That(LanternCombatStatRules.ApplyMaximumHealth(145, snapshot), Is.EqualTo(145));
            Assert.That(LanternCombatStatRules.ApplyArmor(7, snapshot), Is.EqualTo(7));
            Assert.That(LanternCombatStatRules.TideShellOpeningShield(145, snapshot), Is.EqualTo(0));
        }

        private static LanternInstanceState CreateLantern(string instanceId, LanternQuality quality)
        {
            return new LanternInstanceState(instanceId, "lantern.template.test", quality,
                "season-1", "chapter-1", "D0", "lantern.effect.test",
                new[] { new LanternModifierRoll("lantern.modifier.test", 100) },
                "receipt." + instanceId, "lantern.rules.v1");
        }
    }
}
