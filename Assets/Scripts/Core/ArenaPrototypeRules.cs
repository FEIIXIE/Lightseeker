using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ArenaPageState
    {
        Ranking = 0,
        OpponentDetail = 1,
        ChallengeConfirm = 2,
        Preparation = 3,
        Result = 4
    }

    [Serializable]
    public sealed class ArenaUnitProfile
    {
        public int Slot;
        public string CharacterInstanceId;
        public string DefinitionId;
        public CharacterQuality Quality;
        public int BasePower;
        public int AptitudeRoll;
        public int ExternalStar;

        public ArenaUnitProfile(int slot, string characterInstanceId, string definitionId,
            CharacterQuality quality, int basePower, int aptitudeRoll, int externalStar)
        {
            if (slot < 0 || slot >= ArenaPrototypeRules.DefenseSlotCount) throw new ArgumentOutOfRangeException(nameof(slot));
            if (string.IsNullOrWhiteSpace(characterInstanceId)) throw new ArgumentException("An arena unit needs an instance id.", nameof(characterInstanceId));
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("An arena unit needs a definition id.", nameof(definitionId));
            Slot = slot;
            CharacterInstanceId = characterInstanceId;
            DefinitionId = definitionId;
            Quality = quality;
            BasePower = Math.Max(1, basePower);
            AptitudeRoll = Math.Max(0, Math.Min(CharacterRarityInstanceRules.RollScale, aptitudeRoll));
            ExternalStar = Math.Max(0, Math.Min(2, externalStar));
        }
    }

    [Serializable]
    public sealed class ArenaOpponentProfile
    {
        public string OpponentId;
        public string RulesVersion;
        public int SeedRank;
        public string DisplayName;
        public int Rank;
        public int PlaceholderLevel;
        public int Rating;
        public int Power;
        public bool IsBot;
        public bool RewardEligible;
        public ArenaUnitProfile[] DefenseUnits;

        public ArenaOpponentProfile(string opponentId, string rulesVersion, int seedRank,
            string displayName, int rank, int placeholderLevel, int rating, int power,
            bool isBot, bool rewardEligible, ArenaUnitProfile[] defenseUnits)
        {
            OpponentId = opponentId ?? string.Empty;
            RulesVersion = rulesVersion ?? string.Empty;
            SeedRank = Math.Max(0, seedRank);
            DisplayName = displayName ?? string.Empty;
            Rank = Math.Max(1, rank);
            PlaceholderLevel = Math.Max(1, placeholderLevel);
            Rating = Math.Max(0, rating);
            Power = Math.Max(0, power);
            IsBot = isBot;
            RewardEligible = rewardEligible;
            DefenseUnits = defenseUnits ?? new ArenaUnitProfile[0];
        }
    }

    public static class ArenaPrototypeRules
    {
        public const int DailyChallengeLimit = 5;
        public const int DefenseSlotCount = 6;
        public const int PublicDefenseSlotCount = 4;
        public const int TemporaryInitialRating = 1000;
        public const int TemporaryWinRatingDelta = 18;
        public const int TemporaryLossRatingDelta = -8;
        public const int ExistingPowerMultiplier = 140;
        public const int InitialBotCount = 100;
        public const string BotRulesVersion = "arena.bot.v1";
        public const int PveSeasonCycleDays = 14;
        public const int PvpRewardCycleDays = 7;
        public const string RulesStatus = "TEMPORARY_PENDING_ECONOMY_FREEZE";

        public static readonly DateTime CycleEpochUtc =
            new DateTime(2026, 9, 28, 0, 0, 0, DateTimeKind.Utc);

        private static readonly string[] BotNamePrefixes = { "雾港", "旧塔", "潮痕", "苔径", "白礁", "静海", "长夜", "灰岸", "碎星", "晨雾", "落潮", "远灯", "岬角", "荒滩", "归帆", "盐风", "雨湾", "灯峡", "石潮", "海崖" };
        private static readonly string[] BotNameSuffixes = { "守望", "点灯", "巡路", "拾光", "听潮" };
        private static readonly string[] BotDefinitions = { "unit.starblade", "unit.guardian", "unit.thornGuard", "unit.archer", "unit.healer", "unit.starpriest" };
        private static readonly int[] BotBasePowers = { 4, 4, 5, 3, 3, 5 };

        public static string DayKey(DateTime now) { return now.ToString("yyyy-MM-dd"); }

        public static int NormalizeChallengesRemaining(string savedDayKey, int savedRemaining, DateTime now)
        {
            return string.Equals(savedDayKey, DayKey(now), StringComparison.Ordinal)
                ? Math.Max(0, Math.Min(DailyChallengeLimit, savedRemaining)) : DailyChallengeLimit;
        }

        public static bool TryConsumeChallenge(ref int remaining)
        {
            if (remaining <= 0) return false;
            remaining--;
            return true;
        }

        public static TimeSpan RemainingInCycle(DateTime now, int cycleDays)
        {
            if (cycleDays <= 0) throw new ArgumentOutOfRangeException(nameof(cycleDays));
            var utcNow = now.Kind == DateTimeKind.Utc ? now : now.ToUniversalTime();
            var cycleTicks = TimeSpan.FromDays(cycleDays).Ticks;
            var elapsedTicks = utcNow.Ticks - CycleEpochUtc.Ticks;
            var positionTicks = elapsedTicks % cycleTicks;
            if (positionTicks < 0) positionTicks += cycleTicks;
            return TimeSpan.FromTicks(cycleTicks - positionTicks);
        }

        public static int TemporaryRatingDelta(bool playerWon) { return playerWon ? TemporaryWinRatingDelta : TemporaryLossRatingDelta; }

        public static int CalculateExistingPower(IEnumerable<ArenaUnitProfile> units)
        {
            if (units == null) return 0;
            var total = 0;
            foreach (var unit in units) if (unit != null) total += Math.Max(0, unit.BasePower);
            return total * ExistingPowerMultiplier;
        }

        public static ArenaOpponentProfile[] CreateDeterministicBots(int playerRating)
        {
            var result = new ArenaOpponentProfile[InitialBotCount];
            for (var index = 0; index < result.Length; index++)
            {
                var rank = index + 1;
                var botId = BotRulesVersion + "." + rank.ToString("D3");
                var rating = 1020 + 12 * (InitialBotCount - rank);
                CharacterQuality quality;
                int externalStar;
                int aptitudeRoll;
                ResolveBotTier(rank, out quality, out externalStar, out aptitudeRoll);
                var units = new ArenaUnitProfile[DefenseSlotCount];
                for (var slot = 0; slot < units.Length; slot++)
                {
                    var definitionId = BotDefinitions[slot];
                    var instanceId = botId + ".slot" + slot + "." + definitionId;
                    units[slot] = new ArenaUnitProfile(slot, instanceId, definitionId, quality,
                        BotBasePowers[slot], aptitudeRoll, externalStar);
                }
                var displayName = BotNamePrefixes[index % BotNamePrefixes.Length] +
                    BotNameSuffixes[index / BotNamePrefixes.Length];
                result[index] = new ArenaOpponentProfile(botId, BotRulesVersion, rank,
                    displayName, rank, 101 - rank, rating, CalculateBotDisplayPower(
                        quality, aptitudeRoll, externalStar), true, false, units);
            }
            return result;
        }

        public static int CalculateBotFinalStrengthBps(
            CharacterQuality quality, int aptitudeRoll, int externalStar)
        {
            var qualityMultiplier = CharacterRarityInstanceRules.CalculateMultiplier(quality, aptitudeRoll);
            var starMultiplier = externalStar == 0 ? 10000 : externalStar == 1 ? 11000 : 12100;
            return RoundHalfUp((long)qualityMultiplier * starMultiplier, 10000);
        }

        public static int CalculateBotDisplayPower(
            CharacterQuality quality, int aptitudeRoll, int externalStar)
        {
            return RoundHalfUp((long)24 * ExistingPowerMultiplier *
                CalculateBotFinalStrengthBps(quality, aptitudeRoll, externalStar), 10000);
        }

        private static void ResolveBotTier(int rank, out CharacterQuality quality,
            out int externalStar, out int aptitudeRoll)
        {
            if (rank < 1 || rank > InitialBotCount) throw new ArgumentOutOfRangeException(nameof(rank));
            if (rank <= 5) { quality = CharacterQuality.Legendary; externalStar = 2; aptitudeRoll = 11000 - rank * 1000; return; }
            if (rank <= 10) { quality = CharacterQuality.Legendary; externalStar = 1; aptitudeRoll = 16000 - rank * 1000; return; }
            if (rank <= 20) { quality = CharacterQuality.Legendary; externalStar = 0; aptitudeRoll = 21000 - rank * 1000; return; }
            if (rank <= 30) { quality = CharacterQuality.Epic; externalStar = 1; aptitudeRoll = 31000 - rank * 1000; return; }
            if (rank <= 40) { quality = CharacterQuality.Epic; externalStar = 0; aptitudeRoll = 41000 - rank * 1000; return; }
            if (rank <= 50) { quality = CharacterQuality.Rare; externalStar = 1; aptitudeRoll = 51000 - rank * 1000; return; }
            if (rank <= 60) { quality = CharacterQuality.Rare; externalStar = 0; aptitudeRoll = 61000 - rank * 1000; return; }
            if (rank <= 70) { quality = CharacterQuality.Uncommon; externalStar = 1; aptitudeRoll = 71000 - rank * 1000; return; }
            if (rank <= 80) { quality = CharacterQuality.Uncommon; externalStar = 0; aptitudeRoll = 81000 - rank * 1000; return; }
            if (rank <= 90) { quality = CharacterQuality.Common; externalStar = 1; aptitudeRoll = 91000 - rank * 1000; return; }
            quality = CharacterQuality.Common;
            externalStar = 0;
            aptitudeRoll = 101000 - rank * 1000;
        }

        private static int RoundHalfUp(long numerator, int denominator)
        {
            return (int)((numerator + denominator / 2) / denominator);
        }

        public static ArenaOpponentProfile[] SelectNearby(IReadOnlyList<ArenaOpponentProfile> ranking, int selectedRank, int count)
        {
            if (ranking == null || ranking.Count == 0 || count <= 0) return new ArenaOpponentProfile[0];
            var take = Math.Min(count, ranking.Count);
            var centerIndex = ranking.Count - 1;
            for (var index = 0; index < ranking.Count; index++) if (ranking[index].Rank >= selectedRank) { centerIndex = index; break; }
            var start = Math.Max(0, Math.Min(ranking.Count - take, centerIndex - take / 2));
            var result = new ArenaOpponentProfile[take];
            for (var index = 0; index < take; index++) result[index] = ranking[start + index];
            return result;
        }

        public static int CalculatePlayerDisplayRank(
            IReadOnlyList<ArenaOpponentProfile> ranking, int playerRating)
        {
            if (ranking == null || ranking.Count == 0) return 1;
            var stronger = 0;
            for (var index = 0; index < ranking.Count; index++)
                if (ranking[index].Rating > playerRating) stronger++;
            return stronger + 1;
        }
    }
}
