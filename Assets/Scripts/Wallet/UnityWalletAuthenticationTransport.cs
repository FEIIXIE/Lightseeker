using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using UnityEngine;
using UnityEngine.Networking;

namespace SeekerAutoChess.Wallet
{
    public sealed class WalletServerException : Exception
    {
        public WalletServerException(long statusCode, string code, string message, string requestId)
            : base(message)
        {
            StatusCode = statusCode;
            Code = code ?? string.Empty;
            RequestId = requestId ?? string.Empty;
        }

        public long StatusCode { get; private set; }
        public string Code { get; private set; }
        public string RequestId { get; private set; }
    }

    public sealed class UnityWalletAuthenticationTransport : IWalletAuthenticationTransport
    {
        private const int MaximumResponseBytes = 64 * 1024;
        private readonly Uri baseUri;
        private readonly int timeoutSeconds;

        public UnityWalletAuthenticationTransport(
            string baseUrl,
            bool allowInsecureLocalDevelopment,
            int timeoutSeconds = 10)
        {
            Uri parsed;
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException("The wallet server base URL must be absolute.", "baseUrl");
            if (!IsAllowedEndpoint(parsed, allowInsecureLocalDevelopment))
                throw new ArgumentException(
                    "The wallet server must use HTTPS. HTTP is only allowed explicitly for a local development host.",
                    "baseUrl");
            if (timeoutSeconds < 1 || timeoutSeconds > 60)
                throw new ArgumentOutOfRangeException("timeoutSeconds");

            baseUri = new Uri(parsed.AbsoluteUri.TrimEnd('/') + "/", UriKind.Absolute);
            this.timeoutSeconds = timeoutSeconds;
        }

        public async Task<ServerAuthenticationChallenge> CreateChallengeAsync(
            string walletAddress,
            string chainId,
            string clientVersion,
            string platform,
            CancellationToken cancellationToken)
        {
            var request = new CreateChallengeRequest
            {
                walletAddress = walletAddress,
                chainId = chainId,
                clientVersion = clientVersion,
                platform = platform
            };
            var response = await SendJsonAsync<CreateChallengeResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/auth/challenges",
                JsonUtility.ToJson(request),
                null,
                cancellationToken);
            return new ServerAuthenticationChallenge
            {
                ChallengeId = response.challengeId,
                SignInPayload = response.signInPayload == null ? null : new ServerSignInPayload
                {
                    Domain = response.signInPayload.domain,
                    Address = response.signInPayload.address,
                    Statement = response.signInPayload.statement,
                    Uri = response.signInPayload.uri,
                    Version = response.signInPayload.version,
                    ChainId = response.signInPayload.chainId,
                    Nonce = response.signInPayload.nonce,
                    IssuedAt = response.signInPayload.issuedAt,
                    ExpirationTime = response.signInPayload.expirationTime
                },
                ExpiresAt = response.expiresAt,
                RequestId = response.requestId
            };
        }

        public async Task<WalletAuthenticationSession> CreateSessionAsync(
            WalletSessionProof proof,
            CancellationToken cancellationToken)
        {
            if (proof == null) throw new ArgumentNullException("proof");
            var request = new CreateSessionRequest
            {
                challengeId = proof.ChallengeId,
                address = proof.Address,
                signedMessage = proof.SignedMessage,
                signature = proof.Signature,
                invitationCode = proof.InvitationCode
            };
            var response = await SendJsonAsync<CreateSessionResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/auth/sessions",
                JsonUtility.ToJson(request),
                null,
                cancellationToken);
            return new WalletAuthenticationSession
            {
                AccountId = response.accountId,
                WalletAddress = response.walletAddress,
                AccessToken = response.accessToken,
                AccessExpiresAt = response.accessExpiresAt,
                RefreshToken = response.refreshToken,
                RefreshExpiresAt = response.refreshExpiresAt,
                RequestId = response.requestId
            };
        }

        public async Task<WalletSessionTokens> RefreshSessionAsync(
            string refreshToken,
            CancellationToken cancellationToken)
        {
            var response = await SendJsonAsync<RefreshSessionResponse>(
                UnityWebRequest.kHttpVerbPOST,
                "v1/auth/sessions/refresh",
                JsonUtility.ToJson(new RefreshSessionRequest { refreshToken = refreshToken }),
                null,
                cancellationToken);
            return new WalletSessionTokens
            {
                AccessToken = response.accessToken,
                AccessExpiresAt = response.accessExpiresAt,
                RefreshToken = response.refreshToken,
                RefreshExpiresAt = response.refreshExpiresAt,
                RequestId = response.requestId
            };
        }

        public async Task<WalletServerAccount> GetCurrentAccountAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            var response = await SendJsonAsync<CurrentAccountResponse>(
                UnityWebRequest.kHttpVerbGET,
                "v1/me",
                null,
                accessToken,
                cancellationToken);
            return new WalletServerAccount
            {
                AccountId = response.accountId,
                WalletAddress = response.walletAddress,
                AccountStatus = response.accountStatus,
                DisplayName = response.displayName,
                SeasonId = response.seasonId,
                StateVersion = response.stateVersion,
                ServerTime = response.serverTime,
                RequestId = response.requestId
            };
        }

        public async Task<WalletServerAccount> UpdateDisplayNameAsync(
            string accessToken,
            string displayName,
            CancellationToken cancellationToken)
        {
            var response = await SendJsonAsync<CurrentAccountResponse>(
                UnityWebRequest.kHttpVerbPUT,
                "v1/me/display-name",
                JsonUtility.ToJson(new UpdateDisplayNameRequest { displayName = displayName }),
                accessToken,
                cancellationToken);
            return new WalletServerAccount
            {
                AccountId = response.accountId,
                WalletAddress = response.walletAddress,
                AccountStatus = response.accountStatus,
                DisplayName = response.displayName,
                SeasonId = response.seasonId,
                StateVersion = response.stateVersion,
                ServerTime = response.serverTime,
                RequestId = response.requestId
            };
        }

        public async Task RevokeCurrentSessionAsync(
            string accessToken,
            CancellationToken cancellationToken)
        {
            await SendNoContentAsync(
                UnityWebRequest.kHttpVerbDELETE,
                "v1/auth/sessions/current",
                accessToken,
                cancellationToken);
        }

        private async Task<T> SendJsonAsync<T>(
            string method,
            string relativePath,
            string json,
            string accessToken,
            CancellationToken cancellationToken) where T : class
        {
            var responseText = await SendAsync(
                method, relativePath, json, accessToken, cancellationToken);
            var response = JsonUtility.FromJson<T>(responseText);
            if (response == null)
                throw new WalletServerException(0, "CLIENT_RESPONSE_INVALID",
                    "The wallet server returned an invalid JSON response.", string.Empty);
            return response;
        }

        private async Task SendNoContentAsync(
            string method,
            string relativePath,
            string accessToken,
            CancellationToken cancellationToken)
        {
            await SendAsync(method, relativePath, null, accessToken, cancellationToken);
        }

        private async Task<string> SendAsync(
            string method,
            string relativePath,
            string json,
            string accessToken,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = new UnityWebRequest(new Uri(baseUri, relativePath), method);
            try
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.timeout = timeoutSeconds;
                request.SetRequestHeader("Accept", "application/json");
                if (json != null)
                {
                    request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                    request.SetRequestHeader("Content-Type", "application/json; charset=utf-8");
                }
                if (!string.IsNullOrWhiteSpace(accessToken))
                    request.SetRequestHeader("Authorization", "Bearer " + accessToken);

                var operation = request.SendWebRequest();
                var completion = new TaskCompletionSource<bool>();
                operation.completed += ignored => completion.TrySetResult(true);
                if (operation.isDone) completion.TrySetResult(true);
                using (cancellationToken.Register(request.Abort))
                    await completion.Task;
                cancellationToken.ThrowIfCancellationRequested();

                var data = request.downloadHandler == null ? null : request.downloadHandler.data;
                if (data != null && data.Length > MaximumResponseBytes)
                    throw new WalletServerException(request.responseCode, "CLIENT_RESPONSE_TOO_LARGE",
                        "The wallet server response exceeded the client limit.", string.Empty);

                var responseText = request.downloadHandler == null
                    ? string.Empty
                    : request.downloadHandler.text;
                if (request.result != UnityWebRequest.Result.Success)
                {
                    var exception = CreateServerException(
                        request.responseCode, responseText, request.error);
                    Debug.LogWarning(
                        "Wallet server request failed: path=" + relativePath +
                        " status=" + exception.StatusCode +
                        " code=" + exception.Code +
                        " requestId=" + (string.IsNullOrWhiteSpace(exception.RequestId)
                            ? "none"
                            : exception.RequestId));
                    throw exception;
                }
                return responseText;
            }
            finally
            {
                request.Dispose();
            }
        }

        private static WalletServerException CreateServerException(
            long statusCode,
            string responseText,
            string fallbackMessage)
        {
            ErrorResponse error = null;
            if (!string.IsNullOrWhiteSpace(responseText))
            {
                try { error = JsonUtility.FromJson<ErrorResponse>(responseText); }
                catch (ArgumentException) { }
            }
            var message = error != null && !string.IsNullOrWhiteSpace(error.message)
                ? error.message
                : (string.IsNullOrWhiteSpace(fallbackMessage)
                    ? "The wallet server request failed."
                    : fallbackMessage);
            return new WalletServerException(
                statusCode,
                error == null ? "TRANSPORT_FAILED" : error.code,
                message,
                error == null ? string.Empty : error.requestId);
        }

        private static bool IsAllowedEndpoint(Uri endpoint, bool allowInsecureLocalDevelopment)
        {
            if (string.Equals(endpoint.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                return true;
            if (!allowInsecureLocalDevelopment ||
                !string.Equals(endpoint.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(endpoint.Host, "localhost", StringComparison.OrdinalIgnoreCase))
                return true;

            IPAddress address;
            if (!IPAddress.TryParse(endpoint.Host, out address)) return false;
            if (IPAddress.IsLoopback(address)) return true;
            var bytes = address.GetAddressBytes();
            return bytes.Length == 4 &&
                   (bytes[0] == 10 ||
                    (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) ||
                    (bytes[0] == 192 && bytes[1] == 168));
        }

#pragma warning disable 0649 // JsonUtility populates response fields through reflection.
        [Serializable]
        private sealed class CreateChallengeRequest
        {
            public string walletAddress;
            public string chainId;
            public string clientVersion;
            public string platform;
        }

        [Serializable]
        private sealed class CreateChallengeResponse
        {
            public string challengeId;
            public SignInPayloadResponse signInPayload;
            public string expiresAt;
            public string requestId;
        }

        [Serializable]
        private sealed class SignInPayloadResponse
        {
            public string domain;
            public string address;
            public string statement;
            public string uri;
            public string version;
            public string chainId;
            public string nonce;
            public string issuedAt;
            public string expirationTime;
        }

        [Serializable]
        private sealed class CreateSessionRequest
        {
            public string challengeId;
            public string address;
            public string signedMessage;
            public string signature;
            public string invitationCode;
        }

        [Serializable]
        private sealed class CreateSessionResponse
        {
            public string accountId;
            public string walletAddress;
            public string accessToken;
            public string accessExpiresAt;
            public string refreshToken;
            public string refreshExpiresAt;
            public string requestId;
        }

        [Serializable]
        private sealed class RefreshSessionRequest
        {
            public string refreshToken;
        }

        [Serializable]
        private sealed class RefreshSessionResponse
        {
            public string accessToken;
            public string accessExpiresAt;
            public string refreshToken;
            public string refreshExpiresAt;
            public string requestId;
        }

        [Serializable]
        private sealed class CurrentAccountResponse
        {
            public string accountId;
            public string walletAddress;
            public string accountStatus;
            public string displayName;
            public string seasonId;
            public long stateVersion;
            public string serverTime;
            public string requestId;
        }

        [Serializable]
        private sealed class UpdateDisplayNameRequest
        {
            public string displayName;
        }

        [Serializable]
        private sealed class ErrorResponse
        {
            public string code;
            public string message;
            public string requestId;
        }
#pragma warning restore 0649
    }

    /// <summary>
    /// Persists wallet session material with an AndroidKeyStore-backed AES-GCM key.
    /// No token is written to PlayerPrefs or to Unity logs.
    /// </summary>
    public sealed class AndroidWalletAuthenticationSessionStore :
        IWalletAuthenticationSessionStore
    {
        private const int SchemaVersion = 1;
        private const string JavaStoreClass =
            "com.seeker.autochess.security.LightseekerSecureSessionStore";
        private readonly string storageKey;

        public AndroidWalletAuthenticationSessionStore(string serverBaseUrl)
        {
            Uri parsed;
            if (!Uri.TryCreate(serverBaseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException(
                    "The wallet session storage scope must be an absolute server URL.",
                    "serverBaseUrl");
            storageKey = "wallet_session_" + HashScope(
                parsed.AbsoluteUri.TrimEnd('/').ToLowerInvariant());
        }

        public WalletAuthenticationSession Load()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var json = CallStore<string>("load", storageKey);
            if (string.IsNullOrWhiteSpace(json)) return null;
            var persisted = JsonUtility.FromJson<PersistedWalletSession>(json);
            if (persisted == null || persisted.schemaVersion != SchemaVersion)
                throw new InvalidOperationException(
                    "The encrypted wallet session has an unsupported schema version.");
            return persisted.ToSession();
#else
            return null;
#endif
        }

        public void Save(WalletAuthenticationSession session)
        {
            if (session == null) throw new ArgumentNullException("session");
#if UNITY_ANDROID && !UNITY_EDITOR
            var persisted = PersistedWalletSession.FromSession(session);
            CallStore<object>("save", storageKey, JsonUtility.ToJson(persisted));
#else
            throw new PlatformNotSupportedException(
                "Encrypted wallet session storage is available only on an Android player.");
#endif
        }

        public void Clear()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            CallStore<object>("clear", storageKey);
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static T CallStore<T>(string method, params object[] arguments)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var store = new AndroidJavaClass(JavaStoreClass))
            {
                if (activity == null)
                    throw new InvalidOperationException(
                        "The Android activity is unavailable for secure wallet storage.");
                var callArguments = new object[arguments.Length + 1];
                callArguments[0] = activity;
                Array.Copy(arguments, 0, callArguments, 1, arguments.Length);
                if (typeof(T) == typeof(object))
                {
                    store.CallStatic(method, callArguments);
                    return default(T);
                }
                return store.CallStatic<T>(method, callArguments);
            }
        }
#endif

        private static string HashScope(string scope)
        {
            byte[] digest;
            using (var sha256 = SHA256.Create())
                digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(scope));
            var builder = new StringBuilder(32);
            for (var index = 0; index < 16; index++)
                builder.Append(digest[index].ToString("x2"));
            return builder.ToString();
        }

#pragma warning disable 0649 // JsonUtility populates persisted fields through reflection.
        [Serializable]
        private sealed class PersistedWalletSession
        {
            public int schemaVersion;
            public string accountId;
            public string walletAddress;
            public string accessToken;
            public string accessExpiresAt;
            public string refreshToken;
            public string refreshExpiresAt;
            public string requestId;

            public static PersistedWalletSession FromSession(
                WalletAuthenticationSession session)
            {
                return new PersistedWalletSession
                {
                    schemaVersion = SchemaVersion,
                    accountId = session.AccountId,
                    walletAddress = session.WalletAddress,
                    accessToken = session.AccessToken,
                    accessExpiresAt = session.AccessExpiresAt,
                    refreshToken = session.RefreshToken,
                    refreshExpiresAt = session.RefreshExpiresAt,
                    requestId = session.RequestId
                };
            }

            public WalletAuthenticationSession ToSession()
            {
                return new WalletAuthenticationSession
                {
                    AccountId = accountId,
                    WalletAddress = walletAddress,
                    AccessToken = accessToken,
                    AccessExpiresAt = accessExpiresAt,
                    RefreshToken = refreshToken,
                    RefreshExpiresAt = refreshExpiresAt,
                    RequestId = requestId
                };
            }
        }
#pragma warning restore 0649
    }

    /// <summary>
    /// Stores submitted expedition transaction signatures in the same
    /// AndroidKeyStore-backed encrypted store used by wallet sessions. A signature is
    /// removed only after the server accepts the corresponding intent commit.
    /// </summary>
    public sealed class AndroidExpeditionPaymentReceiptStore :
        IExpeditionPaymentReceiptStore
    {
        private const int SchemaVersion = 1;
        private const string JavaStoreClass =
            "com.seeker.autochess.security.LightseekerSecureSessionStore";
        private readonly string storageScope;

        public AndroidExpeditionPaymentReceiptStore(string serverBaseUrl)
        {
            Uri parsed;
            if (!Uri.TryCreate(serverBaseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException(
                    "The payment receipt storage scope must be an absolute server URL.",
                    "serverBaseUrl");
            storageScope = HashScope(parsed.AbsoluteUri.TrimEnd('/').ToLowerInvariant());
        }

        public PendingExpeditionPaymentReceipt Load(string entryIntentId)
        {
            ValidateIntentId(entryIntentId);
#if UNITY_ANDROID && !UNITY_EDITOR
            var json = CallStore<string>("load", StorageKey(entryIntentId));
            if (string.IsNullOrWhiteSpace(json)) return null;
            var persisted = JsonUtility.FromJson<PersistedPaymentReceipt>(json);
            if (persisted == null || persisted.schemaVersion != SchemaVersion ||
                persisted.receipt == null)
                throw new InvalidOperationException(
                    "The encrypted payment receipt has an unsupported schema version.");
            return persisted.receipt;
#else
            return null;
#endif
        }

        public void Save(PendingExpeditionPaymentReceipt receipt)
        {
            if (receipt == null) throw new ArgumentNullException("receipt");
            ValidateIntentId(receipt.EntryIntentId);
            if (string.IsNullOrWhiteSpace(receipt.TransactionSignature))
                throw new InvalidOperationException(
                    "A submitted transaction signature is required for receipt recovery.");
#if UNITY_ANDROID && !UNITY_EDITOR
            CallStore<object>("save", StorageKey(receipt.EntryIntentId),
                JsonUtility.ToJson(new PersistedPaymentReceipt
                {
                    schemaVersion = SchemaVersion,
                    receipt = receipt
                }));
#else
            throw new PlatformNotSupportedException(
                "Encrypted payment receipt storage is available only on an Android player.");
#endif
        }

        public void Clear(string entryIntentId)
        {
            ValidateIntentId(entryIntentId);
#if UNITY_ANDROID && !UNITY_EDITOR
            CallStore<object>("clear", StorageKey(entryIntentId));
#endif
        }

        private string StorageKey(string entryIntentId)
        {
            return "expedition_payment_" + storageScope + "_" + HashScope(entryIntentId);
        }

        private static void ValidateIntentId(string entryIntentId)
        {
            if (string.IsNullOrWhiteSpace(entryIntentId))
                throw new ArgumentException("An entry intent id is required.", "entryIntentId");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static T CallStore<T>(string method, params object[] arguments)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var store = new AndroidJavaClass(JavaStoreClass))
            {
                if (activity == null)
                    throw new InvalidOperationException(
                        "The Android activity is unavailable for secure payment storage.");
                var callArguments = new object[arguments.Length + 1];
                callArguments[0] = activity;
                Array.Copy(arguments, 0, callArguments, 1, arguments.Length);
                if (typeof(T) == typeof(object))
                {
                    store.CallStatic(method, callArguments);
                    return default(T);
                }
                return store.CallStatic<T>(method, callArguments);
            }
        }
#endif

        private static string HashScope(string scope)
        {
            byte[] digest;
            using (var sha256 = SHA256.Create())
                digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(scope));
            var builder = new StringBuilder(32);
            for (var index = 0; index < 16; index++)
                builder.Append(digest[index].ToString("x2"));
            return builder.ToString();
        }

        [Serializable]
        private sealed class PersistedPaymentReceipt
        {
            public int schemaVersion;
            public PendingExpeditionPaymentReceipt receipt;
        }
    }

    /// <summary>
    /// Persists the account/run-bound formal replay journal in the AndroidKeyStore
    /// encrypted store. Editor sessions deliberately remain memory-only.
    /// </summary>
    public sealed class AndroidExpeditionReplayStateStore :
        IExpeditionReplayStateStore
    {
        private const int SchemaVersion = 1;
        private const string JavaStoreClass =
            "com.seeker.autochess.security.LightseekerSecureSessionStore";
        private readonly string storageScope;

        public AndroidExpeditionReplayStateStore(string serverBaseUrl)
        {
            Uri parsed;
            if (!Uri.TryCreate(serverBaseUrl, UriKind.Absolute, out parsed))
                throw new ArgumentException(
                    "The replay storage scope must be an absolute server URL.",
                    "serverBaseUrl");
            storageScope = HashScope(
                parsed.AbsoluteUri.TrimEnd('/').ToLowerInvariant());
        }

        public ExpeditionReplayRecorderState Load(string accountId, string runId)
        {
            ValidateScope(accountId, runId);
#if UNITY_ANDROID && !UNITY_EDITOR
            var json = CallStore<string>("load", StorageKey(accountId, runId));
            if (string.IsNullOrWhiteSpace(json)) return null;
            var persisted = JsonUtility.FromJson<PersistedReplayState>(json);
            if (persisted == null || persisted.schemaVersion != SchemaVersion ||
                persisted.state == null)
                throw new InvalidOperationException(
                    "The encrypted expedition replay has an unsupported schema version.");
            return persisted.state;
#else
            return null;
#endif
        }

        public void Save(ExpeditionReplayRecorderState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            ValidateScope(state.AccountId, state.RunId);
#if UNITY_ANDROID && !UNITY_EDITOR
            CallStore<object>("save", StorageKey(state.AccountId, state.RunId),
                JsonUtility.ToJson(new PersistedReplayState
                {
                    schemaVersion = SchemaVersion,
                    state = state
                }));
#endif
        }

        public void Clear(string accountId, string runId)
        {
            ValidateScope(accountId, runId);
#if UNITY_ANDROID && !UNITY_EDITOR
            CallStore<object>("clear", StorageKey(accountId, runId));
#endif
        }

        private string StorageKey(string accountId, string runId)
        {
            return "expedition_replay_" + storageScope + "_" +
                   HashScope(NormalizeGuid(accountId) + "|" + NormalizeGuid(runId));
        }

        private static void ValidateScope(string accountId, string runId)
        {
            NormalizeGuid(accountId);
            NormalizeGuid(runId);
        }

        private static string NormalizeGuid(string value)
        {
            Guid parsed;
            if (!Guid.TryParse(value, out parsed))
                throw new ArgumentException(
                    "An authoritative account and run id are required.");
            return parsed.ToString("N");
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static T CallStore<T>(string method, params object[] arguments)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var store = new AndroidJavaClass(JavaStoreClass))
            {
                if (activity == null)
                    throw new InvalidOperationException(
                        "The Android activity is unavailable for secure replay storage.");
                var callArguments = new object[arguments.Length + 1];
                callArguments[0] = activity;
                Array.Copy(arguments, 0, callArguments, 1, arguments.Length);
                if (typeof(T) == typeof(object))
                {
                    store.CallStatic(method, callArguments);
                    return default(T);
                }
                return store.CallStatic<T>(method, callArguments);
            }
        }
#endif

        private static string HashScope(string scope)
        {
            byte[] digest;
            using (var sha256 = SHA256.Create())
                digest = sha256.ComputeHash(Encoding.UTF8.GetBytes(scope));
            var builder = new StringBuilder(32);
            for (var index = 0; index < 16; index++)
                builder.Append(digest[index].ToString("x2"));
            return builder.ToString();
        }

#pragma warning disable 0649
        [Serializable]
        private sealed class PersistedReplayState
        {
            public int schemaVersion;
            public ExpeditionReplayRecorderState state;
        }
#pragma warning restore 0649
    }
}
