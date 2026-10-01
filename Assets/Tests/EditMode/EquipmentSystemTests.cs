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
    }
}
