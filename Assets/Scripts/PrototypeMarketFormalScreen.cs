using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private const string MarketFormalModuleRoot = "Art/Market/V100/modules/";

        private readonly Dictionary<string, Texture2D> marketFormalTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private bool marketEquipmentFilterOpen;
        private MarketEquipmentTypeFilter marketEquipmentTypeFilter;
        private MarketSortMode marketSortMode;

        private enum MarketEquipmentTypeFilter { All, Weapon, Head, Body, Hands, Feet }
        private enum MarketSortMode { Default, PriceAscending, PriceDescending, Latest }

        private static readonly Color MarketInk = new Color32(31, 52, 55, 255);
        // Sampled from the accepted Equipment Detail A title bar at native pixel (380, 50).
        // Use only on light detail surfaces; title-bar text keeps its accepted ivory treatment.
        private static readonly Color DetailBodyInk = new Color32(13, 62, 75, 255);
        private static readonly Color MarketIvory = new Color32(245, 232, 190, 255);
        private static readonly Color MarketMuted = new Color32(84, 101, 96, 255);
        private static readonly Color MarketGold = new Color32(173, 119, 37, 255);
        private static readonly Color MarketTeal = new Color32(39, 129, 119, 255);

        private void DrawMarketScreen()
        {
            EnsureMarketState();
            GrantMarketAcceptanceInventoryIfNeeded();

            var modalOpen = IsMarketModalOpenFormal();
            var scrollBeforeModalFrame = marketScroll;
            var previousEnabled = GUI.enabled;
            if (modalOpen) GUI.enabled = false;

            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f),
                MarketFormalModuleRoot, "ART-UI-MARKET-B-BACKGROUND-1080x2400-v1.0.0");
            DrawMarketHeaderFormal();
            DrawMarketTabsFormal();

            switch (marketPageTab)
            {
                case MarketPageTab.Sell:
                    DrawMarketSellFormal();
                    break;
                case MarketPageTab.Mine:
                    DrawMyMarketListingsFormal();
                    break;
                case MarketPageTab.History:
                    DrawMarketHistoryFormal();
                    break;
                default:
                    DrawMarketBrowseFormal();
                    break;
            }

            GUI.enabled = previousEnabled;
            if (modalOpen) marketScroll = scrollBeforeModalFrame;

            if (!string.IsNullOrEmpty(selectedMarketListingId) ||
                !string.IsNullOrEmpty(selectedMarketPreviewAssetInstanceId))
                DrawMarketDetailModalFormal();
            else if (marketShelfExpansionConfirmOpen)
                DrawMarketShelfExpansionConfirmFormal();
        }

        private bool IsMarketModalOpenFormal()
        {
            return marketShelfExpansionConfirmOpen ||
                !string.IsNullOrEmpty(selectedMarketListingId) ||
                !string.IsNullOrEmpty(selectedMarketPreviewAssetInstanceId);
        }

        private void PrepareMarketModalFormal()
        {
            marketEquipmentFilterOpen = false;
            lanternDetailCovenantPage = false;
            warehouseLanternStagePopup = -1;
            GUI.FocusControl(string.Empty);
            GUIUtility.keyboardControl = 0;
        }

        private void DrawMarketModalInputShieldFormal(Rect modalRect)
        {
            DrawMarketModalShieldBandFormal(new Rect(0f, 0f, 1080f, modalRect.y));
            DrawMarketModalShieldBandFormal(new Rect(0f, modalRect.y, modalRect.x, modalRect.height));
            DrawMarketModalShieldBandFormal(new Rect(modalRect.xMax, modalRect.y,
                1080f - modalRect.xMax, modalRect.height));
            DrawMarketModalShieldBandFormal(new Rect(0f, modalRect.yMax, 1080f,
                2400f - modalRect.yMax));
        }

        private static void DrawMarketModalShieldBandFormal(Rect area)
        {
            if (area.width <= 0f || area.height <= 0f) return;
            GUI.Button(area, GUIContent.none, GUIStyle.none);
        }

        private void CloseMarketModalFormal()
        {
            selectedMarketListingId = null;
            selectedMarketPreviewAssetInstanceId = null;
            marketShelfExpansionConfirmOpen = false;
            marketModalSubmissionInProgress = false;
            lanternDetailCovenantPage = false;
            warehouseLanternStagePopup = -1;
            GUI.FocusControl(string.Empty);
            GUIUtility.keyboardControl = 0;
            if (Event.current != null) Event.current.Use();
        }

        private void DrawMarketHeaderFormal()
        {
            GUI.Label(new Rect(35f, 22f, 240f, 48f), "‹ " + T("market.backToHarbor"),
                MarketTextStyle(24, MarketIvory, TextAnchor.MiddleLeft, true));
            if (GUI.Button(new Rect(25f, 12f, 270f, 68f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "market-back-harbor");
                currentView = ScreenView.Harbor;
                return;
            }

            GUI.Label(new Rect(55f, 74f, 430f, 84f), T("market.title"),
                MarketTextStyle(54, MarketIvory, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(56f, 151f, 520f, 38f), T("market.subtitle"),
                MarketTextStyle(22, MarketIvory, TextAnchor.MiddleLeft, false));

            DrawMarketTextureFormal(new Rect(765f, 72f, 280f, 70f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-BALANCE-FLAT-280x70-v1.1.0");
            GUI.Label(new Rect(785f, 72f, 240f, 70f),
                string.Format(T("market.balance"), WalletBalanceText()),
                MarketTextStyle(24, MarketIvory, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(860f, 12f, 185f, 30f), "v" + ClientVersion,
                MarketTextStyle(14, MarketIvory, TextAnchor.MiddleRight, false));
        }

        private void DrawMarketTabsFormal()
        {
            var keys = new[] { "market.tab.browse", "market.tab.sell", "market.tab.mine", "market.tab.history" };
            var icons = new[]
            {
                "ART-UI-MARKET-B-TAB-ICON-BUY-38-v1.2.0",
                "ART-UI-MARKET-B-TAB-ICON-SELL-38-v1.2.0",
                "ART-UI-MARKET-B-TAB-ICON-ORDERS-38-v1.2.0",
                "ART-UI-MARKET-B-TAB-ICON-HISTORY-38-v1.2.0"
            };
            for (var index = 0; index < keys.Length; index++)
            {
                var area = new Rect(30f + index * 252f, 310f, 232f, 72f);
                var selected = (int)marketPageTab == index;
                DrawMarketTextureFormal(area, MarketFormalModuleRoot, selected
                    ? "ART-UI-MARKET-B-TAB-ICON-SELECTED-232x72-v1.2.0"
                    : "ART-UI-MARKET-B-TAB-ICON-NORMAL-232x72-v1.2.0");
                DrawMarketTextureFormal(new Rect(area.x + 20f, area.y + 17f, 38f, 38f),
                    MarketFormalModuleRoot, icons[index]);
                GUI.Label(new Rect(area.x + 66f, area.y, 148f, area.height), T(keys[index]),
                    MarketTextStyle(22, selected ? MarketIvory : MarketInk,
                        TextAnchor.MiddleCenter, selected));
                if (!GUI.Button(area, GUIContent.none, GUIStyle.none) || selected) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "market-tab");
                marketPageTab = (MarketPageTab)index;
                marketScroll = Vector2.zero;
                selectedMarketListingId = null;
                selectedMarketPreviewAssetInstanceId = null;
                selectedMarketAssetInstanceId = null;
                marketListingPriceSkr = 0;
                marketListingPriceInput = string.Empty;
                marketEquipmentFilterOpen = false;
                marketFeedbackKey = "market.status.ready";
            }

            if (marketFeedbackKey != "market.status.ready")
                GUI.Label(new Rect(35f, 382f, 1010f, 24f), T(marketFeedbackKey),
                    MarketTextStyle(15, MarketMuted, TextAnchor.MiddleCenter, false));
        }

        private void DrawMarketBrowseFormal()
        {
            DrawMarketFiltersFormal(true, 405f);
            var active = FilterAndSortMarketListingsFormal(false);
            const float viewportTop = 485f;
            const float viewportBottom = 2130f;
            var contentHeight = Mathf.Max(viewportBottom - viewportTop,
                Mathf.Ceil(active.Count / 2f) * 160f);
            marketScroll = GUI.BeginScrollView(new Rect(20f, viewportTop, 1040f, viewportBottom - viewportTop),
                marketScroll, new Rect(0f, 0f, 1020f, contentHeight), false, false);
            for (var index = 0; index < active.Count; index++)
            {
                var x = 10f + index % 2 * 510f;
                var y = index / 2 * 160f;
                DrawMarketListingCardFormal(new Rect(x, y, 480f, 150f), active[index], false, false);
            }
            GUI.EndScrollView();
            if (marketEquipmentFilterOpen)
                DrawMarketEquipmentDropdownFormal(471f);
        }

        private void DrawMarketSellFormal()
        {
            DrawMarketFiltersFormal(false, 405f);
            var candidates = FilterMarketSaleCandidatesFormal();
            const float viewportTop = 485f;
            const float viewportBottom = 2130f;
            var contentHeight = Mathf.Max(viewportBottom - viewportTop,
                Mathf.Ceil(candidates.Count / 2f) * 160f);
            marketScroll = GUI.BeginScrollView(new Rect(20f, viewportTop, 1040f, viewportBottom - viewportTop),
                marketScroll, new Rect(0f, 0f, 1020f, contentHeight), false, false);
            for (var index = 0; index < candidates.Count; index++)
            {
                var x = 10f + index % 2 * 510f;
                var y = index / 2 * 160f;
                DrawMarketSaleCandidateFormal(new Rect(x, y, 480f, 150f), candidates[index]);
            }
            GUI.EndScrollView();
        }

        private void DrawMyMarketListingsFormal()
        {
            var used = marketBoard.CountActiveListings(MarketRules.LocalPlayerId);
            DrawMarketTextureFormal(new Rect(55f, 405f, 970f, 108f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-SHOP-SHELF-TWO-ZONE-970x108-v1.3.7");
            GUI.Label(new Rect(85f, 415f, 220f, 78f), T("market.shelf"),
                MarketTextStyle(24, MarketInk, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(292f, 414f, 330f, 44f), used + " / " + marketBoard.ListingCapacity,
                MarketTextStyle(31, MarketGold, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(292f, 458f, 330f, 30f), T("market.shelfUsage"),
                MarketTextStyle(15, MarketMuted, TextAnchor.MiddleCenter, false));
            var canExpand = marketBoard.ShopLeaseCount <
                MarketRules.MaximumShopLeasePurchasesPerSeason;
            if (DrawMarketPrimaryButtonFormal(new Rect(735f, 424f, 250f, 70f),
                    T("market.expandShelf") + " · " + MarketRules.ShopLeasePriceSkr + " SKR", canExpand))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-open-shelf-expansion");
                selectedMarketListingId = null;
                selectedMarketPreviewAssetInstanceId = null;
                marketShelfExpansionConfirmOpen = true;
                marketFeedbackKey = "market.status.ready";
                PrepareMarketModalFormal();
                if (Event.current != null) Event.current.Use();
            }

            DrawMarketFiltersFormal(false, 535f);
            var own = FilterAndSortMarketListingsFormal(true);
            const float viewportTop = 615f;
            const float viewportBottom = 2130f;
            var contentHeight = Mathf.Max(viewportBottom - viewportTop,
                Mathf.Ceil(own.Count / 2f) * 160f);
            marketScroll = GUI.BeginScrollView(new Rect(20f, viewportTop, 1040f, viewportBottom - viewportTop),
                marketScroll, new Rect(0f, 0f, 1020f, contentHeight), false, false);
            for (var index = 0; index < own.Count; index++)
            {
                var x = 10f + index % 2 * 510f;
                var y = index / 2 * 160f;
                DrawMarketListingCardFormal(new Rect(x, y, 480f, 150f), own[index], true, true);
            }
            GUI.EndScrollView();
        }

        private void DrawMarketHistoryFormal()
        {
            DrawMarketFiltersFormal(false, 405f);
            var rows = BuildMarketHistoryRowsFormal();
            var headings = new[]
            {
                "market.history.product", "market.history.status", "market.history.time",
                "market.history.price", "market.history.order"
            };
            var widths = new[] { 290f, 130f, 170f, 150f, 230f };
            var x = 55f;
            for (var i = 0; i < headings.Length; i++)
            {
                GUI.Label(new Rect(x, 488f, widths[i], 44f), T(headings[i]),
                    MarketTextStyle(17, MarketMuted, TextAnchor.MiddleCenter, true));
                x += widths[i];
            }
            DrawMarketTextureFormal(new Rect(58f, 534f, 964f, 2f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-RECORD-HEADER-DIVIDER-964x2-v1.0.0");

            const float viewportTop = 548f;
            const float viewportBottom = 2130f;
            var contentHeight = Mathf.Max(viewportBottom - viewportTop, rows.Count * 148f);
            marketScroll = GUI.BeginScrollView(new Rect(45f, viewportTop, 990f, viewportBottom - viewportTop),
                marketScroll, new Rect(0f, 0f, 970f, contentHeight), false, false);
            for (var index = 0; index < rows.Count; index++)
                DrawMarketHistoryRowFormal(new Rect(10f, index * 148f, 970f, 132f), rows[index]);
            GUI.EndScrollView();
        }

        private void DrawMarketFiltersFormal(bool allowEquipmentDropdown, float y)
        {
            DrawMarketFilterButtonFormal(new Rect(40f, y, 180f, 60f), T("market.filter.all"),
                marketAssetFilter == MarketAssetFilter.All, () => SetMarketFilterFormal(MarketAssetFilter.All));
            var equipmentLabel = marketEquipmentTypeFilter == MarketEquipmentTypeFilter.All
                ? T("market.filter.equipment") : T(MarketEquipmentFilterKeyFormal(marketEquipmentTypeFilter));
            DrawMarketFilterButtonFormal(new Rect(230f, y, 180f, 60f), equipmentLabel,
                marketAssetFilter == MarketAssetFilter.Equipment, () =>
                {
                    if (allowEquipmentDropdown && marketAssetFilter == MarketAssetFilter.Equipment)
                        marketEquipmentFilterOpen = !marketEquipmentFilterOpen;
                    else
                    {
                        SetMarketFilterFormal(MarketAssetFilter.Equipment);
                        marketEquipmentFilterOpen = allowEquipmentDropdown;
                    }
                });
            if (allowEquipmentDropdown)
                DrawMarketTextureFormal(new Rect(376f, y + 24f, 18f, 12f), MarketFormalModuleRoot,
                    "ART-UI-MARKET-B-DROPDOWN-TRIANGLE-18x12-v1.2.0");
            DrawMarketFilterButtonFormal(new Rect(420f, y, 180f, 60f), T("market.filter.lantern"),
                marketAssetFilter == MarketAssetFilter.Lantern, () => SetMarketFilterFormal(MarketAssetFilter.Lantern));

            DrawMarketTextureFormal(new Rect(790f, y, 250f, 60f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-SORT-FLAT-250x60-v1.1.0");
            GUI.Label(new Rect(805f, y, 220f, 60f), T(MarketSortKeyFormal()),
                MarketTextStyle(18, MarketInk, TextAnchor.MiddleCenter, false));
            if (GUI.Button(new Rect(790f, y, 250f, 60f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.TabSwitch, "market-sort");
                marketSortMode = (MarketSortMode)(((int)marketSortMode + 1) % 4);
                marketScroll = Vector2.zero;
            }

        }

        private void DrawMarketEquipmentDropdownFormal(float y)
        {
            var values = new[]
            {
                MarketEquipmentTypeFilter.All, MarketEquipmentTypeFilter.Weapon,
                MarketEquipmentTypeFilter.Head, MarketEquipmentTypeFilter.Body,
                MarketEquipmentTypeFilter.Hands, MarketEquipmentTypeFilter.Feet
            };
            for (var index = 0; index < values.Length; index++)
            {
                var value = values[index];
                var area = new Rect(230f, y + index * 60f, 180f, 60f);
                DrawMarketTextureFormal(area, MarketFormalModuleRoot,
                    value == marketEquipmentTypeFilter
                        ? "ART-UI-MARKET-B-FILTER-FLAT-SELECTED-180x60-v1.1.0"
                        : "ART-UI-MARKET-B-FILTER-FLAT-NORMAL-180x60-v1.1.0");
                GUI.Label(area, T(MarketEquipmentFilterKeyFormal(value)),
                    MarketTextStyle(17, value == marketEquipmentTypeFilter ? MarketIvory : MarketInk,
                        TextAnchor.MiddleCenter, value == marketEquipmentTypeFilter));
                if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "market-equipment-filter");
                marketAssetFilter = MarketAssetFilter.Equipment;
                marketEquipmentTypeFilter = value;
                marketEquipmentFilterOpen = false;
                marketScroll = Vector2.zero;
            }
        }

        private void DrawMarketListingCardFormal(Rect area, MarketListing listing, bool own, bool activePill)
        {
            var selected = string.Equals(selectedMarketListingId, listing.ListingId, StringComparison.Ordinal);
            DrawMarketTextureFormal(area, MarketFormalModuleRoot, selected
                ? "ART-UI-MARKET-B-LISTING-CLEAN-SELECTED-480x150-v1.2.0"
                : "ART-UI-MARKET-B-LISTING-CLEAN-NORMAL-480x150-v1.2.0");
            DrawMarketItemLayersFormal(new Rect(area.x + 14f, area.y + 23f, 104f, 104f),
                listing.AssetKind, listing.TemplateId, listing.Quality, false);
            GUI.Label(new Rect(area.x + 132f, area.y + 16f, 320f, 36f), MarketAssetDisplayName(listing),
                MarketTextStyle(21, MarketIvory, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(area.x + 132f, area.y + 51f, 235f, 30f),
                "◆ " + MarketQualityName(listing.AssetKind, listing.Quality),
                MarketTextStyle(16, MarketQualityTextColorFormal(listing.AssetKind, listing.Quality), TextAnchor.MiddleLeft, true));
            GUI.Label(activePill
                    ? new Rect(area.x + 132f, area.y + 98f, 138f, 38f)
                    : new Rect(area.x + 132f, area.y + 98f, 315f, 38f),
                MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                MarketTextStyle(19, MarketGold,
                    activePill ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight, true));
            if (activePill)
            {
                DrawMarketTextureFormal(new Rect(area.x + 280f, area.y + 94f, 104f, 40f),
                    MarketFormalModuleRoot, "ART-UI-MARKET-B-STATUS-GREEN-104x40-v1.0.0");
                GUI.Label(new Rect(area.x + 280f, area.y + 94f, 104f, 40f), T("market.state.active"),
                    MarketTextStyle(15, MarketIvory, TextAnchor.MiddleCenter, true));
            }
            if (!marketEquipmentFilterOpen && GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-open-detail");
                selectedMarketListingId = listing.ListingId;
                selectedMarketPreviewAssetInstanceId = null;
                marketShelfExpansionConfirmOpen = false;
                PrepareMarketModalFormal();
                if (Event.current != null) Event.current.Use();
            }
        }

        private void DrawMarketSaleCandidateFormal(Rect area, MarketSaleCandidate item)
        {
            var selected = selectedMarketAssetKind == item.Kind &&
                string.Equals(selectedMarketAssetInstanceId, item.InstanceId, StringComparison.Ordinal);
            DrawMarketTextureFormal(area, MarketFormalModuleRoot, selected
                ? "ART-UI-MARKET-B-LISTING-CLEAN-SELECTED-480x150-v1.2.0"
                : "ART-UI-MARKET-B-LISTING-CLEAN-NORMAL-480x150-v1.2.0");
            DrawMarketItemLayersFormal(new Rect(area.x + 14f, area.y + 23f, 104f, 104f),
                item.Kind, item.TemplateId, item.Quality, false);
            GUI.Label(new Rect(area.x + 132f, area.y + 16f, 320f, 36f), T(item.TemplateId),
                MarketTextStyle(21, MarketIvory, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(area.x + 132f, area.y + 51f, 235f, 30f),
                "◆ " + MarketQualityName(item.Kind, item.Quality),
                MarketTextStyle(16, MarketQualityTextColorFormal(item.Kind, item.Quality), TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(area.x + 132f, area.y + 98f, 315f, 38f),
                string.Format(T("market.type"), MarketAssetTypeNameFormal(item.Kind, item.TemplateId)),
                MarketTextStyle(16, MarketIvory, TextAnchor.MiddleRight, false));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-select-asset");
                selectedMarketAssetKind = item.Kind;
                selectedMarketAssetInstanceId = item.InstanceId;
                selectedMarketListingId = null;
                selectedMarketPreviewAssetInstanceId = item.InstanceId;
                marketListingPriceSkr = MarketRules.MinimumListingPriceSkr;
                marketListingPriceInput = marketListingPriceSkr.ToString();
                marketFeedbackKey = "market.status.ready";
                marketShelfExpansionConfirmOpen = false;
                PrepareMarketModalFormal();
                if (Event.current != null) Event.current.Use();
            }
        }

        private void DrawMarketHistoryRowFormal(Rect area, MarketListing listing)
        {
            DrawMarketTextureFormal(area, MarketFormalModuleRoot,
                "ART-UI-MARKET-B-RECORD-FIVE-ZONE-970x132-v1.0.0");
            DrawMarketItemLayersFormal(new Rect(area.x + 12f, area.y + 14f, 104f, 104f),
                listing.AssetKind, listing.TemplateId, listing.Quality, false);
            GUI.Label(new Rect(area.x + 126f, area.y + 14f, 160f, 38f), MarketAssetDisplayName(listing),
                MarketTextStyle(17, MarketIvory, TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(area.x + 126f, area.y + 53f, 160f, 32f),
                "◆ " + MarketQualityName(listing.AssetKind, listing.Quality),
                MarketTextStyle(14, MarketQualityTextColorFormal(listing.AssetKind, listing.Quality), TextAnchor.MiddleLeft, true));

            var cancelled = listing.State == MarketListingState.Cancelled;
            var bought = listing.State == MarketListingState.Sold &&
                string.Equals(listing.BuyerId, MarketRules.LocalPlayerId, StringComparison.Ordinal);
            var statusKey = cancelled ? "market.state.cancelled" : bought ? "market.state.bought" : "market.state.soldByMe";
            GUI.Label(new Rect(area.x + 290f, area.y, 130f, area.height), T(statusKey),
                MarketTextStyle(17, cancelled ? MarketMuted : MarketTeal, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(area.x + 420f, area.y, 170f, area.height),
                cancelled ? "—" : MarketHistoryTimeFormal(listing.ResolvedAtUtcTicks),
                MarketTextStyle(15, MarketIvory, TextAnchor.MiddleCenter, false));
            GUI.Label(new Rect(area.x + 590f, area.y, 150f, area.height),
                cancelled ? "—" : MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                MarketTextStyle(16, cancelled ? MarketMuted : MarketGold, TextAnchor.MiddleCenter, true));
            if (GUI.Button(new Rect(area.x, area.y, 740f, area.height), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-open-history-detail");
                selectedMarketListingId = listing.ListingId;
                selectedMarketPreviewAssetInstanceId = null;
                marketShelfExpansionConfirmOpen = false;
                PrepareMarketModalFormal();
                if (Event.current != null) Event.current.Use();
            }
            GUI.Label(new Rect(area.x + 740f, area.y, 230f, area.height), listing.ListingId,
                MarketTextStyle(12, MarketIvory, TextAnchor.MiddleCenter, false));
            if (GUI.Button(new Rect(area.x + 740f, area.y, 230f, area.height), GUIContent.none, GUIStyle.none))
            {
                GUIUtility.systemCopyBuffer = listing.ListingId;
                PlayUiClick(UiAudioCueIds.Tap, "market-copy-order-id");
                marketFeedbackKey = "market.status.orderCopied";
            }
        }

        private void DrawMarketDetailModalFormal()
        {
            var listing = marketBoard.Find(selectedMarketListingId);
            var preview = FindMarketSalePreviewCandidateFormal();
            if (listing == null && preview != null)
                listing = new MarketListing("preview-" + preview.InstanceId, MarketRules.LocalPlayerId,
                    T("market.you"), preview.Kind, preview.InstanceId, preview.TemplateId,
                    preview.Quality, preview.SeasonId,
                    MarketRules.ToMicroSkr(MarketRules.MinimumListingPriceSkr), DateTime.UtcNow.Ticks);
            if (listing == null)
            {
                selectedMarketListingId = null;
                selectedMarketPreviewAssetInstanceId = null;
                return;
            }

            if (listing.AssetKind == MarketAssetKind.Equipment)
            {
                DrawMarketEquipmentDetailUniversalFormal(listing, preview);
                return;
            }

            DrawMarketLanternDetailFormal(listing, preview);
        }

        private void DrawMarketLanternDetailFormal(MarketListing listing,
            MarketSaleCandidate preview)
        {
            var instance = ResolveMarketLanternDetailInstance(listing);
            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            var panel = LanternDetailAFormalArtContract.PanelRect;
            DrawMarketModalInputShieldFormal(panel);
            DrawWarehouseLanternDetailAFormal(instance);

            if (preview != null)
                DrawMarketLanternListingActionFormal(panel);
            else
                DrawMarketLanternActiveActionFormal(panel, listing);

            var closeArea = OffsetEquipmentDetailRect(panel, 662f, 22f, 72f, 72f);
            var closeEnabled = !marketModalSubmissionInProgress && warehouseLanternStagePopup < 0;
            if (DrawLanternDetailAStateButton(closeArea, SettingsFormalArtContract.ModuleRoot,
                    SettingsFormalArtContract.CloseNormal, SettingsFormalArtContract.ClosePressed,
                    SettingsFormalArtContract.CloseDisabled, closeEnabled))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "market-close-lantern-detail");
                CloseMarketModalFormal();
            }
            if (warehouseLanternStagePopup >= 0)
                DrawWarehouseLanternCovenantPopup(panel, instance, warehouseLanternStagePopup);
        }

        private LanternInstanceState ResolveMarketLanternDetailInstance(MarketListing listing)
        {
            var owned = FindWarehouseLantern(listing.AssetInstanceId);
            if (owned != null) return owned;
            var quality = (LanternQuality)Mathf.Clamp(listing.Quality,
                (int)LanternQuality.Common, (int)LanternQuality.Legendary);
            var representative = FirstChapterLanternEconomyRules.CreateConfirmedRepresentative(quality);
            return new LanternInstanceState(listing.AssetInstanceId, listing.TemplateId, quality,
                listing.SeasonId, representative.SourceChapterId, representative.SourceDifficultyId,
                representative.CoreEffectId, representative.ModifierRolls,
                representative.RewardReceiptId, representative.RulesVersion,
                LanternUsageLock.None, representative.BaseFlameStatId,
                representative.BaseFlameValue, representative.ContractStage);
        }

        private void DrawMarketLanternListingActionFormal(Rect panel)
        {
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 892f, 100f, 54f),
                T("market.askingPrice"), MarketTextStyle(16, DetailBodyInk, TextAnchor.MiddleLeft, true));
            var previousEnabled = GUI.enabled;
            GUI.enabled = !marketModalSubmissionInProgress;
            if (DrawMarketNudgeFormal(OffsetEquipmentDetailRect(panel, 150f, 899f, 54f, 54f), "−"))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-lantern-price-down");
                int parsed;
                if (!int.TryParse(marketListingPriceInput, out parsed))
                    parsed = MarketRules.MinimumListingPriceSkr;
                marketListingPriceSkr = Mathf.Max(MarketRules.MinimumListingPriceSkr, parsed - 1);
                marketListingPriceInput = marketListingPriceSkr.ToString();
            }
            var inputArea = OffsetEquipmentDetailRect(panel, 212f, 899f, 144f, 54f);
            DrawMarketTextureFormal(inputArea, MarketFormalModuleRoot,
                "ART-UI-MARKET-B-PRICE-INPUT-144x54-v1.3.6");
            GUI.SetNextControlName("market.lantern.price");
            var edited = GUI.TextField(new Rect(inputArea.x + 6f, inputArea.y,
                    98f, inputArea.height), marketListingPriceInput, 6,
                MarketPriceInputStyleFormal(true));
            GUI.Label(new Rect(inputArea.x + 102f, inputArea.y, 38f, inputArea.height), "SKR",
                MarketTextStyle(13, DetailBodyInk, TextAnchor.MiddleLeft, false));
            if (!string.Equals(edited, marketListingPriceInput, StringComparison.Ordinal))
            {
                marketListingPriceInput = MarketDigitsOnlyFormal(edited);
                int parsed;
                marketListingPriceSkr = int.TryParse(marketListingPriceInput, out parsed) ? parsed : 0;
            }
            if (DrawMarketNudgeFormal(OffsetEquipmentDetailRect(panel, 364f, 899f, 54f, 54f), "+"))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-lantern-price-up");
                int parsed;
                if (!int.TryParse(marketListingPriceInput, out parsed))
                    parsed = MarketRules.MinimumListingPriceSkr - 1;
                marketListingPriceSkr = Mathf.Clamp(parsed + 1,
                    MarketRules.MinimumListingPriceSkr, 999999);
                marketListingPriceInput = marketListingPriceSkr.ToString();
            }
            GUI.enabled = previousEnabled;
            int validPrice;
            var canList = !marketModalSubmissionInProgress &&
                warehouseLanternStagePopup < 0 &&
                int.TryParse(marketListingPriceInput, out validPrice) &&
                validPrice >= MarketRules.MinimumListingPriceSkr && validPrice <= 999999;
            var actionArea = OffsetEquipmentDetailRect(panel, 485f, 892f, 220f, 68f);
            var clicked = DrawLanternDetailAStateButton(actionArea,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.MediumNormal,
                GlobalButtonFormalArtContract.MediumPressed,
                GlobalButtonFormalArtContract.MediumDisabled, canList);
            GUI.Label(actionArea, T("market.createListing"),
                MarketTextStyle(22, canList ? MarketIvory : MarketMuted, TextAnchor.MiddleCenter, true));
            if (clicked)
            {
                marketModalSubmissionInProgress = true;
                var created = TryCreateMarketListing();
                marketModalSubmissionInProgress = false;
                if (created) CloseMarketModalFormal();
            }
        }

        private void DrawMarketLanternActiveActionFormal(Rect panel, MarketListing listing)
        {
            var own = string.Equals(listing.SellerId, MarketRules.LocalPlayerId,
                StringComparison.Ordinal);
            var active = listing.State == MarketListingState.Active &&
                marketPageTab != MarketPageTab.History;
            var actionEnabled = !marketModalSubmissionInProgress && warehouseLanternStagePopup < 0;
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 892f, 380f, 68f),
                MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                MarketTextStyle(24, MarketGold, TextAnchor.MiddleLeft, true));
            var actionArea = OffsetEquipmentDetailRect(panel, 485f, 892f, 220f, 68f);
            var actionLabel = !active ? T("close") : own ? T("market.cancel") : T("market.buy");
            var clicked = DrawLanternDetailAStateButton(actionArea,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.MediumNormal,
                GlobalButtonFormalArtContract.MediumPressed,
                GlobalButtonFormalArtContract.MediumDisabled, actionEnabled);
            GUI.Label(actionArea, actionLabel, MarketTextStyle(22,
                actionEnabled ? MarketIvory : MarketMuted, TextAnchor.MiddleCenter, true));
            if (!clicked) return;
            if (!active)
            {
                CloseMarketModalFormal();
                return;
            }
            marketModalSubmissionInProgress = true;
            if (own) TryCancelMarketListing(listing);
            else TryPurchaseMarketListing(listing);
            marketModalSubmissionInProgress = false;
            if (listing.State != MarketListingState.Active) CloseMarketModalFormal();
        }

        private void DrawMarketEquipmentDetailUniversalFormal(MarketListing listing,
            MarketSaleCandidate preview)
        {
            var listingMode = preview != null;
            var modifier = equipmentCatalog.Get(listing.TemplateId).Modifier;
            var equipmentQuality = (EquipmentQuality)Mathf.Clamp(listing.Quality,
                (int)EquipmentQuality.Common, (int)EquipmentQuality.Epic);
            var panel = EquipmentDetailAFormalArtContract.ActionPanelRect;
            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            DrawMarketModalInputShieldFormal(panel);
            DrawEquipmentDetailAContent(panel, true, T("market.detail"), listing.TemplateId,
                equipmentQuality, MarketAssetDisplayName(listing),
                MarketQualityName(listing.AssetKind, listing.Quality),
                MarketAssetTypeNameFormal(listing.AssetKind, listing.TemplateId), modifier);

            if (listingMode)
                DrawMarketEquipmentDensityListingAction(panel);
            else
                DrawMarketEquipmentDensityCommonAction(panel, listing);

            var closeArea = OffsetEquipmentDetailRect(panel, 660f, 20f, 76f, 76f);
            if (!marketModalSubmissionInProgress &&
                GUI.Button(closeArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "market-close-universal-equipment-detail");
                CloseMarketModalFormal();
            }
        }

        private void DrawMarketEquipmentDensityCommonAction(Rect panel, MarketListing listing)
        {
            var own = string.Equals(listing.SellerId, MarketRules.LocalPlayerId, StringComparison.Ordinal);
            var actionable = listing.State == MarketListingState.Active &&
                !marketModalSubmissionInProgress;
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 552f, 280f, 68f),
                (own ? T("market.listedPrice") + " " : string.Empty) +
                MarketRules.FormatMicroSkr(listing.PriceMicroSkr) + " SKR",
                MarketTextStyle(22, MarketGold, TextAnchor.MiddleLeft, true));
            var actionArea = OffsetEquipmentDetailRect(panel, 470f, 552f, 220f, 68f);
            var label = own ? T("market.cancel") : T("market.confirmPurchase");
            var clicked = DrawLanternDetailAStateButton(actionArea,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.MediumNormal,
                GlobalButtonFormalArtContract.MediumPressed,
                GlobalButtonFormalArtContract.MediumDisabled, actionable);
            GUI.Label(actionArea, label, MarketTextStyle(22,
                actionable ? MarketIvory : MarketMuted, TextAnchor.MiddleCenter, true));
            if (!clicked) return;
            marketModalSubmissionInProgress = true;
            if (own) TryCancelMarketListing(listing);
            else TryPurchaseMarketListing(listing);
            marketModalSubmissionInProgress = false;
            if (listing.State != MarketListingState.Active) CloseMarketModalFormal();
        }

        private void DrawMarketEquipmentDensityListingAction(Rect panel)
        {
            GUI.Label(OffsetEquipmentDetailRect(panel, 26f, 568f, 96f, 32f),
                T("market.sellPrice"), MarketTextStyle(16, MarketInk, TextAnchor.MiddleLeft, true));
            var controlsEnabled = !marketModalSubmissionInProgress;
            var previousEnabled = GUI.enabled;
            GUI.enabled = controlsEnabled;
            if (DrawMarketNudgeFormal(OffsetEquipmentDetailRect(panel, 126f, 559f, 54f, 54f), "−"))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-equipment-price-down");
                int parsed;
                if (!int.TryParse(marketListingPriceInput, out parsed))
                    parsed = MarketRules.MinimumListingPriceSkr;
                marketListingPriceSkr = Mathf.Max(MarketRules.MinimumListingPriceSkr, parsed - 1);
                marketListingPriceInput = marketListingPriceSkr.ToString();
            }
            var inputArea = OffsetEquipmentDetailRect(panel, 184f, 559f, 144f, 54f);
            DrawMarketTextureFormal(inputArea, MarketFormalModuleRoot,
                "ART-UI-MARKET-B-PRICE-INPUT-144x54-v1.3.6");
            GUI.SetNextControlName("market.equipment.price");
            var edited = GUI.TextField(new Rect(inputArea.x + 6f, inputArea.y,
                    98f, inputArea.height), marketListingPriceInput, 6,
                MarketPriceInputStyleFormal(controlsEnabled));
            GUI.Label(new Rect(inputArea.x + 102f, inputArea.y, 38f, inputArea.height), "SKR",
                MarketTextStyle(13, MarketInk, TextAnchor.MiddleLeft, false));
            if (!string.Equals(edited, marketListingPriceInput, StringComparison.Ordinal))
            {
                marketListingPriceInput = MarketDigitsOnlyFormal(edited);
                int parsed;
                marketListingPriceSkr = int.TryParse(marketListingPriceInput, out parsed) ? parsed : 0;
            }
            if (DrawMarketNudgeFormal(OffsetEquipmentDetailRect(panel, 332f, 559f, 54f, 54f), "+"))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-equipment-price-up");
                int parsed;
                if (!int.TryParse(marketListingPriceInput, out parsed))
                    parsed = MarketRules.MinimumListingPriceSkr - 1;
                marketListingPriceSkr = Mathf.Clamp(parsed + 1,
                    MarketRules.MinimumListingPriceSkr, 999999);
                marketListingPriceInput = marketListingPriceSkr.ToString();
            }
            GUI.enabled = previousEnabled;

            int validPrice;
            var canList = !marketModalSubmissionInProgress &&
                int.TryParse(marketListingPriceInput, out validPrice) &&
                validPrice >= MarketRules.MinimumListingPriceSkr && validPrice <= 999999;
            var actionArea = OffsetEquipmentDetailRect(panel, 485f, 552f, 220f, 68f);
            var clicked = DrawLanternDetailAStateButton(actionArea,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.MediumNormal,
                GlobalButtonFormalArtContract.MediumPressed,
                GlobalButtonFormalArtContract.MediumDisabled, canList);
            GUI.Label(actionArea, T("market.createListing"), MarketTextStyle(22,
                canList ? MarketIvory : MarketMuted, TextAnchor.MiddleCenter, true));
            if (clicked)
            {
                marketModalSubmissionInProgress = true;
                var created = TryCreateMarketListing();
                marketModalSubmissionInProgress = false;
                if (created)
                {
                    selectedMarketPreviewAssetInstanceId = null;
                    CloseMarketModalFormal();
                }
            }
        }

        private void DrawMarketBaseAttributesFormal(MarketListing listing)
        {
            var names = new List<string>();
            var values = new List<string>();
            if (listing.AssetKind == MarketAssetKind.Equipment)
            {
                var modifier = equipmentCatalog.Get(listing.TemplateId).Modifier;
                AddMarketBaseAttributeFormal(names, values, "stat.health", modifier.MaxHealth);
                AddMarketBaseAttributeFormal(names, values, "stat.attack", modifier.AttackDamage);
                AddMarketBaseAttributeFormal(names, values, "stat.armor", modifier.Armor);
                AddMarketBaseAttributeFormal(names, values, "stat.healing", modifier.HealAmount);
                AddMarketBaseAttributeFormal(names, values, "stat.speedTier", modifier.SpeedTierShift);
            }
            else
            {
                names.Add(T("market.kind.lantern"));
                values.Add(MarketQualityName(listing.AssetKind, listing.Quality));
                names.Add(T("market.detail.instance"));
                values.Add(T("market.detail.instancePreserved"));
            }

            var rows = Mathf.Min(3, names.Count);
            var rowY = new[] { 1048f, 1109f, 1170f };
            for (var index = 0; index < rows; index++)
            {
                GUI.Label(new Rect(169f, rowY[index], 300f, 44f), names[index],
                    MarketTextStyle(20, MarketInk, TextAnchor.MiddleLeft, false));
                GUI.Label(new Rect(649f, rowY[index], 270f, 44f), values[index],
                    MarketTextStyle(20, MarketInk, TextAnchor.MiddleRight, true));
            }
        }

        private void AddMarketBaseAttributeFormal(List<string> names, List<string> values,
            string localizationKey, int value)
        {
            if (value == 0 || names.Count >= 3) return;
            names.Add(T(localizationKey));
            values.Add(value > 0 ? "+" + value : value.ToString());
        }

        private void DrawMarketMagicAttributesFormal(MarketListing listing)
        {
            // Current equipment and lantern instance schemas do not yet expose independent magic-affix rows.
            // The accepted modal keeps all three formal rows empty until authoritative instance data exists.
        }

        private void DrawMarketListingPriceEditorFormal()
        {
            GUI.Label(new Rect(139f, 1778f, 220f, 54f), T("market.askingPrice"),
                MarketTextStyle(22, MarketInk, TextAnchor.MiddleLeft, true));
            var previousEnabled = GUI.enabled;
            GUI.enabled = !marketModalSubmissionInProgress;
            if (DrawMarketNudgeFormal(new Rect(398f, 1778f, 54f, 54f), "−"))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-price-down");
                int parsed;
                if (!int.TryParse(marketListingPriceInput, out parsed))
                    parsed = MarketRules.MinimumListingPriceSkr;
                marketListingPriceSkr = Mathf.Max(MarketRules.MinimumListingPriceSkr, parsed - 1);
                marketListingPriceInput = marketListingPriceSkr.ToString();
            }
            DrawMarketTextureFormal(new Rect(468f, 1778f, 144f, 54f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-PRICE-INPUT-144x54-v1.3.6");
            GUI.SetNextControlName("market.modal.price");
            var editedPrice = GUI.TextField(new Rect(474f, 1778f, 98f, 54f),
                marketListingPriceInput, 6, MarketPriceInputStyleFormal(true));
            GUI.Label(new Rect(570f, 1778f, 38f, 54f), "SKR",
                MarketTextStyle(13, MarketInk, TextAnchor.MiddleLeft, false));
            if (!string.Equals(editedPrice, marketListingPriceInput, StringComparison.Ordinal))
            {
                marketListingPriceInput = MarketDigitsOnlyFormal(editedPrice);
                int parsed;
                marketListingPriceSkr = int.TryParse(marketListingPriceInput, out parsed) ? parsed : 0;
            }
            if (DrawMarketNudgeFormal(new Rect(628f, 1778f, 54f, 54f), "+"))
            {
                PlayUiClick(UiAudioCueIds.Tap, "market-price-up");
                int parsed;
                if (!int.TryParse(marketListingPriceInput, out parsed))
                    parsed = MarketRules.MinimumListingPriceSkr - 1;
                marketListingPriceSkr = Mathf.Clamp(parsed + 1,
                    MarketRules.MinimumListingPriceSkr, 999999);
                marketListingPriceInput = marketListingPriceSkr.ToString();
            }
            GUI.enabled = previousEnabled;
            if (marketFeedbackKey != "market.status.ready")
                GUI.Label(new Rect(699f, 1778f, 240f, 54f), T(marketFeedbackKey),
                    MarketTextStyle(14, MarketGold, TextAnchor.MiddleRight, false, true));
        }

        private void DrawMarketShelfExpansionConfirmFormal()
        {
            var body = string.Format(T("market.shelfExpansionCost"), MarketRules.ShopLeasePriceSkr) +
                       "\n" + string.Format(T("market.shelfExpansionCapacity"),
                           marketBoard.ListingCapacity,
                           Math.Min(MarketRules.MaximumListingSlots,
                               marketBoard.ListingCapacity + MarketRules.SlotsPerShopLease));
            var supplemental = marketFeedbackKey == "market.status.ready"
                ? string.Empty : T(marketFeedbackKey);
            var result = DrawCommonConfirmDialog(T("market.expandShelf"), body, supplemental,
                T("market.dialogCancel"), T("market.confirmShelfExpansion"),
                !marketModalSubmissionInProgress);
            if (result == 0)
            {
                PlayUiClick(UiAudioCueIds.Cancel, "market-cancel-shelf-expansion");
                CloseMarketModalFormal();
                return;
            }
            if (result == 1)
            {
                marketModalSubmissionInProgress = true;
                var expanded = TryLeaseMarketShop();
                marketModalSubmissionInProgress = false;
                if (expanded) CloseMarketModalFormal();
            }
        }

        private static string MarketDigitsOnlyFormal(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            var result = string.Empty;
            for (var index = 0; index < value.Length && result.Length < 6; index++)
                if (value[index] >= '0' && value[index] <= '9') result += value[index];
            return result.TrimStart('0');
        }

        private GUIStyle MarketPriceInputStyleFormal(bool enabled)
        {
            var style = new GUIStyle(GUIStyle.none)
            {
                font = SourceHanSerifFont(true),
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Normal,
                clipping = TextClipping.Clip,
                padding = new RectOffset(4, 4, 0, 0)
            };
            style.normal.background = null;
            style.hover.background = null;
            style.active.background = null;
            style.focused.background = null;
            style.normal.textColor = enabled ? MarketInk : MarketMuted;
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            style.focused.textColor = style.normal.textColor;
            return style;
        }

        private List<MarketListing> FilterAndSortMarketListingsFormal(bool ownOnly)
        {
            var result = FilterListings(ownOnly);
            for (var index = result.Count - 1; index >= 0; index--)
                if (!MatchesMarketEquipmentSubtypeFormal(result[index])) result.RemoveAt(index);
            result.Sort((left, right) =>
            {
                switch (marketSortMode)
                {
                    case MarketSortMode.PriceAscending:
                        return left.PriceMicroSkr.CompareTo(right.PriceMicroSkr);
                    case MarketSortMode.PriceDescending:
                        return right.PriceMicroSkr.CompareTo(left.PriceMicroSkr);
                    case MarketSortMode.Latest:
                        return right.CreatedAtUtcTicks.CompareTo(left.CreatedAtUtcTicks);
                    default:
                        return string.CompareOrdinal(left.ListingId, right.ListingId);
                }
            });
            return result;
        }

        private List<MarketSaleCandidate> FilterMarketSaleCandidatesFormal()
        {
            var result = BuildMarketSaleCandidates();
            for (var index = result.Count - 1; index >= 0; index--)
            {
                var item = result[index];
                if (marketAssetFilter == MarketAssetFilter.Equipment && item.Kind != MarketAssetKind.Equipment)
                    result.RemoveAt(index);
                else if (marketAssetFilter == MarketAssetFilter.Lantern && item.Kind != MarketAssetKind.Lantern)
                    result.RemoveAt(index);
            }
            return result;
        }

        private List<MarketListing> BuildMarketHistoryRowsFormal()
        {
            var rows = new List<MarketListing>();
            for (var index = marketBoard.Listings.Count - 1; index >= 0; index--)
            {
                var listing = marketBoard.Listings[index];
                if (listing.State == MarketListingState.Active) continue;
                var involved = string.Equals(listing.SellerId, MarketRules.LocalPlayerId, StringComparison.Ordinal) ||
                    string.Equals(listing.BuyerId, MarketRules.LocalPlayerId, StringComparison.Ordinal);
                if (!involved) continue;
                if (marketAssetFilter == MarketAssetFilter.Equipment && listing.AssetKind != MarketAssetKind.Equipment)
                    continue;
                if (marketAssetFilter == MarketAssetFilter.Lantern && listing.AssetKind != MarketAssetKind.Lantern)
                    continue;
                rows.Add(listing);
            }
            return rows;
        }

        private void SetMarketFilterFormal(MarketAssetFilter filter)
        {
            PlayUiClick(UiAudioCueIds.TabSwitch, "market-filter");
            marketAssetFilter = filter;
            marketEquipmentFilterOpen = false;
            marketScroll = Vector2.zero;
        }

        private bool MatchesMarketEquipmentSubtypeFormal(MarketListing listing)
        {
            if (marketAssetFilter != MarketAssetFilter.Equipment ||
                marketEquipmentTypeFilter == MarketEquipmentTypeFilter.All ||
                listing.AssetKind != MarketAssetKind.Equipment) return true;
            return MarketEquipmentTypeFilterForTemplateFormal(listing.TemplateId) == marketEquipmentTypeFilter;
        }

        private MarketSaleCandidate FindSelectedMarketSaleCandidateFormal()
        {
            var candidates = BuildMarketSaleCandidates();
            for (var index = 0; index < candidates.Count; index++)
                if (candidates[index].Kind == selectedMarketAssetKind &&
                    string.Equals(candidates[index].InstanceId, selectedMarketAssetInstanceId,
                        StringComparison.Ordinal)) return candidates[index];
            return null;
        }

        private MarketSaleCandidate FindMarketSalePreviewCandidateFormal()
        {
            if (string.IsNullOrEmpty(selectedMarketPreviewAssetInstanceId)) return null;
            var candidates = BuildMarketSaleCandidates();
            for (var index = 0; index < candidates.Count; index++)
                if (string.Equals(candidates[index].InstanceId, selectedMarketPreviewAssetInstanceId,
                    StringComparison.Ordinal)) return candidates[index];
            return null;
        }

        private string MarketAssetTypeNameFormal(MarketAssetKind kind, string templateId)
        {
            if (kind == MarketAssetKind.Lantern) return T("market.filter.lantern");
            return T("market.type." + MarketEquipmentTypeFilterForTemplateFormal(templateId).ToString().ToLowerInvariant());
        }

        private MarketEquipmentTypeFilter MarketEquipmentTypeFilterForTemplateFormal(string templateId)
        {
            switch (equipmentCatalog.Get(templateId).Slot)
            {
                case EquipmentSlot.Head: return MarketEquipmentTypeFilter.Head;
                case EquipmentSlot.Body: return MarketEquipmentTypeFilter.Body;
                case EquipmentSlot.Hands: return MarketEquipmentTypeFilter.Hands;
                case EquipmentSlot.Feet: return MarketEquipmentTypeFilter.Feet;
                default: return MarketEquipmentTypeFilter.Weapon;
            }
        }

        private string MarketEquipmentFilterKeyFormal(MarketEquipmentTypeFilter filter)
        {
            return filter == MarketEquipmentTypeFilter.All
                ? "market.filter.equipmentAll"
                : "market.type." + filter.ToString().ToLowerInvariant();
        }

        private string MarketSortKeyFormal()
        {
            switch (marketSortMode)
            {
                case MarketSortMode.PriceAscending: return "market.sort.priceAscending";
                case MarketSortMode.PriceDescending: return "market.sort.priceDescending";
                case MarketSortMode.Latest: return "market.sort.latest";
                default: return "market.sort.default";
            }
        }

        private void DrawMarketItemLayersFormal(Rect area, MarketAssetKind kind,
            string templateId, int quality, bool detail)
        {
            var size = detail ? 220 : 104;
            DrawMarketTextureFormal(area, MarketFormalModuleRoot,
                "ART-UI-MARKET-B-QUALITY-BG-" + MarketQualityAssetNameFormal(kind, quality) +
                "-" + size + "-v1.1.0");
            if (kind == MarketAssetKind.Equipment)
            {
                DrawWarehouseTexture(area,
                    detail
                        ? WarehouseAFormalArtContract.EquipmentDetailAppearance(templateId)
                        : WarehouseAFormalArtContract.EquipmentListAppearance(templateId),
                    detail ? "equipment-detail" : "equipment-list");
                return;
            }

            DrawWarehouseTexture(area,
                detail
                    ? WarehouseAFormalArtContract.LanternDetailAppearance(templateId)
                    : WarehouseAFormalArtContract.LanternListAppearance(templateId),
                detail ? "lantern-detail" : "lantern-list");
        }

        private static string MarketQualityAssetNameFormal(MarketAssetKind kind, int quality)
        {
            if (kind == MarketAssetKind.Equipment)
            {
                if (quality <= (int)EquipmentQuality.Common) return "COMMON";
                if (quality == (int)EquipmentQuality.Rare) return "RARE";
                return "EPIC";
            }
            if (quality <= (int)LanternQuality.Common) return "COMMON";
            if (quality == (int)LanternQuality.Uncommon) return "UNCOMMON";
            if (quality == (int)LanternQuality.Rare) return "RARE";
            if (quality == (int)LanternQuality.Epic) return "EPIC";
            return "LEGENDARY";
        }

        private static Color MarketQualityTextColorFormal(MarketAssetKind kind, int quality)
        {
            if (kind == MarketAssetKind.Equipment)
            {
                if (quality <= (int)EquipmentQuality.Common) return new Color32(215, 211, 184, 255);
                if (quality == (int)EquipmentQuality.Rare) return new Color32(64, 176, 222, 255);
                return new Color32(187, 83, 221, 255);
            }
            switch (quality)
            {
                case 1: return new Color32(75, 186, 128, 255);
                case 2: return new Color32(64, 176, 222, 255);
                case 3: return new Color32(187, 83, 221, 255);
                case 4: return new Color32(244, 166, 43, 255);
                default: return new Color32(215, 211, 184, 255);
            }
        }

        private string MarketItemStatsFormal(MarketListing listing)
        {
            if (listing.AssetKind == MarketAssetKind.Lantern)
                return T("market.detail.lanternStats");
            var modifier = equipmentCatalog.Get(listing.TemplateId).Modifier;
            return string.Format(T("market.detail.equipmentStats"), modifier.MaxHealth,
                modifier.AttackDamage, modifier.Armor, modifier.HealAmount);
        }

        private string[] MarketItemStatColumnsFormal(MarketListing listing)
        {
            if (listing.AssetKind == MarketAssetKind.Lantern)
                return new[]
                {
                    T("market.filter.lantern"),
                    MarketQualityName(listing.AssetKind, listing.Quality),
                    T("market.detail.instancePreserved")
                };

            var modifier = equipmentCatalog.Get(listing.TemplateId).Modifier;
            return new[]
            {
                T("stat.health") + " +" + modifier.MaxHealth,
                T("stat.attack") + " +" + modifier.AttackDamage,
                T("stat.armor") + " +" + modifier.Armor +
                    (modifier.HealAmount == 0 ? string.Empty : "\n" + T("stat.healing") + " +" + modifier.HealAmount)
            };
        }

        private string MarketItemDescriptionFormal(MarketListing listing)
        {
            var localized = T(listing.TemplateId + ".description");
            return localized == listing.TemplateId + ".description" ? T("market.detail.defaultDescription") : localized;
        }

        private static string MarketHistoryTimeFormal(long utcTicks)
        {
            if (utcTicks <= 0) return "—";
            try
            {
                return new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime().ToString("MM-dd HH:mm");
            }
            catch (ArgumentOutOfRangeException)
            {
                return "—";
            }
        }

        private void DrawMarketFilterButtonFormal(Rect area, string label, bool selected, Action onClick)
        {
            DrawMarketTextureFormal(area, MarketFormalModuleRoot, selected
                ? "ART-UI-MARKET-B-FILTER-FLAT-SELECTED-180x60-v1.1.0"
                : "ART-UI-MARKET-B-FILTER-FLAT-NORMAL-180x60-v1.1.0");
            GUI.Label(area, label, MarketTextStyle(18, selected ? MarketIvory : MarketInk,
                TextAnchor.MiddleCenter, selected));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none)) onClick();
        }

        private bool DrawMarketPrimaryButtonFormal(Rect area, string label, bool enabled)
        {
            DrawMarketTextureFormal(area, MarketFormalModuleRoot, enabled
                ? "ART-UI-MARKET-B-BUTTON-PRIMARY-FLAT-NORMAL-250x70-v1.1.0"
                : "ART-UI-MARKET-B-BUTTON-PRIMARY-FLAT-DISABLED-250x70-v1.1.0");
            GUI.Label(area, label, MarketTextStyle(18, enabled ? MarketIvory : MarketMuted,
                TextAnchor.MiddleCenter, true));
            return enabled && GUI.Button(area, GUIContent.none, GUIStyle.none);
        }

        private bool DrawMarketNudgeFormal(Rect area, string label)
        {
            DrawMarketTextureFormal(area, MarketFormalModuleRoot,
                "ART-UI-MARKET-B-PRICE-NUDGE-BUTTON-54x54-v1.3.6");
            GUI.Label(area, label, MarketTextStyle(26, MarketIvory, TextAnchor.MiddleCenter, false));
            return GUI.Button(area, GUIContent.none, GUIStyle.none);
        }

        private void DrawMarketTextureFormal(Rect area, string root, string fileName)
        {
            var resourceId = root + fileName;
            Texture2D texture;
            if (!marketFormalTextures.TryGetValue(resourceId, out texture))
            {
                texture = Resources.Load<Texture2D>(resourceId);
                marketFormalTextures[resourceId] = texture;
                if (texture == null) Debug.LogError("Missing formal market texture: " + resourceId);
            }
            if (texture != null) GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
        }

        private GUIStyle MarketTextStyle(int fontSize, Color color, TextAnchor alignment,
            bool emphasized, bool wordWrap = false)
        {
            var style = Style(fontSize, color, alignment, FontStyle.Normal);
            style.font = SourceHanSerifFont(emphasized);
            style.wordWrap = wordWrap;
            style.clipping = TextClipping.Clip;
            return style;
        }
    }
}
