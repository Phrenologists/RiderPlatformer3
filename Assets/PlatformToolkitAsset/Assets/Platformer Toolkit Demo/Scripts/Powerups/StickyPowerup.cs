// StickyPowerup.cs - with rotation, repositioning and active ray tracking
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class StickyPowerup : MountPowerup {

        [Header("Jump Settings")]
        [SerializeField] private float raycastPauseDuration = 1f;

        [Header("Step Threshold")]
        [SerializeField] private float stepsBeforeRaycastActive = 2f;
        // How many distance units the mount must travel on
        // the new surface before the previous surface's ray
        // becomes relevant again

        private StickyCollisionDetector detector;
        private characterJump mountJump;
        private Rigidbody2D mountBody;
        private Collider2D mountCollider;

        private bool stuckToSurface = false;

        // Which ray caused the current rotation
        // None = not stuck, Right/Left/Down/Up = active surface
        private enum ActiveRay { None, Right, Left, Down, Up }
        private ActiveRay currentActiveRay = ActiveRay.None;

        // Steps taken on current surface — used to decide when
        // the previous surface's ray becomes relevant again
        private float stepsOnCurrentSurface = 0f;
        private bool previousRayActive = false;
        // When false, the ray that WAS active before rotation is ignored

        private Vector2 lastPosition;

        protected override void OnActivate() {
            mountJump = manager.MountJump;
            mountBody = manager.MountBody;
            mountCollider = manager.GetComponent<Collider2D>();

            detector = manager.GetComponent<StickyCollisionDetector>();
            if (detector == null)
                detector = manager.gameObject
                    .AddComponent<StickyCollisionDetector>();

            detector.enabled = true;
            lastPosition = mountBody.position;

            Debug.Log("[StickyPowerup] Activated");
        }

        protected override void OnDeactivate() {
            Unstick();

            if (detector != null)
                detector.enabled = false;

            // Restore rotation
            manager.transform.rotation = Quaternion.identity;

            Debug.Log("[StickyPowerup] Deactivated");
        }

        protected override void OnTick(float deltaTime) {
            if (detector == null) return;

            // Track steps on current surface for previous ray cooldown
            if (stuckToSurface) {
                float moved = Vector2.Distance(
                    mountBody.position, lastPosition
                );
                stepsOnCurrentSurface += moved;

                if (!previousRayActive &&
                    stepsOnCurrentSurface >= stepsBeforeRaycastActive) {
                    previousRayActive = true;
                    Debug.Log("[StickyPowerup] Previous ray now active again");
                }
            }

            lastPosition = mountBody.position;

            // Determine best ray to act on, respecting active ray rules
            ActiveRay bestRay = GetBestRay();

            if (bestRay != ActiveRay.None && !stuckToSurface) {
                // First contact — stick and rotate
                manager.StartCoroutine(
                    StickAndRotate(bestRay)
                );
            } else if (bestRay != ActiveRay.None
                && stuckToSurface
                && bestRay != currentActiveRay) {
                // Already stuck but a different surface detected —
                // rotate to new surface
                manager.StartCoroutine(StickAndRotate(bestRay));
            } else if (bestRay == ActiveRay.None && stuckToSurface) {
                Unstick();
            }
        }

        // ── Ray Priority ──────────────────────────────────────────────────

        private ActiveRay GetBestRay() {
            // Build candidate list excluding the previous active ray
            // until the step threshold is met
            bool rightValid = detector.FrontContact
                && IsRayValid(ActiveRay.Right);
            bool leftValid  = detector.BackContact
                && IsRayValid(ActiveRay.Left);
            bool downValid  = detector.DownContact
                && IsRayValid(ActiveRay.Down);
            bool upValid    = detector.UpContact
                && IsRayValid(ActiveRay.Up);

            // Non-active rays take priority over the current active ray
            // Among non-active rays: Down > Right > Left > Up
            // (most common surfaces first)
            if (downValid  && currentActiveRay != ActiveRay.Down)
                return ActiveRay.Down;
            if (rightValid && currentActiveRay != ActiveRay.Right)
                return ActiveRay.Right;
            if (leftValid  && currentActiveRay != ActiveRay.Left)
                return ActiveRay.Left;
            if (upValid    && currentActiveRay != ActiveRay.Up)
                return ActiveRay.Up;

            // Fall back to current active ray if still in contact
            if (currentActiveRay != ActiveRay.None) {
                bool currentContact = GetContactForRay(currentActiveRay);
                if (currentContact) return currentActiveRay;
            }

            return ActiveRay.None;
        }

        private bool IsRayValid(ActiveRay ray) {
            // The previous active ray is invalid until step threshold met
            if (!previousRayActive && ray == GetPreviousActiveRay())
                return false;
            return true;
        }

        // We store the previous ray to know what to suppress
        private ActiveRay previousActiveRay = ActiveRay.None;

        private ActiveRay GetPreviousActiveRay() {
            return previousActiveRay;
        }

        private bool GetContactForRay(ActiveRay ray) {
            switch (ray) {
                case ActiveRay.Right: return detector.FrontContact;
                case ActiveRay.Left:  return detector.BackContact;
                case ActiveRay.Down:  return detector.DownContact;
                case ActiveRay.Up:    return detector.UpContact;
                default: return false;
            }
        }

        // ── Stick and Rotate ──────────────────────────────────────────────

        private IEnumerator StickAndRotate(ActiveRay ray) {
            // Prevent re-entry while rotating
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
            
            var juice = manager.GetComponentInChildren<characterJuice>();
            if (juice != null) juice.externalRotationControl = true;

            // Track previous ray for suppression
            previousActiveRay = currentActiveRay;
            currentActiveRay = ray;
            stepsOnCurrentSurface = 0f;
            previousRayActive = false;

            // Apply rotation instantly
            float angle = GetAngleForRay(ray);
            manager.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Wait one frame for rotation to settle
            yield return new WaitForFixedUpdate();

            // Reposition to be flush against the surface
            // and clear of any other detected surfaces
            RepositionOnSurface(ray);

            // Wait one more frame after reposition
            yield return new WaitForFixedUpdate();

            // Re-enable collider
            if (mountCollider != null)
                mountCollider.isTrigger = false;

            Debug.Log($"[StickyPowerup] Stuck to {ray} at angle {angle}");
        }

        private float GetAngleForRay(ActiveRay ray) {
            switch (ray) {
                case ActiveRay.Right: return 90f;
                case ActiveRay.Left:  return 270f;
                case ActiveRay.Up:    return 180f;
                case ActiveRay.Down:
                default:              return 0f;
            }
        }

        private void RepositionOnSurface(ActiveRay ray) {
            Vector3 pos = manager.transform.position;

            // Move flush against the primary surface
            switch (ray) {
                case ActiveRay.Right:
                    if (detector.RightHit.collider != null) {
                        float gap = detector.detectionDistance
                            - detector.RightHit.distance;
                        pos.x += gap;
                    }
                    break;

                case ActiveRay.Left:
                    if (detector.LeftHit.collider != null) {
                        float gap = detector.detectionDistance
                            - detector.LeftHit.distance;
                        pos.x -= gap;
                    }
                    break;

                case ActiveRay.Down:
                    if (detector.DownHit.collider != null) {
                        float gap = detector.detectionDistance
                            - detector.DownHit.distance;
                        pos.y -= gap;
                    }
                    break;

                case ActiveRay.Up:
                    if (detector.UpHit.collider != null) {
                        float gap = 0;
                        pos.y += gap;
                    }
                    break;
            }

            // Also check other active rays and adjust to avoid
            // clipping into those surfaces too
            // Only adjust axes not already handled by primary surface

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
        }

        // ── Unstick ───────────────────────────────────────────────────────

        private void Unstick() {
            if (!stuckToSurface) return;
            stuckToSurface = false;
            previousActiveRay = currentActiveRay;
            currentActiveRay = ActiveRay.None;
            stepsOnCurrentSurface = 0f;
            previousRayActive = false;

            mountBody.gravityScale = 1f;

            if (!mountJump.enabled)
                mountJump.enabled = true;
            
            var juice = manager.GetComponentInChildren<characterJuice>();
            if (juice != null) juice.externalRotationControl = false;

            Debug.Log("[StickyPowerup] Unstuck");
        }

        // ── Jump ──────────────────────────────────────────────────────────

        public void OnJumpPressed() {
            if (!stuckToSurface) return;

            Debug.Log("[StickyPowerup] Jump pressed");

            // Unstick first — restores gravity and jump script
            Unstick();

            // Restore rotation before jump
            manager.transform.rotation = Quaternion.identity;

            // Pause raycasting so mount clears the surface
            detector.PauseRaycastingFor(raycastPauseDuration);

            manager.StartCoroutine(FireJump());
        }

        private IEnumerator FireJump() {
            yield return null;
            mountJump.TriggerJump();
        }
    }
}
