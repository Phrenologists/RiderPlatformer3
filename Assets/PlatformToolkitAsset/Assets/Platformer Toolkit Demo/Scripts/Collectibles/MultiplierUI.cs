// MultiplierUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class MultiplierUI : MonoBehaviour {

        public static MultiplierUI Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private Image timerBar;
        // Optional — leave null if you only want the text

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.2f;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void Show() {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration);
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide() {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        }

        public void UpdateTimer(float remaining, float max) {
            // Format as seconds with one decimal place
            timerText.text = remaining.ToString("F1") + "s";

            // Update bar fill if assigned
            if (timerBar != null) {
                timerBar.fillAmount = remaining / max;
            }
        }
    }
}
