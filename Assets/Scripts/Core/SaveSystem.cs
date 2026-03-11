using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace KeepItTogether.Core
{
    // ── Save data models ─────────────────────────────────────────────

    /// <summary>
    /// Serializable save data for a single NPC.
    /// </summary>
    [Serializable]
    public class NPCSaveData
    {
        public int id;
        public string name;
        public float hp;
        public float hunger;
        public float morale;
        public float experience;
        public List<string> personalityTraits = new List<string>();
        public bool isAlive;
    }

    /// <summary>
    /// Root save data container for the entire game state.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int currentWave;
        public int gold;
        public int totalKills;
        public string lastPlayedTimestamp; // ISO 8601, parsed as DateTime
        public List<NPCSaveData> npcSaveData = new List<NPCSaveData>();
        public float castleHP;

        /// <summary>
        /// Gets <see cref="lastPlayedTimestamp"/> as a <see cref="DateTime"/> (UTC).
        /// </summary>
        public DateTime GetLastPlayedTime()
        {
            if (DateTime.TryParse(lastPlayedTimestamp, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var dt))
            {
                return dt;
            }
            return DateTime.UtcNow;
        }

        /// <summary>
        /// Sets <see cref="lastPlayedTimestamp"/> from a <see cref="DateTime"/>.
        /// </summary>
        public void SetLastPlayedTime(DateTime time)
        {
            lastPlayedTimestamp = time.ToString("o"); // ISO 8601 round-trip format
        }
    }

    // ── Save system ──────────────────────────────────────────────────

    /// <summary>
    /// Static utility for JSON-based local persistence using <see cref="JsonUtility"/>.
    /// </summary>
    public static class SaveSystem
    {
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, Constants.SaveFileName);

        /// <summary>
        /// Serialize and write <paramref name="data"/> to disk.
        /// </summary>
        public static void Save(SaveData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));

            data.SetLastPlayedTime(DateTime.UtcNow);

            try
            {
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(SavePath, json);
                Debug.Log($"[SaveSystem] Game saved to {SavePath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to save: {e.Message}");
            }
        }

        /// <summary>
        /// Load save data from disk. Returns null if no save exists or deserialization fails.
        /// </summary>
        public static SaveData Load()
        {
            if (!HasSave())
            {
                Debug.Log("[SaveSystem] No save file found.");
                return null;
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                var data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log("[SaveSystem] Save loaded successfully.");
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to load save: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Returns true if a save file exists on disk.
        /// </summary>
        public static bool HasSave()
        {
            return File.Exists(SavePath);
        }

        /// <summary>
        /// Delete the save file if it exists.
        /// </summary>
        public static void DeleteSave()
        {
            if (!HasSave()) return;

            try
            {
                File.Delete(SavePath);
                Debug.Log("[SaveSystem] Save file deleted.");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] Failed to delete save: {e.Message}");
            }
        }
    }
}
