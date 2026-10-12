using System;
using System.Text;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class ExpeditionServerDifficulty
    {
        public string DifficultyId;
        public string DisplayNameKey;
        public long EntryPriceMicroSkr;
        public string RequiresBossClearDifficultyId;
        public bool IsUnlocked;
        public bool CanCreateIntent;
        public string BlockingCode;
    }

    [Serializable]
    public sealed class ExpeditionServerSelection
    {
        public string ChapterId;
        public string SeasonId;
        public string QuotaDayId;
        public int DailyLimit;
        public int UsedSlots;
        public int RemainingSlots;
        public bool FirstClearLanternEligible;
        public string ActiveRunId;
        public ExpeditionServerDifficulty[] Difficulties;
        public string AuthoritativeTimeZone;
        public string QuotaDayStartAtUtc;
        public string QuotaDayEndAtUtc;
        public string FormalCreationCloseAtUtc;
        public string TimeRulesVersion;
    }

    [Serializable]
    public sealed class ExpeditionServerEntryIntent
    {
        public string EntryIntentId;
        public string ChapterId;
        public string SeasonId;
        public string DifficultyId;
        public long EntryPriceMicroSkr;
        public string CanonicalAuthorizationMessage;
        public string ExpiresAt;
        public bool RequiresPaymentAuthorization;
        public string CommittedRunId;
    }

    [Serializable]
    public sealed class ExpeditionServerPaymentTerms
    {
        public string NetworkId;
        public string TokenMintAddress;
        public string TreasuryWalletAddress;
        public int TokenDecimals;
        public string IntentMemoPrefix;
    }

    public interface IExpeditionPaymentGateway
    {
        Task<string> SubmitSplTokenPaymentAsync(
            string expectedWalletAddress,
            string networkId,
            string tokenMintAddress,
            string treasuryWalletAddress,
            long amountInSmallestUnits,
            int tokenDecimals,
            string intentMemo,
            CancellationToken cancellationToken);
    }

    [Serializable]
    public sealed class PendingExpeditionPaymentReceipt
    {
        public string EntryIntentId;
        public string WalletAddress;
        public string NetworkId;
        public string TokenMintAddress;
        public string TreasuryWalletAddress;
        public long AmountInSmallestUnits;
        public int TokenDecimals;
        public string IntentMemo;
        public string TransactionSignature;
    }

    public interface IExpeditionPaymentReceiptStore
    {
        PendingExpeditionPaymentReceipt Load(string entryIntentId);
        void Save(PendingExpeditionPaymentReceipt receipt);
        void Clear(string entryIntentId);
    }

    [Serializable]
    public sealed class ExpeditionServerRunSnapshot
    {
        public string RunId;
        public string EntryIntentId;
        public string AccountId;
        public string WalletAddress;
        public string ChapterId;
        public string DifficultyId;
        public string DifficultyDisplayNameKey;
        public long EntryPriceMicroSkr;
        public string UnlockProofDifficultyId;
        public string QuotaDayId;
        public int QuotaSlot;
        public string RewardOrigin;
        public string TradeStatePolicy;
        public string RewardTableVersion;
        public string EncounterTableVersion;
        public string MapVersion;
        public string RulesetVersion;
        public string SeasonId;
        public string DeckSnapshotHash;
        public string EquipmentSnapshotHash;
        public string LanternSnapshotHash;
        public bool FirstClearLanternEligible;
        public string FirstClearLanternEligibilityKeyHash;
        public string CombatInputContractVersion;
        public string CombatInputHash;
        public string DeckSnapshotCanonical;
        public string EquipmentSnapshotCanonical;
        public string LanternSnapshotCanonical;
        public string FirstClearEligibilityCanonical;
        public string SeedCommitment;
        public string CreatedAt;
        public string AuthoritativeTimeZone;
        public string QuotaDayStartAtUtc;
        public string QuotaDayEndAtUtc;
        public string FormalCreationCloseAtUtc;
        public string RunExpiresAtUtc;
        public string TimeRulesVersion;
        public int QualityRecruitPriceRulesVersion;
    }

    public sealed class CreateExpeditionServerIntent
    {
        public string ChapterId;
        public string SeasonId;
        public string DifficultyId;
        public string IdempotencyKey;
        public string DeckSnapshotHash;
        public string EquipmentSnapshotHash;
        public string LanternSnapshotHash;
        public string FirstClearLanternEligibilityKeyHash;
        public string CombatInputContractVersion;
        public string CombatInputHash;
        public string DeckSnapshotCanonical;
        public string EquipmentSnapshotCanonical;
        public string LanternSnapshotCanonical;
        public string FirstClearEligibilityCanonical;
    }

    public sealed class CommitExpeditionServerIntent
    {
        public string EntryIntentId;
        public string IdempotencyKey;
        public string AddressBase64;
        public string SignedMessageBase64;
        public string SignatureBase64;
        public string PaymentAuthorizationReference;
    }

    public interface IExpeditionServerTransport
    {
        Task<ExpeditionServerSelection> GetSelectionAsync(string accessToken,
            string seasonId, CancellationToken cancellationToken);
        Task<ExpeditionServerPaymentTerms> GetPaymentTermsAsync(
            string accessToken, CancellationToken cancellationToken);
        Task<ExpeditionServerEntryIntent> CreateIntentAsync(string accessToken,
            CreateExpeditionServerIntent request, CancellationToken cancellationToken);
        Task<ExpeditionServerRunSnapshot> CommitIntentAsync(string accessToken,
            CommitExpeditionServerIntent request, CancellationToken cancellationToken);
        Task<ExpeditionServerRunSnapshot> GetCurrentRunAsync(string accessToken,
            string seasonId, CancellationToken cancellationToken);
    }

    public sealed class ExpeditionEntryCoordinator
    {
        public const string ChapterId = "chapter01";
        private readonly IExpeditionServerTransport transport;
        private readonly IWalletSiwsGateway wallet;
        private readonly IExpeditionPaymentGateway paymentGateway;
        private readonly IExpeditionPaymentReceiptStore paymentReceiptStore;
        private int paidOperationInProgress;

        public ExpeditionEntryCoordinator(IExpeditionServerTransport transport,
            IWalletSiwsGateway wallet)
            : this(transport, wallet, null, null)
        {
        }

        public ExpeditionEntryCoordinator(IExpeditionServerTransport transport,
            IWalletSiwsGateway wallet,
            IExpeditionPaymentGateway paymentGateway,
            IExpeditionPaymentReceiptStore paymentReceiptStore = null)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
            this.wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            this.paymentGateway = paymentGateway;
            this.paymentReceiptStore = paymentReceiptStore;
        }

        public Task<ExpeditionServerSelection> LoadSelectionAsync(string accessToken,
            string seasonId, CancellationToken cancellationToken)
        {
            ValidateSession(accessToken, seasonId);
            return transport.GetSelectionAsync(accessToken, seasonId, cancellationToken);
        }

        public Task<ExpeditionServerRunSnapshot> RecoverCurrentRunAsync(string accessToken,
            string seasonId, CancellationToken cancellationToken)
        {
            ValidateSession(accessToken, seasonId);
            return transport.GetCurrentRunAsync(accessToken, seasonId, cancellationToken);
        }

        public async Task<ExpeditionServerRunSnapshot> BeginFreeD0Async(
            string accessToken,
            string seasonId,
            string expectedWalletAddress,
            string idempotencyKey,
            ExpeditionLoadoutSnapshotHashes snapshot,
            CancellationToken cancellationToken)
        {
            ValidateSession(accessToken, seasonId);
            if (string.IsNullOrWhiteSpace(expectedWalletAddress))
                throw new InvalidOperationException("An authenticated wallet address is required.");
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var request = new CreateExpeditionServerIntent
            {
                ChapterId = ChapterId,
                SeasonId = seasonId,
                DifficultyId = "D0",
                IdempotencyKey = idempotencyKey,
                DeckSnapshotHash = snapshot.DeckSnapshotHash,
                EquipmentSnapshotHash = snapshot.EquipmentSnapshotHash,
                LanternSnapshotHash = snapshot.LanternSnapshotHash,
                FirstClearLanternEligibilityKeyHash = snapshot.FirstClearEligibilityKeyHash,
                CombatInputContractVersion = snapshot.CombatInputContractVersion,
                CombatInputHash = snapshot.CombatInputHash,
                DeckSnapshotCanonical = snapshot.DeckSnapshotCanonical,
                EquipmentSnapshotCanonical = snapshot.EquipmentSnapshotCanonical,
                LanternSnapshotCanonical = snapshot.LanternSnapshotCanonical,
                FirstClearEligibilityCanonical = snapshot.FirstClearEligibilityCanonical
            };
            var intent = await transport.CreateIntentAsync(accessToken, request, cancellationToken);
            if (intent == null || intent.DifficultyId != "D0" || intent.EntryPriceMicroSkr != 0 ||
                intent.RequiresPaymentAuthorization || string.IsNullOrWhiteSpace(intent.EntryIntentId) ||
                string.IsNullOrWhiteSpace(intent.CanonicalAuthorizationMessage))
                throw new InvalidOperationException("The server returned an invalid free expedition intent.");

            var identity = await wallet.ConnectAsync(cancellationToken);
            if (identity == null || !string.Equals(identity.WalletAddress, expectedWalletAddress,
                    StringComparison.Ordinal))
                throw new InvalidOperationException("The connected wallet does not match the authenticated account.");
            var message = Encoding.UTF8.GetBytes(intent.CanonicalAuthorizationMessage);
            var signature = await wallet.SignMessageAsync(message, cancellationToken);
            if (signature == null || signature.Length == 0)
                throw new InvalidOperationException("The expedition entry signature was not returned.");
            return await transport.CommitIntentAsync(accessToken,
                new CommitExpeditionServerIntent
                {
                    EntryIntentId = intent.EntryIntentId,
                    IdempotencyKey = idempotencyKey,
                    AddressBase64 = Convert.ToBase64String(identity.PublicKey),
                    SignedMessageBase64 = Convert.ToBase64String(message),
                    SignatureBase64 = Convert.ToBase64String(signature),
                    PaymentAuthorizationReference = null
                }, cancellationToken);
        }

        public async Task<ExpeditionServerRunSnapshot> BeginPaidAsync(
            string accessToken,
            string seasonId,
            string difficultyId,
            string expectedWalletAddress,
            string idempotencyKey,
            ExpeditionLoadoutSnapshotHashes snapshot,
            CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref paidOperationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "A paid expedition wallet operation is already in progress.");
            try
            {
            ValidateSession(accessToken, seasonId);
            if (paymentGateway == null)
                throw new InvalidOperationException("Paid expedition wallet support is unavailable.");
            if (paymentReceiptStore == null)
                throw new InvalidOperationException(
                    "Secure paid expedition receipt recovery is unavailable.");
            if (difficultyId != "D1" && difficultyId != "D2" && difficultyId != "D3")
                throw new InvalidOperationException("A paid expedition difficulty is required.");
            if (string.IsNullOrWhiteSpace(expectedWalletAddress))
                throw new InvalidOperationException("An authenticated wallet address is required.");
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            var terms = await transport.GetPaymentTermsAsync(accessToken, cancellationToken);
            if (terms == null || string.IsNullOrWhiteSpace(terms.NetworkId) ||
                string.IsNullOrWhiteSpace(terms.TokenMintAddress) ||
                string.IsNullOrWhiteSpace(terms.TreasuryWalletAddress) ||
                terms.TokenDecimals < 0 || terms.TokenDecimals > 18 ||
                string.IsNullOrWhiteSpace(terms.IntentMemoPrefix))
                throw new InvalidOperationException("The server returned invalid payment terms.");

            var intent = await transport.CreateIntentAsync(accessToken,
                new CreateExpeditionServerIntent
                {
                    ChapterId = ChapterId,
                    SeasonId = seasonId,
                    DifficultyId = difficultyId,
                    IdempotencyKey = idempotencyKey,
                    DeckSnapshotHash = snapshot.DeckSnapshotHash,
                    EquipmentSnapshotHash = snapshot.EquipmentSnapshotHash,
                    LanternSnapshotHash = snapshot.LanternSnapshotHash,
                    FirstClearLanternEligibilityKeyHash = snapshot.FirstClearEligibilityKeyHash,
                    CombatInputContractVersion = snapshot.CombatInputContractVersion,
                    CombatInputHash = snapshot.CombatInputHash,
                    DeckSnapshotCanonical = snapshot.DeckSnapshotCanonical,
                    EquipmentSnapshotCanonical = snapshot.EquipmentSnapshotCanonical,
                    LanternSnapshotCanonical = snapshot.LanternSnapshotCanonical,
                    FirstClearEligibilityCanonical = snapshot.FirstClearEligibilityCanonical
                }, cancellationToken);
            if (intent == null || intent.DifficultyId != difficultyId ||
                intent.EntryPriceMicroSkr <= 0 || !intent.RequiresPaymentAuthorization ||
                string.IsNullOrWhiteSpace(intent.EntryIntentId) ||
                string.IsNullOrWhiteSpace(intent.CanonicalAuthorizationMessage))
                throw new InvalidOperationException("The server returned an invalid paid expedition intent.");

            var identity = await wallet.ConnectAsync(cancellationToken);
            if (identity == null || !string.Equals(identity.WalletAddress, expectedWalletAddress,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The connected wallet does not match the authenticated account.");
            var memo = terms.IntentMemoPrefix + Sha256Hex(intent.CanonicalAuthorizationMessage);
            var pendingReceipt = paymentReceiptStore.Load(intent.EntryIntentId);
            string transactionSignature;
            if (pendingReceipt == null)
            {
                transactionSignature = await paymentGateway.SubmitSplTokenPaymentAsync(
                    expectedWalletAddress,
                    terms.NetworkId,
                    terms.TokenMintAddress,
                    terms.TreasuryWalletAddress,
                    intent.EntryPriceMicroSkr,
                    terms.TokenDecimals,
                    memo,
                    cancellationToken);
                if (string.IsNullOrWhiteSpace(transactionSignature))
                    throw new InvalidOperationException(
                        "The wallet did not submit the payment transaction.");
                pendingReceipt = new PendingExpeditionPaymentReceipt
                {
                    EntryIntentId = intent.EntryIntentId,
                    WalletAddress = expectedWalletAddress,
                    NetworkId = terms.NetworkId,
                    TokenMintAddress = terms.TokenMintAddress,
                    TreasuryWalletAddress = terms.TreasuryWalletAddress,
                    AmountInSmallestUnits = intent.EntryPriceMicroSkr,
                    TokenDecimals = terms.TokenDecimals,
                    IntentMemo = memo,
                    TransactionSignature = transactionSignature
                };
                // This write is synchronous on Android. A commit retry must reuse the
                // submitted signature instead of asking the wallet to pay again.
                paymentReceiptStore.Save(pendingReceipt);
            }
            else
            {
                ValidatePendingReceipt(pendingReceipt, intent.EntryIntentId,
                    expectedWalletAddress, terms, intent.EntryPriceMicroSkr, memo);
                transactionSignature = pendingReceipt.TransactionSignature;
            }

            var run = await transport.CommitIntentAsync(accessToken,
                new CommitExpeditionServerIntent
                {
                    EntryIntentId = intent.EntryIntentId,
                    IdempotencyKey = idempotencyKey,
                    AddressBase64 = string.Empty,
                    SignedMessageBase64 = string.Empty,
                    SignatureBase64 = string.Empty,
                    PaymentAuthorizationReference = transactionSignature
                }, cancellationToken);
            paymentReceiptStore.Clear(intent.EntryIntentId);
            return run;
            }
            finally
            {
                Interlocked.Exchange(ref paidOperationInProgress, 0);
            }
        }

        private static void ValidatePendingReceipt(PendingExpeditionPaymentReceipt receipt,
            string entryIntentId, string walletAddress, ExpeditionServerPaymentTerms terms,
            long amountInSmallestUnits, string memo)
        {
            if (!string.Equals(receipt.EntryIntentId, entryIntentId, StringComparison.Ordinal) ||
                !string.Equals(receipt.WalletAddress, walletAddress, StringComparison.Ordinal) ||
                !string.Equals(receipt.NetworkId, terms.NetworkId, StringComparison.Ordinal) ||
                !string.Equals(receipt.TokenMintAddress, terms.TokenMintAddress,
                    StringComparison.Ordinal) ||
                !string.Equals(receipt.TreasuryWalletAddress, terms.TreasuryWalletAddress,
                    StringComparison.Ordinal) ||
                receipt.AmountInSmallestUnits != amountInSmallestUnits ||
                receipt.TokenDecimals != terms.TokenDecimals ||
                !string.Equals(receipt.IntentMemo, memo, StringComparison.Ordinal) ||
                string.IsNullOrWhiteSpace(receipt.TransactionSignature))
                throw new InvalidOperationException(
                    "The encrypted pending payment receipt does not match this intent.");
        }

        private static string Sha256Hex(string value)
        {
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))
                    .Replace("-", string.Empty)
                    .ToLowerInvariant();
            }
        }

        private static void ValidateSession(string accessToken, string seasonId)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(seasonId))
                throw new InvalidOperationException("An authenticated session and season are required.");
        }
    }
}
