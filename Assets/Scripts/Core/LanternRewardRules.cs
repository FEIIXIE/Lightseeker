using System;

namespace SeekerAutoChess.Core
{
    public enum LanternBossRewardDisposition
    {
        None = 0,
        FirstClearGuaranteedCommon = 1,
        RepeatClearDropTableRoll = 2
    }

    public sealed class LanternBossRewardContext
    {
        public string AccountId { get; }
        public string SeasonId { get; }
        public string ChapterId { get; }
        public string RunId { get; }
        public string DifficultyId { get; }
        public bool RewardEligible { get; }
        public bool BossDefeated { get; }
        public bool IsFirstSeasonChapterClear { get; }
        public bool SettlementAlreadyRecorded { get; }
        public ExpeditionRewardTableReference LanternRewardTable { get; }

        public LanternBossRewardContext(
            string accountId,
            string seasonId,
            string chapterId,
            string runId,
            string difficultyId,
            bool rewardEligible,
            bool bossDefeated,
            bool isFirstSeasonChapterClear,
            bool settlementAlreadyRecorded,
            ExpeditionRewardTableReference lanternRewardTable)
        {
            if (string.IsNullOrWhiteSpace(accountId)) throw new ArgumentException("An account id is required.", nameof(accountId));
            if (string.IsNullOrWhiteSpace(seasonId)) throw new ArgumentException("A season id is required.", nameof(seasonId));
            if (string.IsNullOrWhiteSpace(chapterId)) throw new ArgumentException("A chapter id is required.", nameof(chapterId));
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run id is required.", nameof(runId));
            if (string.IsNullOrWhiteSpace(difficultyId)) throw new ArgumentException("A difficulty id is required.", nameof(difficultyId));
            AccountId = accountId;
            SeasonId = seasonId;
            ChapterId = chapterId;
            RunId = runId;
            DifficultyId = difficultyId;
            RewardEligible = rewardEligible;
            BossDefeated = bossDefeated;
            IsFirstSeasonChapterClear = isFirstSeasonChapterClear;
            SettlementAlreadyRecorded = settlementAlreadyRecorded;
            LanternRewardTable = lanternRewardTable ?? throw new ArgumentNullException(nameof(lanternRewardTable));
        }
    }

    public sealed class LanternBossRewardDecision
    {
        public LanternBossRewardDisposition Disposition { get; }
        public string SettlementIdempotencyKey { get; }
        public LanternQuality? GuaranteedQuality { get; }
        public ExpeditionRewardTableReference LanternRewardTable { get; }

        internal LanternBossRewardDecision(
            LanternBossRewardDisposition disposition,
            string settlementIdempotencyKey,
            LanternQuality? guaranteedQuality,
            ExpeditionRewardTableReference lanternRewardTable)
        {
            Disposition = disposition;
            SettlementIdempotencyKey = settlementIdempotencyKey;
            GuaranteedQuality = guaranteedQuality;
            LanternRewardTable = lanternRewardTable;
        }
    }

    public static class LanternBossRewardRules
    {
        public const string RulesVersion = "lantern-boss-reward.v1";

        public static LanternBossRewardDecision Evaluate(LanternBossRewardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            var settlementKey = CreateSettlementIdempotencyKey(context);
            if (!context.RewardEligible || !context.BossDefeated || context.SettlementAlreadyRecorded)
            {
                return new LanternBossRewardDecision(
                    LanternBossRewardDisposition.None, settlementKey, null, null);
            }
            if (context.IsFirstSeasonChapterClear)
            {
                return new LanternBossRewardDecision(
                    LanternBossRewardDisposition.FirstClearGuaranteedCommon,
                    settlementKey,
                    LanternQuality.Common,
                    null);
            }
            return new LanternBossRewardDecision(
                LanternBossRewardDisposition.RepeatClearDropTableRoll,
                settlementKey,
                null,
                context.LanternRewardTable);
        }

        public static string CreateSettlementIdempotencyKey(LanternBossRewardContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return context.AccountId + ":" + context.SeasonId + ":" +
                   context.ChapterId + ":" + context.RunId + ":lantern-boss-settlement";
        }
    }
}
