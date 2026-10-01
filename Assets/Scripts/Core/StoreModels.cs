using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    public enum CharacterPackTier
    {
        White = 0,
        Blue = 1,
        Orange = 2
    }

    public enum StoreProductType
    {
        CharacterPack = 0,
        BasePack = 1
    }

    public sealed class StoreProductDefinition
    {
        public string ProductId { get; }
        public StoreProductType ProductType { get; }
        public CharacterPackTier PackTier { get; }
        public string DisplayNameKey { get; }
        public int PriceSkr { get; }
        public string[] BaseSkinIds { get; }

        public StoreProductDefinition(
            string productId,
            StoreProductType productType,
            CharacterPackTier packTier,
            string displayNameKey,
            int priceSkr,
            string[] baseSkinIds = null)
        {
            ProductId = productId;
            ProductType = productType;
            PackTier = packTier;
            DisplayNameKey = displayNameKey;
            PriceSkr = priceSkr;
            BaseSkinIds = baseSkinIds ?? new string[0];
        }
    }

    public static class StoreCatalog
    {
        public const string BasePackProductId = "base.pack.harbor_pair.v1";

        private static readonly StoreProductDefinition[] products =
        {
            new StoreProductDefinition("character.pack.white.v1", StoreProductType.CharacterPack,
                CharacterPackTier.White, "store.pack.white", 50),
            new StoreProductDefinition("character.pack.blue.v1", StoreProductType.CharacterPack,
                CharacterPackTier.Blue, "store.pack.blue", 200),
            new StoreProductDefinition("character.pack.orange.v1", StoreProductType.CharacterPack,
                CharacterPackTier.Orange, "store.pack.orange", 500),
            new StoreProductDefinition(BasePackProductId, StoreProductType.BasePack,
                CharacterPackTier.White, "store.basePack.harbor", 100,
                new[] { PieceBasePresentationContract.DefaultSkinId, PieceBasePresentationContract.DefaultSkinId })
        };

        public static IReadOnlyList<StoreProductDefinition> Products => products;

        public static StoreProductDefinition Get(string productId)
        {
            for (var i = 0; i < products.Length; i++)
                if (products[i].ProductId == productId) return products[i];
            throw new KeyNotFoundException("Unknown store product: " + productId);
        }
    }

    public sealed class WalletPurchaseRequest
    {
        public string OrderId { get; }
        public string ProductId { get; }
        public int PriceSkr { get; }
        public string RulesVersion { get; }
        public string PurchaseSummary { get; }

        public WalletPurchaseRequest(string orderId, string productId, int priceSkr,
            string rulesVersion, string purchaseSummary)
        {
            OrderId = orderId;
            ProductId = productId;
            PriceSkr = priceSkr;
            RulesVersion = rulesVersion;
            PurchaseSummary = purchaseSummary;
        }
    }

    public sealed class WalletSignatureResult
    {
        public bool Approved { get; }
        public string SignatureId { get; }
        public string FailureReason { get; }

        private WalletSignatureResult(bool approved, string signatureId, string failureReason)
        {
            Approved = approved;
            SignatureId = signatureId;
            FailureReason = failureReason;
        }

        public static WalletSignatureResult Approve(string signatureId)
        {
            return new WalletSignatureResult(true, signatureId, null);
        }

        public static WalletSignatureResult Reject(string reason)
        {
            return new WalletSignatureResult(false, null, reason);
        }
    }

    public interface IWalletSignatureGateway
    {
        WalletSignatureResult RequestTransactionSignature(WalletPurchaseRequest request);
    }

    /// <summary>
    /// Offline prototype adapter. Production must replace this with the MWA/server
    /// transaction flow; gameplay code only depends on the interface above.
    /// </summary>
    public sealed class LocalSkrWalletSignatureGateway : IWalletSignatureGateway
    {
        public WalletSignatureResult RequestTransactionSignature(WalletPurchaseRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OrderId))
                return WalletSignatureResult.Reject("invalid-request");
            return WalletSignatureResult.Approve("local-wallet-signature:" + request.OrderId);
        }
    }

    public static class StoreRules
    {
        public const string StoreRulesVersion = "store.v0.1";
        public const int DrawCountPerCharacterPack = 3;
        public const int EpicPityThresholdSkr = 2500;
        public const int LegendaryPityThresholdSkr = 15000;

        public static int[] ProbabilityThresholds(CharacterPackTier tier)
        {
            // Values are cumulative basis points: common, uncommon, rare, epic.
            switch (tier)
            {
                case CharacterPackTier.White: return new[] { 7000, 9480, 9940, 9995 };
                case CharacterPackTier.Blue: return new[] { 1850, 7400, 9680, 9970 };
                case CharacterPackTier.Orange: return new[] { 0, 4480, 8700, 9870 };
                default: throw new ArgumentOutOfRangeException(nameof(tier));
            }
        }

        public static CharacterQuality RollQuality(CharacterPackTier tier, string drawSeed,
            bool forceEpic, bool forceLegendary)
        {
            if (forceLegendary) return CharacterQuality.Legendary;
            var thresholds = ProbabilityThresholds(tier);
            var sample = HashToRange(drawSeed, 10000);
            CharacterQuality quality;
            if (sample < thresholds[0]) quality = CharacterQuality.Common;
            else if (sample < thresholds[1]) quality = CharacterQuality.Uncommon;
            else if (sample < thresholds[2]) quality = CharacterQuality.Rare;
            else if (sample < thresholds[3]) quality = CharacterQuality.Epic;
            else quality = CharacterQuality.Legendary;
            if (forceEpic && quality != CharacterQuality.Epic && quality != CharacterQuality.Legendary)
                quality = CharacterQuality.Epic;
            return quality;
        }

        public static string[] SelectDistinctCharacterIds(CharacterCatalog catalog, string seed, int count)
        {
            var candidates = new List<string>();
            foreach (var definition in catalog.Definitions)
                if (definition.IsCollectible) candidates.Add(definition.DefinitionId);
            if (count > candidates.Count) throw new ArgumentOutOfRangeException(nameof(count));
            var result = new List<string>();
            var cursor = HashToRange(seed + ":start", candidates.Count);
            var step = 1 + HashToRange(seed + ":step", Math.Max(1, candidates.Count - 1));
            while (result.Count < count)
            {
                var id = candidates[cursor % candidates.Count];
                if (!result.Contains(id)) result.Add(id);
                cursor += step;
            }
            return result.ToArray();
        }

        public static int HashToRange(string seed, int exclusiveMax)
        {
            if (exclusiveMax <= 0) return 0;
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(seed ?? string.Empty));
                var value = (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
                return (int)(value % (uint)exclusiveMax);
            }
        }
    }
}
