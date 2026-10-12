using System;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using SeekerAutoChess.Wallet;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private static readonly TimeSpan WalletAccessRefreshLeadTime = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan WalletRefreshCheckInterval = TimeSpan.FromSeconds(30);

        private WalletAuthenticationCoordinator walletServerAuthentication;
        private IWalletSiwsGateway walletServerGateway;
        private ExpeditionEntryCoordinator expeditionEntryCoordinator;
        private FormalExpeditionEntryExecutionCoordinator
            formalExpeditionEntryExecutionCoordinator;
        private IExpeditionReplayStateStore formalExpeditionReplayStateStore;
        private FormalExpeditionClientRunContext formalExpeditionRunContext;
        private ExpeditionReplayProofCoordinator expeditionReplayProofCoordinator;
        private ExpeditionProgressCoordinator expeditionProgressCoordinator;
        private RewardInstanceServerCoordinator rewardInstanceServerCoordinator;
        private IStarterCharacterGrantTransport starterCharacterGrantTransport;
        private IFirstLightseekerBadgeGrantTransport firstLightseekerBadgeGrantTransport;
        private LanternWorkshopCoordinator lanternWorkshopCoordinator;
        private ExpeditionSettlementCoordinator expeditionSettlementCoordinator;
        private FormalExpeditionAuthorityFlowCoordinator
            formalExpeditionAuthorityFlowCoordinator;
        private ExpeditionServerSettlementReceipt pendingFormalExpeditionSettlement;
        private readonly AuthoritativeRewardInventoryMirror authoritativeRewardInventoryMirror =
            new AuthoritativeRewardInventoryMirror();
        private CancellationTokenSource walletServerLifetime;
        private string walletServerBaseUrl;
        private bool walletServerAllowsInsecureLocalDevelopment;
        private DateTimeOffset walletNextRefreshCheckAt;
        private int walletServerOperationInProgress;
        private WalletServerSessionState walletServerSessionState =
            WalletServerSessionState.NotConfigured;
        private string walletServerSessionError;
        private string walletServerDisplayName;

        public event Action<WalletServerSessionState, string> WalletServerSessionStateChanged;

        public WalletServerSessionState WalletServerState
        {
            get { return walletServerSessionState; }
        }

        public string WalletServerError
        {
            get { return walletServerSessionError; }
        }

        public WalletAuthenticationSession CurrentWalletServerSession
        {
            get
            {
                return walletServerAuthentication == null
                    ? null
                    : walletServerAuthentication.CurrentSession;
            }
        }

        public AuthoritativeRewardInventorySnapshot AuthoritativeRewardInventory
        {
            get { return authoritativeRewardInventoryMirror.Current; }
        }

        public string WalletServerDisplayName
        {
            get { return walletServerDisplayName; }
        }

        public bool HasPendingFormalExpeditionSettlement
        {
            get
            {
                return pendingFormalExpeditionSettlement != null &&
                       pendingFormalExpeditionSettlement.RequiresWalletClaim;
            }
        }

        public void ConfigureWalletServerRuntime(
            string baseUrl,
            bool allowInsecureLocalDevelopment,
            string identityUri,
            string iconUri,
            string applicationName,
            string chainId = WalletAuthenticationCoordinator.DevnetChainId)
        {
            if (Interlocked.CompareExchange(ref walletServerOperationInProgress, 0, 0) != 0)
                throw new InvalidOperationException(
                    "Wait for the active wallet server operation before changing configuration.");
            if (walletServerAuthentication != null &&
                walletServerAuthentication.CurrentSession != null)
                throw new InvalidOperationException(
                    "Sign out before changing the wallet server configuration.");

            var gateway = new SolanaMwaSiwsGateway(
                identityUri, iconUri, applicationName, chainId);
            var transport = new UnityWalletAuthenticationTransport(
                baseUrl, allowInsecureLocalDevelopment);
            var sessionStore = new AndroidWalletAuthenticationSessionStore(baseUrl);
            var authentication = new WalletAuthenticationCoordinator(
                gateway, transport, ClientVersion, sessionStore, chainId);
            var expeditionTransport = new UnityExpeditionServerTransport(
                baseUrl, allowInsecureLocalDevelopment);
            var rewardTransport = new UnityRewardInstanceServerTransport(
                baseUrl, allowInsecureLocalDevelopment);
            var starterGrant = new UnityStarterCharacterGrantTransport(
                baseUrl, allowInsecureLocalDevelopment);
            var firstLightseekerBadgeGrant = new UnityFirstLightseekerBadgeGrantTransport(
                baseUrl, allowInsecureLocalDevelopment);
            var lanternWorkshopTransport = new UnityLanternWorkshopServerTransport(
                baseUrl, allowInsecureLocalDevelopment);

            CancelWalletServerLifetime();
            ClearBountyTowerServerSession();
            walletServerAuthentication = authentication;
            walletServerGateway = gateway;
            expeditionEntryCoordinator = new ExpeditionEntryCoordinator(
                expeditionTransport, gateway, gateway,
                new AndroidExpeditionPaymentReceiptStore(baseUrl));
            formalExpeditionReplayStateStore =
                new AndroidExpeditionReplayStateStore(baseUrl);
            formalExpeditionEntryExecutionCoordinator =
                new FormalExpeditionEntryExecutionCoordinator(
                    expeditionEntryCoordinator,
                    formalExpeditionReplayStateStore);
            formalExpeditionRunContext = null;
            pendingFormalExpeditionSettlement = null;
            expeditionReplayProofCoordinator = new ExpeditionReplayProofCoordinator(
                expeditionTransport);
            expeditionProgressCoordinator = new ExpeditionProgressCoordinator(
                expeditionTransport);
            rewardInstanceServerCoordinator = new RewardInstanceServerCoordinator(
                rewardTransport, authoritativeRewardInventoryMirror);
            starterCharacterGrantTransport = starterGrant;
            firstLightseekerBadgeGrantTransport = firstLightseekerBadgeGrant;
            lanternWorkshopCoordinator = new LanternWorkshopCoordinator(
                lanternWorkshopTransport);
            expeditionSettlementCoordinator = new ExpeditionSettlementCoordinator(
                expeditionTransport, rewardInstanceServerCoordinator);
            formalExpeditionAuthorityFlowCoordinator =
                new FormalExpeditionAuthorityFlowCoordinator(
                    expeditionReplayProofCoordinator,
                    expeditionProgressCoordinator,
                    expeditionSettlementCoordinator);
            authoritativeRewardInventoryMirror.Clear();
            walletServerBaseUrl = baseUrl;
            walletServerAllowsInsecureLocalDevelopment = allowInsecureLocalDevelopment;
            walletServerLifetime = new CancellationTokenSource();
            walletNextRefreshCheckAt = DateTimeOffset.MinValue;
            SetWalletServerState(WalletServerSessionState.SignedOut, null);
        }

        public async Task<WalletAuthenticationSession> RestoreWalletServerSessionAsync(
            CancellationToken cancellationToken)
        {
            var authentication = RequireWalletServerAuthentication();
            BeginWalletServerOperation();
            try
            {
                SetWalletServerState(WalletServerSessionState.Restoring, null);
                using (var linked = CreateWalletServerLinkedToken(cancellationToken))
                {
                    var session = await authentication.RestoreAsync(linked.Token);
                    if (session == null)
                    {
                        authoritativeRewardInventoryMirror.Clear();
                        SetWalletServerState(WalletServerSessionState.SignedOut, null);
                        return null;
                    }

                    var account = await authentication.GetCurrentAccountAsync(linked.Token);
                    ValidateWalletServerAccount(session, account);
                    walletServerDisplayName = account.DisplayName;
                    await SynchronizeFirstLightseekerBadgeAsync(session, linked.Token);
                    if (CompetitionDemoMode)
                    {
                        authoritativeRewardInventoryMirror.Clear();
                        ClearBountyTowerServerSession();
                    }
                    else
                    {
                        await SynchronizeAuthoritativeStarterCharactersAsync(
                            session, linked.Token);
                        await TrySynchronizeAuthoritativeRewardsAfterAuthenticationAsync(
                            session, linked.Token);
                        ConfigureBountyTowerServer(
                            walletServerBaseUrl,
                            walletServerAllowsInsecureLocalDevelopment,
                            session);
                    }
                    walletNextRefreshCheckAt = DateTimeOffset.UtcNow +
                                               WalletRefreshCheckInterval;
                    SetWalletServerState(WalletServerSessionState.Authenticated, null);
                    return session;
                }
            }
            catch (OperationCanceledException)
            {
                authoritativeRewardInventoryMirror.Clear();
                throw;
            }
            catch (Exception exception)
            {
                authoritativeRewardInventoryMirror.Clear();
                if (ReferenceEquals(authentication, walletServerAuthentication))
                    SetWalletServerState(WalletServerSessionState.Failed, exception.Message);
                throw;
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public async Task<WalletAuthenticationSession> SignInWalletServerAsync(
            CancellationToken cancellationToken)
        {
            return await SignInWalletServerAsync(null, cancellationToken);
        }

        public async Task<WalletAuthenticationSession> SignInWalletServerAsync(
            string invitationCode,
            CancellationToken cancellationToken)
        {
            var authentication = RequireWalletServerAuthentication();
            BeginWalletServerOperation();
            try
            {
                SetWalletServerState(WalletServerSessionState.SigningIn, null);
                using (var linked = CreateWalletServerLinkedToken(cancellationToken))
                {
                    var session = await authentication.SignInAsync(invitationCode, linked.Token);
                    var account = await authentication.GetCurrentAccountAsync(linked.Token);
                    ValidateWalletServerAccount(session, account);
                    walletServerDisplayName = account.DisplayName;
                    await SynchronizeFirstLightseekerBadgeAsync(session, linked.Token);
                    if (CompetitionDemoMode)
                    {
                        authoritativeRewardInventoryMirror.Clear();
                        ClearBountyTowerServerSession();
                    }
                    else
                    {
                        await SynchronizeAuthoritativeStarterCharactersAsync(
                            session, linked.Token);
                        await TrySynchronizeAuthoritativeRewardsAfterAuthenticationAsync(
                            session, linked.Token);
                        ConfigureBountyTowerServer(
                            walletServerBaseUrl,
                            walletServerAllowsInsecureLocalDevelopment,
                            session);
                    }
                    walletNextRefreshCheckAt = DateTimeOffset.UtcNow + WalletRefreshCheckInterval;
                    SetWalletServerState(WalletServerSessionState.Authenticated, null);
                    return session;
                }
            }
            catch (OperationCanceledException)
            {
                ClearBountyTowerServerSession();
                authoritativeRewardInventoryMirror.Clear();
                await TryDiscardFailedWalletSessionAsync(authentication);
                if (ReferenceEquals(authentication, walletServerAuthentication))
                    SetWalletServerState(WalletServerSessionState.SignedOut, null);
                throw;
            }
            catch (Exception exception)
            {
                ClearBountyTowerServerSession();
                authoritativeRewardInventoryMirror.Clear();
                await TryDiscardFailedWalletSessionAsync(authentication);
                if (ReferenceEquals(authentication, walletServerAuthentication))
                    SetWalletServerState(WalletServerSessionState.Failed, exception.Message);
                throw;
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public async Task<WalletAuthenticationSession> RefreshWalletServerSessionAsync(
            CancellationToken cancellationToken)
        {
            var authentication = RequireWalletServerAuthentication();
            BeginWalletServerOperation();
            try
            {
                SetWalletServerState(WalletServerSessionState.Refreshing, null);
                using (var linked = CreateWalletServerLinkedToken(cancellationToken))
                {
                    var session = await authentication.RefreshAsync(linked.Token);
                    UpdateBountyTowerSession(session);
                    walletNextRefreshCheckAt = DateTimeOffset.UtcNow + WalletRefreshCheckInterval;
                    SetWalletServerState(WalletServerSessionState.Authenticated, null);
                    return session;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                if (ReferenceEquals(authentication, walletServerAuthentication))
                    SetWalletServerState(WalletServerSessionState.Failed, exception.Message);
                throw;
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public async Task<bool> SignOutWalletServerAsync(CancellationToken cancellationToken)
        {
            var authentication = RequireWalletServerAuthentication();
            BeginWalletServerOperation();
            try
            {
                SetWalletServerState(WalletServerSessionState.SigningOut, null);
                bool serverSessionRevoked;
                using (var linked = CreateWalletServerLinkedToken(cancellationToken))
                    serverSessionRevoked = await authentication.SignOutAsync(linked.Token);
                ClearBountyTowerServerSession();
                authoritativeRewardInventoryMirror.Clear();
                formalExpeditionRunContext = null;
                pendingFormalExpeditionSettlement = null;
                SetWalletServerState(WalletServerSessionState.SignedOut, null);
                return serverSessionRevoked;
            }
            catch (Exception exception)
            {
                SetWalletServerState(WalletServerSessionState.Failed, exception.Message);
                throw;
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        private async Task SynchronizeAuthoritativeStarterCharactersAsync(
            WalletAuthenticationSession session,
            CancellationToken cancellationToken)
        {
            if (starterCharacterGrantTransport == null)
                throw new InvalidOperationException("The starter character transport is not configured.");
            var requestId = "public-entry-" + session.AccountId + "-" + MainnetInviteSeasonId;
            var grant = await starterCharacterGrantTransport.ClaimAsync(
                session.AccessToken, MainnetInviteSeasonId, requestId, cancellationToken);
            Guid sessionAccountId;
            Guid grantAccountId;
            if (grant == null ||
                !Guid.TryParse(session.AccountId, out sessionAccountId) ||
                !Guid.TryParse(grant.AccountId, out grantAccountId) ||
                sessionAccountId != grantAccountId ||
                !string.Equals(grant.SeasonId, MainnetInviteSeasonId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The starter character grant does not match the authenticated account and season.");
            string[] authoritativeDeckIds;
            playerCollection = StarterCharacterGrantCollectionRules.Merge(
                characterCatalog, playerCollection, grant, out authoritativeDeckIds);
            nextRunDeckSelection = new RunDeckSelection(
                playerCollection, authoritativeDeckIds, MaximumDeckSize, MinimumDeckSize);
            selectedEquipmentCharacterInstanceId = authoritativeDeckIds.Length == 0
                ? selectedEquipmentCharacterInstanceId
                : authoritativeDeckIds[0];
            AcknowledgeStarterGrantWithoutReveal(grant);
            SaveGame();
        }

        private async Task SynchronizeFirstLightseekerBadgeAsync(
            WalletAuthenticationSession session,
            CancellationToken cancellationToken)
        {
            if (firstLightseekerBadgeGrantTransport == null)
                throw new InvalidOperationException(
                    "The First Lightseeker badge transport is not configured.");
            var grant = await firstLightseekerBadgeGrantTransport.ClaimAsync(
                session.AccessToken, cancellationToken);
            if (grant == null ||
                !string.Equals(grant.AccountId, session.AccountId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The First Lightseeker badge grant does not match the authenticated account.");
            ApplyFirstLightseekerBadgeGrant(grant);
        }

        private void AcknowledgeStarterGrantWithoutReveal(StarterCharacterGrantSnapshot grant)
        {
            if (grant == null || string.IsNullOrWhiteSpace(grant.GrantId)) return;
            ClearPendingStarterGrantReveal(grant.GrantId);
        }

        private void ClearPendingStarterGrantReveal(string grantId)
        {
            if (!string.IsNullOrWhiteSpace(grantId))
                acknowledgedStarterGrantId = grantId;
            if (!pendingStoreRevealAlreadyGranted) return;
            if (storeRevealPreparationCoroutine != null)
            {
                StopCoroutine(storeRevealPreparationCoroutine);
                storeRevealPreparationCoroutine = null;
            }
            storeOpeningActive = false;
            storeOpeningProgress = 0f;
            storeRevealLoadingElapsed = 0f;
            storeRevealAssetsReady = false;
            storeRevealPreloadComplete = false;
            pendingStoreOrderId = null;
            pendingStoreInstanceIds = new string[0];
            pendingStoreDefinitionIds = new string[0];
            pendingStoreQualities = new int[0];
            pendingStoreAptitudeRolls = new int[0];
            pendingStoreRevealAlreadyGranted = false;
            pendingStarterGrantId = null;
        }

        public Task<ExpeditionServerSelection> LoadChapterOneExpeditionSelectionAsync(
            string seasonId, CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionEntryCoordinator().LoadSelectionAsync(
                session.AccessToken, seasonId, cancellationToken);
        }

        public Task<ExpeditionServerRunSnapshot> RecoverChapterOneExpeditionRunAsync(
            string seasonId, CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionEntryCoordinator().RecoverCurrentRunAsync(
                session.AccessToken, seasonId, cancellationToken);
        }

        public Task<ExpeditionServerRunSnapshot> BeginFreeChapterOneD0Async(
            string seasonId,
            string idempotencyKey,
            ExpeditionLoadoutSnapshotHashes snapshot,
            CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionEntryCoordinator().BeginFreeD0Async(
                session.AccessToken, seasonId, session.WalletAddress, idempotencyKey,
                snapshot, cancellationToken);
        }

        public Task<ExpeditionServerRunSnapshot> BeginPaidChapterOneAsync(
            string seasonId,
            string difficultyId,
            string idempotencyKey,
            ExpeditionLoadoutSnapshotHashes snapshot,
            CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionEntryCoordinator().BeginPaidAsync(
                session.AccessToken, seasonId, difficultyId, session.WalletAddress,
                idempotencyKey, snapshot, cancellationToken);
        }

        public FormalExpeditionClientRunContext CurrentFormalExpeditionRun
        {
            get { return formalExpeditionRunContext; }
        }

        public FormalExpeditionClientRunContext
            RestartRecoveredFormalExpeditionWithoutCheckpoint()
        {
            if (formalExpeditionRunContext == null)
                throw new InvalidOperationException(
                    "Recover an authoritative expedition before restarting its local state.");
            formalExpeditionRunContext =
                RequireFormalExpeditionEntryExecutionCoordinator()
                    .RestartRecoveredRunWithoutCheckpoint(formalExpeditionRunContext);
            return formalExpeditionRunContext;
        }

        public async Task<FormalExpeditionClientRunContext>
            ExecuteFormalChapterOneEntryAsync(
                string seasonId,
                string difficultyId,
                string idempotencyKey,
                ExpeditionLoadoutSnapshotHashes snapshot,
                CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            BeginWalletServerOperation();
            try
            {
                using (var linked = CreateWalletServerLinkedToken(cancellationToken))
                {
                    var context = await RequireFormalExpeditionEntryExecutionCoordinator()
                        .ExecuteAsync(
                            session.AccessToken,
                            session.AccountId,
                            session.WalletAddress,
                            seasonId,
                            difficultyId,
                            idempotencyKey,
                            snapshot,
                            linked.Token);
                    var current = RequireAuthenticatedWalletServerSession();
                    if (!string.Equals(current.AccountId, session.AccountId,
                            StringComparison.Ordinal) ||
                        !string.Equals(current.WalletAddress, session.WalletAddress,
                            StringComparison.Ordinal))
                        throw new InvalidOperationException(
                            "The authenticated wallet session changed during expedition entry.");
                    formalExpeditionRunContext = context;
                    return context;
                }
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public Task<AuthoritativeRewardInventorySnapshot>
            RecoverAuthoritativeRewardInventoryAsync(CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return SynchronizeAuthoritativeRewardsAsync(session, true, cancellationToken);
        }

        public Task<AuthoritativeRewardInventorySnapshot>
            RefreshAuthoritativeRewardInventoryAsync(CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return SynchronizeAuthoritativeRewardsAsync(session, false, cancellationToken);
        }

        public async Task<ExpeditionServerSettlementRecoveryResult>
            SubmitChapterOneSettlementAndRecoverAsync(
                SubmitExpeditionServerSettlement request,
                CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            var result = await RequireExpeditionSettlementCoordinator().SubmitAndRecoverAsync(
                session.AccessToken, session.AccountId, request, cancellationToken);
            if (result != null && result.Inventory != null)
                ClearCompletedFormalReplay(session.AccountId, request.RunId);
            return result;
        }

        public Task<ExpeditionServerProgressCheckpoint> SubmitChapterOneProgressAsync(
            SubmitExpeditionServerProgress request,
            CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionProgressCoordinator().SubmitAsync(
                session.AccessToken, session.AccountId, request, cancellationToken);
        }

        public Task<ExpeditionServerReplayProof> IssueChapterOneCheckpointProofAsync(
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionReplayProofCoordinator().IssueCheckpointAsync(
                session.AccessToken,
                session.AccountId,
                seasonId,
                envelope,
                cancellationToken);
        }

        public Task<ExpeditionServerReplayProof> IssueChapterOneTerminalProofAsync(
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionReplayProofCoordinator().IssueTerminalAsync(
                session.AccessToken,
                session.AccountId,
                seasonId,
                envelope,
                cancellationToken);
        }

        private static FormalExpeditionCommitFailure CreateFormalExpeditionCommitFailure(
            FormalExpeditionCommitPhase phase, Exception exception)
        {
            var aggregate = exception as AggregateException;
            var baseException = aggregate == null
                ? exception ?? new InvalidOperationException(
                    "The formal expedition commit failed without an exception.")
                : aggregate.GetBaseException();
            var existing = baseException as FormalExpeditionCommitFailure;
            if (existing != null) return existing;

            var server = baseException as WalletServerException;
            if (server != null)
            {
                return new FormalExpeditionCommitFailure(phase, server.StatusCode,
                    server.Code, server.RequestId, server);
            }

            return new FormalExpeditionCommitFailure(phase, 0,
                baseException is TimeoutException ? "TRANSPORT_TIMEOUT" :
                    "CLIENT_COMMIT_FAILED",
                string.Empty, baseException);
        }

        public async Task<FormalExpeditionCheckpointCommitResult>
            CommitCurrentFormalExpeditionCheckpointAsync(
                string seasonId,
                string idempotencyKey,
                string completedNodeId,
                CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            var context = RequireCurrentFormalExpeditionRun();
            BeginWalletServerOperation();
            try
            {
                try
                {
                    return await RequireFormalExpeditionAuthorityFlowCoordinator()
                        .CommitCheckpointAsync(
                            session.AccessToken,
                            session.AccountId,
                            seasonId,
                            idempotencyKey,
                            completedNodeId,
                            context.ActionJournal.CreateCheckpointEnvelope(),
                            cancellationToken);
                }
                catch (Exception commitFailure)
                {
                    var failure = CreateFormalExpeditionCommitFailure(
                        FormalExpeditionCommitPhase.Checkpoint, commitFailure);
                    if (!FormalExpeditionCommitRetryPolicy.IsOutcomeUncertain(failure))
                        throw failure;
                    ExpeditionServerProgressCheckpoint recovered = null;
                    try
                    {
                        recovered = await RequireExpeditionProgressCoordinator()
                            .RecoverLatestAsync(session.AccessToken, session.AccountId,
                                context.RunBinding.Source.RunId, cancellationToken);
                    }
                    catch (Exception recoveryFailure)
                    {
                        Debug.LogWarning(
                            "Checkpoint recovery did not replace the original commit failure: " +
                            recoveryFailure.Message);
                    }
                    if (recovered == null || !string.Equals(
                            recovered.CompletedNodeId, completedNodeId,
                            StringComparison.Ordinal)) throw failure;
                    return new FormalExpeditionCheckpointCommitResult
                    {
                        Checkpoint = recovered
                    };
                }
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public async Task<FormalExpeditionTerminalCommitResult>
            CommitCurrentFormalExpeditionTerminalAsync(
                string seasonId,
                string idempotencyKey,
                string terminalReason,
                string terminalNodeId,
                CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            var context = RequireCurrentFormalExpeditionRun();
            BeginWalletServerOperation();
            try
            {
                FormalExpeditionTerminalCommitResult result;
                try
                {
                    result = await RequireFormalExpeditionAuthorityFlowCoordinator()
                        .CommitTerminalAsync(
                            session.AccessToken,
                            session.AccountId,
                            seasonId,
                            idempotencyKey,
                            terminalReason,
                            terminalNodeId,
                            context.ActionJournal.CreateCheckpointEnvelope(),
                            cancellationToken);
                }
                catch (Exception commitFailure)
                {
                    var failure = CreateFormalExpeditionCommitFailure(
                        FormalExpeditionCommitPhase.Terminal, commitFailure);
                    if (!FormalExpeditionCommitRetryPolicy.IsOutcomeUncertain(failure))
                        throw failure;
                    ExpeditionServerSettlementReceipt recovered = null;
                    try
                    {
                        recovered = await RequireExpeditionSettlementCoordinator()
                            .RecoverSettlementAsync(session.AccessToken, session.AccountId,
                                context.RunBinding.Source.RunId, cancellationToken);
                    }
                    catch (Exception recoveryFailure)
                    {
                        Debug.LogWarning(
                            "Settlement recovery did not replace the original commit failure: " +
                            recoveryFailure.Message);
                    }
                    if (recovered == null || recovered.Plan == null ||
                        !string.Equals(recovered.Plan.TerminalReason, terminalReason,
                            StringComparison.Ordinal)) throw failure;
                    result = new FormalExpeditionTerminalCommitResult
                    {
                        Recovery = new ExpeditionServerSettlementRecoveryResult
                        {
                            Settlement = recovered
                        }
                    };
                }
                pendingFormalExpeditionSettlement = result.Recovery.Settlement;
                // Terminal commit only freezes the authoritative manifest. The formal
                // settlement screen must display that manifest before the player
                // explicitly confirms it; wallet signing and inventory recovery happen
                // only from the confirmation action.
                return result;
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public async Task<bool> ClaimOrRecoverFormalExpeditionSettlementAsync(
            string completedRunId, CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            BeginWalletServerOperation();
            try
            {
                var settlement = pendingFormalExpeditionSettlement;
                if (settlement == null || !string.Equals(
                        settlement.RunId, completedRunId, StringComparison.Ordinal))
                    settlement = await RequireExpeditionSettlementCoordinator()
                        .RecoverSettlementAsync(session.AccessToken, session.AccountId,
                            completedRunId, cancellationToken);
                if (settlement.RequiresWalletClaim)
                {
                    if (walletServerGateway == null)
                        throw new InvalidOperationException(
                            "Configure the wallet server before claiming an expedition settlement.");
                    var recovered = await RequireExpeditionSettlementCoordinator()
                        .ClaimAndRecoverAsync(
                            session.AccessToken,
                            session.AccountId,
                            session.WalletAddress,
                            settlement,
                            settlement.SettlementId + ":claim",
                            walletServerGateway,
                            cancellationToken);
                    if (recovered == null || recovered.Inventory == null)
                        throw new InvalidOperationException(
                            "The authoritative settlement inventory recovery is incomplete.");
                }
                else
                {
                    var inventory = await SynchronizeAuthoritativeRewardsAsync(
                        session, true, cancellationToken);
                    if (inventory == null)
                        throw new InvalidOperationException(
                            "The authoritative reward inventory recovery is incomplete.");
                }
                pendingFormalExpeditionSettlement = null;
                ClearCompletedFormalReplay(session.AccountId, completedRunId);
                return true;
            }
            finally
            {
                EndWalletServerOperation();
            }
        }

        public Task<ExpeditionServerProgressCheckpoint> RecoverChapterOneProgressAsync(
            string runId, CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionProgressCoordinator().RecoverLatestAsync(
                session.AccessToken, session.AccountId, runId, cancellationToken);
        }

        public async Task<ExpeditionServerSettlementRecoveryResult>
            ClaimChapterOneSettlementAndRecoverAsync(
                ExpeditionServerSettlementReceipt settlement,
                string idempotencyKey,
                CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            if (walletServerGateway == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before claiming an expedition settlement.");
            var result = await RequireExpeditionSettlementCoordinator().ClaimAndRecoverAsync(
                session.AccessToken, session.AccountId, session.WalletAddress,
                settlement, idempotencyKey, walletServerGateway, cancellationToken);
            if (result != null && result.Inventory != null)
                ClearCompletedFormalReplay(session.AccountId, settlement.RunId);
            return result;
        }

        private void ClearCompletedFormalReplay(string accountId, string runId)
        {
            if (formalExpeditionReplayStateStore != null)
                formalExpeditionReplayStateStore.Clear(accountId, runId);
            if (formalExpeditionRunContext != null &&
                string.Equals(formalExpeditionRunContext.RunBinding.Source.RunId,
                    runId, StringComparison.OrdinalIgnoreCase))
                formalExpeditionRunContext = null;
        }

        public Task<ExpeditionServerSettlementReceipt> RecoverChapterOneSettlementAsync(
            string runId, CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            return RequireExpeditionSettlementCoordinator().RecoverSettlementAsync(
                session.AccessToken, session.AccountId, runId, cancellationToken);
        }

        private Task<AuthoritativeRewardInventorySnapshot> SynchronizeAuthoritativeRewardsAsync(
            WalletAuthenticationSession session, bool recover,
            CancellationToken cancellationToken)
        {
            if (rewardInstanceServerCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before synchronizing rewards.");
            return rewardInstanceServerCoordinator.SynchronizeAsync(
                session.AccessToken, recover, cancellationToken);
        }

        private async Task TrySynchronizeAuthoritativeRewardsAfterAuthenticationAsync(
            WalletAuthenticationSession session, CancellationToken cancellationToken)
        {
            try
            {
                await SynchronizeAuthoritativeRewardsAsync(session, true, cancellationToken);
            }
            catch (WalletServerException exception) when (
                exception.StatusCode == 503 &&
                (string.Equals(exception.Code,
                     "REWARD_INSTANCE_GENERATION_PERSISTENCE_UNAVAILABLE",
                     StringComparison.Ordinal) ||
                 string.Equals(exception.Code,
                     "EXPEDITION_REWARD_PROJECTION_PERSISTENCE_UNAVAILABLE",
                     StringComparison.Ordinal)))
            {
                authoritativeRewardInventoryMirror.Clear();
                Debug.LogWarning(
                    "Post-auth reward synchronization deferred: status=" +
                    exception.StatusCode + " code=" + exception.Code +
                    " requestId=" + (string.IsNullOrWhiteSpace(exception.RequestId)
                        ? "none"
                        : exception.RequestId));
            }
        }

        public Task<LanternWorkshopSnapshot> LoadLanternWorkshopAsync(
            CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            if (lanternWorkshopCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before opening the lantern workshop.");
            return lanternWorkshopCoordinator.LoadAsync(session.AccessToken,
                session.AccountId, cancellationToken);
        }

        public Task<LanternCovenantAdvanceResult> AdvanceLanternCovenantAsync(
            string lanternInstanceId, string optionId, int expectedStateVersion,
            string idempotencyKey, CancellationToken cancellationToken)
        {
            var session = RequireAuthenticatedWalletServerSession();
            if (lanternWorkshopCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before advancing a lantern covenant.");
            return lanternWorkshopCoordinator.AdvanceAsync(session.AccessToken,
                session.AccountId, lanternInstanceId, optionId, expectedStateVersion,
                idempotencyKey, cancellationToken);
        }

        private ExpeditionEntryCoordinator RequireExpeditionEntryCoordinator()
        {
            if (expeditionEntryCoordinator == null)
                throw new InvalidOperationException("Configure the wallet server before using expedition entry.");
            return expeditionEntryCoordinator;
        }

        private FormalExpeditionEntryExecutionCoordinator
            RequireFormalExpeditionEntryExecutionCoordinator()
        {
            if (formalExpeditionEntryExecutionCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before executing expedition entry.");
            return formalExpeditionEntryExecutionCoordinator;
        }

        private ExpeditionSettlementCoordinator RequireExpeditionSettlementCoordinator()
        {
            if (expeditionSettlementCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before settling an expedition.");
            return expeditionSettlementCoordinator;
        }

        private ExpeditionProgressCoordinator RequireExpeditionProgressCoordinator()
        {
            if (expeditionProgressCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before recording expedition progress.");
            return expeditionProgressCoordinator;
        }

        private ExpeditionReplayProofCoordinator RequireExpeditionReplayProofCoordinator()
        {
            if (expeditionReplayProofCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before requesting expedition replay proofs.");
            return expeditionReplayProofCoordinator;
        }

        private FormalExpeditionAuthorityFlowCoordinator
            RequireFormalExpeditionAuthorityFlowCoordinator()
        {
            if (formalExpeditionAuthorityFlowCoordinator == null)
                throw new InvalidOperationException(
                    "Configure the wallet server before committing expedition authority state.");
            return formalExpeditionAuthorityFlowCoordinator;
        }

        private FormalExpeditionClientRunContext RequireCurrentFormalExpeditionRun()
        {
            if (formalExpeditionRunContext == null)
                throw new InvalidOperationException(
                    "Begin or recover a formal expedition before committing its state.");
            return formalExpeditionRunContext;
        }

        private WalletAuthenticationSession RequireAuthenticatedWalletServerSession()
        {
            var authentication = RequireWalletServerAuthentication();
            var session = authentication.CurrentSession;
            if (session == null || walletServerSessionState != WalletServerSessionState.Authenticated)
                throw new InvalidOperationException("Authenticate the wallet server session first.");
            return session;
        }

        private void UpdateWalletServerSessionLifecycle(bool forceCheck = false)
        {
            if (walletServerAuthentication == null ||
                Interlocked.CompareExchange(ref walletServerOperationInProgress, 0, 0) != 0)
                return;
            var session = walletServerAuthentication.CurrentSession;
            if (session == null) return;

            var now = DateTimeOffset.UtcNow;
            if (!forceCheck && now < walletNextRefreshCheckAt) return;
            walletNextRefreshCheckAt = now + WalletRefreshCheckInterval;
            if (WalletSessionRefreshPolicy.ShouldRefresh(
                    session, now, WalletAccessRefreshLeadTime))
            {
                RunAutomaticWalletRefreshAsync();
                return;
            }

            if (WalletSessionRefreshPolicy.IsAccessExpired(session, now))
            {
                ClearBountyTowerServerSession();
                authoritativeRewardInventoryMirror.Clear();
                formalExpeditionRunContext = null;
                pendingFormalExpeditionSettlement = null;
                SetWalletServerState(WalletServerSessionState.Expired, null);
            }
        }

        private async void RunAutomaticWalletRefreshAsync()
        {
            try
            {
                await RefreshWalletServerSessionAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Wallet server session refresh failed: " + exception.Message);
            }
        }

        private WalletAuthenticationCoordinator RequireWalletServerAuthentication()
        {
            if (walletServerAuthentication == null)
                throw new InvalidOperationException("The wallet server runtime is not configured.");
            return walletServerAuthentication;
        }

        private CancellationTokenSource CreateWalletServerLinkedToken(
            CancellationToken cancellationToken)
        {
            if (walletServerLifetime == null)
                throw new InvalidOperationException("The wallet server runtime is not configured.");
            return CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, walletServerLifetime.Token);
        }

        private static void ValidateWalletServerAccount(
            WalletAuthenticationSession session,
            WalletServerAccount account)
        {
            if (account == null ||
                !string.Equals(account.AccountId, session.AccountId, StringComparison.Ordinal) ||
                !string.Equals(account.WalletAddress, session.WalletAddress, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The authenticated wallet session does not match the current server account.");
        }

        public async Task<string> UpdateWalletServerDisplayNameAsync(
            string displayName,
            CancellationToken cancellationToken)
        {
            var authentication = RequireWalletServerAuthentication();
            using (var linked = CreateWalletServerLinkedToken(cancellationToken))
            {
                var account = await authentication.UpdateDisplayNameAsync(displayName, linked.Token);
                ValidateWalletServerAccount(RequireAuthenticatedWalletServerSession(), account);
                walletServerDisplayName = account.DisplayName;
                return walletServerDisplayName;
            }
        }

        private static async Task TryDiscardFailedWalletSessionAsync(
            WalletAuthenticationCoordinator authentication)
        {
            if (authentication == null) return;
            try
            {
                await authentication.SignOutAsync(CancellationToken.None);
            }
            catch (Exception)
            {
                // Failed cleanup keeps encrypted session material so revocation can be retried.
            }
        }

        private void BeginWalletServerOperation()
        {
            if (Interlocked.CompareExchange(ref walletServerOperationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "A wallet server operation is already in progress.");
        }

        private void EndWalletServerOperation()
        {
            Interlocked.Exchange(ref walletServerOperationInProgress, 0);
        }

        private void SetWalletServerState(WalletServerSessionState state, string error)
        {
            walletServerSessionState = state;
            walletServerSessionError = error;
            if (state == WalletServerSessionState.SignedOut ||
                state == WalletServerSessionState.Expired ||
                state == WalletServerSessionState.NotConfigured)
                walletServerDisplayName = null;
            var handler = WalletServerSessionStateChanged;
            if (handler == null) return;
            try
            {
                handler(state, error);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void CancelWalletServerLifetime()
        {
            if (walletServerLifetime == null) return;
            walletServerLifetime.Cancel();
            walletServerLifetime.Dispose();
            walletServerLifetime = null;
        }

        private void ShutdownWalletServerRuntime()
        {
            ShutdownMainnetInviteLogin();
            CancelWalletServerLifetime();
            ClearBountyTowerServerSession();
            authoritativeRewardInventoryMirror.Clear();
            formalExpeditionRunContext = null;
            pendingFormalExpeditionSettlement = null;
            walletServerAuthentication = null;
            walletServerGateway = null;
            expeditionEntryCoordinator = null;
            formalExpeditionEntryExecutionCoordinator = null;
            formalExpeditionReplayStateStore = null;
            expeditionReplayProofCoordinator = null;
            expeditionProgressCoordinator = null;
            rewardInstanceServerCoordinator = null;
            starterCharacterGrantTransport = null;
            lanternWorkshopCoordinator = null;
            expeditionSettlementCoordinator = null;
            formalExpeditionAuthorityFlowCoordinator = null;
            walletServerBaseUrl = null;
            walletServerSessionError = null;
            walletServerSessionState = WalletServerSessionState.NotConfigured;
        }
    }
}
