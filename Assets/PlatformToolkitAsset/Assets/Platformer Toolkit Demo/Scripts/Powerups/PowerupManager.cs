// PowerupManager.cs
using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK.PlatformerToolkit {

    public class PowerupManager : MonoBehaviour {

        public static PowerupManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private characterMovement mountMovement;
        [SerializeField] private characterJump mountJump;
        [SerializeField] private Rigidbody2D mountBody;

        // Current active powerup — null if none
        public MountPowerup ActivePowerup { get; private set; }

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update() {
            if (ActivePowerup == null) return;
            ActivePowerup.Tick(Time.deltaTime);
        }

        // ── Input ─────────────────────────────────────────────────────────

        // Wire to PowerupButton input action in InputManager
        public void OnPowerupButton(InputAction.CallbackContext context) {
            if (!context.started) return;
            ActivePowerup?.UseButton();
        }

        // ── Powerup Lifecycle ─────────────────────────────────────────────

        public void CollectPowerup(MountPowerup newPowerup) {
            // Replace existing powerup if any
            if (ActivePowerup != null) {
                ActivePowerup.Deactivate();
                Destroy(ActivePowerup);
            }

            // Add the new powerup component to the mount
            // Powerups are MonoBehaviours so they live on the mount GameObject
            ActivePowerup = newPowerup;
            ActivePowerup.Activate(this);

            PowerupUI.Instance?.ShowPowerup(ActivePowerup);
        }

        public void OnPowerupExpired() {
            if (ActivePowerup == null) return;

            ActivePowerup.Deactivate();
            Destroy(ActivePowerup);
            ActivePowerup = null;

            PowerupUI.Instance?.HidePowerup();
        }
        
        public void OnJumpStateChanged(bool held) {
            if (ActivePowerup is FlightPowerup flightPowerup) {
                flightPowerup.OnJumpHeld(held);
            }
        }

        // ── Mount Stat Access ─────────────────────────────────────────────
        // Powerups use these to modify mount behaviour

        public characterMovement MountMovement => mountMovement;
        public characterJump MountJump => mountJump;
        public Rigidbody2D MountBody => mountBody;
    }
}
