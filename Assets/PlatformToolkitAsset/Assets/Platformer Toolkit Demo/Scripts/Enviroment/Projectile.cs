using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // Attach this to enemy projectiles
    public class Projectile : MonoBehaviour {

        private void OnCollisionEnter2D(Collision2D collision) {
            var hurt = collision.gameObject.GetComponent<characterHurt>();
            if (hurt == null) return;

            hurt.TryHurt(DamageType.Projectile);
            Destroy(gameObject);
        }
    }
}
