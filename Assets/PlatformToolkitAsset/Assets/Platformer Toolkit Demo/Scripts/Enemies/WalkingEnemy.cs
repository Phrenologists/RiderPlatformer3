// WalkingEnemy.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class WalkingEnemy : StationaryEnemy {

        [Header("Walking Settings")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private bool fallsOffPlatforms = false;
        // When true, only turns on walls — falls off edges like a Goomba

        [Header("Detection")]
        [SerializeField] private float edgeDetectionDistance = 0.3f;
        // How far ahead to check for an edge
        [SerializeField] private float wallDetectionDistance = 0.3f;
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private LayerMask wallLayer;
        // Include enemy layers here too if you want enemies to block each other

        [Header("Raycast Offsets")]
        [SerializeField] private Vector2 edgeRaycastOffset = new Vector2(0.5f, 0f);
        // How far horizontally from center to cast the edge ray
        [SerializeField] private Vector2 wallRaycastOffset = new Vector2(0f, 0f);

        private float currentDirection = 1f; // 1 = right, -1 = left

        protected override void Awake() {
            base.Awake();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        private void FixedUpdate() {
            if (GetComponent<EnemyHealth>()?.IsDead == true) return;

            CheckForTurn();
            Move();
        }

        private void Move() {
            body.velocity = new Vector2(
                currentDirection * walkSpeed,
                body.velocity.y
            );

            // Flip sprite to match direction
            transform.localScale = new Vector3(
                currentDirection > 0 ? 1f : -1f, 1f, 1f
            );
        }

        private void CheckForTurn() {
            //Debug.Log(ShouldTurn());
            if (ShouldTurn()) {
                currentDirection *= -1f;
            }
        }

        private bool ShouldTurn() {
            // Always check for walls
            if (DetectsWall()) return true;
            //Debug.Log("Should turn");

            // Only check for edges if not the falling variant
            if (!fallsOffPlatforms && DetectsEdge()) return true;

            return false;
        }

        private bool DetectsEdge() {
            // Cast a ray downward from slightly ahead of the enemy
            Vector2 rayOrigin = (Vector2)transform.position
                + new Vector2(edgeRaycastOffset.x * currentDirection,
                    edgeRaycastOffset.y);

            RaycastHit2D hit = Physics2D.Raycast(
                rayOrigin,
                Vector2.down,
                edgeDetectionDistance + 0.1f,
                groundLayer
            );

            // If nothing below, we're at an edge
            return hit.collider == null;
        }

        private bool DetectsWall() {
            Vector2 rayOrigin = (Vector2)transform.position + wallRaycastOffset * currentDirection;

            RaycastHit2D hit = Physics2D.Raycast(
                rayOrigin,
                new Vector2(currentDirection, 0f),
                wallDetectionDistance,
                wallLayer
            );

            return hit.collider != null;
        }

        private void OnDrawGizmos() {
            // Edge detection ray
            if (!fallsOffPlatforms) {
                Gizmos.color = Color.yellow;
                Vector2 edgeOrigin = (Vector2)transform.position
                    + new Vector2(edgeRaycastOffset.x, edgeRaycastOffset.y);
                Gizmos.DrawLine(edgeOrigin, edgeOrigin + Vector2.down * (edgeDetectionDistance + 0.1f));
            }

            // Wall detection ray
            Gizmos.color = Color.red;
            Vector2 wallOrigin = (Vector2)transform.position + wallRaycastOffset;
            
            Gizmos.DrawLine(wallOrigin, new Vector3(wallDetectionDistance + wallOrigin.x, 0f + wallOrigin.y));
        }
    }
}
