using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum MarketAssetKind
    {
        Equipment = 0,
        Lantern = 1
    }

    public enum MarketListingState
    {
        Active = 0,
        Sold = 1,
        Cancelled = 2
    }

    public enum MarketActionType
    {
        CreateListing = 0,
        Purchase = 1,
        CancelListing = 2,
        LeaseShop = 3
    }

    public static class MarketRules
    {
        public const string RulesVersion = "market.fixed-price.v3";
        public const string LocalPlayerId = "current-player";
        public const long MicroSkrPerSkr = 1000000L;
        public const int MinimumListingPriceSkr = 10;
        public const int InitialListingSlots = 5;
        public const int SellerFeeBasisPoints = 500;
        public const int BurnFeeBasisPoints = 250;
        public const int TeamFeeBasisPoints = 250;
        public const int SlotsPerShopLease = 5;
        public const int ShopLeasePriceSkr = 100;
        public const int MaximumShopLeasePurchasesPerSeason = 1;
        public const int MaximumListingSlots = 10;

        public static long ToMicroSkr(int skr)
        {
            if (skr < 0) throw new ArgumentOutOfRangeException(nameof(skr));
            return checked((long)skr * MicroSkrPerSkr);
        }

        public static string FormatMicroSkr(long microSkr)
        {
            if (microSkr < 0) throw new ArgumentOutOfRangeException(nameof(microSkr));
            var whole = microSkr / MicroSkrPerSkr;
            var remainder = microSkr % MicroSkrPerSkr;
            if (remainder == 0) return whole.ToString();
            return whole + "." + remainder.ToString("D6").TrimEnd('0');
        }

        public static MarketFeeBreakdown CalculateSettlement(long listingPriceMicroSkr)
        {
            if (listingPriceMicroSkr < ToMicroSkr(MinimumListingPriceSkr))
                throw new ArgumentOutOfRangeException(nameof(listingPriceMicroSkr));
            var burn = checked(listingPriceMicroSkr * BurnFeeBasisPoints / 10000L);
            var team = checked(listingPriceMicroSkr * TeamFeeBasisPoints / 10000L);
            var fee = checked(burn + team);
            return new MarketFeeBreakdown(checked(listingPriceMicroSkr - fee), fee, burn, team,
                listingPriceMicroSkr);
        }
    }

    public sealed class MarketFeeBreakdown
    {
        public long SellerProceedsMicroSkr { get; }
        public long SellerFeeMicroSkr { get; }
        public long BurnMicroSkr { get; }
        public long TeamMicroSkr { get; }
        public long BuyerTotalMicroSkr { get; }

        public MarketFeeBreakdown(long sellerProceedsMicroSkr, long sellerFeeMicroSkr,
            long burnMicroSkr, long teamMicroSkr, long buyerTotalMicroSkr)
        {
            SellerProceedsMicroSkr = sellerProceedsMicroSkr;
            SellerFeeMicroSkr = sellerFeeMicroSkr;
            BurnMicroSkr = burnMicroSkr;
            TeamMicroSkr = teamMicroSkr;
            BuyerTotalMicroSkr = buyerTotalMicroSkr;
        }
    }

    public sealed class MarketListing
    {
        public string ListingId { get; }
        public string SellerId { get; }
        public string SellerDisplayName { get; }
        public MarketAssetKind AssetKind { get; }
        public string AssetInstanceId { get; }
        public string TemplateId { get; }
        public int Quality { get; }
        public string SeasonId { get; }
        public long PriceMicroSkr { get; }
        public long CreatedAtUtcTicks { get; }
        public MarketListingState State { get; internal set; }
        public string BuyerId { get; internal set; }
        public long ResolvedAtUtcTicks { get; internal set; }

        public MarketListing(string listingId, string sellerId, string sellerDisplayName,
            MarketAssetKind assetKind, string assetInstanceId, string templateId, int quality,
            string seasonId, long priceMicroSkr, long createdAtUtcTicks,
            MarketListingState state = MarketListingState.Active, string buyerId = null,
            long resolvedAtUtcTicks = 0)
        {
            if (string.IsNullOrWhiteSpace(listingId)) throw new ArgumentException("A listing id is required.", nameof(listingId));
            if (string.IsNullOrWhiteSpace(sellerId)) throw new ArgumentException("A seller id is required.", nameof(sellerId));
            if (string.IsNullOrWhiteSpace(assetInstanceId)) throw new ArgumentException("An asset instance id is required.", nameof(assetInstanceId));
            if (string.IsNullOrWhiteSpace(templateId)) throw new ArgumentException("A template id is required.", nameof(templateId));
            if (string.IsNullOrWhiteSpace(seasonId)) throw new ArgumentException("A season id is required.", nameof(seasonId));
            if (priceMicroSkr < MarketRules.ToMicroSkr(MarketRules.MinimumListingPriceSkr))
                throw new ArgumentOutOfRangeException(nameof(priceMicroSkr));
            ListingId = listingId;
            SellerId = sellerId;
            SellerDisplayName = sellerDisplayName ?? sellerId;
            AssetKind = assetKind;
            AssetInstanceId = assetInstanceId;
            TemplateId = templateId;
            Quality = quality;
            SeasonId = seasonId;
            PriceMicroSkr = priceMicroSkr;
            CreatedAtUtcTicks = createdAtUtcTicks;
            State = state;
            BuyerId = buyerId;
            ResolvedAtUtcTicks = resolvedAtUtcTicks;
        }
    }

    public sealed class MarketTradeReceipt
    {
        public string ReceiptId { get; }
        public string ListingId { get; }
        public string SellerId { get; }
        public string BuyerId { get; }
        public long SellerProceedsMicroSkr { get; }
        public long SellerFeeMicroSkr { get; }
        public long BurnMicroSkr { get; }
        public long TeamMicroSkr { get; }
        public long BuyerTotalMicroSkr { get; }
        public long CompletedAtUtcTicks { get; }

        public MarketTradeReceipt(string receiptId, string listingId, string sellerId,
            string buyerId, MarketFeeBreakdown payment, long completedAtUtcTicks)
        {
            ReceiptId = receiptId;
            ListingId = listingId;
            SellerId = sellerId;
            BuyerId = buyerId;
            SellerProceedsMicroSkr = payment.SellerProceedsMicroSkr;
            SellerFeeMicroSkr = payment.SellerFeeMicroSkr;
            BurnMicroSkr = payment.BurnMicroSkr;
            TeamMicroSkr = payment.TeamMicroSkr;
            BuyerTotalMicroSkr = payment.BuyerTotalMicroSkr;
            CompletedAtUtcTicks = completedAtUtcTicks;
        }
    }

    public sealed class MarketBoard
    {
        private readonly List<MarketListing> listings = new List<MarketListing>();
        private readonly List<MarketTradeReceipt> receipts = new List<MarketTradeReceipt>();
        private int listingSerial;
        private int receiptSerial;

        public IReadOnlyList<MarketListing> Listings { get { return listings; } }
        public IReadOnlyList<MarketTradeReceipt> Receipts { get { return receipts; } }
        public int ShopLeaseCount { get; private set; }
        public int StateVersion { get; private set; }
        public long BurnPoolMicroSkr { get; private set; }
        public long TeamRevenueMicroSkr { get; private set; }
        public int ListingSerial { get { return listingSerial; } }
        public int ReceiptSerial { get { return receiptSerial; } }
        public int ListingCapacity
        {
            get
            {
                return Math.Min(MarketRules.MaximumListingSlots,
                    MarketRules.InitialListingSlots + ShopLeaseCount * MarketRules.SlotsPerShopLease);
            }
        }

        public int CountActiveListings(string sellerId)
        {
            var count = 0;
            for (var i = 0; i < listings.Count; i++)
                if (listings[i].State == MarketListingState.Active &&
                    string.Equals(listings[i].SellerId, sellerId, StringComparison.Ordinal)) count++;
            return count;
        }

        public bool CanCreateListing(string sellerId, MarketAssetKind assetKind,
            string assetInstanceId, long priceMicroSkr)
        {
            if (string.IsNullOrWhiteSpace(sellerId) || string.IsNullOrWhiteSpace(assetInstanceId) ||
                priceMicroSkr < MarketRules.ToMicroSkr(MarketRules.MinimumListingPriceSkr)) return false;
            if (CountActiveListings(sellerId) >= ListingCapacity) return false;
            for (var i = 0; i < listings.Count; i++)
            {
                var listing = listings[i];
                if (listing.State == MarketListingState.Active && listing.AssetKind == assetKind &&
                    string.Equals(listing.AssetInstanceId, assetInstanceId, StringComparison.Ordinal)) return false;
            }
            return true;
        }

        public bool TryCreateListing(string sellerId, string sellerDisplayName,
            MarketAssetKind assetKind, string assetInstanceId, string templateId, int quality,
            string seasonId, long priceMicroSkr, long utcTicks, out MarketListing listing)
        {
            listing = null;
            if (!CanCreateListing(sellerId, assetKind, assetInstanceId, priceMicroSkr)) return false;
            listing = new MarketListing("market-listing-" + (++listingSerial).ToString("D6"),
                sellerId, sellerDisplayName, assetKind, assetInstanceId, templateId, quality,
                seasonId, priceMicroSkr, utcTicks);
            listings.Add(listing);
            StateVersion++;
            return true;
        }

        public bool TryCancelListing(string sellerId, string listingId, long utcTicks)
        {
            var listing = Find(listingId);
            if (listing == null || listing.State != MarketListingState.Active ||
                !string.Equals(listing.SellerId, sellerId, StringComparison.Ordinal)) return false;
            listing.State = MarketListingState.Cancelled;
            listing.ResolvedAtUtcTicks = utcTicks;
            StateVersion++;
            return true;
        }

        public bool TryPurchase(string buyerId, string listingId, long buyerBalanceMicroSkr,
            long utcTicks, out long updatedBuyerBalanceMicroSkr, out MarketTradeReceipt receipt)
        {
            return TryPurchase(buyerId, listingId, buyerBalanceMicroSkr, utcTicks, null,
                out updatedBuyerBalanceMicroSkr, out receipt);
        }

        public bool TryPurchase(string buyerId, string listingId, long buyerBalanceMicroSkr,
            long utcTicks, Func<MarketTradeReceipt, bool> tryTransferAsset,
            out long updatedBuyerBalanceMicroSkr, out MarketTradeReceipt receipt)
        {
            updatedBuyerBalanceMicroSkr = buyerBalanceMicroSkr;
            receipt = null;
            var listing = Find(listingId);
            if (listing == null || listing.State != MarketListingState.Active ||
                string.IsNullOrWhiteSpace(buyerId) ||
                string.Equals(listing.SellerId, buyerId, StringComparison.Ordinal)) return false;
            var payment = MarketRules.CalculateSettlement(listing.PriceMicroSkr);
            if (buyerBalanceMicroSkr < payment.BuyerTotalMicroSkr) return false;
            var pendingReceipt = new MarketTradeReceipt(
                "market-receipt-" + (receiptSerial + 1).ToString("D6"),
                listing.ListingId, listing.SellerId, buyerId, payment, utcTicks);
            if (tryTransferAsset != null && !tryTransferAsset(pendingReceipt)) return false;

            updatedBuyerBalanceMicroSkr = buyerBalanceMicroSkr - payment.BuyerTotalMicroSkr;
            listing.State = MarketListingState.Sold;
            listing.BuyerId = buyerId;
            listing.ResolvedAtUtcTicks = utcTicks;
            BurnPoolMicroSkr = checked(BurnPoolMicroSkr + payment.BurnMicroSkr);
            TeamRevenueMicroSkr = checked(TeamRevenueMicroSkr + payment.TeamMicroSkr);
            receiptSerial++;
            receipt = pendingReceipt;
            receipts.Add(receipt);
            StateVersion++;
            return true;
        }

        public bool TryLeaseShop(long buyerBalanceMicroSkr, out long updatedBuyerBalanceMicroSkr)
        {
            var price = MarketRules.ToMicroSkr(MarketRules.ShopLeasePriceSkr);
            updatedBuyerBalanceMicroSkr = buyerBalanceMicroSkr;
            if (ShopLeaseCount >= MarketRules.MaximumShopLeasePurchasesPerSeason ||
                buyerBalanceMicroSkr < price) return false;
            updatedBuyerBalanceMicroSkr -= price;
            ShopLeaseCount++;
            StateVersion++;
            return true;
        }

        public MarketListing Find(string listingId)
        {
            for (var i = 0; i < listings.Count; i++)
                if (string.Equals(listings[i].ListingId, listingId, StringComparison.Ordinal)) return listings[i];
            return null;
        }

        public void Restore(IEnumerable<MarketListing> restoredListings,
            IEnumerable<MarketTradeReceipt> restoredReceipts, int shopLeaseCount,
            int stateVersion, long burnPoolMicroSkr, long teamRevenueMicroSkr,
            int restoredListingSerial, int restoredReceiptSerial)
        {
            listings.Clear();
            receipts.Clear();
            if (restoredListings != null) listings.AddRange(restoredListings);
            if (restoredReceipts != null) receipts.AddRange(restoredReceipts);
            ShopLeaseCount = Math.Max(0, Math.Min(
                MarketRules.MaximumShopLeasePurchasesPerSeason, shopLeaseCount));
            StateVersion = Math.Max(0, stateVersion);
            BurnPoolMicroSkr = Math.Max(0L, burnPoolMicroSkr);
            TeamRevenueMicroSkr = Math.Max(0L, teamRevenueMicroSkr);
            listingSerial = Math.Max(0, restoredListingSerial);
            receiptSerial = Math.Max(0, restoredReceiptSerial);
        }
    }

    public sealed class WalletMarketActionRequest
    {
        public string OperationId { get; }
        public MarketActionType ActionType { get; }
        public string WalletId { get; }
        public string SeasonId { get; }
        public string ListingId { get; }
        public string AssetInstanceId { get; }
        public long PriceMicroSkr { get; }
        public long BuyerTotalMicroSkr { get; }
        public int StateVersion { get; }
        public string RulesVersion { get; }
        public string Summary { get; }

        public WalletMarketActionRequest(string operationId, MarketActionType actionType,
            string walletId, string seasonId, string listingId, string assetInstanceId,
            long priceMicroSkr, long buyerTotalMicroSkr, int stateVersion, string rulesVersion,
            string summary)
        {
            OperationId = operationId;
            ActionType = actionType;
            WalletId = walletId;
            SeasonId = seasonId;
            ListingId = listingId;
            AssetInstanceId = assetInstanceId;
            PriceMicroSkr = priceMicroSkr;
            BuyerTotalMicroSkr = buyerTotalMicroSkr;
            StateVersion = stateVersion;
            RulesVersion = rulesVersion;
            Summary = summary;
        }
    }
}
