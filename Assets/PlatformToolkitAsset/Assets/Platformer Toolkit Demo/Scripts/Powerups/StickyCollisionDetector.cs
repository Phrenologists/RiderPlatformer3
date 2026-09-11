// StickyCollisionDetector.cs - now reports individual ray results
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class StickyCollisionDetector : MonoBehaviour {

        [Header("Detection Settings")]
        [SerializeField] public LayerMask stickyLayers;
        [SerializeField] public float detectionDistance = 0.3f;

        [Header("Mount Size")]
        [SerializeField] public float halfWidth = 0.5f;
        [SerializeField] public float halfHeight = 0.5f;

        // Individual ray results — publicly readable by StickyPowerup
        public RaycastHit2D RightHit  { get; private set; }
        public RaycastHit2D LeftHit   { get; private set; }
        public RaycastHit2D DownHit   { get; private set; }
        public RaycastHit2D UpHit     { get; private set; }

        public bool FrontContact => RightHit.collider != null;
        public bool BackContact  => LeftHit.collider  != null;
        public bool DownContact  => DownHit.collider  != null;
        public bool UpContact    => UpHit.collider    != null;
        public bool AnyContact   => FrontContact || BackContact
                                 || DownContact  || UpContact;

        private bool raycasting = true;

        public void PauseRaycastingFor(float duration) {
            StartCoroutine(PauseRoutine(duration));
        }

        private IEnumerator PauseRoutine(float duration) {
            raycasting = false;
            yield return new WaitForSeconds(duration);
            raycasting = true;
        }

        private void FixedUpdate() {
            if (!raycasting) {
                RightHit = default;
                LeftHit  = default;
                DownHit  = default;
                UpHit    = default;
                return;
            }

            Vector2 center = transform.position;

            RightHit = Physics2D.Raycast(
                center + Vector2.right * halfWidth,
                Vector2.right, detectionDistance, stickyLayers);
            LeftHit  = Physics2D.Raycast(
                center + Vector2.left  * halfWidth,
                Vector2.left,  detectionDistance, stickyLayers);
            DownHit  = Physics2D.Raycast(
                center + Vector2.down  * halfHeight,
                Vector2.down,  detectionDistance, stickyLayers);
            UpHit    = Physics2D.Raycast(
                center + Vector2.up    * halfHeight,
                Vector2.up,    detectionDistance, stickyLayers);
        }

        private void OnDrawGizmos() {
            Vector2 center = transform.position;

            DrawRay(center + Vector2.right * halfWidth,
                Vector2.right, FrontContact, Color.red);
            DrawRay(center + Vector2.left  * halfWidth,
                Vector2.left,  BackContact,  Color.blue);
            DrawRay(center + Vector2.down  * halfHeight,
                Vector2.down,  DownContact,  Color.green);
            DrawRay(center + Vector2.up    * halfHeight,
                Vector2.up,    UpContact,    Color.yellow);
        }

        private void DrawRay(Vector2 origin, Vector2 dir,
            bool hit, Color color) {
            Gizmos.color = hit ? color : new Color(
                color.r, color.g, color.b, 0.2f);
            Gizmos.DrawLine(origin, origin + dir * detectionDistance);
        }
    }
}
