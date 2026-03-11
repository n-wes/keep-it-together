using System.Collections.Generic;
using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.Enemy
{
    /// <summary>
    /// Singleton that drives wave progression: selects or generates wave data,
    /// delegates spawning to <see cref="WaveSpawner"/>, tracks enemy counts,
    /// and fires wave lifecycle events.
    /// </summary>
    public class WaveManager : Singleton<WaveManager>
    {
        // ── Inspector fields ─────────────────────────────────────────
        [SerializeField] private List<WaveData> _predefinedWaves = new List<WaveData>();
        [SerializeField] private WaveSpawner _waveSpawner;
        [SerializeField] private float _timeBetweenWaves = 10f;

        [Header("Procedural Generation Defaults")]
        [SerializeField] private EnemyData _goblinData;
        [SerializeField] private EnemyData _orcData;

        // ── Runtime state ────────────────────────────────────────────
        private int _currentWaveNumber;
        private int _enemiesAliveThisWave;
        private int _totalEnemiesThisWave;
        private bool _waveActive;
        private float _waveTimer;
        private bool _paused;

        // ── Public properties ────────────────────────────────────────

        /// <summary>The wave number currently in progress or most recently completed.</summary>
        public int CurrentWaveNumber => _currentWaveNumber;

        /// <summary>Enemies still alive in the active wave.</summary>
        public int EnemiesRemaining => _enemiesAliveThisWave;

        /// <summary>True while a wave is actively running.</summary>
        public bool IsWaveActive => _waveActive;

        /// <summary>Seconds remaining until the next wave auto-starts.</summary>
        public float TimeUntilNextWave => _waveActive ? 0f : _waveTimer;

        // ── Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            SubscribeEvents();
        }

        protected override void OnDestroy()
        {
            UnsubscribeEvents();
            base.OnDestroy();
        }

        private void Update()
        {
            if (_paused) return;

            if (!_waveActive)
            {
                _waveTimer -= Time.deltaTime;
                if (_waveTimer <= 0f)
                {
                    StartNextWave();
                }
            }
        }

        // ── Wave progression ─────────────────────────────────────────

        /// <summary>
        /// Advance to the next wave: resolve wave data, notify systems, and begin spawning.
        /// </summary>
        public void StartNextWave()
        {
            _currentWaveNumber++;

            WaveData wave = GetWaveData(_currentWaveNumber);
            _totalEnemiesThisWave = wave.TotalEnemyCount;
            _enemiesAliveThisWave = _totalEnemiesThisWave;
            _waveActive = true;

            EventBus.Publish(new WaveStartedEvent
            {
                WaveNumber = _currentWaveNumber,
                EnemyCount = _totalEnemiesThisWave
            });

            if (_waveSpawner != null)
            {
                _waveSpawner.StartWave(wave);
            }
            else
            {
                Debug.LogError("[WaveManager] WaveSpawner reference is missing.");
            }
        }

        /// <summary>
        /// Returns the predefined wave if one exists for the given number, otherwise generates one procedurally.
        /// </summary>
        public WaveData GetWaveData(int waveNumber)
        {
            // Predefined waves use 1-based waveNumber field
            foreach (WaveData wave in _predefinedWaves)
            {
                if (wave != null && wave.waveNumber == waveNumber)
                    return wave;
            }

            return GenerateProceduralWave(waveNumber);
        }

        // ── Procedural generation ────────────────────────────────────

        /// <summary>
        /// Create a runtime <see cref="WaveData"/> scaled to the given wave number.
        /// Early waves are goblin-only; orcs appear at wave 4+.
        /// </summary>
        public WaveData GenerateProceduralWave(int waveNumber)
        {
            WaveData wave = ScriptableObject.CreateInstance<WaveData>();
            wave.waveNumber = waveNumber;
            wave.waveName = $"Wave {waveNumber}";
            wave.announcement = $"Wave {waveNumber} approaches!";
            wave.difficultyMultiplier = GetWaveDifficulty(waveNumber);
            wave.timeBetweenGroups = 2f;
            wave.prepTime = 3f;
            wave.waveCompletionGold = 50 + (waveNumber * 10);
            wave.enemies = new List<WaveEnemy>();

            int totalCount = GetWaveEnemyCount(waveNumber);

            if (waveNumber < 4 || _orcData == null)
            {
                // Goblins only
                AddEnemyGroup(wave, _goblinData, totalCount);
            }
            else
            {
                // Mix goblins and orcs; orc ratio grows with wave number
                float orcRatio = Mathf.Clamp01((waveNumber - 3) * 0.1f);
                int orcCount = Mathf.Max(1, Mathf.RoundToInt(totalCount * orcRatio));
                int goblinCount = totalCount - orcCount;

                if (goblinCount > 0) AddEnemyGroup(wave, _goblinData, goblinCount);
                if (orcCount > 0) AddEnemyGroup(wave, _orcData, orcCount);
            }

            return wave;
        }

        private void AddEnemyGroup(WaveData wave, EnemyData data, int count)
        {
            if (data == null)
            {
                Debug.LogWarning("[WaveManager] Cannot add enemy group — EnemyData is null.");
                return;
            }

            wave.enemies.Add(new WaveEnemy
            {
                enemyData = data,
                count = count,
                spawnDelay = 0.5f
            });
        }

        /// <summary>
        /// Calculate the total enemy count for a wave. Scales with wave number.
        /// </summary>
        public int GetWaveEnemyCount(int waveNumber)
        {
            int baseCount = 5;
            return Mathf.RoundToInt(baseCount * (1f + waveNumber * 0.15f));
        }

        /// <summary>
        /// Calculate the difficulty multiplier for a wave. Scales linearly.
        /// </summary>
        public float GetWaveDifficulty(int waveNumber)
        {
            return 1.0f + (waveNumber * 0.1f);
        }

        // ── Event handlers ───────────────────────────────────────────

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            if (!_waveActive) return;

            _enemiesAliveThisWave--;

            if (_enemiesAliveThisWave <= 0)
            {
                _enemiesAliveThisWave = 0;
                CompleteWave();
            }
        }

        /// <summary>
        /// Finalize the current wave: award gold, fire completion events, and reset the inter-wave timer.
        /// </summary>
        private void CompleteWave()
        {
            _waveActive = false;

            WaveData waveData = GetWaveData(_currentWaveNumber);

            EventBus.Publish(new WaveCompletedEvent
            {
                WaveNumber = _currentWaveNumber,
                SurvivingDefenders = -1 // NPC system fills this
            });

            EventBus.Publish(new GoldEarnedEvent
            {
                Amount = waveData.waveCompletionGold,
                Source = $"wave_{_currentWaveNumber}_completed"
            });

            // Check if all predefined waves are exhausted (infinite mode continues)
            if (_predefinedWaves.Count > 0 && _currentWaveNumber >= _predefinedWaves.Count)
            {
                EventBus.Publish(new AllWavesClearedEvent());
            }

            _waveTimer = _timeBetweenWaves;
        }

        private void OnGamePaused(GamePausedEvent evt)
        {
            _paused = true;

            if (_waveSpawner != null)
            {
                _waveSpawner.StopSpawning();
            }
        }

        private void OnGameResumed(GameResumedEvent evt)
        {
            _paused = false;
        }

        // ── Event wiring ─────────────────────────────────────────────

        private void SubscribeEvents()
        {
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Subscribe<GameResumedEvent>(OnGameResumed);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Unsubscribe<GameResumedEvent>(OnGameResumed);
        }
    }
}
