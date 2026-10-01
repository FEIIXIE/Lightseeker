using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public static class CardBattleContract
    {
        public const int RulesVersion = 1;
        public const string ResolvePolicyAtRelease = "AtRelease";
    }

    public enum CardBattleActionClass { BasicMelee, BasicProjectile, BasicSupport, HeroActive, RhythmSkill, EliteActive, BossActive, TriggeredEffect }
    public enum CardBattleDeliveryMode { Projectile, Beam, Melee, InstantTarget, Self, GroundPulse, Aura }
    public enum CardBattleImpactEmissionMode { SingleEventSingleTarget, SingleEventMultiTarget, MultiEventPhased }
    public enum CardBattleCancelPolicy { ReselectOnceBeforeRelease, FilterChildrenBeforeRelease, SelfIfActorAlive, TriggerCommitted, BattleEndsPending }
    public enum CardBattleEventKind { ActionStarted, ProjectileReleased, ImpactResolved, StatusFeedback, ActionCompleted }
    public enum CardBattleActionOutcome { Completed, Cancelled, Interrupted, NoValidTarget }
    public enum CardBattleCancelReason { None, ActorDefeatedBeforeRelease, NoValidTarget, BattleEndedPending }
    public enum CardBattleInterruptReason { None, ExplicitControl }
    public enum CardBattleEventAcceptResult { Applied, Duplicate, Stale, Gap, Rejected }

    [Serializable]
    public sealed class CardBattlePresentationRule
    {
        public int BattlePresentationRulesVersion = CardBattleContract.RulesVersion;
        public string AbilityId;
        public CardBattleActionClass ActionClass;
        public string TriggerRule;
        public string TargetRule;
        public int MaxTargetCount = 1;
        public CardBattleDeliveryMode DeliveryMode;
        public string ProjectileProfileId;
        public int WindupMs;
        public int ReleaseWindowMs;
        public int TravelMs;
        public string ResolvePolicy = CardBattleContract.ResolvePolicyAtRelease;
        public int RecoveryMs;
        public CardBattleCancelPolicy CancelPolicy;
        public int ReselectLimit;
        public CardBattleImpactEmissionMode ImpactEmissionMode;
        public List<string> Tags = new List<string>();

        public void Validate()
        {
            if (BattlePresentationRulesVersion != CardBattleContract.RulesVersion)
                throw new InvalidOperationException("Unsupported card battle rules version.");
            Require(AbilityId, "AbilityId");
            Require(TargetRule, "TargetRule");
            if (MaxTargetCount <= 0) throw new ArgumentOutOfRangeException("MaxTargetCount");
            if (WindupMs < 0 || ReleaseWindowMs < 0 || TravelMs < 0 || RecoveryMs < 0)
                throw new ArgumentOutOfRangeException("Card battle timings cannot be negative.");
            if (!string.Equals(ResolvePolicy, CardBattleContract.ResolvePolicyAtRelease, StringComparison.Ordinal))
                throw new InvalidOperationException("Only AtRelease is supported by E50 v0.2.");
            if (DeliveryMode == CardBattleDeliveryMode.Projectile && string.IsNullOrEmpty(ProjectileProfileId))
                throw new InvalidOperationException("Projectile delivery requires a profile id.");
            if (ImpactEmissionMode == CardBattleImpactEmissionMode.SingleEventSingleTarget && MaxTargetCount != 1)
                throw new InvalidOperationException("Single-target emission requires MaxTargetCount=1.");
            if (ReselectLimit < 0) throw new ArgumentOutOfRangeException("ReselectLimit");
            if (string.Equals(TargetRule, "TR_COLUMN_BOUND", StringComparison.Ordinal) &&
                HasTag("attack-speed-percent"))
                throw new InvalidOperationException(
                    "Column-bound C05 presentation rules cannot approximate attack speed.");
        }

        private bool HasTag(string expected)
        {
            foreach (var tag in Tags ?? new List<string>())
                if (string.Equals(tag, expected, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void Require(string value, string name)
        {
            if (string.IsNullOrEmpty(value)) throw new InvalidOperationException(name + " is required.");
        }
    }

    [Serializable]
    public sealed class CardBattleTargetResult
    {
        public int ChildIndex;
        public string TargetId;
        public int Damage;
        public int Healing;
        public int ShieldDelta;
        public int TargetHealthAfter;
        public int TargetShieldAfter;
        public bool IsDefeated;
        public List<string> StatusChanges = new List<string>();

        public CardBattleTargetResult Copy()
        {
            var result = new CardBattleTargetResult {
                ChildIndex = ChildIndex, TargetId = TargetId, Damage = Damage, Healing = Healing,
                ShieldDelta = ShieldDelta, TargetHealthAfter = TargetHealthAfter,
                TargetShieldAfter = TargetShieldAfter, IsDefeated = IsDefeated
            };
            if (StatusChanges != null) result.StatusChanges.AddRange(StatusChanges);
            return result;
        }
    }

    [Serializable]
    public sealed class CardBattleEvent
    {
        public CardBattleEventKind Kind;
        public string BattleId;
        public long EventSequence;
        public long ActionSequence;
        public int RulesVersion = CardBattleContract.RulesVersion;
        public string UnitId;
        public string SourceId;
        public string ActionId;
        public string AbilityId;
        public long AuthorityTick;
        public long StartedAt;
        public long CompletedAt;
        public long ResolvedAt;
        public CardBattleActionOutcome Outcome;
        public CardBattleCancelReason CancelReason;
        public CardBattleInterruptReason InterruptReason;
        public string ProjectileId;
        public CardBattleDeliveryMode DeliveryMode;
        public string OriginAnchor;
        public string TargetAnchor;
        public string ProfileId;
        public int TravelMs;
        public string ImpactId;
        public string TargetId;
        public string StatusKey;
        public string StatusValueBefore;
        public string StatusValueAfter;
        public int StatusPriority;
        public string StateHashAfter;
        public List<CardBattleTargetResult> Results = new List<CardBattleTargetResult>();

        public string IdempotencyKey { get { return CardBattleIdempotency.TopLevel(BattleId, EventSequence); } }

        public CardBattleEvent Copy()
        {
            var copy = (CardBattleEvent)MemberwiseClone();
            copy.Results = new List<CardBattleTargetResult>();
            if (Results != null) foreach (var result in Results) copy.Results.Add(result.Copy());
            return copy;
        }
    }

    public static class CardBattleIdempotency
    {
        public static string TopLevel(string battleId, long eventSequence)
        { return (battleId ?? string.Empty) + ":" + eventSequence; }
        public static string Child(string battleId, long eventSequence, int childIndex)
        { return TopLevel(battleId, eventSequence) + ":" + childIndex; }
    }

    public sealed class CardBattleActionHandle
    {
        internal bool Completed;
        internal bool ImpactResolved;
        internal bool ProjectileReleased;
        internal CardBattleActionHandle(string battleId, string unitId, string actionId, string abilityId,
            long actionSequence, CardBattlePresentationRule rule, long startedAt)
        {
            BattleId = battleId; UnitId = unitId; ActionId = actionId; AbilityId = abilityId;
            ActionSequence = actionSequence; Rule = rule; StartedAt = startedAt;
        }
        public string BattleId { get; private set; }
        public string UnitId { get; private set; }
        public string ActionId { get; private set; }
        public string AbilityId { get; private set; }
        public long ActionSequence { get; private set; }
        public long StartedAt { get; private set; }
        public CardBattlePresentationRule Rule { get; private set; }
    }

    public sealed class CardBattleEventStream
    {
        private readonly List<CardBattleEvent> events = new List<CardBattleEvent>();
        private readonly string battleId;
        private long nextEventSequence = 1;
        private long nextActionSequence = 1;

        public CardBattleEventStream(string battleId)
        {
            if (string.IsNullOrEmpty(battleId)) throw new ArgumentException("Battle id is required.", "battleId");
            this.battleId = battleId;
        }
        public IReadOnlyList<CardBattleEvent> Events { get { return events; } }

        public CardBattleActionHandle BeginAction(string unitId, string actionId,
            CardBattlePresentationRule rule, long startedAt)
        {
            if (rule == null) throw new ArgumentNullException("rule");
            rule.Validate(); RequireText(unitId, "unitId"); RequireText(actionId, "actionId");
            var handle = new CardBattleActionHandle(battleId, unitId, actionId, rule.AbilityId,
                nextActionSequence++, rule, startedAt);
            events.Add(new CardBattleEvent {
                Kind = CardBattleEventKind.ActionStarted, BattleId = battleId,
                EventSequence = nextEventSequence++, ActionSequence = handle.ActionSequence,
                RulesVersion = rule.BattlePresentationRulesVersion, UnitId = unitId, SourceId = unitId,
                ActionId = actionId, AbilityId = rule.AbilityId, StartedAt = startedAt, AuthorityTick = startedAt
            });
            return handle;
        }

        public void ReleaseProjectile(CardBattleActionHandle action, string projectileId,
            string originAnchor, string targetAnchor, string profileId, int travelMs)
        {
            CheckOpen(action);
            if (action.Rule.DeliveryMode != CardBattleDeliveryMode.Projectile)
                throw new InvalidOperationException("Only Projectile actions may release a projectile.");
            if (action.ProjectileReleased)
                throw new InvalidOperationException("Projectile has already been released.");
            if (action.ProjectileReleased)
                throw new InvalidOperationException("Projectile has already been released.");
            RequireText(projectileId, "projectileId"); RequireText(originAnchor, "originAnchor");
            RequireText(targetAnchor, "targetAnchor");
            if (!string.Equals(profileId, action.Rule.ProjectileProfileId, StringComparison.Ordinal))
                throw new InvalidOperationException("Projectile profile does not match the action rule.");
            if (travelMs < 0) throw new ArgumentOutOfRangeException("travelMs");
            action.ProjectileReleased = true;
            events.Add(new CardBattleEvent {
                Kind = CardBattleEventKind.ProjectileReleased, BattleId = battleId,
                EventSequence = nextEventSequence++, ActionSequence = action.ActionSequence,
                RulesVersion = action.Rule.BattlePresentationRulesVersion, UnitId = action.UnitId, SourceId = action.UnitId,
                ActionId = action.ActionId, AbilityId = action.AbilityId, ProjectileId = projectileId,
                DeliveryMode = action.Rule.DeliveryMode, OriginAnchor = originAnchor,
                TargetAnchor = targetAnchor, ProfileId = profileId, TravelMs = travelMs,
                AuthorityTick = action.StartedAt
            });
        }

        public void ResolveImpact(CardBattleActionHandle action, string impactId, long resolvedAt,
            IList<CardBattleTargetResult> targetResults)
        {
            CheckOpen(action); RequireText(impactId, "impactId");
            if (targetResults == null) throw new ArgumentNullException("targetResults");
            if (action.ImpactResolved)
                throw new InvalidOperationException("Impact has already been resolved.");
            if (action.ImpactResolved)
                throw new InvalidOperationException("Impact has already been resolved.");
            if (action.Rule.DeliveryMode == CardBattleDeliveryMode.Projectile && !action.ProjectileReleased)
                throw new InvalidOperationException("Projectile impact requires release first.");
            if (targetResults.Count == 0) throw new InvalidOperationException("ImpactResolved requires a target.");
            if (action.Rule.ImpactEmissionMode == CardBattleImpactEmissionMode.SingleEventSingleTarget &&
                targetResults.Count != 1)
                throw new InvalidOperationException("Single-target impact cannot contain multiple results.");
            if (targetResults.Count > action.Rule.MaxTargetCount)
                throw new InvalidOperationException("Impact exceeds max target count.");
            var copy = new List<CardBattleTargetResult>();
            for (var index = 0; index < targetResults.Count; index++) {
                var result = targetResults[index];
                if (result == null || string.IsNullOrEmpty(result.TargetId))
                    throw new InvalidOperationException("Impact target is required.");
                if (result.ChildIndex != index)
                    throw new InvalidOperationException("Impact childIndex must be contiguous and stable.");
                copy.Add(result.Copy());
            }
            action.ImpactResolved = true;
            events.Add(new CardBattleEvent {
                Kind = CardBattleEventKind.ImpactResolved, BattleId = battleId,
                EventSequence = nextEventSequence++, ActionSequence = action.ActionSequence,
                RulesVersion = action.Rule.BattlePresentationRulesVersion, UnitId = action.UnitId, SourceId = action.UnitId,
                ActionId = action.ActionId, AbilityId = action.AbilityId, DeliveryMode = action.Rule.DeliveryMode,
                ImpactId = impactId,
                ResolvedAt = resolvedAt, AuthorityTick = resolvedAt,
                TargetId = copy.Count == 0 ? null : copy[0].TargetId, Results = copy
            });
        }

        public void AddStatusFeedback(CardBattleActionHandle action, string targetId, string statusKey,
            string before, string after, int priority)
        {
            CheckOpen(action); RequireText(targetId, "targetId"); RequireText(statusKey, "statusKey");
            events.Add(new CardBattleEvent {
                Kind = CardBattleEventKind.StatusFeedback, BattleId = battleId,
                EventSequence = nextEventSequence++, ActionSequence = action.ActionSequence,
                RulesVersion = action.Rule.BattlePresentationRulesVersion, UnitId = action.UnitId, SourceId = action.UnitId,
                ActionId = action.ActionId, AbilityId = action.AbilityId, TargetId = targetId,
                StatusKey = statusKey, StatusValueBefore = before, StatusValueAfter = after,
                StatusPriority = priority, AuthorityTick = action.StartedAt
            });
        }

        public void CompleteAction(CardBattleActionHandle action, long completedAt,
            CardBattleActionOutcome outcome, CardBattleCancelReason cancelReason,
            CardBattleInterruptReason interruptReason)
        {
            CheckOpen(action);
            if (outcome == CardBattleActionOutcome.Completed && !action.ImpactResolved &&
                action.Rule.ActionClass != CardBattleActionClass.TriggeredEffect)
                throw new InvalidOperationException("Completed action requires an impact result.");
            if (outcome == CardBattleActionOutcome.Cancelled && cancelReason == CardBattleCancelReason.None)
                throw new InvalidOperationException("Cancelled action requires a cancel reason.");
            if (outcome == CardBattleActionOutcome.Interrupted && interruptReason == CardBattleInterruptReason.None)
                throw new InvalidOperationException("Interrupted action requires an interrupt reason.");
            if (outcome != CardBattleActionOutcome.Cancelled && cancelReason != CardBattleCancelReason.None)
                throw new InvalidOperationException("Only cancelled actions may carry a cancel reason.");
            if (outcome != CardBattleActionOutcome.Interrupted && interruptReason != CardBattleInterruptReason.None)
                throw new InvalidOperationException("Only interrupted actions may carry an interrupt reason.");
            action.Completed = true;
            events.Add(new CardBattleEvent {
                Kind = CardBattleEventKind.ActionCompleted, BattleId = battleId,
                EventSequence = nextEventSequence++, ActionSequence = action.ActionSequence,
                RulesVersion = action.Rule.BattlePresentationRulesVersion, UnitId = action.UnitId, SourceId = action.UnitId,
                ActionId = action.ActionId, AbilityId = action.AbilityId, CompletedAt = completedAt,
                AuthorityTick = completedAt, Outcome = outcome, CancelReason = cancelReason, InterruptReason = interruptReason
            });
        }

        private static void RequireText(string value, string name)
        { if (string.IsNullOrEmpty(value)) throw new ArgumentException(name + " is required.", name); }
        private static void CheckOpen(CardBattleActionHandle action)
        {
            if (action == null) throw new ArgumentNullException("action");
            if (action.Completed) throw new InvalidOperationException("Action is already completed.");
        }
    }

    public sealed class CardBattleEventConsumer
    {
        private readonly string battleId;
        private readonly HashSet<string> appliedTopLevel = new HashSet<string>(StringComparer.Ordinal);
        private readonly List<CardBattleEvent> appliedEvents = new List<CardBattleEvent>();
        public CardBattleEventConsumer(string battleId)
        {
            if (string.IsNullOrEmpty(battleId)) throw new ArgumentException("Battle id is required.", "battleId");
            this.battleId = battleId;
        }
        public long LastConfirmedEventSequence { get; private set; }
        public IReadOnlyList<CardBattleEvent> AppliedEvents { get { return appliedEvents; } }

        public CardBattleEventAcceptResult Consume(CardBattleEvent battleEvent)
        {
            if (battleEvent == null || !string.Equals(battleId, battleEvent.BattleId, StringComparison.Ordinal) ||
                battleEvent.RulesVersion != CardBattleContract.RulesVersion || battleEvent.EventSequence <= 0)
                return CardBattleEventAcceptResult.Rejected;
            var key = battleEvent.IdempotencyKey;
            if (appliedTopLevel.Contains(key)) return CardBattleEventAcceptResult.Duplicate;
            if (battleEvent.EventSequence <= LastConfirmedEventSequence) return CardBattleEventAcceptResult.Stale;
            if (battleEvent.EventSequence != LastConfirmedEventSequence + 1) return CardBattleEventAcceptResult.Gap;
            if (!ValidateChildren(battleEvent)) return CardBattleEventAcceptResult.Rejected;
            appliedTopLevel.Add(key); appliedEvents.Add(battleEvent.Copy());
            LastConfirmedEventSequence = battleEvent.EventSequence;
            return CardBattleEventAcceptResult.Applied;
        }

        public void ReconcileToSnapshot(long eventSequence)
        {
            if (eventSequence < LastConfirmedEventSequence) throw new ArgumentOutOfRangeException("eventSequence");
            LastConfirmedEventSequence = eventSequence;
        }

        private static bool ValidateChildren(CardBattleEvent battleEvent)
        {
            if (battleEvent.Kind != CardBattleEventKind.ImpactResolved) return true;
            if (battleEvent.Results == null || battleEvent.Results.Count == 0) return false;
            for (var index = 0; index < battleEvent.Results.Count; index++) {
                var result = battleEvent.Results[index];
                if (result == null || result.ChildIndex != index || string.IsNullOrEmpty(result.TargetId)) return false;
            }
            return true;
        }
    }

    public static class CardBattleEventIntegrity
    {
        public static List<string> Validate(IEnumerable<CardBattleEvent> source)
        {
            var errors = new List<string>();
            if (source == null) { errors.Add("event stream is null"); return errors; }
            var previousEvent = 0L;
            var topLevelKeys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var battleEvent in source) {
                if (battleEvent == null) { errors.Add("event is null"); continue; }
                if (battleEvent.EventSequence <= previousEvent) errors.Add("eventSequence is not increasing");
                previousEvent = battleEvent.EventSequence;
                if (!topLevelKeys.Add(battleEvent.IdempotencyKey)) errors.Add("duplicate top-level key");
                if (battleEvent.RulesVersion != CardBattleContract.RulesVersion) errors.Add("unsupported rulesVersion");
                if (string.IsNullOrEmpty(battleEvent.BattleId) || battleEvent.ActionSequence <= 0)
                    errors.Add("event identity is incomplete");
                if (battleEvent.Kind == CardBattleEventKind.ImpactResolved) {
                    if (battleEvent.Results == null || battleEvent.Results.Count == 0) errors.Add("impact has no results");
                    else for (var index = 0; index < battleEvent.Results.Count; index++) {
                        var result = battleEvent.Results[index];
                        if (result == null || result.ChildIndex != index || string.IsNullOrEmpty(result.TargetId))
                            errors.Add("impact childIndex is not stable");
                    }
                }
                if (battleEvent.Kind == CardBattleEventKind.ActionCompleted) {
                    if (battleEvent.Outcome == CardBattleActionOutcome.Cancelled && battleEvent.CancelReason == CardBattleCancelReason.None)
                        errors.Add("cancelled action has no reason");
                    if (battleEvent.Outcome == CardBattleActionOutcome.Interrupted && battleEvent.InterruptReason == CardBattleInterruptReason.None)
                        errors.Add("interrupted action has no reason");
                    if (battleEvent.Outcome != CardBattleActionOutcome.Cancelled && battleEvent.CancelReason != CardBattleCancelReason.None)
                        errors.Add("non-cancelled action has cancel reason");
                    if (battleEvent.Outcome != CardBattleActionOutcome.Interrupted && battleEvent.InterruptReason != CardBattleInterruptReason.None)
                        errors.Add("non-interrupted action has interrupt reason");
                }
            }
            return errors;
        }
    }

    public static class LegacyBattleEventAdapter
    {
        public static List<CardBattleEvent> Convert(BattleResult result, string battleId)
        {
            if (result == null) throw new ArgumentNullException("result");
            var stream = new CardBattleEventStream(battleId);
            var events = result.Events ?? new List<BattleEvent>();
            for (var index = 0; index < events.Count; index++) {
                var legacy = events[index];
                if (legacy == null) continue;
                var isHeal = legacy.Type == BattleEventType.Heal;
                var rule = new CardBattlePresentationRule {
                    AbilityId = isHeal ? "legacy.heal" : "legacy.attack",
                    ActionClass = isHeal ? CardBattleActionClass.BasicSupport : CardBattleActionClass.BasicMelee,
                    TargetRule = "LEGACY_TARGET", MaxTargetCount = 1,
                    DeliveryMode = isHeal ? CardBattleDeliveryMode.InstantTarget : CardBattleDeliveryMode.Melee,
                    CancelPolicy = CardBattleCancelPolicy.ReselectOnceBeforeRelease,
                    ImpactEmissionMode = CardBattleImpactEmissionMode.SingleEventSingleTarget
                };
                var action = stream.BeginAction(legacy.ActorId ?? "system", "legacy-action-" + index, rule, legacy.ActionTime);
                stream.ResolveImpact(action, "legacy-impact-" + index, legacy.ActionTime, new[] {
                    new CardBattleTargetResult {
                        ChildIndex = 0, TargetId = legacy.TargetId ?? "unknown",
                        Damage = isHeal ? 0 : Math.Max(0, legacy.Amount),
                        Healing = isHeal ? Math.Max(0, legacy.Amount) : 0,
                        TargetHealthAfter = legacy.TargetHealthAfter, TargetShieldAfter = legacy.TargetShieldAfter,
                        IsDefeated = legacy.Type == BattleEventType.Defeat || legacy.TargetHealthAfter <= 0
                    }
                });
                stream.CompleteAction(action, legacy.ActionTime, CardBattleActionOutcome.Completed,
                    CardBattleCancelReason.None, CardBattleInterruptReason.None);
            }
            return new List<CardBattleEvent>(stream.Events);
        }
    }
}
