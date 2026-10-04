using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class EquipmentSystemTests
    {
        [Test]
        public void OneInstanceCannotBeEquippedByTwoCharacters()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var helm = inventory.Get("prototype.equipment.lanternHelm");

            Assert.That(inventory.TryEquip(helm.EquipmentInstanceId,
                collection.Get("unit.guardian").CharacterInstanceId), Is.True);
            Assert.That(inventory.TryEquip(helm.EquipmentInstanceId,
                collection.Get("unit.healer").CharacterInstanceId), Is.False);
        }

        [Test]
        public void FrozenSnapshotSurvivesLiveUnequipAndScalesOnceAtRunLevel()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            var body = inventory.Get("prototype.equipment.oldVoyageClothes");
            inventory.TryEquip(helm.EquipmentInstanceId, guardian.CharacterInstanceId);
            inventory.TryEquip(body.EquipmentInstanceId, guardian.CharacterInstanceId);
            var snapshot = inventory.CreateSnapshot(guardian.CharacterInstanceId);

            inventory.TryUnequip(helm.EquipmentInstanceId, guardian.CharacterInstanceId);
            inventory.TryUnequip(body.EquipmentInstanceId, guardian.CharacterInstanceId);
            var derived = EquipmentDerivedStatCalculator.Calculate(
                characters.Get("unit.guardian"), 4, 2, snapshot);

            Assert.That(derived.MaxHealth, Is.EqualTo(39));
            Assert.That(derived.AttackDamage, Is.EqualTo(5));
            Assert.That(derived.Armor, Is.EqualTo(20));
        }

        [Test]
        public void MarketListingForceUnequipsAndRemovesOnlyListedItemFromSnapshot()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            var body = inventory.Get("prototype.equipment.oldVoyageClothes");
            inventory.TryEquip(helm.EquipmentInstanceId, guardian.CharacterInstanceId);
            inventory.TryEquip(body.EquipmentInstanceId, guardian.CharacterInstanceId);
            inventory.TryAddUsageLock(helm.EquipmentInstanceId,
                EquipmentUsageLock.Expedition | EquipmentUsageLock.Defense);
            var staleSnapshot = inventory.CreateSnapshot(guardian.CharacterInstanceId);

            string unequippedCharacterInstanceId;
            Assert.That(inventory.TryBeginMarketListing(
                helm.EquipmentInstanceId, out unequippedCharacterInstanceId), Is.True);
            var resolved = inventory.ResolveOwnedAvailableSnapshot(staleSnapshot);

            Assert.That(unequippedCharacterInstanceId, Is.EqualTo(guardian.CharacterInstanceId));
            Assert.That(helm.EquippedCharacterInstanceId, Is.Null);
            Assert.That(helm.UsageLocks, Is.EqualTo(EquipmentUsageLock.Market));
            Assert.That(resolved.EquipmentInstanceIds, Does.Not.Contain(helm.EquipmentInstanceId));
            Assert.That(resolved.EquipmentInstanceIds, Does.Contain(body.EquipmentInstanceId));
            Assert.That(resolved.Modifier.Armor, Is.EqualTo(0));
            Assert.That(resolved.Modifier.MaxHealth, Is.EqualTo(6));
            Assert.That(inventory.TryEquip(
                helm.EquipmentInstanceId, guardian.CharacterInstanceId), Is.False);
        }

        [Test]
        public void MarketSaleInvalidatesOwnershipAndCancelDoesNotReequip()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            inventory.TryEquip(helm.EquipmentInstanceId, guardian.CharacterInstanceId);
            string ignored;
            inventory.TryBeginMarketListing(helm.EquipmentInstanceId, out ignored);

            Assert.That(inventory.TryCancelMarketListing(helm.EquipmentInstanceId), Is.True);
            Assert.That(helm.EquippedCharacterInstanceId, Is.Null);
            Assert.That(inventory.TryCompleteMarketSale(helm.EquipmentInstanceId), Is.True);
            Assert.That(inventory.Contains(helm.EquipmentInstanceId), Is.False);
        }

        [Test]
        public void AssignmentDraftDoesNotMutateLiveInventoryUntilCommitted()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            var draft = inventory.CreateAssignmentDraft();

            Assert.That(draft.TryEquip(helm, guardian.CharacterInstanceId,
                inventory.Instances), Is.True);
            Assert.That(helm.EquippedCharacterInstanceId, Is.Null);
            Assert.That(draft.CreateSnapshot(guardian.CharacterInstanceId,
                inventory.Instances).Modifier.Armor, Is.EqualTo(2));

            Assert.That(inventory.TryApplyAssignmentDraft(draft,
                new[] { guardian.CharacterInstanceId }), Is.True);
            Assert.That(helm.EquippedCharacterInstanceId,
                Is.EqualTo(guardian.CharacterInstanceId));
        }

        [Test]
        public void RemovingDraftCharacterUnequipsAllOnlyAfterCommit()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var healer = collection.Get("unit.healer");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            var body = inventory.Get("prototype.equipment.oldVoyageClothes");
            inventory.TryEquip(helm.EquipmentInstanceId, guardian.CharacterInstanceId);
            inventory.TryEquip(body.EquipmentInstanceId, guardian.CharacterInstanceId);
            var draft = inventory.CreateAssignmentDraft();

            Assert.That(draft.ClearCharacter(guardian.CharacterInstanceId), Is.EqualTo(2));
            Assert.That(helm.EquippedCharacterInstanceId, Is.EqualTo(guardian.CharacterInstanceId));
            Assert.That(body.EquippedCharacterInstanceId, Is.EqualTo(guardian.CharacterInstanceId));
            Assert.That(inventory.TryApplyAssignmentDraft(draft,
                new[] { healer.CharacterInstanceId }), Is.True);
            Assert.That(helm.EquippedCharacterInstanceId, Is.Null);
            Assert.That(body.EquippedCharacterInstanceId, Is.Null);
        }

        [Test]
        public void AssignmentDraftRejectsEquipmentForCharacterOutsideSavedDeck()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var healer = collection.Get("unit.healer");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            var draft = inventory.CreateAssignmentDraft();
            Assert.That(draft.TryEquip(helm, guardian.CharacterInstanceId,
                inventory.Instances), Is.True);

            Assert.That(inventory.TryApplyAssignmentDraft(draft,
                new[] { healer.CharacterInstanceId }), Is.False);
            Assert.That(helm.EquippedCharacterInstanceId, Is.Null);
        }

        [Test]
        public void AssignmentDraftCanChangeNextRunLoadoutWhileCurrentRunSnapshotIsLocked()
        {
            var characters = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(characters);
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var guardian = collection.Get("unit.guardian");
            var helm = inventory.Get("prototype.equipment.lanternHelm");
            inventory.TryEquip(helm.EquipmentInstanceId, guardian.CharacterInstanceId);
            inventory.TryAddUsageLock(helm.EquipmentInstanceId, EquipmentUsageLock.Expedition);
            var frozen = inventory.CreateSnapshot(guardian.CharacterInstanceId);
            var draft = inventory.CreateAssignmentDraft();

            Assert.That(draft.TryUnequip(helm, guardian.CharacterInstanceId), Is.True);
            Assert.That(frozen.Modifier.Armor, Is.EqualTo(2));
            Assert.That(helm.EquippedCharacterInstanceId, Is.EqualTo(guardian.CharacterInstanceId));
            Assert.That(inventory.TryApplyAssignmentDraft(draft,
                new[] { guardian.CharacterInstanceId }), Is.True);
            Assert.That(helm.EquippedCharacterInstanceId, Is.Null);
            Assert.That(frozen.Modifier.Armor, Is.EqualTo(2));
        }

        [TestCase(1, 11500)]
        [TestCase(-1, 8200)]
        public void LegacySpeedTierShiftMigratesToContinuousPercent(int shift, int expected)
        {
            var stats = new CombatStatBlock(100, 10, 0,
                speedTier: CombatSpeedTier.Standard);
            var snapshot = new EquipmentLoadoutSnapshot(null,
                new EquipmentStatModifier(speedTierShift: shift));

            Assert.That(EquipmentDerivedStatCalculator.Calculate(stats, snapshot)
                .AttackSpeedBasisPoints, Is.EqualTo(expected));
        }

        [Test]
        public void MarketPurchaseCanBeInsertedAtTopWithoutCreatingADuplicate()
        {
            var catalog = EquipmentCatalog.CreatePrototype();
            var inventory = EquipmentInventory.CreatePrototype(catalog);
            var purchased = new EquipmentInstanceState(
                "market.buy.market-receipt-000001", "equipment.lanternHarborBlade",
                EquipmentQuality.Common, "season-0");

            Assert.That(inventory.TryAddFirst(purchased), Is.True);
            Assert.That(inventory.Instances[0], Is.SameAs(purchased));
            Assert.That(inventory.Contains(purchased.EquipmentInstanceId), Is.True);
            Assert.That(inventory.TryAddFirst(purchased), Is.False);
        }
    }
}
