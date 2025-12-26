using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI.WorldMap
{
    /// <summary>
    /// UI component for a level button on the world map
    /// </summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image statusIcon;
        [SerializeField] private Text levelNameText;
        [SerializeField] private Text levelNumberText;

        [Header("State")]
        [SerializeField] private string levelId;
        [SerializeField] private WorldMapController.LevelStatus currentStatus;

        private WorldMapController controller;

        public string LevelId => levelId;
        public WorldMapController.LevelStatus Status => currentStatus;

        public event Action<string, WorldMapController.LevelStatus> OnClicked;

        public void Initialize(string id, string displayName, WorldMapController mapController)
        {
            levelId = id;
            controller = mapController;

            // Find components if not assigned
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (statusIcon == null)
            {
                var iconTransform = transform.Find("StatusIcon");
                if (iconTransform != null)
                {
                    statusIcon = iconTransform.GetComponent<Image>();
                }
            }

            if (levelNameText == null)
            {
                var labelTransform = transform.Find("Label");
                if (labelTransform != null)
                {
                    levelNameText = labelTransform.GetComponent<Text>();
                }
            }

            // Set display name
            if (levelNameText != null)
            {
                levelNameText.text = displayName ?? id;
            }

            // Setup button click
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        public void UpdateStatus(WorldMapController.LevelStatus status, Color color, Sprite icon)
        {
            currentStatus = status;

            // Update background color
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }

            // Update status icon
            if (statusIcon != null)
            {
                if (icon != null)
                {
                    statusIcon.sprite = icon;
                    statusIcon.gameObject.SetActive(true);
                }
                else
                {
                    // Use text fallback for status
                    UpdateStatusText(status);
                }
            }

            // Update button interactability
            if (button != null)
            {
                button.interactable = status != WorldMapController.LevelStatus.Locked;
            }

            // Update visual appearance for locked state
            UpdateVisualState(status);
        }

        private void UpdateStatusText(WorldMapController.LevelStatus status)
        {
            // If no icon sprite, show text indicator
            var textComponent = statusIcon?.GetComponent<Text>();
            if (textComponent == null && statusIcon != null)
            {
                // Create text on the icon object
                var textGO = new GameObject("StatusText");
                textGO.transform.SetParent(statusIcon.transform.parent, false);
                var rect = textGO.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.sizeDelta = new Vector2(50, 50);
                rect.anchoredPosition = statusIcon.rectTransform.anchoredPosition;

                textComponent = textGO.AddComponent<Text>();
                textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                textComponent.fontSize = 36;
                textComponent.fontStyle = FontStyle.Bold;
                textComponent.alignment = TextAnchor.MiddleCenter;
            }

            if (textComponent != null)
            {
                switch (status)
                {
                    case WorldMapController.LevelStatus.Locked:
                        textComponent.text = "🔒";
                        textComponent.color = Color.gray;
                        break;
                    case WorldMapController.LevelStatus.Unlocked:
                        textComponent.text = "⚔";
                        textComponent.color = Color.yellow;
                        break;
                    case WorldMapController.LevelStatus.Completed:
                        textComponent.text = "✓";
                        textComponent.color = Color.green;
                        break;
                    case WorldMapController.LevelStatus.Perfect:
                        textComponent.text = "★";
                        textComponent.color = Color.yellow;
                        break;
                }
            }
        }

        private void UpdateVisualState(WorldMapController.LevelStatus status)
        {
            // Dim locked levels
            CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null && status == WorldMapController.LevelStatus.Locked)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = status == WorldMapController.LevelStatus.Locked ? 0.5f : 1f;
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(levelId, currentStatus);
        }

        /// <summary>
        /// Play animation for level unlock
        /// </summary>
        public void PlayUnlockAnimation()
        {
            // Simple scale punch animation
            StartCoroutine(UnlockAnimationCoroutine());
        }

        private System.Collections.IEnumerator UnlockAnimationCoroutine()
        {
            Vector3 originalScale = transform.localScale;
            Vector3 targetScale = originalScale * 1.2f;

            float duration = 0.3f;
            float elapsed = 0f;

            // Scale up
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, targetScale, t);
                yield return null;
            }

            elapsed = 0f;

            // Scale back down
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(targetScale, originalScale, t);
                yield return null;
            }

            transform.localScale = originalScale;
        }
    }
}
