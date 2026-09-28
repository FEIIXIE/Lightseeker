using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class ExpeditionRewardSnapshot
    {
        private readonly string[] equipmentTemplateIds;
        private readonly string[] completedNodeIds;

        public int TideCrystals { get; }
        public int Stone { get; }
        public int LampOil { get; }
        public int MistLens { get; }
        public IReadOnlyList<string> EquipmentTemplateIds { get { return equipmentTemplateIds; } }
        public IReadOnlyList<string> CompletedNodeIds { get { return completedNodeIds; } }

        public ExpeditionRewardSnapshot(
            int tideCrystals,
            int stone,
            int lampOil,
            int mistLens,
            IEnumerable<string> equipmentTemplateIds)
            : this(tideCrystals, stone, lampOil, mistLens, equipmentTemplateIds, null)
        {
        }

        public ExpeditionRewardSnapshot(
            int tideCrystals,
            int stone,
            int lampOil,
            int mistLens,
            IEnumerable<string> equipmentTemplateIds,
            IEnumerable<string> completedNodeIds)
        {
            if (tideCrystals < 0 || stone < 0 || lampOil < 0 || mistLens < 0)
                throw new ArgumentOutOfRangeException(nameof(tideCrystals));
            TideCrystals = tideCrystals;
            Stone = stone;
            LampOil = lampOil;
            MistLens = mistLens;
            this.equipmentTemplateIds = equipmentTemplateIds == null
                ? new string[0]
                : new List<string>(equipmentTemplateIds).ToArray();
            this.completedNodeIds = completedNodeIds == null
                ? new string[0]
                : new List<string>(completedNodeIds).ToArray();
        }
    }

    /// <summary>
    /// Tracks only rewards earned by completed expedition nodes. Claiming is
    /// deliberately one-shot so returning to the result screen cannot duplicate loot.
    /// </summary>
    public sealed class ExpeditionRewardLedger
    {
        private readonly List<string> equipmentTemplateIds = new List<string>();
        private readonly List<string> completedNodeIds = new List<string>();
        private readonly HashSet<string> completedNodeIdSet =
            new HashSet<string>(StringComparer.Ordinal);

        public int TideCrystals { get; private set; }
        public int Stone { get; private set; }
        public int LampOil { get; private set; }
        public int MistLens { get; private set; }
        public bool IsClaimed { get; private set; }
        public IReadOnlyList<string> EquipmentTemplateIds { get { return equipmentTemplateIds; } }
        public IReadOnlyList<string> CompletedNodeIds { get { return completedNodeIds; } }

        public bool HasCompletedNode(string nodeId)
        {
            return !string.IsNullOrWhiteSpace(nodeId) && completedNodeIdSet.Contains(nodeId);
        }

        /// <summary>
        /// Atomically records the reward for one completed node. Visible but unchosen
        /// candidates never reach this method, and replaying the same node is rejected.
        /// </summary>
        public bool TryRecordCompletedNode(
            string nodeId,
            int tideCrystals,
            int stone,
            int lampOil,
            int mistLens,
            string equipmentTemplateId = null)
        {
            if (IsClaimed || string.IsNullOrWhiteSpace(nodeId) ||
                completedNodeIdSet.Contains(nodeId) || tideCrystals < 0 || stone < 0 ||
                lampOil < 0 || mistLens < 0)
                return false;

            completedNodeIdSet.Add(nodeId);
            completedNodeIds.Add(nodeId);
            TideCrystals += tideCrystals;
            Stone += stone;
            LampOil += lampOil;
            MistLens += mistLens;
            if (!string.IsNullOrWhiteSpace(equipmentTemplateId))
                equipmentTemplateIds.Add(equipmentTemplateId);
            return true;
        }

        public bool TryAddCurrencyAndMaterials(int tideCrystals, int stone, int lampOil, int mistLens)
        {
            if (IsClaimed || tideCrystals < 0 || stone < 0 || lampOil < 0 || mistLens < 0) return false;
            TideCrystals += tideCrystals;
            Stone += stone;
            LampOil += lampOil;
            MistLens += mistLens;
            return true;
        }

        public bool TryAddEquipment(string templateId)
        {
            if (IsClaimed || string.IsNullOrWhiteSpace(templateId)) return false;
            equipmentTemplateIds.Add(templateId);
            return true;
        }

        public bool TryClaim(out ExpeditionRewardSnapshot snapshot)
        {
            snapshot = null;
            if (IsClaimed) return false;
            IsClaimed = true;
            snapshot = CreateSnapshot();
            return true;
        }

        public ExpeditionRewardSnapshot CreateSnapshot()
        {
            return new ExpeditionRewardSnapshot(
                TideCrystals, Stone, LampOil, MistLens, equipmentTemplateIds, completedNodeIds);
        }

        public void Restore(
            int tideCrystals,
            int stone,
            int lampOil,
            int mistLens,
            IEnumerable<string> equipmentTemplates,
            bool isClaimed)
        {
            Restore(tideCrystals, stone, lampOil, mistLens,
                equipmentTemplates, null, isClaimed);
        }

        public void Restore(
            int tideCrystals,
            int stone,
            int lampOil,
            int mistLens,
            IEnumerable<string> equipmentTemplates,
            IEnumerable<string> completedNodes,
            bool isClaimed)
        {
            TideCrystals = Math.Max(0, tideCrystals);
            Stone = Math.Max(0, stone);
            LampOil = Math.Max(0, lampOil);
            MistLens = Math.Max(0, mistLens);
            equipmentTemplateIds.Clear();
            if (equipmentTemplates != null)
            {
                foreach (var templateId in equipmentTemplates)
                {
                    if (!string.IsNullOrWhiteSpace(templateId)) equipmentTemplateIds.Add(templateId);
                }
            }
            completedNodeIds.Clear();
            completedNodeIdSet.Clear();
            if (completedNodes != null)
            {
                foreach (var nodeId in completedNodes)
                {
                    if (string.IsNullOrWhiteSpace(nodeId) || !completedNodeIdSet.Add(nodeId)) continue;
                    completedNodeIds.Add(nodeId);
                }
            }
            IsClaimed = isClaimed;
        }

        public void Reset()
        {
            TideCrystals = 0;
            Stone = 0;
            LampOil = 0;
            MistLens = 0;
            equipmentTemplateIds.Clear();
            completedNodeIds.Clear();
            completedNodeIdSet.Clear();
            IsClaimed = false;
        }
    }
}
