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
        public void EpicAndLegendaryCharactersUseHeroNamesWhileLowerQualitiesUseClassNames()
        {
            Assert.That(CharacterDisplayNameRules.LocalizationKey(
                "unit.guardian", CharacterQuality.Common), Is.EqualTo("unit.guardian"));
            Assert.That(CharacterDisplayNameRules.LocalizationKey(
                "unit.guardian", CharacterQuality.Uncommon), Is.EqualTo("unit.guardian"));
            Assert.That(CharacterDisplayNameRules.LocalizationKey(
                "unit.guardian", CharacterQuality.Rare), Is.EqualTo("unit.guardian"));
            Assert.That(CharacterDisplayNameRules.LocalizationKey(
                "unit.guardian", CharacterQuality.Epic), Is.EqualTo("unit.guardian.heroName"));
            Assert.That(CharacterDisplayNameRules.LocalizationKey(
                "unit.guardian", CharacterQuality.Legendary), Is.EqualTo("unit.guardian.heroName"));
            Assert.That(CharacterDisplayNameRules.LocalizationKey(
                "enemy.b1.heavy", CharacterQuality.Legendary), Is.EqualTo("enemy.b1.heavy"));
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
        public void GameDeckKeepsThreeAndAllowsUpToSixUniqueCharacters()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);
            var selection = new RunDeckSelection(collection,
                new[] { "unit.guardian" }, maximumSize: 6, minimumSize: 3);

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
                new[] { "unit.guardian", "unit.archer", "unit.healer" }, maximumSize: 6, minimumSize: 0);

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
                new[] { "character.guardian.a", "character.healer.a" }, maximumSize: 6);

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
                new OwnedCharacterState("saved.guardian", "unit.guardian", 1, 2,
                    CharacterQuality.Epic, 100L),
                new OwnedCharacterState("saved.healer", "unit.healer", 1, 1,
                    CharacterQuality.Common, 200L)
            };

            var migrated = PlayerCollection.AddMissingFormalQualityVariants(catalog, source);
            var migratedAgain = PlayerCollection.AddMissingFormalQualityVariants(catalog, migrated);

            Assert.That(migrated.Count, Is.EqualTo(migratedAgain.Count));
            Assert.That(migratedAgain[0].CharacterInstanceId, Is.EqualTo("saved.guardian"));
            Assert.That(migratedAgain[0].AcquiredAtUtcTicks, Is.EqualTo(100L));
            Assert.That(migratedAgain.FindAll(entry => entry.DefinitionId == "unit.guardian").Count,
                Is.EqualTo(3));
            Assert.That(migratedAgain.Find(entry =>
                entry.CharacterInstanceId == "character.unit.guardian.legendary").AcquiredAtUtcTicks,
                Is.GreaterThan(200L));
        }

        [Test]
        public void FavoriteRegistryPersistsOnlyOwnedInstancesAndProtectsFusionMaterials()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = PlayerCollection.CreatePrototype(catalog);
            var favorites = new CharacterFavoriteRegistry();
            var instanceId = collection.Entries[0].CharacterInstanceId;

            Assert.That(favorites.Toggle(instanceId), Is.True);
            Assert.That(favorites.IsFavorite(instanceId), Is.True);
            Assert.That(favorites.CanUseAsFusionMaterial(instanceId), Is.False);

            var restored = new CharacterFavoriteRegistry();
            restored.Restore(new[] { instanceId, "missing.instance" }, collection);
            Assert.That(restored.Count, Is.EqualTo(1));
            Assert.That(restored.IsFavorite(instanceId), Is.True);
            Assert.That(restored.Toggle(instanceId), Is.False);
            Assert.That(restored.CanUseAsFusionMaterial(instanceId), Is.True);
        }

        [Test]
        public void CollectionSortSupportsQualityNameAcquisitionAndFavorites()
        {
            var catalog = CharacterCatalog.CreatePrototype();
            var collection = new PlayerCollection(catalog, new[]
            {
                new OwnedCharacterState("acquired.1", "unit.healer", 1, 0,
                    CharacterQuality.Common, 300L),
                new OwnedCharacterState("acquired.2", "unit.guardian", 1, 0,
                    CharacterQuality.Legendary, 100L),
                new OwnedCharacterState("acquired.3", "unit.archer", 1, 0,
                    CharacterQuality.Epic, 200L)
            });
            var favorites = new CharacterFavoriteRegistry();
            favorites.Toggle("acquired.1");

            var byQuality = DeckCollectionSortRules.Sort(collection.Entries,
                DeckCollectionSortMode.Quality, true, entry => entry.DefinitionId, favorites);
            var byName = DeckCollectionSortRules.Sort(collection.Entries,
                DeckCollectionSortMode.Name, false, entry => entry.DefinitionId, favorites);
            var byAcquired = DeckCollectionSortRules.Sort(collection.Entries,
                DeckCollectionSortMode.AcquiredAt, true, entry => entry.DefinitionId, favorites);
            var byFavorite = DeckCollectionSortRules.Sort(collection.Entries,
                DeckCollectionSortMode.Favorite, true, entry => entry.DefinitionId, favorites);

            Assert.That(byQuality[0].CharacterInstanceId, Is.EqualTo("acquired.2"));
            Assert.That(byName[0].DefinitionId, Is.EqualTo("unit.archer"));
            Assert.That(byAcquired[0].CharacterInstanceId, Is.EqualTo("acquired.1"));
            Assert.That(byFavorite[0].CharacterInstanceId, Is.EqualTo("acquired.1"));
            Assert.That(DeckCollectionSortRules.DefaultDescending(DeckCollectionSortMode.Name), Is.False);
            Assert.That(DeckCollectionSortRules.DefaultDescending(DeckCollectionSortMode.Quality), Is.True);
        }
    }
}
