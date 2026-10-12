using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private ExpeditionServerSettlementPlan pendingLocalExpeditionSettlementPlan;
        private int localLampGlowBalance;
        private int localTideIronBalance;
        private int localMistLeatherBalance;
        private int localStarSandBalance;
        private int localBountyTicketBalance;

        private ExpeditionServerSettlementPlan EnsureLocalExpeditionSettlementPlan()
        {
            if (!CompetitionDemoMode) return null;
            if (pendingLocalExpeditionSettlementPlan != null)
                return pendingLocalExpeditionSettlementPlan;
            var clear = runWon && !runAbandoned;
            var deepestNodeId = DeepestCompletedSettlementNodeId();
            pendingLocalExpeditionSettlementPlan =
                CompetitionDemoSettlementRules.CreateD0Plan(runId, clear, deepestNodeId);

            if (expeditionRewards != null && !expeditionRewards.IsClaimed)
            {
                var snapshot = expeditionRewards.CreateSnapshot();
                expeditionRewards.Restore(
                    pendingLocalExpeditionSettlementPlan.TideCrystal,
                    clear ? snapshot.EquipmentTemplateIds : new string[0],
                    snapshot.CompletedNodeIds,
                    false);
            }
            return pendingLocalExpeditionSettlementPlan;
        }

        private void PrepareLocalExpeditionSettlement(bool bossCleared)
        {
            if (!CompetitionDemoMode) return;
            runWon = bossCleared;
            if (bossCleared) runAbandoned = false;
            pendingLocalExpeditionSettlementPlan = null;
            EnsureLocalExpeditionSettlementPlan();
        }

        private string DeepestCompletedSettlementNodeId()
        {
            if (expeditionRewards == null) return "M00";
            var deepestId = "M00";
            var deepestRank = 0;
            foreach (var nodeId in expeditionRewards.CompletedNodeIds)
            {
                var rank = CompetitionDemoSettlementRules.ProgressRank(nodeId);
                if (rank <= deepestRank) continue;
                deepestRank = rank;
                deepestId = nodeId;
            }
            return deepestId;
        }

        private void CreditLocalSettlementMaterials(ExpeditionServerSettlementPlan plan)
        {
            if (plan == null) return;
            localLampGlowBalance += Math.Max(0, plan.LampGlow);
            localTideIronBalance += Math.Max(0, plan.TideIron);
            localMistLeatherBalance += Math.Max(0, plan.MistLeather);
            localStarSandBalance += Math.Max(0, plan.StarSand);
            localBountyTicketBalance += Math.Max(0, plan.BountyTickets);
        }
    }
}
