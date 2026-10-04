using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class LanternRewardRulesTests
    {
        [Test]
        public void FirstClearGuaranteesExactlyCommonWithoutRepeatRoll()
        {
            var decision = LanternBossRewardRules.Evaluate(CreateContext(
                rewardEligible: true, bossDefeated: true,
                isFirstClear: true, alreadySettled: false));

            Assert.That(decision.Disposition,
                Is.EqualTo(LanternBossRewardDisposition.FirstClearGuaranteedCommon));
            Assert.That(decision.GuaranteedQuality, Is.EqualTo(LanternQuality.Common));
            Assert.That(decision.LanternRewardTable, Is.Null);
        }

        [Test]
        public void RepeatClearReferencesFrozenTableWithoutClientProbability()
        {
            var decision = LanternBossRewardRules.Evaluate(CreateContext(
                rewardEligible: true, bossDefeated: true,
                isFirstClear: false, alreadySettled: false));

            Assert.That(decision.Disposition,
                Is.EqualTo(LanternBossRewardDisposition.RepeatClearDropTableRoll));
            Assert.That(decision.GuaranteedQuality, Is.Null);
            Assert.That(decision.LanternRewardTable.TableId, Is.EqualTo("lantern.D3"));
            Assert.That(decision.LanternRewardTable.Version, Is.EqualTo("lantern.v0.3"));
        }

        [TestCase(false, true, false)]
        [TestCase(true, false, false)]
        [TestCase(true, true, true)]
        public void InvalidOrReplayedSettlementNeverGrantsAnotherLantern(
            bool rewardEligible, bool bossDefeated, bool alreadySettled)
        {
            var decision = LanternBossRewardRules.Evaluate(CreateContext(
                rewardEligible, bossDefeated, false, alreadySettled));
            Assert.That(decision.Disposition, Is.EqualTo(LanternBossRewardDisposition.None));
        }

        [Test]
        public void SettlementKeyIsStableForTheSameRun()
        {
            var first = LanternBossRewardRules.Evaluate(CreateContext(true, true, false, false));
            var replay = LanternBossRewardRules.Evaluate(CreateContext(true, true, false, true));
            Assert.That(first.SettlementIdempotencyKey, Is.EqualTo(replay.SettlementIdempotencyKey));
        }

        private static LanternBossRewardContext CreateContext(
            bool rewardEligible,
            bool bossDefeated,
            bool isFirstClear,
            bool alreadySettled)
        {
            return new LanternBossRewardContext(
                "account-1", "season-1", "chapter-1", "run-1", "D3",
                rewardEligible, bossDefeated, isFirstClear, alreadySettled,
                new ExpeditionRewardTableReference("lantern.D3", "lantern.v0.3"));
        }
    }
}
