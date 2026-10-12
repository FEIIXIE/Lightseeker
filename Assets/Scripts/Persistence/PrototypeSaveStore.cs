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
        public int qualityRecruitPriceRulesVersion;
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
        public int roguelikeRuntimeGeneration;
        public string selectedRoguelikeKeystoneId;
        public ChapterOneRoguelikeChoiceSetV11 pendingRoguelikeChoiceSetV11;
        public RoguelikeAcquiredEffect[] acquiredRoguelikeEffectsV11;
        public ChapterOneRoguelikeSelectionReceipt[] roguelikeChoiceReceiptsV11;
        public int encounterPowerBonus;
        public int encounterReward;
        public bool currentEncounterIsElite;
        public int phase;
        public bool runWon;
        public bool runAbandoned;
        public int finalScore;
        public int expeditionEnergy;
        public int skrBalance;
        public int skrSubunitMicro;
        public bool expeditionRewardsEnabled;
        public bool expeditionEntryCommitted;
        public int tideCrystalBalance;
        public int localLampGlowBalance;
        public int localTideIronBalance;
        public int localMistLeatherBalance;
        public int localStarSandBalance;
        public int localBountyTicketBalance;
        // Legacy v32-and-earlier read compatibility. The material repair economy was retired in v33.
        public int stoneBalance;
        public int lampOilBalance;
        public int mistLensBalance;
        public int nextLocalEquipmentSerial;
        public int pendingTideCrystals;
        // Legacy v32-and-earlier read compatibility; never written by the current runtime.
        public int pendingStone;
        public int pendingLampOil;
        public int pendingMistLens;
        public string[] pendingEquipmentTemplateIds;
        public string[] completedRewardNodeIds;
        public bool expeditionRewardsClaimed;
        public bool chapterOneBossDefeated;
        // Legacy migration input. A repaired lighthouse becomes a lit lighthouse.
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
        public string[] expeditionBackpackItemInstanceIds;
        public string[] targetedRecruitTickets;
        public string[] resolvedEconomyActionIds;
        public string activeBattleItemId;
        public string activeBattleItemInstanceId;
        public int activeBattleTargetSlot;
        public string[] lockedEconomyContentKeys;
        public string[] lockedEconomyContentValues;
        public string[] resolvedRunEconomyActionIds;
        public int[] resolvedRunEconomyActionResultCodes;
        public int[] resolvedRunEconomyActionVersions;
        public string lastRunEconomyActionId;
        public int lastRunEconomyResultCode;
        public PrototypeCollectionSaveData[] collection;
        public string[] favoriteCharacterInstanceIds;
        public int storePurchaseSerial;
        public int storeEpicPitySkr;
        public int storeLegendaryPitySkr;
        public string[] ownedBasePackIds;
        public string equippedBasePackProductId;
        public string equippedBattlefieldProductId;
        public bool storeOpeningActive;
        public float storeOpeningProgress;
        public string pendingStoreOrderId;
        public string[] pendingStoreInstanceIds;
        public string[] pendingStoreDefinitionIds;
        public int[] pendingStoreQualities;
        public int[] pendingStoreAptitudeRolls;
        public bool pendingStoreRevealAlreadyGranted;
        public string pendingStarterGrantId;
        public string acknowledgedStarterGrantId;
        public PrototypeEquipmentSaveData[] equipment;
        public string[] warehouseSlotTokens;
        public string[] warehouseAcquisitionOrder;
        public int warehouseSortMode;
        public PrototypeLoadoutSnapshotSaveData[] activeRunLoadouts;
        public PrototypeUnitSaveData[] defenseUnits;
        public int arenaRating;
        public int arenaChallengesRemaining;
        public string arenaChallengeDayKey;
        public PrototypeLanternInstanceSaveData[] lanternInventory;
        public string activeLanternInstanceId;
        public PrototypeLanternSnapshotSaveData expeditionLanternSnapshot;
        public PrototypeExpeditionRunRewardSnapshotSaveData expeditionRewardSnapshot;
        public bool chapterOneLanternAwarded;
        public bool chapterOneLighthouseLit;
        public string arenaBattleId;
        public PrototypeLanternSnapshotSaveData arenaAttackLanternSnapshot;
        public PrototypeLanternSnapshotSaveData arenaDefenseLanternSnapshot;
        public int arenaDefenseSnapshotVersion;
        public bool arenaDefenseNeedsResync;
        public bool marketSeeded;
        public string marketLeaseSeasonId;
        public int marketShopLeaseCount;
        public int marketStateVersion;
        public long marketBurnPoolMicroSkr;
        public long marketTeamRevenueMicroSkr;
        public int marketListingSerial;
        public int marketReceiptSerial;
        public PrototypeMarketListingSaveData[] marketListings;
        public PrototypeMarketReceiptSaveData[] marketReceipts;
    }

    [Serializable]
    internal sealed class PrototypeMarketListingSaveData
    {
        public string listingId;
        public string sellerId;
        public string sellerDisplayName;
        public int assetKind;
        public string assetInstanceId;
        public string templateId;
        public int quality;
        public string seasonId;
        public long priceMicroSkr;
        public long createdAtUtcTicks;
        public int state;
        public string buyerId;
        public long resolvedAtUtcTicks;
    }

    [Serializable]
    internal sealed class PrototypeMarketReceiptSaveData
    {
        public string receiptId;
        public string listingId;
        public string sellerId;
        public string buyerId;
        public long sellerProceedsMicroSkr;
        // Legacy v38 field. v39+ writes sellerFeeMicroSkr instead.
        public long buyerFeeMicroSkr;
        public long sellerFeeMicroSkr;
        public long burnMicroSkr;
        public long teamMicroSkr;
        public long buyerTotalMicroSkr;
        public long completedAtUtcTicks;
    }

    [Serializable]
    internal sealed class PrototypeCollectionSaveData
    {
        public string characterInstanceId;
        public string definitionId;
        public int copyCount;
        public int externalStar;
        public int quality;
        public long acquiredAtUtcTicks;
        public int aptitudeRoll;
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

    [Serializable]
    internal sealed class PrototypeLanternModifierSaveData
    {
        public string modifierId;
        public int value;
    }

    [Serializable]
    internal sealed class PrototypeLanternInstanceSaveData
    {
        public string lanternInstanceId;
        public string templateId;
        public int quality;
        public string seasonId;
        public string sourceChapterId;
        public string sourceDifficultyId;
        public string coreEffectId;
        public string baseFlameStatId;
        public int baseFlameValue;
        public int contractStage;
        public PrototypeLanternModifierSaveData[] modifierRolls;
        public string rewardReceiptId;
        public string rulesVersion;
        public int usageLocks;
    }

    [Serializable]
    internal sealed class PrototypeLanternSnapshotSaveData
    {
        public bool isNone;
        public string lanternInstanceId;
        public string templateId;
        public int quality;
        public string seasonId;
        public string sourceChapterId;
        public string coreEffectId;
        public string baseFlameStatId;
        public int baseFlameValue;
        public int contractStage;
        public PrototypeLanternModifierSaveData[] modifierRolls;
        public string rulesVersion;
    }

    [Serializable]
    internal sealed class PrototypeRewardTableReferenceSaveData
    {
        public string tableId;
        public string version;
    }

    [Serializable]
    internal sealed class PrototypeExpeditionRunRewardSnapshotSaveData
    {
        public string difficultyId;
        public string entryTypeId;
        public string paymentTierId;
        public PrototypeRewardTableReferenceSaveData crystalRewards;
        public PrototypeRewardTableReferenceSaveData equipmentRewards;
        public PrototypeRewardTableReferenceSaveData lanternRewards;
        public string rulesVersion;
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
