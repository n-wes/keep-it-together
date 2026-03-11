using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.Castle
{
    /// <summary>
    /// Singleton manager that owns the <see cref="Castle"/> instance and all
    /// <see cref="DefensePosition"/> nodes. Provides queries for the NPC system
    /// and handles cross-system event wiring.
    /// </summary>
    public class CastleManager : Singleton<CastleManager>
    {
        [Header("References")]
        [SerializeField] private Castle _castle;
        [SerializeField] private Transform _defensePositionsParent;

        [Header("Auto-Repair")]
        [SerializeField] private float _waveRepairPercent = 0.05f;

        private readonly List<DefensePosition> _defensePositions = new List<DefensePosition>();

        // ── Lifecycle ────────────────────────────────────────────────

        protected override void Awake()
        {
            base.Awake();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<CastleDestroyedEvent>(OnCastleDestroyed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<CastleDestroyedEvent>(OnCastleDestroyed);
        }

        // ── Initialization ───────────────────────────────────────────

        /// <summary>
        /// Initializes the castle from data and spawns defense positions.
        /// If no <see cref="Castle"/> component is pre-assigned, one is created
        /// on this GameObject.
        /// </summary>
        public void InitializeCastle(CastleData data)
        {
            if (data == null)
            {
                Debug.LogError("[CastleManager] CastleData is null.");
                return;
            }

            if (_castle == null)
            {
                _castle = gameObject.GetComponent<Castle>();
                if (_castle == null)
                    _castle = gameObject.AddComponent<Castle>();
            }

            _castle.Initialize(data);
            SpawnDefensePositions(data);

            Debug.Log($"[CastleManager] Castle '{data.castleName}' initialized with {_defensePositions.Count} defense positions.");
        }

        /// <summary>
        /// Creates <see cref="DefensePosition"/> GameObjects from the data asset.
        /// </summary>
        public void SpawnDefensePositions(CastleData data)
        {
            // Clear existing positions
            foreach (var pos in _defensePositions)
            {
                if (pos != null) Destroy(pos.gameObject);
            }
            _defensePositions.Clear();

            if (_defensePositionsParent == null)
            {
                var parentGo = new GameObject("DefensePositions");
                parentGo.transform.SetParent(transform);
                parentGo.transform.localPosition = Vector3.zero;
                _defensePositionsParent = parentGo.transform;
            }

            for (int i = 0; i < data.defensePositions.Count; i++)
            {
                var posData = data.defensePositions[i];
                var go = new GameObject($"DefensePosition_{i}");
                go.transform.SetParent(_defensePositionsParent);

                var dp = go.AddComponent<DefensePosition>();
                dp.Initialize(posData);
                _defensePositions.Add(dp);
            }
        }

        // ── Queries ──────────────────────────────────────────────────

        /// <summary>
        /// Returns the first defense position that still has room for another defender.
        /// </summary>
        public DefensePosition GetBestAvailablePosition()
        {
            return _defensePositions.FirstOrDefault(p => p.HasSpace());
        }

        /// <summary>
        /// Returns the closest defense position with available space to the given world point.
        /// </summary>
        public DefensePosition GetNearestPosition(Vector3 fromPosition)
        {
            DefensePosition nearest = null;
            float bestDist = float.MaxValue;

            foreach (var pos in _defensePositions)
            {
                if (!pos.HasSpace()) continue;

                float dist = Vector3.Distance(fromPosition, pos.transform.position);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = pos;
                }
            }

            return nearest;
        }

        /// <summary>Returns all defense positions.</summary>
        public List<DefensePosition> GetAllPositions()
        {
            return new List<DefensePosition>(_defensePositions);
        }

        /// <summary>Returns only positions that have at least one assigned defender.</summary>
        public List<DefensePosition> GetOccupiedPositions()
        {
            return _defensePositions.Where(p => p.CurrentDefenderCount > 0).ToList();
        }

        /// <summary>Returns the active <see cref="Castle"/> component.</summary>
        public Castle GetCastle()
        {
            return _castle;
        }

        /// <summary>True if the castle gate has been breached (gate HP &lt;= 0).</summary>
        public bool IsGateBreached()
        {
            return _castle != null && _castle.CurrentGateHP <= 0f;
        }

        // ── Actions ──────────────────────────────────────────────────

        /// <summary>
        /// Repairs the castle by the specified amount.
        /// </summary>
        public void RepairCastle(float amount)
        {
            _castle?.Repair(amount);
        }

        /// <summary>
        /// Attempts to upgrade the castle using gold from <see cref="GameManager"/>.
        /// </summary>
        /// <returns>True if the upgrade was successful.</returns>
        public bool TryUpgradeCastle()
        {
            if (_castle == null) return false;

            int gold = GameManager.Instance.Gold;
            bool success = _castle.TryUpgrade(ref gold);
            if (success)
            {
                GameManager.Instance.Gold = gold;
            }
            return success;
        }

        // ── Event Handlers ───────────────────────────────────────────

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            if (_castle == null || _castle.IsDestroyed) return;

            float repairAmount = _castle.MaxHP * _waveRepairPercent;
            RepairCastle(repairAmount);
            Debug.Log($"[CastleManager] Post-wave auto-repair: +{repairAmount:F0} HP");
        }

        private void OnCastleDestroyed(CastleDestroyedEvent evt)
        {
            Debug.Log("[CastleManager] Castle destroyed — triggering game over.");
            GameManager.Instance.GameOver();
        }
    }
}
