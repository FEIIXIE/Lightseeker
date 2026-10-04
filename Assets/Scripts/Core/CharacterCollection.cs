using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum CharacterQuality
    {
        Common = 0,
        Rare = 1,
        Epic = 2,
        Legendary = 3,
        // Preserve the numeric values already stored in existing saves.
        Uncommon = 4
    }

    public enum DeckCollectionSortMode
    {
        Quality = 0,
        Name = 1,
        AcquiredAt = 2,
        Favorite = 3
    }

    public static class CharacterDisplayNameRules
    {
        public static bool UsesHeroName(CharacterQuality quality)
        {
            return quality == CharacterQuality.Epic || quality == CharacterQuality.Legendary;
        }

        public static string LocalizationKey(string definitionId, CharacterQuality quality)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("A character definition id is required.", nameof(definitionId));
            return UsesHeroName(quality) && definitionId.StartsWith("unit.", StringComparison.Ordinal)
                ? definitionId + ".heroName"
                : definitionId;
        }
    }

    /// <summary>
    /// Persistent per-instance favorite state. Fusion and other destructive consumers must query
    /// CanUseAsFusionMaterial instead of duplicating protection rules in their UI.
    /// </summary>
    public sealed class CharacterFavoriteRegistry
    {
        private readonly HashSet<string> favoriteInstanceIds =
            new HashSet<string>(StringComparer.Ordinal);

        public int Count => favoriteInstanceIds.Count;

        public bool IsFavorite(string characterInstanceId)
        {
            return !string.IsNullOrEmpty(characterInstanceId) &&
                   favoriteInstanceIds.Contains(characterInstanceId);
        }

        public bool Toggle(string characterInstanceId)
        {
            if (string.IsNullOrWhiteSpace(characterInstanceId)) return false;
            if (favoriteInstanceIds.Remove(characterInstanceId)) return false;
            favoriteInstanceIds.Add(characterInstanceId);
            return true;
        }

        public bool CanUseAsFusionMaterial(string characterInstanceId)
        {
            return !IsFavorite(characterInstanceId);
        }

        public void Restore(IEnumerable<string> characterInstanceIds, PlayerCollection collection)
        {
            favoriteInstanceIds.Clear();
            if (characterInstanceIds == null || collection == null) return;
            foreach (var instanceId in characterInstanceIds)
            {
                OwnedCharacterState ignored;
                if (!string.IsNullOrEmpty(instanceId) &&
                    collection.TryGetByInstanceId(instanceId, out ignored))
                    favoriteInstanceIds.Add(instanceId);
            }
        }

        public string[] CopyInstanceIds()
        {
            var result = new string[favoriteInstanceIds.Count];
            favoriteInstanceIds.CopyTo(result);
            Array.Sort(result, StringComparer.Ordinal);
            return result;
        }
    }

    public static class DeckCollectionSortRules
    {
        public static List<OwnedCharacterState> Sort(
            IReadOnlyList<OwnedCharacterState> source,
            DeckCollectionSortMode mode,
            bool descending,
            Func<OwnedCharacterState, string> displayName,
            CharacterFavoriteRegistry favorites)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (displayName == null) throw new ArgumentNullException(nameof(displayName));
            if (favorites == null) throw new ArgumentNullException(nameof(favorites));

            var result = new List<OwnedCharacterState>(source.Count);
            var acquisitionOrder = new Dictionary<string, int>(StringComparer.Ordinal);
            for (var index = 0; index < source.Count; index++)
            {
                var entry = source[index];
                if (entry == null) continue;
                result.Add(entry);
                acquisitionOrder[entry.CharacterInstanceId] = index;
            }

            result.Sort((left, right) =>
            {
                var comparison = ComparePrimary(left, right, mode, displayName, favorites,
                    acquisitionOrder);
                if (comparison == 0)
                    comparison = acquisitionOrder[left.CharacterInstanceId]
                        .CompareTo(acquisitionOrder[right.CharacterInstanceId]);
                return descending ? -comparison : comparison;
            });
            return result;
        }

        public static bool DefaultDescending(DeckCollectionSortMode mode)
        {
            return mode != DeckCollectionSortMode.Name;
        }

        private static int ComparePrimary(
            OwnedCharacterState left,
            OwnedCharacterState right,
            DeckCollectionSortMode mode,
            Func<OwnedCharacterState, string> displayName,
            CharacterFavoriteRegistry favorites,
            IReadOnlyDictionary<string, int> acquisitionOrder)
        {
            switch (mode)
            {
                case DeckCollectionSortMode.Quality:
                    return QualityRank(left.Quality).CompareTo(QualityRank(right.Quality));
                case DeckCollectionSortMode.Name:
                    return StringComparer.OrdinalIgnoreCase.Compare(displayName(left), displayName(right));
                case DeckCollectionSortMode.AcquiredAt:
                    return left.AcquiredAtUtcTicks.CompareTo(right.AcquiredAtUtcTicks);
                case DeckCollectionSortMode.Favorite:
                    return (favorites.IsFavorite(left.CharacterInstanceId) ? 1 : 0)
                        .CompareTo(favorites.IsFavorite(right.CharacterInstanceId) ? 1 : 0);
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private static int QualityRank(CharacterQuality quality)
        {
            switch (quality)
            {
                case CharacterQuality.Common: return 0;
                case CharacterQuality.Uncommon: return 1;
                case CharacterQuality.Rare: return 2;
                case CharacterQuality.Epic: return 3;
                case CharacterQuality.Legendary: return 4;
                default: return -1;
            }
        }
    }

    /// <summary>
    /// Persistent, out-of-run ownership. External star and quality never represent
    /// the temporary run level used by three-to-one merging.
    /// </summary>
    public sealed class OwnedCharacterState
    {
        public const long LegacyAcquisitionEpochUtcTicks = 637134336000000000L;
        public string CharacterInstanceId { get; }
        public string DefinitionId { get; }
        public int CopyCount { get; }
        public int ExternalStar { get; }
        public CharacterQuality Quality { get; }
        public long AcquiredAtUtcTicks { get; }

        public OwnedCharacterState(string definitionId, int copyCount, int externalStar, CharacterQuality quality)
            : this("character." + definitionId, definitionId, copyCount, externalStar, quality, 0L)
        {
        }

        public OwnedCharacterState(
            string characterInstanceId,
            string definitionId,
            int copyCount,
            int externalStar,
            CharacterQuality quality,
            long acquiredAtUtcTicks = 0L)
        {
            if (string.IsNullOrWhiteSpace(characterInstanceId)) throw new ArgumentException("A character instance id is required.", nameof(characterInstanceId));
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("A definition id is required.", nameof(definitionId));
            if (copyCount < 1) throw new ArgumentOutOfRangeException(nameof(copyCount));
            if (externalStar < 0) throw new ArgumentOutOfRangeException(nameof(externalStar));
            if (!Enum.IsDefined(typeof(CharacterQuality), quality)) throw new ArgumentOutOfRangeException(nameof(quality));
            if (acquiredAtUtcTicks < 0L) throw new ArgumentOutOfRangeException(nameof(acquiredAtUtcTicks));
            CharacterInstanceId = characterInstanceId;
            DefinitionId = definitionId;
            CopyCount = copyCount;
            ExternalStar = externalStar;
            Quality = quality;
            AcquiredAtUtcTicks = acquiredAtUtcTicks;
        }
    }

    public sealed class PlayerCollection
    {
        private readonly OwnedCharacterState[] entries;
        private readonly Dictionary<string, OwnedCharacterState> byId;
        private readonly Dictionary<string, OwnedCharacterState> byInstanceId;
        public IReadOnlyList<OwnedCharacterState> Entries => entries;

        public PlayerCollection(CharacterCatalog catalog, IEnumerable<OwnedCharacterState> entries)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (entries == null) throw new ArgumentNullException(nameof(entries));
            var copied = new List<OwnedCharacterState>();
            byId = new Dictionary<string, OwnedCharacterState>();
            byInstanceId = new Dictionary<string, OwnedCharacterState>();
            foreach (var entry in entries)
            {
                if (entry == null || !catalog.Contains(entry.DefinitionId) ||
                    byInstanceId.ContainsKey(entry.CharacterInstanceId)) continue;
                copied.Add(entry);
                if (!byId.ContainsKey(entry.DefinitionId)) byId.Add(entry.DefinitionId, entry);
                byInstanceId.Add(entry.CharacterInstanceId, entry);
            }
            if (copied.Count == 0) throw new ArgumentException("A collection needs at least one valid owned character.", nameof(entries));
            this.entries = copied.ToArray();
        }

        public bool Owns(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId) && byId.ContainsKey(definitionId);
        }

        public OwnedCharacterState Get(string definitionId)
        {
            OwnedCharacterState entry;
            if (!byId.TryGetValue(definitionId, out entry)) throw new KeyNotFoundException("Character is not owned: " + definitionId);
            return entry;
        }

        public OwnedCharacterState GetByInstanceId(string characterInstanceId)
        {
            OwnedCharacterState entry;
            if (!byInstanceId.TryGetValue(characterInstanceId, out entry))
                throw new KeyNotFoundException("Character instance is not owned: " + characterInstanceId);
            return entry;
        }

        public bool TryGetByInstanceId(string characterInstanceId, out OwnedCharacterState entry)
        {
            if (string.IsNullOrEmpty(characterInstanceId))
            {
                entry = null;
                return false;
            }
            return byInstanceId.TryGetValue(characterInstanceId, out entry);
        }

        public IReadOnlyList<OwnedCharacterState> GetAll(string definitionId)
        {
            var matches = new List<OwnedCharacterState>();
            foreach (var entry in entries)
            {
                if (entry.DefinitionId == definitionId) matches.Add(entry);
            }
            return matches;
        }

        public static PlayerCollection CreatePrototype(CharacterCatalog catalog)
        {
            var entries = new List<OwnedCharacterState>();
            var acquiredAtUtcTicks = OwnedCharacterState.LegacyAcquisitionEpochUtcTicks;
            foreach (var definition in catalog.Definitions)
            {
                if (!definition.IsCollectible) continue;
                entries.Add(new OwnedCharacterState("character." + definition.DefinitionId,
                    definition.DefinitionId, 1, 0, CharacterQuality.Common, acquiredAtUtcTicks++));
            }
            return new PlayerCollection(catalog, AddMissingFormalQualityVariants(catalog, entries));
        }

        public static List<OwnedCharacterState> AddMissingFormalQualityVariants(
            CharacterCatalog catalog, IEnumerable<OwnedCharacterState> source)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new List<OwnedCharacterState>();
            var qualityKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in source)
            {
                if (entry == null) continue;
                result.Add(entry);
                qualityKeys.Add(FormalQualityKey(entry.DefinitionId, entry.Quality));
            }

            var nextAcquiredAtUtcTicks = NextAcquiredAtUtcTicks(result,
                OwnedCharacterState.LegacyAcquisitionEpochUtcTicks);

            foreach (var definition in catalog.Definitions)
            {
                if (!definition.IsCollectible) continue;
                AddFormalQualityVariant(result, qualityKeys, definition.DefinitionId,
                    CharacterQuality.Common, ref nextAcquiredAtUtcTicks);
                AddFormalQualityVariant(result, qualityKeys, definition.DefinitionId,
                    CharacterQuality.Epic, ref nextAcquiredAtUtcTicks);
                AddFormalQualityVariant(result, qualityKeys, definition.DefinitionId,
                    CharacterQuality.Legendary, ref nextAcquiredAtUtcTicks);
            }
            return result;
        }

        public static long NextAcquiredAtUtcTicks(
            IEnumerable<OwnedCharacterState> entries, long candidateUtcTicks)
        {
            var next = Math.Max(1L, candidateUtcTicks);
            if (entries == null) return next;
            foreach (var entry in entries)
            {
                if (entry != null && entry.AcquiredAtUtcTicks >= next)
                    next = entry.AcquiredAtUtcTicks + 1L;
            }
            return next;
        }

        public static string FormalQualityInstanceId(string definitionId, CharacterQuality quality)
        {
            return "character." + definitionId + "." + quality.ToString().ToLowerInvariant();
        }

        private static string FormalQualityKey(string definitionId, CharacterQuality quality)
        {
            return definitionId + "|" + ((int)quality).ToString();
        }

        private static void AddFormalQualityVariant(
            List<OwnedCharacterState> entries,
            HashSet<string> qualityKeys,
            string definitionId,
            CharacterQuality quality,
            ref long acquiredAtUtcTicks)
        {
            if (!qualityKeys.Add(FormalQualityKey(definitionId, quality))) return;
            entries.Add(new OwnedCharacterState(
                FormalQualityInstanceId(definitionId, quality), definitionId, 1, 0, quality,
                acquiredAtUtcTicks++));
        }
    }

    /// <summary>Editable deck for the next run. Active runs consume an immutable RunDeckSnapshot.</summary>
    public sealed class RunDeckSelection
    {
        private readonly PlayerCollection collection;
        private readonly List<string> selectedInstanceIds = new List<string>();
        public int MaximumSize { get; }
        public int MinimumSize { get; }
        public int Count => selectedInstanceIds.Count;
        public IReadOnlyList<string> SelectedInstanceIds => selectedInstanceIds;
        public IReadOnlyList<string> SelectedIds
        {
            get
            {
                var definitionIds = new string[selectedInstanceIds.Count];
                for (var i = 0; i < selectedInstanceIds.Count; i++)
                    definitionIds[i] = collection.GetByInstanceId(selectedInstanceIds[i]).DefinitionId;
                return definitionIds;
            }
        }

        public RunDeckSelection(
            PlayerCollection collection,
            IEnumerable<string> initialIds,
            int maximumSize,
            int minimumSize = 1)
        {
            if (collection == null) throw new ArgumentNullException(nameof(collection));
            if (initialIds == null) throw new ArgumentNullException(nameof(initialIds));
            if (maximumSize <= 0) throw new ArgumentOutOfRangeException(nameof(maximumSize));
            if (minimumSize < 0 || minimumSize > maximumSize) throw new ArgumentOutOfRangeException(nameof(minimumSize));
            this.collection = collection;
            MaximumSize = maximumSize;
            MinimumSize = minimumSize;
            foreach (var id in initialIds)
            {
                if (selectedInstanceIds.Count >= MaximumSize) break;
                OwnedCharacterState entry;
                if (!collection.TryGetByInstanceId(id, out entry) && collection.Owns(id)) entry = collection.Get(id);
                if (entry != null && !Contains(entry.DefinitionId)) selectedInstanceIds.Add(entry.CharacterInstanceId);
            }
            foreach (var entry in collection.Entries)
            {
                if (selectedInstanceIds.Count >= MinimumSize || selectedInstanceIds.Count >= MaximumSize) break;
                if (!Contains(entry.DefinitionId)) selectedInstanceIds.Add(entry.CharacterInstanceId);
            }
        }

        public bool Contains(string definitionId)
        {
            foreach (var instanceId in selectedInstanceIds)
            {
                if (collection.GetByInstanceId(instanceId).DefinitionId == definitionId) return true;
            }
            return false;
        }

        public bool ContainsInstance(string characterInstanceId)
        {
            return selectedInstanceIds.Contains(characterInstanceId);
        }

        public bool TryAdd(string definitionId)
        {
            if (!collection.Owns(definitionId)) return false;
            return TryAddInstance(collection.Get(definitionId).CharacterInstanceId);
        }

        public bool TryAddInstance(string characterInstanceId)
        {
            OwnedCharacterState entry;
            if (!collection.TryGetByInstanceId(characterInstanceId, out entry) ||
                Contains(entry.DefinitionId) || selectedInstanceIds.Count >= MaximumSize) return false;
            selectedInstanceIds.Add(entry.CharacterInstanceId);
            return true;
        }

        public bool TryRemove(string definitionId)
        {
            if (selectedInstanceIds.Count <= MinimumSize) return false;
            for (var i = 0; i < selectedInstanceIds.Count; i++)
            {
                if (collection.GetByInstanceId(selectedInstanceIds[i]).DefinitionId != definitionId) continue;
                selectedInstanceIds.RemoveAt(i);
                return true;
            }
            return false;
        }

        public bool TryRemoveInstance(string characterInstanceId)
        {
            if (selectedInstanceIds.Count <= MinimumSize) return false;
            return selectedInstanceIds.Remove(characterInstanceId);
        }

        public bool TrySelectInstance(string characterInstanceId)
        {
            OwnedCharacterState entry;
            if (!collection.TryGetByInstanceId(characterInstanceId, out entry)) return false;
            for (var i = 0; i < selectedInstanceIds.Count; i++)
            {
                var selected = collection.GetByInstanceId(selectedInstanceIds[i]);
                if (selected.DefinitionId != entry.DefinitionId) continue;
                selectedInstanceIds[i] = entry.CharacterInstanceId;
                return true;
            }
            return TryAddInstance(characterInstanceId);
        }

        public RunDeckSnapshot CreateSnapshot()
        {
            var selected = new List<OwnedCharacterState>();
            foreach (var instanceId in selectedInstanceIds) selected.Add(collection.GetByInstanceId(instanceId));
            return new RunDeckSnapshot(selected);
        }
    }
}
