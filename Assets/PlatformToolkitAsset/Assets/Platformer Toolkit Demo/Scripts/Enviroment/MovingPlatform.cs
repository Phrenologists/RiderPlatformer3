using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class MovingPlatform : MonoBehaviour {

        [Header("Settings")]
        [SerializeField] private Vector3 moveOffset;
        [SerializeField] private float moveDuration = 0.5f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        // Tracks all riders currently on this platform
        private List<Rigidbody2D> riders = new List<Rigidbody2D>();
        private Vector3 previousPosition;

        [Header("Events")]
        public UnityEvent onArrived = new UnityEvent();

        private Vector3 closedPos;
        private Vector3 openPos;

        private void Awake() {
            closedPos = transform.position;
            openPos = closedPos + moveOffset;
            previousPosition = transform.position;
        }

        private void FixedUpdate() {
            // Calculate how far the platform moved this physics step
            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;

            // Push all riders by that same delta
            foreach (var rider in riders) {
                if (rider != null) {
                    rider.position += new Vector2(delta.x, delta.y);
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            // Only register characters landing on top
            if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f) {
                var rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (rb != null && !riders.Contains(rb)) {
                    riders.Add(rb);
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision) {
            var rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null) {
                riders.Remove(rb);
            }
        }

        public void Open() {
            transform.DOKill();
            transform.DOMove(openPos, moveDuration)
                .SetEase(moveEase)
                .SetUpdate(UpdateType.Fixed);
        }

        public void Close() {
            transform.DOKill();
            transform.DOMove(closedPos, moveDuration)
                .SetEase(moveEase)
                .SetUpdate(UpdateType.Fixed);
        }
    }
}
