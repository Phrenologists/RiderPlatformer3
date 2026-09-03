// StickyCollisionListener.cs - updated with debug logging
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class StickyCollisionListener : MonoBehaviour {

        private StickyPowerup powerup;
        private LayerMask stickyLayers;

        [SerializeField] private bool showDebugLogs = true;

        public void Initialise(StickyPowerup stickyPowerup, LayerMask layers) {
            powerup = stickyPowerup;
            stickyLayers = layers;

            Debug.Log($"[StickyListener] Initialised on {gameObject.name}. " +
                $"Layer mask: {layers.value}");
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            bool sticky = IsSticky(collision.gameObject.layer);

            if (showDebugLogs) {
                Debug.Log($"[StickyListener] CollisionEnter: " +
                    $"{collision.gameObject.name}, " +
                    $"Layer: {LayerMask.LayerToName(collision.gameObject.layer)}, " +
                    $"IsSticky: {sticky}, " +
                    $"Normal: {(collision.contacts.Length > 0 ? collision.contacts[0].normal.ToString() : "none")}");
            }

            if (!sticky || collision.contacts.Length == 0) return;
            powerup?.OnSurfaceContact(collision.collider, collision.contacts[0].normal);
        }

        private void OnCollisionStay2D(Collision2D collision) {
            if (!IsSticky(collision.gameObject.layer)) return;
            if (collision.contacts.Length == 0) return;
            // Average all contact normals for stability
            Vector2 averageNormal = Vector2.zero;
            foreach (var contact in collision.contacts) {
                averageNormal += contact.normal;
            }
            averageNormal = (averageNormal / collision.contacts.Length).normalized;

            powerup?.OnSurfaceContact(collision.collider, averageNormal);
        }

        private void OnCollisionExit2D(Collision2D collision) {
            if (!IsSticky(collision.gameObject.layer)) return;

            if (showDebugLogs) {
                Debug.Log($"[StickyListener] CollisionExit: " +
                    $"{collision.gameObject.name}");
            }

            powerup?.OnSurfaceExit(collision.collider);
        }

        private bool IsSticky(int layer) {
            return (stickyLayers.value & (1 << layer)) != 0;
        }
    }
}
