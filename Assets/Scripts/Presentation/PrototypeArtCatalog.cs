using System;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype.Presentation
{
    internal enum LayeredBackgroundKind
    {
        Home,
        Battle,
        Map
    }

    internal enum BackgroundLayerRole
    {
        Base,
        HomeSeaChart,
        HomeCloudFar,
        HomeBeamOverlay,
        HomeChapterIsland,
        HomeFlagSheet,
        HomeCloudNear,
        LighthouseGlow,
        Beam,
        RouteReflection,
        ForegroundFog,
        FarSea,
        IslandGround,
        DeploymentMarkers,
        ForegroundDock,
        MapSeaBase,
        MapDistantIslands
    }

    internal enum ExpeditionMapNodeVisualState
    {
        Current,
        Available,
        Future,
        Completed
    }

    internal static class PrototypeArtResourceIds
    {
        public const string HomeSeaChartBackground = "ART-HOME-SEA-CHART-BG-04";
        public const string HomeChapterOneIsland = "ART-HOME-CH01-LIGHTHOUSE-ISLAND-04";
        public const string HomeCloudFar = "ART-HOME-CLOUD-FAR-02";
        public const string HomeCloudNear = "ART-HOME-CLOUD-NEAR-02";
        public const string HomeLighthouseBeamOverlay = "ART-HOME-LIGHTHOUSE-BEAM-02";
        public const string HomeLighthouseFlagSheet = "ART-HOME-LIGHTHOUSE-FLAG-4F-02";
        public const string HomeStatusCard = "ART-UI-HOME-STATUS-CARD-01";
        public const string HomeChapterMarker = "ART-UI-HOME-CHAPTER-MARKER-01";
        public const string HomeTopFrame = "ART-UI-HOME-TOP-FRAME-01";
        public const string HomeAffairsIcon = "ART-ICON-HOME-AFFAIRS-01";
        public const string HomeHarborIcon = "ART-ICON-HOME-HARBOR-01";
        public const string HomeMaterialStoneIcon = "ART-ICON-HOME-MATERIAL-STONE-01";
        public const string HomeMaterialOilIcon = "ART-ICON-HOME-MATERIAL-OIL-01";
        public const string HomeMaterialLensIcon = "ART-ICON-HOME-MATERIAL-LENS-01";
        public const string HomePrimaryNormal = "ART-UI-HOME-PRIMARY-NORMAL-01";
        public const string HomePrimaryPressed = "ART-UI-HOME-PRIMARY-PRESSED-01";
        public const string HomePrimaryDisabled = "ART-UI-HOME-PRIMARY-DISABLED-01";
        public const string HomeBottomDock = "ART-UI-HOME-BOTTOM-DOCK-01";
        public const string HomeNavActive = "ART-UI-HOME-NAV-ACTIVE-01";
        public const string HomeNavShopIcon = "ART-ICON-HOME-NAV-SHOP-01";
        public const string HomeNavDeckIcon = "ART-ICON-HOME-NAV-DECK-01";
        public const string HomeNavHomeIcon = "ART-ICON-HOME-NAV-HOME-01";
        public const string HomeNavPvpIcon = "ART-ICON-HOME-NAV-PVP-01";
        public const string HomeNavRankIcon = "ART-ICON-HOME-NAV-RANK-01";
        public const string HomeBase = "ART-HOME-WHITE-TIDE-BASE-01";
        public const string HomeLighthouseGlow = "ART-HOME-WHITE-TIDE-LIGHTHOUSE-GLOW-01";
        public const string HomeBeam = "ART-HOME-WHITE-TIDE-BEAM-01";
        public const string HomeRouteReflection = "ART-HOME-WHITE-TIDE-ROUTE-REFLECTION-01";
        public const string HomeForegroundFog = "ART-HOME-WHITE-TIDE-FOREGROUND-FOG-01";

        public const string HomeV38StaticBase = "ART-UI-HOME-STATIC-NO-DYNAMIC-TEXT-1080x2400-01";
        public const string HomeV38StaticBaseEnglish = "localized/en/ART-UI-HOME-STATIC-EN-1080x2400-01";
        public const string HomeV38ExpeditionNormal = "buttons/ART-UI-HOME-EXPEDITION-NORMAL-NO-TEXT-01";
        public const string HomeV38ExpeditionContinue = "buttons/ART-UI-HOME-EXPEDITION-CONTINUE-NO-TEXT-01";
        public const string HomeV38ExpeditionFreeNoReward = "buttons/ART-UI-HOME-EXPEDITION-FREE-NO-REWARD-NO-TEXT-01";
        public const string HomeV38ExpeditionDisabled = "buttons/ART-UI-HOME-EXPEDITION-DISABLED-NO-TEXT-01";
        public const string HomeV38BottomNavShop = "bottom-nav/ART-UI-HOME-BOTTOM-NAV-SHOP-ACTIVE-01";
        public const string HomeV38BottomNavDeck = "bottom-nav/ART-UI-HOME-BOTTOM-NAV-DECK-ACTIVE-01";
        public const string HomeV38BottomNavHome = "bottom-nav/ART-UI-HOME-BOTTOM-NAV-HOME-ACTIVE-01";
        public const string HomeV38BottomNavPvp = "bottom-nav/ART-UI-HOME-BOTTOM-NAV-PVP-ACTIVE-01";
        public const string HomeV38BottomNavHarbor = "bottom-nav/ART-UI-HOME-BOTTOM-NAV-HARBOR-ACTIVE-01";
        public const string HomeV38BottomNavShopEnglish = "localized/en/bottom-nav/ART-UI-HOME-BOTTOM-NAV-SHOP-ACTIVE-EN-01";
        public const string HomeV38BottomNavDeckEnglish = "localized/en/bottom-nav/ART-UI-HOME-BOTTOM-NAV-DECK-ACTIVE-EN-01";
        public const string HomeV38BottomNavHomeEnglish = "localized/en/bottom-nav/ART-UI-HOME-BOTTOM-NAV-HOME-ACTIVE-EN-01";
        public const string HomeV38BottomNavPvpEnglish = "localized/en/bottom-nav/ART-UI-HOME-BOTTOM-NAV-PVP-ACTIVE-EN-01";
        public const string HomeV38BottomNavHarborEnglish = "localized/en/bottom-nav/ART-UI-HOME-BOTTOM-NAV-HARBOR-ACTIVE-EN-01";

        public const string BattleAbandonedSignalYardReadability =
            "ART-BATTLE-CHAPTER-01-ABANDONED-SIGNAL-YARD-READABILITY-02";

        public const string MapSeaBase = "ART-MAP-WHITE-TIDE-SEA-BASE-01";
        public const string MapDistantIslands = "ART-MAP-WHITE-TIDE-DISTANT-ISLANDS-01";

        public const string MapSeaTile = "ART-MAP-WHITE-TIDE-SEA-TILE-02";
        public const string MapReefCluster = "ART-MAP-WHITE-TIDE-REEF-CLUSTER-02";
        public const string MapReefRing = "ART-MAP-WHITE-TIDE-REEF-RING-02";
        public const string MapNodeNormal = "ART-MAP-NODE-BATTLE-NORMAL-02";
        public const string MapNodeElite = "ART-MAP-NODE-BATTLE-ELITE-02";
        public const string MapNodeHighRisk = "ART-MAP-NODE-BATTLE-HIGH-RISK-02";
        public const string MapNodeEvent = "ART-MAP-NODE-EVENT-02";
        public const string MapNodeShop = "ART-MAP-NODE-SHOP-02";
        public const string MapNodeRest = "ART-MAP-NODE-REST-02";
        public const string MapNodeTreasure = "ART-MAP-NODE-TREASURE-02";
        public const string MapNodeBoss = "ART-MAP-NODE-BOSS-02";
        public const string MapRingCurrent = "ART-MAP-NODE-RING-CURRENT-02";
        public const string MapRingAvailable = "ART-MAP-NODE-RING-AVAILABLE-02";
        public const string MapRingFuture = "ART-MAP-NODE-RING-FUTURE-02";
        public const string MapRingCompleted = "ART-MAP-NODE-RING-COMPLETED-02";
        public const string MapRingElite = "ART-MAP-NODE-RING-ELITE-02";
        public const string MapRingBoss = "ART-MAP-NODE-RING-BOSS-02";

        public const string LanternShieldGuard = "ART-CHAR-C01-LAMPGUARD-COMMON-v02";
        public const string LanternBearer = "ART-CHAR-C02-LAMPLADY-COMMON-v01";
        public const string PathHunter = "ART-CHAR-C03-RANGER-COMMON-v03";
        public const string MossHerbalist = "ART-CHAR-C04-HERBALIST-COMMON-v01";
        public const string RockHammerGuard = "ART-CHAR-C05-STONEWARD-COMMON-v01";
        public const string EmberThrower = "ART-CHAR-C06-EMBERHAND-COMMON-v01";

        public const string GateboardGuard = "ART-MONSTER-CH01-N1-GATEBOARD-GUARD-01";
        public const string CableEatingCrab = "ART-MONSTER-CH01-N2-CABLE-EATING-CRAB-01";
        public const string ReefLookout = "ART-MONSTER-CH01-N3-REEF-LOOKOUT-01";
        public const string MistLeapingRay = "ART-MONSTER-CH01-N4-MIST-LEAPING-RAY-01";
        public const string Sailmender = "ART-MONSTER-CH01-N5-SAILMENDER-01";
        public const string BreakwaterShellbeast = "ART-MONSTER-CH01-E1-BREAKWATER-SHELLBEAST-01";
        public const string TrailHunter = "ART-MONSTER-CH01-E2-TRAIL-HUNTER-01";
        public const string EchoBellkeeper = "ART-MONSTER-CH01-E3-ECHO-BELLKEEPER-02";
        public const string KeeperOfNoReturn = "ART-MONSTER-CH01-B1-KEEPER-OF-NO-RETURN-01";
        public const string MistShepherd = "ART-MONSTER-CH01-B2-MIST-SHEPHERD-01";

        public const string FormationBattleV94Pause = "icons/ART-UI-BATTLE-PAUSE-GLYPH-64-V9.4";
        public const string FormationBattleV94SynergyTile = "frames/ART-UI-BATTLE-SYNERGY-TILE-SQUARE-84-V9.4";
        public const string FormationBattleV94BuffTile = "frames/ART-UI-BATTLE-BUFF-TILE-SQUARE-84-V9.4";
        public const string FormationBattleV94CountBadge = "frames/ART-UI-BATTLE-COUNT-BADGE-32-V9.4";
        public const string FormationBattleV94BackFrame = "shared/frame-back-circle-94";
        public const string FormationBattleV94BackGlyph = "shared/action-back-optical-40";
        public const string FormationBattleV941UtilityButton =
            "v941/frames/ART-UI-FORMATION-UTILITY-BUTTON-9SLICE-180x108-V9.4.1";
        public const string FormationBattleV941StartButton =
            "v941/frames/ART-UI-FORMATION-START-BUTTON-9SLICE-580x132-V9.4.1";
        public const string FormationBattleV941GroundEmpty =
            "v941/markers/ART-UI-FORMATION-GROUND-SLOT-EMPTY-200x112-V9.4.1";
        public const string FormationBattleV941GroundOccupied =
            "v941/markers/ART-UI-FORMATION-GROUND-SLOT-OCCUPIED-200x112-V9.4.1";
        public const string FormationBattleV942Nameplate =
            "v942/frames/ART-UI-BATTLE-UNIT-NAMEPLATE-POINTED-9SLICE-320x64-V9.4.2";
        public const string FormationBattleV942RecycleIcon =
            "v942/icons/ART-UI-FORMATION-RECYCLE-ICON-96-V9.4.2";
        public const string FormationBattleV942ShopIcon =
            "v942/icons/ART-UI-FORMATION-SHOP-ICON-96-V9.4.2";
        public const string FormationBattleV944BackpackIcon =
            "v944/icons/ART-UI-FORMATION-BACKPACK-ICON-96-V9.4.4";
        public const string BattleStatusBarV102Track =
            "v102/frames/ART-UI-BATTLE-STATUS-BAR-TRACK-720x44-V1.0.2";
        public const string BattleStatusBarV102HealthFill =
            "v102/frames/ART-UI-BATTLE-STATUS-BAR-HEALTH-FILL-640x37-V1.0.2";
        public const string BattleStatusBarV102DamageLagFill =
            "v102/frames/ART-UI-BATTLE-STATUS-BAR-DAMAGE-LAG-FILL-640x37-V1.0.2";
        public const string BattleStatusBarV102ShieldRail =
            "v102/frames/ART-UI-BATTLE-STATUS-BAR-SHIELD-RAIL-640x14-V1.0.2";
        public const string BattleStatusBarV102CapacityNotch =
            "v102/frames/ART-UI-BATTLE-STATUS-BAR-CAPACITY-NOTCH-8x64-V1.0.2";
        public const string BattleStatusBarV102ShieldIcon =
            "v102/frames/ART-UI-BATTLE-STATUS-BAR-SHIELD-ICON-96x129-V1.0.2";

        public const string DetailTideWindowShell = "frames/detail-tide-window-shell-880x1360";
        public const string DetailMetadataUnderline = "ornaments/detail-meta-underline-230x18";
        public const string DetailTagPillRed = "frames/detail-tag-pill-red-190x76";
        public const string DetailTagPillPurple = "frames/detail-tag-pill-purple-190x76";
        public const string DetailTagPillTeal = "frames/detail-tag-pill-teal-190x76";
        public const string DetailCloseOrnament = "icons/detail-close-x-ornament-48";
        public const string DetailStatHealth = "icons/detail-stat-health-64";
        public const string DetailStatAttack = "icons/detail-stat-attack-64";
        public const string DetailStatArmor = "icons/detail-stat-armor-64";
        public const string DetailStatSpeed = "icons/detail-stat-speed-64";


        public const string LanternHelm = "ART-EQUIP-HEAD-LANTERN-HELM-COMMON-01";
        public const string OldVoyageClothes = "ART-EQUIP-BODY-OLD-VOYAGE-CLOTHES-COMMON-01";
        public const string MossPathShoes = "ART-EQUIP-FEET-MOSS-PATH-SHOES-COMMON-01";
        public const string ForgeRivetGauntlet = "ART-EQUIP-HANDS-FORGE-RIVET-GAUNTLET-COMMON-01";
        public const string LanternHarborBlade = "ART-EQUIP-WEAPON-LANTERN-HARBOR-BLADE-COMMON-01";

        public const string EquipmentCardFrameCommon = "ART-UI-EQUIP-CARD-FRAME-COMMON-01";
        public const string EquipmentCardFrameUncommon = "ART-UI-EQUIP-CARD-FRAME-UNCOMMON-01";
        public const string EquipmentCardFrameRare = "ART-UI-EQUIP-CARD-FRAME-RARE-01";
        public const string EquipmentCardFrameEpic = "ART-UI-EQUIP-CARD-FRAME-EPIC-01";
        public const string EquipmentCardFrameLegendary = "ART-UI-EQUIP-CARD-FRAME-LEGENDARY-01";
        public const string EquipmentDetailBackgroundCommon = "ART-UI-EQUIP-DETAIL-BG-COMMON-01";
        public const string EquipmentDetailBackgroundUncommon = "ART-UI-EQUIP-DETAIL-BG-UNCOMMON-01";
        public const string EquipmentDetailBackgroundRare = "ART-UI-EQUIP-DETAIL-BG-RARE-01";
        public const string EquipmentDetailBackgroundEpic = "ART-UI-EQUIP-DETAIL-BG-EPIC-01";
        public const string EquipmentDetailBackgroundLegendary = "ART-UI-EQUIP-DETAIL-BG-LEGENDARY-01";
    }

    internal sealed class BackgroundLayerDefinition
    {
        public readonly BackgroundLayerRole Role;
        public readonly string ResourceId;
        public readonly string ResourcesPath;

        public BackgroundLayerDefinition(BackgroundLayerRole role, string resourceId, string resourcesPath)
        {
            Role = role;
            ResourceId = resourceId;
            ResourcesPath = resourcesPath;
        }
    }

    /// <summary>
    /// Stable interface between art delivery and the prototype renderer. Each delivered
    /// full-frame transparent layer can replace its procedural fallback independently.
    /// </summary>
    internal interface ILayeredBackgroundSource
    {
        IReadOnlyList<BackgroundLayerDefinition> GetLayers(LayeredBackgroundKind kind);
        Texture2D LoadLayer(BackgroundLayerDefinition layer);
    }

    internal sealed class ResourcesLayeredBackgroundSource : ILayeredBackgroundSource
    {
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private readonly BackgroundLayerDefinition[] homeLayers =
        {
            Layer(BackgroundLayerRole.HomeSeaChart, PrototypeArtResourceIds.HomeSeaChartBackground, "Art/Home"),
            Layer(BackgroundLayerRole.HomeCloudFar, PrototypeArtResourceIds.HomeCloudFar, "Art/Home"),
            Layer(BackgroundLayerRole.HomeBeamOverlay, PrototypeArtResourceIds.HomeLighthouseBeamOverlay, "Art/Home"),
            Layer(BackgroundLayerRole.HomeChapterIsland, PrototypeArtResourceIds.HomeChapterOneIsland, "Art/Home"),
            Layer(BackgroundLayerRole.HomeFlagSheet, PrototypeArtResourceIds.HomeLighthouseFlagSheet, "Art/Home"),
            Layer(BackgroundLayerRole.HomeCloudNear, PrototypeArtResourceIds.HomeCloudNear, "Art/Home")
        };

        private readonly BackgroundLayerDefinition[] battleLayers =
        {
            Layer(BackgroundLayerRole.Base,
                PrototypeArtResourceIds.BattleAbandonedSignalYardReadability, "Art/Battle")
        };

        private readonly BackgroundLayerDefinition[] mapLayers =
        {
            Layer(BackgroundLayerRole.MapSeaBase, PrototypeArtResourceIds.MapSeaBase, "Art/Map"),
            Layer(BackgroundLayerRole.MapDistantIslands, PrototypeArtResourceIds.MapDistantIslands, "Art/Map")
        };

        public IReadOnlyList<BackgroundLayerDefinition> GetLayers(LayeredBackgroundKind kind)
        {
            switch (kind)
            {
                case LayeredBackgroundKind.Home: return homeLayers;
                case LayeredBackgroundKind.Map: return mapLayers;
                default: return battleLayers;
            }
        }

        public Texture2D LoadLayer(BackgroundLayerDefinition layer)
        {
            Texture2D texture;
            if (textureCache.TryGetValue(layer.ResourceId, out texture)) return texture;
            texture = Resources.Load<Texture2D>(layer.ResourcesPath);
            textureCache[layer.ResourceId] = texture;
            return texture;
        }

        private static BackgroundLayerDefinition Layer(
            BackgroundLayerRole role, string resourceId, string parentPath)
        {
            return new BackgroundLayerDefinition(role, resourceId, parentPath + "/" + resourceId);
        }
    }

    internal sealed class CharacterArtDefinition
    {
        public readonly string UnitDefinitionId;
        public readonly string ResourceId;
        public readonly string ResourcesPath;
        public readonly Vector2 UntrimmedPivot;
        public readonly Rect ContentBoundsTopOrigin;
        public readonly float VisualScaleMultiplier;
        public readonly float HudBodyHeightMultiplier;
        public readonly float VisualYOffsetRelativeToUnitHeight;

        public CharacterArtDefinition(
            string unitDefinitionId,
            string resourceId,
            string resourcesPath,
            Vector2 untrimmedPivot,
            Rect contentBoundsTopOrigin,
            float visualScaleMultiplier = 1f,
            float hudBodyHeightMultiplier = 1f,
            float visualYOffsetRelativeToUnitHeight = 0f)
        {
            UnitDefinitionId = unitDefinitionId;
            ResourceId = resourceId;
            ResourcesPath = resourcesPath;
            UntrimmedPivot = untrimmedPivot;
            ContentBoundsTopOrigin = contentBoundsTopOrigin;
            VisualScaleMultiplier = Mathf.Max(0.01f, visualScaleMultiplier);
            HudBodyHeightMultiplier = Mathf.Max(0.01f, hudBodyHeightMultiplier);
            VisualYOffsetRelativeToUnitHeight = visualYOffsetRelativeToUnitHeight;
        }

        public Rect GetVisibleContentRect(Rect textureCanvasRect, bool flipX = false)
        {
            var normalizedX = flipX
                ? 1f - ContentBoundsTopOrigin.xMax
                : ContentBoundsTopOrigin.x;
            return new Rect(
                textureCanvasRect.x + textureCanvasRect.width * normalizedX,
                textureCanvasRect.y + textureCanvasRect.height * ContentBoundsTopOrigin.y,
                textureCanvasRect.width * ContentBoundsTopOrigin.width,
                textureCanvasRect.height * ContentBoundsTopOrigin.height);
        }
    }

    internal sealed class PrototypeArtCatalog
    {
        private readonly Dictionary<string, CharacterArtDefinition> characterDefinitions =
            new Dictionary<string, CharacterArtDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, CharacterArtDefinition> rarityCharacterDefinitions =
            new Dictionary<string, CharacterArtDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> characterSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> formalBattleAnimationSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> pieceBaseSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> characterAvatarResourceIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> characterQualityAvatarResourceIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> characterAvatarSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> characterSlotAvatarSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> equipmentIconResourceIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> equipmentIconSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> equipmentCardFrameResourceIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> equipmentDetailBackgroundResourceIds =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, Sprite> equipmentUiSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> homeUiTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private readonly Dictionary<string, Texture2D> expeditionUiV46Textures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);

        public PrototypeArtCatalog()
        {
            // The formal chapter-one pack uses one untrimmed canvas and pivot for all
            // sixteen units. The only visual-size exception is the tier-wide elite scale.
            foreach (var entry in StaticBattleArtContract.Entries) Register(entry);
            foreach (var entry in CharacterRarityArtContract.Entries) Register(entry);

            foreach (var entry in AvatarArtContract.Entries)
                RegisterAvatar(entry.DefinitionId, entry.QualityId, entry.ResourceId);

            RegisterEquipmentIcon("equipment.lanternHelm", PrototypeArtResourceIds.LanternHelm);
            RegisterEquipmentIcon("equipment.oldVoyageClothes", PrototypeArtResourceIds.OldVoyageClothes);
            RegisterEquipmentIcon("equipment.mossPathShoes", PrototypeArtResourceIds.MossPathShoes);
            RegisterEquipmentIcon("equipment.forgeRivetGauntlet", PrototypeArtResourceIds.ForgeRivetGauntlet);
            RegisterEquipmentIcon("equipment.lanternHarborBlade", PrototypeArtResourceIds.LanternHarborBlade);

            RegisterEquipmentRarityArt("common", PrototypeArtResourceIds.EquipmentCardFrameCommon,
                PrototypeArtResourceIds.EquipmentDetailBackgroundCommon);
            RegisterEquipmentRarityArt("uncommon", PrototypeArtResourceIds.EquipmentCardFrameUncommon,
                PrototypeArtResourceIds.EquipmentDetailBackgroundUncommon);
            RegisterEquipmentRarityArt("rare", PrototypeArtResourceIds.EquipmentCardFrameRare,
                PrototypeArtResourceIds.EquipmentDetailBackgroundRare);
            RegisterEquipmentRarityArt("epic", PrototypeArtResourceIds.EquipmentCardFrameEpic,
                PrototypeArtResourceIds.EquipmentDetailBackgroundEpic);
            RegisterEquipmentRarityArt("legendary", PrototypeArtResourceIds.EquipmentCardFrameLegendary,
                PrototypeArtResourceIds.EquipmentDetailBackgroundLegendary);
        }

        private void Register(StaticBattleArtEntry entry)
        {
            var definition = new CharacterArtDefinition(
                entry.DefinitionId,
                entry.ResourceId,
                StaticBattleArtContract.ResourcesFolder + entry.ResourceId,
                new Vector2(StaticBattleArtContract.PivotX, StaticBattleArtContract.PivotY),
                new Rect(
                    entry.MinX / (float)StaticBattleArtContract.CanvasWidth,
                    entry.MinY / (float)StaticBattleArtContract.CanvasHeight,
                    (entry.MaxX - entry.MinX + 1f) / StaticBattleArtContract.CanvasWidth,
                    (entry.MaxY - entry.MinY + 1f) / StaticBattleArtContract.CanvasHeight),
                entry.PresentationScale,
                1f,
                entry.VisualYOffsetRelativeToUnitHeight);
            characterDefinitions[definition.UnitDefinitionId] = definition;
        }

        private void Register(CharacterRarityArtEntry entry)
        {
            var definition = new CharacterArtDefinition(
                entry.DefinitionId,
                entry.ResourceId,
                StaticBattleArtContract.ResourcesFolder + entry.ResourceId,
                new Vector2(StaticBattleArtContract.PivotX, StaticBattleArtContract.PivotY),
                new Rect(
                    entry.MinX / (float)StaticBattleArtContract.CanvasWidth,
                    entry.MinY / (float)StaticBattleArtContract.CanvasHeight,
                    (entry.MaxX - entry.MinX + 1f) / StaticBattleArtContract.CanvasWidth,
                    (entry.MaxY - entry.MinY + 1f) / StaticBattleArtContract.CanvasHeight),
                1f,
                1f);
            rarityCharacterDefinitions[CharacterRarityKey(entry.DefinitionId, entry.QualityId)] = definition;
        }

        public bool TryGetCharacterArt(
            string unitDefinitionId, out CharacterArtDefinition definition, out Sprite sprite)
        {
            sprite = null;
            if (!characterDefinitions.TryGetValue(unitDefinitionId, out definition)) return false;
            if (!characterSprites.TryGetValue(definition.ResourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>(definition.ResourcesPath);
                characterSprites[definition.ResourceId] = sprite;
            }
            return sprite != null;
        }

        public bool TryGetCharacterArt(
            string unitDefinitionId, string qualityId,
            out CharacterArtDefinition definition, out Sprite sprite)
        {
            sprite = null;
            if (string.IsNullOrEmpty(unitDefinitionId) || string.IsNullOrEmpty(qualityId) ||
                !rarityCharacterDefinitions.TryGetValue(
                    CharacterRarityKey(unitDefinitionId, qualityId), out definition))
                return TryGetCharacterArt(unitDefinitionId, out definition, out sprite);

            if (!characterSprites.TryGetValue(definition.ResourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>(definition.ResourcesPath);
                characterSprites[definition.ResourceId] = sprite;
            }
            return sprite != null;
        }

        public bool TryGetFormalBattleAnimationFrame(
            string unitDefinitionId, string clipId, int elapsedMs, out Sprite sprite)
        {
            sprite = null;
            FormalBattleAnimationClip clip;
            if (!FormalBattleAnimationContract.TryGetClip(unitDefinitionId, clipId, out clip)) return false;
            var frame = clip.FrameAt(elapsedMs);
            if (!formalBattleAnimationSprites.TryGetValue(frame.ResourcePath, out sprite))
            {
                sprite = Resources.Load<Sprite>(frame.ResourcePath);
                formalBattleAnimationSprites[frame.ResourcePath] = sprite;
            }
            return sprite != null;
        }

        public bool TryGetPieceBase(
            string baseSkinId, out PieceBaseSkinDefinition definition, out Sprite sprite)
        {
            sprite = null;
            if (!PieceBasePresentationContract.TryGetSkin(baseSkinId, out definition)) return false;
            if (!pieceBaseSprites.TryGetValue(definition.ResourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>(definition.ResourcesPath);
                pieceBaseSprites[definition.ResourceId] = sprite;
            }
            return sprite != null;
        }

        private static string CharacterRarityKey(string unitDefinitionId, string qualityId)
        {
            return unitDefinitionId + "|" + qualityId;
        }

        private void RegisterAvatar(string unitDefinitionId, string qualityId, string resourceId)
        {
            characterQualityAvatarResourceIds[CharacterRarityKey(unitDefinitionId, qualityId)] = resourceId;
            if (string.Equals(qualityId, "common", StringComparison.Ordinal))
                characterAvatarResourceIds[unitDefinitionId] = resourceId;
        }

        public bool TryGetCharacterAvatar(string unitDefinitionId, out Sprite sprite)
        {
            return TryGetCharacterAvatar(unitDefinitionId, "common", out sprite);
        }

        public bool TryGetCharacterAvatar(string unitDefinitionId, string qualityId, out Sprite sprite)
        {
            sprite = null;
            string resourceId;
            if (!characterQualityAvatarResourceIds.TryGetValue(
                    CharacterRarityKey(unitDefinitionId, qualityId), out resourceId))
                return TryGetCharacterAvatar(unitDefinitionId, out sprite);
            if (!characterAvatarSprites.TryGetValue(resourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>(AvatarArtContract.ResourcesFolder + resourceId);
                characterAvatarSprites[resourceId] = sprite;
            }
            return sprite != null;
        }

        public bool TryGetCharacterSlotAvatar(string unitDefinitionId, out Sprite sprite)
        {
            return TryGetCharacterSlotAvatar(unitDefinitionId, "common", out sprite);
        }

        public bool TryGetCharacterSlotAvatar(string unitDefinitionId, string qualityId, out Sprite sprite)
        {
            sprite = null;
            string resourceId;
            if (!characterQualityAvatarResourceIds.TryGetValue(
                    CharacterRarityKey(unitDefinitionId, qualityId), out resourceId))
            {
                if (!characterAvatarResourceIds.TryGetValue(unitDefinitionId, out resourceId)) return false;
            }
            var slotResourceId = resourceId + AvatarArtContract.SlotResourceSuffix;
            if (!characterSlotAvatarSprites.TryGetValue(slotResourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>(AvatarArtContract.SlotResourcesFolder + slotResourceId);
                characterSlotAvatarSprites[slotResourceId] = sprite;
            }
            return sprite != null;
        }

        private void RegisterEquipmentIcon(string templateId, string resourceId)
        {
            equipmentIconResourceIds[templateId] = resourceId;
        }

        public bool TryGetEquipmentIcon(string templateId, out Sprite sprite)
        {
            sprite = null;
            string resourceId;
            if (!equipmentIconResourceIds.TryGetValue(templateId, out resourceId)) return false;
            if (!equipmentIconSprites.TryGetValue(resourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>("Art/Equipment/" + resourceId);
                equipmentIconSprites[resourceId] = sprite;
            }
            return sprite != null;
        }

        private void RegisterEquipmentRarityArt(string rarityId, string cardFrameId, string detailBackgroundId)
        {
            equipmentCardFrameResourceIds[rarityId] = cardFrameId;
            equipmentDetailBackgroundResourceIds[rarityId] = detailBackgroundId;
        }

        public bool TryGetEquipmentCardFrame(string rarityId, out Sprite sprite)
        {
            return TryGetEquipmentUiSprite(
                equipmentCardFrameResourceIds, rarityId, "Art/EquipmentUI/CardFrames/", out sprite);
        }

        public bool TryGetEquipmentDetailBackground(string rarityId, out Sprite sprite)
        {
            return TryGetEquipmentUiSprite(
                equipmentDetailBackgroundResourceIds, rarityId, "Art/EquipmentUI/DetailBackgrounds/", out sprite);
        }

        public Texture2D GetHomeUiTexture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            Texture2D texture;
            if (homeUiTextures.TryGetValue(resourceId, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/Home/" + resourceId);
            homeUiTextures[resourceId] = texture;
            return texture;
        }

        public Texture2D GetHomeV38Texture(string resourceId)
        {
            var cacheKey = "v38/" + resourceId;
            Texture2D texture;
            if (homeUiTextures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/HomeV38/" + resourceId);
            homeUiTextures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV46Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(resourceId, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V47/" + resourceId);
            if (texture == null)
                texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V46/" + resourceId);
            expeditionUiV46Textures[resourceId] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV52Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "v52/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V52/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV53Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "v53/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V53/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV55Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "v55/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V55/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV62Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "v62/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V62/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV63Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "v63/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V63/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiV67Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "v67/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/V67/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionUiSharedTexture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "shared/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/ExpeditionUI/Shared/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetExpeditionEventFamilyTexture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "event-family-v126/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/UI/Shared/ExpeditionV2/" + resourceId);
            if (texture != null && resourceId.IndexOf("/patterns/", StringComparison.Ordinal) >= 0)
                texture.wrapMode = TextureWrapMode.Clamp;
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetFormationBattleV94Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "formation-battle-v94/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/FormationBattle/V94/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetFormationBattleV95Texture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "formation-battle-v95/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/FormationBattle/V95/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetUnitLevelBadgeTexture(string qualityId, int runLevel)
        {
            var safeQualityId = UnitLevelBadgeQualityId(qualityId);
            var safeRunLevel = Mathf.Clamp(runLevel, 1, RunUnitRules.MaximumRunLevel);
            var resourceId = "unit-level-badge-b-" + safeQualityId + "-lv" + safeRunLevel;
            var cacheKey = "unit-level-badge-v111/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>(
                "Art/FormationBattle/V111/level-badges/2x/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        public Texture2D GetDetailTideWindowTexture(string resourceId)
        {
            if (string.IsNullOrEmpty(resourceId)) return null;
            var cacheKey = "detail-v114/" + resourceId;
            Texture2D texture;
            if (expeditionUiV46Textures.TryGetValue(cacheKey, out texture)) return texture;
            texture = Resources.Load<Texture2D>("Art/DetailModal/V114/" + resourceId);
            expeditionUiV46Textures[cacheKey] = texture;
            return texture;
        }

        private static string UnitLevelBadgeQualityId(string qualityId)
        {
            switch (qualityId)
            {
                case "uncommon":
                case "rare":
                case "epic":
                case "legendary":
                    return qualityId;
                default:
                    return "common";
            }
        }

        private bool TryGetEquipmentUiSprite(
            Dictionary<string, string> resourceIds, string rarityId, string parentPath, out Sprite sprite)
        {
            sprite = null;
            string resourceId;
            if (!resourceIds.TryGetValue(rarityId, out resourceId)) return false;
            if (!equipmentUiSprites.TryGetValue(resourceId, out sprite))
            {
                sprite = Resources.Load<Sprite>(parentPath + resourceId);
                equipmentUiSprites[resourceId] = sprite;
            }
            return sprite != null;
        }
    }

    internal static class ExpeditionUiV46Layout
    {
        public const float ReferenceWidth = 841f;
        public const float UpgradeReferenceHeight = 1871f;
        public const float PreparationReferenceHeight = 1870f;

        public static readonly Rect UpgradeBack = new Rect(426f, 23f, 164f, 56f);
        public static readonly Rect UpgradeAbandon = new Rect(640f, 23f, 163f, 56f);
        public static readonly Rect UpgradeRound = new Rect(49f, 153f, 229f, 59f);
        public static readonly Rect UpgradeCoins = new Rect(302f, 153f, 228f, 59f);
        public static readonly Rect UpgradeLife = new Rect(560f, 153f, 229f, 59f);
        public static readonly Rect UpgradeTitle = new Rect(196f, 482f, 449f, 68f);
        public static readonly Rect UpgradeSubtitle = new Rect(198f, 575f, 445f, 41f);
        public static readonly Rect UpgradeFooterRound = new Rect(330f, 1451f, 181f, 36f);
        public static readonly Rect[] UpgradeCards =
        {
            new Rect(61f, 673f, 723f, 228f),
            new Rect(61f, 930f, 723f, 228f),
            new Rect(61f, 1187f, 723f, 228f)
        };

        public static readonly Rect PreparationBack = new Rect(31f, 51f, 92f, 100f);
        public static readonly Rect PreparationAbandon = new Rect(645f, 119f, 159f, 51f);
        public static readonly Rect PreparationRound = new Rect(40f, 196f, 249f, 57f);
        public static readonly Rect PreparationCoins = new Rect(302f, 196f, 237f, 57f);
        public static readonly Rect PreparationLife = new Rect(554f, 196f, 246f, 57f);
        public static readonly Rect PreparationLineupTitle = new Rect(70f, 291f, 355f, 47f);
        public static readonly Vector2[] PreparationLineupSlotCenters =
        {
            new Vector2(115f, 416f), new Vector2(245f, 416f), new Vector2(377f, 416f),
            new Vector2(505f, 416f), new Vector2(620f, 416f), new Vector2(735f, 416f)
        };
        public const float PreparationLineupSlotDiameter = 107f;
        public static readonly Rect PreparationShopTitle = new Rect(157f, 610f, 355f, 63f);
        public static readonly Rect PreparationRefresh = new Rect(610f, 619f, 171f, 57f);
        public static readonly Rect[] PreparationOffers =
        {
            new Rect(42f, 727f, 757f, 207f),
            new Rect(42f, 945f, 757f, 212f),
            new Rect(42f, 1176f, 757f, 210f)
        };
        public static readonly Rect PreparationOfferCharacterArt = new Rect(7f, 7f, 226f, 193f);
        public static readonly Rect PreparationOfferName = new Rect(289f, 15f, 276f, 52f);
        public static readonly Rect PreparationOfferProfessionIcon = new Rect(240f, 19f, 44f, 44f);
        public static readonly Rect PreparationOfferFaction = new Rect(240f, 69f, 100f, 40f);
        public static readonly Rect PreparationOfferStats = new Rect(240f, 116f, 335f, 62f);
        public static readonly Rect PreparationOfferPriceIcon = new Rect(586f, 50f, 27f, 27f);
        public static readonly Rect PreparationOfferPriceText = new Rect(615f, 40f, 104f, 47f);
        public static readonly Rect PreparationOfferRecruit = new Rect(564f, 100f, 171f, 74f);
        public static readonly Rect PreparationOfferRecruitText = new Rect(564f, 100f, 171f, 74f);
        public static readonly Rect PreparationBenchTitle = new Rect(72f, 1448f, 285f, 44f);
        public static readonly Rect PreparationBackpack = new Rect(568f, 1442f, 108f, 53f);
        public static readonly Rect PreparationRecycle = new Rect(680f, 1442f, 109f, 53f);
        public static readonly Vector2[] PreparationBenchSlotCenters =
        {
            new Vector2(105f, 1573f), new Vector2(230f, 1573f), new Vector2(355f, 1573f),
            new Vector2(480f, 1573f), new Vector2(605f, 1573f), new Vector2(730f, 1573f)
        };
        public const float PreparationBenchSlotDiameter = 102f;
        public static readonly Rect PreparationCloseShop = new Rect(31f, 1681f, 779f, 113f);
        public static readonly Rect PreparationCloseShopText = new Rect(145f, 1697f, 551f, 80f);
    }

    internal static class ExpeditionUiV52Layout
    {
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;

        public static readonly Rect Back = new Rect(35f, 115f, 88f, 88f);
        public static readonly Rect Abandon = new Rect(632f, 128f, 176f, 70f);
        public static readonly Rect Round = new Rect(35f, 220f, 250f, 70f);
        public static readonly Rect Coins = new Rect(304f, 220f, 238f, 70f);
        public static readonly Rect Life = new Rect(558f, 220f, 250f, 70f);

        public static readonly Rect HeaderShip = new Rect(55f, 354f, 90f, 92f);
        public static readonly Rect HeaderTitle = new Rect(151f, 350f, 340f, 62f);
        public static readonly Rect HeaderSubtitle = new Rect(151f, 408f, 400f, 44f);
        public static readonly Rect Refresh = new Rect(585.5f, 366f, 205f, 72f);

        public static readonly Rect LineupTitle = new Rect(78f, 472f, 420f, 58f);
        public static readonly Vector2[] LineupSlotCenters =
        {
            new Vector2(126f, 595f), new Vector2(252f, 595f), new Vector2(378f, 595f),
            new Vector2(504f, 595f), new Vector2(630f, 595f), new Vector2(756f, 595f)
        };
        public const float SlotDiameter = 100f;

        public static readonly Rect[] Offers =
        {
            new Rect(45f, 690f, 751f, 203f),
            new Rect(45f, 896f, 751f, 203f),
            new Rect(45f, 1102f, 751f, 203f)
        };

        public static readonly Rect ReserveTitle = new Rect(78f, 1340f, 390f, 58f);
        public static readonly Rect Backpack = new Rect(523f, 1347f, 122f, 60f);
        public static readonly Rect Recycle = new Rect(656f, 1347f, 122f, 60f);
        public static readonly Vector2[] ReserveSlotCenters =
        {
            new Vector2(126f, 1484f), new Vector2(252f, 1484f), new Vector2(378f, 1484f),
            new Vector2(504f, 1484f), new Vector2(630f, 1484f), new Vector2(756f, 1484f)
        };

        public static readonly Rect Close = new Rect(141f, 1595f, 560f, 96f);
        public static readonly Rect CloseText = new Rect(176f, 1604f, 490f, 78f);
    }

    internal static class ExpeditionUiV53Layout
    {
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;

        public static readonly Rect Back = new Rect(27f, 92f, 94f, 94f);
        public static readonly Rect BackIcon = new Rect(54f, 119f, 40f, 40f);
        public static readonly Rect Compass = new Rect(130f, 84f, 96f, 104f);
        public static readonly Rect Abandon = new Rect(617f, 94f, 195f, 80f);
        public static readonly Rect Round = new Rect(26f, 199f, 259f, 70f);
        public static readonly Rect Coins = new Rect(300f, 199f, 242f, 70f);
        public static readonly Rect Life = new Rect(558f, 199f, 255f, 70f);

        public static readonly Rect HeaderShip = new Rect(55f, 324f, 90f, 92f);
        public static readonly Rect HeaderTitle = new Rect(151f, 320f, 340f, 62f);
        public static readonly Rect HeaderSubtitle = new Rect(151f, 380f, 430f, 44f);
        public static readonly Rect Refresh = new Rect(581.5f, 334f, 205f, 72f);

        public static readonly Rect LineupTitle = new Rect(78f, 441f, 420f, 58f);
        public static readonly Vector2[] LineupSlotCenters =
        {
            new Vector2(115f, 566f), new Vector2(236f, 566f), new Vector2(357f, 566f),
            new Vector2(478f, 566f), new Vector2(599f, 566f), new Vector2(720f, 566f)
        };
        public const float SlotDiameter = 104f;

        public static readonly Rect[] Offers =
        {
            new Rect(42f, 666f, 757f, 220f),
            new Rect(42f, 904f, 757f, 220f),
            new Rect(42f, 1142f, 757f, 220f)
        };

        public static readonly Rect ReserveTitle = new Rect(78f, 1393f, 390f, 58f);
        // The final v5.3.2 underlay pixels differ from the accompanying JSON here.
        // These rectangles follow the visible utility-cell borders in the PNG.
        public static readonly Rect Backpack = new Rect(502f, 1404f, 139f, 56f);
        public static readonly Rect Recycle = new Rect(653f, 1404f, 132f, 56f);
        public static readonly Vector2[] ReserveSlotCenters =
        {
            new Vector2(115f, 1547f), new Vector2(236f, 1547f), new Vector2(357f, 1547f),
            new Vector2(478f, 1547f), new Vector2(599f, 1547f), new Vector2(720f, 1547f)
        };

        // The visible close-button frame is centered at y=1721 in the final PNG.
        public static readonly Rect Close = new Rect(141f, 1673f, 560f, 96f);
        public static readonly Rect CloseText = new Rect(176f, 1682f, 490f, 78f);
    }

    internal static class ExpeditionUiSharedFrames
    {
        // Canonical cross-page assets. Recruitment, enhancement and future expedition
        // pages must reference this specification instead of cloning local lookalikes.
        public const string PageShell = "ART-UI-SHARED-PAGE-SHELL-V55-809x1194-V6.7.3";
        public const string AbandonFrame = "ART-UI-SHARED-ABANDON-FRAME-195x80-V6.7.3";
        public const string ControlsAtlas = "map-a-controls-runtime-v6.2";
        public const string RoundHud = "ART-UI-SHARED-HUD-ROUND-259x70-V6.7.2";
        public const string CoinsHud = "ART-UI-SHARED-HUD-COINS-242x70-V6.7.2";
        public const string LifeHud = "ART-UI-SHARED-HUD-LIFE-255x70-V6.7.2";
        public static readonly Vector4 PageShellBorder = new Vector4(64f, 64f, 64f, 64f);
        public static readonly Vector4 AbandonFrameBorder = new Vector4(32f, 22f, 32f, 22f);
    }

    internal static class ExpeditionEventFamilyV126Layout
    {
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;

        public static readonly Rect Canvas = new Rect(0f, 0f, ReferenceWidth, ReferenceHeight);
        public static readonly Rect SharedLargeShell = new Rect(16f, 430f, 809f, 1425f);
        public static readonly Rect HeaderRibbon = new Rect(0f, 270f, 841f, 280f);
        public static readonly Rect HeaderEmblem = new Rect(332.5f, 322f, 176f, 176f);
        public static readonly Rect Title = new Rect(120f, 492f, 601f, 60f);
        public static readonly Rect Description = new Rect(90f, 620f, 661f, 108f);
        public static readonly Rect[] OptionCards =
        {
            new Rect(42f, 735f, 757f, 333f),
            new Rect(42f, 1085f, 757f, 333f),
            new Rect(42f, 1435f, 757f, 333f)
        };
        public static readonly Rect FooterHint = new Rect(120f, 1780f, 601f, 52f);

        public static readonly Rect IconPlateLocal = new Rect(20f, 74f, 184f, 184f);
        public static readonly Rect PatternLocal = new Rect(44f, 98f, 136f, 136f);
        public static readonly Rect ForegroundIconLocal = new Rect(50f, 104f, 124f, 124f);
        public static readonly Rect CopyGroupLocal = new Rect(214f, 36f, 304f, 261f);
        public static readonly Rect PrimaryButtonLocal = new Rect(520f, 128f, 178f, 76f);
        public static readonly Vector4 OptionCardBorder = new Vector4(52f, 44f, 52f, 44f);

        public const string HeaderRibbonResource =
            "runtime/event-family/header/1x/event-family-header-ribbon-shell-841x280-v12.6.1";
        public const string OptionCardResource =
            "runtime/event-family/option-card/1x/event-family-option-card-frame-757x230-v12.6";
        public const string IconPlateResource =
            "runtime/event-family/option-plate/1x/event-family-option-icon-plate-184-v12.6";
        public const string PrimaryButtonResource = "runtime/buttons/button-primary-198x86";

        public static string EmblemResource(string theme)
        {
            return "runtime/event-family/emblems/1x/" + theme + "-emblem-176-v12.6";
        }

        public static string PatternResource(string theme)
        {
            return "runtime/event-family/patterns/1x/" + theme + "-pattern-overlay-136-v12.6.2";
        }
    }

    internal static class ExpeditionUiV67Layout
    {
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;

        public static readonly Rect Back = new Rect(28f, 80f, 104f, 104f);
        public static readonly Rect BackArrow = new Rect(59f, 112f, 42f, 42f);
        public static readonly Rect Compass = new Rect(138f, 86f, 92f, 96f);
        public static readonly Rect Abandon = new Rect(628f, 92f, 184f, 70f);
        // Shared HUD geometry: this is intentionally identical to the accepted
        // enhancement/map pages and must remain a cross-page component.
        public static readonly Rect Round = new Rect(26f, 199f, 259f, 70f);
        public static readonly Rect Coins = new Rect(300f, 199f, 242f, 70f);
        public static readonly Rect Life = new Rect(558f, 199f, 255f, 70f);

        public static readonly Rect MainPanel = new Rect(18f, 284f, 805f, 1542f);
        public static readonly Rect Header = new Rect(44f, 310f, 753f, 102f);
        public static readonly Rect HeaderShip = new Rect(55f, 317f, 82f, 82f);
        public static readonly Rect HeaderTitle = new Rect(142f, 316f, 420f, 42f);
        public static readonly Rect HeaderSubtitle = new Rect(142f, 357f, 455f, 42f);
        public static readonly Rect Refresh = new Rect(628f, 329f, 154f, 64f);

        public static readonly Rect CurrentPanel = new Rect(44f, 426f, 753f, 196f);
        public static readonly Rect CurrentTitle = new Rect(69f, 440f, 500f, 42f);
        public static readonly Rect[] Offers =
        {
            new Rect(44f, 642f, 753f, 210f),
            new Rect(44f, 870f, 753f, 210f),
            new Rect(44f, 1098f, 753f, 210f)
        };

        public static readonly Rect ReservePanel = new Rect(44f, 1328f, 753f, 196f);
        public static readonly Rect ReserveTitle = new Rect(69f, 1342f, 420f, 42f);
        // The shop canvas is 841 wide while formation uses 1080. Scale the shared
        // 180x108 utility control by 841/1080 so its physical on-device size stays
        // identical after the shop canvas fit is applied. Only the centers align
        // with Close; the side controls intentionally remain smaller.
        private const float SharedUtilityScale = ReferenceWidth / FormationBattleV94Layout.CanvasWidth;
        private const float SharedUtilityWidth = 180f * SharedUtilityScale;
        private const float SharedUtilityHeight = 108f * SharedUtilityScale;
        private const float FooterCenterY = 1702f;
        private const float FooterSideInset = 44f;
        public static readonly Rect Backpack = new Rect(FooterSideInset,
            FooterCenterY - SharedUtilityHeight * 0.5f, SharedUtilityWidth, SharedUtilityHeight);
        public static readonly Rect Close = new Rect(216f, 1648f, 409f, 108f);
        public static readonly Rect Recycle = new Rect(ReferenceWidth - FooterSideInset - SharedUtilityWidth,
            FooterCenterY - SharedUtilityHeight * 0.5f, SharedUtilityWidth, SharedUtilityHeight);

        public const float SlotDiameter = 92f;
        public static readonly Vector2[] LineupSlotCenters = BuildSlotCenters(502f);
        public static readonly Vector2[] ReserveSlotCenters = BuildSlotCenters(1410f);

        private static Vector2[] BuildSlotCenters(float centerY)
        {
            var centers = new Vector2[6];
            for (var index = 0; index < centers.Length; index++)
                centers[index] = new Vector2(72f + SlotDiameter * 0.5f + index * (SlotDiameter + 27f), centerY + SlotDiameter * 0.5f);
            return centers;
        }
    }

    internal static class ExpeditionUiV551Layout
    {
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;

        public static readonly Rect Canvas = new Rect(0f, 0f, ReferenceWidth, ReferenceHeight);
        public static readonly Rect Back = new Rect(27f, 92f, 94f, 94f);
        public static readonly Rect BackIcon = new Rect(54f, 119f, 40f, 40f);
        public static readonly Rect Compass = new Rect(130f, 84f, 96f, 104f);
        public static readonly Rect Abandon = new Rect(617f, 94f, 195f, 80f);
        public static readonly Rect Round = new Rect(26f, 199f, 259f, 70f);
        public static readonly Rect Coins = new Rect(300f, 199f, 242f, 70f);
        public static readonly Rect Life = new Rect(558f, 199f, 255f, 70f);

        public static readonly Rect HeaderShip = new Rect(56f, 359f, 92f, 92f);
        public static readonly Rect HeaderTitle = new Rect(151f, 357f, 390f, 64f);
        public static readonly Rect HeaderSubtitle = new Rect(151f, 423f, 570f, 44f);
        public static readonly Rect[] LegendLabels =
        {
            new Rect(132f, 578f, 90f, 44f), new Rect(254f, 578f, 90f, 44f),
            new Rect(376f, 578f, 90f, 44f), new Rect(498f, 578f, 90f, 44f),
            new Rect(620f, 578f, 90f, 44f)
        };
        public static readonly Rect[] BenefitRows =
        {
            new Rect(42f, 650f, 757f, 230f),
            new Rect(42f, 900f, 757f, 230f),
            new Rect(42f, 1150f, 757f, 230f)
        };
        public static readonly Rect BenefitIcon = new Rect(41f, 30f, 170f, 170f);
        public static readonly Rect QualityBadge = new Rect(228f, 24f, 142f, 50f);
        public static readonly Rect QualityDiamond = new Rect(234f, 29f, 40f, 40f);
        public static readonly Rect QualityLabel = new Rect(278f, 24f, 86f, 50f);
        public static readonly Rect BenefitName = new Rect(218f, 74f, 305f, 48f);
        // Reserve two explicit lines: exact effect/scope and current/max stacks.
        public static readonly Rect BenefitDescription = new Rect(212f, 128f, 321f, 84f);
        public static readonly Rect SelectButton = new Rect(533f, 67f, 198f, 86f);
        public static readonly Rect BottomHint = new Rect(171f, 1452f, 500f, 40f);
    }

    internal static class ExpeditionMapUiV62Layout
    {
        public const float ReferenceWidth = 841f;
        public const float ReferenceHeight = 1870f;
        public const float MapPaperCropTop = 34f;
        public const float MapPaperCropBottom = 34f;

        public static readonly Rect Canvas = new Rect(0f, 0f, ReferenceWidth, ReferenceHeight);
        public static readonly Rect BackFrame = new Rect(27f, 92f, 94f, 94f);
        // The v6.5 atlas cell is square, but the painted ring inside it is 73x80
        // and 4.5 px left of the cell centre. Draw it through this compensated
        // visual rect so the visible ring is circular and shares the arrow centre.
        // BackFrame remains the unchanged 94x94 interaction rect.
        public static readonly Rect BackFrameVisual = new Rect(27.5f, 91f, 103f, 94f);
        public static readonly Rect BackIcon = new Rect(54f, 119f, 40f, 40f);
        public static readonly Rect CompassDecor = new Rect(130f, 84f, 96f, 104f);
        public static readonly Rect AbandonFrame = new Rect(617f, 94f, 195f, 80f);
        public static readonly Rect AbandonIcon = new Rect(651f, 117f, 32f, 32f);
        public static readonly Rect AbandonLabel = new Rect(685f, 108f, 104f, 52f);

        public static readonly Rect[] HudFrames =
        {
            new Rect(26f, 199f, 259f, 70f),
            new Rect(300f, 199f, 242f, 70f),
            new Rect(558f, 199f, 255f, 70f)
        };
        public static readonly Rect[] HudIcons =
        {
            new Rect(69f, 217f, 34f, 34f),
            new Rect(323f, 217f, 34f, 34f),
            new Rect(588f, 217f, 34f, 34f)
        };
        public static readonly Rect[] HudLabels =
        {
            new Rect(108f, 207f, 151f, 54f),
            new Rect(362f, 207f, 164f, 54f),
            new Rect(633f, 207f, 154f, 54f)
        };

        public static readonly Rect HeaderShip = new Rect(47f, 329f, 96f, 108f);
        public static readonly Rect HeaderTitle = new Rect(150f, 344f, 570f, 52f);
        public static readonly Rect HeaderSubtitle = new Rect(150f, 404f, 570f, 40f);
        public static readonly Rect MapViewport = new Rect(49f, 521f, 743f, 984f);

        public static readonly float[] LegendCentersX = { 129f, 275f, 421f, 567f, 713f };
        public const float LegendIconTop = 1573f;
        public static readonly Rect[] LegendLabels =
        {
            new Rect(70f, 1628f, 118f, 48f),
            new Rect(216f, 1628f, 118f, 48f),
            new Rect(362f, 1628f, 118f, 48f),
            new Rect(508f, 1628f, 118f, 48f),
            new Rect(654f, 1628f, 118f, 48f)
        };
        public static readonly Rect DividerCompass = new Rect(405f, 1693f, 32f, 34f);
        public static readonly Rect GestureHint = new Rect(170f, 1727f, 501f, 52f);
    }

    internal static class AspectCoverTextureRenderer
    {
        public static void Draw(Rect canvas, Texture texture, float opacity)
        {
            if (texture == null || canvas.width <= 0f || canvas.height <= 0f) return;

            var sourceAspect = texture.width / Mathf.Max(1f, (float)texture.height);
            var targetAspect = canvas.width / Mathf.Max(1f, canvas.height);
            var sourceUv = new Rect(0f, 0f, 1f, 1f);
            if (sourceAspect > targetAspect)
            {
                sourceUv.width = targetAspect / sourceAspect;
                sourceUv.x = (1f - sourceUv.width) * 0.5f;
            }
            else if (sourceAspect < targetAspect)
            {
                sourceUv.height = sourceAspect / targetAspect;
                sourceUv.y = (1f - sourceUv.height) * 0.5f;
            }

            var tint = Color.white;
            tint.a = Mathf.Clamp01(opacity);
            GUI.color = tint;
            GUI.DrawTextureWithTexCoords(canvas, texture, sourceUv, true);
            GUI.color = Color.white;
        }
    }

    internal sealed class LayeredBattlefieldRenderer
    {
        private static readonly Color Sky = new Color(0.035f, 0.065f, 0.10f);
        private static readonly Color Horizon = new Color(0.12f, 0.09f, 0.14f);
        private static readonly Color Ground = new Color(0.10f, 0.13f, 0.14f);
        private static readonly Color Accent = new Color(0.23f, 0.80f, 0.72f);

        private readonly ILayeredBackgroundSource source;

        public LayeredBattlefieldRenderer(ILayeredBackgroundSource source)
        {
            this.source = source;
        }

        public Color AccentColor { get { return Accent; } }

        public void Draw(Rect canvas, float opacity, float deploymentMarkersOpacity)
        {
            var layers = source.GetLayers(LayeredBackgroundKind.Battle);
            foreach (var layer in layers)
            {
                var texture = source.LoadLayer(layer);
                if (texture != null)
                {
                    if (layer.Role == BackgroundLayerRole.DeploymentMarkers && deploymentMarkersOpacity <= 0f)
                    {
                        continue;
                    }
                    AspectCoverTextureRenderer.Draw(canvas, texture,
                        opacity * (layer.Role == BackgroundLayerRole.DeploymentMarkers ? deploymentMarkersOpacity : 1f));
                    continue;
                }
                DrawProceduralFallback(layer.Role, canvas, opacity, deploymentMarkersOpacity);
            }
        }

        private static void DrawProceduralFallback(
            BackgroundLayerRole role, Rect canvas, float opacity, float deploymentMarkersOpacity)
        {
            switch (role)
            {
                case BackgroundLayerRole.Base:
                    Fill(canvas, Ground, opacity);
                    break;
                case BackgroundLayerRole.FarSea:
                    Fill(canvas, Sky, opacity);
                    break;
                case BackgroundLayerRole.IslandGround:
                    Fill(new Rect(canvas.x, canvas.y + 720f, canvas.width, 520f), Horizon, opacity);
                    break;
                case BackgroundLayerRole.DeploymentMarkers:
                    if (deploymentMarkersOpacity > 0f)
                    {
                        DrawSoftDeploymentMarkers(opacity * deploymentMarkersOpacity);
                    }
                    break;
                case BackgroundLayerRole.ForegroundDock:
                    Fill(new Rect(canvas.x, canvas.y + 1160f, canvas.width, 760f), Ground, opacity);
                    Fill(new Rect(canvas.x + 75f, canvas.y + 835f, 930f, 8f), Accent * 0.45f, opacity);
                    Fill(new Rect(canvas.x + 150f, canvas.y + 1010f, 780f, 5f), Accent * 0.45f, opacity);
                    break;
            }
        }

        private static void DrawSoftDeploymentMarkers(float opacity)
        {
            for (var i = 0; i < 6; i++)
            {
                var column = i % 3;
                var row = i / 3;
                var marker = new Rect(100f + column * 320f, 1130f + row * 240f, 240f, 130f);
                DrawOutline(marker, 4f, ColorWithOpacity(Accent, opacity * 0.34f));
            }
        }

        private static void DrawOutline(Rect rect, float thickness, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void Fill(Rect rect, Color color, float opacity)
        {
            GUI.color = ColorWithOpacity(color, opacity);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Color ColorWithOpacity(Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            return color;
        }
    }

    /// <summary>
    /// Renders a vertically scrollable expedition canvas. Formal art can replace each
    /// fallback layer independently without baking routes, nodes or localized text.
    /// </summary>
    internal sealed class LayeredExpeditionMapRenderer
    {
        private static readonly Color DeepMistSea = new Color(136f / 255f, 187f / 255f, 201f / 255f);
        private static readonly Color DistantCliff = new Color(0.20f, 0.30f, 0.29f);
        // v1.8 was authored against the retired #AECBD2 base. These channel
        // ratios convert that exact base to the approved #88BBC9 while keeping
        // the thin wave-line contrast until the PNG is re-exported.
        private static readonly Color CorrectedMapSeaTint = new Color(
            136f / 174f, 187f / 203f, 201f / 210f, 1f);
        private readonly ILayeredBackgroundSource source;
        private readonly Dictionary<string, Texture2D> textureCache = new Dictionary<string, Texture2D>();
        private ManualMapAnchorFile manualMapAnchors;
        private bool manualMapAnchorsLoaded;
        private FormalRoutePortFile formalRoutePorts;
        private bool formalRoutePortsLoaded;
        private readonly Dictionary<string, FormalRoutePort> formalRoutePortsById =
            new Dictionary<string, FormalRoutePort>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> formalRoutePortOwnersById =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, FormalRouteNode> formalRouteNodesById =
            new Dictionary<string, FormalRouteNode>(StringComparer.Ordinal);
        private readonly Dictionary<string, FormalCandidateEdge> formalRouteEdgesByNodes =
            new Dictionary<string, FormalCandidateEdge>(StringComparer.Ordinal);
        private static readonly string[] NeutralIslandIds =
        {
            "ART-MAP-ISLAND-NEUTRAL-A-01", "ART-MAP-ISLAND-NEUTRAL-B-01",
            "ART-MAP-ISLAND-NEUTRAL-C-01", "ART-MAP-ISLAND-NEUTRAL-D-01",
            "ART-MAP-ISLAND-NEUTRAL-E-01", "ART-MAP-ISLAND-NEUTRAL-F-01",
            "ART-MAP-ISLAND-NEUTRAL-G-01", "ART-MAP-ISLAND-NEUTRAL-H-01",
            "ART-MAP-ISLAND-NEUTRAL-I-01", "ART-MAP-ISLAND-NEUTRAL-J-01",
            "ART-MAP-ISLAND-NEUTRAL-K-01", "ART-MAP-ISLAND-NEUTRAL-L-01",
            "ART-MAP-ISLAND-NEUTRAL-M-01", "ART-MAP-ISLAND-NEUTRAL-N-01",
            "ART-MAP-ISLAND-NEUTRAL-O-01"
        };
        private static readonly Vector2[][] NeutralIslandAnchors =
        {
            Anchors(77, 69, 173, 72, 82, 151, 169, 148),
            Anchors(74, 72, 174, 78, 83, 145, 170, 151),
            Anchors(77, 68, 171, 67, 83, 153, 166, 151),
            Anchors(75, 70, 169, 75, 82, 151, 169, 153),
            Anchors(75, 67, 177, 72, 78, 148, 142, 155),
            Anchors(73, 67, 173, 70, 77, 148, 140, 154),
            Anchors(82, 62, 170, 76, 72, 137, 159, 153),
            Anchors(91, 60, 165, 70, 79, 148, 166, 159),
            Anchors(70, 66, 177, 75, 73, 146, 160, 151),
            Anchors(88, 59, 168, 70, 73, 142, 157, 157),
            Anchors(72, 64, 175, 70, 75, 146, 167, 158),
            Anchors(84, 62, 170, 66, 76, 147, 168, 153),
            Anchors(71, 93, 169, 87, 88, 128, 186, 121),
            Anchors(76, 79, 168, 103, 93, 101, 187, 127),
            Anchors(70, 88, 166, 72, 83, 121, 182, 104)
        };
        private static readonly float[] NeutralIslandLabelOffsets =
        {
            0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 0f, 53f, 58f, 49f
        };
        private static readonly string[][] NeutralDecorationProfiles =
        {
            new[] { "GRASS", "GRASS", "ROCK" },
            new[] { "TREE", "TREE", "SHRUB" },
            new[] { "POOL", "REED", "ROCK" },
            new[] { "ROCK", "PEBBLES", "GRASS" },
            new[] { "REED", "REED", "SHRUB" },
            new[] { "DRIFTWOOD", "GRASS", "PEBBLES" }
        };

        public LayeredExpeditionMapRenderer(ILayeredBackgroundSource source)
        {
            this.source = source;
        }

        public bool DrawChapterOneMap(Rect canvas, float opacity)
        {
            var texture = Load("Art/Map/Chapter01/", "ART-MAP-CHAPTER-01-CLEAN-01");
            if (texture == null) return false;
            GUI.color = ColorWithOpacity(Color.white, opacity);
            GUI.DrawTexture(canvas, texture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            return true;
        }

        public bool TryGetChapterOneLabelAnchor(int anchorIndex, Rect canvas, out Vector2 point)
        {
            point = Vector2.zero;
            EnsureManualMapAnchors();
            if (manualMapAnchors == null || manualMapAnchors.canvas == null ||
                manualMapAnchors.anchors == null || anchorIndex < 0 ||
                anchorIndex >= manualMapAnchors.anchors.Length) return false;
            var anchor = manualMapAnchors.anchors[anchorIndex];
            var expectedId = "M" + anchorIndex.ToString("00");
            if (!string.Equals(anchor.id, expectedId, StringComparison.Ordinal)) return false;
            point = new Vector2(
                canvas.x + anchor.x * canvas.width / manualMapAnchors.canvas.width,
                canvas.y + anchor.y * canvas.height / manualMapAnchors.canvas.height);
            return true;
        }

        public bool TryGetChapterOneRoutePorts(
            string sourceNodeId, string targetNodeId, Rect canvas,
            out Vector2 sourcePoint, out Vector2 target)
        {
            sourcePoint = Vector2.zero;
            target = Vector2.zero;
            EnsureFormalRoutePorts();
            FormalCandidateEdge edge;
            FormalRouteNode sourceNode;
            FormalRouteNode targetNode;
            if (formalRoutePorts != null)
            {
                if (formalRouteEdgesByNodes.TryGetValue(
                        RouteEdgeKey(sourceNodeId, targetNodeId), out edge) &&
                    formalRouteNodesById.TryGetValue(sourceNodeId, out sourceNode) &&
                    formalRouteNodesById.TryGetValue(targetNodeId, out targetNode) &&
                    TryFindNearestRoutePortPair(
                        sourceNode.routePorts, targetNode.routePorts, canvas,
                        out sourcePoint, out target))
                {
                    return true;
                }

                DisableFormalRoutePorts(
                    "Manual route-port data is missing for runtime edge " +
                    sourceNodeId + ">" + targetNodeId + "; using whole-map fallback.");
            }

            int sourceIndex;
            int targetIndex;
            if (!TryParseMapNodeIndex(sourceNodeId, out sourceIndex) ||
                !TryParseMapNodeIndex(targetNodeId, out targetIndex)) return false;
            return TryGetFallbackRoutePort(sourceIndex, true, canvas, out sourcePoint) &&
                   TryGetFallbackRoutePort(targetIndex, false, canvas, out target);
        }

        private bool TryGetFallbackRoutePort(
            int anchorIndex, bool outgoing, Rect canvas, out Vector2 point)
        {
            if (!TryGetChapterOneLabelAnchor(anchorIndex, canvas, out point)) return false;
            var scaleX = canvas.width / manualMapAnchors.canvas.width;
            var scaleY = canvas.height / manualMapAnchors.canvas.height;
            point += new Vector2((outgoing ? 50f : -50f) * scaleX, -54f * scaleY);
            return true;
        }

        private Vector2 ScaleFormalRoutePort(FormalRoutePort port, Rect canvas)
        {
            return new Vector2(
                canvas.x + port.x * canvas.width / formalRoutePorts.coordinateSpace.width,
                canvas.y + port.y * canvas.height / formalRoutePorts.coordinateSpace.height);
        }

        private bool TryFindNearestRoutePortPair(
            FormalRoutePort[] sourcePorts,
            FormalRoutePort[] targetPorts,
            Rect canvas,
            out Vector2 sourcePoint,
            out Vector2 targetPoint)
        {
            sourcePoint = Vector2.zero;
            targetPoint = Vector2.zero;
            if (sourcePorts == null || sourcePorts.Length == 0 ||
                targetPorts == null || targetPorts.Length == 0) return false;

            var scaleX = canvas.width / formalRoutePorts.coordinateSpace.width;
            var scaleY = canvas.height / formalRoutePorts.coordinateSpace.height;
            var shortestDistanceSquared = float.PositiveInfinity;
            FormalRoutePort nearestSource = null;
            FormalRoutePort nearestTarget = null;
            foreach (var sourcePort in sourcePorts)
            {
                foreach (var targetPort in targetPorts)
                {
                    var dx = (targetPort.x - sourcePort.x) * scaleX;
                    var dy = (targetPort.y - sourcePort.y) * scaleY;
                    var distanceSquared = dx * dx + dy * dy;
                    if (distanceSquared >= shortestDistanceSquared) continue;
                    shortestDistanceSquared = distanceSquared;
                    nearestSource = sourcePort;
                    nearestTarget = targetPort;
                }
            }

            if (nearestSource == null || nearestTarget == null) return false;
            sourcePoint = ScaleFormalRoutePort(nearestSource, canvas);
            targetPoint = ScaleFormalRoutePort(nearestTarget, canvas);
            return true;
        }

        private static string RouteEdgeKey(string sourceNodeId, string targetNodeId)
        {
            return sourceNodeId + ">" + targetNodeId;
        }

        private static bool TryParseMapNodeIndex(string nodeId, out int index)
        {
            index = -1;
            return !string.IsNullOrEmpty(nodeId) && nodeId.Length == 3 && nodeId[0] == 'M' &&
                   Int32.TryParse(nodeId.Substring(1), out index) && index >= 0 && index < 30;
        }

        public bool DrawChapterOneNodeMarker(
            Vector2 labelAnchor, ExpeditionNodeType type,
            ExpeditionMapNodeVisualState state, float scale, float opacity)
        {
            var stageIcon = Load("Art/Map/StageIcons/", StageIconResourceId(type));
            if (stageIcon == null) return false;
            var iconSize = 24f * scale;
            var iconArea = new Rect(labelAnchor.x - 47f * scale,
                labelAnchor.y - iconSize * 0.5f, iconSize, iconSize);
            var accent = state == ExpeditionMapNodeVisualState.Current
                ? new Color(0.92f, 0.66f, 0.20f)
                : state == ExpeditionMapNodeVisualState.Available
                    ? new Color(0.37f, 0.76f, 0.72f)
                    : StageIconBackground(type);
            GUI.color = ColorWithOpacity(accent, opacity * 0.94f);
            GUI.DrawTexture(new Rect(iconArea.x - 3f * scale, iconArea.y - 3f * scale,
                iconArea.width + 6f * scale, iconArea.height + 6f * scale), Texture2D.whiteTexture);
            GUI.color = ColorWithOpacity(Color.white, opacity);
            GUI.DrawTexture(iconArea, stageIcon, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
            return true;
        }

        private void EnsureManualMapAnchors()
        {
            if (manualMapAnchorsLoaded) return;
            manualMapAnchorsLoaded = true;
            var asset = Resources.Load<TextAsset>("Art/Map/Chapter01/manual-label-anchors-v0.2");
            if (asset != null) manualMapAnchors = JsonUtility.FromJson<ManualMapAnchorFile>(asset.text);
        }

        private void EnsureFormalRoutePorts()
        {
            if (formalRoutePortsLoaded) return;
            formalRoutePortsLoaded = true;
            var asset = Resources.Load<TextAsset>(
                "Config/Chapter01/CHAPTER-01-MAP-MERGED-V0.4");
            if (asset == null)
            {
                Debug.LogError("E22 merged map configuration is missing; using fallback route ports.");
                return;
            }
            formalRoutePorts = JsonUtility.FromJson<FormalRoutePortFile>(asset.text);
            if (formalRoutePorts == null || formalRoutePorts.coordinateSpace == null ||
                formalRoutePorts.anchors == null ||
                formalRoutePorts.candidateEdgePortBindings == null)
            {
                formalRoutePorts = null;
                Debug.LogError("E22 merged map configuration is invalid; using fallback route ports.");
                return;
            }

            var invalid = formalRoutePorts.coordinateSpace.width != 2172f ||
                          formalRoutePorts.coordinateSpace.height != 724f ||
                          formalRoutePorts.anchors.Length != 30;
            var nodeIds = new HashSet<string>(StringComparer.Ordinal);
            for (var nodeIndex = 0; nodeIndex < formalRoutePorts.anchors.Length; nodeIndex++)
            {
                var node = formalRoutePorts.anchors[nodeIndex];
                var expectedNodeId = "M" + nodeIndex.ToString("00");
                if (node == null || string.IsNullOrEmpty(node.id) || node.routePorts == null ||
                    node.routePorts.Length < 1 || node.routePorts.Length > 5 ||
                    !string.Equals(node.id, expectedNodeId, StringComparison.Ordinal) ||
                    !nodeIds.Add(node.id))
                {
                    invalid = true;
                    continue;
                }
                foreach (var port in node.routePorts)
                {
                    if (port == null || string.IsNullOrEmpty(port.id) ||
                        port.x < 0f || port.x > formalRoutePorts.coordinateSpace.width ||
                        port.y < 0f || port.y > formalRoutePorts.coordinateSpace.height ||
                        formalRoutePortsById.ContainsKey(port.id))
                    {
                        invalid = true;
                        continue;
                    }
                    formalRoutePortsById.Add(port.id, port);
                    formalRoutePortOwnersById.Add(port.id, node.id);
                }
                formalRouteNodesById.Add(node.id, node);
            }
            foreach (var edge in formalRoutePorts.candidateEdgePortBindings)
            {
                if (edge == null || string.IsNullOrEmpty(edge.source) ||
                    string.IsNullOrEmpty(edge.target))
                {
                    invalid = true;
                    continue;
                }
                var edgeKey = RouteEdgeKey(edge.source, edge.target);
                if (formalRouteEdgesByNodes.ContainsKey(edgeKey) ||
                    !formalRouteNodesById.ContainsKey(edge.source) ||
                    !formalRouteNodesById.ContainsKey(edge.target))
                {
                    invalid = true;
                    continue;
                }
                formalRouteEdgesByNodes.Add(edgeKey, edge);
            }
            invalid |= formalRouteNodesById.Count != 30 ||
                       formalRouteEdgesByNodes.Count != 54 ||
                       !formalRouteEdgesByNodes.ContainsKey("M28>M29");
            if (!invalid) return;

            DisableFormalRoutePorts(
                "E22/E27 route-port configuration failed integrity validation; " +
                "using fallback route ports for the whole map.");
        }

        private void DisableFormalRoutePorts(string message)
        {
            formalRoutePorts = null;
            formalRoutePortsById.Clear();
            formalRoutePortOwnersById.Clear();
            formalRouteNodesById.Clear();
            formalRouteEdgesByNodes.Clear();
            Debug.LogError(message);
        }

        [Serializable]
        private sealed class ManualMapAnchorFile
        {
            public ManualMapCanvas canvas = new ManualMapCanvas();
            public ManualMapAnchor[] anchors = new ManualMapAnchor[0];
        }

        [Serializable]
        private sealed class ManualMapCanvas
        {
            public float width = 0f;
            public float height = 0f;
        }

        [Serializable]
        private sealed class ManualMapAnchor
        {
            public string id = string.Empty;
            public float x = 0f;
            public float y = 0f;
        }

        [Serializable]
        private sealed class FormalRoutePortFile
        {
            public ManualMapCanvas coordinateSpace = new ManualMapCanvas();
            public FormalRouteNode[] anchors = new FormalRouteNode[0];
            public FormalCandidateEdge[] candidateEdgePortBindings = new FormalCandidateEdge[0];
        }

        [Serializable]
        private sealed class FormalRouteNode
        {
            public string id = string.Empty;
            public FormalRoutePort[] routePorts = new FormalRoutePort[0];
        }

        [Serializable]
        private sealed class FormalRoutePort
        {
            public string id = string.Empty;
            public float x = 0f;
            public float y = 0f;
        }

        [Serializable]
        private sealed class FormalCandidateEdge
        {
            public string source = string.Empty;
            public string target = string.Empty;
            public string sourcePortId = string.Empty;
            public string targetPortId = string.Empty;
        }

        public void Draw(Rect canvas, float opacity, uint mapSeed)
        {
            var sea = Load("Art/Map/Backgrounds/", PrototypeArtResourceIds.MapSeaTile);
            if (sea != null)
            {
                GUI.color = ColorWithOpacity(CorrectedMapSeaTint, opacity);
                for (var x = canvas.x; x < canvas.xMax; x += 1024f)
                {
                    GUI.DrawTexture(new Rect(x, canvas.y, 1024f, canvas.height),
                        sea, ScaleMode.StretchToFill, true);
                }
                GUI.color = Color.white;
                DrawDeterministicReefs(canvas, opacity, mapSeed);
                return;
            }

            foreach (var layer in source.GetLayers(LayeredBackgroundKind.Map))
            {
                var texture = source.LoadLayer(layer);
                if (texture != null)
                {
                    GUI.color = ColorWithOpacity(Color.white, opacity);
                    GUI.DrawTexture(canvas, texture, ScaleMode.StretchToFill, true);
                    GUI.color = Color.white;
                    continue;
                }
                DrawProceduralFallback(layer.Role, canvas, opacity);
            }
        }

        public bool DrawNode(
            Rect touchArea, uint mapSeed, string nodeId, ExpeditionNodeType type,
            ExpeditionMapNodeVisualState state, float opacity, out Rect labelArea)
        {
            labelArea = new Rect(touchArea.x + 72f, touchArea.yMax - 30f,
                touchArea.width - 78f, 32f);
            var selection = ExpeditionMapVisualSelector.Create(mapSeed, nodeId);
            var islandIndex = selection.IslandTemplateIndex;
            var mirrorX = selection.MirrorX;
            var decorationProfileIndex = selection.DecorationProfileIndex;

            var island = Load("Art/Map/NeutralIslands/", NeutralIslandIds[islandIndex]);
            if (island == null) return false;

            var stateRing = Load("Art/Map/Rings/", RingResourceId(type, state));
            var center = new Vector2(touchArea.center.x, touchArea.y + 88f);
            const float iconSize = 180f;
            const float ringSize = 210f;
            var islandHeight = iconSize * 224f / 256f;
            var islandArea = new Rect(center.x - iconSize * 0.5f,
                center.y - islandHeight * 0.5f, iconSize, islandHeight);
            GUI.color = ColorWithOpacity(Color.white, opacity);
            if (stateRing != null)
            {
                GUI.DrawTexture(CenteredRect(center, ringSize), stateRing, ScaleMode.ScaleToFit, true);
            }
            if (mirrorX)
                GUI.DrawTextureWithTexCoords(islandArea, island, new Rect(1f, 0f, -1f, 1f), true);
            else
                GUI.DrawTexture(islandArea, island, ScaleMode.ScaleToFit, true);

            var profile = NeutralDecorationProfiles[decorationProfileIndex];
            var anchors = NeutralIslandAnchors[islandIndex];
            for (var index = 0; index < selection.AnchorIndices.Count; index++)
            {
                var kind = profile[index % profile.Length];
                var decoration = Load("Art/Map/NeutralDecor/", NeutralDecorationResourceId(kind));
                if (decoration == null) continue;
                var anchor = anchors[selection.AnchorIndices[index]];
                var anchorX = mirrorX ? 256f - anchor.x : anchor.x;
                var size = DecorationSize(kind) + selection.DecorationSizeOffsets[index];
                var x = islandArea.x + anchorX * islandArea.width / 256f;
                var y = islandArea.y + anchor.y * islandArea.height / 224f;
                GUI.DrawTexture(new Rect(x - size * 0.5f, y - size * 0.5f, size, size),
                    decoration, ScaleMode.ScaleToFit, true);
            }

            var labelOffset = NeutralIslandLabelOffsets[islandIndex];
            var labelY = labelOffset > 0f
                ? center.y + labelOffset
                : center.y + islandHeight * 0.5f + 13f;
            var stageIcon = Load("Art/Map/StageIcons/", StageIconResourceId(type));
            var iconArea = new Rect(center.x - 72f, labelY + 2f, 30f, 30f);
            GUI.color = ColorWithOpacity(StageIconBackground(type), opacity * 0.88f);
            GUI.DrawTexture(iconArea, Texture2D.whiteTexture);
            if (stageIcon != null)
            {
                GUI.color = ColorWithOpacity(Color.white, opacity);
                GUI.DrawTexture(iconArea, stageIcon, ScaleMode.ScaleToFit, true);
            }
            labelArea = new Rect(center.x - 31f, labelY, 118f, 34f);
            GUI.color = Color.white;
            return true;
        }

        private static Vector2[] Anchors(
            float x0, float y0, float x1, float y1,
            float x2, float y2, float x3, float y3)
        {
            return new[]
            {
                new Vector2(x0, y0), new Vector2(x1, y1),
                new Vector2(x2, y2), new Vector2(x3, y3)
            };
        }

        private static string NeutralDecorationResourceId(string kind)
        {
            return "ART-MAP-NEUTRAL-DECOR-" + kind + "-01";
        }

        private static float DecorationSize(string kind)
        {
            switch (kind)
            {
                case "ROCK": return 18f;
                case "TREE": return 19f;
                case "GRASS": return 16f;
                case "SHRUB": return 19f;
                case "REED": return 18f;
                case "POOL": return 30f;
                case "DRIFTWOOD": return 27f;
                default: return 23f;
            }
        }

        private static string StageIconResourceId(ExpeditionNodeType type)
        {
            switch (type)
            {
                case ExpeditionNodeType.EliteBattle:
                    return "ART-MAP-STAGE-ICON-ELITE-01";
                case ExpeditionNodeType.HighRiskBattle:
                    return "ART-MAP-STAGE-ICON-HIGH-RISK-01";
                case ExpeditionNodeType.Boss:
                    return "ART-MAP-STAGE-ICON-BOSS-01";
                case ExpeditionNodeType.Event:
                    return "ART-MAP-STAGE-ICON-EVENT-01";
                case ExpeditionNodeType.Rest:
                    return "ART-MAP-STAGE-ICON-REST-01";
                case ExpeditionNodeType.Shop:
                    return "ART-MAP-STAGE-ICON-SHOP-01";
                case ExpeditionNodeType.Treasure:
                case ExpeditionNodeType.Supply:
                    return "ART-MAP-STAGE-ICON-TREASURE-01";
                default:
                    return "ART-MAP-STAGE-ICON-NORMAL-01";
            }
        }

        private static Color StageIconBackground(ExpeditionNodeType type)
        {
            switch (type)
            {
                case ExpeditionNodeType.EliteBattle:
                    return new Color(0.50f, 0.28f, 0.32f);
                case ExpeditionNodeType.HighRiskBattle:
                    return new Color(156f / 255f, 102f / 255f, 87f / 255f);
                case ExpeditionNodeType.Boss:
                    return new Color(89f / 255f, 59f / 255f, 72f / 255f);
                case ExpeditionNodeType.Event:
                    return new Color(0.40f, 0.34f, 0.55f);
                case ExpeditionNodeType.Rest:
                    return new Color(0.42f, 0.53f, 0.38f);
                case ExpeditionNodeType.Shop:
                    return new Color(0.45f, 0.68f, 0.72f);
                case ExpeditionNodeType.Treasure:
                case ExpeditionNodeType.Supply:
                    return new Color(0.71f, 0.54f, 0.28f);
                default:
                    return new Color(0.73f, 0.55f, 0.22f);
            }
        }

        private void DrawDeterministicReefs(Rect canvas, float opacity, uint mapSeed)
        {
            var cluster = Load("Art/Map/Decor/", PrototypeArtResourceIds.MapReefCluster);
            var ring = Load("Art/Map/Decor/", PrototypeArtResourceIds.MapReefRing);
            if (cluster == null && ring == null) return;

            for (var index = 0; index < 10; index++)
            {
                var hash = Mix(mapSeed + (uint)(index + 1) * 0x9E3779B9u);
                var texture = (hash & 1u) == 0u ? cluster : ring;
                if (texture == null) texture = cluster ?? ring;
                var size = 120f + ((hash >> 8) % 95u);
                var x = canvas.x + 55f + ((hash >> 16) % (uint)Mathf.Max(1f, canvas.width - size - 110f));
                var y = canvas.y + 90f + ((hash >> 3) % (uint)Mathf.Max(1f, canvas.height - size - 180f));
                GUI.color = ColorWithOpacity(Color.white, opacity * 0.48f);
                GUI.DrawTexture(new Rect(x, y, size, size), texture, ScaleMode.ScaleToFit, true);
            }
            GUI.color = Color.white;
        }

        private Texture2D Load(string parentPath, string resourceId)
        {
            Texture2D texture;
            if (textureCache.TryGetValue(resourceId, out texture)) return texture;
            texture = Resources.Load<Texture2D>(parentPath + resourceId);
            textureCache[resourceId] = texture;
            return texture;
        }

        private static string NodeResourceId(ExpeditionNodeType type)
        {
            switch (type)
            {
                case ExpeditionNodeType.EliteBattle: return PrototypeArtResourceIds.MapNodeElite;
                case ExpeditionNodeType.HighRiskBattle: return PrototypeArtResourceIds.MapNodeHighRisk;
                case ExpeditionNodeType.Event: return PrototypeArtResourceIds.MapNodeEvent;
                case ExpeditionNodeType.Shop: return PrototypeArtResourceIds.MapNodeShop;
                case ExpeditionNodeType.Rest: return PrototypeArtResourceIds.MapNodeRest;
                case ExpeditionNodeType.Treasure:
                case ExpeditionNodeType.Supply: return PrototypeArtResourceIds.MapNodeTreasure;
                case ExpeditionNodeType.Boss: return PrototypeArtResourceIds.MapNodeBoss;
                default: return PrototypeArtResourceIds.MapNodeNormal;
            }
        }

        private static string RingResourceId(
            ExpeditionNodeType type, ExpeditionMapNodeVisualState state)
        {
            if (type == ExpeditionNodeType.Boss) return PrototypeArtResourceIds.MapRingBoss;
            if (type == ExpeditionNodeType.EliteBattle || type == ExpeditionNodeType.HighRiskBattle)
                return PrototypeArtResourceIds.MapRingElite;
            switch (state)
            {
                case ExpeditionMapNodeVisualState.Current: return PrototypeArtResourceIds.MapRingCurrent;
                case ExpeditionMapNodeVisualState.Available: return PrototypeArtResourceIds.MapRingAvailable;
                case ExpeditionMapNodeVisualState.Completed: return PrototypeArtResourceIds.MapRingCompleted;
                default: return PrototypeArtResourceIds.MapRingFuture;
            }
        }

        private static Rect CenteredRect(Vector2 center, float size)
        {
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }

        private static uint Mix(uint value)
        {
            value ^= value >> 16;
            value *= 0x7FEB352Du;
            value ^= value >> 15;
            value *= 0x846CA68Bu;
            value ^= value >> 16;
            return value;
        }

        private static void DrawProceduralFallback(BackgroundLayerRole role, Rect canvas, float opacity)
        {
            switch (role)
            {
                case BackgroundLayerRole.MapSeaBase:
                    Fill(canvas, DeepMistSea, opacity);
                    break;
                case BackgroundLayerRole.MapDistantIslands:
                    Fill(new Rect(canvas.x + canvas.width * 0.05f, canvas.y + canvas.height * 0.21f,
                        canvas.width * 0.22f, canvas.height * 0.035f), DistantCliff, opacity * 0.24f);
                    Fill(new Rect(canvas.x + canvas.width * 0.70f, canvas.y + canvas.height * 0.61f,
                        canvas.width * 0.25f, canvas.height * 0.04f), DistantCliff, opacity * 0.20f);
                    break;
            }
        }

        private static void Fill(Rect rect, Color color, float opacity)
        {
            GUI.color = ColorWithOpacity(color, opacity);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Color ColorWithOpacity(Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            return color;
        }
    }

    internal sealed class LayeredHomeRenderer
    {
        private static readonly Color DeepSea = new Color(0.035f, 0.09f, 0.13f);
        private static readonly Color NearSea = new Color(0.06f, 0.18f, 0.22f);
        private static readonly Color Island = new Color(0.17f, 0.34f, 0.29f);
        private static readonly Color Cliff = new Color(0.20f, 0.23f, 0.22f);
        private static readonly Color Light = new Color(1f, 0.79f, 0.30f);
        private static readonly int[] FlagPlayback = { 0, 1, 2, 3, 2, 1 };
        private readonly ILayeredBackgroundSource source;

        public LayeredHomeRenderer(ILayeredBackgroundSource source)
        {
            this.source = source;
        }

        public void Draw(
            Rect canvas, float opacity, float animationTime, float chapterOffset, bool lighthouseLit,
            float safeTop, float safeContentHeight)
        {
            var layers = source.GetLayers(LayeredBackgroundKind.Home);
            foreach (var layer in layers)
            {
                var texture = source.LoadLayer(layer);
                if (texture == null)
                {
                    DrawProceduralFallback(layer.Role, canvas, opacity);
                    continue;
                }

                DrawDeliveredLayer(layer.Role, texture, canvas, opacity, animationTime, chapterOffset,
                    lighthouseLit, safeTop, safeContentHeight);
            }
        }

        private static void DrawDeliveredLayer(
            BackgroundLayerRole role,
            Texture2D texture,
            Rect canvas,
            float opacity,
            float animationTime,
            float chapterOffset,
            bool lighthouseLit,
            float safeTop,
            float safeContentHeight)
        {
            var scaleX = canvas.width / 1080f;
            var offset = chapterOffset * scaleX;
            var islandLayout = HomeUiV34Layout.IslandHero;
            var island = new Rect(islandLayout.X * 1080f, islandLayout.Y * safeContentHeight,
                islandLayout.Width * 1080f, islandLayout.Height * safeContentHeight);
            switch (role)
            {
                case BackgroundLayerRole.HomeSeaChart:
                    AspectCoverTextureRenderer.Draw(canvas, texture, opacity);
                    break;
                case BackgroundLayerRole.HomeCloudFar:
                {
                    var drift = Mathf.Sin(animationTime * Mathf.PI * 2f / 48f) * 45f * scaleX;
                    DrawTexture(new Rect(canvas.x - 80f * scaleX + drift, canvas.y + safeTop + 205f * scaleX,
                        1240f * scaleX, 465f * scaleX), texture, opacity * 0.45f);
                    break;
                }
                case BackgroundLayerRole.HomeBeamOverlay:
                    if (lighthouseLit)
                    {
                        var area = new Rect(0.30f * 1080f, 0.285f * safeContentHeight,
                            0.44f * 1080f, 0.20f * safeContentHeight);
                        area.x = canvas.x + area.x * scaleX + offset;
                        area.y = canvas.y + safeTop + area.y;
                        area.width *= scaleX;
                        area.height *= scaleX;
                        var previousMatrix = GUI.matrix;
                        var rotation = Mathf.Repeat(animationTime, 12f) / 12f * 360f;
                        // The complete page already runs through the SafeArea scale matrix.
                        // Rotate in design space so the beam remains pinned to the lamp room.
                        var pivot = new Vector3(area.center.x, area.center.y, 0f);
                        var localRotation = Matrix4x4.TRS(pivot, Quaternion.Euler(0f, 0f, rotation), Vector3.one) *
                                            Matrix4x4.TRS(-pivot, Quaternion.identity, Vector3.one);
                        GUI.matrix = previousMatrix * localRotation;
                        DrawTexture(area, texture, opacity * 0.88f);
                        GUI.matrix = previousMatrix;
                    }
                    break;
                case BackgroundLayerRole.HomeChapterIsland:
                    DrawTextureScaleToFit(new Rect(canvas.x + island.x * scaleX + offset,
                        canvas.y + safeTop + island.y, island.width * scaleX, island.height * scaleX),
                        texture, opacity);
                    break;
                case BackgroundLayerRole.HomeFlagSheet:
                {
                    var playbackIndex = Mathf.FloorToInt(animationTime * 3f) % FlagPlayback.Length;
                    var frame = FlagPlayback[Mathf.Max(0, playbackIndex)];
                    var area = new Rect(0.497f * 1080f, 0.348f * safeContentHeight,
                        0.16f * 1080f, 0.073f * safeContentHeight);
                    area.x = canvas.x + area.x * scaleX + offset;
                    area.y = canvas.y + safeTop + area.y;
                    area.width *= scaleX;
                    area.height *= scaleX;
                    DrawTextureFrame(area, texture, frame, 4, opacity);
                    break;
                }
                case BackgroundLayerRole.HomeCloudNear:
                {
                    var drift = Mathf.Sin(animationTime * Mathf.PI * 2f / 34f + 0.9f) * 70f * scaleX;
                    DrawTexture(new Rect(canvas.x - 100f * scaleX + drift,
                        canvas.y + safeTop + safeContentHeight - 590f * scaleX,
                        1280f * scaleX, 600f * scaleX), texture, opacity * 0.58f);
                    break;
                }
                default:
                    AspectCoverTextureRenderer.Draw(canvas, texture, opacity);
                    break;
            }
        }

        private static void DrawTexture(Rect area, Texture texture, float opacity)
        {
            GUI.color = ColorWithOpacity(Color.white, opacity);
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        private static void DrawTextureScaleToFit(Rect area, Texture texture, float opacity)
        {
            GUI.color = ColorWithOpacity(Color.white, opacity);
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        private static void DrawTextureFrame(
            Rect area, Texture texture, int frameIndex, int frameCount, float opacity)
        {
            GUI.color = ColorWithOpacity(Color.white, opacity);
            GUI.DrawTextureWithTexCoords(area, texture,
                new Rect(frameIndex / (float)frameCount, 0f, 1f / frameCount, 1f), true);
            GUI.color = Color.white;
        }

        private static void DrawProceduralFallback(BackgroundLayerRole role, Rect canvas, float opacity)
        {
            switch (role)
            {
                case BackgroundLayerRole.Base:
                case BackgroundLayerRole.HomeSeaChart:
                    Fill(canvas, DeepSea, opacity);
                    Fill(new Rect(canvas.x, canvas.y + canvas.height * 0.50f, canvas.width, canvas.height * 0.50f), NearSea, opacity);
                    break;
                case BackgroundLayerRole.HomeChapterIsland:
                    Fill(new Rect(canvas.x + canvas.width * 0.24f, canvas.y + canvas.height * 0.51f,
                        canvas.width * 0.52f, canvas.height * 0.22f), Cliff, opacity);
                    Fill(new Rect(canvas.x + canvas.width * 0.30f, canvas.y + canvas.height * 0.43f,
                        canvas.width * 0.40f, canvas.height * 0.20f), Island, opacity);
                    Fill(new Rect(canvas.x + canvas.width * 0.38f, canvas.y + canvas.height * 0.36f,
                        canvas.width * 0.24f, canvas.height * 0.14f), Island * 1.12f, opacity);
                    break;
                case BackgroundLayerRole.HomeCloudFar:
                case BackgroundLayerRole.HomeCloudNear:
                    Fill(new Rect(canvas.x, canvas.y + canvas.height * 0.70f, canvas.width,
                        canvas.height * 0.12f), new Color(0.50f, 0.66f, 0.68f), opacity * 0.10f);
                    break;
                case BackgroundLayerRole.HomeBeamOverlay:
                case BackgroundLayerRole.HomeFlagSheet:
                    break;
                case BackgroundLayerRole.LighthouseGlow:
                    Fill(new Rect(canvas.center.x - 42f, canvas.y + canvas.height * 0.27f, 84f, 190f),
                        new Color(0.82f, 0.86f, 0.78f), opacity);
                    Fill(new Rect(canvas.center.x - 58f, canvas.y + canvas.height * 0.25f, 116f, 42f), Light, opacity);
                    break;
                case BackgroundLayerRole.Beam:
                    Fill(new Rect(canvas.center.x + 40f, canvas.y + canvas.height * 0.25f,
                        canvas.width * 0.28f, 12f), Light, opacity * 0.55f);
                    break;
                case BackgroundLayerRole.RouteReflection:
                    Fill(new Rect(canvas.x + canvas.width * 0.43f, canvas.y + canvas.height * 0.76f,
                        canvas.width * 0.14f, 7f), Light, opacity * 0.38f);
                    Fill(new Rect(canvas.x + canvas.width * 0.46f, canvas.y + canvas.height * 0.82f,
                        canvas.width * 0.08f, 5f), Light, opacity * 0.22f);
                    break;
                case BackgroundLayerRole.ForegroundFog:
                    Fill(new Rect(canvas.x, canvas.y + canvas.height * 0.90f, canvas.width, canvas.height * 0.10f),
                        new Color(0.50f, 0.66f, 0.68f), opacity * 0.12f);
                    break;
            }
        }

        private static void Fill(Rect rect, Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static Color ColorWithOpacity(Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            return color;
        }
    }

    internal static class BattlePresentationLayout
    {
        public const float CompactCharacterHeight = 96f;
        public const float DefaultCharacterHeight = 128f;
        public const float CloseCharacterHeight = 160f;
        public const float StaticBattleCanvasHeight = 160f;

        // 1080x1920 reference contract. Taller screens translate the complete combat
        // rectangle as one unit so preparation and live battle always share positions.
        // Battlefield skins must keep the returned safe rectangle free of obstacles.
        public const float CombatSafeLeft = 80f;
        public const float CombatSafeTop = 400f;
        public const float CombatSafeWidth = 920f;
        public const float CombatSafeHeight = 960f;
        public const float PreparationPhaseTitleY = 230f;
        public const float PreparationSynergyY = 275f;
        public const float PreparationEnemyLabelY = 420f;
        public const float PreparationPlayerLabelY = 900f;
        public const float PreparationStartButtonY = 1370f;
        public const float PreparationToolsY = 1480f;
        public const float PreparationBenchY = 1580f;
        public const float PreparationBenchHeight = 270f;

        public static Rect CombatSafeArea
        {
            get { return new Rect(CombatSafeLeft, CombatSafeTop, CombatSafeWidth, CombatSafeHeight); }
        }

        public static Rect GetCombatSafeArea(float contentHeight)
        {
            var area = CombatSafeArea;
            area.y += GetCombatVerticalOffset(contentHeight);
            return area;
        }

        public static float GetCombatVerticalOffset(float contentHeight)
        {
            return PreparationBattlefieldLayoutMath.Calculate(contentHeight).CombatOffset;
        }

        public static float GetBattlefieldVerticalExpansion(float contentHeight)
        {
            return PreparationBattlefieldLayoutMath.Calculate(contentHeight).Expansion;
        }

        public static float GetPreparationPlayerLabelY(float contentHeight)
        {
            return PreparationBattlefieldLayoutMath.Calculate(contentHeight).PlayerLabelY;
        }

        public static float GetPreparationStartButtonY(float contentHeight)
        {
            return PreparationBattlefieldLayoutMath.Calculate(contentHeight).StartButtonY;
        }

        public static float GetPreparationToolsY(float contentHeight)
        {
            return PreparationBattlefieldLayoutMath.Calculate(contentHeight).ToolsY;
        }

        public static float GetPreparationBenchY(float contentHeight)
        {
            return PreparationBattlefieldLayoutMath.Calculate(contentHeight).BenchY;
        }

        public static Rect GetUnitSlotRect(bool isPlayer, int slot)
        {
            return GetUnitRectAtFootPosition(GetUnitFootPosition(isPlayer, slot));
        }

        public static Rect GetUnitSlotRect(bool isPlayer, int slot, float contentHeight)
        {
            return GetUnitRectAtFootPosition(GetUnitFootPosition(isPlayer, slot, contentHeight));
        }

        public static Vector2 GetUnitFootPosition(bool isPlayer, int slot)
        {
            var column = slot % 3;
            var row = slot / 3;
            var footY = isPlayer
                ? (row == 0 ? 1120f : 1310f) +
                  PreparationBattlefieldLayoutMath.PlayerFormationVerticalShift
                : (row == 0 ? 660f : 850f) +
                  PreparationBattlefieldLayoutMath.EnemyFormationVerticalShift;
            return new Vector2(220f + column * 320f, footY);
        }

        public static Vector2 GetUnitFootPosition(bool isPlayer, int slot, float contentHeight)
        {
            var position = GetUnitFootPosition(isPlayer, slot);
            var layout = PreparationBattlefieldLayoutMath.Calculate(contentHeight);
            var column = Mathf.Clamp(slot % 3, 0, 2);
            var row = Mathf.Clamp(slot / 3, 0, 1);
            var v94Blend = Mathf.Clamp01(layout.Expansion /
                                          PreparationBattlefieldLayoutMath.MaximumBattlefieldExpansion);
            position.x = Mathf.Lerp(position.x, FormationBattleV94Layout.Columns[column], v94Blend);
            if (isPlayer)
                position.y = row == 0 ? layout.PlayerFrontFootY : layout.PlayerBackFootY;
            else
                position.y = row == 0 ? layout.EnemyFrontFootY : layout.EnemyBackFootY;
            return position;
        }

        public static Rect GetUnitRectAtFootPosition(Vector2 footPosition)
        {
            return new Rect(footPosition.x - 120f, footPosition.y - 125f, 240f, 130f);
        }

        public static float GetSortY(bool isPlayer, int slot)
        {
            return GetUnitSlotRect(isPlayer, slot).yMax;
        }

        public static Rect GetCharacterCanvasRect(
            Rect animatedSlotRect,
            Sprite sprite,
            Vector2 fallbackPivot,
            float visibleContentHeightNormalized,
            float desiredVisibleContentHeight = DefaultCharacterHeight)
        {
            var canvasHeight = desiredVisibleContentHeight /
                Mathf.Max(0.01f, visibleContentHeightNormalized);
            var aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var width = canvasHeight * aspect;
            var pivot = sprite.rect.width > 0f && sprite.rect.height > 0f
                ? new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height)
                : fallbackPivot;
            var foot = new Vector2(animatedSlotRect.center.x, animatedSlotRect.yMax - 5f);
            return new Rect(
                foot.x - width * pivot.x,
                foot.y - canvasHeight * (1f - pivot.y),
                width,
                canvasHeight);
        }

        public static Rect GetStaticBattleCanvasRect(
            Rect animatedSlotRect,
            Sprite sprite,
            float tierVisualScale)
        {
            var canvasHeight = StaticBattleCanvasHeight * Mathf.Max(0.01f, tierVisualScale);
            var aspect = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
            var width = canvasHeight * aspect;
            var foot = new Vector2(animatedSlotRect.center.x, animatedSlotRect.yMax - 5f);
            return new Rect(
                foot.x - width * StaticBattleArtContract.PivotX,
                foot.y - canvasHeight * (1f - StaticBattleArtContract.PivotY),
                width,
                canvasHeight);
        }
    }
}
