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
        public void WalletGatewayIsReplaceableAndLocalAdapterDoesNotReject()
        {
            IWalletSignatureGateway gateway = new LocalSkrWalletSignatureGateway();
            var result = gateway.RequestTransactionSignature(new WalletPurchaseRequest(
                "store-order-1", "character.pack.white.v1", 50, StoreRules.StoreRulesVersion, "test"));
            Assert.IsTrue(result.Approved);
            StringAssert.Contains("store-order-1", result.SignatureId);
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
    }
}
