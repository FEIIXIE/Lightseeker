using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum BattleEventType { Attack, Heal, Defeat }

    [Serializable]
    public sealed class BattleUnitState
    {
        public string Id;
        public bool IsPlayer;
        public int Slot;
        public int Health;
        public int MaxHealth;
        public int AttackDamage;
        public int HealAmount;
        public int Armor;
        public int Shield;
        public CombatSpeedTier SpeedTier = CombatSpeedTier.Standard;
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

        public BattleUnitState Copy()
        {
            return new BattleUnitState
            {
                Id = Id, IsPlayer = IsPlayer, Slot = Slot, Health = Health,
                MaxHealth = MaxHealth, AttackDamage = AttackDamage, HealAmount = HealAmount,
                Armor = Math.Max(0, Math.Min(80, Armor)),
                Shield = Math.Max(0, Shield),
                SpeedTier = IsValidSpeedTier(SpeedTier) ? SpeedTier : CombatSpeedTier.Standard,
                IsHealer = IsHealer, LowHealthActionPercent = LowHealthActionPercent,
                FirstActionPercent = FirstActionPercent, ExecuteDamagePercent = ExecuteDamagePercent,
                ArmorPenetration = Math.Max(0, ArmorPenetration),
                DamageTakenPercent = Math.Max(0, DamageTakenPercent),
                DamageTakenBoostHitsRemaining = Math.Max(0, DamageTakenBoostHitsRemaining),
                TemporarySpeedActions = Math.Max(0, TemporarySpeedActions),
                FactionId = FactionId,
                ProfessionId = ProfessionId
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

        public static BattleResult Run(BattleInput input)
        {
            if (input == null) throw new ArgumentNullException("input");
            var players = Clone(input.PlayerUnits);
            var enemies = Clone(input.EnemyUnits);
            var result = new BattleResult { PlayerUnits = players, EnemyUnits = enemies };
            var acted = new HashSet<string>(StringComparer.Ordinal);
            var enhancements = new D1BattleRuntime(input.PlayerRoguelikeEffects);
            enhancements.ApplyBattleStart(players);
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
                    enhancements, enemyFrenzyActive);
                entry.NextActionAt += GetActionPeriod(entry.Unit.SpeedTier, enemyFrenzyActive);
            }

            if (AnyAlive(players) && AnyAlive(enemies))
                ResolveActionLimit(players, enemies, result.Events, input.Seed, actionTime);

            result.PlayerWon = AnyAlive(players) && !AnyAlive(enemies);
            return result;
        }

        public static int GetActionPeriod(CombatSpeedTier speedTier)
        {
            if (speedTier == CombatSpeedTier.Slow) return 1250;
            if (speedTier == CombatSpeedTier.Fast) return 833;
            return 1000;
        }

        public static int GetActionPeriod(CombatSpeedTier speedTier, bool frenzyActive)
        {
            var period = GetActionPeriod(speedTier);
            return frenzyActive ? Math.Max(1, period / FrenzyAttackMultiplier) : period;
        }

        public static int CalculateDamageAfterArmor(int damage, int armor)
        {
            var normalizedDamage = Math.Max(1, damage);
            var normalizedArmor = Math.Max(0, Math.Min(80, armor));
            return Math.Max(1, normalizedDamage * 100 / (100 + normalizedArmor));
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
                schedule.Add(new ActionScheduleEntry
                {
                    Unit = unit,
                    NextActionAt = GetActionPeriod(unit.SpeedTier),
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
            bool enemyFrenzyActive)
        {
            var firstAction = !acted.Contains(actor.Id);
            var allyToHeal = FindLowestHealth(allies);
            if (actor.IsHealer && actor.HealAmount > 0 && allyToHeal != null &&
                allyToHeal.Health * 100 < allyToHeal.MaxHealth * 80)
            {
                var before = allyToHeal.Health;
                var heal = ApplyActionBonuses(actor, allyToHeal, actor.HealAmount, false, firstAction,
                    enhancements.LowHealthActionBasisPoints(actor));
                allyToHeal.Health = Math.Min(allyToHeal.MaxHealth, allyToHeal.Health + heal);
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Heal, ActorId = actor.Id, TargetId = allyToHeal.Id,
                    Amount = allyToHeal.Health - before, TargetHealthAfter = allyToHeal.Health,
                    TargetShieldAfter = allyToHeal.Shield,
                    ActionTime = actionTime
                });
                acted.Add(actor.Id);
                ConsumeTemporarySpeed(actor);
                return;
            }

            var target = FindNearest(actor, opponents);
            if (target == null) return;
            var attackDamage = enemyFrenzyActive
                ? Math.Min(int.MaxValue, (long)actor.AttackDamage * FrenzyAttackMultiplier)
                : actor.AttackDamage;
            var directValue = ApplyActionBonuses(actor, target, (int)attackDamage, true, firstAction,
                enhancements.LowHealthActionBasisPoints(actor));
            var focusReduction = enhancements.FocusArmorReductionFor(actor, target);
            var damage = CalculateDamageAfterArmor(directValue,
                Math.Max(0, target.Armor - actor.ArmorPenetration - focusReduction));
            if (target.DamageTakenBoostHitsRemaining > 0 && target.DamageTakenPercent > 0)
            {
                damage = Math.Max(1, (damage * (100 + target.DamageTakenPercent) + 50) / 100);
                target.DamageTakenBoostHitsRemaining--;
            }
            var healthBefore = target.Health;
            var shieldBefore = target.Shield;
            var shieldAbsorbed = Math.Min(target.Shield, damage);
            target.Shield -= shieldAbsorbed;
            target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
            events.Add(new BattleEvent
            {
                Type = BattleEventType.Attack, ActorId = actor.Id, TargetId = target.Id,
                Amount = damage, TargetHealthAfter = target.Health,
                ShieldAbsorbed = shieldAbsorbed, TargetShieldAfter = target.Shield,
                ActionTime = actionTime
            });
            enhancements.RecordDirectAttack(actor, target);
            acted.Add(actor.Id);
            ConsumeTemporarySpeed(actor);
            var effectiveDirectDamage = shieldAbsorbed + Math.Min(healthBefore, Math.Max(0, damage - shieldAbsorbed));
            if (actor.IsPlayer)
            {
                enhancements.ApplyFire(actor, target, directValue, events, actionTime);
                enhancements.ApplyLeech(actor, effectiveDirectDamage, events, actionTime);
            }
            if (target.IsPlayer && shieldBefore > 0 && target.Shield == 0)
                enhancements.ApplyShieldBreakRepair(target, events, actionTime);
            enhancements.ApplyHealthThreshold(target, healthBefore, events, actionTime);
            if (target.Health == 0)
            {
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Defeat, ActorId = actor.Id, TargetId = target.Id,
                    Amount = 0, TargetHealthAfter = 0, TargetShieldAfter = target.Shield,
                    ActionTime = actionTime
                });
            }
        }

        private static void ConsumeTemporarySpeed(BattleUnitState actor)
        {
            if (actor.TemporarySpeedActions <= 0) return;
            actor.TemporarySpeedActions--;
            if (actor.TemporarySpeedActions == 0 && actor.SpeedTier > CombatSpeedTier.Slow)
                actor.SpeedTier = (CombatSpeedTier)((int)actor.SpeedTier - 1);
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
                ICollection<BattleEvent> events, int actionTime)
            {
                if (fireBasisPoints <= 0 || target.Health <= 0) return;
                var raw = Math.Max(1, (int)((long)directValue * fireBasisPoints / 10000));
                var damage = CalculateDamageAfterArmor(raw, target.Armor);
                var shieldAbsorbed = Math.Min(target.Shield, damage);
                target.Shield -= shieldAbsorbed;
                target.Health = Math.Max(0, target.Health - (damage - shieldAbsorbed));
                events.Add(new BattleEvent
                {
                    Type = BattleEventType.Attack, ActorId = actor.Id, TargetId = target.Id,
                    Amount = damage, TargetHealthAfter = target.Health, ShieldAbsorbed = shieldAbsorbed,
                    TargetShieldAfter = target.Shield, ActionTime = actionTime,
                    IsSecondary = true, SourceEffectId = "B04"
                });
            }

            public void ApplyLeech(BattleUnitState actor, int effectiveDirectDamage,
                ICollection<BattleEvent> events, int actionTime)
            {
                if (leechBasisPoints <= 0 || actor.Health <= 0 || actor.Health >= actor.MaxHealth) return;
                var amount = Math.Min(actor.MaxHealth - actor.Health,
                    (int)((long)effectiveDirectDamage * leechBasisPoints / 10000));
                if (amount <= 0) return;
                actor.Health += amount;
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
                target.Health += amount;
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
                if (lowest == null || unit.Health * lowest.MaxHealth < lowest.Health * unit.MaxHealth)
                    lowest = unit;
            }
            return lowest;
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
            public uint InitiativeRank;
        }
    }
}
