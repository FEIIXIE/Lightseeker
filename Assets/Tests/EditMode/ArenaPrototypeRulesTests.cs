using System;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class ArenaPrototypeRulesTests
    {
        [Test] public void DailyChallengesResetAndConsumeExactlyOnce()
        {
            var today = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Local);
            Assert.AreEqual(5, ArenaPrototypeRules.NormalizeChallengesRemaining("2026-09-29", 1, today));
            var remaining = ArenaPrototypeRules.NormalizeChallengesRemaining("2026-09-30", 5, today);
            Assert.IsTrue(ArenaPrototypeRules.TryConsumeChallenge(ref remaining));
            Assert.AreEqual(4, remaining);
            remaining = 0;
            Assert.IsFalse(ArenaPrototypeRules.TryConsumeChallenge(ref remaining));
        }

        [Test] public void BotRankingIsDeterministicAndMatchesFrozenTopHundredContract()
        {
            var first = ArenaPrototypeRules.CreateDeterministicBots(1000);
            var second = ArenaPrototypeRules.CreateDeterministicBots(1000);
            Assert.AreEqual(100, first.Length);
            Assert.AreEqual(first[0].DisplayName, second[0].DisplayName);
            Assert.AreEqual(first[0].Rating, second[0].Rating);
            Assert.AreEqual(6, first[0].DefenseUnits.Length);
            Assert.AreEqual(first[0].DefenseUnits[0].CharacterInstanceId, second[0].DefenseUnits[0].CharacterInstanceId);
            Assert.AreEqual("arena.bot.v1.001", first[0].OpponentId);
            Assert.AreEqual("雾港守望", first[0].DisplayName);
            Assert.AreEqual(2208, first[0].Rating);
            Assert.AreEqual(100, first[0].PlaceholderLevel);
            Assert.AreEqual(CharacterQuality.Legendary, first[0].DefenseUnits[0].Quality);
            Assert.AreEqual(2, first[0].DefenseUnits[0].ExternalStar);
            Assert.AreEqual(10000, first[0].DefenseUnits[0].AptitudeRoll);
            Assert.AreEqual(9772, first[0].Power);
            Assert.AreEqual("arena.bot.v1.100", first[99].OpponentId);
            Assert.AreEqual("海崖听潮", first[99].DisplayName);
            Assert.AreEqual(1020, first[99].Rating);
            Assert.AreEqual(1, first[99].PlaceholderLevel);
            Assert.AreEqual(1000, first[99].DefenseUnits[0].AptitudeRoll);
            Assert.AreEqual(3279, first[99].Power);
            Assert.IsFalse(first[0].RewardEligible);
        }

        [Test] public void BotDifficultyAndIdentityAreStrictlyDescendingAndUnique()
        {
            var bots = ArenaPrototypeRules.CreateDeterministicBots(5000);
            var ids = new System.Collections.Generic.HashSet<string>();
            var names = new System.Collections.Generic.HashSet<string>();
            for (var index = 0; index < bots.Length; index++)
            {
                Assert.IsTrue(ids.Add(bots[index].OpponentId));
                Assert.IsTrue(names.Add(bots[index].DisplayName));
                Assert.AreEqual(index + 1, bots[index].SeedRank);
                Assert.AreEqual(ArenaPrototypeRules.BotRulesVersion, bots[index].RulesVersion);
                if (index == 0) continue;
                Assert.Greater(bots[index - 1].Rating, bots[index].Rating);
                Assert.Greater(bots[index - 1].Power, bots[index].Power);
            }
        }

        [Test] public void EveryBotUsesTheFrozenSixUnitFormation()
        {
            var bots = ArenaPrototypeRules.CreateDeterministicBots(1000);
            var definitions = new[] { "unit.starblade", "unit.guardian", "unit.thornGuard", "unit.archer", "unit.healer", "unit.starpriest" };
            var powers = new[] { 4, 4, 5, 3, 3, 5 };
            foreach (var bot in bots)
            {
                for (var slot = 0; slot < definitions.Length; slot++)
                {
                    Assert.AreEqual(slot, bot.DefenseUnits[slot].Slot);
                    Assert.AreEqual(definitions[slot], bot.DefenseUnits[slot].DefinitionId);
                    Assert.AreEqual(powers[slot], bot.DefenseUnits[slot].BasePower);
                }
            }
        }

        [Test] public void PublicDefenseContractHidesLastTwoSlots()
        {
            Assert.AreEqual(6, ArenaPrototypeRules.DefenseSlotCount);
            Assert.AreEqual(4, ArenaPrototypeRules.PublicDefenseSlotCount);
        }

        [Test] public void ExistingPowerFormulaUsesFrozenPrototypeMultiplier()
        {
            var units = new[] { new ArenaUnitProfile(0, "one", "unit.guardian", CharacterQuality.Common, 5, 0, 0), new ArenaUnitProfile(1, "two", "unit.archer", CharacterQuality.Epic, 4, 0, 0) };
            Assert.AreEqual(1260, ArenaPrototypeRules.CalculateExistingPower(units));
        }

        [Test] public void NearbySliceKeepsStableRankingOrder()
        {
            var ranking = ArenaPrototypeRules.CreateDeterministicBots(1000);
            var playerRank = ArenaPrototypeRules.CalculatePlayerDisplayRank(ranking, 1000);
            var nearby = ArenaPrototypeRules.SelectNearby(ranking, playerRank, 7);
            Assert.AreEqual(101, playerRank);
            Assert.AreEqual(94, nearby[0].Rank);
            Assert.AreEqual(100, nearby[6].Rank);
            Assert.AreEqual(7, nearby.Length);
            for (var index = 1; index < nearby.Length; index++) Assert.AreEqual(nearby[index - 1].Rank + 1, nearby[index].Rank);
        }

        [Test] public void NearbyHighlightFollowsOnlyTheSelectedOpponent()
        {
            Assert.IsFalse(ArenaPrototypeRules.ShouldHighlightOpponentRow(94, -1));
            Assert.IsFalse(ArenaPrototypeRules.ShouldHighlightOpponentRow(94, 95));
            Assert.IsTrue(ArenaPrototypeRules.ShouldHighlightOpponentRow(95, 95));
        }

        [Test] public void DisplayRankingInsertsCurrentPlayerAtTheCalculatedRank()
        {
            var opponents = ArenaPrototypeRules.CreateDeterministicBots(1190);
            var ranking = ArenaPrototypeRules.CreateDisplayRanking(opponents, 1190, 4321, "我");
            var nearby = ArenaPrototypeRules.SelectNearby(ranking, 86, 7);

            Assert.AreEqual(101, ranking.Length);
            Assert.IsTrue(ranking[85].IsCurrentPlayer);
            Assert.AreEqual(86, ranking[85].Rank);
            Assert.AreEqual(1190, ranking[85].Rating);
            Assert.AreEqual(4321, ranking[85].Power);
            Assert.AreEqual("我", ranking[85].DisplayName);
            Assert.AreEqual(-1, ranking[85].OpponentIndex);
            Assert.IsTrue(nearby[3].IsCurrentPlayer);
            Assert.AreEqual(86, nearby[3].Rank);
            Assert.AreEqual(87, nearby[4].Rank);
            Assert.AreEqual(86, nearby[4].Opponent.SeedRank);
        }

        [Test] public void PveSeasonAndPvpRewardCountdownsUseIndependentCycles()
        {
            var now = new DateTime(2026, 9, 30, 12, 0, 0, DateTimeKind.Utc);
            Assert.AreEqual(TimeSpan.FromDays(11.5),
                ArenaPrototypeRules.RemainingInCycle(now, ArenaPrototypeRules.PveSeasonCycleDays));
            Assert.AreEqual(TimeSpan.FromDays(4.5),
                ArenaPrototypeRules.RemainingInCycle(now, ArenaPrototypeRules.PvpRewardCycleDays));
        }

        [Test] public void CycleBoundaryShowsTheNewFullCycle()
        {
            Assert.AreEqual(TimeSpan.FromDays(14), ArenaPrototypeRules.RemainingInCycle(
                ArenaPrototypeRules.CycleEpochUtc, ArenaPrototypeRules.PveSeasonCycleDays));
            Assert.AreEqual(TimeSpan.FromDays(7), ArenaPrototypeRules.RemainingInCycle(
                ArenaPrototypeRules.CycleEpochUtc.AddDays(7), ArenaPrototypeRules.PvpRewardCycleDays));
        }

        [Test] public void PveLeaderboardUsesLatestFirstReachedTieBreaker()
        {
            var ranking = PrototypeLeaderboardRules.RankEligible(new[]
            {
                new PrototypeLeaderboardEntry("later", "后来者", 100, 20, true, false),
                new PrototypeLeaderboardEntry("early", "先到者", 100, 10, true, false),
                new PrototypeLeaderboardEntry("higher", "高分者", 120, 30, true, false),
                new PrototypeLeaderboardEntry("blocked", "不合格", 999, 1, false, false)
            });
            Assert.AreEqual(3, ranking.Length);
            Assert.AreEqual("higher", ranking[0].AccountId);
            Assert.AreEqual("early", ranking[1].AccountId);
            Assert.AreEqual("later", ranking[2].AccountId);
        }

        [Test] public void PveRewardShowsOnlyTopSixtyPercent()
        {
            Assert.AreEqual(60, PrototypeLeaderboardRules.CalculatePveRewardSlots(100));
            Assert.AreEqual(1, PrototypeLeaderboardRules.CalculatePveRewardSlots(1));
            Assert.Greater(PrototypeLeaderboardRules.CalculatePveRewardMicroSkr(
                60, 100, PrototypeLeaderboardRules.TestPvePoolMicroSkr), 0L);
            Assert.AreEqual(0L, PrototypeLeaderboardRules.CalculatePveRewardMicroSkr(
                61, 100, PrototypeLeaderboardRules.TestPvePoolMicroSkr));
        }

        [Test] public void PvpRewardUsesFrozenTopHundredBands()
        {
            var pool = PrototypeLeaderboardRules.TestPvpPoolMicroSkr;
            Assert.AreEqual(800L * PrototypeLeaderboardRules.MicroSkrPerSkr,
                PrototypeLeaderboardRules.CalculatePvpRewardMicroSkr(1, pool));
            Assert.AreEqual(500L * PrototypeLeaderboardRules.MicroSkrPerSkr,
                PrototypeLeaderboardRules.CalculatePvpRewardMicroSkr(2, pool));
            Assert.AreEqual(39L * PrototypeLeaderboardRules.MicroSkrPerSkr,
                PrototypeLeaderboardRules.CalculatePvpRewardMicroSkr(100, pool));
            Assert.AreEqual(0L, PrototypeLeaderboardRules.CalculatePvpRewardMicroSkr(101, pool));
        }

        [Test] public void DisplayedRewardSlotsNeverExceedTheirTestPool()
        {
            long pveAllocated = 0L;
            for (var rank = 1; rank <= PrototypeLeaderboardRules.CalculatePveRewardSlots(120); rank++)
                pveAllocated += PrototypeLeaderboardRules.CalculatePveRewardMicroSkr(
                    rank, 120, PrototypeLeaderboardRules.TestPvePoolMicroSkr);
            Assert.LessOrEqual(pveAllocated, PrototypeLeaderboardRules.TestPvePoolMicroSkr);

            long pvpAllocated = 0L;
            for (var rank = 1; rank <= 100; rank++)
                pvpAllocated += PrototypeLeaderboardRules.CalculatePvpRewardMicroSkr(
                    rank, PrototypeLeaderboardRules.TestPvpPoolMicroSkr);
            Assert.AreEqual(PrototypeLeaderboardRules.TestPvpPoolMicroSkr, pvpAllocated);
        }
    }
}
