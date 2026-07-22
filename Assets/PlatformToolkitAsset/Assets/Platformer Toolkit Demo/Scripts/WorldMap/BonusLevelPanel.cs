// BonusLevelPanel.cs
using UnityEngine;
using TMPro;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class BonusLevelPanel : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI placeholderText;

        [Header("Settings")]
        [SerializeField] private float animDuration = 0.2f;

        private void Awake() {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;

            if (placeholderText != null)
                placeholderText.text = "Bonus Levels\n(Coming Soon)";
        }

        public void ShowPlaceholder() {
            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, animDuration);
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide() {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, animDuration)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        }
    }
}
