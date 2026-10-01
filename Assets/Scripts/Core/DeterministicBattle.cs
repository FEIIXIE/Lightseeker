using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum BattleEventType { Attack, Heal, Defeat, Shield, Status }

    [Serializable]
    public sealed class BattleUnitState
    {
        public string Id;
        public string DefinitionId;
        public bool IsPlayer;
        public int Slot;
        public int Health;
        public int MaxHealth;
        public int AttackDamage;
        public int HealAmount;
        public int Armor;
        public int Shield;
        public CombatSpeedTier SpeedTier = CombatSpeedTier.Standard;
        public int BaseAttackSpeedBasisPoints;
        public int AttackSpeedBonusBasisPoints;
        public int PendingActionSpeedBonusBasisPoints;
        public bool IsHealer;
        public int LowHealthActionPercent;
        public int FirstActionPercent;
        public int ExecuteDamagePercent;
        public int ArmorPenetration;
        public int DamageTakenPercent;
        public int DamageTakenBoostHitsRemaining;
        public int TemporarySpeedActions;
        public string FactionId;
        public string ProfessionId;
        public CharacterQuality Quality;
        public int Energy;
        public int PendingActionBonusBasisPoints;
        public int OlinTideActionsRemaining;
        public int OlinTideTriggerCount;
        public string YiwenReturnBeaconSourceId;
        public int YiwenReturnBeaconActionsRemaining;
        public int YiwenReturnBeaconHealAmount;
        public bool YiwenLegendarySaveUsed;
        public string WhiteTrailSourceId;
        public bool WhiteTrailSourceIsPlayer;
        public bool WhiteTrailSourceLegendary;
        public int WhiteTrailActionsRemaining;
        public bool WhiteTrailTransferUsed;
        public int LeifuHuntSurgeActionsRemaining;
        public int LeifuHuntSurgeActionBonusBasisPoints;
        public int LeifuHuntSurgeSpeedBonusBasisPoints;
        public string MilaHiddenSpringSourceId;
        public int MilaHiddenSpringActionsRemaining;
        public int MilaHiddenSpringStoredHeal;
        public int MilaHiddenSpringSourceHealAmount;
        public int MilaHiddenSpringActionBonusBasisPoints;
        public int MilaHiddenSpringSpeedBonusBasisPoints;
        public bool MilaHiddenSpringSourceLegendary;
        public int MilaAfterSpringActionBonusBasisPoints;
        public int MilaAfterSpringActionsRemaining;
        public string MilaAfterSpringSourceId;
        public int SharedBarrierColumn = -1;
        public int SharedBarrierInitialCapacity;
        public int SharedBarrierRemainingCapacity;
        public int SharedBarrierActionsRemaining;
        public bool SharedBarrierHalfCounterTriggered;
        public bool SharedBarrierBreakTriggered;
        public int C05ColumnActionBuffActionsRemaining;
        public int C05ColumnActionBuffAttackBonusBasisPoints;
        public int C05ColumnActionBuffHealBonusBasisPoints;
        public string NockFireSourceId;
        public int NockFireActionsRemaining;
        public int NockFireDamageCount;
        public int NockReturnFireActionsRemaining;
        public int NockReturnFireDamageBonusBasisPoints;
        public int NockReturnFireSpeedBonusBasisPoints;
        public int N1ActionCount;
        public string N1HoldGateSourceId;
        public int N1HoldGateShieldRemaining;
        public int N1HoldGateActionsRemaining;
        public int N2ActionCount;
        public int N3ActionCount;
        public int N4ActionCount;
        public int N5ActionCount;
        public int E1ActionCount;
        public string E1SteadyLineSourceId;
        public int E1SteadyLineShieldRemaining;
        public int E1SteadyLineActionsRemaining;
        public int E2ActionCount;
        public int E3ActionCount;
        public int E3BellReturnActionsRemaining;
        public int E3BellReturnActionBonusBasisPoints;
        public string E3BellReturnSourceId;
        public int B1ActionCount;
        public bool B1ClosedArmorApplied;
        public bool B1ClosedArmorBroken;
        public int B1BrokenArmorActionsRemaining;
        public bool B1NoReturnTriggered;
        public int B1NoReturnDamageBonusActionsRemaining;
        public int B2ActionCount;
        public bool B2Recall70Triggered;
        public bool B2Recall35Triggered;
        public int B2DoubleLightSpeedPenaltyActionsRemaining;
        public int B2DeviantLightActionsRemaining;
        public string B2DeviantLightSourceId;
        public bool B2DeviantLightTriggered;

        public BattleUnitState Copy()
        {
            return new BattleUnitState
            {
                Id = Id, DefinitionId = DefinitionId, IsPlayer = IsPlayer, Slot = Slot, Health = Health,
                MaxHealth = MaxHealth, AttackDamage = AttackDamage, HealAmount = HealAmount,
                Armor = Math.Max(0, Math.Min(80, Armor)),
                Shield = Math.Max(0, Shield),
                SpeedTier = IsValidSpeedTier(SpeedTier) ? SpeedTier : CombatSpeedTier.Standard,
                BaseAttackSpeedBasisPoints = BaseAttackSpeedBasisPoints > 0
                    ? BaseAttackSpeedBasisPoints
                    : DeterministicBattle.AttackSpeedBasisPoints(SpeedTier),
                AttackSpeedBonusBasisPoints = AttackSpeedBonusBasisPoints,
                PendingActionSpeedBonusBasisPoints = Math.Max(0, PendingActionSpeedBonusBasisPoints),
                IsHealer = IsHealer, LowHealthActionPercent = LowHealthActionPercent,
                FirstActionPercent = FirstActionPercent, ExecuteDamagePercent = ExecuteDamagePercent,
                ArmorPenetration = Math.Max(0, ArmorPenetration),
                DamageTakenPercent = Math.Max(0, DamageTakenPercent),
                DamageTakenBoostHitsRemaining = Math.Max(0, DamageTakenBoostHitsRemaining),
                TemporarySpeedActions = Math.Max(0, TemporarySpeedActions),
                FactionId = FactionId,
                ProfessionId = ProfessionId, Quality = Quality,
                Energy = Math.Max(0, Math.Min(100, Energy)),
                PendingActionBonusBasisPoints = Math.Max(0, PendingActionBonusBasisPoints),
                OlinTideActionsRemaining = Math.Max(0, OlinTideActionsRemaining),
                OlinTideTriggerCount = Math.Max(0, OlinTideTriggerCount),
                YiwenReturnBeaconSourceId = YiwenReturnBeaconSourceId,
                YiwenReturnBeaconActionsRemaining = Math.Max(0, YiwenReturnBeaconActionsRemaining),
                YiwenReturnBeaconHealAmount = Math.Max(0, YiwenReturnBeaconHealAmount),
                YiwenLegendarySaveUsed = YiwenLegendarySaveUsed,
                WhiteTrailSourceId = WhiteTrailSourceId,
                WhiteTrailSourceIsPlayer = WhiteTrailSourceIsPlayer,
                WhiteTrailSourceLegendary = WhiteTrailSourceLegendary,
                WhiteTrailActionsRemaining = Math.Max(0, WhiteTrailActionsRemaining),
                WhiteTrailTransferUsed = WhiteTrailTransferUsed,
                LeifuHuntSurgeActionsRemaining = Math.Max(0, LeifuHuntSurgeActionsRemaining),
                LeifuHuntSurgeActionBonusBasisPoints = Math.Max(0,
                    LeifuHuntSurgeActionBonusBasisPoints),
                LeifuHuntSurgeSpeedBonusBasisPoints = Math.Max(0,
                    LeifuHuntSurgeSpeedBonusBasisPoints),
                MilaHiddenSpringSourceId = MilaHiddenSpringSourceId,
                MilaHiddenSpringActionsRemaining = Math.Max(0, MilaHiddenSpringActionsRemaining),
                MilaHiddenSpringStoredHeal = Math.Max(0, MilaHiddenSpringStoredHeal),
                MilaHiddenSpringSourceHealAmount = Math.Max(0, MilaHiddenSpringSourceHealAmount),
                MilaHiddenSpringActionBonusBasisPoints = Math.Max(0,
                    MilaHiddenSpringActionBonusBasisPoints),
                MilaHiddenSpringSpeedBonusBasisPoints = Math.Max(0,
                    MilaHiddenSpringSpeedBonusBasisPoints),
                MilaHiddenSpringSourceLegendary = MilaHiddenSpringSourceLegendary,
                MilaAfterSpringActionBonusBasisPoints = Math.Max(0,
                    MilaAfterSpringActionBonusBasisPoints),
                MilaAfterSpringActionsRemaining = Math.Max(0, MilaAfterSpringActionsRemaining),
                MilaAfterSpringSourceId = MilaAfterSpringSourceId,
                SharedBarrierColumn = SharedBarrierColumn,
                SharedBarrierInitialCapacity = Math.Max(0, SharedBarrierInitialCapacity),
                SharedBarrierRemainingCapacity = Math.Max(0, SharedBarrierRemainingCapacity),
                SharedBarrierActionsRemaining = Math.Max(0, SharedBarrierActionsRemaining),
                SharedBarrierHalfCounterTriggered = SharedBarrierHalfCounterTriggered,
                SharedBarrierBreakTriggered = SharedBarrierBreakTriggered,
                C05ColumnActionBuffActionsRemaining = Math.Max(0, C05ColumnActionBuffActionsRemaining),
                C05ColumnActionBuffAttackBonusBasisPoints = Math.Max(0,
                    C05ColumnActionBuffAttackBonusBasisPoints),
                C05ColumnActionBuffHealBonusBasisPoints = Math.Max(0,
                    C05ColumnActionBuffHealBonusBasisPoints),
                NockFireSourceId = NockFireSourceId,
                NockFireActionsRemaining = Math.Max(0, NockFireActionsRemaining),
                NockFireDamageCount = Math.Max(0, NockFireDamageCount),
                NockReturnFireActionsRemaining = Math.Max(0, NockReturnFireActionsRemaining),
                NockReturnFireDamageBonusBasisPoints = Math.Max(0,
                    NockReturnFireDamageBonusBasisPoints),
                NockReturnFireSpeedBonusBasisPoints = Math.Max(0,
                    NockReturnFireSpeedBonusBasisPoints),
                N1ActionCount = Math.Max(0, N1ActionCount),
                N1HoldGateSourceId = N1HoldGateSourceId,
                N1HoldGateShieldRemaining = Math.Max(0, N1HoldGateShieldRemaining),
                N1HoldGateActionsRemaining = Math.Max(0, N1HoldGateActionsRemaining),
                N2ActionCount = Math.Max(0, N2ActionCount),
                N3ActionCount = Math.Max(0, N3ActionCount),
                N4ActionCount = Math.Max(0, N4ActionCount),
                N5ActionCount = Math.Max(0, N5ActionCount),
                E1ActionCount = Math.Max(0, E1ActionCount),
                E1SteadyLineSourceId = E1SteadyLineSourceId,
                E1SteadyLineShieldRemaining = Math.Max(0, E1SteadyLineShieldRemaining),
                E1SteadyLineActionsRemaining = Math.Max(0, E1SteadyLineActionsRemaining),
                E2ActionCount = Math.Max(0, E2ActionCount),
                E3ActionCount = Math.Max(0, E3ActionCount),
                E3BellReturnActionsRemaining = Math.Max(0, E3BellReturnActionsRemaining),
                E3BellReturnActionBonusBasisPoints = Math.Max(0, E3BellReturnActionBonusBasisPoints),
                E3BellReturnSourceId = E3BellReturnSourceId,
                B1ActionCount = Math.Max(0, B1ActionCount),
                B1ClosedArmorApplied = B1ClosedArmorApplied,
                B1ClosedArmorBroken = B1ClosedArmorBroken,
                B1BrokenArmorActionsRemaining = Math.Max(0, B1BrokenArmorActionsRemaining),
                B1NoReturnTriggered = B1NoReturnTriggered,
                B1NoReturnDamageBonusActionsRemaining = Math.Max(0,
                    B1NoReturnDamageBonusActionsRemaining),
                B2ActionCount = Math.Max(0, B2ActionCount),
                B2Recall70Triggered = B2Recall70Triggered,
                B2Recall35Triggered = B2Recall35Triggered,
                B2DoubleLightSpeedPenaltyActionsRemaining = Math.Max(0,
                    B2DoubleLightSpeedPenaltyActionsRemaining),
                B2DeviantLightActionsRemaining = Math.Max(0, B2DeviantLightActionsRemaining),
                B2DeviantLightSourceId = B2DeviantLightSourceId,
                B2DeviantLightTriggered = B2DeviantLightTriggered
            };
        }

        private static bool IsValidSpeedTier(CombatSpeedTier speedTier)
        {
            return speedTier == CombatSpeedTier.Slow || speedTier == CombatSpeedTier.Standard ||
                   speedTier == CombatSpeedTier.Fast;
        }
    }

    [Serializable]
    public sealed class BattleInput
    {
        public uint Seed;
        public List<BattleUnitState> PlayerUnits = new List<BattleUnitState>();
        public List<BattleUnitState> EnemyUnits = new List<BattleUnitState>();
        public List<RoguelikeAcquiredEffect> PlayerRoguelikeEffects = new List<RoguelikeAcquiredEffect>();
    }

    [Serializable]
    public sealed class BattleEvent
    {
        public BattleEventType Type;
        public string ActorId;
        public string TargetId;
        public int Amount;
        public int TargetHealthAfter;
        public int ShieldAbsorbed;
        public int TargetShieldAfter;
        public int ActionTime;
        public int ChildIndex;
        public bool IsSecondary;
        public string SourceEffectId;
    }

    public sealed class BattleResult
    {
        public readonly List<BattleEvent> Events = new List<BattleEvent>();
        public bool PlayerWon;
        public List<BattleUnitState> PlayerUnits;
        public List<BattleUnitState> EnemyUnits;
    }

    /// <summary>Pure, deterministic combat shared by client and future server validation.</summary>
    public static class DeterministicBattle
    {
        public const int MaximumActions = 2000;
        public const int FrenzyStartMilliseconds = 120000;
        public const int FrenzyAttackMultiplier = 2;
        public const int HeroMaximumEnergy = 100;
        public const int HeroNormalActionEnergy = 40;
        public const int HeroDirectHitEnergy = 15;
        public const int E1TriggerActionCount = E1ActiveSkillIntegration.TriggerActionCount;
        public const int E1SelfShieldBasisPoints = E1ActiveSkillIntegration.SelfShieldBasisPoints;
        public const int E1FrontlineShieldBasisPoints = E1ActiveSkillIntegration.FrontlineShieldBasisPoints;
        public const int E1ShieldLifetimeActions = E1ActiveSkillIntegration.ShieldLifetimeActions;
        public const int E2TriggerActionCount = E2ActiveSkillIntegration.TriggerActionCount;
        public const int E3TriggerActionCount = E3ActiveSkillIntegration.TriggerActionCount;
        public const int B1TriggerActionCount = B1ActiveSkillIntegration.TriggerActionCount;
        public const int B2TriggerActionCount = B2ActiveSkillIntegration.TriggerActionCount;
        public const int OlinShieldDamageBasisPoints = 6000;
        public const int OlinMinimumDamageBasisPoints = 1000;
        public const int OlinNoShieldGainBasisPoints = 5000;
        public const int OlinTideActionBonusBasisPoints = 2500;
        public const int OlinTideAttackSpeedBonusBasisPoints = 2000;
        public const int OlinTideBuffActionCount = 3;
        public const int YiwenReturnBeaconActionCount = 2;
        public const int YiwenReturnBeaconLethalHealBasisPoints = 18000;
        public const int YiwenReturnBeaconExpiryHealBasisPoints = 12000;
        public const int YiwenWaitingHealBasisPoints = 12000;
        public const int LeifuWhiteTrailDamageBasisPoints = 14000;
        public const int LeifuWhiteTrailActionCount = 3;
        public const int LeifuWhiteTrailDirectDamageBonusBasisPoints = 2000;
        public const int LeifuWhiteTrailArmorReadDelta = -10;
        public const int LeifuHuntSurgeActionBonusBasisPoints = 2000;
        public const int LeifuHuntSurgeAttackSpeedBonusBasisPoints = 3000;
        public const int MilaHiddenSpringStoredHealCapBasisPoints = 20000;
        public const int C05BarrierCapacityBasisPoints = C05ActiveSkillIntegration.BarrierCapacityBasisPoints;
        public const int C05BarrierLifetimeActions = C05ActiveSkillIntegration.BarrierLifetimeActions;
        public const int C05CounterAttackBasisPoints = C05ActiveSkillIntegration.CounterAttackBasisPoints;
        public const int C05ColumnActionBonusBasisPoints =
            C05ActiveSkillIntegration.ColumnActionAttackBonusBasisPoints;
        public const int C05ColumnActionCount = C05ActiveSkillIntegration.ColumnActionCount;
        public const int C05PersonalShieldBasisPoints = C05ActiveSkillIntegration.PersonalShieldBasisPoints;
        public const int C06AttachedFireDamageBasisPoints =
            C06ActiveSkillIntegration.AttachedFireDamageBasisPoints;
        public const int C06ReturnFireTriggerCount = C06ActiveSkillIntegration.ReturnFireTriggerCount;
        public const int C06ReturnFireDamageBasisPoints = C06ActiveSkillIntegration.ReturnFireDamageBasisPoints;
        public const int C06ReturnFireDamageBonusBasisPoints =
            C06ActiveSkillIntegration.ReturnFireDamageBonusBasisPoints;
        public const int C06ReturnFireAttackSpeedBonusBasisPoints =
            C06ActiveSkillIntegration.ReturnFireAttackSpeedBonusBasisPoints;
        public const int N1TriggerActionCount = N1ActiveSkillIntegration.TriggerActionCount;
        public const int N1ShieldLifetimeActions = N1ActiveSkillIntegration.ShieldLifetimeActions;
        public const int N1SelfShieldBasisPoints = N1ActiveSkillIntegration.SelfShieldBasisPoints;
        public const int N1RearAllyShieldBasisPoints = N1ActiveSkillIntegration.RearAllyShieldBasisPoints;
        public const int N2TriggerActionCount = N2ActiveSkillIntegration.TriggerActionCount;
        public const int N2DirectDamageBasisPoints = N2ActiveSkillIntegration.DirectDamageBasisPoints;
        public const int N3TriggerActionCount = N3ActiveSkillIntegration.TriggerActionCount;
        public const int N3DirectDamageBasisPoints = N3ActiveSkillIntegration.DirectDamageBasisPoints;
        public const int N4TriggerActionCount = N4ActiveSkillIntegration.TriggerActionCount;
        public const int N4DirectDamageBasisPoints = N4ActiveSkillIntegration.DirectDamageBasisPoints;
        public const int N5TriggerActionCount = N5ActiveSkillIntegration.TriggerActionCount;
        public const int N5MaximumTargets = N5ActiveSkillIntegration.MaximumTargets;
        public const int N5HealBasisPoints = N5ActiveSkillIntegration.HealBasisPoints;
        public const int MinimumAttackSpeedBasisPoints = ContinuousAttackSpeedRules.MinimumBasisPoints;
        public const int MaximumAttackSpeedBasisPoints = ContinuousAttackSpeedRules.MaximumBasisPoints;

        public static BattleResult Run(BattleInput input)
        {
            if (input == null) throw new ArgumentNullException("input");
            var players = Clone(input.PlayerUnits);
            var enemies = Clone(input.EnemyUnits);
            var result = new BattleResult { PlayerUnits = players, EnemyUnits = enemies };
            var acted = new HashSet<string>(StringComparer.Ordinal);
            var enhancements = new D1BattleRuntime(input.PlayerRoguelikeEffects);
            enhancements.ApplyBattleStart(players);
            ApplyB1BattleStart(enemies, result.Events);
            var schedule = CreateSchedule(players, enemies, input.Seed);
            var actionTime = 0;

            for (var action = 0; action < MaximumActions && AnyAlive(players) && AnyAlive(enemies); action++)
            {
                var entry = FindNextActor(schedule);
                if (entry == null) break;
                actionTime = entry.NextActionAt;
                var allies = entry.Unit.IsPlayer ? players : enemies;
                var opponents = entry.Unit.IsPlayer ? enemies : players;
                var enemyFrenzyActive = !entry.Unit.IsPlayer &&
                    actionTime >= FrenzyStartMilliseconds;
                ExecuteAction(entry.Unit, allies, opponents, result.Events, acted, actionTime,
                    enhancements, enemyFrenzyActive, schedule);
                entry.CurrentPeriod = GetActionPeriod(entry.Unit, enemyFrenzyActive);
                entry.NextActionAt = actionTime + entry.CurrentPeriod;
            }

            if (AnyAlive(players) && AnyAlive(enemies))
                ResolveActionLimit(players, enemies, result.Events, input.Seed, actionTime);

            result.PlayerWon = AnyAlive(players) && !AnyAlive(enemies);
            return result;
        }

        public static int GetActionPeriod(CombatSpeedTier speedTier)
        {
            return GetActionPeriod(AttackSpeedBasisPoints(speedTier));
        }

        public static int GetActionPeriod(CombatSpeedTier speedTier, bool frenzyActive)
        {
            var period = GetActionPeriod(speedTier);
            return frenzyActive ? Math.Max(1, period / FrenzyAttackMultiplier) : period;
        }

        public static int AttackSpeedBasisPoints(CombatSpeedTier speedTier)
        {
            return ContinuousAttackSpeedRules.FromTier(speedTier);
        }

        public static int GetFinalAttackSpeedBasisPoints(int baseAttackSpeedBasisPoints,
            int attackSpeedBonusBasisPoints)
        {
            return ContinuousAttackSpeedRules.CalculateFinal(baseAttackSpeedBasisPoints,
                attackSpeedBonusBasisPoints);
        }

        public static int GetActionPeriod(int finalAttackSpeedBasisPoints)
        {
            return ContinuousAttackSpeedRules.CalculatePeriod(finalAttackSpeedBasisPoints);
        }

        public static int GetActionPeriod(BattleUnitState unit, bool frenzyActive = false)
        {
            if (unit == null) throw new ArgumentNullException("unit");
            var baseSpeed = unit.BaseAttackSpeedBasisPoints > 0
                ? unit.BaseAttackSpeedBasisPoints
                : AttackSpeedBasisPoints(unit.SpeedTier);
            var finalSpeed = GetFinalAttackSpeedBasisPoints(baseSpeed,
                unit.AttackSpeedBonusBasisPoints + unit.PendingActionSpeedBonusBasisPoints +
                unit.LeifuHuntSurgeSpeedBonusBasisPoints + unit.MilaHiddenSpringSpeedBonusBasisPoints +
                unit.NockReturnFireSpeedBonusBasisPoints);
            var period = GetActionPeriod(finalSpeed);
            return frenzyActive ? Math.Max(1, period / FrenzyAttackMultiplier) : period;
        }

        public static int RescheduleRemainingTime(int remaining, int oldPeriod, int newPeriod)
        {
            return ContinuousAttackSpeedRules.RescheduleRemaining(remaining, oldPeriod, newPeriod);
        }

        public static int CalculateDamageAfterArmor(int damage, int armor)
        {
            var normalizedDamage = Math.Max(1, damage);
            var normalizedArmor = Math.Max(0, Math.Min(80, armor));
            return Math.Max(1, normalizedDamage * 100 / (100 + normalizedArmor));
        }

        private static int ApplyB1IncomingDamageModifiers(BattleUnitState target, int damage)
        {
            var modified = Math.Max(1, damage);
            if (!B1ActiveSkillIntegration.IsB1(target)) return modified;
            if (target.Shield > 0)
                modified = Math.Max(1, (modified *
                    (100 - B1ActiveSkillIntegration.ShieldedDamageReductionPercent) + 50) / 100);
            if (target.B1BrokenArmorActionsRemaining > 0)
                modified = Math.Max(1, (modified *
                    (100 + B1ActiveSkillIntegration.BrokenArmorDamageTakenPercent) + 50) / 100);
            return modified;
        }

        private static void ResolveB1AfterDirectDamage(BattleUnitState target, int shieldBefore,
            int healthBefore, ICollection<BattleEvent> events, int actionTime)
        {
            if (!B1ActiveSkillIntegration.IsB1(target)) return;
            if (!target.B1ClosedArmorBroken && shieldBefore > 0 && target.Shield == 0)
            {
                target.B1ClosedArmorBroken = true;
                target.B1BrokenArmorActionsRemaining =
                    B1ActiveSkillIntegration.BrokenArmorActionCount;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = target.Id, TargetId = target.Id,
                    Amount = target.B1BrokenArmorActionsRemaining,
                    TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime, IsSecondary = true,
                    SourceEffectId = B1ActiveSkillIntegration.ClosedArmorAbilityId
                });
            }
            if (target.B1NoReturnTriggered || target.Health <= 0 ||
                (long)healthBefore * 10000 <=
                    (long)target.MaxHealth * B1ActiveSkillIntegration.NoReturnHealthThresholdBasisPoints ||
                (long)target.Health * 10000 >
                    (long)target.MaxHealth * B1ActiveSkillIntegration.NoReturnHealthThresholdBasisPoints)
                return;
            target.B1NoReturnTriggered = true;
            target.B1NoReturnDamageBonusActionsRemaining =
                B1ActiveSkillIntegration.NoReturnDamageBonusActionCount;
            var shield = Math.Max(1, (int)((long)target.MaxHealth *
                B1ActiveSkillIntegration.NoReturnShieldBasisPoints / 10000));
            target.Shield += shield;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Shield, ActorId = target.Id, TargetId = target.Id,
                Amount = shield, TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                ActionTime = actionTime, IsSecondary = true,
                SourceEffectId = B1ActiveSkillIntegration.NoReturnAbilityId
            });
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status, ActorId = target.Id, TargetId = target.Id,
                Amount = target.B1NoReturnDamageBonusActionsRemaining,
                TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                ActionTime = actionTime, IsSecondary = true,
                SourceEffectId = B1ActiveSkillIntegration.NoReturnAbilityId
            });
        }

        private static List<ActionScheduleEntry> CreateSchedule(
            List<BattleUnitState> players, List<BattleUnitState> enemies, uint seed)
        {
            var schedule = new List<ActionScheduleEntry>();
            AddScheduleEntries(schedule, players, seed);
            AddScheduleEntries(schedule, enemies, seed);
            return schedule;
        }

        private static void AddScheduleEntries(
            ICollection<ActionScheduleEntry> schedule, IEnumerable<BattleUnitState> units, uint seed)
        {
            foreach (var unit in units)
            {
                var period = GetActionPeriod(unit);
                schedule.Add(new ActionScheduleEntry
                {
                    Unit = unit,
                    NextActionAt = period,
                    CurrentPeriod = period,
                    InitiativeRank = CalculateInitiativeRank(seed, unit.Id)
                });
            }
        }

        private static ActionScheduleEntry FindNextActor(IEnumerable<ActionScheduleEntry> schedule)
        {
            ActionScheduleEntry selected = null;
            foreach (var entry in schedule)
            {
                if (entry.Unit.Health <= 0) continue;
                if (selected == null || ComesBefore(entry, selected)) selected = entry;
            }
            return selected;
        }

        private static bool ComesBefore(ActionScheduleEntry left, ActionScheduleEntry right)
        {
            if (left.NextActionAt != right.NextActionAt) return left.NextActionAt < right.NextActionAt;
            if (left.InitiativeRank != right.InitiativeRank) return left.InitiativeRank < right.InitiativeRank;
            var idComparison = StringComparer.Ordinal.Compare(left.Unit.Id, right.Unit.Id);
            if (idComparison != 0) return idComparison < 0;
            if (left.Unit.IsPlayer != right.Unit.IsPlayer) return left.Unit.IsPlayer;
            return left.Unit.Slot < right.Unit.Slot;
        }

        private static uint CalculateInitiativeRank(uint seed, string unitId)
        {
            unchecked
            {
                var hash = 2166136261u ^ seed;
                var value = unitId ?? string.Empty;
                for (var i = 0; i < value.Length; i++)
                {
                    hash ^= value[i];
                    hash *= 16777619u;
                }
                hash ^= hash >> 16;
                hash *= 2246822519u;
                hash ^= hash >> 13;
                return hash;
            }
        }

        private static void ExecuteAction(
            BattleUnitState actor,
            List<BattleUnitState> allies,
            List<BattleUnitState> opponents,
            ICollection<BattleEvent> events,
            ISet<string> acted,
            int actionTime,
            D1BattleRuntime enhancements,
            bool enemyFrenzyActive,
            IList<ActionScheduleEntry> schedule)
        {
            var firstAction = !acted.Contains(actor.Id);
            var isN1 = IsN1(actor);
            if (isN1) actor.N1ActionCount++;
            var isN2 = N2ActiveSkillIntegration.IsN2(actor);
            if (isN2) actor.N2ActionCount++;
            var isN3 = N3ActiveSkillIntegration.IsN3(actor);
            if (isN3) actor.N3ActionCount++;
            var isN4 = N4ActiveSkillIntegration.IsN4(actor);
            if (isN4) actor.N4ActionCount++;
            var isN5 = N5ActiveSkillIntegration.IsN5(actor);
            if (isN5) actor.N5ActionCount++;
            var isE1 = E1ActiveSkillIntegration.IsE1(actor);
            if (isE1) actor.E1ActionCount++;
            var isE2 = E2ActiveSkillIntegration.IsE2(actor);
            if (isE2) actor.E2ActionCount++;
            var isE3 = E3ActiveSkillIntegration.IsE3(actor);
            if (isE3) actor.E3ActionCount++;
            var isB1 = B1ActiveSkillIntegration.IsB1(actor);
            if (isB1) actor.B1ActionCount++;
            var isB2 = B2ActiveSkillIntegration.IsB2(actor);
            if (isB2) actor.B2ActionCount++;
            var pendingActionBonusBasisPoints = actor.OlinTideActionsRemaining > 0
                ? actor.PendingActionBonusBasisPoints
                : 0;
            if (actor.MilaHiddenSpringActionsRemaining > 0)
                pendingActionBonusBasisPoints += C04ActiveSkillIntegration.HiddenSpringAttackBonusBasisPoints;
            if (actor.LeifuHuntSurgeActionsRemaining > 0)
                pendingActionBonusBasisPoints += actor.LeifuHuntSurgeActionBonusBasisPoints;
            if (actor.E3BellReturnActionsRemaining > 0)
                pendingActionBonusBasisPoints += actor.E3BellReturnActionBonusBasisPoints;
            if (actor.B1NoReturnDamageBonusActionsRemaining > 0)
                pendingActionBonusBasisPoints += B1ActiveSkillIntegration.NoReturnDamageBonusBasisPoints;
            if (CanUseOlinActive(actor) && actor.Energy >= HeroMaximumEnergy)
            {
                actor.Energy = 0;
                ExecuteOlinOpenDawn(actor, allies, opponents, events, actionTime, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime);
                return;
            }
            if (CanUseYiwenActive(actor) && actor.Energy >= HeroMaximumEnergy)
            {
                actor.Energy = 0;
                ExecuteYiwenReturnBeacon(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, false);
                return;
            }
            if (CanUseLeifuActive(actor) && actor.Energy >= HeroMaximumEnergy)
            {
                actor.Energy = 0;
                ExecuteLeifuWhiteTrail(actor, allies, opponents, events, actionTime, firstAction,
                    enhancements.LowHealthActionBasisPoints(actor) + pendingActionBonusBasisPoints,
                    schedule, enhancements);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime);
                return;
            }
            if (CanUseMilaActive(actor) && actor.Energy >= HeroMaximumEnergy)
            {
                actor.Energy = 0;
                ExecuteMilaHiddenSpring(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime);
                return;
            }
            if (CanUseC05Active(actor) && actor.Energy >= HeroMaximumEnergy)
            {
                actor.Energy = 0;
                ExecuteC05RebuildBridge(actor, allies, events, actionTime, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime);
                return;
            }
            if (CanUseC06Active(actor) && actor.Energy >= HeroMaximumEnergy)
            {
                actor.Energy = 0;
                ExecuteC06ShareFire(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime);
                return;
            }
            if (isN1 && actor.N1ActionCount % N1TriggerActionCount == 0)
            {
                ExecuteN1HoldGate(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, false, false, false);
                return;
            }
            if (isE1 && actor.E1ActionCount % E1TriggerActionCount == 0)
            {
                ExecuteE1SteadyLine(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, false, false);
                return;
            }
            if (isE2 && actor.E2ActionCount % E2TriggerActionCount == 0)
            {
                ExecuteE2TrackJudgment(actor, allies, opponents, events, actionTime, enhancements, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
                return;
            }
            if (isE3 && actor.E3ActionCount % E3TriggerActionCount == 0)
            {
                ExecuteE3BellReturn(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, false, false);
                return;
            }
            if (isB1 && actor.B1ActionCount % B1TriggerActionCount == 0)
            {
                ExecuteB1BlockadeQuake(actor, allies, opponents, events, actionTime,
                    enhancements, schedule, pendingActionBonusBasisPoints);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
                return;
            }
            if (isB2 && actor.B2ActionCount % B2TriggerActionCount == 0)
            {
                ExecuteB2DoubleLight(actor, allies, opponents, events, actionTime,
                    enhancements, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
                actor.B2DoubleLightSpeedPenaltyActionsRemaining = 1;
                actor.AttackSpeedBonusBasisPoints +=
                    B2ActiveSkillIntegration.DoubleLightSpeedPenaltyBasisPoints;
                AdvanceB2Marks(opponents, actor.Id, events, actionTime);
                return;
            }
            if (isN2 && actor.N2ActionCount % N2TriggerActionCount == 0)
            {
                ExecuteN2CrossCut(actor, allies, opponents, events, actionTime, enhancements, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
                return;
            }
            if (isN3 && actor.N3ActionCount % N3TriggerActionCount == 0)
            {
                ExecuteN3HeavyBolt(actor, allies, opponents, events, actionTime, enhancements, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
                return;
            }
            if (isN4 && actor.N4ActionCount % N4TriggerActionCount == 0)
            {
                ExecuteN4MistHunt(actor, allies, opponents, events, actionTime, enhancements, schedule);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
                return;
            }
            if (isN5 && actor.N5ActionCount % N5TriggerActionCount == 0)
            {
                ExecuteN5DoubleStitch(actor, allies, events, actionTime);
                acted.Add(actor.Id);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, false, false);
                return;
            }
            var allyToHeal = FindLowestHealth(allies);
            if (actor.IsHealer && actor.HealAmount > 0 && allyToHeal != null &&
                allyToHeal.Health * 100 < allyToHeal.MaxHealth * 80)
            {
                var before = allyToHeal.Health;
                var c05ColumnHealBonus = actor.C05ColumnActionBuffActionsRemaining > 0
                    ? actor.C05ColumnActionBuffHealBonusBasisPoints : 0;
                var heal = ApplyActionBonuses(actor, allyToHeal, actor.HealAmount, false, firstAction,
                    enhancements.LowHealthActionBasisPoints(actor) + pendingActionBonusBasisPoints +
                    c05ColumnHealBonus);
                var stored = TryStoreMilaHeal(allyToHeal, heal, events, actionTime);
                if (!stored)
                {
                    allyToHeal.Health = Math.Min(allyToHeal.MaxHealth, allyToHeal.Health + heal);
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Heal, ActorId = actor.Id, TargetId = allyToHeal.Id,
                        Amount = allyToHeal.Health - before, TargetHealthAfter = allyToHeal.Health,
                        TargetShieldAfter = allyToHeal.Shield,
                        ActionTime = actionTime
                    });
                }
                acted.Add(actor.Id);
                AddNormalActionEnergy(actor);
                ConsumeActionScopedBonuses(actor, allies, events, actionTime);
                return;
            }

            var target = isB1
                ? B1ActiveSkillIntegration.SelectBasicTarget(actor, opponents)
                : isB2
                    ? B2ActiveSkillIntegration.SelectBasicTarget(opponents, actor.Id)
                    : FindNearest(actor, opponents);
            if (target == null) return;
            var attackDamage = enemyFrenzyActive
                ? Math.Min(int.MaxValue, (long)actor.AttackDamage * FrenzyAttackMultiplier)
                : actor.AttackDamage;
            var afterSpringAttackBonus = actor.MilaAfterSpringActionsRemaining > 0
                ? actor.MilaAfterSpringActionBonusBasisPoints : 0;
            var nockReturnFireBonus = actor.NockReturnFireActionsRemaining > 0
                ? actor.NockReturnFireDamageBonusBasisPoints : 0;
            var c05ColumnAttackBonus = actor.C05ColumnActionBuffActionsRemaining > 0
                ? actor.C05ColumnActionBuffAttackBonusBasisPoints : 0;
            var directValue = ApplyActionBonuses(actor, target, (int)attackDamage, true, firstAction,
                enhancements.LowHealthActionBasisPoints(actor) + pendingActionBonusBasisPoints +
                afterSpringAttackBonus + c05ColumnAttackBonus + nockReturnFireBonus);
            if (isB2)
            {
                if (target.Health * 2 <= target.MaxHealth)
                    directValue = Math.Max(1, (directValue *
                        (100 + B2ActiveSkillIntegration.BasicLowHealthBonusPercent) + 50) / 100);
                if (target.B2DeviantLightActionsRemaining > 0)
                    directValue = Math.Max(1, (directValue *
                        (100 + B2ActiveSkillIntegration.MarkedTargetBonusPercent) + 50) / 100);
            }
            var focusReduction = enhancements.FocusArmorReductionFor(actor, target);
            var whiteTrailArmorReduction = 0;
            if (IsWhiteTrailDirectAttack(actor, target))
            {
                directValue = Math.Max(1, (int)((long)directValue *
                    (10000 + LeifuWhiteTrailDirectDamageBonusBasisPoints) / 10000));
                whiteTrailArmorReduction = -LeifuWhiteTrailArmorReadDelta;
            }
            var damage = ApplyB1IncomingDamageModifiers(target, CalculateDamageAfterArmor(directValue,
                Math.Max(0, target.Armor - actor.ArmorPenetration - focusReduction -
                    whiteTrailArmorReduction)));
            if (target.DamageTakenBoostHitsRemaining > 0 && target.DamageTakenPercent > 0)
            {
                damage = Math.Max(1, (damage * (100 + target.DamageTakenPercent) + 50) / 100);
                target.DamageTakenBoostHitsRemaining--;
            }
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var sharedBarrierAbsorbed = TryAbsorbC05Barrier(actor, target, opponents, damage,
                events, actionTime, schedule);
            var remainingDamage = Math.Max(0, damage - sharedBarrierAbsorbed);
            var shieldAbsorbed = Math.Min(target.Shield, remainingDamage);
            target.Shield -= shieldAbsorbed;
            ConsumeN1ShieldContribution(target, shieldAbsorbed);
            target.Health = Math.Max(0, target.Health - (int)Math.Min(int.MaxValue,
                remainingDamage - shieldAbsorbed));
            var healthDamage = Math.Max(0, healthBefore - target.Health);
            var rescue = PrepareYiwenLethalProtection(target, opponents);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack, ActorId = actor.Id, TargetId = target.Id,
                Amount = damage, TargetHealthAfter = target.Health,
                ShieldAbsorbed = shieldAbsorbed, TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                SourceEffectId = isB2 ? B2ActiveSkillIntegration.BasicAbilityId :
                    sharedBarrierAbsorbed > 0
                        ? C05ActiveSkillIntegration.HoldBridgeAbilityId : null
            });
            ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
            ResolveB2AfterDirectDamage(actor, target, healthBefore, opponents, events, actionTime,
                schedule);
            EmitYiwenRescue(rescue, target, events, actionTime);
            enhancements.RecordDirectAttack(actor, target);
            acted.Add(actor.Id);
            AddNormalActionEnergy(actor);
            AddDirectHitEnergy(target, shieldAbsorbed + healthDamage);
            ConsumeActionScopedBonuses(actor, allies, events, actionTime, true, true, true);
            if (isB2) AdvanceB2Marks(opponents, actor.Id, events, actionTime);
            var effectiveDirectDamage = shieldAbsorbed + healthDamage;
            if (actor.IsPlayer)
            {
                enhancements.ApplyFire(actor, target, directValue, events, actionTime, opponents);
                enhancements.ApplyLeech(actor, effectiveDirectDamage, events, actionTime);
            }
            ApplyC06AttachedFire(actor, allies, target, opponents, events, actionTime, schedule);
            if (shieldBefore > 0 && target.Shield == 0)
            {
                if (target.IsPlayer) enhancements.ApplyShieldBreakRepair(target, events, actionTime);
                TriggerOlinTide(target, opponents, events, actionTime, schedule);
            }
            enhancements.ApplyHealthThreshold(target, healthBefore, events, actionTime);
            TryReleaseMilaHiddenSpring(target, events, actionTime);
            if (target.Health == 0)
            {
                RemoveN1HoldGateOnOwnerDeath(target, opponents, events, actionTime);
                RemoveC05BarrierOnOwnerDeath(target, opponents, events, actionTime);
                DiscardMilaHiddenSpring(target, events, actionTime);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat, ActorId = actor.Id, TargetId = target.Id,
                    Amount = 0, TargetHealthAfter = 0, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime
                });
                ResolveWhiteTrailMarkedDefeat(target, allies, opponents, events, actionTime, schedule);
            }
        }

        private static bool CanUseOlinActive(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   string.Equals(unit.DefinitionId, "unit.guardian", StringComparison.Ordinal) &&
                   (unit.Quality == CharacterQuality.Epic || unit.Quality == CharacterQuality.Legendary);
        }

        private static bool HasOlinPassive(BattleUnitState unit)
        {
            return unit != null && unit.Quality == CharacterQuality.Legendary &&
                   string.Equals(unit.DefinitionId, "unit.guardian", StringComparison.Ordinal);
        }

        private static bool CanUseYiwenActive(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   string.Equals(unit.DefinitionId, "unit.healer", StringComparison.Ordinal) &&
                   (unit.Quality == CharacterQuality.Epic || unit.Quality == CharacterQuality.Legendary);
        }

        private static bool HasYiwenPassive(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 && !unit.YiwenLegendarySaveUsed &&
                   unit.Quality == CharacterQuality.Legendary &&
                   string.Equals(unit.DefinitionId, "unit.healer", StringComparison.Ordinal);
        }

        private static bool CanUseLeifuActive(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   string.Equals(unit.DefinitionId, "unit.thornGuard", StringComparison.Ordinal) &&
                   (unit.Quality == CharacterQuality.Epic || unit.Quality == CharacterQuality.Legendary);
        }

        private static bool CanUseMilaActive(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   string.Equals(unit.DefinitionId, "unit.archer", StringComparison.Ordinal) &&
                   (unit.Quality == CharacterQuality.Epic || unit.Quality == CharacterQuality.Legendary);
        }

        private static bool CanUseC05Active(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   string.Equals(unit.DefinitionId, "unit.starblade", StringComparison.Ordinal) &&
                   (unit.Quality == CharacterQuality.Epic || unit.Quality == CharacterQuality.Legendary);
        }

        private static bool CanUseC06Active(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   string.Equals(unit.DefinitionId, "unit.starpriest", StringComparison.Ordinal) &&
                   (unit.Quality == CharacterQuality.Epic || unit.Quality == CharacterQuality.Legendary);
        }

        private static bool IsN1(BattleUnitState unit)
        {
            return unit != null && !unit.IsPlayer &&
                string.Equals(unit.DefinitionId, "enemy.n1.guard", StringComparison.Ordinal);
        }

        private static void ApplyB1BattleStart(IEnumerable<BattleUnitState> enemies,
            ICollection<BattleEvent> events)
        {
            foreach (var unit in enemies ?? new List<BattleUnitState>())
            {
                if (!B1ActiveSkillIntegration.IsB1(unit) || unit.Health <= 0 ||
                    unit.B1ClosedArmorApplied) continue;
                var shield = Math.Max(1, (int)((long)unit.MaxHealth *
                    B1ActiveSkillIntegration.ClosedArmorShieldBasisPoints / 10000));
                unit.B1ClosedArmorApplied = true;
                unit.Shield += shield;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Shield, ActorId = unit.Id, TargetId = unit.Id,
                    Amount = shield, TargetHealthAfter = unit.Health, TargetShieldAfter = unit.Shield,
                    ActionTime = 0, IsSecondary = true,
                    SourceEffectId = B1ActiveSkillIntegration.ClosedArmorAbilityId
                });
            }
        }

        private static void ExecuteB2DoubleLight(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime, D1BattleRuntime enhancements,
            IList<ActionScheduleEntry> schedule)
        {
            var targets = B2ActiveSkillIntegration.SelectDoubleLightTargets(opponents, 2, actor.Id);
            for (var index = 0; index < targets.Count; index++)
            {
                ExecuteEnemyRhythmDamage(actor, allies, opponents, targets[index], events,
                    actionTime, enhancements, schedule, B2ActiveSkillIntegration.DoubleLightAbilityId,
                    B2ActiveSkillIntegration.DoubleLightDamageBasisPoints, false, 0, index);
            }
        }

        private static void ResolveB2AfterDirectDamage(BattleUnitState source, BattleUnitState target,
            int healthBefore,
            IEnumerable<BattleUnitState> targetSide, ICollection<BattleEvent> events, int actionTime,
            IList<ActionScheduleEntry> schedule)
        {
            if (source == null || target == null || target.Health <= 0 ||
                ReferenceEquals(source, target) ||
                string.Equals(source.Id, target.Id, StringComparison.Ordinal)) return;
            if (B2ActiveSkillIntegration.IsB2(target))
            {
                if (!target.B2Recall70Triggered && healthBefore * 10000L >
                    target.MaxHealth * (long)B2ActiveSkillIntegration.Recall70ThresholdBasisPoints &&
                    target.Health * 10000L <= target.MaxHealth *
                    (long)B2ActiveSkillIntegration.Recall70ThresholdBasisPoints)
                {
                    target.B2Recall70Triggered = true;
                    TryRecallB2Unit(target, targetSide, events, actionTime, schedule, 1);
                }
                if (!target.B2Recall35Triggered && healthBefore * 10000L >
                    target.MaxHealth * (long)B2ActiveSkillIntegration.Recall35ThresholdBasisPoints &&
                    target.Health * 10000L <= target.MaxHealth *
                    (long)B2ActiveSkillIntegration.Recall35ThresholdBasisPoints)
                {
                    target.B2Recall35Triggered = true;
                    TryRecallB2Unit(target, targetSide, events, actionTime, schedule, 2);
                }
                return;
            }
            if (!B2ActiveSkillIntegration.IsB2(source)) return;
            if (!target.IsPlayer || target.B2DeviantLightTriggered ||
                healthBefore * 10000L <= target.MaxHealth *
                (long)B2ActiveSkillIntegration.MarkHealthThresholdBasisPoints ||
                target.Health * 10000L > target.MaxHealth *
                (long)B2ActiveSkillIntegration.MarkHealthThresholdBasisPoints) return;
            target.B2DeviantLightTriggered = true;
            target.B2DeviantLightActionsRemaining = B2ActiveSkillIntegration.MarkActionCount;
            target.B2DeviantLightSourceId = source.Id;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = source.Id,
                TargetId = target.Id,
                Amount = target.B2DeviantLightActionsRemaining,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = B2ActiveSkillIntegration.DeviantLightAbilityId
            });
        }

        private static void TryRecallB2Unit(BattleUnitState source,
            IEnumerable<BattleUnitState> targetSide, ICollection<BattleEvent> events, int actionTime,
            IList<ActionScheduleEntry> schedule, int recallIndex)
        {
            var allies = targetSide as List<BattleUnitState> ?? new List<BattleUnitState>(targetSide);
            var emptySlot = -1;
            for (var slot = 0; slot < 6; slot++)
            {
                var occupied = false;
                foreach (var unit in allies)
                    if (unit != null && unit.Health > 0 && unit.Slot == slot) occupied = true;
                if (!occupied) { emptySlot = slot; break; }
            }
            if (emptySlot < 0)
            {
                var lowest = FindLowestHealth(allies);
                if (lowest != null)
                {
                    var shield = Math.Max(1, (int)((long)lowest.MaxHealth *
                        B2ActiveSkillIntegration.FullRosterShieldBasisPoints / 10000));
                    lowest.Shield += shield;
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Shield, ActorId = source.Id, TargetId = lowest.Id,
                        Amount = shield, TargetHealthAfter = lowest.Health,
                        TargetShieldAfter = lowest.Shield, ActionTime = actionTime,
                        IsSecondary = true, SourceEffectId = B2ActiveSkillIntegration.RecallAbilityId
                    });
                }
                return;
            }
            var variant = ((source.B2ActionCount + recallIndex - 1) % 5) + 1;
            var recalled = B2ActiveSkillIntegration.CreateRecalledUnit(
                source.Id + "-recall-" + recallIndex, emptySlot, variant);
            allies.Add(recalled);
            var period = GetActionPeriod(recalled);
            schedule.Add(new ActionScheduleEntry
            {
                Unit = recalled,
                NextActionAt = actionTime + period,
                CurrentPeriod = period,
                InitiativeRank = CalculateInitiativeRank((uint)actionTime,
                    recalled.Id)
            });
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = source.Id,
                TargetId = recalled.Id,
                Amount = variant,
                TargetHealthAfter = recalled.Health,
                TargetShieldAfter = recalled.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = B2ActiveSkillIntegration.RecallAbilityId
            });
        }

        private static void ExecuteB1BlockadeQuake(BattleUnitState actor,
            IEnumerable<BattleUnitState> actorAllies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime, D1BattleRuntime enhancements,
            IList<ActionScheduleEntry> schedule, int actionBonusBasisPoints)
        {
            var targets = B1ActiveSkillIntegration.StableBlockadeTargets(opponents);
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var damageBasisPoints = Math.Max(1, (int)((long)
                    B1ActiveSkillIntegration.BlockadeQuakeDamageBasisPoints *
                    (10000 + actionBonusBasisPoints) / 10000));
                ExecuteEnemyRhythmDamage(actor, actorAllies, opponents, target, events, actionTime,
                    enhancements, schedule, B1ActiveSkillIntegration.BlockadeQuakeAbilityId,
                    damageBasisPoints, false, 0, index);
            }
        }

        private static void ExecuteE1SteadyLine(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            if (owner == null || owner.Health <= 0) return;
            var targets = new List<BattleUnitState> { owner };
            var frontline = new List<BattleUnitState>();
            foreach (var ally in allies)
                if (ally != null && ally.Health > 0 && ally.Slot >= 0 && ally.Slot < 3)
                    frontline.Add(ally);
            frontline.Sort((left, right) => left.Slot != right.Slot
                ? left.Slot.CompareTo(right.Slot) : StringComparer.Ordinal.Compare(left.Id, right.Id));
            foreach (var ally in frontline)
                if (!ReferenceEquals(ally, owner)) targets.Add(ally);
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var basisPoints = ReferenceEquals(target, owner)
                    ? E1SelfShieldBasisPoints : E1FrontlineShieldBasisPoints;
                var shield = Math.Max(1, (int)((long)target.MaxHealth * basisPoints / 10000));
                target.Shield += shield;
                target.E1SteadyLineSourceId = owner.Id;
                target.E1SteadyLineShieldRemaining += shield;
                target.E1SteadyLineActionsRemaining = E1ShieldLifetimeActions;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = owner.Id, TargetId = target.Id,
                    Amount = shield, TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield, ActionTime = actionTime,
                    ChildIndex = index, IsSecondary = true,
                    SourceEffectId = E1ActiveSkillIntegration.SteadyLineAbilityId
                });
            }
        }

        private static void ExecuteE2TrackJudgment(BattleUnitState actor,
            IEnumerable<BattleUnitState> actorAllies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime, D1BattleRuntime enhancements,
            IList<ActionScheduleEntry> schedule)
        {
            var target = FindLowestHealth(opponents);
            if (target == null) return;
            ExecuteEnemyRhythmDamage(actor, actorAllies, opponents, target, events, actionTime,
                enhancements, schedule, E2ActiveSkillIntegration.TrackJudgmentAbilityId,
                E2ActiveSkillIntegration.DirectDamageBasisPoints,
                target.Health * 2 <= target.MaxHealth,
                E2ActiveSkillIntegration.LowHealthBonusPercent);
        }

        private static void ExecuteE3BellReturn(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            if (actor == null || actor.Health <= 0) return;
            var targets = new List<BattleUnitState>();
            foreach (var ally in allies)
                if (ally != null && ally.Health > 0) targets.Add(ally);
            targets.Sort((left, right) => left.Slot != right.Slot
                ? left.Slot.CompareTo(right.Slot) : StringComparer.Ordinal.Compare(left.Id, right.Id));
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var before = target.Health;
                var heal = Math.Max(1, (int)((long)Math.Max(1, actor.HealAmount) *
                    E3ActiveSkillIntegration.HealBasisPoints / 10000));
                target.Health = Math.Min(target.MaxHealth, target.Health + heal);
                target.E3BellReturnActionsRemaining = ReferenceEquals(target, actor) ? 2 : 1;
                target.E3BellReturnActionBonusBasisPoints = E3ActiveSkillIntegration.NextActionBonusBasisPoints;
                target.E3BellReturnSourceId = actor.Id;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Heal, ActorId = actor.Id, TargetId = target.Id,
                    Amount = target.Health - before, TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield, ActionTime = actionTime,
                    ChildIndex = index, SourceEffectId = E3ActiveSkillIntegration.BellReturnAbilityId
                });
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = actor.Id, TargetId = target.Id,
                    Amount = E3ActiveSkillIntegration.NextActionBonusBasisPoints,
                    TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime, ChildIndex = index, IsSecondary = true,
                    SourceEffectId = E3ActiveSkillIntegration.BellReturnAbilityId
                });
            }
        }

        private static void ExecuteN2CrossCut(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events,
            int actionTime, D1BattleRuntime enhancements, IList<ActionScheduleEntry> schedule)
        {
            if (actor == null || actor.Health <= 0) return;
            var target = FindNearest(actor, opponents);
            if (target == null || target.Health <= 0) return;
            var directValue = Math.Max(1, (int)((long)actor.AttackDamage *
                N2DirectDamageBasisPoints / 10000));
            var damage = ApplyB1IncomingDamageModifiers(target, CalculateDamageAfterArmor(directValue,
                Math.Max(0, target.Armor - actor.ArmorPenetration)));
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var sharedBarrierAbsorbed = TryAbsorbC05Barrier(actor, target, opponents, damage,
                events, actionTime, schedule);
            var remainingDamage = Math.Max(0, damage - sharedBarrierAbsorbed);
            var shieldAbsorbed = Math.Min(target.Shield, remainingDamage);
            target.Shield -= shieldAbsorbed;
            ConsumeN1ShieldContribution(target, shieldAbsorbed);
            target.Health = Math.Max(0, target.Health - (remainingDamage - shieldAbsorbed));
            var healthDamage = Math.Max(0, healthBefore - target.Health);
            var rescue = PrepareYiwenLethalProtection(target, opponents);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack,
                ActorId = actor.Id,
                TargetId = target.Id,
                Amount = damage,
                TargetHealthAfter = target.Health,
                ShieldAbsorbed = shieldAbsorbed,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                SourceEffectId = N2ActiveSkillIntegration.CrossCutAbilityId
            });
            ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
            ResolveB2AfterDirectDamage(actor, target, healthBefore, opponents, events, actionTime,
                schedule);
            EmitYiwenRescue(rescue, target, events, actionTime);
            AddDirectHitEnergy(target, shieldAbsorbed + healthDamage);
            if (shieldBefore > 0 && target.Shield == 0)
                TriggerOlinTide(target, opponents, events, actionTime, schedule);
            enhancements.ApplyHealthThreshold(target, healthBefore, events, actionTime);
            TryReleaseMilaHiddenSpring(target, events, actionTime);
            if (target.Health <= 0)
            {
                RemoveN1HoldGateOnOwnerDeath(target, opponents, events, actionTime);
                RemoveC05BarrierOnOwnerDeath(target, opponents, events, actionTime);
                DiscardMilaHiddenSpring(target, events, actionTime);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat,
                    ActorId = actor.Id,
                    TargetId = target.Id,
                    Amount = 0,
                    TargetHealthAfter = 0,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    SourceEffectId = N2ActiveSkillIntegration.CrossCutAbilityId
                });
                ResolveWhiteTrailMarkedDefeat(target,
                    opponents as List<BattleUnitState> ?? new List<BattleUnitState>(opponents),
                    allies as List<BattleUnitState> ?? new List<BattleUnitState>(allies),
                    events, actionTime, schedule);
            }
        }

        private static void ExecuteN3HeavyBolt(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime, D1BattleRuntime enhancements,
            IList<ActionScheduleEntry> schedule)
        {
            var target = FindHighestAttack(opponents);
            if (target == null) return;
            ExecuteEnemyRhythmDamage(actor, allies, opponents, target, events, actionTime, enhancements,
                schedule, N3ActiveSkillIntegration.HeavyBoltAbilityId,
                N3ActiveSkillIntegration.DirectDamageBasisPoints, false);
        }

        private static void ExecuteN4MistHunt(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime, D1BattleRuntime enhancements,
            IList<ActionScheduleEntry> schedule)
        {
            var target = FindLowestHealth(opponents);
            if (target == null) return;
            ExecuteEnemyRhythmDamage(actor, allies, opponents, target, events, actionTime, enhancements,
                schedule, N4ActiveSkillIntegration.MistHuntAbilityId,
                N4ActiveSkillIntegration.DirectDamageBasisPoints,
                target.Health * 2 <= target.MaxHealth);
        }

        private static void ExecuteEnemyRhythmDamage(BattleUnitState actor,
            IEnumerable<BattleUnitState> actorAllies, IEnumerable<BattleUnitState> targetSide,
            BattleUnitState target,
            ICollection<BattleEvent> events, int actionTime, D1BattleRuntime enhancements,
            IList<ActionScheduleEntry> schedule, string abilityId, int damageBasisPoints,
            bool executeBonus, int executeBonusPercent = N4ActiveSkillIntegration.ExecuteDamageBonusPercent,
            int childIndex = 0)
        {
            if (actor == null || target == null || actor.Health <= 0 || target.Health <= 0) return;
            var directValue = Math.Max(1, (int)((long)actor.AttackDamage * damageBasisPoints / 10000));
            if (executeBonus)
                directValue = Math.Max(1, (directValue *
                    (100 + executeBonusPercent) + 50) / 100);
            var damage = ApplyB1IncomingDamageModifiers(target, CalculateDamageAfterArmor(directValue,
                Math.Max(0, target.Armor - actor.ArmorPenetration)));
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var sharedBarrierAbsorbed = TryAbsorbC05Barrier(actor, target,
                targetSide, damage, events, actionTime, schedule);
            var remainingDamage = Math.Max(0, damage - sharedBarrierAbsorbed);
            var shieldAbsorbed = Math.Min(target.Shield, remainingDamage);
            target.Shield -= shieldAbsorbed;
            ConsumeN1ShieldContribution(target, shieldAbsorbed);
            target.Health = Math.Max(0, target.Health - (remainingDamage - shieldAbsorbed));
            var healthDamage = Math.Max(0, healthBefore - target.Health);
            var rescue = PrepareYiwenLethalProtection(target, targetSide);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack, ActorId = actor.Id, TargetId = target.Id,
                Amount = damage, TargetHealthAfter = target.Health,
                ShieldAbsorbed = shieldAbsorbed, TargetShieldAfter = target.Shield,
                ActionTime = actionTime, ChildIndex = childIndex, SourceEffectId = abilityId
            });
            ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
            ResolveB2AfterDirectDamage(actor, target, healthBefore, targetSide, events, actionTime,
                schedule);
            EmitYiwenRescue(rescue, target, events, actionTime);
            AddDirectHitEnergy(target, shieldAbsorbed + healthDamage);
            if (shieldBefore > 0 && target.Shield == 0)
                TriggerOlinTide(target, targetSide, events, actionTime, schedule);
            enhancements.ApplyHealthThreshold(target, healthBefore, events, actionTime);
            TryReleaseMilaHiddenSpring(target, events, actionTime);
            if (target.Health <= 0)
            {
                RemoveN1HoldGateOnOwnerDeath(target, targetSide, events, actionTime);
                RemoveC05BarrierOnOwnerDeath(target, targetSide, events, actionTime);
                DiscardMilaHiddenSpring(target, events, actionTime);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat, ActorId = actor.Id, TargetId = target.Id,
                    Amount = 0, TargetHealthAfter = 0, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime, SourceEffectId = abilityId
                });
                ResolveWhiteTrailMarkedDefeat(target,
                    targetSide as List<BattleUnitState> ?? new List<BattleUnitState>(targetSide),
                    actorAllies as List<BattleUnitState> ?? new List<BattleUnitState>(actorAllies),
                    events, actionTime, schedule);
            }
        }

        private static void ExecuteN5DoubleStitch(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            var targets = FindLowestHealthTargets(allies, N5ActiveSkillIntegration.MaximumTargets);
            targets.RemoveAll(unit => unit.Health >= unit.MaxHealth);
            if (targets.Count == 0)
            {
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = actor.Id, TargetId = actor.Id,
                    Amount = 0, TargetHealthAfter = actor.Health, TargetShieldAfter = actor.Shield,
                    ActionTime = actionTime, SourceEffectId = N5ActiveSkillIntegration.DoubleStitchAbilityId
                });
                return;
            }
            for (var index = 0; index < targets.Count; index++)
            {
                var target = targets[index];
                var before = target.Health;
                var requested = Math.Max(1, (int)((long)Math.Max(1, actor.HealAmount) *
                    N5ActiveSkillIntegration.HealBasisPoints / 10000));
                target.Health = Math.Min(target.MaxHealth, target.Health + requested);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Heal, ActorId = actor.Id, TargetId = target.Id,
                    Amount = target.Health - before, TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield, ActionTime = actionTime,
                    ChildIndex = index, SourceEffectId = N5ActiveSkillIntegration.DoubleStitchAbilityId
                });
            }
        }

        private static void ExecuteN1HoldGate(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            if (owner == null || owner.Health <= 0) return;
            var targets = new List<BattleUnitState> { owner };
            targets.AddRange(N1ActiveSkillIntegration.StableSameColumnRearTargets(owner, allies));
            foreach (var target in targets)
            {
                var basisPoints = ReferenceEquals(target, owner)
                    ? N1SelfShieldBasisPoints : N1RearAllyShieldBasisPoints;
                var shield = Math.Max(1, (int)((long)target.MaxHealth * basisPoints / 10000));
                target.Shield += shield;
                target.N1HoldGateSourceId = owner.Id;
                target.N1HoldGateShieldRemaining += shield;
                target.N1HoldGateActionsRemaining = N1ShieldLifetimeActions;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = owner.Id,
                    TargetId = target.Id,
                    Amount = shield,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = N1ActiveSkillIntegration.HoldGateAbilityId
                });
            }
        }

        private static void ConsumeN1ShieldContribution(BattleUnitState target, int absorbed)
        {
            ConsumeE1ShieldContribution(target, absorbed);
            if (target == null || absorbed <= 0 || target.N1HoldGateShieldRemaining <= 0) return;
            target.N1HoldGateShieldRemaining = Math.Max(0,
                target.N1HoldGateShieldRemaining - absorbed);
            if (target.N1HoldGateShieldRemaining == 0)
            {
                target.N1HoldGateSourceId = null;
                target.N1HoldGateActionsRemaining = 0;
            }
        }

        private static void ConsumeE1ShieldContribution(BattleUnitState target, int absorbed)
        {
            if (target == null || absorbed <= 0 || target.E1SteadyLineShieldRemaining <= 0) return;
            target.E1SteadyLineShieldRemaining = Math.Max(0,
                target.E1SteadyLineShieldRemaining - absorbed);
            if (target.E1SteadyLineShieldRemaining == 0)
            {
                target.E1SteadyLineSourceId = null;
                target.E1SteadyLineActionsRemaining = 0;
            }
        }

        private static void AdvanceE1SteadyLine(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            if (!E1ActiveSkillIntegration.IsE1(owner)) return;
            foreach (var target in allies ?? new List<BattleUnitState>())
            {
                if (ReferenceEquals(target, owner) || target == null ||
                    !string.Equals(target.E1SteadyLineSourceId, owner.Id,
                        StringComparison.Ordinal) || target.E1SteadyLineActionsRemaining <= 0) continue;
                target.E1SteadyLineActionsRemaining--;
                if (target.E1SteadyLineActionsRemaining > 0) continue;
                var removed = Math.Min(target.Shield, target.E1SteadyLineShieldRemaining);
                target.Shield -= removed;
                target.E1SteadyLineShieldRemaining = 0;
                target.E1SteadyLineSourceId = null;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = owner.Id, TargetId = target.Id,
                    Amount = -1, TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime, IsSecondary = true,
                    SourceEffectId = E1ActiveSkillIntegration.SteadyLineAbilityId
                });
            }
            if (!string.Equals(owner.E1SteadyLineSourceId, owner.Id, StringComparison.Ordinal) ||
                owner.E1SteadyLineActionsRemaining <= 0) return;
            owner.E1SteadyLineActionsRemaining--;
            if (owner.E1SteadyLineActionsRemaining > 0) return;
            var ownerRemoved = Math.Min(owner.Shield, owner.E1SteadyLineShieldRemaining);
            owner.Shield -= ownerRemoved;
            owner.E1SteadyLineShieldRemaining = 0;
            owner.E1SteadyLineSourceId = null;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status, ActorId = owner.Id, TargetId = owner.Id,
                Amount = -1, TargetHealthAfter = owner.Health, TargetShieldAfter = owner.Shield,
                ActionTime = actionTime, IsSecondary = true,
                SourceEffectId = E1ActiveSkillIntegration.SteadyLineAbilityId
            });
        }

        private static void AdvanceE3BellReturn(BattleUnitState actor,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (actor == null || actor.E3BellReturnActionsRemaining <= 0) return;
            actor.E3BellReturnActionsRemaining--;
            if (actor.E3BellReturnActionsRemaining > 0) return;
            var sourceId = actor.E3BellReturnSourceId;
            actor.E3BellReturnActionBonusBasisPoints = 0;
            actor.E3BellReturnSourceId = null;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status, ActorId = sourceId, TargetId = actor.Id,
                Amount = -1, TargetHealthAfter = actor.Health, TargetShieldAfter = actor.Shield,
                ActionTime = actionTime, IsSecondary = true,
                SourceEffectId = E3ActiveSkillIntegration.BellReturnAbilityId
            });
        }

        private static void RemoveN1HoldGateOnOwnerDeath(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            RemoveE1SteadyLineOnOwnerDeath(owner, allies, events, actionTime);
            if (!IsN1(owner)) return;
            foreach (var target in allies ?? new List<BattleUnitState>())
            {
                if (target == null || !string.Equals(target.N1HoldGateSourceId, owner.Id,
                        StringComparison.Ordinal)) continue;
                var removed = Math.Min(target.Shield, target.N1HoldGateShieldRemaining);
                target.Shield -= removed;
                target.N1HoldGateShieldRemaining = 0;
                target.N1HoldGateActionsRemaining = 0;
                target.N1HoldGateSourceId = null;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = owner.Id,
                    TargetId = target.Id,
                    Amount = -1,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = N1ActiveSkillIntegration.HoldGateAbilityId
                });
            }
        }

        private static void RemoveE1SteadyLineOnOwnerDeath(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            if (!E1ActiveSkillIntegration.IsE1(owner)) return;
            foreach (var target in allies ?? new List<BattleUnitState>())
            {
                if (target == null || !string.Equals(target.E1SteadyLineSourceId, owner.Id,
                        StringComparison.Ordinal)) continue;
                var removed = Math.Min(target.Shield, target.E1SteadyLineShieldRemaining);
                target.Shield -= removed;
                target.E1SteadyLineShieldRemaining = 0;
                target.E1SteadyLineActionsRemaining = 0;
                target.E1SteadyLineSourceId = null;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = owner.Id, TargetId = target.Id,
                    Amount = -1, TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime, IsSecondary = true,
                    SourceEffectId = E1ActiveSkillIntegration.SteadyLineAbilityId
                });
            }
        }

        private static void AdvanceN1HoldGate(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            if (!IsN1(owner)) return;
            foreach (var target in allies ?? new List<BattleUnitState>())
            {
                if (target == null || !string.Equals(target.N1HoldGateSourceId, owner.Id,
                        StringComparison.Ordinal) || target.N1HoldGateActionsRemaining <= 0) continue;
                target.N1HoldGateActionsRemaining--;
                if (target.N1HoldGateActionsRemaining > 0) continue;
                var removed = Math.Min(target.Shield, target.N1HoldGateShieldRemaining);
                target.Shield -= removed;
                target.N1HoldGateShieldRemaining = 0;
                target.N1HoldGateSourceId = null;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = owner.Id,
                    TargetId = target.Id,
                    Amount = -1,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = N1ActiveSkillIntegration.HoldGateAbilityId
                });
            }
            AdvanceN1HoldGateForTarget(owner, events, actionTime);
        }

        private static void AdvanceN1HoldGateForTarget(BattleUnitState owner,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (owner == null || owner.N1HoldGateActionsRemaining <= 0) return;
            owner.N1HoldGateActionsRemaining--;
            if (owner.N1HoldGateActionsRemaining > 0) return;
            var removed = Math.Min(owner.Shield, owner.N1HoldGateShieldRemaining);
            owner.Shield -= removed;
            owner.N1HoldGateShieldRemaining = 0;
            owner.N1HoldGateSourceId = null;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = owner.Id,
                TargetId = owner.Id,
                Amount = -1,
                TargetHealthAfter = owner.Health,
                TargetShieldAfter = owner.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = N1ActiveSkillIntegration.HoldGateAbilityId
            });
        }

        private static bool HasC06Passive(BattleUnitState unit)
        {
            return unit != null && unit.Health > 0 &&
                   unit.Quality == CharacterQuality.Legendary &&
                   string.Equals(unit.DefinitionId, "unit.starpriest", StringComparison.Ordinal);
        }

        private static bool HasMilaSpring(BattleUnitState unit)
        {
            return unit != null && unit.MilaHiddenSpringActionsRemaining > 0;
        }

        private static void AddNormalActionEnergy(BattleUnitState actor)
        {
            if (!CanUseOlinActive(actor) && !CanUseYiwenActive(actor) &&
                !CanUseLeifuActive(actor) && !CanUseMilaActive(actor) && !CanUseC05Active(actor) &&
                !CanUseC06Active(actor)) return;
            actor.Energy = Math.Min(HeroMaximumEnergy, actor.Energy + HeroNormalActionEnergy);
        }

        private static void AddDirectHitEnergy(BattleUnitState target, int effectiveHit)
        {
            if (effectiveHit <= 0 || target.Health <= 0 ||
                (!CanUseOlinActive(target) && !CanUseYiwenActive(target) &&
                  !CanUseLeifuActive(target) && !CanUseMilaActive(target) &&
                  !CanUseC05Active(target) && !CanUseC06Active(target))) return;
            target.Energy = Math.Min(HeroMaximumEnergy, target.Energy + HeroDirectHitEnergy);
        }

        private static void ExecuteC05RebuildBridge(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events,
            int actionTime, IList<ActionScheduleEntry> schedule)
        {
            RemoveC05Barrier(actor, events, actionTime, "REPLACED");
            var capacity = Math.Max(1, (int)((long)actor.MaxHealth *
                C05BarrierCapacityBasisPoints / 10000));
            actor.SharedBarrierColumn = C05ColumnForSlot(actor.Slot);
            actor.SharedBarrierInitialCapacity = capacity;
            actor.SharedBarrierRemainingCapacity = capacity;
            actor.SharedBarrierActionsRemaining = C05BarrierLifetimeActions;
            actor.SharedBarrierHalfCounterTriggered = false;
            actor.SharedBarrierBreakTriggered = false;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = actor.Id,
                TargetId = actor.Id,
                Amount = capacity,
                TargetHealthAfter = actor.Health,
                TargetShieldAfter = actor.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C05ActiveSkillIntegration.RebuildBridgeAbilityId
            });
        }

        private static void ExecuteC06ShareFire(BattleUnitState source,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            var ordered = new List<BattleUnitState>();
            foreach (var ally in allies ?? new List<BattleUnitState>())
                if (ally != null && ally.Health > 0) ordered.Add(ally);
            ordered.Sort((left, right) => left.Slot != right.Slot
                ? left.Slot.CompareTo(right.Slot)
                : StringComparer.Ordinal.Compare(left.Id, right.Id));
            foreach (var ally in ordered)
            {
                ally.NockFireSourceId = source.Id;
                ally.NockFireActionsRemaining = string.Equals(ally.Id, source.Id,
                    StringComparison.Ordinal)
                    ? C06ActiveSkillIntegration.NockSelfChargeCount
                    : C06ActiveSkillIntegration.AllyChargeCount;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = source.Id,
                    TargetId = ally.Id,
                    Amount = ally.NockFireActionsRemaining,
                    TargetHealthAfter = ally.Health,
                    TargetShieldAfter = ally.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C06ActiveSkillIntegration.ShareFireAbilityId
                });
            }
        }

        private static void ApplyC06AttachedFire(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, BattleUnitState primaryTarget,
            IEnumerable<BattleUnitState> opponents, ICollection<BattleEvent> events, int actionTime,
            IList<ActionScheduleEntry> schedule)
        {
            if (actor == null || actor.NockFireActionsRemaining <= 0) return;
            actor.NockFireActionsRemaining--;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = actor.NockFireSourceId,
                TargetId = actor.Id,
                Amount = actor.NockFireActionsRemaining > 0 ? actor.NockFireActionsRemaining : -1,
                TargetHealthAfter = actor.Health,
                TargetShieldAfter = actor.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C06ActiveSkillIntegration.FireChargeStatusId
            });
            if (primaryTarget == null || primaryTarget.Health <= 0) return;
            var source = FindUnitById(allies, actor.NockFireSourceId);
            if (!HasC06Passive(source)) return;
            var rawDamage = Math.Max(1, (int)((long)actor.AttackDamage *
                C06ActiveSkillIntegration.AttachedFireDamageBasisPoints / 10000));
            var healthBefore = primaryTarget.Health;
            var shieldBefore = primaryTarget.Shield;
            var damage = ApplyB1IncomingDamageModifiers(primaryTarget,
                CalculateDamageAfterArmor(rawDamage,
                    Math.Max(0, primaryTarget.Armor - actor.ArmorPenetration)));
            var shieldAbsorbed = Math.Min(primaryTarget.Shield, damage);
            primaryTarget.Shield -= shieldAbsorbed;
            ConsumeN1ShieldContribution(primaryTarget, shieldAbsorbed);
            primaryTarget.Health = Math.Max(0, primaryTarget.Health - (damage - shieldAbsorbed));
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack,
                ActorId = actor.Id,
                TargetId = primaryTarget.Id,
                Amount = damage,
                TargetHealthAfter = primaryTarget.Health,
                ShieldAbsorbed = shieldAbsorbed,
                TargetShieldAfter = primaryTarget.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C06ActiveSkillIntegration.ShareFireAbilityId
            });
            ResolveB1AfterDirectDamage(primaryTarget, shieldBefore, healthBefore, events, actionTime);
            source.NockFireDamageCount++;
            if (source.NockFireDamageCount % C06ActiveSkillIntegration.ReturnFireTriggerCount == 0)
                ExecuteC06ReturnFire(source, allies, opponents, events, actionTime, schedule);
        }

        private static void ExecuteC06ReturnFire(BattleUnitState source,
            IEnumerable<BattleUnitState> allies, IEnumerable<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime,
            IList<ActionScheduleEntry> schedule)
        {
            var target = FindLowestHealth(opponents);
            if (target != null)
            {
                var rawDamage = Math.Max(1, (int)((long)source.AttackDamage *
                    C06ActiveSkillIntegration.ReturnFireDamageBasisPoints / 10000));
                var healthBefore = target.Health;
                var shieldBefore = target.Shield;
                var damage = ApplyB1IncomingDamageModifiers(target,
                    CalculateDamageAfterArmor(rawDamage,
                        Math.Max(0, target.Armor - source.ArmorPenetration)));
                var shieldAbsorbed = Math.Min(target.Shield, damage);
                target.Shield -= shieldAbsorbed;
                ConsumeN1ShieldContribution(target, shieldAbsorbed);
                target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Attack,
                    ActorId = source.Id,
                    TargetId = target.Id,
                    Amount = damage,
                    TargetHealthAfter = target.Health,
                    ShieldAbsorbed = shieldAbsorbed,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C06ActiveSkillIntegration.ReturnFireAbilityId
                });
                ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
            }
            foreach (var ally in allies ?? new List<BattleUnitState>())
            {
                if (ally == null || ally.Health <= 0) continue;
                ally.NockReturnFireActionsRemaining = C06ActiveSkillIntegration.ReturnFireActionCount;
                ally.NockReturnFireDamageBonusBasisPoints =
                    C06ActiveSkillIntegration.ReturnFireDamageBonusBasisPoints;
                ally.NockReturnFireSpeedBonusBasisPoints =
                    C06ActiveSkillIntegration.ReturnFireAttackSpeedBonusBasisPoints;
                RescheduleAttackSpeedBonus(ally, actionTime, schedule);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = source.Id,
                    TargetId = ally.Id,
                    Amount = C06ActiveSkillIntegration.ReturnFireActionCount,
                    TargetHealthAfter = ally.Health,
                    TargetShieldAfter = ally.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C06ActiveSkillIntegration.ReturnFireBuffStatusId
                });
            }
        }

        private static void RescheduleAttackSpeedBonus(BattleUnitState unit, int actionTime,
            IList<ActionScheduleEntry> schedule)
        {
            if (unit == null || schedule == null) return;
            foreach (var entry in schedule)
            {
                if (!ReferenceEquals(entry.Unit, unit)) continue;
                var oldPeriod = Math.Max(1, entry.CurrentPeriod);
                var remaining = Math.Max(0, entry.NextActionAt - actionTime);
                var newPeriod = GetActionPeriod(unit);
                entry.NextActionAt = actionTime + RescheduleRemainingTime(remaining, oldPeriod, newPeriod);
                entry.CurrentPeriod = newPeriod;
                return;
            }
        }

        private static BattleUnitState FindUnitById(IEnumerable<BattleUnitState> units, string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var unit in units ?? new List<BattleUnitState>())
                if (unit != null && string.Equals(unit.Id, id, StringComparison.Ordinal)) return unit;
            return null;
        }

        private static int TryAbsorbC05Barrier(BattleUnitState attacker, BattleUnitState target,
            IEnumerable<BattleUnitState> targetSide, int damage, ICollection<BattleEvent> events,
            int actionTime, IList<ActionScheduleEntry> schedule)
        {
            if (attacker == null || target == null || damage <= 0) return 0;
            BattleUnitState barrierOwner = null;
            foreach (var candidate in targetSide ?? new List<BattleUnitState>())
            {
                if (candidate == null || candidate.Health <= 0 ||
                    candidate.SharedBarrierRemainingCapacity <= 0 ||
                    candidate.SharedBarrierColumn != C05ColumnForSlot(target.Slot)) continue;
                if (barrierOwner == null || StringComparer.Ordinal.Compare(candidate.Id, barrierOwner.Id) < 0)
                    barrierOwner = candidate;
            }
            if (barrierOwner == null) return 0;

            var absorbed = Math.Min(damage, barrierOwner.SharedBarrierRemainingCapacity);
            var before = barrierOwner.SharedBarrierRemainingCapacity;
            barrierOwner.SharedBarrierRemainingCapacity -= absorbed;
            var crossedHalf = !barrierOwner.SharedBarrierHalfCounterTriggered &&
                before > barrierOwner.SharedBarrierInitialCapacity * 0.5f &&
                barrierOwner.SharedBarrierRemainingCapacity <=
                    barrierOwner.SharedBarrierInitialCapacity * 0.5f;
            if (crossedHalf)
            {
                barrierOwner.SharedBarrierHalfCounterTriggered = true;
                if (barrierOwner.Quality == CharacterQuality.Legendary)
                    ApplyC05CounterAttack(barrierOwner, attacker, events, actionTime);
            }
            if (barrierOwner.SharedBarrierRemainingCapacity <= 0 &&
                !barrierOwner.SharedBarrierBreakTriggered)
            {
                barrierOwner.SharedBarrierBreakTriggered = true;
                BreakC05Barrier(barrierOwner, targetSide, events, actionTime, schedule);
            }
            return absorbed;
        }

        private static void ApplyC05CounterAttack(BattleUnitState source, BattleUnitState target,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (source == null || target == null || target.Health <= 0) return;
            var raw = Math.Max(1, (int)((long)source.AttackDamage *
                C05CounterAttackBasisPoints / 10000));
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var damage = ApplyB1IncomingDamageModifiers(target, CalculateDamageAfterArmor(raw,
                Math.Max(0, target.Armor - source.ArmorPenetration)));
            var shieldAbsorbed = Math.Min(target.Shield, damage);
            target.Shield -= shieldAbsorbed;
            ConsumeN1ShieldContribution(target, shieldAbsorbed);
            target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack,
                ActorId = source.Id,
                TargetId = target.Id,
                Amount = damage,
                TargetHealthAfter = target.Health,
                ShieldAbsorbed = shieldAbsorbed,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C05ActiveSkillIntegration.HoldBridgeAbilityId
            });
            ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
            if (target.Health <= 0)
            {
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat,
                    ActorId = source.Id,
                    TargetId = target.Id,
                    Amount = 0,
                    TargetHealthAfter = 0,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C05ActiveSkillIntegration.HoldBridgeAbilityId
                });
            }
        }

        private static void BreakC05Barrier(BattleUnitState owner,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events,
            int actionTime, IList<ActionScheduleEntry> schedule)
        {
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = owner.Id,
                TargetId = owner.Id,
                Amount = 0,
                TargetHealthAfter = owner.Health,
                TargetShieldAfter = owner.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C05ActiveSkillIntegration.BarrierBrokenStatusId
            });
            foreach (var ally in allies ?? new List<BattleUnitState>())
            {
                if (ally == null || ally.Health <= 0 ||
                    C05ColumnForSlot(ally.Slot) != owner.SharedBarrierColumn) continue;
                ally.C05ColumnActionBuffActionsRemaining = C05ActiveSkillIntegration.ColumnActionCount;
                ally.C05ColumnActionBuffAttackBonusBasisPoints =
                    C05ActiveSkillIntegration.ColumnActionAttackBonusBasisPoints;
                ally.C05ColumnActionBuffHealBonusBasisPoints =
                    C05ActiveSkillIntegration.ColumnActionHealBonusBasisPoints;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = owner.Id,
                    TargetId = ally.Id,
                    Amount = C05ActiveSkillIntegration.ColumnActionCount,
                    TargetHealthAfter = ally.Health,
                    TargetShieldAfter = ally.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C05ActiveSkillIntegration.ColumnActionBuffStatusId
                });
            }
            if (owner.Quality == CharacterQuality.Legendary)
            {
                var shield = Math.Max(1, (int)((long)owner.MaxHealth *
                    C05PersonalShieldBasisPoints / 10000));
                owner.Shield += shield;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Shield,
                    ActorId = owner.Id,
                    TargetId = owner.Id,
                    Amount = shield,
                    TargetHealthAfter = owner.Health,
                    TargetShieldAfter = owner.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C05ActiveSkillIntegration.HoldBridgeAbilityId
                });
            }
            owner.SharedBarrierRemainingCapacity = 0;
            owner.SharedBarrierActionsRemaining = 0;
        }

        private static void RemoveC05BarrierOnOwnerDeath(BattleUnitState target,
            IEnumerable<BattleUnitState> targetSide, ICollection<BattleEvent> events, int actionTime)
        {
            if (target == null) return;
            foreach (var owner in targetSide ?? new List<BattleUnitState>())
            {
                if (owner == null || owner.Health > 0 || owner.SharedBarrierRemainingCapacity <= 0) continue;
                RemoveC05Barrier(owner, events, actionTime, "OWNER_DEAD");
            }
        }

        private static int C05ColumnForSlot(int slot)
        {
            return slot < 0 ? -1 : slot % 3;
        }

        private static void RemoveC05Barrier(BattleUnitState owner,
            ICollection<BattleEvent> events, int actionTime, string reason)
        {
            if (owner == null || owner.SharedBarrierRemainingCapacity <= 0) return;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = owner.Id,
                TargetId = owner.Id,
                Amount = -1,
                TargetHealthAfter = owner.Health,
                TargetShieldAfter = owner.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C05ActiveSkillIntegration.BarrierStatusId + ":" + reason
            });
            owner.SharedBarrierColumn = -1;
            owner.SharedBarrierInitialCapacity = 0;
            owner.SharedBarrierRemainingCapacity = 0;
            owner.SharedBarrierActionsRemaining = 0;
            owner.SharedBarrierHalfCounterTriggered = false;
            owner.SharedBarrierBreakTriggered = false;
        }

        private static void ExecuteLeifuWhiteTrail(BattleUnitState actor,
            List<BattleUnitState> allies, List<BattleUnitState> opponents,
            ICollection<BattleEvent> events, int actionTime, bool firstAction,
            int extraActionBonusBasisPoints, IList<ActionScheduleEntry> schedule,
            D1BattleRuntime enhancements)
        {
            RemoveExistingWhiteTrailMarks(opponents, events, actionTime);
            var target = FindHighestMaximumHealth(opponents);
            if (target == null) return;
            var rawDamage = Math.Max(1, (int)((long)actor.AttackDamage *
                LeifuWhiteTrailDamageBasisPoints / 10000));
            rawDamage = ApplyActionBonuses(actor, target, rawDamage, true, firstAction,
                extraActionBonusBasisPoints);
            var damage = ApplyB1IncomingDamageModifiers(target, CalculateDamageAfterArmor(rawDamage,
                Math.Max(0, target.Armor - actor.ArmorPenetration)));
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var shieldAbsorbed = Math.Min(target.Shield, damage);
            target.Shield -= shieldAbsorbed;
            ConsumeN1ShieldContribution(target, shieldAbsorbed);
            target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
            var healthDamage = Math.Max(0, healthBefore - target.Health);
            var rescue = PrepareYiwenLethalProtection(target, opponents);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack,
                ActorId = actor.Id,
                TargetId = target.Id,
                Amount = damage,
                TargetHealthAfter = target.Health,
                ShieldAbsorbed = shieldAbsorbed,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                SourceEffectId = C03ActiveSkillIntegration.WhiteTrailAbilityId
            });
            ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
            ResolveB2AfterDirectDamage(actor, target, healthBefore, opponents, events, actionTime,
                schedule);
            EmitYiwenRescue(rescue, target, events, actionTime);
            AddDirectHitEnergy(target, shieldAbsorbed + healthDamage);
            if (shieldBefore > 0 && target.Shield == 0)
            {
                if (target.IsPlayer) enhancements.ApplyShieldBreakRepair(target, events, actionTime);
                TriggerOlinTide(target, opponents, events, actionTime, schedule);
            }
            enhancements.ApplyHealthThreshold(target, healthBefore, events, actionTime);
            TryReleaseMilaHiddenSpring(target, events, actionTime);
            if (target.Health <= 0)
            {
                RemoveN1HoldGateOnOwnerDeath(target, opponents, events, actionTime);
                DiscardMilaHiddenSpring(target, events, actionTime);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat,
                    ActorId = actor.Id,
                    TargetId = target.Id,
                    TargetHealthAfter = 0,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    SourceEffectId = C03ActiveSkillIntegration.WhiteTrailAbilityId
                });
                return;
            }
            AttachWhiteTrailMark(actor, target, events, actionTime,
                C03ActiveSkillIntegration.WhiteTrailAbilityId);
        }

        private static bool IsWhiteTrailDirectAttack(BattleUnitState actor, BattleUnitState target)
        {
            return actor != null && target != null && target.WhiteTrailActionsRemaining > 0 &&
                   !string.IsNullOrEmpty(target.WhiteTrailSourceId) &&
                   actor.IsPlayer == target.WhiteTrailSourceIsPlayer;
        }

        private static BattleUnitState FindHighestMaximumHealth(IEnumerable<BattleUnitState> units)
        {
            BattleUnitState selected = null;
            foreach (var unit in units ?? new List<BattleUnitState>())
            {
                if (unit == null || unit.Health <= 0) continue;
                if (selected == null || unit.MaxHealth > selected.MaxHealth ||
                    (unit.MaxHealth == selected.MaxHealth && unit.Slot < selected.Slot) ||
                    (unit.MaxHealth == selected.MaxHealth && unit.Slot == selected.Slot &&
                     StringComparer.Ordinal.Compare(unit.Id, selected.Id) < 0))
                    selected = unit;
            }
            return selected;
        }

        private static void AttachWhiteTrailMark(BattleUnitState source, BattleUnitState target,
            ICollection<BattleEvent> events, int actionTime, string sourceEffectId)
        {
            target.WhiteTrailSourceId = source.Id;
            target.WhiteTrailSourceIsPlayer = source.IsPlayer;
            target.WhiteTrailSourceLegendary = source.Quality == CharacterQuality.Legendary;
            target.WhiteTrailActionsRemaining = LeifuWhiteTrailActionCount;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = source.Id,
                TargetId = target.Id,
                Amount = LeifuWhiteTrailActionCount,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = sourceEffectId
            });
        }

        private static void RemoveExistingWhiteTrailMarks(IEnumerable<BattleUnitState> targets,
            ICollection<BattleEvent> events, int actionTime)
        {
            foreach (var target in targets ?? new List<BattleUnitState>())
            {
                if (target == null || target.WhiteTrailActionsRemaining <= 0) continue;
                var sourceId = target.WhiteTrailSourceId;
                ClearWhiteTrailMark(target);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = sourceId,
                    TargetId = target.Id,
                    Amount = -1,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C03ActiveSkillIntegration.WhiteTrailAbilityId
                });
            }
        }

        private static void ClearWhiteTrailMark(BattleUnitState target)
        {
            target.WhiteTrailSourceId = null;
            target.WhiteTrailSourceIsPlayer = false;
            target.WhiteTrailSourceLegendary = false;
            target.WhiteTrailActionsRemaining = 0;
        }

        private static void ResolveWhiteTrailMarkedDefeat(BattleUnitState defeated,
            List<BattleUnitState> sourceAllies, List<BattleUnitState> targetAllies,
            ICollection<BattleEvent> events, int actionTime, IList<ActionScheduleEntry> schedule)
        {
            if (defeated == null || defeated.WhiteTrailActionsRemaining <= 0) return;
            var sourceId = defeated.WhiteTrailSourceId;
            var sourceIsPlayer = defeated.WhiteTrailSourceIsPlayer;
            var sourceLegendary = defeated.WhiteTrailSourceLegendary;
            BattleUnitState source = null;
            foreach (var ally in sourceAllies ?? new List<BattleUnitState>())
            {
                if (ally != null && string.Equals(ally.Id, sourceId, StringComparison.Ordinal))
                {
                    source = ally;
                    break;
                }
            }
            ClearWhiteTrailMark(defeated);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = sourceId,
                TargetId = defeated.Id,
                Amount = -1,
                TargetHealthAfter = defeated.Health,
                TargetShieldAfter = defeated.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C03ActiveSkillIntegration.WhiteTrailAbilityId
            });
            if (!sourceLegendary || source == null || source.WhiteTrailTransferUsed) return;
            var newTarget = FindHighestMaximumHealth(targetAllies);
            if (newTarget == null) return;
            source.WhiteTrailTransferUsed = true;
            newTarget.WhiteTrailSourceId = sourceId;
            newTarget.WhiteTrailSourceIsPlayer = sourceIsPlayer;
            newTarget.WhiteTrailSourceLegendary = true;
            newTarget.WhiteTrailActionsRemaining = LeifuWhiteTrailActionCount;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = defeated.Id,
                TargetId = newTarget.Id,
                Amount = LeifuWhiteTrailActionCount,
                TargetHealthAfter = newTarget.Health,
                TargetShieldAfter = newTarget.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C03ActiveSkillIntegration.TransferAbilityId
            });
            var orderedAllies = new List<BattleUnitState>();
            foreach (var ally in sourceAllies ?? new List<BattleUnitState>())
                if (ally != null && ally.Health > 0) orderedAllies.Add(ally);
            orderedAllies.Sort((left, right) => left.Slot != right.Slot
                ? left.Slot.CompareTo(right.Slot)
                : StringComparer.Ordinal.Compare(left.Id, right.Id));
            foreach (var ally in orderedAllies)
            {
                var oldPeriod = GetActionPeriod(ally);
                ally.LeifuHuntSurgeActionsRemaining = 1;
                ally.LeifuHuntSurgeActionBonusBasisPoints =
                    LeifuHuntSurgeActionBonusBasisPoints;
                ally.LeifuHuntSurgeSpeedBonusBasisPoints =
                    LeifuHuntSurgeAttackSpeedBonusBasisPoints;
                RescheduleNextAction(ally, schedule, actionTime, oldPeriod);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = sourceId,
                    TargetId = ally.Id,
                    Amount = LeifuHuntSurgeActionBonusBasisPoints,
                    TargetHealthAfter = ally.Health,
                    TargetShieldAfter = ally.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C03ActiveSkillIntegration.TransferAbilityId
                });
            }
        }

        private static void ExecuteYiwenReturnBeacon(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            var target = FindLowestHealth(allies);
            if (target == null) return;
            target.YiwenReturnBeaconSourceId = actor.Id;
            target.YiwenReturnBeaconActionsRemaining = YiwenReturnBeaconActionCount;
            target.YiwenReturnBeaconHealAmount = Math.Max(0, actor.HealAmount);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = actor.Id,
                TargetId = target.Id,
                Amount = YiwenReturnBeaconActionCount,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C02ActiveSkillIntegration.ReturnBeaconAbilityId
            });
        }

        private static void ExecuteMilaHiddenSpring(BattleUnitState source,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime)
        {
            var targets = FindLowestHealthTargets(allies, C04ActiveSkillIntegration.HiddenSpringTargetCount);
            foreach (var target in targets)
            {
                if (HasMilaSpring(target)) ReleaseMilaHiddenSpring(target, events, actionTime);
                target.MilaHiddenSpringSourceId = source.Id;
                target.MilaHiddenSpringActionsRemaining = C04ActiveSkillIntegration.HiddenSpringActionCount;
                target.MilaHiddenSpringStoredHeal = 0;
                target.MilaHiddenSpringSourceHealAmount =
                    Math.Max(0, (int)((long)source.HealAmount * 80 / 100));
                target.MilaHiddenSpringActionBonusBasisPoints =
                    C04ActiveSkillIntegration.HiddenSpringAttackBonusBasisPoints;
                target.MilaHiddenSpringSpeedBonusBasisPoints =
                    C04ActiveSkillIntegration.HiddenSpringAttackSpeedBonusBasisPoints;
                target.MilaHiddenSpringSourceLegendary = source.Quality == CharacterQuality.Legendary;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = source.Id,
                    TargetId = target.Id,
                    Amount = C04ActiveSkillIntegration.HiddenSpringActionCount,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C04ActiveSkillIntegration.HiddenSpringAbilityId
                });
            }
        }

        private static bool TryStoreMilaHeal(BattleUnitState target, int requested,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (!HasMilaSpring(target) || requested <= 0) return false;
            var cap = Math.Max(0, (long)target.MaxHealth *
                DeterministicBattle.MilaHiddenSpringStoredHealCapBasisPoints / 10000);
            var available = Math.Max(0L, cap - target.MilaHiddenSpringStoredHeal);
            var stored = (int)Math.Min((long)requested, available);
            if (stored > 0) target.MilaHiddenSpringStoredHeal += stored;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = target.MilaHiddenSpringSourceId,
                TargetId = target.Id,
                Amount = stored,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C04ActiveSkillIntegration.HiddenSpringStoredStatusId
            });
            return true;
        }

        private static void ReleaseMilaHiddenSpring(BattleUnitState target,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (target == null || string.IsNullOrEmpty(target.MilaHiddenSpringSourceId)) return;
            var stored = Math.Max(0, target.MilaHiddenSpringStoredHeal);
            var release = Math.Max(0, stored + target.MilaHiddenSpringSourceHealAmount);
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var heal = Math.Min(Math.Max(0, target.MaxHealth - target.Health), release);
            target.Health += heal;
            var overflow = Math.Max(0, release - heal);
            if (overflow > 0) target.Shield += overflow;
            var bonus = target.MilaHiddenSpringSourceLegendary
                ? Math.Min(C04ActiveSkillIntegration.AfterSpringMaximumBasisPoints,
                    (target.MaxHealth <= 0 ? 0 : (stored * 100 / target.MaxHealth) / 10) *
                    C04ActiveSkillIntegration.AfterSpringBasisPointsPerTenPercent)
                : 0;
            target.MilaAfterSpringActionBonusBasisPoints = bonus;
            target.MilaAfterSpringActionsRemaining = bonus > 0
                ? C04ActiveSkillIntegration.AfterSpringActionCount : 0;
            var sourceId = target.MilaHiddenSpringSourceId;
            target.MilaAfterSpringSourceId = sourceId;
            ClearMilaHiddenSpring(target);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = sourceId,
                TargetId = target.Id,
                Amount = bonus,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C04ActiveSkillIntegration.AfterSpringAbilityId
            });
            if (heal > 0)
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Heal,
                    ActorId = sourceId,
                    TargetId = target.Id,
                    Amount = heal,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C04ActiveSkillIntegration.AfterSpringAbilityId
                });
            if (overflow > 0)
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Shield,
                    ActorId = sourceId,
                    TargetId = target.Id,
                    Amount = overflow,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C04ActiveSkillIntegration.AfterSpringAbilityId
                });
            if (healthBefore == target.Health && shieldBefore == target.Shield && bonus <= 0)
                return;
        }

        private static void DiscardMilaHiddenSpring(BattleUnitState target,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (!HasMilaSpring(target)) return;
            var sourceId = target.MilaHiddenSpringSourceId;
            ClearMilaHiddenSpring(target);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = sourceId,
                TargetId = target.Id,
                Amount = -1,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C04ActiveSkillIntegration.HiddenSpringAbilityId
            });
        }

        private static void ClearMilaHiddenSpring(BattleUnitState target)
        {
            target.MilaHiddenSpringSourceId = null;
            target.MilaHiddenSpringActionsRemaining = 0;
            target.MilaHiddenSpringStoredHeal = 0;
            target.MilaHiddenSpringSourceHealAmount = 0;
            target.MilaHiddenSpringActionBonusBasisPoints = 0;
            target.MilaHiddenSpringSpeedBonusBasisPoints = 0;
            target.MilaHiddenSpringSourceLegendary = false;
        }

        private static void AdvanceMilaHiddenSpring(BattleUnitState actor,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (!HasMilaSpring(actor)) return;
            actor.MilaHiddenSpringActionsRemaining--;
            if (actor.MilaHiddenSpringActionsRemaining <= 0)
            {
                ReleaseMilaHiddenSpring(actor, events, actionTime);
                return;
            }
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = actor.MilaHiddenSpringSourceId,
                TargetId = actor.Id,
                Amount = actor.MilaHiddenSpringActionsRemaining,
                TargetHealthAfter = actor.Health,
                TargetShieldAfter = actor.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C04ActiveSkillIntegration.HiddenSpringAbilityId
            });
        }

        private static void TryReleaseMilaHiddenSpring(BattleUnitState target,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (target == null || !HasMilaSpring(target) || target.Health <= 0) return;
            if ((long)target.Health * 100 <= (long)target.MaxHealth * 20)
                ReleaseMilaHiddenSpring(target, events, actionTime);
        }

        private static YiwenRescueResolution PrepareYiwenLethalProtection(
            BattleUnitState target, IEnumerable<BattleUnitState> allies)
        {
            if (target == null || target.Health > 0) return null;
            if (target.YiwenReturnBeaconActionsRemaining > 0 &&
                !string.IsNullOrEmpty(target.YiwenReturnBeaconSourceId))
            {
                var activeRescue = new YiwenRescueResolution
                {
                    SourceId = target.YiwenReturnBeaconSourceId,
                    AbilityId = C02ActiveSkillIntegration.ReturnBeaconAbilityId,
                    HealAmount = target.YiwenReturnBeaconHealAmount,
                    HealBasisPoints = YiwenReturnBeaconLethalHealBasisPoints
                };
                ClearYiwenReturnBeacon(target);
                target.Health = 1;
                return activeRescue;
            }

            var candidates = new List<BattleUnitState>();
            foreach (var ally in allies ?? new List<BattleUnitState>())
            {
                if (ally == null || string.Equals(ally.Id, target.Id, StringComparison.Ordinal) ||
                    !HasYiwenPassive(ally)) continue;
                candidates.Add(ally);
            }
            candidates.Sort((left, right) => StringComparer.Ordinal.Compare(left.Id, right.Id));
            if (candidates.Count == 0) return null;
            var source = candidates[0];
            source.YiwenLegendarySaveUsed = true;
            target.Health = 1;
            return new YiwenRescueResolution
            {
                SourceId = source.Id,
                AbilityId = C02ActiveSkillIntegration.WaitingAbilityId,
                HealAmount = source.HealAmount,
                HealBasisPoints = YiwenWaitingHealBasisPoints
            };
        }

        private static void EmitYiwenRescue(YiwenRescueResolution rescue,
            BattleUnitState target, ICollection<BattleEvent> events, int actionTime)
        {
            if (rescue == null || target == null) return;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = rescue.SourceId,
                TargetId = target.Id,
                Amount = -1,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = rescue.AbilityId
            });
            ApplyYiwenRescueHeal(rescue.SourceId, rescue.AbilityId, rescue.HealAmount,
                rescue.HealBasisPoints, target, events, actionTime);
            TryReleaseMilaHiddenSpring(target, events, actionTime);
        }

        private static void ApplyYiwenRescueHeal(string sourceId, string abilityId, int healAmount,
            int basisPoints, BattleUnitState target, ICollection<BattleEvent> events, int actionTime)
        {
            var before = target.Health;
            var requested = Math.Max(1, (int)((long)Math.Max(0, healAmount) * basisPoints / 10000));
            target.Health = Math.Min(target.MaxHealth, target.Health + requested);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Heal,
                ActorId = sourceId,
                TargetId = target.Id,
                Amount = target.Health - before,
                TargetHealthAfter = target.Health,
                TargetShieldAfter = target.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = abilityId
            });
        }

        private static void ClearYiwenReturnBeacon(BattleUnitState target)
        {
            target.YiwenReturnBeaconSourceId = null;
            target.YiwenReturnBeaconActionsRemaining = 0;
            target.YiwenReturnBeaconHealAmount = 0;
        }

        private static void ExecuteOlinOpenDawn(
            BattleUnitState actor,
            List<BattleUnitState> allies,
            List<BattleUnitState> opponents,
            ICollection<BattleEvent> events,
            int actionTime,
            IList<ActionScheduleEntry> schedule)
        {
            var shieldSnapshot = Math.Max(0, actor.Shield);
            var damageSource = shieldSnapshot > 0
                ? Math.Max(
                    (int)((long)shieldSnapshot * OlinShieldDamageBasisPoints / 10000),
                    (int)((long)actor.MaxHealth * OlinMinimumDamageBasisPoints / 10000))
                : 0;
            actor.Shield = 0;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status, ActorId = actor.Id, TargetId = actor.Id,
                TargetHealthAfter = actor.Health, TargetShieldAfter = actor.Shield,
                ActionTime = actionTime, IsSecondary = true,
                SourceEffectId = C01ActiveSkillIntegration.OpenDawnAbilityId
            });

            var orderedTargets = new List<BattleUnitState>();
            foreach (var target in opponents)
                if (target.Health > 0) orderedTargets.Add(target);
            orderedTargets.Sort((left, right) => left.Slot != right.Slot
                ? left.Slot.CompareTo(right.Slot)
                : StringComparer.Ordinal.Compare(left.Id, right.Id));

            BattleUnitState defeatedWhiteTrailTarget = null;
            if (damageSource > 0)
            {
                foreach (var target in orderedTargets)
                {
                    var targetDamageSource = damageSource;
                    var whiteTrailArmorReduction = 0;
                    if (IsWhiteTrailDirectAttack(actor, target))
                    {
                        targetDamageSource = Math.Max(1, (int)((long)targetDamageSource *
                            (10000 + LeifuWhiteTrailDirectDamageBonusBasisPoints) / 10000));
                        whiteTrailArmorReduction = -LeifuWhiteTrailArmorReadDelta;
                    }
                    var damage = ApplyB1IncomingDamageModifiers(target,
                        CalculateDamageAfterArmor(targetDamageSource,
                            Math.Max(0, target.Armor - whiteTrailArmorReduction)));
                    var healthBefore = target.Health;
                    var shieldBefore = target.Shield;
                    var shieldAbsorbed = Math.Min(target.Shield, damage);
                    target.Shield -= shieldAbsorbed;
                    ConsumeN1ShieldContribution(target, shieldAbsorbed);
                    target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
                    var healthDamage = Math.Max(0, healthBefore - target.Health);
                    var rescue = PrepareYiwenLethalProtection(target, opponents);
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Attack, ActorId = actor.Id, TargetId = target.Id,
                        Amount = damage, TargetHealthAfter = target.Health,
                        ShieldAbsorbed = shieldAbsorbed, TargetShieldAfter = target.Shield,
                        ActionTime = actionTime, IsSecondary = true,
                        SourceEffectId = C01ActiveSkillIntegration.OpenDawnAbilityId
                    });
                    ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
                    EmitYiwenRescue(rescue, target, events, actionTime);
                    if (shieldAbsorbed > 0 && target.Shield == 0)
                        TriggerOlinTide(target, opponents, events, actionTime, schedule);
                    AddDirectHitEnergy(target, shieldAbsorbed + healthDamage);
                    TryReleaseMilaHiddenSpring(target, events, actionTime);
                    if (target.Health == 0)
                    {
                        RemoveN1HoldGateOnOwnerDeath(target, opponents, events, actionTime);
                        DiscardMilaHiddenSpring(target, events, actionTime);
                        if (target.WhiteTrailActionsRemaining > 0)
                            defeatedWhiteTrailTarget = target;
                        events.Add(new BattleEvent
                        {
                            Type = BattleEventType.Defeat, ActorId = actor.Id, TargetId = target.Id,
                            TargetHealthAfter = 0, TargetShieldAfter = target.Shield,
                            ActionTime = actionTime, IsSecondary = true,
                            SourceEffectId = C01ActiveSkillIntegration.OpenDawnAbilityId
                        });
                    }
                }
            }
            if (defeatedWhiteTrailTarget != null)
                ResolveWhiteTrailMarkedDefeat(defeatedWhiteTrailTarget, allies, opponents, events,
                    actionTime, schedule);

            if (shieldSnapshot == 0)
            {
                var newShield = Math.Max(0,
                    (int)((long)actor.MaxHealth * OlinNoShieldGainBasisPoints / 10000));
                actor.Shield += newShield;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Shield, ActorId = actor.Id, TargetId = actor.Id,
                    Amount = newShield, TargetHealthAfter = actor.Health,
                    TargetShieldAfter = actor.Shield, ActionTime = actionTime,
                    IsSecondary = true, SourceEffectId = C01ActiveSkillIntegration.OpenDawnAbilityId
                });
            }
            if (shieldSnapshot > 0) TriggerOlinTide(actor, allies, events, actionTime, schedule);
        }

        private static void TriggerOlinTide(
            BattleUnitState source,
            IEnumerable<BattleUnitState> allies,
            ICollection<BattleEvent> events,
            int actionTime,
            IList<ActionScheduleEntry> schedule)
        {
            if (!HasOlinPassive(source)) return;
            source.OlinTideTriggerCount++;
            var orderedAllies = new List<BattleUnitState>();
            foreach (var ally in allies)
                if (ally.Health > 0) orderedAllies.Add(ally);
            orderedAllies.Sort((left, right) => left.Slot != right.Slot
                ? left.Slot.CompareTo(right.Slot)
                : StringComparer.Ordinal.Compare(left.Id, right.Id));
            foreach (var ally in orderedAllies)
            {
                var oldPeriod = GetActionPeriod(ally);
                ally.PendingActionBonusBasisPoints = Math.Max(
                    ally.PendingActionBonusBasisPoints, OlinTideActionBonusBasisPoints);
                ally.PendingActionSpeedBonusBasisPoints = Math.Max(
                    ally.PendingActionSpeedBonusBasisPoints, OlinTideAttackSpeedBonusBasisPoints);
                ally.OlinTideActionsRemaining = OlinTideBuffActionCount;
                RescheduleNextAction(ally, schedule, actionTime, oldPeriod);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status, ActorId = source.Id, TargetId = ally.Id,
                    Amount = OlinTideActionBonusBasisPoints,
                    TargetHealthAfter = ally.Health, TargetShieldAfter = ally.Shield,
                    ActionTime = actionTime, IsSecondary = true,
                    SourceEffectId = C01ActiveSkillIntegration.TideAbilityId
                });
            }
        }

        private static void ConsumeActionScopedBonuses(BattleUnitState actor,
            IEnumerable<BattleUnitState> allies, ICollection<BattleEvent> events, int actionTime,
            bool consumeReturnBeacon = true, bool consumeAfterSpringAttack = false,
            bool consumeNockReturnFire = false, bool advanceN1 = true)
        {
            if (actor.B2DoubleLightSpeedPenaltyActionsRemaining > 0)
            {
                actor.B2DoubleLightSpeedPenaltyActionsRemaining--;
                if (actor.B2DoubleLightSpeedPenaltyActionsRemaining == 0)
                    actor.AttackSpeedBonusBasisPoints -=
                        B2ActiveSkillIntegration.DoubleLightSpeedPenaltyBasisPoints;
            }
            if (actor.OlinTideActionsRemaining > 0)
            {
                actor.OlinTideActionsRemaining--;
                if (actor.OlinTideActionsRemaining == 0)
                {
                    actor.PendingActionBonusBasisPoints = 0;
                    actor.PendingActionSpeedBonusBasisPoints = 0;
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Status,
                        ActorId = actor.Id,
                        TargetId = actor.Id,
                        Amount = -1,
                        TargetHealthAfter = actor.Health,
                        TargetShieldAfter = actor.Shield,
                        ActionTime = actionTime,
                        IsSecondary = true,
                        SourceEffectId = C01ActiveSkillIntegration.TideAbilityId
                    });
                }
            }
            if (actor.TemporarySpeedActions > 0)
            {
                actor.TemporarySpeedActions--;
                if (actor.TemporarySpeedActions == 0)
                    actor.AttackSpeedBonusBasisPoints -= 1500;
            }
            if (actor.LeifuHuntSurgeActionsRemaining > 0)
            {
                actor.LeifuHuntSurgeActionsRemaining--;
                if (actor.LeifuHuntSurgeActionsRemaining == 0)
                {
                    actor.LeifuHuntSurgeActionBonusBasisPoints = 0;
                    actor.LeifuHuntSurgeSpeedBonusBasisPoints = 0;
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Status,
                        ActorId = actor.Id,
                        TargetId = actor.Id,
                        Amount = -1,
                        TargetHealthAfter = actor.Health,
                        TargetShieldAfter = actor.Shield,
                        ActionTime = actionTime,
                        IsSecondary = true,
                        SourceEffectId = C03ActiveSkillIntegration.TransferAbilityId
                    });
                }
            }
            if (actor.MilaHiddenSpringActionsRemaining > 0)
            {
                AdvanceMilaHiddenSpring(actor, events, actionTime);
                if (actor.MilaHiddenSpringActionsRemaining == 0)
                {
                    actor.MilaHiddenSpringActionBonusBasisPoints = 0;
                    actor.MilaHiddenSpringSpeedBonusBasisPoints = 0;
                }
            }
            if (consumeAfterSpringAttack && actor.MilaAfterSpringActionsRemaining > 0)
            {
                actor.MilaAfterSpringActionsRemaining--;
                if (actor.MilaAfterSpringActionsRemaining == 0)
                {
                    var sourceId = actor.MilaAfterSpringSourceId;
                    actor.MilaAfterSpringActionBonusBasisPoints = 0;
                    actor.MilaAfterSpringSourceId = null;
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Status,
                        ActorId = sourceId,
                        TargetId = actor.Id,
                        Amount = -1,
                        TargetHealthAfter = actor.Health,
                        TargetShieldAfter = actor.Shield,
                        ActionTime = actionTime,
                        IsSecondary = true,
                        SourceEffectId = C04ActiveSkillIntegration.AfterSpringAbilityId
                    });
                }
            }
            if (consumeReturnBeacon) AdvanceYiwenReturnBeacon(actor, events, actionTime);
            AdvanceWhiteTrailMark(actor, events, actionTime);
            AdvanceC05Barrier(actor, events, actionTime);
            if (actor.C05ColumnActionBuffActionsRemaining > 0)
            {
                actor.C05ColumnActionBuffActionsRemaining--;
                if (actor.C05ColumnActionBuffActionsRemaining == 0)
                {
                    actor.C05ColumnActionBuffAttackBonusBasisPoints = 0;
                    actor.C05ColumnActionBuffHealBonusBasisPoints = 0;
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Status,
                        ActorId = actor.Id,
                        TargetId = actor.Id,
                        Amount = -1,
                        TargetHealthAfter = actor.Health,
                        TargetShieldAfter = actor.Shield,
                        ActionTime = actionTime,
                        IsSecondary = true,
                        SourceEffectId = C05ActiveSkillIntegration.ColumnActionBuffStatusId
                    });
                }
            }
            if (consumeNockReturnFire && actor.NockReturnFireActionsRemaining > 0)
            {
                actor.NockReturnFireActionsRemaining--;
                if (actor.NockReturnFireActionsRemaining == 0)
                {
                    actor.NockReturnFireDamageBonusBasisPoints = 0;
                    actor.NockReturnFireSpeedBonusBasisPoints = 0;
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Status,
                        ActorId = actor.NockFireSourceId,
                        TargetId = actor.Id,
                        Amount = -1,
                        TargetHealthAfter = actor.Health,
                        TargetShieldAfter = actor.Shield,
                        ActionTime = actionTime,
                        IsSecondary = true,
                        SourceEffectId = C06ActiveSkillIntegration.ReturnFireBuffStatusId
                    });
                }
            }
            if (advanceN1) AdvanceN1HoldGate(actor, allies, events, actionTime);
            AdvanceE1SteadyLine(actor, allies, events, actionTime);
            AdvanceE3BellReturn(actor, events, actionTime);
            AdvanceB1ActionStatuses(actor, events, actionTime);
        }

        private static void AdvanceB2Marks(IEnumerable<BattleUnitState> targets, string sourceId,
            ICollection<BattleEvent> events, int actionTime)
        {
            foreach (var target in targets ?? new List<BattleUnitState>())
            {
                if (target == null || target.B2DeviantLightActionsRemaining <= 0) continue;
                target.B2DeviantLightActionsRemaining--;
                if (target.B2DeviantLightActionsRemaining > 0) continue;
                target.B2DeviantLightSourceId = null;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = sourceId,
                    TargetId = target.Id,
                    Amount = -1,
                    TargetHealthAfter = target.Health,
                    TargetShieldAfter = target.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = B2ActiveSkillIntegration.DeviantLightAbilityId
                });
            }
        }

        private static void AdvanceB1ActionStatuses(BattleUnitState actor,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (!B1ActiveSkillIntegration.IsB1(actor)) return;
            if (actor.B1BrokenArmorActionsRemaining > 0)
            {
                actor.B1BrokenArmorActionsRemaining--;
                if (actor.B1BrokenArmorActionsRemaining == 0)
                {
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Status, ActorId = actor.Id, TargetId = actor.Id,
                        Amount = -1, TargetHealthAfter = actor.Health,
                        TargetShieldAfter = actor.Shield, ActionTime = actionTime,
                        IsSecondary = true,
                        SourceEffectId = B1ActiveSkillIntegration.ClosedArmorAbilityId
                    });
                }
            }
            if (actor.B1NoReturnDamageBonusActionsRemaining <= 0) return;
            actor.B1NoReturnDamageBonusActionsRemaining--;
            if (actor.B1NoReturnDamageBonusActionsRemaining > 0) return;
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status, ActorId = actor.Id, TargetId = actor.Id,
                Amount = -1, TargetHealthAfter = actor.Health,
                TargetShieldAfter = actor.Shield, ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = B1ActiveSkillIntegration.NoReturnAbilityId
            });
        }

        private static void AdvanceC05Barrier(BattleUnitState actor,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (actor == null || actor.SharedBarrierActionsRemaining <= 0 ||
                actor.SharedBarrierRemainingCapacity <= 0) return;
            actor.SharedBarrierActionsRemaining--;
            if (actor.SharedBarrierActionsRemaining > 0) return;
            RemoveC05Barrier(actor, events, actionTime, "EXPIRED");
        }

        private static void AdvanceWhiteTrailMark(BattleUnitState actor,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (actor.WhiteTrailActionsRemaining <= 0) return;
            var sourceId = actor.WhiteTrailSourceId;
            actor.WhiteTrailActionsRemaining--;
            var remaining = actor.WhiteTrailActionsRemaining;
            if (remaining == 0) ClearWhiteTrailMark(actor);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = sourceId,
                TargetId = actor.Id,
                Amount = remaining > 0 ? remaining : -1,
                TargetHealthAfter = actor.Health,
                TargetShieldAfter = actor.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C03ActiveSkillIntegration.WhiteTrailAbilityId
            });
        }

        private static void AdvanceYiwenReturnBeacon(BattleUnitState actor,
            ICollection<BattleEvent> events, int actionTime)
        {
            if (actor.YiwenReturnBeaconActionsRemaining <= 0) return;
            actor.YiwenReturnBeaconActionsRemaining--;
            if (actor.YiwenReturnBeaconActionsRemaining > 0)
            {
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Status,
                    ActorId = actor.YiwenReturnBeaconSourceId,
                    TargetId = actor.Id,
                    Amount = actor.YiwenReturnBeaconActionsRemaining,
                    TargetHealthAfter = actor.Health,
                    TargetShieldAfter = actor.Shield,
                    ActionTime = actionTime,
                    IsSecondary = true,
                    SourceEffectId = C02ActiveSkillIntegration.ReturnBeaconAbilityId
                });
                return;
            }
            var sourceId = actor.YiwenReturnBeaconSourceId;
            var healAmount = actor.YiwenReturnBeaconHealAmount;
            ClearYiwenReturnBeacon(actor);
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Status,
                ActorId = sourceId,
                TargetId = actor.Id,
                Amount = -1,
                TargetHealthAfter = actor.Health,
                TargetShieldAfter = actor.Shield,
                ActionTime = actionTime,
                IsSecondary = true,
                SourceEffectId = C02ActiveSkillIntegration.ReturnBeaconAbilityId
            });
            ApplyYiwenRescueHeal(sourceId, C02ActiveSkillIntegration.ReturnBeaconAbilityId,
                healAmount, YiwenReturnBeaconExpiryHealBasisPoints, actor, events, actionTime);
        }

        private static void RescheduleNextAction(BattleUnitState unit,
            IEnumerable<ActionScheduleEntry> schedule, int actionTime, int oldPeriod)
        {
            if (schedule == null) return;
            foreach (var entry in schedule)
            {
                if (!ReferenceEquals(entry.Unit, unit)) continue;
                var remaining = Math.Max(0, entry.NextActionAt - actionTime);
                var newPeriod = GetActionPeriod(unit);
                entry.NextActionAt = actionTime + RescheduleRemainingTime(remaining,
                    Math.Max(1, oldPeriod), newPeriod);
                entry.CurrentPeriod = newPeriod;
                return;
            }
        }

        private static int ApplyActionBonuses(
            BattleUnitState actor, BattleUnitState target, int value, bool isDamage, bool isFirstAction,
            int extraBasisPoints)
        {
            var percent = 0;
            if (actor.Health * 2 <= actor.MaxHealth) percent += actor.LowHealthActionPercent;
            if (isFirstAction) percent += actor.FirstActionPercent;
            if (isDamage && target.Health * 2 <= target.MaxHealth) percent += actor.ExecuteDamagePercent;
            var legacyValue = percent <= 0 ? value : Math.Max(1, (value * (100 + percent) + 50) / 100);
            return extraBasisPoints <= 0
                ? legacyValue
                : Math.Max(1, (int)((long)legacyValue * (10000 + extraBasisPoints) / 10000));
        }

        private sealed class D1BattleRuntime
        {
            private readonly int attackBasisPoints;
            private readonly int healthBasisPoints;
            private readonly int steelGuardianArmor;
            private readonly int fireBasisPoints;
            private readonly int leechBasisPoints;
            private readonly int shieldRepairBasisPoints;
            private readonly int focusArmorReduction;
            private readonly int attackSpeedBasisPoints;
            private readonly int emergencyHealBasisPoints;
            private readonly int lowHealthActionBasisPoints;
            private readonly HashSet<string> repairedShieldUnits = new HashSet<string>(StringComparer.Ordinal);
            private bool emergencyHealConsumed;
            private string focusLastTargetId;
            private string focusLastActorId;
            private string focusArmedTargetId;

            public D1BattleRuntime(IEnumerable<RoguelikeAcquiredEffect> effects)
            {
                attackBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B01", 10000);
                healthBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B02", 10000);
                steelGuardianArmor = RoguelikeEnhancementRules.SumValue(effects, "B03", 80);
                fireBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B04", 4000);
                leechBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B06", 3500);
                shieldRepairBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B08", 2500);
                focusArmorReduction = RoguelikeEnhancementRules.SumValue(effects, "B10", 28);
                attackSpeedBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B13", 3000);
                emergencyHealBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B14", 10000);
                lowHealthActionBasisPoints = RoguelikeEnhancementRules.SumValueBasisPoints(effects, "B15", 3500);
            }

            public void ApplyBattleStart(IEnumerable<BattleUnitState> players)
            {
                foreach (var unit in players)
                {
                    if (attackBasisPoints > 0)
                        unit.AttackDamage = RoguelikeEnhancementRules.ApplyBasisPointIncreaseFloor(
                            unit.AttackDamage, attackBasisPoints);
                    if (healthBasisPoints > 0)
                    {
                        unit.MaxHealth = RoguelikeEnhancementRules.ApplyBasisPointIncreaseFloor(
                            unit.MaxHealth, healthBasisPoints);
                        unit.Health = unit.MaxHealth;
                    }
                    if (steelGuardianArmor > 0 &&
                        (unit.FactionId == "faction.steel" || unit.ProfessionId == "profession.guardian"))
                        unit.Armor = Math.Min(80, unit.Armor + steelGuardianArmor);
                    if (attackSpeedBasisPoints > 0)
                        unit.AttackSpeedBonusBasisPoints += attackSpeedBasisPoints;
                }
            }

            public int LowHealthActionBasisPoints(BattleUnitState actor)
            {
                return actor.IsPlayer && actor.Health * 2 <= actor.MaxHealth ? lowHealthActionBasisPoints : 0;
            }

            public int FocusArmorReductionFor(BattleUnitState actor, BattleUnitState target)
            {
                if (!actor.IsPlayer || focusArmorReduction <= 0 || focusArmedTargetId != target.Id) return 0;
                focusArmedTargetId = null;
                return focusArmorReduction;
            }

            public void RecordDirectAttack(BattleUnitState actor, BattleUnitState target)
            {
                if (!actor.IsPlayer || focusArmorReduction <= 0) return;
                if (focusLastTargetId == target.Id && focusLastActorId != actor.Id)
                    focusArmedTargetId = target.Id;
                else if (focusLastTargetId != target.Id)
                    focusArmedTargetId = null;
                focusLastTargetId = target.Id;
                focusLastActorId = actor.Id;
            }

            public void ApplyFire(BattleUnitState actor, BattleUnitState target, int directValue,
                ICollection<BattleEvent> events, int actionTime,
                IEnumerable<BattleUnitState> targetAllies)
            {
                if (fireBasisPoints <= 0 || target.Health <= 0) return;
                var raw = Math.Max(1, (int)((long)directValue * fireBasisPoints / 10000));
                var healthBefore = target.Health;
                var shieldBefore = target.Shield;
                var damage = ApplyB1IncomingDamageModifiers(target,
                    CalculateDamageAfterArmor(raw, target.Armor));
                var shieldAbsorbed = Math.Min(target.Shield, damage);
                target.Shield -= shieldAbsorbed;
                ConsumeN1ShieldContribution(target, shieldAbsorbed);
                target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
                var rescue = PrepareYiwenLethalProtection(target, targetAllies);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Attack, ActorId = actor.Id, TargetId = target.Id,
                    Amount = damage, TargetHealthAfter = target.Health, ShieldAbsorbed = shieldAbsorbed,
                    TargetShieldAfter = target.Shield, ActionTime = actionTime,
                    IsSecondary = true, SourceEffectId = "B04"
                });
                ResolveB1AfterDirectDamage(target, shieldBefore, healthBefore, events, actionTime);
                EmitYiwenRescue(rescue, target, events, actionTime);
            }

            public void ApplyLeech(BattleUnitState actor, int effectiveDirectDamage,
                ICollection<BattleEvent> events, int actionTime)
            {
                if (leechBasisPoints <= 0 || actor.Health <= 0 || actor.Health >= actor.MaxHealth) return;
                var amount = Math.Min(actor.MaxHealth - actor.Health,
                    (int)((long)effectiveDirectDamage * leechBasisPoints / 10000));
                if (amount <= 0) return;
                var stored = TryStoreMilaHeal(actor, amount, events, actionTime);
                if (!stored) actor.Health += amount;
                if (!stored)
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Heal, ActorId = actor.Id, TargetId = actor.Id,
                        Amount = amount, TargetHealthAfter = actor.Health, TargetShieldAfter = actor.Shield,
                        ActionTime = actionTime, IsSecondary = true, SourceEffectId = "B06"
                    });
            }

            public void ApplyShieldBreakRepair(BattleUnitState target, ICollection<BattleEvent> events, int actionTime)
            {
                if (shieldRepairBasisPoints <= 0 || target.Health <= 0 || !repairedShieldUnits.Add(target.Id)) return;
                ApplyPercentHeal(target, shieldRepairBasisPoints, "B08", events, actionTime);
            }

            public void ApplyHealthThreshold(BattleUnitState target, int healthBefore,
                ICollection<BattleEvent> events, int actionTime)
            {
                if (!target.IsPlayer || emergencyHealConsumed || emergencyHealBasisPoints <= 0 || target.Health <= 0) return;
                if ((long)healthBefore * 100 <= (long)target.MaxHealth * 30 ||
                    (long)target.Health * 100 > (long)target.MaxHealth * 30) return;
                emergencyHealConsumed = true;
                ApplyPercentHeal(target, emergencyHealBasisPoints, "B14", events, actionTime);
            }

            private static void ApplyPercentHeal(BattleUnitState target, int basisPoints, string effectId,
                ICollection<BattleEvent> events, int actionTime)
            {
                var amount = Math.Min(target.MaxHealth - target.Health,
                    Math.Max(1, (int)((long)target.MaxHealth * basisPoints / 10000)));
                if (amount <= 0) return;
                var stored = TryStoreMilaHeal(target, amount, events, actionTime);
                if (!stored) target.Health += amount;
                if (!stored)
                    events.Add(new BattleEvent
                    {
                        Type = BattleEventType.Heal, ActorId = target.Id, TargetId = target.Id,
                        Amount = amount, TargetHealthAfter = target.Health, TargetShieldAfter = target.Shield,
                        ActionTime = actionTime, IsSecondary = true, SourceEffectId = effectId
                    });
            }
        }

        private static void ResolveActionLimit(
            List<BattleUnitState> players,
            List<BattleUnitState> enemies,
            ICollection<BattleEvent> events,
            uint seed,
            int actionTime)
        {
            var comparison = ((long)TotalHealth(players) * TotalMaximumHealth(enemies)).CompareTo(
                (long)TotalHealth(enemies) * TotalMaximumHealth(players));
            if (comparison == 0) comparison = AliveCount(players).CompareTo(AliveCount(enemies));
            if (comparison == 0) comparison = (seed & 1u) == 0u ? 1 : -1;
            DefeatRemaining(comparison > 0 ? enemies : players, events, actionTime);
        }

        private static void DefeatRemaining(
            IEnumerable<BattleUnitState> units, ICollection<BattleEvent> events, int actionTime)
        {
            foreach (var unit in units)
            {
                if (unit.Health <= 0) continue;
                unit.Health = 0;
                unit.Shield = 0;
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat, ActorId = "system", TargetId = unit.Id,
                    Amount = 0, TargetHealthAfter = 0, TargetShieldAfter = 0,
                    ActionTime = actionTime
                });
            }
        }

        private static List<BattleUnitState> Clone(IEnumerable<BattleUnitState> source)
        {
            var result = new List<BattleUnitState>();
            if (source == null) return result;
            foreach (var unit in source) if (unit != null) result.Add(unit.Copy());
            return result;
        }

        private static bool AnyAlive(IEnumerable<BattleUnitState> units)
        {
            foreach (var unit in units) if (unit.Health > 0) return true;
            return false;
        }

        private static int AliveCount(IEnumerable<BattleUnitState> units)
        {
            var count = 0;
            foreach (var unit in units) if (unit.Health > 0) count++;
            return count;
        }

        private static int TotalHealth(IEnumerable<BattleUnitState> units)
        {
            var total = 0;
            foreach (var unit in units) total += Math.Max(0, unit.Health);
            return total;
        }

        private static int TotalMaximumHealth(IEnumerable<BattleUnitState> units)
        {
            var total = 0;
            foreach (var unit in units) total += Math.Max(1, unit.MaxHealth);
            return Math.Max(1, total);
        }

        private static BattleUnitState FindLowestHealth(IEnumerable<BattleUnitState> units)
        {
            BattleUnitState lowest = null;
            foreach (var unit in units)
            {
                if (unit.Health <= 0) continue;
                if (lowest == null || (long)unit.Health * lowest.MaxHealth <
                    (long)lowest.Health * unit.MaxHealth)
                    lowest = unit;
            }
            return lowest;
        }

        private static BattleUnitState FindHighestAttack(IEnumerable<BattleUnitState> units)
        {
            BattleUnitState selected = null;
            foreach (var unit in units ?? new List<BattleUnitState>())
            {
                if (unit == null || unit.Health <= 0) continue;
                if (selected == null || unit.AttackDamage > selected.AttackDamage ||
                    (unit.AttackDamage == selected.AttackDamage && unit.Slot < selected.Slot) ||
                    (unit.AttackDamage == selected.AttackDamage && unit.Slot == selected.Slot &&
                     StringComparer.Ordinal.Compare(unit.Id, selected.Id) < 0))
                    selected = unit;
            }
            return selected;
        }

        private static List<BattleUnitState> FindLowestHealthTargets(
            IEnumerable<BattleUnitState> units, int maximumCount)
        {
            var candidates = new List<BattleUnitState>();
            foreach (var unit in units ?? new List<BattleUnitState>())
                if (unit != null && unit.Health > 0) candidates.Add(unit);
            candidates.Sort((left, right) =>
            {
                var ratio = ((long)left.Health * right.MaxHealth).CompareTo(
                    (long)right.Health * left.MaxHealth);
                if (ratio != 0) return ratio;
                if (left.Slot != right.Slot) return left.Slot.CompareTo(right.Slot);
                return StringComparer.Ordinal.Compare(left.Id, right.Id);
            });
            if (candidates.Count > maximumCount)
                candidates.RemoveRange(maximumCount, candidates.Count - maximumCount);
            return candidates;
        }

        private static BattleUnitState FindNearest(BattleUnitState attacker, IEnumerable<BattleUnitState> targets)
        {
            BattleUnitState nearest = null;
            var bestDistance = Int32.MaxValue;
            foreach (var target in targets)
            {
                if (target.Health <= 0) continue;
                var distance = Math.Abs(attacker.Slot % 3 - target.Slot % 3) +
                               Math.Abs(attacker.Slot / 3 - target.Slot / 3);
                if (distance < bestDistance)
                {
                    nearest = target;
                    bestDistance = distance;
                }
            }
            return nearest;
        }

        private sealed class ActionScheduleEntry
        {
            public BattleUnitState Unit;
            public int NextActionAt;
            public int CurrentPeriod;
            public uint InitiativeRank;
        }

        private sealed class YiwenRescueResolution
        {
            public string SourceId;
            public string AbilityId;
            public int HealAmount;
            public int HealBasisPoints;
        }
    }
}
