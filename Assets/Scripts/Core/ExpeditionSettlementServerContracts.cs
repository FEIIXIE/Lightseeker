using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class SubmitExpeditionServerProgress
    {
        public string RunId;
        public string IdempotencyKey;
        public string CompletedNodeId;
        public string CombatResultDigest;
        public string CombatProofReference;
    }

    [Serializable]
    public sealed class ExpeditionServerProgressCheckpoint
    {
        public string CheckpointId;
        public string RunId;
        public string AccountId;
        public string IdempotencyKey;
        public string CompletedNodeId;
        public int CompletedNodeOrdinal;
        public int ProgressRank;
        public string CombatResultDigest;
        public string RecordedAt;
        public string RequestId;
    }

    public interface IExpeditionProgressServerTransport
    {
        Task<ExpeditionServerProgressCheckpoint> SubmitProgressAsync(
            string accessToken,
            SubmitExpeditionServerProgress request,
            CancellationToken cancellationToken);

        Task<ExpeditionServerProgressCheckpoint> GetLatestProgressAsync(
            string accessToken,
            string runId,
            CancellationToken cancellationToken);
    }

    public sealed class ExpeditionProgressCoordinator
    {
        private readonly IExpeditionProgressServerTransport transport;
        private int operationInProgress;

        public ExpeditionProgressCoordinator(IExpeditionProgressServerTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public async Task<ExpeditionServerProgressCheckpoint> SubmitAsync(
            string accessToken,
            string expectedAccountId,
            SubmitExpeditionServerProgress request,
            CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "An expedition progress checkpoint submission is already in progress.");
            try
            {
                ValidateRequest(accessToken, expectedAccountId, request);
                var checkpoint = await transport.SubmitProgressAsync(
                    accessToken, request, cancellationToken);
                ValidateResponse(checkpoint, expectedAccountId, request.RunId,
                    request.IdempotencyKey);
                return checkpoint;
            }
            finally
            {
                Interlocked.Exchange(ref operationInProgress, 0);
            }
        }

        public async Task<ExpeditionServerProgressCheckpoint> RecoverLatestAsync(
            string accessToken,
            string expectedAccountId,
            string runId,
            CancellationToken cancellationToken)
        {
            Guid parsedAccountId;
            Guid parsedRunId;
            if (string.IsNullOrWhiteSpace(accessToken) ||
                !Guid.TryParse(expectedAccountId, out parsedAccountId) ||
                !Guid.TryParse(runId, out parsedRunId))
                throw new InvalidOperationException(
                    "An authenticated account and authoritative expedition run are required.");

            var checkpoint = await transport.GetLatestProgressAsync(
                accessToken, runId, cancellationToken);
            if (checkpoint == null) return null;
            ValidateRecoveredResponse(checkpoint, expectedAccountId, runId);
            return checkpoint;
        }

        private static void ValidateRequest(
            string accessToken,
            string expectedAccountId,
            SubmitExpeditionServerProgress request)
        {
            int nodeOrdinal;
            if (string.IsNullOrWhiteSpace(accessToken) ||
                !Guid.TryParse(expectedAccountId, out _) || request == null ||
                !Guid.TryParse(request.RunId, out _) ||
                string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
                request.IdempotencyKey.Length > 128 ||
                !TryGetNodeOrdinal(request.CompletedNodeId, out nodeOrdinal) ||
                nodeOrdinal > 28 ||
                !IsDigest(request.CombatResultDigest) ||
                string.IsNullOrWhiteSpace(request.CombatProofReference) ||
                request.CombatProofReference.Length > 256)
                throw new InvalidOperationException(
                    "The authoritative expedition progress request is invalid.");
        }

        private static void ValidateResponse(
            ExpeditionServerProgressCheckpoint checkpoint,
            string expectedAccountId,
            string expectedRunId,
            string expectedIdempotencyKey)
        {
            int nodeOrdinal;
            if (checkpoint == null || !Guid.TryParse(checkpoint.CheckpointId, out _) ||
                !string.Equals(checkpoint.RunId, expectedRunId, StringComparison.Ordinal) ||
                !string.Equals(checkpoint.AccountId, expectedAccountId, StringComparison.Ordinal) ||
                !string.Equals(checkpoint.IdempotencyKey, expectedIdempotencyKey,
                    StringComparison.Ordinal) ||
                !TryGetNodeOrdinal(checkpoint.CompletedNodeId, out nodeOrdinal) ||
                nodeOrdinal != checkpoint.CompletedNodeOrdinal || nodeOrdinal > 28 ||
                checkpoint.ProgressRank < 0 || checkpoint.ProgressRank > 13 ||
                !IsDigest(checkpoint.CombatResultDigest) ||
                string.IsNullOrWhiteSpace(checkpoint.RecordedAt) ||
                string.IsNullOrWhiteSpace(checkpoint.RequestId))
                throw new InvalidOperationException(
                    "The authoritative expedition progress response is invalid.");
        }

        private static void ValidateRecoveredResponse(
            ExpeditionServerProgressCheckpoint checkpoint,
            string expectedAccountId,
            string expectedRunId)
        {
            int nodeOrdinal;
            if (checkpoint == null || !Guid.TryParse(checkpoint.CheckpointId, out _) ||
                !string.Equals(checkpoint.RunId, expectedRunId, StringComparison.Ordinal) ||
                !string.Equals(checkpoint.AccountId, expectedAccountId, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(checkpoint.IdempotencyKey) ||
                checkpoint.IdempotencyKey.Length > 128 ||
                !TryGetNodeOrdinal(checkpoint.CompletedNodeId, out nodeOrdinal) ||
                nodeOrdinal != checkpoint.CompletedNodeOrdinal || nodeOrdinal > 28 ||
                checkpoint.ProgressRank < 0 || checkpoint.ProgressRank > 13 ||
                !IsDigest(checkpoint.CombatResultDigest) ||
                string.IsNullOrWhiteSpace(checkpoint.RecordedAt) ||
                string.IsNullOrWhiteSpace(checkpoint.RequestId))
                throw new InvalidOperationException(
                    "The recovered authoritative expedition progress response is invalid.");
        }

        private static bool TryGetNodeOrdinal(string value, out int ordinal)
        {
            ordinal = -1;
            if (value == null || value.Length != 3 || value[0] != 'M' ||
                value[1] < '0' || value[1] > '9' || value[2] < '0' || value[2] > '9')
                return false;
            ordinal = (value[1] - '0') * 10 + value[2] - '0';
            return ordinal <= 29;
        }

        private static bool IsDigest(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 16 || value.Length > 128)
                return false;
            foreach (var character in value)
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= 'a' && character <= 'z')) &&
                    character != '-' && character != '_')
                    return false;
            return true;
        }
    }

    [Serializable]
    public sealed class SubmitExpeditionServerSettlement
    {
        public string RunId;
        public string IdempotencyKey;
        public string TerminalReason;
        public string TerminalNodeId;
        public string CombatResultDigest;
        public string CombatProofReference;
    }

    [Serializable]
    public sealed class ExpeditionServerSettlementPlan
    {
        public string DifficultyId;
        public string TerminalReason;
        public int ProgressRank;
        public int TideCrystal;
        public int LampGlow;
        public int TideIron;
        public int MistLeather;
        public int StarSand;
        public int BountyTickets;
        public int EquipmentQuantity;
        public string[] EquipmentRarityIds;
        public int LanternQuantity;
        public string LanternRarityId;
        public bool IsFirstClearLantern;
        public bool GrantsFirstClear;
        public bool UnlocksNextDifficulty;
        public bool RequiresEquipmentInstanceGeneration;
        public bool RequiresLanternInstanceGeneration;
        public string RewardTableVersion;
    }

    [Serializable]
    public sealed class ExpeditionServerRewardLedgerLine
    {
        public string LedgerEntryId;
        public string RewardKind;
        public string RewardCode;
        public int Quantity;
        public int Ordinal;
        public string PayloadJson;
    }

    [Serializable]
    public sealed class ExpeditionServerSettlementReceipt
    {
        public string SettlementId;
        public string RunId;
        public string AccountId;
        public string IdempotencyKey;
        public ExpeditionServerSettlementPlan Plan;
        public string SeedRevealHex;
        public ExpeditionServerRewardLedgerLine[] Ledger;
        public string RewardManifestHash;
        public bool RequiresWalletClaim;
        public string ClaimedAt;
        public string CreatedAt;
        public string RequestId;
    }

    [Serializable]
    public sealed class ExpeditionSettlementClaimChallenge
    {
        public string ChallengeId;
        public string SettlementId;
        public string SignaturePurpose;
        public string CanonicalMessage;
        public string RewardManifestHash;
        public string IssuedAt;
        public string ExpiresAt;
        public string RequestId;
    }

    [Serializable]
    public sealed class ClaimExpeditionServerSettlement
    {
        public string SettlementId;
        public string ChallengeId;
        public string IdempotencyKey;
        public string Address;
        public string SignedMessage;
        public string Signature;
    }

    [Serializable]
    public sealed class ExpeditionSettlementClaimReceipt
    {
        public string SettlementId;
        public string AccountId;
        public string RewardManifestHash;
        public string IdempotencyKey;
        public string ClaimedAt;
        public bool WasAlreadyClaimed;
        public string RequestId;
    }

    [Serializable]
    public sealed class ExpeditionServerRewardProjectionStatus
    {
        public string AccountId;
        public int AvailableBountyTickets;
        public int PendingInstanceGenerationItems;
        public int ProjectedLedgerEntries;
        public int PendingLedgerEntries;
        public string RequestId;
    }

    [Serializable]
    public sealed class ExpeditionServerRewardRecovery
    {
        public int ExecutedBatches;
        public int ProcessedLedgerEntries;
        public bool CaughtUp;
        public ExpeditionServerRewardProjectionStatus Snapshot;
        public string RequestId;
    }

    public sealed class ExpeditionServerSettlementRecoveryResult
    {
        public ExpeditionServerSettlementReceipt Settlement;
        public ExpeditionServerRewardRecovery Projection;
        public AuthoritativeRewardInventorySnapshot Inventory;
    }

    public interface IExpeditionSettlementServerTransport
    {
        Task<ExpeditionServerSettlementReceipt> SubmitSettlementAsync(
            string accessToken,
            SubmitExpeditionServerSettlement request,
            CancellationToken cancellationToken);
        Task<ExpeditionServerSettlementReceipt> GetSettlementAsync(
            string accessToken,
            string runId,
            CancellationToken cancellationToken);
        Task<ExpeditionServerRewardRecovery> RecoverRewardsAsync(
            string accessToken,
            CancellationToken cancellationToken);
        Task<ExpeditionSettlementClaimChallenge> CreateClaimChallengeAsync(
            string accessToken,
            string settlementId,
            CancellationToken cancellationToken);
        Task<ExpeditionSettlementClaimReceipt> ClaimSettlementAsync(
            string accessToken,
            ClaimExpeditionServerSettlement request,
            CancellationToken cancellationToken);
    }

    public sealed class ExpeditionSettlementCoordinator
    {
        private static readonly HashSet<string> TerminalReasons =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "BossCleared",
                "PlayerAbandoned",
                "ExpeditionLifeZero",
                "NormalNonBossTerminal"
            };
        private const string ExpiredTerminalReason = "ExpiredAtUtcDayBoundary";

        private readonly IExpeditionSettlementServerTransport settlementTransport;
        private readonly RewardInstanceServerCoordinator rewardInstances;
        private int operationInProgress;

        public ExpeditionSettlementCoordinator(
            IExpeditionSettlementServerTransport settlementTransport,
            RewardInstanceServerCoordinator rewardInstances)
        {
            this.settlementTransport = settlementTransport ??
                throw new ArgumentNullException(nameof(settlementTransport));
            this.rewardInstances = rewardInstances ??
                throw new ArgumentNullException(nameof(rewardInstances));
        }

        public async Task<ExpeditionServerSettlementRecoveryResult> SubmitAndRecoverAsync(
            string accessToken,
            string expectedAccountId,
            SubmitExpeditionServerSettlement request,
            CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "An expedition settlement recovery is already in progress.");
            try
            {
                ValidateRequest(accessToken, expectedAccountId, request);
                var settlement = await settlementTransport.SubmitSettlementAsync(
                    accessToken, request, cancellationToken);
                ValidateSettlement(settlement, expectedAccountId, request.RunId,
                    request.IdempotencyKey);
                if (settlement.RequiresWalletClaim)
                    return new ExpeditionServerSettlementRecoveryResult
                    {
                        Settlement = settlement
                    };
                var projection = await settlementTransport.RecoverRewardsAsync(
                    accessToken, cancellationToken);
                ValidateProjection(projection, expectedAccountId);
                var inventory = await rewardInstances.SynchronizeAsync(
                    accessToken, true, cancellationToken);
                if (!string.Equals(inventory.AccountId, expectedAccountId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The authoritative inventory account does not match the settlement account.");
                return new ExpeditionServerSettlementRecoveryResult
                {
                    Settlement = settlement,
                    Projection = projection,
                    Inventory = inventory
                };
            }
            finally
            {
                Interlocked.Exchange(ref operationInProgress, 0);
            }
        }

        public async Task<ExpeditionServerSettlementRecoveryResult> ClaimAndRecoverAsync(
            string accessToken,
            string expectedAccountId,
            string expectedWalletAddress,
            ExpeditionServerSettlementReceipt settlement,
            string idempotencyKey,
            IWalletSiwsGateway wallet,
            CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "An expedition settlement recovery is already in progress.");
            try
            {
                if (string.IsNullOrWhiteSpace(accessToken) ||
                    !Guid.TryParse(expectedAccountId, out _) ||
                    string.IsNullOrWhiteSpace(expectedWalletAddress) ||
                    settlement == null || !Guid.TryParse(settlement.SettlementId, out _) ||
                    !settlement.RequiresWalletClaim ||
                    string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128 ||
                    wallet == null)
                    throw new InvalidOperationException(
                        "A pending authoritative settlement and wallet are required.");

                var challenge = await settlementTransport.CreateClaimChallengeAsync(
                    accessToken, settlement.SettlementId, cancellationToken);
                if (challenge == null || !Guid.TryParse(challenge.ChallengeId, out _) ||
                    !string.Equals(challenge.SettlementId, settlement.SettlementId,
                        StringComparison.Ordinal) ||
                    !string.Equals(challenge.SignaturePurpose,
                        "EXPEDITION_RETURN_SETTLEMENT", StringComparison.Ordinal) ||
                    !string.Equals(challenge.RewardManifestHash,
                        settlement.RewardManifestHash, StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(challenge.CanonicalMessage))
                    throw new InvalidOperationException(
                        "The expedition settlement claim challenge is invalid.");

                var identity = await wallet.ConnectAsync(cancellationToken);
                if (identity == null || !string.Equals(identity.WalletAddress,
                        expectedWalletAddress, StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The connected wallet does not match the settlement account.");
                var signature = await wallet.SignMessageAsync(
                    Encoding.UTF8.GetBytes(challenge.CanonicalMessage), cancellationToken);
                if (signature == null || signature.Length == 0)
                    throw new InvalidOperationException(
                        "The expedition settlement claim signature was not returned.");

                var claim = await settlementTransport.ClaimSettlementAsync(
                    accessToken,
                    new ClaimExpeditionServerSettlement
                    {
                        SettlementId = settlement.SettlementId,
                        ChallengeId = challenge.ChallengeId,
                        IdempotencyKey = idempotencyKey,
                        Address = identity.WalletAddress,
                        SignedMessage = challenge.CanonicalMessage,
                        Signature = Convert.ToBase64String(signature)
                    },
                    cancellationToken);
                if (claim == null ||
                    !string.Equals(claim.SettlementId, settlement.SettlementId,
                        StringComparison.Ordinal) ||
                    !string.Equals(claim.AccountId, expectedAccountId,
                        StringComparison.Ordinal) ||
                    !string.Equals(claim.RewardManifestHash, settlement.RewardManifestHash,
                        StringComparison.Ordinal) ||
                    !string.Equals(claim.IdempotencyKey, idempotencyKey,
                        StringComparison.Ordinal) ||
                    string.IsNullOrWhiteSpace(claim.ClaimedAt))
                    throw new InvalidOperationException(
                        "The expedition settlement claim receipt is invalid.");

                settlement.RequiresWalletClaim = false;
                settlement.ClaimedAt = claim.ClaimedAt;
                var projection = await settlementTransport.RecoverRewardsAsync(
                    accessToken, cancellationToken);
                ValidateProjection(projection, expectedAccountId);
                var inventory = await rewardInstances.SynchronizeAsync(
                    accessToken, true, cancellationToken);
                if (!string.Equals(inventory.AccountId, expectedAccountId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The authoritative inventory account does not match the settlement account.");
                return new ExpeditionServerSettlementRecoveryResult
                {
                    Settlement = settlement,
                    Projection = projection,
                    Inventory = inventory
                };
            }
            finally
            {
                Interlocked.Exchange(ref operationInProgress, 0);
            }
        }

        public async Task<ExpeditionServerSettlementReceipt> RecoverSettlementAsync(
            string accessToken,
            string expectedAccountId,
            string runId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) ||
                !Guid.TryParse(expectedAccountId, out _) || !Guid.TryParse(runId, out _))
                throw new InvalidOperationException(
                    "An authenticated account and authoritative run are required.");
            var settlement = await settlementTransport.GetSettlementAsync(
                accessToken, runId, cancellationToken);
            ValidateSettlement(settlement, expectedAccountId, runId, null);
            return settlement;
        }

        private static void ValidateRequest(string accessToken, string expectedAccountId,
            SubmitExpeditionServerSettlement request)
        {
            if (string.IsNullOrWhiteSpace(accessToken) ||
                !Guid.TryParse(expectedAccountId, out _) || request == null ||
                !Guid.TryParse(request.RunId, out _) ||
                string.IsNullOrWhiteSpace(request.IdempotencyKey) ||
                request.IdempotencyKey.Length > 128 ||
                !TerminalReasons.Contains(request.TerminalReason) ||
                !IsTerminalNode(request.TerminalNodeId) ||
                !IsDigest(request.CombatResultDigest) ||
                string.IsNullOrWhiteSpace(request.CombatProofReference) ||
                request.CombatProofReference.Length > 256)
                throw new InvalidOperationException(
                    "The authoritative expedition settlement request is invalid.");
            if (request.TerminalReason == "BossCleared" && request.TerminalNodeId != "M29")
                throw new InvalidOperationException(
                    "A boss-clear settlement must terminate at M29.");
            if (request.TerminalReason != "BossCleared" && request.TerminalNodeId == "M29")
                throw new InvalidOperationException(
                    "M29 may only be settled as a boss clear.");
        }

        private static void ValidateSettlement(ExpeditionServerSettlementReceipt receipt,
            string expectedAccountId, string expectedRunId, string expectedIdempotencyKey)
        {
            if (receipt == null || !Guid.TryParse(receipt.SettlementId, out _) ||
                !string.Equals(receipt.RunId, expectedRunId, StringComparison.Ordinal) ||
                !string.Equals(receipt.AccountId, expectedAccountId, StringComparison.Ordinal) ||
                (expectedIdempotencyKey != null &&
                 (receipt.Plan == null ||
                  !string.Equals(receipt.Plan.TerminalReason, ExpiredTerminalReason,
                      StringComparison.Ordinal)) &&
                 !string.Equals(receipt.IdempotencyKey, expectedIdempotencyKey,
                      StringComparison.Ordinal)) ||
                receipt.Plan == null || receipt.Plan.ProgressRank < 0 ||
                receipt.Plan.ProgressRank > 26 ||
                string.IsNullOrWhiteSpace(receipt.Plan.RewardTableVersion) ||
                !IsLowerSha256(receipt.SeedRevealHex) ||
                !IsLowerSha256(receipt.RewardManifestHash) ||
                (receipt.RequiresWalletClaim &&
                 !string.IsNullOrWhiteSpace(receipt.ClaimedAt)) ||
                string.IsNullOrWhiteSpace(receipt.RequestId))
                throw new InvalidOperationException(
                    "The authoritative expedition settlement response is invalid.");

            if (string.Equals(receipt.Plan.TerminalReason, ExpiredTerminalReason,
                    StringComparison.Ordinal) &&
                (receipt.Plan.BountyTickets != 0 || receipt.Plan.EquipmentQuantity != 0 ||
                 receipt.Plan.LanternQuantity != 0 || receipt.Plan.GrantsFirstClear ||
                 receipt.Plan.UnlocksNextDifficulty ||
                 receipt.Plan.RequiresEquipmentInstanceGeneration ||
                 receipt.Plan.RequiresLanternInstanceGeneration))
                throw new InvalidOperationException(
                    "An expired expedition may not grant terminal-only rewards.");

            var ledgerIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var line in receipt.Ledger ?? new ExpeditionServerRewardLedgerLine[0])
            {
                if (line == null || !Guid.TryParse(line.LedgerEntryId, out _) ||
                    string.IsNullOrWhiteSpace(line.RewardKind) ||
                    string.IsNullOrWhiteSpace(line.RewardCode) || line.Quantity < 0 ||
                    line.Ordinal < 0 || !ledgerIds.Add(line.LedgerEntryId))
                    throw new InvalidOperationException(
                        "The authoritative settlement ledger is invalid.");
            }
        }

        private static void ValidateProjection(ExpeditionServerRewardRecovery recovery,
            string expectedAccountId)
        {
            if (recovery == null || recovery.ExecutedBatches < 1 ||
                recovery.ExecutedBatches > 10 || recovery.ProcessedLedgerEntries < 0 ||
                recovery.ProcessedLedgerEntries > 1000 || recovery.Snapshot == null ||
                !string.Equals(recovery.Snapshot.AccountId, expectedAccountId,
                    StringComparison.Ordinal) ||
                recovery.Snapshot.PendingInstanceGenerationItems < 0 ||
                recovery.Snapshot.ProjectedLedgerEntries < 0 ||
                recovery.Snapshot.PendingLedgerEntries < 0 ||
                string.IsNullOrWhiteSpace(recovery.RequestId))
                throw new InvalidOperationException(
                    "The authoritative reward projection response is invalid.");
        }

        private static bool IsTerminalNode(string value)
        {
            if (value == null || value.Length != 3 || value[0] != 'M' ||
                value[1] < '0' || value[1] > '2' ||
                value[2] < '0' || value[2] > '9') return false;
            return true;
        }

        private static bool IsDigest(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 16 || value.Length > 128)
                return false;
            foreach (var character in value)
                if (!((character >= '0' && character <= '9') ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= 'a' && character <= 'z')) &&
                    character != '-' && character != '_')
                    return false;
            return true;
        }

        private static bool IsLowerSha256(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length != 64) return false;
            foreach (var character in value)
                if (!(character >= '0' && character <= '9') &&
                    !(character >= 'a' && character <= 'f')) return false;
            return true;
        }
    }
}
