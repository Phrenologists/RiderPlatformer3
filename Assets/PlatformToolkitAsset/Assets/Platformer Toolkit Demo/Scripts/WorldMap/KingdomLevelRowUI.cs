// KingdomLevelRowUI.cs
using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    // Attach to the level row prefab used inside KingdomInfoPanel
    public class KingdomLevelRowUI : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private TextMeshProUGUI levelNameText;
        [SerializeField] private TextMeshProUGUI smallCollectiblesText;
        [SerializeField] private TextMeshProUGUI bigCollectiblesText;
        [SerializeField] private Transform trialListParent;
        [SerializeField] private GameObject trialEntryPrefab;

        private List<GameObject> trialEntries = new List<GameObject>();

        public void Initialise(LevelDefinition levelDef, LevelSaveData levelSave) {
            levelNameText.text = levelDef.levelName;

            // Small collectibles
            smallCollectiblesText.text =
                $"{levelSave.bestSmallCollectibles}";

            // Big collectibles
            bigCollectiblesText.text =
                $"{levelSave.bestBigCollectibles} / {levelDef.totalBigCollectibles}";

            // Time trials
            RefreshTrialList(levelDef, levelSave);
        }

        private void RefreshTrialList(
            LevelDefinition levelDef,
            LevelSaveData levelSave) {

            foreach (var entry in trialEntries) Destroy(entry);
            trialEntries.Clear();

            // If level not completed, show nothing in the trial section
            if (!levelSave.completed) return;

            foreach (var trialType in levelDef.availableTrialTypes) {
                var entryObj = Instantiate(trialEntryPrefab, trialListParent);
                trialEntries.Add(entryObj);

                var entry = entryObj.GetComponent<TrialEntryUI>();
                if (entry == null) continue;

                float bestTime;
                bool hasTime = levelSave.bestTimes.TryGetValue(
                    trialType, out bestTime
                );

                entry.Initialise(
                    trialType: trialType,
                    timeDisplay: hasTime ? FormatTime(bestTime) : "--:--"
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
