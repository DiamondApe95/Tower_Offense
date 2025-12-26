using System;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay;
using TowerConquest.Progression;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    /// <summary>
    /// UI for selecting a civilization before starting a level
    /// </summary>
    public class CivilizationSelectUI : MonoBehaviour
    {
        [Header("UI References")]
        public Transform civilizationButtonContainer;
        public GameObject civilizationButtonPrefab;

        [Header("Selected Civilization Display")]
        public Text selectedCivName;
        public Text selectedCivDescription;
        public Image selectedCivIcon;
        public Text selectedCivAbilityName;

        [Header("Buttons")]
        public Button confirmButton;
        public Button backButton;

        [Header("Fame Display")]
        public Text fameText;

        public event Action<string> OnCivilizationSelected;
        public event Action OnConfirmed;
        public event Action OnBack;

        private JsonDatabase database;
        private FameManager fameManager;
        private List<CivilizationButton> civButtons = new List<CivilizationButton>();
        private string selectedCivilizationId;

        private void Start()
        {
            database = ServiceLocator.Get<JsonDatabase>();
            ServiceLocator.TryGet(out fameManager);

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBack);
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            CreateCivilizationButtons();
            UpdateFameDisplay();

            // Select first available
            if (string.IsNullOrEmpty(selectedCivilizationId) && database?.Civilizations?.Count > 0)
            {
                SelectCivilization(database.Civilizations[0].id);
            }
        }

        private void CreateCivilizationButtons()
        {
            // Clear existing
            foreach (var btn in civButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            civButtons.Clear();

            if (database?.Civilizations == null || civilizationButtonContainer == null) return;

            foreach (var civ in database.Civilizations)
            {
                CreateCivButton(civ);
            }
        }

        private void CreateCivButton(CivilizationDefinition civ)
        {
            GameObject buttonGO;
            if (civilizationButtonPrefab != null)
            {
                buttonGO = Instantiate(civilizationButtonPrefab, civilizationButtonContainer);
            }
            else
            {
                buttonGO = CreateDefaultCivButton(civ);
            }

            var civButton = buttonGO.GetComponent<CivilizationButton>();
            if (civButton == null)
            {
                civButton = buttonGO.AddComponent<CivilizationButton>();
            }

            bool isUnlocked = civ.unlockCost == 0 || (fameManager != null && fameManager.TotalFame >= civ.unlockCost);
            civButton.Initialize(civ, isUnlocked);
            civButton.OnClicked += OnCivButtonClicked;

            civButtons.Add(civButton);
        }

        private GameObject CreateDefaultCivButton(CivilizationDefinition civ)
        {
            GameObject buttonGO = new GameObject($"CivButton_{civ.id}");
            buttonGO.transform.SetParent(civilizationButtonContainer, false);

            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(200, 80);

            var image = buttonGO.AddComponent<Image>();

            // Parse color from hex
            Color civColor = Color.gray;
            if (!string.IsNullOrEmpty(civ.color))
            {
                ColorUtility.TryParseHtmlString(civ.color, out civColor);
            }
            image.color = civColor;

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // Add CivilizationButton component
            var civButton = buttonGO.AddComponent<CivilizationButton>();

            // Create name label
            GameObject labelGO = new GameObject("Name");
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 10);
            labelRect.offsetMax = new Vector2(-10, -10);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = civ.name;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 18;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            civButton.nameText = labelText;

            // Create lock overlay
            GameObject lockGO = new GameObject("LockOverlay");
            lockGO.transform.SetParent(buttonGO.transform, false);
            var lockRect = lockGO.AddComponent<RectTransform>();
            lockRect.anchorMin = Vector2.zero;
            lockRect.anchorMax = Vector2.one;
            lockRect.offsetMin = Vector2.zero;
            lockRect.offsetMax = Vector2.zero;
            var lockImage = lockGO.AddComponent<Image>();
            lockImage.color = new Color(0, 0, 0, 0.7f);
            civButton.lockOverlay = lockImage;

            // Create unlock cost text
            GameObject costGO = new GameObject("UnlockCost");
            costGO.transform.SetParent(lockGO.transform, false);
            var costRect = costGO.AddComponent<RectTransform>();
            costRect.anchorMin = Vector2.zero;
            costRect.anchorMax = Vector2.one;
            costRect.offsetMin = Vector2.zero;
            costRect.offsetMax = Vector2.zero;
            var costText = costGO.AddComponent<Text>();
            costText.text = $"Unlock: {civ.unlockCost} Fame";
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = 14;
            costText.alignment = TextAnchor.MiddleCenter;
            costText.color = Color.white;
            civButton.unlockCostText = costText;

            return buttonGO;
        }

        private void OnCivButtonClicked(string civId)
        {
            SelectCivilization(civId);
        }

        public void SelectCivilization(string civId)
        {
            selectedCivilizationId = civId;

            var civ = database.FindCivilization(civId);
            if (civ == null) return;

            // Update display
            if (selectedCivName != null)
            {
                selectedCivName.text = civ.name;
            }

            if (selectedCivDescription != null)
            {
                selectedCivDescription.text = civ.description;
            }

            if (selectedCivAbilityName != null && !string.IsNullOrEmpty(civ.specialAbility))
            {
                var ability = database.FindAbility(civ.specialAbility);
                selectedCivAbilityName.text = ability?.name ?? civ.specialAbility;
            }

            // Update button selection visuals
            foreach (var btn in civButtons)
            {
                btn.SetSelected(btn.CivilizationId == civId);
            }

            OnCivilizationSelected?.Invoke(civId);
        }

        private void HandleConfirm()
        {
            if (!string.IsNullOrEmpty(selectedCivilizationId))
            {
                OnConfirmed?.Invoke();
            }
        }

        private void HandleBack()
        {
            OnBack?.Invoke();
        }

        private void UpdateFameDisplay()
        {
            if (fameText != null && fameManager != null)
            {
                fameText.text = $"Fame: {fameManager.TotalFame}";
            }
        }

        public string GetSelectedCivilization()
        {
            return selectedCivilizationId;
        }

        private void OnDestroy()
        {
            foreach (var btn in civButtons)
            {
                if (btn != null)
                {
                    btn.OnClicked -= OnCivButtonClicked;
                }
            }
        }
    }

    /// <summary>
    /// Individual civilization selection button
    /// </summary>
    public class CivilizationButton : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Text nameText;
        public Image lockOverlay;
        public Text unlockCostText;
        public Image selectionBorder;

        public string CivilizationId { get; private set; }
        public bool IsUnlocked { get; private set; }

        public event Action<string> OnClicked;

        public void Initialize(CivilizationDefinition civ, bool unlocked)
        {
            CivilizationId = civ.id;
            IsUnlocked = unlocked;

            if (nameText != null)
            {
                nameText.text = civ.name;
            }

            if (lockOverlay != null)
            {
                lockOverlay.gameObject.SetActive(!unlocked);
            }

            if (unlockCostText != null)
            {
                unlockCostText.gameObject.SetActive(!unlocked);
                if (!unlocked)
                {
                    unlockCostText.text = $"Unlock: {civ.unlockCost} Fame";
                }
            }

            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(HandleClick);
                button.interactable = unlocked;
            }
        }

        private void HandleClick()
        {
            if (IsUnlocked)
            {
                OnClicked?.Invoke(CivilizationId);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectionBorder != null)
            {
                selectionBorder.gameObject.SetActive(selected);
            }
            else if (background != null)
            {
                // Highlight with brightness
                Color baseColor = background.color;
                if (selected)
                {
                    background.color = new Color(
                        Mathf.Min(1f, baseColor.r + 0.2f),
                        Mathf.Min(1f, baseColor.g + 0.2f),
                        Mathf.Min(1f, baseColor.b + 0.2f),
                        baseColor.a
                    );
                }
            }
        }
    }
}
