// PowerupUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class PowerupUI : MonoBehaviour {

        public static PowerupUI Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image powerupIcon;
        [SerializeField] private TextMeshProUGUI counterText;

        [Header("Settings")]
        [SerializeField] private float fadeDuration = 0.2f;

        private MountPowerup trackedPowerup;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        private void Update() {
            if (trackedPowerup == null) return;
            counterText.text = trackedPowerup.GetCounterDisplay();
        }

        public void ShowPowerup(MountPowerup powerup) {
            trackedPowerup = powerup;

            if (powerupIcon != null && powerup.icon != null)
                powerupIcon.sprite = powerup.icon;

            // Hide counter if powerup never expires
            counterText.gameObject.SetActive(
                powerup.expiryType != PowerupExpiry.None
            );

            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, fadeDuration);
            canvasGroup.blocksRaycasts = true;
        }

        public void HidePowerup() {
            trackedPowerup = null;
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        }
    }
}
