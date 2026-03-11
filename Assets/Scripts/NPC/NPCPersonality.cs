using UnityEngine;
using KeepItTogether.Data;

namespace KeepItTogether.NPC
{
    /// <summary>
    /// Translates a <see cref="PersonalityProfile"/> into concrete behavioral decisions.
    /// Pure logic class — no MonoBehaviour dependency.
    /// </summary>
    public class NPCPersonality
    {
        private readonly PersonalityProfile _profile;

        public NPCPersonality(PersonalityProfile profile)
        {
            _profile = profile ?? new PersonalityProfile();
        }

        // ── Decision methods ─────────────────────────────────────────

        /// <summary>
        /// Determines whether the NPC should flee combat.
        /// Cowards flee earlier; the brave hold the line.
        /// </summary>
        public bool ShouldFlee(float currentHP, float maxHP, float moralePercent)
        {
            if (maxHP <= 0f) return true;

            float hpPercent = currentHP / maxHP;
            // Bravery 0 → flee at 50% HP; Bravery 1 → flee at 10% HP
            float hpFleeThreshold = Mathf.Lerp(0.5f, 0.1f, _profile.bravery);
            // Bravery 0 → flee at 40% morale; Bravery 1 → flee at 10% morale
            float moraleFleeThreshold = Mathf.Lerp(0.4f, 0.1f, _profile.bravery);

            return hpPercent < hpFleeThreshold || moralePercent < moraleFleeThreshold;
        }

        /// <summary>
        /// Determines whether the NPC should desert (permanently leave).
        /// Low-loyalty NPCs desert at higher morale thresholds.
        /// </summary>
        public bool ShouldDesert(float moralePercent)
        {
            // Loyalty 0 → desert at 30% morale; Loyalty 1 → desert at 5% morale
            float desertThreshold = Mathf.Lerp(0.3f, 0.05f, _profile.loyalty);
            return moralePercent < desertThreshold;
        }

        /// <summary>
        /// Returns a multiplier (0.5–1.5) for how eagerly the NPC seeks combat.
        /// Aggressive NPCs hit harder and engage faster.
        /// </summary>
        public float GetAggressionModifier()
        {
            return Mathf.Lerp(0.5f, 1.5f, _profile.aggression);
        }

        /// <summary>
        /// Returns a morale decay multiplier. Sociable NPCs lose morale more slowly
        /// (they cope better in a group).
        /// </summary>
        public float GetMoraleDecayModifier()
        {
            // Sociability 0 → 1.3× decay; Sociability 1 → 0.7× decay
            return Mathf.Lerp(1.3f, 0.7f, _profile.sociability);
        }

        /// <summary>
        /// Returns a rest decay multiplier. Lazy NPCs burn through rest faster.
        /// </summary>
        public float GetRestDecayModifier()
        {
            // Laziness 0 → 0.7× decay; Laziness 1 → 1.5× decay
            return Mathf.Lerp(0.7f, 1.5f, _profile.laziness);
        }

        // ── Flavor text ──────────────────────────────────────────────

        private static readonly string[] BraveBattleCries = new[]
        {
            "FOR THE CASTLE!",
            "I didn't sign up for this... oh wait, I did.",
            "Stand your ground! I'll hold the line!",
            "Death before dishonor! ...preferably theirs."
        };

        private static readonly string[] CowardlyBattleCries = new[]
        {
            "I'll fight from back here, thanks!",
            "BEHIND you, not behind ME!",
            "Is it too late to call in sick?",
            "I'm not running, it's a tactical repositioning!"
        };

        private static readonly string[] LazyBattleCries = new[]
        {
            "*yawn* Fine, I'll fight.",
            "Can we wrap this up quickly? I had plans to nap.",
            "Do I HAVE to?",
            "Someone else started this, right?"
        };

        private static readonly string[] AggressiveBattleCries = new[]
        {
            "BLOOD FOR THE KING!",
            "Finally! VIOLENCE!",
            "Come closer so I can hit you HARDER!",
            "Is that all you've got? SEND MORE!"
        };

        private static readonly string[] SmartBattleCries = new[]
        {
            "I've calculated a 73% chance of victory. Acceptable.",
            "Exploiting weakness in 3... 2... 1...",
            "Their formation has a gap. How convenient.",
            "Statistically, they should surrender now."
        };

        private static readonly string[] GenericBattleCries = new[]
        {
            "Here we go!",
            "For glory!",
            "Charge!",
            "Let's get this over with!"
        };

        /// <summary>
        /// Returns a personality-appropriate battle cry. Fun and humorous.
        /// </summary>
        public string GetBattleCry()
        {
            float maxTrait = Mathf.Max(_profile.bravery, _profile.aggression,
                Mathf.Max(_profile.laziness, _profile.intelligence));

            string[] pool;

            if (_profile.bravery < 0.3f)
                pool = CowardlyBattleCries;
            else if (Mathf.Approximately(maxTrait, _profile.aggression) && _profile.aggression > 0.6f)
                pool = AggressiveBattleCries;
            else if (Mathf.Approximately(maxTrait, _profile.laziness) && _profile.laziness > 0.6f)
                pool = LazyBattleCries;
            else if (Mathf.Approximately(maxTrait, _profile.intelligence) && _profile.intelligence > 0.6f)
                pool = SmartBattleCries;
            else if (_profile.bravery > 0.6f)
                pool = BraveBattleCries;
            else
                pool = GenericBattleCries;

            return pool[Random.Range(0, pool.Length)];
        }

        // ── Complaints ───────────────────────────────────────────────

        private static readonly string[] BraveComplaints = new[]
        {
            "I didn't sign up for this... oh wait, I did.",
            "Morale is low, but my resolve is not!",
            "We need better supplies. For honor!",
            "I'd complain, but that's not very brave of me."
        };

        private static readonly string[] CowardlyComplaints = new[]
        {
            "Can we maybe just... surrender?",
            "I heard the enemy has a dental plan...",
            "Is there a complaint box? I have several.",
            "My retirement plan did NOT include this."
        };

        private static readonly string[] LazyComplaints = new[]
        {
            "Is it naptime yet?",
            "Can someone else be the hero today?",
            "I need a break. And a pillow. And a bed.",
            "Defending castles is exhausting. Can we just... not?"
        };

        private static readonly string[] AggressiveComplaints = new[]
        {
            "Not enough things to hit.",
            "I'M HUNGRY AND ANGRY. HANGRY.",
            "If I don't get food soon, I'm eating the furniture.",
            "Everything is terrible and I want to punch it."
        };

        private static readonly string[] SmartComplaints = new[]
        {
            "According to my calculations, we're 94% doomed.",
            "The supply chain logistics here are appalling.",
            "I've written a formal complaint. In triplicate.",
            "This operation is woefully under-resourced."
        };

        private static readonly string[] GenericComplaints = new[]
        {
            "Things could be better around here.",
            "I've had better days.",
            "At least the view is nice... kind of.",
            "Are we getting paid for this?"
        };

        /// <summary>
        /// Returns a personality-appropriate complaint when needs are low.
        /// </summary>
        public string GetComplaint()
        {
            float maxTrait = Mathf.Max(_profile.bravery, _profile.aggression,
                Mathf.Max(_profile.laziness, _profile.intelligence));

            string[] pool;

            if (_profile.bravery < 0.3f)
                pool = CowardlyComplaints;
            else if (Mathf.Approximately(maxTrait, _profile.aggression) && _profile.aggression > 0.6f)
                pool = AggressiveComplaints;
            else if (Mathf.Approximately(maxTrait, _profile.laziness) && _profile.laziness > 0.6f)
                pool = LazyComplaints;
            else if (Mathf.Approximately(maxTrait, _profile.intelligence) && _profile.intelligence > 0.6f)
                pool = SmartComplaints;
            else if (_profile.bravery > 0.6f)
                pool = BraveComplaints;
            else
                pool = GenericComplaints;

            return pool[Random.Range(0, pool.Length)];
        }
    }
}
