// FlyingEnemy.cs
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class FlyingEnemy : StationaryEnemy {

        [Header("Flight Settings")]
        [SerializeField] private Transform topPoint;
        [SerializeField] private Transform bottomPoint;
        [SerializeField] private float flightSpeed = 2f;
        [SerializeField] private float pauseDuration = 0.8f;

        // How long the enemy hovers at top and bottom before reversing

        [SerializeField] private float approachSlowDownDistance = 0.5f;
        // Distance from waypoint at which the enemy starts slowing down

        [SerializeField] private Transform target;
        [SerializeField]private bool movingUp = true;
        [SerializeField]private bool isPaused = false;
        [SerializeField]private bool reachedTarget = false;

        protected override void Awake() {
            base.Awake();
            // Flying enemies use dynamic rigidbody so they can move
            // but we control movement manually, so freeze rotation
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void Start() {
            StartCoroutine(FlightRoutine());
        }

        private IEnumerator FlightRoutine() {
            while (true) {
                if (isPaused) {
                    yield return null;
                    continue;
                }

                target = movingUp ? topPoint : bottomPoint;
                float distanceToTarget = Mathf.Abs(
                    transform.position.y - target.position.y
                );

                // Slow down as we approach the waypoint
                float speedMultiplier = distanceToTarget < approachSlowDownDistance
                    ? distanceToTarget / approachSlowDownDistance
                    : 1f;

                float direction = movingUp ? 1f : -1f;
                body.velocity = new Vector2(
                    0f,
                    direction * flightSpeed * speedMultiplier
                );
                
                float desiredPosition;
                if(movingUp)
                    desiredPosition = target.position.y - 0.05f;
                else
                    desiredPosition = target.position.y + 0.05f;

                // Check if we've reached (or passed) the target
                reachedTarget = movingUp ? transform.position.y >= desiredPosition : transform.position.y <= desiredPosition;

                if (reachedTarget) {
                    // Snap to target and pause
                    transform.position = new Vector3(transform.position.x, target.position.y, transform.position.z);
                    Debug.Log("Reached target position");
                    body.velocity = Vector2.zero;

                    isPaused = true;
                    yield return new WaitForSeconds(pauseDuration);
                    isPaused = false;

                    movingUp = !movingUp;
                    
                    //target = movingUp ? topPoint : bottomPoint;
                }

                yield return null;
            }
        }

        // Flying enemies damage the player from any direction
        // so we override contact to skip directional checks
        protected override void OnPlayerContact(
            GameObject player, ContactDirection direction) {

            var hurt = player.GetComponent<characterHurt>();
            if (hurt != null) {
                hurt.TryHurt(DamageType.Enemy);
            }

            if (contactSound != null)
                contactSound.Play();

            // Still play a contact animation based on direction
            OnContactAnimation(direction);
        }

        private void OnDrawGizmos() {
            if (topPoint == null || bottomPoint == null) return;

            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawLine(topPoint.position, bottomPoint.position);
            Gizmos.DrawSphere(topPoint.position, 0.15f);
            Gizmos.DrawSphere(bottomPoint.position, 0.15f);
        }
    }
}
