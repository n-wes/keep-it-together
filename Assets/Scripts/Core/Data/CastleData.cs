using System.Collections.Generic;
using UnityEngine;

namespace KeepItTogether.Data
{
    /// <summary>
    /// Defines a defensive position on the castle where NPCs can be stationed.
    /// </summary>
    [System.Serializable]
    public class DefensePositionData
    {
        public Vector2 localPosition;
        public int maxDefenders = 1;
        public float rangeBonus = 0f;    // bonus attack range at this position
        public float defenseBonus = 0f;  // damage reduction at this position
    }

    /// <summary>
    /// ScriptableObject defining castle stats, defense positions, and upgrade progression.
    /// </summary>
    [CreateAssetMenu(fileName = "NewCastleData", menuName = "KeepItTogether/Castle Data")]
    public class CastleData : ScriptableObject
    {
        [Header("Identity")]
        public string castleName = "The Crumbling Keep";

        [Header("Castle Stats")]
        public float maxHP = 500f;
        public float gateHP = 200f;
        public float wallDefense = 0.1f;  // damage reduction percentage

        [Header("Defense Positions")]
        public List<DefensePositionData> defensePositions = new List<DefensePositionData>();

        [Header("Upgrades")]
        public int maxUpgradeLevel = 10;
        public float hpPerUpgrade = 50f;
        public float defensePerUpgrade = 0.02f;
        public int baseUpgradeCost = 100;
        public float upgradeCostMultiplier = 1.5f;  // each level costs more

        /// <summary>
        /// Calculates the gold cost to upgrade from the given level to the next.
        /// </summary>
        public int GetUpgradeCost(int currentLevel)
        {
            return Mathf.RoundToInt(baseUpgradeCost * Mathf.Pow(upgradeCostMultiplier, currentLevel));
        }
    }
}
