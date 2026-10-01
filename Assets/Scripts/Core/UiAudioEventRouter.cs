using System;
using System.Collections.Generic;

namespace SeekerAutoChess.Core
{
    public sealed class UiAudioResourceCache<T> where T : class
    {
        private readonly Dictionary<string, T> resources =
            new Dictionary<string, T>(StringComparer.Ordinal);
        private readonly HashSet<string> loadAttempts =
            new HashSet<string>(StringComparer.Ordinal);

        public int Count { get { return resources.Count; } }

        public T GetOrLoad(string resourcePath, Func<T> loader)
        {
            if (string.IsNullOrEmpty(resourcePath) || loader == null) return null;
            T resource;
            if (resources.TryGetValue(resourcePath, out resource)) return resource;
            if (loadAttempts.Contains(resourcePath)) return null;
            loadAttempts.Add(resourcePath);
            resource = loader();
            if (resource != null) resources[resourcePath] = resource;
            return resource;
        }
    }

    public static class UiAudioCueIds
    {
        public const string Tap = "UI_TAP";
        public const string Confirm = "UI_CONFIRM";
        public const string Cancel = "UI_CANCEL";
        public const string Disabled = "UI_DISABLED";
        public const string Error = "UI_ERROR";
        public const string Warning = "UI_WARNING";
        public const string PanelOpen = "UI_PANEL_OPEN";
        public const string PanelClose = "UI_PANEL_CLOSE";
        public const string TabSwitch = "UI_TAB_SWITCH";
        public const string BattleStart = "UI_BATTLE_START";
        public const string Pause = "UI_PAUSE";
        public const string Resume = "UI_RESUME";
        public const string Skip = "UI_SKIP";
        public const string SpeedUp = "UI_SPEED_UP";
        public const string AutoToggle = "UI_AUTO_TOGGLE";
        public const string Purchase = "UI_PURCHASE";
        public const string Upgrade = "UI_UPGRADE";
        public const string DragPick = "UI_DRAG_PICK";
        public const string DragDrop = "UI_DRAG_DROP";
        public const string Reward = "UI_REWARD";
        public const string Milestone = "UI_MILESTONE";
        public const string ConnectionLost = "UI_CONNECTION_LOST";
        public const string Reconnected = "UI_RECONNECTED";
        public const string MuteToggle = "UI_MUTE_TOGGLE";
        public const string SaveOk = "UI_SAVE_OK";
        public const string ContentMissing = "UI_CONTENT_MISSING";
        public const string Settings = "UI_SETTINGS";

        public static readonly IReadOnlyList<string> All = new[]
        {
            Tap, Confirm, Cancel, Disabled, Error, Warning, PanelOpen, PanelClose, TabSwitch,
            BattleStart, Pause, Resume, Skip, SpeedUp, AutoToggle, Purchase, Upgrade, DragPick,
            DragDrop, Reward, Milestone, ConnectionLost, Reconnected, MuteToggle, SaveOk,
            ContentMissing, Settings
        };
    }

    public static class UiMainClickAudioPolicy
    {
        public const string MainClickResourcePath = "Audio/UI/SFX_UI_CLICK_MAIN_KENNEY_SWITCH18_V01";
        public const string CancelResourcePath = "Audio/UI/SFX_UI_CANCEL_RETURN_KENNEY_SWITCH35_V01";
        public const string FormationDropResourcePath = "Audio/UI/SFX_UI_FORMATION_DROP_STONE_A_V06";
        public const string ResourcePath = MainClickResourcePath;
        public const float MainClickVolumeScale = 0.7f;
        public const float FormationDropVolumeScale = 1f;

        private static readonly HashSet<string> MainClickCueIds = new HashSet<string>(
            new[]
            {
                UiAudioCueIds.Tap, UiAudioCueIds.Confirm, UiAudioCueIds.Cancel,
                UiAudioCueIds.PanelOpen, UiAudioCueIds.PanelClose, UiAudioCueIds.TabSwitch,
                UiAudioCueIds.BattleStart, UiAudioCueIds.Pause, UiAudioCueIds.Resume,
                UiAudioCueIds.Skip, UiAudioCueIds.SpeedUp, UiAudioCueIds.AutoToggle,
                UiAudioCueIds.Purchase, UiAudioCueIds.Upgrade,
                UiAudioCueIds.MuteToggle, UiAudioCueIds.Settings
            }, StringComparer.Ordinal);

        public static string ResolveResourcePath(string cueId)
        {
            if (string.Equals(cueId, UiAudioCueIds.DragPick, StringComparison.Ordinal) ||
                string.Equals(cueId, UiAudioCueIds.DragDrop, StringComparison.Ordinal))
                return FormationDropResourcePath;
            if (string.Equals(cueId, UiAudioCueIds.Cancel, StringComparison.Ordinal))
                return CancelResourcePath;
            return MainClickCueIds.Contains(cueId ?? string.Empty) ? MainClickResourcePath : null;
        }

        public static float ResolveVolumeScale(string resourcePath)
        {
            if (string.Equals(resourcePath, FormationDropResourcePath, StringComparison.Ordinal))
                return FormationDropVolumeScale;
            if (string.Equals(resourcePath, MainClickResourcePath, StringComparison.Ordinal))
                return MainClickVolumeScale;
            if (string.Equals(resourcePath, CancelResourcePath, StringComparison.Ordinal))
                return MainClickVolumeScale;
            return 1f;
        }
    }

    public sealed class UiAudioCue
    {
        public string CueId;
        public string InteractionId;
        public long AuthorityTick;
        public bool IsFallback;
    }

    public sealed class AudioMixerGroups
    {
        public float Master = 1f;
        public float Music = 1f;
        public float Battle = 1f;
        public float Ui = 1f;
        public bool Muted;

        public void SetGroup(string groupId, float value)
        {
            value = Math.Max(0f, Math.Min(1f, value));
            if (string.Equals(groupId, "master", StringComparison.Ordinal)) Master = value;
            else if (string.Equals(groupId, "music", StringComparison.Ordinal)) Music = value;
            else if (string.Equals(groupId, "battle", StringComparison.Ordinal)) Battle = value;
            else if (string.Equals(groupId, "ui", StringComparison.Ordinal)) Ui = value;
        }
    }

    public sealed class UiAudioEventRouter
    {
        private readonly Dictionary<string, long> lastTickByCue =
            new Dictionary<string, long>(StringComparer.Ordinal);
        private readonly HashSet<string> knownCueIds =
            new HashSet<string>(UiAudioCueIds.All, StringComparer.Ordinal);

        public AudioMixerGroups Mixer { get; private set; }
        public bool Enabled { get; private set; }
        public int CooldownTicks { get; set; }
        public int RejectedCount { get; private set; }

        public UiAudioEventRouter()
        {
            Mixer = new AudioMixerGroups();
            Enabled = true;
            CooldownTicks = 2;
        }

        public UiAudioCue Consume(string cueId, string interactionId, long authorityTick,
            bool controlEnabled = true)
        {
            if (!knownCueIds.Contains(cueId ?? string.Empty) || !controlEnabled || !Enabled ||
                Mixer.Muted || Mixer.Master <= 0f || Mixer.Ui <= 0f)
            {
                RejectedCount++;
                return null;
            }
            long lastTick;
            if (lastTickByCue.TryGetValue(cueId, out lastTick) &&
                authorityTick - lastTick < CooldownTicks)
            {
                RejectedCount++;
                return null;
            }
            lastTickByCue[cueId] = authorityTick;
            return new UiAudioCue
            {
                CueId = cueId,
                InteractionId = interactionId,
                AuthorityTick = authorityTick,
                IsFallback = true
            };
        }

        public void SetEnabled(bool enabled) { Enabled = enabled; }
        public void SetMuted(bool muted) { Mixer.Muted = muted; }
        public void ResetCooldowns() { lastTickByCue.Clear(); }
    }
}
