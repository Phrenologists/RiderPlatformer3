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
        // How many distance units the mount must travel on
        // the new surface before the previous surface's ray
        // becomes relevant again

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

            // Track steps on current surface for previous ray cooldown
            if (stuckToSurface)
            {
                TrackDistance(deltaTime);
                float moved = Vector2.Distance(mountBody.position, lastPosition);
                stepsOnCurrentSurface += moved;

                /*
                if (!previousRayActive &&
                    stepsOnCurrentSurface >= stepsBeforeRaycastActive) {
                    previousRayActive = true;
                    Debug.Log("[StickyPowerup] Previous ray now active again");
                }
                */
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
                Debug.Break();
                Debug.Log(downHit ? "Down" : "Jump");
                manager.StartCoroutine(StickAndRotate(0f));
                return;
            }

            if (ShouldStickToLeftWall(rot, dirX, dirY,
                    rightHit, leftHit, downHit, upHit))
            {
                Debug.Break();
                manager.StartCoroutine(StickAndRotate(270f));
                return;
            }

            if (ShouldStickToRightWall(rot, dirX, dirY,
                    rightHit, leftHit, downHit, upHit))
            {
                Debug.Break();
                manager.StartCoroutine(StickAndRotate(90f));
                return;
            }

            if (ShouldStickToCeiling(rot, dirX, dirY,
                    rightHit, leftHit, downHit, upHit))
            {
                Debug.Break();
                manager.StartCoroutine(StickAndRotate(180f));
                return;
            }

            /*
            // Determine best ray to act on
            ActiveRay bestRay = GetBestRay();


            if (bestRay != ActiveRay.None && !stuckToSurface) {
                manager.StartCoroutine(StickAndRotate(bestRay));
            } else if (bestRay != ActiveRay.None
                       && stuckToSurface
                       && bestRay != currentActiveRay) {
                manager.StartCoroutine(StickAndRotate(bestRay));
            } else if (bestRay == ActiveRay.None && stuckToSurface) {
                Unstick();
            }
            */
        }

        private bool ShouldStickToFloor(int rot, float dirX, float dirY, bool rightHit, bool leftHit, bool downHit, bool upHit)
        {
            Debug.Log(downHit);

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
        
        private void CheckForNewRotation(int rot, float dirX, float dirY)
        {
            if (rot == 0 && dirX > 0);
            {
                manager.StartCoroutine(StickAndRotate(270f));
            }

            if (rot == 180 && dirX > 0);
            {
                manager.StartCoroutine(StickAndRotate(270f));
            }
            if (rot == 0 && dirX < 0);
            {
                manager.StartCoroutine(StickAndRotate(90f));
            }
            if (rot == 180 && dirX < 0)
            {
                manager.StartCoroutine(StickAndRotate(90f));
            }

            if (rot == 90 && dirY > 0)
            {
                manager.StartCoroutine(StickAndRotate(180f));
            }

            if (rot == 270 && dirY > 0)
            {
                manager.StartCoroutine(StickAndRotate(180f));
            }
            if (rot == 90 && dirY < 0)
            {
                manager.StartCoroutine(StickAndRotate(0f));
                this.transform.position = new Vector3(transform.position.x, transform.position.y + 200f, transform.position.z);
            }

            if (rot == 270 && dirY < 0)
            {
                manager.StartCoroutine(StickAndRotate(0f));
                this.transform.position = new Vector3(transform.position.x, transform.position.y + 200f, transform.position.z);
            }
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


        // ── Ray Priority ──────────────────────────────────────────────────
/*
        private ActiveRay GetBestRay()
        {
            // Build candidate list excluding the previous active ray
            // until the step threshold is met

            bool rightValid = detector.FrontContact
                              && IsRayValid(ActiveRay.Right);
            bool leftValid = detector.BackContact
                             && IsRayValid(ActiveRay.Left);
            bool downValid = detector.DownContact
                             && IsRayValid(ActiveRay.Down);
            bool upValid = detector.UpContact
                           && IsRayValid(ActiveRay.Up);

            // Non-active rays take priority over the current active ray
            // Among non-active rays: Down > Right > Left > Up
            // (most common surfaces first)
            if (downValid && currentActiveRay != ActiveRay.Down)
                return ActiveRay.Down;
            if (rightValid && currentActiveRay != ActiveRay.Right)
                return ActiveRay.Right;
            if (leftValid && currentActiveRay != ActiveRay.Left)
                return ActiveRay.Left;
            if (upValid && currentActiveRay != ActiveRay.Up)
                return ActiveRay.Up;


            // Fall back to current active ray if still in contact
            if (currentActiveRay != ActiveRay.None)
            {
                bool currentContact = GetContactForRay(currentActiveRay);
                if (currentContact) return currentActiveRay;
            }

            // When on ceiling, diagonal hit means we're approaching
            // a wall — treat it as a right or left contact
            // so we transition smoothly instead of losing contact
            if (currentActiveRay == ActiveRay.Up && detector.DiagonalContact)
            {
                // Determine which side the diagonal hit is on
                // by checking which diagonal fired
                rightValid = true;
                // The diagonal detector already picks left vs right
                // internally — if left diagonal fired, treat as left
                // This will trigger StickAndRotate to the wall
            }

            return ActiveRay.None;
        }

        private bool IsRayValid(ActiveRay ray)
        {
            // The previous active ray is invalid until step threshold met
            if (!previousRayActive && ray == GetPreviousActiveRay())
                return false;
            return true;
        }
        */

        // We store the previous ray to know what to suppress
        //private ActiveRay previousActiveRay = ActiveRay.None;

        //private ActiveRay GetPreviousActiveRay()
        //{
            //return previousActiveRay;
        //}

        private bool GetContactForRay(ActiveRay ray)
        {
            switch (ray)
            {
                case ActiveRay.Right: return detector.FrontContact;
                case ActiveRay.Left: return detector.BackContact;
                case ActiveRay.Down: return detector.DownContact;
                case ActiveRay.Up: return detector.UpContact;
                default: return false;
            }
        }

        private void UpdateDetectorDimensions()
        {
            if (detector == null) return;

            switch (rotationAngle)
            {
                case 90:
                    detector.halfWidth = mountHalfHeight + 0.1f;
                    detector.halfHeight = mountHalfWidth;
                    //Debug.Log(manager.transform.rotation.z);
                    break;
                case 270:
                    // Mount is rotated 90/270 degrees on a wall
                    // What was the horizontal axis is now vertical and vice versa
                    // So the up/down raycasts need the wider half extent
                    // and the left/right raycasts need the taller half extent
                    detector.halfWidth = mountHalfHeight + 0.1f;
                    detector.halfHeight = mountHalfWidth;
                    //Debug.Log(manager.transform.rotation.z);
                    break;

                case 180:
                    detector.halfWidth = mountHalfWidth;
                    detector.halfHeight = mountHalfHeight;
                    //Debug.Log(manager.transform.rotation.z);
                    break;
                case 0:
                    detector.halfWidth = mountHalfWidth;
                    detector.halfHeight = mountHalfHeight;
                    //Debug.Log(manager.transform.rotation.z);
                    break;
                default:
                    // Normal orientation or ceiling (180 degrees — same extents)
                    detector.halfWidth = mountHalfWidth;
                    detector.halfHeight = mountHalfHeight;
                    //Debug.Log(manager.transform.rotation.z);
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

            //Debug.Log($"[StickyPowerup] Stuck at {targetAngle} degrees");

            // Track previous ray for suppression
            /*
            if(rotationAngle== 0)
            {
                previousActiveRay = ActiveRay.Down;
                //Debug.Log(manager.transform.rotation.z);
            }
            else if(rotationAngle == 90)
            {
                previousActiveRay = ActiveRay.Right;
                //Debug.Log(manager.transform.rotation.z);
            }
            else if (rotationAngle == 180)
            {
                previousActiveRay = ActiveRay.Up;
                //Debug.Log(manager.transform.rotation.z);
            }
            else if (rotationAngle == 270)
            {
                previousActiveRay = ActiveRay.Left;
                //Debug.Log(manager.transform.rotation.z);
            }
            currentActiveRay = ray;
            stepsOnCurrentSurface = 0f;
            previousRayActive = false;


            // Apply rotation instantly
            rotationAngle = GetAngleForRay(ray);
            manager.transform.rotation = Quaternion.Euler(0f, 0f, rotationAngle);

            // Wait one frame for rotation to settle
            yield return new WaitForFixedUpdate();
            //UpdateDetectorDimensions();

            // Reposition to be flush against the surface
            // and clear of any other detected surfaces
            RepositionOnSurface(ray);

            // Wait one more frame after reposition
            yield return new WaitForFixedUpdate();

            // Re-enable collider
            if (mountCollider != null)
                mountCollider.isTrigger = false;

            Debug.Log($"[StickyPowerup] Stuck to {ray} at angle {rotationAngle}");
            Debug.Log(ray);
            Debug.Log(previousActiveRay);
            */
        }
        /*
        private float GetAngleForRay(ActiveRay ray)
        {
            switch (ray)
            {
                case ActiveRay.Right: return 90f;
                case ActiveRay.Left: return 270f;
                case ActiveRay.Up: return 180f;
                case ActiveRay.Down:
                default: return 0f;
            }
        }
        */

        private void RepositionOnSurface(float angle)
        {

            Vector3 pos = manager.transform.position;
            int rot = Mathf.RoundToInt(angle);

            switch (rot)
            {
                case 0: // Floor
                    if (detector.DownHit.collider != null)
                    {
                        float gap = detector.detectionDistance - detector.DownHit.distance;
                        pos.y -= gap;
                    }

                    break;

                case 90: // Right wall
                    if (detector.RightHit.collider != null)
                    {
                        float gap = detector.detectionDistance - detector.RightHit.distance;
                        pos.x += gap;
                    }

                    break;

                case 270: // Left wall
                    if (detector.LeftHit.collider != null)
                    {
                        float gap = detector.detectionDistance - detector.LeftHit.distance;
                        pos.x -= gap;
                    }

                    break;

                case 180: // Ceiling
                    if (detector.UpHit.collider != null)
                    {
                        float gap = detector.detectionDistance - detector.UpHit.distance;
                        pos.y += gap;
                    }

                    break;
            }

            manager.transform.position = pos;
        }

        // Also check other active rays and adjust to avoid
        // clipping into those surfaces too
        // Only adjust axes not already handled by primary surface
        /*
        if (ray != ActiveRay.Right && ray != ActiveRay.Left) {
            // Primary was vertical — also check horizontal
            if (detector.RightHit.collider != null) {
                float gap = detector.detectionDistance
                    - detector.RightHit.distance;
                pos.x = Mathf.Min(pos.x,
                    pos.x - gap); // push away from right wall
            }
            if (detector.LeftHit.collider != null) {
                float gap = detector.detectionDistance
                    - detector.LeftHit.distance;
                pos.x = Mathf.Max(pos.x,
                    pos.x + gap); // push away from left wall
            }
        }

        if (ray != ActiveRay.Up && ray != ActiveRay.Down) {
            // Primary was horizontal — also check vertical
            if (detector.DownHit.collider != null) {
                float gap = detector.detectionDistance
                    - detector.DownHit.distance;
                pos.y = Mathf.Min(pos.y,
                    pos.y - gap);
            }
            if (detector.UpHit.collider != null) {
                float gap = detector.detectionDistance
                    - detector.UpHit.distance;
                pos.y = Mathf.Max(pos.y,
                    pos.y + gap);
            }
        }

        manager.transform.position = pos;
        */

        // ── Unstick ───────────────────────────────────────────────────────

        private void Unstick()
        {
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

            Vector2 jumpDir = GetJumpDirection();

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
            mountJump.TriggerJump();
            
            string surfaceRay = GetSurfaceRayName();
            
            detector.PauseRaycastingFor(raycastPauseDuration);
            
             foreach (string ray in GetOtherRayNames(surfaceRay)) 
             {
                    detector.PauseRayFor(ray, otherRaycastPauseDuration);
             }
            yield return null;
            mountJump.useExternalJumpDirection = false;
            mountJump.externalJumpDirection = Vector2.up;
            
            yield return new WaitForSeconds(raycastPauseDuration);
            justJumped = false;
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
