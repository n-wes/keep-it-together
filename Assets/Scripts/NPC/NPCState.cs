namespace KeepItTogether.NPC
{
    /// <summary>
    /// All possible behavioral states for an NPC in the castle defense.
    /// </summary>
    public enum NPCState
    {
        Idle,       // No threats, hanging around
        Moving,     // Moving to a position
        Fighting,   // Engaged in combat
        Resting,    // Taking a break (low rest)
        Eating,     // Eating (low hunger)
        Fleeing,    // Running away (low morale + cowardly)
        Deserting,  // Leaving permanently (critical morale)
        Dead        // RIP
    }
}
