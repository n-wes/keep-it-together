using UnityEngine;
using UnityEngine.UI;
using KeepItTogether.Core;

namespace KeepItTogether.UI
{
    /// <summary>
    /// Displays wave start/end announcements with animated text.
    /// </summary>
    public class WaveAnnouncerController : MonoBehaviour
    {
        [SerializeField] private Text _announcementText;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _displayDuration = 3f;
        [SerializeField] private float _fadeSpeed = 2f;

        private float _displayTimer;
        private bool _isShowing;

        private void OnEnable()
        {
            EventBus.Subscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Subscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Subscribe<CastleDestroyedEvent>(OnCastleDestroyed);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<WaveStartedEvent>(OnWaveStarted);
            EventBus.Unsubscribe<WaveCompletedEvent>(OnWaveCompleted);
            EventBus.Unsubscribe<CastleDestroyedEvent>(OnCastleDestroyed);
        }

        private void Start()
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 0f;
            }
        }

        private void Update()
        {
            if (!_isShowing) return;

            _displayTimer -= Time.deltaTime;

            if (_displayTimer <= 0f)
            {
                // Fade out
                if (_canvasGroup != null)
                {
                    _canvasGroup.alpha -= _fadeSpeed * Time.deltaTime;
                    if (_canvasGroup.alpha <= 0f)
                    {
                        _isShowing = false;
                    }
                }
                else
                {
                    _isShowing = false;
                    if (_announcementText != null)
                        _announcementText.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Show an announcement with the given text.
        /// </summary>
        public void ShowAnnouncement(string text)
        {
            if (_announcementText != null)
            {
                _announcementText.text = text;
                _announcementText.gameObject.SetActive(true);
            }

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f;
            }

            _displayTimer = _displayDuration;
            _isShowing = true;
        }

        private void OnWaveStarted(WaveStartedEvent evt)
        {
            ShowAnnouncement($"⚔️ Wave {evt.WaveNumber} — {evt.EnemyCount} enemies incoming!");
        }

        private void OnWaveCompleted(WaveCompletedEvent evt)
        {
            ShowAnnouncement($"✅ Wave {evt.WaveNumber} cleared! {evt.SurvivingDefenders} defenders standing.");
        }

        private void OnCastleDestroyed(CastleDestroyedEvent evt)
        {
            ShowAnnouncement("💀 The castle has fallen! Game Over.");
        }
    }
}
