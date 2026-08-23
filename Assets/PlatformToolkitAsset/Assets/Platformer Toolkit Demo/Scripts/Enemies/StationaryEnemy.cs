using UnityEngine;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class StationaryEnemy : MonoBehaviour {

        [Header("Components")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected GameObject defeatParticlePrefab;
        [SerializeField] protected AudioSource contactSound;
        [SerializeField] protected AudioSource defeatSound;

        [Header("Settings")]
        [SerializeField] private float knockbackForce = 12f;
        [SerializeField] private float knockbackUpForce = 5f;
        [SerializeField] private float destroyDelay = 0.5f;
        // How long after being knocked back before the GameObject is destroyed
        // Gives particles and animation time to play

        [Header("Layer Masks")]
        [SerializeField] private LayerMask playerLayer;

        protected Rigidbody2D body;
        protected Collider2D col;
        protected bool isDefeated = false;
        private EnemyHealth health;
        
        private static readonly int HitFromLeft = Animator.StringToHash("HitFromLeft");
        private static readonly int HitFromRight = Animator.StringToHash("HitFromRight");
        private static readonly int HitFromAbove = Animator.StringToHash("HitFromAbove");
        private static readonly int Defeated = Animator.StringToHash("Defeated");
        private static readonly int Hurt = Animator.StringToHash("Hurt");
        private static readonly int Idle = Animator.StringToHash("Idle");
        private static readonly int ResistanceBroken = Animator.StringToHash("ResistanceBroken");
        
        public virtual void OnSlashHit(float slashDirection) { }

        protected virtual void Awake() {
            body = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            health = GetComponent<EnemyHealth>();

            // Enemies start kinematic so they stay in place
            body.bodyType = RigidbodyType2D.Kinematic;
            
            if (animator != null)
                animator.Play(Idle);
        }

        private void OnCollisionEnter2D(Collision2D collision) {
            //if (isDefeated) return;
            if (health == null || health.IsDead) return;
            if (!IsPlayerLayer(collision.gameObject.layer)) return;

            // Determine contact direction from the collision normal
            Vector2 normal = collision.contacts[0].normal;
            ContactDirection direction = GetContactDirection(normal);

            OnPlayerContact(collision.gameObject, direction);
        }

        // Called when the player touches the enemy — override in subclasses
        // to add animation calls on top
        protected virtual void OnPlayerContact(GameObject player, ContactDirection direction) {
            var hurt = player.GetComponent<characterHurt>();
            Debug.Log(player.name + " hurt");
            if (hurt != null)
            {
                hurt.TryHurt(DamageType.Enemy);
            }

            if (contactSound != null)
                contactSound.Play();

            OnContactAnimation(direction);
        }
        
        public void OnHurt() {
            if (animator != null)
                animator.SetTrigger(Hurt);
        }

        // Called by EnemyHealth for slash death
        public void OnDeathAnimation() {
            if (animator != null)
                animator.SetTrigger(Defeated);
        }

        public virtual void OnResistanceChanged(ChargeResistance newResistance)
        {
            // Base implementation just triggers an animation
            // Subclasses can override for more complex behaviour
            if (animator != null && newResistance == ChargeResistance.None)
            {
                animator.SetTrigger(ResistanceBroken);
            }
        }

        // Override in subclasses to trigger specific animations

        // Called by CharacterMount when a charge hits this enemy
        public void Defeat(float chargeDirection) {
            if (isDefeated) return;
            isDefeated = true;

            // Stop the enemy colliding with the player so it can't deal damage
            // while flying away
            col.isTrigger = true;

            // Switch to dynamic so physics can move it
            body.bodyType = RigidbodyType2D.Dynamic;
            body.velocity = Vector2.zero;

            // Knock it in the charge direction with a slight upward arc
            body.AddForce(
                new Vector2(chargeDirection * knockbackForce, knockbackUpForce),
                ForceMode2D.Impulse
            );
            
            if (animator != null)
                animator.SetTrigger(Defeated);

            if (defeatSound != null)
                defeatSound.Play();

            OnDefeatAnimation();

            if (defeatSound != null)
                defeatSound.Play();

            if (defeatParticlePrefab != null)
                Instantiate(defeatParticlePrefab, transform.position, Quaternion.identity);

            // Destroy after a delay so the knockback arc is visible
            DOVirtual.DelayedCall(destroyDelay, () => {
                if (this != null) Destroy(gameObject);
            });
        }

        // Override in subclasses to trigger defeat animation
        protected virtual void OnDefeatAnimation() { }
        
        protected virtual void OnContactAnimation(ContactDirection direction) {
            if (animator == null) return;
            switch (direction) {
                case ContactDirection.FromLeft:
                    animator.SetTrigger(HitFromLeft); break;
                case ContactDirection.FromRight:
                    animator.SetTrigger(HitFromRight); break;
                case ContactDirection.FromAbove:
                    animator.SetTrigger(HitFromAbove); break;
            }
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

        private bool IsPlayerLayer(int layer) {
            return (playerLayer.value & (1 << layer)) != 0;
        }
    }

    public enum ContactDirection {
        FromLeft,
        FromRight,
        FromAbove
    }
}
