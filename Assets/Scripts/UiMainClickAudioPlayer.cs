using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    [DisallowMultipleComponent]
    public sealed class UiMainClickAudioPlayer : MonoBehaviour
    {
        private static UiMainClickAudioPlayer instance;
        private AudioSource audioSource;
        private readonly UiAudioResourceCache<AudioClip> resourceCache =
            new UiAudioResourceCache<AudioClip>();
        private UiAudioEventRouter router;

        public int SuccessfulPlayCount { get; private set; }
        public int FallbackCount { get; private set; }
        public string LastCueId { get; private set; }
        public string LastResourcePath { get; private set; }
        public int CachedResourceCount { get { return resourceCache.Count; } }

        public static UiMainClickAudioPlayer CreateOrGet(UiAudioEventRouter eventRouter)
        {
            if (instance == null)
            {
                var host = new GameObject("UI Main Click Audio");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<UiMainClickAudioPlayer>();
            }
            instance.Configure(eventRouter);
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

        public void Configure(UiAudioEventRouter eventRouter)
        {
            router = eventRouter;
        }

        public bool TryPlay(string cueId, string interactionId, long authorityTick,
            bool controlEnabled = true)
        {
            if (router == null) return false;
            var cue = router.Consume(cueId, interactionId, authorityTick, controlEnabled);
            if (cue == null) return false;
            LastCueId = cue.CueId;

            var resourcePath = UiMainClickAudioPolicy.ResolveResourcePath(cue.CueId);
            if (string.IsNullOrEmpty(resourcePath))
            {
                FallbackCount++;
                return false;
            }
            LastResourcePath = resourcePath;
            var clip = resourceCache.GetOrLoad(resourcePath,
                () => Resources.Load<AudioClip>(resourcePath));
            if (clip == null)
            {
                FallbackCount++;
                return false;
            }

            cue.IsFallback = false;
            var volume = router.Mixer.Master * router.Mixer.Ui *
                UiMainClickAudioPolicy.ResolveVolumeScale(resourcePath);
            if (volume <= 0f) return false;
            audioSource.PlayOneShot(clip, volume);
            SuccessfulPlayCount++;
            return true;
        }
    }
}
