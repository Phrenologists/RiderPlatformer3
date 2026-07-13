// LevelManager.cs
using UnityEngine;

namespace GMTK.PlatformerToolkit
{

    // Sits in each level scene. Knows level-specific things
    // like total collectible counts and provides the level end trigger.
    public class LevelManager : MonoBehaviour
    {

        public static LevelManager Instance { get; private set; }

        [Header("Level Settings")] [SerializeField]
        private int totalBigCollectibles;

        public int TotalBigCollectibles => totalBigCollectibles;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {

            if (GameManager.Instance.Session == null)
            {
                // Find which kingdom/level index this scene corresponds to,
                // or just use 0,0 as a placeholder for testing
                GameManager.Instance.CreateTestSession();
                // Reset the UI to zero on level load
                CollectibleUI.Instance.Initialise(totalBigCollectibles);

            }
        }
    }
}
