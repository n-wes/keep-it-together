using UnityEngine;

namespace KeepItTogether.Data
{
    /// <summary>
    /// Categorizes enemy behavior archetypes.
    /// </summary>
    public enum EnemyType
    {
        Melee,
        Ranged,
        Siege,
        Flying,
        Boss
    }

    /// <summary>
    /// ScriptableObject defining an enemy type with stats, behavior, and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewEnemyData", menuName = "KeepItTogether/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Identity")]
        public string enemyName;
        [TextArea] public string description;
        public EnemyType enemyType;
        // public Sprite sprite;  // uncomment when art exists

        [Header("Stats")]
        public float maxHP = 50f;
        public float attackDamage = 8f;
        public float attackSpeed = 0.8f;
        public float moveSpeed = 2f;
        public float attackRange = 1.5f;

        [Header("Behavior")]
        public bool targetsCastle = false;  // siege units go for castle, not NPCs
        public bool isFlying = false;
        public float aggroRange = 5f;       // range at which enemy targets defenders

        [Header("Rewards")]
        public int goldReward = 10;
        public float experienceReward = 5f;
    }
}
