using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public enum StaticBattleArtTier
    {
        Player,
        Normal,
        Elite,
        Boss
    }

    /// <summary>
    /// Engine-facing record for the chapter-one static battle art handoff. Coordinates
    /// remain in the original 1212x1297 top-origin canvas so presentation code never
    /// invents per-unit placement compensation.
    /// </summary>
    public sealed class StaticBattleArtEntry
    {
        public string DefinitionId { get; }
        public string ResourceId { get; }
        public string DisplayNameZh { get; }
        public string DisplayNameEn { get; }
        public StaticBattleArtTier Tier { get; }
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }
        public float PresentationScale { get; }
        public float ShadowScale { get; }
        public float VisualYOffsetRelativeToUnitHeight { get; }

        public StaticBattleArtEntry(
            string definitionId,
            string resourceId,
            string displayNameZh,
            string displayNameEn,
            StaticBattleArtTier tier,
            int minX,
            int minY,
            int maxX,
            int maxY,
            float presentationScale,
            float visualYOffsetRelativeToUnitHeight)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("Definition id is required.", nameof(definitionId));
            if (string.IsNullOrWhiteSpace(resourceId)) throw new ArgumentException("Resource id is required.", nameof(resourceId));
            if (minX < 0 || minY < 0 || maxX < minX || maxY < minY ||
                maxX >= StaticBattleArtContract.CanvasWidth || maxY >= StaticBattleArtContract.CanvasHeight)
                throw new ArgumentOutOfRangeException(nameof(minX));
            DefinitionId = definitionId;
            ResourceId = resourceId;
            DisplayNameZh = displayNameZh;
            DisplayNameEn = displayNameEn;
            Tier = tier;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            PresentationScale = Math.Max(0.01f, presentationScale);
            ShadowScale = tier == StaticBattleArtTier.Boss
                ? 1.55f
                : tier == StaticBattleArtTier.Elite ? 1.25f : 1f;
            VisualYOffsetRelativeToUnitHeight = visualYOffsetRelativeToUnitHeight;
        }
    }

    /// <summary>
    /// Single source of truth shared by import validation and runtime presentation.
    /// Formal formation and battle views use fixed slots and fixed front-facing masters.
    /// Per-entry presentation scale/offset values come only from the approved art manifest;
    /// they never alter logical slots, colliders, targeting or selection bounds.
    /// </summary>
    public static class StaticBattleArtContract
    {
        public const int CanvasWidth = 1212;
        public const int CanvasHeight = 1297;
        public const int GroundLineTopOrigin = 1281;
        public const float PivotX = 0.5f;
        public const float PivotY = 0.0123f;
        public const bool FormalBattleFlipX = false;
        public const string ResourcesFolder = "Art/BattleUnits/";

        private static readonly StaticBattleArtEntry[] entries =
        {
            Entry("unit.guardian", "ART-CHAR-C01-LAMPGUARD-COMMON-v02", "灯盾卫", "Lampguard", StaticBattleArtTier.Player, 63, 62, 1116, 1296),
            Entry("unit.healer", "ART-CHAR-C02-LAMPLADY-COMMON-v01", "灯女", "Lamplady", StaticBattleArtTier.Player, 261, 10, 953, 1283),
            Entry("unit.thornGuard", "ART-CHAR-C03-RANGER-COMMON-v03", "林径猎手", "Ranger", StaticBattleArtTier.Player, 215, 15, 1004, 1283),
            Entry("unit.archer", "ART-CHAR-C04-HERBALIST-COMMON-v01", "苔药师", "Herbalist", StaticBattleArtTier.Player, 253, 41, 960, 1284),
            Entry("unit.starblade", "ART-CHAR-C05-STONEWARD-COMMON-v01", "岩锤卫", "Stoneward", StaticBattleArtTier.Player, 86, 43, 1125, 1283),
            Entry("unit.starpriest", "ART-CHAR-C06-EMBERHAND-COMMON-v01", "炉火投手", "Emberhand", StaticBattleArtTier.Player, 258, 0, 953, 1282),

            Entry("enemy.n1.guard", "ART-MONSTER-CH01-N1-GATEBOARD-GUARD-01", "闸盾役", "Gateboard Guard", StaticBattleArtTier.Normal, 125, 120, 1110, 1238),
            Entry("enemy.n2.fighter", "ART-MONSTER-CH01-N2-CABLE-EATING-CRAB-01", "缆噬蟹", "Cable-Eating Crab", StaticBattleArtTier.Normal, 66, 561, 1145, 1280),
            Entry("enemy.n3.ranged", "ART-MONSTER-CH01-N3-REEF-LOOKOUT-01", "礁弩哨", "Reef Lookout", StaticBattleArtTier.Normal, 287, 209, 922, 1281),
            Entry("enemy.n4.skirmisher", "ART-MONSTER-CH01-N4-MIST-LEAPING-RAY-01", "雾跃鳐", "Mist-Leaping Ray", StaticBattleArtTier.Normal, 101, 500, 1110, 1102, 1.2f, -0.12f),
            Entry("enemy.n5.support", "ART-MONSTER-CH01-N5-SAILMENDER-01", "补帆医", "Sailmender", StaticBattleArtTier.Normal, 242, 241, 969, 1280),

            Entry("enemy.e1.fortress", "ART-MONSTER-CH01-E1-BREAKWATER-SHELLBEAST-01", "镇堤壳兽", "Breakwater Shellbeast", StaticBattleArtTier.Elite, 46, 537, 1164, 1281, 1.5f),
            Entry("enemy.e2.hunter", "ART-MONSTER-CH01-E2-TRAIL-HUNTER-01", "索迹猎长", "Trail Hunter", StaticBattleArtTier.Elite, 230, 372, 983, 1276, 1.5f),
            Entry("enemy.e3.caster", "ART-MONSTER-CH01-E3-ECHO-BELLKEEPER-02", "回声司钟", "Echo Bellkeeper", StaticBattleArtTier.Elite, 303, 370, 910, 1269, 1.5f),

            Entry("enemy.b1.heavy", "ART-MONSTER-CH01-B1-KEEPER-OF-NO-RETURN-01", "无归守灯人", "Keeper of No Return", StaticBattleArtTier.Boss, 116, 24, 1102, 1281, 1.5f),
            Entry("enemy.b2.hunter", "ART-MONSTER-CH01-B2-MIST-SHEPHERD-01", "引雾牧者", "Mist Shepherd", StaticBattleArtTier.Boss, 0, 11, 1209, 1281, 1.5f)
        };

        private static readonly Dictionary<string, StaticBattleArtEntry> byDefinitionId = BuildIndex();

        public static IReadOnlyList<StaticBattleArtEntry> Entries { get { return entries; } }

        public static StaticBattleArtEntry Get(string definitionId)
        {
            StaticBattleArtEntry entry;
            if (!byDefinitionId.TryGetValue(definitionId, out entry))
                throw new KeyNotFoundException("Unknown static battle art definition: " + definitionId);
            return entry;
        }

        public static bool TryGet(string definitionId, out StaticBattleArtEntry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(definitionId) && byDefinitionId.TryGetValue(definitionId, out entry);
        }

        private static StaticBattleArtEntry Entry(
            string definitionId,
            string resourceId,
            string displayNameZh,
            string displayNameEn,
            StaticBattleArtTier tier,
            int minX,
            int minY,
            int maxX,
            int maxY,
            float presentationScale = 1f,
            float visualYOffsetRelativeToUnitHeight = 0f)
        {
            return new StaticBattleArtEntry(definitionId, resourceId, displayNameZh, displayNameEn,
                tier, minX, minY, maxX, maxY, presentationScale,
                visualYOffsetRelativeToUnitHeight);
        }

        private static Dictionary<string, StaticBattleArtEntry> BuildIndex()
        {
            var result = new Dictionary<string, StaticBattleArtEntry>(StringComparer.Ordinal);
            foreach (var entry in entries) result.Add(entry.DefinitionId, entry);
            return result;
        }
    }

    public sealed class PieceBaseSkinDefinition
    {
        public string SkinId { get; }
        public string ResourceId { get; }

        public PieceBaseSkinDefinition(string skinId, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(skinId))
                throw new ArgumentException("Piece base skin id is required.", nameof(skinId));
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("Piece base resource id is required.", nameof(resourceId));
            SkinId = skinId;
            ResourceId = resourceId;
        }

        public string ResourcesPath
        {
            get { return PieceBasePresentationContract.ResourcesFolder + ResourceId; }
        }
    }

    /// <summary>
    /// Presentation-only contract for the replaceable fixed-piece base. These values
    /// never alter formation slots, targeting, collision or authoritative battle state.
    /// </summary>
    public static class PieceBasePresentationContract
    {
        public const int RulesVersion = 2;
        public const string DefaultSkinId = "base.harborWoodIron.v04";
        public const string DefaultResourceId = "BASE-HARBOR-WOOD-IRON-MAIN-v04";
        public const string ResourcesFolder = "Art/BattlePieces/";
        public const int CanvasWidth = 2048;
        public const int CanvasHeight = 768;
        public const int AnchorPixelX = 1024;
        public const int AnchorPixelYTopOrigin = 330;
        public const float FootAnchorTopNormalizedX = 0.5f;
        public const float FootAnchorTopNormalizedY = 0.43f;
        public const float DefaultDisplayWidth = 172f;

        private static readonly PieceBaseSkinDefinition defaultSkin =
            new PieceBaseSkinDefinition(DefaultSkinId, DefaultResourceId);

        public static PieceBaseSkinDefinition DefaultSkin
        {
            get { return defaultSkin; }
        }

        public static bool TryGetSkin(string skinId, out PieceBaseSkinDefinition skin)
        {
            if (string.Equals(skinId, DefaultSkinId, StringComparison.Ordinal))
            {
                skin = defaultSkin;
                return true;
            }
            skin = null;
            return false;
        }

        public static float DisplayWidthForVisibleCharacterWidth(float visibleCharacterWidth)
        {
            var measuredWidth = Math.Max(0f, visibleCharacterWidth) * 1.25f;
            return Math.Max(DefaultDisplayWidth * 0.84f,
                Math.Min(DefaultDisplayWidth * 1.60f, measuredWidth));
        }

        public static float DisplayHeightForVisibleCharacterWidth(float visibleCharacterWidth)
        {
            return DisplayWidthForVisibleCharacterWidth(visibleCharacterWidth) *
                   CanvasHeight / (float)CanvasWidth;
        }

    }

    /// <summary>
    /// Canonical five-rarity static-art selection for the six launch characters.
    /// All entries share the untrimmed 1212x1297 canvas and foot pivot defined by
    /// StaticBattleArtContract. Quality changes art selection, never transforms.
    /// </summary>
    public sealed class CharacterRarityArtEntry
    {
        public string DefinitionId { get; }
        public string QualityId { get; }
        public string ResourceId { get; }
        public int MinX { get; }
        public int MinY { get; }
        public int MaxX { get; }
        public int MaxY { get; }

        public CharacterRarityArtEntry(
            string definitionId, string qualityId, string resourceId,
            int minX, int minY, int maxX, int maxY)
        {
            if (string.IsNullOrWhiteSpace(definitionId))
                throw new ArgumentException("Definition id is required.", nameof(definitionId));
            if (string.IsNullOrWhiteSpace(qualityId))
                throw new ArgumentException("Quality id is required.", nameof(qualityId));
            if (string.IsNullOrWhiteSpace(resourceId))
                throw new ArgumentException("Resource id is required.", nameof(resourceId));
            if (minX < 0 || minY < 0 || maxX < minX || maxY < minY ||
                maxX >= StaticBattleArtContract.CanvasWidth || maxY >= StaticBattleArtContract.CanvasHeight)
                throw new ArgumentOutOfRangeException(nameof(minX));

            DefinitionId = definitionId;
            QualityId = qualityId;
            ResourceId = resourceId;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }
    }

    public static class CharacterRarityArtContract
    {
        private static readonly CharacterRarityArtEntry[] entries =
        {
            Entry("unit.guardian", "common", "ART-CHAR-C01-LAMPGUARD-COMMON-v02", 63, 62, 1116, 1296),
            Entry("unit.guardian", "uncommon", "ART-CHAR-C01-LAMPGUARD-UNCOMMON-v02", 41, 8, 1126, 1270),
            Entry("unit.guardian", "rare", "ART-CHAR-C01-LAMPGUARD-RARE-v02", 0, 9, 1112, 1296),
            Entry("unit.guardian", "epic", "ART-CHAR-C01-OLIN-EPIC-BREAKWATER-TOWER-GUARD-v06", 16, 29, 1196, 1284),
            Entry("unit.guardian", "legendary", "ART-CHAR-C01-OLIN-LEGENDARY-TIDEGATE-TOWER-WARDEN-v04", 16, 1, 1211, 1284),
            Entry("unit.healer", "common", "ART-CHAR-C02-LAMPLADY-COMMON-v01", 261, 10, 953, 1283),
            Entry("unit.healer", "uncommon", "ART-CHAR-C02-LAMPLADY-UNCOMMON-v01", 282, 10, 968, 1283),
            Entry("unit.healer", "rare", "ART-CHAR-C02-LAMPLADY-RARE-v01", 265, 5, 970, 1285),
            Entry("unit.healer", "epic", "ART-CHAR-C02-YIWEN-EPIC-TIDAL-LANTERN-OFFICER-v01", 248, 5, 1015, 1281),
            Entry("unit.healer", "legendary", "ART-CHAR-C02-YIWEN-LEGENDARY-RETURN-LIGHT-LANTERN-OFFICER-v02", 249, 1, 1018, 1275),
            Entry("unit.thornGuard", "common", "ART-CHAR-C03-RANGER-COMMON-v03", 215, 15, 1004, 1283),
            Entry("unit.thornGuard", "uncommon", "ART-CHAR-C03-RANGER-UNCOMMON-v03", 213, 15, 1013, 1287),
            Entry("unit.thornGuard", "rare", "ART-CHAR-C03-RANGER-RARE-v03", 211, 13, 1027, 1291),
            Entry("unit.thornGuard", "epic", "ART-CHAR-C03-LEIFU-EPIC-RETURN-ROUTE-CARTOGRAPHER-v03", 208, 12, 1002, 1279),
            Entry("unit.thornGuard", "legendary", "ART-CHAR-C03-LEIFU-LEGENDARY-LAST-WAYMARK-v03", 204, 13, 1007, 1279),
            Entry("unit.archer", "common", "ART-CHAR-C04-HERBALIST-COMMON-v01", 253, 41, 960, 1284),
            Entry("unit.archer", "uncommon", "ART-CHAR-C04-HERBALIST-UNCOMMON-v01", 252, 42, 970, 1282),
            Entry("unit.archer", "rare", "ART-CHAR-C04-HERBALIST-RARE-v01", 254, 36, 984, 1285),
            Entry("unit.archer", "epic", "ART-CHAR-C04-MILA-EPIC-TIDEMARSH-VESTMENT-v01", 221, 24, 1014, 1271),
            Entry("unit.archer", "legendary", "ART-CHAR-C04-MILA-LEGENDARY-TIDEMARSH-MASTER-v01", 210, 22, 1053, 1275),
            Entry("unit.starblade", "common", "ART-CHAR-C05-STONEWARD-COMMON-v01", 86, 43, 1125, 1283),
            Entry("unit.starblade", "uncommon", "ART-CHAR-C05-STONEWARD-UNCOMMON-v01", 96, 43, 1150, 1276),
            Entry("unit.starblade", "rare", "ART-CHAR-C05-STONEWARD-RARE-v01", 99, 12, 1160, 1279),
            Entry("unit.starblade", "epic", "ART-CHAR-C05-BRUN-EPIC-FORGEGATE-MASTER-v03", 39, 11, 1174, 1280),
            Entry("unit.starblade", "legendary", "ART-CHAR-C05-BRUN-LEGENDARY-EVERBRIDGE-FORGE-LORD-v03", 42, 11, 1169, 1279),
            Entry("unit.starpriest", "common", "ART-CHAR-C06-EMBERHAND-COMMON-v01", 258, 0, 953, 1282),
            Entry("unit.starpriest", "uncommon", "ART-CHAR-C06-EMBERHAND-UNCOMMON-v01", 255, 0, 960, 1283),
            Entry("unit.starpriest", "rare", "ART-CHAR-C06-EMBERHAND-RARE-v01", 263, 0, 987, 1283),
            Entry("unit.starpriest", "epic", "ART-CHAR-C06-NOCK-EPIC-RIFTSTONE-ARENA-KILNMASTER-v01", 250, 18, 990, 1284),
            Entry("unit.starpriest", "legendary", "ART-CHAR-C06-NOCK-LEGENDARY-HUNDRED-FIRE-STEAMFORGED-v01", 244, 17, 988, 1276),
        };

        private static readonly Dictionary<string, CharacterRarityArtEntry> byDefinitionAndQuality =
            BuildRarityIndex();

        public static IReadOnlyList<CharacterRarityArtEntry> Entries { get { return entries; } }

        public static bool TryGet(string definitionId, string qualityId, out CharacterRarityArtEntry entry)
        {
            entry = null;
            return !string.IsNullOrEmpty(definitionId) && !string.IsNullOrEmpty(qualityId) &&
                   byDefinitionAndQuality.TryGetValue(RarityKey(definitionId, qualityId), out entry);
        }

        private static CharacterRarityArtEntry Entry(
            string definitionId, string qualityId, string resourceId,
            int minX, int minY, int maxX, int maxY)
        {
            return new CharacterRarityArtEntry(
                definitionId, qualityId, resourceId, minX, minY, maxX, maxY);
        }

        private static Dictionary<string, CharacterRarityArtEntry> BuildRarityIndex()
        {
            var result = new Dictionary<string, CharacterRarityArtEntry>(StringComparer.Ordinal);
            foreach (var entry in entries)
                result.Add(RarityKey(entry.DefinitionId, entry.QualityId), entry);
            return result;
        }

        private static string RarityKey(string definitionId, string qualityId)
        {
            return definitionId + "|" + qualityId;
        }
    }
}
