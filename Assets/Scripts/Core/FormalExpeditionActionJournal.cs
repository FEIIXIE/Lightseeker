using System;
using System.Collections.Generic;
using System.Globalization;

namespace SeekerAutoChess.Core
{
    public enum FormalExpeditionBattleWinner
    {
        Player = 0,
        Enemy = 1
    }

    /// <summary>
    /// Strongly typed writer for the frozen expedition replay action schema.
    /// Gameplay code supplies only authoritative identifiers and hashes; this class
    /// does not invent state, resolve combat, issue proofs, or grant rewards.
    /// </summary>
    public sealed class FormalExpeditionActionJournal
    {
        private readonly ExpeditionReplayRecorder recorder;
        private readonly Action<ExpeditionReplayRecorderState> persistState;

        internal FormalExpeditionActionJournal(
            ExpeditionReplayRecorder recorder,
            Action<ExpeditionReplayRecorderState> persistState = null)
        {
            this.recorder = recorder ?? throw new ArgumentNullException(nameof(recorder));
            this.persistState = persistState;
        }

        public int ActionCount
        {
            get { return recorder.ActionCount; }
        }

        public ExpeditionReplayActionRecord RecordRouteSelection(
            string actionId, int expectedStateVersion, string nodeId,
            string destinationNodeId)
        {
            return Record(actionId, "route.select", expectedStateVersion, nodeId,
                Field("destinationNodeId", destinationNodeId));
        }

        public ExpeditionReplayActionRecord RecordShopRefresh(
            string actionId, int expectedStateVersion, string nodeId,
            string refreshId)
        {
            return Record(actionId, "shop.refresh", expectedStateVersion, nodeId,
                Field("refreshId", refreshId));
        }

        public ExpeditionReplayActionRecord RecordShopPurchase(
            string actionId, int expectedStateVersion, string nodeId,
            string offerId, string runUnitId)
        {
            return Record(actionId, "shop.buy", expectedStateVersion, nodeId,
                Fields("offerId", offerId, "runUnitId", runUnitId));
        }

        public ExpeditionReplayActionRecord RecordMerchantPurchase(
            string actionId, int expectedStateVersion, string nodeId,
            string offerId, string itemInstanceId)
        {
            return Record(actionId, "merchant.buy", expectedStateVersion, nodeId,
                Fields("offerId", offerId, "itemInstanceId", itemInstanceId));
        }

        public ExpeditionReplayActionRecord RecordRosterSale(
            string actionId, int expectedStateVersion, string nodeId,
            string runUnitId)
        {
            return Record(actionId, "roster.sell", expectedStateVersion, nodeId,
                Field("runUnitId", runUnitId));
        }

        public ExpeditionReplayActionRecord RecordRosterMove(
            string actionId, int expectedStateVersion, string nodeId,
            string runUnitId, string destinationSlotId)
        {
            return Record(actionId, "roster.move", expectedStateVersion, nodeId,
                Fields("runUnitId", runUnitId,
                    "destinationSlotId", destinationSlotId));
        }

        public ExpeditionReplayActionRecord RecordRosterMerge(
            string actionId, int expectedStateVersion, string nodeId,
            string sourceUnitIdA, string sourceUnitIdB, string sourceUnitIdC,
            string resultUnitId)
        {
            return Record(actionId, "roster.merge", expectedStateVersion, nodeId,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "sourceUnitIdA", sourceUnitIdA },
                    { "sourceUnitIdB", sourceUnitIdB },
                    { "sourceUnitIdC", sourceUnitIdC },
                    { "resultUnitId", resultUnitId }
                });
        }

        public ExpeditionReplayActionRecord RecordItemBinding(
            string actionId, int expectedStateVersion, string nodeId,
            string itemInstanceId, string runUnitId, string slotId)
        {
            return Record(actionId, "item.bind", expectedStateVersion, nodeId,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "itemInstanceId", itemInstanceId },
                    { "runUnitId", runUnitId },
                    { "slotId", slotId }
                });
        }

        public ExpeditionReplayActionRecord RecordItemDiscard(
            string actionId, int expectedStateVersion, string nodeId,
            string itemInstanceId)
        {
            return Record(actionId, "item.discard", expectedStateVersion, nodeId,
                Field("itemInstanceId", itemInstanceId));
        }

        public ExpeditionReplayActionRecord RecordNodeChoice(
            string actionId, int expectedStateVersion, string nodeId,
            string choiceId)
        {
            return Record(actionId, "node.choice", expectedStateVersion, nodeId,
                Field("choiceId", choiceId));
        }

        public ExpeditionReplayActionRecord RecordUpgradeSelection(
            string actionId, int expectedStateVersion, string nodeId,
            string upgradeId)
        {
            return Record(actionId, "upgrade.select", expectedStateVersion, nodeId,
                Field("upgradeId", upgradeId));
        }

        public ExpeditionReplayActionRecord RecordBattleResolution(
            string actionId, int expectedStateVersion, string nodeId,
            string encounterId, uint battleSeed, string battleInputSha256,
            string battleEventSha256, FormalExpeditionBattleWinner winner)
        {
            return RecordBattleResolution(
                actionId, expectedStateVersion, nodeId, encounterId, battleSeed,
                battleInputSha256, battleEventSha256, winner, true);
        }

        public ExpeditionReplayActionRecord RecordBattleResolutionDeferred(
            string actionId, int expectedStateVersion, string nodeId,
            string encounterId, uint battleSeed, string battleInputSha256,
            string battleEventSha256, FormalExpeditionBattleWinner winner)
        {
            return RecordBattleResolution(
                actionId, expectedStateVersion, nodeId, encounterId, battleSeed,
                battleInputSha256, battleEventSha256, winner, false);
        }

        public void PersistCurrentState()
        {
            if (persistState != null) persistState(recorder.CaptureState());
        }

        private ExpeditionReplayActionRecord RecordBattleResolution(
            string actionId, int expectedStateVersion, string nodeId,
            string encounterId, uint battleSeed, string battleInputSha256,
            string battleEventSha256, FormalExpeditionBattleWinner winner,
            bool persistImmediately)
        {
            string winnerText;
            switch (winner)
            {
                case FormalExpeditionBattleWinner.Player:
                    winnerText = "player";
                    break;
                case FormalExpeditionBattleWinner.Enemy:
                    winnerText = "enemy";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(winner));
            }

            return Record(actionId, "battle.resolve", expectedStateVersion, nodeId,
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    { "encounterId", encounterId },
                    { "battleSeed", battleSeed.ToString(CultureInfo.InvariantCulture) },
                    { "battleInputSha256", battleInputSha256 },
                    { "battleEventSha256", battleEventSha256 },
                    { "winner", winnerText }
                }, persistImmediately);
        }

        public ExpeditionReplayRecorderState CaptureState()
        {
            return recorder.CaptureState();
        }

        public ExpeditionReplayEnvelopeRecord CreateCheckpointEnvelope()
        {
            return recorder.CreateCheckpointEnvelope();
        }

        private ExpeditionReplayActionRecord Record(
            string actionId, string kind, int expectedStateVersion, string nodeId,
            IDictionary<string, string> fields, bool persistImmediately = true)
        {
            var recorded = recorder.Record(
                actionId, kind, expectedStateVersion, nodeId, fields);
            if (persistImmediately) PersistCurrentState();
            return recorded;
        }

        private static Dictionary<string, string> Field(
            string key, string value)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { key, value }
            };
        }

        private static Dictionary<string, string> Fields(
            string firstKey, string firstValue, string secondKey, string secondValue)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { firstKey, firstValue },
                { secondKey, secondValue }
            };
        }
    }
}
