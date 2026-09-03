using UnityEngine;



namespace GMTK.PlatformerToolkit {
    //This script is used by both movement and jump to detect when the character is touching the ground

    public class characterGround : MonoBehaviour {
        private bool onGround;

        [Header("Collider Settings")]
        [SerializeField][Tooltip("Length of the ground-checking collider")] public float groundLength = 0.95f;
        [SerializeField][Tooltip("Distance between the ground-checking colliders")] public Vector3 colliderOffset;

        [Header("Layer Masks")]
        [SerializeField][Tooltip("Which layers are read as the ground")] public LayerMask groundLayer;
        
        [HideInInspector] public Vector2 groundCheckDirection = Vector2.down;
        
        public void SetGroundCheckDirection(Vector2 direction) {
            groundCheckDirection = direction.normalized;
        }


        private void Update() {
            //Determine if the player is stood on objects on the ground layer, using a pair of raycasts
            onGround = Physics2D.Raycast(transform.position + colliderOffset, groundCheckDirection, groundLength, groundLayer) || Physics2D.Raycast(transform.position - colliderOffset, groundCheckDirection, groundLength, groundLayer);
        }

        private void OnDrawGizmos() {
            if (onGround) { Gizmos.color = Color.green; }
            else { Gizmos.color = Color.red; }
            Gizmos.DrawLine(
                transform.position + colliderOffset,
                transform.position + colliderOffset
                                   + (Vector3)groundCheckDirection * groundLength
            );
            Gizmos.DrawLine(
                transform.position - colliderOffset,
                transform.position - colliderOffset
                + (Vector3)groundCheckDirection * groundLength
            );
        }

        //Send ground detection to other scripts
        public bool GetOnGround() { return onGround; }
    }
}