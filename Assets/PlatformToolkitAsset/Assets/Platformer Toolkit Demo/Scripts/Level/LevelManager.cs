// LevelManager.cs
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GMTK.PlatformerToolkit
{

    // Sits in each level scene. Knows level-specific things
    // like total collectible counts and provides the level end trigger.
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private AudioClip levelMusic;
        
        [SerializeField] private LevelDefinition levelDefinition;

        public static LevelManager Instance { get; private set; }

        [Header("Level Settings")] [SerializeField]
        private int totalBigCollectibles;
        
        public bool MusicWasCarriedIn { get; private set; } = false;

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
            GameManager.Instance.StartLevel(GameManager.Instance.CurrentKingdomIndex, GameManager.Instance.CurrentLevelIndex);

            if (GameManager.Instance.Session == null)
            {
                // Find which kingdom/level index this scene corresponds to,
                // or just use 0,0 as a placeholder for testing
                //GameManager.Instance.CreateTestSession();
                //GameManager.Instance.StartLevel(GameManager.Instance.CurrentKingdomIndex, GameManager.Instance.CurrentLevelIndex);
                // Reset the UI to zero on level load
                

            }
            if (levelMusic != null && !MusicManager.Instance.MusicWasCarriedIn) {
                MusicManager.Instance.PlayTrack(levelMusic);
            }
            CollectibleUI.Instance.Initialise(totalBigCollectibles);
        }
        
        
    }
    
}
