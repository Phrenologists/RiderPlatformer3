// EnemySpawner.cs - rewritten
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace GMTK.PlatformerToolkit {

    public class EnemySpawner : MonoBehaviour {

        [Header("Spawning")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int maxEnemies = 3;
        [SerializeField] private float spawnDelay = 2f;
        [SerializeField] private float respawnDelay = 0.5f;

        [Header("Spawn Point")]
        [SerializeField] private Transform spawnPoint;

        // Simple list of all enemies this spawner has created
        // that are still alive
        private List<GameObject> activeEnemies = new List<GameObject>();
        private bool isDestroyed = false;

        private void Start() {
            StartCoroutine(SpawnRoutine());
        }

        private void Update() {
            var health = GetComponent<EnemyHealth>();
            if (health != null && health.IsDead && !isDestroyed) {
                isDestroyed = true;
                StopAllCoroutines();
                Debug.Log("[Spawner] Spawner destroyed - stopping all spawns");
            }
        }

        private IEnumerator SpawnRoutine() {
            // Initial spawn fills up to max immediately on start
            while (!isDestroyed && activeEnemies.Count < maxEnemies) {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }

            // After initial fill, just watch for deaths and respawn
        }

        private void SpawnEnemy() {
            if (enemyPrefab == null || isDestroyed) return;

            Vector3 pos = spawnPoint != null
                ? spawnPoint.position
                : transform.position;

            var enemy = Instantiate(enemyPrefab, pos, Quaternion.identity);
            activeEnemies.Add(enemy);

            //Debug.Log($"[Spawner] Spawned enemy. Active count: {activeEnemies.Count}/{maxEnemies}");

            StartCoroutine(WatchEnemy(enemy));
        }

        private IEnumerator WatchEnemy(GameObject enemy) {
            while (enemy != null) {
                yield return new WaitForSeconds(0.1f);
            }

            // Enemy was destroyed
            activeEnemies.Remove(enemy);
            activeEnemies.RemoveAll(e => e == null);

            Debug.Log($"[Spawner] Enemy died. Active count: {activeEnemies.Count}/{maxEnemies}");

            if (isDestroyed) yield break;

            yield return new WaitForSeconds(respawnDelay);

            if (!isDestroyed && activeEnemies.Count < maxEnemies) {
                SpawnEnemy();
            }
        }

        private void OnDrawGizmos() {
            if (spawnPoint != null) {
                Gizmos.color = new Color(1f, 0.6f, 0f, 0.9f);
                Gizmos.DrawSphere(spawnPoint.position, 0.2f);
                Gizmos.DrawLine(transform.position, spawnPoint.position);
            }
        }
    }
}
