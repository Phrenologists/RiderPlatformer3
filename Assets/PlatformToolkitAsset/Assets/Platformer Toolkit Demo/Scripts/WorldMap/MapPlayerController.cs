// MapPlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class MapPlayerController : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private Animator animator;

        [Header("Settings")]
        [SerializeField] private float moveSpeed = 3f;

        // Animator parameter hashes
        private static readonly int AnimIdle = Animator.StringToHash("Idle");
        private static readonly int AnimWalkLeft = Animator.StringToHash("WalkLeft");
        private static readonly int AnimWalkRight = Animator.StringToHash("WalkRight");
        private static readonly int AnimWalkUp = Animator.StringToHash("WalkUp");
        private static readonly int AnimWalkDown = Animator.StringToHash("WalkDown");

        // State
        private MapNode currentNode;
        private bool isMoving = false;
        private bool inputLocked = false;
        private Vector2 heldDirection = Vector2.zero;
        
        private IMapManager mapManager;

        // ── Initialisation ────────────────────────────────────────────────
        
        private void Start() {
            // Finds whichever manager is present in the current scene
            mapManager = FindObjectOfType<WorldMapManager>() as IMapManager
                         ?? FindObjectOfType<KingdomMapManager>() as IMapManager;
        }

        public void Initialise(MapNode startNode) {
            currentNode = startNode;
            transform.position = startNode.transform.position;
            PlayAnimation(AnimIdle);
            //Debug.Log("Initialising map node: " + startNode);
        }

        // ── Input ─────────────────────────────────────────────────────────

        // Wire this up in the InputManager's PlayerInput Unity Events
        public void OnMapMove(InputAction.CallbackContext context) {
            if (context.performed) {
                Vector2 raw = context.ReadValue<Vector2>();
                heldDirection = SnapToCardinal(raw);
            } else if (context.canceled) {
                heldDirection = Vector2.zero;
            }
        }

        // ── Update ────────────────────────────────────────────────────────

        private void Update() {
            if (isMoving || inputLocked) return;

            if (heldDirection != Vector2.zero) {
                TryMove(heldDirection);
            }
        }

        // ── Movement ──────────────────────────────────────────────────────

        private void TryMove(Vector2 direction) {
            MapPath path = currentNode.GetPathInDirection(direction);
            MapNode targetNode = currentNode.GetNodeInDirection(direction);

            if (path == null || targetNode == null) return;
            if (!targetNode.IsUnlocked()) return;

            // Determine animation based on which direction the TARGET node
            // is relative to the CURRENT node — not the input direction,
            // since diagonal nodes could be mapped to a cardinal input
            Vector2 toTarget = targetNode.transform.position
                             - currentNode.transform.position;
            int walkAnim = GetWalkAnimation(toTarget);

            StartCoroutine(MoveAlongPath(path, targetNode, walkAnim));
        }

        private IEnumerator MoveAlongPath(MapPath path, MapNode targetNode, int walkAnim) {
            isMoving = true;
            //KingdomMapManager.Instance?.OnPlayerLeftNode(currentNode);
            mapManager?.OnPlayerLeftNode(currentNode);
            PlayAnimation(walkAnim);

            // Calculate total path length so we can move at a fixed world speed
            float pathLength = CalculatePathLength(path);
            float duration = pathLength / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = path.GetPositionAtT(t);
                yield return null;
            }

            // Snap to the target node position exactly
            transform.position = targetNode.transform.position;
            currentNode = targetNode;
            isMoving = false;

            // Notify the map manager that we've arrived
            //KingdomMapManager.Instance?.OnPlayerArrivedAtNode(currentNode);
            mapManager?.OnPlayerArrivedAtNode(currentNode);

            // If input is still held in a valid direction, keep moving
            // without playing idle first
            if (heldDirection != Vector2.zero) 
            {
                MapNode nextNode = currentNode.GetNodeInDirection(heldDirection);
                MapPath nextPath = currentNode.GetPathInDirection(heldDirection);
                if (nextNode != null && nextPath != null && nextNode.IsUnlocked()) {
                    // Keep walking — TryMove will be called next Update
                    // so just don't play idle
                    yield return null;
                }
            }

            PlayAnimation(AnimIdle);
        }

        // ── Path Length ───────────────────────────────────────────────────

        // Approximates the length of the path by sampling points along it
        private float CalculatePathLength(MapPath path, int samples = 50) {
            float length = 0f;
            Vector3 prev = path.GetPositionAtT(0f);
            for (int i = 1; i <= samples; i++) {
                float t = i / (float)samples;
                Vector3 next = path.GetPositionAtT(t);
                length += Vector3.Distance(prev, next);
                prev = next;
            }
            return length;
        }

        // ── Input Lock ────────────────────────────────────────────────────

        // Called by KingdomMapManager during unlock animations
        public void LockInput() {
            inputLocked = true;
            heldDirection = Vector2.zero;
        }

        public void UnlockInput() {
            inputLocked = false;
        }

        // ── Animation ─────────────────────────────────────────────────────

        private void PlayAnimation(int stateHash) {
            if (animator != null)
                animator.Play(stateHash);
        }

        private int GetWalkAnimation(Vector2 direction) {
            // Use whichever axis is dominant
            if (Mathf.Abs(direction.x) >= Mathf.Abs(direction.y)) {
                return direction.x > 0 ? AnimWalkRight : AnimWalkLeft;
            } else {
                return direction.y > 0 ? AnimWalkUp : AnimWalkDown;
            }
        }

        private Vector2 SnapToCardinal(Vector2 input) {
            if (Mathf.Abs(input.x) >= Mathf.Abs(input.y)) {
                return input.x > 0 ? Vector2.right : Vector2.left;
            } else {
                return input.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        // ── Unlock animation support ──────────────────────────────────────

        // Called by KingdomMapManager to move the player to a specific node
        // without input, used during the unlock animation sequence
        public IEnumerator MoveToNodeImmediate(MapNode targetNode, MapPath path) {
            isMoving = true;

            Vector2 toTarget = targetNode.transform.position
                             - currentNode.transform.position;
            PlayAnimation(GetWalkAnimation(toTarget));

            float pathLength = CalculatePathLength(path);
            float duration = pathLength / moveSpeed;
            float elapsed = 0f;

            while (elapsed < duration) {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = path.GetPositionAtT(t);
                yield return null;
            }

            transform.position = targetNode.transform.position;
            currentNode = targetNode;
            isMoving = false;
            PlayAnimation(AnimIdle);
        }

        public MapNode CurrentNode => currentNode;
    }
}
