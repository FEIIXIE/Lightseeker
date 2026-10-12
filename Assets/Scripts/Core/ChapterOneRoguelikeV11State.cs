using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum ChapterOneRoguelikeSelectionCode
    {
        Success,
        StaleVersion,
        ChoiceSetNotFound,
        OptionNotFound,
        AlreadySelected,
        ChoiceSetComplete,
        EffectAtCap,
        KeystoneAlreadyOwned
    }

    [Serializable]
    public sealed class ChapterOneRoguelikeChoiceSetV11
    {
        public string runId;
        public string choiceSetId;
        public int choiceOrdinal;
        public int choiceKind;
        public ChapterOneRoguelikeCandidate[] options;
        public string[] selectedOptionIds;
        public int requiredSelectionCount;
        public int expectedRunStateVersion;
        public int contractRulesVersion;
        public string contractId;
        public string generationSeedDigest;
        public bool preparationP11DoubleStack;

        public ChapterOneRoguelikeCandidate FindOption(string optionId)
        {
            if (options == null) return null;
            for (var i = 0; i < options.Length; i++)
                if (string.Equals(OptionId(i), optionId, StringComparison.Ordinal)) return options[i];
            return null;
        }

        public bool ContainsSelected(string optionId)
        {
            if (selectedOptionIds == null) return false;
            for (var i = 0; i < selectedOptionIds.Length; i++)
                if (string.Equals(selectedOptionIds[i], optionId, StringComparison.Ordinal)) return true;
            return false;
        }

        public bool IsComplete => (selectedOptionIds == null ? 0 : selectedOptionIds.Length) >=
                                  Math.Max(1, requiredSelectionCount);

        public static string OptionId(int index) => "O" + (index + 1);
    }

    [Serializable]
    public sealed class ChapterOneRoguelikeSelectionReceipt
    {
        public string actionId;
        public int resultCode;
        public int resultingVersion;
        public string choiceSetId;
        public string optionId;
        public string acquiredId;
        public int acquiredRarityId;
        public int acquiredStackCount;
        public string resultDigest;
        [NonSerialized] public bool isReplay;
    }

    public sealed class ChapterOneRoguelikeV11State
    {
        public const int RuntimeGeneration = 3;

        private readonly List<RoguelikeAcquiredEffect> normalEffects =
            new List<RoguelikeAcquiredEffect>();
        private readonly Dictionary<string, ChapterOneRoguelikeSelectionReceipt> receipts =
            new Dictionary<string, ChapterOneRoguelikeSelectionReceipt>(StringComparer.Ordinal);
        private readonly List<string> receiptOrder = new List<string>();

        public int StateVersion { get; private set; }
        public int ChoiceOrdinal { get; private set; }
        public string SelectedKeystoneId { get; private set; } = string.Empty;
        public ChapterOneRoguelikeChoiceSetV11 PendingChoiceSet { get; private set; }
        public IReadOnlyList<RoguelikeAcquiredEffect> NormalEffects => normalEffects;

        public static bool IsPendingChoiceCompatible(ChapterOneRoguelikeChoiceSetV11 pending)
        {
            return pending == null ||
                   (pending.contractRulesVersion == ChapterOneRoguelikeV11Rules.ContractRulesVersion &&
                    string.Equals(pending.contractId, ChapterOneRoguelikeV11Rules.ContractId,
                        StringComparison.Ordinal));
        }

        public ChapterOneRoguelikeChoiceSetV11 EnsureChoiceSet(string runId, string nodeId,
            bool isBoss, int journeyProgressBasisPoints, bool elite,
            ChapterOneRoguelikeEligibility eligibility, ChapterOneLanternRoute lanternRoute,
            bool preparationP09ChooseTwo, bool preparationP10BuildFocus,
            bool preparationP11DoubleStack)
        {
            if (PendingChoiceSet != null) return PendingChoiceSet;
            var kind = ChapterOneRoguelikeV11Rules.ChoiceKindForVictory(nodeId, isBoss);
            if (kind == ChapterOneRoguelikeChoiceKind.None) return null;
            var candidates = kind == ChapterOneRoguelikeChoiceKind.Keystone
                ? ChapterOneRoguelikeV11Rules.GenerateKeystoneCandidates(runId, ChoiceOrdinal,
                    eligibility)
                : ChapterOneRoguelikeV11Rules.GenerateNormalCandidates(runId, ChoiceOrdinal,
                    journeyProgressBasisPoints, elite, eligibility, normalEffects, lanternRoute,
                    SelectedKeystoneId, preparationP10BuildFocus);
            var digest = RoguelikeEnhancementRules.StableHash(runId + "|" + ChoiceOrdinal + "|" +
                ChapterOneRoguelikeV11Rules.ContractId + "|" + kind).ToString("X8");
            PendingChoiceSet = new ChapterOneRoguelikeChoiceSetV11
            {
                runId = runId,
                choiceSetId = "v11-choice-" + ChoiceOrdinal + "-" + digest,
                choiceOrdinal = ChoiceOrdinal,
                choiceKind = (int)kind,
                options = candidates,
                selectedOptionIds = Array.Empty<string>(),
                requiredSelectionCount = kind == ChapterOneRoguelikeChoiceKind.Normal &&
                                         preparationP09ChooseTwo ? 2 : 1,
                expectedRunStateVersion = StateVersion,
                contractRulesVersion = ChapterOneRoguelikeV11Rules.ContractRulesVersion,
                contractId = ChapterOneRoguelikeV11Rules.ContractId,
                generationSeedDigest = digest,
                preparationP11DoubleStack = kind == ChapterOneRoguelikeChoiceKind.Normal &&
                                            preparationP11DoubleStack
            };
            ChoiceOrdinal++;
            return PendingChoiceSet;
        }

        public ChapterOneRoguelikeSelectionReceipt Select(string choiceSetId, string optionId,
            string actionId, int expectedVersion)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));
            ChapterOneRoguelikeSelectionReceipt prior;
            if (receipts.TryGetValue(actionId, out prior))
            {
                var replay = CopyReceipt(prior);
                replay.isReplay = true;
                return replay;
            }

            var code = ChapterOneRoguelikeSelectionCode.Success;
            ChapterOneRoguelikeCandidate option = null;
            var stackCount = 0;
            if (expectedVersion != StateVersion) code = ChapterOneRoguelikeSelectionCode.StaleVersion;
            else if (PendingChoiceSet == null ||
                     !string.Equals(PendingChoiceSet.choiceSetId, choiceSetId,
                         StringComparison.Ordinal))
                code = ChapterOneRoguelikeSelectionCode.ChoiceSetNotFound;
            else if (PendingChoiceSet.IsComplete) code = ChapterOneRoguelikeSelectionCode.ChoiceSetComplete;
            else if (PendingChoiceSet.ContainsSelected(optionId))
                code = ChapterOneRoguelikeSelectionCode.AlreadySelected;
            else if ((option = PendingChoiceSet.FindOption(optionId)) == null)
                code = ChapterOneRoguelikeSelectionCode.OptionNotFound;
            else if ((ChapterOneRoguelikeChoiceKind)PendingChoiceSet.choiceKind ==
                     ChapterOneRoguelikeChoiceKind.Keystone &&
                     !string.IsNullOrEmpty(SelectedKeystoneId))
                code = ChapterOneRoguelikeSelectionCode.KeystoneAlreadyOwned;
            else if ((ChapterOneRoguelikeChoiceKind)PendingChoiceSet.choiceKind ==
                     ChapterOneRoguelikeChoiceKind.Normal)
            {
                var definition = ChapterOneRoguelikeV11Rules.GetNormal(option.Id);
                var current = RoguelikeEnhancementRules.StackCount(normalEffects, option.Id);
                if (current >= definition.MaximumStacks)
                    code = ChapterOneRoguelikeSelectionCode.EffectAtCap;
                else
                    stackCount = Math.Min(definition.MaximumStacks - current,
                        PendingChoiceSet.preparationP11DoubleStack ? 2 : 1);
            }

            if (code == ChapterOneRoguelikeSelectionCode.Success)
            {
                StateVersion++;
                PendingChoiceSet.selectedOptionIds = Append(PendingChoiceSet.selectedOptionIds, optionId);
                PendingChoiceSet.expectedRunStateVersion = StateVersion;
                if ((ChapterOneRoguelikeChoiceKind)PendingChoiceSet.choiceKind ==
                    ChapterOneRoguelikeChoiceKind.Keystone)
                {
                    SelectedKeystoneId = option.Id;
                    stackCount = 1;
                }
                else
                {
                    normalEffects.Add(new RoguelikeAcquiredEffect
                    {
                        effectId = option.Id,
                        rarityId = option.RarityId,
                        stackCount = stackCount,
                        sourceChoiceSetId = PendingChoiceSet.choiceSetId,
                        acquiredAtRunStateVersion = StateVersion
                    });
                }
            }

            var receipt = new ChapterOneRoguelikeSelectionReceipt
            {
                actionId = actionId,
                resultCode = (int)code,
                resultingVersion = StateVersion,
                choiceSetId = choiceSetId,
                optionId = optionId,
                acquiredId = code == ChapterOneRoguelikeSelectionCode.Success ? option.Id : string.Empty,
                acquiredRarityId = code == ChapterOneRoguelikeSelectionCode.Success
                    ? option.RarityId
                    : -1,
                acquiredStackCount = code == ChapterOneRoguelikeSelectionCode.Success ? stackCount : 0,
                resultDigest = ((int)code) + ":" + StateVersion + ":" + (optionId ?? string.Empty) +
                               ":" + stackCount
            };
            receipts.Add(actionId, receipt);
            receiptOrder.Add(actionId);
            return CopyReceipt(receipt);
        }

        public ChapterOneRoguelikeSelectionReceipt SelectManyAtomic(string choiceSetId,
            string[] optionIds, string actionId, int expectedVersion)
        {
            if (optionIds == null || optionIds.Length == 0)
                return Select(choiceSetId, string.Empty, actionId, expectedVersion);
            if (optionIds.Length == 1)
                return Select(choiceSetId, optionIds[0], actionId, expectedVersion);
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));
            ChapterOneRoguelikeSelectionReceipt prior;
            if (receipts.TryGetValue(actionId, out prior))
            {
                var replay = CopyReceipt(prior);
                replay.isReplay = true;
                return replay;
            }

            var code = ChapterOneRoguelikeSelectionCode.Success;
            var options = new List<ChapterOneRoguelikeCandidate>();
            var selected = new HashSet<string>(StringComparer.Ordinal);
            if (expectedVersion != StateVersion) code = ChapterOneRoguelikeSelectionCode.StaleVersion;
            else if (PendingChoiceSet == null ||
                     !string.Equals(PendingChoiceSet.choiceSetId, choiceSetId,
                         StringComparison.Ordinal))
                code = ChapterOneRoguelikeSelectionCode.ChoiceSetNotFound;
            else if (PendingChoiceSet.IsComplete) code = ChapterOneRoguelikeSelectionCode.ChoiceSetComplete;
            else if (optionIds.Length != PendingChoiceSet.requiredSelectionCount)
                code = ChapterOneRoguelikeSelectionCode.OptionNotFound;

            if (code == ChapterOneRoguelikeSelectionCode.Success)
            {
                for (var index = 0; index < optionIds.Length; index++)
                {
                    var optionId = optionIds[index];
                    var option = PendingChoiceSet.FindOption(optionId);
                    if (!selected.Add(optionId) || option == null)
                    {
                        code = ChapterOneRoguelikeSelectionCode.OptionNotFound;
                        break;
                    }
                    var definition = ChapterOneRoguelikeV11Rules.GetNormal(option.Id);
                    if (RoguelikeEnhancementRules.StackCount(normalEffects, option.Id) >=
                        definition.MaximumStacks)
                    {
                        code = ChapterOneRoguelikeSelectionCode.EffectAtCap;
                        break;
                    }
                    options.Add(option);
                }
            }

            if (code == ChapterOneRoguelikeSelectionCode.Success)
            {
                StateVersion++;
                PendingChoiceSet.selectedOptionIds = (string[])optionIds.Clone();
                PendingChoiceSet.expectedRunStateVersion = StateVersion;
                for (var index = 0; index < options.Count; index++)
                {
                    var option = options[index];
                    normalEffects.Add(new RoguelikeAcquiredEffect
                    {
                        effectId = option.Id,
                        rarityId = option.RarityId,
                        stackCount = 1,
                        sourceChoiceSetId = PendingChoiceSet.choiceSetId,
                        acquiredAtRunStateVersion = StateVersion
                    });
                }
            }

            var joinedOptions = string.Join(",", optionIds);
            var acquiredIds = new List<string>();
            for (var index = 0; index < options.Count; index++) acquiredIds.Add(options[index].Id);
            var receipt = new ChapterOneRoguelikeSelectionReceipt
            {
                actionId = actionId,
                resultCode = (int)code,
                resultingVersion = StateVersion,
                choiceSetId = choiceSetId,
                optionId = joinedOptions,
                acquiredId = code == ChapterOneRoguelikeSelectionCode.Success
                    ? string.Join(",", acquiredIds.ToArray()) : string.Empty,
                acquiredRarityId = -1,
                acquiredStackCount = code == ChapterOneRoguelikeSelectionCode.Success
                    ? options.Count : 0,
                resultDigest = ((int)code) + ":" + StateVersion + ":" + joinedOptions +
                               ":" + options.Count
            };
            receipts.Add(actionId, receipt);
            receiptOrder.Add(actionId);
            return CopyReceipt(receipt);
        }

        public bool CompleteSelection()
        {
            if (PendingChoiceSet == null || !PendingChoiceSet.IsComplete) return false;
            PendingChoiceSet = null;
            return true;
        }

        public void SynchronizeRunStateVersion(int version)
        {
            if (PendingChoiceSet == null) StateVersion = Math.Max(StateVersion, Math.Max(0, version));
        }

        public void Restore(int runtimeGeneration, int stateVersion, int choiceOrdinal,
            string selectedKeystoneId, ChapterOneRoguelikeChoiceSetV11 pending,
            IEnumerable<RoguelikeAcquiredEffect> effects,
            IEnumerable<ChapterOneRoguelikeSelectionReceipt> savedReceipts)
        {
            Reset();
            if (runtimeGeneration != RuntimeGeneration)
                throw new InvalidOperationException("Roguelike runtime generation mismatch.");
            StateVersion = Math.Max(0, stateVersion);
            ChoiceOrdinal = Math.Max(0, choiceOrdinal);
            if (!string.IsNullOrEmpty(selectedKeystoneId))
            {
                ChapterOneRoguelikeV11Rules.GetKeystone(selectedKeystoneId);
                SelectedKeystoneId = selectedKeystoneId;
            }
            if (pending != null)
            {
                if (!IsPendingChoiceCompatible(pending))
                    throw new InvalidOperationException("Pending roguelike contract mismatch.");
                PendingChoiceSet = pending;
            }
            if (effects != null)
                foreach (var effect in effects)
                {
                    if (effect == null) continue;
                    ChapterOneRoguelikeV11Rules.GetNormal(effect.effectId);
                    normalEffects.Add(effect);
                }
            if (savedReceipts == null) return;
            foreach (var receipt in savedReceipts)
            {
                if (receipt == null || string.IsNullOrWhiteSpace(receipt.actionId) ||
                    receipts.ContainsKey(receipt.actionId)) continue;
                var copy = CopyReceipt(receipt);
                receipts.Add(copy.actionId, copy);
                receiptOrder.Add(copy.actionId);
            }
        }

        public RoguelikeAcquiredEffect[] CopyNormalEffects() => normalEffects.ToArray();

        public ChapterOneRoguelikeSelectionReceipt[] CopyReceipts()
        {
            var result = new ChapterOneRoguelikeSelectionReceipt[receiptOrder.Count];
            for (var i = 0; i < result.Length; i++) result[i] = CopyReceipt(receipts[receiptOrder[i]]);
            return result;
        }

        public void Reset()
        {
            normalEffects.Clear();
            receipts.Clear();
            receiptOrder.Clear();
            StateVersion = 0;
            ChoiceOrdinal = 0;
            SelectedKeystoneId = string.Empty;
            PendingChoiceSet = null;
        }

        private static string[] Append(string[] values, string value)
        {
            var source = values ?? Array.Empty<string>();
            var result = new string[source.Length + 1];
            Array.Copy(source, result, source.Length);
            result[result.Length - 1] = value;
            return result;
        }

        private static ChapterOneRoguelikeSelectionReceipt CopyReceipt(
            ChapterOneRoguelikeSelectionReceipt source)
        {
            return new ChapterOneRoguelikeSelectionReceipt
            {
                actionId = source.actionId,
                resultCode = source.resultCode,
                resultingVersion = source.resultingVersion,
                choiceSetId = source.choiceSetId,
                optionId = source.optionId,
                acquiredId = source.acquiredId,
                acquiredRarityId = source.acquiredRarityId,
                acquiredStackCount = source.acquiredStackCount,
                resultDigest = source.resultDigest,
                isReplay = source.isReplay
            };
        }
    }
}
