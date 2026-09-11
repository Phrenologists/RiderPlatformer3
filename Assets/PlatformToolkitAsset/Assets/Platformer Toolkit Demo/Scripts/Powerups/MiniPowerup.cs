// MiniPowerup.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK.PlatformerToolkit {

    public class MiniPowerup : MountPowerup {

        [Header("Size Settings")]
        [SerializeField] private float miniScale = 0.25f;
        // Target scale multiplier — 0.25 = quarter size

        [Header("Carry Settings")]
        [SerializeField] private Vector3 carryOffset = new Vector3(0f, 1f, 0f);
        // Position relative to player when being carried
        // (0, 1, 0) = sitting on player's head

        [SerializeField] private float throwForce = 15f;
        // Force applied when the player throws the mount

        private CharacterMount characterMount;
        private Rigidbody2D mountBody;
        private Collider2D mountCollider;
        private Transform mountTransform;
        private characterMovement mountMovement;
        private characterMovement characterMovement;

        private Transform playerTransform;
        private Rigidbody2D playerBody;
        private characterMovement playerMovement;

        // State
        private bool isMini = false;
        private bool isBeingCarried = false;
        private Vector3 originalScale;
        private Vector3 originalColliderSize;
        private Vector2 originalColliderOffset;

        // We store the box collider specifically since we need
        // to resize it along with the mount
        private BoxCollider2D boxCollider;

        protected override void OnActivate() {
            characterMount = manager.GetComponent<CharacterMount>();
            mountBody = manager.MountBody;
            mountCollider = manager.GetComponent<Collider2D>();
            mountTransform = manager.transform;
            boxCollider = manager.GetComponent<BoxCollider2D>();
            mountMovement = manager.GetComponent<characterMovement>();

            // Find player
            var playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null) {
                playerTransform = playerObj.transform;
                playerBody = playerObj.GetComponent<Rigidbody2D>();
                playerMovement = playerObj.GetComponent<characterMovement>();
            }

            // Store original values before shrinking
            originalScale = mountTransform.localScale;
            if (boxCollider != null) {
                originalColliderSize = boxCollider.size;
                originalColliderOffset = boxCollider.offset;
            }

            // Shrink immediately on collection
            Shrink();
        }

        protected override void OnDeactivate() {
            // If being carried when powerup expires, drop immediately
            if (isBeingCarried) {
                Drop(applyForce: false);
            }

            Grow();
        }

        // ── Size Change ───────────────────────────────────────────────────

        private void Shrink() {
            isMini = true;

            // Scale the transform
            mountTransform.localScale = originalScale * miniScale;
            mountMovement.currentSize = originalScale * miniScale;

            // Scale the collider to match
            if (boxCollider != null) {
                boxCollider.size = originalColliderSize * miniScale;
                boxCollider.offset = originalColliderOffset * miniScale;
            }
        }

        private void Grow() {
            isMini = false;

            // Restore original scale
            mountTransform.localScale = originalScale;
            mountMovement.currentSize = originalScale;
            
             if (boxCollider != null) {
                    float miniHeight = originalColliderSize.y * miniScale;
                    float grownHeight = originalColliderSize.y;
                    float heightDifference = grownHeight - miniHeight;
            
                    // Move mount up by half the difference so it grows upward
                    // rather than downward into the floor
                    mountTransform.position += Vector3.up * (heightDifference * 0.5f);
                }

            // Restore original collider
            if (boxCollider != null) {
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;
            }
        }

        // ── Carry System ──────────────────────────────────────────────────

        // Called by CharacterMount when the player presses the mount button
        // while the mount is mini and unmounted
        public void PickUp() {
            if (isBeingCarried || !isMini) return;
            if (characterMount.IsMounted) return;

            isBeingCarried = true;

            // Make mount kinematic so it doesn't fall while carried
            mountBody.bodyType = RigidbodyType2D.Kinematic;
            mountBody.velocity = Vector2.zero;

            // Parent to player at the carry offset
            mountTransform.SetParent(playerTransform);
            mountTransform.localPosition = carryOffset;
            mountTransform.localRotation = Quaternion.identity;

            // Disable mount's collider while being carried
            // so it doesn't interfere with player movement
            if (mountCollider != null)
                mountCollider.enabled = false;

            Debug.Log("[MiniPowerup] Mount picked up by player");
        }

        public void Throw() {
            if (!isBeingCarried) return;

            Drop(applyForce: true);
        }

        private void Drop(bool applyForce) {
            isBeingCarried = false;

            // Unparent
            mountTransform.SetParent(null);

            // Restore physics
            mountBody.bodyType = RigidbodyType2D.Dynamic;

            // Re-enable collider
            if (mountCollider != null)
                mountCollider.enabled = true;

            if (applyForce && playerMovement != null) {
                // Throw in the direction the player is facing
                float facing = playerTransform.localScale.x > 0 ? 1f : -1f;
                mountBody.velocity = new Vector2(
                    facing * throwForce,
                    throwForce * 0.3f // slight upward arc
                );
            }

            Debug.Log($"[MiniPowerup] Mount dropped. Force applied: {applyForce}");
        }

        // ── Tick ──────────────────────────────────────────────────────────

        protected override void OnTick(float deltaTime) {
            // Keep mount at carry offset while being carried
            // in case the player moves
            if (isBeingCarried) {
                mountTransform.localPosition = carryOffset;
            }
        }

        // ── Public State ──────────────────────────────────────────────────

        public bool IsBeingCarried => isBeingCarried;
        public bool IsMini => isMini;
    }
}
