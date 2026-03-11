namespace KeepItTogether.Core
{
    // ── Wave events ──────────────────────────────────────────────────

    public struct WaveStartedEvent
    {
        public int WaveNumber;
        public int EnemyCount;
    }

    public struct WaveCompletedEvent
    {
        public int WaveNumber;
        public int SurvivingDefenders;
    }

    public struct AllWavesClearedEvent { }

    // ── NPC events ───────────────────────────────────────────────────

    public struct NPCSpawnedEvent
    {
        public int NPCId;
        public string NPCName;
    }

    public struct NPCDiedEvent
    {
        public int NPCId;
        public string NPCName;
        public string CauseOfDeath;
    }

    public struct NPCDesertedEvent
    {
        public int NPCId;
        public string NPCName;
        public float Morale;
    }

    public struct NPCRecruitedEvent
    {
        public int NPCId;
        public string NPCName;
    }

    public struct NPCMoraleChangedEvent
    {
        public int NPCId;
        public float OldMorale;
        public float NewMorale;
    }

    // ── Enemy events ─────────────────────────────────────────────────

    public struct EnemySpawnedEvent
    {
        public int EnemyId;
        public string EnemyType;
    }

    public struct EnemyDiedEvent
    {
        public int EnemyId;
        public string EnemyType;
        public int KillerNPCId;
    }

    // ── Castle events ────────────────────────────────────────────────

    public struct CastleDamagedEvent
    {
        public float Damage;
        public float RemainingHP;
    }

    public struct CastleDestroyedEvent { }

    public struct CastleRepairedEvent
    {
        public float Amount;
        public float CurrentHP;
    }

    // ── Combat events ────────────────────────────────────────────────

    public struct CombatStartedEvent
    {
        public int NPCId;
        public int EnemyId;
    }

    public struct CombatEndedEvent
    {
        public int WinnerId;
        public bool WinnerIsNPC;
    }

    public struct DamageDealtEvent
    {
        public int AttackerId;
        public int DefenderId;
        public float Damage;
        public bool AttackerIsNPC;
    }

    // ── Game state events ────────────────────────────────────────────

    public struct GamePausedEvent { }

    public struct GameResumedEvent { }

    public struct OfflineProgressCalculatedEvent
    {
        public float SecondsAway;
        public int WavesSimulated;
    }

    // ── Resource events ──────────────────────────────────────────────

    public struct GoldEarnedEvent
    {
        public int Amount;
        public string Source;
    }

    public struct GoldSpentEvent
    {
        public int Amount;
        public string Purpose;
    }
}
