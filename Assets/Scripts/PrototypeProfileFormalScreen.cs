using System;
using System.Collections.Generic;
using System.Threading;
using SeekerAutoChess.Core;
using SeekerAutoChess.Prototype.Localization;
using SeekerAutoChess.Prototype.Presentation;
using SeekerAutoChess.Wallet;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    public sealed partial class PrototypeBootstrap
    {
        private const string ProfileMusicPreferenceKey =
            "seeker-auto-chess.preference.music-percent.v1";
        private const string ProfileSfxPreferenceKey =
            "seeker-auto-chess.preference.sfx-percent.v1";
        private const string ProfileOfficialWebsite = "https://lightseeker.games/";

        private readonly Dictionary<string, Texture2D> profileFormalTextures =
            new Dictionary<string, Texture2D>(StringComparer.Ordinal);
        private Vector2 profileScroll;
        private Vector2 profileSettingsScroll;
        private Vector2 profileSettingsSecondaryScroll;
        private bool profileSettingsPointerHeld;
        private Vector2 profileSettingsPointerLast;
        private bool profileSettingsOpen;
        private bool profileAudioPreferencesLoaded;
        private int profileMusicPercent = 70;
        private int profileSfxPercent = 70;
        private ProfileSettingsPage profileSettingsPage;
        private ProfileEditModal profileEditModal;
        private Vector2 profileAvatarPickerScroll;
        private string profileNameDraft = string.Empty;
        private string profileNameError;
        private bool profileNameSaveInProgress;
        private const string ProfileAvatarPreferenceKey =
            "seeker-auto-chess.profile-avatar-instance.v1";

        private enum ProfileSettingsPage
        {
            Main,
            UserAgreement,
            PrivacyPolicy,
            Credits
        }

        private enum ProfileEditModal
        {
            None,
            Avatar,
            DisplayName
        }

        private void DrawProfileFormalScreen()
        {
            EnsureProfileAudioPreferences();
            var viewportHeight = Mathf.Min(layoutCanvasHeight, ProfileFFormalArtContract.CanvasHeight);
            var oldScrollSkin = GUI.skin.verticalScrollbar;
            profileScroll = GUI.BeginScrollView(
                new Rect(0f, 0f, ProfileFFormalArtContract.CanvasWidth, viewportHeight),
                profileScroll,
                new Rect(0f, 0f, ProfileFFormalArtContract.CanvasWidth,
                    ProfileFFormalArtContract.CanvasHeight),
                false, false, GUIStyle.none, GUIStyle.none);

            DrawProfileTexture(new Rect(0f, 0f, 1080f, 2400f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.FullShell,
                1080, 2400);
            DrawProfileContents();
            GUI.EndScrollView();
            GUI.skin.verticalScrollbar = oldScrollSkin;

            if (profileSettingsOpen) DrawProfileSettingsModal();
            else if (profileEditModal != ProfileEditModal.None) DrawProfileEditModal();
        }

        private void DrawProfileContents()
        {
            var interactionEnabled = !profileSettingsOpen && profileEditModal == ProfileEditModal.None;
            GUI.enabled = interactionEnabled;
            DrawProfileTexture(new Rect(40f, 66f, 94f, 94f),
                ProfileFFormalArtContract.ReusedRoot, ProfileFFormalArtContract.BackFrame, 94, 94);
            DrawProfileTexture(new Rect(67f, 93f, 40f, 40f),
                ProfileFFormalArtContract.ReusedRoot, ProfileFFormalArtContract.BackGlyph, 40, 40);
            GUI.enabled = interactionEnabled;
            if (GUI.Button(new Rect(32f, 58f, 110f, 110f), GUIContent.none, GUIStyle.none))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "profile-back");
                profileScroll = Vector2.zero;
                currentView = ScreenView.Home;
            }

            DrawProfileLabel(new Rect(168f, 66f, 520f, 84f),
                ProfileText("航行档案", "VOYAGE PROFILE"), 64, ProfileLight, TextAnchor.MiddleLeft, true);
            DrawProfileLabel(new Rect(170f, 148f, 600f, 48f),
                ProfileText("向更远的海，记录此刻的我", "EVERY VOYAGE LEAVES A TRACE"),
                28, ProfileHeaderSecondary, TextAnchor.MiddleLeft, false);
            DrawProfileLabel(new Rect(882f, 24f, 150f, 30f), ClientVersionLabel, 18,
                ProfileLight, TextAnchor.MiddleRight, false);

            DrawProfileTexture(new Rect(938f, 66f, 94f, 94f),
                ProfileFFormalArtContract.ReusedRoot, ProfileFFormalArtContract.BackFrame, 94, 94);
            if (DrawProfileStateButton(new Rect(949f, 77f, 72f, 72f),
                    ProfileFFormalArtContract.ReusedRoot,
                    ProfileFFormalArtContract.SettingsNormal,
                    ProfileFFormalArtContract.SettingsPressed,
                    ProfileFFormalArtContract.SettingsDisabled, 72, 72, interactionEnabled))
            {
                PlayUiClick(UiAudioCueIds.Settings, "profile-settings-open");
                profileSettingsPage = ProfileSettingsPage.Main;
                profileSettingsScroll = Vector2.zero;
                profileSettingsOpen = true;
            }

            DrawProfileTexture(new Rect(76f, 468f, 64f, 64f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.SectionPersonal, 64, 64);
            DrawProfileLabel(new Rect(160f, 460f, 320f, 76f),
                ProfileText("个人信息", "PERSONAL INFORMATION"), 46,
                ProfileInk, TextAnchor.MiddleLeft, true);
            DrawProfileLabel(new Rect(808f, 468f, 188f, 64f), "PERSONAL\nINFORMATION", 14,
                ProfileHeaderSecondary, TextAnchor.MiddleRight, false);

            DrawProfileAvatar();
            if (DrawProfileStateButton(new Rect(370f, 836f, 64f, 64f),
                    ProfileFFormalArtContract.ReusedRoot,
                    ProfileFFormalArtContract.EditNormal,
                    ProfileFFormalArtContract.EditPressed,
                    ProfileFFormalArtContract.EditDisabled, 64, 64, interactionEnabled))
            {
                PlayUiClick(UiAudioCueIds.TabSwitch, "profile-avatar-edit");
                profileAvatarPickerScroll = Vector2.zero;
                profileEditModal = ProfileEditModal.Avatar;
            }
            if (DrawProfileStateButton(new Rect(694f, 610f, 64f, 64f),
                    ProfileFFormalArtContract.ReusedRoot,
                    ProfileFFormalArtContract.EditNormal,
                    ProfileFFormalArtContract.EditPressed,
                    ProfileFFormalArtContract.EditDisabled, 64, 64, interactionEnabled))
            {
                PlayUiClick(UiAudioCueIds.TabSwitch, "profile-name-edit");
                profileNameDraft = ProfileDisplayName();
                profileNameError = null;
                profileEditModal = ProfileEditModal.DisplayName;
            }

            var profileNameStyle = ProfileStyle(58, ProfileInk, TextAnchor.MiddleLeft, true);
            var profileNameContent = new GUIContent(ProfileDisplayName());
            while (profileNameStyle.fontSize > 36 &&
                   profileNameStyle.CalcSize(profileNameContent).x > 220f)
                profileNameStyle.fontSize--;
            GUI.Label(new Rect(468f, 602f, 220f, 76f), profileNameContent, profileNameStyle);
            DrawProfileLabel(new Rect(470f, 700f, 500f, 52f),
                ProfileText("海的尽头，仍有未至之光。", "LIGHT STILL WAITS BEYOND THE SEA."),
                27, ProfileMuted, TextAnchor.MiddleLeft, false);

            DrawProfileTexture(new Rect(472f, 817f, 48f, 48f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.FieldUid, 48, 48);
            DrawProfileTexture(new Rect(472f, 887f, 48f, 48f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.FieldPower, 48, 48);
            DrawProfileTexture(new Rect(472f, 957f, 48f, 48f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.FieldWallet, 48, 48);
            DrawProfileLabel(new Rect(538f, 814f, 120f, 54f), "UID", 28,
                ProfileInk, TextAnchor.MiddleLeft, false);
            DrawProfileLabel(new Rect(690f, 814f, 280f, 54f), ProfileUid(), 31,
                ProfileInk, TextAnchor.MiddleLeft, false);
            DrawProfileLabel(new Rect(538f, 884f, 120f, 54f),
                ProfileText("战力", "POWER"), 28, ProfileInk, TextAnchor.MiddleLeft, false);
            DrawProfileLabel(new Rect(690f, 884f, 220f, 54f), CurrentPvpDeckPower().ToString("N0"),
                31, ProfileInk, TextAnchor.MiddleLeft, false);
            DrawProfileLabel(new Rect(538f, 954f, 145f, 54f),
                ProfileText("钱包地址", "WALLET"), 28, ProfileInk, TextAnchor.MiddleLeft, false);
            DrawProfileLabel(new Rect(690f, 954f, 140f, 54f), ProfileWalletText(), 31,
                ProfileInk, TextAnchor.MiddleLeft, false);

            var hasWallet = WalletServerState == WalletServerSessionState.Authenticated &&
                            CurrentWalletServerSession != null &&
                            !string.IsNullOrEmpty(CurrentWalletServerSession.WalletAddress);
            var walletBusy = WalletServerState == WalletServerSessionState.Restoring ||
                             WalletServerState == WalletServerSessionState.SigningIn ||
                             WalletServerState == WalletServerSessionState.Refreshing ||
                             WalletServerState == WalletServerSessionState.SigningOut;
            var walletButton = new Rect(844f, 952f, 160f, 64f);
            if (DrawProfileStateButton(walletButton,
                    ProfileFFormalArtContract.ReusedRoot,
                    ProfileFFormalArtContract.WalletNormal,
                    ProfileFFormalArtContract.WalletPressed,
                    ProfileFFormalArtContract.WalletDisabled, 160, 64, !walletBusy && interactionEnabled))
            {
                if (hasWallet)
                {
                    PlayUiClick(UiAudioCueIds.Tap, "profile-wallet-explorer");
                    Application.OpenURL("https://solscan.io/account/" +
                        Uri.EscapeDataString(CurrentWalletServerSession.WalletAddress));
                }
                else
                {
                    PlayUiClick(UiAudioCueIds.Confirm, "profile-wallet-connect");
                    ConnectProfileWalletAsync();
                }
            }
            var walletButtonText = hasWallet
                ? ProfileText("查看钱包", "VIEW WALLET")
                : walletBusy
                    ? ProfileText("连接中", "CONNECTING")
                    : WalletServerState == WalletServerSessionState.Failed
                        ? ProfileText("重试连接", "RETRY")
                        : ProfileText("链接", "CONNECT");
            DrawProfileLabel(walletButton, walletButtonText, 26,
                walletBusy ? ProfileDisabled : ProfileLight, TextAnchor.MiddleCenter, true);

            DrawProfileVoyageAndCompetitive();
            GUI.enabled = true;
        }

        private void DrawProfileVoyageAndCompetitive()
        {
            var completed = expeditionRewards == null ? 0 : expeditionRewards.CompletedNodeIds.Count;
            var lit = chapterOneLighthouse != null && chapterOneLighthouse.IsLit ? 1 : 0;
            var playerRank = ArenaPrototypeRules.CalculatePlayerDisplayRank(arenaOpponents, arenaRating);
            var challenges = Mathf.Max(0, ArenaPrototypeRules.DailyChallengeLimit - arenaChallengesRemaining);
            var progressLabels = new[]
            {
                ProfileText("当前章节", "CHAPTER"), ProfileText("点亮灯塔", "LIGHTHOUSES"),
                ProfileText("完成远征", "EXPEDITIONS"), ProfileText("难度克服", "DIFFICULTY")
            };
            var progressValues = new[]
            {
                ProfileText("第一章", "CHAPTER 1"), lit + " / 1", completed.ToString(),
                completed >= 12 ? "D3" : completed >= 8 ? "D2" : completed >= 4 ? "D1" : "D0"
            };
            var competitiveLabels = new[]
            {
                ProfileText("本赛季挑战", "SEASON CHALLENGES"), ProfileText("胜率", "WIN RATE"),
                ProfileText("最高名次", "BEST RANK"), ProfileText("当前积分", "CURRENT SCORE")
            };
            var competitiveValues = new[]
            {
                challenges.ToString(), "--", "#" + playerRank, arenaRating.ToString("N0")
            };
            DrawProfileTexture(new Rect(76f, 1128f, 64f, 64f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.SectionVoyage, 64, 64);
            DrawProfileLabel(new Rect(160f, 1120f, 320f, 76f),
                ProfileText("航行进度", "VOYAGE PROGRESS"), 46,
                ProfileInk, TextAnchor.MiddleLeft, true);
            DrawProfileLabel(new Rect(822f, 1128f, 174f, 64f), "VOYAGE\nPROGRESS", 14,
                ProfileHeaderSecondary, TextAnchor.MiddleRight, false);
            DrawProfileTexture(new Rect(76f, 1598f, 64f, 64f),
                ProfileFFormalArtContract.ModuleRoot, ProfileFFormalArtContract.SectionCompetitive, 64, 64);
            DrawProfileLabel(new Rect(160f, 1590f, 320f, 76f),
                ProfileText("竞技信息", "COMPETITIVE INFORMATION"), 46,
                ProfileInk, TextAnchor.MiddleLeft, true);
            DrawProfileLabel(new Rect(796f, 1598f, 200f, 64f), "COMPETITIVE\nINFORMATION", 14,
                ProfileHeaderSecondary, TextAnchor.MiddleRight, false);

            var voyageIcons = new[]
            {
                ProfileFFormalArtContract.VoyageChapter, ProfileFFormalArtContract.VoyageLighthouse,
                ProfileFFormalArtContract.VoyageExpedition, ProfileFFormalArtContract.VoyageDifficulty
            };
            var competitiveIcons = new[]
            {
                ProfileFFormalArtContract.CompetitiveChallenges, ProfileFFormalArtContract.CompetitiveWinRate,
                ProfileFFormalArtContract.CompetitiveRank, ProfileFFormalArtContract.CompetitiveScore
            };
            for (var i = 0; i < 4; i++)
            {
                var iconX = 124f + i * 244f;
                var textX = 54f + i * 244f;
                DrawProfileTexture(new Rect(iconX, 1252f, 80f, 96f),
                    ProfileFFormalArtContract.ModuleRoot, voyageIcons[i], 80, 96);
                DrawProfileTexture(new Rect(iconX, 1724f, 80f, 96f),
                    ProfileFFormalArtContract.ModuleRoot, competitiveIcons[i], 80, 96);
                DrawProfileLabel(new Rect(textX + 20f, 1374f, 180f, 48f), progressLabels[i], 27,
                    ProfileMuted, TextAnchor.MiddleCenter, false);
                DrawProfileLabel(new Rect(textX, 1422f, 220f, 80f), progressValues[i], 53,
                    ProfileInk, TextAnchor.MiddleCenter, false);
                DrawProfileLabel(new Rect(textX + 20f, 1846f, 180f, 48f), competitiveLabels[i], 27,
                    ProfileMuted, TextAnchor.MiddleCenter, false);
                DrawProfileLabel(new Rect(textX, 1894f, 220f, 80f), competitiveValues[i], 53,
                    ProfileInk, TextAnchor.MiddleCenter, false);
            }
        }

        private async void ConnectProfileWalletAsync()
        {
            if (WalletServerState == WalletServerSessionState.Restoring ||
                WalletServerState == WalletServerSessionState.SigningIn ||
                WalletServerState == WalletServerSessionState.Refreshing ||
                WalletServerState == WalletServerSessionState.SigningOut)
                return;

            try
            {
                await SignInWalletServerAsync(CancellationToken.None);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Profile wallet connection failed: " + exception.GetType().Name);
            }
        }

        private void DrawProfileAvatar()
        {
            OwnedCharacterState selected;
            if (!TryGetProfileAvatarCharacter(out selected)) return;
            AvatarArtEntry art;
            if (!AvatarArtContract.TryGet(selected.DefinitionId,
                    CharacterQualityResourceId(selected.Quality), out art)) return;
            DrawProfileTexture(new Rect(102f, 618f, 300f, 300f),
                ProfileFFormalArtContract.AvatarRoot,
                art.ResourceId + "-PROFILE-300x300-v1.0.0", 300, 300);
        }

        private bool TryGetProfileAvatarCharacter(out OwnedCharacterState selected)
        {
            selected = null;
            if (playerCollection == null || playerCollection.Entries.Count == 0) return false;
            var preferred = PlayerPrefs.GetString(ProfileAvatarPreferenceKey, string.Empty);
            if (!string.IsNullOrEmpty(preferred) &&
                playerCollection.TryGetByInstanceId(preferred, out selected)) return true;
            selected = playerCollection.Entries[0];
            PlayerPrefs.SetString(ProfileAvatarPreferenceKey, selected.CharacterInstanceId);
            return true;
        }

        private void SelectProfileAvatar(OwnedCharacterState selected)
        {
            if (selected == null) return;
            PlayerPrefs.SetString(ProfileAvatarPreferenceKey, selected.CharacterInstanceId);
            PlayerPrefs.Save();
        }

        private string ProfileDisplayName()
        {
            return string.IsNullOrWhiteSpace(WalletServerDisplayName)
                ? T("home.playerName")
                : WalletServerDisplayName;
        }

        private void DrawProfileEditModal()
        {
            DrawSettingsTexture(new Rect(0f, 0f, 1080f, 2400f), SettingsFormalArtContract.DimOverlay,
                1080, 2400);
            DrawSettingsTexture(new Rect(60f, 420f, 960f, 1560f), SettingsFormalArtContract.ModalShell,
                960, 1560);
            if (DrawSettingsStateButton(new Rect(862f, 480f, 72f, 72f),
                    SettingsFormalArtContract.CloseNormal, SettingsFormalArtContract.ClosePressed,
                    SettingsFormalArtContract.CloseDisabled, 72, 72, !profileNameSaveInProgress))
            {
                PlayUiClick(UiAudioCueIds.PanelClose, "profile-edit-close");
                profileEditModal = ProfileEditModal.None;
                profileNameError = null;
                return;
            }

            if (profileEditModal == ProfileEditModal.Avatar) DrawProfileAvatarPicker();
            else DrawProfileNameEditor();
        }

        private void DrawProfileAvatarPicker()
        {
            DrawProfileLabel(new Rect(120f, 476f, 700f, 80f),
                ProfileText("选择展示头像", "SELECT PROFILE AVATAR"), 46,
                ProfileInk, TextAnchor.MiddleLeft, true);
            var choices = new List<OwnedCharacterState>();
            var seenDefinitions = new HashSet<string>(StringComparer.Ordinal);
            if (playerCollection != null)
                foreach (var entry in playerCollection.Entries)
                    if (entry != null && seenDefinitions.Add(entry.DefinitionId)) choices.Add(entry);
            var rows = Mathf.Max(1, (choices.Count + 1) / 2);
            var contentHeight = rows * 380f;
            profileAvatarPickerScroll = GUI.BeginScrollView(
                new Rect(90f, 590f, 900f, 1260f), profileAvatarPickerScroll,
                new Rect(0f, 0f, 880f, contentHeight), false, false,
                GUIStyle.none, GUIStyle.none);
            OwnedCharacterState current;
            TryGetProfileAvatarCharacter(out current);
            for (var index = 0; index < choices.Count; index++)
            {
                var choice = choices[index];
                var x = index % 2 == 0 ? 70f : 510f;
                var y = index / 2 * 380f;
                AvatarArtEntry art;
                if (AvatarArtContract.TryGet(choice.DefinitionId,
                        CharacterQualityResourceId(choice.Quality), out art))
                    DrawProfileTexture(new Rect(x, y, 300f, 300f),
                        ProfileFFormalArtContract.AvatarRoot,
                        art.ResourceId + "-PROFILE-300x300-v1.0.0", 300, 300);
                var selected = current != null &&
                               current.CharacterInstanceId == choice.CharacterInstanceId;
                DrawProfileLabel(new Rect(x, y + 304f, 300f, 54f),
                    T(choice.DefinitionId) + (selected
                        ? ProfileText(" · 已选择", " · SELECTED") : string.Empty),
                    25, ProfileInk, TextAnchor.MiddleCenter, selected);
                if (GUI.Button(new Rect(x, y, 300f, 354f), GUIContent.none, GUIStyle.none))
                {
                    SelectProfileAvatar(choice);
                    PlayUiClick(UiAudioCueIds.Confirm, "profile-avatar-selected");
                    profileEditModal = ProfileEditModal.None;
                }
            }
            GUI.EndScrollView();
        }

        private void DrawProfileNameEditor()
        {
            DrawProfileLabel(new Rect(120f, 476f, 700f, 80f),
                ProfileText("修改玩家名称", "EDIT DISPLAY NAME"), 46,
                ProfileInk, TextAnchor.MiddleLeft, true);
            DrawProfileLabel(new Rect(130f, 610f, 820f, 70f),
                ProfileText("名称需为 2–16 个字母、数字、汉字或下划线，且不能与其他玩家重名。",
                    "Use 2–16 letters, numbers, or underscores. Names must be unique."),
                24, ProfileMuted, TextAnchor.MiddleLeft, false);
            DrawSettingsTexture(new Rect(130f, 700f, 820f, 96f),
                SettingsFormalArtContract.RowNormal, 820, 96);
            var inputStyle = ProfileStyle(34, ProfileInk, TextAnchor.MiddleLeft, false);
            inputStyle.padding = new RectOffset(18, 18, 0, 0);
            GUI.enabled = !profileNameSaveInProgress;
            profileNameDraft = GUI.TextField(new Rect(150f, 714f, 780f, 68f),
                profileNameDraft ?? string.Empty, 16, inputStyle);
            GUI.enabled = true;
            if (!string.IsNullOrWhiteSpace(profileNameError))
                DrawProfileLabel(new Rect(130f, 820f, 820f, 100f), profileNameError, 24,
                    new Color(0.60f, 0.18f, 0.13f), TextAnchor.UpperLeft, false);

            var saveArea = new Rect(400f, 1640f, 280f, 76f);
            var canSave = !profileNameSaveInProgress &&
                          WalletServerState == WalletServerSessionState.Authenticated &&
                          CurrentWalletServerSession != null &&
                          !string.IsNullOrWhiteSpace(profileNameDraft);
            if (DrawProfileStateButton(saveArea, GlobalButtonFormalArtContract.ResourceRoot,
                    GlobalButtonFormalArtContract.LargeNormal,
                    GlobalButtonFormalArtContract.LargePressed,
                    GlobalButtonFormalArtContract.LargeDisabled, 280, 76, canSave))
            {
                PlayUiClick(UiAudioCueIds.Confirm, "profile-name-save");
                SaveProfileDisplayNameAsync();
            }
            DrawProfileLabel(saveArea,
                profileNameSaveInProgress
                    ? ProfileText("校验中", "CHECKING")
                    : ProfileText("保存名称", "SAVE NAME"),
                26, canSave ? ProfileLight : ProfileDisabled, TextAnchor.MiddleCenter, true);
            if (WalletServerState != WalletServerSessionState.Authenticated)
                DrawProfileLabel(new Rect(130f, 1760f, 820f, 60f),
                    ProfileText("连接钱包并登录后才能修改名称。",
                        "CONNECT AND SIGN IN BEFORE CHANGING THE NAME."),
                    24, ProfileMuted, TextAnchor.MiddleCenter, false);
        }

        private async void SaveProfileDisplayNameAsync()
        {
            if (profileNameSaveInProgress) return;
            profileNameSaveInProgress = true;
            profileNameError = null;
            try
            {
                await UpdateWalletServerDisplayNameAsync(profileNameDraft, CancellationToken.None);
                PlayUiClick(UiAudioCueIds.SaveOk, "profile-name-saved");
                profileEditModal = ProfileEditModal.None;
            }
            catch (WalletServerException exception)
            {
                profileNameError = exception.Code == "PROFILE_DISPLAY_NAME_TAKEN"
                    ? ProfileText("这个名称已经被使用，请换一个。", "THAT NAME IS ALREADY IN USE.")
                    : ProfileText("名称不符合规则或服务器暂时不可用。", "THE NAME IS INVALID OR THE SERVER IS UNAVAILABLE.");
            }
            catch (Exception)
            {
                profileNameError = ProfileText("名称保存失败，请稍后重试。", "NAME SAVE FAILED. TRY AGAIN.");
            }
            finally
            {
                profileNameSaveInProgress = false;
            }
        }

        private void DrawProfileSettingsModal()
        {
            DrawSettingsTexture(new Rect(0f, 0f, 1080f, 2400f), SettingsFormalArtContract.DimOverlay,
                1080, 2400);
            DrawSettingsTexture(new Rect(60f, 420f, 960f, 1560f), SettingsFormalArtContract.ModalShell,
                960, 1560);
            var secondary = profileSettingsPage != ProfileSettingsPage.Main;
            if (secondary && DrawSettingsStateButton(new Rect(88f, 480f, 72f, 72f),
                    SettingsFormalArtContract.BackNormal, SettingsFormalArtContract.BackPressed,
                    SettingsFormalArtContract.BackDisabled, 72, 72, true))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "profile-settings-secondary-back");
                profileSettingsPage = ProfileSettingsPage.Main;
                profileSettingsSecondaryScroll = Vector2.zero;
            }
            if (DrawSettingsStateButton(new Rect(862f, 480f, 72f, 72f),
                    SettingsFormalArtContract.CloseNormal, SettingsFormalArtContract.ClosePressed,
                    SettingsFormalArtContract.CloseDisabled, 72, 72, true))
            {
                PlayUiClick(UiAudioCueIds.PanelClose, "profile-settings-close");
                profileSettingsOpen = false;
                profileSettingsPage = ProfileSettingsPage.Main;
                return;
            }
            DrawProfileLabel(new Rect(secondary ? 180f : 120f, 476f, secondary ? 650f : 320f, 80f),
                ProfileSettingsTitle(), secondary ? 42 : 54, ProfileLight,
                TextAnchor.MiddleLeft, true);
            if (secondary) DrawProfileSettingsSecondary();
            else DrawProfileSettingsMain();
        }

        private void DrawProfileSettingsMain()
        {
            const float contentHeight = 1370f;
            const float viewportHeight = 1310f;
            var maxScroll = contentHeight - viewportHeight;
            profileSettingsScroll = new Vector2(0f, Mathf.Clamp(profileSettingsScroll.y, 0f, maxScroll));
            HandleProfileSettingsPointer(new Rect(120f, 570f, 820f, viewportHeight), maxScroll);

            // Do not use IMGUI's nested ScrollView for this modal. Its scaled clip transform can
            // leak the first row into the accepted blue title bar in the Editor. The fixed group
            // owns the clip while every content element is translated explicitly.
            GUI.BeginGroup(new Rect(120f, 570f, 820f, viewportHeight));
            var y = 0f;
            DrawSettingsDivider(y, ProfileText("语言", "LANGUAGE")); y += 64f;
            if (DrawSettingsRow(y, ProfileText("界面语言", "INTERFACE LANGUAGE"),
                    language == GameLanguage.ChineseSimplified ? "简体中文" : "English",
                    SettingsRowIcon.Chevron, true))
            {
                PlayUiClick(UiAudioCueIds.TabSwitch, "profile-settings-language");
                language = language == GameLanguage.ChineseSimplified
                    ? GameLanguage.English : GameLanguage.ChineseSimplified;
                SaveLanguagePreference();
            }
            // Contract baselines after the language row are 816/912/... in page space.
            // The accepted divider contributes 64 px, so the inter-section advance is 118 px.
            y += 118f;
            DrawSettingsDivider(y, ProfileText("音频", "AUDIO")); y += 64f;
            DrawSettingsAudioRow(y, ProfileText("背景音乐", "MUSIC"), true); y += 96f;
            DrawSettingsAudioRow(y, ProfileText("音效", "SOUND EFFECTS"), false); y += 124f;
            DrawSettingsDivider(y, ProfileText("其他", "OTHER")); y += 64f;
            if (DrawSettingsRow(y, ProfileText("用户协议", "USER AGREEMENT"), string.Empty,
                    SettingsRowIcon.Chevron, true)) OpenProfileSettingsPage(ProfileSettingsPage.UserAgreement);
            y += 96f;
            if (DrawSettingsRow(y, ProfileText("隐私政策", "PRIVACY POLICY"), string.Empty,
                    SettingsRowIcon.Chevron, true)) OpenProfileSettingsPage(ProfileSettingsPage.PrivacyPolicy);
            y += 96f;
            if (DrawSettingsRow(y, ProfileText("致谢与许可", "CREDITS & LICENSES"), string.Empty,
                    SettingsRowIcon.Chevron, true)) OpenProfileSettingsPage(ProfileSettingsPage.Credits);
            y += 96f;
            DrawSettingsRow(y, ProfileText("问题反馈", "FEEDBACK"),
                ProfileText("通道待发布", "NOT PUBLISHED"), SettingsRowIcon.Chevron, false);
            y += 96f;
            if (DrawSettingsRow(y, ProfileText("网站", "WEBSITE"),
                    "lightseeker.games", SettingsRowIcon.External, true))
            {
                PlayUiClick(UiAudioCueIds.Tap, "profile-settings-website");
                Application.OpenURL(ProfileOfficialWebsite);
            }
            y += 96f;
            DrawSettingsRow(y, "X", ProfileText("通道待发布", "NOT PUBLISHED"),
                SettingsRowIcon.External, false);
            y += 96f;
            DrawSettingsRow(y, ProfileText("版本", "VERSION"), ClientVersionLabel,
                SettingsRowIcon.None, false);
            GUI.EndGroup();

            DrawSettingsScrollbar(maxScroll);
            var canSignOut = WalletServerState == WalletServerSessionState.Authenticated &&
                              CurrentWalletServerSession != null;
            var signOutArea = new Rect(400f, 1810f, 280f, 76f);
            if (DrawProfileStateButton(signOutArea, GlobalButtonFormalArtContract.ResourceRoot,
                    GlobalButtonFormalArtContract.LargeNormal,
                    GlobalButtonFormalArtContract.LargePressed,
                    GlobalButtonFormalArtContract.LargeDisabled, 280, 76, canSignOut))
            {
                PlayUiClick(UiAudioCueIds.Cancel, "profile-settings-sign-out");
                profileSettingsOpen = false;
                profileSettingsPage = ProfileSettingsPage.Main;
                SignOutMainnetFromHomeAsync();
            }
            DrawProfileLabel(signOutArea, ProfileText("注销登录", "SIGN OUT"), 26,
                canSignOut ? ProfileLight : ProfileDisabled, TextAnchor.MiddleCenter, true);
        }

        private void DrawSettingsScrollbar(float maxScroll)
        {
            if (maxScroll <= 0f) return;
            var track = new Rect(985f, 617f, 18f, 1180f);
            DrawSettingsTexture(track, SettingsFormalArtContract.ScrollbarTrack, 18, 1180);
            var thumbY = Mathf.Lerp(617f, 1577f, profileSettingsScroll.y / maxScroll);
            var thumb = new Rect(985f, thumbY, 18f, 220f);
            DrawSettingsTexture(thumb, IsPointerDown(thumb)
                ? SettingsFormalArtContract.ScrollbarThumbPressed
                : SettingsFormalArtContract.ScrollbarThumbNormal, 18, 220);
        }

        private void HandleProfileSettingsPointer(Rect viewport, float maxScroll)
        {
            var current = Event.current;
            if (current == null) return;
            if (current.type == EventType.ScrollWheel && viewport.Contains(current.mousePosition))
            {
                profileSettingsScroll.y = Mathf.Clamp(profileSettingsScroll.y + current.delta.y * 28f,
                    0f, maxScroll);
                current.Use();
                return;
            }
            if (current.type == EventType.MouseDown && current.button == 0 &&
                viewport.Contains(current.mousePosition))
            {
                profileSettingsPointerHeld = true;
                profileSettingsPointerLast = current.mousePosition;
            }
            else if (current.type == EventType.MouseDrag && current.button == 0 && profileSettingsPointerHeld)
            {
                var delta = current.mousePosition - profileSettingsPointerLast;
                profileSettingsPointerLast = current.mousePosition;
                profileSettingsScroll.y = Mathf.Clamp(profileSettingsScroll.y - delta.y, 0f, maxScroll);
                GUIUtility.hotControl = 0;
                current.Use();
            }
            else if (current.type == EventType.MouseUp && current.button == 0)
            {
                profileSettingsPointerHeld = false;
            }
        }

        private void DrawProfileSettingsSecondary()
        {
            profileSettingsSecondaryScroll = new Vector2(0f,
                Mathf.Clamp(profileSettingsSecondaryScroll.y, 0f, 130f));
            GUI.BeginGroup(new Rect(120f, 570f, 820f, 1310f));
            profileSettingsSecondaryScroll = GUI.BeginScrollView(new Rect(0f, 0f, 820f, 1310f),
                profileSettingsSecondaryScroll, new Rect(0f, 0f, 820f, 1500f), false, false,
                GUIStyle.none, GUIStyle.none);
            string body;
            if (profileSettingsPage == ProfileSettingsPage.Credits)
            {
                var lines = new List<string>();
                foreach (var entry in ThirdPartyAttributionCatalog.Entries)
                    lines.Add(entry.Title + "\n" + entry.Credit + "\n" + entry.License);
                body = string.Join("\n\n", lines.ToArray());
            }
            else if (profileSettingsPage == ProfileSettingsPage.UserAgreement)
            {
                body = ProfileText(
                    "正式用户协议文本尚未由产品归档发布。本页保留正式空态，不展示占位法律条款。",
                    "The final User Agreement has not been published by Product. This formal empty state does not substitute placeholder legal terms.");
            }
            else
            {
                body = ProfileText(
                    "正式隐私政策文本尚未由产品归档发布。本页保留正式空态，不展示占位法律条款。",
                    "The final Privacy Policy has not been published by Product. This formal empty state does not substitute placeholder legal terms.");
            }
            var bodyStyle = ProfileStyle(23, ProfileInk, TextAnchor.UpperLeft, false);
            bodyStyle.wordWrap = true;
            GUI.Label(new Rect(18f, 18f, 784f, 1440f), body, bodyStyle);
            GUI.EndScrollView();
            GUI.EndGroup();
        }

        private enum SettingsRowIcon { None, Chevron, External }

        private bool DrawSettingsRow(float y, string label, string value, SettingsRowIcon icon, bool enabled)
        {
            // Unity IMGUI can leak a partially scrolled label across nested scaled clip groups.
            // Suppress any element whose top has crossed the fixed title/body boundary instead
            // of relying on clip behavior that differs between Editor and Android backends.
            var visibleY = y - profileSettingsScroll.y;
            if (visibleY < 0f || visibleY >= 1310f) return false;
            var area = new Rect(0f, visibleY, 820f, 96f);
            var pressed = enabled && IsPointerDown(area);
            DrawSettingsTexture(area, enabled
                ? (pressed ? SettingsFormalArtContract.RowPressed : SettingsFormalArtContract.RowNormal)
                : SettingsFormalArtContract.RowDisabled, 820, 96);
            DrawProfileLabel(new Rect(20f, visibleY, 480f, 96f), label, 32,
                enabled ? ProfileInk : ProfileDisabled, TextAnchor.MiddleLeft, true);
            var valueArea = new Rect(460f, visibleY, 290f, 96f);
            var valueStyle = ProfileStyle(30, enabled ? ProfileMuted : ProfileDisabled,
                TextAnchor.MiddleRight, false);
            var valueContent = new GUIContent(value ?? string.Empty);
            while (valueStyle.fontSize > 22 &&
                   valueStyle.CalcSize(valueContent).x > valueArea.width)
                valueStyle.fontSize--;
            GUI.Label(valueArea, valueContent, valueStyle);
            if (icon != SettingsRowIcon.None)
            {
                var normal = icon == SettingsRowIcon.Chevron
                    ? SettingsFormalArtContract.ChevronNormal : SettingsFormalArtContract.ExternalNormal;
                var down = icon == SettingsRowIcon.Chevron
                    ? SettingsFormalArtContract.ChevronPressed : SettingsFormalArtContract.ExternalPressed;
                var disabled = icon == SettingsRowIcon.Chevron
                    ? SettingsFormalArtContract.ChevronDisabled : SettingsFormalArtContract.ExternalDisabled;
                DrawSettingsTexture(new Rect(752f, visibleY + 24f, 48f, 48f), enabled ? (pressed ? down : normal) : disabled,
                    48, 48);
            }
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked;
        }

        private void DrawSettingsAudioRow(float y, string label, bool music)
        {
            var visibleY = y - profileSettingsScroll.y;
            if (visibleY < 0f || visibleY >= 1310f) return;
            var area = new Rect(0f, visibleY, 820f, 96f);
            DrawSettingsTexture(area, SettingsFormalArtContract.RowNormal, 820, 96);
            DrawProfileLabel(new Rect(20f, visibleY, 250f, 96f), label, 32, ProfileInk,
                TextAnchor.MiddleLeft, true);
            var percent = music ? profileMusicPercent : profileSfxPercent;
            var speakerArea = new Rect(270f, visibleY + 20f, 56f, 56f);
            var muted = percent == 0;
            var speakerNormal = muted ? SettingsFormalArtContract.SpeakerMutedNormal : SettingsFormalArtContract.SpeakerNormal;
            var speakerPressed = muted ? SettingsFormalArtContract.SpeakerMutedPressed : SettingsFormalArtContract.SpeakerPressed;
            DrawSettingsTexture(speakerArea, IsPointerDown(speakerArea) ? speakerPressed : speakerNormal, 56, 56);
            if (GUI.Button(speakerArea, GUIContent.none, GUIStyle.none))
                SetProfileAudioPercent(music, muted ? 70 : 0);
            var slider = new Rect(330f, visibleY + 26f, 360f, 44f);
            var pressed = IsPointerDown(slider);
            DrawSettingsTexture(slider, SettingsFormalArtContract.Slider(percent, pressed), 360, 44);
            if (GUI.Button(slider, GUIContent.none, GUIStyle.none))
            {
                var mouseX = Mathf.Clamp(Event.current.mousePosition.x - slider.x, 0f, slider.width);
                percent = Mathf.RoundToInt(mouseX / slider.width * 10f) * 10;
                SetProfileAudioPercent(music, percent);
            }
            DrawProfileLabel(new Rect(705f, visibleY, 95f, 96f), percent + "%", 30,
                ProfileMuted, TextAnchor.MiddleRight, false);
        }

        private void DrawSettingsDivider(float y, string title)
        {
            var visibleY = y - profileSettingsScroll.y;
            if (visibleY < 0f || visibleY >= 1310f) return;
            DrawSettingsTexture(new Rect(0f, visibleY, 820f, 64f), SettingsFormalArtContract.SectionDivider,
                820, 64);
            if (!string.IsNullOrEmpty(title))
                DrawProfileLabel(new Rect(0f, visibleY, 150f, 64f), title, 38,
                    ProfileInk, TextAnchor.MiddleLeft, true);
        }

        private void OpenProfileSettingsPage(ProfileSettingsPage page)
        {
            PlayUiClick(UiAudioCueIds.Tap, "profile-settings-secondary-open");
            profileSettingsPage = page;
            profileSettingsSecondaryScroll = Vector2.zero;
        }

        private void EnsureProfileAudioPreferences()
        {
            if (profileAudioPreferencesLoaded) return;
            profileMusicPercent = Mathf.Clamp(PlayerPrefs.GetInt(ProfileMusicPreferenceKey, 70), 0, 100);
            profileSfxPercent = Mathf.Clamp(PlayerPrefs.GetInt(ProfileSfxPreferenceKey, 70), 0, 100);
            profileMusicPercent = Mathf.RoundToInt(profileMusicPercent / 10f) * 10;
            profileSfxPercent = Mathf.RoundToInt(profileSfxPercent / 10f) * 10;
            ApplyProfileAudioPreferences();
            profileAudioPreferencesLoaded = true;
        }

        private void SetProfileAudioPercent(bool music, int percent)
        {
            percent = Mathf.Clamp(Mathf.RoundToInt(percent / 10f) * 10, 0, 100);
            if (music) profileMusicPercent = percent;
            else profileSfxPercent = percent;
            PlayerPrefs.SetInt(music ? ProfileMusicPreferenceKey : ProfileSfxPreferenceKey, percent);
            PlayerPrefs.Save();
            ApplyProfileAudioPreferences();
            PlayUiClick(UiAudioCueIds.Tap, music ? "profile-music-volume" : "profile-sfx-volume");
        }

        private void ApplyProfileAudioPreferences()
        {
            uiAudioRouter.Mixer.Music = profileMusicPercent / 100f;
            uiAudioRouter.Mixer.Ui = profileSfxPercent / 100f;
            uiAudioRouter.Mixer.Battle = profileSfxPercent / 100f;
        }

        private string ProfileSettingsTitle()
        {
            switch (profileSettingsPage)
            {
                case ProfileSettingsPage.UserAgreement: return ProfileText("用户协议", "USER AGREEMENT");
                case ProfileSettingsPage.PrivacyPolicy: return ProfileText("隐私政策", "PRIVACY POLICY");
                case ProfileSettingsPage.Credits: return ProfileText("制作人员与许可", "CREDITS & LICENSES");
                default: return ProfileText("设置", "SETTINGS");
            }
        }

        private string ProfileUid()
        {
            var address = CurrentWalletServerSession == null ? null : CurrentWalletServerSession.WalletAddress;
            if (string.IsNullOrEmpty(address)) return "SEA-000000";
            unchecked
            {
                uint hash = 2166136261;
                for (var i = 0; i < address.Length; i++) hash = (hash ^ address[i]) * 16777619;
                return "SEA-" + (hash % 1000000).ToString("000000");
            }
        }

        private string ProfileWalletText()
        {
            var address = CurrentWalletServerSession == null ? null : CurrentWalletServerSession.WalletAddress;
            if (string.IsNullOrEmpty(address)) return ProfileText("未连接", "NOT CONNECTED");
            return address.Length <= 14 ? address : address.Substring(0, 6) + "…" + address.Substring(address.Length - 6);
        }

        private LanternInstanceState ProfileCollectionLantern()
        {
            if (lanternInventory == null) return null;
            if (!string.IsNullOrEmpty(lanternInventory.ActiveLanternInstanceId))
                return lanternInventory.Get(lanternInventory.ActiveLanternInstanceId);
            return lanternInventory.Instances.Count == 0 ? null : lanternInventory.Instances[0];
        }

        private EquipmentInstanceState ProfileCollectionEquipment()
        {
            if (equipmentInventory == null || equipmentInventory.Instances.Count == 0) return null;
            foreach (var instance in equipmentInventory.Instances)
                if (!string.IsNullOrEmpty(instance.EquippedCharacterInstanceId)) return instance;
            return equipmentInventory.Instances[0];
        }

        private string ProfileCollectionLanternName(LanternInstanceState instance)
        {
            return instance == null ? ProfileText("未装备", "NOT EQUIPPED") : T(instance.TemplateId);
        }

        private string ProfileCollectionEquipmentName(EquipmentInstanceState instance)
        {
            return instance == null ? ProfileText("未获得", "NOT OWNED") : T(instance.TemplateId);
        }

        private string ProfileText(string chinese, string english)
        {
            return language == GameLanguage.ChineseSimplified ? chinese : english;
        }

        private static readonly Color ProfileInk = new Color(8f / 255f, 55f / 255f, 91f / 255f);
        private static readonly Color ProfileMuted = new Color(72f / 255f, 91f / 255f, 92f / 255f);
        private static readonly Color ProfileLight = new Color(246f / 255f, 235f / 255f, 202f / 255f);
        private static readonly Color ProfileHeaderSecondary = new Color(216f / 255f, 207f / 255f, 184f / 255f, 0.88f);
        private static readonly Color ProfileDisabled = new Color(0.52f, 0.55f, 0.54f);

        private GUIStyle ProfileStyle(int fontSize, Color color, TextAnchor alignment, bool emphasized)
        {
            var style = ArchiveStyle(fontSize, color, alignment, emphasized);
            style.clipping = TextClipping.Clip;
            style.wordWrap = false;
            return style;
        }

        private void DrawProfileLabel(Rect area, string text, int fontSize, Color color,
            TextAnchor alignment, bool emphasized)
        {
            GUI.Label(area, text, ProfileStyle(fontSize, color, alignment, emphasized));
        }

        private bool DrawProfileStateButton(Rect area, string root, string normal, string pressed,
            string disabled, int width, int height, bool enabled)
        {
            DrawProfileTexture(area, root, enabled ? (IsPointerDown(area) ? pressed : normal) : disabled,
                width, height);
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked;
        }

        private bool DrawSettingsStateButton(Rect area, string normal, string pressed, string disabled,
            int width, int height, bool enabled)
        {
            DrawSettingsTexture(area, enabled ? (IsPointerDown(area) ? pressed : normal) : disabled,
                width, height);
            GUI.enabled = enabled;
            var clicked = GUI.Button(area, GUIContent.none, GUIStyle.none);
            GUI.enabled = true;
            return clicked;
        }

        private static bool IsPointerDown(Rect area)
        {
            return Event.current != null && Event.current.type == EventType.MouseDown &&
                   Event.current.button == 0 && area.Contains(Event.current.mousePosition);
        }

        private void DrawSettingsTexture(Rect area, string resourceId, int width, int height)
        {
            DrawProfileTexture(area, SettingsFormalArtContract.ModuleRoot, resourceId, width, height);
        }

        private void DrawProfileTexture(Rect area, string root, string resourceId, int width, int height)
        {
            var key = root + resourceId;
            Texture2D texture;
            if (!profileFormalTextures.TryGetValue(key, out texture))
            {
                texture = Resources.Load<Texture2D>(key);
                profileFormalTextures[key] = texture;
            }
            if (texture == null)
            {
                Debug.LogError("Missing accepted profile/settings resource: " + key);
                return;
            }
            if (texture.width != width || texture.height != height ||
                Mathf.RoundToInt(area.width) != width || Mathf.RoundToInt(area.height) != height)
            {
                Debug.LogError("Rejected resized profile/settings resource: " + key + " expected " +
                               width + "x" + height + " actual " + texture.width + "x" + texture.height +
                               " target " + area.width + "x" + area.height);
                return;
            }
            var previous = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(area, texture, ScaleMode.StretchToFill, true);
            GUI.color = previous;
        }
    }
}
