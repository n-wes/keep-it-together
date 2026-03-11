using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KeepItTogether.Core;

namespace KeepItTogether.UI
{
    /// <summary>
    /// Displays a scrollable list of all NPCs and their current stats.
    /// </summary>
    public class NPCPanelController : MonoBehaviour
    {
        [SerializeField] private Transform _npcListParent;
        [SerializeField] private GameObject _npcEntryPrefab;
        [SerializeField] private Text _panelTitle;

        private readonly List<NPCEntryUI> _entries = new List<NPCEntryUI>();
        private float _refreshTimer;
        private const float _refreshInterval = 0.5f;

        private void Update()
        {
            _refreshTimer += Time.deltaTime;
            if (_refreshTimer < _refreshInterval) return;
            _refreshTimer = 0f;

            RefreshList();
        }

        private void RefreshList()
        {
            if (NPC.NPCManager.Instance == null) return;

            var npcs = NPC.NPCManager.Instance.GetAllAliveNPCs();

            if (_panelTitle != null)
            {
                _panelTitle.text = $"Defenders ({npcs.Count})";
            }

            // Ensure we have enough entry UI elements
            while (_entries.Count < npcs.Count && _npcEntryPrefab != null && _npcListParent != null)
            {
                var go = Instantiate(_npcEntryPrefab, _npcListParent);
                var entry = go.GetComponent<NPCEntryUI>();
                if (entry == null) entry = go.AddComponent<NPCEntryUI>();
                _entries.Add(entry);
            }

            // Update entries
            for (int i = 0; i < _entries.Count; i++)
            {
                if (i < npcs.Count)
                {
                    _entries[i].gameObject.SetActive(true);
                    _entries[i].UpdateDisplay(npcs[i]);
                }
                else
                {
                    _entries[i].gameObject.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// Individual NPC entry in the NPC panel list.
    /// </summary>
    public class NPCEntryUI : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Slider _hpBar;
        [SerializeField] private Slider _hungerBar;
        [SerializeField] private Slider _moraleBar;
        [SerializeField] private Text _stateText;
        [SerializeField] private Text _traitsText;

        /// <summary>
        /// Update the display with current NPC data.
        /// </summary>
        public void UpdateDisplay(NPC.NPC npc)
        {
            if (npc == null || npc.Stats == null) return;

            if (_nameText != null)
                _nameText.text = $"{npc.Stats.Name} (Lv.{npc.Stats.Level})";

            if (_hpBar != null)
                _hpBar.value = npc.Stats.HPPercent;

            if (_hungerBar != null)
                _hungerBar.value = npc.Stats.HungerPercent;

            if (_moraleBar != null)
                _moraleBar.value = npc.Stats.MoralePercent;

            if (_stateText != null)
                _stateText.text = npc.CurrentState.ToString();

            if (_traitsText != null && npc.Stats.Personality != null)
                _traitsText.text = npc.Stats.Personality.GetTraitDescription();
        }
    }
}
