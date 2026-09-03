// StickyPowerup.cs - with debug tools and ground detection fix
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class StickyPowerup : MountPowerup {

        [Header("Sticky Settings")]
        [SerializeField] private LayerMask stickyLayers;
        [SerializeField] private float stickForce = 20f;

        [Header("Ammo Settings")]
        [SerializeField] private float unitsPerAmmo = 1f;

        [Header("Rotation")]
        [SerializeField] private float groundAngle = 0f;
        [SerializeField] private float ceilingAngle = 180f;
        [SerializeField] private float leftWallAngle = 90f;
        [SerializeField] private float rightWallAngle = 270f;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showDebugGizmos = true;

        private Rigidbody2D mountBody;
        private characterMovement mountMovement;
        private characterJump mountJump;
        private characterGround mountGround;
        private CharacterMount characterMount;
        private Transform mountTransform;

        private bool isSticking = false;
        private Vector2 currentSurfaceNormal = Vector2.up;
        private SurfaceType currentSurface = SurfaceType.Ground;

        private Dictionary<Collider2D, Vector2> activeContacts
            = new Dictionary<Collider2D, Vector2>();

        private Vector2 lastPosition;
        private float distanceAccumulator = 0f;

        // Debug state
        private string lastDebugMessage = "Not activated";
        private int contactCount = 0;
        private Vector2 lastAppliedStickForce = Vector2.zero;
        private Vector2 lastMovementVelocity = Vector2.zero;
        
        [Header("Visual Root")]
        [SerializeField] private Transform visualRoot;
        
        private SurfaceType pendingSurface = SurfaceType.Ground;
        private float surfaceTransitionTimer = 0f;
        private const float surfaceTransitionDelay = 0.1f;

        private enum SurfaceType {
            Ground,
            Ceiling,
            LeftWall,
            RightWall
        }

        protected override void OnActivate() {
            mountBody = manager.MountBody;
            mountMovement = manager.MountMovement;
            mountJump = manager.MountJump;
            mountGround = manager.GetComponent<characterGround>();
            mountTransform = manager.transform;
            characterMount = manager.GetComponent<CharacterMount>();

            lastPosition = mountBody.position;

            Log("Powerup activated. Setting up collision listener.");
            Log($"Mount layer: {LayerMask.LayerToName(manager.gameObject.layer)}");
            Log($"Sticky layers mask value: {stickyLayers.value}");

            var listener = manager.GetComponent<StickyCollisionListener>();
            if (listener == null) {
                listener = manager.gameObject
                    .AddComponent<StickyCollisionListener>();
                Log("Created new StickyCollisionListener");
            } else {
                Log("Found existing StickyCollisionListener");
            }
            listener.Initialise(this, stickyLayers);

            // Verify components
            if (mountBody == null)
                Debug.LogError("[StickyPowerup] MountBody is null!");
            if (mountMovement == null)
                Debug.LogError("[StickyPowerup] MountMovement is null!");
            if (mountGround == null)
                Debug.LogWarning("[StickyPowerup] characterGround not found " +
                    "- ground detection rotation won't work");
        }

        protected override void OnDeactivate() {
            Log("Powerup deactivated - restoring state");
            isSticking = false;
            activeContacts.Clear();

            if (mountBody != null) {
                mountBody.gravityScale = 1f;
            }
            if (mountJump != null) {
                mountJump.externalGravityOverride = false;
                mountJump.useExternalJumpDirection = false;
                mountJump.externalJumpDirection = Vector2.up;
                mountJump.CancelJump();
            }
            if (mountMovement != null) {
                mountMovement.externalFlipControl = false;
            }

            if (mountTransform != null) {
                mountTransform.rotation = Quaternion.identity;
            }

            RestoreGroundDetection();

            var listener = manager.GetComponent<StickyCollisionListener>();
            if (listener != null) Destroy(listener);
            
            //Transform target = visualRoot != null ? visualRoot : mountTransform;
            //target.localRotation = Quaternion.identity;
        }

        protected override void OnTick(float deltaTime) {
            if (!isSticking) return;

            ApplyStickForce();
            UpdateGroundDetectionDirection();
            UpdateStickyFacing();
            TrackDistanceForAmmo(deltaTime);
        }

        // ── Surface Contact ───────────────────────────────────────────────

        public void OnSurfaceContact(Collider2D col, Vector2 normal) {
            bool isNew = !activeContacts.ContainsKey(col);
            activeContacts[col] = normal;
            contactCount = activeContacts.Count;

            if (isNew) {
                Log($"New surface contact. Collider: {col.gameObject.name}, " +
                    $"Normal: {normal}, Layer: " +
                    $"{LayerMask.LayerToName(col.gameObject.layer)}, " +
                    $"Total contacts: {contactCount}");
            }

            UpdateStickState();
        }

        public void OnSurfaceExit(Collider2D col) {
            if (activeContacts.ContainsKey(col)) {
                Log($"Surface exit. Collider: {col.gameObject.name}, " +
                    $"Remaining contacts: {activeContacts.Count - 1}");
                activeContacts.Remove(col);
                contactCount = activeContacts.Count;
            }

            UpdateStickState();
        }

        private void UpdateStickState() {
            if (activeContacts.Count == 0) {
                if (isSticking) {
                    Log("No contacts - stopping stick");
                    isSticking = false;
                    mountBody.gravityScale = 1f;
                    mountJump.externalGravityOverride = false;
                    ApplyRotation(SurfaceType.Ground);
                    RestoreGroundDetection();
                }
                lastDebugMessage = "No contacts";
                return;
            }
            
            // Clean up destroyed contacts
            var keysToRemove = new System.Collections.Generic.List<Collider2D>();
            foreach (var kvp in activeContacts) {
                if (kvp.Key == null) keysToRemove.Add(kvp.Key);
            }
            foreach (var key in keysToRemove) activeContacts.Remove(key);

            Vector2 bestNormal = Vector2.up;
            SurfaceType bestSurface = SurfaceType.Ground;
            float bestPriority = -1f;
            string bestColName = "none";

            foreach (var kvp in activeContacts) {
                if (kvp.Key == null) continue;
                Vector2 normal = kvp.Value;
                SurfaceType surface = ClassifySurface(normal);
                float priority = GetSurfacePriority(surface);

                Log($"  Contact: {kvp.Key.gameObject.name}, " +
                    $"Normal: {normal:F2}, Surface: {surface}, " +
                    $"Priority: {priority}");

                if (priority > bestPriority) {
                    bestPriority = priority;
                    bestNormal = normal;
                    bestSurface = surface;
                    bestColName = kvp.Key.gameObject.name;
                }
            }

            bool shouldStick = bestSurface != SurfaceType.Ground;
            bool surfaceChanged = bestSurface != currentSurface;
            bool stickChanged = shouldStick != isSticking;
            
            if (!stickChanged && !surfaceChanged) return;

            SurfaceType previousSurface = currentSurface;
            currentSurfaceNormal = bestNormal;
            currentSurface = bestSurface;

            if (shouldStick != isSticking || bestSurface != previousSurface) {
                Log($"Stick state changed. isSticking: {shouldStick}, " +
                    $"Surface: {bestSurface}, Normal: {bestNormal:F2}, " +
                    $"Best contact: {bestColName}");
            }

            isSticking = shouldStick;
            lastDebugMessage = $"Surface: {bestSurface}, Normal: {bestNormal:F2}, " +
                $"Contacts: {activeContacts.Count}";

            if (isSticking) {
                mountBody.gravityScale = 0f;
                mountJump.externalGravityOverride = true;
                mountJump.useExternalJumpDirection = true;
                mountJump.externalJumpDirection = currentSurfaceNormal;
                mountJump.CancelJump();
                mountMovement.externalFlipControl = true;
                // Zero out velocity component pointing away from surface
                // on first contact so we don't bounce off
                float awayFromSurface = Vector2.Dot(
                    mountBody.velocity, currentSurfaceNormal
                );
                if (awayFromSurface > 0) {
                    mountBody.velocity -= currentSurfaceNormal * awayFromSurface;
                }
                if(surfaceChanged)
                {
                    ApplyRotation(bestSurface);
                    UpdateGroundDetectionDirection();
                }
            } else {
                
                mountBody.gravityScale = 1f;
                mountJump.externalGravityOverride = false;
                mountJump.useExternalJumpDirection = false;
                mountJump.externalJumpDirection = Vector2.up;
                mountMovement.externalFlipControl = false;
                ApplyRotation(SurfaceType.Ground);
                RestoreGroundDetection();
            }
            
            SurfaceType newBestSurface = bestSurface;

            if (newBestSurface != currentSurface) {
                if (newBestSurface != pendingSurface) {
                    // New candidate surface — start timer
                    pendingSurface = newBestSurface;
                    surfaceTransitionTimer = surfaceTransitionDelay;
                } else {
                    // Same candidate — count down
                    surfaceTransitionTimer -= Time.fixedDeltaTime;
                    if (surfaceTransitionTimer <= 0f) {
                        // Transition confirmed
                        currentSurface = pendingSurface;
                        currentSurfaceNormal = bestNormal;
                        Log($"Surface transition confirmed: {currentSurface}");
                    }
                }
            } else {
                // Already on this surface — reset pending
                pendingSurface = currentSurface;
                surfaceTransitionTimer = 0f;
            }
        }

        private SurfaceType ClassifySurface(Vector2 normal) {
            float dot = Vector2.Dot(normal, Vector2.up);

            // Wider thresholds to prevent flickering between categories
            // when contact normals fluctuate slightly
            if (dot > 0.5f) return SurfaceType.Ground;
            if (dot < -0.5f) return SurfaceType.Ceiling;
            if (normal.x > 0.5f) return SurfaceType.RightWall;
            return SurfaceType.LeftWall;
        }

        private float GetSurfacePriority(SurfaceType surface) {
            switch (surface) {
                case SurfaceType.Ceiling: return 3f;
                case SurfaceType.LeftWall:
                case SurfaceType.RightWall: return 2f;
                case SurfaceType.Ground: return 1f;
                default: return 0f;
            }
        }

        // ── Ground Detection ──────────────────────────────────────────────

        private void UpdateGroundDetectionDirection() {
            if (mountGround == null) return;

            // Rotate the ground check direction to match surface normal
            // When on a wall, ground detection points into the wall
            // When on ceiling, it points upward
            mountGround.SetGroundCheckDirection(-currentSurfaceNormal);

            Log($"Ground check direction set to: {-currentSurfaceNormal:F2}");
        }

        private void RestoreGroundDetection() {
            if (mountGround == null) return;
            mountGround.SetGroundCheckDirection(Vector2.down);
            Log("Ground check direction restored to down");
        }

        // ── Physics ───────────────────────────────────────────────────────

        private void ApplyStickForce() {
            if (!isSticking) return;

            // Instead of fighting gravity with a counterforce,
            // zero out gravity and apply our own in the surface direction
            // -currentSurfaceNormal points INTO the surface
            Vector2 customGravity = -currentSurfaceNormal * (Physics2D.gravity.magnitude * mountBody.mass);

            mountBody.AddForce(customGravity, ForceMode2D.Force);

            // Dampen velocity component perpendicular to surface
            // to prevent the mount from bouncing off
            Vector2 perpendicularVelocity = Vector2.Dot(
                mountBody.velocity, -currentSurfaceNormal
            ) * (-currentSurfaceNormal);

            // Only dampen if moving away from surface
            if (Vector2.Dot(mountBody.velocity, currentSurfaceNormal) > 0) {
                mountBody.velocity -= perpendicularVelocity;
            }

            ApplyStickyMovement();
        }

        private void ApplyStickyMovement() {
            float horizontalInput = mountMovement.directionX;
            float verticalInput = mountMovement.directionY;

            // Calculate movement along the surface tangent
            // Tangent is perpendicular to the surface normal
            Vector2 surfaceTangent = new Vector2(
                currentSurfaceNormal.y,
                -currentSurfaceNormal.x
            );

            float inputAlongTangent;
            switch (currentSurface) {
                case SurfaceType.Ceiling:
                    // On ceiling, horizontal input maps to tangent direction
                    inputAlongTangent = horizontalInput;
                    break;
                case SurfaceType.LeftWall:
                case SurfaceType.RightWall:
                    // On wall, vertical input maps to tangent direction
                    inputAlongTangent = verticalInput;
                    break;
                default:
                    return;
            }

            // Apply velocity along surface tangent
            Vector2 targetVelocity = surfaceTangent * inputAlongTangent * mountMovement.maxSpeed;

            // Keep velocity component into the surface (from stick force)
            // but replace tangential component with our input
            float intoSurface = Vector2.Dot(
                mountBody.velocity, -currentSurfaceNormal
            );
            mountBody.velocity = (-currentSurfaceNormal * intoSurface)
                                 + targetVelocity;
        }

        // ── Rotation ──────────────────────────────────────────────────────

        private void ApplyRotation(SurfaceType surface) {
            float angle;
            switch (surface) {
                case SurfaceType.Ceiling: angle = ceilingAngle; break;
                case SurfaceType.LeftWall: angle = leftWallAngle; break;
                case SurfaceType.RightWall: angle = rightWallAngle; break;
                default: angle = groundAngle; break;
            }
            
            mountTransform.rotation = Quaternion.Euler(0f, 0f, angle);


            // Handle facing direction separately via localScale.x
            // since we've taken over flip control
            UpdateStickyFacing();

            if (characterMount != null && characterMount.IsMounted) {
                characterMount.RotatePlayer(angle);
            }

            Log($"Rotation applied: {angle} degrees for {surface}");
        }
        
        private void UpdateStickyFacing() {
            // Determine which input axis controls facing on current surface
            float facingInput;
            switch (currentSurface) {
                case SurfaceType.Ceiling:
                    // On ceiling, horizontal input determines facing
                    facingInput = mountMovement.directionX;
                    break;
                case SurfaceType.LeftWall:
                case SurfaceType.RightWall:
                    // On walls, vertical input determines facing
                    facingInput = mountMovement.directionY;
                    break;
                default:
                    facingInput = mountMovement.directionX;
                    break;
            }

            if (Mathf.Abs(facingInput) > 0.1f) {
                float currentScaleX = mountTransform.localScale.x;
                float newScaleX = facingInput > 0 ? 
                    Mathf.Abs(currentScaleX) : 
                    -Mathf.Abs(currentScaleX);
                mountTransform.localScale = new Vector3(
                    newScaleX,
                    mountTransform.localScale.y,
                    mountTransform.localScale.z
                );
            }
        }


        // ── Ammo Tracking ─────────────────────────────────────────────────

        private void TrackDistanceForAmmo(float deltaTime) {
            Vector2 currentPos = mountBody.position;
            float distanceMoved = Vector2.Distance(currentPos, lastPosition);
            lastPosition = currentPos;

            if (distanceMoved > 0.001f) {
                distanceAccumulator += distanceMoved;

                while (distanceAccumulator >= unitsPerAmmo) {
                    distanceAccumulator -= unitsPerAmmo;
                    ConsumeAmmo(1f);
                    Log($"Ammo consumed. Remaining: {RemainingAmmo}");
                    if (IsExpired) return;
                }
            }
        }

        // ── Debug ─────────────────────────────────────────────────────────

        private void Log(string message) {
            if (showDebugLogs) {
                Debug.Log($"[StickyPowerup] {message}");
            }
        }

        private void OnDrawGizmos() {
            if (!showDebugGizmos || !Application.isPlaying) return;
            if (mountTransform == null) return;

            // Surface normal
            Gizmos.color = isSticking ? Color.green : Color.red;
            Gizmos.DrawLine(
                mountTransform.position,
                mountTransform.position + (Vector3)currentSurfaceNormal
            );

            // Stick force direction
            if (isSticking) {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(
                    mountTransform.position,
                    mountTransform.position
                        + (Vector3)(currentSurfaceNormal * 0.5f)
                );
            }

            // Active contacts
            Gizmos.color = Color.yellow;
            foreach (var kvp in activeContacts) {
                if (kvp.Key == null) continue;
                Gizmos.DrawSphere(
                    kvp.Key.ClosestPoint(mountTransform.position), 0.1f
                );
                Gizmos.DrawLine(
                    mountTransform.position,
                    mountTransform.position + (Vector3)kvp.Value * 0.8f
                );
            }
        }

        private void OnGUI() {
            if (!showDebugGizmos || !Application.isPlaying) return;
            if (Camera.main == null || mountTransform == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(
                mountTransform.position
            );
            if (screenPos.z < 0) return;
            screenPos.y = Screen.height - screenPos.y;

            GUI.color = isSticking ? Color.green : Color.white;
            GUI.Label(
                new Rect(screenPos.x - 120f, screenPos.y - 140f, 260f, 160f),
                $"=== STICKY POWERUP ===\n" +
                $"isSticking: {isSticking}\n" +
                $"Surface: {currentSurface}\n" +
                $"Normal: {currentSurfaceNormal:F2}\n" +
                $"Contacts: {contactCount}\n" +
                $"Gravity scale: {mountBody?.gravityScale:F1}\n" +
                $"Velocity: {mountBody?.velocity:F2}\n" +
                $"Ammo: {RemainingAmmo:F1}\n" +
                $"State: {lastDebugMessage}"
            );
        }
    }
}
