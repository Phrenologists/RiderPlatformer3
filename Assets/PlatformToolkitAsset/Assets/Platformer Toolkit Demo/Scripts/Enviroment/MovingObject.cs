// MovingObject.cs
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class MovingObject : MonoBehaviour {

        public enum LoopMode {
            PingPong,   // reverses through waypoints
            Loop        // jumps back to first waypoint
        }

        public enum ObjectType {
            Platform,   // carries riders
            Hazard      // damages on contact
        }

        [Header("Setup")]
        [SerializeField] private ObjectType objectType = ObjectType.Platform;
        [SerializeField] private LoopMode loopMode = LoopMode.PingPong;

        [Header("Waypoints")]
        [SerializeField] private List<Transform> waypoints = new List<Transform>();
        [SerializeField] private int startingWaypointIndex = 0;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 3f;

        [Header("Hazard Settings")]
        [SerializeField] private LayerMask playerLayer;
        // Only used when objectType is Hazard

        // State
        private int currentWaypointIndex;
        private int direction = 1; // 1 = forward, -1 = reverse (ping-pong)
        private Vector3 previousPosition;

        // Rider tracking — only used for Platform type
        private List<Rigidbody2D> riders = new List<Rigidbody2D>();

        private void Start() {
            if (waypoints == null || waypoints.Count == 0) {
                Debug.LogWarning($"MovingObject on {gameObject.name} " +
                    "has no waypoints assigned.");
                enabled = false;
                return;
            }

            currentWaypointIndex = Mathf.Clamp(
                startingWaypointIndex, 0, waypoints.Count - 1
            );

            transform.position = waypoints[currentWaypointIndex].position;
            previousPosition = transform.position;
        }

        private void FixedUpdate() {
            previousPosition = transform.position;
            MoveTowardsWaypoint();

            if (objectType == ObjectType.Platform) {
                PushRiders();
            }
        }

        // ── Movement ──────────────────────────────────────────────────────

        private void MoveTowardsWaypoint() {
            if (waypoints.Count == 0) return;

            Transform target = waypoints[currentWaypointIndex];
            transform.position = Vector3.MoveTowards(
                transform.position,
                target.position,
                moveSpeed * Time.fixedDeltaTime
            );

            if (Vector3.Distance(transform.position, target.position) < 0.01f) {
                transform.position = target.position;
                AdvanceWaypoint();
            }
        }

        private void AdvanceWaypoint() {
            if (waypoints.Count == 1) return;

            switch (loopMode) {
                case LoopMode.PingPong:
                    // Reverse direction at endpoints
                    if (currentWaypointIndex == waypoints.Count - 1) {
                        direction = -1;
                    } else if (currentWaypointIndex == 0) {
                        direction = 1;
                    }
                    currentWaypointIndex += direction;
                    break;

                case LoopMode.Loop:
                    currentWaypointIndex =
                        (currentWaypointIndex + 1) % waypoints.Count;
                    break;
            }
        }

        // ── Platform Rider Logic ──────────────────────────────────────────

        private void PushRiders() {
            Vector3 delta = transform.position - previousPosition;
            if (delta == Vector3.zero) return;

            foreach (var rider in riders) {
                if (rider != null) {
                    rider.position += new Vector2(delta.x, delta.y);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (objectType == ObjectType.Platform) {
                // Only register characters landing on top
                if (collision.contacts.Length > 0
                    && collision.contacts[0].normal.y < -0.5f) {
                    var rb = collision.gameObject.GetComponent<Rigidbody2D>();
                    if (rb != null && !riders.Contains(rb)) {
                        riders.Add(rb);
                    }
                }
            } else {
                // Hazard — damage on any contact
                HandleHazardContact(collision.gameObject);
            }
        }

        private void OnCollisionExit2D(Collision2D collision) {
            if (objectType == ObjectType.Platform) {
                var rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (rb != null) {
                    riders.Remove(rb);
                }
            }
        }

        // ── Hazard Logic ──────────────────────────────────────────────────

        private void HandleHazardContact(GameObject other) {
            var hurt = other.GetComponent<characterHurt>();
            if (hurt != null) {
                hurt.TryHurt(DamageType.Environment);
            }
        }

        // ── Editor Helpers ────────────────────────────────────────────────

        private void OnDrawGizmos() {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = objectType == ObjectType.Hazard
                ? new Color(1f, 0.2f, 0.2f, 0.8f)
                : new Color(0.2f, 0.8f, 1f, 0.8f);

            for (int i = 0; i < waypoints.Count - 1; i++) {
                if (waypoints[i] == null || waypoints[i + 1] == null) continue;
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            }

            // Draw loop line back to start
            if (loopMode == LoopMode.Loop && waypoints.Count > 1) {
                if (waypoints[0] != null && waypoints[waypoints.Count - 1] != null) {
                    Gizmos.color = objectType == ObjectType.Hazard
                        ? new Color(1f, 0.2f, 0.2f, 0.3f)
                        : new Color(0.2f, 0.8f, 1f, 0.3f);
                    Gizmos.DrawLine(
                        waypoints[waypoints.Count - 1].position,
                        waypoints[0].position
                    );
                }
            }

            // Draw waypoint spheres with index labels
            Gizmos.color = objectType == ObjectType.Hazard
                ? new Color(1f, 0.2f, 0.2f, 1f)
                : new Color(0.2f, 0.8f, 1f, 1f);

            for (int i = 0; i < waypoints.Count; i++) {
                if (waypoints[i] == null) continue;
                Gizmos.DrawSphere(waypoints[i].position, 0.15f);
            }

            // Highlight starting waypoint in yellow
            if (startingWaypointIndex < waypoints.Count
                && waypoints[startingWaypointIndex] != null) {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(
                    waypoints[startingWaypointIndex].position, 0.2f
                );
            }
        }
    }
}
