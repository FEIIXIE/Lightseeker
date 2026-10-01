using System;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Persistence
{
    [Serializable]
    internal sealed class PrototypeRunSaveData
    {
        public int saveVersion;
        public int runEconomyRulesVersion;
        public string runId;
        public int runStateVersion;
        public int coins;
        public int health;
        public int round;
        public int mapSeed;
        public int mapGenerationVersion;
        public int mapVisualGenerationVersion;
        public string currentMapNodeId;
        public int runAttackBonus;
        public int runHealthBonus;
        public int roguelikeRulesVersion;
        public int attackSpeedRulesVersion;
        public int roguelikeRunStateVersion;
        public int roguelikeChoiceOrdinal;
        public RoguelikeChoiceSet pendingRoguelikeChoiceSet;
        public RoguelikeAcquiredEffect[] acquiredRoguelikeEffects;
        public RoguelikeChoiceReceipt[] roguelikeChoiceReceipts;
        public int encounterPowerBonus;
        public int encounterReward;
        public bool currentEncounterIsElite;
        public int phase;
        public bool runWon;
        public bool runAbandoned;
        public int finalScore;
        public int expeditionEnergy;
        public int skrBalance;
        public bool expeditionRewardsEnabled;
        public bool expeditionEntryCommitted;
        public int tideCrystalBalance;
        public int stoneBalance;
        public int lampOilBalance;
        public int mistLensBalance;
        public int nextLocalEquipmentSerial;
        public int pendingTideCrystals;
        public int pendingStone;
        public int pendingLampOil;
        public int pendingMistLens;
        public string[] pendingEquipmentTemplateIds;
        public string[] completedRewardNodeIds;
        public bool expeditionRewardsClaimed;
        public bool chapterOneBossDefeated;
        public bool chapterOneLighthouseRepaired;
        public PrototypeUnitSaveData[] units;
        public PrototypeUnitSaveData[] benchUnits;
        public string[] activeDeckUnitIds;
        public string[] activeDeckCharacterInstanceIds;
        public string[] nextRunDeckUnitIds;
        public string[] nextRunDeckCharacterInstanceIds;
        public string[] shopOffers;
        public int[] shopOfferLevels;
        public string[] shopOfferIds;
        public int[] shopOfferListedPrices;
        public bool[] shopOfferSold;
        public int shopRefreshSerial;
        public bool firstManualRecruitRerollUsed;
        public int recruitRerollVouchers;
        public int recruitmentDiscount;
        public int chestRecruitmentDiscount;
        public int expeditionMorale;
        public string nextBattlePreparationId;
        public string[] expeditionBackpackItemIds;
        public string[] resolvedEconomyActionIds;
        public string activeBattleItemId;
        public int activeBattleTargetSlot;
        public string[] lockedEconomyContentKeys;
        public string[] lockedEconomyContentValues;
        public string[] resolvedRunEconomyActionIds;
        public int[] resolvedRunEconomyActionResultCodes;
        public int[] resolvedRunEconomyActionVersions;
        public string lastRunEconomyActionId;
        public int lastRunEconomyResultCode;
        public PrototypeCollectionSaveData[] collection;
        public int storePurchaseSerial;
        public int storeEpicPitySkr;
        public int storeLegendaryPitySkr;
        public string[] ownedBasePackIds;
        public bool storeOpeningActive;
        public float storeOpeningProgress;
        public string pendingStoreOrderId;
        public string[] pendingStoreInstanceIds;
        public string[] pendingStoreDefinitionIds;
        public int[] pendingStoreQualities;
        public PrototypeEquipmentSaveData[] equipment;
        public PrototypeLoadoutSnapshotSaveData[] activeRunLoadouts;
        public PrototypeUnitSaveData[] defenseUnits;
        public int arenaRating;
        public int arenaChallengesRemaining;
        public string arenaChallengeDayKey;
    }

    [Serializable]
    internal sealed class PrototypeCollectionSaveData
    {
        public string characterInstanceId;
        public string definitionId;
        public int copyCount;
        public int externalStar;
        public int quality;
    }

    [Serializable]
    internal sealed class PrototypeEquipmentSaveData
    {
        public string equipmentInstanceId;
        public string templateId;
        public int quality;
        public string seasonId;
        public string equippedCharacterInstanceId;
        public int usageLocks;
    }

    [Serializable]
    internal sealed class PrototypeLoadoutSnapshotSaveData
    {
        public string definitionId;
        public string[] equipmentInstanceIds;
        public int maxHealth;
        public int attackDamage;
        public int armor;
        public int healAmount;
        public int speedTierShift;
    }

    [Serializable]
    internal sealed class PrototypeUnitSaveData
    {
        public bool occupied;
        public string runUnitId;
        public string nameKey;
        public int power;
        public int basePower;
        public int runLevel;
        public int acquisitionBasis;
        public int quality;
        public int aptitudeRoll;
        public string sourceBusinessId;
        public int externalStar;
        public string characterInstanceId;
        public int maxHealth;
        public int attackDamage;
        public int healAmount;
        public bool isHealer;
    }

    internal static class PrototypeSaveStore
    {
        public static bool TryLoad(string key, out PrototypeRunSaveData data)
        {
            data = null;
            if (!PlayerPrefs.HasKey(key)) return false;
            try
            {
                data = JsonUtility.FromJson<PrototypeRunSaveData>(PlayerPrefs.GetString(key));
                return data != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void Save(string key, PrototypeRunSaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
