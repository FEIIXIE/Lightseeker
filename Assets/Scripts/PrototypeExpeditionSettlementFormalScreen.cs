using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private readonly Dictionary<string, Texture2D> expeditionSettlementTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        private sealed class SettlementRewardVisual
        {
            public string Name;
            public int Quantity;
            public string ResourceId;
            public string Root;
        }

        private void DrawFormalExpeditionSettlementScreen()
        {
            DrawSettlementTexture(new Rect(0f, 0f, 1080f, 2400f),
                ExpeditionSettlementFormalArtContract.ReusedRoot,
                ExpeditionSettlementFormalArtContract.Scrim, 1080, 2400);
            DrawSettlementTexture(new Rect(70f, 300f, 940f, 1410f),
                ExpeditionSettlementFormalArtContract.ModuleRoot,
                ExpeditionSettlementFormalArtContract.Shell, 940, 1410);

            var clear = runWon && !runAbandoned;
            DrawSettlementLabel(new Rect(880f, 24f, 170f, 32f), ClientVersionLabel, 18,
                new Color32(250, 244, 218, 255), TextAnchor.MiddleCenter, false);
            DrawSettlementLabel(new Rect(230f, 485f, 620f, 76f),
                clear ? ProfileText("远征完成", "EXPEDITION COMPLETE") :
                    ProfileText("返航结算", "RETURN SETTLEMENT"), 55,
                new Color32(102, 70, 25, 255), TextAnchor.MiddleCenter, true);
            DrawSettlementLabel(new Rect(170f, 578f, 740f, 40f),
                clear ? ProfileText("航程终点已抵达，奖励等待确认。", "THE VOYAGE IS COMPLETE; REWARDS AWAIT.") :
                    ProfileText("本次航程已经结束。", "THIS VOYAGE HAS ENDED."), 24,
                new Color32(103, 121, 126, 255), TextAnchor.MiddleCenter, false);
            DrawSettlementLabel(new Rect(350f, 676f, 380f, 48f),
                runAbandoned ? ProfileText("主动放弃", "ABANDONED") :
                    clear ? ProfileText("首领已击败", "BOSS DEFEATED") :
                        ProfileText("生命归零", "LIFE DEPLETED"), 28,
                new Color32(250, 244, 218, 255), TextAnchor.MiddleCenter, true);

            var plan = pendingFormalExpeditionSettlement == null
                ? EnsureLocalExpeditionSettlementPlan()
                : pendingFormalExpeditionSettlement.Plan;
            var difficulty = plan == null || string.IsNullOrEmpty(plan.DifficultyId)
                ? "D0" : plan.DifficultyId;
            var progress = plan == null
                ? (expeditionRewards == null ? 0 : expeditionRewards.CompletedNodeIds.Count)
                : plan.ProgressRank;
            DrawSettlementSummary(128f, ProfileText("远征难度", "DIFFICULTY"), difficulty);
            DrawSettlementSummary(396f, ProfileText("航程进度", "PROGRESS"), progress.ToString());
            DrawSettlementSummary(664f, ProfileText("航行时长", "DURATION"), "--");

            DrawSettlementLabel(new Rect(330f, 930f, 420f, 58f),
                clear ? ProfileText("本次收获", "REWARDS") : ProfileText("本次带回", "BROUGHT BACK"),
                36, new Color32(102, 70, 25, 255), TextAnchor.MiddleCenter, true);
            DrawSettlementRewards(BuildSettlementRewardVisuals(plan, clear), clear);

            DrawSettlementLabel(new Rect(clear ? 250f : 180f, 1487f,
                    clear ? 580f : 720f, clear ? 36f : 38f),
                clear ? ProfileText("奖励已锁定，确认后统一入账", "REWARDS ARE LOCKED UNTIL CONFIRMED") :
                    ProfileText("未通关不产出成品装备、航灯与赏金券",
                        "NO FINISHED GEAR, LANTERN OR BOUNTY TICKETS WITHOUT A CLEAR"),
                20, new Color32(103, 121, 126, 255), TextAnchor.MiddleCenter, false);
            DrawSettlementPrimaryAction();
        }

        private void DrawSettlementSummary(float x, string label, string value)
        {
            DrawSettlementLabel(new Rect(x, 788f, 288f, 32f), label, 20,
                new Color32(103, 121, 126, 255), TextAnchor.MiddleCenter, false);
            DrawSettlementLabel(new Rect(x, 823f, 288f, 48f), value, 30,
                new Color32(8, 55, 78, 255), TextAnchor.MiddleCenter, true);
        }

        private List<SettlementRewardVisual> BuildSettlementRewardVisuals(
            ExpeditionServerSettlementPlan plan, bool clear)
        {
            var result = new List<SettlementRewardVisual>(8);
            if (plan != null)
            {
                AddSettlementMaterial(result, "tide-crystal", ProfileText("潮晶", "TIDE CRYSTAL"), plan.TideCrystal);
                AddSettlementMaterial(result, "lamp-glow", ProfileText("灯辉", "LAMP GLOW"), plan.LampGlow);
                AddSettlementMaterial(result, "tide-iron", ProfileText("潮铁", "TIDE IRON"), plan.TideIron);
                AddSettlementMaterial(result, "mist-leather", ProfileText("雾革", "MIST LEATHER"), plan.MistLeather);
                AddSettlementMaterial(result, "star-sand", ProfileText("星砂", "STAR SAND"), plan.StarSand);
                if (clear)
                {
                    AddSettlementMaterial(result, "bounty-ticket", ProfileText("赏金券", "BOUNTY TICKET"),
                        plan.BountyTickets);
                    AddSettlementFinishedItem(result, ProfileText("装备", "EQUIPMENT"), plan.EquipmentQuantity,
                        ExpeditionSettlementFormalArtContract.Equipment);
                    AddSettlementFinishedItem(result, ProfileText("航灯", "LANTERN"), plan.LanternQuantity,
                        ExpeditionSettlementFormalArtContract.Lantern);
                }
            }
            else if (expeditionRewards != null)
            {
                var rewards = expeditionRewards.CreateSnapshot();
                AddSettlementMaterial(result, "tide-crystal", ProfileText("潮晶", "TIDE CRYSTAL"),
                    rewards.TideCrystals);
                if (clear)
                    AddSettlementFinishedItem(result, ProfileText("装备", "EQUIPMENT"),
                        rewards.EquipmentTemplateIds.Count,
                        ExpeditionSettlementFormalArtContract.Equipment);
            }
            return result;
        }

        private static void AddSettlementMaterial(List<SettlementRewardVisual> rewards,
            string kind, string name, int quantity)
        {
            if (quantity <= 0 || rewards.Count >= 8) return;
            rewards.Add(new SettlementRewardVisual
            {
                Name = name,
                Quantity = quantity,
                Root = ExpeditionSettlementFormalArtContract.WarehouseThumbnailRoot,
                ResourceId = ExpeditionSettlementFormalArtContract.WarehouseThumbnail(kind)
            });
        }

        private static void AddSettlementFinishedItem(List<SettlementRewardVisual> rewards,
            string name, int quantity, string resourceId)
        {
            if (quantity <= 0 || rewards.Count >= 8) return;
            rewards.Add(new SettlementRewardVisual
            {
                Name = name,
                Quantity = quantity,
                Root = ExpeditionSettlementFormalArtContract.WarehouseThumbnailRoot,
                ResourceId = resourceId
            });
        }

        private void DrawSettlementRewards(IReadOnlyList<SettlementRewardVisual> rewards, bool clear)
        {
            if (rewards == null || rewards.Count == 0) return;
            var clearX = new[] { 124f, 340f, 556f, 772f };
            var returnX = new[] { 222f, 448f, 674f };
            for (var i = 0; i < rewards.Count && i < 8; i++)
            {
                float x;
                float y;
                if (clear)
                {
                    x = clearX[i % 4];
                    y = i < 4 ? 975f : 1235f;
                }
                else
                {
                    if (i < 3)
                    {
                        x = returnX[i];
                        y = 975f;
                    }
                    else
                    {
                        x = i == 3 ? 335f : 561f;
                        y = 1235f;
                    }
                }
                var reward = rewards[i];
                DrawSettlementTexture(new Rect(x, y, 184f, 184f), reward.Root, reward.ResourceId, 184, 184);
                DrawSettlementLabel(new Rect(x - 8f, y + 184f, 200f, 32f), reward.Name, 22,
                    new Color32(8, 55, 78, 255), TextAnchor.MiddleCenter, true);
                DrawSettlementLabel(new Rect(x - 8f, y + 214f, 200f, 38f), "×" + reward.Quantity.ToString("N0"),
                    28, new Color32(8, 55, 78, 255), TextAnchor.MiddleCenter, false);
            }
        }

        private void DrawSettlementPrimaryAction()
        {
            var area = new Rect(310f, 1530f, 460f, 96f);
            var authoritative = UsesServerAuthoritativeExpedition &&
                                IsCurrentFormalExpeditionRunBound();
            var rewards = expeditionRewards == null ? null : expeditionRewards.CreateSnapshot();
            var canStore = rewards == null || CanStoreExpeditionRewards(rewards);
            var enabled = authoritative
                ? !formalExpeditionSettlementClaimInProgress
                : expeditionRewards != null && (expeditionRewards.IsClaimed || canStore);
            var resourceId = enabled
                ? (IsPointerDown(area) ? ExpeditionSettlementFormalArtContract.ButtonPressed :
                    ExpeditionSettlementFormalArtContract.ButtonNormal)
                : ExpeditionSettlementFormalArtContract.ButtonDisabled;
            DrawSettlementTexture(area, ExpeditionSettlementFormalArtContract.ReusedRoot, resourceId, 460, 96);
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            var label = authoritative ? ProfileText("确认领取", "CONFIRM") :
                expeditionRewards != null && expeditionRewards.IsClaimed
                    ? ProfileText("返回港口", "RETURN TO PORT")
                    : canStore ? ProfileText("确认领取", "CONFIRM") :
                        ProfileText("仓库已满", "WAREHOUSE FULL");
            DrawSettlementLabel(area, label, 32,
                enabled ? new Color32(250, 244, 218, 255) : new Color32(135, 145, 145, 255),
                TextAnchor.MiddleCenter, true);
            if (!clicked) return;
            if (authoritative)
            {
                PlayUiClick(UiAudioCueIds.Reward, "run-result-authoritative-claim");
                ClaimFormalSettlementAndReturnHomeAsync();
            }
            else if (expeditionRewards.IsClaimed)
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-result-return");
                RestartRun();
            }
            else if (ClaimExpeditionRewards())
            {
                PlayUiClick(UiAudioCueIds.Reward, "run-result-claim");
                currentView = ScreenView.Home;
            }
        }

        private void DrawSettlementLabel(Rect area, string text, int size, Color color,
            TextAnchor anchor, bool bold)
        {
            var style = ArchiveStyle(size, color, anchor, bold);
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            GUI.Label(area, text, style);
        }

        private void DrawSettlementTexture(Rect area, string root, string resourceId,
            int width, int height)
        {
            if (string.IsNullOrEmpty(resourceId)) return;
            var key = root + resourceId;
            Texture2D texture;
            if (!expeditionSettlementTextures.TryGetValue(key, out texture))
            {
                texture = Resources.Load<Texture2D>(key);
                expeditionSettlementTextures[key] = texture;
            }
            if (texture == null || texture.width != width || texture.height != height ||
                Mathf.RoundToInt(area.width) != width || Mathf.RoundToInt(area.height) != height)
            {
                Debug.LogError("Rejected missing or resized expedition settlement asset: " + key);
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
        }
    }
}
