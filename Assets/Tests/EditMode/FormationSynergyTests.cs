using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class FormationSynergyTests
    {
        [Test]
        public void TwoDifferentCharactersActivateFactionAndProfession()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var synergy = FormationSynergy.Calculate(catalog,
                new[] { "unit.guardian", "unit.healer", "unit.starblade" });

            Assert.That(synergy.IsFactionActive("faction.steel"), Is.True);
            Assert.That(synergy.IsProfessionActive("profession.guardian"), Is.True);
            Assert.That(synergy.GetMaximumHealthPercent(catalog.Get("unit.guardian")), Is.EqualTo(12));
            Assert.That(synergy.GetOpeningShieldPercent(catalog.Get("unit.guardian")), Is.EqualTo(12));
            Assert.That(synergy.GetOpeningShieldPercent(catalog.Get("unit.healer")), Is.EqualTo(12));
            Assert.That(synergy.GetHealPercent(catalog.Get("unit.healer")), Is.Zero);
        }

        [Test]
        public void DuplicateCharacterDoesNotCountTwice()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var synergy = FormationSynergy.Calculate(catalog,
                new[] { "unit.guardian", "unit.guardian" });

            Assert.That(synergy.IsFactionActive("faction.steel"), Is.False);
            Assert.That(synergy.IsProfessionActive("profession.guardian"), Is.False);
        }

        [Test]
        public void LaunchRosterProvidesTwoCharactersForEveryFactionAndProfession()
        {
            var catalog = CharacterCatalog.CreatePrototype();

            Assert.That(FormationSynergy.Calculate(catalog,
                new[] { "unit.guardian", "unit.healer" }).IsFactionActive("faction.steel"), Is.True);
            Assert.That(FormationSynergy.Calculate(catalog,
                new[] { "unit.thornGuard", "unit.archer" }).IsFactionActive("faction.wild"), Is.True);
            Assert.That(FormationSynergy.Calculate(catalog,
                new[] { "unit.starblade", "unit.starpriest" }).IsFactionActive("faction.starlight"), Is.True);

            Assert.That(FormationSynergy.Calculate(catalog,
                new[] { "unit.guardian", "unit.starblade" }).IsProfessionActive("profession.guardian"), Is.True);
            Assert.That(FormationSynergy.Calculate(catalog,
                new[] { "unit.thornGuard", "unit.starpriest" }).IsProfessionActive("profession.output"), Is.True);
            Assert.That(FormationSynergy.Calculate(catalog,
                new[] { "unit.healer", "unit.archer" }).IsProfessionActive("profession.support"), Is.True);
        }
    }
}
