using System;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private const string FirstLightseekerBadgeId = "badge.first_lightseeker.v1";
        private const string FirstLightseekerBadgeRoot = "Art/FirstLightseekerBadge/V101/";
        private const string FirstLightseekerPopupResource =
            "popup/ART-UI-BADGE-FIRST-LIGHTSEEKER-POPUP-512-v1.0.1";
        private const string FirstLightseekerWarehouseResource =
            "warehouse/ART-UI-WAREHOUSE-A-BADGE-FIRST-LIGHTSEEKER-ORANGE-184-v1.0.1";
        private const string FirstLightseekerAcknowledgedPrefix =
            "lightseeker.first-lightseeker-badge.ack.v1.";
        private const float FirstLightseekerPopupRevealDuration = 0.48f;
        private const float FirstLightseekerPopupRevealOffset = 72f;

        private bool firstLightseekerBadgeOwned;
        private bool firstLightseekerBadgePopupOpen;
        private bool firstLightseekerBadgeDetailOpen;
        private string firstLightseekerBadgeAccountId;
        private float firstLightseekerBadgePopupOpenedAt = -1f;
        private Texture2D firstLightseekerPopupTexture;
        private Texture2D firstLightseekerWarehouseTexture;

        private static string FirstLightseekerBadgeToken()
        {
            return "badge:" + FirstLightseekerBadgeId;
        }

        private void ApplyFirstLightseekerBadgeGrant(
            FirstLightseekerBadgeGrantSnapshot grant)
        {
            if (grant == null ||
                !string.Equals(grant.BadgeId, FirstLightseekerBadgeId, StringComparison.Ordinal) ||
                !string.Equals(grant.Status, "active", StringComparison.Ordinal))
                throw new InvalidOperationException("The First Lightseeker badge grant is invalid.");
            firstLightseekerBadgeOwned = true;
            firstLightseekerBadgeAccountId = grant.AccountId;
            var acknowledged = PlayerPrefs.GetInt(
                FirstLightseekerAcknowledgedPrefix + grant.AccountId, 0) == 1;
            var shouldOpen = !acknowledged;
            if (shouldOpen && !firstLightseekerBadgePopupOpen)
                firstLightseekerBadgePopupOpenedAt = Time.unscaledTime;
            firstLightseekerBadgePopupOpen = shouldOpen;
            ReconcileWarehouseLayout();
        }

        private void DrawFirstLightseekerBadgePopup()
        {
            if (!firstLightseekerBadgePopupOpen) return;
            var elapsed = firstLightseekerBadgePopupOpenedAt < 0f
                ? FirstLightseekerPopupRevealDuration
                : Time.unscaledTime - firstLightseekerBadgePopupOpenedAt;
            var progress = Mathf.Clamp01(elapsed / FirstLightseekerPopupRevealDuration);
            var eased = 1f - Mathf.Pow(1f - progress, 3f);
            var revealOffset = (1f - eased) * FirstLightseekerPopupRevealOffset;
            var previousColor = GUI.color;
            GUI.color = new Color(previousColor.r, previousColor.g, previousColor.b,
                previousColor.a * eased);
            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f),
                MarketFormalModuleRoot, "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            DrawFirstLightseekerPopupArt(new Rect(284f, 430f + revealOffset, 512f, 512f));
            GUI.Label(new Rect(140f, 970f + revealOffset, 800f, 72f),
                "First Lightseeker",
                ArchiveStyle(40, MarketGold, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(135f, 1055f + revealOffset, 810f, 170f),
                ProfileText(
                    "欢迎，首批寻光者。当世界仍笼罩在海雾之中时，你已成为最早踏上寻光旅程的人之一。",
                    "Welcome, First Lightseekers. While the world still lay veiled in sea fog, " +
                    "you were among the first to set out in search of the light."),
                BadgeBodyStyle());
            GUI.Label(new Rect(135f, 1235f + revealOffset, 810f, 48f),
                ProfileText(
                    "已由服务器永久记录 · 已存入仓库「其他」· 不可交易",
                    "PERMANENTLY RECORDED · WAREHOUSE / OTHER · NON-TRANSFERABLE"),
                ArchiveStyle(language == Localization.GameLanguage.ChineseSimplified ? 21 : 17,
                    MarketIvory, TextAnchor.MiddleCenter, false));
            var button = new Rect(310f, 1320f + revealOffset, 460f, 96f);
            DrawSettlementTexture(button, ExpeditionSettlementFormalArtContract.ReusedRoot,
                IsPointerDown(button)
                    ? ExpeditionSettlementFormalArtContract.ButtonPressed
                    : ExpeditionSettlementFormalArtContract.ButtonNormal, 460, 96);
            GUI.Label(button, ProfileText("确认领取", "CONFIRM"),
                ArchiveStyle(32, MarketIvory, TextAnchor.MiddleCenter, true));
            GUI.color = previousColor;
            var previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && progress >= 1f;
            var confirmed = GUI.Button(button, GUIContent.none, GUIStyle.none);
            GUI.enabled = previousEnabled;
            if (!confirmed) return;
            firstLightseekerBadgePopupOpen = false;
            firstLightseekerBadgePopupOpenedAt = -1f;
            if (!string.IsNullOrWhiteSpace(firstLightseekerBadgeAccountId))
            {
                PlayerPrefs.SetInt(
                    FirstLightseekerAcknowledgedPrefix + firstLightseekerBadgeAccountId, 1);
                PlayerPrefs.Save();
            }
            PlayUiClick(UiAudioCueIds.Reward, "first-lightseeker-badge-confirm");
        }

        private GUIStyle BadgeBodyStyle()
        {
            var style = ArchiveStyle(
                language == Localization.GameLanguage.ChineseSimplified ? 30 : 24,
                MarketIvory, TextAnchor.UpperCenter, false);
            style.wordWrap = true;
            style.clipping = TextClipping.Clip;
            return style;
        }

        private void DrawFirstLightseekerWarehouseSlot(Rect area)
        {
            if (!firstLightseekerBadgeOwned) return;
            if (firstLightseekerWarehouseTexture == null)
                firstLightseekerWarehouseTexture = Resources.Load<Texture2D>(
                    FirstLightseekerBadgeRoot + FirstLightseekerWarehouseResource);
            if (firstLightseekerWarehouseTexture == null ||
                firstLightseekerWarehouseTexture.width != 184 ||
                firstLightseekerWarehouseTexture.height != 184 ||
                Mathf.RoundToInt(area.width) != 184 || Mathf.RoundToInt(area.height) != 184)
            {
                Debug.LogError("Rejected missing or resized First Lightseeker warehouse asset.");
                return;
            }
            GUI.DrawTexture(area, firstLightseekerWarehouseTexture, ScaleMode.StretchToFill, true);
            if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) return;
            firstLightseekerBadgeDetailOpen = true;
            PlayUiClick(UiAudioCueIds.Tap, "first-lightseeker-badge-detail-open");
        }

        private void DrawFirstLightseekerBadgeDetail()
        {
            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f),
                MarketFormalModuleRoot, "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            DrawFirstLightseekerPopupArt(new Rect(284f, 360f, 512f, 512f));
            GUI.Label(new Rect(150f, 910f, 780f, 64f), "First Lightseeker",
                ArchiveStyle(38, MarketGold, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(150f, 982f, 780f, 46f),
                ProfileText("徽章 · 不可交易", "BADGE · NON-TRANSFERABLE"),
                ArchiveStyle(24, MarketIvory, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(135f, 1060f, 810f, 260f),
                "Welcome, First Lightseekers. While the world still lay veiled in sea fog, " +
                "you were among the first to set out in search of the light.", BadgeBodyStyle());
            var button = new Rect(310f, 1380f, 460f, 96f);
            DrawSettlementTexture(button, ExpeditionSettlementFormalArtContract.ReusedRoot,
                IsPointerDown(button)
                    ? ExpeditionSettlementFormalArtContract.ButtonPressed
                    : ExpeditionSettlementFormalArtContract.ButtonNormal, 460, 96);
            GUI.Label(button, ProfileText("返回仓库", "BACK TO WAREHOUSE"),
                ArchiveStyle(30, MarketIvory, TextAnchor.MiddleCenter, true));
            if (!GUI.Button(button, GUIContent.none, GUIStyle.none)) return;
            firstLightseekerBadgeDetailOpen = false;
            PlayUiClick(UiAudioCueIds.Cancel, "first-lightseeker-badge-detail-close");
        }

        private void DrawFirstLightseekerPopupArt(Rect area)
        {
            if (firstLightseekerPopupTexture == null)
                firstLightseekerPopupTexture = Resources.Load<Texture2D>(
                    FirstLightseekerBadgeRoot + FirstLightseekerPopupResource);
            if (firstLightseekerPopupTexture == null ||
                firstLightseekerPopupTexture.width != 512 ||
                firstLightseekerPopupTexture.height != 512 ||
                Mathf.RoundToInt(area.width) != 512 || Mathf.RoundToInt(area.height) != 512)
            {
                Debug.LogError("Rejected missing or resized First Lightseeker popup asset.");
                return;
            }
            GUI.DrawTexture(area, firstLightseekerPopupTexture, ScaleMode.StretchToFill, true);
        }
    }
}
