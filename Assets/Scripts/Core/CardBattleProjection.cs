using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    [Serializable]
    public sealed class CardBattleUnitSnapshot
    {
        public string Id;
        public int Health;
        public int Shield;
        public bool IsDefeated;
        public Dictionary<string, string> Statuses = new Dictionary<string, string>(StringComparer.Ordinal);

        public CardBattleUnitSnapshot Copy()
        {
            var copy = new CardBattleUnitSnapshot {
                Id = Id, Health = Health, Shield = Shield, IsDefeated = IsDefeated
            };
            if (Statuses != null)
                foreach (var status in Statuses) copy.Statuses[status.Key] = status.Value;
            return copy;
        }
    }

    /// <summary>
    /// Applies only authoritative card battle events to a presentation snapshot.
    /// Projectile and status feedback events never calculate damage or healing.
    /// </summary>
    public sealed class CardBattleProjection
    {
        private readonly CardBattleEventConsumer consumer;
        private readonly Dictionary<string, CardBattleUnitSnapshot> unitsById =
            new Dictionary<string, CardBattleUnitSnapshot>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> pendingProjectiles =
            new Dictionary<string, long>(StringComparer.Ordinal);

        public CardBattleProjection(string battleId)
        {
            consumer = new CardBattleEventConsumer(battleId);
        }

        public long LastConfirmedEventSequence { get { return consumer.LastConfirmedEventSequence; } }

        public CardBattleUnitSnapshot FindUnit(string unitId)
        {
            CardBattleUnitSnapshot unit;
            return unitId != null && unitsById.TryGetValue(unitId, out unit) ? unit : null;
        }

        public IReadOnlyList<CardBattleUnitSnapshot> Units
        {
            get { return new List<CardBattleUnitSnapshot>(unitsById.Values).AsReadOnly(); }
        }

        public IReadOnlyDictionary<string, long> PendingProjectiles
        {
            get { return new Dictionary<string, long>(pendingProjectiles); }
        }

        public void SetInitialSnapshot(IEnumerable<CardBattleUnitSnapshot> snapshot)
        {
            unitsById.Clear();
            if (snapshot == null) return;
            foreach (var unit in snapshot)
            {
                if (unit == null || string.IsNullOrEmpty(unit.Id))
                    throw new ArgumentException("Snapshot contains an invalid unit.", "snapshot");
                if (unitsById.ContainsKey(unit.Id))
                    throw new ArgumentException("Snapshot contains a duplicate unit.", "snapshot");
                unitsById.Add(unit.Id, unit.Copy());
            }
        }

        public CardBattleEventAcceptResult Consume(CardBattleEvent battleEvent)
        {
            if (battleEvent != null && battleEvent.EventSequence <= LastConfirmedEventSequence)
                return consumer.Consume(battleEvent);
            if (!HasKnownTargets(battleEvent)) return CardBattleEventAcceptResult.Rejected;
            var result = consumer.Consume(battleEvent);
            if (result != CardBattleEventAcceptResult.Applied) return result;
            ApplyAuthoritativeEvent(battleEvent);
            return result;
        }

        public void SettleWithoutFlight(string projectileId)
        {
            if (!string.IsNullOrEmpty(projectileId)) pendingProjectiles.Remove(projectileId);
        }

        public void SkipToSnapshot(long eventSequence, IEnumerable<CardBattleUnitSnapshot> snapshot)
        {
            consumer.ReconcileToSnapshot(eventSequence);
            pendingProjectiles.Clear();
            SetInitialSnapshot(snapshot);
        }

        private bool HasKnownTargets(CardBattleEvent battleEvent)
        {
            if (battleEvent == null) return false;
            if (battleEvent.Kind == CardBattleEventKind.ImpactResolved)
            {
                if (battleEvent.Results == null) return false;
                foreach (var result in battleEvent.Results)
                    if (result == null || FindUnit(result.TargetId) == null) return false;
            }
            if (battleEvent.Kind == CardBattleEventKind.StatusFeedback && FindUnit(battleEvent.TargetId) == null)
                return false;
            return true;
        }

        private void ApplyAuthoritativeEvent(CardBattleEvent battleEvent)
        {
            if (battleEvent.Kind == CardBattleEventKind.ProjectileReleased)
            {
                if (!string.IsNullOrEmpty(battleEvent.ProjectileId))
                    pendingProjectiles[battleEvent.ProjectileId] = battleEvent.EventSequence;
                return;
            }
            if (battleEvent.Kind == CardBattleEventKind.ImpactResolved)
            {
                foreach (var result in battleEvent.Results)
                {
                    var unit = FindUnit(result.TargetId);
                    unit.Health = result.TargetHealthAfter;
                    unit.Shield = result.TargetShieldAfter;
                    unit.IsDefeated = result.IsDefeated || unit.Health <= 0;
                }
                return;
            }
            if (battleEvent.Kind == CardBattleEventKind.StatusFeedback)
            {
                var unit = FindUnit(battleEvent.TargetId);
                unit.Statuses[battleEvent.StatusKey] = battleEvent.StatusValueAfter;
            }
        }
    }
}
