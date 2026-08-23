using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GMTK.PlatformerToolkit
{
    public class Obstacle : MonoBehaviour
    {
        [Header("Layer Masks")] [SerializeField]
        private LayerMask playerLayer;
        
        [SerializeField] protected AudioSource contactSound;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            //if (isDefeated) return;
            if (!IsPlayerLayer(collision.gameObject.layer)) return;

            // Determine contact direction from the collision normal
            Vector2 normal = collision.contacts[0].normal;
            ContactDirection direction = GetContactDirection(normal);

            OnPlayerContact(collision.gameObject, direction);
        }
        protected virtual void OnPlayerContact(GameObject player, ContactDirection direction) {
            var hurt = player.GetComponent<characterHurt>();
            Debug.Log(player.name + " hurt");
            if (hurt != null)
            {
                hurt.TryHurt(DamageType.Enemy);
            }

            if (contactSound != null)
                contactSound.Play();

            //OnContactAnimation(direction);
        }
        
        protected ContactDirection GetContactDirection(Vector2 normal) {
            // The normal points FROM the enemy TO the player, so we invert it
            // to get the direction the player hit FROM
            Vector2 incoming = -normal;

            if (incoming.y < -0.5f)
                return ContactDirection.FromAbove;
            else if (incoming.x > 0.5f)
                return ContactDirection.FromRight;
            else
                return ContactDirection.FromLeft;
        }

        private bool IsPlayerLayer(int layer)
        {
            return (playerLayer.value & (1 << layer)) != 0;
        }
    }
}
