// MapNode.cs
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class MapNode : MonoBehaviour {

        [Header("Definition")]
        public MapNodeType nodeType;
        public LevelDefinition levelDefinition;
        // Only set for Kingdom type nodes
        public KingdomDefinition kingdomDefinition;

        [Header("Connections")]
        // Each direction can lead to a different node
        // Leave null if no connection exists in that direction
        public MapNode northNode;
        public MapNode southNode;
        public MapNode eastNode;
        public MapNode westNode;

        // The paths leading TO each connected node
        // These are used to check unlock state before allowing movement
        public MapPath northPath;
        public MapPath southPath;
        public MapPath eastPath;
        public MapPath westPath;

        [Header("Unlock State")]
        // Set in the Inspector — which kingdom and level index this node represents
        // Used to check SaveData for unlock state
        public int kingdomIndex;
        public int levelIndex;
        // For kingdom nodes on the world map, levelIndex is ignored

        [Header("Visuals")]
        [SerializeField] private SpriteRenderer nodeSprite;
        [SerializeField] private GameObject lockedVisual;
        [SerializeField] private GameObject unlockedVisual;

        private void Start() {
            RefreshVisuals();
        }

        public bool IsUnlocked() {
            var save = GameManager.Instance.SaveData;

            switch (nodeType) {
                case MapNodeType.Level:
                    // Level 0 in kingdom 0 is always unlocked
                    if (kingdomIndex == 0 && levelIndex == 0) return true;
                    // Otherwise check if the previous level is completed
                    if (levelIndex == 0) {
                        // First level of a kingdom — check if kingdom is unlocked
                        return save.kingdoms[kingdomIndex].unlocked;
                    }
                    return save.kingdoms[kingdomIndex]
                        .levels[levelIndex - 1].completed;

                case MapNodeType.Kingdom:
                    return save.kingdoms[kingdomIndex].unlocked;

                case MapNodeType.Bonus:
                    return save.kingdoms[kingdomIndex].bonusLevelUnlocked;

                default:
                    return false;
            }
        }

        public bool IsCompleted() {
            if (nodeType != MapNodeType.Level) return false;
            return GameManager.Instance.SaveData
                .kingdoms[kingdomIndex].levels[levelIndex].completed;
        }

        public void RefreshVisuals() {
            bool unlocked = IsUnlocked();
            if (lockedVisual != null) lockedVisual.SetActive(!unlocked);
            if (unlockedVisual != null) unlockedVisual.SetActive(unlocked);
        }

        // Returns the connected node in a given direction, or null if none
        public MapNode GetNodeInDirection(Vector2 direction) {
            if (direction.y > 0.5f) return northNode;
            if (direction.y < -0.5f) return southNode;
            if (direction.x > 0.5f) return eastNode;
            if (direction.x < -0.5f) return westNode;
            return null;
        }

        // Returns the path leading in a given direction, or null if none
        public MapPath GetPathInDirection(Vector2 direction) {
            if (direction.y > 0.5f) return northPath;
            if (direction.y < -0.5f) return southPath;
            if (direction.x > 0.5f) return eastPath;
            if (direction.x < -0.5f) return westPath;
            return null;
        }
    }
}
