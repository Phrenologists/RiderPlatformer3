// FlightPowerup.cs - revised
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class FlightPowerup : MountPowerup {

        [Header("Flutter Settings")]
        [SerializeField] private float flutterForce = 15f;
        // Higher than before since we're fighting gravity directly
        [SerializeField] private float flutterMaxUpwardVelocity = 2f;
        // Cap so the mount doesn't rocket upward
        [SerializeField] private float flutterActivationDelay = 0.15f;
        // How long after leaving the ground before flutter can activate
        // Prevents immediate flutter on jump press

        private characterJump mountJumpScript;
        private Rigidbody2D body;

        private bool isFluttering = false;
        private bool jumpHeld = false;
        private bool wasOnGround = false;
        private float timeOffGround = 0f;
        private bool pastActivationDelay = false;

        protected override void OnActivate() {
            mountJumpScript = manager.MountJump;
            body = manager.MountBody;
        }

        protected override void OnDeactivate() {
            isFluttering = false;
            pastActivationDelay = false;
            timeOffGround = 0f;
        }

        protected override void OnTick(float deltaTime) {
            if (mountJumpScript == null || body == null) return;

            bool onGround = mountJumpScript.onGround;

            // Reset tracking when landing
            if (onGround) {
                timeOffGround = 0f;
                pastActivationDelay = false;
                isFluttering = false;
                wasOnGround = true;
                return;
            }

            // Count time off ground to enforce activation delay
            // Only start counting after we've been on the ground at least once
            if (!onGround && wasOnGround) {
                timeOffGround += deltaTime;
                if (timeOffGround >= flutterActivationDelay) {
                    pastActivationDelay = true;
                }
            }

            // Flutter is active when:
            // 1. Jump button is held
            // 2. We're in the air past the activation delay
            // 3. We're not still on the rising part of a jump
            //    (only flutter when falling or near apex)
            bool isFallingOrNearApex = body.velocity.y < 1f;

            isFluttering = jumpHeld
                && pastActivationDelay
                && isFallingOrNearApex
                && !onGround;

            if (isFluttering) {
                ApplyFlutter();
            }
        }

        private void ApplyFlutter() {
            // Only add upward force if below the velocity cap
            if (body.velocity.y < flutterMaxUpwardVelocity) {
                body.AddForce(
                    Vector2.up * flutterForce,
                    ForceMode2D.Force
                );
            }
        }

        public void OnJumpHeld(bool held) {
            jumpHeld = held;
        }
    }
}
