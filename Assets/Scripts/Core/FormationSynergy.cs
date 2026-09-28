using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class FormationSynergySnapshot
    {
        private readonly HashSet<string> activeFactions;
        private readonly HashSet<string> activeProfessions;

        internal FormationSynergySnapshot(HashSet<string> factions, HashSet<string> professions)
        {
            activeFactions = factions;
            activeProfessions = professions;
        }

        public IReadOnlyCollection<string> ActiveFactions { get { return activeFactions; } }
        public IReadOnlyCollection<string> ActiveProfessions { get { return activeProfessions; } }

        public bool IsFactionActive(string factionId)
        {
            return !string.IsNullOrEmpty(factionId) && activeFactions.Contains(factionId);
        }

        public bool IsProfessionActive(string professionId)
        {
            return !string.IsNullOrEmpty(professionId) && activeProfessions.Contains(professionId);
        }

        public int GetMaximumHealthPercent(CharacterDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return definition.ProfessionId == "profession.guardian" && IsProfessionActive(definition.ProfessionId)
                ? 12
                : 0;
        }

        public int GetOpeningShieldPercent(CharacterDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return definition.FactionId == "faction.steel" && IsFactionActive(definition.FactionId)
                ? 12
                : 0;
        }

        public int GetHealPercent(CharacterDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return definition.ProfessionId == "profession.support" && IsProfessionActive(definition.ProfessionId)
                ? 15
                : 0;
        }
    }

    public static class FormationSynergy
    {
        public const int ActivationCount = 2;

        public static FormationSynergySnapshot Calculate(
            CharacterCatalog catalog,
            IEnumerable<string> deployedDefinitionIds)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (deployedDefinitionIds == null) throw new ArgumentNullException(nameof(deployedDefinitionIds));

            var uniqueCharacters = new HashSet<string>(StringComparer.Ordinal);
            var factionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            var professionCounts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var id in deployedDefinitionIds)
            {
                if (string.IsNullOrEmpty(id) || !catalog.Contains(id) || !uniqueCharacters.Add(id)) continue;
                var definition = catalog.Get(id);
                Increment(factionCounts, definition.FactionId);
                Increment(professionCounts, definition.ProfessionId);
            }

            return new FormationSynergySnapshot(
                Activated(factionCounts),
                Activated(professionCounts));
        }

        private static void Increment(IDictionary<string, int> counts, string id)
        {
            int value;
            counts[id] = counts.TryGetValue(id, out value) ? value + 1 : 1;
        }

        private static HashSet<string> Activated(IDictionary<string, int> counts)
        {
            var active = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in counts)
            {
                if (pair.Value >= ActivationCount) active.Add(pair.Key);
            }
            return active;
        }
    }
}
