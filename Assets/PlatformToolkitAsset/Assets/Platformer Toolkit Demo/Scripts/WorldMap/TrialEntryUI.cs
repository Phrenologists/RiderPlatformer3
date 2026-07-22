// TrialEntryUI.cs
using UnityEngine;
using TMPro;

namespace GMTK.PlatformerToolkit {

    // Attach to the trial entry prefab
    public class TrialEntryUI : MonoBehaviour {

        [SerializeField] private TextMeshProUGUI trialNameText;
        [SerializeField] private TextMeshProUGUI timeText;

        public void Initialise(TrialType trialType, string timeDisplay) {
            trialNameText.text = GetTrialName(trialType);
            timeText.text = timeDisplay;
        }

        private string GetTrialName(TrialType trialType) {
            switch (trialType) {
                case TrialType.SpeedRun:
                    return "Speed Run";
                case TrialType.SpeedRunCollect:
                    return "Speed + Collectibles";
                case TrialType.BigCollect:
                    return "Big Collectibles";
                case TrialType.FullCollect:
                    return "Full Clear";
                default:
                    return trialType.ToString();
            }
        }
    }
}
