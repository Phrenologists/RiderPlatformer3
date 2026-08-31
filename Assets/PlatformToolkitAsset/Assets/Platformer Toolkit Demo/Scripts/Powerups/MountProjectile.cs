// MountProjectile.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class MountProjectile : MonoBehaviour {

        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private float bounceSpeedRetention = 0.8f;
        // How much speed is kept after bouncing off a surface
        // 1 = full speed, 0.8 = 80% speed after each bounce

        private Vector2 direction;
        private Rigidbody2D body;
        private float playerBounceForce;

        private void Awake() {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
        }

        public void Initialise(Vector2 shootDirection,
            Collider2D shooterCollider,
            float bounceForce) {

            direction = shootDirection.normalized;
            body.velocity = direction * speed;
            playerBounceForce = bounceForce;

            if (shooterCollider != null) {
                var ownCollider = GetComponent<Collider2D>();
                if (ownCollider != null)
                    Physics2D.IgnoreCollision(ownCollider, shooterCollider);
            }

            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            // Bounce the player upward
            var hurt = collision.gameObject.GetComponent<characterHurt>();
            if (hurt == null) {
                // Check if it's the player character via movement script
                var movement = collision.gameObject.GetComponent<characterMovement>();
                if (movement != null) {
                    var playerBody = collision.gameObject
                        .GetComponent<Rigidbody2D>();
                    if (playerBody != null) {
                        playerBody.velocity = new Vector2(
                            playerBody.velocity.x,
                            playerBounceForce
                        );
                    }
                    // Don't destroy — projectile continues after player bounces
                    return;
                }
            }

            // Damage enemies
            var enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead) {
                enemyHealth.TakeDamage(AttackType.Slash, direction.x);
                Destroy(gameObject);
                return;
            }

            // Bounce off terrain
            if (collision.contacts.Length > 0) {
                Vector2 normal = collision.contacts[0].normal;
                direction = Vector2.Reflect(direction, normal);
                body.velocity = direction * speed * bounceSpeedRetention;
                // Don't destroy on terrain bounce
                return;
            }

            Destroy(gameObject);
        }
    }
}
