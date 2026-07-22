// MapCamera.cs
using UnityEngine;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class MapCamera : MonoBehaviour {

        [Header("Target")]
        [SerializeField] private Transform target;
        // Drag the map player character Transform here

        [Header("Follow Settings")]
        [SerializeField, Range(0f, 1f)] private float followSmoothTime = 0.15f;
        // Lower = snappier, higher = more lag

        [Header("Pan Settings")]
        [SerializeField] private float panDuration = 0.6f;
        [SerializeField] private Ease panEase = Ease.InOutQuad;

        public float PanDuration => panDuration;

        // Internal state
        private Vector3 velocity = Vector3.zero;
        private bool isFollowing = true;
        // When false, the camera is under manual control (during unlock animation)

        private float fixedZ;

        private void Awake() {
            fixedZ = transform.position.z;
        }

        private void LateUpdate() {
            if (!isFollowing || target == null) return;

            Vector3 targetPos = new Vector3(
                target.position.x,
                target.position.y,
                fixedZ
            );

            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPos,
                ref velocity,
                followSmoothTime
            );
        }

        // Instantly snap to the target — used on scene load
        public void SnapToTarget() {
            if (target == null) return;
            transform.position = new Vector3(
                target.position.x,
                target.position.y,
                fixedZ
            );
            velocity = Vector3.zero;
        }

        // Smooth pan to the player — called after unlock animation finishes
        public void PanToTarget() {
            if (target == null) return;
            isFollowing = false;

            Vector3 destination = new Vector3(
                target.position.x,
                target.position.y,
                fixedZ
            );

            transform.DOMove(destination, panDuration)
                .SetEase(panEase)
                .OnComplete(() => {
                    isFollowing = true;
                    velocity = Vector3.zero;
                });
        }

        // Pan to a specific world position — used during unlock animation
        // to move to where the next node will appear
        public Tween PanToPosition(Vector3 worldPosition) {
            isFollowing = false;
            velocity = Vector3.zero;

            Vector3 destination = new Vector3(
                worldPosition.x,
                worldPosition.y,
                fixedZ
            );

            return transform.DOMove(destination, panDuration).SetEase(panEase);
        }

        // Re-enable following without a pan — for cases where the camera
        // should just snap back immediately
        public void ResumeFollowing() {
            isFollowing = true;
            velocity = Vector3.zero;
        }
    }
}
