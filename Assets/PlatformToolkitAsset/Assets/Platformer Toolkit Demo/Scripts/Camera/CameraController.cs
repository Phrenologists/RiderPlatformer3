// CameraController.cs - revised
using UnityEngine;
using Cinemachine;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class CameraController : MonoBehaviour {

        public static CameraController Instance { get; private set; }

        [Header("Cinemachine")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        private CinemachineFramingTransposer transposer;

        [Header("Horizontal Settings")]
        [SerializeField] private float horizontalDeadZoneWidth = 0.1f;
        [SerializeField] private float lookAheadTime = 0.2f;
        [SerializeField] private float lookAheadSmoothing = 5f;

        [Header("Vertical Settings")]
        [SerializeField] private bool freeVerticalFollow = false;
        [SerializeField] private float verticalDeadZoneHeight = 0.25f;
        [SerializeField] private float verticalDamping = 1f;

        [Header("Smoothing")]
        [SerializeField] private float horizontalDamping = 0.5f;
        [SerializeField] private float zoomDamping = 0.5f;

        [Header("Transition Settings")]
        [SerializeField] private float transitionDuration = 0.6f;
        [SerializeField] private Ease transitionEase = Ease.InOutQuad;

        private Transform playerTransform;
        // A hidden transform we move to act as a static follow target
        // when locking the camera position
        private Transform staticFollowPoint;

        private float defaultOrthographicSize;
        private float lockedYPosition;

        private Tweener yTween;
        private Tweener zoomTween;
        private Tweener positionTween;
        
        private bool isPositionLocked = false;
        
        

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            transposer = virtualCamera
                .GetCinemachineComponent<CinemachineFramingTransposer>();

            // Create the static follow point — a hidden transform we control
            var staticPoint = new GameObject("CameraStaticFollowPoint");
            staticFollowPoint = staticPoint.transform;
            DontDestroyOnLoad(staticPoint);
        }

        private void Start() {
            playerTransform = virtualCamera.Follow;
            defaultOrthographicSize = virtualCamera.m_Lens.OrthographicSize;
            lockedYPosition = playerTransform.position.y;
            ApplySettings();
        }
        private void LateUpdate() {
            if (Time.frameCount % 60 == 0) { // log once per second
                //Debug.Log($"[Camera] State - isPositionLocked: {isPositionLocked}, " +
                          //$"Follow target: {virtualCamera.Follow?.name}, " +
                          //$"Static point pos: {staticFollowPoint.position}");
            }
        }

        public void LockToPosition(Transform target, float zoomSize = 0f,
            float duration = -1f)
        {
            Debug.Log($"[Camera] LockToPosition called. Target: {target.name}");

            float d = duration < 0 ? transitionDuration : duration;

            // Start the static point at the camera's current world position
            if (virtualCamera.Follow == playerTransform)
            {
                staticFollowPoint.position = new Vector3(
                    virtualCamera.State.FinalPosition.x,
                    virtualCamera.State.FinalPosition.y,
                    staticFollowPoint.position.z
                );
            }
            virtualCamera.Follow = staticFollowPoint;

            positionTween?.Kill();
            positionTween = staticFollowPoint
                .DOMove(new Vector3(target.position.x, target.position.y,
                    staticFollowPoint.position.z), d)
                .SetEase(transitionEase)
                .OnComplete(() => {
                    // Once the pan is done, zero out ALL damping so the camera
                    // holds perfectly still
                    isPositionLocked = true;
                    Debug.Log("[Camera] Pan complete - camera now fully locked");
                    transposer.m_XDamping = 0f;
                    transposer.m_YDamping = 0f;
                    transposer.m_ZDamping = 0f;
                    transposer.m_LookaheadTime = 0f;
                });

            if (zoomSize > 0f) {
                SetZoom(zoomSize, d);
            }
        }
        

        private void ApplySettings() {
            transposer.m_LookaheadTime = lookAheadTime;
            transposer.m_LookaheadSmoothing = lookAheadSmoothing;
            transposer.m_XDamping = horizontalDamping;
            transposer.m_YDamping = freeVerticalFollow ? verticalDamping : 0f;
            transposer.m_ZDamping = zoomDamping;
            transposer.m_DeadZoneWidth = horizontalDeadZoneWidth;
            transposer.m_DeadZoneHeight = freeVerticalFollow
                ? verticalDeadZoneHeight
                : 0f;
        }

        // ── Follow Player ─────────────────────────────────────────────────

        // Restore the camera to following the player
        private void ReturnToPlayerFollow() {
            Debug.Log($"[Camera] ReturnToPlayerFollow called from:\n" +
                      System.Environment.StackTrace);
            isPositionLocked = false;
            positionTween?.Kill();
            virtualCamera.Follow = playerTransform;
            transposer.m_ScreenX = 0.5f;
            transposer.m_ScreenY = 0.5f;
            // Restore settings
            ApplySettings();
        }

        // ── Lock Position ─────────────────────────────────────────────────

        // Pan the camera to a specific world XY and lock it there
        public void LockToPosition(Vector2 worldPosition, float zoomSize = 0f,
            float duration = -1f) {

            float d = duration < 0 ? transitionDuration : duration;

            // Place the static point at the camera's current world position
            // so the pan starts from where the camera currently is
            if (virtualCamera.Follow == playerTransform) {
                staticFollowPoint.position = new Vector3(
                    virtualCamera.State.FinalPosition.x,
                    virtualCamera.State.FinalPosition.y,
                    staticFollowPoint.position.z
                );
            }

            // Switch follow target to the static point
            virtualCamera.Follow = staticFollowPoint;

            // Kill any existing position tween
            positionTween?.Kill();

            // Tween the static point to the desired world position
            // Cinemachine will follow it smoothly
            positionTween = staticFollowPoint
                .DOMove(new Vector3(worldPosition.x, worldPosition.y,
                    staticFollowPoint.position.z), d)
                .SetEase(transitionEase);

            if (zoomSize > 0f) {
                SetZoom(zoomSize, d);
            }
        }

        // ── Lock Y Only ───────────────────────────────────────────────────

        public void SetLockedYPosition(float worldY, float duration = -1f) {
            freeVerticalFollow = false;
            lockedYPosition = worldY;
            ApplySettings();

            float d = duration < 0 ? transitionDuration : duration;
            PanScreenY(WorldYToScreenY(worldY), d);
        }

        // ── Free Vertical Follow ──────────────────────────────────────────

        public void SetFreeVerticalFollow(bool free) {
            // If we were locked to a position, restore player follow first
            Debug.Log($"[Camera] SetFreeVerticalFollow({free}) called from:\n" +
                      System.Environment.StackTrace);
            if (virtualCamera.Follow != playerTransform) {
                ReturnToPlayerFollow();
            }

            freeVerticalFollow = free;
            ApplySettings();

            if (free) {
                PanScreenY(0.5f, transitionDuration);
            }
        }

        // ── Immediate Follow ──────────────────────────────────────────────

        public void SetImmediateFollow() {
            if (virtualCamera.Follow != playerTransform) {
                ReturnToPlayerFollow();
            }

            horizontalDeadZoneWidth = 0f;
            verticalDeadZoneHeight = 0f;
            ApplySettings();
        }

        // ── Zoom ──────────────────────────────────────────────────────────

        public void SetZoom(float orthographicSize, float duration = -1f) {
            float d = duration < 0 ? transitionDuration : duration;
            zoomTween?.Kill();
            zoomTween = DOTween.To(
                () => virtualCamera.m_Lens.OrthographicSize,
                v => {
                    var lens = virtualCamera.m_Lens;
                    lens.OrthographicSize = v;
                    virtualCamera.m_Lens = lens;
                },
                orthographicSize,
                d
            ).SetEase(transitionEase);
        }

        public void ResetZoom(float duration = -1f) {
            SetZoom(defaultOrthographicSize, duration);
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private float WorldYToScreenY(float worldY) {
            if (playerTransform == null) return 0.5f;
            float playerY = playerTransform.position.y;
            float screenY = 0.5f + (worldY - playerY)
                / (virtualCamera.m_Lens.OrthographicSize * 2f);
            return Mathf.Clamp(screenY, 0.1f, 0.9f);
        }

        private void PanScreenY(float targetScreenY, float duration) {
            yTween?.Kill();
            yTween = DOTween.To(
                () => transposer.m_ScreenY,
                v => transposer.m_ScreenY = v,
                targetScreenY,
                duration
            ).SetEase(transitionEase);
        }
        
    }
}
