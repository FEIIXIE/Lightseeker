using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using SeekerAutoChess.Wallet;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private bool formalExpeditionEntryInProgress;
        private string formalExpeditionEntryStatusKey;
        private float formalExpeditionEntryStatusExpiresAt;
        private bool formalExpeditionAuthorityCommitInProgress;
        private bool formalExpeditionAuthorityCommitSucceeded;
        private int formalExpeditionAuthorityRetryCount;
        private bool formalExpeditionSettlementClaimInProgress;
        private const string FormalAuthorityCommitFailureStatusKey =
            "status.authoritativeCommitFailed";

        private async void BeginOrRecoverFormalD0FromHomeAsync()
        {
            if (formalExpeditionEntryInProgress) return;
            if (WalletServerState != WalletServerSessionState.Authenticated ||
                CurrentWalletServerSession == null)
            {
                ShowFormalExpeditionEntryStatus("home.expeditionLoginRequired");
                Debug.LogWarning("Formal expedition entry ignored: wallet session is not authenticated.");
                return;
            }

            formalExpeditionEntryStatusKey = null;
            formalExpeditionEntryInProgress = true;
            var entryStage = 1;
            try
            {
                await SynchronizeAuthoritativeStarterCharactersAsync(
                    CurrentWalletServerSession, CancellationToken.None);
                entryStage = 2;
                var snapshot = CaptureFormalEntrySnapshot();
                entryStage = 3;
                var context = await ExecuteFormalChapterOneEntryAsync(
                    MainnetInviteSeasonId,
                    "D0",
                    "d0-" + Guid.NewGuid().ToString("N"),
                    snapshot,
                    CancellationToken.None);
                entryStage = 4;
                if (context.EntryPlan.Action == FormalExpeditionEntryAction.RecoverActiveRun)
                {
                    if (!TryResumeMatchingLocalFormalRun(context))
                    {
                        var checkpoint = await TryRecoverFormalExpeditionCheckpointAsync(
                            context.RunBinding.Source.RunId);
                        if (checkpoint != null)
                            throw new InvalidOperationException(
                                "The active expedition has authoritative progress that cannot " +
                                "be replaced by stale local state. checkpoint=" +
                                checkpoint.CheckpointId + " node=" + checkpoint.CompletedNodeId +
                                " requestId=" + checkpoint.RequestId);

                        context = RestartRecoveredFormalExpeditionWithoutCheckpoint();
                        InitializeNewFormalExpeditionRun(context);
                        Debug.LogWarning(
                            "Recovered authoritative expedition had no server checkpoint; " +
                            "rebuilt clean local state for run " +
                            context.RunBinding.Source.RunId + ".");
                    }
                }
                else
                {
                    InitializeNewFormalExpeditionRun(context);
                }

                homeTopMenu = 0;
                currentView = ScreenView.Expedition;
                entryStage = 5;
                SaveGame();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                ShowFormalExpeditionEntryStatus(
                    "home.expeditionEntryFailedE" + entryStage);
                Debug.LogWarning("Formal expedition entry failed at E" + entryStage +
                                 ": " + exception.Message);
            }
            finally
            {
                formalExpeditionEntryInProgress = false;
            }
        }

        private void ShowFormalExpeditionEntryStatus(string localizationKey)
        {
            formalExpeditionEntryStatusKey = localizationKey;
            formalExpeditionEntryStatusExpiresAt = Time.realtimeSinceStartup + 5f;
        }

        private ExpeditionLoadoutSnapshotHashes CaptureFormalEntrySnapshot()
        {
            var session = CurrentWalletServerSession;
            if (session == null)
                throw new InvalidOperationException(
                    "An authenticated wallet session is required for formal entry.");
            if (nextRunDeckSelection == null)
                throw new InvalidOperationException("A formal expedition deck is required.");
            var deck = nextRunDeckSelection.CreateSnapshot();
            var equipment = new Dictionary<string, EquipmentLoadoutSnapshot>(
                StringComparer.Ordinal);
            for (var index = 0; index < deck.UnitDefinitionIds.Count; index++)
            {
                var definitionId = deck.UnitDefinitionIds[index];
                var characterInstanceId = deck.CharacterInstanceIds[index];
                OwnedCharacterState character;
                if (!playerCollection.TryGetByInstanceId(characterInstanceId, out character) ||
                    !string.Equals(character.DefinitionId, definitionId,
                        StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The selected formal deck contains an unavailable character instance.");
                equipment.Add(definitionId,
                    equipmentInventory.CreateSnapshot(characterInstanceId));
            }
            return FormalExpeditionSnapshotContract.Capture(
                deck, equipment, CurrentLanternSnapshot(), session.AccountId,
                MainnetInviteSeasonId);
        }

        private bool TryResumeMatchingLocalFormalRun(
            FormalExpeditionClientRunContext context)
        {
            var binding = context.RunBinding;
            var restored = FormalExpeditionSnapshotContract.RestoreAuthoritative(
                binding.Source);
            if (!SameGuid(runId, binding.Source.RunId))
                return RejectFormalExpeditionResume("local run id mismatch", binding.Source.RunId);
            if (expeditionMap == null)
                return RejectFormalExpeditionResume("local map is missing", binding.Source.RunId);
            if (expeditionMapSeed != binding.MapSeed)
                return RejectFormalExpeditionResume("local map seed mismatch", binding.Source.RunId);
            if (qualityRecruitPriceRulesVersion !=
                binding.Source.QualityRecruitPriceRulesVersion)
                return RejectFormalExpeditionResume(
                    "local recruit-price rules mismatch", binding.Source.RunId);
            if (expeditionFlow.IsComplete || runAbandoned || runWon)
                return RejectFormalExpeditionResume(
                    "local run is already terminal", binding.Source.RunId);
            if (runDeck == null || runEquipmentSnapshots == null ||
                expeditionLanternSnapshot == null)
                return RejectFormalExpeditionResume(
                    "local loadout state is incomplete", binding.Source.RunId);

            var session = CurrentWalletServerSession;
            if (session == null)
                return RejectFormalExpeditionResume(
                    "wallet session disappeared", binding.Source.RunId);
            ExpeditionLoadoutSnapshotHashes local;
            try
            {
                local = FormalExpeditionSnapshotContract.Capture(
                    runDeck, runEquipmentSnapshots, expeditionLanternSnapshot,
                    session.AccountId, MainnetInviteSeasonId);
            }
            catch (Exception)
            {
                return RejectFormalExpeditionResume(
                    "local loadout snapshot is invalid", binding.Source.RunId);
            }
            if (!string.Equals(local.CombatInputHash, restored.Hashes.CombatInputHash,
                    StringComparison.Ordinal))
                return RejectFormalExpeditionResume(
                    "local loadout hash mismatch", binding.Source.RunId);

            expeditionRewardSnapshot = binding.Rewards;
            expeditionLanternSnapshot = restored.Lantern;
            currentExpeditionRewardsEnabled = false;
            expeditionEntryCommitted = true;
            return true;
        }

        private async Task<ExpeditionServerProgressCheckpoint>
            TryRecoverFormalExpeditionCheckpointAsync(string authoritativeRunId)
        {
            try
            {
                return await RecoverChapterOneProgressAsync(
                    authoritativeRunId, CancellationToken.None);
            }
            catch (WalletServerException exception) when (exception.StatusCode == 404)
            {
                Debug.LogWarning(
                    "No authoritative checkpoint exists for recovered expedition run " +
                    authoritativeRunId + "; requestId=" +
                    (string.IsNullOrWhiteSpace(exception.RequestId)
                        ? "none"
                        : exception.RequestId) + ".");
                return null;
            }
        }

        private static bool SameGuid(string left, string right)
        {
            Guid leftGuid;
            Guid rightGuid;
            return Guid.TryParse(left, out leftGuid) && leftGuid != Guid.Empty &&
                   Guid.TryParse(right, out rightGuid) && leftGuid == rightGuid;
        }

        private static bool RejectFormalExpeditionResume(
            string reason, string authoritativeRunId)
        {
            Debug.LogWarning(
                "Formal expedition local resume rejected: " + reason +
                "; authoritativeRunId=" + authoritativeRunId + ".");
            return false;
        }

        private void InitializeNewFormalExpeditionRun(
            FormalExpeditionClientRunContext context)
        {
            var binding = context.RunBinding;
            var restored = FormalExpeditionSnapshotContract.RestoreAuthoritative(
                binding.Source);
            ValidateFormalDeckOwnership(restored.Deck);
            var restoredEquipment = BuildFormalEquipmentSnapshots(restored);

            ReleaseRunEquipmentLocks();
            runDeck = restored.Deck;
            activeDeckUnitIds = CopyIds(runDeck.UnitDefinitionIds);
            runEquipmentSnapshots = restoredEquipment;
            foreach (var snapshot in runEquipmentSnapshots.Values)
                foreach (var equipmentId in snapshot.EquipmentInstanceIds)
                    if (!equipmentInventory.TryAddUsageLock(
                            equipmentId, EquipmentUsageLock.Expedition))
                        throw new InvalidOperationException(
                            "The formal expedition equipment could not be locked.");

            runRoster.Clear();
            coins = 10;
            health = ChapterOneExpeditionLifeRules.InitialLife;
            round = 1;
            expeditionMapSeed = binding.MapSeed;
            expeditionMap = ExpeditionMapGraph.CreateChapterOne(expeditionMapSeed);
            currentMapNodeId = expeditionMap.GetLayer(1)[0].Id;
            expeditionMapScroll = Vector2.zero;
            expeditionMapScrollAnchorNodeId = null;
            expeditionMapZoom = 1f;
            expeditionMapPinchDistance = 0f;
            runAttackBonus = 0;
            runHealthBonus = 0;
            roguelikeBuild.Reset();
            encounterPowerBonus = 0;
            encounterReward = 4;
            currentEncounterIsElite = false;
            selectedRunSlot = -1;
            shopRefreshSerial = 0;
            shopOfferLevels = new int[ShopOfferCount];
            shopOfferIds = new string[ShopOfferCount];
            shopOfferListedPrices = new int[ShopOfferCount];
            shopOfferSold = new bool[ShopOfferCount];
            runId = binding.Source.RunId;
            currentRunEconomyRulesVersion = RunEconomyRulesVersion;
            qualityRecruitPriceRulesVersion = binding.Source.QualityRecruitPriceRulesVersion;
            expeditionLanternSnapshot = restored.Lantern;
            expeditionRewardSnapshot = binding.Rewards;
            runEconomyActions.Reset();
            expeditionEconomy.Reset();
            preparationBackpackExpanded = false;
            preparationShopOpen = true;
            ClosePreparationUnitDetail();
            ResetPreparationPointer();
            confirmAbandon = false;
            battlePresentation.Reset();
            battlePaused = false;
            battleControlsEnabled = false;
            battleSceneOpacity = 0f;
            battlePresentationActive = false;
            expeditionFlow.BeginAtRoute();
            runWon = false;
            runAbandoned = false;
            finalScore = 0;
            currentExpeditionRewardsEnabled = false;
            expeditionEntryCommitted = true;
            expeditionRewards.Reset();
            statusKey = "status.route";
            CreateEnemies();
            RefreshAllShopOffers();
        }

        private void ValidateFormalDeckOwnership(RunDeckSnapshot deck)
        {
            for (var index = 0; index < deck.UnitDefinitionIds.Count; index++)
            {
                OwnedCharacterState character;
                if (!playerCollection.TryGetByInstanceId(
                        deck.CharacterInstanceIds[index], out character) ||
                    !string.Equals(character.DefinitionId,
                        deck.UnitDefinitionIds[index], StringComparison.Ordinal))
                    throw new InvalidOperationException(
                        "The authoritative expedition deck is unavailable on this client.");
            }
        }

        private Dictionary<string, EquipmentLoadoutSnapshot> BuildFormalEquipmentSnapshots(
            RestoredFormalExpeditionSnapshot restored)
        {
            var result = new Dictionary<string, EquipmentLoadoutSnapshot>(
                StringComparer.Ordinal);
            var globallyUsed = new HashSet<string>(StringComparer.Ordinal);
            foreach (var definitionId in restored.Deck.UnitDefinitionIds)
            {
                string[] ids;
                if (!restored.EquipmentInstanceIdsByDefinition.TryGetValue(
                        definitionId, out ids))
                    throw new InvalidOperationException(
                        "The authoritative equipment snapshot is incomplete.");
                var modifier = new EquipmentStatModifier();
                foreach (var equipmentId in ids)
                {
                    if (!globallyUsed.Add(equipmentId) ||
                        !equipmentInventory.Contains(equipmentId))
                        throw new InvalidOperationException(
                            "The authoritative expedition equipment is unavailable.");
                    var instance = equipmentInventory.Get(equipmentId);
                    if ((instance.UsageLocks & EquipmentUsageLock.Market) != 0)
                        throw new InvalidOperationException(
                            "Market-listed equipment cannot enter a formal expedition.");
                    modifier = EquipmentStatModifier.Add(
                        modifier, equipmentCatalog.Get(instance.TemplateId).Modifier);
                }
                result.Add(definitionId,
                    new EquipmentLoadoutSnapshot(ids, modifier));
            }
            return result;
        }

        private bool IsCurrentFormalExpeditionRunBound()
        {
            return IsMainnetInviteRuntimeConfigured &&
                   CurrentFormalExpeditionRun != null &&
                   CurrentFormalExpeditionRun.RunBinding != null &&
                   CurrentFormalExpeditionRun.RunBinding.Source != null &&
                   SameGuid(CurrentFormalExpeditionRun.RunBinding.Source.RunId, runId);
        }

        private string CurrentEncounterTableVersion()
        {
            return IsCurrentFormalExpeditionRunBound()
                ? CurrentFormalExpeditionRun.RunBinding.Source.EncounterTableVersion
                : ChapterOneDifficultyRules.EncounterTableVersion;
        }

        private void RecordFormalRouteSelection(string sourceNodeId,
            string destinationNodeId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordRouteSelection(
                NewFormalExpeditionActionId("route"),
                runEconomyActions.StateVersion,
                sourceNodeId,
                destinationNodeId);
        }

        private void RecordFormalShopRefresh(
            string actionId, int expectedStateVersion, string refreshId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordShopRefresh(
                actionId, expectedStateVersion, currentMapNodeId, refreshId);
        }

        private void RecordFormalShopPurchase(
            string actionId, int expectedStateVersion, string offerId,
            string runUnitId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordShopPurchase(
                actionId, expectedStateVersion, currentMapNodeId,
                offerId, runUnitId);
        }

        private void RecordFormalMerchantPurchase(
            string actionId, int expectedStateVersion, string offerId,
            string itemInstanceId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordMerchantPurchase(
                actionId, expectedStateVersion, currentMapNodeId,
                offerId, itemInstanceId);
        }

        private void RecordFormalItemBinding(
            string actionId, int expectedStateVersion, string itemInstanceId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordItemBinding(
                actionId, expectedStateVersion, currentMapNodeId,
                itemInstanceId, "side-player", "global");
        }

        private void RecordFormalItemDiscard(
            string actionId, int expectedStateVersion, string itemInstanceId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordItemDiscard(
                actionId, expectedStateVersion, currentMapNodeId, itemInstanceId);
        }

        private void RecordFormalRosterSale(
            string actionId, int expectedStateVersion, string runUnitId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordRosterSale(
                actionId, expectedStateVersion, currentMapNodeId, runUnitId);
        }

        private void RecordFormalRosterMove(
            string actionId, int expectedStateVersion, string runUnitId,
            int destinationIndex)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordRosterMove(
                actionId, expectedStateVersion, currentMapNodeId,
                runUnitId, FormalRosterSlotId(destinationIndex));
        }

        private void RecordFormalRosterMerge(
            string actionId, int expectedStateVersion,
            string sourceUnitIdA, string sourceUnitIdB,
            string sourceUnitIdC, string resultUnitId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordRosterMerge(
                actionId, expectedStateVersion, currentMapNodeId,
                sourceUnitIdA, sourceUnitIdB, sourceUnitIdC, resultUnitId);
        }

        private void RecordFormalNodeChoice(
            string actionId, int expectedStateVersion, string nodeId,
            string choiceId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordNodeChoice(
                actionId, expectedStateVersion, nodeId, choiceId);
        }

        private void RecordFormalUpgradeSelection(
            string actionId, int expectedStateVersion, string upgradeId)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.RecordUpgradeSelection(
                actionId,
                expectedStateVersion,
                currentMapNodeId,
                upgradeId);
        }

        private void RecordFormalBattleResolution(BattleInput input, BattleResult result)
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            if (input == null || result == null || currentEncounter == null)
                throw new InvalidOperationException(
                    "A formal battle requires a deterministic encounter result.");
            CurrentFormalExpeditionRun.ActionJournal.RecordBattleResolutionDeferred(
                NewFormalExpeditionActionId("battle"),
                runEconomyActions.StateVersion,
                currentMapNodeId,
                currentEncounter.NodeId + ":" +
                currentEncounter.GenerationVersion + ":" +
                currentEncounter.BattleSeed,
                input.Seed,
                FormalBattleReplayHashContract.ComputeInputSha256(input),
                FormalBattleReplayHashContract.ComputeEventSha256(result.Events),
                result.PlayerWon
                    ? FormalExpeditionBattleWinner.Player
                    : FormalExpeditionBattleWinner.Enemy);
        }

        private void PersistFormalBattleResolution()
        {
            if (!IsCurrentFormalExpeditionRunBound()) return;
            CurrentFormalExpeditionRun.ActionJournal.PersistCurrentState();
        }

        private static string NewFormalExpeditionActionId(string kind)
        {
            return kind + "-" + Guid.NewGuid().ToString("N");
        }

        private IEnumerator CommitFormalBattleAuthority(
            bool playerWon, bool runEnded)
        {
            if (!IsCurrentFormalExpeditionRunBound() ||
                (!playerWon && !runEnded)) yield break;

            var terminalReason = runEnded
                ? playerWon ? "BossCleared" : "ExpeditionLifeZero"
                : null;
            var operation = terminalReason == null ? "checkpoint" : "terminal";
            var idempotencyKey = runId + ":" + operation + ":" + currentMapNodeId;
            formalExpeditionAuthorityCommitInProgress = true;
            formalExpeditionAuthorityCommitSucceeded = false;
            formalExpeditionAuthorityRetryCount = 0;
            try
            {
                var commitStartedAt = Time.realtimeSinceStartup;
                var completedAttempts = 0;
                var committed = false;
                using (var overallTimeout = new CancellationTokenSource(
                           TimeSpan.FromSeconds(
                               FormalExpeditionCommitRetryPolicy.MaximumElapsedSeconds)))
                {
                    while (true)
                    {
                        statusKey = formalExpeditionAuthorityRetryCount == 0
                            ? "status.authoritativeCommitInProgress"
                            : "status.authoritativeCommitRetrying";
                        Task commit;
                        try
                        {
                            commit = terminalReason == null
                                ? (Task)CommitCurrentFormalExpeditionCheckpointAsync(
                                    MainnetInviteSeasonId, idempotencyKey,
                                    currentMapNodeId, overallTimeout.Token)
                                : CommitCurrentFormalExpeditionTerminalAsync(
                                    MainnetInviteSeasonId, idempotencyKey,
                                    terminalReason, currentMapNodeId,
                                    overallTimeout.Token);
                        }
                        catch (Exception exception)
                        {
                            Debug.LogWarning(
                                "Formal expedition authority commit could not start: " +
                                exception.Message);
                            commit = Task.FromException(exception);
                        }

                        while (!commit.IsCompleted) yield return null;
                        if (!commit.IsFaulted && !commit.IsCanceled)
                        {
                            committed = true;
                            break;
                        }

                        completedAttempts++;
                        var commitException = commit.Exception == null
                            ? new OperationCanceledException(
                                "The formal expedition commit was cancelled.")
                            : commit.Exception.GetBaseException();
                        var phase = terminalReason == null
                            ? FormalExpeditionCommitPhase.Checkpoint
                            : FormalExpeditionCommitPhase.Terminal;
                        var failure = CreateFormalExpeditionCommitFailure(
                            phase, commitException);
                        var elapsed = Time.realtimeSinceStartup - commitStartedAt;
                        if (!FormalExpeditionCommitRetryPolicy.CanRetry(
                                failure, completedAttempts, elapsed))
                        {
                            statusKey = FormalAuthorityCommitFailureStatusKey;
                            Debug.LogError("Formal expedition authority commit stopped: " +
                                           failure.Message);
                            yield break;
                        }

                        formalExpeditionAuthorityRetryCount = completedAttempts;
                        Debug.LogWarning("Formal expedition authority commit will retry: " +
                                         failure.Message);
                        var jitter = UnityEngine.Random.Range(-0.25f, 0.25f);
                        var delay = Mathf.Clamp(
                            FormalExpeditionCommitRetryPolicy.RetryDelaySeconds(
                                completedAttempts) + jitter,
                            0.5f, 8f);
                        var waited = 0f;
                        while (waited < delay && !overallTimeout.IsCancellationRequested)
                        {
                            waited += Time.unscaledDeltaTime;
                            yield return null;
                        }
                    }
                }
                formalExpeditionAuthorityCommitSucceeded = committed;
            }
            finally
            {
                formalExpeditionAuthorityCommitInProgress = false;
                formalExpeditionAuthorityRetryCount = 0;
            }
        }

        private void ReturnAfterFormalAuthorityCommitFailure()
        {
            // Do not advance the local map after an uncommitted authoritative battle.
            // No save is written while the flow remains in Battle, so the next entry
            // can recover from the server rather than treating this result as local truth.
            battleControlsEnabled = false;
            battlePaused = false;
            battlePresentationActive = false;
            battleSceneOpacity = 0f;
            battlePresentation.Reset();
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            c05BarrierOwnerByColumn.Clear();
            confirmAbandon = false;
            currentView = ScreenView.Home;
        }

        private bool CanCommitCurrentFormalAbandonment()
        {
            if (!IsCurrentFormalExpeditionRunBound() || expeditionFlow.IsBattling)
                return false;
            var envelope = CurrentFormalExpeditionRun.ActionJournal
                .CreateCheckpointEnvelope();
            if (envelope.Actions == null || envelope.Actions.Length == 0)
                return false;
            var finalAction = envelope.Actions[envelope.Actions.Length - 1];
            return string.Equals(finalAction.Kind, "battle.resolve",
                       StringComparison.Ordinal) &&
                   string.Equals(finalAction.NodeId, currentMapNodeId,
                       StringComparison.Ordinal);
        }

        private IEnumerator CommitFormalAbandonment()
        {
            formalExpeditionAuthorityCommitInProgress = true;
            statusKey = "status.authoritativeCommitInProgress";
            var idempotencyKey = runId + ":terminal-abandon:" + currentMapNodeId;
            var commit = CommitCurrentFormalExpeditionTerminalAsync(
                MainnetInviteSeasonId, idempotencyKey, "PlayerAbandoned",
                currentMapNodeId, CancellationToken.None);
            while (!commit.IsCompleted) yield return null;
            formalExpeditionAuthorityCommitInProgress = false;
            if (commit.IsFaulted || commit.IsCanceled)
            {
                var failure = commit.Exception == null
                    ? "cancelled"
                    : commit.Exception.GetBaseException().Message;
                Debug.LogWarning("Formal expedition abandonment failed: " + failure);
                statusKey = "status.authoritativeAbandonFailed";
                yield break;
            }
            CompleteLocalAbandonRun();
        }

        private async void ClaimFormalSettlementAndReturnHomeAsync()
        {
            if (formalExpeditionSettlementClaimInProgress) return;
            formalExpeditionSettlementClaimInProgress = true;
            statusKey = "status.authoritativeSettlementClaimInProgress";
            try
            {
                if (await ClaimOrRecoverFormalExpeditionSettlementAsync(
                        runId, CancellationToken.None))
                {
                    currentView = ScreenView.Home;
                    SaveGame();
                }
            }
            catch (OperationCanceledException)
            {
                statusKey = "status.authoritativeSettlementClaimPending";
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Formal settlement claim/recovery failed: " +
                                 exception.Message);
                statusKey = "status.authoritativeSettlementClaimFailed";
            }
            finally
            {
                formalExpeditionSettlementClaimInProgress = false;
            }
        }

        private string FormalRosterSlotId(int index)
        {
            if (index < 0 || index >= runRoster.TotalCapacity)
                throw new ArgumentOutOfRangeException(nameof(index));
            return index < runRoster.BoardCapacity
                ? "board-" + index
                : "bench-" + (index - runRoster.BoardCapacity);
        }
    }
}
