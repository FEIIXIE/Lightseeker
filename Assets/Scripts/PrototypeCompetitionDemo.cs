using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private static bool CompetitionDemoMode
        {
            get { return true; }
        }
        private const string CompetitionDemoProfilePreferenceKey =
            "lightseeker.competition-demo.profile.v0.43.411";

        private bool UsesServerAuthoritativeExpedition
        {
            get { return IsMainnetInviteRuntimeConfigured && !CompetitionDemoMode; }
        }

        private void EnsureCompetitionDemoProfileInitialized(string accountId)
        {
            if (!CompetitionDemoMode || string.IsNullOrWhiteSpace(accountId)) return;
            var marker = CompetitionDemoRules.ProfileVersion + ":" + accountId;
            if (string.Equals(PlayerPrefs.GetString(CompetitionDemoProfilePreferenceKey, string.Empty),
                    marker, StringComparison.Ordinal)) return;

            EnsureRuntimeState();
            var characters = CompetitionDemoRules.CreateInitialCharacters(characterCatalog);
            playerCollection = new PlayerCollection(characterCatalog, characters);
            var selectedIds = new string[characters.Length];
            for (var index = 0; index < characters.Length; index++)
                selectedIds[index] = characters[index].CharacterInstanceId;
            nextRunDeckSelection = new RunDeckSelection(
                playerCollection, selectedIds, MaximumDeckSize, MinimumDeckSize);
            deckDraftSelection = null;
            deckEquipmentDraft = null;
            runDeck = nextRunDeckSelection.CreateSnapshot();
            activeDeckUnitIds = CopyIds(runDeck.UnitDefinitionIds);
            selectedEquipmentCharacterInstanceId = characters[0].CharacterInstanceId;
            characterFavorites.Restore(Array.Empty<string>(), playerCollection);

            equipmentInventory = new EquipmentInventory(
                equipmentCatalog, CompetitionDemoRules.CreateInitialEquipment(equipmentCatalog));
            var initialLantern = CompetitionDemoRules.CreateInitialLantern();
            lanternInventory = new LanternInventory(
                new[] { initialLantern }, initialLantern.LanternInstanceId);
            chapterOneLighthouse = new ChapterLighthouseProgressState();
            expeditionRewards = new ExpeditionRewardLedger();
            expeditionEconomy = new ExpeditionEconomyState();
            expeditionEnergy = ExpeditionEnergyRules.InitialEnergy;
            currentExpeditionRewardsEnabled = true;
            skrBalance = CompetitionDemoRules.InitialSkrBalance;
            skrSubunitMicro = 0;
            tideCrystalBalance = 0;
            localLampGlowBalance = 0;
            localTideIronBalance = 0;
            localMistLeatherBalance = 0;
            localStarSandBalance = 0;
            localBountyTicketBalance = 0;
            storePurchaseSerial = 0;
            storeEpicPitySkr = 0;
            storeLegendaryPitySkr = 0;
            ownedBasePackIds.Clear();
            equippedBasePackProductId = null;
            equippedBattlefieldProductId = null;
            pendingStoreOrderId = null;
            pendingStoreInstanceIds = new string[0];
            pendingStoreDefinitionIds = new string[0];
            pendingStoreQualities = new int[0];
            pendingStoreAptitudeRolls = new int[0];
            pendingStoreRevealAlreadyGranted = false;
            pendingStarterGrantId = null;
            acknowledgedStarterGrantId = null;

            marketBoard.Restore(Array.Empty<MarketListing>(), Array.Empty<MarketTradeReceipt>(),
                0, 0, 0L, 0L, 0, 0);
            marketSeeded = false;
            EnsureMarketState();
            marketAcceptanceGrantChecked = true;
            selectedMarketListingId = null;
            selectedMarketAssetInstanceId = null;
            selectedMarketPreviewAssetInstanceId = null;
            warehouseAcquisitionOrder.Clear();
            Array.Clear(warehouseSlotTokens, 0, warehouseSlotTokens.Length);

            formalExpeditionRunContext = null;
            pendingFormalExpeditionSettlement = null;
            pendingLocalExpeditionSettlementPlan = null;
            authoritativeRewardInventoryMirror.Clear();
            formalExpeditionEntryInProgress = false;
            formalExpeditionAuthorityCommitInProgress = false;
            formalExpeditionAuthorityCommitSucceeded = true;
            RestartExpeditionState("status.route");
            expeditionEntryCommitted = true;
            ReconcileWarehouseLayout();
            SaveGame();
            PlayerPrefs.SetString(CompetitionDemoProfilePreferenceKey, marker);
            PlayerPrefs.Save();
        }

        private void BeginCompetitionDemoExpeditionFromHome(bool startsNewRun)
        {
            var session = CurrentWalletServerSession;
            if (WalletServerState != WalletServerSessionState.Authenticated || session == null)
            {
                ShowFormalExpeditionEntryStatus("home.expeditionLoginRequired");
                return;
            }
            EnsureCompetitionDemoProfileInitialized(session.AccountId);
            formalExpeditionRunContext = null;
            pendingFormalExpeditionSettlement = null;
            if (startsNewRun) RestartRun();
            homeTopMenu = 0;
            currentView = ScreenView.Expedition;
        }
    }
}
