using System;
using TowerConquest.Debug;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    /// <summary>
    /// Countdown Timer that displays at level start (5 seconds)
    /// Blocks all gameplay until countdown completes
    /// </summary>
    public class BattleCountdownTimer : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject countdownPanel;
        [SerializeField] private Text countdownText;
        [SerializeField] private Text countdownLabel;

        [Header("Settings")]
        [SerializeField] private float countdownDuration = 5f;
        [SerializeField] private float startTextDuration = 1f;
        [SerializeField] private string startText = "START!";
        [SerializeField] private string preparingText = "Vorbereitung...";

        [Header("Animation")]
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseSpeed = 5f;

        // State
        private float currentTime;
        private bool isCountingDown;
        private bool hasCompleted;
        private int lastDisplayedSecond = -1;

        // Events
        public event Action OnCountdownComplete;
        public event Action<int> OnSecondTick; // Fires each second with remaining seconds

        public bool IsCountingDown => isCountingDown;
        public bool HasCompleted => hasCompleted;
        public float RemainingTime => Mathf.Max(0f, currentTime);

        private void Awake()
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Start the countdown timer
        /// </summary>
        public void StartCountdown()
        {
            if (isCountingDown) return;

            currentTime = countdownDuration;
            isCountingDown = true;
            hasCompleted = false;
            lastDisplayedSecond = -1;

            if (countdownPanel != null)
            {
                countdownPanel.SetActive(true);
            }

            if (countdownLabel != null)
            {
                countdownLabel.text = preparingText;
            }

            UpdateDisplay();
            Log.Info("[BattleCountdownTimer] Countdown started");
        }

        /// <summary>
        /// Start countdown with custom duration
        /// </summary>
        public void StartCountdown(float duration)
        {
            countdownDuration = duration;
            StartCountdown();
        }

        /// <summary>
        /// Skip the countdown (for debugging)
        /// </summary>
        public void SkipCountdown()
        {
            if (!isCountingDown) return;

            currentTime = 0f;
            CompleteCountdown();
        }

        private void Update()
        {
            if (!isCountingDown) return;

            currentTime -= Time.deltaTime;

            int currentSecond = Mathf.CeilToInt(currentTime);
            if (currentSecond != lastDisplayedSecond && currentSecond > 0)
            {
                lastDisplayedSecond = currentSecond;
                OnSecondTick?.Invoke(currentSecond);

                // Play tick sound here if needed
            }

            UpdateDisplay();

            if (currentTime <= 0f)
            {
                CompleteCountdown();
            }
        }

        private void UpdateDisplay()
        {
            if (countdownText == null) return;

            int displaySeconds = Mathf.CeilToInt(currentTime);

            if (displaySeconds > 0)
            {
                countdownText.text = displaySeconds.ToString();

                // Pulse effect
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.1f * (pulseScale - 1f);
                countdownText.transform.localScale = Vector3.one * pulse;
            }
            else if (!hasCompleted)
            {
                countdownText.text = startText;
                countdownText.transform.localScale = Vector3.one * pulseScale;
            }
        }

        private void CompleteCountdown()
        {
            if (hasCompleted) return;

            isCountingDown = false;
            hasCompleted = true;

            if (countdownText != null)
            {
                countdownText.text = startText;
                countdownText.transform.localScale = Vector3.one * pulseScale;
            }

            Log.Info("[BattleCountdownTimer] Countdown complete!");

            // Hide panel after short delay
            Invoke(nameof(HideCountdownPanel), startTextDuration);

            OnCountdownComplete?.Invoke();
        }

        private void HideCountdownPanel()
        {
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Reset the timer for reuse
        /// </summary>
        public void Reset()
        {
            CancelInvoke(nameof(HideCountdownPanel));

            isCountingDown = false;
            hasCompleted = false;
            currentTime = countdownDuration;
            lastDisplayedSecond = -1;

            if (countdownPanel != null)
            {
                countdownPanel.SetActive(false);
            }

            if (countdownText != null)
            {
                countdownText.transform.localScale = Vector3.one;
            }
        }

        /// <summary>
        /// Create UI elements if not assigned
        /// </summary>
        public void CreateDefaultUI(Transform parent)
        {
            if (countdownPanel != null) return;

            // Create panel
            countdownPanel = new GameObject("CountdownPanel");
            countdownPanel.transform.SetParent(parent, false);

            var panelRect = countdownPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = countdownPanel.AddComponent<Image>();
            panelImage.color = new Color(0, 0, 0, 0.7f);

            // Create countdown text
            GameObject textGO = new GameObject("CountdownText");
            textGO.transform.SetParent(countdownPanel.transform, false);

            var textRect = textGO.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(400, 200);

            countdownText = textGO.AddComponent<Text>();
            countdownText.text = "5";
            countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countdownText.fontSize = 150;
            countdownText.fontStyle = FontStyle.Bold;
            countdownText.alignment = TextAnchor.MiddleCenter;
            countdownText.color = Color.white;

            // Create label
            GameObject labelGO = new GameObject("CountdownLabel");
            labelGO.transform.SetParent(countdownPanel.transform, false);

            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.anchoredPosition = new Vector2(0, 150);
            labelRect.sizeDelta = new Vector2(400, 50);

            countdownLabel = labelGO.AddComponent<Text>();
            countdownLabel.text = preparingText;
            countdownLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            countdownLabel.fontSize = 36;
            countdownLabel.alignment = TextAnchor.MiddleCenter;
            countdownLabel.color = new Color(1f, 0.9f, 0.5f, 1f);

            countdownPanel.SetActive(false);

            Log.Info("[BattleCountdownTimer] Created default UI");
        }
    }
}
