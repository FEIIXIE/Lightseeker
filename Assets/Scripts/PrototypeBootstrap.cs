using System;
using System.Collections;
using System.Collections.Generic;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Localization;
using SeekerAutoChess.Prototype.Persistence;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    /// <summary>
    /// Temporary first-playable prototype. It deliberately creates all visual elements in code,
    /// so gameplay can be tested before permanent art, prefabs, or network services exist.
    /// </summary>
    public sealed class PrototypeBootstrap : MonoBehaviour
    {
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;
        private const float ContentLeft = 55f;
        private const float ContentRight = 1025f;
        private const float ContentWidth = ContentRight - ContentLeft;
        private const float HeaderButtonWidth = 145f;
        private const float HeaderButtonGap = 10f;
        private const string ClientVersionLabel = "v0.43.96";
        private const float BattleUnitBaseAlignmentDropPercent = 0.05f;
        private static readonly Dictionary<string, Sprite> battleVfxSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> battleShieldOverlayOpacities =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> diagnosticQualityOverrides =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private const int RunEconomyRulesVersion = 1;
        private const int RoguelikeRulesVersion = RoguelikeEnhancementRules.RulesVersion;
        private const int AttackSpeedRulesVersion = 0;
        private const int FinalRound = ExpeditionMapGraph.ChapterLayerCount;
        private const int MinimumRecruitCost = 3;
        private const int ShopOfferCount = 3;
        private const int MinimumDeckSize = 3;
        private const int MaximumDeckSize = 8;
        private const int BenchCapacity = 6;
        private const int ShopRefreshCost = 2;
        private const int EquipmentDetailFontSize = 22;
        private static readonly Rect WarehouseEquipmentDetailPanel = new Rect(215f, 350f, 650f, 1110f);
        // v0.27 persists first-island Boss and lighthouse progression.
        // Existing saves remain readable and do not infer an unverified Boss clear.
        private const int SaveSchemaVersion = 26;
        private const int MinimumCompatibleSaveVersion = 5;
        private const string SaveKey = "seeker-auto-chess.prototype.run.v1";
        private const string LanguagePreferenceKey = "seeker-auto-chess.preference.language.v1";
        private static readonly Dictionary<Color, Texture2D> TextureCache = new Dictionary<Color, Texture2D>();
        private static Texture2D groundShadowTexture;
        private Font sourceHanSerifRegularFont;
        private Font sourceHanSerifSemiBoldFont;
        private Font unpredictableTextFallbackFont;
        private Font battleFloatingNumberFont;
        private bool battleFloatingNumberFontLoadAttempted;
        private bool sourceHanFontWarningLogged;
        private readonly Texture2D[] runLevelCrescentTextures = new Texture2D[RunUnitRules.MaximumRunLevel];

        // Kept for one hot-reload cycle so a running v0.5 editor session can be
        // migrated into RunRoster without losing the user's current expedition.
        private readonly UnitData[] playerSlots = new UnitData[6];
        private UnitData[] benchSlots = new UnitData[BenchCapacity];
        private readonly UnitData[] defenseSlots = new UnitData[6];
        private readonly List<UnitData> enemyUnits = new List<UnitData>();
        private string[] activeDeckUnitIds = { "unit.guardian", "unit.healer", "unit.thornGuard" };
        private CharacterCatalog characterCatalog;
        private PlayerCollection playerCollection;
        private EquipmentCatalog equipmentCatalog;
        private EquipmentInventory equipmentInventory;
        private ExpeditionRewardLedger expeditionRewards = new ExpeditionRewardLedger();
        private LighthouseRepairState chapterOneLighthouse = new LighthouseRepairState();
        private Dictionary<string, EquipmentLoadoutSnapshot> runEquipmentSnapshots =
            new Dictionary<string, EquipmentLoadoutSnapshot>(StringComparer.Ordinal);
        private string selectedEquipmentCharacterInstanceId;
        private RunDeckSelection nextRunDeckSelection;
        private RunDeckSelection deckDraftSelection;
        private RunDeckSnapshot runDeck;
        private RunRoster runRoster = new RunRoster(6, BenchCapacity);
        private string[] shopOffers = new string[ShopOfferCount];
        private int[] shopOfferLevels = new int[ShopOfferCount];
        private string[] shopOfferIds = new string[ShopOfferCount];
        private int[] shopOfferListedPrices = new int[ShopOfferCount];
        private bool[] shopOfferSold = new bool[ShopOfferCount];
        private int shopRefreshSerial;
        private string runId = string.Empty;
        private readonly RunEconomyActionLedger runEconomyActions = new RunEconomyActionLedger();
        private readonly RoguelikeBuildState roguelikeBuild = new RoguelikeBuildState();
        private ChapterOneEncounter currentEncounter;
        private ExpeditionEconomyState expeditionEconomy = new ExpeditionEconomyState();
        private ExpeditionFlow expeditionFlow = new ExpeditionFlow();
        private ExpeditionMapGraph expeditionMap;
        private uint expeditionMapSeed;
        private string currentMapNodeId;
        private bool expeditionMapDiagnosticsOpen;
        private int expeditionMapDiagnosticSeedIndex;
        private int expeditionMapDiagnosticNodeIndex;
        private string eventFamilyTacticalNodeId;
        private int eventFamilyTacticalSelection;
        private bool runEconomyDiagnosticsOpen;
        private bool roguelikeBuildDiagnosticsOpen;
        private int roguelikeDiagnosticStateIndex;
        private bool roguelikeDiagnosticSuppressChoiceSet;
        private int lastBattleEventCount;
        private string lastBattleHash = "none";
        private readonly BattlePresentationModel battlePresentation = new BattlePresentationModel();
        private readonly List<BattleFloatingNumber> battleFloatingNumbers = new List<BattleFloatingNumber>();
        private readonly List<BattleFeedbackVisual> battleFeedbackVisuals =
            new List<BattleFeedbackVisual>();
        private readonly HashSet<string> consumedBattleFeedbackKeys =
            new HashSet<string>(StringComparer.Ordinal);
        private readonly SpatialBattleGrayboxReplay spatialG1Replay = new SpatialBattleGrayboxReplay();
        private SpatialBattleInput spatialG1Input;
        private SpatialBattleResult spatialG1Result;
        private bool spatialG1GrayboxActive;
        private bool spatialG1Paused;
        private readonly SpatialBattleG2GrayboxReplay spatialG2Replay = new SpatialBattleG2GrayboxReplay();
        private SpatialBattleG2Input spatialG2Input;
        private SpatialBattleG2Result spatialG2Result;
        private bool spatialG2GrayboxActive;
        private bool spatialG2Paused;
        private int spatialG2VectorIndex;
        private IReadOnlyList<BattleUnitPresentationState> battlePlayers
        {
            get { return battlePresentation.PlayerUnits; }
        }
        private IReadOnlyList<BattleUnitPresentationState> battleEnemies
        {
            get { return battlePresentation.EnemyUnits; }
        }
        private BattlePresentationCue battleVisual
        {
            get { return battlePresentation.ActiveCue; }
        }
        private bool battlePresentationActive;
        private bool battlePaused;
        private bool battleControlsEnabled;
        private float battleSceneOpacity;
        private PrototypeLocalization localization;
        private PrototypeArtCatalog artCatalog;
        private PrototypeUiSkin uiSkin;
        private PrototypeUiSkin expeditionTrialUiSkin;
        private LayeredBattlefieldRenderer battlefieldRenderer;
        private LayeredExpeditionMapRenderer expeditionMapRenderer;
        private LayeredHomeRenderer homeRenderer;
        private GameLanguage language;
        private int coins = 10;
        private int health = 10;
        private int round = 1;
        private int selectedRunSlot = -1;
        private bool preparationShopOpen;
        private bool preparationUnitDetailOpen;
        private int preparationDetailSlot = -1;
        private bool preparationDetailIsMonster;
        private int preparationDetailEnemyIndex = -1;
        private int preparationPointerSlot = -1;
        private Vector2 preparationPointerDown;
        private Vector2 preparationPointerPosition;
        private bool preparationPointerHeld;
        private bool preparationPointerDragging;
        private bool preparationBackpackExpanded;
        private bool formationOverlayTransitionActive;
        private float formationOverlayTransitionProgress;
        private string selectedFormationSynergyId;
        private bool formationSynergyOverflowOpen;
        private string selectedFormationBuffId;
        private bool formationBuffOverflowOpen;
        private int runAttackBonus;
        private int runHealthBonus;
        private int encounterPowerBonus;
        private int encounterReward = 4;
        private bool currentEncounterIsElite;
        private bool runWon;
        private bool runAbandoned;
        private bool confirmAbandon;
        private int finalScore;
        private int expeditionEnergy = ExpeditionEnergyRules.InitialEnergy;
        private int skrBalance;
        private bool currentExpeditionRewardsEnabled = true;
        private bool expeditionEntryCommitted;
        private int tideCrystalBalance;
        private int stoneBalance;
        private int lampOilBalance;
        private int mistLensBalance;
        private int nextLocalEquipmentSerial = 1;
        private ScreenView currentView = ScreenView.Home;
        private string homeNoticeKey = "home.notice.default";
        private bool deckEditing;
        private bool characterDetailOpen;
        private string detailCharacterInstanceId;
        private string detailEquipmentPreviewId;
        private Vector2 deckCollectionScroll;
        private Vector2 detailEquipmentScroll;
        private Vector2 warehouseEquipmentScroll;
        private Vector2 expeditionMapScroll;
        private string expeditionMapScrollAnchorNodeId;
        private Vector2 expeditionMapPointerDown;
        private Vector2 expeditionMapPointerLast;
        private bool expeditionMapPointerHeld;
        private bool expeditionMapPointerDragging;
        private float expeditionMapZoom = 1f;
        private float expeditionMapPinchDistance;
        private float homeAnimationTime;
        private float homeChapterOffset;
        private float homeChapterOffsetVelocity;
        private Vector2 homeChapterPointerDown;
        private float homeChapterPointerStartOffset;
        private bool homeChapterPointerHeld;
        private bool homeChapterPointerDragging;
        private int homeTopMenu;
        private float layoutCanvasHeight = DesignHeight;
        private float layoutScale = 1f;
        private float layoutSafeTop;
        private float layoutSafeBottom;
        private float layoutContentHeight = DesignHeight;
        private WarehouseTab warehouseTab = WarehouseTab.All;
        private bool warehouseEquipmentDetailOpen;
        private string warehouseEquipmentDetailId;
        private string deckEditorMessageKey = "deck.editorHint";
        private int selectedDefenseSlot = -1;
        private string defenseMessageKey = "defense.subtitle";
        private string statusKey = "status.recruit";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateIfNeeded()
        {
            if (FindObjectOfType<PrototypeBootstrap>() != null)
            {
                return;
            }

            var host = new GameObject("Prototype Bootstrap");
            host.AddComponent<PrototypeBootstrap>();
        }

        private void Awake()
        {
            EnsureRuntimeState();
            localization = PrototypeLocalization.CreateDefault();
            language = DetectSystemLanguage();
            GameLanguage savedLanguage;
            if (PrototypeLocalization.TryParseLanguageCode(
                    PlayerPrefs.GetString(LanguagePreferenceKey, string.Empty), out savedLanguage))
            {
                language = savedLanguage;
            }
            if (!LoadGame())
            {
                CreateNewExpeditionMap();
                runId = "run-" + Guid.NewGuid().ToString("N");
                runEconomyActions.Reset();
                shopRefreshSerial = 0;
                RefreshAllShopOffers();
                expeditionFlow.BeginAtRoute();
                CreateEnemies();
                CreateStarterDefense();
                SaveGame();
            }
            preparationShopOpen = expeditionFlow.CanPrepare;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveGame();
        }

        private void OnApplicationQuit()
        {
            SaveGame();
        }

        private void Update()
        {
            AdvanceBattleFloatingNumbers(Time.unscaledDeltaTime);
            AdvanceBattleFeedback(Time.unscaledDeltaTime);
            if (spatialG1GrayboxActive && !spatialG1Paused)
                spatialG1Replay.Advance(Time.unscaledDeltaTime);
            if (spatialG2GrayboxActive && !spatialG2Paused)
                spatialG2Replay.Advance(Time.unscaledDeltaTime);
            if (currentView == ScreenView.Home)
            {
                homeAnimationTime += Time.unscaledDeltaTime;
                if (!homeChapterPointerHeld)
                {
                    homeChapterOffset = Mathf.SmoothDamp(homeChapterOffset, 0f,
                        ref homeChapterOffsetVelocity, 0.24f, Mathf.Infinity, Time.unscaledDeltaTime);
                }
            }
            else
            {
                homeChapterPointerHeld = false;
                homeChapterPointerDragging = false;
                homeChapterOffset = 0f;
                homeChapterOffsetVelocity = 0f;
            }
        }

        private string T(string key)
        {
            if (localization == null) localization = PrototypeLocalization.CreateDefault();
            return localization.Translate(language, key);
        }

        private static GameLanguage DetectSystemLanguage()
        {
            return Application.systemLanguage == SystemLanguage.Chinese ||
                   Application.systemLanguage == SystemLanguage.ChineseSimplified ||
                   Application.systemLanguage == SystemLanguage.ChineseTraditional
                ? GameLanguage.ChineseSimplified
                : GameLanguage.English;
        }

        private void SaveLanguagePreference()
        {
            PlayerPrefs.SetString(LanguagePreferenceKey, PrototypeLocalization.GetLanguageCode(language));
            PlayerPrefs.Save();
        }

        private void EnsureRuntimeState()
        {
            if (expeditionFlow == null) expeditionFlow = new ExpeditionFlow();
            if (expeditionMap == null)
            {
                expeditionMapSeed = expeditionMapSeed == 0 ? 0x51EA2C31u : expeditionMapSeed;
                expeditionMap = ExpeditionMapGraph.CreateChapterOne(expeditionMapSeed);
            }
            if (string.IsNullOrEmpty(currentMapNodeId) || expeditionMap.GetNode(currentMapNodeId) == null)
            {
                currentMapNodeId = expeditionMap.GetLayer(Mathf.Clamp(round, 1, FinalRound))[0].Id;
            }
            if (localization == null) localization = PrototypeLocalization.CreateDefault();
            if (artCatalog == null) artCatalog = new PrototypeArtCatalog();
            if (uiSkin == null) uiSkin = new PrototypeUiSkin();
            if (expeditionTrialUiSkin == null) expeditionTrialUiSkin = new PrototypeUiSkin("03", true);
            if (battlefieldRenderer == null)
            {
                battlefieldRenderer = new LayeredBattlefieldRenderer(new ResourcesLayeredBackgroundSource());
            }
            if (expeditionMapRenderer == null)
            {
                expeditionMapRenderer = new LayeredExpeditionMapRenderer(new ResourcesLayeredBackgroundSource());
            }
            if (homeRenderer == null)
            {
                homeRenderer = new LayeredHomeRenderer(new ResourcesLayeredBackgroundSource());
            }
            if (characterCatalog == null) characterCatalog = CharacterCatalog.CreatePrototype();
            if (playerCollection == null) playerCollection = PlayerCollection.CreatePrototype(characterCatalog);
            if (equipmentCatalog == null) equipmentCatalog = EquipmentCatalog.CreatePrototype();
            if (equipmentInventory == null) equipmentInventory = EquipmentInventory.CreatePrototype(equipmentCatalog);
            if (expeditionRewards == null) expeditionRewards = new ExpeditionRewardLedger();
            if (expeditionEconomy == null) expeditionEconomy = new ExpeditionEconomyState();
            if (chapterOneLighthouse == null) chapterOneLighthouse = new LighthouseRepairState();
            if (runEquipmentSnapshots == null)
            {
                runEquipmentSnapshots = new Dictionary<string, EquipmentLoadoutSnapshot>(StringComparer.Ordinal);
            }
            if (string.IsNullOrEmpty(selectedEquipmentCharacterInstanceId) && playerCollection.Entries.Count > 0)
            {
                selectedEquipmentCharacterInstanceId = playerCollection.Entries[0].CharacterInstanceId;
            }
            if (activeDeckUnitIds == null || activeDeckUnitIds.Length == 0)
            {
                activeDeckUnitIds = new[] { "unit.guardian", "unit.healer", "unit.thornGuard" };
            }
            if (nextRunDeckSelection == null)
            {
                nextRunDeckSelection = new RunDeckSelection(
                    playerCollection, activeDeckUnitIds, MaximumDeckSize, MinimumDeckSize);
            }
            if (benchSlots == null || benchSlots.Length != BenchCapacity)
            {
                benchSlots = new UnitData[BenchCapacity];
            }
            if (runRoster == null || runRoster.BoardCapacity != 6 || runRoster.BenchCapacity != BenchCapacity)
            {
                runRoster = new RunRoster(6, BenchCapacity);
            }
            MigrateLegacyRuntimeRoster();
            if (runDeck == null) runDeck = nextRunDeckSelection.CreateSnapshot();
            var shopNeedsRefresh = shopOffers == null || shopOffers.Length != ShopOfferCount;
            if (!shopNeedsRefresh)
            {
                shopNeedsRefresh = shopRefreshSerial == 0;
                for (var i = 0; shopNeedsRefresh && i < shopOffers.Length; i++)
                {
                    if (!string.IsNullOrEmpty(shopOffers[i]))
                    {
                        shopNeedsRefresh = false;
                        break;
                    }
                }
            }
            if (shopNeedsRefresh)
            {
                shopOffers = new string[ShopOfferCount];
                RefreshAllShopOffers();
            }
            if (shopOfferLevels == null || shopOfferLevels.Length != ShopOfferCount)
                shopOfferLevels = new[] { 1, 1, 1 };
            if (shopOfferIds == null || shopOfferIds.Length != ShopOfferCount)
                shopOfferIds = new string[ShopOfferCount];
            if (shopOfferListedPrices == null || shopOfferListedPrices.Length != ShopOfferCount)
                shopOfferListedPrices = new int[ShopOfferCount];
            if (shopOfferSold == null || shopOfferSold.Length != ShopOfferCount)
                shopOfferSold = new bool[ShopOfferCount];
        }

        private void CreateEnemies()
        {
            enemyUnits.Clear();
            var node = expeditionMap == null ? null : expeditionMap.GetNode(currentMapNodeId);
            if (node == null || !IsBattleNode(node.Type)) return;
            currentEncounter = ChapterOneEncounterRules.Generate(
                expeditionMapSeed, node, !HasCompletedNormalBattle());
            encounterReward = currentEncounter.CoinReward;
            currentEncounterIsElite = currentEncounter.IsElite;
            foreach (var encounterUnit in currentEncounter.Units)
            {
                enemyUnits.Add(CreateUnit(
                    encounterUnit.DefinitionId,
                    UnitBasePower(encounterUnit.DefinitionId),
                    EnemyColor(encounterUnit.DefinitionId),
                    encounterUnit.RunLevel));
            }
        }

        private bool HasCompletedNormalBattle()
        {
            if (expeditionMap == null || expeditionRewards == null) return false;
            foreach (var nodeId in expeditionRewards.CompletedNodeIds)
            {
                var node = expeditionMap.GetNode(nodeId);
                if (node != null && node.Type == ExpeditionNodeType.NormalBattle) return true;
            }
            return false;
        }

        private void OnGUI()
        {
            EnsureRuntimeState();
            ApplyUnifiedGameFont(GUI.skin);
            ActiveUiSkin.ApplyGlobalControls(GUI.skin);
            // IMGUI tint is global state. Reset it at the start of every event so a
            // translucent texture drawn on one page cannot wash out labels on the next.
            GUI.color = Color.white;
            GUI.contentColor = Color.white;

            // Keep the established 1080-wide design space, but let its height follow
            // the actual portrait window. Backgrounds cover the physical window while
            // controls are translated into Screen.safeArea below.
            GUI.matrix = Matrix4x4.identity;
            GUI.color = new Color(0.04f, 0.07f, 0.10f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var layout = AdaptivePortraitLayout.Calculate(
                Screen.width, Screen.height, Screen.safeArea, DesignWidth);
            layoutScale = layout.Scale;
            layoutCanvasHeight = layout.CanvasHeight;
            layoutSafeTop = layout.SafeTop;
            layoutSafeBottom = layout.SafeBottom;
            layoutContentHeight = layout.SafeContentHeight;
            var rootMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(layout.Scale, layout.Scale, 1f));
            GUI.matrix = rootMatrix;
            var previousBackgroundColor = GUI.backgroundColor;

            DrawBackground();
            if (spatialG2GrayboxActive)
            {
                DrawSpatialG2Graybox();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (spatialG1GrayboxActive)
            {
                DrawSpatialG1Graybox();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Home && !battlePresentationActive)
            {
                // E36 is a pixel-locked 1080 x 2400 composition. Home deliberately
                // stays in full-canvas coordinates instead of inheriting the
                // safe-area translation used by the prototype's older 9:16 pages.
                DrawHomeScreen();
                DrawMainNavigation();
                DrawBuildVersionOverlay(layoutSafeTop);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            GUI.matrix = rootMatrix * Matrix4x4.Translate(new Vector3(0f, layoutSafeTop, 0f));
            if (IsExpeditionUiTrialPage)
            {
                // Tone down the cream v1.9 surfaces on high-brightness phone panels;
                // GUI.backgroundColor tints control backgrounds without muting text.
                GUI.backgroundColor = new Color(0.82f, 0.85f, 0.82f);
            }
            if (battlePresentationActive)
            {
                DrawBattleScreen(battleSceneOpacity);
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            DrawStandardInterface();
            if (IsPreparationScreen && preparationUnitDetailOpen)
            {
                // V11.4.4 requires the snapshot/blur/dim layers to remain outside the
                // safe-area transform. Only the modal itself uses safe-area metrics.
                GUI.matrix = rootMatrix;
                DrawPreparationUnitDetail();
                GUI.matrix = rootMatrix * Matrix4x4.Translate(new Vector3(0f, layoutSafeTop, 0f));
            }
            DrawBuildVersionOverlay(0f);
            GUI.backgroundColor = previousBackgroundColor;
        }

        private void DrawBuildVersionOverlay(float topOffset)
        {
            // This is a non-layout overlay: every page gets the same small build marker
            // without shifting or resizing any existing header or page content.
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(ContentRight - 180f, topOffset + 5f, 180f, 24f), ClientVersionLabel,
                Style(12, Color.white, TextAnchor.UpperRight));
            GUI.color = previousColor;
            GUI.contentColor = previousContentColor;
        }

        private void DrawStandardInterface()
        {
            if (currentView == ScreenView.Home)
            {
                DrawHomeScreen();
                DrawMainNavigation();
                return;
            }
            // E46 owns the complete 841-pixel page, including its top controls. Route
            // these two states before the legacy header so the delivered composition is
            // scaled once as a whole instead of being mixed with 1080-layout elements.
            if (currentView == ScreenView.Expedition && expeditionFlow != null)
            {
                if (expeditionFlow.CanChooseUpgrade)
                {
                    DrawPostBattleUpgradeScreen();
                    return;
                }
                if (expeditionFlow.CanPrepare && preparationShopOpen)
                {
                    DrawPreparationShopModal();
                    return;
                }
                if (expeditionFlow.CanResolveNode && expeditionMap != null)
                {
                    var resolvingNode = expeditionMap.GetNode(currentMapNodeId);
                    if (resolvingNode != null && IsEventFamilyNodeType(resolvingNode.Type))
                    {
                        DrawNonBattleNodeScreen();
                        return;
                    }
                }
                if (expeditionFlow.CanChooseRoute)
                {
                    DrawChapterMapV62Screen();
                    return;
                }
            }
            DrawHeader();
            if (currentView == ScreenView.Deck)
            {
                DrawDeckScreen();
                if (!deckEditing && !characterDetailOpen) DrawMainNavigation();
                return;
            }
            if (currentView == ScreenView.Store)
            {
                DrawPlaceholderMainScreen("store.title", "store.subtitle");
                DrawMainNavigation();
                return;
            }
            if (currentView == ScreenView.Arena)
            {
                DrawArenaScreen();
                DrawMainNavigation();
                return;
            }
            if (currentView == ScreenView.Leaderboard)
            {
                DrawPlaceholderMainScreen("leaderboard.title", "leaderboard.subtitle");
                DrawMainNavigation();
                return;
            }
            if (currentView == ScreenView.Harbor)
            {
                DrawPlaceholderMainScreen("harbor.title", "harbor.subtitle");
                DrawMainNavigation();
                return;
            }
            if (currentView == ScreenView.Warehouse)
            {
                if (warehouseEquipmentDetailOpen)
                {
                    GUI.enabled = false;
                    DrawWarehouseScreen();
                    GUI.enabled = true;
                    GUI.color = new Color(0.01f, 0.025f, 0.035f, 0.58f);
                    GUI.DrawTexture(new Rect(0f, 135f, DesignWidth, layoutContentHeight - 135f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    if (DrawWarehouseEquipmentDetailBackdrop(WarehouseEquipmentDetailPanel)) return;
                    DrawWarehouseEquipmentDetailScreen();
                }
                else DrawWarehouseScreen();
                return;
            }
            if (currentView == ScreenView.Defense)
            {
                DrawDefenseScreen();
                return;
            }
            if (expeditionFlow.CanChooseRoute)
            {
                DrawChapterMapV62Screen();
                return;
            }
            if (expeditionFlow.CanResolveNode)
            {
                DrawNonBattleNodeScreen();
                return;
            }
            if (expeditionFlow.CanChooseUpgrade) return;
            if (expeditionFlow.IsComplete)
            {
                DrawExpeditionResultScreen();
                return;
            }
            DrawPreparationScreen();
        }

        private void DrawBackground()
        {
            GUI.color = new Color(0.04f, 0.07f, 0.10f);
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, layoutCanvasHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (currentView == ScreenView.Home)
            {
                var lockedBaseId = language == GameLanguage.English
                    ? PrototypeArtResourceIds.HomeV38StaticBaseEnglish
                    : PrototypeArtResourceIds.HomeV38StaticBase;
                var lockedBase = artCatalog.GetHomeV38Texture(lockedBaseId);
                if (lockedBase != null)
                {
                    GUI.DrawTexture(new Rect(0f, 0f, HomeUiV38Layout.ReferenceWidth,
                        HomeUiV38Layout.ReferenceHeight), lockedBase, ScaleMode.StretchToFill, false);
                }
                else
                {
                    homeRenderer.Draw(new Rect(0f, 0f, DesignWidth, layoutCanvasHeight), 1f, homeAnimationTime,
                        homeChapterOffset, chapterOneLighthouse.IsRepaired, layoutSafeTop, layoutContentHeight);
                }
                return;
            }
            if (battlePresentationActive || IsPreparationScreen || IsPostBattleExpeditionScreen ||
                IsExpeditionEventFamilyNodeScreen)
            {
                // The interactive preparation slots are drawn from the same coordinates
                // as combat. Do not also paint the legacy baked deployment markers.
                battlefieldRenderer.Draw(new Rect(0f, 0f, DesignWidth, layoutCanvasHeight),
                    battlePresentationActive ? battleSceneOpacity : 1f, 0f);
            }
            GUI.color = Color.white;
        }

        private bool IsPreparationScreen
        {
            get
            {
                return currentView == ScreenView.Expedition && !battlePresentationActive &&
                       expeditionFlow != null && expeditionFlow.CanPrepare;
            }
        }

        private bool IsPostBattleExpeditionScreen
        {
            get
            {
                return currentView == ScreenView.Expedition && !battlePresentationActive &&
                       expeditionFlow != null && (expeditionFlow.CanChooseUpgrade || expeditionFlow.IsComplete);
            }
        }

        private bool IsExpeditionEventFamilyNodeScreen
        {
            get
            {
                if (currentView != ScreenView.Expedition || battlePresentationActive || expeditionFlow == null ||
                    !expeditionFlow.CanResolveNode || expeditionMap == null) return false;
                var node = expeditionMap.GetNode(currentMapNodeId);
                return node != null && IsEventFamilyNodeType(node.Type);
            }
        }

        private static bool IsEventFamilyNodeType(ExpeditionNodeType type)
        {
            return type == ExpeditionNodeType.Event || type == ExpeditionNodeType.Treasure ||
                   type == ExpeditionNodeType.Supply || type == ExpeditionNodeType.Shop ||
                   type == ExpeditionNodeType.Rest;
        }

        private static ExpeditionNodeType EffectiveNodeType(ExpeditionNodeType type)
        {
            return type == ExpeditionNodeType.Supply ? ExpeditionNodeType.Treasure : type;
        }

        private bool IsExpeditionUiTrialPage
        {
            get
            {
                return false;
            }
        }

        private PrototypeUiSkin ActiveUiSkin
        {
            get { return IsExpeditionUiTrialPage ? expeditionTrialUiSkin : uiSkin; }
        }

        private Color PreparationTextPrimary
        {
            get { return IsExpeditionUiTrialPage ? new Color(0.15f, 0.23f, 0.25f) : Color.white; }
        }

        private Color PreparationTextMuted
        {
            get
            {
                return IsExpeditionUiTrialPage
                    ? new Color(0.25f, 0.35f, 0.37f)
                    : new Color(0.74f, 0.84f, 0.89f);
            }
        }

        private void DrawBattleScreen(float opacity)
        {
            opacity = Mathf.Clamp01(opacity);
            battlePresentation.UpdateIdlePoses(Time.unscaledTime);
            DrawHeader();
            var renderUnits = new List<BattleUnitPresentationState>();
            renderUnits.AddRange(battleEnemies);
            renderUnits.AddRange(battlePlayers);
            renderUnits.Sort(CompareBattleRenderOrder);
            foreach (var unit in renderUnits) DrawBattleAvatar(unit, opacity);
            DrawBattleOverlay();
            DrawFormationBattleBuffGroup(opacity);
            DrawFormationBattleSynergyPopupLayer(opacity);
            DrawBattleFeedback(false);
            DrawBattleFloatingNumbers();

            if (battlePaused && opacity >= 0.82f)
            {
                var pauseY = 760f + BattlePresentationLayout.GetCombatVerticalOffset(layoutContentHeight);
                GUI.color = new Color(0f, 0f, 0f, 0.62f);
                GUI.DrawTexture(new Rect(0f, pauseY, DesignWidth, 300f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(80f, pauseY + 65f, 920f, 150f), T("battle.paused"),
                    Style(38, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            }
        }

        private void DrawSpatialG1LaunchButton()
        {
            var area = new Rect(820f, 338f, 205f, 52f);
            if (GUI.Button(area,
                    language == GameLanguage.ChineseSimplified ? "G1移动灰盒" : "G1 MOVE GRAYBOX",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                BeginSpatialG1Graybox();
        }

        private void DrawSpatialG2LaunchButton()
        {
            var area = new Rect(585f, 338f, 225f, 52f);
            if (GUI.Button(area,
                    language == GameLanguage.ChineseSimplified ? "G2支援拥堵" : "G2 SUPPORT / CROWD",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                BeginSpatialG2Graybox(0);
        }

        private void BeginSpatialG2Graybox(int vectorIndex)
        {
            spatialG2VectorIndex = Mathf.Clamp(vectorIndex, 0, 2);
            spatialG2Input = spatialG2VectorIndex == 0
                ? SpatialBattleG2Simulator.CreateG2A()
                : spatialG2VectorIndex == 1
                    ? SpatialBattleG2Simulator.CreateG2B()
                    : SpatialBattleG2Simulator.CreateG2C();
            spatialG2Result = SpatialBattleG2Simulator.Run(spatialG2Input);
            spatialG2Replay.Begin(spatialG2Input, spatialG2Result);
            spatialG2GrayboxActive = true;
            spatialG2Paused = false;
        }

        private void BeginSpatialG1Graybox()
        {
            spatialG1Input = SpatialBattleSimulator.CreateG1Baseline();
            spatialG1Result = SpatialBattleSimulator.Run(spatialG1Input);
            spatialG1Replay.Begin(spatialG1Input, spatialG1Result);
            spatialG1GrayboxActive = true;
            spatialG1Paused = false;
        }

        private void DrawSpatialG1Graybox()
        {
            var field = new Rect(80f, 380f, 920f, 1510f);
            GUI.color = new Color(0.02f, 0.09f, 0.11f, 0.76f);
            GUI.DrawTexture(field, Texture2D.whiteTexture);
            GUI.color = new Color(0.24f, 0.78f, 0.80f, 0.22f);
            for (var row = 1; row < 8; row++)
                GUI.DrawTexture(new Rect(field.x, field.y + field.height * row / 8f,
                    field.width, 2f), Texture2D.whiteTexture);
            for (var column = 1; column < 5; column++)
                GUI.DrawTexture(new Rect(field.x + field.width * column / 5f, field.y,
                    2f, field.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(70f, 125f, 940f, 64f),
                "G1 · battle-rules-v2 · 2 MELEE + 1 RANGED / SIDE",
                Style(24, new Color(0.95f, 0.82f, 0.43f), TextAnchor.MiddleCenter, FontStyle.Bold));
            GUI.Label(new Rect(70f, 188f, 940f, 42f),
                language == GameLanguage.ChineseSimplified
                    ? "独立无奖励灰盒 · 既有静态图占位 · 非完整动画 · 正式 v1 未切换"
                    : "NO-REWARD ISOLATED GRAYBOX · STATIC PLACEHOLDERS · V1 UNCHANGED",
                Style(16, new Color(0.72f, 0.88f, 0.88f), TextAnchor.MiddleCenter));
            var hashText = spatialG1Result == null ? string.Empty :
                "event " + spatialG1Result.EventHash + "   final " + spatialG1Result.FinalStateHash;
            GUI.Label(new Rect(70f, 232f, 940f, 34f), hashText,
                Style(14, new Color(0.75f, 0.82f, 0.82f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(70f, 272f, 940f, 34f),
                "tick " + spatialG1Replay.CurrentTick + " / " +
                (spatialG1Result == null ? 0 : spatialG1Result.CompletedTicks),
                Style(15, Color.white, TextAnchor.MiddleCenter));

            DrawSpatialG1TargetLines(field);
            var renderUnits = new List<SpatialGrayboxUnitView>(spatialG1Replay.Units);
            renderUnits.Sort((left, right) =>
            {
                var leftPosition = left.InterpolatedLogicPosition(spatialG1Replay.InterpolationAlpha);
                var rightPosition = right.InterpolatedLogicPosition(spatialG1Replay.InterpolationAlpha);
                var order = leftPosition.y.CompareTo(rightPosition.y);
                return order != 0 ? order : string.CompareOrdinal(left.Id, right.Id);
            });
            foreach (var unit in renderUnits) DrawSpatialG1Unit(field, unit);

            if (GUI.Button(new Rect(95f, 1940f, 230f, 68f),
                    spatialG1Paused ? "继续" : "暂停",
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
                spatialG1Paused = !spatialG1Paused;
            if (GUI.Button(new Rect(425f, 1940f, 230f, 68f), "重新播放",
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                spatialG1Replay.Reset();
                spatialG1Paused = false;
            }
            if (GUI.Button(new Rect(755f, 1940f, 230f, 68f), "退出灰盒",
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                spatialG1GrayboxActive = false;
                spatialG1Paused = false;
            }
            if (GUI.Button(new Rect(375f, 2030f, 330f, 56f), "复制G1战报",
                    ButtonStyle(UiButtonSemantic.Reward, 14)) && spatialG1Result != null)
                GUIUtility.systemCopyBuffer = SpatialBattleSimulator.CreateRepresentativeLog(spatialG1Result, 80);
        }

        private void DrawSpatialG1TargetLines(Rect field)
        {
            foreach (var unit in spatialG1Replay.Units)
            {
                if (!unit.IsAlive || string.IsNullOrEmpty(unit.TargetId)) continue;
                var target = spatialG1Replay.Find(unit.TargetId);
                if (target == null || !target.IsAlive) continue;
                var start = SpatialG1CanvasPosition(field,
                    unit.InterpolatedLogicPosition(spatialG1Replay.InterpolationAlpha));
                var end = SpatialG1CanvasPosition(field,
                    target.InterpolatedLogicPosition(spatialG1Replay.InterpolationAlpha));
                DrawSpatialG1Line(start, end,
                    unit.IsPlayer ? new Color(0.25f, 0.85f, 0.92f, 0.24f) :
                        new Color(0.95f, 0.35f, 0.28f, 0.24f), 3f);
            }
        }

        private void DrawSpatialG1Unit(Rect field, SpatialGrayboxUnitView unit)
        {
            var point = SpatialG1CanvasPosition(field,
                unit.InterpolatedLogicPosition(spatialG1Replay.InterpolationAlpha));
            var definitionId = SpatialG1DefinitionId(unit.Id);
            var spriteArea = new Rect(point.x - 80f, point.y - 148f, 160f, 170f);
            DrawGroundShadow(new Rect(point.x - 64f, point.y - 11f, 128f, 22f),
                unit.IsAlive ? 0.78f : 0.30f);
            CharacterArtDefinition definition;
            Sprite sprite;
            var tint = unit.IsAlive ? Color.white : new Color(0.32f, 0.32f, 0.32f);
            if (spatialG1Replay.CurrentTick - unit.LastHitTick <= 3)
                tint = Color.Lerp(tint, new Color(1f, 0.34f, 0.24f), 0.55f);
            GUI.color = tint;
            if (artCatalog.TryGetCharacterArt(definitionId, out definition, out sprite))
                DrawStaticBattleSprite(spriteArea, sprite, !unit.FacingRight);
            else GUI.DrawTexture(spriteArea, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var healthWidth = 150f;
            var healthRatio = unit.MaximumHealth <= 0 ? 0f :
                Mathf.Clamp01(unit.Health / (float)unit.MaximumHealth);
            GUI.color = new Color(0.02f, 0.04f, 0.05f, 0.94f);
            GUI.DrawTexture(new Rect(point.x - healthWidth * 0.5f, point.y - 168f,
                healthWidth, 13f), Texture2D.whiteTexture);
            GUI.color = unit.IsPlayer ? new Color(0.20f, 0.84f, 0.42f) : new Color(0.90f, 0.28f, 0.23f);
            GUI.DrawTexture(new Rect(point.x - healthWidth * 0.5f, point.y - 168f,
                healthWidth * healthRatio, 13f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            var stateName = spatialG1Replay.CurrentTick - unit.LastHitTick <= 3 && unit.IsAlive
                ? "Hit (visual only)"
                : unit.State.ToString();
            GUI.Label(new Rect(point.x - 125f, point.y + 12f, 250f, 54f),
                unit.Id + " · " + unit.Role + "\n" + stateName,
                Style(13, unit.IsPlayer ? new Color(0.55f, 0.94f, 1f) :
                    new Color(1f, 0.66f, 0.58f), TextAnchor.UpperCenter));
        }

        private static Vector2 SpatialG1CanvasPosition(Rect field, Vector2 logicPosition)
        {
            return new Vector2(
                field.x + Mathf.Clamp01(logicPosition.x / 10000f) * field.width,
                field.y + Mathf.Clamp01(logicPosition.y / 16000f) * field.height);
        }

        private static string SpatialG1DefinitionId(string battleUnitId)
        {
            if (battleUnitId.EndsWith("M1", StringComparison.Ordinal)) return "unit.guardian";
            if (battleUnitId.EndsWith("M2", StringComparison.Ordinal)) return "unit.starblade";
            return "unit.starpriest";
        }

        private static void DrawSpatialG1Line(Vector2 start, Vector2 end, Color color, float width)
        {
            var delta = end - start;
            if (delta.sqrMagnitude <= 0.001f) return;
            var matrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, delta.magnitude, width),
                Texture2D.whiteTexture);
            GUI.matrix = matrix;
            GUI.color = Color.white;
        }

        private void DrawSpatialG2Graybox()
        {
            var field = new Rect(70f, 375f, 940f, 1455f);
            GUI.color = new Color(0.02f, 0.09f, 0.11f, 0.79f);
            GUI.DrawTexture(field, Texture2D.whiteTexture);
            GUI.color = new Color(0.24f, 0.78f, 0.80f, 0.20f);
            for (var row = 1; row < 8; row++)
                GUI.DrawTexture(new Rect(field.x, field.y + field.height * row / 8f,
                    field.width, 2f), Texture2D.whiteTexture);
            for (var column = 1; column < 5; column++)
                GUI.DrawTexture(new Rect(field.x + field.width * column / 5f, field.y,
                    2f, field.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.Label(new Rect(55f, 105f, 970f, 54f),
                "G2 · SUPPORT / LOW-HP RETARGET / SOFT SEPARATION / TIMEOUT",
                Style(21, new Color(0.95f, 0.82f, 0.43f), TextAnchor.MiddleCenter, FontStyle.Bold));
            GUI.Label(new Rect(55f, 155f, 970f, 42f),
                language == GameLanguage.ChineseSimplified
                    ? "独立无奖励灰盒 · 复用既有静态图 · 非完整动画 · 正式 v1 未切换"
                    : "NO REWARDS · EXISTING STATIC ART · NOT FULL ANIMATION · V1 UNCHANGED",
                Style(15, new Color(0.72f, 0.88f, 0.88f), TextAnchor.MiddleCenter));

            var vectorName = spatialG2Input == null ? string.Empty : spatialG2Input.VectorId;
            var hashes = spatialG2Result == null ? string.Empty :
                "event " + spatialG2Result.EventHash + "   final " + spatialG2Result.FinalStateHash;
            GUI.Label(new Rect(55f, 198f, 970f, 32f), vectorName + "   " + hashes,
                Style(12, new Color(0.76f, 0.84f, 0.84f), TextAnchor.MiddleCenter));
            if (spatialG2Result != null)
            {
                var stats = "tick " + spatialG2Replay.CurrentTick + "/" + spatialG2Result.CompletedTicks +
                            " · events " + spatialG2Replay.ConsumedEventCount + "/" +
                            spatialG2Result.Events.Count + " · actions " + spatialG2Result.ActiveActions +
                            " · heal " + spatialG2Result.HealingDone + " · sep " +
                            spatialG2Result.SeparationCount + " · stuck " +
                            spatialG2Result.StuckDetectionCount + " · side " + spatialG2Result.SidestepCount;
                GUI.Label(new Rect(55f, 230f, 970f, 32f), stats,
                    Style(13, Color.white, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(55f, 260f, 970f, 30f),
                    spatialG2Result.Outcome + " / " + spatialG2Result.EndReason +
                    " / rewards=false",
                    Style(13, new Color(0.96f, 0.68f, 0.38f), TextAnchor.MiddleCenter));
            }

            DrawSpatialG2VectorButtons();
            DrawSpatialG2TargetLines(field);
            foreach (var support in spatialG2Replay.Units)
                if (support.IsAlive && support.Role == SpatialG2Role.Support)
                    DrawSpatialG2RangeCircle(field, support);

            var renderUnits = new List<SpatialG2GrayboxUnitView>(spatialG2Replay.Units);
            renderUnits.Sort((left, right) =>
            {
                var leftPosition = left.InterpolatedLogicPosition(spatialG2Replay.InterpolationAlpha);
                var rightPosition = right.InterpolatedLogicPosition(spatialG2Replay.InterpolationAlpha);
                var order = leftPosition.y.CompareTo(rightPosition.y);
                return order != 0 ? order : string.CompareOrdinal(left.Id, right.Id);
            });
            foreach (var unit in renderUnits) DrawSpatialG2Unit(field, unit);

            if (GUI.Button(new Rect(75f, 1860f, 245f, 66f),
                    spatialG2Paused ? "继续" : "暂停",
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
                spatialG2Paused = !spatialG2Paused;
            if (GUI.Button(new Rect(417f, 1860f, 245f, 66f), "重新播放",
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                spatialG2Replay.Reset();
                spatialG2Paused = false;
            }
            if (GUI.Button(new Rect(760f, 1860f, 245f, 66f), "退出灰盒",
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                spatialG2GrayboxActive = false;
                spatialG2Paused = false;
            }
            if (GUI.Button(new Rect(365f, 1945f, 350f, 58f), "复制G2战报",
                    ButtonStyle(UiButtonSemantic.Reward, 14)) && spatialG2Result != null)
                GUIUtility.systemCopyBuffer = SpatialBattleG2Simulator.CreateRepresentativeLog(
                    spatialG2Result, 100);
        }

        private void DrawSpatialG2VectorButtons()
        {
            var labels = new[] { "G2-A 双支援", "G2-B 低血转火", "G2-C 拥堵超时" };
            for (var index = 0; index < labels.Length; index++)
            {
                var semantic = index == spatialG2VectorIndex
                    ? UiButtonSemantic.Primary
                    : UiButtonSemantic.Secondary;
                if (GUI.Button(new Rect(110f + index * 295f, 304f, 270f, 52f),
                        labels[index], ButtonStyle(semantic, 14)) && index != spatialG2VectorIndex)
                    BeginSpatialG2Graybox(index);
            }
        }

        private void DrawSpatialG2TargetLines(Rect field)
        {
            foreach (var unit in spatialG2Replay.Units)
            {
                if (!unit.IsAlive || string.IsNullOrEmpty(unit.TargetId)) continue;
                var target = spatialG2Replay.Find(unit.TargetId);
                if (target == null || !target.IsAlive) continue;
                var start = SpatialG1CanvasPosition(field,
                    unit.InterpolatedLogicPosition(spatialG2Replay.InterpolationAlpha));
                var end = SpatialG1CanvasPosition(field,
                    target.InterpolatedLogicPosition(spatialG2Replay.InterpolationAlpha));
                Color color;
                float width;
                if (unit.TargetMode == SpatialG2TargetMode.Heal)
                {
                    color = spatialG2Replay.CurrentTick - unit.LastHealTick <= 4
                        ? new Color(1f, 0.76f, 0.24f, 0.92f)
                        : new Color(1f, 0.65f, 0.20f, 0.38f);
                    width = 5f;
                }
                else if (unit.TargetMode == SpatialG2TargetMode.Follow)
                {
                    color = new Color(0.68f, 0.82f, 0.90f, 0.22f);
                    width = 2f;
                }
                else
                {
                    color = unit.IsPlayer
                        ? new Color(0.25f, 0.85f, 0.92f, 0.24f)
                        : new Color(0.95f, 0.35f, 0.28f, 0.24f);
                    width = 3f;
                }
                DrawSpatialG1Line(start, end, color, width);
            }
        }

        private void DrawSpatialG2RangeCircle(Rect field, SpatialG2GrayboxUnitView support)
        {
            var center = SpatialG1CanvasPosition(field,
                support.InterpolatedLogicPosition(spatialG2Replay.InterpolationAlpha));
            var radiusX = 3400f / 10000f * field.width;
            var radiusY = 3400f / 16000f * field.height;
            var previous = new Vector2(center.x + radiusX, center.y);
            const int segments = 36;
            for (var segment = 1; segment <= segments; segment++)
            {
                var angle = segment / (float)segments * Mathf.PI * 2f;
                var next = new Vector2(center.x + Mathf.Cos(angle) * radiusX,
                    center.y + Mathf.Sin(angle) * radiusY);
                DrawSpatialG1Line(previous, next, new Color(1f, 0.68f, 0.22f, 0.16f), 2f);
                previous = next;
            }
        }

        private void DrawSpatialG2Unit(Rect field, SpatialG2GrayboxUnitView unit)
        {
            var point = SpatialG1CanvasPosition(field,
                unit.InterpolatedLogicPosition(spatialG2Replay.InterpolationAlpha));
            var definitionId = SpatialG2DefinitionId(unit.Id);
            var spriteArea = new Rect(point.x - 66f, point.y - 124f, 132f, 142f);
            DrawGroundShadow(new Rect(point.x - 54f, point.y - 9f, 108f, 19f),
                unit.IsAlive ? 0.76f : 0.28f);

            CharacterArtDefinition definition;
            Sprite sprite;
            var tint = unit.IsAlive ? Color.white : new Color(0.32f, 0.32f, 0.32f);
            if (spatialG2Replay.CurrentTick - unit.LastHitTick <= 3)
                tint = Color.Lerp(tint, new Color(1f, 0.30f, 0.22f), 0.58f);
            else if (spatialG2Replay.CurrentTick - unit.LastHealTick <= 4)
                tint = Color.Lerp(tint, new Color(0.38f, 1f, 0.52f), 0.48f);
            else if (spatialG2Replay.CurrentTick - unit.LastSidestepTick <= 5)
                tint = Color.Lerp(tint, new Color(1f, 0.76f, 0.20f), 0.45f);
            else if (spatialG2Replay.CurrentTick - unit.LastSeparationTick <= 3)
                tint = Color.Lerp(tint, new Color(0.24f, 0.90f, 1f), 0.35f);
            GUI.color = tint;
            if (artCatalog.TryGetCharacterArt(definitionId, out definition, out sprite))
                DrawStaticBattleSprite(spriteArea, sprite, !unit.FacingRight);
            else GUI.DrawTexture(spriteArea, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUI.color = new Color(0.20f, 0.92f, 1f, 0.92f);
            GUI.DrawTexture(new Rect(point.x - 4f, point.y - 4f, 8f, 8f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            const float healthWidth = 126f;
            var healthRatio = unit.MaximumHealth <= 0 ? 0f :
                Mathf.Clamp01(unit.Health / (float)unit.MaximumHealth);
            GUI.color = new Color(0.02f, 0.04f, 0.05f, 0.94f);
            GUI.DrawTexture(new Rect(point.x - healthWidth * 0.5f, point.y - 143f,
                healthWidth, 11f), Texture2D.whiteTexture);
            GUI.color = unit.IsPlayer ? new Color(0.20f, 0.84f, 0.42f) : new Color(0.90f, 0.28f, 0.23f);
            GUI.DrawTexture(new Rect(point.x - healthWidth * 0.5f, point.y - 143f,
                healthWidth * healthRatio, 11f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var reason = unit.LastReasonCode ?? string.Empty;
            if (reason.Length > 25) reason = reason.Substring(0, 25);
            GUI.Label(new Rect(point.x - 132f, point.y + 10f, 264f, 72f),
                unit.Id + " · " + unit.Role + " · " + unit.TargetMode + "\n" +
                unit.State + " · " + reason,
                Style(11, unit.IsPlayer ? new Color(0.55f, 0.94f, 1f) :
                    new Color(1f, 0.66f, 0.58f), TextAnchor.UpperCenter));
        }

        private static string SpatialG2DefinitionId(string id)
        {
            return SpatialBattleG2PresentationMapping.DefinitionIdFor(id);
        }

        private void DrawBattleAvatar(BattleUnitPresentationState combatUnit, float opacity)
        {
            var view = battlePresentation.FindView(combatUnit.Id);
            if (view == null || !view.IsVisible) return;
            opacity *= view.Opacity;
            var baseRect = GetArenaBattleRect(combatUnit);
            var area = GetAnimatedBattleRect(combatUnit, baseRect);
            var cardColor = combatUnit.IsAlive ? combatUnit.BaseColor : combatUnit.BaseColor * 0.30f;
            if (battleVisual != null && battleVisual.TargetId == combatUnit.Id &&
                battleVisual.Progress >= 0.46f && battleVisual.Progress <= 0.82f)
            {
                var flash = battleVisual.Type == BattleEventType.Heal
                    ? new Color(0.30f, 1f, 0.58f)
                    : new Color(1f, 0.34f, 0.25f);
                cardColor = Color.Lerp(cardColor, flash, 0.62f);
            }
            CharacterArtDefinition artDefinition;
            Sprite characterSprite;
            if (artCatalog.TryGetCharacterArt(combatUnit.DefinitionId,
                    ResolveUnitLevelBadgeQualityId(combatUnit.DefinitionId, combatUnit.IsPlayer),
                    out artDefinition, out characterSprite))
            {
                characterSprite = ResolveFormalBattleAnimationSprite(combatUnit, characterSprite);
                DrawBattleCharacterArt(combatUnit, area, artDefinition, characterSprite, opacity);
                return;
            }

            DrawBattleTargetFrame(area, combatUnit);
            GUI.color = WithOpacity(new Color(0f, 0f, 0f, 0.45f), opacity);
            GUI.DrawTexture(new Rect(area.x + 10f, area.y + 12f, area.width, area.height), Texture2D.whiteTexture);
            GUI.color = WithOpacity(cardColor, opacity);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawBattleUnitHud(combatUnit, area.center.x, area.y, area.width, opacity,
                PieceBaseBottomY(area.yMax - 5f, area.width));
        }

        private Sprite ResolveFormalBattleAnimationSprite(
            BattleUnitPresentationState combatUnit, Sprite fallback)
        {
            if (combatUnit == null ||
                !FormalBattleAnimationContract.SupportsDefinition(combatUnit.DefinitionId)) return fallback;

            var clipId = FormalBattleAnimationIds.Idle;
            var elapsedMs = Mathf.FloorToInt(Time.unscaledTime * 1000f);
            var cue = battleVisual;
            if (cue != null)
            {
                if (cue.Type == BattleEventType.Defeat && cue.TargetId == combatUnit.Id)
                {
                    clipId = FormalBattleAnimationIds.Defeat;
                    elapsedMs = cue.ElapsedMilliseconds;
                }
                else if (cue.Type == BattleEventType.Attack && cue.ActorId == combatUnit.Id &&
                         cue.DrivesActorAction)
                {
                    clipId = string.Equals(cue.SourceEffectId,
                        FormalBattleAnimationContract.GateStandSourceEffectId,
                        StringComparison.Ordinal)
                        ? FormalBattleAnimationIds.VariantGateStand
                        : FormalBattleAnimationIds.BasicGateShieldStrike;
                    elapsedMs = cue.ElapsedMilliseconds;
                }
                else if (cue.Type == BattleEventType.Attack && cue.TargetId == combatUnit.Id &&
                         cue.Progress >= cue.ImpactProgress)
                {
                    var impactMs = Mathf.RoundToInt(cue.ImpactProgress * cue.DurationSeconds * 1000f);
                    var hitElapsedMs = cue.ElapsedMilliseconds - impactMs;
                    var hitClip = FormalBattleAnimationContract.GetClip(FormalBattleAnimationIds.Hit);
                    if (hitElapsedMs <= hitClip.DurationMs)
                    {
                        clipId = FormalBattleAnimationIds.Hit;
                        elapsedMs = Mathf.Max(0, hitElapsedMs);
                    }
                }
            }

            Sprite animationSprite;
            return artCatalog.TryGetFormalBattleAnimationFrame(
                combatUnit.DefinitionId, clipId, elapsedMs, out animationSprite)
                ? animationSprite
                : fallback;
        }

        private void DrawBattleCharacterArt(
            BattleUnitPresentationState combatUnit,
            Rect animatedSlotRect,
            CharacterArtDefinition artDefinition,
            Sprite characterSprite,
            float opacity)
        {
            // Formal combat uses one approved fixed front view for both sides. Enemy
            // placement never mirrors the canonical master or changes handedness.
            const bool flipX = StaticBattleArtContract.FormalBattleFlipX;
            var visualScale = FormationBattleVisualScale(combatUnit.DefinitionId);
            var textureCanvasRect = BattlePresentationLayout.GetStaticBattleCanvasRect(
                animatedSlotRect, characterSprite, visualScale);
            textureCanvasRect.y += BattlePresentationLayout.StaticBattleCanvasHeight * visualScale *
                                   artDefinition.VisualYOffsetRelativeToUnitHeight;
            textureCanvasRect.y += textureCanvasRect.height * BattleUnitBaseAlignmentDropPercent;
            var visibleRect = artDefinition.GetVisibleContentRect(textureCanvasRect, flipX);
            DrawBattleTargetFrame(new Rect(visibleRect.x - 7f, visibleRect.y - 7f,
                visibleRect.width + 14f, visibleRect.height + 14f), combatUnit);

            var footY = animatedSlotRect.yMax - 5f;
            DrawGroundShadow(FormationGroundShadowRect(
                    combatUnit.DefinitionId, animatedSlotRect.center.x, footY, 172f, 28f),
                opacity * (combatUnit.IsAlive ? 1f : 0.42f));

            var spriteTint = combatUnit.IsAlive ? Color.white : new Color(0.35f, 0.35f, 0.35f);
            if (battleVisual != null && battleVisual.TargetId == combatUnit.Id &&
                battleVisual.Progress >= 0.46f && battleVisual.Progress <= 0.82f)
            {
                var flash = battleVisual.Type == BattleEventType.Heal
                    ? new Color(0.62f, 1f, 0.72f)
                    : new Color(1f, 0.58f, 0.50f);
                spriteTint = Color.Lerp(spriteTint, flash, 0.28f);
            }
            var oldMatrix = GUI.matrix;
            var view = battlePresentation.FindView(combatUnit.Id);
            if (view != null)
            {
                GUIUtility.RotateAroundPivot(view.RotationDegrees,
                    new Vector2(animatedSlotRect.center.x, animatedSlotRect.yMax - 5f));
            }
            DrawPieceBase(new Vector2(animatedSlotRect.center.x, footY), opacity,
                visibleRect.width);
            var characterMatrix = GUI.matrix;
            GUI.matrix = oldMatrix;
            DrawBattleFeedbackForUnit(combatUnit.Id);
            GUI.matrix = characterMatrix;
            GUI.color = WithOpacity(spriteTint, opacity);
            DrawStaticBattleSprite(textureCanvasRect, characterSprite, flipX);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
            DrawBattleShieldOverlay(combatUnit, visibleRect, opacity);
            DrawBattleFeedbackRiseForUnit(combatUnit.Id);

            DrawBattleUnitHud(combatUnit, animatedSlotRect.center.x, visibleRect.y,
                visibleRect.width, opacity,
                PieceBaseBottomY(footY, visibleRect.width));
        }

        private static void DrawStaticBattleSprite(Rect canvasRect, Sprite sprite, bool flipX)
        {
            if (sprite == null || sprite.texture == null) return;
            var texture = sprite.texture;
            var textureRect = sprite.textureRect;
            var uv = new Rect(
                textureRect.x / texture.width,
                textureRect.y / texture.height,
                textureRect.width / texture.width,
                textureRect.height / texture.height);
            if (flipX)
            {
                uv.x += uv.width;
                uv.width = -uv.width;
            }
            GUI.DrawTextureWithTexCoords(canvasRect, texture, uv, true);
        }

        private void DrawPieceBase(
            Vector2 footPosition,
            float opacity,
            float visibleCharacterWidth,
            string baseSkinId = PieceBasePresentationContract.DefaultSkinId)
        {
            PieceBaseSkinDefinition baseDefinition;
            Sprite baseSprite;
            if (!artCatalog.TryGetPieceBase(baseSkinId, out baseDefinition, out baseSprite)) return;

            var width = PieceBasePresentationContract.DisplayWidthForVisibleCharacterWidth(
                visibleCharacterWidth);
            var height = width * baseSprite.rect.height / Mathf.Max(1f, baseSprite.rect.width);
            var area = new Rect(
                footPosition.x - width * PieceBasePresentationContract.FootAnchorTopNormalizedX,
                footPosition.y - height * PieceBasePresentationContract.FootAnchorTopNormalizedY,
                width,
                height);
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawStaticBattleSprite(area, baseSprite, false);
            GUI.color = oldColor;
        }

        private static float PieceBaseBottomY(
            float footY, float visibleCharacterWidth)
        {
            return footY + PieceBasePresentationContract.DisplayHeightForVisibleCharacterWidth(
                visibleCharacterWidth) * (1f - PieceBasePresentationContract.FootAnchorTopNormalizedY);
        }

        private void DrawBattleUnitHud(
            BattleUnitPresentationState combatUnit, float centerX, float visibleTopY,
            float meaningfulVisibleWidth, float opacity, float pieceBaseBottomY)
        {
            var healthRect = FormationStatusBarRect(combatUnit.DefinitionId, centerX,
                visibleTopY, meaningfulVisibleWidth);
            DrawFormationStatusBar(healthRect, combatUnit.HealthPercent,
                FormationStatusCompartmentCount(combatUnit.DefinitionId, combatUnit.MaxHealth),
                PrototypeArtResourceIds.FormationBattleV951HealthFill,
                opacity * (combatUnit.IsAlive ? 1f : 0.44f));

            if (combatUnit.MaxShield > 0)
            {
                var shieldRect = new Rect(healthRect.x, healthRect.y - healthRect.height - 4f,
                    healthRect.width, healthRect.height);
                float shieldVisualOpacity;
                if (!battleShieldOverlayOpacities.TryGetValue(combatUnit.Id,
                        out shieldVisualOpacity)) shieldVisualOpacity = combatUnit.CurrentShield > 0 ? 1f : 0f;
                DrawFormationStatusBar(shieldRect, combatUnit.ShieldPercent,
                    FormationStatusCompartmentCount(combatUnit.DefinitionId, combatUnit.MaxShield),
                    PrototypeArtResourceIds.FormationBattleV951ShieldFill,
                    opacity * shieldVisualOpacity);
            }
            var caption = T(combatUnit.DefinitionId);
            const int nameFontSize = 23;
            var nameArea = FormationNameplateRect(caption, nameFontSize, centerX,
                pieceBaseBottomY);
            DrawFormationUnitNameplate(nameArea, caption, opacity, nameFontSize);
            GUI.color = Color.white;
        }

        private void DrawSegmentedStatusBar(
            Rect area, float fill, int segmentCount, Color filledColor, string label, int fontSize, float opacity)
        {
            segmentCount = Mathf.Max(1, segmentCount);
            fill = Mathf.Clamp01(fill);
            var gap = 2f;
            var segmentWidth = Mathf.Max(1f, (area.width - gap * (segmentCount - 1)) / segmentCount);
            var filledSegments = fill * segmentCount;
            for (var index = 0; index < segmentCount; index++)
            {
                var segment = new Rect(area.x + index * (segmentWidth + gap), area.y, segmentWidth, area.height);
                GUI.color = WithOpacity(new Color(0.025f, 0.045f, 0.055f, 0.82f), opacity);
                GUI.DrawTexture(segment, Texture2D.whiteTexture);

                var segmentFill = Mathf.Clamp01(filledSegments - index);
                if (segmentFill <= 0f) continue;
                GUI.color = WithOpacity(filledColor, opacity);
                GUI.DrawTexture(new Rect(segment.x, segment.y, segment.width * segmentFill, segment.height),
                    Texture2D.whiteTexture);
            }

            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 1f, area.y + 1f, area.width, area.height), label,
                Style(fontSize, WithOpacity(new Color(0f, 0f, 0f, 0.82f), opacity),
                    TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(area, label,
                Style(fontSize, WithOpacity(Color.white, opacity), TextAnchor.MiddleCenter, FontStyle.Normal));
        }

        private static int HealthSegmentCount(int maximumHealth, bool isBoss)
        {
            if (isBoss) return 16;
            if (maximumHealth <= 12) return 5;
            if (maximumHealth <= 20) return 8;
            if (maximumHealth <= 35) return 10;
            return 12;
        }

        private static void DrawGroundShadow(Rect area, float opacity)
        {
            GUI.color = new Color(0.015f, 0.025f, 0.025f, Mathf.Clamp01(opacity) * 0.42f);
            GUI.DrawTexture(area, GroundShadowTexture(), ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        private static Texture2D GroundShadowTexture()
        {
            if (groundShadowTexture != null) return groundShadowTexture;
            const int width = 64;
            const int height = 32;
            var pixels = new Color[width * height];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var normalizedX = ((x + 0.5f) / width - 0.5f) * 2f;
                    var normalizedY = ((y + 0.5f) / height - 0.5f) * 2f;
                    var falloff = Mathf.Clamp01(1f - normalizedX * normalizedX - normalizedY * normalizedY);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, falloff * falloff);
                }
            }

            groundShadowTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            groundShadowTexture.name = "PrototypeGroundShadow";
            groundShadowTexture.wrapMode = TextureWrapMode.Clamp;
            groundShadowTexture.filterMode = FilterMode.Bilinear;
            groundShadowTexture.hideFlags = HideFlags.HideAndDontSave;
            groundShadowTexture.SetPixels(pixels);
            groundShadowTexture.Apply(false, true);
            return groundShadowTexture;
        }

        private static void DrawBattleTopLegibilityScrim(float opacity)
        {
            const int bandCount = 10;
            const float bandHeight = 32f;
            for (var index = 0; index < bandCount; index++)
            {
                var remaining = 1f - index / (float)bandCount;
                GUI.color = new Color(0.015f, 0.035f, 0.045f,
                    Mathf.Clamp01(opacity) * 0.22f * remaining * remaining);
                GUI.DrawTexture(new Rect(0f, index * bandHeight, DesignWidth, bandHeight),
                    Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        private int CompareBattleRenderOrder(BattleUnitPresentationState left, BattleUnitPresentationState right)
        {
            var leftRect = GetAnimatedBattleRect(left, GetArenaBattleRect(left));
            var rightRect = GetAnimatedBattleRect(right, GetArenaBattleRect(right));
            var order = leftRect.yMax.CompareTo(rightRect.yMax);
            return order != 0 ? order : string.CompareOrdinal(left.Id, right.Id);
        }

        private static bool IsPlayerCombatUnit(BattleUnitPresentationState unit)
        {
            return unit.IsPlayer;
        }

        private static Color WithOpacity(Color color, float opacity)
        {
            color.a *= Mathf.Clamp01(opacity);
            return color;
        }

        private void DrawHeader()
        {
            if (currentView == ScreenView.Expedition && (IsPreparationScreen || battlePresentationActive))
            {
                DrawFormationBattleV94Header();
                return;
            }
            var titleColor = IsExpeditionUiTrialPage ? PreparationTextPrimary : Color.white;
            var subtitleColor = IsExpeditionUiTrialPage ? PreparationTextMuted : new Color(0.36f, 0.82f, 0.77f);
            GUI.Label(new Rect(55f, 34f, 440f, 64f), T("title"), Style(42, titleColor, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(58f, 96f, 430f, 38f), T("subtitle"), Style(18, subtitleColor, TextAnchor.MiddleLeft));
            if (currentView == ScreenView.Expedition || currentView == ScreenView.Warehouse ||
                currentView == ScreenView.Defense || (currentView == ScreenView.Deck && characterDetailOpen))
            {
                var expeditionHeader = currentView == ScreenView.Expedition;
                var backKey = currentView == ScreenView.Defense ? "arena.back" :
                    currentView == ScreenView.Deck ? "character.backToDeck" :
                    currentView == ScreenView.Warehouse && warehouseEquipmentDetailOpen
                        ? "equipment.backToWarehouse" : "home.back";
                var backArea = expeditionHeader
                    ? HeaderButtonArea(2)
                    : new Rect(ContentRight - HeaderButtonWidth - HeaderButtonGap - 180f, 45f, 180f, 54f);
                var isBattling = expeditionHeader && expeditionFlow != null && expeditionFlow.IsBattling;
                if (isBattling)
                {
                    GUI.enabled = battleControlsEnabled;
                    var pauseKey = battlePaused ? "battle.resume" : "battle.pause";
                    if (GUI.Button(backArea, T(pauseKey), ButtonStyle(UiButtonSemantic.Secondary, 16)))
                    {
                        battlePaused = !battlePaused;
                    }
                    GUI.enabled = true;
                }
                else if (GUI.Button(backArea, T(backKey), ButtonStyle(UiButtonSemantic.Secondary, 16)))
                {
                    if (currentView == ScreenView.Deck)
                    {
                        characterDetailOpen = false;
                        detailEquipmentPreviewId = null;
                    }
                    else if (currentView == ScreenView.Warehouse && warehouseEquipmentDetailOpen)
                    {
                        warehouseEquipmentDetailOpen = false;
                        warehouseEquipmentDetailId = null;
                    }
                    else currentView = currentView == ScreenView.Defense ? ScreenView.Arena : ScreenView.Home;
                    confirmAbandon = false;
                }
                if (expeditionHeader)
                {
                    var abandonKey = confirmAbandon ? "run.abandonConfirmShort" : "run.abandonShort";
                    if (isBattling) GUI.enabled = battleControlsEnabled;
                    if (GUI.Button(HeaderButtonArea(1), T(abandonKey),
                            ButtonStyle(UiButtonSemantic.Danger, 14)))
                    {
                        if (confirmAbandon) AbandonRun();
                        else
                        {
                            confirmAbandon = true;
                            statusKey = "status.abandonPrompt";
                        }
                    }
                    GUI.enabled = true;
                    if (confirmAbandon)
                    {
                        GUI.Label(new Rect(535f, 101f, 325f, 30f), T("status.abandonPrompt"),
                            Style(13, new Color(1f, 0.60f, 0.56f), TextAnchor.MiddleRight));
                    }
                }
            }
            if (GUI.Button(HeaderButtonArea(0), T("language"), ButtonStyle(UiButtonSemantic.Secondary, 20)))
            {
                language = language == GameLanguage.ChineseSimplified
                    ? GameLanguage.English
                    : GameLanguage.ChineseSimplified;
                SaveLanguagePreference();
            }

            if (currentView == ScreenView.Expedition)
            {
                DrawStat(ThreeColumnArea(0, 142f, 60f), string.Format(T("round"), round));
                DrawStat(ThreeColumnArea(1, 142f, 60f), T("coins") + "  " + coins +
                    (currentExpeditionRewardsEnabled ? string.Empty : "  ·  " + T("run.noRewardsShort")));
                DrawStat(ThreeColumnArea(2, 142f, 60f), T("health") + "  " + health);
            }
        }

        private void DrawFormationBattleV94Header()
        {
            var opacity = battlePresentationActive ? Mathf.Clamp01(battleSceneOpacity) : 1f;
            var navigationArea = new Rect(42f, 52f, 116f, 116f);
            DrawFormationBattleV94Texture(navigationArea,
                PrototypeArtResourceIds.FormationBattleV94BackFrame, ScaleMode.ScaleToFit, opacity);
            var glyphArea = new Rect(navigationArea.center.x - 32f, navigationArea.center.y - 32f, 64f, 64f);
            if (battlePresentationActive)
            {
                DrawFormationBattleV94Texture(glyphArea,
                    PrototypeArtResourceIds.FormationBattleV94Pause, ScaleMode.ScaleToFit, opacity);
                GUI.enabled = battleControlsEnabled;
                if (GUI.Button(navigationArea, GUIContent.none, GUIStyle.none)) battlePaused = !battlePaused;
                GUI.enabled = true;
            }
            else
            {
                if (formationOverlayTransitionActive)
                {
                    var glyphProgress = Mathf.Clamp01(formationOverlayTransitionProgress * 2f);
                    DrawFormationBattleV94Texture(glyphArea,
                        PrototypeArtResourceIds.FormationBattleV94BackGlyph, ScaleMode.ScaleToFit,
                        opacity * (1f - glyphProgress));
                    DrawFormationBattleV94Texture(glyphArea,
                        PrototypeArtResourceIds.FormationBattleV94Pause, ScaleMode.ScaleToFit,
                        opacity * glyphProgress);
                }
                else
                {
                    DrawFormationBattleV94Texture(glyphArea,
                        PrototypeArtResourceIds.FormationBattleV94BackGlyph, ScaleMode.ScaleToFit, opacity);
                }
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(navigationArea, GUIContent.none, GUIStyle.none))
                {
                    currentView = ScreenView.Home;
                    confirmAbandon = false;
                    selectedFormationSynergyId = null;
                    formationSynergyOverflowOpen = false;
                    selectedFormationBuffId = null;
                    formationBuffOverflowOpen = false;
                }
                GUI.enabled = true;
            }

            // This is the same accepted reusable abandon component used by the map,
            // recruitment and enhancement pages. The previous 198x82 placement was
            // roughly twenty percent smaller than its 1080-wide canonical scale.
            var abandonArea = new Rect(798f, 64f, 250f, 103f);
            DrawSharedExpeditionTexture(abandonArea, ExpeditionUiSharedFrames.AbandonFrame,
                ScaleMode.ScaleToFit);
            DrawV67CenteredIconText(abandonArea,
                "ART-UI-PREPARATION-ICON-ABANDONFLAG-V6.7.0",
                confirmAbandon ? T("run.abandonConfirmShort") : T("run.abandonShort"),
                26, ArchiveColor("F6DAD0"), 32f, 8f, true, 16);
            GUI.enabled = !formationOverlayTransitionActive && (!battlePresentationActive || battleControlsEnabled);
            if (GUI.Button(abandonArea, GUIContent.none, GUIStyle.none))
            {
                if (confirmAbandon) AbandonRun();
                else
                {
                    confirmAbandon = true;
                    statusKey = "status.abandonPrompt";
                }
            }
            GUI.enabled = true;

            DrawFormationBattleStatusCell(0, "ART-UI-PREPARATION-ICON-ROUND-V6.7.0", round.ToString(), opacity);
            DrawFormationBattleStatusCell(1, "ART-UI-PREPARATION-ICON-COIN-V6.7.0", coins.ToString(), opacity);
            DrawFormationBattleStatusCell(2, "ART-UI-PREPARATION-ICON-LIFE-V6.7.0", health.ToString(), opacity);
            DrawFormationBattleSynergyGroup(opacity);
        }

        private void DrawFormationBattleStatusCell(int index, string iconResourceId, string value, float opacity)
        {
            var center = FormationBattleV94Layout.StatusCenters[index];
            var area = new Rect(center.X - 150f, center.Y - 43f, 300f, 86f);
            var frameId = index == 0 ? ExpeditionUiSharedFrames.RoundHud :
                index == 1 ? ExpeditionUiSharedFrames.CoinsHud : ExpeditionUiSharedFrames.LifeHud;
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawSharedExpeditionTexture(area, frameId);
            GUI.color = oldColor;
            DrawV67CenteredIconText(area, iconResourceId, value, 32,
                WithOpacity(ArchiveColor("F4F0DD"), opacity), 42f, 14f, true, 20);
        }

        private void DrawFormationBattleSynergyGroup(float opacity)
        {
            var activeIds = ActiveSynergyIds(CurrentFormationSynergy());
            if (activeIds.Count == 0)
            {
                selectedFormationSynergyId = null;
                formationSynergyOverflowOpen = false;
                return;
            }

            var visibleCount = Mathf.Min(FormationBattleV94Layout.MaximumVisibleSynergies, activeIds.Count);
            var hasOverflow = activeIds.Count > FormationBattleV94Layout.MaximumVisibleSynergies;
            var centers = FormationBattleV94Layout.CenteredStatusGroupCenters(
                visibleCount + (hasOverflow ? 1 : 0), FormationBattleV94Layout.SynergyCenterY);
            for (var index = 0; index < visibleCount; index++)
            {
                var tile = CenteredSquare(centers[index].X, centers[index].Y,
                    FormationBattleV94Layout.SynergyTileSize);
                DrawFormationBattleV94Texture(tile, PrototypeArtResourceIds.FormationBattleV94SynergyTile,
                    ScaleMode.StretchToFill, opacity);
                DrawFormationSynergyGlyph(activeIds[index], Inset(tile, 15f), opacity);
                DrawFormationBattleCountBadge(tile, FormationSynergyCount(activeIds[index]), opacity);
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(tile, GUIContent.none, GUIStyle.none))
                {
                    formationSynergyOverflowOpen = false;
                    selectedFormationSynergyId = selectedFormationSynergyId == activeIds[index]
                        ? null : activeIds[index];
                }
                GUI.enabled = true;
            }

            if (hasOverflow)
            {
                var moreCenter = centers[visibleCount];
                var moreTile = CenteredSquare(moreCenter.X, moreCenter.Y,
                    FormationBattleV94Layout.SynergyTileSize);
                DrawFormationBattleV94Texture(moreTile, PrototypeArtResourceIds.FormationBattleV94SynergyTile,
                    ScaleMode.StretchToFill, opacity);
                GUI.Label(moreTile, "+", ArchiveStyle(48,
                    WithOpacity(new Color(0.91f, 0.73f, 0.27f), opacity), TextAnchor.MiddleCenter, true));
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(moreTile, GUIContent.none, GUIStyle.none))
                {
                    selectedFormationSynergyId = null;
                    formationSynergyOverflowOpen = !formationSynergyOverflowOpen;
                }
                GUI.enabled = true;
            }

        }

        private void DrawFormationBattleSynergyPopupLayer(float opacity)
        {
            if (!string.IsNullOrEmpty(selectedFormationSynergyId))
                DrawFormationBattleSynergyPopup(selectedFormationSynergyId, opacity);
            else if (formationSynergyOverflowOpen)
                DrawFormationBattleSynergyOverflowPopup(ActiveSynergyIds(CurrentFormationSynergy()), opacity);
        }

        private void DrawFormationBattleSynergyOverflowPopup(IReadOnlyList<string> activeIds, float opacity)
        {
            var area = ToRect(FormationBattleV94Layout.SynergyPopup);
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawV67NineSlice(area, "ART-UI-PREPARATION-OFFER-CARD-9SLICE-V6.7.0",
                new Vector4(30f, 30f, 30f, 30f));
            GUI.color = oldColor;
            GUI.Label(new Rect(area.x + 34f, area.y + 24f, area.width - 68f, 48f), T("ui.synergy.short"),
                ArchiveStyle(27, WithOpacity(ArchiveInk(), opacity), TextAnchor.MiddleLeft, true));
            var rowY = area.y + 82f;
            for (var index = FormationBattleV94Layout.MaximumVisibleSynergies;
                 index < activeIds.Count && rowY + 44f <= area.yMax - 18f; index++)
            {
                var row = new Rect(area.x + 34f, rowY, area.width - 68f, 44f);
                DrawFormationSynergyGlyph(activeIds[index], new Rect(row.x, row.y + 2f, 40f, 40f), opacity);
                GUI.Label(new Rect(row.x + 54f, row.y, row.width - 54f, row.height),
                    T(activeIds[index]) + "  ×" + FormationSynergyCount(activeIds[index]),
                    ArchiveStyle(20, WithOpacity(ArchiveInk(), opacity), TextAnchor.MiddleLeft));
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    formationSynergyOverflowOpen = false;
                    selectedFormationSynergyId = activeIds[index];
                }
                GUI.enabled = true;
                rowY += 46f;
            }
            var currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.MouseUp && currentEvent.button == 0 &&
                !area.Contains(currentEvent.mousePosition)) formationSynergyOverflowOpen = false;
        }

        private void DrawFormationBattleSynergyPopup(string synergyId, float opacity)
        {
            var area = ToRect(FormationBattleV94Layout.SynergyPopup);
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawV67NineSlice(area, "ART-UI-PREPARATION-OFFER-CARD-9SLICE-V6.7.0",
                new Vector4(30f, 30f, 30f, 30f));
            GUI.color = oldColor;
            DrawFormationSynergyGlyph(synergyId, new Rect(area.x + 30f, area.y + 40f, 92f, 92f), opacity);
            GUI.Label(new Rect(area.x + 145f, area.y + 34f, area.width - 180f, 62f), T(synergyId),
                ArchiveStyle(28, WithOpacity(ArchiveInk(), opacity), TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(area.x + 40f, area.y + 126f, area.width - 80f, area.height - 154f),
                T(synergyId + ".description"),
                ArchiveStyle(20, WithOpacity(ArchiveInk(), opacity), TextAnchor.UpperLeft));

            var currentEvent = Event.current;
            if (currentEvent != null && currentEvent.type == EventType.MouseUp && currentEvent.button == 0 &&
                !area.Contains(currentEvent.mousePosition)) selectedFormationSynergyId = null;
        }

        private void DrawFormationSynergyGlyph(string synergyId, Rect area, float opacity)
        {
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            if (synergyId.StartsWith("faction.", StringComparison.Ordinal))
                DrawV67FactionIcon(area, synergyId);
            else
                DrawV67ProfessionIcon(area, synergyId);
            GUI.color = oldColor;
        }

        private int FormationSynergyCount(string synergyId)
        {
            var count = 0;
            for (var index = 0; index < runRoster.BoardCapacity; index++)
            {
                var unit = runRoster.Get(index);
                if (unit == null) continue;
                var definition = characterCatalog.Get(unit.DefinitionId);
                if (definition.FactionId == synergyId || definition.ProfessionId == synergyId) count++;
            }
            return count;
        }

        private static Rect HeaderButtonArea(int slotFromRight)
        {
            var x = ContentRight - HeaderButtonWidth - slotFromRight * (HeaderButtonWidth + HeaderButtonGap);
            return new Rect(x, 45f, HeaderButtonWidth, 54f);
        }

        private static Rect ThreeColumnArea(int index, float y, float height)
        {
            const float gap = 35f;
            const float width = (ContentWidth - gap * 2f) / 3f;
            return new Rect(ContentLeft + index * (width + gap), y, width, height);
        }

        private void DrawHomeScreen()
        {
            // v3.8 only restores the fields removed from the v3.7 locked composite.
            // No frames, icons, dividers or decoration may be generated here.
            DrawHomeV38Text(HomeUiV38Layout.PlayerName, T("home.playerName"), 31,
                HomeV38Light, TextAnchor.MiddleLeft);
            DrawHomeV38Text(HomeUiV38Layout.PlayerPower,
                string.Format(T("home.pvpPower"), CurrentPvpDeckPower()), 20,
                HomeV38Light, TextAnchor.MiddleLeft);
            DrawHomeV38Text(HomeUiV38Layout.EnergyValue, expeditionEnergy.ToString(), 28,
                HomeV38Light, TextAnchor.MiddleCenter);
            DrawHomeV38Text(HomeUiV38Layout.SkrValue, skrBalance.ToString(), 28,
                HomeV38Light, TextAnchor.MiddleCenter);
            DrawHomeV38Text(HomeUiV38Layout.TideValue, tideCrystalBalance.ToString(), 28,
                HomeV38Light, TextAnchor.MiddleCenter);
            DrawHomeV38Text(HomeUiV38Layout.Season, T("home.season"), 48,
                HomeV38Ink, TextAnchor.MiddleCenter);
            DrawHomeV38Text(HomeUiV38Layout.Chapter, T("home.chapter"), 44,
                HomeV38Ink, TextAnchor.MiddleCenter);
            DrawHomeV38Text(HomeUiV38Layout.IslandName, T("home.island"), 28,
                HomeV38Ink, TextAnchor.MiddleCenter);

            var canRepair = !chapterOneLighthouse.IsRepaired &&
                            chapterOneLighthouse.CanRepair(stoneBalance, lampOilBalance, mistLensBalance);
            var lighthouseTitleKey = chapterOneLighthouse.IsRepaired
                ? "lighthouse.lit"
                : canRepair ? "home.lighthouse.repairable" : "lighthouse.unlit";
            DrawHomeV38Text(HomeUiV38Layout.StatusTitle, T(lighthouseTitleKey), 32,
                HomeV38Ink, TextAnchor.MiddleCenter);
            DrawHomeV38Text(HomeUiV38Layout.StoneCount,
                stoneBalance + "/" + LighthouseRepairState.RequiredStone, 22,
                HomeV38Ink, TextAnchor.MiddleLeft);
            DrawHomeV38Text(HomeUiV38Layout.OilCount,
                lampOilBalance + "/" + LighthouseRepairState.RequiredLampOil, 22,
                HomeV38Ink, TextAnchor.MiddleLeft);
            DrawHomeV38Text(HomeUiV38Layout.LensCount,
                mistLensBalance + "/" + LighthouseRepairState.RequiredMistLens, 22,
                HomeV38Ink, TextAnchor.MiddleLeft);

            var mustStartNewRun = expeditionFlow.IsComplete || runAbandoned || runWon;
            var hasProgress = !mustStartNewRun && HasExpeditionProgress();
            var startsNewRun = mustStartNewRun || !hasProgress;
            var startKey = startsNewRun && expeditionEnergy == 0
                ? "home.freeExpedition"
                : mustStartNewRun
                    ? "home.restartExpedition"
                    : hasProgress ? "home.continueExpedition" : "home.startExpedition";
            var actionRect = HomeV38Rect(HomeUiV38Layout.ExpeditionButton);
            var buttonState = HomeUiV38Layout.ResolveExpeditionButtonState(
                true, hasProgress, startsNewRun, expeditionEnergy);
            var actionTextureId = HomeV38ExpeditionTextureId(buttonState);
            DrawHomeV38Texture(actionRect, actionTextureId);
            DrawHomeV38Text(HomeUiV38Layout.ExpeditionLabel, T(startKey) + "  ›", 47,
                HomeV38Light, TextAnchor.MiddleCenter);
            var startPressed = GUI.Button(actionRect, GUIContent.none, GUIStyle.none);
            if (startPressed)
            {
                if (startsNewRun) RestartRun();
                homeTopMenu = 0;
                currentView = ScreenView.Expedition;
            }

            if (GUI.Button(HomeV37Rect(HomeUiV37Layout.Affairs), GUIContent.none, GUIStyle.none))
                homeTopMenu = homeTopMenu == 1 ? 0 : 1;
            if (GUI.Button(HomeV37Rect(HomeUiV37Layout.Warehouse), GUIContent.none, GUIStyle.none))
                OpenWarehouse();
            if (GUI.Button(HomeV37Rect(HomeUiV37Layout.Season), GUIContent.none, GUIStyle.none))
            {
                homeTopMenu = 0;
                currentView = ScreenView.Leaderboard;
            }
            HandleHomeChapterPointer(HomeV37Rect(HomeUiV37Layout.Island));
            if (GUI.Button(HomeV37Rect(HomeUiV37Layout.StatusCard), GUIContent.none, GUIStyle.none))
                ActivateHomeLighthouseCard(canRepair);

            if (homeTopMenu != 0) DrawHomeTopMenu();
        }

        private static readonly Color HomeV38Ink = new Color(0.090f, 0.208f, 0.243f);
        private static readonly Color HomeV38Light = new Color(0.941f, 0.906f, 0.824f);

        private Rect HomeV38Rect(ReferenceUiRect reference)
        {
            return new Rect(reference.X, reference.Y, reference.Width, reference.Height);
        }

        private Rect HomeV37Rect(NormalizedUiRect reference)
        {
            return new Rect(reference.X * HomeUiV38Layout.ReferenceWidth,
                reference.Y * HomeUiV38Layout.ReferenceHeight,
                reference.Width * HomeUiV38Layout.ReferenceWidth,
                reference.Height * HomeUiV38Layout.ReferenceHeight);
        }

        private void DrawHomeV38Text(ReferenceUiRect reference, string text, int fontSize,
            Color color, TextAnchor alignment)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = HomeV38SerifFont(text, fontSize >= 31),
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = alignment,
                clipping = TextClipping.Clip,
                wordWrap = false,
                richText = false
            };
            style.normal.textColor = color;
            var area = HomeV38Rect(reference);
            var content = new GUIContent(text);
            while (style.fontSize > 16 && style.CalcSize(content).x > area.width)
                style.fontSize--;
            GUI.Label(area, content, style);
        }

        private Font HomeV38SerifFont(string text, bool emphasized)
        {
            var embedded = SourceHanSerifFont(emphasized);
            if (FontSupportsText(embedded, text)) return embedded;

            // User-entered player names can contain characters outside the curated
            // localization subset. Only that unpredictable content may use a device
            // fallback; shipped UI copy always resolves to the embedded serif fonts.
            if (unpredictableTextFallbackFont != null) return unpredictableTextFallbackFont;
            try
            {
                unpredictableTextFallbackFont = Font.CreateDynamicFontFromOSFont(new[]
                {
                    "Noto Serif CJK SC", "Noto Serif SC", "STSong", "SimSun",
                    "Droid Sans Fallback", "sans-serif"
                }, 48);
            }
            catch (Exception)
            {
                unpredictableTextFallbackFont = null;
            }
            return unpredictableTextFallbackFont != null ? unpredictableTextFallbackFont : embedded;
        }

        private Font SourceHanSerifFont(bool emphasized)
        {
            if (sourceHanSerifRegularFont == null)
                sourceHanSerifRegularFont = Resources.Load<Font>(
                    "Fonts/SourceHanSerifSC/SourceHanSerifSC-Regular");
            if (sourceHanSerifSemiBoldFont == null)
                sourceHanSerifSemiBoldFont = Resources.Load<Font>(
                    "Fonts/SourceHanSerifSC/SourceHanSerifSC-SemiBold");

            var selected = emphasized ? sourceHanSerifSemiBoldFont : sourceHanSerifRegularFont;
            if (selected == null)
                selected = emphasized ? sourceHanSerifRegularFont : sourceHanSerifSemiBoldFont;
            if (selected == null && !sourceHanFontWarningLogged)
            {
                sourceHanFontWarningLogged = true;
                Debug.LogError("Embedded Source Han Serif SC fonts are missing from Resources/Fonts.");
            }
            return selected != null ? selected : GUI.skin.label.font;
        }

        private void ApplyUnifiedGameFont(GUISkin skin)
        {
            if (skin == null) return;
            var font = SourceHanSerifFont(false);
            if (font == null) return;

            skin.font = font;
            skin.label.font = font;
            skin.button.font = font;
            skin.box.font = font;
            skin.toggle.font = font;
            skin.window.font = font;
            skin.textField.font = font;
            skin.textArea.font = font;
            if (skin.customStyles != null)
            {
                foreach (var customStyle in skin.customStyles)
                {
                    if (customStyle != null) customStyle.font = font;
                }
            }
            if (uiSkin != null) uiSkin.ApplyFont(font);
            if (expeditionTrialUiSkin != null) expeditionTrialUiSkin.ApplyFont(font);
        }

        private static bool FontSupportsText(Font font, string text)
        {
            if (font == null || string.IsNullOrEmpty(text)) return font != null;
            for (var i = 0; i < text.Length; i++)
            {
                var character = text[i];
                if (!char.IsControl(character) && !font.HasCharacter(character)) return false;
            }
            return true;
        }

        private GUIStyle ArchiveStyle(int fontSize, Color color, TextAnchor alignment,
            bool emphasized = false)
        {
            var style = Style(fontSize, color, alignment, FontStyle.Normal);
            style.font = SourceHanSerifFont(emphasized);
            style.richText = false;
            return style;
        }

        private void DrawHomeV38Texture(Rect area, string resourceId)
        {
            var texture = artCatalog.GetHomeV38Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
        }

        private static string HomeV38ExpeditionTextureId(HomeExpeditionButtonVisualState state)
        {
            switch (state)
            {
                case HomeExpeditionButtonVisualState.Continue:
                    return PrototypeArtResourceIds.HomeV38ExpeditionContinue;
                case HomeExpeditionButtonVisualState.FreeNoReward:
                    return PrototypeArtResourceIds.HomeV38ExpeditionFreeNoReward;
                case HomeExpeditionButtonVisualState.Disabled:
                    return PrototypeArtResourceIds.HomeV38ExpeditionDisabled;
                default:
                    return PrototypeArtResourceIds.HomeV38ExpeditionNormal;
            }
        }

        private void ActivateHomeLighthouseCard(bool canRepair)
        {
            if (chapterOneLighthouse.IsRepaired)
            {
                homeNoticeKey = "home.notice.nextIslandPlaceholder";
                return;
            }
            if (!canRepair) return;
            if (!chapterOneLighthouse.TryRepair(ref stoneBalance, ref lampOilBalance, ref mistLensBalance)) return;
            homeNoticeKey = "home.notice.lighthouseLit";
            SaveGame();
        }

        private Rect HomeRect(float x, float y, float width, float height)
        {
            return new Rect(x * DesignWidth, y * layoutContentHeight,
                width * DesignWidth, height * layoutContentHeight);
        }

        private Rect HomeRect(NormalizedUiRect layout)
        {
            return HomeRect(layout.X, layout.Y, layout.Width, layout.Height);
        }

        private float HomeScale()
        {
            return Mathf.Clamp(layoutContentHeight / 2403f, 0.76f, 1.18f);
        }

        private int HomeFont(float referenceSize)
        {
            return Mathf.Max(11, Mathf.RoundToInt(referenceSize * HomeScale()));
        }

        private void DrawHomePlayerProfile(Rect area)
        {
            var avatarSize = Mathf.Min(area.height - 6f, area.width * 0.34f);
            var avatarRect = new Rect(area.x, area.y + (area.height - avatarSize) * 0.5f,
                avatarSize, avatarSize);
            var leadId = "unit.guardian";
            for (var i = 0; i < defenseSlots.Length; i++)
            {
                if (defenseSlots[i] == null) continue;
                leadId = defenseSlots[i].NameKey;
                break;
            }
            DrawCharacterAvatar(leadId, new Rect(avatarRect.x + 10f, avatarRect.y + 10f,
                avatarRect.width - 20f, avatarRect.height - 20f));
            DrawHomeTexture(avatarRect, PrototypeArtResourceIds.HomeTopFrame, ScaleMode.ScaleToFit);

            var textX = avatarRect.xMax + 15f;
            GUI.Label(new Rect(textX, area.y + 7f, area.xMax - textX, area.height * 0.43f),
                T("home.playerName"), Style(HomeFont(23f), new Color(0.14f, 0.24f, 0.26f),
                    TextAnchor.MiddleLeft));
            GUI.Label(new Rect(textX, area.y + area.height * 0.46f,
                    area.xMax - textX, area.height * 0.34f),
                string.Format(T("home.pvpPower"), CurrentPvpDeckPower()),
                Style(HomeFont(15f), new Color(0.18f, 0.30f, 0.31f), TextAnchor.MiddleLeft));
        }

        private int CurrentPvpDeckPower()
        {
            var power = 0;
            for (var i = 0; i < defenseSlots.Length; i++)
            {
                if (defenseSlots[i] != null) power += Mathf.Max(0, defenseSlots[i].Power);
            }
            return power * 140;
        }

        private void DrawHomeResourceRow(Rect area)
        {
            var itemWidth = area.width / 3f;
            DrawHomeResource(new Rect(area.x, area.y, itemWidth, area.height), 0, expeditionEnergy);
            DrawHomeResource(new Rect(area.x + itemWidth, area.y, itemWidth, area.height), 1, skrBalance);
            DrawHomeResource(new Rect(area.x + itemWidth * 2f, area.y, itemWidth, area.height), 2,
                tideCrystalBalance);
            GUI.color = new Color(0.14f, 0.25f, 0.27f, 0.48f);
            GUI.DrawTexture(new Rect(area.x + itemWidth - 1f, area.y + 12f, 2f, area.height - 24f),
                Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + itemWidth * 2f - 1f, area.y + 12f, 2f, area.height - 24f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawHomeResource(Rect area, int kind, int value)
        {
            var iconSize = Mathf.Min(area.height * 0.68f, 48f * HomeScale());
            var icon = new Rect(area.x + 3f, area.center.y - iconSize * 0.5f, iconSize, iconSize);
            if (kind == 0) DrawHomeLanternGlyph(icon);
            else if (kind == 1) DrawHomeSkrGlyph(icon);
            else DrawHomeTextureTinted(icon, PrototypeArtResourceIds.HomeMaterialOilIcon,
                new Color(0.35f, 0.78f, 0.86f));
            GUI.Label(new Rect(icon.xMax + 6f, area.y, area.xMax - icon.xMax - 8f, area.height),
                value.ToString(), Style(HomeFont(22f), new Color(0.11f, 0.22f, 0.25f),
                    TextAnchor.MiddleLeft));
        }

        private static void DrawHomeLanternGlyph(Rect area)
        {
            GUI.color = new Color(0.82f, 0.58f, 0.22f);
            GUI.DrawTexture(new Rect(area.x + area.width * 0.28f, area.y + area.height * 0.18f,
                area.width * 0.44f, area.height * 0.62f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + area.width * 0.20f, area.y + area.height * 0.11f,
                area.width * 0.60f, area.height * 0.10f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + area.width * 0.19f, area.y + area.height * 0.80f,
                area.width * 0.62f, area.height * 0.10f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawHomeSkrGlyph(Rect area)
        {
            DrawHomeTexture(area, PrototypeArtResourceIds.HomeTopFrame, ScaleMode.ScaleToFit);
            GUI.Label(area, "S", Style(HomeFont(18f), new Color(0.57f, 0.42f, 0.18f),
                TextAnchor.MiddleCenter, FontStyle.Bold));
        }

        private bool DrawHomeUtilityEntry(Rect area, string iconResourceId, string labelKey)
        {
            var visualSize = Mathf.Min(area.width, area.height * 0.67f);
            var visual = new Rect(area.center.x - visualSize * 0.5f, area.y, visualSize, visualSize);
            DrawHomeTexture(visual, PrototypeArtResourceIds.HomeTopFrame, ScaleMode.ScaleToFit);
            var iconArea = new Rect(visual.x + visual.width * 0.23f,
                visual.y + visual.height * 0.23f, visual.width * 0.54f, visual.height * 0.54f);
            if (string.IsNullOrEmpty(iconResourceId)) DrawHomeWarehouseGlyph(iconArea);
            else DrawHomeTexture(iconArea, iconResourceId, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(area.x, visual.yMax - 2f, area.width, area.yMax - visual.yMax + 4f),
                T(labelKey), Style(HomeFont(15f), new Color(0.12f, 0.23f, 0.25f),
                    TextAnchor.MiddleCenter));
            return GUI.Button(area, GUIContent.none, GUIStyle.none);
        }

        private static void DrawHomeWarehouseGlyph(Rect area)
        {
            GUI.color = new Color(0.18f, 0.31f, 0.32f);
            GUI.DrawTexture(new Rect(area.x + area.width * 0.08f, area.y + area.height * 0.28f,
                area.width * 0.84f, area.height * 0.12f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + area.width * 0.14f, area.y + area.height * 0.42f,
                area.width * 0.72f, area.height * 0.44f), Texture2D.whiteTexture);
            GUI.color = new Color(0.78f, 0.78f, 0.67f);
            GUI.DrawTexture(new Rect(area.x + area.width * 0.21f, area.y + area.height * 0.49f,
                area.width * 0.58f, area.height * 0.08f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.center.x - area.width * 0.06f,
                area.y + area.height * 0.62f, area.width * 0.12f, area.height * 0.16f),
                Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void OpenWarehouse()
        {
            warehouseTab = WarehouseTab.All;
            warehouseEquipmentScroll = Vector2.zero;
            warehouseEquipmentDetailOpen = false;
            warehouseEquipmentDetailId = null;
            homeTopMenu = 0;
            currentView = ScreenView.Warehouse;
        }

        private void DrawHomeTopMenu()
        {
            var panelHeight = homeTopMenu == 1 || homeTopMenu == 3 ? 236f : 168f;
            var panel = new Rect(690f, Mathf.Max(278f, layoutContentHeight * 0.122f), 334f, panelHeight);
            GUI.color = new Color(0.035f, 0.10f, 0.12f, 0.94f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (homeTopMenu == 3)
            {
                if (GUI.Button(new Rect(panel.x + 18f, panel.y + 24f, 294f, 52f), T("language"),
                        ButtonStyle(UiButtonSemantic.Secondary, 16)))
                {
                    language = language == GameLanguage.ChineseSimplified
                        ? GameLanguage.English
                        : GameLanguage.ChineseSimplified;
                    SaveLanguagePreference();
                }
                if (GUI.Button(new Rect(panel.x + 18f, panel.y + 92f, 294f, 52f), T("home.audio"),
                        ButtonStyle(UiButtonSemantic.Secondary, 16)))
                {
                    homeNoticeKey = "home.notice.audio";
                    homeTopMenu = 0;
                }
                if (Debug.isDebugBuild || Application.isEditor)
                {
                    GUI.Label(new Rect(panel.x + 18f, panel.y + 158f, 294f, 50f), T("buildVersion"),
                        Style(12, new Color(0.72f, 0.82f, 0.84f), TextAnchor.MiddleCenter));
                }
                return;
            }

            var firstKey = homeTopMenu == 1 ? "home.daily" : "home.warehouse";
            var secondKey = homeTopMenu == 1 ? "home.events" : "home.market";
            if (GUI.Button(new Rect(panel.x + 18f, panel.y + 24f, 294f, 52f), T(firstKey),
                    ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                if (homeTopMenu == 1)
                {
                    homeNoticeKey = "home.notice.daily";
                    homeTopMenu = 0;
                }
                else
                {
                    OpenWarehouse();
                }
            }
            if (GUI.Button(new Rect(panel.x + 18f, panel.y + 92f, 294f, 52f), T(secondKey),
                    ButtonStyle(homeTopMenu == 1 ? UiButtonSemantic.Secondary : UiButtonSemantic.Reward, 16)))
            {
                homeNoticeKey = homeTopMenu == 1 ? "home.notice.events" : "home.notice.market";
                homeTopMenu = 0;
            }
            if (homeTopMenu == 1 && GUI.Button(new Rect(panel.x + 18f, panel.y + 160f, 294f, 52f),
                    T("home.settings"), ButtonStyle(UiButtonSemantic.Secondary, 16)))
            {
                homeTopMenu = 3;
            }
        }

        private void DrawHomeLighthouseStatus(Rect statusRect, Rect hitRect, float scale)
        {
            DrawHomeTexture(statusRect, PrototypeArtResourceIds.HomeStatusCard, ScaleMode.StretchToFill);
            var canRepair = !chapterOneLighthouse.IsRepaired &&
                            chapterOneLighthouse.CanRepair(stoneBalance, lampOilBalance, mistLensBalance);
            var titleKey = chapterOneLighthouse.IsRepaired
                ? "lighthouse.lit"
                : canRepair ? "home.lighthouse.repairable" : "lighthouse.unlit";
            GUI.Label(new Rect(statusRect.x + 38f * scale, statusRect.y + 44f * scale,
                    statusRect.width - 76f * scale, 44f * scale), T(titleKey),
                Style(Mathf.RoundToInt(22f * scale), new Color(0.15f, 0.23f, 0.25f),
                    TextAnchor.MiddleCenter, FontStyle.Normal));

            DrawHomeMaterial(new Rect(statusRect.x + 74f * scale, statusRect.y + 112f * scale,
                    205f * scale, 48f * scale), PrototypeArtResourceIds.HomeMaterialStoneIcon,
                T("material.stone") + " " + stoneBalance + "/" + LighthouseRepairState.RequiredStone, scale);
            DrawHomeMaterial(new Rect(statusRect.x + 309f * scale, statusRect.y + 112f * scale,
                    205f * scale, 48f * scale), PrototypeArtResourceIds.HomeMaterialOilIcon,
                T("material.lampOil") + " " + lampOilBalance + "/" + LighthouseRepairState.RequiredLampOil, scale);
            DrawHomeMaterial(new Rect(statusRect.x + 544f * scale, statusRect.y + 112f * scale,
                    205f * scale, 48f * scale), PrototypeArtResourceIds.HomeMaterialLensIcon,
                T("material.mistLens") + " " + mistLensBalance + "/" + LighthouseRepairState.RequiredMistLens, scale);

            var footerKey = chapterOneLighthouse.IsRepaired
                ? "home.nextIslandLit"
                : canRepair ? "lighthouse.repair" :
                    (chapterOneLighthouse.BossDefeated ? "lighthouse.bossDone" : "lighthouse.bossNeeded");
            GUI.Label(new Rect(statusRect.x + 58f * scale, statusRect.y + 181f * scale,
                    statusRect.width - 116f * scale, 38f * scale), T(footerKey),
                Style(Mathf.RoundToInt(14f * scale), canRepair || chapterOneLighthouse.IsRepaired
                        ? new Color(0.58f, 0.39f, 0.10f) : new Color(0.25f, 0.35f, 0.37f),
                    TextAnchor.MiddleCenter));

            var actionHit = new Rect(statusRect.x + 76f * scale, statusRect.y + 192f * scale,
                statusRect.width - 152f * scale, 44f * scale);
            if (chapterOneLighthouse.IsRepaired && GUI.Button(actionHit, GUIContent.none, GUIStyle.none))
            {
                homeNoticeKey = "home.notice.nextIslandPlaceholder";
                return;
            }
            if (!canRepair || !GUI.Button(actionHit, GUIContent.none, GUIStyle.none)) return;
            if (chapterOneLighthouse.TryRepair(ref stoneBalance, ref lampOilBalance, ref mistLensBalance))
            {
                homeNoticeKey = "home.notice.lighthouseLit";
                SaveGame();
            }
        }

        private void DrawHomeMaterial(Rect area, string resourceId, string text, float scale)
        {
            var iconSize = 36f * scale;
            DrawHomeTexture(new Rect(area.x, area.y + (area.height - iconSize) * 0.5f, iconSize, iconSize),
                resourceId, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(area.x + iconSize + 6f * scale, area.y,
                    area.width - iconSize - 6f * scale, area.height), text,
                Style(Mathf.RoundToInt(13f * scale), new Color(0.15f, 0.23f, 0.25f), TextAnchor.MiddleLeft));
        }

        private GUIStyle HomePrimaryButtonStyle()
        {
            var style = new GUIStyle(GUI.skin.button)
            {
                fontSize = 28,
                fontStyle = FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            var normal = artCatalog.GetHomeUiTexture(PrototypeArtResourceIds.HomePrimaryNormal);
            var pressed = artCatalog.GetHomeUiTexture(PrototypeArtResourceIds.HomePrimaryPressed);
            var disabled = artCatalog.GetHomeUiTexture(PrototypeArtResourceIds.HomePrimaryDisabled);
            style.normal.background = normal;
            style.hover.background = normal;
            style.focused.background = normal;
            style.active.background = pressed != null ? pressed : normal;
            style.onActive.background = style.active.background;
            style.normal.textColor = new Color(0.97f, 0.95f, 0.86f);
            style.hover.textColor = style.normal.textColor;
            style.active.textColor = style.normal.textColor;
            if (!GUI.enabled)
            {
                style.normal.background = disabled != null ? disabled : normal;
                style.normal.textColor = new Color(0.20f, 0.28f, 0.29f);
            }
            return style;
        }

        private void DrawHomeTexture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetHomeUiTexture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private void DrawHomeTextureTinted(Rect area, string resourceId, Color tint)
        {
            var texture = artCatalog.GetHomeUiTexture(resourceId);
            if (texture == null) return;
            GUI.color = tint;
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        private void HandleHomeChapterPointer(Rect dragArea)
        {
            var currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 &&
                dragArea.Contains(currentEvent.mousePosition))
            {
                homeChapterPointerDown = currentEvent.mousePosition;
                homeChapterPointerStartOffset = homeChapterOffset;
                homeChapterPointerHeld = true;
                homeChapterPointerDragging = false;
                return;
            }
            if (currentEvent.type == EventType.MouseDrag && homeChapterPointerHeld)
            {
                var delta = currentEvent.mousePosition.x - homeChapterPointerDown.x;
                if (Mathf.Abs(delta) > 72f) homeChapterPointerDragging = true;
                if (homeChapterPointerDragging)
                {
                    homeChapterOffset = Mathf.Clamp(homeChapterPointerStartOffset + delta, -36f, 36f);
                    homeChapterOffsetVelocity = 0f;
                    currentEvent.Use();
                }
                return;
            }
            if (currentEvent.rawType != EventType.MouseUp || !homeChapterPointerHeld) return;
            if (homeChapterPointerDragging) currentEvent.Use();
            homeChapterPointerHeld = false;
            homeChapterPointerDragging = false;
        }

        private string LighthouseRecipeText()
        {
            return T("material.stone") + " " + stoneBalance + "/" + LighthouseRepairState.RequiredStone + "    " +
                   T("material.lampOil") + " " + lampOilBalance + "/" + LighthouseRepairState.RequiredLampOil + "    " +
                   T("material.mistLens") + " " + mistLensBalance + "/" + LighthouseRepairState.RequiredMistLens + "    " +
                   T(chapterOneLighthouse.BossDefeated ? "lighthouse.bossDone" : "lighthouse.bossNeeded");
        }

        private bool HasExpeditionProgress()
        {
            if (expeditionFlow.IsComplete) return false;
            if (expeditionEntryCommitted) return true;
            var isUntouchedMapStart = round == 1 && coins == 10 && health == 10 &&
                                      runRoster.TotalCount == 0 &&
                                      expeditionFlow.Phase == ExpeditionPhase.Route &&
                                      string.Equals(currentMapNodeId, "M00", StringComparison.Ordinal);
            if (isUntouchedMapStart) return false;
            return round > 1 || coins != 10 || health != 10 || runRoster.TotalCount > 0 ||
                   expeditionFlow.Phase != ExpeditionPhase.Preparation;
        }

        private void DrawMainNavigation()
        {
            var views = new[]
            {
                ScreenView.Store, ScreenView.Deck, ScreenView.Home, ScreenView.Arena, ScreenView.Harbor
            };
            var keys = new[]
            {
                "nav.store", "nav.deck", "nav.home", "nav.arena", "nav.harbor"
            };
            string lockedNavigationId;
            if (TryGetHomeV38NavigationId(currentView, out lockedNavigationId))
            {
                DrawHomeNavigation(views, lockedNavigationId);
                return;
            }

            var navigationY = AdaptivePortraitLayout.BottomAnchoredY(layoutContentHeight, 132f, 0f);
            uiSkin.DrawBottomNavigation(new Rect(0f, navigationY, DesignWidth, 132f));
            for (var i = 0; i < views.Length; i++)
            {
                var active = currentView == views[i];
                if (GUI.Button(new Rect(35f + i * 205f, navigationY + 22f, 190f, 78f), T(keys[i]),
                        TabStyle(active, 18)))
                {
                    currentView = views[i];
                }
            }
        }

        private bool TryGetHomeV38NavigationId(ScreenView view, out string resourceId)
        {
            var english = language == GameLanguage.English;
            switch (view)
            {
                case ScreenView.Store:
                    resourceId = english
                        ? PrototypeArtResourceIds.HomeV38BottomNavShopEnglish
                        : PrototypeArtResourceIds.HomeV38BottomNavShop;
                    return true;
                case ScreenView.Deck:
                    resourceId = english
                        ? PrototypeArtResourceIds.HomeV38BottomNavDeckEnglish
                        : PrototypeArtResourceIds.HomeV38BottomNavDeck;
                    return true;
                case ScreenView.Home:
                    resourceId = english
                        ? PrototypeArtResourceIds.HomeV38BottomNavHomeEnglish
                        : PrototypeArtResourceIds.HomeV38BottomNavHome;
                    return true;
                case ScreenView.Arena:
                    resourceId = english
                        ? PrototypeArtResourceIds.HomeV38BottomNavPvpEnglish
                        : PrototypeArtResourceIds.HomeV38BottomNavPvp;
                    return true;
                case ScreenView.Harbor:
                    resourceId = english
                        ? PrototypeArtResourceIds.HomeV38BottomNavHarborEnglish
                        : PrototypeArtResourceIds.HomeV38BottomNavHarbor;
                    return true;
                default:
                    resourceId = null;
                    return false;
            }
        }

        private void DrawHomeNavigation(ScreenView[] views, string navigationResourceId)
        {
            var dockHeight = HomeUiV38Layout.BottomNavigation.Height;
            var dockY = currentView == ScreenView.Home
                ? HomeUiV38Layout.BottomNavigation.Y
                : AdaptivePortraitLayout.BottomAnchoredY(layoutContentHeight, dockHeight, 0f);
            DrawHomeV38Texture(new Rect(0f, dockY, HomeUiV38Layout.BottomNavigation.Width, dockHeight),
                navigationResourceId);
            for (var i = 0; i < views.Length; i++)
            {
                var referenceSlot = HomeUiV38Layout.BottomNavigationSlot(i);
                var slot = new Rect(referenceSlot.X, dockY, referenceSlot.Width, dockHeight);
                if (!GUI.Button(slot, GUIContent.none, GUIStyle.none)) continue;
                homeTopMenu = 0;
                currentView = views[i];
            }
        }

        private void DrawPlaceholderMainScreen(string titleKey, string subtitleKey)
        {
            var pageExtra = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 1f);
            GUI.Label(new Rect(55f, 205f, 970f, 64f), T(titleKey),
                Style(34, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 280f, 970f, 70f), T(subtitleKey),
                Style(19, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleLeft));
            uiSkin.DrawPanel(new Rect(55f, 410f, 970f, 820f + pageExtra * 0.8f), false);
            GUI.Label(new Rect(100f, 700f + pageExtra * 0.4f, 880f, 160f), T("placeholder.planned"),
                Style(27, new Color(0.62f, 0.74f, 0.79f), TextAnchor.MiddleCenter, FontStyle.Normal));
        }

        private void DrawArenaScreen()
        {
            DrawPlaceholderMainScreen("arena.title", "arena.subtitle");
            var actionY = 1025f + AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 0.5f);
            if (GUI.Button(new Rect(210f, actionY, 660f, 100f), T("arena.defense"),
                    ButtonStyle(UiButtonSemantic.Primary, 22)))
            {
                currentView = ScreenView.Defense;
            }
        }

        private void DrawWarehouseScreen()
        {
            var pageExtra = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 1f);
            GUI.Label(new Rect(55f, 185f, 970f, 58f), T("warehouse.title"),
                Style(34, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 250f, 970f, 70f), T("warehouse.subtitle"),
                Style(18, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleLeft));
            uiSkin.DrawDivider(new Rect(55f, 300f, 970f, 28f));

            var tabs = new[] { WarehouseTab.All, WarehouseTab.Equipment, WarehouseTab.Materials };
            var tabKeys = new[] { "warehouse.all", "warehouse.equipment", "warehouse.materials" };
            for (var i = 0; i < tabs.Length; i++)
            {
                var selected = warehouseTab == tabs[i];
                if (!GUI.Button(new Rect(55f + i * 335f, 330f, 300f, 70f), T(tabKeys[i]),
                        TabStyle(selected, 19)))
                    continue;
                warehouseTab = tabs[i];
                warehouseEquipmentScroll = Vector2.zero;
            }

            var usedSlots = WarehouseStorageRules.CountUsedSlots(
                equipmentInventory.Instances.Count, stoneBalance, lampOilBalance, mistLensBalance);
            GUI.Label(new Rect(55f, 415f, 620f, 42f),
                string.Format(T("warehouse.capacity"), usedSlots, WarehouseStorageRules.InitialCapacity),
                Style(18, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(675f, 415f, 350f, 42f),
                T("warehouse.crystal") + "  " + tideCrystalBalance,
                Style(18, new Color(0.45f, 0.90f, 0.82f), TextAnchor.MiddleRight, FontStyle.Normal));

            var materialKinds = new List<int>();
            if (stoneBalance > 0) materialKinds.Add(0);
            if (lampOilBalance > 0) materialKinds.Add(1);
            if (mistLensBalance > 0) materialKinds.Add(2);
            var visibleItemCount = warehouseTab == WarehouseTab.Equipment
                ? equipmentInventory.Instances.Count
                : warehouseTab == WarehouseTab.Materials
                    ? materialKinds.Count
                    : materialKinds.Count + equipmentInventory.Instances.Count;
            var displaySlotCount = Mathf.Max(WarehouseStorageRules.InitialCapacity, visibleItemCount);
            var rowCount = Mathf.CeilToInt(displaySlotCount / 5f);
            var contentHeight = Mathf.Max(1120f + pageExtra * 0.9f, rowCount * 153f + 10f);
            uiSkin.DrawPanel(new Rect(45f, 462f, 990f, 1156f + pageExtra * 0.9f), false);
            warehouseEquipmentScroll = GUI.BeginScrollView(
                new Rect(55f, 475f, 970f, 1130f + pageExtra * 0.9f), warehouseEquipmentScroll,
                new Rect(0f, 0f, 940f, contentHeight));
            for (var i = 0; i < displaySlotCount; i++)
            {
                var area = new Rect(10f + i % 5 * 185f, i / 5 * 153f, 170f, 138f);
                var materialIndex = warehouseTab == WarehouseTab.Equipment ? -1 : i;
                if (materialIndex >= 0 && materialIndex < materialKinds.Count)
                {
                    DrawWarehouseMaterialSlot(area, materialKinds[materialIndex]);
                    continue;
                }
                var equipmentIndex = warehouseTab == WarehouseTab.Materials
                    ? -1
                    : warehouseTab == WarehouseTab.All ? i - materialKinds.Count : i;
                if (equipmentIndex >= 0 && equipmentIndex < equipmentInventory.Instances.Count)
                {
                    DrawWarehouseEquipmentSlot(area, equipmentInventory.Instances[equipmentIndex]);
                    continue;
                }
                uiSkin.DrawListRow(area, false);
                GUI.Label(area, T("warehouse.empty"),
                    Style(14, new Color(0.39f, 0.50f, 0.55f), TextAnchor.MiddleCenter));
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(55f, 1625f + pageExtra * 0.9f, 970f, 72f), T("warehouse.prototypeHint"),
                Style(16, new Color(0.75f, 0.70f, 0.48f), TextAnchor.MiddleLeft));
        }

        private void DrawWarehouseEquipmentSlot(Rect area, EquipmentInstanceState instance)
        {
            var template = equipmentCatalog.Get(instance.TemplateId);
            DrawEquipmentCardBase(area, instance.Quality);
            DrawEquipmentIcon(instance.TemplateId, new Rect(area.x + 36f, area.y + 4f, 98f, 72f));
            GUI.Label(new Rect(area.x + 7f, area.y + 75f, area.width - 14f, 25f), T(template.TemplateId),
                Style(13, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 7f, area.y + 98f, area.width - 14f, 20f),
                EquipmentModifierText(template.Modifier),
                Style(11, new Color(0.86f, 0.91f, 0.92f), TextAnchor.MiddleCenter));
            if (!string.IsNullOrEmpty(instance.EquippedCharacterInstanceId))
            {
                GUI.Label(new Rect(area.x + 7f, area.y + 117f, area.width - 14f, 18f), T("warehouse.equipped"),
                    Style(10, new Color(1f, 0.86f, 0.42f), TextAnchor.MiddleCenter, FontStyle.Normal));
            }
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                warehouseEquipmentDetailId = instance.EquipmentInstanceId;
                warehouseEquipmentDetailOpen = true;
            }
        }

        private void DrawWarehouseEquipmentDetailScreen()
        {
            EquipmentInstanceState instance = null;
            foreach (var candidate in equipmentInventory.Instances)
            {
                if (candidate.EquipmentInstanceId != warehouseEquipmentDetailId) continue;
                instance = candidate;
                break;
            }
            if (instance == null)
            {
                warehouseEquipmentDetailOpen = false;
                warehouseEquipmentDetailId = null;
                return;
            }

            var template = equipmentCatalog.Get(instance.TemplateId);
            var rarityColor = EquipmentRarityColor(instance.Quality);
            var panel = WarehouseEquipmentDetailPanel;
            Sprite detailBackground;
            var hasDetailBackground = artCatalog.TryGetEquipmentDetailBackground(
                EquipmentRarityId(instance.Quality), out detailBackground);
            GUI.color = Color.white;
            if (hasDetailBackground)
                GUI.DrawTexture(panel, detailBackground.texture, ScaleMode.StretchToFill, true);
            else
            {
                GUI.color = rarityColor;
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
            }
            GUI.color = Color.white;

            GUI.Label(new Rect(245f, 370f, 590f, 58f), T(instance.TemplateId),
                Style(EquipmentDetailFontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));

            DrawEquipmentDetailSection(new Rect(261f, 449f, 558f, 304f),
                "equipment.detail.descriptionTitle", !hasDetailBackground);
            DrawEquipmentIcon(instance.TemplateId, new Rect(270f, 500f, 220f, 205f));
            GUI.Label(new Rect(515f, 505f, 290f, 82f),
                T(EquipmentQualityKey(instance.Quality)) + " · " + T(EquipmentSlotKey(template.Slot)) + "\n" +
                EquipmentUsageStateText(instance),
                Style(EquipmentDetailFontSize, new Color(0.92f, 0.95f, 0.96f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(515f, 590f, 290f, 155f), T(instance.TemplateId + ".description"),
                Style(EquipmentDetailFontSize, Color.white, TextAnchor.MiddleLeft));

            var baseSection = new Rect(261f, 771f, 558f, 275f);
            DrawEquipmentDetailSection(baseSection, "equipment.detail.baseTitle", !hasDetailBackground);
            GUI.Label(baseSection, EquipmentModifierText(template.Modifier),
                Style(EquipmentDetailFontSize, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));

            DrawEquipmentDetailSection(new Rect(261f, 1064f, 558f, 248f),
                "equipment.detail.magicTitle", !hasDetailBackground);
            GUI.Label(new Rect(280f, 1130f, 520f, 55f), T("equipment.detail.noMagic"),
                Style(EquipmentDetailFontSize, new Color(0.82f, 0.87f, 0.89f), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(275f, 1185f, 530f, 130f), T("equipment.detail.magicHint"),
                Style(EquipmentDetailFontSize, new Color(0.72f, 0.80f, 0.82f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(250f, 1335f, 580f, 100f), T("equipment.detail.prototypeHint"),
                Style(EquipmentDetailFontSize, new Color(0.94f, 0.82f, 0.48f), TextAnchor.MiddleCenter));
        }

        private void DrawEquipmentDetailSection(Rect area, string titleKey, bool drawFallbackBackground)
        {
            if (drawFallbackBackground)
            {
                GUI.color = new Color(0.035f, 0.07f, 0.09f, 0.76f);
                GUI.DrawTexture(area, Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 28f, area.y + 14f, area.width - 56f, 46f), T(titleKey),
                Style(EquipmentDetailFontSize, new Color(0.42f, 0.91f, 0.82f), TextAnchor.MiddleLeft, FontStyle.Normal));
        }

        private bool DrawWarehouseEquipmentDetailBackdrop(Rect panel)
        {
            var transparent = GUIStyle.none;
            var closeRequested = GUI.Button(new Rect(0f, 135f, DesignWidth, panel.y - 135f), GUIContent.none, transparent) |
                                 GUI.Button(new Rect(0f, panel.y, panel.x, panel.height), GUIContent.none, transparent) |
                                 GUI.Button(new Rect(panel.xMax, panel.y, DesignWidth - panel.xMax, panel.height), GUIContent.none, transparent) |
                                 GUI.Button(new Rect(0f, panel.yMax, DesignWidth, layoutContentHeight - panel.yMax), GUIContent.none, transparent);
            if (!closeRequested) return false;
            warehouseEquipmentDetailOpen = false;
            warehouseEquipmentDetailId = null;
            return true;
        }

        private string EquipmentUsageStateText(EquipmentInstanceState instance)
        {
            if (instance.UsageLocks != EquipmentUsageLock.None) return T("equipment.locked");
            if (!string.IsNullOrEmpty(instance.EquippedCharacterInstanceId)) return T("warehouse.equipped");
            return T("warehouse.available");
        }

        private void DrawWarehouseMaterialSlot(Rect area, int kind)
        {
            var key = kind == 0 ? "material.stone" : kind == 1 ? "material.lampOil" : "material.mistLens";
            var count = kind == 0 ? stoneBalance : kind == 1 ? lampOilBalance : mistLensBalance;
            uiSkin.DrawListRow(area, false);
            GUI.Label(new Rect(area.x + 8f, area.y + 18f, area.width - 16f, 42f), T(key),
                Style(15, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 8f, area.y + 62f, area.width - 16f, 36f), "× " + count,
                Style(23, new Color(0.75f, 0.96f, 0.88f), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 8f, area.y + 103f, area.width - 16f, 24f), T("warehouse.stacked"),
                Style(11, new Color(0.82f, 0.86f, 0.84f), TextAnchor.MiddleCenter));
        }

        private static string EquipmentInstanceShortLabel(EquipmentInstanceState instance)
        {
            const string rewardPrefix = "local.reward.";
            return instance.EquipmentInstanceId.StartsWith(rewardPrefix, StringComparison.Ordinal)
                ? "#" + instance.EquipmentInstanceId.Substring(rewardPrefix.Length)
                : "#BASE";
        }

        private void DrawDeckScreen()
        {
            if (characterDetailOpen)
            {
                DrawCharacterDetailScreen();
                return;
            }
            if (deckEditing)
            {
                DrawDeckEditorScreen();
                return;
            }
            var pageExtra = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 1f);
            GUI.Label(new Rect(55f, 180f, 970f, 60f), T("collection.title"),
                Style(34, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 245f, 970f, 60f), T("collection.subtitle"),
                Style(18, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleLeft));
            uiSkin.DrawDivider(new Rect(55f, 285f, 970f, 28f));

            DrawActiveDeckSummary(new Rect(40f, 325f, 1000f, 245f));
            GUI.Label(new Rect(70f, 500f, 670f, 54f), T("deck.nextHint"),
                Style(16, new Color(0.66f, 0.82f, 0.84f), TextAnchor.MiddleLeft));
            if (GUI.Button(new Rect(775f, 490f, 225f, 64f), T("deck.edit"),
                    ButtonStyle(UiButtonSemantic.Secondary, 18)))
            {
                BeginDeckEditing();
            }

            GUI.Label(new Rect(55f, 610f, 500f, 48f), T("collection.ownedInstances"),
                Style(26, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 657f, 970f, 38f), T("collection.instanceHint"),
                Style(14, new Color(0.66f, 0.78f, 0.82f), TextAnchor.MiddleLeft));

            var viewport = new Rect(55f, 710f, 970f, 965f + pageExtra * 0.8f);
            var rows = Mathf.CeilToInt(playerCollection.Entries.Count / 2f);
            var contentHeight = Mathf.Max(viewport.height - 4f, rows * 215f + 10f);
            deckCollectionScroll = GUI.BeginScrollView(viewport, deckCollectionScroll,
                new Rect(0f, 0f, 940f, contentHeight));
            for (var i = 0; i < playerCollection.Entries.Count; i++)
            {
                var column = i % 2;
                var row = i / 2;
                DrawOwnedInstanceCard(new Rect(column * 475f, row * 215f, 455f, 190f),
                    playerCollection.Entries[i], nextRunDeckSelection.ContainsInstance(
                        playerCollection.Entries[i].CharacterInstanceId), false);
            }
            GUI.EndScrollView();
            GUI.Label(new Rect(55f, 1690f + pageExtra * 0.8f, 970f, 54f), T("deck.minimum"),
                Style(14, new Color(0.80f, 0.72f, 0.46f), TextAnchor.MiddleLeft));
        }

        private void BeginDeckEditing()
        {
            deckDraftSelection = new RunDeckSelection(playerCollection,
                nextRunDeckSelection.SelectedInstanceIds, MaximumDeckSize, MinimumDeckSize);
            deckEditorMessageKey = "deck.editorHint";
            deckEditing = true;
            deckCollectionScroll = Vector2.zero;
        }

        private void DrawDeckEditorScreen()
        {
            if (deckDraftSelection == null) BeginDeckEditing();
            var pageExtra = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 1f);
            GUI.Label(new Rect(55f, 175f, 970f, 58f), T("deck.editorTitle"),
                Style(34, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 235f, 970f, 55f), T(deckEditorMessageKey),
                Style(17, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleLeft));

            uiSkin.DrawPanel(new Rect(40f, 310f, 1000f, 370f), true);
            GUI.Label(new Rect(70f, 325f, 600f, 42f),
                T("deck.selected") + "  " + deckDraftSelection.Count + "/" + deckDraftSelection.MaximumSize,
                Style(23, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            for (var i = 0; i < deckDraftSelection.SelectedInstanceIds.Count; i++)
            {
                var owned = playerCollection.GetByInstanceId(deckDraftSelection.SelectedInstanceIds[i]);
                var column = i % 4;
                var row = i / 4;
                var area = new Rect(70f + column * 240f, 385f + row * 130f, 220f, 108f);
                GUI.color = PlayerColor(owned.DefinitionId);
                GUI.DrawTexture(area, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(area.x + 8f, area.y + 10f, area.width - 16f, 34f), T(owned.DefinitionId),
                    Style(17, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
                GUI.Label(new Rect(area.x + 8f, area.y + 48f, area.width - 16f, 30f), CharacterInstanceLabel(owned),
                    Style(13, new Color(0.90f, 0.94f, 0.96f), TextAnchor.MiddleCenter));
            }

            GUI.Label(new Rect(55f, 710f, 970f, 46f), T("collection.chooseInstances"),
                Style(26, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            var viewport = new Rect(55f, 765f, 970f, 650f + pageExtra);
            var rows = Mathf.CeilToInt(playerCollection.Entries.Count / 2f);
            var contentHeight = Mathf.Max(viewport.height - 4f, rows * 215f + 10f);
            deckCollectionScroll = GUI.BeginScrollView(viewport, deckCollectionScroll,
                new Rect(0f, 0f, 940f, contentHeight));
            for (var i = 0; i < playerCollection.Entries.Count; i++)
            {
                var owned = playerCollection.Entries[i];
                var column = i % 2;
                var row = i / 2;
                DrawOwnedInstanceCard(new Rect(column * 475f, row * 215f, 455f, 190f), owned,
                    deckDraftSelection.ContainsInstance(owned.CharacterInstanceId), true);
            }
            GUI.EndScrollView();

            if (GUI.Button(new Rect(55f, 1460f + pageExtra, 460f, 88f), T("deck.cancel"),
                    ButtonStyle(UiButtonSemantic.Danger, 20)))
            {
                deckEditing = false;
                deckDraftSelection = null;
            }
            if (GUI.Button(new Rect(565f, 1460f + pageExtra, 460f, 88f), T("deck.save"),
                    ButtonStyle(UiButtonSemantic.Primary, 21)))
            {
                nextRunDeckSelection = new RunDeckSelection(playerCollection,
                    deckDraftSelection.SelectedInstanceIds, MaximumDeckSize, MinimumDeckSize);
                deckEditing = false;
                deckDraftSelection = null;
                SaveGame();
            }
            GUI.Label(new Rect(55f, 1575f + pageExtra, 970f, 105f), T("deck.signatureHint"),
                Style(16, new Color(0.80f, 0.72f, 0.46f), TextAnchor.MiddleLeft));
        }

        private void DrawOwnedInstanceCard(Rect area, OwnedCharacterState owned, bool selected, bool editable)
        {
            var definition = characterCatalog.Get(owned.DefinitionId);
            uiSkin.DrawListRow(area, selected);
            DrawCharacterAvatar(owned.DefinitionId,
                new Rect(area.x + 12f, area.y + 13f, 140f, area.height - 26f));
            GUI.Label(new Rect(area.x + 170f, area.y + 14f, area.width - 188f, 38f), T(owned.DefinitionId),
                Style(21, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 170f, area.y + 55f, area.width - 188f, 30f),
                CharacterInstanceLabel(owned) + "  ·  " + T(QualityKey(owned.Quality)),
                Style(14, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 170f, area.y + 88f, area.width - 188f, 28f),
                T(definition.FactionId) + " · " + T(definition.ProfessionId),
                Style(14, new Color(0.90f, 0.94f, 0.96f), TextAnchor.MiddleLeft));
            var starText = owned.ExternalStar == 0 ? T("collection.notStarred") :
                string.Format(T("collection.star"), owned.ExternalStar);
            GUI.Label(new Rect(area.x + 170f, area.y + 117f, 190f, 32f), starText,
                Style(14, Color.white, TextAnchor.MiddleLeft));
            if (!editable)
            {
                if (selected)
                    GUI.Label(new Rect(area.x + 300f, area.y + 12f, 135f, 30f), T("deck.inActive"),
                        Style(13, new Color(0.86f, 1f, 0.88f), TextAnchor.MiddleRight, FontStyle.Normal));
                if (GUI.Button(new Rect(area.x + 250f, area.y + 140f, 185f, 38f), T("character.openDetails"),
                        ButtonStyle(UiButtonSemantic.Secondary, 14)))
                {
                    OpenCharacterDetail(owned.CharacterInstanceId);
                }
                return;
            }

            var sameDefinitionSelected = deckDraftSelection.Contains(owned.DefinitionId);
            var key = selected ? "deck.remove" : sameDefinitionSelected ? "deck.replace" : "deck.add";
            GUI.enabled = !selected || deckDraftSelection.Count > deckDraftSelection.MinimumSize;
            if (GUI.Button(new Rect(area.x + 250f, area.y + 140f, 185f, 38f), T(key),
                    ButtonStyle(selected ? UiButtonSemantic.Danger : UiButtonSemantic.Primary, 14)))
            {
                if (selected) deckDraftSelection.TryRemoveInstance(owned.CharacterInstanceId);
                else deckDraftSelection.TrySelectInstance(owned.CharacterInstanceId);
                deckEditorMessageKey = "deck.unsaved";
            }
            GUI.enabled = true;
        }

        private void DrawCharacterAvatar(string definitionId, Rect area)
        {
            Sprite avatar;
            if (artCatalog.TryGetCharacterAvatar(definitionId, out avatar))
            {
                GUI.color = Color.white;
                GUI.DrawTexture(area, avatar.texture, ScaleMode.ScaleToFit, true);
                return;
            }

            GUI.color = PlayerColor(definitionId);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private string CharacterInstanceLabel(OwnedCharacterState owned)
        {
            for (var i = 0; i < playerCollection.Entries.Count; i++)
            {
                if (playerCollection.Entries[i].CharacterInstanceId == owned.CharacterInstanceId)
                    return T("collection.instance") + " #" + (i + 1).ToString("00");
            }
            return T("collection.instance");
        }

        private void DrawActiveDeckSummary(Rect area)
        {
            uiSkin.DrawPanel(area, true);
            GUI.Label(new Rect(area.x + 30f, area.y + 14f, area.width - 60f, 40f), T("deck.configured"),
                Style(26, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            for (var i = 0; i < nextRunDeckSelection.SelectedInstanceIds.Count; i++)
            {
                var owned = playerCollection.GetByInstanceId(nextRunDeckSelection.SelectedInstanceIds[i]);
                var column = i % 4;
                var row = i / 4;
                var buttonArea = new Rect(area.x + 30f + column * 235f, area.y + 58f + row * 54f, 220f, 46f);
                if (GUI.Button(buttonArea,
                        T(owned.DefinitionId) + "  ·  " + CharacterInstanceLabel(owned),
                        ButtonStyle(UiButtonSemantic.Secondary, 13)))
                {
                    OpenCharacterDetail(owned.CharacterInstanceId);
                }
            }
        }

        private void OpenCharacterDetail(string characterInstanceId)
        {
            OwnedCharacterState owned;
            if (!playerCollection.TryGetByInstanceId(characterInstanceId, out owned)) return;
            selectedEquipmentCharacterInstanceId = owned.CharacterInstanceId;
            detailCharacterInstanceId = owned.CharacterInstanceId;
            detailEquipmentPreviewId = null;
            detailEquipmentScroll = Vector2.zero;
            deckEditing = false;
            characterDetailOpen = true;
        }

        private void DrawCharacterDetailScreen()
        {
            OwnedCharacterState selected;
            if (!playerCollection.TryGetByInstanceId(detailCharacterInstanceId, out selected))
            {
                characterDetailOpen = false;
                return;
            }
            var definition = characterCatalog.Get(selected.DefinitionId);
            var lowerPageOffset = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 0.5f);
            var starText = selected.ExternalStar == 0 ? T("collection.notStarred") :
                string.Format(T("collection.star"), selected.ExternalStar);
            GUI.Label(new Rect(55f, 150f, 970f, 55f), T("character.details") + " · " + T(selected.DefinitionId),
                Style(32, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 205f, 970f, 42f),
                CharacterInstanceLabel(selected) + "  ·  " + T(QualityKey(selected.Quality)) + "  ·  " + starText +
                "  ·  " + T(definition.FactionId) + " / " + T(definition.ProfessionId),
                Style(16, new Color(0.76f, 0.85f, 0.89f), TextAnchor.MiddleLeft));

            var portraitPanel = new Rect(40f, 265f, 1000f, 650f);
            GUI.color = new Color(0.065f, 0.16f, 0.20f);
            GUI.DrawTexture(portraitPanel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawCharacterPortrait(selected.DefinitionId,
                CharacterQualityResourceId(selected.Quality), portraitPanel);
            DrawEquipmentSlot(selected, EquipmentSlot.Head, new Rect(65f, 345f, 220f, 110f));
            DrawEquipmentSlot(selected, EquipmentSlot.Body, new Rect(65f, 500f, 220f, 110f));
            DrawEquipmentSlot(selected, EquipmentSlot.Feet, new Rect(65f, 655f, 220f, 110f));
            DrawEquipmentSlot(selected, EquipmentSlot.Hands, new Rect(795f, 420f, 220f, 110f));
            DrawEquipmentSlot(selected, EquipmentSlot.Weapon, new Rect(795f, 585f, 220f, 110f));

            var currentStats = EquipmentDerivedStatCalculator.Calculate(definition, definition.BasePower, 1,
                equipmentInventory.CreateSnapshot(selected.CharacterInstanceId));
            GUI.Label(new Rect(315f, 815f, 450f, 58f), StatsText(currentStats),
                Style(15, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(315f, 865f, 450f, 34f), T("character.detailHint"),
                Style(13, new Color(0.66f, 0.78f, 0.82f), TextAnchor.MiddleCenter));

            GUI.Label(new Rect(55f, 935f, 970f, 46f), T("character.warehouseEquipment"),
                Style(26, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            var viewport = new Rect(55f, 990f, 970f, 405f + lowerPageOffset);
            var rows = Mathf.CeilToInt(equipmentInventory.Instances.Count / 2f);
            var contentHeight = Mathf.Max(viewport.height - 4f, rows * 145f + 8f);
            detailEquipmentScroll = GUI.BeginScrollView(viewport, detailEquipmentScroll,
                new Rect(0f, 0f, 940f, contentHeight));
            for (var i = 0; i < equipmentInventory.Instances.Count; i++)
            {
                var column = i % 2;
                var row = i / 2;
                DrawDetailEquipmentCard(new Rect(column * 475f, row * 145f, 455f, 130f),
                    selected, equipmentInventory.Instances[i]);
            }
            GUI.EndScrollView();

            DrawEquipmentPreview(selected, definition, currentStats, 1420f + lowerPageOffset);
            GUI.Label(new Rect(55f, 1740f + lowerPageOffset, 970f, 64f), T("equipment.runHint"),
                Style(14, new Color(0.80f, 0.72f, 0.46f), TextAnchor.MiddleLeft));
        }

        private void DrawCharacterPortrait(string definitionId, string qualityId, Rect panel)
        {
            CharacterArtDefinition artDefinition;
            Sprite characterSprite;
            if (!artCatalog.TryGetCharacterArt(
                    definitionId, qualityId, out artDefinition, out characterSprite))
            {
                GUI.color = PlayerColor(definitionId);
                GUI.DrawTexture(new Rect(panel.center.x - 150f, panel.y + 155f, 300f, 360f), Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.Label(new Rect(panel.center.x - 145f, panel.y + 295f, 290f, 60f), T(definitionId),
                    Style(24, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
                return;
            }
            var anchorRect = new Rect(panel.center.x - 120f, panel.y + panel.height - 180f, 240f, 140f);
            var canvasRect = BattlePresentationLayout.GetCharacterCanvasRect(anchorRect, characterSprite,
                artDefinition.UntrimmedPivot, artDefinition.ContentBoundsTopOrigin.height,
                350f * artDefinition.VisualScaleMultiplier);
            GUI.color = Color.white;
            GUI.DrawTexture(canvasRect, characterSprite.texture, ScaleMode.StretchToFill, true);
        }

        private void DrawEquipmentSlot(OwnedCharacterState selected, EquipmentSlot slot, Rect area)
        {
            var equipped = FindEquippedEquipment(selected.CharacterInstanceId, slot);
            DrawEquipmentCardBase(area, equipped == null ? EquipmentQuality.Common : equipped.Quality);
            if (equipped == null)
            {
                GUI.Label(area, T(EquipmentSlotKey(slot)) + "\n" + T("equipment.empty"),
                    Style(15, new Color(0.56f, 0.66f, 0.70f), TextAnchor.MiddleCenter, FontStyle.Normal));
                return;
            }
            DrawEquipmentIcon(equipped.TemplateId, new Rect(area.x + 7f, area.y + 8f, 78f, 92f));
            GUI.Label(new Rect(area.x + 90f, area.y + 10f, area.width - 100f, 42f),
                T(EquipmentSlotKey(slot)) + "\n" + T(equipped.TemplateId),
                Style(13, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            if (equipped.UsageLocks != EquipmentUsageLock.None)
                GUI.Label(new Rect(area.x + 90f, area.y + 65f, area.width - 100f, 28f), T("equipment.locked"),
                    Style(11, new Color(1f, 0.82f, 0.40f), TextAnchor.MiddleLeft, FontStyle.Normal));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
                detailEquipmentPreviewId = equipped.EquipmentInstanceId;
        }

        private EquipmentInstanceState FindEquippedEquipment(string characterInstanceId, EquipmentSlot slot)
        {
            foreach (var instance in equipmentInventory.Instances)
            {
                if (instance.EquippedCharacterInstanceId == characterInstanceId &&
                    equipmentCatalog.Get(instance.TemplateId).Slot == slot) return instance;
            }
            return null;
        }

        private void DrawDetailEquipmentCard(Rect area, OwnedCharacterState selected, EquipmentInstanceState instance)
        {
            var template = equipmentCatalog.Get(instance.TemplateId);
            var equippedHere = instance.EquippedCharacterInstanceId == selected.CharacterInstanceId;
            var occupiedElsewhere = !string.IsNullOrEmpty(instance.EquippedCharacterInstanceId) && !equippedHere;
            var locked = instance.UsageLocks != EquipmentUsageLock.None;
            var highlighted = detailEquipmentPreviewId == instance.EquipmentInstanceId;
            if (highlighted)
            {
                GUI.color = new Color(0.30f, 0.95f, 0.82f);
                GUI.DrawTexture(new Rect(area.x - 4f, area.y - 4f, area.width + 8f, area.height + 8f),
                    Texture2D.whiteTexture);
            }
            DrawEquipmentCardBase(area, instance.Quality);
            DrawEquipmentIcon(instance.TemplateId, new Rect(area.x + 10f, area.y + 8f, 98f, 112f));
            GUI.Label(new Rect(area.x + 115f, area.y + 8f, 165f, 36f), T(template.TemplateId),
                Style(17, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 115f, area.y + 45f, 165f, 54f),
                T(EquipmentSlotKey(template.Slot)) + " · " + T(EquipmentQualityKey(instance.Quality)) +
                " · " + EquipmentModifierText(template.Modifier),
                Style(13, Color.white, TextAnchor.MiddleLeft));
            var stateKey = occupiedElsewhere ? "equipment.occupied" : locked ? "equipment.locked" :
                equippedHere ? "equipment.previewUnequip" : "equipment.previewEquip";
            GUI.enabled = !occupiedElsewhere && !locked;
            if (GUI.Button(new Rect(area.x + 285f, area.y + 30f, 150f, 66f), T(stateKey),
                    ButtonStyle(equippedHere ? UiButtonSemantic.Danger : UiButtonSemantic.Primary, 13)))
                detailEquipmentPreviewId = instance.EquipmentInstanceId;
            GUI.enabled = true;
        }

        private void DrawEquipmentPreview(OwnedCharacterState selected, CharacterDefinition definition,
            CombatStatBlock currentStats, float y)
        {
            var area = new Rect(55f, y, 970f, 295f);
            GUI.color = new Color(0.08f, 0.20f, 0.25f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 20f, area.y + 12f, area.width - 40f, 36f), T("equipment.previewTitle"),
                Style(21, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            if (string.IsNullOrEmpty(detailEquipmentPreviewId))
            {
                GUI.Label(new Rect(area.x + 20f, area.y + 70f, area.width - 40f, 100f), T("equipment.previewHint"),
                    Style(17, new Color(0.68f, 0.80f, 0.84f), TextAnchor.MiddleCenter));
                return;
            }

            EquipmentInstanceState target;
            try { target = equipmentInventory.Get(detailEquipmentPreviewId); }
            catch (KeyNotFoundException)
            {
                detailEquipmentPreviewId = null;
                return;
            }
            var removing = target.EquippedCharacterInstanceId == selected.CharacterInstanceId;
            var previewStats = CalculatePreviewStats(selected, definition, target, removing);
            GUI.Label(new Rect(area.x + 20f, area.y + 54f, area.width - 40f, 34f),
                T(target.TemplateId) + " · " + EquipmentModifierText(equipmentCatalog.Get(target.TemplateId).Modifier),
                Style(17, new Color(0.42f, 0.90f, 0.82f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 20f, area.y + 98f, area.width - 40f, 32f),
                T("equipment.current") + "  " + StatsText(currentStats),
                Style(14, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 20f, area.y + 136f, area.width - 40f, 32f),
                T("equipment.after") + "  " + StatsText(previewStats),
                Style(14, new Color(0.90f, 0.82f, 0.52f), TextAnchor.MiddleLeft));

            var canApply = CanApplyEquipmentPreview(selected.CharacterInstanceId, target, removing);
            GUI.enabled = canApply;
            if (GUI.Button(new Rect(area.x + 585f, area.y + 194f, 345f, 70f),
                    T(removing ? "equipment.confirmUnequip" : "equipment.confirmEquip"),
                    ButtonStyle(removing ? UiButtonSemantic.Danger : UiButtonSemantic.Primary, 17)))
            {
                var changed = removing
                    ? equipmentInventory.TryUnequip(target.EquipmentInstanceId, selected.CharacterInstanceId)
                    : equipmentInventory.TryEquip(target.EquipmentInstanceId, selected.CharacterInstanceId);
                if (changed)
                {
                    detailEquipmentPreviewId = null;
                    SaveGame();
                }
            }
            GUI.enabled = true;
        }

        private bool CanApplyEquipmentPreview(string characterInstanceId, EquipmentInstanceState target, bool removing)
        {
            if (target.UsageLocks != EquipmentUsageLock.None) return false;
            if (!string.IsNullOrEmpty(target.EquippedCharacterInstanceId) &&
                target.EquippedCharacterInstanceId != characterInstanceId) return false;
            if (removing) return true;
            var slot = equipmentCatalog.Get(target.TemplateId).Slot;
            var replaced = FindEquippedEquipment(characterInstanceId, slot);
            return replaced == null || replaced.UsageLocks == EquipmentUsageLock.None;
        }

        private CombatStatBlock CalculatePreviewStats(OwnedCharacterState selected, CharacterDefinition definition,
            EquipmentInstanceState target, bool removing)
        {
            var targetSlot = equipmentCatalog.Get(target.TemplateId).Slot;
            var modifier = new EquipmentStatModifier();
            foreach (var instance in equipmentInventory.Instances)
            {
                if (instance.EquippedCharacterInstanceId != selected.CharacterInstanceId) continue;
                if (equipmentCatalog.Get(instance.TemplateId).Slot == targetSlot) continue;
                modifier = EquipmentStatModifier.Add(modifier, equipmentCatalog.Get(instance.TemplateId).Modifier);
            }
            if (!removing)
                modifier = EquipmentStatModifier.Add(modifier, equipmentCatalog.Get(target.TemplateId).Modifier);
            return EquipmentDerivedStatCalculator.Calculate(definition, definition.BasePower, 1,
                new EquipmentLoadoutSnapshot(null, modifier));
        }

        private string StatsText(CombatStatBlock stats)
        {
            return T("healthShort") + " " + stats.MaxHealth + " · " +
                   T("attackShort") + " " + stats.AttackDamage + " · " +
                   T("armorShort") + " " + stats.Armor + " · " +
                   T("healShort") + " " + stats.HealAmount + " · " +
                   T("speed." + stats.SpeedTier.ToString().ToLowerInvariant());
        }

        private static string EquipmentSlotKey(EquipmentSlot slot)
        {
            return "equipment.slot." + slot.ToString().ToLowerInvariant();
        }

        private static string EquipmentQualityKey(EquipmentQuality quality)
        {
            return "quality." + quality.ToString().ToLowerInvariant();
        }

        private void DrawDeckSummary(Rect area, string titleKey, string deckNames, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 30f, area.y + 16f, area.width - 60f, 40f), T(titleKey),
                Style(22, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 30f, area.y + 66f, area.width - 60f, 54f), deckNames,
                Style(22, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft));
        }

        private void DrawCollectionCard(Rect area, OwnedCharacterState owned)
        {
            var definition = characterCatalog.Get(owned.DefinitionId);
            var stats = EquipmentDerivedStatCalculator.Calculate(definition, definition.BasePower, 1,
                equipmentInventory.CreateSnapshot(owned.CharacterInstanceId));
            GUI.color = PlayerColor(owned.DefinitionId);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 14f, area.y + 18f, area.width - 28f, 42f), T(owned.DefinitionId),
                Style(23, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 10f, area.y + 66f, area.width - 20f, 30f),
                PrimaryStatsText(stats), Style(14, Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(area.x + 10f, area.y + 96f, area.width - 20f, 30f),
                SecondaryStatsText(stats) + "  ·  " + T("price") + " " + definition.ShopCost,
                Style(13, Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(area.x + 14f, area.y + 132f, area.width - 28f, 30f),
                T("collection.external") + ": " + T(QualityKey(owned.Quality)),
                Style(15, Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(area.x + 14f, area.y + 164f, area.width - 28f, 30f),
                T(definition.FactionId) + " · " + T(definition.ProfessionId),
                Style(15, new Color(0.88f, 0.94f, 0.96f), TextAnchor.MiddleCenter));
            var starText = owned.ExternalStar == 0 ? T("collection.notStarred") : string.Format(T("collection.star"), owned.ExternalStar);
            GUI.Label(new Rect(area.x + 14f, area.y + 198f, area.width - 28f, 30f),
                starText + "  ·  " + T("collection.owned") + " ×" + owned.CopyCount,
                Style(15, Color.white, TextAnchor.MiddleCenter));

            var isSelected = nextRunDeckSelection.Contains(owned.DefinitionId);
            GUI.enabled = isSelected
                ? nextRunDeckSelection.Count > nextRunDeckSelection.MinimumSize
                : nextRunDeckSelection.Count < nextRunDeckSelection.MaximumSize;
            if (GUI.Button(new Rect(area.x + 18f, area.y + 238f, 156f, 60f),
                    T(isSelected ? "deck.remove" : "deck.add"),
                    ButtonStyle(isSelected ? UiButtonSemantic.Danger : UiButtonSemantic.Primary, 15)))
            {
                if (isSelected) nextRunDeckSelection.TryRemove(owned.DefinitionId);
                else nextRunDeckSelection.TryAdd(owned.DefinitionId);
                SaveGame();
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(area.x + 182f, area.y + 238f, 100f, 60f), T("equipment.select"),
                    ButtonStyle(UiButtonSemantic.Secondary, 15)))
            {
                selectedEquipmentCharacterInstanceId = owned.CharacterInstanceId;
            }
        }

        private void DrawEquipmentPanel()
        {
            OwnedCharacterState selected;
            try
            {
                selected = playerCollection.GetByInstanceId(selectedEquipmentCharacterInstanceId);
            }
            catch (KeyNotFoundException)
            {
                selected = playerCollection.Entries[0];
                selectedEquipmentCharacterInstanceId = selected.CharacterInstanceId;
            }

            var snapshot = equipmentInventory.CreateSnapshot(selected.CharacterInstanceId);
            var definition = characterCatalog.Get(selected.DefinitionId);
            var derived = EquipmentDerivedStatCalculator.Calculate(definition, definition.BasePower, 1, snapshot);
            GUI.Label(new Rect(55f, 1430f, 970f, 40f),
                T("equipment.testTitle") + " · " + T(selected.DefinitionId),
                Style(24, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            var derivedText = T("healthShort") + " " + derived.MaxHealth + "  ·  " +
                              T("attackShort") + " " + derived.AttackDamage + "  ·  " +
                              T("armorShort") + " " + derived.Armor + "  ·  " +
                              T("healShort") + " " + derived.HealAmount + "  ·  " +
                              T("speed." + derived.SpeedTier.ToString().ToLowerInvariant());
            GUI.Label(new Rect(55f, 1470f, 970f, 34f), derivedText,
                Style(16, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(55f, 1502f, 970f, 38f), T("equipment.testHint"),
                Style(14, new Color(0.64f, 0.75f, 0.79f), TextAnchor.MiddleLeft));

            for (var i = 0; i < equipmentInventory.Instances.Count; i++)
            {
                var instance = equipmentInventory.Instances[i];
                var template = equipmentCatalog.Get(instance.TemplateId);
                var area = new Rect(55f + i * 195f, 1550f, 180f, 165f);
                DrawEquipmentCardBase(area, instance.Quality);
                DrawEquipmentIcon(instance.TemplateId, new Rect(area.x + 45f, area.y + 6f, 90f, 78f));
                GUI.Label(new Rect(area.x + 8f, area.y + 79f, area.width - 16f, 28f), T(template.TemplateId),
                    Style(15, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
                GUI.Label(new Rect(area.x + 8f, area.y + 104f, area.width - 16f, 24f),
                    EquipmentModifierText(template.Modifier),
                    Style(13, Color.white, TextAnchor.MiddleCenter));

                var equippedHere = instance.EquippedCharacterInstanceId == selected.CharacterInstanceId;
                var available = instance.UsageLocks == EquipmentUsageLock.None &&
                                (string.IsNullOrEmpty(instance.EquippedCharacterInstanceId) || equippedHere);
                GUI.enabled = available;
                var stateKey = instance.UsageLocks != EquipmentUsageLock.None
                    ? "equipment.locked"
                    : equippedHere ? "equipment.unequip"
                    : string.IsNullOrEmpty(instance.EquippedCharacterInstanceId)
                        ? "equipment.equip"
                        : "equipment.occupied";
                if (GUI.Button(new Rect(area.x + 10f, area.y + 130f, area.width - 20f, 28f), T(stateKey),
                        ButtonStyle(equippedHere ? UiButtonSemantic.Danger : UiButtonSemantic.Primary, 13)))
                {
                    if (equippedHere)
                        equipmentInventory.TryUnequip(instance.EquipmentInstanceId, selected.CharacterInstanceId);
                    else
                        equipmentInventory.TryEquip(instance.EquipmentInstanceId, selected.CharacterInstanceId);
                    SaveGame();
                }
                GUI.enabled = true;
            }
            GUI.Label(new Rect(55f, 1730f, 970f, 48f), T("equipment.runHint"),
                Style(14, new Color(0.80f, 0.72f, 0.46f), TextAnchor.MiddleLeft));
        }

        private string EquipmentModifierText(EquipmentStatModifier modifier)
        {
            if (modifier.Armor > 0) return T("armorShort") + " +" + modifier.Armor;
            if (modifier.AttackDamage > 0) return T("attackShort") + " +" + modifier.AttackDamage;
            if (modifier.HealAmount > 0) return T("healShort") + " +" + modifier.HealAmount;
            if (modifier.MaxHealth > 0) return T("healthShort") + " +" + modifier.MaxHealth;
            return T("speedShort");
        }

        private void DrawEquipmentCardBase(Rect area, EquipmentQuality quality)
        {
            uiSkin.DrawListRow(area, false);
            var rarityTint = EquipmentRarityColor(quality);
            rarityTint.a = 0.18f;
            GUI.color = rarityTint;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            Sprite frame;
            if (artCatalog.TryGetEquipmentCardFrame(EquipmentRarityId(quality), out frame))
            {
                GUI.color = Color.white;
                GUI.DrawTexture(area, frame.texture, ScaleMode.StretchToFill, true);
            }
            GUI.color = Color.white;
        }

        private static Color EquipmentRarityColor(EquipmentQuality quality)
        {
            // Current saves contain Common/Rare/Epic. Uncommon and Legendary will use
            // green and orange-gold once stable string rarity ids replace the old enum.
            switch (quality)
            {
                case EquipmentQuality.Rare: return new Color(0.16f, 0.31f, 0.48f);
                case EquipmentQuality.Epic: return new Color(0.34f, 0.22f, 0.48f);
                default: return new Color(0.18f, 0.22f, 0.24f);
            }
        }

        private static string EquipmentRarityId(EquipmentQuality quality)
        {
            switch (quality)
            {
                case EquipmentQuality.Rare: return "rare";
                case EquipmentQuality.Epic: return "epic";
                default: return "common";
            }
        }

        private void DrawEquipmentIcon(string templateId, Rect area)
        {
            Sprite sprite;
            if (!artCatalog.TryGetEquipmentIcon(templateId, out sprite)) return;
            var scale = templateId == "equipment.lanternHelm" ? 0.92f : 1f;
            var drawArea = new Rect(
                area.center.x - area.width * scale * 0.5f,
                area.center.y - area.height * scale * 0.5f,
                area.width * scale,
                area.height * scale);
            GUI.color = Color.white;
            GUI.DrawTexture(drawArea, sprite.texture, ScaleMode.ScaleToFit, true);
        }

        private static string QualityKey(CharacterQuality quality)
        {
            switch (quality)
            {
                case CharacterQuality.Uncommon: return "quality.uncommon";
                case CharacterQuality.Rare: return "quality.rare";
                case CharacterQuality.Epic: return "quality.epic";
                case CharacterQuality.Legendary: return "quality.legendary";
                default: return "quality.common";
            }
        }

        private void DrawStat(Rect area, string value)
        {
            if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawListRow(area, false);
            else
            {
                GUI.color = new Color(0.10f, 0.16f, 0.22f);
                GUI.DrawTexture(area, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(area.x + 18f, area.y + 8f, area.width - 36f, area.height - 16f), value,
                Style(20, PreparationTextPrimary, TextAnchor.MiddleCenter, FontStyle.Normal));
        }

        private void DrawPreparationScreen()
        {
            var overlayAlpha = formationOverlayTransitionActive
                ? FormationBattleV94Layout.FormationOverlayAlpha(formationOverlayTransitionProgress)
                : 1f;
            var overlayScale = formationOverlayTransitionActive
                ? FormationBattleV94Layout.FormationOverlayScale(formationOverlayTransitionProgress)
                : 1f;
            var responsiveLayout = PreparationBattlefieldLayoutMath.Calculate(layoutContentHeight);
            var recycleArea = ScaleAboutCenter(ToRectAtY(FormationBattleV94Layout.RecycleControl,
                responsiveLayout.ToolsY), overlayScale);
            var startArea = ScaleAboutCenter(ToRectAtY(FormationBattleV94Layout.StartControl,
                responsiveLayout.StartButtonY), overlayScale);
            var shopArea = ScaleAboutCenter(ToRectAtY(FormationBattleV94Layout.ShopControl,
                responsiveLayout.ToolsY), overlayScale);
            var controlsEnabled = !preparationShopOpen && !preparationUnitDetailOpen &&
                                  !formationOverlayTransitionActive;
            HandlePreparationPointer(recycleArea);

            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
                DrawFormationPlacementRing(BattlePresentationLayout.GetUnitFootPosition(false, slot, layoutContentHeight),
                    EnemyRosterIndexForFormationSlot(slot) >= 0, overlayAlpha, overlayScale);
            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
                DrawPreparationEnemyV94(slot);

            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
                DrawFormationPlacementRing(BattlePresentationLayout.GetUnitFootPosition(true, slot, layoutContentHeight),
                    runRoster.Get(slot) != null, overlayAlpha, overlayScale);
            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
            {
                var runUnit = runRoster.Get(slot);
                if (runUnit == null) continue;
                DrawPreparationBattlefieldUnit(PreparationBoardSlotRect(slot), runUnit.DefinitionId, slot, true,
                    T(runUnit.DefinitionId), runUnit.RunLevel,
                    preparationPointerHeld && preparationPointerSlot == slot && preparationPointerDragging ? 0.32f : 1f);
                if (IsPreparationMergeMaterial(slot))
                {
                    GUI.color = new Color(1f, 0.78f, 0.24f, 0.95f);
                    DrawRectOutline(PreparationBoardSlotRect(slot), 4f);
                    GUI.color = Color.white;
                }
            }

            DrawFormationBattleBuffGroup(1f);
            DrawFormationBottomControl(recycleArea, T("preparation.sell"), false, overlayAlpha,
                PrototypeArtResourceIds.FormationBattleV942RecycleIcon);
            DrawFormationBottomControl(startArea, T("battle"), true, overlayAlpha, null);
            DrawFormationBottomControl(shopArea, T("preparation.shop"), false, overlayAlpha,
                PrototypeArtResourceIds.FormationBattleV942ShopIcon);
            DrawFormationBench(overlayAlpha, overlayScale);
            DrawFormationBattleSynergyPopupLayer(overlayAlpha);

            GUI.enabled = controlsEnabled && expeditionFlow.CanPrepare;
            if (GUI.Button(startArea, GUIContent.none, GUIStyle.none)) StartBattle();
            if (GUI.Button(shopArea, GUIContent.none, GUIStyle.none))
            {
                preparationBackpackExpanded = false;
                preparationShopOpen = true;
                ResetPreparationPointer();
            }
            GUI.enabled = true;

            DrawPreparationDragGhost();
            if (preparationShopOpen) DrawPreparationShopModal();
        }

        private void DrawPreparationEnemyV94(int formationSlot)
        {
            var enemyIndex = EnemyRosterIndexForFormationSlot(formationSlot);
            if (enemyIndex < 0 || enemyIndex >= enemyUnits.Count) return;
            var unit = enemyUnits[enemyIndex];
            DrawPreparationBattlefieldUnit(PreparationEnemySlotRect(formationSlot), unit.NameKey,
                formationSlot, false, T(unit.NameKey), unit.RunLevel, 0.94f);
        }

        private void DrawFormationPlacementRing(Vector2 foot, bool occupied, float opacity, float scale)
        {
            var width = 200f * Mathf.Max(0.01f, scale);
            var height = 112f * Mathf.Max(0.01f, scale);
            var area = new Rect(foot.x - width * 0.5f, foot.y + 8f - height * 0.5f, width, height);
            DrawFormationBattleV94Texture(area,
                occupied ? PrototypeArtResourceIds.FormationBattleV941GroundOccupied :
                    PrototypeArtResourceIds.FormationBattleV941GroundEmpty,
                ScaleMode.ScaleToFit, opacity);
        }

        private void DrawFormationBottomControl(
            Rect area, string label, bool primary, float opacity, string iconResourceId)
        {
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            var frame = artCatalog.GetFormationBattleV94Texture(primary
                ? PrototypeArtResourceIds.FormationBattleV941StartButton
                : PrototypeArtResourceIds.FormationBattleV941UtilityButton);
            if (frame != null)
                GUI.DrawTexture(area, frame, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
            if (primary || string.IsNullOrEmpty(iconResourceId))
            {
                GUI.Label(area, label, ArchiveStyle(primary ? 44 : 28,
                    WithOpacity(new Color(0.95f, 0.91f, 0.78f), opacity), TextAnchor.MiddleCenter, true));
                return;
            }

            var icon = artCatalog.GetFormationBattleV94Texture(iconResourceId);
            var iconSize = Mathf.Min(72f, Mathf.Min(area.width, area.height) * 2f / 3f);
            GUI.color = WithOpacity(Color.white, opacity);
            if (icon != null) GUI.DrawTexture(new Rect(area.center.x - iconSize * 0.5f,
                    area.center.y - iconSize * 0.5f, iconSize, iconSize),
                icon, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        private void DrawFormationBattleBuffGroup(float opacity)
        {
            var buffs = ActiveFormationBuffs();
            if (buffs.Count == 0)
            {
                selectedFormationBuffId = null;
                formationBuffOverflowOpen = false;
                return;
            }
            var responsiveLayout = PreparationBattlefieldLayoutMath.Calculate(layoutContentHeight);
            var buffY = responsiveLayout.BuffY;
            var visibleCount = Mathf.Min(FormationBattleV94Layout.MaximumVisibleBuffs, buffs.Count);
            var hasOverflow = buffs.Count > FormationBattleV94Layout.MaximumVisibleBuffs;
            var centers = FormationBattleV94Layout.CenteredStatusGroupCenters(
                visibleCount + (hasOverflow ? 1 : 0), buffY);

            for (var index = 0; index < visibleCount; index++)
            {
                var center = centers[index];
                var tile = CenteredSquare(center.X, center.Y, FormationBattleV94Layout.BuffTileSize);
                DrawFormationBattleV94Texture(tile, PrototypeArtResourceIds.FormationBattleV94BuffTile,
                    ScaleMode.StretchToFill, opacity);
                DrawFormationBuffGlyph(buffs[index].Key, Inset(tile, 15f), opacity);
                DrawFormationBattleCountBadge(tile, buffs[index].Value, opacity);
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(tile, GUIContent.none, GUIStyle.none))
                {
                    formationBuffOverflowOpen = false;
                    selectedFormationBuffId = selectedFormationBuffId == buffs[index].Key
                        ? null : buffs[index].Key;
                }
                GUI.enabled = true;
            }

            if (hasOverflow)
            {
                var moreCenter = centers[visibleCount];
                var moreTile = CenteredSquare(moreCenter.X, moreCenter.Y,
                    FormationBattleV94Layout.BuffTileSize);
                DrawFormationBattleV94Texture(moreTile, PrototypeArtResourceIds.FormationBattleV94BuffTile,
                    ScaleMode.StretchToFill, opacity);
                GUI.Label(moreTile, "+", ArchiveStyle(40,
                    WithOpacity(new Color(0.91f, 0.73f, 0.27f), opacity), TextAnchor.MiddleCenter, true));
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(moreTile, GUIContent.none, GUIStyle.none))
                {
                    selectedFormationBuffId = null;
                    formationBuffOverflowOpen = !formationBuffOverflowOpen;
                }
                GUI.enabled = true;
            }

            if (!string.IsNullOrEmpty(selectedFormationBuffId))
                DrawFormationBattleBuffPopup(selectedFormationBuffId, opacity);
            else if (formationBuffOverflowOpen)
                DrawFormationBattleBuffOverflowPopup(buffs, opacity);
        }

        private void DrawFormationBattleBuffPopup(string buffId, float opacity)
        {
            var area = ToRect(FormationBattleV94Layout.BuffPopup);
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawV67NineSlice(area, "ART-UI-PREPARATION-OFFER-CARD-9SLICE-V6.7.0",
                new Vector4(30f, 30f, 30f, 30f));
            GUI.color = oldColor;
            DrawFormationBuffGlyph(buffId, new Rect(area.x + 34f, area.y + 42f, 82f, 82f), opacity);
            GUI.Label(new Rect(area.x + 142f, area.y + 35f, area.width - 180f, 62f),
                FormationBuffTitle(buffId),
                ArchiveStyle(28, WithOpacity(ArchiveInk(), opacity), TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(area.x + 42f, area.y + 122f, area.width - 84f, area.height - 150f),
                FormationBuffDescription(buffId),
                ArchiveStyle(20, WithOpacity(ArchiveInk(), opacity), TextAnchor.UpperLeft));
            CloseFormationBuffPopupOnOutsideRelease(area);
        }

        private void DrawFormationBattleBuffOverflowPopup(
            IReadOnlyList<KeyValuePair<string, int>> buffs, float opacity)
        {
            var area = ToRect(FormationBattleV94Layout.BuffPopup);
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawV67NineSlice(area, "ART-UI-PREPARATION-OFFER-CARD-9SLICE-V6.7.0",
                new Vector4(30f, 30f, 30f, 30f));
            GUI.color = oldColor;
            GUI.Label(new Rect(area.x + 34f, area.y + 24f, area.width - 68f, 48f), T("ui.buff.more"),
                ArchiveStyle(27, WithOpacity(ArchiveInk(), opacity), TextAnchor.MiddleLeft, true));
            var hiddenStart = FormationBattleV94Layout.MaximumVisibleBuffs;
            var rowY = area.y + 82f;
            for (var index = hiddenStart; index < buffs.Count && index < hiddenStart + 4; index++)
            {
                var row = new Rect(area.x + 34f, rowY, area.width - 68f, 42f);
                DrawFormationBuffGlyph(buffs[index].Key, new Rect(row.x, row.y + 3f, 36f, 36f), opacity);
                GUI.Label(new Rect(row.x + 50f, row.y, row.width - 50f, row.height),
                    FormationBuffTitle(buffs[index].Key) + "  ×" + buffs[index].Value,
                    ArchiveStyle(19, WithOpacity(ArchiveInk(), opacity), TextAnchor.MiddleLeft));
                GUI.enabled = !formationOverlayTransitionActive;
                if (GUI.Button(row, GUIContent.none, GUIStyle.none))
                {
                    formationBuffOverflowOpen = false;
                    selectedFormationBuffId = buffs[index].Key;
                }
                GUI.enabled = true;
                rowY += 44f;
            }
            CloseFormationBuffPopupOnOutsideRelease(area);
        }

        private void CloseFormationBuffPopupOnOutsideRelease(Rect area)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseUp || currentEvent.button != 0 ||
                area.Contains(currentEvent.mousePosition)) return;
            selectedFormationBuffId = null;
            formationBuffOverflowOpen = false;
        }

        private string FormationBuffTitle(string buffId)
        {
            if (buffId == "attack") return T("upgrade.might.name");
            if (buffId == "health") return T("upgrade.vitality.name");
            if (IsRoguelikeBuffId(buffId)) return T(RoguelikeEnhancementRules.Get(buffId).NameKey);
            if (!string.IsNullOrEmpty(buffId) && buffId.StartsWith("I", StringComparison.Ordinal))
                return T("economy.item." + buffId);
            return T("formation.buff." + buffId + ".name");
        }

        private string FormationBuffDescription(string buffId)
        {
            if (buffId == "attack") return T("upgrade.might.description");
            if (buffId == "health") return T("upgrade.vitality.description");
            if (IsRoguelikeBuffId(buffId))
            {
                var definition = RoguelikeEnhancementRules.Get(buffId);
                return RoguelikeValueText(definition, EffectiveRoguelikeBuffValue(definition)) + " · " +
                       T(definition.ScopeKey) + "\n" + T("rogue.stack") + " " +
                       RoguelikeEnhancementRules.StackCount(roguelikeBuild.AcquiredEffects, buffId) + "/" +
                       definition.MaximumStacks;
            }
            if (!string.IsNullOrEmpty(buffId) && buffId.StartsWith("I", StringComparison.Ordinal))
                return T("economy.item." + buffId + ".description");
            return T("formation.buff." + buffId + ".description");
        }

        private static bool IsRoguelikeBuffId(string buffId)
        {
            switch (buffId)
            {
                case "B01":
                case "B02":
                case "B03":
                case "B04":
                case "B06":
                case "B08":
                case "B10":
                case "B14":
                case "B15":
                    return true;
                default:
                    return false;
            }
        }

        private int RoguelikeBuffBadgeValue(RoguelikeEffectDefinition definition)
        {
            var value = EffectiveRoguelikeBuffValue(definition);
            return definition.EffectId == "B03" || definition.EffectId == "B10"
                ? value : Mathf.RoundToInt(value / 100f);
        }

        private int EffectiveRoguelikeBuffValue(RoguelikeEffectDefinition definition)
        {
            if (definition == null) return 0;
            switch (definition.EffectId)
            {
                case "B01":
                case "B02":
                case "B14":
                    return RoguelikeEnhancementRules.SumValueBasisPoints(
                        roguelikeBuild.AcquiredEffects, definition.EffectId, 10000);
                case "B03":
                    return RoguelikeEnhancementRules.SumValue(
                        roguelikeBuild.AcquiredEffects, definition.EffectId, 80);
                case "B04":
                    return RoguelikeEnhancementRules.SumValueBasisPoints(
                        roguelikeBuild.AcquiredEffects, definition.EffectId, 4000);
                case "B06":
                case "B15":
                    return RoguelikeEnhancementRules.SumValueBasisPoints(
                        roguelikeBuild.AcquiredEffects, definition.EffectId, 3500);
                case "B08":
                    return RoguelikeEnhancementRules.SumValueBasisPoints(
                        roguelikeBuild.AcquiredEffects, definition.EffectId, 2500);
                case "B10":
                    return RoguelikeEnhancementRules.SumValue(
                        roguelikeBuild.AcquiredEffects, definition.EffectId, 28);
                default:
                    return 0;
            }
        }

        private void DrawFormationBench(float opacity, float scale)
        {
            for (var index = 0; index < runRoster.BenchCapacity; index++)
            {
                var area = ScaleAboutCenter(PreparationBenchSlotRect(index), scale);
                DrawFormationBenchSlot(area, runRoster.Get(runRoster.BoardCapacity + index),
                    runRoster.BoardCapacity + index, opacity);
            }
        }

        private void DrawFormationBenchSlot(Rect area, RunUnitState runUnit, int runIndex, float opacity)
        {
            var selected = preparationPointerHeld && preparationPointerSlot == runIndex;
            if (runUnit == null)
            {
                var empty = artCatalog.GetExpeditionUiV67Texture("ART-UI-PREPARATION-SLOT-FRAME-104-V6.7.0");
                GUI.color = WithOpacity(Color.white, opacity);
                if (empty != null) GUI.DrawTexture(area, empty, ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
                return;
            }

            var surface = artCatalog.GetExpeditionUiSharedTexture(AvatarArtContract.OccupiedSlotSurfaceResourceId);
            var rim = artCatalog.GetExpeditionUiSharedTexture(AvatarArtContract.SlotForegroundRimResourceId);
            var mergeMaterial = IsPreparationMergeMaterial(runIndex);
            GUI.color = WithOpacity(selected || mergeMaterial ? new Color(1f, 0.84f, 0.46f) : Color.white, opacity);
            if (surface != null) GUI.DrawTexture(area, surface, ScaleMode.StretchToFill, true);
            GUI.BeginGroup(area);
            Sprite avatar;
            if (artCatalog.TryGetCharacterSlotAvatar(runUnit.DefinitionId, out avatar))
                GUI.DrawTexture(new Rect(0f, 0f, area.width, area.height), avatar.texture, ScaleMode.ScaleToFit, true);
            GUI.EndGroup();
            if (rim != null) GUI.DrawTexture(area, rim, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
        }

        private bool IsPreparationMergeMaterial(int runIndex)
        {
            if (!preparationPointerHeld || !preparationPointerDragging || preparationPointerSlot < 0)
                return false;
            var destination = PreparationSlotAt(preparationPointerPosition);
            if (destination < 0) return false;
            return runRoster.FindManualMergeThirdIndex(preparationPointerSlot, destination) == runIndex;
        }

        private List<KeyValuePair<string, int>> ActiveFormationBuffs()
        {
            var buffs = new List<KeyValuePair<string, int>>();
            if (runAttackBonus > 0) buffs.Add(new KeyValuePair<string, int>("attack", runAttackBonus));
            if (runHealthBonus > 0) buffs.Add(new KeyValuePair<string, int>("health", runHealthBonus));
            var includedRoguelikeEffects = new HashSet<string>(StringComparer.Ordinal);
            foreach (var acquired in roguelikeBuild.AcquiredEffects)
            {
                if (acquired == null || string.IsNullOrEmpty(acquired.effectId) ||
                    !includedRoguelikeEffects.Add(acquired.effectId)) continue;
                if (!IsRoguelikeBuffId(acquired.effectId)) continue;
                var definition = RoguelikeEnhancementRules.Get(acquired.effectId);
                if (!definition.EnabledInD1) continue;
                buffs.Add(new KeyValuePair<string, int>(acquired.effectId,
                    RoguelikeBuffBadgeValue(definition)));
            }
            if (!string.IsNullOrEmpty(expeditionEconomy.NextBattlePreparationId))
                buffs.Add(new KeyValuePair<string, int>(expeditionEconomy.NextBattlePreparationId, 1));
            if (!string.IsNullOrEmpty(expeditionEconomy.ActiveBattleItemId))
                buffs.Add(new KeyValuePair<string, int>(expeditionEconomy.ActiveBattleItemId, 1));
            return buffs;
        }

        private void DrawFormationBuffGlyph(string buffId, Rect area, float opacity)
        {
            var resourceId = buffId == "health" || buffId == "fortify8" || buffId == "fortify10" ||
                             buffId == "B02" || buffId == "B14"
                ? "ART-UI-PREPARATION-ICON-STATLIFE-V6.7.0"
                : buffId == "formationShield5" || buffId == "I01" ||
                  buffId == "B03" || buffId == "B08"
                    ? "ART-UI-PREPARATION-ICON-STATARMOR-V6.7.0"
                    : buffId == "I02"
                        ? "ART-UI-PREPARATION-ICON-STATSPEED-V6.7.0"
                        : "ART-UI-PREPARATION-ICON-STATATTACK-V6.7.0";
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawV67Texture(area, resourceId, ScaleMode.ScaleToFit);
            GUI.color = oldColor;
        }

        private void DrawFormationBattleCountBadge(Rect parent, int count, float opacity)
        {
            var area = new Rect(parent.xMax - 32f, parent.yMax - 32f, 32f, 32f);
            DrawFormationBattleV94Texture(area, PrototypeArtResourceIds.FormationBattleV94CountBadge,
                ScaleMode.StretchToFill, opacity);
            GUI.Label(area, Mathf.Max(0, count).ToString(), ArchiveStyle(18,
                WithOpacity(Color.white, opacity), TextAnchor.MiddleCenter, true));
        }

        private void DrawFormationBattleV94Texture(Rect area, string resourceId, ScaleMode scaleMode,
            float opacity)
        {
            var texture = artCatalog.GetFormationBattleV94Texture(resourceId);
            if (texture == null) return;
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            GUI.DrawTexture(area, texture, scaleMode, true);
            GUI.color = oldColor;
        }

        private static Rect ToRect(UiRect value)
        {
            return new Rect(value.X, value.Y, value.Width, value.Height);
        }

        private static Rect ToRectAtY(UiRect value, float y)
        {
            return new Rect(value.X, y, value.Width, value.Height);
        }

        private static Rect CenteredSquare(float centerX, float centerY, float size)
        {
            return new Rect(centerX - size * 0.5f, centerY - size * 0.5f, size, size);
        }

        private static Rect PixelAligned(Rect area)
        {
            return new Rect(Mathf.Round(area.x), Mathf.Round(area.y),
                Mathf.Round(area.width), Mathf.Round(area.height));
        }

        private static Rect Inset(Rect area, float amount)
        {
            return new Rect(area.x + amount, area.y + amount,
                Mathf.Max(0f, area.width - amount * 2f), Mathf.Max(0f, area.height - amount * 2f));
        }

        private static Rect ScaleAboutCenter(Rect area, float scale)
        {
            var width = area.width * scale;
            var height = area.height * scale;
            return new Rect(area.center.x - width * 0.5f, area.center.y - height * 0.5f, width, height);
        }

        private void DrawPreparationBattleItems(float y, bool controlsEnabled)
        {
            for (var index = 0; index < expeditionEconomy.Backpack.Count && index < 3; index++)
            {
                var itemId = expeditionEconomy.Backpack[index];
                GUI.enabled = controlsEnabled && string.IsNullOrEmpty(expeditionEconomy.ActiveBattleItemId);
                if (GUI.Button(new Rect(220f + index * 215f, y, 200f, 38f),
                        T("economy.item." + itemId), ButtonStyle(UiButtonSemantic.Reward, 13)))
                {
                    ActivatePreparationBattleItem(index, itemId);
                    break;
                }
            }
            GUI.enabled = true;
        }

        private void ActivatePreparationBattleItem(int backpackIndex, string itemId)
        {
            var targetSlot = itemId == "I05" ? 0 : selectedRunSlot;
            if (itemId != "I05" && (targetSlot < 0 || targetSlot >= runRoster.BoardCapacity ||
                                    runRoster.Get(targetSlot) == null))
            {
                statusKey = "status.selectItemTarget";
                return;
            }
            var actionId = NewRunEconomyActionId("bind-item");
            var receipt = runEconomyActions.Execute(actionId, runEconomyActions.StateVersion, () =>
            {
                if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                return expeditionEconomy.TryActivateBattleItem(backpackIndex, targetSlot)
                    ? RunEconomyResultCode.Success
                    : RunEconomyResultCode.InvalidTarget;
            });
            if (receipt.ResultCode == RunEconomyResultCode.Success)
                statusKey = itemId == "I04" ? "status.itemActivatedSimplified" : "status.itemActivated";
            else ApplyRunEconomyStatus(receipt.ResultCode, "status.itemActivated");
            SaveGame();
        }

        private bool DiscardPreparationBattleItem(int backpackIndex)
        {
            var actionId = NewRunEconomyActionId("discard-item");
            var receipt = runEconomyActions.Execute(actionId, runEconomyActions.StateVersion, () =>
            {
                if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                return expeditionEconomy.TryDiscardCombatItemAt(backpackIndex)
                    ? RunEconomyResultCode.Success
                    : RunEconomyResultCode.InvalidTarget;
            });
            if (receipt.ResultCode == RunEconomyResultCode.Success) statusKey = "status.itemDiscarded";
            else ApplyRunEconomyStatus(receipt.ResultCode, "status.itemDiscarded");
            SaveGame();
            return receipt.ResultCode == RunEconomyResultCode.Success;
        }

        private Rect PreparationBoardSlotRect(int index)
        {
            var battleRect = BattlePresentationLayout.GetUnitSlotRect(true, index, layoutContentHeight);
            return new Rect(battleRect.x - 10f, battleRect.y - 40f, battleRect.width + 20f, 190f);
        }

        private Rect PreparationEnemySlotRect(int index)
        {
            var battleRect = BattlePresentationLayout.GetUnitSlotRect(false, index, layoutContentHeight);
            return new Rect(battleRect.x - 10f, battleRect.y - 40f, battleRect.width + 20f, 190f);
        }

        private int EnemyRosterIndexForFormationSlot(int formationSlot)
        {
            var bossIndex = BossEnemyIndex();
            if (bossIndex < 0) return formationSlot < enemyUnits.Count ? formationSlot : -1;
            if (formationSlot == 1) return bossIndex;
            if (formationSlot == 4) return -1;
            var allowed = new[] { 0, 2, 3, 5 };
            var minionOrdinal = Array.IndexOf(allowed, formationSlot);
            if (minionOrdinal < 0) return -1;
            var current = 0;
            for (var index = 0; index < enemyUnits.Count; index++)
            {
                if (index == bossIndex) continue;
                if (current == minionOrdinal) return index;
                current++;
            }
            return -1;
        }

        private int EnemyFormationSlotForRosterIndex(int enemyIndex)
        {
            var bossIndex = BossEnemyIndex();
            if (bossIndex < 0) return enemyIndex < 6 ? enemyIndex : -1;
            if (enemyIndex == bossIndex) return 1;
            var minionOrdinal = enemyIndex < bossIndex ? enemyIndex : enemyIndex - 1;
            return FormationBattleV94Layout.BossFormationSlotForEnemyIndex(minionOrdinal + 1);
        }

        private int BossEnemyIndex()
        {
            for (var index = 0; index < enemyUnits.Count; index++)
            {
                var definitionId = enemyUnits[index].NameKey;
                if (!string.IsNullOrEmpty(definitionId) &&
                    definitionId.StartsWith("enemy.b", StringComparison.Ordinal)) return index;
            }
            return -1;
        }

        private Rect PreparationBenchSlotRect(int index)
        {
            var reference = FormationBattleV94Layout.BenchSlot(index);
            var responsiveLayout = PreparationBattlefieldLayoutMath.Calculate(layoutContentHeight);
            return new Rect(reference.X, responsiveLayout.BenchY, reference.Width, reference.Height);
        }

        private void DrawPreparationSlot(Rect area, int runIndex, bool compact)
        {
            var selected = preparationPointerHeld && preparationPointerSlot == runIndex;
            GUI.color = selected
                ? new Color(0.98f, 0.78f, 0.26f, 0.34f)
                : new Color(0.56f, 0.79f, 0.78f, compact ? 0.12f : 0.08f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(0.75f, 0.91f, 0.89f, selected ? 0.92f : (compact ? 0.30f : 0.25f));
            DrawRectOutline(area, compact ? 3f : 4f);
            GUI.color = Color.white;

            var runUnit = runRoster.Get(runIndex);
            if (runUnit == null)
            {
                GUI.Label(area, T("empty"), Style(compact ? 13 : 17,
                    new Color(0.72f, 0.82f, 0.82f, 0.58f), TextAnchor.MiddleCenter));
                return;
            }
            if (compact)
                DrawPreparationFullBodyUnit(area, runUnit, true,
                    selected && preparationPointerDragging ? 0.32f : 1f);
            else
                DrawPreparationBattlefieldUnit(area, runUnit.DefinitionId, runIndex, true,
                    T(runUnit.DefinitionId), runUnit.RunLevel,
                    selected && preparationPointerDragging ? 0.32f : 1f);
        }

        private void DrawPreparationEnemySlot(Rect area, int slot)
        {
            GUI.color = new Color(0.60f, 0.18f, 0.20f, 0.07f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(0.94f, 0.58f, 0.54f, 0.24f);
            DrawRectOutline(area, 3f);
            GUI.color = Color.white;

            if (slot >= enemyUnits.Count)
            {
                GUI.Label(area, T("empty"), Style(14,
                    new Color(0.86f, 0.68f, 0.68f, 0.46f), TextAnchor.MiddleCenter));
                return;
            }

            var unit = enemyUnits[slot];
            DrawPreparationBattlefieldUnit(area, unit.NameKey, slot, false,
                T(unit.NameKey), unit.RunLevel, 0.88f);
        }

        private void DrawPreparationBattlefieldUnit(
            Rect area,
            string definitionId,
            int slot,
            bool isPlayer,
            string caption,
            int runLevel,
            float opacity)
        {
            var battleRect = BattlePresentationLayout.GetUnitSlotRect(isPlayer, slot, layoutContentHeight);
            var stableFootY = battleRect.yMax - 5f;
            var footY = battleRect.yMax - 5f;
            var headTopY = footY - BattlePresentationLayout.DefaultCharacterHeight;
            var visibleCenterX = battleRect.center.x;
            CharacterArtDefinition artDefinition;
            Sprite sprite;
            var qualityId = ResolveUnitLevelBadgeQualityId(definitionId, isPlayer);
            var visibleCharacterWidth = 120f;
            var hasArt = artCatalog.TryGetCharacterArt(definitionId, qualityId,
                out artDefinition, out sprite);
            Rect canvasRect = new Rect();
            Rect visibleRect = new Rect();
            var visualScale = 1f;
            if (hasArt)
            {
                visualScale = FormationBattleVisualScale(definitionId);
                canvasRect = BattlePresentationLayout.GetStaticBattleCanvasRect(
                    battleRect, sprite, visualScale);
                canvasRect.y += BattlePresentationLayout.StaticBattleCanvasHeight * visualScale *
                                artDefinition.VisualYOffsetRelativeToUnitHeight;
                canvasRect.y += canvasRect.height * BattleUnitBaseAlignmentDropPercent;
                visibleRect = artDefinition.GetVisibleContentRect(
                    canvasRect, StaticBattleArtContract.FormalBattleFlipX);
                headTopY = visibleRect.y;
                visibleCenterX = visibleRect.center.x;
                visibleCharacterWidth = visibleRect.width;
            }

            DrawGroundShadow(FormationGroundShadowRect(
                definitionId, battleRect.center.x, footY, 164f, 27f), opacity);
            DrawPieceBase(new Vector2(battleRect.center.x, footY), opacity, visibleCharacterWidth);
            if (hasArt)
            {
                GUI.color = WithOpacity(Color.white, opacity);
                DrawStaticBattleSprite(
                    canvasRect, sprite, StaticBattleArtContract.FormalBattleFlipX);
            }
            else
            {
                headTopY = footY - 95f;
                GUI.color = WithOpacity(PlayerColor(definitionId), opacity);
                GUI.DrawTexture(new Rect(battleRect.center.x - 45f, footY - 95f, 90f, 95f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            var expandedSpacing = BattlePresentationLayout.GetBattlefieldVerticalExpansion(layoutContentHeight) >= 120f;
            var nameFontSize = expandedSpacing ? 24 : 22;
            DrawUnitLevelBadge(visibleCenterX, headTopY, qualityId, runLevel, opacity);
            var nameRect = FormationNameplateRect(caption, nameFontSize,
                battleRect.center.x,
                PieceBaseBottomY(stableFootY, visibleCharacterWidth));
            DrawFormationUnitNameplate(nameRect, caption, opacity, nameFontSize);
        }

        private void DrawUnitLevelBadge(
            float visibleCenterX,
            float visibleTopY,
            string qualityId,
            int runLevel,
            float opacity)
        {
            const float referenceWidth = 150f;
            const float referenceHeight = 44f;
            const float gapAboveVisibleSprite = 10f;
            var texture = artCatalog.GetUnitLevelBadgeTexture(qualityId, runLevel);
            if (texture == null) return;
            var area = PixelAligned(new Rect(
                visibleCenterX - referenceWidth * 0.5f,
                visibleTopY - gapAboveVisibleSprite - referenceHeight,
                referenceWidth,
                referenceHeight));
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            GUI.color = oldColor;
        }

        private string ResolveUnitLevelBadgeQualityId(string definitionId, bool isPlayer)
        {
            if (string.IsNullOrEmpty(definitionId)) return "common";
            string diagnosticQuality;
            if (isPlayer && diagnosticQualityOverrides.TryGetValue(definitionId, out diagnosticQuality))
                return diagnosticQuality;
            if (!isPlayer)
            {
                if (definitionId.StartsWith("enemy.b", StringComparison.Ordinal)) return "legendary";
                if (definitionId.StartsWith("enemy.e", StringComparison.Ordinal)) return "epic";
                return "common";
            }
            if (playerCollection == null) return "common";

            OwnedCharacterState character = null;
            if (runDeck != null)
            {
                var characterInstanceId = runDeck.GetCharacterInstanceId(definitionId);
                playerCollection.TryGetByInstanceId(characterInstanceId, out character);
            }
            if (character == null && playerCollection.Owns(definitionId))
                character = playerCollection.Get(definitionId);
            return character == null ? "common" : CharacterQualityResourceId(character.Quality);
        }

        private static string CharacterQualityResourceId(CharacterQuality quality)
        {
            switch (quality)
            {
                case CharacterQuality.Uncommon: return "uncommon";
                case CharacterQuality.Rare: return "rare";
                case CharacterQuality.Epic: return "epic";
                case CharacterQuality.Legendary: return "legendary";
                default: return "common";
            }
        }

        private void DrawFormationUnitNameplate(Rect area, string caption, float opacity, int fontSize)
        {
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            var frame = artCatalog.GetFormationBattleV94Texture(
                PrototypeArtResourceIds.FormationBattleV942Nameplate);
            area = PixelAligned(area);
            DrawStandaloneHorizontalThreeSlice(frame, area, 56f, 56f);
            GUI.color = oldColor;
            var safeArea = new Rect(area.x + 36f, area.center.y - fontSize * 0.6f,
                area.width - 72f, fontSize * 1.2f);
            var displayCaption = caption ?? string.Empty;
            var style = ArchiveStyle(fontSize,
                WithOpacity(new Color(0.95f, 0.91f, 0.78f), opacity), TextAnchor.MiddleCenter, true);
            if (style.CalcSize(new GUIContent(displayCaption)).x > safeArea.width)
                style = ArchiveStyle(Mathf.Max(14, fontSize - 2),
                    WithOpacity(new Color(0.95f, 0.91f, 0.78f), opacity), TextAnchor.MiddleCenter, true);
            displayCaption = EllipsizeFormationName(displayCaption, style, safeArea.width);
            GUI.Label(safeArea, displayCaption, style);
        }

        private Rect FormationNameplateRect(
            string caption, int fontSize, float centerX, float baseBottomY)
        {
            var measureStyle = ArchiveStyle(fontSize, Color.white, TextAnchor.MiddleCenter, true);
            var measuredTextWidth = measureStyle.CalcSize(new GUIContent(caption ?? string.Empty)).x;
            var width = Mathf.Clamp(measuredTextWidth + 72f, 120f, 420f);
            width = Mathf.Ceil(width * 0.5f) * 2f;
            const float height = 35f;
            const float baseToNameplateGap = 12f;
            return new Rect(centerX - width * 0.5f,
                baseBottomY + baseToNameplateGap, width, height);
        }

        private static string EllipsizeFormationName(string value, GUIStyle style, float maximumWidth)
        {
            if (string.IsNullOrEmpty(value) || style.CalcSize(new GUIContent(value)).x <= maximumWidth)
                return value;
            const string ellipsis = "…";
            var length = value.Length;
            while (length > 0 && style.CalcSize(new GUIContent(value.Substring(0, length) + ellipsis)).x > maximumWidth)
                length--;
            return length > 0 ? value.Substring(0, length) + ellipsis : ellipsis;
        }

        private static Rect FormationStatusBarRect(
            string definitionId, float centerX, float visibleTopY, float meaningfulVisibleWidth)
        {
            var size = FormationStatusBarSize(definitionId, meaningfulVisibleWidth);
            return new Rect(centerX - size.x * 0.5f, visibleTopY - size.y - 12f, size.x, size.y);
        }

        private static Vector2 FormationStatusBarSize(string definitionId, float meaningfulVisibleWidth)
        {
            StaticBattleArtEntry entry;
            var tier = StaticBattleArtContract.TryGet(definitionId, out entry)
                ? entry.Tier
                : StaticBattleArtTier.Normal;
            float minimumWidth;
            float maximumWidth;
            float height;
            switch (tier)
            {
                case StaticBattleArtTier.Elite:
                    minimumWidth = 184f;
                    maximumWidth = 300f;
                    height = 38f;
                    break;
                case StaticBattleArtTier.Boss:
                    minimumWidth = 280f;
                    maximumWidth = 420f;
                    height = 46f;
                    break;
                default:
                    minimumWidth = 152f;
                    maximumWidth = 280f;
                    height = 34f;
                    break;
            }
            var width = Mathf.Clamp(Mathf.Round(meaningfulVisibleWidth * 1.12f + 20f),
                minimumWidth, maximumWidth);
            width = Mathf.Ceil(width * 0.5f) * 2f;
            return new Vector2(width, height);
        }

        private static int FormationStatusCompartmentCount(string definitionId, int maximumValue)
        {
            StaticBattleArtEntry entry;
            var maximumCompartments = 5;
            if (StaticBattleArtContract.TryGet(definitionId, out entry))
            {
                if (entry.Tier == StaticBattleArtTier.Elite) maximumCompartments = 7;
                else if (entry.Tier == StaticBattleArtTier.Boss) maximumCompartments = 10;
            }
            return Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(0, maximumValue) / 5f), 1,
                maximumCompartments);
        }

        private void DrawFormationStatusBar(
            Rect area, float fill, int compartmentCount, string fillResourceId, float opacity)
        {
            area = PixelAligned(area);
            var track = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.FormationBattleV951StatusTrack);
            var fillTexture = artCatalog.GetFormationBattleV95Texture(fillResourceId);
            var notchTexture = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.FormationBattleV951CapacityNotch);
            var oldColor = GUI.color;
            GUI.color = WithOpacity(Color.white, opacity);
            DrawStandaloneNineSlice(track, area, new Vector4(36f, 12f, 36f, 12f));

            fill = Mathf.Clamp01(fill);
            if (fillTexture != null && fill > 0f)
            {
                var heightScale = area.height / 46f;
                var safeLeft = 28f * heightScale;
                var safeWidth = Mathf.Max(0f, area.width - safeLeft * 2f);
                var clipRight = area.x + safeLeft + safeWidth * fill;
                GUI.BeginGroup(new Rect(area.x, area.y, Mathf.Max(0f, clipRight - area.x), area.height));
                GUI.color = WithOpacity(Color.white, opacity);
                GUI.DrawTexture(new Rect(0f, 0f, area.width, area.height), fillTexture,
                    ScaleMode.StretchToFill, true);
                GUI.EndGroup();
            }

            if (notchTexture != null && compartmentCount > 1)
            {
                var notchSize = FormationStatusNotchSizeForHeight(area.height);
                var heightScale = area.height / 46f;
                var safeLeft = 28f * heightScale;
                var safeWidth = Mathf.Max(0f, area.width - safeLeft * 2f);
                GUI.color = WithOpacity(Color.white, opacity);
                for (var index = 1; index < compartmentCount; index++)
                {
                    var x = area.x + safeLeft + safeWidth * index / compartmentCount;
                    var notchArea = new Rect(x - notchSize.x * 0.5f,
                        area.center.y - notchSize.y * 0.5f, notchSize.x, notchSize.y);
                    GUI.DrawTexture(notchArea, notchTexture, ScaleMode.ScaleToFit, true);
                }
            }
            GUI.color = oldColor;
        }

        private static Vector2 FormationStatusNotchSizeForHeight(float barHeight)
        {
            if (barHeight >= 45f) return new Vector2(18f, 16f);
            return barHeight >= 37f ? new Vector2(15f, 14f) : new Vector2(13f, 12f);
        }

        private static float FormationBattleVisualScale(string definitionId)
        {
            StaticBattleArtEntry entry;
            if (!StaticBattleArtContract.TryGet(definitionId, out entry)) return 180f / 160f;
            return 180f / 160f * entry.PresentationScale;
        }

        private static Rect FormationGroundShadowRect(
            string definitionId, float centerX, float footY, float baseWidth, float baseHeight)
        {
            StaticBattleArtEntry entry;
            var shadowScale = StaticBattleArtContract.TryGet(definitionId, out entry)
                ? entry.ShadowScale
                : 1f;
            var width = baseWidth * shadowScale;
            var height = baseHeight * shadowScale;
            return new Rect(centerX - width * 0.5f, footY + 2f - height * 0.5f,
                width, height);
        }

        private void DrawPreparationFullBodyUnit(Rect area, RunUnitState runUnit, bool compact, float opacity)
        {
            CharacterArtDefinition artDefinition;
            Sprite sprite;
            var artArea = compact
                ? new Rect(area.x + 8f, area.y + 12f, area.width - 16f, 190f)
                : new Rect(area.x + 12f, area.y + 12f, area.width - 24f, 232f);
            var headTopY = artArea.y;
            if (artCatalog.TryGetCharacterArt(runUnit.DefinitionId,
                    ResolveUnitLevelBadgeQualityId(runUnit.DefinitionId, true),
                    out artDefinition, out sprite))
            {
                DrawFullCharacterSprite(artArea, artDefinition, sprite, opacity);
                headTopY = GetFullCharacterVisibleTop(artArea, artDefinition, sprite);
            }
            var unit = CreateRunUnitEffectiveView(runUnit);
            var textY = compact ? area.y + 202f : area.y + 244f;
            var levelWidth = compact ? 84f : 108f;
            var levelHeight = compact ? 42f : 54f;
            DrawRunLevelCrescent(new Rect(area.center.x - levelWidth * 0.5f,
                    headTopY - levelHeight - 5f, levelWidth, levelHeight),
                runUnit.RunLevel, opacity);
            GUI.Label(new Rect(area.x + 4f, textY, area.width - 8f, 28f),
                T(runUnit.DefinitionId),
                ArchiveStyle(compact ? 17 : 20, WithOpacity(Color.white, opacity), TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(area.x + 4f, textY + 28f, area.width - 8f, 24f),
                PrimaryStatsText(unit), Style(compact ? 11 : 13,
                    WithOpacity(new Color(0.78f, 0.88f, 0.89f), opacity), TextAnchor.MiddleCenter));
        }

        private void DrawRunLevelCrescent(Rect area, int runLevel, float opacity)
        {
            var level = Mathf.Clamp(runLevel, 1, RunUnitRules.MaximumRunLevel);
            var texture = GetRunLevelCrescentTexture(level);
            if (texture == null) return;
            GUI.color = WithOpacity(Color.white, opacity);
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            GUI.color = Color.white;
        }

        private Texture2D GetRunLevelCrescentTexture(int runLevel)
        {
            var level = Mathf.Clamp(runLevel, 1, RunUnitRules.MaximumRunLevel);
            var index = level - 1;
            if (runLevelCrescentTextures[index] != null) return runLevelCrescentTextures[index];

            var resourceId = "Art/GlobalUI/UnitLevelCrescent/V1/ART-BATTLE-LEVEL-CRESCENT-LV" +
                             level + "-01";
            var texture = Resources.Load<Texture2D>(resourceId);
            if (texture == null)
            {
                var sprite = Resources.Load<Sprite>(resourceId);
                if (sprite != null) texture = sprite.texture;
            }
            if (texture == null) texture = CreateRunLevelCrescentTexture(level);
            runLevelCrescentTextures[index] = texture;
            return texture;
        }

        private static Texture2D CreateRunLevelCrescentTexture(int runLevel)
        {
            const int width = 144;
            const int height = 56;
            const float centerX = width * 0.5f;
            const float centerY = -7f;
            const float innerRadius = 43f;
            const float outerRadius = 59f;
            const float startAngle = 19f;
            const float endAngle = 161f;
            const float segmentGap = 3.2f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Runtime-Level-Crescent-Lv" + runLevel,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[width * height];
            var segmentSpan = (endAngle - startAngle) / RunUnitRules.MaximumRunLevel;
            var litColor = new Color(0.96f, 0.78f, 0.31f, 0.98f);
            var dimColor = new Color(0.22f, 0.31f, 0.34f, 0.82f);
            var litOutline = new Color(1f, 0.91f, 0.57f, 1f);
            var dimOutline = new Color(0.54f, 0.63f, 0.64f, 0.90f);

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var dx = x + 0.5f - centerX;
                    var dy = y + 0.5f - centerY;
                    var radius = Mathf.Sqrt(dx * dx + dy * dy);
                    var angle = Mathf.Atan2(dy, dx) * Mathf.Rad2Deg;
                    if (radius < innerRadius || radius > outerRadius ||
                        angle < startAngle || angle > endAngle) continue;

                    var relativeAngle = angle - startAngle;
                    var segment = Mathf.Clamp(Mathf.FloorToInt(relativeAngle / segmentSpan),
                        0, RunUnitRules.MaximumRunLevel - 1);
                    var segmentAngle = relativeAngle - segment * segmentSpan;
                    if (segmentAngle < segmentGap || segmentAngle > segmentSpan - segmentGap) continue;

                    var radialEdge = Mathf.Min(radius - innerRadius, outerRadius - radius);
                    var angularEdge = Mathf.Min(segmentAngle - segmentGap,
                        segmentSpan - segmentGap - segmentAngle) * 0.72f;
                    var edgeDistance = Mathf.Min(radialEdge, angularEdge);
                    var lit = segment < runLevel;
                    var color = edgeDistance < 1.7f
                        ? (lit ? litOutline : dimOutline)
                        : (lit ? litColor : dimColor);
                    color.a *= Mathf.Clamp01(edgeDistance + 0.35f);
                    pixels[y * width + x] = color;
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, false);
            return texture;
        }

        private static void DrawRectOutline(Rect area, float thickness)
        {
            GUI.DrawTexture(new Rect(area.x, area.y, area.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x, area.yMax - thickness, area.width, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x, area.y, thickness, area.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.xMax - thickness, area.y, thickness, area.height), Texture2D.whiteTexture);
        }

        private static void DrawTrashCanIcon(Rect area, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(area.x + 9f, area.y + 12f, area.width - 18f, 5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + 17f, area.y + 5f, area.width - 34f, 5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + 13f, area.y + 20f, area.width - 26f, area.height - 25f), Texture2D.whiteTexture);
            GUI.color = new Color(0.20f, 0.12f, 0.14f, color.a);
            GUI.DrawTexture(new Rect(area.x + 21f, area.y + 25f, 4f, area.height - 36f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + 31f, area.y + 25f, 4f, area.height - 36f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private static void DrawPreparationToolButton(Rect area, Color background)
        {
            GUI.color = background;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(0.63f, 0.82f, 0.80f, 0.75f);
            DrawRectOutline(area, 3f);
            GUI.color = Color.white;
        }

        private static void DrawShopBagIcon(Rect area, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(area.x + 7f, area.y + 17f, area.width - 14f, area.height - 20f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + 15f, area.y + 7f, area.width - 30f, 5f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.x + 12f, area.y + 8f, 5f, 13f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(area.xMax - 17f, area.y + 8f, 5f, 13f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void HandlePreparationPointer(Rect trashArea)
        {
            if (preparationShopOpen || preparationUnitDetailOpen || !expeditionFlow.CanPrepare)
            {
                ResetPreparationPointer();
                return;
            }
            var currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                var slot = PreparationSlotAt(currentEvent.mousePosition);
                if (slot < 0 || runRoster.Get(slot) == null)
                {
                    var enemyIndex = PreparationEnemyAt(currentEvent.mousePosition);
                    if (enemyIndex < 0) return;
                    preparationUnitDetailOpen = true;
                    preparationDetailIsMonster = true;
                    preparationDetailEnemyIndex = enemyIndex;
                    preparationDetailSlot = -1;
                    currentEvent.Use();
                    return;
                }
                preparationPointerSlot = slot;
                preparationPointerDown = currentEvent.mousePosition;
                preparationPointerPosition = currentEvent.mousePosition;
                preparationPointerHeld = true;
                preparationPointerDragging = false;
                currentEvent.Use();
                return;
            }
            if (currentEvent.type == EventType.MouseDrag && preparationPointerHeld)
            {
                preparationPointerPosition = currentEvent.mousePosition;
                if ((preparationPointerPosition - preparationPointerDown).sqrMagnitude >= 100f)
                    preparationPointerDragging = true;
                currentEvent.Use();
                return;
            }
            if (currentEvent.type != EventType.MouseUp || !preparationPointerHeld) return;

            preparationPointerPosition = currentEvent.mousePosition;
            var source = preparationPointerSlot;
            if (!preparationPointerDragging)
            {
                preparationUnitDetailOpen = true;
                preparationDetailIsMonster = false;
                preparationDetailEnemyIndex = -1;
                preparationDetailSlot = source;
            }
            else if (trashArea.Contains(preparationPointerPosition))
            {
                var refund = 0;
                var actionId = NewRunEconomyActionId("sell");
                var receipt = runEconomyActions.Execute(actionId, runEconomyActions.StateVersion, () =>
                {
                    if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                    if (!runRoster.TrySellAt(source, out refund)) return RunEconomyResultCode.UnitNotFound;
                    coins += refund;
                    return RunEconomyResultCode.Success;
                });
                if (receipt.ResultCode == RunEconomyResultCode.Success)
                    statusKey = refund > 0 ? "status.sold" : "status.soldFree";
                else ApplyRunEconomyStatus(receipt.ResultCode, "status.sold");
                SaveGame();
            }
            else
            {
                var destination = PreparationSlotAt(preparationPointerPosition);
                if (destination >= 0 && destination != source)
                {
                    var sourceUnit = runRoster.Get(source);
                    var destinationUnit = runRoster.Get(destination);
                    if (destinationUnit != null && sourceUnit.DefinitionId == destinationUnit.DefinitionId &&
                        sourceUnit.RunLevel == destinationUnit.RunLevel)
                    {
                        var actionId = NewRunEconomyActionId("merge");
                        var receipt = runEconomyActions.Execute(actionId, runEconomyActions.StateVersion, () =>
                        {
                            if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                            if (sourceUnit.RunLevel >= RunUnitRules.MaximumRunLevel)
                                return RunEconomyResultCode.MaximumRunLevel;
                            int finalLevel;
                            return runRoster.TryManualMerge(source, destination, out finalLevel)
                                ? RunEconomyResultCode.Success
                                : RunEconomyResultCode.MergeRequirementsNotMet;
                        });
                        ApplyRunEconomyStatus(receipt.ResultCode, "status.merged");
                    }
                    else
                    {
                        var actionId = NewRunEconomyActionId("move");
                        var receipt = runEconomyActions.Execute(actionId, runEconomyActions.StateVersion, () =>
                        {
                            if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                            return runRoster.TryMoveOrSwap(source, destination)
                                ? RunEconomyResultCode.Success
                                : RunEconomyResultCode.InvalidTarget;
                        });
                        ApplyRunEconomyStatus(receipt.ResultCode, "status.recruit");
                    }
                    SaveGame();
                }
            }
            ResetPreparationPointer();
            currentEvent.Use();
        }

        private int PreparationEnemyAt(Vector2 position)
        {
            for (var formationSlot = 0; formationSlot < runRoster.BoardCapacity; formationSlot++)
            {
                var enemyIndex = EnemyRosterIndexForFormationSlot(formationSlot);
                if (enemyIndex >= 0 && enemyIndex < enemyUnits.Count &&
                    PreparationEnemySlotRect(formationSlot).Contains(position))
                    return enemyIndex;
            }
            return -1;
        }

        private int PreparationSlotAt(Vector2 position)
        {
            for (var i = 0; i < runRoster.BoardCapacity; i++)
                if (PreparationBoardSlotRect(i).Contains(position)) return i;
            for (var i = 0; i < runRoster.BenchCapacity; i++)
                if (PreparationBenchSlotRect(i).Contains(position)) return runRoster.BoardCapacity + i;
            return -1;
        }

        private void ResetPreparationPointer()
        {
            preparationPointerSlot = -1;
            preparationPointerHeld = false;
            preparationPointerDragging = false;
        }

        private void DrawPreparationDragGhost()
        {
            if (!preparationPointerHeld || !preparationPointerDragging || preparationPointerSlot < 0) return;
            var unit = runRoster.Get(preparationPointerSlot);
            if (unit == null) return;
            var area = new Rect(preparationPointerPosition.x - 78f, preparationPointerPosition.y - 105f, 156f, 210f);
            GUI.color = new Color(0.04f, 0.11f, 0.13f, 0.82f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = new Color(0.94f, 0.78f, 0.27f, 0.92f);
            DrawRectOutline(area, 4f);
            GUI.color = Color.white;
            DrawPreparationFullBodyUnit(area, unit, true, 0.94f);
        }

        private void DrawPreparationShopModal()
        {
            var shopBackground = artCatalog.GetExpeditionUiV52Texture(
                "battlefield-signal-yard-preparation-blur-v5.2");
            var previousMatrix = PushExpeditionUiV53Canvas();
            var canvas = new Rect(0f, 0f, ExpeditionUiV67Layout.ReferenceWidth,
                ExpeditionUiV67Layout.ReferenceHeight);
            AspectCoverTextureRenderer.Draw(canvas, shopBackground, 1f);
            DrawPreparationV67TopControls();

            if (preparationBackpackExpanded)
            {
                DrawPreparationBackpackV67();
                GUI.matrix = previousMatrix;
                return;
            }

            // The expedition page shell is a single shared component. Recruitment must not
            // introduce a visually similar local frame: enhancement and future pages reuse it.
            DrawSharedExpeditionPageShell(ExpeditionUiV67Layout.MainPanel);
            DrawV67Texture(ExpeditionUiV67Layout.HeaderShip,
                "ART-UI-PREPARATION-ICON-HEADERSHIP-V6.7.0", ScaleMode.ScaleToFit);
            GUI.Label(ExpeditionUiV67Layout.HeaderTitle, T("preparation.shopTitle"),
                ArchiveStyle(32, ArchiveInk(), TextAnchor.MiddleLeft, true));
            GUI.Label(ExpeditionUiV67Layout.HeaderSubtitle, T("preparation.shopSubtitle"),
                ArchiveStyle(20, ArchiveMutedInk(), TextAnchor.MiddleLeft));

            var rerollCost = expeditionEconomy.PeekManualRerollCost();
            DrawV67HorizontalThreeSlice(ExpeditionUiV67Layout.Refresh,
                "ART-UI-PREPARATION-SECONDARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            DrawV67CenteredIconText(ExpeditionUiV67Layout.Refresh,
                "ART-UI-PREPARATION-ICON-REFRESH-V6.7.0", T("shop.refresh") + " " + rerollCost,
                20, ArchiveColor("F3E7C8"), 26f, 7f, true, 16);
            GUI.enabled = coins >= rerollCost;
            if (GUI.Button(ExpeditionUiV67Layout.Refresh, GUIContent.none, GUIStyle.none))
                RefreshShopOffers(true);
            GUI.enabled = true;

            DrawV67NineSlice(ExpeditionUiV67Layout.CurrentPanel,
                "ART-UI-PREPARATION-SECTION-FRAME-9SLICE-V6.7.0", new Vector4(22f, 22f, 22f, 22f));
            GUI.Label(ExpeditionUiV67Layout.CurrentTitle,
                T("preparation.lineup") + "  " + runRoster.BoardCount + "/" + runRoster.BoardCapacity,
                ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleLeft, true));
            for (var index = 0; index < runRoster.BoardCapacity; index++)
                DrawV67RosterSlot(ExpeditionUiV67Layout.LineupSlotCenters[index], runRoster.Get(index));

            for (var i = 0; i < ShopOfferCount; i++)
                DrawPreparationShopOfferV67(ExpeditionUiV67Layout.Offers[i], i);

            DrawV67NineSlice(ExpeditionUiV67Layout.ReservePanel,
                "ART-UI-PREPARATION-SECTION-FRAME-9SLICE-V6.7.0", new Vector4(22f, 22f, 22f, 22f));
            GUI.Label(ExpeditionUiV67Layout.ReserveTitle,
                T("bench") + "  " + (runRoster.TotalCount - runRoster.BoardCount) + "/" + runRoster.BenchCapacity,
                ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleLeft, true));
            for (var index = 0; index < runRoster.BenchCapacity; index++)
                DrawV67RosterSlot(ExpeditionUiV67Layout.ReserveSlotCenters[index],
                    runRoster.Get(runRoster.BoardCapacity + index));

            DrawFormationBottomControl(ExpeditionUiV67Layout.Backpack, string.Empty, false, 1f,
                PrototypeArtResourceIds.FormationBattleV944BackpackIcon);
            if (GUI.Button(ExpeditionUiV67Layout.Backpack, GUIContent.none, GUIStyle.none))
                preparationBackpackExpanded = true;
            DrawFormationBottomControl(ExpeditionUiV67Layout.Recycle, string.Empty, false, 1f,
                PrototypeArtResourceIds.FormationBattleV942RecycleIcon);
            if (GUI.Button(ExpeditionUiV67Layout.Recycle, GUIContent.none, GUIStyle.none))
            {
                preparationBackpackExpanded = false;
                preparationShopOpen = false;
            }

            DrawV67HorizontalThreeSlice(ExpeditionUiV67Layout.Close,
                "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            GUI.Label(ExpeditionUiV67Layout.Close, T("preparation.closeShop"),
                ArchiveStyle(26, ArchiveColor("F4EBD1"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(ExpeditionUiV67Layout.Close, GUIContent.none, GUIStyle.none))
            {
                preparationBackpackExpanded = false;
                preparationShopOpen = false;
            }
            if (Debug.isDebugBuild || Application.isEditor) DrawRunEconomyDiagnostics();
            GUI.matrix = previousMatrix;
        }

        private void DrawRunEconomyDiagnostics()
        {
            var toggle = new Rect(446f, 286f, 168f, 34f);
            if (GUI.Button(toggle,
                    language == GameLanguage.ChineseSimplified ? "经营测试" : "ECON TEST",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                runEconomyDiagnosticsOpen = !runEconomyDiagnosticsOpen;
            if (!runEconomyDiagnosticsOpen) return;

            var panel = new Rect(112f, 438f, 617f, 760f);
            GUI.color = new Color(0.015f, 0.045f, 0.055f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 48f),
                "C batch / run economy v" + RunEconomyRulesVersion,
                Style(19, new Color(0.91f, 0.83f, 0.57f), TextAnchor.MiddleCenter));
            var labels = new[]
            {
                "C01 BASIC", "C02 TRIPLE", "C03 CHAIN", "C04 FULL NO MERGE",
                "C05 FULL CAN MERGE", "C06 DISCOUNT", "C07 BACKPACK", "C08 STALE REPLAY",
                "C09 SIX EPIC/LEGENDARY"
            };
            for (var index = 0; index < labels.Length; index++)
            {
                var column = index % 2;
                var row = index / 2;
                var area = new Rect(panel.x + 28f + column * 292f, panel.y + 86f + row * 92f,
                    268f, 64f);
                if (GUI.Button(area, labels[index], ButtonStyle(UiButtonSemantic.Secondary, 13)))
                {
                    ApplyRunEconomyDiagnosticState(index);
                    runEconomyDiagnosticsOpen = false;
                    return;
                }
            }
            var summary = "stateVersion=" + runEconomyActions.StateVersion +
                          "  actions=" + runEconomyActions.Count +
                          "\nlast=" + runEconomyActions.LastActionId + " / " + runEconomyActions.LastResultCode;
            GUI.Label(new Rect(panel.x + 28f, panel.y + 580f, panel.width - 56f, 82f), summary,
                Style(14, new Color(0.76f, 0.87f, 0.86f), TextAnchor.UpperLeft));
            if (GUI.Button(new Rect(panel.x + 174f, panel.y + 674f, 268f, 58f),
                    language == GameLanguage.ChineseSimplified ? "复制经营摘要" : "COPY ECON SUMMARY",
                    ButtonStyle(UiButtonSemantic.Reward, 13)))
                GUIUtility.systemCopyBuffer = CreateRunEconomyDiagnosticSummary();
        }

        private void ApplyRunEconomyDiagnosticState(int stateIndex)
        {
            runRoster.Clear();
            diagnosticQualityOverrides.Clear();
            coins = 10;
            expeditionEconomy.Reset();
            runEconomyActions.Reset();
            runId = "diag-C" + (stateIndex + 1).ToString("00");
            shopRefreshSerial = 0;
            shopOffers = new string[ShopOfferCount];
            shopOfferLevels = new int[ShopOfferCount];
            shopOfferIds = new string[ShopOfferCount];
            shopOfferListedPrices = new int[ShopOfferCount];
            shopOfferSold = new bool[ShopOfferCount];
            expeditionFlow.TryRestore((int)ExpeditionPhase.Preparation);

            if (stateIndex == 0)
            {
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
                AddDiagnosticUnit(1, "unit.healer", 1, 0);
                AddDiagnosticUnit(2, "unit.thornGuard", 1, 0);
            }
            else if (stateIndex == 1)
            {
                AddDiagnosticUnit(0, "unit.healer", 1, 0);
                AddDiagnosticUnit(1, "unit.healer", 1, 3);
                AddDiagnosticUnit(6, "unit.healer", 1, 3);
            }
            else if (stateIndex == 2)
            {
                for (var index = 0; index < 9; index++)
                    AddDiagnosticUnit(index, "unit.healer", 1, index == 0 ? 0 : 3);
            }
            else if (stateIndex == 3 || stateIndex == 4)
            {
                var ids = new[]
                {
                    "unit.guardian", "unit.healer", "unit.thornGuard", "unit.archer",
                    "unit.starblade", "unit.starpriest"
                };
                if (stateIndex == 3)
                {
                    for (var index = 0; index < runRoster.TotalCapacity; index++)
                        AddDiagnosticUnit(index, ids[index % ids.Length], index < ids.Length ? 1 : 2, 3);
                }
                else
                {
                    AddDiagnosticUnit(0, "unit.guardian", 1, 3);
                    AddDiagnosticUnit(6, "unit.guardian", 1, 3);
                    var slot = 1;
                    for (var idIndex = 1; idIndex < ids.Length; idIndex++)
                    {
                        AddDiagnosticUnit(slot++, ids[idIndex], 1, 3);
                        if (slot == 6) slot++;
                        AddDiagnosticUnit(slot++, ids[idIndex], 2, 9);
                    }
                    // Exactly two matching copies make the locked first offer a valid
                    // full-capacity purchase/merge transaction.
                    SetDiagnosticOffer(0, "unit.guardian", 1);
                }
            }
            else if (stateIndex == 5)
            {
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
                AddDiagnosticUnit(1, "unit.healer", 1, 0);
                var voucherCoins = coins;
                expeditionEconomy.AddRerollVoucher(ref voucherCoins);
                coins = voucherCoins;
                expeditionEconomy.SetRecruitmentDiscount(2, false);
                expeditionEconomy.SetRecruitmentDiscount(1, true);
            }
            else if (stateIndex == 6)
            {
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
                expeditionEconomy.TryAddCombatItem("I01");
                expeditionEconomy.TryAddCombatItem("I02");
                expeditionEconomy.TryAddCombatItem("I03");
            }
            else if (stateIndex == 7)
            {
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
                runEconomyActions.Execute("C08-replayed-buy", 0,
                    () => RunEconomyResultCode.Success);
                runEconomyActions.Execute("C08-replayed-buy", 0,
                    () => RunEconomyResultCode.InvalidTarget);
                runEconomyActions.Execute("C08-stale-move", 0,
                    () => RunEconomyResultCode.Success);
            }
            else if (stateIndex == 8)
            {
                var ids = new[]
                {
                    "unit.guardian", "unit.healer", "unit.thornGuard", "unit.archer",
                    "unit.starblade", "unit.starpriest"
                };
                var qualityIds = new[] { "epic", "legendary", "epic", "legendary", "epic", "legendary" };
                for (var index = 0; index < ids.Length; index++)
                {
                    AddDiagnosticUnit(index, ids[index], 1, 0);
                    diagnosticQualityOverrides[ids[index]] = qualityIds[index];
                }
            }
            else
            {
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
            }

            if (stateIndex != 4) RefreshAllShopOffers();
            else
            {
                SetDiagnosticOffer(1, "unit.healer", 1);
                SetDiagnosticOffer(2, "unit.archer", 1);
            }
            EnsureRunEconomyIdentity();
            statusKey = "status.recruit";
            SaveGame();
        }

        private void AddDiagnosticUnit(int slot, string definitionId, int runLevel, int acquisitionBasis)
        {
            var id = StableRunEconomyId("unit", runId, slot.ToString(), definitionId,
                runLevel.ToString());
            runRoster.TryRestoreAt(slot, new RunUnitState(definitionId,
                characterCatalog.Get(definitionId).BasePower, runLevel, acquisitionBasis, id));
        }

        private void SetDiagnosticOffer(int offerIndex, string definitionId, int runLevel)
        {
            if (shopOffers == null || shopOffers.Length != ShopOfferCount) shopOffers = new string[ShopOfferCount];
            if (shopOfferLevels == null || shopOfferLevels.Length != ShopOfferCount) shopOfferLevels = new int[ShopOfferCount];
            if (shopOfferIds == null || shopOfferIds.Length != ShopOfferCount) shopOfferIds = new string[ShopOfferCount];
            if (shopOfferListedPrices == null || shopOfferListedPrices.Length != ShopOfferCount)
                shopOfferListedPrices = new int[ShopOfferCount];
            if (shopOfferSold == null || shopOfferSold.Length != ShopOfferCount) shopOfferSold = new bool[ShopOfferCount];
            shopOffers[offerIndex] = definitionId;
            shopOfferLevels[offerIndex] = runLevel;
            shopOfferListedPrices[offerIndex] = UnitCost(definitionId, runLevel);
            shopOfferIds[offerIndex] = StableRunEconomyId("offer", runId, "diagnostic", offerIndex.ToString());
            shopOfferSold[offerIndex] = false;
        }

        private void DrawPreparationBackpackV67()
        {
            // This page only composes approved shared recruitment assets. It does not
            // create replacement artwork or paint over defects in an art resource.
            DrawSharedExpeditionPageShell(ExpeditionUiV67Layout.MainPanel);
            DrawFormationBattleV94Texture(ExpeditionUiV67Layout.HeaderShip,
                PrototypeArtResourceIds.FormationBattleV944BackpackIcon, ScaleMode.ScaleToFit, 1f);
            GUI.Label(ExpeditionUiV67Layout.HeaderTitle, T("preparation.backpackTitle"),
                ArchiveStyle(32, ArchiveInk(), TextAnchor.MiddleLeft, true));
            GUI.Label(ExpeditionUiV67Layout.HeaderSubtitle,
                T("preparation.backpackSubtitle") + "  " + expeditionEconomy.Backpack.Count + "/" +
                ExpeditionEconomyState.BackpackCapacity,
                ArchiveStyle(18, ArchiveMutedInk(), TextAnchor.MiddleLeft));

            for (var index = 0; index < ExpeditionEconomyState.BackpackCapacity; index++)
            {
                var area = new Rect(64f, 474f + index * 330f, 713f, 286f);
                DrawV67NineSlice(area, "ART-UI-PREPARATION-SECTION-FRAME-9SLICE-V6.7.0",
                    new Vector4(22f, 22f, 22f, 22f));
                if (index >= expeditionEconomy.Backpack.Count)
                {
                    GUI.Label(area, T("preparation.backpackEmptySlot"),
                        ArchiveStyle(24, ArchiveMutedInk(), TextAnchor.MiddleCenter));
                    continue;
                }

                var itemId = expeditionEconomy.Backpack[index];
                DrawFormationBuffGlyph(itemId,
                    new Rect(area.x + 36f, area.y + 78f, 112f, 112f), 1f);
                GUI.Label(new Rect(area.x + 174f, area.y + 34f, 350f, 52f),
                    T("economy.item." + itemId),
                    ArchiveStyle(27, ArchiveInk(), TextAnchor.MiddleLeft, true));
                GUI.Label(new Rect(area.x + 174f, area.y + 92f, 350f, 128f),
                    T("economy.item." + itemId + ".description"),
                    ArchiveStyle(18, ArchiveMutedInk(), TextAnchor.UpperLeft));

                var useArea = new Rect(area.xMax - 162f, area.y + 52f, 132f, 72f);
                DrawV67HorizontalThreeSlice(useArea,
                    "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 24f, 24f);
                GUI.Label(useArea, T("preparation.backpackUse"),
                    ArchiveStyle(21, ArchiveColor("F4EBD1"), TextAnchor.MiddleCenter, true));
                GUI.enabled = string.IsNullOrEmpty(expeditionEconomy.ActiveBattleItemId);
                if (GUI.Button(useArea, GUIContent.none, GUIStyle.none))
                {
                    ActivatePreparationBattleItem(index, itemId);
                    GUI.enabled = true;
                    return;
                }
                GUI.enabled = true;

                var discardArea = new Rect(area.xMax - 162f, area.y + 158f, 132f, 62f);
                DrawV67HorizontalThreeSlice(discardArea,
                    "ART-UI-PREPARATION-SECONDARY-BUTTON-9SLICE-V6.7.0", 24f, 24f);
                GUI.Label(discardArea, T("preparation.backpackDiscard"),
                    ArchiveStyle(18, ArchiveInk(), TextAnchor.MiddleCenter, true));
                if (GUI.Button(discardArea, GUIContent.none, GUIStyle.none) &&
                    DiscardPreparationBattleItem(index))
                {
                    return;
                }
            }

            if (!string.IsNullOrEmpty(expeditionEconomy.ActiveBattleItemId))
            {
                GUI.Label(new Rect(94f, 1475f, 653f, 54f),
                    T("preparation.backpackActive") + "  " +
                    T("economy.item." + expeditionEconomy.ActiveBattleItemId),
                    ArchiveStyle(20, ArchiveInk(), TextAnchor.MiddleCenter, true));
            }
            else
            {
                GUI.Label(new Rect(94f, 1475f, 653f, 54f),
                    T("preparation.backpackTargetHint"),
                    ArchiveStyle(17, ArchiveMutedInk(), TextAnchor.MiddleCenter));
            }

            DrawFormationBottomControl(ExpeditionUiV67Layout.Backpack, string.Empty, false, 1f,
                PrototypeArtResourceIds.FormationBattleV944BackpackIcon);
            if (GUI.Button(ExpeditionUiV67Layout.Backpack, GUIContent.none, GUIStyle.none))
                preparationBackpackExpanded = false;

            DrawV67HorizontalThreeSlice(ExpeditionUiV67Layout.Close,
                "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            GUI.Label(ExpeditionUiV67Layout.Close, T("preparation.backToRecruit"),
                ArchiveStyle(26, ArchiveColor("F4EBD1"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(ExpeditionUiV67Layout.Close, GUIContent.none, GUIStyle.none))
                preparationBackpackExpanded = false;
        }

        private void DrawPreparationShopOfferV67(Rect area, int offerIndex)
        {
            DrawV67NineSlice(area, "ART-UI-PREPARATION-OFFER-CARD-9SLICE-V6.7.0",
                new Vector4(22f, 22f, 22f, 22f));
            var nameKey = shopOffers[offerIndex];
            if (string.IsNullOrEmpty(nameKey))
            {
                GUI.Label(area, T("shop.sold"),
                    ArchiveStyle(26, ArchiveMutedInk(), TextAnchor.MiddleCenter, true));
                return;
            }

            var level = ShopOfferLevel(offerIndex);
            var cost = UnitCost(nameKey, level);
            var power = UnitBasePower(nameKey);
            var definition = characterCatalog.Get(nameKey);
            var stats = definition.CalculateCombatStats(power, level);
            var payableCost = expeditionEconomy.QuoteRecruitmentPrice(cost);
            var canBuy = coins >= payableCost && runRoster.CanPurchase(
                new RunUnitState(nameKey, power, level, payableCost));

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(nameKey, out artDefinition, out sprite))
            {
                var characterArea = new Rect(area.x + 18f, area.y + 16f, 174f, 180f);
                characterArea.y += Mathf.Sin(Time.unscaledTime * 1.8f + offerIndex * 1.7f) * 2f;
                DrawPreparationOfferCharacterSprite(characterArea, artDefinition, sprite);
            }

            var textX = area.x + 205f;
            GUI.Label(new Rect(textX, area.y + 25f, 370f, 40f),
                T(nameKey) + "  Lv." + level,
                ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleLeft, true));
            DrawV67FactionIcon(new Rect(area.x + 205f, area.y + 69f, 42f, 42f), definition.FactionId);
            DrawV67ProfessionIcon(new Rect(area.x + 259f, area.y + 69f, 42f, 42f), definition.ProfessionId);
            DrawV67Stats(new Rect(area.x + 198f, area.y + 130f, 322f, 42f), stats);

            var priceArea = new Rect(area.x + 586f, area.y + 25f, 142f, 46f);
            DrawV67CenteredIconText(priceArea, "ART-UI-PREPARATION-ICON-COIN-V6.7.0",
                payableCost.ToString(), 20, canBuy ? ArchiveInk() : ArchiveMutedInk(), 26f, 5f, true, 16);

            var recruitArea = new Rect(area.x + 568f, area.y + 94f, 178f, 76f);
            DrawV67HorizontalThreeSlice(recruitArea,
                "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            GUI.Label(recruitArea, T("shop.buy"),
                ArchiveStyle(26, canBuy ? ArchiveColor("F4F0DD") : ArchiveColor("68747A"),
                    TextAnchor.MiddleCenter, true));
            GUI.enabled = canBuy;
            if (GUI.Button(recruitArea, GUIContent.none, GUIStyle.none))
                Recruit(offerIndex, nameKey, cost, power, level);
            GUI.enabled = true;
        }

        private Matrix4x4 PushExpeditionUiV53Canvas()
        {
            var previous = GUI.matrix;
            var fit = AdaptivePortraitLayoutMath.CalculateFullCanvasFit(
                DesignWidth, layoutCanvasHeight, ExpeditionUiV53Layout.ReferenceWidth,
                ExpeditionUiV53Layout.ReferenceHeight);
            var root = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(layoutScale, layoutScale, 1f));
            GUI.matrix = root * Matrix4x4.TRS(new Vector3(fit.OffsetX, fit.OffsetY, 0f),
                Quaternion.identity, new Vector3(fit.Scale, fit.Scale, 1f));
            return previous;
        }

        private void DrawFullBleedTexture(Texture2D texture)
        {
            if (texture == null) return;
            var previous = GUI.matrix;
            var previousColor = GUI.color;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(layoutScale, layoutScale, 1f));
            GUI.color = Color.white;
            AspectCoverTextureRenderer.Draw(new Rect(0f, 0f, DesignWidth, layoutCanvasHeight), texture, 1f);
            GUI.matrix = previous;
            GUI.color = previousColor;
        }

        private void DrawPreparationV67TopControls()
        {
            DrawV67Texture(ExpeditionUiV67Layout.Back,
                "ART-UI-PREPARATION-BACK-FRAME-104-V6.7.0", ScaleMode.StretchToFill);
            DrawV67Texture(ExpeditionUiV67Layout.BackArrow,
                "ART-UI-PREPARATION-ICON-BACKARROW-V6.7.0", ScaleMode.ScaleToFit);
            if (GUI.Button(ExpeditionUiV67Layout.Back, GUIContent.none, GUIStyle.none))
            {
                preparationBackpackExpanded = false;
                preparationShopOpen = false;
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }

            DrawV67Texture(ExpeditionUiV67Layout.Compass,
                "ART-UI-PREPARATION-ICON-COMPASS-V6.7.0", ScaleMode.ScaleToFit);
            DrawSharedExpeditionTexture(ExpeditionUiV67Layout.Abandon,
                ExpeditionUiSharedFrames.AbandonFrame, ScaleMode.ScaleToFit);
            var abandonLabel = confirmAbandon ? T("run.abandonConfirmShort") : T("run.abandonShort");
            DrawV67CenteredIconText(ExpeditionUiV67Layout.Abandon,
                "ART-UI-PREPARATION-ICON-ABANDONFLAG-V6.7.0", abandonLabel,
                26, ArchiveColor("F6DAD0"), 30f, 7f, true, 16);
            if (GUI.Button(ExpeditionUiV67Layout.Abandon, GUIContent.none, GUIStyle.none))
            {
                if (confirmAbandon) AbandonRun();
                else
                {
                    confirmAbandon = true;
                    statusKey = "status.abandonPrompt";
                }
            }

            DrawV67Hud(ExpeditionUiV67Layout.Round,
                ExpeditionUiSharedFrames.RoundHud,
                "ART-UI-PREPARATION-ICON-ROUND-V6.7.0", round.ToString());
            DrawV67Hud(ExpeditionUiV67Layout.Coins,
                ExpeditionUiSharedFrames.CoinsHud,
                "ART-UI-PREPARATION-ICON-COIN-V6.7.0", coins.ToString());
            DrawV67Hud(ExpeditionUiV67Layout.Life,
                ExpeditionUiSharedFrames.LifeHud,
                "ART-UI-PREPARATION-ICON-LIFE-V6.7.0", health.ToString());
        }

        private void DrawV67Hud(Rect area, string sharedFrameResourceId,
            string iconResourceId, string value)
        {
            DrawSharedExpeditionTexture(area, sharedFrameResourceId);
            DrawV67CenteredIconText(area, iconResourceId, value,
                26, ArchiveColor("F4F0DD"), 34f, 10f, true, 18);
        }

        private void DrawV67RosterSlot(Vector2 center, RunUnitState runUnit)
        {
            var diameter = ExpeditionUiV67Layout.SlotDiameter;
            var area = new Rect(center.x - diameter * 0.5f, center.y - diameter * 0.5f,
                diameter, diameter);
            if (runUnit == null)
            {
                DrawV67Texture(area, "ART-UI-PREPARATION-SLOT-FRAME-104-V6.7.0",
                    ScaleMode.StretchToFill);
                return;
            }

            DrawSharedExpeditionTexture(area, AvatarArtContract.OccupiedSlotSurfaceResourceId);
            DrawRosterAvatarBelowForegroundFrame(area, runUnit);
            DrawSharedExpeditionTexture(area, AvatarArtContract.SlotForegroundRimResourceId);
        }

        private void DrawRosterAvatarBelowForegroundFrame(Rect area, RunUnitState runUnit)
        {
            if (runUnit == null) return;
            GUI.BeginGroup(area);
            DrawCharacterSlotAvatar(runUnit.DefinitionId,
                new Rect(0f, 0f, area.width, area.height));
            GUI.EndGroup();
        }

        private void DrawCharacterSlotAvatar(string definitionId, Rect area)
        {
            Sprite avatar;
            if (artCatalog.TryGetCharacterSlotAvatar(definitionId, out avatar))
            {
                GUI.color = Color.white;
                GUI.DrawTexture(area, avatar.texture, ScaleMode.ScaleToFit, true);
            }
            // Do not fall back to the uncropped portrait. The circular slot asset is the
            // reusable mask contract; drawing a generic portrait here can leak outside
            // the circle even though the foreground rim is rendered afterwards.
        }

        private void DrawV67FactionIcon(Rect area, string factionId)
        {
            var factionKey = ExpeditionArchivePresentation.FactionPresentationKey(factionId);
            var resourceId = factionKey == "steel"
                ? "ART-UI-PREPARATION-ICON-FACTIONRIFTROCK-V6.7.0"
                : factionKey == "wild"
                    ? "ART-UI-PREPARATION-ICON-FACTIONMOSS-V6.7.0"
                    : "ART-UI-PREPARATION-ICON-FACTIONLANTERN-V6.7.0";
            DrawV67Texture(area, resourceId, ScaleMode.ScaleToFit);
        }

        private void DrawV67ProfessionIcon(Rect area, string professionId)
        {
            var resourceId = professionId == "profession.guardian"
                ? "ART-UI-PREPARATION-ICON-PROFESSIONGUARDIAN-V6.7.0"
                : professionId == "profession.support"
                    ? "ART-UI-PREPARATION-ICON-PROFESSIONSUPPORT-V6.7.0"
                    : "ART-UI-PREPARATION-ICON-PROFESSIONOUTPUT-V6.7.0";
            DrawV67Texture(area, resourceId, ScaleMode.ScaleToFit);
        }

        private void DrawV67Stats(Rect area, CombatStatBlock stats)
        {
            var width = area.width * 0.25f;
            DrawV67Stat(new Rect(area.x, area.y, width, area.height),
                "ART-UI-PREPARATION-ICON-STATLIFE-V6.7.0", stats.MaxHealth.ToString());
            DrawV67Stat(new Rect(area.x + width, area.y, width, area.height),
                "ART-UI-PREPARATION-ICON-STATATTACK-V6.7.0", stats.AttackDamage.ToString());
            DrawV67Stat(new Rect(area.x + width * 2f, area.y, width, area.height),
                "ART-UI-PREPARATION-ICON-STATARMOR-V6.7.0", stats.Armor.ToString());
            DrawV67Stat(new Rect(area.x + width * 3f, area.y, width, area.height),
                "ART-UI-PREPARATION-ICON-STATSPEED-V6.7.0", ((int)stats.SpeedTier + 1).ToString());
        }

        private void DrawV67Stat(Rect area, string iconResourceId, string value)
        {
            DrawV67CenteredIconText(area, iconResourceId, value,
                20, ArchiveInk(), 28f, 3f, true, 16);
        }

        private void DrawV67CenteredIconText(Rect area, string iconResourceId, string label,
            int fontSize, Color textColor, float iconSize, float gap, bool emphasized,
            int minimumFontSize)
        {
            var maximumTextWidth = Mathf.Max(0f, area.width - iconSize - gap - 8f);
            float textWidth;
            var style = FitIconTextStyle(
                ArchiveStyle(fontSize, textColor, TextAnchor.MiddleLeft, emphasized),
                label, maximumTextWidth, minimumFontSize, out textWidth);
            var groupWidth = iconSize + gap + textWidth;
            var startX = area.center.x - groupWidth * 0.5f;
            DrawV67Texture(new Rect(startX, area.center.y - iconSize * 0.5f, iconSize, iconSize),
                iconResourceId, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(startX + iconSize + gap, area.y, textWidth, area.height), label, style);
        }

        private void DrawV67Texture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionUiV67Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private void DrawV67NineSlice(Rect area, string resourceId, Vector4 borders)
        {
            var texture = artCatalog.GetExpeditionUiV67Texture(resourceId);
            if (texture == null) return;
            DrawStandaloneNineSlice(texture, area, borders);
        }

        private void DrawV67HorizontalThreeSlice(Rect area, string resourceId,
            float leftBorder, float rightBorder)
        {
            var texture = artCatalog.GetExpeditionUiV67Texture(resourceId);
            DrawStandaloneHorizontalThreeSlice(texture, area, leftBorder, rightBorder);
        }

        private void DrawSharedExpeditionPageShell(Rect area)
        {
            var texture = artCatalog.GetExpeditionUiSharedTexture(
                ExpeditionUiSharedFrames.PageShell);
            DrawStandaloneNineSlice(texture, area, ExpeditionUiSharedFrames.PageShellBorder);
        }

        private void DrawSharedExpeditionTexture(Rect area, string resourceId,
            ScaleMode scaleMode = ScaleMode.StretchToFill)
        {
            var texture = artCatalog.GetExpeditionUiSharedTexture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private static void DrawStandaloneNineSlice(Texture2D texture, Rect destination,
            Vector4 borderLeftTopRightBottom, bool fillCenter = true)
        {
            if (texture == null || destination.width <= 0f || destination.height <= 0f) return;
            var source = new Rect(0f, 0f, texture.width, texture.height);
            var sourceLeft = borderLeftTopRightBottom.x;
            var sourceTop = borderLeftTopRightBottom.y;
            var sourceRight = borderLeftTopRightBottom.z;
            var sourceBottom = borderLeftTopRightBottom.w;
            var destinationLeft = Mathf.Min(sourceLeft, destination.width * 0.5f);
            var destinationTop = Mathf.Min(sourceTop, destination.height * 0.5f);
            var destinationRight = Mathf.Min(sourceRight, destination.width - destinationLeft);
            var destinationBottom = Mathf.Min(sourceBottom, destination.height - destinationTop);

            var sourceCenterWidth = source.width - sourceLeft - sourceRight;
            var sourceCenterHeight = source.height - sourceTop - sourceBottom;
            var destinationCenterWidth = destination.width - destinationLeft - destinationRight;
            var destinationCenterHeight = destination.height - destinationTop - destinationBottom;
            var sourceCenterX = sourceLeft;
            var sourceRightX = source.width - sourceRight;
            var sourceCenterY = sourceTop;
            var sourceBottomY = source.height - sourceBottom;
            var destinationCenterX = destination.x + destinationLeft;
            var destinationRightX = destination.xMax - destinationRight;
            var destinationCenterY = destination.y + destinationTop;
            var destinationBottomY = destination.yMax - destinationBottom;

            DrawAtlasSlice(texture, new Rect(destination.x, destination.y, destinationLeft, destinationTop),
                new Rect(0f, 0f, sourceLeft, sourceTop));
            DrawAtlasSlice(texture, new Rect(destinationCenterX, destination.y, destinationCenterWidth, destinationTop),
                new Rect(sourceCenterX, 0f, sourceCenterWidth, sourceTop));
            DrawAtlasSlice(texture, new Rect(destinationRightX, destination.y, destinationRight, destinationTop),
                new Rect(sourceRightX, 0f, sourceRight, sourceTop));
            DrawAtlasSlice(texture, new Rect(destination.x, destinationCenterY, destinationLeft, destinationCenterHeight),
                new Rect(0f, sourceCenterY, sourceLeft, sourceCenterHeight));
            if (fillCenter)
                DrawAtlasSlice(texture, new Rect(destinationCenterX, destinationCenterY, destinationCenterWidth, destinationCenterHeight),
                    new Rect(sourceCenterX, sourceCenterY, sourceCenterWidth, sourceCenterHeight));
            DrawAtlasSlice(texture, new Rect(destinationRightX, destinationCenterY, destinationRight, destinationCenterHeight),
                new Rect(sourceRightX, sourceCenterY, sourceRight, sourceCenterHeight));
            DrawAtlasSlice(texture, new Rect(destination.x, destinationBottomY, destinationLeft, destinationBottom),
                new Rect(0f, sourceBottomY, sourceLeft, sourceBottom));
            DrawAtlasSlice(texture, new Rect(destinationCenterX, destinationBottomY, destinationCenterWidth, destinationBottom),
                new Rect(sourceCenterX, sourceBottomY, sourceCenterWidth, sourceBottom));
            DrawAtlasSlice(texture, new Rect(destinationRightX, destinationBottomY, destinationRight, destinationBottom),
                new Rect(sourceRightX, sourceBottomY, sourceRight, sourceBottom));
        }

        private static void DrawStandaloneHorizontalThreeSlice(Texture2D texture, Rect destination,
            float sourceLeft, float sourceRight)
        {
            if (texture == null || destination.width <= 0f || destination.height <= 0f) return;
            sourceLeft = Mathf.Clamp(sourceLeft, 0f, texture.width * 0.5f);
            sourceRight = Mathf.Clamp(sourceRight, 0f, texture.width - sourceLeft);
            var destinationLeft = Mathf.Min(sourceLeft, destination.width * 0.5f);
            var destinationRight = Mathf.Min(sourceRight, destination.width - destinationLeft);
            var sourceCenterWidth = texture.width - sourceLeft - sourceRight;
            var destinationCenterWidth = destination.width - destinationLeft - destinationRight;

            DrawAtlasSlice(texture,
                new Rect(destination.x, destination.y, destinationLeft, destination.height),
                new Rect(0f, 0f, sourceLeft, texture.height));
            DrawAtlasSlice(texture,
                new Rect(destination.x + destinationLeft, destination.y,
                    destinationCenterWidth, destination.height),
                new Rect(sourceLeft, 0f, sourceCenterWidth, texture.height));
            DrawAtlasSlice(texture,
                new Rect(destination.xMax - destinationRight, destination.y,
                    destinationRight, destination.height),
                new Rect(texture.width - sourceRight, 0f, sourceRight, texture.height));
        }

        private void DrawPreparationV53TopControls()
        {
            var topIcons = artCatalog.GetExpeditionUiV63Texture("map-a-top-icons-runtime-v6.3");
            var sharedDecor = artCatalog.GetExpeditionUiV62Texture("map-a-decor-runtime-v6.2");
            DrawSharedMapStyleTopFrames(ExpeditionUiV53Layout.Back,
                ExpeditionUiV53Layout.Abandon, ExpeditionUiV53Layout.Round,
                ExpeditionUiV53Layout.Coins, ExpeditionUiV53Layout.Life);
            DrawAtlasSlice(topIcons, ExpeditionUiV53Layout.BackIcon,
                new Rect(8f, 8f, 128f, 128f));
            if (GUI.Button(ExpeditionUiV53Layout.Back, GUIContent.none, GUIStyle.none))
            {
                preparationShopOpen = false;
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }

            DrawAtlasSlice(sharedDecor, ExpeditionUiV53Layout.Compass,
                new Rect(16f, 16f, 96f, 104f));
            DrawSharedMapStyleAbandon(topIcons, ExpeditionUiV53Layout.Abandon);
            DrawSharedMapStyleHud(topIcons, ExpeditionUiV53Layout.Round,
                new Rect(280f, 8f, 128f, 128f), round.ToString());
            DrawSharedMapStyleHud(topIcons, ExpeditionUiV53Layout.Coins,
                new Rect(416f, 8f, 128f, 128f), coins.ToString());
            DrawSharedMapStyleHud(topIcons, ExpeditionUiV53Layout.Life,
                new Rect(552f, 8f, 128f, 128f), health.ToString());
        }

        private void DrawSharedMapStyleAbandon(Texture2D topIcons, Rect area)
        {
            var label = confirmAbandon ? T("run.abandonConfirmShort") : T("run.abandonShort");
            DrawCenteredAtlasIconTextGroup(topIcons, area,
                new Rect(144f, 8f, 128f, 128f), label,
                ArchiveStyle(33, ArchiveColor("F6DAD0"), TextAnchor.MiddleLeft, true),
                new Vector2(32f, 32f), 8f, 14);
            if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) return;
            if (confirmAbandon) AbandonRun();
            else
            {
                confirmAbandon = true;
                statusKey = "status.abandonPrompt";
            }
        }

        private void DrawSharedMapStyleHud(Texture2D topIcons, Rect area,
            Rect iconSource, string value)
        {
            DrawCenteredAtlasIconTextGroup(topIcons, area, iconSource, value,
                ArchiveStyle(language == GameLanguage.English ? 20 : 26,
                    ArchiveColor("F4F0DD"), TextAnchor.MiddleLeft, true),
                new Vector2(34f, 34f), 10f, 14);
        }

        private void DrawSharedMapStyleTopFrames(Rect back, Rect abandon,
            Rect round, Rect coins, Rect life)
        {
            // Keep the 94x94 `back` rect as the interaction target. The ring
            // painted inside the shared atlas cell is 73x80 and sits 4.5 px
            // left of the cell centre, so mirror the accepted map-page visual
            // compensation instead of stretching the cell into the hit rect.
            var backVisual = new Rect(back.x + 0.5f, back.y - 1f,
                back.width + 9f, back.height);
            // The map page keeps the accepted V62 frame. Preparation and
            // upgrade use this final-size clean variant so the old atlas
            // cell's detached lower tail cannot leak below the round button.
            DrawExpeditionUiV53Texture(backVisual,
                "shared-back-frame-clean-v5.3.6", ScaleMode.StretchToFill);
            DrawSharedExpeditionTexture(abandon, ExpeditionUiSharedFrames.AbandonFrame,
                ScaleMode.ScaleToFit);
            DrawSharedExpeditionTexture(round, ExpeditionUiSharedFrames.RoundHud);
            DrawSharedExpeditionTexture(coins, ExpeditionUiSharedFrames.CoinsHud);
            DrawSharedExpeditionTexture(life, ExpeditionUiSharedFrames.LifeHud);
        }

        private void DrawV52ProfessionTag(Rect area, string professionId)
        {
            DrawV46ProfessionEmblem(area, professionId);
        }

        private void DrawV532FactionIcon(Rect area, string factionId)
        {
            var factionKey = ExpeditionArchivePresentation.FactionPresentationKey(factionId);
            var source = factionKey == "steel" ? new Rect(65f, 25f, 629f, 628f) :
                factionKey == "wild" ? new Rect(758f, 21f, 648f, 625f) :
                new Rect(1455f, 27f, 655f, 626f);
            var atlas = artCatalog.GetExpeditionUiV53Texture("preparation-faction-icons-square-v5.3.2");
            DrawAtlasSliceAspectFit(atlas, area, source);
        }

        private void DrawV53CompactStats(Rect offerArea, CombatStatBlock stats)
        {
            var atlas = artCatalog.GetExpeditionUiV53Texture("preparation-command-board-stat-icons-v5.3.2");
            var contentLeft = offerArea.x + 221f;
            var centerY = offerArea.y + 165f;
            DrawV53StatIconValue(atlas, new Vector2(contentLeft + 38f, centerY),
                new Rect(78f, 191f, 435f, 393f), stats.MaxHealth.ToString());
            DrawV53StatIconValue(atlas, new Vector2(contentLeft + 120f, centerY),
                new Rect(608f, 158f, 442f, 425f), stats.AttackDamage.ToString());
            DrawV53StatIconValue(atlas, new Vector2(contentLeft + 202f, centerY),
                new Rect(1129f, 136f, 415f, 455f), stats.Armor.ToString());
            DrawV53StatIconValue(atlas, new Vector2(contentLeft + 284f, centerY),
                new Rect(1647f, 128f, 453f, 464f), ((int)stats.SpeedTier + 1).ToString());
        }

        private static void DrawPreparationOfferCharacterSprite(Rect area,
            CharacterArtDefinition definition, Sprite sprite)
        {
            if (definition == null || sprite == null || sprite.texture == null) return;
            var bounds = definition.ContentBoundsTopOrigin;
            var textureAspect = sprite.texture.width / (float)Mathf.Max(1, sprite.texture.height);
            var targetVisibleHeight = area.height * 0.9f;
            var canvasHeight = targetVisibleHeight / Mathf.Max(0.01f, bounds.height);
            var canvasWidth = canvasHeight * textureAspect;
            var visibleCenterX = (bounds.x + bounds.width * 0.5f) * canvasWidth;
            var visibleCenterY = (bounds.y + bounds.height * 0.5f) * canvasHeight;
            GUI.BeginGroup(area);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(area.width * 0.5f - visibleCenterX,
                    area.height * 0.5f - visibleCenterY, canvasWidth, canvasHeight),
                sprite.texture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            GUI.EndGroup();
        }

        private void DrawV53StatIconValue(Texture2D atlas, Vector2 center, Rect source, string value)
        {
            const float iconSize = 32f;
            var area = new Rect(center.x - 38f, center.y - 21f, 76f, 42f);
            DrawCenteredAtlasIconTextGroup(atlas, area, source, value,
                ArchiveStyle(22, ArchiveInk(), TextAnchor.MiddleLeft, true),
                new Vector2(iconSize, iconSize), 4f, 15);
        }

        private void DrawV52IconControl(Rect area, string iconResourceId, float iconSize)
        {
            DrawExpeditionUiV46Texture(new Rect(area.center.x - iconSize * 0.5f,
                area.center.y - iconSize * 0.5f, iconSize, iconSize),
                iconResourceId, ScaleMode.ScaleToFit);
        }

        private void DrawV532UtilityIconControl(Rect area, string iconResourceId,
            Rect visibleSource, float iconSize)
        {
            var texture = artCatalog.GetExpeditionUiV46Texture(iconResourceId);
            if (texture == null) return;
            var iconArea = new Rect(area.center.x - iconSize * 0.5f,
                area.center.y - iconSize * 0.5f, iconSize, iconSize);
            DrawAtlasSliceAspectFit(texture, iconArea, visibleSource);
        }

        private void DrawV52PrimaryButton(Rect area, bool enabled)
        {
            var atlas = artCatalog.GetExpeditionUiV52Texture("preparation-command-board-controls-v5.2");
            var pressed = enabled && Event.current != null && Event.current.type == EventType.MouseDown &&
                area.Contains(Event.current.mousePosition);
            var source = !enabled ? new Rect(1055f, 142f, 451f, 171f) :
                pressed ? new Rect(541f, 142f, 463f, 171f) : new Rect(38f, 142f, 471f, 171f);
            // Source and destination share the same aspect ratio. Drawing the complete
            // slice avoids fractional nine-slice seams on the middle offer row.
            DrawAtlasSliceAspectCover(atlas, area, source);
        }

        private void DrawV52ControlFrame(Rect area, Rect source, Vector4 borders)
        {
            var atlas = artCatalog.GetExpeditionUiV52Texture("preparation-command-board-controls-v5.2");
            DrawAtlasNineSlice(atlas, area, source, borders);
        }

        private void DrawExpeditionUiV52Texture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionUiV52Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private void DrawExpeditionUiV53Texture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionUiV53Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private static void DrawAtlasSlice(Texture2D atlas, Rect destination, Rect sourceTopLeft)
        {
            if (atlas == null || destination.width <= 0f || destination.height <= 0f) return;
            var uv = new Rect(sourceTopLeft.x / atlas.width,
                1f - (sourceTopLeft.y + sourceTopLeft.height) / atlas.height,
                sourceTopLeft.width / atlas.width, sourceTopLeft.height / atlas.height);
            GUI.color = Color.white;
            GUI.DrawTextureWithTexCoords(destination, atlas, uv, true);
        }

        private static void DrawAtlasSliceAspectCover(Texture2D atlas, Rect destination,
            Rect sourceTopLeft)
        {
            if (atlas == null || destination.width <= 0f || destination.height <= 0f) return;
            var sourceAspect = sourceTopLeft.width / sourceTopLeft.height;
            var destinationAspect = destination.width / destination.height;
            var crop = sourceTopLeft;
            if (sourceAspect < destinationAspect)
            {
                var croppedHeight = sourceTopLeft.width / destinationAspect;
                crop.y += (sourceTopLeft.height - croppedHeight) * 0.5f;
                crop.height = croppedHeight;
            }
            else if (sourceAspect > destinationAspect)
            {
                var croppedWidth = sourceTopLeft.height * destinationAspect;
                crop.x += (sourceTopLeft.width - croppedWidth) * 0.5f;
                crop.width = croppedWidth;
            }
            DrawAtlasSlice(atlas, destination, crop);
        }

        private static void DrawAtlasSliceAspectFit(Texture2D atlas, Rect destination,
            Rect sourceTopLeft)
        {
            if (atlas == null || destination.width <= 0f || destination.height <= 0f) return;
            var sourceAspect = sourceTopLeft.width / sourceTopLeft.height;
            var destinationAspect = destination.width / destination.height;
            var drawArea = destination;
            if (sourceAspect > destinationAspect)
            {
                drawArea.height = destination.width / sourceAspect;
                drawArea.y = destination.center.y - drawArea.height * 0.5f;
            }
            else if (sourceAspect < destinationAspect)
            {
                drawArea.width = destination.height * sourceAspect;
                drawArea.x = destination.center.x - drawArea.width * 0.5f;
            }
            DrawAtlasSlice(atlas, drawArea, sourceTopLeft);
        }

        private static void DrawAtlasNineSlice(Texture2D atlas, Rect destination,
            Rect sourceTopLeft, Vector4 borderLeftTopRightBottom)
        {
            if (atlas == null) return;
            var scale = Mathf.Min(destination.width / sourceTopLeft.width,
                destination.height / sourceTopLeft.height);
            var sourceLeft = borderLeftTopRightBottom.x;
            var sourceTop = borderLeftTopRightBottom.y;
            var sourceRight = borderLeftTopRightBottom.z;
            var sourceBottom = borderLeftTopRightBottom.w;
            var destinationLeft = sourceLeft * scale;
            var destinationTop = sourceTop * scale;
            var destinationRight = sourceRight * scale;
            var destinationBottom = sourceBottom * scale;

            var sourceCenterWidth = sourceTopLeft.width - sourceLeft - sourceRight;
            var sourceCenterHeight = sourceTopLeft.height - sourceTop - sourceBottom;
            var destinationCenterWidth = destination.width - destinationLeft - destinationRight;
            var destinationCenterHeight = destination.height - destinationTop - destinationBottom;
            var sourceCenterX = sourceTopLeft.x + sourceLeft;
            var sourceRightX = sourceTopLeft.xMax - sourceRight;
            var sourceCenterY = sourceTopLeft.y + sourceTop;
            var sourceBottomY = sourceTopLeft.yMax - sourceBottom;
            var destinationCenterX = destination.x + destinationLeft;
            var destinationRightX = destination.xMax - destinationRight;
            var destinationCenterY = destination.y + destinationTop;
            var destinationBottomY = destination.yMax - destinationBottom;

            DrawAtlasSlice(atlas, new Rect(destination.x, destination.y, destinationLeft, destinationTop),
                new Rect(sourceTopLeft.x, sourceTopLeft.y, sourceLeft, sourceTop));
            DrawAtlasSlice(atlas, new Rect(destinationCenterX, destination.y, destinationCenterWidth, destinationTop),
                new Rect(sourceCenterX, sourceTopLeft.y, sourceCenterWidth, sourceTop));
            DrawAtlasSlice(atlas, new Rect(destinationRightX, destination.y, destinationRight, destinationTop),
                new Rect(sourceRightX, sourceTopLeft.y, sourceRight, sourceTop));
            DrawAtlasSlice(atlas, new Rect(destination.x, destinationCenterY, destinationLeft, destinationCenterHeight),
                new Rect(sourceTopLeft.x, sourceCenterY, sourceLeft, sourceCenterHeight));
            DrawAtlasSlice(atlas, new Rect(destinationCenterX, destinationCenterY, destinationCenterWidth, destinationCenterHeight),
                new Rect(sourceCenterX, sourceCenterY, sourceCenterWidth, sourceCenterHeight));
            DrawAtlasSlice(atlas, new Rect(destinationRightX, destinationCenterY, destinationRight, destinationCenterHeight),
                new Rect(sourceRightX, sourceCenterY, sourceRight, sourceCenterHeight));
            DrawAtlasSlice(atlas, new Rect(destination.x, destinationBottomY, destinationLeft, destinationBottom),
                new Rect(sourceTopLeft.x, sourceBottomY, sourceLeft, sourceBottom));
            DrawAtlasSlice(atlas, new Rect(destinationCenterX, destinationBottomY, destinationCenterWidth, destinationBottom),
                new Rect(sourceCenterX, sourceBottomY, sourceCenterWidth, sourceBottom));
            DrawAtlasSlice(atlas, new Rect(destinationRightX, destinationBottomY, destinationRight, destinationBottom),
                new Rect(sourceRightX, sourceBottomY, sourceRight, sourceBottom));
        }

        private void DrawPreparationShopOffer(Rect area, int offerIndex)
        {
            var nameKey = shopOffers[offerIndex];
            if (string.IsNullOrEmpty(nameKey))
            {
                GUI.Label(area, T("shop.sold"), ArchiveStyle(26, ArchiveMutedInk(), TextAnchor.MiddleCenter, true));
                return;
            }
            var level = ShopOfferLevel(offerIndex);
            var cost = UnitCost(nameKey, level);
            var power = UnitBasePower(nameKey);
            var definition = characterCatalog.Get(nameKey);
            var stats = definition.CalculateCombatStats(power, level);
            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(nameKey, out artDefinition, out sprite))
            {
                var sway = Mathf.Sin(Time.unscaledTime * 1.8f + offerIndex * 1.7f) * 3f;
                var characterArea = OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferCharacterArt);
                characterArea.x += sway;
                DrawFullCharacterSprite(characterArea,
                    artDefinition, sprite, 1f);
            }
            GUI.Label(OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferName),
                T(nameKey) + "  Lv." + level,
                ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleLeft, true));
            DrawV46ProfessionEmblem(OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferProfessionIcon),
                definition.ProfessionId);
            DrawV46IdentityTag(OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferFaction),
                definition.FactionId);
            GUI.Label(OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferStats),
                PrimaryStatsText(stats) + "  ·  " + SecondaryStatsText(stats),
                ArchiveStyle(19, ArchiveInk(), TextAnchor.MiddleLeft));
            var payableCost = expeditionEconomy.QuoteRecruitmentPrice(cost);
            var priceIconArea = OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferPriceIcon);
            var priceTextArea = OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferPriceText);
            var priceGroupArea = Rect.MinMaxRect(priceIconArea.xMin,
                Mathf.Min(priceIconArea.yMin, priceTextArea.yMin), priceTextArea.xMax,
                Mathf.Max(priceIconArea.yMax, priceTextArea.yMax));
            DrawV46CenteredIconLabel(priceGroupArea, "utility-coin-v4.6",
                T("price") + " " + payableCost, 19, ArchiveInk(),
                27f, 6f, false, 13);
            var canBuy = coins >= payableCost && runRoster.CanPurchase(
                new RunUnitState(nameKey, power, level, payableCost));
            var recruitArea = OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferRecruit);
            GUI.Label(OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferRecruitText), T("shop.buy"),
                ArchiveStyle(26, canBuy ? ArchiveColor("F3E7C8") : ArchiveColor("829099"),
                    TextAnchor.MiddleCenter, true));
            GUI.enabled = canBuy;
            // Preserve the approved map-like interaction: the complete offer is the
            // recruitment hit target, while the delivered button frame stays visual.
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
                Recruit(offerIndex, nameKey, cost, power, level);
            GUI.enabled = true;
        }

        private Matrix4x4 PushExpeditionUiV46Canvas(float referenceHeight)
        {
            var previous = GUI.matrix;
            var scale = Mathf.Min(DesignWidth / ExpeditionUiV46Layout.ReferenceWidth,
                layoutContentHeight / referenceHeight);
            var offsetX = (DesignWidth - ExpeditionUiV46Layout.ReferenceWidth * scale) * 0.5f;
            var offsetY = (layoutContentHeight - referenceHeight * scale) * 0.5f;
            GUI.matrix = previous * Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            return previous;
        }

        private Matrix4x4 PushExpeditionUiV46FullCanvas(float referenceHeight)
        {
            var previous = GUI.matrix;
            var fit = AdaptivePortraitLayoutMath.CalculateFullCanvasFit(
                DesignWidth, layoutCanvasHeight, ExpeditionUiV46Layout.ReferenceWidth, referenceHeight);
            var root = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(layoutScale, layoutScale, 1f));
            GUI.matrix = root * Matrix4x4.TRS(new Vector3(fit.OffsetX, fit.OffsetY, 0f),
                Quaternion.identity, new Vector3(fit.Scale, fit.Scale, 1f));
            return previous;
        }

        private void DrawPreparationV46TopControls()
        {
            var backIconArea = new Rect(
                ExpeditionUiV46Layout.PreparationBack.center.x - 25f,
                ExpeditionUiV46Layout.PreparationBack.center.y - 5f,
                50f, 50f);
            DrawExpeditionUiV46Texture(backIconArea,
                "utility-back-v4.6", ScaleMode.ScaleToFit);
            if (GUI.Button(ExpeditionUiV46Layout.PreparationBack, GUIContent.none, GUIStyle.none))
            {
                preparationShopOpen = false;
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }

            DrawV46AbandonControl(ExpeditionUiV46Layout.PreparationAbandon, true);
            DrawV46TopStat(ExpeditionUiV46Layout.PreparationRound,
                "utility-round-hourglass-v4.6", round.ToString());
            DrawV46TopStat(ExpeditionUiV46Layout.PreparationCoins,
                "utility-coin-v4.6", coins.ToString());
            DrawV46TopStat(ExpeditionUiV46Layout.PreparationLife,
                "utility-life-heart-v4.6", health.ToString());
        }

        private void DrawUpgradeV46TopControls()
        {
            DrawV46UtilityControl(ExpeditionUiV46Layout.UpgradeBack,
                "utility-back-v4.6", T("home.back"), 19, ArchiveColor("F3E7C8"));
            if (GUI.Button(ExpeditionUiV46Layout.UpgradeBack, GUIContent.none, GUIStyle.none))
            {
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }
            DrawV46AbandonControl(ExpeditionUiV46Layout.UpgradeAbandon);
            DrawV46TopStat(ExpeditionUiV46Layout.UpgradeRound,
                "utility-round-hourglass-v4.6", round.ToString());
            DrawV46TopStat(ExpeditionUiV46Layout.UpgradeCoins,
                "utility-coin-v4.6", coins.ToString());
            DrawV46TopStat(ExpeditionUiV46Layout.UpgradeLife,
                "utility-life-heart-v4.6", health.ToString());
        }

        private void DrawV46AbandonControl(Rect area, bool compactWithIcon = false)
        {
            var label = confirmAbandon ? T("run.abandonConfirmShort") :
                T(compactWithIcon ? "run.abandonShort" : "run.abandonExpeditionShort");
            var fontSize = language == GameLanguage.English && !confirmAbandon ? 16 : 26;
            if (compactWithIcon)
            {
                DrawV46CenteredIconLabel(area, "utility-abandon-v4.6", label, fontSize,
                    ArchiveColor("F6DAD0"), 24f, 6f, true);
            }
            else
            {
                GUI.Label(area, label,
                    ArchiveStyle(fontSize, ArchiveColor("F6DAD0"), TextAnchor.MiddleCenter, true));
            }
            if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) return;
            if (confirmAbandon) AbandonRun();
            else
            {
                confirmAbandon = true;
                statusKey = "status.abandonPrompt";
            }
        }

        private void DrawV46TopStat(Rect area, string iconResourceId, string value)
        {
            DrawV46CenteredIconLabel(area, iconResourceId, value,
                19, ArchiveColor("EDF3ED"), 34f, 10f, false);
        }

        private void DrawV46CenteredIconLabel(Rect area, string iconResourceId, string label,
            int fontSize, Color textColor, float iconSize, float gap, bool emphasized,
            int minimumFontSize = 12)
        {
            var maximumTextWidth = Mathf.Max(0f, area.width - iconSize - gap - 12f);
            float textWidth;
            var style = FitIconTextStyle(
                ArchiveStyle(fontSize, textColor, TextAnchor.MiddleLeft, emphasized),
                label, maximumTextWidth, minimumFontSize, out textWidth);
            var groupWidth = iconSize + gap + textWidth;
            var startX = area.center.x - groupWidth * 0.5f;
            DrawExpeditionUiV46Texture(
                new Rect(startX, area.center.y - iconSize * 0.5f, iconSize, iconSize),
                iconResourceId, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(startX + iconSize + gap, area.y, textWidth, area.height),
                label, style);
        }

        private static GUIStyle FitIconTextStyle(
            GUIStyle sourceStyle, string label, float maximumTextWidth,
            int minimumFontSize, out float textWidth)
        {
            var style = new GUIStyle(sourceStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                clipping = TextClipping.Clip
            };
            var content = new GUIContent(label ?? string.Empty);
            var floor = Mathf.Max(8, minimumFontSize);
            while (style.fontSize > floor && style.CalcSize(content).x > maximumTextWidth)
                style.fontSize--;
            textWidth = Mathf.Min(maximumTextWidth,
                Mathf.Ceil(style.CalcSize(content).x) + 2f);
            return style;
        }

        private void DrawCenteredAtlasIconTextGroup(
            Texture2D atlas, Rect area, Rect iconSource, string label,
            GUIStyle sourceStyle, Vector2 iconSize, float gap, int minimumFontSize)
        {
            var maximumTextWidth = Mathf.Max(0f, area.width - iconSize.x - gap - 12f);
            float textWidth;
            var style = FitIconTextStyle(sourceStyle, label,
                maximumTextWidth, minimumFontSize, out textWidth);
            var groupWidth = iconSize.x + gap + textWidth;
            var startX = area.center.x - groupWidth * 0.5f;
            DrawAtlasSlice(atlas,
                new Rect(startX, area.center.y - iconSize.y * 0.5f, iconSize.x, iconSize.y),
                iconSource);
            GUI.Label(new Rect(startX + iconSize.x + gap, area.y, textWidth, area.height),
                label, style);
        }

        private void DrawV46UtilityControl(Rect area, string iconResourceId, string label,
            int fontSize, Color textColor)
        {
            var iconSize = Mathf.Min(area.height - 16f, 32f);
            DrawV46CenteredIconLabel(area, iconResourceId, label,
                fontSize, textColor, iconSize, 7f, fontSize >= 26);
        }

        private void DrawV46ProfessionEmblem(Rect area, string identityId)
        {
            var resourceId = identityId == "profession.guardian" ? "profession-tag-guardian-v4.6" :
                identityId == "profession.support" ? "profession-tag-support-v4.6" :
                "profession-tag-output-v4.6";
            DrawExpeditionUiV46Texture(area, resourceId, ScaleMode.ScaleToFit);
        }

        private void DrawV46IdentityTag(Rect area, string identityId)
        {
            var factionKey = ExpeditionArchivePresentation.FactionPresentationKey(identityId);
            var resourceId = factionKey == "steel" ? "faction-tag-lantern-normal-v4.6" :
                factionKey == "wild" ? "faction-tag-moss-normal-v4.6" :
                "faction-tag-riftrock-normal-v4.6";
            DrawExpeditionUiV46Texture(area, resourceId, ScaleMode.ScaleToFit);
            GUI.Label(new Rect(area.x + area.width * 0.25f, area.y + 1f,
                    area.width * 0.72f, area.height - 2f),
                T(identityId), ArchiveStyle(19, ArchiveColor("F3E7C8"), TextAnchor.MiddleCenter));
        }

        private void DrawExpeditionUiV46Texture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionUiV46Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private static Rect OffsetRect(Rect parent, float x, float y, float width, float height)
        {
            return new Rect(parent.x + x, parent.y + y, width, height);
        }

        private static Rect OffsetRect(Rect parent, Rect relative)
        {
            return new Rect(parent.x + relative.x, parent.y + relative.y,
                relative.width, relative.height);
        }

        private static Color ArchiveInk()
        {
            return ArchiveColor("173542");
        }

        private static Color ArchiveMutedInk()
        {
            return ArchiveColor("526C72");
        }

        private static Color ArchiveColor(string hex)
        {
            Color color;
            return ColorUtility.TryParseHtmlString("#" + hex, out color) ? color : Color.white;
        }

        private void DrawPreparationUnitDetail()
        {
            RunUnitState runUnit = null;
            UnitData unit;
            string definitionId;
            if (preparationDetailIsMonster)
            {
                if (preparationDetailEnemyIndex < 0 || preparationDetailEnemyIndex >= enemyUnits.Count)
                {
                    ClosePreparationUnitDetail();
                    return;
                }
                unit = enemyUnits[preparationDetailEnemyIndex];
                definitionId = unit.NameKey;
            }
            else
            {
                if (preparationDetailSlot < 0 || preparationDetailSlot >= runRoster.TotalCapacity ||
                    runRoster.Get(preparationDetailSlot) == null)
                {
                    ClosePreparationUnitDetail();
                    return;
                }
                runUnit = runRoster.Get(preparationDetailSlot);
                unit = CreateRunUnitEffectiveView(runUnit);
                definitionId = runUnit.DefinitionId;
            }

            // DetailOverlayRoot: all three layers cover the physical canvas and never
            // inherit the foreground safe-area translation.
            var fullCanvas = new Rect(0f, 0f, DesignWidth, layoutCanvasHeight);
            var blurredBattlefield = artCatalog.GetExpeditionUiV52Texture(
                "battlefield-signal-yard-preparation-blur-v5.2");
            if (blurredBattlefield != null)
            {
                GUI.color = new Color(1f, 1f, 1f, 0.78f);
                GUI.DrawTexture(fullCanvas, blurredBattlefield, ScaleMode.ScaleAndCrop, true);
                GUI.color = Color.white;
            }
            GUI.color = ArchiveColor("0416238E");
            GUI.DrawTexture(fullCanvas, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var modalData = DetailTideWindowLayout.CalculateModal(
                DesignWidth, layoutCanvasHeight, layoutSafeTop, layoutSafeBottom);
            var panel = new Rect(modalData.X, modalData.Y, modalData.Width, modalData.Height);
            ClosePreparationOverlayWhenTappedOutside(panel, ClosePreparationUnitDetail);

            var shell = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailTideWindowShell);
            if (shell != null) GUI.DrawTexture(panel, shell, ScaleMode.StretchToFill, true);

            var ivory = ArchiveColor("F6E8C8");
            GUI.Label(DetailLocalRect(panel, 190f, 70f, 500f, 72f), T(definitionId),
                DetailTextStyle(46, ivory, TextAnchor.MiddleCenter, false, FontStyle.Bold));

            CharacterDefinition characterDefinition = null;
            try { characterDefinition = characterCatalog.Get(definitionId); }
            catch (KeyNotFoundException) { }
            DrawDetailTags(panel, definitionId, characterDefinition, preparationDetailIsMonster, ivory);

            var qualityId = ResolveUnitLevelBadgeQualityId(definitionId, !preparationDetailIsMonster);
            var badge = artCatalog.GetUnitLevelBadgeTexture(qualityId, unit.RunLevel);
            if (badge != null)
                GUI.DrawTexture(DetailLocalRect(panel, 290f, 230f, 300f, 88f), badge,
                    ScaleMode.ScaleToFit, true);

            if (preparationDetailIsMonster)
            {
                DrawDetailMetadataField(panel, 202f, DetailMonsterClassification(definitionId), ivory);
                DrawDetailMetadataField(panel, 448f, T("detail.notRecruitable"), ivory);
            }
            else
            {
                DrawDetailMetadataField(panel, 202f,
                    T("detail.recruitCost") + runUnit.AcquisitionBasis, ivory);
                DrawDetailMetadataField(panel, 448f,
                    T("detail.recycleValue") + (runUnit.AcquisitionBasis / 2), ivory);
            }

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(definitionId, qualityId, out artDefinition, out sprite))
                DrawDetailCharacterSprite(DetailLocalRect(panel, 45f, 390f, 500f, 580f),
                    artDefinition, sprite, preparationDetailIsMonster ? 0.94f : 0.90f);

            DrawDetailStat(panel, new Rect(574f, 393f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatHealth, T("stat.health"), unit.MaxHealth.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 549f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatAttack, T("stat.attack"), unit.AttackDamage.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 705f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatArmor, T("stat.armor"), unit.Armor.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 861f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatSpeed, T("detail.attackSpeed"),
                DetailTideWindowLayout.FormatAttackSpeed(unit.SpeedTier), ivory);

            var quoteStyle = DetailTextStyle(29, ivory, TextAnchor.MiddleCenter, true, FontStyle.Normal);
            GUI.Label(DetailLocalRect(panel, 90f, 1078f, 700f, 132f),
                DetailQuote(definitionId, qualityId), quoteStyle);

            var closeTop = DetailLocalRect(panel, 788f, 55f, 48f, 48f);
            var closeTexture = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailCloseOrnament);
            if (closeTexture != null) GUI.DrawTexture(closeTop, closeTexture, ScaleMode.ScaleToFit, true);
            var closeBottom = DetailLocalRect(panel, 307f, 1261f, 268f, 74f);
            GUI.Label(closeBottom, T("close"), DetailTextStyle(29, ivory,
                TextAnchor.MiddleCenter, false, FontStyle.Normal));
            if (GUI.Button(closeTop, GUIContent.none, GUIStyle.none) ||
                GUI.Button(closeBottom, GUIContent.none, GUIStyle.none))
                ClosePreparationUnitDetail();
        }

        private void DrawDetailMetadataField(Rect panel, float localX, string text, Color textColor)
        {
            var area = DetailLocalRect(panel, localX, 322f, 230f, 58f);
            var underlineArea = DetailLocalRect(panel, localX, 362f, 230f, 18f);
            var underline = artCatalog.GetDetailTideWindowTexture(
                PrototypeArtResourceIds.DetailMetadataUnderline);
            if (underline != null)
            {
                var previousColor = GUI.color;
                GUI.color = Color.white;
                GUI.DrawTexture(underlineArea, underline, ScaleMode.ScaleToFit, true);
                GUI.color = previousColor;
            }

            var style = DetailTextStyle(22, textColor, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            while (style.fontSize > 18 && style.CalcSize(new GUIContent(text)).x > area.width - 20f)
                style.fontSize--;
            GUI.Label(area, text, style);
        }

        private void ClosePreparationUnitDetail()
        {
            preparationUnitDetailOpen = false;
            preparationDetailSlot = -1;
            preparationDetailEnemyIndex = -1;
            preparationDetailIsMonster = false;
        }

        private void DrawDetailTags(Rect panel, string definitionId, CharacterDefinition definition,
            bool isMonster, Color textColor)
        {
            var labels = new List<string>();
            var resources = new List<string>();
            if (isMonster)
            {
                labels.Add(DetailMonsterTier(definitionId));
                resources.Add(PrototypeArtResourceIds.DetailTagPillRed);
                labels.Add(DetailProfessionLabel(definition));
                resources.Add(PrototypeArtResourceIds.DetailTagPillPurple);
                labels.Add(T("faction.prototype.enemy"));
                resources.Add(PrototypeArtResourceIds.DetailTagPillTeal);
            }
            else
            {
                labels.Add(definition == null ? T("detail.unknownTag") : T(definition.FactionId));
                resources.Add(PrototypeArtResourceIds.DetailTagPillTeal);
                labels.Add(DetailProfessionLabel(definition));
                resources.Add(PrototypeArtResourceIds.DetailTagPillPurple);
            }

            var startX = labels.Count == 3 ? 141f : 243f;
            for (var index = 0; index < labels.Count; index++)
            {
                var area = DetailLocalRect(panel, startX + index * 204f, 146f, 190f, 76f);
                var texture = artCatalog.GetDetailTideWindowTexture(resources[index]);
                if (texture != null) GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
                var style = DetailTextStyle(29, textColor, TextAnchor.MiddleCenter, false, FontStyle.Normal);
                var label = labels[index];
                if (style.CalcSize(new GUIContent(label)).x > 150f)
                    style.fontSize = 24;
                GUI.Label(area, label, style);
            }
        }

        private string DetailProfessionLabel(CharacterDefinition definition)
        {
            if (definition == null) return T("detail.unknownTag");
            var shortKey = definition.DetailRoleId + ".short";
            var value = T(shortKey);
            return value == shortKey ? T(definition.DetailRoleId) : value;
        }

        private string DetailMonsterTier(string definitionId)
        {
            if (definitionId.StartsWith("enemy.b", StringComparison.Ordinal)) return T("detail.tag.boss");
            if (definitionId.StartsWith("enemy.e", StringComparison.Ordinal)) return T("detail.tag.elite");
            return T("detail.tag.monster");
        }

        private string DetailMonsterClassification(string definitionId)
        {
            if (definitionId.StartsWith("enemy.b", StringComparison.Ordinal)) return T("detail.classification.boss");
            if (definitionId.StartsWith("enemy.e", StringComparison.Ordinal)) return T("detail.classification.elite");
            return T("detail.classification.normal");
        }

        private string DetailQuote(string definitionId, string qualityId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return T(preparationDetailIsMonster ? "detail.quote.missing.monster" : "detail.quote.missing.character");

            var key = "detail.quote." + definitionId;
            if (!preparationDetailIsMonster &&
                (qualityId == "epic" || qualityId == "legendary"))
                key += ".hero";

            // The first-chapter roster has a complete planner-authored slogan table.
            // Never disguise a missing mapping with the former shared world-building
            // sentence: a visible pending marker makes regressions testable.
            var localized = T(key);
            return localized == key
                ? T(preparationDetailIsMonster ? "detail.quote.missing.monster" : "detail.quote.missing.character")
                : localized;
        }

        private void DrawDetailStat(Rect panel, Rect localFrame, string iconResourceId,
            string label, string value, Color textColor)
        {
            var frame = DetailLocalRect(panel, localFrame.x, localFrame.y, localFrame.width, localFrame.height);
            var labelStyle = DetailTextStyle(24, textColor, TextAnchor.MiddleLeft, false, FontStyle.Normal);
            var valueStyle = DetailTextStyle(29, textColor, TextAnchor.MiddleLeft, false, FontStyle.Normal);
            var textWidth = Mathf.Max(labelStyle.CalcSize(new GUIContent(label)).x,
                valueStyle.CalcSize(new GUIContent(value)).x);
            var groupWidth = 56f + 18f + textWidth;
            var groupX = frame.center.x - groupWidth * 0.5f;
            var iconArea = new Rect(groupX, frame.center.y - 28f, 56f, 56f);
            var icon = artCatalog.GetDetailTideWindowTexture(iconResourceId);
            if (icon != null) GUI.DrawTexture(iconArea, icon, ScaleMode.ScaleToFit, true);
            var textX = groupX + 74f;
            GUI.Label(new Rect(textX, frame.center.y - 38f, textWidth, 32f), label, labelStyle);
            GUI.Label(new Rect(textX, frame.center.y - 4f, textWidth, 42f), value, valueStyle);
        }

        private static Rect DetailLocalRect(Rect panel, float x, float y, float width, float height)
        {
            return new Rect(panel.x + x, panel.y + y, width, height);
        }

        private GUIStyle DetailTextStyle(int size, Color color, TextAnchor alignment,
            bool wordWrap, FontStyle fontStyle)
        {
            var style = Style(size, color, alignment, fontStyle);
            style.wordWrap = wordWrap;
            style.clipping = TextClipping.Clip;
            return style;
        }

        private static void DrawDetailCharacterSprite(
            Rect area, CharacterArtDefinition definition, Sprite sprite, float occupancy)
        {
            if (definition == null || sprite == null || sprite.texture == null) return;
            var bounds = definition.ContentBoundsTopOrigin;
            var textureAspect = sprite.texture.width / (float)Mathf.Max(1, sprite.texture.height);
            var safeOccupancy = Mathf.Clamp(occupancy, 0.1f, 1f);
            var canvasHeight = Mathf.Min(
                area.width * safeOccupancy / Mathf.Max(0.01f, textureAspect * bounds.width),
                area.height * safeOccupancy / Mathf.Max(0.01f, bounds.height));
            var canvasWidth = canvasHeight * textureAspect;
            var visibleCenterX = (bounds.x + bounds.width * 0.5f) * canvasWidth;
            var visibleBottomY = (bounds.y + bounds.height) * canvasHeight;
            GUI.BeginGroup(area);
            GUI.DrawTexture(new Rect(
                    area.width * 0.5f - visibleCenterX,
                    area.height - visibleBottomY,
                    canvasWidth,
                    canvasHeight),
                sprite.texture, ScaleMode.StretchToFill, true);
            GUI.EndGroup();
        }

        private static void ClosePreparationOverlayWhenTappedOutside(Rect panel, Action close)
        {
            var currentEvent = Event.current;
            if (currentEvent == null || currentEvent.type != EventType.MouseUp || currentEvent.button != 0 ||
                panel.Contains(currentEvent.mousePosition)) return;
            close();
            currentEvent.Use();
        }

        private static void DrawFullCharacterSprite(
            Rect area, CharacterArtDefinition definition, Sprite sprite, float opacity)
        {
            if (definition == null || sprite == null || sprite.texture == null) return;
            var bounds = definition.ContentBoundsTopOrigin;
            var textureAspect = sprite.texture.width / (float)Mathf.Max(1, sprite.texture.height);
            var canvasHeight = Mathf.Min(
                area.width / Mathf.Max(0.01f, textureAspect * bounds.width),
                area.height / Mathf.Max(0.01f, bounds.height));
            var canvasWidth = canvasHeight * textureAspect;
            var visibleCenterX = (bounds.x + bounds.width * 0.5f) * canvasWidth;
            var visibleCenterY = (bounds.y + bounds.height * 0.5f) * canvasHeight;
            GUI.BeginGroup(area);
            GUI.color = WithOpacity(Color.white, opacity);
            GUI.DrawTexture(new Rect(
                    area.width * 0.5f - visibleCenterX,
                    area.height * 0.5f - visibleCenterY,
                    canvasWidth,
                    canvasHeight),
                sprite.texture, ScaleMode.StretchToFill, true);
            GUI.color = Color.white;
            GUI.EndGroup();
        }

        private static float GetFullCharacterVisibleTop(
            Rect area, CharacterArtDefinition definition, Sprite sprite)
        {
            if (definition == null || sprite == null || sprite.texture == null) return area.y;
            var bounds = definition.ContentBoundsTopOrigin;
            var textureAspect = sprite.texture.width / (float)Mathf.Max(1, sprite.texture.height);
            var canvasHeight = Mathf.Min(
                area.width / Mathf.Max(0.01f, textureAspect * bounds.width),
                area.height / Mathf.Max(0.01f, bounds.height));
            var visibleHeight = bounds.height * canvasHeight;
            return area.center.y - visibleHeight * 0.5f;
        }

        private void DrawFormation(string label, List<UnitData> units, float y, Color panelColor)
        {
            var panel = new Rect(40f, y, 1000f, 350f);
            if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawPanel(panel, false);
            else
            {
                GUI.color = panelColor;
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(70f, y + 12f, 600f, 40f), label,
                Style(24, PreparationTextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal));

            for (var i = 0; i < 6; i++)
            {
                var column = i % 3;
                var row = i / 3;
                var cell = new Rect(70f + column * 318f, y + 58f + row * 132f, 282f, 112f);
                if (i < units.Count)
                {
                    DrawPreparationUnitCard(cell, units[i], false);
                }
                else
                {
                    if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawListRow(cell, false);
                    else
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.07f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                    GUI.Label(cell, T("empty"), Style(20,
                        IsExpeditionUiTrialPage ? PreparationTextMuted : new Color(1f, 1f, 1f, 0.32f),
                        TextAnchor.MiddleCenter));
                }
            }
        }

        private void DrawUnitCard(Rect area, UnitData unit, BattleUnitPresentationState combatUnit = null, bool showRunLevel = false)
        {
            var cardColor = combatUnit != null && !combatUnit.IsAlive ? unit.Color * 0.36f : unit.Color;
            if (combatUnit != null && battleVisual != null && battleVisual.TargetId == combatUnit.Id &&
                battleVisual.Progress >= 0.46f && battleVisual.Progress <= 0.82f)
            {
                var flash = battleVisual.Type == BattleEventType.Heal
                    ? new Color(0.30f, 1f, 0.58f)
                    : new Color(1f, 0.34f, 0.25f);
                cardColor = Color.Lerp(cardColor, flash, 0.58f);
            }
            if (combatUnit != null && battleVisual != null && battleVisual.Type == BattleEventType.Defeat &&
                battleVisual.TargetId == combatUnit.Id)
            {
                cardColor.a = Mathf.Clamp01(1f - battleVisual.Progress * 0.78f);
            }
            GUI.color = cardColor;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var displayName = T(unit.NameKey);
            if (showRunLevel)
                DrawRunLevelCrescent(new Rect(area.center.x - 32f, area.y + 1f, 64f, 23f),
                    unit.RunLevel, 1f);
            GUI.Label(new Rect(area.x + 14f, area.y + (showRunLevel ? 20f : 16f), area.width - 28f,
                    showRunLevel ? 36f : 42f),
                displayName, ArchiveStyle(20, Color.white, TextAnchor.MiddleCenter, true));
            if (combatUnit == null)
            {
                GUI.Label(new Rect(area.x + 8f, area.y + 60f, area.width - 16f, 27f),
                    PrimaryStatsText(unit), Style(14, Color.white, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(area.x + 8f, area.y + 88f, area.width - 16f, 27f),
                    SecondaryStatsText(unit), Style(14, Color.white, TextAnchor.MiddleCenter));
                return;
            }

            GUI.Label(new Rect(area.x + 14f, area.y + 62f, area.width - 28f, 28f), T("healthShort") + " " + combatUnit.CurrentHealth + "/" + combatUnit.MaxHealth, Style(16, Color.white, TextAnchor.MiddleCenter));
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(area.x + 20f, area.y + 105f, area.width - 40f, 16f), Texture2D.whiteTexture);
            GUI.color = combatUnit.IsAlive ? new Color(0.26f, 0.92f, 0.52f) : new Color(0.32f, 0.32f, 0.32f);
            GUI.DrawTexture(new Rect(area.x + 20f, area.y + 105f, (area.width - 40f) * combatUnit.HealthPercent, 16f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private Rect GetAnimatedBattleRect(BattleUnitPresentationState unit, Rect baseRect)
        {
            // Movement and pose now live in the replaceable view driver. The IMGUI
            // renderer only projects the current foot position and scale.
            var view = unit == null ? null : battlePresentation.FindView(unit.Id);
            if (view == null || Mathf.Approximately(view.Scale, 1f)) return baseRect;
            var foot = new Vector2(baseRect.center.x, baseRect.yMax - 5f);
            var width = baseRect.width * view.Scale;
            var height = baseRect.height * view.Scale;
            return new Rect(foot.x - width * 0.5f, foot.y - height + 5f, width, height);
        }

        private void DrawBattleTargetFrame(Rect area, BattleUnitPresentationState unit)
        {
            if (unit == null || battleVisual == null || battleVisual.TargetId != unit.Id) return;
            var color = battleVisual.Type == BattleEventType.Heal
                ? new Color(0.25f, 1f, 0.56f, 0.82f)
                : new Color(1f, 0.40f, 0.22f, 0.82f);
            color.a *= 0.55f + Mathf.Abs(Mathf.Sin(battleVisual.Progress * Mathf.PI * 3f)) * 0.45f;
            GUI.color = WithOpacity(color, battleSceneOpacity);
            var frame = new Rect(area.x - 8f, area.y - 8f, area.width + 16f, area.height + 16f);
            GUI.DrawTexture(new Rect(frame.x, frame.y, frame.width, 8f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(frame.x, frame.yMax - 8f, frame.width, 8f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(frame.x, frame.y, 8f, frame.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(frame.xMax - 8f, frame.y, 8f, frame.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawBattleOverlay()
        {
            foreach (var effect in battlePresentation.Effects)
            {
                if (!effect.IsActive || effect.Type == BattleEventType.Defeat) continue;
                if (!string.IsNullOrEmpty(effect.ProjectileProfileId) && effect.ReleaseProgress >= 0f)
                    DrawBattleProjectile(effect);
            }
        }

        private void DrawBattleProjectile(BattleEffectInstance effect)
        {
            var releaseProgress = effect.ReleaseProgress;
            var impactProgress = Mathf.Max(releaseProgress + 0.01f, effect.ImpactProgress);
            var color = effect.ProjectileProfileId == BattleProjectileProfileIds.LightHeal
                ? new Color(0.30f, 1f, 0.72f)
                : effect.ProjectileProfileId == BattleProjectileProfileIds.ArrowStandard
                    ? new Color(0.72f, 0.88f, 1f)
                    : new Color(1f, 0.86f, 0.42f);
            var origin = effect.OriginPosition;
            var target = effect.TargetPosition;
            var direction = target - origin;
            var distance = direction.magnitude;
            if (distance <= 0.01f) return;
            direction /= distance;
            var perpendicular = new Vector2(-direction.y, direction.x);
            if (effect.Progress < releaseProgress) return;
            if (effect.Progress <= impactProgress)
            {
                var travel = Mathf.InverseLerp(releaseProgress, impactProgress, effect.Progress);
                var point = Vector2.Lerp(origin, target, travel);
                var tail = Vector2.Lerp(origin, point, Mathf.Max(0f, travel - 0.22f) /
                    Mathf.Max(0.01f, travel));
                if (TryDrawSpriteProjectile(effect.ProjectileProfileId, point, direction)) return;
                DrawBattleProjectileLine(tail, point, WithOpacity(color, battleSceneOpacity),
                    effect.ProjectileProfileId == BattleProjectileProfileIds.ArrowStandard ? 5f : 7f);
                if (effect.ProjectileProfileId == BattleProjectileProfileIds.ArrowStandard)
                {
                    DrawBattleProjectileLine(point - direction * 17f + perpendicular * 7f, point,
                        WithOpacity(color, battleSceneOpacity), 4f);
                    DrawBattleProjectileLine(point - direction * 17f - perpendicular * 7f, point,
                        WithOpacity(color, battleSceneOpacity), 4f);
                }
                else DrawBattleProjectileLine(point - direction * 16f, point,
                    WithOpacity(color, battleSceneOpacity), 4f);
                return;
            }

            var feedbackProfileId = BattleFeedbackProfileForProjectile(effect.ProjectileProfileId);
            if (!string.IsNullOrEmpty(feedbackProfileId) &&
                LoadBattleVfxSprite(BattleFeedbackResourcePath(feedbackProfileId)) != null) return;
            var impactFade = 1f - Mathf.Clamp01((effect.Progress - impactProgress) / 0.18f);
            var impactColor = WithOpacity(color, battleSceneOpacity * impactFade);
            GUI.color = impactColor;
            var impactSize = effect.ProjectileProfileId == BattleProjectileProfileIds.LightHeal ? 38f : 30f;
            DrawRectOutline(new Rect(target.x - impactSize * 0.5f, target.y - impactSize * 0.5f,
                impactSize, impactSize), 4f);
            GUI.color = Color.white;
            DrawBattleProjectileLine(target - perpendicular * impactSize * 0.55f,
                target + perpendicular * impactSize * 0.55f, impactColor, 3f);
        }

        private static string BattleFeedbackProfileForProjectile(string projectileProfileId)
        {
            if (string.Equals(projectileProfileId, BattleProjectileProfileIds.LightHit,
                    StringComparison.Ordinal)) return BattleFeedbackProfileIds.HitFeedback;
            if (string.Equals(projectileProfileId, BattleProjectileProfileIds.LightHeal,
                    StringComparison.Ordinal)) return BattleFeedbackProfileIds.HealFeedback;
            if (string.Equals(projectileProfileId, BattleProjectileProfileIds.SeedHeal,
                    StringComparison.Ordinal)) return BattleFeedbackProfileIds.HealFeedback;
            if (string.Equals(projectileProfileId, BattleProjectileProfileIds.SeedAttack,
                    StringComparison.Ordinal)) return BattleFeedbackProfileIds.HitFeedback;
            if (string.Equals(projectileProfileId, BattleProjectileProfileIds.ArrowStandard,
                    StringComparison.Ordinal)) return BattleFeedbackProfileIds.HitReaction;
            return string.Empty;
        }

        private static Sprite LoadBattleVfxSprite(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath)) return null;
            Sprite sprite;
            if (battleVfxSprites.TryGetValue(resourcePath, out sprite)) return sprite;
            sprite = Resources.Load<Sprite>(resourcePath);
            battleVfxSprites[resourcePath] = sprite;
            return sprite;
        }

        private bool TryDrawSpriteProjectile(string profileId, Vector2 point, Vector2 direction)
        {
            var sprite = LoadBattleVfxSprite(BattleProjectileProfiles.ResourcePathFor(profileId));
            if (sprite == null || sprite.texture == null) return false;

            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var textureRect = sprite.textureRect;
            var sourceWidth = Mathf.Max(1f, sprite.rect.width);
            var sourceHeight = Mathf.Max(1f, sprite.rect.height);
            var width = BattleProjectileProfiles.DisplayWidthFor(profileId);
            var height = width * sourceHeight / sourceWidth;
            var contentScaleX = Mathf.Clamp01(textureRect.width / sourceWidth);
            var contentScaleY = Mathf.Clamp01(textureRect.height / sourceHeight);
            var displayWidth = width * contentScaleX;
            var displayHeight = height * contentScaleY;
            var trimmedPivotX = textureRect.width <= 0f ? 0.5f :
                Mathf.Clamp01((sprite.pivot.x - (textureRect.x - sprite.rect.x)) / textureRect.width);
            var visualCenter = point - direction * (displayWidth * (trimmedPivotX - 0.50f));
            var rect = new Rect(visualCenter.x - displayWidth * 0.5f,
                visualCenter.y - displayHeight * 0.5f, displayWidth, displayHeight);
            var uv = new Rect(
                textureRect.x / sprite.texture.width,
                textureRect.y / sprite.texture.height,
                textureRect.width / sprite.texture.width,
                textureRect.height / sprite.texture.height);
            var oldMatrix = GUI.matrix;
            GUI.color = WithOpacity(Color.white, battleSceneOpacity);
            GUIUtility.RotateAroundPivot(angle, visualCenter);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
            return true;
        }

        private void DrawBattleFeedback(bool healBehindUnits)
        {
            foreach (var feedback in battleFeedbackVisuals)
            {
                var isHealFeedback = IsHealFeedbackProfile(feedback.ProfileId);
                if (isHealFeedback != healBehindUnits) continue;
                DrawBattleFeedbackVisual(feedback);
            }
            GUI.color = Color.white;
        }

        private void DrawBattleFeedbackForUnit(string unitId)
        {
            foreach (var feedback in battleFeedbackVisuals)
            {
                if (!string.Equals(feedback.TargetId, unitId, StringComparison.Ordinal) ||
                    !IsHealFeedbackProfile(feedback.ProfileId) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.HealFeedbackRise,
                        StringComparison.Ordinal)) continue;
                DrawBattleFeedbackVisual(feedback);
            }
            GUI.color = Color.white;
        }

        private void DrawBattleFeedbackRiseForUnit(string unitId)
        {
            foreach (var feedback in battleFeedbackVisuals)
            {
                if (!string.Equals(feedback.TargetId, unitId, StringComparison.Ordinal) ||
                    !string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.HealFeedbackRise,
                        StringComparison.Ordinal)) continue;
                DrawBattleFeedbackVisual(feedback);
            }
            GUI.color = Color.white;
        }

        private void DrawBattleShieldOverlay(
            BattleUnitPresentationState combatUnit, Rect visibleRect, float opacity)
        {
            if (combatUnit == null || !combatUnit.IsAlive || combatUnit.MaxShield <= 0) return;
            float overlayOpacity;
            battleShieldOverlayOpacities.TryGetValue(combatUnit.Id, out overlayOpacity);
            var targetOpacity = combatUnit.CurrentShield > 0 ? 1f : 0f;
            overlayOpacity = Mathf.MoveTowards(overlayOpacity, targetOpacity,
                Time.unscaledDeltaTime / 0.14f);
            battleShieldOverlayOpacities[combatUnit.Id] = overlayOpacity;
            if (overlayOpacity <= 0.001f) return;
            var sprite = LoadBattleVfxSprite(
                BattleProjectileProfiles.ShieldOverlayGenericResourcePath);
            if (sprite == null || sprite.texture == null) return;
            var visibleUv = BattleProjectileProfiles.FeedbackVisibleUvFor(
                BattleFeedbackProfileIds.ShieldOverlayGeneric);
            var contentAspect = visibleUv.width / Mathf.Max(0.01f, visibleUv.height);
            var width = visibleRect.width;
            var height = width / Mathf.Max(0.01f, contentAspect);
            var center = visibleRect.center;
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f,
                width, height);
            var shieldOpacity = opacity * overlayOpacity *
                Mathf.Lerp(0.22f, 0.46f, combatUnit.ShieldPercent);
            GUI.color = WithOpacity(Color.white, shieldOpacity);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, visibleUv, true);
            GUI.color = Color.white;
        }

        private static bool IsHealFeedbackProfile(string profileId)
        {
            return string.Equals(profileId, BattleFeedbackProfileIds.HealFeedback,
                       StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.HealFeedbackRing,
                       StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.HealFeedbackRise,
                       StringComparison.Ordinal);
        }

        private void DrawBattleFeedbackVisual(BattleFeedbackVisual feedback)
        {
            var sprite = LoadBattleVfxSprite(BattleFeedbackResourcePath(feedback.ProfileId));
            if (sprite == null || sprite.texture == null) return;
            if (feedback.ElapsedSeconds < feedback.StartDelaySeconds) return;
            var playDuration = Mathf.Max(0.001f,
                feedback.DurationSeconds - feedback.StartDelaySeconds);
            var localElapsed = feedback.ElapsedSeconds - feedback.StartDelaySeconds;
            var progress = Mathf.Clamp01(localElapsed / playDuration);
            var alpha = Mathf.Sin(progress * Mathf.PI);
            if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.HealFeedbackRing,
                    StringComparison.Ordinal))
            {
                var ringPulse = Mathf.Sin(progress * Mathf.PI);
                alpha *= Mathf.Lerp(0.42f, 1f, ringPulse);
                feedback.RenderScale = 1f + ringPulse * 0.06f;
            }
            else if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.HealFeedbackRise,
                         StringComparison.Ordinal))
            {
                alpha *= Mathf.Lerp(1f, 0.72f, progress);
            }
            var visibleUv = BattleProjectileProfiles.FeedbackVisibleUvFor(feedback.ProfileId);
            var visiblePivot = BattleProjectileProfiles.FeedbackVisiblePivotFor(feedback.ProfileId);
            var width = feedback.Width > 0f
                ? feedback.Width
                : feedback.HeightOverride * visibleUv.width /
                  Mathf.Max(0.01f, visibleUv.height);
            var height = feedback.HeightOverride > 0f
                ? feedback.HeightOverride
                : width * visibleUv.height / Mathf.Max(0.01f, visibleUv.width);
            width *= feedback.RenderScale;
            height *= feedback.RenderScale;
            var position = feedback.Position + feedback.Motion * progress;
            var rect = new Rect(position.x - width * visiblePivot.x,
                position.y - height * visiblePivot.y, width, height);
            GUI.color = WithOpacity(Color.white, battleSceneOpacity * alpha);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, visibleUv, true);
        }

        private static string BattleFeedbackResourcePath(string profileId)
        {
            if (string.Equals(profileId, BattleFeedbackProfileIds.HitFeedback,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HitFeedbackResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedback,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HealFeedbackResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedbackRing,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HealFeedbackRingResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedbackRise,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HealFeedbackRiseResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.ShieldOverlayGeneric,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.ShieldOverlayGenericResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.OpenDawn,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.OpenDawnResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.OlinTide,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.OlinTideResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.RescueBeam,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.RescueBeamResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HitReaction,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HitReactionResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HostileHitFeedbackV2,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HostileHitFeedbackV2ResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C03WhiteMark,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.C03WhiteMarkResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C03MarkTransfer,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.C03MarkTransferResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C04AfterSpringRelease,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.C04AfterSpringReleaseResourcePath;
            return string.Empty;
        }

        private void AdvanceBattleFeedback(float deltaTime)
        {
            if (!battlePresentationActive || battlePaused || deltaTime <= 0f) return;
            for (var index = battleFeedbackVisuals.Count - 1; index >= 0; index--)
            {
                var feedback = battleFeedbackVisuals[index];
                feedback.ElapsedSeconds += deltaTime;
                if (feedback.ElapsedSeconds >= feedback.DurationSeconds)
                    battleFeedbackVisuals.RemoveAt(index);
            }
        }

        private static void DrawBattleProjectileLine(Vector2 start, Vector2 end, Color color, float width)
        {
            var delta = end - start;
            if (delta.sqrMagnitude <= 0.001f) return;
            var oldMatrix = GUI.matrix;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, start);
            GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, delta.magnitude, width),
                Texture2D.whiteTexture);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
        }

        private void DrawBattleFloatingNumbers()
        {
            foreach (var floatingNumber in battleFloatingNumbers)
            {
                var progress = floatingNumber.DurationSeconds <= 0f
                    ? 1f
                    : Mathf.Clamp01(floatingNumber.ElapsedSeconds / floatingNumber.DurationSeconds);
                var amountColor = floatingNumber.Type == BattleEventType.Heal
                    ? new Color(0.30f, 1f, 0.58f)
                    : new Color(1f, 0.45f, 0.30f);
                var prefix = floatingNumber.Type == BattleEventType.Heal ? "+" : "−";
                var lift = progress * 36f;
                GUI.Label(new Rect(floatingNumber.TargetPosition.x - 110f,
                        floatingNumber.TargetPosition.y - 64f - lift, 220f, 72f),
                    prefix + floatingNumber.Amount,
                    BattleFloatingNumberStyle(38, WithOpacity(amountColor, battleSceneOpacity)));
            }
        }

        private void AdvanceBattleFloatingNumbers(float deltaTime)
        {
            if (!battlePresentationActive || battlePaused || deltaTime <= 0f) return;
            for (var index = battleFloatingNumbers.Count - 1; index >= 0; index--)
            {
                var floatingNumber = battleFloatingNumbers[index];
                floatingNumber.ElapsedSeconds += deltaTime;
                if (floatingNumber.ElapsedSeconds >= floatingNumber.DurationSeconds)
                    battleFloatingNumbers.RemoveAt(index);
            }
        }

        private GUIStyle BattleFloatingNumberStyle(int fontSize, Color color)
        {
            var style = Style(fontSize, color, TextAnchor.MiddleCenter, FontStyle.Normal);
            style.font = BattleFloatingNumberFont();
            style.wordWrap = false;
            style.clipping = TextClipping.Overflow;
            return style;
        }

        private Font BattleFloatingNumberFont()
        {
            if (!battleFloatingNumberFontLoadAttempted)
            {
                battleFloatingNumberFontLoadAttempted = true;
                try
                {
                    // Microsoft YaHei is used on Windows. The following names keep the
                    // same sans-serif treatment on Seeker/Android when that OS font is absent.
                    battleFloatingNumberFont = Font.CreateDynamicFontFromOSFont(new[]
                    {
                        "Microsoft YaHei UI", "Microsoft YaHei", "Noto Sans CJK SC",
                        "Noto Sans SC", "Droid Sans Fallback", "sans-serif"
                    }, 56);
                }
                catch (Exception)
                {
                    battleFloatingNumberFont = null;
                }
            }
            return battleFloatingNumberFont != null
                ? battleFloatingNumberFont
                : SourceHanSerifFont(true);
        }

        private Rect GetBaseBattleRect(BattleUnitPresentationState unit)
        {
            if (battlePresentationActive) return GetArenaBattleRect(unit);
            var y = unit.IsPlayer ? 790f : 220f;
            var column = unit.Slot % 3;
            var row = unit.Slot / 3;
            return new Rect(70f + column * 318f, y + 92f + row * 174f, 282f, 142f);
        }

        private static Rect GetArenaBattleRect(BattleUnitPresentationState unit)
        {
            return BattlePresentationLayout.GetUnitRectAtFootPosition(unit.CurrentFootPosition);
        }

        private void DrawPlayerFormation(float y, Color panelColor)
        {
            var panel = new Rect(40f, y, 1000f, 350f);
            if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawPanel(panel, true);
            else
            {
                GUI.color = panelColor;
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(70f, y + 12f, 600f, 40f), T("yourTeam"),
                Style(24, PreparationTextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal));

            for (var i = 0; i < runRoster.BoardCapacity; i++)
            {
                var column = i % 3;
                var row = i / 3;
                var cell = new Rect(70f + column * 318f, y + 58f + row * 132f, 282f, 112f);
                if (selectedRunSlot == i)
                {
                    GUI.color = new Color(0.99f, 0.84f, 0.24f);
                    GUI.DrawTexture(new Rect(cell.x - 7f, cell.y - 7f, cell.width + 14f, cell.height + 14f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }

                var runUnit = runRoster.Get(i);
                if (runUnit != null)
                {
                    DrawPreparationUnitCard(cell, CreateRunUnitEffectiveView(runUnit), true);
                }
                else
                {
                    if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawListRow(cell, false);
                    else
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.07f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                    GUI.Label(cell, T("empty"), Style(20,
                        IsExpeditionUiTrialPage ? PreparationTextMuted : new Color(1f, 1f, 1f, 0.32f),
                        TextAnchor.MiddleCenter));
                }

                if (expeditionFlow.CanPrepare && GUI.Button(cell, GUIContent.none, GUIStyle.none))
                {
                    SelectOrMoveRunUnit(i);
                }
            }

        }

        private void DrawPreparationSynergyStrip(float y)
        {
            var activeIds = ActiveSynergyIds(CurrentFormationSynergy());
            GUI.Label(new Rect(55f, y, 225f, 40f), T("synergy.title"),
                Style(18, IsExpeditionUiTrialPage ? PreparationTextPrimary : new Color(0.98f, 0.83f, 0.30f),
                    TextAnchor.MiddleLeft, FontStyle.Normal));

            if (activeIds.Count == 0)
            {
                GUI.Label(new Rect(290f, y, 735f, 40f), T("synergy.none"),
                    Style(14, IsExpeditionUiTrialPage ? PreparationTextMuted : new Color(0.55f, 0.68f, 0.73f),
                        TextAnchor.MiddleRight));
                return;
            }

            string heldId = null;
            var visibleCount = Mathf.Min(5, activeIds.Count);
            for (var index = 0; index < visibleCount; index++)
            {
                var id = activeIds[index];
                var area = new Rect(290f + index * 149f, y + 3f, 139f, 34f);
                if (DrawHoldSynergyChip(area, id, 1f, 12)) heldId = id;
            }

            if (!string.IsNullOrEmpty(heldId))
            {
                DrawSynergyDescription(new Rect(290f, y + 44f, 735f, 72f), heldId, 1f);
            }
        }

        private void DrawBattleSynergyOverlay(float opacity)
        {
            if (opacity < 0.82f) return;
            var activeIds = ActiveSynergyIds(CurrentFormationSynergy());
            if (activeIds.Count == 0) return;

            GUI.Label(new Rect(55f, 270f, 155f, 34f), T("synergy.title"),
                Style(14, WithOpacity(new Color(0.98f, 0.83f, 0.30f), opacity),
                    TextAnchor.MiddleLeft, FontStyle.Normal));
            string heldId = null;
            var visibleCount = Mathf.Min(5, activeIds.Count);
            for (var index = 0; index < visibleCount; index++)
            {
                var area = new Rect(220f + index * 163f, 270f, 153f, 34f);
                if (DrawHoldSynergyChip(area, activeIds[index], opacity * 0.72f, 12))
                {
                    heldId = activeIds[index];
                }
            }
            if (!string.IsNullOrEmpty(heldId))
            {
                DrawSynergyDescription(new Rect(280f, 312f, 520f, 76f), heldId, opacity * 0.92f);
            }
        }

        private bool DrawHoldSynergyChip(Rect area, string id, float opacity, int fontSize)
        {
            var held = GUI.RepeatButton(area, GUIContent.none, GUIStyle.none);
            var color = held ? new Color(0.13f, 0.70f, 0.62f) : SynergyColor(id);
            GUI.color = WithOpacity(color, opacity);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(area, T(id), Style(fontSize, WithOpacity(Color.white, opacity),
                TextAnchor.MiddleCenter, FontStyle.Normal));
            return held;
        }

        private void DrawSynergyDescription(Rect area, string id, float opacity)
        {
            GUI.color = WithOpacity(new Color(0.03f, 0.10f, 0.14f), opacity);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 12f, area.y + 7f, area.width - 24f, area.height - 14f),
                T(id + ".description"),
                Style(13, WithOpacity(new Color(0.90f, 0.95f, 0.96f), opacity), TextAnchor.MiddleLeft));
        }

        private static List<string> ActiveSynergyIds(FormationSynergySnapshot synergy)
        {
            var active = new List<string>();
            var ids = new[]
            {
                "faction.steel", "faction.wild", "faction.starlight",
                "profession.guardian", "profession.output", "profession.support"
            };
            foreach (var id in ids)
            {
                if (synergy.IsFactionActive(id) || synergy.IsProfessionActive(id)) active.Add(id);
            }
            return active;
        }

        private static Color SynergyColor(string id)
        {
            switch (id)
            {
                case "faction.steel": return new Color(0.25f, 0.39f, 0.52f);
                case "faction.wild": return new Color(0.28f, 0.48f, 0.34f);
                case "faction.starlight": return new Color(0.55f, 0.36f, 0.58f);
                default: return new Color(0.24f, 0.34f, 0.42f);
            }
        }

        private void DrawBench(float y)
        {
            var panel = new Rect(40f, y, 1000f, 350f);
            if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawPanel(panel, false);
            else
            {
                GUI.color = new Color(0.08f, 0.15f, 0.20f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.Label(new Rect(55f, y + 8f, 240f, 38f), T("bench"),
                Style(22, PreparationTextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(300f, y + 8f, 710f, 38f), T("deck.locked") + ": " + DeckDisplayName(),
                Style(12, IsExpeditionUiTrialPage ? PreparationTextMuted : new Color(0.55f, 0.72f, 0.78f),
                    TextAnchor.MiddleRight));

            for (var i = 0; i < runRoster.BenchCapacity; i++)
            {
                var runIndex = runRoster.BoardCapacity + i;
                var column = i % 3;
                var row = i / 3;
                var cell = new Rect(55f + column * 325f, y + 55f + row * 135f, 300f, 120f);
                if (selectedRunSlot == runIndex)
                {
                    GUI.color = new Color(0.99f, 0.84f, 0.24f);
                    GUI.DrawTexture(new Rect(cell.x - 4f, cell.y - 4f, cell.width + 8f, cell.height + 8f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                var runUnit = runRoster.Get(runIndex);
                if (runUnit == null)
                {
                    if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawListRow(cell, false);
                    else
                    {
                        GUI.color = new Color(1f, 1f, 1f, 0.07f);
                        GUI.DrawTexture(cell, Texture2D.whiteTexture);
                        GUI.color = Color.white;
                    }
                    GUI.Label(cell, T("empty"), Style(14,
                        IsExpeditionUiTrialPage ? PreparationTextMuted : new Color(1f, 1f, 1f, 0.32f),
                        TextAnchor.MiddleCenter));
                }
                else
                {
                    DrawPreparationUnitCard(cell, CreateRunUnitEffectiveView(runUnit), true);
                }
                if (expeditionFlow.CanPrepare && GUI.Button(cell, GUIContent.none, GUIStyle.none))
                {
                    SelectOrMoveRunUnit(runIndex);
                }
            }
        }

        private void DrawPreparationUnitCard(Rect area, UnitData unit, bool showRunLevel)
        {
            Sprite avatar;
            if (!artCatalog.TryGetCharacterAvatar(unit.NameKey, out avatar))
            {
                if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawListRow(area, false);
                else
                {
                    GUI.color = unit.Color;
                    GUI.DrawTexture(area, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (showRunLevel)
                    DrawRunLevelCrescent(new Rect(area.center.x - 30f, area.y + 1f, 60f, 22f),
                        unit.RunLevel, 1f);
                GUI.Label(new Rect(area.x + 10f, area.y + (showRunLevel ? 19f : 8f), area.width - 20f, 34f),
                    T(unit.NameKey), ArchiveStyle(18, PreparationTextPrimary, TextAnchor.MiddleCenter, true));
                GUI.Label(new Rect(area.x + 8f, area.y + (showRunLevel ? 50f : 44f), area.width - 16f, 24f),
                    PrimaryStatsText(unit), Style(12, PreparationTextPrimary, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(area.x + 8f, area.y + (showRunLevel ? 76f : 70f), area.width - 16f, 24f),
                    SecondaryStatsText(unit), Style(12, PreparationTextPrimary, TextAnchor.MiddleCenter));
                return;
            }

            if (IsExpeditionUiTrialPage) ActiveUiSkin.DrawListRow(area, false);
            else
            {
                GUI.color = new Color(0.105f, 0.17f, 0.21f);
                GUI.DrawTexture(area, Texture2D.whiteTexture);
                GUI.color = new Color(0.14f, 0.23f, 0.28f);
                GUI.DrawTexture(new Rect(area.x + 5f, area.y + 5f, 96f, area.height - 10f), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(area.x + 8f, area.y + 8f, 90f, area.height - 16f),
                avatar.texture, ScaleMode.ScaleToFit, true);

            var textX = area.x + 108f;
            var textWidth = area.width - 116f;
            if (showRunLevel)
            {
                DrawRunLevelCrescent(new Rect(area.x + 5f, area.y - 24f, 96f, 48f),
                    unit.RunLevel, 1f);
            }
            GUI.Label(new Rect(textX, area.y + 3f, textWidth, 38f), T(unit.NameKey),
                ArchiveStyle(17, PreparationTextPrimary, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(textX, area.y + 60f, textWidth, 20f),
                PrimaryStatsText(unit), Style(11, PreparationTextPrimary, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(textX, area.y + 82f, textWidth, 22f),
                SecondaryStatsText(unit), Style(11, PreparationTextPrimary, TextAnchor.MiddleCenter));
        }

        private void DrawDefenseScreen()
        {
            var pageOffset = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 0.5f);
            GUI.Label(new Rect(55f, 180f, 970f, 60f), T("defense.title"), Style(34, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 250f, 970f, 72f), T(defenseMessageKey), Style(19, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleLeft));
            DrawDefenseFormation(340f + pageOffset);

            GUI.Label(new Rect(55f, 850f + pageOffset, 500f, 44f), T("defense.roster"), Style(26, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            DrawDefenseDeployButton(new Rect(55f, 920f + pageOffset, 300f, 170f), "unit.guardian", UnitBasePower("unit.guardian"), PlayerColor("unit.guardian"));
            DrawDefenseDeployButton(new Rect(390f, 920f + pageOffset, 300f, 170f), "unit.archer", UnitBasePower("unit.archer"), PlayerColor("unit.archer"));
            DrawDefenseDeployButton(new Rect(725f, 920f + pageOffset, 300f, 170f), "unit.healer", UnitBasePower("unit.healer"), PlayerColor("unit.healer"));

            GUI.enabled = selectedDefenseSlot >= 0;
            if (GUI.Button(new Rect(55f, 1150f + pageOffset, 470f, 88f), T("defense.remove"), ButtonStyle(UiButtonSemantic.Danger, 20)))
            {
                defenseSlots[selectedDefenseSlot] = null;
                selectedDefenseSlot = -1;
                defenseMessageKey = "defense.saved";
                SaveGame();
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(555f, 1150f + pageOffset, 470f, 88f), T("defense.save"), ButtonStyle(UiButtonSemantic.Primary, 21)))
            {
                defenseMessageKey = "defense.saved";
                SaveGame();
            }
        }

        private void DrawDefenseFormation(float y)
        {
            GUI.color = new Color(0.09f, 0.28f, 0.34f);
            GUI.DrawTexture(new Rect(40f, y, 1000f, 470f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(70f, y + 22f, 700f, 42f), T("yourTeam"), Style(26, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            for (var i = 0; i < defenseSlots.Length; i++)
            {
                var column = i % 3;
                var row = i / 3;
                var cell = new Rect(70f + column * 318f, y + 88f + row * 170f, 282f, 140f);
                if (selectedDefenseSlot == i)
                {
                    GUI.color = new Color(0.99f, 0.84f, 0.24f);
                    GUI.DrawTexture(new Rect(cell.x - 7f, cell.y - 7f, cell.width + 14f, cell.height + 14f), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                if (defenseSlots[i] == null)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.07f);
                    GUI.DrawTexture(cell, Texture2D.whiteTexture);
                    GUI.color = Color.white;
                    GUI.Label(cell, T("empty"), Style(20, new Color(1f, 1f, 1f, 0.32f), TextAnchor.MiddleCenter));
                }
                else
                {
                    DrawUnitCard(cell, defenseSlots[i]);
                }
                if (GUI.Button(cell, GUIContent.none, GUIStyle.none)) SelectOrMoveDefenseUnit(i);
            }
        }

        private void DrawDefenseDeployButton(Rect area, string nameKey, int power, Color color)
        {
            var stats = characterCatalog.Get(nameKey).CalculateCombatStats(power, 1);
            GUI.color = color;
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 12f, area.y + 14f, area.width - 24f, 42f), T(nameKey), Style(22, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 8f, area.y + 55f, area.width - 16f, 24f),
                PrimaryStatsText(stats), Style(14, Color.white, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(area.x + 8f, area.y + 79f, area.width - 16f, 24f),
                SecondaryStatsText(stats), Style(14, Color.white, TextAnchor.MiddleCenter));
            if (GUI.Button(new Rect(area.x + 24f, area.y + 108f, area.width - 48f, 44f), T("defense.deploy"), ButtonStyle(UiButtonSemantic.Primary, 17)))
            {
                AddDefenseUnit(nameKey, power, color);
            }
        }

        private void SelectOrMoveDefenseUnit(int destination)
        {
            if (selectedDefenseSlot < 0)
            {
                if (defenseSlots[destination] != null)
                {
                    selectedDefenseSlot = destination;
                    defenseMessageKey = "defense.select";
                }
                return;
            }
            if (selectedDefenseSlot == destination)
            {
                selectedDefenseSlot = -1;
                defenseMessageKey = "defense.subtitle";
                return;
            }
            var unit = defenseSlots[selectedDefenseSlot];
            defenseSlots[selectedDefenseSlot] = defenseSlots[destination];
            defenseSlots[destination] = unit;
            selectedDefenseSlot = -1;
            defenseMessageKey = "defense.saved";
            SaveGame();
        }

        private void AddDefenseUnit(string nameKey, int power, Color color)
        {
            for (var i = 0; i < defenseSlots.Length; i++)
            {
                if (defenseSlots[i] != null) continue;
                defenseSlots[i] = CreateUnit(nameKey, power, color);
                defenseMessageKey = "defense.saved";
                SaveGame();
                return;
            }
            defenseMessageKey = "defense.full";
        }

        private void CreateStarterDefense()
        {
            defenseSlots[0] = CreateUnit("unit.guardian", UnitBasePower("unit.guardian"), PlayerColor("unit.guardian"));
            defenseSlots[1] = CreateUnit("unit.archer", UnitBasePower("unit.archer"), PlayerColor("unit.archer"));
            defenseSlots[2] = CreateUnit("unit.healer", UnitBasePower("unit.healer"), PlayerColor("unit.healer"));
        }

        private void SelectOrMoveRunUnit(int destination)
        {
            if (selectedRunSlot < 0)
            {
                if (runRoster.Get(destination) != null)
                {
                    selectedRunSlot = destination;
                    statusKey = "status.position";
                }
                return;
            }

            if (selectedRunSlot == destination)
            {
                selectedRunSlot = -1;
                statusKey = "status.recruit";
                SaveGame();
                return;
            }

            runRoster.TryMoveOrSwap(selectedRunSlot, destination);
            selectedRunSlot = -1;
            statusKey = "status.recruit";
            SaveGame();
        }

        private void DrawShop()
        {
            const float y = 1435f;
            if (expeditionFlow.CanChooseUpgrade)
            {
                DrawUpgradeChoices(y);
                DrawAbandonControl();
                return;
            }
            if (expeditionFlow.IsComplete)
            {
                DrawRunResult(y);
                return;
            }
            GUI.Label(new Rect(55f, y, 360f, 48f), T("shop"),
                Style(28, PreparationTextPrimary, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, y + 54f, 970f, 50f), T(statusKey),
                Style(20, PreparationTextMuted, TextAnchor.MiddleLeft));

            for (var i = 0; i < shopOffers.Length; i++)
            {
                var area = new Rect(55f + i * 335f, y + 95f, 300f, 135f);
                DrawRecruitButton(area, i, shopOffers[i]);
            }

            GUI.enabled = expeditionFlow.CanPrepare;
            if (GUI.Button(new Rect(55f, y + 245f, 970f, 80f), T("battle"), ButtonStyle(UiButtonSemantic.Primary, 25)))
            {
                StartBattle();
            }
            GUI.enabled = true;
            DrawAbandonControl();
        }

        private void DrawAbandonControl()
        {
            if (expeditionFlow.IsComplete) return;
            var key = confirmAbandon ? "run.abandonConfirm" : "run.abandon";
            var y = AdaptivePortraitLayout.BottomAnchoredY(layoutContentHeight, 70f, 45f);
            if (!GUI.Button(new Rect(55f, y, 970f, 70f), T(key), ButtonStyle(UiButtonSemantic.Danger, 18))) return;
            if (!confirmAbandon)
            {
                confirmAbandon = true;
                return;
            }
            AbandonRun();
        }

        private void DrawUpgradeChoices(float y)
        {
            var choiceSet = EnsureRoguelikeChoiceSet();
            GUI.Label(ExpeditionUiV46Layout.UpgradeTitle, T("upgrade.archiveTitle"),
                ArchiveStyle(32, ArchiveInk(), TextAnchor.MiddleCenter, true));
            GUI.Label(ExpeditionUiV46Layout.UpgradeSubtitle,
                T("upgrade.archiveSubtitle") + "  ·  " + T("upgrade.prompt"),
                ArchiveStyle(19, ArchiveMutedInk(), TextAnchor.MiddleCenter));

            if (choiceSet != null)
            {
                DrawUpgradeButton(ExpeditionUiV46Layout.UpgradeCards[0], choiceSet.options[0]);
                DrawUpgradeButton(ExpeditionUiV46Layout.UpgradeCards[1], choiceSet.options[1]);
                DrawUpgradeButton(ExpeditionUiV46Layout.UpgradeCards[2], choiceSet.options[2]);
            }
            GUI.Label(ExpeditionUiV46Layout.UpgradeFooterRound,
                string.Format(T("round"), round),
                ArchiveStyle(19, ArchiveMutedInk(), TextAnchor.MiddleCenter));
        }

        private void DrawPostBattleUpgradeScreen()
        {
            var choiceSet = EnsureRoguelikeChoiceSet();
            var blurredBattlefield = artCatalog.GetExpeditionUiV55Texture(
                "battlefield-signal-yard-roguelike-blur-v5.5");
            DrawFullBleedTexture(blurredBattlefield);
            var previousMatrix = PushExpeditionUiV46FullCanvas(ExpeditionUiV551Layout.ReferenceHeight);
            var canvas = ExpeditionUiV551Layout.Canvas;
            if (blurredBattlefield != null)
            {
                AspectCoverTextureRenderer.Draw(canvas, blurredBattlefield, 1f);
            }
            else
            {
                battlefieldRenderer.Draw(canvas, 1f, 0f);
                GUI.color = new Color(0.09f, 0.165f, 0.22f, 0.36f);
                GUI.DrawTexture(canvas, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            DrawExpeditionUiV55Texture(canvas, "roguelike-enhancement-underlay-v5.5",
                ScaleMode.StretchToFill);
            DrawUpgradeV551TopControls();
            DrawExpeditionUiV55Texture(ExpeditionUiV551Layout.HeaderShip,
                "roguelike-header-ship-v5.5.1", ScaleMode.StretchToFill);
            var headerTitleStyle = ArchiveStyle(language == GameLanguage.English ? 26 : 44,
                ArchiveInk(), TextAnchor.MiddleLeft, true);
            if (language == GameLanguage.English) headerTitleStyle.wordWrap = false;
            GUI.Label(ExpeditionUiV551Layout.HeaderTitle, T("upgrade.title"), headerTitleStyle);
            GUI.Label(ExpeditionUiV551Layout.HeaderSubtitle, T("upgrade.prompt"),
                ArchiveStyle(language == GameLanguage.English ? 24 : 26,
                    ArchiveInk(), TextAnchor.MiddleLeft));

            for (var index = 0; index < ExpeditionUiV551Layout.LegendLabels.Length; index++)
            {
                var rarity = (ExpeditionBuffRarity)index;
                var legendStyle = ArchiveStyle(language == GameLanguage.English ? 12 : 28,
                    ArchiveInk(), TextAnchor.MiddleCenter, true);
                if (language == GameLanguage.English) legendStyle.wordWrap = false;
                GUI.Label(ExpeditionUiV551Layout.LegendLabels[index], T(RarityLocalizationKey(rarity)),
                    legendStyle);
            }

            if (choiceSet != null && choiceSet.options != null && choiceSet.options.Length == 3)
            {
                DrawUpgradeV551Card(ExpeditionUiV551Layout.BenefitRows[0], choiceSet.options[0]);
                DrawUpgradeV551Card(ExpeditionUiV551Layout.BenefitRows[1], choiceSet.options[1]);
                DrawUpgradeV551Card(ExpeditionUiV551Layout.BenefitRows[2], choiceSet.options[2]);
            }
            GUI.Label(ExpeditionUiV551Layout.BottomHint, T("upgrade.chooseHint"),
                ArchiveStyle(28, ArchiveInk(), TextAnchor.MiddleCenter, true));
            if (Debug.isDebugBuild || Application.isEditor) DrawRoguelikeBuildDiagnostics();
            GUI.matrix = previousMatrix;
        }

        private void DrawUpgradeV551TopControls()
        {
            var topIcons = artCatalog.GetExpeditionUiV63Texture("map-a-top-icons-runtime-v6.3");
            var sharedDecor = artCatalog.GetExpeditionUiV62Texture("map-a-decor-runtime-v6.2");
            DrawSharedMapStyleTopFrames(ExpeditionUiV551Layout.Back,
                ExpeditionUiV551Layout.Abandon, ExpeditionUiV551Layout.Round,
                ExpeditionUiV551Layout.Coins, ExpeditionUiV551Layout.Life);
            DrawAtlasSlice(topIcons, ExpeditionUiV551Layout.BackIcon,
                new Rect(8f, 8f, 128f, 128f));
            if (GUI.Button(ExpeditionUiV551Layout.Back, GUIContent.none, GUIStyle.none))
            {
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }

            DrawAtlasSlice(sharedDecor, ExpeditionUiV551Layout.Compass,
                new Rect(16f, 16f, 96f, 104f));
            DrawSharedMapStyleAbandon(topIcons, ExpeditionUiV551Layout.Abandon);
            DrawSharedMapStyleHud(topIcons, ExpeditionUiV551Layout.Round,
                new Rect(280f, 8f, 128f, 128f), round.ToString());
            DrawSharedMapStyleHud(topIcons, ExpeditionUiV551Layout.Coins,
                new Rect(416f, 8f, 128f, 128f), coins.ToString());
            DrawSharedMapStyleHud(topIcons, ExpeditionUiV551Layout.Life,
                new Rect(552f, 8f, 128f, 128f), health.ToString());
        }

        private void DrawUpgradeV551Card(Rect area, RoguelikeChoiceOption option)
        {
            var definition = RoguelikeEnhancementRules.Get(option.effectId);
            var rarity = (ExpeditionBuffRarity)Mathf.Clamp(option.rarityId, 0, 4);
            var rarityIndex = Mathf.Clamp((int)rarity, 0, 4);
            var cardAtlas = artCatalog.GetExpeditionUiV55Texture("roguelike-quality-cards-v5.5.1");
            DrawAtlasSlice(cardAtlas, area, new Rect(133f, 15f + rarityIndex * 250f, 757f, 230f));

            var iconAtlas = artCatalog.GetExpeditionUiV55Texture("roguelike-benefit-icons-v5.5.2");
            var iconSource = option.effectId == "B02" || option.effectId == "B08" || option.effectId == "B14"
                ? new Rect(203f, 11f, 170f, 170f)
                : new Rect(11f, 11f, 170f, 170f);
            DrawAtlasSlice(iconAtlas, OffsetRect(area, ExpeditionUiV551Layout.BenefitIcon), iconSource);

            var qualityAtlas = artCatalog.GetExpeditionUiV55Texture("roguelike-quality-badges-v5.5.1");
            DrawAtlasSlice(qualityAtlas, OffsetRect(area, ExpeditionUiV551Layout.QualityBadge),
                new Rect(16f, 18f + rarityIndex * 58f, 142f, 50f));
            var qualityLabelStyle = ArchiveStyle(language == GameLanguage.English ? 11 : 28,
                ArchiveColor("F4F0DD"), TextAnchor.MiddleLeft, true);
            DrawCenteredAtlasIconTextGroup(qualityAtlas,
                OffsetRect(area, ExpeditionUiV551Layout.QualityBadge),
                new Rect(190f, 19f + rarityIndex * 58f, 40f, 40f),
                T(RarityLocalizationKey(rarity)), qualityLabelStyle,
                new Vector2(40f, 40f), 3f, language == GameLanguage.English ? 9 : 18);

            var benefitNameStyle = ArchiveStyle(language == GameLanguage.English ? 20 : 36,
                ArchiveInk(), TextAnchor.MiddleLeft, true);
            if (language == GameLanguage.English)
                benefitNameStyle.wordWrap = false;
            GUI.Label(OffsetRect(area, ExpeditionUiV551Layout.BenefitName),
                T(definition.NameKey), benefitNameStyle);
            DrawUpgradeV551Description(OffsetRect(area, ExpeditionUiV551Layout.BenefitDescription),
                RoguelikeCardDescription(definition, (RoguelikeRarity)option.rarityId));

            var selectArea = OffsetRect(area, ExpeditionUiV551Layout.SelectButton);
            DrawUpgradeV551SelectButton(selectArea);
            GUI.Label(selectArea, T("upgrade.choose"),
                ArchiveStyle(28, ArchiveColor("F4F0DD"), TextAnchor.MiddleCenter, true));

            // One transparent hit target owns the whole card, including the visual button.
            // This preserves whole-card selection without overlapping controls firing twice.
            if (GUI.Button(area, GUIContent.none, GUIStyle.none)) ApplyUpgrade(option.optionId);
        }

        private void DrawUpgradeV551Description(Rect area, string description)
        {
            var lineBreak = string.IsNullOrEmpty(description) ? -1 : description.IndexOf('\n');
            var effectLine = lineBreak < 0 ? description ?? string.Empty : description.Substring(0, lineBreak);
            var stackLine = lineBreak < 0 ? string.Empty : description.Substring(lineBreak + 1);
            var baseFontSize = language == GameLanguage.English ? 13 : 22;
            var minimumFontSize = language == GameLanguage.English ? 9 : 15;
            var lineHeight = area.height * 0.5f;

            var effectStyle = ArchiveStyle(baseFontSize, ArchiveInk(), TextAnchor.MiddleLeft);
            FitSingleLineFontSize(effectStyle, effectLine, area.width, minimumFontSize);
            GUI.Label(new Rect(area.x, area.y, area.width, lineHeight), effectLine, effectStyle);

            var stackStyle = ArchiveStyle(baseFontSize, ArchiveInk(), TextAnchor.MiddleLeft);
            FitSingleLineFontSize(stackStyle, stackLine, area.width, minimumFontSize);
            GUI.Label(new Rect(area.x, area.y + lineHeight, area.width, lineHeight), stackLine, stackStyle);
        }

        private static void FitSingleLineFontSize(
            GUIStyle style, string text, float maximumWidth, int minimumFontSize)
        {
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            var content = new GUIContent(text ?? string.Empty);
            while (style.fontSize > minimumFontSize && style.CalcSize(content).x > maximumWidth)
                style.fontSize--;
        }

        private void DrawUpgradeV551SelectButton(Rect area)
        {
            var atlas = artCatalog.GetExpeditionUiV52Texture("preparation-command-board-controls-v5.2");
            var pressed = Event.current != null && Event.current.type == EventType.MouseDown &&
                          area.Contains(Event.current.mousePosition);
            DrawAtlasSlice(atlas, area,
                pressed ? new Rect(541f, 142f, 463f, 171f) : new Rect(38f, 142f, 471f, 171f));
        }

        private void DrawRoguelikeBuildDiagnostics()
        {
            var toggle = new Rect(650f, 330f, 150f, 38f);
            if (GUI.Button(toggle, "BUILD TEST", ButtonStyle(UiButtonSemantic.Secondary, 13)))
                roguelikeBuildDiagnosticsOpen = !roguelikeBuildDiagnosticsOpen;
            if (!roguelikeBuildDiagnosticsOpen) return;

            var panel = new Rect(70f, 370f, 700f, 590f);
            GUI.color = new Color(0.01f, 0.035f, 0.045f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            var names = new[]
            {
                "D01 CHOICE", "D02 RARITY", "D03 ELITE", "D04 ELIG/CAP",
                "D05 REPLAY", "D06 BASE", "D07 DEFENSE", "D08 FIRE/LEECH",
                "D09 FOCUS", "D10 HP", "D11 NO BOSS", "D12 MIGRATE"
            };
            for (var index = 0; index < names.Length; index++)
            {
                var column = index % 3;
                var row = index / 3;
                var area = new Rect(panel.x + 18f + column * 222f, panel.y + 18f + row * 54f, 205f, 42f);
                if (!GUI.Button(area, names[index], ButtonStyle(UiButtonSemantic.Secondary, 12))) continue;
                roguelikeDiagnosticStateIndex = index;
                ApplyRoguelikeDiagnosticState(index);
            }
            var summary = CreateRoguelikeDiagnosticSummary();
            GUI.Label(new Rect(panel.x + 20f, panel.y + 245f, panel.width - 40f, 255f), summary,
                Style(12, new Color(0.86f, 0.91f, 0.86f), TextAnchor.UpperLeft));
            if (GUI.Button(new Rect(panel.x + 190f, panel.y + 520f, 320f, 50f),
                    language == GameLanguage.ChineseSimplified ? "复制构筑摘要" : "COPY BUILD SUMMARY",
                    ButtonStyle(UiButtonSemantic.Reward, 13)))
                GUIUtility.systemCopyBuffer = summary;
        }

        private void ApplyRoguelikeDiagnosticState(int stateIndex)
        {
            var effects = new List<RoguelikeAcquiredEffect>();
            if (stateIndex == 3) effects.Add(DiagnosticEffect("B01", RoguelikeRarity.Legendary, 3));
            if (stateIndex == 5)
            {
                effects.Add(DiagnosticEffect("B01", RoguelikeRarity.Rare, 2));
                effects.Add(DiagnosticEffect("B02", RoguelikeRarity.Epic, 2));
            }
            if (stateIndex == 6)
            {
                effects.Add(DiagnosticEffect("B03", RoguelikeRarity.Rare, 1));
                effects.Add(DiagnosticEffect("B08", RoguelikeRarity.Rare, 1));
            }
            if (stateIndex == 7)
            {
                effects.Add(DiagnosticEffect("B04", RoguelikeRarity.Rare, 1));
                effects.Add(DiagnosticEffect("B06", RoguelikeRarity.Rare, 1));
            }
            if (stateIndex == 8) effects.Add(DiagnosticEffect("B10", RoguelikeRarity.Rare, 1));
            if (stateIndex == 9)
            {
                effects.Add(DiagnosticEffect("B14", RoguelikeRarity.Rare, 1));
                effects.Add(DiagnosticEffect("B15", RoguelikeRarity.Rare, 1));
            }
            roguelikeBuild.Restore(0, 0, null, effects, null);
            roguelikeDiagnosticSuppressChoiceSet = stateIndex == 10;
            runAttackBonus = 0;
            runHealthBonus = 0;
            if (stateIndex == 10 || stateIndex == 11) return;

            var context = stateIndex == 3
                ? new RoguelikeEligibilityContext { DirectAttackerCount = 1, HasShieldSource = false }
                : new RoguelikeEligibilityContext
                {
                    DirectAttackerCount = 3, HasShieldSource = true, HasSteelOrGuardian = true
                };
            var progress = stateIndex == 1 ? 7500 : 5000;
            roguelikeBuild.EnsureChoiceSet(runId, progress, stateIndex == 2, false, true, context);
        }

        private static RoguelikeAcquiredEffect DiagnosticEffect(
            string effectId, RoguelikeRarity rarity, int stackCount)
        {
            return new RoguelikeAcquiredEffect
            {
                effectId = effectId, rarityId = (int)rarity, stackCount = stackCount,
                sourceChoiceSetId = "diagnostic", acquiredAtRunStateVersion = 0
            };
        }

        private string CreateRoguelikeDiagnosticSummary()
        {
            return "fixedState=D" + (roguelikeDiagnosticStateIndex + 1).ToString("00") + "\n" +
                   "runStateVersion=" + runEconomyActions.StateVersion + "\n" +
                   roguelikeBuild.CreateDiagnosticSummary(runId) +
                   "eventCount=" + lastBattleEventCount + "\n" +
                   "battleHash=" + lastBattleHash + "\n";
        }

        private void DrawExpeditionUiV55Texture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionUiV55Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private void DrawExpeditionResultScreen()
        {
            var resultY = 280f + AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 0.5f);
            GUI.color = new Color(0.01f, 0.035f, 0.045f, 0.72f);
            GUI.DrawTexture(new Rect(55f, resultY, 970f, 900f), Texture2D.whiteTexture);
            GUI.color = new Color(0.48f, 0.82f, 0.78f, 0.50f);
            DrawRectOutline(new Rect(55f, resultY, 970f, 900f), 4f);
            GUI.color = Color.white;
            DrawRunResult(resultY + 70f);
        }

        private void DrawUpgradeButton(Rect area, RoguelikeChoiceOption option)
        {
            var definition = RoguelikeEnhancementRules.Get(option.effectId);
            var rarity = (ExpeditionBuffRarity)Mathf.Clamp(option.rarityId, 0, 4);
            DrawExpeditionUiV46Texture(area, "buff-card-" + RarityAssetKey(rarity) + "-v4.6",
                ScaleMode.ScaleToFit);
            DrawExpeditionUiV46Texture(OffsetRect(area, 21f, 12f, 218f, 207f),
                option.effectId == "B02" || option.effectId == "B08" || option.effectId == "B14"
                    ? "buff-icon-health-v4.6"
                    : "buff-icon-attack-v4.6", ScaleMode.ScaleToFit);
            GUI.Label(OffsetRect(area, 14f, 4f, 91f, 47f), T(RarityLocalizationKey(rarity)),
                ArchiveStyle(19, ArchiveColor("F6EBD3"), TextAnchor.MiddleCenter));
            GUI.Label(OffsetRect(area, 261f, 40f, 286f, 50f), T(definition.NameKey),
                ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleLeft, true));
            GUI.Label(OffsetRect(area, 261f, 111f, 314f, 72f),
                RoguelikeCardDescription(definition, (RoguelikeRarity)option.rarityId),
                ArchiveStyle(19, ArchiveInk(), TextAnchor.MiddleLeft));
            var selectArea = OffsetRect(area, 534f, 149f, 179f, 61f);
            GUI.Label(selectArea, T("upgrade.choose"),
                ArchiveStyle(26, ArchiveColor("F4E9D0"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(selectArea, GUIContent.none, GUIStyle.none))
            {
                ApplyUpgrade(option.optionId);
            }
        }

        private static string RarityAssetKey(ExpeditionBuffRarity rarity)
        {
            switch (rarity)
            {
                case ExpeditionBuffRarity.Uncommon: return "uncommon";
                case ExpeditionBuffRarity.Rare: return "rare";
                case ExpeditionBuffRarity.Epic: return "epic";
                case ExpeditionBuffRarity.Legendary: return "legendary";
                default: return "common";
            }
        }

        private static string RarityLocalizationKey(ExpeditionBuffRarity rarity)
        {
            switch (rarity)
            {
                case ExpeditionBuffRarity.Uncommon: return "rarity.uncommon";
                case ExpeditionBuffRarity.Rare: return "rarity.rare";
                case ExpeditionBuffRarity.Epic: return "rarity.epic";
                case ExpeditionBuffRarity.Legendary: return "rarity.legendary";
                default: return "rarity.common";
            }
        }

        private static void DrawFloatingCardSurface(Rect area, Color accent, float surfaceOpacity)
        {
            GUI.color = new Color(0f, 0f, 0f, 0.28f);
            GUI.DrawTexture(new Rect(area.x + 12f, area.y + 15f, area.width, area.height), Texture2D.whiteTexture);
            GUI.color = new Color(0.025f, 0.075f, 0.09f, surfaceOpacity);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = WithOpacity(accent, 0.82f);
            DrawRectOutline(area, 4f);
            GUI.color = WithOpacity(accent, 0.52f);
            GUI.DrawTexture(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, 6f), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawChapterMapScreen()
        {
            GUI.Label(new Rect(55f, 225f, 600f, 54f), T("map.chapterTitle"),
                Style(32, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(55f, 277f, 970f, 42f), T("map.chapterPrompt"),
                Style(17, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleLeft));

            var available = expeditionMap.GetAvailableNext(currentMapNodeId);
            var mapContentHeight = Mathf.Max(1370f, layoutContentHeight - 550f);
            const float sourceMapWidth = 2172f;
            const float sourceMapHeight = 724f;
            const float currentAnchorX = 145f;
            var viewport = new Rect(28f, 330f, 1024f, mapContentHeight);
            var mapScale = mapContentHeight / sourceMapHeight;
            var mapContentWidth = sourceMapWidth * mapScale;
            var content = new Rect(0f, 0f, mapContentWidth, mapContentHeight);

            var currentNode = expeditionMap.GetNode(currentMapNodeId);
            var visibleNodeIds = ExpeditionMapDiagnostics.GetVisibleNodeIds(
                expeditionMap, currentMapNodeId);
            if (currentNode != null && expeditionMapScrollAnchorNodeId != currentNode.Id)
            {
                Vector2 currentPoint;
                expeditionMapRenderer.TryGetChapterOneLabelAnchor(
                    currentNode.AnchorIndex, content, out currentPoint);
                var maximumScroll = Mathf.Max(0f, mapContentWidth - viewport.width);
                expeditionMapScroll.x = Mathf.Clamp(
                    currentPoint.x - currentAnchorX, 0f, maximumScroll);
                expeditionMapScroll.y = 0f;
                expeditionMapScrollAnchorNodeId = currentNode.Id;
            }

            var mapEvent = Event.current;
            var mapEventType = mapEvent.rawType;
            var dragArea = new Rect(viewport.x, viewport.y, viewport.width, viewport.height - 32f);
            if (mapEventType == EventType.MouseDown && mapEvent.button == 0 &&
                dragArea.Contains(mapEvent.mousePosition))
            {
                expeditionMapPointerDown = mapEvent.mousePosition;
                expeditionMapPointerLast = mapEvent.mousePosition;
                expeditionMapPointerHeld = true;
                expeditionMapPointerDragging = false;
            }
            else if (mapEventType == EventType.MouseDrag && expeditionMapPointerHeld)
            {
                var pointerPosition = mapEvent.mousePosition;
                if (Vector2.Distance(expeditionMapPointerDown, pointerPosition) > 12f)
                {
                    expeditionMapPointerDragging = true;
                }
                if (expeditionMapPointerDragging)
                {
                    var maximumScroll = Mathf.Max(0f, mapContentWidth - viewport.width);
                    var dragDelta = pointerPosition - expeditionMapPointerLast;
                    expeditionMapScroll.x = Mathf.Clamp(
                        expeditionMapScroll.x - dragDelta.x, 0f, maximumScroll);
                    mapEvent.Use();
                }
                expeditionMapPointerLast = pointerPosition;
            }
            var releaseMapPointerAfterDraw = mapEventType == EventType.MouseUp && expeditionMapPointerHeld;
            if (releaseMapPointerAfterDraw && expeditionMapPointerDragging)
            {
                // Suppress the island button's click after a drag gesture.
                mapEvent.Use();
            }

            expeditionMapScroll = GUI.BeginScrollView(
                viewport, expeditionMapScroll, content, true, false);
            if (!expeditionMapRenderer.DrawChapterOneMap(content, 1f))
                expeditionMapRenderer.Draw(content, 1f, expeditionMap.Seed);

            // Routes and labels share the fixed map's world coordinates. Route ends
            // use the user-marked island ports, while labels retain their independent
            // artist-authored anchors.
            foreach (var node in expeditionMap.Nodes)
            {
                if (!visibleNodeIds.Contains(node.Id)) continue;
                foreach (var nextId in node.NextNodeIds)
                {
                    var target = expeditionMap.GetNode(nextId);
                    if (target == null || !visibleNodeIds.Contains(target.Id)) continue;
                    Vector2 source;
                    Vector2 targetPoint;
                    if (!expeditionMapRenderer.TryGetChapterOneRoutePorts(
                            node.Id, target.Id, content, out source, out targetPoint)) continue;
                    var isTraversed = ExpeditionProgression.IsTraversedEdge(
                        expeditionRewards.CompletedNodeIds, currentMapNodeId, node.Id, target.Id);
                    var isTraversable = node.Id == currentMapNodeId && ContainsNode(available, target.Id);
                    DrawMapLine(source, targetPoint,
                        isTraversed ? new Color(0.96f, 0.77f, 0.30f, 0.98f) :
                        isTraversable ? new Color(0.25f, 0.88f, 0.77f, 0.95f) :
                        new Color(0.20f, 0.31f, 0.36f, 0.58f),
                        isTraversed ? 6f : isTraversable ? 8f : 4f,
                        isTraversed);
                }
            }

            foreach (var node in expeditionMap.Nodes)
            {
                if (!visibleNodeIds.Contains(node.Id)) continue;
                Vector2 labelAnchor;
                if (!expeditionMapRenderer.TryGetChapterOneLabelAnchor(
                        node.AnchorIndex, content, out labelAnchor)) continue;
                DrawFixedChapterMapNode(labelAnchor, mapScale, node, available);
            }

            GUI.EndScrollView();
            if (releaseMapPointerAfterDraw)
            {
                expeditionMapPointerHeld = false;
                expeditionMapPointerDragging = false;
            }
            GUI.Label(new Rect(55f, viewport.yMax + 25f, 970f, 36f), T("map.scrollHint"),
                Style(14, new Color(0.55f, 0.67f, 0.72f), TextAnchor.MiddleCenter));
            DrawAbandonControl();
        }

        private void DrawChapterMapV62Screen()
        {
            DrawFullBleedTexture(artCatalog.GetExpeditionUiV62Texture("map-a-navy-background-v6.2"));
            var previousMatrix = PushExpeditionUiV46FullCanvas(ExpeditionMapUiV62Layout.ReferenceHeight);
            var canvas = ExpeditionMapUiV62Layout.Canvas;
            DrawExpeditionUiV62Texture(canvas, "map-a-navy-background-v6.2", ScaleMode.StretchToFill);

            var available = expeditionMap.GetAvailableNext(currentMapNodeId);
            var viewport = ExpeditionMapUiV62Layout.MapViewport;
            const float sourceMapWidth = 2172f;
            const float sourceMapHeight = 724f;
            var visibleSourceHeight = sourceMapHeight - ExpeditionMapUiV62Layout.MapPaperCropTop -
                                      ExpeditionMapUiV62Layout.MapPaperCropBottom;
            var baseMapScale = viewport.height / visibleSourceHeight;
            HandleMapV62Zoom(viewport, sourceMapWidth * baseMapScale);
            var mapScale = baseMapScale * expeditionMapZoom;
            var mapContentWidth = sourceMapWidth * mapScale;
            var mapContentHeight = sourceMapHeight * mapScale;
            var visibleScaledHeight = visibleSourceHeight * mapScale;
            var mapContentY = (viewport.height - visibleScaledHeight) * 0.5f -
                              ExpeditionMapUiV62Layout.MapPaperCropTop * mapScale;
            var contentWithoutScroll = new Rect(0f, mapContentY, mapContentWidth, mapContentHeight);

            var currentNode = expeditionMap.GetNode(currentMapNodeId);
            var visibleNodeIds = ExpeditionMapDiagnostics.GetVisibleNodeIds(
                expeditionMap, currentMapNodeId);
            if (currentNode != null && expeditionMapScrollAnchorNodeId != currentNode.Id)
            {
                Vector2 currentPoint;
                expeditionMapRenderer.TryGetChapterOneLabelAnchor(
                    currentNode.AnchorIndex, contentWithoutScroll, out currentPoint);
                var maximumScroll = Mathf.Max(0f, mapContentWidth - viewport.width);
                expeditionMapScroll.x = Mathf.Clamp(currentPoint.x - 145f, 0f, maximumScroll);
                expeditionMapScroll.y = 0f;
                expeditionMapScrollAnchorNodeId = currentNode.Id;
            }

            var mapEvent = Event.current;
            var mapEventType = mapEvent.rawType;
            if (mapEventType == EventType.MouseDown && mapEvent.button == 0 &&
                viewport.Contains(mapEvent.mousePosition))
            {
                expeditionMapPointerDown = mapEvent.mousePosition;
                expeditionMapPointerLast = mapEvent.mousePosition;
                expeditionMapPointerHeld = true;
                expeditionMapPointerDragging = false;
            }
            else if (mapEventType == EventType.MouseDrag && expeditionMapPointerHeld)
            {
                var pointerPosition = mapEvent.mousePosition;
                if (Vector2.Distance(expeditionMapPointerDown, pointerPosition) > 12f)
                    expeditionMapPointerDragging = true;
                if (expeditionMapPointerDragging)
                {
                    var maximumScroll = Mathf.Max(0f, mapContentWidth - viewport.width);
                    var dragDelta = pointerPosition - expeditionMapPointerLast;
                    expeditionMapScroll.x = Mathf.Clamp(
                        expeditionMapScroll.x - dragDelta.x, 0f, maximumScroll);
                    mapEvent.Use();
                }
                expeditionMapPointerLast = pointerPosition;
            }
            var releaseMapPointerAfterDraw = mapEventType == EventType.MouseUp && expeditionMapPointerHeld;
            if (releaseMapPointerAfterDraw && expeditionMapPointerDragging) mapEvent.Use();

            GUI.BeginGroup(viewport);
            var content = new Rect(-expeditionMapScroll.x, mapContentY, mapContentWidth, mapContentHeight);
            if (!expeditionMapRenderer.DrawChapterOneMap(content, 1f))
                expeditionMapRenderer.Draw(content, 1f, expeditionMap.Seed);

            foreach (var node in expeditionMap.Nodes)
            {
                if (!visibleNodeIds.Contains(node.Id)) continue;
                Vector2 labelAnchor;
                if (!expeditionMapRenderer.TryGetChapterOneLabelAnchor(
                        node.AnchorIndex, content, out labelAnchor)) continue;
                DrawFixedChapterMapNode(labelAnchor, mapScale, node, available);
            }

            // Routes intentionally render after node badges and labels. When the
            // authored paths cross node information, the route remains the topmost
            // map-content cue while sharing the same pan/zoom transform.
            foreach (var node in expeditionMap.Nodes)
            {
                if (!visibleNodeIds.Contains(node.Id)) continue;
                foreach (var nextId in node.NextNodeIds)
                {
                    var target = expeditionMap.GetNode(nextId);
                    if (target == null || !visibleNodeIds.Contains(target.Id)) continue;
                    Vector2 source;
                    Vector2 targetPoint;
                    if (!expeditionMapRenderer.TryGetChapterOneRoutePorts(
                            node.Id, target.Id, content, out source, out targetPoint)) continue;
                    var isTraversed = ExpeditionProgression.IsTraversedEdge(
                        expeditionRewards.CompletedNodeIds, currentMapNodeId, node.Id, target.Id);
                    var isTraversable = node.Id == currentMapNodeId && ContainsNode(available, target.Id);
                    DrawMapLine(source, targetPoint,
                        isTraversed ? new Color(0.96f, 0.77f, 0.30f, 0.98f) :
                        isTraversable ? new Color(0.96f, 0.77f, 0.30f, 0.98f) :
                        new Color(0.68f, 0.54f, 0.27f, 0.76f),
                        isTraversed ? 6f : isTraversable ? 8f : 4f,
                        isTraversed);
                }
            }

            GUI.EndGroup();

            if (releaseMapPointerAfterDraw)
            {
                expeditionMapPointerHeld = false;
                expeditionMapPointerDragging = false;
            }

            // The underlay is intentionally rendered after MapContent: its transparent
            // viewport masks the map's paper edge while its frame remains pixel locked.
            DrawExpeditionUiV62Texture(canvas, "map-a-panel-underlay-v6.2", ScaleMode.StretchToFill);
            DrawMapV62Chrome();
            if (Debug.isDebugBuild || Application.isEditor) DrawExpeditionMapDiagnostics();
            GUI.matrix = previousMatrix;
        }

        private void DrawExpeditionMapDiagnostics()
        {
            var toggleLabel = language == GameLanguage.ChineseSimplified ? "地图诊断" : "MAP DIAG";
            if (GUI.Button(new Rect(676f, 282f, 137f, 42f), toggleLabel,
                    ButtonStyle(UiButtonSemantic.Secondary, 15)))
                expeditionMapDiagnosticsOpen = !expeditionMapDiagnosticsOpen;
            if (!expeditionMapDiagnosticsOpen || expeditionMap == null) return;

            var panel = new Rect(28f, 320f, 785f, 650f);
            GUI.color = new Color(0.015f, 0.045f, 0.055f, 0.95f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;

            expeditionMapDiagnosticNodeIndex = Mathf.Clamp(
                expeditionMapDiagnosticNodeIndex, 0, expeditionMap.Nodes.Count - 1);
            var inspectedNode = expeditionMap.Nodes[expeditionMapDiagnosticNodeIndex];
            var inspectedEncounter = IsBattleNode(inspectedNode.Type)
                ? ChapterOneEncounterRules.Generate(
                    expeditionMapSeed, inspectedNode,
                    inspectedNode.Type == ExpeditionNodeType.NormalBattle && !HasCompletedNormalBattle())
                : null;
            var summary = ExpeditionMapDiagnostics.CreateEncounterEconomySummary(
                ClientVersionLabel, expeditionMap, inspectedNode.Id, inspectedEncounter,
                coins, expeditionEconomy, expeditionRewards.CompletedNodeIds) +
                CreateRunEconomyDiagnosticSummary();
            GUI.Label(new Rect(44f, 337f, 445f, 612f), summary,
                Style(13, new Color(0.86f, 0.91f, 0.86f), TextAnchor.UpperLeft));

            var fixedSeeds = ExpeditionMapDiagnostics.FixedTestSeeds;
            expeditionMapDiagnosticSeedIndex = Mathf.Clamp(
                expeditionMapDiagnosticSeedIndex, 0, fixedSeeds.Count - 1);
            var selectedSeed = fixedSeeds[expeditionMapDiagnosticSeedIndex];
            GUI.Label(new Rect(500f, 340f, 295f, 72f),
                "seed " + selectedSeed.Seed + "\n" + selectedSeed.Scenario,
                Style(13, new Color(0.96f, 0.80f, 0.37f), TextAnchor.UpperCenter));

            var previousLabel = language == GameLanguage.ChineseSimplified ? "上一个" : "PREV";
            var nextLabel = language == GameLanguage.ChineseSimplified ? "下一个" : "NEXT";
            var applyLabel = language == GameLanguage.ChineseSimplified ? "应用并重开" : "APPLY + RESTART";
            var copyLabel = language == GameLanguage.ChineseSimplified ? "复制摘要" : "COPY SUMMARY";
            if (GUI.Button(new Rect(500f, 426f, 138f, 48f), previousLabel,
                    ButtonStyle(UiButtonSemantic.Secondary, 15)))
                expeditionMapDiagnosticSeedIndex =
                    (expeditionMapDiagnosticSeedIndex + fixedSeeds.Count - 1) % fixedSeeds.Count;
            if (GUI.Button(new Rect(652f, 426f, 138f, 48f), nextLabel,
                    ButtonStyle(UiButtonSemantic.Secondary, 15)))
                expeditionMapDiagnosticSeedIndex =
                    (expeditionMapDiagnosticSeedIndex + 1) % fixedSeeds.Count;
            if (GUI.Button(new Rect(500f, 490f, 290f, 48f), applyLabel,
                    ButtonStyle(UiButtonSemantic.Primary, 15)))
                ApplyExpeditionDiagnosticSeed(selectedSeed.Seed);
            if (GUI.Button(new Rect(500f, 554f, 290f, 48f), copyLabel,
                    ButtonStyle(UiButtonSemantic.Reward, 15)))
                GUIUtility.systemCopyBuffer = summary;

            var nodePreviousLabel = language == GameLanguage.ChineseSimplified ? "上一节点" : "PREV NODE";
            var nodeNextLabel = language == GameLanguage.ChineseSimplified ? "下一节点" : "NEXT NODE";
            GUI.Label(new Rect(500f, 620f, 290f, 52f),
                "snapshot " + inspectedNode.Id + " / " + inspectedNode.Type,
                Style(14, new Color(0.66f, 0.88f, 0.91f), TextAnchor.MiddleCenter));
            if (GUI.Button(new Rect(500f, 680f, 138f, 46f), nodePreviousLabel,
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                expeditionMapDiagnosticNodeIndex =
                    (expeditionMapDiagnosticNodeIndex + expeditionMap.Nodes.Count - 1) % expeditionMap.Nodes.Count;
            if (GUI.Button(new Rect(652f, 680f, 138f, 46f), nodeNextLabel,
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                expeditionMapDiagnosticNodeIndex =
                    (expeditionMapDiagnosticNodeIndex + 1) % expeditionMap.Nodes.Count;
            if (GUI.Button(new Rect(500f, 742f, 88f, 44f), "M19",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                expeditionMapDiagnosticNodeIndex = DiagnosticNodeIndex("M19");
            if (GUI.Button(new Rect(601f, 742f, 88f, 44f), "M28",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                expeditionMapDiagnosticNodeIndex = DiagnosticNodeIndex("M28");
            if (GUI.Button(new Rect(702f, 742f, 88f, 44f), "M29",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                expeditionMapDiagnosticNodeIndex = DiagnosticNodeIndex("M29");
        }

        private int DiagnosticNodeIndex(string nodeId)
        {
            if (expeditionMap == null) return 0;
            for (var index = 0; index < expeditionMap.Nodes.Count; index++)
                if (expeditionMap.Nodes[index].Id == nodeId) return index;
            return 0;
        }

        private string CreateRunEconomyDiagnosticSummary()
        {
            var summary = new System.Text.StringBuilder();
            summary.Append("runEconomyRulesVersion=").Append(RunEconomyRulesVersion).Append('\n');
            summary.Append("runId=").Append(runId ?? string.Empty).Append('\n');
            summary.Append("runStateVersion=").Append(runEconomyActions.StateVersion).Append('\n');
            summary.Append("roster=").Append(runRoster.BoardCount).Append('/')
                .Append(runRoster.TotalCount - runRoster.BoardCount).Append("/12\n");
            summary.Append("shopRefreshSerial=").Append(shopRefreshSerial).Append('\n');
            for (var index = 0; index < ShopOfferCount; index++)
            {
                var definitionId = shopOffers != null && index < shopOffers.Length
                    ? shopOffers[index]
                    : null;
                var level = ShopOfferLevel(index);
                var listed = shopOfferListedPrices != null && index < shopOfferListedPrices.Length
                    ? shopOfferListedPrices[index]
                    : 0;
                summary.Append("offer[").Append(index).Append("]=")
                    .Append(shopOfferIds != null && index < shopOfferIds.Length ? shopOfferIds[index] : string.Empty)
                    .Append(',').Append(definitionId ?? "sold").Append(",L").Append(level)
                    .Append(',').Append(listed).Append("->")
                    .Append(definitionId == null ? 0 : expeditionEconomy.QuoteRecruitmentPrice(listed))
                    .Append('\n');
            }
            for (var index = 0; index < runRoster.TotalCapacity; index++)
            {
                var unit = runRoster.Get(index);
                if (unit == null) continue;
                summary.Append("unit[").Append(index).Append("]=").Append(unit.RunUnitId)
                    .Append(',').Append(unit.DefinitionId).Append(",L").Append(unit.RunLevel)
                    .Append(",basis:").Append(unit.AcquisitionBasis).Append('\n');
            }
            summary.Append("runActionCount=").Append(runEconomyActions.Count).Append('\n');
            summary.Append("lastRunAction=").Append(runEconomyActions.LastActionId ?? string.Empty)
                .Append(',').Append(runEconomyActions.LastResultCode).Append('\n');
            summary.Append(roguelikeBuild.CreateDiagnosticSummary(runId));
            summary.Append("eventCount=").Append(lastBattleEventCount).Append('\n');
            summary.Append("battleHash=").Append(lastBattleHash).Append('\n');
            return summary.ToString();
        }

        private void ApplyExpeditionDiagnosticSeed(uint seed)
        {
            RestartExpeditionState("status.route");
            expeditionMapSeed = seed == 0 ? 0x51EA2C31u : seed;
            expeditionMap = ExpeditionMapGraph.CreateChapterOne(expeditionMapSeed);
            currentMapNodeId = expeditionMap.GetLayer(1)[0].Id;
            round = 1;
            expeditionMapScroll = Vector2.zero;
            expeditionMapScrollAnchorNodeId = null;
            expeditionMapZoom = 1f;
            expeditionMapPinchDistance = 0f;
            expeditionMapDiagnosticNodeIndex = 0;
            preparationShopOpen = false;
            currentView = ScreenView.Expedition;
            SaveGame();
        }

        private void HandleMapV62Zoom(Rect viewport, float baseContentWidth)
        {
            var mapEvent = Event.current;
            if (mapEvent.type == EventType.ScrollWheel && viewport.Contains(mapEvent.mousePosition))
            {
                SetMapV62Zoom(viewport, mapEvent.mousePosition.x, baseContentWidth,
                    expeditionMapZoom - mapEvent.delta.y * 0.08f);
                mapEvent.Use();
            }

            if (mapEvent.type != EventType.Repaint) return;
            if (Input.touchCount != 2)
            {
                expeditionMapPinchDistance = 0f;
                return;
            }

            var inverse = GUI.matrix.inverse;
            var firstTouch = Input.GetTouch(0).position;
            var secondTouch = Input.GetTouch(1).position;
            var first = (Vector2)inverse.MultiplyPoint3x4(
                new Vector3(firstTouch.x, Screen.height - firstTouch.y, 0f));
            var second = (Vector2)inverse.MultiplyPoint3x4(
                new Vector3(secondTouch.x, Screen.height - secondTouch.y, 0f));
            var midpoint = (first + second) * 0.5f;
            var distance = Vector2.Distance(first, second);
            if (viewport.Contains(midpoint) && expeditionMapPinchDistance > 0f)
            {
                var ratio = distance / expeditionMapPinchDistance;
                SetMapV62Zoom(viewport, midpoint.x, baseContentWidth,
                    expeditionMapZoom * ratio);
            }
            expeditionMapPinchDistance = distance;
        }

        private void SetMapV62Zoom(Rect viewport, float focusX, float baseContentWidth, float requestedZoom)
        {
            var nextZoom = Mathf.Clamp(requestedZoom, 1f, 1.8f);
            if (Mathf.Approximately(nextZoom, expeditionMapZoom)) return;
            var focusInViewport = Mathf.Clamp(focusX - viewport.x, 0f, viewport.width);
            var oldWidth = baseContentWidth * expeditionMapZoom;
            var contentRatio = oldWidth <= 0f
                ? 0f
                : (expeditionMapScroll.x + focusInViewport) / oldWidth;
            var newWidth = baseContentWidth * nextZoom;
            expeditionMapScroll.x = Mathf.Clamp(contentRatio * newWidth - focusInViewport,
                0f, Mathf.Max(0f, newWidth - viewport.width));
            expeditionMapZoom = nextZoom;
        }

        private void DrawMapV62Chrome()
        {
            var controls = artCatalog.GetExpeditionUiV62Texture(ExpeditionUiSharedFrames.ControlsAtlas);
            var decor = artCatalog.GetExpeditionUiV62Texture("map-a-decor-runtime-v6.2");
            var legend = artCatalog.GetExpeditionUiV63Texture("map-a-legend-chips-runtime-v6.3");
            var topIcons = artCatalog.GetExpeditionUiV63Texture("map-a-top-icons-runtime-v6.3");

            DrawAtlasSlice(controls, ExpeditionMapUiV62Layout.BackFrameVisual, new Rect(16f, 16f, 94f, 94f));
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.AbandonFrame,
                ExpeditionUiSharedFrames.AbandonFrame, ScaleMode.ScaleToFit);
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.HudFrames[0], ExpeditionUiSharedFrames.RoundHud);
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.HudFrames[1], ExpeditionUiSharedFrames.CoinsHud);
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.HudFrames[2], ExpeditionUiSharedFrames.LifeHud);

            DrawAtlasSlice(decor, ExpeditionMapUiV62Layout.CompassDecor, new Rect(16f, 16f, 96f, 104f));
            DrawAtlasSlice(decor, ExpeditionMapUiV62Layout.HeaderShip, new Rect(128f, 16f, 96f, 108f));
            DrawAtlasSlice(decor, ExpeditionMapUiV62Layout.DividerCompass, new Rect(240f, 52f, 32f, 34f));

            DrawAtlasSlice(topIcons, ExpeditionMapUiV62Layout.BackIcon, new Rect(8f, 8f, 128f, 128f));

            GUI.Label(ExpeditionMapUiV62Layout.HeaderTitle, T("map.chapterTitle"),
                ArchiveStyle(44, ArchiveInk(), TextAnchor.MiddleLeft, true));
            GUI.Label(ExpeditionMapUiV62Layout.HeaderSubtitle, T("map.chapterPrompt"),
                ArchiveStyle(26, ArchiveMutedInk(), TextAnchor.MiddleLeft));

            var legendKeys = new[]
            {
                "map.legend.normal", "map.legend.elite", "map.legend.event",
                "map.legend.supply", "map.legend.boss"
            };
            for (var index = 0; index < ExpeditionMapUiV62Layout.LegendCentersX.Length; index++)
            {
                var iconArea = new Rect(ExpeditionMapUiV62Layout.LegendCentersX[index] - 25f,
                    ExpeditionMapUiV62Layout.LegendIconTop, 50f, 50f);
                DrawAtlasSlice(legend, iconArea, new Rect(8f + index * 136f, 8f, 128f, 128f));
                GUI.Label(ExpeditionMapUiV62Layout.LegendLabels[index], T(legendKeys[index]),
                    ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleCenter));
            }
            GUI.Label(ExpeditionMapUiV62Layout.GestureHint, T("map.scrollHint"),
                ArchiveStyle(26, ArchiveMutedInk(), TextAnchor.MiddleCenter));

            var noRewardsSuffix = currentExpeditionRewardsEnabled
                ? string.Empty
                : " · " + T("map.noRewardsShort");
            var hudValues = language == GameLanguage.ChineseSimplified
                ? new[]
                {
                    round.ToString(),
                    coins + noRewardsSuffix.Replace(" ", string.Empty),
                    health.ToString()
                }
                : new[]
                {
                    round.ToString(),
                    coins + noRewardsSuffix,
                    health.ToString()
                };
            var hudIconSources = new[]
            {
                new Rect(280f, 8f, 128f, 128f),
                new Rect(416f, 8f, 128f, 128f),
                new Rect(552f, 8f, 128f, 128f)
            };
            for (var index = 0; index < hudValues.Length; index++)
                DrawCenteredAtlasIconTextGroup(topIcons,
                    ExpeditionMapUiV62Layout.HudFrames[index], hudIconSources[index],
                    hudValues[index],
                    ArchiveStyle(language == GameLanguage.English ? 20 : 26,
                        ArchiveColor("F4F0DD"), TextAnchor.MiddleLeft, true),
                    new Vector2(34f, 34f), 10f, 14);

            var abandonShort = confirmAbandon ? T("run.abandonConfirmShort") : T("run.abandonShort");
            DrawCenteredAtlasIconTextGroup(topIcons, ExpeditionMapUiV62Layout.AbandonFrame,
                new Rect(144f, 8f, 128f, 128f), abandonShort,
                ArchiveStyle(33, ArchiveColor("F6DAD0"), TextAnchor.MiddleLeft, true),
                new Vector2(32f, 32f), 8f, 14);

            if (GUI.Button(ExpeditionMapUiV62Layout.BackFrame, GUIContent.none, GUIStyle.none))
            {
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }
            if (GUI.Button(ExpeditionMapUiV62Layout.AbandonFrame,
                    GUIContent.none, GUIStyle.none))
            {
                if (confirmAbandon) AbandonRun();
                else
                {
                    confirmAbandon = true;
                    statusKey = "status.abandonPrompt";
                }
            }
        }

        private void DrawExpeditionUiV62Texture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionUiV62Texture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private void DrawNonBattleNodeScreen()
        {
            var node = expeditionMap.GetNode(currentMapNodeId);
            if (node == null)
            {
                expeditionFlow.TryFinishNode();
                return;
            }

            if (IsEventFamilyNodeType(node.Type))
            {
                var previousMatrix = PushExpeditionUiV53Canvas();
                DrawEventFamilyV126Screen(node);
                GUI.matrix = previousMatrix;
                return;
            }

            var nodeOffset = AdaptivePortraitLayout.ExpandedPageOffset(layoutContentHeight, 0.5f);
            GUI.Label(new Rect(55f, 240f + nodeOffset, 970f, 60f), T(MapNodeTypeKey(node.Type)),
                Style(34, MapNodeColor(node.Type), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(90f, 310f + nodeOffset, 900f, 70f), T("node." + NodeActionKey(node.Type) + ".description"),
                Style(19, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleCenter));

            if (node.Type == ExpeditionNodeType.Shop)
            {
                DrawExpeditionItemShop(node, nodeOffset);
            }
            else if (node.Type == ExpeditionNodeType.Event)
            {
                DrawEventChoices(node, nodeOffset);
            }
            else if (node.Type == ExpeditionNodeType.Supply)
            {
                DrawTreasureChoices(node, nodeOffset);
            }
            else if (node.Type == ExpeditionNodeType.Rest)
            {
                DrawRestChoices(node, nodeOffset);
            }
            else
            {
                DrawTreasureChoices(node, nodeOffset);
            }
            DrawExpeditionBackpackStrip(845f + nodeOffset);
            DrawAbandonControl();
        }

        private void DrawEventFamilyV126Screen(ExpeditionMapNode node)
        {
            var effectiveType = EffectiveNodeType(node.Type);
            var theme = EventFamilyTheme(effectiveType);
            var eventId = effectiveType == ExpeditionNodeType.Event
                ? ResolveEventFamilyEventId(node)
                : string.Empty;

            GUI.color = new Color(0f, 0f, 0f, 0.12f);
            GUI.DrawTexture(ExpeditionEventFamilyV126Layout.Canvas, Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawSharedExpeditionPageShell(ExpeditionEventFamilyV126Layout.SharedLargeShell);
            DrawEventFamilyTexture(ExpeditionEventFamilyV126Layout.HeaderRibbon,
                ExpeditionEventFamilyV126Layout.HeaderRibbonResource, ScaleMode.ScaleToFit);
            DrawEventFamilyTexture(ExpeditionEventFamilyV126Layout.HeaderEmblem,
                ExpeditionEventFamilyV126Layout.EmblemResource(theme), ScaleMode.ScaleToFit);
            DrawEventFamilyTopChrome();

            var title = effectiveType == ExpeditionNodeType.Event
                ? T("node.event." + eventId)
                : effectiveType == ExpeditionNodeType.Shop
                    ? T("node.shop.prompt")
                    : T(MapNodeTypeKey(effectiveType));
            GUI.Label(ExpeditionEventFamilyV126Layout.Title, title,
                ArchiveStyle(language == GameLanguage.English ? 28 : 40,
                    ArchiveInk(), TextAnchor.MiddleCenter, true));
            GUI.Label(ExpeditionEventFamilyV126Layout.Description,
                T("node." + NodeActionKey(effectiveType) + ".description"),
                ArchiveStyle(language == GameLanguage.English ? 17 : 23,
                    ArchiveMutedInk(), TextAnchor.MiddleCenter));

            if (effectiveType == ExpeditionNodeType.Event)
                DrawEventFamilyEventChoices(node, eventId);
            else if (effectiveType == ExpeditionNodeType.Shop)
                DrawEventFamilyShopChoices(node);
            else if (effectiveType == ExpeditionNodeType.Rest)
                DrawEventFamilyRestChoices(node);
            else
                DrawEventFamilyTreasureChoices(node);
        }

        private void DrawEventFamilyTopChrome()
        {
            var controls = artCatalog.GetExpeditionUiV62Texture(ExpeditionUiSharedFrames.ControlsAtlas);
            var decor = artCatalog.GetExpeditionUiV62Texture("map-a-decor-runtime-v6.2");
            var topIcons = artCatalog.GetExpeditionUiV63Texture("map-a-top-icons-runtime-v6.3");

            DrawAtlasSlice(controls, ExpeditionMapUiV62Layout.BackFrameVisual,
                new Rect(16f, 16f, 94f, 94f));
            DrawAtlasSlice(topIcons, ExpeditionMapUiV62Layout.BackIcon,
                new Rect(8f, 8f, 128f, 128f));
            DrawAtlasSlice(decor, ExpeditionMapUiV62Layout.CompassDecor,
                new Rect(16f, 16f, 96f, 104f));
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.AbandonFrame,
                ExpeditionUiSharedFrames.AbandonFrame, ScaleMode.ScaleToFit);
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.HudFrames[0],
                ExpeditionUiSharedFrames.RoundHud);
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.HudFrames[1],
                ExpeditionUiSharedFrames.CoinsHud);
            DrawSharedExpeditionTexture(ExpeditionMapUiV62Layout.HudFrames[2],
                ExpeditionUiSharedFrames.LifeHud);

            var noRewardsSuffix = currentExpeditionRewardsEnabled
                ? string.Empty
                : " · " + T("map.noRewardsShort");
            var hudValues = new[]
            {
                round.ToString(), coins + noRewardsSuffix, health.ToString()
            };
            var hudIconSources = new[]
            {
                new Rect(280f, 8f, 128f, 128f),
                new Rect(416f, 8f, 128f, 128f),
                new Rect(552f, 8f, 128f, 128f)
            };
            for (var index = 0; index < hudValues.Length; index++)
                DrawCenteredAtlasIconTextGroup(topIcons,
                    ExpeditionMapUiV62Layout.HudFrames[index], hudIconSources[index],
                    hudValues[index],
                    ArchiveStyle(language == GameLanguage.English ? 20 : 26,
                        ArchiveColor("F4F0DD"), TextAnchor.MiddleLeft, true),
                    new Vector2(34f, 34f), 10f, 14);

            var abandonShort = confirmAbandon ? T("run.abandonConfirmShort") : T("run.abandonShort");
            DrawCenteredAtlasIconTextGroup(topIcons, ExpeditionMapUiV62Layout.AbandonFrame,
                new Rect(144f, 8f, 128f, 128f), abandonShort,
                ArchiveStyle(33, ArchiveColor("F6DAD0"), TextAnchor.MiddleLeft, true),
                new Vector2(32f, 32f), 8f, 14);

            if (GUI.Button(ExpeditionMapUiV62Layout.BackFrame, GUIContent.none, GUIStyle.none))
            {
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }
            if (GUI.Button(ExpeditionMapUiV62Layout.AbandonFrame, GUIContent.none, GUIStyle.none))
            {
                if (confirmAbandon) AbandonRun();
                else
                {
                    confirmAbandon = true;
                    statusKey = "status.abandonPrompt";
                }
            }
        }

        private void DrawEventFamilyEventChoices(ExpeditionMapNode node, string eventId)
        {
            var labels = new[]
            {
                T("node.event." + eventId + ".a"),
                T("node.event." + eventId + ".b"),
                T("node.event.leave")
            };
            var icons = new[]
            {
                "ART-UI-PREPARATION-ICON-COIN-V6.7.0",
                "ART-UI-PREPARATION-ICON-STATLIFE-V6.7.0",
                "ART-UI-PREPARATION-ICON-BACKARROW-V6.7.0"
            };
            for (var index = 0; index < labels.Length; index++)
            {
                string optionTitle;
                string optionDescription;
                SplitEventFamilyOptionCopy(labels[index], out optionTitle, out optionDescription);
                if (!DrawEventFamilyOption(index, "event", optionTitle, optionDescription,
                        icons[index], T("upgrade.choose"), true)) continue;
                if (index == 0) ResolveEventChoice(node, eventId, true);
                else if (index == 1) ResolveEventChoice(node, eventId, false);
                else if (TryChooseNonBattleAction(node, eventId + ":leave")) FinishNonBattleNode();
            }
            DrawEventFamilyFooterHint(T(statusKey));
        }

        private void DrawEventFamilyRestChoices(ExpeditionMapNode node)
        {
            var first = expeditionEconomy.Morale < 2 ? "node.rest.morale" : "node.rest.fortify";
            var labels = new[] { T(first), T("node.rest.scout"), T("node.rest.drill") };
            var icons = new[]
            {
                "ART-UI-PREPARATION-ICON-STATLIFE-V6.7.0",
                "ART-UI-PREPARATION-ICON-COMPASS-V6.7.0",
                "ART-UI-PREPARATION-ICON-STATATTACK-V6.7.0"
            };
            for (var index = 0; index < labels.Length; index++)
            {
                string optionTitle;
                string optionDescription;
                SplitEventFamilyOptionCopy(labels[index], out optionTitle, out optionDescription);
                if (!DrawEventFamilyOption(index, "rest", optionTitle, optionDescription,
                        icons[index], T("upgrade.choose"), true)) continue;
                if (index == 0)
                {
                    if (TryChooseNonBattleAction(node, "rest"))
                    {
                        if (expeditionEconomy.Morale < 2) expeditionEconomy.AddMorale(1);
                        else expeditionEconomy.SetNextBattlePreparation("fortify8");
                    }
                }
                else if (index == 1)
                {
                    if (TryChooseNonBattleAction(node, "scout"))
                        expeditionEconomy.SetNextBattlePreparation("formationShield5");
                }
                else if (TryChooseNonBattleAction(node, "drill"))
                    expeditionEconomy.SetNextBattlePreparation("action8");
                FinishNonBattleNode();
            }
            DrawEventFamilyFooterHint(T(statusKey));
        }

        private void DrawEventFamilyTreasureChoices(ExpeditionMapNode node)
        {
            var amount = ChapterOneExpeditionEconomy.TreasureCoinAmount(node.JourneyProgress);
            if (DrawEventFamilyOption(0, "treasure", T("node.treasure.coins"),
                    "+" + amount + " " + T("coins"),
                    "ART-UI-PREPARATION-ICON-COIN-V6.7.0", T("upgrade.choose"), true))
            {
                if (TryChooseNonBattleAction(node, "coins"))
                {
                    coins += amount;
                    FinishNonBattleNode();
                }
            }
            string recruitTitle;
            string recruitDescription;
            SplitEventFamilyOptionCopy(T("node.treasure.recruit"), out recruitTitle, out recruitDescription);
            if (DrawEventFamilyOption(1, "treasure", recruitTitle, recruitDescription,
                    "ART-UI-PREPARATION-ICON-REFRESH-V6.7.0", T("upgrade.choose"), true))
            {
                if (TryChooseNonBattleAction(node, "recruit"))
                {
                    expeditionEconomy.AddRerollVoucher(ref coins);
                    expeditionEconomy.SetRecruitmentDiscount(
                        ChapterOneExpeditionEconomy.ProgressBand(node.JourneyProgress), true);
                    FinishNonBattleNode();
                }
            }

            var itemCount = ChapterOneExpeditionEconomy.ProgressBand(node.JourneyProgress) <= 1 ? 2 : 3;
            var itemIds = ChapterOneExpeditionEconomy.GenerateCombatItemIds(
                expeditionMapSeed, node.Id, node.JourneyProgress, itemCount, "treasure");
            if (eventFamilyTacticalNodeId != node.Id)
            {
                eventFamilyTacticalNodeId = node.Id;
                eventFamilyTacticalSelection = 0;
            }
            eventFamilyTacticalSelection = Mathf.Clamp(eventFamilyTacticalSelection, 0, itemIds.Length - 1);
            var selectedItemId = itemIds[eventFamilyTacticalSelection];
            var canTakeItem = expeditionEconomy.Backpack.Count < ExpeditionEconomyState.BackpackCapacity;
            if (DrawEventFamilyOption(2, "treasure", T("economy.item." + selectedItemId),
                    T("economy.item." + selectedItemId + ".description"),
                    "ART-UI-PREPARATION-ICON-PROFESSIONOUTPUT-V6.7.0",
                    T("upgrade.choose"), canTakeItem))
            {
                if (TryChooseNonBattleAction(node, "treasure:" + selectedItemId))
                {
                    expeditionEconomy.TryAddCombatItem(selectedItemId);
                    FinishNonBattleNode();
                }
            }
            DrawEventFamilyTacticalSelector(2, itemIds.Length);
            DrawEventFamilyFooterHint(canTakeItem ? T(statusKey) : T("status.backpackFull"));
        }

        private void DrawEventFamilyShopChoices(ExpeditionMapNode node)
        {
            var generatedOffers = ChapterOneExpeditionEconomy.GenerateShopItemIds(
                expeditionMapSeed, node.Id, node.JourneyProgress, expeditionEconomy.Morale,
                expeditionEconomy.HasResolvedAction("run:shop:I06"));
            string lockedOffers;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "shop:" + node.Id, string.Join(",", generatedOffers), out lockedOffers)) SaveGame();
            var offers = lockedOffers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < ExpeditionEventFamilyV126Layout.OptionCards.Length; index++)
            {
                if (index >= offers.Length) continue;
                var item = ChapterOneExpeditionEconomy.GetItem(offers[index]);
                var actionId = item.Id == "I06" ? "run:shop:I06" : node.Id + ":shop:" + item.Id;
                var purchased = expeditionEconomy.HasResolvedAction(actionId);
                var enabled = !purchased && coins >= item.Cost;
                var description = T("economy.item." + item.Id + ".description") +
                                  "\n" + T("price") + " " + item.Cost;
                if (DrawEventFamilyOption(index, "shop", T("economy.item." + item.Id),
                        description, EventFamilyShopIcon(item.Id),
                        purchased ? T("eventFamily.sold") : T("eventFamily.buy"), enabled))
                    PurchaseExpeditionItem(node, item, actionId);
            }
            if (DrawEventFamilyPrimaryButton(
                    new Rect(321.5f, 1772f, 198f, 76f), T("node.continue"), true))
                FinishNonBattleNode();
        }

        private bool DrawEventFamilyOption(int index, string theme, string title,
            string description, string foregroundIconResource, string buttonLabel, bool enabled)
        {
            var card = ExpeditionEventFamilyV126Layout.OptionCards[index];
            var cardTexture = artCatalog.GetExpeditionEventFamilyTexture(
                ExpeditionEventFamilyV126Layout.OptionCardResource);
            DrawStandaloneNineSlice(cardTexture, card, ExpeditionEventFamilyV126Layout.OptionCardBorder);
            DrawEventFamilyTexture(OffsetRect(card, ExpeditionEventFamilyV126Layout.IconPlateLocal),
                ExpeditionEventFamilyV126Layout.IconPlateResource, ScaleMode.ScaleToFit);

            var previousColor = GUI.color;
            GUI.color = Color.white;
            DrawEventFamilyTexture(OffsetRect(card, ExpeditionEventFamilyV126Layout.PatternLocal),
                ExpeditionEventFamilyV126Layout.PatternResource(theme), ScaleMode.ScaleToFit);
            GUI.color = previousColor;
            DrawV67Texture(OffsetRect(card, ExpeditionEventFamilyV126Layout.ForegroundIconLocal),
                foregroundIconResource, ScaleMode.ScaleToFit);

            var copy = OffsetRect(card, ExpeditionEventFamilyV126Layout.CopyGroupLocal);
            DrawEventFamilyOptionCopy(copy, title, description);

            var buttonArea = OffsetRect(card, ExpeditionEventFamilyV126Layout.PrimaryButtonLocal);
            return theme == "shop"
                ? DrawEventFamilyRecruitButton(buttonArea, buttonLabel, enabled)
                : DrawEventFamilyPrimaryButton(buttonArea, buttonLabel, enabled);
        }

        private void DrawEventFamilyOptionCopy(Rect copy, string title, string description)
        {
            var titleFontSize = language == GameLanguage.English ? 18 : 28;
            var descriptionFontSize = language == GameLanguage.English ? 12 : 18;
            var minimumTitleFontSize = language == GameLanguage.English ? 14 : 20;
            var minimumDescriptionFontSize = language == GameLanguage.English ? 10 : 14;
            const float gap = 8f;
            GUIStyle titleStyle;
            GUIStyle descriptionStyle;
            float titleHeight;
            float descriptionHeight;

            while (true)
            {
                titleStyle = ArchiveStyle(titleFontSize, ArchiveInk(), TextAnchor.MiddleCenter, true);
                titleStyle.wordWrap = true;
                titleStyle.clipping = TextClipping.Clip;
                descriptionStyle = ArchiveStyle(descriptionFontSize, ArchiveMutedInk(),
                    TextAnchor.MiddleCenter);
                descriptionStyle.wordWrap = true;
                descriptionStyle.clipping = TextClipping.Clip;
                titleHeight = Mathf.Ceil(titleStyle.CalcHeight(new GUIContent(title), copy.width));
                descriptionHeight = Mathf.Ceil(
                    descriptionStyle.CalcHeight(new GUIContent(description), copy.width));
                if (titleHeight + gap + descriptionHeight <= copy.height ||
                    (titleFontSize <= minimumTitleFontSize &&
                     descriptionFontSize <= minimumDescriptionFontSize)) break;
                if (titleFontSize > minimumTitleFontSize) titleFontSize--;
                if (descriptionFontSize > minimumDescriptionFontSize) descriptionFontSize--;
            }

            var totalHeight = Mathf.Min(copy.height, titleHeight + gap + descriptionHeight);
            var startY = copy.center.y - totalHeight * 0.5f;
            GUI.Label(new Rect(copy.x, startY, copy.width, titleHeight), title, titleStyle);
            GUI.Label(new Rect(copy.x, startY + titleHeight + gap, copy.width,
                Mathf.Max(0f, totalHeight - titleHeight - gap)), description, descriptionStyle);
        }

        private bool DrawEventFamilyPrimaryButton(Rect area, string label, bool enabled)
        {
            DrawEventFamilyTexture(area, ExpeditionEventFamilyV126Layout.PrimaryButtonResource,
                ScaleMode.ScaleToFit);
            GUI.Label(area, label,
                ArchiveStyle(language == GameLanguage.English ? 15 : 25,
                    enabled ? ArchiveColor("F4F0DD") : ArchiveMutedInk(),
                    TextAnchor.MiddleCenter, true));
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked;
        }

        private bool DrawEventFamilyRecruitButton(Rect area, string label, bool enabled)
        {
            DrawV67HorizontalThreeSlice(area,
                "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            GUI.Label(area, label,
                ArchiveStyle(language == GameLanguage.English ? 15 : 25,
                    enabled ? ArchiveColor("F4F0DD") : ArchiveColor("68747A"),
                    TextAnchor.MiddleCenter, true));
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked;
        }

        private void DrawEventFamilyTacticalSelector(int cardIndex, int itemCount)
        {
            if (itemCount <= 1) return;
            var card = ExpeditionEventFamilyV126Layout.OptionCards[cardIndex];
            var previous = new Rect(card.x + 230f, card.y + 292f, 42f, 34f);
            var next = new Rect(card.x + 470f, card.y + 292f, 42f, 34f);
            if (GUI.Button(previous, "‹", ArchiveStyle(28, ArchiveInk(), TextAnchor.MiddleCenter, true)))
                eventFamilyTacticalSelection =
                    (eventFamilyTacticalSelection - 1 + itemCount) % itemCount;
            if (GUI.Button(next, "›", ArchiveStyle(28, ArchiveInk(), TextAnchor.MiddleCenter, true)))
                eventFamilyTacticalSelection = (eventFamilyTacticalSelection + 1) % itemCount;
        }

        private void DrawEventFamilyFooterHint(string hint)
        {
            GUI.Label(ExpeditionEventFamilyV126Layout.FooterHint, hint,
                ArchiveStyle(language == GameLanguage.English ? 15 : 22,
                    ArchiveMutedInk(), TextAnchor.MiddleCenter));
        }

        private void DrawEventFamilyTexture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetExpeditionEventFamilyTexture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private string ResolveEventFamilyEventId(ExpeditionMapNode node)
        {
            var generatedEventId = ChapterOneExpeditionEconomy.GenerateEventId(
                expeditionMapSeed, node.Id, node.JourneyProgress,
                expeditionEconomy.Morale, expeditionEconomy.Backpack.Count > 0);
            string eventId;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "event:" + node.Id, generatedEventId, out eventId)) SaveGame();
            return eventId;
        }

        private static string EventFamilyTheme(ExpeditionNodeType type)
        {
            if (EffectiveNodeType(type) == ExpeditionNodeType.Treasure) return "treasure";
            if (type == ExpeditionNodeType.Shop) return "shop";
            if (type == ExpeditionNodeType.Rest) return "rest";
            return "event";
        }

        private static string EventFamilyShopIcon(string itemId)
        {
            if (itemId == "I01" || itemId == "I04")
                return "ART-UI-PREPARATION-ICON-STATARMOR-V6.7.0";
            if (itemId == "I02") return "ART-UI-PREPARATION-ICON-STATSPEED-V6.7.0";
            if (itemId == "I03" || itemId == "I05")
                return "ART-UI-PREPARATION-ICON-STATATTACK-V6.7.0";
            if (itemId == "I06") return "ART-UI-PREPARATION-ICON-STATLIFE-V6.7.0";
            if (itemId == "I07") return "ART-UI-PREPARATION-ICON-COMPASS-V6.7.0";
            return "ART-UI-PREPARATION-ICON-COIN-V6.7.0";
        }

        private static void SplitEventFamilyOptionCopy(string label,
            out string title, out string description)
        {
            var value = label ?? string.Empty;
            var newline = value.IndexOf('\n');
            if (newline < 0)
            {
                title = value;
                description = string.Empty;
                return;
            }
            title = value.Substring(0, newline).Trim();
            description = value.Substring(newline + 1).Trim();
        }

        private void DrawExpeditionBackpackStrip(float y)
        {
            GUI.Label(new Rect(75f, y, 220f, 42f), T("economy.backpack"),
                Style(18, new Color(0.76f, 0.86f, 0.88f), TextAnchor.MiddleLeft));
            for (var index = 0; index < ExpeditionEconomyState.BackpackCapacity; index++)
            {
                var area = new Rect(300f + index * 225f, y, 205f, 48f);
                if (index >= expeditionEconomy.Backpack.Count)
                {
                    GUI.Label(area, T("empty"),
                        Style(15, new Color(0.48f, 0.58f, 0.60f), TextAnchor.MiddleCenter));
                    continue;
                }
                var itemId = expeditionEconomy.Backpack[index];
                if (GUI.Button(area, T("economy.item." + itemId) + "  ×",
                        ButtonStyle(UiButtonSemantic.Danger, 14)))
                {
                    DiscardPreparationBattleItem(index);
                    break;
                }
            }
        }

        private void DrawEventChoices(ExpeditionMapNode node, float nodeOffset)
        {
            var generatedEventId = ChapterOneExpeditionEconomy.GenerateEventId(
                expeditionMapSeed, node.Id, node.JourneyProgress,
                expeditionEconomy.Morale, expeditionEconomy.Backpack.Count > 0);
            string eventId;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "event:" + node.Id, generatedEventId, out eventId)) SaveGame();
            GUI.Label(new Rect(390f, 405f + nodeOffset, 300f, 45f), T("node.event." + eventId),
                Style(23, new Color(0.92f, 0.78f, 0.38f), TextAnchor.MiddleCenter));
            if (ChoiceButton(0, nodeOffset, T("node.event." + eventId + ".a")))
                ResolveEventChoice(node, eventId, true);
            if (ChoiceButton(1, nodeOffset, T("node.event." + eventId + ".b")))
                ResolveEventChoice(node, eventId, false);
            if (ChoiceButton(2, nodeOffset, T("node.event.leave")))
            {
                if (TryChooseNonBattleAction(node, eventId + ":leave")) FinishNonBattleNode();
            }
        }

        private void ResolveEventChoice(ExpeditionMapNode node, string eventId, bool firstChoice)
        {
            var actionId = eventId + (firstChoice ? ":a" : ":b");
            var cost = 0;
            if (eventId == "V01" && !firstChoice) cost = 2;
            if (eventId == "V02" && firstChoice) cost = 2;
            if (eventId == "V03" && firstChoice) cost = 2;
            if (eventId == "V05" && !firstChoice) cost = 2;
            if (eventId == "V06") cost = 3;
            if (eventId == "V07") cost = firstChoice ? 4 : 2;
            if (coins < cost)
            {
                statusKey = "status.coins";
                return;
            }

            var grantsCombatItem = (eventId == "V01" && !firstChoice) ||
                                   (eventId == "V05" && !firstChoice) ||
                                   (eventId == "V07" && !firstChoice);
            if (grantsCombatItem && expeditionEconomy.Backpack.Count >= ExpeditionEconomyState.BackpackCapacity)
            {
                statusKey = "status.backpackFull";
                return;
            }
            if (eventId == "V05" && firstChoice && expeditionEconomy.Backpack.Count == 0)
            {
                statusKey = "status.backpackEmpty";
                return;
            }
            if (!TryChooseNonBattleAction(node, actionId)) return;
            coins -= cost;

            switch (eventId)
            {
                case "V01":
                    if (firstChoice) coins += ChapterOneExpeditionEconomy.SupplyCoinAmount(node.JourneyProgress);
                    else expeditionEconomy.TryAddCombatItem("I01");
                    break;
                case "V02":
                    if (firstChoice) expeditionEconomy.SetNextBattlePreparation("scout2");
                    else coins += 2;
                    break;
                case "V03":
                    if (firstChoice) expeditionEconomy.SetRecruitmentDiscount(3, false);
                    else expeditionEconomy.AddRerollVoucher(ref coins);
                    break;
                case "V04":
                    if (firstChoice)
                    {
                        expeditionEconomy.AddMorale(-1);
                        coins += 8;
                    }
                    else expeditionEconomy.SetNextBattlePreparation("scout1");
                    break;
                case "V05":
                    if (firstChoice)
                    {
                        expeditionEconomy.TryDiscardFirstCombatItem();
                        coins += 7;
                    }
                    else expeditionEconomy.TryAddCombatItem("I02");
                    break;
                case "V06":
                    expeditionEconomy.SetNextBattlePreparation(firstChoice ? "fortify10" : "action8");
                    break;
                case "V07":
                    if (firstChoice) expeditionEconomy.AddMorale(1);
                    else expeditionEconomy.TryAddCombatItem("I01");
                    break;
                case "V08":
                    if (firstChoice) expeditionEconomy.SetNextBattlePreparation("scoutBoss");
                    else coins += 3;
                    break;
            }
            FinishNonBattleNode();
        }

        private void DrawExpeditionItemShop(ExpeditionMapNode node, float nodeOffset)
        {
            GUI.Label(new Rect(420f, 385f + nodeOffset, 240f, 56f), T("coins") + "  " + coins,
                Style(24, new Color(1f, 0.82f, 0.30f), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(85f, 390f + nodeOffset, 320f, 48f), T("node.shop.prompt"),
                Style(21, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            var generatedOffers = ChapterOneExpeditionEconomy.GenerateShopItemIds(
                expeditionMapSeed, node.Id, node.JourneyProgress, expeditionEconomy.Morale,
                expeditionEconomy.HasResolvedAction("run:shop:I06"));
            string lockedOffers;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "shop:" + node.Id, string.Join(",", generatedOffers), out lockedOffers)) SaveGame();
            var offers = lockedOffers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (var index = 0; index < offers.Length; index++)
            {
                var item = ChapterOneExpeditionEconomy.GetItem(offers[index]);
                var actionId = item.Id == "I06" ? "run:shop:I06" : node.Id + ":shop:" + item.Id;
                var purchased = expeditionEconomy.HasResolvedAction(actionId);
                var area = new Rect(65f + index * 250f, 475f + nodeOffset, 220f, 360f);
                DrawFloatingCardSurface(area, new Color(0.32f, 0.70f, 0.68f), 0.56f);
                GUI.Label(new Rect(area.x + 12f, area.y + 34f, area.width - 24f, 44f),
                    T("economy.item." + item.Id), Style(22, Color.white, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(area.x + 15f, area.y + 95f, area.width - 30f, 125f),
                    T("economy.item." + item.Id + ".description"),
                    Style(15, new Color(0.76f, 0.86f, 0.88f), TextAnchor.MiddleCenter));
                GUI.Label(new Rect(area.x + 12f, area.y + 235f, area.width - 24f, 36f),
                    purchased ? T("shop.sold") : T("price") + " " + item.Cost,
                    Style(18, new Color(0.96f, 0.78f, 0.28f), TextAnchor.MiddleCenter));
                GUI.enabled = !purchased && coins >= item.Cost;
                if (GUI.Button(new Rect(area.x + 15f, area.yMax - 65f, area.width - 30f, 48f),
                        T("shop.buy"), ButtonStyle(UiButtonSemantic.Reward, 17)))
                    PurchaseExpeditionItem(node, item, actionId);
                GUI.enabled = true;
            }
            GUI.Label(new Rect(120f, 910f + nodeOffset, 840f, 54f), T(statusKey),
                Style(17, new Color(0.78f, 0.87f, 0.88f), TextAnchor.MiddleCenter));
            if (GUI.Button(new Rect(140f, 980f + nodeOffset, 800f, 82f), T("node.continue"),
                    ButtonStyle(UiButtonSemantic.Primary, 22))) FinishNonBattleNode();
        }

        private void PurchaseExpeditionItem(
            ExpeditionMapNode node, ExpeditionItemDefinition item, string actionId)
        {
            if (item == null || coins < item.Cost || expeditionEconomy.HasResolvedAction(actionId)) return;
            if (item.Kind == ExpeditionItemKind.Combat && !expeditionEconomy.TryAddCombatItem(item.Id))
            {
                statusKey = "status.backpackFull";
                return;
            }
            if (!expeditionEconomy.TryResolveAction(actionId)) return;
            coins -= item.Cost;
            if (item.Id == "I06") expeditionEconomy.AddMorale(1);
            if (item.Id == "I07") expeditionEconomy.SetNextBattlePreparation("scout1");
            if (item.Id == "I08") expeditionEconomy.SetRecruitmentDiscount(3, false);
            statusKey = "status.itemPurchased";
            SaveGame();
        }

        private void DrawSupplyChoices(ExpeditionMapNode node, float nodeOffset)
        {
            var coinAmount = ChapterOneExpeditionEconomy.SupplyCoinAmount(node.JourneyProgress);
            var recruitCoinAmount = ChapterOneExpeditionEconomy.SupplyRecruitCoinAmount(node.JourneyProgress);
            if (ChoiceButton(0, nodeOffset, T("node.supply.coins") + "\n+" + coinAmount + " " + T("coins")))
            {
                if (TryChooseNonBattleAction(node, "coins"))
                {
                    coins += coinAmount;
                    FinishNonBattleNode();
                }
            }
            if (ChoiceButton(1, nodeOffset, T("node.supply.recruits") + "\n+" + recruitCoinAmount + " " +
                    T("coins") + " / " + T("shop.refresh") + " ×1"))
            {
                if (TryChooseNonBattleAction(node, "recruits"))
                {
                    coins += recruitCoinAmount;
                    expeditionEconomy.AddRerollVoucher(ref coins);
                    FinishNonBattleNode();
                }
            }
            var itemCount = ChapterOneExpeditionEconomy.ProgressBand(node.JourneyProgress) >= 4 ? 3 : 2;
            DrawTacticalCandidateCard(node, nodeOffset, "supply", T("node.supply.tactical"),
                ChapterOneExpeditionEconomy.GenerateCombatItemIds(
                    expeditionMapSeed, node.Id, node.JourneyProgress, itemCount, "supply"));
        }

        private void DrawRestChoices(ExpeditionMapNode node, float nodeOffset)
        {
            var first = expeditionEconomy.Morale < 2 ? "node.rest.morale" : "node.rest.fortify";
            if (ChoiceButton(0, nodeOffset, T(first)))
            {
                if (TryChooseNonBattleAction(node, "rest"))
                {
                    if (expeditionEconomy.Morale < 2) expeditionEconomy.AddMorale(1);
                    else expeditionEconomy.SetNextBattlePreparation("fortify8");
                }
                FinishNonBattleNode();
            }
            if (ChoiceButton(1, nodeOffset, T("node.rest.scout")))
            {
                if (TryChooseNonBattleAction(node, "scout")) expeditionEconomy.SetNextBattlePreparation("formationShield5");
                FinishNonBattleNode();
            }
            if (ChoiceButton(2, nodeOffset, T("node.rest.drill")))
            {
                if (TryChooseNonBattleAction(node, "drill")) expeditionEconomy.SetNextBattlePreparation("action8");
                FinishNonBattleNode();
            }
        }

        private void DrawTreasureChoices(ExpeditionMapNode node, float nodeOffset)
        {
            var amount = ChapterOneExpeditionEconomy.TreasureCoinAmount(node.JourneyProgress);
            if (ChoiceButton(0, nodeOffset, T("node.treasure.coins") + "\n+" + amount + " " + T("coins")))
            {
                if (TryChooseNonBattleAction(node, "coins"))
                {
                    coins += amount;
                    FinishNonBattleNode();
                }
            }
            if (ChoiceButton(1, nodeOffset, T("node.treasure.recruit")))
            {
                if (TryChooseNonBattleAction(node, "recruit"))
                {
                    expeditionEconomy.AddRerollVoucher(ref coins);
                    expeditionEconomy.SetRecruitmentDiscount(
                        ChapterOneExpeditionEconomy.ProgressBand(node.JourneyProgress), true);
                    FinishNonBattleNode();
                }
            }
            var itemCount = ChapterOneExpeditionEconomy.ProgressBand(node.JourneyProgress) <= 1 ? 2 : 3;
            DrawTacticalCandidateCard(node, nodeOffset, "treasure", T("node.treasure.tactical"),
                ChapterOneExpeditionEconomy.GenerateCombatItemIds(
                    expeditionMapSeed, node.Id, node.JourneyProgress, itemCount, "treasure"));
        }

        private void DrawTacticalCandidateCard(
            ExpeditionMapNode node, float offset, string choicePrefix, string title, string[] itemIds)
        {
            var area = new Rect(735f, 500f + offset, 300f, 260f);
            DrawFloatingCardSurface(area, new Color(0.36f, 0.63f, 0.66f), 0.48f);
            GUI.Label(new Rect(area.x + 12f, area.y + 18f, area.width - 24f, 58f), title,
                Style(19, Color.white, TextAnchor.MiddleCenter));
            var height = itemIds.Length > 2 ? 48f : 58f;
            for (var index = 0; index < itemIds.Length; index++)
            {
                var itemId = itemIds[index];
                GUI.enabled = expeditionEconomy.Backpack.Count < ExpeditionEconomyState.BackpackCapacity;
                if (GUI.Button(new Rect(area.x + 28f, area.y + 86f + index * (height + 10f),
                        area.width - 56f, height), T("economy.item." + itemId),
                        ButtonStyle(UiButtonSemantic.Secondary, 15)))
                {
                    if (TryChooseNonBattleAction(node, choicePrefix + ":" + itemId))
                    {
                        expeditionEconomy.TryAddCombatItem(itemId);
                        FinishNonBattleNode();
                    }
                }
                GUI.enabled = true;
            }
            if (expeditionEconomy.Backpack.Count >= ExpeditionEconomyState.BackpackCapacity)
                GUI.Label(new Rect(area.x + 20f, area.yMax - 42f, area.width - 40f, 32f),
                    T("status.backpackFull"),
                    Style(13, new Color(0.96f, 0.66f, 0.58f), TextAnchor.MiddleCenter));
        }

        private bool ChoiceButton(int index, float offset, string label)
        {
            var area = new Rect(75f + index * 330f, 500f + offset, 300f, 260f);
            return GUI.Button(area, label, ButtonStyle(
                index == 0 ? UiButtonSemantic.Primary :
                index == 1 ? UiButtonSemantic.Reward : UiButtonSemantic.Secondary, 20));
        }

        private bool TryChooseNonBattleAction(ExpeditionMapNode node, string choiceId)
        {
            return node != null && expeditionEconomy.TryResolveAction(node.Id + ":choice:" + choiceId);
        }

        private static string NodeActionKey(ExpeditionNodeType type)
        {
            switch (EffectiveNodeType(type))
            {
                case ExpeditionNodeType.Shop: return "shop";
                case ExpeditionNodeType.Supply: return "supply";
                case ExpeditionNodeType.Rest: return "rest";
                case ExpeditionNodeType.Treasure: return "treasure";
                case ExpeditionNodeType.Event: return "event";
                default: return "treasure";
            }
        }

        private void FinishNonBattleNode()
        {
            if (!expeditionFlow.TryFinishNode()) return;
            ExpeditionProgression.TryCompleteNode(
                expeditionRewards, currentMapNodeId, 0, 0, 0, 0);
            confirmAbandon = false;
            statusKey = "status.route";
            SaveGame();
        }

        private void DrawChapterMapNode(
            Rect area,
            ExpeditionMapNode node,
            IReadOnlyList<ExpeditionMapNode> available)
        {
            var isCurrent = node.Id == currentMapNodeId;
            var isAvailable = ContainsNode(available, node.Id);
            var isCompleted = expeditionRewards.HasCompletedNode(node.Id);
            var activeNode = expeditionMap.GetNode(currentMapNodeId);
            var isPast = activeNode != null &&
                          node.JourneyProgress < activeNode.JourneyProgress;
            var color = MapNodeColor(node.Type);
            var opacity = isCurrent || isAvailable ? 1f : isCompleted ? 0.72f : isPast ? 0.28f : 0.58f;
            if (node.Layer == round + 1 && !isAvailable) opacity = 0.32f;

            var visualState = isCurrent ? ExpeditionMapNodeVisualState.Current
                : isAvailable ? ExpeditionMapNodeVisualState.Available
                : isCompleted ? ExpeditionMapNodeVisualState.Completed
                : ExpeditionMapNodeVisualState.Future;
            Rect artLabelArea;
            var artDrawn = expeditionMapRenderer.DrawNode(
                area, expeditionMap.Seed, node.Id, node.Type, visualState, opacity,
                out artLabelArea);
            if (!artDrawn)
            {
                GUI.color = WithOpacity(color, opacity);
                GUI.DrawTexture(area, Texture2D.whiteTexture);
                if (isCurrent || isAvailable)
                {
                    var border = isCurrent ? new Color(1f, 0.82f, 0.25f) : new Color(0.29f, 0.94f, 0.81f);
                    GUI.color = border;
                    GUI.DrawTexture(new Rect(area.x, area.y, area.width, 5f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(area.x, area.yMax - 5f, area.width, 5f), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(area.x, area.y, 5f, area.height), Texture2D.whiteTexture);
                    GUI.DrawTexture(new Rect(area.xMax - 5f, area.y, 5f, area.height), Texture2D.whiteTexture);
                }
            }
            GUI.color = Color.white;
            var textColor = artDrawn ? new Color(38f / 255f, 59f / 255f, 64f / 255f) : Color.white;
            var titleArea = artDrawn
                ? artLabelArea
                : new Rect(area.x + 8f, area.y + 5f, area.width - 16f, 38f);
            GUI.Label(titleArea, T(MapNodeTypeKey(node.Type)),
                Style(artDrawn ? 20 : 17, WithOpacity(textColor, opacity),
                    artDrawn ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter, FontStyle.Normal));
            var stateKey = isCurrent ? "map.current" : isAvailable ? "map.select" : "map.future";
            if (!artDrawn)
            {
                GUI.Label(new Rect(area.x + 8f, area.y + 38f, area.width - 16f, 30f), T(stateKey),
                    Style(13, WithOpacity(Color.white, opacity), TextAnchor.MiddleCenter));
            }

            if (isAvailable && !expeditionMapPointerDragging &&
                GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                SelectMapNode(node.Id);
            }
        }

        private void DrawFixedChapterMapNode(
            Vector2 labelAnchor,
            float mapScale,
            ExpeditionMapNode node,
            IReadOnlyList<ExpeditionMapNode> available)
        {
            var isCurrent = node.Id == currentMapNodeId;
            var isAvailable = ContainsNode(available, node.Id);
            var isCompleted = expeditionRewards.HasCompletedNode(node.Id);
            var activeNode = expeditionMap.GetNode(currentMapNodeId);
            var isPast = activeNode != null &&
                          node.JourneyProgress < activeNode.JourneyProgress;
            var opacity = isCurrent || isAvailable ? 1f : isCompleted ? 0.82f : isPast ? 0.42f : 0.82f;
            if (node.Layer == round + 1 && !isAvailable) opacity = 0.68f;
            DrawMapV63NodeBadge(labelAnchor, node.Type, mapScale, opacity);

            var labelKey = node.AnchorIndex == 0
                ? "map.start"
                : MapNodeCompactLabelKey(node.Type);
            var textArea = new Rect(labelAnchor.x - 12f * mapScale,
                labelAnchor.y - 16f * mapScale, 132f * mapScale, 32f * mapScale);
            GUI.Label(textArea, T(labelKey), Style(
                Mathf.RoundToInt(16f * mapScale),
                WithOpacity(new Color(23f / 255f, 42f / 255f, 46f / 255f), opacity),
                TextAnchor.MiddleLeft, FontStyle.Normal));

            var touchArea = new Rect(labelAnchor.x - 92f * mapScale,
                labelAnchor.y - 135f * mapScale, 184f * mapScale, 165f * mapScale);
            if (isAvailable && !expeditionMapPointerDragging &&
                GUI.Button(touchArea, GUIContent.none, GUIStyle.none))
            {
                SelectMapNode(node.Id);
            }
        }

        private void DrawMapV63NodeBadge(
            Vector2 labelAnchor, ExpeditionNodeType type, float mapScale, float opacity)
        {
            var atlas = artCatalog.GetExpeditionUiV63Texture("map-a-node-badges-runtime-v6.3");
            var index = MapV63NodeBadgeIndex(type);
            var size = 32f * mapScale;
            var area = new Rect(labelAnchor.x - 54f * mapScale,
                labelAnchor.y - size * 0.5f, size, size);
            DrawAtlasSliceWithOpacity(atlas, area,
                new Rect(8f + index * 136f, 8f, 128f, 128f), opacity);
        }

        private static int MapV63NodeBadgeIndex(ExpeditionNodeType type)
        {
            switch (type)
            {
                case ExpeditionNodeType.EliteBattle: return 1;
                case ExpeditionNodeType.Event: return 2;
                case ExpeditionNodeType.Rest: return 3;
                case ExpeditionNodeType.Shop: return 4;
                case ExpeditionNodeType.Treasure:
                case ExpeditionNodeType.Supply: return 5;
                case ExpeditionNodeType.Boss: return 6;
                default: return 0;
            }
        }

        private static void DrawAtlasSliceWithOpacity(
            Texture2D atlas, Rect destination, Rect sourceTopLeft, float opacity)
        {
            if (atlas == null || destination.width <= 0f || destination.height <= 0f) return;
            var uv = new Rect(sourceTopLeft.x / atlas.width,
                1f - (sourceTopLeft.y + sourceTopLeft.height) / atlas.height,
                sourceTopLeft.width / atlas.width, sourceTopLeft.height / atlas.height);
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            GUI.DrawTextureWithTexCoords(destination, atlas, uv, true);
            GUI.color = Color.white;
        }

        private static Rect MapNodeRect(
            ExpeditionMapNode node, int layerNodeCount, float width, float height,
            float mapLeft, float layerStep, float mapContentHeight, uint mapSeed)
        {
            var count = Mathf.Clamp(layerNodeCount, 1, ExpeditionMapGraph.MaximumNodesPerLayer);
            var x = mapLeft + (node.Layer - 1) * layerStep +
                ExpeditionMapColumnLayout.HorizontalOffset(
                    mapSeed, node.Layer, count, Mathf.Clamp(node.Column, 0, count - 1));
            var centerY = ExpeditionMapColumnLayout.CenterY(
                mapSeed, node.Layer, count, Mathf.Clamp(node.Column, 0, count - 1),
                mapContentHeight, height, 20f, 20f);
            var y = Mathf.Clamp(centerY - height * 0.5f, 20f, mapContentHeight - height - 20f);
            return new Rect(x, y, width, height);
        }

        private void DrawHiddenMapMilestone(
            float mapLeft, float layerStep, float nodeWidth, int revealThroughLayer,
            int milestoneLayer, string localizationKey)
        {
            if (milestoneLayer <= revealThroughLayer) return;
            var x = mapLeft + (milestoneLayer - 1) * layerStep;
            GUI.Label(new Rect(x, 565f, nodeWidth, 60f), T(localizationKey),
                Style(15, new Color(0.31f, 0.24f, 0.29f), TextAnchor.MiddleCenter, FontStyle.Normal));
        }

        private static Vector2 MapNodePort(
            ExpeditionMapNode node, int layerNodeCount, float width, float height,
            float mapLeft, float layerStep, float mapContentHeight, uint mapSeed, bool outgoing)
        {
            var area = MapNodeRect(node, layerNodeCount, width, height,
                mapLeft, layerStep, mapContentHeight, mapSeed);
            return new Vector2(outgoing ? area.xMax : area.xMin, area.center.y);
        }

        private static void DrawMapLine(
            Vector2 from, Vector2 to, Color color, float thickness, bool solid)
        {
            GUI.color = color;
            var directDistance = Vector2.Distance(from, to);
            var spacing = solid ? Mathf.Max(1f, thickness * 0.42f) : thickness >= 7f ? 14f : 18f;
            var segmentCount = Mathf.Clamp(
                Mathf.CeilToInt(directDistance / spacing), 2, solid ? 512 : 96);
            var dotSize = solid ? Mathf.Max(4f, thickness * 1.08f) : Mathf.Max(4f, thickness);
            for (var segment = 0; segment <= segmentCount; segment++)
            {
                // Keep every route on the direct line between its two approved E27
                // ports. Axis-aligned dots avoid RotateAroundPivot distortion inside
                // the scaled Android ScrollView while still reading as one straight route.
                var point = Vector2.Lerp(from, to, segment / (float)segmentCount);
                // Axis-aligned route marks avoid RotateAroundPivot. That API composes
                // poorly with the SafeArea scale and ScrollView clip on tall Android
                // screens, which previously folded diagonal links into vertical bars.
                GUI.DrawTexture(new Rect(point.x - dotSize * 0.5f, point.y - dotSize * 0.5f,
                    dotSize, dotSize), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
        }

        private static bool ContainsNode(IReadOnlyList<ExpeditionMapNode> nodes, string id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return true;
            }
            return false;
        }

        private static string MapNodeTypeKey(ExpeditionNodeType type)
        {
            switch (EffectiveNodeType(type))
            {
                case ExpeditionNodeType.EliteBattle: return "node.elite";
                case ExpeditionNodeType.Supply: return "node.supply";
                case ExpeditionNodeType.Boss: return "node.boss";
                case ExpeditionNodeType.Event: return "node.event";
                case ExpeditionNodeType.Shop: return "node.shop";
                case ExpeditionNodeType.Rest: return "node.rest";
                case ExpeditionNodeType.Treasure: return "node.treasure";
                case ExpeditionNodeType.HighRiskBattle: return "node.highRisk";
                default: return "node.normal";
            }
        }

        private static string MapNodeCompactLabelKey(ExpeditionNodeType type)
        {
            switch (type)
            {
                case ExpeditionNodeType.EliteBattle:
                case ExpeditionNodeType.HighRiskBattle: return "map.node.elite";
                case ExpeditionNodeType.Event: return "map.node.event";
                case ExpeditionNodeType.Rest: return "map.node.rest";
                case ExpeditionNodeType.Shop: return "map.node.shop";
                case ExpeditionNodeType.Supply:
                case ExpeditionNodeType.Treasure: return "map.node.treasure";
                case ExpeditionNodeType.Boss: return "map.node.boss";
                default: return "map.node.normal";
            }
        }

        private static Color MapNodeColor(ExpeditionNodeType type)
        {
            switch (EffectiveNodeType(type))
            {
                case ExpeditionNodeType.EliteBattle: return new Color(0.70f, 0.20f, 0.24f);
                case ExpeditionNodeType.Supply: return new Color(0.65f, 0.45f, 0.15f);
                case ExpeditionNodeType.Boss: return new Color(0.58f, 0.16f, 0.42f);
                case ExpeditionNodeType.Event: return new Color(0.34f, 0.30f, 0.66f);
                case ExpeditionNodeType.Shop: return new Color(0.15f, 0.47f, 0.66f);
                case ExpeditionNodeType.Rest: return new Color(0.18f, 0.55f, 0.36f);
                case ExpeditionNodeType.Treasure: return new Color(0.69f, 0.49f, 0.12f);
                case ExpeditionNodeType.HighRiskBattle: return new Color(0.78f, 0.26f, 0.16f);
                default: return new Color(0.18f, 0.48f, 0.44f);
            }
        }

        private void DrawRunResult(float y)
        {
            var titleKey = runAbandoned ? "run.abandoned" : runWon ? "run.victory" : "run.defeat";
            var descriptionKey = runAbandoned
                ? "run.abandonedDescription"
                : runWon ? "run.victoryDescription" : "run.defeatDescription";
            var titleColor = runAbandoned
                ? new Color(0.92f, 0.68f, 0.28f)
                : runWon ? new Color(0.28f, 0.88f, 0.61f) : new Color(0.94f, 0.33f, 0.35f);
            GUI.Label(new Rect(55f, y, 970f, 58f), T(titleKey), Style(34, titleColor, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(70f, y + 58f, 940f, 58f), T(descriptionKey), Style(17, new Color(0.78f, 0.85f, 0.90f), TextAnchor.MiddleCenter));
            if (!runAbandoned)
            {
                GUI.Label(new Rect(70f, y + 120f, 940f, 54f), T("run.score") + "  " + finalScore, Style(25, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            }
            var rewards = expeditionRewards.CreateSnapshot();
            GUI.Label(new Rect(70f, y + 180f, 940f, 38f),
                T(currentExpeditionRewardsEnabled ? "run.localTreasure" : "run.freeNoRewards"),
                Style(21, new Color(1f, 0.78f, 0.25f), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(70f, y + 218f, 940f, 32f),
                T("warehouse.crystal") + " " + rewards.TideCrystals + "    " +
                T("material.stone") + " " + rewards.Stone + "    " +
                T("material.lampOil") + " " + rewards.LampOil + "    " +
                T("material.mistLens") + " " + rewards.MistLens,
                Style(15, new Color(0.82f, 0.88f, 0.90f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(70f, y + 252f, 940f, 30f),
                T("run.equipmentCount") + " " + rewards.EquipmentTemplateIds.Count,
                Style(15, new Color(0.82f, 0.88f, 0.90f), TextAnchor.MiddleCenter));
            var canStoreRewards = CanStoreExpeditionRewards(rewards);
            var actionKey = expeditionRewards.IsClaimed
                ? "run.restart"
                : canStoreRewards ? "run.claimAndReturn" : "run.warehouseFull";
            GUI.enabled = expeditionRewards.IsClaimed || canStoreRewards;
            if (GUI.Button(new Rect(55f, y + 300f, 970f, 100f), T(actionKey),
                    ButtonStyle(expeditionRewards.IsClaimed ? UiButtonSemantic.Primary : UiButtonSemantic.Reward, 24)))
            {
                if (expeditionRewards.IsClaimed)
                {
                    RestartRun();
                }
                else
                {
                    if (ClaimExpeditionRewards())
                    {
                        homeNoticeKey = "home.notice.treasureClaimed";
                        currentView = ScreenView.Home;
                    }
                }
            }
            GUI.enabled = true;
        }

        private void DrawRecruitButton(Rect area, int offerIndex, string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey))
            {
                ActiveUiSkin.DrawListRow(area, false);
                GUI.Label(area, T("shop.sold"), Style(17, PreparationTextMuted, TextAnchor.MiddleCenter));
                return;
            }
            var level = ShopOfferLevel(offerIndex);
            var cost = UnitCost(nameKey, level);
            var power = UnitBasePower(nameKey);
            var stats = characterCatalog.Get(nameKey).CalculateCombatStats(power, level);
            ActiveUiSkin.DrawListRow(area, false);
            DrawCharacterAvatar(nameKey, new Rect(area.x + 9f, area.y + 9f, 92f, 86f));
            GUI.Label(new Rect(area.x + 110f, area.y + 7f, area.width - 120f, 32f),
                T(nameKey) + "  Lv." + level,
                Style(18, PreparationTextPrimary, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(area.x + 108f, area.y + 38f, area.width - 116f, 22f),
                PrimaryStatsText(stats), Style(12, PreparationTextPrimary, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(area.x + 108f, area.y + 59f, area.width - 116f, 22f),
                SecondaryStatsText(stats), Style(12, PreparationTextPrimary, TextAnchor.MiddleCenter));
            var priceArea = new Rect(area.x + area.width - 158f, area.y + 84f, 148f, 44f);
            ActiveUiSkin.DrawPriceTag(priceArea,
                T("price") + " " + expeditionEconomy.QuoteRecruitmentPrice(cost), 15, GUI.enabled);

            // Match map-node interaction: the complete visual card is one target. The
            // price tag is display-only and no longer owns the purchase hit area.
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                Recruit(offerIndex, nameKey, cost, power, level);
            }
        }

        private void Recruit(int offerIndex, string nameKey, int listedCost, int power, int level)
        {
            var actionId = NewRunEconomyActionId("buy");
            var expectedVersion = runEconomyActions.StateVersion;
            var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                if (!CanRecruitNow()) return RunEconomyResultCode.InvalidPhase;
                if (offerIndex < 0 || offerIndex >= ShopOfferCount || shopOfferSold[offerIndex] ||
                    string.IsNullOrEmpty(shopOffers[offerIndex]) || shopOffers[offerIndex] != nameKey)
                    return RunEconomyResultCode.OfferNotFoundOrSold;

                var lockedListedPrice = shopOfferListedPrices[offerIndex] > 0
                    ? shopOfferListedPrices[offerIndex]
                    : listedCost;
                var payableCost = expeditionEconomy.QuoteRecruitmentPrice(lockedListedPrice);
                if (coins < payableCost) return RunEconomyResultCode.InsufficientCoins;
                var runUnitId = StableRunEconomyId("unit", runId, actionId, nameKey, level.ToString());
                var candidate = new RunUnitState(nameKey, power, level, payableCost, runUnitId);
                if (!runRoster.CanPurchase(candidate)) return RunEconomyResultCode.RosterFull;

                // No operation below this line can fail: the complete roster result was
                // preflighted before consuming a discount or any coins.
                var result = runRoster.TryPurchase(candidate);
                if (!result.Added) return RunEconomyResultCode.RosterFull;
                var actualPaid = expeditionEconomy.ApplyRecruitmentDiscount(lockedListedPrice);
                coins -= actualPaid;
                shopOfferSold[offerIndex] = true;
                shopOffers[offerIndex] = null;
                return RunEconomyResultCode.Success;
            });
            ApplyRunEconomyStatus(receipt.ResultCode, "status.recruited");
            SaveGame();
        }

        private bool CanRecruitNow()
        {
            return expeditionFlow.CanPrepare;
        }

        private void ApplyRunBonuses(UnitData unit)
        {
            // v26: legacy +1 attack/+5 health placeholder fields are intentionally inert.
            // Authoritative D1 bonuses are applied once at OnBattleStart by DeterministicBattle.
        }

        private UnitData CreateRunUnitView(RunUnitState runUnit)
        {
            var definition = characterCatalog.Get(runUnit.DefinitionId);
            EquipmentLoadoutSnapshot loadout;
            if (!runEquipmentSnapshots.TryGetValue(runUnit.DefinitionId, out loadout))
                loadout = new EquipmentLoadoutSnapshot(null, null);
            var stats = EquipmentDerivedStatCalculator.Calculate(
                definition, runUnit.BasePower, runUnit.RunLevel, loadout);
            var unit = UnitData.Create(
                definition,
                runUnit.BasePower,
                PlayerColor(runUnit.DefinitionId),
                runUnit.RunLevel,
                stats);
            ApplyRunBonuses(unit);
            ApplyFormationSynergies(unit, definition);
            ApplyNextBattlePreparation(unit);
            return unit;
        }

        private UnitData CreateRunUnitEffectiveView(RunUnitState runUnit)
        {
            var unit = CreateRunUnitView(runUnit);
            ApplyRoguelikePersistentStatPreview(unit);
            return unit;
        }

        private void ApplyRoguelikePersistentStatPreview(UnitData unit)
        {
            if (unit == null || roguelikeBuild == null) return;
            var definition = characterCatalog.Get(unit.NameKey);
            var effects = roguelikeBuild.AcquiredEffects;
            unit.AttackDamage = RoguelikeEnhancementRules.EffectiveAttackDamage(unit.AttackDamage, effects);
            unit.MaxHealth = RoguelikeEnhancementRules.EffectiveMaximumHealth(unit.MaxHealth, effects);
            unit.Armor = RoguelikeEnhancementRules.EffectiveArmor(
                unit.Armor, definition.FactionId, definition.ProfessionId, effects);
        }

        private void ApplyNextBattlePreparation(UnitData unit)
        {
            if (unit == null || expeditionEconomy == null) return;
            switch (expeditionEconomy.NextBattlePreparationId)
            {
                case "fortify8":
                    unit.MaxHealth = Mathf.CeilToInt(unit.MaxHealth * 1.08f);
                    break;
                case "fortify10":
                    unit.MaxHealth = Mathf.CeilToInt(unit.MaxHealth * 1.10f);
                    break;
                case "formationShield5":
                    unit.OpeningShield += Mathf.Max(1, Mathf.CeilToInt(unit.MaxHealth * 0.05f));
                    break;
                case "action8":
                    unit.AttackDamage = Mathf.Max(1, Mathf.CeilToInt(unit.AttackDamage * 1.08f));
                    unit.HealAmount = Mathf.CeilToInt(unit.HealAmount * 1.08f);
                    break;
            }
        }

        private void LockRunEquipmentSnapshots()
        {
            runEquipmentSnapshots.Clear();
            foreach (var definitionId in runDeck.UnitDefinitionIds)
            {
                if (!playerCollection.Owns(definitionId)) continue;
                OwnedCharacterState character;
                var characterInstanceId = runDeck.GetCharacterInstanceId(definitionId);
                if (!playerCollection.TryGetByInstanceId(characterInstanceId, out character))
                    character = playerCollection.Get(definitionId);
                var snapshot = equipmentInventory.CreateSnapshot(character.CharacterInstanceId);
                runEquipmentSnapshots[definitionId] = snapshot;
                foreach (var equipmentId in snapshot.EquipmentInstanceIds)
                {
                    equipmentInventory.TryAddUsageLock(equipmentId, EquipmentUsageLock.Expedition);
                }
            }
        }

        private void ReleaseRunEquipmentLocks()
        {
            foreach (var instance in equipmentInventory.Instances)
                equipmentInventory.TryRemoveUsageLock(instance.EquipmentInstanceId, EquipmentUsageLock.Expedition);
        }

        private void ApplyFormationSynergies(UnitData unit, CharacterDefinition definition)
        {
            var synergy = CurrentFormationSynergy();
            var healthPercent = synergy.GetMaximumHealthPercent(definition);
            var healPercent = synergy.GetHealPercent(definition);
            if (healthPercent > 0)
            {
                unit.MaxHealth = Mathf.CeilToInt(unit.MaxHealth * (100f + healthPercent) / 100f);
            }
            if (healPercent > 0)
            {
                unit.HealAmount = Mathf.RoundToInt(unit.HealAmount * (100f + healPercent) / 100f);
            }
            var shieldPercent = synergy.GetOpeningShieldPercent(definition);
            if (shieldPercent > 0)
            {
                unit.OpeningShield = Mathf.Max(1, unit.MaxHealth * shieldPercent / 100);
            }
        }

        private FormationSynergySnapshot CurrentFormationSynergy()
        {
            var deployed = new List<string>();
            for (var index = 0; index < runRoster.BoardCapacity; index++)
            {
                var runUnit = runRoster.Get(index);
                if (runUnit != null) deployed.Add(runUnit.DefinitionId);
            }
            return FormationSynergy.Calculate(characterCatalog, deployed);
        }

        private void MigrateLegacyRuntimeRoster()
        {
            if (runRoster.TotalCount > 0) return;
            var migrated = false;
            for (var i = 0; i < playerSlots.Length && i < runRoster.BoardCapacity; i++)
            {
                var unit = playerSlots[i];
                if (unit == null) continue;
                var level = unit.RunLevel <= 0 ? 1 : unit.RunLevel;
                var basePower = unit.BasePower <= 0 ? unit.Power : unit.BasePower;
                runRoster.TryRestoreAt(i, new RunUnitState(unit.NameKey, basePower, level));
                playerSlots[i] = null;
                migrated = true;
            }
            for (var i = 0; i < benchSlots.Length && i < runRoster.BenchCapacity; i++)
            {
                var unit = benchSlots[i];
                if (unit == null) continue;
                var level = unit.RunLevel <= 0 ? 1 : unit.RunLevel;
                var basePower = unit.BasePower <= 0 ? unit.Power : unit.BasePower;
                runRoster.TryRestoreAt(runRoster.BoardCapacity + i,
                    new RunUnitState(unit.NameKey, basePower, level));
                benchSlots[i] = null;
                migrated = true;
            }
            if (migrated) SaveGame();
        }

        private string DeckDisplayName()
        {
            return DeckDisplayName(runDeck.UnitDefinitionIds);
        }

        private string NextDeckDisplayName()
        {
            return DeckDisplayName(nextRunDeckSelection.SelectedIds);
        }

        private string DeckDisplayName(IReadOnlyList<string> ids)
        {
            var names = new string[ids.Count];
            for (var i = 0; i < names.Length; i++) names[i] = T(ids[i]);
            return string.Join(" / ", names);
        }

        private void RefreshAllShopOffers()
        {
            string[] offers;
            int[] levels;
            string[] offerIds;
            int[] listedPrices;
            bool[] sold;
            GenerateShopOfferSnapshot(shopRefreshSerial + 1, out offers, out levels, out offerIds,
                out listedPrices, out sold);
            shopRefreshSerial++;
            shopOffers = offers;
            shopOfferLevels = levels;
            shopOfferIds = offerIds;
            shopOfferListedPrices = listedPrices;
            shopOfferSold = sold;
        }

        private void GenerateShopOfferSnapshot(
            int refreshSerial,
            out string[] offers,
            out int[] levels,
            out string[] offerIds,
            out int[] listedPrices,
            out bool[] sold)
        {
            offers = new string[ShopOfferCount];
            levels = new int[ShopOfferCount];
            offerIds = new string[ShopOfferCount];
            listedPrices = new int[ShopOfferCount];
            sold = new bool[ShopOfferCount];
            var random = new System.Random(unchecked((int)expeditionMapSeed ^ round * 7919 ^ refreshSerial * 104729));
            var node = expeditionMap == null ? null : expeditionMap.GetNode(currentMapNodeId);
            var progress = node == null ? 0f : node.JourneyProgress;
            for (var i = 0; i < ShopOfferCount; i++)
            {
                offers[i] = runDeck.GetPrototypeOffer(random.Next(runDeck.UnitDefinitionIds.Count));
                levels[i] = ChapterOneExpeditionEconomy.RecruitOfferLevel(
                    expeditionMapSeed, currentMapNodeId, refreshSerial, i, progress);
                listedPrices[i] = UnitCost(offers[i], levels[i]);
                offerIds[i] = StableRunEconomyId("offer", runId, currentMapNodeId,
                    refreshSerial.ToString(), i.ToString());
            }
        }

        private void RefreshShopOffers(bool spendCoins)
        {
            if (!spendCoins)
            {
                RefreshAllShopOffers();
                statusKey = "status.shopRefreshed";
                SaveGame();
                return;
            }

            var actionId = NewRunEconomyActionId("refresh");
            var expectedVersion = runEconomyActions.StateVersion;
            var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                if (!CanRecruitNow()) return RunEconomyResultCode.InvalidPhase;
                var rerollCost = expeditionEconomy.PeekManualRerollCost();
                if (coins < rerollCost) return RunEconomyResultCode.InsufficientCoins;

                string[] offers;
                int[] levels;
                string[] offerIds;
                int[] listedPrices;
                bool[] sold;
                GenerateShopOfferSnapshot(shopRefreshSerial + 1, out offers, out levels, out offerIds,
                    out listedPrices, out sold);
                coins -= expeditionEconomy.ConsumeManualRerollCost();
                shopRefreshSerial++;
                shopOffers = offers;
                shopOfferLevels = levels;
                shopOfferIds = offerIds;
                shopOfferListedPrices = listedPrices;
                shopOfferSold = sold;
                return RunEconomyResultCode.Success;
            });
            ApplyRunEconomyStatus(receipt.ResultCode, "status.shopRefreshed");
            SaveGame();
        }

        private string NewRunEconomyActionId(string actionType)
        {
            return (actionType ?? "action") + "-" + Guid.NewGuid().ToString("N");
        }

        private static string StableRunEconomyId(string prefix, params string[] parts)
        {
            unchecked
            {
                uint hash = 2166136261;
                if (parts != null)
                {
                    foreach (var part in parts)
                    {
                        var value = part ?? string.Empty;
                        for (var i = 0; i < value.Length; i++)
                        {
                            hash ^= value[i];
                            hash *= 16777619;
                        }
                        hash ^= 0xFF;
                        hash *= 16777619;
                    }
                }
                return (prefix ?? "id") + "-" + hash.ToString("X8");
            }
        }

        private void ApplyRunEconomyStatus(RunEconomyResultCode result, string successStatusKey)
        {
            switch (result)
            {
                case RunEconomyResultCode.Success:
                    statusKey = successStatusKey;
                    break;
                case RunEconomyResultCode.InsufficientCoins:
                    statusKey = "status.coins";
                    break;
                case RunEconomyResultCode.RosterFull:
                    statusKey = "status.full";
                    break;
                case RunEconomyResultCode.MaximumRunLevel:
                    statusKey = "status.mergeMax";
                    break;
                case RunEconomyResultCode.MergeRequirementsNotMet:
                    statusKey = "status.mergeNeedThree";
                    break;
                default:
                    statusKey = "status.repaired";
                    break;
            }
        }

        private int ShopOfferLevel(int offerIndex)
        {
            return shopOfferLevels != null && offerIndex >= 0 && offerIndex < shopOfferLevels.Length
                ? Mathf.Clamp(shopOfferLevels[offerIndex], 1, 3)
                : 1;
        }

        private int UnitCost(string nameKey, int level = 1)
        {
            var multiplier = level <= 1 ? 1 : level == 2 ? 3 : 9;
            return characterCatalog.Get(nameKey).ShopCost * multiplier;
        }

        private int UnitBasePower(string nameKey)
        {
            return characterCatalog.Get(nameKey).BasePower;
        }

        private string PrimaryStatsText(CombatStatBlock stats)
        {
            return T("healthShort") + " " + stats.MaxHealth + "  ·  " +
                   T("attackShort") + " " + stats.AttackDamage;
        }

        private string PrimaryStatsText(UnitData unit)
        {
            return T("healthShort") + " " + unit.MaxHealth + "  ·  " +
                   T("attackShort") + " " + unit.AttackDamage;
        }

        private string SecondaryStatsText(CombatStatBlock stats)
        {
            return T("armorShort") + " " + stats.Armor + "  ·  " +
                   T("speed." + stats.SpeedTier.ToString().ToLowerInvariant());
        }

        private string SecondaryStatsText(UnitData unit)
        {
            return T("armorShort") + " " + unit.Armor + "  ·  " +
                   T("speed." + unit.SpeedTier.ToString().ToLowerInvariant());
        }

        private UnitData CreateUnit(string nameKey, int power, Color color, int runLevel = 1)
        {
            return UnitData.Create(characterCatalog.Get(nameKey), power, color, runLevel);
        }

        private void StartBattle()
        {
            if (!expeditionFlow.CanPrepare || formationOverlayTransitionActive) return;
            if (PlayerUnitCount() == 0)
            {
                statusKey = "status.needUnit";
                return;
            }
            formationOverlayTransitionActive = true;
            formationOverlayTransitionProgress = 0f;
            battleControlsEnabled = false;
            ResetPreparationPointer();
            StartCoroutine(BeginFormationToBattle());
        }

        private IEnumerator BeginFormationToBattle()
        {
            var elapsed = 0f;
            while (elapsed < FormationBattleV94Layout.TransitionDurationSeconds)
            {
                elapsed += Time.unscaledDeltaTime;
                formationOverlayTransitionProgress = Mathf.Clamp01(
                    elapsed / FormationBattleV94Layout.TransitionDurationSeconds);
                yield return null;
            }

            formationOverlayTransitionProgress = 1f;
            formationOverlayTransitionActive = false;
            if (!expeditionFlow.TryBeginBattle())
            {
                formationOverlayTransitionProgress = 0f;
                yield break;
            }
            selectedRunSlot = -1;
            preparationBackpackExpanded = false;
            preparationShopOpen = false;
            ClosePreparationUnitDetail();
            ResetPreparationPointer();
            confirmAbandon = false;
            battlePaused = false;
            battleControlsEnabled = true;
            battleSceneOpacity = 1f;
            battlePresentationActive = true;
            selectedFormationSynergyId = null;
            formationSynergyOverflowOpen = false;
            selectedFormationBuffId = null;
            formationBuffOverflowOpen = false;
            StartCoroutine(ResolveBattle());
        }

        private IEnumerator ResolveBattle()
        {
            statusKey = "status.battle";
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            consumedBattleFeedbackKeys.Clear();
            CreateCombatTeams();
            yield return WaitForBattleSeconds(0.24f);

            var result = DeterministicBattle.Run(CreateBattleInput());
            lastBattleEventCount = result.Events.Count;
            lastBattleHash = ComputeBattleHash(result.Events);
            foreach (var battleEvent in result.Events)
            {
                yield return PlayBattleEvent(battleEvent);
            }
            battlePresentation.EndCue();

            var playerWon = result.PlayerWon;
            if (playerWon)
            {
                if (AwardCompletedBattleNode()) coins += encounterReward;
                expeditionEconomy.ConsumeNextBattlePreparation();
                expeditionEconomy.ClearActiveBattleItem();
                if (round >= FinalRound)
                {
                    runWon = true;
                    finalScore = CalculateScore();
                }
            }
            else
            {
                health = Mathf.Max(0, health - 1);
                if (health == 0)
                {
                    runWon = false;
                    finalScore = CalculateScore();
                }
                else
                {
                    statusKey = "status.loss";
                }
            }
            yield return WaitForBattleSeconds(0.42f);
            var runEnded = health == 0 || (playerWon && round >= FinalRound);
            if (runEnded)
            {
                expeditionEconomy.ConsumeNextBattlePreparation();
                expeditionEconomy.ClearActiveBattleItem();
            }
            expeditionFlow.TryFinishBattle(playerWon, runEnded);
            if (runEnded) ReleaseRunEquipmentLocks();

            if (expeditionFlow.CanChooseUpgrade)
            {
                statusKey = "status.upgrade";
                EnsureRoguelikeChoiceSet();
            }
            else if (expeditionFlow.CanPrepare)
            {
                preparationBackpackExpanded = false;
                preparationShopOpen = true;
                ClosePreparationUnitDetail();
                ResetPreparationPointer();
            }
            battleControlsEnabled = false;
            battlePaused = false;
            yield return AnimateBattleScene(1f, 0f, 0.38f);
            battlePresentationActive = false;
            battlePresentation.Reset();
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            consumedBattleFeedbackKeys.Clear();
            SaveGame();
        }

        private IEnumerator AnimateBattleScene(float from, float to, float duration)
        {
            var elapsed = 0f;
            battleSceneOpacity = from;
            while (elapsed < duration)
            {
                if (!battlePaused) elapsed += Time.unscaledDeltaTime;
                battleSceneOpacity = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
            battleSceneOpacity = to;
        }

        private IEnumerator WaitForBattleSeconds(float duration)
        {
            var elapsed = 0f;
            while (elapsed < duration)
            {
                if (!battlePaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private IEnumerator PlayBattleEvent(BattleEvent battleEvent)
        {
            var cue = battlePresentation.BeginCue(battleEvent);
            var duration = cue.DurationSeconds;
            var elapsed = 0f;
            var applied = false;
            while (elapsed < duration)
            {
                battlePresentation.SetCueProgress(elapsed / duration);
                if (!applied && cue.Progress >= cue.ImpactProgress)
                {
                    ApplyBattleEvent(battleEvent);
                    applied = true;
                }
                if (!battlePaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!applied) ApplyBattleEvent(battleEvent);
            battlePresentation.SetCueProgress(1f);
            yield return WaitForBattleSeconds(0.04f);
            battlePresentation.EndCue();
        }

        private BattleInput CreateBattleInput()
        {
            var input = new BattleInput
            {
                Seed = currentEncounter == null
                    ? (uint)(round * 1009 + encounterPowerBonus * 53 + PlayerUnitCount())
                    : currentEncounter.BattleSeed
            };
            var synergy = CurrentFormationSynergy();
            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
            {
                var runUnit = runRoster.Get(slot);
                if (runUnit == null) continue;
                var unit = CreateRunUnitView(runUnit);
                ApplyActiveBattleItemToPresentationUnit(unit, true, slot);
                input.PlayerUnits.Add(ToBattleUnitState(unit, "player-" + slot, slot, true, synergy));
            }
            for (var index = 0; index < enemyUnits.Count; index++)
            {
                var formationSlot = EnemyFormationSlotForRosterIndex(index);
                if (formationSlot < 0) continue;
                input.EnemyUnits.Add(ToBattleUnitState(
                    enemyUnits[index], "enemy-" + index, formationSlot, false, null));
            }
            input.PlayerRoguelikeEffects.AddRange(roguelikeBuild.AcquiredEffects);
            return input;
        }

        private BattleUnitState ToBattleUnitState(
            UnitData unit,
            string id,
            int slot,
            bool isPlayer,
            FormationSynergySnapshot synergy)
        {
            var definition = characterCatalog.Get(unit.NameKey);
            return new BattleUnitState
            {
                Id = id,
                IsPlayer = isPlayer,
                Slot = slot,
                Health = unit.MaxHealth,
                MaxHealth = unit.MaxHealth,
                AttackDamage = unit.AttackDamage,
                HealAmount = unit.HealAmount,
                Armor = unit.Armor,
                Shield = unit.OpeningShield,
                SpeedTier = unit.SpeedTier,
                IsHealer = unit.IsHealer,
                LowHealthActionPercent = isPlayer && synergy.IsFactionActive("faction.wild") &&
                                         definition.FactionId == "faction.wild" ? 15 : 0,
                FirstActionPercent = isPlayer && synergy.IsFactionActive("faction.starlight") &&
                                     definition.FactionId == "faction.starlight" ? 25 : 0,
                ExecuteDamagePercent = isPlayer && synergy.IsProfessionActive("profession.output") &&
                                       definition.ProfessionId == "profession.output" ? 12 :
                    unit.NameKey == "enemy.n4.skirmisher" ? 15 :
                    unit.NameKey == "enemy.e2.hunter" || unit.NameKey == "enemy.b2.hunter" ? 25 : 0,
                ArmorPenetration = isPlayer && expeditionEconomy.ActiveBattleItemId == "I03" &&
                    expeditionEconomy.ActiveBattleTargetSlot == slot ? 15 : 0,
                DamageTakenPercent = !isPlayer && expeditionEconomy.ActiveBattleItemId == "I05" &&
                    expeditionEconomy.ActiveBattleTargetSlot == slot ? 12 : 0,
                DamageTakenBoostHitsRemaining = !isPlayer && expeditionEconomy.ActiveBattleItemId == "I05" &&
                    expeditionEconomy.ActiveBattleTargetSlot == slot ? 5 : 0,
                TemporarySpeedActions = isPlayer && expeditionEconomy.ActiveBattleItemId == "I02" &&
                    expeditionEconomy.ActiveBattleTargetSlot == slot ? 2 : 0,
                FactionId = definition.FactionId,
                ProfessionId = definition.ProfessionId
            };
        }

        private void ApplyBattleEvent(BattleEvent battleEvent)
        {
            QueueBattleFloatingNumber(battleEvent);
            battlePresentation.Apply(battleEvent);
            SpawnBattleFeedback(battleEvent);
        }

        private void SpawnBattleFeedback(BattleEvent battleEvent)
        {
            var isLeechHeal = battleEvent != null && battleEvent.IsSecondary &&
                battleEvent.Type == BattleEventType.Heal &&
                string.Equals(battleEvent.SourceEffectId, "B06", StringComparison.Ordinal);
            if (battleEvent == null || (battleEvent.IsSecondary && !isLeechHeal) ||
                (battleEvent.Type != BattleEventType.Attack &&
                 battleEvent.Type != BattleEventType.Heal)) return;
            var feedbackKey = string.Concat(
                battleEvent.Type, "|", battleEvent.ActorId, "|", battleEvent.TargetId, "|",
                battleEvent.Amount, "|", battleEvent.ActionTime, "|", battleEvent.SourceEffectId);
            if (!consumedBattleFeedbackKeys.Add(feedbackKey)) return;
            var target = battlePresentation.FindView(battleEvent.TargetId);
            if (target == null || target.State == null) return;

            var profileId = battleEvent.Type == BattleEventType.Heal
                ? BattleFeedbackProfileIds.HealFeedback
                : FeedbackProfileForAttack(battleEvent.ActorId);
            if (battleEvent.Type == BattleEventType.Heal && !target.State.IsAlive) return;
            var isHeal = battleEvent.Type == BattleEventType.Heal;
            var hasRing = isHeal && LoadBattleVfxSprite(
                BattleProjectileProfiles.HealFeedbackRingResourcePath) != null;
            var hasRise = isHeal && LoadBattleVfxSprite(
                BattleProjectileProfiles.HealFeedbackRiseResourcePath) != null;
            var hasLayeredHeal = hasRing && hasRise;
            if (hasLayeredHeal)
            {
                var baseCenter = BattleBaseCenterFor(target);
                var baseWidth = BattleBaseWidthFor(target.State);
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = battleEvent.TargetId,
                    ProfileId = BattleFeedbackProfileIds.HealFeedbackRing,
                    Position = baseCenter,
                    Width = baseWidth * 1.1f,
                    DurationSeconds = 0.65f,
                    ElapsedSeconds = 0f,
                    StartDelaySeconds = 0f,
                    HeightOverride = 0f,
                    Motion = Vector2.zero,
                    RenderScale = 1f
                });
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = battleEvent.TargetId,
                    ProfileId = BattleFeedbackProfileIds.HealFeedbackRise,
                    Position = baseCenter - Vector2.up * (baseWidth * 0.35f),
                    Width = 0f,
                    HeightOverride = baseWidth * 0.58f,
                    DurationSeconds = 0.475f,
                    StartDelaySeconds = 0.075f,
                    Motion = Vector2.zero,
                    ElapsedSeconds = 0f,
                    RenderScale = 1f
                });
                return;
            }

            var position = profileId == BattleFeedbackProfileIds.HealFeedback
                ? BattleBaseCenterFor(target)
                : target.GetAnchor(BattlePresentationIds.HitAnchor);
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = battleEvent.TargetId,
                ProfileId = profileId,
                Position = position,
                Width = FeedbackWidthFor(profileId, target.State),
                DurationSeconds = profileId == BattleFeedbackProfileIds.HealFeedback
                    ? 0.28f
                    : profileId == BattleFeedbackProfileIds.HitReaction ? 0.12f
                    : profileId == BattleFeedbackProfileIds.HostileHitFeedbackV2 ? 0.18f : 0.16f,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f
            });
        }

        private float FeedbackWidthFor(string profileId, BattleUnitPresentationState target)
        {
            if (string.Equals(profileId, BattleFeedbackProfileIds.HealFeedback,
                    StringComparison.Ordinal))
            {
                return BattleBaseWidthFor(target) * 1.1f;
            }
            if (string.Equals(profileId, BattleFeedbackProfileIds.HitReaction,
                    StringComparison.Ordinal)) return 76f * 1.5f;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HostileHitFeedbackV2,
                    StringComparison.Ordinal)) return 84f * 1.5f;
            return 84f * 1.5f;
        }

        private Vector2 BattleBaseCenterFor(BattleUnitViewDriver target)
        {
            if (target == null || target.State == null)
                return target == null ? Vector2.zero : target.GetAnchor(BattlePresentationIds.FeetAnchor);

            var baseWidth = BattleBaseWidthFor(target.State);
            var baseHeight = baseWidth * PieceBasePresentationContract.CanvasHeight /
                             (float)PieceBasePresentationContract.CanvasWidth;
            var centerOffset = baseHeight *
                               (0.5f - PieceBasePresentationContract.FootAnchorTopNormalizedY);
            return target.GetAnchor(BattlePresentationIds.FeetAnchor) + Vector2.up * centerOffset;
        }

        private float BattleBaseWidthFor(BattleUnitPresentationState unit)
        {
            if (unit == null) return PieceBasePresentationContract.DefaultDisplayWidth;

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            var qualityId = ResolveUnitLevelBadgeQualityId(unit.DefinitionId, unit.IsPlayer);
            if (!artCatalog.TryGetCharacterArt(unit.DefinitionId, qualityId,
                    out artDefinition, out sprite) || sprite == null)
            {
                return PieceBasePresentationContract.DisplayWidthForVisibleCharacterWidth(
                    GetAnimatedBattleRect(unit, GetArenaBattleRect(unit)).width);
            }

            var animatedSlotRect = GetAnimatedBattleRect(unit, GetArenaBattleRect(unit));
            var canvasRect = BattlePresentationLayout.GetStaticBattleCanvasRect(
                animatedSlotRect, sprite, FormationBattleVisualScale(unit.DefinitionId));
            canvasRect.y += BattlePresentationLayout.StaticBattleCanvasHeight *
                            FormationBattleVisualScale(unit.DefinitionId) *
                            artDefinition.VisualYOffsetRelativeToUnitHeight;
            var visibleRect = artDefinition.GetVisibleContentRect(
                canvasRect, StaticBattleArtContract.FormalBattleFlipX);
            return PieceBasePresentationContract.DisplayWidthForVisibleCharacterWidth(
                visibleRect.width);
        }

        private string FeedbackProfileForAttack(string actorId)
        {
            var actor = battlePresentation.FindView(actorId);
            if (actor != null && actor.State != null && !actor.State.IsPlayer)
                return BattleFeedbackProfileIds.HostileHitFeedbackV2;
            return actor != null && actor.State != null &&
                   string.Equals(actor.State.DefinitionId, "unit.thornGuard", StringComparison.Ordinal)
                ? BattleFeedbackProfileIds.HitReaction
                : BattleFeedbackProfileIds.HitFeedback;
        }

        private void QueueBattleFloatingNumber(BattleEvent battleEvent)
        {
            if (battleEvent == null || battleEvent.Type == BattleEventType.Defeat || battleEvent.Amount <= 0) return;
            var cue = battleVisual;
            var target = battlePresentation.FindView(battleEvent.TargetId);
            if (cue == null || target == null) return;

            // The old overlay was visible for the final 52% of the cue. Keep that
            // per-cue timing relationship and double it without extending the action itself.
            var duration = Mathf.Max(0.10f, cue.DurationSeconds * (1f - 0.48f) * 2f);
            battleFloatingNumbers.Add(new BattleFloatingNumber
            {
                Type = battleEvent.Type,
                Amount = battleEvent.Amount,
                TargetPosition = target.GetAnchor(cue.TargetAnchorId),
                DurationSeconds = duration,
                ElapsedSeconds = 0f
            });
        }

        private void ApplyUpgrade(string optionId)
        {
            if (!expeditionFlow.CanChooseUpgrade) return;
            var choiceSet = EnsureRoguelikeChoiceSet();
            if (choiceSet == null) return;
            confirmAbandon = false;
            var actionId = NewRunEconomyActionId("boon");
            var expectedVersion = choiceSet.expectedRunStateVersion;
            RoguelikeChoiceReceipt choiceReceipt = null;
            var runReceipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                choiceReceipt = roguelikeBuild.Select(choiceSet.choiceSetId, optionId,
                    actionId, expectedVersion);
                var choiceCode = (RoguelikeChoiceResultCode)choiceReceipt.resultCode;
                return choiceCode == RoguelikeChoiceResultCode.Success
                    ? RunEconomyResultCode.Success
                    : choiceCode == RoguelikeChoiceResultCode.StaleVersion
                        ? RunEconomyResultCode.StaleVersion
                        : RunEconomyResultCode.InvalidTarget;
            });
            if (runReceipt.ResultCode != RunEconomyResultCode.Success || choiceReceipt == null) return;
            expeditionFlow.TryChooseUpgrade();
            roguelikeBuild.CompleteSelection();
            SaveGame();
        }

        private RoguelikeChoiceSet EnsureRoguelikeChoiceSet()
        {
            if (!expeditionFlow.CanChooseUpgrade) return null;
            if (roguelikeBuildDiagnosticsOpen && roguelikeDiagnosticSuppressChoiceSet) return null;
            roguelikeBuild.SynchronizeRunStateVersion(runEconomyActions.StateVersion);
            var progress = Mathf.Clamp((round - 1) * 10000 / Mathf.Max(1, FinalRound - 1), 0, 9999);
            return roguelikeBuild.EnsureChoiceSet(runId, progress, currentEncounterIsElite,
                round >= FinalRound, true, CreateRoguelikeEligibilityContext());
        }

        private RoguelikeEligibilityContext CreateRoguelikeEligibilityContext()
        {
            var context = new RoguelikeEligibilityContext();
            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
            {
                var runUnit = runRoster.Get(slot);
                if (runUnit == null) continue;
                var definition = characterCatalog.Get(runUnit.DefinitionId);
                var view = CreateRunUnitView(runUnit);
                if (view.AttackDamage > 0) context.DirectAttackerCount++;
                if (definition.FactionId == "faction.steel" || definition.ProfessionId == "profession.guardian")
                    context.HasSteelOrGuardian = true;
                if (view.OpeningShield > 0) context.HasShieldSource = true;
            }
            return context;
        }

        private string RoguelikeCardDescription(RoguelikeEffectDefinition definition, RoguelikeRarity rarity)
        {
            var value = definition.Value(rarity);
            var stacks = RoguelikeEnhancementRules.StackCount(roguelikeBuild.AcquiredEffects, definition.EffectId);
            return RoguelikeValueText(definition, value) + " · " + T(definition.ScopeKey) + "\n" +
                   T("rogue.stack") + " " + stacks + "/" + definition.MaximumStacks;
        }

        private string RoguelikeValueText(RoguelikeEffectDefinition definition, int value)
        {
            var prefix = T("rogue." + definition.EffectId + ".stat");
            if (definition.EffectId == "B03") return prefix + " +" + value;
            if (definition.EffectId == "B10") return prefix + " " + value;
            return prefix + " +" + (value / 100f).ToString("0.##") + "%";
        }

        private static string ComputeBattleHash(IEnumerable<BattleEvent> events)
        {
            var builder = new System.Text.StringBuilder();
            foreach (var battleEvent in events)
                builder.Append((int)battleEvent.Type).Append('|').Append(battleEvent.ActorId).Append('|')
                    .Append(battleEvent.TargetId).Append('|').Append(battleEvent.Amount).Append('|')
                    .Append(battleEvent.TargetHealthAfter).Append('|').Append(battleEvent.TargetShieldAfter)
                    .Append('|').Append(battleEvent.IsSecondary ? 1 : 0).Append('|')
                    .Append(battleEvent.SourceEffectId).Append(';');
            return RoguelikeEnhancementRules.StableHash(builder.ToString()).ToString("X8");
        }

        private void SelectMapNode(string nodeId)
        {
            if (!expeditionFlow.CanChooseRoute || expeditionMap == null) return;
            var selected = expeditionMap.GetNode(nodeId);
            if (selected == null || !ContainsNode(expeditionMap.GetAvailableNext(currentMapNodeId), nodeId)) return;

            confirmAbandon = false;
            encounterPowerBonus = 0;
            encounterReward = 0;
            currentEncounterIsElite = selected.Type == ExpeditionNodeType.EliteBattle;

            currentMapNodeId = selected.Id;
            round = selected.Layer;
            if (IsBattleNode(selected.Type))
            {
                if (!expeditionFlow.TryChooseRoute()) return;
                CreateEnemies();
                RefreshAllShopOffers();
                statusKey = "status.recruit";
                preparationBackpackExpanded = false;
                preparationShopOpen = true;
                ClosePreparationUnitDetail();
                ResetPreparationPointer();
            }
            else
            {
                if (!expeditionFlow.TryEnterNode()) return;
                statusKey = "status.node";
            }
            SaveGame();
        }

        private static bool IsBattleNode(ExpeditionNodeType type)
        {
            return type == ExpeditionNodeType.NormalBattle ||
                   type == ExpeditionNodeType.EliteBattle ||
                   type == ExpeditionNodeType.HighRiskBattle ||
                   type == ExpeditionNodeType.Boss;
        }

        private int CalculateScore()
        {
            if (!currentExpeditionRewardsEnabled) return 0;
            return (round - 1) * 100 + coins * 10 + health * 25 + (runWon ? 500 : 0);
        }

        private bool AwardCompletedBattleNode()
        {
            var node = expeditionMap == null ? null : expeditionMap.GetNode(currentMapNodeId);
            if (node == null || expeditionRewards.HasCompletedNode(node.Id)) return false;
            var tideCrystals = 1;
            var stone = 0;
            var lampOil = 0;
            var mistLens = 0;
            string equipmentTemplateId = null;
            switch (node.Type)
            {
                case ExpeditionNodeType.EliteBattle:
                    tideCrystals = 2;
                    stone = 1;
                    break;
                case ExpeditionNodeType.HighRiskBattle:
                    tideCrystals = 3;
                    stone = 1;
                    break;
                case ExpeditionNodeType.Boss:
                    tideCrystals = 10;
                    stone = 4;
                    lampOil = 1;
                    mistLens = 1;
                    var definitions = equipmentCatalog.Definitions;
                    var index = (int)(expeditionMapSeed % (uint)definitions.Count);
                    equipmentTemplateId = definitions[index].TemplateId;
                    break;
            }
            if (!currentExpeditionRewardsEnabled)
            {
                tideCrystals = 0;
                stone = 0;
                lampOil = 0;
                mistLens = 0;
                equipmentTemplateId = null;
            }
            if (!ExpeditionProgression.TryCompleteNode(
                    expeditionRewards, node.Id,
                    tideCrystals, stone, lampOil, mistLens, equipmentTemplateId)) return false;
            if (node.Type == ExpeditionNodeType.Boss) chapterOneLighthouse.RecordBossVictory();
            return true;
        }

        private bool CanStoreExpeditionRewards(ExpeditionRewardSnapshot rewards)
        {
            return rewards != null && WarehouseStorageRules.CanAccept(
                equipmentInventory.Instances.Count,
                stoneBalance,
                lampOilBalance,
                mistLensBalance,
                rewards.EquipmentTemplateIds.Count,
                rewards.Stone,
                rewards.LampOil,
                rewards.MistLens);
        }

        private bool ClaimExpeditionRewards()
        {
            var pending = expeditionRewards.CreateSnapshot();
            if (!CanStoreExpeditionRewards(pending)) return false;
            ExpeditionRewardSnapshot claimed;
            if (!expeditionRewards.TryClaim(out claimed)) return false;
            tideCrystalBalance += claimed.TideCrystals;
            stoneBalance += claimed.Stone;
            lampOilBalance += claimed.LampOil;
            mistLensBalance += claimed.MistLens;
            foreach (var templateId in claimed.EquipmentTemplateIds)
            {
                var instanceId = "local.reward." + nextLocalEquipmentSerial++;
                while (!equipmentInventory.TryAdd(new EquipmentInstanceState(
                    instanceId, templateId, EquipmentQuality.Common, "local-test")))
                {
                    instanceId = "local.reward." + nextLocalEquipmentSerial++;
                }
            }
            SaveGame();
            return true;
        }

        private void RestartRun()
        {
            RestartExpeditionState("status.recruit");
            var energy = ExpeditionEnergyRules.BeginExpedition(expeditionEnergy);
            expeditionEnergy = energy.RemainingEnergy;
            currentExpeditionRewardsEnabled = energy.RewardsEnabled;
            expeditionEntryCommitted = true;
            SaveGame();
        }

        private void AbandonRun()
        {
            StopAllCoroutines();
            battlePresentation.Reset();
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            battlePaused = false;
            battleControlsEnabled = false;
            battleSceneOpacity = 0f;
            battlePresentationActive = false;
            formationOverlayTransitionActive = false;
            formationOverlayTransitionProgress = 0f;
            selectedFormationSynergyId = null;
            formationSynergyOverflowOpen = false;
            selectedFormationBuffId = null;
            formationBuffOverflowOpen = false;
            runWon = false;
            runAbandoned = true;
            finalScore = 0;
            confirmAbandon = false;
            preparationBackpackExpanded = false;
            preparationShopOpen = false;
            ClosePreparationUnitDetail();
            ResetPreparationPointer();
            statusKey = "status.abandoned";
            expeditionFlow.TryAbandon();
            ReleaseRunEquipmentLocks();
            currentView = ScreenView.Home;
            SaveGame();
        }

        private void SaveGame()
        {
            if (expeditionFlow == null || expeditionFlow.IsBattling || runRoster == null || runDeck == null ||
                nextRunDeckSelection == null || playerCollection == null)
            {
                return;
            }
            EnsureRunEconomyIdentity();
            var data = new PrototypeRunSaveData
            {
                saveVersion = SaveSchemaVersion,
                runEconomyRulesVersion = RunEconomyRulesVersion,
                runId = runId,
                runStateVersion = runEconomyActions.StateVersion,
                coins = coins,
                health = health,
                round = round,
                mapSeed = unchecked((int)expeditionMapSeed),
                mapGenerationVersion = ExpeditionMapGraph.MapGenerationVersion,
                mapVisualGenerationVersion = ExpeditionMapVisualSelector.VisualGenerationVersion,
                currentMapNodeId = currentMapNodeId,
                runAttackBonus = runAttackBonus,
                runHealthBonus = runHealthBonus,
                roguelikeRulesVersion = RoguelikeRulesVersion,
                roguelikeRunStateVersion = roguelikeBuild.StateVersion,
                roguelikeChoiceOrdinal = roguelikeBuild.ChoiceOrdinal,
                pendingRoguelikeChoiceSet = roguelikeBuild.PendingChoiceSet,
                acquiredRoguelikeEffects = roguelikeBuild.CopyAcquiredEffects(),
                roguelikeChoiceReceipts = roguelikeBuild.CopyReceipts(),
                encounterPowerBonus = encounterPowerBonus,
                encounterReward = encounterReward,
                currentEncounterIsElite = currentEncounterIsElite,
                phase = (int)expeditionFlow.Phase,
                runWon = runWon,
                runAbandoned = runAbandoned,
                finalScore = finalScore,
                expeditionEnergy = expeditionEnergy,
                skrBalance = skrBalance,
                expeditionRewardsEnabled = currentExpeditionRewardsEnabled,
                expeditionEntryCommitted = expeditionEntryCommitted,
                tideCrystalBalance = tideCrystalBalance,
                stoneBalance = stoneBalance,
                lampOilBalance = lampOilBalance,
                mistLensBalance = mistLensBalance,
                nextLocalEquipmentSerial = nextLocalEquipmentSerial,
                pendingTideCrystals = expeditionRewards.TideCrystals,
                pendingStone = expeditionRewards.Stone,
                pendingLampOil = expeditionRewards.LampOil,
                pendingMistLens = expeditionRewards.MistLens,
                pendingEquipmentTemplateIds = CopyIds(expeditionRewards.EquipmentTemplateIds),
                completedRewardNodeIds = CopyIds(expeditionRewards.CompletedNodeIds),
                expeditionRewardsClaimed = expeditionRewards.IsClaimed,
                chapterOneBossDefeated = chapterOneLighthouse.BossDefeated,
                chapterOneLighthouseRepaired = chapterOneLighthouse.IsRepaired,
                units = new PrototypeUnitSaveData[runRoster.BoardCapacity],
                benchUnits = new PrototypeUnitSaveData[runRoster.BenchCapacity],
                activeDeckUnitIds = CopyIds(runDeck.UnitDefinitionIds),
                activeDeckCharacterInstanceIds = CopyIds(runDeck.CharacterInstanceIds),
                nextRunDeckUnitIds = CopyIds(nextRunDeckSelection.SelectedIds),
                nextRunDeckCharacterInstanceIds = CopyIds(nextRunDeckSelection.SelectedInstanceIds),
                shopOffers = shopOffers,
                shopOfferLevels = shopOfferLevels,
                shopOfferIds = shopOfferIds,
                shopOfferListedPrices = shopOfferListedPrices,
                shopOfferSold = shopOfferSold,
                shopRefreshSerial = shopRefreshSerial,
                firstManualRecruitRerollUsed = expeditionEconomy.FirstManualRecruitRerollUsed,
                recruitRerollVouchers = expeditionEconomy.RecruitRerollVouchers,
                recruitmentDiscount = expeditionEconomy.RecruitmentDiscount,
                chestRecruitmentDiscount = expeditionEconomy.ChestRecruitmentDiscount,
                expeditionMorale = expeditionEconomy.Morale,
                nextBattlePreparationId = expeditionEconomy.NextBattlePreparationId,
                expeditionBackpackItemIds = CopyIds(expeditionEconomy.Backpack),
                resolvedEconomyActionIds = expeditionEconomy.CopyResolvedActionIds(),
                activeBattleItemId = expeditionEconomy.ActiveBattleItemId,
                activeBattleTargetSlot = expeditionEconomy.ActiveBattleTargetSlot,
                lockedEconomyContentKeys = expeditionEconomy.CopyLockedNodeContentKeys(),
                lockedEconomyContentValues = expeditionEconomy.CopyLockedNodeContentValues(),
                resolvedRunEconomyActionIds = runEconomyActions.CopyActionIds(),
                resolvedRunEconomyActionResultCodes = runEconomyActions.CopyResultCodes(),
                resolvedRunEconomyActionVersions = runEconomyActions.CopyResultingVersions(),
                lastRunEconomyActionId = runEconomyActions.LastActionId,
                lastRunEconomyResultCode = (int)runEconomyActions.LastResultCode,
                collection = new PrototypeCollectionSaveData[playerCollection.Entries.Count],
                equipment = new PrototypeEquipmentSaveData[equipmentInventory.Instances.Count],
                activeRunLoadouts = new PrototypeLoadoutSnapshotSaveData[runEquipmentSnapshots.Count],
                defenseUnits = new PrototypeUnitSaveData[defenseSlots.Length]
            };
            for (var i = 0; i < playerCollection.Entries.Count; i++)
            {
                var owned = playerCollection.Entries[i];
                data.collection[i] = new PrototypeCollectionSaveData
                {
                    characterInstanceId = owned.CharacterInstanceId,
                    definitionId = owned.DefinitionId,
                    copyCount = owned.CopyCount,
                    externalStar = owned.ExternalStar,
                    quality = (int)owned.Quality
                };
            }
            for (var i = 0; i < equipmentInventory.Instances.Count; i++)
            {
                var instance = equipmentInventory.Instances[i];
                data.equipment[i] = new PrototypeEquipmentSaveData
                {
                    equipmentInstanceId = instance.EquipmentInstanceId,
                    templateId = instance.TemplateId,
                    quality = (int)instance.Quality,
                    seasonId = instance.SeasonId,
                    equippedCharacterInstanceId = instance.EquippedCharacterInstanceId,
                    usageLocks = (int)instance.UsageLocks
                };
            }
            var loadoutIndex = 0;
            foreach (var pair in runEquipmentSnapshots)
            {
                var modifier = pair.Value.Modifier;
                data.activeRunLoadouts[loadoutIndex++] = new PrototypeLoadoutSnapshotSaveData
                {
                    definitionId = pair.Key,
                    equipmentInstanceIds = CopyIds(pair.Value.EquipmentInstanceIds),
                    maxHealth = modifier.MaxHealth,
                    attackDamage = modifier.AttackDamage,
                    armor = modifier.Armor,
                    healAmount = modifier.HealAmount,
                    speedTierShift = modifier.SpeedTierShift
                };
            }
            for (var i = 0; i < runRoster.BoardCapacity; i++)
            {
                var runUnit = runRoster.Get(i);
                data.units[i] = new PrototypeUnitSaveData();
                if (runUnit == null) continue;
                var unit = CreateRunUnitView(runUnit);
                data.units[i].occupied = true;
                data.units[i].runUnitId = runUnit.RunUnitId;
                data.units[i].nameKey = runUnit.DefinitionId;
                data.units[i].power = unit.Power;
                data.units[i].basePower = runUnit.BasePower;
                data.units[i].runLevel = runUnit.RunLevel;
                data.units[i].acquisitionBasis = runUnit.AcquisitionBasis;
                data.units[i].maxHealth = unit.MaxHealth;
                data.units[i].attackDamage = unit.AttackDamage;
                data.units[i].healAmount = unit.HealAmount;
                data.units[i].isHealer = unit.IsHealer;
            }
            for (var i = 0; i < runRoster.BenchCapacity; i++)
            {
                var runUnit = runRoster.Get(runRoster.BoardCapacity + i);
                data.benchUnits[i] = new PrototypeUnitSaveData();
                if (runUnit == null) continue;
                var unit = CreateRunUnitView(runUnit);
                data.benchUnits[i].occupied = true;
                data.benchUnits[i].runUnitId = runUnit.RunUnitId;
                data.benchUnits[i].nameKey = runUnit.DefinitionId;
                data.benchUnits[i].power = unit.Power;
                data.benchUnits[i].basePower = runUnit.BasePower;
                data.benchUnits[i].runLevel = runUnit.RunLevel;
                data.benchUnits[i].acquisitionBasis = runUnit.AcquisitionBasis;
                data.benchUnits[i].maxHealth = unit.MaxHealth;
                data.benchUnits[i].attackDamage = unit.AttackDamage;
                data.benchUnits[i].healAmount = unit.HealAmount;
                data.benchUnits[i].isHealer = unit.IsHealer;
            }
            for (var i = 0; i < defenseSlots.Length; i++)
            {
                var unit = defenseSlots[i];
                data.defenseUnits[i] = new PrototypeUnitSaveData();
                if (unit == null) continue;
                data.defenseUnits[i].occupied = true;
                data.defenseUnits[i].nameKey = unit.NameKey;
                data.defenseUnits[i].power = unit.Power;
                data.defenseUnits[i].maxHealth = unit.MaxHealth;
                data.defenseUnits[i].attackDamage = unit.AttackDamage;
                data.defenseUnits[i].healAmount = unit.HealAmount;
                data.defenseUnits[i].isHealer = unit.IsHealer;
            }
            PrototypeSaveStore.Save(SaveKey, data);
        }

        private bool LoadGame()
        {
            PrototypeRunSaveData data;
            if (!PrototypeSaveStore.TryLoad(SaveKey, out data) || data.units == null) return false;

            RestoreCollectionAndNextDeck(data);
            RestoreEquipment(data.equipment);
            RestoreRunEquipmentSnapshots(data.activeRunLoadouts);
            RestoreDefense(data.defenseUnits);
            if (data.saveVersion < 21)
            {
                expeditionEnergy = ExpeditionEnergyRules.InitialEnergy;
                skrBalance = 0;
                currentExpeditionRewardsEnabled = true;
            }
            else
            {
                expeditionEnergy = Mathf.Max(0, data.expeditionEnergy);
                skrBalance = Mathf.Max(0, data.skrBalance);
                currentExpeditionRewardsEnabled = data.expeditionRewardsEnabled;
            }
            expeditionEntryCommitted = data.saveVersion >= 22
                ? data.expeditionEntryCommitted
                : data.expeditionEnergy < ExpeditionEnergyRules.InitialEnergy ||
                  data.round > 1 || data.coins != 10 || data.health != 10 ||
                  data.phase != (int)ExpeditionPhase.Route;
            tideCrystalBalance = Mathf.Max(0, data.tideCrystalBalance);
            stoneBalance = Mathf.Max(0, data.stoneBalance);
            lampOilBalance = Mathf.Max(0, data.lampOilBalance);
            mistLensBalance = Mathf.Max(0, data.mistLensBalance);
            nextLocalEquipmentSerial = Mathf.Max(1, data.nextLocalEquipmentSerial);
            chapterOneLighthouse.Restore(
                data.chapterOneBossDefeated,
                data.chapterOneLighthouseRepaired);
            if (data.saveVersion < MinimumCompatibleSaveVersion)
            {
                // Earlier builds could save a partial transition between battle,
                // upgrade and route selection.  There is no trustworthy way to
                // infer which reward was chosen, so restart only the expedition.
                RestartExpeditionState("status.repaired");
                SaveGame();
                return true;
            }

            if (data.mapGenerationVersion != ExpeditionMapGraph.MapGenerationVersion)
            {
                // A topology algorithm change must never silently rewrite an active
                // run beneath the player's current node. Preserve account inventory,
                // collection and loadouts, but start a new locally generated route.
                RestartExpeditionState("status.mapUpdated");
                SaveGame();
                return true;
            }

            RestoreExpedition(data);
            if (ExpeditionFlow.IsUnrecoverablePreparation(
                    expeditionFlow.Phase,
                    coins,
                    runRoster.TotalCount,
                    MinimumRecruitCost))
            {
                RestartExpeditionState("status.repaired");
                SaveGame();
                return true;
            }
            statusKey = expeditionFlow.CanChooseUpgrade
                ? "status.upgrade"
                : expeditionFlow.CanChooseRoute
                    ? "status.route"
                    : expeditionFlow.CanResolveNode ? "status.node" : "status.recruit";
            CreateEnemies();
            return true;
        }

        private void RestoreExpedition(PrototypeRunSaveData data)
        {
            runRoster.Clear();
            coins = data.coins;
            health = data.health;
            round = Mathf.Clamp(data.round, 1, FinalRound);
            expeditionMapSeed = data.mapSeed == 0
                ? unchecked((uint)(0x51EA2C31 + round * 7919))
                : unchecked((uint)data.mapSeed);
            expeditionMap = ExpeditionMapGraph.CreateChapterOne(expeditionMapSeed);
            runId = !string.IsNullOrEmpty(data.runId)
                ? data.runId
                : StableRunEconomyId("run", expeditionMapSeed.ToString(), "v24-migration");
            if (data.saveVersion >= 25)
            {
                runEconomyActions.Restore(data.runStateVersion,
                    data.resolvedRunEconomyActionIds,
                    data.resolvedRunEconomyActionResultCodes,
                    data.resolvedRunEconomyActionVersions);
            }
            else
            {
                runEconomyActions.Reset();
            }
            var restoredNode = expeditionMap.GetNode(data.currentMapNodeId);
            currentMapNodeId = restoredNode != null && restoredNode.Layer == round
                ? restoredNode.Id
                : expeditionMap.GetLayer(round)[0].Id;
            // v25 and earlier used two local placeholder numbers. They are read for save
            // compatibility, then retired instead of being mislabeled as B01/B02.
            runAttackBonus = 0;
            runHealthBonus = 0;
            if (data.saveVersion >= 26 && data.roguelikeRulesVersion == RoguelikeRulesVersion)
            {
                roguelikeBuild.Restore(data.roguelikeRunStateVersion, data.roguelikeChoiceOrdinal,
                    data.pendingRoguelikeChoiceSet, data.acquiredRoguelikeEffects,
                    data.roguelikeChoiceReceipts);
            }
            else
            {
                roguelikeBuild.Reset();
            }
            encounterPowerBonus = data.encounterPowerBonus;
            encounterReward = data.encounterReward == 0 ? 4 : data.encounterReward;
            currentEncounterIsElite = data.currentEncounterIsElite;
            if (!expeditionFlow.TryRestore(data.phase))
            {
                RestartExpeditionState("status.repaired");
                return;
            }
            if (data.saveVersion < 10 && expeditionFlow.CanPrepare)
            {
                var migratedNode = expeditionMap.GetNode(currentMapNodeId);
                if (migratedNode != null && !IsBattleNode(migratedNode.Type))
                {
                    expeditionFlow.TryRestore((int)ExpeditionPhase.Node);
                }
            }
            runWon = data.runWon;
            runAbandoned = data.runAbandoned;
            finalScore = data.finalScore;
            shopRefreshSerial = Mathf.Max(0, data.shopRefreshSerial);
            expeditionEconomy.Restore(
                data.saveVersion >= 23 && data.firstManualRecruitRerollUsed,
                data.recruitRerollVouchers,
                data.recruitmentDiscount,
                data.chestRecruitmentDiscount,
                data.saveVersion >= 23 ? data.expeditionMorale : 2,
                data.nextBattlePreparationId,
                data.expeditionBackpackItemIds,
                data.resolvedEconomyActionIds);
            var savedRewardTemplates = ValidEquipmentTemplateIds(data.pendingEquipmentTemplateIds);
            var migratedCompletedRun = data.saveVersion < 22 && expeditionFlow.IsComplete;
            expeditionRewards.Restore(
                data.pendingTideCrystals,
                data.pendingStone,
                data.pendingLampOil,
                data.pendingMistLens,
                savedRewardTemplates,
                data.completedRewardNodeIds,
                migratedCompletedRun || data.expeditionRewardsClaimed);
            if (data.saveVersion >= 23)
            {
                expeditionEconomy.Restore(
                    data.firstManualRecruitRerollUsed,
                    data.recruitRerollVouchers,
                    data.recruitmentDiscount,
                    data.chestRecruitmentDiscount,
                    data.expeditionMorale,
                    data.nextBattlePreparationId,
                    data.expeditionBackpackItemIds,
                    data.resolvedEconomyActionIds,
                    data.activeBattleItemId,
                    data.activeBattleTargetSlot,
                    data.lockedEconomyContentKeys,
                    data.lockedEconomyContentValues);
            }
            else
            {
                expeditionEconomy.Reset();
            }
            var activeDeckIds = data.activeDeckCharacterInstanceIds != null &&
                                data.activeDeckCharacterInstanceIds.Length > 0
                ? data.activeDeckCharacterInstanceIds
                : data.activeDeckUnitIds;
            if (activeDeckIds != null && activeDeckIds.Length > 0)
            {
                var restoredActiveDeck = new RunDeckSelection(
                    playerCollection, activeDeckIds, MaximumDeckSize, MinimumDeckSize);
                runDeck = restoredActiveDeck.CreateSnapshot();
                activeDeckUnitIds = CopyIds(runDeck.UnitDefinitionIds);
            }
            var validShop = data.shopOffers != null && data.shopOffers.Length == ShopOfferCount;
            if (validShop)
            {
                for (var i = 0; i < data.shopOffers.Length; i++)
                {
                    if (!string.IsNullOrEmpty(data.shopOffers[i]) && !runDeck.Contains(data.shopOffers[i]))
                    {
                        validShop = false;
                        break;
                    }
                }
            }
            if (validShop)
            {
                shopOffers = data.shopOffers;
                shopOfferLevels = data.shopOfferLevels != null &&
                                  data.shopOfferLevels.Length == ShopOfferCount
                    ? data.shopOfferLevels
                    : new[] { 1, 1, 1 };
                shopOfferIds = data.shopOfferIds != null && data.shopOfferIds.Length == ShopOfferCount
                    ? data.shopOfferIds
                    : new string[ShopOfferCount];
                shopOfferListedPrices = data.shopOfferListedPrices != null &&
                                        data.shopOfferListedPrices.Length == ShopOfferCount
                    ? data.shopOfferListedPrices
                    : new int[ShopOfferCount];
                shopOfferSold = data.shopOfferSold != null && data.shopOfferSold.Length == ShopOfferCount
                    ? data.shopOfferSold
                    : new bool[ShopOfferCount];
            }
            else
            {
                RefreshAllShopOffers();
            }
            for (var i = 0; i < runRoster.BoardCapacity && i < data.units.Length; i++)
            {
                var unit = RestoreRunUnit(data.units[i]);
                if (unit != null) runRoster.TryRestoreAt(i, unit);
            }
            if (data.benchUnits != null)
            {
                for (var i = 0; i < runRoster.BenchCapacity && i < data.benchUnits.Length; i++)
                {
                    var unit = RestoreRunUnit(data.benchUnits[i]);
                    if (unit != null) runRoster.TryRestoreAt(runRoster.BoardCapacity + i, unit);
                }
            }
            EnsureRunEconomyIdentity();
        }

        private void RestoreCollectionAndNextDeck(PrototypeRunSaveData data)
        {
            var restored = new List<OwnedCharacterState>();
            if (data.collection != null)
            {
                foreach (var saved in data.collection)
                {
                    if (saved == null || !characterCatalog.Contains(saved.definitionId) || saved.copyCount < 1) continue;
                    var quality = Enum.IsDefined(typeof(CharacterQuality), saved.quality)
                        ? (CharacterQuality)saved.quality
                        : CharacterQuality.Common;
                    var instanceId = string.IsNullOrEmpty(saved.characterInstanceId)
                        ? "character." + saved.definitionId
                        : saved.characterInstanceId;
                    var copyCount = Mathf.Max(1, saved.copyCount);
                    for (var copy = 0; copy < copyCount; copy++)
                    {
                        var expandedInstanceId = copy == 0 ? instanceId : instanceId + ".copy." + (copy + 1);
                        restored.Add(new OwnedCharacterState(expandedInstanceId, saved.definitionId, 1,
                            Mathf.Max(0, saved.externalStar), quality));
                    }
                }
            }
            foreach (var definition in characterCatalog.Definitions)
            {
                if (!definition.IsCollectible || ContainsOwnedCharacter(restored, definition.DefinitionId)) continue;
                restored.Add(new OwnedCharacterState(definition.DefinitionId, 1, 0, CharacterQuality.Common));
            }
            playerCollection = restored.Count > 0
                ? new PlayerCollection(characterCatalog, restored)
                : PlayerCollection.CreatePrototype(characterCatalog);

            var nextIds = data.nextRunDeckCharacterInstanceIds;
            if (nextIds == null || nextIds.Length == 0) nextIds = data.nextRunDeckUnitIds;
            if (nextIds == null || nextIds.Length == 0) nextIds = data.activeDeckUnitIds;
            if (nextIds == null || nextIds.Length == 0) nextIds = activeDeckUnitIds;
            nextRunDeckSelection = new RunDeckSelection(
                playerCollection, nextIds, MaximumDeckSize, MinimumDeckSize);
        }

        private static bool ContainsOwnedCharacter(
            IEnumerable<OwnedCharacterState> entries, string definitionId)
        {
            foreach (var entry in entries)
            {
                if (entry.DefinitionId == definitionId) return true;
            }
            return false;
        }

        private void RestoreEquipment(PrototypeEquipmentSaveData[] savedEquipment)
        {
            var restored = new List<EquipmentInstanceState>();
            var restoredIds = new HashSet<string>(StringComparer.Ordinal);
            if (savedEquipment != null)
            {
                foreach (var saved in savedEquipment)
                {
                    if (saved == null || string.IsNullOrEmpty(saved.equipmentInstanceId) ||
                        string.IsNullOrEmpty(saved.templateId) || restoredIds.Contains(saved.equipmentInstanceId)) continue;
                    try
                    {
                        equipmentCatalog.Get(saved.templateId);
                    }
                    catch (KeyNotFoundException)
                    {
                        continue;
                    }
                    restoredIds.Add(saved.equipmentInstanceId);
                    var quality = Enum.IsDefined(typeof(EquipmentQuality), saved.quality)
                        ? (EquipmentQuality)saved.quality
                        : EquipmentQuality.Common;
                    var owner = OwnsCharacterInstance(saved.equippedCharacterInstanceId)
                        ? saved.equippedCharacterInstanceId
                        : null;
                    var locks = (EquipmentUsageLock)saved.usageLocks &
                                (EquipmentUsageLock.Expedition | EquipmentUsageLock.Defense | EquipmentUsageLock.Market);
                    restored.Add(new EquipmentInstanceState(
                        saved.equipmentInstanceId,
                        saved.templateId,
                        quality,
                        string.IsNullOrEmpty(saved.seasonId) ? "prototype-season" : saved.seasonId,
                        owner,
                        locks));
                }
            }

            var defaults = EquipmentInventory.CreatePrototype(equipmentCatalog);
            foreach (var instance in defaults.Instances)
            {
                if (!restoredIds.Add(instance.EquipmentInstanceId)) continue;
                restored.Add(new EquipmentInstanceState(
                    instance.EquipmentInstanceId,
                    instance.TemplateId,
                    instance.Quality,
                    instance.SeasonId));
            }
            equipmentInventory = new EquipmentInventory(equipmentCatalog, restored);
        }

        private void RestoreRunEquipmentSnapshots(PrototypeLoadoutSnapshotSaveData[] savedLoadouts)
        {
            runEquipmentSnapshots.Clear();
            if (savedLoadouts == null) return;
            foreach (var saved in savedLoadouts)
            {
                if (saved == null || string.IsNullOrEmpty(saved.definitionId) ||
                    !characterCatalog.Contains(saved.definitionId) || runEquipmentSnapshots.ContainsKey(saved.definitionId)) continue;
                var modifier = new EquipmentStatModifier(
                    Mathf.Max(0, saved.maxHealth),
                    Mathf.Max(0, saved.attackDamage),
                    Mathf.Max(0, saved.armor),
                    Mathf.Max(0, saved.healAmount),
                    Mathf.Clamp(saved.speedTierShift, -5, 5));
                runEquipmentSnapshots.Add(saved.definitionId,
                    new EquipmentLoadoutSnapshot(saved.equipmentInstanceIds, modifier));
            }
        }

        private bool OwnsCharacterInstance(string characterInstanceId)
        {
            if (string.IsNullOrEmpty(characterInstanceId)) return false;
            try
            {
                playerCollection.GetByInstanceId(characterInstanceId);
                return true;
            }
            catch (KeyNotFoundException)
            {
                return false;
            }
        }

        private static string[] CopyIds(IReadOnlyList<string> ids)
        {
            var copied = new string[ids.Count];
            for (var i = 0; i < ids.Count; i++) copied[i] = ids[i];
            return copied;
        }

        private string[] ValidEquipmentTemplateIds(IEnumerable<string> ids)
        {
            var valid = new List<string>();
            if (ids == null) return valid.ToArray();
            foreach (var id in ids)
            {
                try
                {
                    equipmentCatalog.Get(id);
                    valid.Add(id);
                }
                catch (KeyNotFoundException)
                {
                    // Ignore stale or corrupted local-test reward references.
                }
            }
            return valid.ToArray();
        }

        private static RunUnitState RestoreRunUnit(PrototypeUnitSaveData savedUnit)
        {
            if (savedUnit == null || !savedUnit.occupied) return null;
            var runLevel = Mathf.Clamp(savedUnit.runLevel == 0 ? 1 : savedUnit.runLevel, 1, RunUnitRules.MaximumRunLevel);
            var basePower = savedUnit.basePower == 0 ? savedUnit.power : savedUnit.basePower;
            return new RunUnitState(savedUnit.nameKey, basePower, runLevel,
                Mathf.Max(0, savedUnit.acquisitionBasis), savedUnit.runUnitId);
        }

        private void EnsureRunEconomyIdentity()
        {
            if (string.IsNullOrEmpty(runId))
                runId = StableRunEconomyId("run", expeditionMapSeed.ToString(), "local");

            for (var index = 0; index < runRoster.TotalCapacity; index++)
            {
                var unit = runRoster.Get(index);
                if (unit == null || !string.IsNullOrEmpty(unit.RunUnitId)) continue;
                unit.EnsureRunUnitId(StableRunEconomyId("unit", runId, index.ToString(),
                    unit.DefinitionId, unit.RunLevel.ToString()));
            }

            if (shopOfferIds == null || shopOfferIds.Length != ShopOfferCount)
                shopOfferIds = new string[ShopOfferCount];
            if (shopOfferListedPrices == null || shopOfferListedPrices.Length != ShopOfferCount)
                shopOfferListedPrices = new int[ShopOfferCount];
            if (shopOfferSold == null || shopOfferSold.Length != ShopOfferCount)
                shopOfferSold = new bool[ShopOfferCount];
            for (var index = 0; index < ShopOfferCount; index++)
            {
                if (string.IsNullOrEmpty(shopOfferIds[index]))
                    shopOfferIds[index] = StableRunEconomyId("offer", runId, currentMapNodeId,
                        shopRefreshSerial.ToString(), index.ToString());
                if (!string.IsNullOrEmpty(shopOffers[index]))
                {
                    shopOfferSold[index] = false;
                    if (shopOfferListedPrices[index] <= 0)
                        shopOfferListedPrices[index] = UnitCost(shopOffers[index], ShopOfferLevel(index));
                }
                else
                {
                    shopOfferSold[index] = true;
                    shopOfferListedPrices[index] = Mathf.Max(0, shopOfferListedPrices[index]);
                }
            }
        }

        private void RestoreDefense(PrototypeUnitSaveData[] savedDefense)
        {
            if (savedDefense == null)
            {
                CreateStarterDefense();
                return;
            }
            for (var i = 0; i < defenseSlots.Length && i < savedDefense.Length; i++)
            {
                var savedUnit = savedDefense[i];
                if (savedUnit == null || !savedUnit.occupied) continue;
                var unit = CreateUnit(savedUnit.nameKey, savedUnit.power, PlayerColor(savedUnit.nameKey));
                unit.MaxHealth = savedUnit.maxHealth;
                unit.AttackDamage = savedUnit.attackDamage;
                unit.HealAmount = savedUnit.healAmount;
                unit.IsHealer = savedUnit.isHealer;
                defenseSlots[i] = unit;
            }
        }

        private void RestartExpeditionState(string newStatusKey)
        {
            ReleaseRunEquipmentLocks();
            runDeck = nextRunDeckSelection.CreateSnapshot();
            activeDeckUnitIds = CopyIds(runDeck.UnitDefinitionIds);
            LockRunEquipmentSnapshots();
            runRoster.Clear();
            coins = 10;
            health = 10;
            round = 1;
            CreateNewExpeditionMap();
            runAttackBonus = 0;
            runHealthBonus = 0;
            roguelikeBuild.Reset();
            encounterPowerBonus = 0;
            encounterReward = 4;
            currentEncounterIsElite = false;
            selectedRunSlot = -1;
            shopRefreshSerial = 0;
            shopOfferLevels = new int[ShopOfferCount];
            shopOfferIds = new string[ShopOfferCount];
            shopOfferListedPrices = new int[ShopOfferCount];
            shopOfferSold = new bool[ShopOfferCount];
            runId = "run-" + Guid.NewGuid().ToString("N");
            runEconomyActions.Reset();
            expeditionEconomy.Reset();
            preparationBackpackExpanded = false;
            preparationShopOpen = true;
            ClosePreparationUnitDetail();
            ResetPreparationPointer();
            confirmAbandon = false;
            battlePresentation.Reset();
            battlePaused = false;
            battleControlsEnabled = false;
            battleSceneOpacity = 0f;
            battlePresentationActive = false;
            expeditionFlow.BeginAtRoute();
            runWon = false;
            runAbandoned = false;
            finalScore = 0;
            expeditionEntryCommitted = false;
            expeditionRewards.Reset();
            statusKey = "status.route";
            CreateEnemies();
            RefreshAllShopOffers();
        }

        private void CreateNewExpeditionMap()
        {
            expeditionMapSeed = unchecked((uint)(DateTime.UtcNow.Ticks ^ GetInstanceID()));
            if (expeditionMapSeed == 0) expeditionMapSeed = 0x51EA2C31u;
            expeditionMap = ExpeditionMapGraph.CreateChapterOne(expeditionMapSeed);
            currentMapNodeId = expeditionMap.GetLayer(1)[0].Id;
            expeditionMapScroll = Vector2.zero;
            expeditionMapScrollAnchorNodeId = null;
            expeditionMapZoom = 1f;
            expeditionMapPinchDistance = 0f;
        }

        private static Color PlayerColor(string nameKey)
        {
            switch (nameKey)
            {
                case "unit.guardian": return new Color(0.16f, 0.50f, 0.61f);
                case "unit.healer": return new Color(0.38f, 0.45f, 0.56f);
                case "unit.thornGuard": return new Color(0.31f, 0.60f, 0.33f);
                case "unit.archer": return new Color(0.52f, 0.40f, 0.55f);
                case "unit.starblade": return new Color(0.43f, 0.38f, 0.33f);
                case "unit.starpriest": return new Color(0.67f, 0.36f, 0.24f);
                default: return Color.gray;
            }
        }

        private static Color EnemyColor(string nameKey)
        {
            if (nameKey != null && nameKey.StartsWith("enemy.b", StringComparison.Ordinal))
                return new Color(0.48f, 0.10f, 0.18f);
            if (nameKey != null && nameKey.StartsWith("enemy.e", StringComparison.Ordinal))
                return new Color(0.62f, 0.18f, 0.25f);
            if (nameKey == "enemy.n5.support") return new Color(0.48f, 0.34f, 0.62f);
            if (nameKey == "enemy.n3.ranged" || nameKey == "enemy.n4.skirmisher")
                return new Color(0.83f, 0.39f, 0.16f);
            return new Color(0.72f, 0.22f, 0.25f);
        }

        private void CreateCombatTeams()
        {
            var players = new List<BattleUnitPresentationState>();
            var enemies = new List<BattleUnitPresentationState>();
            for (var i = 0; i < runRoster.BoardCapacity; i++)
            {
                var runUnit = runRoster.Get(i);
                if (runUnit != null)
                {
                    var unit = CreateRunUnitView(runUnit);
                    ApplyActiveBattleItemToPresentationUnit(unit, true, i);
                    ApplyRoguelikePersistentStatPreview(unit);
                    players.Add(CreateBattleUnitPresentation(
                        unit, true, i, "player-" + i));
                }
            }
            for (var i = 0; i < enemyUnits.Count; i++)
            {
                var formationSlot = EnemyFormationSlotForRosterIndex(i);
                if (formationSlot < 0) continue;
                ApplyActiveBattleItemToPresentationUnit(enemyUnits[i], false, i);
                enemies.Add(CreateBattleUnitPresentation(enemyUnits[i], false, formationSlot, "enemy-" + i));
            }
            battlePresentation.BeginBattle(players, enemies);
        }

        private void ApplyActiveBattleItemToPresentationUnit(UnitData unit, bool isPlayer, int slot)
        {
            if (unit == null || expeditionEconomy.ActiveBattleTargetSlot != slot) return;
            if (isPlayer && expeditionEconomy.ActiveBattleItemId == "I01")
                unit.OpeningShield += Mathf.Max(1, Mathf.CeilToInt(unit.MaxHealth * 0.15f));
            if (isPlayer && expeditionEconomy.ActiveBattleItemId == "I02" &&
                unit.SpeedTier < CombatSpeedTier.Fast)
                unit.SpeedTier = (CombatSpeedTier)((int)unit.SpeedTier + 1);
        }

        private BattleUnitPresentationState CreateBattleUnitPresentation(
            UnitData unit, bool isPlayer, int slot, string id)
        {
            return new BattleUnitPresentationState(
                id,
                unit.NameKey,
                isPlayer,
                slot,
                unit.Color,
                unit.MaxHealth,
                unit.AttackDamage,
                unit.HealAmount,
                unit.Armor,
                unit.SpeedTier,
                unit.OpeningShield,
                BattlePresentationLayout.GetUnitFootPosition(isPlayer, slot, layoutContentHeight),
                unit.RunLevel);
        }

        private BattleUnitPresentationState FindCombatUnitById(string id)
        {
            return battlePresentation.FindUnit(id);
        }

        private BattleUnitPresentationState FindCombatUnit(
            IReadOnlyList<BattleUnitPresentationState> units, int slot)
        {
            foreach (var unit in units)
            {
                if (unit.Slot == slot) return unit;
            }
            return null;
        }

        private int PlayerUnitCount()
        {
            return runRoster.BoardCount;
        }

        private static GUIStyle Style(int fontSize, Color color, TextAnchor alignment, FontStyle fontStyle = FontStyle.Normal)
        {
            return new GUIStyle(GUI.skin.label)
            {
                fontSize = fontSize,
                normal = { textColor = color },
                alignment = alignment,
                fontStyle = fontStyle,
                wordWrap = true
            };
        }

        private GUIStyle ButtonStyle(UiButtonSemantic semantic, int fontSize)
        {
            return ActiveUiSkin.Button(semantic, fontSize, GUI.enabled);
        }

        private GUIStyle TabStyle(bool active, int fontSize)
        {
            return ActiveUiSkin.Tab(active, fontSize);
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D cached;
            if (TextureCache.TryGetValue(color, out cached))
            {
                return cached;
            }
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            TextureCache[color] = texture;
            return texture;
        }

        private sealed class BattleFloatingNumber
        {
            public BattleEventType Type;
            public int Amount;
            public Vector2 TargetPosition;
            public float DurationSeconds;
            public float ElapsedSeconds;
        }

        private sealed class BattleFeedbackVisual
        {
            public string TargetId;
            public string ProfileId;
            public Vector2 Position;
            public float Width;
            public float DurationSeconds;
            public float ElapsedSeconds;
            public float StartDelaySeconds;
            public float HeightOverride;
            public Vector2 Motion;
            public float RenderScale;
        }

        private sealed class UnitData
        {
            public string NameKey;
            public int Power;
            public int BasePower;
            public int RunLevel;
            public Color Color;
            public int MaxHealth;
            public int AttackDamage;
            public int HealAmount;
            public int Armor;
            public CombatSpeedTier SpeedTier;
            public int OpeningShield;
            public bool IsHealer;

            public static UnitData Create(
                CharacterDefinition definition,
                int power,
                Color color,
                int runLevel = 1,
                CombatStatBlock calculatedStats = null)
            {
                var stats = calculatedStats ?? definition.CalculateCombatStats(power, runLevel);
                return new UnitData
                {
                    NameKey = definition.DefinitionId,
                    Power = power,
                    BasePower = power,
                    RunLevel = runLevel,
                    Color = color,
                    MaxHealth = stats.MaxHealth,
                    AttackDamage = stats.AttackDamage,
                    HealAmount = stats.HealAmount,
                    Armor = stats.Armor,
                    SpeedTier = stats.SpeedTier,
                    IsHealer = stats.HealAmount > 0
                };
            }
        }

        private enum ScreenView
        {
            Home,
            Store,
            Deck,
            Arena,
            Leaderboard,
            Harbor,
            Expedition,
            Warehouse,
            Defense
        }

        private enum WarehouseTab
        {
            All,
            Equipment,
            Materials
        }
    }
}

