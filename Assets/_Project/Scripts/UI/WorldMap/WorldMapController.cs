using System;
using TowerConquest.Debug;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerConquest.UI.WorldMap
{
    /// <summary>
    /// Controls the World Map screen where players select levels
    /// Displays level status icons (locked, unlocked, completed, perfect)
    /// </summary>
    public class WorldMapController : MonoBehaviour
    {
        public enum LevelStatus
        {
            Locked,
            Unlocked,
            Completed,
            Perfect
        }

        [Header("UI References")]
        [SerializeField] private Transform levelButtonContainer;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private Image backgroundImage;

        [Header("Navigation")]
        [SerializeField] private Button backToMenuButton;
        [SerializeField] private Button fameShopButton;

        [Header("Info Panel")]
        [SerializeField] private GameObject levelInfoPanel;
        [SerializeField] private Text levelNameText;
        [SerializeField] private Text levelDescriptionText;
        [SerializeField] private Text levelDifficultyText;
        [SerializeField] private Button playButton;

        [Header("Status Icons")]
        [SerializeField] private Sprite lockedIcon;
        [SerializeField] private Sprite unlockedIcon;
        [SerializeField] private Sprite completedIcon;
        [SerializeField] private Sprite perfectIcon;

        [Header("Status Colors")]
        [SerializeField] private Color lockedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private Color unlockedColor = new Color(1f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color completedColor = new Color(0.3f, 0.8f, 0.3f, 1f);
        [SerializeField] private Color perfectColor = new Color(1f, 0.9f, 0.2f, 1f);

        // Runtime
        private List<LevelButtonUI> levelButtons = new List<LevelButtonUI>();
        private string selectedLevelId;
        private JsonDatabase database;
        private SaveManager saveManager;
        private PlayerProgress playerProgress;

        public event Action<string> OnLevelSelected;
        public event Action OnFameShopRequested;
        public event Action OnBackToMenuRequested;

        private void Awake()
        {
            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(OnBackToMenuClicked);
            }

            if (fameShopButton != null)
            {
                fameShopButton.onClick.AddListener(OnFameShopClicked);
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }
        }

        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            database = ServiceLocator.Get<JsonDatabase>();
            saveManager = ServiceLocator.Get<SaveManager>();
            playerProgress = saveManager.GetOrCreateProgress();

            CreateLevelButtons();
            UpdateAllLevelStatuses();
            HideLevelInfo();
        }

        private void CreateLevelButtons()
        {
            // Clear existing buttons
            foreach (var btn in levelButtons)
            {
                if (btn != null)
                {
                    Destroy(btn.gameObject);
                }
            }
            levelButtons.Clear();

            if (levelButtonContainer == null) return;

            // Get all levels from database
            var levels = database?.GetAllLevels();
            if (levels == null || levels.Count == 0)
            {
                // Create default test levels
                CreateDefaultLevelButtons();
                return;
            }

            foreach (var level in levels)
            {
                CreateLevelButton(level);
            }
        }

        private void CreateDefaultLevelButtons()
        {
            // Create some test level buttons
            string[] defaultLevels = new string[]
            {
                "lvl_01_etruria_outpost",
                "lvl_02_gallic_frontier",
                "lvl_03_carthage_siege",
                "lvl_04_greek_colony",
                "lvl_05_germanic_forest"
            };

            for (int i = 0; i < defaultLevels.Length; i++)
            {
                var levelDef = new LevelDefinition
                {
                    id = defaultLevels[i],
                    display_name = $"Level {i + 1}",
                    description = $"Battle in the {defaultLevels[i].Replace("lvl_0" + (i + 1) + "_", "").Replace("_", " ")}",
                    aiDifficulty = i < 2 ? "easy" : (i < 4 ? "normal" : "hard")
                };

                CreateLevelButton(levelDef);
            }
        }

        private void CreateLevelButton(LevelDefinition level)
        {
            GameObject buttonObj;

            if (levelButtonPrefab != null)
            {
                buttonObj = Instantiate(levelButtonPrefab, levelButtonContainer);
            }
            else
            {
                buttonObj = CreateDefaultLevelButton();
                buttonObj.transform.SetParent(levelButtonContainer, false);
            }

            buttonObj.name = $"LevelButton_{level.id}";

            var levelButton = buttonObj.GetComponent<LevelButtonUI>();
            if (levelButton == null)
            {
                levelButton = buttonObj.AddComponent<LevelButtonUI>();
            }

            levelButton.Initialize(level.id, level.display_name, this);
            levelButton.OnClicked += OnLevelButtonClicked;

            levelButtons.Add(levelButton);
        }

        private GameObject CreateDefaultLevelButton()
        {
            GameObject buttonObj = new GameObject("LevelButton");

            var rectTransform = buttonObj.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120, 120);

            var image = buttonObj.AddComponent<Image>();
            image.color = unlockedColor;

            var button = buttonObj.AddComponent<Button>();
            button.targetGraphic = image;

            // Create name label
            GameObject labelGO = new GameObject("Label");
            labelGO.transform.SetParent(buttonObj.transform, false);
            var labelRect = labelGO.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 0.5f);
            labelRect.offsetMin = new Vector2(5, 5);
            labelRect.offsetMax = new Vector2(-5, 0);
            var labelText = labelGO.AddComponent<Text>();
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 14;
            labelText.alignment = TextAnchor.MiddleCenter;
            labelText.color = Color.white;

            // Create status icon
            GameObject iconGO = new GameObject("StatusIcon");
            iconGO.transform.SetParent(buttonObj.transform, false);
            var iconRect = iconGO.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(50, 50);
            iconRect.anchoredPosition = new Vector2(0, 10);
            var iconImage = iconGO.AddComponent<Image>();
            iconImage.color = Color.white;

            return buttonObj;
        }

        public void UpdateAllLevelStatuses()
        {
            playerProgress = saveManager.GetOrCreateProgress();

            // First level is always unlocked
            bool previousCompleted = true;

            foreach (var btn in levelButtons)
            {
                if (btn == null) continue;

                LevelStatus status = GetLevelStatus(btn.LevelId, previousCompleted);
                btn.UpdateStatus(status, GetStatusColor(status), GetStatusIcon(status));

                // Check if this level is completed for next level unlock
                previousCompleted = playerProgress.IsLevelCompleted(btn.LevelId);
            }
        }

        private LevelStatus GetLevelStatus(string levelId, bool previousCompleted)
        {
            // Check if perfect (3 stars)
            int stars = playerProgress.GetLevelStars(levelId);
            if (stars >= 3)
            {
                return LevelStatus.Perfect;
            }

            // Check if completed
            if (playerProgress.IsLevelCompleted(levelId))
            {
                return LevelStatus.Completed;
            }

            // Check if unlocked
            if (playerProgress.IsLevelUnlocked(levelId) || previousCompleted)
            {
                return LevelStatus.Unlocked;
            }

            return LevelStatus.Locked;
        }

        private Color GetStatusColor(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Locked: return lockedColor;
                case LevelStatus.Unlocked: return unlockedColor;
                case LevelStatus.Completed: return completedColor;
                case LevelStatus.Perfect: return perfectColor;
                default: return lockedColor;
            }
        }

        private Sprite GetStatusIcon(LevelStatus status)
        {
            switch (status)
            {
                case LevelStatus.Locked: return lockedIcon;
                case LevelStatus.Unlocked: return unlockedIcon;
                case LevelStatus.Completed: return completedIcon;
                case LevelStatus.Perfect: return perfectIcon;
                default: return lockedIcon;
            }
        }

        private void OnLevelButtonClicked(string levelId, LevelStatus status)
        {
            if (status == LevelStatus.Locked)
            {
                Log.Info($"[WorldMapController] Level {levelId} is locked");
                return;
            }

            selectedLevelId = levelId;
            ShowLevelInfo(levelId);
            OnLevelSelected?.Invoke(levelId);
        }

        private void ShowLevelInfo(string levelId)
        {
            if (levelInfoPanel != null)
            {
                levelInfoPanel.SetActive(true);
            }

            var level = database?.FindLevel(levelId);

            if (levelNameText != null)
            {
                levelNameText.text = level?.display_name ?? levelId;
            }

            if (levelDescriptionText != null)
            {
                levelDescriptionText.text = level?.description ?? "Complete this level to progress";
            }

            if (levelDifficultyText != null)
            {
                levelDifficultyText.text = $"Difficulty: {level?.aiDifficulty ?? "normal"}";
            }

            if (playButton != null)
            {
                playButton.interactable = true;
            }
        }

        private void HideLevelInfo()
        {
            if (levelInfoPanel != null)
            {
                levelInfoPanel.SetActive(false);
            }
            selectedLevelId = null;
        }

        private void OnPlayClicked()
        {
            if (string.IsNullOrEmpty(selectedLevelId))
            {
                Log.Warning("[WorldMapController] No level selected");
                return;
            }

            // Save selected level
            playerProgress.lastSelectedLevelId = selectedLevelId;
            if (!playerProgress.unlockedLevelIds.Contains(selectedLevelId))
            {
                playerProgress.unlockedLevelIds.Add(selectedLevelId);
            }
            saveManager.SaveProgress(playerProgress);

            // Load gameplay scene
            if (Application.CanStreamedLevelBeLoaded("LevelGameplay"))
            {
                SceneManager.LoadScene("LevelGameplay");
            }
            else
            {
                // Load current scene as fallback for testing
                Log.Info($"[WorldMapController] Starting level: {selectedLevelId}");
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        private void OnBackToMenuClicked()
        {
            OnBackToMenuRequested?.Invoke();

            if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Log.Info("[WorldMapController] Would return to main menu");
            }
        }

        private void OnFameShopClicked()
        {
            OnFameShopRequested?.Invoke();
            Log.Info("[WorldMapController] Opening Fame Shop");
            // Fame Shop would be opened here
        }

        /// <summary>
        /// Unlock the next level after completing current
        /// </summary>
        public void UnlockNextLevel(string completedLevelId)
        {
            int index = levelButtons.FindIndex(b => b.LevelId == completedLevelId);
            if (index >= 0 && index < levelButtons.Count - 1)
            {
                string nextLevelId = levelButtons[index + 1].LevelId;
                playerProgress.UnlockLevel(nextLevelId);
                saveManager.SaveProgress(playerProgress);

                UpdateAllLevelStatuses();
            }
        }

        private void OnDestroy()
        {
            foreach (var btn in levelButtons)
            {
                if (btn != null)
                {
                    btn.OnClicked -= OnLevelButtonClicked;
                }
            }
        }
    }
}
