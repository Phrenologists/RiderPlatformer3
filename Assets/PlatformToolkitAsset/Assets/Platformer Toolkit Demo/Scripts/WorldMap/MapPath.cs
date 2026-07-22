// MapPath.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class MapPath : MonoBehaviour {

        [Header("Path Points")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        // Place these child Transforms along the path in order from
        // start node to end node. Add as many as you need to get the
        // shape you want — the curve passes through each one.
        // Minimum 2 points (start and end), but more gives more control.

        [Header("Dot Settings")]
        [SerializeField] private GameObject dotPrefab;
        [SerializeField] private int dotCount = 20;
        [SerializeField] private float dotScale = 0.15f;

        [Header("Animation Settings")]
        [SerializeField] private float dotRevealInterval = 0.05f;

        private List<GameObject> dots = new List<GameObject>();
        private bool isRevealed = false;

        private void Awake() {
            GenerateDots();
            SetDotsVisible(false);
        }

        private void Start() {
            if (isRevealed) {
                SetDotsVisible(true);
            }
        }

        public void SetRevealed(bool revealed) {
            isRevealed = revealed;
        }

        public bool IsRevealed() => isRevealed;

        // ── Public path sampling ──────────────────────────────────────────

        // Returns a world position along the path at t (0-1)
        // Used by MapPlayerController to move the character along the path
        public Vector3 GetPositionAtT(float t) {
            return EvaluateCatmullRom(t);
        }

        // ── Reveal animation ──────────────────────────────────────────────

        public IEnumerator PlayRevealAnimation() {
            isRevealed = true;
            foreach (var dot in dots) {
                if (dot != null) {
                    dot.SetActive(true);
                    yield return new WaitForSeconds(dotRevealInterval);
                }
            }
        }

        // ── Dot generation ────────────────────────────────────────────────

        private void GenerateDots() {
            foreach (var dot in dots) {
                if (dot != null) DestroyImmediate(dot);
            }
            dots.Clear();

            if (waypoints == null || waypoints.Count < 2) return;

            for (int i = 0; i < dotCount; i++) {
                float t = i / (float)(dotCount - 1);
                Vector3 position = EvaluateCatmullRom(t);

                var dot = Instantiate(dotPrefab, position, Quaternion.identity, transform);
                dot.transform.localScale = Vector3.one * dotScale;
                dot.SetActive(false);
                dots.Add(dot);
            }
        }

        // ── Catmull-Rom spline ────────────────────────────────────────────

        private Vector3 EvaluateCatmullRom(float t) {
            if (waypoints.Count < 2) return Vector3.zero;
            if (waypoints.Count == 2) {
                // Fall back to a straight line if only two points
                return Vector3.Lerp(
                    waypoints[0].position,
                    waypoints[1].position,
                    t
                );
            }

            // Map t to a segment index
            // We have (waypoints.Count - 1) segments
            int segmentCount = waypoints.Count - 1;
            float scaledT = t * segmentCount;
            int segmentIndex = Mathf.Min(Mathf.FloorToInt(scaledT), segmentCount - 1);
            float segmentT = scaledT - segmentIndex;

            // Get the four points needed for Catmull-Rom
            // p1 and p2 are the segment endpoints
            // p0 and p3 are the points before and after, used to calculate tangents
            // We clamp to handle the endpoints where p0 or p3 don't exist
            Vector3 p0 = waypoints[Mathf.Max(segmentIndex - 1, 0)].position;
            Vector3 p1 = waypoints[segmentIndex].position;
            Vector3 p2 = waypoints[segmentIndex + 1].position;
            Vector3 p3 = waypoints[Mathf.Min(segmentIndex + 2, waypoints.Count - 1)].position;

            return CatmullRom(p0, p1, p2, p3, segmentT);
        }

        private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t) {
            // Standard Catmull-Rom formula
            return 0.5f * (
                (2f * p1)
                + (-p0 + p2) * t
                + (2f * p0 - 5f * p1 + 4f * p2 - p3) * (t * t)
                + (-p0 + 3f * p1 - 3f * p2 + p3) * (t * t * t)
            );
        }

        // ── Visibility ────────────────────────────────────────────────────

        private void SetDotsVisible(bool visible) {
            foreach (var dot in dots) {
                if (dot != null) dot.SetActive(visible);
            }
        }

        // ── Editor helpers ────────────────────────────────────────────────

        private void OnValidate() {
            if (waypoints != null && waypoints.Count >= 2 && dotPrefab != null) {
                GenerateDots();
            }
        }

        private void OnDrawGizmos() {
            if (waypoints == null || waypoints.Count < 2) return;

            // Draw the curve
            Gizmos.color = Color.yellow;
            Vector3 prev = EvaluateCatmullRom(0f);
            int steps = 40;
            for (int i = 1; i <= steps; i++) {
                float t = i / (float)steps;
                Vector3 next = EvaluateCatmullRom(t);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }

            // Draw waypoints
            Gizmos.color = Color.cyan;
            foreach (var wp in waypoints) {
                if (wp != null) {
                    Gizmos.DrawSphere(wp.position, 0.12f);
                }
            }

            // Draw lines between waypoints to make order clear
            Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
            for (int i = 0; i < waypoints.Count - 1; i++) {
                if (waypoints[i] != null && waypoints[i + 1] != null) {
                    Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                }
            }
        }
    }
}
