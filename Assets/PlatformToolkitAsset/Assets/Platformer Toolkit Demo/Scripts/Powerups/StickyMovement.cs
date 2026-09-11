// StickyMovement.cs
// Replaces characterMovement and characterJump while active
using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

namespace GMTK.PlatformerToolkit {

    public class StickyMovement : MonoBehaviour {

        [Header("Movement Stats")]
        [SerializeField] private float maxSpeed = 10f;
        [SerializeField] private float acceleration = 52f;
        [SerializeField] private float deceleration = 52f;
        // These mirror the values from characterMovement
        // They are set from StickyPowerup when activating
        // to match whatever preset is currently installed

        [Header("Jump Settings")]
        [SerializeField] private float jumpForce = 15f;
        // Applied away from the surface when jumping

        [Header("Surface Alignment")]
        [SerializeField] private float surfaceAlignSpeed = 20f;
        // How fast the mount rotates to match the surface
        // Instant at high values — keep high for snappy feel

        private Rigidbody2D body;
        private StickyCollisionDetector detector;
        private characterJuice juice;

        private bool isOnSurface = false;
        private Vector2 surfaceNormal = Vector2.up;
        private float currentSpeed = 0f;
        private characterMovement mountMovement;
        private characterJump mountJump;
        private characterGround mountGround;
        private characterJuice mountJuice;
        private Rigidbody2D mountBody;
        private SpriteRenderer mountSprite;
        private StickyPowerup powerupSource;
        
        [SerializeField] private float dropDownInputThreshold = -0.5f;
        
        [SerializeField] private PlayerInput playerInput;
        
        public float directionX;
        public float directionY { get; private set; }
        
        public void OnMovement(InputAction.CallbackContext context) {
            //This is called when you input a direction on a valid input type, such as arrow keys or analogue stick
            //The value will read -1 when pressing left, 0 when idle, and 1 when pressing right.

            if (movementLimiter.instance.CharacterCanMove) {
                Vector2 input = context.ReadValue<Vector2>();
                directionX = input.x;
                directionY = input.y;
            }
        }

        // Input — modify this section if controls feel wrong
        // ── INPUT HANDLING ──────────────────────────────────────────
        // When on a wall, we use the vertical axis for movement
        // When on a ceiling, we use the horizontal axis
        // The sign is determined by the surface normal direction
        // If controls feel reversed on a specific surface, negate
        // the relevant input read below
        private float GetMovementInput() {
            //var movement = GetComponent<characterMovement>();
            //if (movement == null) return 0f;

            Vector2 normal = surfaceNormal;

            // Determine which input axis to use based on surface
            // and which direction is "forward" along the surface

            // Surface tangent = perpendicular to normal
            // On ground (normal = up): tangent = right, use X input
            // On ceiling (normal = down): tangent = right, use X input
            //     but negate because we're upside down
            // On right wall (normal = left): tangent = up, use Y input
            // On left wall (normal = right): tangent = up, use Y input

            float dot = Vector2.Dot(normal, Vector2.up);

            if (Mathf.Abs(dot) > 0.7f) {
                // Floor or ceiling — use horizontal input
                // Negate on ceiling so left is still left
                float input = directionX;
                if (dot < 0) input = -input; // ceiling: flip horizontal
                return input;
            } else {
                // Wall — use vertical input
                // Negate on left wall so up is still up
                float input = directionY;
                if (normal.x > 0) input = -input; // left wall: flip vertical
                return input;
            }
        }
        // ── END INPUT HANDLING ───────────────────────────────────────

        private void Awake() {
            body = GetComponent<Rigidbody2D>();
            detector = GetComponent<StickyCollisionDetector>();
            juice = GetComponentInChildren<characterJuice>();
        }

        private void OnEnable() {
            if (detector != null) {
                //detector.OnContactChanged += HandleContactChanged;
            }

            // Zero gravity immediately when enabled
            body.gravityScale = 0f;
            //body.velocity = Vector2.zero;
        }

        private void OnDisable() {
            if (detector != null) {
               //detector.OnContactChanged -= HandleContactChanged;
            }
        }

        private void HandleContactChanged(bool hasContact, Vector2 normal) {
            isOnSurface = hasContact;
            if (hasContact) {
                surfaceNormal = normal;
                AlignToSurface(normal);
            }
        }

        private void FixedUpdate() {
            //if (!isOnSurface) {
                // In the air between surfaces — apply small pull toward
                // last known surface to help find the next one
                //body.velocity = Vector2.MoveTowards(
                    //body.velocity,
                    //Vector2.zero,
                    //deceleration * Time.fixedDeltaTime
                //);
                //Debug.Log("kill me");
                //return;
            //}

            // Apply gentle force toward surface to stay pressed against it
            //float gravityMagnitude = Physics2D.gravity.magnitude
                //* body.mass;
            //body.AddForce(
                //-surfaceNormal * gravityMagnitude,
                //ForceMode2D.Force
            //);

            // Cancel velocity component pointing away from surface
            float awaySpeed = Vector2.Dot(body.velocity, surfaceNormal);
            if (awaySpeed > 0) {
                body.velocity -= surfaceNormal * awaySpeed;
            }

            // Apply movement along surface tangent
            float input = GetMovementInput();
            Vector2 tangent = new Vector2(surfaceNormal.y, -surfaceNormal.x);

            // Match characterMovement acceleration pattern
            float targetSpeed = input * maxSpeed;
            float speedChange;

            if (Mathf.Abs(input) > 0.01f) {
                // Check if turning around
                float currentTangentSpeed = Vector2.Dot(body.velocity, tangent);
                if (Mathf.Sign(input) != Mathf.Sign(currentTangentSpeed)
                    && Mathf.Abs(currentTangentSpeed) > 0.1f) {
                    speedChange = acceleration * 2f * Time.fixedDeltaTime;
                } else {
                    speedChange = acceleration * Time.fixedDeltaTime;
                }
            } else {
                speedChange = deceleration * Time.fixedDeltaTime;
            }

            currentSpeed = Mathf.MoveTowards(
                Vector2.Dot(body.velocity, tangent),
                targetSpeed,
                speedChange
            );

            // Reconstruct velocity: tangent movement + surface press
            float intoSurface = Mathf.Min(
                Vector2.Dot(body.velocity, -surfaceNormal), 0
            );
            body.velocity = tangent * currentSpeed
                + (-surfaceNormal) * Mathf.Abs(intoSurface);
            //Debug.Log("Velocity Affected");
        }

        private void AlignToSurface(Vector2 normal) {
            // Calculate rotation angle from normal
            float angle = Mathf.Atan2(-normal.x, -normal.y) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Suppress juice tilt while sticky
            if (juice != null) juice.externalRotationControl = true;
        }

        // Called by StickyPowerup when jump is pressed
        public void Jump(InputAction.CallbackContext context) {
            //if (!isOnSurface) return;
            //This function is called when one of the jump buttons (like space or the A button) is pressed.
            mountJump.enabled = true;
            mountJump.useExternalJumpDirection = true;

            if (movementLimiter.instance.CharacterCanMove) {
                //When we press the jump button, tell the script that we desire a jump.
                //Also, use the started and canceled contexts to know if we're currently holding the button
                if (context.started) {
                    float verticalInput = directionY;
                    if (verticalInput < dropDownInputThreshold && isOnSurface) {
                        mountJump.enabled = true;
                        mountJump.TryDropThrough();
                        mountJump.enabled = false;
                        //Debug.Log("Passing through");
                        return; // Don't jump, drop instead
                    }
                    PowerupManager.Instance?.OnJumpStateChanged(true);
                    
                }

                if (context.canceled) {
                    PowerupManager.Instance?.OnJumpStateChanged(false);
                }
            }

            // Apply jump force away from the surface
            //body.velocity = surfaceNormal * jumpForce;
            //powerupSource.OnJumpPressed();

            isOnSurface = false;
        }

        // Mirror stats from the regular movement scripts
        // so the sticky movement feels consistent
        public void SyncStats(characterMovement movement, characterJump jump, CharacterMount characterMount, StickyPowerup stickyPowerup) {
            maxSpeed = movement.maxSpeed;
            acceleration = movement.maxAcceleration;
            deceleration = movement.maxDecceleration;
            playerInput = characterMount.playerInput;
            powerupSource = stickyPowerup;
            mountJump = jump;
            
            var moveAction = playerInput.actions["Movement"];
            var jumpAction = playerInput.actions["Jump"];
            
            //moveAction.performed -= mountMovement.OnMovement;
            //moveAction.canceled -= mountMovement.OnMovement;
            //jumpAction.started -= OnJump;
            //jumpAction.canceled -= OnJump;

            moveAction.performed += OnMovement;
            moveAction.canceled += OnMovement;
            jumpAction.started += Jump;
            jumpAction.canceled += Jump;
            
            jumpForce = Mathf.Sqrt(
                2f * Physics2D.gravity.magnitude * jump.jumpHeight
            );
        }
    }
}
