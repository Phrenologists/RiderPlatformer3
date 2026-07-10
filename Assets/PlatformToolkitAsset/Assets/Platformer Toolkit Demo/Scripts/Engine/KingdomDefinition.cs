// ScriptableObjects/KingdomDefinition.cs
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    [CreateAssetMenu(fileName = "KingdomDefinition", menuName = "PlatformerToolkit/Kingdom Definition")]
    public class KingdomDefinition : ScriptableObject {
        public string kingdomId;
        public string kingdomName;
        public List<LevelDefinition> mainLevels = new List<LevelDefinition>();
        public List<LevelDefinition> bonusLevels = new List<LevelDefinition>();

        // Which main level index (0-based) unlocks the bonus level tile
        public int bonusUnlockLevelIndex;
    }
}
