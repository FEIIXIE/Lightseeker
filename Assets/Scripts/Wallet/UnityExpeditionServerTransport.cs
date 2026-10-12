using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace SeekerAutoChess.Wallet
{
    public sealed class UnityExpeditionServerTransport : IExpeditionServerTransport,
        IExpeditionSettlementServerTransport, IExpeditionProgressServerTransport,
        IExpeditionReplayProofServerTransport
    {
        private const int MaximumResponseBytes = 128 * 1024;
        private readonly Uri baseUri;
        private readonly int timeoutSeconds;

        public UnityExpeditionServerTransport(string baseUrl,
            bool allowInsecureLocalDevelopment, int timeoutSeconds = 10)
        {
            Uri parsed;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException("The expedition server base URL must be absolute.", "baseUrl");
            if (!IsAllowedEndpoint(parsed, allowInsecureLocalDevelopment))
                throw new ArgumentException(
                    "The expedition server must use HTTPS. HTTP is only allowed explicitly for a local development host.",
                    "baseUrl");
            if (timeoutSeconds < 1 || timeoutSeconds > 60)
                throw new ArgumentOutOfRangeException("timeoutSeconds");
            baseUri = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<ExpeditionServerSelection> GetSelectionAsync(string accessToken,
            string seasonId, CancellationToken cancellationToken)
        {
            var response = await SendAsync<SelectionResponse>(UnityWebRequest.kHttpVerbGET,
                "v1/expeditions/chapter01/selection?seasonId=" + UnityWebRequest.EscapeURL(seasonId),
                null, accessToken, cancellationToken);
            var source = response.difficulties ?? new DifficultyResponse[0];
            var difficulties = new ExpeditionServerDifficulty[source.Length];
            for (var index = 0; index < source.Length; index++)
            {
                var item = source[index] ?? new DifficultyResponse();
                difficulties[index] = new ExpeditionServerDifficulty
                {
                    DifficultyId = item.difficultyId,
                    DisplayNameKey = item.displayNameKey,
                    EntryPriceMicroSkr = item.entryPriceMicroSkr,
                    RequiresBossClearDifficultyId = item.requiresBossClearDifficultyId,
                    IsUnlocked = item.isUnlocked,
                    CanCreateIntent = item.canCreateIntent,
                    BlockingCode = item.blockingCode
                };
            }
            return new ExpeditionServerSelection
            {
                ChapterId = response.chapterId,
                SeasonId = response.seasonId,
                QuotaDayId = response.quotaDayId,
                DailyLimit = response.dailyLimit,
                UsedSlots = response.usedSlots,
                RemainingSlots = response.remainingSlots,
                FirstClearLanternEligible = response.firstClearLanternEligible,
                ActiveRunId = response.activeRunId,
                Difficulties = difficulties,
                AuthoritativeTimeZone = response.authoritativeTimeZone,
                QuotaDayStartAtUtc = response.quotaDayStartAtUtc,
                QuotaDayEndAtUtc = response.quotaDayEndAtUtc,
                FormalCreationCloseAtUtc = response.formalCreationCloseAtUtc,
                TimeRulesVersion = response.timeRulesVersion
            };
        }

        public async Task<ExpeditionServerPaymentTerms> GetPaymentTermsAsync(
            string accessToken, CancellationToken cancellationToken)
        {
            var response = await SendAsync<PaymentTermsResponse>(UnityWebRequest.kHttpVerbGET,
                "v1/expeditions/chapter01/payment-terms", null, accessToken, cancellationToken);
            return new ExpeditionServerPaymentTerms
            {
                NetworkId = response.networkId,
                TokenMintAddress = response.tokenMintAddress,
                TreasuryWalletAddress = response.treasuryWalletAddress,
                TokenDecimals = response.tokenDecimals,
                IntentMemoPrefix = response.intentMemoPrefix
            };
        }

        public async Task<ExpeditionServerEntryIntent> CreateIntentAsync(string accessToken,
            CreateExpeditionServerIntent request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException("request");
            var response = await SendAsync<IntentResponse>(UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/intents",
                JsonUtility.ToJson(new CreateIntentRequest
                {
                    chapterId = request.ChapterId,
                    seasonId = request.SeasonId,
                    difficultyId = request.DifficultyId,
                    idempotencyKey = request.IdempotencyKey,
                    deckSnapshotHash = request.DeckSnapshotHash,
                    equipmentSnapshotHash = request.EquipmentSnapshotHash,
                    lanternSnapshotHash = request.LanternSnapshotHash,
                    firstClearLanternEligibilityKeyHash = request.FirstClearLanternEligibilityKeyHash,
                    combatInputContractVersion = request.CombatInputContractVersion,
                    combatInputHash = request.CombatInputHash,
                    deckSnapshotCanonical = request.DeckSnapshotCanonical,
                    equipmentSnapshotCanonical = request.EquipmentSnapshotCanonical,
                    lanternSnapshotCanonical = request.LanternSnapshotCanonical,
                    firstClearEligibilityCanonical = request.FirstClearEligibilityCanonical
                }), accessToken, cancellationToken);
            return MapIntent(response);
        }

        public async Task<ExpeditionServerRunSnapshot> CommitIntentAsync(string accessToken,
            CommitExpeditionServerIntent request, CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.EntryIntentId))
                throw new ArgumentException("A server entry intent is required.", "request");
            var response = await SendAsync<RunResponse>(UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/intents/" + UnityWebRequest.EscapeURL(request.EntryIntentId) + "/commit",
                JsonUtility.ToJson(new CommitIntentRequest
                {
                    idempotencyKey = request.IdempotencyKey,
                    address = request.AddressBase64,
                    signedMessage = request.SignedMessageBase64,
                    signature = request.SignatureBase64,
                    paymentAuthorizationReference = request.PaymentAuthorizationReference
                }), accessToken, cancellationToken);
            return MapRun(response);
        }

        public async Task<ExpeditionServerRunSnapshot> GetCurrentRunAsync(string accessToken,
            string seasonId, CancellationToken cancellationToken)
        {
            var response = await SendAsync<RunResponse>(UnityWebRequest.kHttpVerbGET,
                "v1/expeditions/chapter01/current?seasonId=" + UnityWebRequest.EscapeURL(seasonId),
                null, accessToken, cancellationToken);
            return MapRun(response);
        }

        public async Task<ExpeditionServerSettlementReceipt> SubmitSettlementAsync(
            string accessToken, SubmitExpeditionServerSettlement request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RunId))
                throw new ArgumentException("An authoritative expedition run is required.",
                    "request");
            var response = await SendAsync<SettlementResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/runs/" +
                UnityWebRequest.EscapeURL(request.RunId) + "/settlement",
                JsonUtility.ToJson(new SubmitSettlementRequest
                {
                    idempotencyKey = request.IdempotencyKey,
                    terminalReason = request.TerminalReason,
                    terminalNodeId = request.TerminalNodeId,
                    combatResultDigest = request.CombatResultDigest,
                    combatProofReference = request.CombatProofReference
                }), accessToken, cancellationToken);
            return MapSettlement(response);
        }

        public async Task<ExpeditionServerProgressCheckpoint> SubmitProgressAsync(
            string accessToken, SubmitExpeditionServerProgress request,
            CancellationToken cancellationToken)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RunId))
                throw new ArgumentException("An authoritative expedition run is required.",
                    "request");
            var response = await SendAsync<ProgressResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/runs/" +
                UnityWebRequest.EscapeURL(request.RunId) + "/progress",
                JsonUtility.ToJson(new SubmitProgressRequest
                {
                    idempotencyKey = request.IdempotencyKey,
                    completedNodeId = request.CompletedNodeId,
                    combatResultDigest = request.CombatResultDigest,
                    combatProofReference = request.CombatProofReference
                }), accessToken, cancellationToken);
            return MapProgress(response);
        }

        public Task<ExpeditionServerReplayProof> IssueCheckpointProofAsync(
            string accessToken,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken)
        {
            return IssueReplayProofAsync(
                accessToken, seasonId, envelope, "checkpoint-proof", cancellationToken);
        }

        public Task<ExpeditionServerReplayProof> IssueTerminalProofAsync(
            string accessToken,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken)
        {
            return IssueReplayProofAsync(
                accessToken, seasonId, envelope, "terminal-proof", cancellationToken);
        }

        private async Task<ExpeditionServerReplayProof> IssueReplayProofAsync(
            string accessToken,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            string proofPath,
            CancellationToken cancellationToken)
        {
            if (envelope == null || string.IsNullOrWhiteSpace(envelope.RunId))
                throw new ArgumentException(
                    "An authoritative expedition replay envelope is required.",
                    "envelope");
            var response = await SendAsync<ReplayProofResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/runs/" +
                UnityWebRequest.EscapeURL(envelope.RunId) + "/replay/" + proofPath,
                JsonUtility.ToJson(new SubmitReplayProofRequest
                {
                    seasonId = seasonId,
                    envelope = envelope
                }),
                accessToken,
                cancellationToken);
            return new ExpeditionServerReplayProof
            {
                RunId = response.runId,
                CombatResultDigest = response.combatResultDigest,
                CombatProofReference = response.combatProofReference,
                TranscriptSha256 = response.transcriptSha256,
                ActionCount = response.actionCount,
                RequestId = response.requestId
            };
        }

        public async Task<ExpeditionServerProgressCheckpoint> GetLatestProgressAsync(
            string accessToken, string runId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("An authoritative expedition run is required.",
                    "runId");
            return MapProgress(await SendAsync<ProgressResponse>(
                UnityWebRequest.kHttpVerbGET,
                "v1/expeditions/chapter01/runs/" + UnityWebRequest.EscapeURL(runId) +
                "/progress/current", null, accessToken, cancellationToken));
        }

        public async Task<ExpeditionServerSettlementReceipt> GetSettlementAsync(
            string accessToken, string runId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("An authoritative expedition run is required.",
                    "runId");
            return MapSettlement(await SendAsync<SettlementResponse>(
                UnityWebRequest.kHttpVerbGET,
                "v1/expeditions/chapter01/runs/" + UnityWebRequest.EscapeURL(runId) +
                "/settlement", null, accessToken, cancellationToken));
        }

        public async Task<ExpeditionServerRewardRecovery> RecoverRewardsAsync(
            string accessToken, CancellationToken cancellationToken)
        {
            var response = await SendAsync<RewardRecoveryResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/rewards/recover", "{}", accessToken,
                cancellationToken);
            var snapshot = response.snapshot ?? new RewardProjectionResponse();
            return new ExpeditionServerRewardRecovery
            {
                ExecutedBatches = response.executedBatches,
                ProcessedLedgerEntries = response.processedLedgerEntries,
                CaughtUp = response.caughtUp,
                Snapshot = new ExpeditionServerRewardProjectionStatus
                {
                    AccountId = snapshot.accountId,
                    AvailableBountyTickets = snapshot.availableBountyTickets,
                    PendingInstanceGenerationItems = snapshot.pendingInstanceGenerationItems,
                    ProjectedLedgerEntries = snapshot.projectedLedgerEntries,
                    PendingLedgerEntries = snapshot.pendingLedgerEntries,
                    RequestId = snapshot.requestId
                },
                RequestId = response.requestId
            };
        }

        public async Task<ExpeditionSettlementClaimChallenge> CreateClaimChallengeAsync(
            string accessToken, string settlementId, CancellationToken cancellationToken)
        {
            if (!Guid.TryParse(settlementId, out _))
                throw new ArgumentException("An authoritative settlement is required.",
                    "settlementId");
            var response = await SendAsync<ClaimChallengeResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/settlements/" +
                UnityWebRequest.EscapeURL(settlementId) + "/claim-challenges",
                "{}", accessToken, cancellationToken);
            return new ExpeditionSettlementClaimChallenge
            {
                ChallengeId = response.challengeId,
                SettlementId = response.settlementId,
                SignaturePurpose = response.signaturePurpose,
                CanonicalMessage = response.canonicalMessage,
                RewardManifestHash = response.rewardManifestHash,
                IssuedAt = response.issuedAt,
                ExpiresAt = response.expiresAt,
                RequestId = response.requestId
            };
        }

        public async Task<ExpeditionSettlementClaimReceipt> ClaimSettlementAsync(
            string accessToken, ClaimExpeditionServerSettlement request,
            CancellationToken cancellationToken)
        {
            if (request == null || !Guid.TryParse(request.SettlementId, out _))
                throw new ArgumentException("An authoritative settlement claim is required.",
                    "request");
            var response = await SendAsync<ClaimResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/expeditions/chapter01/settlements/" +
                UnityWebRequest.EscapeURL(request.SettlementId) + "/claim",
                JsonUtility.ToJson(new ClaimRequest
                {
                    challengeId = request.ChallengeId,
                    idempotencyKey = request.IdempotencyKey,
                    address = request.Address,
                    signedMessage = request.SignedMessage,
                    signature = request.Signature
                }), accessToken, cancellationToken);
            return new ExpeditionSettlementClaimReceipt
            {
                SettlementId = response.settlementId,
                AccountId = response.accountId,
                RewardManifestHash = response.rewardManifestHash,
                IdempotencyKey = response.idempotencyKey,
                ClaimedAt = response.claimedAt,
                WasAlreadyClaimed = response.wasAlreadyClaimed,
                RequestId = response.requestId
            };
        }

        private async Task<T> SendAsync<T>(string method, string relativePath, string json,
            string accessToken, CancellationToken cancellationToken) where T : class
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("An authenticated access token is required.", "accessToken");
            cancellationToken.ThrowIfCancellationRequested();
            var request = new UnityWebRequest(new Uri(baseUri, relativePath), method);
            try
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                }
                var operation = request.SendWebRequest();
                var completion = new TaskCompletionSource<bool>();
                operation.completed += ignored => completion.TrySetResult(true);
                if (operation.isDone) completion.TrySetResult(true);
                using (cancellationToken.Register(request.Abort)) await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();
                var data = request.downloadHandler == null ? null : request.downloadHandler.data;
                if (data != null && data.Length > MaximumResponseBytes)
                    throw new WalletServerException(request.responseCode, "CLIENT_RESPONSE_TOO_LARGE",
                        "The expedition server response exceeded the client limit.", string.Empty);
                var text = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                    throw CreateServerException(request.responseCode, text, request.error);
                var response = JsonUtility.FromJson<T>(text);
                if (response == null)
                    throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                        "The expedition server returned invalid JSON.", string.Empty);
                return response;
            }
            finally { request.Dispose(); }
        }

        private static ExpeditionServerEntryIntent MapIntent(IntentResponse response)
        {
            return new ExpeditionServerEntryIntent
            {
                EntryIntentId = response.entryIntentId,
                ChapterId = response.chapterId,
                SeasonId = response.seasonId,
                DifficultyId = response.difficultyId,
                EntryPriceMicroSkr = response.entryPriceMicroSkr,
                CanonicalAuthorizationMessage = response.canonicalAuthorizationMessage,
                ExpiresAt = response.expiresAt,
                RequiresPaymentAuthorization = response.requiresPaymentAuthorization,
                CommittedRunId = response.committedRunId
            };
        }

        private static ExpeditionServerRunSnapshot MapRun(RunResponse response)
        {
            return new ExpeditionServerRunSnapshot
            {
                RunId = response.runId, EntryIntentId = response.entryIntentId,
                AccountId = response.accountId, WalletAddress = response.walletAddress,
                ChapterId = response.chapterId, DifficultyId = response.difficultyId,
                DifficultyDisplayNameKey = response.difficultyDisplayNameKey,
                EntryPriceMicroSkr = response.entryPriceMicroSkr,
                UnlockProofDifficultyId = response.unlockProofDifficultyId,
                QuotaDayId = response.quotaDayId, QuotaSlot = response.quotaSlot,
                RewardOrigin = response.rewardOrigin, TradeStatePolicy = response.tradeStatePolicy,
                RewardTableVersion = response.rewardTableVersion,
                EncounterTableVersion = response.encounterTableVersion,
                MapVersion = response.mapVersion, RulesetVersion = response.rulesetVersion,
                SeasonId = response.seasonId, DeckSnapshotHash = response.deckSnapshotHash,
                EquipmentSnapshotHash = response.equipmentSnapshotHash,
                LanternSnapshotHash = response.lanternSnapshotHash,
                FirstClearLanternEligible = response.firstClearLanternEligible,
                FirstClearLanternEligibilityKeyHash = response.firstClearLanternEligibilityKeyHash,
                CombatInputContractVersion = response.combatInputContractVersion,
                CombatInputHash = response.combatInputHash,
                DeckSnapshotCanonical = response.deckSnapshotCanonical,
                EquipmentSnapshotCanonical = response.equipmentSnapshotCanonical,
                LanternSnapshotCanonical = response.lanternSnapshotCanonical,
                FirstClearEligibilityCanonical = response.firstClearEligibilityCanonical,
                SeedCommitment = response.seedCommitment, CreatedAt = response.createdAt,
                AuthoritativeTimeZone = response.authoritativeTimeZone,
                QuotaDayStartAtUtc = response.quotaDayStartAtUtc,
                QuotaDayEndAtUtc = response.quotaDayEndAtUtc,
                FormalCreationCloseAtUtc = response.formalCreationCloseAtUtc,
                RunExpiresAtUtc = response.runExpiresAtUtc,
                TimeRulesVersion = response.timeRulesVersion,
                QualityRecruitPriceRulesVersion = response.qualityRecruitPriceRulesVersion
            };
        }

        private static ExpeditionServerProgressCheckpoint MapProgress(
            ProgressResponse response)
        {
            return new ExpeditionServerProgressCheckpoint
            {
                CheckpointId = response.checkpointId,
                RunId = response.runId,
                AccountId = response.accountId,
                IdempotencyKey = response.idempotencyKey,
                CompletedNodeId = response.completedNodeId,
                CompletedNodeOrdinal = response.completedNodeOrdinal,
                ProgressRank = response.progressRank,
                CombatResultDigest = response.combatResultDigest,
                RecordedAt = response.recordedAt,
                RequestId = response.requestId
            };
        }

        private static ExpeditionServerSettlementReceipt MapSettlement(
            SettlementResponse response)
        {
            if (response == null)
                throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                    "The expedition settlement response is missing.", string.Empty);
            var sourceLedger = response.ledger ?? new LedgerLineResponse[0];
            var ledger = new ExpeditionServerRewardLedgerLine[sourceLedger.Length];
            for (var index = 0; index < sourceLedger.Length; index++)
            {
                var line = sourceLedger[index] ?? new LedgerLineResponse();
                ledger[index] = new ExpeditionServerRewardLedgerLine
                {
                    LedgerEntryId = line.ledgerEntryId,
                    RewardKind = line.rewardKind,
                    RewardCode = line.rewardCode,
                    Quantity = line.quantity,
                    Ordinal = line.ordinal,
                    PayloadJson = line.payloadJson
                };
            }
            var plan = response.plan ?? new SettlementPlanResponse();
            return new ExpeditionServerSettlementReceipt
            {
                SettlementId = response.settlementId,
                RunId = response.runId,
                AccountId = response.accountId,
                IdempotencyKey = response.idempotencyKey,
                Plan = new ExpeditionServerSettlementPlan
                {
                    DifficultyId = plan.difficultyId,
                    TerminalReason = plan.terminalReason,
                    ProgressRank = plan.progressRank,
                    TideCrystal = plan.tideCrystal,
                    LampGlow = plan.lampGlow,
                    TideIron = plan.tideIron,
                    MistLeather = plan.mistLeather,
                    StarSand = plan.starSand,
                    BountyTickets = plan.bountyTickets,
                    EquipmentQuantity = plan.equipmentQuantity,
                    EquipmentRarityIds = plan.equipmentRarityIds ?? new string[0],
                    LanternQuantity = plan.lanternQuantity,
                    LanternRarityId = plan.lanternRarityId,
                    IsFirstClearLantern = plan.isFirstClearLantern,
                    GrantsFirstClear = plan.grantsFirstClear,
                    UnlocksNextDifficulty = plan.unlocksNextDifficulty,
                    RequiresEquipmentInstanceGeneration =
                        plan.requiresEquipmentInstanceGeneration,
                    RequiresLanternInstanceGeneration =
                        plan.requiresLanternInstanceGeneration,
                    RewardTableVersion = plan.rewardTableVersion
                },
                SeedRevealHex = response.seedRevealHex,
                Ledger = ledger,
                RewardManifestHash = response.rewardManifestHash,
                RequiresWalletClaim = response.requiresWalletClaim,
                ClaimedAt = response.claimedAt,
                CreatedAt = response.createdAt,
                RequestId = response.requestId
            };
        }

        private static WalletServerException CreateServerException(long statusCode,
            string responseText, string fallbackMessage)
        {
            ErrorResponse error = null;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                try { error = JsonUtility.FromJson<ErrorResponse>(responseText); }
                catch (ArgumentException) { }
            }
            return new WalletServerException(statusCode,
                error == null ? "TRANSPORT_FAILED" : error.code,
                error != null && !string.IsNullOrWhiteSpace(error.message) ? error.message :
                    (string.IsNullOrWhiteSpace(fallbackMessage) ?
                        "The expedition server request failed." : fallbackMessage),
                error == null ? string.Empty : error.requestId);
        }

        private static bool IsAllowedEndpoint(Uri endpoint, bool allowInsecureLocalDevelopment)
        {
            if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)) return true;
            if (!allowInsecureLocalDevelopment ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)) return false;
            if (string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
            IPAddress address;
            if (!IPAddress.TryParse(endpoint.Host, out address)) return false;
            if (IPAddress.IsLoopback(address)) return true;
            var bytes = address.GetAddressBytes();
            return bytes.Length == 4 && (bytes[0] == 10 ||
                (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                (bytes[0] == 192 && bytes[1] == 168));
        }

#pragma warning disable 0649
        [Serializable] private sealed class CreateIntentRequest { public string chapterId; public string seasonId; public string difficultyId; public string idempotencyKey; public string deckSnapshotHash; public string equipmentSnapshotHash; public string lanternSnapshotHash; public string firstClearLanternEligibilityKeyHash; public string combatInputContractVersion; public string combatInputHash; public string deckSnapshotCanonical; public string equipmentSnapshotCanonical; public string lanternSnapshotCanonical; public string firstClearEligibilityCanonical; }
        [Serializable] private sealed class CommitIntentRequest { public string idempotencyKey; public string address; public string signedMessage; public string signature; public string paymentAuthorizationReference; }
        [Serializable] private sealed class DifficultyResponse { public string difficultyId; public string displayNameKey; public long entryPriceMicroSkr; public string requiresBossClearDifficultyId; public bool isUnlocked; public bool canCreateIntent; public string blockingCode; }
        [Serializable] private sealed class SelectionResponse { public string chapterId; public string seasonId; public string quotaDayId; public int dailyLimit; public int usedSlots; public int remainingSlots; public bool firstClearLanternEligible; public string activeRunId; public DifficultyResponse[] difficulties; public string authoritativeTimeZone; public string quotaDayStartAtUtc; public string quotaDayEndAtUtc; public string formalCreationCloseAtUtc; public string timeRulesVersion; }
        [Serializable] private sealed class PaymentTermsResponse { public string networkId; public string tokenMintAddress; public string treasuryWalletAddress; public int tokenDecimals; public string intentMemoPrefix; }
        [Serializable] private sealed class IntentResponse { public string entryIntentId; public string chapterId; public string seasonId; public string difficultyId; public long entryPriceMicroSkr; public string canonicalAuthorizationMessage; public string expiresAt; public bool requiresPaymentAuthorization; public string committedRunId; }
        [Serializable] private sealed class RunResponse { public string runId; public string entryIntentId; public string accountId; public string walletAddress; public string chapterId; public string difficultyId; public string difficultyDisplayNameKey; public long entryPriceMicroSkr; public string unlockProofDifficultyId; public string quotaDayId; public int quotaSlot; public string rewardOrigin; public string tradeStatePolicy; public string rewardTableVersion; public string encounterTableVersion; public string mapVersion; public string rulesetVersion; public string seasonId; public string deckSnapshotHash; public string equipmentSnapshotHash; public string lanternSnapshotHash; public bool firstClearLanternEligible; public string firstClearLanternEligibilityKeyHash; public string combatInputContractVersion; public string combatInputHash; public string deckSnapshotCanonical; public string equipmentSnapshotCanonical; public string lanternSnapshotCanonical; public string firstClearEligibilityCanonical; public string seedCommitment; public string createdAt; public string authoritativeTimeZone; public string quotaDayStartAtUtc; public string quotaDayEndAtUtc; public string formalCreationCloseAtUtc; public string runExpiresAtUtc; public string timeRulesVersion; public int qualityRecruitPriceRulesVersion; }
        [Serializable] private sealed class SubmitSettlementRequest { public string idempotencyKey; public string terminalReason; public string terminalNodeId; public string combatResultDigest; public string combatProofReference; }
        [Serializable] private sealed class SubmitProgressRequest { public string idempotencyKey; public string completedNodeId; public string combatResultDigest; public string combatProofReference; }
        [Serializable] private sealed class SubmitReplayProofRequest { public string seasonId; public ExpeditionReplayEnvelopeRecord envelope; }
        [Serializable] private sealed class ReplayProofResponse { public string runId; public string combatResultDigest; public string combatProofReference; public string transcriptSha256; public int actionCount; public string requestId; }
        [Serializable] private sealed class ClaimRequest { public string challengeId; public string idempotencyKey; public string address; public string signedMessage; public string signature; }
        [Serializable] private sealed class ProgressResponse { public string checkpointId; public string runId; public string accountId; public string idempotencyKey; public string completedNodeId; public int completedNodeOrdinal; public int progressRank; public string combatResultDigest; public string recordedAt; public string requestId; }
        [Serializable] private sealed class SettlementPlanResponse { public string difficultyId; public string terminalReason; public int progressRank; public int tideCrystal; public int lampGlow; public int tideIron; public int mistLeather; public int starSand; public int bountyTickets; public int equipmentQuantity; public string[] equipmentRarityIds; public int lanternQuantity; public string lanternRarityId; public bool isFirstClearLantern; public bool grantsFirstClear; public bool unlocksNextDifficulty; public bool requiresEquipmentInstanceGeneration; public bool requiresLanternInstanceGeneration; public string rewardTableVersion; }
        [Serializable] private sealed class LedgerLineResponse { public string ledgerEntryId; public string rewardKind; public string rewardCode; public int quantity; public int ordinal; public string payloadJson; }
        [Serializable] private sealed class SettlementResponse { public string settlementId; public string runId; public string accountId; public string idempotencyKey; public SettlementPlanResponse plan; public string seedRevealHex; public LedgerLineResponse[] ledger; public string rewardManifestHash; public bool requiresWalletClaim; public string claimedAt; public string createdAt; public string requestId; }
        [Serializable] private sealed class ClaimChallengeResponse { public string challengeId; public string settlementId; public string signaturePurpose; public string canonicalMessage; public string rewardManifestHash; public string issuedAt; public string expiresAt; public string requestId; }
        [Serializable] private sealed class ClaimResponse { public string settlementId; public string accountId; public string rewardManifestHash; public string idempotencyKey; public string claimedAt; public bool wasAlreadyClaimed; public string requestId; }
        [Serializable] private sealed class RewardProjectionResponse { public string accountId; public int availableBountyTickets; public int pendingInstanceGenerationItems; public int projectedLedgerEntries; public int pendingLedgerEntries; public string requestId; }
        [Serializable] private sealed class RewardRecoveryResponse { public int executedBatches; public int processedLedgerEntries; public bool caughtUp; public RewardProjectionResponse snapshot; public string requestId; }
        [Serializable] private sealed class ErrorResponse { public string code; public string message; public string requestId; }
#pragma warning restore 0649
    }
}
