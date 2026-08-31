// GuardingEnemy.cs - with debug tools added
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class GuardingEnemy : StationaryEnemy {

        public enum GuardMode {
            TimeWindow,
            Simultaneous
        }

        [Header("Guard Settings")]
        [SerializeField] private GuardMode guardMode = GuardMode.TimeWindow;
        [SerializeField] private float detectionRange = 8f;
        [SerializeField] private float attackTimeWindow = 0.5f;
        [SerializeField] private float simultaneousDistanceThreshold = 1.5f;
        [SerializeField] private float guardResetDelay = 2f;

        [Header("References")]
        //[SerializeField] private LayerMask playerLayer;
        [SerializeField] private LayerMask mountLayer;

        [Header("Debug")]
        [SerializeField] private bool showDebugLogs = true;
        [SerializeField] private bool showDebugGizmos = true;

        // State
        private float guardDirection = 1f;
        private bool guardBroken = false;
        private bool isResetting = false;

        // Time window tracking
        private bool receivedSlashHit = false;
        private bool receivedChargeHit = false;
        private float timeWindowCounter = 0f;
        private bool timeWindowActive = false;

        private Transform playerTransform;
        private Transform mountTransform;

        // Debug state for display
        private string lastBlockReason = "None";
        private float playerDistance = 0f;
        private float mountDistance = 0f;
        private float playerSide = 0f;
        private float mountSide = 0f;

        protected override void Awake() {
            base.Awake();

            var player = GameObject.FindWithTag("Player");
            if (player != null) {
                playerTransform = player.transform;
                Log($"Found player: {player.name}");
            } else {
                Debug.LogWarning("[GuardingEnemy] No GameObject tagged 'Player' found.");
            }

            var mount = GameObject.FindWithTag("Mount");
            if (mount != null) {
                mountTransform = mount.transform;
                Log($"Found mount: {mount.name}");
            } else {
                Debug.LogWarning("[GuardingEnemy] No GameObject tagged 'Mount' found.");
            }
        }

        private void Update() {
            if (guardBroken || isResetting) return;

            UpdateGuardDirection();

            if (guardMode == GuardMode.TimeWindow && timeWindowActive) {
                timeWindowCounter += Time.deltaTime;
                if (timeWindowCounter > attackTimeWindow) {
                    Log($"Time window expired after {timeWindowCounter:F2}s. " +
                        $"SlashHit: {receivedSlashHit}, ChargeHit: {receivedChargeHit}");
                    ResetTimeWindow();
                }
            }

            if (guardMode == GuardMode.Simultaneous) {
                CheckSimultaneousAttack();
            }
        }

        private void UpdateGuardDirection() {
            Transform closestThreat = GetClosestThreat();

            if (closestThreat == null) {
                lastBlockReason = "No threat in range";
                return;
            }

            float newDirection = closestThreat.position.x > transform.position.x
                ? 1f : -1f;

            if (newDirection != guardDirection) {
                Log($"Guard direction changed from {guardDirection} to {newDirection}. " +
                    $"Threat: {closestThreat.name} at {closestThreat.position}");
                guardDirection = newDirection;
                UpdateFacingDirection();
            }
        }

        private Transform GetClosestThreat() {
            playerDistance = playerTransform != null
                ? Vector2.Distance(transform.position, playerTransform.position)
                : float.MaxValue;
            mountDistance = mountTransform != null
                ? Vector2.Distance(transform.position, mountTransform.position)
                : float.MaxValue;

            bool playerInRange = playerDistance <= detectionRange;
            bool mountInRange = mountDistance <= detectionRange;

            if (!playerInRange && !mountInRange) {
                lastBlockReason = $"Both out of range. " +
                    $"Player: {playerDistance:F1}, Mount: {mountDistance:F1}, " +
                    $"Range: {detectionRange}";
                return null;
            }

            if (playerDistance < mountDistance && playerInRange)
                return playerTransform;
            if (mountInRange)
                return mountTransform;

            return null;
        }

        private void UpdateFacingDirection() {
            transform.localScale = new Vector3(guardDirection, 1f, 1f);
            if (animator != null)
                animator.SetFloat("GuardDirection", guardDirection);
        }

        // ── Hit Detection ─────────────────────────────────────────────────

        public override bool OnSlashHit(float slashDirection) {
            Log($"OnSlashHit called. slashDirection: {slashDirection}, " +
                $"guardDirection: {guardDirection}, guardBroken: {guardBroken}");

            if (guardBroken) {
                Log("Guard already broken - slash hits freely");
                return false;
            }

            bool blocked = IsAttackBlocked(slashDirection);
            Log($"Slash blocked: {blocked}. " +
                $"AttackSign: {Mathf.Sign(slashDirection)}, " +
                $"GuardSign: {Mathf.Sign(guardDirection)}");

            if (blocked) {
                lastBlockReason = $"Slash blocked. slashDir: {slashDirection:F1}, " +
                    $"guardDir: {guardDirection:F1}";
                return true;
            }

            if (guardMode == GuardMode.TimeWindow) {
                Log("Unguarded slash registered for time window");
                OnUnguardedSlash();
                return false;
            }
            return false;
        }

        public void OnChargeHit(float chargeDirection) {
            Log($"OnChargeHit called. chargeDirection: {chargeDirection}, " +
                $"guardDirection: {guardDirection}, guardBroken: {guardBroken}");

            if (guardBroken) {
                Log("Guard already broken - charge hits freely");
                var health = GetComponent<EnemyHealth>();
                health?.TakeDamage(AttackType.Charge, chargeDirection);
                return;
            }

            bool blocked = IsAttackBlocked(chargeDirection);
            Log($"Charge blocked: {blocked}");

            if (blocked) {
                lastBlockReason = $"Charge blocked. chargeDir: {chargeDirection:F1}, " +
                    $"guardDir: {guardDirection:F1}";
                return;
            }

            if (guardMode == GuardMode.TimeWindow) {
                Log("Unguarded charge registered for time window");
                OnUnguardedCharge();
            }
        }

        private void OnUnguardedSlash() {
            receivedSlashHit = true;
            if (!timeWindowActive) {
                timeWindowActive = true;
                timeWindowCounter = 0f;
                Log("Time window started by slash");
            }
            CheckTimeWindowComplete();
        }

        private void OnUnguardedCharge() {
            receivedChargeHit = true;
            if (!timeWindowActive) {
                timeWindowActive = true;
                timeWindowCounter = 0f;
                Log("Time window started by charge");
            }
            CheckTimeWindowComplete();
        }

        private bool IsAttackBlocked(float attackDirection) {
            // Guard faces guardDirection
            // An attack from the right has a negative attackDirection (going left)
            // Guard facing right (guardDirection = 1) blocks attacks from the right
            // i.e. blocks when attackDirection < 0 and guardDirection > 0
            // i.e. blocks when their signs are OPPOSITE
            bool blocked = Mathf.Sign(attackDirection) != Mathf.Sign(guardDirection);
            return blocked;
        }

        // ── Time Window ───────────────────────────────────────────────────

        private void CheckTimeWindowComplete() {
            Log($"Checking time window. Slash: {receivedSlashHit}, " +
                $"Charge: {receivedChargeHit}");
            if (receivedSlashHit && receivedChargeHit) {
                Log("Both hits received - breaking guard!");
                BreakGuard();
            }
        }

        private void ResetTimeWindow() {
            receivedSlashHit = false;
            receivedChargeHit = false;
            timeWindowActive = false;
            timeWindowCounter = 0f;
        }

        // ── Simultaneous ──────────────────────────────────────────────────

        private void CheckSimultaneousAttack() {
            if (playerTransform == null || mountTransform == null) return;

            playerSide = playerTransform.position.x > transform.position.x ? 1f : -1f;
            mountSide = mountTransform.position.x > transform.position.x ? 1f : -1f;

            playerDistance = Vector2.Distance(
                transform.position, playerTransform.position);
            mountDistance = Vector2.Distance(
                transform.position, mountTransform.position);

            bool oppositeSides = playerSide != mountSide;
            bool playerClose = playerDistance <= simultaneousDistanceThreshold;
            bool mountClose = mountDistance <= simultaneousDistanceThreshold;

            if (showDebugLogs && Time.frameCount % 30 == 0) {
                Log($"Simultaneous check - PlayerSide: {playerSide}, " +
                    $"MountSide: {mountSide}, OppositeSides: {oppositeSides}, " +
                    $"PlayerDist: {playerDistance:F1}, MountDist: {mountDistance:F1}, " +
                    $"PlayerClose: {playerClose}, MountClose: {mountClose}");
            }

            if (oppositeSides && playerClose && mountClose) {
                Log("Simultaneous attack condition met - breaking guard!");
                BreakGuard();
            }
        }

        // ── Guard Break ───────────────────────────────────────────────────

        private void BreakGuard() {
            if (guardBroken) return;
            guardBroken = true;

            Log($"Guard broken! Mode: {guardMode}");

            if (animator != null)
                animator.SetTrigger("GuardBroken");

            var health = GetComponent<EnemyHealth>();
            if (health != null) {
                health.TakeDamage(AttackType.Slash, 0f);
                Log($"Dealt damage. IsDead: {health.IsDead}");
            } else {
                Debug.LogWarning("[GuardingEnemy] No EnemyHealth component found!");
            }

            if (!GetComponent<EnemyHealth>().IsDead) {
                StartCoroutine(ResetGuardAfterDelay());
            }
        }

        private IEnumerator ResetGuardAfterDelay() {
            isResetting = true;
            Log($"Guard resetting in {guardResetDelay}s");
            yield return new WaitForSeconds(guardResetDelay);

            guardBroken = false;
            isResetting = false;
            ResetTimeWindow();

            Log("Guard reset complete");

            if (animator != null)
                animator.SetTrigger("GuardReset");

            UpdateFacingDirection();
        }

        // ── Debug ─────────────────────────────────────────────────────────

        private void Log(string message) {
            if (showDebugLogs) {
                //Debug.Log($"[GuardingEnemy:{gameObject.name}] {message}");
            }
        }

        private void OnDrawGizmos() {
            if (!showDebugGizmos) return;

            // Detection range
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            // Simultaneous threshold
            if (guardMode == GuardMode.Simultaneous) {
                Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
                Gizmos.DrawWireSphere(transform.position,
                    simultaneousDistanceThreshold);
            }

            // Guard direction indicator
            Gizmos.color = guardBroken
                ? Color.red
                : new Color(0f, 0.8f, 1f, 0.9f);
            Vector3 guardIndicator = transform.position
                + new Vector3(guardDirection * 0.8f, 0f, 0f);
            Gizmos.DrawLine(transform.position, guardIndicator);
            Gizmos.DrawSphere(guardIndicator, 0.1f);

            // Time window progress (shown as a small bar above enemy)
            if (timeWindowActive) {
                float progress = timeWindowCounter / attackTimeWindow;
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(
                    transform.position + Vector3.up * 1.2f,
                    transform.position + Vector3.up * 1.2f
                        + Vector3.right * progress
                );
            }

            // Last block reason label position marker
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(transform.position + Vector3.up * 1.5f, 0.05f);
        }

        // Show debug state in editor
        private void OnGUI() {
            if (!showDebugGizmos) return;
            if (Camera.main == null) return;

            Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position);
            if (screenPos.z < 0) return;

            // Flip Y for GUI coordinates
            screenPos.y = Screen.height - screenPos.y;

            GUI.color = Color.yellow;
            GUI.Label(new Rect(screenPos.x - 100f, screenPos.y - 80f, 220f, 100f),
                $"Guard: {(guardBroken ? "BROKEN" : "ACTIVE")}\n" +
                $"Facing: {(guardDirection > 0 ? "Right" : "Left")}\n" +
                $"Slash hit: {receivedSlashHit}\n" +
                $"Charge hit: {receivedChargeHit}\n" +
                $"Last block: {lastBlockReason}"
            );
        }
    }
}
