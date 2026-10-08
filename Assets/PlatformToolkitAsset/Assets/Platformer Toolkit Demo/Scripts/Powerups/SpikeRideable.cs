// SpikeRideable.cs
// Added to the spike GameObject when extended horizontally
// Makes it act as a platform the player can walk on
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class SpikeRideable : MonoBehaviour {

        private List<Rigidbody2D> riders = new List<Rigidbody2D>();
        private Vector3 previousPosition;

        private void Awake() {
            previousPosition = transform.position;
        }

        private void FixedUpdate() {
            riders.RemoveAll(r => r == null);

            Vector3 delta = transform.position - previousPosition;
            previousPosition = transform.position;

            if (delta == Vector3.zero) return;

            foreach (var rider in riders) {
                if (rider != null)
                    rider.position += new Vector2(delta.x, delta.y);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            if (collision.contacts.Length > 0
                && collision.contacts[0].normal.y < -0.5f) {
                var rb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (rb != null && !riders.Contains(rb))
                    riders.Add(rb);
            }
        }

        private void OnCollisionExit2D(Collision2D collision) {
            var rb = collision.gameObject.GetComponent<Rigidbody2D>();
            if (rb != null) riders.Remove(rb);
        }
    }
}
