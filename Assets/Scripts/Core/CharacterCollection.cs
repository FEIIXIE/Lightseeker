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

    /// <summary>
    /// Persistent, out-of-run ownership. External star and quality never represent
    /// the temporary run level used by three-to-one merging.
    /// </summary>
    public sealed class OwnedCharacterState
    {
        public string CharacterInstanceId { get; }
        public string DefinitionId { get; }
        public int CopyCount { get; }
        public int ExternalStar { get; }
        public CharacterQuality Quality { get; }

        public OwnedCharacterState(string definitionId, int copyCount, int externalStar, CharacterQuality quality)
            : this("character." + definitionId, definitionId, copyCount, externalStar, quality)
        {
        }

        public OwnedCharacterState(
            string characterInstanceId,
            string definitionId,
            int copyCount,
            int externalStar,
            CharacterQuality quality)
        {
            if (string.IsNullOrWhiteSpace(characterInstanceId)) throw new ArgumentException("A character instance id is required.", nameof(characterInstanceId));
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("A definition id is required.", nameof(definitionId));
            if (copyCount < 1) throw new ArgumentOutOfRangeException(nameof(copyCount));
            if (externalStar < 0) throw new ArgumentOutOfRangeException(nameof(externalStar));
            if (!Enum.IsDefined(typeof(CharacterQuality), quality)) throw new ArgumentOutOfRangeException(nameof(quality));
            CharacterInstanceId = characterInstanceId;
            DefinitionId = definitionId;
            CopyCount = copyCount;
            ExternalStar = externalStar;
            Quality = quality;
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
            foreach (var definition in catalog.Definitions)
            {
                if (!definition.IsCollectible) continue;
                entries.Add(new OwnedCharacterState(definition.DefinitionId, 1, 0, CharacterQuality.Common));
            }
            return new PlayerCollection(catalog, entries);
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
            if (minimumSize <= 0 || minimumSize > maximumSize) throw new ArgumentOutOfRangeException(nameof(minimumSize));
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
