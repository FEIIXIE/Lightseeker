using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum SpatialG2Role
    {
        Melee = 0,
        Ranged = 1,
        Support = 2,
        LowHealthHunter = 3
    }

    public enum SpatialG2ActionState
    {
        AcquireTarget = 0,
        MoveIntoRange = 1,
        FollowFormation = 2,
        Windup = 3,
        Release = 4,
        Impact = 5,
        Recovery = 6,
        Sidestep = 7,
        Defeated = 8
    }

    public enum SpatialG2TargetMode
    {
        None = 0,
        Attack = 1,
        Heal = 2,
        Follow = 3
    }

    public enum SpatialG2EventType
    {
        TargetAcquired = 0,
        TargetRetained = 1,
        TargetInvalidated = 2,
        StateChanged = 3,
        Moved = 4,
        Released = 5,
        Impacted = 6,
        Damaged = 7,
        Healed = 8,
        Defeated = 9,
        Separated = 10,
        StuckDetected = 11,
        Sidestepped = 12,
        Retargeted = 13,
        BattleEnded = 14
    }

    public enum SpatialG2Outcome
    {
        PlayerVictory = 0,
        EnemyVictory = 1,
        DrawTimeout = 2,
        SafetyLimit = 3
    }

    public enum SpatialG2EndReason
    {
        Elimination = 0,
        TimeLimit = 1,
        ActiveActionLimit = 2,
        EventSafetyLimit = 3
    }

    public sealed class SpatialBattleG2Config
    {
        public const string SupportedRulesVersion = "battle-rules-v2-g2";
        public const int SupportedConfigVersion = 1;

        public string RulesVersion = SupportedRulesVersion;
        public int ConfigVersion = SupportedConfigVersion;
        public int TickMilliseconds = 50;
        public int FieldWidth = 10000;
        public int FieldHeight = 16000;
        public int MeleeReach = 250;
        public int RangedReach = 3500;
        public int SupportAttackReach = 2500;
        public int HealRange = 4000;
        public int SupportDesiredCenterDistance = 3400;
        public int WindupMilliseconds = 200;
        public int StuckDetectionMilliseconds = 1000;
        public int LowHealthRetargetGapBasisPoints = 2000;
        public int SidestepDistance = 700;
        public int SeparationStep = 80;
        public int TimeoutMilliseconds = 90000;
        public int MaximumActiveActions = 200;
        public int MaximumEvents = 200000;

        public int MaximumTicks => TimeoutMilliseconds / TickMilliseconds;
        public int StuckTicks => Math.Max(1,
            (StuckDetectionMilliseconds + TickMilliseconds - 1) / TickMilliseconds);

        public void Validate()
        {
            if (!string.Equals(RulesVersion, SupportedRulesVersion, StringComparison.Ordinal))
                throw new NotSupportedException("Unsupported G2 rules version: " + RulesVersion);
            if (ConfigVersion != SupportedConfigVersion)
                throw new NotSupportedException("Unsupported G2 config version: " + ConfigVersion);
            if (TickMilliseconds <= 0 || TickMilliseconds > 1000 ||
                TimeoutMilliseconds < TickMilliseconds || TimeoutMilliseconds % TickMilliseconds != 0)
                throw new ArgumentOutOfRangeException(nameof(TickMilliseconds));
            if (FieldWidth <= 1000 || FieldHeight <= 1000)
                throw new ArgumentOutOfRangeException(nameof(FieldWidth));
            if (MeleeReach < 0 || RangedReach <= 0 || SupportAttackReach <= 0 || HealRange <= 0 ||
                SupportDesiredCenterDistance <= 0 || SupportDesiredCenterDistance > HealRange)
                throw new ArgumentOutOfRangeException(nameof(HealRange));
            if (WindupMilliseconds < 0 || StuckDetectionMilliseconds < TickMilliseconds ||
                LowHealthRetargetGapBasisPoints < 0 || LowHealthRetargetGapBasisPoints > 10000 ||
                SidestepDistance <= 0 || SeparationStep <= 0 || MaximumActiveActions <= 0 ||
                MaximumEvents <= 0)
                throw new ArgumentOutOfRangeException(nameof(MaximumActiveActions));
        }
    }

    public sealed class SpatialBattleG2UnitInput
    {
        public string BattleUnitId = string.Empty;
        public bool IsPlayer;
        public SpatialG2Role Role;
        public SpatialPoint Position;
        public int Radius = 350;
        public int MoveSpeed = 1000;
        public int MaximumHealth = 40;
        public int InitialHealth = 40;
        public int AttackDamage = 5;
        public int HealAmount = 0;
        public int ActionPeriodMilliseconds = 1000;

        public SpatialBattleG2UnitInput Copy()
        {
            return new SpatialBattleG2UnitInput
            {
                BattleUnitId = BattleUnitId,
                IsPlayer = IsPlayer,
                Role = Role,
                Position = Position,
                Radius = Radius,
                MoveSpeed = MoveSpeed,
                MaximumHealth = MaximumHealth,
                InitialHealth = InitialHealth,
                AttackDamage = AttackDamage,
                HealAmount = HealAmount,
                ActionPeriodMilliseconds = ActionPeriodMilliseconds
            };
        }

        internal void Validate(SpatialBattleG2Config config)
        {
            if (string.IsNullOrWhiteSpace(BattleUnitId))
                throw new ArgumentException("BattleUnitId is required.");
            if (!Enum.IsDefined(typeof(SpatialG2Role), Role))
                throw new ArgumentOutOfRangeException(nameof(Role));
            if (Radius <= 0 || Radius * 2 >= config.FieldWidth || Radius * 2 >= config.FieldHeight)
                throw new ArgumentOutOfRangeException(nameof(Radius));
            if (MoveSpeed <= 0 || MaximumHealth <= 0 || InitialHealth <= 0 ||
                InitialHealth > MaximumHealth || AttackDamage < 0 || HealAmount < 0 ||
                ActionPeriodMilliseconds < config.TickMilliseconds)
                throw new ArgumentOutOfRangeException(nameof(MoveSpeed));
            if (Role == SpatialG2Role.Support && HealAmount <= 0)
                throw new ArgumentException("Support units require a positive HealAmount.");
            if (Role != SpatialG2Role.Support && AttackDamage <= 0)
                throw new ArgumentException("Combat units require positive AttackDamage.");
        }
    }

    public sealed class SpatialBattleG2Input
    {
        public string VectorId = string.Empty;
        public uint Seed;
        public SpatialBattleG2Config Config = new SpatialBattleG2Config();
        public List<SpatialBattleG2UnitInput> PlayerUnits = new List<SpatialBattleG2UnitInput>();
        public List<SpatialBattleG2UnitInput> EnemyUnits = new List<SpatialBattleG2UnitInput>();

        public void Validate()
        {
            if (string.IsNullOrWhiteSpace(VectorId)) throw new ArgumentException("VectorId is required.");
            if (Config == null) throw new ArgumentNullException(nameof(Config));
            Config.Validate();
            if (PlayerUnits == null || EnemyUnits == null || PlayerUnits.Count == 0 || EnemyUnits.Count == 0)
                throw new ArgumentException("Both G2 teams require at least one unit.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            ValidateTeam(PlayerUnits, true, ids);
            ValidateTeam(EnemyUnits, false, ids);
        }

        private void ValidateTeam(IEnumerable<SpatialBattleG2UnitInput> source, bool player, ISet<string> ids)
        {
            foreach (var unit in source)
            {
                if (unit == null) throw new ArgumentNullException(nameof(source));
                unit.Validate(Config);
                if (unit.IsPlayer != player)
                    throw new ArgumentException("Unit team flag does not match its collection.");
                if (!ids.Add(unit.BattleUnitId))
                    throw new ArgumentException("Duplicate BattleUnitId: " + unit.BattleUnitId);
            }
        }
    }

    public sealed class SpatialBattleG2Event
    {
        public int Tick;
        public int Sequence;
        public SpatialG2EventType Type;
        public string ActorId = string.Empty;
        public string TargetId = string.Empty;
        public SpatialG2ActionState State;
        public SpatialG2TargetMode TargetMode;
        public SpatialPoint Position;
        public int Amount;
        public int TargetHealthAfter;
        public int Overheal;
        public string ReasonCode = string.Empty;

        public override string ToString()
        {
            return Tick + ":" + Sequence + ":" + Type + ":" + ActorId + ":" + TargetId +
                   ":" + State + ":" + TargetMode + ":" + Position + ":" + Amount + ":" +
                   TargetHealthAfter + ":" + Overheal + ":" + ReasonCode;
        }
    }

    public sealed class SpatialBattleG2UnitSnapshot
    {
        public string BattleUnitId = string.Empty;
        public bool IsPlayer;
        public SpatialG2Role Role;
        public SpatialPoint Position;
        public int Radius;
        public int Health;
        public int MaximumHealth;
        public string TargetId = string.Empty;
        public SpatialG2TargetMode TargetMode;
        public SpatialG2ActionState ActionState;
        public int RetargetCount;
        public int PursuitDistance;
        public int SeparationCount;
        public int StuckDetectionCount;
        public int SidestepCount;
    }

    public sealed class SpatialBattleG2Result
    {
        public SpatialG2Outcome Outcome;
        public SpatialG2EndReason EndReason;
        public int CompletedTicks;
        public int ActiveActions;
        public int HealingDone;
        public int Overhealing;
        public int RetargetCount;
        public int SeparationCount;
        public int StuckDetectionCount;
        public int SidestepCount;
        public bool RewardsGranted;
        public string InputHash = string.Empty;
        public string EventHash = string.Empty;
        public string FinalStateHash = string.Empty;
        public List<SpatialBattleG2Event> Events = new List<SpatialBattleG2Event>();
        public List<SpatialBattleG2UnitSnapshot> FinalUnits = new List<SpatialBattleG2UnitSnapshot>();
    }

    /// <summary>
    /// Keeps the G2 graybox battle ids bound to the already approved static-art ids.
    /// This is presentation-only data and is intentionally independent from simulation state.
    /// </summary>
    public static class SpatialBattleG2PresentationMapping
    {
        public static string DefinitionIdFor(string battleUnitId)
        {
            if (string.IsNullOrEmpty(battleUnitId)) return "unit.guardian";
            if (battleUnitId.StartsWith("P-C01", StringComparison.Ordinal)) return "unit.guardian";
            if (battleUnitId.StartsWith("P-C02", StringComparison.Ordinal)) return "unit.healer";
            if (battleUnitId.StartsWith("P-C03", StringComparison.Ordinal)) return "unit.thornGuard";
            if (battleUnitId.StartsWith("P-C05", StringComparison.Ordinal)) return "unit.starblade";
            if (battleUnitId.StartsWith("E-N2", StringComparison.Ordinal)) return "enemy.n2.fighter";
            if (battleUnitId.StartsWith("E-N3", StringComparison.Ordinal)) return "enemy.n3.ranged";
            if (battleUnitId.StartsWith("E-N4", StringComparison.Ordinal)) return "enemy.n4.skirmisher";
            if (battleUnitId.StartsWith("E-N5", StringComparison.Ordinal)) return "enemy.n5.support";
            return "unit.guardian";
        }
    }
}
