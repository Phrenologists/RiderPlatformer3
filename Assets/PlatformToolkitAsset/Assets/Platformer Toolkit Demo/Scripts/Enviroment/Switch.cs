// Switch.cs - basic placeholder
using UnityEngine;
using UnityEngine.Events;

namespace GMTK.PlatformerToolkit {

    public class Switch : MonoBehaviour {

        [Header("Settings")]
        [SerializeField] private bool toggleable = true;
        // If true, hitting it again deactivates it

        [Header("Events")]
        public UnityEvent onActivated = new UnityEvent();
        public UnityEvent onDeactivated = new UnityEvent();

        [Header("Visuals")]
        [SerializeField] private Animator animator;
        private static readonly int Activated =
            Animator.StringToHash("Activated");

        private bool isActive = false;

        public void Activate() {
            if (!toggleable && isActive) return;

            isActive = !isActive;

            if (isActive) {
                onActivated?.Invoke();
                if (animator != null) animator.SetBool(Activated, true);
            } else {
                onDeactivated?.Invoke();
                if (animator != null) animator.SetBool(Activated, false);
            }
        }
    }
}
