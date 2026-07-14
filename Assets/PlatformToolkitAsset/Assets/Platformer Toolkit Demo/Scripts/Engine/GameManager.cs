// GameManager.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK.PlatformerToolkit {

    public class GameManager : MonoBehaviour {

        public static GameManager Instance { get; private set; }

        [Header("Game Definitions")]
        [SerializeField] private List<KingdomDefinition> kingdoms;
        // Drag all KingdomDefinition assets here in order

        [Header("Scene Names")]
        [SerializeField] private string worldMapSceneName = "WorldMap";

        // Current save data
        public SaveData SaveData { get; private set; }

        // Session state — resets each level
        public SessionData Session { get; private set; }

        [SerializeField] private string editorSavePath = "SaveData";
        
        
        private string SavePath {
            get {
#if UNITY_EDITOR
                // Use a path relative to the project root during development
                string fullPath = System.IO.Path.Combine(
                    System.IO.Directory.GetParent(Application.dataPath).FullName,
                    editorSavePath
                );
                // Create the folder if it doesn't exist yet
                if (!System.IO.Directory.Exists(fullPath))
                    System.IO.Directory.CreateDirectory(fullPath);
                return System.IO.Path.Combine(fullPath, "save.json");
#else
            // Always use persistentDataPath in a real build
            return System.IO.Path.Combine(Application.persistentDataPath, "save.json");
#endif
            }
        }
        
        [SerializeField] private AudioClip worldMapMusicClip;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadGame();
        }

        // ── Save / Load ───────────────────────────────────────────────────

        public void LoadGame() {
            if (File.Exists(SavePath)) {
                string json = File.ReadAllText(SavePath);
                SaveData = JsonUtility.FromJson<SaveData>(json);
            } else {
                SaveData = CreateNewSave();
                WriteToDisk();
            }
        }

        public void WriteToDisk() {
            string json = JsonUtility.ToJson(SaveData, prettyPrint: true);
            File.WriteAllText(SavePath, json);
        }

        private SaveData CreateNewSave() {
            var save = new SaveData();

            // Initialise kingdom and level data from definitions
            foreach (var kingdom in kingdoms) {
                var kingdomSave = new KingdomSaveData();
                foreach (var level in kingdom.mainLevels) {
                    kingdomSave.levels.Add(new LevelSaveData());
                }
                save.kingdoms.Add(kingdomSave);
            }

            // First kingdom starts unlocked
            save.kingdoms[0].unlocked = true;

            return save;
        }

        // ── Session Management ────────────────────────────────────────────

        public void StartLevel(int kingdomIndex, int levelIndex, TrialType? trialType = null) {
            Session = new SessionData(kingdomIndex, levelIndex, trialType);
            var level = kingdoms[kingdomIndex].mainLevels[levelIndex];
            SceneTransition.Instance.TransitionToScene(worldMapSceneName);
        }
        public void CreateTestSession(int kingdomIndex = 0, int levelIndex = 0) {
            Session = new SessionData(kingdomIndex, levelIndex, null);
        }

        // Called by the level's end trigger
        public void CompleteLevel() {
            int ki = Session.KingdomIndex;
            int li = Session.LevelIndex;
            var levelSave = SaveData.kingdoms[ki].levels[li];
            var levelDef = kingdoms[ki].mainLevels[li];

            // Mark completed
            levelSave.completed = true;

            // Update best small collectibles
            if (Session.SmallCollectiblesThisRun > levelSave.bestSmallCollectibles) {
                int gained = Session.SmallCollectiblesThisRun - levelSave.bestSmallCollectibles;
                levelSave.bestSmallCollectibles = Session.SmallCollectiblesThisRun;
                // Only add the improvement to the spendable pool, not the full amount
                // (avoids exploiting replays to farm currency)
                SaveData.spendableSmallCollectibles += gained;
            }

            // Update best big collectibles
            levelSave.bestBigCollectibles = Mathf.Max(
                levelSave.bestBigCollectibles,
                Session.BigCollectiblesThisRun
            );

            // Update time trial if this was one
            if (Session.ActiveTrialType.HasValue) {
                float currentTime = Session.ElapsedTime;
                float existingBest;
                if (!levelSave.bestTimes.TryGetValue(Session.ActiveTrialType.Value, out existingBest)
                    || currentTime < existingBest) {
                    levelSave.bestTimes[Session.ActiveTrialType.Value] = currentTime;
                }
            }

            // Commit any moves unlocked during this level
            foreach (var moveId in Session.MovesUnlockedThisLevel) {
                if (!SaveData.unlockedMoveIds.Contains(moveId)) {
                    SaveData.unlockedMoveIds.Add(moveId);
                }
            }

            // Unlock next level if there is one
            if (li + 1 < SaveData.kingdoms[ki].levels.Count) {
                // Next level node unlocked — handled by world map reading completed state
            }

            // Unlock next kingdom if this was the last main level
            if (li == kingdoms[ki].mainLevels.Count - 1
                && ki + 1 < SaveData.kingdoms.Count) {
                SaveData.kingdoms[ki + 1].unlocked = true;
            }

            // Unlock bonus level tile if applicable
            if (li == kingdoms[ki].bonusUnlockLevelIndex) {
                SaveData.kingdoms[ki].bonusLevelUnlocked = true;
            }
            
            

            WriteToDisk();
            
            var musicLevelDef = kingdoms[Session.KingdomIndex].mainLevels[Session.LevelIndex];

            MusicManager.Instance.SetNextSceneMusic(
                carryOver: musicLevelDef.carryMusicToWorldMap,
                nextTrack: musicLevelDef.carryMusicToWorldMap ? null : worldMapMusicClip
            );
            SceneTransition.Instance.TransitionToScene(worldMapSceneName);
        }

        // ── Move Unlock Helpers ───────────────────────────────────────────

        public bool IsMoveUnlocked(string moveId) {
            return SaveData.unlockedMoveIds.Contains(moveId)
                || (Session != null && Session.MovesUnlockedThisLevel.Contains(moveId));
        }

        // Called mid-level when the player hits the unlock trigger.
        // Stored in session only — committed to save on CompleteLevel()
        public void UnlockMoveMidLevel(string moveId) {
            if (!Session.MovesUnlockedThisLevel.Contains(moveId)) {
                Session.MovesUnlockedThisLevel.Add(moveId);
            }
        }

        // ── World Map Helpers ─────────────────────────────────────────────

        public void SetLastMapPosition(int kingdomIndex, int levelNodeIndex) {
            SaveData.lastKingdomIndex = kingdomIndex;
            SaveData.lastLevelNodeIndex = levelNodeIndex;
            WriteToDisk();
        }

        public KingdomDefinition GetKingdomDefinition(int index) => kingdoms[index];
        public int KingdomCount => kingdoms.Count;
    }
}
