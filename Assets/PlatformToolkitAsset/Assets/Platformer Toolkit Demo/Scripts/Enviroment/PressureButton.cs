using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

namespace GMTK.PlatformerToolkit {

    public class PressureButton : MonoBehaviour {

        [Header("Components")]
        [SerializeField] private Transform buttonVisual;
        // The visual part of the button that moves down when pressed

        [Header("Settings")]
        [SerializeField] private float pressDepth = 0.15f;
        [SerializeField] private float pressDuration = 0.1f;
        [SerializeField] private LayerMask characterLayer;
        // Set this to whichever layer(s) your player and mount are on

        [Header("Events")]
        public UnityEvent onPressed = new UnityEvent();
        public UnityEvent onReleased = new UnityEvent();

        private Vector3 originalLocalPos;
        private HashSet<GameObject> contacts = new HashSet<GameObject>();
        private bool isPressed = false;

        private void Awake() {
            originalLocalPos = buttonVisual.localPosition;
        }
        
        private void Update() {
            contacts.RemoveWhere(go => go == null);
            if (contacts.Count == 0 && isPressed) {
                Release();
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!IsCharacterLayer(collision.gameObject.layer)) return;
            if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
            {
                contacts.Add(collision.gameObject);
                if (contacts.Count == 1)
                {
                    Press();
                }
            }
        }

        private void OnCollisionExit2D(Collision2D collision) {
            if (!IsCharacterLayer(collision.gameObject.layer)) return;
            contacts.Remove(collision.gameObject);
            if (contacts.Count == 0) {
                Release();
            }
        }

        private bool IsCharacterLayer(int layer) {
            return (characterLayer.value & (1 << layer)) != 0;
        }

        private void Press() {
            if (isPressed) return;
            isPressed = true;

            buttonVisual.DOKill();
            buttonVisual.DOLocalMoveY(originalLocalPos.y - pressDepth, pressDuration)
                .SetEase(Ease.OutQuad);

            onPressed?.Invoke();
        }

        private void Release() {
            if (!isPressed) return;
            isPressed = false;

            buttonVisual.DOKill();
            buttonVisual.DOLocalMoveY(originalLocalPos.y, pressDuration)
                .SetEase(Ease.OutQuad);

            onReleased?.Invoke();
        }
    }
}
