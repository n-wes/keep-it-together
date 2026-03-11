namespace KeepItTogether.Core
{
    /// <summary>
    /// Game-wide constants for layers, tags, sorting layers, save settings, and balance defaults.
    /// </summary>
    public static class Constants
    {
        // ── Layers ───────────────────────────────────────────────────
        public const int NPCLayer = 8;
        public const int EnemyLayer = 9;
        public const int CastleLayer = 10;

        // ── Tags ─────────────────────────────────────────────────────
        public const string NPCTag = "NPC";
        public const string EnemyTag = "Enemy";
        public const string CastleTag = "Castle";
        public const string DefensePositionTag = "DefensePosition";

        // ── Sorting layers ───────────────────────────────────────────
        public const string BackgroundSortLayer = "Background";
        public const string CastleSortLayer = "Castle";
        public const string CharactersSortLayer = "Characters";
        public const string ProjectilesSortLayer = "Projectiles";
        public const string UISortLayer = "UI";

        // ── Save ─────────────────────────────────────────────────────
        public const string SaveFileName = "save.json";
        public const float AutoSaveInterval = 30f;
        public const float MaxOfflineHours = 8f;

        // ── Game balance defaults ────────────────────────────────────
        public const int StartingGold = 100;
        public const float BaseNPCHungerDecayRate = 0.01f;  // per second
        public const float BaseNPCMoraleDecayRate = 0.005f; // per second
        public const float BaseNPCRestDecayRate = 0.008f;   // per second
    }
}
