// CameraZone.cs - revised with LockPosition
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class CameraZone : MonoBehaviour {

        public enum ZoneType {
            LockY,
            FreeFollowY,
            LockPosition,   // renamed from SetDeadZone
            ImmediateFollow
        }

        [Header("Zone Settings")]
        [SerializeField] private ZoneType zoneType;
        [SerializeField] private float transitionDuration = 0f;
        // 0 = use CameraController's default

        [Header("LockY Settings")]
        [SerializeField] private float lockedWorldY;

        [Header("LockPosition Settings")]
        [SerializeField] private Transform lockedPositionTarget;
        // The exact world XY the camera should pan to and hold
        [SerializeField] private float zoomSize = 0f;
        // 0 = no zoom change

        [Header("Exit Behaviour")]
        [SerializeField] private bool revertOnExit = true;
        [SerializeField] private ZoneType exitZoneType = ZoneType.FreeFollowY;
        [SerializeField] private float exitTransitionDuration = 0f;
        [SerializeField] private bool resetZoomOnExit = true;
        

        private void OnTriggerEnter2D(Collider2D other) {
            if (!IsPlayer(other)) return;
            ApplyZone(zoneType, transitionDuration);
        }

       // private void OnTriggerExit2D(Collider2D other) {
            //if (!IsPlayer(other)) return;
            //if (revertOnExit) {
                //ApplyZone(exitZoneType, exitTransitionDuration);
                //if (resetZoomOnExit && zoomSize > 0f) {
                    //float d = exitTransitionDuration <= 0f
                       // ? -1f : exitTransitionDuration;
                    //CameraController.Instance.ResetZoom(d);
               // }
            //}
        //}

        private void ApplyZone(ZoneType type, float duration) {
            float d = duration <= 0f ? -1f : duration;

            switch (type) {
                case ZoneType.LockY:
                    CameraController.Instance.SetLockedYPosition(lockedWorldY, d);
                    break;
                case ZoneType.FreeFollowY:
                    CameraController.Instance.SetFreeVerticalFollow(true);
                    break;
                case ZoneType.LockPosition:
                    if (lockedPositionTarget == null) {
                        Debug.LogWarning($"CameraZone on {gameObject.name}: " +
                                         "LockPosition zone has no target assigned.");
                        return;
                    }
                    CameraController.Instance.LockToPosition(
                        lockedPositionTarget, zoomSize, d
                    );
                    break;
                case ZoneType.ImmediateFollow:
                    CameraController.Instance.SetImmediateFollow();
                    break;
            }
        }

        private bool IsPlayer(Collider2D other) {
            return other.GetComponent<characterMovement>() != null;
        }

        private void OnDrawGizmos() {
            Gizmos.color = zoneType == ZoneType.LockPosition
                ? new Color(0.5f, 0f, 1f, 0.3f)    // purple for position lock
                : zoneType == ZoneType.LockY
                    ? new Color(1f, 0.5f, 0f, 0.3f) // orange for Y lock
                    : new Color(0f, 0.8f, 1f, 0.3f); // blue for others

            var col = GetComponent<Collider2D>();
            if (col != null) {
                Gizmos.DrawCube(transform.position, col.bounds.size);
            }

            // Draw a crosshair at the locked position
            if (zoneType == ZoneType.LockPosition && lockedPositionTarget != null) {
                Gizmos.color = new Color(0.5f, 0f, 1f, 0.9f);
                Vector3 t = lockedPositionTarget.position;
                // Crosshair at target
                Gizmos.DrawLine(new Vector3(t.x - 2f, t.y), new Vector3(t.x + 2f, t.y));
                Gizmos.DrawLine(new Vector3(t.x, t.y - 2f), new Vector3(t.x, t.y + 2f));
                // Line from zone to target so you can see the connection
                Gizmos.color = new Color(0.5f, 0f, 1f, 0.4f);
                Gizmos.DrawLine(transform.position, t);
            }

            if (zoneType == ZoneType.LockY) {
                Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
                Gizmos.DrawLine(
                    new Vector3(transform.position.x - 50f, lockedWorldY, 0f),
                    new Vector3(transform.position.x + 50f, lockedWorldY, 0f)
                );
            }
        }
    }
}
