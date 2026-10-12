using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ExpeditionPreparationCategory
    {
        Combat,
        Economy,
        Build
    }

    public sealed class ExpeditionPreparationDefinition
    {
        public ExpeditionPreparationDefinition(string id, string nameZhHans,
            ExpeditionPreparationCategory category, int weightPpm)
        {
            Id = id;
            NameZhHans = nameZhHans;
            Category = category;
            WeightPpm = weightPpm;
        }

        public string Id { get; private set; }
        public string NameZhHans { get; private set; }
        public ExpeditionPreparationCategory Category { get; private set; }
        public int WeightPpm { get; private set; }
    }

    public sealed class ExpeditionPreparationEligibility
    {
        public bool HasValidLantern;
        public bool HasBackpackItem;
        public bool HasLaterCombat = true;
        public bool HasFutureMerchant = true;
        public bool HasEligibleCharacterSale = true;
        public bool HasEligibleNormalEnhancement = true;
        public bool HasNaturalRecruitRefresh = true;
    }

    public static class ChapterOneExpeditionPreparationRules
    {
        public const int RulesVersion = 12;
        public const string ContractId = "chapter01.expedition.preparation-pool.v1.2";

        private static readonly ExpeditionPreparationDefinition[] definitions =
        {
            new ExpeditionPreparationDefinition("P01", "固守阵线", ExpeditionPreparationCategory.Combat, 250000),
            new ExpeditionPreparationDefinition("P02", "抢潮号令", ExpeditionPreparationCategory.Combat, 250000),
            new ExpeditionPreparationDefinition("P03", "破阵部署", ExpeditionPreparationCategory.Combat, 250000),
            new ExpeditionPreparationDefinition("P04", "灯火共振", ExpeditionPreparationCategory.Combat, 250000),
            new ExpeditionPreparationDefinition("P05", "悬赏令", ExpeditionPreparationCategory.Economy, 250000),
            new ExpeditionPreparationDefinition("P06", "战场回收", ExpeditionPreparationCategory.Economy, 250000),
            new ExpeditionPreparationDefinition("P07", "商路议价", ExpeditionPreparationCategory.Economy, 250000),
            new ExpeditionPreparationDefinition("P08", "整编回收", ExpeditionPreparationCategory.Economy, 250000),
            new ExpeditionPreparationDefinition("P09", "战术推演", ExpeditionPreparationCategory.Build, 150000),
            new ExpeditionPreparationDefinition("P10", "流派聚焦", ExpeditionPreparationCategory.Build, 350000),
            new ExpeditionPreparationDefinition("P11", "强化增幅", ExpeditionPreparationCategory.Build, 150000),
            new ExpeditionPreparationDefinition("P12", "招募预案", ExpeditionPreparationCategory.Build, 350000)
        };

        public static IReadOnlyList<ExpeditionPreparationDefinition> Definitions { get { return definitions; } }

        public static ExpeditionPreparationDefinition Get(string id)
        {
            for (var index = 0; index < definitions.Length; index++)
                if (string.Equals(definitions[index].Id, id, StringComparison.Ordinal))
                    return definitions[index];
            return null;
        }

        public static string Generate(string runId, string nodeId,
            ExpeditionPreparationEligibility eligibility,
            Func<string, bool> canAcquire)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run id is required.", nameof(runId));
            if (string.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("A node id is required.", nameof(nodeId));
            if (eligibility == null) throw new ArgumentNullException(nameof(eligibility));

            var categories = new List<WeightedCategory>();
            AddCategory(categories, ExpeditionPreparationCategory.Combat,
                string.Equals(nodeId, "M28", StringComparison.Ordinal) ? 1000000 : 333300,
                nodeId, eligibility, canAcquire);
            if (!string.Equals(nodeId, "M28", StringComparison.Ordinal))
                AddCategory(categories, ExpeditionPreparationCategory.Economy, 333300,
                    nodeId, eligibility, canAcquire);
            if (!string.Equals(nodeId, "M18", StringComparison.Ordinal) &&
                !string.Equals(nodeId, "M28", StringComparison.Ordinal))
                AddCategory(categories, ExpeditionPreparationCategory.Build, 333400,
                    nodeId, eligibility, canAcquire);
            if (categories.Count == 0) return string.Empty;

            var seed = RoguelikeEnhancementRules.StableHash(
                runId + "|" + nodeId + "|" + RulesVersion);
            var category = PickCategory(categories, seed ^ 0x9E3779B9u);
            return PickItem(category.Items, seed ^ 0x85EBCA6Bu);
        }

        public static bool IsEligible(string id, string nodeId,
            ExpeditionPreparationEligibility context)
        {
            if (context == null || Get(id) == null) return false;
            if (string.Equals(nodeId, "M18", StringComparison.Ordinal) &&
                (id == "P09" || id == "P10" || id == "P11" || id == "P12")) return false;
            if (string.Equals(nodeId, "M28", StringComparison.Ordinal) &&
                !(id == "P01" || id == "P02" || id == "P03" || id == "P04")) return false;
            switch (id)
            {
                case "P04": return context.HasValidLantern;
                case "P06": return context.HasBackpackItem && context.HasLaterCombat;
                case "P07": return context.HasFutureMerchant;
                case "P08": return context.HasEligibleCharacterSale;
                case "P09": case "P10": case "P11": return context.HasEligibleNormalEnhancement;
                case "P12": return context.HasNaturalRecruitRefresh;
                default: return context.HasLaterCombat;
            }
        }

        private static void AddCategory(ICollection<WeightedCategory> destination,
            ExpeditionPreparationCategory category, int weight, string nodeId,
            ExpeditionPreparationEligibility eligibility, Func<string, bool> canAcquire)
        {
            var items = new List<ExpeditionPreparationDefinition>();
            for (var index = 0; index < definitions.Length; index++)
            {
                var definition = definitions[index];
                if (definition.Category != category || !IsEligible(definition.Id, nodeId, eligibility) ||
                    canAcquire != null && !canAcquire(definition.Id)) continue;
                items.Add(definition);
            }
            if (items.Count == 0 && category != ExpeditionPreparationCategory.Build)
            {
                for (var index = 0; index < definitions.Length; index++)
                {
                    var definition = definitions[index];
                    if (definition.Category == category &&
                        IsEligible(definition.Id, nodeId, eligibility)) items.Add(definition);
                }
            }
            if (items.Count > 0) destination.Add(new WeightedCategory(category, weight, items));
        }

        private static WeightedCategory PickCategory(
            IList<WeightedCategory> values, uint seed)
        {
            var total = 0;
            for (var index = 0; index < values.Count; index++) total += values[index].Weight;
            var roll = (int)(seed % (uint)total);
            var cumulative = 0;
            for (var index = 0; index < values.Count; index++)
            {
                cumulative += values[index].Weight;
                if (roll < cumulative) return values[index];
            }
            return values[values.Count - 1];
        }

        private static string PickItem(IList<ExpeditionPreparationDefinition> candidates, uint seed)
        {
            var total = 0;
            for (var index = 0; index < candidates.Count; index++) total += candidates[index].WeightPpm;
            return PickItem(candidates, seed, total);
        }

        private static string PickItem(IList<ExpeditionPreparationDefinition> candidates,
            uint seed, int total)
        {
            var roll = (int)(seed % (uint)total);
            var cumulative = 0;
            for (var index = 0; index < candidates.Count; index++)
            {
                cumulative += candidates[index].WeightPpm;
                if (roll < cumulative) return candidates[index].Id;
            }
            return candidates[candidates.Count - 1].Id;
        }

        private sealed class WeightedCategory
        {
            public WeightedCategory(ExpeditionPreparationCategory category, int weight,
                List<ExpeditionPreparationDefinition> items)
            {
                Category = category;
                Weight = weight;
                Items = items;
            }
            public ExpeditionPreparationCategory Category { get; private set; }
            public int Weight { get; private set; }
            public List<ExpeditionPreparationDefinition> Items { get; private set; }
        }
    }
}
