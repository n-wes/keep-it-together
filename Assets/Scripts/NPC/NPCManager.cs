using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.NPC
{
    /// <summary>
    /// Singleton manager that owns and orchestrates all active NPCs.
    /// Handles spawning, despawning, batch needs ticking, and event integration.
    /// </summary>
    public class NPCManager : Singleton<NPCManager>
    {
        // ── Inspector ────────────────────────────────────────────────
        [SerializeField] private GameObject _npcPrefab;

        // ── Runtime state ────────────────────────────────────────────
        private readonly List<NPC> _activeNPCs = new List<NPC>();
        private int _nextNPCId = 1;

        // Morale adjustments
        private const float WAVE_COMPLETE_MORALE_BOOST = 15f;
        private const float NEARBY_DEATH_MORALE_PENALTY = -10f;
        private const float NEARBY_DEATH_RADIUS = 10f;

        // ── Public properties ────────────────────────────────────────

        /// <summary>Number of currently alive NPCs.</summary>
        public int AliveCount => _activeNPCs.Count(n => n != null && n.Stats != null && n.Stats.IsAlive);

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

        // ── Spawning / Despawning ────────────────────────────────────

        /// <summary>
        /// Instantiate a new NPC from the given data at the specified world position.
        /// </summary>
        public NPC SpawnNPC(NPCData data, Vector3 position)
        {
            if (data == null)
            {
                Debug.LogError("[NPCManager] Cannot spawn NPC: NPCData is null.");
                return null;
            }

            int id = _nextNPCId++;

            GameObject go;
            if (_npcPrefab != null)
            {
                go = Instantiate(_npcPrefab, position, Quaternion.identity);
            }
            else
            {
                go = new GameObject();
                go.transform.position = position;
            }

            var npc = go.GetComponent<NPC>();
            if (npc == null)
            {
                npc = go.AddComponent<NPC>();
            }

            npc.Initialize(data, id);
            _activeNPCs.Add(npc);

            EventBus.Publish(new NPCSpawnedEvent
            {
                NPCId = id,
                NPCName = data.npcName
            });

            Debug.Log($"[NPCManager] Spawned {data.npcName} (ID {id}) at {position}");
            return npc;
        }

        /// <summary>
        /// Remove an NPC by runtime ID. Destroys the GameObject.
        /// </summary>
        public void DespawnNPC(int npcId)
        {
            var npc = GetNPC(npcId);
            if (npc == null) return;

            _activeNPCs.Remove(npc);

            if (npc.gameObject != null)
            {
                Destroy(npc.gameObject);
            }
        }

        /// <summary>
        /// Find an active NPC by runtime ID.
        /// </summary>
        public NPC GetNPC(int id)
        {
            return _activeNPCs.FirstOrDefault(n => n != null && n.Stats != null && n.Stats.Id == id);
        }

        /// <summary>
        /// Returns all alive NPCs.
        /// </summary>
        public List<NPC> GetAllAliveNPCs()
        {
            return _activeNPCs.Where(n => n != null && n.Stats != null && n.Stats.IsAlive).ToList();
        }

        /// <summary>
        /// Returns all NPCs currently in the given state.
        /// </summary>
        public List<NPC> GetNPCsByState(NPCState state)
        {
            return _activeNPCs
                .Where(n => n != null && n.Stats != null && n.Stats.State == state)
                .ToList();
        }

        // ── Batch updates ────────────────────────────────────────────

        /// <summary>
        /// Batch-tick all NPC needs decay. Useful for offline progression or custom tick loops.
        /// </summary>
        public void TickAllNPCNeeds(float deltaTime)
        {
            foreach (var npc in _activeNPCs)
            {
                if (npc == null || npc.Stats == null || !npc.Stats.IsAlive) continue;

                var personality = npc.PersonalityDriver;

                float moraleDecayMod = personality != null ? personality.GetMoraleDecayModifier() : 1f;
                float restDecayMod = personality != null ? personality.GetRestDecayModifier() : 1f;

                npc.Stats.ModifyHunger(-npc.Stats.HungerDecayRate * deltaTime);
                npc.Stats.ModifyMorale(-npc.Stats.MoraleDecayRate * moraleDecayMod * deltaTime);
                npc.Stats.ModifyRest(-npc.Stats.RestDecayRate * restDecayMod * deltaTime);
            }
        }

        // ── Save / Load ──────────────────────────────────────────────

        /// <summary>
        /// Collect save data from every active NPC.
        /// </summary>
        public List<NPCSaveData> GetSaveData()
        {
            var list = new List<NPCSaveData>();
            foreach (var npc in _activeNPCs)
            {
                if (npc != null && npc.Stats != null)
                {
                    list.Add(npc.Stats.ToSaveData());
                }
            }
            return list;
        }

        /// <summary>
        /// Restore NPC state from saved data. Typically called during game load
        /// after NPCs have been re-spawned from their <see cref="NPCData"/> templates.
        /// </summary>
        public void LoadFromSaveData(List<NPCSaveData> data)
        {
            if (data == null) return;

            foreach (var saveData in data)
            {
                var npc = GetNPC(saveData.id);
                if (npc != null && npc.Stats != null)
                {
                    npc.Stats.LoadFromSaveData(saveData);
                }
            }

            // Keep ID counter ahead of all loaded IDs
            if (data.Count > 0)
            {
                int maxId = data.Max(d => d.id);
                if (_nextNPCId <= maxId)
                {
                    _nextNPCId = maxId + 1;
                }
            }
        }

        // ── Event handlers ───────────────────────────────────────────

        private void SubscribeEvents()
        {
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<NPCDiedEvent>(OnNPCDied);
        }

        private void UnsubscribeEvents()
        {
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<NPCDiedEvent>(OnNPCDied);
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            // Boost morale for all surviving NPCs
            foreach (var npc in GetAllAliveNPCs())
            {
                npc.Stats.ModifyMorale(WAVE_COMPLETE_MORALE_BOOST);
            }

            Debug.Log($"[NPCManager] Wave {evt.WaveNumber} complete! Morale boosted for {AliveCount} survivors.");
        }

        private void OnNPCDied(NPCDiedEvent evt)
        {
            // Nearby NPCs lose morale when a comrade falls
            var deadNPC = GetNPC(evt.NPCId);
            Vector3 deathPos = deadNPC != null ? deadNPC.transform.position : Vector3.zero;

            foreach (var npc in GetAllAliveNPCs())
            {
                if (npc.Stats.Id == evt.NPCId) continue;

                float dist = Vector3.Distance(npc.transform.position, deathPos);
                if (dist <= NEARBY_DEATH_RADIUS)
                {
                    // Closer NPCs feel it more; sociable ones feel it the most
                    float distFactor = 1f - (dist / NEARBY_DEATH_RADIUS);
                    float socialFactor = npc.Stats.Personality != null
                        ? Mathf.Lerp(0.5f, 1.5f, npc.Stats.Personality.sociability)
                        : 1f;

                    npc.Stats.ModifyMorale(NEARBY_DEATH_MORALE_PENALTY * distFactor * socialFactor);
                }
            }
        }
    }
}
