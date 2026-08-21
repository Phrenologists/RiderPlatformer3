using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class CollectibleMultiplier : MonoBehaviour {

        [Header("Settings")]
        [SerializeField] private float duration = 10f;
        [SerializeField] private AudioSource pickupSound;
        [SerializeField] private Animator animator;

        private bool collected = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (collected) return;
            if (other.GetComponent<characterMovement>() == null) return;

            collected = true;

            MultiplierManager.Instance.ActivateMultiplier(duration);

            if (pickupSound != null) {
                pickupSound.transform.SetParent(null);
                pickupSound.Play();
                Destroy(pickupSound.gameObject, pickupSound.clip.length);
            }

            Destroy(gameObject);
        }
    }
}
