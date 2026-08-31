// ProjectilePowerup.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class ProjectilePowerup : MountPowerup {

        [Header("Projectile Settings")]
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform shootPoint;
        // Child transform on the mount — where projectiles spawn
        [SerializeField] private float projectileSpeed = 10f;
        [SerializeField] private AudioSource shootSound;

        [Header("Bounce Settings")]
        [SerializeField] private float playerBounceForce = 12f;
        // Force applied upward to the player when bouncing off a projectile

        protected override void OnActivate() { }

        protected override void OnDeactivate() { }

        protected override void OnButtonPressed() {
            Shoot();
        }

        private void Shoot() {
            if (projectilePrefab == null) return;

            // Shoot in the direction the mount is facing
            float facing = manager.MountBody.transform.localScale.x > 0 ? 1f : -1f;
            Vector2 direction = new Vector2(facing, 0f);

            Vector3 spawnPos = shootPoint != null
                ? shootPoint.position
                : manager.transform.position;

            var projectileObj = Instantiate(
                projectilePrefab, spawnPos, Quaternion.identity
            );

            var projectile = projectileObj.GetComponent<MountProjectile>();
            if (projectile != null) {
                projectile.Initialise(
                    direction,
                    GetComponent<Collider2D>(),
                    playerBounceForce
                );
            }

            if (shootSound != null) shootSound.Play();

            ConsumeAmmo();
        }
    }
}
