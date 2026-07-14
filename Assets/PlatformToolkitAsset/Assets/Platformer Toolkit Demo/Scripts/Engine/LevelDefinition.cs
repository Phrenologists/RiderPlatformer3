// ScriptableObjects/LevelDefinition.cs
using System.Collections.Generic;
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "PlatformerToolkit/Level Definition")]
    public class LevelDefinition : ScriptableObject {
        public string levelId;
        public string sceneName;
        public int totalSmallCollectibles;
        public int totalBigCollectibles;
        public List<TrialType> availableTrialTypes = new List<TrialType>();
        public bool carryMusicToWorldMap = false;
        public bool carryMusicToNextScene = false;

        // Null if this level unlocks no move
        public MoveDefinition moveUnlockedHere;
    }
}
