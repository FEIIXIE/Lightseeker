using System;
using System.Collections.Generic;
using System.Threading;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Presentation;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private const string MainnetConsentPreferenceKey =
            "lightseeker.mainnet-login-consent.login-c-v1.0.2";

        private readonly Dictionary<string, Texture2D> loginFormalTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private Vector2 loginScroll;
        private bool loginUserAgreementAccepted;
        private bool loginPrivacyPolicyAccepted;
        private bool mainnetInviteLoginComplete;
        private string pendingMainnetInvitationCode;
        private string loginStatusKey = "login.status.ready";
        private bool mainnetDeepLinkSubscribed;

        private bool ShouldDrawMainnetInviteLogin
        {
            get { return IsMainnetInviteRuntimeConfigured && !mainnetInviteLoginComplete; }
        }

        private void InitializeMainnetInviteLogin()
        {
            var consentAccepted = PlayerPrefs.GetInt(MainnetConsentPreferenceKey, 0) == 1;
            loginUserAgreementAccepted = consentAccepted;
            loginPrivacyPolicyAccepted = consentAccepted;
            mainnetInviteLoginComplete = false;
            pendingMainnetInvitationCode = null;
            loginStatusKey = IsMainnetPublicAccess
                ? "login.status.ready"
                : "login.status.inviteReady";

            if (!mainnetDeepLinkSubscribed)
            {
                Application.deepLinkActivated += OnMainnetInviteDeepLink;
                mainnetDeepLinkSubscribed = true;
            }
            if (!string.IsNullOrWhiteSpace(Application.absoluteURL))
                OnMainnetInviteDeepLink(Application.absoluteURL);
            RestoreMainnetInviteSessionOnStartupAsync();
        }

        private void ShutdownMainnetInviteLogin()
        {
            if (mainnetDeepLinkSubscribed)
            {
                Application.deepLinkActivated -= OnMainnetInviteDeepLink;
                mainnetDeepLinkSubscribed = false;
            }
            pendingMainnetInvitationCode = null;
        }

        private void OnMainnetInviteDeepLink(string url)
        {
            if (IsMainnetPublicAccess) return;
            if (!MainnetInvitationLinkContract.IsInvitationLink(url)) return;
            string invitationCode;
            if (!MainnetInvitationLinkContract.TryExtractCode(url, out invitationCode))
            {
                pendingMainnetInvitationCode = null;
                loginStatusKey = "login.status.inviteInvalid";
                return;
            }

            pendingMainnetInvitationCode = invitationCode;
            loginStatusKey = "login.status.inviteReceived";
        }

        private async void RestoreMainnetInviteSessionOnStartupAsync()
        {
            try
            {
                var session = await RestoreWalletServerSessionAsync(CancellationToken.None);
                if (session == null)
                {
                    loginStatusKey = pendingMainnetInvitationCode == null
                        ? (IsMainnetPublicAccess
                            ? "login.status.ready"
                            : "login.status.inviteReady")
                        : "login.status.inviteReceived";
                    return;
                }
                loginStatusKey = "login.status.authenticated";
                if (loginUserAgreementAccepted && loginPrivacyPolicyAccepted)
                {
                    EnsureCompetitionDemoProfileInitialized(session.AccountId);
                    mainnetInviteLoginComplete = true;
                    if (storeOpeningActive && pendingStoreRevealAlreadyGranted)
                        currentView = ScreenView.Store;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                loginStatusKey = "login.status.restoreFailed";
                Debug.LogWarning("Mainnet wallet session restore failed: " + exception.GetType().Name);
            }
        }

        private async void SignInMainnetInviteAsync()
        {
            if (WalletServerState == WalletServerSessionState.Restoring ||
                WalletServerState == WalletServerSessionState.SigningIn ||
                WalletServerState == WalletServerSessionState.Refreshing ||
                WalletServerState == WalletServerSessionState.SigningOut)
                return;

            var invitationCode = pendingMainnetInvitationCode;
            pendingMainnetInvitationCode = null;
            loginStatusKey = "login.status.connecting";
            try
            {
                await SignInWalletServerAsync(invitationCode, CancellationToken.None);
                loginStatusKey = "login.status.authenticated";
            }
            catch (OperationCanceledException)
            {
                loginStatusKey = invitationCode == null
                    ? (IsMainnetPublicAccess
                        ? "login.status.ready"
                        : "login.status.inviteReady")
                    : "login.status.inviteReceived";
            }
            catch (Exception exception)
            {
                loginStatusKey = invitationCode == null
                    ? (IsMainnetPublicAccess
                        ? "login.status.signInFailed"
                        : "login.status.inviteRequired")
                    : "login.status.inviteRejected";
                Debug.LogWarning("Mainnet wallet sign-in failed: " + exception.GetType().Name);
            }
        }

        private async void SignOutMainnetFromHomeAsync()
        {
            if (WalletServerState != WalletServerSessionState.Authenticated ||
                CurrentWalletServerSession == null) return;
            try
            {
                await SignOutWalletServerAsync(CancellationToken.None);
                mainnetInviteLoginComplete = false;
                pendingMainnetInvitationCode = null;
                loginStatusKey = IsMainnetPublicAccess
                    ? "login.status.ready"
                    : "login.status.inviteReady";
                homeTopMenu = 0;
                currentView = storeOpeningActive && pendingStoreRevealAlreadyGranted
                    ? ScreenView.Store : ScreenView.Home;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Mainnet wallet sign-out failed: " + exception.GetType().Name);
            }
        }

        private void DrawLoginFormalScreen()
        {
            var viewportHeight = Mathf.Min(layoutCanvasHeight, LoginCFormalArtContract.CanvasHeight);
            loginScroll = GUI.BeginScrollView(
                new Rect(0f, 0f, LoginCFormalArtContract.CanvasWidth, viewportHeight),
                loginScroll,
                new Rect(0f, 0f, LoginCFormalArtContract.CanvasWidth,
                    LoginCFormalArtContract.CanvasHeight),
                false, false, GUIStyle.none, GUIStyle.none);

            DrawLoginTexture(new Rect(0f, 0f, 1080f, 2400f),
                LoginCFormalArtContract.ModuleRoot, LoginCFormalArtContract.Background,
                1080, 2400);
            DrawLoginTexture(new Rect(130f, 70f, 820f, 420f),
                LoginCFormalArtContract.ModuleRoot, LoginCFormalArtContract.TitleOrnament,
                820, 420);
            DrawLoginLabel(new Rect(130f, 156f, 820f, 150f), T("login.title"),
                language == Localization.GameLanguage.ChineseSimplified ? 118 : 88,
                new Color(0.94f, 0.82f, 0.57f), TextAnchor.MiddleCenter, true);
            DrawLoginLabel(new Rect(130f, 352f, 820f, 64f), T("login.subtitle"),
                language == Localization.GameLanguage.ChineseSimplified ? 30 : 26,
                new Color(0.84f, 0.73f, 0.52f), TextAnchor.MiddleCenter, false);

            var interactionEnabled = !profileSettingsOpen;
            var settingsVisual = new Rect(948f, 52f, 72f, 72f);
            var settingsTouch = new Rect(936f, 40f, 96f, 96f);
            DrawLoginStateTexture(settingsVisual, LoginCFormalArtContract.ReusedRoot,
                LoginCFormalArtContract.SettingsNormal,
                LoginCFormalArtContract.SettingsPressed,
                LoginCFormalArtContract.SettingsDisabled, 72, 72,
                interactionEnabled && IsPointerDown(settingsTouch), interactionEnabled);
            GUI.enabled = interactionEnabled;
            if (GUI.Button(settingsTouch, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Tap, "login-settings");
                profileSettingsPage = ProfileSettingsPage.Main;
                profileSettingsOpen = true;
            }
            GUI.enabled = true;

            DrawLoginLabel(new Rect(160f, 1665f, 760f, 60f), T(loginStatusKey), 24,
                LoginStatusColor(), TextAnchor.MiddleCenter, false);
            DrawLoginLabel(new Rect(120f, 700f, 840f, 72f), T("login.demoTitle"), 42,
                new Color(0.96f, 0.78f, 0.38f), TextAnchor.MiddleCenter, true);
            DrawLoginLabel(new Rect(125f, 790f, 830f, 330f), T("login.demoNotice"),
                language == Localization.GameLanguage.ChineseSimplified ? 34 : 27,
                new Color(0.90f, 0.84f, 0.72f), TextAnchor.UpperCenter, false, true);

            var authenticated = WalletServerState == WalletServerSessionState.Authenticated &&
                                CurrentWalletServerSession != null;
            var canEnter = interactionEnabled && authenticated &&
                           loginUserAgreementAccepted && loginPrivacyPolicyAccepted;
            var mainPressed = canEnter && IsPointerDown(new Rect(170f, 1789f, 740f, 137f));
            var mainResource = canEnter
                ? (mainPressed ? LoginCFormalArtContract.MainPressed : LoginCFormalArtContract.MainNormal)
                : LoginCFormalArtContract.MainDisabled;
            var mainHeight = canEnter ? (mainPressed ? 137 : 132) : 134;
            var mainRect = new Rect(170f, 1858f - mainHeight / 2f, 740f, mainHeight);
            DrawLoginTexture(mainRect, LoginCFormalArtContract.ReusedRoot, mainResource,
                740, mainHeight);
            DrawLoginLabel(mainRect, T("login.start"),
                language == Localization.GameLanguage.ChineseSimplified ? 62 : 48,
                canEnter ? new Color(0.96f, 0.86f, 0.65f) : new Color(0.54f, 0.56f, 0.56f),
                TextAnchor.MiddleCenter, true);
            GUI.enabled = canEnter;
            if (GUI.Button(new Rect(170f, 1789f, 740f, 137f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Confirm, "login-start");
                PlayerPrefs.SetInt(MainnetConsentPreferenceKey, 1);
                PlayerPrefs.Save();
                EnsureCompetitionDemoProfileInitialized(CurrentWalletServerSession.AccountId);
                mainnetInviteLoginComplete = true;
                loginScroll = Vector2.zero;
                currentView = ScreenView.Home;
            }
            GUI.enabled = true;

            var walletBusy = WalletServerState == WalletServerSessionState.Restoring ||
                             WalletServerState == WalletServerSessionState.SigningIn ||
                             WalletServerState == WalletServerSessionState.Refreshing ||
                             WalletServerState == WalletServerSessionState.SigningOut;
            var walletEnabled = interactionEnabled && !walletBusy && !authenticated;
            var walletVisual = new Rect(330f, 1960f, 96f, 96f);
            var walletTouch = new Rect(314f, 1944f, 128f, 128f);
            DrawLoginStateTexture(walletVisual, LoginCFormalArtContract.ModuleRoot,
                LoginCFormalArtContract.WalletNormal,
                LoginCFormalArtContract.WalletPressed,
                LoginCFormalArtContract.WalletDisabled, 96, 96,
                walletEnabled && IsPointerDown(walletTouch), walletEnabled);
            GUI.enabled = walletEnabled;
            if (GUI.Button(walletTouch, GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Confirm, "login-wallet");
                SignInMainnetInviteAsync();
            }
            GUI.enabled = true;

            DrawLoginTexture(new Rect(654f, 1960f, 96f, 96f),
                LoginCFormalArtContract.ModuleRoot, LoginCFormalArtContract.GoogleDisabled,
                96, 96);

            DrawLoginTexture(new Rect(141f, 2149f, 150f, 32f),
                LoginCFormalArtContract.ModuleRoot, LoginCFormalArtContract.LegalLeft,
                150, 32);
            DrawLoginTexture(new Rect(789f, 2149f, 150f, 32f),
                LoginCFormalArtContract.ModuleRoot, LoginCFormalArtContract.LegalRight,
                150, 32);
            DrawLoginConsentGroup(new Rect(299f, 2133f, 268f, 64f),
                new Rect(307f, 2141f, 48f, 48f), ref loginUserAgreementAccepted,
                "login.userAgreement", interactionEnabled);
            DrawLoginConsentGroup(new Rect(575f, 2133f, 268f, 64f),
                new Rect(583f, 2141f, 48f, 48f), ref loginPrivacyPolicyAccepted,
                "login.privacyPolicy", interactionEnabled);
            DrawLoginLabel(new Rect(780f, 2220f, 180f, 48f), ClientVersionLabel, 22,
                new Color(0.80f, 0.75f, 0.63f), TextAnchor.MiddleRight, false);

            GUI.EndScrollView();
            if (profileSettingsOpen) DrawProfileSettingsModal();
        }

        private void DrawLoginConsentGroup(Rect touchArea, Rect visualArea, ref bool accepted,
            string textKey, bool enabled)
        {
            var pressed = enabled && IsPointerDown(touchArea);
            var resource = accepted
                ? (enabled
                    ? (pressed ? LoginCFormalArtContract.CheckedPressed : LoginCFormalArtContract.CheckedNormal)
                    : LoginCFormalArtContract.CheckedDisabled)
                : (enabled
                    ? (pressed ? LoginCFormalArtContract.UncheckedPressed : LoginCFormalArtContract.UncheckedNormal)
                    : LoginCFormalArtContract.UncheckedDisabled);
            DrawLoginTexture(visualArea, LoginCFormalArtContract.ModuleRoot, resource, 48, 48);
            DrawLoginLabel(new Rect(visualArea.x + 56f, touchArea.y, touchArea.width - 64f, touchArea.height),
                T(textKey), language == Localization.GameLanguage.ChineseSimplified ? 28 : 22,
                new Color(0.90f, 0.82f, 0.64f), TextAnchor.MiddleLeft, false);
            GUI.enabled = enabled;
            if (GUI.Button(touchArea, GUIContent.none, GUIStyle.none))
            {
                accepted = !accepted;
                PlayUiClick(UiAudioCueIds.Tap, "login-consent");
            }
            GUI.enabled = true;
        }

        private Color LoginStatusColor()
        {
            if (loginStatusKey == "login.status.inviteInvalid" ||
                loginStatusKey == "login.status.inviteRejected" ||
                loginStatusKey == "login.status.restoreFailed" ||
                loginStatusKey == "login.status.inviteRequired" ||
                loginStatusKey == "login.status.signInFailed")
                return new Color(0.96f, 0.69f, 0.48f);
            return new Color(0.84f, 0.80f, 0.70f);
        }

        private void DrawLoginStateTexture(Rect area, string root, string normal,
            string pressed, string disabled, int width, int height, bool isPressed,
            bool enabled = true)
        {
            DrawLoginTexture(area, root, enabled ? (isPressed ? pressed : normal) : disabled,
                width, height);
        }

        private void DrawLoginTexture(Rect area, string root, string resourceId, int width, int height)
        {
            var key = root + resourceId;
            Texture2D texture;
            if (!loginFormalTextures.TryGetValue(key, out texture))
            {
                texture = Resources.Load<Texture2D>(key);
                loginFormalTextures[key] = texture;
            }
            if (texture == null)
            {
                Debug.LogError("Missing accepted Login C resource: " + key);
                return;
            }
            if (texture.width != width || texture.height != height ||
                Mathf.RoundToInt(area.width) != width || Mathf.RoundToInt(area.height) != height)
            {
                Debug.LogError("Rejected resized Login C resource: " + key + " expected " +
                               width + "x" + height + " actual " + texture.width + "x" +
                               texture.height + " target " + area.width + "x" + area.height);
                return;
            }
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
        }

        private void DrawLoginLabel(Rect area, string value, int fontSize, Color color,
            TextAnchor alignment, bool emphasized, bool wordWrap = false)
        {
            var style = ProfileStyle(fontSize, color, alignment, emphasized);
            style.wordWrap = wordWrap;
            style.clipping = TextClipping.Clip;
            GUI.Label(area, value, style);
        }
    }
}
