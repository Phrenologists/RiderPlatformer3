// BigCollectible.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class BigCollectible : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private AudioSource pickupSound;
        [SerializeField] private Animator animator;

        private bool collected = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (collected) return;
            if (other.GetComponent<characterMovement>() == null) return;

            Collect();
        }

        private void Collect() {
            collected = true;

            GameManager.Instance.Session.BigCollectiblesThisRun++;

            CollectibleUI.Instance.UpdateBigCount(
                GameManager.Instance.Session.BigCollectiblesThisRun,
                LevelManager.Instance.TotalBigCollectibles
            );

            if (pickupSound != null) {
                pickupSound.transform.SetParent(null);
                pickupSound.Play();
                Destroy(pickupSound.gameObject, pickupSound.clip.length);
            }

            // Big collectibles might have a collect animation before destroying
            if (animator != null) {
                animator.SetTrigger("Collected");
                // Destroy after animation — set this to match your animation length
                Destroy(gameObject, 0.5f);
            } else {
                Destroy(gameObject);
            }
        }
    }
}
