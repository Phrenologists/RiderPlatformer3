// EnemyHealth.cs
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    [RequireComponent(typeof(Collider2D))]
    public class EnemyHealth : MonoBehaviour {

        [Header("Stats")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private ChargeResistance chargeResistance = ChargeResistance.None;
        
        [Header("Vulnerabilities")]
        [SerializeField] private bool invulnerableToSlash = false;

        [Header("Death")]
        [SerializeField] private GameObject deathParticlePrefab;
        [SerializeField] private float deathAnimationDuration = 0.5f;
        [SerializeField] private AudioSource deathSound;

        private int currentHealth;
        private bool isDead = false;
        private Collider2D col;
        private StationaryEnemy enemy;

        private void Awake() {
            currentHealth = maxHealth;
            col = GetComponent<Collider2D>();
            enemy = GetComponent<StationaryEnemy>();
        }

        public ChargeResistance ChargeResistance => chargeResistance;
        public bool IsDead => isDead;

        // ── Damage ────────────────────────────────────────────────────────
        
        public void SetChargeResistance(ChargeResistance newResistance) {
            chargeResistance = newResistance;
            // Notify the enemy so it can update visuals
            enemy?.OnResistanceChanged(chargeResistance);
        }


        public void TakeDamage(AttackType attackType, float sourceDirection = 0f) {
            if (isDead) return;

            int damage = attackType == AttackType.Charge ? 3 : 1;
            
            if (attackType == AttackType.Slash) {
                // Even if invulnerable, we still return here without damage
                // The bounce is handled separately in SwordSlash — it checks
                // for the enemy's presence, not whether damage was dealt
                if (invulnerableToSlash) return;

                ApplyDamage(1, sourceDirection, sendFlying: false);

            }

            if (attackType == AttackType.Charge) {
                switch (chargeResistance) {
                    case ChargeResistance.None:
                        ApplyDamage(damage, sourceDirection, sendFlying: true);
                        break;
                    case ChargeResistance.Partial:
                        ApplyDamage(damage, sourceDirection, sendFlying: true);
                        break;
                    case ChargeResistance.Full:
                        // Damage blocked — caller handles mount bounce
                        return;
                }
            } else {
                // Slash — never sends flying
                ApplyDamage(damage, sourceDirection, sendFlying: false);
            }
        }

        private void ApplyDamage(int damage, float sourceDirection, bool sendFlying) {
            currentHealth -= damage;

            if (currentHealth <= 0) {
                Die(sendFlying, sourceDirection);
            } else {
                // Play hurt animation/feedback if not dead
                enemy?.OnHurt();
            }
        }

        private void Die(bool sendFlying, float sourceDirection) {
            if (isDead) return;
            isDead = true;

            // Remove hitbox immediately so player can pass through
            col.isTrigger = true;

            if (sendFlying) {
                enemy?.Defeat(sourceDirection);
            } else {
                StartCoroutine(DeathRoutine());
            }
        }

        private IEnumerator DeathRoutine() {
            // Play death animation if available
            enemy?.OnDeathAnimation();

            if (deathSound != null)
                deathSound.Play();

            yield return new WaitForSeconds(deathAnimationDuration);

            if (deathParticlePrefab != null)
                Instantiate(deathParticlePrefab, transform.position,
                    Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
