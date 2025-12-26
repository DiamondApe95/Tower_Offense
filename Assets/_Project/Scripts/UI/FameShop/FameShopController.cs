using System;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Progression;
using TowerConquest.Saving;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI.FameShop
{
    /// <summary>
    /// Controls the Fame Shop where players upgrade units, towers, heroes, etc.
    /// </summary>
    public class FameShopController : MonoBehaviour
    {
        public enum UpgradeCategory
        {
            Units,
            Heroes,
            Towers,
            Traps,
            Abilities
        }

        [Header("UI References")]
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Transform categoryButtonContainer;
        [SerializeField] private Transform upgradeItemContainer;
        [SerializeField] private GameObject upgradeItemPrefab;

        [Header("Fame Display")]
        [SerializeField] private Text currentFameText;
        [SerializeField] private Text totalFameEarnedText;

        [Header("Category Buttons")]
        [SerializeField] private Button unitsButton;
        [SerializeField] private Button heroesButton;
        [SerializeField] private Button towersButton;
        [SerializeField] private Button trapsButton;
        [SerializeField] private Button abilitiesButton;

        [Header("Selected Item Info")]
        [SerializeField] private GameObject itemInfoPanel;
        [SerializeField] private Text itemNameText;
        [SerializeField] private Text itemDescriptionText;
        [SerializeField] private Text currentLevelText;
        [SerializeField] private Text upgradeEffectText;
        [SerializeField] private Text upgradeCostText;
        [SerializeField] private Button upgradeButton;

        [Header("Navigation")]
        [SerializeField] private Button closeButton;

        // Runtime
        private UpgradeCategory currentCategory = UpgradeCategory.Units;
        private List<UpgradeItemUI> upgradeItems = new List<UpgradeItemUI>();
        private string selectedItemId;
        private JsonDatabase database;
        private FameManager fameManager;
        private UpgradeSystem upgradeSystem;
        private SaveManager saveManager;

        public event Action OnShopClosed;

        private void Awake()
        {
            SetupButtons();
        }

        private void Start()
        {
            Initialize();
        }

        private void SetupButtons()
        {
            if (unitsButton != null)
                unitsButton.onClick.AddListener(() => SetCategory(UpgradeCategory.Units));
            if (heroesButton != null)
                heroesButton.onClick.AddListener(() => SetCategory(UpgradeCategory.Heroes));
            if (towersButton != null)
                towersButton.onClick.AddListener(() => SetCategory(UpgradeCategory.Towers));
            if (trapsButton != null)
                trapsButton.onClick.AddListener(() => SetCategory(UpgradeCategory.Traps));
            if (abilitiesButton != null)
                abilitiesButton.onClick.AddListener(() => SetCategory(UpgradeCategory.Abilities));

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
            if (upgradeButton != null)
                upgradeButton.onClick.AddListener(PurchaseUpgrade);
        }

        public void Initialize()
        {
            database = ServiceLocator.Get<JsonDatabase>();
            ServiceLocator.TryGet(out saveManager);

            if (!ServiceLocator.TryGet(out fameManager))
            {
                // Create fame manager if not exists
                var fameGO = new GameObject("FameManager");
                fameManager = fameGO.AddComponent<FameManager>();
                ServiceLocator.Register(fameManager);
            }

            // UpgradeSystem is a regular class, not a MonoBehaviour
            if (upgradeSystem == null)
            {
                upgradeSystem = new UpgradeSystem(database, fameManager);
            }

            UpdateFameDisplay();
            SetCategory(UpgradeCategory.Units);
            HideItemInfo();
        }

        public void Open()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(true);
            }

            UpdateFameDisplay();
            RefreshCurrentCategory();
        }

        public void Close()
        {
            if (shopPanel != null)
            {
                shopPanel.SetActive(false);
            }

            OnShopClosed?.Invoke();
        }

        private void SetCategory(UpgradeCategory category)
        {
            currentCategory = category;
            UpdateCategoryButtonStates();
            RefreshCurrentCategory();
            HideItemInfo();
        }

        private void UpdateCategoryButtonStates()
        {
            SetButtonSelected(unitsButton, currentCategory == UpgradeCategory.Units);
            SetButtonSelected(heroesButton, currentCategory == UpgradeCategory.Heroes);
            SetButtonSelected(towersButton, currentCategory == UpgradeCategory.Towers);
            SetButtonSelected(trapsButton, currentCategory == UpgradeCategory.Traps);
            SetButtonSelected(abilitiesButton, currentCategory == UpgradeCategory.Abilities);
        }

        private void SetButtonSelected(Button button, bool selected)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = selected ? new Color(0.3f, 0.6f, 0.9f, 1f) : Color.white;
            button.colors = colors;
        }

        private void RefreshCurrentCategory()
        {
            ClearUpgradeItems();

            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    PopulateUnits();
                    break;
                case UpgradeCategory.Heroes:
                    PopulateHeroes();
                    break;
                case UpgradeCategory.Towers:
                    PopulateTowers();
                    break;
                case UpgradeCategory.Traps:
                    PopulateTraps();
                    break;
                case UpgradeCategory.Abilities:
                    PopulateAbilities();
                    break;
            }
        }

        private void ClearUpgradeItems()
        {
            foreach (var item in upgradeItems)
            {
                if (item != null)
                {
                    item.OnClicked -= OnItemClicked;
                    Destroy(item.gameObject);
                }
            }
            upgradeItems.Clear();
        }

        private void PopulateUnits()
        {
            var units = database?.GetAllUnits();
            if (units == null) return;

            foreach (var unit in units)
            {
                CreateUpgradeItem(unit.id, unit.display_name, GetUpgradeLevel(unit.id), GetMaxLevel(unit.id));
            }
        }

        private void PopulateHeroes()
        {
            var heroes = database?.GetAllHeroes();
            if (heroes == null) return;

            foreach (var hero in heroes)
            {
                CreateUpgradeItem(hero.id, hero.name, GetUpgradeLevel(hero.id), GetMaxLevel(hero.id));
            }
        }

        private void PopulateTowers()
        {
            var towers = database?.GetAllTowers();
            if (towers == null) return;

            foreach (var tower in towers)
            {
                CreateUpgradeItem(tower.id, tower.display_name, GetUpgradeLevel(tower.id), GetMaxLevel(tower.id));
            }
        }

        private void PopulateTraps()
        {
            var traps = database?.GetAllTraps();
            if (traps == null) return;

            foreach (var trap in traps)
            {
                CreateUpgradeItem(trap.id, trap.display_name, GetUpgradeLevel(trap.id), GetMaxLevel(trap.id));
            }
        }

        private void PopulateAbilities()
        {
            var abilities = database?.GetAllAbilities();
            if (abilities == null) return;

            foreach (var ability in abilities)
            {
                CreateUpgradeItem(ability.id, ability.name, GetUpgradeLevel(ability.id), GetMaxLevel(ability.id));
            }
        }

        private void CreateUpgradeItem(string id, string displayName, int currentLevel, int maxLevel)
        {
            if (upgradeItemContainer == null) return;

            GameObject itemObj;
            if (upgradeItemPrefab != null)
            {
                itemObj = Instantiate(upgradeItemPrefab, upgradeItemContainer);
            }
            else
            {
                itemObj = CreateDefaultUpgradeItem();
                itemObj.transform.SetParent(upgradeItemContainer, false);
            }

            var item = itemObj.GetComponent<UpgradeItemUI>();
            if (item == null)
            {
                item = itemObj.AddComponent<UpgradeItemUI>();
            }

            item.Initialize(id, displayName, currentLevel, maxLevel);
            item.OnClicked += OnItemClicked;

            upgradeItems.Add(item);
        }

        private GameObject CreateDefaultUpgradeItem()
        {
            GameObject itemObj = new GameObject("UpgradeItem");

            var rect = itemObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(200, 80);

            var image = itemObj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            var button = itemObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Create name text
            GameObject nameGO = new GameObject("Name");
            nameGO.transform.SetParent(itemObj.transform, false);
            var nameRect = nameGO.AddComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0.7f, 1);
            nameRect.offsetMin = new Vector2(10, 5);
            nameRect.offsetMax = new Vector2(0, -5);
            var nameText = nameGO.AddComponent<Text>();
            nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            nameText.fontSize = 16;
            nameText.alignment = TextAnchor.MiddleLeft;
            nameText.color = Color.white;

            // Create level text
            GameObject levelGO = new GameObject("Level");
            levelGO.transform.SetParent(itemObj.transform, false);
            var levelRect = levelGO.AddComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.7f, 0.5f);
            levelRect.anchorMax = new Vector2(1, 1);
            levelRect.offsetMin = new Vector2(0, 5);
            levelRect.offsetMax = new Vector2(-10, -5);
            var levelText = levelGO.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.fontSize = 14;
            levelText.alignment = TextAnchor.MiddleRight;
            levelText.color = new Color(1f, 0.8f, 0.3f, 1f);

            return itemObj;
        }

        private void OnItemClicked(string itemId)
        {
            selectedItemId = itemId;
            ShowItemInfo(itemId);
        }

        private void ShowItemInfo(string itemId)
        {
            if (itemInfoPanel != null)
            {
                itemInfoPanel.SetActive(true);
            }

            int currentLevel = GetUpgradeLevel(itemId);
            int maxLevel = GetMaxLevel(itemId);
            int upgradeCost = GetUpgradeCost(itemId, currentLevel + 1);

            if (itemNameText != null)
            {
                itemNameText.text = GetDisplayName(itemId);
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = GetDescription(itemId);
            }

            if (currentLevelText != null)
            {
                currentLevelText.text = $"Level: {currentLevel}/{maxLevel}";
            }

            if (upgradeEffectText != null)
            {
                if (currentLevel < maxLevel)
                {
                    upgradeEffectText.text = GetUpgradeEffect(itemId, currentLevel + 1);
                }
                else
                {
                    upgradeEffectText.text = "Max level reached!";
                }
            }

            if (upgradeCostText != null)
            {
                if (currentLevel < maxLevel)
                {
                    upgradeCostText.text = $"Cost: {upgradeCost} Fame";
                }
                else
                {
                    upgradeCostText.text = "";
                }
            }

            if (upgradeButton != null)
            {
                bool canUpgrade = currentLevel < maxLevel && fameManager.CurrentFame >= upgradeCost;
                upgradeButton.interactable = canUpgrade;
            }
        }

        private void HideItemInfo()
        {
            if (itemInfoPanel != null)
            {
                itemInfoPanel.SetActive(false);
            }
            selectedItemId = null;
        }

        private void PurchaseUpgrade()
        {
            if (string.IsNullOrEmpty(selectedItemId))
            {
                return;
            }

            int currentLevel = GetUpgradeLevel(selectedItemId);
            int maxLevel = GetMaxLevel(selectedItemId);

            if (currentLevel >= maxLevel)
            {
                Debug.Log("[FameShop] Already at max level");
                return;
            }

            bool upgraded = false;

            // Apply upgrade based on category
            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    upgraded = upgradeSystem?.UpgradeUnit(selectedItemId) ?? false;
                    break;
                case UpgradeCategory.Heroes:
                    upgraded = upgradeSystem?.UpgradeHero(selectedItemId) ?? false;
                    break;
                case UpgradeCategory.Towers:
                    int towerCost = GetUpgradeCost(selectedItemId, currentLevel + 1);
                    upgraded = upgradeSystem?.UpgradeTower(selectedItemId, towerCost) ?? false;
                    break;
                default:
                    Debug.Log($"[FameShop] Upgrade for {currentCategory} not yet implemented");
                    return;
            }

            if (!upgraded)
            {
                Debug.Log("[FameShop] Upgrade failed (not enough fame or already max level)");
                return;
            }

            // Refresh display
            UpdateFameDisplay();
            RefreshCurrentCategory();
            ShowItemInfo(selectedItemId);

            Debug.Log($"[FameShop] Upgraded {selectedItemId} to level {currentLevel + 1}");
        }

        private void UpdateFameDisplay()
        {
            if (currentFameText != null && fameManager != null)
            {
                currentFameText.text = $"Fame: {fameManager.CurrentFame}";
            }

            if (totalFameEarnedText != null && fameManager != null)
            {
                totalFameEarnedText.text = $"Total Earned: {fameManager.TotalFameEarned}";
            }
        }

        #region Data Access Helpers

        private int GetUpgradeLevel(string itemId)
        {
            if (upgradeSystem == null) return 1;

            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    return upgradeSystem.GetUnitLevel(itemId);
                case UpgradeCategory.Heroes:
                    return upgradeSystem.GetHeroLevel(itemId);
                case UpgradeCategory.Towers:
                    return upgradeSystem.GetTowerLevel(itemId);
                default:
                    return 1;
            }
        }

        private int GetMaxLevel(string itemId)
        {
            // Check upgrade levels defined in data
            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    var unit = database?.FindUnit(itemId);
                    return (unit?.upgradeLevels?.Length ?? 0) + 1;
                case UpgradeCategory.Heroes:
                    var hero = database?.FindHero(itemId);
                    return (hero?.upgradeLevels?.Length ?? 0) + 1;
                default:
                    return 5; // Default max level
            }
        }

        private int GetUpgradeCost(string itemId, int targetLevel)
        {
            if (upgradeSystem == null)
            {
                return 100 * targetLevel;
            }

            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    return upgradeSystem.GetUnitUpgradeCost(itemId);
                case UpgradeCategory.Heroes:
                    return upgradeSystem.GetHeroUpgradeCost(itemId);
                default:
                    return 100 * targetLevel;
            }
        }

        private string GetDisplayName(string itemId)
        {
            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    return database?.FindUnit(itemId)?.display_name ?? itemId;
                case UpgradeCategory.Heroes:
                    return database?.FindHero(itemId)?.name ?? itemId;
                case UpgradeCategory.Towers:
                    return database?.GetTower(itemId)?.display_name ?? itemId;
                case UpgradeCategory.Traps:
                    return database?.FindTrap(itemId)?.display_name ?? itemId;
                case UpgradeCategory.Abilities:
                    return database?.FindAbility(itemId)?.name ?? itemId;
                default:
                    return itemId;
            }
        }

        private string GetDescription(string itemId)
        {
            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    return database?.FindUnit(itemId)?.description ?? $"Upgrade {GetDisplayName(itemId)} to improve its stats.";
                case UpgradeCategory.Heroes:
                    return database?.FindHero(itemId)?.description ?? $"Upgrade {GetDisplayName(itemId)} to improve its stats.";
                case UpgradeCategory.Towers:
                    return database?.GetTower(itemId)?.description ?? $"Upgrade {GetDisplayName(itemId)} to improve its stats.";
                default:
                    return $"Upgrade {GetDisplayName(itemId)} to improve its stats.";
            }
        }

        private string GetUpgradeEffect(string itemId, int level)
        {
            switch (currentCategory)
            {
                case UpgradeCategory.Units:
                    var unitUpgrade = upgradeSystem?.GetUnitUpgradeBonus(itemId);
                    if (unitUpgrade != null)
                    {
                        return $"+{unitUpgrade.hpBonus}% HP, +{unitUpgrade.damageBonus}% Damage";
                    }
                    return $"+{level * 10}% HP, +{level * 5}% Damage";
                case UpgradeCategory.Heroes:
                    var heroUpgrade = upgradeSystem?.GetHeroUpgradeBonus(itemId);
                    if (heroUpgrade != null)
                    {
                        return $"+{heroUpgrade.hpBonus}% HP, +{heroUpgrade.damageBonus}% Damage";
                    }
                    return $"+{level * 15}% HP, +{level * 10}% Damage";
                case UpgradeCategory.Towers:
                    return $"+{level * 10}% Damage, +{level * 5}% Range";
                case UpgradeCategory.Traps:
                    return $"+{level * 15}% Effect, -{level * 5}% Cooldown";
                case UpgradeCategory.Abilities:
                    return $"+{level * 10}% Effect, -{level * 3}% Cooldown";
                default:
                    return "Improved stats";
            }
        }

        #endregion
    }
}
