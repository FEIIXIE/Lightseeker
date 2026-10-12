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
    public sealed class UnityStarterCharacterGrantTransport : IStarterCharacterGrantTransport
    {
        private const int MaximumResponseBytes = 128 * 1024;
        private readonly Uri baseUri;
        private readonly int timeoutSeconds;

        public UnityStarterCharacterGrantTransport(string baseUrl,
            bool allowInsecureLocalDevelopment, int timeoutSeconds = 15)
        {
            Uri parsed;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException("The starter grant server URL must be absolute.", nameof(baseUrl));
            if (!IsAllowedEndpoint(parsed, allowInsecureLocalDevelopment))
                throw new ArgumentException("The starter grant server must use HTTPS.", nameof(baseUrl));
            baseUri = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<StarterCharacterGrantSnapshot> ClaimAsync(
            string accessToken,
            string seasonId,
            string requestId,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(seasonId) ||
                string.IsNullOrWhiteSpace(requestId))
                throw new ArgumentException("An authenticated starter grant request is required.");
            var body = JsonUtility.ToJson(new ClaimRequest
            {
                seasonId = seasonId,
                requestId = requestId
            });
            using (var request = new UnityWebRequest(
                       new Uri(baseUri, "v1/characters/starter-grant"),
                       UnityWebRequest.kHttpVerbPOST))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                request.SetRequestHeader("Authorization", "Bearer " + accessToken);
                var operation = request.SendWebRequest();
                var completion = new TaskCompletionSource<bool>();
                operation.completed += ignored => completion.TrySetResult(true);
                if (operation.isDone) completion.TrySetResult(true);
                using (cancellationToken.Register(request.Abort)) await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();
                var data = request.downloadHandler == null ? null : request.downloadHandler.data;
                if (data != null && data.Length > MaximumResponseBytes)
                    throw new WalletServerException(request.responseCode, "CLIENT_RESPONSE_TOO_LARGE",
                        "The starter grant response exceeded the client limit.", string.Empty);
                var text = request.downloadHandler == null ? string.Empty : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                    throw CreateServerException(request.responseCode, text, request.error);
                var response = JsonUtility.FromJson<GrantResponse>(text);
                if (response == null || response.characters == null)
                    throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                        "The starter grant response was invalid.", string.Empty);
                var characters = new StarterCharacterGrantCharacter[response.characters.Length];
                for (var index = 0; index < characters.Length; index++)
                {
                    var source = response.characters[index];
                    characters[index] = new StarterCharacterGrantCharacter
                    {
                        CharacterInstanceId = source.characterInstanceId,
                        CharacterId = source.characterId,
                        Quality = source.quality,
                        AptitudeRoll = source.aptitudeRoll,
                        ExternalStar = source.externalStar,
                        SourceSlotIndex = source.sourceSlotIndex
                    };
                }
                return new StarterCharacterGrantSnapshot
                {
                    GrantId = response.grantId,
                    AccountId = response.accountId,
                    SeasonId = response.seasonId,
                    Characters = characters,
                    IdempotentReplay = response.idempotentReplay,
                    RequestId = response.requestId
                };
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
                    ? error.message
                    : (string.IsNullOrWhiteSpace(fallbackMessage)
                        ? "The starter grant request failed." : fallbackMessage),
                error == null ? string.Empty : error.requestId);
        }

        private static bool IsAllowedEndpoint(Uri endpoint, bool allowInsecureLocalDevelopment)
        {
            if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!allowInsecureLocalDevelopment ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                return false;
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
        [Serializable] private sealed class ClaimRequest { public string seasonId; public string requestId; }
        [Serializable] private sealed class GrantCharacterResponse { public string characterInstanceId; public string characterId; public int quality; public int aptitudeRoll; public int externalStar; public int sourceSlotIndex; }
        [Serializable] private sealed class GrantResponse { public string grantId; public string accountId; public string seasonId; public GrantCharacterResponse[] characters; public bool idempotentReplay; public string requestId; }
        [Serializable] private sealed class ErrorResponse { public string code; public string message; public string requestId; }
#pragma warning restore 0649
    }
}
