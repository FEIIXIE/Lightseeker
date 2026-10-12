using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    /// <summary>
    /// Runtime adapter for the accepted six-page gameplay tutorial and handbook entry.
    /// </summary>
    public sealed partial class PrototypeBootstrap
    {
        private const string GameplayTutorialSeenPreferenceKey =
            "lightseeker.gameplay-tutorial.seen.v1";

        private readonly GameplayTutorialFlow gameplayTutorialFlow =
            new GameplayTutorialFlow();
        private Vector2 gameplayTutorialPointerDown;
        private bool gameplayTutorialPointerHeld;
        private Texture2D gameplayTutorialShell;
        private Texture2D gameplayTutorialScrim;
        private Texture2D gameplayTutorialClose;
        private Texture2D homeHandbookPopover;

        private bool IsGameplayTutorialOpen => gameplayTutorialFlow.IsOpen;

        private GameplayTutorialPage CurrentGameplayTutorialPage =>
            GameplayTutorialCatalog.GetPage(gameplayTutorialFlow.CurrentPageIndex);

        private void TryOpenFirstVisitGameplayTutorialOnHome()
        {
            if (currentView != ScreenView.Home || battlePresentationActive ||
                ShouldDrawMainnetInviteLogin || harborEvidenceCaptureActive) return;
            var hasSeen = PlayerPrefs.GetInt(GameplayTutorialSeenPreferenceKey, 0) == 1;
            gameplayTutorialFlow.TryOpenFirstVisit(hasSeen);
        }

        private void OpenGameplayTutorialFromHandbook()
        {
            gameplayTutorialFlow.OpenManual();
            gameplayTutorialPointerHeld = false;
            homeTopMenu = 0;
        }

        private void CloseGameplayTutorial()
        {
            var returnToHandbook = gameplayTutorialFlow.OpenReason == GameplayTutorialOpenReason.Manual;
            gameplayTutorialPointerHeld = false;
            if (gameplayTutorialFlow.Close() == GameplayTutorialCloseResult.PersistSeen)
            {
                PlayerPrefs.SetInt(GameplayTutorialSeenPreferenceKey, 1);
                PlayerPrefs.Save();
            }
            if (returnToHandbook) homeTopMenu = 1;
        }

        private GameplayTutorialSwipeResult HandleGameplayTutorialSwipe(Rect swipeArea)
        {
            var current = Event.current;
            if (current == null || !gameplayTutorialFlow.IsOpen)
                return GameplayTutorialSwipeResult.None;

            if (current.type == EventType.MouseDown && current.button == 0 &&
                swipeArea.Contains(current.mousePosition))
            {
                gameplayTutorialPointerDown = current.mousePosition;
                gameplayTutorialPointerHeld = true;
                return GameplayTutorialSwipeResult.None;
            }

            if (current.rawType != EventType.MouseUp || !gameplayTutorialPointerHeld)
                return GameplayTutorialSwipeResult.None;

            gameplayTutorialPointerHeld = false;
            var delta = current.mousePosition - gameplayTutorialPointerDown;
            if (Mathf.Abs(delta.x) <= Mathf.Abs(delta.y))
                return GameplayTutorialSwipeResult.None;
            var result = gameplayTutorialFlow.HandleHorizontalSwipe(delta.x);
            if (result != GameplayTutorialSwipeResult.None) current.Use();
            return result;
        }

        private string GameplayTutorialTitle()
        {
            return language == Localization.GameLanguage.ChineseSimplified
                ? CurrentGameplayTutorialPage.ChineseTitle
                : CurrentGameplayTutorialPage.EnglishTitle;
        }

        private string GameplayTutorialBody()
        {
            return language == Localization.GameLanguage.ChineseSimplified
                ? CurrentGameplayTutorialPage.ChineseBody
                : CurrentGameplayTutorialPage.EnglishBody;
        }

        private string GameplayTutorialAction()
        {
            return language == Localization.GameLanguage.ChineseSimplified
                ? CurrentGameplayTutorialPage.ChineseAction
                : CurrentGameplayTutorialPage.EnglishAction;
        }

        private void DrawGameplayTutorialOverlay()
        {
            gameplayTutorialScrim ??= Resources.Load<Texture2D>(
                "Art/ExpeditionSettlement/V101/reused/ART-UI-MARKET-B-SCRIM-1080x2400-v1.0.0");
            gameplayTutorialShell ??= Resources.Load<Texture2D>(
                "Art/HomeTutorial/V110/modules/ART-UI-HOME-TUTORIAL-COMPACT-SHELL-1024x1536-v1.1.0");
            gameplayTutorialClose ??= Resources.Load<Texture2D>(
                "Art/Settings/V111/modules/ART-UI-SETTINGS-CLOSE-NORMAL-72-v1.0.0");

            DrawGameplayTutorialTexture(gameplayTutorialScrim, new Rect(0f, 0f, 1080f, 2400f), 1080, 2400);
            DrawGameplayTutorialTexture(gameplayTutorialShell, new Rect(28f, 100f, 1024f, 1536f), 1024, 1536);
            DrawGameplayTutorialTexture(gameplayTutorialClose, new Rect(870f, 350f, 72f, 72f), 72, 72);

            var chinese = language == Localization.GameLanguage.ChineseSimplified;
            DrawGameplayTutorialText(new Rect(176f, 402f, 82f, 82f),
                (gameplayTutorialFlow.CurrentPageIndex + 1).ToString("00"), 30,
                new Color(166f / 255f, 116f / 255f, 70f / 255f), TextAnchor.MiddleCenter, true);
            DrawGameplayTutorialText(new Rect(300f, 394f, 560f, 44f),
                chinese ? "第一次远征" : "FIRST EXPEDITION", 28,
                new Color(166f / 255f, 116f / 255f, 70f / 255f), TextAnchor.MiddleLeft, true);
            DrawGameplayTutorialText(new Rect(300f, 446f, 600f, 78f), GameplayTutorialTitle(), 50,
                new Color(25f / 255f, 61f / 255f, 76f / 255f), TextAnchor.MiddleLeft, true);
            DrawGameplayTutorialText(new Rect(150f, 590f, 300f, 56f),
                chinese ? "操作步骤" : "STEPS", 32,
                new Color(166f / 255f, 116f / 255f, 70f / 255f), TextAnchor.MiddleLeft, true);
            DrawGameplayTutorialText(new Rect(150f, 676f, 800f, 326f), GameplayTutorialBody(), 40,
                new Color(25f / 255f, 61f / 255f, 76f / 255f), TextAnchor.UpperLeft, false);
            DrawGameplayTutorialText(new Rect(150f, 1048f, 220f, 56f),
                chinese ? "目标" : "GOAL", 32,
                new Color(166f / 255f, 116f / 255f, 70f / 255f), TextAnchor.MiddleLeft, true);
            DrawGameplayTutorialText(new Rect(150f, 1134f, 800f, 112f), GameplayTutorialAction(), 42,
                new Color(25f / 255f, 61f / 255f, 76f / 255f), TextAnchor.UpperLeft, true);

            DrawGameplayTutorialText(new Rect(419f + gameplayTutorialFlow.CurrentPageIndex * 42f,
                    1278f, 32f, 42f), "●", 26,
                new Color(166f / 255f, 116f / 255f, 70f / 255f), TextAnchor.MiddleCenter, true);

            HandleGameplayTutorialSwipe(new Rect(28f, 100f, 1024f, 1536f));
            if (GUI.Button(new Rect(850f, 330f, 112f, 112f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "gameplay-tutorial-close");
                CloseGameplayTutorial();
            }
        }

        private void DrawGameplayTutorialTexture(Texture2D texture, Rect rect, int expectedWidth, int expectedHeight)
        {
            if (texture == null) return;
            if (texture.width != expectedWidth || texture.height != expectedHeight)
            {
                Debug.LogError("Rejected resized formal tutorial resource: " + texture.name);
                return;
            }
            var oldColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            GUI.color = oldColor;
        }

        private void DrawGameplayTutorialText(Rect rect, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized)
        {
            var style = new GUIStyle(GUI.skin.label)
            {
                font = SourceHanSerifFont(emphasized),
                fontSize = fontSize,
                fontStyle = FontStyle.Normal,
                alignment = alignment,
                clipping = TextClipping.Clip,
                wordWrap = true,
                richText = false
            };
            style.normal.textColor = color;
            GUI.Label(rect, new GUIContent(text), style);
        }

        private Texture2D HomeHandbookPopover()
        {
            return homeHandbookPopover ??= Resources.Load<Texture2D>(
                "Art/HomeHandbook/V103/modules/ART-UI-HOME-HANDBOOK-SYMMETRIC-POPOVER-1024x1536-v1.0.3");
        }
    }
}
