using System;
using UnityEngine;

namespace KeepItTogether.Core
{
    /// <summary>
    /// Singleton that tracks in-game time, real time, and calculates offline deltas on resume.
    /// </summary>
    public class TimeManager : Singleton<TimeManager>
    {
        // ── Inspector fields ─────────────────────────────────────────
        [SerializeField] private float _maxOfflineHours = Constants.MaxOfflineHours;

        // ── Internal state ───────────────────────────────────────────
        private float _gameTime;
        private float _offlineSeconds;
        private bool _isPaused;

        // ── Public properties ────────────────────────────────────────

        /// <summary>Accumulated time the game has been actively playing (excludes pauses).</summary>
        public float GameTime => _gameTime;

        /// <summary>Frame delta time (zero when paused).</summary>
        public float DeltaTime => _isPaused ? 0f : Time.deltaTime;

        /// <summary>Seconds the player was away, computed on the most recent resume.</summary>
        public float OfflineSeconds => _offlineSeconds;

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
            if (_isPaused) return;
            _gameTime += Time.deltaTime;
        }

        // ── Public API ───────────────────────────────────────────────

        /// <summary>
        /// Returns the number of real-world seconds since the last play session,
        /// capped at <see cref="_maxOfflineHours"/> hours.
        /// Reads the timestamp from the most recent save file.
        /// </summary>
        public float GetOfflineDuration()
        {
            var save = SaveSystem.Load();
            if (save == null) return 0f;

            var lastPlayed = save.GetLastPlayedTime();
            double seconds = (DateTime.UtcNow - lastPlayed).TotalSeconds;
            float capped = Mathf.Min((float)seconds, _maxOfflineHours * 3600f);
            return Mathf.Max(0f, capped);
        }

        // ── Mobile / focus hooks ─────────────────────────────────────

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus)
            {
                CalculateOfflineProgress();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                CalculateOfflineProgress();
            }
        }

        // ── Internal ─────────────────────────────────────────────────

        private void SubscribeEvents()
        {
            EventBus.Subscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Subscribe<GameResumedEvent>(OnGameResumed);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<GamePausedEvent>(OnGamePaused);
            EventBus.Unsubscribe<GameResumedEvent>(OnGameResumed);
        }

        private void OnGamePaused(GamePausedEvent _) => _isPaused = true;
        private void OnGameResumed(GameResumedEvent _) => _isPaused = false;

        private void CalculateOfflineProgress()
        {
            _offlineSeconds = GetOfflineDuration();

            if (_offlineSeconds <= 0f) return;

            Debug.Log($"[TimeManager] Player was away for {_offlineSeconds:F0}s");

            EventBus.Publish(new OfflineProgressCalculatedEvent
            {
                SecondsAway = _offlineSeconds,
                WavesSimulated = 0 // populated by the idle simulation system
            });
        }
    }
}
