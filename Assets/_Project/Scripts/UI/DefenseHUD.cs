using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Gameplay.Entities;

namespace TowerConquest.UI
{
    /// <summary>
    /// DefenseHUD: UI für den Defense-Modus mit Tower-Auswahl, Gold-Anzeige und Tower-Info.
    /// </summary>
    public class DefenseHUD : MonoBehaviour
    {
        [Header("Gold Display")]
        public Text goldText;
        public Image goldIcon;

        [Header("Tower Selection")]
        public Transform towerButtonContainer;
        public GameObject towerButtonPrefab;

        [Header("Tower Info Panel")]
        public GameObject towerInfoPanel;
        public Text towerNameText;
        public Text towerStatsText;
        public Text towerCostText;
        public Button upgradeButton;
        public Button sellButton;

        [Header("Wave Info")]
        public Text waveText;
        public Text enemyCountText;
        public Button startWaveButton;

        [Header("Selected Tower Indicator")]
        public Image selectedTowerHighlight;
        public Color selectedColor = new Color(1f, 0.8f, 0.2f, 1f);
        public Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        [Header("Preview Settings")]
        public Color validBuildColor = new Color(0.2f, 0.8f, 0.2f, 0.5f);
        public Color invalidBuildColor = new Color(0.8f, 0.2f, 0.2f, 0.5f);

        private BuildManager buildManager;
        private List<TowerButtonData> towerButtons = new List<TowerButtonData>();
        private int selectedButtonIndex = -1;

        private class TowerButtonData
        {
            public GameObject buttonObj;
            public Button button;
            public Image icon;
            public Text costText;
            public Image background;
            public int index;
        }

        private void Start()
        {
            buildManager = FindFirstObjectByType<BuildManager>();

            if (buildManager != null)
            {
                buildManager.OnGoldChanged += RefreshGold;
                buildManager.OnTowerBuilt += OnTowerBuilt;
                buildManager.OnTowerSold += OnTowerSold;
                buildManager.OnTowerSelected += OnTowerSelected;
                buildManager.OnTileHovered += OnTileHovered;
            }

            SetupButtons();
            CreateTowerButtons();
            RefreshUI();
            HideTowerInfo();
        }

        private void OnDestroy()
        {
            if (buildManager != null)
            {
                buildManager.OnGoldChanged -= RefreshGold;
                buildManager.OnTowerBuilt -= OnTowerBuilt;
                buildManager.OnTowerSold -= OnTowerSold;
                buildManager.OnTowerSelected -= OnTowerSelected;
                buildManager.OnTileHovered -= OnTileHovered;
            }
        }

        private void SetupButtons()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.AddListener(OnUpgradeClicked);
            }

            if (sellButton != null)
            {
                sellButton.onClick.AddListener(OnSellClicked);
            }

            if (startWaveButton != null)
            {
                startWaveButton.onClick.AddListener(OnStartWaveClicked);
            }
        }

        private void CreateTowerButtons()
        {
            if (buildManager == null || towerButtonContainer == null || towerButtonPrefab == null) return;

            // Alte Buttons entfernen
            foreach (var btnData in towerButtons)
            {
                if (btnData.buttonObj != null)
                {
                    Destroy(btnData.buttonObj);
                }
            }
            towerButtons.Clear();

            // Neue Buttons erstellen
            for (int i = 0; i < buildManager.availableTowerPrefabs.Count; i++)
            {
                GameObject prefab = buildManager.availableTowerPrefabs[i];
                if (prefab == null) continue;

                CreateTowerButton(prefab, i);
            }

            // Ersten Button auswählen
            if (towerButtons.Count > 0)
            {
                SelectTowerButton(0);
            }
        }

        private void CreateTowerButton(GameObject towerPrefab, int index)
        {
            GameObject btnObj = Instantiate(towerButtonPrefab, towerButtonContainer);

            var btnData = new TowerButtonData
            {
                buttonObj = btnObj,
                button = btnObj.GetComponent<Button>(),
                icon = btnObj.transform.Find("Icon")?.GetComponent<Image>(),
                costText = btnObj.transform.Find("CostText")?.GetComponent<Text>(),
                background = btnObj.GetComponent<Image>(),
                index = index
            };

            if (btnData.button != null)
            {
                int capturedIndex = index;
                btnData.button.onClick.AddListener(() => SelectTowerButton(capturedIndex));
            }

            // Tower-Info laden
            TowerController controller = towerPrefab.GetComponent<TowerController>();
            int cost = controller?.BuildCost ?? 50;

            if (btnData.costText != null)
            {
                btnData.costText.text = cost.ToString();
            }

            // Icon aus TowerDefinition oder Prefab
            if (btnData.icon != null)
            {
                SpriteRenderer spriteRenderer = towerPrefab.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                {
                    btnData.icon.sprite = spriteRenderer.sprite;
                }
            }

            towerButtons.Add(btnData);
        }

        public void SelectTowerButton(int index)
        {
            if (index < 0 || index >= towerButtons.Count) return;

            selectedButtonIndex = index;

            // Visual Update
            for (int i = 0; i < towerButtons.Count; i++)
            {
                var btn = towerButtons[i];
                if (btn.background != null)
                {
                    btn.background.color = (i == index) ? selectedColor : unselectedColor;
                }
            }

            // BuildManager aktualisieren
            if (buildManager != null)
            {
                buildManager.SelectTowerType(index);
            }

            RefreshUI();
        }

        private void RefreshUI()
        {
            RefreshGold(buildManager?.CurrentGold ?? 0);
            RefreshButtonStates();
        }

        private void RefreshGold(int gold)
        {
            if (goldText != null)
            {
                goldText.text = gold.ToString();
            }

            RefreshButtonStates();
        }

        private void RefreshButtonStates()
        {
            if (buildManager == null) return;

            foreach (var btn in towerButtons)
            {
                if (btn.button == null) continue;

                GameObject prefab = buildManager.availableTowerPrefabs[btn.index];
                int cost = buildManager.GetTowerCost(prefab);
                bool canAfford = buildManager.CurrentGold >= cost;

                btn.button.interactable = canAfford;

                if (btn.costText != null)
                {
                    btn.costText.color = canAfford ? Color.white : Color.red;
                }
            }
        }

        private void OnTowerBuilt(GameObject tower)
        {
            RefreshUI();
        }

        private void OnTowerSold(GameObject tower)
        {
            RefreshUI();
            HideTowerInfo();
        }

        private void OnTowerSelected(GameObject towerObj)
        {
            if (towerObj == null)
            {
                HideTowerInfo();
                return;
            }

            ShowTowerInfo(towerObj.GetComponent<TowerController>());
        }

        private void OnTileHovered(Transform tile, bool isValid)
        {
            // Visuelle Feedback für gültiges/ungültiges Bauen
            // Könnte hier implementiert werden
        }

        public void ShowTowerInfo(TowerController tower)
        {
            if (towerInfoPanel == null || tower == null) return;

            towerInfoPanel.SetActive(true);

            if (towerNameText != null)
            {
                towerNameText.text = tower.towerId;
            }

            if (towerStatsText != null)
            {
                towerStatsText.text = $"Damage: {tower.damage:F0}\n" +
                                       $"Range: {tower.range:F1}\n" +
                                       $"APS: {tower.attacksPerSecond:F1}\n" +
                                       $"DPS: {tower.EstimatedDps:F1}";
            }

            if (towerCostText != null)
            {
                int sellValue = buildManager?.GetTowerSellValue(tower.gameObject) ?? 0;
                towerCostText.text = $"Sell: {sellValue}";
            }

            if (upgradeButton != null)
            {
                // Upgrade-Button deaktivieren wenn kein Upgrade verfügbar
                upgradeButton.interactable = false; // TODO: Upgrade-System implementieren
            }

            if (sellButton != null)
            {
                sellButton.interactable = true;
            }
        }

        public void HideTowerInfo()
        {
            if (towerInfoPanel != null)
            {
                towerInfoPanel.SetActive(false);
            }
        }

        private void OnUpgradeClicked()
        {
            // TODO: Upgrade-System implementieren
            UnityEngine.Debug.Log("DefenseHUD: Upgrade clicked (not implemented)");
        }

        private void OnSellClicked()
        {
            if (buildManager != null)
            {
                buildManager.SellSelectedTower();
            }
        }

        private void OnStartWaveClicked()
        {
            // Trigger Wave Start
            var levelController = FindFirstObjectByType<TowerConquest.Gameplay.LevelController>();
            if (levelController != null)
            {
                levelController.StartWave();
            }
        }

        public void UpdateWaveInfo(int currentWave, int maxWaves, int enemiesRemaining)
        {
            if (waveText != null)
            {
                waveText.text = $"Wave {currentWave}/{maxWaves}";
            }

            if (enemyCountText != null)
            {
                enemyCountText.text = $"Enemies: {enemiesRemaining}";
            }
        }

        public void SetStartWaveButtonActive(bool active)
        {
            if (startWaveButton != null)
            {
                startWaveButton.gameObject.SetActive(active);
            }
        }
    }
}
