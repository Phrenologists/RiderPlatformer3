// SpikeContactDamage.cs
// Attached to the mount while spike powerup is active
// Makes the whole mount body damage enemies on contact
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class SpikeContactDamage : MonoBehaviour {

        private void OnCollisionEnter2D(Collision2D collision) {
            // Damage enemies the same way as a charge
            if (collision.gameObject.TryGetComponent<EnemyHealth>(
                    out var health)) {
                if (!health.IsDead) {
                    // Use the contact direction as charge direction
                    float dir = Mathf.Sign(
                        collision.transform.position.x
                        - transform.position.x
                    );
                    health.TakeDamage(AttackType.Charge, dir);
                }
            }
        }
    }
}
