// SwordSlash.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class SwordSlash : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private Collider2D slashHitbox;
        // A child GameObject with a trigger Collider2D — position and size
        // it in the inspector to match your sprite

        [Header("Slash Settings")]
        [SerializeField] private float slashDuration = 0.15f;
        // How long the hitbox is active
        [SerializeField] private float slashCooldown = 0.1f;
        [SerializeField] private float bounceForce = 15f;
        // Upward force applied when slashing down into a bounceable target

        [Header("Hitbox Offsets")]
        [SerializeField] private Vector2 forwardHitboxOffset = new Vector2(1f, 0f);
        [SerializeField] private Vector2 upHitboxOffset = new Vector2(0f, 1f);
        [SerializeField] private Vector2 downHitboxOffset = new Vector2(0f, -1f);
        [SerializeField] private Vector2 hitboxSize = new Vector2(1.5f, 0.8f);

        private characterMovement movement;
        private characterJump jump;
        private Rigidbody2D body;

        private bool isSlashing = false;
        private bool onCooldown = false;
        private float slashDirection = 0f;
        // -1 = left, 1 = right, used for damage source direction

        private void Awake() {
            movement = GetComponent<characterMovement>();
            jump = GetComponent<characterJump>();
            body = GetComponent<Rigidbody2D>();

            // Start with hitbox disabled
            if (slashHitbox != null)
                slashHitbox.enabled = false;
        }

        // ── Input ─────────────────────────────────────────────────────────

        // Wire to Slash input action in InputManager
        public void OnSlash(InputAction.CallbackContext context) {
            if (!context.started) return;
            if (isSlashing || onCooldown) return;
            if (!movementLimiter.instance.CharacterCanMove) return;

            StartCoroutine(PerformSlash());
        }

        // ── Slash Logic ───────────────────────────────────────────────────

        private IEnumerator PerformSlash() {
            isSlashing = true;

            // Determine slash direction from input
            float vertical = movement.directionY;
            Vector2 offset;
            bool isDownSlash = false;

            if (vertical > 0.2f) {
                // Upward slash
                offset = upHitboxOffset;
            } else if (vertical < -0.2f) {
                // Downward slash
                offset = downHitboxOffset;
                isDownSlash = true;
            } else {
                // Forward slash — use facing direction
                float facing = transform.localScale.x; //> 0 ? 1f : -1f;
                offset = new Vector2(forwardHitboxOffset.x, forwardHitboxOffset.y);
                slashDirection = facing;
            }

            // Position and enable the hitbox
            PositionHitbox(offset, isDownSlash);
            slashHitbox.enabled = true;

            // Play slash animation if animator exists
            // animator?.SetTrigger("Slash");

            // Wait for the active window
            yield return new WaitForSeconds(slashDuration);

            // Disable hitbox
            slashHitbox.enabled = false;
            isSlashing = false;

            // Brief cooldown before next slash
            onCooldown = true;
            yield return new WaitForSeconds(slashCooldown);
            onCooldown = false;
        }

        private void PositionHitbox(Vector2 offset, bool isDownSlash) {
            if (slashHitbox == null) return;

            slashHitbox.transform.localPosition = offset;

            // Rotate hitbox size for up/down slashes
            var box = slashHitbox as BoxCollider2D;
            if (box != null) {
                box.size = isDownSlash || offset == upHitboxOffset
                    ? new Vector2(hitboxSize.y, hitboxSize.x) // rotated
                    : hitboxSize;
            }
        }

        // ── Collision ─────────────────────────────────────────────────────

        private void OnTriggerEnter2D(Collider2D other) {
            if (!isSlashing) return;

            // Check for enemy
            var enemyHealth = other.GetComponent<EnemyHealth>();
            var enemy = other.GetComponent<StationaryEnemy>();
            enemy?.OnSlashHit(slashDirection);
            if (enemyHealth != null && !enemyHealth.IsDead) { enemyHealth.TakeDamage(AttackType.Slash, slashDirection);

                // Bounce if this was a down slash
                if (IsDownSlash()) {
                    ApplyBounce();
                }
                return;
            }

            // Check for bounceable object (down slash only)
            if (IsDownSlash() && other.CompareTag("Bounceable")) {
                ApplyBounce();
                return;
            }

            // Check for switch
            if (other.CompareTag("Switch")) {
                var sw = other.GetComponent<Switch>();
                sw?.Activate();
            }
        }

        private bool IsDownSlash() {
            return slashHitbox.transform.localPosition.y < -0.1f;
        }

        private void ApplyBounce() {
            // Cancel current Y velocity then apply bounce
            body.velocity = new Vector2(body.velocity.x, 0f);
            body.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
        }
    }
}
