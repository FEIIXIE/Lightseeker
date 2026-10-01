using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SeekerAutoChess.Core
{
    public sealed class CharacterRarityInstanceSnapshot
    {
        public string InstanceId { get; private set; }
        public string TemplateId { get; private set; }
        public CharacterQuality Quality { get; private set; }
        public int AptitudeRoll { get; private set; }
        public int ExternalStar { get; private set; }
        public CombatStatBlock DerivedStats { get; private set; }
        public string SourceBusinessId { get; private set; }
        public string RarityRulesVersion { get; private set; }
        public string RollRulesVersion { get; private set; }
        public string StateHash { get; private set; }

        public CharacterRarityInstanceSnapshot(
            string instanceId,
            string templateId,
            CharacterQuality quality,
            int aptitudeRoll,
            int externalStar,
            CombatStatBlock derivedStats,
            string sourceBusinessId,
            int stateVersion = 1,
            string bindingState = "unbound",
            string seasonId = "prototype-season")
        {
            if (string.IsNullOrWhiteSpace(instanceId)) throw new ArgumentException("An instance id is required.", nameof(instanceId));
            if (string.IsNullOrWhiteSpace(templateId)) throw new ArgumentException("A template id is required.", nameof(templateId));
            if (!Enum.IsDefined(typeof(CharacterQuality), quality)) throw new ArgumentOutOfRangeException(nameof(quality));
            if (aptitudeRoll < 0 || aptitudeRoll > CharacterRarityInstanceRules.RollScale)
                throw new ArgumentOutOfRangeException(nameof(aptitudeRoll));
            if (externalStar < 0 || externalStar > 2) throw new ArgumentOutOfRangeException(nameof(externalStar));
            if (derivedStats == null) throw new ArgumentNullException(nameof(derivedStats));
            InstanceId = instanceId;
            TemplateId = templateId;
            Quality = quality;
            AptitudeRoll = aptitudeRoll;
            ExternalStar = externalStar;
            DerivedStats = derivedStats;
            SourceBusinessId = string.IsNullOrWhiteSpace(sourceBusinessId) ? instanceId : sourceBusinessId;
            RarityRulesVersion = CharacterRarityInstanceRules.RarityRulesVersion;
            RollRulesVersion = CharacterRarityInstanceRules.RollRulesVersion;
            StateHash = CharacterRarityInstanceRules.ComputeStateHash(
                this, bindingState, seasonId, stateVersion);
        }
    }

    public static class CharacterRarityInstanceRules
    {
        public const int MultiplierScale = 10000;
        public const int RollScale = 10000;
        public const string RarityRulesVersion = "rarity.v0.2";
        public const string RollRulesVersion = "roll.v0.2";

        private struct Range
        {
            public int Min;
            public int Max;

            public Range(int min, int max)
            {
                Min = min;
                Max = max;
            }
        }

        public static int ComputeAptitudeRoll(string sourceBusinessId)
        {
            if (string.IsNullOrWhiteSpace(sourceBusinessId)) sourceBusinessId = "missing-business-id";
            var digest = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(sourceBusinessId));
            var first = ToUnsignedInt(digest, 0) % (RollScale + 1);
            var second = ToUnsignedInt(digest, 4) % (RollScale + 1);
            return RoundHalfUp((int)(first + second), 2);
        }

        public static int CalculateMultiplier(CharacterQuality quality, int aptitudeRoll)
        {
            if (aptitudeRoll < 0 || aptitudeRoll > RollScale)
                throw new ArgumentOutOfRangeException(nameof(aptitudeRoll));
            var range = GetRange(quality);
            return RoundHalfUp(range.Min * (RollScale - aptitudeRoll) + range.Max * aptitudeRoll, RollScale);
        }

        public static CombatStatBlock CalculateCombatStats(
            CharacterDefinition definition,
            int basePower,
            int runLevel,
            CharacterQuality quality,
            int aptitudeRoll,
            int externalStar = 0)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (externalStar < 0 || externalStar > 2) throw new ArgumentOutOfRangeException(nameof(externalStar));
            var baseStats = definition.CalculateCombatStats(basePower, runLevel);
            var instanceMultiplier = CalculateMultiplier(quality, aptitudeRoll);
            var starMultiplier = externalStar == 0 ? MultiplierScale : externalStar == 1 ? 11000 : 12100;
            return new CombatStatBlock(
                Scale(Scale(baseStats.MaxHealth, instanceMultiplier), starMultiplier),
                Scale(Scale(baseStats.AttackDamage, instanceMultiplier), starMultiplier),
                Scale(Scale(baseStats.HealAmount, instanceMultiplier), starMultiplier),
                baseStats.Armor,
                baseStats.SpeedTier,
                baseStats.AttackSpeedBasisPoints);
        }

        public static CharacterRarityInstanceSnapshot CreateSnapshot(
            CharacterDefinition definition,
            string instanceId,
            string sourceBusinessId,
            CharacterQuality quality,
            int basePower,
            int runLevel,
            int externalStar = 0,
            int? aptitudeRoll = null,
            string bindingState = "unbound",
            string seasonId = "prototype-season",
            int stateVersion = 1)
        {
            var roll = aptitudeRoll.HasValue ? aptitudeRoll.Value : ComputeAptitudeRoll(sourceBusinessId);
            var stats = CalculateCombatStats(definition, basePower, runLevel, quality, roll, externalStar);
            return new CharacterRarityInstanceSnapshot(instanceId, definition.DefinitionId,
                quality, roll, externalStar, stats, sourceBusinessId, stateVersion, bindingState, seasonId);
        }

        public static int GetMinimumMultiplier(CharacterQuality quality) { return GetRange(quality).Min; }
        public static int GetMaximumMultiplier(CharacterQuality quality) { return GetRange(quality).Max; }

        public static string ComputeStateHash(
            CharacterRarityInstanceSnapshot snapshot,
            string bindingState,
            string seasonId,
            int stateVersion)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var stats = snapshot.DerivedStats;
            var canonical = "{\"instanceId\":\"" + Escape(snapshot.InstanceId) +
                "\",\"templateId\":\"" + Escape(snapshot.TemplateId) +
                "\",\"quality\":\"" + snapshot.Quality.ToString().ToLowerInvariant() +
                "\",\"aptitudeRoll\":" + snapshot.AptitudeRoll +
                ",\"primaryRoll\":null,\"traitBudgetBps\":null,\"derivedStats\":{"+
                "\"maxHealth\":" + stats.MaxHealth + ",\"attackDamage\":" + stats.AttackDamage +
                ",\"healAmount\":" + stats.HealAmount + ",\"armor\":" + stats.Armor +
                ",\"speedTier\":" + (int)stats.SpeedTier + "},\"rarityRulesVersion\":\"" +
                RarityRulesVersion + "\",\"rollRulesVersion\":\"" + RollRulesVersion +
                "\",\"sourceBusinessId\":\"" + Escape(snapshot.SourceBusinessId) +
                "\",\"bindingState\":\"" + Escape(bindingState ?? string.Empty) +
                "\",\"seasonId\":\"" + Escape(seasonId ?? string.Empty) +
                "\",\"stateVersion\":" + stateVersion + "}";
            using (var sha = SHA256.Create())
            {
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(canonical)))
                    .Replace("-", string.Empty);
            }
        }

        private static Range GetRange(CharacterQuality quality)
        {
            switch (quality)
            {
                case CharacterQuality.Common: return new Range(9700, 10300);
                case CharacterQuality.Uncommon: return new Range(11834, 12566);
                case CharacterQuality.Rare: return new Range(14475, 15525);
                case CharacterQuality.Epic: return new Range(17760, 19240);
                case CharacterQuality.Legendary: return new Range(21965, 24035);
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }

        private static int Scale(int value, int multiplier)
        {
            if (value <= 0) return 0;
            return RoundHalfUp(value * multiplier, MultiplierScale);
        }

        private static int RoundHalfUp(int numerator, int denominator)
        {
            return (numerator + denominator / 2) / denominator;
        }

        private static uint ToUnsignedInt(byte[] bytes, int offset)
        {
            return (uint)(bytes[offset] | bytes[offset + 1] << 8 |
                bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
