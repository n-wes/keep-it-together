namespace KeepItTogether.Enemy
{
    /// <summary>
    /// Lifecycle states for enemy instances.
    /// </summary>
    public enum EnemyState
    {
        Spawning,    // Just appeared, brief invulnerability
        Approaching, // Moving toward castle/defenders
        Attacking,   // Engaged in combat
        Retreating,  // Falling back (rare, boss mechanic)
        Dead         // Eliminated
    }
}
