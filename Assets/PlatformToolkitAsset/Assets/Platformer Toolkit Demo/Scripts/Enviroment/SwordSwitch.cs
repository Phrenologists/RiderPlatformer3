// SwordSwitch.cs
using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class SwordSwitch : MonoBehaviour {

        [Header("Connected Platforms")]
        [SerializeField] private List<SwitchPlatform> connectedPlatforms
            = new List<SwitchPlatform>();
        // Drag any number of SwitchPlatform components here

        [Header("Activation Settings")]
        [SerializeField] private float activationDelay = 0.2f;
        // Delay between switch activation and platforms starting to move

        [Header("Visuals")]
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject activateParticlePrefab;
        [SerializeField] private GameObject deactivateParticlePrefab;
        [SerializeField] private AudioSource activateSound;
        [SerializeField] private AudioSource deactivateSound;

        // Animator hashes
        private static readonly int Activated = Animator.StringToHash("Activated");

        [Header("Events")]
        public UnityEvent onActivated = new UnityEvent();
        public UnityEvent onDeactivated = new UnityEvent();

        private bool isActive = false;

        // Called by SwordSlash via the Switch tag
        public void Activate() {
            isActive = !isActive;

            if (isActive) {
                OnActivate();
            } else {
                OnDeactivate();
            }
        }

        private void OnActivate() {
            if (animator != null)
                animator.SetBool(Activated, true);

            if (activateParticlePrefab != null)
                Instantiate(activateParticlePrefab,
                    transform.position, Quaternion.identity);

            if (activateSound != null)
                activateSound.Play();

            onActivated?.Invoke();

            StartCoroutine(ActivatePlatformsDelayed(true));
        }

        private void OnDeactivate() {
            if (animator != null)
                animator.SetBool(Activated, false);

            if (deactivateParticlePrefab != null)
                Instantiate(deactivateParticlePrefab,
                    transform.position, Quaternion.identity);

            if (deactivateSound != null)
                deactivateSound.Play();

            onDeactivated?.Invoke();

            StartCoroutine(ActivatePlatformsDelayed(false));
        }

        private IEnumerator ActivatePlatformsDelayed(bool activate) {
            yield return new WaitForSeconds(activationDelay);

            foreach (var platform in connectedPlatforms) {
                if (platform != null) {
                    if (activate) platform.MoveToTarget();
                    else platform.MoveToOrigin();
                }
            }
        }
    }
}
