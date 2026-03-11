using UnityEngine;
using UnityEngine.UI;
using KeepItTogether.Core;

namespace KeepItTogether.UI
{
    /// <summary>
    /// Main HUD displaying wave counter, castle HP, gold, and NPC count.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        [Header("Text References (assign in Inspector or use TMPro)")]
        [SerializeField] private Text _waveText;
        [SerializeField] private Text _goldText;
        [SerializeField] private Text _castleHPText;
        [SerializeField] private Text _npcCountText;
        [SerializeField] private Text _enemyCountText;

        [Header("HP Bar")]
        [SerializeField] private Slider _castleHPBar;

        private void Update()
        {
            UpdateWaveDisplay();
            UpdateGoldDisplay();
            UpdateCastleDisplay();
            UpdateNPCDisplay();
            UpdateEnemyDisplay();
        }

        private void UpdateWaveDisplay()
        {
            if (_waveText == null || GameManager.Instance == null) return;

            var waveManager = Enemy.WaveManager.Instance;
            if (waveManager != null && waveManager.IsWaveActive)
            {
                _waveText.text = $"Wave {waveManager.CurrentWaveNumber}";
            }
            else if (waveManager != null)
            {
                float timeLeft = waveManager.TimeUntilNextWave;
                _waveText.text = $"Next Wave: {timeLeft:F0}s";
            }
            else
            {
                _waveText.text = $"Wave {GameManager.Instance.CurrentWave}";
            }
        }

        private void UpdateGoldDisplay()
        {
            if (_goldText == null || GameManager.Instance == null) return;
            _goldText.text = $"{GameManager.Instance.Gold}g";
        }

        private void UpdateCastleDisplay()
        {
            if (Castle.CastleManager.Instance == null) return;
            var castle = Castle.CastleManager.Instance.GetCastle();
            if (castle == null) return;

            if (_castleHPText != null)
            {
                _castleHPText.text = $"Castle: {castle.CurrentHP:F0}/{castle.MaxHP:F0}";
            }
            if (_castleHPBar != null)
            {
                _castleHPBar.value = castle.HPPercent;
            }
        }

        private void UpdateNPCDisplay()
        {
            if (_npcCountText == null) return;
            int count = NPC.NPCManager.Instance != null ? NPC.NPCManager.Instance.AliveCount : 0;
            _npcCountText.text = $"Defenders: {count}";
        }

        private void UpdateEnemyDisplay()
        {
            if (_enemyCountText == null) return;
            var waveManager = Enemy.WaveManager.Instance;
            int remaining = waveManager != null ? waveManager.EnemiesRemaining : 0;
            _enemyCountText.text = $"Enemies: {remaining}";
        }
    }
}
