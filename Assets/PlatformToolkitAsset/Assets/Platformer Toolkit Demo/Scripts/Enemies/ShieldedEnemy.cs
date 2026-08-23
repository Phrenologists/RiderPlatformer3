using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // This enemy starts with full charge resistance (blocks the mount).
    // A sword slash removes the armour and sets resistance to None,
    // making it vulnerable to the next charge.
    public class ShieldedEnemy : StationaryEnemy {

        [Header("Armour Settings")]
        [SerializeField] private GameObject armourVisual;
        // A child sprite or object that represents the armour — 
        // disable this when the armour breaks
        [SerializeField] private AudioSource armourBreakSound;
        [SerializeField] private GameObject armourBreakParticlePrefab;

        private EnemyHealth health;
        private bool isArmoured = true;

        protected override void Awake() {
            base.Awake();
            health = GetComponent<EnemyHealth>();

            // Make sure the EnemyHealth starts with Full resistance
            // This should also be set in the Inspector, but we enforce
            // it here as a safety net
            health.SetChargeResistance(ChargeResistance.Full);

            if (armourVisual != null)
                armourVisual.SetActive(true);
        }
        
        public override void OnSlashHit(float slashDirection) {
            if (!isArmoured) return;
            // Break armour without dealing HP damage
            health.SetChargeResistance(ChargeResistance.None);
        }

        // Called by EnemyHealth when resistance changes
        public override void OnResistanceChanged(ChargeResistance newResistance) {
            if (newResistance == ChargeResistance.None && isArmoured) {
                BreakArmour();
            }
        }
        

        // Called when the player slashes this enemy — breaks the armour
        // and makes it vulnerable to charge
        private void BreakArmour() {
            isArmoured = false;

            if (armourVisual != null)
                armourVisual.SetActive(false);

            if (armourBreakSound != null)
                armourBreakSound.Play();

            if (armourBreakParticlePrefab != null)
                Instantiate(armourBreakParticlePrefab,
                    transform.position, Quaternion.identity);

            if (animator != null)
                animator.SetTrigger("ShieldBroken");
        }
    }
}
