using System;
using UnityEngine;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.NPC
{
    /// <summary>
    /// Runtime stats container for a living NPC. Separate from <see cref="NPCData"/>
    /// (the ScriptableObject template) — this holds mutable per-instance values.
    /// </summary>
    [Serializable]
    public class NPCStats
    {
        // ── Identity ─────────────────────────────────────────────────
        public int Id { get; private set; }
        public string Name { get; private set; }

        // ── Health ───────────────────────────────────────────────────
        public float CurrentHP { get; private set; }
        public float MaxHP { get; private set; }

        // ── Needs (start full, decay toward 0) ──────────────────────
        public float CurrentHunger { get; private set; }
        public float MaxHunger { get; private set; }
        public float CurrentMorale { get; private set; }
        public float MaxMorale { get; private set; }
        public float CurrentRest { get; private set; }
        public float MaxRest { get; private set; }

        // ── Decay rates (per second, from NPCData) ──────────────────
        public float HungerDecayRate { get; private set; }
        public float MoraleDecayRate { get; private set; }
        public float RestDecayRate { get; private set; }

        // ── Combat ───────────────────────────────────────────────────
        public float AttackDamage { get; private set; }
        public float AttackSpeed { get; private set; }
        public float MoveSpeed { get; private set; }
        public float AttackRange { get; private set; }

        // ── Progression ──────────────────────────────────────────────
        public float Experience { get; private set; }
        public float Level { get; private set; }

        // ── Personality ──────────────────────────────────────────────
        public PersonalityProfile Personality { get; private set; }

        // ── State ────────────────────────────────────────────────────
        public bool IsAlive { get; private set; }
        public NPCState State { get; set; }

        private const float XP_PER_LEVEL = 100f;
        private const float STAT_GAIN_PER_LEVEL = 0.05f; // 5% per level

        // ── Initialization ───────────────────────────────────────────

        /// <summary>
        /// Populate all runtime stats from an <see cref="NPCData"/> ScriptableObject.
        /// </summary>
        public void InitializeFromData(NPCData data, int id)
        {
            Id = id;
            Name = data.npcName;

            MaxHP = data.maxHP;
            CurrentHP = MaxHP;

            MaxHunger = data.maxHunger;
            CurrentHunger = MaxHunger;

            MaxMorale = data.maxMorale;
            CurrentMorale = MaxMorale;

            MaxRest = data.maxRest;
            CurrentRest = MaxRest;

            HungerDecayRate = data.hungerDecayRate;
            MoraleDecayRate = data.moraleDecayRate;
            RestDecayRate = data.restDecayRate;

            AttackDamage = data.attackDamage;
            AttackSpeed = data.attackSpeed;
            MoveSpeed = data.moveSpeed;
            AttackRange = data.attackRange;

            Experience = 0f;
            Level = 1f;

            Personality = data.defaultPersonality != null
                ? data.defaultPersonality.Clone()
                : new PersonalityProfile();

            IsAlive = true;
            State = NPCState.Idle;
        }

        // ── Health ───────────────────────────────────────────────────

        /// <summary>
        /// Apply damage to this NPC. Fires <see cref="DamageDealtEvent"/> and
        /// transitions to <see cref="NPCState.Dead"/> if HP reaches zero.
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            float effectiveDamage = Mathf.Max(0f, damage);
            CurrentHP = Mathf.Max(0f, CurrentHP - effectiveDamage);

            if (CurrentHP <= 0f)
            {
                Die("combat");
            }
        }

        /// <summary>
        /// Restore HP up to <see cref="MaxHP"/>.
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;
            CurrentHP = Mathf.Min(MaxHP, CurrentHP + Mathf.Abs(amount));
        }

        // ── Needs ────────────────────────────────────────────────────

        /// <summary>
        /// Adjust hunger by <paramref name="delta"/>. Negative values deplete hunger.
        /// </summary>
        public void ModifyHunger(float delta)
        {
            if (!IsAlive) return;
            CurrentHunger = Mathf.Clamp(CurrentHunger + delta, 0f, MaxHunger);
        }

        /// <summary>
        /// Adjust morale by <paramref name="delta"/>. Fires <see cref="NPCMoraleChangedEvent"/>.
        /// </summary>
        public void ModifyMorale(float delta)
        {
            if (!IsAlive) return;

            float oldMorale = CurrentMorale;
            CurrentMorale = Mathf.Clamp(CurrentMorale + delta, 0f, MaxMorale);

            if (!Mathf.Approximately(oldMorale, CurrentMorale))
            {
                EventBus.Publish(new NPCMoraleChangedEvent
                {
                    NPCId = Id,
                    OldMorale = oldMorale,
                    NewMorale = CurrentMorale
                });
            }
        }

        /// <summary>
        /// Adjust rest by <paramref name="delta"/>. Negative values deplete rest.
        /// </summary>
        public void ModifyRest(float delta)
        {
            if (!IsAlive) return;
            CurrentRest = Mathf.Clamp(CurrentRest + delta, 0f, MaxRest);
        }

        /// <summary>
        /// Restore hunger (e.g., from a meal).
        /// </summary>
        public void Feed(float amount)
        {
            ModifyHunger(Mathf.Abs(amount));
        }

        /// <summary>
        /// Restore rest (e.g., from sleeping).
        /// </summary>
        public void Rest(float amount)
        {
            ModifyRest(Mathf.Abs(amount));
        }

        // ── Progression ──────────────────────────────────────────────

        /// <summary>
        /// Award experience and check for level-ups. Each level boosts base stats by 5%.
        /// </summary>
        public void AddExperience(float xp)
        {
            if (!IsAlive) return;

            Experience += Mathf.Abs(xp);

            while (Experience >= Level * XP_PER_LEVEL)
            {
                Experience -= Level * XP_PER_LEVEL;
                Level++;
                ApplyLevelUp();
            }
        }

        // ── Combat effectiveness ─────────────────────────────────────

        /// <summary>
        /// Returns a 0–1 multiplier reflecting how well this NPC can fight right now.
        /// Hungry, tired, or demoralized NPCs are less effective.
        /// </summary>
        public float GetCombatEffectiveness()
        {
            if (!IsAlive) return 0f;

            float hungerFactor = MaxHunger > 0f ? CurrentHunger / MaxHunger : 1f;
            float moraleFactor = MaxMorale > 0f ? CurrentMorale / MaxMorale : 1f;
            float restFactor = MaxRest > 0f ? CurrentRest / MaxRest : 1f;

            // Weight: morale counts more than hunger/rest in combat
            float raw = (hungerFactor * 0.25f) + (moraleFactor * 0.4f) + (restFactor * 0.35f);
            return Mathf.Clamp01(raw);
        }

        // ── Save / Load ──────────────────────────────────────────────

        /// <summary>
        /// Serialize this NPC's state into a save-friendly struct.
        /// </summary>
        public NPCSaveData ToSaveData()
        {
            var save = new NPCSaveData
            {
                id = Id,
                name = Name,
                hp = CurrentHP,
                hunger = CurrentHunger,
                morale = CurrentMorale,
                experience = Experience,
                isAlive = IsAlive
            };

            if (Personality != null)
            {
                save.personalityTraits = new System.Collections.Generic.List<string>
                {
                    Personality.bravery.ToString("F3"),
                    Personality.loyalty.ToString("F3"),
                    Personality.aggression.ToString("F3"),
                    Personality.sociability.ToString("F3"),
                    Personality.laziness.ToString("F3"),
                    Personality.intelligence.ToString("F3")
                };
            }

            return save;
        }

        /// <summary>
        /// Restore NPC state from save data. Call after <see cref="InitializeFromData"/>
        /// to override the template defaults with saved values.
        /// </summary>
        public void LoadFromSaveData(NPCSaveData data)
        {
            Id = data.id;
            Name = data.name;
            CurrentHP = data.hp;
            CurrentHunger = data.hunger;
            CurrentMorale = data.morale;
            Experience = data.experience;
            IsAlive = data.isAlive;
            State = IsAlive ? NPCState.Idle : NPCState.Dead;

            if (data.personalityTraits != null && data.personalityTraits.Count >= 6)
            {
                Personality = new PersonalityProfile
                {
                    bravery = float.Parse(data.personalityTraits[0]),
                    loyalty = float.Parse(data.personalityTraits[1]),
                    aggression = float.Parse(data.personalityTraits[2]),
                    sociability = float.Parse(data.personalityTraits[3]),
                    laziness = float.Parse(data.personalityTraits[4]),
                    intelligence = float.Parse(data.personalityTraits[5])
                };
            }
        }

        // ── Internal ─────────────────────────────────────────────────

        private void Die(string cause)
        {
            IsAlive = false;
            CurrentHP = 0f;
            State = NPCState.Dead;

            EventBus.Publish(new NPCDiedEvent
            {
                NPCId = Id,
                NPCName = Name,
                CauseOfDeath = cause
            });
        }

        private void ApplyLevelUp()
        {
            MaxHP += MaxHP * STAT_GAIN_PER_LEVEL;
            CurrentHP = MaxHP; // full heal on level up
            AttackDamage += AttackDamage * STAT_GAIN_PER_LEVEL;
            AttackSpeed += AttackSpeed * STAT_GAIN_PER_LEVEL;
            MoveSpeed += MoveSpeed * STAT_GAIN_PER_LEVEL;

            Debug.Log($"[NPCStats] {Name} leveled up to {Level}!");
        }
    }
}
