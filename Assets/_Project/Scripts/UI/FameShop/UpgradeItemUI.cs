using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI.FameShop
{
    /// <summary>
    /// UI component for an upgradeable item in the Fame Shop
    /// </summary>
    public class UpgradeItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Button button;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text nameText;
        [SerializeField] private Text levelText;
        [SerializeField] private Image iconImage;

        [Header("State")]
        [SerializeField] private string itemId;
        [SerializeField] private int currentLevel;
        [SerializeField] private int maxLevel;

        public string ItemId => itemId;
        public int CurrentLevel => currentLevel;

        public event Action<string> OnClicked;

        public void Initialize(string id, string displayName, int level, int max)
        {
            itemId = id;
            currentLevel = level;
            maxLevel = max;

            // Find components if not assigned
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (backgroundImage == null)
            {
                backgroundImage = GetComponent<Image>();
            }

            if (nameText == null)
            {
                var nameTransform = transform.Find("Name");
                if (nameTransform != null)
                {
                    nameText = nameTransform.GetComponent<Text>();
                }
            }

            if (levelText == null)
            {
                var levelTransform = transform.Find("Level");
                if (levelTransform != null)
                {
                    levelText = levelTransform.GetComponent<Text>();
                }
            }

            // Set display values
            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (levelText != null)
            {
                levelText.text = $"Lv.{currentLevel}/{maxLevel}";

                // Color based on level
                if (currentLevel >= maxLevel)
                {
                    levelText.color = Color.green;
                }
                else if (currentLevel > 0)
                {
                    levelText.color = new Color(1f, 0.8f, 0.3f, 1f);
                }
                else
                {
                    levelText.color = Color.gray;
                }
            }

            // Setup button click
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }

            UpdateVisualState();
        }

        public void SetLevel(int level)
        {
            currentLevel = level;

            if (levelText != null)
            {
                levelText.text = $"Lv.{currentLevel}/{maxLevel}";
            }

            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            if (backgroundImage != null)
            {
                if (currentLevel >= maxLevel)
                {
                    // Max level - golden tint
                    backgroundImage.color = new Color(0.3f, 0.3f, 0.15f, 0.9f);
                }
                else if (currentLevel > 0)
                {
                    // Upgraded - slight highlight
                    backgroundImage.color = new Color(0.25f, 0.25f, 0.3f, 0.9f);
                }
                else
                {
                    // Not upgraded
                    backgroundImage.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);
                }
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(itemId);
        }

        public void SetSelected(bool selected)
        {
            if (backgroundImage != null)
            {
                Color baseColor = backgroundImage.color;
                if (selected)
                {
                    backgroundImage.color = new Color(baseColor.r + 0.1f, baseColor.g + 0.1f, baseColor.b + 0.2f, baseColor.a);
                }
                else
                {
                    UpdateVisualState();
                }
            }
        }
    }
}
