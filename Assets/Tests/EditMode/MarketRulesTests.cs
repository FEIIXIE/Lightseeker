using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class MarketRulesTests
    {
        [Test]
        public void MinimumListingUsesFiveInitialSlots()
        {
            Assert.That(MarketRules.MinimumListingPriceSkr, Is.EqualTo(10));
            Assert.That(MarketRules.InitialListingSlots, Is.EqualTo(5));
        }

        [Test]
        public void SellerPaysExactFivePercentFeeSplitEvenAtMinimumPrice()
        {
            var payment = MarketRules.CalculateSettlement(MarketRules.ToMicroSkr(10));
            Assert.That(payment.SellerProceedsMicroSkr, Is.EqualTo(9500000L));
            Assert.That(payment.SellerFeeMicroSkr, Is.EqualTo(500000L));
            Assert.That(payment.BurnMicroSkr, Is.EqualTo(250000L));
            Assert.That(payment.TeamMicroSkr, Is.EqualTo(250000L));
            Assert.That(payment.BuyerTotalMicroSkr, Is.EqualTo(10000000L));
        }

        [Test]
        public void ShopLeaseChargesSkrAndAddsFiveSlots()
        {
            var board = new MarketBoard();
            long balance;
            Assert.That(board.TryLeaseShop(MarketRules.ToMicroSkr(150), out balance), Is.True);
            Assert.That(balance, Is.EqualTo(MarketRules.ToMicroSkr(50)));
            Assert.That(board.ListingCapacity, Is.EqualTo(10));
        }

        [Test]
        public void SoldListingCannotBePurchasedTwice()
        {
            var board = new MarketBoard();
            MarketListing listing;
            Assert.That(board.TryCreateListing("seller", "seller", MarketAssetKind.Equipment,
                "equipment-instance", "equipment.lanternHarborBlade", 0, "season-0",
                MarketRules.ToMicroSkr(20), 1, out listing), Is.True);
            long balance;
            MarketTradeReceipt receipt;
            Assert.That(board.TryPurchase("buyer", listing.ListingId, MarketRules.ToMicroSkr(100),
                2, out balance, out receipt), Is.True);
            Assert.That(board.TryPurchase("buyer", listing.ListingId, MarketRules.ToMicroSkr(100),
                3, out balance, out receipt), Is.False);
        }

        [Test]
        public void BuyerWithExactListedPriceCanPurchaseWithoutExtraFee()
        {
            var board = new MarketBoard();
            MarketListing listing;
            Assert.That(board.TryCreateListing("seller", "seller", MarketAssetKind.Equipment,
                "exact-price-equipment", "equipment.lanternHarborBlade", 0, "season-0",
                MarketRules.ToMicroSkr(10), 1, out listing), Is.True);
            long balance;
            MarketTradeReceipt receipt;
            Assert.That(board.TryPurchase("buyer", listing.ListingId, MarketRules.ToMicroSkr(10),
                2, out balance, out receipt), Is.True);
            Assert.That(balance, Is.Zero);
            Assert.That(receipt.BuyerTotalMicroSkr, Is.EqualTo(MarketRules.ToMicroSkr(10)));
            Assert.That(receipt.SellerProceedsMicroSkr, Is.EqualTo(9500000L));
        }

        [Test]
        public void FailedInventoryTransferDoesNotConsumeListingOrBalance()
        {
            var board = new MarketBoard();
            MarketListing listing;
            Assert.That(board.TryCreateListing("seller", "seller", MarketAssetKind.Equipment,
                "transfer-test-equipment", "equipment.lanternHarborBlade", 0, "season-0",
                MarketRules.ToMicroSkr(10), 1, out listing), Is.True);
            long balance;
            MarketTradeReceipt receipt;
            var transferAttempted = false;

            Assert.That(board.TryPurchase("buyer", listing.ListingId, MarketRules.ToMicroSkr(20),
                2, pendingReceipt =>
                {
                    transferAttempted = true;
                    return false;
                }, out balance, out receipt), Is.False);

            Assert.That(transferAttempted, Is.True);
            Assert.That(balance, Is.EqualTo(MarketRules.ToMicroSkr(20)));
            Assert.That(receipt, Is.Null);
            Assert.That(listing.State, Is.EqualTo(MarketListingState.Active));
            Assert.That(board.Receipts, Is.Empty);
            Assert.That(board.BurnPoolMicroSkr, Is.Zero);
            Assert.That(board.TeamRevenueMicroSkr, Is.Zero);
        }
    }
}
