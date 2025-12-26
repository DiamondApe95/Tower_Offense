using System;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    /// <summary>
    /// Individual unit spawn button for the Live Battle HUD
    /// </summary>
    public class UnitSpawnButton : MonoBehaviour
    {
        [Header("UI References")]
        public Button button;
        public Image icon;
        public Text nameText;
        public Text costText;
        public Image cooldownOverlay;
        public Text cooldownText;
        public Image affordableIndicator;

        [Header("Colors")]
        public Color affordableColor = new Color(0.2f, 0.6f, 0.2f, 1f);
        public Color notAffordableColor = new Color(0.6f, 0.2f, 0.2f, 1f);
        public Color cooldownColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);

        public string UnitId { get; private set; }
        public int SlotIndex { get; private set; }
        public int GoldCost { get; private set; }

        public event Action<int> OnClicked;

        private float maxCooldown = 1f;

        public void Initialize(string unitId, string displayName, int cost, int slotIndex)
        {
            UnitId = unitId;
            SlotIndex = slotIndex;
            GoldCost = cost;

            if (nameText != null)
            {
                // Shorten name if too long
                string shortName = displayName.Length > 10 ? displayName.Substring(0, 8) + ".." : displayName;
                nameText.text = shortName;
            }

            if (costText != null)
            {
                costText.text = cost.ToString();
            }

            // Setup button click
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
            }
        }

        private void HandleClick()
        {
            OnClicked?.Invoke(SlotIndex);
        }

        public void UpdateState(bool canSpawn, float cooldownRemaining, int cost, int currentGold)
        {
            GoldCost = cost;

            // Update cost text
            if (costText != null)
            {
                costText.text = cost.ToString();
                costText.color = currentGold >= cost ? affordableColor : notAffordableColor;
            }

            // Update button interactability
            if (button != null)
            {
                button.interactable = canSpawn;
            }

            // Update cooldown overlay
            if (cooldownOverlay != null)
            {
                bool onCooldown = cooldownRemaining > 0;
                cooldownOverlay.gameObject.SetActive(onCooldown);

                if (onCooldown)
                {
                    // Track max cooldown for fill calculation
                    if (cooldownRemaining > maxCooldown)
                    {
                        maxCooldown = cooldownRemaining;
                    }
                    cooldownOverlay.fillAmount = cooldownRemaining / maxCooldown;
                }
                else
                {
                    maxCooldown = 1f; // Reset
                }
            }

            // Update cooldown text
            if (cooldownText != null)
            {
                bool onCooldown = cooldownRemaining > 0;
                cooldownText.gameObject.SetActive(onCooldown);
                if (onCooldown)
                {
                    cooldownText.text = $"{cooldownRemaining:F1}s";
                }
            }

            // Update affordable indicator
            if (affordableIndicator != null)
            {
                affordableIndicator.color = currentGold >= cost ? affordableColor : notAffordableColor;
            }
        }

        public void SetIcon(Sprite sprite)
        {
            if (icon != null && sprite != null)
            {
                icon.sprite = sprite;
                icon.enabled = true;
            }
        }
    }
}
