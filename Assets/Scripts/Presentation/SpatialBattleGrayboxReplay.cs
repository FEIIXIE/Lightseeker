using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Presentation
{
    internal sealed class SpatialGrayboxUnitView
    {
        public readonly string Id;
        public readonly bool IsPlayer;
        public readonly SpatialUnitRole Role;
        public readonly int MaximumHealth;
        public SpatialPoint PreviousLogicPosition;
        public SpatialPoint LogicPosition;
        public int Health;
        public string TargetId = string.Empty;
        public SpatialActionState State = SpatialActionState.AcquireTarget;
        public bool FacingRight;
        public int LastHitTick = -1000;

        public bool IsAlive => Health > 0;

        public SpatialGrayboxUnitView(SpatialBattleUnitInput input)
        {
            Id = input.BattleUnitId;
            IsPlayer = input.IsPlayer;
            Role = input.Role;
            PreviousLogicPosition = input.Position;
            LogicPosition = input.Position;
            MaximumHealth = input.MaximumHealth;
            Health = input.MaximumHealth;
            FacingRight = input.IsPlayer;
        }

        public Vector2 InterpolatedLogicPosition(float alpha)
        {
            return new Vector2(
                Mathf.Lerp(PreviousLogicPosition.X, LogicPosition.X, alpha),
                Mathf.Lerp(PreviousLogicPosition.Y, LogicPosition.Y, alpha));
        }
    }

    internal sealed class SpatialBattleGrayboxReplay
    {
        private readonly List<SpatialGrayboxUnitView> units = new List<SpatialGrayboxUnitView>();
        private readonly Dictionary<string, SpatialGrayboxUnitView> byId =
            new Dictionary<string, SpatialGrayboxUnitView>(StringComparer.Ordinal);
        private SpatialBattleInput input;
        private SpatialBattleResult result;
        private int nextEventIndex;
        private float elapsedSeconds;

        public IReadOnlyList<SpatialGrayboxUnitView> Units => units;
        public SpatialBattleResult Result => result;
        public int CurrentTick { get; private set; }
        public float InterpolationAlpha { get; private set; }
        public bool IsComplete { get; private set; }

        public void Begin(SpatialBattleInput battleInput, SpatialBattleResult battleResult)
        {
            input = battleInput ?? throw new ArgumentNullException(nameof(battleInput));
            result = battleResult ?? throw new ArgumentNullException(nameof(battleResult));
            Reset();
        }

        public void Reset()
        {
            units.Clear();
            byId.Clear();
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
                Apply(result.Events[nextEventIndex++]);
            }
            RefreshFacing();
            IsComplete = CurrentTick >= result.CompletedTicks;
        }

        public SpatialGrayboxUnitView Find(string id)
        {
            SpatialGrayboxUnitView view;
            return !string.IsNullOrEmpty(id) && byId.TryGetValue(id, out view) ? view : null;
        }

        private void AddUnits(IEnumerable<SpatialBattleUnitInput> source)
        {
            foreach (var unit in source)
            {
                var view = new SpatialGrayboxUnitView(unit);
                units.Add(view);
                byId.Add(view.Id, view);
            }
        }

        private void Apply(SpatialBattleEvent battleEvent)
        {
            var actor = Find(battleEvent.ActorId);
            var target = Find(battleEvent.TargetId);
            switch (battleEvent.Type)
            {
                case SpatialBattleEventType.TargetAcquired:
                    if (actor != null) actor.TargetId = battleEvent.TargetId;
                    break;
                case SpatialBattleEventType.StateChanged:
                    if (actor != null) actor.State = battleEvent.State;
                    break;
                case SpatialBattleEventType.Moved:
                    if (actor != null)
                    {
                        actor.PreviousLogicPosition = actor.LogicPosition;
                        actor.LogicPosition = battleEvent.Position;
                    }
                    break;
                case SpatialBattleEventType.Damaged:
                    if (target != null)
                    {
                        target.Health = battleEvent.TargetHealthAfter;
                        target.LastHitTick = battleEvent.Tick;
                    }
                    break;
                case SpatialBattleEventType.Defeated:
                    if (actor != null)
                    {
                        actor.Health = 0;
                        actor.State = SpatialActionState.Defeated;
                        actor.TargetId = string.Empty;
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
