using System.Collections.Generic;
using SeekerAutoChess.Core;
using UnityEngine;

namespace SeekerAutoChess.Prototype
{
    [DisallowMultipleComponent]
    public sealed class HomeBackgroundMusicPlayer : MonoBehaviour
    {
        private static HomeBackgroundMusicPlayer instance;
        private AudioSource[] sources = new AudioSource[2];
        private Dictionary<BackgroundMusicTrack, AudioClip> clips =
            new Dictionary<BackgroundMusicTrack, AudioClip>();
        private HashSet<string> missingResourcePaths = new HashSet<string>();
        private AudioMixerGroups mixer;
        private int activeSourceIndex = -1;
        private int outgoingSourceIndex = -1;
        private float outgoingStartVolume;
        private float transitionElapsed;
        private float transitionDuration;
        private bool transitioning;
        private bool hasCurrentTrack;
        private BackgroundMusicTrack currentTrack;

        public string LoadedResourcePath { get; private set; }
        public int SuccessfulStartCount { get; private set; }
        public int MissingResourceCount { get; private set; }
        public BackgroundMusicTrack CurrentTrack { get { return currentTrack; } }

        public static HomeBackgroundMusicPlayer CreateOrGet(AudioMixerGroups audioMixer)
        {
            if (instance == null)
            {
                var host = new GameObject("Scene Background Music");
                DontDestroyOnLoad(host);
                instance = host.AddComponent<HomeBackgroundMusicPlayer>();
            }
            instance.Configure(audioMixer);
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
            EnsureSources();
        }

        private void Update()
        {
            AdvanceTransition(Time.unscaledDeltaTime);
        }

        public void Configure(AudioMixerGroups audioMixer)
        {
            mixer = audioMixer;
            EnsureSources();
        }

        public void SetTrack(BackgroundMusicTrack track)
        {
            EnsureSources();
            if (hasCurrentTrack && currentTrack == track) return;

            var resourcePath = BackgroundMusicPolicy.ResourcePath(track);
            AudioClip nextClip;
            if (!clips.TryGetValue(track, out nextClip))
            {
                if (missingResourcePaths.Contains(resourcePath)) return;
                LoadedResourcePath = resourcePath;
                nextClip = Resources.Load<AudioClip>(resourcePath);
                if (nextClip == null)
                {
                    missingResourcePaths.Add(resourcePath);
                    MissingResourceCount++;
                    return;
                }
                clips[track] = nextClip;
            }

            var previousTrack = currentTrack;
            var nextSourceIndex = activeSourceIndex < 0 ? 0 : 1 - activeSourceIndex;
            var nextSource = sources[nextSourceIndex];
            nextSource.Stop();
            nextSource.clip = nextClip;
            nextSource.time = 0f;
            nextSource.volume = 0f;
            nextSource.Play();

            outgoingSourceIndex = activeSourceIndex;
            outgoingStartVolume = outgoingSourceIndex >= 0 ? sources[outgoingSourceIndex].volume : 0f;
            activeSourceIndex = nextSourceIndex;
            currentTrack = track;
            transitionElapsed = 0f;
            transitionDuration = hasCurrentTrack
                ? BackgroundMusicPolicy.TransitionSeconds(previousTrack, track)
                : BackgroundMusicPolicy.InitialFadeInSeconds;
            hasCurrentTrack = true;
            transitioning = true;
            LoadedResourcePath = resourcePath;
            SuccessfulStartCount++;
        }

        // Retained for compatibility with older prototype integrations.
        public void SetHomeActive(bool active)
        {
            if (active) SetTrack(BackgroundMusicTrack.Home);
        }

        private void EnsureSources()
        {
            // Unity can preserve a live MonoBehaviour while non-serialized fields are
            // cleared during script/domain reload. Recreate all runtime-only state
            // before indexing it so Play Mode hot reload cannot strand the music host.
            if (sources == null || sources.Length != 2)
                sources = new AudioSource[2];
            if (clips == null)
                clips = new Dictionary<BackgroundMusicTrack, AudioClip>();
            if (missingResourcePaths == null)
                missingResourcePaths = new HashSet<string>();
            if (sources[0] != null && sources[1] != null) return;
            var existing = GetComponents<AudioSource>();
            for (var i = 0; i < sources.Length; i++)
            {
                sources[i] = i < existing.Length ? existing[i] : gameObject.AddComponent<AudioSource>();
                sources[i].playOnAwake = false;
                sources[i].loop = true;
                sources[i].spatialBlend = 0f;
            }
        }

        private void AdvanceTransition(float deltaTime)
        {
            if (activeSourceIndex < 0) return;
            EnsureSources();
            if (activeSourceIndex >= sources.Length || sources[activeSourceIndex] == null)
            {
                activeSourceIndex = -1;
                outgoingSourceIndex = -1;
                transitioning = false;
                return;
            }
            var targetVolume = BackgroundMusicPolicy.ResolveVolume(mixer, currentTrack);
            if (!transitioning)
            {
                sources[activeSourceIndex].volume = targetVolume;
                return;
            }

            transitionElapsed += Mathf.Max(0f, deltaTime);
            var progress = transitionDuration <= 0f ? 1f : Mathf.Clamp01(transitionElapsed / transitionDuration);
            var radians = progress * Mathf.PI * 0.5f;
            sources[activeSourceIndex].volume = targetVolume * Mathf.Sin(radians);
            if (outgoingSourceIndex >= 0 && outgoingSourceIndex < sources.Length &&
                sources[outgoingSourceIndex] != null)
                sources[outgoingSourceIndex].volume = outgoingStartVolume * Mathf.Cos(radians);

            if (progress < 1f) return;
            if (outgoingSourceIndex >= 0 && outgoingSourceIndex < sources.Length &&
                sources[outgoingSourceIndex] != null)
            {
                sources[outgoingSourceIndex].Stop();
                sources[outgoingSourceIndex].clip = null;
            }
            outgoingSourceIndex = -1;
            sources[activeSourceIndex].volume = targetVolume;
            transitioning = false;
        }
    }
}
