using System;
using TowerConquest.Debug;
using System.Collections.Generic;
using TowerConquest.Core;
using TowerConquest.Data;
using TowerConquest.Progression;
using TowerConquest.Saving;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TowerConquest.UI
{
    /// <summary>
    /// Result screen shown after battle ends
    /// Shows victory/defeat, fame earned, statistics, and navigation options
    /// </summary>
    public class ResultScreenView : MonoBehaviour
    {
        [Header("Main Display")]
        public GameObject root;
        public Text resultLabel;
        public Text resultSubtitle;
        public Image resultBackground;

        [Header("Fame Display")]
        public Text fameEarnedText;
        public Text totalFameText;
        public GameObject fameAnimationTarget;

        [Header("Statistics")]
        public GameObject statsPanel;
        public Text unitsSpawnedText;
        public Text enemiesKilledText;
        public Text towersBuiltText;
        public Text battleTimeText;

        [Header("Star Rating")]
        public GameObject starContainer;
        public Image[] starImages;
        public Sprite starFilledSprite;
        public Sprite starEmptySprite;

        [Header("Navigation Buttons")]
        public Button worldMapButton;
        public Button retryButton;
        public Button nextLevelButton;

        [Header("Colors")]
        public Color victoryColor = new Color(0.2f, 0.6f, 0.2f, 0.9f);
        public Color defeatColor = new Color(0.6f, 0.2f, 0.2f, 0.9f);

        // Events
        public event Action OnWorldMapRequested;
        public event Action OnRetryRequested;
        public event Action OnNextLevelRequested;

        // State
        private bool isVictory;
        private int starsEarned;
        private string currentLevelId;
        private string nextLevelId;

        private void Awake()
        {
            SetupButtons();
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        private void SetupButtons()
        {
            if (worldMapButton != null)
            {
                worldMapButton.onClick.AddListener(OnWorldMapClicked);
            }

            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);
            }
        }

        /// <summary>
        /// Show results with basic info
        /// </summary>
        public void ShowResults(bool victory, bool nextLevelUnlocked)
        {
            ShowResults(victory, nextLevelUnlocked, 0, new BattleStats());
        }

        /// <summary>
        /// Show results with fame and statistics
        /// </summary>
        public void ShowResults(bool victory, bool nextLevelUnlocked, int fameEarned, BattleStats stats)
        {
            isVictory = victory;

            if (root != null)
            {
                root.SetActive(true);
            }

            // Get current level ID
            if (ServiceLocator.TryGet(out SaveManager saveManager))
            {
                currentLevelId = saveManager.GetOrCreateProgress().lastSelectedLevelId;
            }

            // Main result
            if (resultLabel != null)
            {
                resultLabel.text = victory ? "VICTORY" : "DEFEAT";
                resultLabel.color = victory ? Color.green : Color.red;
            }

            if (resultSubtitle != null)
            {
                resultSubtitle.text = victory
                    ? "Well done, Commander!"
                    : "Your forces have been defeated...";
            }

            if (resultBackground != null)
            {
                resultBackground.color = victory ? victoryColor : defeatColor;
            }

            // Fame display
            UpdateFameDisplay(fameEarned);

            // Statistics
            UpdateStatistics(stats);

            // Star rating
            starsEarned = CalculateStars(victory, stats);
            UpdateStarDisplay(starsEarned);

            // Save progress (this also finds and unlocks next level)
            SaveResults(victory, starsEarned);

            // Navigation buttons
            if (nextLevelButton != null)
            {
                // Show next level button only if victory and next level exists
                bool hasNextLevel = !string.IsNullOrEmpty(nextLevelId);
                nextLevelButton.gameObject.SetActive(victory && hasNextLevel);
                nextLevelButton.interactable = hasNextLevel;
            }

            Log.Info($"ResultScreenView: Victory={victory}, Fame={fameEarned}, Stars={starsEarned}");
        }

        private void UpdateFameDisplay(int fameEarned)
        {
            if (fameEarnedText != null)
            {
                fameEarnedText.text = $"+{fameEarned} Fame";
            }

            if (totalFameText != null && ServiceLocator.TryGet(out FameManager fameManager))
            {
                totalFameText.text = $"Total Fame: {fameManager.CurrentFame}";
            }
        }

        private void UpdateStatistics(BattleStats stats)
        {
            if (statsPanel != null)
            {
                statsPanel.SetActive(true);
            }

            if (unitsSpawnedText != null)
            {
                unitsSpawnedText.text = $"Units Spawned: {stats.unitsSpawned}";
            }

            if (enemiesKilledText != null)
            {
                enemiesKilledText.text = $"Enemies Killed: {stats.enemiesKilled}";
            }

            if (towersBuiltText != null)
            {
                towersBuiltText.text = $"Towers Built: {stats.towersBuilt}";
            }

            if (battleTimeText != null)
            {
                int minutes = Mathf.FloorToInt(stats.battleTime / 60f);
                int seconds = Mathf.FloorToInt(stats.battleTime % 60f);
                battleTimeText.text = $"Battle Time: {minutes:00}:{seconds:00}";
            }
        }

        private int CalculateStars(bool victory, BattleStats stats)
        {
            if (!victory) return 0;

            int stars = 1; // Base star for victory

            // Second star for efficiency (battle time under 5 minutes)
            if (stats.battleTime < 300f)
            {
                stars++;
            }

            // Third star for perfect (base HP > 50%)
            if (stats.baseHpPercent > 0.5f)
            {
                stars++;
            }

            return stars;
        }

        private void UpdateStarDisplay(int stars)
        {
            if (starImages == null || starImages.Length == 0) return;

            for (int i = 0; i < starImages.Length; i++)
            {
                if (starImages[i] != null)
                {
                    bool filled = i < stars;

                    if (starFilledSprite != null && starEmptySprite != null)
                    {
                        starImages[i].sprite = filled ? starFilledSprite : starEmptySprite;
                    }
                    else
                    {
                        // Fallback: use color
                        starImages[i].color = filled ? Color.yellow : Color.gray;
                    }
                }
            }
        }

        private void SaveResults(bool victory, int stars)
        {
            if (!ServiceLocator.TryGet(out SaveManager saveManager)) return;

            var progress = saveManager.GetOrCreateProgress();

            if (victory && !string.IsNullOrEmpty(currentLevelId))
            {
                progress.CompletLevel(currentLevelId, stars * 100, stars);

                // Unlock next level
                nextLevelId = FindNextLevel(currentLevelId);
                if (!string.IsNullOrEmpty(nextLevelId))
                {
                    progress.UnlockLevel(nextLevelId);
                    Log.Info($"[ResultScreenView] Unlocked next level: {nextLevelId}");
                }
            }

            saveManager.SaveProgress(progress);
        }

        /// <summary>
        /// Find the next level after the current one
        /// </summary>
        private string FindNextLevel(string currentId)
        {
            if (!ServiceLocator.TryGet(out JsonDatabase database)) return null;

            var levels = database.Levels;
            if (levels == null || levels.Count == 0) return null;

            // Find current level index
            int currentIndex = -1;
            for (int i = 0; i < levels.Count; i++)
            {
                if (levels[i]?.id == currentId)
                {
                    currentIndex = i;
                    break;
                }
            }

            // Return next level if exists
            if (currentIndex >= 0 && currentIndex < levels.Count - 1)
            {
                return levels[currentIndex + 1]?.id;
            }

            return null;
        }

        private void OnWorldMapClicked()
        {
            OnWorldMapRequested?.Invoke();

            if (Application.CanStreamedLevelBeLoaded("WorldMap"))
            {
                SceneManager.LoadScene("WorldMap");
            }
            else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
            {
                SceneManager.LoadScene("MainMenu");
            }
            else
            {
                Log.Info("[ResultScreenView] Would return to world map");
            }
        }

        private void OnRetryClicked()
        {
            OnRetryRequested?.Invoke();

            // Reload current scene
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnNextLevelClicked()
        {
            OnNextLevelRequested?.Invoke();

            // Set next level as selected
            if (!string.IsNullOrEmpty(nextLevelId) && ServiceLocator.TryGet(out SaveManager saveManager))
            {
                var progress = saveManager.GetOrCreateProgress();
                progress.lastSelectedLevelId = nextLevelId;
                saveManager.SaveProgress(progress);
            }

            // Load next level
            if (Application.CanStreamedLevelBeLoaded("LevelGameplay"))
            {
                SceneManager.LoadScene("LevelGameplay");
            }
            else
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Battle statistics for the result screen
    /// </summary>
    [Serializable]
    public struct BattleStats
    {
        public int unitsSpawned;
        public int enemiesKilled;
        public int towersBuilt;
        public float battleTime;
        public float baseHpPercent;
        public int goldEarned;

        public BattleStats(int units = 0, int enemies = 0, int towers = 0, float time = 0f, float hp = 1f, int gold = 0)
        {
            unitsSpawned = units;
            enemiesKilled = enemies;
            towersBuilt = towers;
            battleTime = time;
            baseHpPercent = hp;
            goldEarned = gold;
        }
    }
}
