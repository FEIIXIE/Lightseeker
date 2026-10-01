using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum EquipmentSlot { Head, Body, Feet, Hands, Weapon }
    public enum EquipmentQuality { Common, Rare, Epic }

    [Flags]
    public enum EquipmentUsageLock
    {
        None = 0,
        Expedition = 1,
        Defense = 2,
        Market = 4
    }

    public sealed class EquipmentStatModifier
    {
        public int MaxHealth { get; }
        public int AttackDamage { get; }
        public int Armor { get; }
        public int HealAmount { get; }
        public int SpeedTierShift { get; }

        public EquipmentStatModifier(
            int maxHealth = 0,
            int attackDamage = 0,
            int armor = 0,
            int healAmount = 0,
            int speedTierShift = 0)
        {
            if (maxHealth < 0 || attackDamage < 0 || armor < 0 || healAmount < 0)
                throw new ArgumentOutOfRangeException(nameof(maxHealth));
            if (speedTierShift < -5 || speedTierShift > 5)
                throw new ArgumentOutOfRangeException(nameof(speedTierShift));
            MaxHealth = maxHealth;
            AttackDamage = attackDamage;
            Armor = armor;
            HealAmount = healAmount;
            SpeedTierShift = speedTierShift;
        }

        public static EquipmentStatModifier Add(EquipmentStatModifier left, EquipmentStatModifier right)
        {
            left = left ?? new EquipmentStatModifier();
            right = right ?? new EquipmentStatModifier();
            return new EquipmentStatModifier(
                left.MaxHealth + right.MaxHealth,
                left.AttackDamage + right.AttackDamage,
                left.Armor + right.Armor,
                left.HealAmount + right.HealAmount,
                Math.Max(-5, Math.Min(5, left.SpeedTierShift + right.SpeedTierShift)));
        }
    }

    public sealed class EquipmentTemplateDefinition
    {
        public string TemplateId { get; }
        public EquipmentSlot Slot { get; }
        public EquipmentStatModifier Modifier { get; }

        public EquipmentTemplateDefinition(
            string templateId, EquipmentSlot slot, EquipmentStatModifier modifier)
        {
            if (string.IsNullOrWhiteSpace(templateId)) throw new ArgumentException("A template id is required.", nameof(templateId));
            TemplateId = templateId;
            Slot = slot;
            Modifier = modifier ?? throw new ArgumentNullException(nameof(modifier));
        }
    }

    public sealed class EquipmentCatalog
    {
        private readonly Dictionary<string, EquipmentTemplateDefinition> byId;
        private readonly EquipmentTemplateDefinition[] definitions;
        public IReadOnlyList<EquipmentTemplateDefinition> Definitions { get { return definitions; } }

        public EquipmentCatalog(IEnumerable<EquipmentTemplateDefinition> definitions)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            var copied = new List<EquipmentTemplateDefinition>();
            byId = new Dictionary<string, EquipmentTemplateDefinition>(StringComparer.Ordinal);
            foreach (var definition in definitions)
            {
                if (definition == null || byId.ContainsKey(definition.TemplateId))
                    throw new ArgumentException("Equipment templates must be non-null and unique.", nameof(definitions));
                byId.Add(definition.TemplateId, definition);
                copied.Add(definition);
            }
            if (copied.Count == 0) throw new ArgumentException("An equipment catalog cannot be empty.", nameof(definitions));
            this.definitions = copied.ToArray();
        }

        public EquipmentTemplateDefinition Get(string templateId)
        {
            EquipmentTemplateDefinition definition;
            if (!byId.TryGetValue(templateId, out definition))
                throw new KeyNotFoundException("Unknown equipment template: " + templateId);
            return definition;
        }

        public static EquipmentCatalog CreatePrototype()
        {
            return new EquipmentCatalog(new[]
            {
                new EquipmentTemplateDefinition("equipment.lanternHelm", EquipmentSlot.Head,
                    new EquipmentStatModifier(armor: 2)),
                new EquipmentTemplateDefinition("equipment.oldVoyageClothes", EquipmentSlot.Body,
                    new EquipmentStatModifier(maxHealth: 6)),
                new EquipmentTemplateDefinition("equipment.mossPathShoes", EquipmentSlot.Feet,
                    new EquipmentStatModifier(maxHealth: 3)),
                new EquipmentTemplateDefinition("equipment.forgeRivetGauntlet", EquipmentSlot.Hands,
                    new EquipmentStatModifier(attackDamage: 1)),
                new EquipmentTemplateDefinition("equipment.lanternHarborBlade", EquipmentSlot.Weapon,
                    new EquipmentStatModifier(attackDamage: 1))
            });
        }
    }

    public sealed class EquipmentInstanceState
    {
        public string EquipmentInstanceId { get; }
        public string TemplateId { get; }
        public EquipmentQuality Quality { get; }
        public string SeasonId { get; }
        public string EquippedCharacterInstanceId { get; internal set; }
        public EquipmentUsageLock UsageLocks { get; internal set; }

        public EquipmentInstanceState(
            string equipmentInstanceId,
            string templateId,
            EquipmentQuality quality,
            string seasonId,
            string equippedCharacterInstanceId = null,
            EquipmentUsageLock usageLocks = EquipmentUsageLock.None)
        {
            if (string.IsNullOrWhiteSpace(equipmentInstanceId)) throw new ArgumentException("An equipment instance id is required.", nameof(equipmentInstanceId));
            if (string.IsNullOrWhiteSpace(templateId)) throw new ArgumentException("A template id is required.", nameof(templateId));
            if (string.IsNullOrWhiteSpace(seasonId)) throw new ArgumentException("A season id is required.", nameof(seasonId));
            EquipmentInstanceId = equipmentInstanceId;
            TemplateId = templateId;
            Quality = quality;
            SeasonId = seasonId;
            EquippedCharacterInstanceId = equippedCharacterInstanceId;
            UsageLocks = usageLocks;
        }
    }

    public sealed class EquipmentLoadoutSnapshot
    {
        private readonly string[] equipmentInstanceIds;
        public IReadOnlyList<string> EquipmentInstanceIds { get { return equipmentInstanceIds; } }
        public EquipmentStatModifier Modifier { get; }

        public EquipmentLoadoutSnapshot(IEnumerable<string> equipmentInstanceIds, EquipmentStatModifier modifier)
        {
            this.equipmentInstanceIds = equipmentInstanceIds == null
                ? new string[0]
                : new List<string>(equipmentInstanceIds).ToArray();
            Modifier = modifier ?? new EquipmentStatModifier();
        }
    }

    public sealed class EquipmentInventory
    {
        private readonly EquipmentCatalog catalog;
        private readonly Dictionary<string, EquipmentInstanceState> byId;
        private readonly List<EquipmentInstanceState> instances;
        public IReadOnlyList<EquipmentInstanceState> Instances { get { return instances; } }

        public EquipmentInventory(EquipmentCatalog catalog, IEnumerable<EquipmentInstanceState> instances)
        {
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            if (instances == null) throw new ArgumentNullException(nameof(instances));
            var copied = new List<EquipmentInstanceState>();
            byId = new Dictionary<string, EquipmentInstanceState>(StringComparer.Ordinal);
            foreach (var instance in instances)
            {
                if (instance == null || byId.ContainsKey(instance.EquipmentInstanceId))
                    throw new ArgumentException("Equipment instances must be non-null and unique.", nameof(instances));
                catalog.Get(instance.TemplateId);
                byId.Add(instance.EquipmentInstanceId, instance);
                copied.Add(instance);
            }
            this.instances = copied;
        }

        public EquipmentInstanceState Get(string equipmentInstanceId)
        {
            EquipmentInstanceState instance;
            if (!byId.TryGetValue(equipmentInstanceId, out instance))
                throw new KeyNotFoundException("Unknown equipment instance: " + equipmentInstanceId);
            return instance;
        }

        public bool TryAdd(EquipmentInstanceState instance)
        {
            if (instance == null || byId.ContainsKey(instance.EquipmentInstanceId)) return false;
            try
            {
                catalog.Get(instance.TemplateId);
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
            byId.Add(instance.EquipmentInstanceId, instance);
            instances.Add(instance);
            return true;
        }

        public bool TryEquip(string equipmentInstanceId, string characterInstanceId)
        {
            EquipmentInstanceState instance;
            if (string.IsNullOrWhiteSpace(characterInstanceId) || !byId.TryGetValue(equipmentInstanceId, out instance)) return false;
            if (instance.UsageLocks != EquipmentUsageLock.None) return false;
            if (!string.IsNullOrEmpty(instance.EquippedCharacterInstanceId) &&
                instance.EquippedCharacterInstanceId != characterInstanceId) return false;
            var slot = catalog.Get(instance.TemplateId).Slot;
            foreach (var other in instances)
            {
                if (other.EquipmentInstanceId == instance.EquipmentInstanceId ||
                    other.EquippedCharacterInstanceId != characterInstanceId ||
                    catalog.Get(other.TemplateId).Slot != slot) continue;
                if (other.UsageLocks != EquipmentUsageLock.None) return false;
                other.EquippedCharacterInstanceId = null;
            }
            instance.EquippedCharacterInstanceId = characterInstanceId;
            return true;
        }

        public bool TryUnequip(string equipmentInstanceId, string characterInstanceId)
        {
            EquipmentInstanceState instance;
            if (!byId.TryGetValue(equipmentInstanceId, out instance) ||
                instance.EquippedCharacterInstanceId != characterInstanceId ||
                instance.UsageLocks != EquipmentUsageLock.None) return false;
            instance.EquippedCharacterInstanceId = null;
            return true;
        }

        public bool TryAddUsageLock(string equipmentInstanceId, EquipmentUsageLock usageLock)
        {
            EquipmentInstanceState instance;
            if (usageLock == EquipmentUsageLock.None || !byId.TryGetValue(equipmentInstanceId, out instance))
                return false;
            instance.UsageLocks |= usageLock;
            return true;
        }

        public bool TryRemoveUsageLock(string equipmentInstanceId, EquipmentUsageLock usageLock)
        {
            EquipmentInstanceState instance;
            if (usageLock == EquipmentUsageLock.None || !byId.TryGetValue(equipmentInstanceId, out instance))
                return false;
            instance.UsageLocks &= ~usageLock;
            return true;
        }

        public EquipmentLoadoutSnapshot CreateSnapshot(string characterInstanceId)
        {
            var ids = new List<string>();
            var modifier = new EquipmentStatModifier();
            foreach (var instance in instances)
            {
                if (instance.EquippedCharacterInstanceId != characterInstanceId) continue;
                ids.Add(instance.EquipmentInstanceId);
                modifier = EquipmentStatModifier.Add(modifier, catalog.Get(instance.TemplateId).Modifier);
            }
            return new EquipmentLoadoutSnapshot(ids, modifier);
        }

        public static EquipmentInventory CreatePrototype(EquipmentCatalog catalog)
        {
            var instances = new List<EquipmentInstanceState>();
            foreach (var definition in catalog.Definitions)
            {
                instances.Add(new EquipmentInstanceState(
                    "prototype." + definition.TemplateId,
                    definition.TemplateId,
                    EquipmentQuality.Common,
                    "prototype-season"));
            }
            return new EquipmentInventory(catalog, instances);
        }
    }

    public static class EquipmentDerivedStatCalculator
    {
        public static CombatStatBlock Calculate(
            CharacterDefinition definition,
            int power,
            int runLevel,
            EquipmentLoadoutSnapshot loadout)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            return Calculate(definition.CalculateCombatStats(power, runLevel), loadout);
        }

        public static CombatStatBlock Calculate(
            CombatStatBlock baseStats,
            EquipmentLoadoutSnapshot loadout)
        {
            if (baseStats == null) throw new ArgumentNullException(nameof(baseStats));
            var modifier = loadout == null ? new EquipmentStatModifier() : loadout.Modifier;
            var speedBonus = modifier.SpeedTierShift >= 0
                ? Math.Min(2500, modifier.SpeedTierShift * 1500)
                : Math.Max(-3500, modifier.SpeedTierShift * 1800);
            var attackSpeed = ContinuousAttackSpeedRules.CalculateFinal(
                baseStats.AttackSpeedBasisPoints, speedBonus);
            return new CombatStatBlock(
                baseStats.MaxHealth + modifier.MaxHealth,
                baseStats.AttackDamage + modifier.AttackDamage,
                baseStats.HealAmount + modifier.HealAmount,
                Math.Max(0, Math.Min(80, baseStats.Armor + modifier.Armor)),
                baseStats.SpeedTier,
                attackSpeed);
        }
    }
}
