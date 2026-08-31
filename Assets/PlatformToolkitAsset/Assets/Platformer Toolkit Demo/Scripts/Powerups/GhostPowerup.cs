// GhostPowerup.cs
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class GhostPowerup : MountPowerup {

        [Header("Ghost Settings")]
        [SerializeField] private List<string> layersToIgnore = new List<string>();
        // Layer names to ignore when ghosting
        // e.g. "Ground", "OneWayPlatform", "Wall"
        // Set in the Inspector

        [Header("Visuals")]
        [SerializeField] private float ghostAlpha = 0.5f;
        // Visual transparency while ghosting
        [SerializeField] private Color ghostColor = new Color(0.5f, 0.8f, 1f, 0.5f);
        [SerializeField] private AudioSource ghostLoopSound;

        private Collider2D mountCollider;
        private SpriteRenderer mountSprite;
        private Color originalColor;
        private bool isGhosting = false;
        private bool isExpiredStuck = false;
        private CharacterMount characterMount;

        // Store original collision state per ignored layer
        private List<int> ignoredLayerIndices = new List<int>();

        protected override void OnActivate() {
            characterMount = manager.GetComponent<CharacterMount>();
            if(characterMount == null) return;
            mountCollider = manager.GetComponent<Collider2D>();
            mountSprite = manager.GetComponentInChildren<SpriteRenderer>();

            if (mountSprite != null)
                originalColor = mountSprite.color;

            // Resolve layer names to indices
            ignoredLayerIndices.Clear();
            foreach (var layerName in layersToIgnore) {
                int layerIndex = LayerMask.NameToLayer(layerName);
                if (layerIndex == -1) {
                    Debug.LogWarning($"[GhostPowerup] Layer '{layerName}' not found.");
                    continue;
                }
                ignoredLayerIndices.Add(layerIndex);
            }

            StartGhosting();
        }

        protected override void OnDeactivate() {
            if (isExpiredStuck) return;
            // Only restore if not stuck — stuck mount keeps ignoring collisions
            StopGhosting();
        }

        protected override void OnTick(float deltaTime) {
            // Check mounted state each tick
            // Ghost only active when unmounted, but timer always ticks
            bool shouldBeGhosting = !IsMounted() && !isExpiredStuck;

            if (shouldBeGhosting && !isGhosting) {
                StartGhosting();
            } else if (!shouldBeGhosting && isGhosting) {
                PauseGhosting();
            }
        }

        // Called by base class when timer hits zero
        protected override void OnExpired() {
            // Mount stays stuck wherever it is, still ignoring collisions
            isExpiredStuck = true;
            isGhosting = false;

            // Keep visual feedback to show it's stuck
            if (mountSprite != null) {
                mountSprite.color = new Color(
                    ghostColor.r, ghostColor.g, ghostColor.b, 0.3f
                );
            }

            if (ghostLoopSound != null)
                ghostLoopSound.Stop();

            // Freeze the mount's rigidbody so it stops moving
            manager.MountBody.velocity = Vector2.zero;
            manager.MountBody.bodyType = RigidbodyType2D.Static;

            // Disable mount movement scripts
            manager.MountMovement.enabled = false;
            manager.MountJump.enabled = false;

            Debug.Log("[GhostPowerup] Timer expired while potentially inside wall. " +
                "Mount is now static. Use a portal to recall it.");
        }

        // ── Ghost State ───────────────────────────────────────────────────

        private void StartGhosting() {
            if (isGhosting) return;
            isGhosting = true;
            
            mountCollider.gameObject.layer = LayerMask.NameToLayer("Mount");

            SetLayerCollisions(false);

            if (mountSprite != null)
                mountSprite.color = ghostColor;

            if (ghostLoopSound != null)
                ghostLoopSound.Play();
        }

        private void PauseGhosting() {
            // Mounted — restore collision but keep visual hint
            isGhosting = false;

            SetLayerCollisions(true);

            // Slightly tinted to show powerup is still active
            if (mountSprite != null) {
                Color tinted = originalColor;
                tinted.a = 0.8f;
                mountSprite.color = tinted;
            }

            if (ghostLoopSound != null)
                ghostLoopSound.Pause();
        }

        private void StopGhosting() {
            isGhosting = false;
            SetLayerCollisions(true);
            
            mountCollider.gameObject.layer = LayerMask.NameToLayer("Player");

            if (mountSprite != null)
                mountSprite.color = originalColor;

            if (ghostLoopSound != null)
                ghostLoopSound.Stop();
        }

        private void SetLayerCollisions(bool enabled) {
            if (mountCollider == null) return;

            int mountLayer = manager.gameObject.layer;

            foreach (int layerIndex in ignoredLayerIndices) {
                Physics2D.IgnoreLayerCollision(
                    mountLayer,
                    layerIndex,
                    !enabled // true = ignore, false = restore
                );
            }
        }

        // ── Portal Recall ─────────────────────────────────────────────────

        public void TeleportToPortal(Vector3 portalPosition) {
            if (isExpiredStuck) {
                // Unstick the mount on recall
                manager.MountBody.bodyType = RigidbodyType2D.Dynamic;
                manager.MountMovement.enabled = true;
                manager.MountJump.enabled = true;
                isExpiredStuck = false;
            }

            manager.transform.position = portalPosition;

            // Re-enable ghosting if still unmounted
            if (!IsMounted()) {
                StartGhosting();
            }

            Debug.Log($"[GhostPowerup] Mount recalled to {portalPosition}");
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private bool IsMounted() {
            return characterMount != null && characterMount.IsMounted;
        }
    }
}
