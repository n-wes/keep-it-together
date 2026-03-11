using UnityEngine;

namespace KeepItTogether.Data
{
    /// <summary>
    /// ScriptableObject defining an NPC archetype with base stats, needs, and personality.
    /// </summary>
    [CreateAssetMenu(fileName = "NewNPCData", menuName = "KeepItTogether/NPC Data")]
    public class NPCData : ScriptableObject
    {
        [Header("Identity")]
        public string npcName;
        [TextArea] public string description;
        // public Sprite portrait;  // uncomment when art exists

        [Header("Base Stats")]
        public float maxHP = 100f;
        public float attackDamage = 10f;
        public float attackSpeed = 1f;   // attacks per second
        public float moveSpeed = 3f;
        public float attackRange = 1.5f;

        [Header("Needs Thresholds")]
        public float maxHunger = 100f;
        public float maxMorale = 100f;
        public float maxRest = 100f;

        [Header("Decay Rates (per second)")]
        public float hungerDecayRate = 0.01f;
        public float moraleDecayRate = 0.005f;
        public float restDecayRate = 0.008f;

        [Header("Cost")]
        public int recruitCost = 50;

        [Header("Personality")]
        public PersonalityProfile defaultPersonality;
    }
}
