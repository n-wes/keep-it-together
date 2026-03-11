using System.Collections.Generic;
using UnityEngine;
using KeepItTogether.Data;

namespace KeepItTogether.Castle
{
    /// <summary>
    /// A physical position on or around the castle where NPC defenders can be stationed.
    /// Spawned at runtime from <see cref="DefensePositionData"/> defined in <see cref="CastleData"/>.
    /// </summary>
    public class DefensePosition : MonoBehaviour
    {
        [SerializeField] private float _slotSpacing = 0.6f;

        private DefensePositionData _data;
        private readonly List<Transform> _assignedDefenders = new List<Transform>();

        /// <summary>Maximum number of defenders this position supports.</summary>
        public int MaxDefenders => _data != null ? _data.maxDefenders : 0;

        /// <summary>Bonus attack range granted to defenders at this position.</summary>
        public float RangeBonus => _data != null ? _data.rangeBonus : 0f;

        /// <summary>Damage reduction bonus granted to defenders at this position.</summary>
        public float DefenseBonus => _data != null ? _data.defenseBonus : 0f;

        /// <summary>Number of defenders currently assigned.</summary>
        public int CurrentDefenderCount => _assignedDefenders.Count;

        /// <summary>
        /// Initializes the position from serialized data.
        /// </summary>
        public void Initialize(DefensePositionData data)
        {
            _data = data;
            transform.localPosition = new Vector3(data.localPosition.x, data.localPosition.y, 0f);
            gameObject.tag = KeepItTogether.Core.Constants.DefensePositionTag;
        }

        /// <summary>
        /// Returns true if at least one defender slot is open.
        /// </summary>
        public bool HasSpace()
        {
            return _assignedDefenders.Count < MaxDefenders;
        }

        /// <summary>
        /// Assigns an NPC defender to this position if space is available.
        /// </summary>
        /// <returns>True if the defender was successfully assigned.</returns>
        public bool AssignDefender(Transform npcTransform)
        {
            if (npcTransform == null || !HasSpace() || _assignedDefenders.Contains(npcTransform))
                return false;

            _assignedDefenders.Add(npcTransform);
            return true;
        }

        /// <summary>
        /// Removes a previously assigned defender from this position.
        /// </summary>
        public void RemoveDefender(Transform npcTransform)
        {
            _assignedDefenders.Remove(npcTransform);
        }

        /// <summary>
        /// Returns the world-space stand position for a given defender slot index.
        /// Slots are evenly spaced along the local X axis, centered on this position.
        /// </summary>
        public Vector3 GetStandPosition(int slotIndex)
        {
            int max = Mathf.Max(1, MaxDefenders);
            float totalWidth = (max - 1) * _slotSpacing;
            float startX = -totalWidth * 0.5f;
            float localX = startX + Mathf.Clamp(slotIndex, 0, max - 1) * _slotSpacing;
            return transform.TransformPoint(new Vector3(localX, 0f, 0f));
        }

        /// <summary>
        /// Calculates effective attack range for a defender stationed here.
        /// </summary>
        public float GetEffectiveRange(float baseRange)
        {
            return baseRange + RangeBonus;
        }

        /// <summary>
        /// Returns the damage reduction factor provided by this position.
        /// </summary>
        public float GetDamageReduction()
        {
            return DefenseBonus;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // Position marker
            Gizmos.color = HasSpace() ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);

            // Slot markers
            int slotCount = _data != null ? MaxDefenders : 1;
            Gizmos.color = Color.cyan;
            for (int i = 0; i < slotCount; i++)
            {
                Vector3 slot = GetStandPosition(i);
                Gizmos.DrawWireCube(slot, new Vector3(0.2f, 0.2f, 0.2f));
            }
        }
#endif
    }
}
