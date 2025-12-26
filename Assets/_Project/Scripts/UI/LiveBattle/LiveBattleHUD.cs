using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    /// <summary>
    /// HUD for Live Battle mode - displays gold, units, hero, ability, and base HP
    /// </summary>
    public class LiveBattleHUD : MonoBehaviour
    {
        [Header("Gold Display")]
        public Text goldText;
        public Text passiveIncomeText;

        [Header("Time Display")]
        public Text battleTimeText;

        [Header("Base HP")]
        public Slider playerBaseHPSlider;
        public Text playerBaseHPText;
        public Slider enemyBaseHPSlider;
        public Text enemyBaseHPText;

        [Header("Unit Spawn Bar")]
        public Transform unitButtonContainer;
        public UnitSpawnButton unitButtonPrefab;
        private List<UnitSpawnButton> unitButtons = new List<UnitSpawnButton>();

        [Header("Hero Button")]
        public Button heroButton;
        public Image heroIcon;
        public Text heroNameText;
        public Image heroCooldownOverlay;
        public Text heroCooldownText;

        [Header("Ability Button")]
        public Button abilityButton;
        public Image abilityIcon;
        public Text abilityNameText;
        public Image abilityCooldownOverlay;
        public Text abilityCooldownText;

        [Header("Status")]
        public Text statusText;

        private LiveBattleLevelController levelController;
        private JsonDatabase database;

        public void Initialize(LiveBattleLevelController controller)
        {
            levelController = controller;
            database = ServiceLocator.Get<JsonDatabase>();

            // Subscribe to events
            if (controller.PlayerGold != null)
            {
                controller.PlayerGold.OnGoldChanged += OnGoldChanged;
            }

            // Create unit buttons
            CreateUnitButtons();

            // Setup hero button
            SetupHeroButton();

            // Setup ability button
            SetupAbilityButton();

            Refresh();
            Debug.Log("[LiveBattleHUD] Initialized");
        }

        private void CreateUnitButtons()
        {
            // Clear existing buttons
            foreach (var btn in unitButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            unitButtons.Clear();

            if (levelController?.PlayerDeck == null) return;

            for (int i = 0; i < levelController.PlayerDeck.SelectedUnits.Count; i++)
            {
                int slotIndex = i; // Capture for lambda
                string unitId = levelController.PlayerDeck.SelectedUnits[i];

                UnitSpawnButton button = CreateUnitButton(unitId, slotIndex);
                if (button != null)
                {
                    unitButtons.Add(button);
                }
            }
        }

        private UnitSpawnButton CreateUnitButton(string unitId, int slotIndex)
        {
            if (unitButtonContainer == null) return null;

            GameObject buttonGO;
            if (unitButtonPrefab != null)
            {
                buttonGO = Instantiate(unitButtonPrefab.gameObject, unitButtonContainer);
            }
            else
            {
                buttonGO = CreateDefaultUnitButton(unitId);
            }

            var unitButton = buttonGO.GetComponent<UnitSpawnButton>();
            if (unitButton == null)
            {
                unitButton = buttonGO.AddComponent<UnitSpawnButton>();
            }

            var unitDef = database?.FindUnit(unitId);
            unitButton.Initialize(unitId, unitDef?.display_name ?? unitId, unitDef?.goldCost ?? 0, slotIndex);
            unitButton.OnClicked += OnUnitButtonClicked;

            return unitButton;
        }

        private GameObject CreateDefaultUnitButton(string unitId)
        {
            GameObject buttonGO = new GameObject($"UnitButton_{unitId}");
            buttonGO.transform.SetParent(unitButtonContainer, false);

            var rectTransform = buttonGO.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(80, 100);

            var image = buttonGO.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.25f, 0.9f);

            var button = buttonGO.AddComponent<Button>();
            button.targetGraphic = image;

            // Add UnitSpawnButton component
            var unitButton = buttonGO.AddComponent<UnitSpawnButton>();

            // Create name label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonGO.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var labelText = labelGO.AddComponent<Text>();
            labelText.text = unitId;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;
            unitButton.nameText = labelText;

            // Create cost label
            GameObject costGO = new GameObject("Cost");
            costGO.transform.SetParent(buttonGO.transform, false);
            var costRect = costGO.AddComponent<RectTransform>();
            costRect.anchorMin = new Vector2(0, 0);
            costRect.anchorMax = new Vector2(1, 0.3f);
            costRect.offsetMin = Vector2.zero;
            costRect.offsetMax = Vector2.zero;
            var costText = costGO.AddComponent<Text>();
            costText.text = "0";
            costText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            costText.fontSize = 14;
            costText.fontStyle = FontStyle.Bold;
            costText.alignment = TextAnchor.MiddleCenter;
            costText.color = new Color(1f, 0.85f, 0.3f, 1f);
            unitButton.costText = costText;

            // Create cooldown overlay
            GameObject cooldownGO = new GameObject("CooldownOverlay");
            cooldownGO.transform.SetParent(buttonGO.transform, false);
            var cooldownRect = cooldownGO.AddComponent<RectTransform>();
            cooldownRect.anchorMin = Vector2.zero;
            cooldownRect.anchorMax = Vector2.one;
            cooldownRect.offsetMin = Vector2.zero;
            cooldownRect.offsetMax = Vector2.zero;
            var cooldownImage = cooldownGO.AddComponent<Image>();
            cooldownImage.color = new Color(0, 0, 0, 0.7f);
            cooldownImage.type = Image.Type.Filled;
            cooldownImage.fillMethod = Image.FillMethod.Vertical;
            cooldownImage.fillOrigin = 0;
            cooldownImage.fillAmount = 0f;
            cooldownGO.SetActive(false);
            unitButton.cooldownOverlay = cooldownImage;

            return buttonGO;
        }

        private void OnUnitButtonClicked(int slotIndex)
        {
            if (levelController != null)
            {
                levelController.TrySpawnPlayerUnit(slotIndex);
            }
        }

        private void SetupHeroButton()
        {
            if (heroButton == null) return;

            heroButton.onClick.AddListener(OnHeroButtonClicked);

            // Update hero info
            string heroId = levelController?.PlayerDeck?.SelectedHero;
            if (!string.IsNullOrEmpty(heroId))
            {
                var heroDef = database?.FindHero(heroId);
                if (heroNameText != null)
                {
                    heroNameText.text = heroDef?.name ?? heroId;
                }
            }
        }

        private void OnHeroButtonClicked()
        {
            if (levelController != null)
            {
                levelController.TrySpawnPlayerHero();
            }
        }

        private void SetupAbilityButton()
        {
            if (abilityButton == null) return;

            abilityButton.onClick.AddListener(OnAbilityButtonClicked);

            // Update ability info
            string civId = levelController?.PlayerDeck?.CivilizationID;
            if (!string.IsNullOrEmpty(civId))
            {
                var civ = database?.FindCivilization(civId);
                if (civ != null && !string.IsNullOrEmpty(civ.specialAbility))
                {
                    var ability = database.FindAbility(civ.specialAbility);
                    if (abilityNameText != null)
                    {
                        abilityNameText.text = ability?.name ?? civ.specialAbility;
                    }
                }
            }
        }

        private void OnAbilityButtonClicked()
        {
            if (levelController != null)
            {
                levelController.TryUseAbility();
            }
        }

        private void OnGoldChanged(int newGold)
        {
            if (goldText != null)
            {
                goldText.text = $"{newGold}";
            }
        }

        public void Refresh()
        {
            if (levelController == null) return;

            // Update gold
            if (goldText != null && levelController.PlayerGold != null)
            {
                goldText.text = $"{levelController.PlayerGold.CurrentGold}";
            }

            // Update time
            if (battleTimeText != null)
            {
                battleTimeText.text = levelController.GetFormattedBattleTime();
            }

            // Update base HP
            if (playerBaseHPSlider != null)
            {
                playerBaseHPSlider.value = levelController.GetPlayerBaseHPPercent();
            }
            if (playerBaseHPText != null && levelController.PlayerBase != null)
            {
                playerBaseHPText.text = $"{Mathf.CeilToInt(levelController.PlayerBase.currentHp)}";
            }

            if (enemyBaseHPSlider != null)
            {
                enemyBaseHPSlider.value = levelController.GetEnemyBaseHPPercent();
            }
            if (enemyBaseHPText != null && levelController.EnemyBase != null)
            {
                enemyBaseHPText.text = $"{Mathf.CeilToInt(levelController.EnemyBase.currentHp)}";
            }

            // Update unit buttons
            UpdateUnitButtons();

            // Update hero button
            UpdateHeroButton();

            // Update ability button
            UpdateAbilityButton();

            // Update status
            if (statusText != null)
            {
                if (!levelController.IsBattleActive && !levelController.IsBattleEnded)
                {
                    statusText.text = "Preparing...";
                }
                else if (levelController.IsBattleActive)
                {
                    statusText.text = "Battle!";
                }
                else
                {
                    statusText.text = levelController.PlayerVictory ? "Victory!" : "Defeat";
                }
            }
        }

        private void UpdateUnitButtons()
        {
            if (levelController?.PlayerSpawner == null) return;

            for (int i = 0; i < unitButtons.Count; i++)
            {
                var button = unitButtons[i];
                if (button == null) continue;

                bool canSpawn = levelController.PlayerSpawner.CanSpawnUnit(i);
                float cooldown = levelController.PlayerSpawner.GetUnitCooldown(i);
                int cost = levelController.PlayerSpawner.GetUnitCost(i);

                button.UpdateState(canSpawn, cooldown, cost, levelController.PlayerGold?.CurrentGold ?? 0);
            }
        }

        private void UpdateHeroButton()
        {
            if (heroButton == null || levelController?.PlayerSpawner == null) return;

            bool canSpawn = levelController.PlayerSpawner.CanSpawnHero();
            float cooldown = levelController.PlayerSpawner.GetHeroCooldown();

            heroButton.interactable = canSpawn;

            if (heroCooldownOverlay != null)
            {
                bool onCooldown = cooldown > 0;
                heroCooldownOverlay.gameObject.SetActive(onCooldown);
                if (onCooldown)
                {
                    heroCooldownOverlay.fillAmount = cooldown / levelController.PlayerSpawner.heroRespawnTime;
                }
            }

            if (heroCooldownText != null)
            {
                heroCooldownText.gameObject.SetActive(cooldown > 0);
                if (cooldown > 0)
                {
                    heroCooldownText.text = $"{Mathf.CeilToInt(cooldown)}s";
                }
            }
        }

        private void UpdateAbilityButton()
        {
            if (abilityButton == null) return;

            bool canUse = levelController.CanUseAbility();
            float cooldown = levelController.GetAbilityCooldown();

            abilityButton.interactable = canUse && levelController.IsBattleActive;

            // Update ability name if not set
            if (abilityNameText != null && string.IsNullOrEmpty(abilityNameText.text))
            {
                string abilityName = levelController.GetAbilityName();
                if (!string.IsNullOrEmpty(abilityName))
                {
                    abilityNameText.text = abilityName;
                }
            }

            // Update cooldown overlay
            if (abilityCooldownOverlay != null)
            {
                bool onCooldown = cooldown > 0;
                abilityCooldownOverlay.gameObject.SetActive(onCooldown);
                if (onCooldown && levelController.PlayerAbility != null)
                {
                    abilityCooldownOverlay.fillAmount = cooldown / levelController.PlayerAbility.CooldownDuration;
                }
            }

            // Update cooldown text
            if (abilityCooldownText != null)
            {
                bool onCooldown = cooldown > 0;
                abilityCooldownText.gameObject.SetActive(onCooldown);
                if (onCooldown)
                {
                    abilityCooldownText.text = $"{Mathf.CeilToInt(cooldown)}s";
                }
            }
        }

        private void Update()
        {
            // Continuously refresh (can be optimized with events)
            Refresh();
        }

        private void OnDestroy()
        {
            if (levelController?.PlayerGold != null)
            {
                levelController.PlayerGold.OnGoldChanged -= OnGoldChanged;
            }

            foreach (var btn in unitButtons)
            {
                if (btn != null)
                {
                    btn.OnClicked -= OnUnitButtonClicked;
                }
            }
        }
    }
}
