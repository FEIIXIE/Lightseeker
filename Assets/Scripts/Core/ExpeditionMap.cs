using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ExpeditionNodeType
    {
        NormalBattle = 0,
        EliteBattle = 1,
        Supply = 2,
        Boss = 3,
        Event = 4,
        Shop = 5,
        Rest = 6,
        Treasure = 7,
        HighRiskBattle = 8
    }

    [Serializable]
    public sealed class ExpeditionMapNode
    {
        private readonly List<string> nextNodeIds = new List<string>();

        public string Id { get; private set; }
        public int Layer { get; private set; }
        public int Column { get; private set; }
        public int AnchorIndex { get; private set; }
        public float JourneyProgress { get; private set; }
        public ExpeditionNodeType Type { get; private set; }
        public IReadOnlyList<string> NextNodeIds { get { return nextNodeIds; } }

        internal ExpeditionMapNode(
            string id, int layer, int column, int anchorIndex,
            float journeyProgress, ExpeditionNodeType type)
        {
            Id = id;
            Layer = layer;
            Column = column;
            AnchorIndex = anchorIndex;
            JourneyProgress = journeyProgress;
            Type = type;
        }

        internal void AddNext(string nodeId)
        {
            if (!nextNodeIds.Contains(nodeId)) nextNodeIds.Add(nodeId);
        }
    }

    /// <summary>
    /// Deterministic first-chapter route graph. The caller supplies the seed; this core
    /// type has no Unity, clock, persistence or network dependency.
    /// </summary>
    public sealed class ExpeditionMapGraph
    {
        public const int ChapterLayerCount = 15;
        public const int LegacyFiveTypeMapGenerationVersion = 8;
        public const int MapGenerationVersion = 9;
        public const int FixedAnchorCount = 30;
        public const int MaximumNodesPerLayer = 4;
        public const int MaximumOrdinaryOutDegree = 3;

        private readonly List<ExpeditionMapNode> nodes;
        private readonly Dictionary<string, ExpeditionMapNode> nodesById;

        private ExpeditionMapGraph(
            uint seed, int generationVersion, string skeletonId, int contentAttempts,
            List<ExpeditionMapNode> generatedNodes)
        {
            Seed = seed;
            GenerationVersion = generationVersion;
            SkeletonId = skeletonId;
            ContentAttempts = contentAttempts;
            nodes = generatedNodes;
            nodesById = new Dictionary<string, ExpeditionMapNode>(StringComparer.Ordinal);
            foreach (var node in nodes) nodesById.Add(node.Id, node);
        }

        public uint Seed { get; private set; }
        public int GenerationVersion { get; private set; }
        public string SkeletonId { get; private set; }
        public int ContentAttempts { get; private set; }
        public IReadOnlyList<ExpeditionMapNode> Nodes { get { return nodes; } }

        public static ExpeditionMapGraph CreateChapterOne(uint seed)
        {
            return CreateChapterOne(seed, MapGenerationVersion);
        }

        public static ExpeditionMapGraph CreateChapterOne(uint seed, int generationVersion)
        {
            if (seed == 0) seed = 0x51EA2C31u;
            if (generationVersion != LegacyFiveTypeMapGenerationVersion &&
                generationVersion != MapGenerationVersion)
                throw new ArgumentOutOfRangeException(nameof(generationVersion),
                    "The chapter-one map generation version is unsupported.");
            var rules = ChapterOneMapRules.CreateDefault();
            var generated = new List<ExpeditionMapNode>();
            int contentAttempts;
            var assignedTypes = CreateNodeTypes(
                rules, seed, generationVersion, out contentAttempts);
            for (var layer = 1; layer <= rules.StageCount; layer++)
            {
                var column = 0;
                foreach (var rule in rules.Nodes)
                {
                    if (rule.Stage != layer) continue;
                    generated.Add(new ExpeditionMapNode(
                        rule.Id, layer, column++, rule.AnchorIndex,
                        rule.JourneyProgress, assignedTypes[rule.Id]));
                }
            }

            var byId = new Dictionary<string, ExpeditionMapNode>(StringComparer.Ordinal);
            foreach (var node in generated) byId.Add(node.Id, node);
            foreach (var encodedEdge in rules.SkeletonEdges(seed))
            {
                var separator = encodedEdge.IndexOf('>');
                if (separator <= 0 || separator >= encodedEdge.Length - 1) continue;
                ExpeditionMapNode source;
                ExpeditionMapNode target;
                if (!byId.TryGetValue(encodedEdge.Substring(0, separator), out source) ||
                    !byId.TryGetValue(encodedEdge.Substring(separator + 1), out target)) continue;
                source.AddNext(target.Id);
            }

            return new ExpeditionMapGraph(
                seed, generationVersion, rules.SkeletonId(seed), contentAttempts, generated);
        }

        public ExpeditionMapNode GetNode(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            ExpeditionMapNode node;
            return nodesById.TryGetValue(id, out node) ? node : null;
        }

        public IReadOnlyList<ExpeditionMapNode> GetLayer(int layer)
        {
            return GetLayer(nodes, layer);
        }

        public IReadOnlyList<ExpeditionMapNode> GetAvailableNext(string currentNodeId)
        {
            var result = new List<ExpeditionMapNode>();
            var current = GetNode(currentNodeId);
            if (current == null) return result;
            foreach (var id in current.NextNodeIds)
            {
                var node = GetNode(id);
                if (node != null) result.Add(node);
            }
            return result;
        }

        public bool IsValidChapterOne()
        {
            return ValidateChapterOne().Count == 0;
        }

        /// <summary>
        /// Returns all generator-contract violations instead of only a boolean. This is
        /// shared by edit-mode, smoke and future server-side snapshot validation.
        /// </summary>
        public IReadOnlyList<string> ValidateChapterOne()
        {
            var errors = new List<string>();
            var start = GetNode("M00");
            var guardian = GetNode("M19");
            var finalPreparation = GetNode("M28");
            var boss = GetNode("M29");

            if (nodes.Count != FixedAnchorCount)
                errors.Add("Chapter one must contain exactly 30 fixed map anchors.");
            if (start == null || start.Layer != 1)
                errors.Add("Stage 1 must contain fixed start node M00.");
            if (boss == null || boss.Layer != ChapterLayerCount || boss.Type != ExpeditionNodeType.Boss)
                errors.Add("Stage 15 must contain fixed Boss node M29.");
            if (guardian == null || guardian.Type != ExpeditionNodeType.EliteBattle)
                errors.Add("M19 must remain the mandatory guardian encounter.");
            if (GenerationVersion == LegacyFiveTypeMapGenerationVersion)
            {
                if (finalPreparation == null ||
                    finalPreparation.Type != ExpeditionNodeType.NormalBattle)
                    errors.Add("Legacy M28 must remain the fixed final normal battle.");
            }
            else if (finalPreparation == null ||
                     finalPreparation.Type != ExpeditionNodeType.Supply)
                errors.Add("M28 must be the fixed Boss preparation supply.");
            if (SkeletonId != "A" && SkeletonId != "B" && SkeletonId != "C" && SkeletonId != "D")
                errors.Add("Chapter one must use one reviewed A-D route skeleton.");

            var incoming = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var node in nodes) incoming[node.Id] = new List<string>();
            foreach (var node in nodes)
            {
                if (node.Id == "M29")
                {
                    if (node.NextNodeIds.Count != 0)
                        errors.Add("Boss node must not have outgoing routes.");
                }
                else if (node.NextNodeIds.Count < 1 || node.NextNodeIds.Count > MaximumOrdinaryOutDegree)
                    errors.Add("Node " + node.Id + " has an invalid out-degree.");

                foreach (var nextId in node.NextNodeIds)
                {
                    var next = GetNode(nextId);
                    if (next == null)
                    {
                        errors.Add("Node " + node.Id + " points to an unknown anchor.");
                        continue;
                    }
                    if (next.JourneyProgress <= node.JourneyProgress)
                        errors.Add("Route " + node.Id + " -> " + next.Id + " must move strictly right.");
                    incoming[next.Id].Add(node.Id);
                }
            }

            var reachable = new HashSet<string>(StringComparer.Ordinal);
            CollectReachable(start, reachable);
            foreach (var node in nodes)
                if (!reachable.Contains(node.Id))
                    errors.Add("Node " + node.Id + " is unreachable from the start.");

            foreach (var node in nodes)
                if (node.Id != "M00" && incoming[node.Id].Count == 0)
                    errors.Add("Node " + node.Id + " has no incoming route.");
            if (incoming["M19"].Count != 1 || incoming["M19"][0] != "M18")
                errors.Add("M19 must have M18 as its only logical predecessor.");
            if (GetNode("M18").NextNodeIds.Count != 1 ||
                GetNode("M18").NextNodeIds[0] != "M19")
                errors.Add("M18 must route directly and exclusively to M19.");
            if (incoming["M29"].Count != 1 || incoming["M29"][0] != "M28")
                errors.Add("M29 must have M28 as its only logical predecessor.");
            if (GetNode("M28").NextNodeIds.Count != 1 ||
                GetNode("M28").NextNodeIds[0] != "M29")
                errors.Add("M28 must route directly and exclusively to M29.");

            var canReachBoss = new HashSet<string>(StringComparer.Ordinal);
            if (boss != null) canReachBoss.Add(boss.Id);
            for (var index = nodes.Count - 1; index >= 0; index--)
            {
                var node = nodes[index];
                foreach (var nextId in node.NextNodeIds)
                {
                    if (!canReachBoss.Contains(nextId)) continue;
                    canReachBoss.Add(node.Id);
                    break;
                }
            }
            foreach (var node in nodes)
            {
                if (!canReachBoss.Contains(node.Id))
                    errors.Add("Node " + node.Id + " cannot reach the Boss.");
            }

            ValidateTypeCount(ExpeditionNodeType.NormalBattle, 15, errors);
            ValidateTypeCount(ExpeditionNodeType.EliteBattle, 4, errors);
            ValidateTypeCount(ExpeditionNodeType.Supply,
                GenerationVersion == LegacyFiveTypeMapGenerationVersion ? 0 : 4, errors);
            ValidateTypeCount(ExpeditionNodeType.Event,
                GenerationVersion == LegacyFiveTypeMapGenerationVersion ? 2 : 4, errors);
            ValidateTypeCount(ExpeditionNodeType.Shop, 2, errors);
            ValidateTypeCount(ExpeditionNodeType.Rest,
                GenerationVersion == LegacyFiveTypeMapGenerationVersion ? 2 : 0, errors);
            ValidateTypeCount(ExpeditionNodeType.Treasure,
                GenerationVersion == LegacyFiveTypeMapGenerationVersion ? 4 : 0, errors);
            ValidateTypeCount(ExpeditionNodeType.Boss, 1, errors);
            if (!IsBattleType(GetNode("M01").Type) && !IsBattleType(GetNode("M02").Type))
                errors.Add("M01 and M02 cannot both be non-combat.");
            for (var index = 1; index <= 7; index++)
                if (GetNode("M" + index.ToString("00")).Type == ExpeditionNodeType.EliteBattle)
                    errors.Add("M01-M07 cannot contain an optional elite encounter.");
            if (!ContainsTypeInRange(ExpeditionNodeType.EliteBattle, 8, 18) ||
                !ContainsTypeInRange(ExpeditionNodeType.EliteBattle, 20, 27))
                errors.Add("Optional elites must appear on both sides of M19.");
            if (!ContainsTypeInRange(ExpeditionNodeType.Shop, 8, 18) ||
                !ContainsTypeInRange(ExpeditionNodeType.Shop, 20, 27))
                errors.Add("Shops must appear on both sides of M19.");
            if (GenerationVersion == LegacyFiveTypeMapGenerationVersion &&
                !IsImprovementType(GetNode("M26").Type) &&
                !IsImprovementType(GetNode("M27").Type))
                errors.Add("M26 or M27 must provide an effective improvement.");
            if (GenerationVersion == MapGenerationVersion)
            {
                if (GetNode("M18").Type != ExpeditionNodeType.Supply)
                    errors.Add("M18 must be the fixed guardian preparation supply.");
                var forbiddenSupplyAnchors = new[] { 15, 16, 17, 26, 27 };
                foreach (var anchor in forbiddenSupplyAnchors)
                    if (GetNode("M" + anchor.ToString("00")).Type == ExpeditionNodeType.Supply)
                        errors.Add("M" + anchor.ToString("00") +
                                   " cannot generate a dynamic supply.");
                foreach (var node in nodes)
                    foreach (var nextId in node.NextNodeIds)
                        if (node.Type == ExpeditionNodeType.Event &&
                            GetNode(nextId).Type == ExpeditionNodeType.Event)
                            errors.Add("Event nodes cannot connect directly to Event nodes.");
            }
            ValidateCompletePaths(errors);

            return errors;
        }

        private void CollectReachable(ExpeditionMapNode node, ISet<string> reachable)
        {
            if (node == null || !reachable.Add(node.Id)) return;
            foreach (var nextId in node.NextNodeIds) CollectReachable(GetNode(nextId), reachable);
        }

        private bool ContainsTypeInRange(ExpeditionNodeType type, int minimum, int maximum)
        {
            for (var index = minimum; index <= maximum; index++)
            {
                var node = GetNode("M" + index.ToString("00"));
                if (node != null && node.Type == type) return true;
            }
            return false;
        }

        private void ValidateCompletePaths(ICollection<string> errors)
        {
            var start = GetNode("M00");
            if (start == null) return;
            var minimumLength = MinimumPathMetric(start, false, false,
                new Dictionary<string, int>(StringComparer.Ordinal));
            var maximumLength = MaximumPathLength(start,
                new Dictionary<string, int>(StringComparer.Ordinal));
            var minimumBattles = MinimumPathMetric(start, true, false,
                new Dictionary<string, int>(StringComparer.Ordinal));
            var minimumDecisions = MinimumPathMetric(start, false, true,
                new Dictionary<string, int>(StringComparer.Ordinal));
            if (minimumLength < 12 || maximumLength > 18)
                errors.Add("Complete route length must stay within 12-18 nodes.");
            if (minimumBattles < 8)
                errors.Add("Every complete route must contain at least eight battles.");
            if (HasThreeConsecutiveNonBattles(start, 0, new HashSet<string>(StringComparer.Ordinal)))
                errors.Add("A complete route contains more than two consecutive non-battle nodes.");
            if (CanReachBossWithoutGuardian(start, new HashSet<string>(StringComparer.Ordinal)))
                errors.Add("Every complete route must pass through M19.");
            if (minimumDecisions < 4)
                errors.Add("Every complete route must expose at least four route decisions.");
            if (MinimumBattlesBetween("M00", "M19") < 4)
                errors.Add("Every route must contain at least four battles before M19.");
            if (MinimumBattlesBetween("M19", "M29") < 2)
                errors.Add("Every route must contain at least two battles after M19 and before M29.");
        }

        private int MinimumBattlesBetween(string sourceId, string targetId)
        {
            return MinimumBattlesBetween(
                GetNode(sourceId), sourceId, targetId,
                new Dictionary<string, int>(StringComparer.Ordinal));
        }

        private int MinimumBattlesBetween(
            ExpeditionMapNode node, string sourceId, string targetId,
            IDictionary<string, int> memo)
        {
            if (node == null) return Int32.MaxValue / 4;
            if (node.Id == targetId) return 0;
            int cached;
            if (memo.TryGetValue(node.Id, out cached)) return cached;
            var own = node.Id != sourceId && IsBattleType(node.Type) ? 1 : 0;
            var minimum = Int32.MaxValue / 4;
            foreach (var nextId in node.NextNodeIds)
            {
                var candidate = MinimumBattlesBetween(GetNode(nextId), sourceId, targetId, memo);
                if (candidate < minimum) minimum = candidate;
            }
            var result = minimum >= Int32.MaxValue / 4 ? minimum : own + minimum;
            memo[node.Id] = result;
            return result;
        }

        private int MinimumPathMetric(
            ExpeditionMapNode node, bool countBattles, bool countDecisions,
            IDictionary<string, int> memo)
        {
            int cached;
            if (memo.TryGetValue(node.Id, out cached)) return cached;
            var own = 0;
            if (node.Id != "M00" && (!countBattles || IsBattleType(node.Type))) own = 1;
            if (countDecisions) own = node.NextNodeIds.Count > 1 ? 1 : 0;
            if (node.Id == "M29") return own;
            var minimum = Int32.MaxValue;
            foreach (var nextId in node.NextNodeIds)
            {
                var next = GetNode(nextId);
                if (next == null) continue;
                minimum = Math.Min(minimum, MinimumPathMetric(next, countBattles, countDecisions, memo));
            }
            var result = minimum == Int32.MaxValue ? -1000 : own + minimum;
            memo[node.Id] = result;
            return result;
        }

        private int MaximumPathLength(ExpeditionMapNode node, IDictionary<string, int> memo)
        {
            int cached;
            if (memo.TryGetValue(node.Id, out cached)) return cached;
            var own = node.Id == "M00" ? 0 : 1;
            if (node.Id == "M29") return own;
            var maximum = Int32.MinValue;
            foreach (var nextId in node.NextNodeIds)
            {
                var next = GetNode(nextId);
                if (next != null) maximum = Math.Max(maximum, MaximumPathLength(next, memo));
            }
            var result = maximum == Int32.MinValue ? 1000 : own + maximum;
            memo[node.Id] = result;
            return result;
        }

        private bool HasThreeConsecutiveNonBattles(
            ExpeditionMapNode node, int consecutive, ISet<string> visitedStates)
        {
            if (node.Id != "M00") consecutive = IsBattleType(node.Type) ? 0 : consecutive + 1;
            if (consecutive > 2) return true;
            var state = node.Id + ":" + consecutive;
            if (!visitedStates.Add(state)) return false;
            foreach (var nextId in node.NextNodeIds)
            {
                var next = GetNode(nextId);
                if (next != null && HasThreeConsecutiveNonBattles(next, consecutive, visitedStates)) return true;
            }
            return false;
        }

        private bool CanReachBossWithoutGuardian(ExpeditionMapNode node, ISet<string> visited)
        {
            if (node.Id == "M19") return false;
            if (node.Id == "M29") return true;
            if (!visited.Add(node.Id)) return false;
            foreach (var nextId in node.NextNodeIds)
            {
                var next = GetNode(nextId);
                if (next != null && CanReachBossWithoutGuardian(next, visited)) return true;
            }
            return false;
        }

        private static bool IsBattleType(ExpeditionNodeType type)
        {
            return type == ExpeditionNodeType.NormalBattle ||
                   type == ExpeditionNodeType.EliteBattle ||
                   type == ExpeditionNodeType.Boss;
        }

        private static Dictionary<string, ExpeditionNodeType> CreateNodeTypes(
            ChapterOneMapRules rules, uint seed, int generationVersion,
            out int contentAttempts)
        {
            for (var attempt = 0; attempt < rules.MaximumContentAttempts; attempt++)
            {
                var result = generationVersion == LegacyFiveTypeMapGenerationVersion
                    ? MakeLegacyContentAssignment(rules, seed, attempt)
                    : MakeThreeTypeContentAssignment(rules, seed, attempt);
                if (!ValidateContentAssignment(
                        rules, result, seed, generationVersion)) continue;
                contentAttempts = attempt + 1;
                return result;
            }
            contentAttempts = rules.MaximumContentAttempts;
            throw new InvalidOperationException(
                "Chapter-one content assignment failed after " + rules.MaximumContentAttempts +
                " deterministic attempts for seed " + seed + ".");
        }

        private static Dictionary<string, ExpeditionNodeType> MakeLegacyContentAssignment(
            ChapterOneMapRules rules, uint seed, int attempt)
        {
            var salt = unchecked((uint)(attempt + 1) * 0x85ebca6bu);
            var random = new E21Random(seed, salt);
            var result = new Dictionary<string, ExpeditionNodeType>(StringComparer.Ordinal)
            {
                { "M00", ExpeditionNodeType.NormalBattle },
                { "M19", ExpeditionNodeType.EliteBattle },
                { "M28", ExpeditionNodeType.NormalBattle },
                { "M29", ExpeditionNodeType.Boss }
            };
            var available = new List<string>();
            for (var index = 1; index <= 27; index++)
                if (index != 19) available.Add("M" + index.ToString("00"));

            AssignPicked(result, available, ExpeditionNodeType.EliteBattle,
                CandidateIds(available, 8, 18), ref random);
            AssignPicked(result, available, ExpeditionNodeType.EliteBattle,
                CandidateIds(available, 20, 27), ref random);
            var thirdEliteCandidates = CandidateIds(available, 8, 18);
            thirdEliteCandidates.AddRange(CandidateIds(available, 20, 27));
            AssignPicked(result, available, ExpeditionNodeType.EliteBattle,
                thirdEliteCandidates, ref random);
            AssignPicked(result, available, ExpeditionNodeType.Shop,
                CandidateIds(available, 8, 18), ref random);
            AssignPicked(result, available, ExpeditionNodeType.Shop,
                CandidateIds(available, 20, 27), ref random);

            var bag = new List<ExpeditionNodeType>();
            AddCopies(bag, ExpeditionNodeType.NormalBattle, 13);
            AddCopies(bag, ExpeditionNodeType.Event, 2);
            AddCopies(bag, ExpeditionNodeType.Rest, 2);
            AddCopies(bag, ExpeditionNodeType.Treasure, 4);
            Shuffle(bag, ref random);
            Shuffle(available, ref random);
            for (var index = 0; index < available.Count; index++)
                result[available[index]] = bag[index];
            return result;
        }

        private static Dictionary<string, ExpeditionNodeType> MakeThreeTypeContentAssignment(
            ChapterOneMapRules rules, uint seed, int attempt)
        {
            var salt = unchecked((uint)(attempt + 1) * 0x85ebca6bu);
            var random = new E21Random(seed, salt);
            var result = new Dictionary<string, ExpeditionNodeType>(StringComparer.Ordinal)
            {
                { "M00", ExpeditionNodeType.NormalBattle },
                { "M18", ExpeditionNodeType.Supply },
                { "M19", ExpeditionNodeType.EliteBattle },
                { "M28", ExpeditionNodeType.Supply },
                { "M29", ExpeditionNodeType.Boss }
            };
            var available = new List<string>();
            for (var index = 1; index <= 27; index++)
                if (index != 18 && index != 19)
                    available.Add("M" + index.ToString("00"));

            AssignPicked(result, available, ExpeditionNodeType.EliteBattle,
                CandidateIds(available, 8, 17), ref random);
            AssignPicked(result, available, ExpeditionNodeType.EliteBattle,
                CandidateIds(available, 20, 27), ref random);
            var thirdEliteCandidates = CandidateIds(available, 8, 17);
            thirdEliteCandidates.AddRange(CandidateIds(available, 20, 27));
            AssignPicked(result, available, ExpeditionNodeType.EliteBattle,
                thirdEliteCandidates, ref random);
            AssignPicked(result, available, ExpeditionNodeType.Shop,
                CandidateIds(available, 8, 17), ref random);
            AssignPicked(result, available, ExpeditionNodeType.Shop,
                CandidateIds(available, 20, 27), ref random);

            var supplyCandidates = CandidateIds(available, 1, 27);
            supplyCandidates.Remove("M15");
            supplyCandidates.Remove("M16");
            supplyCandidates.Remove("M17");
            supplyCandidates.Remove("M26");
            supplyCandidates.Remove("M27");
            AssignPicked(result, available, ExpeditionNodeType.Supply,
                supplyCandidates, ref random);
            supplyCandidates = CandidateIds(available, 1, 27);
            supplyCandidates.Remove("M15");
            supplyCandidates.Remove("M16");
            supplyCandidates.Remove("M17");
            supplyCandidates.Remove("M26");
            supplyCandidates.Remove("M27");
            AssignPicked(result, available, ExpeditionNodeType.Supply,
                supplyCandidates, ref random);

            var bag = new List<ExpeditionNodeType>();
            AddCopies(bag, ExpeditionNodeType.NormalBattle, 14);
            AddCopies(bag, ExpeditionNodeType.Event, 4);
            Shuffle(bag, ref random);
            Shuffle(available, ref random);
            for (var index = 0; index < available.Count; index++)
                result[available[index]] = bag[index];
            return result;
        }

        private static List<string> CandidateIds(
            IEnumerable<string> available, int minimum, int maximum)
        {
            var result = new List<string>();
            foreach (var id in available)
            {
                var index = Int32.Parse(id.Substring(1));
                if (index >= minimum && index <= maximum) result.Add(id);
            }
            return result;
        }

        private static void AssignPicked(
            IDictionary<string, ExpeditionNodeType> result,
            ICollection<string> available,
            ExpeditionNodeType type,
            IReadOnlyList<string> candidates,
            ref E21Random random)
        {
            var id = candidates[random.Next(candidates.Count)];
            result[id] = type;
            available.Remove(id);
        }

        private static bool ValidateContentAssignment(
            ChapterOneMapRules rules,
            IReadOnlyDictionary<string, ExpeditionNodeType> types,
            uint seed,
            int generationVersion)
        {
            if (!IsBattleType(types["M01"]) && !IsBattleType(types["M02"])) return false;
            if (generationVersion == LegacyFiveTypeMapGenerationVersion &&
                !IsImprovementType(types["M26"]) && !IsImprovementType(types["M27"]))
                return false;

            var predecessors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var rule in rules.Nodes) predecessors[rule.Id] = new List<string>();
            foreach (var encodedEdge in rules.SkeletonEdges(seed))
            {
                var separator = encodedEdge.IndexOf('>');
                predecessors[encodedEdge.Substring(separator + 1)].Add(
                    encodedEdge.Substring(0, separator));
            }

            var minimumCombat = new Dictionary<string, int>(StringComparer.Ordinal) { { "M00", 0 } };
            var maximumNonCombatRun = new Dictionary<string, int>(StringComparer.Ordinal) { { "M00", 0 } };
            var globalMaximumNonCombatRun = 0;
            for (var index = 1; index < rules.Nodes.Count; index++)
            {
                var id = rules.Nodes[index].Id;
                var combat = IsBattleType(types[id]);
                var minimumBefore = Int32.MaxValue;
                var maximumBefore = Int32.MinValue;
                foreach (var predecessor in predecessors[id])
                {
                    minimumBefore = Math.Min(minimumBefore, minimumCombat[predecessor]);
                    maximumBefore = Math.Max(maximumBefore, maximumNonCombatRun[predecessor]);
                }
                minimumCombat[id] = minimumBefore + (combat ? 1 : 0);
                maximumNonCombatRun[id] = combat ? 0 : maximumBefore + 1;
                globalMaximumNonCombatRun = Math.Max(
                    globalMaximumNonCombatRun, maximumNonCombatRun[id]);
            }
            if (minimumCombat["M29"] < 8 || globalMaximumNonCombatRun > 2) return false;
            if (minimumCombat["M19"] - 1 < 4) return false;

            if (generationVersion == MapGenerationVersion)
            {
                foreach (var encodedEdge in rules.SkeletonEdges(seed))
                {
                    var separator = encodedEdge.IndexOf('>');
                    var sourceId = encodedEdge.Substring(0, separator);
                    var targetId = encodedEdge.Substring(separator + 1);
                    if (types[sourceId] == ExpeditionNodeType.Event &&
                        types[targetId] == ExpeditionNodeType.Event)
                        return false;
                }
            }

            var minimumPostGuardCombat = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { "M19", 0 }
            };
            for (var index = 20; index < rules.Nodes.Count; index++)
            {
                var id = rules.Nodes[index].Id;
                var minimumBefore = Int32.MaxValue;
                foreach (var predecessor in predecessors[id])
                {
                    int value;
                    if (minimumPostGuardCombat.TryGetValue(predecessor, out value))
                        minimumBefore = Math.Min(minimumBefore, value);
                }
                if (minimumBefore == Int32.MaxValue) continue;
                var countHere = id != "M29" && IsBattleType(types[id]) ? 1 : 0;
                minimumPostGuardCombat[id] = minimumBefore + countHere;
            }
            return minimumPostGuardCombat["M29"] >= 2;
        }

        private static bool IsImprovementType(ExpeditionNodeType type)
        {
            return type == ExpeditionNodeType.NormalBattle || type == ExpeditionNodeType.EliteBattle ||
                   type == ExpeditionNodeType.Supply || type == ExpeditionNodeType.Treasure ||
                   type == ExpeditionNodeType.Shop;
        }

        private static void AddCopies(
            ICollection<ExpeditionNodeType> values, ExpeditionNodeType type, int count)
        {
            for (var index = 0; index < count; index++) values.Add(type);
        }

        private void ValidateTypeCount(
            ExpeditionNodeType type, int expected, ICollection<string> errors)
        {
            var actual = 0;
            foreach (var node in nodes)
            {
                if (node.Type == type) actual++;
            }
            if (actual != expected)
                errors.Add(type + " count must be " + expected + ", found " + actual + ".");
        }

        private static List<ExpeditionMapNode> GetLayer(
            IEnumerable<ExpeditionMapNode> source, int layer)
        {
            var result = new List<ExpeditionMapNode>();
            foreach (var node in source)
            {
                if (node.Layer == layer) result.Add(node);
            }
            return result;
        }

        private static void ConnectLayers(
            IReadOnlyList<ExpeditionMapNode> current,
            IReadOnlyList<ExpeditionMapNode> next,
            ref MapRandom random)
        {
            var connected = new bool[current.Count, next.Count];
            var outgoing = new int[current.Count];

            var localMaximum = Math.Min(MaximumOrdinaryOutDegree, next.Count);

            // Give every target an incoming route. Start near its vertical neighbour,
            // then use a seeded one-lane offset where capacity permits; this keeps the
            // graph readable without making every layer repeat the same fan pattern.
            for (var toIndex = 0; toIndex < next.Count; toIndex++)
            {
                var fromIndex = FindCoverageSource(
                    toIndex, current.Count, next.Count, outgoing, localMaximum, ref random);
                AddConnection(current, next, connected, outgoing, fromIndex, toIndex);
            }

            // Every source still needs a legal route when a layer converges.
            for (var fromIndex = 0; fromIndex < current.Count; fromIndex++)
            {
                if (outgoing[fromIndex] > 0) continue;
                AddConnection(current, next, connected, outgoing, fromIndex,
                    NearestNextIndex(fromIndex, current.Count, next.Count));
            }

            // Add a deterministic amount of local branching. Each route exposes only
            // its local subset of up to three targets, even when the next layer has five.
            for (var fromIndex = 0; fromIndex < current.Count; fromIndex++)
            {
                var minimumChoices = next.Count > 1 ? Math.Min(2, localMaximum) : 1;
                var desired = minimumChoices + random.Next(localMaximum - minimumChoices + 1);
                desired = Math.Max(desired, outgoing[fromIndex]);
                while (outgoing[fromIndex] < desired)
                {
                    var candidate = FindNearestUnconnected(
                        connected, fromIndex, current.Count, next.Count);
                    if (candidate < 0) break;
                    AddConnection(current, next, connected, outgoing, fromIndex, candidate);
                }
            }
        }

        private static int FindCoverageSource(
            int toIndex,
            int currentCount,
            int nextCount,
            int[] outgoing,
            int localMaximum,
            ref MapRandom random)
        {
            if (currentCount <= 1) return 0;
            var nearest = (int)Math.Round(
                NormalizedColumn(toIndex, nextCount) * (currentCount - 1));
            var preferred = Math.Max(0, Math.Min(
                currentCount - 1, nearest + random.Next(3) - 1));
            if (outgoing[preferred] < localMaximum) return preferred;

            var best = -1;
            var bestDistance = Int32.MaxValue;
            for (var sourceIndex = 0; sourceIndex < currentCount; sourceIndex++)
            {
                if (outgoing[sourceIndex] >= localMaximum) continue;
                var distance = Math.Abs(sourceIndex - nearest);
                if (distance > bestDistance) continue;
                if (distance == bestDistance && best >= 0 && random.Next(2) == 0) continue;
                best = sourceIndex;
                bestDistance = distance;
            }
            return best >= 0 ? best : nearest;
        }

        private static void AddConnection(
            IReadOnlyList<ExpeditionMapNode> current,
            IReadOnlyList<ExpeditionMapNode> next,
            bool[,] connected,
            int[] outgoing,
            int fromIndex,
            int toIndex)
        {
            if (connected[fromIndex, toIndex]) return;
            connected[fromIndex, toIndex] = true;
            outgoing[fromIndex]++;
            current[fromIndex].AddNext(next[toIndex].Id);
        }

        private static int FindNearestUnconnected(
            bool[,] connected,
            int fromIndex,
            int currentCount,
            int nextCount)
        {
            var nearest = -1;
            var nearestDistance = Single.MaxValue;
            var fromPosition = NormalizedColumn(fromIndex, currentCount);
            for (var toIndex = 0; toIndex < nextCount; toIndex++)
            {
                if (connected[fromIndex, toIndex]) continue;
                var distance = Math.Abs(fromPosition - NormalizedColumn(toIndex, nextCount));
                if (distance >= nearestDistance) continue;
                nearest = toIndex;
                nearestDistance = distance;
            }
            return nearest;
        }

        private static int NearestNextIndex(int fromIndex, int currentCount, int nextCount)
        {
            if (nextCount <= 1) return 0;
            return (int)Math.Round(
                NormalizedColumn(fromIndex, currentCount) * (nextCount - 1));
        }

        private static float NormalizedColumn(int index, int count)
        {
            return count <= 1 ? 0.5f : (float)index / (count - 1);
        }

        private static bool ContainsType(
            IReadOnlyList<ExpeditionMapNode> values, ExpeditionNodeType type)
        {
            foreach (var candidate in values)
            {
                if (candidate.Type == type) return true;
            }
            return false;
        }

        private static bool ContainsDangerousNode(IReadOnlyList<ExpeditionMapNode> values)
        {
            return ContainsType(values, ExpeditionNodeType.EliteBattle) ||
                   ContainsType(values, ExpeditionNodeType.HighRiskBattle);
        }

        private static bool AllTargetSetsEqual(IReadOnlyList<ExpeditionMapNode> values)
        {
            if (values.Count <= 1) return true;
            for (var index = 1; index < values.Count; index++)
            {
                if (!SameTargetSet(values[0].NextNodeIds, values[index].NextNodeIds))
                    return false;
            }
            return true;
        }

        private static bool SameTargetSet(
            IReadOnlyList<string> first, IReadOnlyList<string> second)
        {
            if (first.Count != second.Count) return false;
            foreach (var value in first)
            {
                var found = false;
                foreach (var candidate in second)
                {
                    if (candidate != value) continue;
                    found = true;
                    break;
                }
                if (!found) return false;
            }
            return true;
        }

        private static void Shuffle(ExpeditionNodeType[] values, ref MapRandom random)
        {
            for (var index = values.Length - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                var value = values[index];
                values[index] = values[swap];
                values[swap] = value;
            }
        }

        private static void Shuffle(List<ExpeditionNodeType> values, ref MapRandom random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                var value = values[index];
                values[index] = values[swap];
                values[swap] = value;
            }
        }

        private static void Shuffle<T>(List<T> values, ref E21Random random)
        {
            for (var index = values.Count - 1; index > 0; index--)
            {
                var swap = random.Next(index + 1);
                var value = values[index];
                values[index] = values[swap];
                values[swap] = value;
            }
        }

        /// <summary>
        /// Exact uint32 port of the E21 JavaScript reference random stream. Multiplication,
        /// addition and shifts intentionally retain unchecked 32-bit overflow semantics.
        /// </summary>
        private struct E21Random
        {
            private uint state;

            public E21Random(uint seed, uint salt)
            {
                state = unchecked(seed ^ salt ^ 0x9e3779b9u);
            }

            public int Next(int maximum)
            {
                state = unchecked(state + 0x6D2B79F5u);
                var value = state;
                value = unchecked((value ^ (value >> 15)) * (value | 1u));
                value ^= unchecked(value + unchecked((value ^ (value >> 7)) * (value | 61u)));
                var sample = value ^ (value >> 14);
                return maximum <= 1
                    ? 0
                    : (int)(((ulong)sample * (uint)maximum) >> 32);
            }
        }

        private struct MapRandom
        {
            private uint state;

            public MapRandom(uint seed)
            {
                state = seed == 0 ? 0x51EA2C31u : seed;
            }

            public int Next(int maximum)
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return maximum <= 1 ? 0 : (int)(state % (uint)maximum);
            }
        }
    }
}
