using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class FormalExpeditionAuthorityFlowTests
    {
        private const string AccountId = "33333333-3333-3333-3333-333333333333";
        private const string RunId = "11111111-1111-1111-1111-111111111111";

        [Test]
        public void CheckpointUsesOnlyAuthorityProofAndCommitsMatchingNode()
        {
            var envelope = CreateEnvelope("M01");
            var proofTransport = new FakeProofTransport(envelope);
            var progressTransport = new FakeProgressTransport
            {
                Response = new ExpeditionServerProgressCheckpoint
                {
                    CheckpointId = "22222222-2222-2222-2222-222222222222",
                    RunId = envelope.RunId,
                    // Server response preserves the authenticated account id format.
                    AccountId = AccountId,
                    IdempotencyKey = "checkpoint-m01",
                    CompletedNodeId = "M01",
                    CompletedNodeOrdinal = 1,
                    ProgressRank = 1,
                    CombatResultDigest = proofTransport.Response.CombatResultDigest,
                    RecordedAt = "2026-10-09T00:00:00Z",
                    RequestId = "req-progress"
                }
            };
            var flow = CreateFlow(proofTransport, progressTransport,
                new FakeSettlementTransport());

            var result = flow.CommitCheckpointAsync(
                    "access-token", AccountId, "season-a", "checkpoint-m01",
                    "M01", envelope, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(proofTransport.CheckpointCalls, Is.EqualTo(1));
            Assert.That(proofTransport.TerminalCalls, Is.EqualTo(0));
            Assert.That(progressTransport.Calls, Is.EqualTo(1));
            Assert.That(progressTransport.Request.CombatResultDigest,
                Is.EqualTo(proofTransport.Response.CombatResultDigest));
            Assert.That(progressTransport.Request.CombatProofReference,
                Is.EqualTo(proofTransport.Response.CombatProofReference));
            Assert.That(result.Checkpoint.CompletedNodeId, Is.EqualTo("M01"));
        }

        [Test]
        public void MismatchedCheckpointNodeFailsBeforeProofOrProgress()
        {
            var envelope = CreateEnvelope("M01");
            var proofTransport = new FakeProofTransport(envelope);
            var progressTransport = new FakeProgressTransport();
            var flow = CreateFlow(proofTransport, progressTransport,
                new FakeSettlementTransport());

            Assert.Throws<InvalidOperationException>(() =>
                flow.CommitCheckpointAsync(
                        "access-token", AccountId, "season-a", "checkpoint-m02",
                        "M02", envelope, CancellationToken.None)
                    .GetAwaiter().GetResult());
            Assert.That(proofTransport.CheckpointCalls, Is.EqualTo(0));
            Assert.That(progressTransport.Calls, Is.EqualTo(0));
        }

        [Test]
        public void TerminalProofIsForwardedDirectlyIntoSettlement()
        {
            var envelope = CreateEnvelope("M29");
            var proofTransport = new FakeProofTransport(envelope);
            var settlementTransport = new FakeSettlementTransport
            {
                Response = new ExpeditionServerSettlementReceipt
                {
                    SettlementId = "55555555-5555-5555-5555-555555555555",
                    RunId = envelope.RunId,
                    // Server response preserves the authenticated account id format.
                    AccountId = AccountId,
                    IdempotencyKey = "terminal-m29",
                    Plan = new ExpeditionServerSettlementPlan
                    {
                        TerminalReason = "BossCleared",
                        ProgressRank = 26,
                        RewardTableVersion = "chapter01.reward.v1"
                    },
                    SeedRevealHex = new string('a', 64),
                    Ledger = new ExpeditionServerRewardLedgerLine[0],
                    RewardManifestHash = new string('b', 64),
                    RequiresWalletClaim = true,
                    RequestId = "req-settlement"
                }
            };
            var flow = CreateFlow(proofTransport, new FakeProgressTransport(),
                settlementTransport);

            var result = flow.CommitTerminalAsync(
                    "access-token", AccountId, "season-a", "terminal-m29",
                    "BossCleared", "M29", envelope, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(proofTransport.TerminalCalls, Is.EqualTo(1));
            Assert.That(settlementTransport.SubmitCalls, Is.EqualTo(1));
            Assert.That(settlementTransport.Request.CombatResultDigest,
                Is.EqualTo(proofTransport.Response.CombatResultDigest));
            Assert.That(settlementTransport.Request.CombatProofReference,
                Is.EqualTo(proofTransport.Response.CombatProofReference));
            Assert.That(result.Recovery.Settlement.Plan.TerminalReason,
                Is.EqualTo("BossCleared"));
        }

        [Test]
        public void NonBossReasonCannotSettleBossNode()
        {
            var envelope = CreateEnvelope("M29");
            var proofTransport = new FakeProofTransport(envelope);
            var settlementTransport = new FakeSettlementTransport();
            var flow = CreateFlow(proofTransport, new FakeProgressTransport(),
                settlementTransport);

            Assert.Throws<InvalidOperationException>(() =>
                flow.CommitTerminalAsync(
                        "access-token", AccountId, "season-a", "terminal-m29",
                        "ExpeditionLifeZero", "M29", envelope,
                        CancellationToken.None)
                    .GetAwaiter().GetResult());
            Assert.That(proofTransport.TerminalCalls, Is.EqualTo(0));
            Assert.That(settlementTransport.SubmitCalls, Is.EqualTo(0));
        }

        [Test]
        public void PlayerAbandonedReasonUsesSignedTerminalFlow()
        {
            var envelope = CreateEnvelope("M12");
            var proofTransport = new FakeProofTransport(envelope);
            var settlementTransport = new FakeSettlementTransport
            {
                Response = new ExpeditionServerSettlementReceipt
                {
                    SettlementId = "55555555-5555-5555-5555-555555555555",
                    RunId = envelope.RunId,
                    AccountId = AccountId,
                    IdempotencyKey = "terminal-abandon-m12",
                    Plan = new ExpeditionServerSettlementPlan
                    {
                        TerminalReason = "PlayerAbandoned",
                        ProgressRank = 6,
                        RewardTableVersion = "chapter01.reward.v1"
                    },
                    SeedRevealHex = new string('a', 64),
                    Ledger = new ExpeditionServerRewardLedgerLine[0],
                    RewardManifestHash = new string('b', 64),
                    RequiresWalletClaim = true,
                    RequestId = "req-settlement"
                }
            };
            var flow = CreateFlow(proofTransport, new FakeProgressTransport(),
                settlementTransport);

            var result = flow.CommitTerminalAsync(
                    "access-token", AccountId, "season-a", "terminal-abandon-m12",
                    "PlayerAbandoned", "M12", envelope, CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(proofTransport.TerminalCalls, Is.EqualTo(1));
            Assert.That(settlementTransport.SubmitCalls, Is.EqualTo(1));
            Assert.That(settlementTransport.Request.TerminalReason,
                Is.EqualTo("PlayerAbandoned"));
            Assert.That(result.Recovery.Settlement.Plan.TerminalReason,
                Is.EqualTo("PlayerAbandoned"));
        }

        [TestCase(400)]
        [TestCase(401)]
        [TestCase(403)]
        [TestCase(404)]
        [TestCase(409)]
        [TestCase(422)]
        public void DeterministicHttpFailureDoesNotRetryOrRecover(long statusCode)
        {
            var failure = new FormalExpeditionCommitFailure(
                FormalExpeditionCommitPhase.Checkpoint, statusCode,
                "REQUEST_REJECTED", "req-400", null);

            Assert.That(failure.HttpStatus, Is.EqualTo(statusCode));
            Assert.That(failure.ErrorCode, Is.EqualTo("REQUEST_REJECTED"));
            Assert.That(failure.RequestId, Is.EqualTo("req-400"));
            Assert.That(failure.Phase, Is.EqualTo(FormalExpeditionCommitPhase.Checkpoint));
            Assert.That(FormalExpeditionCommitRetryPolicy.IsRetryable(failure), Is.False);
            Assert.That(FormalExpeditionCommitRetryPolicy.IsOutcomeUncertain(failure), Is.False);
        }

        [Test]
        public void TransientFailureIsBoundedAndCanBeRecoveredSafely()
        {
            var failure = new FormalExpeditionCommitFailure(
                FormalExpeditionCommitPhase.Checkpoint, 503,
                "REPLAY_UNAVAILABLE", "req-503", null);

            Assert.That(FormalExpeditionCommitRetryPolicy.IsRetryable(failure), Is.True);
            Assert.That(FormalExpeditionCommitRetryPolicy.IsOutcomeUncertain(failure), Is.True);
            Assert.That(FormalExpeditionCommitRetryPolicy.CanRetry(failure, 1, 1f), Is.True);
            Assert.That(FormalExpeditionCommitRetryPolicy.CanRetry(failure, 2, 10f), Is.True);
            Assert.That(FormalExpeditionCommitRetryPolicy.CanRetry(failure, 3, 10f), Is.False);
            Assert.That(FormalExpeditionCommitRetryPolicy.CanRetry(failure, 1,
                FormalExpeditionCommitRetryPolicy.MaximumElapsedSeconds), Is.False);
        }

        private static FormalExpeditionAuthorityFlowCoordinator CreateFlow(
            FakeProofTransport proofTransport,
            FakeProgressTransport progressTransport,
            FakeSettlementTransport settlementTransport)
        {
            var proof = new ExpeditionReplayProofCoordinator(proofTransport);
            var progress = new ExpeditionProgressCoordinator(progressTransport);
            var reward = new RewardInstanceServerCoordinator(
                new FakeRewardTransport(), new AuthoritativeRewardInventoryMirror());
            var settlement = new ExpeditionSettlementCoordinator(
                settlementTransport, reward);
            return new FormalExpeditionAuthorityFlowCoordinator(
                proof, progress, settlement);
        }

        private static ExpeditionReplayEnvelopeRecord CreateEnvelope(string nodeId)
        {
            var payload = ExpeditionReplayClientContract.BuildPayload(
                "battle.resolve",
                new Dictionary<string, string>
                {
                    { "encounterId", "M01:3:42" },
                    { "battleSeed", "42" },
                    { "battleInputSha256", new string('a', 64) },
                    { "battleEventSha256", new string('b', 64) },
                    { "winner", "player" }
                });
            return ExpeditionReplayClientContract.CreateEnvelope(
                RunId,
                AccountId,
                new string('1', 64),
                new string('2', 64),
                new[]
                {
                    ExpeditionReplayClientContract.CreateAction(
                        0, "battle-1", "battle.resolve", 1, nodeId, payload)
                });
        }

        private sealed class FakeProofTransport : IExpeditionReplayProofServerTransport
        {
            public FakeProofTransport(ExpeditionReplayEnvelopeRecord envelope)
            {
                Response = new ExpeditionServerReplayProof
                {
                    RunId = envelope.RunId,
                    CombatResultDigest = new string('c', 64),
                    CombatProofReference = "v1.authority-proof",
                    TranscriptSha256 = envelope.TranscriptSha256,
                    ActionCount = envelope.Actions.Length,
                    RequestId = "req-proof"
                };
            }

            public ExpeditionServerReplayProof Response;
            public int CheckpointCalls;
            public int TerminalCalls;

            public Task<ExpeditionServerReplayProof> IssueCheckpointProofAsync(
                string accessToken, string seasonId,
                ExpeditionReplayEnvelopeRecord envelope,
                CancellationToken cancellationToken)
            {
                CheckpointCalls++;
                return Task.FromResult(Response);
            }

            public Task<ExpeditionServerReplayProof> IssueTerminalProofAsync(
                string accessToken, string seasonId,
                ExpeditionReplayEnvelopeRecord envelope,
                CancellationToken cancellationToken)
            {
                TerminalCalls++;
                return Task.FromResult(Response);
            }
        }

        private sealed class FakeProgressTransport : IExpeditionProgressServerTransport
        {
            public ExpeditionServerProgressCheckpoint Response;
            public SubmitExpeditionServerProgress Request;
            public int Calls;

            public Task<ExpeditionServerProgressCheckpoint> SubmitProgressAsync(
                string accessToken, SubmitExpeditionServerProgress request,
                CancellationToken cancellationToken)
            {
                Calls++;
                Request = request;
                return Task.FromResult(Response);
            }

            public Task<ExpeditionServerProgressCheckpoint> GetLatestProgressAsync(
                string accessToken, string runId,
                CancellationToken cancellationToken)
            {
                return Task.FromResult(Response);
            }
        }

        private sealed class FakeSettlementTransport : IExpeditionSettlementServerTransport
        {
            public ExpeditionServerSettlementReceipt Response;
            public SubmitExpeditionServerSettlement Request;
            public int SubmitCalls;

            public Task<ExpeditionServerSettlementReceipt> SubmitSettlementAsync(
                string accessToken, SubmitExpeditionServerSettlement request,
                CancellationToken cancellationToken)
            {
                SubmitCalls++;
                Request = request;
                return Task.FromResult(Response);
            }

            public Task<ExpeditionServerSettlementReceipt> GetSettlementAsync(
                string accessToken, string runId,
                CancellationToken cancellationToken) => Task.FromResult(Response);

            public Task<ExpeditionServerRewardRecovery> RecoverRewardsAsync(
                string accessToken,
                CancellationToken cancellationToken) =>
                Task.FromResult<ExpeditionServerRewardRecovery>(null);

            public Task<ExpeditionSettlementClaimChallenge> CreateClaimChallengeAsync(
                string accessToken, string settlementId,
                CancellationToken cancellationToken) =>
                Task.FromResult<ExpeditionSettlementClaimChallenge>(null);

            public Task<ExpeditionSettlementClaimReceipt> ClaimSettlementAsync(
                string accessToken, ClaimExpeditionServerSettlement request,
                CancellationToken cancellationToken) =>
                Task.FromResult<ExpeditionSettlementClaimReceipt>(null);
        }

        private sealed class FakeRewardTransport : IRewardInstanceServerTransport
        {
            public Task<AuthoritativeRewardInventorySnapshot> GetInventoryAsync(
                string accessToken, CancellationToken cancellationToken) =>
                Task.FromResult<AuthoritativeRewardInventorySnapshot>(null);

            public Task<AuthoritativeRewardRecoveryResult> RecoverAsync(
                string accessToken, CancellationToken cancellationToken) =>
                Task.FromResult<AuthoritativeRewardRecoveryResult>(null);
        }
    }
}
