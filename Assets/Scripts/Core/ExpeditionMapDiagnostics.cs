using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SeekerAutoChess.Core
{
    public sealed class ExpeditionMapTestSeed
    {
        public ExpeditionMapTestSeed(uint seed, string scenario)
        {
            Seed = seed;
            Scenario = scenario ?? string.Empty;
        }

        public uint Seed { get; private set; }
        public string Scenario { get; private set; }
    }

    public sealed class ExpeditionRouteStatistics
    {
        public ExpeditionRouteStatistics(int minimumSelections, int maximumSelections)
        {
            MinimumSelections = minimumSelections;
            MaximumSelections = maximumSelections;
        }

        public int MinimumSelections { get; private set; }
        public int MaximumSelections { get; private set; }
    }

    /// <summary>
    /// Development-only map diagnostics. It intentionally exposes topology metadata,
    /// not hidden node rewards, so the full seeded map stays locked without leaking
    /// unrevealed content into the normal player UI.
    /// </summary>
    public static class ExpeditionMapDiagnostics
    {
        public const int MapVisibilityRulesVersion = 2;
        public const int VisibleRouteDepth = 3;

        private static readonly ExpeditionMapTestSeed[] TestSeeds =
        {
            new ExpeditionMapTestSeed(1u, "skeleton-A / B2 boss / all node types"),
            new ExpeditionMapTestSeed(2u, "skeleton-B / B1 boss"),
            new ExpeditionMapTestSeed(3u, "skeleton-C / long route / B2 boss"),
            new ExpeditionMapTestSeed(4u, "skeleton-D / branch comparison / B2 boss"),
            new ExpeditionMapTestSeed(5u, "skeleton-A / M28 three-star sample"),
            new ExpeditionMapTestSeed(6u, "skeleton-B / B1 alternate content"),
            new ExpeditionMapTestSeed(42u, "skeleton-B / B2 golden regression")
        };

        public static IReadOnlyList<ExpeditionMapTestSeed> FixedTestSeeds
        {
            get { return TestSeeds; }
        }

        public static ISet<string> GetVisibleNodeIds(
            ExpeditionMapGraph map, string currentNodeId)
        {
            var visible = new HashSet<string>(StringComparer.Ordinal);
            if (map == null) return visible;
            var current = map.GetNode(currentNodeId) ?? map.GetNode("M00");
            if (current == null) return visible;
            var maximumVisibleLayer = current.Layer + VisibleRouteDepth;

            // Previously exposed branches remain visible but de-emphasized. Only
            // future nodes reachable from the current node within three actual route
            // choices are newly exposed.
            foreach (var node in map.Nodes)
            {
                if (node.Layer <= current.Layer) visible.Add(node.Id);
            }

            var nodes = new Queue<ExpeditionMapNode>();
            var depths = new Queue<int>();
            nodes.Enqueue(current);
            depths.Enqueue(0);
            while (nodes.Count > 0)
            {
                var node = nodes.Dequeue();
                var depth = depths.Dequeue();
                visible.Add(node.Id);
                if (depth >= VisibleRouteDepth) continue;
                foreach (var nextId in node.NextNodeIds)
                {
                    var next = map.GetNode(nextId);
                    if (next == null || next.Layer > maximumVisibleLayer) continue;
                    visible.Add(next.Id);
                    nodes.Enqueue(next);
                    depths.Enqueue(depth + 1);
                }
            }
            return visible;
        }

        public static ExpeditionRouteStatistics AnalyzeRoutes(ExpeditionMapGraph map)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            var start = map.GetNode("M00");
            if (start == null) return new ExpeditionRouteStatistics(0, 0);
            return new ExpeditionRouteStatistics(
                PathLength(map, start, true, new Dictionary<string, int>(StringComparer.Ordinal)),
                PathLength(map, start, false, new Dictionary<string, int>(StringComparer.Ordinal)));
        }

        public static string CreateSummary(
            ExpeditionMapGraph map,
            string currentNodeId,
            uint encounterSeed)
        {
            if (map == null) return "map=unavailable";
            var current = map.GetNode(currentNodeId) ?? map.GetNode("M00");
            var progress = current == null ? 0f : current.JourneyProgress;
            var routes = AnalyzeRoutes(map);
            var builder = new StringBuilder(320);
            builder.Append("mapSeed=").Append(map.Seed).Append('\n');
            builder.Append("mapGenerationVersion=").Append(map.GenerationVersion).Append('\n');
            builder.Append("routeSkeleton=").Append(map.SkeletonId).Append('\n');
            builder.Append("currentNodeId=").Append(current == null ? "none" : current.Id).Append('\n');
            builder.Append("journeyProgress=").Append(progress.ToString("0.000", CultureInfo.InvariantCulture)).Append('\n');
            builder.Append("mapVisibilityRulesVersion=").Append(MapVisibilityRulesVersion).Append('\n');
            builder.Append("visibleRouteDepth=").Append(VisibleRouteDepth).Append('\n');
            builder.Append("routeSelectionRange=").Append(routes.MinimumSelections).Append('-')
                .Append(routes.MaximumSelections).Append('\n');
            builder.Append("encounterSeed=").Append(encounterSeed).Append('\n');
            builder.Append("battleSeed=").Append(encounterSeed).Append('\n');
            builder.Append("encounterGenerationVersion=")
                .Append(ChapterOneEncounterRules.EncounterGenerationVersion).Append('\n');
            builder.Append("rewardVersion=").Append(ChapterOneEncounterRules.EncounterRewardVersion);
            return builder.ToString();
        }

        public static string CreateEncounterEconomySummary(
            string clientVersion,
            ExpeditionMapGraph map,
            string inspectedNodeId,
            ChapterOneEncounter encounter,
            int currentCoins,
            ExpeditionEconomyState economy,
            IEnumerable<string> completedNodeIds)
        {
            if (map == null) return "map=unavailable";
            var node = map.GetNode(inspectedNodeId) ?? map.GetNode("M00");
            var summary = new StringBuilder(768);
            summary.Append("clientVersion=").Append(clientVersion ?? "unknown").Append('\n');
            summary.Append(CreateSummary(
                map,
                node == null ? "M00" : node.Id,
                encounter == null ? 0u : encounter.BattleSeed)).Append('\n');
            summary.Append("nodeType=").Append(node == null ? "none" : node.Type.ToString()).Append('\n');
            summary.Append("encounterRewardVersion=")
                .Append(ChapterOneEncounterRules.EncounterRewardVersion).Append('\n');
            summary.Append("enemyUnits=");
            if (encounter == null || encounter.Units.Count == 0)
            {
                summary.Append("none");
            }
            else
            {
                for (var index = 0; index < encounter.Units.Count; index++)
                {
                    if (index > 0) summary.Append(',');
                    summary.Append(encounter.Units[index].DefinitionId).Append('+')
                        .Append(encounter.Units[index].RunLevel);
                }
            }
            summary.Append('\n');
            summary.Append("pendingCoinReward=").Append(encounter == null ? 0 : encounter.CoinReward).Append('\n');
            summary.Append("currentCoins=").Append(Math.Max(0, currentCoins)).Append('\n');
            summary.Append("morale=").Append(economy == null ? 0 : economy.Morale).Append('\n');
            summary.Append("backpack=");
            AppendValues(summary, economy == null ? null : economy.Backpack);
            summary.Append('\n');
            summary.Append("rerollState=");
            if (economy == null)
            {
                summary.Append("unavailable");
            }
            else
            {
                summary.Append("firstUsed:").Append(economy.FirstManualRecruitRerollUsed ? 1 : 0)
                    .Append(",vouchers:").Append(economy.RecruitRerollVouchers)
                    .Append(",unitDiscount:").Append(economy.RecruitmentDiscount)
                    .Append(",chestDiscount:").Append(economy.ChestRecruitmentDiscount);
            }
            summary.Append('\n');
            summary.Append("resolvedActions=");
            AppendSummary(summary, economy == null ? null : economy.CopyResolvedActionIds());
            summary.Append('\n');
            summary.Append("completedNodes=");
            AppendSummary(summary, completedNodeIds);
            return summary.ToString();
        }

        public static string DeterministicSignature(ExpeditionMapGraph map)
        {
            if (map == null) return string.Empty;
            var builder = new StringBuilder(2048);
            builder.Append(map.Seed).Append('|').Append(map.SkeletonId);
            foreach (var node in map.Nodes)
            {
                builder.Append('|').Append(node.Id).Append(':').Append((int)node.Type).Append('>');
                for (var index = 0; index < node.NextNodeIds.Count; index++)
                {
                    if (index > 0) builder.Append(',');
                    builder.Append(node.NextNodeIds[index]);
                }
            }
            return builder.ToString();
        }

        private static int PathLength(
            ExpeditionMapGraph map,
            ExpeditionMapNode node,
            bool minimum,
            IDictionary<string, int> memo)
        {
            int cached;
            if (memo.TryGetValue(node.Id, out cached)) return cached;
            if (node.Id == "M29") return 1;
            var selected = minimum ? Int32.MaxValue : Int32.MinValue;
            foreach (var nextId in node.NextNodeIds)
            {
                var next = map.GetNode(nextId);
                if (next == null) continue;
                var length = PathLength(map, next, minimum, memo);
                selected = minimum ? Math.Min(selected, length) : Math.Max(selected, length);
            }
            if (selected == Int32.MaxValue || selected == Int32.MinValue) selected = 0;
            var result = (node.Id == "M00" ? 0 : 1) + selected;
            memo[node.Id] = result;
            return result;
        }

        private static void AppendValues(StringBuilder builder, IEnumerable<string> values)
        {
            if (values == null)
            {
                builder.Append("none");
                return;
            }
            var count = 0;
            foreach (var value in values)
            {
                if (count > 0) builder.Append(',');
                builder.Append(value);
                count++;
            }
            if (count == 0) builder.Append("none");
        }

        private static void AppendSummary(StringBuilder builder, IEnumerable<string> values)
        {
            if (values == null)
            {
                builder.Append("count:0");
                return;
            }
            var list = new List<string>();
            foreach (var value in values)
                if (!string.IsNullOrEmpty(value)) list.Add(value);
            list.Sort(StringComparer.Ordinal);
            builder.Append("count:").Append(list.Count).Append(",tail:");
            var start = Math.Max(0, list.Count - 4);
            for (var index = start; index < list.Count; index++)
            {
                if (index > start) builder.Append(',');
                builder.Append(list[index]);
            }
            if (list.Count == 0) builder.Append("none");
        }
    }
}
