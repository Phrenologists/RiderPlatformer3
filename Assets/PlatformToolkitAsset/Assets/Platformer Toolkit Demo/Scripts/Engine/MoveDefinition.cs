// ScriptableObjects/MoveDefinition.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    [CreateAssetMenu(fileName = "MoveDefinition", menuName = "PlatformerToolkit/Move Definition")]
    public class MoveDefinition : ScriptableObject {
        public string moveId;
        public string moveName;
        [TextArea] public string description;
        // The component type name to enable on the player when this move is unlocked.
        // Stored as a string so it serializes cleanly; resolved to a Type at runtime.
        public string componentTypeName;
    }
}
