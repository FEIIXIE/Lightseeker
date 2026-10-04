using System;

namespace SeekerAutoChess.Core
{
    public enum BackgroundMusicTrack
    {
        Home,
        Expedition,
        Battle
    }

    /// <summary>
    /// Authoritative routing and transition settings for the three runtime BGM tracks.
    /// The short expedition and battle assets already contain their own loop-boundary
    /// crossfades; these durations apply only when changing gameplay contexts.
    /// </summary>
    public static class BackgroundMusicPolicy
    {
        public const string HomeResourcePath =
            "Audio/Music/BGM_HOME_KINGDOM_QUEST_SOUNDIMAGE_V01";
        public const string ExpeditionResourcePath =
            "Audio/Music/BGM_EXPEDITION_THE_VOYAGE_BEGINS_30S_LOOP_SOUNDIMAGE_V02";
        public const string BattleResourcePath =
            "Audio/Music/BGM_BATTLE_TRAINING_SOUNDIMAGE_V01";

        public const float HomeVolumeScale = 0.55f;
        public const float ExpeditionVolumeScale = 0.55f;
        public const float BattleVolumeScale = 0.55f;
        public const float InitialFadeInSeconds = 1.0f;

        public static BackgroundMusicTrack ResolveTrack(bool inExpedition, bool battleActive)
        {
            if (battleActive) return BackgroundMusicTrack.Battle;
            return inExpedition ? BackgroundMusicTrack.Expedition : BackgroundMusicTrack.Home;
        }

        public static string ResourcePath(BackgroundMusicTrack track)
        {
            switch (track)
            {
                case BackgroundMusicTrack.Expedition:
                    return ExpeditionResourcePath;
                case BackgroundMusicTrack.Battle:
                    return BattleResourcePath;
                default:
                    return HomeResourcePath;
            }
        }

        public static float ResolveVolume(AudioMixerGroups mixer, BackgroundMusicTrack track)
        {
            if (mixer == null || mixer.Muted) return 0f;
            var scale = HomeVolumeScale;
            if (track == BackgroundMusicTrack.Expedition) scale = ExpeditionVolumeScale;
            else if (track == BackgroundMusicTrack.Battle) scale = BattleVolumeScale;
            return Math.Max(0f, Math.Min(1f, mixer.Master * mixer.Music * scale));
        }

        public static float TransitionSeconds(BackgroundMusicTrack from, BackgroundMusicTrack to)
        {
            if (from == to) return 0f;
            if (from == BackgroundMusicTrack.Home && to == BackgroundMusicTrack.Expedition) return 1.0f;
            if (from == BackgroundMusicTrack.Expedition && to == BackgroundMusicTrack.Battle) return 0.8f;
            if (from == BackgroundMusicTrack.Battle && to == BackgroundMusicTrack.Expedition) return 1.5f;
            if (to == BackgroundMusicTrack.Battle) return 0.8f;
            return 1.2f;
        }
    }

    // Compatibility surface retained for existing tests and callers that only need the
    // original home-track volume contract.
    public static class HomeBackgroundMusicPolicy
    {
        public const string ResourcePath = BackgroundMusicPolicy.HomeResourcePath;
        public const float VolumeScale = BackgroundMusicPolicy.HomeVolumeScale;

        public static float ResolveVolume(AudioMixerGroups mixer)
        {
            return BackgroundMusicPolicy.ResolveVolume(mixer, BackgroundMusicTrack.Home);
        }
    }
}
