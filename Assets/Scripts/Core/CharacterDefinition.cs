using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum CombatSpeedTier
    {
        Slow = 0,
        Standard = 1,
        Fast = 2
    }

    public static class ContinuousAttackSpeedRules
    {
        public const int MinimumBasisPoints = 6000;
        public const int MaximumBasisPoints = 20000;

        public static int FromTier(CombatSpeedTier speedTier)
        {
            if (speedTier == CombatSpeedTier.Slow) return 8000;
            if (speedTier == CombatSpeedTier.Fast) return 12000;
            return 10000;
        }

        public static int CalculateFinal(int baseAttackSpeedBasisPoints, int bonusBasisPoints)
        {
            var normalizedBase = baseAttackSpeedBasisPoints > 0 ? baseAttackSpeedBasisPoints : 10000;
            var unclamped = (int)((long)normalizedBase * (10000 + bonusBasisPoints) / 10000);
            return Math.Max(MinimumBasisPoints, Math.Min(MaximumBasisPoints, unclamped));
        }

        public static int CalculatePeriod(int finalAttackSpeedBasisPoints)
        {
            var speed = Math.Max(MinimumBasisPoints,
                Math.Min(MaximumBasisPoints, finalAttackSpeedBasisPoints));
            return (10000000 + speed / 2) / speed;
        }

        public static int RescheduleRemaining(int remaining, int oldPeriod, int newPeriod)
        {
            if (remaining <= 0) return 0;
            if (oldPeriod <= 0) return Math.Max(1, newPeriod);
            return Math.Max(1, (int)(((long)remaining * newPeriod + oldPeriod - 1) / oldPeriod));
        }
    }

    public sealed class CombatStatBlock
    {
        public int MaxHealth { get; }
        public int AttackDamage { get; }
        public int HealAmount { get; }
        public int Armor { get; }
        public CombatSpeedTier SpeedTier { get; }
        public int AttackSpeedBasisPoints { get; }

        public CombatStatBlock(
            int maxHealth,
            int attackDamage,
            int healAmount,
            int armor = 0,
            CombatSpeedTier speedTier = CombatSpeedTier.Standard,
            int attackSpeedBasisPoints = 0)
        {
            if (maxHealth <= 0) throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (attackDamage <= 0) throw new ArgumentOutOfRangeException(nameof(attackDamage));
            if (healAmount < 0) throw new ArgumentOutOfRangeException(nameof(healAmount));
            if (armor < 0 || armor > 80) throw new ArgumentOutOfRangeException(nameof(armor));
            if (!Enum.IsDefined(typeof(CombatSpeedTier), speedTier))
                throw new ArgumentOutOfRangeException(nameof(speedTier));
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            HealAmount = healAmount;
            Armor = armor;
            SpeedTier = speedTier;
            AttackSpeedBasisPoints = attackSpeedBasisPoints > 0
                ? ContinuousAttackSpeedRules.CalculateFinal(attackSpeedBasisPoints, 0)
                : ContinuousAttackSpeedRules.FromTier(speedTier);
        }
    }

    /// <summary>
    /// Integer combat scaling keeps the client and a future authoritative server on
    /// the same E08 basis-point curves and rounding path.
    /// </summary>
    public sealed class CombatStatFormula
    {
        public int HealthPerPower { get; }
        public int HealthFlat { get; }
        public int AttackPerPower { get; }
        public int AttackFlat { get; }
        public int MinimumAttack { get; }
        public int HealPerPower { get; }
        public int HealFlat { get; }
        public int Armor { get; }
        public CombatSpeedTier SpeedTier { get; }
        public int AttackSpeedBasisPoints { get; }

        public CombatStatFormula(
            int healthPerPower,
            int healthFlat,
            int attackPerPower,
            int attackFlat,
            int minimumAttack,
            int healPerPower = 0,
            int healFlat = 0,
            int armor = 0,
            CombatSpeedTier speedTier = CombatSpeedTier.Standard,
            int attackSpeedBasisPoints = 0)
        {
            if (healthPerPower < 0 || healthFlat < 0 || healthPerPower + healthFlat <= 0)
                throw new ArgumentOutOfRangeException(nameof(healthPerPower));
            if (attackPerPower < 0 || minimumAttack <= 0)
                throw new ArgumentOutOfRangeException(nameof(attackPerPower));
            if (healPerPower < 0 || healFlat < 0)
                throw new ArgumentOutOfRangeException(nameof(healPerPower));
            if (armor < 0 || armor > 80)
                throw new ArgumentOutOfRangeException(nameof(armor));
            if (!Enum.IsDefined(typeof(CombatSpeedTier), speedTier))
                throw new ArgumentOutOfRangeException(nameof(speedTier));
            HealthPerPower = healthPerPower;
            HealthFlat = healthFlat;
            AttackPerPower = attackPerPower;
            AttackFlat = attackFlat;
            MinimumAttack = minimumAttack;
            HealPerPower = healPerPower;
            HealFlat = healFlat;
            Armor = armor;
            SpeedTier = speedTier;
            AttackSpeedBasisPoints = attackSpeedBasisPoints > 0
                ? ContinuousAttackSpeedRules.CalculateFinal(attackSpeedBasisPoints, 0)
                : ContinuousAttackSpeedRules.FromTier(speedTier);
        }

        public CombatStatBlock Calculate(int power, int runLevel)
        {
            if (power <= 0) throw new ArgumentOutOfRangeException(nameof(power));
            var health = RunUnitRules.ScaleHealth(power * HealthPerPower + HealthFlat, runLevel);
            var attack = RunUnitRules.ScaleAction(
                Math.Max(MinimumAttack, power * AttackPerPower + AttackFlat), runLevel);
            var heal = RunUnitRules.ScaleAction(power * HealPerPower + HealFlat, runLevel);
            return new CombatStatBlock(health, attack, heal, Armor, SpeedTier,
                AttackSpeedBasisPoints);
        }
    }

    public sealed class CharacterDefinition
    {
        public string DefinitionId { get; }
        public bool IsCollectible { get; }
        public int ShopCost { get; }
        public int BasePower { get; }
        public string FactionId { get; }
        public string ProfessionId { get; }
        public string DetailRoleId { get; }
        public string TraitId { get; }
        public CombatStatFormula Stats { get; }

        public CharacterDefinition(
            string definitionId,
            bool isCollectible,
            int shopCost,
            int basePower,
            string factionId,
            string professionId,
            string traitId,
            CombatStatFormula stats,
            string detailRoleId = null)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("A definition id is required.", nameof(definitionId));
            if (shopCost < 0 || (isCollectible && shopCost == 0)) throw new ArgumentOutOfRangeException(nameof(shopCost));
            if (basePower <= 0) throw new ArgumentOutOfRangeException(nameof(basePower));
            if (string.IsNullOrWhiteSpace(factionId)) throw new ArgumentException("A faction id is required.", nameof(factionId));
            if (string.IsNullOrWhiteSpace(professionId)) throw new ArgumentException("A profession id is required.", nameof(professionId));
            if (string.IsNullOrWhiteSpace(traitId)) throw new ArgumentException("A trait id is required.", nameof(traitId));
            DefinitionId = definitionId;
            IsCollectible = isCollectible;
            ShopCost = shopCost;
            BasePower = basePower;
            FactionId = factionId;
            ProfessionId = professionId;
            DetailRoleId = string.IsNullOrWhiteSpace(detailRoleId) ? professionId : detailRoleId;
            TraitId = traitId;
            Stats = stats ?? throw new ArgumentNullException(nameof(stats));
        }

        public CombatStatBlock CalculateCombatStats(int power, int runLevel)
        {
            return Stats.Calculate(power, runLevel);
        }
    }

    /// <summary>Versionable immutable definitions shared by collection, deck, battle and future server simulation.</summary>
    public sealed class CharacterCatalog
    {
        // Stable IDs are retained for save compatibility. Player-facing names are
        // the worldbuilding factions: Lantern Wardens, Moss Court and Riftrock.
        private const string LanternLeagueFaction = "faction.steel";
        private const string MossCourtFaction = "faction.wild";
        private const string GravelForgeFaction = "faction.starlight";
        private const string EnemyFaction = "faction.prototype.enemy";
        private readonly CharacterDefinition[] definitions;
        private readonly Dictionary<string, CharacterDefinition> byId;
        public IReadOnlyList<CharacterDefinition> Definitions => definitions;

        public CharacterCatalog(IEnumerable<CharacterDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var copied = new List<CharacterDefinition>();
            byId = new Dictionary<string, CharacterDefinition>();
            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Catalog entries cannot be null.", nameof(definitions));
                if (byId.ContainsKey(definition.DefinitionId)) throw new ArgumentException("Duplicate character definition id.", nameof(definitions));
                copied.Add(definition);
                byId.Add(definition.DefinitionId, definition);
            }
            if (copied.Count == 0) throw new ArgumentException("A catalog cannot be empty.", nameof(definitions));
            this.definitions = copied.ToArray();
        }

        public CharacterDefinition Get(string definitionId)
        {
            CharacterDefinition definition;
            if (!byId.TryGetValue(definitionId, out definition)) throw new KeyNotFoundException("Unknown character: " + definitionId);
            return definition;
        }

        public bool Contains(string definitionId)
        {
            return !string.IsNullOrEmpty(definitionId) && byId.ContainsKey(definitionId);
        }

        public static CharacterCatalog CreatePrototype()
        {
            return new CharacterCatalog(new[]
            {
                // C01 Lantern Shield Guard: Lantern Wardens / Guardian.
                Definition("unit.guardian", true, 5, 4, LanternLeagueFaction, "profession.guardian", 5, 0, 1, -1, 2,
                    armor: 18, speedTier: CombatSpeedTier.Slow, attackSpeedBasisPoints: 8000),
                // C02 Lantern Bearer: Lantern Wardens / Support.
                Definition("unit.healer", true, 3, 3, LanternLeagueFaction, "profession.support", 4, 0, 0, 1, 1, 1, 1,
                    armor: 7, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000),
                // C03 Path Hunter: Moss Court / Damage.
                Definition("unit.thornGuard", true, 5, 5, MossCourtFaction, "profession.output", 3, 0, 1, 0, 1,
                    armor: 5, speedTier: CombatSpeedTier.Fast, attackSpeedBasisPoints: 12000),
                // C04 Moss Herbalist: Moss Court / Support.
                Definition("unit.archer", true, 3, 3, MossCourtFaction, "profession.support", 4, 0, 0, 1, 1, 1, 1,
                    armor: 6, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000),
                // C05 Rock Hammer Guard: Riftrock / Guardian.
                Definition("unit.starblade", true, 3, 4, GravelForgeFaction, "profession.guardian", 5, 0, 1, -1, 2,
                    armor: 22, speedTier: CombatSpeedTier.Slow, attackSpeedBasisPoints: 8000),
                // C06 Ember Thrower: Riftrock / Damage.
                Definition("unit.starpriest", true, 5, 5, GravelForgeFaction, "profession.output", 3, 0, 1, 0, 1,
                    armor: 4, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000),
                Definition("unit.raider", false, 0, 3, EnemyFaction, "profession.guardian", 4, 0, 1, -1, 2,
                    armor: 12, speedTier: CombatSpeedTier.Slow, attackSpeedBasisPoints: 8000),
                Definition("unit.slinger", false, 0, 2, EnemyFaction, "profession.output", 2, 4, 1, 0, 1,
                    armor: 3, speedTier: CombatSpeedTier.Fast, attackSpeedBasisPoints: 12000),
                Definition("unit.boss", false, 0, 5, EnemyFaction, "profession.boss", 5, 0, 1, 0, 1,
                    armor: 20, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000),
                // E16 chapter-one enemy roster. Old prototype enemy ids remain above
                // solely for save compatibility and are no longer generated.
                Definition("enemy.n1.guard", false, 0, 1, EnemyFaction, "profession.guardian", 0, 20, 0, 3, 3,
                    armor: 20, speedTier: CombatSpeedTier.Slow, attackSpeedBasisPoints: 8000),
                Definition("enemy.n2.fighter", false, 0, 1, EnemyFaction, "profession.output", 0, 16, 0, 5, 5,
                    armor: 10, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000),
                Definition("enemy.n3.ranged", false, 0, 1, EnemyFaction, "profession.output", 0, 12, 0, 5, 5,
                    armor: 3, speedTier: CombatSpeedTier.Fast, attackSpeedBasisPoints: 12000),
                Definition("enemy.n4.skirmisher", false, 0, 1, EnemyFaction, "profession.output", 0, 13, 0, 4, 4,
                    armor: 5, speedTier: CombatSpeedTier.Fast, attackSpeedBasisPoints: 12000, trait: "trait.execute15"),
                Definition("enemy.n5.support", false, 0, 1, EnemyFaction, "profession.support", 0, 14, 0, 2, 2,
                    healFlat: 5, armor: 7, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000),
                Definition("enemy.e1.fortress", false, 0, 1, EnemyFaction, "profession.elite", 0, 34, 0, 4, 4,
                    armor: 28, speedTier: CombatSpeedTier.Slow, attackSpeedBasisPoints: 8000, trait: "trait.pendingProtection",
                    detailRole: "profession.guardian"),
                Definition("enemy.e2.hunter", false, 0, 1, EnemyFaction, "profession.elite", 0, 22, 0, 8, 8,
                    armor: 8, speedTier: CombatSpeedTier.Fast, attackSpeedBasisPoints: 12000, trait: "trait.execute25",
                    detailRole: "profession.output"),
                Definition("enemy.e3.caster", false, 0, 1, EnemyFaction, "profession.elite", 0, 26, 0, 3, 3,
                    healFlat: 7, armor: 12, speedTier: CombatSpeedTier.Standard, attackSpeedBasisPoints: 10000, trait: "trait.pendingControl",
                    detailRole: "profession.caster"),
                Definition("enemy.b1.heavy", false, 0, 1, EnemyFaction, "profession.boss", 0, 72, 0, 9, 9,
                    armor: 34, speedTier: CombatSpeedTier.Slow, attackSpeedBasisPoints: 8000, trait: "trait.pendingBossPhase",
                    detailRole: "profession.guardian"),
                Definition("enemy.b2.hunter", false, 0, 1, EnemyFaction, "profession.boss", 0, 58, 0, 10, 10,
                    armor: 16, speedTier: CombatSpeedTier.Fast, attackSpeedBasisPoints: 12000, trait: "trait.execute25PendingSummon",
                    detailRole: "profession.output")
            });
        }

        private static CharacterDefinition Definition(
            string id,
            bool collectible,
            int shopCost,
            int basePower,
            string faction,
            string profession,
            int healthPerPower,
            int healthFlat,
            int attackPerPower,
            int attackFlat,
            int minimumAttack,
            int healPerPower = 0,
            int healFlat = 0,
            int armor = 0,
            CombatSpeedTier speedTier = CombatSpeedTier.Standard,
            int attackSpeedBasisPoints = 10000,
            string trait = "trait.none",
            string detailRole = null)
        {
            return new CharacterDefinition(
                id,
                collectible,
                shopCost,
                basePower,
                faction,
                profession,
                trait,
                new CombatStatFormula(healthPerPower, healthFlat, attackPerPower, attackFlat,
                    minimumAttack, healPerPower, healFlat, armor, speedTier, attackSpeedBasisPoints),
                detailRole);
        }
    }
}
