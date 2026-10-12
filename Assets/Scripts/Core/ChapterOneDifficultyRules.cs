using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class ChapterOneDifficultyDefinition
    {
        public ChapterOneDifficultyDefinition(string difficultyId, string displayNameKey,
            int entryPriceSkr, string requiredBossClearDifficultyId)
        {
            DifficultyId = difficultyId;
            DisplayNameKey = displayNameKey;
            EntryPriceSkr = Math.Max(0, entryPriceSkr);
            RequiredBossClearDifficultyId = requiredBossClearDifficultyId;
        }

        public string DifficultyId { get; private set; }
        public string DisplayNameKey { get; private set; }
        public int EntryPriceSkr { get; private set; }
        public string RequiredBossClearDifficultyId { get; private set; }
    }

    public static class ChapterOneDifficultyRules
    {
        public const int LegacyEncounterGenerationVersion = 3;
        public const string LegacyEncounterTableVersion = "chapter01.encounters.v3";
        public const int EncounterGenerationVersion = 4;
        public const string EncounterTableVersion = "chapter01.encounters.v4";
        public const string RewardTableVersion = "chapter01.expedition.rewards.v1.8";
        public const string MapVersion = "CHAPTER-01-MAP-MERGED-V0.4";
        public const string RulesVersion = "chapter01.expedition.entry.v1.3";
        public const string TimeRulesVersion = "expedition.utc-same-day-validity.v1.0";
        public const string AuthoritativeTimeZone = "UTC";
        public const int DailyFormalRunLimit = 5;

        private const string N1 = "enemy.n1.guard";
        private const string N2 = "enemy.n2.fighter";
        private const string N3 = "enemy.n3.ranged";
        private const string N4 = "enemy.n4.skirmisher";
        private const string N5 = "enemy.n5.support";
        private const string E1 = "enemy.e1.fortress";
        private const string E2 = "enemy.e2.hunter";
        private const string E3 = "enemy.e3.caster";

        private static readonly ChapterOneDifficultyDefinition[] definitions =
        {
            new ChapterOneDifficultyDefinition("D0", "expedition.difficulty.d0", 0, null),
            new ChapterOneDifficultyDefinition("D1", "expedition.difficulty.d1", 50, "D0"),
            new ChapterOneDifficultyDefinition("D2", "expedition.difficulty.d2", 100, "D1"),
            new ChapterOneDifficultyDefinition("D3", "expedition.difficulty.d3", 200, "D2")
        };

        public static IReadOnlyList<ChapterOneDifficultyDefinition> Definitions
        {
            get { return definitions; }
        }

        public static ChapterOneDifficultyDefinition Get(string difficultyId)
        {
            for (var i = 0; i < definitions.Length; i++)
                if (string.Equals(definitions[i].DifficultyId, difficultyId, StringComparison.Ordinal))
                    return definitions[i];
            throw new ArgumentException("Unknown chapter-one difficulty.", nameof(difficultyId));
        }

        public static ChapterOneEncounter GenerateEncounter(uint mapSeed, ExpeditionMapNode node,
            string difficultyId, int completedBattleCount)
        {
            return GenerateEncounter(mapSeed, node, difficultyId, completedBattleCount,
                EncounterTableVersion);
        }

        public static ChapterOneEncounter GenerateEncounter(uint mapSeed, ExpeditionMapNode node,
            string difficultyId, int completedBattleCount, string encounterTableVersion)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var generationVersion = GenerationVersion(encounterTableVersion);
            var difficultyIndex = DifficultyIndex(difficultyId);
            var seed = ChapterOneEncounterRules.Mix(
                ChapterOneEncounterRules.Mix(mapSeed, node.Id, generationVersion),
                difficultyId, generationVersion);
            var random = new DeterministicRandom(seed);
            var isBoss = node.Id == "M29" || node.Type == ExpeditionNodeType.Boss;
            var isElite = node.Id == "M19" || node.Type == ExpeditionNodeType.EliteBattle;
            List<UnitSpec> specs;

            if (generationVersion == EncounterGenerationVersion && difficultyIndex == 0)
                specs = D0V4Specs(node, completedBattleCount, isBoss, isElite, random);
            else if (isBoss) specs = BossSpecs(difficultyIndex, random);
            else if (node.Id == "M19") specs = GuardianSpecs(difficultyIndex, random);
            else if (node.Id == "M28") specs = M28Specs(difficultyIndex, random);
            else
            {
                var stage = JourneyStage(node.JourneyProgress);
                specs = NormalSpecs(difficultyIndex, stage, random);
                if (isElite) ReplaceWithElite(specs, difficultyIndex, stage, random);
                if (difficultyIndex == 0 && completedBattleCount == 0 &&
                    node.Type == ExpeditionNodeType.NormalBattle)
                    specs = TutorialSpecs(random);
            }

            var formationTier = FormationTier(difficultyIndex, node.JourneyProgress, isBoss);
            var units = AssignSlots(specs, random, isBoss);
            var reward = isBoss ? 0 : isElite ? EliteReward(node.JourneyProgress) :
                NormalReward(node.JourneyProgress);
            return new ChapterOneEncounter(
                node.Id,
                generationVersion,
                ChapterOneEncounterRules.Mix(seed, "battle", generationVersion),
                reward,
                isElite,
                isBoss,
                units,
                difficultyId,
                formationTier,
                ThreatBudget(specs));
        }

        private static int GenerationVersion(string encounterTableVersion)
        {
            if (string.Equals(encounterTableVersion, EncounterTableVersion,
                    StringComparison.Ordinal)) return EncounterGenerationVersion;
            if (string.Equals(encounterTableVersion, LegacyEncounterTableVersion,
                    StringComparison.Ordinal)) return LegacyEncounterGenerationVersion;
            throw new ArgumentException("Unknown encounter table version.",
                nameof(encounterTableVersion));
        }

        private static List<UnitSpec> D0V4Specs(ExpeditionMapNode node,
            int completedBattleCount, bool isBoss, bool isElite,
            DeterministicRandom random)
        {
            if (isBoss) return D0V4BossSpecs(random);
            if (string.Equals(node.Id, "M19", StringComparison.Ordinal))
            {
                var guardian = BuildRoleBalanced(P(0, 0, 3), random);
                guardian.Insert(0, new UnitSpec(random.Pick(E1, E2, E3), 1));
                return guardian;
            }

            StarProfile profile;
            if (completedBattleCount == 0 &&
                node.Type == ExpeditionNodeType.NormalBattle)
                profile = P(0, 0, 1);
            else if (node.JourneyProgress < 20f) profile = P(0, 0, 2);
            else if (node.JourneyProgress < 45f) profile = P(0, 0, 3);
            else if (node.JourneyProgress < 62.3f) profile = P(0, 1, 2);
            else if (node.JourneyProgress < 85f) profile = P(0, 2, 2);
            else profile = P(0, 3, 2);

            var result = BuildRoleBalanced(profile, random);
            if (isElite) ReplaceHighestWithD0V4Elite(result, random);
            return result;
        }

        private static List<UnitSpec> D0V4BossSpecs(DeterministicRandom random)
        {
            var heavy = random.Roll(50);
            var result = BuildRoleBalanced(heavy ? P(0, 2, 1) : P(0, 0, 2), random);
            result.Insert(0, new UnitSpec(heavy ? "enemy.b1.heavy" : "enemy.b2.hunter", 1));
            return result;
        }

        private static void ReplaceHighestWithD0V4Elite(List<UnitSpec> specs,
            DeterministicRandom random)
        {
            if (specs.Count == 0) return;
            var highest = 0;
            for (var index = 1; index < specs.Count; index++)
                if (specs[index].Level > specs[highest].Level) highest = index;
            specs[highest] = new UnitSpec(random.Pick(E1, E2, E3), specs[highest].Level);
        }

        private static List<UnitSpec> TutorialSpecs(DeterministicRandom random)
        {
            return new List<UnitSpec>
            {
                new UnitSpec(random.Roll(50) ? N1 : N2, 1),
                new UnitSpec(random.Roll(50) ? N3 : N4, 1)
            };
        }

        private static List<UnitSpec> NormalSpecs(int difficulty, int stage,
            DeterministicRandom random)
        {
            // Rows are D0-D3, columns are A-F. Each entry is [three-star, two-star, one-star].
            var curve = new[]
            {
                new[] { P(0,0,2), P(0,0,3), P(0,1,2), P(0,2,3), P(0,4,1), P(1,4,0) },
                new[] { P(0,0,3), P(0,1,2), P(0,1,3), P(0,3,2), P(0,5,1), P(1,5,0) },
                new[] { P(0,1,2), P(0,1,3), P(0,2,2), P(1,3,1), P(2,4,0), P(3,3,0) },
                new[] { P(0,1,3), P(0,2,2), P(0,3,2), P(1,4,1), P(3,3,0), P(4,2,0) }
            };
            return BuildRoleBalanced(curve[difficulty][Math.Max(0, Math.Min(5, stage))], random);
        }

        private static List<UnitSpec> M28Specs(int difficulty, DeterministicRandom random)
        {
            var profiles = new[] { P(2,3,0), P(2,4,0), P(4,2,0), P(5,1,0) };
            return BuildRoleBalanced(profiles[difficulty], random);
        }

        private static List<UnitSpec> GuardianSpecs(int difficulty, DeterministicRandom random)
        {
            var eliteLevel = difficulty < 2 ? 2 : 3;
            var elite = new UnitSpec(random.Pick(E1, E2, E3), eliteLevel);
            var followers = difficulty == 0 ? P(0,1,2) :
                difficulty == 1 ? P(0,2,2) :
                difficulty == 2 ? P(0,2,2) : P(0,3,2);
            var result = BuildRoleBalanced(followers, random);
            result.Insert(0, elite);
            return result;
        }

        private static List<UnitSpec> BossSpecs(int difficulty, DeterministicRandom random)
        {
            var boss = random.Roll(50) ? "enemy.b1.heavy" : "enemy.b2.hunter";
            var bossLevel = difficulty < 2 ? 1 : 2;
            var followerProfiles = new[] { P(1,3,0), P(2,2,0), P(3,1,0), P(4,0,0) };
            var result = BuildRoleBalanced(followerProfiles[difficulty], random);
            result.Insert(0, new UnitSpec(boss, bossLevel));
            return result;
        }

        private static void ReplaceWithElite(List<UnitSpec> specs, int difficulty, int stage,
            DeterministicRandom random)
        {
            if (specs.Count == 0) return;
            var eliteLevel = difficulty >= 2 && stage >= 4 ? 3 :
                difficulty >= 1 || stage >= 3 ? 2 : 1;
            var replace = random.Next(specs.Count);
            specs[replace] = new UnitSpec(random.Pick(E1, E2, E3), eliteLevel);
        }

        private static List<UnitSpec> BuildRoleBalanced(StarProfile profile,
            DeterministicRandom random)
        {
            var count = profile.Three + profile.Two + profile.One;
            var ids = new List<string>(count);
            if (count > 0) ids.Add(random.Roll(55) ? N1 : N2);
            if (count > 1) ids.Add(random.Roll(50) ? N3 : N4);
            if (count > 3) ids.Add(N2);
            if (count > 4) ids.Add(N5);
            while (ids.Count < count)
                ids.Add(random.Pick(N1, N2, N3, N4));
            Shuffle(ids, random);

            var levels = new List<int>(count);
            for (var i = 0; i < profile.Three; i++) levels.Add(3);
            for (var i = 0; i < profile.Two; i++) levels.Add(2);
            for (var i = 0; i < profile.One; i++) levels.Add(1);
            Shuffle(levels, random);
            var result = new List<UnitSpec>(count);
            for (var i = 0; i < count; i++) result.Add(new UnitSpec(ids[i], levels[i]));
            return result;
        }

        private static List<ChapterOneEncounterUnit> AssignSlots(List<UnitSpec> specs,
            DeterministicRandom random, bool isBoss)
        {
            if (isBoss)
            {
                var bossSlots = new[] { 1, 0, 2, 3, 5 };
                var bossUnits = new List<ChapterOneEncounterUnit>(specs.Count);
                for (var i = 0; i < specs.Count; i++)
                    bossUnits.Add(new ChapterOneEncounterUnit(
                        specs[i].DefinitionId, specs[i].Level, bossSlots[Math.Min(i, bossSlots.Length - 1)]));
                bossUnits.Sort((left, right) => left.EnemySlot.CompareTo(right.EnemySlot));
                return bossUnits;
            }

            var availableFront = new List<int> { 1, 0, 2 };
            var availableBack = new List<int> { 4, 3, 5 };
            var result = new List<ChapterOneEncounterUnit>(specs.Count);
            specs.Sort((left, right) => RolePriority(left.DefinitionId).CompareTo(RolePriority(right.DefinitionId)));
            foreach (var spec in specs)
            {
                var backRole = IsBackRole(spec.DefinitionId);
                var preferred = backRole ? availableBack : availableFront;
                var fallback = backRole ? availableFront : availableBack;
                var source = preferred.Count > 0 ? preferred : fallback;
                var index = source.Count == 1 ? 0 : random.Next(source.Count);
                var slot = source[index];
                source.RemoveAt(index);
                result.Add(new ChapterOneEncounterUnit(spec.DefinitionId, spec.Level, slot));
            }
            result.Sort((left, right) => left.EnemySlot.CompareTo(right.EnemySlot));
            return result;
        }

        private static bool IsBackRole(string id)
        {
            return id == N3 || id == N5 || id == E3;
        }

        private static int RolePriority(string id)
        {
            if (id == N1 || id == N2 || id == E1) return 0;
            if (id == N4 || id == E2) return 1;
            return 2;
        }

        private static float ThreatBudget(List<UnitSpec> specs)
        {
            var result = 0f;
            foreach (var spec in specs)
            {
                var elite = spec.DefinitionId == E1 || spec.DefinitionId == E2 || spec.DefinitionId == E3;
                var weight = spec.Level == 3 ? 2.4f : spec.Level == 2 ? 1.6f : 1f;
                result += elite ? weight * 2f : weight;
            }
            return result;
        }

        private static int FormationTier(int difficulty, float progress, bool isBoss)
        {
            if (isBoss) return difficulty >= 2 ? 3 : 2;
            if (difficulty == 3) return progress >= 20f ? 3 : 2;
            if (difficulty == 2) return progress >= 62.3f ? 3 : progress >= 8f ? 2 : 1;
            if (difficulty == 1) return progress >= 45f ? 2 : 1;
            if (progress >= 45f) return 2;
            return progress >= 8f ? 1 : 0;
        }

        private static int JourneyStage(float progress)
        {
            if (progress < 8f) return 0;
            if (progress < 20f) return 1;
            if (progress < 45f) return 2;
            if (progress < 62.3f) return 3;
            if (progress < 85f) return 4;
            return 5;
        }

        private static int DifficultyIndex(string difficultyId)
        {
            Get(difficultyId);
            return difficultyId[1] - '0';
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

        private static StarProfile P(int three, int two, int one)
        {
            return new StarProfile(three, two, one);
        }

        private static void Shuffle<T>(List<T> values, DeterministicRandom random)
        {
            for (var i = values.Count - 1; i > 0; i--)
            {
                var other = random.Next(i + 1);
                var value = values[i];
                values[i] = values[other];
                values[other] = value;
            }
        }

        private sealed class UnitSpec
        {
            public UnitSpec(string definitionId, int level)
            {
                DefinitionId = definitionId;
                Level = level;
            }

            public string DefinitionId { get; private set; }
            public int Level { get; private set; }
        }

        private struct StarProfile
        {
            public StarProfile(int three, int two, int one)
            {
                Three = three;
                Two = two;
                One = one;
            }

            public int Three;
            public int Two;
            public int One;
        }

        private sealed class DeterministicRandom
        {
            private uint state;
            public DeterministicRandom(uint seed) { state = seed == 0 ? 0x9E3779B9u : seed; }
            private uint NextUInt()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
            public int Next(int maximum) { return maximum <= 1 ? 0 : (int)(NextUInt() % (uint)maximum); }
            public bool Roll(int percent) { return Next(100) < percent; }
            public string Pick(params string[] values) { return values[Next(values.Length)]; }
        }
    }
}
