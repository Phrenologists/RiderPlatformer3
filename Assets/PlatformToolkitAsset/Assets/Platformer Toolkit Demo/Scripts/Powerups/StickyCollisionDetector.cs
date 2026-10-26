// StickyCollisionDetector.cs - now reports individual ray results
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class StickyCollisionDetector : MonoBehaviour {

        [Header("Detection Settings")]
        [SerializeField] public LayerMask stickyLayers;
        [SerializeField] public float detectionDistance = 0.3f;

        [Header("Mount Size")]
        [SerializeField] public float halfWidth = 1f;
        [SerializeField] public float halfHeight = 1f;

        // Individual ray results — publicly readable by StickyPowerup
        public RaycastHit2D RightHit  { get; private set; }
        public RaycastHit2D LeftHit   { get; private set; }
        public RaycastHit2D DownHit   { get; private set; }
        public RaycastHit2D UpHit     { get; private set; }
        
        public RaycastHit2D DiagonalHit { get; private set; }
        public bool DiagonalContact => DiagonalHit.collider != null;

        public bool FrontContact => RightHit.collider != null;
        public bool BackContact  => LeftHit.collider  != null;
        public bool DownContact  => DownHit.collider  != null;
        public bool UpContact    => UpHit.collider    != null;
        public bool AnyContact   => FrontContact || BackContact
                                 || DownContact  || UpContact;

        private bool raycasting = true;
        
        private bool rightPaused = false;
        private bool leftPaused  = false;
        private bool downPaused  = false;
        private bool upPaused    = false;
        
        [Header("Detection Distances")]
        [SerializeField] public float rightDistance = 0.8f;
        [SerializeField] public float leftDistance  = 0.8f;
        [SerializeField] public float downDistance  = 0.6f;
        [SerializeField] public float upDistance    = 0.6f;
        
        public void PauseRayFor(string ray, float duration) {
            StartCoroutine(PauseRayRoutine(ray, duration));
        }

        private IEnumerator PauseRayRoutine(string ray, float duration) {
            SetRayPaused(ray, true);
            yield return new WaitForSeconds(duration);
            SetRayPaused(ray, false);
        }

        private void SetRayPaused(string ray, bool paused) {
            switch (ray) {
                case "Right": rightPaused = paused; break;
                case "Left":  leftPaused  = paused; break;
                case "Down":  downPaused  = paused; break;
                case "Up":    upPaused    = paused; break;
            }
        }

        public void PauseRaycastingFor(float duration) {
            //Debug.Log("[PowerupManager] Pause Raycasting for " + duration);
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

            RightHit = rightPaused ? default : Physics2D.Raycast(
                center, Vector2.right, rightDistance, stickyLayers);
            LeftHit  = leftPaused  ? default : Physics2D.Raycast(
                center, Vector2.left,  leftDistance,  stickyLayers);
            DownHit  = downPaused  ? default : Physics2D.Raycast(
                center, Vector2.down,  downDistance,  stickyLayers);
            UpHit    = upPaused    ? default : Physics2D.Raycast(
                center, Vector2.up,    upDistance,    stickyLayers);
            
            Vector2 diagonalOrigin = center
                                     + Vector2.up    * halfHeight
                                     + Vector2.right * halfWidth;
            DiagonalHit = Physics2D.Raycast(
                diagonalOrigin,
                new Vector2(1f, 1f).normalized,
                detectionDistance,
                stickyLayers
            );
            
            Vector2 diagonalOriginLeft = center
                                         + Vector2.up   * halfHeight
                                         + Vector2.left * halfWidth;
            RaycastHit2D diagonalLeftHit = Physics2D.Raycast(
                diagonalOriginLeft,
                new Vector2(-1f, 1f).normalized,
                detectionDistance,
                stickyLayers
            );
            
            if (diagonalLeftHit.collider != null) DiagonalHit = diagonalLeftHit;
        }

        private void OnDrawGizmos() {
            Vector2 center = transform.position;

            DrawDetailedRay(center, Vector2.right, rightDistance,
                FrontContact, Color.red,    "R");
            DrawDetailedRay(center, Vector2.left,  leftDistance,
                BackContact,  Color.blue,   "L");
            DrawDetailedRay(center, Vector2.down,  downDistance,
                DownContact,  Color.green,  "D");
            DrawDetailedRay(center, Vector2.up,    upDistance,
                UpContact,    Color.yellow, "U");
        }

        private void DrawDetailedRay(Vector2 origin, Vector2 dir,
            float distance, bool hit, Color color, string label) {

            Gizmos.color = hit ? color : new Color(color.r, color.g, color.b, 0.2f);
            Gizmos.DrawLine(origin, origin + dir * distance);
            Gizmos.DrawSphere(origin + dir * distance, hit ? 0.08f : 0.04f);

            if (hit) {
                RaycastHit2D hitResult = GetHitForLabel(label);
                if (hitResult.collider != null) {
                    Gizmos.color = Color.white;
                    float crossSize = 0.1f;
                    Gizmos.DrawLine(
                        hitResult.point + Vector2.up    * crossSize,
                        hitResult.point + Vector2.down  * crossSize);
                    Gizmos.DrawLine(
                        hitResult.point + Vector2.left  * crossSize,
                        hitResult.point + Vector2.right * crossSize);
                }
            }
        }
        
        private void OnGUI() {
            if (!Application.isPlaying) return;
            if (Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z < 0) return;
            screenPos.y = Screen.height - screenPos.y;

            GUI.color = AnyContact ? Color.green : Color.red;
            GUI.Label(new Rect(screenPos.x + 20f, screenPos.y - 100f, 220f, 140f),
                $"=== STICKY RAYS ===\n" +
                $"Right: {(FrontContact ? "HIT" : "---")} " +
                $"{(rightPaused ? "[P]" : "")} " +
                $"{(RightHit.collider != null ? RightHit.distance.ToString("F2") : "")}\n" +
                $"Left:  {(BackContact  ? "HIT" : "---")} " +
                $"{(leftPaused  ? "[P]" : "")} " +
                $"{(LeftHit.collider  != null ? LeftHit.distance.ToString("F2")  : "")}\n" +
                $"Down:  {(DownContact  ? "HIT" : "---")} " +
                $"{(downPaused  ? "[P]" : "")} " +
                $"{(DownHit.collider  != null ? DownHit.distance.ToString("F2")  : "")}\n" +
                $"Up:    {(UpContact    ? "HIT" : "---")} " +
                $"{(upPaused    ? "[P]" : "")} " +
                $"{(UpHit.collider    != null ? UpHit.distance.ToString("F2")    : "")}\n" +
                $"Global: {(!raycasting ? "[PAUSED]" : "active")}"
            );
        }
        private RaycastHit2D GetHitForLabel(string label) {
            switch (label) {
                case "R": return RightHit;
                case "L": return LeftHit;
                case "D": return DownHit;
                case "U": return UpHit;
                default: return default;
            }
        }

        private void DrawRay(Vector2 origin, Vector2 dir,
            bool hit, Color color) {
            Gizmos.color = hit ? color : new Color(
                color.r, color.g, color.b, 0.2f);
            Gizmos.DrawLine(origin, origin + dir * detectionDistance);
        }
    }
}
