using System;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    /// <summary>
    /// UI for selecting units and hero for the player's deck
    /// </summary>
    public class UnitDeckSelectUI : MonoBehaviour
    {
        [Header("Available Units")]
        public Transform availableUnitsContainer;
        public GameObject unitSelectionButtonPrefab;

        [Header("Selected Deck")]
        public Transform selectedDeckContainer;
        public GameObject deckSlotPrefab;
        public Text deckCountText;

        [Header("Heroes")]
        public Transform heroContainer;
        public GameObject heroButtonPrefab;

        [Header("Selected Info")]
        public Text selectedUnitName;
        public Text selectedUnitDescription;
        public Text selectedUnitCost;
        public Text selectedUnitStats;

        [Header("Buttons")]
        public Button confirmButton;
        public Button backButton;
        public Button clearDeckButton;

        public event Action<UnitDeck> OnDeckConfirmed;
        public event Action OnBack;

        private JsonDatabase database;
        private UnitDeck currentDeck;
        private string selectedCivilizationId;

        private List<UnitSelectionButton> availableUnitButtons = new List<UnitSelectionButton>();
        private List<DeckSlotButton> deckSlotButtons = new List<DeckSlotButton>();
        private List<HeroSelectionButton> heroButtons = new List<HeroSelectionButton>();

        private void Start()
        {
            database = ServiceLocator.Get<JsonDatabase>();

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirm);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBack);
            }

            if (clearDeckButton != null)
            {
                clearDeckButton.onClick.AddListener(HandleClearDeck);
            }
        }

        public void Initialize(string civilizationId)
        {
            selectedCivilizationId = civilizationId;
            currentDeck = new UnitDeck();
            currentDeck.SetCivilization(civilizationId);

            RefreshUI();
        }

        public void RefreshUI()
        {
            CreateAvailableUnitButtons();
            CreateDeckSlots();
            CreateHeroButtons();
            UpdateDeckCount();
            UpdateConfirmButton();
        }

        private void CreateAvailableUnitButtons()
        {
            // Clear existing
            foreach (var btn in availableUnitButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            availableUnitButtons.Clear();

            if (availableUnitsContainer == null) return;

            var units = database.GetUnitsForCivilization(selectedCivilizationId);
            foreach (var unit in units)
            {
                CreateUnitButton(unit);
            }
        }

        private void CreateUnitButton(UnitDefinition unit)
        {
            GameObject buttonGO;
            if (unitSelectionButtonPrefab != null)
            {
                buttonGO = Instantiate(unitSelectionButtonPrefab, availableUnitsContainer);
            }
            else
            {
                buttonGO = CreateDefaultUnitSelectionButton(unit);
            }

            var unitButton = buttonGO.GetComponent<UnitSelectionButton>();
            if (unitButton == null)
            {
                unitButton = buttonGO.AddComponent<UnitSelectionButton>();
            }

            unitButton.Initialize(unit);
            unitButton.OnClicked += OnAvailableUnitClicked;

            availableUnitButtons.Add(unitButton);
        }

        private GameObject CreateDefaultUnitSelectionButton(UnitDefinition unit)
        {
            GameObject buttonGO = new GameObject($"UnitBtn_{unit.id}");
            buttonGO.transform.SetParent(availableUnitsContainer, false);

            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(100, 80);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.25f, 0.25f, 0.3f, 1f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            var unitButton = buttonGO.AddComponent<UnitSelectionButton>();

            // Name label
            GameObject labelGO = new GameObject("Name");
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.4f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(5, 0);
            labelRect.offsetMax = new Vector2(-5, -5);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = unit.display_name ?? unit.id;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 11;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            unitButton.nameText = labelText;

            // Cost label
            GameObject costGO = new GameObject("Cost");
            costGO.transform.SetParent(buttonGO.transform, false);
            var costRect = costGO.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(1, 0.4f);
            costRect.offsetMin = new Vector2(5, 5);
            costRect.offsetMax = new Vector2(-5, 0);
            var costText = costGO.AddComponent<Text>();
            costText.text = $"{unit.goldCost}g";
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = 12;
            costText.fontStyle = FontStyle.Bold;
            costText.alignment = TextAnchor.MiddleCenter;
            costText.color = new Color(1f, 0.85f, 0.3f, 1f);
            unitButton.costText = costText;

            return buttonGO;
        }

        private void OnAvailableUnitClicked(string unitId)
        {
            if (currentDeck.HasUnit(unitId))
            {
                // Already in deck, remove it
                currentDeck.RemoveUnit(unitId);
            }
            else
            {
                // Try to add to deck
                currentDeck.AddUnit(unitId);
            }

            UpdateDeckDisplay();
            UpdateUnitButtonStates();
        }

        private void CreateDeckSlots()
        {
            // Clear existing
            foreach (var slot in deckSlotButtons)
            {
                if (slot != null)
                {
                    Destroy(slot.gameObject);
                }
            }
            deckSlotButtons.Clear();

            if (selectedDeckContainer == null) return;

            for (int i = 0; i < UnitDeck.MAX_UNITS; i++)
            {
                CreateDeckSlot(i);
            }
        }

        private void CreateDeckSlot(int index)
        {
            GameObject slotGO;
            if (deckSlotPrefab != null)
            {
                slotGO = Instantiate(deckSlotPrefab, selectedDeckContainer);
            }
            else
            {
                slotGO = CreateDefaultDeckSlot(index);
            }

            var slotButton = slotGO.GetComponent<DeckSlotButton>();
            if (slotButton == null)
            {
                slotButton = slotGO.AddComponent<DeckSlotButton>();
            }

            slotButton.Initialize(index);
            slotButton.OnClicked += OnDeckSlotClicked;

            deckSlotButtons.Add(slotButton);
        }

        private GameObject CreateDefaultDeckSlot(int index)
        {
            GameObject slotGO = new GameObject($"DeckSlot_{index}");
            slotGO.transform.SetParent(selectedDeckContainer, false);

            var rectTransform = slotGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80, 80);

            var image = slotGO.AddComponent<Image>();
            image.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

            var button = slotGO.AddComponent<Button>();
            button.targetGraphic = image;

            var slotButton = slotGO.AddComponent<DeckSlotButton>();

            // Slot number
            GameObject numGO = new GameObject("Number");
            numGO.transform.SetParent(slotGO.transform, false);
            var numRect = numGO.AddComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0, 0);
            numRect.anchorMax = new Vector2(0.3f, 0.3f);
            numRect.offsetMin = Vector2.zero;
            numRect.offsetMax = Vector2.zero;
            var numText = numGO.AddComponent<Text>();
            numText.text = $"{index + 1}";
            numText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            numText.fontSize = 12;
            numText.alignment = TextAnchor.MiddleCenter;
            numText.color = new Color(1f, 1f, 1f, 0.5f);
            slotButton.slotNumberText = numText;

            // Unit name
            GameObject nameGO = new GameObject("UnitName");
            nameGO.transform.SetParent(slotGO.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.offsetMin = new Vector2(5, 5);
            nameRect.offsetMax = new Vector2(-5, -5);
            var nameText = nameGO.AddComponent<Text>();
            nameText.text = "";
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 11;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
            slotButton.unitNameText = nameText;

            return slotGO;
        }

        private void OnDeckSlotClicked(int slotIndex)
        {
            if (slotIndex < currentDeck.SelectedUnits.Count)
            {
                string unitId = currentDeck.SelectedUnits[slotIndex];
                currentDeck.RemoveUnit(unitId);
                UpdateDeckDisplay();
                UpdateUnitButtonStates();
            }
        }

        private void CreateHeroButtons()
        {
            // Clear existing
            foreach (var btn in heroButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            heroButtons.Clear();

            if (heroContainer == null) return;

            var heroes = database.GetHeroesForCivilization(selectedCivilizationId);
            foreach (var hero in heroes)
            {
                CreateHeroButton(hero);
            }
        }

        private void CreateHeroButton(HeroDefinition hero)
        {
            GameObject buttonGO;
            if (heroButtonPrefab != null)
            {
                buttonGO = Instantiate(heroButtonPrefab, heroContainer);
            }
            else
            {
                buttonGO = CreateDefaultHeroButton(hero);
            }

            var heroButton = buttonGO.GetComponent<HeroSelectionButton>();
            if (heroButton == null)
            {
                heroButton = buttonGO.AddComponent<HeroSelectionButton>();
            }

            heroButton.Initialize(hero);
            heroButton.OnClicked += OnHeroClicked;

            heroButtons.Add(heroButton);
        }

        private GameObject CreateDefaultHeroButton(HeroDefinition hero)
        {
            GameObject buttonGO = new GameObject($"HeroBtn_{hero.id}");
            buttonGO.transform.SetParent(heroContainer, false);

            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 100);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.6f, 0.5f, 0.2f, 1f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            var heroButton = buttonGO.AddComponent<HeroSelectionButton>();

            // Name label
            GameObject labelGO = new GameObject("Name");
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10, 10);
            labelRect.offsetMax = new Vector2(-10, -10);
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = hero.name ?? hero.id;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.fontStyle = FontStyle.Bold;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            heroButton.nameText = labelText;

            return buttonGO;
        }

        private void OnHeroClicked(string heroId)
        {
            currentDeck.SetHero(heroId);
            UpdateHeroButtonStates();
            UpdateConfirmButton();
        }

        private void UpdateDeckDisplay()
        {
            for (int i = 0; i < deckSlotButtons.Count; i++)
            {
                var slot = deckSlotButtons[i];
                if (i < currentDeck.SelectedUnits.Count)
                {
                    string unitId = currentDeck.SelectedUnits[i];
                    var unit = database.FindUnit(unitId);
                    slot.SetUnit(unitId, unit?.display_name ?? unitId);
                }
                else
                {
                    slot.SetEmpty();
                }
            }

            UpdateDeckCount();
            UpdateConfirmButton();
        }

        private void UpdateUnitButtonStates()
        {
            foreach (var btn in availableUnitButtons)
            {
                bool inDeck = currentDeck.HasUnit(btn.UnitId);
                bool deckFull = currentDeck.SelectedUnits.Count >= UnitDeck.MAX_UNITS;
                btn.SetSelected(inDeck);
                btn.SetInteractable(!deckFull || inDeck);
            }
        }

        private void UpdateHeroButtonStates()
        {
            foreach (var btn in heroButtons)
            {
                btn.SetSelected(btn.HeroId == currentDeck.SelectedHero);
            }
        }

        private void UpdateDeckCount()
        {
            if (deckCountText != null)
            {
                deckCountText.text = $"{currentDeck.SelectedUnits.Count}/{UnitDeck.MAX_UNITS}";
            }
        }

        private void UpdateConfirmButton()
        {
            if (confirmButton != null)
            {
                confirmButton.interactable = currentDeck.IsValid();
            }
        }

        private void HandleConfirm()
        {
            if (currentDeck.IsValid())
            {
                OnDeckConfirmed?.Invoke(currentDeck);
            }
        }

        private void HandleBack()
        {
            OnBack?.Invoke();
        }

        private void HandleClearDeck()
        {
            currentDeck.Clear();
            currentDeck.SetCivilization(selectedCivilizationId);
            UpdateDeckDisplay();
            UpdateUnitButtonStates();
            UpdateHeroButtonStates();
        }

        public UnitDeck GetCurrentDeck()
        {
            return currentDeck;
        }

        private void OnDestroy()
        {
            foreach (var btn in availableUnitButtons)
            {
                if (btn != null) btn.OnClicked -= OnAvailableUnitClicked;
            }
            foreach (var slot in deckSlotButtons)
            {
                if (slot != null) slot.OnClicked -= OnDeckSlotClicked;
            }
            foreach (var btn in heroButtons)
            {
                if (btn != null) btn.OnClicked -= OnHeroClicked;
            }
        }
    }

    public class UnitSelectionButton : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Text nameText;
        public Text costText;
        public Image selectedIndicator;

        public string UnitId { get; private set; }
        public event Action<string> OnClicked;

        public void Initialize(UnitDefinition unit)
        {
            UnitId = unit.id;

            if (nameText != null)
            {
                nameText.text = unit.display_name ?? unit.id;
            }

            if (costText != null)
            {
                costText.text = $"{unit.goldCost}g";
            }

            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClicked?.Invoke(UnitId));
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
            {
                selectedIndicator.gameObject.SetActive(selected);
            }
            else if (background != null)
            {
                background.color = selected ? new Color(0.3f, 0.5f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.3f, 1f);
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }

    public class DeckSlotButton : MonoBehaviour
    {
        public Button button;
        public Text slotNumberText;
        public Text unitNameText;
        public Image unitIcon;

        public int SlotIndex { get; private set; }
        public string UnitId { get; private set; }
        public event Action<int> OnClicked;

        public void Initialize(int index)
        {
            SlotIndex = index;

            if (slotNumberText != null)
            {
                slotNumberText.text = $"{index + 1}";
            }

            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClicked?.Invoke(SlotIndex));
            }

            SetEmpty();
        }

        public void SetUnit(string unitId, string displayName)
        {
            UnitId = unitId;
            if (unitNameText != null)
            {
                unitNameText.text = displayName;
            }
            if (button != null)
            {
                button.interactable = true;
            }
        }

        public void SetEmpty()
        {
            UnitId = null;
            if (unitNameText != null)
            {
                unitNameText.text = "Empty";
            }
            if (button != null)
            {
                button.interactable = false;
            }
        }
    }

    public class HeroSelectionButton : MonoBehaviour
    {
        public Button button;
        public Image background;
        public Text nameText;
        public Image selectedIndicator;

        public string HeroId { get; private set; }
        public event Action<string> OnClicked;

        public void Initialize(HeroDefinition hero)
        {
            HeroId = hero.id;

            if (nameText != null)
            {
                nameText.text = hero.name ?? hero.id;
            }

            if (button == null) button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnClicked?.Invoke(HeroId));
            }
        }

        public void SetSelected(bool selected)
        {
            if (selectedIndicator != null)
            {
                selectedIndicator.gameObject.SetActive(selected);
            }
            else if (background != null)
            {
                background.color = selected ? new Color(0.7f, 0.6f, 0.2f, 1f) : new Color(0.5f, 0.4f, 0.15f, 1f);
            }
        }
    }
}
