using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // Attach this to saw blades, spikes, or any always-on hazard
    public class EnvironmentHazard : MonoBehaviour {

        [SerializeField] private bool stopVelocityOnContact = false;
        // Spikes stop the player, saw blades don't

        private void OnCollisionEnter2D(Collision2D collision) {
            var hurt = collision.gameObject.GetComponent<characterHurt>();
            if (hurt == null) return;

            if (stopVelocityOnContact) {
                var body = collision.gameObject.GetComponent<Rigidbody2D>();
                if (body != null) body.velocity = Vector2.zero;
            }

            hurt.TryHurt(DamageType.Environment);
        }
    }
}
