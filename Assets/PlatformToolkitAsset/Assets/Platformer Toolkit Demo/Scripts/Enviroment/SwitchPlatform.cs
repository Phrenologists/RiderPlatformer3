// SwitchPlatform.cs
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class SwitchPlatform : MonoBehaviour {

        [Header("Movement")]
        [SerializeField] private Transform targetPosition;
        // Drag an empty GameObject to where the platform should move

        [SerializeField] private float moveDuration = 1f;
        [SerializeField] private Ease moveEase = Ease.OutCubic;
        // OutCubic gives a natural slow-start feel out of the box
        // Change per platform in the Inspector to suit the context

        [Header("Rider Settings")]
        [SerializeField] private bool carriersRiders = true;
        // Turn off for hazard-type switch platforms

        // State
        private Vector3 originPosition;
        private bool isMoving = false;
        private Tweener activeTween;

        // Rider tracking
        private List<Rigidbody2D> riders = new List<Rigidbody2D>();
        private Vector3 previousPosition;

        private void Awake() {
            originPosition = transform.position;
            previousPosition = transform.position;
        }

        private void FixedUpdate() {
            if (!carriersRiders) return;

            riders.RemoveAll(r => r == null);

            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;

            if (delta == Vector3.zero) return;

            foreach (var rider in riders) {
                if (rider != null) {
                    rider.position += new Vector2(delta.x, delta.y);
                }
            }
        }

        // ── Movement ──────────────────────────────────────────────────────

        public void MoveToTarget() {
            if (targetPosition == null) {
                Debug.LogWarning($"SwitchPlatform on {gameObject.name} " +
                    "has no target position assigned.");
                return;
            }
            MoveTo(targetPosition.position);
        }

        public void MoveToOrigin() {
            MoveTo(originPosition);
        }

        private void MoveTo(Vector3 destination) {
            activeTween?.Kill();
            activeTween = transform
                .DOMove(destination, moveDuration)
                .SetEase(moveEase)
                .SetUpdate(UpdateType.Fixed);
        }

        // ── Rider Tracking ────────────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision) {
            if (!carriersRiders) return;
            if (collision.contacts.Length > 0
                && collision.contacts[0].normal.y < -0.5f) {
                var rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (rb != null && !riders.Contains(rb))
                    riders.Add(rb);
            }
        }

        private void OnCollisionExit2D(Collision2D collision) {
            if (!carriersRiders) return;
            var rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null) riders.Remove(rb);
        }

        // ── Editor ────────────────────────────────────────────────────────

        private void OnDrawGizmos() {
            if (targetPosition == null) return;

            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.8f);
            Gizmos.DrawLine(transform.position, targetPosition.position);
            Gizmos.DrawSphere(targetPosition.position, 0.15f);

            // Draw a faded copy of the platform at the target position
            Gizmos.color = new Color(0.3f, 1f, 0.4f, 0.2f);
            Gizmos.DrawCube(targetPosition.position,
                GetComponent<Collider2D>()?.bounds.size ?? Vector3.one);
        }
    }
}
