using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Persistence;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private const string MarketSeasonId = "season-0";
        private readonly MarketBoard marketBoard = new MarketBoard();
        private bool marketSeeded;
        private int skrSubunitMicro;
        private MarketPageTab marketPageTab;
        private MarketAssetFilter marketAssetFilter;
        private Vector2 marketScroll;
        private string selectedMarketListingId;
        private string selectedMarketPreviewAssetInstanceId;
        private MarketAssetKind selectedMarketAssetKind;
        private string selectedMarketAssetInstanceId;
        private int marketListingPriceSkr;
        private string marketListingPriceInput = string.Empty;
        private string marketFeedbackKey = "market.status.ready";
        private int marketOperationSerial;
        private bool marketShelfExpansionConfirmOpen;
        private bool marketModalSubmissionInProgress;
        private string pendingMarketWarehouseFocusInstanceId;
        private MarketAssetKind? pendingMarketWarehouseFocusKind;
        private bool marketAcceptanceGrantChecked;

        private enum MarketPageTab { Browse, Sell, Mine, History }
        private enum MarketAssetFilter { All, Equipment, Lantern }

        private long WalletBalanceMicroSkr
        {
            get { return checked((long)Mathf.Max(0, skrBalance) * MarketRules.MicroSkrPerSkr + Mathf.Max(0, skrSubunitMicro)); }
        }

        private string WalletBalanceText()
        {
            return MarketRules.FormatMicroSkr(WalletBalanceMicroSkr);
        }

        private void SetWalletBalanceMicroSkr(long value)
        {
            value = Math.Max(0L, value);
            skrBalance = checked((int)(value / MarketRules.MicroSkrPerSkr));
            skrSubunitMicro = checked((int)(value % MarketRules.MicroSkrPerSkr));
        }

        private void DebitWalletWholeSkr(int amount)
        {
            SetWalletBalanceMicroSkr(WalletBalanceMicroSkr - MarketRules.ToMicroSkr(amount));
        }

        private void EnsureMarketState()
        {
            if (marketSeeded) return;
            marketSeeded = true;
            SeedRemoteMarketListing("remote.shoal.001", "market.seller.shoal",
                MarketAssetKind.Equipment, "remote.equipment.001", "equipment.lanternHarborBlade",
                (int)EquipmentQuality.Common, 18);
            SeedRemoteMarketListing("remote.forge.001", "market.seller.forge",
                MarketAssetKind.Equipment, "remote.equipment.002", "equipment.forgeRivetGauntlet",
                (int)EquipmentQuality.Rare, 25);
            SeedRemoteMarketListing("remote.voyage.001", "market.seller.voyage",
                MarketAssetKind.Equipment, "remote.equipment.003", "equipment.oldVoyageClothes",
                (int)EquipmentQuality.Epic, 40);
            SeedRemoteMarketListing("remote.lamp.001", "market.seller.lamp",
                MarketAssetKind.Lantern, "remote.lantern.001", FirstChapterLanternFixtures.GuardTideTemplateId,
                (int)LanternQuality.Rare, 80);
        }

        private void SeedRemoteMarketListing(string sellerId, string sellerNameKey,
            MarketAssetKind kind, string instanceId, string templateId, int quality, int priceSkr)
        {
            MarketListing ignored;
            marketBoard.TryCreateListing(sellerId, T(sellerNameKey), kind, instanceId, templateId,
                quality, MarketSeasonId, MarketRules.ToMicroSkr(priceSkr), DateTime.UtcNow.Ticks, out ignored);
        }

        private void DrawLegacyMarketScreen()
        {
            EnsureMarketState();
            GrantMarketAcceptanceInventoryIfNeeded();
            var accent = new Color(0.16f, 0.58f, 0.56f);
            var ink = new Color(0.08f, 0.18f, 0.20f);
            var muted = new Color(0.31f, 0.42f, 0.43f);
            DrawHomeLighthouseCanvas(PrototypeArtResourceIds.HomeLighthouseBackground, false);
            GUI.Label(new Rect(55f, 70f, 520f, 70f), T("market.title"),
                Style(46, new Color(0.94f, 0.87f, 0.68f), TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(55f, 138f, 700f, 44f), T("market.subtitle"),
                Style(19, new Color(0.80f, 0.84f, 0.78f), TextAnchor.MiddleLeft));
            GUI.Label(new Rect(805f, 65f, 220f, 48f),
                string.Format(T("market.balance"), WalletBalanceText()),
                Style(20, new Color(0.94f, 0.87f, 0.68f), TextAnchor.MiddleRight, FontStyle.Bold));
            if (GUI.Button(new Rect(805f, 128f, 220f, 54f), T("market.backToHarbor"),
                    ButtonStyle(UiButtonSemantic.Secondary, 17)))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "market-back-harbor");
                currentView = ScreenView.Harbor;
                return;
            }

            var rulePanel = new Rect(55f, 215f, 970f, 118f);
            uiSkin.DrawPanel(rulePanel, true);
            GUI.Label(new Rect(80f, 228f, 920f, 38f),
                string.Format(T("market.rules"), MarketRules.MinimumListingPriceSkr),
                Style(17, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(80f, 273f, 620f, 36f),
                string.Format(T("market.slots"), marketBoard.CountActiveListings(MarketRules.LocalPlayerId),
                    marketBoard.ListingCapacity), Style(16, muted, TextAnchor.MiddleLeft));
            if (GUI.Button(new Rect(720f, 268f, 280f, 46f),
                    string.Format(T("market.lease"), MarketRules.ShopLeasePriceSkr, MarketRules.SlotsPerShopLease),
                    ButtonStyle(UiButtonSemantic.Secondary, 15)))
            {
                TryLeaseMarketShop();
            }

            DrawMarketTabs(355f);
            GUI.Label(new Rect(70f, 445f, 940f, 42f), T(marketFeedbackKey),
                Style(16, muted, TextAnchor.MiddleLeft));
            switch (marketPageTab)
            {
                case MarketPageTab.Sell:
                    DrawMarketSell(ink, muted, accent);
                    break;
                case MarketPageTab.Mine:
                    DrawMyMarketListings(ink, muted, accent);
                    break;
                case MarketPageTab.History:
                    DrawMarketHistory(ink, muted, accent);
                    break;
                default:
                    DrawMarketBrowse(ink, muted, accent);
                    break;
            }
        }

        private void GrantMarketAcceptanceInventoryIfNeeded()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (marketAcceptanceGrantChecked) return;
            marketAcceptanceGrantChecked = true;
            if (PlayerPrefs.GetInt(MarketAcceptanceGrantPreferenceKey, 0) != 0) return;

            SetWalletBalanceMicroSkr(Math.Max(WalletBalanceMicroSkr, MarketRules.ToMicroSkr(3000)));
            var capacityRemaining = Mathf.Max(0, WarehouseStorageRules.InitialCapacity -
                WarehouseStorageRules.CountUsedSlots(
                    equipmentInventory.Instances.Count, lanternInventory.Instances.Count));
            var grantCount = Mathf.Min(5, capacityRemaining);
            var acceptanceQualities = new[]
            {
                EquipmentQuality.Rare,
                EquipmentQuality.Epic,
                EquipmentQuality.Rare,
                EquipmentQuality.Epic,
                EquipmentQuality.Common
            };
            for (var index = 0; index < grantCount; index++)
            {
                var definition = equipmentCatalog.Definitions[index % equipmentCatalog.Definitions.Count];
                equipmentInventory.TryAddFirst(new EquipmentInstanceState(
                    "market.acceptance.v043281." + (index + 1).ToString("D2"),
                    definition.TemplateId, acceptanceQualities[index], MarketSeasonId));
            }
            SeedMarketAcceptancePurchaseListings();
            SeedMarketAcceptanceHistory();

            SaveGame();
            PlayerPrefs.SetInt(MarketAcceptanceGrantPreferenceKey, 1);
            PlayerPrefs.Save();
            marketFeedbackKey = grantCount > 0
                ? "market.status.acceptanceGrant"
                : "market.status.acceptanceGrantWarehouseFull";
#endif
        }

        private void SeedMarketAcceptancePurchaseListings()
        {
            var now = DateTime.UtcNow.Ticks;
            for (var index = 0; index < equipmentCatalog.Definitions.Count; index++)
            {
                var definition = equipmentCatalog.Definitions[index % equipmentCatalog.Definitions.Count];
                MarketListing ignored;
                marketBoard.TryCreateListing(
                    "market-acceptance-equipment-v043281", T("market.seller.acceptance"),
                    MarketAssetKind.Equipment,
                    "market.acceptance.equipment.v043281." + (index + 1).ToString("D2"),
                    definition.TemplateId, index % 3, MarketSeasonId,
                    MarketRules.ToMicroSkr(10 + index * 5), now + index,
                    out ignored);
            }

            for (var index = 0; index <= (int)LanternQuality.Legendary; index++)
            {
                MarketListing ignored;
                marketBoard.TryCreateListing(
                    "market-acceptance-lantern-v043281", T("market.seller.acceptance"),
                    MarketAssetKind.Lantern,
                    "market.acceptance.lantern.v043281." + (index + 1).ToString("D2"),
                    HomeLanternModularArtContract.TemplateIdForAppearanceId(
                        "L" + (index + 1).ToString("00")),
                    index, MarketSeasonId, MarketRules.ToMicroSkr(45 + index * 15),
                    now + 100 + index, out ignored);
            }
        }

        private void SeedMarketAcceptanceHistory()
        {
            if (equipmentCatalog.Definitions.Count == 0) return;
            var now = DateTime.UtcNow.Ticks;
            var template = equipmentCatalog.Definitions[0].TemplateId;

            MarketListing bought;
            if (marketBoard.TryCreateListing("market-history-seller-v043249", T("market.seller.acceptance"),
                    MarketAssetKind.Equipment, "market.history.bought.v043249", template,
                    (int)EquipmentQuality.Rare, MarketSeasonId, MarketRules.ToMicroSkr(35),
                    now - TimeSpan.FromMinutes(30).Ticks, out bought))
            {
                long ignoredBalance;
                MarketTradeReceipt ignoredReceipt;
                marketBoard.TryPurchase(MarketRules.LocalPlayerId, bought.ListingId,
                    MarketRules.ToMicroSkr(999999), now - TimeSpan.FromMinutes(25).Ticks,
                    receipt => true, out ignoredBalance, out ignoredReceipt);
            }

            MarketListing sold;
            if (marketBoard.TryCreateListing(MarketRules.LocalPlayerId, T("market.you"),
                    MarketAssetKind.Equipment, "market.history.sold.v043249", template,
                    (int)EquipmentQuality.Epic, MarketSeasonId, MarketRules.ToMicroSkr(60),
                    now - TimeSpan.FromMinutes(20).Ticks, out sold))
            {
                long ignoredBalance;
                MarketTradeReceipt ignoredReceipt;
                marketBoard.TryPurchase("market-history-buyer-v043249", sold.ListingId,
                    MarketRules.ToMicroSkr(999999), now - TimeSpan.FromMinutes(15).Ticks,
                    receipt => true, out ignoredBalance, out ignoredReceipt);
            }

            MarketListing cancelled;
            if (marketBoard.TryCreateListing(MarketRules.LocalPlayerId, T("market.you"),
                    MarketAssetKind.Equipment, "market.history.cancelled.v043249", template,
                    (int)EquipmentQuality.Common, MarketSeasonId, MarketRules.ToMicroSkr(20),
                    now - TimeSpan.FromMinutes(10).Ticks, out cancelled))
                marketBoard.TryCancelListing(MarketRules.LocalPlayerId, cancelled.ListingId,
                    now - TimeSpan.FromMinutes(5).Ticks);
        }

        private void DrawMarketTabs(float y)
        {
            var keys = new[] { "market.tab.browse", "market.tab.sell", "market.tab.mine", "market.tab.history" };
            for (var index = 0; index < keys.Length; index++)
            {
                if (!GUI.Button(new Rect(55f + index * 242f, y, 232f, 66f), T(keys[index]),
                        TabStyle((int)marketPageTab == index, 18)) || (int)marketPageTab == index) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "market-tab");
                marketPageTab = (MarketPageTab)index;
                marketScroll = Vector2.zero;
                selectedMarketListingId = null;
                selectedMarketAssetInstanceId = null;
                marketFeedbackKey = "market.status.ready";
            }
        }

        private void DrawMarketBrowse(Color ink, Color muted, Color accent)
        {
            var filterKeys = new[] { "market.filter.all", "market.filter.equipment", "market.filter.lantern" };
            for (var index = 0; index < filterKeys.Length; index++)
            {
                if (!GUI.Button(new Rect(55f + index * 205f, 500f, 195f, 54f), T(filterKeys[index]),
                        TabStyle((int)marketAssetFilter == index, 16)) || (int)marketAssetFilter == index) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "market-filter");
                marketAssetFilter = (MarketAssetFilter)index;
                marketScroll = Vector2.zero;
            }
            var active = FilterListings(false);
            var listArea = new Rect(55f, 580f, 620f, 1130f);
            uiSkin.DrawPanel(listArea, false);
            marketScroll = GUI.BeginScrollView(new Rect(70f, 600f, 590f, 1090f), marketScroll,
                new Rect(0f, 0f, 560f, Mathf.Max(1070f, active.Count * 126f)), false, true);
            for (var index = 0; index < active.Count; index++)
                DrawMarketListingRow(new Rect(0f, index * 126f, 540f, 116f), active[index], ink, muted, accent);
            GUI.EndScrollView();
            DrawMarketPurchasePanel(new Rect(700f, 580f, 325f, 1130f), ink, muted, accent);
        }

        private void DrawMarketListingRow(Rect area, MarketListing listing,
            Color ink, Color muted, Color accent)
        {
            uiSkin.DrawPanel(area, string.Equals(selectedMarketListingId, listing.ListingId, StringComparison.Ordinal));
            GUI.Label(new Rect(area.x + 18f, area.y + 10f, area.width - 36f, 32f),
                MarketAssetDisplayName(listing), Style(19, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(area.x + 18f, area.y + 45f, area.width - 36f, 28f),
                MarketListingMetadata(listing), Style(14, muted, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 18f, area.y + 77f, 300f, 28f), listing.SellerDisplayName,
                Style(14, muted, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 330f, area.y + 72f, 180f, 34f),
                MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                Style(18, accent, TextAnchor.MiddleRight, FontStyle.Bold));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-select-listing");
                selectedMarketListingId = listing.ListingId;
            }
        }

        private void DrawMarketPurchasePanel(Rect area, Color ink, Color muted, Color accent)
        {
            uiSkin.DrawPanel(area, true);
            GUI.Label(new Rect(area.x + 20f, area.y + 20f, area.width - 40f, 42f), T("market.detail"),
                Style(24, accent, TextAnchor.MiddleLeft, FontStyle.Bold));
            var listing = marketBoard.Find(selectedMarketListingId);
            if (listing == null || listing.State != MarketListingState.Active)
            {
                GUI.Label(new Rect(area.x + 20f, area.y + 90f, area.width - 40f, 150f),
                    T("market.selectHint"), Style(17, muted, TextAnchor.UpperLeft));
                return;
            }
            var payment = MarketRules.CalculateSettlement(listing.PriceMicroSkr);
            GUI.Label(new Rect(area.x + 20f, area.y + 90f, area.width - 40f, 70f),
                MarketAssetDisplayName(listing), Style(21, ink, TextAnchor.UpperLeft, FontStyle.Bold));
            GUI.Label(new Rect(area.x + 20f, area.y + 175f, area.width - 40f, 42f),
                string.Format(T("market.price"), MarketRules.FormatMicroSkr(listing.PriceMicroSkr)),
                Style(16, ink, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 20f, area.y + 220f, area.width - 40f, 42f),
                string.Format(T("market.sellerFee"), MarketRules.FormatMicroSkr(payment.SellerFeeMicroSkr)),
                Style(16, ink, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 20f, area.y + 265f, area.width - 40f, 42f),
                string.Format(T("market.sellerProceeds"), MarketRules.FormatMicroSkr(payment.SellerProceedsMicroSkr)),
                Style(18, accent, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(area.x + 20f, area.y + 325f, area.width - 40f, 110f),
                T("market.feeSplit"), Style(14, muted, TextAnchor.UpperLeft));
            if (GUI.Button(new Rect(area.x + 35f, area.y + 465f, area.width - 70f, 64f),
                    T("market.buy"), ButtonStyle(UiButtonSemantic.Primary, 19))) TryPurchaseMarketListing(listing);
        }

        private void DrawMarketSell(Color ink, Color muted, Color accent)
        {
            var candidates = BuildMarketSaleCandidates();
            var listArea = new Rect(55f, 500f, 620f, 1210f);
            uiSkin.DrawPanel(listArea, false);
            GUI.Label(new Rect(78f, 515f, 570f, 42f), T("market.sellInventory"),
                Style(23, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
            marketScroll = GUI.BeginScrollView(new Rect(70f, 570f, 590f, 1120f), marketScroll,
                new Rect(0f, 0f, 560f, Mathf.Max(1100f, candidates.Count * 112f)), false, true);
            for (var index = 0; index < candidates.Count; index++)
                DrawMarketSaleCandidate(new Rect(0f, index * 112f, 540f, 102f), candidates[index], ink, muted, accent);
            GUI.EndScrollView();

            var form = new Rect(700f, 500f, 325f, 1210f);
            uiSkin.DrawPanel(form, true);
            GUI.Label(new Rect(720f, 520f, 285f, 42f), T("market.createListing"),
                Style(23, accent, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(720f, 590f, 285f, 90f), SelectedMarketAssetText(),
                Style(17, ink, TextAnchor.UpperLeft));
            GUI.Label(new Rect(720f, 705f, 285f, 36f), T("market.askingPrice"),
                Style(16, muted, TextAnchor.MiddleLeft));
            if (GUI.Button(new Rect(720f, 760f, 70f, 54f), "-1", ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-price-down");
                marketListingPriceSkr = Mathf.Max(MarketRules.MinimumListingPriceSkr, marketListingPriceSkr - 1);
            }
            GUI.Label(new Rect(800f, 760f, 120f, 54f), marketListingPriceSkr + " SKR",
                Style(18, ink, TextAnchor.MiddleCenter, FontStyle.Bold));
            if (GUI.Button(new Rect(930f, 760f, 70f, 54f), "+1", ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-price-up");
                marketListingPriceSkr = Mathf.Min(999999, marketListingPriceSkr + 1);
            }
            GUI.Label(new Rect(720f, 835f, 285f, 145f), T("market.listingWarning"),
                Style(14, muted, TextAnchor.UpperLeft));
            if (GUI.Button(new Rect(730f, 1000f, 265f, 64f), T("market.list"),
                    ButtonStyle(UiButtonSemantic.Primary, 19))) TryCreateMarketListing();
        }

        private sealed class MarketSaleCandidate
        {
            public MarketAssetKind Kind;
            public string InstanceId;
            public string TemplateId;
            public int Quality;
            public string SeasonId;
        }

        private List<MarketSaleCandidate> BuildMarketSaleCandidates()
        {
            var result = new List<MarketSaleCandidate>();
            for (var i = 0; i < equipmentInventory.Instances.Count; i++)
            {
                var item = equipmentInventory.Instances[i];
                if ((item.UsageLocks & EquipmentUsageLock.Market) != 0) continue;
                result.Add(new MarketSaleCandidate { Kind = MarketAssetKind.Equipment,
                    InstanceId = item.EquipmentInstanceId, TemplateId = item.TemplateId,
                    Quality = (int)item.Quality, SeasonId = item.SeasonId });
            }
            for (var i = 0; i < lanternInventory.Instances.Count; i++)
            {
                var item = lanternInventory.Instances[i];
                if ((item.UsageLocks & LanternUsageLock.Market) != 0) continue;
                result.Add(new MarketSaleCandidate { Kind = MarketAssetKind.Lantern,
                    InstanceId = item.LanternInstanceId, TemplateId = item.TemplateId,
                    Quality = (int)item.Quality, SeasonId = item.SeasonId });
            }
            return result;
        }

        private void DrawMarketSaleCandidate(Rect area, MarketSaleCandidate item,
            Color ink, Color muted, Color accent)
        {
            var selected = selectedMarketAssetKind == item.Kind &&
                string.Equals(selectedMarketAssetInstanceId, item.InstanceId, StringComparison.Ordinal);
            uiSkin.DrawPanel(area, selected);
            GUI.Label(new Rect(area.x + 18f, area.y + 10f, area.width - 36f, 34f),
                T(item.TemplateId), Style(19, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(area.x + 18f, area.y + 48f, area.width - 36f, 30f),
                MarketCandidateMetadata(item), Style(14, muted, TextAnchor.MiddleLeft));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-select-asset");
                selectedMarketAssetKind = item.Kind;
                selectedMarketAssetInstanceId = item.InstanceId;
                marketFeedbackKey = "market.status.ready";
            }
        }

        private void DrawMyMarketListings(Color ink, Color muted, Color accent)
        {
            var own = FilterListings(true);
            var panel = new Rect(55f, 500f, 970f, 1210f);
            uiSkin.DrawPanel(panel, false);
            GUI.Label(new Rect(80f, 518f, 900f, 42f),
                string.Format(T("market.mySlots"), marketBoard.CountActiveListings(MarketRules.LocalPlayerId),
                    marketBoard.ListingCapacity), Style(23, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
            marketScroll = GUI.BeginScrollView(new Rect(75f, 580f, 930f, 1100f), marketScroll,
                new Rect(0f, 0f, 900f, Mathf.Max(1080f, own.Count * 126f)), false, true);
            for (var index = 0; index < own.Count; index++)
            {
                var listing = own[index];
                var area = new Rect(0f, index * 126f, 880f, 116f);
                uiSkin.DrawPanel(area, false);
                GUI.Label(new Rect(18f, area.y + 12f, 500f, 34f), MarketAssetDisplayName(listing),
                    Style(19, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
                GUI.Label(new Rect(18f, area.y + 53f, 500f, 30f), MarketListingMetadata(listing),
                    Style(14, muted, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(520f, area.y + 16f, 170f, 38f),
                    MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                    Style(18, accent, TextAnchor.MiddleRight, FontStyle.Bold));
                if (GUI.Button(new Rect(710f, area.y + 28f, 150f, 58f), T("market.cancel"),
                        ButtonStyle(UiButtonSemantic.Secondary, 17))) TryCancelMarketListing(listing);
            }
            GUI.EndScrollView();
        }

        private void DrawMarketHistory(Color ink, Color muted, Color accent)
        {
            var panel = new Rect(55f, 500f, 970f, 1210f);
            uiSkin.DrawPanel(panel, false);
            GUI.Label(new Rect(80f, 518f, 900f, 42f), T("market.historyTitle"),
                Style(23, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
            var rows = new List<MarketListing>();
            for (var i = marketBoard.Listings.Count - 1; i >= 0; i--)
                if (marketBoard.Listings[i].State != MarketListingState.Active) rows.Add(marketBoard.Listings[i]);
            marketScroll = GUI.BeginScrollView(new Rect(75f, 580f, 930f, 1100f), marketScroll,
                new Rect(0f, 0f, 900f, Mathf.Max(1080f, rows.Count * 112f)), false, true);
            for (var index = 0; index < rows.Count; index++)
            {
                var listing = rows[index];
                var area = new Rect(0f, index * 112f, 880f, 102f);
                uiSkin.DrawPanel(area, false);
                GUI.Label(new Rect(18f, area.y + 10f, 500f, 34f), MarketAssetDisplayName(listing),
                    Style(18, ink, TextAnchor.MiddleLeft, FontStyle.Bold));
                GUI.Label(new Rect(18f, area.y + 49f, 500f, 30f),
                    T(listing.State == MarketListingState.Sold ? "market.state.sold" : "market.state.cancelled"),
                    Style(14, muted, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(560f, area.y + 28f, 280f, 38f),
                    MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                    Style(17, accent, TextAnchor.MiddleRight, FontStyle.Bold));
            }
            GUI.EndScrollView();
        }

        private List<MarketListing> FilterListings(bool ownOnly)
        {
            var result = new List<MarketListing>();
            for (var index = 0; index < marketBoard.Listings.Count; index++)
            {
                var listing = marketBoard.Listings[index];
                if (listing.State != MarketListingState.Active) continue;
                var own = string.Equals(listing.SellerId, MarketRules.LocalPlayerId, StringComparison.Ordinal);
                if (ownOnly != own) continue;
                if (!ownOnly && marketAssetFilter == MarketAssetFilter.Equipment &&
                    listing.AssetKind != MarketAssetKind.Equipment) continue;
                if (!ownOnly && marketAssetFilter == MarketAssetFilter.Lantern &&
                    listing.AssetKind != MarketAssetKind.Lantern) continue;
                result.Add(listing);
            }
            return result;
        }

        private string MarketAssetDisplayName(MarketListing listing)
        {
            return listing == null ? string.Empty : T(listing.TemplateId);
        }

        private string MarketListingMetadata(MarketListing listing)
        {
            return T(listing.AssetKind == MarketAssetKind.Equipment ? "market.kind.equipment" : "market.kind.lantern") +
                   " · " + MarketQualityName(listing.AssetKind, listing.Quality) + " · " + listing.SeasonId;
        }

        private string MarketCandidateMetadata(MarketSaleCandidate item)
        {
            return T(item.Kind == MarketAssetKind.Equipment ? "market.kind.equipment" : "market.kind.lantern") +
                   " · " + MarketQualityName(item.Kind, item.Quality) + " · " + item.SeasonId;
        }

        private string MarketQualityName(MarketAssetKind kind, int quality)
        {
            if (kind == MarketAssetKind.Lantern)
                return T("quality." + ((LanternQuality)quality).ToString().ToLowerInvariant());
            return T(EquipmentQualityKey((EquipmentQuality)quality));
        }

        private string SelectedMarketAssetText()
        {
            var candidates = BuildMarketSaleCandidates();
            for (var i = 0; i < candidates.Count; i++)
            {
                var item = candidates[i];
                if (item.Kind == selectedMarketAssetKind && item.InstanceId == selectedMarketAssetInstanceId)
                    return T(item.TemplateId) + "\n" + MarketCandidateMetadata(item);
            }
            return T("market.selectAssetHint");
        }

        private WalletSignatureResult RequestMarketSignature(MarketActionType action,
            MarketListing listing, string assetInstanceId, long priceMicroSkr, long totalMicroSkr,
            string summary)
        {
            var operationId = "market-" + (++marketOperationSerial) + "-" + DateTime.UtcNow.Ticks;
            return walletSignatureGateway.RequestMarketActionSignature(new WalletMarketActionRequest(
                operationId, action, MarketRules.LocalPlayerId, MarketSeasonId,
                listing == null ? string.Empty : listing.ListingId, assetInstanceId,
                priceMicroSkr, totalMicroSkr, marketBoard.StateVersion,
                MarketRules.RulesVersion, summary));
        }

        private bool TryApplyMarketListingPriceInput()
        {
            int parsed;
            if (!int.TryParse(marketListingPriceInput, out parsed) ||
                parsed < MarketRules.MinimumListingPriceSkr || parsed > 999999)
            {
                marketListingPriceSkr = 0;
                marketFeedbackKey = "market.status.invalidListing";
                return false;
            }
            marketListingPriceSkr = parsed;
            marketListingPriceInput = parsed.ToString();
            return true;
        }

        private bool TryCreateMarketListing()
        {
            if (string.IsNullOrEmpty(selectedMarketAssetInstanceId))
            {
                marketFeedbackKey = "market.status.selectAsset";
                return false;
            }
            var candidates = BuildMarketSaleCandidates();
            MarketSaleCandidate selected = null;
            for (var i = 0; i < candidates.Count; i++)
                if (candidates[i].Kind == selectedMarketAssetKind &&
                    candidates[i].InstanceId == selectedMarketAssetInstanceId) selected = candidates[i];
            if (selected == null)
            {
                marketFeedbackKey = "market.status.assetUnavailable";
                return false;
            }
            if (!TryApplyMarketListingPriceInput()) return false;
            var price = MarketRules.ToMicroSkr(marketListingPriceSkr);
            if (!marketBoard.CanCreateListing(MarketRules.LocalPlayerId, selected.Kind, selected.InstanceId, price))
            {
                marketFeedbackKey = marketBoard.CountActiveListings(MarketRules.LocalPlayerId) >= marketBoard.ListingCapacity
                    ? "market.status.slotsFull" : "market.status.invalidListing";
                return false;
            }
            var signature = RequestMarketSignature(MarketActionType.CreateListing, null,
                selected.InstanceId, price, 0L, "list:" + selected.InstanceId + ":" + marketListingPriceSkr);
            if (signature == null || !signature.Approved)
            {
                marketFeedbackKey = "market.status.signatureRejected";
                return false;
            }
            var locked = false;
            if (selected.Kind == MarketAssetKind.Equipment)
            {
                string ignored;
                locked = equipmentInventory.TryBeginMarketListing(selected.InstanceId, out ignored);
                if (locked) ReconcileUnavailableEquipmentSnapshots(true);
            }
            else
            {
                bool clearedActive;
                locked = lanternInventory.TryBeginMarketListing(selected.InstanceId, out clearedActive);
                if (locked)
                {
                    if (clearedActive) chapterOneLighthouse.RecordLanternUnequipped();
                    ReconcileUnavailableLanternSnapshots();
                }
            }
            MarketListing created;
            if (!locked || !marketBoard.TryCreateListing(MarketRules.LocalPlayerId, T("market.you"),
                    selected.Kind, selected.InstanceId, selected.TemplateId, selected.Quality,
                    selected.SeasonId, price, DateTime.UtcNow.Ticks, out created))
            {
                if (locked)
                {
                    if (selected.Kind == MarketAssetKind.Equipment) equipmentInventory.TryCancelMarketListing(selected.InstanceId);
                    else lanternInventory.TryCancelMarketListing(selected.InstanceId);
                }
                marketFeedbackKey = "market.status.invalidListing";
                return false;
            }
            PlayUiClick(UiAudioCueIds.Confirm, "market-create-listing");
            marketFeedbackKey = "market.status.listed";
            selectedMarketAssetInstanceId = null;
            marketListingPriceSkr = 0;
            marketListingPriceInput = string.Empty;
            SaveGame();
            return true;
        }

        private void TryCancelMarketListing(MarketListing listing)
        {
            if (listing == null || listing.State != MarketListingState.Active) return;
            var signature = RequestMarketSignature(MarketActionType.CancelListing, listing,
                listing.AssetInstanceId, listing.PriceMicroSkr, 0L, "cancel:" + listing.ListingId);
            if (signature == null || !signature.Approved)
            {
                marketFeedbackKey = "market.status.signatureRejected";
                return;
            }
            var unlocked = listing.AssetKind == MarketAssetKind.Equipment
                ? equipmentInventory.TryCancelMarketListing(listing.AssetInstanceId)
                : lanternInventory.TryCancelMarketListing(listing.AssetInstanceId);
            if (!unlocked || !marketBoard.TryCancelListing(MarketRules.LocalPlayerId,
                    listing.ListingId, DateTime.UtcNow.Ticks))
            {
                marketFeedbackKey = "market.status.assetUnavailable";
                return;
            }
            PlayUiClick(UiAudioCueIds.Cancel, "market-cancel-listing");
            marketFeedbackKey = "market.status.cancelled";
            SaveGame();
        }

        private void TryPurchaseMarketListing(MarketListing listing)
        {
            if (listing == null || listing.State != MarketListingState.Active) return;
            if (!WarehouseStorageRules.CanAccept(equipmentInventory.Instances.Count, 1,
                    lanternInventory.Instances.Count))
            {
                marketFeedbackKey = "market.status.warehouseFull";
                return;
            }
            var payment = MarketRules.CalculateSettlement(listing.PriceMicroSkr);
            if (WalletBalanceMicroSkr < payment.BuyerTotalMicroSkr)
            {
                marketFeedbackKey = "market.status.insufficient";
                return;
            }
            var signature = RequestMarketSignature(MarketActionType.Purchase, listing,
                listing.AssetInstanceId, listing.PriceMicroSkr, payment.BuyerTotalMicroSkr,
                "buy:" + listing.ListingId + ":" + MarketRules.FormatMicroSkr(payment.BuyerTotalMicroSkr));
            if (signature == null || !signature.Approved)
            {
                marketFeedbackKey = "market.status.signatureRejected";
                return;
            }
            long updatedBalance;
            MarketTradeReceipt receipt;
            var transferAttempted = false;
            if (!marketBoard.TryPurchase(MarketRules.LocalPlayerId, listing.ListingId,
                    WalletBalanceMicroSkr, DateTime.UtcNow.Ticks,
                    pendingReceipt =>
                    {
                        transferAttempted = true;
                        return listing.AssetKind == MarketAssetKind.Equipment
                            ? equipmentInventory.TryAddFirst(new EquipmentInstanceState(
                                "market.buy." + pendingReceipt.ReceiptId, listing.TemplateId,
                                (EquipmentQuality)listing.Quality, listing.SeasonId))
                            : lanternInventory.TryAddFirst(CreatePurchasedLantern(
                                listing, pendingReceipt.ReceiptId));
                    }, out updatedBalance, out receipt))
            {
                marketFeedbackKey = transferAttempted
                    ? "market.status.transferFailed"
                    : "market.status.assetUnavailable";
                return;
            }
            SetWalletBalanceMicroSkr(updatedBalance);
            PlayUiClick(UiAudioCueIds.Confirm, "market-purchase");
            marketFeedbackKey = "market.status.purchased";
            pendingMarketWarehouseFocusKind = listing.AssetKind;
            pendingMarketWarehouseFocusInstanceId = "market.buy." + receipt.ReceiptId;
            selectedMarketListingId = null;
            SaveGame();
        }

        private static LanternInstanceState CreatePurchasedLantern(MarketListing listing, string receiptId)
        {
            var representative = FirstChapterLanternEconomyRules.CreateConfirmedRepresentative((LanternQuality)listing.Quality);
            return new LanternInstanceState("market.buy." + receiptId, listing.TemplateId,
                representative.Quality, listing.SeasonId, representative.SourceChapterId,
                representative.SourceDifficultyId, representative.CoreEffectId,
                representative.ModifierRolls, receiptId, representative.RulesVersion,
                LanternUsageLock.None, representative.BaseFlameStatId,
                representative.BaseFlameValue, representative.ContractStage);
        }

        private bool TryLeaseMarketShop()
        {
            if (marketBoard.ShopLeaseCount >= MarketRules.MaximumShopLeasePurchasesPerSeason)
            {
                marketFeedbackKey = "market.status.leaseLimit";
                return false;
            }
            var price = MarketRules.ToMicroSkr(MarketRules.ShopLeasePriceSkr);
            if (WalletBalanceMicroSkr < price)
            {
                marketFeedbackKey = "market.status.insufficient";
                return false;
            }
            var signature = RequestMarketSignature(MarketActionType.LeaseShop, null,
                "market-shop", price, price, "lease-shop:" + MarketRules.ShopLeasePriceSkr);
            if (signature == null || !signature.Approved)
            {
                marketFeedbackKey = "market.status.signatureRejected";
                return false;
            }
            long updated;
            if (!marketBoard.TryLeaseShop(WalletBalanceMicroSkr, out updated))
            {
                marketFeedbackKey = marketBoard.ShopLeaseCount >=
                    MarketRules.MaximumShopLeasePurchasesPerSeason
                    ? "market.status.leaseLimit"
                    : "market.status.insufficient";
                return false;
            }
            SetWalletBalanceMicroSkr(updated);
            PlayUiClick(UiAudioCueIds.Confirm, "market-lease-shop");
            marketFeedbackKey = "market.status.leased";
            SaveGame();
            return true;
        }

        private PrototypeMarketListingSaveData[] SaveMarketListings()
        {
            var saved = new PrototypeMarketListingSaveData[marketBoard.Listings.Count];
            for (var i = 0; i < saved.Length; i++)
            {
                var listing = marketBoard.Listings[i];
                saved[i] = new PrototypeMarketListingSaveData
                {
                    listingId = listing.ListingId, sellerId = listing.SellerId,
                    sellerDisplayName = listing.SellerDisplayName, assetKind = (int)listing.AssetKind,
                    assetInstanceId = listing.AssetInstanceId, templateId = listing.TemplateId,
                    quality = listing.Quality, seasonId = listing.SeasonId,
                    priceMicroSkr = listing.PriceMicroSkr, createdAtUtcTicks = listing.CreatedAtUtcTicks,
                    state = (int)listing.State, buyerId = listing.BuyerId,
                    resolvedAtUtcTicks = listing.ResolvedAtUtcTicks
                };
            }
            return saved;
        }

        private PrototypeMarketReceiptSaveData[] SaveMarketReceipts()
        {
            var saved = new PrototypeMarketReceiptSaveData[marketBoard.Receipts.Count];
            for (var i = 0; i < saved.Length; i++)
            {
                var receipt = marketBoard.Receipts[i];
                saved[i] = new PrototypeMarketReceiptSaveData
                {
                    receiptId = receipt.ReceiptId, listingId = receipt.ListingId,
                    sellerId = receipt.SellerId, buyerId = receipt.BuyerId,
                    sellerProceedsMicroSkr = receipt.SellerProceedsMicroSkr,
                    sellerFeeMicroSkr = receipt.SellerFeeMicroSkr, burnMicroSkr = receipt.BurnMicroSkr,
                    teamMicroSkr = receipt.TeamMicroSkr, buyerTotalMicroSkr = receipt.BuyerTotalMicroSkr,
                    completedAtUtcTicks = receipt.CompletedAtUtcTicks
                };
            }
            return saved;
        }

        private void RestoreMarketState(PrototypeRunSaveData data)
        {
            var listings = new List<MarketListing>();
            if (data.marketListings != null)
            {
                for (var i = 0; i < data.marketListings.Length; i++)
                {
                    var saved = data.marketListings[i];
                    if (saved == null || string.IsNullOrEmpty(saved.listingId)) continue;
                    listings.Add(new MarketListing(saved.listingId, saved.sellerId,
                        saved.sellerDisplayName, (MarketAssetKind)saved.assetKind,
                        saved.assetInstanceId, saved.templateId, saved.quality, saved.seasonId,
                        saved.priceMicroSkr, saved.createdAtUtcTicks, (MarketListingState)saved.state,
                        saved.buyerId, saved.resolvedAtUtcTicks));
                }
            }
            var receipts = new List<MarketTradeReceipt>();
            if (data.marketReceipts != null)
            {
                for (var i = 0; i < data.marketReceipts.Length; i++)
                {
                    var saved = data.marketReceipts[i];
                    if (saved == null || string.IsNullOrEmpty(saved.receiptId)) continue;
                    MarketFeeBreakdown payment;
                    if (data.saveVersion >= 39)
                    {
                        payment = new MarketFeeBreakdown(saved.sellerProceedsMicroSkr,
                            saved.sellerFeeMicroSkr, saved.burnMicroSkr, saved.teamMicroSkr,
                            saved.buyerTotalMicroSkr);
                    }
                    else
                    {
                        MarketListing legacyListing = null;
                        for (var listingIndex = 0; listingIndex < listings.Count; listingIndex++)
                        {
                            if (!string.Equals(listings[listingIndex].ListingId, saved.listingId,
                                    StringComparison.Ordinal)) continue;
                            legacyListing = listings[listingIndex];
                            break;
                        }
                        var legacyPrice = legacyListing == null
                            ? saved.sellerProceedsMicroSkr
                            : legacyListing.PriceMicroSkr;
                        payment = MarketRules.CalculateSettlement(legacyPrice);
                    }
                    receipts.Add(new MarketTradeReceipt(saved.receiptId, saved.listingId,
                        saved.sellerId, saved.buyerId, payment, saved.completedAtUtcTicks));
                }
            }
            var currentSeasonLeaseCount = string.Equals(data.marketLeaseSeasonId, MarketSeasonId,
                StringComparison.Ordinal) ? data.marketShopLeaseCount : 0;
            marketBoard.Restore(listings, receipts, currentSeasonLeaseCount,
                data.marketStateVersion, data.marketBurnPoolMicroSkr,
                data.marketTeamRevenueMicroSkr, data.marketListingSerial, data.marketReceiptSerial);
            marketSeeded = data.marketSeeded;
            if (!marketSeeded) EnsureMarketState();
        }
    }
}
