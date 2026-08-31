// ProjectileEnemy.cs
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class ProjectileEnemy : StationaryEnemy {

        public enum AimMode {
            Straight,   // shoots in a fixed/flippable direction
            Tracing     // shoots at player's current position
        }

        [Header("Aim Settings")]
        [SerializeField] private AimMode aimMode = AimMode.Straight;

        [Header("Straight Shot Settings")]
        [SerializeField] private ShootingDirection shootingDirection
            = ShootingDirection.Right;
        [SerializeField] private FlipMode flipMode = FlipMode.None;

        [Header("Shooting")]
        [SerializeField] private float shootInterval = 2f;
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform shootPoint;
        // Empty child GameObject — where projectiles spawn from
        [SerializeField] private AudioSource shootSound;

        [Header("Detection")]
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float detectionAngle = 90f;
        // Total angle of the detection cone in degrees
        // 360 = omnidirectional, 90 = forward quarter-circle
        [SerializeField] private float baseDetectionAngle = 0f;
        // The center angle of the detection cone in degrees
        // 0 = right, 90 = up, 180 = left, 270 = down
        [SerializeField] private LayerMask obstacleLayer;
        // Layers that block line of sight

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        private Transform playerTransform;
        private bool canSeePlayer = false;

        // Animator hash for wind-up (future use)
        private static readonly int ShootTrigger = Animator.StringToHash("Shoot");

        protected override void Awake() {
            base.Awake();

            var player = GameObject.FindWithTag("Player");
            if (player != null) {
                playerTransform = player.transform;
            } else {
                Debug.LogWarning($"[ProjectileEnemy:{gameObject.name}] " +
                    "No GameObject tagged 'Player' found.");
            }
        }

        private void Start() {
            StartCoroutine(ShootRoutine());
        }

        private void Update() {
            if (playerTransform == null) return;
            canSeePlayer = CheckLineOfSight();
            UpdateFacingDirection();
        }

        // ── Line of Sight ─────────────────────────────────────────────────

        private bool CheckLineOfSight() {
            if (playerTransform == null) return false;

            Vector2 toPlayer = playerTransform.position - transform.position;
            float distance = toPlayer.magnitude;
            

            // Check range
            if (distance > detectionRange) return false;

            // Check angle
            float angleToPlayer = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
            float angleDifference = Mathf.DeltaAngle(baseDetectionAngle, angleToPlayer);

            if (Mathf.Abs(angleDifference) > detectionAngle * 0.5f) return false;

            // Check line of sight — raycast for obstacles
            RaycastHit2D hit = Physics2D.Raycast(transform.position, toPlayer.normalized, distance, obstacleLayer);

            // If nothing blocking, we can see the player
            return hit.collider != null;
        }

        // ── Facing Direction ──────────────────────────────────────────────

        private void UpdateFacingDirection() {
            if (playerTransform == null) return;

            Vector2 toPlayer = playerTransform.position - transform.position;

            switch (flipMode) {
                case FlipMode.None:
                    // Fixed direction — set scale based on shootingDirection
                    transform.localScale = GetScaleForDirection(shootingDirection);
                    break;

                case FlipMode.FlipX:
                    // Flip horizontally based on player X position
                    float scaleX = toPlayer.x >= 0 ? 1f : -1f;
                    transform.localScale = new Vector3(scaleX, 1f, 1f);
                    break;

                case FlipMode.FlipY:
                    // Flip vertically based on player Y position
                    float scaleY = toPlayer.y >= 0 ? 1f : -1f;
                    transform.localScale = new Vector3(1f, scaleY, 1f);
                    break;

                case FlipMode.FlipBoth:
                    float sx = toPlayer.x >= 0 ? 1f : -1f;
                    float sy = toPlayer.y >= 0 ? 1f : -1f;
                    transform.localScale = new Vector3(sx, sy, 1f);
                    break;
            }
        }

        private Vector3 GetScaleForDirection(ShootingDirection dir) {
            switch (dir) {
                case ShootingDirection.Left: return new Vector3(-1f, 1f, 1f);
                case ShootingDirection.Right: return new Vector3(1f, 1f, 1f);
                case ShootingDirection.Up: return new Vector3(1f, 1f, 1f);
                case ShootingDirection.Down: return new Vector3(1f, -1f, 1f);
                default: return Vector3.one;
            }
        }

        // ── Shooting ──────────────────────────────────────────────────────

        private IEnumerator ShootRoutine() {
            while (true) {
                yield return new WaitForSeconds(shootInterval);

                if (GetComponent<EnemyHealth>()?.IsDead == true) yield break;
                if (!canSeePlayer) continue;

                Shoot();
            }
        }

        private void Shoot() {
            if (projectilePrefab == null) return;

            Debug.Log("Shooting");

            Vector2 direction = GetShootDirection();
            Vector3 spawnPos = shootPoint != null
                ? shootPoint.position
                : transform.position;

            var projectileObj = Instantiate(
                projectilePrefab, spawnPos, Quaternion.identity
            );
            var projectile = projectileObj.GetComponent<Projectile>();
            var ownCollider = GetComponentInParent<Collider2D>();
            projectile?.Initialise(direction, ownCollider);

            if (animator != null)
                animator.SetTrigger(ShootTrigger);

            if (shootSound != null)
                shootSound.Play();
        }

        private Vector2 GetShootDirection() {
            if (aimMode == AimMode.Tracing && playerTransform != null) {
                return (playerTransform.position - transform.position).normalized;
            }

            // Straight mode — use shooting direction modified by flip
            ShootingDirection effectiveDirection = GetEffectiveDirection();
            return DirectionToVector(effectiveDirection);
        }

        private ShootingDirection GetEffectiveDirection() {
            if (playerTransform == null) return shootingDirection;

            Vector2 toPlayer = playerTransform.position - transform.position;

            switch (flipMode) {
                case FlipMode.FlipX:
                    return toPlayer.x >= 0
                        ? ShootingDirection.Right
                        : ShootingDirection.Left;

                case FlipMode.FlipY:
                    return toPlayer.y >= 0
                        ? ShootingDirection.Up
                        : ShootingDirection.Down;

                case FlipMode.FlipBoth:
                    // Use whichever axis has the greater difference
                    if (Mathf.Abs(toPlayer.x) >= Mathf.Abs(toPlayer.y)) {
                        return toPlayer.x >= 0
                            ? ShootingDirection.Right
                            : ShootingDirection.Left;
                    } else {
                        return toPlayer.y >= 0
                            ? ShootingDirection.Up
                            : ShootingDirection.Down;
                    }

                default:
                    return shootingDirection;
            }
        }

        private Vector2 DirectionToVector(ShootingDirection dir) {
            switch (dir) {
                case ShootingDirection.Left: return Vector2.left;
                case ShootingDirection.Right: return Vector2.right;
                case ShootingDirection.Up: return Vector2.up;
                case ShootingDirection.Down: return Vector2.down;
                default: return Vector2.right;
            }
        }

        // ── Debug Gizmos ──────────────────────────────────────────────────

        private void OnDrawGizmos() {
            if (!showDebugGizmos) return;

            // Detection range circle
            Gizmos.color = canSeePlayer
                ? new Color(1f, 0f, 0f, 0.15f)
                : new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Detection cone
            DrawDetectionCone();

            // Line to player when visible
            if (canSeePlayer && playerTransform != null) {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, playerTransform.position);
            }

            // Shoot point
            if (shootPoint != null) {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(shootPoint.position, 0.1f);
            }
        }

        private void DrawDetectionCone() {
            int segments = 20;
            float halfAngle = detectionAngle * 0.5f;

            Vector3 prevPoint = transform.position + (Vector3)AngleToDirection(
                baseDetectionAngle - halfAngle
            ) * detectionRange;

            Gizmos.color = new Color(1f, 0.8f, 0f, 0.6f);

            // Draw arc
            for (int i = 1; i <= segments; i++) {
                float t = i / (float)segments;
                float angle = (baseDetectionAngle - halfAngle)
                    + t * detectionAngle;
                Vector3 nextPoint = transform.position
                    + (Vector3)AngleToDirection(angle) * detectionRange;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }

            // Draw cone edges
            Vector3 leftEdge = transform.position
                + (Vector3)AngleToDirection(baseDetectionAngle - halfAngle)
                * detectionRange;
            Vector3 rightEdge = transform.position
                + (Vector3)AngleToDirection(baseDetectionAngle + halfAngle)
                * detectionRange;

            Gizmos.DrawLine(transform.position, leftEdge);
            Gizmos.DrawLine(transform.position, rightEdge);

            // Draw center direction
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(
                transform.position,
                transform.position + (Vector3)AngleToDirection(baseDetectionAngle)
                    * detectionRange
            );
        }

        private Vector2 AngleToDirection(float angleDegrees) {
            float rad = angleDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
        }
    }
}
