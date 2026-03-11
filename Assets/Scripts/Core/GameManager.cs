using System;
using UnityEngine;

namespace KeepItTogether.Core
{
    /// <summary>
    /// High-level game states.
    /// </summary>
    public enum GameState
    {
        MainMenu,
        Playing,
        Paused,
        WavePreparing,
        WaveActive,
        GameOver
    }

    /// <summary>
    /// Singleton MonoBehaviour that owns the game state machine, core stats, and auto-save loop.
    /// </summary>
    public class GameManager : Singleton<GameManager>
    {
        // ── Inspector fields ─────────────────────────────────────────
        [SerializeField] private float _autoSaveInterval = Constants.AutoSaveInterval;

        // ── State ────────────────────────────────────────────────────
        private GameState _currentState = GameState.MainMenu;
        private int _currentWave;
        private int _gold;
        private int _totalKills;
        private float _autoSaveTimer;

        // ── Events (C# delegates alongside EventBus) ────────────────
        public event Action<GameState, GameState> OnStateChanged;

        // ── Public properties ────────────────────────────────────────

        /// <summary>Current game state.</summary>
        public GameState CurrentState => _currentState;

        /// <summary>Current wave number.</summary>
        public int CurrentWave
        {
            get => _currentWave;
            set => _currentWave = value;
        }

        /// <summary>Player gold. Setting fires <see cref="GoldEarnedEvent"/> or <see cref="GoldSpentEvent"/>.</summary>
        public int Gold
        {
            get => _gold;
            set
            {
                int delta = value - _gold;
                if (delta == 0) return;

                _gold = value;

                if (delta > 0)
                    EventBus.Publish(new GoldEarnedEvent { Amount = delta, Source = "direct" });
                else
                    EventBus.Publish(new GoldSpentEvent { Amount = -delta, Purpose = "direct" });
            }
        }

        /// <summary>Total enemies killed this session.</summary>
        public int TotalKills
        {
            get => _totalKills;
            set => _totalKills = value;
        }

        // ── Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
            _gold = Constants.StartingGold;
        }

        private void Update()
        {
            if (_currentState == GameState.Paused || _currentState == GameState.MainMenu)
                return;

            HandleAutoSave();
        }

        // ── State transitions ────────────────────────────────────────

        /// <summary>Transition from MainMenu to Playing.</summary>
        public void StartGame()
        {
            SetState(GameState.Playing);
        }

        /// <summary>Pause the game.</summary>
        public void PauseGame()
        {
            if (_currentState == GameState.Paused) return;
            SetState(GameState.Paused);
            EventBus.Publish(new GamePausedEvent());
        }

        /// <summary>Resume from pause.</summary>
        public void ResumeGame()
        {
            if (_currentState != GameState.Paused) return;
            SetState(GameState.Playing);
            EventBus.Publish(new GameResumedEvent());
        }

        /// <summary>Enter game-over state (castle destroyed, etc.).</summary>
        public void GameOver()
        {
            SetState(GameState.GameOver);
        }

        /// <summary>Begin the wave preparation phase.</summary>
        public void PrepareWave()
        {
            SetState(GameState.WavePreparing);
        }

        /// <summary>Start the active wave. Increments <see cref="CurrentWave"/> and fires <see cref="WaveStartedEvent"/>.</summary>
        public void StartWave(int enemyCount = 0)
        {
            _currentWave++;
            SetState(GameState.WaveActive);
            EventBus.Publish(new WaveStartedEvent
            {
                WaveNumber = _currentWave,
                EnemyCount = enemyCount
            });
        }

        // ── Mobile background hooks ──────────────────────────────────

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                ForceSave();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                ForceSave();
            }
        }

        // ── Save helpers ─────────────────────────────────────────────

        /// <summary>Build a <see cref="SaveData"/> snapshot from the current game state.</summary>
        public SaveData BuildSaveData()
        {
            return new SaveData
            {
                currentWave = _currentWave,
                gold = _gold,
                totalKills = _totalKills,
                castleHP = 0f // populated by CastleManager when available
            };
        }

        /// <summary>Restore game stats from loaded save data.</summary>
        public void LoadFromSave(SaveData data)
        {
            if (data == null) return;

            _currentWave = data.currentWave;
            _gold = data.gold;
            _totalKills = data.totalKills;
        }

        /// <summary>Immediately persist the current game state to disk.</summary>
        public void ForceSave()
        {
            SaveSystem.Save(BuildSaveData());
        }

        // ── Internal ─────────────────────────────────────────────────

        private void SetState(GameState newState)
        {
            if (_currentState == newState) return;

            var previous = _currentState;
            _currentState = newState;
            OnStateChanged?.Invoke(previous, newState);
            Debug.Log($"[GameManager] {previous} → {newState}");
        }

        private void HandleAutoSave()
        {
            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                ForceSave();
            }
        }
    }
}
