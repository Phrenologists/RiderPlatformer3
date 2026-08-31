// MountPowerup.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // Base class all powerups inherit from
    // Attach subclasses to the mount GameObject
    public abstract class MountPowerup : MonoBehaviour {

        [Header("Powerup Info")]
        public string powerupName;
        public Sprite icon;

        [Header("Expiry")]
        public PowerupExpiry expiryType = PowerupExpiry.Timer;
        [SerializeField] protected float maxDuration = 10f;
        [SerializeField] protected float maxAmmo = 5f;

        // Current state — read by PowerupUI
        public float RemainingTime { get; protected set; }
        public float RemainingAmmo { get; protected set; }
        public bool IsExpired { get; private set; } = false;

        protected PowerupManager manager;
        
        protected virtual void OnExpired() { }

        // Called by PowerupManager when this powerup becomes active
        public void Activate(PowerupManager powerupManager) {
            manager = powerupManager;
            RemainingTime = maxDuration;
            RemainingAmmo = maxAmmo;
            IsExpired = false;
            OnActivate();
        }

        // Called by PowerupManager when replaced or expired
        public void Deactivate() {
            OnDeactivate();
            IsExpired = true;
        }

        // Called every frame by PowerupManager
        public void Tick(float deltaTime) {
            if (IsExpired) return;

            OnTick(deltaTime);

            if (expiryType == PowerupExpiry.Timer) {
                RemainingTime -= deltaTime;
                if (RemainingTime <= 0f) {
                    RemainingTime = 0f;
                    Expire();
                }
            }
        }

        // Called by PowerupManager when the powerup button is pressed
        public void UseButton() {
            if (IsExpired) return;
            OnButtonPressed();
        }

        protected void ConsumeAmmo(float amount = 1f) {
            if (expiryType != PowerupExpiry.Ammo) return;
            RemainingAmmo -= amount;
            if (RemainingAmmo <= 0f) {
                RemainingAmmo = 0f;
                Expire();
            }
        }

        protected void Expire() {
            if (IsExpired) return;
            OnExpired();
            manager?.OnPowerupExpired();
        }

        // ── Abstract / Virtual ────────────────────────────────────────────

        // Called when the powerup becomes active
        protected abstract void OnActivate();

        // Called when the powerup is removed
        protected abstract void OnDeactivate();

        // Called every frame while active
        protected virtual void OnTick(float deltaTime) { }

        // Called when the powerup button is pressed
        protected virtual void OnButtonPressed() { }

        // Returns display string for UI counter
        // Override for custom formatting
        public virtual string GetCounterDisplay() {
            switch (expiryType) {
                case PowerupExpiry.Timer:
                    return RemainingTime.ToString("F1") + "s";
                case PowerupExpiry.Ammo:
                    return Mathf.CeilToInt(RemainingAmmo).ToString();
                default:
                    return string.Empty;
            }
        }
    }
}
