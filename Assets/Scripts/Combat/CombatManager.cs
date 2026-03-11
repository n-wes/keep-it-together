using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepItTogether.Core;

namespace KeepItTogether.Combat
{
    /// <summary>
    /// Orchestrates all active combat encounters between NPCs and enemies.
    /// Runs on a configurable tick rate for performance.
    /// </summary>
    public class CombatManager : Singleton<CombatManager>
    {
        [SerializeField] private float _combatTickRate = 0.5f;
        [SerializeField] private float _engagementRange = 2f;

        private readonly List<CombatResolver> _activeCombats = new List<CombatResolver>();
        private readonly HashSet<int> _engagedNPCIds = new HashSet<int>();
        private readonly HashSet<int> _engagedEnemyIds = new HashSet<int>();
        private float _tickTimer;

        public int ActiveCombatCount => _activeCombats.Count;

        private void OnEnable()
        {
            EventBus.Subscribe<NPCDiedEvent>(OnNPCDied);
            EventBus.Subscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NPCDiedEvent>(OnNPCDied);
            EventBus.Unsubscribe<EnemyDiedEvent>(OnEnemyDied);
        }

        private void Update()
        {
            _tickTimer += Time.deltaTime;
            if (_tickTimer < _combatTickRate) return;

            float dt = _tickTimer;
            _tickTimer = 0f;

            TickCombats(dt);
            MatchNewCombatants();
            HandleCastleAttackers();
        }

        private void TickCombats(float deltaTime)
        {
            for (int i = _activeCombats.Count - 1; i >= 0; i--)
            {
                if (!_activeCombats[i].Tick(deltaTime))
                {
                    var finished = _activeCombats[i];
                    _engagedNPCIds.Remove(finished.NPC.Stats.Id);
                    _engagedEnemyIds.Remove(finished.Enemy.Id);
                    _activeCombats.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Find unengaged NPCs and enemies that are close enough to fight.
        /// </summary>
        private void MatchNewCombatants()
        {
            if (NPC.NPCManager.Instance == null) return;

            var availableNPCs = NPC.NPCManager.Instance.GetAllAliveNPCs()
                .Where(n => !_engagedNPCIds.Contains(n.Stats.Id) &&
                            n.CurrentState != NPC.NPCState.Fleeing &&
                            n.CurrentState != NPC.NPCState.Deserting &&
                            n.CurrentState != NPC.NPCState.Resting &&
                            n.CurrentState != NPC.NPCState.Eating)
                .ToList();

            var enemies = FindObjectsByType<Enemy.Enemy>(FindObjectsSortMode.None)
                .Where(e => e.CurrentState != Enemy.EnemyState.Dead &&
                            e.CurrentState != Enemy.EnemyState.Spawning &&
                            !e.TargetsCastle &&
                            !_engagedEnemyIds.Contains(e.Id))
                .ToList();

            foreach (var npc in availableNPCs)
            {
                if (enemies.Count == 0) break;

                // Find closest enemy in range
                Enemy.Enemy closest = null;
                float closestDist = _engagementRange;

                foreach (var enemy in enemies)
                {
                    if (_engagedEnemyIds.Contains(enemy.Id)) continue;
                    float dist = Vector2.Distance(npc.transform.position, enemy.transform.position);
                    if (dist < closestDist)
                    {
                        closest = enemy;
                        closestDist = dist;
                    }
                }

                if (closest != null)
                {
                    StartCombat(npc, closest);
                    enemies.Remove(closest);
                }
            }
        }

        /// <summary>
        /// Handle enemies that target the castle directly (siege units).
        /// </summary>
        private void HandleCastleAttackers()
        {
            if (Castle.CastleManager.Instance == null) return;
            var castle = Castle.CastleManager.Instance.GetCastle();
            if (castle == null || castle.IsDestroyed) return;

            var siegeEnemies = FindObjectsByType<Enemy.Enemy>(FindObjectsSortMode.None)
                .Where(e => e.CurrentState == Enemy.EnemyState.Attacking &&
                            e.TargetsCastle &&
                            !_engagedEnemyIds.Contains(e.Id));

            foreach (var enemy in siegeEnemies)
            {
                float dist = Vector2.Distance(enemy.transform.position, castle.transform.position);
                if (dist < enemy.AttackRange)
                {
                    DamageSystem.ApplyEnemyDamageToCastle(enemy, castle);
                }
            }
        }

        /// <summary>
        /// Start a new combat encounter between an NPC and enemy.
        /// </summary>
        public void StartCombat(NPC.NPC npc, Enemy.Enemy enemy)
        {
            if (_engagedNPCIds.Contains(npc.Stats.Id) || _engagedEnemyIds.Contains(enemy.Id))
                return;

            var resolver = new CombatResolver(npc, enemy);
            _activeCombats.Add(resolver);
            _engagedNPCIds.Add(npc.Stats.Id);
            _engagedEnemyIds.Add(enemy.Id);

            npc.SetTarget(enemy.transform);
        }

        /// <summary>
        /// End all active combats (e.g. on wave end).
        /// </summary>
        public void ClearAllCombats()
        {
            _activeCombats.Clear();
            _engagedNPCIds.Clear();
            _engagedEnemyIds.Clear();
        }

        private void OnNPCDied(NPCDiedEvent evt)
        {
            _engagedNPCIds.Remove(evt.NPCId);
        }

        private void OnEnemyDied(EnemyDiedEvent evt)
        {
            _engagedEnemyIds.Remove(evt.EnemyId);
        }
    }
}
