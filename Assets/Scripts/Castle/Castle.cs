using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.Castle
{
    /// <summary>
    /// The main castle entity. Tracks hit-points, gate integrity, upgrades,
    /// and publishes events when damaged, destroyed, or repaired.
    /// </summary>
    public class Castle : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private CastleData _castleData;

        [Header("Runtime State")]
        [SerializeField] private float _currentHP;
        [SerializeField] private float _currentGateHP;
        [SerializeField] private int _upgradeLevel;
        [SerializeField] private bool _isDestroyed;

        // ── Properties ───────────────────────────────────────────────

        /// <summary>Current hit-points of the castle walls.</summary>
        public float CurrentHP => _currentHP;

        /// <summary>Maximum HP including upgrade bonuses.</summary>
        public float MaxHP => _castleData.maxHP + _upgradeLevel * _castleData.hpPerUpgrade;

        /// <summary>Normalized HP (0-1).</summary>
        public float HPPercent => MaxHP > 0f ? Mathf.Clamp01(_currentHP / MaxHP) : 0f;

        /// <summary>Current gate hit-points.</summary>
        public float CurrentGateHP => _currentGateHP;

        /// <summary>Maximum gate HP (not affected by upgrades).</summary>
        public float GateMaxHP => _castleData.gateHP;

        /// <summary>Current upgrade level.</summary>
        public int UpgradeLevel => _upgradeLevel;

        /// <summary>True if the castle has been destroyed.</summary>
        public bool IsDestroyed => _isDestroyed;

        /// <summary>
        /// Total damage reduction factor: base wall defense + per-upgrade bonus.
        /// </summary>
        public float CurrentDefense =>
            _castleData.wallDefense + _upgradeLevel * _castleData.defensePerUpgrade;

        // ── Initialization ───────────────────────────────────────────

        /// <summary>
        /// Sets up the castle from its data asset, resetting HP to max.
        /// </summary>
        public void Initialize(CastleData data)
        {
            _castleData = data;
            _upgradeLevel = 0;
            _currentHP = MaxHP;
            _currentGateHP = data.gateHP;
            _isDestroyed = false;
            gameObject.tag = Constants.CastleTag;
        }

        // ── Damage ───────────────────────────────────────────────────

        /// <summary>
        /// Applies damage to the castle walls after defense reduction.
        /// Publishes <see cref="CastleDamagedEvent"/> and, if HP reaches zero,
        /// <see cref="CastleDestroyedEvent"/>.
        /// </summary>
        public void TakeDamage(float rawDamage)
        {
            if (_isDestroyed || rawDamage <= 0f) return;

            float mitigated = rawDamage * Mathf.Clamp01(1f - CurrentDefense);
            _currentHP = Mathf.Max(0f, _currentHP - mitigated);

            EventBus.Publish(new CastleDamagedEvent
            {
                Damage = mitigated,
                RemainingHP = _currentHP
            });

            if (_currentHP <= 0f)
            {
                HandleDestruction();
            }
        }

        /// <summary>
        /// Applies damage directly to the castle gate (bypasses wall defense).
        /// When the gate reaches zero HP, it is considered breached.
        /// </summary>
        public void TakeGateDamage(float rawDamage)
        {
            if (_isDestroyed || rawDamage <= 0f) return;

            _currentGateHP = Mathf.Max(0f, _currentGateHP - rawDamage);

            EventBus.Publish(new CastleDamagedEvent
            {
                Damage = rawDamage,
                RemainingHP = _currentHP
            });
        }

        // ── Repair ───────────────────────────────────────────────────

        /// <summary>
        /// Restores HP up to the current maximum and publishes a
        /// <see cref="CastleRepairedEvent"/>.
        /// </summary>
        public void Repair(float amount)
        {
            if (_isDestroyed || amount <= 0f) return;

            float before = _currentHP;
            _currentHP = Mathf.Min(MaxHP, _currentHP + amount);
            float healed = _currentHP - before;

            if (healed > 0f)
            {
                EventBus.Publish(new CastleRepairedEvent
                {
                    Amount = healed,
                    CurrentHP = _currentHP
                });
            }
        }

        // ── Upgrades ─────────────────────────────────────────────────

        /// <summary>
        /// Attempts to upgrade the castle. Deducts gold by reference and
        /// publishes <see cref="GoldSpentEvent"/> on success.
        /// </summary>
        /// <returns>True if the upgrade was purchased.</returns>
        public bool TryUpgrade(ref int gold)
        {
            if (_isDestroyed) return false;
            if (_upgradeLevel >= _castleData.maxUpgradeLevel) return false;

            int cost = GetUpgradeCost();
            if (gold < cost) return false;

            gold -= cost;
            _upgradeLevel++;

            // Increase current HP by the per-upgrade amount so the player benefits immediately
            _currentHP = Mathf.Min(MaxHP, _currentHP + _castleData.hpPerUpgrade);

            EventBus.Publish(new GoldSpentEvent
            {
                Amount = cost,
                Purpose = "Castle Upgrade"
            });

            return true;
        }

        /// <summary>
        /// Returns the gold cost for the next upgrade level.
        /// </summary>
        public int GetUpgradeCost()
        {
            return _castleData.GetUpgradeCost(_upgradeLevel);
        }

        // ── Save / Load ──────────────────────────────────────────────

        /// <summary>
        /// Captures current state into a serializable snapshot.
        /// </summary>
        public CastleSaveData ToSaveData()
        {
            return new CastleSaveData
            {
                currentHP = _currentHP,
                currentGateHP = _currentGateHP,
                upgradeLevel = _upgradeLevel
            };
        }

        /// <summary>
        /// Restores state from a previously captured snapshot.
        /// </summary>
        public void LoadFromSaveData(CastleSaveData data)
        {
            if (data == null) return;

            _upgradeLevel = data.upgradeLevel;
            _currentHP = Mathf.Min(data.currentHP, MaxHP);
            _currentGateHP = Mathf.Min(data.currentGateHP, GateMaxHP);
            _isDestroyed = _currentHP <= 0f;
        }

        // ── Internal ─────────────────────────────────────────────────

        private void HandleDestruction()
        {
            _isDestroyed = true;
            EventBus.Publish(new CastleDestroyedEvent());
            Debug.Log("[Castle] The castle has fallen!");
        }
    }
}
