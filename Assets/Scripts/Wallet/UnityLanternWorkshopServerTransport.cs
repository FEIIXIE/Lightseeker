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
    public sealed class UnityLanternWorkshopServerTransport : ILanternWorkshopServerTransport
    {
        private readonly Uri baseUri;
        private readonly bool allowInsecureLocalDevelopment;

        public UnityLanternWorkshopServerTransport(string baseUrl,
            bool allowInsecureLocalDevelopment)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out baseUri) ||
                !IsAllowedEndpoint(baseUri, allowInsecureLocalDevelopment))
                throw new ArgumentException("A secure lantern workshop server URL is required.", "baseUrl");
            this.allowInsecureLocalDevelopment = allowInsecureLocalDevelopment;
        }

        public async Task<LanternWorkshopSnapshot> GetSnapshotAsync(
            string accessToken, CancellationToken cancellationToken)
        {
            var response = await SendAsync<SnapshotResponse>(UnityWebRequest.kHttpVerbGET,
                "v1/lantern-workshop", null, accessToken, cancellationToken);
            var sourceLanterns = response.lanterns ?? new LanternStateResponse[0];
            var lanterns = new LanternWorkshopLanternState[sourceLanterns.Length];
            for (var index = 0; index < sourceLanterns.Length; index++)
            {
                var item = sourceLanterns[index] ?? new LanternStateResponse();
                lanterns[index] = new LanternWorkshopLanternState
                {
                    LanternInstanceId = item.lanternInstanceId,
                    SeasonId = item.seasonId,
                    ContractStage = item.contractStage,
                    OptionIds = item.optionIds ?? new string[0],
                    StateVersion = item.stateVersion,
                    StateHash = item.stateHash
                };
            }
            return new LanternWorkshopSnapshot
            {
                AccountId = response.accountId,
                LampGlowBalance = response.lampGlowBalance,
                CovenantContractId = response.covenantContractId,
                StageCosts = response.stageCosts ?? new int[0],
                Lanterns = lanterns,
                RequestId = response.requestId
            };
        }

        public async Task<LanternCovenantAdvanceResult> AdvanceAsync(
            string accessToken, string lanternInstanceId, string optionId,
            int expectedStateVersion, string idempotencyKey,
            CancellationToken cancellationToken)
        {
            var body = JsonUtility.ToJson(new AdvanceRequest
            {
                optionId = optionId,
                expectedStateVersion = expectedStateVersion,
                idempotencyKey = idempotencyKey
            });
            var response = await SendAsync<AdvanceResponse>(UnityWebRequest.kHttpVerbPOST,
                "v1/lantern-workshop/" + Uri.EscapeDataString(lanternInstanceId) + "/advance",
                body, accessToken, cancellationToken);
            return new LanternCovenantAdvanceResult
            {
                ReceiptId = response.receiptId,
                AccountId = response.accountId,
                LanternInstanceId = response.lanternInstanceId,
                ResultingStage = response.resultingStage,
                SelectedOptionId = response.selectedOptionId,
                CostLampGlow = response.costLampGlow,
                RemainingLampGlow = response.remainingLampGlow,
                ResultingOptionIds = response.resultingOptionIds ?? new string[0],
                ResultingStateVersion = response.resultingStateVersion,
                ResultingStateHash = response.resultingStateHash,
                IdempotencyKey = response.idempotencyKey,
                CreatedAt = response.createdAt,
                WasAlreadyApplied = response.wasAlreadyApplied,
                RequestId = response.requestId
            };
        }

        private async Task<T> SendAsync<T>(string method, string relativePath,
            string json, string accessToken, CancellationToken cancellationToken) where T : class
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("An authenticated access token is required.", "accessToken");
            var endpoint = new Uri(baseUri, relativePath);
            if (!IsAllowedEndpoint(endpoint, allowInsecureLocalDevelopment))
                throw new InvalidOperationException("The lantern workshop endpoint is not allowed.");
            using (var request = new UnityWebRequest(endpoint, method))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json");
                }
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
                var responseText = request.downloadHandler == null
                    ? string.Empty : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                    throw CreateServerException(request.responseCode, responseText, request.error);
                var result = JsonUtility.FromJson<T>(responseText);
                if (result == null) throw new InvalidOperationException(
                    "The lantern workshop server returned an empty response.");
                return result;
            }
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
                error != null && !string.IsNullOrWhiteSpace(error.message)
                    ? error.message : fallbackMessage,
                error == null ? string.Empty : error.requestId);
        }

        private static bool IsAllowedEndpoint(Uri endpoint,
            bool allowInsecureLocalDevelopment)
        {
            if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase)) return true;
            if (!allowInsecureLocalDevelopment ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp,
                    StringComparison.OrdinalIgnoreCase)) return false;
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
        [Serializable] private sealed class AdvanceRequest { public string optionId; public int expectedStateVersion; public string idempotencyKey; }
        [Serializable] private sealed class LanternStateResponse { public string lanternInstanceId; public string seasonId; public int contractStage; public string[] optionIds; public int stateVersion; public string stateHash; }
        [Serializable] private sealed class SnapshotResponse { public string accountId; public long lampGlowBalance; public string covenantContractId; public int[] stageCosts; public LanternStateResponse[] lanterns; public string requestId; }
        [Serializable] private sealed class AdvanceResponse { public string receiptId; public string accountId; public string lanternInstanceId; public int resultingStage; public string selectedOptionId; public int costLampGlow; public long remainingLampGlow; public string[] resultingOptionIds; public int resultingStateVersion; public string resultingStateHash; public string idempotencyKey; public string createdAt; public bool wasAlreadyApplied; public string requestId; }
        [Serializable] private sealed class ErrorResponse { public string code; public string message; public string requestId; }
#pragma warning restore 0649
    }
}
