// LevelInfoPanel.cs
using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class LevelInfoPanel : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI smallCollectiblesText;
        [SerializeField] private TextMeshProUGUI bigCollectiblesText;
        [SerializeField] private Transform trialListParent;
        [SerializeField] private GameObject trialEntryPrefab;

        [Header("Settings")]
        [SerializeField] private float animDuration = 0.2f;
        [SerializeField] private Ease showEase = Ease.OutQuad;
        [SerializeField] private Ease hideEase = Ease.InQuad;

        private List<GameObject> trialEntries = new List<GameObject>();

        private void Awake() {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
        }

        public void Show(MapNode node, int kingdomIndex) {
            if (node.levelDefinition == null) return;

            var levelDef = node.levelDefinition;
            var levelSave = GameManager.Instance.SaveData
                .kingdoms[kingdomIndex].levels[node.levelIndex];

            // Level name
            levelNameText.text = levelDef.levelName;

            // Small collectibles — just show best count, no total
            smallCollectiblesText.text =
                $"{levelSave.bestSmallCollectibles}";

            // Big collectibles — show current / total
            bigCollectiblesText.text =
                $"{levelSave.bestBigCollectibles} / {levelDef.totalBigCollectibles}";

            // Time trials — only show if level has been completed
            RefreshTrialList(levelDef, levelSave);

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

        private void RefreshTrialList(LevelDefinition levelDef, LevelSaveData levelSave) {
            // Clear existing entries
            foreach (var entry in trialEntries) {
                Destroy(entry);
            }
            trialEntries.Clear();

            // Don't show trials at all if the level hasn't been completed
            if (!levelSave.completed) return;

            foreach (var trialType in levelDef.availableTrialTypes) {
                var entryObj = Instantiate(trialEntryPrefab, trialListParent);
                trialEntries.Add(entryObj);

                var entry = entryObj.GetComponent<TrialEntryUI>();
                if (entry == null) continue;

                float bestTime;
                bool hasTime = levelSave.bestTimes.TryGetValue(trialType, out bestTime);

                entry.Initialise(
                    trialType: trialType,
                    timeDisplay: hasTime ? FormatTime(bestTime) : "-- : --"
                );
            }
        }

        private string FormatTime(float seconds) {
            int mins = Mathf.FloorToInt(seconds / 60f);
            int secs = Mathf.FloorToInt(seconds % 60f);
            int centiseconds = Mathf.FloorToInt((seconds * 100f) % 100f);
            return $"{mins:00}:{secs:00}.{centiseconds:00}";
        }
    }
}
