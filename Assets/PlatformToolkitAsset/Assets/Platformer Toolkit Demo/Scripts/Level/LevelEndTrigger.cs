// LevelEndTrigger.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit {

    public class LevelEndTrigger : MonoBehaviour {

        private bool triggered = false;

        private void OnTriggerEnter2D(Collider2D other) {
            if (triggered) return;
            if (other.GetComponent<characterMovement>() == null) return;

            triggered = true;
            GameManager.Instance.CompleteLevel();
        }
    }
}
