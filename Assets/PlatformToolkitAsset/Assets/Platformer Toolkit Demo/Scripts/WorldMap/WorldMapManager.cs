// WorldMapManager.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

namespace GMTK.PlatformerToolkit {

    public class WorldMapManager : MonoBehaviour, IMapManager {

        public static WorldMapManager Instance { get; private set; }

        [Header("Components")]
        [SerializeField] private MapPlayerController playerController;
        [SerializeField] private MapCamera mapCamera;
        [SerializeField] private List<MapNode> allNodes = new List<MapNode>();

        [Header("UI")]
        [SerializeField] private KingdomInfoPanel kingdomInfoPanel;
        [SerializeField] private GlobalCollectiblePanel globalCollectiblePanel;

        [Header("Audio")]
        [SerializeField] private AudioClip worldMapMusic;

        [Header("Unlock Animation Settings")]
        [SerializeField] private float pauseAfterPathDrawn = 0.4f;
        [SerializeField] private float nodeAppearDuration = 0.3f;

        [Header("Panel Settings")]
        [SerializeField] private float panelShowDelay = 0.15f;
        
        

        private void Awake() {
            if (Instance != null && Instance != this) {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start() {
            // Start world map music only if it wasn't carried over
            if (!MusicManager.Instance.MusicWasCarriedIn) {
                MusicManager.Instance.PlayTrack(worldMapMusic);
            }

            InitialiseNodes();
            InitialisePlayer();
            globalCollectiblePanel.Refresh();

            StartCoroutine(PlayPendingUnlockAnimations());
        }

        // ── Initialisation ────────────────────────────────────────────────

        private void InitialiseNodes() {
            foreach (var node in allNodes) {
                node.RefreshVisuals();
                TryRevealPathsForNode(node);

                // Hide nodes that aren't unlocked yet so they
                // can be revealed by the unlock animation
                if (!node.IsUnlocked()) {
                    node.gameObject.SetActive(false);
                }
            }
        }

        private void TryRevealPathsForNode(MapNode node) {
            TryRevealPath(node.northPath, node.IsUnlocked());
            TryRevealPath(node.southPath, node.IsUnlocked());
            TryRevealPath(node.eastPath, node.IsUnlocked());
            TryRevealPath(node.westPath, node.IsUnlocked());
        }

        private void TryRevealPath(MapPath path, bool visible) {
            if (path == null) return;
            if (visible && !path.IsRevealed()) {
                path.SetRevealed(true);
            }
        }

        private void InitialisePlayer() {
            int lastKingdomIndex = GameManager.Instance.SaveData.lastKingdomIndex;
            MapNode startNode = allNodes[
                Mathf.Clamp(lastKingdomIndex, 0, allNodes.Count - 1)
            ];
            playerController.Initialise(startNode);
            mapCamera.SnapToTarget();
        }

        // ── Input ─────────────────────────────────────────────────────────

        // Wire to confirm input action in InputManager
        public void OnConfirm(InputAction.CallbackContext context) {
            if (!context.started) return;
            OnConfirmAtNode(playerController.CurrentNode);
        }

        // ── Node Arrival ──────────────────────────────────────────────────

        public void OnPlayerArrivedAtNode(MapNode node) {
            // Save position — on the world map we track by kingdom index
            GameManager.Instance.SetLastMapPosition(
                node.kingdomIndex,
                GameManager.Instance.SaveData.lastLevelNodeIndex
            );

            if (node.nodeType == MapNodeType.Kingdom) {
                StartCoroutine(ShowKingdomInfoDelayed(node));
            }
        }

        public void OnPlayerLeftNode(MapNode node) {
            kingdomInfoPanel.Hide();
        }

        private IEnumerator ShowKingdomInfoDelayed(MapNode node) {
            yield return new WaitForSeconds(panelShowDelay);
            kingdomInfoPanel.Show(node.kingdomIndex);
        }

        // ── Kingdom Loading ───────────────────────────────────────────────

        public void OnConfirmAtNode(MapNode node) {
            if (node.nodeType != MapNodeType.Kingdom) return;
            
            GameManager.Instance.EnterKingdom(node.kingdomIndex);
            GameManager.Instance.KingdomEntryMode = GameManager.KingdomMapEntryMode.FromWorldMap;

            var kingdomDef = GameManager.Instance.GetKingdomDefinition(node.kingdomIndex);
            kingdomInfoPanel.Hide();

            // World map never carries music to kingdom map —
            // each kingdom map manages its own music
            MusicManager.Instance.SetNextSceneMusic(carryOver: false, nextTrack: null);

            SceneTransition.Instance.TransitionToScene(kingdomDef.kingdomMapSceneName);
        }

        // ── Unlock Animations ─────────────────────────────────────────────

        private IEnumerator PlayPendingUnlockAnimations() {
            playerController.LockInput();

            var save = GameManager.Instance.SaveData;
            bool anyPlayed = false;

            foreach (var node in allNodes) {
                if (node.nodeType != MapNodeType.Kingdom) continue;

                string animKey = $"kingdom_{node.kingdomIndex}";
                bool alreadyPlayed = save.playedUnlockAnimations.Contains(animKey);
                bool shouldPlay = node.IsUnlocked()
                    && !alreadyPlayed
                    && node.kingdomIndex > 0;
                // Kingdom 0 starts unlocked and never needs an animation

                if (!shouldPlay) continue;

                anyPlayed = true;

                MapNode previousNode = FindPreviousNode(node);
                if (previousNode != null) {
                    MapPath pathToNode = GetPathBetween(previousNode, node);

                    // Walk player to the previously completed kingdom node
                    if (previousNode != playerController.CurrentNode) {
                        MapPath pathToPrevious = GetPathBetween(
                            playerController.CurrentNode, previousNode
                        );
                        if (pathToPrevious != null) {
                            yield return StartCoroutine(
                                playerController.MoveToNodeImmediate(
                                    previousNode, pathToPrevious)
                            );
                        }
                    }

                    // Pan to where the new kingdom node will appear
                    yield return mapCamera
                        .PanToPosition(node.transform.position)
                        .WaitForCompletion();

                    // Draw the path
                    if (pathToNode != null) {
                        yield return StartCoroutine(
                            pathToNode.PlayRevealAnimation()
                        );
                    }

                    // Pause after path is drawn
                    yield return new WaitForSeconds(pauseAfterPathDrawn);

                    // Reveal the new kingdom node
                    yield return StartCoroutine(RevealNode(node));
                }

                save.playedUnlockAnimations.Add(animKey);
                GameManager.Instance.WriteToDisk();
            }

            if (anyPlayed) {
                mapCamera.PanToTarget();
                yield return new WaitForSeconds(mapCamera.PanDuration);
            }

            playerController.UnlockInput();
        }

        private IEnumerator RevealNode(MapNode node) {
            node.gameObject.SetActive(true);
            node.RefreshVisuals();

            node.transform.localScale = Vector3.zero;
            yield return node.transform
                .DOScale(Vector3.one, nodeAppearDuration)
                .SetEase(Ease.OutBack)
                .WaitForCompletion();
        }

        private MapNode FindPreviousNode(MapNode node) {
            foreach (var candidate in allNodes) {
                if (candidate.northNode == node) return candidate;
                if (candidate.southNode == node) return candidate;
                if (candidate.eastNode == node) return candidate;
                if (candidate.westNode == node) return candidate;
            }
            return null;
        }

        private MapPath GetPathBetween(MapNode from, MapNode to) {
            if (from.northNode == to) return from.northPath;
            if (from.southNode == to) return from.southPath;
            if (from.eastNode == to) return from.eastPath;
            if (from.westNode == to) return from.westPath;
            return null;
        }
        
    }
}
