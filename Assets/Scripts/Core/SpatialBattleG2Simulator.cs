using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SeekerAutoChess.Core
{
    public static class SpatialBattleG2Simulator
    {
        public static SpatialBattleG2Result Run(SpatialBattleG2Input input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            input.Validate();
            var config = input.Config;
            var units = CreateRuntimeUnits(input);
            var events = new List<SpatialBattleG2Event>();
            var sequence = 0;
            var activeActions = 0;
            var completedTicks = 0;
            var outcome = SpatialG2Outcome.SafetyLimit;
            var endReason = SpatialG2EndReason.EventSafetyLimit;

            for (var tick = 0; tick < config.MaximumTicks; tick++)
            {
                if (!AnyAlive(units, true) || !AnyAlive(units, false))
                {
                    outcome = AnyAlive(units, true)
                        ? SpatialG2Outcome.PlayerVictory
                        : SpatialG2Outcome.EnemyVictory;
                    endReason = SpatialG2EndReason.Elimination;
                    completedTicks = tick;
                    break;
                }

                for (var index = 0; index < units.Count; index++)
                {
                    var actor = units[index];
                    if (!actor.IsAlive) continue;
                    StepUnit(actor, units, config, events, tick, ref sequence, ref activeActions);
                    if (activeActions >= config.MaximumActiveActions)
                    {
                        completedTicks = tick + 1;
                        outcome = SpatialG2Outcome.DrawTimeout;
                        endReason = SpatialG2EndReason.ActiveActionLimit;
                        goto Complete;
                    }
                    if (events.Count >= config.MaximumEvents)
                    {
                        completedTicks = tick + 1;
                        outcome = SpatialG2Outcome.SafetyLimit;
                        endReason = SpatialG2EndReason.EventSafetyLimit;
                        goto Complete;
                    }
                }

                ApplySoftSeparation(units, config, events, tick, ref sequence);
                completedTicks = tick + 1;
                if (completedTicks >= config.MaximumTicks)
                {
                    outcome = SpatialG2Outcome.DrawTimeout;
                    endReason = SpatialG2EndReason.TimeLimit;
                }
            }

        Complete:
            AddEvent(events, completedTicks, ref sequence, SpatialG2EventType.BattleEnded,
                string.Empty, string.Empty, SpatialG2ActionState.AcquireTarget,
                SpatialG2TargetMode.None, default(SpatialPoint), (int)outcome, 0, 0,
                EndReasonCode(endReason));
            var result = new SpatialBattleG2Result
            {
                Outcome = outcome,
                EndReason = endReason,
                CompletedTicks = completedTicks,
                ActiveActions = activeActions,
                RewardsGranted = false,
                InputHash = HashInput(input),
                Events = events,
                EventHash = HashEvents(events)
            };
            foreach (var unit in units)
            {
                result.FinalUnits.Add(unit.Snapshot());
                result.HealingDone += unit.HealingDone;
                result.Overhealing += unit.Overhealing;
                result.RetargetCount += unit.RetargetCount;
                result.SeparationCount += unit.SeparationCount;
                result.StuckDetectionCount += unit.StuckDetectionCount;
                result.SidestepCount += unit.SidestepCount;
            }
            result.FinalStateHash = HashFinalState(result);
            return result;
        }

        public static SpatialBattleG2Input CreateG2A(uint seed = 20260920u)
        {
            var input = new SpatialBattleG2Input { VectorId = "G2-A_DUAL_SUPPORT", Seed = seed };
            input.PlayerUnits.Add(Unit("P-C01", true, SpatialG2Role.Melee, 2800, 12800, 1000, 44, 25, 6));
            input.PlayerUnits.Add(Unit("P-C05", true, SpatialG2Role.Melee, 7200, 12800, 850, 52, 40, 6));
            input.PlayerUnits.Add(Unit("P-C03", true, SpatialG2Role.Ranged, 4000, 14500, 950, 34, 27, 5));
            input.PlayerUnits.Add(Support("P-C02", true, 6000, 14500, 40, 40, 7));
            input.EnemyUnits.Add(Unit("E-N2A", false, SpatialG2Role.Melee, 2800, 3200, 1000, 42, 27, 6));
            input.EnemyUnits.Add(Unit("E-N2B", false, SpatialG2Role.Melee, 7200, 3200, 1000, 42, 34, 6));
            input.EnemyUnits.Add(Unit("E-N3", false, SpatialG2Role.Ranged, 4000, 1500, 950, 32, 25, 5));
            input.EnemyUnits.Add(Support("E-N5", false, 6000, 1500, 38, 38, 6));
            return input;
        }

        public static SpatialBattleG2Input CreateG2B(uint seed = 20260921u)
        {
            var input = CreateG2A(seed);
            input.VectorId = "G2-B_LOW_HP_RETARGET";
            input.EnemyUnits.RemoveAt(1);
            input.EnemyUnits.Add(Unit("E-N4", false, SpatialG2Role.LowHealthHunter,
                7200, 3200, 1100, 38, 38, 1));
            // N4 initially selects C01 by ratio on the right lane. N2/N3 pressure C05
            // on the left lane until it becomes at least 20 percentage points lower,
            // producing a deterministic mid-battle retarget rather than only testing
            // the initial low-health selection.
            input.PlayerUnits[0].Position = new SpatialPoint(7200, 12800);
            input.PlayerUnits[0].InitialHealth = 32; // C01: 72.7%
            input.PlayerUnits[1].Position = new SpatialPoint(2800, 12800);
            input.PlayerUnits[1].InitialHealth = 45; // C05: 86.5%
            input.PlayerUnits[2].Position = new SpatialPoint(5000, 14500);
            input.PlayerUnits[3].Position = new SpatialPoint(7200, 14500);
            input.EnemyUnits[0].AttackDamage = 12;
            input.EnemyUnits[1].AttackDamage = 10;
            return input;
        }

        public static SpatialBattleG2Input CreateG2C(uint seed = 20260922u)
        {
            var input = new SpatialBattleG2Input { VectorId = "G2-C_CONGESTION_TIMEOUT", Seed = seed };
            input.Config.MaximumActiveActions = 200;
            input.PlayerUnits.Add(Unit("P-C01A", true, SpatialG2Role.Melee, 4850, 10900, 120, 9999, 9999, 1));
            input.PlayerUnits.Add(Unit("P-C01B", true, SpatialG2Role.Melee, 5000, 11000, 120, 9999, 9999, 1));
            input.PlayerUnits.Add(Unit("P-C05", true, SpatialG2Role.Melee, 5150, 11100, 120, 9999, 9999, 1));
            input.EnemyUnits.Add(Unit("E-N2A", false, SpatialG2Role.Melee, 4850, 5100, 120, 9999, 9999, 1));
            input.EnemyUnits.Add(Unit("E-N2B", false, SpatialG2Role.Melee, 5000, 5000, 120, 9999, 9999, 1));
            input.EnemyUnits.Add(Unit("E-N2C", false, SpatialG2Role.Melee, 5150, 4900, 120, 9999, 9999, 1));
            return input;
        }

        public static string CreateRepresentativeLog(SpatialBattleG2Result result, int maximumEvents = 50)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            var text = new StringBuilder();
            text.Append("rules=").Append(SpatialBattleG2Config.SupportedRulesVersion)
                .Append(" input=").Append(result.InputHash)
                .Append(" events=").Append(result.EventHash)
                .Append(" final=").Append(result.FinalStateHash)
                .Append(" outcome=").Append(result.Outcome)
                .Append(" reason=").Append(result.EndReason)
                .Append(" ticks=").Append(result.CompletedTicks)
                .Append(" actions=").Append(result.ActiveActions)
                .Append(" healing=").Append(result.HealingDone)
                .Append(" overheal=").Append(result.Overhealing)
                .Append(" retargets=").Append(result.RetargetCount)
                .Append(" separation=").Append(result.SeparationCount)
                .Append(" stuck=").Append(result.StuckDetectionCount)
                .Append(" sidesteps=").Append(result.SidestepCount).Append('\n');
            var count = Math.Min(Math.Max(0, maximumEvents), result.Events.Count);
            for (var index = 0; index < count; index++) text.Append(result.Events[index]).Append('\n');
            if (count < result.Events.Count)
                text.Append("... ").Append(result.Events.Count - count).Append(" more events\n");
            return text.ToString();
        }

        public static bool IsN4RetargetThresholdMet(
            int candidateHealth,
            int candidateMaximumHealth,
            int currentHealth,
            int currentMaximumHealth,
            int gapBasisPoints = 2000)
        {
            if (candidateMaximumHealth <= 0 || currentMaximumHealth <= 0 ||
                candidateHealth < 0 || currentHealth < 0 || gapBasisPoints < 0 || gapBasisPoints > 10000)
                throw new ArgumentOutOfRangeException(nameof(candidateMaximumHealth));
            var differenceNumerator = (long)currentHealth * candidateMaximumHealth -
                                      (long)candidateHealth * currentMaximumHealth;
            return differenceNumerator > 0 && differenceNumerator * 10000L >=
                   (long)gapBasisPoints * currentMaximumHealth * candidateMaximumHealth;
        }

        private static SpatialBattleG2UnitInput Unit(
            string id, bool player, SpatialG2Role role, int x, int y, int speed,
            int maximumHealth, int initialHealth, int damage)
        {
            return new SpatialBattleG2UnitInput
            {
                BattleUnitId = id,
                IsPlayer = player,
                Role = role,
                Position = new SpatialPoint(x, y),
                Radius = 350,
                MoveSpeed = speed,
                MaximumHealth = maximumHealth,
                InitialHealth = initialHealth,
                AttackDamage = damage,
                HealAmount = 0,
                ActionPeriodMilliseconds = 1000
            };
        }

        private static SpatialBattleG2UnitInput Support(
            string id, bool player, int x, int y, int maximumHealth, int initialHealth, int heal)
        {
            var unit = Unit(id, player, SpatialG2Role.Support, x, y, 950,
                maximumHealth, initialHealth, 2);
            unit.HealAmount = heal;
            unit.ActionPeriodMilliseconds = 1200;
            return unit;
        }

        private static List<RuntimeUnit> CreateRuntimeUnits(SpatialBattleG2Input input)
        {
            var units = new List<RuntimeUnit>(input.PlayerUnits.Count + input.EnemyUnits.Count);
            foreach (var unit in input.PlayerUnits) units.Add(new RuntimeUnit(unit, input.Config));
            foreach (var unit in input.EnemyUnits) units.Add(new RuntimeUnit(unit, input.Config));
            units.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return units;
        }

        private static void StepUnit(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence,
            ref int activeActions)
        {
            if (actor.Role == SpatialG2Role.LowHealthHunter &&
                actor.TargetMode == SpatialG2TargetMode.Attack &&
                actor.State != SpatialG2ActionState.Defeated)
                EvaluateN4Retarget(actor, units, config, events, tick, ref sequence);

            switch (actor.State)
            {
                case SpatialG2ActionState.AcquireTarget:
                    Acquire(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialG2ActionState.MoveIntoRange:
                    MoveIntoRange(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialG2ActionState.FollowFormation:
                    FollowFormation(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialG2ActionState.Windup:
                    Windup(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialG2ActionState.Release:
                    Release(actor, units, config, events, tick, ref sequence, ref activeActions);
                    break;
                case SpatialG2ActionState.Impact:
                    Impact(actor, units, config, events, tick, ref sequence);
                    break;
                case SpatialG2ActionState.Recovery:
                    Recovery(actor, events, tick, ref sequence);
                    break;
                case SpatialG2ActionState.Sidestep:
                    Transition(actor, SpatialG2ActionState.MoveIntoRange, config,
                        events, tick, ref sequence, "sidestep_complete");
                    break;
            }
        }

        private static void Acquire(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            if (actor.Role == SpatialG2Role.Support)
            {
                var injured = FindHealTarget(actor, units);
                if (injured != null)
                {
                    SetTarget(actor, injured, SpatialG2TargetMode.Heal, events, tick, ref sequence,
                        "lowest_hp_ratio");
                    Transition(actor, IsInRange(actor, injured, config)
                            ? SpatialG2ActionState.Windup
                            : SpatialG2ActionState.MoveIntoRange,
                        config, events, tick, ref sequence, "support_heal_target");
                    return;
                }

                var enemyInRange = FindNearestEnemy(actor, units, true, config);
                if (enemyInRange != null)
                {
                    SetTarget(actor, enemyInRange, SpatialG2TargetMode.Attack,
                        events, tick, ref sequence, "enemy_already_in_support_range");
                    Transition(actor, SpatialG2ActionState.Windup, config,
                        events, tick, ref sequence, "support_weak_attack");
                    return;
                }

                var anchor = FindFormationAnchor(actor, units);
                if (anchor != null)
                {
                    SetTarget(actor, anchor, SpatialG2TargetMode.Follow,
                        events, tick, ref sequence, "formation_anchor");
                    Transition(actor, SpatialG2ActionState.FollowFormation, config,
                        events, tick, ref sequence, "no_injured_ally");
                }
                return;
            }

            RuntimeUnit target;
            if (actor.Role == SpatialG2Role.LowHealthHunter)
            {
                // Recovery returns to AcquireTarget without discarding a still-legal
                // lock. Preserve that lock here; EvaluateN4Retarget is the only path
                // allowed to replace it while legal, so the 20-point rule cannot be
                // bypassed at the next action cycle.
                var current = FindById(units, actor.TargetId);
                target = current != null && current.IsAlive && current.IsPlayer != actor.IsPlayer
                    ? current
                    : FindLowestHealthEnemy(actor, units);
            }
            else target = FindNearestEnemy(actor, units, false, config);
            if (target == null) return;
            SetTarget(actor, target, SpatialG2TargetMode.Attack, events, tick, ref sequence,
                actor.Role == SpatialG2Role.LowHealthHunter ? "n4_lowest_hp_ratio" : "nearest_enemy");
            Transition(actor, IsInRange(actor, target, config)
                    ? SpatialG2ActionState.Windup
                    : SpatialG2ActionState.MoveIntoRange,
                config, events, tick, ref sequence, "attack_target");
        }

        private static void MoveIntoRange(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            if (!IsTargetLegal(actor, target))
            {
                InvalidateTarget(actor, events, tick, ref sequence, "target_invalid");
                return;
            }
            if (actor.TargetMode == SpatialG2TargetMode.Heal && target.Health >= target.MaximumHealth)
            {
                InvalidateTarget(actor, events, tick, ref sequence, "heal_target_full");
                return;
            }
            if (IsInRange(actor, target, config))
            {
                ResetProgress(actor, target);
                Transition(actor, SpatialG2ActionState.Windup, config,
                    events, tick, ref sequence, "entered_range");
                return;
            }

            var previous = actor.Position;
            var destination = target.Position;
            var desired = EffectiveRange(actor, target, config);
            if (actor.Role == SpatialG2Role.LowHealthHunter)
            {
                destination = LateralApproachPoint(actor, target, config);
                desired = Math.Max(0, desired - 400);
            }
            MoveToward(actor, destination, desired, config);
            if (!actor.Position.Equals(previous))
            {
                actor.PursuitDistance += SpatialBattleMath.IntegerSqrt(
                    SpatialBattleMath.DistanceSquared(previous, actor.Position));
                AddEvent(events, tick, ref sequence, SpatialG2EventType.Moved,
                    actor.Id, target.Id, actor.State, actor.TargetMode, actor.Position,
                    0, target.Health, 0, actor.Role == SpatialG2Role.LowHealthHunter
                        ? "stable_lateral_approach" : "straight_approach");
            }
            TrackProgress(actor, target, units, config, events, tick, ref sequence);
            if (IsInRange(actor, target, config))
            {
                ResetProgress(actor, target);
                Transition(actor, SpatialG2ActionState.Windup, config,
                    events, tick, ref sequence, "entered_range");
            }
        }

        private static void FollowFormation(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            if (FindHealTarget(actor, units) != null)
            {
                InvalidateTarget(actor, events, tick, ref sequence, "injured_ally_available");
                return;
            }
            var enemy = FindNearestEnemy(actor, units, true, config);
            if (enemy != null)
            {
                SetTarget(actor, enemy, SpatialG2TargetMode.Attack,
                    events, tick, ref sequence, "enemy_already_in_support_range");
                Transition(actor, SpatialG2ActionState.Windup, config,
                    events, tick, ref sequence, "support_weak_attack");
                return;
            }

            var anchor = FindById(units, actor.TargetId);
            if (anchor == null || !anchor.IsAlive || anchor.IsPlayer != actor.IsPlayer)
            {
                anchor = FindFormationAnchor(actor, units);
                if (anchor == null)
                {
                    InvalidateTarget(actor, events, tick, ref sequence, "formation_anchor_missing");
                    return;
                }
                SetTarget(actor, anchor, SpatialG2TargetMode.Follow,
                    events, tick, ref sequence, "formation_anchor_reselected");
            }

            var desired = Math.Max(actor.Radius + anchor.Radius,
                config.SupportDesiredCenterDistance - 400);
            if (SpatialBattleMath.DistanceSquared(actor.Position, anchor.Position) <= (long)desired * desired)
                return;
            var previous = actor.Position;
            MoveToward(actor, anchor.Position, desired, config);
            if (!actor.Position.Equals(previous))
                AddEvent(events, tick, ref sequence, SpatialG2EventType.Moved,
                    actor.Id, anchor.Id, actor.State, actor.TargetMode, actor.Position,
                    0, anchor.Health, 0, "formation_follow");
        }

        private static void Windup(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            if (!IsTargetLegal(actor, target) ||
                (actor.TargetMode == SpatialG2TargetMode.Heal && target.Health >= target.MaximumHealth))
            {
                InvalidateTarget(actor, events, tick, ref sequence, "windup_target_invalid");
                return;
            }
            actor.StateTicksRemaining--;
            if (actor.StateTicksRemaining <= 0)
                Transition(actor, SpatialG2ActionState.Release, config,
                    events, tick, ref sequence, "windup_complete");
        }

        private static void Release(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence,
            ref int activeActions)
        {
            var target = FindById(units, actor.TargetId);
            activeActions++;
            AddEvent(events, tick, ref sequence, SpatialG2EventType.Released,
                actor.Id, actor.TargetId, actor.State, actor.TargetMode, actor.Position,
                0, target == null ? 0 : target.Health, 0,
                actor.TargetMode == SpatialG2TargetMode.Heal ? "heal_release" : "attack_release");
            Transition(actor, SpatialG2ActionState.Impact, config,
                events, tick, ref sequence, "release_complete");
        }

        private static void Impact(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            var target = FindById(units, actor.TargetId);
            if (actor.TargetMode == SpatialG2TargetMode.Heal)
            {
                var legal = target != null && target.IsAlive && target.IsPlayer == actor.IsPlayer &&
                            IsInRange(actor, target, config);
                var missing = legal ? Math.Max(0, target.MaximumHealth - target.Health) : 0;
                var actual = Math.Min(actor.HealAmount, missing);
                var overheal = legal ? Math.Max(0, actor.HealAmount - actual) : 0;
                AddEvent(events, tick, ref sequence, SpatialG2EventType.Impacted,
                    actor.Id, actor.TargetId, actor.State, actor.TargetMode,
                    target == null ? actor.Position : target.Position, actual,
                    target == null ? 0 : target.Health, overheal,
                    legal ? "heal_impact" : "heal_impact_invalid");
                if (legal)
                {
                    target.Health += actual;
                    actor.HealingDone += actual;
                    actor.Overhealing += overheal;
                    AddEvent(events, tick, ref sequence, SpatialG2EventType.Healed,
                        actor.Id, target.Id, actor.State, actor.TargetMode, target.Position,
                        actual, target.Health, overheal, "lowest_hp_healed");
                }
            }
            else
            {
                var legal = target != null && target.IsAlive && target.IsPlayer != actor.IsPlayer &&
                            (actor.Role == SpatialG2Role.Ranged ||
                             actor.Role == SpatialG2Role.LowHealthHunter ||
                             IsInRange(actor, target, config));
                AddEvent(events, tick, ref sequence, SpatialG2EventType.Impacted,
                    actor.Id, actor.TargetId, actor.State, actor.TargetMode,
                    target == null ? actor.Position : target.Position,
                    legal ? actor.AttackDamage : 0, target == null ? 0 : target.Health, 0,
                    legal ? "attack_impact" : "attack_impact_invalid");
                if (legal)
                {
                    target.Health = Math.Max(0, target.Health - actor.AttackDamage);
                    AddEvent(events, tick, ref sequence, SpatialG2EventType.Damaged,
                        actor.Id, target.Id, actor.State, actor.TargetMode, target.Position,
                        actor.AttackDamage, target.Health, 0, "damage_applied");
                    if (!target.IsAlive && target.State != SpatialG2ActionState.Defeated)
                    {
                        target.TargetId = string.Empty;
                        target.TargetMode = SpatialG2TargetMode.None;
                        target.State = SpatialG2ActionState.Defeated;
                        target.StateTicksRemaining = 0;
                        AddEvent(events, tick, ref sequence, SpatialG2EventType.Defeated,
                            target.Id, actor.Id, target.State, target.TargetMode, target.Position,
                            0, 0, 0, "health_zero");
                    }
                }
            }
            Transition(actor, SpatialG2ActionState.Recovery, config,
                events, tick, ref sequence, "impact_complete");
        }

        private static void Recovery(
            RuntimeUnit actor,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            actor.StateTicksRemaining--;
            if (actor.StateTicksRemaining <= 0)
                Transition(actor, SpatialG2ActionState.AcquireTarget, null,
                    events, tick, ref sequence, "recovery_complete");
        }

        private static void EvaluateN4Retarget(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            var current = FindById(units, actor.TargetId);
            var candidate = FindLowestHealthEnemy(actor, units);
            if (candidate == null) return;
            if (current == null || !current.IsAlive)
            {
                SetTarget(actor, candidate, SpatialG2TargetMode.Attack,
                    events, tick, ref sequence, "n4_invalid_current");
                return;
            }
            if (string.Equals(current.Id, candidate.Id, StringComparison.Ordinal)) return;
            if (IsAtLeastBasisPointsLower(candidate, current,
                    config.LowHealthRetargetGapBasisPoints))
            {
                var previousId = current.Id;
                actor.RetargetCount++;
                actor.TargetId = candidate.Id;
                actor.TargetMode = SpatialG2TargetMode.Attack;
                ResetProgress(actor, candidate);
                AddEvent(events, tick, ref sequence, SpatialG2EventType.Retargeted,
                    actor.Id, candidate.Id, actor.State, actor.TargetMode, actor.Position,
                    0, candidate.Health, 0, "n4_gap_2000_from_" + previousId);
                if (actor.State == SpatialG2ActionState.Windup ||
                    actor.State == SpatialG2ActionState.Release ||
                    actor.State == SpatialG2ActionState.Impact)
                    Transition(actor, IsInRange(actor, candidate, config)
                            ? SpatialG2ActionState.Windup
                            : SpatialG2ActionState.MoveIntoRange,
                        config, events, tick, ref sequence, "n4_retarget_restart");
            }
            else
            {
                AddEvent(events, tick, ref sequence, SpatialG2EventType.TargetRetained,
                    actor.Id, current.Id, actor.State, actor.TargetMode, actor.Position,
                    0, current.Health, 0, "n4_gap_below_2000");
            }
        }

        private static void TrackProgress(
            RuntimeUnit actor,
            RuntimeUnit target,
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            var distance = SpatialBattleMath.DistanceSquared(actor.Position, target.Position);
            if (distance < actor.BestTargetDistanceSquared)
            {
                actor.BestTargetDistanceSquared = distance;
                actor.NoProgressTicks = 0;
                return;
            }
            actor.NoProgressTicks++;
            if (actor.NoProgressTicks < config.StuckTicks) return;

            actor.StuckDetectionCount++;
            AddEvent(events, tick, ref sequence, SpatialG2EventType.StuckDetected,
                actor.Id, target.Id, actor.State, actor.TargetMode, actor.Position,
                actor.NoProgressTicks, target.Health, 0,
                actor.SidestepTried ? "stuck_after_sidestep" : "stuck_1000ms");
            actor.NoProgressTicks = 0;
            actor.BestTargetDistanceSquared = distance;
            if (!actor.SidestepTried)
            {
                actor.SidestepTried = true;
                actor.SidestepCount++;
                PerformSidestep(actor, target, config);
                AddEvent(events, tick, ref sequence, SpatialG2EventType.Sidestepped,
                    actor.Id, target.Id, SpatialG2ActionState.Sidestep, actor.TargetMode,
                    actor.Position, actor.LateralSign, target.Health, 0, "stable_id_side");
                actor.State = SpatialG2ActionState.Sidestep;
                AddEvent(events, tick, ref sequence, SpatialG2EventType.StateChanged,
                    actor.Id, target.Id, actor.State, actor.TargetMode, actor.Position,
                    0, actor.Health, 0, "stuck_sidestep");
                return;
            }

            actor.RetargetCount++;
            var oldTarget = actor.TargetId;
            actor.TargetId = string.Empty;
            actor.TargetMode = SpatialG2TargetMode.None;
            actor.SidestepTried = false;
            AddEvent(events, tick, ref sequence, SpatialG2EventType.Retargeted,
                actor.Id, oldTarget, actor.State, actor.TargetMode, actor.Position,
                0, actor.Health, 0, "stuck_retarget");
            Transition(actor, SpatialG2ActionState.AcquireTarget, config,
                events, tick, ref sequence, "stuck_reacquire");
        }

        private static void PerformSidestep(RuntimeUnit actor, RuntimeUnit target, SpatialBattleG2Config config)
        {
            var dx = (long)target.Position.X - actor.Position.X;
            var dy = (long)target.Position.Y - actor.Position.Y;
            var distance = Math.Max(1, SpatialBattleMath.IntegerSqrt(dx * dx + dy * dy));
            var maximumStep = Math.Max(1, actor.MoveSpeed * config.TickMilliseconds / 1000);
            var step = Math.Min(config.SidestepDistance, maximumStep);
            var moveX = (int)(-dy * actor.LateralSign * step / distance);
            var moveY = (int)(dx * actor.LateralSign * step / distance);
            if (moveX == 0 && moveY == 0) moveX = actor.LateralSign;
            actor.Position = SpatialBattleMath.ClampToField(
                new SpatialPoint(actor.Position.X + moveX, actor.Position.Y + moveY),
                actor.Radius, config.FieldWidth, config.FieldHeight);
        }

        private static void ApplySoftSeparation(
            IReadOnlyList<RuntimeUnit> units,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence)
        {
            for (var firstIndex = 0; firstIndex < units.Count; firstIndex++)
            {
                var first = units[firstIndex];
                if (!first.IsAlive) continue;
                for (var secondIndex = firstIndex + 1; secondIndex < units.Count; secondIndex++)
                {
                    var second = units[secondIndex];
                    if (!second.IsAlive) continue;
                    var minimum = first.Radius + second.Radius;
                    if (SpatialBattleMath.DistanceSquared(first.Position, second.Position) >=
                        (long)minimum * minimum) continue;

                    var previous = second.Position;
                    var dx = (long)second.Position.X - first.Position.X;
                    var dy = (long)second.Position.Y - first.Position.Y;
                    if (dx == 0 && dy == 0) dx = second.LateralSign;
                    var distance = Math.Max(1, SpatialBattleMath.IntegerSqrt(dx * dx + dy * dy));
                    var moveX = (int)(dx * config.SeparationStep / distance);
                    var moveY = (int)(dy * config.SeparationStep / distance);
                    if (moveX == 0 && moveY == 0) moveX = second.LateralSign;
                    second.Position = SpatialBattleMath.ClampToField(
                        new SpatialPoint(second.Position.X + moveX, second.Position.Y + moveY),
                        second.Radius, config.FieldWidth, config.FieldHeight);
                    if (second.Position.Equals(previous)) continue;
                    second.SeparationCount++;
                    AddEvent(events, tick, ref sequence, SpatialG2EventType.Separated,
                        second.Id, first.Id, second.State, second.TargetMode, second.Position,
                        config.SeparationStep, second.Health, 0, "stable_id_soft_separation");
                }
            }
        }

        private static RuntimeUnit FindHealTarget(RuntimeUnit actor, IReadOnlyList<RuntimeUnit> units)
        {
            RuntimeUnit best = null;
            var bestClaimed = true;
            var bestDistance = long.MaxValue;
            for (var index = 0; index < units.Count; index++)
            {
                var candidate = units[index];
                if (!candidate.IsAlive || candidate.IsPlayer != actor.IsPlayer ||
                    candidate.Id == actor.Id || candidate.Health >= candidate.MaximumHealth) continue;
                var claimed = IsHealClaimedByOther(actor, candidate, units);
                var distance = SpatialBattleMath.DistanceSquared(actor.Position, candidate.Position);
                if (best == null || CompareHealthRatio(candidate, best) < 0 ||
                    (CompareHealthRatio(candidate, best) == 0 && claimed != bestClaimed && !claimed) ||
                    (CompareHealthRatio(candidate, best) == 0 && claimed == bestClaimed &&
                     (distance < bestDistance ||
                      (distance == bestDistance && string.CompareOrdinal(candidate.Id, best.Id) < 0))))
                {
                    best = candidate;
                    bestClaimed = claimed;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static bool IsHealClaimedByOther(
            RuntimeUnit actor, RuntimeUnit candidate, IReadOnlyList<RuntimeUnit> units)
        {
            for (var index = 0; index < units.Count; index++)
            {
                var healer = units[index];
                if (healer.Id == actor.Id || !healer.IsAlive || healer.IsPlayer != actor.IsPlayer ||
                    healer.Role != SpatialG2Role.Support || healer.TargetMode != SpatialG2TargetMode.Heal)
                    continue;
                if (string.Equals(healer.TargetId, candidate.Id, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static RuntimeUnit FindFormationAnchor(RuntimeUnit actor, IReadOnlyList<RuntimeUnit> units)
        {
            RuntimeUnit bestGuard = null;
            var guardDistance = long.MaxValue;
            RuntimeUnit bestFallback = null;
            for (var index = 0; index < units.Count; index++)
            {
                var candidate = units[index];
                if (!candidate.IsAlive || candidate.IsPlayer != actor.IsPlayer || candidate.Id == actor.Id)
                    continue;
                if (bestFallback == null || candidate.MaximumHealth > bestFallback.MaximumHealth ||
                    (candidate.MaximumHealth == bestFallback.MaximumHealth &&
                     string.CompareOrdinal(candidate.Id, bestFallback.Id) < 0))
                    bestFallback = candidate;
                if (candidate.Role != SpatialG2Role.Melee) continue;
                var distance = SpatialBattleMath.DistanceSquared(actor.Position, candidate.Position);
                if (bestGuard == null || distance < guardDistance ||
                    (distance == guardDistance && string.CompareOrdinal(candidate.Id, bestGuard.Id) < 0))
                {
                    bestGuard = candidate;
                    guardDistance = distance;
                }
            }
            return bestGuard ?? bestFallback;
        }

        private static RuntimeUnit FindNearestEnemy(
            RuntimeUnit actor,
            IReadOnlyList<RuntimeUnit> units,
            bool requireInSupportRange,
            SpatialBattleG2Config config)
        {
            RuntimeUnit best = null;
            var bestDistance = long.MaxValue;
            for (var index = 0; index < units.Count; index++)
            {
                var candidate = units[index];
                if (!candidate.IsAlive || candidate.IsPlayer == actor.IsPlayer) continue;
                var distance = SpatialBattleMath.DistanceSquared(actor.Position, candidate.Position);
                if (requireInSupportRange)
                {
                    var range = actor.Radius + candidate.Radius + config.SupportAttackReach;
                    if (distance > (long)range * range) continue;
                }
                if (best == null || distance < bestDistance ||
                    (distance == bestDistance && string.CompareOrdinal(candidate.Id, best.Id) < 0))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static RuntimeUnit FindLowestHealthEnemy(RuntimeUnit actor, IReadOnlyList<RuntimeUnit> units)
        {
            RuntimeUnit best = null;
            var bestDistance = long.MaxValue;
            for (var index = 0; index < units.Count; index++)
            {
                var candidate = units[index];
                if (!candidate.IsAlive || candidate.IsPlayer == actor.IsPlayer) continue;
                var distance = SpatialBattleMath.DistanceSquared(actor.Position, candidate.Position);
                var ratioComparison = best == null ? -1 : CompareHealthRatio(candidate, best);
                if (best == null || ratioComparison < 0 ||
                    (ratioComparison == 0 && (distance < bestDistance ||
                     (distance == bestDistance && string.CompareOrdinal(candidate.Id, best.Id) < 0))))
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }
            return best;
        }

        private static int CompareHealthRatio(RuntimeUnit first, RuntimeUnit second)
        {
            var left = (long)first.Health * second.MaximumHealth;
            var right = (long)second.Health * first.MaximumHealth;
            return left.CompareTo(right);
        }

        private static bool IsAtLeastBasisPointsLower(
            RuntimeUnit candidate, RuntimeUnit current, int basisPoints)
        {
            return IsN4RetargetThresholdMet(candidate.Health, candidate.MaximumHealth,
                current.Health, current.MaximumHealth, basisPoints);
        }

        private static SpatialPoint LateralApproachPoint(
            RuntimeUnit actor, RuntimeUnit target, SpatialBattleG2Config config)
        {
            var dx = (long)target.Position.X - actor.Position.X;
            var dy = (long)target.Position.Y - actor.Position.Y;
            var distance = Math.Max(1, SpatialBattleMath.IntegerSqrt(dx * dx + dy * dy));
            var offset = Math.Min(600, config.SidestepDistance);
            var x = target.Position.X + (int)(-dy * actor.LateralSign * offset / distance);
            var y = target.Position.Y + (int)(dx * actor.LateralSign * offset / distance);
            return SpatialBattleMath.ClampToField(new SpatialPoint(x, y), actor.Radius,
                config.FieldWidth, config.FieldHeight);
        }

        private static void MoveToward(
            RuntimeUnit actor, SpatialPoint destination, int desiredDistance, SpatialBattleG2Config config)
        {
            var numerator = (long)actor.MoveSpeed * config.TickMilliseconds + actor.MoveRemainder;
            var step = (int)(numerator / 1000L);
            actor.MoveRemainder = (int)(numerator % 1000L);
            actor.Position = SpatialBattleMath.MoveToward(actor.Position, destination, step,
                desiredDistance, actor.Radius, config.FieldWidth, config.FieldHeight);
        }

        private static bool IsTargetLegal(RuntimeUnit actor, RuntimeUnit target)
        {
            if (target == null || !target.IsAlive) return false;
            if (actor.TargetMode == SpatialG2TargetMode.Heal || actor.TargetMode == SpatialG2TargetMode.Follow)
                return target.IsPlayer == actor.IsPlayer;
            return actor.TargetMode == SpatialG2TargetMode.Attack && target.IsPlayer != actor.IsPlayer;
        }

        private static bool IsInRange(RuntimeUnit actor, RuntimeUnit target, SpatialBattleG2Config config)
        {
            var range = EffectiveRange(actor, target, config);
            return SpatialBattleMath.DistanceSquared(actor.Position, target.Position) <= (long)range * range;
        }

        private static int EffectiveRange(RuntimeUnit actor, RuntimeUnit target, SpatialBattleG2Config config)
        {
            if (actor.TargetMode == SpatialG2TargetMode.Heal) return config.SupportDesiredCenterDistance;
            var reach = actor.Role == SpatialG2Role.Melee ? config.MeleeReach :
                actor.Role == SpatialG2Role.Support ? config.SupportAttackReach : config.RangedReach;
            return actor.Radius + target.Radius + reach;
        }

        private static void SetTarget(
            RuntimeUnit actor,
            RuntimeUnit target,
            SpatialG2TargetMode mode,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence,
            string reason)
        {
            actor.TargetId = target.Id;
            actor.TargetMode = mode;
            ResetProgress(actor, target);
            AddEvent(events, tick, ref sequence, SpatialG2EventType.TargetAcquired,
                actor.Id, target.Id, actor.State, mode, actor.Position,
                0, target.Health, 0, reason);
        }

        private static void InvalidateTarget(
            RuntimeUnit actor,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence,
            string reason)
        {
            var previous = actor.TargetId;
            AddEvent(events, tick, ref sequence, SpatialG2EventType.TargetInvalidated,
                actor.Id, previous, actor.State, actor.TargetMode, actor.Position,
                0, actor.Health, 0, reason);
            actor.TargetId = string.Empty;
            actor.TargetMode = SpatialG2TargetMode.None;
            actor.SidestepTried = false;
            Transition(actor, SpatialG2ActionState.AcquireTarget, null,
                events, tick, ref sequence, reason);
        }

        private static void ResetProgress(RuntimeUnit actor, RuntimeUnit target)
        {
            actor.BestTargetDistanceSquared = target == null
                ? long.MaxValue
                : SpatialBattleMath.DistanceSquared(actor.Position, target.Position);
            actor.NoProgressTicks = 0;
            actor.SidestepTried = false;
        }

        private static void Transition(
            RuntimeUnit actor,
            SpatialG2ActionState state,
            SpatialBattleG2Config config,
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence,
            string reason)
        {
            actor.State = state;
            if (state == SpatialG2ActionState.Windup)
            {
                var tickMs = config == null ? 50 : config.TickMilliseconds;
                var windupMs = config == null ? 200 : config.WindupMilliseconds;
                actor.StateTicksRemaining = Math.Max(1, DivideRoundUp(windupMs, tickMs));
            }
            else if (state == SpatialG2ActionState.Recovery)
            {
                var tickMs = config == null ? 50 : config.TickMilliseconds;
                var windupMs = config == null ? 200 : config.WindupMilliseconds;
                var recovery = Math.Max(tickMs,
                    actor.ActionPeriodMilliseconds - windupMs - tickMs * 2);
                actor.StateTicksRemaining = Math.Max(1, DivideRoundUp(recovery, tickMs));
            }
            else actor.StateTicksRemaining = 0;

            AddEvent(events, tick, ref sequence, SpatialG2EventType.StateChanged,
                actor.Id, actor.TargetId, state, actor.TargetMode, actor.Position,
                0, actor.Health, 0, reason);
        }

        private static RuntimeUnit FindById(IReadOnlyList<RuntimeUnit> units, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (var index = 0; index < units.Count; index++)
                if (string.Equals(units[index].Id, id, StringComparison.Ordinal)) return units[index];
            return null;
        }

        private static bool AnyAlive(IEnumerable<RuntimeUnit> units, bool player)
        {
            foreach (var unit in units)
                if (unit.IsPlayer == player && unit.IsAlive) return true;
            return false;
        }

        private static int DivideRoundUp(int value, int divisor)
        {
            return (value + divisor - 1) / divisor;
        }

        private static string EndReasonCode(SpatialG2EndReason reason)
        {
            switch (reason)
            {
                case SpatialG2EndReason.Elimination: return "elimination";
                case SpatialG2EndReason.TimeLimit: return "draw_timeout_90s";
                case SpatialG2EndReason.ActiveActionLimit: return "draw_timeout_200_actions";
                default: return "event_safety_limit";
            }
        }

        private static void AddEvent(
            ICollection<SpatialBattleG2Event> events,
            int tick,
            ref int sequence,
            SpatialG2EventType type,
            string actorId,
            string targetId,
            SpatialG2ActionState state,
            SpatialG2TargetMode targetMode,
            SpatialPoint position,
            int amount,
            int targetHealthAfter,
            int overheal,
            string reason)
        {
            events.Add(new SpatialBattleG2Event
            {
                Tick = tick,
                Sequence = sequence++,
                Type = type,
                ActorId = actorId ?? string.Empty,
                TargetId = targetId ?? string.Empty,
                State = state,
                TargetMode = targetMode,
                Position = position,
                Amount = amount,
                TargetHealthAfter = targetHealthAfter,
                Overheal = overheal,
                ReasonCode = reason ?? string.Empty
            });
        }

        private static string HashInput(SpatialBattleG2Input input)
        {
            var config = input.Config;
            var text = new StringBuilder();
            text.Append(config.RulesVersion).Append('|').Append(config.ConfigVersion).Append('|')
                .Append(input.VectorId).Append('|').Append(input.Seed).Append('|')
                .Append(config.TickMilliseconds).Append('|').Append(config.FieldWidth).Append('|')
                .Append(config.FieldHeight).Append('|').Append(config.MeleeReach).Append('|')
                .Append(config.RangedReach).Append('|').Append(config.SupportAttackReach).Append('|')
                .Append(config.HealRange).Append('|').Append(config.SupportDesiredCenterDistance).Append('|')
                .Append(config.WindupMilliseconds).Append('|').Append(config.StuckDetectionMilliseconds).Append('|')
                .Append(config.LowHealthRetargetGapBasisPoints).Append('|').Append(config.SidestepDistance).Append('|')
                .Append(config.SeparationStep).Append('|').Append(config.TimeoutMilliseconds).Append('|')
                .Append(config.MaximumActiveActions).Append('|').Append(config.MaximumEvents).Append('|');
            var units = new List<SpatialBattleG2UnitInput>();
            units.AddRange(input.PlayerUnits);
            units.AddRange(input.EnemyUnits);
            units.Sort((left, right) => string.CompareOrdinal(left.BattleUnitId, right.BattleUnitId));
            foreach (var unit in units)
                text.Append(unit.BattleUnitId).Append(':').Append(unit.IsPlayer ? 1 : 0).Append(':')
                    .Append((int)unit.Role).Append(':').Append(unit.Position.X).Append(':')
                    .Append(unit.Position.Y).Append(':').Append(unit.Radius).Append(':')
                    .Append(unit.MoveSpeed).Append(':').Append(unit.MaximumHealth).Append(':')
                    .Append(unit.InitialHealth).Append(':').Append(unit.AttackDamage).Append(':')
                    .Append(unit.HealAmount).Append(':').Append(unit.ActionPeriodMilliseconds).Append('|');
            return StableHash(text.ToString());
        }

        private static string HashEvents(IEnumerable<SpatialBattleG2Event> events)
        {
            var text = new StringBuilder();
            foreach (var battleEvent in events) text.Append(battleEvent).Append('|');
            return StableHash(text.ToString());
        }

        private static string HashFinalState(SpatialBattleG2Result result)
        {
            var text = new StringBuilder();
            text.Append((int)result.Outcome).Append('|').Append((int)result.EndReason).Append('|')
                .Append(result.CompletedTicks).Append('|').Append(result.ActiveActions).Append('|')
                .Append(result.HealingDone).Append('|').Append(result.Overhealing).Append('|')
                .Append(result.RetargetCount).Append('|').Append(result.SeparationCount).Append('|')
                .Append(result.StuckDetectionCount).Append('|').Append(result.SidestepCount).Append('|');
            foreach (var unit in result.FinalUnits)
                text.Append(unit.BattleUnitId).Append(':').Append(unit.Position.X).Append(':')
                    .Append(unit.Position.Y).Append(':').Append(unit.Health).Append(':')
                    .Append(unit.TargetId).Append(':').Append((int)unit.TargetMode).Append(':')
                    .Append((int)unit.ActionState).Append(':').Append(unit.RetargetCount).Append(':')
                    .Append(unit.PursuitDistance).Append(':').Append(unit.SeparationCount).Append(':')
                    .Append(unit.StuckDetectionCount).Append(':').Append(unit.SidestepCount).Append('|');
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
            public readonly SpatialG2Role Role;
            public readonly int Radius;
            public readonly int MoveSpeed;
            public readonly int MaximumHealth;
            public readonly int AttackDamage;
            public readonly int HealAmount;
            public readonly int ActionPeriodMilliseconds;
            public readonly int LateralSign;
            public SpatialPoint Position;
            public int Health;
            public string TargetId = string.Empty;
            public SpatialG2TargetMode TargetMode;
            public SpatialG2ActionState State = SpatialG2ActionState.AcquireTarget;
            public int StateTicksRemaining;
            public int MoveRemainder;
            public long BestTargetDistanceSquared = long.MaxValue;
            public int NoProgressTicks;
            public bool SidestepTried;
            public int HealingDone;
            public int Overhealing;
            public int RetargetCount;
            public int PursuitDistance;
            public int SeparationCount;
            public int StuckDetectionCount;
            public int SidestepCount;

            public bool IsAlive => Health > 0;

            public RuntimeUnit(SpatialBattleG2UnitInput input, SpatialBattleG2Config config)
            {
                Id = input.BattleUnitId;
                IsPlayer = input.IsPlayer;
                Role = input.Role;
                Radius = input.Radius;
                MoveSpeed = input.MoveSpeed;
                MaximumHealth = input.MaximumHealth;
                Health = input.InitialHealth;
                AttackDamage = input.AttackDamage;
                HealAmount = input.HealAmount;
                ActionPeriodMilliseconds = input.ActionPeriodMilliseconds;
                Position = SpatialBattleMath.ClampToField(input.Position, Radius,
                    config.FieldWidth, config.FieldHeight);
                LateralSign = StableSide(Id);
            }

            public SpatialBattleG2UnitSnapshot Snapshot()
            {
                return new SpatialBattleG2UnitSnapshot
                {
                    BattleUnitId = Id,
                    IsPlayer = IsPlayer,
                    Role = Role,
                    Position = Position,
                    Radius = Radius,
                    Health = Health,
                    MaximumHealth = MaximumHealth,
                    TargetId = TargetId,
                    TargetMode = TargetMode,
                    ActionState = State,
                    RetargetCount = RetargetCount,
                    PursuitDistance = PursuitDistance,
                    SeparationCount = SeparationCount,
                    StuckDetectionCount = StuckDetectionCount,
                    SidestepCount = SidestepCount
                };
            }

            private static int StableSide(string id)
            {
                var parity = 0;
                for (var index = 0; index < id.Length; index++) parity ^= id[index];
                return (parity & 1) == 0 ? -1 : 1;
            }
        }
    }
}
