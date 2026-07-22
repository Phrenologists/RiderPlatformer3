// KingdomMapManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class KingdomMapManager : MonoBehaviour, IMapManager {

        public static KingdomMapManager Instance { get; private set; }

        [Header("Kingdom Setup")]
        [SerializeField] private int kingdomIndex;
        // Set this in the Inspector for each kingdom map scene

        [Header("Components")]
        [SerializeField] private MapPlayerController playerController;
        [SerializeField] private MapCamera mapCamera;
        [SerializeField] private List<MapNode> allNodes = new List<MapNode>();
        // Drag all MapNode objects in this kingdom into this list

        [Header("UI")]
        [SerializeField] private LevelInfoPanel levelInfoPanel;
        [SerializeField] private BonusLevelPanel bonusLevelPanel;
        [SerializeField] private GlobalCollectiblePanel globalCollectiblePanel;

        [Header("Unlock Animation Settings")]
        [SerializeField] private float pauseAfterPathDrawn = 0.4f;
        [SerializeField] private float nodeAppearDuration = 0.3f;

        [Header("Panel Settings")]
        [SerializeField] private float panelShowDelay = 0.15f;

        private KingdomSaveData kingdomSave =>
            GameManager.Instance.SaveData.kingdoms[kingdomIndex];

        //public bool levelUnlocked = false;

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start() {
            InitialiseNodes();
            InitialisePlayer();
            globalCollectiblePanel.Refresh();

            // Play any pending unlock animations
            StartCoroutine(PlayPendingUnlockAnimations());
        }

        // ── Input ─────────────────────────────────────────────────────────
        
        public void OnMapConfirm(InputAction.CallbackContext context) {
            if (!context.started) return;
            OnConfirmAtNode(playerController.CurrentNode);
        }

        // Wire to a "MapBack" input action in the InputManager
        public void OnMapBack(InputAction.CallbackContext context) {
            if (!context.started) return;
            ReturnToWorldMap();
        }

        // ── Initialisation ────────────────────────────────────────────────

        private void InitialiseNodes() {
            foreach (var node in allNodes) {
                bool unlocked = node.IsUnlocked();
                node.RefreshVisuals();

                // Show paths that are already unlocked
                // A path leading TO a node should be visible if that node is unlocked
                ShowPathsForNode(node, unlocked);
            }
        }

        private void ShowPathsForNode(MapNode node, bool visible) {
            // Mark paths as revealed if their destination node is unlocked
            // We check all four directions on every node
            TryRevealPath(node.northPath, visible);
            TryRevealPath(node.southPath, visible);
            TryRevealPath(node.eastPath, visible);
            TryRevealPath(node.westPath, visible);
        }

        private void TryRevealPath(MapPath path, bool visible) {
            if (path == null) return;
            if (visible && !path.IsRevealed()) {
                path.SetRevealed(true);
            }
        }

        private void InitialisePlayer() {
            if (allNodes.Count == 0) {
                Debug.LogError("KingdomMapManager: allNodes list is empty.");
                return;
            }

            int startIndex;

            if (GameManager.Instance.KingdomEntryMode == GameManager.KingdomMapEntryMode.FromLevel) {
                // Returning from a level — restore the saved node position
                startIndex = GameManager.Instance.SaveData.lastLevelNodeIndex;
            } else {
                // Entering from world map — always start on first node
                startIndex = 0;
            }

            MapNode startNode = allNodes[Mathf.Clamp(startIndex, 0, allNodes.Count - 1)];
            playerController.Initialise(startNode);
            mapCamera.SnapToTarget();
        }

        // ── Node Arrival ──────────────────────────────────────────────────

        // Called by MapPlayerController when the player arrives at a node
        public void OnPlayerArrivedAtNode(MapNode node) {
            // Save the player's position on the map
            GameManager.Instance.SetLastMapPosition(kingdomIndex, allNodes.IndexOf(node));

            switch (node.nodeType) {
                case MapNodeType.Level:
                    StartCoroutine(ShowLevelInfoDelayed(node));
                    break;
                case MapNodeType.Bonus:
                    StartCoroutine(ShowLevelInfoDelayed(node));
                    break;
            }
        }

        // Called by MapPlayerController when the player leaves a node
        public void OnPlayerLeftNode(MapNode node) {
            levelInfoPanel.Hide();
            bonusLevelPanel.Hide();
        }

        private IEnumerator ShowLevelInfoDelayed(MapNode node) {
            yield return new WaitForSeconds(panelShowDelay);

            if (node.nodeType == MapNodeType.Level) {
                levelInfoPanel.Show(node, kingdomIndex);
            } else if (node.nodeType == MapNodeType.Bonus) {
                bonusLevelPanel.ShowPlaceholder();
            }
        }

        // ── Level Loading ─────────────────────────────────────────────────

        // Called when the player presses the confirm button on a level node
        public void OnConfirmAtNode(MapNode node) {
            if (node.nodeType == MapNodeType.Level && node.levelDefinition != null) {
                Debug.Log("OnConfirmAtNode");
                int levelIndex = allNodes.IndexOf(node);
                GameManager.Instance.EnterLevel(levelIndex);
                MusicManager.Instance.SetNextSceneMusic(
                    carryOver: false,
                    nextTrack: node.levelDefinition.levelMusic
                );
                SceneTransition.Instance.TransitionToScene(
                    node.levelDefinition.sceneName
                );
                //GameManager.Instance.StartLevel(kingdomIndex, levelIndex);
            }
        }

        // ── Unlock Animations ─────────────────────────────────────────────

        private IEnumerator PlayPendingUnlockAnimations()
        {
            //if (!levelUnlocked) yield break;
            playerController.LockInput();

            var save = GameManager.Instance.SaveData;
            bool anyPlayed = false;

            foreach (var node in allNodes) {
                if (node.nodeType != MapNodeType.Level) continue;

                string animKey = $"kingdom_{kingdomIndex}_level_{node.levelIndex}";
                //Debug.Log(animKey);
                bool alreadyPlayed = save.playedUnlockAnimations.Contains(animKey);
                //Debug.Log(alreadyPlayed);
                bool shouldPlay = node.IsUnlocked() && !alreadyPlayed;

                if (!shouldPlay) continue;

                anyPlayed = true;

                // Move player to the previous node (the one they just completed)
                // then play the reveal animation for the path to this node
                MapNode previousNode = FindPreviousNode(node);
                if (previousNode != null) {
                    MapPath pathToNode = GetPathBetween(previousNode, node);

                    // Walk player to the completed node
                    if (previousNode != playerController.CurrentNode) {
                        MapPath pathToPrevious = GetPathBetween(
                            playerController.CurrentNode, previousNode
                        );
                        if (pathToPrevious != null) {
                            yield return StartCoroutine(
                                playerController.MoveToNodeImmediate(previousNode, pathToPrevious)
                            );
                        }
                    }
                    // Pan camera to where the new node will appear
                    // before drawing the path, so the player can see it
                    yield return mapCamera
                        .PanToPosition(node.transform.position)
                        .WaitForCompletion();

                    // Draw the path to the new node
                    if (pathToNode != null) {
                        yield return StartCoroutine(pathToNode.PlayRevealAnimation());
                    }

                    // Brief pause after path is drawn
                    yield return new WaitForSeconds(pauseAfterPathDrawn);

                    // Make the new node appear
                    yield return StartCoroutine(RevealNode(node));
                }

                // Mark this animation as played
                save.playedUnlockAnimations.Add(animKey);
                GameManager.Instance.WriteToDisk();
            }

            if (anyPlayed) {
                // Pan camera back to the player after all animations finish
                mapCamera.PanToTarget();
                yield return new WaitForSeconds(mapCamera.PanDuration);
            }

            playerController.UnlockInput();
        }

        private IEnumerator RevealNode(MapNode node) {
            node.gameObject.SetActive(true);
            node.RefreshVisuals();

            // Simple scale-up appear animation
            node.transform.localScale = Vector3.zero;
            yield return node.transform
                .DOScale(Vector3.one, nodeAppearDuration)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }

        // Finds the node that the given node was unlocked by
        // (i.e. the previous node in the sequence)
        private MapNode FindPreviousNode(MapNode node) {
            foreach (var candidate in allNodes) {
                if (candidate.northNode == node) return candidate;
                if (candidate.southNode == node) return candidate;
                if (candidate.eastNode == node) return candidate;
                if (candidate.westNode == node) return candidate;
            }
            return null;
        }

        // Finds the path connecting two adjacent nodes
        private MapPath GetPathBetween(MapNode from, MapNode to) {
            if (from.northNode == to) return from.northPath;
            if (from.southNode == to) return from.southPath;
            if (from.eastNode == to) return from.eastPath;
            if (from.westNode == to) return from.westPath;
            return null;
        }

        // ── World Map ─────────────────────────────────────────────────────

        private void ReturnToWorldMap() {
            levelInfoPanel.Hide();
            bonusLevelPanel.Hide();
            MusicManager.Instance.SetNextSceneMusic(carryOver: false,
                nextTrack: null);
            // WorldMapManager will handle starting its own music
            SceneTransition.Instance.TransitionToScene("WorldMap");
        }
    }
}
