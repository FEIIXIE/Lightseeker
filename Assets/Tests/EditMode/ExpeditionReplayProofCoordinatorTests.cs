using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class ExpeditionReplayProofCoordinatorTests
    {
        [Test]
        public void ForwardsBoundCheckpointAndValidatesProofResponse()
        {
            var accountId = "33333333-3333-3333-3333-333333333333";
            var envelope = CreateEnvelope(accountId);
            var transport = new FakeTransport
            {
                Response = new ExpeditionServerReplayProof
                {
                    RunId = envelope.RunId,
                    CombatResultDigest = new string('c', 64),
                    CombatProofReference = "v1.test-proof",
                    TranscriptSha256 = envelope.TranscriptSha256,
                    ActionCount = envelope.Actions.Length,
                    RequestId = "req_test"
                }
            };
            var coordinator = new ExpeditionReplayProofCoordinator(transport);

            var proof = coordinator.IssueCheckpointAsync(
                    "access-token", accountId, "season-a", envelope,
                    CancellationToken.None)
                .GetAwaiter().GetResult();

            Assert.That(proof.CombatProofReference, Is.EqualTo("v1.test-proof"));
            Assert.That(transport.CheckpointCalls, Is.EqualTo(1));
            Assert.That(transport.TerminalCalls, Is.EqualTo(0));
        }

        [Test]
        public void RejectsAccountMismatchBeforeTransport()
        {
            var envelope = CreateEnvelope(
                "33333333-3333-3333-3333-333333333333");
            var transport = new FakeTransport();
            var coordinator = new ExpeditionReplayProofCoordinator(transport);

            Assert.Throws<InvalidOperationException>(() =>
                coordinator.IssueTerminalAsync(
                        "access-token",
                        "44444444-4444-4444-4444-444444444444",
                        "season-a",
                        envelope,
                        CancellationToken.None)
                    .GetAwaiter().GetResult());
            Assert.That(transport.TerminalCalls, Is.EqualTo(0));
        }

        [Test]
        public void RejectsMismatchedAuthorityResponse()
        {
            var accountId = "33333333-3333-3333-3333-333333333333";
            var envelope = CreateEnvelope(accountId);
            var transport = new FakeTransport
            {
                Response = new ExpeditionServerReplayProof
                {
                    RunId = envelope.RunId,
                    CombatResultDigest = new string('d', 64),
                    CombatProofReference = "v1.test-proof",
                    TranscriptSha256 = new string('e', 64),
                    ActionCount = envelope.Actions.Length,
                    RequestId = "req_test"
                }
            };
            var coordinator = new ExpeditionReplayProofCoordinator(transport);

            Assert.Throws<InvalidOperationException>(() =>
                coordinator.IssueCheckpointAsync(
                        "access-token", accountId, "season-a", envelope,
                        CancellationToken.None)
                    .GetAwaiter().GetResult());
        }

        private static ExpeditionReplayEnvelopeRecord CreateEnvelope(string accountId)
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
                "11111111-1111-1111-1111-111111111111",
                accountId,
                new string('1', 64),
                new string('2', 64),
                new[]
                {
                    ExpeditionReplayClientContract.CreateAction(
                        0, "battle-1", "battle.resolve", 1, "M01", payload)
                });
        }

        private sealed class FakeTransport : IExpeditionReplayProofServerTransport
        {
            public ExpeditionServerReplayProof Response;
            public int CheckpointCalls;
            public int TerminalCalls;

            public Task<ExpeditionServerReplayProof> IssueCheckpointProofAsync(
                string accessToken,
                string seasonId,
                ExpeditionReplayEnvelopeRecord envelope,
                CancellationToken cancellationToken)
            {
                CheckpointCalls++;
                return Task.FromResult(Response);
            }

            public Task<ExpeditionServerReplayProof> IssueTerminalProofAsync(
                string accessToken,
                string seasonId,
                ExpeditionReplayEnvelopeRecord envelope,
                CancellationToken cancellationToken)
            {
                TerminalCalls++;
                return Task.FromResult(Response);
            }
        }
    }
}
