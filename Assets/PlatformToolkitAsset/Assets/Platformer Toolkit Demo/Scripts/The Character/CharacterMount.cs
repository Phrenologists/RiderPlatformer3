using UnityEngine;
using UnityEngine.InputSystem;

namespace GMTK.PlatformerToolkit {

    public class CharacterMount : MonoBehaviour {

        [Header("Setup")]
        [SerializeField] private Transform saddlePoint;
        [SerializeField] private float mountRange = 1.5f;
        [SerializeField] private float dismountJumpForce = 8f;

        [Header("References")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private characterMovement mountMovement;
        [SerializeField] private characterJump mountJump;

        [Header("Mounted Dash")]
        [SerializeField] private float mountedWindupDuration = 0.2f;
        [SerializeField] private float mountedDashSpeed = 20f;
        [SerializeField] private float mountedDashDuration = 0.3f;
        [SerializeField] private float mountedDashCooldown = 0.5f;

        [Header("Unmounted Charge")]
        [SerializeField] private float unmountedChargeSpeed = 15f;

        // Cached player references
        private Transform player;
        private Rigidbody2D playerBody;
        private characterMovement playerMovement;
        private characterJump playerJump;
        private Collider2D playerCollider;

        // Mount references
        private Rigidbody2D mountBody;

        // State
        private bool isMounted = false;
        private enum ChargeState { Idle, Windup, Charging }
        private ChargeState chargeState = ChargeState.Idle;

        private float chargeDirection = 0f;   // -1 for left, 1 for right
        private float chargeTimer = 0f;
        private float cooldownTimer = 0f;
        private bool unmountedChargeLeft = false;
        private bool unmountedChargeRight = false;
        
        private bool isCharging = false;
        
        private characterHurt playerHurt;
        private characterHurt mountHurt;

        private void Start() {
            var playerObj = GameObject.FindWithTag("Player");
            player = playerObj.transform;
            playerBody = playerObj.GetComponent<Rigidbody2D>();
            playerMovement = playerObj.GetComponent<characterMovement>();
            playerJump = playerObj.GetComponent<characterJump>();
            playerCollider = playerObj.GetComponent<Collider2D>();
            mountBody = GetComponent<Rigidbody2D>();
            playerHurt = playerObj.GetComponent<characterHurt>();
            mountHurt = GetComponent<characterHurt>();
        }

        private void Update() {
            // Tick cooldown
            if (cooldownTimer > 0f)
                cooldownTimer -= Time.deltaTime;

            // Auto-remount: only while player is in a jump and close enough
            //if (!isMounted
                //&& playerJump.currentlyJumping
                //&& Vector2.Distance(transform.position, player.position) < mountRange) {
                //TryMount();
            //}
        }

        private void FixedUpdate() {
            switch (chargeState) {
                case ChargeState.Windup:
                    HandleWindup();
                    break;

                case ChargeState.Charging:
                if(isMounted)
                {
                    HandleCharging();
                }
                    break;

                case ChargeState.Idle:
                    if (!isMounted)
                        HandleUnmountedCharge();
                    break;
            }
        }

        // ─── Input Callbacks ────────────────────────────────────────────

        public void OnMount(InputAction.CallbackContext context) {
            if (!context.started) return;

            if (!isMounted) {
                TryMount();
            } else {
                Dismount();
            }
        }

        public void OnChargeLeft(InputAction.CallbackContext context) {
            if (isMounted) {
                // Mounted: one-shot dash on button press
                if (context.started)
                    TryBeginMountedDash(-1f);
            } else {
                // Unmounted: hold to charge the mount
                unmountedChargeLeft = context.started || context.performed;
                if (context.canceled)
                {
                    isCharging = false;
                    unmountedChargeLeft = false;
                    mountBody.gravityScale = mountJump.defaultGravityScale;
                }
            }
        }

        public void OnChargeRight(InputAction.CallbackContext context) {
            if (isMounted) {
                if (context.started)
                    TryBeginMountedDash(1f);
            } else {
                unmountedChargeRight = context.started || context.performed;
                if (context.canceled)
                {
                    isCharging = false;
                    unmountedChargeRight = false;
                    mountBody.gravityScale = mountJump.defaultGravityScale;
                }
            }
        }

        // ─── Mount / Dismount ────────────────────────────────────────────

        private void TryMount() {
            if (Vector2.Distance(transform.position, player.position) > mountRange)
                return;

            isMounted = true;

            var moveAction = playerInput.actions["Movement"];
            var jumpAction = playerInput.actions["Jump"];

            moveAction.performed -= playerMovement.OnMovement;
            moveAction.canceled -= playerMovement.OnMovement;
            jumpAction.started -= playerJump.OnJump;
            jumpAction.canceled -= playerJump.OnJump;

            moveAction.performed += mountMovement.OnMovement;
            moveAction.canceled += mountMovement.OnMovement;
            jumpAction.started += mountJump.OnJump;
            jumpAction.canceled += mountJump.OnJump;
            
            mountBody.velocity = new Vector2(playerBody.velocity.x, 0f);
            mountMovement.directionX = moveAction.ReadValue<Vector2>().x;

            playerMovement.directionX = 0f;
            playerBody.bodyType = RigidbodyType2D.Kinematic;
            playerBody.velocity = Vector2.zero;
            playerMovement.enabled = false;
            playerJump.enabled = false;
            playerCollider.isTrigger = true;
            

            player.SetParent(saddlePoint);
            player.localPosition = Vector3.zero;
            player.localRotation = Quaternion.identity;
        }

        private void Dismount() {
            // Don't allow dismounting mid-dash
            if (chargeState != ChargeState.Idle) return;

            isMounted = false;

            var moveAction = playerInput.actions["Movement"];
            var jumpAction = playerInput.actions["Jump"];

            moveAction.performed -= mountMovement.OnMovement;
            moveAction.canceled -= mountMovement.OnMovement;
            jumpAction.started -= mountJump.OnJump;
            jumpAction.canceled -= mountJump.OnJump;

            moveAction.performed += playerMovement.OnMovement;
            moveAction.canceled += playerMovement.OnMovement;
            jumpAction.started += playerJump.OnJump;
            jumpAction.canceled += playerJump.OnJump;

            mountMovement.directionX = 0f;

            player.SetParent(null);
            playerBody.bodyType = RigidbodyType2D.Dynamic;
            playerCollider.isTrigger = false;

            playerBody.velocity = new Vector2(mountBody.velocity.x, dismountJumpForce);
            playerMovement.directionX = moveAction.ReadValue<Vector2>().x;

            playerMovement.enabled = true;
            playerJump.enabled = true;
        }

        // ─── Mounted Dash ────────────────────────────────────────────────

        private void TryBeginMountedDash(float direction) {
            if (cooldownTimer > 0f) return;
            if (chargeState != ChargeState.Idle) return;

            chargeDirection = direction;
            chargeTimer = mountedWindupDuration;
            chargeState = ChargeState.Windup;

            // Stop the mount moving during windup
            mountMovement.enabled = false;
            mountBody.gravityScale = 0;
            mountBody.velocity = Vector2.zero;
            
        }

        private void HandleWindup() {
            chargeTimer -= Time.fixedDeltaTime;

            // Hold still during windup
            mountBody.velocity = Vector2.zero;
            
            mountBody.gravityScale = 0;

            if (chargeTimer <= 0f) {
                // Windup finished — begin the actual dash
                chargeTimer = mountedDashDuration;
                chargeState = ChargeState.Charging;
            }
        }

        private void HandleCharging() {
            chargeTimer -= Time.fixedDeltaTime;
            isCharging = true;

            // Drive the mount directly, bypassing characterMovement
            mountBody.velocity = new Vector2(chargeDirection * mountedDashSpeed, 0);
            mountHurt.isCharging = true;
            
            Debug.Log(mountHurt.isCharging);
            
            mountBody.gravityScale = 0;

            if (chargeTimer <= 0f) {
                EndMountedDash();
            }
        }

        private void EndMountedDash() {
            chargeState = ChargeState.Idle;
            cooldownTimer = mountedDashCooldown;
            mountMovement.enabled = true;
            mountBody.velocity = Vector2.zero;
            isCharging = false;
            mountHurt.isCharging = false;
            //mountBody.gravityScale = mountJump.defaultGravityScale;
        }

        // ─── Unmounted Charge ────────────────────────────────────────────

        private void HandleUnmountedCharge() {
            // LB and RB can cancel each other out if both held — last press wins
            // since they map to fixed directions this is intentional
            if (unmountedChargeLeft && !unmountedChargeRight) {
                mountMovement.enabled = false;
                mountBody.velocity = new Vector2(-unmountedChargeSpeed,0f);
                mountBody.gravityScale = 0;
                isCharging = true;
                //chargeState = ChargeState.Charging;
            } else if (unmountedChargeRight && !unmountedChargeLeft) {
                mountMovement.enabled = false;
                mountBody.velocity = new Vector2(unmountedChargeSpeed, 0f);
                mountBody.gravityScale = 0;
                isCharging = true;
                //chargeState = ChargeState.Charging;
            } else {
                // Neither or both held — hand control back to characterMovement
                mountMovement.enabled = true;
            }
            mountHurt.isCharging = (unmountedChargeLeft || unmountedChargeRight);
        }

        // ─── Destructible Objects ────────────────────────────────────────

        private void OnCollisionEnter2D(Collision2D collision) {
            if (isCharging == false) return;

            if (collision.gameObject.CompareTag("Destructible"))
            {
                var destructible = collision.gameObject.GetComponent<DestructibleObject>();
                if (destructible != null)
                {
                    destructible.Break();
                }
                else
                {
                    Destroy(collision.gameObject);
                }
                
                // Don't end the dash — let it continue through multiple objects
            } 
            else if (collision.gameObject.TryGetComponent<StationaryEnemy>(out var enemy))
            {
                enemy.Defeat(chargeDirection);
            }

            else {
                // Hit a solid wall mid-dash — stop the dash early
                //EndMountedDash();
            }
        }
    }
}
