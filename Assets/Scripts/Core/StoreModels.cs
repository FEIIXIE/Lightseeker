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
        BasePack = 1,
        Battlefield = 2
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
        public const string CenterCardBackResourceId =
            "ART-UI-STORE-E-CARDBACK-CENTER-154x244-v1.1.0";
        public const string SideCardBackResourceId =
            "ART-UI-STORE-E-CARDBACK-SIDE-142x224-v1.1.0";
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

        public const float FrozenNavigationTop = 2158f;
        public const float FrozenNavigationHeight = 242f;
    }

    /// <summary>
    /// Native-size runtime contract for the accepted Store single-column v1.6.0 package.
    /// Only products backed by StoreCatalog may be rendered as purchasable entries.
    /// The remaining accepted art stays outside the build until its product contract exists.
    /// </summary>
    public static class StoreSingleColumnFormalArtContract
    {
        public const string PackageVersion = "store-page-single-column-continuous-formal-v1.6.0";
        public const string ResourceRoot = "Art/Store/V160/modules/";
        public const string ProductRow =
            "ART-UI-STORE-SINGLE-COLUMN-PRODUCT-ROW-912x330-v1.6.0";
        public const string CardPackChuhang =
            "ART-UI-STORE-SINGLE-COLUMN-CARDPACK-CHUHANG-280x236-v1.6.0";
        public const string CardPackZhuchao =
            "ART-UI-STORE-SINGLE-COLUMN-CARDPACK-ZHUCHAO-280x236-v1.6.0";
        public const string CardPackGuihang =
            "ART-UI-STORE-SINGLE-COLUMN-CARDPACK-GUIHANG-280x236-v1.6.0";
        public const string BasePairMistHarbor =
            "ART-UI-STORE-SINGLE-COLUMN-BASEPAIR-MIST-HARBOR-300x180-v1.6.0";
        public const string BasePairLighthouseWhite =
            "ART-UI-STORE-SINGLE-COLUMN-BASEPAIR-LIGHTHOUSE-WHITE-300x180-v1.6.0";
        public const string BasePairDeeptideAstral =
            "ART-UI-STORE-SINGLE-COLUMN-BASEPAIR-DEEPTIDE-ASTRAL-300x180-v1.6.0";
        public const string BattlefieldMistHarbor =
            "ART-UI-STORE-SINGLE-COLUMN-BATTLEFIELD-MIST-HARBOR-TABLETOP-300x220-v1.6.0";
        public const string BattlefieldLighthouseCourtyard =
            "ART-UI-STORE-SINGLE-COLUMN-BATTLEFIELD-LIGHTHOUSE-COURTYARD-TABLETOP-300x220-v1.6.0";
        public const string BattlefieldDeeptideAstral =
            "ART-UI-STORE-SINGLE-COLUMN-BATTLEFIELD-DEEPTIDE-ASTRAL-TABLETOP-300x220-v1.6.0";

        public const int ProductRowWidth = 912;
        public const int ProductRowHeight = 330;
        public const int CharacterPackWidth = 280;
        public const int CharacterPackHeight = 236;
        public const int BasePairWidth = 300;
        public const int BasePairHeight = 180;
        public const int BattlefieldWidth = 300;
        public const int BattlefieldHeight = 220;

        public static readonly string[] CharacterPackArt =
        {
            CardPackChuhang,
            CardPackZhuchao,
            CardPackGuihang
        };

        public static readonly string[] BasePairArt =
        {
            BasePairMistHarbor,
            BasePairLighthouseWhite,
            BasePairDeeptideAstral
        };

        public static readonly string[] BattlefieldArt =
        {
            BattlefieldMistHarbor,
            BattlefieldLighthouseCourtyard,
            BattlefieldDeeptideAstral
        };

        public const float ViewportX = 76f;
        public const float ViewportY = 190f;
        public const float ViewportWidth = 928f;
        public const float ViewportHeight = 1968f;
        public const float CurrentCatalogContentHeight = 3286f;
    }

    /// <summary>
    /// Runtime bindings for the accepted Store v1.7.0 cosmetic originals. Product
    /// thumbnails and playable art are intentionally separate: the former may only
    /// be used in store/warehouse UI, while the latter is the actual equipped skin.
    /// </summary>
    public static class StoreCosmeticFormalArtContract
    {
        public const string PackageVersion = "store-page-single-column-continuous-formal-v1.7.0";
        public const string BattlefieldPackageVersion = "store-battlefield-safe-area-v1.8.0";
        public const string BattlefieldResourceRoot = "Art/Battlefields/V180/";
        public const string BaseResourceRoot = "Art/Store/V170/bases/";
        public const string WarehouseThumbnailResourceRoot = "Art/Store/V170/warehouse-thumbnails/";

        public const int BattlefieldWidth = 1080;
        public const int BattlefieldHeight = 2400;
        public const int BaseWidth = 192;
        public const int BaseHeight = 72;
        public const int BaseFootAnchorX = 96;
        public const int BaseFootAnchorY = 31;
        public const int WarehouseThumbnailWidth = 184;
        public const int WarehouseThumbnailHeight = 184;

        public static string BattlefieldResourceId(string productId)
        {
            switch (productId)
            {
                case "battlefield.mist_harbor.v1":
                    return "ART-BATTLE-RUNTIME-MIST-HARBOR-1080x2400-v1.8.0";
                case "battlefield.lighthouse_courtyard.v1":
                    return "ART-BATTLE-RUNTIME-LIGHTHOUSE-COURTYARD-1080x2400-v1.8.0";
                case "battlefield.deeptide_astral.v1":
                    return "ART-BATTLE-RUNTIME-DEEPTIDE-ASTRAL-1080x2400-v1.8.0";
                default:
                    return null;
            }
        }

        public static string BaseResourceId(string productId, bool defender)
        {
            var side = defender ? "DEFENDER" : "ENEMY";
            switch (productId)
            {
                case "base.pack.harbor_pair.v1":
                    return "ART-BASE-RUNTIME-MIST-HARBOR-" + side + "-192x72-v1.7.0";
                case "base.pack.lighthouse_white.v1":
                    return "ART-BASE-RUNTIME-LIGHTHOUSE-WHITE-" + side + "-192x72-v1.7.0";
                case "base.pack.deeptide_astral.v1":
                    return "ART-BASE-RUNTIME-DEEPTIDE-ASTRAL-" + side + "-192x72-v1.7.0";
                default:
                    return null;
            }
        }

        public static string WarehouseThumbnailResourceId(string productId)
        {
            switch (productId)
            {
                case "base.pack.harbor_pair.v1":
                    return "ART-UI-WAREHOUSE-A-COSMETIC-BASE-MIST-HARBOR-184x184-v1.7.0";
                case "base.pack.lighthouse_white.v1":
                    return "ART-UI-WAREHOUSE-A-COSMETIC-BASE-LIGHTHOUSE-WHITE-184x184-v1.7.0";
                case "base.pack.deeptide_astral.v1":
                    return "ART-UI-WAREHOUSE-A-COSMETIC-BASE-DEEPTIDE-ASTRAL-184x184-v1.7.0";
                case "battlefield.mist_harbor.v1":
                    return "ART-UI-WAREHOUSE-A-COSMETIC-BATTLEFIELD-MIST-HARBOR-184x184-v1.7.0";
                case "battlefield.lighthouse_courtyard.v1":
                    return "ART-UI-WAREHOUSE-A-COSMETIC-BATTLEFIELD-LIGHTHOUSE-COURTYARD-184x184-v1.7.0";
                case "battlefield.deeptide_astral.v1":
                    return "ART-UI-WAREHOUSE-A-COSMETIC-BATTLEFIELD-DEEPTIDE-ASTRAL-184x184-v1.7.0";
                default:
                    return null;
            }
        }

        public static string ThumbnailResourceId(string productId)
        {
            switch (productId)
            {
                case "base.pack.harbor_pair.v1": return StoreSingleColumnFormalArtContract.BasePairArt[0];
                case "base.pack.lighthouse_white.v1": return StoreSingleColumnFormalArtContract.BasePairArt[1];
                case "base.pack.deeptide_astral.v1": return StoreSingleColumnFormalArtContract.BasePairArt[2];
                case "battlefield.mist_harbor.v1": return StoreSingleColumnFormalArtContract.BattlefieldArt[0];
                case "battlefield.lighthouse_courtyard.v1": return StoreSingleColumnFormalArtContract.BattlefieldArt[1];
                case "battlefield.deeptide_astral.v1": return StoreSingleColumnFormalArtContract.BattlefieldArt[2];
                default: return null;
            }
        }

        public static bool HasNativeRuntimeVisual(string productId)
        {
            return !string.IsNullOrEmpty(BattlefieldResourceId(productId)) ||
                   !string.IsNullOrEmpty(BaseResourceId(productId, true));
        }
    }

    public sealed class StoreProductDefinition
    {
        public string ProductId { get; }
        public StoreProductType ProductType { get; }
        public CharacterPackTier PackTier { get; }
        public string DisplayNameKey { get; }
        public int PriceSkr { get; }
        public string[] BaseSkinIds { get; }
        public bool IsPurchasable => PriceSkr > 0;

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
                new[] { PieceBasePresentationContract.DefaultSkinId, PieceBasePresentationContract.DefaultSkinId }),
            new StoreProductDefinition("base.pack.lighthouse_white.v1", StoreProductType.BasePack,
                CharacterPackTier.White, "store.basePack.lighthouse", 100),
            new StoreProductDefinition("base.pack.deeptide_astral.v1", StoreProductType.BasePack,
                CharacterPackTier.White, "store.basePack.deeptide", 100),
            new StoreProductDefinition("battlefield.mist_harbor.v1", StoreProductType.Battlefield,
                CharacterPackTier.White, "store.battlefield.mistHarbor", 100),
            new StoreProductDefinition("battlefield.lighthouse_courtyard.v1", StoreProductType.Battlefield,
                CharacterPackTier.White, "store.battlefield.lighthouse", 100),
            new StoreProductDefinition("battlefield.deeptide_astral.v1", StoreProductType.Battlefield,
                CharacterPackTier.White, "store.battlefield.deeptide", 100)
        };

        public static IReadOnlyList<StoreProductDefinition> Products => products;

        public static StoreProductDefinition Get(string productId)
        {
            for (var i = 0; i < products.Length; i++)
                if (products[i].ProductId == productId) return products[i];
            throw new KeyNotFoundException("Unknown store product: " + productId);
        }

        public static bool TryGet(string productId, out StoreProductDefinition product)
        {
            for (var i = 0; i < products.Length; i++)
            {
                if (!string.Equals(products[i].ProductId, productId, StringComparison.Ordinal)) continue;
                product = products[i];
                return true;
            }
            product = null;
            return false;
        }
    }

    public static class StoreCosmeticRules
    {
        public static bool CanEquip(string productId, StoreProductType expectedType,
            IEnumerable<string> ownedProductIds)
        {
            if (string.IsNullOrEmpty(productId) || ownedProductIds == null) return false;
            StoreProductDefinition product;
            if (!StoreCatalog.TryGet(productId, out product) || product.ProductType != expectedType)
                return false;
            foreach (var ownedId in ownedProductIds)
                if (string.Equals(productId, ownedId, StringComparison.Ordinal)) return true;
            return false;
        }

        public static string NormalizeEquipped(string productId, StoreProductType expectedType,
            IEnumerable<string> ownedProductIds)
        {
            return CanEquip(productId, expectedType, ownedProductIds) ? productId : null;
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
