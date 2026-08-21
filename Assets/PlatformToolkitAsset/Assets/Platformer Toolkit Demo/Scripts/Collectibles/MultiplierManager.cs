// MultiplierManager.cs
using UnityEngine;
using System.Collections;

namespace GMTK.PlatformerToolkit {

    public class MultiplierManager : MonoBehaviour {

        public static MultiplierManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float multiplierValue = 2f;
        // How much each small collectible counts for while active

        public bool IsActive { get; private set; } = false;
        public float RemainingTime { get; private set; } = 0f;
        public float MaxDuration { get; private set; } = 0f;

        private Coroutine activeCoroutine;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ActivateMultiplier(float duration) {
            // If already active, restart — don't exceed max duration
            if (activeCoroutine != null)
                StopCoroutine(activeCoroutine);

            MaxDuration = duration;
            RemainingTime = duration;
            IsActive = true;

            MultiplierUI.Instance?.Show();
            activeCoroutine = StartCoroutine(CountDown());
        }

        private IEnumerator CountDown() {
            while (RemainingTime > 0f) {
                RemainingTime -= Time.deltaTime;
                MultiplierUI.Instance?.UpdateTimer(RemainingTime, MaxDuration);
                yield return null;
            }

            RemainingTime = 0f;
            IsActive = false;
            activeCoroutine = null;
            MultiplierUI.Instance?.Hide();
        }

        // Returns how much a single small collectible is worth right now
        public int GetCollectibleValue() {
            return IsActive ? Mathf.RoundToInt(multiplierValue) : 1;
        }
    }
}
