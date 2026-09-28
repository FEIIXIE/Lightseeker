using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum SpatialUnitRole
    {
        Melee = 0,
        Ranged = 1
    }

    public enum SpatialActionState
    {
        AcquireTarget = 0,
        MoveIntoRange = 1,
        Windup = 2,
        Release = 3,
        Impact = 4,
        Recovery = 5,
        Defeated = 6
    }

    public enum SpatialBattleEventType
    {
        TargetAcquired = 0,
        StateChanged = 1,
        Moved = 2,
        Released = 3,
        Impacted = 4,
        Damaged = 5,
        Defeated = 6,
        BattleEnded = 7
    }

    public enum SpatialBattleOutcome
    {
        PlayerVictory = 0,
        EnemyVictory = 1,
        SafetyLimit = 2
    }

    public sealed class SpatialBattleConfig
    {
        public const string SupportedRulesVersion = "battle-rules-v2";
        public const int SupportedSpaceConfigVersion = 1;

        public string RulesVersion = SupportedRulesVersion;
        public int SpaceConfigVersion = SupportedSpaceConfigVersion;
        public int TickMilliseconds = 50;
        public int FieldWidth = 10000;
        public int FieldHeight = 16000;
        public int MeleeReach = 250;
        public int RangedReach = 3500;
        public int WindupMilliseconds = 200;
        public int MaximumTicks = 2400;
        public int MaximumEvents = 100000;

        public void Validate()
        {
            if (!string.Equals(RulesVersion, SupportedRulesVersion, StringComparison.Ordinal))
                throw new NotSupportedException("Unsupported battle rules version: " + RulesVersion);
            if (SpaceConfigVersion != SupportedSpaceConfigVersion)
                throw new NotSupportedException("Unsupported space config version: " + SpaceConfigVersion);
            if (TickMilliseconds <= 0 || TickMilliseconds > 1000)
                throw new ArgumentOutOfRangeException(nameof(TickMilliseconds));
            if (FieldWidth <= 1000 || FieldHeight <= 1000)
                throw new ArgumentOutOfRangeException(nameof(FieldWidth));
            if (MeleeReach < 0 || RangedReach <= 0 || WindupMilliseconds < 0)
                throw new ArgumentOutOfRangeException(nameof(MeleeReach));
            if (MaximumTicks <= 0 || MaximumEvents <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaximumTicks));
        }
    }

    public sealed class SpatialBattleUnitInput
    {
        public string BattleUnitId = string.Empty;
        public bool IsPlayer;
        public SpatialUnitRole Role;
        public SpatialPoint Position;
        public int Radius = 350;
        public int MoveSpeed = 1000;
        public int MaximumHealth = 30;
        public int AttackDamage = 5;
        public int AttackPeriodMilliseconds = 1000;

        public SpatialBattleUnitInput Copy()
        {
            return new SpatialBattleUnitInput
            {
                BattleUnitId = BattleUnitId,
                IsPlayer = IsPlayer,
                Role = Role,
                Position = Position,
                Radius = Radius,
                MoveSpeed = MoveSpeed,
                MaximumHealth = MaximumHealth,
                AttackDamage = AttackDamage,
                AttackPeriodMilliseconds = AttackPeriodMilliseconds
            };
        }

        internal void Validate(SpatialBattleConfig config)
        {
            if (string.IsNullOrWhiteSpace(BattleUnitId))
                throw new ArgumentException("BattleUnitId is required.");
            if (!Enum.IsDefined(typeof(SpatialUnitRole), Role))
                throw new ArgumentOutOfRangeException(nameof(Role));
            if (Radius <= 0 || Radius * 2 >= config.FieldWidth || Radius * 2 >= config.FieldHeight)
                throw new ArgumentOutOfRangeException(nameof(Radius));
            if (MoveSpeed <= 0 || MaximumHealth <= 0 || AttackDamage <= 0 ||
                AttackPeriodMilliseconds < config.TickMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(MoveSpeed));
        }
    }

    public sealed class SpatialBattleInput
    {
        public uint Seed;
        public SpatialBattleConfig Config = new SpatialBattleConfig();
        public List<SpatialBattleUnitInput> PlayerUnits = new List<SpatialBattleUnitInput>();
        public List<SpatialBattleUnitInput> EnemyUnits = new List<SpatialBattleUnitInput>();

        public void Validate()
        {
            if (Config == null) throw new ArgumentNullException(nameof(Config));
            Config.Validate();
            if (PlayerUnits == null || EnemyUnits == null || PlayerUnits.Count == 0 || EnemyUnits.Count == 0)
                throw new ArgumentException("Both teams require at least one unit.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            ValidateTeam(PlayerUnits, true, ids);
            ValidateTeam(EnemyUnits, false, ids);
        }

        private void ValidateTeam(
            IEnumerable<SpatialBattleUnitInput> units,
            bool expectedPlayer,
            ISet<string> ids)
        {
            foreach (var unit in units)
            {
                if (unit == null) throw new ArgumentNullException(nameof(units));
                unit.Validate(Config);
                if (unit.IsPlayer != expectedPlayer)
                    throw new ArgumentException("Unit team flag does not match its input collection.");
                if (!ids.Add(unit.BattleUnitId))
                    throw new ArgumentException("Duplicate BattleUnitId: " + unit.BattleUnitId);
            }
        }
    }

    public sealed class SpatialBattleEvent
    {
        public int Tick;
        public int Sequence;
        public SpatialBattleEventType Type;
        public string ActorId = string.Empty;
        public string TargetId = string.Empty;
        public SpatialActionState State;
        public SpatialPoint Position;
        public int Amount;
        public int TargetHealthAfter;

        public override string ToString()
        {
            return Tick + ":" + Sequence + ":" + Type + ":" + ActorId + ":" + TargetId +
                   ":" + State + ":" + Position + ":" + Amount + ":" + TargetHealthAfter;
        }
    }

    public sealed class SpatialBattleUnitSnapshot
    {
        public string BattleUnitId = string.Empty;
        public bool IsPlayer;
        public SpatialUnitRole Role;
        public SpatialPoint Position;
        public int Radius;
        public int Health;
        public int MaximumHealth;
        public string TargetId = string.Empty;
        public SpatialActionState ActionState;
    }

    public sealed class SpatialBattleResult
    {
        public SpatialBattleOutcome Outcome;
        public int CompletedTicks;
        public string InputHash = string.Empty;
        public string EventHash = string.Empty;
        public string FinalStateHash = string.Empty;
        public List<SpatialBattleEvent> Events = new List<SpatialBattleEvent>();
        public List<SpatialBattleUnitSnapshot> FinalUnits = new List<SpatialBattleUnitSnapshot>();
    }
}
