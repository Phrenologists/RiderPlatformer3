// GlobalCollectiblePanel.cs
using UnityEngine;
using TMPro;

namespace GMTK.PlatformerToolkit {

    public class GlobalCollectiblePanel : MonoBehaviour {

        [SerializeField] private TextMeshProUGUI smallCollectiblesText;
        [SerializeField] private TextMeshProUGUI bigCollectiblesText;

        public void Refresh() {
            var save = GameManager.Instance.SaveData;

            // Sum big collectibles across all kingdoms and levels
            int totalBig = 0;
            foreach (var kingdom in save.kingdoms) {
                foreach (var level in kingdom.levels) {
                    totalBig += level.bestBigCollectibles;
                }
            }

            smallCollectiblesText.text =
                $"{save.spendableSmallCollectibles}";
            bigCollectiblesText.text = $"{totalBig}";
        }
    }
}
