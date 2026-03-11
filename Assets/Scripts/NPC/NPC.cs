using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.NPC
{
    /// <summary>
    /// Main NPC MonoBehaviour attached to NPC GameObjects.
    /// Owns the state machine, ticks needs decay, and delegates logic to
    /// <see cref="NPCStats"/> and <see cref="NPCPersonality"/>.
    /// </summary>
    public class NPC : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────────────
        [SerializeField] private NPCData _npcData;

        // ── Runtime state ────────────────────────────────────────────
        private Transform _defensePosition;
        private Transform _combatTarget;
        private float _attackTimer;
        private float _stateTimer;

        // Needs recovery rates while resting/eating
        private const float REST_RECOVERY_RATE = 20f;
        private const float EAT_RECOVERY_RATE = 25f;
        private const float NEEDS_CRITICAL_THRESHOLD = 0.2f;
        private const float NEEDS_FULL_THRESHOLD = 0.9f;
        private const float FLEE_DISTANCE = 8f;
        private const float DESERT_OFFSCREEN_X = 25f;

        // ── Public API ───────────────────────────────────────────────

        /// <summary>Runtime stats for this NPC.</summary>
        public NPCStats Stats { get; private set; }

        /// <summary>Personality-driven behavior driver.</summary>
        public NPCPersonality PersonalityDriver { get; set; }

        /// <summary>Current behavioral state.</summary>
        public NPCState CurrentState => Stats != null ? Stats.State : NPCState.Dead;

        // ── Lifecycle ────────────────────────────────────────────────

        private void Awake()
        {
            if (_npcData != null)
            {
                Initialize(_npcData, GetInstanceID());
            }
        }

        private void Update()
        {
            if (Stats == null || !Stats.IsAlive) return;

            float dt = TimeManager.HasInstance ? TimeManager.Instance.DeltaTime : Time.deltaTime;
            if (dt <= 0f) return;

            TickNeedsDecay(dt);
            RunStateMachine(dt);
        }

        // ── Public methods ───────────────────────────────────────────

        /// <summary>
        /// Initialize this NPC from data and a unique runtime ID.
        /// Called by <see cref="NPCManager"/> on spawn.
        /// </summary>
        public void Initialize(NPCData data, int id)
        {
            _npcData = data;

            Stats = new NPCStats();
            Stats.InitializeFromData(data, id);

            PersonalityDriver = new NPCPersonality(Stats.Personality);

            gameObject.name = $"NPC_{Stats.Name}_{Stats.Id}";
            gameObject.tag = Constants.NPCTag;
            gameObject.layer = Constants.NPCLayer;
        }

        /// <summary>
        /// Assign a defense position this NPC should move toward and hold.
        /// </summary>
        public void AssignDefensePosition(Transform position)
        {
            _defensePosition = position;

            if (Stats.State == NPCState.Idle)
            {
                Stats.State = NPCState.Moving;
            }
        }

        /// <summary>
        /// Apply damage to this NPC (delegates to <see cref="NPCStats.TakeDamage"/>).
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (Stats == null || !Stats.IsAlive) return;

            Stats.TakeDamage(damage);

            if (!Stats.IsAlive)
            {
                OnDeath();
            }
        }

        /// <summary>
        /// Set a combat target for this NPC to attack.
        /// </summary>
        public void SetTarget(Transform target)
        {
            _combatTarget = target;

            if (target != null && Stats.IsAlive && Stats.State != NPCState.Dead)
            {
                if (Stats.State != NPCState.Fleeing && Stats.State != NPCState.Deserting)
                {
                    Stats.State = NPCState.Fighting;
                    _attackTimer = 0f;

                    EventBus.Publish(new CombatStartedEvent
                    {
                        NPCId = Stats.Id,
                        EnemyId = target.GetInstanceID()
                    });

                    Debug.Log($"[NPC] {Stats.Name}: \"{PersonalityDriver.GetBattleCry()}\"");
                }
            }
        }

        // ── State machine ────────────────────────────────────────────

        private void RunStateMachine(float dt)
        {
            switch (Stats.State)
            {
                case NPCState.Idle:
                    UpdateIdle();
                    break;
                case NPCState.Moving:
                    UpdateMoving(dt);
                    break;
                case NPCState.Fighting:
                    UpdateFighting(dt);
                    break;
                case NPCState.Resting:
                    UpdateResting(dt);
                    break;
                case NPCState.Eating:
                    UpdateEating(dt);
                    break;
                case NPCState.Fleeing:
                    UpdateFleeing(dt);
                    break;
                case NPCState.Deserting:
                    UpdateDeserting(dt);
                    break;
                case NPCState.Dead:
                    break;
            }
        }

        private void UpdateIdle()
        {
            // Check critical needs first
            if (CheckCriticalNeeds()) return;

            // Check for an active combat target
            if (_combatTarget != null)
            {
                Stats.State = NPCState.Fighting;
                _attackTimer = 0f;
                return;
            }

            // Move to defense position if assigned and not already there
            if (_defensePosition != null)
            {
                float dist = Vector3.Distance(transform.position, _defensePosition.position);
                if (dist > 0.5f)
                {
                    Stats.State = NPCState.Moving;
                }
            }
        }

        private void UpdateMoving(float dt)
        {
            if (CheckCriticalNeeds()) return;

            Transform target = _defensePosition;
            if (target == null)
            {
                Stats.State = NPCState.Idle;
                return;
            }

            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * Stats.MoveSpeed * dt;

            float dist = Vector3.Distance(transform.position, target.position);
            if (dist <= 0.5f)
            {
                transform.position = target.position;
                Stats.State = NPCState.Idle;
            }
        }

        private void UpdateFighting(float dt)
        {
            // Check if we should flee
            float moralePercent = Stats.MaxMorale > 0f ? Stats.CurrentMorale / Stats.MaxMorale : 0f;
            if (PersonalityDriver.ShouldFlee(Stats.CurrentHP, Stats.MaxHP, moralePercent))
            {
                Stats.State = NPCState.Fleeing;
                _stateTimer = 0f;
                return;
            }

            // Lost target
            if (_combatTarget == null || !_combatTarget.gameObject.activeInHierarchy)
            {
                _combatTarget = null;
                EventBus.Publish(new CombatEndedEvent
                {
                    WinnerId = Stats.Id,
                    WinnerIsNPC = true
                });
                Stats.State = NPCState.Idle;
                return;
            }

            // Move into range
            float dist = Vector3.Distance(transform.position, _combatTarget.position);
            if (dist > Stats.AttackRange)
            {
                Vector3 direction = (_combatTarget.position - transform.position).normalized;
                transform.position += direction * Stats.MoveSpeed * dt;
                return;
            }

            // Attack on timer
            _attackTimer += dt;
            float attackInterval = Stats.AttackSpeed > 0f ? 1f / Stats.AttackSpeed : 1f;

            if (_attackTimer >= attackInterval)
            {
                _attackTimer = 0f;
                PerformAttack();
            }
        }

        private void UpdateResting(float dt)
        {
            Stats.Rest(REST_RECOVERY_RATE * dt);

            float restPercent = Stats.MaxRest > 0f ? Stats.CurrentRest / Stats.MaxRest : 1f;
            if (restPercent >= NEEDS_FULL_THRESHOLD)
            {
                Stats.State = NPCState.Idle;
            }
        }

        private void UpdateEating(float dt)
        {
            Stats.Feed(EAT_RECOVERY_RATE * dt);

            float hungerPercent = Stats.MaxHunger > 0f ? Stats.CurrentHunger / Stats.MaxHunger : 1f;
            if (hungerPercent >= NEEDS_FULL_THRESHOLD)
            {
                Stats.State = NPCState.Idle;
            }
        }

        private void UpdateFleeing(float dt)
        {
            // Check if morale is so low that we desert
            float moralePercent = Stats.MaxMorale > 0f ? Stats.CurrentMorale / Stats.MaxMorale : 0f;
            if (PersonalityDriver.ShouldDesert(moralePercent))
            {
                Stats.State = NPCState.Deserting;
                Debug.Log($"[NPC] {Stats.Name}: \"{PersonalityDriver.GetComplaint()}\" (deserting!)");
                return;
            }

            // Move away from the threat
            Vector3 fleeDirection = Vector3.right; // default: flee right
            if (_combatTarget != null)
            {
                fleeDirection = (transform.position - _combatTarget.position).normalized;
            }

            transform.position += fleeDirection * Stats.MoveSpeed * 1.2f * dt;

            // If we've fled far enough and morale has recovered a bit, return to idle
            _stateTimer += dt;
            if (_stateTimer > 3f && moralePercent > 0.3f)
            {
                Stats.State = NPCState.Idle;
                _combatTarget = null;
            }
        }

        private void UpdateDeserting(float dt)
        {
            // Move off-screen to the right
            transform.position += Vector3.right * Stats.MoveSpeed * 1.5f * dt;

            if (Mathf.Abs(transform.position.x) > DESERT_OFFSCREEN_X)
            {
                EventBus.Publish(new NPCDesertedEvent
                {
                    NPCId = Stats.Id,
                    NPCName = Stats.Name,
                    Morale = Stats.CurrentMorale
                });

                Stats.State = NPCState.Dead;
                gameObject.SetActive(false);

                if (NPCManager.HasInstance)
                {
                    NPCManager.Instance.DespawnNPC(Stats.Id);
                }
            }
        }

        // ── Needs ────────────────────────────────────────────────────

        private void TickNeedsDecay(float dt)
        {
            float moraleDecayMod = PersonalityDriver.GetMoraleDecayModifier();
            float restDecayMod = PersonalityDriver.GetRestDecayModifier();

            Stats.ModifyHunger(-Stats.HungerDecayRate * dt);
            Stats.ModifyMorale(-Stats.MoraleDecayRate * moraleDecayMod * dt);
            Stats.ModifyRest(-Stats.RestDecayRate * restDecayMod * dt);

            // Starvation damage
            float hungerPercent = Stats.MaxHunger > 0f ? Stats.CurrentHunger / Stats.MaxHunger : 1f;
            if (hungerPercent <= 0f)
            {
                Stats.TakeDamage(dt * 2f);
                if (!Stats.IsAlive) OnDeath();
            }
        }

        /// <summary>
        /// Returns true if a critical need forced a state change.
        /// </summary>
        private bool CheckCriticalNeeds()
        {
            float hungerPercent = Stats.MaxHunger > 0f ? Stats.CurrentHunger / Stats.MaxHunger : 1f;
            float restPercent = Stats.MaxRest > 0f ? Stats.CurrentRest / Stats.MaxRest : 1f;
            float moralePercent = Stats.MaxMorale > 0f ? Stats.CurrentMorale / Stats.MaxMorale : 1f;

            // Check desertion first (most extreme)
            if (PersonalityDriver.ShouldDesert(moralePercent))
            {
                Stats.State = NPCState.Deserting;
                Debug.Log($"[NPC] {Stats.Name}: \"{PersonalityDriver.GetComplaint()}\" (deserting!)");
                return true;
            }

            // Check fleeing
            if (PersonalityDriver.ShouldFlee(Stats.CurrentHP, Stats.MaxHP, moralePercent))
            {
                Stats.State = NPCState.Fleeing;
                _stateTimer = 0f;
                return true;
            }

            // Hunger critical → eat
            if (hungerPercent < NEEDS_CRITICAL_THRESHOLD && Stats.State != NPCState.Fighting)
            {
                Stats.State = NPCState.Eating;
                Debug.Log($"[NPC] {Stats.Name}: \"{PersonalityDriver.GetComplaint()}\"");
                return true;
            }

            // Rest critical → rest
            if (restPercent < NEEDS_CRITICAL_THRESHOLD && Stats.State != NPCState.Fighting)
            {
                Stats.State = NPCState.Resting;
                Debug.Log($"[NPC] {Stats.Name}: \"{PersonalityDriver.GetComplaint()}\"");
                return true;
            }

            return false;
        }

        // ── Combat helpers ───────────────────────────────────────────

        private void PerformAttack()
        {
            if (_combatTarget == null) return;

            float effectiveness = Stats.GetCombatEffectiveness();
            float aggressionMod = PersonalityDriver.GetAggressionModifier();
            float damage = Stats.AttackDamage * effectiveness * aggressionMod;

            EventBus.Publish(new DamageDealtEvent
            {
                AttackerId = Stats.Id,
                DefenderId = _combatTarget.GetInstanceID(),
                Damage = damage,
                AttackerIsNPC = true
            });

            // If the target has an IDamageable or similar, deal damage directly
            var targetNPC = _combatTarget.GetComponent<NPC>();
            if (targetNPC != null)
            {
                targetNPC.TakeDamage(damage);
            }
        }

        private void OnDeath()
        {
            Stats.State = NPCState.Dead;
            gameObject.SetActive(false);
        }
    }
}
