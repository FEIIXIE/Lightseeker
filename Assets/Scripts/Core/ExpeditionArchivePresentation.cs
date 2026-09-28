using System;

namespace SeekerAutoChess.Core
{
    public enum ExpeditionBuffRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    /// <summary>
    /// Stable presentation-only selections for the E41/E43 archive UI. These rules do
    /// not change combat values, rewards, faction ids, or save data.
    /// </summary>
    public static class ExpeditionArchivePresentation
    {
        public static ExpeditionBuffRarity SelectBuffRarity(
            uint mapSeed,
            string nodeId,
            int round,
            int offerIndex)
        {
            if (round < 1) throw new ArgumentOutOfRangeException(nameof(round));
            if (offerIndex < 0) throw new ArgumentOutOfRangeException(nameof(offerIndex));

            var hash = mapSeed ^ 2166136261u;
            Mix(ref hash, nodeId ?? string.Empty);
            Mix(ref hash, round.ToString());
            Mix(ref hash, offerIndex.ToString());
            var roll = (int)(hash % 100u);

            if (roll < 4) return ExpeditionBuffRarity.Legendary;
            if (roll < 13) return ExpeditionBuffRarity.Epic;
            if (roll < 30) return ExpeditionBuffRarity.Rare;
            if (roll < 58) return ExpeditionBuffRarity.Uncommon;
            return ExpeditionBuffRarity.Common;
        }

        public static string FactionPresentationKey(string factionId)
        {
            switch (factionId)
            {
                case "faction.steel": return "steel";
                case "faction.wild": return "wild";
                case "faction.starlight": return "starlight";
                default: return "neutral";
            }
        }

        private static void Mix(ref uint hash, string value)
        {
            unchecked
            {
                for (var index = 0; index < value.Length; index++)
                {
                    hash ^= value[index];
                    hash *= 16777619u;
                }
                hash ^= 0x9Eu;
                hash *= 16777619u;
            }
        }
    }
}
