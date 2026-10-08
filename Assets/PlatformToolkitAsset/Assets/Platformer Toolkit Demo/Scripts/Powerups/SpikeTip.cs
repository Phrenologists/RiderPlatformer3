// SpikeTip.cs
// Attach to the spike prefab
// Detects when the spike tip enters an attachable surface
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class SpikeTip : MonoBehaviour {

        [SerializeField] private LayerMask attachableLayers;
        // Set from SpikePowerup after instantiation

        public LayerMask AttachableLayers {
            get => attachableLayers;
            set => attachableLayers = value;
        }

        // Called when spike tip enters a surface
        public System.Action<Collider2D, Vector2> OnSurfaceHit;
        // Called when spike tip exits a surface
        public System.Action OnSurfaceExit;

        private bool isAttached = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (isAttached) return;
            if (!IsAttachable(other.gameObject.layer)) return;

            isAttached = true;

            // Get contact point — use the collider bounds to find
            // the approximate hit position
            Vector2 hitPoint = other.ClosestPoint(transform.position);
            OnSurfaceHit?.Invoke(other, hitPoint);
        }

        private void OnTriggerExit2D(Collider2D other) {
            if (!IsAttachable(other.gameObject.layer)) return;
            isAttached = false;
            OnSurfaceExit?.Invoke();
        }

        private bool IsAttachable(int layer) {
            return (attachableLayers.value & (1 << layer)) != 0;
        }

        public void Reset() {
            isAttached = false;
        }
    }
}
