using UnityEngine;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class MovingDoor : MonoBehaviour {

        [Header("Settings")]
        [SerializeField] private Vector3 moveOffset = new Vector3(0f, 3f, 0f);
        // Direction and distance to move when triggered, relative to start position

        [SerializeField] private float moveDuration = 0.5f;
        [SerializeField] private Ease moveEase = Ease.OutQuad;

        private Vector3 closedPos;
        private Vector3 openPos;

        private void Awake() {
            closedPos = transform.position;
            openPos = closedPos + moveOffset;
        }

        public void Open() {
            transform.DOKill();
            transform.DOMove(openPos, moveDuration).SetEase(moveEase);
        }

        public void Close() {
            transform.DOKill();
            transform.DOMove(closedPos, moveDuration).SetEase(moveEase);
        }
    }
}
