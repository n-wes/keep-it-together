using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.Enemy
{
    /// <summary>
    /// Main enemy component attached to every enemy game object.
    /// Drives a simple state machine: Spawning → Approaching → Attacking → Dead.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        // ── Inspector fields ─────────────────────────────────────────
        [SerializeField] private EnemyData _enemyData;

        // ── Runtime stats ────────────────────────────────────────────
        private int _id;
        private float _currentHP;
        private float _maxHP;
        private float _attackDamage;
        private float _attackSpeed;
        private float _moveSpeed;
        private float _attackRange;
        private float _aggroRange;
        private bool _targetsCastle;
        private float _difficultyMultiplier = 1f;

        // ── State machine ────────────────────────────────────────────
        private EnemyState _currentState;
        private Transform _currentTarget;
        private float _attackTimer;
        private float _spawnTimer;

        private const float SpawnInvulnerabilityDuration = 0.5f;

        // ── Public properties ────────────────────────────────────────

        /// <summary>Unique runtime identifier.</summary>
        public int Id => _id;

        /// <summary>Current hit points.</summary>
        public float CurrentHP => _currentHP;

        /// <summary>Maximum hit points (after difficulty scaling).</summary>
        public float MaxHP => _maxHP;

        /// <summary>Current lifecycle state.</summary>
        public EnemyState CurrentState => _currentState;

        /// <summary>The backing ScriptableObject data.</summary>
        public EnemyData Data => _enemyData;

        /// <summary>Whether this enemy targets the castle instead of NPCs.</summary>
        public bool TargetsCastle => _targetsCastle;

        /// <summary>Attack damage (after difficulty scaling).</summary>
        public float AttackDamage => _attackDamage;

        /// <summary>Attacks per second.</summary>
        public float AttackSpeed => _attackSpeed;

        /// <summary>Attack range in world units.</summary>
        public float AttackRange => _attackRange;

        /// <summary>Experience granted to the NPC that kills this enemy.</summary>
        public float ExperienceReward => _enemyData != null ? _enemyData.experienceReward : 5f;

        /// <summary>Gold reward on death.</summary>
        public int GoldReward => _enemyData != null ? _enemyData.goldReward : 10;

        // ── Initialization ───────────────────────────────────────────

        /// <summary>
        /// Configure this enemy from data, assign a unique id, and apply difficulty scaling.
        /// </summary>
        public void Initialize(EnemyData data, int id, float difficultyMultiplier)
        {
            _enemyData = data;
            _id = id;
            _difficultyMultiplier = difficultyMultiplier;

            _maxHP = data.maxHP * difficultyMultiplier;
            _currentHP = _maxHP;
            _attackDamage = data.attackDamage * difficultyMultiplier;
            _attackSpeed = data.attackSpeed;
            _moveSpeed = data.moveSpeed;
            _attackRange = data.attackRange;
            _aggroRange = data.aggroRange;
            _targetsCastle = data.targetsCastle;

            _currentState = EnemyState.Spawning;
            _spawnTimer = SpawnInvulnerabilityDuration;
            _attackTimer = 0f;
            _currentTarget = null;

            gameObject.tag = Constants.EnemyTag;
            gameObject.layer = Constants.EnemyLayer;
        }

        // ── MonoBehaviour ────────────────────────────────────────────

        private void Update()
        {
            switch (_currentState)
            {
                case EnemyState.Spawning:
                    HandleSpawning();
                    break;
                case EnemyState.Approaching:
                    HandleApproaching();
                    break;
                case EnemyState.Attacking:
                    HandleAttacking();
                    break;
                case EnemyState.Retreating:
                    HandleRetreating();
                    break;
                case EnemyState.Dead:
                    // Handled by WaveManager / object pool
                    break;
            }
        }

        // ── State handlers ───────────────────────────────────────────

        private void HandleSpawning()
        {
            _spawnTimer -= Time.deltaTime;
            if (_spawnTimer <= 0f)
            {
                _currentState = EnemyState.Approaching;
            }
        }

        private void HandleApproaching()
        {
            // Non-siege enemies scan for nearby NPCs first
            if (!_targetsCastle)
            {
                Transform npc = FindNearestNPC();
                if (npc != null && GetDistanceTo(npc) <= _aggroRange)
                {
                    SetTarget(npc);
                }
            }

            // Acquire a target if we don't have one (castle or NPC)
            if (_currentTarget == null)
            {
                FindNearestTarget();
            }

            if (_currentTarget == null) return;

            float distance = GetDistanceTo(_currentTarget);
            if (distance <= _attackRange)
            {
                _currentState = EnemyState.Attacking;
                _attackTimer = 0f;
                return;
            }

            // Move toward target
            Vector3 direction = (_currentTarget.position - transform.position).normalized;
            transform.position += direction * (_moveSpeed * Time.deltaTime);
        }

        private void HandleAttacking()
        {
            // Target lost — re-acquire
            if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
            {
                _currentTarget = null;
                _currentState = EnemyState.Approaching;
                return;
            }

            // Out of range — close distance
            if (GetDistanceTo(_currentTarget) > _attackRange * 1.2f)
            {
                _currentState = EnemyState.Approaching;
                return;
            }

            // Auto-attack tick
            _attackTimer += Time.deltaTime;
            if (_attackTimer >= 1f / _attackSpeed)
            {
                _attackTimer = 0f;
                PerformAttack();
            }
        }

        private void HandleRetreating()
        {
            // Boss-specific retreat: move away from castle briefly
            if (_currentTarget != null)
            {
                Vector3 direction = (transform.position - _currentTarget.position).normalized;
                transform.position += direction * (_moveSpeed * 0.5f * Time.deltaTime);
            }
        }

        // ── Combat ───────────────────────────────────────────────────

        private void PerformAttack()
        {
            if (_currentTarget == null) return;

            EventBus.Publish(new DamageDealtEvent
            {
                AttackerId = _id,
                DefenderId = 0, // resolved by combat system
                Damage = _attackDamage,
                AttackerIsNPC = false
            });

            // Direct damage to castle if targeting the castle object
            if (_currentTarget.CompareTag(Constants.CastleTag))
            {
                EventBus.Publish(new CastleDamagedEvent
                {
                    Damage = _attackDamage,
                    RemainingHP = -1f // castle manager fills this
                });
            }
        }

        /// <summary>
        /// Apply damage to this enemy. Fires <see cref="EnemyDiedEvent"/> on death.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (_currentState == EnemyState.Dead || _currentState == EnemyState.Spawning)
                return;

            _currentHP -= damage;

            if (_currentHP <= 0f)
            {
                _currentHP = 0f;
                Die();
            }
        }

        private void Die()
        {
            _currentState = EnemyState.Dead;

            EventBus.Publish(new EnemyDiedEvent
            {
                EnemyId = _id,
                EnemyType = _enemyData.enemyName,
                KillerNPCId = -1 // resolved by combat system
            });

            EventBus.Publish(new GoldEarnedEvent
            {
                Amount = _enemyData.goldReward,
                Source = $"enemy_{_enemyData.enemyName}"
            });

            gameObject.SetActive(false);
        }

        // ── Targeting ────────────────────────────────────────────────

        /// <summary>
        /// Assign a specific target transform (NPC or castle).
        /// </summary>
        public void SetTarget(Transform target)
        {
            _currentTarget = target;
        }

        /// <summary>
        /// Locate the most appropriate target based on enemy behaviour flags.
        /// Siege enemies always prefer the castle; others prefer nearby NPCs.
        /// </summary>
        public void FindNearestTarget()
        {
            if (_targetsCastle)
            {
                GameObject castle = GameObject.FindGameObjectWithTag(Constants.CastleTag);
                if (castle != null)
                {
                    _currentTarget = castle.transform;
                    return;
                }
            }

            Transform npc = FindNearestNPC();
            if (npc != null)
            {
                _currentTarget = npc;
                return;
            }

            // Fallback: target castle if no NPCs remain
            GameObject fallbackCastle = GameObject.FindGameObjectWithTag(Constants.CastleTag);
            if (fallbackCastle != null)
            {
                _currentTarget = fallbackCastle.transform;
            }
        }

        private Transform FindNearestNPC()
        {
            GameObject[] npcs = GameObject.FindGameObjectsWithTag(Constants.NPCTag);
            Transform nearest = null;
            float closestDistance = float.MaxValue;

            foreach (GameObject npc in npcs)
            {
                if (!npc.activeInHierarchy) continue;

                float dist = GetDistanceTo(npc.transform);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    nearest = npc.transform;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Returns the world-space distance between this enemy and the given target.
        /// </summary>
        public float GetDistanceTo(Transform target)
        {
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
        }
    }
}
