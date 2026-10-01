using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    public static class C01C05ActiveSkillContract
    {
        public const string D2AttackSpeedBlocked = "ATTACK_SPEED_COMPONENT_BLOCKED";
        public const string EventLogPath = "outputs/logs/c01-c05-active-skill-events.jsonl";

        public static bool IsC01ToC05(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId)) return false;
            return IsC01Ability(abilityId) || IsC02Ability(abilityId) || IsC03Ability(abilityId) ||
                   IsC04Ability(abilityId) ||
                   C05ActiveSkillIntegration.IsC05Ability(abilityId) ||
                   abilityId.StartsWith("C01", StringComparison.Ordinal) ||
                   abilityId.StartsWith("C02", StringComparison.Ordinal) ||
                   abilityId.StartsWith("C03", StringComparison.Ordinal) ||
                   abilityId.StartsWith("C04", StringComparison.Ordinal) ||
                   abilityId.StartsWith("C05", StringComparison.Ordinal);
        }

        private static bool IsC01Ability(string abilityId)
        {
            return string.Equals(abilityId, "S_OLIN_OPEN_DAWN", StringComparison.Ordinal) ||
                   string.Equals(abilityId, "P_OLIN_TIDE", StringComparison.Ordinal) ||
                   abilityId.StartsWith("C01", StringComparison.Ordinal);
        }

        private static bool IsC02Ability(string abilityId)
        {
            return string.Equals(abilityId, "S_YIWEN_RETURN_BEACON", StringComparison.Ordinal) ||
                   string.Equals(abilityId, "P_YIWEN_WAITING", StringComparison.Ordinal);
        }

        private static bool IsC03Ability(string abilityId)
        {
            return string.Equals(abilityId, C03ActiveSkillIntegration.WhiteTrailAbilityId,
                       StringComparison.Ordinal) ||
                   string.Equals(abilityId, C03ActiveSkillIntegration.TransferAbilityId,
                       StringComparison.Ordinal);
        }

        private static bool IsC04Ability(string abilityId)
        {
            return C04ActiveSkillIntegration.IsC04Ability(abilityId);
        }

        public static string ValidateRule(CardBattlePresentationRule rule)
        {
            if (rule == null || !IsC01ToC05(rule.AbilityId)) return null;
            // Continuous attack speed is active for all current skill contracts.
            // Keep the legacy constant for old replay data, but never block a new rule.
            if (IsC01Ability(rule.AbilityId)) return null;
            var prefix = IsC02Ability(rule.AbilityId) ? "C02" :
                IsC03Ability(rule.AbilityId) ? "C03" :
                IsC04Ability(rule.AbilityId) ? "C04" :
                C05ActiveSkillIntegration.IsC05Ability(rule.AbilityId) ? "C05" :
                rule.AbilityId.Substring(0, 3);
            if (prefix == "C01" && (rule.MaxTargetCount < 2 ||
                rule.ImpactEmissionMode != CardBattleImpactEmissionMode.SingleEventMultiTarget))
                return "C01_REQUIRES_MULTI_TARGET_RANGE_EVENT";
            if (string.Equals(rule.AbilityId, "S_YIWEN_RETURN_BEACON", StringComparison.Ordinal) &&
                rule.DeliveryMode != CardBattleDeliveryMode.InstantTarget)
                return "C02_ACTIVE_REQUIRES_INSTANT_TARGET_DELIVERY";
            if (string.Equals(rule.AbilityId, "P_YIWEN_WAITING", StringComparison.Ordinal) &&
                rule.DeliveryMode != CardBattleDeliveryMode.Beam)
                return "C02_PASSIVE_REQUIRES_BEAM_DELIVERY";
            if (prefix == "C02" &&
                !string.Equals(rule.AbilityId, "S_YIWEN_RETURN_BEACON", StringComparison.Ordinal) &&
                rule.DeliveryMode != CardBattleDeliveryMode.Beam)
                return "C02_REQUIRES_BEAM_DELIVERY";
            if (prefix == "C04" && rule.MaxTargetCount > C04ActiveSkillIntegration.HiddenSpringTargetCount)
                return "C04_MAX_TWO_TARGETS";
            if (prefix == "C05" && (rule.TargetRule == null ||
                rule.TargetRule.IndexOf("column", StringComparison.OrdinalIgnoreCase) < 0))
                return "C05_REQUIRES_COLUMN_BOUND_TARGET";
            if (prefix == "C05" && rule.DeliveryMode != CardBattleDeliveryMode.GroundPulse)
                return "C05_REQUIRES_GROUND_PULSE";
            return null;
        }

        public static string ValidateImpact(CardBattlePresentationRule rule,
            IList<CardBattleTargetResult> results)
        {
            if (rule == null || !IsC01ToC05(rule.AbilityId)) return null;
            if (results == null || results.Count == 0) return "IMPACT_REQUIRES_TARGET";
            var prefix = rule.AbilityId.StartsWith("S_OLIN_OPEN_DAWN", StringComparison.Ordinal)
                ? "C01"
                : IsC02Ability(rule.AbilityId) ? "C02"
                : IsC03Ability(rule.AbilityId) ? "C03"
                : IsC04Ability(rule.AbilityId) ? "C04" :
                C05ActiveSkillIntegration.IsC05Ability(rule.AbilityId) ? "C05" :
                rule.AbilityId.Substring(0, 3);
            var targets = new HashSet<string>(StringComparer.Ordinal);
            foreach (var result in results)
            {
                if (result == null || string.IsNullOrEmpty(result.TargetId)) return "TARGET_REQUIRED";
                if (!targets.Add(result.TargetId)) return "DUPLICATE_TARGET";
                if (result.StatusChanges == null) continue;
                if (prefix == "C01" && result.StatusChanges.Any(change =>
                    string.Equals(change, "shield-break:recursive", StringComparison.Ordinal)))
                    return "C01_SHIELD_BREAK_MUST_NOT_RECURSE";
                if (prefix == "C02" && result.Healing > 0 && result.TargetHealthAfter < 1)
                    return "C02_BEAM_MUST_RETAIN_ONE_HEALTH_BEFORE_HEAL";
                if (prefix == "C03" && result.IsDefeated && result.StatusChanges.Any(change =>
                    string.Equals(change, "white-mark:transfer", StringComparison.Ordinal)))
                    return "C03_DIRECT_KILL_MUST_NOT_TRANSFER_WHITE_MARK";
            }
            if (prefix == "C04" && results.Count > C04ActiveSkillIntegration.HiddenSpringTargetCount)
                return "C04_MAX_TWO_TARGETS";
            return null;
        }

        public static bool HasWhiteMarkTransfer(IList<CardBattleTargetResult> results)
        {
            if (results == null) return false;
            return results.Any(result => result != null && result.StatusChanges != null &&
                result.StatusChanges.Any(change => string.Equals(change, "white-mark:transfer", StringComparison.Ordinal)));
        }
    }

    public sealed class C01C05ReconnectState
    {
        public long EventSequence;
        public string StateHash;
        public IReadOnlyList<CardBattleUnitSnapshot> Snapshot;
    }

    public sealed class C01C05ActiveSkillEventPipeline
    {
        private readonly CardBattleEventStream stream;
        private readonly CardBattleProjection projection;
        private readonly List<string> eventLog = new List<string>();

        public C01C05ActiveSkillEventPipeline(string battleId, IEnumerable<CardBattleUnitSnapshot> initialSnapshot)
        {
            stream = new CardBattleEventStream(battleId);
            projection = new CardBattleProjection(battleId);
            projection.SetInitialSnapshot(initialSnapshot);
        }

        public IReadOnlyList<CardBattleEvent> ProducedEvents { get { return stream.Events; } }
        public IReadOnlyList<string> EventLog { get { return eventLog.AsReadOnly(); } }
        public CardBattleProjection Projection { get { return projection; } }
        public long LastConfirmedEventSequence { get { return projection.LastConfirmedEventSequence; } }
        public string FinalStateHash { get { return ComputeStateHash(projection.Units); } }
        public const string TestEntry = "C01-C05 Active Skill Graybox / Event Log";

        public CardBattleActionHandle BeginAction(string unitId, string actionId,
            CardBattlePresentationRule rule, long startedAt)
        {
            var error = C01C05ActiveSkillContract.ValidateRule(rule);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            return stream.BeginAction(unitId, actionId, rule, startedAt);
        }

        public void ReleaseProjectile(CardBattleActionHandle action, string projectileId,
            string originAnchor, string targetAnchor, string profileId, int travelMs)
        { stream.ReleaseProjectile(action, projectileId, originAnchor, targetAnchor, profileId, travelMs); }

        public void ResolveImpact(CardBattleActionHandle action, string impactId, long resolvedAt,
            IList<CardBattleTargetResult> results)
        {
            var error = C01C05ActiveSkillContract.ValidateImpact(action.Rule, results);
            if (!string.IsNullOrEmpty(error)) throw new InvalidOperationException(error);
            foreach (var result in results)
            {
                var target = projection.FindUnit(result.TargetId);
                if (target == null || target.IsDefeated)
                    throw new InvalidOperationException("TARGET_INVALID_BEFORE_IMPACT");
            }
            stream.ResolveImpact(action, impactId, resolvedAt, results);
        }

        public void AddStatusFeedback(CardBattleActionHandle action, string targetId,
            string statusKey, string before, string after, int priority)
        { stream.AddStatusFeedback(action, targetId, statusKey, before, after, priority); }

        public void ExpireStatus(CardBattleActionHandle action, string targetId,
            string statusKey, string before, int priority)
        { AddStatusFeedback(action, targetId, statusKey, before, string.Empty, priority); }

        public void CompleteAction(CardBattleActionHandle action, long completedAt,
            CardBattleActionOutcome outcome, CardBattleCancelReason cancelReason,
            CardBattleInterruptReason interruptReason)
        { stream.CompleteAction(action, completedAt, outcome, cancelReason, interruptReason); }

        public CardBattleEventAcceptResult Consume(CardBattleEvent battleEvent)
        {
            var result = projection.Consume(battleEvent);
            if (result == CardBattleEventAcceptResult.Applied)
            {
                battleEvent.StateHashAfter = FinalStateHash;
                eventLog.Add(SerializeLogLine(battleEvent));
            }
            return result;
        }

        public void ConsumeProducedEvents()
        {
            foreach (var battleEvent in stream.Events) Consume(battleEvent);
        }

        public void SettleForSkipOrAcceleration(long eventSequence,
            IEnumerable<CardBattleUnitSnapshot> authoritativeSnapshot)
        {
            projection.SkipToSnapshot(eventSequence, authoritativeSnapshot);
            eventLog.Add("{\"kind\":\"settled\",\"eventSequence\":" + eventSequence +
                ",\"stateHash\":\"" + FinalStateHash + "\"}");
        }

        public void SettleWithoutFlight(string projectileId)
        { projection.SettleWithoutFlight(projectileId); }

        public C01C05ReconnectState CaptureReconnectState()
        {
            return new C01C05ReconnectState {
                EventSequence = LastConfirmedEventSequence, StateHash = FinalStateHash,
                Snapshot = projection.Units.Select(unit => unit.Copy()).ToList().AsReadOnly()
            };
        }

        public void RestoreReconnectState(C01C05ReconnectState state)
        {
            if (state == null) throw new ArgumentNullException("state");
            projection.SkipToSnapshot(state.EventSequence, state.Snapshot);
            if (!string.Equals(FinalStateHash, state.StateHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Reconnect snapshot hash mismatch.");
            eventLog.Add("{\"kind\":\"reconnected\",\"eventSequence\":" + state.EventSequence +
                ",\"stateHash\":\"" + FinalStateHash + "\"}");
        }

        public List<string> ValidateProducedEvents()
        {
            var errors = CardBattleEventIntegrity.Validate(stream.Events);
            foreach (var battleEvent in stream.Events)
            {
                if (battleEvent.Kind != CardBattleEventKind.ImpactResolved) continue;
                var rule = new CardBattlePresentationRule { AbilityId = battleEvent.AbilityId,
                    TargetRule = "validated", MaxTargetCount = battleEvent.Results.Count,
                    DeliveryMode = battleEvent.DeliveryMode,
                    ImpactEmissionMode = battleEvent.Results.Count > 1
                        ? CardBattleImpactEmissionMode.SingleEventMultiTarget
                        : CardBattleImpactEmissionMode.SingleEventSingleTarget };
                var error = C01C05ActiveSkillContract.ValidateImpact(rule, battleEvent.Results);
                if (!string.IsNullOrEmpty(error)) errors.Add(error);
            }
            return errors;
        }

        private static string SerializeLogLine(CardBattleEvent battleEvent)
        {
            var childIndex = battleEvent.Results != null && battleEvent.Results.Count > 0
                ? battleEvent.Results[0].ChildIndex : 0;
            return "{\"kind\":\"" + battleEvent.Kind + "\",\"actionId\":\"" +
                (battleEvent.ActionId ?? string.Empty) + "\",\"sourceId\":\"" +
                (battleEvent.SourceId ?? battleEvent.UnitId ?? string.Empty) + "\",\"targetId\":\"" +
                (battleEvent.TargetId ?? string.Empty) + "\",\"abilityId\":\"" +
                (battleEvent.AbilityId ?? string.Empty) + "\",\"eventSequence\":" +
                battleEvent.EventSequence + ",\"childIndex\":" + childIndex +
                ",\"authorityTick\":" + battleEvent.AuthorityTick + ",\"outcome\":\"" +
                battleEvent.Outcome + "\",\"childCount\":" +
                (battleEvent.Results == null ? 0 : battleEvent.Results.Count) +
                ",\"stateHash\":\"" + (battleEvent.StateHashAfter ?? string.Empty) + "\"}";
        }

        public static string ComputeStateHash(IEnumerable<CardBattleUnitSnapshot> units)
        {
            var canonical = new StringBuilder();
            foreach (var unit in (units ?? new List<CardBattleUnitSnapshot>()).OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                canonical.Append(unit.Id).Append('|').Append(unit.Health).Append('|').Append(unit.Shield)
                    .Append('|').Append(unit.IsDefeated ? '1' : '0').Append('|');
                foreach (var status in (unit.Statuses ?? new Dictionary<string, string>())
                    .OrderBy(item => item.Key, StringComparer.Ordinal))
                    canonical.Append(status.Key).Append('=').Append(status.Value).Append(';');
                canonical.Append('\n');
            }
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical.ToString()));
                return BitConverter.ToString(bytes).Replace("-", string.Empty);
            }
        }
    }
}
