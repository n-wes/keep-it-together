using UnityEngine;
using KeepItTogether.Core;

namespace KeepItTogether.Combat
{
    /// <summary>
    /// Resolves individual combat encounters between an NPC and an enemy.
    /// </summary>
    public class CombatResolver
    {
        private readonly NPC.NPC _npc;
        private readonly Enemy.Enemy _enemy;
        private float _npcAttackTimer;
        private float _enemyAttackTimer;
        private bool _isActive;

        public NPC.NPC NPC => _npc;
        public Enemy.Enemy Enemy => _enemy;
        public bool IsActive => _isActive;
        public bool IsFinished => !_isActive;

        public CombatResolver(NPC.NPC npc, Enemy.Enemy enemy)
        {
            _npc = npc;
            _enemy = enemy;
            _isActive = true;
            _npcAttackTimer = 0f;
            _enemyAttackTimer = 0f;

            EventBus.Publish(new CombatStartedEvent
            {
                NPCId = npc.Stats.Id,
                EnemyId = enemy.Id
            });
        }

        /// <summary>
        /// Tick the combat encounter. Returns true if combat is still active.
        /// </summary>
        public bool Tick(float deltaTime)
        {
            if (!_isActive) return false;

            // Check if either combatant is dead/gone
            if (!_npc.Stats.IsAlive || _npc.CurrentState == NPC.NPCState.Fleeing ||
                _npc.CurrentState == NPC.NPCState.Deserting)
            {
                EndCombat(_enemy.Id, false);
                return false;
            }

            if (_enemy.CurrentState == Enemy.EnemyState.Dead)
            {
                EndCombat(_npc.Stats.Id, true);
                return false;
            }

            // NPC attacks
            _npcAttackTimer += deltaTime;
            if (_npcAttackTimer >= 1f / _npc.Stats.AttackSpeed)
            {
                _npcAttackTimer = 0f;
                DamageSystem.ApplyNPCDamageToEnemy(_npc, _enemy);
            }

            // Enemy attacks
            _enemyAttackTimer += deltaTime;
            if (_enemyAttackTimer >= 1f / _enemy.AttackSpeed)
            {
                _enemyAttackTimer = 0f;
                DamageSystem.ApplyEnemyDamageToNPC(_enemy, _npc);
            }

            return true;
        }

        private void EndCombat(int winnerId, bool winnerIsNPC)
        {
            _isActive = false;
            EventBus.Publish(new CombatEndedEvent
            {
                WinnerId = winnerId,
                WinnerIsNPC = winnerIsNPC
            });

            // Grant XP to surviving NPC
            if (winnerIsNPC && _npc.Stats.IsAlive)
            {
                _npc.Stats.AddExperience(_enemy.ExperienceReward);
            }
        }
    }
}
