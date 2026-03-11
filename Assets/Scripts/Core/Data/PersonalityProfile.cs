using UnityEngine;

namespace KeepItTogether.Data
{
    /// <summary>
    /// Serializable personality data that influences NPC behavior and ML reward functions.
    /// </summary>
    [System.Serializable]
    public class PersonalityProfile
    {
        [Range(0f, 1f)] public float bravery = 0.5f;      // 0=coward, 1=fearless
        [Range(0f, 1f)] public float loyalty = 0.5f;       // 0=self-serving, 1=devoted
        [Range(0f, 1f)] public float aggression = 0.5f;    // 0=passive, 1=bloodthirsty
        [Range(0f, 1f)] public float sociability = 0.5f;   // 0=loner, 1=social butterfly
        [Range(0f, 1f)] public float laziness = 0.5f;      // 0=workaholic, 1=complete slacker
        [Range(0f, 1f)] public float intelligence = 0.5f;  // 0=dim, 1=genius tactician

        /// <summary>
        /// Generates a random personality with values distributed around 0.5 using the given variance.
        /// </summary>
        public static PersonalityProfile GenerateRandom(float variance = 0.3f)
        {
            var profile = new PersonalityProfile
            {
                bravery = RandomTrait(variance),
                loyalty = RandomTrait(variance),
                aggression = RandomTrait(variance),
                sociability = RandomTrait(variance),
                laziness = RandomTrait(variance),
                intelligence = RandomTrait(variance)
            };
            return profile;
        }

        /// <summary>
        /// Returns a human-readable description of the most dominant personality traits.
        /// </summary>
        public string GetTraitDescription()
        {
            var traits = new System.Collections.Generic.List<string>();

            if (bravery > 0.7f) traits.Add("Brave");
            else if (bravery < 0.3f) traits.Add("Cowardly");

            if (loyalty > 0.7f) traits.Add("Loyal");
            else if (loyalty < 0.3f) traits.Add("Self-Serving");

            if (aggression > 0.7f) traits.Add("Aggressive");
            else if (aggression < 0.3f) traits.Add("Passive");

            if (sociability > 0.7f) traits.Add("Social");
            else if (sociability < 0.3f) traits.Add("Loner");

            if (laziness > 0.7f) traits.Add("Lazy");
            else if (laziness < 0.3f) traits.Add("Hardworking");

            if (intelligence > 0.7f) traits.Add("Clever");
            else if (intelligence < 0.3f) traits.Add("Dim");

            if (traits.Count == 0)
                return "Unremarkable";

            // Join with commas, inserting "but" before negative-connotation traits at the end
            return string.Join(", ", traits);
        }

        /// <summary>
        /// Creates a deep clone of this personality profile.
        /// </summary>
        public PersonalityProfile Clone()
        {
            return new PersonalityProfile
            {
                bravery = bravery,
                loyalty = loyalty,
                aggression = aggression,
                sociability = sociability,
                laziness = laziness,
                intelligence = intelligence
            };
        }

        private static float RandomTrait(float variance)
        {
            float value = 0.5f + Random.Range(-variance, variance);
            return Mathf.Clamp01(value);
        }
    }
}
