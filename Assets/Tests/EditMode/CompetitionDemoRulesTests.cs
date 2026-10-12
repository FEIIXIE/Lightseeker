using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class CompetitionDemoRulesTests
    {
        [Test]
        public void InitialProfileContainsExactlySixUniqueCommonCharacters()
        {
            var characters = CompetitionDemoRules.CreateInitialCharacters(
                CharacterCatalog.CreatePrototype());
            Assert.AreEqual(6, characters.Length);
            var definitions = new HashSet<string>();
            var instances = new HashSet<string>();
            foreach (var character in characters)
            {
                Assert.AreEqual(CharacterQuality.Common, character.Quality);
                Assert.IsTrue(definitions.Add(character.DefinitionId));
                Assert.IsTrue(instances.Add(character.CharacterInstanceId));
            }
        }

        [Test]
        public void InitialProfileContainsOneCommonItemForEveryEquipmentSlot()
        {
            var equipment = CompetitionDemoRules.CreateInitialEquipment(
                EquipmentCatalog.CreatePrototype());
            Assert.AreEqual(5, equipment.Length);
            var instances = new HashSet<string>();
            foreach (var item in equipment)
            {
                Assert.AreEqual(EquipmentQuality.Common, item.Quality);
                Assert.IsTrue(instances.Add(item.EquipmentInstanceId));
            }
        }

        [Test]
        public void InitialProfileContainsActiveEligibleCommonLantern()
        {
            var lantern = CompetitionDemoRules.CreateInitialLantern();
            Assert.AreEqual(CompetitionDemoRules.InitialLanternInstanceId,
                lantern.LanternInstanceId);
            Assert.AreEqual(LanternQuality.Common, lantern.Quality);
            Assert.AreEqual(LanternUsageLock.None, lantern.UsageLocks);
        }

        [Test]
        public void CompletionLanternIsRareAndReplayStable()
        {
            var first = CompetitionDemoRules.CreateCompletionLantern("run-abc", 123u);
            var second = CompetitionDemoRules.CreateCompletionLantern("run-abc", 123u);
            Assert.AreEqual(LanternQuality.Rare, first.Quality);
            Assert.AreEqual(first.LanternInstanceId, second.LanternInstanceId);
            Assert.AreEqual(first.TemplateId, second.TemplateId);
            Assert.AreEqual(first.BaseFlameValue, second.BaseFlameValue);
        }

        [Test]
        public void RewardCapacityIncludesCompletionLantern()
        {
            Assert.AreEqual(3, CompetitionDemoRules.IncomingRewardSlots(2, true));
            Assert.AreEqual(2, CompetitionDemoRules.IncomingRewardSlots(2, false));
        }

        [Test]
        public void FullClearSettlementContainsFrozenDemoRewards()
        {
            var plan = CompetitionDemoSettlementRules.CreateD0Plan(
                "run-full-clear", true, "M28");

            Assert.AreEqual("BossCleared", plan.TerminalReason);
            Assert.AreEqual(26, plan.ProgressRank);
            Assert.AreEqual(50, plan.TideCrystal);
            Assert.AreEqual(2, plan.LampGlow);
            Assert.AreEqual(4, plan.TideIron);
            Assert.AreEqual(1, plan.MistLeather);
            Assert.AreEqual(1, plan.EquipmentQuantity);
            Assert.AreEqual(1, plan.LanternQuantity);
        }

        [Test]
        public void AbandonSettlementIsProgressBoundAndReplayStable()
        {
            var early = CompetitionDemoSettlementRules.CreateD0Plan(
                "run-abandon", false, "M05");
            var replay = CompetitionDemoSettlementRules.CreateD0Plan(
                "run-abandon", false, "M05");
            var later = CompetitionDemoSettlementRules.CreateD0Plan(
                "run-abandon-later", false, "M28");

            Assert.AreEqual("Abandoned", early.TerminalReason);
            Assert.AreEqual(2, early.ProgressRank);
            Assert.AreEqual(early.TideCrystal, replay.TideCrystal);
            Assert.AreEqual(early.LampGlow, replay.LampGlow);
            Assert.Less(early.TideCrystal, later.TideCrystal);
            Assert.AreEqual(0, early.EquipmentQuantity);
            Assert.AreEqual(0, early.LanternQuantity);
        }

        [Test]
        public void SettlementProgressRanksCoverEveryFrozenMapBand()
        {
            Assert.AreEqual(0, CompetitionDemoSettlementRules.ProgressRank("M00"));
            Assert.AreEqual(1, CompetitionDemoSettlementRules.ProgressRank("M02"));
            Assert.AreEqual(7, CompetitionDemoSettlementRules.ProgressRank("M18"));
            Assert.AreEqual(8, CompetitionDemoSettlementRules.ProgressRank("M19"));
            Assert.AreEqual(13, CompetitionDemoSettlementRules.ProgressRank("M28"));
        }
    }
}
