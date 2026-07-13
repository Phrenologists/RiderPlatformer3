// CollectibleUI.cs
using UnityEngine;
using TMPro;

namespace GMTK.PlatformerToolkit {

    public class CollectibleUI : MonoBehaviour {

        public static CollectibleUI Instance { get; private set; }

        [Header("Small Collectibles")]
        [SerializeField] private TextMeshProUGUI smallCountText;
        // Displays running count only, e.g. "47"

        [Header("Big Collectibles")]
        [SerializeField] private TextMeshProUGUI bigCountText;
        // Displays current/total, e.g. "3 / 5"

        private int totalBig;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // Called by LevelManager on scene load to set initial state
        public void Initialise(int totalBigCollectibles) {
            totalBig = totalBigCollectibles;
            smallCountText.text = "0";
            bigCountText.text = $"0 / {totalBig}";
        }

        public void UpdateSmallCount(int current) {
            smallCountText.text = current.ToString();
        }

        public void UpdateBigCount(int current, int total) {
            bigCountText.text = $"{current} / {total}";
        }
    }
}
