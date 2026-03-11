using System.Collections;
using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.Enemy
{
    /// <summary>
    /// Handles the physical spawning of enemies for a wave using coroutines.
    /// Instantiates enemy prefabs at randomised spawn points and fires spawn events.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        // ── Inspector fields ─────────────────────────────────────────
        [SerializeField] private Transform[] _spawnPoints;
        [SerializeField] private GameObject _enemyPrefab;

        // ── Runtime state ────────────────────────────────────────────
        private WaveData _currentWave;
        private int _enemiesSpawnedThisWave;
        private int _totalEnemiesToSpawn;
        private bool _isSpawning;
        private Coroutine _spawnCoroutine;

        private static int _nextEnemyId = 1;

        // ── Public properties ────────────────────────────────────────

        /// <summary>True while a spawn coroutine is actively running.</summary>
        public bool IsSpawning => _isSpawning;

        /// <summary>Number of enemies spawned in the current wave so far.</summary>
        public int EnemiesSpawnedThisWave => _enemiesSpawnedThisWave;

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Begin spawning enemies defined in the given wave data.
        /// </summary>
        public void StartWave(WaveData waveData)
        {
            if (_isSpawning)
            {
                Debug.LogWarning("[WaveSpawner] Already spawning. Stopping current wave first.");
                StopSpawning();
            }

            _currentWave = waveData;
            _enemiesSpawnedThisWave = 0;
            _totalEnemiesToSpawn = waveData.TotalEnemyCount;
            _spawnCoroutine = StartCoroutine(SpawnWaveCoroutine(waveData));
        }

        /// <summary>
        /// Cancel the active spawn coroutine immediately.
        /// </summary>
        public void StopSpawning()
        {
            if (_spawnCoroutine != null)
            {
                StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = null;
            }

            _isSpawning = false;
        }

        // ── Coroutines ──────────────────────────────────────────────

        private IEnumerator SpawnWaveCoroutine(WaveData wave)
        {
            _isSpawning = true;

            // Optional prep delay before the first group
            if (wave.prepTime > 0f)
            {
                yield return new WaitForSeconds(wave.prepTime);
            }

            foreach (WaveEnemy group in wave.enemies)
            {
                if (group.enemyData == null)
                {
                    Debug.LogWarning("[WaveSpawner] WaveEnemy has null enemyData — skipping group.");
                    continue;
                }

                for (int i = 0; i < group.count; i++)
                {
                    SpawnEnemy(group.enemyData, wave.difficultyMultiplier);
                    _enemiesSpawnedThisWave++;

                    if (group.spawnDelay > 0f)
                    {
                        yield return new WaitForSeconds(group.spawnDelay);
                    }
                }

                // Pause between groups
                if (wave.timeBetweenGroups > 0f)
                {
                    yield return new WaitForSeconds(wave.timeBetweenGroups);
                }
            }

            _isSpawning = false;
            _spawnCoroutine = null;
        }

        // ── Spawning ────────────────────────────────────────────────

        private void SpawnEnemy(EnemyData data, float difficultyMultiplier)
        {
            if (_enemyPrefab == null)
            {
                Debug.LogError("[WaveSpawner] Enemy prefab is not assigned.");
                return;
            }

            Transform spawnPoint = GetRandomSpawnPoint();
            Vector3 position = spawnPoint != null ? spawnPoint.position : transform.position;

            GameObject enemyObj = Instantiate(_enemyPrefab, position, Quaternion.identity);
            enemyObj.name = $"{data.enemyName}_{_nextEnemyId}";

            Enemy enemy = enemyObj.GetComponent<Enemy>();
            if (enemy == null)
            {
                enemy = enemyObj.AddComponent<Enemy>();
            }

            int id = _nextEnemyId++;
            enemy.Initialize(data, id, difficultyMultiplier);

            EventBus.Publish(new EnemySpawnedEvent
            {
                EnemyId = id,
                EnemyType = data.enemyName
            });
        }

        /// <summary>
        /// Returns a random spawn point from the configured array, or this transform if none are set.
        /// </summary>
        public Transform GetRandomSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return transform;

            return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        }
    }
}
