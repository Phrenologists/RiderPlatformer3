// PlatformPowerup.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class PlatformPowerup : MountPowerup {

        [Header("Platform Settings")]
        [SerializeField] private GameObject platformPrefab;
        // Should have a OneWayPlatform component and PlatformEffector2D
        [SerializeField] private float platformLifetime = 5f;
        [SerializeField] private Vector2 platformOffset = new Vector2(0f, -1f);
        // Offset from mount position where platform spawns
        // Negative Y places it just below the mount's feet

        [Header("Bounce Settings")]
        [SerializeField] private float bounceForce = 12f;
        // Upward force applied to mount and player after platform creation
        // Only applied when mounted

        [Header("Overlap Check")]
        [SerializeField] private float overlapCheckRadius = 0.5f;
        // Radius to check for existing platforms before placing
        [SerializeField] private LayerMask platformLayer;
        // Layer your platforms are on — used for overlap detection

        private CharacterMount characterMount;
        private Rigidbody2D mountBody;

        // Track all active platforms so we can destroy old ones
        // and check for stacking
        private List<GameObject> activePlatforms = new List<GameObject>();

        protected override void OnActivate() {
            characterMount = manager.GetComponent<CharacterMount>();
            mountBody = manager.MountBody;
        }

        protected override void OnDeactivate() {
            // Clean up any remaining platforms when powerup expires
            foreach (var platform in activePlatforms) {
                if (platform != null) Destroy(platform);
            }
            activePlatforms.Clear();
        }

        protected override void OnButtonPressed() {
            TryCreatePlatform();
        }

        // ── Platform Creation ─────────────────────────────────────────────

        private void TryCreatePlatform() {
            if (platformPrefab == null) return;

            Vector3 spawnPos = manager.transform.position
                + (Vector3)platformOffset;

            // Check for existing platform at this location
            // If found, destroy it and replace
            CheckAndDestroyOverlapping(spawnPos);

            // Create the platform
            var platform = Instantiate(
                platformPrefab,
                spawnPos,
                Quaternion.identity
            );
            activePlatforms.Add(platform);

            // Start the lifetime countdown
            manager.StartCoroutine(
                DestroyPlatformAfterDelay(platform, platformLifetime)
            );

            // Consume ammo only on successful placement
            ConsumeAmmo();

            // Only bounce if mounted
            if (characterMount != null && characterMount.IsMounted) {
                ApplyBounce();
            }
        }

        private void CheckAndDestroyOverlapping(Vector3 position) {
            // Clean up destroyed platforms first
            activePlatforms.RemoveAll(p => p == null);

            // Check if any active platform overlaps the spawn position
            foreach (var platform in activePlatforms) {
                if (platform == null) continue;

                float dist = Vector3.Distance(
                    platform.transform.position, position
                );

                if (dist < overlapCheckRadius) {
                    Destroy(platform);
                    activePlatforms.Remove(platform);
                    break;
                    // Only destroy one — there should only ever be one
                    // in this spot anyway
                }
            }
        }

        private IEnumerator DestroyPlatformAfterDelay(
            GameObject platform, float delay) {

            float elapsed = 0f;

            // Optionally fade the platform out in the last second
            // to warn the player it's disappearing
            var renderer = platform?.GetComponent<SpriteRenderer>();

            while (elapsed < delay) {
                elapsed += Time.deltaTime;

                if (platform == null) yield break;

                // Fade out in the last second
                if (renderer != null && elapsed > delay - 1f) {
                    float fadeProgress = (elapsed - (delay - 1f));
                    Color c = renderer.color;
                    c.a = Mathf.Lerp(1f, 0f, fadeProgress);
                    renderer.color = c;
                }

                yield return null;
            }

            if (platform != null) {
                activePlatforms.Remove(platform);
                Destroy(platform);
            }
        }

        // ── Bounce ────────────────────────────────────────────────────────

        private void ApplyBounce() {
            // Apply to mount
            mountBody.velocity = new Vector2(mountBody.velocity.x, 0f);
            mountBody.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

            // Apply to player if mounted
            // Find player via CharacterMount
            var playerBody = characterMount?.GetPlayerBody();
            if (playerBody != null) {
                playerBody.velocity = new Vector2(playerBody.velocity.x, 0f);
                playerBody.AddForce(
                    Vector2.up * bounceForce, ForceMode2D.Impulse
                );
            }
        }
    }
}
