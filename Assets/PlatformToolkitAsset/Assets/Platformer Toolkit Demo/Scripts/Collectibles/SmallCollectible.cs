// SmallCollectible.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class SmallCollectible : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private AudioSource pickupSound;
        [SerializeField] private Animator animator;

        [Header("Settings")]
        [SerializeField] private CircleCollider2D pickupCollider;
        // Set the collider radius larger than the sprite in the Inspector

        private bool collected = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (collected) return;

            // Check if it's the player or the mount
            if (other.GetComponent<characterMovement>() == null) return;

            Collect();
        }

        private void Collect() {
            collected = true;
            
            int value = MultiplierManager.Instance != null
                ? MultiplierManager.Instance.GetCollectibleValue()
                : 1;


            GameManager.Instance.Session.SmallCollectiblesThisRun += value;

            // Tell the UI to update
            CollectibleUI.Instance.UpdateSmallCount(
                GameManager.Instance.Session.SmallCollectiblesThisRun
            );

            if (pickupSound != null) {
                // Detach sound so it finishes playing after the object is destroyed
                pickupSound.transform.SetParent(null);
                pickupSound.Play();
                Destroy(pickupSound.gameObject, pickupSound.clip.length);
            }

            Destroy(gameObject);
        }
    }
}
