// PowerupPickup.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class PowerupPickup : MonoBehaviour {

        [Header("Powerup")]
        [SerializeField] private MountPowerup powerupPrefab;
        // Assign a prefab that has the specific powerup component on it

        [Header("Visuals")]
        [SerializeField] private AudioSource pickupSound;
        [SerializeField] private Animator animator;

        private bool collected = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (collected) return;

            // Only the mount can collect powerups
            var powerupManager = other.GetComponent<PowerupManager>();
            if (powerupManager == null) return;

            collected = true;

            // Instantiate the powerup and give it to the manager
            var powerup = Instantiate(powerupPrefab);
            powerup.gameObject.SetActive(false);
            powerup.transform.SetParent(other.transform);
            powerupManager.CollectPowerup(powerup);

            if (pickupSound != null) {
                pickupSound.transform.SetParent(null);
                pickupSound.Play();
                Destroy(pickupSound.gameObject, pickupSound.clip.length);
            }

            Destroy(gameObject);
        }
    }
}
