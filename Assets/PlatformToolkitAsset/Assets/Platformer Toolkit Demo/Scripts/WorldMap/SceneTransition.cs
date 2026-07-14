// SceneTransition.cs
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;
using UnityEngine.SceneManagement;

namespace GMTK.PlatformerToolkit {

    public class SceneTransition : MonoBehaviour {

        public static SceneTransition Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        // A CanvasGroup on a full-screen black Image child of this GameObject

        [Header("Settings")]
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private Ease transitionEase = Ease.InOutQuad;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Start invisible
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void OnEnable() {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable() {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        // Called by anything that wants to transition to a new scene
        public void TransitionToScene(string sceneName) {
            TransitionOut(() => SceneManager.LoadScene(sceneName));
        }

        // Fade out (to black), then call onComplete
        // Virtual so it can be overridden with a proper animation later
        public virtual void TransitionOut(Action onComplete) {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.DOFade(1f, transitionDuration)
                .SetEase(transitionEase)
                .SetUpdate(UpdateType.Normal, true)
                // SetUpdate true = runs even if Time.timeScale is 0,
                // important since some screens may pause the game
                .OnComplete(() => onComplete?.Invoke());
        }

        // Fade back in after a scene loads
        // Virtual for the same reason as TransitionOut
        public virtual void TransitionIn() {
            canvasGroup.DOFade(0f, transitionDuration)
                .SetEase(transitionEase)
                .SetUpdate(UpdateType.Normal, true)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            // Automatically fade in whenever a new scene loads
            TransitionIn();
        }
    }
}
