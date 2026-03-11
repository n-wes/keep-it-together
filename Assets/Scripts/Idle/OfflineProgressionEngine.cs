using UnityEngine;
using KeepItTogether.Core;

namespace KeepItTogether.Idle
{
    /// <summary>
    /// Calculates and applies what happened while the player was away.
    /// Integrates IdleSimulator results back into the live game state.
    /// </summary>
    public class OfflineProgressionEngine : Singleton<OfflineProgressionEngine>
    {
        [SerializeField] private float _maxOfflineHours = 8f;
        [SerializeField] private float _offlineEfficiency = 0.5f;

        private ResourceAccumulator _resourceAccumulator;

        private void Start()
        {
            _resourceAccumulator = GetComponent<ResourceAccumulator>();
            if (_resourceAccumulator == null)
            {
                _resourceAccumulator = gameObject.AddComponent<ResourceAccumulator>();
            }

            EventBus.Subscribe<OfflineProgressCalculatedEvent>(OnOfflineDetected);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<OfflineProgressCalculatedEvent>(OnOfflineDetected);
        }

        private void OnOfflineDetected(OfflineProgressCalculatedEvent evt)
        {
            if (evt.SecondsAway < 60f) return; // ignore < 1 minute

            float cappedSeconds = Mathf.Min(evt.SecondsAway, _maxOfflineHours * 3600f);
            ProcessOfflineTime(cappedSeconds);
        }

        /// <summary>
        /// Process offline time: simulate combat, accumulate resources, apply NPC needs decay.
        /// </summary>
        public void ProcessOfflineTime(float seconds)
        {
            int npcCount = NPC.NPCManager.Instance != null
                ? NPC.NPCManager.Instance.AliveCount : 0;

            if (npcCount == 0)
            {
                // No NPCs — just give passive gold
                int gold = _resourceAccumulator != null
                    ? _resourceAccumulator.CalculateOfflineGold(seconds, _offlineEfficiency)
                    : 0;
                GameManager.Instance.Gold += gold;
                Debug.Log($"[Offline] No NPCs. Earned {gold}g passively over {seconds:F0}s.");
                return;
            }

            // Get average NPC stats for simulation
            float avgDamage = 0f, avgHP = 0f;
            var npcs = NPC.NPCManager.Instance.GetAllAliveNPCs();
            foreach (var npc in npcs)
            {
                avgDamage += npc.Stats.AttackDamage;
                avgHP += npc.Stats.MaxHP;
            }
            avgDamage /= npcCount;
            avgHP /= npcCount;

            float castleHP = Castle.CastleManager.Instance != null
                ? Castle.CastleManager.Instance.GetCastle().CurrentHP : 500f;
            int currentWave = GameManager.Instance.CurrentWave;

            // Run simulation
            var result = IdleSimulator.Simulate(
                seconds, npcCount, avgDamage, avgHP,
                currentWave, castleHP, _offlineEfficiency);

            // Apply results
            GameManager.Instance.Gold += result.GoldEarned;
            GameManager.Instance.TotalKills += result.EnemiesKilled;

            // Apply NPC needs decay (simplified: just decay hunger/morale/rest)
            foreach (var npc in npcs)
            {
                float hungerDecay = Constants.BaseNPCHungerDecayRate * seconds * _offlineEfficiency;
                float moraleDecay = Constants.BaseNPCMoraleDecayRate * seconds * _offlineEfficiency;
                float restDecay = Constants.BaseNPCRestDecayRate * seconds * _offlineEfficiency;

                npc.Stats.ModifyHunger(-hungerDecay);
                npc.Stats.ModifyMorale(-moraleDecay);
                npc.Stats.ModifyRest(-restDecay);
            }

            // Apply castle damage
            if (result.CastleDamageTaken > 0 && Castle.CastleManager.Instance != null)
            {
                Castle.CastleManager.Instance.GetCastle().TakeDamage(result.CastleDamageTaken);
            }

            // Advance wave counter
            if (result.WavesSimulated > 0)
            {
                // GameManager tracks wave advancement
                for (int i = 0; i < result.WavesSimulated; i++)
                {
                    GameManager.Instance.CurrentWave++;
                }
            }

            // Log summary
            Debug.Log($"[Offline] {seconds:F0}s away: {result.WavesSimulated} waves, " +
                      $"+{result.GoldEarned}g, {result.EnemiesKilled} kills, " +
                      $"{result.NPCsLost} NPCs lost");

            // Fire event so UI can show summary
            EventBus.Publish(new OfflineProgressCalculatedEvent
            {
                SecondsAway = seconds,
                WavesSimulated = result.WavesSimulated
            });
        }
    }
}
