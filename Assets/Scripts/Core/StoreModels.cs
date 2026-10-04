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

    /// <summary>
    /// Immutable handoff contract for the approved Store E v1.1.0 art package.
    /// Runtime code may position the complete module canvases at native size and
    /// rotate only the authored card/base modules. The full-page preview is not a
    /// runtime asset.
    /// </summary>
    public static class StoreFormalArtContract
    {
        public const string PackageVersion = "store-e-v1.1.0";
        public const string ResourceRoot = "Art/Store/V110/modules/";
        public const string BackgroundVeilResourceId =
            "ART-UI-STORE-E-BACKGROUND-VEIL-1080x2400-v1.1.0";
        public const string BalancePanelResourceId =
            "ART-UI-STORE-E-BALANCE-PANEL-240x96-v1.1.0";
        public const string DefenderBaseResourceId =
            "ART-UI-STORE-E-BASE-DEFENDER-DISPLAY-308x116-v1.1.0";
        public const string EnemyBaseResourceId =
            "ART-UI-STORE-E-BASE-ENEMY-DISPLAY-308x116-v1.1.0";
        public const string CenterCardBackResourceId =
            "ART-UI-STORE-E-CARDBACK-CENTER-154x244-v1.1.0";
        public const string SideCardBackResourceId =
            "ART-UI-STORE-E-CARDBACK-SIDE-142x224-v1.1.0";
        public const string HeaderBadgeResourceId =
            "ART-UI-STORE-E-HEADER-BADGE-228x48-v1.1.0";
        public const string HeaderDividerResourceId =
            "ART-UI-STORE-E-HEADER-DIVIDER-964x2-v1.1.0";
        public const string InstructionPanelResourceId =
            "ART-UI-STORE-E-INSTRUCTION-PANEL-912x234-v1.1.0";
        public const string DefenderLabelResourceId =
            "ART-UI-STORE-E-LABEL-DEFENDER-108x40-v1.1.0";
        public const string EnemyLabelResourceId =
            "ART-UI-STORE-E-LABEL-ENEMY-108x40-v1.1.0";
        public const string MainBoardResourceId =
            "ART-UI-STORE-E-MAIN-BOARD-988x1814-v1.1.0";
        public const string PityStripResourceId =
            "ART-UI-STORE-E-PITY-STRIP-624x76-v1.1.0";
        public const string ProductCardResourceId =
            "ART-UI-STORE-E-PRODUCT-CARD-442x634-v1.1.0";
        public const string TallProductCardResourceId =
            "ART-UI-STORE-E-PRODUCT-CARD-TALL-442x654-v1.1.0";
        public const string PurchaseButtonResourceId =
            "ART-UI-STORE-E-PURCHASE-BUTTON-322x62-v1.1.0";
        public const string SkrBadgeResourceId =
            "ART-UI-STORE-E-SKR-BADGE-58x62-v1.1.0";

        public static readonly string[] QualityBadgeResourceIds =
        {
            "ART-UI-STORE-E-QUALITY-COMMON-30x30-v1.1.0",
            "ART-UI-STORE-E-QUALITY-UNCOMMON-30x30-v1.1.0",
            "ART-UI-STORE-E-QUALITY-RARE-30x30-v1.1.0",
            "ART-UI-STORE-E-QUALITY-EPIC-30x30-v1.1.0",
            "ART-UI-STORE-E-QUALITY-LEGENDARY-30x30-v1.1.0"
        };

        public const int SideCardWidth = 142;
        public const int SideCardHeight = 224;
        public const int CenterCardWidth = 154;
        public const int CenterCardHeight = 244;
        public const float LeftCardAngle = -11f;
        public const float RightCardAngle = 11f;

        public const int BaseWidth = 308;
        public const int BaseHeight = 116;
        public const float DefenderBaseAngle = -7f;
        public const float EnemyBaseAngle = 8f;

        public const float FrozenNavigationTop = 2158f;
        public const float FrozenNavigationHeight = 242f;
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

    public sealed class WalletDeckChangeRequest
    {
        public string OperationId { get; }
        public string RulesVersion { get; }
        public string ChangeSummary { get; }

        public WalletDeckChangeRequest(string operationId, string rulesVersion, string changeSummary)
        {
            OperationId = operationId;
            RulesVersion = rulesVersion;
            ChangeSummary = changeSummary;
        }
    }

    public interface IWalletSignatureGateway
    {
        WalletSignatureResult RequestTransactionSignature(WalletPurchaseRequest request);
        WalletSignatureResult RequestDeckChangeSignature(WalletDeckChangeRequest request);
        WalletSignatureResult RequestMarketActionSignature(WalletMarketActionRequest request);
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

        public WalletSignatureResult RequestDeckChangeSignature(WalletDeckChangeRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperationId))
                return WalletSignatureResult.Reject("invalid-request");
            return WalletSignatureResult.Approve("local-wallet-signature:" + request.OperationId);
        }

        public WalletSignatureResult RequestMarketActionSignature(WalletMarketActionRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.OperationId) ||
                string.IsNullOrEmpty(request.WalletId) || string.IsNullOrEmpty(request.RulesVersion))
                return WalletSignatureResult.Reject("invalid-market-request");
            return WalletSignatureResult.Approve("local-wallet-signature:" + request.OperationId);
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
            // The former modular-step walk could cycle over only a subset when its
            // step shared a divisor with the roster size. Asking for a third distinct
            // card then spun on the main thread until integer overflow. A seeded
            // Fisher-Yates shuffle is bounded, deterministic and always distinct.
            for (var index = candidates.Count - 1; index > 0; index--)
            {
                var swapIndex = HashToRange(seed + ":shuffle:" + index, index + 1);
                var temporary = candidates[index];
                candidates[index] = candidates[swapIndex];
                candidates[swapIndex] = temporary;
            }
            return candidates.GetRange(0, count).ToArray();
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

    public static class StoreRevealPresentationPolicy
    {
        public const float MinimumLoadingAnimationSeconds = 0.75f;
        public const float CardRevealDurationSeconds = 1.35f;

        public static bool CanBeginCardReveal(bool assetsReady, float loadingElapsedSeconds)
        {
            return assetsReady && loadingElapsedSeconds >= MinimumLoadingAnimationSeconds;
        }
    }
}
