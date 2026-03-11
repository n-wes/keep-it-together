using UnityEngine;

namespace KeepItTogether.Data
{
    /// <summary>
    /// ScriptableObject holding global game configuration for balance tuning.
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "KeepItTogether/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Starting Values")]
        public int startingGold = 100;
        public int startingNPCs = 2;

        [Header("Idle Progression")]
        public float maxOfflineHours = 8f;
        public float offlineEfficiency = 0.5f;  // offline simulation runs at 50% effectiveness
        public float autoSaveInterval = 30f;

        [Header("Recruitment")]
        public int baseRecruitCost = 50;
        public float recruitCostMultiplier = 1.2f;  // each recruit costs more
        public int maxNPCs = 20;

        [Header("Wave Scaling")]
        public float waveHealthScaling = 1.1f;   // each wave enemies get 10% more HP
        public float waveDamageScaling = 1.05f;  // each wave enemies do 5% more damage
        public float waveCountScaling = 1.15f;   // each wave has 15% more enemies
        public float timeBetweenWaves = 10f;

        [Header("NPC Needs")]
        public float hungerDangerThreshold = 20f;    // below this, NPC starts losing HP
        public float moraleDangerThreshold = 20f;    // below this, NPC might desert
        public float restDangerThreshold = 15f;      // below this, NPC fights poorly
        public float desertionChancePerSecond = 0.01f; // when morale is critical

        [Header("Combat")]
        public float combatTickRate = 0.5f;  // how often combat resolves (seconds)
    }
}
