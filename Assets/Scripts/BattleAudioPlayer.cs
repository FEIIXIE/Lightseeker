using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    [DisallowMultipleComponent]
    public sealed class BattleAudioPlayer : MonoBehaviour
    {
        private static BattleAudioPlayer instance;
        private AudioSource audioSource;
        private readonly UiAudioResourceCache<AudioClip> resourceCache =
            new UiAudioResourceCache<AudioClip>();
        private BattleAudioEventRouter router;
        private AudioMixerGroups mixer;

        public int SuccessfulPlayCount { get; private set; }
        public int FallbackCount { get; private set; }
        public int CachedResourceCount { get { return resourceCache.Count; } }

        public static BattleAudioPlayer CreateOrGet(BattleAudioEventRouter eventRouter,
            AudioMixerGroups audioMixer)
        {
            if (instance == null)
            {
                var host = new GameObject("Battle Audio");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<BattleAudioPlayer>();
            }
            instance.router = eventRouter;
            instance.mixer = audioMixer;
            return instance;
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        public bool TryPlay(BattleAudioCue cue)
        {
            if (cue == null || router == null || !router.Enabled) return false;
            var resourcePath = BattleAudioResourcePolicy.ResolveResourcePath(cue.CueId);
            if (string.IsNullOrEmpty(resourcePath))
            {
                FallbackCount++;
                return false;
            }
            var clip = resourceCache.GetOrLoad(resourcePath,
                () => Resources.Load<AudioClip>(resourcePath));
            if (clip == null)
            {
                FallbackCount++;
                return false;
            }
            var master = mixer == null ? 1f : mixer.Master;
            var battle = mixer == null ? 1f : mixer.Battle;
            if (mixer != null && mixer.Muted) return false;
            var volume = master * battle * BattleAudioResourcePolicy.ResolveVolumeScale(resourcePath);
            if (volume <= 0f) return false;
            audioSource.pitch = cue.PlaybackRate;
            audioSource.PlayOneShot(clip, volume);
            SuccessfulPlayCount++;
            return true;
        }
    }
}
