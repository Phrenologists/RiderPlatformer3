using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class DestructibleObject : MonoBehaviour {

        [Header("Effects")]
        [SerializeField] private GameObject breakParticlePrefab;
        [SerializeField] private AudioSource breakSound;

        [Header("Settings")]
        [SerializeField] private bool dropPickup = false;
        [SerializeField] private GameObject pickupPrefab;

        public void Break() {
            if (breakParticlePrefab != null)
                Instantiate(breakParticlePrefab, transform.position, Quaternion.identity);

            if (breakSound != null)
                breakSound.Play();

            if (dropPickup && pickupPrefab != null)
                Instantiate(pickupPrefab, transform.position, Quaternion.identity);

            Destroy(gameObject);
        }
    }
}
