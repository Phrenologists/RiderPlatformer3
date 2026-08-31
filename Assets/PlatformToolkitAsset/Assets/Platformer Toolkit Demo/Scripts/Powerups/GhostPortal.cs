// GhostPortal.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    // Place in the level alongside a switch
    // When activated, teleports the mount to this portal's position
    public class GhostPortal : MonoBehaviour {

        [Header("Visuals")]
        [SerializeField] private Animator animator;
        [SerializeField] private AudioSource activateSound;
        [SerializeField] private GameObject recallParticlePrefab;

        private static readonly int ActivatedHash =
            Animator.StringToHash("Activated");

        // Called by a Switch's onActivated UnityEvent
        public void RecallMount() {
            var ghost = PowerupManager.Instance?.ActivePowerup as GhostPowerup;
            if (ghost == null) 
            {
                Debug.Log("[GhostPortal] No active ghost powerup found.");
                return;
            }

            ghost.TeleportToPortal(transform.position);

            if (animator != null)
                animator.SetTrigger(ActivatedHash);

            if (activateSound != null)
                activateSound.Play();

            if (recallParticlePrefab != null)
                Instantiate(recallParticlePrefab,
                    transform.position, Quaternion.identity);
        }

        private void OnDrawGizmos() {
            Gizmos.color = new Color(0.5f, 0f, 1f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 0.5f);
            Gizmos.DrawSphere(transform.position, 0.15f);
        }
    }
}
