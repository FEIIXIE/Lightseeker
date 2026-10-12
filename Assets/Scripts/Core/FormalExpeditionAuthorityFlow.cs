using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    public sealed class FormalExpeditionCheckpointCommitResult
    {
        public ExpeditionServerReplayProof Proof { get; set; }
        public ExpeditionServerProgressCheckpoint Checkpoint { get; set; }
    }

    public sealed class FormalExpeditionTerminalCommitResult
    {
        public ExpeditionServerReplayProof Proof { get; set; }
        public ExpeditionServerSettlementRecoveryResult Recovery { get; set; }
    }

    /// <summary>
    /// The only client-side orchestration path from a canonical action transcript to
    /// an authoritative progress checkpoint or terminal settlement. It never accepts
    /// a caller-supplied result digest or proof reference: those values must come from
    /// the isolated replay authority and are immediately forwarded to the player API.
    /// </summary>
    public sealed class FormalExpeditionAuthorityFlowCoordinator
    {
        private readonly ExpeditionReplayProofCoordinator proofs;
        private readonly ExpeditionProgressCoordinator progress;
        private readonly ExpeditionSettlementCoordinator settlements;
        private int operationInProgress;

        public FormalExpeditionAuthorityFlowCoordinator(
            ExpeditionReplayProofCoordinator proofs,
            ExpeditionProgressCoordinator progress,
            ExpeditionSettlementCoordinator settlements)
        {
            this.proofs = proofs ?? throw new ArgumentNullException(nameof(proofs));
            this.progress = progress ?? throw new ArgumentNullException(nameof(progress));
            this.settlements = settlements ?? throw new ArgumentNullException(nameof(settlements));
        }

        public async Task<FormalExpeditionCheckpointCommitResult> CommitCheckpointAsync(
            string accessToken,
            string expectedAccountId,
            string seasonId,
            string idempotencyKey,
            string completedNodeId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken)
        {
            Begin();
            try
            {
                ValidateCommon(expectedAccountId, idempotencyKey, completedNodeId,
                    envelope, allowBossNode: false);
                var proof = await proofs.IssueCheckpointAsync(
                    accessToken, expectedAccountId, seasonId, envelope,
                    cancellationToken);
                var checkpoint = await progress.SubmitAsync(
                    accessToken,
                    expectedAccountId,
                    new SubmitExpeditionServerProgress
                    {
                        RunId = envelope.RunId,
                        IdempotencyKey = idempotencyKey,
                        CompletedNodeId = completedNodeId,
                        CombatResultDigest = proof.CombatResultDigest,
                        CombatProofReference = proof.CombatProofReference
                    },
                    cancellationToken);
                if (!string.Equals(checkpoint.CompletedNodeId, completedNodeId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The committed expedition checkpoint changed the completed node.");
                return new FormalExpeditionCheckpointCommitResult
                {
                    Proof = proof,
                    Checkpoint = checkpoint
                };
            }
            finally
            {
                End();
            }
        }

        public async Task<FormalExpeditionTerminalCommitResult> CommitTerminalAsync(
            string accessToken,
            string expectedAccountId,
            string seasonId,
            string idempotencyKey,
            string terminalReason,
            string terminalNodeId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken)
        {
            Begin();
            try
            {
                ValidateCommon(expectedAccountId, idempotencyKey, terminalNodeId,
                    envelope, allowBossNode: true);
                ValidateTerminalReason(terminalReason, terminalNodeId);
                var proof = await proofs.IssueTerminalAsync(
                    accessToken, expectedAccountId, seasonId, envelope,
                    cancellationToken);
                var recovery = await settlements.SubmitAndRecoverAsync(
                    accessToken,
                    expectedAccountId,
                    new SubmitExpeditionServerSettlement
                    {
                        RunId = envelope.RunId,
                        IdempotencyKey = idempotencyKey,
                        TerminalReason = terminalReason,
                        TerminalNodeId = terminalNodeId,
                        CombatResultDigest = proof.CombatResultDigest,
                        CombatProofReference = proof.CombatProofReference
                    },
                    cancellationToken);
                if (recovery == null || recovery.Settlement == null ||
                    !string.Equals(recovery.Settlement.RunId, envelope.RunId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The authoritative expedition settlement recovery is incomplete.");
                return new FormalExpeditionTerminalCommitResult
                {
                    Proof = proof,
                    Recovery = recovery
                };
            }
            finally
            {
                End();
            }
        }

        private void Begin()
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "An authoritative expedition commit is already in progress.");
        }

        private void End()
        {
            Interlocked.Exchange(ref operationInProgress, 0);
        }

        private static void ValidateCommon(
            string expectedAccountId,
            string idempotencyKey,
            string expectedNodeId,
            ExpeditionReplayEnvelopeRecord envelope,
            bool allowBossNode)
        {
            Guid accountId;
            Guid envelopeAccountId;
            if (!Guid.TryParse(expectedAccountId, out accountId) || envelope == null ||
                !Guid.TryParse(envelope.AccountId, out envelopeAccountId) ||
                accountId != envelopeAccountId ||
                string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128 ||
                !TryGetNodeOrdinal(expectedNodeId, out var nodeOrdinal) ||
                (!allowBossNode && nodeOrdinal > 28))
                throw new InvalidOperationException(
                    "The authoritative expedition commit binding is invalid.");

            ExpeditionReplayClientContract.Validate(envelope);
            var finalAction = envelope.Actions[envelope.Actions.Length - 1];
            if (!string.Equals(finalAction.Kind, "battle.resolve", StringComparison.Ordinal) ||
                !string.Equals(finalAction.NodeId, expectedNodeId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The authoritative expedition commit node does not match the replay transcript.");
        }

        private static void ValidateTerminalReason(string reason, string nodeId)
        {
            if (reason != "BossCleared" && reason != "PlayerAbandoned" &&
                reason != "ExpeditionLifeZero" && reason != "NormalNonBossTerminal")
                throw new InvalidOperationException(
                    "The authoritative expedition terminal reason is invalid.");
            if ((reason == "BossCleared") != string.Equals(nodeId, "M29",
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The authoritative expedition terminal reason does not match its node.");
        }

        private static bool TryGetNodeOrdinal(string value, out int ordinal)
        {
            ordinal = -1;
            if (value == null || value.Length != 3 || value[0] != 'M' ||
                value[1] < '0' || value[1] > '2' ||
                value[2] < '0' || value[2] > '9') return false;
            ordinal = (value[1] - '0') * 10 + value[2] - '0';
            return ordinal <= 29;
        }
    }
}
