// MusicManager.cs
using UnityEngine;
using DG.Tweening;
using UnityEngine.SceneManagement;

namespace GMTK.PlatformerToolkit {

    public class MusicManager : MonoBehaviour {

        public static MusicManager Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private AudioSource audioSource;

        [Header("Settings")]
        [SerializeField] private float fadeOutDuration = 0.5f;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private Ease fadeEase = Ease.InOutQuad;

        // If true, the current track keeps playing when the next scene loads
        private bool carryOverToNextScene = false;

        // The track that should play in the next scene if not carrying over
        private AudioClip pendingTrack = null;
        
        public bool MusicWasCarriedIn { get; private set; } = false;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // ── Public API ────────────────────────────────────────────────────

        // Play a track immediately, fading out the current one first
        public void PlayTrack(AudioClip clip, bool loop = true) {
            if (audioSource.isPlaying) {
                audioSource.DOFade(0f, fadeOutDuration)
                    .SetEase(fadeEase)
                    .OnComplete(() => StartTrack(clip, loop));
            } else {
                StartTrack(clip, loop);
            }
        }

        public void StopMusic() {
            audioSource.DOFade(0f, fadeOutDuration)
                .SetEase(fadeEase)
                .OnComplete(() => audioSource.Stop());
        }

        // Call this before triggering a scene transition to tell
        // the MusicManager what should happen when the next scene loads.
        // carryOver = true: keep playing whatever is currently playing
        // carryOver = false: fade out current track and play nextTrack instead
        //                    (pass null to just stop music in the next scene)
        public void SetNextSceneMusic(bool carryOver, AudioClip nextTrack = null) {
            carryOverToNextScene = carryOver;
            pendingTrack = nextTrack;
        }

        // ── Scene Handling ────────────────────────────────────────────────

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            if (carryOverToNextScene) {
                // Keep playing — do nothing to the audio source
                // Reset the flag so the scene after this one
                // doesn't also carry over unless explicitly set
                MusicWasCarriedIn = true;
                carryOverToNextScene = false;
                return;
            }
            MusicWasCarriedIn = false;

            if (pendingTrack != null) {
                PlayTrack(pendingTrack);
                pendingTrack = null;
            } else {
                StopMusic();
            }
        }

        // ── Internal ──────────────────────────────────────────────────────

        private void StartTrack(AudioClip clip, bool loop) {
            audioSource.clip = clip;
            audioSource.loop = loop;
            audioSource.volume = 0f;
            audioSource.Play();
            audioSource.DOFade(1f, fadeInDuration).SetEase(fadeEase);
        }
    }
}
