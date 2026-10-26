// StickyPowerup.cs - with rotation, repositioning and active ray tracking
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit
{

    public class StickyPowerup : MountPowerup
    {

        [Header("Jump Settings")] [SerializeField]
        private float raycastPauseDuration = 1f;
        
        [SerializeField] private float otherRaycastPauseDuration = 0.2f;

        [SerializeField] private float unitsPerAmmo = 1f;

        [Header("Step Threshold")] [SerializeField]
        private float stepsBeforeRaycastActive = 2f;

        private StickyCollisionDetector detector;
        private characterJump mountJump;
        private characterGround mountGround;
        private Rigidbody2D mountBody;
        private Collider2D mountCollider;
        private characterJuice mountJuice;


        private bool stuckToSurface = false;
        private float currentRotation = 0f;

        private bool isInAir = false;
        
        private bool justJumped = false;

        [Header("Mount Dimensions")] [SerializeField]
        private float mountHalfWidth = 0.5f;

        [SerializeField] private float mountHalfHeight = 0.14f;

        private float distanceAccumulator = 0f;

        // Re-entry guard for StickAndRotate
        private bool isTransitioning = false;

        [Header("Surface Offsets")]
        [SerializeField] private float floorOffset = 0.5f;
        // Distance from mount center to floor surface
        [SerializeField] private float wallOffset = 0.5f;
        // Distance from mount center to wall surface  
        [SerializeField] private float ceilingOffset = 0.5f;
        // Distance from mount center to ceiling surface
        // These should match your mount's half extents
        // e.g. if mount is 1 unit tall, floorOffset = 0.5

        // Which ray caused the current rotation
        // None = not stuck, Right/Left/Down/Up = active surface
        private enum ActiveRay
        {
            None,
            Right,
            Left,
            Down,
            Up
        }

        private ActiveRay currentActiveRay = ActiveRay.None;

        //private ActiveRay lastSavedRay = ActiveRay.None;

        // Steps taken on current surface — used to decide when
        // the previous surface's ray becomes relevant again
        private float stepsOnCurrentSurface = 0f;
        private bool previousRayActive = false;
        // When false, the ray that WAS active before rotation is ignored

        private Vector2 lastPosition;

        private characterMovement mountMovement;

        private float rotationAngle = 0f;
        
        private bool shouldStickToSurface = false;

        [SerializeField] private float floorGap = 1f;
        [SerializeField] private float leftWallGap = 1f;
        [SerializeField] private float rightWallGap = 1f;
        [SerializeField] private float ceilingGap = 1f;

        private Vector2 jumpDir = Vector2.up;
        
        
        

        protected override void OnActivate()
        {
            mountJump = manager.MountJump;
            mountGround = manager.GetComponent<characterGround>();
            mountBody = manager.MountBody;
            mountCollider = manager.GetComponent<Collider2D>();
            mountMovement = manager.MountMovement;
            mountJuice = manager.GetComponentInChildren<characterJuice>();

            detector = manager.GetComponent<StickyCollisionDetector>();
            if (detector == null)
                detector = manager.gameObject
                    .AddComponent<StickyCollisionDetector>();

            detector.enabled = true;
            lastPosition = mountBody.position;
            currentRotation = 0f;
            mountHalfHeight = detector.halfHeight;
            mountHalfWidth = detector.halfWidth;

            isInAir = !mountGround.GetOnGround();

            //Debug.Log("[StickyPowerup] Activated");
        }

        protected override void OnDeactivate()
        {
            Unstick();

            if (detector != null)
                detector.enabled = false;

            // Restore rotation
            manager.transform.rotation = Quaternion.identity;

            Debug.Log("[StickyPowerup] Deactivated");
        }

        protected override void OnTick(float deltaTime)
        {
            if (detector == null || isTransitioning || justJumped) return;

            isInAir = !mountGround.GetOnGround() && !stuckToSurface;

            if (mountGround.GetOnGround() && currentRotation == 0)
            {
                jumpDir = Vector2.up;
            }

            // Track steps on current surface for previous ray cooldown
            if (stuckToSurface)
            {
                TrackDistance(deltaTime);
                float moved = Vector2.Distance(mountBody.position, lastPosition);
                stepsOnCurrentSurface += moved;
            }

            float dirX = mountMovement.directionX;
            float dirY = mountMovement.directionY;

            bool rightHit = detector.FrontContact;
            bool leftHit = detector.BackContact;
            bool downHit = detector.DownContact;
            bool upHit = detector.UpContact;

            int rot = Mathf.RoundToInt(currentRotation);

            lastPosition = mountBody.position;

            if (!rightHit && !leftHit && !downHit && !upHit)
            {
                //Debug.Log("No collisions");
                Unstick();
                return;
            }


            // Handle wall movement input
            if (stuckToSurface)
            {
                HandleSurfaceMovement();
            }

            // Check all four stick methods in order
            if (ShouldStickToFloor(rot, dirX, dirY, rightHit, leftHit, downHit, upHit))
            {
                mountJump.useExternalJumpDirection = false;
                jumpDir = Vector2.up;
                Debug.Log(downHit ? "Down" : "Jump");
                mountMovement.enabled = true;
                manager.StartCoroutine(StickAndRotate(0f));
                return;

            }

            if (ShouldStickToLeftWall(rot, dirX, dirY,
                    rightHit, leftHit, downHit, upHit))
            {
                mountJump.useExternalJumpDirection = true;
                jumpDir = Vector2.right;
                mountMovement.enabled = false;
                manager.StartCoroutine(StickAndRotate(270f));
                return;
            }

            if (ShouldStickToRightWall(rot, dirX, dirY,
                    rightHit, leftHit, downHit, upHit))
            {
                mountJump.useExternalJumpDirection = true;
                jumpDir = Vector2.left;
                mountMovement.enabled = false;
                manager.StartCoroutine(StickAndRotate(90f));
                return;
            }

            if (ShouldStickToCeiling(rot, dirX, dirY,
                    rightHit, leftHit, downHit, upHit))
            {
                mountJump.useExternalJumpDirection = true;
                jumpDir = Vector2.down;
                mountMovement.enabled = true;
                manager.StartCoroutine(StickAndRotate(180f));
                return;
            }
        }

        private bool ShouldStickToFloor(int rot, float dirX, float dirY, bool rightHit, bool leftHit, bool downHit, bool upHit)
        {
            //Debug.Log(downHit);

            // Already on floor
            if (rot == 0 && stuckToSurface) return false;

            // Mid-air and down ray hits
            if (isInAir && downHit) return true;

            // On right wall (90), moving down, down ray hits
            if (rot == 90 && dirY < 0 && downHit) return true;

            // On right wall (90), moving up, right ray loses contact
            //if (rot == 90 && dirY > 0 && !rightHit && stuckToSurface) return true;

            // On left wall (270), moving down, down ray hits
            if (rot == 270 && dirY < 0 && downHit) return true;

            // On left wall (270), moving up, left ray loses contact
            //if (rot == 270 && dirY > 0 && !leftHit && stuckToSurface) return true;
            
            if(rot == 0 && shouldStickToSurface) return true;

            return false;
        }

        private bool ShouldStickToLeftWall(int rot, float dirX, float dirY,
            bool rightHit, bool leftHit, bool downHit, bool upHit)
        {

            // Already on left wall
            if (rot == 270 && stuckToSurface) return false;

            // Mid-air and left ray hits
            if (isInAir && leftHit) return true;

            // On floor (0), moving left, left ray hits
            if (rot == 0 && dirX < 0 && leftHit) return true;

            // On floor (0), moving right, down ray loses contact
            //if (rot == 0 && dirX > 0 && !downHit && stuckToSurface) return true;

            // On ceiling (180), moving left, left ray hits
            if (rot == 180 && dirX < 0 && leftHit) return true;

            // On ceiling (180), moving right, down ray loses contact
            // (down ray in ceiling context = away from ceiling)
            //if (rot == 180 && dirX > 0 && !downHit && stuckToSurface) return true;
            
            if(rot == 270 && shouldStickToSurface) return true;
            
            if(rot == 0 && dirX > 0 && !downHit)
            {
                Debug.Log("[StickyPowerup] Turn to left");
                return true;
            }

            return false;
        }

        private bool ShouldStickToRightWall(int rot, float dirX, float dirY,
            bool rightHit, bool leftHit, bool downHit, bool upHit)
        {
            //Debug.Log(dirX);

            // Already on right wall
            if (rot == 90 && stuckToSurface) return false;

            // Mid-air and right ray hits
            if (isInAir && rightHit) return true;

            // On floor (0), moving right, right ray hits
            if (rot == 0 && dirX > 0 && rightHit) return true;

            // On floor (0), moving left, down ray loses contact
            //if (rot == 0 && dirX < 0 && !downHit && stuckToSurface) return true;

            // On ceiling (180), moving right, right ray hits
            if (rot == 180 && dirX > 0 && rightHit) return true;

            // On ceiling (180), moving left, down ray loses contact
            //if (rot == 180 && dirX < 0 && !downHit && stuckToSurface) return true;
            
            if(rot == 90 && shouldStickToSurface) return true;

            return false;
        }

        private bool ShouldStickToCeiling(int rot, float dirX, float dirY, bool rightHit, bool leftHit, bool downHit, bool upHit)
        {

            // Already on ceiling
            if (rot == 180 && stuckToSurface) return false;

            // Mid-air and up ray hits
            if (isInAir && upHit) return true;

            // On right wall (90), moving up, up ray hits
            if (rot == 90 && dirY > 0 && upHit) return true;

            // On right wall (90), moving down, right ray loses contact
            //if (rot == 90 && dirY < 0 && !rightHit && stuckToSurface) return true;

            // On left wall (270), moving up, up ray hits
            if (rot == 270 && dirY > 0 && upHit) return true;

            // On left wall (270), moving down, left ray loses contact
            //if (rot == 270 && dirY < 0 && !leftHit && stuckToSurface) return true;
            
            if(rot == 180 && shouldStickToSurface) return true;

            return false;
        }

        
        private void HandleSurfaceMovement()
        {
            switch (currentRotation)
            {
                case 270:
                case 90:
                    // On a wall — zero X input and use Y input for movement
                    // ── WALL MOVEMENT INPUT ──────────────────────────────
                    // directionX is zeroed so the mount doesn't drift
                    // off or into the wall via the normal movement system
                    // directionY drives vertical movement directly on the body
                    // If up/down feel reversed on a specific wall, negate
                    // the mountMovement.directionY read below
                    mountMovement.directionX = 0f;

                    // Use the same acceleration values as horizontal movement
                    // by replicating characterMovement's MoveTowards logic
                    // but applying it to the Y axis instead
                    float targetYSpeed = mountMovement.directionY
                                         * mountMovement.maxSpeed;

                    float currentYSpeed = mountBody.velocity.y;

                    float speedChange;
                    if (Mathf.Abs(mountMovement.directionY) > 0.01f)
                    {
                        // Accelerating or turning
                        if (Mathf.Sign(mountMovement.directionY)
                            != Mathf.Sign(currentYSpeed)
                            && Mathf.Abs(currentYSpeed) > 0.1f)
                        {
                            // Turning around on the wall
                            speedChange = mountMovement.maxTurnSpeed * Time.deltaTime;
                        }
                        else
                        {
                            speedChange = mountMovement.maxAcceleration * Time.deltaTime;
                        }
                    }
                    else
                    {
                        // No input — decelerate
                        speedChange = mountMovement.maxDecceleration * Time.deltaTime;
                    }

                    float newYSpeed = Mathf.MoveTowards(
                        currentYSpeed, targetYSpeed, speedChange
                    );

                    mountBody.velocity = new Vector2(0f, newYSpeed);
                    break;

                case 0:
                case 180:
                    // On ceiling or floor — normal X movement applies
                    // characterMovement handles this already, nothing to do
                    break;
            }
        }

        // ── Stick and Rotate ──────────────────────────────────────────────

        private IEnumerator StickAndRotate(float targetAngle)
        {
            // Prevent re-entry while rotating
            isTransitioning = true;
            stuckToSurface = true;

            // Disable collider briefly during rotation
            if (mountCollider != null)
                mountCollider.isTrigger = true;

            // Zero gravity and velocity
            mountBody.gravityScale = 0f;
            mountBody.velocity = Vector2.zero;

            // Disable jump
            if (mountJump.enabled)
                mountJump.enabled = false;


            if (mountJuice != null)
                mountJuice.externalRotationControl = true;
            // Apply rotation
            currentRotation = targetAngle;
            manager.transform.rotation =
                Quaternion.Euler(0f, 0f, targetAngle);

            yield return new WaitForFixedUpdate();

            RepositionOnSurface(targetAngle);

            yield return new WaitForFixedUpdate();

            if (mountCollider != null)
                mountCollider.isTrigger = false;

            isTransitioning = false;
        }

        private void RepositionOnSurface(float angle)
        {

            Vector3 pos = manager.transform.position;
            int rot = Mathf.RoundToInt(angle);

            switch (rot)
            {
                case 0: // Floor
                    if (detector.DownHit.collider != null)
                    {
                        pos.y = detector.DownHit.point.y + floorOffset;
                    }

                    break;

                case 90: // Right wall
                    if (detector.RightHit.collider != null)
                    {
                        pos.x = detector.RightHit.point.x - wallOffset;
                        // Also align Y to where the ray hit
                        // so the mount isn't floating above or below
                        // the wall contact point after rotation
                        pos.y = detector.RightHit.point.y;
                    }

                    break;

                case 270: // Left wall
                    if (detector.LeftHit.collider != null)
                    {
                        pos.x = detector.LeftHit.point.x + wallOffset;
                        pos.y = detector.LeftHit.point.y;

                    }

                    break;

                case 180: // Ceiling
                    if (detector.UpHit.collider != null)
                    {
                        pos.y = detector.UpHit.point.y - ceilingOffset;
                    }

                    break;
            }

            manager.transform.position = pos;
        }

        private void Unstick()
        {

            mountMovement.enabled = true;
 

            if (stuckToSurface)
            {
                //CheckForNewRotation(Mathf.RoundToInt(currentRotation), mountMovement.directionX, mountMovement.directionY);
            }
            if (!stuckToSurface && currentRotation == 0f) return;
            stuckToSurface = false;
            currentRotation = 0f;
            isTransitioning = false;
            //previousActiveRay = currentActiveRay;

            // Restore directionX if we were on a wall
            // so the mount doesn't stay frozen horizontally
            //if (currentActiveRay == ActiveRay.Right
            //|| currentActiveRay == ActiveRay.Left) {
            // Don't manually set directionX back — the input system
            // will naturally update it on the next movement input
            // Just zero velocity so there's no leftover Y speed
            //mountBody.velocity = Vector2.zero;
            //}

            currentActiveRay = ActiveRay.None;
            stepsOnCurrentSurface = 0f;
            previousRayActive = false;

            mountBody.gravityScale = 1f;

            if (!mountJump.enabled)
                mountJump.enabled = true;

            if (mountJuice != null) mountJuice.externalRotationControl = false;

            manager.transform.rotation = Quaternion.identity;

            //Debug.Log("[StickyPowerup] Unstuck");
            //UpdateDetectorDimensions();
        }

        

        // ── Jump ──────────────────────────────────────────────────────────

        public void OnJumpPressed()
        {
            //if (!stuckToSurface) return;

            //Debug.Log("[StickyPowerup] Jump pressed");

            //jumpDir = GetJumpDirection();

            // Unstick first — restores gravity and jump script
            Unstick();
            
            justJumped = true;

            mountJump.useExternalJumpDirection = true;
            mountJump.externalJumpDirection = jumpDir;

            //detector.PauseRaycastingFor(raycastPauseDuration);

            // Restore rotation before jump
            manager.transform.rotation = Quaternion.identity;

            // Pause raycasting so mount clears the surface
            //detector.PauseRaycastingFor(raycastPauseDuration);

            manager.StartCoroutine(FireJump());
        }

        private Vector2 GetJumpDirection()
        {
            int rot = Mathf.RoundToInt(currentRotation);
            Debug.Log("Getting current rotation");
            switch (rot)
            {
                case 90: return Vector2.left;
                case 270: return Vector2.right;
                case 180: return Vector2.down;
                default: return Vector2.up;
            }
        }

        private IEnumerator FireJump()
        {
            yield return null;
            mountJump.forceJumpNextFrame = true;
            mountJump.TriggerJump();
            
            string surfaceRay = GetSurfaceRayName();
            
            detector.PauseRaycastingFor(raycastPauseDuration);
            
             foreach (string ray in GetOtherRayNames(surfaceRay)) 
             {
                    detector.PauseRayFor(ray, otherRaycastPauseDuration);
             }
            yield return null;

            Debug.Log($"[FireJump] Clearing external direction. " +
            $"useExternal was: {mountJump.useExternalJumpDirection}");

            mountJump.useExternalJumpDirection = false;
            mountJump.externalJumpDirection = Vector2.up;
            
            yield return new WaitForSeconds(raycastPauseDuration);
            justJumped = false;
            Debug.Log("[FireJump] justJumped cleared");
        }
        
        private string GetSurfaceRayName() {
            int rot = Mathf.RoundToInt(currentRotation);
            switch (rot) {
                case 90:  return "Right";
                case 270: return "Left";
                case 180: return "Up";
                default:  return "Down";
            }
        }
        private string[] GetOtherRayNames(string excluded) {
            var all = new[] { "Right", "Left", "Down", "Up" };
            var others = new System.Collections.Generic.List<string>();
            foreach (var ray in all) {
                if (ray != excluded) others.Add(ray);
            }
            return others.ToArray();
        }

        public void OnMountHit()
        {
            if (!stuckToSurface) return;
            Unstick();
            // Apply bounce away from surface
            Vector2 bounceDir = GetJumpDirection();
            mountBody.AddForce(bounceDir * 10f, ForceMode2D.Impulse);
            Debug.Log("[StickyPowerup] Mount hit while stuck - bounced off");
        }

        private void TrackDistance(float deltaTime)
        {
            Vector2 currentPos = mountBody.position;
            float dist = Vector2.Distance(currentPos, lastPosition);
            lastPosition = currentPos;

            if (dist > 0.001f)
            {
                distanceAccumulator += dist;
                while (distanceAccumulator >= unitsPerAmmo)
                {
                    distanceAccumulator -= unitsPerAmmo;
                    ConsumeAmmo(1f);
                    if (IsExpired) return;
                }
            }
        }
    }
}