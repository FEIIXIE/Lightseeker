using NUnit.Framework;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Tests
{
    public sealed class CharacterCollectionTests
    {
        [Test]
        public void NextDeckOnlyAcceptsOwnedUniqueCharactersAndKeepsOneEntry()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);
            var selection = new RunDeckSelection(collection, new[] { "unit.guardian" }, 3);

            Assert.That(selection.TryAdd("unit.archer"), Is.True);
            Assert.That(selection.TryAdd("unit.archer"), Is.False);
            Assert.That(selection.TryAdd("unit.unknown"), Is.False);
            Assert.That(selection.TryRemove("unit.guardian"), Is.True);
            Assert.That(selection.TryRemove("unit.archer"), Is.False);
            Assert.That(selection.Count, Is.EqualTo(1));
        }

        [Test]
        public void EditingNextDeckDoesNotMutateLockedRunSnapshot()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);
            var selection = new RunDeckSelection(collection,
                new[] { "unit.guardian", "unit.archer", "unit.healer" }, 3);
            var activeRun = selection.CreateSnapshot();

            Assert.That(selection.TryRemove("unit.guardian"), Is.True);
            Assert.That(activeRun.Contains("unit.guardian"), Is.True);
            Assert.That(selection.Contains("unit.guardian"), Is.False);
        }

        [Test]
        public void ExternalProgressIsSeparateFromTemporaryRunLevel()
        {
            var owned = new OwnedCharacterState("unit.guardian", 7, 2, CharacterQuality.Rare);
            var runUnit = new RunUnitState(owned.DefinitionId, 4, runLevel: 3);

            Assert.That(owned.ExternalStar, Is.EqualTo(2));
            Assert.That(owned.Quality, Is.EqualTo(CharacterQuality.Rare));
            Assert.That(runUnit.RunLevel, Is.EqualTo(3));
        }

        [Test]
        public void UncommonQualityAddsTheFifthVisualStateWithoutChangingExistingSaveValues()
        {
            Assert.That((int)CharacterQuality.Common, Is.EqualTo(0));
            Assert.That((int)CharacterQuality.Rare, Is.EqualTo(1));
            Assert.That((int)CharacterQuality.Epic, Is.EqualTo(2));
            Assert.That((int)CharacterQuality.Legendary, Is.EqualTo(3));
            Assert.That((int)CharacterQuality.Uncommon, Is.EqualTo(4));

            var owned = new OwnedCharacterState("unit.guardian", 1, 0, CharacterQuality.Uncommon);
            Assert.That(owned.Quality, Is.EqualTo(CharacterQuality.Uncommon));
        }

        [Test]
        public void EliteAndBossDetailRolesDoNotReplaceTheirRuntimeProfessionIds()
        {
            var catalog = CharacterCatalog.CreatePrototype();

            Assert.That(catalog.Get("enemy.e1.fortress").ProfessionId, Is.EqualTo("profession.elite"));
            Assert.That(catalog.Get("enemy.e1.fortress").DetailRoleId, Is.EqualTo("profession.guardian"));
            Assert.That(catalog.Get("enemy.e2.hunter").DetailRoleId, Is.EqualTo("profession.output"));
            Assert.That(catalog.Get("enemy.e3.caster").DetailRoleId, Is.EqualTo("profession.caster"));
            Assert.That(catalog.Get("enemy.b1.heavy").ProfessionId, Is.EqualTo("profession.boss"));
            Assert.That(catalog.Get("enemy.b1.heavy").DetailRoleId, Is.EqualTo("profession.guardian"));
            Assert.That(catalog.Get("enemy.b2.hunter").DetailRoleId, Is.EqualTo("profession.output"));
        }

        [Test]
        public void GameDeckKeepsThreeAndAllowsUpToEightUniqueCharacters()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);
            var selection = new RunDeckSelection(collection,
                new[] { "unit.guardian" }, maximumSize: 8, minimumSize: 3);

            Assert.That(selection.Count, Is.EqualTo(3));
            Assert.That(selection.MinimumSize, Is.EqualTo(3));
            Assert.That(selection.TryRemove(selection.SelectedIds[0]), Is.False);
            Assert.That(selection.TryAdd("unit.starblade"), Is.True);
        }

        [Test]
        public void DraftDeckCanTemporarilyDropBelowThreeBeforeSaveValidation()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);
            var draft = new RunDeckSelection(collection,
                new[] { "unit.guardian", "unit.archer", "unit.healer" }, maximumSize: 8, minimumSize: 0);

            Assert.That(draft.TryRemove("unit.guardian"), Is.True);
            Assert.That(draft.TryRemove("unit.archer"), Is.True);
            Assert.That(draft.TryRemove("unit.healer"), Is.True);
            Assert.That(draft.Count, Is.EqualTo(0));
        }

        [Test]
        public void CollectionAllowsDuplicateCharactersButDeckSelectsOneInstancePerCharacter()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = new PlayerCollection(catalog, new[]
            {
                new OwnedCharacterState("character.guardian.a", "unit.guardian", 1, 0, CharacterQuality.Common),
                new OwnedCharacterState("character.guardian.b", "unit.guardian", 1, 1, CharacterQuality.Rare),
                new OwnedCharacterState("character.healer.a", "unit.healer", 1, 0, CharacterQuality.Common)
            });
            var selection = new RunDeckSelection(collection,
                new[] { "character.guardian.a", "character.healer.a" }, maximumSize: 8);

            Assert.That(collection.GetAll("unit.guardian").Count, Is.EqualTo(2));
            Assert.That(selection.ContainsInstance("character.guardian.a"), Is.True);
            Assert.That(selection.TryAddInstance("character.guardian.b"), Is.False);
            Assert.That(selection.TrySelectInstance("character.guardian.b"), Is.True);
            Assert.That(selection.ContainsInstance("character.guardian.a"), Is.False);
            Assert.That(selection.ContainsInstance("character.guardian.b"), Is.True);
            Assert.That(selection.CreateSnapshot().GetCharacterInstanceId("unit.guardian"),
                Is.EqualTo("character.guardian.b"));
        }

        [Test]
        public void PrototypeCollectionIncludesStableEpicAndLegendaryInstances()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);

            Assert.That(collection.GetByInstanceId("character.unit.guardian.epic").Quality,
                Is.EqualTo(CharacterQuality.Epic));
            Assert.That(collection.GetByInstanceId("character.unit.guardian.legendary").Quality,
                Is.EqualTo(CharacterQuality.Legendary));
            Assert.That(collection.GetAll("unit.guardian").Count, Is.EqualTo(3));
        }

        [Test]
        public void FormalQualityMigrationIsIdempotentAndPreservesExistingCards()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var source = new[]
            {
                new OwnedCharacterState("saved.guardian", "unit.guardian", 1, 2, CharacterQuality.Epic),
                new OwnedCharacterState("saved.healer", "unit.healer", 1, 1, CharacterQuality.Common)
            };

            var migrated = PlayerCollection.AddMissingFormalQualityVariants(catalog, source);
            var migratedAgain = PlayerCollection.AddMissingFormalQualityVariants(catalog, migrated);

            Assert.That(migrated.Count, Is.EqualTo(migratedAgain.Count));
            Assert.That(migratedAgain[0].CharacterInstanceId, Is.EqualTo("saved.guardian"));
            Assert.That(migratedAgain.FindAll(entry => entry.DefinitionId == "unit.guardian").Count,
                Is.EqualTo(3));
        }
    }
}
