using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    public sealed class FormalExpeditionClientRunContext
    {
        internal FormalExpeditionClientRunContext(
            FormalExpeditionEntryPlan entryPlan,
            AuthoritativeExpeditionRunBinding runBinding,
            ExpeditionReplayRecorder replayRecorder,
            Action<ExpeditionReplayRecorderState> persistReplayState = null)
        {
            EntryPlan = entryPlan ?? throw new ArgumentNullException(nameof(entryPlan));
            RunBinding = runBinding ?? throw new ArgumentNullException(nameof(runBinding));
            ReplayRecorder = replayRecorder ??
                throw new ArgumentNullException(nameof(replayRecorder));
            ActionJournal = new FormalExpeditionActionJournal(
                ReplayRecorder, persistReplayState);
        }

        public FormalExpeditionEntryPlan EntryPlan { get; private set; }
        public AuthoritativeExpeditionRunBinding RunBinding { get; private set; }
        public ExpeditionReplayRecorder ReplayRecorder { get; private set; }
        public FormalExpeditionActionJournal ActionJournal { get; private set; }
    }

    /// <summary>
    /// Executes a validated formal entry plan and returns only a fully bound run context.
    /// Callers never receive an unverified server run and cannot start action recording
    /// before account, wallet, season, loadout and seed contracts all match.
    /// </summary>
    public sealed class FormalExpeditionEntryExecutionCoordinator
    {
        private readonly ExpeditionEntryCoordinator entryCoordinator;
        private readonly IExpeditionReplayStateStore replayStateStore;
        private int operationInProgress;

        public FormalExpeditionEntryExecutionCoordinator(
            ExpeditionEntryCoordinator entryCoordinator,
            IExpeditionReplayStateStore replayStateStore = null)
        {
            this.entryCoordinator = entryCoordinator ??
                throw new ArgumentNullException(nameof(entryCoordinator));
            this.replayStateStore = replayStateStore;
        }

        public async Task<FormalExpeditionClientRunContext> ExecuteAsync(
            string accessToken,
            string accountId,
            string walletAddress,
            string seasonId,
            string requestedDifficultyId,
            string idempotencyKey,
            ExpeditionLoadoutSnapshotHashes snapshotHashes,
            CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "A formal expedition entry operation is already in progress.");
            try
            {
                RequireIdentity(accessToken, accountId, walletAddress, seasonId);
                var selection = await entryCoordinator.LoadSelectionAsync(
                    accessToken, seasonId, cancellationToken);
                var plan = FormalExpeditionEntryPreparationContract.Prepare(
                    selection, seasonId, requestedDifficultyId, snapshotHashes);

                ExpeditionServerRunSnapshot run;
                ExpeditionLoadoutSnapshotHashes bindingHashes;
                switch (plan.Action)
                {
                    case FormalExpeditionEntryAction.RecoverActiveRun:
                        run = await entryCoordinator.RecoverCurrentRunAsync(
                            accessToken, seasonId, cancellationToken);
                        RequireRecoveredRun(plan.ActiveRunId, run);
                        var restored = FormalExpeditionSnapshotContract
                            .RestoreAuthoritative(run);
                        bindingHashes = restored.Hashes;
                        plan = new FormalExpeditionEntryPlan(
                            FormalExpeditionEntryAction.RecoverActiveRun,
                            seasonId, null, plan.ActiveRunId, bindingHashes);
                        break;
                    case FormalExpeditionEntryAction.BeginFreeD0:
                        RequireIdempotencyKey(idempotencyKey);
                        run = await entryCoordinator.BeginFreeD0Async(
                            accessToken, seasonId, walletAddress, idempotencyKey,
                            snapshotHashes, cancellationToken);
                        bindingHashes = snapshotHashes;
                        break;
                    case FormalExpeditionEntryAction.BeginPaidDifficulty:
                        RequireIdempotencyKey(idempotencyKey);
                        run = await entryCoordinator.BeginPaidAsync(
                            accessToken, seasonId, plan.DifficultyId, walletAddress,
                            idempotencyKey, snapshotHashes, cancellationToken);
                        bindingHashes = snapshotHashes;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "The formal expedition entry action is unsupported.");
                }

                var binding = FormalExpeditionSnapshotContract.Bind(
                    run, accountId, walletAddress, seasonId, bindingHashes);
                var recorder = RestoreOrStartRecorder(plan, binding);
                Action<ExpeditionReplayRecorderState> persist = null;
                if (replayStateStore != null)
                {
                    persist = state => replayStateStore.Save(state);
                    replayStateStore.Save(recorder.CaptureState());
                }
                return new FormalExpeditionClientRunContext(
                    plan, binding, recorder, persist);
            }
            finally
            {
                Interlocked.Exchange(ref operationInProgress, 0);
            }
        }

        public FormalExpeditionClientRunContext RestartRecoveredRunWithoutCheckpoint(
            FormalExpeditionClientRunContext context)
        {
            if (context == null || context.EntryPlan == null ||
                context.EntryPlan.Action != FormalExpeditionEntryAction.RecoverActiveRun ||
                context.RunBinding == null || context.RunBinding.Source == null)
                throw new InvalidOperationException(
                    "Only a recovered authoritative expedition can be restarted.");

            var binding = context.RunBinding;
            var accountId = binding.Source.AccountId;
            var runId = binding.Source.RunId;
            Action<ExpeditionReplayRecorderState> persist = null;
            if (replayStateStore != null)
            {
                replayStateStore.Clear(accountId, runId);
                persist = state => replayStateStore.Save(state);
            }

            var recorder = ExpeditionReplayRecorder.Start(binding);
            if (replayStateStore != null)
                replayStateStore.Save(recorder.CaptureState());
            return new FormalExpeditionClientRunContext(
                context.EntryPlan, binding, recorder, persist);
        }

        private ExpeditionReplayRecorder RestoreOrStartRecorder(
            FormalExpeditionEntryPlan plan,
            AuthoritativeExpeditionRunBinding binding)
        {
            if (replayStateStore == null)
                return ExpeditionReplayRecorder.Start(binding);
            var accountId = binding.Source.AccountId;
            var runId = binding.Source.RunId;
            if (plan.Action == FormalExpeditionEntryAction.RecoverActiveRun)
            {
                var state = replayStateStore.Load(accountId, runId);
                return state == null
                    ? ExpeditionReplayRecorder.Start(binding)
                    : ExpeditionReplayRecorder.Restore(binding, state);
            }
            replayStateStore.Clear(accountId, runId);
            return ExpeditionReplayRecorder.Start(binding);
        }

        private static void RequireIdentity(
            string accessToken,
            string accountId,
            string walletAddress,
            string seasonId)
        {
            Guid parsedAccountId;
            if (string.IsNullOrWhiteSpace(accessToken) ||
                !Guid.TryParse(accountId, out parsedAccountId) ||
                string.IsNullOrWhiteSpace(walletAddress) ||
                string.IsNullOrWhiteSpace(seasonId))
                throw new InvalidOperationException(
                    "An authenticated account, wallet and season are required.");
        }

        private static void RequireIdempotencyKey(string idempotencyKey)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey) ||
                idempotencyKey.Length > 128)
                throw new InvalidOperationException(
                    "A bounded formal expedition idempotency key is required.");
        }

        private static void RequireRecoveredRun(
            string expectedRunId,
            ExpeditionServerRunSnapshot run)
        {
            Guid expected;
            Guid actual;
            if (!Guid.TryParse(expectedRunId, out expected) || run == null ||
                !Guid.TryParse(run.RunId, out actual) || expected != actual)
                throw new InvalidOperationException(
                    "The recovered expedition run does not match the active run.");
        }
    }
}
