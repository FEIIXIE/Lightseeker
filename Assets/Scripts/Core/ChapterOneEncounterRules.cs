using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class ChapterOneEncounterUnit
    {
        public ChapterOneEncounterUnit(string definitionId, int runLevel)
            : this(definitionId, runLevel, -1)
        {
        }

        public ChapterOneEncounterUnit(string definitionId, int runLevel, int enemySlot)
        {
            DefinitionId = definitionId;
            RunLevel = Math.Max(1, Math.Min(3, runLevel));
            EnemySlot = enemySlot < 0 ? -1 : Math.Min(5, enemySlot);
        }

        public string DefinitionId { get; private set; }
        public int RunLevel { get; private set; }
        public int EnemySlot { get; private set; }
    }

    public sealed class ChapterOneEncounter
    {
        private readonly ChapterOneEncounterUnit[] units;

        public ChapterOneEncounter(
            string nodeId,
            int generationVersion,
            uint battleSeed,
            int coinReward,
            bool isElite,
            bool isBoss,
            IEnumerable<ChapterOneEncounterUnit> generatedUnits)
            : this(nodeId, generationVersion, battleSeed, coinReward, isElite, isBoss,
                generatedUnits, "D0", 0, 0f)
        {
        }

        public ChapterOneEncounter(
            string nodeId,
            int generationVersion,
            uint battleSeed,
            int coinReward,
            bool isElite,
            bool isBoss,
            IEnumerable<ChapterOneEncounterUnit> generatedUnits,
            string difficultyId,
            int formationTier,
            float threatBudget)
        {
            NodeId = nodeId;
            GenerationVersion = generationVersion;
            BattleSeed = battleSeed;
            CoinReward = Math.Max(0, coinReward);
            IsElite = isElite;
            IsBoss = isBoss;
            DifficultyId = string.IsNullOrWhiteSpace(difficultyId) ? "D0" : difficultyId;
            FormationTier = Math.Max(0, Math.Min(3, formationTier));
            ThreatBudget = Math.Max(0f, threatBudget);
            units = generatedUnits == null
                ? new ChapterOneEncounterUnit[0]
                : new List<ChapterOneEncounterUnit>(generatedUnits).ToArray();
        }

        public string NodeId { get; private set; }
        public int GenerationVersion { get; private set; }
        public uint BattleSeed { get; private set; }
        public int CoinReward { get; private set; }
        public bool IsElite { get; private set; }
        public bool IsBoss { get; private set; }
        public string DifficultyId { get; private set; }
        public int FormationTier { get; private set; }
        public float ThreatBudget { get; private set; }
        public IReadOnlyList<ChapterOneEncounterUnit> Units { get { return units; } }
    }

    /// <summary>
    /// E23/E16 chapter-one encounter authority. Every decision is derived from the
    /// run map seed, node id and version; retrying or reloading a node returns the
    /// identical encounter and battle seed.
    /// </summary>
    public static class ChapterOneEncounterRules
    {
        public const int EncounterGenerationVersion = 2;
        public const int EncounterRewardVersion = 2;

        private const string N1 = "enemy.n1.guard";
        private const string N2 = "enemy.n2.fighter";
        private const string N3 = "enemy.n3.ranged";
        private const string N4 = "enemy.n4.skirmisher";
        private const string N5 = "enemy.n5.support";
        private const string E1 = "enemy.e1.fortress";
        private const string E2 = "enemy.e2.hunter";
        private const string E3 = "enemy.e3.caster";
        private static readonly string[] Tanks = { N1, N2 };
        private static readonly string[] Damage = { N2, N3, N4 };
        private static readonly string[] Normals = { N1, N2, N3, N4, N5 };
        private static readonly string[] NormalsNoSupport = { N1, N2, N3, N4 };
        private static readonly string[] Elites = { E1, E2, E3 };

        public static ChapterOneEncounter Generate(
            uint mapSeed,
            ExpeditionMapNode node,
            bool isFirstActualNormalBattle)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var seed = Mix(mapSeed, node.Id, EncounterGenerationVersion);
            var random = new StableRandom(seed);
            var isBoss = node.Id == "M29" || node.Type == ExpeditionNodeType.Boss;
            var isElite = node.Id == "M19" || node.Type == ExpeditionNodeType.EliteBattle;
            var progress = node.JourneyProgress;
            List<ChapterOneEncounterUnit> units;
            int reward;

            if (isBoss)
            {
                units = GenerateBoss(random);
                reward = 0;
            }
            else if (node.Id == "M19")
            {
                units = GenerateGuardian(random);
                reward = 8;
            }
            else if (isElite)
            {
                units = GenerateElite(random, progress);
                reward = EliteReward(progress);
            }
            else if (node.Id == "M28")
            {
                units = GenerateM28(random);
                reward = 6;
            }
            else if (isFirstActualNormalBattle)
            {
                units = GenerateQ2(random);
                reward = NormalReward(progress);
            }
            else
            {
                units = GenerateNormal(random, progress);
                reward = NormalReward(progress);
            }

            return new ChapterOneEncounter(
                node.Id, EncounterGenerationVersion,
                Mix(seed, "battle", EncounterRewardVersion), reward,
                isElite, isBoss, units);
        }

        public static bool IsThreeStarAllowed(float journeyProgress)
        {
            return journeyProgress >= 85f;
        }

        private static List<ChapterOneEncounterUnit> GenerateNormal(StableRandom random, float progress)
        {
            if (progress < 20f)
            {
                return random.Roll(70) ? GenerateQ2(random) : ApplyLevels(GenerateQ3BalancedIds(random), 0);
            }
            if (progress < 45f)
            {
                var ids = PickSkeleton(random, 15, 45, 25);
                return ApplyEarlyLevels(random, ids, 75);
            }
            if (progress < 62.3f)
            {
                var ids = PickSkeleton(random, 0, 30, 20);
                return ApplyCountedLevelTwos(random, ids, random.WeightedIndex(30, 55, 15));
            }
            if (progress < 85f)
            {
                var ids = PickSkeleton(random, 0, 15, 15);
                var bucket = random.WeightedIndex(5, 25, 50, 20);
                return ApplyCountedLevelTwos(random, ids, bucket == 0 ? 0 : bucket);
            }

            var late = random.WeightedIndex(40, 35, 25);
            if (late == 2)
            {
                var ids = GenerateQ3AssaultIds(random);
                return ApplySingleThreeStar(random, ids);
            }
            return ApplyCountedLevelTwos(random, GenerateQ4Ids(random), late == 0 ? 2 : 3);
        }

        private static List<string> PickSkeleton(StableRandom random, int q2Weight, int balancedWeight, int assaultWeight)
        {
            var q4Weight = 100 - q2Weight - balancedWeight - assaultWeight;
            var pick = random.WeightedIndex(q2Weight, balancedWeight, assaultWeight, q4Weight);
            if (pick == 0) return GenerateQ2Ids(random);
            if (pick == 1) return GenerateQ3BalancedIds(random);
            if (pick == 2) return GenerateQ3AssaultIds(random);
            return GenerateQ4Ids(random);
        }

        private static List<ChapterOneEncounterUnit> GenerateElite(StableRandom random, float progress)
        {
            var eliteId = random.Pick(Elites);
            var followerCount = progress < 45f && random.Roll(80) ? 2 : 3;
            var followerIds = GenerateEliteFollowers(random, eliteId, followerCount);
            var units = new List<ChapterOneEncounterUnit>
            {
                new ChapterOneEncounterUnit(eliteId, progress >= 62.3f ? 2 : 1)
            };
            var levelTwos = 0;
            if (progress >= 85f) levelTwos = random.Roll(70) ? 2 : 3;
            else if (progress >= 62.3f) levelTwos = random.Roll(60) ? 1 : 2;
            else if (progress >= 45f) levelTwos = random.Roll(50) ? 0 : 1;
            Shuffle(random, followerIds);
            for (var i = 0; i < followerIds.Count; i++)
                units.Add(new ChapterOneEncounterUnit(followerIds[i], i < levelTwos ? 2 : 1));
            return units;
        }

        private static List<ChapterOneEncounterUnit> GenerateGuardian(StableRandom random)
        {
            var eliteId = random.Pick(Elites);
            var followers = GenerateEliteFollowers(random, eliteId, 3);
            Shuffle(random, followers);
            var result = new List<ChapterOneEncounterUnit>
            {
                new ChapterOneEncounterUnit(eliteId, 2)
            };
            for (var i = 0; i < followers.Count; i++)
                result.Add(new ChapterOneEncounterUnit(followers[i], i == 0 ? 2 : 1));
            return result;
        }

        private static List<ChapterOneEncounterUnit> GenerateBoss(StableRandom random)
        {
            var b1 = random.Roll(50);
            var ids = b1
                ? new[] { N2, N3, random.Pick(new[] { N4, N5 }) }
                : new[] { random.Pick(new[] { N1, N2 }), N3, random.Pick(new[] { N2, N4 }) };
            var result = new List<ChapterOneEncounterUnit>
            {
                new ChapterOneEncounterUnit(b1 ? "enemy.b1.heavy" : "enemy.b2.hunter", 1)
            };
            foreach (var id in ids) result.Add(new ChapterOneEncounterUnit(id, 2));
            return result;
        }

        private static List<ChapterOneEncounterUnit> GenerateM28(StableRandom random)
        {
            var pick = random.WeightedIndex(50, 25, 25);
            if (pick == 2) return ApplySingleThreeStar(random, GenerateQ3AssaultIds(random));
            return ApplyCountedLevelTwos(random, GenerateQ4Ids(random), pick == 0 ? 3 : 4);
        }

        private static List<ChapterOneEncounterUnit> GenerateQ2(StableRandom random)
        {
            return ApplyLevels(GenerateQ2Ids(random), 0);
        }

        private static List<string> GenerateQ2Ids(StableRandom random)
        {
            var first = random.Pick(Tanks);
            var secondPool = new List<string>(Damage);
            secondPool.Remove(first);
            return new List<string> { first, random.Pick(secondPool) };
        }

        private static List<string> GenerateQ3BalancedIds(StableRandom random)
        {
            var result = new List<string> { random.Pick(Tanks), random.Pick(Damage) };
            result.Add(PickWithCaps(random, result, Normals));
            return result;
        }

        private static List<string> GenerateQ3AssaultIds(StableRandom random)
        {
            return new List<string> { N2, random.Pick(new[] { N3, N4 }), random.Pick(NormalsNoSupport) };
        }

        private static List<string> GenerateQ4Ids(StableRandom random)
        {
            var result = new List<string>
            {
                random.Pick(Tanks), random.Pick(Damage), random.Pick(Damage)
            };
            result.Add(PickWithCaps(random, result, Normals));
            if (AllSame(result)) result[result.Count - 1] = N3;
            return result;
        }

        private static List<string> GenerateEliteFollowers(StableRandom random, string eliteId, int count)
        {
            var result = new List<string>();
            if (eliteId == E1)
            {
                result.Add(random.Pick(new[] { N3, N4 }));
                while (result.Count < count) result.Add(PickWithCaps(random, result, new[] { N2, N3, N5 }));
            }
            else if (eliteId == E2)
            {
                result.Add(random.Pick(Tanks));
                while (result.Count < count) result.Add(PickWithCaps(random, result, new[] { N2, N3, N4 }));
            }
            else
            {
                result.Add(random.Pick(Tanks));
                if (result.Count < count) result.Add(random.Pick(new[] { N3, N4 }));
                while (result.Count < count) result.Add(random.Pick(NormalsNoSupport));
            }
            return result;
        }

        private static string PickWithCaps(StableRandom random, List<string> existing, string[] pool)
        {
            var legal = new List<string>();
            foreach (var id in pool)
            {
                if (id == N5 && Count(existing, N5) >= 1) continue;
                if (id == N4 && Count(existing, N4) >= 2) continue;
                legal.Add(id);
            }
            return random.Pick(legal);
        }

        private static List<ChapterOneEncounterUnit> ApplyEarlyLevels(StableRandom random, List<string> ids, int allOneChance)
        {
            return ApplyCountedLevelTwos(random, ids, random.Roll(allOneChance) ? 0 : 1);
        }

        private static List<ChapterOneEncounterUnit> ApplyCountedLevelTwos(StableRandom random, List<string> ids, int count)
        {
            Shuffle(random, ids);
            var result = new List<ChapterOneEncounterUnit>();
            for (var i = 0; i < ids.Count; i++)
                result.Add(new ChapterOneEncounterUnit(ids[i], i < count ? 2 : 1));
            return result;
        }

        private static List<ChapterOneEncounterUnit> ApplySingleThreeStar(StableRandom random, List<string> ids)
        {
            var eligible = new List<int>();
            for (var i = 0; i < ids.Count; i++) if (ids[i] != N5) eligible.Add(i);
            var selected = eligible[random.Next(eligible.Count)];
            var result = new List<ChapterOneEncounterUnit>();
            for (var i = 0; i < ids.Count; i++)
                result.Add(new ChapterOneEncounterUnit(ids[i], i == selected ? 3 : 1));
            return result;
        }

        private static List<ChapterOneEncounterUnit> ApplyLevels(List<string> ids, int levelTwoCount)
        {
            var result = new List<ChapterOneEncounterUnit>();
            for (var i = 0; i < ids.Count; i++)
                result.Add(new ChapterOneEncounterUnit(ids[i], i < levelTwoCount ? 2 : 1));
            return result;
        }

        private static int NormalReward(float progress)
        {
            if (progress < 20f) return 3;
            if (progress < 45f) return 4;
            if (progress < 85f) return 5;
            return 6;
        }

        private static int EliteReward(float progress)
        {
            if (progress < 45f) return 6;
            if (progress < 62.3f) return 7;
            if (progress < 85f) return 8;
            return 9;
        }

        private static bool AllSame(List<string> values)
        {
            for (var i = 1; i < values.Count; i++) if (values[i] != values[0]) return false;
            return true;
        }

        private static int Count(List<string> values, string target)
        {
            var result = 0;
            foreach (var value in values) if (value == target) result++;
            return result;
        }

        private static void Shuffle<T>(StableRandom random, List<T> values)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var other = random.Next(i + 1);
                var value = values[i];
                values[i] = values[other];
                values[other] = value;
            }
        }

        public static uint Mix(uint seed, string value, int version)
        {
            var hash = seed == 0 ? 2166136261u : seed;
            hash = (hash ^ (uint)version) * 16777619u;
            if (value != null)
                foreach (var character in value) hash = (hash ^ character) * 16777619u;
            return hash == 0 ? 0x9E3779B9u : hash;
        }

        private sealed class StableRandom
        {
            private uint state;
            public StableRandom(uint seed) { state = seed == 0 ? 0x9E3779B9u : seed; }
            private uint NextUInt()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
            public int Next(int maximum) { return maximum <= 1 ? 0 : (int)(NextUInt() % (uint)maximum); }
            public bool Roll(int percent) { return Next(100) < percent; }
            public T Pick<T>(IList<T> values) { return values[Next(values.Count)]; }
            public int WeightedIndex(params int[] weights)
            {
                var total = 0;
                foreach (var weight in weights) total += Math.Max(0, weight);
                var roll = Next(total);
                for (var i = 0; i < weights.Length; i++)
                {
                    roll -= Math.Max(0, weights[i]);
                    if (roll < 0) return i;
                }
                return weights.Length - 1;
            }
        }
    }
}
