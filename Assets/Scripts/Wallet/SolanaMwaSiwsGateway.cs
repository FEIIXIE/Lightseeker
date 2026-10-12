using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SeekerAutoChess.Core;
using Solana.Unity.Programs;
using Solana.Unity.Rpc.Models;
using Solana.Unity.Rpc.Types;
using Solana.Unity.SDK;
using Solana.Unity.Wallet;
using Solana.Unity.Wallet.Utilities;
using UnityEngine;

namespace SeekerAutoChess.Wallet
{
    /// <summary>
    /// Android Mobile Wallet Adapter bridge for SIWS and server-authorized expedition payments.
    /// </summary>
    public sealed class SolanaMwaSiwsGateway : IWalletSiwsGateway, IExpeditionPaymentGateway
    {
        private readonly SolanaWalletAdapterOptions options;
        private readonly IMwaAuthCache authCache = new VolatileAuthCache();
        private readonly IMwaWalletSelectionCache walletSelectionCache = new VolatileWalletSelectionCache();
        private readonly string chainId;
        private readonly RpcCluster rpcCluster;
        private SolanaWalletAdapter adapter;

        public SolanaMwaSiwsGateway(string identityUri, string iconUri, string applicationName)
            : this(identityUri, iconUri, applicationName,
                WalletAuthenticationCoordinator.DevnetChainId)
        {
        }

        public SolanaMwaSiwsGateway(string identityUri, string iconUri, string applicationName,
            string chainId)
        {
            Uri parsedIdentity;
            if (!Uri.TryCreate(identityUri, UriKind.Absolute, out parsedIdentity) ||
                !string.Equals(parsedIdentity.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The MWA identity URI must be an absolute HTTPS URI.", "identityUri");
            if (string.IsNullOrWhiteSpace(iconUri) || Uri.IsWellFormedUriString(iconUri, UriKind.Absolute))
                throw new ArgumentException("The MWA icon URI must be a non-empty relative URI.", "iconUri");
            if (string.IsNullOrWhiteSpace(applicationName))
                throw new ArgumentException("The MWA application name is required.", "applicationName");

            this.chainId = NormalizeChainId(chainId);
            rpcCluster = MapRpcCluster(this.chainId);

            options = new SolanaWalletAdapterOptions
            {
                solanaMobileWalletAdapterOptions = new SolanaMobileWalletAdapterOptions
                {
                    identityUri = parsedIdentity.AbsoluteUri,
                    iconUri = iconUri,
                    name = applicationName,
                    keepConnectionAlive = true
                }
            };
        }

        public async Task<string> SubmitSplTokenPaymentAsync(
            string expectedWalletAddress,
            string networkId,
            string tokenMintAddress,
            string treasuryWalletAddress,
            long amountInSmallestUnits,
            int tokenDecimals,
            string intentMemo,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.Equals(chainId, NormalizeChainId(networkId), StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The payment network does not match the authenticated wallet network.");
            if (amountInSmallestUnits <= 0)
                throw new ArgumentOutOfRangeException("amountInSmallestUnits");
            if (tokenDecimals < 0 || tokenDecimals > 18)
                throw new ArgumentOutOfRangeException("tokenDecimals");
            if (string.IsNullOrWhiteSpace(intentMemo))
                throw new ArgumentException("A payment intent memo is required.", "intentMemo");

            var activeAdapter = adapter;
            if (activeAdapter == null || activeAdapter.Account == null ||
                activeAdapter.Account.PublicKey == null)
                throw new InvalidOperationException(
                    "Connect the mobile wallet before submitting a payment.");
            if (!string.Equals(activeAdapter.Account.PublicKey.Key, expectedWalletAddress,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The connected wallet does not match the authenticated account.");

            PublicKey mint;
            PublicKey treasury;
            try
            {
                mint = new PublicKey(tokenMintAddress);
                treasury = new PublicKey(treasuryWalletAddress);
            }
            catch (Exception exception)
            {
                throw new ArgumentException("The server returned an invalid Solana payment address.",
                    "tokenMintAddress", exception);
            }

            var sourceTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                activeAdapter.Account.PublicKey, mint);
            var destinationTokenAccount = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(
                treasury, mint);
            var blockHash = await activeAdapter.GetBlockHash(Commitment.Confirmed, useCache: false);
            if (string.IsNullOrWhiteSpace(blockHash))
                throw new InvalidOperationException("A recent Solana blockhash was not available.");

            var transaction = new Transaction
            {
                RecentBlockHash = blockHash,
                FeePayer = activeAdapter.Account.PublicKey,
                Instructions = new List<TransactionInstruction>
                {
                    MemoProgram.NewMemo(activeAdapter.Account.PublicKey, intentMemo),
                    TokenProgram.TransferChecked(
                        sourceTokenAccount,
                        destinationTokenAccount,
                        checked((ulong)amountInSmallestUnits),
                        tokenDecimals,
                        activeAdapter.Account.PublicKey,
                        mint,
                        new PublicKey[0])
                },
                Signatures = new List<SignaturePubKeyPair>()
            };

            var result = await activeAdapter.SignAndSendTransactions(
                new[] { transaction },
                new SignAndSendTransactionsOptions
                {
                    Commitment = "confirmed",
                    SkipPreflight = false,
                    MaxRetries = 3
                });
            cancellationToken.ThrowIfCancellationRequested();

            var success = result as SignAndSendTxResult.Success;
            if (success != null && success.Signatures != null && success.Signatures.Length == 1 &&
                success.Signatures[0] != null && success.Signatures[0].Length == 64)
                return Encoders.Base58.EncodeData(success.Signatures[0]);
            if (result is SignAndSendTxResult.UserDeclined)
                throw new InvalidOperationException("The wallet declined the payment transaction.");
            if (result is SignAndSendTxResult.NotSubmitted)
                throw new InvalidOperationException("The wallet did not submit the payment transaction.");
            if (result is SignAndSendTxResult.InvalidPayloads)
                throw new InvalidOperationException("The wallet rejected the payment transaction payload.");
            if (result is SignAndSendTxResult.TooManyPayloads)
                throw new InvalidOperationException("The wallet rejected the payment transaction batch.");
            if (result is SignAndSendTxResult.NotSupported)
                throw new InvalidOperationException(
                    "The connected wallet does not support transaction submission.");
            if (result is SignAndSendTxResult.Unauthorized)
                throw new InvalidOperationException("The wallet payment authorization expired.");
            var failed = result as SignAndSendTxResult.Failed;
            throw new InvalidOperationException(failed == null || string.IsNullOrWhiteSpace(failed.Message)
                ? "The wallet payment transaction failed."
                : "The wallet payment transaction failed: " + failed.Message);
        }

        public async Task<WalletIdentity> ConnectAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var activeAdapter = GetOrCreateAdapter();
            var account = activeAdapter.Account ?? await activeAdapter.Login();
            if (cancellationToken.IsCancellationRequested)
            {
                await DisconnectAsync(CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (account == null || account.PublicKey == null || account.PublicKey.KeyBytes == null ||
                account.PublicKey.KeyBytes.Length != 32 || string.IsNullOrWhiteSpace(account.PublicKey.Key))
                throw new InvalidOperationException("The mobile wallet did not return a valid Solana account.");

            return new WalletIdentity(account.PublicKey.Key, account.PublicKey.KeyBytes);
        }

        public async Task<byte[]> SignMessageAsync(byte[] message, CancellationToken cancellationToken)
        {
            if (message == null || message.Length == 0)
                throw new ArgumentException("A non-empty SIWS message is required.", "message");
            cancellationToken.ThrowIfCancellationRequested();

            var activeAdapter = adapter;
            if (activeAdapter == null || activeAdapter.Account == null)
                throw new InvalidOperationException("Connect the mobile wallet before requesting a signature.");

            var messageCopy = WalletIdentity.Copy(message);
            var signedPayload = await activeAdapter.SignMessage(messageCopy);
            if (cancellationToken.IsCancellationRequested)
            {
                await DisconnectAsync(CancellationToken.None);
                cancellationToken.ThrowIfCancellationRequested();
            }
            return WalletSignedPayload.ExtractEd25519Signature(messageCopy, signedPayload);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            var activeAdapter = adapter;
            adapter = null;
            if (activeAdapter == null) return;

            await activeAdapter.DisconnectWallet();
        }

        private SolanaWalletAdapter GetOrCreateAdapter()
        {
            if (Application.platform != RuntimePlatform.Android)
                throw new PlatformNotSupportedException("Mobile Wallet Adapter is available only in an Android player.");

            if (adapter == null)
            {
                adapter = new SolanaWalletAdapter(
                    options,
                    rpcCluster,
                    autoConnectOnStartup: false,
                    authCache: authCache,
                    walletSelectionCache: walletSelectionCache);
            }

            return adapter;
        }

        private static string NormalizeChainId(string value)
        {
            var normalized = string.IsNullOrWhiteSpace(value)
                ? string.Empty
                : value.Trim().ToLowerInvariant();
            if (normalized == WalletAuthenticationCoordinator.DevnetChainId ||
                normalized == WalletAuthenticationCoordinator.TestnetChainId ||
                normalized == WalletAuthenticationCoordinator.MainnetChainId)
                return normalized;
            throw new ArgumentException("An explicit supported Solana chain id is required.", "chainId");
        }

        private static RpcCluster MapRpcCluster(string value)
        {
            if (value == WalletAuthenticationCoordinator.DevnetChainId) return RpcCluster.DevNet;
            if (value == WalletAuthenticationCoordinator.TestnetChainId) return RpcCluster.TestNet;
            if (value == WalletAuthenticationCoordinator.MainnetChainId) return RpcCluster.MainNet;
            throw new ArgumentException("The Solana chain id is not supported.", "chainId");
        }

        private sealed class VolatileAuthCache : IMwaAuthCache
        {
            private string token;

            public Task<string> Get()
            {
                return Task.FromResult(token);
            }

            public Task Set(string authToken)
            {
                if (!string.IsNullOrEmpty(authToken)) token = authToken;
                return Task.CompletedTask;
            }

            public Task Clear()
            {
                token = null;
                return Task.CompletedTask;
            }
        }

        private sealed class VolatileWalletSelectionCache : IMwaWalletSelectionCache
        {
            private string packageName;

            public Task<string> GetSelectedWalletPackage()
            {
                return Task.FromResult(packageName);
            }

            public Task SetSelectedWalletPackage(string selectedPackageName)
            {
                if (!string.IsNullOrEmpty(selectedPackageName)) packageName = selectedPackageName;
                return Task.CompletedTask;
            }

            public Task ClearSelectedWalletPackage()
            {
                packageName = null;
                return Task.CompletedTask;
            }
        }
    }
}
