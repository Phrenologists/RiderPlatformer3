// KingdomInfoPanel.cs
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class KingdomInfoPanel : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI kingdomNameText;
        [SerializeField] private Transform levelListParent;
        [SerializeField] private GameObject levelRowPrefab;

        [Header("Settings")]
        [SerializeField] private float animDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutQuad;
        [SerializeField] private Ease hideEase = Ease.InQuad;

        private List<GameObject> levelRows = new List<GameObject>();

        private void Awake() {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void Show(int kingdomIndex) {
            var kingdomDef = GameManager.Instance
                .GetKingdomDefinition(kingdomIndex);
            var kingdomSave = GameManager.Instance
                .SaveData.kingdoms[kingdomIndex];

            kingdomNameText.text = kingdomDef.kingdomName;

            RefreshLevelList(kingdomDef, kingdomSave);

            canvasGroup.DOKill();
            canvasGroup.DOFade(1f, animDuration).SetEase(showEase);
            canvasGroup.blocksRaycasts = true;
        }

        public void Hide() {
            canvasGroup.DOKill();
            canvasGroup.DOFade(0f, animDuration)
                .SetEase(hideEase)
                .OnComplete(() => canvasGroup.blocksRaycasts = false);
        }

        private void RefreshLevelList(
            KingdomDefinition kingdomDef,
            KingdomSaveData kingdomSave) {

            foreach (var row in levelRows) Destroy(row);
            levelRows.Clear();

            for (int i = 0; i < kingdomDef.mainLevels.Count; i++) {
                var levelDef = kingdomDef.mainLevels[i];
                var levelSave = i < kingdomSave.levels.Count
                    ? kingdomSave.levels[i]
                    : new LevelSaveData();

                var rowObj = Instantiate(levelRowPrefab, levelListParent);
                levelRows.Add(rowObj);

                var row = rowObj.GetComponent<KingdomLevelRowUI>();
                if (row != null) {
                    row.Initialise(levelDef, levelSave);
                }
            }
        }
    }
}
