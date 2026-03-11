using System.Collections.Generic;
using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.NPC
{
    /// <summary>
    /// Player-facing recruitment system. Generates random NPC options from a pool
    /// and lets the player recruit them for gold.
    /// </summary>
    public class NPCSpawner : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────
        [SerializeField] private List<NPCData> _availableNPCPool = new List<NPCData>();
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private int _optionsCount = 3;
        [SerializeField] private int _refreshCost = 25;

        // ── Runtime ──────────────────────────────────────────────────
        private NPCData[] _currentRecruitOptions;
        private PersonalityProfile[] _currentPersonalities;

        // Cost scaling
        private const float RECRUIT_COST_MULTIPLIER = 1.2f;

        // ── Public API ───────────────────────────────────────────────

        /// <summary>Current recruit options the player can pick from.</summary>
        public NPCData[] CurrentOptions => _currentRecruitOptions;

        /// <summary>Personality profiles for the current options (randomized from template).</summary>
        public PersonalityProfile[] CurrentPersonalities => _currentPersonalities;

        // ── Lifecycle ────────────────────────────────────────────────

        private void Start()
        {
            GenerateRecruitOptions();
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        private void OnDestroy()
        {
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
        }

        // ── Recruitment ──────────────────────────────────────────────

        /// <summary>
        /// Generate a fresh set of recruit options by randomly picking from the pool
        /// and giving each a slightly randomized personality.
        /// </summary>
        public void GenerateRecruitOptions()
        {
            if (_availableNPCPool == null || _availableNPCPool.Count == 0)
            {
                Debug.LogWarning("[NPCSpawner] NPC pool is empty. Cannot generate recruit options.");
                _currentRecruitOptions = new NPCData[0];
                _currentPersonalities = new PersonalityProfile[0];
                return;
            }

            int count = Mathf.Min(_optionsCount, _availableNPCPool.Count);
            _currentRecruitOptions = new NPCData[count];
            _currentPersonalities = new PersonalityProfile[count];

            // Shuffle-pick without duplicates when possible
            var pool = new List<NPCData>(_availableNPCPool);
            for (int i = 0; i < count; i++)
            {
                int idx = Random.Range(0, pool.Count);
                _currentRecruitOptions[i] = pool[idx];

                // Clone the default personality and add variance
                var baseProfile = pool[idx].defaultPersonality;
                _currentPersonalities[i] = baseProfile != null
                    ? ApplyPersonalityVariance(baseProfile.Clone(), 0.15f)
                    : PersonalityProfile.GenerateRandom();

                // Only remove if pool is large enough to fill remaining slots
                if (pool.Count > count - i)
                {
                    pool.RemoveAt(idx);
                }
            }
        }

        /// <summary>
        /// Recruit the NPC at the given option index. Deducts gold and spawns via <see cref="NPCManager"/>.
        /// </summary>
        /// <returns>The spawned NPC, or null if recruitment failed.</returns>
        public NPC RecruitNPC(int optionIndex)
        {
            if (_currentRecruitOptions == null || optionIndex < 0 || optionIndex >= _currentRecruitOptions.Length)
            {
                Debug.LogWarning("[NPCSpawner] Invalid recruit option index.");
                return null;
            }

            if (!NPCManager.HasInstance)
            {
                Debug.LogError("[NPCSpawner] NPCManager not found.");
                return null;
            }

            int cost = GetRecruitCost();

            if (!GameManager.HasInstance || GameManager.Instance.Gold < cost)
            {
                Debug.Log("[NPCSpawner] Not enough gold to recruit.");
                return null;
            }

            var data = _currentRecruitOptions[optionIndex];
            var personality = _currentPersonalities[optionIndex];

            // Deduct gold
            GameManager.Instance.Gold -= cost;
            EventBus.Publish(new GoldSpentEvent
            {
                Amount = cost,
                Purpose = $"Recruit {data.npcName}"
            });

            // Spawn
            Vector3 pos = _spawnPoint != null ? _spawnPoint.position : transform.position;
            var npc = NPCManager.Instance.SpawnNPC(data, pos);

            if (npc != null && npc.Stats != null)
            {
                // Apply the randomized personality to the stats clone
                npc.Stats.Personality.bravery = personality.bravery;
                npc.Stats.Personality.loyalty = personality.loyalty;
                npc.Stats.Personality.aggression = personality.aggression;
                npc.Stats.Personality.sociability = personality.sociability;
                npc.Stats.Personality.laziness = personality.laziness;
                npc.Stats.Personality.intelligence = personality.intelligence;

                // Re-create the personality driver with the updated traits
                npc.PersonalityDriver = new NPCPersonality(personality);

                EventBus.Publish(new NPCRecruitedEvent
                {
                    NPCId = npc.Stats.Id,
                    NPCName = npc.Stats.Name
                });
            }

            return npc;
        }

        /// <summary>
        /// Calculate the current recruitment cost. Scales with the number of living NPCs.
        /// </summary>
        public int GetRecruitCost()
        {
            int currentCount = NPCManager.HasInstance ? NPCManager.Instance.AliveCount : 0;
            int baseCost = _availableNPCPool.Count > 0 ? _availableNPCPool[0].recruitCost : 50;

            float scaled = baseCost * Mathf.Pow(RECRUIT_COST_MULTIPLIER, currentCount);
            return Mathf.RoundToInt(scaled);
        }

        /// <summary>
        /// Refresh the recruit options. Costs gold unless <paramref name="free"/> is true.
        /// </summary>
        public void RefreshOptions(bool free = false)
        {
            if (!free)
            {
                if (!GameManager.HasInstance || GameManager.Instance.Gold < _refreshCost)
                {
                    Debug.Log("[NPCSpawner] Not enough gold to refresh recruit options.");
                    return;
                }

                GameManager.Instance.Gold -= _refreshCost;
                EventBus.Publish(new GoldSpentEvent
                {
                    Amount = _refreshCost,
                    Purpose = "Refresh recruit options"
                });
            }

            GenerateRecruitOptions();
        }

        // ── Internal ─────────────────────────────────────────────────

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            // Free refresh after each wave
            RefreshOptions(free: true);
        }

        private static PersonalityProfile ApplyPersonalityVariance(PersonalityProfile profile, float variance)
        {
            profile.bravery = Mathf.Clamp01(profile.bravery + Random.Range(-variance, variance));
            profile.loyalty = Mathf.Clamp01(profile.loyalty + Random.Range(-variance, variance));
            profile.aggression = Mathf.Clamp01(profile.aggression + Random.Range(-variance, variance));
            profile.sociability = Mathf.Clamp01(profile.sociability + Random.Range(-variance, variance));
            profile.laziness = Mathf.Clamp01(profile.laziness + Random.Range(-variance, variance));
            profile.intelligence = Mathf.Clamp01(profile.intelligence + Random.Range(-variance, variance));
            return profile;
        }
    }
}
