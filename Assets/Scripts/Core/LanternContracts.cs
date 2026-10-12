using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum LanternVisualForm
    {
        Breakwater = 0,
        LayeredShell = 1,
        TideEcho = 2
    }

    public static class LanternArtContract
    {
        public const int NativeSize = 142;
        public const string RuntimeResourceRoot =
            "Art/Lanterns/Chapter01/GuardTideCircular/";
        public const string BreakwaterTemplateId = FirstChapterLanternFixtures.GuardTideTemplateId;
        public const string LayeredShellTemplateId = "lantern.guard_tide.layered_shell.ch1";
        public const string TideEchoTemplateId = "lantern.guard_tide.tide_echo.ch1";

        public static LanternVisualForm ResolveForm(string templateId)
        {
            if (string.Equals(templateId, LayeredShellTemplateId, StringComparison.Ordinal))
                return LanternVisualForm.LayeredShell;
            if (string.Equals(templateId, TideEchoTemplateId, StringComparison.Ordinal))
                return LanternVisualForm.TideEcho;
            return LanternVisualForm.Breakwater;
        }

        public static string ResourceId(string templateId, LanternQuality quality)
        {
            if (!Enum.IsDefined(typeof(LanternQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            var qualityName = quality.ToString().ToUpperInvariant();
            var qualityFolder = quality.ToString().ToLowerInvariant();
            return RuntimeResourceRoot + qualityFolder + "/" +
                   "ART-LANTERN-CH01-GUARDTIDE-B-RING-" + FormToken(ResolveForm(templateId)) +
                   "-" + qualityName + "-142-v1.0.0";
        }

        private static string FormToken(LanternVisualForm form)
        {
            switch (form)
            {
                case LanternVisualForm.Breakwater:
                    return "A1-BREAKWATER";
                case LanternVisualForm.LayeredShell:
                    return "A2-LAYERED-SHELL";
                case LanternVisualForm.TideEcho:
                    return "A3-TIDE-ECHO";
                default:
                    throw new ArgumentOutOfRangeException(nameof(form));
            }
        }
    }

    public static class HomeLanternModularArtContract
    {
        public const int NativeSize = 142;
        public const int AppearanceCount = 6;
        public const string PackageVersion = "home-lantern-modular-combination-v1.1.0";
        public const string StarCompassTemplateId = "lantern.star_compass.ch1";
        public const string DeepSeaPrismTemplateId = "lantern.deep_sea_prism.ch1";
        public const string DawnTideCrownTemplateId = "lantern.dawn_tide_crown.ch1";
        public const string QualityBackgroundRoot =
            "Art/HomeLanternModular/V110/quality-backgrounds/";
        public const string PatternRoot = "Art/HomeLanternModular/V110/lantern-patterns/";

        public static string QualityBackgroundResourceId(LanternQuality quality)
        {
            if (!Enum.IsDefined(typeof(LanternQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            return QualityBackgroundRoot +
                   "ART-UI-HOME-LANTERN-SLOT-INNER-BG-CHART-" +
                   quality.ToString().ToUpperInvariant() + "-142-v1.1.0";
        }

        public static string PatternResourceId(string appearanceId)
        {
            if (!IsValidAppearanceId(appearanceId))
                throw new ArgumentOutOfRangeException(nameof(appearanceId));
            return PatternRoot + "ART-UI-HOME-LANTERN-PATTERN-" +
                   appearanceId + "-142-v1.1.0";
        }

        public static string ResolveAppearanceId(string templateId)
        {
            if (string.Equals(templateId, LanternArtContract.LayeredShellTemplateId,
                    StringComparison.Ordinal))
                return "L02";
            if (string.Equals(templateId, LanternArtContract.TideEchoTemplateId,
                    StringComparison.Ordinal))
                return "L03";
            if (string.Equals(templateId, StarCompassTemplateId, StringComparison.Ordinal))
                return "L04";
            if (string.Equals(templateId, DeepSeaPrismTemplateId, StringComparison.Ordinal))
                return "L05";
            if (string.Equals(templateId, DawnTideCrownTemplateId, StringComparison.Ordinal))
                return "L06";
            return "L01";
        }

        public static string TemplateIdForAppearanceId(string appearanceId)
        {
            if (!IsValidAppearanceId(appearanceId))
                throw new ArgumentOutOfRangeException(nameof(appearanceId));
            switch (appearanceId)
            {
                case "L01": return FirstChapterLanternFixtures.GuardTideTemplateId;
                case "L02": return LanternArtContract.LayeredShellTemplateId;
                case "L03": return LanternArtContract.TideEchoTemplateId;
                case "L04": return StarCompassTemplateId;
                case "L05": return DeepSeaPrismTemplateId;
                case "L06": return DawnTideCrownTemplateId;
                default: throw new ArgumentOutOfRangeException(nameof(appearanceId));
            }
        }

        public static bool IsValidAppearanceId(string appearanceId)
        {
            if (string.IsNullOrEmpty(appearanceId) || appearanceId.Length != 3 ||
                appearanceId[0] != 'L') return false;
            int index;
            return int.TryParse(appearanceId.Substring(1), out index) && index >= 1 && index <= 6;
        }
    }

    public enum LanternQuality
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4
    }

    [Flags]
    public enum LanternUsageLock
    {
        None = 0,
        Expedition = 1,
        Defense = 2,
        Market = 4
    }

    public sealed class LanternModifierRoll
    {
        public string ModifierId { get; }
        public int Value { get; }

        public LanternModifierRoll(string modifierId, int value)
        {
            if (string.IsNullOrWhiteSpace(modifierId))
                throw new ArgumentException("A lantern modifier id is required.", nameof(modifierId));
            ModifierId = modifierId;
            Value = value;
        }
    }

    public sealed class LanternInstanceState
    {
        private readonly LanternModifierRoll[] modifierRolls;

        public string LanternInstanceId { get; }
        public string TemplateId { get; }
        public LanternQuality Quality { get; }
        public string SeasonId { get; }
        public string SourceChapterId { get; }
        public string SourceDifficultyId { get; }
        public string CoreEffectId { get; }
        public string BaseFlameStatId { get; }
        public int BaseFlameValue { get; }
        public int ContractStage { get; }
        public IReadOnlyList<LanternModifierRoll> ModifierRolls { get { return modifierRolls; } }
        public string RewardReceiptId { get; }
        public string RulesVersion { get; }
        public LanternUsageLock UsageLocks { get; internal set; }

        public LanternInstanceState(
            string lanternInstanceId,
            string templateId,
            LanternQuality quality,
            string seasonId,
            string sourceChapterId,
            string sourceDifficultyId,
            string coreEffectId,
            IEnumerable<LanternModifierRoll> modifierRolls,
            string rewardReceiptId,
            string rulesVersion,
            LanternUsageLock usageLocks = LanternUsageLock.None,
            string baseFlameStatId = "team.max_health_bps",
            int baseFlameValue = 0,
            int contractStage = 0)
        {
            if (string.IsNullOrWhiteSpace(lanternInstanceId))
                throw new ArgumentException("A lantern instance id is required.", nameof(lanternInstanceId));
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("A lantern template id is required.", nameof(templateId));
            if (!Enum.IsDefined(typeof(LanternQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            if (string.IsNullOrWhiteSpace(seasonId))
                throw new ArgumentException("A lantern season id is required.", nameof(seasonId));
            if (string.IsNullOrWhiteSpace(sourceChapterId))
                throw new ArgumentException("A source chapter id is required.", nameof(sourceChapterId));
            if (string.IsNullOrWhiteSpace(sourceDifficultyId))
                throw new ArgumentException("A source difficulty id is required.", nameof(sourceDifficultyId));
            if (string.IsNullOrWhiteSpace(coreEffectId))
                throw new ArgumentException("A core effect id is required.", nameof(coreEffectId));
            if (string.IsNullOrWhiteSpace(rewardReceiptId))
                throw new ArgumentException("A reward receipt id is required.", nameof(rewardReceiptId));
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("A lantern rules version is required.", nameof(rulesVersion));
            if (string.IsNullOrWhiteSpace(baseFlameStatId))
                throw new ArgumentException("A base flame stat id is required.", nameof(baseFlameStatId));
            if (baseFlameValue < 0)
                throw new ArgumentOutOfRangeException(nameof(baseFlameValue));
            if (contractStage < 0 || contractStage > 6)
                throw new ArgumentOutOfRangeException(nameof(contractStage));

            LanternInstanceId = lanternInstanceId;
            TemplateId = templateId;
            Quality = quality;
            SeasonId = seasonId;
            SourceChapterId = sourceChapterId;
            SourceDifficultyId = sourceDifficultyId;
            CoreEffectId = coreEffectId;
            BaseFlameStatId = baseFlameStatId;
            BaseFlameValue = baseFlameValue;
            ContractStage = contractStage;
            this.modifierRolls = CopyModifierRolls(modifierRolls);
            RewardReceiptId = rewardReceiptId;
            RulesVersion = rulesVersion;
            UsageLocks = usageLocks;
        }

        private static LanternModifierRoll[] CopyModifierRolls(IEnumerable<LanternModifierRoll> values)
        {
            if (values == null) return new LanternModifierRoll[0];
            var copied = new List<LanternModifierRoll>();
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentException("Lantern modifiers cannot contain null entries.", nameof(values));
                copied.Add(new LanternModifierRoll(value.ModifierId, value.Value));
            }
            return copied.ToArray();
        }
    }

    public sealed class LanternSnapshot
    {
        public const string NoneInstanceId = "lantern.none";
        private readonly LanternModifierRoll[] modifierRolls;

        public bool IsNone { get; }
        public string LanternInstanceId { get; }
        public string TemplateId { get; }
        public LanternQuality Quality { get; }
        public string SeasonId { get; }
        public string SourceChapterId { get; }
        public string CoreEffectId { get; }
        public string BaseFlameStatId { get; }
        public int BaseFlameValue { get; }
        public int ContractStage { get; }
        public IReadOnlyList<LanternModifierRoll> ModifierRolls { get { return modifierRolls; } }
        public string RulesVersion { get; }

        private LanternSnapshot(
            bool isNone,
            string lanternInstanceId,
            string templateId,
            LanternQuality quality,
            string seasonId,
            string sourceChapterId,
            string coreEffectId,
            IEnumerable<LanternModifierRoll> modifierRolls,
            string rulesVersion,
            string baseFlameStatId,
            int baseFlameValue,
            int contractStage)
        {
            IsNone = isNone;
            LanternInstanceId = lanternInstanceId ?? string.Empty;
            TemplateId = templateId ?? string.Empty;
            Quality = quality;
            SeasonId = seasonId ?? string.Empty;
            SourceChapterId = sourceChapterId ?? string.Empty;
            CoreEffectId = coreEffectId ?? string.Empty;
            BaseFlameStatId = baseFlameStatId ?? string.Empty;
            BaseFlameValue = baseFlameValue;
            ContractStage = contractStage;
            this.modifierRolls = CopyModifierRolls(modifierRolls);
            RulesVersion = rulesVersion ?? string.Empty;
        }

        public static LanternSnapshot None(string rulesVersion)
        {
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("A snapshot rules version is required.", nameof(rulesVersion));
            return new LanternSnapshot(true, NoneInstanceId, NoneInstanceId,
                LanternQuality.Common, string.Empty, string.Empty, string.Empty,
                null, rulesVersion, string.Empty, 0, 0);
        }

        public static LanternSnapshot FromInstance(LanternInstanceState instance)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            return new LanternSnapshot(false, instance.LanternInstanceId, instance.TemplateId,
                instance.Quality, instance.SeasonId, instance.SourceChapterId,
                instance.CoreEffectId, instance.ModifierRolls, instance.RulesVersion,
                instance.BaseFlameStatId, instance.BaseFlameValue, instance.ContractStage);
        }

        public static LanternSnapshot Restore(
            bool isNone,
            string lanternInstanceId,
            string templateId,
            LanternQuality quality,
            string seasonId,
            string sourceChapterId,
            string coreEffectId,
            IEnumerable<LanternModifierRoll> modifierRolls,
            string rulesVersion,
            string baseFlameStatId = "team.max_health_bps",
            int baseFlameValue = 0,
            int contractStage = 0)
        {
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("A snapshot rules version is required.", nameof(rulesVersion));
            if (isNone) return None(rulesVersion);
            if (string.IsNullOrWhiteSpace(lanternInstanceId))
                throw new ArgumentException("A lantern instance id is required.", nameof(lanternInstanceId));
            if (string.IsNullOrWhiteSpace(templateId))
                throw new ArgumentException("A lantern template id is required.", nameof(templateId));
            if (!Enum.IsDefined(typeof(LanternQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            if (string.IsNullOrWhiteSpace(seasonId))
                throw new ArgumentException("A lantern season id is required.", nameof(seasonId));
            if (string.IsNullOrWhiteSpace(sourceChapterId))
                throw new ArgumentException("A source chapter id is required.", nameof(sourceChapterId));
            if (string.IsNullOrWhiteSpace(coreEffectId))
                throw new ArgumentException("A core effect id is required.", nameof(coreEffectId));
            if (string.IsNullOrWhiteSpace(baseFlameStatId))
                throw new ArgumentException("A base flame stat id is required.", nameof(baseFlameStatId));
            if (baseFlameValue < 0)
                throw new ArgumentOutOfRangeException(nameof(baseFlameValue));
            if (contractStage < 0 || contractStage > 6)
                throw new ArgumentOutOfRangeException(nameof(contractStage));
            return new LanternSnapshot(false, lanternInstanceId, templateId, quality,
                seasonId, sourceChapterId, coreEffectId, modifierRolls, rulesVersion,
                baseFlameStatId, baseFlameValue, contractStage);
        }

        private static LanternModifierRoll[] CopyModifierRolls(IEnumerable<LanternModifierRoll> values)
        {
            if (values == null) return new LanternModifierRoll[0];
            var copied = new List<LanternModifierRoll>();
            foreach (var value in values)
            {
                if (value == null) throw new ArgumentException("Lantern modifiers cannot contain null entries.", nameof(values));
                copied.Add(new LanternModifierRoll(value.ModifierId, value.Value));
            }
            return copied.ToArray();
        }
    }

    public static class FirstChapterLanternFixtures
    {
        public const string FirstClearInstanceId = "LGT-C-000001";
        public const string FirstClearRewardReceiptId = "local-first-clear-test-LGT-C-000001";
        public const string GuardTideTemplateId = "lantern.guard_tide.ch1";
        public const string TideShellInitialCoreEffectId = "lantern.core.tide_shell.initial";
        public const string ArmorModifierId = "armor_flat";
        public const string MaxHealthModifierId = "max_health_bps";
        public const string AttackModifierId = "attack_bps";
        public const string HealingModifierId = "healing_bps";
        public const string AttackSpeedModifierId = "precise_attack_speed_bps";
        public const string MaxHealthBasisPointsStatId = "team.max_health_bps";
        public const string LegacyArmorModifierId = "lantern.modifier.armor";
        public const string LegacyMaxHealthModifierId = "lantern.modifier.max_health";
        public const string LegacyAttackModifierId = "lantern.modifier.attack";
        public const string LegacyHealingModifierId = "lantern.modifier.healing";
        public const string LegacyAttackSpeedModifierId = "lantern.modifier.attack_speed";
        public const string LegacyMaxHealthBasisPointsStatId = "maxHealthBps";
        public const int FirstClearBaseFlameValue = 1300;
        public const int FirstClearArmorValue = 3;

        public static LanternInstanceState CreateFirstClearCommon()
        {
            return new LanternInstanceState(
                FirstClearInstanceId,
                GuardTideTemplateId,
                LanternQuality.Common,
                "season-0",
                "chapter-1",
                "D0",
                TideShellInitialCoreEffectId,
                new[] { new LanternModifierRoll(ArmorModifierId, FirstClearArmorValue) },
                FirstClearRewardReceiptId,
                "guard-tide-initial.v0.4",
                LanternUsageLock.None,
                MaxHealthBasisPointsStatId,
                FirstClearBaseFlameValue,
                0);
        }

        public static bool EnsureFirstClearCommon(LanternInventory inventory)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            foreach (var instance in inventory.Instances)
            {
                if (string.Equals(instance.LanternInstanceId, FirstClearInstanceId,
                        StringComparison.Ordinal)) return false;
            }
            return inventory.TryAdd(CreateFirstClearCommon());
        }

        public static bool IsFirstClearAward(LanternInstanceState instance)
        {
            return instance != null &&
                   string.Equals(instance.LanternInstanceId, FirstClearInstanceId,
                       StringComparison.Ordinal) &&
                   string.Equals(instance.RewardReceiptId, FirstClearRewardReceiptId,
                       StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// First-chapter economy baseline confirmed in the economist's v0.8/v0.2 contracts.
    /// Percentage values are basis points; armor is an integer. The representative instances
    /// are fixed audit fixtures, while production rewards must persist their independently
    /// rolled values inside LanternInstanceState.
    /// </summary>
    public static class FirstChapterLanternEconomyRules
    {
        public const string RulesVersion = "chapter01.guard_tide.initial.v0.3";

        private static readonly int[] BaseFlameMinimum = { 1000, 1500, 2000, 2500, 3000 };
        private static readonly int[] BaseFlameMaximum = { 1500, 2000, 2500, 3000, 3500 };
        private static readonly int[] GlyphCounts = { 1, 1, 2, 2, 3 };
        private static readonly string[] GlyphIds =
        {
            FirstChapterLanternFixtures.MaxHealthModifierId,
            FirstChapterLanternFixtures.AttackModifierId,
            FirstChapterLanternFixtures.HealingModifierId,
            FirstChapterLanternFixtures.AttackSpeedModifierId,
            FirstChapterLanternFixtures.ArmorModifierId
        };

        public static int BaseFlameMinimumFor(LanternQuality quality)
        {
            return BaseFlameMinimum[QualityIndex(quality)];
        }

        public static int BaseFlameMaximumFor(LanternQuality quality)
        {
            return BaseFlameMaximum[QualityIndex(quality)];
        }

        public static int GlyphCountFor(LanternQuality quality)
        {
            return GlyphCounts[QualityIndex(quality)];
        }

        public static bool IsBaseFlameValueValid(LanternQuality quality, int value)
        {
            return value >= BaseFlameMinimumFor(quality) &&
                   value <= BaseFlameMaximumFor(quality) && value % 10 == 0;
        }

        public static bool IsGlyphValueValid(LanternQuality quality, LanternModifierRoll modifier)
        {
            if (modifier == null) return false;
            int minimum;
            int maximum;
            int step;
            if (!TryGetGlyphRange(quality, modifier.ModifierId, out minimum, out maximum,
                    out step)) return false;
            return modifier.Value >= minimum && modifier.Value <= maximum &&
                   (modifier.Value - minimum) % step == 0;
        }

        /// <summary>
        /// Creates one replay-stable lantern instance after the authoritative reward table has
        /// already selected its quality. Appearance, base flame and glyph rolls are independent
        /// deterministic rolls derived from the settlement seed; client code never invents
        /// quality probabilities here.
        /// </summary>
        public static LanternInstanceState CreateGeneratedInstance(
            string instanceId,
            LanternQuality quality,
            string generationSeed,
            string rewardReceiptId,
            string seasonId = "season-0",
            string chapterId = "chapter-1",
            string difficultyId = "D0")
        {
            if (string.IsNullOrWhiteSpace(generationSeed))
                throw new ArgumentException("A replay-stable generation seed is required.",
                    nameof(generationSeed));

            var state = StableSeed(generationSeed);
            var appearanceId = "L" + (Next(ref state,
                HomeLanternModularArtContract.AppearanceCount) + 1).ToString("00");
            var templateId = HomeLanternModularArtContract.TemplateIdForAppearanceId(appearanceId);

            var minimumBaseFlame = BaseFlameMinimumFor(quality);
            var maximumBaseFlame = BaseFlameMaximumFor(quality);
            var baseFlame = minimumBaseFlame + Next(ref state,
                (maximumBaseFlame - minimumBaseFlame) / 10 + 1) * 10;

            var availableGlyphs = new List<string>(GlyphIds);
            var glyphs = new List<LanternModifierRoll>();
            for (var index = 0; index < GlyphCountFor(quality); index++)
            {
                var selectedIndex = Next(ref state, availableGlyphs.Count);
                var modifierId = availableGlyphs[selectedIndex];
                availableGlyphs.RemoveAt(selectedIndex);
                int minimum;
                int maximum;
                int step;
                if (!TryGetGlyphRange(quality, modifierId, out minimum, out maximum, out step))
                    throw new InvalidOperationException("Unknown lantern glyph range: " + modifierId);
                var value = minimum + Next(ref state, (maximum - minimum) / step + 1) * step;
                glyphs.Add(new LanternModifierRoll(modifierId, value));
            }

            return new LanternInstanceState(
                instanceId,
                templateId,
                quality,
                seasonId,
                chapterId,
                difficultyId,
                FirstChapterLanternFixtures.TideShellInitialCoreEffectId,
                glyphs,
                rewardReceiptId,
                RulesVersion,
                LanternUsageLock.None,
                FirstChapterLanternFixtures.MaxHealthBasisPointsStatId,
                baseFlame,
                0);
        }

        public static LanternInstanceState CreateConfirmedRepresentative(LanternQuality quality)
        {
            var qualityName = quality.ToString().ToLowerInvariant();
            var baseFlame = 1300;
            LanternModifierRoll[] glyphs;
            switch (quality)
            {
                case LanternQuality.Common:
                    glyphs = new[]
                    {
                        new LanternModifierRoll(FirstChapterLanternFixtures.ArmorModifierId, 3)
                    };
                    break;
                case LanternQuality.Uncommon:
                    baseFlame = 1750;
                    glyphs = new[]
                    {
                        new LanternModifierRoll(FirstChapterLanternFixtures.AttackModifierId, 450)
                    };
                    break;
                case LanternQuality.Rare:
                    baseFlame = 2200;
                    glyphs = new[]
                    {
                        new LanternModifierRoll(FirstChapterLanternFixtures.HealingModifierId, 650),
                        new LanternModifierRoll(FirstChapterLanternFixtures.ArmorModifierId, 5)
                    };
                    break;
                case LanternQuality.Epic:
                    baseFlame = 2700;
                    glyphs = new[]
                    {
                        new LanternModifierRoll(FirstChapterLanternFixtures.MaxHealthModifierId, 700),
                        new LanternModifierRoll(FirstChapterLanternFixtures.ArmorModifierId, 8)
                    };
                    break;
                case LanternQuality.Legendary:
                    baseFlame = 3300;
                    glyphs = new[]
                    {
                        new LanternModifierRoll(FirstChapterLanternFixtures.MaxHealthModifierId, 900),
                        new LanternModifierRoll(FirstChapterLanternFixtures.HealingModifierId, 1000),
                        new LanternModifierRoll(FirstChapterLanternFixtures.AttackSpeedModifierId, 1000)
                    };
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(quality));
            }

            return new LanternInstanceState(
                "economy-proof-" + qualityName,
                HomeLanternModularArtContract.TemplateIdForAppearanceId(
                    "L" + ((int)quality + 1).ToString("00")),
                quality,
                "season-0",
                "chapter-1",
                "D0",
                FirstChapterLanternFixtures.TideShellInitialCoreEffectId,
                glyphs,
                "economy-proof-" + qualityName,
                RulesVersion,
                LanternUsageLock.None,
                FirstChapterLanternFixtures.MaxHealthBasisPointsStatId,
                baseFlame,
                0);
        }

        public static int EnsureDevelopmentQualitySet(LanternInventory inventory)
        {
            if (inventory == null) throw new ArgumentNullException(nameof(inventory));
            var added = 0;
            for (var qualityIndex = (int)LanternQuality.Uncommon;
                 qualityIndex <= (int)LanternQuality.Legendary;
                 qualityIndex++)
            {
                var instance = CreateConfirmedRepresentative((LanternQuality)qualityIndex);
                if (!inventory.Contains(instance.LanternInstanceId) && inventory.TryAdd(instance)) added++;
            }
            return added;
        }

        public static bool MatchesConfirmedQualityContract(LanternInstanceState instance)
        {
            if (instance == null || !IsBaseFlameValueValid(instance.Quality, instance.BaseFlameValue) ||
                instance.ModifierRolls.Count != GlyphCountFor(instance.Quality)) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var modifier in instance.ModifierRolls)
                if (!IsGlyphValueValid(instance.Quality, modifier) || !ids.Add(modifier.ModifierId))
                    return false;
            return true;
        }

        private static int QualityIndex(LanternQuality quality)
        {
            if (!Enum.IsDefined(typeof(LanternQuality), quality))
                throw new ArgumentOutOfRangeException(nameof(quality));
            return (int)quality;
        }

        private static bool TryGetGlyphRange(
            LanternQuality quality, string modifierId,
            out int minimum, out int maximum, out int step)
        {
            var index = QualityIndex(quality);
            step = 10;
            switch (modifierId)
            {
                case FirstChapterLanternFixtures.MaxHealthModifierId:
                case FirstChapterLanternFixtures.LegacyMaxHealthModifierId:
                    minimum = new[] { 300, 400, 500, 700, 900 }[index];
                    maximum = new[] { 500, 600, 800, 1000, 1200 }[index];
                    return true;
                case FirstChapterLanternFixtures.AttackModifierId:
                case FirstChapterLanternFixtures.LegacyAttackModifierId:
                    minimum = new[] { 250, 400, 600, 800, 1050 }[index];
                    maximum = new[] { 400, 600, 800, 1050, 1300 }[index];
                    return true;
                case FirstChapterLanternFixtures.HealingModifierId:
                case FirstChapterLanternFixtures.LegacyHealingModifierId:
                    minimum = new[] { 300, 450, 600, 800, 1000 }[index];
                    maximum = new[] { 500, 650, 850, 1100, 1400 }[index];
                    return true;
                case FirstChapterLanternFixtures.AttackSpeedModifierId:
                case FirstChapterLanternFixtures.LegacyAttackSpeedModifierId:
                    minimum = new[] { 250, 350, 500, 700, 900 }[index];
                    maximum = new[] { 400, 550, 750, 1000, 1300 }[index];
                    return true;
                case FirstChapterLanternFixtures.ArmorModifierId:
                case FirstChapterLanternFixtures.LegacyArmorModifierId:
                    minimum = new[] { 2, 3, 5, 7, 10 }[index];
                    maximum = new[] { 4, 5, 7, 10, 13 }[index];
                    step = 1;
                    return true;
                default:
                    minimum = 0;
                    maximum = 0;
                    step = 1;
                    return false;
            }
        }

        private static uint StableSeed(string value)
        {
            unchecked
            {
                var hash = 2166136261u;
                foreach (var character in value)
                {
                    hash ^= character;
                    hash *= 16777619u;
                }
                return hash == 0u ? 0x9E3779B9u : hash;
            }
        }

        private static int Next(ref uint state, int maximum)
        {
            if (maximum <= 1) return 0;
            unchecked
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
            }
            return (int)(state % (uint)maximum);
        }
    }

    public static class LanternCombatStatRules
    {
        public const int BasisPointScale = 10000;
        public const int TideShellInitialShieldBasisPoints = 2500;

        public static int ApplyMaximumHealth(int baseMaximumHealth, LanternSnapshot snapshot)
        {
            var safeBase = Math.Max(0, baseMaximumHealth);
            if (snapshot == null || snapshot.IsNone ||
                !MatchesBaseFlameStat(snapshot.BaseFlameStatId)) return safeBase;
            var bonus = snapshot.BaseFlameValue + ModifierTotal(snapshot,
                FirstChapterLanternFixtures.MaxHealthModifierId);
            return ClampToInt((long)safeBase * (BasisPointScale + bonus) /
                              BasisPointScale);
        }

        public static int ApplyArmor(int baseArmor, LanternSnapshot snapshot)
        {
            var result = Math.Max(0, baseArmor);
            if (snapshot == null || snapshot.IsNone) return result;
            foreach (var modifier in snapshot.ModifierRolls)
            {
                if (modifier != null && MatchesModifierId(modifier.ModifierId,
                        FirstChapterLanternFixtures.ArmorModifierId,
                        FirstChapterLanternFixtures.LegacyArmorModifierId))
                    result = ClampToInt((long)result + modifier.Value);
            }
            return Math.Min(80, result);
        }

        public static int ApplyAttackDamage(int baseAttackDamage, LanternSnapshot snapshot)
        {
            return ApplyPercentageModifier(baseAttackDamage, snapshot,
                FirstChapterLanternFixtures.AttackModifierId);
        }

        public static int ApplyHealingAmount(int baseHealingAmount, LanternSnapshot snapshot)
        {
            return ApplyPercentageModifier(baseHealingAmount, snapshot,
                FirstChapterLanternFixtures.HealingModifierId);
        }

        public static int ApplyAttackSpeed(int baseAttackSpeedBasisPoints, LanternSnapshot snapshot)
        {
            var safeBase = baseAttackSpeedBasisPoints > 0 ? baseAttackSpeedBasisPoints : BasisPointScale;
            if (snapshot == null || snapshot.IsNone) return safeBase;
            return ContinuousAttackSpeedRules.CalculateFinal(safeBase,
                ModifierTotal(snapshot, FirstChapterLanternFixtures.AttackSpeedModifierId));
        }

        public static int TideShellOpeningShield(int finalMaximumHealth, LanternSnapshot snapshot)
        {
            if (snapshot == null || snapshot.IsNone ||
                !string.Equals(snapshot.CoreEffectId,
                    FirstChapterLanternFixtures.TideShellInitialCoreEffectId,
                    StringComparison.Ordinal)) return 0;
            var shield = (long)Math.Max(0, finalMaximumHealth) * TideShellInitialShieldBasisPoints /
                         BasisPointScale;
            return shield <= 0 ? 1 : ClampToInt(shield);
        }

        private static int ClampToInt(long value)
        {
            if (value <= 0) return 0;
            return value >= int.MaxValue ? int.MaxValue : (int)value;
        }

        private static int ApplyPercentageModifier(int baseValue, LanternSnapshot snapshot,
            string modifierId)
        {
            var safeBase = Math.Max(0, baseValue);
            if (snapshot == null || snapshot.IsNone) return safeBase;
            return ClampToInt((long)safeBase *
                              (BasisPointScale + ModifierTotal(snapshot, modifierId)) /
                              BasisPointScale);
        }

        private static int ModifierTotal(LanternSnapshot snapshot, string modifierId)
        {
            var result = 0;
            foreach (var modifier in snapshot.ModifierRolls)
                if (modifier != null && MatchesModifierId(modifier.ModifierId, modifierId,
                        LegacyModifierId(modifierId)))
                    result = ClampToInt((long)result + modifier.Value);
            return result;
        }

        private static bool MatchesBaseFlameStat(string statId)
        {
            return string.Equals(statId,
                       FirstChapterLanternFixtures.MaxHealthBasisPointsStatId,
                       StringComparison.Ordinal) ||
                   string.Equals(statId,
                       FirstChapterLanternFixtures.LegacyMaxHealthBasisPointsStatId,
                       StringComparison.Ordinal);
        }

        private static bool MatchesModifierId(string actual, string current, string legacy)
        {
            return string.Equals(actual, current, StringComparison.Ordinal) ||
                   string.Equals(actual, legacy, StringComparison.Ordinal);
        }

        private static string LegacyModifierId(string modifierId)
        {
            if (string.Equals(modifierId, FirstChapterLanternFixtures.MaxHealthModifierId,
                    StringComparison.Ordinal))
                return FirstChapterLanternFixtures.LegacyMaxHealthModifierId;
            if (string.Equals(modifierId, FirstChapterLanternFixtures.AttackModifierId,
                    StringComparison.Ordinal))
                return FirstChapterLanternFixtures.LegacyAttackModifierId;
            if (string.Equals(modifierId, FirstChapterLanternFixtures.HealingModifierId,
                    StringComparison.Ordinal))
                return FirstChapterLanternFixtures.LegacyHealingModifierId;
            if (string.Equals(modifierId, FirstChapterLanternFixtures.AttackSpeedModifierId,
                    StringComparison.Ordinal))
                return FirstChapterLanternFixtures.LegacyAttackSpeedModifierId;
            return FirstChapterLanternFixtures.LegacyArmorModifierId;
        }
    }

    public sealed class LanternInventory
    {
        private readonly List<LanternInstanceState> instances = new List<LanternInstanceState>();
        private readonly Dictionary<string, LanternInstanceState> byId =
            new Dictionary<string, LanternInstanceState>(StringComparer.Ordinal);

        public IReadOnlyList<LanternInstanceState> Instances { get { return instances; } }
        public string ActiveLanternInstanceId { get; private set; }

        public LanternInventory(IEnumerable<LanternInstanceState> initialInstances = null,
            string activeLanternInstanceId = null)
        {
            if (initialInstances != null)
            {
                foreach (var instance in initialInstances)
                {
                    if (!TryAdd(instance))
                        throw new ArgumentException("Lantern instances must be non-null and unique.", nameof(initialInstances));
                }
            }
            if (!string.IsNullOrEmpty(activeLanternInstanceId) && !TrySetActive(activeLanternInstanceId) &&
                !byId.ContainsKey(activeLanternInstanceId))
                throw new ArgumentException("The active lantern must exist in the inventory.", nameof(activeLanternInstanceId));
        }

        public LanternInstanceState Get(string lanternInstanceId)
        {
            LanternInstanceState instance;
            if (!byId.TryGetValue(lanternInstanceId, out instance))
                throw new KeyNotFoundException("Unknown lantern instance: " + lanternInstanceId);
            return instance;
        }

        public bool Contains(string lanternInstanceId)
        {
            return !string.IsNullOrWhiteSpace(lanternInstanceId) && byId.ContainsKey(lanternInstanceId);
        }

        public bool TryAdd(LanternInstanceState instance)
        {
            return TryAdd(instance, false);
        }

        public bool TryAddFirst(LanternInstanceState instance)
        {
            return TryAdd(instance, true);
        }

        private bool TryAdd(LanternInstanceState instance, bool insertFirst)
        {
            if (instance == null || byId.ContainsKey(instance.LanternInstanceId)) return false;
            byId.Add(instance.LanternInstanceId, instance);
            if (insertFirst) instances.Insert(0, instance);
            else instances.Add(instance);
            return true;
        }

        public bool TrySetActive(string lanternInstanceId)
        {
            LanternInstanceState instance;
            if (string.IsNullOrWhiteSpace(lanternInstanceId) ||
                !byId.TryGetValue(lanternInstanceId, out instance) ||
                (instance.UsageLocks & LanternUsageLock.Market) != 0)
                return false;
            ActiveLanternInstanceId = lanternInstanceId;
            return true;
        }

        public void ClearActive()
        {
            ActiveLanternInstanceId = null;
        }

        public LanternSnapshot CreateActiveSnapshot(string noneRulesVersion)
        {
            if (string.IsNullOrEmpty(ActiveLanternInstanceId)) return LanternSnapshot.None(noneRulesVersion);
            return LanternSnapshot.FromInstance(Get(ActiveLanternInstanceId));
        }

        public bool TryAddUsageLock(string lanternInstanceId, LanternUsageLock usageLock)
        {
            LanternInstanceState instance;
            if (usageLock == LanternUsageLock.None || !byId.TryGetValue(lanternInstanceId, out instance))
                return false;
            if ((usageLock & LanternUsageLock.Market) != 0)
            {
                bool ignored;
                return TryBeginMarketListing(lanternInstanceId, out ignored);
            }
            instance.UsageLocks |= usageLock;
            return true;
        }

        public bool TryRemoveUsageLock(string lanternInstanceId, LanternUsageLock usageLock)
        {
            LanternInstanceState instance;
            if (usageLock == LanternUsageLock.None || !byId.TryGetValue(lanternInstanceId, out instance))
                return false;
            instance.UsageLocks &= ~usageLock;
            return true;
        }

        public bool CanTransfer(string lanternInstanceId)
        {
            LanternInstanceState instance;
            return byId.TryGetValue(lanternInstanceId, out instance) &&
                   instance.UsageLocks == LanternUsageLock.None &&
                   !string.Equals(ActiveLanternInstanceId, lanternInstanceId, StringComparison.Ordinal);
        }

        public bool TryBeginMarketListing(string lanternInstanceId, out bool clearedActiveSelection)
        {
            clearedActiveSelection = false;
            LanternInstanceState instance;
            if (string.IsNullOrWhiteSpace(lanternInstanceId) ||
                !byId.TryGetValue(lanternInstanceId, out instance)) return false;

            clearedActiveSelection = string.Equals(ActiveLanternInstanceId,
                lanternInstanceId, StringComparison.Ordinal);
            if (clearedActiveSelection) ActiveLanternInstanceId = null;
            instance.UsageLocks &= ~(LanternUsageLock.Expedition | LanternUsageLock.Defense);
            instance.UsageLocks |= LanternUsageLock.Market;
            return true;
        }

        public bool TryCancelMarketListing(string lanternInstanceId)
        {
            LanternInstanceState instance;
            if (string.IsNullOrWhiteSpace(lanternInstanceId) ||
                !byId.TryGetValue(lanternInstanceId, out instance) ||
                (instance.UsageLocks & LanternUsageLock.Market) == 0) return false;
            instance.UsageLocks &= ~LanternUsageLock.Market;
            return true;
        }

        public bool TryCompleteMarketSale(string lanternInstanceId)
        {
            LanternInstanceState instance;
            if (string.IsNullOrWhiteSpace(lanternInstanceId) ||
                !byId.TryGetValue(lanternInstanceId, out instance)) return false;
            if (string.Equals(ActiveLanternInstanceId, lanternInstanceId,
                    StringComparison.Ordinal)) ActiveLanternInstanceId = null;
            byId.Remove(lanternInstanceId);
            instances.Remove(instance);
            return true;
        }

        public LanternSnapshot ResolveOwnedAvailableSnapshot(
            LanternSnapshot snapshot, string noneRulesVersion)
        {
            if (snapshot == null || snapshot.IsNone) return LanternSnapshot.None(noneRulesVersion);
            LanternInstanceState instance;
            if (!byId.TryGetValue(snapshot.LanternInstanceId, out instance) ||
                (instance.UsageLocks & LanternUsageLock.Market) != 0)
                return LanternSnapshot.None(noneRulesVersion);
            return LanternSnapshot.FromInstance(instance);
        }
    }

    public sealed class ExpeditionRewardTableReference
    {
        public string TableId { get; }
        public string Version { get; }

        public ExpeditionRewardTableReference(string tableId, string version)
        {
            if (string.IsNullOrWhiteSpace(tableId))
                throw new ArgumentException("A reward table id is required.", nameof(tableId));
            if (string.IsNullOrWhiteSpace(version))
                throw new ArgumentException("A reward table version is required.", nameof(version));
            TableId = tableId;
            Version = version;
        }
    }

    public sealed class ExpeditionRunRewardSnapshot
    {
        public string DifficultyId { get; }
        public string EntryTypeId { get; }
        public string PaymentTierId { get; }
        public ExpeditionRewardTableReference CrystalRewards { get; }
        public ExpeditionRewardTableReference EquipmentRewards { get; }
        public ExpeditionRewardTableReference LanternRewards { get; }
        public string RulesVersion { get; }

        public ExpeditionRunRewardSnapshot(
            string difficultyId,
            string entryTypeId,
            string paymentTierId,
            ExpeditionRewardTableReference crystalRewards,
            ExpeditionRewardTableReference equipmentRewards,
            ExpeditionRewardTableReference lanternRewards,
            string rulesVersion)
        {
            if (string.IsNullOrWhiteSpace(difficultyId))
                throw new ArgumentException("A difficulty id is required.", nameof(difficultyId));
            if (string.IsNullOrWhiteSpace(entryTypeId))
                throw new ArgumentException("An entry type id is required.", nameof(entryTypeId));
            if (string.IsNullOrWhiteSpace(paymentTierId))
                throw new ArgumentException("A payment tier id is required.", nameof(paymentTierId));
            DifficultyId = difficultyId;
            EntryTypeId = entryTypeId;
            PaymentTierId = paymentTierId;
            CrystalRewards = crystalRewards ?? throw new ArgumentNullException(nameof(crystalRewards));
            EquipmentRewards = equipmentRewards ?? throw new ArgumentNullException(nameof(equipmentRewards));
            LanternRewards = lanternRewards ?? throw new ArgumentNullException(nameof(lanternRewards));
            if (string.IsNullOrWhiteSpace(rulesVersion))
                throw new ArgumentException("A run reward rules version is required.", nameof(rulesVersion));
            RulesVersion = rulesVersion;
        }
    }

    public sealed class ExpeditionRunSnapshot
    {
        public string RunId { get; }
        public string SeasonId { get; }
        public string ChapterId { get; }
        public string IdempotencyKey { get; }
        public LanternSnapshot Lantern { get; }
        public ExpeditionRunRewardSnapshot Rewards { get; }

        public ExpeditionRunSnapshot(string runId, string seasonId, string chapterId,
            string idempotencyKey, LanternSnapshot lantern, ExpeditionRunRewardSnapshot rewards)
        {
            if (string.IsNullOrWhiteSpace(runId)) throw new ArgumentException("A run id is required.", nameof(runId));
            if (string.IsNullOrWhiteSpace(seasonId)) throw new ArgumentException("A season id is required.", nameof(seasonId));
            if (string.IsNullOrWhiteSpace(chapterId)) throw new ArgumentException("A chapter id is required.", nameof(chapterId));
            if (string.IsNullOrWhiteSpace(idempotencyKey)) throw new ArgumentException("An idempotency key is required.", nameof(idempotencyKey));
            RunId = runId;
            SeasonId = seasonId;
            ChapterId = chapterId;
            IdempotencyKey = idempotencyKey;
            Lantern = lantern ?? throw new ArgumentNullException(nameof(lantern));
            Rewards = rewards ?? throw new ArgumentNullException(nameof(rewards));
        }
    }

    public sealed class ChapterLighthouseProgressState
    {
        private bool firstClearTestBypassActive;

        public bool BossDefeated { get; private set; }
        public bool ChapterLanternAwarded { get; private set; }
        public bool IsLit { get; private set; }

        public bool CanLight
        {
            get
            {
                return !IsLit && (BossDefeated || firstClearTestBypassActive) &&
                       ChapterLanternAwarded;
            }
        }

        public void RecordBossVictory()
        {
            BossDefeated = true;
            firstClearTestBypassActive = false;
        }

        public void RecordChapterLanternAward()
        {
            ChapterLanternAwarded = true;
        }

        public bool RecordEquippedChapterLantern(LanternInstanceState instance,
            bool allowFirstClearTestBypass = false)
        {
            if (instance == null || !string.Equals(instance.SourceChapterId, "chapter-1",
                    StringComparison.Ordinal)) return false;
            firstClearTestBypassActive = allowFirstClearTestBypass &&
                                         FirstChapterLanternFixtures.IsFirstClearAward(instance);
            RecordChapterLanternAward();
            return TryLight();
        }

        public void RecordLanternUnequipped()
        {
            IsLit = false;
            firstClearTestBypassActive = false;
        }

        public bool TryLight()
        {
            if (!CanLight) return false;
            IsLit = true;
            return true;
        }

        public void Restore(bool bossDefeated, bool chapterLanternAwarded, bool isLit)
        {
            firstClearTestBypassActive = false;
            BossDefeated = bossDefeated;
            ChapterLanternAwarded = chapterLanternAwarded || isLit;
            IsLit = isLit && BossDefeated && ChapterLanternAwarded;
        }
    }

    public sealed class LanternLegacyMigrationResult
    {
        public const string LegacyNoneRulesVersion = "lantern.none.legacy-v1";

        public bool BossDefeated { get; }
        public bool ChapterLighthouseLit { get; }
        public LanternSnapshot ExpeditionLanternSnapshot { get; }
        public LanternSnapshot ArenaDefenseLanternSnapshot { get; }
        public bool ArenaDefenseNeedsResync { get; }

        private LanternLegacyMigrationResult(bool bossDefeated, bool chapterLighthouseLit,
            LanternSnapshot expeditionLanternSnapshot, LanternSnapshot arenaDefenseLanternSnapshot,
            bool arenaDefenseNeedsResync)
        {
            BossDefeated = bossDefeated;
            ChapterLighthouseLit = chapterLighthouseLit;
            ExpeditionLanternSnapshot = expeditionLanternSnapshot;
            ArenaDefenseLanternSnapshot = arenaDefenseLanternSnapshot;
            ArenaDefenseNeedsResync = arenaDefenseNeedsResync;
        }

        public static LanternLegacyMigrationResult FromLegacySave(
            bool chapterBossDefeated,
            bool chapterLighthouseRepaired,
            bool hasPublishedDefense)
        {
            return new LanternLegacyMigrationResult(
                chapterBossDefeated || chapterLighthouseRepaired,
                chapterLighthouseRepaired,
                LanternSnapshot.None(LegacyNoneRulesVersion),
                LanternSnapshot.None(LegacyNoneRulesVersion),
                hasPublishedDefense);
        }
    }
}
