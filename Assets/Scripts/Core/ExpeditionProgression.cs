using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    /// <summary>
    /// Applies node completion as one atomic rule and exposes the actual travelled
    /// path for map presentation. A retry, reconnect or duplicate callback for the
    /// same node cannot grant its rewards twice.
    /// </summary>
    public static class ExpeditionProgression
    {
        public static bool TryCompleteNode(
            ExpeditionRewardLedger ledger,
            string nodeId,
            int tideCrystals,
            string equipmentTemplateId = null)
        {
            if (ledger == null) throw new ArgumentNullException(nameof(ledger));
            if (!ledger.TryRecordCompletedNode(
                    nodeId, tideCrystals, equipmentTemplateId))
                return false;
            return true;
        }

        public static bool IsTraversedEdge(
            IReadOnlyList<string> completedNodeIds,
            string currentNodeId,
            string sourceNodeId,
            string targetNodeId)
        {
            if (completedNodeIds == null || string.IsNullOrWhiteSpace(sourceNodeId) ||
                string.IsNullOrWhiteSpace(targetNodeId)) return false;

            for (var index = 1; index < completedNodeIds.Count; index++)
            {
                if (string.Equals(completedNodeIds[index - 1], sourceNodeId,
                        StringComparison.Ordinal) &&
                    string.Equals(completedNodeIds[index], targetNodeId,
                        StringComparison.Ordinal)) return true;
            }

            // M00 is the expedition origin rather than a resolvable encounter, so it
            // is intentionally absent from the completion ledger. Restore the missing
            // first leg from M00 to the first completed/current destination here.
            if (string.Equals(sourceNodeId, "M00", StringComparison.Ordinal))
            {
                if (completedNodeIds.Count > 0 &&
                    string.Equals(completedNodeIds[0], targetNodeId, StringComparison.Ordinal))
                    return true;
                if (completedNodeIds.Count == 0 &&
                    string.Equals(currentNodeId, targetNodeId, StringComparison.Ordinal))
                    return true;
            }

            // Selecting a destination means the party has already travelled from the
            // last completed node to the current node, even before that destination is
            // resolved. This keeps the latest leg solid after save/restore as well.
            return completedNodeIds.Count > 0 &&
                   string.Equals(completedNodeIds[completedNodeIds.Count - 1], sourceNodeId,
                       StringComparison.Ordinal) &&
                   string.Equals(currentNodeId, targetNodeId, StringComparison.Ordinal);
        }
    }
}
