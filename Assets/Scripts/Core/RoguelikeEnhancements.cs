using System;
using System.Collections.Generic;
using System.Text;

namespace SeekerAutoChess.Core
{
    public enum RoguelikeRarity { Common, Uncommon, Rare, Epic, Legendary }

    public enum RoguelikeChoiceResultCode
    {
        Success,
        StaleVersion,
        ChoiceSetNotFound,
        OptionNotFound,
        AlreadySelected,
        EffectAtCap
    }

    public sealed class RoguelikeEffectDefinition
    {
        public string EffectId { get; }
        public string NameKey { get; }
        public string ScopeKey { get; }
        public int MaximumStacks { get; }
        public bool EnabledInD1 { get; }
        public IReadOnlyList<int> Values { get; }

        public RoguelikeEffectDefinition(string effectId, string nameKey, string scopeKey,
            int maximumStacks, bool enabledInD1, params int[] values)
        {
            EffectId = effectId;
            NameKey = nameKey;
            ScopeKey = scopeKey;
            MaximumStacks = maximumStacks;
            EnabledInD1 = enabledInD1;
            Values = values;
        }

        public int Value(RoguelikeRarity rarity)
        {
            return Values[Math.Max(0, Math.Min(Values.Count - 1, (int)rarity))];
        }
    }

    [Serializable]
    public sealed class RoguelikeAcquiredEffect
    {
        public string effectId;
        public int rarityId;
        public int stackCount;
        public string sourceChoiceSetId;
        public int acquiredAtRunStateVersion;
    }

    [Serializable]
    public sealed class RoguelikeChoiceOption
    {
        public string optionId;
        public string effectId;
        public int rarityId;
    }

    [Serializable]
    public sealed class RoguelikeChoiceSet
    {
        public string runId;
        public string choiceSetId;
        public int choiceOrdinal;
        public RoguelikeChoiceOption[] options;
        public string selectedOptionId;
        public int expectedRunStateVersion;
        public int roguelikeRulesVersion;
        public string generationSeedDigest;

        public RoguelikeChoiceOption FindOption(string id)
        {
            if (options == null) return null;
            for (var i = 0; i < options.Length; i++)
                if (options[i] != null && options[i].optionId == id) return options[i];
            return null;
        }
    }

    public sealed class RoguelikeEligibilityContext
    {
        public bool HasSteelOrGuardian;
        public int DirectAttackerCount;
        public bool HasShieldSource;
    }

    [Serializable]
    public sealed class RoguelikeChoiceReceipt
    {
        public string actionId;
        public int resultCode;
        public int resultingVersion;
        public string choiceSetId;
        public string optionId;
        public string resultDigest;
        [NonSerialized] public bool isReplay;
    }

    public static class RoguelikeEnhancementRules
    {
        public const int RulesVersion = 2;

        private static readonly RoguelikeEffectDefinition[] definitions =
        {
            new RoguelikeEffectDefinition("B01", "rogue.B01.name", "rogue.scope.team", 3, true, 400, 600, 800, 1100, 1500),
            new RoguelikeEffectDefinition("B02", "rogue.B02.name", "rogue.scope.team", 3, true, 500, 700, 1000, 1400, 1900),
            new RoguelikeEffectDefinition("B03", "rogue.B03.name", "rogue.scope.steelGuardian", 2, true, 3, 5, 7, 10, 14),
            new RoguelikeEffectDefinition("B04", "rogue.B04.name", "rogue.scope.directAttack", 2, true, 600, 900, 1200, 1700, 2300),
            new RoguelikeEffectDefinition("B06", "rogue.B06.name", "rogue.scope.directAttack", 2, true, 500, 800, 1100, 1500, 2000),
            new RoguelikeEffectDefinition("B08", "rogue.B08.name", "rogue.scope.shield", 2, true, 400, 600, 800, 1100, 1500),
            new RoguelikeEffectDefinition("B10", "rogue.B10.name", "rogue.scope.focus", 2, true, 4, 6, 9, 13, 18),
            // B12 is deliberately registered for stable IDs/save compatibility, but is neither offered nor effective in D1.
            new RoguelikeEffectDefinition("B12", "rogue.B12.name", "rogue.scope.escort", 1, false, 0, 0, 0, 0, 0),
            new RoguelikeEffectDefinition("B13", "rogue.B13.name", "rogue.scope.team", 2, true, 400, 600, 900, 1300, 1800),
            new RoguelikeEffectDefinition("B14", "rogue.B14.name", "rogue.scope.team", 1, true, 600, 800, 1100, 1500, 2000),
            new RoguelikeEffectDefinition("B15", "rogue.B15.name", "rogue.scope.lowHealth", 2, true, 600, 900, 1200, 1700, 2300)
        };

        private static readonly Dictionary<string, RoguelikeEffectDefinition> byId = BuildIndex();
        public static IReadOnlyList<RoguelikeEffectDefinition> Definitions => definitions;

        public static RoguelikeEffectDefinition Get(string effectId)
        {
            RoguelikeEffectDefinition definition;
            if (!byId.TryGetValue(effectId ?? string.Empty, out definition))
                throw new KeyNotFoundException("Unknown roguelike effect: " + effectId);
            return definition;
        }

        public static bool IsEligible(string effectId, RoguelikeEligibilityContext context)
        {
            if (context == null) return false;
            switch (effectId)
            {
                case "B03": return context.HasSteelOrGuardian;
                case "B04":
                case "B06": return context.DirectAttackerCount >= 1;
                case "B08": return context.HasShieldSource;
                case "B10": return context.DirectAttackerCount >= 2;
                case "B01":
                case "B02":
                case "B13":
                case "B14":
                case "B15": return true;
                default: return false;
            }
        }

        public static int SumValueBasisPoints(IEnumerable<RoguelikeAcquiredEffect> acquired, string effectId, int cap)
        {
            var value = 0;
            if (acquired != null)
            {
                foreach (var item in acquired)
                {
                    if (item == null || item.effectId != effectId) continue;
                    value += Get(effectId).Value((RoguelikeRarity)item.rarityId) * Math.Max(0, item.stackCount);
                }
            }
            return Math.Min(cap, Math.Max(0, value));
        }

        public static int SumValue(IEnumerable<RoguelikeAcquiredEffect> acquired, string effectId, int cap)
        {
            return SumValueBasisPoints(acquired, effectId, cap);
        }

        public static int ApplyBasisPointIncreaseFloor(int baseValue, int basisPoints)
        {
            return Math.Max(1, (int)((long)Math.Max(1, baseValue) *
                (10000 + Math.Max(0, basisPoints)) / 10000));
        }

        public static int EffectiveAttackDamage(int baseAttackDamage,
            IEnumerable<RoguelikeAcquiredEffect> acquired)
        {
            return ApplyBasisPointIncreaseFloor(baseAttackDamage,
                SumValueBasisPoints(acquired, "B01", 10000));
        }

        public static int EffectiveMaximumHealth(int baseMaximumHealth,
            IEnumerable<RoguelikeAcquiredEffect> acquired)
        {
            return ApplyBasisPointIncreaseFloor(baseMaximumHealth,
                SumValueBasisPoints(acquired, "B02", 10000));
        }

        public static int EffectiveAttackSpeedBasisPoints(int baseAttackSpeedBasisPoints,
            IEnumerable<RoguelikeAcquiredEffect> acquired)
        {
            return ContinuousAttackSpeedRules.CalculateFinal(baseAttackSpeedBasisPoints,
                SumValueBasisPoints(acquired, "B13", 3000));
        }

        public static int EffectiveArmor(int baseArmor, string factionId, string professionId,
            IEnumerable<RoguelikeAcquiredEffect> acquired)
        {
            var armor = Math.Max(0, Math.Min(80, baseArmor));
            if (factionId != "faction.steel" && professionId != "profession.guardian") return armor;
            return Math.Min(80, armor + SumValue(acquired, "B03", 80));
        }

        public static int StackCount(IEnumerable<RoguelikeAcquiredEffect> acquired, string effectId)
        {
            var count = 0;
            if (acquired != null)
                foreach (var item in acquired)
                    if (item != null && item.effectId == effectId) count += Math.Max(0, item.stackCount);
            return count;
        }

        public static RoguelikeChoiceSet GenerateChoiceSet(string runId, int choiceOrdinal,
            int runStateVersion, int journeyProgressBasisPoints, bool elite,
            RoguelikeEligibilityContext context, IEnumerable<RoguelikeAcquiredEffect> acquired)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run id is required.", nameof(runId));
            var eligible = new List<string>();
            for (var i = 0; i < definitions.Length; i++)
            {
                var definition = definitions[i];
                if (!definition.EnabledInD1 || !IsEligible(definition.EffectId, context)) continue;
                if (StackCount(acquired, definition.EffectId) >= definition.MaximumStacks) continue;
                eligible.Add(definition.EffectId);
            }
            if (eligible.Count < 3) throw new InvalidOperationException("D1 requires three distinct immediately effective options.");

            var seed = StableHash(runId + "|" + choiceOrdinal + "|" + RulesVersion);
            var selected = new string[3];
            selected[0] = Pick(eligible, seed ^ 0xA511E9B3u);

            var safe = Filter(eligible, "B01", "B02", "B14");
            Remove(safe, selected[0]);
            if (safe.Count == 0) safe = Without(eligible, selected);
            selected[1] = Pick(safe, seed ^ 0x63D83595u);

            var alternate = Without(eligible, selected);
            selected[2] = Pick(alternate, seed ^ 0xC2B2AE35u);

            var options = new RoguelikeChoiceOption[3];
            for (var i = 0; i < options.Length; i++)
            {
                var rarity = RollRarity(seed ^ (uint)(0x9E3779B9u * (uint)(i + 1)), journeyProgressBasisPoints, false);
                options[i] = new RoguelikeChoiceOption
                {
                    optionId = "O" + (i + 1), effectId = selected[i], rarityId = (int)rarity
                };
            }
            if (elite && !ContainsRareOrBetter(options))
                options[(int)(seed % 3u)].rarityId = (int)RollRarity(seed ^ 0x85EBCA6Bu, journeyProgressBasisPoints, true);

            var digest = seed.ToString("X8");
            return new RoguelikeChoiceSet
            {
                runId = runId,
                choiceSetId = "choice-" + choiceOrdinal + "-" + digest,
                choiceOrdinal = choiceOrdinal,
                options = options,
                selectedOptionId = string.Empty,
                expectedRunStateVersion = runStateVersion,
                roguelikeRulesVersion = RulesVersion,
                generationSeedDigest = digest
            };
        }

        public static RoguelikeRarity RollRarity(uint seed, int progressBasisPoints, bool guaranteedRare)
        {
            if (guaranteedRare)
            {
                var roll = (int)(seed % 100u);
                return roll < 75 ? RoguelikeRarity.Rare : roll < 97 ? RoguelikeRarity.Epic : RoguelikeRarity.Legendary;
            }
            var weights = RarityWeights(progressBasisPoints);
            var value = (int)(seed % 10000u);
            var sum = 0;
            for (var i = 0; i < weights.Length; i++)
            {
                sum += weights[i];
                if (value < sum) return (RoguelikeRarity)i;
            }
            return RoguelikeRarity.Legendary;
        }

        public static int[] RarityWeights(int progressBasisPoints)
        {
            if (progressBasisPoints < 2500) return new[] { 6000, 3000, 900, 100, 0 };
            if (progressBasisPoints < 5000) return new[] { 4800, 3400, 1500, 280, 20 };
            if (progressBasisPoints < 7500) return new[] { 3800, 3400, 2100, 650, 50 };
            return new[] { 2800, 3400, 2700, 950, 150 };
        }

        public static uint StableHash(string text)
        {
            unchecked
            {
                var hash = 2166136261u;
                var value = text ?? string.Empty;
                for (var i = 0; i < value.Length; i++) { hash ^= value[i]; hash *= 16777619u; }
                hash ^= hash >> 16; hash *= 2246822519u; hash ^= hash >> 13;
                return hash;
            }
        }

        private static Dictionary<string, RoguelikeEffectDefinition> BuildIndex()
        {
            var result = new Dictionary<string, RoguelikeEffectDefinition>(StringComparer.Ordinal);
            for (var i = 0; i < definitions.Length; i++) result.Add(definitions[i].EffectId, definitions[i]);
            return result;
        }

        private static List<string> Filter(IEnumerable<string> source, params string[] allowed)
        {
            var set = new HashSet<string>(allowed, StringComparer.Ordinal);
            var result = new List<string>();
            foreach (var item in source) if (set.Contains(item)) result.Add(item);
            return result;
        }

        private static List<string> Without(IEnumerable<string> source, IEnumerable<string> excluded)
        {
            var set = new HashSet<string>(excluded ?? Array.Empty<string>(), StringComparer.Ordinal);
            var result = new List<string>();
            foreach (var item in source) if (!set.Contains(item)) result.Add(item);
            return result;
        }

        private static void Remove(ICollection<string> values, string value) { values.Remove(value); }
        private static string Pick(IReadOnlyList<string> values, uint seed) { return values[(int)(seed % (uint)values.Count)]; }
        private static bool ContainsRareOrBetter(IEnumerable<RoguelikeChoiceOption> options)
        {
            foreach (var option in options) if (option.rarityId >= (int)RoguelikeRarity.Rare) return true;
            return false;
        }
    }

    public sealed class RoguelikeBuildState
    {
        private readonly List<RoguelikeAcquiredEffect> acquired = new List<RoguelikeAcquiredEffect>();
        private readonly Dictionary<string, RoguelikeChoiceReceipt> receipts =
            new Dictionary<string, RoguelikeChoiceReceipt>(StringComparer.Ordinal);
        private readonly List<string> receiptOrder = new List<string>();

        public int StateVersion { get; private set; }
        public int ChoiceOrdinal { get; private set; }
        public RoguelikeChoiceSet PendingChoiceSet { get; private set; }
        public IReadOnlyList<RoguelikeAcquiredEffect> AcquiredEffects => acquired;
        public string LastActionReceipt { get; private set; } = string.Empty;

        public RoguelikeChoiceSet EnsureChoiceSet(string runId, int journeyProgressBasisPoints,
            bool elite, bool boss, bool battleVictory, RoguelikeEligibilityContext context)
        {
            if (PendingChoiceSet != null) return PendingChoiceSet;
            if (!battleVictory || boss) return null;
            PendingChoiceSet = RoguelikeEnhancementRules.GenerateChoiceSet(runId, ChoiceOrdinal,
                StateVersion, journeyProgressBasisPoints, elite, context, acquired);
            ChoiceOrdinal++;
            return PendingChoiceSet;
        }

        public void SynchronizeRunStateVersion(int runStateVersion)
        {
            if (PendingChoiceSet != null) return;
            StateVersion = Math.Max(StateVersion, Math.Max(0, runStateVersion));
        }

        public RoguelikeChoiceReceipt Select(string choiceSetId, string optionId, string actionId, int expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(actionId)) throw new ArgumentException("An action id is required.", nameof(actionId));
            RoguelikeChoiceReceipt existing;
            if (receipts.TryGetValue(actionId, out existing))
            {
                var replay = CopyReceipt(existing); replay.isReplay = true; return replay;
            }

            var code = RoguelikeChoiceResultCode.Success;
            RoguelikeChoiceOption option = null;
            if (expectedVersion != StateVersion) code = RoguelikeChoiceResultCode.StaleVersion;
            else if (PendingChoiceSet == null || PendingChoiceSet.choiceSetId != choiceSetId)
                code = RoguelikeChoiceResultCode.ChoiceSetNotFound;
            else if (!string.IsNullOrEmpty(PendingChoiceSet.selectedOptionId)) code = RoguelikeChoiceResultCode.AlreadySelected;
            else if ((option = PendingChoiceSet.FindOption(optionId)) == null) code = RoguelikeChoiceResultCode.OptionNotFound;
            else if (RoguelikeEnhancementRules.StackCount(acquired, option.effectId) >=
                     RoguelikeEnhancementRules.Get(option.effectId).MaximumStacks)
                code = RoguelikeChoiceResultCode.EffectAtCap;

            if (code == RoguelikeChoiceResultCode.Success)
            {
                StateVersion++;
                PendingChoiceSet.selectedOptionId = option.optionId;
                acquired.Add(new RoguelikeAcquiredEffect
                {
                    effectId = option.effectId,
                    rarityId = option.rarityId,
                    stackCount = 1,
                    sourceChoiceSetId = PendingChoiceSet.choiceSetId,
                    acquiredAtRunStateVersion = StateVersion
                });
            }
            var receipt = new RoguelikeChoiceReceipt
            {
                actionId = actionId,
                resultCode = (int)code,
                resultingVersion = StateVersion,
                choiceSetId = choiceSetId,
                optionId = optionId,
                resultDigest = ((int)code) + ":" + StateVersion + ":" + (optionId ?? string.Empty)
            };
            receipts.Add(actionId, receipt);
            receiptOrder.Add(actionId);
            LastActionReceipt = receipt.resultDigest;
            return CopyReceipt(receipt);
        }

        public void CompleteSelection() { PendingChoiceSet = null; }

        public void Restore(int stateVersion, int choiceOrdinal, RoguelikeChoiceSet pending,
            IEnumerable<RoguelikeAcquiredEffect> effects, IEnumerable<RoguelikeChoiceReceipt> savedReceipts)
        {
            Reset();
            StateVersion = Math.Max(0, stateVersion);
            ChoiceOrdinal = Math.Max(0, choiceOrdinal);
            PendingChoiceSet = pending;
            if (effects != null) foreach (var effect in effects) if (effect != null) acquired.Add(effect);
            if (savedReceipts == null) return;
            foreach (var receipt in savedReceipts)
            {
                if (receipt == null || string.IsNullOrWhiteSpace(receipt.actionId) || receipts.ContainsKey(receipt.actionId)) continue;
                var copy = CopyReceipt(receipt);
                receipts.Add(copy.actionId, copy);
                receiptOrder.Add(copy.actionId);
                LastActionReceipt = copy.resultDigest ?? string.Empty;
            }
        }

        public RoguelikeChoiceReceipt[] CopyReceipts()
        {
            var result = new RoguelikeChoiceReceipt[receiptOrder.Count];
            for (var i = 0; i < result.Length; i++) result[i] = CopyReceipt(receipts[receiptOrder[i]]);
            return result;
        }

        public RoguelikeAcquiredEffect[] CopyAcquiredEffects()
        {
            return acquired.ToArray();
        }

        public void Reset()
        {
            acquired.Clear(); receipts.Clear(); receiptOrder.Clear();
            StateVersion = 0; ChoiceOrdinal = 0; PendingChoiceSet = null; LastActionReceipt = string.Empty;
        }

        public string CreateDiagnosticSummary(string runId)
        {
            var text = new StringBuilder();
            text.Append("runId=").Append(runId ?? string.Empty).Append('\n');
            text.Append("roguelikeRunStateVersion=").Append(StateVersion).Append('\n');
            text.Append("roguelikeRulesVersion=").Append(RoguelikeEnhancementRules.RulesVersion).Append('\n');
            text.Append("attackSpeedRulesVersion=1\n");
            text.Append("choiceSetId=").Append(PendingChoiceSet == null ? "none" : PendingChoiceSet.choiceSetId).Append('\n');
            text.Append("options=");
            if (PendingChoiceSet != null && PendingChoiceSet.options != null)
                for (var i = 0; i < PendingChoiceSet.options.Length; i++)
                {
                    if (i > 0) text.Append('|');
                    text.Append(PendingChoiceSet.options[i].effectId).Append(':').Append(PendingChoiceSet.options[i].rarityId);
                }
            text.Append("\nselected=").Append(PendingChoiceSet == null ? string.Empty : PendingChoiceSet.selectedOptionId).Append('\n');
            text.Append("acquiredEffects=");
            for (var i = 0; i < acquired.Count; i++)
            {
                if (i > 0) text.Append('|');
                text.Append(acquired[i].effectId).Append(':').Append(acquired[i].rarityId).Append('x').Append(acquired[i].stackCount);
            }
            text.Append("\nunitAttackSpeeds=continuous-basis-points\nactionPeriods=integer-e27\n");
            text.Append("lastActionReceipt=").Append(LastActionReceipt ?? string.Empty).Append('\n');
            return text.ToString();
        }

        private static RoguelikeChoiceReceipt CopyReceipt(RoguelikeChoiceReceipt source)
        {
            return new RoguelikeChoiceReceipt
            {
                actionId = source.actionId, resultCode = source.resultCode,
                resultingVersion = source.resultingVersion, choiceSetId = source.choiceSetId,
                optionId = source.optionId, resultDigest = source.resultDigest, isReplay = source.isReplay
            };
        }
    }
}
