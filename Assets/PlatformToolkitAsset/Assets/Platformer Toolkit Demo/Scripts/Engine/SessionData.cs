// SessionData.cs
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class SessionData {

        public int KingdomIndex { get; }
        public int LevelIndex { get; }
        public TrialType? ActiveTrialType { get; }

        // Collectibles picked up during this run
        public int SmallCollectiblesThisRun { get; set; }
        public int BigCollectiblesThisRun { get; set; }

        // Timer — updated by a level-side component each frame
        public float ElapsedTime { get; set; }

        // Moves unlocked during this level run, not yet committed to save
        public List<string> MovesUnlockedThisLevel { get; } = new List<string>();

        public SessionData(int kingdomIndex, int levelIndex, TrialType? trialType) {
            KingdomIndex = kingdomIndex;
            LevelIndex = levelIndex;
            ActiveTrialType = trialType;
        }
    }
}
