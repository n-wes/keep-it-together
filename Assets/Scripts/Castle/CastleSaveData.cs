namespace KeepItTogether.Castle
{
    /// <summary>
    /// Serializable snapshot of castle state for persistence.
    /// </summary>
    [System.Serializable]
    public class CastleSaveData
    {
        public float currentHP;
        public float currentGateHP;
        public int upgradeLevel;
    }
}
