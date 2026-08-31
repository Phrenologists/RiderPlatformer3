// Projectile.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class Projectile : MonoBehaviour {

        [Header("Settings")]
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 3f;

        private Vector2 direction;
        private Rigidbody2D body;

        private void Awake() {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
        }

        public void Initialise(Vector2 shootDirection, Collider2D shooterCollider = null) {
            direction = shootDirection.normalized;
            body.velocity = direction * speed;

            // Ignore collision with the enemy that fired this projectile
            if (shooterCollider != null) {
                var ownCollider = GetComponent<Collider2D>();
                if (ownCollider != null) {
                    Debug.Log(shooterCollider);
                    Physics2D.IgnoreCollision(ownCollider, shooterCollider);
                }
            }

            Destroy(gameObject, lifetime);
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            // Damage player
            var hurt = collision.gameObject.GetComponent<characterHurt>();
            if (hurt != null) {
                hurt.TryHurt(DamageType.Projectile);
                Destroy(gameObject);
                return;
            }

            // Damage enemies
            var enemyHealth = collision.gameObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null && !enemyHealth.IsDead) {
                // Use slash damage type as specified
                enemyHealth.TakeDamage(AttackType.Slash, direction.x);

                // Also call OnSlashHit so guarding enemies can react
                var enemy = collision.gameObject.GetComponent<StationaryEnemy>();
                enemy?.OnSlashHit(direction.x);

                Destroy(gameObject);
                return;
            }

            // Hit terrain or any other object — destroy
            Destroy(gameObject);
        }
    }
}
