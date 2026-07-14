// SaveData.cs
//KillMe
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {
    [System.Serializable]
    
    
    public class LevelSaveData {
        public bool completed;
        public int bestSmallCollectibles;
        public int bestBigCollectibles;
        public SerializableDictionary<TrialType, float> bestTimes
            = new SerializableDictionary<TrialType, float>();
    }

    [System.Serializable]
    public class KingdomSaveData {
        public bool unlocked;
        public bool bonusLevelUnlocked;
        public List<LevelSaveData> levels = new List<LevelSaveData>();
    }

    [System.Serializable]
    public class SaveData {

        // World map state — where to place the player on load
        public int lastKingdomIndex = 0;
        public int lastLevelNodeIndex = 0;
        
        public List<string> playedUnlockAnimations = new List<string>();
        
        public bool carryOverMusic = false;

        // Per-kingdom, per-level progress
        public List<KingdomSaveData> kingdoms = new List<KingdomSaveData>();

        // Unlocked moves — stored by MoveDefinition id
        public List<string> unlockedMoveIds = new List<string>();

        // Small collectibles as spendable currency on the world map
        // This is a sum of bestSmallCollectibles across all levels,
        // minus what has already been spent
        public int spendableSmallCollectibles = 0;
    }
}
