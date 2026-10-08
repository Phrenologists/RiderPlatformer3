// SpikePowerup.cs
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class SpikePowerup : MountPowerup {

        [Header("Spike Settings")]
        [SerializeField] private float spikeSpeed = 8f;
        [SerializeField] private float maxSpikeLength = 3f;
        [SerializeField] private float spikeThickness = 0.15f;
        [SerializeField] private LayerMask attachableLayers;
        [SerializeField] private LayerMask solidLayers;
        // Separate mask for non-attachable solids the spike stops on
        // Should include ground, walls — everything solid

        [Header("Floor Push")]
        [SerializeField] private float floorPushSpeed = 8f;

        [Header("Prefab")]
        [SerializeField] private GameObject spikePrefab;
        // Simple rectangle sprite centered at (0,0) in local space
        // No special pivot needed

        private characterMovement mountMovement;
        private characterJump mountJump;
        private characterGround mountGround;
        private Rigidbody2D mountBody;
        private CharacterMount characterMount;
        private Collider2D mountCollider;

        // Spike state
        private GameObject activeSpikeObj;
        private BoxCollider2D spikeCollider;
        private Vector2 spikeDirection;
        private float currentSpikeLength = 0f;
        private bool isExtending = false;
        private bool isRetracting = false;
        private bool isAttached = false;
        private bool isFloorPushing = false;
        private bool isComingFromTheGround = false;

        // Contact damage
        private SpikeContactDamage contactDamage;
        
        // Add this field to SpikePowerup:
        [Header("Platform")]
        [SerializeField] private GameObject spikePlatformPrefab;
// Prefab with EdgeCollider2D, Rigidbody2D (kinematic) and OneWayPlatform

        public GameObject activePlatform;

        protected override void OnActivate() {
            mountMovement = manager.MountMovement;
            mountJump = manager.MountJump;
            mountGround = manager.GetComponent<characterGround>();
            mountBody = manager.MountBody;
            mountCollider = manager.GetComponent<Collider2D>();
            characterMount = manager.GetComponent<CharacterMount>();

            contactDamage = manager.GetComponent<SpikeContactDamage>();
            if (contactDamage == null)
                contactDamage = manager.gameObject
                    .AddComponent<SpikeContactDamage>();
            contactDamage.enabled = true;

            Debug.Log("[SpikePowerup] Activated");
        }

        protected override void OnDeactivate() {
            if (activeSpikeObj != null) {
                activeSpikeObj.transform.SetParent(null);
                Destroy(activeSpikeObj);
                activeSpikeObj = null;
            }

            isExtending = false;
            isRetracting = false;
            isAttached = false;
            isFloorPushing = false;

            if (contactDamage != null)
                contactDamage.enabled = false;

            mountBody.gravityScale = 1f;

            Debug.Log("[SpikePowerup] Deactivated");
        }

        protected override void OnButtonPressed() {
            if (activeSpikeObj == null) {
                StartExtending();
            }
        }

        protected override void OnButtonReleased() {
                StartRetracting();
                mountBody.bodyType = RigidbodyType2D.Dynamic;
        }

        // ── Direction ─────────────────────────────────────────────────────

        private Vector2 GetSpikeDirection() {
            // ── SPIKE DIRECTION INPUT ─────────────────────────────────
            // Vertical input takes priority
            // Falls back to facing direction for horizontal
            // Modify here if direction feels wrong
            float vertical = mountMovement.directionY;
            float horizontal = mountMovement.directionX;

            if (vertical > 0.5f)   return Vector2.up;
            if (vertical < -0.5f)  return Vector2.down;
            if (horizontal < -0.5f) return Vector2.left;

            float facing = manager.transform.localScale.x > 0 ? 1f : -1f;
            return facing > 0 ? Vector2.right : Vector2.left;
        }

        // ── Extension ─────────────────────────────────────────────────────

        private void StartExtending() {
            spikeDirection = GetSpikeDirection();
            currentSpikeLength = 0f;
            isExtending = true;
            isRetracting = false;
            isAttached = false;
            isFloorPushing = false;

            // Instantiate and parent to mount
            activeSpikeObj = Instantiate(spikePrefab, manager.transform);
            activeSpikeObj.transform.localPosition = Vector3.zero;

            // Rotate to face spike direction
            float angle = Mathf.Atan2(spikeDirection.y, spikeDirection.x) * Mathf.Rad2Deg;
            activeSpikeObj.transform.localRotation = Quaternion.Euler(0f, 0f, angle);

            // Get collider and ignore mount collision
            spikeCollider = activeSpikeObj.GetComponent<BoxCollider2D>();

            if (spikeCollider != null) {
                // Ignore mount's own colliders
                Collider2D[] mountColliders =
                    manager.GetComponents<Collider2D>();
                foreach (var col in mountColliders) {
                    Physics2D.IgnoreCollision(spikeCollider, col);
                }

                // Ignore player collider if mounted
                if (characterMount != null && characterMount.IsMounted) {
                    var playerCol = characterMount.GetPlayerBody()
                        ?.GetComponent<Collider2D>();
                    if (playerCol != null)
                        Physics2D.IgnoreCollision(spikeCollider, playerCol);
                }
            }

            Debug.Log($"[SpikePowerup] Extending in direction: {spikeDirection}");
        }

        protected override void OnTick(float deltaTime) {
            if (activeSpikeObj == null) return;

            if (isExtending) {
                ExtendTick(deltaTime);
            } else if (isRetracting) {
                RetractTick(deltaTime);
            }

            if (isFloorPushing && currentSpikeLength <= maxSpikeLength + 0.1f) {
                FloorPushTick(deltaTime);
                isComingFromTheGround = true;
                currentSpikeLength += spikeSpeed * deltaTime;
                UpdateSpikeTransform();
            }
        }

        private void ExtendTick(float deltaTime) {
            currentSpikeLength += spikeSpeed * deltaTime;
            
            if (currentSpikeLength >= maxSpikeLength && isComingFromTheGround) {
                //Debug.Log("Reached PEAK");
                mountBody.gravityScale = 0f;
                mountBody.velocity = Vector2.zero;
                mountBody.bodyType = RigidbodyType2D.Static;
                currentSpikeLength = maxSpikeLength;
                return;
                //isExtending = false;
            }

            // Skip raycasts until spike has meaningful length
            if (currentSpikeLength < 0.05f) {
                UpdateSpikeTransform();
                return;
            }

            // Raycast from mount center in spike direction
            Vector2 origin = (Vector2)manager.transform.position;

            // Check for attachable surface
            RaycastHit2D attachHit = Physics2D.Raycast(
                origin,
                spikeDirection,
                currentSpikeLength,
                attachableLayers
            );

            if (attachHit.collider != null) {
                currentSpikeLength = attachHit.distance;
                UpdateSpikeTransform();
                Attach();
                return;
            }

            // Check for non-attachable solid — spike stops but retracts
            // Exclude mount and spike layers
            int excludeMask = ~((1 << manager.gameObject.layer)
                | (activeSpikeObj != null
                    ? (1 << activeSpikeObj.layer) : 0));

            RaycastHit2D solidHit = Physics2D.Raycast(
                origin,
                spikeDirection,
                currentSpikeLength,
                solidLayers & excludeMask
            );

            if (solidHit.collider != null && !IsInLayerMask(solidHit.collider.gameObject.layer, attachableLayers)) {
                currentSpikeLength = solidHit.distance;
                UpdateSpikeTransform();
                StartRetracting();
                return;
            }

            // Reached max length — hold until button released
            if (currentSpikeLength >= maxSpikeLength) {
                Debug.Log("Reached PEAK");
                if (isComingFromTheGround)
                {
                    mountBody.gravityScale = 0f;
                    mountBody.velocity = Vector2.zero;
                    mountBody.bodyType = RigidbodyType2D.Static;
                }
                currentSpikeLength = maxSpikeLength;
                //isExtending = false;
            }

            UpdateSpikeTransform();
        }

        private void RetractTick(float deltaTime) {
            currentSpikeLength -= spikeSpeed * deltaTime;

            if (currentSpikeLength <= 0f) {
                currentSpikeLength = 0f;
                isRetracting = false;
                activeSpikeObj.transform.SetParent(null);
                Destroy(activeSpikeObj);
                activeSpikeObj = null;
                Debug.Log("[SpikePowerup] Spike fully retracted");
                return;
            }

            UpdateSpikeTransform();
        }

        private void Attach() {
            isExtending = false;
            isAttached = true;
            //Debug.Log("attached");

            bool isDown = spikeDirection == Vector2.down;
            bool grounded = mountGround != null
                && mountGround.GetOnGround();

            if (isDown && grounded) {
                // Floor push — mount moves upward as spike extends
                isFloorPushing = true;
                Debug.Log("[SpikePowerup] Floor push started");
            } else {
                // Freeze mount mid-air
                mountBody.gravityScale = 0f;
                mountBody.velocity = Vector2.zero;
                mountBody.bodyType = RigidbodyType2D.Static;
                Debug.Log("[SpikePowerup] Attached and frozen");
            }
            if (IsHorizontal(spikeDirection) && spikePlatformPrefab != null) {
                SpawnSpikePlatform();
            }
        }

        // ── Floor Push ────────────────────────────────────────────────────

        private void FloorPushTick(float deltaTime) {
            
            if (currentSpikeLength >= maxSpikeLength && isComingFromTheGround) {
                Debug.Log("Reached PEAK");
                mountBody.gravityScale = 0f;
                if(mountBody.bodyType == RigidbodyType2D.Dynamic)
                {
                    mountBody.velocity = Vector2.zero;
                }
                mountBody.bodyType = RigidbodyType2D.Static;
                currentSpikeLength = maxSpikeLength;
                isExtending = false;
                isComingFromTheGround = false;
                return;
            }
            if (!isAttached) {
                Debug.Log("[SpikePowerup] Floor push ended");
                isFloorPushing = false;
                return;
            }
            

            mountBody.velocity = new Vector2(mountBody.velocity.x, floorPushSpeed);
        }

        // ── Retraction ────────────────────────────────────────────────────

        private void StartRetracting() {
            isExtending = false;
            isAttached = false;
            isFloorPushing = false;
            isRetracting = true;
            isComingFromTheGround = false;

            // Restore gravity immediately so mount starts falling
            mountBody.gravityScale = 1f;
            
            if (activePlatform != null) {
                Destroy(activePlatform);
                activePlatform = null;
            }


            Debug.Log("[SpikePowerup] Retracting");
        }

        // ── Transform ─────────────────────────────────────────────────────

        private void UpdateSpikeTransform() {
            if (activeSpikeObj == null) return;

            // Position in local space — spike is parented to mount
            // Offset by half length so base is at mount center
            if(spikeDirection == Vector2.right || spikeDirection == Vector2.up || spikeDirection == Vector2.down)
            {
                activeSpikeObj.transform.localPosition = spikeDirection * (currentSpikeLength * 0.5f);

                activeSpikeObj.transform.localScale = new Vector3(currentSpikeLength, spikeThickness, 1f);
            }
            else if (spikeDirection == Vector2.left)
            {
                activeSpikeObj.transform.localPosition = spikeDirection * (currentSpikeLength * -0.5f);

                activeSpikeObj.transform.localScale = new Vector3(currentSpikeLength, spikeThickness, 1f);
            }
        }
        // Add this method:
        private void SpawnSpikePlatform() {
            activePlatform = Instantiate(spikePlatformPrefab, activeSpikeObj.transform);

            var edgeCollider = activePlatform.GetComponent<EdgeCollider2D>();
            if (edgeCollider == null) {
                Debug.LogWarning("[SpikePowerup] SpikePlatform prefab " +
                                 "has no EdgeCollider2D");
                return;
            }
            activePlatform.transform.localPosition = Vector3.zero;
    
            // Mount center in world space
            Vector2 mountCenter = manager.transform.position;
            
            Debug.Log(manager.transform.position);

            // Set edge collider points in world space
            // EdgeCollider2D points are in local space, so position
            // the platform at world origin and use world coords directly
            edgeCollider.SetPoints(new System.Collections.Generic.List<Vector2> { new Vector2 (-0.1f, 0), new Vector2(0.17f, 0)});

            // Make the platform kinematic so it doesn't fall
            var rb = activePlatform.GetComponent<Rigidbody2D>();
            if (rb != null) rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private bool IsHorizontal(Vector2 dir) {
            return Mathf.Abs(dir.x) > Mathf.Abs(dir.y);
        }

        private bool IsInLayerMask(int layer, LayerMask mask) {
            return (mask.value & (1 << layer)) != 0;
        }
    }
}
