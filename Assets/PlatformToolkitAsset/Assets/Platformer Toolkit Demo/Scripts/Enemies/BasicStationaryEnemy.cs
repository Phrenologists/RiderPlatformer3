using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class BasicStationaryEnemy : StationaryEnemy {

        // Animator parameter names as constants so typos are caught at compile time
        // rather than silently failing at runtime
        private static readonly int HitFromLeft = Animator.StringToHash("HitFromLeft");
        private static readonly int HitFromRight = Animator.StringToHash("HitFromRight");
        private static readonly int HitFromAbove = Animator.StringToHash("HitFromAbove");
        private static readonly int Defeated = Animator.StringToHash("Defeated");
        private static readonly int Idle = Animator.StringToHash("Idle");

        protected override void Awake() {
            base.Awake();

            // Play idle animation if one exists — fails silently if not
            if (animator != null)
                animator.Play(Idle);
        }

        protected override void OnContactAnimation(ContactDirection direction) {
            if (animator == null) return;

            switch (direction) {
                case ContactDirection.FromLeft:
                    animator.SetTrigger(HitFromLeft);
                    break;
                case ContactDirection.FromRight:
                    animator.SetTrigger(HitFromRight);
                    break;
                case ContactDirection.FromAbove:
                    animator.SetTrigger(HitFromAbove);
                    break;
            }
        }

        protected override void OnDefeatAnimation() {
            if (animator != null)
                animator.SetTrigger(Defeated);
        }
    }
}
