using System;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class ExpeditionServerReplayProof
    {
        public string RunId;
        public string CombatResultDigest;
        public string CombatProofReference;
        public string TranscriptSha256;
        public int ActionCount;
        public string RequestId;
    }

    public interface IExpeditionReplayProofServerTransport
    {
        Task<ExpeditionServerReplayProof> IssueCheckpointProofAsync(
            string accessToken,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken);

        Task<ExpeditionServerReplayProof> IssueTerminalProofAsync(
            string accessToken,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken);
    }

    public sealed class ExpeditionReplayProofCoordinator
    {
        private readonly IExpeditionReplayProofServerTransport transport;
        private int operationInProgress;

        public ExpeditionReplayProofCoordinator(
            IExpeditionReplayProofServerTransport transport)
        {
            this.transport = transport ?? throw new ArgumentNullException(nameof(transport));
        }

        public Task<ExpeditionServerReplayProof> IssueCheckpointAsync(
            string accessToken,
            string expectedAccountId,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken) => IssueAsync(
                accessToken,
                expectedAccountId,
                seasonId,
                envelope,
                terminal: false,
                cancellationToken);

        public Task<ExpeditionServerReplayProof> IssueTerminalAsync(
            string accessToken,
            string expectedAccountId,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            CancellationToken cancellationToken) => IssueAsync(
                accessToken,
                expectedAccountId,
                seasonId,
                envelope,
                terminal: true,
                cancellationToken);

        private async Task<ExpeditionServerReplayProof> IssueAsync(
            string accessToken,
            string expectedAccountId,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope,
            bool terminal,
            CancellationToken cancellationToken)
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException(
                    "An expedition replay proof request is already in progress.");
            try
            {
                ValidateRequest(accessToken, expectedAccountId, seasonId, envelope);
                var proof = terminal
                    ? await transport.IssueTerminalProofAsync(
                        accessToken, seasonId, envelope, cancellationToken)
                    : await transport.IssueCheckpointProofAsync(
                        accessToken, seasonId, envelope, cancellationToken);
                ValidateResponse(proof, envelope);
                return proof;
            }
            finally
            {
                Interlocked.Exchange(ref operationInProgress, 0);
            }
        }

        private static void ValidateRequest(
            string accessToken,
            string expectedAccountId,
            string seasonId,
            ExpeditionReplayEnvelopeRecord envelope)
        {
            Guid parsedAccountId;
            if (string.IsNullOrWhiteSpace(accessToken) ||
                !Guid.TryParse(expectedAccountId, out parsedAccountId) ||
                !IsIdentifier(seasonId, 96) || envelope == null)
                throw new InvalidOperationException(
                    "An authenticated account and replay envelope are required.");
            ExpeditionReplayClientContract.Validate(envelope);
            Guid envelopeAccountId;
            if (!Guid.TryParse(envelope.AccountId, out envelopeAccountId) ||
                envelopeAccountId != parsedAccountId)
                throw new InvalidOperationException(
                    "The replay envelope does not belong to the authenticated account.");
        }

        private static void ValidateResponse(
            ExpeditionServerReplayProof proof,
            ExpeditionReplayEnvelopeRecord envelope)
        {
            Guid responseRunId;
            Guid envelopeRunId;
            if (proof == null ||
                !Guid.TryParse(proof.RunId, out responseRunId) ||
                !Guid.TryParse(envelope.RunId, out envelopeRunId) ||
                responseRunId != envelopeRunId ||
                !IsSha256Hex(proof.CombatResultDigest) ||
                string.IsNullOrWhiteSpace(proof.CombatProofReference) ||
                proof.CombatProofReference.Length > 256 ||
                !string.Equals(proof.TranscriptSha256, envelope.TranscriptSha256,
                    StringComparison.Ordinal) ||
                proof.ActionCount != envelope.Actions.Length ||
                string.IsNullOrWhiteSpace(proof.RequestId))
                throw new InvalidOperationException(
                    "The expedition replay authority returned an invalid proof response.");
        }

        private static bool IsIdentifier(string value, int maximumLength)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > maximumLength)
                return false;
            foreach (var character in value)
            {
                if (!((character >= 'a' && character <= 'z') ||
                      (character >= 'A' && character <= 'Z') ||
                      (character >= '0' && character <= '9') ||
                      character == '-' || character == '_' || character == '.'))
                    return false;
            }
            return true;
        }

        private static bool IsSha256Hex(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
            byte[] parsed;
            return TryFromHex(value, out parsed) && parsed.Length == 32;
        }

        private static bool TryFromHex(string value, out byte[] bytes)
        {
            bytes = null;
            try
            {
                bytes = new byte[value.Length / 2];
                for (var index = 0; index < bytes.Length; index++)
                    bytes[index] = Convert.ToByte(value.Substring(index * 2, 2), 16);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (OverflowException)
            {
                return false;
            }
        }
    }
}
