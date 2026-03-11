using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KeepItTogether.Data
{
    /// <summary>
    /// Defines a group of enemies within a wave, including spawn timing.
    /// </summary>
    [System.Serializable]
    public class WaveEnemy
    {
        public EnemyData enemyData;
        public int count = 1;
        public float spawnDelay = 0.5f;  // delay between each spawn in this group
    }

    /// <summary>
    /// ScriptableObject defining a complete enemy wave with composition, timing, and rewards.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWaveData", menuName = "KeepItTogether/Wave Data")]
    public class WaveData : ScriptableObject
    {
        [Header("Wave Info")]
        public int waveNumber;
        public string waveName;           // e.g. "The First Assault"
        [TextArea] public string announcement;  // displayed to player

        [Header("Enemies")]
        public List<WaveEnemy> enemies = new List<WaveEnemy>();

        [Header("Timing")]
        public float timeBetweenGroups = 2f;  // pause between enemy groups
        public float prepTime = 5f;           // time before wave starts

        [Header("Difficulty")]
        public float difficultyMultiplier = 1f;  // scales enemy stats

        [Header("Rewards")]
        public int waveCompletionGold = 50;

        /// <summary>
        /// Returns the total number of enemies across all groups in this wave.
        /// </summary>
        public int TotalEnemyCount => enemies.Sum(e => e.count);
    }
}
