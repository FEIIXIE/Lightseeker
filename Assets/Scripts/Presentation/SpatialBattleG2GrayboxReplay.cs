using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Presentation
{
    internal sealed class SpatialG2GrayboxUnitView
    {
        public readonly string Id;
        public readonly bool IsPlayer;
        public readonly SpatialG2Role Role;
        public readonly int MaximumHealth;
        public SpatialPoint PreviousLogicPosition;
        public SpatialPoint LogicPosition;
        public int Health;
        public string TargetId = string.Empty;
        public SpatialG2TargetMode TargetMode;
        public SpatialG2ActionState State = SpatialG2ActionState.AcquireTarget;
        public bool FacingRight;
        public int LastHitTick = -1000;
        public int LastHealTick = -1000;
        public int LastSeparationTick = -1000;
        public int LastSidestepTick = -1000;
        public string LastReasonCode = string.Empty;

        public bool IsAlive => Health > 0;

        public SpatialG2GrayboxUnitView(SpatialBattleG2UnitInput input)
        {
            Id = input.BattleUnitId;
            IsPlayer = input.IsPlayer;
            Role = input.Role;
            PreviousLogicPosition = input.Position;
            LogicPosition = input.Position;
            MaximumHealth = input.MaximumHealth;
            Health = input.InitialHealth;
            FacingRight = input.IsPlayer;
        }

        public Vector2 InterpolatedLogicPosition(float alpha)
        {
            return new Vector2(
                Mathf.Lerp(PreviousLogicPosition.X, LogicPosition.X, alpha),
                Mathf.Lerp(PreviousLogicPosition.Y, LogicPosition.Y, alpha));
        }
    }

    internal sealed class SpatialBattleG2GrayboxReplay
    {
        private readonly List<SpatialG2GrayboxUnitView> units =
            new List<SpatialG2GrayboxUnitView>();
        private readonly Dictionary<string, SpatialG2GrayboxUnitView> byId =
            new Dictionary<string, SpatialG2GrayboxUnitView>(StringComparer.Ordinal);
        private readonly HashSet<int> consumedSequences = new HashSet<int>();
        private SpatialBattleG2Input input;
        private SpatialBattleG2Result result;
        private int nextEventIndex;
        private float elapsedSeconds;

        public IReadOnlyList<SpatialG2GrayboxUnitView> Units => units;
        public SpatialBattleG2Result Result => result;
        public int CurrentTick { get; private set; }
        public float InterpolationAlpha { get; private set; }
        public bool IsComplete { get; private set; }
        public int ConsumedEventCount => consumedSequences.Count;

        public void Begin(SpatialBattleG2Input battleInput, SpatialBattleG2Result battleResult)
        {
            input = battleInput ?? throw new ArgumentNullException(nameof(battleInput));
            result = battleResult ?? throw new ArgumentNullException(nameof(battleResult));
            Reset();
        }

        public void Reset()
        {
            units.Clear();
            byId.Clear();
            consumedSequences.Clear();
            nextEventIndex = 0;
            elapsedSeconds = 0f;
            CurrentTick = 0;
            InterpolationAlpha = 0f;
            IsComplete = false;
            if (input == null) return;
            AddUnits(input.PlayerUnits);
            AddUnits(input.EnemyUnits);
            units.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            RefreshFacing();
        }

        public void Advance(float deltaSeconds)
        {
            if (input == null || result == null || IsComplete) return;
            elapsedSeconds += Mathf.Max(0f, deltaSeconds);
            var tickSeconds = input.Config.TickMilliseconds / 1000f;
            var exactTick = elapsedSeconds / Mathf.Max(0.001f, tickSeconds);
            CurrentTick = Mathf.Min(result.CompletedTicks, Mathf.FloorToInt(exactTick));
            InterpolationAlpha = Mathf.Clamp01(exactTick - Mathf.Floor(exactTick));
            while (nextEventIndex < result.Events.Count &&
                   result.Events[nextEventIndex].Tick <= CurrentTick)
            {
                var battleEvent = result.Events[nextEventIndex++];
                if (!consumedSequences.Add(battleEvent.Sequence))
                    throw new InvalidOperationException("G2 presentation consumed event twice: " +
                                                        battleEvent.Sequence);
                Apply(battleEvent);
            }
            RefreshFacing();
            IsComplete = CurrentTick >= result.CompletedTicks;
        }

        public SpatialG2GrayboxUnitView Find(string id)
        {
            SpatialG2GrayboxUnitView view;
            return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out view) ? view : null;
        }

        private void AddUnits(IEnumerable<SpatialBattleG2UnitInput> source)
        {
            foreach (var unit in source)
            {
                var view = new SpatialG2GrayboxUnitView(unit);
                units.Add(view);
                byId.Add(view.Id, view);
            }
        }

        private void Apply(SpatialBattleG2Event battleEvent)
        {
            var actor = Find(battleEvent.ActorId);
            var target = Find(battleEvent.TargetId);
            if (actor != null) actor.LastReasonCode = battleEvent.ReasonCode;
            switch (battleEvent.Type)
            {
                case SpatialG2EventType.TargetAcquired:
                case SpatialG2EventType.Retargeted:
                    if (actor != null)
                    {
                        actor.TargetId = battleEvent.TargetId;
                        actor.TargetMode = battleEvent.TargetMode;
                    }
                    break;
                case SpatialG2EventType.TargetInvalidated:
                    if (actor != null)
                    {
                        actor.TargetId = string.Empty;
                        actor.TargetMode = SpatialG2TargetMode.None;
                    }
                    break;
                case SpatialG2EventType.StateChanged:
                    if (actor != null)
                    {
                        actor.State = battleEvent.State;
                        actor.TargetMode = battleEvent.TargetMode;
                    }
                    break;
                case SpatialG2EventType.Moved:
                case SpatialG2EventType.Separated:
                case SpatialG2EventType.Sidestepped:
                    if (actor != null)
                    {
                        actor.PreviousLogicPosition = actor.LogicPosition;
                        actor.LogicPosition = battleEvent.Position;
                        if (battleEvent.Type == SpatialG2EventType.Separated)
                            actor.LastSeparationTick = battleEvent.Tick;
                        if (battleEvent.Type == SpatialG2EventType.Sidestepped)
                            actor.LastSidestepTick = battleEvent.Tick;
                    }
                    break;
                case SpatialG2EventType.Damaged:
                    if (target != null)
                    {
                        target.Health = battleEvent.TargetHealthAfter;
                        target.LastHitTick = battleEvent.Tick;
                    }
                    break;
                case SpatialG2EventType.Healed:
                    if (target != null)
                    {
                        target.Health = battleEvent.TargetHealthAfter;
                        target.LastHealTick = battleEvent.Tick;
                    }
                    if (actor != null) actor.LastHealTick = battleEvent.Tick;
                    break;
                case SpatialG2EventType.Defeated:
                    if (actor != null)
                    {
                        actor.Health = 0;
                        actor.State = SpatialG2ActionState.Defeated;
                        actor.TargetId = string.Empty;
                        actor.TargetMode = SpatialG2TargetMode.None;
                    }
                    break;
            }
        }

        private void RefreshFacing()
        {
            foreach (var unit in units)
            {
                var target = Find(unit.TargetId);
                if (target != null && target.IsAlive && target.LogicPosition.X != unit.LogicPosition.X)
                {
                    unit.FacingRight = target.LogicPosition.X > unit.LogicPosition.X;
                    continue;
                }
                var deltaX = unit.LogicPosition.X - unit.PreviousLogicPosition.X;
                if (deltaX != 0) unit.FacingRight = deltaX > 0;
            }
        }
    }
}
