// MoveUnlockManager.cs
using System;
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // Attach to the player. On level load, enables components
    // for all currently unlocked moves. Also handles mid-level unlocks.
    public class MoveUnlockManager : MonoBehaviour {

        private void Start() {
            // Apply all permanently unlocked moves on level start
            foreach (var moveId in GameManager.Instance.SaveData.unlockedMoveIds) {
                EnableMoveComponent(moveId);
            }
        }

        // Called by the in-level unlock trigger
        public void UnlockMove(MoveDefinition move) {
            GameManager.Instance.UnlockMoveMidLevel(move.moveId);
            EnableMoveComponent(move.moveId);
            // Here you'd also trigger the cutscene / unlock animation
        }

        private void EnableMoveComponent(string moveId) {
            // Find the MoveDefinition by id to get the component type name
            // In a larger project you might cache these; fine for now
            var component = GetComponent(Type.GetType(
                $"GMTK.PlatformerToolkit.{moveId}"
            ));
            if (component is MonoBehaviour mb) {
                mb.enabled = true;
            }
        }
    }
}
