using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SeekerAutoChess.Core
{
    public static class SpatialBattleSimulator
    {
        public static SpatialBattleResult Run(SpatialBattleInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            input.Validate();

            var config = input.Config;
            var units = CreateRuntimeUnits(input);
            var events = new List<SpatialBattleEvent>();
            var sequence = 0;
            var completedTicks = 0;
            var outcome = SpatialBattleOutcome.SafetyLimit;

            for (var tick = 0; tick < config.MaximumTicks; tick++)
            {
                if (!AnyAlive(units, true) || !AnyAlive(units, false))
                {
                    outcome = AnyAlive(units, true)
                        ? SpatialBattleOutcome.PlayerVictory
                        : SpatialBattleOutcome.EnemyVictory;
                    completedTicks = tick;
                    break;
                }

                for (var index = 0; index < units.Count; index++)
                {
                    var unit = units[index];
                    if (!unit.IsAlive) continue;
                    StepUnit(unit, units, config, events, tick, ref sequence);
                    if (events.Count >= config.MaximumEvents)
                    {
                        completedTicks = tick + 1;
                        outcome = SpatialBattleOutcome.SafetyLimit;
                        goto Complete;
                    }
                }

                completedTicks = tick + 1;
                if (tick == config.MaximumTicks - 1) outcome = SpatialBattleOutcome.SafetyLimit;
            }

        Complete:
            AddEvent(events, completedTicks, ref sequence, SpatialBattleEventType.BattleEnded,
                string.Empty, string.Empty, SpatialActionState.AcquireTarget,
                default(SpatialPoint), (int)outcome, 0);
            var result = new SpatialBattleResult
            {
                Outcome = outcome,
                CompletedTicks = completedTicks,
                InputHash = HashInput(input),
                Events = events,
                EventHash = HashEvents(events)
            };
            for (var index = 0; index < units.Count; index++)
                result.FinalUnits.Add(units[index].Snapshot());
            result.FinalStateHash = HashFinalState(result);
            return result;
        }

        public static SpatialBattleInput CreateG1Baseline(uint seed = 20260919u)
        {
            var input = new SpatialBattleInput { Seed = seed };
            input.PlayerUnits.Add(Unit("P-M1", true, SpatialUnitRole.Melee, 3000, 13000, 1000, 36, 6));
            input.PlayerUnits.Add(Unit("P-M2", true, SpatialUnitRole.Melee, 7000, 13000, 850, 42, 5));
            input.PlayerUnits.Add(Unit("P-R1", true, SpatialUnitRole.Ranged, 5000, 14500, 950, 28, 4));
            input.EnemyUnits.Add(Unit("E-M1", false, SpatialUnitRole.Melee, 3000, 3000, 1000, 36, 6));
            input.EnemyUnits.Add(Unit("E-M2", false, SpatialUnitRole.Melee, 7000, 3000, 850, 42, 5));
            input.EnemyUnits.Add(Unit("E-R1", false, SpatialUnitRole.Ranged, 5000, 1500, 950, 28, 4));
            return input;
        }

        public static string CreateRepresentativeLog(SpatialBattleResult result, int maximumEvents = 40)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var text = new StringBuilder();
            text.Append("rules=").Append(SpatialBattleConfig.SupportedRulesVersion)
                .Append(" input=").Append(result.InputHash)
                .Append(" events=").Append(result.EventHash)
                .Append(" final=").Append(result.FinalStateHash)
                .Append(" outcome=").Append(result.Outcome)
                .Append(" ticks=").Append(result.CompletedTicks).Append('\n');
            var count = Math.Min(Math.Max(0, maximumEvents), result.Events.Count);
            for (var index = 0; index < count; index++) text.Append(result.Events[index]).Append('\n');
            if (count < result.Events.Count)
                text.Append("... ").Append(result.Events.Count - count).Append(" more events\n");
            return text.ToString();
        }

        private static SpatialBattleUnitInput Unit(
            string id, bool isPlayer, SpatialUnitRole role, int x, int y,
            int moveSpeed, int health, int damage)
        {
            return new SpatialBattleUnitInput
            {
                BattleUnitId = id,
                IsPlayer = isPlayer,
                Role = role,
                Position = new SpatialPoint(x, y),
                Radius = 350,
                MoveSpeed = moveSpeed,
                MaximumHealth = health,
                AttackDamage = damage,
                AttackPeriodMilliseconds = 1000
            };
        }

        private static List<RuntimeUnit> CreateRuntimeUnits(SpatialBattleInput input)
        {
            var units = new List<RuntimeUnit>(input.PlayerUnits.Count + input.EnemyUnits.Count);
            AddRuntimeUnits(units, input.PlayerUnits, input.Config);
            AddRuntimeUnits(units, input.EnemyUnits, input.Config);
            units.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return units;
        }

        private static void AddRuntimeUnits(
            ICollection<RuntimeUnit> destination,
            IEnumerable<SpatialBattleUnitInput> source,
            SpatialBattleConfig config)
        {
            foreach (var input in source)
                destination.Add(new RuntimeUnit(input, config));
        }

        private static void StepUnit(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleConfig config,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            switch (actor.State)
            {
                case SpatialActionState.AcquireTarget:
                    AcquireTarget(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialActionState.MoveIntoRange:
                    Move(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialActionState.Windup:
                    Windup(actor, units, events, tick, ref sequence);
                    break;
                case SpatialActionState.Release:
                    Release(actor, units, events, tick, ref sequence);
                    break;
                case SpatialActionState.Impact:
                    Impact(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialActionState.Recovery:
                    Recovery(actor, events, tick, ref sequence);
                    break;
            }
        }

        private static void AcquireTarget(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleConfig config,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            var target = FindTarget(actor, units);
            actor.TargetId = target == null ? string.Empty : target.Id;
            if (target == null) return;
            AddEvent(events, tick, ref sequence, SpatialBattleEventType.TargetAcquired,
                actor.Id, target.Id, actor.State, actor.Position, 0, target.Health);
            Transition(actor, IsInRange(actor, target, config)
                ? SpatialActionState.Windup
                : SpatialActionState.MoveIntoRange, config, events, tick, ref sequence);
        }

        private static void Move(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleConfig config,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            if (target == null || !target.IsAlive)
            {
                actor.TargetId = string.Empty;
                Transition(actor, SpatialActionState.AcquireTarget, config, events, tick, ref sequence);
                return;
            }
            if (IsInRange(actor, target, config))
            {
                Transition(actor, SpatialActionState.Windup, config, events, tick, ref sequence);
                return;
            }

            var numerator = (long)actor.MoveSpeed * config.TickMilliseconds + actor.MoveRemainder;
            var step = (int)(numerator / 1000L);
            actor.MoveRemainder = (int)(numerator % 1000L);
            var previous = actor.Position;
            actor.Position = SpatialBattleMath.MoveToward(previous, target.Position, step,
                EffectiveRange(actor, target, config), actor.Radius, config.FieldWidth, config.FieldHeight);
            if (!actor.Position.Equals(previous))
                AddEvent(events, tick, ref sequence, SpatialBattleEventType.Moved,
                    actor.Id, target.Id, actor.State, actor.Position, 0, target.Health);
            if (IsInRange(actor, target, config))
                Transition(actor, SpatialActionState.Windup, config, events, tick, ref sequence);
        }

        private static void Windup(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            if (target == null || !target.IsAlive)
            {
                actor.TargetId = string.Empty;
                Transition(actor, SpatialActionState.AcquireTarget, null, events, tick, ref sequence);
                return;
            }
            actor.StateTicksRemaining--;
            if (actor.StateTicksRemaining <= 0)
                Transition(actor, SpatialActionState.Release, null, events, tick, ref sequence);
        }

        private static void Release(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            AddEvent(events, tick, ref sequence, SpatialBattleEventType.Released,
                actor.Id, actor.TargetId, actor.State, actor.Position, 0, target == null ? 0 : target.Health);
            Transition(actor, SpatialActionState.Impact, null, events, tick, ref sequence);
        }

        private static void Impact(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleConfig config,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            var legal = target != null && target.IsAlive &&
                        (actor.Role == SpatialUnitRole.Ranged || IsInRange(actor, target, config));
            AddEvent(events, tick, ref sequence, SpatialBattleEventType.Impacted,
                actor.Id, actor.TargetId, actor.State, target == null ? actor.Position : target.Position,
                legal ? actor.AttackDamage : 0, target == null ? 0 : target.Health);
            if (legal)
            {
                target.Health = Math.Max(0, target.Health - actor.AttackDamage);
                AddEvent(events, tick, ref sequence, SpatialBattleEventType.Damaged,
                    actor.Id, target.Id, actor.State, target.Position, actor.AttackDamage, target.Health);
                if (!target.IsAlive && target.State != SpatialActionState.Defeated)
                {
                    target.TargetId = string.Empty;
                    target.State = SpatialActionState.Defeated;
                    target.StateTicksRemaining = 0;
                    AddEvent(events, tick, ref sequence, SpatialBattleEventType.Defeated,
                        target.Id, actor.Id, target.State, target.Position, 0, 0);
                }
            }
            Transition(actor, SpatialActionState.Recovery, config, events, tick, ref sequence);
        }

        private static void Recovery(
            RuntimeUnit actor,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            actor.StateTicksRemaining--;
            if (actor.StateTicksRemaining <= 0)
                Transition(actor, SpatialActionState.AcquireTarget, null, events, tick, ref sequence);
        }

        private static void Transition(
            RuntimeUnit actor,
            SpatialActionState state,
            SpatialBattleConfig config,
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence)
        {
            actor.State = state;
            if (state == SpatialActionState.Windup)
            {
                var tickMs = config == null ? 50 : config.TickMilliseconds;
                var windupMs = config == null ? 200 : config.WindupMilliseconds;
                actor.StateTicksRemaining = Math.Max(1, DivideRoundUp(windupMs, tickMs));
            }
            else if (state == SpatialActionState.Recovery)
            {
                var tickMs = config == null ? 50 : config.TickMilliseconds;
                var windupMs = config == null ? 200 : config.WindupMilliseconds;
                var recoveryMs = Math.Max(tickMs, actor.AttackPeriodMilliseconds - windupMs - tickMs * 2);
                actor.StateTicksRemaining = Math.Max(1, DivideRoundUp(recoveryMs, tickMs));
            }
            else actor.StateTicksRemaining = 0;

            AddEvent(events, tick, ref sequence, SpatialBattleEventType.StateChanged,
                actor.Id, actor.TargetId, state, actor.Position, 0, actor.Health);
        }

        private static RuntimeUnit FindTarget(RuntimeUnit actor, IReadOnlyList<RuntimeUnit> units)
        {
            RuntimeUnit best = null;
            var bestDistance = long.MaxValue;
            for (var index = 0; index < units.Count; index++)
            {
                var candidate = units[index];
                if (!candidate.IsAlive || candidate.IsPlayer == actor.IsPlayer) continue;
                var distance = SpatialBattleMath.DistanceSquared(actor.Position, candidate.Position);
                if (best == null || distance < bestDistance ||
                    (distance == bestDistance && string.CompareOrdinal(candidate.Id, best.Id) < 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static RuntimeUnit FindById(IReadOnlyList<RuntimeUnit> units, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (var index = 0; index < units.Count; index++)
                if (string.Equals(units[index].Id, id, StringComparison.Ordinal)) return units[index];
            return null;
        }

        private static bool IsInRange(RuntimeUnit actor, RuntimeUnit target, SpatialBattleConfig config)
        {
            var range = EffectiveRange(actor, target, config);
            return SpatialBattleMath.DistanceSquared(actor.Position, target.Position) <= (long)range * range;
        }

        private static int EffectiveRange(RuntimeUnit actor, RuntimeUnit target, SpatialBattleConfig config)
        {
            return actor.Radius + target.Radius +
                   (actor.Role == SpatialUnitRole.Melee ? config.MeleeReach : config.RangedReach);
        }

        private static bool AnyAlive(IEnumerable<RuntimeUnit> units, bool player)
        {
            foreach (var unit in units)
                if (unit.IsPlayer == player && unit.IsAlive) return true;
            return false;
        }

        private static void AddEvent(
            ICollection<SpatialBattleEvent> events,
            int tick,
            ref int sequence,
            SpatialBattleEventType type,
            string actorId,
            string targetId,
            SpatialActionState state,
            SpatialPoint position,
            int amount,
            int targetHealthAfter)
        {
            events.Add(new SpatialBattleEvent
            {
                Tick = tick,
                Sequence = sequence++,
                Type = type,
                ActorId = actorId ?? string.Empty,
                TargetId = targetId ?? string.Empty,
                State = state,
                Position = position,
                Amount = amount,
                TargetHealthAfter = targetHealthAfter
            });
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }

        private static string HashInput(SpatialBattleInput input)
        {
            var text = new StringBuilder();
            var config = input.Config;
            text.Append(config.RulesVersion).Append('|').Append(config.SpaceConfigVersion).Append('|')
                .Append(input.Seed).Append('|').Append(config.TickMilliseconds).Append('|')
                .Append(config.FieldWidth).Append('|').Append(config.FieldHeight).Append('|')
                .Append(config.MeleeReach).Append('|').Append(config.RangedReach).Append('|')
                .Append(config.WindupMilliseconds).Append('|').Append(config.MaximumTicks).Append('|');
            var units = new List<SpatialBattleUnitInput>();
            units.AddRange(input.PlayerUnits);
            units.AddRange(input.EnemyUnits);
            units.Sort((left, right) => string.CompareOrdinal(left.BattleUnitId, right.BattleUnitId));
            foreach (var unit in units)
                text.Append(unit.BattleUnitId).Append(':').Append(unit.IsPlayer ? 1 : 0).Append(':')
                    .Append((int)unit.Role).Append(':').Append(unit.Position.X).Append(':')
                    .Append(unit.Position.Y).Append(':').Append(unit.Radius).Append(':')
                    .Append(unit.MoveSpeed).Append(':').Append(unit.MaximumHealth).Append(':')
                    .Append(unit.AttackDamage).Append(':').Append(unit.AttackPeriodMilliseconds).Append('|');
            return StableHash(text.ToString());
        }

        private static string HashEvents(IEnumerable<SpatialBattleEvent> events)
        {
            var text = new StringBuilder();
            foreach (var battleEvent in events) text.Append(battleEvent).Append('|');
            return StableHash(text.ToString());
        }

        private static string HashFinalState(SpatialBattleResult result)
        {
            var text = new StringBuilder();
            text.Append((int)result.Outcome).Append('|').Append(result.CompletedTicks).Append('|');
            foreach (var unit in result.FinalUnits)
                text.Append(unit.BattleUnitId).Append(':').Append(unit.Position.X).Append(':')
                    .Append(unit.Position.Y).Append(':').Append(unit.Health).Append(':')
                    .Append(unit.TargetId).Append(':').Append((int)unit.ActionState).Append('|');
            return StableHash(text.ToString());
        }

        private static string StableHash(string value)
        {
            const ulong offset = 14695981039346656037UL;
            const ulong prime = 1099511628211UL;
            var hash = offset;
            for (var index = 0; index < value.Length; index++)
            {
                var character = value[index];
                hash ^= (byte)(character & 0xff);
                hash *= prime;
                hash ^= (byte)(character >> 8);
                hash *= prime;
            }
            return hash.ToString("X16", CultureInfo.InvariantCulture);
        }

        private sealed class RuntimeUnit
        {
            public readonly string Id;
            public readonly bool IsPlayer;
            public readonly SpatialUnitRole Role;
            public readonly int Radius;
            public readonly int MoveSpeed;
            public readonly int MaximumHealth;
            public readonly int AttackDamage;
            public readonly int AttackPeriodMilliseconds;
            public SpatialPoint Position;
            public int Health;
            public string TargetId = string.Empty;
            public SpatialActionState State = SpatialActionState.AcquireTarget;
            public int StateTicksRemaining;
            public int MoveRemainder;

            public bool IsAlive => Health > 0;

            public RuntimeUnit(SpatialBattleUnitInput input, SpatialBattleConfig config)
            {
                Id = input.BattleUnitId;
                IsPlayer = input.IsPlayer;
                Role = input.Role;
                Radius = input.Radius;
                MoveSpeed = input.MoveSpeed;
                MaximumHealth = input.MaximumHealth;
                Health = input.MaximumHealth;
                AttackDamage = input.AttackDamage;
                AttackPeriodMilliseconds = input.AttackPeriodMilliseconds;
                Position = SpatialBattleMath.ClampToField(input.Position, Radius,
                    config.FieldWidth, config.FieldHeight);
            }

            public SpatialBattleUnitSnapshot Snapshot()
            {
                return new SpatialBattleUnitSnapshot
                {
                    BattleUnitId = Id,
                    IsPlayer = IsPlayer,
                    Role = Role,
                    Position = Position,
                    Radius = Radius,
                    Health = Health,
                    MaximumHealth = MaximumHealth,
                    TargetId = TargetId,
                    ActionState = State
                };
            }
        }
    }
}
