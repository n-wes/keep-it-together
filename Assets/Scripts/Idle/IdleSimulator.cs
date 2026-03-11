using System.Collections.Generic;
using UnityEngine;
using KeepItTogether.Core;

namespace KeepItTogether.Idle
{
    /// <summary>
    /// Simulates what happened while the player was away.
    /// Uses deterministic time-delta calculations — no randomness in offline sim.
    /// </summary>
    public static class IdleSimulator
    {
        /// <summary>
        /// Result of an offline simulation.
        /// </summary>
        public struct SimulationResult
        {
            public float SecondsSimulated;
            public int WavesSimulated;
            public int NPCsLost;
            public int EnemiesKilled;
            public int GoldEarned;
            public float CastleDamageTaken;
            public List<string> EventLog;
        }

        /// <summary>
        /// Run a fast-forward simulation of the given duration.
        /// Uses simplified combat math rather than full game tick.
        /// </summary>
        public static SimulationResult Simulate(
            float seconds,
            int npcCount,
            float avgNPCDamage,
            float avgNPCHP,
            int currentWave,
            float castleHP,
            float offlineEfficiency = 0.5f)
        {
            var result = new SimulationResult
            {
                SecondsSimulated = seconds,
                EventLog = new List<string>()
            };

            float timeRemaining = seconds;
            float waveInterval = 60f; // simplified: one wave per minute
            int wave = currentWave;
            float currentCastleHP = castleHP;
            int npcsAlive = npcCount;
            int totalGold = 0;

            while (timeRemaining > 0f && npcsAlive > 0 && currentCastleHP > 0f)
            {
                float waveDuration = Mathf.Min(waveInterval, timeRemaining);
                timeRemaining -= waveDuration;
                wave++;

                // Simplified wave: calculate enemy count and strength
                int enemyCount = Mathf.RoundToInt(5 * Mathf.Pow(1.15f, wave));
                float enemyHP = 50f * (1f + wave * 0.1f);
                float enemyDamage = 8f * (1f + wave * 0.05f);

                // Simplified combat resolution
                float npcTotalDPS = npcsAlive * avgNPCDamage * offlineEfficiency;
                float enemyTotalDPS = enemyCount * enemyDamage * 0.8f; // enemies are slightly less effective

                float timeToKillAll = (enemyCount * enemyHP) / Mathf.Max(1f, npcTotalDPS);
                float damageToNPCs = enemyTotalDPS * Mathf.Min(timeToKillAll, waveDuration);

                // Distribute damage across NPCs
                int npcsKilled = Mathf.FloorToInt(damageToNPCs / avgNPCHP);
                npcsKilled = Mathf.Min(npcsKilled, npcsAlive);
                npcsAlive -= npcsKilled;
                result.NPCsLost += npcsKilled;

                // Castle takes damage if NPCs are overwhelmed
                if (npcsAlive == 0)
                {
                    float spilloverDamage = enemyTotalDPS * (waveDuration - timeToKillAll);
                    currentCastleHP -= Mathf.Max(0f, spilloverDamage) * 0.5f;
                    result.CastleDamageTaken += spilloverDamage * 0.5f;
                }

                // Gold reward
                int waveGold = Mathf.RoundToInt(50 + wave * 10);
                totalGold += waveGold;
                result.EnemiesKilled += enemyCount;
                result.WavesSimulated++;

                result.EventLog.Add($"Wave {wave}: {enemyCount} enemies — {npcsKilled} NPCs lost, +{waveGold}g");
            }

            result.GoldEarned = totalGold;
            return result;
        }
    }
}
