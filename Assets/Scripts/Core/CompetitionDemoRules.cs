using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Immutable seed rules for the competition demo. Wallet identity is authenticated by the
    /// server, while every value produced here is deliberately local and non-authoritative.
    /// </summary>
    public static class CompetitionDemoRules
    {
        public const string ProfileVersion = "competition-demo.v0.43.411";
        public const int InitialSkrBalance = 500;
        public const string InitialLanternInstanceId = "demo.v043410.lantern.initial";

        public static OwnedCharacterState[] CreateInitialCharacters(CharacterCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var result = new List<OwnedCharacterState>();
            var acquiredAt = OwnedCharacterState.LegacyAcquisitionEpochUtcTicks;
            foreach (var definition in catalog.Definitions)
            {
                if (!definition.IsCollectible) continue;
                result.Add(new OwnedCharacterState(
                    "demo.v043410.character." + definition.DefinitionId,
                    definition.DefinitionId, 1, 0, CharacterQuality.Common, acquiredAt++));
            }
            return result.ToArray();
        }

        public static EquipmentInstanceState[] CreateInitialEquipment(EquipmentCatalog catalog)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            var result = new List<EquipmentInstanceState>();
            for (var index = 0; index < catalog.Definitions.Count; index++)
            {
                result.Add(new EquipmentInstanceState(
                    "demo.v043410.equipment." + (index + 1).ToString("00"),
                    catalog.Definitions[index].TemplateId,
                    EquipmentQuality.Common,
                    "competition-demo"));
            }
            return result.ToArray();
        }

        public static LanternInstanceState CreateInitialLantern()
        {
            return FirstChapterLanternEconomyRules.CreateGeneratedInstance(
                InitialLanternInstanceId,
                LanternQuality.Common,
                ProfileVersion + ":initial-lantern",
                ProfileVersion + ":initial-lantern-receipt");
        }

        public static LanternInstanceState CreateCompletionLantern(string runId, uint mapSeed)
        {
            if (string.IsNullOrWhiteSpace(runId))
                throw new ArgumentException("A local run id is required.", nameof(runId));
            var suffix = runId.Replace("run-", string.Empty);
            return FirstChapterLanternEconomyRules.CreateGeneratedInstance(
                "demo.v043410.reward.lantern." + suffix,
                LanternQuality.Rare,
                ProfileVersion + ":completion:" + runId + ":" + mapSeed,
                ProfileVersion + ":completion-receipt:" + runId);
        }

        public static int IncomingRewardSlots(int equipmentCount, bool includesCompletionLantern)
        {
            if (equipmentCount < 0) throw new ArgumentOutOfRangeException(nameof(equipmentCount));
            return equipmentCount + (includesCompletionLantern ? 1 : 0);
        }
    }
}
