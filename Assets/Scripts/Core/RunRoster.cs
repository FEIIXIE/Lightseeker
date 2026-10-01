using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class RunUnitState
    {
        public string RunUnitId { get; private set; }
        public string DefinitionId { get; }
        public int BasePower { get; }
        public int RunLevel { get; }
        public int AcquisitionBasis { get; }
        public CharacterQuality Quality { get; }
        public int AptitudeRoll { get; }
        public string SourceBusinessId { get; }
        public int ExternalStar { get; }

        public RunUnitState(
            string definitionId,
            int basePower,
            int runLevel = 1,
            int acquisitionBasis = 0,
            string runUnitId = null,
            CharacterQuality quality = CharacterQuality.Common,
            int aptitudeRoll = -1,
            string sourceBusinessId = null,
            int externalStar = 0)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("A unit definition id is required.", nameof(definitionId));
            if (basePower <= 0) throw new ArgumentOutOfRangeException(nameof(basePower));
            if (runLevel < 1 || runLevel > RunUnitRules.MaximumRunLevel) throw new ArgumentOutOfRangeException(nameof(runLevel));
            if (acquisitionBasis < 0) throw new ArgumentOutOfRangeException(nameof(acquisitionBasis));
            if (!Enum.IsDefined(typeof(CharacterQuality), quality)) throw new ArgumentOutOfRangeException(nameof(quality));
            if (externalStar < 0 || externalStar > 2) throw new ArgumentOutOfRangeException(nameof(externalStar));
            RunUnitId = runUnitId ?? string.Empty;
            DefinitionId = definitionId;
            BasePower = basePower;
            RunLevel = runLevel;
            AcquisitionBasis = acquisitionBasis;
            Quality = quality;
            ExternalStar = externalStar;
            SourceBusinessId = string.IsNullOrWhiteSpace(sourceBusinessId)
                ? (string.IsNullOrEmpty(RunUnitId) ? definitionId : RunUnitId)
                : sourceBusinessId;
            AptitudeRoll = aptitudeRoll < 0
                ? CharacterRarityInstanceRules.ComputeAptitudeRoll(SourceBusinessId)
                : aptitudeRoll;
            if (AptitudeRoll < 0 || AptitudeRoll > CharacterRarityInstanceRules.RollScale)
                throw new ArgumentOutOfRangeException(nameof(aptitudeRoll));
        }

        public RunUnitState Promote()
        {
            if (RunLevel >= RunUnitRules.MaximumRunLevel) throw new InvalidOperationException("A maximum-level run unit cannot be promoted.");
            return new RunUnitState(DefinitionId, BasePower, RunLevel + 1, AcquisitionBasis, RunUnitId,
                Quality, AptitudeRoll, SourceBusinessId, ExternalStar);
        }

        internal RunUnitState Promote(int combinedAcquisitionBasis)
        {
            if (RunLevel >= RunUnitRules.MaximumRunLevel) throw new InvalidOperationException("A maximum-level run unit cannot be promoted.");
            return new RunUnitState(DefinitionId, BasePower, RunLevel + 1, combinedAcquisitionBasis, RunUnitId,
                Quality, AptitudeRoll, SourceBusinessId, ExternalStar);
        }

        public void EnsureRunUnitId(string runUnitId)
        {
            if (!string.IsNullOrEmpty(RunUnitId)) return;
            if (string.IsNullOrWhiteSpace(runUnitId))
                throw new ArgumentException("A run unit id is required.", nameof(runUnitId));
            RunUnitId = runUnitId;
        }
    }

    public sealed class RunRosterAddResult
    {
        public bool Added { get; internal set; }
        public int FinalIndex { get; internal set; } = -1;
        public int MergeCount { get; internal set; }
        public int FinalRunLevel { get; internal set; }
    }

    /// <summary>
    /// Pure run-scoped inventory. Indices [0, BoardCapacity) are deployed;
    /// the remaining indices are the bench. It contains no Unity or wallet state.
    /// </summary>
    public sealed class RunRoster
    {
        private readonly RunUnitState[] slots;

        public int BoardCapacity { get; }
        public int BenchCapacity { get; }
        public int TotalCapacity => slots.Length;

        public int TotalCount
        {
            get
            {
                var count = 0;
                foreach (var unit in slots) if (unit != null) count++;
                return count;
            }
        }

        public int BoardCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < BoardCapacity; i++) if (slots[i] != null) count++;
                return count;
            }
        }

        public RunRoster(int boardCapacity, int benchCapacity)
        {
            if (boardCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(boardCapacity));
            if (benchCapacity < 0) throw new ArgumentOutOfRangeException(nameof(benchCapacity));
            BoardCapacity = boardCapacity;
            BenchCapacity = benchCapacity;
            slots = new RunUnitState[boardCapacity + benchCapacity];
        }

        public RunUnitState Get(int index)
        {
            ValidateIndex(index);
            return slots[index];
        }

        public bool TryRestoreAt(int index, RunUnitState unit)
        {
            if (index < 0 || index >= slots.Length || unit == null || slots[index] != null) return false;
            slots[index] = unit;
            return true;
        }

        public bool ReplaceAt(int index, RunUnitState unit)
        {
            if (index < 0 || index >= slots.Length || unit == null) return false;
            slots[index] = unit;
            return true;
        }

        public bool TryMoveOrSwap(int sourceIndex, int destinationIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= slots.Length ||
                destinationIndex < 0 || destinationIndex >= slots.Length ||
                sourceIndex == destinationIndex || slots[sourceIndex] == null)
            {
                return false;
            }
            var source = slots[sourceIndex];
            slots[sourceIndex] = slots[destinationIndex];
            slots[destinationIndex] = source;
            return true;
        }

        public RunRosterAddResult TryAdd(RunUnitState newUnit)
        {
            if (newUnit == null) throw new ArgumentNullException(nameof(newUnit));
            var result = new RunRosterAddResult();
            var destination = FirstEmptyIndex();
            if (destination < 0) return result;
            slots[destination] = newUnit;
            result.Added = true;
            result.FinalIndex = destination;
            result.FinalRunLevel = newUnit.RunLevel;
            return result;
        }

        public RunRosterAddResult TryPurchase(RunUnitState newUnit)
        {
            if (newUnit == null) throw new ArgumentNullException(nameof(newUnit));
            var destination = FirstEmptyIndex();
            if (destination >= 0) return TryAdd(newUnit);

            var result = new RunRosterAddResult();
            if (newUnit.RunLevel >= RunUnitRules.MaximumRunLevel) return result;
            var matches = MatchingIndices(newUnit.DefinitionId, newUnit.RunLevel);
            if (matches.Count < RunUnitRules.CopiesPerMerge - 1) return result;

            var retainedIndex = PreferredPurchaseMergeIndex(matches);
            var consumedIndex = -1;
            for (var i = 0; i < matches.Count; i++)
            {
                if (matches[i] == retainedIndex) continue;
                consumedIndex = matches[i];
                break;
            }
            if (consumedIndex < 0) return result;

            var retained = slots[retainedIndex];
            var combinedBasis = retained.AcquisitionBasis + slots[consumedIndex].AcquisitionBasis +
                                newUnit.AcquisitionBasis;
            slots[consumedIndex] = null;
            slots[retainedIndex] = retained.Promote(combinedBasis);
            result.Added = true;
            result.FinalIndex = retainedIndex;
            result.MergeCount = 1;
            result.FinalRunLevel = slots[retainedIndex].RunLevel;
            return result;
        }

        public bool CanPurchase(RunUnitState newUnit)
        {
            if (newUnit == null) return false;
            if (FirstEmptyIndex() >= 0) return true;
            return newUnit.RunLevel < RunUnitRules.MaximumRunLevel &&
                   MatchingIndices(newUnit.DefinitionId, newUnit.RunLevel).Count >=
                   RunUnitRules.CopiesPerMerge - 1;
        }

        public bool TryManualMerge(int sourceIndex, int destinationIndex, out int finalRunLevel)
        {
            finalRunLevel = 0;
            if (sourceIndex < 0 || sourceIndex >= slots.Length ||
                destinationIndex < 0 || destinationIndex >= slots.Length ||
                sourceIndex == destinationIndex || slots[sourceIndex] == null || slots[destinationIndex] == null)
            {
                return false;
            }

            var source = slots[sourceIndex];
            var destination = slots[destinationIndex];
            if (source.DefinitionId != destination.DefinitionId || source.RunLevel != destination.RunLevel ||
                destination.RunLevel >= RunUnitRules.MaximumRunLevel)
            {
                return false;
            }

            var thirdIndex = FindManualMergeThirdIndex(sourceIndex, destinationIndex);
            if (thirdIndex < 0) return false;

            var combinedBasis = source.AcquisitionBasis + destination.AcquisitionBasis + slots[thirdIndex].AcquisitionBasis;
            slots[sourceIndex] = null;
            slots[thirdIndex] = null;
            slots[destinationIndex] = destination.Promote(combinedBasis);
            finalRunLevel = slots[destinationIndex].RunLevel;
            return true;
        }

        public int FindManualMergeThirdIndex(int sourceIndex, int destinationIndex)
        {
            if (sourceIndex < 0 || sourceIndex >= slots.Length ||
                destinationIndex < 0 || destinationIndex >= slots.Length ||
                sourceIndex == destinationIndex || slots[sourceIndex] == null || slots[destinationIndex] == null)
            {
                return -1;
            }
            var source = slots[sourceIndex];
            var destination = slots[destinationIndex];
            if (source.DefinitionId != destination.DefinitionId || source.RunLevel != destination.RunLevel ||
                destination.RunLevel >= RunUnitRules.MaximumRunLevel)
            {
                return -1;
            }

            // Slot order is authoritative. RunUnitId is stable across movement and is
            // retained for diagnostics, but two units can never occupy the same slot.
            for (var i = 0; i < slots.Length; i++)
            {
                if (i == sourceIndex || i == destinationIndex || slots[i] == null) continue;
                if (slots[i].DefinitionId == destination.DefinitionId && slots[i].RunLevel == destination.RunLevel)
                    return i;
            }
            return -1;
        }

        public bool TrySellAt(int index, out int refund)
        {
            refund = 0;
            if (index < 0 || index >= slots.Length || slots[index] == null) return false;
            refund = slots[index].AcquisitionBasis / 2;
            slots[index] = null;
            return true;
        }

        public void Clear()
        {
            Array.Clear(slots, 0, slots.Length);
        }

        private int FirstEmptyIndex()
        {
            for (var i = 0; i < slots.Length; i++) if (slots[i] == null) return i;
            return -1;
        }

        private List<int> MatchingIndices(string definitionId, int runLevel)
        {
            var result = new List<int>();
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null && slots[i].DefinitionId == definitionId && slots[i].RunLevel == runLevel)
                    result.Add(i);
            }
            return result;
        }

        private int PreferredPurchaseMergeIndex(IReadOnlyList<int> matches)
        {
            for (var i = 0; i < matches.Count; i++)
                if (matches[i] < BoardCapacity) return matches[i];
            return matches.Count == 0 ? -1 : matches[0];
        }

        private void ValidateIndex(int index)
        {
            if (index < 0 || index >= slots.Length) throw new ArgumentOutOfRangeException(nameof(index));
        }
    }

    public enum RunEconomyResultCode
    {
        Success = 0,
        InsufficientCoins = 1,
        RosterFull = 2,
        BackpackFullNeedsChoice = 3,
        MaximumRunLevel = 4,
        MergeRequirementsNotMet = 5,
        InvalidPhase = 6,
        OfferNotFoundOrSold = 7,
        UnitNotFound = 8,
        StaleVersion = 9,
        InvalidTarget = 10
    }

    public sealed class RunEconomyActionReceipt
    {
        public string ActionId { get; }
        public RunEconomyResultCode ResultCode { get; }
        public int ResultingVersion { get; }
        public string ResultDigest { get; }
        public bool IsReplay { get; }

        internal RunEconomyActionReceipt(
            string actionId,
            RunEconomyResultCode resultCode,
            int resultingVersion,
            string resultDigest,
            bool isReplay)
        {
            ActionId = actionId;
            ResultCode = resultCode;
            ResultingVersion = resultingVersion;
            ResultDigest = resultDigest;
            IsReplay = isReplay;
        }

        internal RunEconomyActionReceipt AsReplay()
        {
            return new RunEconomyActionReceipt(ActionId, ResultCode, ResultingVersion, ResultDigest, true);
        }
    }

    /// <summary>
    /// Run-scoped idempotency/version gate. Mutation callbacks must validate all
    /// input before changing state; only a Success receipt advances the version.
    /// </summary>
    public sealed class RunEconomyActionLedger
    {
        private readonly Dictionary<string, RunEconomyActionReceipt> receipts =
            new Dictionary<string, RunEconomyActionReceipt>(StringComparer.Ordinal);
        private readonly List<string> order = new List<string>();

        public int StateVersion { get; private set; }
        public int Count => order.Count;
        public string LastActionId { get; private set; } = string.Empty;
        public RunEconomyResultCode LastResultCode { get; private set; } = RunEconomyResultCode.Success;

        public RunEconomyActionReceipt Execute(
            string actionId,
            int expectedVersion,
            Func<RunEconomyResultCode> mutation)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("An action id is required.", nameof(actionId));
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));

            RunEconomyActionReceipt existing;
            if (receipts.TryGetValue(actionId, out existing)) return existing.AsReplay();

            var result = expectedVersion == StateVersion
                ? mutation()
                : RunEconomyResultCode.StaleVersion;
            if (result == RunEconomyResultCode.Success) StateVersion++;
            var digest = ((int)result) + ":" + StateVersion;
            var receipt = new RunEconomyActionReceipt(actionId, result, StateVersion, digest, false);
            receipts.Add(actionId, receipt);
            order.Add(actionId);
            LastActionId = actionId;
            LastResultCode = result;
            return receipt;
        }

        public void Restore(
            int stateVersion,
            IReadOnlyList<string> actionIds,
            IReadOnlyList<int> resultCodes,
            IReadOnlyList<int> resultingVersions)
        {
            receipts.Clear();
            order.Clear();
            StateVersion = Math.Max(0, stateVersion);
            LastActionId = string.Empty;
            LastResultCode = RunEconomyResultCode.Success;
            if (actionIds == null) return;
            for (var i = 0; i < actionIds.Count; i++)
            {
                var id = actionIds[i];
                if (string.IsNullOrWhiteSpace(id) || receipts.ContainsKey(id)) continue;
                var rawCode = resultCodes != null && i < resultCodes.Count ? resultCodes[i] : 0;
                var code = Enum.IsDefined(typeof(RunEconomyResultCode), rawCode)
                    ? (RunEconomyResultCode)rawCode
                    : RunEconomyResultCode.InvalidTarget;
                var version = resultingVersions != null && i < resultingVersions.Count
                    ? Math.Max(0, resultingVersions[i])
                    : StateVersion;
                var receipt = new RunEconomyActionReceipt(id, code, version,
                    ((int)code) + ":" + version, false);
                receipts.Add(id, receipt);
                order.Add(id);
                LastActionId = id;
                LastResultCode = code;
            }
        }

        public string[] CopyActionIds()
        {
            return order.ToArray();
        }

        public int[] CopyResultCodes()
        {
            var values = new int[order.Count];
            for (var i = 0; i < values.Length; i++) values[i] = (int)receipts[order[i]].ResultCode;
            return values;
        }

        public int[] CopyResultingVersions()
        {
            var values = new int[order.Count];
            for (var i = 0; i < values.Length; i++) values[i] = receipts[order[i]].ResultingVersion;
            return values;
        }

        public void Reset()
        {
            receipts.Clear();
            order.Clear();
            StateVersion = 0;
            LastActionId = string.Empty;
            LastResultCode = RunEconomyResultCode.Success;
        }
    }

    public sealed class RunDeckSnapshot
    {
        private readonly string[] unitDefinitionIds;
        private readonly string[] characterInstanceIds;
        public IReadOnlyList<string> UnitDefinitionIds => unitDefinitionIds;
        public IReadOnlyList<string> CharacterInstanceIds => characterInstanceIds;

        public RunDeckSnapshot(IEnumerable<string> unitDefinitionIds)
        {
            if (unitDefinitionIds == null) throw new ArgumentNullException(nameof(unitDefinitionIds));
            var copied = new List<string>();
            foreach (var id in unitDefinitionIds)
            {
                if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Deck entries require definition ids.", nameof(unitDefinitionIds));
                copied.Add(id);
            }
            if (copied.Count == 0) throw new ArgumentException("A run deck cannot be empty.", nameof(unitDefinitionIds));
            this.unitDefinitionIds = copied.ToArray();
            characterInstanceIds = new string[this.unitDefinitionIds.Length];
        }

        public RunDeckSnapshot(IEnumerable<OwnedCharacterState> characters)
        {
            if (characters == null) throw new ArgumentNullException(nameof(characters));
            var definitions = new List<string>();
            var instances = new List<string>();
            var uniqueDefinitions = new HashSet<string>();
            foreach (var character in characters)
            {
                if (character == null) throw new ArgumentException("Deck character instances cannot be null.", nameof(characters));
                if (!uniqueDefinitions.Add(character.DefinitionId))
                    throw new ArgumentException("A run deck cannot contain two instances of the same character definition.", nameof(characters));
                definitions.Add(character.DefinitionId);
                instances.Add(character.CharacterInstanceId);
            }
            if (definitions.Count == 0) throw new ArgumentException("A run deck cannot be empty.", nameof(characters));
            unitDefinitionIds = definitions.ToArray();
            characterInstanceIds = instances.ToArray();
        }

        public bool Contains(string definitionId)
        {
            foreach (var id in unitDefinitionIds) if (id == definitionId) return true;
            return false;
        }

        public string GetPrototypeOffer(int offerIndex)
        {
            if (offerIndex < 0) throw new ArgumentOutOfRangeException(nameof(offerIndex));
            return unitDefinitionIds[offerIndex % unitDefinitionIds.Length];
        }

        public string GetCharacterInstanceId(string definitionId)
        {
            for (var i = 0; i < unitDefinitionIds.Length; i++)
            {
                if (unitDefinitionIds[i] == definitionId) return characterInstanceIds[i];
            }
            return null;
        }
    }
}
