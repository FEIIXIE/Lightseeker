using System.Collections.Generic;
using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests.EditMode
{
    public sealed class StoreRulesTests
    {
        [Test]
        public void PackPricesAndDrawCountMatchFrozenEconomy()
        {
            Assert.AreEqual(50, StoreCatalog.Get("character.pack.white.v1").PriceSkr);
            Assert.AreEqual(200, StoreCatalog.Get("character.pack.blue.v1").PriceSkr);
            Assert.AreEqual(500, StoreCatalog.Get("character.pack.orange.v1").PriceSkr);
            Assert.AreEqual(3, StoreRules.DrawCountPerCharacterPack);
        }

        [Test]
        public void FormalStoreArtContractKeepsCurrentCatalogAtNativeCanvasSizes()
        {
            Assert.AreEqual("store-page-single-column-continuous-formal-v1.6.0",
                StoreSingleColumnFormalArtContract.PackageVersion);
            Assert.AreEqual(912, StoreSingleColumnFormalArtContract.ProductRowWidth);
            Assert.AreEqual(330, StoreSingleColumnFormalArtContract.ProductRowHeight);
            Assert.AreEqual(280, StoreSingleColumnFormalArtContract.CharacterPackWidth);
            Assert.AreEqual(236, StoreSingleColumnFormalArtContract.CharacterPackHeight);
            Assert.AreEqual(300, StoreSingleColumnFormalArtContract.BasePairWidth);
            Assert.AreEqual(180, StoreSingleColumnFormalArtContract.BasePairHeight);
            Assert.AreEqual(3, StoreSingleColumnFormalArtContract.CharacterPackArt.Length);
            Assert.AreEqual(
                "ART-UI-STORE-SINGLE-COLUMN-BASEPAIR-MIST-HARBOR-300x180-v1.6.0",
                StoreSingleColumnFormalArtContract.BasePairMistHarbor);
            Assert.AreEqual(3286f, StoreSingleColumnFormalArtContract.CurrentCatalogContentHeight);
            Assert.AreEqual(3, StoreSingleColumnFormalArtContract.BasePairArt.Length);
            Assert.AreEqual(3, StoreSingleColumnFormalArtContract.BattlefieldArt.Length);
        }

        [Test]
        public void PurchasedCosmeticsCanBeValidatedAndOnlyNativeBattlefieldsCanRender()
        {
            var owned = new[]
            {
                "base.pack.harbor_pair.v1",
                "battlefield.mist_harbor.v1"
            };
            Assert.IsTrue(StoreCosmeticRules.CanEquip(
                "base.pack.harbor_pair.v1", StoreProductType.BasePack, owned));
            Assert.IsTrue(StoreCosmeticRules.CanEquip(
                "battlefield.mist_harbor.v1", StoreProductType.Battlefield, owned));
            Assert.IsFalse(StoreCosmeticRules.CanEquip(
                "battlefield.mist_harbor.v1", StoreProductType.BasePack, owned));
            Assert.IsNull(StoreCosmeticRules.NormalizeEquipped(
                "battlefield.deeptide_astral.v1", StoreProductType.Battlefield, owned));
            Assert.IsTrue(StoreCosmeticFormalArtContract.HasNativeRuntimeVisual(
                "battlefield.mist_harbor.v1"));
            Assert.IsFalse(StoreCosmeticFormalArtContract.HasNativeRuntimeVisual(
                "base.pack.harbor_pair.v1"));
        }

        [Test]
        public void PackRolesAreDistinctAndDeterministic()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var first = StoreRules.SelectDistinctCharacterIds(catalog, "store-order-1", 3);
            var second = StoreRules.SelectDistinctCharacterIds(catalog, "store-order-1", 3);
            CollectionAssert.AreEqual(first, second);
            Assert.AreEqual(3, first.Length);
            Assert.AreNotEqual(first[0], first[1]);
            Assert.AreNotEqual(first[0], first[2]);
            Assert.AreNotEqual(first[1], first[2]);
        }

        [Test]
        public void EverySampledStoreOrderReturnsThreeDistinctCardsWithoutCycling()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            for (var serial = 1; serial <= 1000; serial++)
            {
                var result = StoreRules.SelectDistinctCharacterIds(
                    catalog, "store-order-" + serial, StoreRules.DrawCountPerCharacterPack);
                Assert.AreEqual(StoreRules.DrawCountPerCharacterPack, result.Length);
                Assert.AreEqual(result.Length, new HashSet<string>(result).Count,
                    "Duplicate draw at serial " + serial);
            }
        }

        [Test]
        public void WalletGatewayIsReplaceableAndLocalAdapterDoesNotReject()
        {
            IWalletSignatureGateway gateway = new LocalSkrWalletSignatureGateway();
            var result = gateway.RequestTransactionSignature(new WalletPurchaseRequest(
                "store-order-1", "character.pack.white.v1", 50, StoreRules.StoreRulesVersion, "test"));
            Assert.IsTrue(result.Approved);
            StringAssert.Contains("store-order-1", result.SignatureId);
        }

        [Test]
        public void WalletGatewayApprovesDeckChangeRequest()
        {
            IWalletSignatureGateway gateway = new LocalSkrWalletSignatureGateway();
            var result = gateway.RequestDeckChangeSignature(new WalletDeckChangeRequest(
                "deck-change-test", "deck-loadout.v1", "characters=3;equipmentChanges=2"));

            Assert.IsTrue(result.Approved);
            StringAssert.Contains("deck-change-test", result.SignatureId);
        }

        [Test]
        public void PityFlagsCanPromoteOnlyTheCurrentDraw()
        {
            Assert.AreEqual(CharacterQuality.Epic,
                StoreRules.RollQuality(CharacterPackTier.White, "pity-epic", true, false));
            Assert.AreEqual(CharacterQuality.Legendary,
                StoreRules.RollQuality(CharacterPackTier.White, "pity-legendary", true, true));
        }

        [Test]
        public void IndependentStoreInstanceIdsProducePersistentQualityRangeStats()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var definition = catalog.Get("unit.guardian");
            const string firstId = "character.store.42.0.unit.guardian";
            const string secondId = "character.store.43.0.unit.guardian";
            var firstRoll = CharacterRarityInstanceRules.ComputeAptitudeRoll(firstId);
            var secondRoll = CharacterRarityInstanceRules.ComputeAptitudeRoll(secondId);
            Assert.AreNotEqual(firstRoll, secondRoll);

            var firstStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, CharacterQuality.Epic, firstRoll, 0);
            var repeatedStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, CharacterQuality.Epic,
                CharacterRarityInstanceRules.ComputeAptitudeRoll(firstId), 0);
            Assert.AreEqual(firstStats.MaxHealth, repeatedStats.MaxHealth);
            Assert.AreEqual(firstStats.AttackDamage, repeatedStats.AttackDamage);
            Assert.GreaterOrEqual(firstRoll, 0);
            Assert.LessOrEqual(firstRoll, CharacterRarityInstanceRules.RollScale);
        }

        [Test]
        public void StoreRevealWaitsForBothAssetPreloadAndMinimumAnimation()
        {
            Assert.IsFalse(StoreRevealPresentationPolicy.CanBeginCardReveal(false, 10f));
            Assert.IsFalse(StoreRevealPresentationPolicy.CanBeginCardReveal(true,
                StoreRevealPresentationPolicy.MinimumLoadingAnimationSeconds - 0.01f));
            Assert.IsTrue(StoreRevealPresentationPolicy.CanBeginCardReveal(true,
                StoreRevealPresentationPolicy.MinimumLoadingAnimationSeconds));
            Assert.Greater(StoreRevealPresentationPolicy.CardRevealDurationSeconds, 0f);
        }
    }
}
