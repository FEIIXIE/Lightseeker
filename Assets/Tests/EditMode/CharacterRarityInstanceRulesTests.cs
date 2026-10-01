using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class CharacterRarityInstanceRulesTests
    {
        [Test]
        public void FrozenFiveQualityRangesAreOrderedAndStable()
        {
            Assert.That(CharacterRarityInstanceRules.GetMinimumMultiplier(CharacterQuality.Common), Is.EqualTo(9700));
            Assert.That(CharacterRarityInstanceRules.GetMaximumMultiplier(CharacterQuality.Common), Is.EqualTo(10300));
            Assert.That(CharacterRarityInstanceRules.GetMinimumMultiplier(CharacterQuality.Uncommon), Is.EqualTo(11834));
            Assert.That(CharacterRarityInstanceRules.GetMaximumMultiplier(CharacterQuality.Uncommon), Is.EqualTo(12566));
            Assert.That(CharacterRarityInstanceRules.GetMinimumMultiplier(CharacterQuality.Rare), Is.EqualTo(14475));
            Assert.That(CharacterRarityInstanceRules.GetMaximumMultiplier(CharacterQuality.Rare), Is.EqualTo(15525));
            Assert.That(CharacterRarityInstanceRules.GetMinimumMultiplier(CharacterQuality.Epic), Is.EqualTo(17760));
            Assert.That(CharacterRarityInstanceRules.GetMaximumMultiplier(CharacterQuality.Epic), Is.EqualTo(19240));
            Assert.That(CharacterRarityInstanceRules.GetMinimumMultiplier(CharacterQuality.Legendary), Is.EqualTo(21965));
            Assert.That(CharacterRarityInstanceRules.GetMaximumMultiplier(CharacterQuality.Legendary), Is.EqualTo(24035));
        }

        [Test]
        public void SameBusinessIdProducesTheSameAptitudeAndStateHash()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var definition = catalog.Get("unit.guardian");
            var first = CharacterRarityInstanceRules.CreateSnapshot(definition, "instance.a", "business.a",
                CharacterQuality.Epic, definition.BasePower, 1);
            var second = CharacterRarityInstanceRules.CreateSnapshot(definition, "instance.a", "business.a",
                CharacterQuality.Epic, definition.BasePower, 1);
            Assert.That(first.AptitudeRoll, Is.EqualTo(second.AptitudeRoll));
            Assert.That(first.StateHash, Is.EqualTo(second.StateHash));
        }

        [Test]
        public void QualityChangesHealthAttackAndHealingButNotArmorOrSpeed()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var definition = catalog.Get("unit.healer");
            var common = CharacterRarityInstanceRules.CalculateCombatStats(definition, definition.BasePower, 1,
                CharacterQuality.Common, 5000);
            var legendary = CharacterRarityInstanceRules.CalculateCombatStats(definition, definition.BasePower, 1,
                CharacterQuality.Legendary, 5000);
            Assert.That(legendary.MaxHealth, Is.GreaterThan(common.MaxHealth));
            Assert.That(legendary.AttackDamage, Is.GreaterThan(common.AttackDamage));
            Assert.That(legendary.HealAmount, Is.GreaterThan(common.HealAmount));
            Assert.That(legendary.Armor, Is.EqualTo(common.Armor));
            Assert.That(legendary.SpeedTier, Is.EqualTo(common.SpeedTier));
        }

        [Test]
        public void StarMultiplierAppliesOnceAfterQuality()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var definition = catalog.Get("unit.guardian");
            var oneStar = CharacterRarityInstanceRules.CalculateCombatStats(definition, definition.BasePower, 1,
                CharacterQuality.Epic, 5000, 0);
            var threeStar = CharacterRarityInstanceRules.CalculateCombatStats(definition, definition.BasePower, 1,
                CharacterQuality.Epic, 5000, 2);
            Assert.That(threeStar.MaxHealth, Is.EqualTo((oneStar.MaxHealth * 12100 + 5000) / 10000));
        }
    }
}
