using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay;
using TowerConquest.Debug;

namespace TowerConquest.UI
{
    /// <summary>
    /// UI Panel for selecting which tower to build.
    /// Shows all available towers for the current civilization.
    /// </summary>
    public class TowerSelectionPanel : MonoBehaviour
    {
        [Header("References")]
        public Transform buttonContainer;
        public TowerBuildButton buttonPrefab;

        [Header("State")]
        public bool isVisible = false;

        // Events
        public event Action<string> OnTowerSelected;

        private List<TowerBuildButton> towerButtons = new List<TowerBuildButton>();
        private JsonDatabase database;
        private BuildCategoryManager buildCategoryManager;
        private GoldManager playerGold;
        private string currentCivilizationId;

        private void Awake()
        {
            // Start hidden
            gameObject.SetActive(false);
        }

        public void Initialize(BuildCategoryManager categoryManager, GoldManager gold, string civilizationId)
        {
            buildCategoryManager = categoryManager;
            playerGold = gold;
            currentCivilizationId = civilizationId;
            database = ServiceLocator.Get<JsonDatabase>();

            CreateTowerButtons();

            Log.Info($"[TowerSelectionPanel] Initialized with {towerButtons.Count} tower buttons for {civilizationId}");
        }

        private void CreateTowerButtons()
        {
            // Clear existing buttons
            foreach (var btn in towerButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            towerButtons.Clear();

            if (database == null || string.IsNullOrEmpty(currentCivilizationId))
            {
                Log.Warning("[TowerSelectionPanel] Cannot create buttons - database or civilization not set");
                return;
            }

            // Get available towers for civilization
            var civ = database.FindCivilization(currentCivilizationId);
            if (civ?.availableTowers == null)
            {
                Log.Warning($"[TowerSelectionPanel] No towers available for {currentCivilizationId}");
                return;
            }

            foreach (string towerId in civ.availableTowers)
            {
                var towerDef = database.FindTower(towerId);
                if (towerDef != null)
                {
                    TowerBuildButton button = CreateTowerButton(towerDef);
                    if (button != null)
                    {
                        towerButtons.Add(button);
                    }
                }
            }
        }

        private TowerBuildButton CreateTowerButton(TowerDefinition towerDef)
        {
            if (buttonContainer == null) return null;

            GameObject buttonGO;
            if (buttonPrefab != null)
            {
                buttonGO = Instantiate(buttonPrefab.gameObject, buttonContainer);
            }
            else
            {
                buttonGO = CreateDefaultTowerButton(towerDef);
            }

            var towerButton = buttonGO.GetComponent<TowerBuildButton>();
            if (towerButton == null)
            {
                towerButton = buttonGO.AddComponent<TowerBuildButton>();
            }

            towerButton.Initialize(towerDef.id, towerDef.display_name, towerDef.goldCost);
            towerButton.OnClicked += OnTowerButtonClicked;

            return towerButton;
        }

        private GameObject CreateDefaultTowerButton(TowerDefinition towerDef)
        {
            GameObject buttonGO = new GameObject($"TowerButton_{towerDef.id}");
            buttonGO.transform.SetParent(buttonContainer, false);

            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 120);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.15f, 0.35f, 0.15f, 0.9f); // Green for towers

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // Add TowerBuildButton component
            var towerButton = buttonGO.AddComponent<TowerBuildButton>();

            // Create name label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.4f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(4, 0);
            labelRect.offsetMax = new Vector2(-4, -4);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = towerDef.display_name;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 16;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            towerButton.nameText = labelText;

            // Create cost label
            GameObject costGO = new GameObject("Cost");
            costGO.transform.SetParent(buttonGO.transform, false);
            var costRect = costGO.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(1, 0.4f);
            costRect.offsetMin = new Vector2(4, 4);
            costRect.offsetMax = new Vector2(-4, 0);
            var costText = costGO.AddComponent<Text>();
            costText.text = $"{towerDef.goldCost}";
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = 20;
            costText.fontStyle = FontStyle.Bold;
            costText.alignment = TextAnchor.MiddleCenter;
            costText.color = new Color(1f, 0.85f, 0.3f, 1f);
            towerButton.costText = costText;

            return buttonGO;
        }

        private void OnTowerButtonClicked(string towerId)
        {
            Log.Info($"[TowerSelectionPanel] Tower selected: {towerId}");

            // Select tower in BuildCategoryManager
            if (buildCategoryManager != null)
            {
                buildCategoryManager.SetCategory(BuildCategoryManager.BuildCategory.Towers);
                buildCategoryManager.SelectBuildable(towerId);
            }

            OnTowerSelected?.Invoke(towerId);

            // Hide panel after selection
            Hide();
        }

        public void Show()
        {
            isVisible = true;
            gameObject.SetActive(true);
            UpdateButtonStates();
        }

        public void Hide()
        {
            isVisible = false;
            gameObject.SetActive(false);
        }

        public void Toggle()
        {
            if (isVisible)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        private void Update()
        {
            if (isVisible)
            {
                UpdateButtonStates();
            }
        }

        private void UpdateButtonStates()
        {
            int currentGold = playerGold?.CurrentGold ?? 0;

            foreach (var button in towerButtons)
            {
                if (button != null)
                {
                    button.UpdateState(currentGold);
                }
            }
        }

        private void OnDestroy()
        {
            foreach (var btn in towerButtons)
            {
                if (btn != null)
                {
                    btn.OnClicked -= OnTowerButtonClicked;
                }
            }
        }
    }

    /// <summary>
    /// Button for building a specific tower
    /// </summary>
    public class TowerBuildButton : MonoBehaviour
    {
        public Text nameText;
        public Text costText;
        public Image iconImage;
        public Button button;

        public string TowerId { get; private set; }
        public int Cost { get; private set; }

        public event Action<string> OnClicked;

        public void Initialize(string towerId, string displayName, int cost)
        {
            TowerId = towerId;
            Cost = cost;

            if (nameText != null)
            {
                nameText.text = displayName;
            }

            if (costText != null)
            {
                costText.text = $"{cost}";
            }

            // Get or add button
            if (button == null)
            {
                button = GetComponent<Button>();
            }

            if (button != null)
            {
                button.onClick.AddListener(OnButtonClick);
            }
        }

        private void OnButtonClick()
        {
            OnClicked?.Invoke(TowerId);
        }

        public void UpdateState(int currentGold)
        {
            bool canAfford = currentGold >= Cost;

            if (button != null)
            {
                button.interactable = canAfford;
            }

            // Update cost text color based on affordability
            if (costText != null)
            {
                costText.color = canAfford
                    ? new Color(1f, 0.85f, 0.3f, 1f)   // Gold/Yellow
                    : new Color(0.7f, 0.3f, 0.3f, 1f); // Red
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(OnButtonClick);
            }
        }
    }
}
