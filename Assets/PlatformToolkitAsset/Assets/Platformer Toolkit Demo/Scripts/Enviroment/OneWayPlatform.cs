// OneWayPlatform.cs
using System.Collections;
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class OneWayPlatform : MonoBehaviour {

        [SerializeField] private float dropThroughDuration = 0.3f;
        // How long the platform stays passable when dropping through

        private Collider2D platformCollider;

        private void Awake() {
            platformCollider = GetComponent<Collider2D>();
        }

        // Called by the player when holding down + jump
        public void DropThrough() {
            StartCoroutine(DisableTemporarily());
        }

        private IEnumerator DisableTemporarily() {
            platformCollider.enabled = false;
            yield return new WaitForSeconds(dropThroughDuration);
            platformCollider.enabled = true;
        }
    }
}
