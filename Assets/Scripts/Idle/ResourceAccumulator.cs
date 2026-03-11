using UnityEngine;
using KeepItTogether.Core;

namespace KeepItTogether.Idle
{
    /// <summary>
    /// Tracks and accumulates passive resource generation over time.
    /// </summary>
    public class ResourceAccumulator : MonoBehaviour
    {
        [SerializeField] private float _baseGoldPerSecond = 0.5f;
        [SerializeField] private float _goldPerNPCPerSecond = 0.1f;

        private float _accumulator;

        public float GoldPerSecond
        {
            get
            {
                int npcCount = NPC.NPCManager.Instance != null
                    ? NPC.NPCManager.Instance.AliveCount
                    : 0;
                return _baseGoldPerSecond + (npcCount * _goldPerNPCPerSecond);
            }
        }

        private void Update()
        {
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.CurrentState != GameState.Playing &&
                GameManager.Instance.CurrentState != GameState.WaveActive &&
                GameManager.Instance.CurrentState != GameState.WavePreparing)
                return;

            _accumulator += GoldPerSecond * Time.deltaTime;

            if (_accumulator >= 1f)
            {
                int gold = Mathf.FloorToInt(_accumulator);
                _accumulator -= gold;
                GameManager.Instance.Gold += gold;

                EventBus.Publish(new GoldEarnedEvent
                {
                    Amount = gold,
                    Source = "Passive"
                });
            }
        }

        /// <summary>
        /// Calculate gold earned during offline time.
        /// </summary>
        public int CalculateOfflineGold(float seconds, float efficiency = 0.5f)
        {
            return Mathf.RoundToInt(GoldPerSecond * seconds * efficiency);
        }
    }
}
