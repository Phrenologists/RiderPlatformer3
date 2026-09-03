// RotationDebugger.cs - revised approach
using UnityEngine;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    [ExecuteAlways]
    public class RotationDebugger : MonoBehaviour {

        [SerializeField] private bool lockRotation = false;
        [SerializeField] private float lockedAngle = 270f;
        // When lockRotation is true, this forces the rotation
        // and logs anything that tries to change it

        private Quaternion lastEndOfFrameRotation;
        private Vector3 lastEndOfFrameScale;

        // Log of recent changes for analysis
        private Queue<string> changeLog = new Queue<string>();
        private const int maxLogEntries = 10;

        private void Start() {
            lastEndOfFrameRotation = transform.rotation;
            lastEndOfFrameScale = transform.localScale;
        }

        private void LateUpdate() {
            // LateUpdate runs after all Update() calls
            // So if rotation changed, something in Update() did it

            bool rotationChanged = transform.rotation != lastEndOfFrameRotation;
            bool scaleChanged = transform.localScale != lastEndOfFrameScale;

            if (rotationChanged) {
                string msg = $"Frame {Time.frameCount}: Rotation " +
                    $"{lastEndOfFrameRotation.eulerAngles:F1} -> " +
                    $"{transform.rotation.eulerAngles:F1}";
                changeLog.Enqueue(msg);
                if (changeLog.Count > maxLogEntries)
                    changeLog.Dequeue();

                Debug.Log($"[RotationDebugger] {msg}", gameObject);

                if (lockRotation) {
                    // Force it back so we can see the jitter stopped
                    transform.rotation = Quaternion.Euler(0f, 0f, lockedAngle);
                }
            }

            if (scaleChanged) {
                string msg = $"Frame {Time.frameCount}: Scale " +
                    $"{lastEndOfFrameScale:F2} -> " +
                    $"{transform.localScale:F2}";
                changeLog.Enqueue(msg);
                if (changeLog.Count > maxLogEntries)
                    changeLog.Dequeue();

                Debug.Log($"[RotationDebugger] SCALE: {msg}", gameObject);
            }

            lastEndOfFrameRotation = transform.rotation;
            lastEndOfFrameScale = transform.localScale;
        }

        private void OnGUI() {
            if (!Application.isPlaying) return;
            GUI.color = Color.yellow;
            float y = 200f;
            GUI.Label(new Rect(10, y, 400, 20),
                $"Current rotation: {transform.rotation.eulerAngles:F1}");
            GUI.Label(new Rect(10, y + 20, 400, 20),
                $"Current scale: {transform.localScale:F2}");
            y += 50f;
            GUI.Label(new Rect(10, y, 400, 20), "Recent changes:");
            y += 20f;
            foreach (var entry in changeLog) {
                GUI.Label(new Rect(10, y, 500, 20), entry);
                y += 18f;
            }
        }
    }
}
