using System;
using System.Linq;
using System.Threading;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private bool lanternWorkshopMode;
        private bool lanternWorkshopLoading;
        private bool lanternWorkshopAdvancing;
        private LanternWorkshopSnapshot lanternWorkshopSnapshot;
        private string lanternWorkshopError;
        private string lanternWorkshopSelectedInstanceId;
        private string lanternWorkshopPendingOptionId;

        private async void RequestLanternWorkshopRefresh()
        {
            if (lanternWorkshopLoading) return;
            lanternWorkshopLoading = true;
            lanternWorkshopError = null;
            try
            {
                var token = walletServerLifetime == null
                    ? CancellationToken.None : walletServerLifetime.Token;
                lanternWorkshopSnapshot = await LoadLanternWorkshopAsync(token);
                if (lanternWorkshopSelectedInstanceId != null &&
                    FindWorkshopState(lanternWorkshopSelectedInstanceId) == null)
                    lanternWorkshopSelectedInstanceId = null;
            }
            catch (Exception exception)
            {
                lanternWorkshopError = exception.Message;
            }
            finally
            {
                lanternWorkshopLoading = false;
            }
        }

        private async void ConfirmLanternWorkshopAdvance()
        {
            var state = FindWorkshopState(lanternWorkshopSelectedInstanceId);
            if (state == null || string.IsNullOrWhiteSpace(lanternWorkshopPendingOptionId) ||
                lanternWorkshopAdvancing) return;
            lanternWorkshopAdvancing = true;
            lanternWorkshopError = null;
            var optionId = lanternWorkshopPendingOptionId;
            lanternWorkshopPendingOptionId = null;
            try
            {
                var token = walletServerLifetime == null
                    ? CancellationToken.None : walletServerLifetime.Token;
                await AdvanceLanternCovenantAsync(state.LanternInstanceId, optionId,
                    state.StateVersion, Guid.NewGuid().ToString("N"), token);
                lanternWorkshopSnapshot = await LoadLanternWorkshopAsync(token);
            }
            catch (Exception exception)
            {
                lanternWorkshopError = exception.Message;
            }
            finally
            {
                lanternWorkshopAdvancing = false;
            }
        }

        private void DrawLanternWorkshopScreen()
        {
            DrawWarehouseTexture(new Rect(0f, 0f, 1080f, 2400f),
                WarehouseAFormalArtContract.Background, "modules");
            GUI.Label(new Rect(48f, 54f, 420f, 92f), T("lantern.workshop.title"),
                Style(54, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleLeft));
            var returnArea = new Rect(800f, 52f, 192f, 72f);
            DrawWarehouseButtonTexture(returnArea, WarehouseAFormalArtContract.ReturnNormal,
                WarehouseAFormalArtContract.ReturnPressed);
            GUI.Label(new Rect(842f, 52f, 140f, 72f), T("home.back"),
                Style(28, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
            if (!lanternWorkshopAdvancing && GUI.Button(returnArea, GUIContent.none, GUIStyle.none))
            {
                lanternWorkshopMode = false;
                lanternWorkshopSelectedInstanceId = null;
                lanternWorkshopPendingOptionId = null;
                currentView = ScreenView.Harbor;
                PlayUiClick(UiAudioCueIds.Cancel, "lantern-workshop-return");
                return;
            }

            var balance = lanternWorkshopSnapshot == null ? 0L : lanternWorkshopSnapshot.LampGlowBalance;
            GUI.Label(new Rect(48f, 190f, 700f, 72f),
                string.Format(T("lantern.workshop.balance"), balance),
                Style(29, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleLeft));
            GUI.Label(new Rect(48f, 270f, 960f, 68f), T("lantern.workshop.subtitle"),
                Style(23, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleLeft));

            if (lanternWorkshopLoading)
                GUI.Label(new Rect(80f, 640f, 920f, 100f), T("lantern.workshop.loading"),
                    Style(30, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
            else if (!string.IsNullOrWhiteSpace(lanternWorkshopError))
            {
                GUI.Label(new Rect(80f, 530f, 920f, 180f), lanternWorkshopError,
                    Style(24, new Color(0.95f, 0.75f, 0.62f), TextAnchor.MiddleCenter));
                var retry = new Rect(400f, 760f, 280f, 76f);
                if (DrawCommonConfirmButton(retry, T("lantern.workshop.retry"), true, true))
                    RequestLanternWorkshopRefresh();
            }
            else DrawLanternWorkshopInventory();

            if (!string.IsNullOrEmpty(lanternWorkshopSelectedInstanceId))
                DrawLanternWorkshopDetail();
            if (!string.IsNullOrEmpty(lanternWorkshopPendingOptionId))
                DrawLanternWorkshopConfirmation();
        }

        private void DrawLanternWorkshopInventory()
        {
            var lanterns = lanternWorkshopSnapshot == null
                ? new LanternWorkshopLanternState[0]
                : lanternWorkshopSnapshot.Lanterns ?? new LanternWorkshopLanternState[0];
            if (lanterns.Length == 0)
            {
                GUI.Label(new Rect(80f, 600f, 920f, 180f), T("lantern.workshop.empty"),
                    Style(28, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
                return;
            }
            for (var index = 0; index < lanterns.Length && index < 50; index++)
            {
                var item = lanterns[index];
                var authoritative = FindAuthoritativeLantern(item.LanternInstanceId);
                var area = new Rect(48f + index % 5 * 200f,
                    390f + index / 5 * 230f, 184f, 184f);
                var quality = authoritative == null ? LanternQuality.Common :
                    ParseWorkshopQuality(authoritative.RarityId);
                DrawWarehouseTexture(area,
                    WarehouseAFormalArtContract.QualityBackground(
                        quality.ToString().ToLowerInvariant()), "modules");
                if (authoritative != null &&
                    HomeLanternModularArtContract.IsValidAppearanceId(authoritative.AppearanceId))
                {
                    var templateId = HomeLanternModularArtContract.TemplateIdForAppearanceId(
                        authoritative.AppearanceId);
                    DrawWarehouseTexture(area,
                        WarehouseAFormalArtContract.LanternAppearance(templateId), "modules");
                }
                GUI.Label(new Rect(area.x, area.y + 184f, area.width, 40f),
                    string.Format(T("warehouse.lanternStage"), item.ContractStage, 6),
                    Style(20, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
                if (GUI.Button(area, GUIContent.none, GUIStyle.none))
                {
                    lanternWorkshopSelectedInstanceId = item.LanternInstanceId;
                    PlayUiClick(UiAudioCueIds.Tap, "lantern-workshop-select");
                }
            }
        }

        private void DrawLanternWorkshopDetail()
        {
            var state = FindWorkshopState(lanternWorkshopSelectedInstanceId);
            if (state == null) { lanternWorkshopSelectedInstanceId = null; return; }
            var panel = LanternDetailFormalArtContract.PanelRect;
            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            DrawMarketModalInputShieldFormal(panel);
            DrawMarketTextureFormal(panel, LanternDetailFormalArtContract.ResourceRoot,
                LanternDetailFormalArtContract.FixedShell);
            GUI.Label(OffsetEquipmentDetailRect(panel, 250f, 36f, 482f, 84f),
                T("lantern.workshop.detailTitle"),
                Style(34, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));
            var pageArea = OffsetEquipmentDetailRect(panel, 62f, 258f, 858f, 930f);
            DrawMarketTextureFormal(pageArea, LanternDetailFormalArtContract.ResourceRoot,
                LanternDetailFormalArtContract.CovenantPage);
            GUI.Label(OffsetEquipmentDetailRect(panel, 82f, 286f, 818f, 56f),
                string.Format(T("lantern.workshop.currentStage"), state.ContractStage, 6),
                Style(28, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 82f, 350f, 818f, 56f),
                string.Format(T("lantern.workshop.balance"), lanternWorkshopSnapshot.LampGlowBalance),
                Style(24, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));

            for (var index = 0; index < 6; index++)
            {
                var row = OffsetEquipmentDetailRect(panel, 82f, 446f + index * 88f, 818f, 76f);
                var value = index < state.ContractStage
                    ? T("lantern.covenant." + state.OptionIds[index])
                    : T("lantern.detail.locked");
                GUI.Label(row, (index + 1) + ".  " + value,
                    Style(22, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            }

            if (state.ContractStage >= GuardTideCovenantRules.MaximumStage)
                GUI.Label(OffsetEquipmentDetailRect(panel, 82f, 1040f, 818f, 90f),
                    T("lantern.workshop.complete"),
                    Style(28, DetailBodyInk, TextAnchor.MiddleCenter, FontStyle.Bold));
            else DrawLanternWorkshopNextOptions(panel, state);

            var closeArea = OffsetEquipmentDetailRect(panel, 885f, 42f, 70f, 70f);
            if (!lanternWorkshopAdvancing && GUI.Button(closeArea, GUIContent.none, GUIStyle.none))
                lanternWorkshopSelectedInstanceId = null;
        }

        private void DrawLanternWorkshopNextOptions(Rect panel,
            LanternWorkshopLanternState state)
        {
            var nextStage = state.ContractStage + 1;
            var cost = GuardTideCovenantRules.CostForStage(nextStage);
            GUI.Label(OffsetEquipmentDetailRect(panel, 82f, 1020f, 818f, 54f),
                string.Format(T("lantern.workshop.nextCost"), nextStage, cost),
                Style(24, DetailBodyInk, TextAnchor.MiddleCenter, FontStyle.Bold));
            var options = GuardTideCovenantRules.OptionsForStage(nextStage);
            var startX = options.Count == 2 ? 190f : 52f;
            for (var index = 0; index < options.Count; index++)
            {
                var area = OffsetEquipmentDetailRect(panel, startX + index * 300f,
                    1218f, 280f, 76f);
                var enabled = !lanternWorkshopAdvancing &&
                    lanternWorkshopSnapshot.LampGlowBalance >= cost;
                if (DrawCommonConfirmButton(area,
                        T("lantern.covenant." + options[index]), true, enabled))
                    lanternWorkshopPendingOptionId = options[index];
            }
            if (lanternWorkshopSnapshot.LampGlowBalance < cost)
                GUI.Label(OffsetEquipmentDetailRect(panel, 82f, 1310f, 818f, 52f),
                    T("lantern.workshop.insufficient"),
                    Style(22, MarketMuted, TextAnchor.MiddleCenter, FontStyle.Bold));
        }

        private void DrawLanternWorkshopConfirmation()
        {
            var state = FindWorkshopState(lanternWorkshopSelectedInstanceId);
            if (state == null) { lanternWorkshopPendingOptionId = null; return; }
            var nextStage = state.ContractStage + 1;
            var cost = GuardTideCovenantRules.CostForStage(nextStage);
            var result = DrawCommonConfirmDialog(T("lantern.workshop.confirmTitle"),
                string.Format(T("lantern.workshop.confirmBody"),
                    T("lantern.covenant." + lanternWorkshopPendingOptionId), cost),
                T("lantern.workshop.irreversible"), T("arena.cancel"),
                T("lantern.workshop.confirm"), !lanternWorkshopAdvancing);
            if (result == 0) lanternWorkshopPendingOptionId = null;
            else if (result == 1) ConfirmLanternWorkshopAdvance();
        }

        private LanternWorkshopLanternState FindWorkshopState(string instanceId)
        {
            if (lanternWorkshopSnapshot == null || string.IsNullOrEmpty(instanceId)) return null;
            return (lanternWorkshopSnapshot.Lanterns ?? new LanternWorkshopLanternState[0])
                .FirstOrDefault(item => string.Equals(item.LanternInstanceId,
                    instanceId, StringComparison.OrdinalIgnoreCase));
        }

        private AuthoritativeLanternInstance FindAuthoritativeLantern(string instanceId)
        {
            var snapshot = AuthoritativeRewardInventory;
            if (snapshot == null) return null;
            return (snapshot.Lanterns ?? new AuthoritativeLanternInstance[0])
                .FirstOrDefault(item => string.Equals(item.InstanceId,
                    instanceId, StringComparison.OrdinalIgnoreCase));
        }

        private static LanternQuality ParseWorkshopQuality(string rarityId)
        {
            LanternQuality quality;
            return Enum.TryParse(rarityId, true, out quality) ? quality : LanternQuality.Common;
        }
    }
}
