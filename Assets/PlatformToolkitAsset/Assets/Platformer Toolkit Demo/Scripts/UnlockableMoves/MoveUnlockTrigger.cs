// MoveUnlockTrigger.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // Place in a level where a move should be unlocked
    // Works with MoveUnlockManager on the player
    public class MoveUnlockTrigger : MonoBehaviour {

        [SerializeField] private MoveDefinition moveToUnlock;
        private bool triggered = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (triggered) return;
            var unlockManager = other.GetComponent<MoveUnlockManager>();
            if (unlockManager == null) return;

            triggered = true;
            unlockManager.UnlockMove(moveToUnlock);
            // Optionally hide or animate the trigger object here
            gameObject.SetActive(false);
        }
    }
}
