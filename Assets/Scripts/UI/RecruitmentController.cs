using UnityEngine;
using UnityEngine.UI;
using KeepItTogether.Core;
using KeepItTogether.Data;

namespace KeepItTogether.UI
{
    /// <summary>
    /// UI controller for the NPC recruitment interface.
    /// Shows available recruits and lets the player hire them.
    /// </summary>
    public class RecruitmentController : MonoBehaviour
    {
        [SerializeField] private NPC.NPCSpawner _spawner;
        [SerializeField] private Transform _optionsParent;
        [SerializeField] private GameObject _optionPrefab;
        [SerializeField] private Text _costText;
        [SerializeField] private Button _refreshButton;
        [SerializeField] private Text _refreshCostText;

        [Header("Feedback")]
        [SerializeField] private Text _feedbackText;
        [SerializeField] private float _feedbackDuration = 2f;

        private RecruitOptionUI[] _optionUIs;
        private float _feedbackTimer;

        private void Start()
        {
            if (_refreshButton != null)
            {
                _refreshButton.onClick.AddListener(OnRefreshClicked);
            }

            InitializeOptions();
        }

        private void Update()
        {
            UpdateCostDisplay();
            UpdateFeedback();
        }

        private void InitializeOptions()
        {
            if (_spawner == null || _optionPrefab == null || _optionsParent == null) return;

            var options = _spawner.CurrentOptions;
            if (options == null) return;

            _optionUIs = new RecruitOptionUI[options.Length];

            for (int i = 0; i < options.Length; i++)
            {
                var go = Instantiate(_optionPrefab, _optionsParent);
                var ui = go.GetComponent<RecruitOptionUI>();
                if (ui == null) ui = go.AddComponent<RecruitOptionUI>();

                int index = i; // capture for closure
                ui.Initialize(options[i], _spawner.CurrentPersonalities[i], () => OnRecruitClicked(index));
                _optionUIs[i] = ui;
            }
        }

        private void OnRecruitClicked(int index)
        {
            if (_spawner == null) return;

            var npc = _spawner.RecruitNPC(index);
            if (npc != null)
            {
                ShowFeedback($"Recruited {npc.Stats.Name}!");
                RefreshOptionDisplay(index);
            }
            else
            {
                ShowFeedback("Not enough gold!");
            }
        }

        private void OnRefreshClicked()
        {
            if (_spawner == null) return;
            _spawner.RefreshOptions();

            for (int i = 0; i < _optionUIs.Length; i++)
            {
                RefreshOptionDisplay(i);
            }
        }

        private void RefreshOptionDisplay(int index)
        {
            if (_optionUIs == null || index >= _optionUIs.Length) return;
            var options = _spawner.CurrentOptions;
            var personalities = _spawner.CurrentPersonalities;

            if (options != null && index < options.Length)
            {
                _optionUIs[index].Initialize(options[index], personalities[index], () => OnRecruitClicked(index));
            }
        }

        private void UpdateCostDisplay()
        {
            if (_costText == null || _spawner == null) return;
            _costText.text = $"Recruit Cost: {_spawner.GetRecruitCost()}g";
        }

        private void ShowFeedback(string message)
        {
            if (_feedbackText == null) return;
            _feedbackText.text = message;
            _feedbackText.gameObject.SetActive(true);
            _feedbackTimer = _feedbackDuration;
        }

        private void UpdateFeedback()
        {
            if (_feedbackText == null || !_feedbackText.gameObject.activeSelf) return;
            _feedbackTimer -= Time.deltaTime;
            if (_feedbackTimer <= 0f)
            {
                _feedbackText.gameObject.SetActive(false);
            }
        }
    }

    /// <summary>
    /// UI for a single recruit option.
    /// </summary>
    public class RecruitOptionUI : MonoBehaviour
    {
        [SerializeField] private Text _nameText;
        [SerializeField] private Text _statsText;
        [SerializeField] private Text _traitsText;
        [SerializeField] private Button _recruitButton;

        /// <summary>
        /// Set up this option with NPC data and a recruit callback.
        /// </summary>
        public void Initialize(NPCData data, PersonalityProfile personality, System.Action onRecruit)
        {
            if (data == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_nameText != null)
                _nameText.text = data.npcName;

            if (_statsText != null)
                _statsText.text = $"HP:{data.maxHP:F0}  ATK:{data.attackDamage:F0}  SPD:{data.attackSpeed:F1}";

            if (_traitsText != null && personality != null)
                _traitsText.text = personality.GetTraitDescription();

            if (_recruitButton != null)
            {
                _recruitButton.onClick.RemoveAllListeners();
                _recruitButton.onClick.AddListener(() => onRecruit?.Invoke());
            }
        }
    }
}
