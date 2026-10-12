using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class ExpeditionReplayRecorderState
    {
        public string ContractVersion;
        public string RunId;
        public string AccountId;
        public string CombatInputContractVersion;
        public string CombatInputHash;
        public string SeedCommitment;
        public ExpeditionReplayActionRecord[] Actions;
    }

    /// <summary>
    /// Records only actions that are bound to a validated server-authoritative run.
    /// It never decides combat results, issues proofs, or mutates rewards. A checkpoint
    /// can only be exported after a battle.resolve action closes the transcript.
    /// </summary>
    public sealed class ExpeditionReplayRecorder
    {
        private readonly string runId;
        private readonly string accountId;
        private readonly string combatInputContractVersion;
        private readonly string combatInputHash;
        private readonly string seedCommitment;
        private readonly List<ExpeditionReplayActionRecord> actions =
            new List<ExpeditionReplayActionRecord>();
        private readonly HashSet<string> actionIds =
            new HashSet<string>(StringComparer.Ordinal);

        private ExpeditionReplayRecorder(AuthoritativeExpeditionRunBinding binding)
        {
            if (binding == null || binding.Source == null)
                throw new ArgumentNullException(nameof(binding));
            var source = binding.Source;
            Guid parsedRunId;
            Guid parsedAccountId;
            if (!Guid.TryParse(source.RunId, out parsedRunId) ||
                !Guid.TryParse(source.AccountId, out parsedAccountId) ||
                source.CombatInputContractVersion !=
                    ExpeditionReplayClientContract.CombatInputVersion ||
                !FormalExpeditionSnapshotContract.IsSha256Hex(source.CombatInputHash) ||
                !FormalExpeditionSnapshotContract.IsSha256Hex(source.SeedCommitment))
                throw new InvalidOperationException(
                    "The authoritative expedition replay binding is invalid.");

            runId = parsedRunId.ToString("N");
            accountId = parsedAccountId.ToString("N");
            combatInputContractVersion = source.CombatInputContractVersion;
            combatInputHash = source.CombatInputHash;
            seedCommitment = source.SeedCommitment;
        }

        public int ActionCount
        {
            get { return actions.Count; }
        }

        public int LastStateVersion
        {
            get { return actions.Count == 0 ? -1 : actions[actions.Count - 1].ExpectedStateVersion; }
        }

        public static ExpeditionReplayRecorder Start(
            AuthoritativeExpeditionRunBinding binding)
        {
            return new ExpeditionReplayRecorder(binding);
        }

        public static ExpeditionReplayRecorder Restore(
            AuthoritativeExpeditionRunBinding binding,
            ExpeditionReplayRecorderState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var recorder = new ExpeditionReplayRecorder(binding);
            if (state.ContractVersion != ExpeditionReplayClientContract.Version ||
                state.RunId != recorder.runId || state.AccountId != recorder.accountId ||
                state.CombatInputContractVersion != recorder.combatInputContractVersion ||
                state.CombatInputHash != recorder.combatInputHash ||
                state.SeedCommitment != recorder.seedCommitment || state.Actions == null ||
                state.Actions.Length > ExpeditionReplayClientContract.MaximumActionCount)
                throw new InvalidOperationException(
                    "The saved expedition replay is not bound to this authoritative run.");

            for (var index = 0; index < state.Actions.Length; index++)
            {
                var source = state.Actions[index];
                if (source == null || source.Sequence != index)
                    throw new InvalidOperationException(
                        "The saved expedition replay action ordering is invalid.");
                recorder.AppendCanonical(source.ActionId, source.Kind,
                    source.ExpectedStateVersion, source.NodeId, source.PayloadCanonical,
                    source.PayloadSha256);
            }
            return recorder;
        }

        public ExpeditionReplayActionRecord Record(
            string actionId,
            string kind,
            int expectedStateVersion,
            string nodeId,
            IDictionary<string, string> fields)
        {
            var payload = ExpeditionReplayClientContract.BuildPayload(kind, fields);
            return AppendCanonical(actionId, kind, expectedStateVersion, nodeId,
                payload, ExpeditionReplayClientContract.ComputePayloadHash(payload));
        }

        public ExpeditionReplayRecorderState CaptureState()
        {
            return new ExpeditionReplayRecorderState
            {
                ContractVersion = ExpeditionReplayClientContract.Version,
                RunId = runId,
                AccountId = accountId,
                CombatInputContractVersion = combatInputContractVersion,
                CombatInputHash = combatInputHash,
                SeedCommitment = seedCommitment,
                Actions = CopyActions()
            };
        }

        public ExpeditionReplayEnvelopeRecord CreateCheckpointEnvelope()
        {
            return ExpeditionReplayClientContract.CreateEnvelope(
                runId, accountId, combatInputHash, seedCommitment, CopyActions());
        }

        private ExpeditionReplayActionRecord AppendCanonical(
            string actionId,
            string kind,
            int expectedStateVersion,
            string nodeId,
            string payloadCanonical,
            string expectedPayloadHash)
        {
            if (actions.Count >= ExpeditionReplayClientContract.MaximumActionCount)
                throw new InvalidOperationException(
                    "The expedition replay action limit has been reached.");
            if (string.IsNullOrWhiteSpace(actionId) || !actionIds.Add(actionId))
                throw new InvalidOperationException(
                    "The expedition replay action id is missing or duplicated.");
            if (expectedStateVersion < 0 || expectedStateVersion < LastStateVersion)
            {
                actionIds.Remove(actionId);
                throw new InvalidOperationException(
                    "The expedition replay state version cannot move backwards.");
            }

            ExpeditionReplayActionRecord action;
            try
            {
                action = ExpeditionReplayClientContract.CreateAction(
                    actions.Count, actionId, kind, expectedStateVersion, nodeId,
                    payloadCanonical);
                if (!string.Equals(action.PayloadSha256, expectedPayloadHash,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The expedition replay action payload has been altered.");
            }
            catch
            {
                actionIds.Remove(actionId);
                throw;
            }
            actions.Add(action);
            return CopyAction(action);
        }

        private ExpeditionReplayActionRecord[] CopyActions()
        {
            var copied = new ExpeditionReplayActionRecord[actions.Count];
            for (var index = 0; index < actions.Count; index++)
                copied[index] = CopyAction(actions[index]);
            return copied;
        }

        private static ExpeditionReplayActionRecord CopyAction(
            ExpeditionReplayActionRecord source)
        {
            return new ExpeditionReplayActionRecord
            {
                Sequence = source.Sequence,
                ActionId = source.ActionId,
                Kind = source.Kind,
                ExpectedStateVersion = source.ExpectedStateVersion,
                NodeId = source.NodeId,
                PayloadCanonical = source.PayloadCanonical,
                PayloadSha256 = source.PayloadSha256
            };
        }
    }
}
