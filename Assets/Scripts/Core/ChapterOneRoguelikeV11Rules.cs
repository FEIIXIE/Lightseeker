using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ChapterOneRoguelikeChoiceKind
    {
        None,
        Normal,
        Keystone
    }

    public enum ChapterOneLanternRoute
    {
        None,
        Wall,
        Break,
        Formation
    }

    public sealed class ChapterOneRoguelikeEligibility
    {
        public bool HasAnyAlly;
        public bool HasSteelOrGuardian;
        public bool HasGuard;
        public bool HasOutput;
        public bool HasSupport;
        public bool HasActiveHeal;
        public bool HasShieldSource;
        public bool HasFireSource;
        public bool HasFrontAndSameColumnBack;
        public int DirectAttackerCount;

        public bool HasGuardOutputSupport => HasGuard && HasOutput && HasSupport;
        public bool HasSupportAndActiveHeal => HasSupport && HasActiveHeal;
        public bool HasGuardAndSameColumnBack => HasGuard && HasFrontAndSameColumnBack;
    }

    public sealed class ChapterOneNormalEnhancementDefinition
    {
        public string Id { get; }
        public string NameKey { get; }
        public int MaximumStacks { get; }
        public IReadOnlyList<int> Values { get; }
        public IReadOnlyList<int> SecondaryValues { get; }
        public int FamilyCap { get; }

        public ChapterOneNormalEnhancementDefinition(string id, string nameKey, int maximumStacks,
            int familyCap, int[] values, int[] secondaryValues = null)
        {
            Id = id;
            NameKey = nameKey;
            MaximumStacks = maximumStacks;
            FamilyCap = familyCap;
            Values = values ?? throw new ArgumentNullException(nameof(values));
            SecondaryValues = secondaryValues ?? Array.Empty<int>();
            if (Values.Count != 5)
                throw new ArgumentException("Five rarity values are required.", nameof(values));
            if (SecondaryValues.Count != 0 && SecondaryValues.Count != 5)
                throw new ArgumentException("Secondary rarity values must be empty or contain five values.",
                    nameof(secondaryValues));
        }

        public int Value(RoguelikeRarity rarity) => Values[(int)rarity];
        public int SecondaryValue(RoguelikeRarity rarity) =>
            SecondaryValues.Count == 0 ? 0 : SecondaryValues[(int)rarity];
    }

    public sealed class ChapterOneKeystoneDefinition
    {
        public string Id { get; }
        public string NameKey { get; }

        public ChapterOneKeystoneDefinition(string id, string nameKey)
        {
            Id = id;
            NameKey = nameKey;
        }
    }

    public sealed class ChapterOneRoguelikeCandidate
    {
        public string Id;
        public int RarityId;
    }

    /// <summary>
    /// Exact executable representation of the product-approved v1.1 chapter-one contract.
    /// This intentionally remains separate from the legacy partial runtime rules until all
    /// battle effects and persistence migration are wired in one versioned activation batch.
    /// </summary>
    public static class ChapterOneRoguelikeV11Rules
    {
        public const int ContractRulesVersion = 2;
        public const string ContractId =
            "chapter01.expedition.roguelike-enhancement-and-keystone.v1.1";

        private static readonly ChapterOneNormalEnhancementDefinition[] normalDefinitions =
        {
            Normal("B01", 3, 0, 400, 600, 800, 1100, 1500),
            Normal("B02", 3, 0, 500, 700, 1000, 1400, 1900),
            Normal("B03", 2, 80, 3, 5, 7, 10, 14),
            Normal("B04", 2, 4000, 600, 900, 1200, 1700, 2300),
            Normal("B05", 1, 0, 1800, 2600, 3600, 5000, 7000),
            Normal("B06", 2, 3500, 500, 800, 1100, 1500, 2000),
            Normal("B07", 1, 0, 3500, 5000, 7000, 9500, 13000),
            Normal("B08", 2, 2500, 400, 600, 800, 1100, 1500),
            new ChapterOneNormalEnhancementDefinition("B09", "rogue.B09.name", 1, 0,
                new[] { 2500, 3500, 4500, 6000, 8000 },
                new[] { 1200, 1500, 2000, 2500, 3000 }),
            Normal("B10", 2, 28, 4, 6, 9, 13, 18),
            Normal("B11", 2, 4000, 700, 1000, 1400, 1900, 2600),
            Normal("B12", 2, 2500, 400, 600, 800, 1100, 1500),
            Normal("B13", 2, 3000, 400, 600, 900, 1300, 1800),
            Normal("B14", 1, 0, 600, 800, 1100, 1500, 2000),
            Normal("B15", 2, 3500, 600, 900, 1200, 1700, 2300),
            Normal("B16", 1, 0, 200, 300, 400, 600, 800)
        };

        private static readonly ChapterOneKeystoneDefinition[] keystoneDefinitions =
        {
            Keystone("K01"), Keystone("K02"), Keystone("K03"),
            Keystone("K04"), Keystone("K05"), Keystone("K06"),
            Keystone("K07"), Keystone("K08"), Keystone("K09")
        };

        private static readonly Dictionary<string, ChapterOneNormalEnhancementDefinition> normalById =
            BuildNormalIndex();
        private static readonly Dictionary<string, ChapterOneKeystoneDefinition> keystoneById =
            BuildKeystoneIndex();

        private static readonly string[] universalPool = { "B01", "B02", "B13", "B14", "B15" };
        private static readonly string[] wallPool = { "B02", "B03", "B07", "B08", "B09", "B14" };
        private static readonly string[] breakPool = { "B01", "B10", "B11", "B13", "B15" };
        private static readonly string[] formationPool = { "B02", "B03", "B08", "B12", "B16" };

        private static readonly Dictionary<string, string[]> keystonePools =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                { "K01", new[] { "B01", "B02", "B03", "B07", "B13" } },
                { "K02", new[] { "B02", "B03", "B08", "B12", "B16" } },
                { "K03", new[] { "B01", "B02", "B03", "B09", "B13" } },
                { "K04", new[] { "B06", "B11", "B14", "B15" } },
                { "K05", new[] { "B02", "B09", "B14", "B15", "B16" } },
                { "K06", new[] { "B01", "B04", "B05", "B10", "B13" } },
                { "K07", new[] { "B01", "B10", "B11", "B13" } },
                { "K08", new[] { "B01", "B04", "B06", "B10", "B11", "B13" } },
                { "K09", new[] { "B01", "B02", "B03", "B09", "B11", "B12", "B16" } }
            };

        public static IReadOnlyList<ChapterOneNormalEnhancementDefinition> NormalDefinitions =>
            normalDefinitions;
        public static IReadOnlyList<ChapterOneKeystoneDefinition> KeystoneDefinitions =>
            keystoneDefinitions;

        public static ChapterOneNormalEnhancementDefinition GetNormal(string id)
        {
            ChapterOneNormalEnhancementDefinition value;
            if (!normalById.TryGetValue(id ?? string.Empty, out value))
                throw new KeyNotFoundException("Unknown chapter-one normal enhancement: " + id);
            return value;
        }

        public static ChapterOneKeystoneDefinition GetKeystone(string id)
        {
            ChapterOneKeystoneDefinition value;
            if (!keystoneById.TryGetValue(id ?? string.Empty, out value))
                throw new KeyNotFoundException("Unknown chapter-one keystone: " + id);
            return value;
        }

        public static ChapterOneRoguelikeChoiceKind ChoiceKindForVictory(string nodeId, bool isBoss)
        {
            if (isBoss || string.Equals(nodeId, "M29", StringComparison.Ordinal))
                return ChapterOneRoguelikeChoiceKind.None;
            return string.Equals(nodeId, "M19", StringComparison.Ordinal)
                ? ChapterOneRoguelikeChoiceKind.Keystone
                : ChapterOneRoguelikeChoiceKind.Normal;
        }

        public static bool PreparationAffectsChoice(string preparationId,
            ChapterOneRoguelikeChoiceKind kind)
        {
            if (kind != ChapterOneRoguelikeChoiceKind.Normal) return false;
            return string.Equals(preparationId, "P09", StringComparison.Ordinal) ||
                   string.Equals(preparationId, "P10", StringComparison.Ordinal) ||
                   string.Equals(preparationId, "P11", StringComparison.Ordinal);
        }

        public static bool IsPreparationAllowedAtNode(string preparationId, string nodeId,
            string nextBattleNodeId)
        {
            if (string.Equals(nodeId, "M18", StringComparison.Ordinal) &&
                (preparationId == "P09" || preparationId == "P10" || preparationId == "P11" ||
                 preparationId == "P12")) return false;
            if (string.Equals(preparationId, "E05", StringComparison.Ordinal) &&
                string.Equals(nextBattleNodeId, "M19", StringComparison.Ordinal)) return false;
            return true;
        }

        public static bool IsNormalEligible(string id, ChapterOneRoguelikeEligibility context)
        {
            if (context == null || !context.HasAnyAlly || !normalById.ContainsKey(id ?? string.Empty))
                return false;
            switch (id)
            {
                case "B03": case "B07": return context.HasSteelOrGuardian;
                case "B04": case "B06": return context.DirectAttackerCount >= 1;
                case "B05": return context.HasFireSource;
                case "B08": return context.HasShieldSource;
                case "B09": return context.HasActiveHeal;
                case "B10": return context.DirectAttackerCount >= 2;
                case "B11": return context.HasOutput;
                case "B12": return context.HasFrontAndSameColumnBack;
                case "B16": return context.HasGuardOutputSupport;
                default: return true;
            }
        }

        public static bool IsKeystoneEligible(string id, ChapterOneRoguelikeEligibility context)
        {
            if (context == null || !context.HasAnyAlly || !keystoneById.ContainsKey(id ?? string.Empty))
                return false;
            switch (id)
            {
                case "K01": return context.HasGuard;
                case "K02": return context.HasGuardAndSameColumnBack;
                case "K03": return context.HasShieldSource;
                case "K04": return context.HasAnyAlly;
                case "K05": return context.HasSupportAndActiveHeal;
                case "K06": return context.HasFireSource;
                case "K07": return context.DirectAttackerCount >= 2;
                case "K08": return context.DirectAttackerCount >= 1;
                case "K09": return context.HasGuardOutputSupport;
                default: return false;
            }
        }

        public static int[] RarityWeights(int journeyProgressBasisPoints)
        {
            if (journeyProgressBasisPoints < 2500) return new[] { 5500, 3000, 1200, 300, 0 };
            if (journeyProgressBasisPoints < 5000) return new[] { 4300, 3100, 1900, 600, 100 };
            if (journeyProgressBasisPoints < 7500) return new[] { 3200, 3000, 2500, 1000, 300 };
            return new[] { 2000, 3000, 3000, 1500, 500 };
        }

        public static RoguelikeRarity RollRarity(uint seed, int journeyProgressBasisPoints)
        {
            var weights = RarityWeights(journeyProgressBasisPoints);
            return RollWeightedRarity(seed, weights, 0);
        }

        public static RoguelikeRarity RollEliteGuaranteedRarity(uint seed,
            int journeyProgressBasisPoints)
        {
            var weights = RarityWeights(journeyProgressBasisPoints);
            return RollWeightedRarity(seed, weights, (int)RoguelikeRarity.Rare);
        }

        public static ChapterOneRoguelikeCandidate[] GenerateKeystoneCandidates(string runId,
            int choiceOrdinal, ChapterOneRoguelikeEligibility context)
        {
            var eligible = new List<string>();
            for (var i = 0; i < keystoneDefinitions.Length; i++)
                if (IsKeystoneEligible(keystoneDefinitions[i].Id, context))
                    eligible.Add(keystoneDefinitions[i].Id);
            if (eligible.Count < 3)
                throw new InvalidOperationException("M19 requires three distinct eligible keystones.");
            var seed = StableSeed(runId, choiceOrdinal, "keystone");
            var selected = PickDistinct(eligible, seed, 3);
            return new[]
            {
                Candidate(selected[0], RoguelikeRarity.Common),
                Candidate(selected[1], RoguelikeRarity.Common),
                Candidate(selected[2], RoguelikeRarity.Common)
            };
        }

        public static ChapterOneRoguelikeCandidate[] GenerateNormalCandidates(string runId,
            int choiceOrdinal, int journeyProgressBasisPoints, bool elite,
            ChapterOneRoguelikeEligibility context, IEnumerable<RoguelikeAcquiredEffect> acquired,
            ChapterOneLanternRoute lanternRoute, string selectedKeystoneId, bool forceTwoBuildMatches)
        {
            var eligible = EligibleNormalIds(context, acquired);
            if (eligible.Count < 3)
                throw new InvalidOperationException("A normal choice requires three distinct eligible effects.");

            var seed = StableSeed(runId, choiceOrdinal, "normal");
            var selected = new List<string>(3);
            var primaryPool = !string.IsNullOrEmpty(selectedKeystoneId)
                ? Filter(eligible, KeystonePool(selectedKeystoneId))
                : Filter(eligible, RoutePool(lanternRoute));
            AddPick(selected, primaryPool, seed ^ 0xA511E9B3u);

            var buildPool = BuildSynergyPool(eligible, context, lanternRoute, selectedKeystoneId);
            AddPick(selected, Without(buildPool, selected), seed ^ 0x63D83595u);

            if (forceTwoBuildMatches && selected.Count < 2)
                AddPick(selected, Without(buildPool, selected), seed ^ 0x85EBCA6Bu);
            AddPick(selected, Without(Filter(eligible, universalPool), selected), seed ^ 0xC2B2AE35u);
            while (selected.Count < 3)
                AddPick(selected, Without(eligible, selected), seed ^ (uint)(0x9E3779B9u * selected.Count));

            var result = new ChapterOneRoguelikeCandidate[3];
            for (var i = 0; i < result.Length; i++)
                result[i] = Candidate(selected[i], RollRarity(seed ^
                    (uint)(0x9E3779B9u * (uint)(i + 1)), journeyProgressBasisPoints));
            if (elite && !ContainsRareOrBetter(result))
            {
                var slot = (int)(seed % 3u);
                result[slot].RarityId = (int)RollEliteGuaranteedRarity(seed ^ 0x27D4EB2Du,
                    journeyProgressBasisPoints);
            }
            return result;
        }

        public static IReadOnlyList<string> KeystoneConvergencePool(string keystoneId) =>
            KeystonePool(keystoneId);

        public static int SumNormalValue(IEnumerable<RoguelikeAcquiredEffect> acquired,
            string effectId, int familyCap)
        {
            var definition = GetNormal(effectId);
            var total = 0;
            if (acquired != null)
                foreach (var item in acquired)
                {
                    if (item == null || !string.Equals(item.effectId, effectId,
                            StringComparison.Ordinal)) continue;
                    total += definition.Value((RoguelikeRarity)Math.Max(0,
                        Math.Min(4, item.rarityId))) * Math.Max(0, item.stackCount);
                }
            return familyCap > 0 ? Math.Min(familyCap, Math.Max(0, total)) : Math.Max(0, total);
        }

        private static ChapterOneNormalEnhancementDefinition Normal(string id, int maximumStacks,
            int familyCap, params int[] values) =>
            new ChapterOneNormalEnhancementDefinition(id, "rogue." + id + ".name",
                maximumStacks, familyCap, values);

        private static ChapterOneKeystoneDefinition Keystone(string id) =>
            new ChapterOneKeystoneDefinition(id, "rogue." + id + ".name");

        private static Dictionary<string, ChapterOneNormalEnhancementDefinition> BuildNormalIndex()
        {
            var result = new Dictionary<string, ChapterOneNormalEnhancementDefinition>(StringComparer.Ordinal);
            for (var i = 0; i < normalDefinitions.Length; i++) result.Add(normalDefinitions[i].Id,
                normalDefinitions[i]);
            return result;
        }

        private static Dictionary<string, ChapterOneKeystoneDefinition> BuildKeystoneIndex()
        {
            var result = new Dictionary<string, ChapterOneKeystoneDefinition>(StringComparer.Ordinal);
            for (var i = 0; i < keystoneDefinitions.Length; i++) result.Add(keystoneDefinitions[i].Id,
                keystoneDefinitions[i]);
            return result;
        }

        private static List<string> EligibleNormalIds(ChapterOneRoguelikeEligibility context,
            IEnumerable<RoguelikeAcquiredEffect> acquired)
        {
            var result = new List<string>();
            for (var i = 0; i < normalDefinitions.Length; i++)
            {
                var definition = normalDefinitions[i];
                if (!IsNormalEligible(definition.Id, context)) continue;
                if (RoguelikeEnhancementRules.StackCount(acquired, definition.Id) >= definition.MaximumStacks)
                    continue;
                result.Add(definition.Id);
            }
            return result;
        }

        private static List<string> BuildSynergyPool(IEnumerable<string> eligible,
            ChapterOneRoguelikeEligibility context, ChapterOneLanternRoute lanternRoute,
            string selectedKeystoneId)
        {
            var desired = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in RoutePool(lanternRoute)) desired.Add(id);
            foreach (var id in KeystonePool(selectedKeystoneId)) desired.Add(id);
            if (context.HasFireSource) { desired.Add("B04"); desired.Add("B05"); }
            if (context.HasActiveHeal) { desired.Add("B09"); desired.Add("B15"); }
            if (context.HasShieldSource) { desired.Add("B08"); desired.Add("B09"); }
            if (context.DirectAttackerCount >= 2) { desired.Add("B10"); desired.Add("B11"); }
            var result = new List<string>();
            foreach (var id in eligible) if (desired.Contains(id)) result.Add(id);
            return result;
        }

        private static IReadOnlyList<string> RoutePool(ChapterOneLanternRoute route)
        {
            switch (route)
            {
                case ChapterOneLanternRoute.Wall: return wallPool;
                case ChapterOneLanternRoute.Break: return breakPool;
                case ChapterOneLanternRoute.Formation: return formationPool;
                default: return Array.Empty<string>();
            }
        }

        private static IReadOnlyList<string> KeystonePool(string id)
        {
            string[] pool;
            return !string.IsNullOrEmpty(id) && keystonePools.TryGetValue(id, out pool)
                ? pool
                : Array.Empty<string>();
        }

        private static RoguelikeRarity RollWeightedRarity(uint seed, IReadOnlyList<int> weights,
            int minimumIndex)
        {
            var total = 0;
            for (var i = minimumIndex; i < weights.Count; i++) total += Math.Max(0, weights[i]);
            if (total <= 0) throw new InvalidOperationException("Rarity weights must contain a positive value.");
            var roll = (int)(seed % (uint)total);
            var cumulative = 0;
            for (var i = minimumIndex; i < weights.Count; i++)
            {
                cumulative += Math.Max(0, weights[i]);
                if (roll < cumulative) return (RoguelikeRarity)i;
            }
            return RoguelikeRarity.Legendary;
        }

        private static uint StableSeed(string runId, int choiceOrdinal, string kind)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run id is required.",
                nameof(runId));
            return RoguelikeEnhancementRules.StableHash(runId + "|" + choiceOrdinal + "|" +
                ContractId + "|" + kind);
        }

        private static List<string> PickDistinct(IReadOnlyList<string> values, uint seed, int count)
        {
            var remaining = new List<string>(values);
            var result = new List<string>();
            for (var i = 0; i < count; i++)
            {
                var index = (int)((seed ^ (uint)(0x9E3779B9u * (uint)(i + 1))) %
                    (uint)remaining.Count);
                result.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            return result;
        }

        private static void AddPick(ICollection<string> selected, IReadOnlyList<string> pool, uint seed)
        {
            if (pool == null || pool.Count == 0) return;
            selected.Add(pool[(int)(seed % (uint)pool.Count)]);
        }

        private static ChapterOneRoguelikeCandidate Candidate(string id, RoguelikeRarity rarity) =>
            new ChapterOneRoguelikeCandidate { Id = id, RarityId = (int)rarity };

        private static List<string> Filter(IEnumerable<string> source, IReadOnlyList<string> allowed)
        {
            var set = new HashSet<string>(allowed ?? Array.Empty<string>(), StringComparer.Ordinal);
            var result = new List<string>();
            foreach (var id in source) if (set.Contains(id)) result.Add(id);
            return result;
        }

        private static List<string> Without(IEnumerable<string> source, IEnumerable<string> excluded)
        {
            var set = new HashSet<string>(excluded ?? Array.Empty<string>(), StringComparer.Ordinal);
            var result = new List<string>();
            foreach (var id in source) if (!set.Contains(id)) result.Add(id);
            return result;
        }

        private static bool ContainsRareOrBetter(IEnumerable<ChapterOneRoguelikeCandidate> candidates)
        {
            foreach (var candidate in candidates)
                if (candidate.RarityId >= (int)RoguelikeRarity.Rare) return true;
            return false;
        }
    }
}
