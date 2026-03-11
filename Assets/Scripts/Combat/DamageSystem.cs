using UnityEngine;
using KeepItTogether.Core;

namespace KeepItTogether.Combat
{
    /// <summary>
    /// Handles damage calculation and application between attackers and defenders.
    /// Pure static utility — no MonoBehaviour needed.
    /// </summary>
    public static class DamageSystem
    {
        /// <summary>
        /// Calculate final damage after defense reduction.
        /// </summary>
        public static float CalculateDamage(float rawDamage, float defenseReduction)
        {
            return Mathf.Max(1f, rawDamage * (1f - Mathf.Clamp01(defenseReduction)));
        }

        /// <summary>
        /// Apply damage from an NPC to an enemy.
        /// </summary>
        public static void ApplyNPCDamageToEnemy(NPC.NPC attacker, Enemy.Enemy target)
        {
            if (attacker == null || target == null) return;
            if (!attacker.Stats.IsAlive || target.CurrentState == Enemy.EnemyState.Dead) return;

            float effectiveness = attacker.Stats.GetCombatEffectiveness();
            float damage = attacker.Stats.AttackDamage * effectiveness;

            target.TakeDamage(damage);

            EventBus.Publish(new DamageDealtEvent
            {
                AttackerId = attacker.Stats.Id,
                DefenderId = target.Id,
                Damage = damage,
                AttackerIsNPC = true
            });
        }

        /// <summary>
        /// Apply damage from an enemy to an NPC.
        /// </summary>
        public static void ApplyEnemyDamageToNPC(Enemy.Enemy attacker, NPC.NPC target)
        {
            if (attacker == null || target == null) return;
            if (attacker.CurrentState == Enemy.EnemyState.Dead || !target.Stats.IsAlive) return;

            float damage = attacker.AttackDamage;
            target.TakeDamage(damage);

            EventBus.Publish(new DamageDealtEvent
            {
                AttackerId = attacker.Id,
                DefenderId = target.Stats.Id,
                Damage = damage,
                AttackerIsNPC = false
            });
        }

        /// <summary>
        /// Apply damage from an enemy to the castle.
        /// </summary>
        public static void ApplyEnemyDamageToCastle(Enemy.Enemy attacker, Castle.Castle castle)
        {
            if (attacker == null || castle == null || castle.IsDestroyed) return;
            if (attacker.CurrentState == Enemy.EnemyState.Dead) return;

            float damage = CalculateDamage(attacker.AttackDamage, castle.CurrentDefense);
            castle.TakeDamage(damage);
        }
    }
}
