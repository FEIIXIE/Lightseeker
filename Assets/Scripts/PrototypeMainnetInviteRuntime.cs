using System;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private const string MainnetInviteConfigurationResource =
            "MainnetInviteRuntimeConfig";

        private string mainnetInviteSeasonId;
        private string mainnetAccessMode;

        public bool IsMainnetInviteRuntimeConfigured
        {
            get { return !string.IsNullOrWhiteSpace(mainnetInviteSeasonId); }
        }

        public string MainnetInviteSeasonId
        {
            get { return mainnetInviteSeasonId; }
        }

        public bool IsMainnetPublicAccess
        {
            get { return string.Equals(mainnetAccessMode, "public", StringComparison.Ordinal); }
        }

        private void TryConfigureMainnetInviteRuntime()
        {
            var asset = Resources.Load<TextAsset>(MainnetInviteConfigurationResource);
            if (asset == null) return;

            var configuration = JsonUtility.FromJson<MainnetInviteRuntimeConfiguration>(
                asset.text);
            ValidateMainnetInviteRuntimeConfiguration(configuration);
            ConfigureWalletServerRuntime(
                configuration.serverBaseUrl,
                allowInsecureLocalDevelopment: false,
                configuration.identityUri,
                configuration.iconUri,
                configuration.applicationName,
                 WalletAuthenticationCoordinator.MainnetChainId);
            mainnetInviteSeasonId = configuration.expeditionSeasonId.Trim();
            mainnetAccessMode = configuration.accessMode.Trim().ToLowerInvariant();
            InitializeMainnetInviteLogin();
        }

        public static void ValidateMainnetInviteRuntimeConfiguration(
            MainnetInviteRuntimeConfiguration configuration)
        {
            if (configuration == null)
                throw new InvalidOperationException(
                    "The mainnet invite runtime configuration is missing or malformed.");
            RequireAbsoluteHttps(configuration.serverBaseUrl, "serverBaseUrl");
            RequireAbsoluteHttps(configuration.identityUri, "identityUri");
            if (string.IsNullOrWhiteSpace(configuration.iconUri) ||
                Uri.IsWellFormedUriString(configuration.iconUri, UriKind.Absolute))
                throw new InvalidOperationException(
                    "The mainnet invite icon URI must be a non-empty relative URI.");
            if (string.IsNullOrWhiteSpace(configuration.applicationName))
                throw new InvalidOperationException(
                    "The mainnet invite application name is required.");
            if (!string.Equals(configuration.chainId,
                    WalletAuthenticationCoordinator.MainnetChainId,
                    StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The mainnet invite build must use solana:mainnet.");
            if (string.IsNullOrWhiteSpace(configuration.expeditionSeasonId))
                throw new InvalidOperationException(
                    "The mainnet invite expedition season is required.");
            if (!string.Equals(configuration.accessMode, "invite", StringComparison.Ordinal) &&
                !string.Equals(configuration.accessMode, "public", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "The mainnet access mode must be exactly 'invite' or 'public'.");
        }

        private static void RequireAbsoluteHttps(string value, string field)
        {
            Uri parsed;
            if (!Uri.TryCreate(value, UriKind.Absolute, out parsed) ||
                !string.Equals(parsed.Scheme, Uri.UriSchemeHttps,
                    StringComparison.OrdinalIgnoreCase) ||
                !string.IsNullOrEmpty(parsed.UserInfo) ||
                !string.IsNullOrEmpty(parsed.Query) ||
                !string.IsNullOrEmpty(parsed.Fragment))
                throw new InvalidOperationException(
                    "The mainnet invite " + field +
                    " must be an absolute HTTPS URI without credentials, query, or fragment.");
        }
    }

    [Serializable]
    public sealed class MainnetInviteRuntimeConfiguration
    {
        public string serverBaseUrl;
        public string identityUri;
        public string iconUri;
        public string applicationName;
        public string chainId;
        public string expeditionSeasonId;
        public string accessMode;
    }
}
