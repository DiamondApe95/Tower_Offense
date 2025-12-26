using System;
using System.Collections.Generic;

namespace TowerConquest.Saving
{
    /// <summary>
    /// PlayerProgress: Speichert den Spielfortschritt des Spielers.
    /// </summary>
    [Serializable]
    public class PlayerProgress
    {
        // Level Progress
        public List<string> unlockedLevelIds = new();
        public List<string> completedLevelIds = new();
        public string lastSelectedLevelId;
        public string selectedGameMode = "offense";

        // Civilization & Fame Progress
        public List<string> unlockedCivilizationIds = new();
        public int fame;

        // Tutorial
        public bool tutorialCompleted;
        public bool offenseTutorialCompleted;
        public bool defenseTutorialCompleted;

        // Statistics
        public int totalLevelsCompleted;
        public int totalUnitsSpawned;
        public int totalEnemiesKilled;
        public int totalTowersBuilt;
        public int totalGoldEarned;
        public float totalPlayTime;

        // Achievements
        public List<string> unlockedAchievements = new();

        // Best Scores per Level
        public Dictionary<string, LevelScore> bestScores = new();

        // Settings State
        public string lastDifficulty = "normal";

        /// <summary>
        /// Markiert ein Level als abgeschlossen.
        /// </summary>
        public void CompletLevel(string levelId, int score = 0, int stars = 0)
        {
            if (!completedLevelIds.Contains(levelId))
            {
                completedLevelIds.Add(levelId);
                totalLevelsCompleted++;
            }

            // Update best score
            if (!bestScores.ContainsKey(levelId) || bestScores[levelId].score < score)
            {
                bestScores[levelId] = new LevelScore
                {
                    levelId = levelId,
                    score = score,
                    stars = stars,
                    completionTime = DateTime.Now.ToString("o")
                };
            }
        }

        /// <summary>
        /// Entsperrt ein Level.
        /// </summary>
        public void UnlockLevel(string levelId)
        {
            if (!unlockedLevelIds.Contains(levelId))
            {
                unlockedLevelIds.Add(levelId);
            }
        }

        /// <summary>
        /// Prüft ob ein Level abgeschlossen ist.
        /// </summary>
        public bool IsLevelCompleted(string levelId)
        {
            return completedLevelIds.Contains(levelId);
        }

        /// <summary>
        /// Prüft ob ein Level entsperrt ist.
        /// </summary>
        public bool IsLevelUnlocked(string levelId)
        {
            return unlockedLevelIds.Contains(levelId);
        }

        /// <summary>
        /// Gibt die Sterne für ein Level zurück.
        /// </summary>
        public int GetLevelStars(string levelId)
        {
            if (bestScores.TryGetValue(levelId, out LevelScore score))
            {
                return score.stars;
            }
            return 0;
        }

        /// <summary>
        /// Initialisiert Standard-Werte für neue Spieler.
        /// </summary>
        public void InitializeDefaults()
        {
            if (unlockedLevelIds.Count == 0)
            {
                unlockedLevelIds.Add("lvl_01_etruria_outpost");
            }

            tutorialCompleted = false;
            offenseTutorialCompleted = false;
            defenseTutorialCompleted = false;
            lastDifficulty = "normal";
        }

        /// <summary>
        /// Setzt den Fortschritt zurück.
        /// </summary>
        public void Reset()
        {
            unlockedLevelIds.Clear();
            completedLevelIds.Clear();
            lastSelectedLevelId = null;
            selectedGameMode = "offense";
            tutorialCompleted = false;
            offenseTutorialCompleted = false;
            defenseTutorialCompleted = false;
            totalLevelsCompleted = 0;
            totalUnitsSpawned = 0;
            totalEnemiesKilled = 0;
            totalTowersBuilt = 0;
            totalGoldEarned = 0;
            totalPlayTime = 0;
            unlockedAchievements.Clear();
            bestScores.Clear();
            lastDifficulty = "normal";

            InitializeDefaults();
        }
    }

    /// <summary>
    /// Score-Daten für ein Level.
    /// </summary>
    [Serializable]
    public class LevelScore
    {
        public string levelId;
        public int score;
        public int stars;
        public string completionTime;
    }
}
