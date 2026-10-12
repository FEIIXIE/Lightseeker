using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SeekerAutoChess.Core
{
    public sealed class WalletIdentity
    {
        public WalletIdentity(string walletAddress, byte[] publicKey)
        {
            if (string.IsNullOrWhiteSpace(walletAddress))
                throw new ArgumentException("A wallet address is required.", "walletAddress");
            if (publicKey == null || publicKey.Length != 32)
                throw new ArgumentException("A Solana public key must contain exactly 32 bytes.", "publicKey");

            WalletAddress = walletAddress;
            PublicKey = Copy(publicKey);
        }

        public string WalletAddress { get; private set; }
        public byte[] PublicKey { get; private set; }

        public static byte[] Copy(byte[] source)
        {
            var result = new byte[source.Length];
            Buffer.BlockCopy(source, 0, result, 0, source.Length);
            return result;
        }
    }

    public sealed class ServerSignInPayload
    {
        public string Domain { get; set; }
        public string Address { get; set; }
        public string Statement { get; set; }
        public string Uri { get; set; }
        public string Version { get; set; }
        public string ChainId { get; set; }
        public string Nonce { get; set; }
        public string IssuedAt { get; set; }
        public string ExpirationTime { get; set; }
    }

    public sealed class ServerAuthenticationChallenge
    {
        public string ChallengeId { get; set; }
        public ServerSignInPayload SignInPayload { get; set; }
        public string ExpiresAt { get; set; }
        public string RequestId { get; set; }
    }

    public sealed class WalletSessionProof
    {
        public string ChallengeId { get; set; }
        public string Address { get; set; }
        public string SignedMessage { get; set; }
        public string Signature { get; set; }
        public string InvitationCode { get; set; }
    }

    public sealed class WalletAuthenticationSession
    {
        public string AccountId { get; set; }
        public string WalletAddress { get; set; }
        public string AccessToken { get; set; }
        public string AccessExpiresAt { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshExpiresAt { get; set; }
        public string RequestId { get; set; }

        public WalletAuthenticationSession WithRotatedTokens(WalletSessionTokens tokens)
        {
            if (tokens == null) throw new ArgumentNullException("tokens");
            return new WalletAuthenticationSession
            {
                AccountId = AccountId,
                WalletAddress = WalletAddress,
                AccessToken = tokens.AccessToken,
                AccessExpiresAt = tokens.AccessExpiresAt,
                RefreshToken = tokens.RefreshToken,
                RefreshExpiresAt = tokens.RefreshExpiresAt,
                RequestId = tokens.RequestId
            };
        }
    }

    public sealed class WalletSessionTokens
    {
        public string AccessToken { get; set; }
        public string AccessExpiresAt { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshExpiresAt { get; set; }
        public string RequestId { get; set; }
    }

    public sealed class WalletServerAccount
    {
        public string AccountId { get; set; }
        public string WalletAddress { get; set; }
        public string AccountStatus { get; set; }
        public string DisplayName { get; set; }
        public string SeasonId { get; set; }
        public long StateVersion { get; set; }
        public string ServerTime { get; set; }
        public string RequestId { get; set; }
    }

    public interface IWalletSiwsGateway
    {
        Task<WalletIdentity> ConnectAsync(CancellationToken cancellationToken);
        Task<byte[]> SignMessageAsync(byte[] message, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
    }

    public interface IWalletAuthenticationTransport
    {
        Task<ServerAuthenticationChallenge> CreateChallengeAsync(
            string walletAddress,
            string chainId,
            string clientVersion,
            string platform,
            CancellationToken cancellationToken);

        Task<WalletAuthenticationSession> CreateSessionAsync(
            WalletSessionProof proof,
            CancellationToken cancellationToken);

        Task<WalletSessionTokens> RefreshSessionAsync(
            string refreshToken,
            CancellationToken cancellationToken);

        Task<WalletServerAccount> GetCurrentAccountAsync(
            string accessToken,
            CancellationToken cancellationToken);

        Task<WalletServerAccount> UpdateDisplayNameAsync(
            string accessToken,
            string displayName,
            CancellationToken cancellationToken);

        Task RevokeCurrentSessionAsync(
            string accessToken,
            CancellationToken cancellationToken);
    }

    public interface IWalletAuthenticationSessionStore
    {
        WalletAuthenticationSession Load();
        void Save(WalletAuthenticationSession session);
        void Clear();
    }

    public static class WalletSignedPayload
    {
        public const int Ed25519SignatureLength = 64;

        public static byte[] ExtractEd25519Signature(byte[] originalMessage, byte[] signedPayload)
        {
            if (originalMessage == null) throw new ArgumentNullException("originalMessage");
            if (signedPayload == null)
                throw new InvalidOperationException("The wallet did not return a signed message payload.");

            if (signedPayload.Length == Ed25519SignatureLength)
                return WalletIdentity.Copy(signedPayload);

            if (signedPayload.Length != originalMessage.Length + Ed25519SignatureLength)
                throw new InvalidOperationException("The wallet returned an unsupported signed message payload length.");

            for (var index = 0; index < originalMessage.Length; index++)
            {
                if (signedPayload[index] != originalMessage[index])
                    throw new InvalidOperationException("The wallet changed the SIWS message before signing it.");
            }

            var signature = new byte[Ed25519SignatureLength];
            Buffer.BlockCopy(signedPayload, originalMessage.Length, signature, 0, signature.Length);
            return signature;
        }
    }

    public static class SiwsCanonicalMessageFormatter
    {
        public static string Format(ServerSignInPayload payload)
        {
            if (payload == null) throw new ArgumentNullException("payload");
            Require(payload.Domain, "domain");
            Require(payload.Address, "address");
            Require(payload.Statement, "statement");
            Require(payload.Uri, "uri");
            Require(payload.Version, "version");
            Require(payload.ChainId, "chainId");
            Require(payload.Nonce, "nonce");

            DateTimeOffset issuedAt;
            DateTimeOffset expirationTime;
            if (!DateTimeOffset.TryParse(payload.IssuedAt, out issuedAt))
                throw new InvalidOperationException("The server SIWS issuedAt value is invalid.");
            if (!DateTimeOffset.TryParse(payload.ExpirationTime, out expirationTime))
                throw new InvalidOperationException("The server SIWS expirationTime value is invalid.");
            if (expirationTime <= issuedAt)
                throw new InvalidOperationException("The server SIWS expiration time must follow its issue time.");

            var builder = new StringBuilder(512);
            builder.Append(payload.Domain)
                .Append(" wants you to sign in with your Solana account:\n")
                .Append(payload.Address)
                .Append("\n\n")
                .Append(payload.Statement)
                .Append("\n\nURI: ")
                .Append(payload.Uri)
                .Append("\nVersion: ")
                .Append(payload.Version)
                .Append("\nChain ID: ")
                .Append(payload.ChainId)
                .Append("\nNonce: ")
                .Append(payload.Nonce)
                .Append("\nIssued At: ")
                .Append(issuedAt.ToUniversalTime().ToString("O"))
                .Append("\nExpiration Time: ")
                .Append(expirationTime.ToUniversalTime().ToString("O"));
            return builder.ToString();
        }

        private static void Require(string value, string fieldName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("The server SIWS " + fieldName + " value is missing.");
        }
    }

    public sealed class WalletAuthenticationCoordinator
    {
        public const string DevnetChainId = "solana:devnet";
        public const string TestnetChainId = "solana:testnet";
        public const string MainnetChainId = "solana:mainnet";
        public const string AndroidPlatform = "android";

        private readonly IWalletSiwsGateway wallet;
        private readonly IWalletAuthenticationTransport transport;
        private readonly IWalletAuthenticationSessionStore sessionStore;
        private readonly string clientVersion;
        private readonly string chainId;
        private readonly object sessionGate = new object();
        private int operationInProgress;
        private WalletAuthenticationSession currentSession;

        public WalletAuthenticationCoordinator(
            IWalletSiwsGateway wallet,
            IWalletAuthenticationTransport transport,
            string clientVersion)
            : this(wallet, transport, clientVersion, null, DevnetChainId)
        {
        }

        public WalletAuthenticationCoordinator(
            IWalletSiwsGateway wallet,
            IWalletAuthenticationTransport transport,
            string clientVersion,
            IWalletAuthenticationSessionStore sessionStore)
            : this(wallet, transport, clientVersion, sessionStore, DevnetChainId)
        {
        }

        public WalletAuthenticationCoordinator(
            IWalletSiwsGateway wallet,
            IWalletAuthenticationTransport transport,
            string clientVersion,
            IWalletAuthenticationSessionStore sessionStore,
            string chainId)
        {
            this.wallet = wallet ?? throw new ArgumentNullException("wallet");
            this.transport = transport ?? throw new ArgumentNullException("transport");
            if (string.IsNullOrWhiteSpace(clientVersion))
                throw new ArgumentException("A client version is required.", "clientVersion");
            if (chainId != DevnetChainId && chainId != TestnetChainId && chainId != MainnetChainId)
                throw new ArgumentException("The Solana chain ID is not supported.", "chainId");
            this.clientVersion = clientVersion;
            this.sessionStore = sessionStore;
            this.chainId = chainId;
        }

        public WalletAuthenticationSession CurrentSession
        {
            get
            {
                lock (sessionGate) return currentSession;
            }
        }

        public async Task<WalletAuthenticationSession> SignInAsync(CancellationToken cancellationToken)
        {
            return await SignInAsync(null, cancellationToken);
        }

        public async Task<WalletAuthenticationSession> SignInAsync(
            string invitationCode,
            CancellationToken cancellationToken)
        {
            BeginOperation();
            try
            {
                var identity = await wallet.ConnectAsync(cancellationToken);
                if (identity == null)
                    throw new InvalidOperationException("The wallet did not return an identity.");

                var challenge = await transport.CreateChallengeAsync(
                    identity.WalletAddress,
                    chainId,
                    clientVersion,
                    AndroidPlatform,
                    cancellationToken);
                ValidateChallenge(challenge, identity.WalletAddress);

                var message = Encoding.UTF8.GetBytes(
                    SiwsCanonicalMessageFormatter.Format(challenge.SignInPayload));
                var signature = await wallet.SignMessageAsync(
                    WalletIdentity.Copy(message), cancellationToken);
                if (signature == null || signature.Length != 64)
                    throw new InvalidOperationException("The wallet must return a 64-byte Ed25519 signature.");

                var proof = new WalletSessionProof
                {
                    ChallengeId = challenge.ChallengeId,
                    Address = Convert.ToBase64String(identity.PublicKey),
                    SignedMessage = Convert.ToBase64String(message),
                    Signature = Convert.ToBase64String(signature),
                    InvitationCode = string.IsNullOrWhiteSpace(invitationCode)
                        ? null
                        : invitationCode.Trim()
                };
                var session = await transport.CreateSessionAsync(proof, cancellationToken);
                ValidateSession(session, identity.WalletAddress);
                lock (sessionGate) currentSession = session;
                if (sessionStore != null) sessionStore.Save(session);
                return session;
            }
            finally
            {
                EndOperation();
            }
        }

        public async Task<WalletAuthenticationSession> RestoreAsync(
            CancellationToken cancellationToken)
        {
            BeginOperation();
            try
            {
                if (sessionStore == null) return null;
                var persisted = sessionStore.Load();
                if (persisted == null) return null;
                ValidatePersistedSession(persisted);
                if (!WalletSessionRefreshPolicy.HasUsableRefreshToken(
                        persisted, DateTimeOffset.UtcNow))
                {
                    sessionStore.Clear();
                    return null;
                }

                var tokens = await transport.RefreshSessionAsync(
                    persisted.RefreshToken, cancellationToken);
                ValidateTokens(tokens);
                var restored = persisted.WithRotatedTokens(tokens);
                lock (sessionGate) currentSession = restored;
                sessionStore.Save(restored);
                return restored;
            }
            finally
            {
                EndOperation();
            }
        }

        public async Task<WalletAuthenticationSession> RefreshAsync(CancellationToken cancellationToken)
        {
            BeginOperation();
            try
            {
                WalletAuthenticationSession session;
                lock (sessionGate) session = currentSession;
                if (session == null || string.IsNullOrWhiteSpace(session.RefreshToken))
                    throw new InvalidOperationException("There is no authenticated session to refresh.");

                var tokens = await transport.RefreshSessionAsync(session.RefreshToken, cancellationToken);
                ValidateTokens(tokens);
                var rotated = session.WithRotatedTokens(tokens);
                lock (sessionGate) currentSession = rotated;
                if (sessionStore != null) sessionStore.Save(rotated);
                return rotated;
            }
            finally
            {
                EndOperation();
            }
        }

        public async Task<WalletServerAccount> GetCurrentAccountAsync(CancellationToken cancellationToken)
        {
            WalletAuthenticationSession session;
            lock (sessionGate) session = currentSession;
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                throw new InvalidOperationException("There is no authenticated session.");
            return await transport.GetCurrentAccountAsync(session.AccessToken, cancellationToken);
        }

        public async Task<WalletServerAccount> UpdateDisplayNameAsync(
            string displayName,
            CancellationToken cancellationToken)
        {
            WalletAuthenticationSession session;
            lock (sessionGate) session = currentSession;
            if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
                throw new InvalidOperationException("There is no authenticated session.");
            return await transport.UpdateDisplayNameAsync(
                session.AccessToken, displayName, cancellationToken);
        }

        public async Task<bool> SignOutAsync(CancellationToken cancellationToken)
        {
            BeginOperation();
            try
            {
                WalletAuthenticationSession session;
                lock (sessionGate) session = currentSession;
                if (session == null && sessionStore != null)
                    session = sessionStore.Load();

                var serverSessionRevoked = false;
                if (session != null)
                {
                    ValidatePersistedSession(session);
                    if (WalletSessionRefreshPolicy.IsAccessExpired(
                            session, DateTimeOffset.UtcNow))
                    {
                        if (!WalletSessionRefreshPolicy.HasUsableRefreshToken(
                                session, DateTimeOffset.UtcNow))
                            throw new InvalidOperationException(
                                "The stored wallet session has expired and cannot be revoked from this device.");
                        var tokens = await transport.RefreshSessionAsync(
                            session.RefreshToken, cancellationToken);
                        ValidateTokens(tokens);
                        session = session.WithRotatedTokens(tokens);
                        lock (sessionGate) currentSession = session;
                        if (sessionStore != null) sessionStore.Save(session);
                    }

                    await transport.RevokeCurrentSessionAsync(
                        session.AccessToken, cancellationToken);
                    serverSessionRevoked = true;
                }

                if (sessionStore != null) sessionStore.Clear();
                lock (sessionGate) currentSession = null;
                await wallet.DisconnectAsync(cancellationToken);
                return serverSessionRevoked;
            }
            finally
            {
                EndOperation();
            }
        }

        private void ValidateChallenge(ServerAuthenticationChallenge challenge, string walletAddress)
        {
            if (challenge == null || challenge.SignInPayload == null)
                throw new InvalidOperationException("The server returned an invalid authentication challenge.");
            if (string.IsNullOrWhiteSpace(challenge.ChallengeId))
                throw new InvalidOperationException("The server challenge ID is missing.");
            if (!string.Equals(challenge.SignInPayload.Address, walletAddress, StringComparison.Ordinal))
                throw new InvalidOperationException("The server challenge wallet does not match the connected wallet.");
            if (!string.Equals(challenge.SignInPayload.ChainId, chainId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The server challenge does not match the configured Solana network.");
            if (!string.Equals(challenge.SignInPayload.Version, "1", StringComparison.Ordinal))
                throw new InvalidOperationException("The server returned an unsupported SIWS version.");
        }

        private static void ValidateSession(WalletAuthenticationSession session, string walletAddress)
        {
            if (session == null)
                throw new InvalidOperationException("The server did not return an authenticated session.");
            if (!string.Equals(session.WalletAddress, walletAddress, StringComparison.Ordinal))
                throw new InvalidOperationException("The authenticated session belongs to a different wallet.");
            if (string.IsNullOrWhiteSpace(session.AccountId))
                throw new InvalidOperationException("The authenticated session account ID is missing.");
            ValidateTokens(new WalletSessionTokens
            {
                AccessToken = session.AccessToken,
                AccessExpiresAt = session.AccessExpiresAt,
                RefreshToken = session.RefreshToken,
                RefreshExpiresAt = session.RefreshExpiresAt,
                RequestId = session.RequestId
            });
        }

        private static void ValidatePersistedSession(WalletAuthenticationSession session)
        {
            if (session == null ||
                string.IsNullOrWhiteSpace(session.AccountId) ||
                string.IsNullOrWhiteSpace(session.WalletAddress))
                throw new InvalidOperationException("The stored wallet session is incomplete.");
            ValidateTokens(new WalletSessionTokens
            {
                AccessToken = session.AccessToken,
                AccessExpiresAt = session.AccessExpiresAt,
                RefreshToken = session.RefreshToken,
                RefreshExpiresAt = session.RefreshExpiresAt,
                RequestId = session.RequestId
            });
        }

        private static void ValidateTokens(WalletSessionTokens tokens)
        {
            if (tokens == null ||
                string.IsNullOrWhiteSpace(tokens.AccessToken) ||
                string.IsNullOrWhiteSpace(tokens.RefreshToken))
                throw new InvalidOperationException("The server returned incomplete session tokens.");
        }

        private void BeginOperation()
        {
            if (Interlocked.CompareExchange(ref operationInProgress, 1, 0) != 0)
                throw new InvalidOperationException("A wallet authentication operation is already in progress.");
        }

        private void EndOperation()
        {
            Interlocked.Exchange(ref operationInProgress, 0);
        }
    }
}
