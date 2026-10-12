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
    public sealed partial class PrototypeBootstrap : MonoBehaviour
    {
        private const float DesignWidth = 1080f;
        private const float DesignHeight = 1920f;
        private const float ContentLeft = 55f;
        private const float ContentRight = 1025f;
        private const float ContentWidth = ContentRight - ContentLeft;
        private const float HeaderButtonWidth = 145f;
        private const float HeaderButtonGap = 10f;
        private const string ClientVersion = "0.43.413"; // Visible build identity; keep synchronized.
        private const bool LanternWorkshopReleased = false;
        private const string ClientVersionLabel = "v" + ClientVersion;
        // The product-selected alternate keeps blacksmith and lantern workshop disabled on the
        // accepted gray construction plaque. Both variants use the same five destination modules.
        private const HarborPageVariant ActiveHarborPageVariant =
            HarborPageVariant.ConstructionAlternate;
        private const float BattleUnitBaseAlignmentDropPercent = 0.05f;
        private static readonly Dictionary<string, Sprite> battleVfxSprites =
            new Dictionary<string, Sprite>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> battleShieldOverlayOpacities =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, float> battleHealthLagPercents =
            new Dictionary<string, float>(StringComparer.Ordinal);
        private readonly Dictionary<string, int> persistentMilaHiddenSpringSeeds =
            new Dictionary<string, int>(StringComparer.Ordinal);
        private readonly Dictionary<string, string> diagnosticQualityOverrides =
            new Dictionary<string, string>(StringComparer.Ordinal);
        private const int RunEconomyRulesVersion = 2;
        private const int RoguelikeRulesVersion = RoguelikeEnhancementRules.RulesVersion;
        private const int AttackSpeedRulesVersion = 1;
        private const int FinalRound = ExpeditionMapGraph.ChapterLayerCount;
        private const int MinimumRecruitCost = 3;
        private const int ShopOfferCount = 3;
        private const int MinimumDeckSize = 3;
        private const int MaximumDeckSize = 6;
        private const int BenchCapacity = 6;
        private const int ShopRefreshCost = 2;
        private const string DeckChangeRulesVersion = "deck-loadout.v1";
        private static readonly Rect WarehouseEquipmentDetailPanel =
            EquipmentDetailAFormalArtContract.ViewPanelRect;
        private static readonly Rect WarehouseLanternDetailPanel = LanternDetailAFormalArtContract.PanelRect;
        // v46 migrates ten-life local runs to the five-life contract while preserving losses taken.
        private const int SaveSchemaVersion = 48;
        private const int FiveLifeSaveSchemaVersion = 46;
        private const int MinimumCompatibleSaveVersion = 5;
        private const string SaveKey = "seeker-auto-chess.prototype.run.v1";
        private const string LanguagePreferenceKey = "seeker-auto-chess.preference.language.v1";
        private const string ArenaChallengeTestResetPreferenceKey =
            "seeker-auto-chess.test.arena-challenge-reset.v0.43.232";
        private const string MarketAcceptanceGrantPreferenceKey =
            "seeker-auto-chess.test.market-acceptance-grant.v0.43.281";
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
        private readonly CharacterFavoriteRegistry characterFavorites = new CharacterFavoriteRegistry();
        private EquipmentCatalog equipmentCatalog;
        private EquipmentInventory equipmentInventory;
        private ExpeditionRewardLedger expeditionRewards = new ExpeditionRewardLedger();
        private ChapterLighthouseProgressState chapterOneLighthouse = new ChapterLighthouseProgressState();
        private LanternInventory lanternInventory = new LanternInventory();
        private LanternSnapshot expeditionLanternSnapshot =
            LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
        private ExpeditionRunRewardSnapshot expeditionRewardSnapshot = CreateLegacyLocalRewardSnapshot();
        private Dictionary<string, EquipmentLoadoutSnapshot> runEquipmentSnapshots =
            new Dictionary<string, EquipmentLoadoutSnapshot>(StringComparer.Ordinal);
        private string selectedEquipmentCharacterInstanceId;
        private RunDeckSelection nextRunDeckSelection;
        private RunDeckSelection deckDraftSelection;
        private EquipmentAssignmentDraft deckEquipmentDraft;
        private int deckDraftAutomaticUnequipCount;
        private RunDeckSnapshot runDeck;
        private RunRoster runRoster = new RunRoster(6, BenchCapacity);
        private string[] shopOffers = new string[ShopOfferCount];
        private int[] shopOfferLevels = new int[ShopOfferCount];
        private string[] shopOfferIds = new string[ShopOfferCount];
        private int[] shopOfferListedPrices = new int[ShopOfferCount];
        private bool[] shopOfferSold = new bool[ShopOfferCount];
        private int shopRefreshSerial;
        private string runId = string.Empty;
        private int currentRunEconomyRulesVersion = RunEconomyRulesVersion;
        private int qualityRecruitPriceRulesVersion = QualityRecruitPriceRules.CurrentRulesVersion;
        private readonly RunEconomyActionLedger runEconomyActions = new RunEconomyActionLedger();
        private readonly RoguelikeBuildState roguelikeBuild = new RoguelikeBuildState();
        private readonly ChapterOneRoguelikeV11State roguelikeBuildV11 =
            new ChapterOneRoguelikeV11State();
        private int currentRoguelikeRuntimeGeneration =
            ChapterOneRoguelikeV11State.RuntimeGeneration;
        private string pendingP09FirstOptionId;
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
        private bool audioDiagnosticsOpen;
        private long audioDiagnosticSequence;
        private readonly BattleAudioEventRouter battleAudioRouter = new BattleAudioEventRouter();
        private readonly UiAudioEventRouter uiAudioRouter = new UiAudioEventRouter();
        private UiMainClickAudioPlayer uiMainClickAudioPlayer;
        private BattleAudioPlayer battleAudioPlayer;
        private HomeBackgroundMusicPlayer homeBackgroundMusicPlayer;
        private long battleAudioEventSequence;
        private readonly HashSet<string> consumedBattleAudioActionKeys =
            new HashSet<string>(StringComparer.Ordinal);
        private bool c01SkillDiagnosticActive;
        private string c01SkillDiagnosticQuality;
        private bool c02SkillDiagnosticActive;
        private string c02SkillDiagnosticQuality;
        private bool c03SkillDiagnosticActive;
        private string c03SkillDiagnosticQuality;
        private bool c04SkillDiagnosticActive;
        private string c04SkillDiagnosticQuality;
        private bool c05SkillDiagnosticActive;
        private string c05SkillDiagnosticQuality;
        private bool c06SkillDiagnosticActive;
        private string c06SkillDiagnosticQuality;
        private bool b1SkillDiagnosticActive;
        private bool b2SkillDiagnosticActive;
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
        private readonly Dictionary<int, string> c05BarrierOwnerByColumn =
            new Dictionary<int, string>();
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
        private GameLanguage language;
        private int coins = 10;
        private int health = ChapterOneExpeditionLifeRules.InitialLife;
        private int round = 1;
        private int selectedRunSlot = -1;
        private bool preparationShopOpen;
        private bool preparationUnitDetailOpen;
        private UnitData arenaFormationDetailUnit;
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
        private int storePurchaseSerial;
        private int storeEpicPitySkr;
        private int storeLegendaryPitySkr;
        private Vector2 storeCatalogScroll;
        private bool storeCatalogPointerHeld;
        private bool storeCatalogPointerDragging;
        private Vector2 storeCatalogPointerDown;
        private Vector2 storeCatalogPointerLast;
        private readonly List<string> ownedBasePackIds = new List<string>();
        private string equippedBasePackProductId;
        private string equippedBattlefieldProductId;
        private readonly IWalletSignatureGateway walletSignatureGateway =
            new LocalSkrWalletSignatureGateway();
        private string storeStatusMessage = "store.status.ready";
        private float storePurchaseNoticeUntil;
        private bool storeOpeningActive;
        private float storeOpeningProgress;
        private float storeRevealLoadingElapsed;
        private bool storeRevealAssetsReady;
        private bool storeRevealPreloadComplete;
        private Coroutine storeRevealPreparationCoroutine;
        private int storeRevealSelectedIndex = 1;
        private string pendingStoreOrderId;
        private string[] pendingStoreInstanceIds = new string[0];
        private string[] pendingStoreDefinitionIds = new string[0];
        private int[] pendingStoreQualities = new int[0];
        private int[] pendingStoreAptitudeRolls = new int[0];
        private bool pendingStoreRevealAlreadyGranted;
        private string pendingStarterGrantId;
        private string acknowledgedStarterGrantId;
        private bool currentExpeditionRewardsEnabled = true;
        private bool expeditionEntryCommitted;
        private int tideCrystalBalance;
        private int nextLocalEquipmentSerial = 1;
        private ScreenView currentView = ScreenView.Home;
        private float homeLanternUnlockHintUntil;
        private bool deckEditing;
        private bool characterDetailOpen;
        private bool characterDetailReturnToDeckEditing;
        private DeckCharacterDetailMode deckCharacterDetailMode;
        private string detailCharacterInstanceId;
        private string detailEquipmentPreviewId;
        private EquipmentSlot detailEquipmentSlot = EquipmentSlot.Weapon;
        private readonly Dictionary<EquipmentSlot, string> detailEquipmentPreviewBySlot =
            new Dictionary<EquipmentSlot, string>();
        private const string EmptyEquipmentPreviewId = "__character_detail_empty__";
        private Vector2 deckCollectionScroll;
        private DeckCollectionSortMode deckCollectionSortMode = DeckCollectionSortMode.Quality;
        private bool deckCollectionSortDescending = true;
        private Vector2 deckCollectionPointerDown;
        private Vector2 deckCollectionPointerLast;
        private bool deckCollectionPointerHeld;
        private bool deckCollectionPointerDragging;
        private Vector2 detailEquipmentScroll;
        private Vector2 warehouseEquipmentScroll;
        private Vector2 warehousePointerDown;
        private Vector2 warehousePointerLast;
        private bool warehousePointerHeld;
        private bool warehousePointerDragging;
        private readonly string[] warehouseSlotTokens =
            new string[WarehouseStorageRules.InitialCapacity];
        private readonly List<string> warehouseAcquisitionOrder = new List<string>();
        private WarehouseSortMode warehouseSortMode = WarehouseSortMode.Time;
        private int warehouseDragSourceSlot = -1;
        private string warehouseDraggedToken;
        private Vector2 warehouseItemPointerDown;
        private Vector2 warehouseItemPointerLast;
        private bool warehouseItemPointerHeld;
        private bool warehouseItemPointerDragging;
        private Vector2 creditsScroll;
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
        private bool warehouseLanternDetailOpen;
        private string warehouseLanternDetailId;
        private bool warehouseCosmeticDetailOpen;
        private string warehouseCosmeticDetailProductId;
        private bool lanternDetailCovenantPage;
        private int warehouseLanternStagePopup = -1;
        private LanternChangeAction pendingLanternChangeAction;
        private string pendingLanternChangeInstanceId;
        private string deckEditorMessageKey = "deck.editorHint";
        private bool deckSaveConfirmationOpen;
        private int selectedDefenseSlot = -1;
        private string defenseMessageKey = "defense.subtitle";
        private int pendingDefenseRemovedCount;
        private int selectedHarborFeatureIndex = -1;
        private int leaderboardTab;
        private Vector2 leaderboardScroll;
        private ArenaPageState arenaPageState;
        private ArenaOpponentProfile[] arenaOpponents = new ArenaOpponentProfile[0];
        private int selectedArenaOpponentIndex = -1;
        private Vector2 arenaRankingScroll;
        private bool arenaRankingScrollInitialized;
        private bool arenaRankingPointerHeld;
        private bool arenaRankingPointerDragging;
        private Vector2 arenaRankingPointerLast;
        private int arenaRankingContentCount;
        private int arenaRating = ArenaPrototypeRules.TemporaryInitialRating;
        private int arenaChallengesRemaining = ArenaPrototypeRules.DailyChallengeLimit;
        private string arenaChallengeDayKey;
        private string arenaBattleId = string.Empty;
        private LanternSnapshot arenaAttackLanternSnapshot =
            LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
        private LanternSnapshot arenaDefenseLanternSnapshot =
            LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
        private int arenaDefenseSnapshotVersion;
        private bool arenaDefenseNeedsResync;
        private readonly UnitData[] arenaAttackSlots = new UnitData[ArenaPrototypeRules.DefenseSlotCount];
        private readonly UnitData[] arenaAttackBenchSlots = new UnitData[BenchCapacity];
        private readonly UnitData[] defenseBenchSlots = new UnitData[BenchCapacity];
        private int selectedArenaAttackSlot = -1;
        private int arenaPreviousRating;
        private int arenaRatingDelta;
        private bool arenaLastBattleWon;
        private int arenaBattlePowerSnapshot;
        private bool arenaResultCommitted;
        private bool arenaResultReturnPending;
        private bool arenaChallengeTestResetChecked;
        private bool arenaFormationDetailUsesPlayerLantern;
        private bool arenaBattleActive;
        private bool harborEvidenceCaptureActive;
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
            harborEvidenceCaptureActive = IsHarborEvidenceCaptureRequested();
            EnsureRuntimeState();
            uiMainClickAudioPlayer = UiMainClickAudioPlayer.CreateOrGet(uiAudioRouter);
            battleAudioPlayer = BattleAudioPlayer.CreateOrGet(battleAudioRouter, uiAudioRouter.Mixer);
            homeBackgroundMusicPlayer = HomeBackgroundMusicPlayer.CreateOrGet(uiAudioRouter.Mixer);
            localization = PrototypeLocalization.CreateDefault();
            language = DetectSystemLanguage();
            GameLanguage savedLanguage;
            if (PrototypeLocalization.TryParseLanguageCode(
                    PlayerPrefs.GetString(LanguagePreferenceKey, string.Empty), out savedLanguage))
            {
                language = savedLanguage;
            }
            storeStatusMessage = T("store.status.ready");
            if (!LoadGame())
            {
                CreateNewExpeditionMap();
                runId = "run-" + Guid.NewGuid().ToString("N");
                CaptureCurrentRunContracts();
                runEconomyActions.Reset();
                shopRefreshSerial = 0;
                RefreshAllShopOffers();
                expeditionFlow.BeginAtRoute();
                CreateEnemies();
                CreateStarterDefense();
                if (!harborEvidenceCaptureActive) SaveGame();
            }
            preparationShopOpen = expeditionFlow.CanPrepare;
            if (storeOpeningActive) BeginStoreRevealPreparation(false);
            TryStartHarborEvidenceCapture();
            TryConfigureMainnetInviteRuntime();
            TryAttachWalletLanDiagnostic();
        }

        private void TryStartHarborEvidenceCapture()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!harborEvidenceCaptureActive) return;
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < arguments.Length; index++)
            {
                if (!string.Equals(arguments[index], "-capture-harbor-output",
                        StringComparison.OrdinalIgnoreCase)) continue;
                currentView = ScreenView.Harbor;
                language = GameLanguage.ChineseSimplified;
                Screen.SetResolution(1080, 2400, false);
                StartCoroutine(CaptureHarborEvidence(arguments[index + 1]));
                break;
            }
#endif
        }

        private static bool IsHarborEvidenceCaptureRequested()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            var arguments = Environment.GetCommandLineArgs();
            for (var index = 0; index + 1 < arguments.Length; index++)
            {
                if (string.Equals(arguments[index], "-capture-harbor-output",
                        StringComparison.OrdinalIgnoreCase)) return true;
            }
#endif
            return false;
        }

        private IEnumerator CaptureHarborEvidence(string outputPath)
        {
            // Allow resolution, font atlases, textures, and the selected navigation
            // state to settle before reading the standalone player's frame buffer.
            for (var frame = 0; frame < 30; frame++) yield return null;
            yield return new WaitForSecondsRealtime(1f);
            yield return new WaitForEndOfFrame();
            Texture2D screenshot = null;
            try
            {
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                if (screenshot == null || screenshot.width != 1080 || screenshot.height != 2400)
                    throw new InvalidOperationException("Harbor evidence frame is not 1080x2400");
                var directory = System.IO.Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directory)) System.IO.Directory.CreateDirectory(directory);
                System.IO.File.WriteAllBytes(outputPath, screenshot.EncodeToPNG());
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Application.Quit(2);
                yield break;
            }
            finally
            {
                if (screenshot != null) Destroy(screenshot);
            }
            Application.Quit(0);
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SaveGame();
            else UpdateWalletServerSessionLifecycle(true);
        }

        private void OnApplicationQuit()
        {
            ShutdownWalletServerRuntime();
            CancelBountyTowerRefresh();
            if (!SkillDiagnosticActive && !harborEvidenceCaptureActive) SaveGame();
        }

        private void Update()
        {
            UpdateWalletServerSessionLifecycle();
            if (homeBackgroundMusicPlayer == null)
                homeBackgroundMusicPlayer = HomeBackgroundMusicPlayer.CreateOrGet(uiAudioRouter.Mixer);
            var arenaResultShowing = currentView == ScreenView.Arena &&
                                     arenaPageState == ArenaPageState.Result;
            homeBackgroundMusicPlayer.SetTrack(BackgroundMusicPolicy.ResolveTrack(
                currentView == ScreenView.Expedition,
                arenaBattleActive || (battlePresentationActive && !arenaResultShowing)));
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
            if (storeOpeningActive)
            {
                storeRevealLoadingElapsed += Time.unscaledDeltaTime;
                if (!storeRevealAssetsReady && StoreRevealPresentationPolicy.CanBeginCardReveal(
                        storeRevealPreloadComplete, storeRevealLoadingElapsed))
                {
                    storeRevealAssetsReady = true;
                    storeOpeningProgress = 0f;
                }
                if (storeRevealAssetsReady)
                {
                    storeOpeningProgress = Mathf.Clamp01(storeOpeningProgress +
                        Time.unscaledDeltaTime / StoreRevealPresentationPolicy.CardRevealDurationSeconds);
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
            if (characterCatalog == null) characterCatalog = CharacterCatalog.CreatePrototype();
            if (playerCollection == null) playerCollection = PlayerCollection.CreatePrototype(characterCatalog);
            if (equipmentCatalog == null) equipmentCatalog = EquipmentCatalog.CreatePrototype();
            if (equipmentInventory == null)
                equipmentInventory = new EquipmentInventory(
                    equipmentCatalog, Array.Empty<EquipmentInstanceState>());
            if (expeditionRewards == null) expeditionRewards = new ExpeditionRewardLedger();
            if (expeditionEconomy == null) expeditionEconomy = new ExpeditionEconomyState();
            if (chapterOneLighthouse == null) chapterOneLighthouse = new ChapterLighthouseProgressState();
            if (lanternInventory == null) lanternInventory = new LanternInventory();
            EnsureMarketState();
            if (expeditionLanternSnapshot == null)
                expeditionLanternSnapshot = LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
            if (expeditionRewardSnapshot == null)
                expeditionRewardSnapshot = CreateLegacyLocalRewardSnapshot();
            if (arenaAttackLanternSnapshot == null)
                arenaAttackLanternSnapshot = LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
            if (arenaDefenseLanternSnapshot == null)
                arenaDefenseLanternSnapshot = LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
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
                    playerCollection, PreferredFormalDeckInstanceIds(), MaximumDeckSize, MinimumDeckSize);
            }
            if (benchSlots == null || benchSlots.Length != BenchCapacity)
            {
                benchSlots = new UnitData[BenchCapacity];
            }
            if (runRoster == null || runRoster.BoardCapacity != 6 || runRoster.BenchCapacity != BenchCapacity)
            {
                runRoster = new RunRoster(6, BenchCapacity);
            }
            RefreshArenaRuntimeState();
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
            currentEncounter = expeditionRewardSnapshot != null &&
                string.Equals(expeditionRewardSnapshot.RulesVersion,
                    ChapterOneDifficultyRules.RulesVersion, StringComparison.Ordinal)
                ? ChapterOneDifficultyRules.GenerateEncounter(
                    expeditionMapSeed, node, expeditionRewardSnapshot.DifficultyId,
                    CompletedBattleCount(), CurrentEncounterTableVersion())
                : ChapterOneEncounterRules.Generate(
                    expeditionMapSeed, node, !HasCompletedNormalBattle());
            if (b1SkillDiagnosticActive || b2SkillDiagnosticActive)
            {
                var bossId = b1SkillDiagnosticActive ? "enemy.b1.heavy" : "enemy.b2.hunter";
                var generated = new List<ChapterOneEncounterUnit>
                {
                    new ChapterOneEncounterUnit(bossId, 1),
                    new ChapterOneEncounterUnit("enemy.n2.fighter", 1),
                    new ChapterOneEncounterUnit("enemy.n3.ranged", 1),
                    new ChapterOneEncounterUnit(b1SkillDiagnosticActive
                        ? "enemy.n4.skirmisher" : "enemy.n1.guard", 1)
                };
                currentEncounter = new ChapterOneEncounter(
                    currentEncounter.NodeId, currentEncounter.GenerationVersion,
                    currentEncounter.BattleSeed, currentEncounter.CoinReward,
                    currentEncounter.IsElite, true, generated);
            }
            encounterReward = currentEncounter.CoinReward;
            currentEncounterIsElite = currentEncounter.IsElite;
            foreach (var encounterUnit in currentEncounter.Units)
            {
                var enemyUnit = CreateUnit(
                    encounterUnit.DefinitionId,
                    UnitBasePower(encounterUnit.DefinitionId),
                    EnemyColor(encounterUnit.DefinitionId),
                    encounterUnit.RunLevel);
                if (b2SkillDiagnosticActive &&
                    string.Equals(encounterUnit.DefinitionId, "enemy.b2.hunter", StringComparison.Ordinal))
                    enemyUnit.MaxHealth *= 4;
                enemyUnits.Add(enemyUnit);
            }
        }

        private static ExpeditionRunRewardSnapshot CreateLegacyLocalRewardSnapshot()
        {
            return new ExpeditionRunRewardSnapshot(
                "D0", "local-prototype", "none",
                new ExpeditionRewardTableReference("legacy.local.crystal", "legacy.v1"),
                new ExpeditionRewardTableReference("legacy.local.equipment", "legacy.v1"),
                new ExpeditionRewardTableReference("legacy.local.lantern-none", "legacy.v1"),
                "legacy.local.run-reward.v1");
        }

        private static ExpeditionRunRewardSnapshot CreateCurrentLocalD0RewardSnapshot()
        {
            var rewardVersion = ChapterOneDifficultyRules.RewardTableVersion;
            return new ExpeditionRunRewardSnapshot(
                "D0", "local-prototype-no-authority", "none",
                new ExpeditionRewardTableReference("chapter01.full-clear.currency", rewardVersion),
                new ExpeditionRewardTableReference("chapter01.full-clear.equipment", rewardVersion),
                new ExpeditionRewardTableReference("chapter01.full-clear.lantern", rewardVersion),
                ChapterOneDifficultyRules.RulesVersion);
        }

        private void CaptureCurrentRunContracts()
        {
            currentRunEconomyRulesVersion = RunEconomyRulesVersion;
            qualityRecruitPriceRulesVersion = QualityRecruitPriceRules.CurrentRulesVersion;
            expeditionLanternSnapshot = lanternInventory.CreateActiveSnapshot(
                LanternLegacyMigrationResult.LegacyNoneRulesVersion);
            expeditionRewardSnapshot = CreateCurrentLocalD0RewardSnapshot();
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

        private int CompletedBattleCount()
        {
            if (expeditionMap == null || expeditionRewards == null) return 0;
            var count = 0;
            foreach (var nodeId in expeditionRewards.CompletedNodeIds)
            {
                var node = expeditionMap.GetNode(nodeId);
                if (node != null && IsBattleNode(node.Type)) count++;
            }
            return count;
        }

        private void OnGUI()
        {
            // IMGUI state can survive a native Android surface recreation. Always
            // restore interactivity before drawing the next frame.
            GUI.enabled = true;
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

            if (ShouldDrawMainnetInviteLogin)
            {
                DrawLoginFormalScreen();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
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
            if (currentView == ScreenView.Profile && !battlePresentationActive)
            {
                // Profile E owns its complete 1080 x 2400 source canvas. On shorter
                // devices the page scrolls vertically instead of scaling or cropping
                // any accepted art module.
                DrawProfileFormalScreen();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Home && !battlePresentationActive)
            {
                // The approved home is a pixel-locked 1080 x 2400 composition. Its
                // environment layers and native-size modules share the same canvas and
                // deliberately do not inherit the older 9:16 safe-area translation.
                var badgePopupOpen = firstLightseekerBadgePopupOpen;
                var tutorialOpen = IsGameplayTutorialOpen && !badgePopupOpen;
                GUI.enabled = !tutorialOpen && !badgePopupOpen;
                DrawHomeScreen();
                DrawMainNavigation();
                GUI.enabled = true;
                if (badgePopupOpen) DrawFirstLightseekerBadgePopup();
                else if (tutorialOpen) DrawGameplayTutorialOverlay();
                DrawBuildVersionOverlay(layoutSafeTop);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Store && !battlePresentationActive)
            {
                // Store E is authored against the same locked 1080 x 2400 canvas as
                // the accepted bottom navigation. Do not apply the legacy safe-area
                // translation a second time or the formal modules drift vertically.
                var openingModal = storeOpeningActive;
                DrawStoreScreen(!openingModal);
                GUI.enabled = !openingModal;
                DrawMainNavigation();
                GUI.enabled = true;
                if (openingModal) DrawStoreOpeningOverlay();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (IsFormalArenaPage)
            {
                // The accepted Arena E page and its horizontal opponent detail are
                // authored on the complete 1080 x 2400 canvas, including navigation.
                DrawArenaScreen();
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Deck && !battlePresentationActive)
            {
                // Deck A v1.0.6 is authored on the complete locked 1080 x 2400 canvas.
                // Keep its native modules outside the legacy safe-area translation.
                DrawDeckScreen();
                if (!deckEditing && !characterDetailOpen) DrawMainNavigation();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Harbor && !battlePresentationActive)
            {
                // Harbor A is authored on the full locked 1080 x 2400 canvas. The page
                // background continues behind the shared bottom navigation; the page
                // itself owns only y=0..2157 and never redraws the navigation assets.
                DrawHarborScreen();
                DrawMainNavigation();
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Leaderboard && !battlePresentationActive)
            {
                // Bounty Tower A owns the complete locked 1080 x 2400 canvas and its own
                // return interaction. It deliberately does not draw the shared bottom menu.
                DrawLeaderboardScreen();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Market && !battlePresentationActive)
            {
                // Market is authored against the complete 1080 x 2400 canvas. Routing it
                // before the legacy safe-area translation prevents the Android top inset
                // from being applied twice and shifting the whole page downward.
                DrawMarketScreen();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Warehouse && !battlePresentationActive)
            {
                if (lanternWorkshopMode)
                {
                    DrawLanternWorkshopScreen();
                    DrawBuildVersionOverlay(0f);
                    GUI.backgroundColor = previousBackgroundColor;
                    return;
                }
                // Warehouse A is authored on the complete locked 1080 x 2400 canvas.
                // It owns its return control and deliberately does not use the legacy header.
                if (warehouseEquipmentDetailOpen || warehouseLanternDetailOpen ||
                    warehouseCosmeticDetailOpen || firstLightseekerBadgeDetailOpen)
                {
                    GUI.enabled = false;
                    DrawWarehouseScreen();
                    GUI.enabled = true;
                    DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f),
                        MarketFormalModuleRoot, "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
                    if (firstLightseekerBadgeDetailOpen)
                        DrawFirstLightseekerBadgeDetail();
                    else
                    {
                        if (pendingLanternChangeAction == LanternChangeAction.None &&
                            DrawWarehouseEquipmentDetailBackdrop(warehouseLanternDetailOpen
                                ? WarehouseLanternDetailPanel : CurrentWarehouseEquipmentDetailPanel())) return;
                        if (warehouseLanternDetailOpen) DrawWarehouseLanternDetailScreen();
                        else if (warehouseCosmeticDetailOpen) DrawWarehouseCosmeticDetailScreen();
                        else DrawWarehouseEquipmentDetailScreen();
                    }
                }
                else DrawWarehouseScreen();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            GUI.matrix = rootMatrix * Matrix4x4.Translate(new Vector3(0f, layoutSafeTop, 0f));
            if (currentView == ScreenView.Expedition && expeditionFlow != null &&
                expeditionFlow.IsComplete && !battlePresentationActive)
            {
                // Keep the final battlefield as the result backdrop, then return to the
                // locked 1080x2400 root canvas so the accepted settlement shell is not
                // shifted down a second time by the Android safe-area transform.
                GUI.enabled = false;
                DrawBattleScreen(1f);
                GUI.enabled = true;
                GUI.matrix = rootMatrix;
                DrawExpeditionResultScreen();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
            if (currentView == ScreenView.Arena && arenaPageState == ArenaPageState.Result)
            {
                // Result A must preserve the exact final live battlefield rather than
                // navigating to the retired standalone result page. Freeze and draw the
                // retained battle presentation under a full-canvas modal layer.
                GUI.enabled = false;
                DrawBattleScreen(1f);
                GUI.enabled = true;
                GUI.matrix = rootMatrix;
                DrawArenaResult();
                DrawBuildVersionOverlay(0f);
                GUI.backgroundColor = previousBackgroundColor;
                return;
            }
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
            if (IsArenaFormationScreen && arenaFormationDetailUnit != null)
            {
                GUI.matrix = rootMatrix;
                DrawArenaFormationUnitDetail();
                GUI.matrix = rootMatrix * Matrix4x4.Translate(new Vector3(0f, layoutSafeTop, 0f));
            }
            DrawBuildVersionOverlay(0f);
            GUI.backgroundColor = previousBackgroundColor;
        }

        private void DrawBuildVersionOverlay(float topOffset)
        {
            // Dynamic text is allowed on formal pages; no code-drawn backing shape may
            // replace the art-owned HUD surface underneath this build identifier.
            var previousColor = GUI.color;
            var previousContentColor = GUI.contentColor;
            GUI.color = Color.white;
            GUI.contentColor = Color.white;
            GUI.Label(new Rect(ContentRight - 170f, topOffset + 5f, 170f, 30f), ClientVersionLabel,
                Style(13, new Color(0.91f, 0.90f, 0.84f), TextAnchor.MiddleRight));
            GUI.color = previousColor;
            GUI.contentColor = previousContentColor;
        }

        // Returns -1 while open, 0 for the secondary action and 1 for the primary action.
        private int DrawCommonConfirmDialog(string title, string body, string supplemental,
            string secondaryLabel, string primaryLabel, bool primaryEnabled = true)
        {
            var panel = CommonConfirmFormalArtContract.PanelRect;
            DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f), MarketFormalModuleRoot,
                "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            DrawMarketModalInputShieldFormal(panel);
            DrawMarketTextureFormal(panel, CommonConfirmFormalArtContract.ResourceRoot,
                CommonConfirmFormalArtContract.DialogShell);

            var titleArea = CommonConfirmLocalRect(panel, CommonConfirmFormalArtContract.TitleLocal);
            var bodyArea = CommonConfirmLocalRect(panel, CommonConfirmFormalArtContract.BodyLocal);
            var supplementalArea = CommonConfirmLocalRect(panel,
                CommonConfirmFormalArtContract.SupplementalLocal);
            GUI.Label(titleArea, title, CommonConfirmTextStyle(title, titleArea,
                38, 28, MarketInk, true, false));
            GUI.Label(bodyArea, body, CommonConfirmTextStyle(body, bodyArea,
                28, 21, MarketInk, false, true));
            if (!string.IsNullOrEmpty(supplemental))
                GUI.Label(supplementalArea, supplemental,
                    CommonConfirmTextStyle(supplemental, supplementalArea,
                        23, 18, MarketMuted, false, false));

            if (string.IsNullOrEmpty(secondaryLabel))
            {
                var single = CommonConfirmLocalRect(panel,
                    CommonConfirmFormalArtContract.SingleButtonLocal);
                return DrawCommonConfirmButton(single, primaryLabel, true, primaryEnabled) ? 1 : -1;
            }

            var secondary = CommonConfirmLocalRect(panel,
                CommonConfirmFormalArtContract.SecondaryButtonLocal);
            var primary = CommonConfirmLocalRect(panel,
                CommonConfirmFormalArtContract.PrimaryButtonLocal);
            if (DrawCommonConfirmButton(secondary, secondaryLabel, false, true)) return 0;
            return DrawCommonConfirmButton(primary, primaryLabel, true, primaryEnabled) ? 1 : -1;
        }

        private bool DrawCommonConfirmButton(Rect area, string label, bool primary, bool enabled)
        {
            var pressed = enabled && area.Contains(Event.current.mousePosition) && Input.GetMouseButton(0);
            var resourceId = primary
                ? !enabled ? CommonConfirmFormalArtContract.PrimaryDisabled :
                    pressed ? CommonConfirmFormalArtContract.PrimaryPressed :
                    CommonConfirmFormalArtContract.PrimaryNormal
                : !enabled ? CommonConfirmFormalArtContract.SecondaryDisabled :
                    pressed ? CommonConfirmFormalArtContract.SecondaryPressed :
                    CommonConfirmFormalArtContract.SecondaryNormal;
            DrawMarketTextureFormal(area, CommonConfirmFormalArtContract.ResourceRoot, resourceId);
            var textArea = new Rect(area.x + CommonConfirmFormalArtContract.ButtonTextLocal.x,
                area.y + CommonConfirmFormalArtContract.ButtonTextLocal.y,
                CommonConfirmFormalArtContract.ButtonTextLocal.width,
                CommonConfirmFormalArtContract.ButtonTextLocal.height);
            var color = enabled ? primary ? MarketIvory : MarketInk : MarketMuted;
            GUI.Label(textArea, label, CommonConfirmTextStyle(label, textArea,
                32, 20, color, true, false));
            return enabled && GUI.Button(area, GUIContent.none, GUIStyle.none);
        }

        private GUIStyle CommonConfirmTextStyle(string text, Rect area, int preferredSize,
            int minimumSize, Color color, bool bold, bool wordWrap)
        {
            var content = new GUIContent(text ?? string.Empty);
            for (var size = preferredSize; size >= minimumSize; size--)
            {
                var candidate = Style(size, color, TextAnchor.MiddleCenter,
                    bold ? FontStyle.Bold : FontStyle.Normal);
                candidate.wordWrap = wordWrap;
                candidate.clipping = TextClipping.Clip;
                var height = candidate.CalcHeight(content, area.width);
                var width = wordWrap ? area.width : candidate.CalcSize(content).x;
                if (width <= area.width && height <= area.height) return candidate;
            }
            var fallback = Style(minimumSize, color, TextAnchor.MiddleCenter,
                bold ? FontStyle.Bold : FontStyle.Normal);
            fallback.wordWrap = wordWrap;
            fallback.clipping = TextClipping.Clip;
            return fallback;
        }

        private static Rect CommonConfirmLocalRect(Rect panel, Rect local)
        {
            return new Rect(panel.x + local.x, panel.y + local.y, local.width, local.height);
        }

        private void DrawStandardInterface()
        {
            if (currentView == ScreenView.Home)
            {
                var previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && !firstLightseekerBadgePopupOpen;
                DrawHomeScreen();
                DrawMainNavigation();
                GUI.enabled = previousEnabled;
                DrawFirstLightseekerBadgePopup();
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
            if (currentView == ScreenView.Credits)
            {
                DrawCreditsScreen();
                return;
            }
            if (currentView == ScreenView.Market)
            {
                DrawMarketScreen();
                return;
            }
            DrawHeader();
            if (currentView == ScreenView.Deck)
            {
                DrawDeckScreen();
                if (!deckEditing && !characterDetailOpen) DrawMainNavigation();
                return;
            }
            if (currentView == ScreenView.Arena)
            {
                DrawArenaScreen();
                return;
            }
            if (currentView == ScreenView.Leaderboard)
            {
                DrawLeaderboardScreen();
                return;
            }
            if (currentView == ScreenView.Warehouse)
            {
                if (warehouseEquipmentDetailOpen || warehouseLanternDetailOpen ||
                    warehouseCosmeticDetailOpen || firstLightseekerBadgeDetailOpen)
                {
                    GUI.enabled = false;
                    DrawWarehouseScreen();
                    GUI.enabled = true;
                    DrawMarketTextureFormal(new Rect(0f, 0f, 1080f, 2400f),
                        MarketFormalModuleRoot, "ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
                    if (firstLightseekerBadgeDetailOpen)
                        DrawFirstLightseekerBadgeDetail();
                    else
                    {
                        if (pendingLanternChangeAction == LanternChangeAction.None &&
                            DrawWarehouseEquipmentDetailBackdrop(warehouseLanternDetailOpen
                                ? WarehouseLanternDetailPanel : CurrentWarehouseEquipmentDetailPanel())) return;
                        if (warehouseLanternDetailOpen) DrawWarehouseLanternDetailScreen();
                        else if (warehouseCosmeticDetailOpen) DrawWarehouseCosmeticDetailScreen();
                        else DrawWarehouseEquipmentDetailScreen();
                    }
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
                DrawHomeLighthouseCanvas(PrototypeArtResourceIds.HomeLighthouseBackground, false);
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseTowerUnlit,
                    HomeLighthouseV140Layout.TowerModule);
                if (chapterOneLighthouse.IsLit ||
                    (lanternInventory != null &&
                     !string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId)))
                    DrawHomeLighthouseCanvas(PrototypeArtResourceIds.HomeLighthouseLitOverlay, true);

                // z=30 chapter ornament.
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseChapterOrnament,
                    HomeLighthouseV140Layout.ChapterOrnamentModule);

                // z=40..42 HUD modules. The accepted left rail is one indivisible native-size asset.
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseEnergyGlyph,
                    HomeLighthouseV140Layout.EnergyGlyphModule);
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseSkrBadge,
                    HomeLighthouseV140Layout.SkrBadgeModule);
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseTideCrystal,
                    HomeLighthouseV140Layout.TideCrystalModule);
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseResourceSeparator,
                    HomeLighthouseV140Layout.ResourceSeparatorOneModule);
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseResourceSeparator,
                    HomeLighthouseV140Layout.ResourceSeparatorTwoModule);
                DrawHomeLeftCompleteModule();

                // z=45 lantern content is state-driven. When no lantern is equipped, the
                // approved panel cutout remains transparent and shows only the page background.
                var activeHomeLantern = GetActiveHomeLantern();
                if (activeHomeLantern != null)
                    DrawHomeLanternModularComponent(activeHomeLantern,
                        HomeLighthouseRect(HomeLighthouseV140Layout.LanternMedallionModule), "home");
                DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseLanternPanel,
                    HomeLighthouseV140Layout.LanternPanelModule);
                return;
            }
            if (battlePresentationActive ||
                (currentView == ScreenView.Arena && arenaPageState == ArenaPageState.Result) ||
                IsPreparationScreen || IsArenaFormationScreen ||
                IsPostBattleExpeditionScreen ||
                IsExpeditionEventFamilyNodeScreen)
            {
                // The interactive preparation slots are drawn from the same coordinates
                // as combat. Do not also paint the legacy baked deployment markers.
                var opacity = battlePresentationActive ? battleSceneOpacity : 1f;
                if (!DrawEquippedBattlefieldSkin(opacity))
                    battlefieldRenderer.Draw(new Rect(0f, 0f, DesignWidth, layoutCanvasHeight),
                        opacity, 0f);
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

        private bool DrawEquippedBattlefieldSkin(float opacity)
        {
            Texture2D texture;
            if (!TryGetEquippedBattlefieldTexture(out texture)) return false;
            var area = new Rect(0f, 0f, StoreCosmeticFormalArtContract.BattlefieldWidth,
                StoreCosmeticFormalArtContract.BattlefieldHeight);
            var previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
            return true;
        }

        private bool TryGetEquippedBattlefieldTexture(out Texture2D texture)
        {
            var resourceId = StoreCosmeticFormalArtContract.BattlefieldResourceId(
                equippedBattlefieldProductId);
            texture = string.IsNullOrEmpty(resourceId)
                ? null
                : artCatalog.GetStoreCosmeticTexture(resourceId, true);
            if (texture != null &&
                texture.width == StoreCosmeticFormalArtContract.BattlefieldWidth &&
                texture.height == StoreCosmeticFormalArtContract.BattlefieldHeight) return true;
            if (!string.IsNullOrEmpty(resourceId))
                Debug.LogError("Rejected missing or resized equipped battlefield: " + resourceId);
            texture = null;
            return false;
        }

        private bool IsArenaFormationScreen
        {
            get
            {
                return !battlePresentationActive &&
                       ((currentView == ScreenView.Arena && arenaPageState == ArenaPageState.Preparation) ||
                        currentView == ScreenView.Defense);
            }
        }

        private bool IsFormalArenaPage
        {
            get
            {
                return currentView == ScreenView.Arena && !battlePresentationActive &&
                       (arenaPageState == ArenaPageState.Ranking ||
                        arenaPageState == ArenaPageState.OpponentDetail ||
                        arenaPageState == ArenaPageState.ChallengeConfirm);
            }
        }

        private bool SkillDiagnosticActive
        {
            get { return c01SkillDiagnosticActive || c02SkillDiagnosticActive ||
                         c03SkillDiagnosticActive || c04SkillDiagnosticActive ||
                         c05SkillDiagnosticActive || c06SkillDiagnosticActive ||
                         b1SkillDiagnosticActive || b2SkillDiagnosticActive; }
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
            return type;
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
            if (!(currentView == ScreenView.Arena && arenaPageState == ArenaPageState.Result))
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
                    CharacterQualityResourceId(combatUnit.Quality),
                    out artDefinition, out characterSprite))
            {
                characterSprite = ResolveFormalBattleAnimationSprite(combatUnit, characterSprite);
                DrawBattleCharacterArt(combatUnit, area, artDefinition, characterSprite, opacity);
                return;
            }

            GUI.color = WithOpacity(new Color(0f, 0f, 0f, 0.45f), opacity);
            GUI.DrawTexture(new Rect(area.x + 10f, area.y + 12f, area.width, area.height), Texture2D.whiteTexture);
            GUI.color = WithOpacity(cardColor, opacity);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            DrawBattleUnitHud(combatUnit, area.center.x, area.y, area.width, opacity,
                PieceBaseBottomY(area.yMax - 5f, area.width, combatUnit.IsPlayer));
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
                visibleRect.width, combatUnit.IsPlayer);
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
                PieceBaseBottomY(footY, visibleRect.width, combatUnit.IsPlayer));
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
            bool defender,
            string baseSkinId = PieceBasePresentationContract.DefaultSkinId)
        {
            Texture2D equippedTexture;
            if (TryGetEquippedBaseTexture(defender, out equippedTexture))
            {
                var equippedArea = new Rect(
                    footPosition.x - StoreCosmeticFormalArtContract.BaseFootAnchorX,
                    footPosition.y - StoreCosmeticFormalArtContract.BaseFootAnchorY,
                    StoreCosmeticFormalArtContract.BaseWidth,
                    StoreCosmeticFormalArtContract.BaseHeight);
                var equippedColor = GUI.color;
                GUI.color = WithOpacity(Color.white, opacity);
                GUI.DrawTexture(equippedArea, equippedTexture, ScaleMode.StretchToFill, true);
                GUI.color = equippedColor;
                return;
            }

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

        private float PieceBaseBottomY(
            float footY, float visibleCharacterWidth, bool defender)
        {
            Texture2D equippedTexture;
            if (TryGetEquippedBaseTexture(defender, out equippedTexture))
                return footY + StoreCosmeticFormalArtContract.BaseHeight -
                       StoreCosmeticFormalArtContract.BaseFootAnchorY;
            return footY + PieceBasePresentationContract.DisplayHeightForVisibleCharacterWidth(
                visibleCharacterWidth) * (1f - PieceBasePresentationContract.FootAnchorTopNormalizedY);
        }

        private bool TryGetEquippedBaseTexture(bool defender, out Texture2D texture)
        {
            texture = null;
            var resourceId = StoreCosmeticFormalArtContract.BaseResourceId(
                equippedBasePackProductId, defender);
            if (string.IsNullOrEmpty(resourceId)) return false;
            texture = artCatalog.GetStoreCosmeticTexture(resourceId, false);
            return texture != null &&
                   texture.width == StoreCosmeticFormalArtContract.BaseWidth &&
                   texture.height == StoreCosmeticFormalArtContract.BaseHeight;
        }

        private void DrawBattleUnitHud(
            BattleUnitPresentationState combatUnit, float centerX, float visibleTopY,
            float meaningfulVisibleWidth, float opacity, float pieceBaseBottomY)
        {
            var healthRect = FormationStatusBarRect(combatUnit.DefinitionId, centerX,
                visibleTopY, meaningfulVisibleWidth);
            float lagHealthPercent;
            if (!battleHealthLagPercents.TryGetValue(combatUnit.Id, out lagHealthPercent))
                lagHealthPercent = combatUnit.HealthPercent;
            if (combatUnit.HealthPercent >= lagHealthPercent)
                lagHealthPercent = combatUnit.HealthPercent;
            else
                lagHealthPercent = Mathf.MoveTowards(lagHealthPercent,
                    combatUnit.HealthPercent, Time.unscaledDeltaTime * 0.32f);
            battleHealthLagPercents[combatUnit.Id] = lagHealthPercent;
            DrawBattleStatusBarV102(healthRect, combatUnit.HealthPercent, lagHealthPercent,
                combatUnit.CurrentHealth, combatUnit.MaxHealth, combatUnit.ShieldPercent,
                combatUnit.CurrentShield, FormationStatusCompartmentCount(combatUnit.MaxHealth),
                opacity * (combatUnit.IsAlive ? 1f : 0.44f));
            DrawPersistentMilaHiddenSpringSeeds(combatUnit.Id, healthRect, opacity);
            var caption = CharacterDisplayName(combatUnit.DefinitionId, combatUnit.Quality);
            const int nameFontSize = 23;
            var nameArea = FormationNameplateRect(caption, nameFontSize, centerX,
                pieceBaseBottomY);
            DrawFormationUnitNameplate(nameArea, caption, opacity, nameFontSize);
            GUI.color = Color.white;
        }

        private void DrawPersistentMilaHiddenSpringSeeds(string unitId, Rect healthRect, float opacity)
        {
            int count;
            if (!persistentMilaHiddenSpringSeeds.TryGetValue(unitId, out count) || count <= 0) return;
            var sprite = LoadBattleVfxSprite(BattleProjectileProfiles.ResourcePathFor(
                BattleProjectileProfileIds.HiddenSpringSeed));
            if (sprite == null || sprite.texture == null) return;
            const float iconWidth = 22f;
            const float gap = 4f;
            var totalWidth = count * iconWidth + (count - 1) * gap;
            var startX = healthRect.center.x - totalWidth * 0.5f;
            var y = healthRect.yMax + 3f;
            GUI.color = WithOpacity(Color.white, opacity);
            for (var index = 0; index < count; index++)
            {
                var iconHeight = iconWidth * sprite.rect.height / Mathf.Max(1f, sprite.rect.width);
                DrawStaticBattleSprite(new Rect(startX + index * (iconWidth + gap),
                    y, iconWidth, iconHeight), sprite, false);
            }
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
            if (IsArenaFormationScreen)
            {
                DrawArenaFormationHeader();
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
                    currentView == ScreenView.Warehouse &&
                    (warehouseEquipmentDetailOpen || warehouseLanternDetailOpen ||
                     warehouseCosmeticDetailOpen || firstLightseekerBadgeDetailOpen)
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
                        PlayUiClick(battlePaused ? UiAudioCueIds.Resume : UiAudioCueIds.Pause,
                            battlePaused ? "battle-resume-header" : "battle-pause-header");
                        battlePaused = !battlePaused;
                    }
                    GUI.enabled = true;
                }
                else if (GUI.Button(backArea, T(backKey), ButtonStyle(UiButtonSemantic.Secondary, 16)))
                {
                    PlayUiClick(UiAudioCueIds.Cancel, "page-back");
                    if (currentView == ScreenView.Deck)
                    {
                        characterDetailOpen = false;
                        characterDetailReturnToDeckEditing = false;
                        detailEquipmentPreviewId = null;
                    }
                    else if (currentView == ScreenView.Warehouse && warehouseEquipmentDetailOpen)
                    {
                        warehouseEquipmentDetailOpen = false;
                        warehouseEquipmentDetailId = null;
                    }
                    else if (currentView == ScreenView.Warehouse && warehouseLanternDetailOpen)
                    {
                        if (pendingLanternChangeAction != LanternChangeAction.None)
                        {
                            CloseLanternChangeConfirmation();
                            return;
                        }
                        warehouseLanternDetailOpen = false;
                        warehouseLanternDetailId = null;
                    }
                    else if (currentView == ScreenView.Warehouse && warehouseCosmeticDetailOpen)
                    {
                        warehouseCosmeticDetailOpen = false;
                        warehouseCosmeticDetailProductId = null;
                    }
                    else if (currentView == ScreenView.Warehouse && firstLightseekerBadgeDetailOpen)
                    {
                        firstLightseekerBadgeDetailOpen = false;
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
                        PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-header");
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
                if (GUI.Button(navigationArea, GUIContent.none, GUIStyle.none))
                {
                    PlayUiClick(battlePaused ? UiAudioCueIds.Resume : UiAudioCueIds.Pause,
                        battlePaused ? "battle-resume-formation" : "battle-pause-formation");
                    battlePaused = !battlePaused;
                }
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
                    PlayUiClick(UiAudioCueIds.Cancel, "formation-page-back");
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
                PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-formation");
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

        private void DrawArenaFormationHeader()
        {
            var navigationArea = new Rect(42f, 52f, 116f, 116f);
            DrawFormationBattleV94Texture(navigationArea,
                PrototypeArtResourceIds.FormationBattleV94BackFrame, ScaleMode.ScaleToFit, 1f);
            var glyphArea = new Rect(navigationArea.center.x - 32f, navigationArea.center.y - 32f, 64f, 64f);
            DrawFormationBattleV94Texture(glyphArea,
                PrototypeArtResourceIds.FormationBattleV94BackGlyph, ScaleMode.ScaleToFit, 1f);
            GUI.enabled = arenaFormationDetailUnit == null;
            if (GUI.Button(navigationArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "arena-formation-back");
                ResetSharedFormationPointer();
                CloseArenaFormationUnitDetail();
                if (currentView == ScreenView.Defense) currentView = ScreenView.Arena;
                else arenaPageState = ArenaPageState.OpponentDetail;
            }
            GUI.enabled = true;

            var title = currentView == ScreenView.Defense ? T("defense.title") : T("arena.preparation");
            GUI.Label(new Rect(180f, 55f, 620f, 62f), title,
                ArchiveStyle(30, ArchiveColor("F6E8C8"), TextAnchor.MiddleLeft, true));
            GUI.Label(new Rect(180f, 112f, 620f, 42f),
                currentView == ScreenView.Defense && pendingDefenseRemovedCount > 0
                    ? string.Format(T("defense.deckChangedRemoved"), pendingDefenseRemovedCount)
                    : currentView == ScreenView.Defense ? T(defenseMessageKey) : T("arena.preparationHint"),
                ArchiveStyle(16, ArchiveColor("A8C5C5"), TextAnchor.MiddleLeft, false));

            var statusArea = new Rect(798f, 64f, 250f, 103f);
            DrawSharedExpeditionTexture(statusArea, ExpeditionUiSharedFrames.CoinsHud, ScaleMode.ScaleToFit);
            var status = currentView == ScreenView.Defense
                ? string.Format(T("arena.power"), CurrentPvpDeckPower())
                : string.Format(T("arena.daily"), arenaChallengesRemaining, ArenaPrototypeRules.DailyChallengeLimit);
            GUI.Label(statusArea, status, ArchiveStyle(18, ArchiveColor("F6E8C8"), TextAnchor.MiddleCenter, true));
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
                    PlayUiClick(UiAudioCueIds.Tap, "formation-synergy-tile");
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
                    PlayUiClick(UiAudioCueIds.Tap, "formation-synergy-more");
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
                    PlayUiClick(UiAudioCueIds.Tap, "formation-synergy-overflow-row");
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
            DrawHomeDynamicHud();
            DrawHomeLighthouseText(HomeLighthouseV140Layout.Chapter, T("home.chapter"), 42,
                HomeLighthouseDeep, TextAnchor.MiddleCenter, true);
            DrawHomeLighthouseText(HomeLighthouseV140Layout.IslandName, T("home.island"), 31,
                HomeLighthouseDeep, TextAnchor.MiddleCenter, false);

            DrawHomeLanternPanel();

            var mustStartNewRun = expeditionFlow.IsComplete || runAbandoned || runWon;
            var hasProgress = !mustStartNewRun && HasExpeditionProgress();
            var startsNewRun = mustStartNewRun || !hasProgress;
            var startKey = startsNewRun && expeditionEnergy == 0
                ? "home.freeExpedition"
                : mustStartNewRun
                    ? "home.restartExpedition"
                    : hasProgress ? "home.continueExpedition" : "home.startExpedition";
            var actionRect = HomeLighthouseRect(HomeLighthouseV140Layout.ExpeditionButton);
            var entryEnabled = GUI.enabled && !formalExpeditionEntryInProgress;
            var pointerDown = Event.current != null && Event.current.type == EventType.MouseDown &&
                              actionRect.Contains(Event.current.mousePosition) && entryEnabled;
            var actionResource = entryEnabled
                ? pointerDown
                    ? PrototypeArtResourceIds.HomeLighthouseExpeditionPressed
                    : PrototypeArtResourceIds.HomeLighthouseExpeditionNormal
                : PrototypeArtResourceIds.HomeLighthouseExpeditionDisabled;
            var actionModule = entryEnabled
                ? pointerDown
                    ? HomeLighthouseV140Layout.ExpeditionPressedModule
                    : HomeLighthouseV140Layout.ExpeditionNormalModule
                : HomeLighthouseV140Layout.ExpeditionDisabledModule;
            DrawHomeLighthouseModule(actionResource, actionModule);
            var expeditionLabelKey = !string.IsNullOrWhiteSpace(formalExpeditionEntryStatusKey) &&
                                     Time.realtimeSinceStartup < formalExpeditionEntryStatusExpiresAt
                ? formalExpeditionEntryStatusKey
                : startKey;
            if (expeditionLabelKey == startKey) formalExpeditionEntryStatusKey = null;
            DrawHomeLighthouseText(HomeLighthouseV140Layout.ExpeditionLabel,
                T(expeditionLabelKey) + "  ›", 42,
                HomeLighthouseGold, TextAnchor.MiddleCenter, true, false);
            var previousEnabled = GUI.enabled;
            GUI.enabled = entryEnabled;
            var startPressed = GUI.Button(actionRect, GUIContent.none, GUIStyle.none);
            GUI.enabled = previousEnabled;
            if (startPressed)
            {
                PlayUiClick(UiAudioCueIds.Confirm, "home-expedition-entry");
                if (CompetitionDemoMode)
                {
                    BeginCompetitionDemoExpeditionFromHome(startsNewRun);
                    return;
                }
                if (UsesServerAuthoritativeExpedition)
                {
                    BeginOrRecoverFormalD0FromHomeAsync();
                    return;
                }
                if (!ExpeditionEconomyAuthorityPolicy.AllowsLocalRewardMutation(
                        UsesServerAuthoritativeExpedition))
                {
                    return;
                }
                if (startsNewRun) RestartRun();
                homeTopMenu = 0;
                currentView = ScreenView.Expedition;
            }

            if (GUI.Button(HomeLighthouseRect(HomeLighthouseV140Layout.Chapter), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-chapter-leaderboard");
                homeTopMenu = 0;
                currentView = ScreenView.Leaderboard;
                RequestBountyTowerRefreshIfNeeded(false);
            }
            HandleHomeChapterPointer(HomeLighthouseRect(HomeLighthouseV140Layout.IslandInteraction));

            if (homeTopMenu != 0) DrawHomeTopMenu();
        }

        private static readonly Color HomeLighthouseDeep = new Color(25f / 255f, 57f / 255f, 66f / 255f);
        private static readonly Color HomeLighthouseGold = new Color(239f / 255f, 226f / 255f, 188f / 255f);
        private static readonly Color HomeLighthouseSecondary = new Color(61f / 255f, 84f / 255f, 88f / 255f);

        private static Rect HomeLighthouseRect(ReferenceUiRect reference)
        {
            return new Rect(reference.X, reference.Y, reference.Width, reference.Height);
        }

        private void DrawHomeLighthouseText(ReferenceUiRect reference, string text, int fontSize,
            Color color, TextAnchor alignment, bool emphasized, bool wordWrap = true)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = HomeV38SerifFont(text, emphasized),
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = alignment,
                clipping = TextClipping.Clip,
                wordWrap = wordWrap,
                richText = false
            };
            style.normal.textColor = color;
            GUI.Label(HomeLighthouseRect(reference), new GUIContent(text), style);
        }

        private void DrawHomeLighthouseFittedText(ReferenceUiRect reference, string text,
            int preferredFontSize, int minimumFontSize, Color color, TextAnchor alignment,
            bool emphasized, bool wordWrap)
        {
            var area = HomeLighthouseRect(reference);
            var content = new GUIContent(text ?? string.Empty);
            GUIStyle fitted = null;
            for (var size = preferredFontSize; size >= minimumFontSize; size--)
            {
                var candidate = new GUIStyle(GUI.skin.label)
                {
                    font = HomeV38SerifFont(text, emphasized),
                    fontSize = size,
                    fontStyle = FontStyle.Normal,
                    alignment = alignment,
                    clipping = TextClipping.Clip,
                    wordWrap = wordWrap,
                    richText = false
                };
                candidate.normal.textColor = color;
                var requiredHeight = candidate.CalcHeight(content, area.width);
                var requiredWidth = wordWrap ? area.width : candidate.CalcSize(content).x;
                fitted = candidate;
                if (requiredWidth <= area.width && requiredHeight <= area.height) break;
            }
            GUI.Label(area, content, fitted);
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
            try
            {
                for (var i = 0; i < text.Length; i++)
                {
                    var character = text[i];
                    if (!char.IsControl(character) && !font.HasCharacter(character)) return false;
                }
            }
            catch (Exception)
            {
                return false;
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

        private void DrawHomeDynamicHud()
        {
            DrawHomeLighthouseText(HomeLighthouseV140Layout.EnergyValue, expeditionEnergy.ToString(), 23,
                HomeLighthouseGold, TextAnchor.MiddleCenter, false, false);
            DrawHomeLighthouseText(HomeLighthouseV140Layout.SkrValue, WalletBalanceText(), 23,
                HomeLighthouseGold, TextAnchor.MiddleCenter, false, false);
            DrawHomeLighthouseText(HomeLighthouseV140Layout.TideValue, tideCrystalBalance.ToString(), 23,
                HomeLighthouseGold, TextAnchor.MiddleCenter, false, false);
            if (GUI.Button(HomeLighthouseRect(HomeLighthouseV140Layout.ProfileClick),
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-profile-entry");
                homeTopMenu = 0;
                profileScroll = Vector2.zero;
                profileSettingsOpen = false;
                currentView = ScreenView.Profile;
            }

            if (GUI.Button(HomeLighthouseRect(HomeLighthouseV140Layout.AffairsClick),
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-affairs-menu");
                homeTopMenu = homeTopMenu == 1 ? 0 : 1;
            }
            if (GUI.Button(HomeLighthouseRect(HomeLighthouseV140Layout.WarehouseClick),
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-warehouse-entry");
                OpenWarehouse();
            }
        }

        private void DrawHomeLeftCompleteModule()
        {
            var placement = HomeLighthouseV140Layout.LeftCompleteModule;
            var texture = artCatalog.GetHomeLeftCompleteTexture(
                HomeLeftCompleteFormalArtContract.CompleteComponent);
            if (texture == null) return;
            if (texture.width != (int)placement.Width || texture.height != (int)placement.Height)
            {
                Debug.LogError("Rejected resized formal-home-left module: " + texture.width + "x" +
                               texture.height + " expected " + placement.Width + "x" + placement.Height);
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(HomeLighthouseRect(placement), texture, ScaleMode.StretchToFill, true);
        }

        private void DrawHomeLighthouseCanvas(string resourceId, bool alphaBlend, float yOffset = 0f)
        {
            var texture = artCatalog.GetHomeLighthouseTexture(resourceId);
            if (texture == null) return;
            if (texture.width != (int)HomeLighthouseV140Layout.ReferenceWidth ||
                texture.height != (int)HomeLighthouseV140Layout.ReferenceHeight)
            {
                Debug.LogError("Rejected non-native formal-home texture: " + resourceId + " " +
                               texture.width + "x" + texture.height);
                return;
            }
            GUI.color = Color.white;
            // Source and destination are the same locked 1080 x 2400 canvas.
            GUI.DrawTexture(new Rect(0f, yOffset, texture.width, texture.height), texture,
                ScaleMode.StretchToFill, alphaBlend);
        }

        private void DrawHomeLighthouseModule(string resourceId, ReferenceUiRect placement, float yOffset = 0f)
        {
            var texture = artCatalog.GetHomeLighthouseTexture(resourceId);
            if (texture == null) return;
            if (texture.width != (int)placement.Width || texture.height != (int)placement.Height)
            {
                Debug.LogError("Rejected resized formal-home module: " + resourceId + " " +
                               texture.width + "x" + texture.height + " expected " +
                               placement.Width + "x" + placement.Height);
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(placement.X, placement.Y + yOffset, texture.width, texture.height),
                texture, ScaleMode.StretchToFill, true);
        }

        private void DrawLanternCircularComponent(LanternInstanceState instance, Rect area, string context)
        {
            if (instance == null) return;
            if (Mathf.RoundToInt(area.width) != LanternArtContract.NativeSize ||
                Mathf.RoundToInt(area.height) != LanternArtContract.NativeSize)
            {
                Debug.LogError("Rejected non-native lantern target in " + context + ": " +
                               area.width + "x" + area.height + " expected " +
                               LanternArtContract.NativeSize + "x" + LanternArtContract.NativeSize);
                return;
            }
            var texture = artCatalog.GetLanternCircularTexture(instance.TemplateId, instance.Quality);
            if (texture == null)
            {
                Debug.LogError("Missing formal lantern component in " + context + ": " +
                               LanternArtContract.ResourceId(instance.TemplateId, instance.Quality));
                return;
            }
            if (texture.width != LanternArtContract.NativeSize ||
                texture.height != LanternArtContract.NativeSize)
            {
                Debug.LogError("Rejected resized lantern component in " + context + ": " +
                               texture.width + "x" + texture.height);
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(area.x, area.y, texture.width, texture.height), texture,
                ScaleMode.StretchToFill, true);
        }

        private void DrawHomeLanternModularComponent(
            LanternInstanceState instance, Rect area, string context)
        {
            if (instance == null) return;
            if (Mathf.RoundToInt(area.width) != HomeLanternModularArtContract.NativeSize ||
                Mathf.RoundToInt(area.height) != HomeLanternModularArtContract.NativeSize)
            {
                Debug.LogError("Rejected non-native modular home lantern target in " + context + ": " +
                               area.width + "x" + area.height + " expected " +
                               HomeLanternModularArtContract.NativeSize + "x" +
                               HomeLanternModularArtContract.NativeSize);
                return;
            }

            var appearanceId = HomeLanternModularArtContract.ResolveAppearanceId(instance.TemplateId);
            var qualityBackground = artCatalog.GetHomeLanternQualityBackground(instance.Quality);
            var pattern = artCatalog.GetHomeLanternPattern(appearanceId);
            if (!IsNativeHomeLanternLayer(qualityBackground) || !IsNativeHomeLanternLayer(pattern))
            {
                Debug.LogError("Missing or resized modular home lantern layer in " + context + ": " +
                               instance.Quality + "/" + appearanceId);
                return;
            }

            var drawArea = new Rect(area.x, area.y,
                HomeLanternModularArtContract.NativeSize, HomeLanternModularArtContract.NativeSize);
            GUI.color = Color.white;
            GUI.DrawTexture(drawArea, qualityBackground, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(drawArea, pattern, ScaleMode.StretchToFill, true);
        }

        private static bool IsNativeHomeLanternLayer(Texture2D texture)
        {
            return texture != null &&
                   texture.width == HomeLanternModularArtContract.NativeSize &&
                   texture.height == HomeLanternModularArtContract.NativeSize;
        }

        private void DrawHomeLanternPanel()
        {
            var canLight = chapterOneLighthouse.CanLight;
            var bossUnlockRequired = !chapterOneLighthouse.BossDefeated;
            var showUnlockHint = bossUnlockRequired && Time.unscaledTime < homeLanternUnlockHintUntil;
            string statusKey;
            if (showUnlockHint)
                statusKey = "home.lantern.unlockHint";
            else if (bossUnlockRequired)
                statusKey = "home.lanternPanel.title";
            else if (chapterOneLighthouse.IsLit)
                statusKey = "lighthouse.lit";
            else
                statusKey = canLight ? "home.lighthouse.readyToLight" : "lighthouse.lanternNeeded";
            DrawHomeLighthouseText(HomeLighthouseV140Layout.PanelTitle, T(statusKey), 32,
                HomeLighthouseGold, TextAnchor.MiddleCenter, true, false);

            var active = GetActiveHomeLantern();
            var lanternName = active == null ? T("home.lantern.none") : T(active.TemplateId);
            var coreEffect = active == null
                ? T("home.lantern.corePending")
                : string.Format(T("home.lantern.coreEffect"), T(active.CoreEffectId));
            DrawHomeLighthouseText(HomeLighthouseV140Layout.LanternLabel,
                T("home.lantern.current"), 22, HomeLighthouseSecondary,
                TextAnchor.MiddleLeft, false, false);
            DrawHomeLighthouseFittedText(HomeLighthouseV140Layout.LanternName, lanternName,
                32, 18, HomeLighthouseDeep, TextAnchor.MiddleLeft, true, false);
            DrawHomeLighthouseFittedText(HomeLighthouseV140Layout.LanternDetails,
                coreEffect, 22, 17,
                HomeLighthouseSecondary, TextAnchor.UpperLeft, false, true);

            DrawHomeLighthouseText(HomeLighthouseV140Layout.LanternChange, T("home.lantern.change"), 24,
                HomeLighthouseGold, TextAnchor.MiddleCenter, true, false);
            if (GUI.Button(HomeLighthouseRect(HomeLighthouseV140Layout.LanternChange),
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-lantern-change");
                OpenLanternWarehouse();
            }
            if (GUI.Button(HomeLighthouseRect(HomeLighthouseV140Layout.PanelTitle),
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-lantern-panel");
                if (bossUnlockRequired)
                {
                    homeLanternUnlockHintUntil = Time.unscaledTime + 3f;
                }
                else
                {
                    ActivateHomeLighthouseCard(canLight);
                }
            }
        }

        private LanternInstanceState GetActiveHomeLantern()
        {
            if (lanternInventory == null || string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId))
                return null;
            return lanternInventory.Get(lanternInventory.ActiveLanternInstanceId);
        }

        private void OpenLanternWarehouse()
        {
            OpenWarehouse();
            warehouseTab = WarehouseTab.Lantern;
        }

        private void ActivateHomeLighthouseCard(bool canLight)
        {
            if (chapterOneLighthouse.IsLit)
            {
                return;
            }
            if (!canLight || !chapterOneLighthouse.TryLight()) return;
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

        private int CurrentPvpDeckPower()
        {
            var power = 0;
            for (var i = 0; i < defenseSlots.Length; i++)
            {
                if (defenseSlots[i] != null) power += Mathf.Max(0, defenseSlots[i].Power);
            }
            return power * 140;
        }

        private void OpenWarehouse()
        {
            var focusEquipment = pendingMarketWarehouseFocusKind == MarketAssetKind.Equipment &&
                                 equipmentInventory != null &&
                                 equipmentInventory.Contains(pendingMarketWarehouseFocusInstanceId);
            var focusLantern = pendingMarketWarehouseFocusKind == MarketAssetKind.Lantern &&
                               lanternInventory != null &&
                               lanternInventory.Contains(pendingMarketWarehouseFocusInstanceId);
            warehouseTab = focusEquipment
                ? WarehouseTab.Equipment
                : focusLantern ? WarehouseTab.Lantern : WarehouseTab.All;
            pendingMarketWarehouseFocusKind = null;
            pendingMarketWarehouseFocusInstanceId = null;
            warehouseEquipmentScroll = Vector2.zero;
            warehouseEquipmentDetailOpen = false;
            warehouseEquipmentDetailId = null;
            warehouseLanternDetailOpen = false;
            warehouseLanternDetailId = null;
            warehouseCosmeticDetailOpen = false;
            warehouseCosmeticDetailProductId = null;
            firstLightseekerBadgeDetailOpen = false;
            CloseLanternChangeConfirmation();
            homeTopMenu = 0;
            currentView = ScreenView.Warehouse;
        }

        private void DrawHomeTopMenu()
        {
            var texture = HomeHandbookPopover();
            if (texture == null || texture.width != 1024 || texture.height != 1536)
            {
                Debug.LogError("Accepted home handbook popover is missing or resized.");
                return;
            }
            GUI.DrawTexture(new Rect(28f, 0f, 1024f, 1536f), texture, ScaleMode.StretchToFill, true);
            DrawHomeLighthouseText(new ReferenceUiRect(350f, 348f, 380f, 58f),
                language == Localization.GameLanguage.ChineseSimplified ? "航海手册" : "SAILING HANDBOOK",
                32, new Color(231f / 255f, 213f / 255f, 166f / 255f), TextAnchor.MiddleCenter, true);
            DrawHomeLighthouseText(new ReferenceUiRect(405f, 436f, 300f, 96f),
                T("home.daily"), 29, new Color(183f / 255f, 198f / 255f, 205f / 255f),
                TextAnchor.MiddleCenter, false);
            DrawHomeLighthouseText(new ReferenceUiRect(405f, 582f, 300f, 96f),
                T("home.events"), 29, new Color(183f / 255f, 198f / 255f, 205f / 255f),
                TextAnchor.MiddleCenter, false);
            DrawHomeLighthouseText(new ReferenceUiRect(405f, 728f, 300f, 96f),
                language == Localization.GameLanguage.ChineseSimplified ? "玩法教程" : "GAMEPLAY TUTORIAL",
                31, new Color(28f / 255f, 67f / 255f, 84f / 255f), TextAnchor.MiddleCenter, true);

            if (GUI.Button(new Rect(271f, 704f, 536f, 120f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "home-handbook-tutorial");
                OpenGameplayTutorialFromHandbook();
            }
        }

        private void DrawCreditsScreen()
        {
            var primary = new Color(0.91f, 0.93f, 0.90f);
            var secondary = new Color(0.56f, 0.69f, 0.70f);
            var accent = new Color(0.36f, 0.82f, 0.77f);

            if (GUI.Button(new Rect(55f, 42f, 160f, 58f), T("home.back"),
                    ButtonStyle(UiButtonSemantic.Secondary, 18)))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "credits-back");
                creditsScroll = Vector2.zero;
                currentView = ScreenView.Home;
                return;
            }
            GUI.Label(new Rect(245f, 34f, 620f, 72f), T("credits.title"),
                Style(38, primary, TextAnchor.MiddleLeft));

            var viewport = new Rect(55f, 130f, 970f, Mathf.Max(480f, layoutContentHeight - 160f));
            const float contentWidth = 930f;
            var contentHeight = CalculateCreditsContentHeight();
            creditsScroll = GUI.BeginScrollView(viewport, creditsScroll,
                new Rect(0f, 0f, contentWidth, contentHeight));

            var y = 0f;
            GUI.Label(new Rect(8f, y, 900f, 86f), T("credits.intro"),
                Style(20, secondary, TextAnchor.UpperLeft));
            y += 102f;
            DrawAttributionGroup(ref y, AttributionCategory.Music, "credits.category.music",
                primary, secondary, accent);
            DrawAttributionGroup(ref y, AttributionCategory.SoundEffects, "credits.category.sfx",
                primary, secondary, accent);
            DrawAttributionGroup(ref y, AttributionCategory.Fonts, "credits.category.fonts",
                primary, secondary, accent);

            GUI.EndScrollView();
        }

        private void DrawAttributionGroup(ref float y, AttributionCategory category, string titleKey,
            Color primary, Color secondary, Color accent)
        {
            GUI.Label(new Rect(8f, y, 900f, 48f), T(titleKey),
                Style(25, accent, TextAnchor.MiddleLeft, FontStyle.Bold));
            y += 58f;
            foreach (var entry in ThirdPartyAttributionCatalog.Entries)
            {
                if (entry.Category != category) continue;
                var row = new Rect(0f, y, 912f, 194f);
                ActiveUiSkin.DrawPanel(row, false);
                GUI.Label(new Rect(28f, y + 18f, 520f, 38f), entry.Title,
                    Style(22, primary, TextAnchor.MiddleLeft, FontStyle.Bold));
                GUI.Label(new Rect(566f, y + 18f, 316f, 38f), entry.License,
                    Style(15, accent, TextAnchor.MiddleRight));
                GUI.Label(new Rect(28f, y + 62f, 630f, 30f), entry.Credit,
                    Style(17, secondary, TextAnchor.MiddleLeft));
                var purpose = language == GameLanguage.ChineseSimplified
                    ? entry.PurposeChinese : entry.PurposeEnglish;
                GUI.Label(new Rect(28f, y + 98f, 630f, 30f), purpose,
                    Style(16, secondary, TextAnchor.MiddleLeft));
                GUI.Label(new Rect(28f, y + 134f, 520f, 38f), entry.SourceUrl,
                    Style(13, new Color(0.40f, 0.56f, 0.58f), TextAnchor.MiddleLeft));
                if (GUI.Button(new Rect(578f, y + 132f, 138f, 44f), T("credits.source"),
                        ButtonStyle(UiButtonSemantic.Secondary, 14)))
                {
                    PlayUiClick(UiAudioCueIds.Tap, "credits-source");
                    Application.OpenURL(entry.SourceUrl);
                }
                if (GUI.Button(new Rect(732f, y + 132f, 150f, 44f), T("credits.license"),
                        ButtonStyle(UiButtonSemantic.Secondary, 14)))
                {
                    PlayUiClick(UiAudioCueIds.Tap, "credits-license");
                    Application.OpenURL(entry.LicenseUrl);
                }
                y += 210f;
            }
            y += 20f;
        }

        private static float CalculateCreditsContentHeight()
        {
            return 102f + 3f * 78f + ThirdPartyAttributionCatalog.Entries.Count * 210f + 40f;
        }

        private void DrawHomeTexture(Rect area, string resourceId, ScaleMode scaleMode)
        {
            var texture = artCatalog.GetHomeUiTexture(resourceId);
            if (texture == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
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

        private bool HasExpeditionProgress()
        {
            if (expeditionFlow.IsComplete) return false;
            if (expeditionEntryCommitted) return true;
            var isUntouchedMapStart = round == 1 && coins == 10 &&
                                      health == ChapterOneExpeditionLifeRules.InitialLife &&
                                      runRoster.TotalCount == 0 &&
                                      expeditionFlow.Phase == ExpeditionPhase.Route &&
                                      string.Equals(currentMapNodeId, "M00", StringComparison.Ordinal);
            if (isUntouchedMapStart) return false;
            return round > 1 || coins != 10 ||
                   health != ChapterOneExpeditionLifeRules.InitialLife || runRoster.TotalCount > 0 ||
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
            if (IsApprovedHomeNavigationView(currentView))
            {
                DrawHomeNavigation(views);
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
                    PlayUiClick(UiAudioCueIds.TabSwitch, "main-navigation");
                    currentView = views[i];
                }
            }
        }

        private static bool IsApprovedHomeNavigationView(ScreenView view)
        {
            switch (view)
            {
                case ScreenView.Store:
                case ScreenView.Deck:
                case ScreenView.Home:
                case ScreenView.Arena:
                case ScreenView.Harbor:
                    return true;
                default:
                    return false;
            }
        }

        private void DrawHomeNavigation(ScreenView[] views)
        {
            // Non-home screens already apply the safe-area translation. Offset the full-canvas
            // art by the inverse amount so its final screen origin remains exactly (0,0).
            var canvasYOffset = currentView == ScreenView.Home || currentView == ScreenView.Store ||
                                currentView == ScreenView.Deck || currentView == ScreenView.Harbor ||
                                IsFormalArenaPage
                ? 0f
                : -layoutSafeTop;
            var selectedIndex = 0;
            for (var i = 0; i < views.Length; i++)
            {
                if (currentView == views[i]) selectedIndex = i;
            }
            DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseNavNeutral,
                HomeLighthouseV140Layout.NavNeutralModule, canvasYOffset);
            DrawHomeLighthouseModule(PrototypeArtResourceIds.HomeLighthouseNavSelection,
                HomeLighthouseV140Layout.NavigationSelection(selectedIndex), canvasYOffset);
            var labelKeys = new[] { "nav.store", "nav.deck", "nav.home", "nav.arena", "nav.harbor" };
            for (var i = 0; i < views.Length; i++)
            {
                var referenceLabel = HomeLighthouseV140Layout.NavigationLabel(i);
                var label = new ReferenceUiRect(referenceLabel.X, referenceLabel.Y + canvasYOffset,
                    referenceLabel.Width, referenceLabel.Height);
                DrawHomeLighthouseText(label, T(labelKeys[i]), 25, HomeLighthouseGold,
                    TextAnchor.MiddleCenter, false, false);
                var referenceSlot = HomeLighthouseV140Layout.BottomNavigationSlot(i);
                var slot = new Rect(referenceSlot.X, referenceSlot.Y + canvasYOffset,
                    referenceSlot.Width, referenceSlot.Height);
                if (!GUI.Button(slot, GUIContent.none, GUIStyle.none)) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "home-navigation");
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

        private void DrawHarborScreen()
        {
            DrawHarborModule(HarborFormalArtContract.Background,
                new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);

            DrawHarborText(new Rect(64f, 62f, 300f, 72f), T("harbor.title"), 58,
                ArchiveColor("F3E4BE"), TextAnchor.MiddleLeft, true);
            DrawHarborText(new Rect(68f, 138f, 700f, 42f), T("harbor.subtitle"), 23,
                ArchiveColor("D9CDAA"), TextAnchor.MiddleLeft, false);
            DrawHarborText(new Rect(940f, 18f, 120f, 30f), ClientVersionLabel, 16,
                ArchiveColor("EAE1C7"), TextAnchor.MiddleRight, false);

            DrawHarborDestination(new Rect(620f, 390f, 280f, 88f), true,
                "harbor.leaderboard", "harbor.leaderboardDescription", true, 2);

            var alternate = ActiveHarborPageVariant == HarborPageVariant.ConstructionAlternate;
            DrawHarborDestination(alternate
                    ? new Rect(144f, 779f, 260f, 78f)
                    : new Rect(134f, 774f, 280f, 88f),
                !alternate, "harbor.blacksmith",
                alternate ? "harbor.construction" : "harbor.blacksmithDescription", false, 1,
                alternate);
            var lanternWorkshopConstruction =
                alternate || !LanternWorkshopReleased;
            DrawHarborDestination(alternate
                     ? new Rect(676f, 817f, 260f, 78f)
                     : new Rect(676f, 812f, 260f, 88f),
                !lanternWorkshopConstruction,
                "harbor.lanternWorkshop",
                lanternWorkshopConstruction
                    ? "harbor.construction"
                    : "harbor.lanternWorkshopDescription",
                false, 3, lanternWorkshopConstruction);

            DrawHarborDestination(new Rect(114f, 1030f, 260f, 78f), false,
                "harbor.construction", "harbor.unavailable", false, 4, true);
            DrawHarborDestination(new Rect(706f, 1060f, 260f, 78f), false,
                "harbor.construction", "harbor.unavailable", false, 5, true);
            DrawHarborDestination(new Rect(410f, 1360f, 260f, 88f), true,
                "harbor.market", "harbor.marketDescription", false, 0);
        }

        private void DrawHarborDestination(Rect area, bool enabled, string titleKey,
            string descriptionKey, bool opensBountyTower, int featureIndex, bool construction = false)
        {
            var wide = !construction && Mathf.Approximately(area.width, 280f);
            var pressed = enabled && Event.current.type == EventType.MouseDown && area.Contains(Event.current.mousePosition);
            var resourceId = construction
                ? HarborFormalArtContract.Construction
                : wide
                    ? (pressed ? HarborFormalArtContract.WidePressed : HarborFormalArtContract.WideNormal)
                    : (pressed ? HarborFormalArtContract.CompactPressed : HarborFormalArtContract.CompactNormal);
            DrawHarborModule(resourceId, area, Mathf.RoundToInt(area.width), Mathf.RoundToInt(area.height));

            var constructionInk = ArchiveColor("AEB3AE");
            var textColor = construction ? constructionInk : ArchiveColor("1E3F46");
            var titleFontSize = construction ? 24 : 30;
            var descriptionFontSize = construction ? 17 : 19;
            var titleRect = construction
                ? new Rect(area.x + 10f, area.y + 8f, area.width - 20f, 31f)
                : new Rect(area.x + 10f, area.y + 9f, area.width - 20f, 38f);
            var descriptionRect = construction
                ? new Rect(area.x + 10f, area.y + 41f, area.width - 20f, 25f)
                : new Rect(area.x + 10f, area.y + 49f, area.width - 20f, 27f);
            DrawHarborText(titleRect, T(titleKey), titleFontSize, textColor,
                TextAnchor.MiddleCenter, true);
            DrawHarborText(descriptionRect, T(descriptionKey), descriptionFontSize, textColor,
                TextAnchor.MiddleCenter, false);

            if (!enabled || !GUI.Button(area, GUIContent.none, GUIStyle.none)) return;
            PlayUiClick(UiAudioCueIds.Tap, opensBountyTower ? "harbor-bounty-tower" : "harbor-feature-select");
            selectedHarborFeatureIndex = featureIndex;
            if (opensBountyTower)
            {
                currentView = ScreenView.Leaderboard;
                RequestBountyTowerRefreshIfNeeded(false);
            }
            else if (featureIndex == 0) currentView = ScreenView.Market;
            else if (featureIndex == 3)
            {
                lanternWorkshopMode = true;
                lanternWorkshopSelectedInstanceId = null;
                lanternWorkshopPendingOptionId = null;
                currentView = ScreenView.Warehouse;
                RequestLanternWorkshopRefresh();
            }
        }

        private void DrawHarborModule(string resourceId, Rect area, int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetHarborTexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight) return;
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawHarborText(Rect area, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized)
        {
            var style = ArchiveStyle(fontSize, color, alignment, emphasized);
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            GUI.Label(area, new GUIContent(text), style);
        }

        private void DrawLeaderboardScreen()
        {
            DrawFormalBountyTowerScreen();
        }

        private void DrawStoreScreen(bool interactive)
        {
            // Render the complete accepted v1.6.0 catalog. Products whose economy
            // contract is still pending remain visible but cannot be purchased.
            DrawHomeLighthouseCanvas(PrototypeArtResourceIds.HomeLighthouseBackground, false);
            DrawStoreText(new Rect(72f, 42f, 420f, 72f), T("store.title"), 52,
                new Color32(244, 231, 195, 255), TextAnchor.MiddleLeft, true);
            DrawStoreText(new Rect(76f, 112f, 650f, 42f), T("store.subtitle.formal"), 21,
                new Color32(201, 218, 211, 255), TextAnchor.MiddleLeft, false);
            DrawStoreModule(StoreFormalArtContract.BalancePanelResourceId,
                new Rect(768f, 36f, 240f, 96f), 240, 96);
            DrawStoreModule(StoreFormalArtContract.SkrBadgeResourceId,
                new Rect(790f, 52f, 58f, 62f), 58, 62);
            DrawStoreText(new Rect(852f, 46f, 118f, 48f), WalletBalanceText(), 34,
                new Color32(244, 231, 195, 255), TextAnchor.MiddleRight, true);
            DrawStoreText(new Rect(852f, 90f, 118f, 30f), T("store.skrBalance"), 17,
                new Color32(201, 218, 211, 255), TextAnchor.MiddleRight, false);

            var viewport = new Rect(StoreSingleColumnFormalArtContract.ViewportX,
                StoreSingleColumnFormalArtContract.ViewportY,
                StoreSingleColumnFormalArtContract.ViewportWidth,
                StoreSingleColumnFormalArtContract.ViewportHeight);
            var content = new Rect(0f, 0f, StoreSingleColumnFormalArtContract.ViewportWidth,
                StoreSingleColumnFormalArtContract.CurrentCatalogContentHeight);
            var storeEvent = Event.current;
            var storeEventType = storeEvent.rawType;
            if (storeEventType == EventType.MouseDown && storeEvent.button == 0 &&
                viewport.Contains(storeEvent.mousePosition))
            {
                storeCatalogPointerDown = storeEvent.mousePosition;
                storeCatalogPointerLast = storeEvent.mousePosition;
                storeCatalogPointerHeld = true;
                storeCatalogPointerDragging = false;
            }
            else if (storeEventType == EventType.MouseDrag && storeCatalogPointerHeld)
            {
                var pointerPosition = storeEvent.mousePosition;
                if (Vector2.Distance(storeCatalogPointerDown, pointerPosition) > 12f)
                    storeCatalogPointerDragging = true;
                if (storeCatalogPointerDragging)
                {
                    var maximumScroll = Mathf.Max(0f, content.height - viewport.height);
                    var dragDelta = pointerPosition - storeCatalogPointerLast;
                    storeCatalogScroll.y = Mathf.Clamp(
                        storeCatalogScroll.y - dragDelta.y, 0f, maximumScroll);
                    storeEvent.Use();
                }
                storeCatalogPointerLast = pointerPosition;
            }
            var releaseStorePointerAfterDraw = storeEventType == EventType.MouseUp &&
                                               storeCatalogPointerHeld;
            if (releaseStorePointerAfterDraw && storeCatalogPointerDragging)
                storeEvent.Use();
            storeCatalogScroll = GUI.BeginScrollView(viewport, storeCatalogScroll, content,
                false, false, GUIStyle.none, GUIStyle.none);
            DrawStoreSingleColumnSection(0f, T("store.characterPacks"),
                T("store.characterPacks.subtitle"));
            var products = StoreCatalog.Products;
            DrawStoreSingleColumnProductRow(76f, products[0],
                StoreSingleColumnFormalArtContract.CharacterPackArt[0],
                T("store.pack.white.description"), interactive);
            DrawStoreSingleColumnProductRow(414f, products[1],
                StoreSingleColumnFormalArtContract.CharacterPackArt[1],
                T("store.pack.blue.description"), interactive);
            DrawStoreSingleColumnProductRow(752f, products[2],
                StoreSingleColumnFormalArtContract.CharacterPackArt[2],
                T("store.pack.orange.description"), interactive);
            DrawStoreSingleColumnSection(1098f, T("store.baseCategory"),
                T("store.baseCategory.subtitle"));
            DrawStoreSingleColumnProductRow(1174f, products[3],
                StoreSingleColumnFormalArtContract.BasePairArt[0],
                T("store.basePackDescription.formal"), interactive);
            DrawStoreSingleColumnProductRow(1512f, products[4],
                StoreSingleColumnFormalArtContract.BasePairArt[1],
                T("store.basePackDescription.formal"), interactive);
            DrawStoreSingleColumnProductRow(1850f, products[5],
                StoreSingleColumnFormalArtContract.BasePairArt[2],
                T("store.basePackDescription.formal"), interactive);
            DrawStoreSingleColumnSection(2196f, T("store.battlefieldCategory"),
                T("store.battlefieldCategory.subtitle"));
            DrawStoreSingleColumnProductRow(2272f, products[6],
                StoreSingleColumnFormalArtContract.BattlefieldArt[0],
                T("store.battlefieldDescription.formal"), interactive);
            DrawStoreSingleColumnProductRow(2610f, products[7],
                StoreSingleColumnFormalArtContract.BattlefieldArt[1],
                T("store.battlefieldDescription.formal"), interactive);
            DrawStoreSingleColumnProductRow(2948f, products[8],
                StoreSingleColumnFormalArtContract.BattlefieldArt[2],
                T("store.battlefieldDescription.formal"), interactive);
            GUI.EndScrollView();
            if (releaseStorePointerAfterDraw)
            {
                storeCatalogPointerHeld = false;
                storeCatalogPointerDragging = false;
            }

            if (!string.IsNullOrEmpty(storeStatusMessage) &&
                !string.Equals(storeStatusMessage, T("store.status.ready"), StringComparison.Ordinal))
                DrawStoreText(new Rect(180f, 154f, 720f, 32f), storeStatusMessage, 17,
                    new Color32(244, 231, 195, 255), TextAnchor.MiddleCenter, true);
        }

        private void DrawStoreSingleColumnSection(float y, string title, string subtitle)
        {
            DrawStoreText(new Rect(12f, y, 220f, 44f), title, 31,
                new Color32(244, 231, 195, 255), TextAnchor.MiddleLeft, true);
            DrawStoreText(new Rect(12f, y + 44f, 500f, 28f), subtitle, 17,
                new Color32(201, 218, 211, 255), TextAnchor.MiddleLeft, false);
            DrawMarketTextureFormal(new Rect(248f, y + 16f, 664f, 20f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.SectionDivider);
        }

        private void DrawStoreSingleColumnProductRow(float y, StoreProductDefinition product,
            string artResourceId, string description, bool interactive)
        {
            var characterPack = product.ProductType == StoreProductType.CharacterPack;
            var battlefield = product.ProductType == StoreProductType.Battlefield;
            DrawStoreSingleColumnModule(StoreSingleColumnFormalArtContract.ProductRow,
                new Rect(8f, y, 912f, 330f), 912, 330);
            if (characterPack)
                DrawStoreSingleColumnModule(artResourceId, new Rect(18f, y + 47f, 280f, 236f), 280, 236);
            else if (battlefield)
                DrawStoreSingleColumnModule(artResourceId, new Rect(14f, y + 55f, 300f, 220f), 300, 220);
            else
                DrawStoreSingleColumnModule(artResourceId, new Rect(20f, y + 75f, 300f, 180f), 300, 180);

            DrawStoreText(new Rect(340f, y + (characterPack ? 60f : 88f), 310f, 44f),
                T(product.DisplayNameKey), 27, new Color32(31, 61, 65, 255),
                TextAnchor.MiddleLeft, true);
            DrawStoreText(new Rect(340f, y + (characterPack ? 106f : 136f), 310f, 72f),
                description, 18, new Color32(75, 104, 102, 255), TextAnchor.UpperLeft, false, true);
            if (characterPack) DrawStoreSingleColumnProbabilityGrid(y, product.PackTier);

            var owned = product.ProductType != StoreProductType.CharacterPack &&
                         ownedBasePackIds.Contains(product.ProductId);
            DrawStoreText(new Rect(676f, y + 88f, 200f, 48f),
                owned ? T("store.owned") : product.IsPurchasable
                    ? product.PriceSkr + " SKR" : T("store.pricePending"), 25,
                owned ? new Color32(79, 133, 99, 255) : new Color32(182, 112, 54, 255),
                TextAnchor.MiddleLeft, true);
            var button = new Rect(660f, y + 152f, 220f, 68f);
            var enabled = interactive && !owned && product.IsPurchasable;
            var clicked = DrawLanternDetailAStateButton(button,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.MediumNormal,
                GlobalButtonFormalArtContract.MediumPressed,
                GlobalButtonFormalArtContract.MediumDisabled, enabled);
            DrawStoreText(button, T(owned ? "store.owned" : product.IsPurchasable
                    ? "store.purchase" : "store.pending"), 23,
                new Color32(244, 229, 184, 255), TextAnchor.MiddleCenter, true);
            if (!clicked) return;
            PlayUiClick(UiAudioCueIds.Purchase, "store-single-column-" + product.ProductId);
            TryPurchaseStoreProduct(product);
        }

        private void DrawStoreSingleColumnProbabilityGrid(float y, CharacterPackTier tier)
        {
            var values = StoreProbabilityValues(tier);
            var xOffsets = new[] { 340f, 462f, 340f, 462f, 340f };
            var yOffsets = new[] { 195f, 195f, 231f, 231f, 267f };
            for (var index = 0; index < values.Length; index++)
            {
                var badgeId = StoreFormalArtContract.QualityBadgeResourceIds[index];
                DrawStoreModule(badgeId,
                    new Rect(xOffsets[index], y + yOffsets[index], 30f, 30f), 30, 30);
                DrawStoreText(new Rect(xOffsets[index] + 34f, y + yOffsets[index] - 2f, 82f, 34f),
                    values[index], 16, new Color32(44, 72, 73, 255),
                    TextAnchor.MiddleLeft, true);
            }
        }

        private void DrawStoreSingleColumnModule(string resourceId, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetStoreSingleColumnTexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Store v1.6 module: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawStoreCardBackFan(Rect area)
        {
            var sideCard = GetVerifiedStoreTexture(StoreFormalArtContract.SideCardBackResourceId,
                StoreFormalArtContract.SideCardWidth, StoreFormalArtContract.SideCardHeight);
            var centerCard = GetVerifiedStoreTexture(StoreFormalArtContract.CenterCardBackResourceId,
                StoreFormalArtContract.CenterCardWidth, StoreFormalArtContract.CenterCardHeight);
            var centerY = area.y + area.height * 0.54f;
            DrawRotatedStoreTexture(sideCard, new Vector2(area.x + area.width * 0.34f, centerY + 12f),
                StoreFormalArtContract.SideCardWidth, StoreFormalArtContract.SideCardHeight,
                StoreFormalArtContract.LeftCardAngle);
            DrawRotatedStoreTexture(sideCard, new Vector2(area.x + area.width * 0.66f, centerY + 12f),
                StoreFormalArtContract.SideCardWidth, StoreFormalArtContract.SideCardHeight,
                StoreFormalArtContract.RightCardAngle);
            DrawRotatedStoreTexture(centerCard, new Vector2(area.x + area.width * 0.50f, centerY - 5f),
                StoreFormalArtContract.CenterCardWidth, StoreFormalArtContract.CenterCardHeight, 0f);
        }

        private void DrawStoreModule(string resourceId, Rect rect, int expectedWidth, int expectedHeight)
        {
            var texture = GetVerifiedStoreTexture(resourceId, expectedWidth, expectedHeight);
            if (texture == null) return;
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawStoreText(Rect rect, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized, bool wordWrap = false)
        {
            var style = ArchiveStyle(fontSize, color, alignment, emphasized);
            style.wordWrap = wordWrap;
            style.clipping = TextClipping.Clip;
            GUI.Label(rect, new GUIContent(text), style);
        }

        private Texture2D GetVerifiedStoreTexture(string resourceId, int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetStoreETexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight) return null;
            return texture;
        }

        private static void DrawRotatedStoreTexture(Texture2D texture, Vector2 center, float width, float height,
            float angle)
        {
            if (texture == null) return;
            var previousMatrix = GUI.matrix;
            var previousColor = GUI.color;
            GUIUtility.RotateAroundPivot(angle, center);
            GUI.color = Color.white;
            GUI.DrawTexture(new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height), texture,
                ScaleMode.StretchToFill, true);
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
        }

        private static string[] StoreProbabilityValues(CharacterPackTier tier)
        {
            switch (tier)
            {
                case CharacterPackTier.White: return new[] { "70.0%", "24.8%", "4.6%", "0.55%", "0.05%" };
                case CharacterPackTier.Blue: return new[] { "18.5%", "55.5%", "22.8%", "2.9%", "0.3%" };
                default: return new[] { "0%", "44.8%", "42.2%", "11.7%", "1.3%" };
            }
        }

        private string PackProbabilityText(CharacterPackTier tier)
        {
            switch (tier)
            {
                case CharacterPackTier.White: return T("quality.common") + " 70.0%\n" + T("quality.uncommon") + " 24.8%\n" +
                    T("quality.rare") + " 4.6%\n" + T("quality.epic") + " 0.55%\n" + T("quality.legendary") + " 0.05%";
                case CharacterPackTier.Blue: return T("quality.common") + " 18.5%\n" + T("quality.uncommon") + " 55.5%\n" +
                    T("quality.rare") + " 22.8%\n" + T("quality.epic") + " 2.9%\n" + T("quality.legendary") + " 0.3%";
                default: return T("quality.common") + " 0%\n" + T("quality.uncommon") + " 44.8%\n" +
                    T("quality.rare") + " 42.2%\n" + T("quality.epic") + " 11.7%\n" + T("quality.legendary") + " 1.3%";
            }
        }

        private void TryPurchaseStoreProduct(StoreProductDefinition product)
        {
            if (product == null) return;
            if (!product.IsPurchasable)
            {
                storeStatusMessage = T("store.status.pricePending");
                return;
            }
            if (storeOpeningActive)
            {
                storeStatusMessage = T("store.status.claimFirst");
                return;
            }
            if (product.ProductType != StoreProductType.CharacterPack &&
                ownedBasePackIds.Contains(product.ProductId))
            {
                storeStatusMessage = T("store.status.alreadyOwned");
                return;
            }
            if (WalletBalanceMicroSkr < MarketRules.ToMicroSkr(product.PriceSkr))
            {
                storeStatusMessage = T("store.status.insufficient");
                storePurchaseNoticeUntil = Time.unscaledTime + 2f;
                return;
            }
            storePurchaseNoticeUntil = 0f;

            var orderId = "store-order-" + (storePurchaseSerial + 1);
            var request = new WalletPurchaseRequest(orderId, product.ProductId, product.PriceSkr,
                StoreRules.StoreRulesVersion, T(product.DisplayNameKey) + " / " + product.PriceSkr + " SKR");
            var signature = walletSignatureGateway.RequestTransactionSignature(request);
            if (signature == null || !signature.Approved)
            {
                storeStatusMessage = T("store.status.signatureRejected");
                return;
            }

            DebitWalletWholeSkr(product.PriceSkr);
            storePurchaseSerial++;
            if (product.ProductType == StoreProductType.CharacterPack)
            {
                PrepareCharacterPackReveal(product, orderId);
                storeStatusMessage = T("store.status.revealReady");
            }
            else
            {
                ownedBasePackIds.Add(product.ProductId);
                storeStatusMessage = T("store.status.baseOwned");
                SaveGame();
            }
        }

        private void PrepareCharacterPackReveal(StoreProductDefinition product, string orderId)
        {
            var ids = StoreRules.SelectDistinctCharacterIds(characterCatalog, orderId,
                StoreRules.DrawCountPerCharacterPack);
            pendingStoreDefinitionIds = new string[ids.Length];
            pendingStoreInstanceIds = new string[ids.Length];
            pendingStoreQualities = new int[ids.Length];
            pendingStoreAptitudeRolls = new int[ids.Length];
            pendingStoreRevealAlreadyGranted = false;
            pendingStarterGrantId = null;
            storeEpicPitySkr += product.PriceSkr;
            storeLegendaryPitySkr += product.PriceSkr;
            for (var i = 0; i < ids.Length; i++)
            {
                var forceLegendary = storeLegendaryPitySkr >= StoreRules.LegendaryPityThresholdSkr;
                var forceEpic = storeEpicPitySkr >= StoreRules.EpicPityThresholdSkr;
                var quality = StoreRules.RollQuality(product.PackTier, orderId + ":slot:" + i,
                    forceEpic, forceLegendary);
                if (quality == CharacterQuality.Legendary)
                {
                    storeLegendaryPitySkr = 0;
                    storeEpicPitySkr = 0;
                }
                else if (quality == CharacterQuality.Epic)
                {
                    storeEpicPitySkr = 0;
                }
                var instanceId = "character.store." + storePurchaseSerial + "." + i + "." + ids[i];
                pendingStoreDefinitionIds[i] = ids[i];
                pendingStoreInstanceIds[i] = instanceId;
                pendingStoreQualities[i] = (int)quality;
                pendingStoreAptitudeRolls[i] =
                    CharacterRarityInstanceRules.ComputeAptitudeRoll(instanceId);
            }
            pendingStoreOrderId = orderId;
            storeOpeningProgress = 0f;
            storeRevealLoadingElapsed = 0f;
            storeRevealAssetsReady = false;
            storeRevealPreloadComplete = false;
            storeRevealSelectedIndex = 1;
            storeOpeningActive = true;
            BeginStoreRevealPreparation(true);
        }

        private void BeginStoreRevealPreparation(bool savePendingPurchase)
        {
            if (!storeOpeningActive || storeRevealPreparationCoroutine != null) return;
            storeRevealLoadingElapsed = 0f;
            storeRevealAssetsReady = false;
            storeRevealPreloadComplete = false;
            storeRevealPreparationCoroutine = StartCoroutine(
                PrepareStoreRevealPresentation(savePendingPurchase));
        }

        private IEnumerator PrepareStoreRevealPresentation(bool savePendingPurchase)
        {
            // Two frame yields are intentional: the coroutine resumes before IMGUI,
            // so the second yield gives the modal and cached card backs one complete
            // rendered frame before synchronous persistence work begins.
            yield return null;
            yield return null;
            if (savePendingPurchase) SaveGame();

            var qualityIds = new string[pendingStoreQualities.Length];
            for (var index = 0; index < pendingStoreQualities.Length; index++)
                qualityIds[index] = CharacterQualityResourceId((CharacterQuality)pendingStoreQualities[index]);
            yield return artCatalog.PreloadStoreRevealAssets(pendingStoreDefinitionIds, qualityIds);
            storeRevealPreloadComplete = true;
            storeRevealPreparationCoroutine = null;
        }

        private void ClaimPendingStorePack()
        {
            if (!storeOpeningActive || pendingStoreDefinitionIds == null ||
                pendingStoreInstanceIds == null || pendingStoreQualities == null ||
                pendingStoreAptitudeRolls == null ||
                pendingStoreDefinitionIds.Length != StoreRules.DrawCountPerCharacterPack ||
                pendingStoreInstanceIds.Length != StoreRules.DrawCountPerCharacterPack ||
                pendingStoreQualities.Length != StoreRules.DrawCountPerCharacterPack ||
                pendingStoreAptitudeRolls.Length != StoreRules.DrawCountPerCharacterPack)
            {
                storeStatusMessage = T("store.status.noPending");
                return;
            }

            var starterGrantReveal = pendingStoreRevealAlreadyGranted;
            if (!starterGrantReveal)
            {
                var entries = new List<OwnedCharacterState>(playerCollection.Entries);
                var acquiredAtUtcTicks = PlayerCollection.NextAcquiredAtUtcTicks(
                    entries, DateTime.UtcNow.Ticks);
                for (var i = 0; i < StoreRules.DrawCountPerCharacterPack; i++)
                {
                    entries.Add(new OwnedCharacterState(
                        pendingStoreInstanceIds[i],
                        pendingStoreDefinitionIds[i],
                        1,
                        0,
                        (CharacterQuality)pendingStoreQualities[i],
                        acquiredAtUtcTicks++,
                        pendingStoreAptitudeRolls[i]));
                }

                var selectedIds = nextRunDeckSelection == null
                    ? new string[0] : CopyIds(nextRunDeckSelection.SelectedInstanceIds);
                playerCollection = new PlayerCollection(characterCatalog, entries);
                nextRunDeckSelection = new RunDeckSelection(playerCollection, selectedIds,
                    MaximumDeckSize, MinimumDeckSize);
            }
            else
            {
                acknowledgedStarterGrantId = pendingStarterGrantId;
            }
            pendingStoreOrderId = null;
            pendingStoreInstanceIds = new string[0];
            pendingStoreDefinitionIds = new string[0];
            pendingStoreQualities = new int[0];
            pendingStoreAptitudeRolls = new int[0];
            pendingStoreRevealAlreadyGranted = false;
            pendingStarterGrantId = null;
            storeOpeningProgress = 0f;
            storeRevealLoadingElapsed = 0f;
            storeRevealAssetsReady = false;
            storeRevealPreloadComplete = false;
            storeOpeningActive = false;
            storeStatusMessage = T(starterGrantReveal
                ? "starterGrant.status.claimed" : "store.status.claimed");
            if (starterGrantReveal) currentView = ScreenView.Home;
            SaveGame();
        }

        private void DrawStoreOpeningOverlay()
        {
            GUI.color = new Color(0.01f, 0.02f, 0.03f, 0.86f);
            GUI.DrawTexture(new Rect(0f, 0f, DesignWidth, layoutContentHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            uiSkin.DrawPanel(new Rect(42f, 190f, 996f, 1500f), true);
            GUI.Label(new Rect(72f, 225f, 936f, 62f),
                T(pendingStoreRevealAlreadyGranted
                    ? "starterGrant.opening.title" : "store.opening.title"),
                Style(32, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(72f, 292f, 936f, 40f),
                !storeRevealAssetsReady || storeOpeningProgress < 1f
                    ? T("store.opening")
                    : T(pendingStoreRevealAlreadyGranted
                        ? "starterGrant.reveal.ready" : "store.reveal.ready"),
                Style(17, new Color(0.74f, 0.84f, 0.89f), TextAnchor.MiddleCenter));

            if (!storeRevealAssetsReady)
            {
                DrawStoreRevealLoadingAnimation();
                GUI.enabled = false;
                GUI.Button(new Rect(300f, 1240f, 480f, 86f),
                    T(pendingStoreRevealAlreadyGranted ? "starterGrant.claim" : "store.claim"),
                    ButtonStyle(UiButtonSemantic.Primary, 23));
                GUI.enabled = true;
                GUI.Label(new Rect(72f, 1350f, 936f, 120f),
                    T(pendingStoreRevealAlreadyGranted
                        ? "starterGrant.reveal.hint" : "store.reveal.hint"),
                    Style(16, new Color(0.70f, 0.78f, 0.82f), TextAnchor.MiddleCenter));
                return;
            }

            var revealIndex = Mathf.Clamp(Mathf.FloorToInt(storeOpeningProgress * 3.5f), 0, 3);
            storeRevealSelectedIndex = Mathf.Clamp(storeRevealSelectedIndex, 0,
                StoreRules.DrawCountPerCharacterPack - 1);
            for (var i = 0; i < StoreRules.DrawCountPerCharacterPack; i++)
            {
                if (i == storeRevealSelectedIndex) continue;
                DrawStoreRevealCard(i, revealIndex, false);
            }
            DrawStoreRevealCard(storeRevealSelectedIndex, revealIndex, true);
            HandleStoreRevealCardSelection(revealIndex);

            GUI.enabled = storeOpeningProgress >= 1f;
            if (GUI.Button(new Rect(300f, 1240f, 480f, 86f),
                    T(pendingStoreRevealAlreadyGranted ? "starterGrant.claim" : "store.claim"),
                    ButtonStyle(UiButtonSemantic.Primary, 23)))
            {
                PlayUiClick(UiAudioCueIds.Confirm, "store-claim");
                ClaimPendingStorePack();
            }
            GUI.enabled = true;
            GUI.Label(new Rect(72f, 1350f, 936f, 120f),
                T(pendingStoreRevealAlreadyGranted
                    ? "starterGrant.reveal.hint" : "store.reveal.hint"),
                Style(16, new Color(0.70f, 0.78f, 0.82f), TextAnchor.MiddleCenter));
        }

        private void DrawStoreRevealLoadingAnimation()
        {
            var bob = Mathf.Sin(storeRevealLoadingElapsed * Mathf.PI * 2.4f) * 8f;
            DrawStoreCardBackFan(new Rect(321f, 475f + bob, 438f, 330f));
            var dotCount = 1 + Mathf.FloorToInt(storeRevealLoadingElapsed * 2.5f) % 3;
            GUI.Label(new Rect(270f, 890f, 540f, 70f), T("store.opening") + new string('·', dotCount),
                Style(25, new Color(0.84f, 0.78f, 0.60f), TextAnchor.MiddleCenter));
        }

        private void DrawStoreRevealCard(int index, int revealIndex, bool selected)
        {
            var area = StoreRevealCardRect(index, selected);
            var previousMatrix = GUI.matrix;
            if (!selected)
            {
                var angle = index == 0 ? -5f : index == 2 ? 5f : 0f;
                GUIUtility.RotateAroundPivot(angle, area.center);
            }

            if (index < revealIndex)
            {
                DrawStoreRevealDetailCard(area, pendingStoreDefinitionIds[index],
                    (CharacterQuality)pendingStoreQualities[index], pendingStoreInstanceIds[index],
                    pendingStoreAptitudeRolls[index]);
            }
            else
            {
                var shell = artCatalog.GetDetailTideWindowTexture(
                    PrototypeArtResourceIds.DetailTideWindowShell);
                GUI.color = new Color(0.34f, 0.42f, 0.44f, 1f);
                if (shell != null) GUI.DrawTexture(area, shell, ScaleMode.ScaleToFit, true);
                GUI.color = Color.white;
                GUI.Label(new Rect(area.x + 40f, area.center.y - 60f, area.width - 80f, 120f),
                    index == 0 ? T("store.reveal.waiting") : "?",
                    Style(index == 0 ? 24 : 72, new Color(0.74f, 0.84f, 0.89f),
                        TextAnchor.MiddleCenter));
            }
            GUI.matrix = previousMatrix;
        }

        private Rect StoreRevealCardRect(int index, bool selected)
        {
            const float cardWidth = 450f;
            var cardHeight = cardWidth * DetailTideWindowLayout.ModalHeight /
                DetailTideWindowLayout.ModalWidth;
            var x = 65f + index * 250f;
            var fanY = index == 1 ? 400f : 438f;
            return new Rect(x, selected ? 338f : fanY, cardWidth, cardHeight);
        }

        private void HandleStoreRevealCardSelection(int revealIndex)
        {
            if (revealIndex < StoreRules.DrawCountPerCharacterPack || Event.current == null ||
                Event.current.type != EventType.MouseUp || Event.current.button != 0) return;
            var pointer = Event.current.mousePosition;
            if (StoreRevealCardRect(storeRevealSelectedIndex, true).Contains(pointer)) return;
            for (var index = StoreRules.DrawCountPerCharacterPack - 1; index >= 0; index--)
            {
                if (index == storeRevealSelectedIndex ||
                    !StoreRevealCardRect(index, false).Contains(pointer)) continue;
                storeRevealSelectedIndex = index;
                PlayUiClick(UiAudioCueIds.Tap, "store-reveal-card-" + index);
                Event.current.Use();
                return;
            }
        }

        private void DrawStoreRevealDetailCard(
            Rect area, string definitionId, CharacterQuality quality, string instanceId,
            int aptitudeRoll)
        {
            var definition = characterCatalog.Get(definitionId);
            var qualityId = CharacterQualityResourceId(quality);
            aptitudeRoll = aptitudeRoll < 0
                ? CharacterRarityInstanceRules.ComputeAptitudeRoll(instanceId)
                : aptitudeRoll;
            var stats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, quality, aptitudeRoll, 0);
            var ivory = ArchiveColor("F6E8C8");
            var previousMatrix = GUI.matrix;
            var scale = area.width / DetailTideWindowLayout.ModalWidth;
            GUI.matrix = previousMatrix * Matrix4x4.TRS(
                new Vector3(area.x, area.y, 0f), Quaternion.identity,
                new Vector3(scale, scale, 1f));
            var panel = new Rect(0f, 0f, DetailTideWindowLayout.ModalWidth,
                DetailTideWindowLayout.ModalHeight);
            var shell = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailTideWindowShell);
            if (shell != null) GUI.DrawTexture(panel, shell, ScaleMode.StretchToFill, true);

            var instanceLabel = T("store.reveal.instance") + " · " + instanceId;
            var instanceStyle = DetailTextStyle(18, new Color(0.72f, 0.78f, 0.78f),
                TextAnchor.MiddleCenter, false, FontStyle.Normal);
            while (instanceStyle.fontSize > 13 &&
                   instanceStyle.CalcSize(new GUIContent(instanceLabel)).x > 760f)
                instanceStyle.fontSize--;
            GUI.Label(new Rect(60f, 24f, 760f, 32f),
                instanceLabel, instanceStyle);
            GUI.Label(DetailLocalRect(panel, 190f, 70f, 500f, 72f),
                CharacterDisplayName(definitionId, quality),
                DetailTextStyle(46, ivory, TextAnchor.MiddleCenter, false, FontStyle.Bold));
            DrawDetailTags(panel, definitionId, definition, false, ivory);

            var badge = artCatalog.GetUnitLevelBadgeTexture(qualityId, 1);
            if (badge != null)
                GUI.DrawTexture(DetailLocalRect(panel, 290f, 230f, 300f, 88f), badge,
                    ScaleMode.ScaleToFit, true);

            DrawDetailMetadataField(panel, 202f, T(QualityKey(quality)), ivory);
            DrawDetailMetadataField(panel, 448f, T("collection.notStarred"), ivory);

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(definitionId, qualityId, out artDefinition, out sprite))
                DrawDetailCharacterSprite(DetailLocalRect(panel, 45f, 390f, 500f, 580f),
                    artDefinition, sprite, 0.90f);

            DrawDetailStat(panel, new Rect(574f, 393f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatHealth, T("stat.health"), stats.MaxHealth.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 549f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatAttack, T("stat.attack"), stats.AttackDamage.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 705f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatArmor, T("stat.armor"), stats.Armor.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 861f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatSpeed, T("detail.attackSpeed"),
                DetailTideWindowLayout.FormatAttackSpeed(stats.AttackSpeedBasisPoints), ivory);

            var quoteStyle = DetailTextStyle(29, ivory, TextAnchor.MiddleCenter, true, FontStyle.Normal);
            GUI.Label(DetailLocalRect(panel, 90f, 1078f, 700f, 132f),
                DetailQuote(definitionId, qualityId), quoteStyle);
            GUI.Label(DetailLocalRect(panel, 307f, 1261f, 268f, 74f), T(QualityKey(quality)),
                DetailTextStyle(29, QualityColor(quality), TextAnchor.MiddleCenter,
                    false, FontStyle.Bold));
            GUI.matrix = previousMatrix;
        }

        private void DrawArenaScreen()
        {
            GrantArenaChallengeTestResetIfNeeded();
            RefreshArenaRuntimeState();
            if (arenaPageState == ArenaPageState.Preparation) { DrawArenaPreparation(); return; }
            if (arenaPageState == ArenaPageState.Result) { DrawArenaResult(); return; }

            var modalOpen = selectedArenaOpponentIndex >= 0 &&
                (arenaPageState == ArenaPageState.OpponentDetail || arenaPageState == ArenaPageState.ChallengeConfirm);
            DrawArenaModule(ArenaFormalArtContract.MapBackground,
                new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);
            DrawArenaText(new Rect(92f, 62f, 650f, 98f), T("arena.title"), 78,
                ArenaIvory, TextAnchor.UpperLeft, true);
            DrawArenaText(new Rect(112f, 164f, 790f, 42f), T("arena.subtitle"), 27,
                ArenaIvory, TextAnchor.UpperLeft, false);
            DrawArenaText(new Rect(910f, 12f, 120f, 30f), ClientVersionLabel, 16,
                ArenaIvory, TextAnchor.MiddleCenter, false);

            var nowUtc = DateTime.UtcNow;
            var cycleRemaining = ArenaPrototypeRules.RemainingInCycle(
                nowUtc, ArenaPrototypeRules.PvpRewardCycleDays);
            var cycleNumber = Mathf.Max(1, Mathf.FloorToInt((float)(
                nowUtc - ArenaPrototypeRules.CycleEpochUtc).TotalDays /
                ArenaPrototypeRules.PvpRewardCycleDays) + 1);
            var cycleEndUtc = nowUtc + cycleRemaining;
            DrawArenaText(new Rect(760f, 62f, 260f, 40f),
                string.Format(T("arena.cycleTitle"), cycleNumber), 24,
                ArenaIvory, TextAnchor.MiddleRight, true);
            DrawArenaText(new Rect(720f, 105f, 300f, 36f),
                string.Format(T("arena.cycleCountdown"), FormatArenaCountdown(cycleRemaining)), 19,
                ArenaIvory, TextAnchor.MiddleRight, false);
            DrawArenaText(new Rect(700f, 145f, 320f, 34f),
                FormatArenaLockTime(cycleEndUtc), 16,
                ArenaIvory, TextAnchor.MiddleRight, false);

            var displayRanking = ArenaPrototypeRules.CreateDisplayRanking(
                arenaOpponents, arenaRating, CurrentPvpDeckPower(), T("home.playerName"));
            var playerDisplayRank = ArenaPrototypeRules.CalculatePlayerDisplayRank(arenaOpponents, arenaRating);
            var playerRowIndex = 0;
            for (var index = 0; index < displayRanking.Length; index++)
            {
                if (!displayRanking[index].IsCurrentPlayer) continue;
                playerRowIndex = index;
                break;
            }

            DrawArenaModule(ArenaFormalArtContract.StatusPanel,
                new Rect(40f, 244f, 1000f, 190f), 1000, 190);
            DrawArenaModule(ArenaFormalArtContract.TrophyIcon,
                new Rect(88f, 304f, 64f, 64f), 64, 64);
            DrawArenaModule(ArenaFormalArtContract.SwordsIcon,
                new Rect(414f, 304f, 64f, 64f), 64, 64);
            DrawArenaModule(ArenaFormalArtContract.ClockIcon,
                new Rect(738f, 304f, 64f, 64f), 64, 64);
            DrawArenaText(new Rect(156f, 278f, 202f, 40f), T("arena.ratingLabel"), 22,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(482f, 278f, 202f, 40f), T("arena.rankLabel"), 22,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(806f, 278f, 202f, 40f), T("arena.dailyLabel"), 22,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(156f, 318f, 202f, 52f), arenaRating.ToString(), 35,
                ArenaCopper, TextAnchor.MiddleCenter, false);
            DrawArenaText(new Rect(482f, 318f, 202f, 52f),
                "#" + playerDisplayRank, 35,
                ArenaInk, TextAnchor.MiddleCenter, false);
            DrawArenaText(new Rect(806f, 318f, 202f, 52f),
                arenaChallengesRemaining + "/" + ArenaPrototypeRules.DailyChallengeLimit, 35,
                ArenaInk, TextAnchor.MiddleCenter, false);
            DrawArenaText(new Rect(72f, 446f, 292f, 34f),
                T("arena.rewardEligibility") + " —", 22,
                ArenaIvory, TextAnchor.MiddleCenter, false);
            DrawArenaText(new Rect(394f, 446f, 292f, 34f),
                T("arena.defenseScoring") + " —", 22,
                ArenaIvory, TextAnchor.MiddleCenter, false);
            DrawArenaText(new Rect(716f, 446f, 292f, 34f),
                T("arena.estimatedPool") + " —", 22,
                ArenaIvory, TextAnchor.MiddleCenter, false);

            DrawArenaModule(ArenaFormalArtContract.MainBoard,
                new Rect(40f, 500f, 1000f, 1600f), 1000, 1600);
            DrawArenaText(new Rect(100f, 612f, 880f, 70f), T("arena.nearby"), 34,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(130f, 686f, 108f, 42f), T("arena.rankColumn"), 19,
                ArenaInk, TextAnchor.MiddleCenter, false);
            DrawArenaText(new Rect(246f, 686f, 390f, 42f), T("arena.playerColumn"), 19,
                ArenaInk, TextAnchor.MiddleLeft, false);
            DrawArenaText(new Rect(654f, 686f, 266f, 42f), T("arena.ratingColumn"), 19,
                ArenaInk, TextAnchor.MiddleRight, false);

            var previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && !modalOpen;
            var rankingViewport = new Rect(130f, 736f, 820f, 1056f);
            var rankingContentHeight = Mathf.Max(rankingViewport.height, displayRanking.Length * 96f);
            var rankingMaxScroll = Mathf.Max(0f, rankingContentHeight - rankingViewport.height);
            if (!arenaRankingScrollInitialized || arenaRankingContentCount != displayRanking.Length)
            {
                arenaRankingScroll.y = Mathf.Clamp((playerRowIndex - 5) * 96f, 0f, rankingMaxScroll);
                arenaRankingContentCount = displayRanking.Length;
                arenaRankingScrollInitialized = true;
            }
            arenaRankingScroll.y = Mathf.Clamp(arenaRankingScroll.y, 0f, rankingMaxScroll);
            HandleArenaRankingPointer(rankingViewport, rankingMaxScroll, !modalOpen);
            arenaRankingScroll = GUI.BeginScrollView(rankingViewport, arenaRankingScroll,
                new Rect(0f, 0f, 820f, rankingContentHeight), false, false,
                GUIStyle.none, GUIStyle.none);
            for (var row = 0; row < displayRanking.Length; row++)
            {
                var area = new Rect(0f, row * 96f, 820f, 96f);
                var rankingRow = displayRanking[row];
                DrawArenaModule(rankingRow.IsCurrentPlayer
                        ? ArenaFormalArtContract.PlayerRankRow
                        : ArenaFormalArtContract.RankRow,
                    area, 820, 96);
                DrawArenaText(new Rect(area.x + 16f, area.y, 92f, 96f), "#" + rankingRow.Rank, 24,
                    ArenaInk, TextAnchor.MiddleCenter, true);
                DrawArenaText(new Rect(area.x + 116f, area.y, 390f, 96f), rankingRow.DisplayName, 23,
                    ArenaInk, TextAnchor.MiddleLeft, rankingRow.IsCurrentPlayer);
                DrawArenaText(new Rect(area.x + 524f, area.y, 266f, 96f),
                    rankingRow.Rating.ToString("N0"), 21,
                    ArenaCopper, TextAnchor.MiddleRight, false);
            }
            GUI.EndScrollView();

            DrawArenaText(new Rect(118f, 1810f, 844f, 54f),
                string.Format(T("arena.myRank"), playerDisplayRank), 22,
                ArenaMutedInk, TextAnchor.MiddleCenter, false);
            var defenseButton = new Rect(348f, 1918f, 220f, 68f);
            if (DrawArenaFormalButton(defenseButton, T("arena.defense"), true, false))
            {
                PlayUiClick(UiAudioCueIds.Tap, "arena-defense");
                var removed = ReconcileDefenseWithActiveDeck();
                if (removed > 0) pendingDefenseRemovedCount += removed;
                defenseMessageKey = pendingDefenseRemovedCount > 0
                    ? "defense.deckChangedRemoved" : "defense.subtitle";
                RefreshDefenseFormationBench();
                SaveGame();
                currentView = ScreenView.Defense;
            }
            var findButton = new Rect(590f, 1909f, 390f, 86f);
            if (DrawArenaFormalButton(findButton, T("arena.findOpponents"), true, true))
            {
                var candidates = ArenaPrototypeRules.SelectNearby(arenaOpponents, playerDisplayRank, 6);
                if (candidates.Length > 0)
                    SelectArenaOpponent(Array.IndexOf(arenaOpponents, candidates[0]));
            }
            DrawMainNavigation();
            GUI.enabled = previousEnabled;
            if (selectedArenaOpponentIndex >= 0) DrawArenaOpponentDetail(arenaOpponents[selectedArenaOpponentIndex]);
            if (arenaPageState == ArenaPageState.ChallengeConfirm) DrawArenaChallengeConfirmation();
        }

        private void HandleArenaRankingPointer(Rect viewport, float maxScroll, bool enabled)
        {
            var current = Event.current;
            if (!enabled || current == null)
            {
                arenaRankingPointerHeld = false;
                arenaRankingPointerDragging = false;
                return;
            }

            if (current.type == EventType.MouseDown && current.button == 0 &&
                viewport.Contains(current.mousePosition))
            {
                arenaRankingPointerHeld = true;
                arenaRankingPointerDragging = false;
                arenaRankingPointerLast = current.mousePosition;
                return;
            }

            if (current.type == EventType.MouseDrag && arenaRankingPointerHeld)
            {
                var delta = current.mousePosition - arenaRankingPointerLast;
                if (Mathf.Abs(delta.y) >= 1f) arenaRankingPointerDragging = true;
                arenaRankingScroll.y = Mathf.Clamp(arenaRankingScroll.y - delta.y, 0f, maxScroll);
                arenaRankingPointerLast = current.mousePosition;
                current.Use();
                return;
            }

            if (current.type != EventType.MouseUp || current.button != 0 ||
                !arenaRankingPointerHeld) return;
            if (arenaRankingPointerDragging) current.Use();
            arenaRankingPointerHeld = false;
            arenaRankingPointerDragging = false;
        }

        private string FormatArenaLockTime(DateTime cycleEndUtc)
        {
            cycleEndUtc = cycleEndUtc.Kind == DateTimeKind.Utc
                ? cycleEndUtc : cycleEndUtc.ToUniversalTime();
            return language == GameLanguage.ChineseSimplified
                ? cycleEndUtc.ToString("M月d日  HH:mm 'UTC' 锁定")
                : cycleEndUtc.ToString("MMM d  HH:mm 'UTC LOCK'");
        }

        private void DrawArenaOpponentDetail(ArenaOpponentProfile opponent)
        {
            var panel = new Rect(60f, 420f, 960f, 1560f);
            if (arenaPageState == ArenaPageState.OpponentDetail && Event.current != null &&
                Event.current.type == EventType.MouseUp && Event.current.button == 0 &&
                !panel.Contains(Event.current.mousePosition))
            {
                CloseArenaOpponentDetail("arena-opponent-detail-dismiss");
                Event.current.Use();
                return;
            }
            DrawArenaModule(ArenaFormalArtContract.DimOverlay,
                new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);
            DrawArenaModule(ArenaFormalArtContract.OpponentPickerShell, panel, 960, 1560);
            DrawArenaText(new Rect(130f, 526f, 760f, 78f), T("arena.opponentPickerTitle"), 34,
                ArenaIvory, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(130f, 652f, 820f, 48f),
                T("arena.candidateRemaining") + " —", 20,
                ArenaMutedInk, TextAnchor.MiddleCenter, false);

            var playerDisplayRank = ArenaPrototypeRules.CalculatePlayerDisplayRank(arenaOpponents, arenaRating);
            var candidates = ArenaPrototypeRules.SelectNearby(arenaOpponents, playerDisplayRank, 6);
            for (var row = 0; row < candidates.Length; row++)
            {
                var candidate = candidates[row];
                var candidateIndex = Array.IndexOf(arenaOpponents, candidate);
                var selected = candidateIndex == selectedArenaOpponentIndex;
                var rowArea = new Rect(130f, 782f + row * 96f, 820f, 96f);
                if (selected)
                    DrawArenaSelectedCandidateRow(rowArea);
                else
                    DrawArenaModule(ArenaFormalArtContract.RankRow, rowArea, 820, 96);
                DrawArenaText(new Rect(rowArea.x + 28f, rowArea.y, 330f, 96f), candidate.DisplayName, 23,
                    selected ? ArenaIvory : ArenaInk, TextAnchor.MiddleLeft, selected);
                DrawArenaText(new Rect(rowArea.x + 372f, rowArea.y, 174f, 96f),
                    string.Format(T("arena.rating"), candidate.Rating), 20,
                    selected ? ArenaIvory : ArenaCopper, TextAnchor.MiddleCenter, false);
                DrawArenaText(new Rect(rowArea.x + 540f, rowArea.y, 170f, 96f),
                    T("arena.estimatedChange") + " +" + ArenaPrototypeRules.TemporaryWinRatingDelta,
                    17, selected ? ArenaIvory : ArenaMutedInk, TextAnchor.MiddleRight, false);
                var chevronArea = new Rect(858f, 806f + row * 96f, 48f, 48f);
                DrawArenaModule(selected ? ArenaFormalArtContract.ChevronPressed : ArenaFormalArtContract.ChevronNormal,
                    chevronArea, 48, 48);
                if (arenaPageState == ArenaPageState.OpponentDetail &&
                    GUI.Button(rowArea, GUIContent.none, GUIStyle.none))
                    SelectArenaOpponent(candidateIndex);
            }

            DrawArenaText(new Rect(130f, 1358f, 820f, 54f),
                opponent.DisplayName + " · " + T("arena.defensePreview"), 24,
                ArenaInk, TextAnchor.MiddleCenter, true);
            for (var slot = 0; slot < ArenaPrototypeRules.DefenseSlotCount; slot++)
            {
                var area = new Rect(138f + slot * 134f, 1434f, 134f, 210f);
                DrawArenaModule(ArenaFormalArtContract.DefenderSlot, area, 134, 210);
                if (slot >= ArenaPrototypeRules.PublicDefenseSlotCount)
                {
                    DrawArenaText(new Rect(area.x, area.y + 17f, 134f, 176f), T("arena.hidden"), 48,
                        ArenaIvory, TextAnchor.MiddleCenter, true);
                    continue;
                }
                DrawArenaModule(ArenaFormalArtContract.PublicDefenders[slot],
                    new Rect(area.x + 8f, area.y + 17f, 118f, 176f), 118, 176);
            }

            var detailInteractive = arenaPageState == ArenaPageState.OpponentDetail;
            var closeButton = new Rect(816f, 530f, 72f, 72f);
            DrawArenaModule(detailInteractive && Event.current != null && Input.GetMouseButton(0) &&
                    closeButton.Contains(Event.current.mousePosition)
                    ? ArenaFormalArtContract.ClosePressed
                    : detailInteractive ? ArenaFormalArtContract.CloseNormal : ArenaFormalArtContract.CloseDisabled,
                closeButton, 72, 72);
            if (detailInteractive && GUI.Button(closeButton, GUIContent.none, GUIStyle.none))
            {
                CloseArenaOpponentDetail("arena-opponent-detail-close");
                return;
            }
            var challengeButton = new Rect(345f, 1760f, 390f, 86f);
            if (DrawArenaFormalButton(challengeButton,
                    T("arena.challenge") + " " + arenaChallengesRemaining + "/" +
                    ArenaPrototypeRules.DailyChallengeLimit,
                    detailInteractive && arenaChallengesRemaining > 0, true))
            {
                PlayUiClick(UiAudioCueIds.Confirm, "arena-challenge-prompt");
                arenaPageState = ArenaPageState.ChallengeConfirm;
            }
        }

        private bool DrawArenaFormalButton(Rect area, string label, bool enabled, bool extraLarge)
        {
            var pressed = enabled && Event.current != null &&
                area.Contains(Event.current.mousePosition) && Input.GetMouseButton(0);
            var resourceId = extraLarge
                ? enabled
                    ? pressed ? ArenaFormalArtContract.ButtonXlPressed : ArenaFormalArtContract.ButtonXlNormal
                    : ArenaFormalArtContract.ButtonXlDisabled
                : enabled
                    ? pressed ? ArenaFormalArtContract.ButtonMPressed : ArenaFormalArtContract.ButtonMNormal
                    : ArenaFormalArtContract.ButtonMDisabled;
            DrawArenaModule(resourceId, area, Mathf.RoundToInt(area.width), Mathf.RoundToInt(area.height));
            DrawArenaText(area, label, extraLarge ? 27 : 22,
                enabled ? ArenaIvory : ArenaMutedInk, TextAnchor.MiddleCenter, true);
            return enabled && GUI.Button(area, GUIContent.none, GUIStyle.none);
        }

        private static readonly Color ArenaIvory = new Color32(241, 231, 191, 255);
        private static readonly Color ArenaInk = new Color32(22, 57, 70, 255);
        private static readonly Color ArenaMutedInk = new Color32(94, 94, 78, 255);
        private static readonly Color ArenaCopper = new Color32(156, 83, 47, 255);

        private void DrawArenaText(Rect rect, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized)
        {
            var style = ArchiveStyle(fontSize, color, alignment, emphasized);
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            GUI.Label(rect, new GUIContent(text), style);
        }

        private void DrawArenaModule(string resourceId, Rect rect, int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetArenaETexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight) return;
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawArenaSelectedCandidateRow(Rect rowArea)
        {
            // The accepted pressed-state artwork is intentionally translucent. Re-compositing the
            // same unmodified 1:1 formal asset makes the selected opponent unmistakable without
            // generating, recolouring, cropping, or resizing any art resource.
            for (var pass = 0; pass < 4; pass++)
                DrawArenaModule(ArenaFormalArtContract.PlayerRankRow, rowArea, 820, 96);
        }

        private void CloseArenaOpponentDetail(string audioSource)
        {
            PlayUiClick(UiAudioCueIds.Cancel, audioSource);
            selectedArenaOpponentIndex = -1;
            arenaPageState = ArenaPageState.Ranking;
        }

        private void DrawArenaChallengeConfirmation()
        {
            var result = DrawCommonConfirmDialog(T("arena.confirmTitle"),
                string.Format(T("arena.confirmBody"), arenaChallengesRemaining,
                    Mathf.Max(0, arenaChallengesRemaining - 1)), string.Empty,
                T("arena.cancel"), T("arena.confirm"));
            if (result == 0)
            {
                PlayUiClick(UiAudioCueIds.Cancel, "arena-challenge-cancel");
                arenaPageState = ArenaPageState.OpponentDetail;
            }
            else if (result == 1) ConfirmArenaChallenge();
        }

        private void DrawArenaPreparation()
        {
            DrawSharedFormationPage(arenaAttackSlots, arenaAttackBenchSlots, true);
        }

        private void DrawArenaFormation(Rect panel)
        {
            DrawSharedFormationPage(arenaAttackSlots, arenaAttackBenchSlots, true);
        }

        private void DrawSharedFormationPage(UnitData[] boardSlots, UnitData[] benchSlots, bool arenaChallenge)
        {
            var responsiveLayout = PreparationBattlefieldLayoutMath.Calculate(layoutContentHeight);
            var startArea = ToRectAtY(FormationBattleV94Layout.StartControl, responsiveLayout.StartButtonY);
            HandleSharedFormationPointer(boardSlots, benchSlots);

            UnitData[] opponentSlots = null;
            if (arenaChallenge && selectedArenaOpponentIndex >= 0 && selectedArenaOpponentIndex < arenaOpponents.Length)
            {
                var opponent = arenaOpponents[selectedArenaOpponentIndex];
                opponentSlots = new UnitData[Mathf.Min(ArenaPrototypeRules.DefenseSlotCount, opponent.DefenseUnits.Length)];
                for (var slot = 0; slot < opponentSlots.Length; slot++) opponentSlots[slot] = CreateArenaProfileUnit(opponent.DefenseUnits[slot]);
            }

            for (var slot = 0; slot < ArenaPrototypeRules.DefenseSlotCount; slot++)
            {
                var opponent = opponentSlots != null && slot < opponentSlots.Length ? opponentSlots[slot] : null;
                DrawFormationPlacementRing(BattlePresentationLayout.GetUnitFootPosition(false, slot, layoutContentHeight), opponent != null, 1f, 1f);
                if (opponent != null)
                {
                    DrawSharedFormationUnit(opponent, slot, false, 0.94f);
                    if (arenaFormationDetailUnit == null && GUI.Button(
                            PreparationEnemySlotRect(slot), GUIContent.none, GUIStyle.none))
                    {
                        arenaFormationDetailUnit = opponent;
                        arenaFormationDetailUsesPlayerLantern = false;
                        PlayUiClick(UiAudioCueIds.Tap, "arena-opponent-formation-unit-detail");
                    }
                }
            }
            for (var slot = 0; slot < boardSlots.Length; slot++)
            {
                DrawFormationPlacementRing(BattlePresentationLayout.GetUnitFootPosition(true, slot, layoutContentHeight), boardSlots[slot] != null, 1f, 1f);
                if (boardSlots[slot] != null)
                    DrawSharedFormationUnit(boardSlots[slot], slot, true,
                        preparationPointerHeld && preparationPointerSlot == slot && preparationPointerDragging ? 0.32f : 1f);
            }

            DrawFormationBottomControl(startArea, arenaChallenge ? T("arena.start") : T("defense.save"), true, 1f, null);
            DrawSharedFormationBench(benchSlots);
            DrawSharedFormationDragGhost(boardSlots, benchSlots);

            if (arenaFormationDetailUnit != null) return;
            GUI.enabled = CountUnits(boardSlots) > 0;
            if (GUI.Button(startArea, GUIContent.none, GUIStyle.none))
            {
                if (arenaChallenge)
                {
                    PlayUiClick(UiAudioCueIds.BattleStart, "arena-battle-start");
                    StartArenaBattle();
                }
                else
                {
                    PlayUiClick(UiAudioCueIds.Confirm, "defense-save");
                    arenaDefenseLanternSnapshot = CurrentLanternSnapshot();
                    arenaDefenseSnapshotVersion++;
                    arenaDefenseNeedsResync = false;
                    pendingDefenseRemovedCount = 0;
                    defenseMessageKey = "defense.saved";
                    SaveGame();
                    currentView = ScreenView.Arena;
                }
                ResetSharedFormationPointer();
            }
            GUI.enabled = true;
        }

        private void DrawSharedFormationUnit(UnitData unit, int slot, bool isPlayer, float opacity)
        {
            DrawPreparationBattlefieldUnit(isPlayer ? PreparationBoardSlotRect(slot) : PreparationEnemySlotRect(slot),
                unit.NameKey, slot, isPlayer, CharacterDisplayName(unit.NameKey, unit.Quality),
                unit.RunLevel, opacity,
                CharacterQualityResourceId(unit.Quality));
        }

        private void DrawSharedFormationBench(UnitData[] benchSlots)
        {
            for (var index = 0; index < benchSlots.Length; index++)
            {
                var area = PreparationBenchSlotRect(index);
                var visualArea = NativeCharacterAvatarRect(area, AvatarArtContext.SlotCircle);
                var unit = benchSlots[index];
                if (unit == null)
                {
                    var empty = artCatalog.GetExpeditionUiV67Texture("ART-UI-PREPARATION-SLOT-FRAME-104-V6.7.0");
                    if (empty != null) GUI.DrawTexture(visualArea, empty, ScaleMode.StretchToFill, true);
                    continue;
                }
                var surface = artCatalog.GetExpeditionUiSharedTexture(AvatarArtContract.OccupiedSlotSurfaceResourceId);
                var rim = artCatalog.GetExpeditionUiSharedTexture(AvatarArtContract.SlotForegroundRimResourceId);
                if (surface != null) GUI.DrawTexture(visualArea, surface, ScaleMode.StretchToFill, true);
                DrawCharacterSlotAvatar(unit.NameKey, unit.Quality, visualArea);
                if (rim != null) GUI.DrawTexture(visualArea, rim, ScaleMode.StretchToFill, true);
            }
        }

        private void DrawArenaResult()
        {
            // Accepted Result A v1.1.0: actual final battlefield -> runtime veil ->
            // native full shell -> shared swords icon -> state overlay -> dynamic text.
            GUI.color = new Color32(4, 17, 24, 118);
            GUI.DrawTexture(new Rect(0f, 0f, 1080f, 2400f), Texture2D.whiteTexture);
            GUI.color = Color.white;

            DrawArenaBattleResultAModule(ArenaBattleResultAFormalArtContract.FullShell,
                new Rect(24f, 350f, 1032f, 728f), 1032, 728);
            DrawArenaModule(ArenaFormalArtContract.SwordsIcon,
                new Rect(205f, 854f, 64f, 64f), 64, 64);

            var returnArea = new Rect(310f, 936f, 460f, 96f);
            var canReturn = arenaResultCommitted && !arenaResultReturnPending;
            var pressed = canReturn && Event.current != null &&
                          Event.current.type == EventType.MouseDown &&
                          returnArea.Contains(Event.current.mousePosition);
            if (!canReturn)
            {
                DrawArenaBattleResultAModule(
                    ArenaBattleResultAFormalArtContract.ButtonDisabledOverlay,
                    returnArea, 460, 96);
            }
            else if (pressed)
            {
                DrawArenaBattleResultAModule(
                    ArenaBattleResultAFormalArtContract.ButtonPressedOverlay,
                    returnArea, 460, 96);
            }

            DrawArenaText(new Rect(190f, 500f, 700f, 120f),
                T(arenaLastBattleWon ? "arena.resultWin" : "arena.resultLoss"), 66,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(340f, 676f, 400f, 42f), T("arena.ratingLabel"), 27,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(145f, 720f, 215f, 80f), arenaPreviousRating.ToString("N0"), 51,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(360f, 720f, 100f, 80f), "→", 51,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(455f, 714f, 190f, 92f), FormatArenaRatingDelta(), 66,
                ArenaResultDeltaColor(), TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(660f, 720f, 250f, 80f), arenaRating.ToString("N0"), 51,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(270f, 858f, 610f, 90f),
                string.Format(T("arena.power"), arenaBattlePowerSnapshot), 32,
                ArenaInk, TextAnchor.MiddleCenter, true);
            DrawArenaText(new Rect(310f, 949f, 460f, 96f), T("arena.return"), 34,
                canReturn ? ArenaIvory : ArenaMutedInk, TextAnchor.MiddleCenter, true);

            GUI.enabled = canReturn;
            if (GUI.Button(returnArea, GUIContent.none, GUIStyle.none))
            {
                // Commit the return only once, retain the existing ranking scroll, and
                // release the frozen battle snapshot after switching back to the board.
                arenaResultReturnPending = true;
                PlayUiClick(UiAudioCueIds.Cancel, "arena-result-return");
                arenaPageState = ArenaPageState.Ranking;
                selectedArenaOpponentIndex = -1;
                battlePresentationActive = false;
                battleControlsEnabled = false;
                battlePaused = false;
                battlePresentation.Reset();
                battleFloatingNumbers.Clear();
                battleFeedbackVisuals.Clear();
                c05BarrierOwnerByColumn.Clear();
                persistentMilaHiddenSpringSeeds.Clear();
                battleHealthLagPercents.Clear();
                consumedBattleFeedbackKeys.Clear();
                arenaResultCommitted = false;
                arenaResultReturnPending = false;
            }
            GUI.enabled = true;
        }

        private void DrawArenaBattleResultAModule(string resourceId, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetArenaBattleResultATexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Arena Result A asset: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private string FormatArenaRatingDelta()
        {
            return arenaRatingDelta > 0 ? "+" + arenaRatingDelta : arenaRatingDelta.ToString();
        }

        private Color ArenaResultDeltaColor()
        {
            if (arenaRatingDelta > 0) return new Color32(27, 102, 88, 255);
            if (arenaRatingDelta < 0) return ArenaCopper;
            return new Color32(52, 78, 82, 255);
        }

        private void RefreshArenaRuntimeState()
        {
            var today = ArenaPrototypeRules.DayKey(DateTime.Now);
            if (!string.Equals(arenaChallengeDayKey, today, StringComparison.Ordinal))
            {
                arenaChallengesRemaining = ArenaPrototypeRules.DailyChallengeLimit;
                arenaChallengeDayKey = today;
            }
            if (arenaOpponents == null || arenaOpponents.Length == 0)
                arenaOpponents = ArenaPrototypeRules.CreateDeterministicBots(arenaRating);
        }

        private void GrantArenaChallengeTestResetIfNeeded()
        {
            if (arenaChallengeTestResetChecked) return;
            arenaChallengeTestResetChecked = true;
            if (PlayerPrefs.GetInt(ArenaChallengeTestResetPreferenceKey, 0) != 0) return;

            arenaChallengesRemaining = ArenaPrototypeRules.DailyChallengeLimit;
            arenaChallengeDayKey = ArenaPrototypeRules.DayKey(DateTime.Now);
            SaveGame();
            PlayerPrefs.SetInt(ArenaChallengeTestResetPreferenceKey, 1);
            PlayerPrefs.Save();
        }

        private string FormatArenaCountdown(TimeSpan remaining)
        {
            return string.Format(T("arena.countdownFormat"),
                Mathf.Max(0, remaining.Days), remaining.Hours, remaining.Minutes, remaining.Seconds);
        }

        private void SelectArenaOpponent(int index)
        {
            if (index < 0 || index >= arenaOpponents.Length) return;
            PlayUiClick(UiAudioCueIds.Tap, "arena-opponent-" + index);
            selectedArenaOpponentIndex = index;
            arenaPageState = ArenaPageState.OpponentDetail;
        }

        private void ConfirmArenaChallenge()
        {
            if (selectedArenaOpponentIndex < 0 || selectedArenaOpponentIndex >= arenaOpponents.Length ||
                !ArenaPrototypeRules.TryConsumeChallenge(ref arenaChallengesRemaining))
            {
                PlayUiClick(UiAudioCueIds.Disabled, "arena-challenge-unavailable");
                return;
            }
            PlayUiClick(UiAudioCueIds.Confirm, "arena-challenge-confirm");
            FillArenaAttackSlotsFromActiveDeck();
            selectedArenaAttackSlot = -1;
            arenaChallengeDayKey = ArenaPrototypeRules.DayKey(DateTime.Now);
            arenaPageState = ArenaPageState.Preparation;
            SaveGame();
        }

        private void FillArenaAttackSlotsFromActiveDeck()
        {
            Array.Clear(arenaAttackSlots, 0, arenaAttackSlots.Length);
            Array.Clear(arenaAttackBenchSlots, 0, arenaAttackBenchSlots.Length);
            for (var index = 0; index < nextRunDeckSelection.SelectedInstanceIds.Count; index++)
            {
                var unit = CreateOwnedArenaUnit(playerCollection.GetByInstanceId(nextRunDeckSelection.SelectedInstanceIds[index]));
                if (index < arenaAttackSlots.Length) arenaAttackSlots[index] = unit;
                else if (index - arenaAttackSlots.Length < arenaAttackBenchSlots.Length)
                    arenaAttackBenchSlots[index - arenaAttackSlots.Length] = unit;
            }
        }

        private void RefreshDefenseFormationBench()
        {
            Array.Clear(defenseBenchSlots, 0, defenseBenchSlots.Length);
            var benchIndex = 0;
            for (var index = 0; index < nextRunDeckSelection.SelectedInstanceIds.Count && benchIndex < defenseBenchSlots.Length; index++)
            {
                var owned = playerCollection.GetByInstanceId(nextRunDeckSelection.SelectedInstanceIds[index]);
                if (FindDefenseInstance(owned.CharacterInstanceId) >= 0) continue;
                defenseBenchSlots[benchIndex++] = CreateOwnedArenaUnit(owned);
            }
        }

        private int ReconcileDefenseWithActiveDeck()
        {
            if (nextRunDeckSelection == null) return 0;
            var activeInstanceIds = new HashSet<string>(
                nextRunDeckSelection.SelectedInstanceIds, StringComparer.Ordinal);
            var removed = 0;
            for (var slot = 0; slot < defenseSlots.Length; slot++)
            {
                var unit = defenseSlots[slot];
                if (unit == null || activeInstanceIds.Contains(unit.CharacterInstanceId)) continue;
                defenseSlots[slot] = null;
                removed++;
            }
            return removed;
        }

        private void HandleSharedFormationPointer(UnitData[] boardSlots, UnitData[] benchSlots)
        {
            if (arenaFormationDetailUnit != null)
            {
                ResetSharedFormationPointer();
                return;
            }
            var currentEvent = Event.current;
            if (currentEvent == null) return;
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
            {
                var slot = SharedFormationSlotAt(currentEvent.mousePosition, boardSlots.Length, benchSlots.Length);
                if (slot < 0 || SharedFormationUnitAt(boardSlots, benchSlots, slot) == null) return;
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
                if (!preparationPointerDragging && (preparationPointerPosition - preparationPointerDown).sqrMagnitude >= 100f)
                {
                    preparationPointerDragging = true;
                    PlayUiClick(UiAudioCueIds.DragPick, "arena-formation-drag-pick");
                }
                currentEvent.Use();
                return;
            }
            if (currentEvent.type != EventType.MouseUp || !preparationPointerHeld) return;
            preparationPointerPosition = currentEvent.mousePosition;
            var destination = SharedFormationSlotAt(preparationPointerPosition, boardSlots.Length, benchSlots.Length);
            if (!preparationPointerDragging)
            {
                arenaFormationDetailUnit = SharedFormationUnitAt(
                    boardSlots, benchSlots, preparationPointerSlot);
                arenaFormationDetailUsesPlayerLantern = arenaFormationDetailUnit != null;
                PlayUiClick(UiAudioCueIds.Tap, "arena-formation-unit-detail");
            }
            else if (destination >= 0 && destination != preparationPointerSlot)
            {
                SwapSharedFormationUnits(boardSlots, benchSlots, preparationPointerSlot, destination);
                PlayUiClick(UiAudioCueIds.DragDrop, "arena-formation-drag-drop");
            }
            ResetSharedFormationPointer();
            currentEvent.Use();
        }

        private int SharedFormationSlotAt(Vector2 position, int boardCount, int benchCount)
        {
            for (var index = 0; index < boardCount; index++)
                if (PreparationBoardSlotRect(index).Contains(position)) return index;
            for (var index = 0; index < benchCount; index++)
                if (PreparationBenchSlotRect(index).Contains(position)) return boardCount + index;
            return -1;
        }

        private static UnitData SharedFormationUnitAt(UnitData[] boardSlots, UnitData[] benchSlots, int slot)
        {
            return slot < boardSlots.Length ? boardSlots[slot] : benchSlots[slot - boardSlots.Length];
        }

        private static void SwapSharedFormationUnits(UnitData[] boardSlots, UnitData[] benchSlots, int source, int destination)
        {
            var sourceUnit = SharedFormationUnitAt(boardSlots, benchSlots, source);
            var destinationUnit = SharedFormationUnitAt(boardSlots, benchSlots, destination);
            if (source < boardSlots.Length) boardSlots[source] = destinationUnit;
            else benchSlots[source - boardSlots.Length] = destinationUnit;
            if (destination < boardSlots.Length) boardSlots[destination] = sourceUnit;
            else benchSlots[destination - boardSlots.Length] = sourceUnit;
        }

        private static int CountUnits(UnitData[] slots)
        {
            var count = 0;
            for (var index = 0; index < slots.Length; index++) if (slots[index] != null) count++;
            return count;
        }

        private void DrawSharedFormationDragGhost(UnitData[] boardSlots, UnitData[] benchSlots)
        {
            if (!preparationPointerHeld || !preparationPointerDragging || preparationPointerSlot < 0) return;
            var unit = SharedFormationUnitAt(boardSlots, benchSlots, preparationPointerSlot);
            if (unit == null) return;
            var area = new Rect(preparationPointerPosition.x - 78f, preparationPointerPosition.y - 105f, 156f, 210f);
            DrawCharacterAvatar(unit.NameKey, unit.Quality, AvatarArtContext.DragGhost, area);
        }

        private void DrawArenaFormationUnitDetail()
        {
            var sourceUnit = arenaFormationDetailUnit;
            if (sourceUnit == null) return;
            var unit = arenaFormationDetailUsesPlayerLantern
                ? CreateLanternAdjustedCopy(sourceUnit, CurrentLanternSnapshot())
                : sourceUnit;
            var fullCanvas = new Rect(0f, 0f, DesignWidth, layoutCanvasHeight);
            GUI.color = ArchiveColor("041623D8");
            GUI.DrawTexture(fullCanvas, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var modalData = DetailTideWindowLayout.CalculateModal(
                DesignWidth, layoutCanvasHeight, layoutSafeTop, layoutSafeBottom);
            var panel = new Rect(modalData.X, modalData.Y, modalData.Width, modalData.Height);
            ClosePreparationOverlayWhenTappedOutside(panel, CloseArenaFormationUnitDetail);
            var shell = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailTideWindowShell);
            if (shell != null) GUI.DrawTexture(panel, shell, ScaleMode.StretchToFill, true);

            var ivory = ArchiveColor("F6E8C8");
            GUI.Label(DetailLocalRect(panel, 190f, 70f, 500f, 72f),
                CharacterDisplayName(unit.NameKey, unit.Quality),
                DetailTextStyle(46, ivory, TextAnchor.MiddleCenter, false, FontStyle.Bold));
            CharacterDefinition definition = null;
            try { definition = characterCatalog.Get(unit.NameKey); }
            catch (KeyNotFoundException) { }
            DrawDetailTags(panel, unit.NameKey, definition, false, ivory);

            var qualityId = CharacterQualityResourceId(unit.Quality);
            var badge = artCatalog.GetUnitLevelBadgeTexture(qualityId, unit.RunLevel);
            if (badge != null)
                GUI.DrawTexture(DetailLocalRect(panel, 290f, 230f, 300f, 88f), badge,
                    ScaleMode.ScaleToFit, true);
            DrawDetailMetadataField(panel, 202f, T(QualityKey(unit.Quality)), ivory);
            DrawDetailMetadataField(panel, 448f,
                unit.ExternalStar == 0 ? T("collection.notStarred") :
                string.Format(T("collection.star"), unit.ExternalStar), ivory);

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(unit.NameKey, qualityId, out artDefinition, out sprite))
                DrawDetailCharacterSprite(DetailLocalRect(panel, 45f, 390f, 500f, 580f),
                    artDefinition, sprite, 0.90f);
            DrawDetailStat(panel, new Rect(574f, 393f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatHealth, T("stat.health"), unit.MaxHealth.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 549f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatAttack, T("stat.attack"), unit.AttackDamage.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 705f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatArmor, T("stat.armor"), unit.Armor.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 861f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatSpeed, T("detail.attackSpeed"),
                DetailTideWindowLayout.FormatAttackSpeed(unit.AttackSpeedBasisPoints), ivory);
            GUI.Label(DetailLocalRect(panel, 90f, 1078f, 700f, 132f),
                DetailQuote(unit.NameKey, qualityId),
                DetailTextStyle(29, ivory, TextAnchor.MiddleCenter, true, FontStyle.Normal));

            var closeTop = DetailLocalRect(panel, 788f, 55f, 48f, 48f);
            var closeTexture = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailCloseOrnament);
            if (closeTexture != null) GUI.DrawTexture(closeTop, closeTexture, ScaleMode.ScaleToFit, true);
            var closeBottom = DetailLocalRect(panel, 307f, 1261f, 268f, 74f);
            GUI.Label(closeBottom, T("close"), DetailTextStyle(29, ivory,
                TextAnchor.MiddleCenter, false, FontStyle.Normal));
            if (GUI.Button(closeTop, GUIContent.none, GUIStyle.none) ||
                GUI.Button(closeBottom, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "arena-formation-unit-detail-close");
                CloseArenaFormationUnitDetail();
            }
        }

        private void CloseArenaFormationUnitDetail()
        {
            arenaFormationDetailUnit = null;
            arenaFormationDetailUsesPlayerLantern = false;
        }

        private void ResetSharedFormationPointer()
        {
            ResetPreparationPointer();
        }

        private UnitData CreateOwnedArenaUnit(OwnedCharacterState owned)
        {
            var definition = characterCatalog.Get(owned.DefinitionId);
            var aptitudeRoll = OwnedAptitudeRoll(owned);
            var rarityStats = CharacterRarityInstanceRules.CalculateCombatStats(definition, definition.BasePower, 1,
                owned.Quality, aptitudeRoll, owned.ExternalStar);
            var stats = EquipmentDerivedStatCalculator.Calculate(rarityStats,
                equipmentInventory.CreateSnapshot(owned.CharacterInstanceId));
            var unit = UnitData.Create(definition, definition.BasePower, PlayerColor(owned.DefinitionId), 1, stats);
            unit.CharacterInstanceId = owned.CharacterInstanceId;
            unit.Quality = owned.Quality;
            unit.AptitudeRoll = aptitudeRoll;
            unit.ExternalStar = owned.ExternalStar;
            return unit;
        }

        private static int OwnedAptitudeRoll(OwnedCharacterState owned)
        {
            if (owned == null) throw new ArgumentNullException(nameof(owned));
            return owned.AptitudeRoll >= 0
                ? owned.AptitudeRoll
                : CharacterRarityInstanceRules.ComputeAptitudeRoll(owned.CharacterInstanceId);
        }

        private UnitData CreateArenaProfileUnit(ArenaUnitProfile profile)
        {
            var definition = characterCatalog.Get(profile.DefinitionId);
            var stats = CharacterRarityInstanceRules.CalculateCombatStats(definition, profile.BasePower, 1,
                profile.Quality, profile.AptitudeRoll, profile.ExternalStar);
            var unit = UnitData.Create(definition, profile.BasePower, EnemyColor(profile.DefinitionId), 1, stats);
            unit.CharacterInstanceId = profile.CharacterInstanceId;
            unit.Quality = profile.Quality;
            unit.AptitudeRoll = profile.AptitudeRoll;
            unit.ExternalStar = profile.ExternalStar;
            return unit;
        }

        private void DrawArenaRosterButton(Rect area, OwnedCharacterState owned, bool defense)
        {
            var deployed = defense ? FindDefenseInstance(owned.CharacterInstanceId) >= 0 : FindArenaAttackInstance(owned.CharacterInstanceId) >= 0;
            uiSkin.DrawListRow(area, deployed);
            // The main-art avatar contract reserves arena roster identity cells for the
            // player-avatar system. Character portrait art must not be substituted here.
            GUI.Label(new Rect(area.x + 120f, area.y + 16f, 165f, 34f),
                CharacterDisplayName(owned.DefinitionId, owned.Quality),
                Style(17, Color.white, TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 120f, area.y + 52f, 165f, 30f), T(QualityKey(owned.Quality)), Style(14, QualityColor(owned.Quality), TextAnchor.MiddleLeft));
            GUI.Label(new Rect(area.x + 120f, area.y + 86f, 165f, 28f), deployed ? T("deck.inActive") : T("defense.deploy"), Style(13, Color.white, TextAnchor.MiddleLeft));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none) && !deployed)
            {
                PlayUiClick(UiAudioCueIds.Tap, defense ? "defense-roster-deploy" : "arena-roster-deploy");
                if (defense) AddDefenseOwnedUnit(owned);
                else AddArenaAttackOwnedUnit(owned);
            }
        }

        private int FindArenaAttackInstance(string instanceId)
        {
            for (var index = 0; index < arenaAttackSlots.Length; index++)
                if (arenaAttackSlots[index] != null && arenaAttackSlots[index].CharacterInstanceId == instanceId) return index;
            return -1;
        }

        private int FindDefenseInstance(string instanceId)
        {
            for (var index = 0; index < defenseSlots.Length; index++)
                if (defenseSlots[index] != null && defenseSlots[index].CharacterInstanceId == instanceId) return index;
            return -1;
        }

        private void AddArenaAttackOwnedUnit(OwnedCharacterState owned)
        {
            for (var slot = 0; slot < arenaAttackSlots.Length; slot++)
            {
                if (arenaAttackSlots[slot] != null) continue;
                arenaAttackSlots[slot] = CreateOwnedArenaUnit(owned);
                return;
            }
        }

        private void SelectOrMoveArenaAttackUnit(int destination)
        {
            PlayUiClick(UiAudioCueIds.Tap, "arena-formation-slot");
            if (selectedArenaAttackSlot < 0)
            {
                if (arenaAttackSlots[destination] != null) selectedArenaAttackSlot = destination;
                return;
            }
            if (selectedArenaAttackSlot == destination) { selectedArenaAttackSlot = -1; return; }
            var unit = arenaAttackSlots[selectedArenaAttackSlot];
            arenaAttackSlots[selectedArenaAttackSlot] = arenaAttackSlots[destination];
            arenaAttackSlots[destination] = unit;
            selectedArenaAttackSlot = -1;
        }

        private int ArenaAttackUnitCount()
        {
            var count = 0;
            for (var index = 0; index < arenaAttackSlots.Length; index++) if (arenaAttackSlots[index] != null) count++;
            return count;
        }

        private int ArenaAttackPower()
        {
            var power = 0;
            for (var index = 0; index < arenaAttackSlots.Length; index++) if (arenaAttackSlots[index] != null) power += Mathf.Max(0, arenaAttackSlots[index].Power);
            return power * ArenaPrototypeRules.ExistingPowerMultiplier;
        }

        private void DrawWarehouseScreen()
        {
            DrawWarehouseTexture(new Rect(0f, 0f, 1080f, 2400f),
                WarehouseAFormalArtContract.Background, "modules");
            GUI.Label(new Rect(48f, 54f, 420f, 92f), T("warehouse.title"),
                Style(54, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleLeft, FontStyle.Normal));

            var returnArea = new Rect(800f, 52f, 192f, 72f);
            DrawWarehouseButtonTexture(returnArea, WarehouseAFormalArtContract.ReturnNormal,
                WarehouseAFormalArtContract.ReturnPressed);
            GUI.Label(new Rect(842f, 52f, 140f, 72f), T("home.back"),
                Style(28, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
            if (GUI.Button(returnArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "warehouse-return-home");
                warehouseEquipmentScroll = Vector2.zero;
                currentView = ScreenView.Home;
                return;
            }

            var tabs = new[]
            {
                WarehouseTab.All, WarehouseTab.Equipment, WarehouseTab.Lantern,
                WarehouseTab.Materials, WarehouseTab.Other
            };
            var tabKeys = new[]
            {
                "warehouse.all", "warehouse.equipment", "warehouse.lantern",
                "warehouse.materials", "warehouse.other"
            };
            for (var i = 0; i < tabs.Length; i++)
            {
                var selected = warehouseTab == tabs[i];
                var tabArea = new Rect(48f + i * 200f, 220f, 184f, 88f);
                DrawWarehouseTexture(tabArea, selected
                    ? WarehouseAFormalArtContract.TabSelected
                    : WarehouseAFormalArtContract.TabNormal, "modules");
                GUI.Label(tabArea, T(tabKeys[i]), Style(30,
                    new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
                if (!GUI.Button(tabArea, GUIContent.none, GUIStyle.none))
                    continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "warehouse-tab-" + tabs[i]);
                warehouseTab = tabs[i];
                warehouseEquipmentScroll = Vector2.zero;
                ResetWarehouseItemPointer();
            }

            ReconcileWarehouseLayout();
            var usedSlots = WarehouseStorageRules.CountUsedSlots(
                equipmentInventory.Instances.Count, lanternInventory.Instances.Count,
                OwnedStoreCosmeticCount());
            GUI.Label(new Rect(48f, 356f, 520f, 72f),
                string.Format(T("warehouse.capacity"), usedSlots, WarehouseStorageRules.InitialCapacity),
                Style(28, new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleLeft, FontStyle.Normal));

            var organizeArea = new Rect(704f, 356f, 160f, 72f);
            var sortArea = new Rect(880f, 356f, 160f, 72f);
            DrawWarehouseButtonTexture(organizeArea, WarehouseAFormalArtContract.ToolNormal,
                WarehouseAFormalArtContract.ToolPressed);
            DrawWarehouseButtonTexture(sortArea, WarehouseAFormalArtContract.ToolNormal,
                WarehouseAFormalArtContract.ToolPressed);
            GUI.Label(organizeArea, T("warehouse.organize"), Style(27,
                new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
            GUI.Label(sortArea, T("warehouse.sort") + "·" + T(WarehouseSortLocalizationKey()), Style(21,
                new Color(0.95f, 0.89f, 0.72f), TextAnchor.MiddleCenter));
            if (GUI.Button(organizeArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "warehouse-organize");
                if (WarehouseLayoutRules.Organize(warehouseSlotTokens)) SaveGame();
            }
            if (GUI.Button(sortArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "warehouse-sort");
                warehouseSortMode = (WarehouseSortMode)(((int)warehouseSortMode + 1) % 3);
                WarehouseLayoutRules.Sort(warehouseSlotTokens,
                    CreateWarehouseSortEntries(), warehouseSortMode, WarehouseTokenVisible);
                SaveGame();
            }

            var viewport = new Rect(40f, WarehouseAFormalArtContract.GridY, 992f,
                WarehouseAFormalArtContract.ScrollViewportHeight);
            HandleWarehouseItemPointer(viewport);
            HandleWarehouseScrollPointer(viewport, WarehouseAFormalArtContract.ScrollContentHeight);
            warehouseEquipmentScroll = GUI.BeginScrollView(
                viewport, warehouseEquipmentScroll,
                new Rect(0f, 0f, 992f, WarehouseAFormalArtContract.ScrollContentHeight),
                false, false, GUIStyle.none, GUIStyle.none);
            var visibleStorageSlots = WarehouseLayoutRules.VisibleSlotIndices(
                warehouseSlotTokens, warehouseTab == WarehouseTab.All ? null : WarehouseTokenVisible);
            for (var i = 0; i < WarehouseAFormalArtContract.SlotCount; i++)
            {
                var area = new Rect(8f + i % WarehouseAFormalArtContract.ColumnCount *
                    WarehouseAFormalArtContract.GridStep,
                    i / WarehouseAFormalArtContract.ColumnCount * WarehouseAFormalArtContract.GridStep,
                    WarehouseAFormalArtContract.SlotSize, WarehouseAFormalArtContract.SlotSize);
                var storageSlot = warehouseTab == WarehouseTab.All
                    ? i
                    : i < visibleStorageSlots.Count ? visibleStorageSlots[i] : -1;
                var token = storageSlot >= 0 ? warehouseSlotTokens[storageSlot] : null;
                if (string.IsNullOrEmpty(token) ||
                    (warehouseItemPointerDragging && i == warehouseDragSourceSlot))
                    DrawWarehouseTexture(area, WarehouseAFormalArtContract.EmptySlot, "modules");
                else if (!DrawWarehouseSlotToken(area, token))
                    DrawWarehouseTexture(area, WarehouseAFormalArtContract.EmptySlot, "modules");
            }
            GUI.EndScrollView();
            DrawWarehouseTexture(new Rect(1034f, WarehouseAFormalArtContract.GridY, 32f,
                WarehouseAFormalArtContract.ScrollViewportHeight),
                WarehouseAFormalArtContract.Scrollbar, "modules");
            DrawWarehouseDragGhost();
        }

        private static string WarehouseEquipmentToken(string instanceId)
        {
            return "equipment:" + instanceId;
        }

        private static string WarehouseLanternToken(string instanceId)
        {
            return "lantern:" + instanceId;
        }

        private static string WarehouseCosmeticToken(string productId)
        {
            return "cosmetic:" + productId;
        }

        private int OwnedStoreCosmeticCount()
        {
            var count = 0;
            for (var index = 0; index < ownedBasePackIds.Count; index++)
            {
                StoreProductDefinition product;
                if (StoreCatalog.TryGet(ownedBasePackIds[index], out product) &&
                    product.ProductType != StoreProductType.CharacterPack) count++;
            }
            return count;
        }

        private void ReconcileWarehouseLayout()
        {
            var current = new List<string>();
            foreach (var instance in equipmentInventory.Instances)
                current.Add(WarehouseEquipmentToken(instance.EquipmentInstanceId));
            foreach (var instance in lanternInventory.Instances)
                current.Add(WarehouseLanternToken(instance.LanternInstanceId));
            if (firstLightseekerBadgeOwned) current.Add(FirstLightseekerBadgeToken());
            for (var index = 0; index < ownedBasePackIds.Count; index++)
            {
                StoreProductDefinition product;
                if (StoreCatalog.TryGet(ownedBasePackIds[index], out product) &&
                    product.ProductType != StoreProductType.CharacterPack)
                    current.Add(WarehouseCosmeticToken(product.ProductId));
            }

            var currentSet = new HashSet<string>(current, StringComparer.Ordinal);
            for (var index = warehouseAcquisitionOrder.Count - 1; index >= 0; index--)
                if (!currentSet.Contains(warehouseAcquisitionOrder[index]))
                    warehouseAcquisitionOrder.RemoveAt(index);
            if (warehouseAcquisitionOrder.Count == 0)
                warehouseAcquisitionOrder.AddRange(current);
            else
            {
                for (var index = current.Count - 1; index >= 0; index--)
                    if (!warehouseAcquisitionOrder.Contains(current[index]))
                        warehouseAcquisitionOrder.Insert(0, current[index]);
            }
            WarehouseLayoutRules.Reconcile(warehouseSlotTokens, current);
        }

        private string WarehouseSortLocalizationKey()
        {
            switch (warehouseSortMode)
            {
                case WarehouseSortMode.Type: return "warehouse.sortType";
                case WarehouseSortMode.Name: return "warehouse.sortName";
                default: return "warehouse.sortTime";
            }
        }

        private List<WarehouseSortEntry> CreateWarehouseSortEntries()
        {
            var entries = new List<WarehouseSortEntry>();
            foreach (var instance in equipmentInventory.Instances)
            {
                var token = WarehouseEquipmentToken(instance.EquipmentInstanceId);
                entries.Add(new WarehouseSortEntry(token,
                    WarehouseAcquisitionIndex(token), 0,
                    (int)equipmentCatalog.Get(instance.TemplateId).Slot, T(instance.TemplateId)));
            }
            foreach (var instance in lanternInventory.Instances)
            {
                var token = WarehouseLanternToken(instance.LanternInstanceId);
                entries.Add(new WarehouseSortEntry(token,
                    WarehouseAcquisitionIndex(token), 1, 0, T(instance.TemplateId)));
            }
            if (firstLightseekerBadgeOwned)
            {
                var token = FirstLightseekerBadgeToken();
                entries.Add(new WarehouseSortEntry(token,
                    WarehouseAcquisitionIndex(token), 2, 0, "First Lightseeker"));
            }
            for (var index = 0; index < ownedBasePackIds.Count; index++)
            {
                StoreProductDefinition product;
                if (!StoreCatalog.TryGet(ownedBasePackIds[index], out product) ||
                    product.ProductType == StoreProductType.CharacterPack) continue;
                var token = WarehouseCosmeticToken(product.ProductId);
                entries.Add(new WarehouseSortEntry(token, WarehouseAcquisitionIndex(token), 2,
                    (int)product.ProductType, T(product.DisplayNameKey)));
            }
            return entries;
        }

        private int WarehouseAcquisitionIndex(string token)
        {
            var index = warehouseAcquisitionOrder.IndexOf(token);
            return index < 0 ? int.MaxValue : index;
        }

        private bool WarehouseTokenVisible(string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (warehouseTab == WarehouseTab.All) return true;
            if (warehouseTab == WarehouseTab.Equipment)
                return token.StartsWith("equipment:", StringComparison.Ordinal);
            if (warehouseTab == WarehouseTab.Lantern)
                return token.StartsWith("lantern:", StringComparison.Ordinal);
            if (warehouseTab == WarehouseTab.Other)
                return token.StartsWith("cosmetic:", StringComparison.Ordinal) ||
                       token.StartsWith("badge:", StringComparison.Ordinal);
            if (warehouseTab == WarehouseTab.Materials)
                return token.StartsWith("material:", StringComparison.Ordinal);
            return false;
        }

        private string WarehouseDisplayToken(int displaySlot)
        {
            if (displaySlot < 0 || displaySlot >= WarehouseAFormalArtContract.SlotCount) return null;
            if (warehouseTab == WarehouseTab.Materials)
            {
                var materials = WarehouseMaterialTokens();
                return displaySlot < materials.Count ? materials[displaySlot] : null;
            }
            if (warehouseTab == WarehouseTab.All) return warehouseSlotTokens[displaySlot];
            var visible = WarehouseLayoutRules.VisibleSlotIndices(warehouseSlotTokens, WarehouseTokenVisible);
            return displaySlot < visible.Count ? warehouseSlotTokens[visible[displaySlot]] : null;
        }

        private bool DrawWarehouseSlotToken(Rect area, string token)
        {
            if (string.IsNullOrEmpty(token)) return false;
            if (token.StartsWith("equipment:", StringComparison.Ordinal))
            {
                var id = token.Substring("equipment:".Length);
                if (!equipmentInventory.Contains(id)) return false;
                DrawWarehouseEquipmentSlot(area, equipmentInventory.Get(id));
                return true;
            }
            if (token.StartsWith("lantern:", StringComparison.Ordinal))
            {
                var id = token.Substring("lantern:".Length);
                if (!lanternInventory.Contains(id)) return false;
                DrawWarehouseLanternSlot(area, lanternInventory.Get(id));
                return true;
            }
            if (token.StartsWith("cosmetic:", StringComparison.Ordinal))
            {
                var productId = token.Substring("cosmetic:".Length);
                StoreProductDefinition product;
                if (!ownedBasePackIds.Contains(productId) || !StoreCatalog.TryGet(productId, out product) ||
                    product.ProductType == StoreProductType.CharacterPack) return false;
                DrawWarehouseCosmeticSlot(area, product);
                return true;
            }
            if (token.StartsWith("material:", StringComparison.Ordinal))
            {
                DrawWarehouseMaterialSlot(area, token.Substring("material:".Length));
                return true;
            }
            if (string.Equals(token, FirstLightseekerBadgeToken(), StringComparison.Ordinal))
            {
                DrawFirstLightseekerWarehouseSlot(area);
                return firstLightseekerBadgeOwned;
            }
            return false;
        }

        private List<string> WarehouseMaterialTokens()
        {
            var tokens = new List<string>(6);
            if (tideCrystalBalance > 0) tokens.Add("material:tide-crystal");
            if (localLampGlowBalance > 0 ||
                (lanternWorkshopSnapshot != null && lanternWorkshopSnapshot.LampGlowBalance > 0))
                tokens.Add("material:lamp-glow");
            if (localTideIronBalance > 0) tokens.Add("material:tide-iron");
            if (localMistLeatherBalance > 0) tokens.Add("material:mist-leather");
            if (localStarSandBalance > 0) tokens.Add("material:star-sand");
            if (localBountyTicketBalance > 0) tokens.Add("material:bounty-ticket");
            return tokens;
        }

        private void DrawWarehouseMaterialSlot(Rect area, string kind)
        {
            var resourceId = ExpeditionSettlementFormalArtContract.WarehouseThumbnail(kind);
            DrawSettlementTexture(area, ExpeditionSettlementFormalArtContract.WarehouseThumbnailRoot,
                resourceId, 184, 184);
            long quantity = 0;
            if (string.Equals(kind, "tide-crystal", StringComparison.Ordinal))
                quantity = tideCrystalBalance;
            else if (string.Equals(kind, "lamp-glow", StringComparison.Ordinal))
                quantity = localLampGlowBalance +
                           (lanternWorkshopSnapshot == null ? 0 : lanternWorkshopSnapshot.LampGlowBalance);
            else if (string.Equals(kind, "tide-iron", StringComparison.Ordinal))
                quantity = localTideIronBalance;
            else if (string.Equals(kind, "mist-leather", StringComparison.Ordinal))
                quantity = localMistLeatherBalance;
            else if (string.Equals(kind, "star-sand", StringComparison.Ordinal))
                quantity = localStarSandBalance;
            else if (string.Equals(kind, "bounty-ticket", StringComparison.Ordinal))
                quantity = localBountyTicketBalance;
            GUI.Label(new Rect(area.x + 8f, area.y + 142f, 168f, 34f), "×" + quantity.ToString("N0"),
                ArchiveStyle(22, new Color32(8, 55, 78, 255), TextAnchor.MiddleRight, true));
        }

        private void DrawWarehouseCosmeticSlot(Rect area, StoreProductDefinition product)
        {
            DrawWarehouseCosmeticVisual(area, product);
            if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) return;
            PlayUiClick(UiAudioCueIds.Tap, "warehouse-cosmetic-detail-open");
            warehouseCosmeticDetailProductId = product.ProductId;
            warehouseCosmeticDetailOpen = true;
        }

        private void DrawWarehouseCosmeticVisual(Rect area, StoreProductDefinition product)
        {
            var thumbnailId = StoreCosmeticFormalArtContract.WarehouseThumbnailResourceId(
                product.ProductId);
            var thumbnail = artCatalog.GetStoreCosmeticWarehouseThumbnail(thumbnailId);
            if (thumbnail == null ||
                thumbnail.width != StoreCosmeticFormalArtContract.WarehouseThumbnailWidth ||
                thumbnail.height != StoreCosmeticFormalArtContract.WarehouseThumbnailHeight)
            {
                DrawWarehouseTexture(area, WarehouseAFormalArtContract.EmptySlot, "modules");
                Debug.LogError("Rejected missing or resized cosmetic warehouse thumbnail: " +
                               thumbnailId);
                return;
            }
            var thumbnailArea = new Rect(area.x, area.y,
                StoreCosmeticFormalArtContract.WarehouseThumbnailWidth,
                StoreCosmeticFormalArtContract.WarehouseThumbnailHeight);
            GUI.DrawTexture(thumbnailArea, thumbnail, ScaleMode.StretchToFill, true);
            var equipped = IsStoreCosmeticEquipped(product);
            if (equipped)
                GUI.Label(new Rect(area.x + 10f, area.y + 145f, 164f, 28f),
                    T("warehouse.cosmeticEquipped"),
                    Style(15, MarketGold, TextAnchor.MiddleCenter, FontStyle.Bold));
        }

        private bool IsStoreCosmeticEquipped(StoreProductDefinition product)
        {
            if (product == null) return false;
            return string.Equals(product.ProductId,
                product.ProductType == StoreProductType.BasePack
                    ? equippedBasePackProductId : equippedBattlefieldProductId,
                StringComparison.Ordinal);
        }

        private void DrawWarehouseCosmeticDetailScreen()
        {
            StoreProductDefinition product;
            if (!ownedBasePackIds.Contains(warehouseCosmeticDetailProductId) ||
                !StoreCatalog.TryGet(warehouseCosmeticDetailProductId, out product) ||
                product.ProductType == StoreProductType.CharacterPack)
            {
                warehouseCosmeticDetailOpen = false;
                warehouseCosmeticDetailProductId = null;
                return;
            }

            var panel = EquipmentDetailAFormalArtContract.ActionPanelRect;
            DrawEquipmentDetailAModule(panel, EquipmentDetailAFormalArtContract.ActionShell, 760, 650);
            GUI.Label(OffsetEquipmentDetailRect(panel, 130f, 18f, 500f, 70f),
                T("market.detail"), Style(31, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));

            var appearance = OffsetEquipmentDetailRect(panel, 48f, 145f, 184f, 184f);
            var thumbnailId = StoreCosmeticFormalArtContract.WarehouseThumbnailResourceId(
                product.ProductId);
            var thumbnail = artCatalog.GetStoreCosmeticWarehouseThumbnail(thumbnailId);
            if (thumbnail != null &&
                thumbnail.width == StoreCosmeticFormalArtContract.WarehouseThumbnailWidth &&
                thumbnail.height == StoreCosmeticFormalArtContract.WarehouseThumbnailHeight)
                GUI.DrawTexture(appearance, thumbnail, ScaleMode.StretchToFill, true);
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 337f, 184f, 40f),
                T(product.DisplayNameKey), Style(21, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 380f, 184f, 30f),
                product.ProductType == StoreProductType.BasePack
                    ? T("warehouse.cosmeticBase") : T("warehouse.cosmeticBattlefield"),
                Style(18, MarketGold, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 413f, 190f, 48f),
                !StoreCosmeticFormalArtContract.HasNativeRuntimeVisual(product.ProductId)
                    ? T("warehouse.cosmeticArtBlocked")
                    : IsStoreCosmeticEquipped(product)
                        ? T("warehouse.cosmeticEquipped") : T("warehouse.cosmeticAvailable"),
                Style(18, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));

            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 260f, 145f, 4f, 280f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.VerticalDivider);
            DrawGenericCosmeticDetailRow(panel, 138f, 172f, "warehouse.cosmeticDescription",
                product.ProductType == StoreProductType.BasePack
                    ? "warehouse.cosmeticBaseDescription" : "warehouse.cosmeticBattlefieldDescription");
            DrawGenericCosmeticDetailRow(panel, 242f, 276f, "warehouse.cosmeticUsage",
                product.ProductType == StoreProductType.BasePack
                    ? "warehouse.cosmeticBaseUsage" : "warehouse.cosmeticBattlefieldUsage");
            DrawGenericCosmeticDetailRow(panel, 346f, 380f, "warehouse.cosmeticQuantity",
                "warehouse.cosmeticQuantityOne");

            var equipped = IsStoreCosmeticEquipped(product);
            var runtimeVisualReady = StoreCosmeticFormalArtContract.HasNativeRuntimeVisual(product.ProductId);
            var actionArea = OffsetEquipmentDetailRect(panel, 520f, 552f, 220f, 68f);
            var clicked = DrawLanternDetailAStateButton(actionArea,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.MediumNormal,
                GlobalButtonFormalArtContract.MediumPressed,
                GlobalButtonFormalArtContract.MediumDisabled, runtimeVisualReady && !equipped);
            GUI.Label(actionArea, T(!runtimeVisualReady
                    ? "warehouse.cosmeticArtBlocked"
                    : equipped ? "warehouse.cosmeticEquipped" : "warehouse.cosmeticEquip"),
                Style(22, equipped || !runtimeVisualReady ? MarketMuted : MarketIvory,
                    TextAnchor.MiddleCenter, FontStyle.Bold));
            if (clicked && runtimeVisualReady)
            {
                if (product.ProductType == StoreProductType.BasePack)
                    equippedBasePackProductId = product.ProductId;
                else
                    equippedBattlefieldProductId = product.ProductId;
                PlayUiClick(UiAudioCueIds.Confirm, "warehouse-cosmetic-equip");
                SaveGame();
            }

            if (!GUI.Button(OffsetEquipmentDetailRect(panel, 660f, 20f, 76f, 76f),
                    GUIContent.none, GUIStyle.none)) return;
            PlayUiClick(UiAudioCueIds.Cancel, "warehouse-cosmetic-detail-close");
            warehouseCosmeticDetailOpen = false;
            warehouseCosmeticDetailProductId = null;
            firstLightseekerBadgeDetailOpen = false;
        }

        private void DrawGenericCosmeticDetailRow(Rect panel, float titleY, float rowY,
            string titleKey, string valueKey)
        {
            GUI.Label(OffsetEquipmentDetailRect(panel, 292f, titleY, 360f, 30f), T(titleKey),
                Style(22, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            var row = OffsetEquipmentDetailRect(panel, 292f, rowY, 360f, 56f);
            DrawEquipmentDetailAModule(row, EquipmentDetailAFormalArtContract.BaseRow, 360, 56);
            var style = Style(20, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal);
            style.wordWrap = true;
            GUI.Label(new Rect(row.x + 18f, row.y, row.width - 36f, row.height), T(valueKey), style);
        }

        private int WarehouseSlotAt(Rect viewport, Vector2 pointerPosition)
        {
            if (!viewport.Contains(pointerPosition)) return -1;
            var localX = pointerPosition.x - viewport.x - 8f;
            var localY = pointerPosition.y - viewport.y + warehouseEquipmentScroll.y;
            if (localX < 0f || localY < 0f) return -1;
            var column = Mathf.FloorToInt(localX / WarehouseAFormalArtContract.GridStep);
            var row = Mathf.FloorToInt(localY / WarehouseAFormalArtContract.GridStep);
            if (column < 0 || column >= WarehouseAFormalArtContract.ColumnCount || row < 0) return -1;
            if (localX - column * WarehouseAFormalArtContract.GridStep >= WarehouseAFormalArtContract.SlotSize ||
                localY - row * WarehouseAFormalArtContract.GridStep >= WarehouseAFormalArtContract.SlotSize)
                return -1;
            var slot = row * WarehouseAFormalArtContract.ColumnCount + column;
            return slot < WarehouseAFormalArtContract.SlotCount ? slot : -1;
        }

        private void HandleWarehouseItemPointer(Rect viewport)
        {
            var pointerEvent = Event.current;
            if (pointerEvent == null) return;
            var pointerPosition = pointerEvent.mousePosition;
            if (pointerEvent.type == EventType.MouseDown && pointerEvent.button == 0)
            {
                var source = WarehouseSlotAt(viewport, pointerPosition);
                var sourceToken = WarehouseDisplayToken(source);
                if (source < 0 || string.IsNullOrEmpty(sourceToken)) return;
                warehouseDragSourceSlot = source;
                warehouseDraggedToken = sourceToken;
                warehouseItemPointerDown = pointerPosition;
                warehouseItemPointerLast = pointerPosition;
                warehouseItemPointerHeld = true;
                warehouseItemPointerDragging = false;
                return;
            }
            if (pointerEvent.type == EventType.MouseDrag && warehouseItemPointerHeld)
            {
                warehouseItemPointerLast = pointerPosition;
                if (!warehouseItemPointerDragging &&
                    (pointerPosition - warehouseItemPointerDown).sqrMagnitude >= 144f)
                {
                    warehouseItemPointerDragging = true;
                    PlayUiClick(UiAudioCueIds.DragPick, "warehouse-item-drag-pick");
                }
                if (warehouseItemPointerDragging) pointerEvent.Use();
                return;
            }
            if (pointerEvent.type != EventType.MouseUp || !warehouseItemPointerHeld) return;
            warehouseItemPointerLast = pointerPosition;
            if (warehouseItemPointerDragging)
            {
                var destination = WarehouseSlotAt(viewport, pointerPosition);
                var moved = false;
                if (destination >= 0)
                {
                    moved = warehouseTab == WarehouseTab.All
                        ? WarehouseLayoutRules.TryMove(
                            warehouseSlotTokens, warehouseDragSourceSlot, destination)
                        : WarehouseLayoutRules.TryReorderVisible(
                            warehouseSlotTokens, WarehouseTokenVisible,
                            warehouseDragSourceSlot, destination);
                }
                if (moved)
                {
                    PlayUiClick(UiAudioCueIds.DragDrop, "warehouse-item-drag-drop");
                    SaveGame();
                }
                pointerEvent.Use();
            }
            warehouseDragSourceSlot = -1;
            warehouseDraggedToken = null;
            warehouseItemPointerHeld = false;
            warehouseItemPointerDragging = false;
        }

        private void DrawWarehouseDragGhost()
        {
            if (!warehouseItemPointerDragging || string.IsNullOrEmpty(warehouseDraggedToken)) return;
            var area = new Rect(warehouseItemPointerLast.x - WarehouseAFormalArtContract.SlotSize * 0.5f,
                warehouseItemPointerLast.y - WarehouseAFormalArtContract.SlotSize * 0.5f,
                WarehouseAFormalArtContract.SlotSize, WarehouseAFormalArtContract.SlotSize);
            if (warehouseDraggedToken.StartsWith("equipment:", StringComparison.Ordinal))
            {
                var id = warehouseDraggedToken.Substring("equipment:".Length);
                if (equipmentInventory.Contains(id)) DrawWarehouseEquipmentVisual(area, equipmentInventory.Get(id));
            }
            else if (warehouseDraggedToken.StartsWith("lantern:", StringComparison.Ordinal))
            {
                var id = warehouseDraggedToken.Substring("lantern:".Length);
                if (lanternInventory.Contains(id)) DrawWarehouseLanternVisual(area, lanternInventory.Get(id));
            }
            else if (warehouseDraggedToken.StartsWith("cosmetic:", StringComparison.Ordinal))
            {
                var productId = warehouseDraggedToken.Substring("cosmetic:".Length);
                StoreProductDefinition product;
                if (StoreCatalog.TryGet(productId, out product)) DrawWarehouseCosmeticVisual(area, product);
            }
        }

        private void DrawWarehouseLanternSlot(Rect area, LanternInstanceState instance)
        {
            DrawWarehouseLanternVisual(area, instance);
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "warehouse-lantern-detail-open");
                warehouseLanternDetailId = instance.LanternInstanceId;
                warehouseLanternDetailOpen = true;
                lanternDetailCovenantPage = false;
                warehouseLanternStagePopup = -1;
            }
        }

        private void DrawWarehouseLanternVisual(Rect area, LanternInstanceState instance)
        {
            DrawWarehouseTexture(area, WarehouseAFormalArtContract.QualityBackground(
                instance.Quality.ToString()), "quality");
            DrawWarehouseTexture(area, WarehouseAFormalArtContract.LanternAppearance(
                instance.TemplateId), "lantern");
            var isActive = string.Equals(lanternInventory.ActiveLanternInstanceId,
                instance.LanternInstanceId, StringComparison.Ordinal);
            if (isActive)
                GUI.Label(new Rect(area.x + 8f, area.y + 6f, area.width - 16f, 28f),
                    T("warehouse.lanternEquipped"), Style(14, new Color(1f, 0.86f, 0.42f),
                        TextAnchor.MiddleLeft, FontStyle.Bold));
        }

        private string LanternModifierText(LanternInstanceState instance, string separator)
        {
            if (instance == null || instance.ModifierRolls.Count == 0)
                return T("warehouse.lanternNoGlyph");
            var text = string.Empty;
            for (var index = 0; index < instance.ModifierRolls.Count; index++)
            {
                var modifier = instance.ModifierRolls[index];
                if (index > 0) text += separator;
                var percentage = !string.Equals(modifier.ModifierId,
                    FirstChapterLanternFixtures.ArmorModifierId, StringComparison.Ordinal);
                text += string.Format(T(modifier.ModifierId),
                    percentage ? (object)(modifier.Value / 100f) : modifier.Value);
            }
            return text;
        }

        private void EquipWarehouseLantern(LanternInstanceState instance)
        {
            if (instance == null || !lanternInventory.TrySetActive(instance.LanternInstanceId)) return;
            chapterOneLighthouse.RecordEquippedChapterLantern(
                instance, AllowLanternBossClearBypassForTesting);
            MarkLanternSelectionChanged();
            PlayUiClick(UiAudioCueIds.Confirm, "warehouse-equip-lantern");
            SaveGame();
        }

        private void UnequipWarehouseLantern()
        {
            if (string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId)) return;
            lanternInventory.ClearActive();
            chapterOneLighthouse.RecordLanternUnequipped();
            MarkLanternSelectionChanged();
            PlayUiClick(UiAudioCueIds.Confirm, "warehouse-unequip-lantern");
            SaveGame();
        }

        private void MarkLanternSelectionChanged()
        {
            if (CountUnits(defenseSlots) > 0) arenaDefenseNeedsResync = true;
        }

        public bool ApplyLanternMarketListingStarted(string lanternInstanceId)
        {
            if (lanternInventory == null) return false;
            bool clearedActiveSelection;
            if (!lanternInventory.TryBeginMarketListing(
                    lanternInstanceId, out clearedActiveSelection)) return false;
            if (clearedActiveSelection) chapterOneLighthouse.RecordLanternUnequipped();
            ReconcileUnavailableLanternSnapshots();
            SaveGame();
            return true;
        }

        public bool ApplyLanternMarketSaleCompleted(string lanternInstanceId)
        {
            if (lanternInventory == null) return false;
            var clearedActiveSelection = string.Equals(
                lanternInventory.ActiveLanternInstanceId, lanternInstanceId,
                StringComparison.Ordinal);
            if (!lanternInventory.TryCompleteMarketSale(lanternInstanceId)) return false;
            if (clearedActiveSelection) chapterOneLighthouse.RecordLanternUnequipped();
            ReconcileUnavailableLanternSnapshots();
            SaveGame();
            return true;
        }

        public bool ApplyEquipmentMarketListingStarted(string equipmentInstanceId)
        {
            if (equipmentInventory == null) return false;
            string ignoredCharacterInstanceId;
            if (!equipmentInventory.TryBeginMarketListing(
                    equipmentInstanceId, out ignoredCharacterInstanceId)) return false;
            ReconcileUnavailableEquipmentSnapshots(true);
            SaveGame();
            return true;
        }

        public bool ApplyEquipmentMarketSaleCompleted(string equipmentInstanceId)
        {
            if (equipmentInventory == null ||
                !equipmentInventory.TryCompleteMarketSale(equipmentInstanceId)) return false;
            ReconcileUnavailableEquipmentSnapshots(true);
            SaveGame();
            return true;
        }

        private void ReconcileUnavailableLanternSnapshots()
        {
            if (lanternInventory == null) return;
            var noneRulesVersion = LanternLegacyMigrationResult.LegacyNoneRulesVersion;
            expeditionLanternSnapshot = lanternInventory.ResolveOwnedAvailableSnapshot(
                expeditionLanternSnapshot, noneRulesVersion);
            arenaAttackLanternSnapshot = lanternInventory.ResolveOwnedAvailableSnapshot(
                arenaAttackLanternSnapshot, noneRulesVersion);
            var previousDefenseSnapshot = arenaDefenseLanternSnapshot;
            arenaDefenseLanternSnapshot = lanternInventory.ResolveOwnedAvailableSnapshot(
                arenaDefenseLanternSnapshot, noneRulesVersion);
            if (previousDefenseSnapshot != null && !previousDefenseSnapshot.IsNone &&
                arenaDefenseLanternSnapshot.IsNone)
            {
                arenaDefenseSnapshotVersion++;
                arenaDefenseNeedsResync = true;
            }
        }

        private bool ReconcileUnavailableEquipmentSnapshots(bool forceDefenseInvalidation = false)
        {
            if (equipmentInventory == null || runEquipmentSnapshots == null) return false;
            var changed = false;
            var definitionIds = new List<string>(runEquipmentSnapshots.Keys);
            foreach (var definitionId in definitionIds)
            {
                var previous = runEquipmentSnapshots[definitionId];
                var resolved = equipmentInventory.ResolveOwnedAvailableSnapshot(previous);
                if (previous == null ||
                    resolved.EquipmentInstanceIds.Count != previous.EquipmentInstanceIds.Count)
                    changed = true;
                runEquipmentSnapshots[definitionId] = resolved;
            }
            if (!changed && !forceDefenseInvalidation) return false;

            RefreshArenaFormationsAfterEquipmentOwnershipChange();
            arenaDefenseSnapshotVersion++;
            arenaDefenseNeedsResync = true;
            return true;
        }

        private void RefreshArenaFormationsAfterEquipmentOwnershipChange()
        {
            RefreshArenaFormationUnits(defenseSlots);
            RefreshArenaFormationUnits(arenaAttackSlots);
            RefreshArenaFormationUnits(defenseBenchSlots);
            RefreshArenaFormationUnits(arenaAttackBenchSlots);
        }

        private void RefreshArenaFormationUnits(UnitData[] units)
        {
            if (units == null || playerCollection == null) return;
            for (var index = 0; index < units.Length; index++)
            {
                var current = units[index];
                if (current == null || string.IsNullOrEmpty(current.CharacterInstanceId)) continue;
                OwnedCharacterState owned;
                if (!playerCollection.TryGetByInstanceId(current.CharacterInstanceId, out owned)) continue;
                units[index] = CreateOwnedArenaUnit(owned);
            }
        }

        private bool HasMarketLockedEquipment()
        {
            if (equipmentInventory == null) return false;
            foreach (var instance in equipmentInventory.Instances)
                if ((instance.UsageLocks & EquipmentUsageLock.Market) != 0) return true;
            return false;
        }

        private void OpenLanternChangeConfirmation(LanternChangeAction action,
            LanternInstanceState instance)
        {
            if (action == LanternChangeAction.None) return;
            if ((action == LanternChangeAction.Equip || action == LanternChangeAction.Replace) &&
                instance == null) return;
            pendingLanternChangeAction = action;
            pendingLanternChangeInstanceId = instance == null ? null : instance.LanternInstanceId;
            PlayUiClick(UiAudioCueIds.Tap, "warehouse-lantern-confirm-open");
        }

        private void CloseLanternChangeConfirmation()
        {
            pendingLanternChangeAction = LanternChangeAction.None;
            pendingLanternChangeInstanceId = null;
        }

        private void ConfirmLanternChange()
        {
            var action = pendingLanternChangeAction;
            var instance = FindWarehouseLantern(pendingLanternChangeInstanceId);
            CloseLanternChangeConfirmation();
            if (action == LanternChangeAction.Unequip) UnequipWarehouseLantern();
            else if (action == LanternChangeAction.Equip || action == LanternChangeAction.Replace)
                EquipWarehouseLantern(instance);
        }

        private LanternInstanceState FindWarehouseLantern(string instanceId)
        {
            if (string.IsNullOrEmpty(instanceId)) return null;
            foreach (var candidate in lanternInventory.Instances)
                if (string.Equals(candidate.LanternInstanceId, instanceId, StringComparison.Ordinal))
                    return candidate;
            return null;
        }

        private void DrawWarehouseLanternDetailScreen()
        {
            var instance = FindWarehouseLantern(warehouseLanternDetailId);
            if (instance == null)
            {
                warehouseLanternDetailOpen = false;
                warehouseLanternDetailId = null;
                return;
            }

            var panel = DrawWarehouseLanternDetailAFormal(instance);

            var isActive = string.Equals(lanternInventory.ActiveLanternInstanceId,
                instance.LanternInstanceId, StringComparison.Ordinal);
            var hasActiveLantern = !string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId);
            var actionArea = OffsetEquipmentDetailRect(panel, 240f, 888f, 280f, 76f);
            var actionEnabled = pendingLanternChangeAction == LanternChangeAction.None &&
                warehouseLanternStagePopup < 0;
            var actionLabelKey = isActive ? "warehouse.lanternUnequip" :
                hasActiveLantern ? "warehouse.lanternChange" : "warehouse.lanternEquipAction";
            var actionClicked = DrawLanternDetailAStateButton(actionArea,
                GlobalButtonFormalArtContract.ResourceRoot,
                GlobalButtonFormalArtContract.LargeNormal,
                GlobalButtonFormalArtContract.LargePressed,
                GlobalButtonFormalArtContract.LargeDisabled, actionEnabled);
            GUI.Label(actionArea, T(actionLabelKey),
                Style(24, actionEnabled ? MarketIvory : MarketMuted,
                    TextAnchor.MiddleCenter, FontStyle.Bold));
            if (actionClicked)
            {
                if (isActive)
                    OpenLanternChangeConfirmation(LanternChangeAction.Unequip, instance);
                else OpenLanternChangeConfirmation(hasActiveLantern
                    ? LanternChangeAction.Replace
                    : LanternChangeAction.Equip, instance);
            }
            var closeArea = OffsetEquipmentDetailRect(panel, 662f, 22f, 72f, 72f);
            var closeEnabled = pendingLanternChangeAction == LanternChangeAction.None &&
                warehouseLanternStagePopup < 0;
            if (DrawLanternDetailAStateButton(closeArea, SettingsFormalArtContract.ModuleRoot,
                    SettingsFormalArtContract.CloseNormal, SettingsFormalArtContract.ClosePressed,
                    SettingsFormalArtContract.CloseDisabled, closeEnabled))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "warehouse-lantern-detail-close");
                warehouseLanternDetailOpen = false;
                warehouseLanternDetailId = null;
                warehouseLanternStagePopup = -1;
            }
            if (warehouseLanternStagePopup >= 0)
                DrawWarehouseLanternCovenantPopup(panel, instance, warehouseLanternStagePopup);
            if (pendingLanternChangeAction != LanternChangeAction.None)
                DrawLanternChangeConfirmation();
        }

        private Rect DrawWarehouseLanternDetailAFormal(LanternInstanceState instance)
        {
            var panel = LanternDetailAFormalArtContract.PanelRect;
            DrawMarketTextureFormal(panel, LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.CompactShell);
            GUI.Label(OffsetEquipmentDetailRect(panel, 200f, 22f, 360f, 72f),
                T("warehouse.lanternDetail"),
                Style(31, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));

            var appearance = OffsetEquipmentDetailRect(panel, 48f, 150f, 184f, 184f);
            DrawWarehouseTexture(appearance, WarehouseAFormalArtContract.QualityBackground(
                instance.Quality.ToString()), "quality");
            DrawWarehouseTexture(appearance, WarehouseAFormalArtContract.LanternAppearance(
                instance.TemplateId), "lantern");
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 344f, 210f, 34f), T(instance.TemplateId),
                Style(27, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 380f, 210f, 30f),
                T("quality." + instance.Quality.ToString().ToLowerInvariant()),
                Style(21, MarketQualityTextColorFormal(MarketAssetKind.Lantern,
                    (int)instance.Quality), TextAnchor.MiddleLeft, FontStyle.Normal));
            var equipped = string.Equals(lanternInventory.ActiveLanternInstanceId,
                instance.LanternInstanceId, StringComparison.Ordinal);
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 412f, 210f, 30f),
                equipped ? T("warehouse.lanternEquipped") : T("warehouse.lanternEquip"),
                Style(20, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 444f, 210f, 30f),
                string.Format(T("warehouse.lanternStage"), instance.ContractStage, 6),
                Style(20, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));

            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 258f, 150f, 4f, 280f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.VerticalDivider);
            GUI.Label(OffsetEquipmentDetailRect(panel, 292f, 142f, 140f, 40f),
                T("lantern.detail.baseAttributes"),
                Style(25, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 432f, 146f, 280f, 20f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.SubsectionDivider);
            GUI.Label(OffsetEquipmentDetailRect(panel, 292f, 181f, 360f, 24f),
                T("lantern.detail.baseFlameTitle"),
                Style(19, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 292f, 205f, 360f, 56f),
                EquipmentDetailAFormalArtContract.ResourceRoot,
                EquipmentDetailAFormalArtContract.BaseRow);
            GUI.Label(OffsetEquipmentDetailRect(panel, 310f, 205f, 324f, 56f),
                string.Format(T("warehouse.lanternBaseFlame"), instance.BaseFlameValue / 100f),
                Style(20, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 292f, 273f, 360f, 24f),
                T("warehouse.lanternGlyphTitle"),
                Style(19, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            for (var index = 0; index < 3; index++)
            {
                var row = OffsetEquipmentDetailRect(panel, 292f, 301f + index * 60f, 360f, 56f);
                DrawMarketTextureFormal(row, EquipmentDetailAFormalArtContract.ResourceRoot,
                    EquipmentDetailAFormalArtContract.MagicRow);
                var text = index < instance.ModifierRolls.Count
                    ? LanternModifierRowText(instance.ModifierRolls[index])
                    : index == 0 ? T("warehouse.lanternNoGlyph") : string.Empty;
                GUI.Label(new Rect(row.x + 18f, row.y, row.width - 36f, row.height), text,
                    Style(20, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            }

            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 48f, 488f, 664f, 20f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.SectionDivider);
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 515f, 240f, 28f),
                T("warehouse.lanternCoreTitle"),
                Style(24, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 48f, 548f, 664f, 112f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.CorePanel);
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 62f, 548f, 112f, 112f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.CoreEmblem);
            GUI.Label(OffsetEquipmentDetailRect(panel, 190f, 554f, 492f, 38f),
                T(instance.CoreEffectId),
                Style(23, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 190f, 590f, 492f, 56f),
                string.Format(T("lantern.detail.coreRules"), instance.RulesVersion),
                Style(18, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 48f, 655f, 664f, 20f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.SectionDivider);
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 680f, 240f, 28f),
                T("lantern.detail.covenantSlots"),
                Style(24, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));

            var authoritative = FindAuthoritativeLantern(instance.LanternInstanceId);
            var optionIds = authoritative == null || authoritative.CovenantOptionIds == null
                ? new string[0] : authoritative.CovenantOptionIds;
            for (var index = 0; index < 6; index++)
                DrawWarehouseLanternCovenantSlot(panel, instance, optionIds, index);
            return panel;
        }

        private void DrawWarehouseLanternCovenantSlot(Rect panel, LanternInstanceState instance,
            string[] optionIds, int index)
        {
            var row = index / 2;
            var column = index % 2;
            var slot = OffsetEquipmentDetailRect(panel, column == 0 ? 48f : 392f,
                712f + row * 58f, 320f, 56f);
            var lit = index < instance.ContractStage;
            DrawMarketTextureFormal(slot, LanternDetailAFormalArtContract.ResourceRoot,
                lit ? LanternDetailAFormalArtContract.CovenantSlotNormal :
                    LanternDetailAFormalArtContract.CovenantSlotDisabled);
            var roman = new[] { "I", "II", "III", "IV", "V", "VI" };
            GUI.Label(new Rect(slot.x + 12f, slot.y + 4f, 48f, 48f), roman[index],
                Style(17, lit ? DetailBodyInk : MarketMuted, TextAnchor.MiddleCenter, FontStyle.Normal));
            var optionId = index < optionIds.Length ? optionIds[index] : null;
            var name = lit && !string.IsNullOrWhiteSpace(optionId)
                ? T("lantern.covenant." + optionId)
                : lit ? T("lantern.detail.stageLit") : T("lantern.detail.stageUnlit");
            DrawLanternDetailASlotName(new Rect(slot.x + 66f, slot.y + 4f, 186f, 48f), name,
                lit ? DetailBodyInk : MarketMuted);
            var info = new Rect(slot.x + 268f, slot.y + 6f, 44f, 44f);
            var canInspect = lit;
            var clicked = DrawLanternDetailAStateButton(info,
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.InfoNormal,
                LanternDetailAFormalArtContract.InfoPressed,
                LanternDetailAFormalArtContract.InfoDisabled, canInspect);
            if (clicked)
            {
                warehouseLanternStagePopup = index;
                PlayUiClick(UiAudioCueIds.Tap, "lantern-covenant-stage-detail");
            }
        }

        private void DrawLanternDetailASlotName(Rect area, string value, Color color)
        {
            var text = value ?? string.Empty;
            GUIStyle style = null;
            for (var size = 17; size >= 13; size--)
            {
                style = Style(size, color, TextAnchor.MiddleLeft, FontStyle.Normal);
                style.clipping = TextClipping.Clip;
                if (style.CalcSize(new GUIContent(text)).x <= area.width) break;
            }
            if (style.CalcSize(new GUIContent(text)).x > area.width)
            {
                const string ellipsis = "…";
                while (text.Length > 0 &&
                       style.CalcSize(new GUIContent(text + ellipsis)).x > area.width)
                    text = text.Substring(0, text.Length - 1);
                text += ellipsis;
            }
            GUI.Label(area, text, style);
        }

        private void DrawWarehouseLanternCovenantPopup(Rect panel, LanternInstanceState instance,
            int stageIndex)
        {
            var popup = OffsetEquipmentDetailRect(panel, 100f, 260f, 560f, 460f);
            DrawMarketTextureFormal(popup, LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.CovenantPopupShell);
            GUI.Label(new Rect(popup.x + 130f, popup.y + 10f, 300f, 72f),
                T("lantern.detail.popupTitle"),
                Style(27, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));
            var close = new Rect(popup.x + 470f, popup.y + 10f, 72f, 72f);
            if (DrawLanternDetailAStateButton(close, SettingsFormalArtContract.ModuleRoot,
                    SettingsFormalArtContract.CloseNormal, SettingsFormalArtContract.ClosePressed,
                    SettingsFormalArtContract.CloseDisabled, true))
            {
                warehouseLanternStagePopup = -1;
                PlayUiClick(UiAudioCueIds.Cancel, "lantern-covenant-stage-close");
                return;
            }
            DrawMarketTextureFormal(new Rect(popup.x + 32f, popup.y + 94f, 112f, 112f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.StageMedallion);
            var roman = new[] { "I", "II", "III", "IV", "V", "VI" };
            GUI.Label(new Rect(popup.x + 32f, popup.y + 94f, 112f, 112f), roman[stageIndex],
                Style(28, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));
            DrawMarketTextureFormal(new Rect(popup.x + 164f, popup.y + 90f, 4f, 120f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.CovenantPopupVerticalDivider);
            var authoritative = FindAuthoritativeLantern(instance.LanternInstanceId);
            var optionIds = authoritative == null || authoritative.CovenantOptionIds == null
                ? new string[0] : authoritative.CovenantOptionIds;
            var optionId = stageIndex < optionIds.Length ? optionIds[stageIndex] : null;
            var title = string.IsNullOrWhiteSpace(optionId)
                ? T("lantern.detail.stageLit") : T("lantern.covenant." + optionId);
            GUI.Label(new Rect(popup.x + 192f, popup.y + 92f, 320f, 48f), title,
                Style(25, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(popup.x + 192f, popup.y + 142f, 320f, 40f),
                T("lantern.detail.stageStatusLit"),
                Style(20, MarketGold, TextAnchor.MiddleLeft, FontStyle.Normal));
            DrawMarketTextureFormal(new Rect(popup.x + 50f, popup.y + 224f, 460f, 20f),
                LanternDetailAFormalArtContract.ResourceRoot,
                LanternDetailAFormalArtContract.CovenantPopupDivider);
            GUI.Label(new Rect(popup.x + 50f, popup.y + 252f, 460f, 38f),
                T("lantern.detail.popupEffectTitle"),
                Style(24, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(new Rect(popup.x + 50f, popup.y + 294f, 460f, 82f),
                string.IsNullOrWhiteSpace(optionId)
                    ? T("lantern.detail.selectionUnavailable")
                    : T("lantern.detail.effectServerRule"),
                Style(19, DetailBodyInk, TextAnchor.UpperLeft, FontStyle.Normal));
            GUI.Label(new Rect(popup.x + 50f, popup.y + 390f, 460f, 32f),
                string.Format(T("lantern.detail.rulesVersion"), instance.RulesVersion),
                Style(17, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
        }

        private bool DrawLanternDetailAStateButton(Rect area, string root, string normal,
            string pressed, string disabled, bool enabled)
        {
            var pointerDown = enabled && Event.current != null &&
                area.Contains(Event.current.mousePosition) && Input.GetMouseButton(0);
            DrawMarketTextureFormal(area, root, enabled ? (pointerDown ? pressed : normal) : disabled);
            var previousEnabled = GUI.enabled;
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = previousEnabled;
            return clicked;
        }

        private Rect DrawLanternDetailFormal(LanternInstanceState instance, string displayTemplateId)
        {
            var panel = LanternDetailFormalArtContract.PanelRect;
            DrawMarketTextureFormal(panel, LanternDetailFormalArtContract.ResourceRoot,
                LanternDetailFormalArtContract.FixedShell);
            GUI.Label(OffsetEquipmentDetailRect(panel, 250f, 36f, 482f, 84f),
                T("warehouse.lanternDetail"),
                Style(34, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));

            var attributesTab = OffsetEquipmentDetailRect(panel, 150f, 172f, 330f, 64f);
            var covenantTab = OffsetEquipmentDetailRect(panel, 502f, 172f, 330f, 64f);
            DrawMarketTextureFormal(attributesTab, LanternDetailFormalArtContract.ResourceRoot,
                lanternDetailCovenantPage ? LanternDetailFormalArtContract.TabInactive :
                    LanternDetailFormalArtContract.TabActive);
            DrawMarketTextureFormal(covenantTab, LanternDetailFormalArtContract.ResourceRoot,
                lanternDetailCovenantPage ? LanternDetailFormalArtContract.TabActive :
                    LanternDetailFormalArtContract.TabInactive);
            GUI.Label(attributesTab, T("lantern.detail.attributesTab"),
                Style(24, lanternDetailCovenantPage ? MarketInk : MarketIvory,
                    TextAnchor.MiddleCenter, FontStyle.Bold));
            GUI.Label(covenantTab, T("lantern.detail.covenantTab"),
                Style(24, lanternDetailCovenantPage ? MarketIvory : MarketInk,
                    TextAnchor.MiddleCenter, FontStyle.Bold));
            if (GUI.Button(attributesTab, GUIContent.none, GUIStyle.none))
            {
                lanternDetailCovenantPage = false;
                PlayUiClick(UiAudioCueIds.TabSwitch, "lantern-detail-attributes");
            }
            if (GUI.Button(covenantTab, GUIContent.none, GUIStyle.none))
            {
                lanternDetailCovenantPage = true;
                PlayUiClick(UiAudioCueIds.TabSwitch, "lantern-detail-covenant");
            }

            var pageArea = OffsetEquipmentDetailRect(panel, 62f, 258f, 858f, 930f);
            DrawMarketTextureFormal(pageArea, LanternDetailFormalArtContract.ResourceRoot,
                lanternDetailCovenantPage ? LanternDetailFormalArtContract.CovenantPage :
                    LanternDetailFormalArtContract.AttributesPage);
            if (lanternDetailCovenantPage)
                DrawLanternCovenantPageFormal(panel, instance);
            else
                DrawLanternAttributesPageFormal(panel, instance, displayTemplateId);
            return panel;
        }

        private void DrawLanternAttributesPageFormal(Rect panel, LanternInstanceState instance,
            string displayTemplateId)
        {
            DrawWarehouseTexture(OffsetEquipmentDetailRect(panel, 70f, 270f, 220f, 220f),
                WarehouseAFormalArtContract.LanternDetailAppearance(displayTemplateId), "lantern-detail");
            GUI.Label(OffsetEquipmentDetailRect(panel, 70f, 496f, 280f, 54f), T(displayTemplateId),
                Style(28, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 104f, 554f, 246f, 50f),
                T("quality." + instance.Quality.ToString().ToLowerInvariant()),
                Style(21, MarketQualityTextColorFormal(MarketAssetKind.Lantern, (int)instance.Quality),
                    TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(OffsetEquipmentDetailRect(panel, 70f, 606f, 280f, 40f),
                string.Format(T("warehouse.lanternStage"), instance.ContractStage, 6),
                Style(21, MarketInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(OffsetEquipmentDetailRect(panel, 410f, 282f, 360f, 54f),
                T("lantern.detail.baseFlameTitle"),
                Style(24, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 850f, 280f, 56f, 56f),
                LanternDetailFormalArtContract.SharedResourceRoot, EquipmentDetailFormalArtContract.Info);
            GUI.Label(OffsetEquipmentDetailRect(panel, 438f, 360f, 446f, 122f),
                string.Format(T("warehouse.lanternBaseFlame"), instance.BaseFlameValue / 100f),
                Style(23, MarketInk, TextAnchor.MiddleCenter, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 70f, 712f, 350f, 54f),
                T("warehouse.lanternGlyphTitle"),
                Style(24, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 850f, 711f, 56f, 56f),
                LanternDetailFormalArtContract.SharedResourceRoot, EquipmentDetailFormalArtContract.Info);
            for (var index = 0; index < 3; index++)
            {
                var text = index < instance.ModifierRolls.Count
                    ? LanternModifierRowText(instance.ModifierRolls[index])
                    : index == 0 ? T("warehouse.lanternNoGlyph") : string.Empty;
                GUI.Label(OffsetEquipmentDetailRect(panel, 102f, 790f + index * 110f, 778f, 84f), text,
                    Style(23, MarketInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            }
        }

        private void DrawLanternCovenantPageFormal(Rect panel, LanternInstanceState instance)
        {
            GUI.Label(OffsetEquipmentDetailRect(panel, 62f, 270f, 500f, 56f),
                T("warehouse.lanternCoreTitle"),
                Style(28, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 850f, 268f, 56f, 56f),
                LanternDetailFormalArtContract.SharedResourceRoot, EquipmentDetailFormalArtContract.Info);
            GUI.Label(OffsetEquipmentDetailRect(panel, 102f, 346f, 250f, 56f),
                T("lantern.detail.initialForm"),
                Style(24, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 632f, 346f, 260f, 56f),
                string.Format(T("warehouse.lanternStage"), instance.ContractStage, 6),
                Style(23, MarketInk, TextAnchor.MiddleRight, FontStyle.Bold));
            GUI.Label(OffsetEquipmentDetailRect(panel, 102f, 416f, 778f, 86f),
                T(instance.CoreEffectId), Style(25, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            var roman = new[] { "I", "II", "III", "IV", "V", "VI" };
            for (var index = 0; index < roman.Length; index++)
            {
                var row = OffsetEquipmentDetailRect(panel, 62f, 558f + index * 96f, 858f, 82f);
                GUI.Label(new Rect(row.x + 34f, row.y, 92f, row.height), roman[index],
                    Style(24, MarketInk, TextAnchor.MiddleCenter, FontStyle.Bold));
                GUI.Label(new Rect(row.x + 142f, row.y, 250f, row.height),
                    index == 0 ? T("lantern.detail.initialForm") : T("lantern.detail.contractStage"),
                    Style(22, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
                GUI.Label(new Rect(row.x + 402f, row.y, 416f, row.height),
                    instance.ContractStage >= index + 1 ? T(instance.CoreEffectId) : T("lantern.detail.locked"),
                    Style(22, MarketInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            }
            GUI.Label(OffsetEquipmentDetailRect(panel, 62f, 1140f, 858f, 42f),
                string.Format(T("lantern.detail.rulesVersion"), instance.RulesVersion),
                Style(20, MarketInk, TextAnchor.MiddleCenter, FontStyle.Bold));
        }

        private string LanternModifierRowText(LanternModifierRoll modifier)
        {
            var percentage = !string.Equals(modifier.ModifierId,
                FirstChapterLanternFixtures.ArmorModifierId, StringComparison.Ordinal);
            return string.Format(T(modifier.ModifierId),
                percentage ? (object)(modifier.Value / 100f) : modifier.Value);
        }

        private void DrawLanternChangeConfirmation()
        {
            var titleKey = pendingLanternChangeAction == LanternChangeAction.Unequip
                ? "warehouse.lanternConfirmUnequipTitle"
                : pendingLanternChangeAction == LanternChangeAction.Replace
                    ? "warehouse.lanternConfirmReplaceTitle"
                    : "warehouse.lanternConfirmEquipTitle";
            var result = DrawCommonConfirmDialog(T(titleKey), T("warehouse.lanternConfirmBody"),
                string.Empty, T("arena.cancel"), T("warehouse.lanternConfirmAction"));
            if (result == 0)
            {
                PlayUiClick(UiAudioCueIds.Cancel, "warehouse-lantern-confirm-cancel");
                CloseLanternChangeConfirmation();
            }
            else if (result == 1) ConfirmLanternChange();
        }

        private void DrawWarehouseEquipmentSlot(Rect area, EquipmentInstanceState instance)
        {
            DrawWarehouseEquipmentVisual(area, instance);
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "warehouse-equipment-detail-open");
                warehouseEquipmentDetailId = instance.EquipmentInstanceId;
                warehouseEquipmentDetailOpen = true;
            }
        }

        private void DrawWarehouseEquipmentVisual(Rect area, EquipmentInstanceState instance)
        {
            DrawWarehouseTexture(area, WarehouseAFormalArtContract.QualityBackground(
                EquipmentRarityId(instance.Quality)), "quality");
            DrawWarehouseTexture(area, WarehouseAFormalArtContract.EquipmentAppearance(
                instance.TemplateId), "equipment");
            if (!string.IsNullOrEmpty(instance.EquippedCharacterInstanceId))
            {
                GUI.Label(new Rect(area.x + 8f, area.y + 6f, area.width - 16f, 28f), T("warehouse.equipped"),
                    Style(14, new Color(1f, 0.86f, 0.42f), TextAnchor.MiddleLeft, FontStyle.Bold));
            }
        }

        private void DrawWarehouseTexture(Rect area, string resourceId, string group)
        {
            var texture = artCatalog.GetWarehouseTexture(resourceId, group);
            if (texture == null) return;
            if (Mathf.RoundToInt(area.width) != texture.width || Mathf.RoundToInt(area.height) != texture.height)
                return;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
        }

        private void DrawEquipmentDetailAppearanceFormal(Rect area, string templateId, int quality)
        {
            DrawMarketItemLayersFormal(area, MarketAssetKind.Equipment, templateId, quality, true);
        }

        private void DrawWarehouseButtonTexture(Rect area, string normalResourceId,
            string pressedResourceId)
        {
            var currentEvent = Event.current;
            var pressed = currentEvent != null && area.Contains(currentEvent.mousePosition) &&
                          Input.GetMouseButton(0);
            DrawWarehouseTexture(area, pressed ? pressedResourceId : normalResourceId, "modules");
        }

        private void HandleWarehouseScrollPointer(Rect viewport, float contentHeight)
        {
            var pointerEvent = Event.current;
            if (pointerEvent == null) return;
            var pointerPosition = pointerEvent.mousePosition;
            if (pointerEvent.type == EventType.MouseDown && pointerEvent.button == 0 &&
                viewport.Contains(pointerPosition) && !warehouseItemPointerHeld)
            {
                warehousePointerHeld = true;
                warehousePointerDragging = false;
                warehousePointerDown = pointerPosition;
                warehousePointerLast = pointerPosition;
                return;
            }
            if (pointerEvent.type == EventType.MouseDrag && warehousePointerHeld)
            {
                if (!warehousePointerDragging &&
                    (pointerPosition - warehousePointerDown).sqrMagnitude >= 64f)
                    warehousePointerDragging = true;
                if (warehousePointerDragging)
                {
                    var maximumScroll = Mathf.Max(0f, contentHeight - viewport.height);
                    var dragDelta = pointerPosition - warehousePointerLast;
                    warehouseEquipmentScroll.y = Mathf.Clamp(
                        warehouseEquipmentScroll.y - dragDelta.y, 0f, maximumScroll);
                    pointerEvent.Use();
                }
                warehousePointerLast = pointerPosition;
                return;
            }
            if (pointerEvent.type != EventType.MouseUp || !warehousePointerHeld) return;
            if (warehousePointerDragging) pointerEvent.Use();
            warehousePointerHeld = false;
            warehousePointerDragging = false;
        }

        private void ResetWarehouseItemPointer()
        {
            warehouseDragSourceSlot = -1;
            warehouseDraggedToken = null;
            warehouseItemPointerHeld = false;
            warehouseItemPointerDragging = false;
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
            var panel = EquipmentDetailAFormalArtContract.ViewPanelRect;
            DrawEquipmentDetailAContent(panel, false, T("market.detail"), instance.TemplateId,
                instance.Quality, T(instance.TemplateId), T(EquipmentQualityKey(instance.Quality)),
                T(EquipmentSlotKey(template.Slot)) + " · " + EquipmentUsageStateText(instance),
                template.Modifier);

            if (GUI.Button(OffsetEquipmentDetailRect(panel, 660f, 20f, 76f, 76f),
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "warehouse-equipment-detail-close");
                warehouseEquipmentDetailOpen = false;
                warehouseEquipmentDetailId = null;
            }
        }

        private Rect CurrentWarehouseEquipmentDetailPanel()
        {
            return warehouseCosmeticDetailOpen
                ? EquipmentDetailAFormalArtContract.ActionPanelRect
                : WarehouseEquipmentDetailPanel;
        }

        private static Rect OffsetEquipmentDetailRect(Rect panel, float x, float y, float width, float height)
        {
            return new Rect(panel.x + x, panel.y + y, width, height);
        }

        private void DrawEquipmentDetailAContent(Rect panel, bool actionShell, string title,
            string templateId, EquipmentQuality quality, string displayName, string qualityName,
            string slotAndLimit, EquipmentStatModifier modifier)
        {
            DrawEquipmentDetailAModule(panel, actionShell
                    ? EquipmentDetailAFormalArtContract.ActionShell
                    : EquipmentDetailAFormalArtContract.ViewShell,
                760, actionShell ? 650 : 570);
            GUI.Label(OffsetEquipmentDetailRect(panel, 130f, 20f, 500f, 70f), title,
                Style(31, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));

            var appearance = OffsetEquipmentDetailRect(panel, 48f, 145f, 184f, 184f);
            DrawWarehouseTexture(appearance, WarehouseAFormalArtContract.QualityBackground(
                EquipmentRarityId(quality)), "quality");
            DrawWarehouseTexture(appearance,
                WarehouseAFormalArtContract.EquipmentAppearance(templateId), "equipment");
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 340f, 210f, 34f), displayName,
                Style(26, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            DrawMarketTextureFormal(OffsetEquipmentDetailRect(panel, 48f, 381f, 24f, 24f),
                MarketFormalModuleRoot, "ART-UI-MARKET-B-QUALITY-" +
                MarketQualityAssetNameFormal(MarketAssetKind.Equipment, (int)quality) +
                "-24-v1.0.0");
            GUI.Label(OffsetEquipmentDetailRect(panel, 82f, 381f, 176f, 30f), qualityName,
                Style(21, EquipmentRarityColor(quality), TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(OffsetEquipmentDetailRect(panel, 48f, 417f, 210f, 30f), slotAndLimit,
                Style(21, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));

            GUI.Label(OffsetEquipmentDetailRect(panel, 292f, 139f, 360f, 35f),
                T("market.detail.baseAttributes"),
                Style(25, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            var baseNames = new List<string>();
            var baseValues = new List<string>();
            CollectEquipmentDetailBaseAttributes(modifier, baseNames, baseValues);
            if (baseNames.Count > 0)
            {
                var baseRow = OffsetEquipmentDetailRect(panel, 292f, 174f, 360f, 56f);
                DrawEquipmentDetailAModule(baseRow, EquipmentDetailAFormalArtContract.BaseRow, 360, 56);
                DrawEquipmentDetailARowText(baseRow, baseNames[0], baseValues[0]);
            }

            DrawEquipmentDetailAModule(OffsetEquipmentDetailRect(panel, 292f, 240f, 360f, 24f),
                EquipmentDetailAFormalArtContract.SectionDivider, 360, 24);
            GUI.Label(OffsetEquipmentDetailRect(panel, 292f, 270f, 360f, 35f),
                T("market.detail.magicAttributes"),
                Style(25, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Bold));
            for (var index = 1; index < baseNames.Count && index < 4; index++)
            {
                var row = OffsetEquipmentDetailRect(panel, 292f, 305f + (index - 1) * 60f, 360f, 56f);
                DrawEquipmentDetailAModule(row, EquipmentDetailAFormalArtContract.MagicRow, 360, 56);
                DrawEquipmentDetailARowText(row, baseNames[index], baseValues[index]);
            }
        }

        private void DrawEquipmentDetailARowText(Rect row, string name, string value)
        {
            GUI.Label(new Rect(row.x + 18f, row.y, 230f, row.height), name,
                Style(20, DetailBodyInk, TextAnchor.MiddleLeft, FontStyle.Normal));
            GUI.Label(new Rect(row.x + 248f, row.y, 94f, row.height), value,
                Style(20, MarketGold, TextAnchor.MiddleRight, FontStyle.Bold));
        }

        private void CollectEquipmentDetailBaseAttributes(EquipmentStatModifier modifier,
            List<string> names, List<string> values)
        {
            if (modifier == null) return;
            AddEquipmentDetailBaseAttribute(names, values, "stat.health", modifier.MaxHealth);
            AddEquipmentDetailBaseAttribute(names, values, "stat.attack", modifier.AttackDamage);
            AddEquipmentDetailBaseAttribute(names, values, "stat.armor", modifier.Armor);
            AddEquipmentDetailBaseAttribute(names, values, "stat.healing", modifier.HealAmount);
            AddEquipmentDetailBaseAttribute(names, values, "stat.speedTier", modifier.SpeedTierShift);
        }

        private void AddEquipmentDetailBaseAttribute(List<string> names, List<string> values,
            string localizationKey, int value)
        {
            if (value == 0) return;
            names.Add(T(localizationKey));
            values.Add(value > 0 ? "+" + value : value.ToString());
        }

        private void DrawEquipmentDetailAModule(Rect area, string resourceId,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetEquipmentDetailATexture(resourceId);
            if (texture == null) return;
            if (texture.width != expectedWidth || texture.height != expectedHeight ||
                Mathf.RoundToInt(area.width) != expectedWidth ||
                Mathf.RoundToInt(area.height) != expectedHeight)
            {
                Debug.LogError("Rejected resized Equipment Detail A module: " + resourceId);
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
        }

        private bool DrawWarehouseEquipmentDetailBackdrop(Rect panel)
        {
            var transparent = GUIStyle.none;
            var closeRequested = GUI.Button(new Rect(0f, 135f, DesignWidth, panel.y - 135f), GUIContent.none, transparent) |
                                 GUI.Button(new Rect(0f, panel.y, panel.x, panel.height), GUIContent.none, transparent) |
                                 GUI.Button(new Rect(panel.xMax, panel.y, DesignWidth - panel.xMax, panel.height), GUIContent.none, transparent) |
                                 GUI.Button(new Rect(0f, panel.yMax, DesignWidth, layoutContentHeight - panel.yMax), GUIContent.none, transparent);
            if (!closeRequested) return false;
            PlayUiClick(UiAudioCueIds.Cancel, "warehouse-detail-backdrop-close");
            warehouseEquipmentDetailOpen = false;
            warehouseEquipmentDetailId = null;
            warehouseLanternDetailOpen = false;
            warehouseLanternDetailId = null;
            warehouseCosmeticDetailOpen = false;
            warehouseCosmeticDetailProductId = null;
            return true;
        }

        private string EquipmentUsageStateText(EquipmentInstanceState instance)
        {
            if (instance.UsageLocks != EquipmentUsageLock.None) return T("equipment.locked");
            if (!string.IsNullOrEmpty(instance.EquippedCharacterInstanceId)) return T("warehouse.equipped");
            return T("warehouse.available");
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
            DrawDeckWorkspace(false);
        }

        private void DrawDeckWorkspace(bool editable)
        {
            var selection = editable ? deckDraftSelection : nextRunDeckSelection;
            DrawHomeLighthouseCanvas(PrototypeArtResourceIds.HomeLighthouseBackground, false);
            DrawStoreModule(StoreFormalArtContract.BackgroundVeilResourceId,
                new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);
            DrawDeckText(new Rect(72f, 28f, 560f, 72f), T("nav.deck"), 60,
                new Color32(244, 231, 195, 255), TextAnchor.MiddleLeft, true);
            DrawDeckText(new Rect(76f, 100f, 760f, 38f),
                T(editable ? deckEditorMessageKey : "collection.subtitle"), 25,
                new Color32(227, 211, 162, 255), TextAnchor.MiddleLeft, false);

            DrawDeckModule(DeckAtlasFormalArtContract.ActivePanel,
                new Rect(40f, 145f, 1000f, 440f), 1000, 440);
            DrawActiveDeckCards(new Rect(40f, 145f, 1000f, 440f), selection, editable);
            var editButton = new Rect(345f, 456f, 390f, 86f);
            DrawArenaModule(ArenaFormalArtContract.ModalButton, editButton, 390, 86);
            DrawDeckText(editButton, T(editable ? "deck.editorTitle" : "deck.edit"), 28,
                new Color32(244, 231, 195, 255), TextAnchor.MiddleCenter, true);
            if (!editable && GUI.Button(editButton, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "deck-edit-open");
                BeginDeckEditing();
                return;
            }

            DrawDeckModule(DeckAtlasFormalArtContract.LibraryPanel,
                new Rect(40f, 600f, 1000f, 1600f), 1000, 1600);

            var entries = DeckCollectionSortRules.Sort(playerCollection.Entries,
                deckCollectionSortMode, deckCollectionSortDescending,
                owned => CharacterDisplayName(owned.DefinitionId, owned.Quality), characterFavorites);
            var viewport = new Rect(105f, 790f, 870f, 1330f);
            var rows = Mathf.CeilToInt(entries.Count / 3f);
            var contentHeight = Mathf.Max(viewport.height, rows * 398f);
            HandleDeckCollectionPointer(viewport, contentHeight);
            deckCollectionScroll = GUI.BeginScrollView(viewport, deckCollectionScroll,
                new Rect(0f, 0f, 870f, contentHeight), false, false, GUIStyle.none, GUIStyle.none);
            for (var i = 0; i < entries.Count; i++)
            {
                var column = i % 3;
                var row = i / 3;
                var cardY = row * 398f;
                if (cardY + 378f < deckCollectionScroll.y - 20f ||
                    cardY > deckCollectionScroll.y + viewport.height + 20f) continue;
                var selected = selection.ContainsInstance(entries[i].CharacterInstanceId);
                DrawDeckLibraryCard(new Rect(5f + column * 295f, cardY, 270f, 378f),
                    entries[i], selected, editable);
            }
            GUI.EndScrollView();
            DrawDeckModule(DeckAtlasFormalArtContract.LibraryPanelFront,
                new Rect(40f, 600f, 1000f, 1600f), 1000, 1600);
            DrawDeckCollectionSortControls(new Rect(215f, 712f, 650f, 64f));
            DrawDeckScrollBar(contentHeight, viewport.height);
        }

        private void BeginDeckEditing()
        {
            deckDraftSelection = new RunDeckSelection(playerCollection,
                nextRunDeckSelection.SelectedInstanceIds, MaximumDeckSize, 0);
            deckEquipmentDraft = equipmentInventory.CreateAssignmentDraft();
            deckDraftAutomaticUnequipCount = 0;
            deckEditorMessageKey = "deck.editorHint";
            deckSaveConfirmationOpen = false;
            deckEditing = true;
            characterDetailReturnToDeckEditing = false;
            deckCollectionScroll = Vector2.zero;
        }

        private void DrawDeckEditorScreen()
        {
            if (deckDraftSelection == null) BeginDeckEditing();
            if (deckSaveConfirmationOpen)
            {
                DrawDeckSaveConfirmation();
                return;
            }
            DrawDeckWorkspace(true);
            var cancelRect = new Rect(220f, 2210f, 280f, 76f);
            var saveRect = new Rect(580f, 2210f, 280f, 76f);
            var cancelClicked = DrawLanternDetailAStateButton(cancelRect,
                GlobalButtonFormalArtContract.ResourceRoot, GlobalButtonFormalArtContract.LargeNormal,
                GlobalButtonFormalArtContract.LargePressed, GlobalButtonFormalArtContract.LargeDisabled, true);
            GUI.Label(cancelRect, T("deck.cancel"),
                Style(24, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));
            var saveClicked = DrawLanternDetailAStateButton(saveRect,
                GlobalButtonFormalArtContract.ResourceRoot, GlobalButtonFormalArtContract.LargeNormal,
                GlobalButtonFormalArtContract.LargePressed, GlobalButtonFormalArtContract.LargeDisabled, true);
            GUI.Label(saveRect, T("deck.save"),
                Style(24, MarketIvory, TextAnchor.MiddleCenter, FontStyle.Bold));
            if (cancelClicked)
            {
                PlayUiClick(UiAudioCueIds.Cancel, "deck-edit-cancel");
                deckSaveConfirmationOpen = false;
                deckEditing = false;
                deckDraftSelection = null;
                deckEquipmentDraft = null;
                deckDraftAutomaticUnequipCount = 0;
            }
            if (saveClicked)
            {
                if (deckDraftSelection.Count < MinimumDeckSize)
                {
                    PlayUiClick(UiAudioCueIds.Disabled, "deck-save-below-minimum");
                    deckEditorMessageKey = "deck.minimumRequired";
                }
                else
                {
                    PlayUiClick(UiAudioCueIds.Warning, "deck-save-impact-warning");
                    deckSaveConfirmationOpen = true;
                }
            }
        }

        private void DrawDeckSaveConfirmation()
        {
            var result = DrawCommonConfirmDialog(T("deck.confirmTitle"),
                string.Format(T("deck.confirmBody"), deckDraftSelection.Count,
                    deckDraftAutomaticUnequipCount), string.Empty,
                T("deck.continueEditing"), T("deck.confirmSave"));
            if (result == 0)
            {
                PlayUiClick(UiAudioCueIds.Cancel, "deck-save-confirm-cancel");
                deckSaveConfirmationOpen = false;
            }
            else if (result == 1) CommitDeckChanges();
        }

        private void CommitDeckChanges()
        {
            if (deckDraftSelection == null || deckEquipmentDraft == null ||
                deckDraftSelection.Count < MinimumDeckSize)
            {
                deckSaveConfirmationOpen = false;
                deckEditorMessageKey = "deck.minimumRequired";
                PlayUiClick(UiAudioCueIds.Disabled, "deck-save-confirm-invalid");
                return;
            }
            var changeCount = deckEquipmentDraft.CountChanges(equipmentInventory.Instances);
            var request = new WalletDeckChangeRequest(
                "deck-change-" + ClientVersion + "-" + DateTime.UtcNow.Ticks,
                DeckChangeRulesVersion,
                "characters=" + deckDraftSelection.Count + ";equipmentChanges=" + changeCount);
            var signature = walletSignatureGateway.RequestDeckChangeSignature(request);
            if (signature == null || !signature.Approved)
            {
                deckSaveConfirmationOpen = false;
                deckEditorMessageKey = "deck.signatureRejected";
                PlayUiClick(UiAudioCueIds.Disabled, "deck-save-signature-rejected");
                return;
            }
            if (!equipmentInventory.TryApplyAssignmentDraft(deckEquipmentDraft,
                    deckDraftSelection.SelectedInstanceIds))
            {
                deckSaveConfirmationOpen = false;
                deckEditorMessageKey = "deck.commitInvalid";
                PlayUiClick(UiAudioCueIds.Disabled, "deck-save-equipment-invalid");
                return;
            }
            PlayUiClick(UiAudioCueIds.Confirm, "deck-save-confirm");
            nextRunDeckSelection = new RunDeckSelection(playerCollection,
                deckDraftSelection.SelectedInstanceIds, MaximumDeckSize, MinimumDeckSize);
            var removed = ReconcileDefenseWithActiveDeck();
            if (removed > 0) pendingDefenseRemovedCount += removed;
            deckSaveConfirmationOpen = false;
            deckEditing = false;
            deckDraftSelection = null;
            deckEquipmentDraft = null;
            deckDraftAutomaticUnequipCount = 0;
            RefreshArenaFormationsAfterEquipmentOwnershipChange();
            SaveGame();
        }

        private void HandleDeckCollectionPointer(Rect viewport, float contentHeight)
        {
            var pointerEvent = Event.current;
            if (pointerEvent == null) return;

            var pointerPosition = pointerEvent.mousePosition;
            if (pointerEvent.type == EventType.MouseDown && pointerEvent.button == 0 &&
                viewport.Contains(pointerPosition))
            {
                deckCollectionPointerHeld = true;
                deckCollectionPointerDragging = false;
                deckCollectionPointerDown = pointerPosition;
                deckCollectionPointerLast = pointerPosition;
                return;
            }

            if (pointerEvent.type == EventType.MouseDrag && deckCollectionPointerHeld)
            {
                if (!deckCollectionPointerDragging &&
                    (pointerPosition - deckCollectionPointerDown).sqrMagnitude >= 64f)
                    deckCollectionPointerDragging = true;

                if (deckCollectionPointerDragging)
                {
                    var maximumScroll = Mathf.Max(0f, contentHeight - viewport.height);
                    var dragDelta = pointerPosition - deckCollectionPointerLast;
                    deckCollectionScroll.y = Mathf.Clamp(
                        deckCollectionScroll.y - dragDelta.y, 0f, maximumScroll);
                    pointerEvent.Use();
                }
                deckCollectionPointerLast = pointerPosition;
                return;
            }

            if (pointerEvent.type != EventType.MouseUp || !deckCollectionPointerHeld) return;
            if (deckCollectionPointerDragging) pointerEvent.Use();
            deckCollectionPointerHeld = false;
            deckCollectionPointerDragging = false;
        }

        private void DrawActiveDeckCards(Rect area, RunDeckSelection selection, bool editable)
        {
            // Keep the dynamic title clear of the approved panel's left ornament and
            // rounded border. Only the text safe area moves; the art remains untouched.
            DrawDeckText(new Rect(110f, 220f, 360f, 36f),
                T("deck.configured") + "  " + selection.Count + "/" + MaximumDeckSize, 23,
                new Color32(31, 61, 65, 255), TextAnchor.MiddleLeft, false);
            for (var index = 0; index < MaximumDeckSize; index++)
            {
                var slot = new Rect(85f + 150f * index, 260f, 130f, 182f);
                var occupied = index < selection.SelectedInstanceIds.Count;
                if (!occupied) continue;

                var owned = playerCollection.GetByInstanceId(selection.SelectedInstanceIds[index]);
                DrawDeckSmallCard(slot, owned, editable);
                if (GUI.Button(new Rect(slot.x, slot.y, slot.width, 136f),
                        GUIContent.none, GUIStyle.none))
                {
                    PlayUiClick(UiAudioCueIds.Tap, "deck-active-card-open");
                    if (!editable) BeginDeckEditing();
                    OpenCharacterDetail(owned.CharacterInstanceId,
                        DeckCharacterDetailMode.ActiveDeckLoadout, true);
                }
                if (!editable) continue;
                var remove = new Rect(slot.x + 82f, slot.y + 134f, 44f, 44f);
                DrawDeckInteractionModule(DeckAtlasFormalArtContract.RemoveControl, remove, 44, 44);
                if (GUI.Button(remove, GUIContent.none, GUIStyle.none))
                {
                    PlayUiClick(UiAudioCueIds.Tap, "deck-active-card-remove");
                    deckDraftSelection.TryRemoveInstance(owned.CharacterInstanceId);
                    if (deckEquipmentDraft != null)
                        deckDraftAutomaticUnequipCount +=
                            deckEquipmentDraft.ClearCharacter(owned.CharacterInstanceId);
                    deckEditorMessageKey = "deck.unsaved";
                }
            }
        }

        private void DrawDeckCollectionSortControls(Rect area)
        {
            var sortBar = DeckAtlasFormalArtContract.SortBarFor(deckCollectionSortMode);
            if (deckCollectionSortMode == DeckCollectionSortMode.Quality)
                DrawDeckModule(sortBar, area, 650, 64);
            else
                DrawDeckInteractionModule(sortBar, area, 650, 64);
            var modes = new[]
            {
                DeckCollectionSortMode.Quality, DeckCollectionSortMode.Name,
                DeckCollectionSortMode.AcquiredAt, DeckCollectionSortMode.Favorite
            };
            var keys = new[]
            {
                "collection.sort.quality", "collection.sort.name",
                "collection.sort.acquired", "collection.sort.favorite"
            };
            var width = area.width / modes.Length;
            for (var i = 0; i < modes.Length; i++)
            {
                var selected = deckCollectionSortMode == modes[i];
                var label = T(keys[i]) + (selected ? (deckCollectionSortDescending ? " ↓" : " ↑") : string.Empty);
                var segment = new Rect(area.x + i * width, area.y, width, area.height);
                DrawDeckText(segment, label, 20,
                    selected ? new Color32(246, 239, 213, 255) : new Color32(31, 61, 65, 255),
                    TextAnchor.MiddleCenter, selected);
                if (!GUI.Button(segment, GUIContent.none, GUIStyle.none))
                    continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "deck-sort");
                if (selected) deckCollectionSortDescending = !deckCollectionSortDescending;
                else
                {
                    deckCollectionSortMode = modes[i];
                    deckCollectionSortDescending = DeckCollectionSortRules.DefaultDescending(modes[i]);
                }
                deckCollectionScroll = Vector2.zero;
            }
        }

        private void DrawDeckLibraryCard(Rect area, OwnedCharacterState owned, bool selected, bool editable)
        {
            DrawDeckModule(DeckAtlasFormalArtContract.LargeCardBase, area, 270, 378);
            DrawDeckCharacterLayer(owned.DefinitionId, owned.Quality, false,
                new Rect(area.x + 15f, area.y + 21f, 240f, 261f), 240, 261);
            DrawDeckModule(DeckAtlasFormalArtContract.LargeCardFront, area, 270, 378);
            DrawDeckModule(DeckAtlasFormalArtContract.QualityLayer(false, owned.Quality), area, 270, 378);
            var nameArea = new Rect(area.x + 54f, area.y + 299f, 162f, 64f);
            var displayName = CharacterDisplayName(owned.DefinitionId, owned.Quality);
            GUI.Label(nameArea, displayName,
                DeckCardNameStyle(displayName, nameArea.width, 26, 15));

            if (GUI.Button(new Rect(area.x, area.y, area.width, 329f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "deck-library-card-open");
                OpenCharacterDetail(owned.CharacterInstanceId,
                    DeckCharacterDetailMode.LibraryBase, editable);
            }

            var favoriteArea = new Rect(area.x + 8f, area.y + 330f, 44f, 44f);
            if (characterFavorites.IsFavorite(owned.CharacterInstanceId))
                DrawDeckInteractionModule(DeckAtlasFormalArtContract.FavoriteSelectedControl,
                    favoriteArea, 44, 44);
            else
                DrawDeckModule(DeckAtlasFormalArtContract.FavoriteControl, favoriteArea, 44, 44);
            if (GUI.Button(favoriteArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "deck-favorite-toggle");
                characterFavorites.Toggle(owned.CharacterInstanceId);
                SaveGame();
            }

            var addArea = new Rect(area.x + 218f, area.y + 330f, 44f, 44f);
            DrawDeckModule(DeckAtlasFormalArtContract.AddControl, addArea, 44, 44);
            if (!editable) return;
            var sameDefinitionSelected = deckDraftSelection.Contains(owned.DefinitionId);
            GUI.enabled = !selected && !sameDefinitionSelected && deckDraftSelection.Count < MaximumDeckSize;
            if (GUI.Button(addArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "deck-active-card-add");
                deckDraftSelection.TrySelectInstance(owned.CharacterInstanceId);
                deckEditorMessageKey = "deck.unsaved";
            }
            GUI.enabled = true;
        }

        private void DrawDeckSmallCard(Rect area, OwnedCharacterState owned, bool editable)
        {
            DrawDeckModule(DeckAtlasFormalArtContract.SmallCardBase, area, 130, 182);
            DrawDeckCharacterLayer(owned.DefinitionId, owned.Quality, true,
                new Rect(area.x + 7f, area.y + 10f, 116f, 126f), 116, 126);
            DrawDeckModule(DeckAtlasFormalArtContract.SmallCardFront, area, 130, 182);
            DrawDeckModule(DeckAtlasFormalArtContract.QualityLayer(true, owned.Quality), area, 130, 182);
            var nameArea = new Rect(area.x + 8f, area.y + 137f, editable ? 70f : 114f, 37f);
            var displayName = CharacterDisplayName(owned.DefinitionId, owned.Quality);
            GUI.Label(nameArea, displayName,
                DeckCardNameStyle(displayName, nameArea.width, 16, 10));
        }

        private void DrawDeckModule(string resourceId, Rect rect, int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetDeckAtlasTexture(resourceId, false, false);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Deck A module: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawDeckInteractionModule(string resourceId, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetDeckInteractionTexture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Deck A interaction module: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawDeckCharacterLayer(string definitionId, CharacterQuality quality, bool small, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(definitionId, CharacterQualityResourceId(quality),
                    out artDefinition, out sprite))
            {
                DrawFullCharacterSprite(rect, artDefinition, sprite, 1f);
                return;
            }

            // Keep the approved deck export as a fallback only. The primary path must use
            // the owned instance's five-tier art so class names and character visuals cannot diverge.
            var resourceId = DeckAtlasFormalArtContract.CharacterLayer(definitionId, small);
            var texture = artCatalog.GetDeckAtlasTexture(resourceId, true, small);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Deck A character layer: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private GUIStyle DeckCardNameStyle(string text, float safeWidth, int maximumSize, int minimumSize)
        {
            var style = ArchiveStyle(maximumSize, new Color32(244, 231, 195, 255),
                TextAnchor.MiddleCenter, true);
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            while (style.fontSize > minimumSize &&
                   style.CalcSize(new GUIContent(text)).x > safeWidth)
                style.fontSize--;
            return style;
        }

        private void DrawDeckText(Rect rect, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized)
        {
            var style = ArchiveStyle(fontSize, color, alignment, emphasized);
            style.wordWrap = false;
            style.clipping = TextClipping.Clip;
            GUI.Label(rect, new GUIContent(text), style);
        }

        private void DrawDeckScrollBar(float contentHeight, float viewportHeight)
        {
            const float trackY = 790f;
            const float trackHeight = 1235f;
            var previousColor = GUI.color;
            GUI.color = new Color32(61, 91, 91, 150);
            GUI.DrawTexture(new Rect(982f, trackY, 3f, trackHeight), Texture2D.whiteTexture);
            var maximumScroll = Mathf.Max(0f, contentHeight - viewportHeight);
            var thumbHeight = maximumScroll <= 0f
                ? trackHeight
                : Mathf.Max(84f, trackHeight * viewportHeight / contentHeight);
            var progress = maximumScroll <= 0f ? 0f : Mathf.Clamp01(deckCollectionScroll.y / maximumScroll);
            GUI.color = new Color32(92, 122, 116, 235);
            GUI.DrawTexture(new Rect(976f, trackY + (trackHeight - thumbHeight) * progress,
                12f, thumbHeight), Texture2D.whiteTexture);
            GUI.color = previousColor;
        }

        private void DrawCharacterAvatar(string definitionId, CharacterQuality quality,
            AvatarArtContext context, Rect area)
        {
            Sprite avatar;
            if (artCatalog.TryGetCharacterAvatar(definitionId,
                    CharacterQualityResourceId(quality), context, out avatar))
            {
                var spec = AvatarArtContract.GetContextSpec(context);
                if (avatar.texture.width != spec.CanvasWidth || avatar.texture.height != spec.CanvasHeight ||
                    !Mathf.Approximately(area.width, spec.CanvasWidth) ||
                    !Mathf.Approximately(area.height, spec.CanvasHeight))
                {
                    Debug.LogError("Rejected non-native avatar placement: " + definitionId + "|" +
                                   CharacterQualityResourceId(quality) + "|" + context);
                    return;
                }
                GUI.color = Color.white;
                GUI.DrawTexture(area, avatar.texture, ScaleMode.StretchToFill, true);
            }
        }

        private static Rect NativeCharacterAvatarRect(Rect hostArea, AvatarArtContext context)
        {
            var spec = AvatarArtContract.GetContextSpec(context);
            return new Rect(
                Mathf.Round(hostArea.center.x - spec.CanvasWidth * 0.5f),
                Mathf.Round(hostArea.center.y - spec.CanvasHeight * 0.5f),
                spec.CanvasWidth,
                spec.CanvasHeight);
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

        private void OpenCharacterDetail(string characterInstanceId, DeckCharacterDetailMode mode,
            bool returnToDeckEditing)
        {
            OwnedCharacterState owned;
            if (!playerCollection.TryGetByInstanceId(characterInstanceId, out owned)) return;
            selectedEquipmentCharacterInstanceId = owned.CharacterInstanceId;
            detailCharacterInstanceId = owned.CharacterInstanceId;
            detailEquipmentPreviewId = null;
            detailEquipmentSlot = EquipmentSlot.Weapon;
            detailEquipmentPreviewBySlot.Clear();
            detailEquipmentScroll = Vector2.zero;
            deckCharacterDetailMode = mode;
            characterDetailReturnToDeckEditing = returnToDeckEditing;
            characterDetailOpen = true;
        }

        private void DrawCharacterDetailScreen()
        {
            OwnedCharacterState selected;
            if (!playerCollection.TryGetByInstanceId(detailCharacterInstanceId, out selected))
            {
                characterDetailOpen = false;
                characterDetailReturnToDeckEditing = false;
                return;
            }
            var definition = characterCatalog.Get(selected.DefinitionId);
            if (deckCharacterDetailMode == DeckCharacterDetailMode.LibraryBase)
            {
                DrawLibraryBaseCharacterDetail(selected, definition);
                return;
            }
            DrawCharacterEquipmentDetailB(selected, definition);
        }

        private void DrawCharacterEquipmentDetailB(OwnedCharacterState selected,
            CharacterDefinition definition)
        {
            DrawHomeLighthouseCanvas(PrototypeArtResourceIds.HomeLighthouseBackground, false);
            DrawCharacterDetailCShared(PrototypeArtResourceIds.FormationBattleV94BackFrame,
                new Rect(45f, 60f, 94f, 94f), 94, 94);
            DrawCharacterDetailCShared(PrototypeArtResourceIds.FormationBattleV94BackGlyph,
                new Rect(72f, 87f, 40f, 40f), 40, 40);
            if (GUI.Button(new Rect(45f, 60f, 94f, 94f), GUIContent.none, GUIStyle.none))
            {
                CloseDeckCharacterDetail();
                return;
            }
            DrawDeckText(new Rect(165f, 60f, 600f, 72f), T("character.details"), 46,
                ArchiveColor("F4E7C5"), TextAnchor.MiddleLeft, true);
            DrawDeckText(new Rect(167f, 132f, 700f, 36f), T("deck.editCharacterHint"), 18,
                ArchiveColor("A8C5C5"), TextAnchor.MiddleLeft, false);

            DrawCharacterDetailBTexture(CharacterDetailBFormalArtContract.WideSafePanel,
                "module", new Rect(10f, 180f, 1060f, 1900f), 1060, 1900);
            DrawCharacterDetailCModule(CharacterDetailCFormalArtContract.CharacterViewport,
                new Rect(70f, 270f, 520f, 370f), 520, 370);
            DrawCharacterAvatar(selected.DefinitionId, selected.Quality, AvatarArtContext.Detail,
                new Rect(110f, 292f, 440f, 326f));
            DrawDeckText(new Rect(625f, 275f, 345f, 56f),
                CharacterDisplayName(selected.DefinitionId, selected.Quality), 34,
                Color.black, TextAnchor.MiddleCenter, true);

            var currentStats = CalculateDraftDeckCharacterStats(selected, definition);
            DrawCharacterDetailBStatRows(currentStats);
            DrawCharacterDetailBEquippedSlots(selected);
            DrawCharacterDetailBTexture(CharacterDetailBFormalArtContract.SectionDivider,
                "divider", new Rect(208f, 882f, 664f, 20f), 664, 20);
            DrawDeckText(new Rect(92f, 922f, 320f, 68f), T("equipment.pickerTitle"), 30,
                Color.black, TextAnchor.MiddleLeft, true);
            DrawArenaFormalButton(new Rect(430f, 922f, 220f, 68f),
                T("equipment.autoEquip"), false, false);
            DrawCharacterDetailBCandidates(selected);

            EquipmentInstanceState previewTarget;
            var hasTarget = TryGetDetailEquipmentPreview(out previewTarget);
            var afterStats = hasTarget
                ? CalculateEquipmentSelectionPreview(selected, definition, detailEquipmentSlot,
                    previewTarget)
                : currentStats;
            DrawCharacterDetailBSelectionSummary(previewTarget);

            var alreadyEquipped = previewTarget != null && deckEquipmentDraft != null &&
                                  deckEquipmentDraft.IsEquippedTo(previewTarget.EquipmentInstanceId,
                                      selected.CharacterInstanceId);
            var unequipSelection = alreadyEquipped;
            var canConfirm = CanConfirmCharacterDetailCSelection(selected, previewTarget, unequipSelection);
            if (DrawArenaFormalButton(new Rect(345f, 1610f, 390f, 86f),
                    T(unequipSelection ? "equipment.unequip" : "equipment.equip"), canConfirm, true))
            {
                var changed = false;
                if (unequipSelection)
                {
                    var equipped = FindEquippedEquipment(selected.CharacterInstanceId, detailEquipmentSlot);
                    changed = equipped != null && deckEquipmentDraft != null &&
                              deckEquipmentDraft.TryUnequip(equipped, selected.CharacterInstanceId);
                }
                else if (previewTarget != null && deckEquipmentDraft != null)
                {
                    changed = deckEquipmentDraft.TryEquip(previewTarget, selected.CharacterInstanceId,
                        equipmentInventory.Instances);
                }
                if (changed)
                {
                    detailEquipmentPreviewId = null;
                    detailEquipmentPreviewBySlot.Remove(detailEquipmentSlot);
                    deckEditorMessageKey = "deck.unsaved";
                    PlayUiClick(UiAudioCueIds.Confirm, "character-detail-b-draft-equipment");
                }
            }
            var breakdownBaseStats = previewTarget == null
                ? afterStats
                : CalculateEquipmentSelectionPreview(selected, definition, detailEquipmentSlot, null);
            var breakdownModifier = previewTarget == null
                ? null
                : equipmentCatalog.Get(previewTarget.TemplateId).Modifier;
            DrawCharacterDetailBAttributeComposition(currentStats, afterStats,
                breakdownBaseStats, breakdownModifier);

            DrawHomeNavigation(new[]
            {
                ScreenView.Store, ScreenView.Deck, ScreenView.Home, ScreenView.Arena, ScreenView.Harbor
            });
        }

        private void DrawCharacterDetailBTexture(string resourceId, string group, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetCharacterDetailBTexture(resourceId, group);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Character Detail B asset: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawCharacterDetailBStatRows(CombatStatBlock stats)
        {
            var labels = new[] { T("stat.health"), T("stat.attack"), T("stat.armor"), T("detail.attackSpeed") };
            var values = new[]
            {
                stats.MaxHealth.ToString(), stats.AttackDamage.ToString(), stats.Armor.ToString(),
                DetailTideWindowLayout.FormatAttackSpeed(stats.AttackSpeedBasisPoints)
            };
            var icons = new[]
            {
                CharacterDetailCFormalArtContract.StatHealth,
                CharacterDetailCFormalArtContract.StatAttack,
                CharacterDetailCFormalArtContract.StatArmor,
                CharacterDetailCFormalArtContract.StatSpeed
            };
            for (var i = 0; i < labels.Length; i++)
            {
                var y = 350f + i * 62f;
                DrawCharacterDetailCTexture(icons[i], "icon", new Rect(646f, y, 48f, 48f), 48, 48);
                DrawDeckText(new Rect(706f, y, 120f, 48f), labels[i], 20,
                    Color.black, TextAnchor.MiddleCenter, false);
                DrawDeckText(new Rect(875f, y, 100f, 48f), values[i], 23,
                    Color.black, TextAnchor.MiddleCenter, true);
            }
        }

        private void DrawCharacterDetailBEquippedSlots(OwnedCharacterState selected)
        {
            var slots = new[]
            {
                EquipmentSlot.Weapon, EquipmentSlot.Body, EquipmentSlot.Head,
                EquipmentSlot.Hands, EquipmentSlot.Feet
            };
            var x = new[] { 70f, 256f, 442f, 628f, 814f };
            for (var i = 0; i < slots.Length; i++)
            {
                var area = new Rect(x[i], 660f, 184f, 184f);
                var equipped = FindEquippedEquipment(selected.CharacterInstanceId, slots[i]);
                if (equipped == null)
                {
                    DrawCharacterDetailBTexture(CharacterDetailBSlotSupport(slots[i]), "equipment",
                        area, 184, 184);
                }
                else
                {
                    DrawWarehouseEquipmentVisual(area, equipped);
                }
                DrawDeckText(new Rect(area.x, 844f, 184f, 34f), T(EquipmentSlotKey(slots[i])), 18,
                    Color.black, TextAnchor.MiddleCenter, slots[i] == detailEquipmentSlot);
                if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) continue;
                PlayUiClick(UiAudioCueIds.TabSwitch, "character-detail-b-equipped-slot");
                if (!string.IsNullOrEmpty(detailEquipmentPreviewId))
                    detailEquipmentPreviewBySlot[detailEquipmentSlot] = detailEquipmentPreviewId;
                detailEquipmentSlot = slots[i];
                detailEquipmentPreviewId = equipped == null ? null : equipped.EquipmentInstanceId;
                if (string.IsNullOrEmpty(detailEquipmentPreviewId))
                    detailEquipmentPreviewBySlot.Remove(detailEquipmentSlot);
                else
                    detailEquipmentPreviewBySlot[detailEquipmentSlot] = detailEquipmentPreviewId;
            }
        }

        private static string CharacterDetailBSlotSupport(EquipmentSlot slot)
        {
            switch (slot)
            {
                case EquipmentSlot.Weapon:
                    return "ART-UI-WAREHOUSE-A-ITEM-WEAPON-184-v1.0.0";
                case EquipmentSlot.Body:
                    return "ART-UI-WAREHOUSE-A-ITEM-BODY-184-v1.0.0";
                case EquipmentSlot.Head:
                    return "ART-UI-WAREHOUSE-A-ITEM-HEAD-184-v1.0.0";
                case EquipmentSlot.Hands:
                    return "ART-UI-WAREHOUSE-A-ITEM-HANDS-184-v1.0.0";
                default:
                    return "ART-UI-WAREHOUSE-A-ITEM-FEET-184-v1.0.0";
            }
        }

        private void DrawCharacterDetailBCandidates(OwnedCharacterState selected)
        {
            var candidates = FindFormalCharacterDetailCandidates(selected.CharacterInstanceId,
                detailEquipmentSlot);
            EnsureCharacterDetailCSelection(selected.CharacterInstanceId, candidates);
            var visibleCount = Math.Min(4, candidates.Count);
            for (var i = 0; i < 4; i++)
            {
                var area = new Rect(150f + i * 200f, 1000f, 184f, 184f);
                if (i >= visibleCount) continue;
                var candidate = candidates[i];
                DrawWarehouseEquipmentVisual(area, candidate);
                if (!GUI.Button(area, GUIContent.none, GUIStyle.none)) continue;
                PlayUiClick(UiAudioCueIds.Tap, "character-detail-b-equipment-preview");
                detailEquipmentPreviewId = candidate.EquipmentInstanceId;
                detailEquipmentPreviewBySlot[detailEquipmentSlot] = detailEquipmentPreviewId;
            }
        }

        private void DrawCharacterDetailBSelectionSummary(EquipmentInstanceState previewTarget)
        {
            if (previewTarget == null)
            {
                DrawDeckText(new Rect(180f, 1184f, 720f, 48f), T("equipment.noSelection"), 28,
                    Color.black, TextAnchor.MiddleCenter, true);
                DrawDeckText(new Rect(180f, 1232f, 720f, 40f), T("equipment.previewHint"), 17,
                    Color.black, TextAnchor.MiddleCenter, false);
            }
            else
            {
                DrawDeckText(new Rect(180f, 1184f, 720f, 48f), T(previewTarget.TemplateId), 28,
                    Color.black, TextAnchor.MiddleCenter, true);
                DrawDeckText(new Rect(180f, 1232f, 720f, 40f),
                    T("quality." + EquipmentRarityId(previewTarget.Quality)), 21,
                    EquipmentRarityColor(previewTarget.Quality), TextAnchor.MiddleCenter, true);
            }

            DrawDeckText(new Rect(180f, 1276f, 720f, 40f), T("equipment.detail.baseTitle"), 23,
                Color.black, TextAnchor.MiddleCenter, true);
            var baseText = previewTarget == null
                ? "—"
                : EquipmentModifierText(equipmentCatalog.Get(previewTarget.TemplateId).Modifier);
            DrawDeckText(new Rect(180f, 1318f, 720f, 52f), baseText, 24,
                Color.black, TextAnchor.MiddleCenter, true);
            DrawCharacterDetailBTexture(CharacterDetailBFormalArtContract.SectionDivider,
                "divider", new Rect(208f, 1390f, 664f, 20f), 664, 20);
            DrawDeckText(new Rect(180f, 1410f, 720f, 42f), T("equipment.detail.magicTitle"), 23,
                Color.black, TextAnchor.MiddleCenter, true);
            DrawDeckText(new Rect(180f, 1460f, 720f, 80f), T("equipment.noMagicRecorded"), 18,
                Color.black, TextAnchor.MiddleCenter, false);
        }

        private void DrawCharacterDetailBAttributeComposition(CombatStatBlock current,
            CombatStatBlock after, CombatStatBlock breakdownBase,
            EquipmentStatModifier selectedModifier)
        {
            var labels = new[] { T("stat.health"), T("stat.attack"), T("stat.armor"), T("detail.attackSpeed") };
            var afterValues = new[]
            {
                after.MaxHealth, after.AttackDamage, after.Armor, after.AttackSpeedBasisPoints
            };
            var baseValues = new[]
            {
                breakdownBase.MaxHealth, breakdownBase.AttackDamage, breakdownBase.Armor,
                breakdownBase.AttackSpeedBasisPoints
            };
            var equipmentBonuses = selectedModifier == null
                ? new[] { 0, 0, 0, 0 }
                : new[]
                {
                    selectedModifier.MaxHealth, selectedModifier.AttackDamage,
                    selectedModifier.Armor, 0
                };
            var x = new[] { 148f, 348f, 548f, 748f };
            DrawDeckText(new Rect(92f, 1732f, 320f, 48f), T("equipment.after"), 26,
                Color.black, TextAnchor.MiddleLeft, true);
            for (var i = 0; i < 4; i++)
            {
                var area = new Rect(x[i], 1800f, 184f, 88f);
                DrawWarehouseTexture(area, WarehouseAFormalArtContract.TabSelected, "modules");
                DrawDeckText(new Rect(area.x, 1812f, 184f, 28f), labels[i], 17,
                    ArchiveColor("F4E7C5"), TextAnchor.MiddleCenter, false);
                var value = i == 3
                    ? DetailTideWindowLayout.FormatAttackSpeed(afterValues[i])
                    : equipmentBonuses[i] > 0
                        ? baseValues[i] + " + " + equipmentBonuses[i]
                        : afterValues[i].ToString();
                var comparisonColor = afterValues[i] > new[]
                    { current.MaxHealth, current.AttackDamage, current.Armor, current.AttackSpeedBasisPoints }[i]
                    ? ArchiveColor("74B7A8")
                    : ArchiveColor("F4E7C5");
                DrawDeckText(new Rect(area.x, 1840f, 184f, 42f), value, 23,
                    comparisonColor, TextAnchor.MiddleCenter, true);
            }
        }

        private void DrawCharacterDetailCModule(string resourceId, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            DrawCharacterDetailCTexture(resourceId, "module", rect, expectedWidth, expectedHeight);
        }

        private void DrawCharacterDetailCTexture(string resourceId, string group, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetCharacterDetailCTexture(resourceId, group);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Character Detail C asset: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void DrawCharacterDetailCShared(string resourceId, Rect rect,
            int expectedWidth, int expectedHeight)
        {
            var texture = artCatalog.GetFormationBattleV94Texture(resourceId);
            if (texture == null || texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected non-native Character Detail C shared asset: " + resourceId);
                return;
            }
            var previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void EnsureCharacterDetailCSelection(string characterInstanceId,
            IReadOnlyList<EquipmentInstanceState> candidates)
        {
            if (!string.IsNullOrEmpty(detailEquipmentPreviewId) || candidates == null ||
                candidates.Count == 0) return;

            EquipmentInstanceState equipped = null;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                if (deckEquipmentDraft != null && deckEquipmentDraft.IsEquippedTo(
                        candidate.EquipmentInstanceId, characterInstanceId))
                {
                    equipped = candidate;
                    continue;
                }
                detailEquipmentPreviewId = candidate.EquipmentInstanceId;
                detailEquipmentPreviewBySlot[detailEquipmentSlot] = detailEquipmentPreviewId;
                return;
            }

            if (equipped == null) return;
            detailEquipmentPreviewId = equipped.EquipmentInstanceId;
            detailEquipmentPreviewBySlot[detailEquipmentSlot] = detailEquipmentPreviewId;
        }

        private List<EquipmentInstanceState> FindFormalCharacterDetailCandidates(string characterInstanceId,
            EquipmentSlot slot)
        {
            var equipped = new List<EquipmentInstanceState>();
            var available = new List<EquipmentInstanceState>();
            foreach (var instance in equipmentInventory.Instances)
            {
                if (equipmentCatalog.Get(instance.TemplateId).Slot != slot) continue;
                if ((instance.UsageLocks & EquipmentUsageLock.Market) != 0) continue;
                if (deckEquipmentDraft != null && deckEquipmentDraft.IsEquippedTo(
                        instance.EquipmentInstanceId, characterInstanceId))
                {
                    equipped.Add(instance);
                    continue;
                }
                var assigned = deckEquipmentDraft == null
                    ? instance.EquippedCharacterInstanceId
                    : deckEquipmentDraft.GetAssignedCharacterId(instance.EquipmentInstanceId);
                if (string.IsNullOrEmpty(assigned)) available.Add(instance);
            }
            equipped.AddRange(available);
            return equipped;
        }

        private bool TryGetDetailEquipmentPreview(out EquipmentInstanceState target)
        {
            target = null;
            if (string.IsNullOrEmpty(detailEquipmentPreviewId) ||
                detailEquipmentPreviewId == EmptyEquipmentPreviewId) return false;
            try
            {
                target = equipmentInventory.Get(detailEquipmentPreviewId);
                return equipmentCatalog.Get(target.TemplateId).Slot == detailEquipmentSlot;
            }
            catch (KeyNotFoundException)
            {
                detailEquipmentPreviewId = null;
                detailEquipmentPreviewBySlot.Remove(detailEquipmentSlot);
                return false;
            }
        }

        private bool CanConfirmCharacterDetailCSelection(OwnedCharacterState selected,
            EquipmentInstanceState target, bool removeSelection)
        {
            if (deckEquipmentDraft == null) return false;
            if (removeSelection)
                return FindEquippedEquipment(selected.CharacterInstanceId, detailEquipmentSlot) != null;
            if (target == null || (target.UsageLocks & EquipmentUsageLock.Market) != 0) return false;
            var assigned = deckEquipmentDraft.GetAssignedCharacterId(target.EquipmentInstanceId);
            if (string.Equals(assigned, selected.CharacterInstanceId, StringComparison.Ordinal)) return false;
            return string.IsNullOrEmpty(assigned);
        }

        private CombatStatBlock CalculateEquipmentSelectionPreview(OwnedCharacterState selected,
            CharacterDefinition definition, EquipmentSlot slot, EquipmentInstanceState target)
        {
            var modifier = new EquipmentStatModifier();
            foreach (var instance in equipmentInventory.Instances)
            {
                if (deckEquipmentDraft == null || !deckEquipmentDraft.IsEquippedTo(
                        instance.EquipmentInstanceId, selected.CharacterInstanceId)) continue;
                if (equipmentCatalog.Get(instance.TemplateId).Slot == slot) continue;
                modifier = EquipmentStatModifier.Add(modifier,
                    equipmentCatalog.Get(instance.TemplateId).Modifier);
            }
            if (target != null)
                modifier = EquipmentStatModifier.Add(modifier,
                    equipmentCatalog.Get(target.TemplateId).Modifier);
            var rarityStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, selected.Quality,
                OwnedAptitudeRoll(selected),
                selected.ExternalStar);
            return ApplyLanternToStats(EquipmentDerivedStatCalculator.Calculate(rarityStats,
                new EquipmentLoadoutSnapshot(null, modifier)), CurrentLanternSnapshot());
        }

        private void DrawLibraryBaseCharacterDetail(OwnedCharacterState selected,
            CharacterDefinition definition)
        {
            DrawStoreModule(StoreFormalArtContract.BackgroundVeilResourceId,
                new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);
            var fullCanvas = new Rect(0f, 0f, DesignWidth, layoutCanvasHeight);
            GUI.color = ArchiveColor("041623D8");
            GUI.DrawTexture(fullCanvas, Texture2D.whiteTexture);
            GUI.color = Color.white;

            var modalData = DetailTideWindowLayout.CalculateModal(
                DesignWidth, layoutCanvasHeight, layoutSafeTop, layoutSafeBottom);
            var panel = new Rect(modalData.X, modalData.Y, modalData.Width, modalData.Height);
            var shell = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailTideWindowShell);
            if (shell != null) GUI.DrawTexture(panel, shell, ScaleMode.StretchToFill, true);

            var ivory = ArchiveColor("F6E8C8");
            GUI.Label(DetailLocalRect(panel, 190f, 70f, 500f, 72f),
                CharacterDisplayName(selected.DefinitionId, selected.Quality),
                DetailTextStyle(46, ivory, TextAnchor.MiddleCenter, false, FontStyle.Bold));
            DrawDetailTags(panel, selected.DefinitionId, definition, false, ivory);
            var qualityId = CharacterQualityResourceId(selected.Quality);
            var badge = artCatalog.GetUnitLevelBadgeTexture(qualityId, 1);
            if (badge != null)
                GUI.DrawTexture(DetailLocalRect(panel, 290f, 230f, 300f, 88f), badge,
                    ScaleMode.ScaleToFit, true);
            DrawDetailMetadataField(panel, 202f, T(QualityKey(selected.Quality)), ivory);
            DrawDetailMetadataField(panel, 448f, T("collection.notStarred"), ivory);

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(selected.DefinitionId, qualityId,
                    out artDefinition, out sprite))
                DrawDetailCharacterSprite(DetailLocalRect(panel, 45f, 390f, 500f, 580f),
                    artDefinition, sprite, 0.90f);

            var baseStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, selected.Quality,
                OwnedAptitudeRoll(selected), 0);
            DrawDetailStat(panel, new Rect(574f, 393f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatHealth, T("stat.health"),
                baseStats.MaxHealth.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 549f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatAttack, T("stat.attack"),
                baseStats.AttackDamage.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 705f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatArmor, T("stat.armor"),
                baseStats.Armor.ToString(), ivory);
            DrawDetailStat(panel, new Rect(574f, 861f, 250f, 137f),
                PrototypeArtResourceIds.DetailStatSpeed, T("detail.attackSpeed"),
                DetailTideWindowLayout.FormatAttackSpeed(baseStats.AttackSpeedBasisPoints), ivory);
            GUI.Label(DetailLocalRect(panel, 90f, 1078f, 700f, 132f),
                DetailQuote(selected.DefinitionId, qualityId),
                DetailTextStyle(29, ivory, TextAnchor.MiddleCenter, true, FontStyle.Normal));

            var closeTop = DetailLocalRect(panel, 788f, 55f, 48f, 48f);
            var closeTexture = artCatalog.GetDetailTideWindowTexture(PrototypeArtResourceIds.DetailCloseOrnament);
            if (closeTexture != null) GUI.DrawTexture(closeTop, closeTexture, ScaleMode.ScaleToFit, true);
            var closeBottom = DetailLocalRect(panel, 307f, 1261f, 268f, 74f);
            GUI.Label(closeBottom, T("close"), DetailTextStyle(29, ivory,
                TextAnchor.MiddleCenter, false, FontStyle.Normal));
            if (GUI.Button(closeTop, GUIContent.none, GUIStyle.none) ||
                GUI.Button(closeBottom, GUIContent.none, GUIStyle.none))
                CloseDeckCharacterDetail();
        }

        private void CloseDeckCharacterDetail(bool playAudio = true)
        {
            if (playAudio) PlayUiClick(UiAudioCueIds.Cancel, "deck-character-detail-close");
            characterDetailOpen = false;
            characterDetailReturnToDeckEditing = false;
            deckCharacterDetailMode = DeckCharacterDetailMode.LibraryBase;
            detailEquipmentPreviewId = null;
            detailEquipmentSlot = EquipmentSlot.Weapon;
            detailEquipmentPreviewBySlot.Clear();
            detailEquipmentScroll = Vector2.zero;
        }

        private EquipmentInstanceState FindEquippedEquipment(string characterInstanceId, EquipmentSlot slot)
        {
            foreach (var instance in equipmentInventory.Instances)
            {
                var assignedCharacterId = deckEquipmentDraft == null
                    ? instance.EquippedCharacterInstanceId
                    : deckEquipmentDraft.GetAssignedCharacterId(instance.EquipmentInstanceId);
                if (assignedCharacterId == characterInstanceId &&
                    equipmentCatalog.Get(instance.TemplateId).Slot == slot) return instance;
            }
            return null;
        }

        private CombatStatBlock CalculateDraftDeckCharacterStats(
            OwnedCharacterState selected, CharacterDefinition definition)
        {
            var rarityStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, selected.Quality,
                OwnedAptitudeRoll(selected),
                selected.ExternalStar);
            var loadout = deckEquipmentDraft == null
                ? equipmentInventory.CreateSnapshot(selected.CharacterInstanceId)
                : deckEquipmentDraft.CreateSnapshot(selected.CharacterInstanceId,
                    equipmentInventory.Instances);
            return ApplyLanternToStats(
                EquipmentDerivedStatCalculator.Calculate(rarityStats, loadout),
                CurrentLanternSnapshot());
        }

        private static CombatStatBlock ApplyLanternToStats(CombatStatBlock stats,
            LanternSnapshot snapshot)
        {
            return new CombatStatBlock(
                LanternCombatStatRules.ApplyMaximumHealth(stats.MaxHealth, snapshot),
                LanternCombatStatRules.ApplyAttackDamage(stats.AttackDamage, snapshot),
                LanternCombatStatRules.ApplyHealingAmount(stats.HealAmount, snapshot),
                LanternCombatStatRules.ApplyArmor(stats.Armor, snapshot),
                stats.SpeedTier,
                LanternCombatStatRules.ApplyAttackSpeed(stats.AttackSpeedBasisPoints, snapshot));
        }

        private CombatStatBlock CalculateCharacterInstanceStats(
            OwnedCharacterState selected, CharacterDefinition definition)
        {
            var rarityStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, selected.Quality,
                OwnedAptitudeRoll(selected),
                selected.ExternalStar);
            return EquipmentDerivedStatCalculator.Calculate(rarityStats,
                equipmentInventory.CreateSnapshot(selected.CharacterInstanceId));
        }

        private string StatsText(CombatStatBlock stats)
        {
            return T("healthShort") + " " + stats.MaxHealth + " · " +
                   T("attackShort") + " " + stats.AttackDamage + " · " +
                   T("armorShort") + " " + stats.Armor + " · " +
                   T("healShort") + " " + stats.HealAmount + " · " +
                   DetailTideWindowLayout.FormatAttackSpeed(stats.AttackSpeedBasisPoints);
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
            var stats = CalculateCharacterInstanceStats(owned, definition);
            GUI.color = PlayerColor(owned.DefinitionId);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x + 14f, area.y + 18f, area.width - 28f, 42f),
                CharacterDisplayName(owned.DefinitionId, owned.Quality),
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
                PlayUiClick(UiAudioCueIds.Tap, isSelected ? "deck-card-remove" : "deck-card-add");
                if (isSelected) nextRunDeckSelection.TryRemove(owned.DefinitionId);
                else nextRunDeckSelection.TryAdd(owned.DefinitionId);
                var removed = ReconcileDefenseWithActiveDeck();
                if (removed > 0) pendingDefenseRemovedCount += removed;
                SaveGame();
            }
            GUI.enabled = true;
            if (GUI.Button(new Rect(area.x + 182f, area.y + 238f, 100f, 60f), T("equipment.select"),
                    ButtonStyle(UiButtonSemantic.Secondary, 15)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "equipment-character-select");
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
            var rarityStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, definition.BasePower, 1, selected.Quality,
                OwnedAptitudeRoll(selected),
                selected.ExternalStar);
            var derived = EquipmentDerivedStatCalculator.Calculate(rarityStats, snapshot);
            GUI.Label(new Rect(55f, 1430f, 970f, 40f),
                T("equipment.testTitle") + " · " +
                CharacterDisplayName(selected.DefinitionId, selected.Quality),
                Style(24, new Color(0.36f, 0.82f, 0.77f), TextAnchor.MiddleLeft, FontStyle.Normal));
            var derivedText = T("healthShort") + " " + derived.MaxHealth + "  ·  " +
                              T("attackShort") + " " + derived.AttackDamage + "  ·  " +
                              T("armorShort") + " " + derived.Armor + "  ·  " +
                              T("healShort") + " " + derived.HealAmount + "  ·  " +
                              DetailTideWindowLayout.FormatAttackSpeed(derived.AttackSpeedBasisPoints);
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
                    PlayUiClick(UiAudioCueIds.Tap,
                        equippedHere ? "equipment-unequip" : "equipment-equip");
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

        private string CharacterDisplayName(string definitionId, CharacterQuality quality)
        {
            return T(CharacterDisplayNameRules.LocalizationKey(definitionId, quality));
        }

        private static Color QualityColor(CharacterQuality quality)
        {
            switch (quality)
            {
                case CharacterQuality.Uncommon: return new Color32(0x7E, 0xBC, 0x86, 0xFF);
                case CharacterQuality.Rare: return new Color32(0x33, 0x9C, 0xE8, 0xFF);
                case CharacterQuality.Epic: return new Color32(0x9D, 0x5E, 0xCF, 0xFF);
                case CharacterQuality.Legendary: return new Color32(0xE6, 0x87, 0x30, 0xFF);
                default: return new Color32(0x8E, 0x94, 0x97, 0xFF);
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
                    CharacterDisplayName(runUnit.DefinitionId, runUnit.Quality), runUnit.RunLevel,
                    preparationPointerHeld && preparationPointerSlot == slot && preparationPointerDragging ? 0.32f : 1f,
                    CharacterQualityResourceId(runUnit.Quality));
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
            if (GUI.Button(startArea, GUIContent.none, GUIStyle.none))
            {
                StartBattle();
            }
            if (GUI.Button(shopArea, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "preparation-shop-open");
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
                    PlayUiClick(UiAudioCueIds.Tap, "formation-buff-tile");
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
                    PlayUiClick(UiAudioCueIds.Tap, "formation-buff-more");
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
                    PlayUiClick(UiAudioCueIds.Tap, "formation-buff-overflow-row");
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
            var visualArea = NativeCharacterAvatarRect(area, AvatarArtContext.SlotCircle);
            var selected = preparationPointerHeld && preparationPointerSlot == runIndex;
            if (runUnit == null)
            {
                var empty = artCatalog.GetExpeditionUiV67Texture("ART-UI-PREPARATION-SLOT-FRAME-104-V6.7.0");
                GUI.color = WithOpacity(Color.white, opacity);
                if (empty != null) GUI.DrawTexture(visualArea, empty, ScaleMode.StretchToFill, true);
                GUI.color = Color.white;
                return;
            }

            var surface = artCatalog.GetExpeditionUiSharedTexture(AvatarArtContract.OccupiedSlotSurfaceResourceId);
            var rim = artCatalog.GetExpeditionUiSharedTexture(AvatarArtContract.SlotForegroundRimResourceId);
            var mergeMaterial = IsPreparationMergeMaterial(runIndex);
            GUI.color = WithOpacity(selected || mergeMaterial ? new Color(1f, 0.84f, 0.46f) : Color.white, opacity);
            if (surface != null) GUI.DrawTexture(visualArea, surface, ScaleMode.StretchToFill, true);
            DrawCharacterSlotAvatar(runUnit.DefinitionId, runUnit.Quality, visualArea);
            GUI.color = WithOpacity(selected || mergeMaterial ? new Color(1f, 0.84f, 0.46f) : Color.white, opacity);
            if (rim != null) GUI.DrawTexture(visualArea, rim, ScaleMode.StretchToFill, true);
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
            var itemInstanceId = backpackIndex >= 0 &&
                                 backpackIndex < expeditionEconomy.BackpackInstanceIds.Count
                ? expeditionEconomy.BackpackInstanceIds[backpackIndex]
                : string.Empty;
            const int targetSlot = -1;
            var actionId = NewRunEconomyActionId("bind-item");
            var expectedVersion = runEconomyActions.StateVersion;
            var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                return expeditionEconomy.TryActivateBattleItem(backpackIndex, targetSlot)
                    ? RunEconomyResultCode.Success
                    : RunEconomyResultCode.InvalidTarget;
            });
            if (receipt.ResultCode == RunEconomyResultCode.Success)
            {
                RecordFormalItemBinding(actionId, expectedVersion, itemInstanceId);
                PlayUiClick(UiAudioCueIds.Tap, "preparation-item-activate");
                statusKey = itemId == "I04" ? "status.itemActivatedSimplified" : "status.itemActivated";
            }
            else
            {
                PlayUiClick(UiAudioCueIds.Disabled, "preparation-item-activate-failed");
                ApplyRunEconomyStatus(receipt.ResultCode, "status.itemActivated");
            }
            SaveGame();
        }

        private bool DiscardPreparationBattleItem(int backpackIndex)
        {
            var itemInstanceId = backpackIndex >= 0 &&
                                 backpackIndex < expeditionEconomy.BackpackInstanceIds.Count
                ? expeditionEconomy.BackpackInstanceIds[backpackIndex]
                : string.Empty;
            var actionId = NewRunEconomyActionId("discard-item");
            var expectedVersion = runEconomyActions.StateVersion;
            var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                return expeditionEconomy.TryDiscardCombatItemAt(backpackIndex)
                    ? RunEconomyResultCode.Success
                    : RunEconomyResultCode.InvalidTarget;
            });
            if (receipt.ResultCode == RunEconomyResultCode.Success)
            {
                RecordFormalItemDiscard(actionId, expectedVersion, itemInstanceId);
                PlayUiClick(UiAudioCueIds.Cancel, "preparation-item-discard");
                statusKey = "status.itemDiscarded";
            }
            else
            {
                PlayUiClick(UiAudioCueIds.Disabled, "preparation-item-discard-failed");
                ApplyRunEconomyStatus(receipt.ResultCode, "status.itemDiscarded");
            }
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
            if (currentEncounter != null && currentEncounter.GenerationVersion >= 3)
            {
                for (var index = 0; index < currentEncounter.Units.Count; index++)
                    if (currentEncounter.Units[index].EnemySlot == formationSlot) return index;
                return -1;
            }
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
            if (currentEncounter != null && currentEncounter.GenerationVersion >= 3 &&
                enemyIndex >= 0 && enemyIndex < currentEncounter.Units.Count)
                return currentEncounter.Units[enemyIndex].EnemySlot;
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
                    CharacterDisplayName(runUnit.DefinitionId, runUnit.Quality), runUnit.RunLevel,
                    selected && preparationPointerDragging ? 0.32f : 1f,
                    CharacterQualityResourceId(runUnit.Quality));
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
            float opacity,
            string qualityIdOverride = null)
        {
            var battleRect = BattlePresentationLayout.GetUnitSlotRect(isPlayer, slot, layoutContentHeight);
            var stableFootY = battleRect.yMax - 5f;
            var footY = battleRect.yMax - 5f;
            var headTopY = footY - BattlePresentationLayout.DefaultCharacterHeight;
            var visibleCenterX = battleRect.center.x;
            CharacterArtDefinition artDefinition;
            Sprite sprite;
            var qualityId = string.IsNullOrEmpty(qualityIdOverride)
                ? ResolveUnitLevelBadgeQualityId(definitionId, isPlayer)
                : qualityIdOverride;
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
            DrawPieceBase(new Vector2(battleRect.center.x, footY), opacity,
                visibleCharacterWidth, isPlayer);
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
                PieceBaseBottomY(stableFootY, visibleCharacterWidth, isPlayer));
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
                    height = 30f;
                    break;
                case StaticBattleArtTier.Boss:
                    minimumWidth = 280f;
                    maximumWidth = 420f;
                    height = 34f;
                    break;
                default:
                    minimumWidth = 152f;
                    maximumWidth = 280f;
                    height = 26f;
                    break;
            }
            var width = Mathf.Clamp(Mathf.Round(meaningfulVisibleWidth * 1.12f + 20f),
                minimumWidth, maximumWidth);
            width = Mathf.Ceil(width * 0.5f) * 2f;
            return new Vector2(width, height);
        }

        private static int FormationStatusCompartmentCount(int maximumValue)
        {
            return BattleStatusBarRules.SegmentCount(maximumValue);
        }

        private void DrawBattleStatusBarV102(
            Rect area,
            float healthFill,
            float lagHealthFill,
            int currentHealth,
            int maximumHealth,
            float shieldFill,
            int shieldValue,
            int compartmentCount,
            float opacity)
        {
            area = PixelAligned(area);
            var track = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.BattleStatusBarV102Track);
            var healthTexture = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.BattleStatusBarV102HealthFill);
            var damageLagTexture = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.BattleStatusBarV102DamageLagFill);
            var shieldRailTexture = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.BattleStatusBarV102ShieldRail);
            var notchTexture = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.BattleStatusBarV102CapacityNotch);
            var shieldIconTexture = artCatalog.GetFormationBattleV95Texture(
                PrototypeArtResourceIds.BattleStatusBarV102ShieldIcon);
            var oldColor = GUI.color;

            GUI.color = WithOpacity(Color.white, opacity);
            DrawBattleStatusTrack(track, area);

            var safeLeft = area.height * 0.78f;
            var safeArea = new Rect(
                area.x + safeLeft,
                area.y + area.height * 0.28f,
                Mathf.Max(0f, area.width - safeLeft * 2f),
                area.height * 0.47f);
            healthFill = Mathf.Clamp01(healthFill);
            lagHealthFill = Mathf.Clamp01(Mathf.Max(healthFill, lagHealthFill));
            if (lagHealthFill > healthFill + 0.001f)
                DrawClippedStatusFill(damageLagTexture, safeArea, lagHealthFill, opacity);
            DrawClippedStatusFill(healthTexture, safeArea, healthFill, opacity);

            if (notchTexture != null && compartmentCount > 1)
            {
                var notchHeight = safeArea.height * 0.72f;
                var notchWidth = Mathf.Max(1f, notchHeight * notchTexture.width /
                    Mathf.Max(1f, notchTexture.height));
                GUI.color = WithOpacity(Color.white, opacity);
                for (var index = 1; index < compartmentCount; index++)
                {
                    var x = safeArea.x + safeArea.width * index / compartmentCount;
                    GUI.DrawTexture(new Rect(x - notchWidth * 0.5f,
                            safeArea.center.y - notchHeight * 0.5f, notchWidth, notchHeight),
                        notchTexture, ScaleMode.StretchToFill, true);
                }
            }

            var healthFontSize = Mathf.Max(10, Mathf.RoundToInt(area.height * 0.38f));
            var healthTextArea = new Rect(safeArea.x, area.y, safeArea.width, area.height);
            var healthLabel = Mathf.Max(0, currentHealth) + "/" + Mathf.Max(0, maximumHealth);
            GUI.Label(new Rect(healthTextArea.x + 1f, healthTextArea.y + 1f,
                    healthTextArea.width, healthTextArea.height), healthLabel,
                Style(healthFontSize, WithOpacity(new Color(0.02f, 0.05f, 0.07f, 0.84f), opacity),
                    TextAnchor.MiddleCenter, FontStyle.Bold));
            GUI.Label(healthTextArea, healthLabel,
                Style(healthFontSize, WithOpacity(new Color(0.95f, 0.91f, 0.78f), opacity),
                    TextAnchor.MiddleCenter, FontStyle.Bold));

            if (shieldValue > 0 && shieldFill > 0f)
            {
                var railHeight = Mathf.Max(3f, area.height * 0.16f);
                var railArea = new Rect(safeArea.x, area.y - railHeight - 1f,
                    safeArea.width * Mathf.Clamp01(shieldFill), railHeight);
                GUI.color = WithOpacity(Color.white, opacity);
                if (shieldRailTexture != null)
                    GUI.DrawTexture(railArea, shieldRailTexture, ScaleMode.StretchToFill, true);

                var iconSize = Mathf.Max(9f, area.height * 0.32f);
                var shieldFontSize = Mathf.Max(9, Mathf.RoundToInt(area.height * 0.38f));
                var shieldStyle = Style(shieldFontSize,
                    WithOpacity(new Color(0.36f, 0.91f, 0.96f), opacity),
                    TextAnchor.MiddleCenter, FontStyle.Bold);
                var shieldLabel = shieldValue.ToString();
                var shieldTextWidth = Mathf.Max(10f,
                    shieldStyle.CalcSize(new GUIContent(shieldLabel)).x);
                var groupWidth = iconSize + 2f + shieldTextWidth;
                var groupX = area.center.x - groupWidth * 0.5f;
                var centerY = railArea.center.y;
                if (shieldIconTexture != null)
                    GUI.DrawTexture(new Rect(groupX, centerY - iconSize * 0.5f,
                            iconSize, iconSize), shieldIconTexture, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(groupX + iconSize + 2f, centerY - iconSize * 0.6f,
                    shieldTextWidth, iconSize * 1.2f), shieldLabel, shieldStyle);
            }
            GUI.color = oldColor;
        }

        private static void DrawBattleStatusTrack(Texture2D texture, Rect destination)
        {
            if (texture == null || destination.width <= 0f || destination.height <= 0f) return;
            const float sourceCap = 48f;
            var destinationCap = Mathf.Min(destination.width * 0.5f, destination.height * 1.08f);
            var sourceCenterWidth = Mathf.Max(1f, texture.width - sourceCap * 2f);
            var destinationCenterWidth = Mathf.Max(0f, destination.width - destinationCap * 2f);
            DrawAtlasSlice(texture,
                new Rect(destination.x, destination.y, destinationCap, destination.height),
                new Rect(0f, 0f, sourceCap, texture.height));
            DrawAtlasSlice(texture,
                new Rect(destination.x + destinationCap, destination.y,
                    destinationCenterWidth, destination.height),
                new Rect(sourceCap, 0f, sourceCenterWidth, texture.height));
            DrawAtlasSlice(texture,
                new Rect(destination.xMax - destinationCap, destination.y,
                    destinationCap, destination.height),
                new Rect(texture.width - sourceCap, 0f, sourceCap, texture.height));
        }

        private static void DrawClippedStatusFill(
            Texture2D texture, Rect area, float fill, float opacity)
        {
            if (texture == null || fill <= 0f || area.width <= 0f || area.height <= 0f) return;
            var clippedWidth = area.width * Mathf.Clamp01(fill);
            GUI.BeginGroup(new Rect(area.x, area.y, clippedWidth, area.height));
            GUI.color = WithOpacity(Color.white, opacity);
            GUI.DrawTexture(new Rect(0f, 0f, area.width, area.height), texture,
                ScaleMode.StretchToFill, true);
            GUI.EndGroup();
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
                    CharacterQualityResourceId(runUnit.Quality),
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
                CharacterDisplayName(runUnit.DefinitionId, runUnit.Quality),
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
                if (!preparationPointerDragging &&
                    (preparationPointerPosition - preparationPointerDown).sqrMagnitude >= 100f)
                {
                    preparationPointerDragging = true;
                    PlayUiClick(UiAudioCueIds.DragPick, "formation-drag-pick");
                }
                currentEvent.Use();
                return;
            }
            if (currentEvent.type != EventType.MouseUp || !preparationPointerHeld) return;

            preparationPointerPosition = currentEvent.mousePosition;
            var source = preparationPointerSlot;
            var acceptedDrop = false;
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
                var soldUnit = runRoster.Get(source);
                var actionId = NewRunEconomyActionId("sell");
                var expectedVersion = runEconomyActions.StateVersion;
                var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
                {
                    if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                    if (!runRoster.TrySellAt(source, out refund)) return RunEconomyResultCode.UnitNotFound;
                    coins += refund;
                    if (soldUnit != null && expeditionEconomy.PendingPreparationId == "P08")
                    {
                        var improvedRefund = soldUnit.AcquisitionBasis * 3 / 4;
                        var extraRefund = Mathf.Min(6, Mathf.Max(0, improvedRefund - refund));
                        coins += extraRefund;
                        refund += extraRefund;
                        expeditionEconomy.TryConsumePreparation("P08");
                    }
                    return RunEconomyResultCode.Success;
                });
                if (receipt.ResultCode == RunEconomyResultCode.Success)
                {
                    RecordFormalRosterSale(
                        actionId, expectedVersion, soldUnit.RunUnitId);
                    statusKey = refund > 0 ? "status.sold" : "status.soldFree";
                    acceptedDrop = true;
                }
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
                        var thirdIndex = runRoster.FindManualMergeThirdIndex(source, destination);
                        var thirdUnit = thirdIndex >= 0 ? runRoster.Get(thirdIndex) : null;
                        var actionId = NewRunEconomyActionId("merge");
                        var expectedVersion = runEconomyActions.StateVersion;
                        var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
                        {
                            if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                            if (sourceUnit.RunLevel >= RunUnitRules.MaximumRunLevel)
                                return RunEconomyResultCode.MaximumRunLevel;
                            int finalLevel;
                            return runRoster.TryManualMerge(source, destination, out finalLevel)
                                ? RunEconomyResultCode.Success
                                : RunEconomyResultCode.MergeRequirementsNotMet;
                        });
                        if (receipt.ResultCode == RunEconomyResultCode.Success)
                            RecordFormalRosterMerge(
                                actionId, expectedVersion,
                                sourceUnit.RunUnitId, destinationUnit.RunUnitId,
                                thirdUnit.RunUnitId, destinationUnit.RunUnitId);
                        ApplyRunEconomyStatus(receipt.ResultCode, "status.merged");
                        acceptedDrop = receipt.ResultCode == RunEconomyResultCode.Success;
                    }
                    else
                    {
                        var actionId = NewRunEconomyActionId("move");
                        var expectedVersion = runEconomyActions.StateVersion;
                        var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
                        {
                            if (!expeditionFlow.CanPrepare) return RunEconomyResultCode.InvalidPhase;
                            return runRoster.TryMoveOrSwap(source, destination)
                                ? RunEconomyResultCode.Success
                                : RunEconomyResultCode.InvalidTarget;
                        });
                        if (receipt.ResultCode == RunEconomyResultCode.Success)
                            RecordFormalRosterMove(
                                actionId, expectedVersion, sourceUnit.RunUnitId,
                                destination);
                        ApplyRunEconomyStatus(receipt.ResultCode, "status.recruit");
                        acceptedDrop = receipt.ResultCode == RunEconomyResultCode.Success;
                    }
                    SaveGame();
                }
            }
            if (acceptedDrop) PlayUiClick(UiAudioCueIds.DragDrop, "formation-drag-drop");
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
            DrawCharacterAvatar(unit.DefinitionId, unit.Quality, AvatarArtContext.DragGhost, area);
        }

        private void DrawPreparationShopModal()
        {
            var shopBackground = artCatalog.GetExpeditionUiV52Texture(
                "battlefield-signal-yard-preparation-blur-v5.2");
            Texture2D equippedBattlefield;
            var useEquippedBattlefield = TryGetEquippedBattlefieldTexture(out equippedBattlefield);
            var previousMatrix = PushExpeditionUiV53Canvas();
            var canvas = new Rect(0f, 0f, ExpeditionUiV67Layout.ReferenceWidth,
                ExpeditionUiV67Layout.ReferenceHeight);
            // DrawBackground already placed the accepted 1080x2400 equipped battlefield at
            // native size. Do not cover it with the legacy blurred signal-yard background.
            if (!useEquippedBattlefield)
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
            {
                PlayUiClick(UiAudioCueIds.Tap, "recruit-shop-refresh");
                RefreshShopOffers(true);
            }
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
            {
                PlayUiClick(UiAudioCueIds.Tap, "recruit-shop-backpack-open");
                preparationBackpackExpanded = true;
            }
            DrawFormationBottomControl(ExpeditionUiV67Layout.Recycle, string.Empty, false, 1f,
                PrototypeArtResourceIds.FormationBattleV942RecycleIcon);
            if (GUI.Button(ExpeditionUiV67Layout.Recycle, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "recruit-shop-recycle");
                preparationBackpackExpanded = false;
                preparationShopOpen = false;
            }

            DrawV67HorizontalThreeSlice(ExpeditionUiV67Layout.Close,
                "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            GUI.Label(ExpeditionUiV67Layout.Close, T("preparation.closeShop"),
                ArchiveStyle(26, ArchiveColor("F4EBD1"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(ExpeditionUiV67Layout.Close, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "recruit-shop-close");
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
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-diagnostics-toggle");
                runEconomyDiagnosticsOpen = !runEconomyDiagnosticsOpen;
            }
            var audioToggle = new Rect(624f, 286f, 168f, 34f);
            if (GUI.Button(audioToggle,
                    language == GameLanguage.ChineseSimplified ? "音效测试" : "AUDIO TEST",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                audioDiagnosticsOpen = !audioDiagnosticsOpen;
            if (audioDiagnosticsOpen) DrawAudioDiagnostics();
            if (!runEconomyDiagnosticsOpen) return;

            var panel = new Rect(112f, 438f, 617f, 1320f);
            GUI.color = new Color(0.015f, 0.045f, 0.055f, 0.97f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 48f),
                "经营测试 / 远征诊断 v" + RunEconomyRulesVersion,
                Style(19, new Color(0.91f, 0.83f, 0.57f), TextAnchor.MiddleCenter));
            var labels = new[]
            {
                "C01 基础流程", "C02 三目标", "C03 连锁标记", "C04 不补位",
                "C05 可合并", "C06 招募折扣", "C07 背包", "C08 重复/过期事件",
                "C09 六名史诗/传说"
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
            var c01Epic = new Rect(panel.x + 28f, panel.y + 550f, 268f, 64f);
            var c01Legendary = new Rect(panel.x + 321f, panel.y + 550f, 268f, 64f);
            if (GUI.Button(c01Epic, "C01 史诗技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c01-epic");
                ApplyRunEconomyDiagnosticState(9);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(c01Legendary, "C01 传说技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c01-legendary");
                ApplyRunEconomyDiagnosticState(10);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var c02Epic = new Rect(panel.x + 28f, panel.y + 626f, 268f, 64f);
            var c02Legendary = new Rect(panel.x + 321f, panel.y + 626f, 268f, 64f);
            if (GUI.Button(c02Epic, "C02 史诗技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c02-epic");
                ApplyRunEconomyDiagnosticState(11);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(c02Legendary, "C02 传说技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c02-legendary");
                ApplyRunEconomyDiagnosticState(12);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var c03Epic = new Rect(panel.x + 28f, panel.y + 702f, 268f, 64f);
            var c03Legendary = new Rect(panel.x + 321f, panel.y + 702f, 268f, 64f);
            if (GUI.Button(c03Epic, "C03 史诗技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c03-epic");
                ApplyRunEconomyDiagnosticState(13);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(c03Legendary, "C03 传说技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c03-legendary");
                ApplyRunEconomyDiagnosticState(14);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var c04Epic = new Rect(panel.x + 28f, panel.y + 778f, 268f, 64f);
            var c04Legendary = new Rect(panel.x + 321f, panel.y + 778f, 268f, 64f);
            if (GUI.Button(c04Epic, "C04 史诗技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c04-epic");
                ApplyRunEconomyDiagnosticState(15);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(c04Legendary, "C04 传说技能测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c04-legendary");
                ApplyRunEconomyDiagnosticState(16);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var c05Epic = new Rect(panel.x + 28f, panel.y + 850f, 268f, 48f);
            var c05Legendary = new Rect(panel.x + 321f, panel.y + 850f, 268f, 48f);
            if (GUI.Button(c05Epic, "C05 史诗技能测试", ButtonStyle(UiButtonSemantic.Secondary, 12)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c05-epic");
                ApplyRunEconomyDiagnosticState(17);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var c06Epic = new Rect(panel.x + 28f, panel.y + 906f, 268f, 48f);
            var c06Legendary = new Rect(panel.x + 321f, panel.y + 906f, 268f, 48f);
            if (GUI.Button(c06Epic, "C06 史诗技能测试", ButtonStyle(UiButtonSemantic.Secondary, 12)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c06-epic");
                ApplyRunEconomyDiagnosticState(19);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(c06Legendary, "C06 传说技能测试", ButtonStyle(UiButtonSemantic.Secondary, 12)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c06-legendary");
                ApplyRunEconomyDiagnosticState(20);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(c05Legendary, "C05 传说技能测试", ButtonStyle(UiButtonSemantic.Secondary, 12)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-c05-legendary");
                ApplyRunEconomyDiagnosticState(18);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var b1Boss = new Rect(panel.x + 28f, panel.y + 970f, 268f, 52f);
            var b2Boss = new Rect(panel.x + 321f, panel.y + 970f, 268f, 52f);
            if (GUI.Button(b1Boss, "B1 Boss 技能测试", ButtonStyle(UiButtonSemantic.Secondary, 12)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-b1-boss");
                ApplyRunEconomyDiagnosticState(21);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            if (GUI.Button(b2Boss, "B2 Boss 技能测试", ButtonStyle(UiButtonSemantic.Secondary, 12)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "run-economy-b2-boss");
                ApplyRunEconomyDiagnosticState(22);
                runEconomyDiagnosticsOpen = false;
                return;
            }
            var summary = "状态版本=" + runEconomyActions.StateVersion +
                          "  已执行=" + runEconomyActions.Count +
                          "\n最近操作=" + runEconomyActions.LastActionId + " / " + runEconomyActions.LastResultCode;
            if (c01SkillDiagnosticActive)
                summary += "\n当前预设=C01 " + (c01SkillDiagnosticQuality == "legendary" ? "传说" : "史诗") +
                           "（无奖励，不写入正式卡组）";
            if (c02SkillDiagnosticActive)
                summary += "\n当前预设=C02 " + (c02SkillDiagnosticQuality == "legendary" ? "传说" : "史诗") +
                           "（无奖励，不写入正式卡组）";
            if (c03SkillDiagnosticActive)
                summary += "\n当前预设=C03 " + (c03SkillDiagnosticQuality == "legendary" ? "传说" : "史诗") +
                           "（无奖励，不写入正式卡组）";
            if (c04SkillDiagnosticActive)
                summary += "\n当前预设=C04 " + (c04SkillDiagnosticQuality == "legendary" ? "传说" : "史诗") +
                           "（无奖励，不写入正式卡组）";
            if (c05SkillDiagnosticActive)
                summary += "\n当前预设=C05 " + (c05SkillDiagnosticQuality == "legendary" ? "传说" : "史诗") +
                           "（无奖励，不写入正式卡组）";
            if (c06SkillDiagnosticActive)
                summary += "\n当前预设=C06 " + (c06SkillDiagnosticQuality == "legendary" ? "传说" : "史诗") +
                           "（无奖励，不写入正式卡组）";
            if (b1SkillDiagnosticActive) summary += "\n当前预设=B1 Boss（固定 Boss，无奖励）";
            if (b2SkillDiagnosticActive) summary += "\n当前预设=B2 Boss（固定 Boss，无奖励）";
            GUI.Label(new Rect(panel.x + 28f, panel.y + 1036f, panel.width - 56f, 80f), summary,
                Style(14, new Color(0.76f, 0.87f, 0.86f), TextAnchor.UpperLeft));
            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 1140f, 268f, 58f),
                    "复制经营摘要",
                    ButtonStyle(UiButtonSemantic.Reward, 13)))
                GUIUtility.systemCopyBuffer = CreateRunEconomyDiagnosticSummary();
            if (GUI.Button(new Rect(panel.x + 321f, panel.y + 1140f, 268f, 58f),
                    "恢复正常模式",
                    ButtonStyle(UiButtonSemantic.Secondary, 13)))
                RestoreNormalModeAfterC01Diagnostic();
        }

        private void DrawAudioDiagnostics()
        {
            var panel = new Rect(112f, 438f, 617f, 1050f);
            GUI.color = new Color(0.015f, 0.045f, 0.055f, 0.98f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(panel.x + 24f, panel.y + 20f, panel.width - 48f, 48f),
                 "音效占位测试 " + ClientVersionLabel,
                Style(19, new Color(0.91f, 0.83f, 0.57f), TextAnchor.MiddleCenter));
            var uiIds = new[] { UiAudioCueIds.Tap, UiAudioCueIds.Confirm, UiAudioCueIds.Cancel,
                UiAudioCueIds.Disabled, UiAudioCueIds.Error, UiAudioCueIds.Pause, UiAudioCueIds.Skip,
                UiAudioCueIds.SpeedUp, UiAudioCueIds.AutoToggle, UiAudioCueIds.Purchase };
            for (var index = 0; index < uiIds.Length; index++)
            {
                var column = index % 2;
                var row = index / 2;
                var area = new Rect(panel.x + 28f + column * 292f, panel.y + 86f + row * 62f,
                    268f, 48f);
                if (GUI.Button(area, uiIds[index], ButtonStyle(UiButtonSemantic.Secondary, 12)))
                    PlayUiClick(uiIds[index], "diagnostic-ui-" + index);
            }
            var eventIds = new[] { "C01", "C02", "C03", "C04", "C05" };
            for (var index = 0; index < eventIds.Length; index++)
            {
                var area = new Rect(panel.x + 28f + (index % 2) * 292f,
                    panel.y + 420f + (index / 2) * 62f, 268f, 48f);
                if (GUI.Button(area, eventIds[index] + " 事件占位", ButtonStyle(UiButtonSemantic.Reward, 12)))
                    battleAudioRouter.Consume(CreateAudioDiagnosticEvent(eventIds[index]));
            }
            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 610f, 268f, 48f),
                    "静音切换", ButtonStyle(UiButtonSemantic.Secondary, 12)))
                uiAudioRouter.SetMuted(!uiAudioRouter.Mixer.Muted);
            if (GUI.Button(new Rect(panel.x + 321f, panel.y + 610f, 268f, 48f),
                    "跳过并收束", ButtonStyle(UiButtonSemantic.Secondary, 12)))
                battleAudioRouter.SkipAndFlush("audio-diagnostic");
            var summary = "战斗音效=" + (battleAudioRouter.Enabled ? "启用" : "关闭") +
                          "  UI音效=" + (uiAudioRouter.Enabled ? "启用" : "关闭") +
                          "  Fallback=是\n" +
                          "重复抑制=" + battleAudioRouter.DuplicateCount +
                          "  并发抑制=" + battleAudioRouter.SuppressedCount +
                          "  UI拒绝=" + uiAudioRouter.RejectedCount + "\n" +
                          "主音量=" + uiAudioRouter.Mixer.Master.ToString("0.00") +
                          "  音乐=" + uiAudioRouter.Mixer.Music.ToString("0.00") +
                          "  战斗=" + uiAudioRouter.Mixer.Battle.ToString("0.00") +
                          "  界面=" + uiAudioRouter.Mixer.Ui.ToString("0.00") + "\n" +
                          "主点击播放=" + (uiMainClickAudioPlayer == null ? 0 :
                              uiMainClickAudioPlayer.SuccessfulPlayCount) +
                          "  缺资源回退=" + (uiMainClickAudioPlayer == null ? 0 :
                              uiMainClickAudioPlayer.FallbackCount);
            GUI.Label(new Rect(panel.x + 28f, panel.y + 700f, panel.width - 56f, 150f), summary,
                Style(14, new Color(0.76f, 0.87f, 0.86f), TextAnchor.UpperLeft));
            if (GUI.Button(new Rect(panel.x + 28f, panel.y + 996f, 268f, 58f),
                    "关闭音效测试", ButtonStyle(UiButtonSemantic.Secondary, 13)))
                audioDiagnosticsOpen = false;
        }

        private CardBattleEvent CreateAudioDiagnosticEvent(string family)
        {
            audioDiagnosticSequence++;
            var abilityId = family == "C01" ? "S_OLIN_OPEN_DAWN" :
                family == "C02" ? "S_YIWEN_RETURN_BEACON" :
                family == "C03" ? "S_LEIF_WHITE_TRAIL" :
                family == "C04" ? "S_MILA_HIDDEN_SPRING" : "S_C05_BARRIER";
            return new CardBattleEvent
            {
                Kind = CardBattleEventKind.ActionStarted,
                BattleId = "audio-diagnostic",
                EventSequence = audioDiagnosticSequence,
                ActionId = "audio-diagnostic",
                AbilityId = abilityId,
                SourceId = "diagnostic-source",
                AuthorityTick = audioDiagnosticSequence,
                Outcome = CardBattleActionOutcome.Completed
            };
        }

        private bool PlayUiClick(string cueId, string interactionId)
        {
            audioDiagnosticSequence++;
            if (uiMainClickAudioPlayer == null)
                uiMainClickAudioPlayer = UiMainClickAudioPlayer.CreateOrGet(uiAudioRouter);
            return uiMainClickAudioPlayer.TryPlay(cueId, interactionId, Time.frameCount);
        }

        private void RestoreNormalModeAfterC01Diagnostic()
        {
            c01SkillDiagnosticActive = false;
            c01SkillDiagnosticQuality = null;
            c02SkillDiagnosticActive = false;
            c02SkillDiagnosticQuality = null;
            c03SkillDiagnosticActive = false;
            c03SkillDiagnosticQuality = null;
            c04SkillDiagnosticActive = false;
            c04SkillDiagnosticQuality = null;
            c05SkillDiagnosticActive = false;
            c05SkillDiagnosticQuality = null;
            c06SkillDiagnosticActive = false;
            c06SkillDiagnosticQuality = null;
            b1SkillDiagnosticActive = false;
            b2SkillDiagnosticActive = false;
            diagnosticQualityOverrides.Clear();
            runEconomyDiagnosticsOpen = false;
            roguelikeBuildDiagnosticsOpen = false;
            roguelikeDiagnosticStateIndex = 0;
            roguelikeDiagnosticSuppressChoiceSet = false;
            if (!LoadGame())
            {
                statusKey = "status.recruit";
                return;
            }
            if (!string.IsNullOrEmpty(runId) && runId.StartsWith("diag-C", StringComparison.OrdinalIgnoreCase))
            {
                NormalizeDiagnosticRosterToCommonQuality();
                runId = StableRunEconomyId("run", expeditionMapSeed.ToString(), "restored-normal");
                SaveGame();
            }
            statusKey = "status.recruit";
        }

        private void NormalizeDiagnosticRosterToCommonQuality()
        {
            for (var index = 0; index < runRoster.TotalCapacity; index++)
            {
                var current = runRoster.Get(index);
                if (current == null) continue;
                var sourceBusinessId = string.IsNullOrEmpty(current.SourceBusinessId)
                    ? current.RunUnitId : current.SourceBusinessId;
                var normalized = new RunUnitState(current.DefinitionId, current.BasePower,
                    current.RunLevel, current.AcquisitionBasis, current.RunUnitId,
                    CharacterQuality.Common,
                    CharacterRarityInstanceRules.ComputeAptitudeRoll(sourceBusinessId),
                    sourceBusinessId, current.ExternalStar);
                runRoster.ReplaceAt(index, normalized);
            }
        }

        private void ApplyRunEconomyDiagnosticState(int stateIndex)
        {
            runRoster.Clear();
            diagnosticQualityOverrides.Clear();
            c01SkillDiagnosticActive = false;
            c01SkillDiagnosticQuality = null;
            c02SkillDiagnosticActive = false;
            c02SkillDiagnosticQuality = null;
            c03SkillDiagnosticActive = false;
            c03SkillDiagnosticQuality = null;
            c04SkillDiagnosticActive = false;
            c04SkillDiagnosticQuality = null;
            c05SkillDiagnosticActive = false;
            c05SkillDiagnosticQuality = null;
            c06SkillDiagnosticActive = false;
            c06SkillDiagnosticQuality = null;
            b1SkillDiagnosticActive = false;
            b2SkillDiagnosticActive = false;
            roguelikeBuildDiagnosticsOpen = false;
            roguelikeDiagnosticStateIndex = 0;
            roguelikeDiagnosticSuppressChoiceSet = false;
            coins = 10;
            expeditionEconomy.Reset();
            runEconomyActions.Reset();
            roguelikeBuild.Reset();
            runAttackBonus = 0;
            runHealthBonus = 0;
            currentExpeditionRewardsEnabled = false;
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
                    diagnosticQualityOverrides[ids[index]] = qualityIds[index];
                    AddDiagnosticUnit(index, ids[index], 1, 0);
                }
            }
            else if (stateIndex == 9 || stateIndex == 10)
            {
                c01SkillDiagnosticActive = true;
                c01SkillDiagnosticQuality = stateIndex == 10 ? "legendary" : "epic";
                diagnosticQualityOverrides["unit.guardian"] = c01SkillDiagnosticQuality;
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
                AddDiagnosticUnit(1, "unit.healer", 1, 0);
                AddDiagnosticUnit(2, "unit.thornGuard", 1, 0);
            }
            else if (stateIndex == 11 || stateIndex == 12)
            {
                c02SkillDiagnosticActive = true;
                c02SkillDiagnosticQuality = stateIndex == 12 ? "legendary" : "epic";
                diagnosticQualityOverrides["unit.healer"] = c02SkillDiagnosticQuality;
                AddDiagnosticUnit(0, "unit.healer", 1, 0);
                AddDiagnosticUnit(1, "unit.guardian", 1, 0);
                AddDiagnosticUnit(2, "unit.thornGuard", 1, 0);
            }
            else if (stateIndex == 13 || stateIndex == 14)
            {
                c03SkillDiagnosticActive = true;
                c03SkillDiagnosticQuality = stateIndex == 14 ? "legendary" : "epic";
                diagnosticQualityOverrides["unit.thornGuard"] = c03SkillDiagnosticQuality;
                AddDiagnosticUnit(0, "unit.thornGuard", 1, 0);
                AddDiagnosticUnit(1, "unit.guardian", 1, 0);
                AddDiagnosticUnit(2, "unit.healer", 1, 0);
            }
            else if (stateIndex == 15 || stateIndex == 16)
            {
                c04SkillDiagnosticActive = true;
                c04SkillDiagnosticQuality = stateIndex == 16 ? "legendary" : "epic";
                diagnosticQualityOverrides["unit.archer"] = c04SkillDiagnosticQuality;
                AddDiagnosticUnit(0, "unit.archer", 1, 0);
                AddDiagnosticUnit(1, "unit.healer", 1, 0);
                AddDiagnosticUnit(2, "unit.guardian", 1, 0);
            }
            else if (stateIndex == 17 || stateIndex == 18)
            {
                c05SkillDiagnosticActive = true;
                c05SkillDiagnosticQuality = stateIndex == 18 ? "legendary" : "epic";
                diagnosticQualityOverrides["unit.starblade"] = c05SkillDiagnosticQuality;
                AddDiagnosticUnit(0, "unit.starblade", 1, 0);
                AddDiagnosticUnit(1, "unit.guardian", 1, 0);
                AddDiagnosticUnit(2, "unit.healer", 1, 0);
            }
            else if (stateIndex == 19 || stateIndex == 20)
            {
                c06SkillDiagnosticActive = true;
                c06SkillDiagnosticQuality = stateIndex == 20 ? "legendary" : "epic";
                diagnosticQualityOverrides["unit.starpriest"] = c06SkillDiagnosticQuality;
                AddDiagnosticUnit(0, "unit.starpriest", 1, 0);
                AddDiagnosticUnit(1, "unit.guardian", 1, 0);
                AddDiagnosticUnit(2, "unit.thornGuard", 1, 0);
            }
            else if (stateIndex == 21 || stateIndex == 22)
            {
                coins = 999;
                b1SkillDiagnosticActive = stateIndex == 21;
                b2SkillDiagnosticActive = stateIndex == 22;
                AddDiagnosticUnit(0, "unit.guardian", 1, 0);
                AddDiagnosticUnit(1, "unit.healer", 1, 0);
                AddDiagnosticUnit(2, "unit.thornGuard", 1, 0);
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
            if (b1SkillDiagnosticActive || b2SkillDiagnosticActive)
                CreateEnemies();
            statusKey = "status.recruit";
            if (!SkillDiagnosticActive) SaveGame();
        }

        private void AddDiagnosticUnit(int slot, string definitionId, int runLevel, int acquisitionBasis)
        {
            var id = StableRunEconomyId("unit", runId, slot.ToString(), definitionId,
                runLevel.ToString());
            var quality = ResolveDiagnosticQuality(definitionId);
            var sourceBusinessId = id;
            var aptitudeRoll = CharacterRarityInstanceRules.ComputeAptitudeRoll(sourceBusinessId);
            OwnedCharacterState lockedCharacter;
            if (SkillDiagnosticActive && TryGetLockedRunCharacter(definitionId, out lockedCharacter))
            {
                sourceBusinessId = lockedCharacter.CharacterInstanceId;
                aptitudeRoll = CharacterRarityInstanceRules.ComputeAptitudeRoll(sourceBusinessId);
            }
            runRoster.TryRestoreAt(slot, new RunUnitState(definitionId,
                characterCatalog.Get(definitionId).BasePower, runLevel, acquisitionBasis, id,
                quality, aptitudeRoll, sourceBusinessId));
        }

        private CharacterQuality ResolveDiagnosticQuality(string definitionId)
        {
            string qualityId;
            if (!diagnosticQualityOverrides.TryGetValue(definitionId, out qualityId))
                return CharacterQuality.Common;
            switch ((qualityId ?? string.Empty).ToLowerInvariant())
            {
                case "uncommon": return CharacterQuality.Uncommon;
                case "rare": return CharacterQuality.Rare;
                case "epic": return CharacterQuality.Epic;
                case "legendary": return CharacterQuality.Legendary;
                default: return CharacterQuality.Common;
            }
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
            {
                PlayUiClick(UiAudioCueIds.Cancel, "backpack-collapse");
                preparationBackpackExpanded = false;
            }

            DrawV67HorizontalThreeSlice(ExpeditionUiV67Layout.Close,
                "ART-UI-PREPARATION-PRIMARY-BUTTON-9SLICE-V6.7.0", 28f, 28f);
            GUI.Label(ExpeditionUiV67Layout.Close, T("preparation.backToRecruit"),
                ArchiveStyle(26, ArchiveColor("F4EBD1"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(ExpeditionUiV67Layout.Close, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "backpack-back-to-recruit");
                preparationBackpackExpanded = false;
            }
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
            var cost = ShopOfferListedPrice(offerIndex, nameKey, level);
            var power = UnitBasePower(nameKey);
            var definition = characterCatalog.Get(nameKey);
            CharacterQuality quality;
            var stats = CalculateShopOfferStats(nameKey, power, level, out quality);
            var payableCost = expeditionEconomy.QuoteRecruitmentPrice(cost);
            var canBuy = coins >= payableCost && runRoster.CanPurchase(
                new RunUnitState(nameKey, power, level, payableCost));

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(nameKey, CharacterQualityResourceId(quality),
                    out artDefinition, out sprite))
            {
                var characterArea = new Rect(area.x + 18f, area.y + 16f, 174f, 180f);
                characterArea.y += Mathf.Sin(Time.unscaledTime * 1.8f + offerIndex * 1.7f) * 2f;
                DrawPreparationOfferCharacterSprite(characterArea, artDefinition, sprite);
            }

            var textX = area.x + 205f;
            GUI.Label(new Rect(textX, area.y + 25f, 370f, 40f),
                CharacterDisplayName(nameKey, quality) + "  Lv." + level,
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
            {
                Recruit(offerIndex, nameKey, cost, power, level);
            }
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
                PlayUiClick(UiAudioCueIds.Cancel, "preparation-v67-back");
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
                PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-v67");
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
            var visualArea = NativeCharacterAvatarRect(area, AvatarArtContext.SlotCircle);
            if (runUnit == null)
            {
                DrawV67Texture(visualArea, "ART-UI-PREPARATION-SLOT-FRAME-104-V6.7.0",
                    ScaleMode.StretchToFill);
                return;
            }

            DrawSharedExpeditionTexture(visualArea, AvatarArtContract.OccupiedSlotSurfaceResourceId);
            DrawRosterAvatarBelowForegroundFrame(visualArea, runUnit);
            DrawSharedExpeditionTexture(visualArea, AvatarArtContract.SlotForegroundRimResourceId);
        }

        private void DrawRosterAvatarBelowForegroundFrame(Rect area, RunUnitState runUnit)
        {
            if (runUnit == null) return;
            DrawCharacterSlotAvatar(runUnit.DefinitionId, runUnit.Quality, area);
        }

        private void DrawCharacterSlotAvatar(string definitionId, CharacterQuality quality, Rect area)
        {
            DrawCharacterAvatar(definitionId, quality, AvatarArtContext.SlotCircle, area);
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
                PlayUiClick(UiAudioCueIds.Cancel, "preparation-v53-back");
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
            PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-map-style");
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
            var cost = ShopOfferListedPrice(offerIndex, nameKey, level);
            var power = UnitBasePower(nameKey);
            var definition = characterCatalog.Get(nameKey);
            CharacterQuality quality;
            var stats = CalculateShopOfferStats(nameKey, power, level, out quality);
            CharacterArtDefinition artDefinition;
            Sprite sprite;
            if (artCatalog.TryGetCharacterArt(nameKey, CharacterQualityResourceId(quality),
                    out artDefinition, out sprite))
            {
                var sway = Mathf.Sin(Time.unscaledTime * 1.8f + offerIndex * 1.7f) * 3f;
                var characterArea = OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferCharacterArt);
                characterArea.x += sway;
                DrawFullCharacterSprite(characterArea,
                    artDefinition, sprite, 1f);
            }
            GUI.Label(OffsetRect(area, ExpeditionUiV46Layout.PreparationOfferName),
                CharacterDisplayName(nameKey, quality) + "  Lv." + level,
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
                PlayUiClick(UiAudioCueIds.Cancel, "preparation-v46-back");
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
                PlayUiClick(UiAudioCueIds.Cancel, "upgrade-v46-back");
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
            PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-v46");
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
            if (!DrawEquippedBattlefieldSkin(0.78f) && blurredBattlefield != null)
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
            var detailDisplayName = preparationDetailIsMonster
                ? T(definitionId)
                : CharacterDisplayName(definitionId, runUnit.Quality);
            GUI.Label(DetailLocalRect(panel, 190f, 70f, 500f, 72f), detailDisplayName,
                DetailTextStyle(46, ivory, TextAnchor.MiddleCenter, false, FontStyle.Bold));

            CharacterDefinition characterDefinition = null;
            try { characterDefinition = characterCatalog.Get(definitionId); }
            catch (KeyNotFoundException) { }
            DrawDetailTags(panel, definitionId, characterDefinition, preparationDetailIsMonster, ivory);

            var qualityId = preparationDetailIsMonster
                ? ResolveUnitLevelBadgeQualityId(definitionId, false)
                : CharacterQualityResourceId(runUnit.Quality);
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
                DetailTideWindowLayout.FormatAttackSpeed(unit.AttackSpeedBasisPoints), ivory);

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
            {
                PlayUiClick(UiAudioCueIds.Cancel, "unit-detail-close");
                ClosePreparationUnitDetail();
            }
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
            var displayName = CharacterDisplayName(unit.NameKey, unit.Quality);
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
            if (string.Equals(effect.ProjectileProfileId,
                    BattleProjectileProfileIds.BossLampBeam, StringComparison.Ordinal))
            {
                DrawBattleLampBeam(effect);
                return;
            }
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
            var useApprovedAlphaBounds = string.Equals(profileId, BattleProjectileProfileIds.FireShare,
                    StringComparison.Ordinal) ||
                string.Equals(profileId, BattleProjectileProfileIds.FireCauldron,
                    StringComparison.Ordinal);
            var approvedUv = useApprovedAlphaBounds
                ? BattleProjectileProfiles.FeedbackVisibleUvFor(profileId)
                : new Rect(textureRect.x / sprite.texture.width, textureRect.y / sprite.texture.height,
                    textureRect.width / sprite.texture.width, textureRect.height / sprite.texture.height);
            var sourceWidth = useApprovedAlphaBounds
                ? Mathf.Max(1f, approvedUv.width * sprite.texture.width)
                : Mathf.Max(1f, sprite.rect.width);
            var sourceHeight = useApprovedAlphaBounds
                ? Mathf.Max(1f, approvedUv.height * sprite.texture.height)
                : Mathf.Max(1f, sprite.rect.height);
            var width = BattleProjectileProfiles.DisplayWidthFor(profileId);
            var height = width * sourceHeight / sourceWidth;
            var contentScaleX = useApprovedAlphaBounds ? 1f : Mathf.Clamp01(textureRect.width / sourceWidth);
            var contentScaleY = useApprovedAlphaBounds ? 1f : Mathf.Clamp01(textureRect.height / sourceHeight);
            var displayWidth = width * contentScaleX;
            var displayHeight = height * contentScaleY;
            var trimmedPivotX = textureRect.width <= 0f ? 0.5f :
                Mathf.Clamp01((sprite.pivot.x - (textureRect.x - sprite.rect.x)) / textureRect.width);
            var visualCenter = point - direction * (displayWidth * (trimmedPivotX - 0.50f));
            var uv = approvedUv;
            var oldMatrix = GUI.matrix;
            var screenCenter3 = oldMatrix.MultiplyPoint3x4(
                new Vector3(visualCenter.x, visualCenter.y, 0f));
            var screenScaleX = oldMatrix.MultiplyVector(Vector3.right).magnitude;
            var screenScaleY = oldMatrix.MultiplyVector(Vector3.up).magnitude;
            var screenCenter = new Vector2(screenCenter3.x, screenCenter3.y);
            var screenRect = new Rect(
                screenCenter.x - displayWidth * screenScaleX * 0.5f,
                screenCenter.y - displayHeight * screenScaleY * 0.5f,
                displayWidth * screenScaleX,
                displayHeight * screenScaleY);
            GUI.matrix = Matrix4x4.identity;
            GUI.color = WithOpacity(Color.white, battleSceneOpacity);
            GUIUtility.RotateAroundPivot(angle, screenCenter);
            GUI.DrawTextureWithTexCoords(screenRect, sprite.texture, uv, true);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
            return true;
        }

        private void DrawBattleFeedback(bool healBehindUnits)
        {
            foreach (var feedback in battleFeedbackVisuals)
            {
                if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C03WhiteMark,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E2TrackJudgment,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E1SteadyLine,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.B2DeviantLight,
                        StringComparison.Ordinal)) continue;
                var isBetweenBaseAndUnit = IsBetweenBaseAndUnitFeedbackProfile(feedback.ProfileId);
                if (isBetweenBaseAndUnit != healBehindUnits) continue;
                DrawBattleFeedbackVisual(feedback);
            }
            GUI.color = Color.white;
        }

        private void DrawBattleLampBeam(BattleEffectInstance effect)
        {
            if (effect.Progress >= 1f) return;
            var sprite = LoadBattleVfxSprite(BattleProjectileProfiles.ResourcePathFor(
                BattleProjectileProfileIds.BossLampBeam));
            if (sprite == null || sprite.texture == null) return;
            var oldMatrix = GUI.matrix;
            var screenStart3 = oldMatrix.MultiplyPoint3x4(
                new Vector3(effect.OriginPosition.x, effect.OriginPosition.y, 0f));
            var screenEnd3 = oldMatrix.MultiplyPoint3x4(
                new Vector3(effect.TargetPosition.x, effect.TargetPosition.y, 0f));
            var screenStart = new Vector2(screenStart3.x, screenStart3.y);
            var screenEnd = new Vector2(screenEnd3.x, screenEnd3.y);
            var delta = screenEnd - screenStart;
            var distance = delta.magnitude;
            if (distance <= 0.01f) return;
            var uv = BattleProjectileProfiles.FeedbackVisibleUvFor(
                BattleProjectileProfileIds.BossLampBeam);
            const float beamThickness = 32f;
            var height = beamThickness;
            var center = (screenStart + screenEnd) * 0.5f;
            var rect = new Rect(center.x - distance * 0.5f, center.y - height * 0.5f,
                distance, height);
            var alpha = Mathf.Lerp(0.82f, 1f,
                (Mathf.Sin(effect.Progress * Mathf.PI) + 1f) * 0.5f);
            GUI.matrix = Matrix4x4.identity;
            GUI.color = WithOpacity(Color.white, battleSceneOpacity * alpha);
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, center);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
        }

        private void DrawBattleFeedbackForUnit(string unitId)
        {
            foreach (var feedback in battleFeedbackVisuals)
            {
                if (!string.Equals(feedback.TargetId, unitId, StringComparison.Ordinal) ||
                    !IsBetweenBaseAndUnitFeedbackProfile(feedback.ProfileId) ||
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
                if (!string.Equals(feedback.TargetId, unitId, StringComparison.Ordinal)) continue;
                var isForegroundUnitFeedback = string.Equals(feedback.ProfileId,
                    BattleFeedbackProfileIds.HealFeedbackRise, StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C03WhiteMark,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.N1HoldGateStamp,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E2TrackJudgment,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E1SteadyLine,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.B2DeviantLight,
                        StringComparison.Ordinal);
                if (!isForegroundUnitFeedback) continue;
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
            if (HasExclusiveSkillShieldFeedback(combatUnit.Id)) return;
            if (overlayOpacity <= 0.001f) return;
            var sprite = LoadBattleVfxSprite(
                BattleProjectileProfiles.ShieldOverlayGenericResourcePath);
            if (sprite == null || sprite.texture == null) return;
            var visibleUv = BattleProjectileProfiles.FeedbackVisibleUvFor(
                BattleFeedbackProfileIds.ShieldOverlayGeneric);
            var contentAspect = (visibleUv.width * sprite.texture.width) /
                Mathf.Max(0.01f, visibleUv.height * sprite.texture.height);
            var shieldDisplayScale = string.Equals(combatUnit.DefinitionId, "enemy.n1.guard",
                StringComparison.Ordinal) ? 0.8f : 1f;
            var width = visibleRect.width * shieldDisplayScale;
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

        private bool HasExclusiveSkillShieldFeedback(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return false;
            foreach (var feedback in battleFeedbackVisuals)
            {
                if (!string.Equals(feedback.TargetId, targetId, StringComparison.Ordinal) ||
                    feedback.FadingOut) continue;
                if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.N1HoldGateStamp,
                        StringComparison.Ordinal) ||
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E1SteadyLine,
                        StringComparison.Ordinal)) return true;
            }
            return false;
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

        private static bool IsBetweenBaseAndUnitFeedbackProfile(string profileId)
        {
            return IsHealFeedbackProfile(profileId) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.OpenDawn,
                        StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.OlinTide,
                        StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.ReturnBeaconRing,
                         StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.C04AfterSpringRelease,
                          StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                           StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.C06ReturnFireBuff,
                            StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.E3BellReturn,
                            StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.B1BlockadeQuake,
                            StringComparison.Ordinal) ||
                   string.Equals(profileId, BattleFeedbackProfileIds.B2RecallLost,
                            StringComparison.Ordinal);
        }

        private void DrawBattleFeedbackVisual(BattleFeedbackVisual feedback)
        {
            if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.OlinTide,
                    StringComparison.Ordinal))
            {
                var tideTarget = battlePresentation.FindView(feedback.TargetId);
                if (tideTarget != null && tideTarget.State != null)
                {
                    feedback.Position = BattleBaseCenterFor(tideTarget);
                    feedback.Width = BattleBaseWidthFor(tideTarget.State) * 1.1f;
                }
            }
            else if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C03WhiteMark,
                          StringComparison.Ordinal))
            {
                var markTarget = battlePresentation.FindView(feedback.TargetId);
                if (markTarget != null && markTarget.State != null)
                {
                    feedback.Position = markTarget.GetAnchor(BattlePresentationIds.BodyAnchor);
                    feedback.Width = C03WhiteMarkWidthFor(markTarget.State);
                }
            }
            else if (string.Equals(feedback.ProfileId,
                         BattleFeedbackProfileIds.B2DeviantLight,
                         StringComparison.Ordinal))
            {
                var markTarget = battlePresentation.FindView(feedback.TargetId);
                if (markTarget != null && markTarget.State != null)
                {
                    feedback.Position = markTarget.GetAnchor(BattlePresentationIds.BodyAnchor) +
                        new Vector2(46f, -48f);
                    feedback.Width = BattleBaseWidthFor(markTarget.State) * 0.21f;
                }
            }
            else if (string.Equals(feedback.ProfileId,
                         BattleFeedbackProfileIds.C04AfterSpringRelease,
                         StringComparison.Ordinal))
            {
                var releaseTarget = battlePresentation.FindView(feedback.TargetId);
                if (releaseTarget != null && releaseTarget.State != null)
                {
                    feedback.Position = BattleBaseCenterFor(releaseTarget);
                    feedback.Width = BattleBaseWidthFor(releaseTarget.State) * 1.1f;
                }
            }
            else if (string.Equals(feedback.ProfileId,
                         BattleFeedbackProfileIds.N1HoldGateStamp,
                         StringComparison.Ordinal))
            {
                var n1Target = battlePresentation.FindView(feedback.TargetId);
                if (n1Target != null && n1Target.State != null)
                {
                    feedback.Position = n1Target.GetAnchor(BattlePresentationIds.BodyAnchor);
                    feedback.Width = BattleBaseWidthFor(n1Target.State) *
                        BattleProjectileProfiles.N1HoldGateStampDisplayScale * 0.78f;
                }
            }
            else if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E1SteadyLine,
                         StringComparison.Ordinal) ||
                     string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E3BellReturn,
                         StringComparison.Ordinal))
            {
                var auraTarget = battlePresentation.FindView(feedback.TargetId);
                if (auraTarget != null && auraTarget.State != null)
                {
                    var isGroundAura = string.Equals(feedback.ProfileId,
                        BattleFeedbackProfileIds.E3BellReturn, StringComparison.Ordinal);
                    feedback.Position = isGroundAura
                        ? BattleBaseCenterFor(auraTarget)
                        : auraTarget.GetAnchor(BattlePresentationIds.BodyAnchor);
                    feedback.Width = BattleBaseWidthFor(auraTarget.State) *
                        (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E3BellReturn,
                            StringComparison.Ordinal) ? 1.55f : 1.15f);
                }
            }
            else if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05Barrier,
                         StringComparison.Ordinal) ||
                     string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05BarrierBreak,
                         StringComparison.Ordinal) ||
                     string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                         StringComparison.Ordinal))
            {
                var c05Target = battlePresentation.FindView(feedback.TargetId);
                if (c05Target != null && c05Target.State != null)
                {
                    feedback.Position = BattleBaseCenterFor(c05Target);
                    feedback.Width = BattleBaseWidthFor(c05Target.State) *
                        (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                            StringComparison.Ordinal) ? 1f : 1.15f);
                }
            }
            else if (string.Equals(feedback.ProfileId,
                         BattleFeedbackProfileIds.B1BlockadeQuake, StringComparison.Ordinal))
            {
                var b1Target = battlePresentation.FindView(feedback.TargetId);
                if (b1Target != null && b1Target.State != null)
                {
                    var baseWidth = BattleBaseWidthFor(b1Target.State);
                    feedback.Width = baseWidth * 2f;
                    var b1VisibleUv = BattleProjectileProfiles.FeedbackVisibleUvFor(
                        BattleFeedbackProfileIds.B1BlockadeQuake);
                    var effectHeight = feedback.Width * b1VisibleUv.height /
                                       Mathf.Max(0.01f, b1VisibleUv.width);
                    var baseHeight = baseWidth * PieceBasePresentationContract.CanvasHeight /
                                     (float)PieceBasePresentationContract.CanvasWidth;
                    feedback.Position = BattleBaseCenterFor(b1Target) +
                        Vector2.down * Mathf.Max(0f, (effectHeight - baseHeight) * 0.5f);
                }
            }
            var sprite = LoadBattleVfxSprite(BattleFeedbackResourcePath(feedback.ProfileId));
            if (sprite == null || sprite.texture == null) return;
            if (feedback.ElapsedSeconds < feedback.StartDelaySeconds) return;
            var playDuration = Mathf.Max(0.001f,
                feedback.DurationSeconds - feedback.StartDelaySeconds);
            var localElapsed = feedback.ElapsedSeconds - feedback.StartDelaySeconds;
            var progress = Mathf.Clamp01(localElapsed / playDuration);
            var alpha = feedback.Persistent
                ? Mathf.Clamp01(feedback.ElapsedSeconds / 0.18f) *
                  Mathf.Lerp(0.72f, 0.92f, (Mathf.Sin(feedback.ElapsedSeconds * 3.5f) + 1f) * 0.5f)
                : feedback.FadingOut
                    ? 1f - progress
                    : Mathf.Sin(progress * Mathf.PI);
            if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.N2CrossCutSlash,
                    StringComparison.Ordinal))
            {
                DrawN2CrossCutVisual(feedback, sprite, alpha);
                return;
            }
            if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.RescueBeam,
                    StringComparison.Ordinal))
            {
                DrawRescueBeamVisual(feedback, sprite, alpha);
                return;
            }
            if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C03MarkTransfer,
                    StringComparison.Ordinal))
            {
                DrawC03MarkTransferVisual(feedback, sprite, alpha);
                return;
            }
            if (feedback.Persistent && string.Equals(feedback.ProfileId,
                    BattleFeedbackProfileIds.OlinTide, StringComparison.Ordinal))
            {
                feedback.RenderScale = 1f + Mathf.Sin(feedback.ElapsedSeconds * 3.5f) * 0.025f;
            }
            else if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.HealFeedbackRing,
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
            else if (feedback.Persistent &&
                     (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05Barrier,
                         StringComparison.Ordinal) ||
                      string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                         StringComparison.Ordinal)))
            {
                feedback.RenderScale = 1f + Mathf.Sin(feedback.ElapsedSeconds * 3.5f) * 0.02f;
            }
            else if (feedback.Persistent &&
                     (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E1SteadyLine,
                         StringComparison.Ordinal) ||
                      string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.E3BellReturn,
                         StringComparison.Ordinal)))
            {
                feedback.RenderScale = 1f + Mathf.Sin(feedback.ElapsedSeconds * 3.2f) * 0.025f;
            }
            else if (string.Equals(feedback.ProfileId,
                         BattleFeedbackProfileIds.B1BlockadeQuake, StringComparison.Ordinal))
            {
                var press = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress / 0.34f));
                feedback.RenderScale = Mathf.Lerp(0.88f, 1f, press);
                alpha *= Mathf.Sin(Mathf.Clamp01(progress) * Mathf.PI);
            }
            else if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05BarrierBreak,
                         StringComparison.Ordinal))
            {
                feedback.RenderScale = 1f + Mathf.Clamp01(progress) * 0.08f;
            }
            var visibleUv = BattleProjectileProfiles.FeedbackVisibleUvFor(feedback.ProfileId);
            if (feedback.MirrorX)
            {
                visibleUv.x += visibleUv.width;
                visibleUv.width = -visibleUv.width;
            }
            var visiblePivot = BattleProjectileProfiles.FeedbackVisiblePivotFor(feedback.ProfileId);
            var visibleUvWidth = Mathf.Max(0.01f, Mathf.Abs(visibleUv.width));
            if (string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.N1HoldGateStamp,
                    StringComparison.Ordinal))
            {
                var riseProgress = feedback.Persistent
                    ? Mathf.Clamp01(feedback.ElapsedSeconds / 0.28f)
                    : Mathf.Clamp01(progress / 0.42f);
                var rise = Mathf.SmoothStep(0.20f, 1f, riseProgress);
                feedback.HeightOverride = feedback.Width * 1022f / 418f * rise;
            }
            var width = feedback.Width > 0f
                ? feedback.Width
                : feedback.HeightOverride * visibleUvWidth /
                  Mathf.Max(0.01f, visibleUv.height);
            var height = feedback.HeightOverride > 0f
                ? feedback.HeightOverride
                : width * visibleUv.height / visibleUvWidth;
            width *= feedback.RenderScale;
            height *= feedback.RenderScale;
            var position = feedback.Position + feedback.Motion * progress;
            if (feedback.UseArcMotion)
            {
                var start = feedback.Position;
                var end = feedback.Position + feedback.Motion;
                var control = end + feedback.ArcControlOffset;
                var remaining = 1f - progress;
                position = remaining * remaining * start +
                    2f * remaining * progress * control +
                    progress * progress * end;
            }
            var rect = new Rect(position.x - width * visiblePivot.x,
                position.y - height * visiblePivot.y, width, height);
            GUI.color = WithOpacity(Color.white, battleSceneOpacity * alpha);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, visibleUv, true);
        }

        private void DrawRescueBeamVisual(BattleFeedbackVisual feedback, Sprite sprite, float alpha)
        {
            var source = battlePresentation.FindView(feedback.SourceId);
            var target = battlePresentation.FindView(feedback.TargetId);
            if (source == null || target == null || source.State == null || target.State == null) return;
            var start = source.GetAnchor(BattlePresentationIds.SkillOriginAnchor);
            var end = target.GetAnchor(BattlePresentationIds.BodyAnchor);
            var oldMatrix = GUI.matrix;
            var screenStart3 = oldMatrix.MultiplyPoint3x4(new Vector3(start.x, start.y, 0f));
            var screenEnd3 = oldMatrix.MultiplyPoint3x4(new Vector3(end.x, end.y, 0f));
            var screenStart = new Vector2(screenStart3.x, screenStart3.y);
            var screenEnd = new Vector2(screenEnd3.x, screenEnd3.y);
            var delta = screenEnd - screenStart;
            if (delta.sqrMagnitude <= 0.001f) return;
            var uv = BattleProjectileProfiles.FeedbackVisibleUvFor(feedback.ProfileId);
            var center = (screenStart + screenEnd) * 0.5f;
            var height = Mathf.Max(12f, delta.magnitude * uv.height / Mathf.Max(0.01f, uv.width));
            var rect = new Rect(center.x - delta.magnitude * 0.5f, center.y - height * 0.5f,
                delta.magnitude, height);
            GUI.matrix = Matrix4x4.identity;
            GUI.color = WithOpacity(Color.white, battleSceneOpacity * alpha);
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, center);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
        }

        private void DrawN2CrossCutVisual(BattleFeedbackVisual feedback, Sprite sprite, float alpha)
        {
            if (sprite == null || sprite.texture == null) return;
            var texture = sprite.texture;
            var width = Mathf.Clamp(feedback.Width * 1.56f, 86.4f, 280.8f);
            var aspect = texture.width / Mathf.Max(1f, texture.height);
            var height = width / Mathf.Max(0.01f, aspect);
            var uv = new Rect(0f, 0f, 1f, 1f);
            if (feedback.MirrorX)
            {
                uv.x = 1f;
                uv.width = -1f;
            }
            var playDuration = Mathf.Max(0.001f,
                feedback.DurationSeconds - feedback.StartDelaySeconds);
            var progress = Mathf.Clamp01((feedback.ElapsedSeconds - feedback.StartDelaySeconds) /
                playDuration);
            var movementProgress = Mathf.SmoothStep(0f, 1f, progress);
            var position = feedback.Position + feedback.Motion * movementProgress;
            var rect = new Rect(position.x - width * 0.5f, position.y - height * 0.5f,
                width, height);
            GUI.color = WithOpacity(Color.white, battleSceneOpacity * alpha);
            GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
            GUI.color = Color.white;
        }

        private void DrawC03MarkTransferVisual(BattleFeedbackVisual feedback, Sprite sprite, float alpha)
        {
            var source = battlePresentation.FindView(feedback.SourceId);
            var target = battlePresentation.FindView(feedback.TargetId);
            if (source == null || target == null || source.State == null || target.State == null) return;
            var start = source.GetAnchor(BattlePresentationIds.BodyAnchor);
            var end = target.GetAnchor(BattlePresentationIds.BodyAnchor);
            var oldMatrix = GUI.matrix;
            var screenStart3 = oldMatrix.MultiplyPoint3x4(new Vector3(start.x, start.y, 0f));
            var screenEnd3 = oldMatrix.MultiplyPoint3x4(new Vector3(end.x, end.y, 0f));
            var screenStart = new Vector2(screenStart3.x, screenStart3.y);
            var screenEnd = new Vector2(screenEnd3.x, screenEnd3.y);
            var delta = screenEnd - screenStart;
            if (delta.sqrMagnitude <= 0.001f) return;
            var uv = BattleProjectileProfiles.FeedbackVisibleUvFor(feedback.ProfileId);
            var width = Mathf.Min(delta.magnitude, 240f);
            var height = Mathf.Max(18f, width * uv.height / Mathf.Max(0.01f, uv.width));
            var center = Vector2.Lerp(screenStart, screenEnd,
                Mathf.Clamp01(feedback.ElapsedSeconds / Mathf.Max(0.001f, feedback.DurationSeconds)));
            var rect = new Rect(center.x - width * 0.5f, center.y - height * 0.5f, width, height);
            GUI.matrix = Matrix4x4.identity;
            GUI.color = WithOpacity(Color.white, battleSceneOpacity * alpha);
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, center);
            GUI.DrawTextureWithTexCoords(rect, sprite.texture, uv, true);
            GUI.matrix = oldMatrix;
            GUI.color = Color.white;
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
            if (string.Equals(profileId, BattleFeedbackProfileIds.ReturnBeaconRing,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.HealFeedbackRingResourcePath;
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
            if (string.Equals(profileId, BattleFeedbackProfileIds.C05Barrier,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.C05BarrierResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C05BarrierBreak,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.C05BarrierBreakResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                    StringComparison.Ordinal)) return BattleProjectileProfiles.C05ColumnActionBuffResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C06FireCharge,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.C06FireChargeResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.C06ReturnFireBuff,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.C06ReturnFireBuffResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.N1HoldGateStamp,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.N1HoldGateStampResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.N2CrossCutSlash,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.N2CrossCutSlashResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.N4MistHuntCrescent,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.N4MistHuntCrescentResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.E1SteadyLine,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.E1SteadyLineResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.E2TrackJudgment,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.E2TrackJudgmentResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.E3BellReturn,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.E3BellReturnResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.B1BlockadeQuake,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.B1BlockadeQuakeResourcePath;
            if (string.Equals(profileId, BattleProjectileProfileIds.BossLampBeam,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.B2LampBeamResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.B2RecallLost,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.B2RecallLostResourcePath;
            if (string.Equals(profileId, BattleFeedbackProfileIds.B2DeviantLight,
                     StringComparison.Ordinal)) return BattleProjectileProfiles.B2DeviantLightResourcePath;
            return string.Empty;
        }

        private void AdvanceBattleFeedback(float deltaTime)
        {
            if (!battlePresentationActive || battlePaused || deltaTime <= 0f) return;
            for (var index = battleFeedbackVisuals.Count - 1; index >= 0; index--)
            {
                var feedback = battleFeedbackVisuals[index];
                feedback.ElapsedSeconds += deltaTime;
                if (feedback.Persistent)
                {
                    var target = battlePresentation.FindView(feedback.TargetId);
                    if (target == null || target.State == null || !target.State.IsAlive)
                        battleFeedbackVisuals.RemoveAt(index);
                    continue;
                }
                if (feedback.ElapsedSeconds >= feedback.DurationSeconds)
                    battleFeedbackVisuals.RemoveAt(index);
            }
        }

        private static void DrawBattleProjectileLine(Vector2 start, Vector2 end, Color color, float width)
        {
            var delta = end - start;
            if (delta.sqrMagnitude <= 0.001f) return;
            var oldMatrix = GUI.matrix;
            var screenStart3 = oldMatrix.MultiplyPoint3x4(new Vector3(start.x, start.y, 0f));
            var screenEnd3 = oldMatrix.MultiplyPoint3x4(new Vector3(end.x, end.y, 0f));
            var screenStart = new Vector2(screenStart3.x, screenStart3.y);
            var screenEnd = new Vector2(screenEnd3.x, screenEnd3.y);
            var screenDelta = screenEnd - screenStart;
            var screenWidth = width * oldMatrix.MultiplyVector(Vector3.up).magnitude;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = color;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(screenDelta.y, screenDelta.x) * Mathf.Rad2Deg,
                screenStart);
            GUI.DrawTexture(new Rect(screenStart.x, screenStart.y - screenWidth * 0.5f,
                    screenDelta.magnitude, screenWidth),
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
            if (!artCatalog.TryGetCharacterAvatar(unit.NameKey,
                    CharacterQualityResourceId(unit.Quality), AvatarArtContext.PreparationList, out avatar))
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
                    CharacterDisplayName(unit.NameKey, unit.Quality),
                    ArchiveStyle(18, PreparationTextPrimary, TextAnchor.MiddleCenter, true));
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
            var avatarHost = new Rect(area.x + 8f, area.y, 90f, area.height);
            DrawCharacterAvatar(unit.NameKey, unit.Quality, AvatarArtContext.PreparationList,
                NativeCharacterAvatarRect(avatarHost, AvatarArtContext.PreparationList));

            var textX = area.x + 108f;
            var textWidth = area.width - 116f;
            if (showRunLevel)
            {
                DrawRunLevelCrescent(new Rect(area.x + 5f, area.y - 24f, 96f, 48f),
                    unit.RunLevel, 1f);
            }
            GUI.Label(new Rect(textX, area.y + 3f, textWidth, 38f),
                CharacterDisplayName(unit.NameKey, unit.Quality),
                ArchiveStyle(17, PreparationTextPrimary, TextAnchor.MiddleCenter, true));
            GUI.Label(new Rect(textX, area.y + 60f, textWidth, 20f),
                PrimaryStatsText(unit), Style(11, PreparationTextPrimary, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(textX, area.y + 82f, textWidth, 22f),
                SecondaryStatsText(unit), Style(11, PreparationTextPrimary, TextAnchor.MiddleCenter));
        }

        private void DrawDefenseScreen()
        {
            DrawSharedFormationPage(defenseSlots, defenseBenchSlots, false);
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
            PlayUiClick(UiAudioCueIds.Tap, "defense-formation-slot");
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
            PlayUiClick(UiAudioCueIds.Tap, "defense-unit-deploy");
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

        private void AddDefenseOwnedUnit(OwnedCharacterState owned)
        {
            for (var index = 0; index < defenseSlots.Length; index++)
            {
                if (defenseSlots[index] != null) continue;
                defenseSlots[index] = CreateOwnedArenaUnit(owned);
                defenseMessageKey = "defense.saved";
                SaveGame();
                return;
            }
            defenseMessageKey = "defense.full";
        }

        private void CreateStarterDefense()
        {
            for (var index = 0; index < nextRunDeckSelection.SelectedInstanceIds.Count && index < defenseSlots.Length; index++)
                defenseSlots[index] = CreateOwnedArenaUnit(playerCollection.GetByInstanceId(nextRunDeckSelection.SelectedInstanceIds[index]));
        }

        private void SelectOrMoveRunUnit(int destination)
        {
            PlayUiClick(UiAudioCueIds.Tap, "preparation-formation-slot");
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

            var source = selectedRunSlot;
            var sourceUnit = runRoster.Get(source);
            var actionId = NewRunEconomyActionId("move");
            var expectedVersion = runEconomyActions.StateVersion;
            var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
                runRoster.TryMoveOrSwap(source, destination)
                    ? RunEconomyResultCode.Success
                    : RunEconomyResultCode.InvalidTarget);
            if (receipt.ResultCode == RunEconomyResultCode.Success)
                RecordFormalRosterMove(
                    actionId, expectedVersion, sourceUnit.RunUnitId, destination);
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
            PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-generic");
            if (!confirmAbandon)
            {
                confirmAbandon = true;
                return;
            }
            AbandonRun();
        }

        private void DrawUpgradeChoices(float y)
        {
            GUI.Label(ExpeditionUiV46Layout.UpgradeTitle, T("upgrade.archiveTitle"),
                ArchiveStyle(32, ArchiveInk(), TextAnchor.MiddleCenter, true));
            GUI.Label(ExpeditionUiV46Layout.UpgradeSubtitle,
                T("upgrade.archiveSubtitle") + "  ·  " + T("upgrade.prompt"),
                ArchiveStyle(19, ArchiveMutedInk(), TextAnchor.MiddleCenter));

            if (currentRoguelikeRuntimeGeneration == ChapterOneRoguelikeV11State.RuntimeGeneration)
            {
                var choiceSetV11 = EnsureRoguelikeChoiceSetV11();
                if (choiceSetV11 != null)
                    for (var index = 0; index < choiceSetV11.options.Length; index++)
                        DrawUpgradeButtonV11(ExpeditionUiV46Layout.UpgradeCards[index],
                            choiceSetV11.options[index], ChapterOneRoguelikeChoiceSetV11.OptionId(index));
            }
            else
            {
                var choiceSet = EnsureRoguelikeChoiceSet();
                if (choiceSet != null)
                {
                    DrawUpgradeButton(ExpeditionUiV46Layout.UpgradeCards[0], choiceSet.options[0]);
                    DrawUpgradeButton(ExpeditionUiV46Layout.UpgradeCards[1], choiceSet.options[1]);
                    DrawUpgradeButton(ExpeditionUiV46Layout.UpgradeCards[2], choiceSet.options[2]);
                }
            }
            GUI.Label(ExpeditionUiV46Layout.UpgradeFooterRound,
                string.Format(T("round"), round),
                ArchiveStyle(19, ArchiveMutedInk(), TextAnchor.MiddleCenter));
        }

        private void DrawPostBattleUpgradeScreen()
        {
            var choiceSet = currentRoguelikeRuntimeGeneration ==
                ChapterOneRoguelikeV11State.RuntimeGeneration ? null : EnsureRoguelikeChoiceSet();
            var choiceSetV11 = currentRoguelikeRuntimeGeneration ==
                ChapterOneRoguelikeV11State.RuntimeGeneration ? EnsureRoguelikeChoiceSetV11() : null;
            var blurredBattlefield = artCatalog.GetExpeditionUiV55Texture(
                "battlefield-signal-yard-roguelike-blur-v5.5");
            Texture2D equippedBattlefield;
            var useEquippedBattlefield = TryGetEquippedBattlefieldTexture(out equippedBattlefield);
            if (!useEquippedBattlefield) DrawFullBleedTexture(blurredBattlefield);
            var previousMatrix = PushExpeditionUiV46FullCanvas(ExpeditionUiV551Layout.ReferenceHeight);
            var canvas = ExpeditionUiV551Layout.Canvas;
            if (useEquippedBattlefield)
            {
                // The native-size battlefield was already drawn before entering this scaled
                // 841-wide page canvas. Leaving it untouched preserves the formal 1:1 asset.
            }
            else if (blurredBattlefield != null)
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
            else if (choiceSetV11 != null && choiceSetV11.options != null &&
                     choiceSetV11.options.Length == 3)
            {
                for (var index = 0; index < choiceSetV11.options.Length; index++)
                    DrawUpgradeV551CardV11(ExpeditionUiV551Layout.BenefitRows[index],
                        choiceSetV11.options[index], ChapterOneRoguelikeChoiceSetV11.OptionId(index));
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
                PlayUiClick(UiAudioCueIds.Cancel, "upgrade-v551-back");
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
            if (GUI.Button(area, GUIContent.none, GUIStyle.none))
            {
                ApplyUpgrade(option.optionId);
            }
        }

        private void DrawUpgradeV551CardV11(Rect area,
            ChapterOneRoguelikeCandidate option, string optionId)
        {
            var rarity = (ExpeditionBuffRarity)Mathf.Clamp(option.RarityId, 0, 4);
            var rarityIndex = Mathf.Clamp((int)rarity, 0, 4);
            var cardAtlas = artCatalog.GetExpeditionUiV55Texture("roguelike-quality-cards-v5.5.1");
            DrawAtlasSlice(cardAtlas, area,
                new Rect(133f, 15f + rarityIndex * 250f, 757f, 230f));

            // V11 reuses the accepted V5.5 icon and rarity atlases. The first V11
            // integration drew only text, leaving the formal icon plate and quality
            // badge empty even though the approved resources were already present.
            var iconAtlas = artCatalog.GetExpeditionUiV55Texture("roguelike-benefit-icons-v5.5.2");
            DrawAtlasSlice(iconAtlas, OffsetRect(area, ExpeditionUiV551Layout.BenefitIcon),
                RoguelikeV11IconSource(option.Id));
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

            GUI.Label(OffsetRect(area, ExpeditionUiV551Layout.BenefitName),
                T(RoguelikeV11NameKey(option.Id)), ArchiveStyle(language == GameLanguage.English ? 20 : 36,
                    ArchiveInk(), TextAnchor.MiddleLeft, true));
            DrawUpgradeV551Description(
                OffsetRect(area, ExpeditionUiV551Layout.BenefitDescription),
                RoguelikeV11CardDescription(option));
            var selectArea = OffsetRect(area, ExpeditionUiV551Layout.SelectButton);
            DrawUpgradeV551SelectButton(selectArea);
            GUI.Label(selectArea, T("upgrade.choose"),
                ArchiveStyle(28, ArchiveColor("F4F0DD"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(area, GUIContent.none, GUIStyle.none)) ApplyUpgradeV11(optionId);
        }

        private static Rect RoguelikeV11IconSource(string effectId)
        {
            switch (effectId)
            {
                case "B02": case "B03": case "B07": case "B08":
                case "B09": case "B12": case "B14":
                case "K01": case "K02": case "K05":
                    return new Rect(203f, 11f, 170f, 170f);
                case "B16": case "K09":
                    return new Rect(395f, 11f, 170f, 170f);
                default:
                    return new Rect(11f, 11f, 170f, 170f);
            }
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
            DrawFormalExpeditionSettlementScreen();
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

        private void DrawUpgradeButtonV11(Rect area,
            ChapterOneRoguelikeCandidate option, string optionId)
        {
            var rarity = (ExpeditionBuffRarity)Mathf.Clamp(option.RarityId, 0, 4);
            DrawExpeditionUiV46Texture(area, "buff-card-" + RarityAssetKey(rarity) + "-v4.6",
                ScaleMode.ScaleToFit);
            GUI.Label(OffsetRect(area, 261f, 40f, 286f, 50f), T(RoguelikeV11NameKey(option.Id)),
                ArchiveStyle(26, ArchiveInk(), TextAnchor.MiddleLeft, true));
            GUI.Label(OffsetRect(area, 261f, 111f, 314f, 72f),
                RoguelikeV11CardDescription(option),
                ArchiveStyle(19, ArchiveInk(), TextAnchor.MiddleLeft));
            var selectArea = OffsetRect(area, 534f, 149f, 179f, 61f);
            GUI.Label(selectArea, T("upgrade.choose"),
                ArchiveStyle(26, ArchiveColor("F4E9D0"), TextAnchor.MiddleCenter, true));
            if (GUI.Button(selectArea, GUIContent.none, GUIStyle.none)) ApplyUpgradeV11(optionId);
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
                ? expeditionRewardSnapshot != null && string.Equals(
                    expeditionRewardSnapshot.RulesVersion,
                    ChapterOneDifficultyRules.RulesVersion, StringComparison.Ordinal)
                    ? ChapterOneDifficultyRules.GenerateEncounter(
                        expeditionMapSeed, inspectedNode, expeditionRewardSnapshot.DifficultyId,
                        CompletedBattleCount(), CurrentEncounterTableVersion())
                    : ChapterOneEncounterRules.Generate(
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
            summary.Append("runEconomyRulesVersion=").Append(currentRunEconomyRulesVersion).Append('\n');
            summary.Append("qualityRecruitPriceRulesVersion=")
                .Append(qualityRecruitPriceRulesVersion).Append('\n');
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
                var quality = ShopOfferQuality(definitionId);
                summary.Append("offer[").Append(index).Append("]=")
                    .Append(shopOfferIds != null && index < shopOfferIds.Length ? shopOfferIds[index] : string.Empty)
                    .Append(',').Append(definitionId ?? "sold").Append(',')
                    .Append(definitionId == null ? "sold" : quality.ToString().ToLowerInvariant())
                    .Append(",L").Append(level)
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

            var mapTitle = T("map.chapterTitle");
            var mapTitleStyle = CommonConfirmTextStyle(mapTitle,
                ExpeditionMapUiV62Layout.HeaderTitle,
                language == GameLanguage.English ? 34 : 40, 24,
                ArchiveInk(), true, false);
            mapTitleStyle.alignment = TextAnchor.MiddleLeft;
            mapTitleStyle.font = SourceHanSerifFont(true);
            GUI.Label(ExpeditionMapUiV62Layout.HeaderTitle, mapTitle, mapTitleStyle);
            var mapSubtitle = T("map.chapterPrompt");
            var mapSubtitleStyle = CommonConfirmTextStyle(mapSubtitle,
                ExpeditionMapUiV62Layout.HeaderSubtitle,
                language == GameLanguage.English ? 22 : 26, 18,
                ArchiveMutedInk(), false, false);
            mapSubtitleStyle.alignment = TextAnchor.MiddleLeft;
            mapSubtitleStyle.font = SourceHanSerifFont(false);
            GUI.Label(ExpeditionMapUiV62Layout.HeaderSubtitle, mapSubtitle, mapSubtitleStyle);

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
                PlayUiClick(UiAudioCueIds.Cancel, "expedition-map-v62-back");
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }
            if (GUI.Button(ExpeditionMapUiV62Layout.AbandonFrame,
                    GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-map-v62");
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
            else if (effectiveType == ExpeditionNodeType.Supply)
                DrawEventFamilySupplyChoices(node);
            else
                DrawEventFamilyTreasureChoices(node);
        }

        private void DrawEventFamilySupplyChoices(ExpeditionMapNode node)
        {
            var coinAmount = ChapterOneExpeditionEconomy.SupplyCoinAmount(node.JourneyProgress);
            if (DrawEventFamilyOption(0, "treasure", T("node.supply.coins"),
                    "+" + coinAmount + " " + T("coins"),
                    "ART-UI-PREPARATION-ICON-COIN-V6.7.0", T("upgrade.choose"), true))
            {
                if (TryChooseNonBattleAction(node, "coins"))
                {
                    coins += coinAmount;
                    FinishNonBattleNode();
                }
            }

            var recruitTicket = ResolveSupplyRecruitTicket(node);
            if (DrawEventFamilyOption(1, "treasure", T("node.supply.recruits"),
                    RecruitTicketDisplay(recruitTicket),
                    "ART-UI-PREPARATION-ICON-REFRESH-V6.7.0", T("upgrade.choose"), true))
            {
                if (TryChooseNonBattleAction(node, "recruits"))
                {
                    EnqueueRecruitTicket(recruitTicket);
                    FinishNonBattleNode();
                }
            }

            var preparationId = ResolveSupplyPreparationId(node);
            var hasPendingPreparation = !string.IsNullOrEmpty(expeditionEconomy.PendingPreparationId);
            var shownPreparationId = hasPendingPreparation
                ? expeditionEconomy.PendingPreparationId
                : preparationId;
            var canTakePreparation = !hasPendingPreparation &&
                                     !string.IsNullOrEmpty(shownPreparationId);
            if (DrawEventFamilyOption(2, "treasure", T("preparation." + shownPreparationId),
                    hasPendingPreparation
                        ? T("preparation.ready") + "\n" + T("preparation." + shownPreparationId + ".description")
                        : T("preparation." + shownPreparationId + ".description"),
                    "ART-UI-PREPARATION-ICON-PROFESSIONOUTPUT-V6.7.0",
                    hasPendingPreparation ? T("preparation.ready") : T("upgrade.choose"),
                    canTakePreparation))
            {
                if (TryChooseNonBattleAction(node, "preparation:" + shownPreparationId) &&
                    expeditionEconomy.TryAcquirePreparation(shownPreparationId))
                {
                    LockP12RecruitTarget(node, shownPreparationId);
                    FinishNonBattleNode();
                }
            }
            DrawEventFamilyFooterHint(hasPendingPreparation
                ? T("preparation.occupiedHint")
                : canTakePreparation ? T(statusKey) : T("preparation.noneEligible"));
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
                PlayUiClick(UiAudioCueIds.Cancel, "event-page-v62-back");
                currentView = ScreenView.Home;
                confirmAbandon = false;
            }
            if (GUI.Button(ExpeditionMapUiV62Layout.AbandonFrame, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "run-abandon-event-v62");
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
                T("node.event." + eventId + ".1"),
                T("node.event." + eventId + ".2"),
                T("node.event." + eventId + ".3")
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
                ResolveEventChoice(node, eventId, index);
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
            var canTakeItem = ExpeditionEconomyAuthorityPolicy.AllowsClientSideItemMutation(
                                  IsCurrentFormalExpeditionRunBound()) &&
                              expeditionEconomy.Backpack.Count < ExpeditionEconomyState.BackpackCapacity;
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
            var offers = GenerateAndLockMerchantOffers(node);
            for (var index = 0; index < ExpeditionEventFamilyV126Layout.OptionCards.Length; index++)
            {
                if (index >= offers.Length) continue;
                var item = ChapterOneExpeditionEconomy.GetItem(offers[index]);
                var actionId = node.Id + ":shop:" + index + ":" + item.Id;
                var purchased = expeditionEconomy.HasResolvedAction(actionId);
                var price = CurrentMerchantItemPrice(item.Cost);
                var enabled = !purchased && coins >= price;
                var description = T("economy.item." + item.Id + ".description") +
                                  "\n" + T("price") + " " + price;
                if (DrawEventFamilyOption(index, "shop", T("economy.item." + item.Id),
                        description, EventFamilyShopIcon(item.Id),
                        purchased ? T("eventFamily.sold") : T("eventFamily.buy"), enabled))
                    PurchaseExpeditionItem(node, item, actionId);
            }
            if (DrawEventFamilyPrimaryButton(
                    ExpeditionEventFamilyV126Layout.ShopCompleteButton, T("node.continue"), true))
            {
                expeditionEconomy.TryConsumePreparation("P07");
                FinishNonBattleNode();
            }
        }

        private bool DrawEventFamilyOption(int index, string theme, string title,
            string description, string foregroundIconResource, string buttonLabel, bool enabled)
        {
            var card = ExpeditionEventFamilyV126Layout.OptionCards[index];
            DrawEventFamilyTexture(card, ExpeditionEventFamilyV126Layout.OptionCardResource,
                ScaleMode.ScaleToFit);
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
            return DrawEventFamilyPrimaryButton(buttonArea, buttonLabel, enabled);
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
            if (clicked) PlayUiClick(UiAudioCueIds.Tap, "event-primary-action");
            return clicked;
        }

        private void DrawEventFamilyTacticalSelector(int cardIndex, int itemCount)
        {
            if (itemCount <= 1) return;
            var card = ExpeditionEventFamilyV126Layout.OptionCards[cardIndex];
            var previous = new Rect(card.x + 230f, card.y + 190f, 42f, 34f);
            var next = new Rect(card.x + 470f, card.y + 190f, 42f, 34f);
            if (GUI.Button(previous, "‹", ArchiveStyle(28, ArchiveInk(), TextAnchor.MiddleCenter, true)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "event-tactical-previous");
                eventFamilyTacticalSelection =
                    (eventFamilyTacticalSelection - 1 + itemCount) % itemCount;
            }
            if (GUI.Button(next, "›", ArchiveStyle(28, ArchiveInk(), TextAnchor.MiddleCenter, true)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "event-tactical-next");
                eventFamilyTacticalSelection = (eventFamilyTacticalSelection + 1) % itemCount;
            }
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
            if (Mathf.RoundToInt(area.width) != texture.width ||
                Mathf.RoundToInt(area.height) != texture.height)
            {
                Debug.LogError("Event-family formal texture must render 1:1: " + resourceId +
                               " expected=" + texture.width + "x" + texture.height +
                               " actual=" + area.width + "x" + area.height);
                return;
            }
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, scaleMode, true);
        }

        private string ResolveEventFamilyEventId(ExpeditionMapNode node)
        {
            var generatedEventId = ChapterOneExpeditionEventRules.Generate(
                expeditionMapSeed, runId, node.Id, runEconomyActions.StateVersion,
                CreateEventEligibility(node),
                id => expeditionEconomy.HasResolvedAction("event:seen:" + id));
            string eventId;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "event:" + node.Id, generatedEventId, out eventId))
            {
                expeditionEconomy.AddRuleMarker("event:seen:" + eventId);
                SaveGame();
            }
            return eventId;
        }

        private static string EventFamilyTheme(ExpeditionNodeType type)
        {
            var effectiveType = EffectiveNodeType(type);
            if (effectiveType == ExpeditionNodeType.Treasure ||
                effectiveType == ExpeditionNodeType.Supply) return "treasure";
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
            var eventId = ResolveEventFamilyEventId(node);
            GUI.Label(new Rect(390f, 405f + nodeOffset, 300f, 45f), T("node.event." + eventId),
                Style(23, new Color(0.92f, 0.78f, 0.38f), TextAnchor.MiddleCenter));
            for (var index = 0; index < 3; index++)
                if (ChoiceButton(index, nodeOffset,
                        T("node.event." + eventId + "." + (index + 1))))
                    ResolveEventChoice(node, eventId, index);
        }

        private void ResolveEventChoice(ExpeditionMapNode node, string eventId, int choiceIndex)
        {
            var actionId = ChapterOneExpeditionEventRules.ChoiceId(eventId, choiceIndex);
            var cost = ChapterOneExpeditionEventRules.ChoiceCost(eventId, choiceIndex);
            if (coins < cost)
            {
                statusKey = "status.coins";
                return;
            }

            if (eventId == "E03" && expeditionEconomy.Backpack.Count == 0)
            {
                statusKey = "status.backpackEmpty";
                return;
            }
            if (!TryChooseNonBattleAction(node, actionId)) return;
            coins -= cost;

            switch (eventId)
            {
                case "E01":
                    expeditionEconomy.AddRuleMarker(
                        ChapterOneExpeditionEventRules.PendingMarker(eventId, choiceIndex));
                    break;
                case "E02":
                    if (choiceIndex == 0) coins += 3;
                    else if (choiceIndex == 1)
                        expeditionEconomy.AddRuleMarker("event:pending:E02B");
                    else
                    {
                        coins += 7;
                        expeditionEconomy.AddRuleMarker("event:pending:E02C");
                    }
                    break;
                case "E03":
                    if (choiceIndex < 2)
                    {
                        var itemIndex = LowestPriceBackpackItemIndex();
                        expeditionEconomy.TryDiscardCombatItemAt(itemIndex);
                        if (choiceIndex == 0) coins += 7;
                        else expeditionEconomy.AddRuleMarker("event:pending:E03B");
                    }
                    else expeditionEconomy.AddRuleMarker("event:pending:E03C");
                    break;
                case "E04":
                    var target = ResolveEventRecruitTarget(node, choiceIndex == 0);
                    expeditionEconomy.AddRuleMarker(
                        choiceIndex == 2 ? "event:pending:E04C" :
                        "event:pending:E04" + (choiceIndex == 0 ? "A:" : "B:") + target);
                    break;
                case "E05":
                    expeditionEconomy.AddRuleMarker(
                        ChapterOneExpeditionEventRules.PendingMarker(eventId, choiceIndex));
                    if (choiceIndex > 0)
                        expeditionEconomy.AddRuleMarker("prep:acquired:EXTRA_GROWTH_PREP");
                    break;
            }
            FinishNonBattleNode();
        }

        private ChapterOneEventEligibility CreateEventEligibility(ExpeditionMapNode node)
        {
            var profession = ResolveEventRecruitTarget(node, true);
            var faction = ResolveEventRecruitTarget(node, false);
            return new ChapterOneEventEligibility
            {
                HasLaterBattle = node != null && node.JourneyProgress < 96.3f,
                NextBattleHasBaseGold = node != null && node.JourneyProgress < 96.3f,
                NextBattleIsBoss = false,
                HasBackpackItem = expeditionEconomy.Backpack.Count > 0,
                HasPendingP05 = expeditionEconomy.PendingPreparationId == "P05",
                HasPendingP06 = expeditionEconomy.PendingPreparationId == "P06",
                HasNaturalRecruitRefresh = node != null && node.JourneyProgress < 96.3f,
                HasRecruitProfessionTarget = !string.IsNullOrEmpty(profession),
                HasRecruitFactionTarget = !string.IsNullOrEmpty(faction),
                HasEligibleNormalEnhancement = node != null && node.JourneyProgress < 83.5f,
                ExtraGrowthAvailable = expeditionEconomy.CanAcquirePreparation("P09") ||
                                       expeditionEconomy.CanAcquirePreparation("P11"),
                Coins = coins
            };
        }

        private string ResolveEventRecruitTarget(ExpeditionMapNode node, bool profession)
        {
            var deployed = new List<string>();
            if (runRoster != null)
                for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
                {
                    var unit = runRoster.Get(slot);
                    if (unit != null) deployed.Add(unit.DefinitionId);
                }
            return ChapterOneExpeditionEconomy.MostRepresentedRecruitTarget(
                expeditionMapSeed, runId, node == null ? currentMapNodeId : node.Id,
                characterCatalog, deployed, runDeck.UnitDefinitionIds, profession);
        }

        private int LowestPriceBackpackItemIndex()
        {
            var selected = 0;
            var price = int.MaxValue;
            for (var index = 0; index < expeditionEconomy.Backpack.Count; index++)
            {
                var definition = ChapterOneExpeditionEconomy.GetItem(
                    expeditionEconomy.Backpack[index]);
                var candidate = definition == null ? int.MaxValue : definition.Cost;
                if (candidate < price)
                {
                    selected = index;
                    price = candidate;
                }
            }
            return selected;
        }

        private void DrawExpeditionItemShop(ExpeditionMapNode node, float nodeOffset)
        {
            GUI.Label(new Rect(420f, 385f + nodeOffset, 240f, 56f), T("coins") + "  " + coins,
                Style(24, new Color(1f, 0.82f, 0.30f), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(85f, 390f + nodeOffset, 320f, 48f), T("node.shop.prompt"),
                Style(21, Color.white, TextAnchor.MiddleLeft, FontStyle.Normal));
            var offers = GenerateAndLockMerchantOffers(node);
            for (var index = 0; index < offers.Length; index++)
            {
                var item = ChapterOneExpeditionEconomy.GetItem(offers[index]);
                var actionId = node.Id + ":shop:" + index + ":" + item.Id;
                var purchased = expeditionEconomy.HasResolvedAction(actionId);
                var price = CurrentMerchantItemPrice(item.Cost);
                var area = new Rect(65f + index * 250f, 475f + nodeOffset, 220f, 360f);
                DrawFloatingCardSurface(area, new Color(0.32f, 0.70f, 0.68f), 0.56f);
                GUI.Label(new Rect(area.x + 12f, area.y + 34f, area.width - 24f, 44f),
                    T("economy.item." + item.Id), Style(22, Color.white, TextAnchor.MiddleCenter));
                GUI.Label(new Rect(area.x + 15f, area.y + 95f, area.width - 30f, 125f),
                    T("economy.item." + item.Id + ".description"),
                    Style(15, new Color(0.76f, 0.86f, 0.88f), TextAnchor.MiddleCenter));
                GUI.Label(new Rect(area.x + 12f, area.y + 235f, area.width - 24f, 36f),
                    purchased ? T("shop.sold") : T("price") + " " + price,
                    Style(18, new Color(0.96f, 0.78f, 0.28f), TextAnchor.MiddleCenter));
                GUI.enabled = !purchased && coins >= price &&
                              expeditionEconomy.Backpack.Count < ExpeditionEconomyState.BackpackCapacity;
                if (GUI.Button(new Rect(area.x + 15f, area.yMax - 65f, area.width - 30f, 48f),
                        T("shop.buy"), ButtonStyle(UiButtonSemantic.Reward, 17)))
                    PurchaseExpeditionItem(node, item, actionId);
                GUI.enabled = true;
            }
            GUI.Label(new Rect(120f, 910f + nodeOffset, 840f, 54f), T(statusKey),
                Style(17, new Color(0.78f, 0.87f, 0.88f), TextAnchor.MiddleCenter));
            if (GUI.Button(new Rect(140f, 980f + nodeOffset, 800f, 82f), T("node.continue"),
                    ButtonStyle(UiButtonSemantic.Primary, 22)))
            {
                PlayUiClick(UiAudioCueIds.Tap, "node-shop-continue");
                expeditionEconomy.TryConsumePreparation("P07");
                FinishNonBattleNode();
            }
        }

        private void PurchaseExpeditionItem(
            ExpeditionMapNode node, ExpeditionItemDefinition item, string actionId)
        {
            var price = item == null ? 0 : CurrentMerchantItemPrice(item.Cost);
            if (item == null || coins < price || expeditionEconomy.HasResolvedAction(actionId))
            {
                PlayUiClick(UiAudioCueIds.Disabled, "node-item-purchase-unavailable");
                return;
            }
            var expectedVersion = runEconomyActions.StateVersion;
            var itemInstanceId = StableRunEconomyId(
                "item", runId, node.Id, actionId, item.Id);
            var receipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                if (item.Kind == ExpeditionItemKind.Combat &&
                    !expeditionEconomy.TryAddCombatItem(item.Id, itemInstanceId))
                    return RunEconomyResultCode.RosterFull;
                if (!expeditionEconomy.TryResolveAction(actionId))
                    return RunEconomyResultCode.OfferNotFoundOrSold;
                coins -= price;
                return RunEconomyResultCode.Success;
            });
            if (receipt.ResultCode != RunEconomyResultCode.Success)
            {
                PlayUiClick(UiAudioCueIds.Disabled, "node-item-purchase-failed");
                statusKey = expeditionEconomy.Backpack.Count >= ExpeditionEconomyState.BackpackCapacity
                    ? "status.backpackFull" : "status.itemPurchased";
                return;
            }
            RecordFormalMerchantPurchase(actionId, expectedVersion, item.Id, itemInstanceId);
            PlayUiClick(UiAudioCueIds.Purchase, "node-item-purchase");
            statusKey = "status.itemPurchased";
            SaveGame();
        }

        private int CurrentMerchantItemPrice(int basePrice)
        {
            return expeditionEconomy != null && expeditionEconomy.PendingPreparationId == "P07"
                ? Mathf.Max(1, basePrice - 2)
                : Mathf.Max(0, basePrice);
        }

        private string[] GenerateAndLockMerchantOffers(ExpeditionMapNode node)
        {
            string firstMerchantInventory;
            var hasFirstMerchant = expeditionEconomy.TryGetLockedNodeContent(
                "shop:first-inventory", out firstMerchantInventory);
            var previousOffers = hasFirstMerchant
                ? firstMerchantInventory.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                : null;
            var generatedOffers = ChapterOneExpeditionEconomy.GenerateShopItemIds(
                expeditionMapSeed, node.Id, node.JourneyProgress, expeditionEconomy.Morale,
                expeditionEconomy.HasResolvedAction("run:shop:I06"), previousOffers);
            string lockedOffers;
            var changed = expeditionEconomy.GetOrLockNodeContent(
                "shop:" + node.Id, string.Join(",", generatedOffers), out lockedOffers);
            if (!hasFirstMerchant)
            {
                string ignored;
                changed |= expeditionEconomy.GetOrLockNodeContent(
                    "shop:first-inventory", lockedOffers, out ignored);
            }
            if (changed) SaveGame();
            return lockedOffers.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private void DrawSupplyChoices(ExpeditionMapNode node, float nodeOffset)
        {
            var coinAmount = ChapterOneExpeditionEconomy.SupplyCoinAmount(node.JourneyProgress);
            var recruitTicket = ResolveSupplyRecruitTicket(node);
            if (ChoiceButton(0, nodeOffset, T("node.supply.coins") + "\n+" + coinAmount + " " + T("coins")))
            {
                if (TryChooseNonBattleAction(node, "coins"))
                {
                    coins += coinAmount;
                    FinishNonBattleNode();
                }
            }
            if (ChoiceButton(1, nodeOffset, T("node.supply.recruits") + "\n" +
                    RecruitTicketDisplay(recruitTicket)))
            {
                if (TryChooseNonBattleAction(node, "recruits"))
                {
                    EnqueueRecruitTicket(recruitTicket);
                    FinishNonBattleNode();
                }
            }
            var preparationId = ResolveSupplyPreparationId(node);
            var hasPendingPreparation = !string.IsNullOrEmpty(expeditionEconomy.PendingPreparationId);
            var shownPreparationId = hasPendingPreparation
                ? expeditionEconomy.PendingPreparationId
                : preparationId;
            GUI.enabled = !hasPendingPreparation && !string.IsNullOrEmpty(shownPreparationId);
            if (ChoiceButton(2, nodeOffset,
                    T("preparation." + shownPreparationId) + "\n" +
                    (hasPendingPreparation ? T("preparation.ready") :
                        T("preparation." + shownPreparationId + ".description"))))
            {
                if (TryChooseNonBattleAction(node, "preparation:" + shownPreparationId) &&
                    expeditionEconomy.TryAcquirePreparation(shownPreparationId))
                {
                    LockP12RecruitTarget(node, shownPreparationId);
                    FinishNonBattleNode();
                }
            }
            GUI.enabled = true;
        }

        private string ResolveSupplyPreparationId(ExpeditionMapNode node)
        {
            if (node == null || expeditionEconomy == null) return string.Empty;
            if (!string.IsNullOrEmpty(expeditionEconomy.PendingPreparationId))
                return expeditionEconomy.PendingPreparationId;
            var eligibility = new ExpeditionPreparationEligibility
            {
                HasValidLantern = expeditionLanternSnapshot != null &&
                                  !expeditionLanternSnapshot.IsNone,
                HasBackpackItem = expeditionEconomy.Backpack.Count > 0,
                HasLaterCombat = node.JourneyProgress < 96.3f,
                HasFutureMerchant = node.JourneyProgress < 85f,
                HasEligibleCharacterSale = runRoster != null && runRoster.TotalCount > 0,
                HasEligibleNormalEnhancement = currentRoguelikeRuntimeGeneration ==
                                               ChapterOneRoguelikeV11State.RuntimeGeneration &&
                                               node.JourneyProgress < 83.5f,
                HasNaturalRecruitRefresh = node.JourneyProgress < 96.3f &&
                                           runRoster != null && runRoster.TotalCount > 0
            };
            var generated = ChapterOneExpeditionPreparationRules.Generate(
                string.IsNullOrEmpty(runId) ? "local-" + expeditionMapSeed : runId,
                node.Id, eligibility, expeditionEconomy.CanAcquirePreparation);
            string locked;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "preparation:" + node.Id, generated, out locked)) SaveGame();
            return locked;
        }

        private string ResolveSupplyRecruitTicket(ExpeditionMapNode node)
        {
            var generated = ChapterOneExpeditionEconomy.GenerateTargetedRecruitTicket(
                expeditionMapSeed, runId, node.Id, characterCatalog,
                runDeck.UnitDefinitionIds);
            string locked;
            if (expeditionEconomy.GetOrLockNodeContent(
                    "recruit-ticket:" + node.Id, generated, out locked)) SaveGame();
            return locked;
        }

        private void EnqueueRecruitTicket(string ticket)
        {
            var separator = string.IsNullOrEmpty(ticket) ? -1 : ticket.IndexOf('=');
            if (separator <= 0 || separator >= ticket.Length - 1) return;
            expeditionEconomy.EnqueueTargetedRecruitTicket(
                ticket.Substring(0, separator), ticket.Substring(separator + 1));
        }

        private string RecruitTicketDisplay(string ticket)
        {
            var separator = string.IsNullOrEmpty(ticket) ? -1 : ticket.IndexOf('=');
            if (separator <= 0 || separator >= ticket.Length - 1) return T("preparation.noneEligible");
            return (ticket.StartsWith("profession=", StringComparison.Ordinal)
                ? T("supply.ticket.profession") : T("supply.ticket.faction")) + " · " +
                   T(ticket.Substring(separator + 1));
        }

        private void LockP12RecruitTarget(ExpeditionMapNode node, string preparationId)
        {
            if (preparationId != "P12") return;
            var dimensionSeed = ChapterOneEncounterRules.Mix(
                expeditionMapSeed, runId + ":" + node.Id + ":P12", 12);
            var profession = (dimensionSeed & 1u) == 0u;
            var target = ResolveEventRecruitTarget(node, profession);
            if (string.IsNullOrEmpty(target))
            {
                profession = !profession;
                target = ResolveEventRecruitTarget(node, profession);
            }
            if (!string.IsNullOrEmpty(target))
                expeditionEconomy.AddRuleMarker(
                    "preparation:P12-target:" + (profession ? "profession=" : "faction=") + target);
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
                GUI.enabled = ExpeditionEconomyAuthorityPolicy.AllowsClientSideItemMutation(
                                  IsCurrentFormalExpeditionRunBound()) &&
                              expeditionEconomy.Backpack.Count <
                              ExpeditionEconomyState.BackpackCapacity;
                if (GUI.Button(new Rect(area.x + 28f, area.y + 86f + index * (height + 10f),
                        area.width - 56f, height), T("economy.item." + itemId),
                        ButtonStyle(UiButtonSemantic.Secondary, 15)))
                {
                    PlayUiClick(UiAudioCueIds.Tap, "node-tactical-item");
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
            var clicked = GUI.Button(area, label, ButtonStyle(
                index == 0 ? UiButtonSemantic.Primary :
                index == 1 ? UiButtonSemantic.Reward : UiButtonSemantic.Secondary, 20));
            if (clicked) PlayUiClick(UiAudioCueIds.Tap, "node-choice");
            return clicked;
        }

        private bool TryChooseNonBattleAction(ExpeditionMapNode node, string choiceId)
        {
            if (node == null) return false;
            var resolvedActionId = node.Id + ":choice:" + choiceId;
            if (!expeditionEconomy.TryResolveAction(resolvedActionId)) return false;
            RecordFormalNodeChoice(
                NewFormalExpeditionActionId("choice"),
                runEconomyActions.StateVersion,
                node.Id,
                choiceId);
            return true;
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
                expeditionRewards, currentMapNodeId, 0);
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
                PlayUiClick(UiAudioCueIds.Confirm, "map-node-" + node.Id);
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
                PlayUiClick(UiAudioCueIds.Confirm, "map-node-" + node.Id);
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
                case ExpeditionNodeType.Supply: return "map.node.supply";
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
            if (UsesServerAuthoritativeExpedition && expeditionEntryCommitted)
            {
                GUI.Label(new Rect(70f, y + 180f, 940f, 72f),
                    T(HasPendingFormalExpeditionSettlement
                        ? "run.authoritativeSettlementPending"
                        : "run.authoritativeSettlementReady"),
                    Style(20, new Color(1f, 0.78f, 0.25f),
                        TextAnchor.MiddleCenter, FontStyle.Normal));
                GUI.enabled = !formalExpeditionSettlementClaimInProgress;
                if (GUI.Button(new Rect(55f, y + 300f, 970f, 100f),
                        T("run.claimAuthoritativeSettlement"),
                        ButtonStyle(UiButtonSemantic.Reward, 24)))
                {
                    PlayUiClick(UiAudioCueIds.Reward,
                        "run-result-authoritative-claim");
                    ClaimFormalSettlementAndReturnHomeAsync();
                }
                GUI.enabled = true;
                return;
            }
            var rewards = expeditionRewards.CreateSnapshot();
            GUI.Label(new Rect(70f, y + 180f, 940f, 38f),
                T(currentExpeditionRewardsEnabled ? "run.localTreasure" : "run.freeNoRewards"),
                Style(21, new Color(1f, 0.78f, 0.25f), TextAnchor.MiddleCenter, FontStyle.Normal));
            GUI.Label(new Rect(70f, y + 218f, 940f, 32f),
                T("warehouse.crystal") + " " + rewards.TideCrystals,
                Style(15, new Color(0.82f, 0.88f, 0.90f), TextAnchor.MiddleCenter));
            GUI.Label(new Rect(70f, y + 252f, 940f, 30f),
                T("run.equipmentCount") + " " + rewards.EquipmentTemplateIds.Count +
                (runWon && currentExpeditionRewardsEnabled ? " · " + T("run.lanternCount") + " 1" : string.Empty),
                Style(15, new Color(0.82f, 0.88f, 0.90f), TextAnchor.MiddleCenter));
            var canStoreRewards = CanStoreExpeditionRewards(rewards);
            var actionKey = expeditionRewards.IsClaimed
                ? "run.restart"
                : canStoreRewards ? "run.claimAndReturn" : "run.warehouseFull";
            GUI.enabled = expeditionRewards.IsClaimed || canStoreRewards;
            if (GUI.Button(new Rect(55f, y + 300f, 970f, 100f), T(actionKey),
                    ButtonStyle(expeditionRewards.IsClaimed ? UiButtonSemantic.Primary : UiButtonSemantic.Reward, 24)))
            {
                PlayUiClick(expeditionRewards.IsClaimed ? UiAudioCueIds.Tap : UiAudioCueIds.Reward,
                    expeditionRewards.IsClaimed ? "run-result-restart" : "run-result-claim");
                if (expeditionRewards.IsClaimed)
                {
                    RestartRun();
                }
                else
                {
                    if (ClaimExpeditionRewards())
                    {
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
            var cost = ShopOfferListedPrice(offerIndex, nameKey, level);
            var power = UnitBasePower(nameKey);
            CharacterQuality quality;
            var stats = CalculateShopOfferStats(nameKey, power, level, out quality);
            ActiveUiSkin.DrawListRow(area, false);
            DrawCharacterAvatar(nameKey, quality, AvatarArtContext.Recruit,
                new Rect(area.x + 9f, area.y + 9f, 92f, 86f));
            GUI.Label(new Rect(area.x + 110f, area.y + 7f, area.width - 120f, 32f),
                CharacterDisplayName(nameKey, quality) + "  Lv." + level,
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
            var purchasedOfferId = offerIndex >= 0 && offerIndex < shopOfferIds.Length
                ? shopOfferIds[offerIndex] : null;
            string purchasedRunUnitId = null;
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
                purchasedRunUnitId = runUnitId;
                var quality = CharacterQuality.Common;
                var sourceBusinessId = runUnitId;
                var externalStar = 0;
                OwnedCharacterState lockedCharacter;
                if (TryGetLockedRunCharacter(nameKey, out lockedCharacter))
                {
                    quality = lockedCharacter.Quality;
                    sourceBusinessId = lockedCharacter.CharacterInstanceId;
                    externalStar = lockedCharacter.ExternalStar;
                }
                if (SkillDiagnosticActive)
                    quality = ResolveDiagnosticQuality(nameKey);
                var candidate = new RunUnitState(nameKey, power, level, payableCost, runUnitId,
                    quality, CharacterRarityInstanceRules.ComputeAptitudeRoll(sourceBusinessId),
                    sourceBusinessId, externalStar);
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
            if (receipt.ResultCode == RunEconomyResultCode.Success)
                RecordFormalShopPurchase(
                    actionId, expectedVersion, purchasedOfferId,
                    purchasedRunUnitId);
            PlayUiClick(receipt.ResultCode == RunEconomyResultCode.Success
                    ? UiAudioCueIds.Purchase
                    : UiAudioCueIds.Disabled,
                receipt.ResultCode == RunEconomyResultCode.Success
                    ? "recruit-shop-buy"
                    : "recruit-shop-buy-failed");
            ApplyRunEconomyStatus(receipt.ResultCode, "status.recruited");
            SaveGame();
        }

        private bool CanRecruitNow()
        {
            return expeditionFlow.CanPrepare;
        }

        private bool TryGetLockedRunCharacter(string definitionId, out OwnedCharacterState character)
        {
            character = null;
            if (string.IsNullOrEmpty(definitionId) || playerCollection == null) return false;

            var lockedInstanceId = runDeck == null ? null : runDeck.GetCharacterInstanceId(definitionId);
            if (!string.IsNullOrEmpty(lockedInstanceId) &&
                playerCollection.TryGetByInstanceId(lockedInstanceId, out character) &&
                character.DefinitionId == definitionId)
                return true;

            try
            {
                character = playerCollection.Get(definitionId);
                return character != null;
            }
            catch (KeyNotFoundException)
            {
                character = null;
                return false;
            }
        }

        private CombatStatBlock CalculateShopOfferStats(string definitionId, int basePower, int level,
            out CharacterQuality quality)
        {
            quality = CharacterQuality.Common;
            OwnedCharacterState lockedCharacter;
            if (!TryGetLockedRunCharacter(definitionId, out lockedCharacter))
                return characterCatalog.Get(definitionId).CalculateCombatStats(basePower, level);

            quality = SkillDiagnosticActive ? ResolveDiagnosticQuality(definitionId) : lockedCharacter.Quality;
            var sourceBusinessId = lockedCharacter.CharacterInstanceId;
            var aptitudeRoll = CharacterRarityInstanceRules.ComputeAptitudeRoll(sourceBusinessId);
            return CharacterRarityInstanceRules.CalculateCombatStats(
                characterCatalog.Get(definitionId), basePower, level, quality, aptitudeRoll,
                lockedCharacter.ExternalStar);
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
            var rarityStats = CharacterRarityInstanceRules.CalculateCombatStats(
                definition, runUnit.BasePower, runUnit.RunLevel, runUnit.Quality,
                runUnit.AptitudeRoll, runUnit.ExternalStar);
            var stats = EquipmentDerivedStatCalculator.Calculate(rarityStats, loadout);
            var unit = UnitData.Create(
                definition,
                runUnit.BasePower,
                PlayerColor(runUnit.DefinitionId),
                runUnit.RunLevel,
                stats);
            unit.CharacterInstanceId = runUnit.SourceBusinessId;
            unit.Quality = runUnit.Quality;
            unit.AptitudeRoll = runUnit.AptitudeRoll;
            unit.ExternalStar = runUnit.ExternalStar;
            ApplyRunBonuses(unit);
            ApplyFormationSynergies(unit, definition);
            ApplyNextBattlePreparation(unit);
            var preLanternMaxHealth = unit.MaxHealth;
            var preLanternArmor = unit.Armor;
            var preLanternAttackDamage = unit.AttackDamage;
            var preLanternHealAmount = unit.HealAmount;
            var preLanternAttackSpeed = unit.AttackSpeedBasisPoints;
            ApplyLanternCombatStats(unit, expeditionLanternSnapshot);
            if (expeditionEconomy != null && expeditionEconomy.PendingPreparationId == "P04")
            {
                unit.MaxHealth += ScalePreparationLanternDelta(unit.MaxHealth - preLanternMaxHealth);
                unit.Armor += ScalePreparationLanternDelta(unit.Armor - preLanternArmor);
                unit.AttackDamage += ScalePreparationLanternDelta(unit.AttackDamage - preLanternAttackDamage);
                unit.HealAmount += ScalePreparationLanternDelta(unit.HealAmount - preLanternHealAmount);
                unit.AttackSpeedBasisPoints += ScalePreparationLanternDelta(
                    unit.AttackSpeedBasisPoints - preLanternAttackSpeed);
            }
            if (expeditionEconomy != null && expeditionEconomy.PendingPreparationId == "P01")
                unit.OpeningShield += Mathf.Max(1, Mathf.CeilToInt(unit.MaxHealth * 0.15f));
            return unit;
        }

        private static int ScalePreparationLanternDelta(int delta)
        {
            if (delta == 0) return 0;
            return delta > 0
                ? Mathf.CeilToInt(delta * 0.30f)
                : Mathf.FloorToInt(delta * 0.30f);
        }

        private UnitData CreateRunUnitEffectiveView(RunUnitState runUnit)
        {
            var unit = CreateRunUnitView(runUnit);
            ApplyRoguelikePersistentStatPreview(unit);
            return unit;
        }

        private LanternSnapshot CurrentLanternSnapshot()
        {
            return lanternInventory == null
                ? LanternSnapshot.None(LanternLegacyMigrationResult.LegacyNoneRulesVersion)
                : lanternInventory.CreateActiveSnapshot(LanternLegacyMigrationResult.LegacyNoneRulesVersion);
        }

        private static void ApplyLanternCombatStats(UnitData unit, LanternSnapshot snapshot)
        {
            if (unit == null) return;
            unit.MaxHealth = LanternCombatStatRules.ApplyMaximumHealth(unit.MaxHealth, snapshot);
            unit.Armor = LanternCombatStatRules.ApplyArmor(unit.Armor, snapshot);
            unit.AttackDamage = LanternCombatStatRules.ApplyAttackDamage(unit.AttackDamage, snapshot);
            unit.HealAmount = LanternCombatStatRules.ApplyHealingAmount(unit.HealAmount, snapshot);
            unit.AttackSpeedBasisPoints = LanternCombatStatRules.ApplyAttackSpeed(
                unit.AttackSpeedBasisPoints, snapshot);
        }

        private static UnitData CreateLanternAdjustedCopy(UnitData source, LanternSnapshot snapshot)
        {
            if (source == null) return null;
            var copy = new UnitData
            {
                NameKey = source.NameKey,
                Power = source.Power,
                BasePower = source.BasePower,
                RunLevel = source.RunLevel,
                Color = source.Color,
                MaxHealth = source.MaxHealth,
                AttackDamage = source.AttackDamage,
                HealAmount = source.HealAmount,
                Armor = source.Armor,
                SpeedTier = source.SpeedTier,
                AttackSpeedBasisPoints = source.AttackSpeedBasisPoints,
                OpeningShield = source.OpeningShield,
                IsHealer = source.IsHealer,
                CharacterInstanceId = source.CharacterInstanceId,
                Quality = source.Quality,
                AptitudeRoll = source.AptitudeRoll,
                ExternalStar = source.ExternalStar
            };
            ApplyLanternCombatStats(copy, snapshot);
            return copy;
        }

        private static void ApplyLanternOpeningShield(
            IList<BattleUnitState> units, LanternSnapshot snapshot)
        {
            BattleUnitState target = null;
            foreach (var unit in units)
            {
                if (unit == null || !unit.IsPlayer || unit.Slot < 0 || unit.Slot >= 3) continue;
                if (target == null || unit.MaxHealth > target.MaxHealth ||
                    unit.MaxHealth == target.MaxHealth && unit.Slot < target.Slot) target = unit;
            }
            if (target == null) return;
            target.Shield += LanternCombatStatRules.TideShellOpeningShield(target.MaxHealth, snapshot);
        }

        private static void ApplyLanternOpeningShield(
            IList<BattleUnitPresentationState> units, LanternSnapshot snapshot)
        {
            BattleUnitPresentationState target = null;
            foreach (var unit in units)
            {
                if (unit == null || !unit.IsPlayer || unit.Slot < 0 || unit.Slot >= 3) continue;
                if (target == null || unit.MaxHealth > target.MaxHealth ||
                    unit.MaxHealth == target.MaxHealth && unit.Slot < target.Slot) target = unit;
            }
            if (target == null) return;
            var shield = LanternCombatStatRules.TideShellOpeningShield(target.MaxHealth, snapshot);
            target.MaxShield += shield;
            target.CurrentShield += shield;
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
            unit.AttackSpeedBasisPoints = RoguelikeEnhancementRules.EffectiveAttackSpeedBasisPoints(
                unit.AttackSpeedBasisPoints, effects);
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
                case "P01":
                    break;
                case "P02":
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
            return DeckDisplayName(runDeck.UnitDefinitionIds, runDeck.CharacterInstanceIds);
        }

        private string NextDeckDisplayName()
        {
            return DeckDisplayName(nextRunDeckSelection.SelectedIds,
                nextRunDeckSelection.SelectedInstanceIds);
        }

        private string DeckDisplayName(
            IReadOnlyList<string> definitionIds,
            IReadOnlyList<string> characterInstanceIds)
        {
            var names = new string[definitionIds.Count];
            for (var i = 0; i < names.Length; i++)
            {
                OwnedCharacterState owned;
                names[i] = characterInstanceIds != null && i < characterInstanceIds.Count &&
                           playerCollection.TryGetByInstanceId(characterInstanceIds[i], out owned)
                    ? CharacterDisplayName(owned.DefinitionId, owned.Quality)
                    : T(definitionIds[i]);
            }
            return string.Join(" / ", names);
        }

        private void RefreshAllShopOffers()
        {
            string[] offers;
            int[] levels;
            string[] offerIds;
            int[] listedPrices;
            bool[] sold;
            GenerateShopOfferSnapshot(shopRefreshSerial + 1, true, out offers, out levels, out offerIds,
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
            bool naturalRefresh,
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
            var node = expeditionMap == null ? null : expeditionMap.GetNode(currentMapNodeId);
            var progress = node == null ? 0f : node.JourneyProgress;
            var target = naturalRefresh
                ? NaturalRecruitTargetMarker()
                : expeditionEconomy.PeekTargetedRecruitTicket();
            var guaranteedCount = naturalRefresh &&
                                  (target.StartsWith("profession=", StringComparison.Ordinal) ||
                                   target.StartsWith("faction=", StringComparison.Ordinal))
                ? (expeditionEconomy.FindRuleMarker("event:pending:E04A:").Length > 0 ||
                   expeditionEconomy.FindRuleMarker("event:pending:E04B:").Length > 0 ? 2 : 1)
                : string.IsNullOrEmpty(target) ? 0 : 1;
            var matchingDeckIndexes = new List<int>();
            for (var deckIndex = 0; deckIndex < runDeck.UnitDefinitionIds.Count; deckIndex++)
                if (ChapterOneExpeditionEconomy.MatchesRecruitTarget(
                        characterCatalog, runDeck.GetPrototypeOffer(deckIndex), target))
                    matchingDeckIndexes.Add(deckIndex);
            for (var i = 0; i < ShopOfferCount; i++)
            {
                var definitionIndex = i < guaranteedCount && matchingDeckIndexes.Count > 0
                    ? matchingDeckIndexes[ChapterOneExpeditionEconomy.RecruitOfferDefinitionIndex(
                        expeditionMapSeed, round, refreshSerial, i, matchingDeckIndexes.Count)]
                    : ChapterOneExpeditionEconomy.RecruitOfferDefinitionIndex(
                        expeditionMapSeed, round, refreshSerial, i, runDeck.UnitDefinitionIds.Count);
                offers[i] = runDeck.GetPrototypeOffer(definitionIndex);
                levels[i] = ChapterOneExpeditionEconomy.RecruitOfferLevel(
                    expeditionMapSeed, currentMapNodeId, refreshSerial, i, progress);
                listedPrices[i] = UnitCost(offers[i], levels[i]);
                if (naturalRefresh && expeditionEconomy.HasResolvedAction("event:pending:E04C"))
                    listedPrices[i] = Mathf.Max(1, listedPrices[i] - 3);
                offerIds[i] = StableRunEconomyId("offer", runId, currentMapNodeId,
                    refreshSerial.ToString(), i.ToString());
            }
            if (naturalRefresh)
            {
                ConsumeNaturalRecruitEffects();
            }
            else if (!string.IsNullOrEmpty(target))
            {
                expeditionEconomy.ConsumeTargetedRecruitTicket(target);
            }
        }

        private string NaturalRecruitTargetMarker()
        {
            var marker = expeditionEconomy.FindRuleMarker("event:pending:E04A:");
            if (!string.IsNullOrEmpty(marker))
                return "profession=" + marker.Substring("event:pending:E04A:".Length);
            marker = expeditionEconomy.FindRuleMarker("event:pending:E04B:");
            if (!string.IsNullOrEmpty(marker))
                return "faction=" + marker.Substring("event:pending:E04B:".Length);
            marker = expeditionEconomy.FindRuleMarker("preparation:P12-target:");
            return string.IsNullOrEmpty(marker)
                ? string.Empty
                : marker.Substring("preparation:P12-target:".Length);
        }

        private void ConsumeNaturalRecruitEffects()
        {
            var marker = expeditionEconomy.FindRuleMarker("event:pending:E04A:");
            if (!string.IsNullOrEmpty(marker)) expeditionEconomy.ConsumeRuleMarker(marker);
            marker = expeditionEconomy.FindRuleMarker("event:pending:E04B:");
            if (!string.IsNullOrEmpty(marker)) expeditionEconomy.ConsumeRuleMarker(marker);
            expeditionEconomy.ConsumeRuleMarker("event:pending:E04C");
            marker = expeditionEconomy.FindRuleMarker("preparation:P12-target:");
            if (!string.IsNullOrEmpty(marker)) expeditionEconomy.ConsumeRuleMarker(marker);
            expeditionEconomy.TryConsumePreparation("P12");
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
            var refreshId = StableRunEconomyId(
                "refresh", runId, currentMapNodeId,
                (shopRefreshSerial + 1).ToString());
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
                GenerateShopOfferSnapshot(shopRefreshSerial + 1, false, out offers, out levels, out offerIds,
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
            if (receipt.ResultCode == RunEconomyResultCode.Success)
                RecordFormalShopRefresh(
                    actionId, expectedVersion, refreshId);
            ApplyRunEconomyStatus(receipt.ResultCode, "status.shopRefreshed");
            SaveGame();
        }

        private string NewRunEconomyActionId(string actionType)
        {
            return (actionType ?? "action") + "-" + Guid.NewGuid().ToString("N");
        }

        private static string StableRunEconomyId(string prefix, params string[] parts)
        {
            return RunEconomyIdentity.StableId(prefix, parts);
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
            var definition = characterCatalog.Get(nameKey);
            return QualityRecruitPriceRules.QuoteListedPrice(
                definition.ShopCost,
                ShopOfferQuality(nameKey),
                Mathf.Clamp(level, 1, RunUnitRules.MaximumRunLevel),
                qualityRecruitPriceRulesVersion);
        }

        private int ShopOfferListedPrice(int offerIndex, string definitionId, int runLevel)
        {
            if (shopOfferListedPrices != null && offerIndex >= 0 &&
                offerIndex < shopOfferListedPrices.Length && shopOfferListedPrices[offerIndex] > 0)
                return shopOfferListedPrices[offerIndex];
            return UnitCost(definitionId, runLevel);
        }

        private CharacterQuality ShopOfferQuality(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId)) return CharacterQuality.Common;
            if (SkillDiagnosticActive) return ResolveDiagnosticQuality(definitionId);
            if (qualityRecruitPriceRulesVersion ==
                QualityRecruitPriceRules.QualityScaledRulesVersion)
            {
                var lockedInstanceId = runDeck == null
                    ? null
                    : runDeck.GetCharacterInstanceId(definitionId);
                OwnedCharacterState exactLockedCharacter;
                if (string.IsNullOrEmpty(lockedInstanceId) || playerCollection == null ||
                    !playerCollection.TryGetByInstanceId(lockedInstanceId, out exactLockedCharacter) ||
                    exactLockedCharacter.DefinitionId != definitionId)
                {
                    throw new InvalidOperationException(
                        "Current quality recruitment pricing requires an exact locked deck instance.");
                }
                return exactLockedCharacter.Quality;
            }
            OwnedCharacterState lockedCharacter;
            return TryGetLockedRunCharacter(definitionId, out lockedCharacter)
                ? lockedCharacter.Quality
                : CharacterQuality.Common;
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
                   DetailTideWindowLayout.FormatAttackSpeed(stats.AttackSpeedBasisPoints);
        }

        private string SecondaryStatsText(UnitData unit)
        {
            return T("armorShort") + " " + unit.Armor + "  ·  " +
                   DetailTideWindowLayout.FormatAttackSpeed(unit.AttackSpeedBasisPoints);
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
                PlayUiClick(UiAudioCueIds.Disabled, "preparation-start-without-unit");
                statusKey = "status.needUnit";
                return;
            }
            PlayUiClick(UiAudioCueIds.BattleStart, "preparation-start-battle");
            formationOverlayTransitionActive = true;
            formationOverlayTransitionProgress = 0f;
            battleControlsEnabled = false;
            ResetPreparationPointer();
            StartCoroutine(BeginFormationToBattle());
        }

        private void StartArenaBattle()
        {
            if (arenaBattleActive || ArenaAttackUnitCount() == 0 || selectedArenaOpponentIndex < 0 ||
                selectedArenaOpponentIndex >= arenaOpponents.Length) return;
            arenaBattleId = "arena-" + Guid.NewGuid().ToString("N");
            arenaAttackLanternSnapshot = CurrentLanternSnapshot();
            arenaBattlePowerSnapshot = ArenaAttackPower();
            arenaResultCommitted = false;
            arenaResultReturnPending = false;
            arenaBattleActive = true;
            battlePaused = false;
            battleControlsEnabled = true;
            battleSceneOpacity = 1f;
            battlePresentationActive = true;
            StartCoroutine(ResolveArenaBattle());
        }

        private IEnumerator ResolveArenaBattle()
        {
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            c05BarrierOwnerByColumn.Clear();
            persistentMilaHiddenSpringSeeds.Clear();
            battleHealthLagPercents.Clear();
            consumedBattleFeedbackKeys.Clear();
            consumedBattleAudioActionKeys.Clear();
            battleAudioEventSequence = 0;
            CreateArenaCombatTeams();
            yield return WaitForBattleSeconds(0.24f);

            var result = DeterministicBattle.Run(CreateArenaBattleInput());
            lastBattleEventCount = result.Events.Count;
            lastBattleHash = ComputeBattleHash(result.Events);
            for (var eventIndex = 0; eventIndex < result.Events.Count;)
            {
                var firstEvent = result.Events[eventIndex];
                var group = new List<BattleEvent> { firstEvent };
                eventIndex++;
                while (eventIndex < result.Events.Count && CanResolveBattleEventsTogether(firstEvent, result.Events[eventIndex]))
                {
                    group.Add(result.Events[eventIndex]);
                    eventIndex++;
                }
                yield return group.Count == 1 ? PlayBattleEvent(firstEvent) : PlayBattleEventGroup(group);
            }
            battlePresentation.EndCue();
            arenaLastBattleWon = result.PlayerWon;
            arenaPreviousRating = arenaRating;
            arenaRatingDelta = ArenaPrototypeRules.TemporaryRatingDelta(arenaLastBattleWon);
            arenaRating = Mathf.Max(0, arenaRating + arenaRatingDelta);
            yield return WaitForBattleSeconds(0.42f);
            battleControlsEnabled = false;
            battlePaused = false;
            arenaBattleActive = false;
            battleSceneOpacity = 1f;
            arenaResultCommitted = true;
            arenaPageState = ArenaPageState.Result;
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            c05BarrierOwnerByColumn.Clear();
            persistentMilaHiddenSpringSeeds.Clear();
            battleHealthLagPercents.Clear();
            consumedBattleFeedbackKeys.Clear();
            arenaOpponents = ArenaPrototypeRules.CreateDeterministicBots(arenaRating);
            SaveGame();
        }

        private void CreateArenaCombatTeams()
        {
            var players = new List<BattleUnitPresentationState>();
            var enemies = new List<BattleUnitPresentationState>();
            for (var slot = 0; slot < arenaAttackSlots.Length; slot++)
            {
                var unit = arenaAttackSlots[slot];
                if (unit != null)
                {
                    var adjusted = CreateLanternAdjustedCopy(unit, arenaAttackLanternSnapshot);
                    players.Add(CreateBattleUnitPresentation(adjusted, true, slot, "arena-player-" + slot));
                }
            }
            ApplyLanternOpeningShield(players, arenaAttackLanternSnapshot);
            var opponent = arenaOpponents[selectedArenaOpponentIndex];
            for (var slot = 0; slot < opponent.DefenseUnits.Length && slot < ArenaPrototypeRules.DefenseSlotCount; slot++)
            {
                var unit = CreateArenaProfileUnit(opponent.DefenseUnits[slot]);
                enemies.Add(CreateBattleUnitPresentation(unit, false, slot, "arena-enemy-" + slot));
            }
            battlePresentation.BeginBattle(players, enemies);
        }

        private BattleInput CreateArenaBattleInput()
        {
            var input = new BattleInput { Seed = unchecked((uint)(arenaRating * 1009 + selectedArenaOpponentIndex * 53 + arenaChallengesRemaining)) };
            var playerDefinitionIds = new List<string>();
            for (var slot = 0; slot < arenaAttackSlots.Length; slot++)
                if (arenaAttackSlots[slot] != null) playerDefinitionIds.Add(arenaAttackSlots[slot].NameKey);
            var synergy = FormationSynergy.Calculate(characterCatalog, playerDefinitionIds);
            for (var slot = 0; slot < arenaAttackSlots.Length; slot++)
            {
                var unit = arenaAttackSlots[slot];
                if (unit != null)
                {
                    var adjusted = CreateLanternAdjustedCopy(unit, arenaAttackLanternSnapshot);
                    input.PlayerUnits.Add(ToArenaBattleUnitState(adjusted, "arena-player-" + slot, slot, true, synergy));
                }
            }
            ApplyLanternOpeningShield(input.PlayerUnits, arenaAttackLanternSnapshot);
            var opponent = arenaOpponents[selectedArenaOpponentIndex];
            var enemyDefinitionIds = new List<string>();
            for (var slot = 0; slot < opponent.DefenseUnits.Length && slot < ArenaPrototypeRules.DefenseSlotCount; slot++)
                enemyDefinitionIds.Add(opponent.DefenseUnits[slot].DefinitionId);
            var enemySynergy = FormationSynergy.Calculate(characterCatalog, enemyDefinitionIds);
            for (var slot = 0; slot < opponent.DefenseUnits.Length && slot < ArenaPrototypeRules.DefenseSlotCount; slot++)
            {
                var unit = CreateArenaProfileUnit(opponent.DefenseUnits[slot]);
                input.EnemyUnits.Add(ToArenaBattleUnitState(unit, "arena-enemy-" + slot, slot, false, enemySynergy));
            }
            return input;
        }

        private BattleUnitState ToArenaBattleUnitState(UnitData unit, string id, int slot, bool isPlayer,
            FormationSynergySnapshot synergy)
        {
            var definition = characterCatalog.Get(unit.NameKey);
            return new BattleUnitState
            {
                Id = id,
                DefinitionId = unit.NameKey,
                IsPlayer = isPlayer,
                Slot = slot,
                Health = unit.MaxHealth,
                MaxHealth = unit.MaxHealth,
                AttackDamage = unit.AttackDamage,
                HealAmount = unit.HealAmount,
                Armor = unit.Armor + (isPlayer && expeditionEconomy.ActiveBattleItemId == "I01" ? 8 : 0),
                Shield = isPlayer && expeditionEconomy.ActiveBattleItemId == "I03"
                    ? Mathf.CeilToInt(unit.OpeningShield * 1.25f)
                    : unit.OpeningShield,
                SpeedTier = unit.SpeedTier,
                BaseAttackSpeedBasisPoints = unit.AttackSpeedBasisPoints,
                IsHealer = unit.IsHealer,
                LowHealthActionPercent = isPlayer && synergy != null && synergy.IsFactionActive("faction.wild") && definition.FactionId == "faction.wild" ? 15 : 0,
                FirstActionPercent = isPlayer && synergy != null && synergy.IsFactionActive("faction.starlight") && definition.FactionId == "faction.starlight" ? 25 : 0,
                ExecuteDamagePercent = isPlayer && synergy != null && synergy.IsProfessionActive("profession.output") && definition.ProfessionId == "profession.output" ? 12 : 0,
                FactionId = definition.FactionId,
                ProfessionId = definition.ProfessionId,
                Quality = unit.Quality
            };
        }

        private IEnumerator BeginFormationToBattle()
        {
            if (!ExpeditionEconomyAuthorityPolicy.AllowsLocalRewardMutation(
                    UsesServerAuthoritativeExpedition) &&
                !IsCurrentFormalExpeditionRunBound())
            {
                statusKey = "status.authoritativeExpeditionRequired";
                yield break;
            }
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
            c05BarrierOwnerByColumn.Clear();
            persistentMilaHiddenSpringSeeds.Clear();
            battleHealthLagPercents.Clear();
            consumedBattleFeedbackKeys.Clear();
            consumedBattleAudioActionKeys.Clear();
            battleAudioEventSequence = 0;
            CreateCombatTeams();
            yield return WaitForBattleSeconds(0.24f);

            var battleInput = CreateBattleInput();
            var result = DeterministicBattle.Run(battleInput);
            RecordFormalBattleResolution(battleInput, result);
            lastBattleEventCount = result.Events.Count;
            lastBattleHash = ComputeBattleHash(result.Events);
            for (var eventIndex = 0; eventIndex < result.Events.Count;)
            {
                var firstEvent = result.Events[eventIndex];
                var group = new List<BattleEvent> { firstEvent };
                eventIndex++;
                while (eventIndex < result.Events.Count &&
                       CanResolveBattleEventsTogether(firstEvent, result.Events[eventIndex]))
                {
                    group.Add(result.Events[eventIndex]);
                    eventIndex++;
                }
                yield return group.Count == 1
                    ? PlayBattleEvent(firstEvent)
                    : PlayBattleEventGroup(group);
            }
            battlePresentation.EndCue();

            var playerWon = result.PlayerWon;
            var activatedBattleItemId = expeditionEconomy.ActiveBattleItemId;
            var activatedBattleItemInstanceId = expeditionEconomy.ActiveBattleItemInstanceId;
            if (playerWon)
            {
                var completedNode = expeditionMap == null ? null : expeditionMap.GetNode(currentMapNodeId);
                var completedBattleNode = AwardCompletedBattleNode();
                if (!SkillDiagnosticActive && completedBattleNode &&
                    !expeditionEconomy.HasResolvedAction("event:pending:E02C"))
                    coins += encounterReward;
                if (!SkillDiagnosticActive && completedBattleNode &&
                    expeditionEconomy.HasResolvedAction("event:pending:E02B"))
                    coins += 8;
                if (!SkillDiagnosticActive && completedBattleNode && encounterReward > 0 &&
                    completedNode != null && completedNode.Type != ExpeditionNodeType.Boss &&
                    expeditionEconomy.TryConsumePreparation("P05"))
                    coins += encounterReward;
                if (!string.IsNullOrEmpty(activatedBattleItemId) &&
                    expeditionEconomy.PendingPreparationId == "P06")
                {
                    expeditionEconomy.TryAddCombatItem(
                        activatedBattleItemId, activatedBattleItemInstanceId);
                }
                if (!string.IsNullOrEmpty(activatedBattleItemId) &&
                    expeditionEconomy.HasResolvedAction("event:pending:E03C"))
                    expeditionEconomy.TryAddCombatItem(
                        activatedBattleItemId, activatedBattleItemInstanceId);
                expeditionEconomy.ClearActiveBattleItem();
                // P06 binds to the next combat and expires at that combat's end,
                // even when the player chooses not to activate a backpack item.
                expeditionEconomy.TryConsumePreparation("P06");
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
            var runEnded = health == 0 || (playerWon && round >= FinalRound);
            if (!playerWon && runEnded)
            {
                expeditionEconomy.ClearActiveBattleItem();
                expeditionEconomy.TryConsumePreparation("P06");
            }
            // A failed non-terminal attempt retries the same bound encounter.
            // Battle preparations, event effects and the committed item therefore
            // remain unchanged until victory or terminal failure.
            if (playerWon || runEnded)
            {
                expeditionEconomy.TryConsumePreparation("P01");
                expeditionEconomy.TryConsumePreparation("P02");
                expeditionEconomy.TryConsumePreparation("P03");
                expeditionEconomy.TryConsumePreparation("P04");
                CompleteEventBattleContracts(runEnded);
            }
            yield return WaitForBattleSeconds(0.42f);
            if (UsesServerAuthoritativeExpedition)
                yield return CommitFormalBattleAuthority(playerWon, runEnded);
            else
                formalExpeditionAuthorityCommitSucceeded = true;
            if (!formalExpeditionAuthorityCommitSucceeded)
            {
                ReturnAfterFormalAuthorityCommitFailure();
                yield break;
            }
            PersistFormalBattleResolution();
            if (runEnded)
            {
                expeditionEconomy.ConsumeNextBattlePreparation();
                expeditionEconomy.ClearActiveBattleItem();
            }
            if (runEnded && !UsesServerAuthoritativeExpedition)
                PrepareLocalExpeditionSettlement(playerWon && round >= FinalRound);
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
            c05BarrierOwnerByColumn.Clear();
            persistentMilaHiddenSpringSeeds.Clear();
            battleHealthLagPercents.Clear();
            consumedBattleFeedbackKeys.Clear();
            if (!SkillDiagnosticActive) SaveGame();
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

        private static bool CanResolveBattleEventsTogether(BattleEvent first, BattleEvent next)
        {
            return first != null && next != null && first.ActionTime == next.ActionTime &&
                   string.Equals(first.ActorId, next.ActorId, StringComparison.Ordinal);
        }

        private IEnumerator PlayBattleEventGroup(IList<BattleEvent> events)
        {
            var cue = battlePresentation.BeginCueGroup(events);
            var duration = cue.DurationSeconds;
            var elapsed = 0f;
            var applied = false;
            while (elapsed < duration)
            {
                battlePresentation.SetCueProgress(elapsed / duration);
                if (!applied && cue.Progress >= cue.ImpactProgress)
                {
                    for (var index = 0; index < events.Count; index++)
                        ApplyBattleEvent(events[index]);
                    applied = true;
                }
                if (!battlePaused) elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            if (!applied)
                for (var index = 0; index < events.Count; index++)
                    ApplyBattleEvent(events[index]);
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
                input.PlayerUnits.Add(ToBattleUnitState(unit, "player-" + slot, slot, true, synergy,
                    runUnit.Quality));
            }
            ApplyLanternOpeningShield(input.PlayerUnits, expeditionLanternSnapshot);
            for (var index = 0; index < enemyUnits.Count; index++)
            {
                var formationSlot = EnemyFormationSlotForRosterIndex(index);
                if (formationSlot < 0) continue;
                input.EnemyUnits.Add(ToBattleUnitState(
                    enemyUnits[index], "enemy-" + index, formationSlot, false, null));
            }
            if (currentRoguelikeRuntimeGeneration == ChapterOneRoguelikeV11State.RuntimeGeneration)
            {
                input.PlayerRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration;
                input.PlayerKeystoneId = roguelikeBuildV11.SelectedKeystoneId;
                input.PlayerRoguelikeEffects.AddRange(roguelikeBuildV11.NormalEffects);
            }
            else
            {
                input.PlayerRoguelikeEffects.AddRange(roguelikeBuild.AcquiredEffects);
            }
            return input;
        }

        private BattleUnitState ToBattleUnitState(
            UnitData unit,
            string id,
            int slot,
            bool isPlayer,
            FormationSynergySnapshot synergy,
            CharacterQuality quality = CharacterQuality.Common)
        {
            var definition = characterCatalog.Get(unit.NameKey);
            var eventArmor = isPlayer && expeditionEconomy.HasResolvedAction("event:pending:E01A")
                ? 20 : isPlayer && expeditionEconomy.HasResolvedAction("event:pending:E03B") ? 12 : 0;
            var eventHealthMultiplier = isPlayer &&
                                        expeditionEconomy.HasResolvedAction("event:pending:E01C")
                ? 0.85f : 1f;
            var eventAttackMultiplier = isPlayer &&
                                        expeditionEconomy.HasResolvedAction("event:pending:E01B")
                ? 1.20f : 1f;
            var eventHealMultiplier = isPlayer &&
                                      expeditionEconomy.HasResolvedAction("event:pending:E01B")
                ? 0.75f : 1f;
            var eventSpeed = isPlayer && expeditionEconomy.HasResolvedAction("event:pending:E01A")
                ? -1000 : isPlayer && expeditionEconomy.HasResolvedAction("event:pending:E01C")
                    ? 2500 : 0;
            return new BattleUnitState
            {
                Id = id,
                DefinitionId = unit.NameKey,
                IsPlayer = isPlayer,
                Slot = slot,
                Health = Mathf.Max(1, Mathf.CeilToInt(unit.MaxHealth * eventHealthMultiplier)),
                MaxHealth = Mathf.Max(1, Mathf.CeilToInt(unit.MaxHealth * eventHealthMultiplier)),
                AttackDamage = Mathf.Max(0, Mathf.CeilToInt(unit.AttackDamage * eventAttackMultiplier)),
                HealAmount = Mathf.Max(0, Mathf.CeilToInt(unit.HealAmount * eventHealMultiplier)),
                Armor = unit.Armor + eventArmor,
                Shield = unit.OpeningShield,
                SpeedTier = unit.SpeedTier,
                BaseAttackSpeedBasisPoints = unit.AttackSpeedBasisPoints > 0
                    ? unit.AttackSpeedBasisPoints
                    : DeterministicBattle.AttackSpeedBasisPoints(unit.SpeedTier),
                AttackSpeedBonusBasisPoints = isPlayer && expeditionEconomy.PendingPreparationId == "P02"
                    ? 2500 + eventSpeed
                    : isPlayer ? eventSpeed
                    : !isPlayer && expeditionEconomy.ActiveBattleItemId == "I09" ? -1500 : 0,
                IsHealer = unit.IsHealer,
                LowHealthActionPercent = isPlayer && synergy.IsFactionActive("faction.wild") &&
                                         definition.FactionId == "faction.wild" ? 15 : 0,
                FirstActionPercent = isPlayer && synergy.IsFactionActive("faction.starlight") &&
                                     definition.FactionId == "faction.starlight" ? 25 : 0,
                ExecuteDamagePercent = isPlayer && synergy.IsProfessionActive("profession.output") &&
                                       definition.ProfessionId == "profession.output" ? 12 :
                    unit.NameKey == "enemy.n4.skirmisher" ? 15 :
                    unit.NameKey == "enemy.e2.hunter" || unit.NameKey == "enemy.b2.hunter" ? 25 : 0,
                ArmorPenetration = isPlayer
                    ? (expeditionEconomy.ActiveBattleItemId == "I04" ? 10 : 0) +
                      (expeditionEconomy.PendingPreparationId == "P03" ? 15 : 0)
                    : 0,
                MerchantDirectDamageTakenReductionBasisPoints = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I02" ? 1500 : 0,
                MerchantDirectDamageReductionExpiresAt = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I02" ? 12000 : 0,
                MerchantShieldGainBonusBasisPoints = isPlayer
                    ? (expeditionEconomy.ActiveBattleItemId == "I03" ? 2500 : 0) +
                      (expeditionEconomy.HasResolvedAction("event:pending:E03B") ? 2000 : 0)
                    : 0,
                MerchantLowHealthDirectDamageBonusBasisPoints = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I05" ? 2000 : 0,
                MerchantDirectAttackDamageBonusBasisPoints = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I06" ? 4000 : 0,
                MerchantDirectAttackBonusHitsRemaining = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I06" ? 3 : 0,
                MerchantActiveHealingBonusBasisPoints = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I07" ? 2500 : 0,
                MerchantActiveActionBonusBasisPoints = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I08" ? 3000 : 0,
                MerchantActiveActionBonusActionsRemaining = isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I08" ? 3 : 0,
                MerchantTimedAttackSpeedBonusBasisPoints = isPlayer &&
                    expeditionEconomy.PendingPreparationId == "P02" ? 2500 :
                    !isPlayer && expeditionEconomy.ActiveBattleItemId == "I09" ? -1500 : 0,
                MerchantAttackSpeedBonusExpiresAt = (isPlayer &&
                    expeditionEconomy.PendingPreparationId == "P02") || (!isPlayer &&
                    expeditionEconomy.ActiveBattleItemId == "I09") ? 12000 : 0,
                FactionId = definition.FactionId,
                ProfessionId = definition.ProfessionId,
                Quality = quality
            };
        }

        private void CompleteEventBattleContracts(bool runEnded)
        {
            expeditionEconomy.ConsumeRuleMarker("event:pending:E01A");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E01B");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E01C");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E02B");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E02C");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E03B");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E03C");
            if (!runEnded) return;
            expeditionEconomy.ConsumeRuleMarker("event:pending:E04C");
            expeditionEconomy.ConsumeRuleMarker(expeditionEconomy.FindRuleMarker("event:pending:E04A:"));
            expeditionEconomy.ConsumeRuleMarker(expeditionEconomy.FindRuleMarker("event:pending:E04B:"));
            expeditionEconomy.ConsumeRuleMarker("event:pending:E05A");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E05B");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E05C");
        }

        private void ApplyBattleEvent(BattleEvent battleEvent)
        {
            PlayBattleAudioForEvent(battleEvent);
            QueueBattleFloatingNumber(battleEvent);
            battlePresentation.Apply(battleEvent);
            SpawnBattleFeedback(battleEvent);
        }

        private void PlayBattleAudioForEvent(BattleEvent battleEvent)
        {
            if (battleEvent == null || battleAudioPlayer == null) return;
            var sourceEffectId = battleEvent.SourceEffectId ?? string.Empty;
            var actionKey = string.Concat(battleEvent.ActorId, "|", battleEvent.ActionTime,
                "|", sourceEffectId);
            if (!battleEvent.IsSecondary && sourceEffectId.StartsWith("S_", StringComparison.Ordinal) &&
                consumedBattleAudioActionKeys.Add(actionKey))
            {
                var started = new CardBattleEvent
                {
                    Kind = CardBattleEventKind.ActionStarted,
                    BattleId = runId,
                    EventSequence = ++battleAudioEventSequence,
                    ActionId = actionKey,
                    AbilityId = sourceEffectId,
                    SourceId = battleEvent.ActorId,
                    TargetId = battleEvent.TargetId,
                    AuthorityTick = battleEvent.ActionTime,
                    Outcome = CardBattleActionOutcome.Completed
                };
                PlayBattleAudioCues(battleAudioRouter.Consume(started));
            }

            var kind = battleEvent.Type == BattleEventType.Attack ||
                       battleEvent.Type == BattleEventType.Heal
                ? CardBattleEventKind.ImpactResolved
                : battleEvent.Type == BattleEventType.Status
                    ? CardBattleEventKind.StatusFeedback
                    : (CardBattleEventKind?)null;
            if (!kind.HasValue) return;
            var abilityId = sourceEffectId;
            if (string.IsNullOrEmpty(abilityId))
            {
                if (battleEvent.Type == BattleEventType.Heal) abilityId = "A_BASIC_SUPPORT";
                else if (battleEvent.Type == BattleEventType.Attack)
                {
                    var actor = battlePresentation.FindView(battleEvent.ActorId);
                    var isRanged = actor != null && actor.State != null &&
                        IsRangedBasicAttacker(actor.State.DefinitionId);
                    abilityId = isRanged ? "A_BASIC_RANGED" : "A_BASIC_MELEE";
                }
                else abilityId = "STATUS_GENERIC";
            }
            var eventData = new CardBattleEvent
            {
                Kind = kind.Value,
                BattleId = runId,
                EventSequence = ++battleAudioEventSequence,
                ActionId = actionKey,
                AbilityId = abilityId,
                SourceId = battleEvent.ActorId,
                TargetId = battleEvent.TargetId,
                AuthorityTick = battleEvent.ActionTime,
                Outcome = CardBattleActionOutcome.Completed,
                StatusKey = sourceEffectId
            };
            if (kind.Value == CardBattleEventKind.ImpactResolved)
            {
                eventData.Results.Add(new CardBattleTargetResult
                {
                    ChildIndex = battleEvent.ChildIndex,
                    TargetId = battleEvent.TargetId,
                    Damage = battleEvent.Type == BattleEventType.Attack ? battleEvent.Amount : 0,
                    Healing = battleEvent.Type == BattleEventType.Heal ? battleEvent.Amount : 0,
                    ShieldDelta = battleEvent.TargetShieldAfter,
                    TargetHealthAfter = battleEvent.TargetHealthAfter,
                    TargetShieldAfter = battleEvent.TargetShieldAfter,
                    IsDefeated = battleEvent.TargetHealthAfter <= 0
                });
            }
            PlayBattleAudioCues(battleAudioRouter.Consume(eventData));
        }

        private static bool IsRangedBasicAttacker(string definitionId)
        {
            return string.Equals(definitionId, "unit.healer", StringComparison.Ordinal) ||
                   string.Equals(definitionId, "unit.starpriest", StringComparison.Ordinal) ||
                   string.Equals(definitionId, "unit.archer", StringComparison.Ordinal) ||
                   string.Equals(definitionId, "unit.thornGuard", StringComparison.Ordinal) ||
                   string.Equals(definitionId, "enemy.n3.ranged", StringComparison.Ordinal);
        }

        private void PlayBattleAudioCues(IReadOnlyList<BattleAudioCue> cues)
        {
            if (cues == null || battleAudioPlayer == null) return;
            for (var index = 0; index < cues.Count; index++)
            {
                battleAudioPlayer.TryPlay(cues[index]);
                battleAudioRouter.CompleteCue(cues[index]);
            }
        }

        private void SpawnBattleFeedback(BattleEvent battleEvent)
        {
            if (battleEvent == null) return;
            if (battleEvent.TargetShieldAfter <= 0)
            {
                StopPersistentN1HoldGateStamp(battleEvent.TargetId);
                StopPersistentE1SteadyLineFeedback(battleEvent.TargetId);
            }
            var isOpenDawnCast = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C01ActiveSkillIntegration.OpenDawnAbilityId, StringComparison.Ordinal);
            var isOlinTideStatus = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C01ActiveSkillIntegration.TideAbilityId, StringComparison.Ordinal);
            if (isOlinTideStatus)
            {
                if (battleEvent.Amount < 0)
                    StopPersistentOlinTideFeedback(battleEvent.TargetId);
                else
                    StartOrRefreshPersistentOlinTideFeedback(battleEvent.TargetId);
                return;
            }
            var isReturnBeaconStatus = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C02ActiveSkillIntegration.ReturnBeaconAbilityId, StringComparison.Ordinal);
            if (isReturnBeaconStatus)
            {
                if (battleEvent.Amount < 0)
                    StopPersistentReturnBeaconFeedback(battleEvent.TargetId);
                else
                    SetPersistentReturnBeaconFeedback(battleEvent.TargetId, battleEvent.Amount);
                return;
            }
            var isC06FireCharge = battleEvent.Type == BattleEventType.Status &&
                (string.Equals(battleEvent.SourceEffectId,
                    C06ActiveSkillIntegration.ShareFireAbilityId, StringComparison.Ordinal) ||
                 string.Equals(battleEvent.SourceEffectId,
                    C06ActiveSkillIntegration.FireChargeStatusId, StringComparison.Ordinal));
            if (isC06FireCharge)
            {
                if (battleEvent.Amount > 0)
                    SetPersistentC06FireCharges(battleEvent.TargetId, battleEvent.Amount);
                else
                    StopPersistentC06FireCharges(battleEvent.TargetId);
                return;
            }
            var isC06ReturnFireBuff = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C06ActiveSkillIntegration.ReturnFireBuffStatusId, StringComparison.Ordinal);
            if (isC06ReturnFireBuff)
            {
                if (battleEvent.Amount > 0)
                    StartPersistentC06ReturnFireBuff(battleEvent.TargetId);
                else
                    StopPersistentC06ReturnFireBuff(battleEvent.TargetId);
                return;
            }
            var isN1HoldGateStatus = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    N1ActiveSkillIntegration.HoldGateAbilityId, StringComparison.Ordinal);
            if (isN1HoldGateStatus)
            {
                if (battleEvent.Amount > 0)
                    SpawnN1HoldGateStamp(battleEvent.TargetId);
                else
                    StopPersistentN1HoldGateStamp(battleEvent.TargetId);
                return;
            }
            var isE1SteadyLineStatus = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    E1ActiveSkillIntegration.SteadyLineAbilityId, StringComparison.Ordinal);
            if (isE1SteadyLineStatus)
            {
                if (battleEvent.Amount > 0)
                    StartPersistentE1SteadyLineFeedback(battleEvent.TargetId);
                else
                    StopPersistentE1SteadyLineFeedback(battleEvent.TargetId);
                return;
            }
            var isE2TrackJudgment = battleEvent.Type == BattleEventType.Attack &&
                !battleEvent.IsSecondary && string.Equals(battleEvent.SourceEffectId,
                    E2ActiveSkillIntegration.TrackJudgmentAbilityId, StringComparison.Ordinal);
            if (isE2TrackJudgment)
            {
                SpawnE2TrackJudgmentFeedback(battleEvent.TargetId);
                return;
            }
            var isE3BellReturn = string.Equals(battleEvent.SourceEffectId,
                E3ActiveSkillIntegration.BellReturnAbilityId, StringComparison.Ordinal);
            if (isE3BellReturn && battleEvent.Type == BattleEventType.Status &&
                battleEvent.Amount > 0 && battleEvent.ChildIndex == 0)
                SpawnE3BellReturnFeedback(battleEvent.ActorId);
            var isB1BlockadeQuake = battleEvent.Type == BattleEventType.Attack &&
                string.Equals(battleEvent.SourceEffectId,
                    B1ActiveSkillIntegration.BlockadeQuakeAbilityId, StringComparison.Ordinal);
            if (isB1BlockadeQuake && battleEvent.ChildIndex == 0)
                SpawnB1BlockadeQuakeFeedback(battleEvent.ActorId);
            var isB2Recall = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    B2ActiveSkillIntegration.RecallAbilityId, StringComparison.Ordinal) &&
                battleEvent.Amount > 0;
            if (isB2Recall)
            {
                var b2Source = battlePresentation.FindView(battleEvent.ActorId);
                if (b2Source != null && b2Source.State != null)
                    battleFeedbackVisuals.Add(new BattleFeedbackVisual
                    {
                        TargetId = battleEvent.TargetId,
                        ProfileId = BattleFeedbackProfileIds.B2RecallLost,
                        Position = BattleBaseCenterFor(b2Source),
                        Width = BattleBaseWidthFor(b2Source.State) * 0.95f,
                        DurationSeconds = BattleProjectileProfiles.B2RecallDurationSeconds,
                        ElapsedSeconds = 0f,
                        StartDelaySeconds = 0f,
                        HeightOverride = 0f,
                        Motion = Vector2.zero,
                        RenderScale = 1f
                    });
                return;
            }
            var isB2DeviantLight = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    B2ActiveSkillIntegration.DeviantLightAbilityId, StringComparison.Ordinal);
            if (isB2DeviantLight)
            {
                if (battleEvent.Amount < 0)
                    StopPersistentB2DeviantLightFeedback(battleEvent.TargetId);
                else
                    StartPersistentB2DeviantLightFeedback(battleEvent.TargetId);
                return;
            }
            var isN2CrossCut = battleEvent.Type == BattleEventType.Attack &&
                !battleEvent.IsSecondary && string.Equals(battleEvent.SourceEffectId,
                    N2ActiveSkillIntegration.CrossCutAbilityId, StringComparison.Ordinal);
            if (isN2CrossCut)
            {
                var n2Target = battlePresentation.FindView(battleEvent.TargetId);
                if (n2Target == null || n2Target.State == null) return;
                var center = n2Target.GetAnchor(BattlePresentationIds.HitAnchor);
                var width = BattleBaseWidthFor(n2Target.State) * 0.95f;
                var diagonalTravel = width * 0.72f;
                var firstOffset = new Vector2(diagonalTravel, -diagonalTravel);
                var secondOffset = new Vector2(-diagonalTravel, -diagonalTravel);
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = battleEvent.TargetId,
                    ProfileId = BattleFeedbackProfileIds.N2CrossCutSlash,
                    Position = center + firstOffset,
                    Width = width,
                    DurationSeconds = 0.29f,
                    ElapsedSeconds = 0f,
                    StartDelaySeconds = 0f,
                    HeightOverride = 0f,
                    Motion = -firstOffset,
                    RenderScale = 1f,
                    MirrorX = false
                });
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = battleEvent.TargetId,
                    ProfileId = BattleFeedbackProfileIds.N2CrossCutSlash,
                    Position = center + secondOffset,
                    Width = width,
                    DurationSeconds = 0.29f,
                    ElapsedSeconds = 0f,
                    StartDelaySeconds = 0.09f,
                    HeightOverride = 0f,
                    Motion = -secondOffset,
                    RenderScale = 1f,
                    MirrorX = true
                });
                return;
            }
            var isN4MistHunt = battleEvent.Type == BattleEventType.Attack &&
                !battleEvent.IsSecondary && string.Equals(battleEvent.SourceEffectId,
                    N4ActiveSkillIntegration.MistHuntAbilityId, StringComparison.Ordinal);
            if (isN4MistHunt)
            {
                var n4Target = battlePresentation.FindView(battleEvent.TargetId);
                var n4Source = battlePresentation.FindView(battleEvent.ActorId);
                if (n4Target == null || n4Target.State == null) return;
                if (LoadBattleVfxSprite(BattleProjectileProfiles.N4MistHuntCrescentResourcePath) != null)
                {
                    var n4TargetCenter = n4Target.GetAnchor(BattlePresentationIds.HitAnchor);
                    var n4Mirror = n4Source != null && n4Source.GetAnchor(BattlePresentationIds.BodyAnchor).x >
                        n4Target.GetAnchor(BattlePresentationIds.BodyAnchor).x;
                    var n4Travel = BattleBaseWidthFor(n4Target.State) * 0.72f;
                    var n4Side = n4Mirror ? -1f : 1f;
                    var n4StartOffset = new Vector2(n4Side * n4Travel, n4Travel * 0.08f);
                    battleFeedbackVisuals.Add(new BattleFeedbackVisual
                    {
                        TargetId = battleEvent.TargetId,
                        ProfileId = BattleFeedbackProfileIds.N4MistHuntCrescent,
                        Position = n4TargetCenter + n4StartOffset,
                        Width = BattleBaseWidthFor(n4Target.State) * 1.1f,
                        DurationSeconds = 0.22f,
                        ElapsedSeconds = 0f,
                        StartDelaySeconds = 0f,
                        HeightOverride = 0f,
                        Motion = -n4StartOffset,
                        ArcControlOffset = new Vector2(n4Side * n4Travel * 0.55f, -n4Travel * 0.24f),
                        RenderScale = 1f,
                        MirrorX = n4Mirror,
                        UseArcMotion = true
                    });
                    return;
                }
            }
            var isWhiteTrailStatus = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C03ActiveSkillIntegration.WhiteTrailAbilityId, StringComparison.Ordinal);
            if (isWhiteTrailStatus)
            {
                if (battleEvent.Amount < 0)
                    StopPersistentWhiteTrailFeedback(battleEvent.TargetId);
                else
                    SetPersistentWhiteTrailFeedback(battleEvent.TargetId);
                return;
            }
            var isWhiteTrailTransfer = battleEvent.Type == BattleEventType.Status &&
                battleEvent.Amount == DeterministicBattle.LeifuWhiteTrailActionCount &&
                string.Equals(battleEvent.SourceEffectId,
                    C03ActiveSkillIntegration.TransferAbilityId, StringComparison.Ordinal);
            if (isWhiteTrailTransfer)
            {
                StopPersistentWhiteTrailFeedback(battleEvent.ActorId);
                SpawnWhiteTrailTransferFeedback(battleEvent.ActorId, battleEvent.TargetId);
                SetPersistentWhiteTrailFeedback(battleEvent.TargetId);
                return;
            }
            var isMilaAfterSpringRelease = battleEvent.Type == BattleEventType.Status &&
                battleEvent.Amount >= 0 &&
                string.Equals(battleEvent.SourceEffectId,
                    C04ActiveSkillIntegration.AfterSpringAbilityId, StringComparison.Ordinal);
            var isMilaHiddenSpringStatus = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C04ActiveSkillIntegration.HiddenSpringAbilityId, StringComparison.Ordinal);
            if (isMilaHiddenSpringStatus)
            {
                if (battleEvent.Amount > 0)
                    SetPersistentMilaHiddenSpringSeeds(battleEvent.TargetId, battleEvent.Amount);
                else
                    StopPersistentMilaHiddenSpringSeeds(battleEvent.TargetId);
            }
            if (isMilaAfterSpringRelease)
            {
                StopPersistentMilaHiddenSpringSeeds(battleEvent.TargetId);
                var releaseTarget = battlePresentation.FindView(battleEvent.TargetId);
                if (releaseTarget != null && releaseTarget.State != null && releaseTarget.State.IsAlive)
                    battleFeedbackVisuals.Add(new BattleFeedbackVisual
                    {
                        TargetId = battleEvent.TargetId,
                        ProfileId = BattleFeedbackProfileIds.C04AfterSpringRelease,
                        Position = BattleBaseCenterFor(releaseTarget),
                        Width = BattleBaseWidthFor(releaseTarget.State) * 1.1f,
                        DurationSeconds = 0.72f,
                        ElapsedSeconds = 0f,
                        StartDelaySeconds = 0f,
                        HeightOverride = 0f,
                        Motion = Vector2.zero,
                        RenderScale = 1f
                    });
                return;
            }
            if (isOpenDawnCast)
            {
                var skillTarget = battlePresentation.FindView(battleEvent.TargetId);
                if (skillTarget == null || skillTarget.State == null || !skillTarget.State.IsAlive) return;
                var skillBaseWidth = BattleBaseWidthFor(skillTarget.State);
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = battleEvent.TargetId,
                    ProfileId = BattleFeedbackProfileIds.OpenDawn,
                    Position = BattleBaseCenterFor(skillTarget),
                    Width = skillBaseWidth * 1.45f,
                    DurationSeconds = 0.72f,
                    ElapsedSeconds = 0f,
                    StartDelaySeconds = 0.08f,
                    HeightOverride = 0f,
                    Motion = Vector2.zero,
                    RenderScale = 1f
                });
                return;
            }
            var isC05BarrierStart = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C05ActiveSkillIntegration.RebuildBridgeAbilityId, StringComparison.Ordinal);
            if (isC05BarrierStart)
            {
                StartPersistentC05BarrierFeedback(battleEvent.TargetId);
                return;
            }
            var isC05BarrierRemoved = battleEvent.Type == BattleEventType.Status &&
                (string.Equals(battleEvent.SourceEffectId, C05ActiveSkillIntegration.BarrierBrokenStatusId,
                    StringComparison.Ordinal) ||
                 (battleEvent.SourceEffectId ?? string.Empty).StartsWith(
                    C05ActiveSkillIntegration.BarrierStatusId + ":", StringComparison.Ordinal));
            if (isC05BarrierRemoved)
            {
                StopPersistentC05BarrierFeedback(battleEvent.TargetId);
                if (string.Equals(battleEvent.SourceEffectId,
                        C05ActiveSkillIntegration.BarrierBrokenStatusId, StringComparison.Ordinal))
                    SpawnC05BarrierBreakFeedback(battleEvent.TargetId);
                return;
            }
            var isC05ColumnBuff = battleEvent.Type == BattleEventType.Status &&
                string.Equals(battleEvent.SourceEffectId,
                    C05ActiveSkillIntegration.ColumnActionBuffStatusId, StringComparison.Ordinal);
            if (isC05ColumnBuff)
            {
                if (battleEvent.Amount < 0)
                    StopPersistentC05ColumnBuffFeedback(battleEvent.TargetId);
                else
                    StartPersistentC05ColumnBuffFeedback(battleEvent.TargetId);
                return;
            }
            var isYiwenRescueHeal = battleEvent.IsSecondary &&
                battleEvent.Type == BattleEventType.Heal &&
                (string.Equals(battleEvent.SourceEffectId,
                     C02ActiveSkillIntegration.ReturnBeaconAbilityId, StringComparison.Ordinal) ||
                 string.Equals(battleEvent.SourceEffectId,
                     C02ActiveSkillIntegration.WaitingAbilityId, StringComparison.Ordinal));
            if (isYiwenRescueHeal)
                SpawnYiwenRescueBeam(battleEvent.ActorId, battleEvent.TargetId);
            var isLeechHeal = battleEvent != null && battleEvent.IsSecondary &&
                battleEvent.Type == BattleEventType.Heal &&
                string.Equals(battleEvent.SourceEffectId, "B06", StringComparison.Ordinal);
            var isOpenDawnHit = battleEvent.IsSecondary && battleEvent.Type == BattleEventType.Attack &&
                string.Equals(battleEvent.SourceEffectId,
                    C01ActiveSkillIntegration.OpenDawnAbilityId, StringComparison.Ordinal);
            var isC05Counter = battleEvent.IsSecondary && battleEvent.Type == BattleEventType.Attack &&
                string.Equals(battleEvent.SourceEffectId,
                    C05ActiveSkillIntegration.HoldBridgeAbilityId, StringComparison.Ordinal);
            if ((battleEvent.IsSecondary && !isLeechHeal && !isOpenDawnHit && !isYiwenRescueHeal &&
                 !isC05Counter) ||
                (battleEvent.Type != BattleEventType.Attack &&
                 battleEvent.Type != BattleEventType.Heal)) return;
            var feedbackKey = string.Concat(
                battleEvent.Type, "|", battleEvent.ActorId, "|", battleEvent.TargetId, "|",
                battleEvent.Amount, "|", battleEvent.ActionTime, "|", battleEvent.SourceEffectId);
            if (!consumedBattleFeedbackKeys.Add(feedbackKey)) return;
            var target = battlePresentation.FindView(battleEvent.TargetId);
            if (target == null || target.State == null) return;
            var feedbackTarget = target;
            var feedbackTargetId = battleEvent.TargetId;
            var removeC05BarrierMappingAfterImpact = false;
            if (!battleEvent.IsSecondary && battleEvent.Type == BattleEventType.Attack &&
                string.Equals(battleEvent.SourceEffectId,
                    C05ActiveSkillIntegration.HoldBridgeAbilityId, StringComparison.Ordinal))
            {
                string barrierOwnerId;
                if (c05BarrierOwnerByColumn.TryGetValue(C05ColumnForSlot(target.State.Slot), out barrierOwnerId))
                {
                    var barrierOwner = battlePresentation.FindView(barrierOwnerId);
                    if (barrierOwner != null && barrierOwner.State != null && barrierOwner.State.IsAlive)
                    {
                        feedbackTarget = barrierOwner;
                        feedbackTargetId = barrierOwnerId;
                        removeC05BarrierMappingAfterImpact = !HasPersistentC05BarrierFeedback(barrierOwnerId);
                    }
                }
            }

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
                var baseCenter = BattleBaseCenterFor(feedbackTarget);
                var baseWidth = BattleBaseWidthFor(feedbackTarget.State);
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = feedbackTargetId,
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
                    TargetId = feedbackTargetId,
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
                ? BattleBaseCenterFor(feedbackTarget)
                : feedbackTarget.GetAnchor(BattlePresentationIds.HitAnchor);
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = feedbackTargetId,
                ProfileId = profileId,
                Position = position,
                Width = FeedbackWidthFor(profileId, feedbackTarget.State),
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
            if (removeC05BarrierMappingAfterImpact)
                c05BarrierOwnerByColumn.Remove(C05ColumnForSlot(target.State.Slot));
        }

        private bool HasPersistentC05BarrierFeedback(string targetId)
        {
            foreach (var feedback in battleFeedbackVisuals)
            {
                if (feedback.Persistent && string.Equals(feedback.TargetId, targetId, StringComparison.Ordinal) &&
                    string.Equals(feedback.ProfileId, BattleFeedbackProfileIds.C05Barrier,
                        StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private void SetPersistentMilaHiddenSpringSeeds(string targetId, int remainingActions)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            persistentMilaHiddenSpringSeeds[targetId] = Mathf.Clamp(remainingActions, 1, 2);
        }

        private void StopPersistentMilaHiddenSpringSeeds(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            persistentMilaHiddenSpringSeeds.Remove(targetId);
        }

        private void SetPersistentC06FireCharges(string targetId, int remainingCharges)
        {
            for (var index = battleFeedbackVisuals.Count - 1; index >= 0; index--)
            {
                var existing = battleFeedbackVisuals[index];
                if (string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) &&
                    string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C06FireCharge,
                        StringComparison.Ordinal))
                    battleFeedbackVisuals.RemoveAt(index);
            }
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            var count = Mathf.Clamp(remainingCharges, 1, C06ActiveSkillIntegration.NockSelfChargeCount);
            var center = target.GetAnchor(BattlePresentationIds.HealthBarAnchor) + new Vector2(0f, -20f);
            var width = BattleBaseWidthFor(target.State) * 0.096f;
            for (var index = 0; index < count; index++)
            {
                var offset = count == 1 ? 0f : (index - (count - 1) * 0.5f) * width * 0.9f;
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = targetId,
                    ProfileId = BattleFeedbackProfileIds.C06FireCharge,
                    Position = center + Vector2.right * offset,
                    Width = width,
                    DurationSeconds = float.MaxValue,
                    ElapsedSeconds = 0f,
                    StartDelaySeconds = index * 0.04f,
                    HeightOverride = 0f,
                    Motion = Vector2.zero,
                    RenderScale = 1f,
                    Persistent = true
                });
            }
        }

        private void StopPersistentC06FireCharges(string targetId)
        {
            for (var index = battleFeedbackVisuals.Count - 1; index >= 0; index--)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C06FireCharge,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.DurationSeconds = 0.18f;
            }
        }

        private void StartPersistentC06ReturnFireBuff(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C06ReturnFireBuff,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = true;
                existing.FadingOut = false;
                existing.DurationSeconds = float.MaxValue;
                return;
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.C06ReturnFireBuff,
                Position = BattleBaseCenterFor(target),
                Width = BattleBaseWidthFor(target.State),
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentC06ReturnFireBuff(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C06ReturnFireBuff,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.DurationSeconds = 0.22f;
            }
        }

        private void StartOrRefreshPersistentOlinTideFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.OlinTide,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = true;
                existing.FadingOut = false;
                existing.DurationSeconds = float.MaxValue;
                return;
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.OlinTide,
                Position = BattleBaseCenterFor(target),
                Width = BattleBaseWidthFor(target.State) * 1.1f,
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentOlinTideFeedback(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.OlinTide,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.24f;
            }
        }

        private void SetPersistentReturnBeaconFeedback(string targetId, int remainingActions)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = battleFeedbackVisuals.Count - 1; index >= 0; index--)
            {
                var existing = battleFeedbackVisuals[index];
                if (string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) &&
                    string.Equals(existing.ProfileId, BattleFeedbackProfileIds.ReturnBeaconRing,
                        StringComparison.Ordinal))
                    battleFeedbackVisuals.RemoveAt(index);
            }
            var ringCount = Mathf.Clamp(remainingActions, 1, DeterministicBattle.YiwenReturnBeaconActionCount);
            var baseCenter = BattleBaseCenterFor(target);
            var baseWidth = BattleBaseWidthFor(target.State);
            for (var index = 0; index < ringCount; index++)
            {
                battleFeedbackVisuals.Add(new BattleFeedbackVisual
                {
                    TargetId = targetId,
                    ProfileId = BattleFeedbackProfileIds.ReturnBeaconRing,
                    Position = baseCenter,
                    Width = baseWidth * (index == 0 ? 1.08f : 0.82f),
                    DurationSeconds = float.MaxValue,
                    ElapsedSeconds = 0f,
                    StartDelaySeconds = index * 0.06f,
                    HeightOverride = 0f,
                    Motion = Vector2.zero,
                    RenderScale = 1f,
                    Persistent = true
                });
            }
        }

        private void StopPersistentReturnBeaconFeedback(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.ReturnBeaconRing,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.24f;
            }
        }

        private void SpawnN1HoldGateStamp(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.N1HoldGateStamp,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = true;
                existing.FadingOut = false;
                existing.DurationSeconds = float.MaxValue;
                existing.ElapsedSeconds = 0f;
                return;
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.N1HoldGateStamp,
                Position = target.GetAnchor(BattlePresentationIds.BodyAnchor),
                Width = BattleBaseWidthFor(target.State) *
                    BattleProjectileProfiles.N1HoldGateStampDisplayScale * 0.78f,
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentN1HoldGateStamp(string targetId)
        {
            if (string.IsNullOrEmpty(targetId)) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.N1HoldGateStamp,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.22f;
            }
        }

        private void StartPersistentE1SteadyLineFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            StopPersistentE1SteadyLineFeedback(targetId);
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.E1SteadyLine,
                Position = BattleBaseCenterFor(target),
                Width = BattleBaseWidthFor(target.State) * 1.15f,
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentE1SteadyLineFeedback(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.E1SteadyLine,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.22f;
            }
        }

        private void SpawnE2TrackJudgmentFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.E2TrackJudgment,
                Position = target.GetAnchor(BattlePresentationIds.HitAnchor),
                Width = BattleBaseWidthFor(target.State) *
                    BattleProjectileProfiles.E2TrackJudgmentDisplayScale,
                DurationSeconds = BattleProjectileProfiles.E2TrackJudgmentDurationSeconds,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f
            });
        }

        private void SpawnE3BellReturnFeedback(string actorId)
        {
            var source = battlePresentation.FindView(actorId);
            if (source == null || source.State == null || !source.State.IsAlive) return;
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = actorId,
                ProfileId = BattleFeedbackProfileIds.E3BellReturn,
                Position = BattleBaseCenterFor(source),
                Width = BattleBaseWidthFor(source.State) * 1.55f,
                DurationSeconds = 0.62f,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f
            });
        }

        private void SpawnB1BlockadeQuakeFeedback(string actorId)
        {
            var source = battlePresentation.FindView(actorId);
            if (source == null || source.State == null || !source.State.IsAlive) return;
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = actorId,
                ProfileId = BattleFeedbackProfileIds.B1BlockadeQuake,
                Position = BattleBaseCenterFor(source),
                Width = BattleBaseWidthFor(source.State) * 2f,
                DurationSeconds = 0.46f,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f
            });
        }

        private void StartPersistentC05BarrierFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            var column = C05ColumnForSlot(target.State.Slot);
            c05BarrierOwnerByColumn[column] = targetId;
            var protectedUnits = target.State.IsPlayer
                ? battlePresentation.PlayerUnits : battlePresentation.EnemyUnits;
            foreach (var unit in protectedUnits)
            {
                if (unit == null || !unit.IsAlive || C05ColumnForSlot(unit.Slot) != column) continue;
                StartPersistentC05BarrierFeedbackForUnit(unit.Id);
            }
        }

        private void StartPersistentC05BarrierFeedbackForUnit(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) &&
                    string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C05Barrier,
                        StringComparison.Ordinal))
                {
                    existing.Persistent = true;
                    existing.FadingOut = false;
                    existing.DurationSeconds = float.MaxValue;
                    return;
                }
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.C05Barrier,
                Position = BattleBaseCenterFor(target),
                Width = BattleBaseWidthFor(target.State) * 1.15f,
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentC05BarrierFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null) return;
            var column = C05ColumnForSlot(target.State.Slot);
            var protectedUnits = target.State.IsPlayer
                ? battlePresentation.PlayerUnits : battlePresentation.EnemyUnits;
            foreach (var unit in protectedUnits)
            {
                if (unit == null || C05ColumnForSlot(unit.Slot) != column) continue;
                StopPersistentC05BarrierFeedbackForUnit(unit.Id);
            }
        }

        private void StopPersistentC05BarrierFeedbackForUnit(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C05Barrier,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.30f;
            }
        }

        private void SpawnC05BarrierBreakFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null) return;
            var column = C05ColumnForSlot(target.State.Slot);
            var protectedUnits = target.State.IsPlayer
                ? battlePresentation.PlayerUnits : battlePresentation.EnemyUnits;
            foreach (var unit in protectedUnits)
            {
                if (unit == null || !unit.IsAlive || C05ColumnForSlot(unit.Slot) != column) continue;
                SpawnC05BarrierBreakFeedbackForUnit(unit.Id);
            }
        }

        private void SpawnC05BarrierBreakFeedbackForUnit(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null) return;
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.C05BarrierBreak,
                Position = BattleBaseCenterFor(target),
                Width = BattleBaseWidthFor(target.State) * 1.15f,
                DurationSeconds = 0.38f,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f
            });
        }

        private void StartPersistentC05ColumnBuffFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) &&
                    string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                        StringComparison.Ordinal))
                {
                    existing.Persistent = true;
                    existing.FadingOut = false;
                    existing.DurationSeconds = float.MaxValue;
                    return;
                }
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.C05ColumnActionBuff,
                Position = BattleBaseCenterFor(target),
                Width = BattleBaseWidthFor(target.State),
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0.08f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentC05ColumnBuffFeedback(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C05ColumnActionBuff,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.22f;
            }
        }

        private void SpawnYiwenRescueBeam(string sourceId, string targetId)
        {
            var source = battlePresentation.FindView(sourceId);
            var target = battlePresentation.FindView(targetId);
            if (source == null || target == null || source.State == null || target.State == null) return;
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                SourceId = sourceId,
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.RescueBeam,
                Position = Vector2.zero,
                Width = 0f,
                DurationSeconds = 0.32f,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f
            });
        }

        private void SetPersistentWhiteTrailFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C03WhiteMark,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = true;
                existing.FadingOut = false;
                existing.DurationSeconds = float.MaxValue;
                return;
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.C03WhiteMark,
                Position = target.GetAnchor(BattlePresentationIds.BodyAnchor),
                Width = C03WhiteMarkWidthFor(target.State),
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentWhiteTrailFeedback(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.C03WhiteMark,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.20f;
            }
        }

        private void StartPersistentB2DeviantLightFeedback(string targetId)
        {
            var target = battlePresentation.FindView(targetId);
            if (target == null || target.State == null || !target.State.IsAlive) return;
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.B2DeviantLight,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = true;
                existing.FadingOut = false;
                existing.DurationSeconds = float.MaxValue;
                return;
            }
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.B2DeviantLight,
                Position = target.GetAnchor(BattlePresentationIds.BodyAnchor) + new Vector2(46f, -48f),
                Width = BattleBaseWidthFor(target.State) * 0.21f,
                DurationSeconds = float.MaxValue,
                ElapsedSeconds = 0f,
                StartDelaySeconds = 0f,
                HeightOverride = 0f,
                Motion = Vector2.zero,
                RenderScale = 1f,
                Persistent = true
            });
        }

        private void StopPersistentB2DeviantLightFeedback(string targetId)
        {
            for (var index = 0; index < battleFeedbackVisuals.Count; index++)
            {
                var existing = battleFeedbackVisuals[index];
                if (!string.Equals(existing.TargetId, targetId, StringComparison.Ordinal) ||
                    !string.Equals(existing.ProfileId, BattleFeedbackProfileIds.B2DeviantLight,
                        StringComparison.Ordinal)) continue;
                existing.Persistent = false;
                existing.FadingOut = true;
                existing.ElapsedSeconds = 0f;
                existing.StartDelaySeconds = 0f;
                existing.DurationSeconds = 0.22f;
            }
        }

        private void SpawnWhiteTrailTransferFeedback(string sourceId, string targetId)
        {
            var source = battlePresentation.FindView(sourceId);
            var target = battlePresentation.FindView(targetId);
            if (source == null || target == null || source.State == null || target.State == null) return;
            battleFeedbackVisuals.Add(new BattleFeedbackVisual
            {
                SourceId = sourceId,
                TargetId = targetId,
                ProfileId = BattleFeedbackProfileIds.C03MarkTransfer,
                Position = Vector2.zero,
                Width = 0f,
                DurationSeconds = 0.26f,
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
            if (string.Equals(profileId, BattleFeedbackProfileIds.N2CrossCutSlash,
                     StringComparison.Ordinal)) return BattleBaseWidthFor(target) * 0.95f;
            if (string.Equals(profileId, BattleFeedbackProfileIds.HostileHitFeedbackV2,
                    StringComparison.Ordinal)) return 84f * 1.5f;
            return 84f * 1.5f;
        }

        private float C03WhiteMarkWidthFor(BattleUnitPresentationState target)
        {
            return Mathf.Max(1f, BattleBaseWidthFor(target) * 0.72f * 0.80f);
        }

        private Vector2 BattleBaseCenterFor(BattleUnitViewDriver target)
        {
            if (target == null || target.State == null)
                return target == null ? Vector2.zero : target.GetAnchor(BattlePresentationIds.FeetAnchor);

            Texture2D equippedTexture;
            if (TryGetEquippedBaseTexture(target.State.IsPlayer, out equippedTexture))
            {
                var equippedCenterOffset = StoreCosmeticFormalArtContract.BaseHeight * 0.5f -
                                           StoreCosmeticFormalArtContract.BaseFootAnchorY;
                return target.GetAnchor(BattlePresentationIds.FeetAnchor) +
                       Vector2.up * equippedCenterOffset;
            }

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
            Texture2D equippedTexture;
            if (TryGetEquippedBaseTexture(unit.IsPlayer, out equippedTexture))
                return StoreCosmeticFormalArtContract.BaseWidth;

            CharacterArtDefinition artDefinition;
            Sprite sprite;
            var qualityId = CharacterQualityResourceId(unit.Quality);
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

        private static int C05ColumnForSlot(int slot)
        {
            return slot < 0 ? -1 : slot % 3;
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
            if (battleEvent == null ||
                (battleEvent.Type != BattleEventType.Attack && battleEvent.Type != BattleEventType.Heal) ||
                battleEvent.Amount <= 0) return;
            var cue = battleVisual;
            var target = battlePresentation.FindView(battleEvent.TargetId);
            if (cue == null || target == null) return;
            if (!battleEvent.IsSecondary && battleEvent.Type == BattleEventType.Attack &&
                string.Equals(battleEvent.SourceEffectId,
                    C05ActiveSkillIntegration.HoldBridgeAbilityId, StringComparison.Ordinal))
            {
                string barrierOwnerId;
                if (c05BarrierOwnerByColumn.TryGetValue(C05ColumnForSlot(target.State.Slot), out barrierOwnerId))
                {
                    var barrierOwner = battlePresentation.FindView(barrierOwnerId);
                    if (barrierOwner != null) target = barrierOwner;
                }
            }

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
            if (!expeditionFlow.CanChooseUpgrade)
            {
                PlayUiClick(UiAudioCueIds.Disabled, "upgrade-choice-unavailable");
                return;
            }
            var choiceSet = EnsureRoguelikeChoiceSet();
            if (choiceSet == null)
            {
                PlayUiClick(UiAudioCueIds.Disabled, "upgrade-choice-missing");
                return;
            }
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
            if (runReceipt.ResultCode != RunEconomyResultCode.Success || choiceReceipt == null)
            {
                PlayUiClick(UiAudioCueIds.Disabled, "upgrade-choice-failed");
                return;
            }
            PlayUiClick(UiAudioCueIds.Upgrade, "upgrade-choice");
            expeditionFlow.TryChooseUpgrade();
            roguelikeBuild.CompleteSelection();
            RecordFormalUpgradeSelection(actionId, expectedVersion, optionId);
            SaveGame();
        }

        private void ApplyUpgradeV11(string optionId)
        {
            if (!expeditionFlow.CanChooseUpgrade) return;
            var choiceSet = EnsureRoguelikeChoiceSetV11();
            if (choiceSet == null) return;
            if (choiceSet.requiredSelectionCount == 2 &&
                string.IsNullOrEmpty(pendingP09FirstOptionId))
            {
                pendingP09FirstOptionId = optionId;
                PlayUiClick(UiAudioCueIds.Upgrade, "upgrade-v11-first-choice");
                return;
            }
            if (choiceSet.requiredSelectionCount == 2 &&
                string.Equals(pendingP09FirstOptionId, optionId, StringComparison.Ordinal))
            {
                PlayUiClick(UiAudioCueIds.Disabled, "upgrade-v11-duplicate-choice");
                return;
            }

            var actionId = NewRunEconomyActionId("boon-v11");
            var expectedVersion = choiceSet.expectedRunStateVersion;
            ChapterOneRoguelikeSelectionReceipt choiceReceipt = null;
            var selectedOptionIds = choiceSet.requiredSelectionCount == 2
                ? new[] { pendingP09FirstOptionId, optionId }
                : new[] { optionId };
            var runReceipt = runEconomyActions.Execute(actionId, expectedVersion, () =>
            {
                choiceReceipt = roguelikeBuildV11.SelectManyAtomic(
                    choiceSet.choiceSetId, selectedOptionIds, actionId, expectedVersion);
                var code = (ChapterOneRoguelikeSelectionCode)choiceReceipt.resultCode;
                return code == ChapterOneRoguelikeSelectionCode.Success
                    ? RunEconomyResultCode.Success
                    : code == ChapterOneRoguelikeSelectionCode.StaleVersion
                        ? RunEconomyResultCode.StaleVersion
                        : RunEconomyResultCode.InvalidTarget;
            });
            if (runReceipt.ResultCode != RunEconomyResultCode.Success || choiceReceipt == null)
            {
                PlayUiClick(UiAudioCueIds.Disabled, "upgrade-v11-choice-failed");
                return;
            }

            if (expeditionEconomy.PendingPreparationId == "P09" ||
                expeditionEconomy.PendingPreparationId == "P10" ||
                expeditionEconomy.PendingPreparationId == "P11" &&
                choiceReceipt.acquiredStackCount >= 2)
                expeditionEconomy.TryConsumePreparation(expeditionEconomy.PendingPreparationId);
            expeditionEconomy.ConsumeRuleMarker("event:pending:E05A");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E05B");
            expeditionEconomy.ConsumeRuleMarker("event:pending:E05C");
            pendingP09FirstOptionId = null;
            roguelikeBuildV11.CompleteSelection();
            expeditionFlow.TryChooseUpgrade();
            RecordFormalUpgradeSelection(actionId, expectedVersion,
                string.Join(",", selectedOptionIds));
            PlayUiClick(UiAudioCueIds.Upgrade, "upgrade-v11-choice");
            SaveGame();
        }

        private ChapterOneRoguelikeChoiceSetV11 EnsureRoguelikeChoiceSetV11()
        {
            if (!expeditionFlow.CanChooseUpgrade) return null;
            roguelikeBuildV11.SynchronizeRunStateVersion(runEconomyActions.StateVersion);
            var node = expeditionMap == null ? null : expeditionMap.GetNode(currentMapNodeId);
            var preparation = expeditionEconomy.PendingPreparationId;
            var eventP10 = expeditionEconomy.HasResolvedAction("event:pending:E05A");
            var eventP11 = expeditionEconomy.HasResolvedAction("event:pending:E05B");
            var eventP09 = expeditionEconomy.HasResolvedAction("event:pending:E05C");
            return roguelikeBuildV11.EnsureChoiceSet(runId, currentMapNodeId,
                node != null && node.Type == ExpeditionNodeType.Boss,
                node == null ? 0 : Mathf.RoundToInt(node.JourneyProgress * 100f),
                currentEncounterIsElite, CreateRoguelikeEligibilityV11(),
                ChapterOneLanternRoute.None,
                preparation == "P09" || eventP09,
                preparation == "P10" || eventP10,
                preparation == "P11" || eventP11);
        }

        private ChapterOneRoguelikeEligibility CreateRoguelikeEligibilityV11()
        {
            var result = new ChapterOneRoguelikeEligibility();
            for (var slot = 0; slot < runRoster.BoardCapacity; slot++)
            {
                var runUnit = runRoster.Get(slot);
                if (runUnit == null) continue;
                result.HasAnyAlly = true;
                var definition = characterCatalog.Get(runUnit.DefinitionId);
                var view = CreateRunUnitView(runUnit);
                if (view.AttackDamage > 0) result.DirectAttackerCount++;
                result.HasSteelOrGuardian |= definition.FactionId == "faction.steel" ||
                                             definition.ProfessionId == "profession.guardian";
                result.HasGuard |= definition.ProfessionId == "profession.guardian";
                result.HasOutput |= definition.ProfessionId == "profession.output";
                result.HasSupport |= definition.ProfessionId == "profession.support";
                result.HasActiveHeal |= view.IsHealer || view.HealAmount > 0;
                result.HasShieldSource |= view.OpeningShield > 0 ||
                                          definition.ProfessionId == "profession.guardian";
                result.HasFireSource |= runUnit.DefinitionId == "unit.starpriest";
                if (slot < 3 && runRoster.Get(slot + 3) != null)
                    result.HasFrontAndSameColumnBack = true;
            }
            return result;
        }

        private static string RoguelikeV11NameKey(string id)
        {
            return id != null && id.StartsWith("K", StringComparison.Ordinal)
                ? ChapterOneRoguelikeV11Rules.GetKeystone(id).NameKey
                : ChapterOneRoguelikeV11Rules.GetNormal(id).NameKey;
        }

        private string RoguelikeV11CardDescription(ChapterOneRoguelikeCandidate option)
        {
            if (option.Id.StartsWith("K", StringComparison.Ordinal))
                return T("rogue." + option.Id + ".description") + "\n" +
                       T("rogue.keystone.unique");
            var definition = ChapterOneRoguelikeV11Rules.GetNormal(option.Id);
            var value = definition.Value((RoguelikeRarity)option.RarityId);
            var secondaryValue = definition.SecondaryValue((RoguelikeRarity)option.RarityId);
            var stacks = RoguelikeEnhancementRules.StackCount(
                roguelikeBuildV11.NormalEffects, option.Id);
            string effect;
            if (option.Id == "B03" || option.Id == "B10")
                effect = string.Format(T("rogue." + option.Id + ".description"), value);
            else if (option.Id == "B09")
                effect = string.Format(T("rogue.B09.description"),
                    RoguelikePercent(value), RoguelikePercent(secondaryValue));
            else
                effect = string.Format(T("rogue." + option.Id + ".description"),
                    RoguelikePercent(value));
            return effect + "\n" +
                   T("rogue.stack") + " " + stacks + "/" + definition.MaximumStacks;
        }

        private static string RoguelikePercent(int basisPoints)
        {
            return (basisPoints / 100f).ToString("0.##");
        }

        private RoguelikeChoiceSet EnsureRoguelikeChoiceSet()
        {
            if (!expeditionFlow.CanChooseUpgrade) return null;
            if (IsDiagnosticRunId(runId) && roguelikeDiagnosticSuppressChoiceSet) return null;
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

            var sourceNodeId = currentMapNodeId;
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
            RecordFormalRouteSelection(sourceNodeId, selected.Id);
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
            var formalRun = IsCurrentFormalExpeditionRunBound();
            if (UsesServerAuthoritativeExpedition && !formalRun) return false;

            // The client still needs a local completion marker so the bound run can
            // advance and award run-scoped coins. Persistent rewards and lighthouse
            // progression remain server-authoritative and therefore stay at zero.
            if (formalRun)
                return ExpeditionProgression.TryCompleteNode(
                    expeditionRewards, node.Id, 0, null);

            if (!ExpeditionEconomyAuthorityPolicy.AllowsLocalRewardMutation(
                    UsesServerAuthoritativeExpedition)) return false;
            var tideCrystals = 1;
            string equipmentTemplateId = null;
            switch (node.Type)
            {
                case ExpeditionNodeType.EliteBattle:
                    tideCrystals = 2;
                    break;
                case ExpeditionNodeType.HighRiskBattle:
                    tideCrystals = 3;
                    break;
                case ExpeditionNodeType.Boss:
                    tideCrystals = 10;
                    var definitions = equipmentCatalog.Definitions;
                    var index = (int)(expeditionMapSeed % (uint)definitions.Count);
                    equipmentTemplateId = definitions[index].TemplateId;
                    break;
            }
            if (!currentExpeditionRewardsEnabled)
            {
                tideCrystals = 0;
                equipmentTemplateId = null;
            }
            if (!ExpeditionProgression.TryCompleteNode(
                    expeditionRewards, node.Id,
                    tideCrystals, equipmentTemplateId)) return false;
            if (node.Type == ExpeditionNodeType.Boss)
            {
                chapterOneLighthouse.RecordBossVictory();
                if (!string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId))
                    chapterOneLighthouse.RecordChapterLanternAward();
                chapterOneLighthouse.TryLight();
            }
            return true;
        }

        private bool CanStoreExpeditionRewards(ExpeditionRewardSnapshot rewards)
        {
            if (!ExpeditionEconomyAuthorityPolicy.AllowsLocalRewardMutation(
                    UsesServerAuthoritativeExpedition)) return false;
            var includesCompletionLantern = runWon && currentExpeditionRewardsEnabled;
            return rewards != null && WarehouseStorageRules.CanAccept(
                equipmentInventory.Instances.Count,
                CompetitionDemoRules.IncomingRewardSlots(
                    rewards.EquipmentTemplateIds.Count, includesCompletionLantern),
                lanternInventory.Instances.Count);
        }

        private bool ClaimExpeditionRewards()
        {
            if (!ExpeditionEconomyAuthorityPolicy.AllowsLocalRewardMutation(
                    UsesServerAuthoritativeExpedition)) return false;
            var pending = expeditionRewards.CreateSnapshot();
            if (!CanStoreExpeditionRewards(pending)) return false;
            ExpeditionRewardSnapshot claimed;
            if (!expeditionRewards.TryClaim(out claimed)) return false;
            var localPlan = EnsureLocalExpeditionSettlementPlan();
            tideCrystalBalance += claimed.TideCrystals;
            CreditLocalSettlementMaterials(localPlan);
            foreach (var templateId in claimed.EquipmentTemplateIds)
            {
                var instanceId = "local.reward." + nextLocalEquipmentSerial++;
                while (!equipmentInventory.TryAdd(new EquipmentInstanceState(
                    instanceId, templateId, EquipmentQuality.Common, "local-test")))
                {
                    instanceId = "local.reward." + nextLocalEquipmentSerial++;
                }
            }
            if (runWon && currentExpeditionRewardsEnabled)
            {
                var lantern = CompetitionDemoRules.CreateCompletionLantern(runId, expeditionMapSeed);
                lanternInventory.TryAdd(lantern);
            }
            SaveGame();
            return true;
        }

        private void RestartRun()
        {
            if (!ExpeditionEconomyAuthorityPolicy.AllowsLocalRewardMutation(
                    UsesServerAuthoritativeExpedition))
            {
                return;
            }
            RestartExpeditionState("status.recruit");
            var energy = ExpeditionEnergyRules.BeginExpedition(expeditionEnergy);
            expeditionEnergy = energy.RemainingEnergy;
            currentExpeditionRewardsEnabled = energy.RewardsEnabled;
            expeditionEntryCommitted = true;
            SaveGame();
        }

        private void AbandonRun()
        {
            if (IsCurrentFormalExpeditionRunBound())
            {
                if (formalExpeditionAuthorityCommitInProgress) return;
                if (!CanCommitCurrentFormalAbandonment())
                {
                    statusKey = "status.authoritativeAbandonUnavailable";
                    confirmAbandon = false;
                    return;
                }
                StartCoroutine(CommitFormalAbandonment());
                return;
            }
            CompleteLocalAbandonRun();
        }

        private void CompleteLocalAbandonRun()
        {
            StopAllCoroutines();
            battlePresentation.Reset();
            battleFloatingNumbers.Clear();
            battleFeedbackVisuals.Clear();
            c05BarrierOwnerByColumn.Clear();
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
            PrepareLocalExpeditionSettlement(false);
            ReleaseRunEquipmentLocks();
            currentView = ScreenView.Expedition;
            SaveGame();
        }

        private void SaveGame()
        {
            if (SkillDiagnosticActive || IsDiagnosticRunId(runId) ||
                expeditionFlow == null || expeditionFlow.IsBattling ||
                runRoster == null || runDeck == null ||
                nextRunDeckSelection == null || playerCollection == null)
            {
                return;
            }
            EnsureRunEconomyIdentity();
            ReconcileUnavailableLanternSnapshots();
            ReconcileUnavailableEquipmentSnapshots();
            ReconcileWarehouseLayout();
            var data = new PrototypeRunSaveData
            {
                saveVersion = SaveSchemaVersion,
                runEconomyRulesVersion = currentRunEconomyRulesVersion,
                qualityRecruitPriceRulesVersion = qualityRecruitPriceRulesVersion,
                runId = runId,
                runStateVersion = runEconomyActions.StateVersion,
                coins = coins,
                health = health,
                round = round,
                mapSeed = unchecked((int)expeditionMapSeed),
                mapGenerationVersion = expeditionMap == null
                    ? ExpeditionMapGraph.MapGenerationVersion
                    : expeditionMap.GenerationVersion,
                mapVisualGenerationVersion = ExpeditionMapVisualSelector.VisualGenerationVersion,
                currentMapNodeId = currentMapNodeId,
                runAttackBonus = runAttackBonus,
                runHealthBonus = runHealthBonus,
                roguelikeRulesVersion = RoguelikeRulesVersion,
                attackSpeedRulesVersion = AttackSpeedRulesVersion,
                roguelikeRunStateVersion = currentRoguelikeRuntimeGeneration ==
                    ChapterOneRoguelikeV11State.RuntimeGeneration
                    ? roguelikeBuildV11.StateVersion : roguelikeBuild.StateVersion,
                roguelikeChoiceOrdinal = currentRoguelikeRuntimeGeneration ==
                    ChapterOneRoguelikeV11State.RuntimeGeneration
                    ? roguelikeBuildV11.ChoiceOrdinal : roguelikeBuild.ChoiceOrdinal,
                pendingRoguelikeChoiceSet = roguelikeBuild.PendingChoiceSet,
                acquiredRoguelikeEffects = roguelikeBuild.CopyAcquiredEffects(),
                roguelikeChoiceReceipts = roguelikeBuild.CopyReceipts(),
                roguelikeRuntimeGeneration = currentRoguelikeRuntimeGeneration,
                selectedRoguelikeKeystoneId = roguelikeBuildV11.SelectedKeystoneId,
                pendingRoguelikeChoiceSetV11 = roguelikeBuildV11.PendingChoiceSet,
                acquiredRoguelikeEffectsV11 = roguelikeBuildV11.CopyNormalEffects(),
                roguelikeChoiceReceiptsV11 = roguelikeBuildV11.CopyReceipts(),
                encounterPowerBonus = encounterPowerBonus,
                encounterReward = encounterReward,
                currentEncounterIsElite = currentEncounterIsElite,
                phase = (int)expeditionFlow.Phase,
                runWon = runWon,
                runAbandoned = runAbandoned,
                finalScore = finalScore,
                expeditionEnergy = expeditionEnergy,
                skrBalance = skrBalance,
                skrSubunitMicro = skrSubunitMicro,
                expeditionRewardsEnabled = currentExpeditionRewardsEnabled,
                expeditionEntryCommitted = expeditionEntryCommitted,
                tideCrystalBalance = tideCrystalBalance,
                localLampGlowBalance = localLampGlowBalance,
                localTideIronBalance = localTideIronBalance,
                localMistLeatherBalance = localMistLeatherBalance,
                localStarSandBalance = localStarSandBalance,
                localBountyTicketBalance = localBountyTicketBalance,
                nextLocalEquipmentSerial = nextLocalEquipmentSerial,
                pendingTideCrystals = expeditionRewards.TideCrystals,
                pendingEquipmentTemplateIds = CopyIds(expeditionRewards.EquipmentTemplateIds),
                completedRewardNodeIds = CopyIds(expeditionRewards.CompletedNodeIds),
                expeditionRewardsClaimed = expeditionRewards.IsClaimed,
                chapterOneBossDefeated = chapterOneLighthouse.BossDefeated,
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
                expeditionBackpackItemInstanceIds = CopyIds(expeditionEconomy.BackpackInstanceIds),
                targetedRecruitTickets = CopyIds(expeditionEconomy.TargetedRecruitTickets),
                resolvedEconomyActionIds = expeditionEconomy.CopyResolvedActionIds(),
                activeBattleItemId = expeditionEconomy.ActiveBattleItemId,
                activeBattleItemInstanceId = expeditionEconomy.ActiveBattleItemInstanceId,
                activeBattleTargetSlot = expeditionEconomy.ActiveBattleTargetSlot,
                lockedEconomyContentKeys = expeditionEconomy.CopyLockedNodeContentKeys(),
                lockedEconomyContentValues = expeditionEconomy.CopyLockedNodeContentValues(),
                resolvedRunEconomyActionIds = runEconomyActions.CopyActionIds(),
                resolvedRunEconomyActionResultCodes = runEconomyActions.CopyResultCodes(),
                resolvedRunEconomyActionVersions = runEconomyActions.CopyResultingVersions(),
                lastRunEconomyActionId = runEconomyActions.LastActionId,
                lastRunEconomyResultCode = (int)runEconomyActions.LastResultCode,
                collection = new PrototypeCollectionSaveData[playerCollection.Entries.Count],
                favoriteCharacterInstanceIds = characterFavorites.CopyInstanceIds(),
                storePurchaseSerial = storePurchaseSerial,
                storeEpicPitySkr = storeEpicPitySkr,
                storeLegendaryPitySkr = storeLegendaryPitySkr,
                ownedBasePackIds = ownedBasePackIds.ToArray(),
                equippedBasePackProductId = equippedBasePackProductId,
                equippedBattlefieldProductId = equippedBattlefieldProductId,
                storeOpeningActive = storeOpeningActive,
                storeOpeningProgress = storeOpeningProgress,
                pendingStoreOrderId = pendingStoreOrderId,
                pendingStoreInstanceIds = CopyIds(pendingStoreInstanceIds),
                pendingStoreDefinitionIds = CopyIds(pendingStoreDefinitionIds),
                pendingStoreQualities = pendingStoreQualities == null
                    ? new int[0] : (int[])pendingStoreQualities.Clone(),
                pendingStoreAptitudeRolls = pendingStoreAptitudeRolls == null
                    ? new int[0] : (int[])pendingStoreAptitudeRolls.Clone(),
                pendingStoreRevealAlreadyGranted = pendingStoreRevealAlreadyGranted,
                pendingStarterGrantId = pendingStarterGrantId,
                acknowledgedStarterGrantId = acknowledgedStarterGrantId,
                equipment = new PrototypeEquipmentSaveData[equipmentInventory.Instances.Count],
                warehouseSlotTokens = (string[])warehouseSlotTokens.Clone(),
                warehouseAcquisitionOrder = warehouseAcquisitionOrder.ToArray(),
                warehouseSortMode = (int)warehouseSortMode,
                activeRunLoadouts = new PrototypeLoadoutSnapshotSaveData[runEquipmentSnapshots.Count],
                defenseUnits = new PrototypeUnitSaveData[defenseSlots.Length],
                arenaRating = arenaRating,
                arenaChallengesRemaining = arenaChallengesRemaining,
                arenaChallengeDayKey = arenaChallengeDayKey,
                lanternInventory = PrototypeLanternSaveCodec.SaveInventory(lanternInventory),
                activeLanternInstanceId = lanternInventory.ActiveLanternInstanceId,
                expeditionLanternSnapshot = PrototypeLanternSaveCodec.SaveSnapshot(expeditionLanternSnapshot),
                expeditionRewardSnapshot = PrototypeLanternSaveCodec.SaveRewardSnapshot(expeditionRewardSnapshot),
                chapterOneLanternAwarded = chapterOneLighthouse.ChapterLanternAwarded,
                chapterOneLighthouseLit = chapterOneLighthouse.IsLit,
                arenaBattleId = arenaBattleId,
                arenaAttackLanternSnapshot = PrototypeLanternSaveCodec.SaveSnapshot(arenaAttackLanternSnapshot),
                arenaDefenseLanternSnapshot = PrototypeLanternSaveCodec.SaveSnapshot(arenaDefenseLanternSnapshot),
                arenaDefenseSnapshotVersion = arenaDefenseSnapshotVersion,
                arenaDefenseNeedsResync = arenaDefenseNeedsResync,
                marketSeeded = marketSeeded,
                marketLeaseSeasonId = MarketSeasonId,
                marketShopLeaseCount = marketBoard.ShopLeaseCount,
                marketStateVersion = marketBoard.StateVersion,
                marketBurnPoolMicroSkr = marketBoard.BurnPoolMicroSkr,
                marketTeamRevenueMicroSkr = marketBoard.TeamRevenueMicroSkr,
                marketListingSerial = marketBoard.ListingSerial,
                marketReceiptSerial = marketBoard.ReceiptSerial,
                marketListings = SaveMarketListings(),
                marketReceipts = SaveMarketReceipts()
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
                    quality = (int)owned.Quality,
                    acquiredAtUtcTicks = owned.AcquiredAtUtcTicks,
                    aptitudeRoll = owned.AptitudeRoll
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
                data.units[i].quality = (int)runUnit.Quality;
                data.units[i].aptitudeRoll = runUnit.AptitudeRoll;
                data.units[i].sourceBusinessId = runUnit.SourceBusinessId;
                data.units[i].externalStar = runUnit.ExternalStar;
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
                data.benchUnits[i].quality = (int)runUnit.Quality;
                data.benchUnits[i].aptitudeRoll = runUnit.AptitudeRoll;
                data.benchUnits[i].sourceBusinessId = runUnit.SourceBusinessId;
                data.benchUnits[i].externalStar = runUnit.ExternalStar;
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
                data.defenseUnits[i].basePower = unit.BasePower;
                data.defenseUnits[i].runLevel = unit.RunLevel;
                data.defenseUnits[i].quality = (int)unit.Quality;
                data.defenseUnits[i].aptitudeRoll = unit.AptitudeRoll;
                data.defenseUnits[i].externalStar = unit.ExternalStar;
                data.defenseUnits[i].characterInstanceId = unit.CharacterInstanceId;
                data.defenseUnits[i].maxHealth = unit.MaxHealth;
                data.defenseUnits[i].attackDamage = unit.AttackDamage;
                data.defenseUnits[i].healAmount = unit.HealAmount;
                data.defenseUnits[i].isHealer = unit.IsHealer;
            }
            PrototypeSaveStore.Save(SaveKey, data);
        }

        private static bool IsDiagnosticRunId(string value)
        {
            return !string.IsNullOrEmpty(value) &&
                value.StartsWith("diag-C", StringComparison.OrdinalIgnoreCase);
        }

        private bool LoadGame()
        {
            PrototypeRunSaveData data;
            if (!PrototypeSaveStore.TryLoad(SaveKey, out data) || data.units == null) return false;

            RestoreCollectionAndNextDeck(data);
            characterFavorites.Restore(data.favoriteCharacterInstanceIds, playerCollection);
            RestoreEquipment(data.equipment);
            RestoreRunEquipmentSnapshots(data.activeRunLoadouts);
            RestoreDefense(data.defenseUnits);
            RestoreLanternContracts(data);
            RestoreWarehouseLayout(data);
            ReconcileUnavailableEquipmentSnapshots(HasMarketLockedEquipment());
            pendingDefenseRemovedCount = ReconcileDefenseWithActiveDeck();
            arenaRating = data.saveVersion >= 32 && data.arenaRating > 0
                ? data.arenaRating : ArenaPrototypeRules.TemporaryInitialRating;
            arenaChallengeDayKey = data.arenaChallengeDayKey;
            arenaChallengesRemaining = ArenaPrototypeRules.NormalizeChallengesRemaining(
                arenaChallengeDayKey,
                data.saveVersion >= 32 ? data.arenaChallengesRemaining : ArenaPrototypeRules.DailyChallengeLimit,
                DateTime.Now);
            arenaChallengeDayKey = ArenaPrototypeRules.DayKey(DateTime.Now);
            arenaOpponents = ArenaPrototypeRules.CreateDeterministicBots(arenaRating);
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
                skrSubunitMicro = data.saveVersion >= 38
                    ? Mathf.Clamp(data.skrSubunitMicro, 0, (int)MarketRules.MicroSkrPerSkr - 1)
                    : 0;
                currentExpeditionRewardsEnabled = data.expeditionRewardsEnabled;
            }
            if (data.saveVersion >= 38) RestoreMarketState(data);
            storePurchaseSerial = Mathf.Max(0, data.storePurchaseSerial);
            storeEpicPitySkr = Mathf.Max(0, data.storeEpicPitySkr);
            storeLegendaryPitySkr = Mathf.Max(0, data.storeLegendaryPitySkr);
            storeOpeningActive = data.storeOpeningActive;
            storeOpeningProgress = Mathf.Clamp01(data.storeOpeningProgress);
            pendingStoreOrderId = data.pendingStoreOrderId;
            pendingStoreInstanceIds = data.pendingStoreInstanceIds ?? new string[0];
            pendingStoreDefinitionIds = data.pendingStoreDefinitionIds ?? new string[0];
            pendingStoreQualities = data.pendingStoreQualities ?? new int[0];
            pendingStoreAptitudeRolls = data.pendingStoreAptitudeRolls ?? new int[0];
            pendingStoreRevealAlreadyGranted = data.pendingStoreRevealAlreadyGranted;
            pendingStarterGrantId = data.pendingStarterGrantId;
            acknowledgedStarterGrantId = data.acknowledgedStarterGrantId;
            if (storeOpeningActive &&
                pendingStoreAptitudeRolls.Length != StoreRules.DrawCountPerCharacterPack &&
                pendingStoreInstanceIds.Length == StoreRules.DrawCountPerCharacterPack)
            {
                pendingStoreAptitudeRolls = new int[StoreRules.DrawCountPerCharacterPack];
                for (var index = 0; index < pendingStoreAptitudeRolls.Length; index++)
                    pendingStoreAptitudeRolls[index] =
                        CharacterRarityInstanceRules.ComputeAptitudeRoll(
                            pendingStoreInstanceIds[index]);
            }
            if (storeOpeningActive &&
                (pendingStoreInstanceIds.Length != StoreRules.DrawCountPerCharacterPack ||
                 pendingStoreDefinitionIds.Length != StoreRules.DrawCountPerCharacterPack ||
                 pendingStoreQualities.Length != StoreRules.DrawCountPerCharacterPack ||
                 pendingStoreAptitudeRolls.Length != StoreRules.DrawCountPerCharacterPack))
            {
                storeOpeningActive = false;
                storeOpeningProgress = 0f;
                pendingStoreOrderId = null;
                pendingStoreInstanceIds = new string[0];
                pendingStoreDefinitionIds = new string[0];
                pendingStoreQualities = new int[0];
                pendingStoreAptitudeRolls = new int[0];
                pendingStoreRevealAlreadyGranted = false;
                pendingStarterGrantId = null;
            }
            if (pendingStoreRevealAlreadyGranted)
                ClearPendingStarterGrantReveal(pendingStarterGrantId);
            ownedBasePackIds.Clear();
            if (data.ownedBasePackIds != null)
            {
                for (var i = 0; i < data.ownedBasePackIds.Length; i++)
                {
                    if (!string.IsNullOrEmpty(data.ownedBasePackIds[i]) &&
                        !ownedBasePackIds.Contains(data.ownedBasePackIds[i]))
                        ownedBasePackIds.Add(data.ownedBasePackIds[i]);
                }
            }
            equippedBasePackProductId = StoreCosmeticRules.NormalizeEquipped(
                data.equippedBasePackProductId, StoreProductType.BasePack, ownedBasePackIds);
            equippedBattlefieldProductId = StoreCosmeticRules.NormalizeEquipped(
                data.equippedBattlefieldProductId, StoreProductType.Battlefield, ownedBasePackIds);
            expeditionEntryCommitted = data.saveVersion >= 22
                ? data.expeditionEntryCommitted
                : data.expeditionEnergy < ExpeditionEnergyRules.InitialEnergy ||
                  data.round > 1 || data.coins != 10 || data.health != 10 ||
                  data.phase != (int)ExpeditionPhase.Route;
            tideCrystalBalance = Mathf.Max(0, data.tideCrystalBalance);
            localLampGlowBalance = Mathf.Max(0, data.localLampGlowBalance);
            localTideIronBalance = Mathf.Max(0, data.localTideIronBalance);
            localMistLeatherBalance = Mathf.Max(0, data.localMistLeatherBalance);
            localStarSandBalance = Mathf.Max(0, data.localStarSandBalance);
            localBountyTicketBalance = Mathf.Max(0, data.localBountyTicketBalance);
            nextLocalEquipmentSerial = Mathf.Max(1, data.nextLocalEquipmentSerial);
            if (data.saveVersion < MinimumCompatibleSaveVersion)
            {
                // Earlier builds could save a partial transition between battle,
                // upgrade and route selection.  There is no trustworthy way to
                // infer which reward was chosen, so restart only the expedition.
                RestartExpeditionState("status.repaired");
                SaveGame();
                return true;
            }

            if (data.mapGenerationVersion != ExpeditionMapGraph.MapGenerationVersion &&
                data.mapGenerationVersion !=
                    ExpeditionMapGraph.LegacyFiveTypeMapGenerationVersion)
            {
                // A topology algorithm change must never silently rewrite an active
                // run beneath the player's current node. Preserve account inventory,
                // collection and loadouts, but start a new locally generated route.
                RestartExpeditionState("status.mapUpdated");
                SaveGame();
                return true;
            }

            if (data.roguelikeRuntimeGeneration == ChapterOneRoguelikeV11State.RuntimeGeneration &&
                !ChapterOneRoguelikeV11State.IsPendingChoiceCompatible(
                    data.pendingRoguelikeChoiceSetV11))
            {
                // A pending choice generated by an older balance contract cannot be selected
                // deterministically under the current rules. Preserve account-wide state loaded
                // above, but repair only the active expedition instead of blocking application boot.
                RestartExpeditionState("status.repaired");
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

        private void RestoreWarehouseLayout(PrototypeRunSaveData data)
        {
            Array.Clear(warehouseSlotTokens, 0, warehouseSlotTokens.Length);
            warehouseAcquisitionOrder.Clear();
            if (data != null && data.saveVersion >= 40)
            {
                if (data.warehouseSlotTokens != null)
                    Array.Copy(data.warehouseSlotTokens, warehouseSlotTokens,
                        Mathf.Min(data.warehouseSlotTokens.Length, warehouseSlotTokens.Length));
                if (data.warehouseAcquisitionOrder != null)
                {
                    foreach (var token in data.warehouseAcquisitionOrder)
                        if (!string.IsNullOrEmpty(token) && !warehouseAcquisitionOrder.Contains(token))
                            warehouseAcquisitionOrder.Add(token);
                }
                if (Enum.IsDefined(typeof(WarehouseSortMode), data.warehouseSortMode))
                    warehouseSortMode = (WarehouseSortMode)data.warehouseSortMode;
            }
            ReconcileWarehouseLayout();
        }

        private void RestoreLanternContracts(PrototypeRunSaveData data)
        {
            if (data.saveVersion >= 33)
            {
                lanternInventory = PrototypeLanternSaveCodec.RestoreInventory(
                    data.lanternInventory, data.activeLanternInstanceId);
                RemoveLegacyLocalLanternFixtures();
                expeditionLanternSnapshot = PrototypeLanternSaveCodec.RestoreSnapshot(
                    data.expeditionLanternSnapshot,
                    LanternLegacyMigrationResult.LegacyNoneRulesVersion);
                expeditionRewardSnapshot = PrototypeLanternSaveCodec.RestoreRewardSnapshot(
                    data.expeditionRewardSnapshot, CreateLegacyLocalRewardSnapshot());
                chapterOneLighthouse.Restore(
                    data.chapterOneBossDefeated,
                    data.chapterOneLanternAwarded,
                    data.chapterOneLighthouseLit || data.chapterOneLighthouseRepaired);
                ReconcileActiveLanternLighthouseProgress();
                arenaBattleId = data.arenaBattleId ?? string.Empty;
                arenaAttackLanternSnapshot = PrototypeLanternSaveCodec.RestoreSnapshot(
                    data.arenaAttackLanternSnapshot,
                    LanternLegacyMigrationResult.LegacyNoneRulesVersion);
                arenaDefenseLanternSnapshot = PrototypeLanternSaveCodec.RestoreSnapshot(
                    data.arenaDefenseLanternSnapshot,
                    LanternLegacyMigrationResult.LegacyNoneRulesVersion);
                arenaDefenseSnapshotVersion = Mathf.Max(0, data.arenaDefenseSnapshotVersion);
                arenaDefenseNeedsResync = data.arenaDefenseNeedsResync;
                ReconcileUnavailableLanternSnapshots();
                return;
            }

            var migrated = LanternLegacyMigrationResult.FromLegacySave(
                data.chapterOneBossDefeated,
                data.chapterOneLighthouseRepaired,
                HasSavedDefense(data.defenseUnits));
            lanternInventory = new LanternInventory();
            expeditionLanternSnapshot = migrated.ExpeditionLanternSnapshot;
            expeditionRewardSnapshot = CreateLegacyLocalRewardSnapshot();
            chapterOneLighthouse.Restore(
                migrated.BossDefeated,
                migrated.ChapterLighthouseLit,
                migrated.ChapterLighthouseLit);
            ReconcileActiveLanternLighthouseProgress();
            arenaBattleId = string.Empty;
            arenaAttackLanternSnapshot = LanternSnapshot.None(
                LanternLegacyMigrationResult.LegacyNoneRulesVersion);
            arenaDefenseLanternSnapshot = migrated.ArenaDefenseLanternSnapshot;
            arenaDefenseSnapshotVersion = 0;
            arenaDefenseNeedsResync = migrated.ArenaDefenseNeedsResync;
            ReconcileUnavailableLanternSnapshots();
        }

        private void ReconcileActiveLanternLighthouseProgress()
        {
            if (lanternInventory == null ||
                string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId))
            {
                chapterOneLighthouse.RecordLanternUnequipped();
                return;
            }
            chapterOneLighthouse.RecordEquippedChapterLantern(
                lanternInventory.Get(lanternInventory.ActiveLanternInstanceId),
                AllowLanternBossClearBypassForTesting);
        }

        private static bool AllowLanternBossClearBypassForTesting
        {
            get { return Application.isEditor || Debug.isDebugBuild; }
        }

        private void EnsureDevelopmentLanternQualitySet()
        {
            if (!AllowLanternBossClearBypassForTesting || lanternInventory == null) return;
            FirstChapterLanternEconomyRules.EnsureDevelopmentQualitySet(lanternInventory);
        }

        private void RemoveLegacyLocalLanternFixtures()
        {
            if (lanternInventory == null) return;
            lanternInventory.TryCompleteMarketSale(FirstChapterLanternFixtures.FirstClearInstanceId);
        }

        private static bool HasSavedDefense(PrototypeUnitSaveData[] savedDefense)
        {
            if (savedDefense == null) return false;
            for (var index = 0; index < savedDefense.Length; index++)
                if (savedDefense[index] != null && savedDefense[index].occupied) return true;
            return false;
        }

        private void RestoreExpedition(PrototypeRunSaveData data)
        {
            runRoster.Clear();
            currentRunEconomyRulesVersion = data.runEconomyRulesVersion >= 1 &&
                                            data.runEconomyRulesVersion <= RunEconomyRulesVersion
                ? data.runEconomyRulesVersion
                : 1;
            qualityRecruitPriceRulesVersion = data.saveVersion >= 41 &&
                                              (data.qualityRecruitPriceRulesVersion ==
                                                   QualityRecruitPriceRules.QualityScaledRulesVersion ||
                                               data.qualityRecruitPriceRulesVersion ==
                                                   QualityRecruitPriceRules.CurrentRulesVersion)
                ? data.qualityRecruitPriceRulesVersion
                : QualityRecruitPriceRules.LegacyRulesVersion;
            coins = data.coins;
            health = ChapterOneExpeditionLifeRules.NormalizePersistedLife(
                data.health, data.saveVersion < FiveLifeSaveSchemaVersion);
            round = Mathf.Clamp(data.round, 1, FinalRound);
            expeditionMapSeed = data.mapSeed == 0
                ? unchecked((uint)(0x51EA2C31 + round * 7919))
                : unchecked((uint)data.mapSeed);
            expeditionMap = ExpeditionMapGraph.CreateChapterOne(
                expeditionMapSeed, data.mapGenerationVersion);
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
            if (data.saveVersion >= 26 && data.roguelikeRulesVersion >= 1 &&
                data.roguelikeRulesVersion <= RoguelikeRulesVersion)
            {
                roguelikeBuild.Restore(data.roguelikeRunStateVersion, data.roguelikeChoiceOrdinal,
                    data.pendingRoguelikeChoiceSet, data.acquiredRoguelikeEffects,
                    data.roguelikeChoiceReceipts);
            }
            else
            {
                roguelikeBuild.Reset();
            }
            currentRoguelikeRuntimeGeneration = data.saveVersion >= 43
                ? data.roguelikeRuntimeGeneration : 0;
            if (currentRoguelikeRuntimeGeneration == ChapterOneRoguelikeV11State.RuntimeGeneration)
            {
                roguelikeBuildV11.Restore(currentRoguelikeRuntimeGeneration,
                    data.roguelikeRunStateVersion, data.roguelikeChoiceOrdinal,
                    data.selectedRoguelikeKeystoneId, data.pendingRoguelikeChoiceSetV11,
                    data.acquiredRoguelikeEffectsV11, data.roguelikeChoiceReceiptsV11);
            }
            else
            {
                roguelikeBuildV11.Reset();
            }
            pendingP09FirstOptionId = null;
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
                    data.lockedEconomyContentValues,
                    data.expeditionBackpackItemInstanceIds,
                    data.activeBattleItemInstanceId,
                    data.targetedRecruitTickets);
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
            var legacyAcquiredAtUtcTicks = OwnedCharacterState.LegacyAcquisitionEpochUtcTicks;
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
                    var hasPersistedAcquisitionTime = saved.acquiredAtUtcTicks > 0L;
                    var acquiredAtUtcTicks = hasPersistedAcquisitionTime
                        ? saved.acquiredAtUtcTicks
                        : legacyAcquiredAtUtcTicks;
                    if (!hasPersistedAcquisitionTime) legacyAcquiredAtUtcTicks += copyCount;
                    for (var copy = 0; copy < copyCount; copy++)
                    {
                        var expandedInstanceId = copy == 0 ? instanceId : instanceId + ".copy." + (copy + 1);
                        var aptitudeRoll = data.saveVersion >= 44
                            ? Mathf.Clamp(saved.aptitudeRoll, -1,
                                CharacterRarityInstanceRules.RollScale)
                            : -1;
                        restored.Add(new OwnedCharacterState(expandedInstanceId, saved.definitionId, 1,
                            Mathf.Max(0, saved.externalStar), quality, acquiredAtUtcTicks + copy,
                            aptitudeRoll));
                    }
                }
            }
            var nextAcquiredAtUtcTicks = PlayerCollection.NextAcquiredAtUtcTicks(
                restored, legacyAcquiredAtUtcTicks);
            if (!CompetitionDemoMode)
            {
                foreach (var definition in characterCatalog.Definitions)
                {
                    if (!definition.IsCollectible || ContainsOwnedCharacter(restored, definition.DefinitionId)) continue;
                    restored.Add(new OwnedCharacterState("character." + definition.DefinitionId,
                        definition.DefinitionId, 1, 0, CharacterQuality.Common,
                        nextAcquiredAtUtcTicks++));
                }
                restored = PlayerCollection.AddMissingFormalQualityVariants(characterCatalog, restored);
            }
            playerCollection = restored.Count > 0
                ? new PlayerCollection(characterCatalog, restored)
                : PlayerCollection.CreatePrototype(characterCatalog);

            var nextIds = data.nextRunDeckCharacterInstanceIds;
            if (data.saveVersion < 30)
                nextIds = PreferredFormalDeckInstanceIds();
            if (nextIds == null || nextIds.Length == 0) nextIds = data.nextRunDeckUnitIds;
            if (nextIds == null || nextIds.Length == 0) nextIds = data.activeDeckUnitIds;
            if (nextIds == null || nextIds.Length == 0) nextIds = activeDeckUnitIds;
            nextRunDeckSelection = new RunDeckSelection(
                playerCollection, nextIds, MaximumDeckSize, MinimumDeckSize);
        }

        private string[] PreferredFormalDeckInstanceIds()
        {
            var preferred = new List<string>();
            foreach (var definition in characterCatalog.Definitions)
            {
                if (!definition.IsCollectible || preferred.Count >= MaximumDeckSize) continue;
                var instanceId = PlayerCollection.FormalQualityInstanceId(
                    definition.DefinitionId, CharacterQuality.Legendary);
                OwnedCharacterState owned;
                if (playerCollection.TryGetByInstanceId(instanceId, out owned))
                    preferred.Add(instanceId);
            }
            return preferred.ToArray();
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
                    if (saved.equipmentInstanceId.StartsWith("prototype.",
                            StringComparison.Ordinal) ||
                        string.Equals(saved.seasonId, "prototype-season",
                            StringComparison.Ordinal)) continue;
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
            var quality = Enum.IsDefined(typeof(CharacterQuality), savedUnit.quality)
                ? (CharacterQuality)savedUnit.quality : CharacterQuality.Common;
            var sourceBusinessId = string.IsNullOrEmpty(savedUnit.sourceBusinessId)
                ? savedUnit.runUnitId : savedUnit.sourceBusinessId;
            var aptitudeRoll = string.IsNullOrEmpty(savedUnit.sourceBusinessId)
                ? -1 : Mathf.Clamp(savedUnit.aptitudeRoll, 0, CharacterRarityInstanceRules.RollScale);
            return new RunUnitState(savedUnit.nameKey, basePower, runLevel,
                Mathf.Max(0, savedUnit.acquisitionBasis), savedUnit.runUnitId,
                quality, aptitudeRoll,
                sourceBusinessId, Mathf.Clamp(savedUnit.externalStar, 0, 2));
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
                var definition = characterCatalog.Get(savedUnit.nameKey);
                var quality = Enum.IsDefined(typeof(CharacterQuality), savedUnit.quality)
                    ? (CharacterQuality)savedUnit.quality : CharacterQuality.Common;
                var sourceId = string.IsNullOrEmpty(savedUnit.characterInstanceId)
                    ? "defense." + i + "." + savedUnit.nameKey : savedUnit.characterInstanceId;
                var aptitudeRoll = savedUnit.aptitudeRoll;
                if (aptitudeRoll < 0 || aptitudeRoll > CharacterRarityInstanceRules.RollScale)
                    aptitudeRoll = CharacterRarityInstanceRules.ComputeAptitudeRoll(sourceId);
                var basePower = savedUnit.basePower > 0 ? savedUnit.basePower : savedUnit.power;
                var stats = CharacterRarityInstanceRules.CalculateCombatStats(definition, basePower, 1,
                    quality, aptitudeRoll, Mathf.Clamp(savedUnit.externalStar, 0, 2));
                var unit = UnitData.Create(definition, basePower, PlayerColor(savedUnit.nameKey), 1, stats);
                unit.CharacterInstanceId = sourceId;
                unit.Quality = quality;
                unit.AptitudeRoll = aptitudeRoll;
                unit.ExternalStar = Mathf.Clamp(savedUnit.externalStar, 0, 2);
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
            health = ChapterOneExpeditionLifeRules.InitialLife;
            round = 1;
            CreateNewExpeditionMap();
            runAttackBonus = 0;
            runHealthBonus = 0;
            roguelikeBuild.Reset();
            roguelikeBuildV11.Reset();
            currentRoguelikeRuntimeGeneration = ChapterOneRoguelikeV11State.RuntimeGeneration;
            pendingP09FirstOptionId = null;
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
            CaptureCurrentRunContracts();
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
            pendingLocalExpeditionSettlementPlan = null;
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
            ApplyLanternOpeningShield(players, expeditionLanternSnapshot);
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
            if (unit == null) return;
            if (isPlayer && expeditionEconomy.ActiveBattleItemId == "I01")
                unit.Armor += 8;
            if (isPlayer && expeditionEconomy.ActiveBattleItemId == "I03")
                unit.OpeningShield = Mathf.CeilToInt(unit.OpeningShield * 1.25f);
            if (isPlayer && expeditionEconomy.ActiveBattleItemId == "I07")
                unit.HealAmount = Mathf.CeilToInt(unit.HealAmount * 1.25f);
            if (!isPlayer && expeditionEconomy.ActiveBattleItemId == "I09")
                unit.AttackSpeedBasisPoints = ContinuousAttackSpeedRules.CalculateFinal(
                    unit.AttackSpeedBasisPoints, -1500);
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
                unit.Quality,
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
            public string SourceId;
            public string TargetId;
            public string ProfileId;
            public Vector2 Position;
            public float Width;
            public float DurationSeconds;
            public float ElapsedSeconds;
            public float StartDelaySeconds;
            public float HeightOverride;
            public Vector2 Motion;
            public Vector2 ArcControlOffset;
            public float RenderScale;
            public bool MirrorX;
            public bool UseArcMotion;
            public bool Persistent;
            public bool FadingOut;
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
            public int AttackSpeedBasisPoints;
            public int OpeningShield;
            public bool IsHealer;
            public string CharacterInstanceId;
            public CharacterQuality Quality;
            public int AptitudeRoll;
            public int ExternalStar;

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
                    AttackSpeedBasisPoints = stats.AttackSpeedBasisPoints,
                    IsHealer = stats.HealAmount > 0
                };
            }
        }

        private enum ScreenView
        {
            Home,
            Profile,
            Store,
            Deck,
            Arena,
            Leaderboard,
            Harbor,
            Market,
            Expedition,
            Warehouse,
            Defense,
            Credits
        }

        private enum WarehouseTab
        {
            All,
            Equipment,
            Lantern,
            Materials,
            Other
        }

        private enum DeckCharacterDetailMode
        {
            LibraryBase,
            ActiveDeckLoadout
        }

        private enum LanternChangeAction
        {
            None,
            Equip,
            Replace,
            Unequip
        }
    }
}

