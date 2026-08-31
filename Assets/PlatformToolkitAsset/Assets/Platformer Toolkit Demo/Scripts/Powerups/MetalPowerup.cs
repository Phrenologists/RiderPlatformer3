// MetalPowerup.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class MetalPowerup : MountPowerup {

        [Header("Metal Settings")]
        [SerializeField] private float speedMultiplier = 0.5f;
        // Multiplied against maxSpeed and maxAirAcceleration
        // 0.5 = half speed
        [SerializeField] private float jumpHeightMultiplier = 0.6f;
        // Multiplied against jumpHeight
        // 0.6 = 60% of normal jump height
        [SerializeField] private float gravityMultiplier = 1.5f;
        // Makes the mount feel heavier on the way down

        private characterMovement mountMovementScript;
        private characterJump mountJumpScript;

        // Store original values to restore on deactivate
        private float originalMaxSpeed;
        private float originalMaxAcceleration;
        private float originalMaxDeceleration;
        private float originalMaxAirAcceleration;
        private float originalJumpHeight;
        private float originalDownwardMultiplier;

        protected override void OnActivate() {
            mountMovementScript = manager.MountMovement;
            mountJumpScript = manager.MountJump;

            // Store originals
            originalMaxSpeed = mountMovementScript.maxSpeed;
            originalMaxAcceleration = mountMovementScript.maxAcceleration;
            originalMaxDeceleration = mountMovementScript.maxDecceleration;
            originalMaxAirAcceleration = mountMovementScript.maxAirAcceleration;
            originalJumpHeight = mountJumpScript.jumpHeight;
            originalDownwardMultiplier = mountJumpScript.downwardMovementMultiplier;

            // Apply metal stats
            mountMovementScript.maxSpeed *= speedMultiplier;
            mountMovementScript.maxAcceleration *= speedMultiplier;
            mountMovementScript.maxDecceleration *= speedMultiplier;
            mountMovementScript.maxAirAcceleration *= speedMultiplier;
            mountJumpScript.jumpHeight *= jumpHeightMultiplier;
            mountJumpScript.downwardMovementMultiplier *= gravityMultiplier;
        }

        protected override void OnDeactivate() {
            if (mountMovementScript == null || mountJumpScript == null) return;

            // Restore original stats
            mountMovementScript.maxSpeed = originalMaxSpeed;
            mountMovementScript.maxAcceleration = originalMaxAcceleration;
            mountMovementScript.maxDecceleration = originalMaxDeceleration;
            mountMovementScript.maxAirAcceleration = originalMaxAirAcceleration;
            mountJumpScript.jumpHeight = originalJumpHeight;
            mountJumpScript.downwardMovementMultiplier = originalDownwardMultiplier;
        }

        // Metal mount is completely resistant to damage
        // Hook into characterHurt on the mount if it has one
        // For now, handled by checking active powerup type in characterHurt
    }
}
